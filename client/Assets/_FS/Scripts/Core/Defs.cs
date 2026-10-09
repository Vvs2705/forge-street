using System;

namespace FS.Core
{
    /// <summary>Itens em ordem de cadeia: minerio -> lingote -> produto (espada, escudo, ferramenta; joia na 2a area).</summary>
    public enum Item { Ore = 0, Ingot = 1, Sword = 2, Shield = 3, Tool = 4, Jewel = 5 }

    public enum Kind { Deposit, Furnace, Crafter, Counter }

    /// <summary>
    /// Os 15 upgrades do MVP (GDD §18/§19) + 2 da 2a area (v0.2) + 3 da joalheria (fase 2). A ORDEM do enum e' a ordem de compra pretendida (tier): o custo sai de
    /// Upgrades.Cost(tier). Cada um muda o FLUXO (desbloqueia estacao, contrata, remove caminhada) ou um numero
    /// que desloca o gargalo; a raia B pediu decisao real de onde investir, nao so "estacao vermelha = upgrade".
    /// </summary>
    public enum Upgrade
    {
        FurnaceSpeed1,   // §3 2:30  fornalha 4,0 s -> 2,5 s
        Anvil2,          // §3 3:30  2a bigorna (espadas)
        Helper1,         // §3 4:30  ajudante 1: deposito -> fornalhas
        Shields,         // §3 5:30  2a linha: bancada de escudos (2 lingotes, paga 25)
        PlayerCapacity,  // mochila 3 -> 6
        Helper2,         // ajudante 2: fornalhas -> bancadas
        Conveyor,        // §3 8:30  esteira fornalha A -> bigorna A (remove caminhada)
        FurnaceSpeed2,   // fornalha 2,5 s -> 1,6 s
        PlayerSpeed,     // botas 3,0 -> 4,2 m/s
        Tools,           // 3a linha: bancada de ferramentas (1 lingote, paga 16)
        CounterCapacity, // vitrine: estoque 5 -> 10, clientes 30% mais frequentes (a fila saiu dela na FASE7: virou o balcao evolutivo)
        Furnace2,        // 2a fornalha
        Helper3,         // ajudante 3: bancadas -> balcao
        HelperSpeed,     // ajudantes 2,4 -> 3,4 m/s e carga 2 -> 4
        HammerSpeed,     // bancadas 2,0 s -> 1,3 s
        // 2a area (docs/AREA2_JOALHERIA.md): no FIM do enum, saves antigos com 15 flags continuam validos
        SideCorridor,    // §3 6:30  abre a rua lateral (x 9-15)
        Jewelry,         // §3 9:30  4a linha: bancada de joias (2 lingotes, paga 60) + loja de joias
        // 2a area, fase 2 (docs/AREA2_FASE2.md): ANEXADOS, saves de 15/17 flags continuam validos
        Jeweler,         // joalheiro (ajudante papel 3): lingote -> bancada de joias, joia -> loja de joias
        JewelSpeed,      // lupa: bancada de joias x0,6
        JewelVitrine,    // vitrine de joias: fila 3 -> 5, nobres x0,7
        WorkshopFacade, // luxo, anexado: entrada nobre, sem bonus de producao
        WorkshopFloor,  // luxo: piso de oficina
        JewelryDecor,   // luxo: adornos da joalheria
        // fase 4 (docs/FASE4_MINERIO.md): PRODUTIVOS anexados depois dos luxos; saves de 15/17/20/23 flags continuam validos
        Miner,          // mineiro: 2o ajudante papel 0 (deposito -> fornalhas)
        Jeweler2,       // joalheiro 2: 2o ajudante papel 3
        // FASE7 (docs/FASE7_CARGA_BALCAO.md): balcao evolutivo no MENU, +1 vaga cada, em cadeia. PRODUTIVOS anexados no fim: saves de
        // 15/17/20/23/25 flags continuam validos
        Counter5,       // balcao 4 -> 5 vagas
        Counter6,       // 5 -> 6
        Counter7,       // 6 -> 7
        Counter8        // 7 -> 8
    }

