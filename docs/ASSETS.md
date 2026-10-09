# Forge Street — Assets do MVP (PROPOSTO, 2026-10-06)

Status: PROPOSTO pela raia 2; nada gerado ainda. Estilo e paleta em `ART_BIBLE.md`. Cadeia: **Tripo3D** (texto → imagem gerada dentro do site → 3D → rig) → **Blender** (orientar −Y, escalar em metros, decimar, limpar emissivo preto, Mixamo) → **sprites** (§6). Preços Tripo: **45 créditos/modelo, 20/rig** (LICOES 2026-10-01). "Min" = tempo de mão do dev solo (site + Blender + Mixamo), sem contar fila de geração. Estações contadas uma vez: Fornalha 2 e Bigorna 2 são instâncias.

Sufixos de prompt (colar ao fim do assunto):
- `[S]` personagem = "stylized low-poly game asset, chibi proportions 3 heads tall, flat colors with soft two-tone shading, clean readable silhouette, single character, centered, no background, no text, no logo"
- `[T]` pose = "T-pose, arms straight out horizontal, legs slightly apart, neutral face, symmetrical"
- `[P]` prop/estação = "stylized low-poly game prop, flat colors with soft two-tone shading, chunky beveled shapes, single object, centered, no background, no text"
- `[I]` **imagem-primeiro** (gerar DENTRO do Tripo antes do 3D; mesmo assunto da linha + este sufixo): personagem = "full body front view, T-pose arms horizontal, soft even studio light, plain light grey background, 3D render look, no ground shadow"; prop = "3/4 front view product shot, plain light grey background, soft even light, no ground shadow". Frontal verdadeira no slot frontal (lição Arkana 2026-08-26).

Alvos (saída do Tripo vem com 100–300 k tris; decimar no Blender antes de salvar o `.blend`): personagem 4–6 k tris / textura 512; estação 2–3 k / 512; item ≤ 300 / 128. Para sprites o polycount só pesa no repositório; os alvos valem se a opção 3D no Unity (§6-B) for escolhida depois.

## 1. Lote 1 — mínimo para o criativo de 3 s e o playtest

| # | Asset | Tipo | Prompt (assunto + sufixos) | Tris | Tex | Rig Mixamo | Cr | Min |
|---|---|---|---|---|---|---|---|---|
| 1 | Ferreiro (jogador) | Personagem | young blacksmith hero, broad shoulders, dark brown leather apron to the knees over cream shirt with rolled sleeves, oversized grey gloves, red bandana, big smithing hammer hooked on the belt, friendly round face, short dark hair `[S][T]` | 5 000 | 512 | sim | 65 | 45 |
| 2 | Ajudante (base única) | Personagem | small apprentice helper, round cloth cap, short leather apron, cream tunic, rolled sleeves, big simple boots, cheerful face `[S][T]` | 4 000 | 512 | sim | 65 | 40 |
| 3 | Cliente: Guerreira | Personagem | warrior woman adventurer, high ponytail, round wooden shield with iron rim on her back, shoulder pauldrons, blue tunic, leather belt, short sword at the hip `[S][T]` | 5 000 | 512 | sim | 65 | 40 |
| 4 | Cliente: Anão | Personagem | dwarf adventurer, short and wide body, long braided wedge-shaped auburn beard, round iron helmet with nose guard, heavy belt with pouches, big boots `[S][T]` | 5 000 | 512 | sim | 65 | 40 |
| 5 | Fornalha | Estação | stone brick smelting furnace, square body, arched mouth with glowing orange embers, short chimney with iron cap, iron hoops, small leather bellows on the side, small red lantern on the top-right corner `[P]` | 3 000 | 512 | — | 45 | 20 |
| 6 | Bigorna | Estação | iron anvil on a thick oak stump, wooden trip-hammer frame above it, wooden water bucket beside, small red lantern on a post `[P]` (pilão separado em objeto no Blender para animar) | 2 500 | 512 | — | 45 | 25 |
| 7 | Balcão | Estação | wooden shop counter with striped gold and cream awning canopy, gold trim, coin tray on top, blank hanging sign `[P]` | 2 500 | 512 | — | 45 | 20 |
| 8 | Depósito de minério | Estação | wooden mine cart full of brown ore rocks with orange glints, iron wheels, on a short rail piece `[P]` | 2 000 | 512 | — | 45 | 20 |
| 9 | Item: Minério | Item | Blender: ico-esfera + displace, 5–6 faces, cores §3 da bíblia (Tripo opcional 45: chunk of brown iron ore rock with orange veins `[P]`) | 120 | flat | — | 0 | 10 |
| 10 | Item: Lingote | Item | Blender: cubo trapezoidal chanfrado (Tripo opcional 45: beveled steel ingot bar `[P]`) | 60 | flat | — | 0 | 5 |
| 11 | Item: Espada | Item | Blender ou Tripo opcional 45: short broad fantasy sword, steel-blue blade, wide gold crossguard, dark wood grip, round pommel `[P]` | 300 | 128 | — | 0 | 15 |
| 12 | Item: Escudo | Item | Blender ou Tripo opcional 45: round red wooden shield, gold center boss, iron rim `[P]` | 200 | 128 | — | 0 | 10 |
| 13 | Item: Ferramenta | Item | Blender ou Tripo opcional 45: blacksmith hammer, steel head, green-wrapped wooden handle `[P]` | 150 | 128 | — | 0 | 10 |
| 14 | Pad de upgrade | Prop | Blender: cilindro baixo de pedra + torus de ouro; preenchimento continua procedural | 200 | flat | — | 0 | 10 |
| 15 | Chão da oficina + rua | Ambiente | Blender: plano com tile de pedra quente e tile de pedra fria (2 materiais, 256 px, sem costura) | 2 | 256 | — | 0 | 20 |
| 16 | Faíscas, fumaça, brilho da brasa | VFX | Unity `ParticleSystem` com 1 sprite (disco 16 px); glow aditivo na boca da fornalha | — | 16–32 | — | 0 | 30 |
| 17 | Ícones HUD (5 itens + moeda) | UI | render frontal dos próprios modelos a 128 px, fundo transparente | — | 128 | — | 0 | 20 |

