#!/usr/bin/env bash
# Teste do Save 2.0 no emulador: 0.6.0 com progresso -> instala o novo por cima -> progresso igual; reabrir; 1 byte corrompido.
set -u
export MSYS_NO_PATHCONV=1   # Git Bash reescrevia /sdcard/... para C:/Program Files/Git/sdcard/...
OUT="$(dirname "$0")/emu_save2"; rm -rf "$OUT"; mkdir -p "$OUT"
ADB="$LOCALAPPDATA/Android/Sdk/platform-tools/adb.exe"
EMU="$LOCALAPPDATA/Android/Sdk/emulator/emulator.exe"
P=br.com.vstack.forgestreet
ACT=$P/com.unity3d.player.UnityPlayerGameActivity
OLD="C:/Users/VINICIUS/Videos/MEUS PROJETOS/JOGOS NOVOS/03_FORGE_STREET/client/Builds/android/ForgeStreet-emu.apk"
NEW="C:/fsv/client/Builds/android/ForgeStreet-emu.apk"
F=/sdcard/Android/data/$P/files

log() { echo "== $*"; }
run() { if [ -n "${1:-}" ]; then "$ADB" shell "am start -n $ACT -e unity '$1'" >/dev/null; else "$ADB" shell "am start -n $ACT" >/dev/null; fi; }
fora() { "$ADB" shell input keyevent KEYCODE_HOME; sleep 4; "$ADB" shell am force-stop $P; sleep 1; }
gold() { tr -d '\r' | grep -o '^gold=[0-9]*' | head -1; }

"$EMU" -avd fs_playstore -no-window -no-audio -gpu swiftshader_indirect -crash-report-mode never -no-metrics -no-snapshot-save > "$OUT/emulador.log" 2>&1 &
"$ADB" wait-for-device
for i in $(seq 120); do [ "$("$ADB" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = 1 ] && break; sleep 2; done
for i in $(seq 60); do "$ADB" shell ls /sdcard/Android/data >/dev/null 2>&1 && break; sleep 2; done   # armazenamento externo montado
sleep 25   # sistema assenta depois do boot (1a tentativa: o app perdeu o foco em 0,6 s e a cena nao iniciou)
"$ADB" shell input keyevent KEYCODE_WAKEUP; "$ADB" shell wm dismiss-keyguard; "$ADB" shell am broadcast -a android.intent.action.CLOSE_SYSTEM_DIALOGS >/dev/null
log "boot: $("$ADB" shell getprop sys.boot_completed | tr -d '\r') (API $("$ADB" shell getprop ro.build.version.sdk | tr -d '\r'))"
"$ADB" logcat -c
# a cena iniciou? (Ads.Init loga "ADS ad_init" no Awake do Game)
cena() { for i in $(seq 30); do "$ADB" logcat -d | grep -q 'ADS ad_init' && { echo "cena ok"; return 0; }; sleep 2; done; echo "CENA NAO INICIOU"; "$ADB" exec-out screencap -p > "$OUT/sem_cena.png"; "$ADB" logcat -d > "$OUT/logcat.txt"; "$ADB" emu kill; exit 1; }

log "1) instala a 0.6.0 limpa e o bot joga 90 s"
"$ADB" uninstall $P >/dev/null 2>&1
"$ADB" install "$OLD" | tail -1
"$ADB" shell dumpsys package $P | grep -m1 versionName
run "-bot -speed 4"; cena; sleep 60; "$ADB" exec-out screencap -p > "$OUT/0.6_bot.png"; sleep 30; fora
"$ADB" shell run-as $P cat shared_prefs/$P.v2.playerprefs.xml > "$OUT/prefs_0.6.xml"
python -I -c "
import re,sys,urllib.parse,html
s=open(sys.argv[1],encoding='utf-8',errors='replace').read()
m=re.search(r'name=\"fs.save\">(.*?)</string>',s,re.S)
v=html.unescape(urllib.parse.unquote(m.group(1))) if m else ''
open(sys.argv[2],'w',encoding='utf-8').write(v)
print('fs.save no PlayerPrefs:', len(v), 'bytes;', [l for l in v.splitlines() if l.startswith(('gold=','up='))])
" "$OUT/prefs_0.6.xml" "$OUT/save_0.6.txt"
"$ADB" shell ls -la $F/ | tr -d '\r'

log "2) instala o Save 2.0 POR CIMA (-r) e abre sem flags"
"$ADB" install -r "$NEW" | tail -1
"$ADB" shell dumpsys package $P | grep -m1 versionName
"$ADB" logcat -c; run ""; cena; sleep 25; "$ADB" exec-out screencap -p > "$OUT/novo_migrado.png"; fora
"$ADB" shell ls -la $F/ | tr -d '\r'
"$ADB" shell cat $F/save.txt > "$OUT/save_novo1.txt"
head -4 "$OUT/save_novo1.txt" | tr -d '\r'
log "ouro 0.6: $(gold < "$OUT/save_0.6.txt") | ouro depois da migração: $(gold < "$OUT/save_novo1.txt")"
log "up 0.6: $(tr -d '\r' < "$OUT/save_0.6.txt" | grep -m1 '^up=') | up novo: $(tr -d '\r' < "$OUT/save_novo1.txt" | grep -m1 '^up=')"
"$ADB" shell grep -h 'save_' $F/diario.csv | tr -d '\r'

log "3) reabre (force-stop antes): progresso tem de continuar"
run ""; sleep 15; fora
"$ADB" shell cat $F/save.txt > "$OUT/save_novo2.txt"
log "ouro reaberto: $(gold < "$OUT/save_novo2.txt") | up: $(tr -d '\r' < "$OUT/save_novo2.txt" | grep -m1 '^up=')"

log "4) corrompe 1 byte do save.txt (gold=1 -> gold=9...) e reabre: carrega o .bak, quarentena, save_corrupt"
"$ADB" shell "sed -i 's/^gold=/gold=9/' $F/save.txt"
run ""; sleep 15; fora
"$ADB" shell ls -la $F/ | tr -d '\r'
"$ADB" shell cat $F/save.txt > "$OUT/save_novo3.txt"
log "ouro depois da corrupção: $(gold < "$OUT/save_novo3.txt") (antes: $(gold < "$OUT/save_novo2.txt"))"
"$ADB" shell grep -h 'save_' $F/diario.csv | tr -d '\r'

"$ADB" logcat -d > "$OUT/logcat.txt"
log "exceções no logcat do pacote: $(grep -E "FATAL EXCEPTION|>>> $P <<<|ANR in $P" "$OUT/logcat.txt" | wc -l) fatais; $(grep -E ' Unity +: .*Exception' "$OUT/logcat.txt" | wc -l) linhas Unity com Exception"
grep -E ' Unity +: .*Exception' "$OUT/logcat.txt" | head -5

"$ADB" emu kill >/dev/null 2>&1; sleep 5
tasklist 2>/dev/null | grep -i qemu && taskkill //F //IM qemu-system-x86_64.exe >/dev/null 2>&1
log "fim"
