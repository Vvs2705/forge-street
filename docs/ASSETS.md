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
| `coroa` (v0.5c: aro de ouro com 5 pontas, veludo vinho, rubis e safiras) | `icone` | 128×128 | 128 | centro | cabeça do cliente VIP (0,46 m), botão "Chamar VIP" e aviso "Cliente VIP!" |
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

## 10. Leva 9 — ícones dos produtos dos distritos 2–6 (2026-10-13, 0 crédito)

Wave F antecipada (raia ARTE do ORQUESTRADOR): os 34 produtos novos do GDD §7 e 7 intermediários novos, modelados por primitivas no Blender e renderizados com a pipeline do `item_espada` da §7. Matiz por família na ART_BIBLE §3.1 e proveniência com SHA-256 na PROVENIENCIA §8. **Não integrados:** a View ainda só lê os 6 itens do `Item` (`Art.ItemArtName`).

Regenerar (raiz do projeto, com o Unity fechado; ~20 s; IDs opcionais regeram só aqueles itens):
```
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1 --python client/tools/itens_blender.py [-- item_elmo ...]
python client/tools/folha_produtos.py
```
O 1º comando grava, em `client/Assets/_FS/Resources/Sprites/<id>/`, os arquivos `icone.png` e `deitado.png` (128×128, ppu 128, pivô no centro, formato do `item_espada`) e o `meta.json`. Os `.meta` do Unity só são criados quando faltam, e o GUID nunca muda. O 2º grava a folha de QA `client/Builds/validation_art_produtos/folha.png`: cada item a 128 e a 64 px em fundo claro e escuro, silhueta preta a 64 px e o `deitado`; embaixo, todos lado a lado. As referências são os 6 itens atuais, a moeda, a coroa e o sino.

| Tier / distrito | IDs (`item_*`) |
|---|---|
| 1 (Blueprint) | `espada_longa` |
| 2 Armorer's Row | `elmo`, `peitoral`, `grevas`, `manoplas`, `lanca`, `machado_guerra`, `maca` |
| 3 Artisan Quarter | `colar`, `broche`, `cinzel`, `castical`, `caixa_joias`, `calice`, `fivela` |
| 4 Royal Arsenal | `kit_guarda`, `kit_batedor`, `alabarda_cerimonial`, `espada_gala`, `armadura_gala`, `insignia_real` |
| 5 Arcane Foundry | `lamina_runica`, `escudo_runico`, `amuleto_arcano`, `cajado_liga`, `anel_cristal`, `lanterna_arcana`, `liga_celeste` |
| 5+ Grand Forge | `panoplia_campeao`, `lamina_estelar`, `coroa_reino`, `estatua_bronze`, `sino_catedral`, `relicario_real` |
| intermediários | `couro`, `placa`, `rebite`, `gema`, `liga`, `minerio_runico`, `cristal` |

Integração (raias do núcleo e da View; nada disto foi feito nesta leva):
1. O `ItemDef.iconId` (ECONOMIA_WAVE_B §8.1) é o nome da pasta. `Art.Icon(iconId, "icone"|"deitado")` já carrega qualquer pasta, e o tamanho segue a regra do `PaintItem` (escala = tamanho × 1,1).
2. Não há versão `_em_pe` (estoque no painel do estande). O estande só existe no Old Forge e só usa espada, escudo e ferramenta. Quando o estande dos distritos 2+ existir, a versão `_em_pe` sai do mesmo construtor pelo `arte_v05_blender.em_pe`.

## 11. Lote 5A — 7 workers + 4 clientes (Tripo, 2026-10-13)

Gerados, rigados e exportados pelo worker da raia ARTE TRIPO (Leva 9), com o aval do Vinicius de 2026-10-13. Gasto: **715 créditos** (11 × 45 + 11 × 20; saldo 17 285 → 16 570). Ledger, tasks, imagem escolhida e SHA-256: `PROVENIENCIA.md` §9. Receita igual à dos Lotes 1–4 (imagem grátis no site, HD H3.1, 8 000 triângulos, 2K, Privado, Auto Rig Mixamo, FBX Mixamo 2K).

Sprites em `client/Assets/_FS/Resources/Sprites/<id>/`, com `render_sprites.py` sem mudança (4 direções S/W/N/E, 12 fps, célula 128 px, ppu automático):
- **workers** com os clipes do ajudante (`client/Builds/anims_sets/ajudante/`: Idle, LookingAround, Walking). O ajudante não tem clipe de carregar: a View empilha os itens na cabeça por cima do Walking (a "pose própria braços erguidos" do §5 nunca foi feita), e os workers novos seguem o mesmo contrato.
- **clientes** com os do Lote 4 (`anims_sets/guerreira/`: Angry, Idle, LookingAround, Thankful, Walking, Waving).

Conferência: `client/Builds/sprites_contact/v09_lote5.png` (A: escala do modelo; B: fila na escala do jogo com o CharScale abaixo, ao lado do ferreiro, do ajudante, da guerreira e do minerador; C: Idle S a 64 px em cor e em silhueta preta) e `<id>.png` (1º quadro de cada clipe, direção S).

**CharScale** pela conta do §9: `(altura_m / 0,664) × (1,20 / alvo)`, com `altura_m` = altura do Idle S quadro 0 (alfa ≥ 128) ÷ ppu. Alvos pela ART_BIBLE §4 (ferreiro 1,20, ajudante 1,05): workers entre o ajudante e o ferreiro, o gerente na altura do ferreiro; o alvo inclui chapéu, ferramenta ou arma quando eles fazem o ponto alto da silhueta. **Aplicado na View pelo B-CORE-04** (`WorldView.CharScale`, os 11): oficial (Bruna, Tomé, Iara, Joaquim), balconista (Celeste), almoxarife (Baltazar) e mestre_obras (Otávio) aparecem no posto quando contratados e no retrato do cartão; fundidor, mensageiro, gerente e os 4 clientes esperam os distritos.

