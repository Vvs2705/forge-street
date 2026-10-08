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
