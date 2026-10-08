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
    /// 2a area (docs/AREA2_JOALHERIA.md s7): rua lateral x 9-15 em pedra fria, escura com rotulo e o arco de teaser ate
    /// o Corredor; loja de joias escurecida "fechada" ate a Joalheria; clientes da JewelQueue com a folha `nobre`.
    /// Fase 2 (docs/AREA2_FASE2.md s4): joalheiro (papel 3) com tinta roxa; bau de marco disponivel pulsando com o rotulo.
    /// Fase 3 (docs/FASE3_LUXO.md): fachada nobre, piso de ladrilhos e joalheria real, procedurais, ligados pelas flags de luxo.
    /// Cenario: chaos com textura repetida (Resources/Textures ou reserva procedural), paredes 2,5D fora da area andavel e props
    /// pre-renderizados (tools/props_blender.py) encostados nelas, com tochas de brilho tremulo.
    /// v0.5 (docs/ASSETS.md s7, BENCHMARK_VISUAL s4): itens renderizados (icone/deitado, mascaras so de reserva), balao grande so no
    /// 1o da fila, estande do balcao montado por vaga com o estoque em pe, portoes vistos de lado e quem acha a fila cheia passando.
    /// </summary>
    public sealed class WorldView : MonoBehaviour
    {
        sealed class StationV
        {
            public Station S; public Transform Root; public SpriteRenderer Base, BarBg, BarFill;
            public SpriteRenderer[] InPile, OutPile, Stock; public Item[] StockItems; public Text Label;   // Stock: 6 por item vendido
            public bool Baked;   // Base e' o sprite pre-renderizado (sem icone)
            public bool Stand;   // balcao principal desenhado pelo estande modular (BuildStand), sem Base nem grade de estoque
        }
        sealed class PadV { public Pad P; public Transform Root; public SpriteRenderer Ring, Fill, Plus; public Text Label; }
        sealed class CarrierV { public Carrier C; public Transform Root; public SpriteRenderer[] Stack; public Body Body; public float CheerT, StackY; }   // StackY: onde a pilha apoia
        /// <summary>Balao grande do 1o da fila (um por fila, segue o cliente): trilho escuro, anel de paciencia, corpo, rabicho e icone.</summary>
        sealed class BubbleV { public Transform Root; public SpriteRenderer Ring, Icon; }
        sealed class ClientV
        {
            public Client C; public int Id; public Transform Root; public SpriteRenderer Want, Patience; public float TopY;   // TopY: topo da cabeca
            public Body Body; public int Phase; public float LeaveT; public bool Served; public Vector3 Lane, Exit;   // saida (so com arte)
            public bool Jewel;   // fila da loja de joias (JewelQueue/JewelSlot) em vez da do balcao
        }
        sealed class ChestV { public Chest C; public Transform Root; public Text Label; }
        sealed class Floater { public Text T; public Vector3 World; public float Age; public bool Live; }
        /// <summary>Decoracao de uma compra de luxo: raiz ligada pela flag `Bought`; PopT >= 0 = pop em andamento.</summary>
        sealed class LuxV { public Upgrade U; public Transform Root; public float PopT = -1f; }

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
        static readonly Item[] PileOrder = { Item.Ore, Item.Shield, Item.Jewel, Item.Tool, Item.Ingot, Item.Sword };
        static readonly float[] LyingH = { 0.88f, 0.57f, 0.3f, 0.95f, 0.61f, 0.95f };
        readonly float[] _pileY = new float[Balance.PlayerCapUp * 6];
        const float PileS = 0.4f, PileStep = 0.2f;   // pilhas das estacoes (2 colunas de itens deitados)
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
        static float CharScale(string n) => CharPpuMul * n switch
        {
            "ajudante" => 1.19f, "guerreira" => 1.24f, "anao" => 1.08f, "elfa" => 1.03f, "goblin" => 1.11f, "cavaleiro" => 0.79f, "nobre" => 0.91f, _ => 1f,
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
        static readonly Color Dimmed = new Color(0.55f, 0.55f, 0.6f, 1f);
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
        // Fase 3 (docs/FASE3_LUXO.md): luxo so decora. Pop 0,6 -> 1 em 0,3 s na compra; ao carregar aparece pronto.
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
        const int ExteriorOrder = -8, FloorOrder = -7, LuxOrder = -6, ShadeOrder = -5, WallOrder = -4, GlowOrder = -3;   // abaixo do tapete (-1), sombras e pads (1-4)
        static readonly Color ExteriorTint = Gray(0.42f), FaceTint = Gray(0.55f), ShadeColor = new Color(0f, 0f, 0f, 0.35f);
        static readonly Color MuroTint = new Color(0.72f, 0.8f, 0.95f), MuroFaceTint = new Color(0.4f, 0.44f, 0.52f);   // pedra fria (ART_BIBLE s2) sobre a parede quente
        // Props (tools/props_blender.py; escala real, 1 m do Blender = 1 m do mundo) encostados nas paredes, fora do clamp e longe de
        // pads, estacoes, filas, baus e rotulos: tampo da parede esquerda (x -0,3 a -0,45), quintal atras da parede de baixo (y < -1,2),
        // muro da rua lateral (x 15,2-15,45) e quintal atras do muro de baixo da rua. Borda de cada prop <= x 0,05 / >= x 14,95: o corpo
        // do jogador encostado no clamp (raio ~0,25) nao entra nele. Tocha: pivo no pe da parede, chama 0,46 m acima.
        static readonly (string Name, float X, float Y)[] Props =
        {
            ("balde", 0f, 0.1f), ("prateleira", -0.42f, 2.75f), ("tocha", -0.3f, 4.0f), ("tocha", -0.3f, 11.0f),
            ("suporte_armas", -0.45f, 12.4f), ("barril", -0.3f, 13.55f),
            ("lenha", 0.6f, -1.45f), ("tocha", 2.0f, -1.2f), ("barril", 3.0f, -1.35f), ("caixote", 4.1f, -1.4f), ("sacos", 5.3f, -1.4f),
            ("tocha", 6.6f, -1.2f), ("barril", 7.6f, -1.35f), ("balde", 8.3f, -1.3f),
            ("barril", 15.4f, 0.6f), ("caixote", 15.45f, 1.6f), ("tocha", 15.2f, 7.0f), ("sacos", 15.4f, 10.2f), ("tocha", 15.2f, 13.0f),
            ("lenha", 11.2f, -0.95f), ("barril", 13.4f, -0.9f), ("balde", 14.0f, -0.85f),
        };
        const float FlameY = 0.46f, GlowSize = 1.5f, GlowAlpha = 0.3f;   // brilho alfa (Sprites/Default): aditivo pediria shader fora do APK
        static readonly Color GlowColor = Art.Hex(0xFF9A3D);
        static float PropShadow(string n) => n switch { "tocha" => 0f, "balde" => 0.4f, "barril" => 0.6f, "prateleira" or "suporte_armas" or "lenha" => 0.9f, _ => 0.75f };
        static Color Gray(float v) => new Color(v, v, v, 1f);
        // Joalheria real: tapete roxo da bancada a loja (x 11,45-12,55) e 4 pedestais de gema fora dos pads, da fila e do bau.
        static readonly Color Carpet = Color.Lerp(Art.ItemColor[(int)Item.Jewel], Art.Bg, 0.55f);   // #59447F: a joia do estoque ainda le por cima
        const float CarpetW = 1.1f;
        static readonly V2[] Pedestals = { new V2(10.95f, 8.4f), new V2(13.05f, 8.4f), new V2(10.95f, 10.9f), new V2(13.05f, 10.9f) };

        /// <summary>sortingOrder pelo pe: quem esta mais abaixo na tela fica na frente (1 cm de resolucao/2).</summary>
        static int Depth(float footY) => 1000 - Mathf.RoundToInt(footY * 50f);

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
        SpriteRenderer _street, _shopTeaser; Text _streetLabel, _shopLabel;
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
        readonly List<(Item It, SpriteRenderer R)> _standStock = new List<(Item, SpriteRenderer)>();
        readonly int[] _shown = new int[6];
        readonly List<Floater> _floaters = new List<Floater>();
        readonly List<LuxV> _lux = new List<LuxV>();
        readonly List<SpriteRenderer> _glows = new List<SpriteRenderer>();
        Transform _conveyor; SpriteRenderer[] _convDots; float _convLen;
        int _clientSeq, _bought;

        public static Vector3 W(V2 p) => new Vector3(p.X, p.Y, 0f);

        public void Init(Sim sim, Camera cam, RectTransform labels)
        {
            _sim = sim; _cam = cam; _labels = labels;
            _bought = sim.UpgradesBought;
            foreach (string n in ClientArt)
                if (SpriteSheet.TryGet(n, out SpriteSheet sh)) { sh.Scale = CharScale(n); _clientSheets.Add(sh); }
            if (SpriteSheet.TryGet("nobre", out _nobre)) _nobre.Scale = CharScale("nobre");
            BuildScenery();
            _streetLabel = Art.FreeText(_labels, "RuaLateral", 26, new Vector2(200f, 80f));
            _streetLabel.text = "Rua\nlateral";   // 2 linhas: so 1,2 m da rua cabe na tela antes do Corredor
            _streetLabel.color = Art.ComAlfa(Art.Ink, 0.55f);
            Deco("carroca", CartPos, CartCell, 1.3f);
            _shopTeaser = Deco("loja_joalheria", sim.JewelShop.Pos, StationCell, 1.3f);   // mesma folha e celula da estacao
            if (_shopTeaser != null) _shopTeaser.color = Shut;
            _shopLabel = Art.FreeText(_labels, "JoalheriaFechada", 26, new Vector2(400f, 40f));
            _shopLabel.text = "Joalheria — fechada";
            _shopLabel.color = Art.ComAlfa(Art.Ink, 0.7f);
            Lux(Upgrade.WorkshopFloor, BuildTiles());
            Lux(Upgrade.WorkshopFacade, BuildFacade());
            Lux(Upgrade.JewelryDecor, BuildJewelryDecor());
            _hasStand = true;
            foreach (string n in StandPieces) _hasStand &= SpriteSheet.TryGet(n, out _);   // sem os modulos: balcao procedural de sempre
            _bubble = BuildBubble("BalaoFila");
            _jewelBubble = BuildBubble("BalaoJoias");
            foreach (Station s in sim.Stations) _stations.Add(BuildStation(s));
            foreach (Pad p in sim.Pads) _pads.Add(BuildPad(p));
            foreach (Chest c in sim.Chests) _chests.Add(BuildChest(c));
            BuildConveyor();
            _player = BuildCarrier(sim.Player, Art.Player, 0.62f, Balance.PlayerCapUp * sim.Player.Held.Length);   // FASE7: ate o teto de CADA tipo
        }

        // ------------------------------------------------------------------ construcao

        string StationArt(Station s)
        {
            switch (s.Kind)
            {
                case Kind.Deposit: return "deposito";
                case Kind.Furnace: return "fornalha";
                case Kind.Counter: return s == _sim.JewelShop ? "loja_joalheria" : null;   // balcao: estande modular (o toldo saiu na v0.5)
                default: return s.OutItem switch { Item.Sword => "bigorna", Item.Shield => "bancada_escudos", Item.Tool => "bancada_ferramentas", Item.Jewel => "bancada_joalheria", _ => null };
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
            float ws = Balance.WorkshopW, w = Balance.WorldW, h = Balance.WorldH;
            Sprite rua = Art.Ground("rua"), parede = Art.Ground("parede");
            Tiled("Exterior", rua, ExteriorTint, ExteriorOrder, new Vector2(-4f, -6f), new Vector2(w + 4f, h + 7f));   // fundo de toda a camera
            Tiled("Rua", rua, StreetOpen, FloorOrder, new Vector2(-4f, h), new Vector2(w + 4f, h + 2.6f));             // rua dos clientes, aberta
            _street = Tiled("RuaLateral", rua, StreetShut, FloorOrder, new Vector2(ws, 0f), new Vector2(w, h));
            Tiled("Chao", Art.Ground("piso_oficina"), Color.white, FloorOrder, Vector2.zero, new Vector2(ws, DeckY));
            Tiled("Assoalho", Art.Ground("madeira"), Color.white, FloorOrder, new Vector2(0f, DeckY), new Vector2(ws, h));
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

        StationV BuildStation(Station s)
        {
            var v = new StationV { S = s, Root = new GameObject(s.Name).transform, Stand = s == _sim.Counter && _hasStand };
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
            v.Label = Art.FreeText(_labels, s.Name, 26, new Vector2(320f, 40f));
            v.Label.text = s.Name;
            v.Label.color = Art.ComAlfa(Art.Ink, 0.8f);
            // bocas no chao (FASE5 s2b), na cor do que passa por ela: deposito so entrega, balcoes so recebem, quem produz tem as duas
            if (s.Kind == Kind.Deposit) Mouth(v.Root, s.OutAt - s.Pos, Art.ItemColor[(int)Item.Ore], false, (int)Item.Ore);
            else Mouth(v.Root, s.InAt - s.Pos, s.Produces ? Art.ItemColor[(int)s.InItem] : Art.Accent, true, s.Produces ? (int)s.InItem : s == _sim.JewelShop ? (int)Item.Jewel : -1);
            if (s.Produces) Mouth(v.Root, s.OutAt - s.Pos, Art.ItemColor[(int)s.OutItem], false, (int)s.OutItem);
            if (s.Produces)
            {
                v.InPile = Pile(v.Root, s.InItem, s.InCap, -1.05f);
                v.OutPile = Pile(v.Root, s.OutItem, s.OutCap, 1.05f);
                v.BarBg = Art.NewSprite(v.Root, "BarraFundo", Art.Square(), Art.Dim, 3, new Vector2(0f, -0.72f), new Vector2(1.3f, 0.12f));
                v.BarFill = Art.NewSprite(v.Root, "Barra", Art.Square(), Art.Accent, 4, new Vector2(-0.65f, -0.72f), new Vector2(0f, 0.12f));
            }
            if (s.Kind == Kind.Counter && !v.Stand)
            {
                var sold = new List<Item>();   // loja de joias: so joia; balcao sem os modulos do estande: espada/escudo/ferramenta
                for (Item it = Item.Sword; it <= Item.Jewel; it++) if (_sim.Sells(s, it)) sold.Add(it);
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
            }
            v.Root.gameObject.SetActive(s.Unlocked);
            return v;
        }

        /// <summary>
        /// Boca no chao: anel da zona (Balance.MouthRadius) com o item que entra/sai (arte deitada de 0,45 m a 50%); sem item (balcao,
        /// que recebe 3 produtos) ou sem arte, a seta de sempre apontando para a estacao (entra) ou para fora (sai).
        /// </summary>
        static void Mouth(Transform root, V2 off, Color c, bool into, int item)
        {
            var at = new Vector2(off.X, off.Y);
            Art.NewSprite(root, into ? "BocaEntra" : "BocaSai", Art.Disc(), Art.ComAlfa(c, 0.18f), 1, at, Vector2.one * Balance.MouthRadius * 2f);
            Art.NewSprite(root, "BocaAnel", Art.Ring(), Art.ComAlfa(c, 0.6f), 1, at, Vector2.one * Balance.MouthRadius * 2f);
            if (item >= 0 && Art.ItemArt((Item)item, false) != null)
            {
                SpriteRenderer r = Art.NewSprite(root, "BocaItem", null, Color.white, 2, at, Vector2.one);
                Art.PaintItem(r, (Item)item, false, 0.45f);
                r.color = Art.ComAlfa(Color.white, 0.5f);
                return;
            }
            float ang = Mathf.Atan2(off.Y, off.X) * Mathf.Rad2Deg + (into ? 90f : -90f);   // Triangle aponta para +y
            Art.NewSprite(root, "BocaSeta", Art.Triangle(), Art.ComAlfa(c, 0.9f), 1, at, Vector2.one * 0.32f, ang);
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
            // canteiro de obra: chao aparece por baixo, anel tracejado, "+" no meio; o pago enche de ouro do centro para fora
            Art.NewSprite(v.Root, "Fundo", Art.Disc(), Art.ComAlfa(Art.Pad, 0.5f), 2, Vector2.zero, Vector2.one * 1.15f);
            v.Fill = Art.NewSprite(v.Root, "Pago", Art.Disc(), Art.PadFill, 3, Vector2.zero, Vector2.zero);
            v.Ring = Art.NewSprite(v.Root, "Anel", Art.DashedRing(), Art.Accent, 4, Vector2.zero, Vector2.one * 1.15f);
            v.Plus = Art.NewSprite(v.Root, "Obra", Art.Plus(), Art.Accent, 4, Vector2.zero, Vector2.one * 0.3f);
            v.Label = Art.FreeText(_labels, "PadLabel", 24, new Vector2(300f, 70f));
            return v;
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
            v.Label.text = c.Label;
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

        /// <summary>Liga a decoracao pela flag ja carregada (save ou -buy): aparece pronta, sem pop.</summary>
        void Lux(Upgrade u, Transform root)
        {
            root.gameObject.SetActive(_sim.Bought[(int)u]);
            _lux.Add(new LuxV { U = u, Root = root });
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

        void BuildConveyor()
        {
            Vector3 a = W(_sim.FurnaceA.Pos), b = W(_sim.AnvilA.Pos);
            _convLen = Vector3.Distance(a, b) - 1.2f;
            _conveyor = new GameObject("Esteira").transform;
            _conveyor.SetParent(transform, false);
            _conveyor.localPosition = (a + b) * 0.5f + new Vector3(0.45f, 0f, 0f);
            Art.NewSprite(_conveyor, "Trilho", Art.Square(), Art.Dim, 1, Vector2.zero, new Vector2(0.34f, _convLen));
            _convDots = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                _convDots[i] = Art.NewSprite(_conveyor, "Lingote", null, Color.white, 2, Vector2.zero, Vector2.one);
                Art.PaintItem(_convDots[i], Item.Ingot, false, 0.3f);
            }
            _conveyor.gameObject.SetActive(false);
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
            return v;
        }

        /// <summary>Balao grande (BENCHMARK_VISUAL P0-2), escondido ate a fila ter alguem; RefreshQueue o prende no 1o.</summary>
        BubbleV BuildBubble(string name)
        {
            var b = new BubbleV { Root = Group(name, new V2(0f, 0f)) };
            Art.NewSprite(b.Root, "Trilho", Art.BalloonRing(1f), Art.ComAlfa(Art.Bg, 0.85f), BubbleOrder + 10, Vector2.zero, RingSize);
            b.Ring = Art.NewSprite(b.Root, "Paciencia", Art.BalloonRing(1f), Art.Good, BubbleOrder + 11, Vector2.zero, RingSize);
            Art.NewSprite(b.Root, "Balao", Art.Rounded(), Art.Bubble, BubbleOrder + 12, Vector2.zero, BalloonSize);
            Art.NewSprite(b.Root, "Rabicho", Art.Triangle(), Art.Bubble, BubbleOrder + 12, new Vector2(-BubbleDX, TailY), TailSize, 180f);
            b.Icon = Art.NewSprite(b.Root, "Pedido", null, Color.white, BubbleOrder + 13, Vector2.zero, Vector2.one);
            b.Root.gameObject.SetActive(false);
            return b;
        }

        /// <summary>Balao no 1o da fila: icone de 0,9 m, anel verde -> amarelo -> vermelho e tremida de +-3 graus a 6 Hz abaixo de 20%.</summary>
        void PlaceBubble(BubbleV b, ClientV v, Item want, float f)
        {
            b.Root.localPosition = v.Root.localPosition + new Vector3(BubbleDX, v.TopY + BubbleUp, 0f);
            b.Root.localRotation = Quaternion.Euler(0f, 0f, f < 0.2f ? 3f * Mathf.Sin(Time.time * 6f * 2f * Mathf.PI) : 0f);
            Art.PaintItem(b.Icon, want, true, BubbleIcon);
            b.Ring.sprite = Art.BalloonRing(f);
            b.Ring.color = f > 0.5f ? Color.Lerp(Art.Gold, Art.Good, f * 2f - 1f) : Color.Lerp(Art.Bad, Art.Gold, f * 2f);
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
            v.Root.gameObject.SetActive(true);
            return v;
        }

        /// <summary>Liga um Client novo da fila a uma view livre.</summary>
        ClientV BindClient(Client c, int slot, bool jewel)
        {
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
            foreach (StationV v in _stations) RefreshStation(v, pulse);
            RefreshStand(dt);
            foreach (PadV v in _pads) RefreshPad(v, pulse);
            foreach (ChestV v in _chests) RefreshChest(v);
            foreach (LuxV v in _lux) RefreshLux(v, dt);

            bool conv = _sim.HasConveyor;
            if (_conveyor.gameObject.activeSelf != conv) _conveyor.gameObject.SetActive(conv);
            if (conv)
                for (int i = 0; i < _convDots.Length; i++)
                {
                    float t = Mathf.Repeat(Time.time * 0.45f + i / (float)_convDots.Length, 1f);
                    _convDots[i].transform.localPosition = new Vector3(0f, (t - 0.5f) * _convLen, 0f);
                    _convDots[i].enabled = _sim.FurnaceA.Out > 0 || _sim.AnvilA.In > 0;
                }

            if (_sim.UpgradesBought != _bought) { _bought = _sim.UpgradesBought; Cheer(); }   // comprou upgrade
            RefreshCarrier(_player, dt);
            while (_workers.Count < _sim.Workers.Count) _workers.Add(BuildCarrier(_sim.Workers[_workers.Count], Art.Worker, 0.52f, Balance.WorkerCapUp));
            foreach (CarrierV v in _workers) RefreshCarrier(v, dt);

            RefreshClients(dt);

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
            bool corridor = _sim.Bought[(int)Upgrade.SideCorridor], teaser = corridor && !_sim.Bought[(int)Upgrade.Jewelry];
            _street.color = corridor ? StreetOpen : StreetShut;
            foreach ((GameObject shut, GameObject open) in _gates)   // fechado ate o Corredor, aberto depois (folhas contra o muro da rua)
            {
                if (shut != null && shut.activeSelf == corridor) shut.SetActive(!corridor);
                if (open != null && open.activeSelf != corridor) open.SetActive(corridor);
            }
            _streetLabel.enabled = !corridor;
            if (!corridor) PlaceLabel(_streetLabel, new Vector3(Balance.WorkshopW + 0.6f, (Balance.SideWallOpenings[1] + Balance.SideWallOpenings[2]) / 2f, 0f), Vector2.zero);   // na parede entre a porta e o arco, nao em cima da porta
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
                v.Want.enabled = v.Patience.enabled = false;
                Vector3 p = v.Root.localPosition;
                float laneY = Mathf.Max(W(Slot(v.Jewel, 0)).y, Balance.WorldH) + 0.7f;   // faixa da rua de cima, atras das duas filas
                v.Lane = new Vector3(p.x + (v.Id % 3 - 1) * 0.3f, laneY, 0f);
                v.Exit = new Vector3(v.Served ? Balance.WorldW + 1.6f : -1.6f, laneY, 0f);   // fora da camera (margem 1,2 m)
                v.Phase = 0;
                v.LeaveT = 0f;
            }
            RefreshQueue(false, dt);
            RefreshQueue(true, dt);
            foreach (ClientV v in _clients) if (v.C == null && v.Body != null && v.Root.gameObject.activeSelf) RefreshLeaving(v, dt);
        }

        /// <summary>
        /// Uma fila (balcao ou loja de joias): mesma maquina de estados, so muda a lista, os slots e o elenco. O 1o leva o balao
        /// grande; os outros, mini-icone e paciencia. Vagas que se recentram (balcao evolutivo) = todos deslizam ate a vaga nova.
        /// </summary>
        void RefreshQueue(bool jewel, float dt)
        {
            List<Client> q = QueueOf(jewel);
            BubbleV b = jewel ? _jewelBubble : _bubble;
            if (b.Root.gameObject.activeSelf != q.Count > 0) b.Root.gameObject.SetActive(q.Count > 0);
            for (int i = 0; i < q.Count; i++)
            {
                Client c = q[i];
                ClientV v = null;
                foreach (ClientV x in _clients) if (x.C == c) { v = x; break; }
                if (v == null) v = BindClient(c, i, jewel);
                Vector3 before = v.Root.localPosition;
                v.Root.localPosition = Vector3.MoveTowards(before, W(Slot(jewel, i)), 4f * dt);
                float f = Mathf.Clamp01(c.Patience / c.MaxPatience);
                v.Want.enabled = v.Patience.enabled = i > 0;
                if (i == 0) PlaceBubble(b, v, c.Want, f);
                else
                {
                    Art.PaintItem(v.Want, c.Want, true, MiniIcon);
                    v.Patience.transform.localScale = new Vector3(PatW * f, 0.06f, 1f);
                    v.Patience.transform.localPosition = new Vector3(PatW * (f - 1f) / 2f, v.TopY + PatY, 0f);
                    v.Patience.color = Color.Lerp(Art.Bad, Art.Good, f);
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

        void RefreshStation(StationV v, float pulse)
        {
            Station s = v.S;
            if (v.Root.gameObject.activeSelf != s.Unlocked) v.Root.gameObject.SetActive(s.Unlocked);
            if (!s.Unlocked) { v.Label.enabled = false; return; }
            v.Label.enabled = !v.Stand;   // o estande com espadas em pe ja le como loja; o rotulo cairia em cima da fila
            float labelY = s == _sim.Counter ? 0.9f : v.Baked ? 1.35f : 0.95f;   // balcao: abaixo do varal da Fachada nobre (y 14,1)
            PlaceLabel(v.Label, W(s.Pos) + new Vector3(0f, labelY, 0f), Vector2.zero);
            if (s.Produces)
            {
                for (int i = 0; i < v.InPile.Length; i++) v.InPile[i].enabled = i < s.In;
                bool blocked = s.Blocked || (s.Out >= s.OutCap);
                for (int i = 0; i < v.OutPile.Length; i++)
                {
                    v.OutPile[i].enabled = i < s.Out;
                    v.OutPile[i].color = blocked ? Color.Lerp(Art.ItemTint(s.OutItem), Art.Bad, 0.5f * pulse) : Art.ItemTint(s.OutItem);
                }
                float p = s.Busy ? s.Progress : 0f;
                v.BarFill.transform.localScale = new Vector3(1.3f * p, 0.12f, 1f);
                v.BarFill.transform.localPosition = new Vector3(-0.65f + 0.65f * p, -0.72f, 0f);
                bool starving = !s.Busy && s.In < s.Need;
                v.BarBg.color = starving ? Color.Lerp(Art.Dim, Art.Bad, 0.35f * pulse) : Art.Dim;
                v.Base.color = v.Baked ? (s.Busy ? Color.white : Dimmed) : Art.ComAlfa(Art.StationColor(s), s.Busy ? 1f : 0.8f);
            }
            if (s.Kind == Kind.Counter && v.Stock != null)
                for (int p = 0; p < v.StockItems.Length; p++)
                    for (int i = 0; i < 6; i++) v.Stock[p * 6 + i].enabled = i < _sim.Stock[(int)v.StockItems[p]];
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
            foreach ((Item it, SpriteRenderer r) in _standStock) r.enabled = _shown[(int)it]++ < _sim.Stock[(int)it];   // enche da esquerda para a direita
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
                    _standStock.Add((it, Art.NewSprite(_stand, "Estoque", item, Color.white, order + 1, new Vector2(x + enc[k], enc[k + 1]), Vector2.one)));
            }
            StandPiece("balcao_ponta_dir", x - Balance.SlotStep / 2f + Balance.CounterEnd / 2f, order);
        }

        SpriteSheet StandPiece(string name, float x, int order)
        {
            SpriteSheet.TryGet(name, out SpriteSheet sh);   // _hasStand garante as 5 folhas
            Art.NewSprite(_stand, name, StaticArt(name, RealCell(name)), Color.white, order, new Vector2(x, 0f), Vector2.one);
            return sh;
        }

        void RefreshPad(PadV v, float pulse)
        {
            int u = v.P.Current(_sim);
            bool on = u >= 0;
            if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
            v.Label.enabled = on;
            if (!on) return;
            int cost = Upgrades.Cost(u), remaining = cost - v.P.Paid;
            bool affordable = _sim.Gold >= remaining;
            float frac = cost > 0 ? v.P.Paid / (float)cost : 0f;
            v.Fill.transform.localScale = Vector3.one * (1.0f * Mathf.Sqrt(frac));
            v.Ring.color = affordable ? Art.ComAlfa(Art.Accent, pulse) : Art.ComAlfa(Art.Ink, 0.45f);
            v.Plus.color = v.Ring.color;
            if (affordable) v.Ring.transform.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 20f);   // gira devagar quando da para pagar
            v.Label.text = Upgrades.All[u].Name + "\n" + remaining;
            v.Label.color = affordable ? Art.Accent : Art.ComAlfa(Art.Ink, 0.75f);
            PlaceLabel(v.Label, W(v.P.Pos) + new Vector3(0f, 0.95f, 0f), Vector2.zero);
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
            bool on = _sim.Bought[(int)v.U];
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
                v.Body.Tick(clip, dt, v.Root.localPosition - before, clip == "LookingAround");
            }
            // FASE7: pilha mista (o ferreiro leva ate o teto de cada tipo), na ordem PileOrder; o 1o item apoia na cabeca (StackY)
            int k = 0;
            float y = 0f, prev = 0f;
            foreach (Item it in PileOrder)
                for (int n = 0; n < c.Held[(int)it] && k < v.Stack.Length; n++, k++)
                {
                    float h = LyingH[(int)it] * ItemS;
                    y += k == 0 ? h * 0.5f : (prev + h) * 0.375f;   // centro a centro = 75% da media das alturas
                    prev = h;
                    _pileY[k] = y;
                    v.Stack[k].enabled = true;
                    Art.PaintItem(v.Stack[k], it, false, ItemS);
                }
            float fit = y > StackMax ? StackMax / y : 1f;
            for (int i = 0; i < k; i++) v.Stack[i].transform.localPosition = new Vector3(0f, v.StackY - 0.03f + _pileY[i] * fit, 0f);
            for (; k < v.Stack.Length; k++) v.Stack[k].enabled = false;
        }

        // ------------------------------------------------------------------ rotulos na HUD

        void PlaceLabel(Text t, Vector3 world, Vector2 offsetPx)
        {
            Vector3 sp = _cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_labels, sp, null, out Vector2 local);
            t.rectTransform.anchoredPosition = local + offsetPx;
        }

        /// <summary>Numero flutuante ("+10", nome do upgrade) subindo do ponto do mundo.</summary>
        public void Float(V2 world, string text, Color color, int size = 36)
        {
            Floater f = null;
            foreach (Floater x in _floaters) if (!x.Live) { f = x; break; }
            if (f == null)
            {
                f = new Floater { T = Art.FreeText(_labels, "Flutuante", size, new Vector2(400f, 60f)) };
                f.T.fontStyle = FontStyle.Bold;
                _floaters.Add(f);
            }
            f.Live = true; f.Age = 0f; f.World = W(world);
            f.T.enabled = true; f.T.text = text; f.T.fontSize = size; f.T.color = color;
            PlaceLabel(f.T, f.World + new Vector3(0f, 0.5f, 0f), Vector2.zero);
        }
    }
}