| Id | Papel | Assunto (resumo) | Imagem | Altura medida (px na folha · m) | Alvo (m) | CharScale proposto |
|---|---|---|---|---|---|---|
| oficial | worker Oficial (Smith) | faixa verde, avental de couro, luvas grossas, tenaz comprida nas costas | #2 de 4 | 79 px · 0,651 | 1,15 (entre ajudante e ferreiro; tenaz na altura da cabeça) | **1,02** |
| balconista | worker Balconista (Clerk) | blusa creme, colete verde, bolsa de moedas, pena atrás da orelha, livro-caixa na mão, calça e botas | #2 de 4 | 81 px · 0,715 | 1,08 | **1,20** |
| almoxarife | worker Almoxarife (Quartermaster) | atarracado, careca, barba grisalha, argola de chaves no cinto, colete cinza, caixote nas costas | #4 de 4 | 80 px · 0,653 | 1,00 (baixo e largo; o caixote é o ponto alto) | **1,18** |
| mestre_obras | worker Mestre de obras (Foreman) | gorro de couro, corda no ombro, malho grande nas costas, túnica laranja, cinto de ferramentas, bigode | #3 de 4 | 84 px · 0,740 | 1,15 (malho acima do ombro) | **1,16** |
| fundidor | worker Fundidor (Smelter, Wave C) | casaco cinza grosso, viseira erguida, manoplas, tenaz nas costas, fuligem no rosto | #2 de 4 | 81 px · 0,650 | 1,15 (capacete com viseira) | **1,02** |
| mensageiro | worker Mensageiro (Courier, Wave C) | boné azul com pena, sacola atravessada, capinha azul, pergaminho na mão | #2 de 4 | 78 px · 0,623 | 1,10 | **1,02** |
| gerente | worker Gerente | casaca bordô com botões de latão, óculos, corrente de relógio, cabelo grisalho, livro de contas na mão | #4 de 4 | 83 px · 0,662 | 1,20 (= ferreiro: o chefe) | **1,00** |
| campones | cliente Camponês (distrito 2+) | chapéu de palha largo, túnica marrom remendada, cinto de corda, forcado nas costas, rosto redondo | #3 de 4 | 59 px · 0,661 | 1,20 (de cima a aba larga já é a cabeça, como no mago) | **1,00** |
| guarda_real | cliente Guarda real | elmo de abas, tabardo azul e ouro sobre malha, lança nas costas, escudo redondo no antebraço | #1 de 4 | 67 px · 0,634 | 1,30 (armadura, como a paladina) | **0,88** |
| emissario | cliente Emissário real | boina azul com pena longa, gibão azul e ouro, capa curta com brasão bigorna + chama, pergaminho com selo vermelho | #2 de 4 | 67 px · 0,684 | 1,25 (boina + pena, um pouco acima do bardo) | **0,99** |
| colecionador | cliente Colecionador (Renome 2) | cartola com pena, casaco roxo com dourado, lupa na corrente, anéis, bigode enrolado | #3 de 4 | 82 px · 0,664 | 1,40 (cartola, como o chapéu do pirata) | **0,86** |

Para a View (`WorldView.CharScale`; worker da View decide onde cada id entra):
```
// Lote 5A (raia ARTE TRIPO 2026-10-13, ASSETS s11: (altura_m / 0,664) x (1,20 / alvo))
"oficial" => 1.02f, "balconista" => 1.20f, "almoxarife" => 1.18f, "mestre_obras" => 1.16f, "fundidor" => 1.02f,
"mensageiro" => 1.02f, "gerente" => 1.00f,
"campones" => 1.00f, "guarda_real" => 0.88f, "emissario" => 0.99f, "colecionador" => 0.86f,
```
Gerente e camponês dão 1,00, igual ao padrão `_ => 1f`; ficam listados para a tabela não depender do padrão.

Na fila da folha (parte B, 2× o aparelho), os 11 ficam entre 137 px (almoxarife, o baixinho largo) e 192 px (colecionador com a cartola), contra 166 do ferreiro, 146 do ajudante, 165 da guerreira e 172 do minerador. Na 1ª passada da folha o camponês (1,25), o guarda (1,45), o emissário (1,30) e o colecionador (1,40) saíam com o corpo maior que o do minerador, porque de cima a aba do chapéu, o elmo e a boina pesam mais que a altura; desceram para 1,20, 1,30 e 1,25 e o colecionador ficou em 1,40.

Conferido na folha: nenhum quadro em pose T (a maior largura de quadro em S/N fica entre 0,91× e 1,62× a do Idle S; a guerreira atual chega a 1,71×), pés no pivô e cada um lê a 64 px pela silhueta (caixote, malho, viseira, chapéu de palha, elmo + lança, boina + pena, cartola). Pontos para o aparelho:
- a pena branca do emissário, de cima, lembra uma lâmina clara na diagonal (a espada do pedido é azul, então não confunde o pedido, mas vale olhar);
- o boné azul do mensageiro fica perto do boné azul-acinzentado do ajudante a 64 px; a pena, a capinha e a sacola separam;
- mestre de obras, camponês e monge ficam todos em laranja/palha; a forma (malho, aba larga, careca) separa;
- os workers novos têm só Idle, LookingAround e Walking, como o ajudante. Se a View quiser Cheering ou carregar com braços erguidos, é re-render de 0 crédito com o mesmo comando e outra pasta em `--anims`.

### 11.1 Prompts exatos do Lote 5A (imagem no Tripo, Nano Banana 2, 1:1, 4 imagens grátis)
Todos = assunto + ", " + `[S]` + ", " + `[I]` do cabeçalho deste arquivo, digitados nessa ordem. Desvios do pedido, para não travar o rig na pose T: o livro-caixa da balconista foi "debaixo do braço" → "na mão"; o escudo do guarda vai "no antebraço esquerdo". Imagem escolhida entre 4 e task em `PROVENIENCIA.md` §9.