    /// <summary>Eventos de um tick, para a view (SFX, particulas, diario). A = item/upgrade/estacao, B = detalhe.
    /// Milestone: bau de marco apareceu (A = indice em Balance.Milestones, B = ouro); ChestOpened: o jogador abriu (A = indice, B = ouro).
    /// FASE8 (anexados): VipArrived = o VIP entrou numa vaga do balcao (A = item, B = unidades do pacote); VipServed = levou o pacote todo
    /// (A = item, B = ouro que pagou); VipLeft = cansou antes (A = item, B = unidades que faltaram). Cada unidade do VIP sai tambem como Sold (B = 3x o preco).
    /// FASE9 (anexados): OrderNew = encomenda nova (A = item, B = quantas vender); OrderDone = entregue (A = item, B = premio em ouro).</summary>
    public enum Ev { Picked, Deposited, Crafted, Sold, Bought, ClientArrived, ClientLeft, Bottleneck, Hired, Unlocked, Offline, Milestone, ChestOpened, VipArrived, VipServed, VipLeft, OrderNew, OrderDone }

    /// <summary>Dica contextual da HUD (a view so traduz). Arg = indice do pad (BuyPad), do item, do bau (OpenChest) ou do upgrade (BuyMenu).
    /// BuyMenu ANEXADO no fim (FASE6): a compra mais barata pagavel agora e' um upgrade do menu inferior.</summary>
    public enum Hint { GrabOre, OreToFurnace, PickIngots, IngotToCrafter, PickProducts, ProductToCounter, BuyPad, ClientWaiting, OpenChest, BuyMenu }

    public struct V2
    {
        public float X, Y;
        public V2(float x, float y) { X = x; Y = y; }
        public float Len => (float)Math.Sqrt(X * X + Y * Y);
        public static V2 operator -(V2 a, V2 b) => new V2(a.X - b.X, a.Y - b.Y);
        public static V2 operator +(V2 a, V2 b) => new V2(a.X + b.X, a.Y + b.Y);
        public static V2 operator *(V2 a, float k) => new V2(a.X * k, a.Y * k);
        public static float Dist(V2 a, V2 b) => (a - b).Len;
        public override string ToString() => X.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," + Y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Corpo solido no chao (docs/FASE5_FISICA_PACIENCIA.md §2): caixa alinhada aos eixos, centro e meia-medida em m.</summary>
    public struct Box
    {
        public V2 Pos, Half;
        public Box(V2 pos, V2 half) { Pos = pos; Half = half; }
        /// <summary>Ponto da caixa mais perto de `p` (o proprio `p` se estiver dentro).</summary>
        public V2 Closest(V2 p) => new V2(Math.Max(Pos.X - Half.X, Math.Min(p.X, Pos.X + Half.X)), Math.Max(Pos.Y - Half.Y, Math.Min(p.Y, Pos.Y + Half.Y)));
        /// <summary>Distancia de `p` ate a caixa (0 dentro).</summary>
        public float Dist(V2 p) => V2.Dist(p, Closest(p));
    }

    /// <summary>Decoracao solida: `Body` no mapa enquanto `Requires` (upgrade) estiver comprado; -1 = sempre. A view desenha pela mesma geometria.</summary>
    public sealed class ObstacleDef
    {
        public readonly string Name; public readonly Box Body; public readonly int Requires;
        public ObstacleDef(string name, float x, float y, float hx, float hy, int requires = -1) { Name = name; Body = new Box(new V2(x, y), new V2(hx, hy)); Requires = requires; }
    }

