# Forge Street: benchmark visual e game feel (v0.4.1 → pacote v0.5)

Raia "benchmark visual e game feel", 2026-10-08. Status: **PROPOSTO** (nada implementado). Pedido do Vinicius: "a espada é apenas um triângulo azul nos pedidos e quando prontas, elas precisam parecer realmente espadas [...] nosso jogo ainda está bem cru, o portão lateral mesmo está errado" e "faça uma análise agora de jogos similares e veja o que estamos perdendo".

**Método e limite.** Olhei as capturas de loja (App Store US, 2026-10-08) de 8 jogos e as nossas fotos no aparelho (`client/Builds/validation_device_v041/01_abertura_save_030.png`, `02_menu_aberto.png`, POCO F4 1080×2400), `validation_menu/shots/62_rua_lateral.png`, `63_luxo.png`, `validation_portas/shots/41_porta_aberta.png` e `creatives/fs_01_contato.png`. Capturas de loja são material de marketing (às vezes exageram pilhas e efeitos). O que só se vê com o jogo rodando (tween, som, vibração) está marcado **não confirmado**. Não instalei nenhum jogo.

**Escala de referência (nossa).** A câmera mostra 11,4 m de largura (`Game.VisibleWidth` = oficina 9 m + 2 × 1,2 m) em 1080 px. Então **1 m ≈ 95 px** no aparelho, e todos os tamanhos abaixo usam essa conta.

---

## 1. Jogos analisados