| Id | Assunto | Imagem |
|---|---|---|
| oficial | young journeyman blacksmith, sleeveless dark brown leather apron over a light grey shirt, thick padded leather gloves, long iron tongs strapped on the back, dark green headband, short messy brown hair, brown trousers, sturdy boots | #2 |
| balconista | young shop counter clerk woman, light cream blouse with rolled sleeves, fitted green vest, leather coin purse on the belt, quill pen tucked behind her ear, holding a thick ledger book in one hand, brown hair in a low bun, brown trousers tucked into boots | #2 |
| almoxarife | stocky storekeeper, short and wide body, bald head, short grey beard, big iron ring of large keys hanging on the belt, grey wool vest over a beige shirt, small wooden crate strapped on the back, brown trousers, sturdy boots | #4 |
| mestre_obras | construction foreman, brown leather cap, coiled measuring rope over one shoulder, big wooden mallet strapped on the back, orange tunic, leather tool belt with a hammer and chisels, thick brown mustache, brown trousers, work boots | #3 |
| fundidor | foundry smelter worker, thick heavy grey heat-resistant coat, metal welding visor flipped up on top of the head, very thick padded gauntlets, long iron tongs strapped on the back, soot smudges on the face, dark trousers, heavy boots | #2 |
| mensageiro | young messenger boy, slim build, blue cap with a feather, brown leather satchel with the strap across the chest, short blue shoulder cape, holding a rolled parchment scroll in one hand, beige tunic, brown trousers, light boots | #2 |
| gerente | distinguished workshop manager, knee-length burgundy frock coat with brass buttons open at the front, round spectacles, golden pocket watch chain on the vest, neatly combed grey hair, holding a thick account book in one hand, dark trousers, polished brown shoes | #4 |
| campones | cheerful peasant farmer, wide straw hat, patched brown tunic, rope belt, pitchfork strapped on the back, round smiling face, rolled-up beige trousers, simple brown boots | #3 |
| guarda_real | royal guard, blue and gold tabard over chainmail, round brimmed iron kettle helmet, long spear strapped on the back, small round shield strapped on the left forearm, leather belt, brown trousers, iron-capped boots | #1 |
| emissario | royal envoy, blue velvet doublet with gold trim, blue beret with a long white feather, holding a rolled parchment with a red wax seal in one hand, short blue cape with a gold anvil-and-flame crest, white tights, brown pointed boots, neat short beard | #2 |
| colecionador | wealthy art collector, purple velvet coat to the hips, tall black top hat with a feather, brass magnifying glass hanging on a chain, golden rings on the fingers, curled black mustache, dark trousers, buckled shoes | #3 |

## 12. Mochila do ferreiro presa ao corpo (2026-10-14, Blender — 0 crédito)
Feedback do POCO (Vinicius, 2026-10-13, P1): a mochila da melhoria "parece um item solto mal colado" (ícone `melhoria_mochila` colado nas costas pela `WorldView`). Solução: folha própria `client/Assets/_FS/Resources/Sprites/ferreiro_mochila/` (Idle 32 q, Walking 16 q, Cheering 32 q, 4 direções) = re-render do ferreiro do Lote 1 (`arte/tripo/ferreiro` + os 3 clipes Mixamo de `arte/mixamo`) com o prop `mochila_ferreiro` (`client/tools/props_blender.py`: 1 416 tris, 10 materiais, primitivas nas coordenadas do ferreiro em repouso — corpo de couro, tampa, aba com 2 correias e fivelas de latão, bolso, cobertor creme enrolado mais largo que a cabeça, alças sobre os ombros) parentado ao osso `mixamorig:Spine2` (`render_sprites.py --attach prop.fbx --bone mixamorig:Spine2`): a mochila segue o balanço de cada quadro. Mesmos parâmetros da folha `ferreiro` fixados à mão (`--scale 109.87 --pivot-y 35.58`, célula 128, elev 60, flat, 12 fps, teto 32 q) para a `WorldView` trocar a folha do corpo na compra sem pular (`_packSheet`/`_plainSheet`; sem a folha, cai no ícone solto de antes). Conferência antes × depois: `client/Builds/sprites_contact/mochila_antes_depois.png` (`client/tools/folha_mochila.py`).

## 13. Marca e ícone (C-ART-14, 2026-10-15, 0 crédito)

Ticket C-ART-14 (`PRODUCAO_V1_MAPA_TICKETS.md`): marca, logo e 4 variantes de ícone; o ícone lê a 48 px e entra no build. As variantes seguem o DM §77 (personagem + martelo, bigorna + fogo, personagem + ouro, close da obra-prima), e o logo segue o DM §179–180 (metal e madeira, bigorna e martelo, simples, legível pequeno, sem letra medieval). Tudo sai de render dos modelos do jogo (Blender) e de um quadro gravado do jogo. Não houve crédito de IA nem download. Proveniência com SHA-256 na PROVENIENCIA §11.

Regenerar (raiz do projeto, Unity fechado; o 1º comando leva de 5 a 11 min na 1ª vez, que é o import do FBX do ferreiro, e 7 s depois, com o cache `client/Builds/marca/cache_ferreiro.blend`):
```
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-exit-code 1 --python client/tools/marca_blender.py
python client/tools/marca_compor.py
```
O 1º grava as camadas RGBA em `client/Builds/marca/camadas/` (2048 px, EEVEE com o toon de 3 faixas e o contorno do preset `forja` do `render_sprites.py`, luz-chave quente de baixo-esquerda). O 2º compõe tudo com Pillow + numpy: fundo, halo, contorno `#1E1612`, faíscas com semente fixa, texto e folhas. A escolhida está na constante `ESCOLHIDA` do `marca_compor.py`.

| Variante (`arte/marca/`) | O que tem | Fundo |
|---|---|---|
| `icone_a_ferreiro_martelo.png` (**ESCOLHIDA**) | o ferreiro do Lote 1, posado por script, do peito para cima, com um martelo de forja exagerado erguido e faíscas na cabeça do martelo | laranja da forja, claro no meio e com vinheta vinho na borda |
| `icone_b_bigorna_fogo.png` | a bigorna da heráldica do jogo (o perfil `ANVIL` da moeda e do lingote, em 3D), a espada do jogo em brasa no tampo, a chama atrás e faíscas | pedra quente escura |
| `icone_c_ferreiro_ouro.png` | o mesmo ferreiro com uma moeda do jogo erguida (1,6×) e uma pilha de moedas na frente | azul-real |
| `icone_d_obra_prima.png` | o `item_espada` grande na diagonal, com halo e 4 brilhos de 4 pontas (a faísca dourada da obra-prima) | vinho com raios dourados |

**Escolha (A, ferreiro + martelo):**
1. **Leitura a 48 px.** As 2 maiores formas são o martelo em T (cerca de 14 × 8 px) e a cabeça com a bandana (cerca de 11 × 7 px), e uma não se confunde com a outra. A figura continua legível a 36 px e no círculo do adaptativo (folha abaixo).
2. **Contraste.** A figura tem contorno escuro sobre o laranja da forja, com o miolo claro e a borda escura, e por isso destaca no lançador claro e no escuro. A C tem mais contraste de cor (ouro sobre azul), mas comunica "ouro" e não "forja".
3. **Coerência.** É o personagem jogável, com a bandana vermelha (código distintivo da ART_BIBLE §1) e a luz quente da forja (§7): é o primeiro boneco que o jogador vê. B e D funcionam como ícone de RPG genérico e não mostram o personagem.

