# Áudio — mix do Forge Street

Fonte única do mix: o que toca, quando, com que volume e por quê. Código: `client/Assets/_FS/Scripts/View/Mixagem.cs` (política, C# puro) e `Som.cs` (`Sintese` gera os sons; `Sfx` só executa o que a `Mixagem` decide). Testes e medida: `client/Assets/_FS/Tests/EditMode/MixagemTests.cs`.

## 1. Por que (fix/mix-audio, 2026-10-13)

Vinicius, depois de 5 min no POCO: *"que barulheira, muitos sons ao mesmo tempo, uma zona sonora… eu mesmo me irritei"*. A meta é um jogo que dá prazer de ouvir por 30 min.

O que o código fazia até a 0.6 (conferido):

- **6 vozes em rodízio, sem prioridade nem categoria**: cheia, roubava a da vez — inclusive a fanfarra do VIP ou da encomenda no meio, para tocar uma moeda.
- **Intervalo só por nome do clip** (0,05–0,2 s): o martelo de **cada batida de cada bancada na tela** (uma a cada 0,45 s por bancada, ~130/min cada) só era limitado a 10/s no total; a esteira tocava ~4 clacs/s; moeda a cada venda (inclusive da joalheria, a cada 0,4 s) com a altura **subindo** a cada venda; sino a cada cliente; pegar/largar de todo ajudante e até a entrega da esteira.
- **Tudo a 0,55 de volume**: o golpe final da peça (−16 dBFS), a fornalha (−16) e o sino (−17) soavam **mais alto que a compra** (−17). Sem hierarquia, nada se destaca.
- **Música a 0,3 num laço de 28,8 s sem parar** (~60 voltas em 30 min) e ambiente que cresce +9,4 dB com a oficina cheia.

Medido pelo bot (§4): 401 sons/min aos 30 min, 54% do tempo com 3 ou mais sons juntos, 369 vozes cortadas, 3 Importantes cortados; com a oficina cheia, 702 sons/min e 84% do tempo com ≥ 3.

## 2. Níveis: RMS dos clips e alvo por categoria

Medido nos clips da `Sintese` (RMS da janela de 50 ms mais forte, no volume 1,0; "agudo" = parte da energia acima de 2 kHz). O nível de saída é esse RMS vezes o volume da regra. **Alvos**: Importante −14 dBFS, Ação −19, UI −21, Mundo −25 (perto do ferreiro, antes da distância). Clip agudo (> 60% acima de 2 kHz: moeda, sino, cofre, clique) leva −3 dB extra: o alto-falante do celular e o ouvido realçam 2–6 kHz, e é o agudo repetido que cansa.

| Som | Clip (s; RMS 50 ms; agudo) | Categoria | Volume antes → depois | Saída antes → depois (dBFS) |
|---|---|---|---|---|
| compra | upgrade (0,90; −12,2; 37%) | Importante | 0,55 → 0,82 | −17,4 → −13,9 |
| contrata | hire (0,75; −12,1; 45%) | Importante | 0,55 → 0,81 | −17,3 → −14,0 |
| vip, renome | vip (1,10; −10,7; 38%) | Importante | 0,55 → 0,69 | −15,9 → −13,9 |
| encomenda, obra, projeto | order (1,00; −11,6; 49%) | Importante | 0,55 → 0,76 | −16,8 → −14,0 |
| cofre, vipPago | offline (1,10; −12,7; 75%) | Importante | 0,55 → 0,60 | −17,9 → −17,1 (agudo) |
| meta, marco, bau | coin (0,45; −13,5; 77%) | Importante | 0,55 → 0,60–0,66 | −18,7 → −17,6 (agudo) |
| golpe (de mestre) | hammer (0,75; −11,2; 39%) | Ação | 0,55 → 0,57 | −16,3 → −16,0 (+3 dB: acerto de habilidade) |
| minerio | ore (0,20; −14,1; 13%) | Ação | 0,55 → 0,57 | −19,3 → −19,0 |
| pegar, largar | drop (0,16; −12,2; 11%) | Ação | 0,55 → 0,46 | −17,4 → −19,0 |
| venda | coin | Ação | 0,55 → 0,32 | −18,7 → −23,4 (agudo) |
| fina | coin (×1,15) | Ação | 0,55 → 0,33 | −18,7 → −23,1 (agudo) |
| click | click (0,06; −19,0; 73%) | UI | 0,55 → 0,55 | −24,2 (agudo) |
| peca (golpe final) | hammer | Mundo | 0,55 → 0,29 | −16,3 → −21,9 (+3 dB: acento do compasso) |
| lingote | furnace (0,70; −11,0; 34%) | Mundo | 0,55 → 0,20 | −16,2 → −24,9 |
| martelo (compasso) | martelo (0,40; −11,2; 44%) | Mundo | 0,25 → 0,20 | −23,3 → −25,2 |
| sino | client (0,80; −11,7; 68%) | Mundo | 0,55 → 0,20 | −16,9 → −25,6 (agudo; +2 dB: é aviso) |
| esteira | esteira (0,16; −15,2; 12%) | Mundo | 0,20 → 0,20 | −29,2 (tique mecânico, −4 dB) |
| ajudante | drop (×0,8) | Mundo | 0,55 → 0,16 | −17,4 → −28,2 (−3 dB: não é o jogador) |

Resultado: o Importante fica 5 dB acima da Ação e 11 dB acima do Mundo, que ainda cai com a distância e desce mais 6 dB quando um Importante toca.

**Música e ambiente** (RMS médio): música 0,3 → **0,18** = −29 → **−33 dB**; ambiente 0,15 → **0,11** com as camadas de densidade (fogo grave e rua densa) a 60%: oficina parada −35 → **−38 dB**, cheia −26 → **−30 dB** (o crescimento cai de +9,4 para **+7,7 dB**: ainda se ouve o negócio crescer, sem virar parede).

## 3. Regras por categoria

Orçamento: **4 vozes** no total (eram 6). A ordem das categorias é a prioridade.

| Categoria | Sons | Teto de vozes | Intervalo entre 2 da categoria | Distância / tela | Quando o orçamento está cheio |
|---|---|---|---|---|---|
| **Importante** | compra, contrata, boost, vip (VIP e compra na loja), renome, encomenda, obra, projeto, cofre, vipPago, meta (metas, marco de evento, Passe), marco, bau, supply, vipFoi, falhou | 1 | — | não atenua | **sempre toca**: rouba a voz de menor prioridade (a mais antiga). Outro Importante soando = **fila** (toca quando aquele acaba: fanfarras nunca se atropelam); o mesmo nome reinicia a própria voz. Enquanto soa, música, ambiente e Mundo descem **6 dB** por até 1 s e voltam em 0,3 s (ducking) |
| **UI** | click (e o "quase" do golpe) | 1 | — | — | rouba Mundo/Ação; o clique novo troca o velho |
| **Ação** | minerio, pegar, largar, venda, fina, golpe, perdeu | 2 | — | com posição (venda, perdeu): cheio até 2 m, cai até o **piso de 35%** | rouba só Mundo; no teto, o novo é barrado |
| **Mundo** | martelo, peca, lingote, esteira, sino, ajudante | 2 | **0,75 s** | **só com a fonte na tela**; cheio até 2 m do ferreiro, zero a 6 m; ganho < 0,2 (além de ~5,2 m) nem toca | nunca rouba; barrado |

Intervalo mínimo **por nome** (de qualquer fonte: todas as bancadas dividem o "martelo"), pitch e gatilho:

| Som | Intervalo | Pitch | Gatilho (antes → agora) |
|---|---|---|---|
| martelo | 3 s | por família (escudo 0,8, ferramenta 1,15, joia **1,25** — era 1,5) ±3% | cada batida de cada bancada na tela (0,1 s) → 1 martelada a cada 3 s, da bancada perto |
| peca | 2 s | 1 ±3% | `Ev.Crafted` de produto (0,12 s) |
| lingote | 2 s | 1 ±3% | `Ev.Crafted` de lingote (0,12 s) |
| esteira | 3 s | 0,9 + 0,1 × boost ±3% | cada divisa no tambor (~4/s) → 1 a cada 3 s |
| sino | 2 s | 1 ±3% | todo cliente (0,2 s) → **só quem chega à fila vazia** |
| ajudante | 4 s | 0,8 ±3% | pegar/largar de ajudantes e workers (antes iguais aos do ferreiro) e a entrega da esteira → só ajudantes (a esteira já fala pelo clac) |
| minerio, pegar, largar | 0,45 s | 1 / 1 / 1,1 ±2% | pegar/largar do ferreiro (0,08 s): numa carga de 6 itens (0,2 s cada) tocam 2–3 |
| venda | 2 s | **1, sempre** ±2% | cada venda (0,1 s, altura subindo a cada venda) → vendas a menos de 2 s viram 1 moeda (1,2 s dava ~50/min com a oficina cheia no build; coordenador, 2026-10-13) |
| fina | 6 s | 1,15 (Superior ×1,08) | cada peça Fina/Superior (1,35/1,6: agudo repetido) |
| perdeu | 3 s | 1 | cliente que cansou |
| golpe | 0,1 s | 1,25 | acerto do golpe de mestre |
| click | 0,05 s | 1 | botões |
| Importantes | — | da regra | fila e reinício (acima) |

A variação de pitch é uma sequência fixa (não sorteio): o que repete não soa igual duas vezes seguidas e nenhum som sobe de altura a cada repetição.

**Mesmos gatilhos no jogo e na medida:** `Mixagem.DoEvento` (evento do Sim → som) é chamado pelo `Game.HandleEvents` (via `Sfx.Evento`) e pela medida; compra, contratação/nível de worker e Renome tocam no `Game` (`Bought`, `WorkerComprado`, `SubiuRenome`), que a medida repete; compasso e esteira usam as funções puras da `StationView`/`EsteiraView`. Código de outra raia que ainda chama `Sfx.Play("<clip>")` (ex.: `"order"`) toca como Importante (`"click"` como UI).

## 4. Medida antes → depois (bot, sem Unity)

`MixagemTests.Medida_AntesDepoisPeloBot`: o bot F2P (o humano dos `BalanceTests`) joga pelo núcleo a 30 ticks/s; cada gatilho vai, no mesmo instante, para a política antiga (6 vozes em rodízio, intervalo por clip, os gatilhos da 0.6 congelados no teste) e para a nova; a cada tick conta as vozes soando (duração do clip ÷ pitch). Câmera do POCO (9:20, 9 m) seguindo o ferreiro, para a regra da tela.

```
dotnet test client/tools/coretests --filter Name=Medida_AntesDepoisPeloBot --logger "console;verbosity=detailed"
```

| Cenário | Sons/min | Pico de vozes | Tempo com ≥ 3 sons | Vozes cortadas | Importante cortado | Por min Importante/Ação/Mundo |
|---|---|---|---|---|---|---|
| bot F2P, 5 min (começo) | 139 → **47** | 6 → 3 | 4,8% → **0,4%** | 0 → 0 | 0 → **0** (1 esperou na fila) | 2/42/96 → 2/27/18 |
| bot F2P, 30 min | 401 → **73** | 6 → 4 | 53,6% → **0,9%** | 369 → 0 | 3 → **0** (8 na fila) | 2/61/339 → 2/38/33 |
| oficina cheia (`-equipe`), 5 min | 702 → **105** | 6 → 3 | 83,8% → **1,9%** | 423 → 0 | 4 → **0** | 2/93/607 → 3/53/48 |

Sons por minuto, por nome (política nova):

- **começo (5 min):** largar 10,8 · pegar 9,2 · martelo 8,6 · venda 5,6 · lingote 4,0 · peça 3,4 · minério 1,8 · ajudante 1,8 · compra 1,0 · encomenda 0,4 · marco/baú/contrata 0,2
- **30 min:** martelo 15,7 · largar 12,2 · venda 12,1 · pegar 11,2 · peça 7,4 · ajudante 6,4 · fina 2,3 · lingote 2,0 · esteira 1,8 · compra 0,8 · encomenda 0,3 · vip/vipPago 0,2
- **oficina cheia:** venda 21,6 · martelo 14,4 · largar 13,2 · peça 11,0 · pegar 10,6 · ajudante 10,0 · fina 7,4 · esteira 6,2 · lingote 6,0 · compra 1,2 · sino/marco/baú 0,8

Portões do teste (caem se uma mudança voltar a "zona"): começo ≤ 60 sons/min (meta do coordenador: 40–60), 30 min ≤ 80, oficina cheia ≤ 110; ≥ 3 sons juntos em ≤ 5% do tempo; Importante cortado = 0; pico ≤ 4; a antiga toca mais que o dobro da nova. Os portões das regras estão nos outros testes do `MixagemTests` e nas mutações `mix-*` do `client/tools/mutantes/mutantes.py`.

O começo é dominado pelo que o jogador faz (pegar/largar ~20/min, só dele); o Mundo virou textura (18/min, quieto e perto). Aos 30 min e com a oficina cheia o que mais cresce é a venda — o pagamento do loop, agrupado e sem subir de altura.

## 5. Música

Uma volta da música (uma das 5 da §10; a `Sintese.Musica` de 28,8 s só quando falta algum arquivo), depois **30 s só com o ambiente**, e volta (`Mixagem.MusicaPausa`). Entra em 0,4 s e sai nos últimos 2,5 s (`Mixagem.MusicaGanho`), sem estalo no fim; com a sintetizada, em 30 min eram ~30 voltas em vez de ~60; com as reais (2–5 min) são ~8, e o silêncio entre elas faz cada volta soar nova. Abaixa 6 dB sob os Importantes. Ligar a chave Música recomeça a volta do início.

## 6. Configurações

Sem mudança: as chaves **Música** e **Efeitos** (Configurações) seguem iguais — Efeitos desligado para efeitos e ambiente (e esvazia a fila dos Importantes), Música desligada para a música.

## 7. Conferir no build (coordenador)

`-somlog` (build dev): uma linha por minuto no Player.log / logcat, depois de 60 s de jogo:

```
SOM min=<n> plays=<tocados> pico=<vozes> cortados=<n> barrados=<n> fila=<n> porCategoria=Importante:<n>,Acao:<n>,Mundo:<n>,UI:<n>
```

`plays` = sons tocados no minuto, `pico` = mais vozes juntas, `cortados` = voz soando trocada por outro som (Importante nunca), `barrados` = pedidos que a política segurou (intervalo, teto, distância, fora da tela), `fila` = Importantes que esperaram outro acabar.

- começo: `-testsession -bot -somlog` → esperar `plays` perto de 45–50, `pico` ≤ 3–4, `cortados=0`;
- cenário cheio: `-testsession -equipe -bot -somlog` → `plays` ~100, `pico` ≤ 4;
- ouvido: compra/VIP/encomenda claramente por cima; martelo e esteira como fundo, somem longe da bancada; moeda sem subir de altura; música com pausa de 30 s.

## 8. Calibrar (revisar)

Números decididos pelo RMS e pela medida do bot, sem ouvir no aparelho (ponytail: calibrar no POCO, alto-falante e fone, e no PC). O que mais pode pedir ajuste de ouvido: o volume da categoria Mundo (−25 dBFS), o intervalo do martelo (3 s), o piso de 35% da venda longe, a pausa de 30 s da música e o ambiente cheio (−30 dB). Todos são uma linha em `Mixagem.Regras`/constantes da `Mixagem` ou do `Sfx`.

## 9. Efeitos reais CC0 (Kenney, 2026-10-23)

Sete sons da `Sintese` passam a tocar arquivos reais do banco CC0 da Kenney (15 WAVs; licença e SHA no `PROVENIENCIA.md` §19). O `Sfx.Init` carrega `Resources/Audio/<nome>/` (em ordem de nome) e só sintetiza quando a pasta não existe; as variações alternam como antes (`Som.K`).

| som | uso (§3) | arquivos (Kenney) | variações (antes → agora) |
|---|---|---|---|
| hammer | golpe final da peça, golpe de mestre | `impactMetal_heavy_000–002` | 3 → 3 |
| martelo | batida do compasso das bancadas | `impactMetal_medium_000–002` | 3 → 3 |
| ore | minério (pegar no depósito) | `impactMining_000–001` | 2 → 2 |
| drop | pegar, largar, ajudante | `impactWood_light_000–001` | 2 → 2 |
| esteira | clac da esteira | `impactPlate_light_000–001` | 2 → 2 |
| click | botões | `click_002` (interface-sounds) | 1 → 1 |
| coin | venda, fina, meta, marco, baú | `handleCoins`, `handleCoins2` (rpg-audio) | 3 → 2 |

**A mixagem não muda:** cada arquivo foi normalizado ao RMS 50 ms do clip sintetizado que substitui (coluna da §2), então os volumes da `Mixagem.Regras` e os alvos por categoria continuam valendo; o teste `SomTests.Cc0_NoNivelDoSintetizado` cai se um arquivo sair do nível (±1 dB), do formato (mono, 16 bits, 22 050 Hz) ou clipar. A duração muda (os impactos reais são mais curtos: 0,07–0,53 s contra 0,16–0,75 s), e a `Mixagem` mede a voz ocupada pelo `AudioClip.length` do 1º arquivo; a medida do bot da §4 segue com as durações da `Sintese` (núcleo sem Unity), um teto conservador: vozes mais curtas só baixam o pico e o tempo com ≥ 3 sons.

Continuam sintetizados: furnace (lingote), upgrade, hire, client (sino), leave, offline, vip, order e o ambiente (sem equivalente no banco escolhido). A música real está na §10. Para refazer: `python client/tools/audio_cc0.py <pasta com os zips extraídos>` (precisa do ffmpeg).

## 10. As 5 músicas (CC0, RandomMind, 2026-10-24)

GDD v2 §22 ("5 músicas: main, workshop, royal, event, arcane; medieval estilizado, sem cansar"). Cinco faixas CC0 do mesmo autor (RandomMind, OpenGameArt; licença, origem e SHA no `PROVENIENCIA.md` §23), em `client/Assets/_FS/Resources/Musica/<trilha>.ogg`, importadas como **Streaming** (Vorbis 0,5, sem pré-carga, carga em segundo plano: só o trecho que toca fica na memória).

| trilha | faixa | quando toca |
|---|---|---|
| main | The Old Tower Inn | a 1ª volta da sessão e a cada 4 voltas (o tema) |
| workshop | Market Day | a da casa no Old Forge, na Rua dos Armeiros e no Artisan Quarter |
| royal | King's Feast | a da casa no Royal Arsenal e na Grand Forge |
| arcane | Rising Moon | a da casa na Arcane Foundry |
| event | Rejoicing | com evento ativo, as voltas ímpares |

Regra (`Mixagem.Trilha`, pura, `MixagemTests.Musica_TrilhaPorDistritoEEvento`): volta 0 e múltiplas de 4 = main; com evento, ímpares = event; o resto = a da casa (`Mixagem.TrilhaDoDistrito`). Sem sorteio. Entre as voltas segue a pausa de 30 s da §5 (com entrada e saída suaves e o ducking dos Importantes). **Trocar de distrito** com a música ligada começa já a da casa nova (a viagem é o corte). Sem os 5 arquivos (projeto incompleto), toda volta toca a `Sintese.Musica`, como antes.

**Nível:** o `audio_musica.py` põe o RMS médio de cada faixa em −18 dBFS, o da `Sintese.Musica`, então o `MusicaVol` 0,18 continua dando os −33 dB da §2 e a hierarquia do mix não muda. Picos num joelho suave (teto 0,95), sem clipar. Para refazer: `python client/tools/audio_musica.py <pasta com os 5 mp3>` (precisa do ffmpeg com libvorbis).

**Calibrar (revisar):** ouvir no POCO se a música real, mais cheia que a sintetizada, pede 1–2 dB a menos no `MusicaVol`, e se a pausa de 30 s ainda faz sentido com faixas de 2–5 min.
