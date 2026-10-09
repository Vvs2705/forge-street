#!/bin/bash
# Ponto de entrada único de verificação do Forge Street (ticket A-PLAT-04). Git Bash no Windows.
# uso: client/tools/verify.sh quick | full | android-emulator | release | release-check <arquivo.aab|apk>
#   quick             núcleo (dotnet test) + viewcheck do PC e do Android, sem abrir o Unity
#   full              quick + Unity batchmode: EditMode, Setup.BuildWindows e o .exe com -autoplay 10
#   android-emulator  Setup.BuildAndroidEmu + emulador + adb install + abre com '-bot -speed 4' + BACK/HOME + logcat sem exceção
#   release           quick + Setup.BuildAndroidRelease (AAB assinado; exige FS_KEYSTORE, FS_KEYSTORE_PASS, FS_KEY_ALIAS, FS_KEY_PASS) + release-check
#   release-check     só confere um artefato pronto: targetSdk, versionCode, debuggable, ABI, 16 KB, assinatura e permissões
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
  # diff de conteúdo (o Unity regrava CRLF/LF sem mudar nada e o status acusaria) + arquivos novos
  local p=(client/ProjectSettings client/Assets client/Packages) s
  s=$(git -C "$ROOT" diff --name-only -- "${p[@]}"; git -C "$ROOT" ls-files --others --exclude-standard -- "${p[@]}")
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

# base = permissões do APK 0.6.0 (LevelPlay/Unity Ads, WorkManager, androidx). Permissão nova = revisar o Data safety antes de subir
PERMS="android.permission.INTERNET android.permission.ACCESS_NETWORK_STATE android.permission.VIBRATE android.permission.WAKE_LOCK
  android.permission.RECEIVE_BOOT_COMPLETED android.permission.FOREGROUND_SERVICE com.google.android.gms.permission.AD_ID
  android.permission.ACCESS_ADSERVICES_TOPICS android.permission.ACCESS_ADSERVICES_ATTRIBUTION $PKG.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION"

