#!/bin/bash
# Ponto de entrada único de verificação do Forge Street (ticket A-PLAT-04). Git Bash no Windows.
# uso: client/tools/verify.sh quick | full | android-emulator
#   quick             núcleo (dotnet test) + viewcheck do PC e do Android, sem abrir o Unity
#   full              quick + Unity batchmode: EditMode, Setup.BuildWindows e o .exe com -autoplay 10
#   android-emulator  Setup.BuildAndroidEmu + emulador + adb install + abre com '-bot -speed 4' + BACK/HOME + logcat sem exceção
# Regra do PC (7,7 GB): um processo pesado por vez. Nunca Unity e emulador juntos; aborta se o Unity estiver aberto neste projeto.
# Variáveis opcionais: UNITY_EXE, ADB, EMULATOR, AVD (padrão fs_playstore). Logs desta rodada em client/Builds/verify/.
set -uo pipefail
export MSYS_NO_PATHCONV=1 DOTNET_CLI_UI_LANGUAGE=en   # caminhos do Windows passam intactos; saída do dotnet em inglês para o grep

ROOT="$(cd "$(dirname "$0")/../.." && pwd -W)"; CLIENT="$ROOT/client"; OUT="$CLIENT/Builds/verify"
UNITY="${UNITY_EXE:-C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe}"
SDK="$LOCALAPPDATA/Android/Sdk"; ADB="${ADB:-$SDK/platform-tools/adb.exe}"; EMU="${EMULATOR:-$SDK/emulator/emulator.exe}"
AVD="${AVD:-fs_playstore}"; PKG=br.com.vstack.forgestreet
T0=$SECONDS

ok()   { echo "OK   $* [$((SECONDS - T0)) s]"; }
fail() { echo "FALHOU: $*" >&2; exit 1; }
adb()  { "$ADB" "$@"; }

quick() {
  local sa="$CLIENT/Library/ScriptAssemblies" d log p
  # o viewcheck lê DLLs que só o Unity gera; sem elas seriam dezenas de erros confusos
  for d in UnityEngine.UI Unity.InputSystem Unity.LevelPlay; do
    [ -f "$sa/$d.dll" ] || fail "falta $sa/$d.dll. Abra o projeto no Unity 1 vez; numa worktree, copie as 3 DLLs de client/Library/ScriptAssemblies da pasta principal"
  done
  log=$(dotnet test "$CLIENT/tools/coretests" -nologo 2>&1) || { echo "$log"; fail "dotnet test do núcleo"; }
  ok "núcleo: $(echo "$log" | grep -m1 -oE 'Failed: +[0-9]+, Passed: +[0-9]+, Skipped: +[0-9]+, Total: +[0-9]+' | tr -s ' ')"
  # view = caminho do PC/Editor; android = os #if UNITY_ANDROID (Ads.cs/LevelPlay, JNI) que o view/ nunca compila
  for p in view android; do
    log=$(dotnet build "$CLIENT/tools/viewcheck/$p" -nologo -v q 2>&1) || { echo "$log"; fail "viewcheck $p"; }
    ok "viewcheck $p: $(echo "$log" | grep -oE '[0-9]+ (Warning|Error)\(s\)' | paste -sd, | tr -s ' ')"
  done
}

emu_on() { tasklist 2>/dev/null | grep -qiE '^(qemu-system|emulator)'; }

# antes de abrir o Unity: nada de Editor neste projeto nem emulador ligado
guard() {
  [ -f "$UNITY" ] || fail "Unity não achado em $UNITY (defina UNITY_EXE)"
  local cl me
  cl=$(powershell.exe -NoProfile -Command "(Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\").CommandLine" 2>/dev/null | tr 'A-Z\\' 'a-z/')
  me=$(echo "$CLIENT" | tr 'A-Z' 'a-z')
  echo "$cl" | grep -qF "$me" && fail "o Unity está aberto neste projeto ($CLIENT): feche o Editor antes"
  [ -n "$(echo "$cl" | tr -d '[:space:]')" ] && echo "AVISO: há outro Unity.exe aberto (PC de 7,7 GB)"
  emu_on && fail "o emulador está ligado: feche-o antes (nunca Unity e emulador juntos)"
  mkdir -p "$OUT"
}

# Unity em batchmode; o log é apagado antes, então o que estiver nele é desta rodada
run_unity() {
  local log="$OUT/$1"; shift
  rm -f "$log"
  "$UNITY" -batchmode -nographics -projectPath "$CLIENT" -logFile "$log" "$@"
}

# o Setup.Apply regrava settings; mostra o que mudou em arquivo versionado (git checkout -- se o gerador não mudou)
dirty() {
  local s; s=$(git -C "$ROOT" status --short -- client/ProjectSettings client/Assets client/Packages)
  [ -z "$s" ] || printf 'AVISO: o Unity mexeu em arquivos versionados:\n%s\n' "$s"
}