    /// <summary>
    /// Numeros do jogo. Todos HIPOTESE: o Bot (BalanceTests) e' quem confirma que a §3 do GDD bate; os valores
    /// medidos estao em docs/BALANCE.md. Mudou aqui, roda `dotnet test` e atualiza o BALANCE.md.
    /// </summary>
    public static class Balance
    {
        public const float WorkshopW = 9f;                     // oficina (x 0-9): o jogador fica nela ate comprar o Corredor
        public const float WorldW = 15f, WorldH = 14f;        // metros, retrato; x 9-15 = rua lateral (2a area)
        public const float PadRadius = 0.6f;
        // Fisica (docs/FASE5_FISICA_PACIENCIA.md §2/§2b): estacao = caixa solida; personagem = circulo; interacao so nas bocas
        public static readonly V2 StationHalf = new V2(0.65f, 0.40f);   // corpo de toda estacao (base do sprite de 1,65 m)
        public const float CharRadius = 0.25f;                  // jogador, ajudantes e bot
        public const float MouthRadius = 0.4f;                  // zona da boca: centro do personagem a ate 0,4 m do ponto da boca (encostado)
        public const float WorkerReach = 0.3f;                  // ajudante para a 0,3 m do ponto da boca (dentro da zona)
        public const float PlayerSpeed = 3f, PlayerSpeedUp = 4.2f;
        public const int PlayerCap = 3, PlayerCapUp = 6;
        public const float WorkerSpeed = 2.4f, WorkerSpeedUp = 3.4f;
        public const int WorkerCap = 2, WorkerCapUp = 4;
        public const float WorkerPatience = 1.2f;              // s parado na fonte vazia antes de ir entregar o que tem
        public const float GrabTime = 0.4f;                     // 1 minerio do deposito
        public const float TransferTime = 0.2f;                 // 1 item pego/depositado
        public const float FurnaceTime0 = 4f, FurnaceTime1 = 2.5f, FurnaceTime2 = 1.6f;
        public const float HammerTime0 = 5f, HammerTime1 = 3f;    // bigorna mais lenta que a fornalha com fole: a 2a bigorna "reduz a fila" (§3 3:30)
        public const float ShieldTimeMul = 1.5f, JewelTimeMul = 2f;
        public const int FurnaceIn = 6, FurnaceOut = 4, CrafterIn = 4, CrafterOut = 3;
        public const int CounterCap0 = 5, CounterCap1 = 10;     // estoque do balcao por produto; a Vitrine dobra
        public const int QueueCap0 = 4, QueueCapMax = 8;        // vagas do balcao (clientes atendidos lado a lado): 4 + 1 por evolucao (FASE7 §2)
        public const float SlotStep = 0.85f, CounterEnd = 0.2f; // m entre vagas; ponta do balcao alem da ultima vaga
        /// <summary>Meia-largura do corpo do balcao com `slots` vagas: uma vaga = 0,85 m de balcao + as pontas. 4 vagas: x 2,6-6,4; 8: x 0,9-8,1.</summary>
        public static float CounterHalfX(int slots) => SlotStep * slots / 2f + CounterEnd;
        public const float PatienceBase = 30f, PatiencePerSecond = 3f;   // paciencia(item) = base + k x producao inicial (FASE5 §1, calibrado no BALANCE.md §13)
        public const int JewelQueueCap = 3;                     // fila da loja de joias (nobres: menos gente)
        public const int JewelQueueCapUp = 5; public const float JewelVitrineClientMul = 0.7f;   // Vitrine de joias: fila 3 -> 5, nobres x0,7 (soma com a Vitrine)
        public const float JewelSpeedMul = 0.6f;                // Lupa: tempo da bancada de joias x0,6
        public const float ServeTime = 0.4f;                    // s entre duas vendas no balcao
        public const float FirstClientAt = 8f;                  // o 1o cliente de espada chega cedo (§3: venda em <90 s)
        public static readonly float[] ClientInterval = { 0, 0, 6f, 9f, 8f, 14f }; // s entre clientes, por linha, no inicio
        public const float FamePer10Sales = 0.94f, FameFloor = 0.4f; // "fama": a cada 10 vendas os clientes chegam 6% mais rapido, ate 2,5x
        public const float VitrineClientMul = 0.7f;             // CounterCapacity atrai mais gente
        public const float ConveyorTime = 1f;                   // s por lingote na esteira
        public const float PadDrainSeconds = 1f;                // tempo para pagar um pad inteiro com ouro na mao
        public const float PadDwell = 0.5f;                     // andando por cima, so paga depois deste tempo (atravessar nao gasta); parado paga na hora
        public const int CostBase = 50; public const float CostGrowth = 1.30f; // custo(tier) = 50 * 1,30^tier, arredondado a 5 (varredura em docs/BALANCE.md)
        public const int SideCorridorCost = 690, JewelryCost = 1515;          // 2a area: fora da formula (coordenador 2026-10-07, BALANCE.md §9)
        public const int JewelerCost = 2200, JewelSpeedCost = 5500, JewelVitrineCost = 9000;   // fase 2: decisao aprovada (BALANCE.md §10.7)
        public const int JewelPriceUp = 80;                     // Vitrine de joias: nobres pagam mais, sem mudar as outras linhas
        public const int WorkshopFacadeCost = 14000, WorkshopFloorCost = 18000, JewelryDecorCost = 22000;   // luxo: Leva 11, Fachada +8,7 min depois da producao (BALANCE.md §15)
        public const int MinerCost = 3000, Jeweler2Cost = 2400;   // fase 4: regra de retorno 8-12 min, varredura em BALANCE.md §14
        // FASE7: balcao 5/6/7/8 vagas. Ganho medido 4 -> 8 = +89 ouro/min comprando aos 15 min (0 antes dos 10 e depois dos 30): soma 750
        // = retorno 8,4 min; o Balcao 5 exige a Vitrine para nao cair nos primeiros 10 min, onde nao rende (BALANCE.md §17)
        public static readonly int[] CounterSlotCost = { 150, 175, 200, 225 };
        public const int OfflineCapSeconds = 7200; public const float OfflineFactor = 0.25f;   // raia B §5 + coordenador 2026-10-06: teto 2 h, 25% da taxa online
        public const float OfflineMaxNextUpgrades = 2f;         // e nunca mais que 2x o preco do upgrade mais barato ainda travado
        public const float RateTau = 60f;                       // s da media movel de ouro/s (taxa online)
        // FASE8 (docs/FASE8_VIP_VELOCIDADE.md, medido no BALANCE.md §18): cliente VIP e velocidade por anuncio
        public const float VipMin = 240f, VipMax = 360f;        // s entre VIPs (sorteio deterministico); o relogio so anda depois da 1a venda
        public const int VipPackMin = 3, VipPriceMul = 3;       // pacote de no minimo 3 unidades (no maximo a prateleira, CounterCap) de um produto do balcao, 3x o preco cada
        public const float VipShare = 0.07f;                    // o pacote vale ~7% do ouro de um intervalo medio entre VIPs (sweep de 3/5/6 fixos e 5/7/10%: BALANCE §18)
        public const float VipPatienceBase = 90f, VipPatiencePerUnit = 6f;   // rodada 3: paciencia do VIP = 90 s + 6 s por unidade do pacote (3 = 108 s, 10 = 150 s, 20 = 210 s)
        /// <summary>Paciencia do VIP com `units` no pacote (natural e chamado). O cliente normal tem 57-77 s.</summary>
        public static float VipPatienceFor(int units) => VipPatienceBase + VipPatiencePerUnit * units;
        public const float VipSummonMinIn = 60f;                // chamar por anuncio: so sem VIP ativo e com o proximo natural a mais de 60 s
        public const int VipSummonPackMul = 2, VipSummonPackMax = 20;   // o VIP chamado e' EXTRA (nao mexe no relogio natural) e traz 2x o pacote do natural, ate 20 (coordenador)
        public const float BoostSeconds = 60f, BoostMax = 3f, BoostPlayerMax = 1.3f;   // 1 anuncio = 2x por 60 s, o 2o sobe para 3x; o ferreiro no maximo 1,3x
        // FASE9 (docs/FASE9_ENCOMENDAS.md): encomenda = vender N de uma linha aberta; uma por vez, sem prazo nem falha
        public const float OrderFirstDelay = 45f, OrderGap = 20f;   // s depois da 1a venda; s entre uma entregue e a proxima
        public const int OrderFirstTarget = 5, OrderMin = 3, OrderMax = 30;   // a 1a e' 5 espadas; depois N = OrderSeconds da renda da linha
        public const float OrderSeconds = 150f, OrderRewardSeconds = 10f;     // premio ~10 s da taxa online: 4,2% da receita no bot de 60 min (BALANCE §20)
        public const int OrderRewardMin = 25;
        public const float BoostCooldownSeconds = 300f;         // recarga: 5 min de jogo contados do FIM de cada boost (coordenador; sem ela, 2x renovado fechava a producao aos 26:56)
        public static readonly int[] Price = { 0, 0, 10, 25, 16, 60 };     // ouro por item vendido
        public static readonly int[] IngotsPer = { 0, 0, 1, 2, 1, 2 };     // lingotes por produto
        public static readonly string[] ItemName = { "minério", "lingote", "espada", "escudo", "ferramenta", "joia" };