**Subtotal Lote 1:** 8 modelos Tripo (4 personagens + 4 estações) + 4 rigs = **440 créditos** (665 se os 5 itens forem ao Tripo) · ≈ 6 h 20 de mão + ≈ 1 h 40 de render de sprites (§6).

## 2. Lote 2 — resto do MVP (GDD §18: 5 estações, 3 ajudantes, 15 upgrades)

| # | Asset | Tipo | Prompt (assunto + sufixos) | Tris | Tex | Rig Mixamo | Cr | Min |
|---|---|---|---|---|---|---|---|---|
| 18 | Cliente: Mago | Personagem | wizard, very tall pointed hat, long purple robe with wide sleeves, wooden staff with a green gem on top, short grey beard, leather satchel `[S][T]` | 5 000 | 512 | sim | 65 | 40 |
| 19 | Cliente: Elfa | Personagem | elf ranger woman, long pointed ears, moss-green hooded cape with pointed hood, wooden bow on her back, quiver, slim boots `[S][T]` | 5 000 | 512 | sim | 65 | 40 |
| 20 | Cliente: Mercador goblin | Personagem | goblin merchant, green skin, big pointed ears, long nose, huge backpack of goods bigger than his body (rolled rugs, pots, sacks), wide-brim hat, coin pouch `[S][T]` | 5 000 | 512 | sim | 65 | 40 |
| 21 | Cliente: Cavaleiro | Personagem | knight in full plate armor, closed helmet with a tall vertical red plume, kite shield on the left arm, blue tabard with a plain anvil emblem, sword at the hip `[S][T]` | 5 000 | 512 | sim | 65 | 40 |
| 22 | Bancada de escudos | Estação | blacksmith workbench with a rack holding three round red shields, bench vise, red cloth, small red lantern on the top-right corner `[P]` | 3 000 | 512 | — | 45 | 20 |
| 23 | Bancada de ferramentas | Estação | blacksmith workbench with a grindstone wheel, rack of hammers and tongs, green cloth, small red lantern on the top-right corner `[P]` | 3 000 | 512 | — | 45 | 20 |
| 24 | Fornalha 2, Bigorna 2 | Estação | instâncias de #5 e #6 | — | — | — | 0 | 0 |
| 25 | Ajudantes 1, 2, 3 | Personagem | base #2 + prop no Blender: cesto de vime (A1), pinças (A2), bandeja (A3); touca na cor do papel (§5 da bíblia) | +150 cada | 512 | herda | 0 | 30 |
| 26 | Esteira | Prop | Blender: módulo reto de 1 m, roletes (cilindros) + correia de couro (Tripo opcional 45: wooden roller conveyor with leather belt, straight modular segment `[P]`) | 400 | 128 | — | 0 | 20 |
| 27 | Pilhas de chão e na cabeça | Prop | reaproveitam os itens #9–13; 2 colunas como hoje | — | — | — | 0 | 0 |
| 28 | Balão do pedido | UI | 9-slice creme `#F4F6FA` com rabicho; ícone #17 dentro | — | 64 | — | 0 | 10 |
| 29 | VFX de compra e milestone | VFX | confete de moedas (sprite da moeda) + anel de ouro expandindo no pad | — | 32 | — | 0 | 20 |
| 30 | Skin da HUD e painel offline | UI | 9-slice madeira + ouro (2 tons); fonte continua a embutida | — | 128 | — | 0 | 30 |