build() {  # build <método> <log> <BuildTarget>
  run_unity "$2" -quit -executeMethod "FS.EditorTools.Setup.$1"; local rc=$?
  local sum; sum=$(grep -m1 -o "BuildSummary($3): .*" "$OUT/$2")
  [ $rc -eq 0 ] && [[ "$sum" == *result=Succeeded* ]] || fail "$1 (saída $rc, '${sum:-sem BuildSummary}', ver $OUT/$2)"
  ok "$1: $sum"
}

full() {
  quick
  guard
  rm -f "$OUT/editmode.xml"
  run_unity editmode.log -runTests -testPlatform EditMode -testResults "$OUT/editmode.xml"   # veredito no XML, não no código de saída
  local run; run=$(grep -m1 -o '<test-run [^>]*>' "$OUT/editmode.xml" 2>/dev/null) || fail "EditMode sem XML (ver $OUT/editmode.log)"
  local n; n=$(echo "$run" | grep -oE '(total|passed|failed)="[0-9]+"' | paste -sd' ')
  [[ "$run" == *'result="Passed"'* ]] || fail "EditMode: $n (ver $OUT/editmode.xml)"
  ok "EditMode: $n"
  build BuildWindows build_win.log StandaloneWindows64
  rm -f "$OUT/autoplay.log"
  "$CLIENT/Builds/win/ForgeStreet.exe" -batchmode -nographics -autoplay 10 -logFile "$OUT/autoplay.log" || fail "autoplay saiu $? (ver $OUT/autoplay.log)"
  local a; a=$(grep -m1 -o 'AUTOPLAY OK.*' "$OUT/autoplay.log") || fail "sem 'AUTOPLAY OK' (ver $OUT/autoplay.log)"
  ok "autoplay: $a"
  dirty
}

android_emulator() {
  guard
  [ -f "$EMU" ] && [ -f "$ADB" ] || fail "emulador ou adb não achados (EMULATOR=$EMU ADB=$ADB)"
  build BuildAndroidEmu build_android_emu.log Android
  local apk="$CLIENT/Builds/android/ForgeStreet-emu.apk"
  [ -f "$apk" ] || fail "APK não achado: $apk"
  dirty
  # só agora o emulador (o Unity já saiu); desliga ao sair, com ou sem falha, para liberar a RAM
  "$EMU" -avd "$AVD" -no-window -crash-report-mode never -no-metrics > "$OUT/emulator.log" 2>&1 &
  # o guard provou que não havia emulador antes, então o qemu que sobrar é este
  trap 'adb emu kill > /dev/null 2>&1 || taskkill /F /IM qemu-system-x86_64.exe > /dev/null 2>&1' EXIT
  timeout 180 "$ADB" wait-for-device || fail "o emulador não apareceu no adb (ver $OUT/emulator.log)"
  local i=0
  until [ "$(adb shell getprop sys.boot_completed 2> /dev/null | tr -d '\r')" = 1 ]; do
    i=$((i + 1)); [ $i -le 90 ] || fail "o emulador não terminou o boot em 180 s"; sleep 2
  done
  ok "emulador $AVD ligado"
  adb install -r "$apk" > /dev/null || fail "adb install"
  adb logcat -c
  # aspas simples DENTRO das duplas: sem elas o shell do aparelho quebra o extra (docs/VALIDACAO_V06A.md)
  adb shell "am start -n $PKG/com.unity3d.player.UnityPlayerGameActivity -e unity '-bot -speed 4'" > /dev/null
  sleep 40
  adb shell pidof $PKG > /dev/null || fail "o jogo não está rodando 40 s depois de abrir"
  adb shell input keyevent KEYCODE_BACK; sleep 3
  adb shell input keyevent KEYCODE_HOME; sleep 3
  adb logcat -d > "$OUT/logcat.txt"
  grep -q "ADS ad_init" "$OUT/logcat.txt" || fail "sem 'ADS ad_init' no logcat: o Game não chegou ao Awake"
  # crash Java, crash nativo (tombstone), ANR e exceção logada pela Unity; o resto do sistema não conta
  local bad; bad=$( { grep -A1 "FATAL EXCEPTION" "$OUT/logcat.txt" | grep "Process: $PKG"
                      grep -E ">>> $PKG <<<|ANR in $PKG| Unity +: .*Exception" "$OUT/logcat.txt"; } | head -5)
  [ -z "$bad" ] || fail "erro no logcat (ver $OUT/logcat.txt):"$'\n'"$bad"
  ok "abriu com -bot -speed 4, BACK e HOME, logcat sem exceção"
}

case "${1:-}" in
  quick) quick ;;
  full) full ;;
  android-emulator) android_emulator ;;
  *) sed -n '3,6p' "$0"; exit 2 ;;
esac
echo "verify $1: OK em $((SECONDS - T0)) s"
