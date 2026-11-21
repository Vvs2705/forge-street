# Fixa o adb no emulador. Lido (source) pelo soak_emulador.sh e pelo verify.sh android-emulator; pede ADB e fail().
# P0 (2026-11): com o POCO no USB e o AVD sem boot, o `adb wait-for-device` voltava com o POCO, o sys.boot_completed dele era 1 e o
# `adb uninstall` apagava o save do aparelho real. Agora: o serial vem da linha ^emulator- do `adb devices`, vai para o ANDROID_SERIAL
# (exportado: o adb e o medir_aparelho.sh o seguem) e o adb_emu recusa qualquer comando em serial que não comece com emulator-.

# emuladores: os seriais emulator-N que o adb lista, em qualquer estado (no boot o emulador aparece offline antes de virar device)
emuladores() { "$ADB" devices 2> /dev/null | tr -d '\r' | awk '$1 ~ /^emulator-[0-9]+$/ { print $1 }'; }

# fixar_emulador [SEGUNDOS=600]: espera o emulador aparecer, fixa o serial dele e espera o adb dele. Só aparelho físico (ou nada) no fim
# do prazo, ou 2 emuladores, aborta antes de qualquer install/uninstall
fixar_emulador() {
  local prazo="${1:-600}" e n fim
  fim=$((SECONDS + prazo))
  unset ANDROID_SERIAL
  while :; do
    e=$(emuladores); n=$(printf '%s\n' "$e" | grep -c .)
    [ "$n" -le 1 ] || fail "$n emuladores no adb ($(echo $e)): desligue os outros antes (nada foi instalado nem desinstalado)"
    [ "$n" = 1 ] && break
    [ $SECONDS -lt $fim ] || fail "nenhum emulador no adb em $prazo s (outros aparelhos: $("$ADB" devices 2> /dev/null | tr -d '\r' | awk 'NR > 1 && NF { printf "%s%s", s, $1; s = " " }')): nada foi instalado nem desinstalado"
    sleep 5
  done
  export ANDROID_SERIAL="$e"
  timeout "$prazo" "$ADB" -s "$ANDROID_SERIAL" wait-for-device || fail "o $ANDROID_SERIAL não ficou pronto no adb em $prazo s"
  echo "adb fixo no $ANDROID_SERIAL"
}

# adb_emu ARGS: o adb só no emulador fixado (-s explícito, além do ANDROID_SERIAL); MSYS_NO_PATHCONV: o Git Bash não troca /sdcard
adb_emu() {
  case "${ANDROID_SERIAL:-}" in
    emulator-*) ;;
    *) fail "adb $* recusado: o serial '${ANDROID_SERIAL:-}' não é de emulador (o uninstall apagaria o save do aparelho real)" ;;
  esac
  MSYS_NO_PATHCONV=1 "$ADB" -s "$ANDROID_SERIAL" "$@"
}

# desliga só o emulador fixado (trap EXIT); sem serial fixo, o qemu que este script ligou
desligar_emulador() {
  case "${ANDROID_SERIAL:-}" in
    emulator-*) "$ADB" -s "$ANDROID_SERIAL" emu kill > /dev/null 2>&1 && return ;;
  esac
  # Conselho do #189: sem serial (o fixar abortou, p.ex. com 2 emuladores) mata so o emulador que ESTE script ligou, nunca todos
  [ -n "${EMU_PID:-}" ] && kill "$EMU_PID" > /dev/null 2>&1
  return 0
}