**Subtotal Lote 2:** 6 modelos + 4 rigs = **350 créditos** (395 com a esteira no Tripo) · ≈ 5 h 10 de mão + ≈ 1 h de render.

## 3. Lote 3 — 2ª área (corredor lateral e joalheria: GDD §3 9:30, §5 D3, §20)

| # | Asset | Tipo | Prompt (assunto + sufixos) | Tris | Tex | Rig Mixamo | Cr | Min |
|---|---|---|---|---|---|---|---|---|
| 31 | Bancada de joalheria | Estação | jeweler's workbench with magnifier lamp, gem tray with colorful gems, small polishing wheel, purple cloth, small red lantern on the top-right corner `[P]` | 3 000 | 512 | — | 45 | 20 |
| 32 | Loja de joalheria (fachada) | Estação | small jewelry shop front with an opaque pale display case showing gems, purple awning, gold trim, blank sign `[P]` | 3 000 | 512 | — | 45 | 20 |
| 33 | Baú de milestone | Prop | ornate wooden treasure chest with iron bands, lid slightly open with warm gold glow inside `[P]` | 800 | 256 | — | 45 | 15 |
| 34 | Carroça de rua | Prop | wooden merchant cart with two spoked wheels, barrels and sacks, folded canvas `[P]` | 1 500 | 256 | — | 45 | 15 |
| 35 | Arco de entrada da rua | Ambiente | wooden street entrance arch with two hanging lanterns and a blank hanging sign `[P]` | 1 500 | 256 | — | 45 | 15 |
| 36 | Cliente: Nobre colecionador | Personagem | noble collector, velvet cape with gold trim, feathered cap, monocle, holding a small ring box `[S][T]` | 5 000 | 512 | sim | 65 | 40 |
| 37 | Postes, lampiões, barris, cercas | Prop | Blender: primitivas (cilindro, torus, caixa) com a paleta §2 | ≤ 300 | flat | — | 0 | 30 |
| 38 | Item: Joia (anel) | Item | Blender: torus + gema em losango; 6º matiz da cadeia: roxo `#B07CF2` | 150 | flat | — | 0 | 10 |
| 39 | Ajudantes 4–6 (visuais) | Personagem | base #2 + prop/cor novos | +150 | 512 | herda | 0 | 30 |

**Subtotal Lote 3:** 6 modelos + 1 rig = **290 créditos** · ≈ 3 h 15 de mão + ≈ 40 min de render.

## 4. Totais

| Lote | Modelos Tripo | Rigs | Créditos (rota recomendada) | Créditos (tudo no Tripo) | Mão de obra |
|---|---|---|---|---|---|
| 1 | 8 | 4 | **440** | 665 | ≈ 8 h |
| 2 | 6 | 4 | **350** | 395 | ≈ 6 h |
| 3 | 6 | 1 | **290** | 290 | ≈ 4 h |
| **Total** | **20** | **9** | **1 080** | 1 350 | ≈ 18 h |

39 linhas de asset; 20 geradas no Tripo, 19 feitas no Blender/Unity sem crédito. Cada geração entra em `docs/PROVENIENCIA.md` (ferramenta, plano pago, prompt, data, SHA-256).

## 5. Animações Mixamo por personagem

Fluxo: FBX já rigado pelo Tripo → Mixamo "Upload character" → baixar cada clip "With Skin" para o Blender (sprites) ou "Without Skin" para o Unity. ≈ 10 min por personagem para 5 clips.