        /// <summary>Segundos na fila antes de desistir: base + k x tempo de producao da linha no inicio (fornalha inicial x
        /// lingotes + bancada inicial). Espada 9 s, escudo 15,5 s, ferramenta 9 s, joia 18 s.</summary>
        public static float PatienceFor(Item item) => PatienceBase + PatiencePerSecond *
            (FurnaceTime0 * IngotsPer[(int)item] + HammerTime0 * (item == Item.Shield ? ShieldTimeMul : item == Item.Jewel ? JewelTimeMul : 1f));

        /// <summary>Parede direita da oficina (x 9,0-9,6, y 0-14), solida, com duas aberturas em pares y0, y1: a porta lateral na
        /// altura da joalheria (5,6-7,4) e o arco (11,6-13,4). As pontas dos segmentos sao os batentes (o arco nao tem poste solto).</summary>
        public const float SideWallX0 = 9.0f, SideWallX1 = 9.6f;
        public static readonly float[] SideWallOpenings = { 5.6f, 7.4f, 11.6f, 13.4f };

        static ObstacleDef Wall(float y0, float y1) => new ObstacleDef("parede", (SideWallX0 + SideWallX1) / 2f, (y0 + y1) / 2f, (SideWallX1 - SideWallX0) / 2f, (y1 - y0) / 2f);