# confere um .aab ou .apk pronto, sem abrir o Unity; lista tudo o que reprova e só então sai 1
release_check() {
  local f="$1" A="${UNITY%/*}/Data/PlaybackEngines/AndroidPlayer" n=0 t
  [ -f "$f" ] || fail "artefato não achado: $f"
  local java="$A/OpenJDK/bin/java.exe" bt=("$A"/Tools/bundletool-all-*.jar) bin=("$A"/SDK/build-tools/*)
  local elf="$A/NDK/toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-readelf.exe"; bin="${bin[-1]}"
  for t in "$java" "${bt[0]}" "$elf" "$bin/aapt2.exe"; do [ -f "$t" ] || fail "ferramenta do Unity não achada: $t"; done
  bad() { echo "REPROVA: $*"; n=$((n + 1)); }

  # manifesto: o AAB guarda em protobuf (bundletool devolve XML); o APK sai pelo aapt2 badging
  local m vc vn min tgt dbg perms aab=0; [[ "$f" == *.aab ]] && aab=1
  if [ $aab = 1 ]; then
    m=$("$java" -jar "${bt[0]}" dump manifest --bundle="$f") || fail "bundletool dump manifest"
    get() { echo "$m" | grep -m1 -oE "android:$1=\"[^\"]*" | cut -d'"' -f2; }
    dbg=$(get debuggable)
    perms=$(echo "$m" | grep -oE '<uses-permission[^>]*' | grep -oE 'android:name="[^"]*' | cut -d'"' -f2)
  else
    m=$("$bin/aapt2.exe" dump badging "$f") || fail "aapt2 dump badging"
    get() { echo "$m" | grep -m1 -oE "(^| )$1[=:]'[^']*" | cut -d"'" -f2; }
    echo "$m" | grep -q '^application-debuggable' && dbg=true
    perms=$(echo "$m" | grep -oE "^uses-permission: name='[^']*" | cut -d"'" -f2)
  fi
  vc=$(get versionCode); vn=$(get versionName); min=$(get minSdkVersion); tgt=$(get targetSdkVersion)
  echo "     $f: $vn (versionCode ${vc:-?}), minSdk ${min:-?}, targetSdk ${tgt:-?}"

  [ "${tgt:-0}" -ge 36 ] && ok "targetSdk $tgt" || bad "targetSdk ${tgt:-?} < 36 (exigência da Play desde 31/08/2026)"
  # mesmo esquema do Setup.VersionCode: maior*10000 + menor*100 + patch
  local exp=?; [[ "$vn" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]] && exp=$((10#${BASH_REMATCH[1]} * 10000 + 10#${BASH_REMATCH[2]} * 100 + 10#${BASH_REMATCH[3]}))
  [ "${vc:-0}" -gt 1 ] && [ "$vc" = "$exp" ] && ok "versionCode $vc" || bad "versionCode ${vc:-?}: para $vn o esperado é $exp (e sempre > 1, o último publicado)"
  [ "$dbg" = true ] && bad "debuggable=true (build de desenvolvimento)" || ok "não-debuggable"

  local so abis; so=$(unzip -Z1 "$f" | grep '\.so$')
  abis=$(echo "$so" | sed -nE 's#^(.*/)?lib/([^/]+)/.*#\2#p' | sort -u | paste -sd' ')
  [ "$abis" = arm64-v8a ] && ok "ABI só arm64-v8a" || bad "ABIs '${abis:-nenhuma}' (só arm64-v8a)"

  # 16 KB: todo segmento LOAD de todo .so com Align >= 0x4000 (o unzip do Git Bash não casa curinga: extrai pelo nome exato)
  local tmp s ruins=(); tmp=$(cygpath -m "$(mktemp -d)"); mapfile -t s < <(echo "$so" | grep .)   # C:/...: o readelf é .exe nativo
  [ ${#s[@]} = 0 ] || unzip -qo "$f" "${s[@]}" -d "$tmp" || fail "unzip dos .so"
  for t in "${s[@]}"; do
    "$elf" -lW "$tmp/$t" | awk '$1 == "LOAD" { n++; if (strtonum($NF) < 16384) b = 1 } END { exit !n || b }' || ruins+=("${t##*/}")
  done
  rm -rf "$tmp"
  [ ${#ruins[@]} = 0 ] && ok "16 KB: ${#s[@]} .so com LOAD align >= 0x4000" || bad "16 KB: LOAD align < 0x4000 em ${ruins[*]}"
  # 16 KB no zip: só importa para .so sem compressão (o Unity 6 usa useLegacyPackaging, .so comprimidos)
  if [ $aab = 1 ]; then
    t=$("$java" -jar "${bt[0]}" dump config --bundle="$f" | tr -d ' \r\n')
    [[ "$t" == *'"uncompressNativeLibraries":{"enabled":true'* && "$t" != *PAGE_ALIGNMENT_16K* ]] \
      && bad "AAB com .so sem compressão e sem PAGE_ALIGNMENT_16K (bundletool dump config)" || ok "zip: .so comprimidos ou com PAGE_ALIGNMENT_16K (bundletool dump config)"
  else
    "$bin/zipalign.exe" -c -P 16 4 "$f" > /dev/null && ok "zipalign -c -P 16: ok" || bad "zipalign -c -P 16 4 reprovou"
  fi

  # assinatura: AAB é jar (jarsigner); APK v2/v3 (apksigner). A chave de debug reprova
  local sig
  if [ $aab = 1 ]; then
    sig=$("$A/OpenJDK/bin/jarsigner.exe" -J-Duser.language=en -verify -verbose:summary -certs "$f" 2>&1); [[ "$sig" == *"jar verified."* ]] || sig=
  else
    sig=$("$java" -jar "$bin/lib/apksigner.jar" verify --print-certs "$f" 2>&1) || sig=
  fi
  if [ -z "$sig" ]; then bad "sem assinatura válida"
  elif [[ "$sig" == *"CN=Android Debug"* ]]; then bad "assinado com a chave de DEBUG"
  else ok "assinado: $(echo "$sig" | grep -m1 -oE 'CN=[^,]*')"; fi

  local p novas=() lista; lista=" $(echo $PERMS) "
  for p in $perms; do [[ "$lista" == *" $p "* ]] || novas+=("$p"); done
  echo "     permissões: $(echo "$perms" | sed 's/.*\.//' | paste -sd' ')"
  [ ${#novas[@]} = 0 ] || echo "AVISO: permissão fora da lista aprovada (revisar Data safety): ${novas[*]}"

  [ $n = 0 ] || fail "release-check: $n item(ns) reprovado(s) em $f"
  ok "release-check: $f"
}

release() {
  # sem as 4 variáveis nem abre o Unity: o release nunca cai na chave de debug (os valores não vão para log)
  local v; for v in FS_KEYSTORE FS_KEYSTORE_PASS FS_KEY_ALIAS FS_KEY_PASS; do [ -n "${!v:-}" ] || fail "defina $v (assinatura do release, ver README)"; done
  [ -f "$FS_KEYSTORE" ] || fail "keystore não achado: $FS_KEYSTORE"
  quick
  guard
  v=$(sed -nE 's/.*const string Version = "([^"]+)".*/\1/p' "$CLIENT/Assets/_FS/Editor/Setup.cs")
  local aab="$CLIENT/Builds/android/ForgeStreet-$v.aab"
  rm -f "$aab"   # o que for conferido é desta rodada
  build BuildAndroidRelease build_android_release.log Android
  dirty
  release_check "$aab"
}

case "${1:-}" in
  quick) quick ;;
  full) full ;;
  android-emulator) android_emulator ;;
  release) release ;;
  release-check) [ -n "${2:-}" ] || fail "uso: $0 release-check <arquivo.aab|apk>"; release_check "$2" ;;
  *) sed -n '3,8p' "$0"; exit 2 ;;
esac
echo "verify $1: OK em $((SECONDS - T0)) s"