| Personagem | Clip (nome no Mixamo) | Uso no jogo | Status do nome |
|---|---|---|---|
| Todos | `Idle` | parado na estação / na fila | confirmado |
| Todos | `Walking` | andar; jogador e ajudantes em 8 direções, clientes em 1 (descem a rua) | confirmado |
| Ferreiro, ajudantes | `Walking` + pose própria "braços erguidos" (1 quadro no Blender, camada NLA só nos ossos do tronco e braços) | carregar pilha na cabeça | pose própria: os clips de carregar do Mixamo seguram caixa à frente do peito, não na cabeça |
| Ferreiro | martelada própria: 3 keyframes no braço direito, 0,5 s, laço | criativo de 3 s (close na bigorna); no jogo a bigorna trabalha sozinha (martelo-pilão, 4 quadros) | `Hammering` a confirmar no catálogo; substituto confirmado: `Standing Melee Attack Downward` |
| Ferreiro | `Cheering` | comprou upgrade / milestone | confirmado |
| Ajudantes | `Looking Around` | espera na fonte vazia (`WorkerPatience` 1,2 s) | confirmado |
| Clientes | `Looking Around` → `Angry` | paciência < 50% → < 20% (barra continua) | confirmados; `Impatient Idle` / `Bored` a confirmar |
| Clientes | `Thankful` ou `Clapping` | recebeu o produto | confirmados |
| Clientes | `Waving` | sai feliz (opcional) | confirmado |
| Entregar / despejar (pouring, handing) | nenhum: o item voa da cabeça à pilha em 0,2 s (`TransferTime`) com squash do personagem | — | pulado de propósito: 0,2 s não comporta clip |

Lição aplicada: a pose T em jogo vinha dos CLIPS do placeholder, não do rig (LICOES 2026-09-30); em sprites isso vira um quadro em T no atlas, então cada atlas é conferido na foto `-shot` antes de entrar.

## 6. Render para sprites 2D ou cena 3D no Unity

| Critério | A) Blender renderiza sprites (view 2D atual) | B) Cena 3D no Unity |
|---|---|---|
| Dev solo | 1 script Blender (câmera 60°, 8 rotações, PNG transparente) + `SpriteAtlas`; sem importador de rig, Animator ou materiais | import FBX Humanoid, Animator Controller por papel, materiais URP, luz, sombras, LOD, ciclo de medição no aparelho |
| Android médio | ~300 quads, ≤ 10 draw calls, 8 atlases 2048² ASTC ≈ 12 MB; 60 fps sem medir | 15 skinned Humanoid (30–50% mais CPU que Generic) + 10 estações ≈ 100 k tris; 60 fps provável, mas é o "ajuste no aparelho" que deu CLI 5/10 ao Forge na raia C |
| Código atual | `WorldView` troca `Art.Disc()`/`Rounded()` por `atlas[anim][dir][frame]` em `BuildCarrier`, `BuildClient`, `BuildStation` (~120 linhas); `Sim`, `Game`, câmera, HUD e joystick intactos | `WorldView` reescrita (~400 linhas), câmera nova, XY → XZ, sorting por Y some; `Sim` intacto |
| Criativo 9:16 | renderizado no Blender com os mesmos modelos (câmera livre, bloom): melhor que in-game | filmável in-game; bloom/pós no URP mobile a configurar |
| Limites | zoom > 1,5× borra (render a 256 px se a 2ª área exigir zoom); luz e ângulo fixos; direção nova = re-render | zoom, câmera que segue e luz dinâmica de graça; temas cosméticos de forja mais baratos |
| Retrabalho | os modelos 3D servem aos dois; migrar para B depois não perde asset | — |

**Escolha:** **A) sprites pré-renderizados no Blender** — mantém view 2D e `Sim` como estão, roda em qualquer Android e os mesmos modelos rendem o criativo direto no Blender; B só se a 2ª área exigir zoom ou câmera livre, depois do gate de criativos.

Pipeline A, resumido:
1. `arte/forge_render.blend`: câmera ortográfica a 60° do chão, luz-chave quente + preenchimento frio fixos (bíblia §7), chão invisível (a sombra elipse continua no Unity).
2. Personagem: 8 rotações × (Idle 1 + Walking 8 + carregar 8 + Cheering 6 + Looking Around 4 + Angry 4 = 31 quadros) a 12 fps, 128 px de altura → 248 quadros num atlas 2048² (256 slots). Clientes: 1 direção de andar + 1 de fila ≈ 50 quadros.
3. Estações: 2 estados (ligada / apagada) × 1 ângulo; pilão da bigorna em 4 quadros; "travada" = tint vermelho em runtime, como hoje.
4. Itens: 1 quadro a 64 px (pilha, cabeça, balão) + 128 px (HUD).
5. Unity: um `SpriteRenderer` por portador; `frame = (int)(t × 12) % n`; direção = `atan2` do movimento quantizado em 8; `SpriteAtlas` por personagem.
6. Portão: foto `-shot` do APK no aparelho, silhuetas em preto a 30%, ícones a 24 px (bíblia §10).