        /// <summary>
        /// Decoracao solida (FASE5 §2), mesma geometria da view: carroca e os 3 segmentos da parede sempre; pedestais da Joalheria
        /// real e postes da Fachada nobre so depois da compra. Medidas = pe do sprite no chao (WorldView: CartPos, Pedestals, Fachada).
        /// </summary>
        public static readonly ObstacleDef[] Obstacles =
        {
            new ObstacleDef("carroca", 14.2f, 3.0f, 0.65f, 0.40f),
            Wall(0f, SideWallOpenings[0]),
            Wall(SideWallOpenings[1], SideWallOpenings[2]),
            Wall(SideWallOpenings[3], WorldH),
            new ObstacleDef("pedestal", 10.95f, 8.4f, 0.2f, 0.1f, (int)Upgrade.JewelryDecor),
            new ObstacleDef("pedestal", 13.05f, 8.4f, 0.2f, 0.1f, (int)Upgrade.JewelryDecor),
            new ObstacleDef("pedestal", 10.95f, 10.9f, 0.2f, 0.1f, (int)Upgrade.JewelryDecor),
            new ObstacleDef("pedestal", 13.05f, 10.9f, 0.2f, 0.1f, (int)Upgrade.JewelryDecor),
            new ObstacleDef("poste_fachada", 0.15f, 14.1f, 0.16f, 0.06f, (int)Upgrade.WorkshopFacade),
            new ObstacleDef("poste_fachada", 8.85f, 14.1f, 0.16f, 0.06f, (int)Upgrade.WorkshopFacade),
        };

        /// <summary>
        /// Baus de marco (docs/AREA2_FASE2.md §3), na ordem de exibicao. Um bau disponivel por vez em cada posicao. O 1o e'
        /// 15 espadas e nao 10 (desvio medido, BALANCE.md §10): com 10 o bau abre aos 1:36 e a 2a bigorna cai abaixo do piso da §3.
        /// </summary>
        // oficina: (2,8; 12,6) -> (2,2; 12,0) na FASE7, o balcao de 4 vagas (x 2,6-6,4) cobria o bau; rua: (14,2; 13,0) -> (10,2; 9,6), coordenador
        public static readonly V2 WorkshopChest = new V2(2.2f, 12.0f), StreetChest = new V2(10.2f, 9.6f);
        public static readonly MilestoneDef[] Milestones =
        {
            new MilestoneDef((int)Item.Sword, 15, 60, WorkshopChest, "15 espadas!"),
            new MilestoneDef(-1, 50, 250, WorkshopChest, "50 vendas!"),
            new MilestoneDef((int)Item.Jewel, 10, 600, StreetChest, "10 joias!"),
            new MilestoneDef(-1, 200, 1200, WorkshopChest, "200 vendas!"),
        };
    }