**Folha de leitura:** `arte/marca/icone_leitura.png`, com cópia local em `client/Builds/marca/icone_leitura.png`. Cada variante aparece a 512, 192, 96, 48 e 36 px em fundo claro (`#F2F2F2`) e escuro (`#121212`), mais o adaptativo recortado em círculo a 96 e 48 px. As 4 leem a 48 px. A B perde a borda no lançador escuro, porque o fundo dela também é escuro.

**No build** (`client/Assets/_FS/Art/Marca/`, PNG com `.meta` versionado e GUID novo, importados sem compressão e sem mipmap):
- `icone.png` (512, RGB) é a variante A. Serve de ícone padrão (o `.exe` do PC) e de ícone legado e redondo do Android.
- `icone_fundo.png` e `icone_frente.png` (432 = 108 dp a xxxhdpi) formam o ícone adaptativo (API 26+, que cobre todo aparelho do minSdk 26). A composição é a mesma com a janela de 72 dp (288 px) no meio. Cabeça e martelo ficam dentro do círculo seguro de 66 dp. O corpo e o fundo continuam até a borda (sangria para o parallax do lançador).
- `Editor/Setup.cs`, em `Icones()`, chamado pelo `Apply()` e portanto por todo build: `PlayerSettings.SetIcons(NamedBuildTarget.Unknown, …)` define o padrão, e `GetSupportedIconKinds/GetPlatformIcons/SetPlatformIcons(NamedBuildTarget.Android, …)` preenche os slots. Slot de 2 camadas recebe (fundo, frente); os demais recebem `icone.png`. O `ProjectSettings.asset` não foi editado à mão: o Unity grava os slots na próxima vez que o `Apply` rodar.

**Logo** (`arte/marca/logo.png`, 1024 × 835 RGBA): "FORGE" em Inter Black, em ouro (`#FFE08A`→`#B8862A`) com contorno `#1E1612` e extrusão bronze. "STREET" vem em aço claro sobre uma tábua de madeira da paleta (§2), com 2 rebites. O símbolo é a bigorna da heráldica com o martelo atravessado atrás e a chama de 3 línguas (vermelho, laranja e amarelo da brasa). A fonte é provisória até o A-UI-15 (Lilita One ou Fredoka, P11): para trocar, basta mudar `INTER` no `marca_compor.py`.

**Feature graphic** (`arte/marca/feature_graphic.png`, 1024 × 500 RGB, sem alfa, como a Play pede): a cena é um quadro real do jogo, o criativo `fs_01_bigorna_rua.mp4` (build v0.6, `CRIATIVOS.md`) aos 22,5 s, com a oficina cheia. O recorte é 1000 × 488 a partir de y 290: rua com clientes, balcão com espadas, bigornas e a bancada de escudos, sem a HUD e sem a legenda. Por cima vão um véu escuro no meio e o logo na horizontal (símbolo à esquerda, letreiro à direita).

**Reprovado:** a bigorna Tripo da estação (`arte/tripo/bigorna`) como figura de ícone. A bigorna é preta, fica sob o pilão de madeira e tem a lanterna; mesmo sem o pilão, o poste e o balde, ela vira uma mancha a 48 px. Por isso a variante B usa a bigorna da heráldica.

**Fora desta leva:** o teste das 4 variantes na loja (G-UA-03, experimento da página da loja na Play Console, só com a página no ar), a fonte OFL definitiva (A-UI-15) e um logo animado ou uma splash dentro do jogo.

## 14. Lote 5B, parte 1 — estações da Rua dos Armeiros (C-ART-13, Tripo, 2026-10-16)

5 estações × 45 = **225 créditos** (saldo 16 570 → 16 345). Ledger e SHA: `PROVENIENCIA.md` §12. Receita dos Lotes 2–3 (imagem grátis Nano Banana 2 1:1, HD H3.1 8 000 tris 2K, Privado, FBX Blender 2K) e render `--dirs 1 --size 256`. A carroça do couro continua com a `carroca` do Lote 3; a Vulcana usa o `fundidor` do Lote 5A.

| id | estações da Row | assunto do prompt (+ `[P]` + `[I]`) | imagem | yaw |
|---|---|---|---|---|
| prensa | prensa, prensa2 | blacksmith plate press station: sturdy dark iron screw press frame on a thick wooden base, big spoked iron handwheel on top, a neat stack of flat steel plates beside it, leather strap details, small red lantern on the top-right corner | #3 | 0 |
| rebitadeira | rebitadeira, rebitadeira2 | blacksmith rivet maker station: small dark iron lever press bolted on a sturdy wooden workbench, long lever handle raised, a wooden bucket overflowing with shiny steel rivets on the bench, coil of iron rod, small red lantern on the top-right corner | #2 | 90 |
| montagem | montagem_elmo/grevas/manoplas/peitoral | armorer assembly workbench: sturdy dark wooden workbench with a steel knight helmet and a breastplate resting on a wooden armor stand at one end, small riveting hammer, coiled brown leather straps and a few rivets on the bench, iron vise, teal blue cloth, small red lantern on the top-right corner | #3 | 270 |
| forja_armas | bigorna_pesada, forja_hastes, forja_machados | heavy weapon forge station: big dark iron anvil on a squat stone block, wooden weapon rack behind it holding a spiked mace, a war axe and a long spear, heavy sledgehammer leaning on the anvil, small water bucket, small red lantern on the top-right corner | #4 | 0 |
| fornalha_liga | fornalha_liga | alloy smelting furnace station: round squat dark stone furnace with iron bands, open top holding a glowing crucible of molten silver-blue metal, short chimney, small leather bellows on the side, iron tongs hanging, two stacked steel ingots beside it, small red lantern on the top-right corner | #2 | 0 |

Pendente (parte 2, com os distritos 3–4 na `main`): estações do Artisan Quarter (crisol, lapidário, bancada de ourives, loja fina, depósitos de cobre/ouro/gemas) e do Royal Arsenal (forja de lâminas, bancadas de kits, mesa do gravador, forjas cerimonial e de alabardas, montagem de gala, pátio de cotas).


## 15. Lote 5B, parte 2 — estações do Artisan Quarter e do Royal Arsenal (C-ART-13, Tripo, 2026-10-16)