## 7. v0.5 — leva 1 de arte procedural (2026-10-08, 0 crédito)

Pedido do Vinicius (2026-10-08): "a espada é apenas um triângulo azul nos pedidos [...] precisam parecer realmente espadas", balcão **sem toldo**, evolutivo de 4 a 8 vagas (+1 por evolução), e portão lateral certo. Direção: `BENCHMARK_VISUAL.md` §3–4 (PROPOSTO) + ajustes do coordenador. Tudo modelado por script no Blender 5.2 (primitivas, sem Tripo nem Mixamo), com a câmera e a luz dos props (Workbench FLAT + contorno + cavidade, AA 8, ortográfica a 60° sobre o chão). Substitui o toldo do #7 e as formas de `Art.ItemSprite` (#9–13, #38); a integração na View é a leva 2.

Regenerar tudo (raiz do projeto, ~8 s; grupos opcionais `itens balcao portao`):
```
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1 --python client/tools/arte_v05_blender.py -- --check <pasta temporária>
python client/tools/folhas_v05.py --check <a mesma pasta>
```
O 1º grava 27 PNG + `meta.json` em `client/Assets/_FS/Resources/Sprites/<nome>/` (formato do `render_sprites.py`, lido pelo `SpriteSheet.cs`) e a prova de emenda do balcão na pasta temporária. O 2º (Python 3 + Pillow, só QA) grava `client/Builds/sprites_contact/v05_itens.png`, `v05_balcao_4e8.png` e `v05_portao.png`, cenas falsas na escala do aparelho (1 m = 95 px) com as texturas e o elenco reais.

| Pasta | Clipes | px | ppu | Pivô | Uso |
|---|---|---|---|---|---|
| `item_minerio`, `item_lingote`, `item_espada`, `item_escudo`, `item_ferramenta`, `item_joia`, `moeda` | `icone` (3/4 na diagonal), `deitado` (de lado na câmera de 60°) | 128×128 | 128 (1 célula = 1 unidade, como `Art.cs`) | centro | `icone`: balão do pedido, cartão do menu, HUD, moedas voando. `deitado`: pilha na cabeça, pilhas das estações, bocas |
| `item_espada_em_pe`, `item_escudo_em_pe`, `item_ferramenta_em_pe` | `Static` | 36×106, 72×70, 60×84 | 160 (escala real) | borda de baixo do item (apoio) | estoque físico no painel do estande, 1 por encaixe |
| `balcao_ponta_esq`, `balcao_ponta_dir` | `Static` | 64×269 | 160 | centro dos 0,2 m da ponta, no chão (`pivot` x 0,75 / 0,25) | pontas do estande (`Balance.CounterEnd` = 0,2 m) |
| `balcao_meio` (4 espadas), `balcao_escudos` (2), `balcao_ferramentas` (3) | `Static` | 136×269 | 160 | centro da vaga (0,85 m), no chão | 1 módulo por vaga; `meta.json` traz `encaixes` [x0, y0, …] em m relativos ao pivô, no plano do sprite |
| `pilar` | `Static` | 142×207 | 160 | centro da base | batente de pedra nas 4 pontas das aberturas da parede direita |
| `portao_fechado`, `portao_aberto` | `Static` | 104×380, 336×591 | 160 | centro do vão na linha da parede (x 9,3), no chão | abertura de cima (y 11,6–13,4): fechado com tranca até o Corredor; aberto = folhas giradas para a rua |
| `porta_servico_fechada`, `porta_servico_aberta` | `Static` | 62×356, 308×567 | 160 | idem | abertura de baixo (y 5,6–7,4), mais baixa, com trinco |