    /// <summary>Marco: vender `Count` de `Item` (-1 = qualquer produto) paga `Gold` num bau em `Pos`.</summary>
    public sealed class MilestoneDef
    {
        public readonly int Item, Count, Gold; public readonly V2 Pos; public readonly string Label;
        public MilestoneDef(int item, int count, int gold, V2 pos, string label) { Item = item; Count = count; Gold = gold; Pos = pos; Label = label; }
    }

    public sealed class UpgradeDef
    {
        public readonly Upgrade Id;
        public readonly int Requires;   // indice de outro upgrade ou -1
        public readonly string Name, Desc;
        public readonly bool Luxury;    // luxo: so depois de TODOS os produtivos, fora de ProductionComplete e do teto offline
        public readonly bool InMenu;    // FASE6: comprado por toque no menu inferior (Sim.TryBuyMenu), nunca em pad; o pad antigo fica na lista (save por indice)
        public UpgradeDef(Upgrade id, int requires, string name, string desc, bool luxury = false, bool menu = false) { Id = id; Requires = requires; Name = name; Desc = desc; Luxury = luxury; InMenu = menu; }
    }

    public static class Upgrades
    {
        public const int ProductionCount = 26, Count = 29;   // ProductionCount = quantos NAO sao luxo (os IDs nao precisam ser contiguos)

        /// <summary>Marca por upgrade, nao por ID: um produtivo anexado depois dos luxos continua produtivo.</summary>
        public static bool IsLuxury(int u) => All[u].Luxury;

