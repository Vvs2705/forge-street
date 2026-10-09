# Criativos UA 9:16 — Forge Street (Leva 13, GDD §17)

Gravados do build Windows de verdade (v0.4.1 + flag `-record`), jogado pelo bot ou pelos ajudantes. Zero IA, zero crédito, nada enviado nem publicado.
Saída em `client/Builds/creatives/` (fora do git): `.mp4` 1080×1920, H.264, 30 fps, yuv420p, faixa AAC muda (o `-record` não grava som); `fs_0X_contato.png` = folha de contato para revisar sem abrir o vídeo.

| id | arquivo | gancho (0–2 s) | duração | o que mostra | hipótese que testa |
|---|---|---|---|---|---|
| #1 | `fs_01_bigorna_rua.mp4` | "COMECEI COM UMA BIGORNA VAZIA..." | 25,7 s | oficina vazia (bigorna apagada) a 1×; time-lapse do bot com contador "MINUTO 0→44" (5 s de jogo por quadro); oficina completa a 1× | a transformação visual vazio → cheio reduz o CPI (GDD §16). Medir CPI/IPM contra a média dos 3 |
| #2 | `fs_02_fila_fole.mp4` | "FILA GIGANTE? A FORNALHA NÃO DÁ CONTA" | 21,3 s | fila de 5–6 clientes, bigornas com fome; compra Fole e Fole duplo no menu (ouro 680 → 335); trecho 4× com rótulo até "FILA ZERADA!" | "ache o gargalo e resolva" (problema → solução) prende melhor que o time-lapse. Medir hold de 3 s, CTR, CPI |
| #8 | `fs_08_erro_proposital.mp4` | "O QUE ELE FEZ DE ERRADO?" | 14,1 s | 4 bancadas (2 bigornas, escudos, ferramentas) e 1 fornalha sem fole: bancadas apagadas, fila cheia; pede comentário | o erro proposital gera comentários e hold barato. Medir comentários/1000 impressões, hold de 3 s, CPI |

Todos terminam com o cartão "FORGE STREET" (1,5 s, último quadro desfocado). Não há selo de loja.

**Honestidade:** só aparece gameplay real. Os trechos acelerados levam o rótulo "VÍDEO ACELERADO". Os estados montados com `-buyids` existem no jogo de verdade (o jogador só não comprou o Fole) e o #2 paga as duas compras com o ouro que tinha. No time-lapse do #1, a multidão de clientes saindo pelo topo fica mais densa que a 1×: eles andam na velocidade da animação enquanto a simulação roda a 50×.

## v0.6 (2026-10-09)