Forma e cor (matiz de `Art.ItemColor` mantido; contorno `#1E1612` de 3 px + sombra de 4 px a 35% nos itens):
- **Espada:** lâmina azul-aço `#7FC4FF` de 0,40 m com fio claro chanfrado e sulco, guarda dourada de 0,155 m (39% da lâmina), punho de couro com tiras, pomo de ouro; ícone a 45°. Lê como espada a 48 px e a 24 px (ampliação na `v05_itens.png`).
- **Escudo:** redondo, face `#F2545B` abaulada, aro de aço com 8 rebites, umbo dourado. **Ferramenta:** martelo de forja na diagonal oposta à espada, pano verde `#4CD964` no cabo e faixa verde no olho. **Joia:** anel de ouro com gema roxa `#B07CF2` grande (cintura 0,17 m). **Minério:** pedra marrom facetada `#9C8468` com veio e 5 cristais `#FF7A1F` de ponta amarela na silhueta. **Lingote:** barra trapezoidal prata com tampo `#EEF1F8`, 2 riscos de brilho e bigorna carimbada. **Moeda:** ouro com aro alto e bigorna em relevo.
- **Estande:** balcão de tábuas com postes e cantoneiras de ferro a cada vaga, faixa de ferro com rebites, tampo claro de 2 tábuas e **painel inclinado de expor** (37,5° da vertical) na metade de trás. O item deitado no painel aparece 1,7× mais alto que em pé a 60°, e o tampo da frente fica livre. Painel escuro (`#5C3A1E`) para o estoque saltar. Sem matiz de item na madeira (regra dos props). Sombra de contato a 35% já assada.
- **Emenda:** os módulos são recortes de um render contínuo, com tábuas por módulo sempre no mesmo padrão, então qualquer módulo encaixa em qualquer posição. Prova: render direto de 4 e de 8 vagas × montagem dos recortes, diferença máx. 9–10/255 e p99,9 4–5/255 (ruído de AA do Workbench, sem estrutura nas juntas).
- **Portões vistos DE LADO** (parede norte–sul, modelo já girado, mesma câmera de 60°). Fechado: folhas no meio da parede, grossas (0,30 m), com tampas de ferro sobre as tábuas e tranca do lado da oficina; de lado só o tampo aparece, e ele preenche o vão entre os pilares. Aberto: folhas giradas 150° na face de fora, rentes ao muro do lado da rua, e o vão fica livre. Não há soleira: o chão da View aparece no vão.

Integração (leva 2, `WorldView`; nada disto foi feito nesta leva):
1. **Itens:** `ItemArt(item, "icone"|"deitado")` com reserva em `Art.ItemSprite`. Cor `Color.white`; tirar o `StackBg` (o contorno já vem no PNG) e o `ItemScale` achatado do lingote (a barra já é barra). Tamanhos sugeridos na tela: balão 0,9 m (~85 px) dentro de um balão de 1,3 × 1,1 m, pilha na cabeça 0,5 m (48 px, passo de 0,2 m), pilhas das estações 0,4 m, bocas 0,45 m a 50% de alfa. A célula de 128 px tem o item em ~110 px, então a escala do `SpriteRenderer` = tamanho desejado × 1,16.
2. **Estande:** para N = `QueueCap` vagas, centro (4,5; 13): `xl = 4,5 − (0,85·N + 0,4)/2`. A ponta esquerda fica em `xl + 0,1`, a vaga i em `xl + 0,625 + 0,85·i` e a ponta direita em `xl + 0,85·N + 0,3`, todas em y 13 com escala 1 (`Deco` já faz: célula = `Size/Ppu`). Ordem de desenho pelo pé (y 13) e, sem elipse, a sombra está no PNG. Itens em pé: em `(x_módulo + enc[2k], 13)` com deslocamento de tela `+enc[2k+1]` m para cima, ordem logo acima do módulo; 1 encaixe por unidade de estoque (Vitrine 10). Distribuição sugerida para 4 vagas: [espadas, escudos (ao comprar Escudos), espadas, ferramentas (ao comprar Ferramentas)]. Encaixe vazio = rack vazio (lê como falta).
3. **Portões:** `pilar` em (9,3; 11,37), (9,3; 13,63), (9,3; 5,37) e (9,3; 7,63), ordenados pelo pé. `portao_*` em (9,3; 12,5) e `porta_servico_*` em (9,3; 6,5), com ordem logo acima de `WallOrder` (abaixo de todo corpo). Troca fechado → aberto quando o Corredor é comprado (`RefreshArea`, onde hoje some o `_door`). Os batentes `Batente` e a `Porta` tingida e o `arco` Tripo em (9,0; 12,6) saem.

## 8. v0.5b — ícones de melhoria e exterior (2026-10-08, 0 crédito)