| Jogo | Estúdio | Números na loja (US, 2026-10-08) | Por que serve de referência |
|---|---|---|---|
| [Burger Please!](https://apps.apple.com/us/app/id1668713081) | Supercent | 136 mil avaliações, nota 4,7 | Arcade idle de carregar pilha, o mesmo laço que o nosso. Balão de pedido grande, pilha "MAX", cartões de upgrade |
| [Pizza Ready!](https://apps.apple.com/us/app/id6450917563) | Supercent | 344 mil avaliações, nota 4,6 | Mesmo laço. Portal de entrada, placas de compra no chão, dinheiro físico, paredes em corte |
| [My Perfect Hotel](https://apps.apple.com/us/app/id1635760774) | SayGames | 706 mil avaliações, nota 4,7 | Câmera quase de cima em retrato, a mais parecida com a nossa. HUD de nível e moedas, portal temático |
| [My Mini Mart](https://apps.apple.com/us/app/id1592004814) | Supersonic Studios | 126 mil avaliações, nota 4,6 | **Prateleira-vitrine com placa do item e estoque físico.** É o modelo direto do estande de espadas |
| [My Craft Mart: Idle Mini Shop](https://apps.apple.com/us/app/id6447640481) | Many People, Inc. | 5 avaliações | Arcade idle de **ferreiro** (minério, armas, carregadores, caixas). Mesa de expor, espada em pedestal, pedido "0/1" |
| [Forge & Fortune](https://apps.apple.com/us/app/id6756655173) | Supercent | 1 avaliação (nota 1,0; parece lançamento de teste) | **Concorrente direto.** Arcade idle de forja da Supercent: racks de armas com "5/5", portão de arco tapado com tábuas, minério com contorno |
| [Eatventure](https://apps.apple.com/us/app/id1600871388) | Lessmore | ~73 mil avaliações, nota 4,8 (segundo a busca; não li no detalhe) | Idle de toque, não de carregar, mas com câmera quase de cima e o melhor "juice" de moedas e balões do grupo. Evolução barraca → lanchonete |
| [Idle Forge Craft](https://apps.apple.com/us/app/id1491457841) | 文辉 江 | 3,4 mil avaliações, nota 4,8 | Forja em close. Só serve para a silhueta da espada (lâmina + guarda em cruz + punho), que lê até em lava |

Fora do benchmark: Idle Lumber Empire não foi checado (é tycoon de toque, não arcade de carregar). Contexto de mercado da categoria: [PocketGamer.biz, desconstrução de My Mini Mart](https://www.pocketgamer.biz/feature/81292/under-the-hood-deconstructing-the-top-hypercasual-games/) e [Supercent sobre Burger Please](https://supercent.notion.site/2023-08-28-Supercent-s-Burger-Please-ranked-2nd-in-downloads-among-all-apps-released-in-2023-7d4a6ce16889400dbdaf74e37c6eb7bf).

## 2. O que cada um faz (visto nas capturas)

| Eixo | Burger Please / Pizza Ready (Supercent) | My Perfect Hotel | My Mini Mart / My Craft Mart | Forge & Fortune | Eatventure |
|---|---|---|---|---|---|
| Arte e câmera | 3D low-poly chapado, isométrica ~45°, saturação alta, sem contorno, sombra suave de bolha | 3D estilizado, câmera alta (~60–70°, a mais parecida com a nossa), uma paleta temática por hotel | 3D low-poly, ~45–50°, cores pastel vivas, chão de areia ou ladrilho claro | 3D semirrealista, ~50°, madeira + pedra cinza, iluminado (nada fica escuro) | 3D simples, quase de cima (~75°), cores chapadas |
| Itens | O produto **é** o ícone: hambúrguer e pizza em 3D, grandes, em colunas de 8 a 15 na mão e nas grelhas de saída; "MAX" vermelho em itálico | Pilhas de dinheiro azul | Tomate, cenoura e lingote expostos fisicamente na prateleira; placa com o ícone no topo | Espadas em **rack vertical** (lâmina para cima) e machados em rack próprio; minério cobre com contorno preto; lingotes de ouro empilhados em mesas | Ícone do item dentro do balão |
| Pedido do cliente | **Um** balão grande no balcão (branco, cantos redondos, ícone + número em negrito, ~1/5 da largura da tela). A fila não tem balão | — | Bolha pequena "ícone 0/2" sobre cada cliente | Contador "5/5" em pílula laranja sobre o rack | Balões redondos com ícone e número |
| HUD | Estrela de nível + barra de XP + pílulas de dinheiro e gema; engrenagem | Estrela + nome do hotel + barra "MAX" + 4 moedas com "+" | Pílula de moeda com moedas em 3D; botões quadrados coloridos de borda grossa nas laterais | Retrato do herói com nível e barra verde; pílula de moeda-martelo | Pílula branca de moeda, engrenagem |
| Cartões e compras | Cartões creme com render 3D da máquina, números "+67%", botão verde "Select" com chanfro, fita "HOT"; placas escuras no chão com pictograma e preço | Pílula "CLEANER 300" com retângulo tracejado no chão | Placa no chão com ícone e preço | Cartões laterais com arte ("PLAYER UPGRADE", "WEAPON UPGRADE") e seta laranja; placa "Lift Upgrade 800" | Cartões brancos de borda grossa com ícone e preço em moeda |
| Feedback | Rastro de notas voando no pagamento (visto parado em Pizza Ready), emoji de coração, "MAX". Tween, tremida, som e vibração: **não confirmados** | Confete | Brilho de estrelas em baú e item novo | Número de dano "10", faíscas, emoji de satisfação, torres de moedas numa zona de coleta | Moedas douradas em arco saindo do balcão, "Perfect" amarelo com contorno |
| Ambiente | Paredes em corte com tampo claro e friso colorido; rua com carros, grama | Muros grossos com tampo; mapa-múndi | Muro baixo em corte com friso azul; grama e plantação fora | Muro de pedra em corte, piso de madeira e pedra, grama, cerca, caminho de pedras | Rua com faixa, grama |
| Evolução | Card de máquina troca o modelo e a cor ("Level 3") | Decorar quartos troca a arte | Nova prateleira por produto; novo mercado ~30–40 min (PocketGamer) | Nível de jogador e de arma; sala nova atrás do portão tapado | Barraca de limonada (com toldo) → food truck → café → diner |
| Personagens | Manequins cinza sem rosto; o jogador se destaca pelo boné vermelho | Bonecos com rosto | Bonecos-feijão coloridos | Humanos semirrealistas | Bonecos simples |

**Onde já ganhamos.** O elenco Tripo + Mixamo tem mais personalidade (Thankful, Waving, Angry) do que os manequins da Supercent, e a bandana vermelha do ferreiro faz o mesmo papel do boné. Os props também estão bons (barril, tocha, prateleira). O problema não é o elenco. É **escala, ícone, HUD e cenário**.

---

## 3. O que estamos perdendo: visual

Prioridade: P0 = o Vinicius já notou ou é o 1º quadro do criativo; P1 = falta de acabamento que todo similar tem; P2 = polimento.
Esforço: P (≤ 0,5 dia), M (1–2 dias), G (3+ dias). "Blender" = arte nova; "código" = só view/UI.

### P0

**P0-1. Ícones dos itens (espada que parece espada)** · M · Blender + código
- **Hoje.** `Art.ItemSprite` desenha máscaras de 64 px numa cor só: espada = `Triangle()`, escudo = `Diamond()` (a ART_BIBLE §3 pede disco), ferramenta = `Plus()`, lingote = `Rounded()` achatado. No aparelho (foto 02) o pedido é um triângulo azul de **~28 px** (0,3 m) num quadrado branco de 57×47 px. A pilha na cabeça é um triângulo de ~32 px sobre disco escuro. O estoque do balcão tem 21 px (0,22 m) e as pilhas das estações 23 px. Isso não lê como espada nem como coisa nenhuma.
- **Similares.** Em Burger Please e Pizza Ready o produto em 3D é o próprio ícone, a 80–100 px. Forge & Fortune e My Craft Mart desenham a espada com lâmina, guarda e punho. Até em Idle Forge Craft a cruz lâmina + guarda lê sozinha.
- **Correção.** Pré-renderizar os 6 itens no Blender (mesma pipeline de props, `--light flat` com contorno) em **dois cortes**:
  - **"ícone"**: vista 3/4, item na diagonal, para balão, HUD e cartão;
  - **"deitado"**: item de lado na câmera de 60°, para pilha, estação e estande.
- Fonte 256×256 px, contorno escuro `#1E1612` de 3 px a 128 px, sombra de 4 px a 35% deslocada para baixo, 2 tons + 1 brilho (ART_BIBLE §1.2).
- Tamanho na tela: balão **80–96 px**, pilha 48–56 px, estoque e estande 40–48 px, cartão 96–120 px, HUD 48 px. A cor por item continua (matiz da ART_BIBLE §3); a forma passa a ser o objeto real. Aceite: a 24 px em preto, 5 de 5 identificáveis. Especificação por item na §4.1.

**P0-2. Balão de pedido grande, só para quem está no balcão** · P · código (usa os ícones do P0-1)
- **Hoje.** 4 a 8 balões brancos pequenos lado a lado (foto 02, topo; foto 63: 6 balões). Cada um com ícone de 28 px e barra de paciência de 5 px.
- **Similares.** Burger Please e Pizza Ready mostram **um** balão grande na frente do caixa, com ícone e número; a fila não tem balão. My Mini Mart usa bolhas pequenas com "0/2". Eatventure usa balão redondo com contador.
- **Correção.**
  - Cliente da frente: balão de fala branco **1,3 × 1,1 m (~125 × 105 px)** com rabicho, borda escura de 3 px e ícone de 80–96 px. Se a quantidade for maior que 1, escrever "×2" em negrito de 40 px.
  - Paciência vira um anel em volta do balão (verde → amarelo → vermelho) em vez de barra solta. Abaixo de 20%, o balão treme ±3° a 6 Hz e aparece um emoji bravo de 48 px.
  - Demais clientes da fila: mini-ícone de 40 px sem balão, ou nada. Vale testar as duas no aparelho.
  - Ao vender, o balão estoura (escala 1 → 1,25 → 0 em 0,15 s) e sobe um coração de 48 px por 0,6 s.

**P0-3. Balcão sem toldo → estande de espadas que cresce de 4 para 8 clientes** · G · Blender + código + core (número da fila)
- **Hoje.** O sprite `balcao` é uma barraca de feira com toldo listrado ouro/creme (foto 02: ~0,9 m de largura, ~85 px) e é a menor coisa da linha de estações. Uma fila de 4 a 6 clientes se espreme sobre ela. O estoque é uma grade de ícones de 21 px na frente do sprite. A ART_BIBLE §6 e o ASSETS #7 ainda pedem o toldo, então os dois precisam de revisão.
- **Similares.**
  - Forge & Fortune: racks de armas com espadas em pé e pílula "5/5".
  - My Mini Mart: prateleira com placa do produto e os itens arrumados fisicamente; o estoque é visível.
  - My Craft Mart: mesa de expor com tapete colorido.
  - Burger Please e Pizza Ready: balcão com caixa registradora e o dinheiro empilhado ao lado.
  - Eatventure: o toldo é a barraca do **início**, e o jogo evolui para lanchonete. Toldo lê como "nível 1 de feira", não como loja de ferreiro.
- **Correção.** Especificação na §4.2. Em resumo: balcão de madeira reto + racks modulares onde as espadas prontas aparecem **em pé, uma por encaixe** (estoque físico), contador "n/8" e vagas da fila pintadas no chão. São 4 vagas no início e 8 com a Vitrine. Hoje `Balance.QueueCap1 = 6`, então chegar a 8 é decisão de balanço para a raia core; a arte já nasce com 8 encaixes.

**P0-4. Portão lateral: hoje está errado** · M · Blender + código (core só se unificar as aberturas)
- **O que está errado** (fotos 01, 02, 62 e 41; `WorldView.cs` `ArchPos (9,0; 12,6)`, `Balance.SideWallOpenings {5,6; 7,4; 11,6; 13,4}`):
  1. **Orientação.** O `arco` foi renderizado de frente, com a viga deitada em X (leste–oeste), para uma passagem que se atravessa no sentido norte–sul. Só que ele está numa parede que corre de norte a sul, onde se passa de oeste para leste. O resultado é um poste dentro da oficina, outro na rua e a viga cruzando a linha da parede. Lê como uma forca de madeira largada no canto, não como portão (foto 02, canto superior direito, sobre "Corredor 690").
  2. **A porta de baixo é um retângulo chapado** de textura de madeira tingida (`_door`, `Tiled("Porta")`), sem moldura, dobradiça, espessura ou volume. Quando o Corredor é comprado ela **some** (`_door.enabled = !corridor`) e fica um buraco com dois batentes finos (foto 41).
  3. **Duas aberturas para a mesma compra** (porta e arco). O pad "Corredor 690" fica perto do arco, não na porta. O jogador não vê "portão trancado + preço" num lugar só.
  4. **O teaser da rua é texto.** "Rua lateral" está escrito em cima da parede, e o outro lado é um calçamento escurecido uniforme, sem nada para desejar.
  5. **A parede é uma faixa plana** em planta, sem altura, então as aberturas não leem como vão de porta. Só a parede de baixo tem face.
- **Similares.**
  - Forge & Fortune: arco de pedra **tapado com tábuas pregadas** e placa "Next Room"; a parede tem altura e o vão é óbvio.
  - Pizza Ready: o portal de entrada é um 3D grande, orientado para o caminho, com pilares e placa.
  - My Perfect Hotel: portal temático de frente para a câmera, na parede de frente.
  - PocketGamer (My Mini Mart): a câmera desliza até o novo ponto comprável para chamar atenção.
- **Correção.** Especificação na §4.3: portão único renderizado **de lado** (modelo girado 90° em Z, mesma câmera de 60°), fechado com folhas de tábua, barra e placa de preço; pad colado na frente; ao comprar, as folhas abrem com pó e a câmera desliza.

**P0-5. Venda sem dinheiro visível** · P–M · código
- **Hoje.** Venda = "+25" em texto Arial subindo 1,1 s (`WorldView.Float`) e um bipe. O HUD não reage. Não existe moeda na cena.
- **Similares.** Em todos o dinheiro é **objeto**: notas verdes empilhadas no balcão (Burger Please, Pizza Ready), torres de moedas numa zona de coleta (Forge & Fortune), moedas em arco do balcão (Eatventure), rastro de notas voando no pagamento (Pizza Ready).
- **Correção, versão só visual, sem mudar regra.**
  - 5 a 8 moedas (sprite 3D renderizado, 40 px) saem do cliente em arco (altura 0,8 m, 0,45 s, intervalo de 0,03 s) até a pílula de moedas do HUD.
  - A pílula dá um punch (escala 1 → 1,15 → 1 em 0,2 s) e o número rola até o valor novo em 0,3 s.
  - O "+25" passa a 48 px com contorno.
- **Versão de design, decisão do Vinicius.** Moedas se acumulam numa pilha física ao lado do estande e o jogador recolhe ao passar, como em todo o gênero. Isso muda o laço (e o bot), então fica fora deste pacote sem decisão.

### P1

**P1-1. HUD de topo pesado e "de protótipo"** · P · código + fonte
- **Hoje.** Painel azul-marinho de ~290 px (12% da tela) com moeda chapada (disco amarelo), "Forge Street v0.4.1" e frase de dica. A fonte é `LegacyRuntime.ttf` (Arial), fina e sem contorno (foto 01).
- **Similares.** Burger Please, My Perfect Hotel e Forge & Fortune usam pílulas finas com ícone 3D, número em negrito com contorno e barra de nível. Nenhum mostra versão no HUD.
- **Correção.**
  - Barra de 110–130 px. Pílula de moeda: fundo `#2A1E14` a 85%, borda clara de 3 px, moeda 3D de 72 px e número de 56 px.
  - Fonte display arredondada com licença OFL (Lilita One, Fredoka ou Baloo 2; conferir a licença antes de embutir) e `Outline` uGUI de 3–4 px escuro, mais `Shadow`.
  - Versão sai do HUD (vai para configurações ou só no build dev).
  - Dica vira **seta no mundo** sobre o alvo (quica 0,15 m em 0,6 s). Texto só no tutorial.

**P1-2. Cartões do menu escuros e com ícone genérico** · M · código + ícones Blender
- **Hoje** (foto 02). Cartões azul-marinho. Mochila e Botas têm o **mesmo disco amarelo** como ícone, e o Fole duplo um quadrado cinza. O efeito vem em frase ("Carrega 6 em vez de 3"). O preço vem em texto vermelho "145 de ouro", que lê como erro.
- **Similares.** Burger Please: cartão creme com render 3D, números grandes, botão verde chanfrado, fita "HOT". Eatventure: cartão de borda grossa, ícone e preço em moeda. Forge & Fortune: cartão com ilustração e seta laranja.
- **Correção.**
  - Cartão de 300×380 px, fundo creme `#FFF1D6`, borda de 4 px `#5C3A1E`, cantos de 24 px e base 8 px mais escura (chanfro).
  - Ícone renderizado de 120 px por upgrade: mochila, fole, fole duplo, botas, martelo dourado, estande/vitrine, lupa, ajudante.
  - Efeito como **"3 → 6"** com seta verde.
  - Botão de preço verde `#4CD964` com moeda e valor quando dá para comprar; cinza quando não dá; cadeado + "requer X" quando travado; "MAX" quando comprado.
  - Ao tocar: escala 0,94 em 0,06 s, volta com sobra em 0,15 s e moedas voam do HUD para o cartão.
  - Botão "Melhorias": ícone de seta/martelo + **badge vermelho de 44 px** com o número, no lugar do "(1)". Pulsa (1 → 1,06) quando há algo comprável.

**P1-3. Bocas "▶" parecem botão de vídeo, e rótulos de texto em toda estação** · P · código (+ 1 sprite)
- **Hoje.** Entrada e saída são anéis com triângulo "play" (`Mouth`). "Bigorna", "Bigorna 2", "Fornalha", "Depósito" e "Balcão" ficam escritos o tempo todo (fotos 01 e 02).
- **Similares.** Burger Please usa grelha metálica escura no chão como zona de saída, onde o produto empilha. Pizza Ready e Forge & Fortune usam mesa ou palete de saída. Nenhum escreve o nome da estação.
- **Correção.**
  - Boca de **entrada**: placa quadrada de 0,9 m (palete/grelha) com o ícone "deitado" do item a 30% de alfa.
  - Boca de **saída**: mesa ou palete onde a pilha cresce de verdade, com os ícones novos.
  - Barra de progresso: arredondada, 0,12 → 0,16 m, com o ícone do produto na ponta.
  - Nome da estação só aparece 2 s quando ela nasce (pop) e no tutorial.

**P1-4. Pads de construção = círculo tracejado com "+" e texto em cima** · P–M · código + ícones
- **Hoje.** `DashedRing` + "+" + rótulo "2ª fornalha / 895" (foto 01).
- **Similares.** Pizza Ready e Burger Please usam placa retangular arredondada no chão com pictograma e preço; My Perfect Hotel usa pílula "CLEANER 300"; Forge & Fortune usa "Lift Upgrade 800".
- **Correção.**
  - Placa de chão de 1,2 × 1,2 m, cantos de 0,2 m, borda tracejada branca, com o **ícone do que constrói** (bigorna, fornalha, rack de escudo, ajudante) e preço grande (48 px, moeda à esquerda) dentro da placa.
  - Enquanto paga, moedas voam do jogador para o pad (1 a cada 0,04 s) e a placa enche.
  - Ao concluir: puff de poeira e a estação nasce com pop 0 → 1,1 → 1 em 0,35 s (reaproveitar `PopTime` do luxo). A câmera desliza 0,6 s até o próximo pad liberado (PocketGamer).

**P1-5. Pilha nas costas pequena e rígida** · P · código (usa P0-1)
- **Hoje.** Itens de 0,34 m (~32 px) sobre disco escuro, empilhados reto a cada 0,2 m acima da cabeça (`ItemS`, `StackStep`).
- **Similares.** Burger Please e Pizza Ready mostram coluna alta de produto 3D com "MAX" vermelho quando cheia. Forge & Fortune carrega espadas cruzadas.
- **Correção.**
  - Sprite "deitado" de 0,5 m (~48 px), sem disco de fundo (o contorno do ícone resolve), passo de 0,13 m.
  - Cada item com rotação aleatória fixa de ±4° e deslocamento de ±0,02 m.
  - Ao andar, balanço em mola (atraso 0,08 s por item, amplitude 3°).
  - Item entra voando em arco de 0,18 s, com 0,06 s entre itens e tique de pitch crescente.
  - "MAX" vermelho com contorno branco quando `Count == cap`.

**P1-6. Cenário escuro e chato: chão, paredes, exterior, luz** · M · código + texturas
- **Hoje.** Laje `#4E4539` sobre ~70% da tela (fotos 01 e 02). Paredes são faixas em planta sem altura (só a de baixo tem face). O exterior é calçamento cinza-escuro uniforme. Tochas com brilho de 1,5 m a 30%. O jogo inteiro fica em valor baixo, contra a ART_BIBLE §7 ("nenhum preto fechado").
- **Similares.** Todos têm chão claro e de valor alto e paredes em corte com altura visível (face + tampo claro + friso colorido: Pizza Ready, My Mini Mart, Forge & Fortune). Fora da loja há mundo: grama, árvores, rua com carros, cerca.
- **Correção.**
  - Chão da oficina com valor +35% (base ~`#7A6A58`) ou assoalho claro na área de trabalho. Pedra fria só na rua.
  - Paredes com altura: face de 0,5 m em toda parede de frente + tampo claro `#9A8E80` de 0,15 m + viga de madeira; pilar de pedra de 0,6 × 0,6 m em cada ponta de abertura.
  - Poças de luz quente (glow de 3–4 m, alfa 0,25) nas fornalhas e tochas; vinheta leve (−15% nas bordas).
  - Exterior: calçada com meio-fio, 2 postes, 2–3 árvores ou canteiros, fachadas vizinhas na borda de cima (de onde vêm os clientes) e uma carroça parada.

### P2

**P2-1. Produção sem partícula** · P–M · código
- Bigorna: 8–12 faíscas `#FFD166` → `#FF7A1F` em 0,3 s a cada martelada, com punch 1 → 1,06.
- Fornalha: fumaça na chaminé (3 puffs/s, sobe 1 m) e boca pulsando.
- Item pronto: pop 0 → 1,15 → 1 em 0,2 s.
- Forge & Fortune mostra faíscas e fumaça; o movimento é **não confirmado**.

**P2-2. Upgrade não aparece na estação** · M · Blender + código
- Fole e Fole duplo, Martelo veloz, Mochila e Botas mudam número mas não mudam imagem.
- Correção com props sobrepostos: fole extra animado na fornalha, martelo dourado na bigorna, mochila sobre o sprite do ferreiro, rastro de poeira nas botas.
- Referência: o card de máquina do Burger Please troca o modelo ("Level 3").

**P2-3. Câmera longe demais** · P (código) · **experimento**, mexe na leitura do jogo
- Em 11,4 m de largura tudo fica pequeno. O ferreiro tem ~50 px, e é a causa de fundo do "cru".
- Os similares enquadram bem mais perto. Estimativa a olho, não medida: ~7–9 m de largura.
- Testar `VisibleWidth` de 9 m com câmera seguindo o jogador (já segue depois do Corredor) num APK de playtest, antes de adotar.

**P2-4. Vibração** · P · código
- Pulso de 15–25 ms em venda, compra e "MAX" (Android `VibrationEffect`), com opção de desligar.
- Nos similares é **não confirmado**: loja não mostra vibração.

---

## 4. Especificação direta para as raias de arte e integração

### 4.1 Ícones dos 6 itens

Regras comuns:
- Blender, câmera ortográfica, `--light flat` com contorno; fonte 256×256 px, fundo transparente.
- Dois cortes por item: `icone_<item>` (3/4, diagonal) e `deitado_<item>` (de lado na câmera de 60°).
- Contorno `#1E1612` de 3 px a 128 px; 2 tons + 1 brilho; sombra de 4 px a 35% para baixo.
- Matiz por item como na ART_BIBLE §3, mas na **parte dominante do objeto real**.
- Item que não lê a 24 px em preto muda de forma, não de cor.
- Na integração, `Art.ItemSprite`/`ItemScale` passam a devolver esses sprites (Resources), com as máscaras atuais como reserva se faltar arquivo, igual a `Art.Ground`.

| Item | Silhueta e pose (corte "ícone") | Cores | Detalhe que dá a leitura | Corte "deitado" |
|---|---|---|---|---|
| Minério | Pedra de 5–6 faces + 2 lascas menores grudadas, ocupando 80% da célula | Base `#6E5A48`, sombra `#3E3128`, luz `#9C8468` | 2–3 veios brilhando `#FF7A1F` com miolo `#FFD166` (é minério, não pedra) | Mesma pedra, mais achatada |
| Lingote | Barra trapezoidal chanfrada em 3/4, comprimento = 85% da célula | Laterais `#C8D0DC`/`#8C96A6`, tampo `#EEF1F8` | Reflexo diagonal branco no tampo; nas pilhas, empilhar em "tijolinho" 2 + 1 | Barra de lado, 0,35 × 0,15 m |
| **Espada** | **Diagonal a 45°, ponta para cima-direita.** Lâmina reta = 70% da diagonal, com ponta e sulco central | Lâmina `#BFE3FF` (luz) / `#7FC4FF` (base) / `#3F6E99` (sombra) | **Guarda reta dourada `#E0B23A` larga (35–40% do comprimento da lâmina)**, punho de couro `#5C3A1E` com 2 faixas, pomo redondo dourado. A cruz lâmina × guarda é o que lê | Espada deitada na horizontal, guarda visível; no estande, **em pé** (lâmina para cima), corte próprio `empe_espada` |
| Escudo | **Redondo** (sai o losango do código), inclinado 15° | Face `#F2545B`, aro de aço `#8C96A6` (12% do raio), umbo `#E0B23A` | Umbo central + 4 rebites; aro grosso separa do disco liso | Escudo de pé de perfil a 60° |
| Ferramenta | Martelo de ferreiro na **diagonal oposta à espada** (cabeça em cima à esquerda) | Cabeça `#8C96A6` com face `#D5DCE6`, cabo de madeira `#8A5A32` | **Pano verde `#4CD964` enrolado no cabo** (mantém o matiz verde) + cabeça em T | Martelo deitado |
| Joia | Anel de ouro grosso com gema grande no topo (gema = 40% da altura) | Aro `#E0B23A`/`#9C7520`, gema `#B07CF2` facetada | Brilho branco na gema + cintilo de 4 pontas (só no ícone) | Anel deitado, gema para cima |

Também renderizar no mesmo lote: `moeda` (moeda de ouro 3/4, 72 px no HUD, 40 px no voo) e os ícones de upgrade do P1-2 (mochila, fole, fole duplo, botas, martelo dourado, estande, lupa, ajudante).

### 4.2 Balcão → estande evolutivo (sem toldo)

Não é uma barraca. É a **frente de loja do ferreiro**: balcão de madeira + racks de expor. Os clientes ficam na rua, em cima, olhando para baixo, e a câmera vê o balcão pelo lado da oficina.

| Módulo (sprite separado) | Medida (m) | Forma | Notas |
|---|---|---|---|
| `estande_base` | 2,4 × 0,7, altura 0,9 | Balcão reto de madeira escura `#5C3A1E`, tampo claro `#B8824F`, quinas com cantoneira de ferro | **Sem toldo.** Cofre de ferro com moedas à mostra na ponta direita (é onde as moedas do P0-5 caem) |
| `estande_ext` | +1,2 × 0,7 | Módulo de extensão do balcão | Entra com a Vitrine |
| `rack_espadas` | 1,1 × 0,3, altura 1,2 | Rack de madeira com **4 encaixes**; placa de ferro no topo com o ícone da espada | As espadas são `empe_espada` colocadas por código encaixe a encaixe: **o estoque é o rack** (substitui a grade de 21 px) |
| `rack_escudos` | 1,0 × 0,3 | 3 ganchos na vertical | Aparece com a compra de Escudos |
| `cavalete_ferramentas` | 0,9 × 0,4 | Cavalete com 3 martelos pendurados | Aparece com a compra de Ferramentas |
| vagas de fila | 0,6 por vaga | Marcas de pé pintadas no calçamento ou postes com corda | 4 visíveis no início, 8 com a Vitrine |

Evolução:
1. **Início:** `estande_base` + 1 `rack_espadas` (4 encaixes) + 4 vagas. Pílula "n/4" sobre o rack, laranja e "MAX" quando cheio.
2. **Escudos / Ferramentas:** o rack ou cavalete do produto nasce ao lado, com pop.
3. **Vitrine:** `estande_ext` + 2º `rack_espadas` → 8 encaixes e **8 vagas**. Hoje `Balance.QueueCap1 = 6` e `CounterCap1 = 10`; a raia core decide 6 → 8 e remede o bot (§3 do GDD).
4. **Fachada nobre (luxo):** friso dourado no balcão, estandarte com a heráldica bigorna + chama, 2 lanternas (combina com o varal que já existe).

Na ART_BIBLE §6 e no ASSETS #7, trocar "toldo listrado ouro/creme" por "estande de madeira com racks de expor". O `loja_joalheria` (toldo roxo) pode ficar como está, porque é outra loja, na rua.

### 4.3 Portão lateral

- **Um portão só, na abertura de cima** (y 11,6–13,4, ao lado do balcão, onde o jogador já está).
- **Abertura de baixo** (y 5,6–7,4):
  - Opção A, sem tocar no core: vira **porta de serviço** com a mesma arte de lado, menor (folha única).
  - Opção B: fechar com parede, o que mexe em `Balance.SideWallOpenings`, nos testes `Parede_SoPassaPelaPortaEPeloArco_AjudanteUsaAAbertura` e no Corredor; é decisão da raia core.
- **Arte (Blender):** girar o modelo **90° em Z** antes de renderizar, com a mesma câmera de 60°. Assim ele sai **de lado**, como uma parede leste–oeste é vista de cima. Os dois pilares ficam nas pontas da parede (x 9,0–9,6) em y 11,6 e 13,4, e a viga corre ao longo de Y.
- Três sprites:
  - `portao_fechado`: 2 folhas de tábua com 2 faixas de ferro + tranca atravessada + placa pendurada (em branco; o preço vai na HUD);
  - `portao_aberto`: folhas abertas contra a parede, do lado da rua;
  - `pilar` (0,6 × 0,6 m com capitel), também usado nas pontas da porta de serviço.
- **Pad:** o "Corredor" vai para o chão **colado na frente do portão** (x ≈ 8,3, y 12,5; conferir a distância das bocas e da fila, como pede a `FASE6` §2), usando a placa nova do P1-4 com ícone de portão e "690" grande.
- **Antes da compra:** a rua lateral aparece **em penumbra, mas com conteúdo**: silhuetas da barraca de joias e da carroça a 40% de valor. Sai o texto "Rua lateral" em cima da parede; uma plaquinha com ícone de gema fica pendurada no portão.
- **Na compra:**
  - a tranca cai e as folhas abrem (troca de sprite com escala X 1 → 0 → 1 em 0,4 s);
  - puff de poeira;
  - a câmera desliza 0,6 s para dentro da rua e volta;
  - som de dobradiça (`Sfx` sintetizado, varredura grave).
- **Porta de serviço (se ficar, opção A):** mesma regra. Folha fechada até o Corredor e **aberta** (não some) depois.

---

## 5. Pacote visual v0.5, em ordem de execução

Ordem: o que destrava os outros vem antes (ícones), depois o maior impacto por esforço.

| # | Mudança | Prioridade | Esforço | Depende de |
|---|---|---|---|---|
| 1 | Renderizar os 6 itens nos 2 cortes + `empe_espada` + `moeda` (§4.1) e ligar no `Art.ItemSprite` com reserva procedural | P0 | M | Blender + código |
| 2 | Balão grande só no 1º da fila, anel de paciência, estouro + coração (P0-2) | P0 | P | código |
| 3 | Moedas voando para a pílula do HUD + punch e número rolando (P0-5, versão visual) | P0 | P | código (+ `moeda` do #1) |
| 4 | Estande sem toldo com racks e estoque físico, 4 vagas (§4.2, passos 1–2) | P0 | G | Blender + código |
| 5 | Portão lateral de lado, fechado/aberto, pad colado, rua em penumbra com conteúdo (§4.3) | P0 | M | Blender + código |
| 6 | HUD: barra fina, fonte OFL com contorno, sem versão, dica como seta no mundo (P1-1) | P1 | P | código + fonte |
| 7 | Pilha nas costas: sprite "deitado" de 0,5 m, balanço, arco de entrada, "MAX" (P1-5) | P1 | P | código (#1) |
| 8 | Bocas viram placas de chão/mesa de saída; sem rótulo fixo nas estações (P1-3) | P1 | P | código |
| 9 | Pads de construção viram placas com ícone e preço; moedas para o pad; pop + câmera no novo ponto (P1-4) | P1 | P–M | código + ícones |
| 10 | Cartões do menu claros com ícone renderizado, "3 → 6", botão verde/cinza, badge no "Melhorias" (P1-2) | P1 | M | código + ícones Blender |
| 11 | Chão mais claro, paredes com altura e pilares, poças de luz, exterior com calçada, árvores e fachadas (P1-6) | P1 | M | código + texturas |
| 12 | Vitrine → `estande_ext` + 8 encaixes + 8 vagas (§4.2, passo 3), **depois** da decisão do core sobre `QueueCap1` | P0 | P (arte já feita no #4) | core + código |

Fora do pacote, para depois: P2-1 (partículas), P2-2 (upgrades visíveis), P2-3 (câmera mais perto, como experimento de playtest), P2-4 (vibração) e a moeda física recolhível (decisão de design).

**Portão do pacote:**
- foto `-shot` do APK no aparelho (ART_BIBLE §10), comparada lado a lado com a foto 02 atual;
- os 6 ícones a 24 px em preto: 5 de 5 identificáveis;
- 3 pessoas reconhecem "espada" no balão em menos de 1 s.
Até isso rodar, este documento é **hipótese de direção**, não medida.

## 6. Lacunas desta pesquisa

- Só capturas de loja, que são marketing e podem exagerar pilhas, efeitos e enquadramento. Não joguei nenhum título (regra da raia), então tween, som, vibração e ritmo de feedback ficam **não confirmados**.
- Ângulo de câmera e largura visível dos similares são estimativas a olho, não medidas.
- Forge & Fortune parece lançamento de teste (1 avaliação). Serve de referência de forma, não de sucesso comercial.
- Idle Lumber Empire não foi checado; a página da Google Play de Eatventure não foi lida (usei a App Store).
- Faltou ver os vídeos de prévia da loja, que mostrariam o "juice" em movimento. É o próximo passo barato se quiserem confirmar o P0-5 e o P2.