Regravados do build Windows da v0.6 (v0.6d) com o mesmo pipeline. Saída em `client/Builds/creatives/v06/`; os da v0.4.1 acima ficam onde estão. As folhas de contato estão no git, os MP4 não (os da v0.4.1 foram versionados no PR #3, apesar do "fora do git"; os novos ficaram fora para não somar ~35 MB).

| id | duração | o que mudou em relação ao corte da v0.4.1 |
|---|---|---|
| #8 | 14,1 s | Mesmos flags e filtro. Na v0.6 as 2 bigornas e as Ferramentas ficam apagadas até o quadro ~340 (Escudos e fornalha acesos); fila 4/4 com 1 venda em 13 s (`RECORD OK t=133,4 fila=4/4 ouro=363`). |
| #2 | 23,0 s | Gancho **"FILA CHEIA?"** (era "FILA GIGANTE?": desde a v0.5 o balcão tem 4 vagas e a fila fica em 3–4). Ouro 740 → 690 → 385 (as encomendas pagam). A Bigorna 2 fica apagada 100% até o quadro 333 e acesa 65% depois, então "BIGORNAS ACESAS..." entra aos 7,63 s (quadro 334) e "...E A FILA ANDOU" aos 11,85 s (quadro 840, a fila cai de 4 para 2). 4× do quadro 210 ao 1771; **"FILA ZERADA!" a 1×**, quadros 1771–1827 (1,9 s sem ninguém no balcão), sem o rótulo de acelerado; cartão sobre o quadro 1827. |
| #1 | 26,3 s | Time-lapse de 60 s (era 54): o bot da tela compra a Vitrine de joias, último produtivo, no quadro 536 (minuto 44; no `-autoplay` sai aos 41:31). Trim 2–548, contador "MINUTO 0→45". Estado final = **26 produtivos** (0–19 e 23–28: entraram as 4 vagas do balcão). |

- **Textos:** a câmera da v0.6 enquadra a oficina inteira e os textos no meio cobriam a fornalha; desceram para a faixa de baixo (y 1250–1620), longe das bancadas, da fornalha e da fila. No #2 o menu aberto ocupa y 1380–1740, então as linhas ficam em y 1250/1400 e cobrem só o título dos cartões.
- **HUD:** o `-record` já esconde o cartão da encomenda, o aviso de encomenda entregue, a engrenagem e a versão. Os botões VIP e 2× aparecem (no PC o anúncio simulado está sempre pronto): é a HUD real.
- **Como conferir a fila e a fome sem olhar quadro a quadro:** `ffmpeg -i "$Q/f2/f%05d.jpg" -vf "crop=100:80:350:185,signalstats,metadata=print:key=lavfi.signalstats.YAVG:file=y2.txt" -f null -` (YAVG ~214 = balão do 1º da fila; < 112 por 6+ quadros = fila vazia); Bigorna 2 com `crop=110:110:525:665` e `SATAVG` (> 26 = acesa). Os mesmos números de quadro valem enquanto o build e os flags não mudarem.

Comandos que mudaram (o resto é o bloco "Reproduzir" com `Q=.../fs_quadros_v06` e `OUT=../creatives/v06`):

```bash
# #2: mesma gravação; o cartão usa o quadro 1827
ffmpeg -y -framerate 30 -i "$Q/f2/f%05d.jpg" -loop 1 -framerate 30 -t 1.5 -i "$Q/f2/f01827.jpg" $MUDO -/filter_complex "$F/fs02.filter" -map "[v]" -map 2:a -shortest $ENC $OUT/fs_02_fila_fole.mp4
# #1: time-lapse de 60 s e estado final com os 26 produtivos (fs1a e o ffmpeg iguais)
./ForgeStreet.exe $W -bot -speed 50 -record "$Q/fs1b" -recordsec 60 -recordfps 10 -logFile "$Q/fs1b.log"
./ForgeStreet.exe $W -buyids 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,23,24,25,26,27,28 -warmup 90 -px 4.5,8.2 -record "$Q/fs1c" -recordsec 4 -logFile "$Q/fs1c.log"
# folhas de contato: fps 1/2.25 no #1, 1/1.96 no #2, 1/1.2 no #8
```

```text
# fs02.filter v0.6 (1x ate as 2 compras; 4x ate o quadro 1771; 1x com a fila vazia)
[0:v]split=3[s1][s2][s3];
[s1]trim=start_frame=12:end_frame=210,setpts=PTS-STARTPTS[a];
[s2]trim=start_frame=210:end_frame=1771,setpts=(PTS-STARTPTS)/4,fps=30,
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='VÍDEO ACELERADO 4×':fontsize=50:fontcolor=white:borderw=5:bordercolor=black:x=w-text_w-40:y=1185[b];
[s3]trim=start_frame=1771:end_frame=1828,setpts=PTS-STARTPTS,
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FILA ZERADA!':fontsize=160:fontcolor=0xFFD166:borderw=11:bordercolor=black:x=(w-text_w)/2:y=1250[c];
[a][b]concat=n=2:v=1:a=0,
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FILA CHEIA?':fontsize=150:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1250:enable='lt(t,4)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='A FORNALHA NÃO DÁ CONTA':fontsize=92:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1400:enable='between(t,1.2,4)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='MELHORA A FORNALHA':fontsize=108:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1260:enable='between(t,4,7.63)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FOLE + FOLE DUPLO':fontsize=92:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1400:enable='between(t,4.4,7.63)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='BIGORNAS ACESAS...':fontsize=110:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1260:enable='gte(t,7.63)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='...E A FILA ANDOU':fontsize=110:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1400:enable='gte(t,11.85)',
setsar=1[ab];
[c]setsar=1[c1];
[1:v]boxblur=24:2,eq=brightness=-0.25,drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FORGE STREET':fontsize=170:fontcolor=0xFFD166:borderw=12:bordercolor=black:x=(w-text_w)/2:y=(h-text_h)/2,setsar=1[card];
[ab][c1][card]concat=n=3:v=1:a=0,scale=out_range=tv:out_color_matrix=bt709,format=yuv420p[v]

# fs01.filter v0.6 (igual ao de cima, com o trim do time-lapse em 548, a troca de texto aos 21,2 s e os textos em y 1320-1470)
[0:v]trim=start_frame=12:end_frame=102,setpts=PTS-STARTPTS[a];
[1:v]trim=start_frame=2:end_frame=548,setpts=PTS-STARTPTS[b];
[2:v]trim=start_frame=12:end_frame=120,setpts=PTS-STARTPTS[c];
[a][b][c]concat=n=3:v=1:a=0,
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='COMECEI COM UMA':fontsize=110:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1320:enable='lt(t,3)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='BIGORNA VAZIA...':fontsize=150:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1450:enable='lt(t,3)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='MINUTO %{eif\:(15+(t-3)*150)/60\:d}':fontsize=120:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1330:enable='between(t,3,21.2)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='VÍDEO ACELERADO':fontsize=56:fontcolor=white:borderw=5:bordercolor=black:x=(w-text_w)/2:y=1470:enable='between(t,3,21.2)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='...E A OFICINA LOTOU!':fontsize=120:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1360:enable='gte(t,21.2)',
setsar=1[main];
[3:v]boxblur=24:2,eq=brightness=-0.25,drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FORGE STREET':fontsize=170:fontcolor=0xFFD166:borderw=12:bordercolor=black:x=(w-text_w)/2:y=(h-text_h)/2,setsar=1[card];
[main][card]concat=n=2:v=1:a=0,scale=out_range=tv:out_color_matrix=bt709,format=yuv420p[v]
```

## Reproduzir

Primeiro faça o build Windows (README). A janela do jogo precisa ficar visível durante a gravação (`-batchmode`/`-nographics` não têm tela para capturar). Cada quadro tem ~0,7 MB: grave fora do repo. A simulação não tem aleatoriedade e `-record` trava o relógio em 1/F s por quadro, então o mesmo comando gera os mesmos quadros. Os números de quadro dos filtros dependem disso.

```bash
cd client/Builds/win
Q="$(cygpath -m "$TEMP")/fs_quadros"; F="$Q/filtros"; mkdir -p "$F"   # salve os 3 filtros abaixo em $F
W="-screen-fullscreen 0 -screen-width 540 -screen-height 960 -testsession"
ENC="-c:v libx264 -preset slow -crf 20 -r 30 -pix_fmt yuv420p -color_range tv -colorspace bt709 -c:a aac -b:a 64k -movflags +faststart"
MUDO="-f lavfi -i anullsrc=r=44100:cl=stereo"; OUT=../creatives; mkdir -p $OUT

# #8: 4 bancadas, 1 fornalha sem fole (ids: 1 2a bigorna, 2 Ajudante, 3 Escudos, 5 Ajudante 2, 9 Ferramentas, 12 Ajudante 3, 10 Vitrine, 14 Martelo veloz)
./ForgeStreet.exe $W -buyids 1,2,3,5,9,12,10,14 -warmup 120 -px 4.5,8.2 -record "$Q/f8" -recordsec 13.4 -logFile "$Q/f8.log"
ffmpeg -y -framerate 30 -i "$Q/f8/f%05d.jpg" -loop 1 -framerate 30 -t 1.5 -i "$Q/f8/f00389.jpg" $MUDO -/filter_complex "$F/fs08.filter" -map "[v]" -map 2:a -shortest $ENC $OUT/fs_08_erro_proposital.mp4

# #2: só espadas, ajudantes, esteira, vitrine e martelo veloz; Fole (0) aos 294,5 s e Fole duplo (7) aos 296 s de jogo, cobrando o preço
./ForgeStreet.exe $W -buyids 1,2,5,6,12,13,10,14,0@294.5,7@296 -warmup 290 -px 4.5,8.2 -menu -record "$Q/f2" -recordsec 62.4 -logFile "$Q/f2.log"
ffmpeg -y -framerate 30 -i "$Q/f2/f%05d.jpg" -loop 1 -framerate 30 -t 1.5 -i "$Q/f2/f01799.jpg" $MUDO -/filter_complex "$F/fs02.filter" -map "[v]" -map 2:a -shortest $ENC $OUT/fs_02_fila_fole.mp4

# #1: gancho a 1x, time-lapse (bot, -speed 50 a 10 fps = 5 s de jogo por quadro), estado final = os 22 produtivos (o que o bot tem aos 44 min)
./ForgeStreet.exe $W -bot -record "$Q/fs1a" -recordsec 3.4 -logFile "$Q/fs1a.log"
./ForgeStreet.exe $W -bot -speed 50 -record "$Q/fs1b" -recordsec 54 -recordfps 10 -logFile "$Q/fs1b.log"
./ForgeStreet.exe $W -buyids 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,23,24 -warmup 90 -px 4.5,8.2 -record "$Q/fs1c" -recordsec 4 -logFile "$Q/fs1c.log"
ffmpeg -y -framerate 30 -i "$Q/fs1a/f%05d.jpg" -framerate 30 -i "$Q/fs1b/f%05d.jpg" -framerate 30 -i "$Q/fs1c/f%05d.jpg" -loop 1 -framerate 30 -t 1.5 -i "$Q/fs1c/f00119.jpg" $MUDO -/filter_complex "$F/fs01.filter" -map "[v]" -map 4:a -shortest $ENC $OUT/fs_01_bigorna_rua.mp4

# folha de contato (12 quadros); fps 1/2.2 no #1, 1/1.8 no #2, 1/1.2 no #8
ffmpeg -y -i $OUT/fs_08_erro_proposital.mp4 -vf "fps=1/1.2,scale=270:-1,tile=6x2" -frames:v 1 $OUT/fs_08_contato.png
```

Notas: os 12 primeiros quadros de cada gravação são descartados porque, no 1º quadro, os rótulos do mundo ainda estão na posição da janela anterior. Use `-px` com y ≥ 8: com o ferreiro embaixo, a câmera desce e a fila do balcão fica atrás do painel de ouro. Cada gravação termina com `RECORD OK t=… fila=…` no log.

### Filtros (`$F/fs08.filter`, `fs02.filter`, `fs01.filter`)

Cada bloco abaixo é um arquivo. As linhas `#` só identificam o bloco e não entram no arquivo.

```text
# fs08.filter
[0:v]trim=start_frame=12:end_frame=390,setpts=PTS-STARTPTS,
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='O QUE ELE FEZ':fontsize=130:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1330:enable='lt(t,3.2)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='DE ERRADO?':fontsize=150:fontcolor=0xFFD166:borderw=10:bordercolor=black:x=(w-text_w)/2:y=1470:enable='lt(t,3.2)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='BANCADAS APAGADAS...':fontsize=100:fontcolor=white:borderw=8:bordercolor=black:x=(w-text_w)/2:y=1350:enable='between(t,3.2,8)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='E A FILA QUASE NÃO ANDA':fontsize=90:fontcolor=0xFFD166:borderw=8:bordercolor=black:x=(w-text_w)/2:y=1470:enable='between(t,4.6,8)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='ACHOU O ERRO?':fontsize=140:fontcolor=0xFFD166:borderw=10:bordercolor=black:x=(w-text_w)/2:y=1330:enable='gte(t,8)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='COMENTA AÍ!':fontsize=120:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1480:enable='gte(t,9)'[a];
[1:v]boxblur=24:2,eq=brightness=-0.25,
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FORGE STREET':fontsize=170:fontcolor=0xFFD166:borderw=12:bordercolor=black:x=(w-text_w)/2:y=(h-text_h)/2,
setsar=1[b];
[a]setsar=1[a1];[a1][b]concat=n=2:v=1:a=0,scale=out_range=tv:out_color_matrix=bt709,format=yuv420p[v]

# fs02.filter (1x ate as 2 compras; depois 4x ate a fila zerar no quadro ~1620)
[0:v]split[s1][s2];
[s1]trim=start_frame=12:end_frame=210,setpts=PTS-STARTPTS[a];
[s2]trim=start_frame=210:end_frame=1800,setpts=(PTS-STARTPTS)/4,fps=30[b];
[a][b]concat=n=2:v=1:a=0,
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FILA GIGANTE?':fontsize=150:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=930:enable='lt(t,4)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='A FORNALHA NÃO DÁ CONTA':fontsize=92:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1085:enable='between(t,1.2,4)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='MELHORA A FORNALHA':fontsize=108:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=930:enable='between(t,4,6.6)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FOLE + FOLE DUPLO':fontsize=92:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1070:enable='between(t,4.4,6.6)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='BIGORNAS ACESAS...':fontsize=110:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=930:enable='between(t,6.6,18.35)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='...E A FILA ANDOU':fontsize=110:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1065:enable='between(t,11,18.35)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FILA ZERADA!':fontsize=160:fontcolor=0xFFD166:borderw=11:bordercolor=black:x=(w-text_w)/2:y=960:enable='gte(t,18.35)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='VÍDEO ACELERADO 4×':fontsize=50:fontcolor=white:borderw=5:bordercolor=black:x=w-text_w-40:y=1360:enable='gte(t,6.6)',
setsar=1[main];
[1:v]boxblur=24:2,eq=brightness=-0.25,drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FORGE STREET':fontsize=170:fontcolor=0xFFD166:borderw=12:bordercolor=black:x=(w-text_w)/2:y=(h-text_h)/2,setsar=1[card];
[main][card]concat=n=2:v=1:a=0,scale=out_range=tv:out_color_matrix=bt709,format=yuv420p[v]

# fs01.filter (minuto de jogo no time-lapse = (quadro+1) x 5 s)
[0:v]trim=start_frame=12:end_frame=102,setpts=PTS-STARTPTS[a];
[1:v]trim=start_frame=2:end_frame=530,setpts=PTS-STARTPTS[b];
[2:v]trim=start_frame=12:end_frame=120,setpts=PTS-STARTPTS[c];
[a][b][c]concat=n=3:v=1:a=0,
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='COMECEI COM UMA':fontsize=110:fontcolor=white:borderw=9:bordercolor=black:x=(w-text_w)/2:y=880:enable='lt(t,3)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='BIGORNA VAZIA...':fontsize=150:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=1010:enable='lt(t,3)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='MINUTO %{eif\:(15+(t-3)*150)/60\:d}':fontsize=120:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=900:enable='between(t,3,20.6)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='VÍDEO ACELERADO':fontsize=56:fontcolor=white:borderw=5:bordercolor=black:x=(w-text_w)/2:y=1040:enable='between(t,3,20.6)',
drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='...E A OFICINA LOTOU!':fontsize=120:fontcolor=0xFFD166:borderw=9:bordercolor=black:x=(w-text_w)/2:y=900:enable='gte(t,20.6)',
setsar=1[main];
[3:v]boxblur=24:2,eq=brightness=-0.25,drawtext=fontfile='C\:/Windows/Fonts/impact.ttf':text='FORGE STREET':fontsize=170:fontcolor=0xFFD166:borderw=12:bordercolor=black:x=(w-text_w)/2:y=(h-text_h)/2,setsar=1[card];
[main][card]concat=n=2:v=1:a=0,scale=out_range=tv:out_color_matrix=bt709,format=yuv420p[v]
```