15 estações × 45 = **675 créditos** (saldo 16 345 → 15 670). Ledger e SHA: `PROVENIENCIA.md` §13. Mesma receita da §14. As cores seguem a família: Quarter em roxo e ouro (joia, ART_BIBLE §3), Arsenal em azul real e ouro.

| id | uso | assunto do prompt (+ `[P]` + `[I]`) | imagem | yaw |
|---|---|---|---|---|
| crisol | crisol de cobre e de ouro | goldsmith crucible furnace station: small round brick kiln with a glowing orange clay crucible of molten copper on top, short copper chimney pipe, small leather bellows, a neat stack of copper ingots beside it, small red lantern on the top-right corner | #4 | 0 |
| lapidario | lapidário | gem cutter station: sturdy wooden bench with a big foot-pedal grinding wheel, a tray of sparkling purple and blue cut gems, a brass magnifying lens on a swing arm, small red lantern on the top-right corner | #3 | 0 |
| bancada_ourives | bancadas de ourives (configuráveis) | goldsmith workbench station: elegant dark wood jeweler bench with a curved front cut-out and a leather catch apron, tiny hammers and pliers on a small rack, gold rings and a small necklace on a purple velvet tray, small brass oil lamp, small red lantern on the top-right corner | #3 | 0 |
| loja_rica | loja fina (balcão do distrito) | fine jewelry shop stall: wide wooden counter with a glass display case showing gold necklaces and rings on purple velvet, striped purple and gold awning on two carved posts, small brass bell on the counter | #4 | 0 |
| deposito_cobre | depósito de cobre | small wooden mine cart full of chunky orange-brown copper ore rocks with green patina specks, sitting on a short rail segment | #2 | 0 |
| deposito_ouro | depósito de ouro | small wooden mine cart full of grey rocks with big shiny yellow gold ore nuggets, sitting on a short rail segment | #2 | 0 |
| deposito_gemas | gemas brutas | open wooden crate overflowing with rough uncut purple and blue gem crystals, a small pickaxe leaning on the crate | #1 | 0 |
| forja_laminas | forja de lâminas | royal blade forge station: long low stone forge with a glowing orange coal bed, a wooden rack of unfinished steel sword blades, a quenching water trough, a royal blue cloth banner with gold trim, small red lantern on the top-right corner | #1 | 270 |
| bancada_kits | bancadas de kits | scout kit workbench station: sturdy wooden workbench with a leather backpack, a rolled bedroll, a water flask and a short dagger neatly laid out, royal blue cloth, small red lantern on the top-right corner | #3 | 0 |
| bancada_guarda | bancada da guarda | royal guard kit workbench station: sturdy dark wood workbench with a round royal blue and gold shield propped up, a steel kettle helmet and a folded blue tabard with gold trim on the bench, small red lantern on the top-right corner | #2 | 0 |
| mesa_gravador | mesa do gravador | royal engraver desk station: small carved wooden desk with a big gold royal insignia medallion under a brass magnifying lens, fine engraving chisels in a cup, a few small gold bars, blue velvet cloth, small red lantern on the top-right corner | #3 | 0 |
| forja_cerimonial | forja cerimonial | ceremonial sword forge station: polished steel anvil on a carved stone pedestal, an ornate gold-hilted ceremonial sword lying on top, a tall royal blue and gold banner on a pole behind, small red lantern on the top-right corner | #3 | 0 |
| forja_alabardas | forja de alabardas | halberd forge station: dark iron anvil on a stone block with a tall wooden rack holding three ornate halberds with gold trim and small blue pennants, a sledgehammer leaning on the anvil, small red lantern on the top-right corner | #2 | 0 |
| montagem_gala | montagem de gala | gala armor assembly station: wooden armor stand holding a shiny polished plate armor with gold trim and a short royal blue cape, a low workbench beside it with rivets, a small hammer and gold buckles, small red lantern on the top-right corner | #4 | 0 |
| patio_cotas | pátio de cotas | royal quota yard station: low wide wooden loading platform with three painted delivery lanes, stacked wooden crates with royal blue and gold wax seals, a tall royal blue and gold banner on a pole, a small wooden ledger stand | #3 | 0 |

**Ligação no jogo (2026-10-16):** Artisan Quarter com as folhas `deposito_cobre`, `deposito_ouro`, `deposito_gemas`, `crisol` (os 4 crisóis), `lapidario` (os 2) e `bancada_ourives` (as 3 configuráveis; antes apontavam para uma folha `joalheria` que não existe e caíam no desenho procedural). A `loja_rica` fica guardada: o balcão principal do distrito usa o estande modular por vaga. Royal Arsenal liga as dele junto com o distrito.

**Ícones dos insumos novos** (0 crédito, `client/tools/itens_blender.py`, mesma pipeline da Leva 9): `item_minerio_cobre` (veio em pátina verde), `item_minerio_ouro` (pepitas), `item_lingote_cobre`, `item_lingote_ouro`, `item_gema_bruta` (cristais roxos; a lapidada é o `item_gema`) e `item_lamina` (lâmina crua em aço, sem guarda: separa da espada azul). Antes saíam como blocos coloridos procedurais.

**Insumos da Foundry e da Grand Forge (2026-10-23, 0 crédito, `itens_blender.py`):** `item_cristal_bruto` (rocha arcana com veio ciano), `item_lingote_runico` (barra roxo-noite com tampo ciano), `item_bronze` (barra de bronze), `item_pedra_estelar` (rocha azul-noite com estrelas) e `item_aco_estelar` (barra azul-noite com tampo prateado). Antes caíam no bloco de cor sólida (rosa e vermelho nas bocas de entrada da Grand Forge). Os 58 itens dos 6 distritos agora têm folha.

## 16. Elenco mais nítido (programa de arte, etapa 5, 2026-10-16)

Re-render dos 32 personagens com célula de 192 px (era 128) e teto de 21 quadros por clipe, para caber em 4 096 px de largura: 1,5× os pixels por metro, mesmo tamanho no mundo (o `ppu` do `meta.json` é a escala de exibição). Comando e detalhes: `PROVENIENCIA.md` §14. Comparação antes/depois na mesma escala: `arte/auditoria/2026-10-16/nitidez_antes_depois.png`.

**Custo medido (2026-10-16, `Setup.BuildAndroidDev`, ARM64):** APK de desenvolvimento 91,5 MB (`main` 2e6f6a9+) → **121,7 MB** (+30 MB, 61% do limite de 200 MB do módulo base do Google Play); texturas no build 116 → 181 MB descomprimidas (Build Report). Decidido por padrão (revisar): aceitar. Se os distritos 5–6 apertarem o limite, a saída é ASTC 8×8 nas folhas de clientes (−~40% nelas) ou células de 160 px.