        public static readonly UpgradeDef[] All =
        {
            new UpgradeDef(Upgrade.FurnaceSpeed1, -1, "Fole", "Fornalha mais rápida", menu: true),
            new UpgradeDef(Upgrade.Anvil2, -1, "Bigorna 2", "Mais espadas por minuto"),
            new UpgradeDef(Upgrade.Helper1, -1, "Ajudante", "Leva minério à fornalha"),
            new UpgradeDef(Upgrade.Shields, -1, "Escudos", "Nova linha: 2 lingotes, paga 25"),
            new UpgradeDef(Upgrade.PlayerCapacity, -1, "Mochila", "Carrega 6 em vez de 3", menu: true),
            new UpgradeDef(Upgrade.Helper2, (int)Upgrade.Helper1, "Ajudante 2", "Leva lingotes às bancadas"),
            new UpgradeDef(Upgrade.Conveyor, -1, "Esteira", "Fornalha → bigorna sem andar"),
            new UpgradeDef(Upgrade.FurnaceSpeed2, (int)Upgrade.FurnaceSpeed1, "Fole duplo", "Fornalha ainda mais rápida", menu: true),
            new UpgradeDef(Upgrade.PlayerSpeed, -1, "Botas", "Você anda mais rápido", menu: true),
            new UpgradeDef(Upgrade.Tools, (int)Upgrade.Shields, "Ferramentas", "Nova linha: 1 lingote, paga 16"),
            new UpgradeDef(Upgrade.CounterCapacity, -1, "Vitrine", "Mais estoque e mais clientes", menu: true),
            new UpgradeDef(Upgrade.Furnace2, (int)Upgrade.Anvil2, "Fornalha 2", "Dobra os lingotes"),
            new UpgradeDef(Upgrade.Helper3, (int)Upgrade.Helper2, "Ajudante 3", "Leva produtos ao balcão"),
            new UpgradeDef(Upgrade.HelperSpeed, (int)Upgrade.Helper1, "Ajudantes ágeis", "Mais rápidos e carregam 4", menu: true),
            new UpgradeDef(Upgrade.HammerSpeed, -1, "Martelo veloz", "Bancadas mais rápidas", menu: true),
            new UpgradeDef(Upgrade.SideCorridor, -1, "Corredor", "Abre a rua lateral"),
            new UpgradeDef(Upgrade.Jewelry, (int)Upgrade.SideCorridor, "Joalheria", "Nova linha: 2 lingotes, paga 60"),
            new UpgradeDef(Upgrade.Jeweler, (int)Upgrade.Jewelry, "Joalheiro", "Ajudante leva lingotes à joalheria e joias à loja"),
            new UpgradeDef(Upgrade.JewelSpeed, (int)Upgrade.Jewelry, "Lupa", "Joalheria mais rápida", menu: true),
            new UpgradeDef(Upgrade.JewelVitrine, (int)Upgrade.Jewelry, "Vitrine de joias", "Nobres pagam 80 e a fila cresce", menu: true),
            new UpgradeDef(Upgrade.WorkshopFacade, (int)Upgrade.JewelVitrine, "Fachada nobre", "Entrada dourada e bandeirolas", luxury: true),
            new UpgradeDef(Upgrade.WorkshopFloor, (int)Upgrade.WorkshopFacade, "Piso de oficina", "Ladrilhos para valorizar a oficina", luxury: true),
            new UpgradeDef(Upgrade.JewelryDecor, (int)Upgrade.WorkshopFloor, "Joalheria real", "Tapetes e adornos para a joalheria", luxury: true),
            new UpgradeDef(Upgrade.Miner, (int)Upgrade.Jeweler, "Mineiro", "Mais um ajudante leva minério às fornalhas"),
            new UpgradeDef(Upgrade.Jeweler2, (int)Upgrade.Miner, "Joalheiro 2", "Mais um ajudante na joalheria"),
            new UpgradeDef(Upgrade.Counter5, (int)Upgrade.CounterCapacity, "Balcão 5 vagas", "Atende 5 clientes ao mesmo tempo", menu: true),
            new UpgradeDef(Upgrade.Counter6, (int)Upgrade.Counter5, "Balcão 6 vagas", "Atende 6 clientes ao mesmo tempo", menu: true),
            new UpgradeDef(Upgrade.Counter7, (int)Upgrade.Counter6, "Balcão 7 vagas", "Atende 7 clientes ao mesmo tempo", menu: true),
            new UpgradeDef(Upgrade.Counter8, (int)Upgrade.Counter7, "Balcão 8 vagas", "Atende 8 clientes ao mesmo tempo", menu: true),
        };

        /// <summary>custo(tier) = CostBase * CostGrowth^tier, arredondado ao multiplo de 5 (preco legivel no pad).</summary>
        public static int Cost(int tier)
        {
            // ponytail: a 2a area sai da formula porque fica no FIM do enum (save antigo de 15/17 flags) com preco de meio de
            // jogo. Mais uma area assim = trocar por uma tabela de precos por upgrade.
            switch ((Upgrade)tier)
            {
                case Upgrade.SideCorridor: return Balance.SideCorridorCost;
                case Upgrade.Jewelry: return Balance.JewelryCost;
                case Upgrade.Jeweler: return Balance.JewelerCost;
                case Upgrade.JewelSpeed: return Balance.JewelSpeedCost;
                case Upgrade.JewelVitrine: return Balance.JewelVitrineCost;
                case Upgrade.WorkshopFacade: return Balance.WorkshopFacadeCost;
                case Upgrade.WorkshopFloor: return Balance.WorkshopFloorCost;
                case Upgrade.JewelryDecor: return Balance.JewelryDecorCost;
                case Upgrade.Miner: return Balance.MinerCost;
                case Upgrade.Jeweler2: return Balance.Jeweler2Cost;
                case Upgrade.Counter5: case Upgrade.Counter6: case Upgrade.Counter7: case Upgrade.Counter8:
                    return Balance.CounterSlotCost[tier - (int)Upgrade.Counter5];
            }
            double c = Balance.CostBase * Math.Pow(Balance.CostGrowth, tier);
            return Math.Max(5, (int)(Math.Round(c / 5.0) * 5.0));
        }

        public static int Cost(Upgrade u) => Cost((int)u);
    }
}
