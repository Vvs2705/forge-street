#!/bin/bash
# Soak no emulador (docs/SOAK.md): o APK do emulador ja construido (verify.sh android-emulator ou Setup.BuildAndroidEmu) joga MIN minutos
# com o bot (-bot -speed 4 -soak) e, a cada minuto, amostra o PSS (dumpsys meminfo); o jogo poe no logcat 1 linha SOAK por minuto (fps,
# heap mono, reservado, GCs). No fim: SAIDA/soak.csv, o logcat e o veredito: PSS e mono sem crescer > 10% depois do minuto 10 e 0 excecao.
# uso: client/tools/soak_emulador.sh SAIDA [MIN=60]      (um processo pesado por vez: nada de Unity aberto)
set -u
SDK="${LOCALAPPDATA:-}/Android/Sdk"; ADB="${ADB:-$SDK/platform-tools/adb.exe}"; EMU="${EMULATOR:-$SDK/emulator/emulator.exe}"
AVD="${AVD:-fs_base}"; PKG=br.com.vstack.forgestreet   # AVD: o fs_playstore segue sem boot (ROTEIRO_TESTE_POCO.md §6)
OUT="${1:?pasta de saida}"; MIN="${2:-60}"
APK="$(dirname "$0")/../Builds/android/ForgeStreet-emu.apk"
mkdir -p "$OUT"
adb() { "$ADB" "$@"; }
fail() { echo "FALHOU: $*"; exit 1; }
[ -f "$APK" ] || fail "APK nao achado: $APK"
# EMU_ARGS: o Vulkan do emulador (vulkan.ranchu.so) derruba o build Development no vk::Image::SetLabel (SIGSEGV no UnityGfxDeviceW, minuto 8
# do 1o soak, 2026-10-25); sem ele a Unity cai no GLES 3 com a GPU do host. O Vulkan de verdade se mede no POCO.
"$EMU" -avd "$AVD" ${EMU_ARGS:--feature -Vulkan} -no-window -no-snapshot -crash-report-mode never -no-metrics > "$OUT/emulator.log" 2>&1 &
trap 'adb emu kill > /dev/null 2>&1 || taskkill /F /IM qemu-system-x86_64.exe > /dev/null 2>&1' EXIT
timeout 600 "$ADB" wait-for-device || fail "o emulador nao apareceu no adb em 10 min"
fim=$((SECONDS + 600))
until [ "$(adb shell getprop sys.boot_completed 2> /dev/null | tr -d '\r')" = 1 ]; do [ $SECONDS -lt $fim ] || fail "sem boot em 10 min"; sleep 5; done
sleep 25
adb uninstall $PKG > /dev/null 2>&1   # estado novo: a mesma partida em toda rodada
adb install -r "$(cygpath -m "$APK")" > /dev/null || fail "adb install"
adb logcat -c
adb shell "am start -n $PKG/com.unity3d.player.UnityPlayerGameActivity -e unity '-bot -speed 4 -soak'" > /dev/null
echo "min,pss_mb" > "$OUT/pss.csv"
for m in $(seq 1 "$MIN"); do
  sleep 60
  adb shell pidof $PKG > /dev/null || { echo "o jogo morreu no minuto $m" | tee "$OUT/morreu.txt"; break; }
  kb=$(adb shell dumpsys meminfo $PKG | tr -d '\r' | awk '/TOTAL PSS:/ { print $3; exit } /^ *TOTAL / { print $2; exit }')
  echo "$m,$(awk -v k="${kb:-0}" 'BEGIN { printf "%.1f", k / 1024 }')" >> "$OUT/pss.csv"
  [ $((m % 10)) = 0 ] && echo "minuto $m: PSS $(tail -1 "$OUT/pss.csv" | cut -d, -f2) MB"
done
adb logcat -d > "$OUT/logcat.txt"
python -I "$(dirname "$0")/soak_relatorio.py" "$OUT"
