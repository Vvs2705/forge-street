using System.Collections.Generic;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// Desenha a oficina a partir do Sim, sem regra nenhuma: estacoes com pilhas de entrada/saida visiveis e barra de
    /// progresso, pads com preco e preenchimento, portadores com a pilha na cabeca (cor + forma por item), clientes
    /// com balao do pedido e barra de paciencia, esteira, numeros flutuantes. Rotulos de texto vivem na HUD e seguem
    /// o ponto do mundo (uGUI, sem TextMesh). Personagens e as 4 estacoes do Lote 1 usam a folha pre-renderizada
    /// (SpriteSheet) quando ela existe; sem folha, o desenho procedural de sempre.
    /// 2a area (docs/archive/AREA2_JOALHERIA.md s7): rua lateral x 9-15 em pedra fria, escura com rotulo e o arco de teaser ate
    /// o Corredor; loja de joias escurecida "fechada" ate a Joalheria; clientes da JewelQueue com a folha `nobre`.
    /// Fase 2 (docs/archive/AREA2_FASE2.md s4): joalheiro (papel 3) com tinta roxa; bau de marco disponivel pulsando com o rotulo.
    /// Fase 3 (docs/archive/FASE3_LUXO.md): fachada nobre, piso de ladrilhos e joalheria real, procedurais, ligados pelas flags de luxo.
    /// Cenario: chaos com textura repetida (Resources/Textures ou reserva procedural), paredes 2,5D fora da area andavel e props
    /// pre-renderizados (tools/props_blender.py) encostados nelas, com tochas de brilho tremulo.
    /// v0.5 (docs/ASSETS.md s7, BENCHMARK_VISUAL s4): itens renderizados (icone/deitado, mascaras so de reserva), balao grande so no
    /// 1o da fila, estande do balcao montado por vaga com o estoque em pe, portoes vistos de lado e quem acha a fila cheia passando.
    /// v0.5b (BENCHMARK_VISUAL P1-3/4/6 + juice): bocas viram placas no chao (grelha de entrada, palete de saida), nome das estacoes so
    /// perto do jogador ou no 1o minuto, placas de obra com icone do que constroi + preco (moedas voam do jogador, a estacao nasce com
    /// pop e poeira), chao lavado de luz quente, poca de luz nas fornalhas/tochas/postes, grama, arvores e postes fora da oficina, balao
    /// que estoura com coracao na venda, rosto bravo abaixo de 20% e seta da dica no mundo.
    /// v0.6a (BENCHMARK_VISUAL P2-1, P2-2, P1-5): faiscas e punch na martelada, fumaca e boca acesa na fornalha, pop do item pronto,
    /// "MAX" sobre a pilha do ferreiro e upgrades visiveis (fole na fornalha, martelo dourado na bancada, mochila, poeira das botas).
    /// </summary>
    public sealed class WorldView : MonoBehaviour
    {
        sealed class StationV
        {
            public Station S; public Transform Root; public SpriteRenderer Base, BarBg, BarFill, BarIcon;
            public SpriteRenderer[] InPile, OutPile, Stock; public Item[] StockItems; public Text Label;   // Stock: 6 por item vendido
            public bool Baked;   // Base e' o sprite pre-renderizado (sem icone)
            public bool Stand;   // balcao principal desenhado pelo estande modular (BuildStand), sem Base nem grade de estoque
            public float PopT = -1f;   // nasceu agora (compra): pop 0 -> 1,1 -> 1
            // v0.6a: escala de repouso da pilha (pop do item novo)
            public Vector3 PileScale; public float ItemPopT = -1f;
            public StationView Forno;   // A-ART-06: os 6 estados da fornalha (StationView)
            public StationView Bancada; // A-ART-07: martelo no compasso da bigorna/bancada (StationView)
            // B-CORE-03: brilho de cada peca (Fina = estrela de 4 pontas prata, Superior = estrela de 5 pontas ouro), barra do acumulador e "Fina em 7"
            public SpriteRenderer[] OutGlint, StockGlint; public SpriteRenderer QBarBg, QBarFill; public Text QLabel; public float QT;
            public RecipeDef Receita;   // C-CORE-06: a receita com que as pilhas foram montadas (a bancada configuravel troca: a estacao e' refeita)
            public Text[] Faixas;   // C-CORE-07: uma linha por faixa do patio de cotas (o toque troca o produto dela, Game.Toque)
        }
        /// <summary>Placa de obra: fundo escuro, enchimento de ouro, borda tracejada, icone do que constroi; preco (moeda + valor) e nome na HUD.</summary>
        sealed class PadV
        {
            public Pad P; public Transform Root; public SpriteRenderer Fill, Border, Icon; public Text Label, Price; public RectTransform PriceBox; public Image Coin;
            public int LastU = -2; public long LastPaid, LastRem = -1; public float CoinT; public bool Pulsing, LastOk;   // LastRem/LastOk: o preco so e' reescrito quando muda
        }
        sealed class CarrierV { public Carrier C; public Transform Root; public SpriteRenderer[] Stack; public Body Body; public float CheerT, StackY, Top, DustT; public SpriteRenderer Pack; }   // StackY: onde a pilha apoia; Top: topo dela
        /// <summary>Balao grande do 1o da fila (um por fila, segue o cliente): trilho escuro, anel de paciencia, corpo, rabicho, icone e rosto bravo.</summary>
        sealed class BubbleV
        {
            public Transform Root; public SpriteRenderer Ring, Icon, Track, Body, Tail, Frame; public Transform Angry; public float PopT = -1f;
            public RectTransform Pill; public Text Pack; public int PackN = -1;   // v0.6d: pilula "xN" do pacote do VIP (era selo "x3" + "xN" solto)
        }
        /// <summary>Efeito curto (moeda, coracao, brilho, poeira): curva de Bezier A -> M -> B em `Life` s, escala S0 -> S1, some no fim se `Fade`.</summary>
        sealed class Fx { public SpriteRenderer R; public Vector3 A, M, B; public float Age, Life, S0, S1, Spin; public bool Fade, Live; public Color C, C2; }   // C -> C2 na vida
        sealed class ClientV
        {
            public Client C; public int Id; public Transform Root; public SpriteRenderer Want, Patience; public float TopY;   // TopY: topo da cabeca
            public Body Body; public int Phase; public float LeaveT; public bool Served; public Vector3 Lane, Exit;   // saida (so com arte)
            public bool Jewel;   // fila da loja de joias (JewelQueue/JewelSlot) em vez da do balcao
            public SpriteRenderer Crown; public Text Tag;   // v0.5c: coroa do VIP e "xN" do pacote (ou "VIP" esperando vaga)
            public bool Waiting;                             // VIP esperando ao lado da fila cheia (sem Client ainda)
        }
        sealed class ChestV { public Chest C; public Transform Root; public Text Label; }
        sealed class Floater { public Text T; public Vector3 World; public float Age; public bool Live; }
        /// <summary>Decoracao de uma compra de luxo: raiz ligada pela flag `Bought`; PopT >= 0 = pop em andamento.</summary>
        sealed class LuxV { public Upgrade U; public Transform Root; public float PopT = -1f; public int Off = -1; }   // Off: apaga quando esta compra existe (Fole -> Fole duplo)

        /// <summary>Corpo com folha pre-renderizada: clipe atual, tempo dentro dele e ultima direcao.</summary>
        sealed class Body
        {
            public SpriteSheet Sheet; public SpriteRenderer R; public string Clip; public float T; public int Dir;
            public void Tick(string clip, float dt, Vector2 vel, bool loop = false)
            {
                if (clip != Clip) { Clip = clip; T = 0f; } else T += dt;
                Dir = Sheet.Dir(vel, Dir);
                R.sprite = Sheet.Frame(Clip, Dir, T, loop);
                R.sortingOrder = Depth(R.transform.position.y);
            }
        }

        // Pilha na cabeca: item deitado de 0,5 m (~48 px no aparelho), sem disco de fundo (o contorno vem no PNG). Empilha do mais
        // grosso (embaixo) ao mais fino (em cima), cada um sobre o anterior com 25% de sobreposicao: a espada deitada (0,15 m) nao some
        // sob um escudo. Altura do deitado / lado, medida nos PNG da v0.5 (bbox x 1,1 / 128; arte nova = remedir). Coluna > StackMax encolhe.
        const float ItemS = 0.5f, StackMax = 1.6f;
        static readonly Item[] PileOrder = { Item.Ore, Item.Shield, Item.Jewel, Item.Tool, Item.Ingot, Item.LongSword, Item.Sword };   // B-CORE-07: a espada longa, fina como a espada
        static readonly float[] LyingH = { 0.88f, 0.57f, 0.3f, 0.95f, 0.61f, 0.95f, 0.3f };   // por Item (B-CORE-07: espada longa ~ espada; remedir com a arte)
        float[] _pileY;
        // C-CORE-01: a ordem e as alturas acima sao dos itens do Old Forge; distrito com outra tabela empilha na ordem dela, 0,6 de altura (remedir com a arte)
        Item[] _pileOrder; float[] _lyingH;
        internal const float PileS = 0.4f; const float PileStep = 0.2f;   // pilhas das estacoes (2 colunas de itens deitados)
        // Balao do pedido (BENCHMARK_VISUAL P0-2): 1o da fila com balao 1,32 x 1,12 m e icone de 0,9 m, deslocado 0,2 m para a esquerda
        // (o 1o e' a vaga mais a esquerda) para nao encostar no mini-icone do 2o; o rabicho fica sobre a cabeca. Anel = borda de 0,1 m.
        const float BubbleIcon = 0.9f, BubbleDX = -0.2f, BubbleUp = 0.77f, TailY = -0.62f;   // ponta do rabicho ~0,02 m acima de ClientTop
        static readonly Vector2 BalloonSize = new Vector2(1.32f, 1.12f), RingSize = new Vector2(1.53f, 1.33f), TailSize = new Vector2(0.32f, 0.28f);
        // demais clientes: mini-icone de 0,42 m (~40 px) acima da cabeca e a paciencia numa barrinha logo acima dele
        const float MiniIcon = 0.42f, MiniY = 0.24f, PatY = 0.51f, PatW = 0.36f;

        // Arte pre-renderizada. 1 m do Blender = 1/CharPpuMul m do mundo para TODOS os personagens (o anao fica menor que o
        // ferreiro, como no modelo). O ferreiro (0,98 m) mede 75-80 px de altura com ppu 109,9: a camera a 60 graus achata a
        // vertical pela metade (~0,70 m no plano da imagem), entao 0,76 da ~0,9 m de altura na tela. Ajuste fino pela foto.
        const float CharPpuMul = 0.76f;
        // Alturas do Tripo variam por modelo; iguala a razao da ART_BIBLE s4 (ferreiro 1,20 / ajudante 1,05 / guerreira 1,20 /
        // anao 0,95) medindo a altura em px de cada folha. Fator = altura_atual / altura_alvo, relativo ao ferreiro.
        // Mago fica em 1: de 60 graus a aba do chapeu esconde o corpo e a altura medida engana; goblin com a mochila (1,30 m) e cavaleiro com a pluma (1,55 m), como na ART_BIBLE s4.
        internal static float CharScale(string n) => CharPpuMul * n switch   // internal: o cartao do worker (MenuBar) usa a mesma escala da folha
        {
            "ajudante" => 1.19f, "guerreira" => 1.24f, "anao" => 1.08f, "elfa" => 1.03f, "goblin" => 1.11f, "cavaleiro" => 0.79f, "nobre" => 0.91f,
            // Lote 4 (coordenador 2026-10-08, ASSETS s9: (altura_m / 0,664) x (1,20 / alvo), reproduz os de cima +-0,01)
            "minerador" => 0.96f, "bardo" => 0.85f, "alquimista" => 0.99f, "monge" => 0.95f, "ladina" => 0.98f,
            "paladina" => 0.86f, "orc" => 0.76f, "barbaro" => 0.79f, "pirata" => 0.97f, "cacadora" => 0.93f,
            // Lote 5A (raia ARTE TRIPO 2026-10-13, ASSETS s11: (altura_m / 0,664) x (1,20 / alvo)); os 4 clientes entram com os distritos
            "oficial" => 1.02f, "balconista" => 1.20f, "almoxarife" => 1.18f, "mestre_obras" => 1.16f, "fundidor" => 1.02f,
            "mensageiro" => 1.02f, "gerente" => 1.00f,
            "campones" => 1.00f, "guarda_real" => 0.88f, "emissario" => 0.99f, "colecionador" => 0.86f,
            _ => 1f,
        };
        // m do mundo por celula de estacao. Com 1,65 a peca ocupava 0,9-1,24 m de largura e o corpo solido tem 1,3 m (FASE5): o
        // ferreiro batia numa borda invisivel. Com 1,95 a arte cobre o corpo (1,08-1,47 m de largura, 1,06-1,49 m de altura).
        const float StationCell = 1.95f;
        // Topo da cabeca na tela, medido na foto v0.5 (validation_v05/shots, 47,4 px/m): ferreiro ~0,65 m acima do pe, clientes
        // 0,5-0,65 (chapeu do mago e da elfa). Antes era 0,95 "no olho": pilha e balao flutuavam ~0,3 m acima da cabeca.
        const float HeadY = 0.65f, ClientTop = 0.6f;
        const float LeaveSpeed = 2.5f;     // m/s do cliente indo embora pela rua
        const float OneShotMax = 3f;       // ponytail: teto de Thankful (3,0 s no Lote 1) e Waving (0,5 s) para clipe longo futuro nao empilhar a rua no pico de vendas
        const int StackOrder = 2000, BubbleOrder = 3000;   // acima de todo corpo ordenado por Depth
        // estacao parada: "fome" = apagada e cinza (ART_BIBLE §6). Era 0,78: no video do criativo #8 (docs/CRIATIVOS.md) a bigorna
        // com fome ficava igual a que trabalhava; 0,55 frio le a 1 m do celular e continua mais clara que a loja fechada (Shut).
        internal static readonly Color Dimmed = new Color(0.55f, 0.55f, 0.6f, 1f);
        // clientes do balcao (Lote 1-2 + Lote 4 de 2026-10-08); nobre = so a loja de joias. Nome sem folha em Resources e' ignorado.
        static readonly string[] ClientArt = { "guerreira", "anao", "mago", "elfa", "goblin", "cavaleiro",
            "minerador", "bardo", "alquimista", "monge", "ladina", "paladina", "orc", "barbaro", "pirata", "cacadora" };
        // 2a area (AREA2 s7). Carroca na celula das estacoes (o arco Tripo saiu na v0.5: o portao de lado ocupa a abertura de cima).
        const float CartCell = StationCell;
        static readonly V2 CartPos = new V2(14.2f, 3.0f);
        static readonly Color StreetOpen = Gray(0.8f), StreetShut = Gray(0.35f);    // tinta da rua lateral: aberta / escurecida ate o Corredor
        static readonly Color Shut = new Color(0.42f, 0.42f, 0.48f, 1f);             // loja fechada (teaser): mais escura que a "fome"
        // Bau de marco (AREA2_FASE2 s3/s4): o bau ocupa 179 de 256 px da folha, entao celula 1,1 m da ~0,77 m de largura (menor
        // que a estacao de 1,5 m, maior que o item da pilha). Pulsa 1 <-> 1,08 pelo pe (pivo da folha) enquanto disponivel.
        const float ChestCell = 1.1f, ChestLabelY = 0.85f;
        // Fase 3 (docs/archive/FASE3_LUXO.md): luxo so decora. Pop 0,6 -> 1 em 0,3 s na compra; ao carregar aparece pronto.
        const float PopTime = 0.3f, PopFrom = 0.6f;
        // Fachada: soleira dourada na borda de cima da oficina, 2 postes com estrela e varal de bandeirolas em 3 arcos.
        // Atras do balcao, da fila (pe em y 14,0 = Depth 300) e do jogador; na frente de quem passa pela rua (y 14,7).
        const float FacadeY = 14.1f, PostH = 0.85f, PostInset = 0.15f, Sag = 0.3f;
        const int FacadeArcs = 3, FacadeSeg = 6;   // 18 bandeirolas, ~0,48 m entre elas
        // Luxo Piso de oficina: ladrilho de 1 m limpo, xadrez de pedra quente base/luz (ART_BIBLE s2) com junta de ouro apagado.
        static readonly Color TileA = Art.Hex(0x574C43), TileB = Art.Hex(0x483F38), TileJoint = Art.Hex(0x7A6232);
        // Cenario (Vinicius 2026-10-07: "chao, paredes, outras coisas esteticas"). Chaos em textura repetida (Art.Ground) e paredes
        // 2,5D FORA da area andavel (jogador preso em [0,3; W-0,3] x [0,3; 13,7]): tampo claro e, so na parede de baixo (a unica de
        // frente para a camera), face escura logo abaixo. Oficina x 0-9: parede alta de pedra quente com a abertura do arco; rua
        // lateral x 9-15: muro baixo da pedra fria da rua. A borda de cima (rua dos clientes) fica aberta.
        const float WallT = 0.6f, WallFace = 0.6f, MuroT = 0.4f, MuroFace = 0.35f;
        // Parede direita da oficina = Balance.SideWallX0-X1 com as aberturas de Balance.SideWallOpenings (porta de servico 5,6-7,4 e
        // portao 11,6-13,4; a mesma geometria solida da simulacao). v0.5 (ASSETS s7): portoes vistos DE LADO, fechados ate o Corredor,
        // com pilar de pedra nas 4 pontas (centro 0,23 m alem da ponta: o pe de 0,6 m encosta 0,07 m no vao).
        const float PillarOut = 0.23f;
        const float DeckY = 12f;                     // assoalho (madeira) na frente da loja: balcao, bau e fila; o luxo Piso cobre so a pedra
        internal const int ExteriorOrder = -10, FloorOrder = -9, LuxOrder = -8, WashOrder = -7, ShadeOrder = -5, WallOrder = -4, GlowOrder = -3;   // abaixo do tapete (-1), sombras e pads (1-4)
        static readonly Color FaceTint = Gray(0.55f), ShadeColor = new Color(0f, 0f, 0f, 0.35f);
        // v0.5b (P1-6): luz quente "lavando" o chao da oficina (alfa sobre a laje ~#493F36 -> ~#7A6A58, sem shader aditivo: Built-in RP
        // e nenhum shader extra no APK), grama no exterior, tampo claro na borda de dentro das paredes e pilares nos cantos.
        static readonly Color Wash = new Color(0.95f, 0.78f, 0.56f, 0.3f), StreetWash = new Color(0.75f, 0.83f, 0.95f, 0.22f), WallCap = Art.Hex(0xB3A797);
        const float CapW = 0.1f;
        static readonly V2[] CornerPillars = { new V2(-0.3f, -0.3f), new V2(-0.3f, 7.4f), new V2(-0.3f, 13.75f), new V2(9.3f, -0.3f) };
        // exterior: arvores e arbustos atras da parede de baixo e alem da rua de cima, postes na beira da rua (com poca de luz), canteiros.
        // Fora do clamp do jogador e dos clientes (rua de cima ate y 16,6; faixa de quem vai embora em y ~14,7).
        static readonly (string Name, float X, float Y)[] Outside =
        {
            ("arvore", -0.9f, -2.5f), ("arbusto", 1.4f, -2.1f), ("arvore", 4.6f, -2.7f), ("arbusto", 6.9f, -2.2f), ("arvore", 9.9f, -2.5f),
            ("arbusto", 12.4f, -2.0f), ("arvore", 15.9f, -2.4f), ("arvore", 16.3f, 4.2f), ("arbusto", 16.1f, 9.0f), ("arvore", 16.3f, 12.6f),
            ("arvore", -0.6f, 17.4f), ("canteiro", 2.2f, 17.0f), ("arvore", 4.6f, 17.6f), ("canteiro", 7.0f, 17.0f), ("arvore", 10.2f, 17.4f),
            ("canteiro", 12.6f, 17.0f), ("arvore", 15.2f, 17.6f),
            ("poste", 0.9f, 16.75f), ("poste", 8.2f, 16.75f), ("poste", 13.9f, 16.75f),
        };
        const float LampY = 2.13f;
        static readonly Color MuroTint = new Color(0.72f, 0.8f, 0.95f), MuroFaceTint = new Color(0.4f, 0.44f, 0.52f);   // pedra fria (ART_BIBLE s2) sobre a parede quente
        // Props (tools/props_blender.py; escala real, 1 m do Blender = 1 m do mundo) encostados nas paredes, fora do clamp e longe de
        // pads, estacoes, filas, baus e rotulos: tampo da parede esquerda (x -0,3 a -0,45), quintal atras da parede de baixo (y < -1,2),
        // muro da rua lateral (x 15,2-15,45) e quintal atras do muro de baixo da rua. Borda de cada prop <= x 0,05 / >= x 14,95: o corpo
        // do jogador encostado no clamp (raio ~0,25) nao entra nele. Tocha: pivo no pe da parede, chama 0,46 m acima.
        static readonly (string Name, float X, float Y)[] Props =
        {
            ("balde", 0f, 0.55f), ("prateleira", -0.42f, 2.75f), ("tocha", -0.3f, 4.0f), ("tocha", -0.3f, 11.0f),
            ("suporte_armas", -0.45f, 12.4f),   // v0.5b: barril (13,55) e balde (0,1) cederam o canto aos pilares
            ("lenha", 0.6f, -1.45f), ("tocha", 2.0f, -1.2f), ("barril", 3.0f, -1.35f), ("caixote", 4.1f, -1.4f), ("sacos", 5.3f, -1.4f),
            ("tocha", 6.6f, -1.2f), ("barril", 7.6f, -1.35f), ("balde", 8.3f, -1.3f),
            ("barril", 15.4f, 0.6f), ("caixote", 15.45f, 1.6f), ("tocha", 15.2f, 7.0f), ("sacos", 15.4f, 10.2f), ("tocha", 15.2f, 13.0f),
            ("lenha", 11.2f, -0.95f), ("barril", 13.4f, -0.9f), ("balde", 14.0f, -0.85f),
        };
        // brilho alfa (Sprites/Default): aditivo pediria shader fora do APK. v0.5b: 1,5 m -> 2,6 m (poca de luz no chao, P1-6)
        const float FlameY = 0.46f, GlowSize = 2.6f, GlowAlpha = 0.2f;
        // Placa de obra (P1-4): 1,2 m (= Balance.PadRadius x 2), icone de 0,7 m acima do meio, preco (moeda + valor) no terco de baixo
        const float PadSize = 1.2f, PadIconBox = 0.66f, PadIconY = 0.13f, PadPriceY = -0.33f, PadCoinEvery = 0.06f;
        internal static readonly Color GrateC = Art.Hex(0x2B2E36), GrateBar = Art.Hex(0x4F5866), PalletC = Art.Hex(0x6B4428), PlankC = Art.Hex(0x9A6A3E);
        const float NearLabel = 2.6f, LabelFade = 0.25f, TutorialLabels = 60f;   // nome de estacao/placa: perto do jogador ou no 1o minuto (fade curto: meio-alfa lia como defeito)
        const float StationPop = 0.35f, BubblePop = 0.32f;
        const float BarW = 1.2f, BarH = 0.16f, BarY = -0.74f;   // barra de progresso arredondada (era 1,3 x 0,12 reta)
        // VIP (v0.5c, docs/FASE8_VIP_VELOCIDADE.md s5): coroa de 0,46 m "vestida" na cabeca (centro 0,04 m acima do topo), mini-icone do VIP
        // fora da fila sobe 0,16 m para a coroa caber; balao com moldura de ouro, corpo creme e selo "x3"; quem espera a fila cheia fica
        // 0,95 m a direita da ultima vaga, 0,35 m para tras.
        const float CrownS = 0.46f, CrownY = 0.04f, VipMiniUp = 0.16f;
        static readonly Vector3 VipWaitOff = new Vector3(0.95f, 0.35f, 0f);
        static readonly Color VipCream = Art.Hex(0xFFF3D2);
        const int FxOrder = 3500, FxMax = 64;
        internal static readonly Color Dust = new Color(0.86f, 0.78f, 0.66f, 0.55f), HeartC = Art.Hex(0xFF4D6D), AngryC = Art.Hex(0xFF6B3D);
        internal static readonly Color GlowColor = Art.Hex(0xFF9A3D);
        static float PropShadow(string n) => n switch { "tocha" => 0f, "balde" => 0.4f, "barril" => 0.6f, "prateleira" or "suporte_armas" or "lenha" => 0.9f, _ => 0.75f };
        static Color Gray(float v) => new Color(v, v, v, 1f);
        // Joalheria real: tapete roxo da bancada a loja (x 11,45-12,55) e 4 pedestais de gema fora dos pads, da fila e do bau.
        static readonly Color Carpet = Color.Lerp(Art.ItemColor[(int)Item.Jewel], Art.Bg, 0.55f);   // #59447F: a joia do estoque ainda le por cima
        const float CarpetW = 1.1f;
        static readonly V2[] Pedestals = { new V2(10.95f, 8.4f), new V2(13.05f, 8.4f), new V2(10.95f, 10.9f), new V2(13.05f, 10.9f) };
        // v0.6a (P2-1): pop do item pronto 0 -> 1,15 -> 1 em 0,2 s. A martelada (faiscas, punch) foi para a StationView no A-ART-07.
        const float ItemPop = 0.2f;
        const int FxAmbient = 40;   // faisca, fumaca e poeira so com menos de 40 efeitos vivos: venda, moedas e obra nunca ficam sem vaga
        internal static readonly Color SparkA = Art.Hex(0xFFD166), SparkB = Art.Hex(0xFF7A1F);
        // P2-2: icone do upgrade como prop. Fole na parede esquerda da fornalha (a direita ja tem o fole da arte), acima da pilha de
        // entrada; martelo dourado encostado no lado direito da bancada, acima da pilha de saida; mochila nas costas do ferreiro (so sem a
        // folha ferreiro_mochila, que ja traz a mochila no corpo).
        static readonly Vector2 BellowsAt = new Vector2(-0.7f, 0.55f), HammerAt = new Vector2(0.66f, 0.22f);
        const float BellowsS = 0.45f, HammerS = 0.5f, PackS = 0.38f, PackY = 0.33f, PackDX = 0.14f, DustEvery = 0.34f;   // botas: ate 3 baforadas/s
        // P1-5: "MAX" vermelho em italico com contorno escuro sobre a pilha, enquanto o ferreiro insiste e 0,8 s depois
        const float MaxHold = 0.8f;
        static readonly Color MaxC = Art.Hex(0xFF2D2D);
        // B-CORE-03 (GDD §18: Fina = brilho prata, Superior = dourado; cor + forma): estrela de 4 pontas prata de 0,2 m ou de 5 pontas ouro de
        // 0,26 m no canto da peca, piscando. Barra do acumulador (0,07 m) logo abaixo da de progresso; "Fina em 7" logo abaixo dela (o nome da
        // estacao desce), so perto: o toque nela (360 x 64 px, pega a barra) abre o QualityBreakdown. Anel do golpe de 170 px 1,6 m acima da
        // bancada, so com o ferreiro parado na boca dela.
        internal static readonly Color Silver = Art.Hex(0xDDE6F2);
        const float GlintFine = 0.2f, GlintSup = 0.26f, QBarY = BarY - 0.14f, QBarH = 0.07f, QLabelY = BarY - 0.36f, NameQY = BarY - 0.62f, RingY = 1.6f, RingPx = 170f;
        // v0.6d (revisao de UX, px na largura 1080 = 94,7 px/m): seta da dica de ~80 px quicando 12 px a 1,5 Hz; placa creme com borda
        // verde e pulso 1,05 quando da para pagar, preco salmao quando nao da; rotulos de 30 px com contorno #2A1E14 de 3 px
        const float ArrowS = 0.95f, ArrowY = 1.5f, ArrowBob = 0.127f, SaleMerge = 0.4f;
        static readonly Color Edge = Art.Hex(0x2A1E14), PlateCream = Art.ComAlfa(Art.Hex(0xE9D8B4), 0.9f), PadOk = Art.Hex(0x5BD16B),
            PadNo = Art.ComAlfa(Art.Hex(0x2A1E14), 0.45f), PriceNo = Art.Hex(0xFF8A7A), StreetInk = Art.Hex(0xE8D9B8), PillC = Art.Hex(0xFFD34D);

        /// <summary>sortingOrder pelo pe: quem esta mais abaixo na tela fica na frente (1 cm de resolucao/2).</summary>
        internal static int Depth(float footY) => 1000 - Mathf.RoundToInt(footY * 50f);

        Sim _sim; Camera _cam; RectTransform _labels;
        readonly List<StationV> _stations = new List<StationV>();
        readonly List<PadV> _pads = new List<PadV>();
        readonly List<ChestV> _chests = new List<ChestV>();
        CarrierV _player;
        readonly List<CarrierV> _workers = new List<CarrierV>();
        readonly List<ClientV> _clients = new List<ClientV>();
        readonly List<SpriteSheet> _clientSheets = new List<SpriteSheet>();
        // Elenco "rotacionando" (pedido do Vinicius): saco embaralhado, usa todos antes de repetir e nao repete o ultimo na virada.
        // ponytail: semente fixa = mesma ordem a cada sessao (fotos/criativos reproduziveis); ninguem decora 16 clientes.
        readonly List<int> _artBag = new List<int>();
        readonly System.Random _artRng = new System.Random(7);
        int _lastArt = -1;
        SpriteSheet _nobre;
        SpriteSheet _packSheet, _plainSheet;   // ferreiro com e sem a mochila (Init; troca em RefreshCarrier)
        SpriteRenderer _street, _streetWash, _shopTeaser; Text _streetLabel, _shopLabel;
        readonly List<(GameObject Shut, GameObject Open)> _gates = new List<(GameObject, GameObject)>();   // portao e porta de servico
        BubbleV _bubble, _jewelBubble;
        // Estande do balcao (ASSETS s7, integracao 2): modulos de 0,85 m (um por vaga) entre pontas de 0,2 m, na largura do corpo do
        // balcao (Balance.CounterHalfX); estoque em pe nos encaixes do meta.json, 1 item por unidade de estoque ate encher os encaixes.
        // Modulos = os N primeiros desta ordem; linha ainda nao comprada vira rack de espadas (encaixe vazio leria como "falta").
        // ponytail: com a Vitrine (estoque 10) os encaixes de escudo/ferramenta (2-6) mostram so parte do estoque; 1 por unidade
        // pediria 12 modulos. Se o playtest ler "cheio" errado, uma pilula "n/10" no modulo resolve.
        static readonly Item[] StandOrder = { Item.Sword, Item.Shield, Item.Sword, Item.Tool, Item.Shield, Item.Sword, Item.Tool, Item.Shield };
        static readonly string[] StandPieces = { "balcao_ponta_esq", "balcao_meio", "balcao_escudos", "balcao_ferramentas", "balcao_ponta_dir" };
        bool _hasStand; Transform _stand; int _standKey = -1; float _standPop = -1f;
        readonly List<(Item It, SpriteRenderer R, SpriteRenderer G)> _standStock = new List<(Item, SpriteRenderer, SpriteRenderer)>();   // G: brilho (B-CORE-03)
        readonly int[] _shown = new int[6];
        readonly List<Floater> _floaters = new List<Floater>();
        readonly List<LuxV> _lux = new List<LuxV>();
        readonly List<SpriteRenderer> _glows = new List<SpriteRenderer>();
        readonly List<Fx> _fx = new List<Fx>();
        Transform _arrow; bool _hintOn; V2 _hintAt;   // seta da dica (ShowHint)
        int _fxLive;                                  // efeitos vivos (TickFx), para o teto dos ambientes
        Text _max; float _maxT, _maxAge;              // "MAX" sobre a pilha do ferreiro (RefreshMax)
        RectTransform _ring; Image _ringOut, _ringTarget; float _ringT; Station _ringAt;   // B-CORE-03: anel do golpe (RefreshRing)
        Sprite _coin, _crown;
        ClientV _vipWait;
        EsteiraView _esteira;   // B-ART-08: faixa, lingotes em transito, clac e particulas
        int _clientSeq, _bought;
        Floater _sale; int _saleSum;   // v0.6d: ultimo "+N" de venda (SaleFloat soma nele)
        float _streetHalfW;
        /// <summary>Base da faixa da HUD de cima em px de tela (Game.Fit): o balao do 1o da fila fica 24 px abaixo (v0.6d).</summary>
        public float HudBottomPx = float.MaxValue;

        public static Vector3 W(V2 p) => new Vector3(p.X, p.Y, 0f);

        public void Init(Sim sim, Camera cam, RectTransform labels)
        {
            _sim = sim; _cam = cam; _labels = labels;
            _bought = sim.UpgradesBought;
            int nItens = sim.ItemDefs.Length;
            _pileY = new float[Balance.PlayerCapUp * nItens];
            _pileOrder = PileOrder; _lyingH = LyingH;
            if (sim.ItemDefs != Items.All)
            {
                _pileOrder = new Item[nItens]; _lyingH = new float[nItens];
                for (int i = 0; i < nItens; i++) { _pileOrder[i] = (Item)i; _lyingH[i] = 0.6f; }
            }
            _coin = Art.Icon("moeda", "icone");
            _crown = Art.Icon("coroa", "icone");
            foreach (string n in ClientArt)
                if (SpriteSheet.TryGet(n, out SpriteSheet sh)) { sh.Scale = CharScale(n); _clientSheets.Add(sh); }
            if (SpriteSheet.TryGet("nobre", out _nobre)) _nobre.Scale = CharScale("nobre");
            BuildScenery();
            _streetLabel = Art.FreeText(_labels, "RuaLateral", 28, new Vector2(200f, 80f));
            _streetLabel.text = Loc.T("mundo.rua");   // 2 linhas: so 1,2 m da rua cabe na tela antes do Corredor
            _streetLabel.color = StreetInk;       // v0.6d: 28 px #E8D9B8 (era Ink a 55%, sumia na pedra)
            _streetHalfW = _streetLabel.preferredWidth / 2f;
            // C-CORE-01: decoracao do Old Forge so onde o distrito tem a peca (carroca = o obstaculo dela; loja e luxo = o efeito/estacao)
            foreach (ObstacleDef o in sim.Distrito.Obstaculos) if (o.Name == "carroca") Deco("carroca", CartPos, CartCell, 1.3f);
            _shopTeaser = sim.JewelShop != null ? Deco("loja_joalheria", sim.JewelShop.Pos, StationCell, 1.3f) : null;   // mesma folha e celula da estacao
            if (_shopTeaser != null) _shopTeaser.color = Shut;
            _shopLabel = Art.FreeText(_labels, "JoalheriaFechada", 26, new Vector2(400f, 40f));
            _shopLabel.text = Loc.T("mundo.joalheria_fechada");
            _shopLabel.color = Art.ComAlfa(Art.Ink, 0.7f);
            _shopLabel.enabled = _streetLabel.enabled = false;
            if (sim.TemEfeito(Upgrade.WorkshopFloor)) Lux(Upgrade.WorkshopFloor, BuildTiles());
            if (sim.TemEfeito(Upgrade.WorkshopFacade)) Lux(Upgrade.WorkshopFacade, BuildFacade());
            if (sim.TemEfeito(Upgrade.JewelryDecor) && sim.JewelShop != null) Lux(Upgrade.JewelryDecor, BuildJewelryDecor());
            _hasStand = sim.ItemDefs == Items.All;   // os modulos do estande sao das linhas do Old Forge (espada, escudo, ferramenta)
            foreach (string n in StandPieces) _hasStand &= SpriteSheet.TryGet(n, out _);   // sem os modulos: balcao procedural de sempre
            _bubble = BuildBubble("BalaoFila");
            _jewelBubble = BuildBubble("BalaoJoias");
            foreach (Station s in sim.Stations) _stations.Add(BuildStation(s));
            foreach (Pad p in sim.Pads) _pads.Add(BuildPad(p));
            foreach (Chest c in sim.Chests) _chests.Add(BuildChest(c));
            _esteira = new EsteiraView(this, sim, transform);
            _player = BuildCarrier(sim.Player, Art.Player, 0.62f, Balance.PlayerCapUp * sim.Player.Held.Length);   // FASE7: ate o teto de CADA tipo
            // Mochila (feedback do POCO 2026-10-13, "item solto"): com a folha ferreiro_mochila (mesmo render, mochila presa ao Spine2) a compra
            // troca a folha do corpo (RefreshCarrier). Sem ela, v0.6a (P2-2): icone do cartao nas costas (RefreshCarrier a poe do lado das costas)
            if (_player.Body != null && SpriteSheet.TryGet("ferreiro_mochila", out _packSheet)) { _packSheet.Scale = CharScale("ferreiro"); _plainSheet = _player.Body.Sheet; }
            else
            {
                Transform pack = _player.Body != null ? Prop(_player.Root, "melhoria_mochila", Upgrade.PlayerCapacity, Vector2.zero, PackS, 0) : null;
                if (pack != null) _player.Pack = pack.GetComponentInChildren<SpriteRenderer>(true);
            }
            // "MAX" (P1-5) na HUD
            _max = Art.Outlined(Art.FreeText(_labels, "Max", 46, new Vector2(200f, 64f)), 3f);
            _max.text = Loc.T("mundo.max"); _max.fontStyle = FontStyle.BoldAndItalic; _max.color = MaxC; _max.enabled = false;
            // seta da dica: contorno escuro atras, ouro na frente, por cima de tudo menos os efeitos. v0.6d: ~80 px (era 0,5 m = 42 px),
            // contorno de 4 px #2A1E14 (Art.Arrow)
            _arrow = Group("SetaDica", new V2(0f, 0f));
            Art.NewSprite(_arrow, "Contorno", Art.Arrow(true), Edge, FxOrder - 2, Vector2.zero, Vector2.one * ArrowS);
            Art.NewSprite(_arrow, "Seta", Art.Arrow(false), Art.Accent, FxOrder - 1, Vector2.zero, Vector2.one * ArrowS);
            _arrow.gameObject.SetActive(false);
            BuildRing();
        }

        /// <summary>
        /// B-CORE-03: anel do golpe de mestre na HUD (rotulos), num lugar fixo acima da bancada: alvo de ouro parado, anel branco fechando sobre
        /// ele (QualidadeView.Escala), martelo no meio e "GOLPE" embaixo. Na janela do acerto o anel fica verde e o alvo acende. O toque e' do
        /// Game (NoAnel/AcertoAgora), por fora do joystick.
        /// </summary>
        void BuildRing()
        {
            _ring = Art.Node(_labels, "AnelGolpe", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _ring.sizeDelta = Vector2.one * RingPx;
            Image fundo = Art.Node(_ring, "Fundo", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            fundo.sprite = Art.Disc(); fundo.color = Art.ComAlfa(Art.HudFace, 0.55f); fundo.raycastTarget = false;
            _ringTarget = Art.Node(_ring, "Alvo", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            _ringTarget.sprite = Art.Ring(); _ringTarget.color = Art.Gold; _ringTarget.raycastTarget = false;
            _ringOut = Art.Node(_ring, "Fecha", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            _ringOut.sprite = Art.Ring(); _ringOut.raycastTarget = false;
            Image ham = Art.Node(_ring, "Martelo", new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f)).gameObject.AddComponent<Image>();
            Sprite hs = Art.Icon("melhoria_martelo", "icone");
            ham.sprite = hs != null ? hs : Art.Star(); ham.color = hs != null ? Color.white : Art.Accent; ham.preserveAspect = true; ham.raycastTarget = false;
            Text t = Art.Outlined(Art.NewText(_ring, "Nome", 30, new Vector2(0f, -0.3f), new Vector2(1f, 0f)), 2.5f, Edge);
            t.text = Loc.T("golpe.nome"); t.fontStyle = FontStyle.Bold; t.color = Art.Accent; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            _ring.gameObject.SetActive(false);
        }

        /// <summary>B-CORE-03: brilho por peca de uma pilha ou estoque, ao lado de cada item (desligado ate a peca ser Fina ou Superior).</summary>
        static SpriteRenderer[] Glints(Transform root, SpriteRenderer[] items, int order)
        {
            var g = new SpriteRenderer[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                g[i] = Art.NewSprite(root, "Brilho", Art.Sparkle(), Silver, order, (Vector2)items[i].transform.localPosition + new Vector2(0.11f, 0.07f), Vector2.one * GlintFine);
                g[i].enabled = false;
            }
            return g;
        }

        /// <summary>Liga o brilho no nivel `l` (forma e cor), piscando 1 -> 1,2 -> 1 (cada peca na sua fase).</summary>
        static void Glint(SpriteRenderer g, Quality l, int i)
        {
            bool on = l > Quality.Normal;
            if (g.enabled != on) g.enabled = on;
            if (!on) return;
            bool sup = l == Quality.Superior;
            g.sprite = sup ? Art.Star() : Art.Sparkle();
            g.color = sup ? SparkA : Silver;
            g.transform.localScale = Vector3.one * ((sup ? GlintSup : GlintFine) * (1.1f + 0.1f * Mathf.Sin(Time.time * 5f + i * 1.7f)));
        }

        // ------------------------------------------------------------------ construcao

        string StationArt(Station s)
        {
            if (s.Arte != null) return s.Arte;   // C-CORE-01: StationDef.Arte (a Row usa a da familia mais proxima)
            switch (s.Kind)
            {
                case Kind.Deposit: return "deposito";
                case Kind.Furnace: return "fornalha";
                case Kind.Counter: return s == _sim.JewelShop ? "loja_joalheria" : null;   // balcao: estande modular (o toldo saiu na v0.5)
                default: return s.OutItem switch { Item.Sword or Item.LongSword => "bigorna", Item.Shield => "bancada_escudos", Item.Tool => "bancada_ferramentas", Item.Jewel => "bancada_joalheria", _ => null };
            }
        }

        /// <summary>Quadro da folha estatica `name` numa celula de `cell` m (ppu = celula); null sem folha (procedural).</summary>
        static Sprite StaticArt(string name, float cell)
        {
            if (name == null || !SpriteSheet.TryGet(name, out SpriteSheet sh)) return null;
            sh.Scale = sh.Size / (cell * sh.Ppu);   // 1 celula por nome: o 1o Frame fica no cache
            return sh.Frame("Static", 0, 0f);
        }

        /// <summary>Decoracao estatica ordenada pelo pe como os corpos, com sombra de `shadow` m de largura (0 = sem); devolve o sprite (o pai liga/desliga junto com a sombra), ou null sem folha.</summary>
        SpriteRenderer Deco(string name, V2 pos, float cell, float shadow)
        {
            Sprite art = StaticArt(name, cell);
            if (art == null) return null;   // ponytail: sem folha a decoracao nao existe (o greybox nao precisa dela)
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            root.localPosition = W(pos);
            if (shadow > 0f) Art.NewSprite(root, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.25f), 1, Vector2.zero, new Vector2(shadow, shadow * 0.46f));
            return Art.NewSprite(root, "Arte", art, Color.white, Depth(pos.Y), Vector2.zero, Vector2.one);
        }

        /// <summary>Celula da folha na escala real (1 m do Blender = 1 m do mundo): props, estande, portoes.</summary>
        static float RealCell(string name) => SpriteSheet.TryGet(name, out SpriteSheet sh) ? sh.Size / sh.Ppu : 1f;

        // ------------------------------------------------------------------ cenario: chao, paredes, props

        /// <summary>Chaos, paredes, sombra de contato e props. Nada muda com o jogo, so a tinta da rua lateral (RefreshArea) e o brilho das tochas.</summary>
        void BuildScenery()
        {
            if (!_sim.Distrito.Parede) { BuildRua(); return; }
            float ws = Balance.WorkshopW, w = Balance.WorldW, h = Balance.WorldH;
            Sprite rua = Art.Ground("rua"), parede = Art.Ground("parede");
            // fundo de toda a camera: grama (v0.5b; era calcamento a 42%, "exterior vazio"), um pouco abaixo do valor da oficina
            Tiled("Exterior", Art.Ground("grama"), Gray(0.82f), ExteriorOrder, new Vector2(-4f, -7f), new Vector2(w + 4f, h + 8f));
            Tiled("Rua", rua, StreetOpen, FloorOrder, new Vector2(-4f, h), new Vector2(w + 4f, h + 2.6f));             // rua dos clientes, aberta
            _street = Tiled("RuaLateral", rua, StreetShut, FloorOrder, new Vector2(ws, 0f), new Vector2(w, h));
            Tiled("Chao", Art.Ground("piso_oficina"), Color.white, FloorOrder, Vector2.zero, new Vector2(ws, DeckY));
            Tiled("Assoalho", Art.Ground("madeira"), Color.white, FloorOrder, new Vector2(0f, DeckY), new Vector2(ws, h));
            Art.NewSprite(transform, "LuzQuente", Art.Square(), Wash, WashOrder, new Vector2(ws / 2f, h / 2f), new Vector2(ws, h));   // sobre chao, assoalho e piso de luxo
            // ruas: luz fria (pedra fria da ART_BIBLE s2 mais clara); a lateral so depois do Corredor (antes ela e' a penumbra do teaser)
            Art.NewSprite(transform, "LuzRua", Art.Square(), StreetWash, WashOrder, new Vector2(w / 2f, h + 1.3f), new Vector2(w + 8f, 2.6f));
            _streetWash = Art.NewSprite(transform, "LuzRuaLateral", Art.Square(), StreetWash, WashOrder, new Vector2((ws + w) / 2f, h / 2f), new Vector2(w - ws, h));
            // sombra de contato: luz-chave de baixo-esquerda (ART_BIBLE s7) -> a parede projeta para a direita/para cima
            Shade(Vector2.zero, new Vector2(0.35f, h), false);
            Shade(Vector2.zero, new Vector2(w, 0.3f), true);
            // oficina: esquerda e direita so o tampo (de lado nao ha face para a camera); embaixo tampo + face
            Tiled("ParedeEsq", parede, Color.white, WallOrder, new Vector2(-WallT, 0f), new Vector2(0f, h), true);
            float x0 = Balance.SideWallX0, x1 = Balance.SideWallX1;
            float[] gaps = Balance.SideWallOpenings;
            for (int i = 0; i <= gaps.Length; i += 2)   // segmentos entre as aberturas: [0, g0], [g1, g2], [g3, h]
            {
                float y0 = i == 0 ? 0f : gaps[i - 1], y1 = i < gaps.Length ? gaps[i] : h;
                Tiled("ParedeDir", parede, Color.white, WallOrder, new Vector2(x0, y0), new Vector2(x1, y1), true);
                Shade(new Vector2(x1, y0), new Vector2(x1 + 0.35f, y1), false);
            }
            // pilares nas pontas das aberturas (ordem pelo pe) e as folhas de lado: portao em cima, porta de servico embaixo (RefreshArea)
            float wx = (x0 + x1) / 2f;
            for (int i = 0; i < gaps.Length; i++) Deco("pilar", new V2(wx, gaps[i] + (i % 2 == 0 ? -PillarOut : PillarOut)), RealCell("pilar"), 0f);
            _gates.Add(Gate("portao_fechado", "portao_aberto", new V2(wx, (gaps[2] + gaps[3]) / 2f)));
            _gates.Add(Gate("porta_servico_fechada", "porta_servico_aberta", new V2(wx, (gaps[0] + gaps[1]) / 2f)));
            Tiled("ParedeBaixo", parede, Color.white, WallOrder, new Vector2(-WallT, -WallT), new Vector2(ws + WallT, 0f));
            Tiled("ParedeFace", parede, FaceTint, WallOrder, new Vector2(-WallT, -WallT - WallFace), new Vector2(ws + WallT, -WallT));
            // rua lateral: muro baixo, a mesma alvenaria esfriada (calcamento no tampo lia como chao, nao como muro)
            Tiled("MuroDir", parede, MuroTint, WallOrder, new Vector2(w, 0f), new Vector2(w + MuroT, h), true);
            Tiled("MuroBaixo", parede, MuroTint, WallOrder, new Vector2(ws + WallT, -MuroT), new Vector2(w + MuroT, 0f));
            Tiled("MuroFace", parede, MuroFaceTint, WallOrder, new Vector2(ws + WallT, -MuroT - MuroFace), new Vector2(w + MuroT, -MuroT));
            foreach ((string n, float x, float y) in Props)
            {
                SpriteRenderer art = Deco(n, new V2(x, y), RealCell(n), PropShadow(n));
                if (art != null && n == "tocha")
                    _glows.Add(Art.NewSprite(art.transform.parent, "Brilho", Art.Glow(), Art.ComAlfa(GlowColor, GlowAlpha), GlowOrder, new Vector2(0f, FlameY), Vector2.one * GlowSize));
            }
            // v0.5b (P1-6): tampo claro na borda de dentro das paredes (le como topo de parede alta) e pilares nos cantos
            Art.NewSprite(transform, "TampoEsq", Art.Square(), WallCap, WallOrder + 1, new Vector2(-CapW / 2f, h / 2f), new Vector2(CapW, h));
            Art.NewSprite(transform, "TampoBaixo", Art.Square(), WallCap, WallOrder + 1, new Vector2(ws / 2f, -CapW / 2f), new Vector2(ws, CapW));
            for (int i = 0; i <= gaps.Length; i += 2)
            {
                float y0 = i == 0 ? 0f : gaps[i - 1], y1 = i < gaps.Length ? gaps[i] : h;
                Art.NewSprite(transform, "TampoDir", Art.Square(), WallCap, WallOrder + 1, new Vector2(x0 + CapW / 2f, (y0 + y1) / 2f), new Vector2(CapW, y1 - y0));
            }
            foreach (V2 p in CornerPillars) Deco("pilar", p, RealCell("pilar"), 0f);
            foreach ((string n, float x, float y) in Outside)
            {
                SpriteRenderer art = Deco(n, new V2(x, y), RealCell(n), n == "arvore" ? 1.3f : n == "poste" ? 0.4f : 1f);
                if (art != null && n == "poste")
                    _glows.Add(Art.NewSprite(art.transform.parent, "Brilho", Art.Glow(), Art.ComAlfa(GlowColor, GlowAlpha), GlowOrder, new Vector2(0f, LampY * 0.5f), Vector2.one * GlowSize * 1.2f));
            }
        }

        /// <summary>
        /// C-CORE-01: distrito sem a parede lateral do Old Forge (Armorer's Row): uma rua so, de pedra fria, com a parede de pedra quente
        /// a esquerda e embaixo, o muro baixo a direita, os mesmos props e o exterior. ponytail: o kit visual proprio do distrito (C-ART-13)
        /// troca o chao, a paleta e os props.
        /// </summary>
        void BuildRua()
        {
            float w = Balance.WorldW, h = Balance.WorldH;
            Sprite rua = Art.Ground("rua"), parede = Art.Ground("parede");
            Tiled("Exterior", Art.Ground("grama"), Gray(0.82f), ExteriorOrder, new Vector2(-4f, -7f), new Vector2(w + 4f, h + 8f));
            Tiled("Rua", rua, StreetOpen, FloorOrder, new Vector2(-4f, h), new Vector2(w + 4f, h + 2.6f));
            Tiled("Chao", rua, StreetOpen, FloorOrder, Vector2.zero, new Vector2(w, h));
            Art.NewSprite(transform, "LuzRua", Art.Square(), StreetWash, WashOrder, new Vector2(w / 2f, (h + 2.6f) / 2f), new Vector2(w + 8f, h + 2.6f));
            Shade(Vector2.zero, new Vector2(0.35f, h), false);
            Shade(Vector2.zero, new Vector2(w, 0.3f), true);
            Tiled("ParedeEsq", parede, Color.white, WallOrder, new Vector2(-WallT, 0f), new Vector2(0f, h), true);
            Tiled("ParedeBaixo", parede, Color.white, WallOrder, new Vector2(-WallT, -WallT), new Vector2(w + MuroT, 0f));
            Tiled("ParedeFace", parede, FaceTint, WallOrder, new Vector2(-WallT, -WallT - WallFace), new Vector2(w + MuroT, -WallT));
            Tiled("MuroDir", parede, MuroTint, WallOrder, new Vector2(w, 0f), new Vector2(w + MuroT, h), true);
            Art.NewSprite(transform, "TampoEsq", Art.Square(), WallCap, WallOrder + 1, new Vector2(-CapW / 2f, h / 2f), new Vector2(CapW, h));
            Art.NewSprite(transform, "TampoBaixo", Art.Square(), WallCap, WallOrder + 1, new Vector2(w / 2f, -CapW / 2f), new Vector2(w, CapW));
            foreach ((string n, float x, float y) in Props)
            {
                SpriteRenderer art = Deco(n, new V2(x, y), RealCell(n), PropShadow(n));
                if (art != null && n == "tocha")
                    _glows.Add(Art.NewSprite(art.transform.parent, "Brilho", Art.Glow(), Art.ComAlfa(GlowColor, GlowAlpha), GlowOrder, new Vector2(0f, FlameY), Vector2.one * GlowSize));
            }
            for (int i = 0; i < 3; i++) Deco("pilar", CornerPillars[i], RealCell("pilar"), 0f);   // o 4o canto e' o da parede lateral do Old Forge
            foreach ((string n, float x, float y) in Outside)
            {
                SpriteRenderer art = Deco(n, new V2(x, y), RealCell(n), n == "arvore" ? 1.3f : n == "poste" ? 0.4f : 1f);
                if (art != null && n == "poste")
                    _glows.Add(Art.NewSprite(art.transform.parent, "Brilho", Art.Glow(), Art.ComAlfa(GlowColor, GlowAlpha), GlowOrder, new Vector2(0f, LampY * 0.5f), Vector2.one * GlowSize * 1.2f));
            }
        }

        /// <summary>
        /// Folhas fechada e aberta de uma abertura da parede direita (raizes; RefreshArea liga uma ou outra), logo acima da parede e
        /// abaixo de todo corpo: o fechado fica na linha da parede, o aberto encostado nela do lado da rua. Sem folha: nada (greybox).
        /// </summary>
        (GameObject Shut, GameObject Open) Gate(string shut, string open, V2 pos)
        {
            SpriteRenderer a = Deco(shut, pos, RealCell(shut), 0f), b = Deco(open, pos, RealCell(open), 0f);
            if (a != null) a.sortingOrder = WallOrder + 1;
            if (b != null) b.sortingOrder = WallOrder + 1;
            return (a != null ? a.transform.parent.gameObject : null, b != null ? b.transform.parent.gameObject : null);
        }

        /// <summary>Retangulo [min, max] com a textura repetida (1 renderer, modo Tiled); `along` gira 90 graus (parede vertical: fiadas ao longo dela).</summary>
        SpriteRenderer Tiled(string name, Sprite s, Color c, int order, Vector2 min, Vector2 max, bool along = false, Transform parent = null)
        {
            Vector2 size = max - min;
            SpriteRenderer r = Art.NewSprite(parent != null ? parent : transform, name, s, c, order, (min + max) * 0.5f, Vector2.one, along ? 90f : 0f);
            r.drawMode = SpriteDrawMode.Tiled;
            r.size = along ? new Vector2(size.y, size.x) : size;
            return r;
        }

        /// <summary>Sombra de contato no chao ao pe de uma parede: degrade escuro saindo dela (`up` = parede embaixo; senao a esquerda).</summary>
        void Shade(Vector2 min, Vector2 max, bool up)
        {
            Vector2 size = max - min;
            Art.NewSprite(transform, "SombraParede", Art.Fade(), ShadeColor, ShadeOrder, (min + max) * 0.5f, up ? new Vector2(size.y, size.x) : size, up ? 90f : 0f);
        }

        // cor do papel: o que ele carrega (3 = joalheiro, roxo #B07CF2 da joia; AREA2_FASE2 s4)
        static Item RoleItem(int role) => role == 0 ? Item.Ore : role == 1 ? Item.Ingot : role == 3 ? Item.Jewel : Item.Sword;

        /// <summary>C-CORE-06: a bancada configuravel trocou de receita: pilhas, bocas e barra mostram o item novo (a estacao toda e' refeita, uma
        /// vez por troca; ponytail: so as pilhas bastariam, mas o icone da barra e as bocas tambem sao do item).</summary>
        StationV Refaz(StationV v)
        {
            Destroy(v.Root.gameObject);
            if (v.Label != null) Destroy(v.Label.gameObject);
            if (v.QLabel != null) Destroy(v.QLabel.gameObject);
            return BuildStation(v.S);
        }

        StationV BuildStation(Station s)
        {
            var v = new StationV { S = s, Root = new GameObject(s.Name).transform, Stand = s == _sim.Counter && _hasStand, Receita = s.Recipe };
            v.Root.SetParent(transform, false);
            v.Root.localPosition = W(s.Pos);
            Sprite art = StaticArt(StationArt(s), StationCell);
            if (v.Stand) { }   // estande: modulos por vaga (BuildStand, refeito quando as vagas mudam)
            else if (art != null)
            {
                Art.NewSprite(v.Root, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.25f), 1, Vector2.zero, new Vector2(1.55f, 0.7f));
                v.Base = Art.NewSprite(v.Root, "Arte", art, Color.white, Depth(s.Pos.Y), Vector2.zero, Vector2.one);
                v.Baked = true;
            }
            else
            {
                Color c = Art.StationColor(s);
                v.Base = Art.NewSprite(v.Root, "Base", Art.Rounded(), c, 2, Vector2.zero, new Vector2(1.5f, 1.1f));
                Sprite icon = s.Kind == Kind.Counter ? Art.Star() : s.Kind == Kind.Deposit ? Art.Rock() : Art.ItemSprite(s.OutItem);
                Color ic = s.Kind == Kind.Counter ? Art.Accent : s.Kind == Kind.Deposit ? Art.ItemColor[0] : Art.ItemColor[(int)s.OutItem];
                Vector2 isc = s.Kind == Kind.Counter || s.Kind == Kind.Deposit ? new Vector2(0.5f, 0.5f) : Art.ItemScale(s.OutItem, 0.5f);
                Art.NewSprite(v.Root, "Icone", icon, Art.ComAlfa(ic, 0.55f), 3, new Vector2(0f, 0.08f), isc);
            }
            v.Label = Art.Outlined(Art.FreeText(_labels, s.Name, 30, new Vector2(320f, 44f)), 3f, Edge);   // v0.6d: 28 -> 30 px, contorno 3 px
            v.Label.text = Textos.Estacao(s.Index);
            v.Label.fontStyle = FontStyle.Bold;
            if (s.Kind == Kind.Quota)   // C-CORE-07: as faixas do patio, sempre visiveis embaixo dele (ponytail: texto; a arte das faixas vem com o C-ART-13)
            {
                v.Faixas = new Text[_sim.Faixas.Length];
                for (int k = 0; k < v.Faixas.Length; k++)
                {
                    v.Faixas[k] = Art.Outlined(Art.FreeText(_labels, "Faixa", 24, new Vector2(460f, 34f)), 2.5f, Edge);
                    v.Faixas[k].fontStyle = FontStyle.Bold;
                    v.Faixas[k].enabled = false;
                }
            }
            // bocas no chao (FASE5 s2b): deposito so entrega, balcoes so recebem, quem produz tem as duas. v0.5b: placa no chao
            if (s.Kind == Kind.Deposit) Mouth(v.Root, s.OutAt - s.Pos, false, (int)s.OutItem);   // o bruto do deposito (minerio; couro na Row)
            else Mouth(v.Root, s.InAt - s.Pos, true, s.Produces ? (int)s.InItem : s == _sim.JewelShop ? (int)Item.Jewel : -1);
            if (s.Produces) Mouth(v.Root, s.OutAt - s.Pos, false, (int)s.OutItem);
            if (s.Produces)
            {
                v.InPile = Pile(v.Root, s.InItem, s.InCap, -1.05f);
                // B-CORE-04: a pilha de saida cresce com o almoxarife (Equipe.Saida); pre-aloca o teto
                v.OutPile = Pile(v.Root, s.OutItem, Mathf.Max(s.Kind == Kind.Furnace ? Balance.FurnaceOut : Balance.CrafterOut, s.Recipe.Qtd > 1 ? 2 * s.Recipe.Qtd : 0) + WorkerDefs.QmSaida, 1.05f);   // C-CORE-01: rebitadeira guarda 8
                v.PileScale = v.OutPile[0].transform.localScale;
                // barra arredondada (capsula 9-fatias) com o icone do produto na ponta (P1-3)
                v.BarBg = Capsule(v.Root, "BarraFundo", Art.ComAlfa(Art.Bg, 0.85f), 3, new Vector2(0f, BarY), BarW, BarH);
                v.BarFill = Capsule(v.Root, "Barra", Art.Accent, 4, new Vector2(0f, BarY), BarH - 0.05f, BarH - 0.05f);
                v.BarIcon = Art.NewSprite(v.Root, "BarraIcone", null, Color.white, 5, new Vector2(BarW / 2f + 0.1f, BarY + 0.02f), Vector2.one);
                Art.PaintItem(v.BarIcon, s.OutItem, true, 0.34f);
                if (_sim.IsProduct(s.OutItem))   // B-CORE-03: brilho das pecas, barra do acumulador e "Fina em 7" (ligados na revelacao)
                {
                    v.OutGlint = Glints(v.Root, v.OutPile, 7);
                    v.QBarBg = Capsule(v.Root, "QualidadeFundo", Art.ComAlfa(Art.Bg, 0.85f), 3, new Vector2(0f, QBarY), BarW, QBarH);
                    v.QBarFill = Capsule(v.Root, "Qualidade", Silver, 4, new Vector2(0f, QBarY), QBarH, QBarH);
                    v.QLabel = Art.Outlined(Art.FreeText(_labels, "Qualidade", 26, new Vector2(360f, 64f)), 2.5f, Edge);
                    v.QLabel.fontStyle = FontStyle.Bold;
                    v.QBarBg.enabled = v.QBarFill.enabled = v.QLabel.enabled = false;
                }
            }
            if (s.Kind == Kind.Furnace)   // A-ART-06: luz, boca, fumaca e os 6 estados na StationView; aqui so o fole do upgrade (o simples apaga quando chega o duplo)
            {
                v.Forno = new StationView(this, s, v.Root, v.Base, v.Baked);
                Prop(v.Root, "melhoria_fole", Upgrade.FurnaceSpeed1, BellowsAt, BellowsS, Depth(s.Pos.Y) + 1, (int)Upgrade.FurnaceSpeed2);
                Prop(v.Root, "melhoria_fole_duplo", Upgrade.FurnaceSpeed2, BellowsAt, BellowsS * 1.15f, Depth(s.Pos.Y) + 1);
            }
            if (s.Kind == Kind.Crafter)
            {
                v.Bancada = new StationView(this, s, v.Root, v.Base, v.Baked);   // A-ART-07: martelo, faiscas, som e "+1" no compasso
                Prop(v.Root, "melhoria_martelo", Upgrade.HammerSpeed, HammerAt, HammerS, Depth(s.Pos.Y) + 1);   // Martelo veloz
            }
            if (s.Kind == Kind.Counter && !v.Stand)
            {
                var sold = new List<Item>();   // loja de joias: so joia; balcao sem os modulos do estande: espada/escudo/ferramenta
                for (int k = (int)Item.Sword; k < _sim.ItemDefs.Length; k++) if (_sim.Sells(s, (Item)k)) sold.Add((Item)k);   // B-CORE-07: todo produto
                v.StockItems = sold.ToArray();
                v.Stock = new SpriteRenderer[sold.Count * 6];   // 6 visiveis por produto, colunas centradas na estacao
                for (int p = 0; p < sold.Count; p++)
                    for (int i = 0; i < 6; i++)
                    {
                        Item item = sold[p];
                        var pos = new Vector2((p - (sold.Count - 1) * 0.5f) * 0.95f + (i % 3) * 0.26f - 0.26f, -0.62f - (i / 3) * 0.26f);
                        v.Stock[p * 6 + i] = Art.NewSprite(v.Root, "Estoque", null, Color.white, 3, pos, Vector2.one);
                        Art.PaintItem(v.Stock[p * 6 + i], item, false, 0.25f);
                        v.Stock[p * 6 + i].enabled = false;
                    }
                v.StockGlint = Glints(v.Root, v.Stock, 5);   // B-CORE-03
            }
            v.Root.gameObject.SetActive(s.Unlocked);
            return v;
        }

        /// <summary>
        /// Boca no chao (v0.5b, BENCHMARK P1-3; era anel com "play"): placa quadrada do tamanho da zona (Balance.MouthRadius x 2).
        /// Entrada = grelha de ferro escura com o item que entra deitado a 55% ("ponha aqui"; balcao = moeda); saida = palete de
        /// madeira (a pilha da estacao fica ao lado). Sem a arte do item, a mascara dele.
        /// </summary>
        static void Mouth(Transform root, V2 off, bool into, int item)
        {
            var at = new Vector2(off.X, off.Y);
            float d = Balance.MouthRadius * 2f;
            Art.NewSprite(root, into ? "BocaEntra" : "BocaSai", Art.Rounded(), Art.ComAlfa(into ? GrateC : PalletC, 0.9f), 0, at, Vector2.one * d);   // ripas (1) por cima
            for (int k = -1; k <= 1; k++)   // 3 barras da grelha / 3 tabuas do palete
                Art.NewSprite(root, "BocaRipa", Art.Square(), into ? GrateBar : PlankC, 1, at + new Vector2(0f, k * d * 0.27f), new Vector2(d * 0.82f, d * (into ? 0.06f : 0.2f)));
            if (!into) return;
            SpriteRenderer r = Art.NewSprite(root, "BocaItem", null, Color.white, 2, at, Vector2.one);
            if (item >= 0) Art.PaintItem(r, (Item)item, false, 0.5f);
            else if (Art.Icon("moeda", "deitado") is Sprite coin) { r.sprite = coin; r.transform.localScale = Vector3.one * 0.5f * 1.1f; }
            else { r.sprite = Art.Disc(); r.color = Art.Accent; r.transform.localScale = Vector3.one * 0.35f; }
            r.color = Art.ComAlfa(r.color, 0.55f);
        }

        /// <summary>Capsula (pontas redondas em qualquer largura): SpriteRenderer Sliced com a escala = altura.</summary>
        static SpriteRenderer Capsule(Transform root, string name, Color c, int order, Vector2 at, float w, float h)
        {
            SpriteRenderer r = Art.NewSprite(root, name, Art.Capsule(), c, order, at, Vector2.one * h);
            r.drawMode = SpriteDrawMode.Sliced;
            r.size = new Vector2(w / h, 1f);
            return r;
        }

        /// <summary>Pilha visivel ao lado da estacao: 2 colunas de itens deitados, de baixo para cima (a fileira de cima cobre a de baixo).</summary>
        SpriteRenderer[] Pile(Transform root, Item item, int cap, float x)
        {
            var arr = new SpriteRenderer[cap];
            for (int i = 0; i < cap; i++)
            {
                var pos = new Vector2(x + (i % 2) * 0.27f - 0.13f, -0.38f + (i / 2) * PileStep);
                arr[i] = Art.NewSprite(root, "Pilha", null, Color.white, 3 + i / 2, pos, Vector2.one);
                Art.PaintItem(arr[i], item, false, PileS);
                arr[i].enabled = false;
            }
            return arr;
        }

        PadV BuildPad(Pad p)
        {
            var v = new PadV { P = p, Root = new GameObject("Pad " + p.Slot).transform };
            v.Root.SetParent(transform, false);
            v.Root.localPosition = W(p.Pos);
            // placa de obra (P1-4; era anel tracejado com "+"): quadrado arredondado escuro, enchimento de ouro do centro para fora,
            // borda tracejada e o icone do que sera construido; preco (moeda + valor) e nome vivem na HUD (RefreshPad)
            // v0.6d: placa creme (era #2A2420, sumia no chao escuro) e enchimento de ouro mais forte para ler sobre ela
            Art.NewSprite(v.Root, "Placa", Art.Rounded(), PlateCream, 2, Vector2.zero, Vector2.one * PadSize);
            v.Fill = Art.NewSprite(v.Root, "Pago", Art.Rounded(), Art.ComAlfa(Art.Gold, 0.8f), 3, Vector2.zero, Vector2.zero);
            v.Border = Art.NewSprite(v.Root, "Borda", Art.DashedBox(), Color.white, 4, Vector2.zero, Vector2.one * PadSize);
            v.Icon = Art.NewSprite(v.Root, "Obra", null, Color.white, 5, Vector2.zero, Vector2.one);
            v.Label = Art.Outlined(Art.FreeText(_labels, "PadNome", 30, new Vector2(360f, 44f)), 3f, Edge);   // v0.6d: 24 -> 30 px
            v.Label.fontStyle = FontStyle.Bold;
            v.PriceBox = Art.Node(_labels, "PadPreco", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            v.PriceBox.sizeDelta = new Vector2(150f, 44f);
            v.Coin = Art.Node(v.PriceBox, "Moeda", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).gameObject.AddComponent<Image>();
            v.Coin.rectTransform.sizeDelta = new Vector2(40f, 40f);
            v.Coin.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            v.Coin.sprite = _coin != null ? _coin : Art.Disc(); v.Coin.color = _coin != null ? Color.white : Art.Accent; v.Coin.raycastTarget = false;
            v.Price = Art.Outlined(Art.NewText(v.PriceBox, "Valor", 34, Vector2.zero, Vector2.one, TextAnchor.MiddleLeft), 2.5f);
            v.Price.fontStyle = FontStyle.Bold;
            v.Price.horizontalOverflow = HorizontalWrapMode.Overflow;
            v.Price.rectTransform.offsetMin = new Vector2(46f, 0f);
            return v;
        }

        /// <summary>
        /// Icone da placa: a arte do que nasce ali (estacao, portao, ajudante, item); sem arte, "+" de sempre. `fill` = fracao da celula
        /// que a arte ocupa (medida nos PNG: estacoes 0,62-0,77, ajudante 0,65), para o desenho e nao a celula vazia encher a caixa.
        /// </summary>
        public Sprite PadIcon(int u, out Color tint, out float fill)
        {
            tint = Color.white;
            fill = 0.72f;
            // C-CORE-01: a estacao que o upgrade abre (a 1a do distrito; Joalheria = a bancada), o efeito (Corredor, Esteira) ou o ajudante que ele contrata
            Station abre = _sim.Stations.Find(x => x.UnlockBy == u && x.Kind != Kind.Counter);
            string st = abre != null ? StationArt(abre) : null;
            if (st != null) return StaticArt(st, StationCell);   // mesma celula da estacao: o 1o Frame no cache serve aos dois
            UpgradeDef d = _sim.UpgradeDefs[u];
            if (d.Efeito == Upgrade.SideCorridor) { fill = 1f; return StaticArt("portao_fechado", RealCell("portao_fechado")); }
            if (d.Efeito == Upgrade.Conveyor) { fill = 0.9f; return Art.ItemArt(_sim.EsteiraDe != null ? _sim.EsteiraDe.OutItem : Item.Ingot, true) ?? Art.Rounded(); }
            if (System.Array.Exists(_sim.Equipe.Defs, k => k.Contrata == d.Id) && SpriteSheet.TryGet("ajudante", out SpriteSheet sh))
            {
                sh.Scale = CharScale("ajudante");   // o quadro entra no cache com a escala do corpo do ajudante (BuildCarrier)
                fill = 0.65f;
                return sh.Frame("Idle", 0, 0f);
            }
            fill = 1f;
            if (d.Luxury) { tint = Art.Accent; return Art.Star(); }
            tint = Art.Accent;
            return Art.Plus();
        }

        /// <summary>Encaixa o sprite (qualquer ppu/pivo) numa caixa de `box` m centrada em `center`.</summary>
        static void Fit(SpriteRenderer r, Sprite s, float box, Vector2 center)
        {
            r.sprite = s;
            Bounds b = s.bounds;
            float k = box / Mathf.Max(b.size.x, b.size.y, 0.01f);
            r.transform.localScale = new Vector3(k, k, 1f);
            r.transform.localPosition = center - (Vector2)b.center * k;
        }

        /// <summary>Bau de marco: folha `bau` ordenada pelo pe como os corpos (sem folha, caixa dourada), rotulo do marco acima. Comeca escondido.</summary>
        ChestV BuildChest(Chest c)
        {
            var v = new ChestV { C = c, Root = new GameObject("Bau " + c.Index).transform };
            v.Root.SetParent(transform, false);
            v.Root.localPosition = W(c.Pos);
            Art.NewSprite(v.Root, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.25f), 1, Vector2.zero, new Vector2(0.9f, 0.4f));
            Sprite art = StaticArt("bau", ChestCell);
            if (art != null) Art.NewSprite(v.Root, "Arte", art, Color.white, Depth(c.Pos.Y), Vector2.zero, Vector2.one);
            else Art.NewSprite(v.Root, "Arte", Art.Rounded(), Art.CounterC, Depth(c.Pos.Y), new Vector2(0f, 0.2f), new Vector2(0.7f, 0.5f));
            v.Label = Art.FreeText(_labels, "Marco", 26, new Vector2(300f, 40f));
            v.Label.text = Textos.Bau(c.Index);
            v.Label.color = Art.Accent;
            v.Label.fontStyle = FontStyle.Bold;
            v.Label.enabled = false;
            v.Root.gameObject.SetActive(false);
            return v;
        }

        // ------------------------------------------------------------------ luxo (fase 3): so decoracao, zero regra

        Transform Group(string name, V2 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(transform, false);
            t.localPosition = W(pos);
            return t;
        }

        /// <summary>Liga a decoracao pela flag ja carregada (save ou -buy): aparece pronta, sem pop. `off` = upgrade que a apaga.</summary>
        void Lux(Upgrade u, Transform root, int off = -1)
        {
            var v = new LuxV { U = u, Root = root, Off = off };
            root.gameObject.SetActive(On(v));
            _lux.Add(v);
        }

        bool On(LuxV v) => _sim.Has(v.U) && (v.Off < 0 || !_sim.Has((Upgrade)v.Off));   // C-CORE-01: pelo efeito (Fole de couro = fole; Mochila do ferreiro)

        /// <summary>
        /// v0.6a (P2-2): icone renderizado do upgrade (`melhoria_*`, 1 m de celula) como prop de `size` m em `at` do pai, ligado pela
        /// flag com o pop do luxo (RefreshLux). Devolve a raiz; null sem o icone (greybox sem prop).
        /// </summary>
        Transform Prop(Transform parent, string icon, Upgrade u, Vector2 at, float size, int order, int off = -1)
        {
            Sprite sp = Art.Icon(icon, "icone");
            if (sp == null) return null;
            var root = new GameObject(icon).transform;
            root.SetParent(parent, false);
            root.localPosition = at;
            Art.NewSprite(root, "Arte", sp, Color.white, order, Vector2.zero, Vector2.one * size);
            Lux(u, root, off);
            return root;
        }

        /// <summary>Piso de oficina (luxo): ladrilhos limpos de 1 m com junta dourada sobre a pedra (x 0-9, y 0-12); o assoalho da frente fica.</summary>
        Transform BuildTiles()
        {
            Vector2 half = new Vector2(Balance.WorkshopW, DeckY) * 0.5f;
            Transform root = Group("PisoLadrilhos", new V2(half.x, half.y));
            Tiled("Ladrilhos", Art.Tiles(TileA, TileB, TileJoint), Color.white, LuxOrder, -half, half, false, root);
            return root;
        }

        /// <summary>Fachada nobre: soleira dourada no chao, 2 postes (luz-chave da esquerda, ART_BIBLE s7) e varal de bandeirolas nas cores dos itens.</summary>
        Transform BuildFacade()
        {
            float ws = Balance.WorkshopW;
            Transform root = Group("FachadaNobre", new V2(ws / 2f, FacadeY));
            int d = Depth(FacadeY);   // 295..298: atras da fila (300), na frente da rua de cima
            Art.NewSprite(root, "Soleira", Art.Square(), Art.Gold, 1, new Vector2(-0.15f, 0f), new Vector2(ws + 0.3f, 0.16f));
            Art.NewSprite(root, "SoleiraLuz", Art.Square(), Art.Accent, 2, new Vector2(-0.15f, 0.06f), new Vector2(ws + 0.3f, 0.04f));
            float x0 = PostInset - ws / 2f, span = (ws - 2f * PostInset) / FacadeArcs;
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? x0 : -x0;
                Art.NewSprite(root, "PosteSombra", Art.Disc(), new Color(0f, 0f, 0f, 0.25f), 1, new Vector2(x, 0f), new Vector2(0.45f, 0.16f));
                Art.NewSprite(root, "PosteBase", Art.Rounded(), Art.GoldDark, d, new Vector2(x, 0.04f), new Vector2(0.32f, 0.12f));
                Art.NewSprite(root, "Poste", Art.Square(), Art.Gold, d + 1, new Vector2(x, PostH / 2f), new Vector2(0.16f, PostH));
                Art.NewSprite(root, "PosteLuz", Art.Square(), Art.Accent, d + 2, new Vector2(x - 0.05f, PostH / 2f), new Vector2(0.04f, PostH));
                Art.NewSprite(root, "Remate", Art.Star(), Art.Accent, d + 3, new Vector2(x, PostH + 0.08f), Vector2.one * 0.3f);
            }
            int k = 0;
            for (int a = 0; a < FacadeArcs; a++)
            {
                float ax = x0 + a * span;
                for (int i = 0; i < FacadeSeg; i++, k++)
                {
                    Vector2 p = Rope(ax, span, i / (float)FacadeSeg), q = Rope(ax, span, (i + 1) / (float)FacadeSeg), m = (p + q) * 0.5f, dl = q - p;
                    Art.NewSprite(root, "Corda", Art.Square(), Art.GoldDark, d, m, new Vector2(dl.magnitude + 0.02f, 0.035f), Mathf.Atan2(dl.y, dl.x) * Mathf.Rad2Deg);
                    // Triangle girado 180: a base (0,4 da altura acima do centro) fica pendurada na corda, a ponta para baixo
                    Art.NewSprite(root, "Bandeirola", Art.Triangle(), Art.ItemColor[(int)Item.Sword + k % 4], d + 1, m - new Vector2(0f, 0.11f), new Vector2(0.22f, 0.28f), 180f);
                }
                if (a > 0) Art.NewSprite(root, "Roseta", Art.Disc(), Art.Gold, d + 3, new Vector2(ax, PostH), Vector2.one * 0.14f);
            }
            return root;
        }

        /// <summary>Ponto do varal: arco de `span` m a partir de `x`, preso no topo dos postes e cedendo `Sag` no meio.</summary>
        static Vector2 Rope(float x, float span, float t) => new Vector2(x + span * t, PostH - Sag * 4f * t * (1f - t));

        /// <summary>Joalheria real: tapete roxo com moldura e losangos dourados da bancada a loja, e 4 pedestais de gema.</summary>
        Transform BuildJewelryDecor()
        {
            V2 a = _sim.JewelBench.Pos, b = _sim.JewelShop.Pos;   // (12; 6,5) -> (12; 11,5)
            float y0 = a.Y + 0.1f, y1 = b.Y - 0.1f, len = y1 - y0;
            var c = new V2(a.X, (y0 + y1) / 2f);
            Transform root = Group("JoalheriaReal", c);
            // no chao: tapete -1 (acima da rua -2), moldura e losangos 0, abaixo das sombras (1) e do estoque da loja (3)
            Art.NewSprite(root, "Tapete", Art.Square(), Carpet, -1, Vector2.zero, new Vector2(CarpetW, len));
            for (int s = -1; s <= 1; s += 2)
            {
                Art.NewSprite(root, "Moldura", Art.Square(), Art.Gold, 0, new Vector2(s * (CarpetW / 2f - 0.1f), 0f), new Vector2(0.05f, len - 0.15f));
                Art.NewSprite(root, "Moldura", Art.Square(), Art.Gold, 0, new Vector2(0f, s * (len / 2f - 0.1f)), new Vector2(CarpetW - 0.15f, 0.05f));
            }
            for (float y = 0.5f - len / 2f; y < len / 2f - 1f; y += 0.6f)   // para 0,4 m antes do estoque da loja (y 10,5-11): o item le limpo
                Art.NewSprite(root, "Losango", Art.Diamond(), Art.Accent, 0, new Vector2(0f, y), new Vector2(0.14f, 0.2f));
            foreach (V2 p in Pedestals) Pedestal(root, new Vector2(p.X - c.X, p.Y - c.Y), Depth(p.Y));
            return root;
        }

        /// <summary>Pedestal dourado com gema roxa (forma de losango, como a gema da joia), ordenado pelo pe.</summary>
        static void Pedestal(Transform root, Vector2 at, int d)
        {
            Art.NewSprite(root, "PedestalSombra", Art.Disc(), new Color(0f, 0f, 0f, 0.25f), 1, at, new Vector2(0.5f, 0.2f));
            Art.NewSprite(root, "PedestalBase", Art.Rounded(), Art.GoldDark, d, at + new Vector2(0f, 0.05f), new Vector2(0.4f, 0.12f));
            Art.NewSprite(root, "Coluna", Art.Square(), Art.Gold, d + 1, at + new Vector2(0f, 0.3f), new Vector2(0.2f, 0.42f));
            Art.NewSprite(root, "Capitel", Art.Rounded(), Art.GoldDark, d + 2, at + new Vector2(0f, 0.53f), new Vector2(0.36f, 0.1f));
            Art.NewSprite(root, "Gema", Art.Diamond(), Art.ItemColor[(int)Item.Jewel], d + 3, at + new Vector2(0f, 0.76f), new Vector2(0.3f, 0.38f));
            Art.NewSprite(root, "Brilho", Art.Star(), Art.Accent, d + 4, at + new Vector2(-0.06f, 0.84f), Vector2.one * 0.12f);
        }

        CarrierV BuildCarrier(Carrier c, Color color, float size, int maxStack)
        {
            var v = new CarrierV { C = c, Root = new GameObject(c.Role < 0 ? "Jogador" : "Ajudante " + c.Role).transform };
            v.Root.SetParent(transform, false);
            v.Root.localPosition = W(c.Pos);
            float stackY = size * 0.5f;
            int order = 8;
            string artName = c.Role < 0 ? "ferreiro" : "ajudante";
            if (SpriteSheet.TryGet(artName, out SpriteSheet sh))
            {
                sh.Scale = CharScale(artName);
                Art.NewSprite(v.Root, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.3f), 5, Vector2.zero, new Vector2(0.55f, 0.22f));
                Color tint = c.Role < 0 ? Color.white : Color.Lerp(Color.white, Art.ItemColor[(int)RoleItem(c.Role)], 0.3f);
                v.Body = new Body { Sheet = sh, R = Art.NewSprite(v.Root, "Corpo", null, tint, 0, Vector2.zero, Vector2.one) };
                stackY = HeadY;
                order = StackOrder;
            }
            else
            {
                Art.NewSprite(v.Root, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.3f), 5, new Vector2(0.04f, -0.08f), Vector2.one * size);
                Art.NewSprite(v.Root, "Corpo", Art.Disc(), color, 6, Vector2.zero, Vector2.one * size);
                Art.NewSprite(v.Root, "Cabeca", Art.Disc(), Art.Bg, 7, new Vector2(0f, 0.1f), Vector2.one * size * 0.42f);
                if (c.Role >= 0) Art.NewSprite(v.Root, "Papel", Art.Ring(), Art.ItemColor[(int)RoleItem(c.Role)], 7, Vector2.zero, Vector2.one * size);
            }
            v.StackY = stackY;
            v.Stack = new SpriteRenderer[maxStack];
            for (int i = 0; i < maxStack; i++)
            {
                v.Stack[i] = Art.NewSprite(v.Root, "Item", null, Color.white, order + i, Vector2.zero, Vector2.one);   // o de cima cobre o de baixo
                v.Stack[i].enabled = false;
            }
            return v;
        }

        ClientV BuildClient()
        {
            var v = new ClientV { Root = new GameObject("Cliente").transform, TopY = 0.3f };
            v.Root.SetParent(transform, false);
            int order = 30;   // pedido sempre por cima das pilhas
            if (_clientSheets.Count > 0)
            {
                Art.NewSprite(v.Root, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.3f), 5, Vector2.zero, new Vector2(0.5f, 0.2f));
                v.Body = new Body { R = Art.NewSprite(v.Root, "Corpo", null, Color.white, 0, Vector2.zero, Vector2.one) };
                v.TopY = ClientTop;
                order = BubbleOrder;
            }
            else
            {
                Art.NewSprite(v.Root, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.3f), 5, new Vector2(0.04f, -0.08f), Vector2.one * 0.5f);
                Art.NewSprite(v.Root, "Corpo", Art.Disc(), Art.Client, 6, Vector2.zero, Vector2.one * 0.5f);
                Art.NewSprite(v.Root, "Cabeca", Art.Disc(), Art.Bg, 7, new Vector2(0f, 0.08f), Vector2.one * 0.2f);
            }
            // fila: mini-icone do pedido acima da cabeca e a paciencia logo acima dele; o 1o da fila troca os dois pelo balao grande
            v.Want = Art.NewSprite(v.Root, "Pedido", null, Color.white, order, new Vector2(0f, v.TopY + MiniY), Vector2.one);
            v.Patience = Art.NewSprite(v.Root, "Paciencia", Art.Square(), Art.Good, order + 1, new Vector2(0f, v.TopY + PatY), new Vector2(PatW, 0.06f));
            // coroa do VIP: acima do rabicho do balao (BubbleOrder + 17); sem a folha, estrela de ouro
            v.Crown = Art.NewSprite(v.Root, "Coroa", _crown != null ? _crown : Art.Star(), _crown != null ? Color.white : Art.Gold, BubbleOrder + 18,
                new Vector2(0f, v.TopY + CrownY), Vector2.one * (_crown != null ? CrownS * 1.1f : CrownS * 0.8f));
            v.Crown.enabled = false;
            v.Tag = Art.Outlined(Art.FreeText(_labels, "Pacote", 30, new Vector2(140f, 40f)), 2f);
            v.Tag.fontStyle = FontStyle.Bold; v.Tag.color = Art.Accent; v.Tag.enabled = false;
            return v;
        }

        /// <summary>Balao grande (BENCHMARK_VISUAL P0-2), escondido ate a fila ter alguem; RefreshQueue o prende no 1o.</summary>
        BubbleV BuildBubble(string name)
        {
            var b = new BubbleV { Root = Group(name, new V2(0f, 0f)) };
            b.Track = Art.NewSprite(b.Root, "Trilho", Art.BalloonRing(1f), Art.ComAlfa(Art.Bg, 0.85f), BubbleOrder + 10, Vector2.zero, RingSize);
            b.Ring = Art.NewSprite(b.Root, "Paciencia", Art.BalloonRing(1f), Art.Good, BubbleOrder + 11, Vector2.zero, RingSize);
            // VIP: moldura de ouro 0,12 m maior que o corpo (o trilho fica escondido sob o anel cheio de paciencia)
            b.Frame = Art.NewSprite(b.Root, "Moldura", Art.Rounded(), Art.Gold, BubbleOrder + 12, Vector2.zero, BalloonSize + Vector2.one * 0.12f);
            b.Frame.enabled = false;
            b.Body = Art.NewSprite(b.Root, "Balao", Art.Rounded(), Art.Bubble, BubbleOrder + 13, Vector2.zero, BalloonSize);
            b.Tail = Art.NewSprite(b.Root, "Rabicho", Art.Triangle(), Art.Bubble, BubbleOrder + 13, new Vector2(-BubbleDX, TailY), TailSize, 180f);
            // VIP: o pacote numa pilula so de 40 px, "xN" #2A1E14 no #FFD34D, na borda de baixo a esquerda do balao (v0.6d: era o selo
            // dourado "x3" do preco + o "xN" solto; 2 numeros no balao, e o jogador nao sabia qual era a quantidade)
            b.Pill = Art.Node(_labels, name + "Pacote", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            b.Pill.sizeDelta = new Vector2(80f, 40f);
            Image pill = b.Pill.gameObject.AddComponent<Image>();
            pill.sprite = Art.Box(); pill.type = Image.Type.Sliced; pill.color = PillC; pill.raycastTarget = false;
            b.Pack = Art.NewText(b.Pill, "Texto", 30, Vector2.zero, Vector2.one);
            b.Pack.fontStyle = FontStyle.Bold; b.Pack.color = Edge; b.Pack.horizontalOverflow = HorizontalWrapMode.Overflow;
            b.Pill.gameObject.SetActive(false);
            b.Icon = Art.NewSprite(b.Root, "Pedido", null, Color.white, BubbleOrder + 14, Vector2.zero, Vector2.one);
            // rosto bravo de ~0,5 m (48 px) no canto de cima a direita, abaixo de 20% de paciencia (P0-2): contorno, rosto, tracos
            b.Angry = Group(name + "Bravo", new V2(0f, 0f));
            b.Angry.SetParent(b.Root, false);
            b.Angry.localPosition = new Vector3(BalloonSize.x / 2f, BalloonSize.y / 2f, 0f);
            Art.NewSprite(b.Angry, "Contorno", Art.Disc(), Art.ComAlfa(Art.Bg, 0.9f), BubbleOrder + 15, Vector2.zero, Vector2.one * 0.56f);
            Art.NewSprite(b.Angry, "Rosto", Art.Disc(), AngryC, BubbleOrder + 16, Vector2.zero, Vector2.one * 0.5f);
            Art.NewSprite(b.Angry, "Tracos", Art.AngryFace(), Art.Hex(0x3A1208), BubbleOrder + 17, Vector2.zero, Vector2.one * 0.5f);
            b.Angry.gameObject.SetActive(false);
            b.Root.gameObject.SetActive(false);
            return b;
        }

        /// <summary>
        /// Balao no 1o da fila: icone de 0,9 m, anel verde -> amarelo -> vermelho e tremida de +-3 graus a 6 Hz abaixo de 20% (com o
        /// rosto bravo). Venda (Sold): estoura 1 -> 1,25 -> 0 em 0,15 s e volta com sobra no proximo da fila.
        /// </summary>
        void PlaceBubble(BubbleV b, ClientV v, Client c, float f, float dt)
        {
            b.Root.localPosition = OnScreen(v.Root.localPosition + new Vector3(BubbleDX, v.TopY + BubbleUp, 0f));
            b.Root.localRotation = Quaternion.Euler(0f, 0f, f < 0.2f ? 3f * Mathf.Sin(Time.time * 6f * 2f * Mathf.PI) : 0f);
            float s = 1f;
            if (b.PopT >= 0f)
            {
                b.PopT += dt;
                float k = b.PopT / 0.15f;
                s = k < 0.4f ? Mathf.Lerp(1f, 1.25f, k / 0.4f) : k < 1f ? Mathf.Lerp(1.25f, 0f, (k - 0.4f) / 0.6f) : PopCurve((b.PopT - 0.15f) / (BubblePop - 0.15f));
                if (b.PopT >= BubblePop) { b.PopT = -1f; s = 1f; }
            }
            b.Root.localScale = Vector3.one * s;
            Art.PaintItem(b.Icon, c.Want, true, BubbleIcon);
            // VIP (v0.5c): trilho de ouro, corpo creme e a pilula "xN" do pacote, que cai a cada unidade entregue
            bool vip = c.Vip, pill = vip && s > 0.5f;
            b.Track.color = vip ? Art.GoldDark : Art.ComAlfa(Art.Bg, 0.85f);
            b.Frame.enabled = vip;
            b.Body.color = b.Tail.color = vip ? VipCream : Art.Bubble;
            if (b.Pill.gameObject.activeSelf != pill) b.Pill.gameObject.SetActive(pill);
            if (pill)
            {
                if (b.PackN != c.Pack)   // string nova so quando o pacote muda
                {
                    b.PackN = c.Pack; b.Pack.text = "×" + c.Pack;
                    b.Pill.sizeDelta = new Vector2(b.Pack.preferredWidth + 28f, 40f);
                }
                PlaceRect(b.Pill, b.Root.localPosition + new Vector3(-0.5f, -0.52f, 0f));   // sobre a borda, longe do rabicho e da coroa (a direita)
            }
            b.Ring.sprite = Art.BalloonRing(f);
            b.Ring.color = f > 0.5f ? Color.Lerp(Art.Gold, Art.Good, f * 2f - 1f) : Color.Lerp(Art.Bad, Art.Gold, f * 2f);
            bool angry = f < 0.2f;
            if (b.Angry.gameObject.activeSelf != angry) b.Angry.gameObject.SetActive(angry);
            if (angry) b.Angry.localScale = Vector3.one * (1f + 0.1f * Mathf.Abs(Mathf.Sin(Time.time * 7f)));
        }

        /// <summary>
        /// v0.6d (revisao de UX): centro do balao grande preso na tela pelo anel (1,53 x 1,33 m): topo >= 24 px abaixo da HUD e x entre 48 e
        /// 1032 (px na largura 1080). Com 8 vagas ele encostava na faixa de cima; com -cam menor saia pela borda.
        /// </summary>
        Vector3 OnScreen(Vector3 w)
        {
            float k = Screen.width / 1080f, ppm = Screen.height / (2f * _cam.orthographicSize);
            float hw = RingSize.x * 0.5f * ppm, hh = RingSize.y * 0.5f * ppm;
            Vector3 sp = _cam.WorldToScreenPoint(w);
            float x = Mathf.Clamp(sp.x, 48f * k + hw, 1032f * k - hw), y = Mathf.Min(sp.y, HudBottomPx - 24f * k - hh);
            if (x == sp.x && y == sp.y) return w;
            Vector3 p = _cam.ScreenToWorldPoint(new Vector3(x, y, sp.z));
            return new Vector3(p.x, p.y, w.z);
        }

        /// <summary>Mini-icone so de quem esta dentro da tela (v0.6d).</summary>
        bool InView(Vector3 p)
        {
            Vector3 vp = _cam.WorldToViewportPoint(p);
            return vp.x > 0.03f && vp.x < 0.97f && vp.y > 0f && vp.y < 1f;
        }

        /// <summary>Pop de quem nasce: 0 -> `peak` (60% do tempo, ease-out) -> 1.</summary>
        static float PopCurve(float t, float peak = 1.1f)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.6f) { float k = t / 0.6f; return peak * (1f - (1f - k) * (1f - k)); }
            return Mathf.Lerp(peak, 1f, (t - 0.6f) / 0.4f);
        }

        V2 Slot(bool jewel, int i) => jewel ? _sim.JewelSlot(i) : _sim.ClientSlot(i);
        List<Client> QueueOf(bool jewel) => jewel ? _sim.JewelQueue : _sim.Queue;

        SpriteSheet NextClientSheet()
        {
            if (_artBag.Count == 0)
            {
                for (int i = 0; i < _clientSheets.Count; i++) _artBag.Add(i);
                for (int i = _artBag.Count - 1; i > 0; i--) { int j = _artRng.Next(i + 1); (_artBag[i], _artBag[j]) = (_artBag[j], _artBag[i]); }
                int last = _artBag.Count - 1;   // tira do fim: o proximo nao pode ser o mesmo que acabou de sair
                if (last > 0 && _artBag[last] == _lastArt) (_artBag[0], _artBag[last]) = (_artBag[last], _artBag[0]);
            }
            _lastArt = _artBag[_artBag.Count - 1];
            _artBag.RemoveAt(_artBag.Count - 1);
            return _clientSheets[_lastArt];
        }

        /// <summary>View livre (sem cliente e escondida) ou uma nova. Id local (o Core nao tem) alterna o elenco; loja de joias = nobre.</summary>
        ClientV FreeClient(bool jewel)
        {
            ClientV v = null;
            foreach (ClientV x in _clients) if (x.C == null && !x.Root.gameObject.activeSelf) { v = x; break; }
            if (v == null) _clients.Add(v = BuildClient());
            v.Jewel = jewel;
            v.Id = _clientSeq++;
            if (v.Body != null) { v.Body.Sheet = jewel && _nobre != null ? _nobre : NextClientSheet(); v.Body.Clip = null; }
            v.Crown.enabled = v.Tag.enabled = v.Waiting = false;
            v.Root.gameObject.SetActive(true);
            return v;
        }

        /// <summary>Liga um Client novo da fila a uma view livre.</summary>
        ClientV BindClient(Client c, int slot, bool jewel)
        {
            if (c.Vip && _vipWait != null)   // o VIP que esperava ao lado da fila cheia entra andando de onde estava
            {
                ClientV w = _vipWait;
                _vipWait = null;
                w.Waiting = false;
                w.C = c;
                w.Want.enabled = w.Patience.enabled = true;
                return w;
            }
            ClientV v = FreeClient(jewel);
            v.C = c;
            v.Want.enabled = v.Patience.enabled = true;
            v.Root.localPosition = W(Slot(jewel, slot)) + new Vector3(0f, 1.5f, 0f);   // chega pela rua de cima
            return v;
        }

        /// <summary>
        /// Quem chega com a fila cheia (Ev.ClientLeft B = 2, na vaga QueueCap; FASE7 s5): desce da rua de cima ate a faixa atras da
        /// fila e vai embora pela esquerda, como quem cansou. ponytail: sem folha de personagem (procedural) nao aparece, como antes.
        /// </summary>
        public void TurnedAway(V2 at, bool jewel)
        {
            if (_clientSheets.Count == 0) return;
            ClientV v = FreeClient(jewel);
            v.C = null;
            v.Want.enabled = v.Patience.enabled = false;
            float laneY = Mathf.Max(W(Slot(jewel, 0)).y, Balance.WorldH) + 0.7f;
            v.Root.localPosition = new Vector3(at.X, laneY + 0.9f, 0f);
            v.Lane = new Vector3(at.X, laneY, 0f);
            v.Exit = new Vector3(-1.6f, laneY, 0f);
            v.Served = false;
            v.Phase = 0;
            v.LeaveT = 0f;
        }

        // ------------------------------------------------------------------ quadro

        public void Refresh(float dt)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
            RefreshArea();
            for (int i = 0; i < _glows.Count; i++)   // tochas tremulam: 2 senos fora de fase por tocha
                _glows[i].color = Art.ComAlfa(GlowColor, GlowAlpha + 0.05f * Mathf.Sin(Time.time * 9f + i * 2.1f) + 0.03f * Mathf.Sin(Time.time * 23f + i));
            _esteira.Refresh();   // antes das estacoes: a pilha da Bigorna desconta o lingote em cima da faixa ja neste quadro
            for (int i = 0; i < _stations.Count; i++)
            {
                if (_stations[i].Receita != _stations[i].S.Recipe) _stations[i] = Refaz(_stations[i]);   // C-CORE-06: trocou a receita
                RefreshStation(_stations[i], pulse, dt);
            }
            RefreshStand(dt);
            foreach (PadV v in _pads) RefreshPad(v, dt);
            foreach (ChestV v in _chests) RefreshChest(v);
            foreach (LuxV v in _lux) RefreshLux(v, dt);

            if (_sim.UpgradesBought != _bought) { _bought = _sim.UpgradesBought; Cheer(); }   // comprou upgrade
            RefreshCarrier(_player, dt);
            RefreshMax(dt);
            RefreshRing(dt);
            while (_workers.Count < _sim.Workers.Count)
            {
                _workers.Add(BuildCarrier(_sim.Workers[_workers.Count], Art.Worker, 0.52f, Balance.WorkerCapUp + WorkerDefs.CargaMax));   // B-CORE-04: carga do nivel
                if (Time.timeSinceLevelLoad > 1f) Poof(_sim.Workers[_workers.Count - 1].Pos, 1f);   // contratou agora (o save carrega sem poeira)
            }
            foreach (CarrierV v in _workers) RefreshCarrier(v, dt);
            RefreshPostos(dt);

            RefreshClients(dt);

            TickFx(dt);
            RefreshArrow();
            for (int i = 0; i < _floaters.Count; i++)
            {
                Floater f = _floaters[i];
                if (!f.Live) continue;
                f.Age += dt;
                if (f.Age >= 1.1f) { f.Live = false; f.T.enabled = false; continue; }
                f.T.color = Art.ComAlfa(f.T.color, Mathf.Clamp01(1.4f - f.Age * 1.3f));
                PlaceLabel(f.T, f.World + new Vector3(0f, 0.5f + f.Age * 0.9f, 0f), Vector2.zero);
            }
        }

        /// <summary>O ferreiro comemora 1 vez (upgrade comprado, marco batido: ASSETS "Cheering"); andar cancela.</summary>
        public void Cheer() { if (_player.Body != null) _player.CheerT = _player.Body.Sheet.Length("Cheering"); }

        /// <summary>Rua lateral escura com rotulo ate o Corredor; loja de joias escurecida "fechada" entre o Corredor e a Joalheria (AREA2 s7).</summary>
        void RefreshArea()
        {
            if (!_sim.Distrito.Parede) return;   // C-CORE-01: sem rua lateral nem loja fechada (BuildRua)
            bool corridor = _sim.Has(Upgrade.SideCorridor), teaser = corridor && !_sim.Has(Upgrade.Jewelry) && _sim.JewelShop != null;
            _street.color = corridor ? StreetOpen : StreetShut;
            _streetWash.enabled = corridor;
            foreach ((GameObject shut, GameObject open) in _gates)   // fechado ate o Corredor, aberto depois (folhas contra o muro da rua)
            {
                if (shut != null && shut.activeSelf == corridor) shut.SetActive(!corridor);
                if (open != null && open.activeSelf != corridor) open.SetActive(corridor);
            }
            _streetLabel.enabled = !corridor;
            if (!corridor)
            {
                PlaceLabel(_streetLabel, new Vector3(Balance.WorkshopW + 0.6f, (Balance.SideWallOpenings[1] + Balance.SideWallOpenings[2]) / 2f, 0f), Vector2.zero);   // na parede entre a porta e o arco, nao em cima da porta
                // v0.6d: inteiro dentro de x <= 1032 (px na largura 1080); o "lateral" saia pela borda direita
                Vector2 at = _streetLabel.rectTransform.anchoredPosition;
                float max = _labels.rect.width * (1032f / 1080f - 0.5f) - _streetHalfW;
                if (at.x > max) _streetLabel.rectTransform.anchoredPosition = new Vector2(max, at.y);
            }
            if (_shopTeaser != null && _shopTeaser.transform.parent.gameObject.activeSelf != teaser) _shopTeaser.transform.parent.gameObject.SetActive(teaser);
            _shopLabel.enabled = teaser;
            if (teaser) PlaceLabel(_shopLabel, W(_sim.JewelShop.Pos) + new Vector3(0f, 1.2f, 0f), Vector2.zero);
        }

        /// <summary>Cada view segue um Client pela referencia: a fila desliza quando alguem sai e quem sai vai embora andando.</summary>
        void RefreshClients(float dt)
        {
            foreach (ClientV v in _clients)
            {
                if (v.C == null || QueueOf(v.Jewel).Contains(v.C)) continue;
                v.Served = v.C.Patience > 0f;   // vendido sai com paciencia sobrando; quem cansou sai com <= 0
                v.C = null;
                if (v.Body == null) { v.Root.gameObject.SetActive(false); continue; }   // procedural: some na hora, como antes
                v.Want.enabled = v.Patience.enabled = v.Tag.enabled = false;   // a coroa vai embora com ele
                Vector3 p = v.Root.localPosition;
                float laneY = Mathf.Max(W(Slot(v.Jewel, 0)).y, Balance.WorldH) + 0.7f;   // faixa da rua de cima, atras das duas filas
                v.Lane = new Vector3(p.x + (v.Id % 3 - 1) * 0.3f, laneY, 0f);
                v.Exit = new Vector3(v.Served ? Balance.WorldW + 1.6f : -1.6f, laneY, 0f);   // fora da camera (margem 1,2 m)
                v.Phase = 0;
                v.LeaveT = 0f;
            }
            RefreshQueue(false, dt);   // antes do RefreshVipWaiting: o BindClient tem de achar o _vipWait para o VIP entrar andando de onde esperava
            RefreshQueue(true, dt);
            RefreshVipWaiting(dt);
            foreach (ClientV v in _clients) if (v.C == null && !v.Waiting && v.Body != null && v.Root.gameObject.activeSelf) RefreshLeaving(v, dt);
        }

        /// <summary>
        /// VIP sorteado/chamado com a fila cheia (Sim.VipActive sem VIP na Queue): desce da rua de cima e espera de coroa 0,95 m a direita
        /// da ultima vaga, olhando em volta, com "VIP" em cima; quando abre vaga, o BindClient o leva andando. ponytail: sem folha de
        /// personagem (procedural) ele nao aparece esperando, so na vaga.
        /// </summary>
        void RefreshVipWaiting(float dt)
        {
            bool pending = _sim.VipActive && !_sim.Queue.Exists(c => c.Vip);
            if (pending && _vipWait == null && _clientSheets.Count > 0)
            {
                _vipWait = FreeClient(false);
                _vipWait.C = null; _vipWait.Waiting = true;
                _vipWait.Want.enabled = _vipWait.Patience.enabled = false;
                _vipWait.Crown.enabled = true;
                _vipWait.Root.localPosition = VipWaitSpot() + new Vector3(0f, 1.5f, 0f);
            }
            if (_vipWait == null) return;
            if (!pending) { _vipWait.Root.gameObject.SetActive(false); _vipWait.Waiting = false; _vipWait.Tag.enabled = false; _vipWait = null; return; }
            ClientV v = _vipWait;
            Vector3 before = v.Root.localPosition;
            v.Root.localPosition = Vector3.MoveTowards(before, VipWaitSpot(), 4f * dt);
            bool moving = v.Root.localPosition != before;
            v.Body.Tick(moving ? "Walking" : "LookingAround", dt, moving ? (Vector2)(v.Root.localPosition - before) : Vector2.down, true);
            v.Tag.enabled = true; v.Tag.text = Loc.T("hud.vip"); v.Tag.fontSize = 30;
            PlaceLabel(v.Tag, v.Root.localPosition + new Vector3(0f, v.TopY + 0.4f, 0f), Vector2.zero);
        }

        Vector3 VipWaitSpot() => W(Slot(false, _sim.QueueCap - 1)) + VipWaitOff;

        /// <summary>
        /// Uma fila (balcao ou loja de joias): mesma maquina de estados, so muda a lista, os slots e o elenco. O 1o leva o balao
        /// grande; os outros, mini-icone e paciencia. Vagas que se recentram (balcao evolutivo) = todos deslizam ate a vaga nova.
        /// </summary>
        void RefreshQueue(bool jewel, float dt)
        {
            List<Client> q = QueueOf(jewel);
            BubbleV b = jewel ? _jewelBubble : _bubble;
            if (b.Root.gameObject.activeSelf != q.Count > 0)
            {
                b.Root.gameObject.SetActive(q.Count > 0);
                b.PopT = q.Count > 0 ? 0.15f : -1f;   // 1o cliente chegou: o balao entra com pop (pula a fase de estouro)
            }
            if (q.Count == 0 && b.Pill.gameObject.activeSelf) b.Pill.gameObject.SetActive(false);   // a pilula do VIP vive na HUD: nao fica solta quando o balao some
            for (int i = 0; i < q.Count; i++)
            {
                Client c = q[i];
                ClientV v = null;
                foreach (ClientV x in _clients) if (x.C == c) { v = x; break; }
                if (v == null) v = BindClient(c, i, jewel);
                Vector3 before = v.Root.localPosition;
                v.Root.localPosition = Vector3.MoveTowards(before, W(Slot(jewel, i)), 4f * dt);
                float f = Mathf.Clamp01(c.Patience / c.MaxPatience);
                // v0.6d: mini-icone e paciencia so do 2o ao 4o da fila e dentro da tela (com 8 vagas eram 7 minis em volta do balao)
                bool mini = i > 0 && i < 4 && InView(v.Root.localPosition);
                v.Want.enabled = v.Patience.enabled = mini;
                v.Crown.enabled = c.Vip;
                v.Tag.enabled = c.Vip && mini;
                if (i == 0) PlaceBubble(b, v, c, f, dt);
                else if (mini)
                {
                    float up = c.Vip ? VipMiniUp : 0f;   // a coroa fica na cabeca, o pedido sobe
                    Art.PaintItem(v.Want, c.Want, true, MiniIcon);
                    v.Want.transform.localPosition = new Vector3(0f, v.TopY + MiniY + up, 0f);
                    v.Patience.transform.localScale = new Vector3(PatW * f, 0.06f, 1f);
                    v.Patience.transform.localPosition = new Vector3(PatW * (f - 1f) / 2f, v.TopY + PatY + up, 0f);
                    v.Patience.color = Color.Lerp(Art.Bad, Art.Good, f);
                    if (c.Vip)
                    {
                        v.Tag.text = "×" + c.Pack; v.Tag.fontSize = 30;
                        PlaceLabel(v.Tag, v.Root.localPosition + new Vector3(0.36f, v.TopY + MiniY + up, 0f), Vector2.zero);
                    }
                }
                if (v.Body == null) continue;
                bool moving = v.Root.localPosition != before;
                string clip = moving ? "Walking" : f < 0.2f ? "Angry" : f < 0.5f ? "LookingAround" : "Idle";
                v.Body.Tick(clip, dt, moving ? (Vector2)(v.Root.localPosition - before) : Vector2.down, true);   // esperando: de frente para o balcao
            }
        }

        /// <summary>Saida com arte: anda ate a rua; atendido agradece e acena (1 vez cada) e sai pela direita, quem cansou sai pela esquerda.</summary>
        void RefreshLeaving(ClientV v, float dt)
        {
            if (v.Phase == 1 || v.Phase == 2)
            {
                string clip = v.Phase == 1 ? "Thankful" : "Waving";
                v.LeaveT += dt;
                v.Body.Tick(clip, dt, Vector2.down);
                if (v.LeaveT >= Mathf.Min(v.Body.Sheet.Length(clip), OneShotMax)) { v.Phase++; v.LeaveT = 0f; }
                return;
            }
            Vector3 before = v.Root.localPosition, target = v.Phase == 0 ? v.Lane : v.Exit;
            v.Root.localPosition = Vector3.MoveTowards(before, target, LeaveSpeed * dt);
            v.Body.Tick("Walking", dt, v.Root.localPosition - before);
            if (v.Root.localPosition != target) return;
            if (v.Phase == 0) v.Phase = v.Served ? 1 : 3;
            else v.Root.gameObject.SetActive(false);
        }

        void RefreshStation(StationV v, float pulse, float dt)
        {
            Station s = v.S;
            if (v.Root.gameObject.activeSelf != s.Unlocked)
            {
                v.Root.gameObject.SetActive(s.Unlocked);
                if (s.Unlocked) { v.PopT = 0f; Poof(s.Pos, 1.6f); }   // comprou agora: nasce com pop e poeira (o save liga no Build, sem pop)
            }
            if (!s.Unlocked) { v.Label.enabled = false; return; }
            if (v.Faixas != null)   // C-CORE-07: "Faixa 1: 3/10 kits do batedor · 2:31", 0,32 m entre linhas, de cima para baixo
                for (int k = 0; k < v.Faixas.Length; k++)
                {
                    v.Faixas[k].enabled = k < _sim.FaixasAbertas;
                    if (!v.Faixas[k].enabled) continue;
                    v.Faixas[k].text = Textos.Faixa(_sim, k);
                    v.Faixas[k].color = _sim.Faixas[k].Produto >= 0 ? Art.Ink : Art.Accent;
                    PlaceLabel(v.Faixas[k], W(s.Pos) + new Vector3(0f, -0.9f - 0.32f * k, 0f), Vector2.zero);
                }
            if (v.PopT >= 0f)
            {
                v.PopT += dt;
                v.Root.localScale = Vector3.one * PopCurve(v.PopT / StationPop);
                if (v.PopT >= StationPop) { v.PopT = -1f; v.Root.localScale = Vector3.one; }
            }
            // nome so perto do jogador ou no 1o minuto de jogo (tutorial); o estande ja le como loja e o rotulo cairia na fila
            float near = Mathf.Clamp01((NearLabel - V2.Dist(_sim.Player.Pos, s.Pos)) / LabelFade);
            float la = _sim.Time < TutorialLabels ? 1f : near;
            v.Label.enabled = !v.Stand && la > 0.01f;
            if (v.Label.enabled)
            {
                v.Label.color = Art.ComAlfa(Art.Ink, 0.95f * la);
                // quem produz: nome embaixo da barra (em cima batia no preco da placa da Esteira, a 2 m da Bigorna); balcao: abaixo do varal
                float labelY = s.Produces ? BarY - 0.3f : s == _sim.Counter ? 0.9f : v.Baked ? 1.35f : 0.95f;
                if (v.QBarBg != null && _sim.QualityRevealed) labelY = NameQY;   // B-CORE-03: o "Fina em 7" fica entre a barra e o nome
                PlaceLabel(v.Label, W(s.Pos) + new Vector3(0f, labelY, 0f), Vector2.zero);
            }
            if (s.Produces)
            {
                int inn = s == _sim.EsteiraPara ? s.In - _esteira.Levando : s.In;   // B-ART-08: o lingote da esteira so entra na pilha quando chega
                for (int i = 0; i < v.InPile.Length; i++) v.InPile[i].enabled = i < inn;
                bool blocked = s.Blocked || (s.Out >= s.OutCap);
                int pop = -1; float ps = 1f;   // v0.6a: o item que acabou de sair (Ev.Crafted) da pop 0 -> 1,15 -> 1
                bool popping = v.ItemPopT >= 0f;   // revisao da v0.6: escala so durante a pop (e no quadro em que ela acaba volta a 1)
                if (popping)
                {
                    v.ItemPopT += dt;
                    pop = s.Out - 1;
                    ps = PopCurve(v.ItemPopT / ItemPop, 1.15f);
                    if (v.ItemPopT >= ItemPop) v.ItemPopT = -1f;
                }
                for (int i = 0; i < v.OutPile.Length; i++)
                {
                    v.OutPile[i].enabled = i < s.Out;
                    v.OutPile[i].color = blocked ? Color.Lerp(Art.ItemTint(s.OutItem), Art.Bad, 0.5f * pulse) : Art.ItemTint(s.OutItem);
                    if (popping) v.OutPile[i].transform.localScale = i == pop ? v.PileScale * ps : v.PileScale;
                    // B-CORE-03: a melhor fica no topo da pilha (e' a que sai primeiro)
                    if (v.OutGlint != null) Glint(v.OutGlint[i], i < s.Out ? QualidadeView.Nivel(s.Out - 1 - i, s.OutQ) : Quality.Normal, i);
                }
                if (v.QBarBg != null) RefreshQuality(v, la, dt);
                if (v.Forno != null) v.Forno.Refresh(dt, _sim.BoostLeft > 0f, pulse);   // A-ART-06: arte, luz, boca e efeitos da fornalha
                else v.Bancada.Refresh(dt, false, pulse);   // A-ART-07: martelo, faiscas e som no compasso da peca
                float p = s.Busy ? s.Progress : 0f;
                float fh = BarH - 0.05f, fw = Mathf.Lerp(fh, BarW - 0.05f, p);
                v.BarFill.enabled = p > 0.01f;
                v.BarFill.size = new Vector2(fw / fh, 1f);
                v.BarFill.transform.localPosition = new Vector3(-(BarW - 0.05f) / 2f + fw / 2f, BarY, 0f);
                bool starving = !s.Busy && s.In < s.Need;
                v.BarBg.color = starving ? Color.Lerp(Art.ComAlfa(Art.Bg, 0.85f), Art.Bad, 0.35f * pulse) : Art.ComAlfa(Art.Bg, 0.85f);
                if (v.Forno == null) v.Base.color = v.Baked ? (s.Busy ? Color.white : Dimmed) : Art.ComAlfa(Art.StationColor(s), s.Busy ? 1f : 0.8f);
            }
            if (s.Kind == Kind.Counter && v.Stock != null)
                for (int p = 0; p < v.StockItems.Length; p++)
                    for (int i = 0; i < 6; i++)
                    {
                        int it = (int)v.StockItems[p];
                        v.Stock[p * 6 + i].enabled = i < _sim.Stock[it];
                        Glint(v.StockGlint[p * 6 + i], i < _sim.Stock[it] ? QualidadeView.Nivel(i, _sim.StockQ[it]) : Quality.Normal, i);   // B-CORE-03
                    }
        }

        /// <summary>
        /// B-CORE-03 (GDD §19), da revelacao (Ferramentas) em diante: barra do acumulador (prata enchendo ate a proxima Fina; golpe valendo =
        /// ouro e cheia) e, perto do ferreiro, "Fina em 7" (4x/s) abaixo do nome, que o toque abre no QualityBreakdown (Game).
        /// </summary>
        void RefreshQuality(StationV v, float la, float dt)
        {
            Station s = v.S;
            bool rev = _sim.QualityRevealed;
            v.QBarBg.enabled = rev;
            float f = s.Strike ? 1f : s.QAcc / 100f, fw = Mathf.Lerp(QBarH, BarW, f);
            v.QBarFill.enabled = rev && f > 0.01f;
            if (v.QBarFill.enabled)
            {
                v.QBarFill.size = new Vector2(fw / QBarH, 1f);
                v.QBarFill.transform.localPosition = new Vector3(-BarW / 2f + fw / 2f, QBarY, 0f);
                v.QBarFill.color = s.Strike ? SparkA : Silver;
            }
            v.QLabel.enabled = rev && la > 0.01f;
            if (!v.QLabel.enabled) { v.QT = 0f; return; }
            if ((v.QT -= dt) <= 0f) { v.QT = 0.25f; v.QLabel.text = Textos.Barra(_sim, s); }
            v.QLabel.color = Art.ComAlfa(s.Strike ? SparkA : Silver, la);
            PlaceLabel(v.QLabel, W(s.Pos) + new Vector3(0f, QLabelY, 0f), Vector2.zero);
        }

        /// <summary>B-CORE-03: anel do golpe acima da bancada onde o ferreiro pode golpear (Sim.StrikeStation); a volta recomeca quando ele aparece
        /// ou muda de bancada.</summary>
        void RefreshRing(float dt)
        {
            Station s = _sim.StrikeStation;
            if (s != _ringAt) { _ringAt = s; _ringT = 0f; }
            bool on = s != null;
            if (_ring.gameObject.activeSelf != on) _ring.gameObject.SetActive(on);
            if (!on) return;
            _ringT += dt;
            PlaceRect(_ring, W(s.Pos) + new Vector3(0f, RingY, 0f));
            _ringOut.rectTransform.localScale = Vector3.one * QualidadeView.Escala(_ringT);
            bool hit = AcertoAgora;
            _ringOut.color = hit ? Art.Good : Color.white;
            _ringTarget.color = hit ? SparkA : Art.Gold;
        }

        /// <summary>O ponto de tela (px) cai no anel do golpe visivel.</summary>
        public bool NoAnel(Vector2 px) => _ring.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(_ring, px, null);

        /// <summary>Um toque agora acertaria a janela do anel (Tuning.GolpeJanela).</summary>
        public bool AcertoAgora => QualidadeView.Acerto(_ringT, _sim.T.GolpeJanela);

        /// <summary>C-CORE-07: faixa do patio de cotas cuja linha visivel contem o ponto de tela (px), ou -1.</summary>
        public int FaixaEm(Vector2 px)
        {
            foreach (StationV v in _stations)
                if (v.Faixas != null)
                    for (int k = 0; k < v.Faixas.Length; k++)
                        if (v.Faixas[k].enabled && RectTransformUtility.RectangleContainsScreenPoint(v.Faixas[k].rectTransform, px, null)) return k;
            return -1;
        }

        /// <summary>Estacao cuja "Fina em N" visivel contem o ponto de tela (px), ou null.</summary>
        public Station BarraEm(Vector2 px)
        {
            foreach (StationV v in _stations)
                if (v.QLabel != null && v.QLabel.enabled && RectTransformUtility.RectangleContainsScreenPoint(v.QLabel.rectTransform, px, null)) return v.S;
            return null;
        }

        /// <summary>Ev.Graded (Game): peca Fina ou Superior saiu na pilha da estacao: "Fina!" prata ou "Superior!" ouro e 5 brilhos na pilha.</summary>
        public void Graded(int station, Quality l)
        {
            Station s = _sim.Stations[station];
            bool sup = l == Quality.Superior;
            var at = new V2(s.Pos.X + 1.05f, s.Pos.Y - 0.2f);
            Float(at, Loc.T("fx.qualidade", Textos.Qualidade(l)), sup ? SparkA : Silver, sup ? 40 : 34);
            Vector3 c = W(at);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 0.4f + 0.3f;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.55f;
                Spawn(sup ? Art.Star() : Art.Sparkle(), sup ? SparkA : Silver, c, c + d * 0.6f, c + d, 0.45f, sup ? 0.3f : 0.24f, 0.05f, true, 180f);
            }
        }

        /// <summary>Ev.Crafted (Game): o item novo da pilha de saida da pop 0 -> 1,15 -> 1 em 0,2 s (P2-1); na bigorna/bancada, o golpe
        /// final do compasso com "+1" (A-ART-07).</summary>
        public void Crafted(int station)
        {
            foreach (StationV v in _stations)
                if (v.S.Index == station) { v.ItemPopT = 0f; v.Bancada?.Feito(); }
        }

        /// <summary>
        /// "MAX" (P1-5): o ferreiro esta numa boca de onde recolheria um tipo que ja esta no teto (o deposito, ou a saida com produto
        /// pronto). Fica enquanto ele insiste e some 0,8 s depois; o pop so roda quando estava apagado (nao pisca a cada quadro).
        /// </summary>
        void RefreshMax(float dt)
        {
            Carrier p = _sim.Player;
            Station s = _sim.StationAt(p.Pos, out bool outZone);
            bool full = s != null && (s.Kind == Kind.Deposit || (outZone && s.Out > 0))
                && p.Held[(int)(s.Kind == Kind.Deposit ? Item.Ore : s.OutItem)] >= p.Cap;
            if (full) { if (_maxT <= 0f) { _maxAge = 0f; Ajustes.Pulso(15, 60); } _maxT = MaxHold; }   // v0.6b: pulso so quando aparece
            else _maxT -= dt;
            _max.enabled = _maxT > 0f;
            if (!_max.enabled) return;
            _maxAge += dt;
            _max.rectTransform.localScale = Vector3.one * PopCurve(_maxAge / 0.25f, 1.25f);
            _max.color = Art.ComAlfa(MaxC, Mathf.Clamp01(_maxT / 0.25f));
            PlaceLabel(_max, _player.Root.localPosition + new Vector3(0f, _player.Top + 0.3f, 0f), Vector2.zero);
        }

        static string StandArt(Item it) => it == Item.Shield ? "balcao_escudos" : it == Item.Tool ? "balcao_ferramentas" : "balcao_meio";
        static string StandItemArt(Item it) => it == Item.Shield ? "item_escudo_em_pe" : it == Item.Tool ? "item_ferramenta_em_pe" : "item_espada_em_pe";

        /// <summary>Remonta o estande quando mudam as vagas ou abre uma linha (no maximo 7 vezes no jogo, com pop) e acende 1 item por unidade de estoque.</summary>
        void RefreshStand(float dt)
        {
            if (!_hasStand) return;
            int n = _sim.QueueCap, key = n * 4 + (_sim.LineUnlocked(Item.Shield) ? 1 : 0) + (_sim.LineUnlocked(Item.Tool) ? 2 : 0);
            if (key != _standKey) { _standPop = _standKey < 0 ? -1f : 0f; _standKey = key; BuildStand(n); }
            System.Array.Clear(_shown, 0, _shown.Length);
            foreach ((Item it, SpriteRenderer r, SpriteRenderer g) in _standStock)   // enche da esquerda para a direita; B-CORE-03: a melhor na frente
            {
                int k = _shown[(int)it]++;
                r.enabled = k < _sim.Stock[(int)it];
                Glint(g, r.enabled ? QualidadeView.Nivel(k, _sim.StockQ[(int)it]) : Quality.Normal, k);
            }
            if (_standPop < 0f) return;
            _standPop += dt;
            float t = Mathf.Clamp01(_standPop / PopTime);
            _stand.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, 1f - (1f - t) * (1f - t) * (1f - t));
            if (t >= 1f) _standPop = -1f;
        }

        /// <summary>Pontas + N modulos centrados no balcao (o modulo i fica sob a vaga i: Sim.ClientSlot), ordem pelo pe; itens em pe logo acima.</summary>
        void BuildStand(int n)
        {
            if (_stand != null) Destroy(_stand.gameObject);
            _stand = Group("Estande", _sim.Counter.Pos);
            _standStock.Clear();
            int order = Depth(_sim.Counter.Pos.Y);
            float x = -(Balance.SlotStep * n + 2f * Balance.CounterEnd) / 2f;   // borda esquerda do corpo: -Balance.CounterHalfX(n)
            StandPiece("balcao_ponta_esq", x + Balance.CounterEnd / 2f, order);
            x += Balance.CounterEnd + Balance.SlotStep / 2f;
            for (int i = 0; i < n; i++, x += Balance.SlotStep)
            {
                Item it = _sim.LineUnlocked(StandOrder[i]) ? StandOrder[i] : Item.Sword;
                float[] enc = StandPiece(StandArt(it), x, order).Slots;
                string em = StandItemArt(it);
                Sprite item = StaticArt(em, RealCell(em));
                for (int k = 0; k + 1 < enc.Length; k += 2)
                {
                    var at = new Vector2(x + enc[k], enc[k + 1]);
                    SpriteRenderer g = Art.NewSprite(_stand, "Brilho", Art.Sparkle(), Silver, order + 2, at + new Vector2(0.09f, 0.28f), Vector2.one * GlintFine);   // B-CORE-03
                    g.enabled = false;
                    _standStock.Add((it, Art.NewSprite(_stand, "Estoque", item, Color.white, order + 1, at, Vector2.one), g));
                }
            }
            StandPiece("balcao_ponta_dir", x - Balance.SlotStep / 2f + Balance.CounterEnd / 2f, order);
        }

        SpriteSheet StandPiece(string name, float x, int order)
        {
            SpriteSheet.TryGet(name, out SpriteSheet sh);   // _hasStand garante as 5 folhas
            Art.NewSprite(_stand, name, StaticArt(name, RealCell(name)), Color.white, order, new Vector2(x, 0f), Vector2.one);
            return sh;
        }

        /// <summary>
        /// Placa de obra: icone do que nasce ali (cinza quando nao da para pagar), enchimento de ouro pelo que ja foi pago, borda que
        /// pulsa quando da, preco com moeda dentro da placa e o nome so perto. Pagando: moedas voam do jogador para a placa (1 a cada
        /// 0,06 s). Concluiu (o upgrade da vez virou comprado): poeira; a estacao nasce com pop no RefreshStation.
        /// </summary>
        void RefreshPad(PadV v, float dt)
        {
            int u = v.P.Current(_sim);
            if (u != v.LastU)
            {
                if (v.LastU >= 0 && _sim.Bought[v.LastU]) Poof(v.P.Pos, 1.3f);
                v.LastU = u; v.LastPaid = v.P.Paid; v.LastRem = -1;
                if (u >= 0) { Sprite ic = PadIcon(u, out Color tint, out float fill); Fit(v.Icon, ic, PadIconBox / fill, new Vector2(0f, PadIconY)); v.Icon.color = tint; }
            }
            bool on = u >= 0;
            if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
            if (v.PriceBox.gameObject.activeSelf != on) v.PriceBox.gameObject.SetActive(on);
            if (!on) { v.Label.enabled = false; return; }
            long cost = _sim.T.Cost(u), remaining = cost - v.P.Paid;
            bool affordable = _sim.Gold >= remaining;
            float frac = cost > 0 ? v.P.Paid / (float)cost : 0f;
            v.Fill.transform.localScale = Vector3.one * (PadSize * 0.92f * Mathf.Sqrt(frac));
            // v0.6d: da para pagar = borda verde e a placa pulsa 1 -> 1,05 (escala so escrita enquanto pulsa); nao da = borda apagada
            if (affordable) { v.Root.localScale = Vector3.one * (1.025f + 0.025f * Mathf.Sin(Time.time * 6f)); v.Pulsing = true; }
            else if (v.Pulsing) { v.Root.localScale = Vector3.one; v.Pulsing = false; }
            v.Border.color = affordable ? PadOk : PadNo;
            v.Icon.color = Art.ComAlfa(v.Icon.color, affordable ? 1f : 0.75f);   // v0.6d: 0,55 sumia na placa creme
            // pagando: moedas do jogador para a placa
            if (v.P.Paid > v.LastPaid)
            {
                v.CoinT -= dt;
                if (v.CoinT <= 0f)
                {
                    v.CoinT = PadCoinEvery;
                    Vector3 a = W(_sim.Player.Pos) + new Vector3(0f, HeadY + 0.3f, 0f), b = W(v.P.Pos);
                    Spawn(_coin != null ? _coin : Art.Disc(), _coin != null ? Color.white : Art.Accent, a, (a + b) * 0.5f + new Vector3(0f, 1.6f, 0f), b, 0.32f, 0.44f, 0.26f, false);
                }
            }
            v.LastPaid = v.P.Paid;
            // preco: moeda + valor centrados no terco de baixo da placa; v0.6d: salmao #FF8A7A quando nao da, e so reescrito quando muda
            if (remaining != v.LastRem || affordable != v.LastOk)
            {
                v.LastRem = remaining; v.LastOk = affordable;
                v.Price.text = remaining.ToString();
                v.Price.color = affordable ? Color.white : PriceNo;
                v.PriceBox.sizeDelta = new Vector2(40f + 6f + v.Price.preferredWidth, 44f);
            }
            PlaceRect(v.PriceBox, W(v.P.Pos) + new Vector3(0f, PadPriceY, 0f));
            // nome: perto do jogador ou no 1o minuto (a seta da dica ja aponta a placa da vez)
            float la = _sim.Time < TutorialLabels ? 1f : Mathf.Clamp01((NearLabel - V2.Dist(_sim.Player.Pos, v.P.Pos)) / LabelFade);
            v.Label.enabled = la > 0.01f;
            if (!v.Label.enabled) return;
            v.Label.text = Textos.Nome(u);
            v.Label.color = affordable ? Art.ComAlfa(Art.Accent, la) : Art.ComAlfa(Art.Ink, 0.85f * la);
            PlaceLabel(v.Label, W(v.P.Pos) + new Vector3(0f, PadSize / 2f + 0.22f, 0f), Vector2.zero);
        }

        /// <summary>So o bau disponivel aparece; aberto some (as moedas e o "+ouro" saem do Ev.ChestOpened no Game).</summary>
        void RefreshChest(ChestV v)
        {
            bool on = v.C.State == 1;
            if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
            v.Label.enabled = on;
            if (!on) return;
            v.Root.localScale = Vector3.one * (1.04f + 0.04f * Mathf.Sin(Time.time * 4f));   // 1 <-> 1,08
            PlaceLabel(v.Label, W(v.C.Pos) + new Vector3(0f, ChestLabelY, 0f), Vector2.zero);
        }

        /// <summary>
        /// Flag virou (compra: Ev.Bought, cujo som "upgrade", "+nome!" e a comemoracao ja saem do Game/Refresh) = liga com pop
        /// 0,6 -> 1 em ease-out. Le a flag, nao o evento: vale igual para compra, -buy e save carregado.
        /// </summary>
        void RefreshLux(LuxV v, float dt)
        {
            bool on = On(v);
            if (v.Root.gameObject.activeSelf != on) { v.Root.gameObject.SetActive(on); v.PopT = on ? 0f : -1f; }
            if (v.PopT < 0f) return;
            v.PopT += dt;
            float t = Mathf.Clamp01(v.PopT / PopTime), e = 1f - (1f - t) * (1f - t) * (1f - t);
            v.Root.localScale = Vector3.one * Mathf.Lerp(PopFrom, 1f, e);
            if (t >= 1f) v.PopT = -1f;
        }

        void RefreshCarrier(CarrierV v, float dt)
        {
            Carrier c = v.C;
            if (v.Body == null)
            {
                v.Root.localPosition = W(c.Pos);
                float bob = c.Moving ? Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.06f : 0f;
                v.Root.localPosition += new Vector3(0f, bob, 0f);
            }
            else
            {
                Vector3 before = v.Root.localPosition;
                v.Root.localPosition = W(c.Pos);
                if (c.Moving) v.CheerT = 0f; else v.CheerT -= dt;
                // ajudante parado ha 0,5 s (fonte vazia ou nada a fazer) olha em volta: le como "esperando" o gargalo
                string clip = c.Moving ? "Walking" : v.CheerT > 0f ? "Cheering" : c.Role >= 0 && c.IdleT > 0.5f ? "LookingAround" : "Idle";
                if (v == _player && _packSheet != null)   // mesma animacao e quadro, outra folha; comprou agora = poeira (o save carrega sem)
                {
                    SpriteSheet want = _sim.Has(Upgrade.PlayerCapacity) ? _packSheet : _plainSheet;   // Has: a mochila e' do ferreiro em todo distrito (C-CORE-02)
                    if (v.Body.Sheet != want && want == _packSheet && Time.timeSinceLevelLoad > 1f) Poof(c.Pos, 0.6f);
                    v.Body.Sheet = want;
                }
                v.Body.Tick(clip, dt, v.Root.localPosition - before, clip == "LookingAround");
                if (v.Pack != null)   // costas: atras do corpo de frente (S), na frente de costas (N) e de lado; linhas do ferreiro S, W, N, E
                {
                    int d = v.Body.Dir;
                    v.Pack.transform.parent.localPosition = new Vector3(d == 1 ? PackDX : d == 3 ? -PackDX : 0f, PackY, 0f);
                    v.Pack.sortingOrder = v.Body.R.sortingOrder + (d == 0 ? -1 : 1);
                }
            }
            // v0.6a (P2-2): Botas = poeirinha nos pes andando, ate 3/s (ambiente: so com vaga no pool)
            if (v == _player && c.Moving && _sim.Has(Upgrade.PlayerSpeed) && (v.DustT -= dt) <= 0f)
            {
                v.DustT = DustEvery;
                if (_fxLive < FxAmbient)
                {
                    Vector3 f = W(c.Pos) + new Vector3(Random.Range(-0.12f, 0.12f), 0.02f, 0f);
                    Spawn(Art.Disc(), Dust, f, f + new Vector3(0f, 0.1f, 0f), f + new Vector3(0f, 0.18f, 0f), 0.45f, 0.12f, 0.32f, true, 0f, null, Depth(c.Pos.Y) - 1);
                }
            }
            // FASE7: pilha mista (o ferreiro leva ate o teto de cada tipo), na ordem PileOrder; o 1o item apoia na cabeca (StackY)
            int k = 0;
            float y = 0f, prev = 0f;
            foreach (Item it in _pileOrder)
                for (int n = 0; n < c.Held[(int)it] && k < v.Stack.Length; n++, k++)
                {
                    float h = _lyingH[(int)it] * ItemS;
                    y += k == 0 ? h * 0.5f : (prev + h) * 0.375f;   // centro a centro = 75% da media das alturas
                    prev = h;
                    _pileY[k] = y;
                    v.Stack[k].enabled = true;
                    Art.PaintItem(v.Stack[k], it, false, ItemS);
                }
            float fit = y > StackMax ? StackMax / y : 1f;
            v.Top = v.StackY - 0.03f + (y + prev * 0.5f) * fit;
            for (int i = 0; i < k; i++) v.Stack[i].transform.localPosition = new Vector3(0f, v.StackY - 0.03f + _pileY[i] * fit, 0f);
            for (; k < v.Stack.Length; k++) v.Stack[k].enabled = false;
        }

        // ------------------------------------------------------------------ workers de posto (B-CORE-04)

        /// <summary>Worker da Equipe parado no posto (oficial, balconista, almoxarife, mestre de obras), com a folha da skin do WorkerDef (Lote 5A,
        /// ASSETS s11; sem folha, disco na cor dos ajudantes com o aro do papel). PostoOlha s de Idle e um LookingAround inteiro, em ciclo.</summary>
        sealed class PostoV { public int K; public Transform Root; public Body Body; public float T; }
        readonly List<PostoV> _postos = new List<PostoV>();
        const float PostoOlha = 6f;

        /// <summary>
        /// Onde o worker fica: o oficial atras da 1a bancada aberta da familia, a balconista na ponta direita do balcao (lado da oficina), o
        /// almoxarife ao lado do deposito e o mestre de obras no meio da oficina. So visual: o Sim nao tem o corpo dele (ninguem esbarra).
        /// ponytail: posto fixo por papel; conferir na foto (-equipe) e mover aqui.
        /// </summary>
        V2 Posto(WorkerDef d)
        {
            switch (d.Papel)
            {
                case WorkerRole.Smith:
                    Station st = _sim.Stations.Find(x => x.Unlocked && x.Produces && x.Recipe.Station == d.Station);
                    return st != null ? new V2(st.Pos.X, st.Pos.Y + 0.62f) : _sim.Counter.Pos;
                case WorkerRole.Smelter:   // C-CORE-05: a Vulcana em cima da fornalha que ela opera (a do Requer)
                    Station f = _sim.Stations.Find(x => x.Unlocked && x.Produces && x.Recipe.Family == d.Requer);
                    return f != null ? new V2(f.Pos.X, f.Pos.Y + 0.62f) : _sim.Counter.Pos;
                case WorkerRole.Clerk: return new V2(_sim.Counter.Pos.X + _sim.Counter.Half.X - 0.35f, _sim.Counter.Pos.Y - 0.62f);
                case WorkerRole.Quartermaster: return new V2(3.2f, 2.0f);
                default: return new V2(6.0f, 7.5f);
            }
        }

        void RefreshPostos(float dt)
        {
            Equipe e = _sim.Equipe;
            for (int k = 0; k < e.Defs.Length; k++)
            {
                if (e.Defs[k].Papel == WorkerRole.Porter || e.Nivel(k) == 0 || _postos.Exists(p => p.K == k)) continue;
                _postos.Add(BuildPosto(k));
                if (Time.timeSinceLevelLoad > 1f) Poof(Posto(e.Defs[k]), 1f);   // contratou agora (o save carrega sem poeira)
            }
            foreach (PostoV p in _postos)
            {
                p.Root.localPosition = W(Posto(e.Defs[p.K]));   // o balcao alarga com as vagas
                if (p.Body == null) continue;
                p.T += dt;
                float look = p.Body.Sheet.Length("LookingAround");   // ~6,3 s no Lote 5A: PostoOlha de Idle e o LookingAround inteiro, em ciclo
                p.Body.Tick(look > 0f && p.T % (PostoOlha + look) >= PostoOlha ? "LookingAround" : "Idle", dt, Vector2.zero);
            }
        }

        PostoV BuildPosto(int k)
        {
            WorkerDef d = _sim.Equipe.Defs[k];
            var p = new PostoV { K = k, Root = new GameObject("Worker " + d.Id).transform, T = k * 1.3f };   // fases diferentes: nao olham todos juntos
            p.Root.SetParent(transform, false);
            p.Root.localPosition = W(Posto(d));
            if (SpriteSheet.TryGet(d.Skin, out SpriteSheet sh))
            {
                sh.Scale = CharScale(d.Skin);
                Art.NewSprite(p.Root, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.3f), 5, Vector2.zero, new Vector2(0.5f, 0.2f));
                p.Body = new Body { Sheet = sh, R = Art.NewSprite(p.Root, "Corpo", null, Color.white, 0, Vector2.zero, Vector2.one) };
            }
            else
            {
                int o = Depth(Posto(d).Y);
                Art.NewSprite(p.Root, "Sombra", Art.Disc(), new Color(0f, 0f, 0f, 0.3f), 5, new Vector2(0.04f, -0.08f), Vector2.one * 0.52f);
                Art.NewSprite(p.Root, "Corpo", Art.Disc(), Art.Worker, o, Vector2.zero, Vector2.one * 0.52f);
                Art.NewSprite(p.Root, "Papel", Art.Ring(), Art.Accent, o + 1, Vector2.zero, Vector2.one * 0.52f);
            }
            return p;
        }

        // ------------------------------------------------------------------ juice (v0.5b): efeitos curtos, venda, seta da dica

        /// <summary>Venda (Ev.Sold, chamado pelo Game): o balao da fila estoura e sobe um coracao com 6 brilhos em volta.</summary>
        public void Sold(V2 at, bool jewel)
        {
            BubbleV b = jewel ? _jewelBubble : _bubble;
            b.PopT = 0f;
            Vector3 c = b.Root.gameObject.activeSelf ? b.Root.localPosition : W(at) + new Vector3(BubbleDX, ClientTop + BubbleUp, 0f);
            Spawn(Art.Heart(), HeartC, c, c + new Vector3(0f, 0.5f, 0f), c + new Vector3(0f, 1.1f, 0f), 0.7f, 0.45f, 0.62f, true);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f + 0.3f;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.85f;
                Spawn(Art.Sparkle(), Art.Accent, c, c + d * 0.6f, c + d, 0.38f, 0.32f, 0.05f, true, 180f);
            }
        }

        /// <summary>VIP entrou na vaga (Ev.VipArrived) ou levou o pacote (Ev.VipServed): anel de 10 brilhos dourados; servido = 2 coracoes a mais.</summary>
        public void VipBurst(V2 at, bool served)
        {
            Vector3 c = W(at) + new Vector3(0f, ClientTop + 0.2f, 0f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI / 5f;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.8f, 0f) * (served ? 1.3f : 1f);
                Spawn(Art.Sparkle(), Art.Accent, c, c + d * 0.6f, c + d, 0.5f, 0.4f, 0.06f, true, 200f);
            }
            if (!served) return;
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 h = c + new Vector3(k * 0.35f, 0.2f, 0f);
                Spawn(Art.Heart(), HeartC, h, h + new Vector3(k * 0.2f, 0.6f, 0f), h + new Vector3(k * 0.3f, 1.3f, 0f), 0.9f, 0.4f, 0.6f, true);
            }
        }

        /// <summary>Poeira da obra: 7 nuvenzinhas saindo do pe e crescendo, somem em 0,5 s.</summary>
        void Poof(V2 at, float radius)
        {
            Vector3 c = W(at);
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f;
                Vector3 d = new Vector3(Mathf.Cos(a) * radius * 0.6f, Mathf.Sin(a) * radius * 0.3f + 0.15f, 0f);
                Spawn(Art.Disc(), Dust, c, c + d * 0.7f, c + d, 0.5f, 0.3f, 0.75f, true);
            }
        }

        /// <summary>
        /// Efeito do pool (ate FxMax vivos; cheio = o efeito e' pulado). `size` em m como o PaintItem (arte com ArtFill). v0.6a: cor
        /// vai de `c` a `to` na vida; `order` (faisca/fumaca/poeira ordenadas com a estacao ou o pe; padrao por cima de tudo).
        /// </summary>
        internal void Spawn(Sprite s, Color c, Vector3 a, Vector3 m, Vector3 b, float life, float s0, float s1, bool fade, float spin = 0f, Color? to = null, int order = FxOrder)
        {
            Fx f = null;
            foreach (Fx x in _fx) if (!x.Live) { f = x; break; }
            if (f == null)
            {
                if (_fx.Count >= FxMax) return;   // ponytail: pico de efeitos some em vez de crescer o pool
                _fx.Add(f = new Fx { R = Art.NewSprite(transform, "Fx", null, Color.white, FxOrder, Vector2.zero, Vector2.one) });
            }
            f.R.sprite = s; f.R.color = f.C = c; f.C2 = to ?? c; f.R.sortingOrder = order; f.R.enabled = true;
            _fxLive++;
            f.A = a; f.M = m; f.B = b; f.Life = life; f.S0 = s0; f.S1 = s1; f.Fade = fade; f.Spin = spin; f.Age = 0f; f.Live = true;
            f.R.transform.localPosition = a; f.R.transform.localScale = Vector3.one * s0; f.R.transform.localRotation = Quaternion.identity;
        }

        /// <summary>Efeito ambiente (faisca, fumaca) cabe: a estacao esta na camera e os `n` novos nao passam de FxAmbient vivos.</summary>
        internal bool Ambiente(Transform root, int n) => _fxLive + n <= FxAmbient && NaTela(root);

        /// <summary>O ponto esta na camera (com 10% de folga): som e "+1" da bigorna so de quem se ve (A-ART-07).</summary>
        internal bool NaTela(Transform root)
        {
            Vector3 vp = _cam.WorldToViewportPoint(root.position);
            return vp.x >= -0.1f && vp.x <= 1.1f && vp.y >= -0.1f && vp.y <= 1.1f;
        }

        /// <summary>Estado de cada fornalha aberta e da esteira (B-ART-08: levando N / parada), para as linhas SHOT do -shot e RECORD OK do
        /// -record (A-ART-06: confere a foto contra a decisao).</summary>
        public string Fornalhas()
        {
            var sb = new System.Text.StringBuilder();
            foreach (StationV v in _stations) if (v.Forno != null && v.S.Unlocked) sb.Append(v.S.Name).Append('=').Append(v.Forno.Atual).Append(' ');
            if (_sim.HasConveyor) sb.Append("Esteira=").Append(_esteira.Levando > 0 ? "levando" + _esteira.Levando : "parada");
            return sb.ToString().TrimEnd();
        }

        void TickFx(float dt)
        {
            _fxLive = 0;
            foreach (Fx f in _fx)
            {
                if (!f.Live) continue;
                f.Age += dt;
                float t = Mathf.Clamp01(f.Age / f.Life), u = 1f - t;
                f.R.transform.localPosition = u * u * f.A + 2f * u * t * f.M + t * t * f.B;
                f.R.transform.localScale = Vector3.one * Mathf.Lerp(f.S0, f.S1, t);
                if (f.Spin != 0f) f.R.transform.localRotation = Quaternion.Euler(0f, 0f, f.Spin * f.Age);
                Color col = Color.Lerp(f.C, f.C2, t);
                if (f.Fade) col.a = f.C.a * Mathf.Clamp01(u / 0.4f);
                f.R.color = col;
                if (t >= 1f) { f.Live = false; f.R.enabled = false; }
                else _fxLive++;
            }
        }

        /// <summary>Alvo da dica (Game, 4x/s): placa da vez, boca onde depositar/recolher, bau ou deposito; sem alvo no mundo, sem seta.</summary>
        public void ShowHint(Hint h, int arg)
        {
            _hintOn = true;
            switch (h)
            {
                case Hint.BuyPad: _hintAt = _sim.Pads[arg].Pos; break;
                case Hint.ProductToCounter: _hintAt = _sim.CounterFor((Item)arg).InAt; break;
                case Hint.IngotToCrafter: _hintOn = Nearest(s => s.Kind == Kind.Crafter && s.In < s.InCap, true); break;
                case Hint.OreToFurnace: _hintOn = Nearest(s => s.Kind == Kind.Furnace && s.In < s.InCap, true); break;
                case Hint.PickProducts: _hintOn = Nearest(s => s.Kind == Kind.Crafter && (int)s.OutItem == arg && s.Out > 0, false); break;
                case Hint.PickIngots: _hintOn = Nearest(s => s.Kind == Kind.Furnace && s.Out > 0, false); break;
                case Hint.OpenChest: _hintAt = _sim.Chests[arg].Pos; break;
                case Hint.GrabOre: _hintAt = _sim.Deposit.OutAt; break;
                default: _hintOn = false; break;   // Melhorias (o badge pulsa) e cliente esperando (a dica de texto basta)
            }
        }

        bool Nearest(System.Predicate<Station> ok, bool input)
        {
            float best = float.MaxValue;
            foreach (Station s in _sim.Stations)
            {
                if (!s.Unlocked || !ok(s)) continue;
                V2 p = input ? s.InAt : s.OutAt;
                float d = V2.Dist(p, _sim.Player.Pos);
                if (d < best) { best = d; _hintAt = p; }
            }
            return best < float.MaxValue;
        }

        /// <summary>Seta acima do alvo quicando 12 px a 1,5 Hz (v0.6d; era 0,15 m a ~1,7 Hz); some com o jogador em cima dele.</summary>
        void RefreshArrow()
        {
            bool on = _hintOn && V2.Dist(_hintAt, _sim.Player.Pos) > 0.9f;
            if (_arrow.gameObject.activeSelf != on) _arrow.gameObject.SetActive(on);
            if (on) _arrow.localPosition = W(_hintAt) + new Vector3(0f, ArrowY + ArrowBob * Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * 1.5f)), 0f);
        }

        // ------------------------------------------------------------------ rotulos na HUD

        void PlaceLabel(Text t, Vector3 world, Vector2 offsetPx) => PlaceRect(t.rectTransform, world, offsetPx);

        void PlaceRect(RectTransform r, Vector3 world, Vector2 offsetPx = default)
        {
            Vector3 sp = _cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_labels, sp, null, out Vector2 local);
            r.anchoredPosition = local + offsetPx;
        }

        /// <summary>
        /// "+N" da venda (v0.6d): vendas a menos de 0,4 s do ultimo "+N" somam nele em vez de abrir outro, entao nasce no maximo 1 a cada
        /// 0,4 s e, vivendo 1,1 s, nunca passam de 3 na tela. ponytail: soma tambem a venda da loja de joias no "+N" do balcao (2 lojas no mesmo 0,4 s, raro).
        /// </summary>
        public void SaleFloat(V2 world, int gold)
        {
            if (_sale != null && _sale.Live && _sale.Age < SaleMerge) { _saleSum += gold; _sale.T.text = "+" + _saleSum; return; }
            _saleSum = gold;
            _sale = NewFloat(world, "+" + gold, Art.Accent, 44);
        }

        /// <summary>Numero flutuante ("+10", nome do upgrade) subindo do ponto do mundo.</summary>
        public void Float(V2 world, string text, Color color, int size = 36) => NewFloat(world, text, color, size);

        Floater NewFloat(V2 world, string text, Color color, int size)
        {
            Floater f = null;
            foreach (Floater x in _floaters) if (!x.Live) { f = x; break; }
            if (f == null)
            {
                f = new Floater { T = Art.Outlined(Art.FreeText(_labels, "Flutuante", size, new Vector2(400f, 60f)), 2.5f) };   // v0.5b: contorno
                f.T.fontStyle = FontStyle.Bold;
                _floaters.Add(f);
            }
            f.Live = true; f.Age = 0f; f.World = W(world);
            f.T.enabled = true; f.T.text = text; f.T.fontSize = size; f.T.color = color;
            PlaceLabel(f.T, f.World + new Vector3(0f, 0.5f, 0f), Vector2.zero);
            return f;
        }
    }
}
