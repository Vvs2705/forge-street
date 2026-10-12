#!/bin/bash
# Mede o Forge Street no aparelho ligado pelo cabo (POCO F4): versão instalada, PAGE_SIZE, FPS (SurfaceFlinger), memória, temperatura e
# erros, e puxa o diário.
# uso: client/tools/medir_aparelho.sh SAIDA [SEGUNDOS_DE_JOGO_ANTES] [--temp-serie MINUTOS]   (o jogo já instalado e ABERTO, jogando na tela)
#   --temp-serie N   antes da medida final, N minutos com uma linha por minuto (0..N) em SAIDA/temperatura.csv: bateria em °C, status do
#                    thermalservice (0 nenhum, 1 leve, 2 moderado, 3 severo, 4 crítico, 5 emergência, 6 desligando) e FPS/p95 dos últimos
#                    ~127 quadros do SurfaceFlinger (vazio se a camada sumiu). Ex.: os 30 min do POCO_RELEASE_GATE = --temp-serie 30
# O diário sai de /sdcard/Android/data pelo adb (não precisa de APK debuggable), mas o release só grava com -playtest.
# Git Bash: as aspas e o MSYS_NO_PATHCONV evitam que /sdcard vire caminho do Windows.
set -u
A="${ADB:-$LOCALAPPDATA/Android/Sdk/platform-tools/adb.exe}"; P=br.com.vstack.forgestreet
OUT="${1:?pasta de saida}"; shift; WAIT=0; SERIE=0
while [ $# -gt 0 ]; do
  case "$1" in
    --temp-serie) SERIE="${2:-}"; shift; [ $# -gt 0 ] && shift ;;
    *) WAIT="$1"; shift ;;
  esac
done
[[ "$WAIT" =~ ^[0-9]+$ && "$SERIE" =~ ^[0-9]+$ ]] || { sed -n '4,7p' "$0"; exit 2; }
mkdir -p "$OUT"
sa() { "$A" shell "$@" | tr -d '\r'; }   # adb shell sem o \r do fim de linha

# fps ARQUIVO [csv]: quadros do "dumpsys SurfaceFlinger --latency"; csv = só "fps,p95_ms" (",": poucos quadros)
fps() {
  python - "$@" <<'PY'
import sys
L=[l.split() for l in open(sys.argv[1]).read().split('\n')[1:] if len(l.split())==3]
t=[int(x[1]) for x in L if 0<int(x[1])<9e18]
d=sorted((b-a)/1e6 for a,b in zip(t,t[1:]) if b>a)
csv=len(sys.argv)>2
if len(d)<10: print("," if csv else "FPS: poucos quadros (camada errada?)"); sys.exit()
f=1000*len(d)/((t[-1]-t[0])/1e6); med=d[len(d)//2]; p95=d[int(len(d)*0.95)]
print(f"{f:.1f},{p95:.1f}" if csv else f"FPS: {f:.1f} medio | mediana {med:.1f} ms | p95 {p95:.1f} ms | pior {d[-1]:.1f} ms | >33 ms: {sum(x>33 for x in d)} de {len(d)}")
PY
}
bateria() { sa dumpsys battery | awk '/temperature/ { print $2 / 10; exit }'; }
termico() { sa dumpsys thermalservice 2>/dev/null | grep -m1 -oE 'Thermal Status: *[0-9]+' | grep -oE '[0-9]+$'; }

[ "$WAIT" -gt 0 ] && { echo "jogando ${WAIT}s antes de medir..."; sleep "$WAIT"; }
# I-PLAT-02: o que está instalado (versionCode/targetSdk do dumpsys, não do APK que se acha que foi instalado) e a página de memória
{ sa getprop ro.product.model
  sa dumpsys package $P | grep -m2 -oE '(versionCode|versionName|targetSdk)=[^ ]+'
  echo "page_size=$(sa getconf PAGE_SIZE)"; } | tee "$OUT/aparelho.txt"
# FPS: camada do Unity (SurfaceView ... BLAST) do nosso pacote
# Android 13/14 lista so o nome; o 15 embrulha em "RequestedLayerState{NOME parentId=...}"
LAYER=$(sa dumpsys SurfaceFlinger --list | grep -F "$P" | grep -F "(BLAST)" | grep -m1 "SurfaceView" | sed -E 's/^RequestedLayerState\{//; s/ parentId=.*$//; s/\}$//')
echo "camada: $LAYER" | tee -a "$OUT/aparelho.txt"

if [ "$SERIE" -gt 0 ]; then
  CSV="$OUT/temperatura.csv"; echo "utc,minuto,bateria_c,thermal_status,fps,p95_ms" > "$CSV"
  echo "serie de temperatura: $SERIE min -> $CSV"
  T0=$SECONDS
  for ((m = 0; m <= SERIE; m++)); do
    s=$((T0 + 60 * m - SECONDS)); [ "$s" -gt 0 ] && sleep "$s"
    F=","; [ -n "$LAYER" ] && { "$A" shell dumpsys SurfaceFlinger --latency "\"$LAYER\"" > "$OUT/latency_serie.txt"; F=$(fps "$OUT/latency_serie.txt" csv); }
    echo "$(date -u +%Y-%m-%dT%H:%M:%S),$m,$(bateria),$(termico),$F" | tee -a "$CSV"
  done
fi

"$A" shell dumpsys SurfaceFlinger --latency-clear "\"$LAYER\"" >/dev/null; sleep 3
"$A" shell dumpsys SurfaceFlinger --latency "\"$LAYER\"" > "$OUT/latency.txt"
fps "$OUT/latency.txt"
"$A" shell dumpsys meminfo $P > "$OUT/meminfo.txt"; grep -E "TOTAL PSS|TOTAL:" "$OUT/meminfo.txt" | head -1
"$A" shell dumpsys battery > "$OUT/battery.txt"; echo "bateria: $(grep -m1 temperature "$OUT/battery.txt" | awk '{print $2/10}') C"
"$A" shell dumpsys thermalservice > "$OUT/thermal.txt" 2>/dev/null; grep -m3 -iE "Thermal Status|mStatus" "$OUT/thermal.txt"
"$A" logcat -d -s Unity | grep -iE "exception|error" | grep -v LoadFailed | sort | uniq -c | sort -rn | head -10 > "$OUT/erros.txt"; echo "erros Unity: $(wc -l < "$OUT/erros.txt") tipos"
if MSYS_NO_PATHCONV=1 "$A" pull /sdcard/Android/data/$P/files/diario.csv "$OUT/diario.csv" > /dev/null 2>&1; then
  echo "diario: $OUT/diario.csv"; echo "relatorio: python client/tools/diario_report.py $OUT"
else echo "sem diario.csv no aparelho (o release so grava com -playtest)"; fi