## 17. Marca v2 — emblema A3 (ícone) e logo C2 (título) (programa de arte, etapas 2–3, 2026-10-16, 0 crédito)

Substitui o ícone e o logo da §13 (C-ART-14). O Vinicius viu 3 famílias × 4 opções geradas no Tripo (gerador de imagem Nano Banana 2, grátis; folha `arte/tripo/logo_opcoes/opcoes_logo.png` na pasta principal, fora do git) e escolheu **A3 como emblema/ícone** e **C2 como logo de título** ("Aplica a A3 como emblema/ícone e a C2 como logo de título.").

| Família | Prompt (Nano Banana 2) | Escolha |
|---|---|---|
| A — emblema | emblem logo for a blacksmith tycoon game named FORGE STREET: a sturdy shield-shaped badge with a dark iron anvil, a smithing hammer and a bright orange flame in the center, a curved gold ribbon banner across the bottom reading FORGE STREET, warm orange, gold and dark slate colors, stylized polished mobile game art, centered, plain dark background, no other text | **A3** → ícone |
| B — placa | placa de madeira e ferro com o nome (reprovada) | — |
| C — ouro 3D | game title logo for a cozy blacksmith tycoon mobile game, the words FORGE STREET in bold chunky 3D letters made of warm gold and hot orange metal with a thick dark iron outline, a glowing anvil with a crossed smithing hammer emblem behind the text, a few flying sparks, stylized polished mobile game art, centered, plain dark charcoal background, no other text | **C2** → logo |

Regenerar (raiz do projeto, Python 3 + Pillow + numpy + OpenCV, ffmpeg no PATH): `python client/tools/marca_v2.py`. As 2 imagens-fonte ficam versionadas em `arte/marca/v2/`. O recorte é GrabCut (OpenCV) num retângulo + o brilho acima da cor do fundo (glow das letras e faíscas viram alfa), com a sombra projetada da imagem tirada na faixa externa.

- **Ícone (A3):** o escudo com bigorna, martelo, chama e a fita "FORGE STREET", sobre o fundo ardósia do próprio A3 com uma brasa laranja atrás e vinheta. `icone.png` (512, legado/redondo/PC) e `icone_loja_512.png` (a loja) são o mesmo arquivo; o emblema ocupa 84% do quadrado.
- **Adaptativo:** `icone_fundo.png` (ardósia + brasa, sangra até a borda) e `icone_frente.png` (o emblema numa caixa de 236 px em 432): as pontas da fita ficam dentro do círculo seguro de 66 dp (132 px de raio). Mesmo `Setup.Icones()` da §13, sem mudar código.
- **Folha de leitura:** `arte/marca/v2/icone_leitura.png` (512 → 36 px, fundo claro e escuro, adaptativo no círculo). Lê a 48 px: escudo + chama; a fita vira uma faixa dourada (o nome não se lê a 48 px, como em qualquer ícone com texto).
- **Logo (C2):** "FORGE STREET" em letras 3D de ouro e laranja com contorno de ferro, bigorna e martelos cruzados atrás, faíscas; recorte RGBA 1024 × 636.
- **No jogo:** `Resources/UI/logo.png` (fora do `SpriteImport`, que trata `Resources/Textures` como chão repetido; sem compressão, ~2,6 MB de RAM) abre o jogo (`Game.Abertura`): véu escuro sobre o mundo e o logo crescendo de leve, some em 2,4 s ou num toque. Fotos e smoke (`-testsession`, `-autoplay`, `-record`) não a veem; `-abertura` a mostra e segura (foto).
- **Feature graphic:** o mesmo quadro do jogo da §13 sob o véu, com o logo C2 no centro.
- Ledger e SHA: `PROVENIENCIA.md` §15.


## 18. Interface v2 (programa de arte, etapa 4, 2026-10-16, 0 crédito)

Resolve os itens 2, 3, 4, 7 e 8 da `AUDITORIA_VISUAL.md` (painéis chapados, fonte do sistema, botões planos, recompensa só em texto, barra plana) sem asset pesado: quase tudo é gerado por código em `Art.cs` e vale para todo painel que já usa `Art.Panel`, `Art.NewButton` e `Art.NewText`.

- **Fontes** (Google Fonts, SIL OFL 1.1, em `Resources/Fonts` com a licença ao lado): **Nunito Bold** no texto e **Lilita One** em título, número grande (≥ 40 px, `Art.TamTitulo`) e todo botão. A Lilita não tem negrito; o `SemNegrito` tira o Bold sintético que o chamador põe depois. Sem os arquivos, volta a fonte embutida. Cobrem pt/en/es; a seta → cai no fallback do sistema.
- **Moldura** (`Art.Moldura`, 9-fatias 64 px gerado por código): aro escuro de 3 px, filete claro em cima e sombra embaixo, em cinza que a cor do `Image` tinge. É o sprite de todo `Art.Panel`.
- **Botão** (`Art.NewButton`): lábio 8 px abaixo (`Shadow` na cor 55% mais escura), brilho na metade de cima e escala 0,94 ao apertar (`Aperta`, também nos botões montados à mão: ações da HUD, engrenagem, selo do Renome, login, evento).
- **Barra de progresso** (`Art.Barra`): trilho escuro com aro e cheio com relevo e brilho; substitui as 4 cópias (metas, evento/Passe, encomendas, Renome).
- **Fundo dos modais** (`Art.Veu`): véu escuro quente, brasa laranja no meio e vinheta, o mesmo clima da abertura e do ícone; substitui o preto 65% de 9 painéis.
- **Ícones pintados por tipo de prêmio** (`Art.IconePremio`): moeda renderizada para o ouro e 4 ícones do Tripo (Nano Banana 2, grátis) em `Resources/UI/premio_*.png`, no login de 7 dias (ícone + número) e em cada nível do Passe. Sem o arquivo, a forma procedural de sempre. Regenerar: `python client/tools/icones_premio.py` (GrabCut da marca v2; o XP tira o halo escuro do brilho). Ledger: `PROVENIENCIA.md` §16.

