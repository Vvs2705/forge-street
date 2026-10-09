#!/bin/bash
# Mede o Forge Street no aparelho ligado pelo cabo (POCO F4): FPS (SurfaceFlinger), memória, temperatura e erros, e puxa o diário.
# uso: client/tools/medir_aparelho.sh SAIDA [SEGUNDOS_DE_JOGO_ANTES]   (o jogo já instalado e ABERTO, jogando na tela)
# Git Bash: as aspas e o MSYS_NO_PATHCONV evitam que /sdcard vire caminho do Windows.
set -u
A="${ADB:-$LOCALAPPDATA/Android/Sdk/platform-tools/adb.exe}"; P=br.com.vstack.forgestreet
OUT="${1:?pasta de saida}"; WAIT="${2:-0}"; mkdir -p "$OUT"
[ "$WAIT" -gt 0 ] && { echo "jogando ${WAIT}s antes de medir..."; sleep "$WAIT"; }
"$A" shell getprop ro.product.model > "$OUT/aparelho.txt"; "$A" shell dumpsys package $P | grep -m1 versionName >> "$OUT/aparelho.txt"
# FPS: camada do Unity (SurfaceView ... BLAST) do nosso pacote
# Android 13/14 lista so o nome; o 15 embrulha em "RequestedLayerState{NOME parentId=...}"
LAYER=$("$A" shell dumpsys SurfaceFlinger --list | tr -d '\r' | grep -F "$P" | grep -F "(BLAST)" | grep -m1 "SurfaceView" | sed -E 's/^RequestedLayerState\{//; s/ parentId=.*$//; s/\}$//')
echo "camada: $LAYER" | tee -a "$OUT/aparelho.txt"
"$A" shell dumpsys SurfaceFlinger --latency-clear "\"$LAYER\"" >/dev/null; sleep 3
"$A" shell dumpsys SurfaceFlinger --latency "\"$LAYER\"" > "$OUT/latency.txt"
python - "$OUT/latency.txt" <<'PY'
import sys
L=[l.split() for l in open(sys.argv[1]).read().split('\n')[1:] if len(l.split())==3]
t=[int(x[1]) for x in L if 0<int(x[1])<9e18]
d=sorted((b-a)/1e6 for a,b in zip(t,t[1:]) if b>a)
if len(d)<10: print("FPS: poucos quadros (camada errada?)"); sys.exit()
med=d[len(d)//2]; p95=d[int(len(d)*0.95)]
print(f"FPS: {1000*len(d)/((t[-1]-t[0])/1e6):.1f} medio | mediana {med:.1f} ms | p95 {p95:.1f} ms | pior {d[-1]:.1f} ms | >33 ms: {sum(x>33 for x in d)} de {len(d)}")
PY
"$A" shell dumpsys meminfo $P > "$OUT/meminfo.txt"; grep -E "TOTAL PSS|TOTAL:" "$OUT/meminfo.txt" | head -1
"$A" shell dumpsys battery > "$OUT/battery.txt"; echo "bateria: $(grep -m1 temperature "$OUT/battery.txt" | awk '{print $2/10}') C"
"$A" shell dumpsys thermalservice > "$OUT/thermal.txt" 2>/dev/null; grep -m3 -iE "Thermal Status|mStatus" "$OUT/thermal.txt"
"$A" logcat -d -s Unity | grep -iE "exception|error" | grep -v LoadFailed | sort | uniq -c | sort -rn | head -10 > "$OUT/erros.txt"; echo "erros Unity: $(wc -l < "$OUT/erros.txt") tipos"
MSYS_NO_PATHCONV=1 "$A" pull /sdcard/Android/data/$P/files/diario.csv "$OUT/diario.csv" | tail -1
echo "relatorio: python client/tools/diario_report.py $OUT"
