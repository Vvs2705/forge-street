using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    public struct SimEvent
    {
        public Ev Kind; public int A, B; public V2 Pos;
        public SimEvent(Ev kind, int a, int b, V2 pos) { Kind = kind; A = a; B = b; Pos = pos; }
    }

    public sealed class Station
    {
        public int Index; public Kind Kind; public V2 Pos; public string Name;
        /// <summary>B-CORE-01: receita da familia da estacao (null = nao produz: deposito e balcoes). Insumos, produto e tempo saem dela.</summary>
        public RecipeDef Recipe;
        /// <summary>Entrada: quantos de cada insumo da receita (paralelo a Recipe.Inputs), ate InCap de cada.</summary>
        public int[] Ins = new int[1];
        public Item OutItem;
        public int UnlockBy = -1; public bool Unlocked = true;
        public int Out, InCap, OutCap;
        /// <summary>Atalhos do 1o insumo (o unico em todas as receitas de hoje): a View, a dica e os testes leem a entrada por eles.</summary>
        public Item InItem => Produces ? Recipe.Inputs[0].Item : Item.Ore;
        public int Need => Produces ? Recipe.Inputs[0].Qty : 0;
        public int In { get => Ins[0]; set => Ins[0] = value; }
        /// <summary>Posicao do insumo `i` na receita, ou -1.</summary>
        public int InIndex(Item i)
        {
            if (Produces) for (int k = 0; k < Ins.Length; k++) if (Recipe.Inputs[k].Item == i) return k;
            return -1;
        }
        /// <summary>A entrada recebe mais um `i` agora?</summary>
        public bool Accepts(Item i) { int k = InIndex(i); return k >= 0 && Ins[k] < InCap; }
        public float Time = 1f, Progress; public bool Busy, Blocked;
        public int Crafted;
        /// <summary>B-CORE-03: nivel da familia (1 + niveis comprados; Sim.Recompute), acumulador de qualidade em centesimos (0-99), golpe de mestre
        /// valendo para a proxima peca e quantas das `Out` sao Fina e Superior (OutQ[(int)Quality]; Normal = o resto, Sim.Fit).</summary>
        public int Level = 1, QAcc; public bool Strike;
        public readonly int[] OutQ = new int[Sim.Graus];
        public float StarveSeconds, StallSeconds;   // metricas de gargalo (GDD §14 bottleneck_seconds)
        /// <summary>Bocas (FASE5 §2b): ponto onde o personagem encosta para DEPOSITAR (InAt) e para RECOLHER (OutAt), em lados
        /// opostos do corpo; deposito e balcoes tem uma zona so (InAt = OutAt). Zona = ate Balance.MouthRadius do ponto.</summary>
        public V2 InAt, OutAt;
        public bool Produces => Recipe != null;
        /// <summary>Meia-medida do corpo: Balance.StationHalf; o balcao principal alarga com as vagas (FASE7 §2, Sim.Recompute).</summary>
        public V2 Half = Balance.StationHalf;
        public Box Body => new Box(Pos, Half);
    }

    /// <summary>Jogador (Role = -1) ou ajudante (Role 0 minerio, 1 lingote, 2 produto, 3 joalheiro). Pilha POR ITEM (FASE7 §1): o
    /// jogador leva todos os tipos ao mesmo tempo, ate Cap de CADA um; o ajudante continua com um tipo por vez (Sim.CanPick).</summary>
    public sealed class Carrier
    {
        public V2 Pos; public float Speed; public int Cap; public int Role = -1;
        public readonly int[] Held;   // quantos de cada item na mao, por handle
        /// <summary>B-CORE-03: quantos dos Held[item] sao Fina e Superior (HeldQ[item][(int)Quality]; Normal = o resto).</summary>
        public readonly int[][] HeldQ;
        public Carrier() : this(Items.All.Length) { }
        /// <summary>`items` = quantos itens o conteudo da partida tem (Sim.ItemDefs).</summary>
        public Carrier(int items) { Held = new int[items]; HeldQ = Sim.Lotes(items); }
        public float ActT, IdleT; public bool Moving;
        /// <summary>B-CORE-04: indice do WorkerDef deste ajudante (Equipe: nivel, passo e carga); -1 = jogador ou ajudante de teste.</summary>
        public int Wk = -1;
        public int State;            // ajudante: 0 buscando, 1 entregando
        public int Target = -1;      // ajudante: indice da estacao alvo (-1 = parado)
        /// <summary>s andando sem sair de Balance.StuckRadius de um ponto (sem progresso): o Sim.Steer usa para sair do impasse. Nao vai no save.</summary>
        public float StuckT;
        V2 _anchor;

        /// <summary>Poe o personagem em `p` (todo movimento do Sim passa aqui) e conta o tempo andando sem progredir.</summary>
        public void MoveTo(V2 p, float dt)
        {
            Pos = p;
            if (!Moving || V2.Dist(p, _anchor) > Balance.StuckRadius) { _anchor = p; StuckT = 0f; }
            else StuckT += dt;
        }
        public bool Has(Item i) => Held[(int)i] > 0;
        /// <summary>Total na mao, todos os tipos.</summary>
        public int Count { get { int n = 0; foreach (int h in Held) n += h; return n; } }
        /// <summary>Item mais adiantado na cadeia que esta na mao (joia > ferramenta > escudo > espada > lingote > minerio): o que a
        /// dica e o bot entregam primeiro. Ajudante: o unico tipo dele. Maos vazias: minerio.</summary>
        public Item Item { get { for (int i = Held.Length - 1; i > 0; i--) if (Held[i] > 0) return (Item)i; return Item.Ore; } }
    }

    public sealed class Client
    {
        public Item Want; public float Patience, MaxPatience;
        /// <summary>FASE8: VIP = pacote de `Pack` unidades (o que ainda falta) a 3x o preco; `Paid` = ouro que ja pagou nesta vaga. P14: `Summoned` =
        /// chamado por anuncio (quando ele sai, comeca a recarga do botao).</summary>
        public bool Vip, Summoned; public int Pack, Paid;
    }

    /// <summary>Bau de marco (um por Balance.Milestones, mesmo Index). State: 0 nao batido, 1 disponivel, 2 aberto.</summary>
    public sealed class Chest
    {
        public int Index, State, Gold; public V2 Pos; public string Label;
    }

    /// <summary>Pad de upgrade: ficar em cima despeja ouro ate completar o preco. Um slot tem uma cadeia (ex.: Ajudante 1/2/3).</summary>
    public sealed class Pad
    {
        public int Slot; public V2 Pos; public Upgrade[] Chain; public long Paid; public double Acc;
        public bool Armed = true;   // depois de comprar, so volta a cobrar quando o jogador sai de cima (nao engole o proximo nivel)

        /// <summary>Upgrade a venda neste pad agora, ou -1 (cadeia esgotada, pre-requisito faltando ou upgrade do menu = pad invisivel).</summary>
        public int Current(Sim s)
        {
            foreach (Upgrade u in Chain)
            {
                if (s.Bought[(int)u]) continue;
                if (Upgrades.All[(int)u].InMenu) return -1;   // FASE6: vendido no menu inferior; cadeia so de menu = pad invisivel para sempre
                if (Upgrades.IsLuxury((int)u) && !s.ProductionComplete) return -1;
                if (!s.TemProjeto((int)u)) return -1;   // B-CORE-07: a bigorna longa so aparece com o projeto
                int req = Upgrades.All[(int)u].Requires;
                return req < 0 || s.Bought[req] ? (int)u : -1;
            }
            return -1;
        }
    }

    /// <summary>
    /// Simulacao deterministica da oficina, em metros e segundos, sem UnityEngine. Um `Tick(dt, inX, inY)` move o
    /// jogador, pensa os ajudantes, faz as trocas automaticas (pega/deposita/vende/paga pad), produz, anda a esteira,
    /// atende clientes e atualiza as metricas. A view le `Events` depois de cada tick. Nao ha aleatoriedade:
    /// mesma entrada, mesmo resultado (BalanceTests.Determinismo).
    /// </summary>
    public sealed class Sim
    {
        /// <summary>A-CORE-03: numeros de economia/ritmo e feature flags desta partida (RemoteConfig e eventos). Padrao = `new Tuning()`.</summary>
        public readonly Tuning T;
        /// <summary>D-CORE-01/02: metas (Metas.cs). Gancho unico: o Emit conta o evento; o save leva as linhas mt.* so depois da 1a Agora.</summary>
        public readonly Metas Metas;
        /// <summary>E-PLAT-02/03: rewarded por placement (ID de transacao, recarga, teto diario) e o veto do intersticial (Anuncios.cs).</summary>
        public readonly Anuncios Anuncios;
        /// <summary>B-CORE-06: Renome (Renome.cs), trilha que so sobe. Ganchos: o Emit credita encomenda e bau, o Sell a Superior; save na linha rn=.</summary>
        public readonly Renome Renome;
        /// <summary>B-CORE-04: workers (Equipe.cs): contratacao, nivel e o efeito de cada papel. Ganchos: Recompute (passo, carga, tempo, pilhas,
        /// estoque), QualityBreakdown, Clients (paciencia), Serve (atendimento); save na linha wkn=.</summary>
        public readonly Equipe Equipe;
        /// <summary>D-CORE-03/04: eventos e Masterwork Pass (LiveOps.cs). Ganchos: o Emit conta, o Strike conta o golpe; save nas linhas ev.*/ps.*.</summary>
        public readonly LiveOps LiveOps;
        /// <summary>B-CORE-07: Blueprints possuidos (Projetos.cs), posse e nunca saldo. Ganchos: o Tick checa os marcos, o Sell conta as Fina ou
        /// melhor; save nas linhas bp=/bpq=.</summary>
        public readonly Projetos Projetos;
        /// <summary>B-CORE-08: o Hall das obras-primas e a Collection (Masterworks.cs). Ganchos: MasterworkPending e o Produce (Hall), o Tick
        /// (marcos), o Sell (Superiores por item), o PriceOf (+5% da familia completa); save nas linhas hall=/colq=.</summary>
        public readonly Hall Hall; public readonly Colecao Colecao;
        /// <summary>B-CORE-01: itens e receitas desta partida (padrao Items.All e Recipes.All; so o teste injeta outro). Handle de item = posicao
        /// em ItemDefs; os arrays por item (Stock, SoldItems, Crafted, Held) tem esse tamanho.</summary>
        public readonly ItemDef[] ItemDefs; public readonly RecipeDef[] RecipeDefs;
        public float Time;
        /// <summary>A-CORE-06: ouro, custos e somas em long (o save e' texto: cabe sem mudar o formato). ponytail: soma sem saturar; 9,2e18 nao
        /// se alcanca jogando (1 milhao/s por 100 anos = 3e15), so editando o save.</summary>
        public long Gold;
        public readonly bool[] Bought = new bool[Upgrades.Count];
        public readonly float[] UpgradeTime = new float[Upgrades.Count];
        public readonly List<Station> Stations = new List<Station>();
        public readonly List<Pad> Pads = new List<Pad>();
        /// <summary>Corpos solidos ativos (estacoes desbloqueadas, balcoes e Balance.Obstacles comprados); refeito no Recompute.</summary>
        public readonly List<Box> Solids = new List<Box>();
        public readonly Carrier Player;
        public readonly List<Carrier> Workers = new List<Carrier>();
        public readonly List<Client> Queue = new List<Client>();        // fila do balcao principal (espada/escudo/ferramenta)
        public readonly List<Client> JewelQueue = new List<Client>();   // fila da loja de joias (2a area)
        public readonly int[] Stock;                                     // por item; Stock[Jewel] fica na loja de joias
        /// <summary>B-CORE-03: quantos dos Stock[item] sao Fina e Superior (StockQ[item][(int)Quality]; Normal = o resto). O balcao vende a melhor primeiro.</summary>
        public readonly int[][] StockQ;
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public int CounterCap, QueueCap, JewelQueueCap;   // derivados no Recompute
        public bool HasConveyor;
        public readonly List<Chest> Chests = new List<Chest>();          // baus de marco, na ordem de Balance.Milestones
        public readonly int[] SoldItems;                                 // vendas por item (marcos "10 espadas", "10 joias")
        public float MaxX = Balance.WorkshopW - 0.3f;   // limite direito do jogador: a oficina ate o Corredor, o mapa inteiro depois
        // FASE8 (docs/FASE8_VIP_VELOCIDADE.md): cliente VIP e velocidade por anuncio
        /// <summary>s ate o proximo VIP natural (parado ate a 1a venda); VipCount = VIPs ja sorteados, naturais e chamados (semente do intervalo e do
        /// produto). Vao no save.</summary>
        public float VipIn; public int VipCount;
        // FASE9: encomenda ativa (OrderItem -1 = nenhuma) e relogio ate a proxima (so anda depois da 1a venda)
        public int OrderItem = -1, OrderTarget, OrderProgress, OrderReward, OrderCount; public float OrderIn;
        /// <summary>E-PLAT-02: encomendas puladas pelo "Trocar" (anuncio). Indice no rodizio = OrderIndex (entregues + puladas); vai no save (ord=).</summary>
        public int OrderSkips;
        public int OrderIndex => OrderCount + OrderSkips;
        public long OrderGold;   // premios pagos nesta sessao (metrica do bot; fora do GoldEarned, nao vai no save)
        /// <summary>Velocidade por anuncio: multiplicador (1, 2 ou 3), s restantes e s de recarga (conta do fim do boost), no tempo de jogo. A-CORE-02:
        /// os tres vao no save (bst=): reabrir nao libera outro boost e o ativo volta com o que restou; o tempo fora nao corre nenhum (como na pausa).</summary>
        public float BoostMul = 1f, BoostLeft, BoostCooldown;
        /// <summary>P14 (V11): s de recarga do "Chamar VIP", no tempo de jogo; comeca quando o VIP chamado sai (levou o pacote ou cansou), entao
        /// com ele na vaga o botao ja esta travado por VipActive. Vai no save (vipc=), como o boost: reabrir nao libera outro.</summary>
        public float VipCooldown;
        int _vipWant = -1, _vipPack; bool _vipSummoned;   // VIP sorteado esperando vaga (-1 = nenhum); chamado por anuncio?
        int _vipPaid; bool _vipQuiet;  // VIP que estava na vaga ao salvar: volta com o que ja pagou e sem novo aviso de chegada
        // B-CORE-03 (ECONOMIA_WAVE_B §1.4): golpe de mestre
        /// <summary>s de jogo ate o ferreiro poder acertar outro golpe (Tuning.GolpeRecarga depois de cada acerto). Vai no save (golpe=).</summary>
        public float StrikeCooldown;
        /// <summary>B-CORE-08 liga: ha Masterwork pendente para a familia desta estacao (projeto e condicoes). null = nenhuma: o golpe numa Superior
        /// fica Superior. Masterworks = obras-primas que sairam para o Hall (nunca pilha, balcao nem GoldEarned).</summary>
        public Func<Station, bool> MasterworkPending;
        public int Masterworks;
        /// <summary>Metricas do bot (fora do save, como o OrderGold): ouro de venda acima do preco Normal, valor que o golpe somou as pecas e golpes acertados.</summary>
        public long QualityGold, StrikeGold; public int Strikes;

        // metricas de playtest (GDD §14/§15)
        public float FirstSaleTime = -1f, WalkNoDecision;
        public int Sales, ClientsLost, ClientsTurnedAway, MaxQueue, UpgradesBought;
        public long GoldEarned, OfflineEarned;
        public bool ProductionComplete
        {
            get
            {
                for (int i = 0; i < Upgrades.Count; i++) if (!Bought[i] && Upgrades.IsProduction(i)) return false;   // luxo e Meta nao contam
                return true;
            }
        }
        public readonly int[] Crafted;
        public double RateEma;                 // ouro/s online, media movel (base do offline)
        public long SavedAt, LastClaim;        // unix s; o claim offline e' idempotente por SavedAt, que nunca recua (Save)
        /// <summary>Ouro do ultimo resgate do cofre nesta sessao (0 = nenhum): o "Cofre 2x" dobra este (Anuncios). Nao vai no save.</summary>
        public long LastClaimGold;
        /// <summary>A-CORE-02: s que o relogio do aparelho estava atras do SavedAt, so no 1o Save de cada recuo (senao 0): a View loga clock_back
        /// uma vez, nao a cada autosave.</summary>
        public long ClockWentBack;
        bool _clockBehind;

        public readonly Station Deposit, FurnaceA, FurnaceB, AnvilA, AnvilB, ShieldBench, ToolBench, Counter, JewelBench, JewelShop, LongAnvil;
        public static readonly V2 HireSpot = new V2(8.3f, 1.5f);
        // B-CORE-04: os ajudantes sao os WorkerDef Porter (upgrade que contrata e rota), na ordem da tabela = a de antes: Ajudante 1-3, Joalheiro,
        // Mineiro (2o papel 0), Joalheiro 2 (2o papel 3, FASE4_MINERIO)
        static readonly int[] HireDef = Porters();
        static readonly Upgrade[] HireBy = Array.ConvertAll(HireDef, k => (Upgrade)Upgrades.IndexOf(WorkerDefs.All[k].Contrata));
        static readonly int[] HireRole = Array.ConvertAll(HireDef, k => WorkerDefs.All[k].Rota);

        static int[] Porters()
        {
            var k = new List<int>();
            for (int i = 0; i < WorkerDefs.All.Length; i++) if (WorkerDefs.All[i].Contrata != null) k.Add(i);
            return k.ToArray();
        }

        readonly float[] _clientT;
        float _serveT, _serveJ, _convT, _padDwell;
        Pad _padUnder;

        /// <summary>`items`/`recipes`/`blueprints`/`masterworks` = conteudo injetado (null = Items.All / Recipes.All / ProjetosDefs.All /
        /// MasterworksDefs.All); so o teste usa.</summary>
        public Sim(Tuning tuning = null, ItemDef[] items = null, RecipeDef[] recipes = null, BlueprintDef[] blueprints = null, MasterworkDef[] masterworks = null)
        {
            T = tuning ?? new Tuning();
            Metas = new Metas(this);
            Anuncios = new Anuncios(this);
            Renome = new Renome(this);
            Equipe = new Equipe(this);
            LiveOps = new LiveOps(this);
            ItemDefs = items ?? Items.All; RecipeDefs = recipes ?? Recipes.All;
            int n = ItemDefs.Length;
            Projetos = new Projetos(this, blueprints);
            Hall = new Hall(this, masterworks); Colecao = new Colecao(this, null);
            MasterworkPending = s => Hall.Pendente(s) >= 0;   // B-CORE-08: projeto e condicoes da 1a pendente da familia
            Stock = new int[n]; SoldItems = new int[n]; Crafted = new int[n]; StockQ = Lotes(n);
            Player = new Carrier(n) { Pos = new V2(4.5f, 3.5f), Speed = Balance.PlayerSpeed, Cap = Balance.PlayerCap };
            VipIn = T.VipInterval(0);
            OrderIn = T.OrderFirstDelay;
            // 1o cliente: espada cedo (FirstClientAt); as outras linhas no intervalo delas (B-CORE-07: por item, a espada longa entrou)
            _clientT = new float[n];
            for (int i = (int)Item.Sword; i < n; i++) _clientT[i] = i == (int)Item.Sword ? T.FirstClientAt : HasClients((Item)i) ? T.ClientInterval[i] : 0f;
            for (int i = 0; i < Upgrades.Count; i++) UpgradeTime[i] = -1f;
            // Bocas (FASE5 §2b): (inX, inY) = lado da ENTRADA; a SAIDA fica no lado oposto. Toda estacao que produz: entrada a
            // esquerda, saida a direita (pilhas da view InPile -1,05 / OutPile +1,05). Fornalha, Bigorna e Joalheria eram
            // embaixo/em cima por causa dos pads do Fole, do Martelo veloz e da Lupa, que foram para o menu (FASE6 §2).
            // B-CORE-01: a ultima coluna e' a familia; a estacao faz a receita da familia dela (RecipeDefs)
            Deposit = Add(Kind.Deposit, 1.5f, 1.5f, "Depósito", 0f, 1f);             // zona unica acima (lado das fornalhas)
            FurnaceA = Add(Kind.Furnace, 1.5f, 5.5f, "Fornalha", -1f, 0f, -1, "fornalha");
            FurnaceB = Add(Kind.Furnace, 4.5f, 5.5f, "Fornalha 2", -1f, 0f, (int)Upgrade.Furnace2, "fornalha");
            AnvilA = Add(Kind.Crafter, 1.5f, 9.5f, "Bigorna", -1f, 0f, -1, "bigorna");
            AnvilB = Add(Kind.Crafter, 4.5f, 9.5f, "Bigorna 2", -1f, 0f, (int)Upgrade.Anvil2, "bigorna");
            ShieldBench = Add(Kind.Crafter, 7.5f, 9.5f, "Escudos", -1f, 0f, (int)Upgrade.Shields, "escudos");
            ToolBench = Add(Kind.Crafter, 7.5f, 5.5f, "Ferramentas", -1f, 0f, (int)Upgrade.Tools, "ferramentas");
            Counter = Add(Kind.Counter, 4.5f, 13f, "Balcão", 0f, -1f);                // zona unica embaixo (fila fica em cima)
            // 2a area: ANEXADAS ao fim (indices 8 e 9) para os indices e o save antigos nao mudarem
            JewelBench = Add(Kind.Crafter, 12f, 6.5f, "Joalheria", -1f, 0f, (int)Upgrade.Jewelry, "joalheria");
            JewelShop = Add(Kind.Counter, 12f, 11.5f, "Loja de joias", 0f, -1f, (int)Upgrade.Jewelry);
            // B-CORE-07: bigorna longa ANEXADA (indice 10), na fileira livre do Deposito, entre o Mineiro (2,8; 0,6) e a contratacao (8,3; 1,5)
            LongAnvil = Add(Kind.Crafter, 5.0f, 1.5f, "Bigorna longa", -1f, 0f, (int)Upgrade.LongAnvil, "bigorna_longa");

            // Pads fora das linhas de caminhada (coluna x=1,5 e diagonais ate o balcao): atravessar um pad nao pode gastar.
            // FASE6: os pads de upgrades do menu (Fole, Mochila, Botas, Vitrine, Ajudantes ageis, Martelo veloz, Lupa, Vitrine de
            // joias) continuam na lista (o indice do pad e' o da View e da dica; o save e' por ID desde o schema 2), mas ficam invisiveis para sempre (Pad.Current).
            Slot(0.5f, 6.5f, Upgrade.FurnaceSpeed1, Upgrade.FurnaceSpeed2);
            Slot(FurnaceB.Pos.X, FurnaceB.Pos.Y, Upgrade.Furnace2);
            Slot(AnvilB.Pos.X, AnvilB.Pos.Y, Upgrade.Anvil2);
            Slot(ShieldBench.Pos.X, ShieldBench.Pos.Y, Upgrade.Shields);
            Slot(ToolBench.Pos.X, ToolBench.Pos.Y, Upgrade.Tools);
            Slot(HireSpot.X, HireSpot.Y, Upgrade.Helper1, Upgrade.Helper2, Upgrade.Helper3);
            Slot(6.5f, 0.6f, Upgrade.HelperSpeed);
            Slot(4.5f, 0.6f, Upgrade.PlayerCapacity);
            Slot(2.8f, 0.6f, Upgrade.PlayerSpeed);
            Slot(0.5f, 11.2f, Upgrade.Conveyor);                     // era (0,5; 7,8): com as bocas laterais caiu no corredor da parede entre as entradas da Fornalha e da Bigorna (BALANCE §15)
            Slot(8.4f, 3.4f, Upgrade.HammerSpeed);                    // era (0,5; 9,5): rotulo sobre o da Bigorna e corredor da fisica (FASE4 §3, BALANCE §14)
            Slot(6.5f, 13f, Upgrade.CounterCapacity);
            Slot(8.4f, 11.6f, Upgrade.SideCorridor);                 // 2a area, pads 12 e 13: lado direito da oficina, fora das linhas
            Slot(JewelBench.Pos.X, JewelBench.Pos.Y, Upgrade.Jewelry);
            Slot(14.2f, 8.5f, Upgrade.Jeweler);                      // fase 2, pads 14-16: rua lateral, fora da linha bancada -> loja (x=12)
            Slot(14.2f, 6.8f, Upgrade.JewelSpeed);                   // era (10,2; 6,5): ficou na boca da porta lateral (FASE5 §2, BALANCE §13)
            Slot(14.2f, 11.5f, Upgrade.JewelVitrine);
            Slot(6.5f, 3.2f, Upgrade.WorkshopFacade);               // luxo, pads 17-19: so depois de todos os produtivos
            Slot(4.5f, 3.2f, Upgrade.WorkshopFloor);
            Slot(14.2f, 5.1f, Upgrade.JewelryDecor);
            Slot(2.8f, 0.6f, Upgrade.Miner);                         // fase 4, pads 20-21. Era (0,5; 3,5): raspava a rota Deposito -> entrada da Fornalha; agora no lugar das Botas (menu, pad invisivel)
            Slot(12f, 3.5f, Upgrade.Jeweler2);                       // rua lateral embaixo da Joalheria: longe das bocas, fila, bau e decoracao
            Slot(LongAnvil.Pos.X, LongAnvil.Pos.Y, Upgrade.LongAnvil); // B-CORE-07, pad 22: no lugar da estacao (so com o projeto: Pad.Current)
            foreach (MilestoneDef m in Balance.Milestones) Chests.Add(new Chest { Index = Chests.Count, Pos = m.Pos, Gold = m.Gold, Label = m.Label });
            Recompute();
        }

        Station Add(Kind kind, float x, float y, string name, float inX, float inY, int unlockBy = -1, string family = null)
        {
            var s = new Station { Index = Stations.Count, Kind = kind, Pos = new V2(x, y), Name = name, UnlockBy = unlockBy, Unlocked = unlockBy < 0 };
            s.Recipe = family == null ? null : Array.Find(RecipeDefs, r => r.Station == family);   // familia sem receita: nao produz (ContentValidator acusa)
            if (s.Produces)
            {
                s.Ins = new int[s.Recipe.Inputs.Length]; s.OutItem = s.Recipe.Output;
                bool furnace = kind == Kind.Furnace;   // pilhas: geometria da estacao, nao da receita
                s.InCap = furnace ? Balance.FurnaceIn : Balance.CrafterIn; s.OutCap = furnace ? Balance.FurnaceOut : Balance.CrafterOut;
            }
            // ponto da boca = personagem encostado no meio da face (centro a CharRadius da face)
            V2 side = new V2(inX, inY), half = Balance.StationHalf;
            V2 reach = side * (Math.Abs(inX) * half.X + Math.Abs(inY) * half.Y + Balance.CharRadius);
            s.InAt = s.Pos + reach;
            s.OutAt = s.Produces ? s.Pos - reach : s.InAt;
            Stations.Add(s);
            return s;
        }

        void Slot(float x, float y, params Upgrade[] chain) => Pads.Add(new Pad { Slot = Pads.Count, Pos = new V2(x, y), Chain = chain });

        /// <summary>B-CORE-01: o ItemDef diz (handle fora da tabela = nao).</summary>
        public bool IsProduct(Item i) => (uint)i < (uint)ItemDefs.Length && ItemDefs[(int)i].Product;
        /// <summary>Linha aberta = alguma estacao aberta faz este produto (o Recompute abre a estacao pelo upgrade dela).</summary>
        public bool LineUnlocked(Item product)
        {
            if (!IsProduct(product)) return false;
            foreach (Station s in Stations) if (s.Unlocked && s.Produces && s.OutItem == product) return true;
            return false;
        }
        /// <summary>O balcao `counter` vende `i`? O principal vende espada/escudo/ferramenta; a loja de joias, so joia.</summary>
        public bool Sells(Station counter, Item i) => counter.Kind == Kind.Counter && IsProduct(i) && (counter == JewelShop) == (i == Item.Jewel);
        public Station CounterFor(Item product) => product == Item.Jewel ? JewelShop : Counter;
        public List<Client> QueueFor(Item product) => product == Item.Jewel ? JewelQueue : Queue;
        /// <summary>Produto na mao de `c` que o balcao dele aceita agora (estoque com espaco), o mais adiantado primeiro; Item.Ore = nenhum.</summary>
        public Item Deliverable(Carrier c)
        {
            for (int k = ItemDefs.Length - 1; k >= (int)Item.Sword; k--) if (IsProduct((Item)k) && c.Held[k] > 0 && Stock[k] < CounterCap) return (Item)k;
            return Item.Ore;
        }
        /// <summary>B-CORE-07: a linha tem cliente (produto com intervalo no Tuning.ClientInterval). Produto sem intervalo (so o teste injeta) nao.</summary>
        public bool HasClients(Item p) => IsProduct(p) && (int)p < T.ClientInterval.Length && T.ClientInterval[(int)p] > 0f;
        /// <summary>B-CORE-07: o upgrade `u` nao exige projeto, ou o projeto que ele exige ja e' posse.</summary>
        public bool TemProjeto(int u) { string b = Upgrades.All[u].Blueprint; return b == null || Projetos.Tem(b); }
        /// <summary>Algum cliente na fila quer `product` e nao ha nenhum no estoque (FASE7 §3: o que destrava a fila sem compra direta).</summary>
        public bool Wanted(Item product) => Stock[(int)product] == 0 && QueueFor(product).Exists(c => c.Want == product);
        /// <summary>Fama: multiplicador (1 -> 0,4) do intervalo entre clientes, pela quantidade de vendas ate aqui.</summary>
        public float Fame => Math.Max(T.FameFloor, (float)Math.Pow(T.FamePer10Sales, Sales / 10));
        public float ClientInterval(Item product) => T.ClientInterval[(int)product] * Fame * (Bought[(int)Upgrade.CounterCapacity] ? T.VitrineClientMul : 1f)
            * (product == Item.Jewel && Bought[(int)Upgrade.JewelVitrine] ? T.JewelVitrineClientMul : 1f);
        /// <summary>Ha um VIP esperando vaga ou numa vaga do balcao.</summary>
        public bool VipActive => _vipWant >= 0 || Queue.Exists(c => c.Vip);
        /// <summary>Botao "chamar o VIP" (anuncio): flag Vip e Ad(Vip).Ligado, depois da 1a venda, sem VIP ativo, com o natural a mais de 60 s e a
        /// recarga de 5 min (P14) zerada (contra abuso).</summary>
        public bool CanSummonVip => T.Vip && T.Ad(Anuncio.Vip).Ligado && AdsUnlocked && FirstSaleTime >= 0f && !VipActive && VipIn > T.VipSummonMinIn && VipCooldown <= 0f;

        /// <summary>D6 (V18): os anuncios (todo placement de rewarded) so aparecem depois da 1a melhoria comprada (knob do Tuning).</summary>
        public bool AdsUnlocked => !T.AnunciosDepoisDaPrimeiraMelhoria || UpgradesBought > 0;

        /// <summary>Anuncio "chamar o VIP": um VIP EXTRA agora (entra na proxima vaga livre), com 2x o pacote do natural (ate 20). NAO mexe no
        /// relogio natural (so o segura em 0 se ele zerar com o extra ainda na vaga). Fora de CanSummonVip recusa (false).</summary>
        public bool SummonVip()
        {
            if (!CanSummonVip) return false;
            SpawnVip(true);
            return true;
        }

        /// <summary>Botao "velocidade" (Ad(Velocidade).Ligado): com boost ativo, ate chegar a 3x; sem boost, so com a recarga (5 min de jogo desde o fim
        /// do ultimo) zerada. Flag desligada nao corta o boost ja ativo: ele acaba no tempo dele.</summary>
        public bool CanBoost => T.Ad(Anuncio.Velocidade).Ligado && AdsUnlocked && (BoostLeft > 0f ? BoostMul < T.BoostMax : BoostCooldown <= 0f);

        /// <summary>Anuncio "velocidade": 2x por 60 s; outro com o boost ativo sobe para 3x e reinicia os 60 s. Fora de CanBoost recusa: nada muda e
        /// devolve 0. Senao devolve o multiplicador novo.</summary>
        public float StartBoost()
        {
            if (!CanBoost) return 0f;
            BoostMul += 1f;   // CanBoost segura o teto de 3x
            BoostLeft = T.Ad(Anuncio.Velocidade).Valor;
            return BoostMul;
        }

        /// <summary>
        /// Sorteia o VIP: produto do balcao principal ja desbloqueado, em rodizio (espada, escudo, ferramenta); fica em _vipWant ate ter vaga.
        /// Pacote: ~7% do ouro de um intervalo medio (300 s x RateEma) a 3x o preco, entre 3 e a prateleira (CounterCap). ponytail: a taxa online
        /// e' o termometro (3 no comeco, 10 no fim); fixo em 3 o VIP caia para 1-3% da janela depois dos 35 min, quando a joia domina a renda.
        /// `extra` = chamado por anuncio: 2x o pacote do natural (o teto de 20 sai daqui: o natural ja vem cortado na prateleira, no maximo 10) e
        /// o relogio natural fica como esta.
        /// </summary>
        void SpawnVip(bool extra)
        {
            var lines = new List<Item>();
            for (Item p = Item.Sword; p <= Item.Tool; p++) if (LineUnlocked(p)) lines.Add(p);
            _vipWant = (int)lines[VipCount % lines.Count];
            double raw = T.VipShare * (T.VipMin + T.VipMax) / 2.0 * RateEma / (T.VipPriceMul * PriceOf((Item)_vipWant));
            int pack = Math.Max(T.VipPackMin, Math.Min(CounterCap, (int)Math.Round(raw)));
            _vipPack = extra ? Math.Min(T.VipSummonPackMax, (int)T.Ad(Anuncio.Vip).Valor * pack) : pack;   // padrao: 2 x 10 = 20, o teto so corta prateleira de Tuning maior
            _vipSummoned = extra;
            VipCount++;
            if (!extra) VipIn = T.VipInterval(VipCount);
        }

        /// <summary>Vaga `i` do balcao (FASE7 §2): QueueCap vagas lado a lado em cima dele, centradas; i = QueueCap = compra direta, logo
        /// depois da ultima. 4 vagas: x 3,2-5,8; 8 vagas: x 1,5-7,5 (compra direta 8,3), dentro da oficina.</summary>
        public V2 ClientSlot(int i) => new V2(Counter.Pos.X + Balance.SlotStep * (i - (QueueCap - 1) / 2f), Counter.Pos.Y + 1.0f);
        public V2 JewelSlot(int i) => new V2(JewelShop.Pos.X + 0.85f * (i - 2), JewelShop.Pos.Y + 1.0f);   // centrada na loja: i 0..4 = x 10,3..13,7 (coordenador)

        // ------------------------------------------------------------------ tick

        public void Tick(float dt, float inX, float inY)
        {
            Events.Clear();
            if (dt <= 0f) return;
            Time += dt;
            long earnedBefore = GoldEarned;
            // FASE8: o boost acelera estacoes, esteira, ajudantes e a chegada de clientes; o ferreiro (andar e maos) no maximo 1,3x, para o
            // joystick nao fugir do controle. Paciencia, atendimento, relogio do VIP e o proprio boost correm no tempo de jogo. Sem boost, fast = hand = dt
            float fast = dt * BoostMul, hand = dt * Math.Min(BoostMul, T.BoostPlayerMax);

            MovePlayer(hand, inX, inY);
            foreach (Carrier w in Workers) WorkerThink(w, fast);
            Interact(Player, hand);
            foreach (Carrier w in Workers) Interact(w, fast);
            foreach (Station s in Stations) Produce(s, fast);
            Conveyor(fast);
            Anuncios.Tick(dt);   // E-PLAT-02: recargas e o Supply Drop entrando nas fornalhas
            VipCooldown = Math.Max(0f, VipCooldown - dt);   // P14: no tempo de jogo; o Serve recarrega quando o chamado sai
            StrikeCooldown = Math.Max(0f, StrikeCooldown - dt);   // B-CORE-03: golpe de mestre, no tempo de jogo (o boost nao encurta)
            Clients(dt);
            Milestones();
            Projetos.Checa();   // B-CORE-07: marcos, Renome e conquista viram projeto (Ev.Blueprint)
            Colecao.Checa();    // B-CORE-08: marcos da colecao (Ev.Colecao), 1x pelo ID
            Orders(dt);
            if (BoostLeft > 0f) { if ((BoostLeft -= dt) <= 0f) { BoostLeft = 0f; BoostMul = 1f; BoostCooldown = T.Ad(Anuncio.Velocidade).Recarga; } }   // recarga conta do fim
            else BoostCooldown = Math.Max(0f, BoostCooldown - dt);

            long cheapest = CheapestVisibleCost();
            // kill criterion do GDD §26 ("andar entre pilhas sem decisao"): anda de maos vazias sem nenhum pad pagavel = puro deslocamento
            if (Player.Moving && Player.Count == 0 && (cheapest < 0 || Gold < cheapest)) WalkNoDecision += dt;
            // FASE8: ouro por segundo de FABRICA (dt x boost): a taxa do cofre offline e do pacote do VIP nao aprende o boost. ponytail: o
            // ferreiro so vai a 1,3x, entao no boost a taxa sai um pouco abaixo da real; congelar a media deixava ela velha para quem vive no boost
            double inst = (GoldEarned - earnedBefore) / (double)fast;
            RateEma += (inst - RateEma) * Math.Min(1.0, dt / Balance.RateTau);
        }

        void MovePlayer(float dt, float inX, float inY)
        {
            float len = (float)Math.Sqrt(inX * inX + inY * inY);
            Player.Moving = len > 0.01f;
            if (len > 1f) { inX /= len; inY /= len; }
            V2 p = Player.Moving ? new V2(Player.Pos.X + inX * Player.Speed * dt, Player.Pos.Y + inY * Player.Speed * dt) : Player.Pos;
            Player.MoveTo(Collide(Clamp(p, MaxX)), dt);   // parado tambem: estacao comprada embaixo do jogador o empurra para fora
        }

        static V2 Clamp(V2 p, float maxX) => new V2(Math.Max(0.3f, Math.Min(maxX, p.X)), Math.Max(0.3f, Math.Min(Balance.WorldH - 0.3f, p.Y)));

        /// <summary>
        /// Personagem (circulo de CharRadius em `p`) contra os corpos solidos: empurra para fora pela normal da caixa, entao o
        /// que sobra do passo e' a tangente (desliza na face e contorna a quina). Centro dentro da caixa sai pelo lado mais
        /// perto; empate sai por baixo (na frente, na view).
        /// </summary>
        public V2 Collide(V2 p)
        {
            float r = Balance.CharRadius;
            for (int pass = 0; pass < 2; pass++)   // ponytail: 2 passes bastam no mapa esparso; vao estreito entre 2 corpos pede solver iterativo
                foreach (Box b in Solids)
                {
                    V2 q = b.Closest(p), d = p - q;
                    float len = d.Len;
                    if (len >= r) continue;
                    if (len > 1e-5f) { p = q + d * (r / len); continue; }
                    float down = p.Y - (b.Pos.Y - b.Half.Y), up = b.Pos.Y + b.Half.Y - p.Y, left = p.X - (b.Pos.X - b.Half.X), right = b.Pos.X + b.Half.X - p.X;
                    float m = Math.Min(Math.Min(down, up), Math.Min(left, right));
                    p = m == down ? new V2(p.X, b.Pos.Y - b.Half.Y - r) : m == up ? new V2(p.X, b.Pos.Y + b.Half.Y + r)
                      : m == left ? new V2(b.Pos.X - b.Half.X - r, p.Y) : new V2(b.Pos.X + b.Half.X + r, p.Y);
                }
            return p;
        }

        /// <summary>
        /// Direcao (unitaria) de quem anda sozinho rumo a `target` (ajudante e bot; o jogador humano nao): encostado num corpo
        /// que fica na frente da linha reta ate o alvo, contorna pelas quinas (caixa inflada pelo raio) no sentido de menor
        /// caminho, que soma quina a quina ate o alvo ficar visivel. Sem isso o empurrao so anula o passo e ele trava atras da
        /// estacao. Empate: anti-horario.
        /// Impasse (Leva 4; econsim: Ideal a 36,36 ticks/s parado aos 31 min na quina da Ferramentas): quem bate de frente numa quina
        /// fica na curva dela, a ~0,10 m (r(raiz 2 - 1)) do canto da caixa inflada; mirar esse canto o leva para FORA da faixa de
        /// contato, de la a reta ao alvo volta para a mesma quina e o Collide o devolve ao mesmo ponto (vai-e-volta sem fim). Com
        /// `who` andando sem progresso ha Balance.StuckSeconds, quem esta a menos de r do canto ja conta como na quina e mira o ponto
        /// seguinte da rota. So age no impasse: fora dele a rota e' a de sempre (o antigo "ja na quina" de 0,05 m nunca disparava,
        /// encostado nunca chega a menos de 0,09 m do canto), entao os numeros do bot nao mudam.
        /// </summary>
        public V2 Steer(Carrier who, V2 target)
        {
            V2 from = who.Pos, d = target - from;
            float dl = d.Len;
            if (dl < 1e-5f) return new V2(0f, 0f);
            float r = Balance.CharRadius;
            // FASE7: todos os corpos encostados que barram a linha (antes: so o 1o da lista), e rota cuja quina cai dentro de OUTRO corpo
            // (vao mais estreito que o personagem) e' descartada: na boca da Loja de joias, entre a loja e os pedestais da Joalheria real
            // (vaos de 0,1-0,2 m), o bot ia e voltava no vao para sempre. ponytail: resolve vao entre 2 corpos; labirinto pede grafo de waypoints.
            V2 bestStep = d; float bestLen = float.MaxValue;
            bool stuck = who.StuckT >= Balance.StuckSeconds;   // ponytail: ligado sempre contorna melhor toda quina, mas mexe na curva do bot (BALANCE inteiro de novo)
            foreach (Box b in Solids)
            {
                V2 n = from - b.Closest(from), shrunk = b.Half + new V2(r - 0.02f, r - 0.02f);
                if (n.Len > r + 0.01f || !Crosses(from, target, b.Pos, shrunk)) continue;
                V2 h = b.Half + new V2(r, r);
                int face = Math.Abs(n.Y) >= Math.Abs(n.X) ? (n.Y < 0f ? 0 : 2) : (n.X > 0f ? 1 : 3);   // 0 baixo, 1 direita, 2 cima, 3 esquerda
                for (int turn = 1; turn <= 3; turn += 2)   // quinas em ordem anti-horaria; +1 = anti-horario, +3 = horario
                {
                    int i = turn == 1 ? (face + 1) % 4 : face;
                    V2 step = Corner(b.Pos, h, i) - from;
                    bool open = Fits(from + step, b);
                    if (stuck && step.Len < r)   // ja na quina: rumo ao ponto seguinte da rota (a proxima quina, ou o alvo se ele ja aparece dali)
                    {
                        V2 c = Corner(b.Pos, h, i), next = Crosses(c, target, b.Pos, shrunk) ? Corner(b.Pos, h, (i + turn) % 4) : target;
                        step = next - from; open = Fits(next, b);
                    }
                    float len = V2.Dist(from, Corner(b.Pos, h, i));
                    for (int k = 0; k < 4 && Crosses(Corner(b.Pos, h, i), target, b.Pos, shrunk); k++)
                    {
                        int j = (i + turn) % 4;
                        len += V2.Dist(Corner(b.Pos, h, i), Corner(b.Pos, h, j));
                        i = j;
                        open &= Fits(Corner(b.Pos, h, i), b);
                    }
                    len += V2.Dist(Corner(b.Pos, h, i), target);
                    if (open && len < bestLen) { bestLen = len; bestStep = step; }
                }
            }
            return bestStep * (1f / bestStep.Len);
        }

        /// <summary>O personagem cabe em `p` (quina de rota do Steer) sem entrar em outro corpo que nao `except`?</summary>
        bool Fits(V2 p, Box except)
        {
            foreach (Box o in Solids)
                if (!(o.Pos.X == except.Pos.X && o.Pos.Y == except.Pos.Y) && o.Dist(p) < Balance.CharRadius - 0.01f) return false;
            return true;
        }

        /// <summary>Quina `i` (0 baixo-esquerda, 1 baixo-direita, 2 cima-direita, 3 cima-esquerda) da caixa de centro `c` e meia-medida `h`.</summary>
        static V2 Corner(V2 c, V2 h, int i) => new V2(i == 1 || i == 2 ? c.X + h.X : c.X - h.X, i >= 2 ? c.Y + h.Y : c.Y - h.Y);

        /// <summary>O segmento a -> b atravessa a caixa (centro c, meia-medida h)? Teste das faixas.</summary>
        static bool Crosses(V2 a, V2 b, V2 c, V2 h)
        {
            float t0 = 0f, t1 = 1f;
            return Slab(a.X - c.X, b.X - a.X, h.X, ref t0, ref t1) && Slab(a.Y - c.Y, b.Y - a.Y, h.Y, ref t0, ref t1);
        }

        static bool Slab(float p, float d, float h, ref float t0, ref float t1)
        {
            if (Math.Abs(d) < 1e-6f) return p > -h && p < h;
            float u0 = (-h - p) / d, u1 = (h - p) / d;
            if (u0 > u1) { float x = u0; u0 = u1; u1 = x; }
            t0 = Math.Max(t0, u0); t1 = Math.Min(t1, u1);
            return t0 < t1;
        }

        /// <summary>
        /// Ponto intermediario para cruzar a parede lateral (x 9,0-9,6): alvo do outro lado = o meio da abertura (porta ou
        /// arco) com o caminho mais curto, na altura em que a reta cruzaria, dentro da faixa livre; alvo do mesmo lado = o alvo.
        /// ponytail: regra de 2 aberturas, sem pathfinding; mais paredes pedem grafo de waypoints.
        /// </summary>
        public static V2 Via(V2 from, V2 target)
        {
            float x = (Balance.SideWallX0 + Balance.SideWallX1) / 2f;
            if (!(from.X < x && target.X > x) && !(from.X > x && target.X < x)) return target;
            // ja no meio do vao (so da para estar em x 9,3 dentro de uma abertura): segue direto. Sem isso, quem chega ao ponto
            // de passagem com x 9,29999 (arredondamento) mira o proprio lugar, o Steer devolve direcao zero e trava ali (Leva 11)
            if (Math.Abs(from.X - x) < 0.01f) return target;
            float cross = from.Y + (target.Y - from.Y) * (x - from.X) / (target.X - from.X), m = Balance.CharRadius + 0.1f;
            V2 best = target; float bestLen = float.MaxValue;
            for (int i = 0; i + 1 < Balance.SideWallOpenings.Length; i += 2)
            {
                var p = new V2(x, Math.Max(Balance.SideWallOpenings[i] + m, Math.Min(Balance.SideWallOpenings[i + 1] - m, cross)));
                float len = V2.Dist(from, p) + V2.Dist(p, target);
                if (len < bestLen) { bestLen = len; best = p; }
            }
            return best;
        }

        /// <summary>Move `c` em direcao a `target` desviando dos corpos; devolve true quando chegou (dentro de `reach`).</summary>
        public bool MoveTowards(Carrier c, V2 target, float dt, float reach)
        {
            V2 d = target - c.Pos;
            float len = d.Len;
            c.Moving = len > reach;
            if (!c.Moving) return true;
            V2 goal = Via(c.Pos, target);
            V2 dir = Steer(c, goal);
            c.MoveTo(Collide(Clamp(c.Pos + dir * Math.Min(V2.Dist(c.Pos, goal), c.Speed * dt), Balance.WorldW - 0.3f)), dt);   // ponytail: ajudante usa o mapa inteiro; so mira estacao desbloqueada, entao nunca entra na rua fechada
            return false;
        }

        // ------------------------------------------------------------------ ajudantes

        void WorkerThink(Carrier w, float dt)
        {
            if (w.State == 0)
            {
                if (w.Count >= w.Cap || (w.Count > 0 && w.IdleT > Balance.WorkerPatience)) { w.State = 1; w.Target = -1; w.IdleT = 0f; }
            }
            else if (w.Count == 0) { w.State = 0; w.Target = -1; w.IdleT = 0f; }

            Station t;
            if (w.Role == 3) t = w.State == 0 ? JewelerSource(w) : w.Item == Item.Jewel ? JewelShop : JewelBench;   // joalheiro: re-escolhe todo tick
            else
            {
                t = w.Target >= 0 ? Stations[w.Target] : null;
                bool valid = t != null && t.Unlocked && (w.State == 0 ? IsSourceFor(w, t) : IsDestFor(w, t));
                if (!valid) t = w.State == 0 ? PickSource(w) : PickDest(w);
            }
            w.Target = t != null ? t.Index : -1;
            if (t == null) { w.Moving = false; return; }
            MoveTowards(w, w.State == 0 ? t.OutAt : t.InAt, dt, Balance.WorkerReach);   // buscando -> saida da fonte; entregando -> entrada do destino
        }

        bool IsSourceFor(Carrier w, Station s)
        {
            if (w.Role == 0) return s.Kind == Kind.Deposit;
            if (w.Count > 0 && s.OutItem != w.Item) return false;
            if (w.Role == 2 && !Fits(w, s)) return false;
            return s.Produces && (w.Role == 1 ? s.Kind == Kind.Furnace : s.Kind == Kind.Crafter) && (s.Out > 0 || w.Target == s.Index);
        }

        /// <summary>Ajudante de produto: so busca o que ainda cabe na prateleira (sem a compra direta o estoque so desce com cliente
        /// na vaga; com a prateleira cheia ele parava no balcao e escudo/ferramenta encalhavam). Mesma regra do JewelerSource.</summary>
        bool Fits(Carrier w, Station s) => Stock[(int)s.OutItem] + w.Count < CounterCap;

        bool IsDestFor(Carrier w, Station s)
        {
            if (w.Role == 2) return Sells(s, w.Item) && Stock[(int)w.Item] < CounterCap;
            return s.Accepts(w.Item) && (w.Role == 0 ? s.Kind == Kind.Furnace : s.Kind == Kind.Crafter);
        }

        Station PickSource(Carrier w)
        {
            if (w.Role == 0) return Deposit;
            Station best = null; float bestScore = float.MinValue;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked || !s.Produces || (w.Role == 1 ? s.Kind != Kind.Furnace : s.Kind != Kind.Crafter)) continue;
                if (w.Count > 0 && s.OutItem != w.Item) continue;
                if (w.Role == 2 && !Fits(w, s)) continue;
                if (s.Out == 0 && w.Count == 0 && !(s.Busy && s.Progress > 0.5f)) continue;   // vale esperar o que esta quase pronto
                float score = s.Out * 10f - V2.Dist(w.Pos, s.Pos);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        Station PickDest(Carrier w)
        {
            if (w.Role == 2) return CounterFor(w.Item);
            Station best = null; float bestScore = float.MinValue;
            foreach (Station s in Stations)
            {
                int k = s.InIndex(w.Item);   // -1 = nao produz ou a receita nao usa
                if (!s.Unlocked || k < 0 || (w.Role == 0 ? s.Kind != Kind.Furnace : s.Kind != Kind.Crafter)) continue;
                int room = s.InCap - s.Ins[k];
                if (room <= 0) continue;
                float score = room * 4f + Need(s) - V2.Dist(w.Pos, s.Pos);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        /// <summary>
        /// Joalheiro (papel 3): vazio (ou ja com joia), joia pronta e espaco na loja -> bancada de joias; senao, enquanto a
        /// bancada de joias ainda cabe lingote, a fornalha com mais lingote (a atual ganha empate: nao fica indo e voltando).
        /// Bancada abastecida = null: nao tira lingote das outras linhas. Action so deixa ele mexer no proprio alvo.
        /// </summary>
        Station JewelerSource(Carrier w)
        {
            bool jewel = w.Count > 0 && w.Item == Item.Jewel;
            if ((w.Count == 0 || jewel) && JewelBench.Out > 0 && Stock[(int)Item.Jewel] + w.Count < CounterCap) return JewelBench;
            if (jewel || JewelBench.In + w.Count >= JewelBench.InCap) return null;
            Station best = null; float bestScore = float.MinValue;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked || s.Kind != Kind.Furnace || (s.Out == 0 && !s.Busy)) continue;
                float score = s.Out * 10f - V2.Dist(w.Pos, s.Pos) + (s.Index == w.Target ? 5f : 0f);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        /// <summary>Quanto o balcao que vende o produto desta bancada precisa dele (cliente da frente esperando pesa muito).</summary>
        float Need(Station crafter)
        {
            if (crafter.Kind != Kind.Crafter) return 0f;
            Item it = crafter.OutItem;
            List<Client> q = QueueFor(it);
            float n = (CounterCap - Stock[(int)it]) * 2f;
            if (q.Count > 0 && q[0].Want == it && Stock[(int)it] == 0) n += 12f;
            return n;
        }

        // ------------------------------------------------------------------ trocas automaticas

        /// <summary>Estacao desbloqueada cuja zona (boca) contem `p`; `outZone` = boca de SAIDA (so recolhe), senao ENTRADA (so deposita).</summary>
        public Station StationAt(V2 p, out bool outZone)
        {
            Station best = null; float bd = Balance.MouthRadius; outZone = false;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked) continue;
                float dIn = V2.Dist(p, s.InAt), dOut = V2.Dist(p, s.OutAt);
                if (dIn <= bd) { bd = dIn; best = s; outZone = false; }
                if (s.Produces && dOut <= bd) { bd = dOut; best = s; outZone = true; }
            }
            return best;
        }

        public Pad PadAt(V2 p)
        {
            foreach (Pad pad in Pads)
                if (pad.Current(this) >= 0 && V2.Dist(p, pad.Pos) <= Balance.PadRadius) return pad;
            return null;
        }

        /// <summary>Cabe mais um `i` na mao? Teto POR TIPO. Jogador: qualquer tipo, sem bloqueio por misturar (FASE7 §1: mao cheia de
        /// lingote e entrada da bigorna cheia, ele ainda recolhe as espadas da saida). Ajudante: um tipo por vez, so o do papel dele.</summary>
        public bool CanPick(Carrier c, Item i)
        {
            if (c.Held[(int)i] >= c.Cap) return false;
            if (c.Role < 0) return true;
            if (c.Count > 0 && c.Item != i) return false;
            if (c.Role == 0) return i == Item.Ore;
            if (c.Role == 1) return i == Item.Ingot;
            if (c.Role == 2) return IsProduct(i);
            if (c.Role == 3) return i == Item.Ingot || i == Item.Jewel;
            return true;
        }

        /// <summary>0 nada, 1 deposita na entrada, 2 pega da saida, 3 pega minerio, 4 abastece o balcao; `item` = o que passa de mao.
        /// Boca de entrada nunca recolhe e boca de saida nunca deposita (FASE5 §2b): da para abastecer sem tirar o que ja ficou pronto.
        /// Entrada recebe so os insumos da receita (B-CORE-01: o 1o da receita que esta na mao e cabe), saida entrega so o OutItem, balcao
        /// recebe os produtos que vende (FASE7 §1, pilha mista).</summary>
        int Action(Carrier c, Station s, bool outZone, out Item item)
        {
            item = s.Kind == Kind.Deposit ? Item.Ore : outZone ? s.OutItem : s.InItem;
            if (c.Role == 3 && s.Index != c.Target) return 0;   // joalheiro so mexe no proprio alvo: nao abastece a bancada do caminho
            switch (s.Kind)
            {
                case Kind.Deposit: return CanPick(c, Item.Ore) ? 3 : 0;
                case Kind.Counter:
                    for (int k = (int)Item.Sword; k < ItemDefs.Length; k++)   // B-CORE-07: todo produto (a espada longa entrou)
                        if (c.Held[k] > 0 && Sells(s, (Item)k) && Stock[k] < CounterCap) { item = (Item)k; return 4; }
                    return 0;
                default:
                    if (outZone) return s.Out > 0 && CanPick(c, s.OutItem) ? 2 : 0;
                    for (int k = 0; k < s.Ins.Length && s.Produces; k++)
                        if (c.Has(s.Recipe.Inputs[k].Item) && s.Ins[k] < s.InCap) { item = s.Recipe.Inputs[k].Item; return 1; }
                    return 0;
            }
        }

        void Interact(Carrier c, float dt)
        {
            if (c.Role < 0)
            {
                Pad pad = PadAt(c.Pos);
                if (pad != _padUnder) { _padUnder = pad; _padDwell = 0f; }
                if (pad == null) foreach (Pad p in Pads) p.Armed = true;
                else
                {
                    _padDwell += dt;
                    if (pad.Armed && (!c.Moving || _padDwell >= Balance.PadDwell)) Drain(pad, dt);
                }
            }
            Station s = StationAt(c.Pos, out bool outZone);
            Item it = Item.Ore;
            int act = s != null ? Action(c, s, outZone, out it) : 0;
            if (act == 0) { c.ActT = 0f; c.IdleT += dt; return; }
            c.IdleT = 0f;
            c.ActT += dt;
            float step = act == 3 ? Balance.GrabTime : Balance.TransferTime;
            while (c.ActT >= step)
            {
                c.ActT -= step;
                switch (act)
                {
                    // B-CORE-03: a peca passa de mao com o nivel dela, a melhor primeiro (insumo e minerio sao sempre Normal)
                    case 1: Take(ref c.Held[(int)it], c.HeldQ[(int)it]); s.Ins[s.InIndex(it)]++; break;
                    case 2: Put(ref c.Held[(int)it], c.HeldQ[(int)it], Take(ref s.Out, s.OutQ)); break;
                    case 3: c.Held[(int)it]++; break;
                    case 4: Put(ref Stock[(int)it], StockQ[(int)it], Take(ref c.Held[(int)it], c.HeldQ[(int)it])); break;
                }
                Emit(act == 2 || act == 3 ? Ev.Picked : Ev.Deposited, (int)it, c.Role, s.Pos);
                if (Action(c, s, outZone, out it) != act) { c.ActT = 0f; break; }   // balcao: o proximo produto da mao segue no mesmo ritmo
            }
        }

        void Drain(Pad pad, float dt)
        {
            int u = pad.Current(this);
            if (u < 0) return;
            long cost = T.Cost(u);
            pad.Acc += cost / Balance.PadDrainSeconds * dt;
            long want = (long)pad.Acc;
            pad.Acc -= want;
            long step = Math.Min(want, Math.Min(Gold, cost - pad.Paid));
            if (step <= 0) return;
            Gold -= step;
            pad.Paid += step;
            if (pad.Paid >= cost) { pad.Paid = 0; pad.Acc = 0; pad.Armed = false; Buy((Upgrade)u); }
        }

        /// <summary>Compra direta (pad completo, teste ou cheat). Idempotente: ja comprado devolve false.</summary>
        public bool Buy(Upgrade u)
        {
            if (Bought[(int)u]) return false;
            if (Upgrades.IsLuxury((int)u) && (!ProductionComplete || !Bought[Upgrades.All[(int)u].Requires])) return false;
            if (!TemProjeto((int)u)) return false;   // B-CORE-07: nivel IV-V e bigorna longa so com o projeto
            int workersBefore = Workers.Count;
            Bought[(int)u] = true;
            UpgradeTime[(int)u] = Time;
            UpgradesBought++;
            Recompute();
            Emit(Ev.Bought, (int)u, (int)Math.Min(int.MaxValue, T.Cost(u)), PadOf(u));   // ponytail: evento e' int; acima de 2,1 bi sai no teto (so o diario le)
            foreach (Station s in Stations) if (s.UnlockBy == (int)u) Emit(Ev.Unlocked, (int)s.OutItem, s.Index, s.Pos);
            if (Workers.Count > workersBefore) Emit(Ev.Hired, Workers.Count - 1, 0, HireSpot);
            return true;
        }

        /// <summary>
        /// Compra pelo menu inferior (FASE6): so upgrade InMenu, nao comprado, com o pre-requisito comprado e Gold >= Cost. Cobra
        /// na hora (sem dreno) e aplica pelo MESMO Buy do pad (Ev.Bought na posicao do jogador). Senao devolve false e nada muda.
        /// Fora do Tick o evento entra em Events na hora; o proximo Tick limpa a lista.
        /// </summary>
        public bool TryBuyMenu(Upgrade u)
        {
            long cost = T.Cost(u);
            if (!MenuAvailable((int)u) || Gold < cost || !Buy(u)) return false;
            Gold -= cost;
            return true;
        }

        /// <summary>Upgrade do menu a venda agora (com ou sem ouro): InMenu, nao comprado, pre-requisito comprado e o projeto que ele exige (B-CORE-07).</summary>
        public bool MenuAvailable(int u)
        {
            UpgradeDef d = Upgrades.All[u];
            return d.InMenu && !Bought[u] && (d.Requires < 0 || Bought[d.Requires]) && TemProjeto(u);
        }

        V2 PadOf(Upgrade u)
        {
            if (Upgrades.All[(int)u].InMenu) return Player.Pos;   // menu: o efeito sai do jogador, nao do pad antigo
            foreach (Pad p in Pads) foreach (Upgrade c in p.Chain) if (c == u) return p.Pos;
            return Player.Pos;
        }

        /// <summary>Deriva TUDO dos flags Bought (idempotente: vale depois de Buy e depois de Load).</summary>
        public void Recompute()
        {
            float ft = Bought[(int)Upgrade.FurnaceSpeed2] ? T.FurnaceTime2 : Bought[(int)Upgrade.FurnaceSpeed1] ? T.FurnaceTime1 : T.FurnaceTime0;
            float ht = Bought[(int)Upgrade.HammerSpeed] ? T.HammerTime1 : T.HammerTime0;
            foreach (Station s in Stations)
            {
                if (s.UnlockBy >= 0) s.Unlocked = Bought[s.UnlockBy];
                // B-CORE-03: nivel da familia = 1 + niveis comprados dela
                s.Level = 1;
                if (s.Produces) foreach (UpgradeDef d in Upgrades.All) if (d.Station == s.Recipe.Family && Bought[(int)d.Up]) s.Level++;   // B-CORE-07: pela familia (bigorna longa = bigornas)
                // B-CORE-01: base do tipo (fornalha: Fole; bancada: Martelo veloz) x multiplicador da receita; a Lupa so na Joalheria. B-CORE-03:
                // cada nivel acima do 1 da +5% de velocidade (nivel 1 divide por 1: o tempo de antes, bit a bit)
                if (s.Produces) s.Time = (s.Kind == Kind.Furnace ? ft : ht) * (s.Recipe.TimeMul(T) * (s == JewelBench && Bought[(int)Upgrade.JewelSpeed] ? T.JewelSpeedMul : 1f))
                    / (1f + T.NivelEstacaoVelocidade * (s.Level - 1)) / Equipe.Ritmo(s);   // B-CORE-04: oficial e mestre de obras (ninguem = / 1f)
                if (s.Produces) s.OutCap = (s.Kind == Kind.Furnace ? Balance.FurnaceOut : Balance.CrafterOut) + Equipe.Saida(s);   // B-CORE-04: almoxarife
            }
            // balcao evolutivo (FASE7 §2): +1 vaga por evolucao comprada; o corpo alarga junto (antes dos Solids)
            QueueCap = T.QueueCap0;
            for (int u = (int)Upgrade.Counter5; u <= (int)Upgrade.Counter8; u++) if (Bought[u]) QueueCap++;
            QueueCap = Math.Min(QueueCap, Balance.QueueCapMax);   // trava de geometria: um QueueCap0 maior no Tuning nao tira o balcao da oficina
            Counter.Half = new V2(Balance.CounterHalfX(QueueCap), Balance.StationHalf.Y);
            // corpos: estacao travada nao e' solida (o pad de compra fica no lugar dela); loja de joias sim (a view desenha a fachada fechada)
            Solids.Clear();
            foreach (Station s in Stations) if (s.Unlocked || s.Kind == Kind.Counter) Solids.Add(s.Body);
            foreach (ObstacleDef o in Balance.Obstacles) if (o.Requires < 0 || Bought[o.Requires]) Solids.Add(o.Body);
            MaxX = (Bought[(int)Upgrade.SideCorridor] ? Balance.WorldW : Balance.WorkshopW) - 0.3f;
            Player.Speed = Bought[(int)Upgrade.PlayerSpeed] ? Balance.PlayerSpeedUp : Balance.PlayerSpeed;
            Player.Cap = Bought[(int)Upgrade.PlayerCapacity] ? Balance.PlayerCapUp : Balance.PlayerCap;
            CounterCap = (Bought[(int)Upgrade.CounterCapacity] ? T.CounterCap1 : T.CounterCap0) + Equipe.Estoque;   // B-CORE-04: almoxarife
            JewelQueueCap = Bought[(int)Upgrade.JewelVitrine] ? Balance.JewelQueueCapUp : Balance.JewelQueueCap;
            HasConveyor = Bought[(int)Upgrade.Conveyor];
            // um ajudante por upgrade de contratacao (papel em HireRole); a lista so cresce, na ordem da compra (o Joalheiro nao exige o Ajudante 3)
            for (int k = 0; k < HireBy.Length; k++)
            {
                if (!Bought[(int)HireBy[k]]) continue;
                int role = HireRole[k], want = 0, have = 0;
                for (int j = 0; j <= k; j++) if (HireRole[j] == role && Bought[(int)HireBy[j]]) want++;
                foreach (Carrier w in Workers) if (w.Role == role) have++;
                if (have < want) Workers.Add(new Carrier(ItemDefs.Length) { Pos = HireSpot, Role = role, Wk = HireDef[k] });
            }
            bool fast = Bought[(int)Upgrade.HelperSpeed];
            foreach (Carrier w in Workers)   // B-CORE-04: nivel do ajudante (nivel 1 = x 1f e + 0)
            {
                w.Speed = (fast ? Balance.WorkerSpeedUp : Balance.WorkerSpeed) * Equipe.Passo(w);
                w.Cap = (fast ? Balance.WorkerCapUp : Balance.WorkerCap) + Equipe.Carga(w);
            }
        }

        // ------------------------------------------------------------------ producao, esteira, clientes

        void Produce(Station s, float dt)
        {
            if (!s.Unlocked || !s.Produces) return;
            if (s.Busy)
            {
                if (s.Progress < 1f) s.Progress = Math.Min(1f, s.Progress + dt / s.Time);
                if (s.Progress >= 1f)
                {
                    if (s.Out < s.OutCap)
                    {
                        s.Busy = false; s.Progress = 0f; s.Blocked = false; s.Crafted++; Crafted[(int)s.OutItem]++;
                        Quality l = Grade(s);   // B-CORE-03: acumulador e golpe
                        if (l == Quality.Masterwork) { Masterworks++; Hall.Registra(s); }   // vai ao Hall (B-CORE-08): nunca a pilha nem o balcao
                        else
                        {
                            Put(ref s.Out, s.OutQ, l);
                            if (l > Quality.Normal) Emit(Ev.Graded, s.Index, (int)l, s.Pos);
                        }
                        Emit(Ev.Crafted, (int)s.OutItem, s.Index, s.Pos);
                    }
                    else Stall(s, dt);
                }
            }
            if (s.Busy) return;
            for (int k = 0; k < s.Ins.Length; k++) if (s.Ins[k] < s.Recipe.Inputs[k].Qty) { s.StarveSeconds += dt; return; }   // so com a receita inteira
            if (s.Out >= s.OutCap) { Stall(s, dt); return; }
            for (int k = 0; k < s.Ins.Length; k++) s.Ins[k] -= s.Recipe.Inputs[k].Qty;
            s.Busy = true; s.Progress = 0f;
        }

        void Stall(Station s, float dt)
        {
            s.StallSeconds += dt;
            if (s.Blocked) return;
            s.Blocked = true;
            Emit(Ev.Bottleneck, s.Index, 0, s.Pos);
        }

        void Conveyor(float dt)
        {
            if (!HasConveyor) return;
            _convT += dt;
            while (_convT >= T.ConveyorTime)
            {
                _convT -= T.ConveyorTime;
                if (FurnaceA.Out <= 0 || !AnvilA.Accepts(FurnaceA.OutItem)) { _convT = 0f; break; }
                FurnaceA.Out--; AnvilA.Ins[AnvilA.InIndex(FurnaceA.OutItem)]++;
                Emit(Ev.Deposited, (int)FurnaceA.OutItem, 9, AnvilA.Pos);   // B = 9: veio pela esteira
            }
        }

        void Clients(float dt)
        {
            // FASE8: o relogio do VIP corre so depois da 1a venda; no zero sorteia o VIP se nao houver outro ativo (senao segura em 0 ate ele
            // sair). O sorteado pega a proxima vaga livre ANTES de quem chega neste tick: com a fila cheia ele espera, nao some.
            // Flag Vip desligada: o relogio para e nao sorteia; o VIP que ja estava sorteado (pendente do save) ainda entra e leva o pacote
            if (T.Vip && FirstSaleTime >= 0f)
            {
                VipIn = Math.Max(0f, VipIn - dt);
                if (VipIn <= 0f && !VipActive) SpawnVip(false);
            }
            if (_vipWant >= 0 && Queue.Count < QueueCap)
            {
                float pat = T.VipPatienceFor(_vipPack);   // pacote maior, mais tempo: o humano e' mais lento que o bot
                Queue.Add(new Client { Want = (Item)_vipWant, Patience = pat, MaxPatience = pat, Vip = true, Summoned = _vipSummoned, Pack = _vipPack, Paid = _vipPaid });
                if (!_vipQuiet) Emit(Ev.VipArrived, _vipWant, _vipPack, ClientSlot(Queue.Count - 1));
                _vipWant = -1; _vipPaid = 0; _vipQuiet = false; _vipSummoned = false;
            }
            for (int k = (int)Item.Sword; k < ItemDefs.Length; k++)   // B-CORE-07: toda linha aberta com cliente (a espada longa entrou)
            {
                Item p = (Item)k;
                if (!LineUnlocked(p) || !HasClients(p)) continue;
                _clientT[(int)p] -= dt * BoostMul;   // FASE8: a demanda acompanha o boost
                if (_clientT[(int)p] > 0f) continue;
                _clientT[(int)p] += ClientInterval(p);
                bool j = p == Item.Jewel;
                List<Client> q = QueueFor(p);
                int cap = j ? JewelQueueCap : QueueCap;
                float patience = T.PatienceFor(p, RecipeDefs) * (j ? 1f : Equipe.Paciencia);   // B-CORE-04: balconista (so o balcao principal)
                if (q.Count < cap)
                {
                    q.Add(new Client { Want = p, Patience = patience, MaxPatience = patience });
                    Emit(Ev.ClientArrived, (int)p, q.Count, Slot(j, q.Count - 1));
                }
                // fila cheia: vai embora sem comprar. FASE7 §3: a "compra direta da vitrine" (BALANCE §9) saiu, porque vendia sem cliente
                // visivel ("+10" numa vaga vazia; 28-30% das vendas do balcao em 45 min). A trava que ela resolvia agora se desfaz pela
                // paciencia por item, pelas vagas do balcao evolutivo e pela carga mista do ferreiro (CoreTests.FilaCheia_...)
                else { ClientsTurnedAway++; Emit(Ev.ClientLeft, (int)p, 2, Slot(j, cap)); }
            }
            if (Queue.Count > MaxQueue) MaxQueue = Queue.Count;
            Serve(Queue, false, ref _serveT, dt);
            Serve(JewelQueue, true, ref _serveJ, dt);   // a loja de joias tem o proprio atendente (timer separado)
        }

        V2 Slot(bool jewel, int i) => jewel ? JewelSlot(i) : ClientSlot(i);

        public int PriceOf(Item item) => item == Item.Jewel && Bought[(int)Upgrade.JewelVitrine] ? T.JewelPriceUp : T.Price[(int)item];

        /// <summary>Vende 1 `want` do balcao: a melhor qualidade primeiro (B-CORE-03), ao PriceOf do nivel x `mul` (VIP = 3x). Masterwork nunca
        /// chega aqui (nao entra em lote: Grade a manda ao Hall).</summary>
        int Sell(Item want, V2 pos, int mul = 1)
        {
            Quality l = Take(ref Stock[(int)want], StockQ[(int)want]);
            Renome.OnVendaQualidade((int)l);   // B-CORE-06: +1 a cada 10 Superiores vendidas
            Projetos.OnVenda((int)want, l);    // B-CORE-07: marco "N Fina ou melhor"
            Colecao.OnVenda((int)want, l);     // B-CORE-08: aba Produtos (item x qualidade)
            int price = PriceOf(want, l) * mul;
            QualityGold += price - PriceOf(want, Quality.Normal) * mul;
            Gold += price; GoldEarned += price; Sales++; SoldItems[(int)want]++;
            if ((int)want == OrderItem && OrderProgress < OrderTarget) OrderProgress++;
            if (FirstSaleTime < 0f) FirstSaleTime = Time;
            Emit(Ev.Sold, (int)want, price, pos);
            return price;
        }

        /// <summary>
        /// Baus de marco (docs/archive/AREA2_FASE2.md §3): o jogador em cima de um disponivel abre e ganha o ouro (uma vez: State 2
        /// vai no save); um marco batido so vira bau quando nao ha outro disponivel no mesmo lugar (um por vez na tela), e
        /// entao emite Milestone. O ouro do bau NAO entra em GoldEarned (nao infla a taxa online nem o cofre offline).
        /// Flag Baus desligada: nenhum bau novo aparece; o que ja estava na tela ainda abre.
        /// </summary>
        void Milestones()
        {
            foreach (Chest c in Chests)
            {
                if (c.State != 1 || V2.Dist(Player.Pos, c.Pos) > Balance.PadRadius) continue;
                c.State = 2; Gold += c.Gold;
                Emit(Ev.ChestOpened, c.Index, c.Gold, c.Pos);
            }
            if (!T.Baus) return;
            foreach (Chest c in Chests)
            {
                MilestoneDef m = Balance.Milestones[c.Index];
                if (c.State != 0 || (m.Item < 0 ? Sales : SoldItems[m.Item]) < m.Count || !ChestSpotFree(c.Pos)) continue;
                c.State = 1;
                Emit(Ev.Milestone, c.Index, c.Gold, c.Pos);
            }
        }

        /// <summary>Lugar livre para um bau novo: nenhum outro disponivel ali e o jogador fora de cima (senao o proximo abria no tick seguinte, sem ser visto).</summary>
        bool ChestSpotFree(V2 pos)
        {
            if (V2.Dist(Player.Pos, pos) <= Balance.PadRadius) return false;
            foreach (Chest c in Chests) if (c.State == 1 && V2.Dist(c.Pos, pos) < 0.01f) return false;
            return true;
        }

        void Serve(List<Client> q, bool jewel, ref float serveT, float dt)
        {
            serveT -= dt;
            if (serveT <= 0f)
            {
                // atende o primeiro da fila que da para atender: um cliente de escudo sem escudo no balcao nao trava quem quer espada
                for (int i = 0; i < q.Count; i++)
                {
                    Client c = q[i];
                    if (Stock[(int)c.Want] <= 0) continue;
                    serveT = jewel ? Balance.ServeTime : Equipe.Atendimento;   // B-CORE-04: balconista no balcao principal
                    if (c.Vip)   // FASE8: uma unidade por atendimento a 3x o preco; fica na vaga ate levar o pacote todo
                    {
                        c.Paid += Sell(c.Want, Slot(jewel, i), T.VipPriceMul);
                        if (--c.Pack > 0) break;
                        q.RemoveAt(i);
                        if (c.Summoned) VipCooldown = T.Ad(Anuncio.Vip).Recarga;   // P14: a recarga conta de quando o chamado sai
                        Emit(Ev.VipServed, (int)c.Want, c.Paid, Slot(jewel, i));
                        break;
                    }
                    q.RemoveAt(i);
                    Sell(c.Want, Slot(jewel, i));
                    break;
                }
            }
            for (int i = q.Count - 1; i >= 0; i--)
            {
                Client c = q[i];
                c.Patience -= dt;
                if (c.Patience > 0f) continue;
                if (c.Vip) Emit(Ev.VipLeft, (int)c.Want, c.Pack, Slot(jewel, i));   // FASE8: o VIP fica fora do ClientsLost (metrica dos clientes normais)
                else { Emit(Ev.ClientLeft, (int)c.Want, 0, Slot(jewel, i)); ClientsLost++; }
                if (c.Summoned) VipCooldown = T.Ad(Anuncio.Vip).Recarga;   // P14: cansou tambem conta (sem isso, deixar ir embora furava a recarga)
                q.RemoveAt(i);
            }
        }

        internal void Emit(Ev kind, int a, int b, V2 pos)   // internal: a Equipe (B-CORE-04) emite a contratacao e o nivel; o Projetos (B-CORE-07), o Ev.Blueprint
        {
            Events.Add(new SimEvent(kind, a, b, pos));
            Metas.On(kind, a, b);   // D-CORE-02: tambem os de fora do Tick (menu, cofre)
            Renome.On(kind, a, b);  // B-CORE-06: encomenda entregue e bau aberto
            LiveOps.On(kind, a, b); // D-CORE-03/04: missoes do evento ativo e XP de encomenda do Pass
        }

        /// <summary>
        /// FASE9: encomenda entregue paga o premio (como o bau, FORA do GoldEarned: nao infla a taxa online nem o cofre) e a proxima vem
        /// OrderGap s depois. A 1a (5 espadas) vem OrderFirstDelay s depois da 1a venda. ponytail: um tipo so (vender N de uma linha), sem
        /// prazo; "comprar melhoria"/"atender VIP" se o playtest pedir. Flag Encomendas desligada: a aberta ainda entrega e paga; o relogio
        /// da proxima para e nenhuma nova abre.
        /// </summary>
        void Orders(float dt)
        {
            if (FirstSaleTime < 0f) return;
            if (OrderItem >= 0)
            {
                if (OrderProgress < OrderTarget) return;
                Gold += OrderReward;
                OrderGold += OrderReward;
                Emit(Ev.OrderDone, OrderItem, OrderReward, CounterFor((Item)OrderItem).Pos);   // joia: na loja de joias
                OrderItem = -1; OrderCount++; OrderIn = T.OrderGap;
                return;
            }
            if (!T.Encomendas || (OrderIn -= dt) > 0f) return;
            OrderIn = 0f;
            AbreEncomenda();
        }

        /// <summary>A encomenda `idx` do rodizio com a taxa de agora: item = as linhas abertas em rodizio; N = OrderFirstTarget na 1a, depois
        /// ~150 s da renda real dividida entre as linhas (no comeco o gargalo e' a producao, nao o cliente: pela demanda a 2a ja pedia 27
        /// espadas), entre 3 e 30; premio ~10 s da taxa online. E-PLAT-02: a natural seguinte e a do "Trocar" (mostrada antes) saem daqui.</summary>
        public (Item item, int target, int reward) OrderAt(int idx)
        {
            var lines = new List<Item>();
            for (Item p = Item.Sword; p <= Item.Jewel; p++) if (LineUnlocked(p)) lines.Add(p);
            Item it = lines[idx % lines.Count];
            int target = idx == 0 ? T.OrderFirstTarget
                : Math.Max(T.OrderMin, Math.Min(T.OrderMax, (int)Math.Round(T.OrderSeconds * RateEma / (lines.Count * PriceOf(it)))));
            return (it, target, Math.Max(T.OrderRewardMin, 5 * (int)Math.Round(RateEma * T.OrderRewardSeconds / 5.0)));
        }

        void AbreEncomenda()
        {
            (Item it, int target, int reward) = OrderAt(OrderIndex);
            OrderItem = (int)it; OrderTarget = target; OrderProgress = 0; OrderReward = reward;
            Emit(Ev.OrderNew, OrderItem, OrderTarget, CounterFor(it).Pos);
        }

        /// <summary>E-PLAT-02 "Trocar encomenda" (so o Anuncios.Pagar chama): a ativa sai sem pagar e entra a proxima do rodizio, do zero (a que
        /// o botao mostrou: Anuncios.ProximaEncomenda). Sem sorteio (P5).</summary>
        internal void TrocarEncomenda()
        {
            OrderSkips++;
            AbreEncomenda();
        }

        // ------------------------------------------------------------------ qualidade (B-CORE-03, docs/ECONOMIA_WAVE_B.md §1)

        /// <summary>Niveis que circulam numa pilha (Normal, Fina, Superior): tamanho dos lotes OutQ/HeldQ/StockQ. A Masterwork nunca entra num.</summary>
        public const int Graus = (int)Quality.Masterwork;
        /// <summary>Um lote (Fina/Superior por nivel) por item.</summary>
        public static int[][] Lotes(int items)
        {
            var q = new int[items][];
            for (int i = 0; i < items; i++) q[i] = new int[Graus];
            return q;
        }

        /// <summary>Fina e Superior de um lote nunca passam do `total` (Normal = o resto): corta de baixo para cima, a melhor fica. Vale depois
        /// de alguem mexer so no total (load com teto, -stock, teste).</summary>
        public static void Fit(int total, int[] q)
        {
            for (int l = Graus - 1; l > 0; l--) { q[l] = Clamp0(q[l], total); total -= q[l]; }
        }

        /// <summary>Tira 1 de `total` (pilha, mao ou balcao) e devolve o nivel dela: a melhor primeiro (Superior, Fina, Normal).</summary>
        public static Quality Take(ref int total, int[] q)
        {
            Fit(total, q);
            total--;
            for (int l = Graus - 1; l > 0; l--) if (q[l] > 0) { q[l]--; return (Quality)l; }
            return Quality.Normal;
        }

        /// <summary>Poe 1 peca de nivel `l` no lote.</summary>
        public static void Put(ref int total, int[] q, Quality l)
        {
            total++;
            if (l > Quality.Normal) q[(int)l]++;
        }

        /// <summary>Revelacao progressiva (GDD §19): qualidade, niveis de estacao e golpe de mestre so depois das Ferramentas (~11 min). Os 10
        /// primeiros minutos ficam intocados (ECONOMIA_WAVE_B §0.6).</summary>
        public bool QualityRevealed => Bought[(int)Upgrade.Tools];

        /// <summary>Pontos de qualidade (centesimos) da estacao, por fonte (QualitySource). A soma e' o q do acumulador (QualityPoints): o que
        /// esta aqui explica 100% das pecas. Fornalha, deposito e balcao: tudo 0 (lingote e' intermediario).</summary>
        public int[] QualityBreakdown(Station s)
        {
            var b = new int[(int)QualitySource.Buff + 1];   // uma por fonte (a ultima do enum)
            if (!s.Produces || !IsProduct(s.OutItem)) return b;
            b[(int)QualitySource.StationLevel] = (s.Level - 1) * T.NivelEstacaoQ;
            b[(int)QualitySource.Worker] = Equipe.QualidadeOficial(s);   // B-CORE-04: oficial da familia
            b[(int)QualitySource.Buff] = Equipe.QualidadeMestre(s);      // B-CORE-04: mestre de obras
            b[(int)QualitySource.Blueprint] = Projetos.Tecnica(s.Recipe.Family);   // B-CORE-07: tecnica da familia (posse do projeto)
            return b;
        }

        /// <summary>q da estacao: soma do QualityBreakdown.</summary>
        public int QualityPoints(Station s)
        {
            int q = 0;
            foreach (int p in QualityBreakdown(s)) q += p;
            return q;
        }

        /// <summary>Preco de `item` no nivel `l`: preco base x Tuning.QualidadePct, inteiro e meio para cima (espada 10/15/25, escudo 25/38/63).
        /// Masterwork = 0 (nao vende). B-CORE-08: familia completa no Hall, +5% (Hall.BonusPct) na mesma conta (sem bonus, o de antes ao byte).</summary>
        public int PriceOf(Item item, Quality l) => (int)(((long)PriceOf(item) * T.QualidadePct[(int)l] * (100 + Hall.BonusPct(item)) + 5000) / 10000);

        /// <summary>
        /// Nivel da peca que sai agora da estacao (§1.3): parte inteira de q = nivel garantido; a fracao enche o acumulador e, ao passar de 100,
        /// esta peca sobe um nivel. A automacao para em Superior. Golpe valendo: +1 nivel; numa Superior com Masterwork pendente, Masterwork.
        /// Sem sorteio: mesma sequencia de compras e golpes, mesma sequencia de niveis.
        /// </summary>
        Quality Grade(Station s)
        {
            if (!IsProduct(s.OutItem)) return Quality.Normal;
            int q = QualityPoints(s), l = q / 100;
            s.QAcc += q % 100;
            if (s.QAcc >= 100) { l++; s.QAcc -= 100; }
            l = Math.Min(l, (int)Quality.Superior);
            if (!s.Strike) return (Quality)l;
            s.Strike = false;
            if (l == (int)Quality.Superior && MasterworkPending != null && MasterworkPending(s)) return Quality.Masterwork;
            int up = Math.Min(l + 1, (int)Quality.Superior);
            StrikeGold += PriceOf(s.OutItem, (Quality)up) - PriceOf(s.OutItem, (Quality)l);
            return (Quality)up;
        }

        /// <summary>
        /// Bancada onde o ferreiro pode dar o golpe de mestre agora, ou null: qualidade revelada, recarga zerada e o ferreiro PARADO na boca de
        /// entrada de uma bancada aberta que tem a proxima peca (trabalhando ou com a receita inteira na entrada) e ainda sem golpe valendo. So o
        /// ferreiro: ajudante, offline e cofre nao passam por aqui.
        /// </summary>
        public Station StrikeStation
        {
            get
            {
                if (!QualityRevealed || StrikeCooldown > 0f || Player.Moving) return null;
                Station s = StationAt(Player.Pos, out bool outZone);
                if (s == null || outZone || s.Kind != Kind.Crafter || !s.Produces || !IsProduct(s.OutItem) || s.Strike) return null;
                if (s.Busy) return s;
                for (int k = 0; k < s.Ins.Length; k++) if (s.Ins[k] < s.Recipe.Inputs[k].Qty) return null;
                return s;
            }
        }

        /// <summary>O anel do golpe (View): `hit` = o toque caiu na janela de Tuning.GolpeJanela. Acertou com StrikeStation valendo: a proxima peca
        /// daquela bancada sobe 1 nivel e a recarga comeca. Errou ou fora de hora: nada muda (errar nao pune, sem recarga) e devolve false.</summary>
        public bool Strike(bool hit)
        {
            Station s = StrikeStation;
            if (s == null || !hit) return false;
            s.Strike = true;
            StrikeCooldown = T.GolpeRecarga;
            Strikes++;
            LiveOps.OnGolpe((int)s.OutItem);   // D-CORE-03: master_strike das missoes de evento
            return true;
        }

        // ------------------------------------------------------------------ consultas para view, bot e dica

        public long CheapestVisibleCost()
        {
            long best = -1;
            foreach (Pad p in Pads)
            {
                int u = p.Current(this);
                if (u < 0) continue;
                long remaining = T.Cost(u) - p.Paid;
                if (best < 0 || remaining < best) best = remaining;
            }
            return best;
        }

        /// <summary>Pad visivel mais barato que o ouro atual ja paga inteiro, ou null.</summary>
        public Pad CheapestAffordablePad()
        {
            Pad best = null; long bestRemaining = long.MaxValue;
            foreach (Pad p in Pads)
            {
                int u = p.Current(this);
                if (u < 0) continue;
                long remaining = T.Cost(u) - p.Paid;
                if (remaining <= Gold && remaining < bestRemaining) { bestRemaining = remaining; best = p; }
            }
            return best;
        }

        /// <summary>
        /// Upgrade do menu que e' a compra mais barata que o ouro atual ja paga, ou -1 (nenhum pagavel, ou um pad visivel
        /// pagavel custa menos). Empate com pad: o menu, que nao precisa andar. Mesma regra da dica e do bot.
        /// </summary>
        public int CheapestAffordableMenu()
        {
            int best = -1;
            for (int u = 0; u < Upgrades.Count; u++)
                if (MenuAvailable(u) && T.Cost(u) <= Gold && (best < 0 || T.Cost(u) < T.Cost(best))) best = u;
            Pad pad = best >= 0 ? CheapestAffordablePad() : null;
            return pad != null && T.Cost(pad.Current(this)) - pad.Paid < T.Cost(best) ? -1 : best;
        }

        public int HintArg;

        public Hint CurrentHint()
        {
            int menu = CheapestAffordableMenu();
            if (menu >= 0) { HintArg = menu; return Hint.BuyMenu; }
            Pad pad = CheapestAffordablePad();
            if (pad != null) { HintArg = pad.Slot; return Hint.BuyPad; }
            foreach (Chest c in Chests) if (c.State == 1) { HintArg = c.Index; return Hint.OpenChest; }   // o 1o disponivel, na ordem dos marcos
            // FASE7: pilha mista. Produto na mao primeiro; depois produto pronto que ainda cabe na mao (vale com lingote na mao: e' o
            // que destrava a bancada com a saida cheia); so entao o insumo na mao
            // FASE7 §3: produto que o balcao nao aceita (estoque cheio) nao segura a dica no balcao; produto que a fila pede vem antes
            Item deliver = Deliverable(Player);
            if (deliver != Item.Ore) { HintArg = (int)deliver; return Hint.ProductToCounter; }
            for (int pass = 0; pass < 2; pass++)
                foreach (Station s in Stations)
                    if (s.Unlocked && s.Kind == Kind.Crafter && s.Out > 0 && CanPick(Player, s.OutItem) && (pass == 1 || Wanted(s.OutItem))) { HintArg = (int)s.OutItem; return Hint.PickProducts; }
            Item raw = Player.Has(Item.Ingot) ? Item.Ingot : Item.Ore;   // insumo mais adiantado na mao
            if (Player.Has(raw)) { HintArg = (int)raw; return raw == Item.Ingot ? Hint.IngotToCrafter : Hint.OreToFurnace; }
            foreach (Station s in Stations) if (s.Unlocked && s.Kind == Kind.Furnace && s.Out > 0) { HintArg = (int)Item.Ingot; return Hint.PickIngots; }
            if (Queue.Count > 0 && Stock[(int)Queue[0].Want] == 0) { HintArg = (int)Queue[0].Want; return Hint.ClientWaiting; }
            if (JewelQueue.Count > 0 && Stock[(int)Item.Jewel] == 0) { HintArg = (int)Item.Jewel; return Hint.ClientWaiting; }
            HintArg = (int)Item.Ore;
            return Hint.GrabOre;
        }

        // ------------------------------------------------------------------ offline

        /// <summary>
        /// Cofre offline (raia B §5 + decisao do coordenador 2026-10-06): 25% da taxa online medida (RateEma), teto de 2 h,
        /// e NUNCA mais que 2x o preco do upgrade mais barato ainda travado (tudo comprado: 2x o ultimo), para o cofre
        /// nao comprar o resto do jogo sozinho. Delta negativo da 0 e o claim e' idempotente por id (SavedAt do save):
        /// id igual ou mais antigo que o ultimo nunca paga de novo.
        /// </summary>
        public long ApplyOffline(long elapsedSeconds, long claimId)
        {
            if (elapsedSeconds <= 0 || claimId <= LastClaim) return 0;
            LastClaim = claimId;
            long capped = Math.Min(elapsedSeconds, T.OfflineCap);   // A-CORE-03: o Tuning so encurta; 2 h e' trava (Balance.OfflineCapSeconds)
            long gold = (long)Math.Min(Math.Floor(RateEma * T.OfflineFactor * capped), OfflineMaxGold());
            LastClaimGold = Math.Max(0, gold);   // E-PLAT-02: o "Cofre 2x" dobra este resgate (1x por claimId)
            if (gold <= 0) return 0;
            Gold += gold;
            OfflineEarned += gold;
            Emit(Ev.Offline, (int)Math.Min(int.MaxValue, gold), (int)capped, Counter.Pos);
            return gold;
        }

        /// <summary>Menor produtivo nao comprado e a venda (pre-requisito comprado); producao completa = o produtivo mais caro (hoje a Vitrine de joias, 9.000:
        /// e' o "ultimo" de antes, mas nao muda quando um produtivo mais barato e' anexado ao enum). Luxo e Meta nao alteram o cofre.</summary>
        public long CheapestLockedCost()
        {
            long best = -1, top = 0;
            for (int i = 0; i < Upgrades.Count; i++)
            {
                if (!Upgrades.IsProduction(i)) continue;   // luxo e Meta (B-CORE-03) nao mexem no cofre
                long c = T.Cost(i); int req = Upgrades.All[i].Requires;
                top = Math.Max(top, c);
                // FASE7: so o que esta A VENDA (pre-requisito comprado). Sem isso o Balcao 5 (150, exige a Vitrine) baixava o cofre de
                // 1.060 para 300 aos 10 min; e o Joalheiro 2 (2.400, exige o Mineiro) segurava o teto do par em 4.800 (agora 2x Mineiro)
                if (!Bought[i] && (req < 0 || Bought[req]) && (best < 0 || c < best)) best = c;
            }
            return best >= 0 ? best : top;
        }

        /// <summary>Teto relativo do cofre: 2x o proximo upgrade travado.</summary>
        public long OfflineMaxGold() => (long)Math.Round(T.OfflineMaxNextUpgrades * CheapestLockedCost());

        // ------------------------------------------------------------------ save/load (chave=valor; lixo do payload vira estado inicial;
        // soma, schema e versao do jogo ficam no SaveEnvelope)

        /// <summary>A-CORE-01: versao do payload (linha v=). Subir = anexar o passo em Migrations (Migracao_TodaVersaoAnteriorTemPasso cobra).
        /// Campo novo que nao muda o significado de save antigo (hold=, vip=, ord=) nao sobe. 2 = A-CORE-04 (upgrades e parcial dos pads por ID):
        /// upgrade novo, em qualquer lugar do enum, nao sobe mais o schema. 3 = B-CORE-01 (estoque, vendas e maos por ID de item). B-CORE-03 so
        /// anexou chaves (stocksq=, holdsq=, wksq=, qstN=, golpe=): sem elas o save e' o de sempre, tudo Normal.</summary>
        public const int Schema = 3;
        /// <summary>Migracoes sobre o texto bruto, antes do parse: Migrations[i] leva o schema i+1 ao i+2. Schema 1 = 0.1-0.7; 2 = 0.8.</summary>
        public static readonly Func<string, string>[] Migrations = { V1ParaV2, V2ParaV3 };

        /// <summary>B-CORE-01: itens na ordem do enum Item ate o schema 2, CONGELADA: e' como o stock=/sold=/hold=/wk= por posicao se le (e o
        /// espelho que o save ainda grava para a 0.5). Nunca mude nem reordene; o ContentValidator.Itens cobra que sao as 6 primeiras da tabela.</summary>
        public static readonly string[] ItemIdsV2 = { "minerio", "lingote", "espada", "escudo", "ferramenta", "joia" };

        /// <summary>A-CORE-04: ordem do enum na 0.1-0.7 (schema 1), CONGELADA: e' como o up= daquelas versoes se le. Nunca mude nem reordene; o
        /// ContentValidator.Ids cobra que todos continuam na tabela.</summary>
        public static readonly string[] IdsV1 =
        {
            "fole", "bigorna2", "ajudante1", "escudos", "mochila", "ajudante2", "esteira", "fole_duplo", "botas", "ferramentas", "vitrine", "fornalha2",
            "ajudante3", "ajudantes_ageis", "martelo_veloz", "corredor", "joalheria", "joalheiro1", "lupa", "vitrine_joias", "fachada_nobre",
            "piso_oficina", "joalheria_real", "mineiro", "joalheiro2", "balcao5", "balcao6", "balcao7", "balcao8",
        };
        /// <summary>Pads do schema 1 por slot (cadeia de IDs, CONGELADA): o pads= da 0.1-0.7 e' o parcial por indice de slot.</summary>
        static readonly string[] PadsV1 =
        {
            "fole fole_duplo", "fornalha2", "bigorna2", "escudos", "ferramentas", "ajudante1 ajudante2 ajudante3", "ajudantes_ageis", "mochila", "botas",
            "esteira", "martelo_veloz", "vitrine", "corredor", "joalheria", "joalheiro1", "lupa", "vitrine_joias", "fachada_nobre", "piso_oficina",
            "joalheria_real", "mineiro", "joalheiro2",
        };

        /// <summary>
        /// Schema 1 -> 2: up= (flags na ordem IdsV1), ut= (instante por indice) e pads= (parcial por slot) viram ups=id:instante e upp=id:pago,
        /// pelas listas congeladas. O parcial do slot vai para o 1o upgrade nao comprado da cadeia (toda comprada: o ultimo; o Load devolve o
        /// ouro do pad que nao aparece). O resto do texto passa intacto; lixo segue adiante e o Load le com o default.
        /// </summary>
        static string V1ParaV2(string text)
        {
            string up = "", ut = "", pads = "";
            var rest = new StringBuilder();
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                string key = eq > 0 ? raw.Substring(0, eq).Trim() : "", val = eq > 0 ? raw.Substring(eq + 1).Trim() : "";
                if (key == "up") up = val; else if (key == "ut") ut = val; else if (key == "pads") pads = val; else rest.Append(raw).Append('\n');
            }
            string[] t = ut.Split(','), paid = pads.Split(',');
            var ups = new List<string>(); var bought = new HashSet<string>();
            for (int i = 0; i < Math.Min(up.Length, IdsV1.Length); i++)
                if (up[i] == '1') { bought.Add(IdsV1[i]); ups.Add(IdsV1[i] + ":" + (i < t.Length ? t[i].Trim() : "-1")); }
            var upp = new List<string>();
            for (int i = 0; i < Math.Min(paid.Length, PadsV1.Length); i++)
            {
                string[] chain = PadsV1[i].Split(' ');
                upp.Add((Array.Find(chain, c => !bought.Contains(c)) ?? chain[chain.Length - 1]) + ":" + paid[i].Trim());
            }
            return rest.Append("ups=").Append(string.Join(",", ups)).Append("\nupp=").Append(string.Join(",", upp)).Append('\n').ToString();
        }

        /// <summary>
        /// Schema 2 -> 3 (B-CORE-01): por posicao vira id:n pela lista congelada ItemIdsV2. stock=/sold= (a partir da espada) -> stocks=/solds=,
        /// hold= -> holds=, wk=papel:contagens;... -> wks=papel:id:n,...;... (o da 0.5.0, sem papel, vira ":id:n" e segue pela posicao do
        /// ajudante). O valor passa cru: o Load corta lixo, negativo e teto como antes. O resto do texto passa intacto.
        /// </summary>
        static string V2ParaV3(string text)
        {
            var sb = new StringBuilder();
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                string key = eq > 0 ? raw.Substring(0, eq).Trim() : "", val = eq > 0 ? raw.Substring(eq + 1).Trim() : "";
                if (key == "stock" || key == "sold") sb.Append(key).Append("s=").Append(IdTokens(val, 2));
                else if (key == "hold") sb.Append("holds=").Append(IdTokens(val, 0));
                else if (key == "wk")
                    sb.Append("wks=").Append(string.Join(";", Array.ConvertAll(val.Split(';'), w =>
                    {
                        int c = w.IndexOf(':');
                        return (c > 0 ? w.Substring(0, c) : "") + ":" + IdTokens(w.Substring(c + 1), 0);
                    })));
                else sb.Append(raw);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>"a,b,c" por posicao a partir do item `from` da lista congelada -> "id:a,id:b,id:c".</summary>
        static string IdTokens(string counts, int from)
        {
            string[] p = counts.Split(',');
            var t = new List<string>();
            for (int i = 0; i < p.Length && from + i < ItemIdsV2.Length; i++) t.Add(ItemIdsV2[from + i] + ":" + p[i].Trim());
            return string.Join(",", t);
        }

        /// <summary>A-CORE-04: IDs do save que este build nao conhece (conteudo de um build mais novo, depois de um downgrade): ficam de fora no
        /// Load (o parcial de pad volta ao ouro) e o Salvamento loga save_ids_desconhecidos. Nao vai no save: regravar os descarta.</summary>
        public int UnknownIds;

        /// <summary>Espelho por posicao para o downgrade (hold=/wk=): so as posicoes da lista congelada ItemIdsV2.</summary>
        static string EspelhoV2(int[] a) => string.Join(",", Array.ConvertAll(a, x => x.ToString(CultureInfo.InvariantCulture)), 0, Math.Min(a.Length, ItemIdsV2.Length));

        /// <summary>B-CORE-01: contagem por handle -> "id:n,..." (so o que nao e' zero).</summary>
        string IdCounts(int[] a)
        {
            var t = new List<string>();
            for (int i = 0; i < a.Length; i++) if (a[i] != 0) t.Add(ItemDefs[i].Id + ":" + a[i].ToString(CultureInfo.InvariantCulture));
            return string.Join(",", t);
        }

        /// <summary>B-CORE-01: "id:n,..." -> into[handle] = n (negativo e lixo = 0); ID que o conteudo nao tem conta em UnknownIds.</summary>
        static void ReadIdCounts(Sim sim, string[] tokens, int[] into)
        {
            foreach (string tok in tokens)
            {
                string[] kv = tok.Split(':');
                int i = Items.IndexOf(kv[0].Trim(), sim.ItemDefs);
                if (i >= 0) into[i] = Math.Max(0, kv.Length > 1 ? I(kv[1], 0) : 0);
                else if (kv[0].Trim().Length > 0) sim.UnknownIds++;
            }
        }

        /// <summary>B-CORE-03: lotes -> "id:fina:superior,..." (so os itens com Fina ou Superior), ja cortados ao total (Fit).</summary>
        string IdQ(int[] totals, int[][] q)
        {
            var t = new List<string>();
            for (int i = 0; i < q.Length; i++)
            {
                Fit(totals[i], q[i]);
                bool any = false;
                var tok = new StringBuilder(ItemDefs[i].Id);
                for (int l = 1; l < Graus; l++) { tok.Append(':').Append(q[i][l]); any |= q[i][l] > 0; }
                if (any) t.Add(tok.ToString());
            }
            return string.Join(",", t);
        }

        /// <summary>B-CORE-03: "id:fina:superior,..." -> into[handle][nivel] (negativo e lixo = 0; ID desconhecido fica de fora: o total dele ja
        /// contou no UnknownIds). O Load corta ao total depois (Fit).</summary>
        static void ReadIdQ(Sim sim, string[] tokens, int[][] into)
        {
            foreach (string tok in tokens)
            {
                string[] kv = tok.Split(':');
                int i = Items.IndexOf(kv[0].Trim(), sim.ItemDefs);
                if (i >= 0) for (int l = 1; l < Graus && l < kv.Length; l++) into[i][l] = Math.Max(0, I(kv[l], 0));
            }
        }

        public string Save(long nowUnix)
        {
            // A-CORE-02: SavedAt = maior instante ja visto. Relogio que voltou nao regrava o SavedAt para tras: adiantar -> resgatar -> voltar
            // -> adiantar nao gera claim novo, e voltar -> fechar -> acertar a hora nao paga tempo que nao passou
            long back = SavedAt - nowUnix;
            ClockWentBack = back > 0 && !_clockBehind ? back : 0;
            _clockBehind = back > 0;
            SavedAt = Math.Max(SavedAt, nowUnix);
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("v=").Append(Schema).Append('\n');
            sb.Append("t=").Append(Time.ToString("0.###", ci)).Append('\n');
            sb.Append("gold=").Append(Gold).Append('\n');
            // A-CORE-04: comprados por ID estavel (id:instante da compra em s de jogo; -1 = desconhecido), na ordem da tabela; o Load nao depende dela
            var ups = new List<string>();
            foreach (UpgradeDef d in Upgrades.All) if (Bought[(int)d.Up]) ups.Add(d.Id + ":" + UpgradeTime[(int)d.Up].ToString("0.##", ci));
            sb.Append("ups=").Append(string.Join(",", ups)).Append('\n');
            // espelho para a 0.1-0.6 sem envelope (downgrade le o mesmo texto: Salvamento.Gravar): flags na ordem CONGELADA IdsV1, nunca na do
            // enum. O Load do schema 2 ignora; parcial de pad e instantes nao descem
            var up = new StringBuilder();
            foreach (string id in IdsV1) { int u = Upgrades.IndexOf(id); up.Append(u >= 0 && Bought[u] ? '1' : '0'); }
            sb.Append("up=").Append(up).Append('\n');
            // parcial de pad pelo ID do upgrade que ele esta pagando: o 1o nao comprado da cadeia (toda comprada: o ultimo; o Load devolve)
            var upp = new List<string>();
            foreach (Pad p in Pads)
            {
                if (p.Paid <= 0) continue;
                int k = Array.FindIndex(p.Chain, c => !Bought[(int)c]);
                upp.Add(Upgrades.All[(int)p.Chain[k < 0 ? p.Chain.Length - 1 : k]].Id + ":" + p.Paid);
            }
            sb.Append("upp=").Append(string.Join(",", upp)).Append('\n');
            foreach (Station s in Stations)
            {
                if (!s.Produces) continue;
                sb.Append("st").Append(s.Index).Append('=').Append(s.In).Append(',').Append(s.Out).Append(',').Append(s.Busy ? 1 : 0).Append(',').Append(s.Progress.ToString("0.###", ci)).Append(',').Append(s.Crafted);
                for (int k = 1; k < s.Ins.Length; k++) sb.Append(',').Append(s.Ins[k]);   // B-CORE-01: do 2o insumo em diante (receita de 1 insumo: a linha de sempre)
                sb.Append('\n');
            }
            // B-CORE-01 (schema 3): estoque, vendas e maos por ID de item (id:n, so o que nao e' zero). stock=, sold=, hold= e wk= ficam como
            // espelho por posicao do enum para o downgrade a 0.5 (como o up=); o Load do schema 3 nao os le
            sb.Append("stocks=").Append(IdCounts(Stock)).Append('\n');
            sb.Append("solds=").Append(IdCounts(SoldItems)).Append('\n');
            sb.Append("holds=").Append(IdCounts(Player.Held)).Append('\n');
            if (Workers.Count > 0) sb.Append("wks=").Append(string.Join(";", Workers.ConvertAll(w => w.Role + ":" + IdCounts(w.Held)))).Append('\n');
            // B-CORE-03: qualidade. Lote por ID de item "id:fina:superior" (so quem tem Fina ou Superior; Normal = o resto do stocks=/holds=/wks=,
            // que seguem sendo o TOTAL: a 0.8 e a 0.5 leem tudo Normal), um lote por ajudante na ordem do wks=, acumulador/golpe/pilha de cada
            // bancada por indice como o stN= (qstN=acc,golpe,fina,superior) e a recarga do golpe. Save sem estas chaves = tudo Normal, acumulador 0
            sb.Append("stocksq=").Append(IdQ(Stock, StockQ)).Append('\n');
            sb.Append("holdsq=").Append(IdQ(Player.Held, Player.HeldQ)).Append('\n');
            if (Workers.Count > 0) sb.Append("wksq=").Append(string.Join(";", Workers.ConvertAll(w => IdQ(w.Held, w.HeldQ)))).Append('\n');
            foreach (Station s in Stations)
            {
                if (!s.Produces || !IsProduct(s.OutItem)) continue;
                Fit(s.Out, s.OutQ);
                sb.Append("qst").Append(s.Index).Append('=').Append(s.QAcc).Append(',').Append(s.Strike ? 1 : 0);
                for (int l = 1; l < Graus; l++) sb.Append(',').Append(s.OutQ[l]);
                sb.Append('\n');
            }
            sb.Append("golpe=").Append(StrikeCooldown.ToString("0.###", ci)).Append('\n');
            sb.Append("stock=").Append(Stock[2]).Append(',').Append(Stock[3]).Append(',').Append(Stock[4]).Append(',').Append(Stock[5]).Append('\n');
            sb.Append("sold=").Append(SoldItems[2]).Append(',').Append(SoldItems[3]).Append(',').Append(SoldItems[4]).Append(',').Append(SoldItems[5]).Append('\n');
            var ms = new StringBuilder();
            foreach (Chest c in Chests) { if (ms.Length > 0) ms.Append(','); ms.Append(c.State); }
            sb.Append("ms=").Append(ms).Append('\n');
            sb.Append("ema=").Append(RateEma.ToString("0.#####", ci)).Append('\n');
            sb.Append("saved=").Append(SavedAt).Append('\n');
            sb.Append("claim=").Append(LastClaim).Append('\n');
            sb.Append("m=").Append(FirstSaleTime.ToString("0.##", ci)).Append(',').Append(Sales).Append(',').Append(GoldEarned).Append(',').Append(OfflineEarned).Append(',')
              .Append(ClientsLost).Append(',').Append(ClientsTurnedAway).Append(',').Append(MaxQueue).Append(',').Append(WalkNoDecision.ToString("0.##", ci)).Append(',').Append(UpgradesBought).Append('\n');
            sb.Append("px=").Append(Player.Pos.ToString()).Append('\n');
            // FASE7: o que esta na mao volta ao reabrir. Espelho por posicao para a 0.5 (jogador: quantos de cada item; ajudantes "papel:contagens"
            // separados por ';'); o schema 3 le holds=/wks= (B-CORE-01)
            // B-CORE-07: so as posicoes da lista congelada (ItemIdsV2): item anexado depois (espada longa) nao entra no espelho
            sb.Append("hold=").Append(EspelhoV2(Player.Held)).Append('\n');
            if (Workers.Count > 0) sb.Append("wk=").Append(string.Join(";", Workers.ConvertAll(w => w.Role + ":" + EspelhoV2(w.Held)))).Append('\n');
            // FASE8: relogio do VIP, VIPs sorteados e o VIP pendente (produto, unidades que faltam). A fila nao vai no save: o VIP que ja estava
            // na vaga volta como pendente e pega a 1a vaga ao reabrir, com paciencia cheia
            Client vip = Queue.Find(c => c.Vip);
            int vipWant = vip != null ? (int)vip.Want : _vipWant, vipPack = vip != null ? vip.Pack : _vipWant >= 0 ? _vipPack : 0;
            sb.Append("vip=").Append(VipIn.ToString("0.###", ci)).Append(',').Append(VipCount).Append(',').Append(vipWant).Append(',').Append(vipPack)
              .Append(',').Append(vip != null ? 1 : 0).Append(',').Append(vip != null ? vip.Paid : 0)   // na vaga?, ja pagou
              .Append(',').Append((vip != null ? vip.Summoned : _vipWant >= 0 && _vipSummoned) ? 1 : 0).Append('\n');   // P14: chamado? (a recarga comeca quando ele sair)
            sb.Append("vipc=").Append(VipCooldown.ToString("0.###", ci)).Append('\n');   // P14: recarga do Chamar VIP
            // FASE9: encomenda (item, alvo, progresso, premio, entregues, relogio ate a proxima)
            sb.Append("ord=").Append(OrderItem).Append(',').Append(OrderTarget).Append(',').Append(OrderProgress).Append(',').Append(OrderReward)
              .Append(',').Append(OrderCount).Append(',').Append(OrderIn.ToString("0.###", ci)).Append(',').Append(OrderSkips).Append('\n');   // E-PLAT-02: puladas no fim
            // A-CORE-02: velocidade (multiplicador, s restantes, s de recarga), no tempo de jogo
            sb.Append("bst=").Append(BoostMul.ToString("0", ci)).Append(',').Append(BoostLeft.ToString("0.###", ci)).Append(',')
              .Append(BoostCooldown.ToString("0.###", ci)).Append('\n');
            Anuncios.Save(sb);   // E-PLAT-02: linhas ad.* (so o que nao e' o padrao)
            Renome.Save(sb);  // B-CORE-06: rn= (o Load de antes ignora chave desconhecida)
            Equipe.Save(sb);  // B-CORE-04: wkn= (so com alguem de nivel; senao o texto e' o de antes)
            Projetos.Save(sb);   // B-CORE-07: bp= e bpq= (idem)
            Hall.Save(sb); Colecao.Save(sb);   // B-CORE-08: hall= e colq= (so com algo dentro)
            Metas.Save(sb);   // D-CORE-01/02: linhas mt.* (nada antes da 1a Agora)
            LiveOps.Save(sb); // D-CORE-03/04: linhas ev.*/ps.* (nada antes da 1a Agora)
            return sb.ToString();
        }

        /// <summary>Carrega o payload sobre `tuning` (null = padrao): os tetos do load (boost, VIP, encomenda) sao os do Tuning da partida.
        /// `table` = a tabela que resolve os IDs (A-CORE-04; null = Upgrades.All) e `items`/`recipes` = o conteudo (B-CORE-01; null = o padrao):
        /// so o teste injeta outro (B-CORE-07: `blueprints` idem).</summary>
        public static Sim Load(string text, Tuning tuning = null, UpgradeDef[] table = null, ItemDef[] items = null, RecipeDef[] recipes = null,
            BlueprintDef[] blueprints = null)
        {
            var sim = new Sim(tuning, items, recipes, blueprints);
            Tuning T = sim.T;   // os tetos abaixo
            if (string.IsNullOrEmpty(text)) return sim;
            // schema: sem v= (lixo, testes) = 1; maior que Schema (downgrade) = melhor esforco, como a 0.6 faz com o save da 0.7
            int v = 1;
            foreach (string raw in text.Split('\n')) if (raw.StartsWith("v=", StringComparison.Ordinal)) { v = Math.Max(1, I(raw.Substring(2), 1)); break; }
            for (; v < Schema; v++) text = Migrations[v - 1](text);
            sim.Equipe.Load(text);   // B-CORE-04: antes das pilhas (stN=): o teto da saida depende do almoxarife
            sim.Recompute();
            var ci = CultureInfo.InvariantCulture;
            string wk = "", wkq = "";   // carga dos ajudantes (e a qualidade dela): aplicada depois do Recompute (eles nascem dos flags)
            int vipWant = -1, vipPack = 0, vipPaid = 0; bool vipQuiet = false, vipSummoned = false;
            int ordItem = -1, ordTarget = 0, ordProgress = 0, ordReward = 0;   // VIP pendente: so depois do Recompute (a linha do produto tem de estar aberta)
            long refund = 0;   // parcial de pad sem pad (ID desconhecido ou upgrade so de menu): volta ao ouro depois do gold=
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw.Substring(0, eq).Trim(), val = raw.Substring(eq + 1).Trim();
                string[] parts = val.Split(',');
                switch (key)
                {
                    case "t": sim.Time = F(val, 0f); break;
                    case "gold": sim.Gold = Math.Max(0, L(val, 0)); break;
                    // A-CORE-04: por ID (up=/ut=/pads= do schema 1 ja viraram isto no V1ParaV2; o espelho up= do schema 2 fica sem leitor). ID
                    // desconhecido fica de fora e conta em UnknownIds; o parcial vai ao pad cuja cadeia tem o upgrade
                    case "ups":
                        foreach (string tok in parts)
                        {
                            string[] kv = tok.Split(':');
                            int u = Upgrades.IndexOf(kv[0].Trim(), table);
                            if (u >= 0) { sim.Bought[u] = true; sim.UpgradeTime[u] = kv.Length > 1 ? F(kv[1], -1f) : -1f; }
                            else if (kv[0].Trim().Length > 0) sim.UnknownIds++;
                        }
                        break;
                    case "upp":
                        foreach (string tok in parts)
                        {
                            string[] kv = tok.Split(':');
                            long paid = kv.Length > 1 ? Math.Max(0, L(kv[1], 0)) : 0;
                            if (paid <= 0) continue;
                            int u = Upgrades.IndexOf(kv[0].Trim(), table);
                            Pad pad = u >= 0 ? sim.Pads.Find(p => Array.IndexOf(p.Chain, (Upgrade)u) >= 0) : null;
                            if (pad != null) pad.Paid += paid; else refund += paid;
                            if (u < 0) sim.UnknownIds++;
                        }
                        break;
                    // B-CORE-01: por ID de item (stock=/sold=/hold=/wk= por posicao ja viraram isto no V2ParaV3; no schema 3 sao so espelho)
                    case "stocks": ReadIdCounts(sim, parts, sim.Stock); break;       // save antigo com 3 produtos: joia fica 0
                    case "solds": ReadIdCounts(sim, parts, sim.SoldItems); break;    // save antigo sem sold=: conta do zero
                    case "ms": for (int i = 0; i < Math.Min(parts.Length, sim.Chests.Count); i++) sim.Chests[i].State = Clamp0(I(parts[i], 0), 2); break;      // save antigo sem ms=: nenhum marco
                    case "ema": sim.RateEma = Math.Max(0.0, D(val, 0.0)); break;
                    case "saved": sim.SavedAt = L(val, 0); break;
                    case "claim": sim.LastClaim = L(val, 0); break;
                    case "m":
                        if (parts.Length >= 9)
                        {
                            sim.FirstSaleTime = F(parts[0], -1f); sim.Sales = I(parts[1], 0); sim.GoldEarned = L(parts[2], 0); sim.OfflineEarned = L(parts[3], 0);
                            sim.ClientsLost = I(parts[4], 0); sim.ClientsTurnedAway = I(parts[5], 0); sim.MaxQueue = I(parts[6], 0); sim.WalkNoDecision = F(parts[7], 0f); sim.UpgradesBought = I(parts[8], 0);
                        }
                        break;
                    case "px": if (parts.Length == 2) sim.Player.Pos = new V2(F(parts[0], 4.5f), F(parts[1], 3.5f)); break;   // clamp so' depois do Recompute (MaxX depende do Corredor)
                    case "holds": ReadIdCounts(sim, parts, sim.Player.Held); break;   // save antigo sem hold=: maos vazias
                    case "wks": wk = val; break;
                    // B-CORE-03; save antigo sem estas chaves: tudo Normal, acumuladores 0, sem golpe valendo nem recarga
                    case "stocksq": ReadIdQ(sim, parts, sim.StockQ); break;
                    case "holdsq": ReadIdQ(sim, parts, sim.Player.HeldQ); break;
                    case "wksq": wkq = val; break;
                    case "golpe": sim.StrikeCooldown = Math.Max(0f, Math.Min(T.GolpeRecarga, F(val, 0f))); break;
                    case "ord":   // FASE9; save antigo sem ord=: do zero. A encomenda so volta depois do Recompute (linha aberta)
                        if (parts.Length >= 6)
                        {
                            ordItem = I(parts[0], -1); ordTarget = Clamp0(I(parts[1], 0), T.OrderMax);
                            ordProgress = Clamp0(I(parts[2], 0), ordTarget); ordReward = Math.Max(0, I(parts[3], 0));
                            sim.OrderCount = Math.Max(0, I(parts[4], 0));
                            sim.OrderIn = Math.Max(0f, Math.Min(T.OrderFirstDelay, F(parts[5], T.OrderFirstDelay)));
                            if (parts.Length >= 7) sim.OrderSkips = Math.Max(0, I(parts[6], 0));   // E-PLAT-02; save de antes: nenhuma pulada
                        }
                        break;
                    case "vip":   // FASE8; save antigo sem vip=: relogio novo (VipInterval(0)) e nenhum VIP pendente
                        if (parts.Length >= 4)
                        {
                            sim.VipIn = Math.Max(0f, Math.Min(T.VipMax, F(parts[0], sim.VipIn)));
                            sim.VipCount = Math.Max(0, I(parts[1], 0));
                            vipWant = I(parts[2], -1); vipPack = Clamp0(I(parts[3], 0), T.VipSummonPackMax);
                            if (parts.Length >= 6) { vipQuiet = I(parts[4], 0) == 1; vipPaid = Math.Max(0, I(parts[5], 0)); }   // save da 0.5.0: 4 campos
                            if (parts.Length >= 7) vipSummoned = I(parts[6], 0) == 1;   // P14; save de antes: natural (nao recarrega ao sair)
                        }
                        break;
                    case "vipc": sim.VipCooldown = Math.Max(0f, Math.Min(T.Ad(Anuncio.Vip).Recarga, F(val, 0f))); break;   // P14; save antigo sem vipc=: sem recarga
                    case "bst":   // A-CORE-02; save antigo sem bst=: sem boost e sem recarga (como antes). Sem tempo = sem boost; ativo = 2x ou 3x
                        if (parts.Length >= 3)
                        {
                            sim.BoostLeft = Math.Max(0f, Math.Min(T.Ad(Anuncio.Velocidade).Valor, F(parts[1], 0f)));
                            sim.BoostMul = sim.BoostLeft > 0f ? Math.Max(2f, Math.Min(T.BoostMax, (float)Math.Round(F(parts[0], 2f)))) : 1f;
                            sim.BoostCooldown = Math.Max(0f, Math.Min(T.Ad(Anuncio.Velocidade).Recarga, F(parts[2], 0f)));
                        }
                        break;
                    default:
                        if (key.StartsWith("st") && parts.Length >= 4)
                        {
                            int idx = I(key.Substring(2), -1);
                            if (idx < 0 || idx >= sim.Stations.Count || !sim.Stations[idx].Produces) break;
                            Station s = sim.Stations[idx];
                            s.In = Clamp0(I(parts[0], 0), s.InCap); s.Out = Clamp0(I(parts[1], 0), s.OutCap);
                            s.Busy = I(parts[2], 0) == 1; s.Progress = Math.Max(0f, Math.Min(1f, F(parts[3], 0f)));
                            if (parts.Length >= 5) s.Crafted = Math.Max(0, I(parts[4], 0));
                            for (int k = 1; k < s.Ins.Length && 4 + k < parts.Length; k++) s.Ins[k] = Clamp0(I(parts[4 + k], 0), s.InCap);   // B-CORE-01: 2o insumo em diante
                        }
                        else if (key.StartsWith("qst") && parts.Length >= 2)   // B-CORE-03: acumulador, golpe valendo e Fina/Superior da pilha
                        {
                            int idx = I(key.Substring(3), -1);
                            if (idx < 0 || idx >= sim.Stations.Count || !sim.Stations[idx].Produces) break;
                            Station s = sim.Stations[idx];
                            s.QAcc = Clamp0(I(parts[0], 0), 99); s.Strike = I(parts[1], 0) == 1;
                            for (int l = 1; l < Graus && l + 1 < parts.Length; l++) s.OutQ[l] = Math.Max(0, I(parts[l + 1], 0));
                        }
                        break;
                }
            }
            sim.Projetos.Load(text);   // B-CORE-07: antes do pad (o parcial da bigorna longa so fica com o projeto); sem bp= o 1o Tick da o retroativo dos marcos
            sim.Colecao.Load(text);    // B-CORE-08: colq= (Superiores por item; Normal e Fina saem do solds= e do bpq=)
            sim.Gold += refund;
            sim.UpgradesBought = 0;
            foreach (bool b in sim.Bought) if (b) sim.UpgradesBought++;
            sim.Recompute();
            for (int i = 0; i < sim.Stock.Length; i++)   // teto so' e' conhecido depois dos upgrades; insumo nao fica no balcao nem conta venda
                if (sim.IsProduct((Item)i)) sim.Stock[i] = Math.Min(sim.Stock[i], sim.CounterCap); else sim.Stock[i] = sim.SoldItems[i] = 0;
            for (int i = 0; i < sim.Player.Held.Length; i++) sim.Player.Held[i] = Math.Min(sim.Player.Held[i], sim.Player.Cap);   // teto por tipo (Mochila)
            for (int i = 0; i < sim.Stock.Length; i++) { Fit(sim.Stock[i], sim.StockQ[i]); Fit(sim.Player.Held[i], sim.Player.HeldQ[i]); }   // B-CORE-03: lote cabe no total
            foreach (Station s in sim.Stations) Fit(s.Out, s.OutQ);
            string[] ws = wk.Split(';'), wq = wkq.Split(';');
            for (int k = 0; k < ws.Length; k++)   // ajudante: so o 1o tipo valido para o papel (na ordem dos itens), ate o teto; lixo = mao vazia
            {
                // a lista e' recriada na ordem do HireBy, nao na da compra: vai para o proximo ajudante vazio do mesmo papel.
                // Save da 0.5.0 (sem papel; o V2ParaV3 o deixa ":id:n") segue a posicao na lista
                int colon = ws[k].IndexOf(':'), role = colon > 0 ? I(ws[k].Substring(0, colon), -9) : -9;
                Carrier w = colon > 0 ? sim.Workers.Find(c => c.Role == role && c.Count == 0) : k < sim.Workers.Count ? sim.Workers[k] : null;
                if (w == null) continue;
                var h = new int[w.Held.Length];
                ReadIdCounts(sim, ws[k].Substring(colon + 1).Split(','), h);
                for (int i = 0; i < h.Length; i++)
                {
                    int n = Clamp0(h[i], w.Cap);
                    if (n > 0 && w.Count == 0 && sim.CanPick(w, (Item)i)) w.Held[i] = n;
                }
                if (k < wq.Length) ReadIdQ(sim, wq[k].Split(','), w.HeldQ);   // B-CORE-03: o lote do mesmo ajudante (ordem do wks=)
                for (int i = 0; i < h.Length; i++) Fit(w.Held[i], w.HeldQ[i]);
            }
            if (vipPack > 0 && vipWant >= (int)Item.Sword && vipWant <= (int)Item.Tool && sim.LineUnlocked((Item)vipWant)) { sim._vipWant = vipWant; sim._vipPack = vipPack; sim._vipPaid = vipPaid; sim._vipQuiet = vipQuiet; sim._vipSummoned = vipSummoned; }
            if (ordItem >= (int)Item.Sword && ordItem <= (int)Item.Jewel && ordTarget > 0 && sim.LineUnlocked((Item)ordItem))
            { sim.OrderItem = ordItem; sim.OrderTarget = ordTarget; sim.OrderProgress = ordProgress; sim.OrderReward = ordReward; }
            sim.Player.Pos = sim.Collide(Clamp(sim.Player.Pos, sim.MaxX));   // save antigo com o jogador dentro de um corpo: sai pela borda
            // pad escondido devolve o parcial: o save da v0.3 com produtivos novos (Mineiro, Joalheiro 2) esconde o luxo ate completar
            // de novo, e o pad cujo upgrade foi para o menu (FASE6) nunca mais aparece
            foreach (Pad p in sim.Pads) if (p.Current(sim) < 0) { sim.Gold += p.Paid; p.Paid = 0; }
            sim.Metas.Load(text);   // D-CORE-01/02: save sem mt.* = metas zeradas
            sim.Anuncios.Load(text);   // E-PLAT-02: save sem ad.* = nenhum anuncio visto
            sim.Renome.Load(text);  // B-CORE-06: save sem rn= = retroativo (depois das metas: soma o Renome que elas guardaram)
            sim.Hall.Load(text);    // B-CORE-08: depois do Renome (o mw: do ledger sem hall= volta registrado)
            sim.LiveOps.Load(text); // D-CORE-03/04: save sem ev.*/ps.* = nenhum evento, Pass do zero (o XP das metas entra 1x na 1a temporada)
            return sim;
        }

        static int Clamp0(int v, int max) => Math.Max(0, Math.Min(max, v));
        static int I(string s, int d) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        static long L(string s, long d) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : d;
        static float F(string s, float d) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : d;
        static double D(string s, double d) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && !double.IsNaN(v) && !double.IsInfinity(v) ? v : d;
    }
}