Pedido do Vinicius: "melhore a qualidade gráfica [...] nosso jogo ainda está bem cru". Fecha o que a leva 2 deixou de fora do pacote do `BENCHMARK_VISUAL.md` §5 (#6, #8–11 e o juice). Mesma pipeline da §7 (`client/tools/ui_v05b_blender.py` importa `arte_v05_blender.py`: primitivas, Workbench FLAT, contorno `#1E1612`).

Regenerar (raiz do projeto, ~11 s; grupos opcionais `icones exterior`):
```
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1 --python client/tools/ui_v05b_blender.py
```

| Pasta | Clipes | px | ppu | Pivô | Uso |
|---|---|---|---|---|---|
| `melhoria_mochila`, `melhoria_botas` (com asinha), `melhoria_fole`, `melhoria_fole_duplo`, `melhoria_martelo` (cabeça de ouro), `melhoria_lupa`, `melhoria_vitrine` (3 espadas em pé), `melhoria_sino` (sino de balcão), `melhoria_cesto` (cesto com minério) | `icone` | 128×128 | 128 | centro | cartões do menu (`MenuBar.Icon`: Fole, Mochila, Fole duplo, Botas, Vitrine, Ajudantes ágeis, Martelo veloz, Lupa, Balcão 5–8 vagas; a Vitrine de joias usa `item_joia`) e martelo do botão Melhorias |
| `arvore` (2,3 m), `arbusto`, `poste` (lanterna amarela, a View põe a poça de luz), `canteiro` (1,2 m, flores creme/amarelas) | `Static` | 268×239, 170×119, 60×225, 206×96 | 160 (escala real) | centro da base | exterior: atrás da parede de baixo, além da rua de cima e à direita da rua lateral (`WorldView.Outside`) |

Sem matiz de item nos props (ART_BIBLE §3): copa verde-oliva `#3F6B3A`/`#365E33` (longe do `#4CD964` da ferramenta). Procedurais novos em `Art.cs` (sem arquivo): grama do exterior (`Art.Ground("grama")`, ruído de valor em 3 verdes-oliva), placa tracejada, coração, brilho de 4 pontas, rosto bravo, seta da dica, cápsula 9-fatias (barra de progresso) e retângulo 9-fatias (todo painel uGUI).

## 9. Lote 4 — 10 clientes (Tripo, 2026-10-08)

Gerados e rigados pelo coordenador (ledger e tasks em `PROVENIENCIA.md` §7, 650 créditos); sprites com os parâmetros dos 6 clientes atuais (6 clipes, 4 direções, célula 128 px, ppu automático), em `client/Assets/_FS/Resources/Sprites/<cliente>/`. Hashes e comando: `PROVENIENCIA.md` §7, "Arquivos do Lote 4". Conferência: `client/Builds/sprites_contact/v05_clientes_lote4.png`.

**Prompt:** o texto exato não foi registrado no ledger (o §7 diz só "assunto + `[S][T]` + `[I]`"). A coluna abaixo descreve o modelo renderizado; o coordenador completa com o assunto que usou.

**CharScale** = fator sobre `CharPpuMul` (0,76), mesma conta dos atuais: `(altura_m / 0,664) × (1,20 / alvo)`. `altura_m` = altura do Idle, direção S, quadro 0 (alfa ≥ 128) ÷ ppu da folha. 0,664 é o ferreiro (1,20 m). A conta reproduz os valores da View: guerreira 1,24, anão 1,08, ajudante 1,19 e cavaleiro 0,79 (±0,01). O alvo inclui chapéu, pluma ou arma quando a silhueta inclui, como o goblin (1,30 com a mochila) e o cavaleiro (1,55 com a pluma) da ART_BIBLE §4. **Proposto, NÃO aplicado na View.**

| Cliente | Assunto (pelo modelo) | Imagem escolhida | Altura medida (px na folha · m projetado) | Alvo (m) | CharScale proposto |
|---|---|---|---|---|---|
| minerador | capacete escuro, bigode, camisa azul, cinto de couro, picareta nas costas | #2 de 4 | 77 px · 0,662 | 1,25 (picareta acima da cabeça) | **0,96** |
| bardo | boina vermelha com pena, gibão amarelo, calça verde, alaúde nas costas | #2 de 4 | 65 px · 0,568 | 1,20 | **0,85** |
| alquimista | cabelo roxo armado, óculos de proteção, avental verde, frasco azul brilhante na mão | #4 de 4 | 87 px · 0,713 | 1,30 (cabelo armado ≈ chapéu) | **0,99** |
| monge | careca, túnica laranja com cordão, faixas brancas nos braços | #4 de 4 (calça visível sob a túnica) | 85 px · 0,633 | 1,20 | **0,95** |
| ladina | capuz e capa curta roxos, couro escuro, adagas | #2 de 4 (capa curta) | 81 px · 0,648 | 1,20 | **0,98** |
| paladina | armadura clara com dourado, coifa branca com tiara, capa azul, mangual | #2 de 4 | 77 px · 0,622 | 1,30 (armadura, como o cavaleiro sem pluma) | **0,86** |
| orc | pele cinza-esverdeada, coque preto, couro com rebites, cutelo grande nas costas | #4 de 4 (pose A) | 71 px · 0,593 | 1,40 (o grandalhão do elenco) | **0,76** |
| barbaro | cabelo e barba laranja, peles, machado nas costas | #4 de 4 (única em pose T) | 78 px · 0,613 | 1,40 (com o machado) | **0,79** |
| pirata | tricórnio preto com pena branca, casaca vermelha com dourado, sabre | #2 de 4 (sem gancho) | 80 px · 0,748 | 1,40 (chapéu + pena, como a pluma do cavaleiro) | **0,97** |
| cacadora | capuz de pele de lobo cinza, couro marrom, arco nas costas | #2 de 4 | 79 px · 0,642 | 1,25 (capuz, como a elfa) | **0,93** |

Para a View (`WorldView.CharScale` e `ClientArt`), dono: worker da View:
```
"minerador" => 0.96f, "bardo" => 0.85f, "alquimista" => 0.99f, "monge" => 0.95f, "ladina" => 0.98f,
"paladina" => 0.86f, "orc" => 0.76f, "barbaro" => 0.79f, "pirata" => 0.97f, "cacadora" => 0.93f,
```
Na fila da folha (parte B, 2× o aparelho), os 10 ficam entre 165 e 194 px, contra 166 do ferreiro, 131 do anão e 215 do cavaleiro. O pirata a 1,25 m ficava de corpo menor que a guerreira (o chapéu e a pena comem a altura), então subiu para 1,40. Orc e bárbaro ficam como os maiores do elenco sem pluma.

### 9.1 Prompts exatos do Lote 4 (imagem no Tripo, Nano Banana 2, 1:1, 4 imagens grátis)
Todos = assunto + `[S]` + `[I]` do cabeçalho deste arquivo. Imagem escolhida entre 4 e task do modelo em `PROVENIENCIA.md` §7.
| Cliente | Assunto | Imagem |
|---|---|---|
| bárbaro | barbarian warrior, very broad muscular shoulders, wild red hair and short braided red beard, brown fur cloak over the shoulders, bare arms with leather bracers, big two-handed battle axe strapped on the back, short leather kilt, fur boots | #4 (única em pose T) |
| pirata | pirate captain, black tricorn hat with a big white feather, black eyepatch, long red coat with gold buttons, white striped shirt, curved cutlass at the hip, short black beard, brown boots | #2 (sem gancho) |
| paladina | paladin woman, shining white and gold plate armor, short blue cape, golden sun emblem on the chest, long blonde braid, war mace at the hip, small open-face helmet | #2 |
| orc | orc mercenary, grey-green skin, two small lower tusks, black topknot hair, spiked brown leather armor, big meat cleaver on the back, wide belt with iron buckle, bulky arms | #4 (pose A) |
| ladina | rogue thief woman, dark purple hood and face scarf covering the mouth, fitted dark leather outfit, two daggers on the belt, many small pouches, short dark cape, slim boots | #2 (capa curta) |
| bardo | cheerful bard, red beret with a long feather, wooden lute on the back, yellow and red patchwork doublet, curly brown mustache, green tights, brown pointed shoes | #2 |
| monge | martial arts monk, bald head, knee-length orange robe tied with a rope belt, wooden prayer beads necklace, cloth wraps on hands and feet, calm smile | #4 (calça visível) |
| caçadora | monster hunter woman, grey wolf-head fur hood, brown leather coat to the hips, crossbow strapped on the back, bandolier with bolts across the chest, tall leather boots, short brown hair | #2 |
| alquimista | alchemist woman, round brass goggles on the forehead, green leather apron with potion bottles in the pockets, rolled-up white sleeves, messy purple hair bun, glowing blue flask hanging on the belt, brown boots | #4 (calça, sem saia) |
| minerador | medieval miner, leather cap with a small brass oil lamp on the front, dirty blue work tunic and brown trousers, pickaxe strapped on the back, big black mustache, soot on the cheeks, sturdy boots | #2 |
