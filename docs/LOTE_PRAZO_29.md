# Lote do prazo — Tripo3D até 2026-11-29 (Forge Street)

A conta A do Tripo3D vence em **2026-11-29**. Este lote usa o que sobrar do Chronicles of Existence para gerar agora as peças que o Forge Street **já tem lugar no jogo e mostra em procedural ou não mostra**: os 15 luxos dos distritos 2–6 (hoje invisíveis, placar `PROJETO.md` §8 "E: 15 luxos invisíveis"), o pedestal do Hall (FS-6: "Hall só em painel, sem pedestal no mundo") e os dois luxos do Old Forge que ainda são procedurais. Mesmo formato do `chronicles-of-existence/docs/arte/LOTE_PRAZO_29.md` (PR #65 lá).

**Gerar agora, aprovar depois.** Nada daqui entra em `client/Assets/_FS/Resources/` antes de passar pelo Blender (`props_blender.py` / `render_sprites.py --dirs 1 --size 256`), pela foto 540×960 e pelo `verify.sh full`. Ligar cada luxo no `WorldView` é trabalho de código depois do dia 29, não deste lote.

**Aval:** `CLAUDE.md` exige aval explícito para crédito de IA. Este lote foi pedido pelo Vinicius no projeto TIME DE JOGOS (2026-11-27: gastar o crédito que vence no Courier e no Forge). Quem executa anota isso no ledger da `PROVENIENCIA.md` §1.

## 0. Regras que não mudam (leia uma vez)

1. **Privado sempre.** Toda geração com Privacidade = Privado. **Não usar o Agentic Canvas do Tripo**: ele não tem geração privada (testado no COE em 2026-11-27).
2. **Baixar no mesmo dia, tudo.** O zip/FBX de cada malha e o PNG da imagem escolhida. Depois do dia 29 a conta pode perder o histórico. Último download até **29/11 às 20h**.
3. **Onde salvar (no PC, fora do git):** `forge-street\arte\tripo\_prazo29\<id>\` com o nome `<id>_<tipo>_v01.<ext>` (`v02`… nunca sobrescreve). `arte/tripo/` já está no `.gitignore`: o repositório é **público**, então bruto do Tripo não vai para o GitHub.
4. **Uma linha por geração** em [`LOTE_PRAZO_29.csv`](LOTE_PRAZO_29.csv): `data,conta,id,tipo,task_id,creditos,arquivo,obs`. É a proveniência mínima; a linha completa da `PROVENIENCIA.md` §2 (SHA-256 do zip e do FBX) se escreve na integração.
5. **Encher a fila.** Mande 5 a 10 tarefas de uma vez, baixe as prontas, mande mais.
6. **Créditos reais do COE em 2026-11-27:** imagem GPT Image 2.5 ≈ 8 (Nano Banana 2 é grátis enquanto houver cota do dia: use primeiro), malha HD H3.5 ≈ 60, Auto Rig ≈ 20. Anote o valor que a tela mostrar.
7. **Parar na reserva.** As seções 3 e 4 são o lote; a §5 só roda se ainda houver crédito no fim da fila conjunta (§6).

## 1. Receita

| Tipo | Onde no Tripo | Opções |
|---|---|---|
| `img` | Imagem (Nano Banana 2; se acabar, GPT Image 2.5), 1:1, 4 variações | assunto da linha + `[P]` + `[I]`; fica com a que mostra a peça inteira, de 3/4, sem chão |
| `malha` | Imagem para 3D a partir da `img` escolhida | HD (H3.5), textura ligada, PBR ligado, 8K desligado, **Privado**; baixar FBX Blender 2K |
| `var` | 2ª malha da mesma peça (outra das 4 imagens) | só nas peças marcadas; dá escolha no G2 |

Nenhuma peça deste lote leva rig.

## 2. Bloco de estilo (cole depois do assunto)

Sufixos de sempre do `ASSETS.md` (os mesmos de todas as estações dos 6 distritos):

```text
[P] stylized low-poly game prop, flat colors with soft two-tone shading, chunky beveled shapes, single object, centered, no background, no text
[I] 3/4 front view product shot, plain light grey background, soft even light, no ground shadow
```

Reforço deste lote (cole no fim de toda linha): `no people, no ground plane, no letters or numbers on any sign. Do not copy any existing game, film or artist.`

Paleta por distrito (ART_BIBLE §2–§3.1, a mesma das estações já geradas):
- **Armorer's Row:** aço `#8C96A6`, couro `#7A4A2A`, rebite cobre `#C8743A`, pano azul-petróleo (o das montagens).
- **Artisan Quarter:** roxo de joia `#B07CF2`/`#5B2E8C` e ouro `#E0B23A`.
- **Royal Arsenal:** azul real `#2F64B8`/`#1F4E8C` e ouro.
- **Arcane Foundry:** ciano `#3FE0D0`, roxo-noite `#4B2E83`, prata `#A4AEBE`.
- **Grand Forge:** bronze `#A8743A`, mármore `#C9C4BA`, azul-noite `#1B2350`.

## 3. Lote 1 — os 15 luxos dos distritos 2–6 (prioridade máxima)

Cada luxo já existe no núcleo (`Distritos.*.cs`, `luxo: true`, +5% em toda venda do distrito) e só aparece como placa no chão. O id da tabela é o do upgrade e vira o nome da folha. Ordem = prioridade (o jogador vê primeiro a Row).

| # | id (upgrade) | distrito | assunto do prompt (+ `[P]` + `[I]` + reforço) | passos | créd. |
|---|---|---|---|---|---|
| F01 | `estandartes_armeiro` | Row | `a pair of tall armorer guild banner poles 3 m tall, dark iron poles with a bronze cap, long hanging steel-grey and teal blue cloth banners with a plain embroidered anvil-and-shield emblem, small stone base` | img → malha | 68 |
| F02 | `rua_calcada` | Row | `square section of new cobblestone street 2 x 2 m, neat cool grey-blue cut stones with a gold-brown border curb, slightly raised, seen from above at 60 degrees` | img → malha | 68 |
| F03 | `manequins` | Row | `two wooden armor display mannequins on round stands, one wearing a shiny steel plate armor with a teal blue tabard, the other a steel helmet and chainmail, side by side` | img → malha | 68 |
| F04 | `fachada_dourada` | Quarter | `ornate jeweler shop facade front 4 m wide, purple painted wood panels with gold trim, a gold-framed display window with small gems, arched door with a gold handle, small purple awning` | img → malha → var | 128 |
| F05 | `lanternas_rua` | Quarter | `elegant street lamp post 2.6 m, slim dark iron post with gold details, purple glass lantern with a warm soft light, small flower basket hanging from the arm` | img → malha | 68 |
| F06 | `fonte_bairro` | Quarter | `small round neighborhood fountain 2 m wide, white stone basin with a gold rim, a central column topped with a purple gem-shaped ornament, clear blue water` | img → malha → var | 128 |
| F07 | `flamulas_reais` | Arsenal | `three royal pennant poles of different heights in a row, long royal blue swallowtail pennants with a gold border and a plain gold crown emblem, dark iron poles with gold finials, stone base` | img → malha | 68 |
| F08 | `portao_real` | Arsenal | `royal arsenal gate of honor 4 m wide, two square white-grey stone pillars with gold crown ornaments on top, open royal blue iron double gate with gold spear tips` | img → malha → var | 128 |
| F09 | `torre_relogio` | Arsenal | `small square clock tower 6 m tall, grey stone base, royal blue painted wood upper part, a round gold clock face with simple hands and no numbers, small pointed blue roof with a gold finial` | img → malha → var | 128 |
| F10 | `vitrais` | Foundry | `freestanding stained glass window panel 2.5 m tall in a night-purple stone pointed-arch frame with silver trim, glowing cyan and purple geometric glass pattern, no figures` | img → malha | 68 |
| F11 | `runas_piso` | Foundry | `round floor rune circle 2.5 m wide, dark purple stone disk with glowing cyan carved geometric runes in two rings and a silver border, flat on the ground, seen from above at 60 degrees` | img → malha | 68 |
| F12 | `observatorio` | Foundry | `small arcane observatory turret 5 m tall, round night-purple stone tower with silver bands, a dome roof with an open slit and a silver telescope pointing up, tiny cyan crystal lights` | img → malha → var | 128 |
| F13 | `vitral_forja` | Grand Forge | `tall cathedral stained glass window 3 m in a white marble pointed-arch frame with bronze trim, warm gold, bronze and night-blue glass showing a plain anvil and hammer shape` | img → malha | 68 |
| F14 | `praca_sinos` | Grand Forge | `bell plaza frame 3.5 m wide, sturdy wooden beam frame on a white marble platform holding three bronze bells of different sizes, night-blue cloth wrapped on the posts` | img → malha → var | 128 |
| F15 | `estatua_heroi` | Grand Forge | `bronze statue of a heroic blacksmith on a white marble pedestal 3 m tall: broad shoulders, knee-length apron, bandana, raising a big hammer, the other hand on an anvil; stylized, simple shapes` | img → malha → var | 128 |

**Subtotal ≈ 1.320 créditos** (15 × 68 + 5 variações × 60). A estátua deve ler como o herói (`ferreiro`: ombros largos, avental até o joelho, bandana, martelo grande; ART_BIBLE §4), sem rosto de ninguém real.

## 4. Lote 2 — Hall e luxos do Old Forge

| # | id | uso no jogo | assunto do prompt (+ `[P]` + `[I]` + reforço) | passos | créd. |
|---|---|---|---|---|---|
| F16 | `pedestal_obra_oldforge` | pedestal das Masterworks (FS-6) e as 4 colunas da `JewelryDecor` (`WorldView.Pedestal`, hoje formas) | `display pedestal 1.1 m tall for a masterwork, warm grey stone base, short gold column, gold capital with a small purple velvet cushion on top, empty` | img → malha | 68 |
| F17 | `pedestal_obra_row` | Hall da Row | igual ao F16 com `steel and dark iron column with copper rivets, teal blue cushion` | img → malha | 68 |
| F18 | `pedestal_obra_quarter` | Hall do Quarter | igual ao F16 com `purple marble column with gold bands, purple velvet cushion` | img → malha | 68 |
| F19 | `pedestal_obra_arsenal` | Hall do Arsenal | igual ao F16 com `white stone column with royal blue and gold bands, blue cushion` | img → malha | 68 |
| F20 | `pedestal_obra_foundry` | Hall da Foundry | igual ao F16 com `night-purple stone column with silver bands and small glowing cyan runes, cyan crystal holder instead of a cushion` | img → malha | 68 |
| F21 | `pedestal_obra_grandforge` | Hall da Grand Forge | igual ao F16 com `white marble column with bronze bands, night-blue cushion` | img → malha | 68 |
| F22 | `fachada_nobre` | luxo `WorkshopFacade` do Old Forge (hoje soleira, postes e bandeirolas procedurais) | `noble blacksmith workshop entrance 4 m wide: gold-trimmed wooden lintel beam on two dark wooden posts, each post topped with a small gold star, a string of small colored pennants (steel blue, red, green, gold) hanging between them` | img → malha | 68 |
| F23 | `vitrine_joias` | luxo `JewelryDecor` (vitrine da joalheria) | `jeweler's glass display cabinet 1.4 m wide, dark wood with gold trim, glass top showing rings, necklaces and purple gems on purple velvet` | img → malha | 68 |

**Subtotal ≈ 544 créditos.** O pedestal entra vazio: a obra em cima é o ícone do item que já existe (`item_*`).

**Total do lote (§3 + §4) ≈ 1.860 créditos.**

## 5. Reserva (só se sobrar no fim da fila conjunta)

| # | o quê | créd. |
|---|---|---|
| R1 | `var` dos 8 luxos sem variação (F01–F03, F05, F07, F10, F11, F13) e de F22–F23 (10 peças) | 600 |
| R2 | `var` dos 6 pedestais (F16–F21) | 360 |

Imagens de chão (`grama`, `piso_oficina`; o "chão piorou" do placar) **não** entram aqui: o gerador de imagem é grátis e não vence com a conta.

## 6. Ordem global com o Dungeon Courier

A conta é uma só. Ordem de execução das duas filas: Courier **D01** (golem v2) → **F01–F15** → Courier D02–D07 (roupas) → **F16–F23** → reservas (R1, R2 e a do Courier). Fila do Courier: `dungeon-courier/docs/LOTE_PRAZO_29.md`. Total com reservas ≈ 3.860 créditos.

## 7. Depois do dia 29 (sem pressa)

1. `props_blender.py` / `render_sprites.py --dirs 1 --size 256` por pasta de `_prazo29`; foto 540×960 de cada distrito com `-buy` comprando os luxos.
2. Luxo no mundo: o `WorldView` liga a folha pela flag `Bought` (como o `Lux(...)` do Old Forge, linhas ~333–335), com o pop de 0,3 s que já existe. Teste novo no estilo do `DistritosTests.Arte_TodaEstacaoTemAFolha` para todo luxo.
3. Pedestal: trocar `WorldView.Pedestal` (formas) pela folha do distrito e pôr o Hall no mundo (FS-6).
4. Linha completa na `PROVENIENCIA.md` §2 (task, zip, FBX, SHA-256) e no `ASSETS.md`, como nos lotes anteriores.
