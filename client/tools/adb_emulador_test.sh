#!/bin/bash
# Teste do P0 do adb (adb_emulador.sh) com um adb falso, sem emulador nem aparelho: com só o POCO no USB o soak aborta antes de qualquer
# install/uninstall; 2 emuladores abortam; o adb_emu recusa serial de aparelho; o medir_aparelho.sh exige 1 aparelho. ~10 s.
# uso: bash client/tools/adb_emulador_test.sh
set -u
T="$(dirname "$0")"; W=$(mktemp -d); trap 'rm -rf "$W"' EXIT; erros=0
cat > "$W/adb" <<'EOF'
#!/bin/bash
echo "adb $*" >> "$ADB_LOG"
[ "$1" = -s ] && shift 2
case "$1" in
  devices) echo "List of devices attached"; IFS=';' read -ra L <<< "$DEVICES"; for d in "${L[@]}"; do printf '%s\t%s\n' ${d% *} ${d#* }; done ;;
  shell) case "$*" in *boot_completed*) echo 1 ;; esac ;;
esac
exit 0
EOF
printf '#!/bin/bash\nexit 0\n' > "$W/emu"; chmod +x "$W/adb" "$W/emu"; echo apk > "$W/x.apk"
confere() { if eval "$2"; then echo "ok   $1"; else echo "FALHA $1"; cat "$W/log" 2> /dev/null; erros=$((erros + 1)); fi; }
soak() { rm -f "$W/log"; DEVICES="$1" ADB_LOG="$W/log" ADB="$W/adb" EMULATOR="$W/emu" APK="$W/x.apk" EMU_ESPERA=3 \
  bash "$T/soak_emulador.sh" "$W/out" 0 > "$W/saida" 2>&1; }

soak "8a3f9c21 device"
confere "só o POCO: soak aborta sem install/uninstall/am" '! grep -qE "install|am start" "$W/log" && grep -q "nenhum emulador" "$W/saida"'
soak "emulator-5554 device;emulator-5556 offline"
confere "2 emuladores: soak aborta sem install" '! grep -q install "$W/log" && grep -q "2 emuladores" "$W/saida"'
rm -f "$W/log"
( ADB="$W/adb" ADB_LOG="$W/log"; fail() { exit 1; }; . "$T/adb_emulador.sh"; ANDROID_SERIAL=8a3f9c21 adb_emu uninstall x ) > /dev/null 2>&1
confere "adb_emu recusa serial que não é emulator-" '[ ! -f "$W/log" ]'
rm -f "$W/log"; DEVICES="8a3f9c21 device;emulator-5554 device" ADB_LOG="$W/log" ADB="$W/adb" bash "$T/medir_aparelho.sh" "$W/m" > "$W/saida" 2>&1; st=$?
confere "medir_aparelho com 2 aparelhos e sem ANDROID_SERIAL sai com 2" '[ $st = 2 ] && grep -q "2 aparelhos" "$W/saida" && [ "$(grep -vc "devices" "$W/log")" = 0 ]'
[ $erros = 0 ] && echo "adb_emulador: tudo ok" || { echo "adb_emulador: $erros falha(s)"; exit 1; }
