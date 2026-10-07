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
        CounterCapacity, // vitrine: estoque 3 -> 6, fila 4 -> 6, clientes 30% mais frequentes
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
        JewelVitrine     // vitrine de joias: fila 3 -> 5, nobres x0,7
    }

    /// <summary>Eventos de um tick, para a view (SFX, particulas, diario). A = item/upgrade/estacao, B = detalhe.
    /// Milestone: bau de marco apareceu (A = indice em Balance.Milestones, B = ouro); ChestOpened: o jogador abriu (A = indice, B = ouro).</summary>
    public enum Ev { Picked, Deposited, Crafted, Sold, Bought, ClientArrived, ClientLeft, Bottleneck, Hired, Unlocked, Offline, Milestone, ChestOpened }

    /// <summary>Dica contextual da HUD (a view so traduz). Arg = indice do pad, do item ou (OpenChest) do bau.</summary>
    public enum Hint { GrabOre, OreToFurnace, PickIngots, IngotToCrafter, PickProducts, ProductToCounter, BuyPad, ClientWaiting, OpenChest }

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

    /// <summary>
    /// Numeros do jogo. Todos HIPOTESE: o Bot (BalanceTests) e' quem confirma que a §3 do GDD bate; os valores
    /// medidos estao em docs/BALANCE.md. Mudou aqui, roda `dotnet test` e atualiza o BALANCE.md.
    /// </summary>
    public static class Balance
    {
        public const float WorkshopW = 9f;                     // oficina (x 0-9): o jogador fica nela ate comprar o Corredor
        public const float WorldW = 15f, WorldH = 14f;        // metros, retrato; x 9-15 = rua lateral (2a area)
        public const float StationRadius = 0.9f, PadRadius = 0.6f;
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
        public const int CounterCap0 = 5, CounterCap1 = 10, QueueCap0 = 4, QueueCap1 = 6;
        public const float Patience = 25f;                      // s na fila antes de desistir
        public const int JewelQueueCap = 3; public const float JewelPatience = 40f;   // fila da loja de joias (nobres: menos gente, mais paciencia)
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
        public const int OfflineCapSeconds = 7200; public const float OfflineFactor = 0.25f;   // raia B §5 + coordenador 2026-10-06: teto 2 h, 25% da taxa online
        public const float OfflineMaxNextUpgrades = 2f;         // e nunca mais que 2x o preco do upgrade mais barato ainda travado
        public const float RateTau = 60f;                       // s da media movel de ouro/s (taxa online)
        public static readonly int[] Price = { 0, 0, 10, 25, 16, 60 };     // ouro por item vendido
        public static readonly int[] IngotsPer = { 0, 0, 1, 2, 1, 2 };     // lingotes por produto
        public static readonly string[] ItemName = { "minério", "lingote", "espada", "escudo", "ferramenta", "joia" };

        /// <summary>
        /// Baus de marco (docs/AREA2_FASE2.md §3), na ordem de exibicao. Um bau disponivel por vez em cada posicao. O 1o e'
        /// 15 espadas e nao 10 (desvio medido, BALANCE.md §10): com 10 o bau abre aos 1:36 e a 2a bigorna cai abaixo do piso da §3.
        /// </summary>
        public static readonly V2 WorkshopChest = new V2(2.8f, 12.6f), StreetChest = new V2(10.2f, 9.6f);   // rua: (14,2; 13,0) -> (10,2; 9,6), coordenador (sobreposicao na view)
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
        public UpgradeDef(Upgrade id, int requires, string name, string desc) { Id = id; Requires = requires; Name = name; Desc = desc; }
    }

    public static class Upgrades
    {
        public const int Count = 20;

        public static readonly UpgradeDef[] All =
        {
            new UpgradeDef(Upgrade.FurnaceSpeed1, -1, "Fole", "Fornalha mais rápida"),
            new UpgradeDef(Upgrade.Anvil2, -1, "2ª bigorna", "Mais espadas por minuto"),
            new UpgradeDef(Upgrade.Helper1, -1, "Ajudante", "Leva minério à fornalha"),
            new UpgradeDef(Upgrade.Shields, -1, "Escudos", "Nova linha: 2 lingotes, paga 25"),
            new UpgradeDef(Upgrade.PlayerCapacity, -1, "Mochila", "Carrega 6 em vez de 3"),
            new UpgradeDef(Upgrade.Helper2, (int)Upgrade.Helper1, "Ajudante 2", "Leva lingotes às bancadas"),
            new UpgradeDef(Upgrade.Conveyor, -1, "Esteira", "Fornalha → bigorna sem andar"),
            new UpgradeDef(Upgrade.FurnaceSpeed2, (int)Upgrade.FurnaceSpeed1, "Fole duplo", "Fornalha ainda mais rápida"),
            new UpgradeDef(Upgrade.PlayerSpeed, -1, "Botas", "Você anda mais rápido"),
            new UpgradeDef(Upgrade.Tools, (int)Upgrade.Shields, "Ferramentas", "Nova linha: 1 lingote, paga 16"),
            new UpgradeDef(Upgrade.CounterCapacity, -1, "Vitrine", "Mais estoque, fila e clientes"),
            new UpgradeDef(Upgrade.Furnace2, (int)Upgrade.Anvil2, "2ª fornalha", "Dobra os lingotes"),
            new UpgradeDef(Upgrade.Helper3, (int)Upgrade.Helper2, "Ajudante 3", "Leva produtos ao balcão"),
            new UpgradeDef(Upgrade.HelperSpeed, (int)Upgrade.Helper1, "Ajudantes ágeis", "Mais rápidos e carregam 4"),
            new UpgradeDef(Upgrade.HammerSpeed, -1, "Martelo veloz", "Bancadas mais rápidas"),
            new UpgradeDef(Upgrade.SideCorridor, -1, "Corredor", "Abre a rua lateral"),
            new UpgradeDef(Upgrade.Jewelry, (int)Upgrade.SideCorridor, "Joalheria", "Nova linha: 2 lingotes, paga 60"),
            new UpgradeDef(Upgrade.Jeweler, (int)Upgrade.Jewelry, "Joalheiro", "Ajudante leva lingotes à joalheria e joias à loja"),
            new UpgradeDef(Upgrade.JewelSpeed, (int)Upgrade.Jewelry, "Lupa", "Joalheria mais rápida"),
            new UpgradeDef(Upgrade.JewelVitrine, (int)Upgrade.Jewelry, "Vitrine de joias", "Nobres pagam 80 e a fila cresce"),
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
            }
            double c = Balance.CostBase * Math.Pow(Balance.CostGrowth, tier);
            return Math.Max(5, (int)(Math.Round(c / 5.0) * 5.0));
        }

        public static int Cost(Upgrade u) => Cost((int)u);
    }
}