| arquivo | assunto do prompt (+ ", stylized polished hand-painted mobile game art, thick dark outline, soft highlights, single centered object filling most of the frame, plain flat dark charcoal background, no text", prefixo "mobile game UI reward icon: ") | escolhida (de 4) |
|---|---|---|
| `premio_renome` | a shiny gold star medal with a green laurel wreath around it and a short red ribbon | 3 |
| `premio_passxp` | a glowing bright blue faceted crystal gem shaped like a star with small white sparkles around it, experience points reward | 2 |
| `premio_cosmetico` | a small decorative shop banner flag on a short wooden pole, purple and gold striped cloth with a tiny golden anvil emblem, cosmetic decoration reward | 4 |
| `premio_blueprint` | a half-unrolled blue blueprint paper scroll with a white technical drawing of a sword on it, tied with a brown leather string, crafting blueprint reward | 3 |

**Fora desta etapa:** cabeçalho em fita nos títulos, selos de estado desenhados (item 10) e a barra de baixo com destaque de botão principal (item 12).

## 19. Estações da Grand Forge (Tripo, 2026-10-23)

10 estações × 45 = **450 créditos** (saldo 15 670 → 15 220). Ledger e SHA: `PROVENIENCIA.md` §17. Receita da §14 (imagem grátis Nano Banana 2 1:1 gerada em 2026-10-16, HD H3.1 Ultra 8 000 tris 2K, Privado, FBX Blender 2K) e render `--dirs 1 --size 256`, todas com yaw 0. Paleta do distrito: bronze, mármore e azul-noite (ART_BIBLE). Os crisóis de bronze continuam com a folha `crisol` e o minério de cobre com a `deposito_cobre`.

| id | uso | assunto do prompt (+ `[P]` + `[I]`) | imagem | yaw |
|---|---|---|---|---|
| fundicao_sinos | fundição de sinos | abell foundry station: round clay casting pit with a glowing orange bronze mold, a big bronze bell hanging from a sturdy wooden beam above it, a long-handled iron ladle, small red lantern on the top-right corner | #1 | 0 |
| cratera_estelar | depósito da cratera estelar | small star crater deposit: a shallow rocky crater rim filled with glowing night-blue meteorite rocks and tiny white star sparkles, a small pickaxe leaning on the rim | #3 | 0 |
| forja_estelar | forja estelar | star-steel forge station: tall dark stone forge with a glowing deep blue and white fire, a night-blue metal hood with small star-shaped bronze rivets, a shining half-forged blade on a small anvil beside it, small red lantern on the top-right corner | #2 | 0 |
| fundicao_estatuas | fundição de estátuas | statue foundry station: a white marble pedestal with a half-finished bronze knight statue on top, a small bronze pouring crucible on a brazier beside it, sculpting chisels on a wooden stand, small red lantern on the top-right corner | #1 | 0 |
| cais | cais (importação do Mensageiro) | small wooden harbor dock station: short wooden pier platform with two mooring posts, stacked cargo crates with bronze bands, a coiled rope and a small night-blue pennant on a pole | #3 | 0 |
| patio_exportacao | pátio de exportação | export yard station: wide stone loading platform with a sturdy wooden cart full of bronze-trimmed crates, a white marble milestone with a bronze compass emblem, a night-blue and bronze banner on a pole | #2 | 0 |
| forja_sintese | forja de síntese | grand synthesis forge station: big ornate bronze anvil on a white marble base, a glowing golden crucible on top, four small bronze braziers around it, night-blue cloth with gold stars, small red lantern on the top-right corner | #1 | 0 |
| bancada_relicarios | bancada de relicários | reliquary workbench station: elegant marble-topped workbench with a small ornate gold and bronze reliquary box shaped like a tiny chapel with a roof, fine tools in a bronze cup, night-blue velvet cloth, small red lantern on the top-right corner | #2 | 0 |
| bancada_coroas | bancada de coroas | crown workbench station: dark wood workbench with a closed royal crown with arches and a small golden orb on top resting on a burgundy velvet cushion, small gems on a tray, a tiny hammer, small red lantern on the top-right corner | #3 | 0 |
| bancada_panoplia | banca da panóplia | champion panoply assembly station: wooden armor stand with a shiny champion plate armor, two crossed swords behind a burgundy shield on a small wall rack beside it, a low bronze-trimmed bench, small red lantern on the top-right corner | #1 | 0 |

**Forja estelar clareada (2026-10-23, 0 crédito):** a render saiu escura (luz flat, textura do Tripo em pedra e azul-noite: luminância média 64, contra 110–150 nas outras estações). `client/tools/clarear_sprite.py` sobe o brilho em HSV (V^0,62, saturação ×1,35, alfa intacto) e soma o fogo azul-claro que a textura não trouxe (brilho radial em screen na boca da forja, `--brilho 130,132,46,9FD8FF,0.85`): luminância 64 → 94.

**Ligação no jogo:** os `arte:` das 10 estações em `Distritos.GrandForge.cs` trocam a arte emprestada (forja de armas, gemas, forja de lâminas, carroça, pátio de cotas, forja cerimonial, mesa do gravador, bancada de kits e montagem de gala) pela própria. Teste novo `DistritosTests.Arte_TodaEstacaoTemAFolha`: toda `arte:` de estação, nos 6 distritos, tem `Resources/Sprites/<arte>/Static.png`.

## 20. Estações da Arcane Foundry e a Vulcana (Tripo, 2026-10-23)

11 estações × 45 + a Vulcana (45 + rig 20) = **560 créditos** (saldo 15 220 → 14 660). Ledger e SHA: `PROVENIENCIA.md` §18. Imagens grátis no Nano Banana 2 (1:1, 4 por peça) geradas em 2026-10-23; receita da §19 (HD H3.1 Ultra 8 000 tris 2K, Privado, FBX Blender 2K); a Vulcana com Auto Rig humanoide Mixamo e FBX Mixamo 2K, como o Lote 5A (§11). Render `--dirs 1 --size 256`, yaw 0; a Vulcana com os clipes do ajudante (Idle, LookingAround, Walking; 4 direções, célula 128). Paleta do distrito: ciano + roxo-noite com prata (ART_BIBLE, família Arcano). A fornalha comum e a fornalha de liga continuam com as folhas `fornalha` e `fornalha_liga`.

