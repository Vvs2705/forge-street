using System;

namespace FS.Core
{
    /// <summary>Itens em ordem de cadeia: minerio -> lingote -> produto (espada, escudo, ferramenta; joia na 2a area). B-CORE-01: o item e' dado
    /// (Items.All); o enum e' so o atalho das primeiras posicoes da tabela (o handle de um item e' a posicao dele, (Item)8 vale). B-CORE-07: a
    /// espada longa (linha Meta da bigorna longa, desbloqueada por projeto) e' a 7a.</summary>
    public enum Item { Ore = 0, Ingot = 1, Sword = 2, Shield = 3, Tool = 4, Jewel = 5, LongSword = 6 }

    /// <summary>C-CORE-07 (anexado): Quota = o patio de cotas do Royal Arsenal (CONTEUDO_V1 §2.4; Sim.Cotas.cs): recebe produto, nao produz.</summary>
    public enum Kind { Deposit, Furnace, Crafter, Counter, Quota }

    /// <summary>
    /// Os 15 upgrades do MVP (GDD §18/§19) + 2 da 2a area (v0.2) + 3 da joalheria (fase 2). A ORDEM do enum e' a ordem de compra pretendida (tier): o custo sai de
    /// Tuning.Cost(tier). Cada um muda o FLUXO (desbloqueia estacao, contrata, remove caminhada) ou um numero
    /// que desloca o gargalo; a raia B pediu decisao real de onde investir, nao so "estacao vermelha = upgrade".
    /// A-CORE-04: o save grava o UpgradeDef.Id (string), nunca este numero; os comentarios "saves de N flags" abaixo sao do schema 1 (0.1-0.7).
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
        // 2a area (docs/archive/AREA2_JOALHERIA.md): no FIM do enum, saves antigos com 15 flags continuam validos
        SideCorridor,    // §3 6:30  abre a rua lateral (x 9-15)
        Jewelry,         // §3 9:30  4a linha: bancada de joias (2 lingotes, paga 60) + loja de joias
        // 2a area, fase 2 (docs/archive/AREA2_FASE2.md): ANEXADOS, saves de 15/17 flags continuam validos
        Jeweler,         // joalheiro (ajudante papel 3): lingote -> bancada de joias, joia -> loja de joias
        JewelSpeed,      // lupa: bancada de joias x0,6
        JewelVitrine,    // vitrine de joias: fila 3 -> 5, nobres x0,7
        WorkshopFacade, // luxo, anexado: entrada nobre, sem bonus de producao
        WorkshopFloor,  // luxo: piso de oficina
        JewelryDecor,   // luxo: adornos da joalheria
        // fase 4 (docs/archive/FASE4_MINERIO.md): PRODUTIVOS anexados depois dos luxos; saves de 15/17/20/23 flags continuam validos
        Miner,          // mineiro: 2o ajudante papel 0 (deposito -> fornalhas)
        Jeweler2,       // joalheiro 2: 2o ajudante papel 3
        // FASE7 (docs/archive/FASE7_CARGA_BALCAO.md): balcao evolutivo no MENU, +1 vaga cada, em cadeia. PRODUTIVOS anexados no fim: saves de
        // 15/17/20/23/25 flags continuam validos
        Counter5,       // balcao 4 -> 5 vagas
        Counter6,       // 5 -> 6
        Counter7,       // 6 -> 7
        Counter8,       // 7 -> 8
        // B-CORE-03 (ECONOMIA_WAVE_B §1.5): niveis 2-3 de cada familia de estacao, categoria Meta (fora da producao completa e do teto
        // offline), no menu depois das Ferramentas. Os niveis 4-5 exigem projeto (bp_bigorna_mestra/bp_bancada_mestra): entram com o B-CORE-07
        AnvilsLv2, AnvilsLv3, ShieldsLv2, ShieldsLv3, ToolsLv2, ToolsLv3, JewelsLv2, JewelsLv3,
        // B-CORE-07 (ECONOMIA_WAVE_B §1.5, CONTEUDO_V1 §5.1): niveis IV-V (UpgradeDef.Blueprint = o projeto de mestre da familia), Meta, logo depois
        // dos II-III (a faixa AnvilsLv2..JewelsLv5 e' o Tuning.NivelEstacaoCusto); e a bigorna longa (pad, Meta: linha da espada longa, com projeto)
        AnvilsLv4, AnvilsLv5, ShieldsLv4, ShieldsLv5, ToolsLv4, ToolsLv5, JewelsLv4, JewelsLv5, LongAnvil
    }

    /// <summary>B-CORE-03 (GDD §9): nivel de qualidade de uma peca, sem sorteio. Normal, Fina e Superior circulam (pilha, mao, balcao) e vendem
    /// por Tuning.QualidadePct; Masterwork so pelo golpe do ferreiro numa Superior com projeto e condicoes (B-CORE-08), vai ao Hall e nunca e' vendida.</summary>
    public enum Quality { Normal, Fine, Superior, Masterwork }

    /// <summary>Fontes dos pontos de qualidade de uma estacao (DM §15, Sim.QualityBreakdown). Existem o nivel da estacao (B-CORE-03) e a tecnica
    /// (Blueprint da familia, B-CORE-07); oficial e fundidor (B-CORE-04, C-CORE-05), liga (Material, C-CORE-05) e buff (mestre de obras, evento).
    /// C-CORE-08 (anexado): Energizada = a peca do altar no modo Forcado (Sim.Arcano.cs; CONTEUDO_V1 §2.5 regra 4).</summary>
    public enum QualitySource { StationLevel, Worker, Blueprint, Material, Buff, Energizada }

    /// <summary>Eventos de um tick, para a view (SFX, particulas, diario). A = item/upgrade/estacao, B = detalhe.
    /// Milestone: bau de marco apareceu (A = indice em Balance.Milestones, B = ouro); ChestOpened: o jogador abriu (A = indice, B = ouro).
    /// FASE8 (anexados): VipArrived = o VIP entrou numa vaga do balcao (A = item, B = unidades do pacote); VipServed = levou o pacote todo
    /// (A = item, B = ouro que pagou); VipLeft = cansou antes (A = item, B = unidades que faltaram). Cada unidade do VIP sai tambem como Sold (B = 3x o preco).
    /// FASE9 (anexados): OrderNew = encomenda nova (A = item, B = quantas vender); OrderDone = entregue (A = item, B = premio em ouro).
    /// B-CORE-03 (anexados): Graded = peca acima de Normal na pilha (A = estacao, B = Quality); Masterwork = obra-prima foi ao Hall (A = item, B = estacao).
    /// B-CORE-04: Hired = ajudante do upgrade (A = indice em Sim.Workers, B = 0) ou worker da Equipe (A = indice em WorkerDefs, B = WorkerRole,
    /// nunca 0); WorkerUp (anexado) = subiu de nivel (A = indice em WorkerDefs, B = nivel novo).
    /// B-CORE-07 (anexado): Blueprint = projeto novo, 1x por ID (A = indice em Projetos.Defs, B = BlueprintSourceKind da fonte).
    /// B-CORE-08: Masterwork ganhou o B = indice em Hall.Defs (-1 = obra-prima sem MasterworkDef, so o gancho de teste); Colecao (anexado) =
    /// marco da colecao pago, 1x por ID (A = indice em Colecao.Marcos, B = Renome). D-CORE-04 (anexado): ContractDone = Royal Contract
    /// concluido, 1x por ID (A = indice em Contratos.Defs, B = ouro pago). C-CORE-07 (anexado): QuotaBlock = bloco do patio de cotas fechado e
    /// pago (A = item, B = ouro; cada peca sai tambem como Sold).</summary>
    public enum Ev { Picked, Deposited, Crafted, Sold, Bought, ClientArrived, ClientLeft, Bottleneck, Hired, Unlocked, Offline, Milestone, ChestOpened, VipArrived, VipServed, VipLeft, OrderNew, OrderDone, Graded, Masterwork, WorkerUp, Blueprint, Colecao, ContractDone, QuotaBlock }

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

    /// <summary>Corpo solido no chao (docs/archive/FASE5_FISICA_PACIENCIA.md §2): caixa alinhada aos eixos, centro e meia-medida em m.</summary>
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
    /// Numeros FIXOS do jogo: geometria, IDs, efeitos de upgrade e limites de seguranca (itens e receitas: Items/Recipes). Os de economia e ritmo que o RemoteConfig e
    /// os eventos ajustam estao no Tuning (A-CORE-03). Todos HIPOTESE: o Bot (BalanceTests) e' quem confirma que a §3 do GDD bate; os valores
    /// medidos estao em docs/BALANCE.md. Mudou aqui ou no Tuning, roda `dotnet test` e atualiza o BALANCE.md.
    /// </summary>
    public static class Balance
    {
        public const float WorkshopW = 9f;                     // oficina (x 0-9): o jogador fica nela ate comprar o Corredor
        public const float WorldW = 15f, WorldH = 14f;        // metros, retrato; x 9-15 = rua lateral (2a area)
        public const float PadRadius = 0.6f;
        // Fisica (docs/archive/FASE5_FISICA_PACIENCIA.md §2/§2b): estacao = caixa solida; personagem = circulo; interacao so nas bocas
        public static readonly V2 StationHalf = new V2(0.65f, 0.40f);   // corpo de toda estacao (base do sprite de 1,65 m)
        public const float CharRadius = 0.25f;                  // jogador, ajudantes e bot
        public const float MouthRadius = 0.4f;                  // zona da boca: centro do personagem a ate 0,4 m do ponto da boca (encostado)
        public const float WorkerReach = 0.3f;                  // ajudante para a 0,3 m do ponto da boca (dentro da zona)
        // impasse do Sim.Steer: andando 1 s sem sair de 0,75 m (> 2 passos do mais rapido, ajudante 3x a 30 ticks/s = 0,34 m; quem anda livre sai em < 0,45 s)
        public const float StuckRadius = 0.75f, StuckSeconds = 1f;
        // efeitos de upgrade e de maos: a View pre-aloca as pilhas por PlayerCapUp/WorkerCapUp (viram dado no WorkerDef, B-CORE-04)
        public const float PlayerSpeed = 3f, PlayerSpeedUp = 4.2f;
        public const int PlayerCap = 3, PlayerCapUp = 6;
        public const float WorkerSpeed = 2.4f, WorkerSpeedUp = 3.4f;
        public const int WorkerCap = 2, WorkerCapUp = 4;
        public const float WorkerPatience = 1.2f;              // s parado na fonte vazia antes de ir entregar o que tem
        public const float TransferTime = 0.2f;                 // 1 item pego/depositado
        public const int FurnaceIn = 6, FurnaceOut = 4, CrafterIn = 4, CrafterOut = 3;
        public const int QueueCapMax = 8;                       // trava de geometria: 8 vagas cabem na oficina; Tuning.QueueCap0 + evolucoes nunca passa
        public const float SlotStep = 0.85f, CounterEnd = 0.2f; // m entre vagas; ponta do balcao alem da ultima vaga
        /// <summary>Meia-largura do corpo do balcao com `slots` vagas: uma vaga = 0,85 m de balcao + as pontas. 4 vagas: x 2,6-6,4; 8: x 0,9-8,1.</summary>
        public static float CounterHalfX(int slots) => SlotStep * slots / 2f + CounterEnd;
        public const int JewelQueueCap = 3;                     // fila da loja de joias (nobres: menos gente); geometria: o JewelSlot centra ate 5 vagas
        public const int JewelQueueCapUp = 5;                   // Vitrine de joias: fila 3 -> 5 (o ContentValidator confere as vagas ate aqui)
        public const float ServeTime = 0.4f;                    // s entre duas vendas no balcao
        public const float PadDrainSeconds = 1f;                // tempo para pagar um pad inteiro com ouro na mao
        public const float PadDwell = 0.5f;                     // andando por cima, so paga depois deste tempo (atravessar nao gasta); parado paga na hora
        /// <summary>Guardrail do estudio (offline com teto de 2 h; managers e premium mexem na taxa, nao no tempo): trava do nucleo, NUNCA knob.
        /// O Tuning.OfflineMaxSeconds so encurta (Tuning.OfflineCap).</summary>
        public const int OfflineCapSeconds = 7200;
        public const float RateTau = 60f;                       // s da media movel de ouro/s (taxa online)
        /// <summary>Nome de cada item do conteudo padrao, por handle: atalho da View (diario e dicas); a fonte e' Items.All.</summary>
        public static readonly string[] ItemName = Array.ConvertAll(Items.All, d => d.Name);

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
        /// Baus de marco (docs/archive/AREA2_FASE2.md §3), na ordem de exibicao. Um bau disponivel por vez em cada posicao. O 1o e'
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

    /// <summary>
    /// A-CORE-03: numeros de economia e ritmo que o RemoteConfig e os eventos ajustam, por INSTANCIA (cada Sim le o dele, `Sim.T`). Os padroes
    /// sao os de sempre (BALANCE.md): com `new Tuning()` o bot de 60 min sai identico. Geometria, IDs, efeitos de upgrade e limites de
    /// seguranca ficam const no Balance; o teto offline de 2 h e' trava do nucleo (Balance.OfflineCapSeconds), nunca knob.
    /// Flag desligada: o sistema para de oferecer ou gerar o NOVO (encomenda, VIP, anuncio, bau); o que ja estava em andamento termina (boost
    /// ativo, VIP pendente ou na vaga, encomenda aberta, bau na tela) e o estado vai e volta no save intacto: religar continua de onde parou.
    /// ponytail: sem validacao de faixa alem das travas (offline, vagas); o RemoteConfig (A-PLAT-08) valida o JSON antes de aplicar.
    /// </summary>
    public sealed class Tuning
    {
        /// <summary>Os padroes numa copia nova a cada leitura (o Tuning e' mutavel: ninguem muda o padrao do outro).</summary>
        public static Tuning Padrao => new Tuning();

        // feature flags (os de anuncio premiado sao o AdPlacementDef.Ligado de cada placement)
        public bool Encomendas = true, Vip = true, Baus = true;
        /// <summary>P3/V10 (E-PLAT-03): intersticial atras desta flag, DESLIGADA por padrao; liga depois do A/B no Limited Release. Desligada,
        /// nada carrega nem mostra (Ads.Init); ligada, valem os 5 vetos do Anuncios.VetoIntersticial, com cap e cooldown abaixo.</summary>
        public bool Interstitial;
        public float InterstitialCooldown = 180f;   // s de sessao entre dois intersticiais
        public int InterstitialMaxSessao = 3;       // por sessao (processo)

        // E-PLAT-02 (GDD §17.1): rewarded = um AdPlacementDef por placement, na ordem do enum Anuncio. Valor: VIP = pacote do chamado (x o do
        // natural); Velocidade = s de boost (o 2o anuncio sobe a 3x); Cofre = fracao a mais do resgate (1 = dobra); bonus da encomenda = fracao a
        // mais do premio (ouro; nao Renome nem Blueprint); Trocar = sem valor (pula 1 no rodizio); Supply = s de producao das fornalhas abertas
        public AdPlacementDef[] Placements =
        {
            new AdPlacementDef { Id = "vip", Recarga = 300f, Valor = 2f },           // P14/V11: 5 min contados de quando o chamado SAI (levou o pacote ou cansou)
            new AdPlacementDef { Id = "velocidade", Recarga = 300f, Valor = 60f },   // 5 min contados do FIM do boost (sem ela, 2x renovado fechava a producao aos 26:56)
            new AdPlacementDef { Id = "cofre2x", Valor = 1f },                       // 1 por resgate (claimId); botao no painel "Bem-vindo de volta"
            new AdPlacementDef { Id = "encomenda_bonus", Valor = 0.5f },             // 1 por encomenda: +50% do premio, pago na entrega
            new AdPlacementDef { Id = "encomenda_troca", TetoDia = 5 },              // 1 por encomenda: a proxima do rodizio, mostrada antes (P5)
            new AdPlacementDef { Id = "supply", Recarga = 600f, Valor = 60f },       // lingotes = 60 s das fornalhas, mostrados antes; 10 min de recarga
        };
        public AdPlacementDef Ad(Anuncio a) => Placements[(int)a];
        public int AnunciosTetoDia = 30;   // teto diario TOTAL de rewarded (dia local das metas; relogio para tras nao reabre)
        /// <summary>B-CORE-05: o "bonus da encomenda" vale pelo menos estes s da renda de agora (o Valor x premio sozinho era menos que os ~30 s
        /// de relogio do anuncio: o AdWatcherE fechava a producao depois do AdWatcher, BALANCE.md §24/§28).</summary>
        public float EncomendaBonusMinSeg = 60f;

        // precos e custos
        public int[] Price = { 0, 0, 10, 25, 16, 60, 40 };      // ouro por item vendido (B-CORE-07: espada longa 40 = 20 por lingote, CONTEUDO_V1 §2.1)
        public int JewelPriceUp = 80;                           // Vitrine de joias: nobres pagam mais, sem mudar as outras linhas
        public int CostBase = 50; public float CostGrowth = 1.30f; // custo(tier) = 50 * 1,30^tier, arredondado a 5 (varredura em docs/BALANCE.md)
        public int SideCorridorCost = 690, JewelryCost = 1515;  // 2a area: fora da formula (coordenador 2026-10-07, BALANCE.md §9)
        /// <summary>C-CORE-01: custo por indice do upgrade no distrito (DistrictDef.Ajuste; CONTEUDO_V1 §5). null = a formula e as excecoes do
        /// Old Forge abaixo (o padrao: nada muda).</summary>
        public long[] CustoTabela;
        public int JewelerCost = 2200, JewelSpeedCost = 5500, JewelVitrineCost = 9000;   // fase 2: decisao aprovada (BALANCE.md §10.7)
        public int WorkshopFacadeCost = 14000, WorkshopFloorCost = 18000, JewelryDecorCost = 22000;   // luxo: Leva 11, Fachada +8,7 min depois da producao (BALANCE.md §15)
        public int MinerCost = 3000, Jeweler2Cost = 2400;       // fase 4: regra de retorno 8-12 min, varredura em BALANCE.md §14
        // FASE7: balcao 5/6/7/8 vagas. Ganho medido 4 -> 8 = +89 ouro/min comprando aos 15 min (0 antes dos 10 e depois dos 30): soma 750
        // = retorno 8,4 min; o Balcao 5 exige a Vitrine para nao cair nos primeiros 10 min, onde nao rende (BALANCE.md §17)
        public int[] CounterSlotCost = { 150, 175, 200, 225 };

        // tempos das estacoes
        public float FurnaceTime0 = 4f, FurnaceTime1 = 2.5f, FurnaceTime2 = 1.6f;
        public float HammerTime0 = 5f, HammerTime1 = 3f;        // bigorna mais lenta que a fornalha com fole: a 2a bigorna "reduz a fila" (§3 3:30)
        public float ShieldTimeMul = 1.5f, JewelTimeMul = 2f, LongSwordTimeMul = 1.5f;   // B-CORE-07: espada longa 7,5 s na bancada (CONTEUDO_V1 §3.1)
        public float JewelSpeedMul = 0.6f;                      // Lupa: tempo da bancada de joias x0,6
        public float ConveyorTime = 1f;                         // s por lingote na esteira

        // clientes, paciencia e vagas
        public int CounterCap0 = 5, CounterCap1 = 10;           // estoque do balcao por produto; a Vitrine dobra
        public int QueueCap0 = 4;                               // vagas do balcao (clientes lado a lado): 4 + 1 por evolucao (FASE7 §2), ate Balance.QueueCapMax
        public float PatienceBase = 30f, PatiencePerSecond = 3f; // paciencia(item) = base + k x producao inicial (FASE5 §1, calibrado no BALANCE.md §13)
        public float FirstClientAt = 8f;                        // o 1o cliente de espada chega cedo (§3: venda em <90 s)
        public float[] ClientInterval = { 0, 0, 6f, 9f, 8f, 14f, 12f }; // s entre clientes, por linha, no inicio (produto sem intervalo aqui nao tem cliente)
        public float FamePer10Sales = 0.94f, FameFloor = 0.4f;  // "fama": a cada 10 vendas os clientes chegam 6% mais rapido, ate 2,5x
        public float VitrineClientMul = 0.7f;                   // CounterCapacity atrai mais gente
        public float JewelVitrineClientMul = 0.7f;              // Vitrine de joias: nobres x0,7 (soma com a Vitrine)

        // FASE8 (docs/FASE8_VIP_VELOCIDADE.md, medido no BALANCE.md §18): cliente VIP e velocidade por anuncio
        public float VipMin = 240f, VipMax = 360f;              // s entre VIPs (sorteio deterministico); o relogio so anda depois da 1a venda
        public int VipPackMin = 3, VipPriceMul = 3;             // pacote de no minimo 3 unidades (no maximo a prateleira, CounterCap) de um produto do balcao, 3x o preco cada
        public float VipShare = 0.07f;                          // o pacote vale ~7% do ouro de um intervalo medio entre VIPs (sweep de 3/5/6 fixos e 5/7/10%: BALANCE §18)
        public float VipPatienceBase = 90f, VipPatiencePerUnit = 6f;   // rodada 3: paciencia do VIP = 90 s + 6 s por unidade do pacote (3 = 108 s, 10 = 150 s, 20 = 210 s)
        public float VipSummonMinIn = 60f;                      // chamar por anuncio: so sem VIP ativo e com o proximo natural a mais de 60 s
        public int VipSummonPackMax = 20;                       // o VIP chamado e' EXTRA (nao mexe no relogio natural) e traz Ad(Vip).Valor x o pacote do natural, ate 20 (coordenador)
        public float BoostMax = 3f, BoostPlayerMax = 1.3f;      // Velocidade: o 2o anuncio sobe para 3x; o ferreiro no maximo 1,3x (tempo e recarga: Ad(Velocidade))
        public bool AnunciosDepoisDaPrimeiraMelhoria = true;    // D6/V18: todo rewarded so depois da 1a melhoria comprada (o boost no segundo 0 e o VIP da 1a venda furavam o §3)

        // FASE9 (docs/FASE9_ENCOMENDAS.md): encomenda = vender N de uma linha aberta; sem falha. B-CORE-05: os tipos, alvos e premios estao nos
        // OrderDef (EncomendasDefs.cs); OrderSeconds e OrderRewardSeconds sao os da Common e escalam todos os tipos (x / 150, x / 10)
        public float OrderFirstDelay = 45f, OrderGap = 20f;     // s depois da 1a venda; s entre uma entregue e a proxima vaga
        public int OrderFirstTarget = 5, OrderMin = 3, OrderMax = 30;   // a 1a e' 5 espadas; OrderMax = teto do ord= do save antigo (o N vem do OrderDef; OrderMin sem uso)
        public float GrabTime = 0.4f;                           // 1 minerio do deposito (knob do Rare Ore Rush, LIVEOPS_V1 §2.3: x0,7)
        public float OrderSeconds = 150f, OrderRewardSeconds = 10f;     // premio ~10 s da taxa online: 4,2% da receita no bot de 60 min (BALANCE §20)
        public int OrderRewardMin = 25;

        // B-CORE-03 (docs/ECONOMIA_WAVE_B.md §1): qualidade sem sorteio. Preco por nivel (Quality) em %, inteiro e meio para cima (Sim.PriceOf):
        // escudo 25 -> 38 -> 63. Masterwork 0 = nao vende (vai ao Hall). ContentValidator.Qualidade cobra crescente e 0 so na Masterwork
        public int[] QualidadePct = { 100, 150, 250, 0 };
        public int NivelEstacaoQ = 15;                          // pontos de qualidade (centesimos) por nivel de estacao acima do 1: L2 = 1 em cada 7 Fina
        public float NivelEstacaoVelocidade = 0.05f;            // +5% de velocidade da familia por nivel acima do 1
        // na ordem do enum (AnvilsLv2 .. JewelsLv3): METADE do §1.5 (500/1.500, 650/1.900, 500/1.500, 1.000/3.000). Com o §1.5 o retorno medido
        // dos 8 niveis foi 37 min (portao: <= 20) e a producao completa do F2P passou a 50 min; com a metade, 18 min e 47 min (BALANCE.md §23).
        // B-CORE-07: os IV-V seguem na ordem do enum (AnvilsLv4 .. JewelsLv5). Com a metade do §1.5 (V26: 34.000) o retorno medido foi 104 min
        // (portao: <= 40); IV = 1/5 e V = 1/6 do §1.5 (12.035; o IV ainda acima do III) da 34-39 min (BALANCE.md §26, revisar)
        public int[] NivelEstacaoCusto = { 250, 750, 325, 950, 250, 750, 500, 1500, 800, 1500, 1000, 1835, 800, 1500, 1600, 3000 };
        public int BigornaLongaCusto = 6000;                    // B-CORE-07: linha Meta da espada longa (CONTEUDO_V1 §5.1)
        public int TecnicaQ = 20;                               // B-CORE-07: pontos de qualidade do Blueprint de tecnica da familia (ECONOMIA_WAVE_B §1.2)
        public int TecnicaItemQ = 15;                           // C-CORE-06: a tecnica de um produto (bp_tecnica_lapidacao: colar e caixa de joias, CONTEUDO_V1 §9)
        public int LigaQ = 30;                                  // C-CORE-05: pontos da liga na peca (QualitySource.Material; CONTEUDO_V1 §2.2 regra 2)
        // ---- royal_arsenal (C-CORE-07): patio de cotas (CONTEUDO_V1 §2.4, Sim.Cotas.cs) e o selo do distrito (§5.4)
        /// <summary>Bloco de cota: 1o de cada produto com CotaBloco0 pecas, +CotaBlocoPasso a cada bloco fechado dele, ate CotaBlocoMax.</summary>
        public int CotaBloco0 = 10, CotaBlocoPasso = 2, CotaBlocoMax = 30;
        /// <summary>A Coroa paga CotaPct% da soma dos PriceOf das pecas do bloco (venda); fechado ate CotaPrazo s da 1a peca (CotaPrazoQm com o
        /// almoxarife do distrito, wk_matias), mais CotaBonusPct% sobre isso. Renome por bloco e o extra no prazo.</summary>
        public int CotaPct = 130, CotaBonusPct = 25, CotaRenome = 2, CotaRenomePrazo = 1;
        public float CotaPrazo = 180f, CotaPrazoQm = 240f;
        /// <summary>DistrictDef.Selo comprado: q +SeloQ em toda estacao que faz produto no distrito (QualitySource.Buff; selo_arsenal, §5.4).</summary>
        public int SeloQ = 25;
        // ---- arcane_foundry (C-CORE-08): medidor de carga dos altares (CONTEUDO_V1 §2.5, Sim.Arcano.cs), em centesimos (100 = 1 ponto de E)
        /// <summary>Carga de cada peca, por handle de item (lanterna 15, anel 12...; o Ajuste do distrito); null = nenhum altar (os outros
        /// distritos). Estacao com carga > 0 no produto e' altar: tem medidor E e modo Estavel/Forcado.</summary>
        public int[] CargaArcana;
        /// <summary>E cai DrenoBase/s (300 = 3/s); cada condensador soma DrenoCondensador; a oficial arcana (wk_isis) DrenoIsis no nivel 1 e
        /// DrenoIsisPasso nos niveis 3, 5, 7 e 9. ArcanoTeto = 100 (o Estavel nunca passa; o Forcado que passa sobrecarrega): pausa de
        /// SobrecargaSeg e depois E = SobrecargaE. EnergizadaQ = q da peca energizada (Forcado).</summary>
        public int DrenoBase = 300, DrenoCondensador = 200, DrenoIsis = 100, DrenoIsisPasso = 50, ArcanoTeto = 10000, SobrecargaE = 4000, EnergizadaQ = 20;
        public float SobrecargaSeg = 10f;
        /// <summary>B-CORE-08: "modificador de evento 0-25" (ECONOMIA_WAVE_B §1.2, LIVEOPS_V1 §2.3): pontos de qualidade por item (indice = handle),
        /// 0 fora de evento; so o modificador do evento (Hunt: +15 na espada ou na joia) e o RemoteConfig (trava 0-25) mexem.</summary>
        public int[] QualidadeEvento = { 0, 0, 0, 0, 0, 0, 0 };
        public float GolpeJanela = 0.8f, GolpeRecarga = 20f;    // golpe de mestre (FS-2): anel de 0,8 s; 20 s de jogo entre um acerto e o proximo (sobe se passar de 8% da receita)

        /// <summary>B-CORE-04 (flag): contratacao e nivel de workers a venda (Equipe). Desligada, nada novo e' oferecido e quem ja foi contratado
        /// continua; com ela desligada desde o inicio o jogo e' o de antes do B-CORE-04 (o econsim --tuning Workers=false, BALANCE.md §25).</summary>
        public bool Workers = true;
        /// <summary>D-CORE-04 (flag): Royal Contracts (Contratos). Desligada, nenhum contrato e' oferecido (o ativo continua e paga); com ela
        /// desligada desde o inicio o jogo e' o de antes do D-CORE-04 (o econsim --tuning Contratos=false, BALANCE.md §32).</summary>
        public bool Contratos = true;

        // offline (raia B §5 + coordenador 2026-10-06)
        public float OfflineFactor = 0.25f;                     // 25% da taxa online
        public float OfflineMaxNextUpgrades = 2f;               // e nunca mais que 2x o preco do upgrade mais barato ainda travado
        public int OfflineMaxSeconds = Balance.OfflineCapSeconds;   // um evento pode ENCURTAR; passar das 2 h nao passa (OfflineCap)
        /// <summary>Teto de tempo do cofre que vale: o knob, nunca acima da trava do nucleo (Balance.OfflineCapSeconds = 7.200 s).</summary>
        public long OfflineCap => Math.Min(OfflineMaxSeconds, Balance.OfflineCapSeconds);

        /// <summary>A-PLAT-08 (DM §130): chave mestra do RemoteConfig. false = nenhum anuncio: o Game nem inicia o SDK e o RemoteConfig.Aplicar
        /// desliga as flags de anuncio (rewarded e intersticial). Feature nova com flag propria (IAP, Pass) ja e' chaveavel pelo nome do campo.</summary>
        public bool AnunciosLigados = true;
        /// <summary>D-CORE-03/04 (DM §130): chaves do LiveOps. Eventos false = nenhum evento entra e o ativo sai (os knobs voltam); Passe false = o
        /// Pass para de ganhar XP. Ledger, pontos e niveis ficam no save: religar continua.</summary>
        public bool Eventos = true, Passe = true;
        /// <summary>A-PLAT-08 / E-PLAT-04: chave da loja. false = o botao LOJA some (nenhuma compra nova); recibo, restore automatico e posse
        /// seguem (quem pagou nao perde).</summary>
        public bool Loja = true;

        /// <summary>Segundos na fila antes de desistir: base + k x tempo de producao da linha no inicio (B-CORE-01: soma de quantidade x tempo
        /// inicial da receita de cada insumo + o da propria receita; hoje = fornalha inicial x lingotes + bancada inicial). Espada 9 s, escudo
        /// 15,5 s, ferramenta 9 s, joia 18 s. `recipes` = o conteudo da partida (null = Recipes.All). ponytail: um nivel de insumo (o do
        /// insumo do insumo nao entra); cadeia mais funda que lingote -> produto pede a soma recursiva.</summary>
        public float PatienceFor(Item item, RecipeDef[] recipes = null)
        {
            RecipeDef r = Recipes.Making(item, recipes);
            if (r == null) return PatienceBase;
            float t = 0f;
            foreach ((Item input, int qty) in r.Inputs)
            {
                RecipeDef ri = Recipes.Making(input, recipes);
                if (ri != null) t += StartTime(ri) * qty;
            }
            return PatienceBase + PatiencePerSecond * (t + StartTime(r));
        }

        /// <summary>Tempo da receita no inicio (sem upgrade): base do tipo da estacao x multiplicador da receita.</summary>
        float StartTime(RecipeDef r) => (r.Kind == Kind.Furnace ? FurnaceTime0 : HammerTime0) * r.TimeMul(this);

        /// <summary>Paciencia do VIP com `units` no pacote (natural e chamado). O cliente normal tem 57-77 s.</summary>
        public float VipPatienceFor(int units) => VipPatienceBase + VipPatiencePerUnit * units;

        /// <summary>s entre o VIP `n` e o seguinte: 4-6 min, sorteio deterministico (sequencia de Weyl pela razao aurea: sem estado alem de VipCount).</summary>
        public float VipInterval(int n) => VipMin + (VipMax - VipMin) * (float)((n + 1) * 0.6180339887498949 % 1.0);

        /// <summary>custo(tier) = CostBase * CostGrowth^tier, arredondado ao multiplo de 5 (preco legivel no pad). A-CORE-06: long, ate
        /// Upgrades.CostMax (em int estourava no tier 67 e o Math.Max devolvia 5).</summary>
        public long Cost(int tier)
        {
            if (CustoTabela != null) return tier >= 0 && tier < CustoTabela.Length ? CustoTabela[tier] : Upgrades.CostMax;   // C-CORE-01: distrito com tabela
            // ponytail: a 2a area sai da formula porque fica no FIM do enum (save antigo de 15/17 flags) com preco de meio de
            // jogo. Mais uma area assim = trocar por uma tabela de precos por upgrade.
            switch ((Upgrade)tier)
            {
                case Upgrade.SideCorridor: return SideCorridorCost;
                case Upgrade.Jewelry: return JewelryCost;
                case Upgrade.Jeweler: return JewelerCost;
                case Upgrade.JewelSpeed: return JewelSpeedCost;
                case Upgrade.JewelVitrine: return JewelVitrineCost;
                case Upgrade.WorkshopFacade: return WorkshopFacadeCost;
                case Upgrade.WorkshopFloor: return WorkshopFloorCost;
                case Upgrade.JewelryDecor: return JewelryDecorCost;
                case Upgrade.Miner: return MinerCost;
                case Upgrade.Jeweler2: return Jeweler2Cost;
                case Upgrade.Counter5: case Upgrade.Counter6: case Upgrade.Counter7: case Upgrade.Counter8:
                    return CounterSlotCost[tier - (int)Upgrade.Counter5];
                case Upgrade.LongAnvil: return BigornaLongaCusto;
            }
            if (tier >= (int)Upgrade.AnvilsLv2 && tier <= (int)Upgrade.JewelsLv5) return NivelEstacaoCusto[tier - (int)Upgrade.AnvilsLv2];
            double c = Math.Min(CostBase * Math.Pow(CostGrowth, tier), Upgrades.CostMax);
            return Math.Max(5, (long)Math.Round(c / 5.0) * 5);   // x5 em long: em double, acima de 2^53 o produto perdia o multiplo de 5
        }

        public long Cost(Upgrade u) => Cost((int)u);
    }

    /// <summary>Marco: vender `Count` de `Item` (-1 = qualquer produto) paga `Gold` num bau em `Pos`.</summary>
    public sealed class MilestoneDef
    {
        public readonly int Item, Count, Gold; public readonly V2 Pos; public readonly string Label;
        public MilestoneDef(int item, int count, int gold, V2 pos, string label) { Item = item; Count = count; Gold = gold; Pos = pos; Label = label; }
    }

    public sealed class UpgradeDef
    {
        /// <summary>A-CORE-04: ID estavel (convencao em Upgrades.All). E' o que o save grava; nunca muda depois de publicado.</summary>
        public readonly string Id;
        /// <summary>Handle do codigo: indice em Bought/All e tier da formula de custo. O save nao depende dele.</summary>
        public readonly Upgrade Up;
        public readonly int Requires;   // indice de outro upgrade ou -1
        public readonly string Name, Desc;
        public readonly bool Luxury;    // luxo: so depois de TODOS os produtivos, fora de ProductionComplete e do teto offline
        public readonly bool InMenu;    // FASE6: comprado por toque no menu inferior (Sim.TryBuyMenu), nunca em pad; o pad antigo fica na lista (pad = indice da View)
        /// <summary>B-CORE-03: nivel de estacao da familia com esta receita (RecipeDef.Station; null = nao e'). Cada um comprado da +1 nivel as
        /// estacoes da familia (Sim.Recompute).</summary>
        public readonly string Station;
        /// <summary>Categoria Meta (ECONOMIA_WAVE_B §0.7): ralo da Wave B, fora da producao completa e do teto offline, como o luxo. Todo nivel de
        /// estacao e' Meta; B-CORE-07: a bigorna longa tambem (linha nova sem mexer nos portoes da producao completa, CONTEUDO_V1 §2.1).</summary>
        public readonly bool Meta;
        /// <summary>B-CORE-07: ID do Blueprint que a compra exige (posse, nunca gasto; null = nenhum). Sem ele: fora do menu, pad escondido e o
        /// Sim.Buy recusa (Sim.TemProjeto).</summary>
        public readonly string Blueprint;
        /// <summary>C-CORE-01: efeito generico (Sim.Has) = o do upgrade do Old Forge com este handle: Fole, Martelo veloz, Mochila, Esteira,
        /// Balcao N vagas... No Old Forge e' o proprio `Up`; num distrito, o upgrade dele que faz o mesmo (fole_couro = FurnaceSpeed1).
        /// Sem efeito generico (abre estacao, contrata, luxo) = (Upgrade)(-1).</summary>
        public readonly Upgrade Efeito;
        public UpgradeDef(Upgrade up, string id, int requires, string name, string desc, bool luxury = false, bool menu = false, string station = null,
            bool meta = false, string blueprint = null, Upgrade? efeito = null)
        {
            Up = up; Id = id; Requires = requires; Name = name; Desc = desc; Luxury = luxury; InMenu = menu; Station = station;
            Meta = meta || station != null; Blueprint = blueprint; Efeito = efeito ?? up;
        }
    }

    public static class Upgrades
    {
        public const int ProductionCount = 26, Count = 46;   // ProductionCount = quantos NAO sao luxo nem Meta (os IDs nao precisam ser contiguos)

        /// <summary>Marca por upgrade, nao por ID: um produtivo anexado depois dos luxos continua produtivo.</summary>
        public static bool IsLuxury(int u) => All[u].Luxury;
        /// <summary>Produtivo = nem luxo nem Meta: o que a producao completa (Sim.ProductionComplete) e o teto offline contam.</summary>
        public static bool IsProduction(int u) => !All[u].Luxury && !All[u].Meta;

        /// <summary>B-CORE-03: nivel de estacao que o upgrade `u` da (2, 3...; 1 = nao e' nivel): 1 + os niveis da cadeia de requisitos dele.</summary>
        public static int StationLevel(int u)
        {
            int n = 1;
            for (int r = u; r >= 0 && All[r].Station != null; r = All[r].Requires) n++;
            return n;
        }

        /// <summary>A-CORE-04: indice (= (int)Upgrade) do upgrade com este ID, ou -1 (desconhecido). Vai pelo `Up` da definicao, nunca pela
        /// posicao na tabela: reordenar a tabela ou o enum nao muda o que um ID significa. `table` = so o teste injeta. ponytail: busca linear em 29.</summary>
        public static int IndexOf(string id, UpgradeDef[] table = null)
        {
            UpgradeDef d = Array.Find(table ?? All, x => x.Id == id);
            return d != null ? (int)d.Up : -1;
        }

        /// <summary>
        /// Na ordem do enum (ContentValidator.Ids cobra). ID (A-CORE-04) = nome do cartao em pt-BR, ASCII minusculo sem acento, so [a-z0-9_]:
        /// palavras ligadas por "_" e sem "de"; o nivel de uma serie vai colado no fim (ajudante1..3, joalheiro1..2, bigorna2, fornalha2,
        /// balcao5..8). O save grava o ID: renomear o cartao NAO renomeia o ID; renomear ou apagar um ID pede passo em Sim.Migrations (o
        /// golden do ContentValidatorTests fica vermelho). ID novo entra no fim do golden.
        /// </summary>
        public static readonly UpgradeDef[] All =
        {
            new UpgradeDef(Upgrade.FurnaceSpeed1, "fole", -1, "Fole", "Fornalha mais rápida", menu: true),
            new UpgradeDef(Upgrade.Anvil2, "bigorna2", -1, "Bigorna 2", "Mais espadas por minuto"),
            new UpgradeDef(Upgrade.Helper1, "ajudante1", -1, "Ajudante", "Leva minério à fornalha"),
            new UpgradeDef(Upgrade.Shields, "escudos", -1, "Escudos", "Nova linha: 2 lingotes, paga 25"),
            new UpgradeDef(Upgrade.PlayerCapacity, "mochila", -1, "Mochila", "Carrega 6 em vez de 3", menu: true),
            new UpgradeDef(Upgrade.Helper2, "ajudante2", (int)Upgrade.Helper1, "Ajudante 2", "Leva lingotes às bancadas"),
            new UpgradeDef(Upgrade.Conveyor, "esteira", -1, "Esteira", "Fornalha → bigorna sem andar"),
            new UpgradeDef(Upgrade.FurnaceSpeed2, "fole_duplo", (int)Upgrade.FurnaceSpeed1, "Fole duplo", "Fornalha ainda mais rápida", menu: true),
            new UpgradeDef(Upgrade.PlayerSpeed, "botas", -1, "Botas", "Você anda mais rápido", menu: true),
            new UpgradeDef(Upgrade.Tools, "ferramentas", (int)Upgrade.Shields, "Ferramentas", "Nova linha: 1 lingote, paga 16"),
            new UpgradeDef(Upgrade.CounterCapacity, "vitrine", -1, "Vitrine", "Mais estoque e mais clientes", menu: true),
            new UpgradeDef(Upgrade.Furnace2, "fornalha2", (int)Upgrade.Anvil2, "Fornalha 2", "Dobra os lingotes"),
            new UpgradeDef(Upgrade.Helper3, "ajudante3", (int)Upgrade.Helper2, "Ajudante 3", "Leva produtos ao balcão"),
            new UpgradeDef(Upgrade.HelperSpeed, "ajudantes_ageis", (int)Upgrade.Helper1, "Ajudantes ágeis", "Mais rápidos e carregam 4", menu: true),
            new UpgradeDef(Upgrade.HammerSpeed, "martelo_veloz", -1, "Martelo veloz", "Bancadas mais rápidas", menu: true),
            new UpgradeDef(Upgrade.SideCorridor, "corredor", -1, "Corredor", "Abre a rua lateral"),
            new UpgradeDef(Upgrade.Jewelry, "joalheria", (int)Upgrade.SideCorridor, "Joalheria", "Nova linha: 2 lingotes, paga 60"),
            new UpgradeDef(Upgrade.Jeweler, "joalheiro1", (int)Upgrade.Jewelry, "Joalheiro", "Ajudante leva lingotes à joalheria e joias à loja"),
            new UpgradeDef(Upgrade.JewelSpeed, "lupa", (int)Upgrade.Jewelry, "Lupa", "Joalheria mais rápida", menu: true),
            new UpgradeDef(Upgrade.JewelVitrine, "vitrine_joias", (int)Upgrade.Jewelry, "Vitrine de joias", "Nobres pagam 80 e a fila cresce", menu: true),
            new UpgradeDef(Upgrade.WorkshopFacade, "fachada_nobre", (int)Upgrade.JewelVitrine, "Fachada nobre", "Entrada dourada e bandeirolas", luxury: true),
            new UpgradeDef(Upgrade.WorkshopFloor, "piso_oficina", (int)Upgrade.WorkshopFacade, "Piso de oficina", "Ladrilhos para valorizar a oficina", luxury: true),
            new UpgradeDef(Upgrade.JewelryDecor, "joalheria_real", (int)Upgrade.WorkshopFloor, "Joalheria real", "Tapetes e adornos para a joalheria", luxury: true),
            new UpgradeDef(Upgrade.Miner, "mineiro", (int)Upgrade.Jeweler, "Mineiro", "Mais um ajudante leva minério às fornalhas"),
            new UpgradeDef(Upgrade.Jeweler2, "joalheiro2", (int)Upgrade.Miner, "Joalheiro 2", "Mais um ajudante na joalheria"),
            new UpgradeDef(Upgrade.Counter5, "balcao5", (int)Upgrade.CounterCapacity, "Balcão 5 vagas", "Atende 5 clientes ao mesmo tempo", menu: true),
            new UpgradeDef(Upgrade.Counter6, "balcao6", (int)Upgrade.Counter5, "Balcão 6 vagas", "Atende 6 clientes ao mesmo tempo", menu: true),
            new UpgradeDef(Upgrade.Counter7, "balcao7", (int)Upgrade.Counter6, "Balcão 7 vagas", "Atende 7 clientes ao mesmo tempo", menu: true),
            new UpgradeDef(Upgrade.Counter8, "balcao8", (int)Upgrade.Counter7, "Balcão 8 vagas", "Atende 8 clientes ao mesmo tempo", menu: true),
            // B-CORE-03: nivel 2 abre com as Ferramentas (a joalheria com a Joalheria); o 3 exige o 2. Nome do cartao "Familia II" (<= 16 nos 3 idiomas)
            new UpgradeDef(Upgrade.AnvilsLv2, "bigornas_nivel2", (int)Upgrade.Tools, "Bigornas II", "Mais qualidade e velocidade", menu: true, station: "bigorna"),
            new UpgradeDef(Upgrade.AnvilsLv3, "bigornas_nivel3", (int)Upgrade.AnvilsLv2, "Bigornas III", "Mais qualidade e velocidade", menu: true, station: "bigorna"),
            new UpgradeDef(Upgrade.ShieldsLv2, "escudos_nivel2", (int)Upgrade.Tools, "Escudos II", "Mais qualidade e velocidade", menu: true, station: "escudos"),
            new UpgradeDef(Upgrade.ShieldsLv3, "escudos_nivel3", (int)Upgrade.ShieldsLv2, "Escudos III", "Mais qualidade e velocidade", menu: true, station: "escudos"),
            new UpgradeDef(Upgrade.ToolsLv2, "ferramentas_nivel2", (int)Upgrade.Tools, "Ferramentas II", "Mais qualidade e velocidade", menu: true, station: "ferramentas"),
            new UpgradeDef(Upgrade.ToolsLv3, "ferramentas_nivel3", (int)Upgrade.ToolsLv2, "Ferramentas III", "Mais qualidade e velocidade", menu: true, station: "ferramentas"),
            new UpgradeDef(Upgrade.JewelsLv2, "joalheria_nivel2", (int)Upgrade.Jewelry, "Joalheria II", "Mais qualidade e velocidade", menu: true, station: "joalheria"),
            new UpgradeDef(Upgrade.JewelsLv3, "joalheria_nivel3", (int)Upgrade.JewelsLv2, "Joalheria III", "Mais qualidade e velocidade", menu: true, station: "joalheria"),
            // B-CORE-07: o IV exige o III e o projeto de mestre da familia; o V, o IV (e o mesmo projeto). A bigorna longa e' pad no lugar da estacao
            new UpgradeDef(Upgrade.AnvilsLv4, "bigornas_nivel4", (int)Upgrade.AnvilsLv3, "Bigornas IV", "Mais qualidade e velocidade", menu: true, station: "bigorna", blueprint: "bp_bigorna_mestra"),
            new UpgradeDef(Upgrade.AnvilsLv5, "bigornas_nivel5", (int)Upgrade.AnvilsLv4, "Bigornas V", "Mais qualidade e velocidade", menu: true, station: "bigorna", blueprint: "bp_bigorna_mestra"),
            new UpgradeDef(Upgrade.ShieldsLv4, "escudos_nivel4", (int)Upgrade.ShieldsLv3, "Escudos IV", "Mais qualidade e velocidade", menu: true, station: "escudos", blueprint: "bp_bancada_mestra"),
            new UpgradeDef(Upgrade.ShieldsLv5, "escudos_nivel5", (int)Upgrade.ShieldsLv4, "Escudos V", "Mais qualidade e velocidade", menu: true, station: "escudos", blueprint: "bp_bancada_mestra"),
            new UpgradeDef(Upgrade.ToolsLv4, "ferramentas_nivel4", (int)Upgrade.ToolsLv3, "Ferramentas IV", "Mais qualidade e velocidade", menu: true, station: "ferramentas", blueprint: "bp_bancada_mestra"),
            new UpgradeDef(Upgrade.ToolsLv5, "ferramentas_nivel5", (int)Upgrade.ToolsLv4, "Ferramentas V", "Mais qualidade e velocidade", menu: true, station: "ferramentas", blueprint: "bp_bancada_mestra"),
            new UpgradeDef(Upgrade.JewelsLv4, "joalheria_nivel4", (int)Upgrade.JewelsLv3, "Joalheria IV", "Mais qualidade e velocidade", menu: true, station: "joalheria", blueprint: "bp_bancada_mestra"),
            new UpgradeDef(Upgrade.JewelsLv5, "joalheria_nivel5", (int)Upgrade.JewelsLv4, "Joalheria V", "Mais qualidade e velocidade", menu: true, station: "joalheria", blueprint: "bp_bancada_mestra"),
            new UpgradeDef(Upgrade.LongAnvil, "bigorna_longa", (int)Upgrade.HammerSpeed, "Bigorna longa", "Nova linha: 2 lingotes, paga 40", meta: true, blueprint: "bp_rec_espada_longa"),
        };

        /// <summary>A-CORE-06: teto da curva (1e18, cabe em long com folga para somar ouro). 50 x 1,3^tier chega nele no tier 144. O custo
        /// de cada upgrade e' do Tuning (Tuning.Cost); o teto e' trava, nao knob.</summary>
        public const long CostMax = 1000000000000000000;
    }

    /// <summary>B-CORE-01: item em dados. O handle no codigo e' a posicao na tabela (Items.All; o enum Item e' atalho das 6 primeiras). O save
    /// grava o Id (schema 3). Preco de quem vende: Tuning.Price[handle].</summary>
    public sealed class ItemDef
    {
        /// <summary>ID estavel (mesma convencao dos upgrades, A-CORE-04): o save grava; nunca muda depois de publicado.</summary>
        public readonly string Id, Name;
        public readonly int Tier;      // 1-5 (GDD §7)
        public readonly bool Product;  // vai ao balcao e tem preco; minerio e lingote (insumos) nao
        /// <summary>B-CORE-08 (ECONOMIA_WAVE_B §8.1): familia de produto (agrupa as Masterworks e o +5% do Hall); o proprio ID quando nao dada.</summary>
        public readonly string Family;
        public ItemDef(string id, string name, int tier, bool product, string family = null) { Id = id; Name = name; Tier = tier; Product = product; Family = family ?? id; }
    }

    /// <summary>
    /// B-CORE-01: receita em dados. As estacoes da familia `Station` (todas do tipo `Kind`) trocam `Inputs` (item, quantidade) por 1 `Output`
    /// em (tempo base do tipo no Tuning: fornalha FurnaceTime*, bancada HammerTime*) x TimeMul(Tuning). Os upgrades de velocidade (Fole,
    /// Martelo veloz, Lupa) agem na estacao (Sim.Recompute). ponytail: uma receita por familia; bancada que escolhe o produto (distrito 3)
    /// pede a receita ativa na estacao. B-CORE-07: `Family` = familia de nivel e qualidade (UpgradeDef.Station, tecnica; oficial e Masterwork
    /// depois), a propria `Station` quando nao dada: a bigorna longa faz a receita dela e sobe com os niveis das bigornas (CONTEUDO_V1 §2.1).
    /// </summary>
    public sealed class RecipeDef
    {
        public readonly string Station, Family; public readonly Kind Kind;
        public readonly Item Output;
        /// <summary>C-CORE-01 (CONTEUDO_V1 §3.1, outputCount): pecas por ciclo (rebitadeira: 1 lingote -> 4 rebites). 1 em toda receita do Old Forge.</summary>
        public readonly int Qtd = 1;
        /// <summary>C-CORE-05: Qty 0 = insumo opcional (a liga, CONTEUDO_V1 §2.2 regra 2): a peca nao espera por ele; com 1 na entrada quando a
        /// peca sai, consome 1 e soma Tuning.LigaQ (Station.Liga, Sim.Grade). Um por receita, ao lado de um obrigatorio (ContentValidator).</summary>
        public readonly (Item Item, int Qty)[] Inputs;
        public readonly Func<Tuning, float> TimeMul;
        /// <summary>C-CORE-06: ID do upgrade que libera esta receita numa bancada configuravel (Sim.Bancada; null = do kit). Fora dela, sem uso:
        /// a estacao fixa abre pelo StationDef.Abre.</summary>
        public string Abre;
        public RecipeDef(string station, Kind kind, Item output, Func<Tuning, float> timeMul, params (Item Item, int Qty)[] inputs)
            : this(station, station, kind, output, timeMul, inputs) { }
        public RecipeDef(string station, string family, Kind kind, Item output, Func<Tuning, float> timeMul, params (Item Item, int Qty)[] inputs)
        { Station = station; Family = family; Kind = kind; Output = output; TimeMul = timeMul; Inputs = inputs; }
        public RecipeDef(string station, string family, Kind kind, Item output, int qtd, Func<Tuning, float> timeMul, params (Item Item, int Qty)[] inputs)
            : this(station, family, kind, output, timeMul, inputs) { Qtd = qtd; }
    }

    public static class Items
    {
        /// <summary>Na ordem do enum Item (ContentValidator.Itens cobra: e' tambem a ordem do espelho do save, Sim.ItemIdsV2). Item novo entra no fim.</summary>
        public static readonly ItemDef[] All =
        {
            new ItemDef("minerio", "minério", 1, false),
            new ItemDef("lingote", "lingote", 1, false),
            new ItemDef("espada", "espada", 1, true),
            new ItemDef("escudo", "escudo", 1, true),
            new ItemDef("ferramenta", "ferramenta", 1, true),
            new ItemDef("joia", "joia", 3, true),
            new ItemDef("espada_longa", "espada longa", 1, true, "espada"),   // B-CORE-07: linha Meta da bigorna longa (CONTEUDO_V1 §2.1); familia espada (§3)
        };

        /// <summary>Handle do item com este ID, ou -1 (desconhecido). `table` = o conteudo da partida (null = All).</summary>
        public static int IndexOf(string id, ItemDef[] table = null) => Array.FindIndex(table ?? All, d => d.Id == id);
    }

    public static class Recipes
    {
        /// <summary>As 4 linhas da v0.x mais a fornalha (BALANCE.md) e a espada longa (B-CORE-07): o minerio sai do deposito, sem receita.</summary>
        public static readonly RecipeDef[] All =
        {
            new RecipeDef("fornalha", Kind.Furnace, Item.Ingot, t => 1f, (Item.Ore, 1)),
            new RecipeDef("bigorna", Kind.Crafter, Item.Sword, t => 1f, (Item.Ingot, 1)),
            new RecipeDef("escudos", Kind.Crafter, Item.Shield, t => t.ShieldTimeMul, (Item.Ingot, 2)),
            new RecipeDef("ferramentas", Kind.Crafter, Item.Tool, t => 1f, (Item.Ingot, 1)),
            new RecipeDef("joalheria", Kind.Crafter, Item.Jewel, t => t.JewelTimeMul, (Item.Ingot, 2)),
            new RecipeDef("bigorna_longa", "bigorna", Kind.Crafter, Item.LongSword, t => t.LongSwordTimeMul, (Item.Ingot, 2)),   // B-CORE-07: familia das bigornas
        };

        /// <summary>Receita que produz `item`, ou null (minerio, ou item sem receita). `table` = o conteudo da partida (null = All).</summary>
        public static RecipeDef Making(Item item, RecipeDef[] table = null) => Array.Find(table ?? All, r => r.Output == item);
    }
}