| id | estações | assunto do prompt (+ `[P]` + `[I]`; a Vulcana `[S]` + `[I]`) | imagem | yaw |
|---|---|---|---|---|
| deposito_cristal | depósito de cristal bruto | raw crystal deposit: low pile of rough grey rocks with big clusters of glowing cyan crystals sticking out, a few loose crystal shards on the ground, a small pickaxe leaning on the rocks | #2 | 0 |
| deposito_runico | veio rúnico | runic ore vein deposit: dark purple-grey rock outcrop with glowing cyan rune veins carved into it, chunks of runic ore in a small wooden crate beside it, a small pickaxe | #2 | 0 |
| cuba_cristal | cuba de cristal, cuba 2, cuba 3 | crystal vat station: round night-purple stone vat with silver bands, filled with glowing cyan liquid and floating crystal shards, a silver ladle hooked on the rim, small purple pipes at the base, small red lantern on the top-right corner | #1 | 0 |
| fornalha_runica | forno rúnico, forno rúnico 2 | runic furnace station: squat dark purple stone furnace with glowing cyan rune carvings and silver bands, open mouth with cyan and white fire, short chimney, iron tongs hanging on the side, small red lantern on the top-right corner | #2 | 0 |
| altar_lanternas | altar das lanternas | arcane lantern altar workbench: night-purple stone altar table with silver trim, a glowing cyan crystal lantern on top, a second unfinished lantern frame and small crystals beside it, tiny silver tools, small red lantern on the top-right corner | #3 | 0 |
| altar_aneis | altar dos anéis | arcane ring altar workbench: night-purple stone altar table with silver trim, a big silver ring with a tall glowing cyan crystal on a small velvet stand, a tray of tiny crystals and silver wire, small red lantern on the top-right corner | #4 | 0 |
| altar_amuletos | altar dos amuletos | arcane amulet altar workbench: night-purple stone altar table with silver trim, a glowing cyan diamond-shaped amulet hanging from a small silver stand, open jewelry box with chains, a small rune book, small red lantern on the top-right corner | #3 | 0 |
| altar_laminas | altar das lâminas | runic blade altar workbench: night-purple stone altar table with silver trim, a curved silver saber with glowing cyan runes lying on top, a whetstone and a small hammer, floating cyan rune sparks, small red lantern on the top-right corner | #3 | 0 |
| altar_cajados | altar dos cajados | arcane staff altar workbench: night-purple stone altar table with silver trim, a tall silver staff with a glowing cyan orb standing in a holder beside it, carving tools and a crystal on the table, small red lantern on the top-right corner | #4 | 0 |
| altar_escudos | altar dos escudos | runic shield altar workbench: night-purple stone altar table with silver trim, a hexagonal silver shield with a glowing cyan rune ring resting upright on it, a small hammer and silver rivets, small red lantern on the top-right corner | #1 | 0 |
| cadinho_celeste | cadinho celeste | celestial crucible station: silver crucible on a three-legged stand over glowing cyan flames, a floating silver gyroscope ring orbiting above it, a hexagonal glowing cyan alloy bar on a small night-purple stone block beside it, small red lantern on the top-right corner | #2 | 0 |
| vulcana | worker Vulcana (Fundidora da Row) | strong young woman foundry smelter, thick heavy dark red heat-resistant leather coat, metal welding visor flipped up on top of the head, red hair in a thick braid, very thick padded gauntlets, long iron tongs strapped on the back, soot smudges on the cheeks, dark trousers, heavy boots | #4 | 0 |

**Ligação no jogo:** os `arte:` da Foundry em `Distritos.ArcaneFoundry.cs` trocam a arte emprestada (crisol do Quarter nas cubas e nos fornos rúnicos, mesa do gravador do Arsenal nos 7 altares e no cadinho, gemas e ouro nos depósitos) pela própria; o `DistritosTests.Arte_TodaEstacaoTemAFolha` (§19) cobre as folhas. A Vulcana (`wk_vulcana`, `Distritos.cs`) troca a folha do Fundidor pela `vulcana`, com `WorldView.CharScale` **1,03** pela conta do §11: Idle S quadro 0 com 78 px a ppu 118,9 = 0,656 m, alvo 1,15 (viseira erguida, como o fundidor). O `fundidor` fica no repositório para o Smelter dos distritos seguintes.

## 21. Herói em 3 estágios pelo Renome (B-ART-05, 2026-10-23, 0 crédito)

Decisão do ticket (crédito ou adereço no Blender): **adereço no Blender**, 0 crédito. O herói continua o mesmo modelo (`arte/tripo/ferreiro`) e os mesmos clipes (`client/Builds/anims_sets/ferreiro`: Idle, Walking, Cheering); o que muda é o que vai na cabeça, preso ao osso `mixamorig:Head` pelo `render_sprites.py --attach` (que agora aceita vários `--attach`/`--bone`, um por adereço):

| estágio | Renome do Império | adereço (`client/tools/props_blender.py`) | folhas |
|---|---|---|---|
| 1 | 0–2 | — (a bandana vermelha de sempre) | `ferreiro`, `ferreiro_mochila` |
| 2 | 3 (Mestre artesão) | `chapeu_couro`: chapéu de couro de aba larga (0,47 m), fita e fivela de latão | `ferreiro_e2`, `ferreiro_e2_mochila` |
| 3 | 4+ (Fornecedor do reino) | `barrete_mestre`: barrete alto de veludo azul com faixa e broche de ouro e pluma vermelha | `ferreiro_e3`, `ferreiro_e3_mochila` |

Render com os parâmetros do `ferreiro` (`--size 192 --max-frames 21 --scale 164.82 --pivot-y 53.4`, 4 direções), então o tamanho e o pivô são iguais e a troca de folha não pula; nas `_mochila` a mochila vai junto (`--attach chapeu --bone mixamorig:Head --attach mochila_ferreiro --bone mixamorig:Spine2`). Silhuetas (preto, alfa ≥ 128, Idle quadro 0 nas 4 direções): o disco da aba larga no estágio 2 e o barrete alto com a pluma no 3 separam dos três de longe; a folha de conferência fica em `arte/auditoria/2026-10-23/heroi_estagios.jpg`.

**Ligação no jogo:** `RenomeDefs.EstagioHeroi(nivel)` (0/1/2 pelo Renome do Império) e a `WorldView` troca a folha do corpo pelo estágio e pela mochila (`FolhasHeroi`; folha que faltar repete a do estágio de baixo), com a poeira da troca. Teste: `RenomeTests.Heroi_TresEstagiosPeloRenome_ComAsFolhas`. O cinto do estágio 4 (Renome 5) e o uniforme recolorido ficam para depois (GDD §151: o resto entre gameplay e cosmético).
