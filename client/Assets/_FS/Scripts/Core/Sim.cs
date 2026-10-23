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

    public sealed partial class Station
    {
        public int Index; public Kind Kind; public V2 Pos; public string Name;
        /// <summary>C-CORE-01: ID do StationDef (o texto est.&lt;id&gt; sai dele) e a folha de arte (null = pelo tipo e produto, View).</summary>
        public string Id, Arte;
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
        /// <summary>C-CORE-05: posicao do insumo opcional (a liga, Qty 0) na receita; -1 = nenhum. O Ins dele e' o slot de liga.
        /// ponytail: o slot usa o InCap da entrada (4-8), nao a capacidade 2 do CONTEUDO_V1 §2.2; so muda o estoque transitorio de liga.</summary>
        public int Liga = -1;
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
        /// <summary>C-CORE-06 (Sim.Clientes): qualidade minima da peca que ele leva e a familia dele (DistrictDef.Clientes; -1 = cliente de sempre).</summary>
        public Quality MinQ; public int Familia = -1;
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
                if (s.UpgradeDefs[(int)u].InMenu) return -1;   // FASE6: vendido no menu inferior; cadeia so de menu = pad invisivel para sempre
                if (s.UpgradeDefs[(int)u].Luxury && !s.ProductionComplete) return -1;
                if (!s.TemProjeto((int)u)) return -1;   // B-CORE-07: a bigorna longa so aparece com o projeto
                int req = s.UpgradeDefs[(int)u].Requires;
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
    public sealed partial class Sim
    {
        /// <summary>A-CORE-03: numeros de economia/ritmo e feature flags desta partida (RemoteConfig e eventos). Padrao = `new Tuning()`.</summary>
        public readonly Tuning T;
        /// <summary>D-CORE-01/02: metas (Metas.cs). Gancho unico: o Emit conta o evento; o save leva as linhas mt.* so depois da 1a Agora.</summary>
        public readonly Metas Metas;
        /// <summary>E-PLAT-02/03: rewarded por placement (ID de transacao, recarga, teto diario) e o veto do intersticial (Anuncios.cs).</summary>
        public readonly Anuncios Anuncios;
        /// <summary>B-CORE-06: Renome (Renome.cs), trilha que so sobe. Ganchos: o Emit credita o bau, o Sell a Superior (a encomenda: Encomendas.Entregar, pelo tipo); save na linha rn=.
        /// C-CORE-03: e' do Imperio: num distrito (Heroi) e' o da raiz, entao encomenda, Superior, obra-prima e marco do distrito sobem o mesmo Renome
        /// (e o nivel dele libera vagas e projetos no distrito); o payload do distrito nao leva rn=.</summary>
        public Renome Renome => Heroi != null ? Heroi.Renome : _renome;
        readonly Renome _renome;
        /// <summary>B-CORE-05: Orders 2.0 (Encomendas.cs): vagas, rodizio, premio. Ganchos: Tick, Sell (OnVenda), Save/Load.</summary>
        public readonly Encomendas Encomendas;
        /// <summary>D-CORE-04: Royal Contracts (Contratos.cs) do Imperio: os da raiz (Heroi) quando este Sim e' outro distrito, senao os
        /// proprios. Ganchos: Tick (o prazo anda no distrito ativo), Sell (pelo ID do item: a venda de qualquer distrito conta) e Save/Load
        /// (ctr=, ctr.v=, so no payload da raiz).</summary>
        public Contratos Contratos => Heroi != null ? Heroi.Contratos : _contratos;
        readonly Contratos _contratos;
        /// <summary>E-PLAT-04/05: compras in-app (Compras.cs): ledger por ID de transacao e posse re-derivada do recibo; save nas linhas iap.*.</summary>
        public readonly Compras Compras;
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
        /// <summary>C-CORE-01: o distrito desta instancia (padrao Distritos.OldForge) e a tabela de upgrades dele (Old Forge = Upgrades.All):
        /// Bought, UpgradeTime e o tier do custo sao indices AQUI.</summary>
        public readonly DistrictDef Distrito; public readonly UpgradeDef[] UpgradeDefs;
        /// <summary>C-CORE-02: o Sim do Old Forge quando este e' outro distrito do Imperio: Mochila e Botas sao do ferreiro e valem em todo
        /// distrito (CONTEUDO_V1 §15 #15; Sim.Has). null = so os upgrades deste distrito.</summary>
        public Sim Heroi;
        public float Time;
        /// <summary>A-CORE-06: ouro, custos e somas em long (o save e' texto: cabe sem mudar o formato). ponytail: soma sem saturar; 9,2e18 nao
        /// se alcanca jogando (1 milhao/s por 100 anos = 3e15), so editando o save.</summary>
        public long Gold;
        public readonly bool[] Bought;
        public readonly float[] UpgradeTime;
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
        // FASE9: encomendas entregues (vai no save, ord=); as vagas, o rodizio e o relogio estao no Encomendas (B-CORE-05)
        public int OrderCount;
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
        /// <summary>C-CORE-05 (o AlloyPieces do CONTEUDO_V1 §2.2): pecas feitas com liga, por item. Vai no save (liga=, so com alguma).</summary>
        public readonly int[] PecasComLiga;

        // metricas de playtest (GDD §14/§15)
        public float FirstSaleTime = -1f, WalkNoDecision;
        public int Sales, ClientsLost, ClientsTurnedAway, MaxQueue, UpgradesBought;
        public long GoldEarned, OfflineEarned;
        public bool ProductionComplete
        {
            get
            {
                for (int i = 0; i < UpgradeDefs.Length; i++) if (!Bought[i] && IsProduction(i)) return false;   // luxo e Meta nao contam
                return true;
            }
        }
        /// <summary>Produtivo = nem luxo nem Meta, na tabela deste distrito (Upgrades.IsProduction e' a do Old Forge).</summary>
        public bool IsProduction(int u) => !UpgradeDefs[u].Luxury && !UpgradeDefs[u].Meta;
        public readonly int[] Crafted;
        public double RateEma;                 // ouro/s online, media movel (base do offline)
        public long SavedAt, LastClaim;        // unix s; o claim offline e' idempotente por SavedAt, que nunca recua (Save)
        /// <summary>Ouro do ultimo resgate do cofre nesta sessao (0 = nenhum): o "Cofre 2x" dobra este (Anuncios). Nao vai no save.</summary>
        public long LastClaimGold;
        /// <summary>A-CORE-02: s que o relogio do aparelho estava atras do SavedAt, so no 1o Save de cada recuo (senao 0): a View loga clock_back
        /// uma vez, nao a cada autosave.</summary>
        public long ClockWentBack;
        bool _clockBehind;

        /// <summary>Estacoes com papel fixo no codigo, achadas pelo ID do StationDef (C-CORE-01): deposito, fornalha e balcao existem em todo
        /// distrito (ContentValidator.Layout); as outras sao do Old Forge e ficam null onde o distrito nao as tem.</summary>
        public readonly Station Deposit, FurnaceA, FurnaceB, AnvilA, AnvilB, ShieldBench, ToolBench, Counter, JewelBench, JewelShop, LongAnvil;
        /// <summary>C-CORE-01: as duas pontas da esteira (DistrictDef.EsteiraDe/Para; Old Forge: Fornalha -> Bigorna).</summary>
        public readonly Station EsteiraDe, EsteiraPara;
        /// <summary>Ponto de contratacao do Old Forge; o de cada distrito e' DistrictDef.Contratacao (Sim.Contratacao).</summary>
        public static readonly V2 HireSpot = new V2(8.3f, 1.5f);
        public V2 Contratacao => Distrito.Contratacao;
        // B-CORE-04: os ajudantes sao os WorkerDef Porter (upgrade que contrata e rota), na ordem da tabela = a de antes: Ajudante 1-3, Joalheiro,
        // Mineiro (2o papel 0), Joalheiro 2 (2o papel 3, FASE4_MINERIO). C-CORE-01: os do distrito (Equipe.Defs); _hireBy = indice do upgrade
        readonly int[] _hireDef, _hireBy, _hireRole;
        /// <summary>C-CORE-01: efeito generico (UpgradeDef.Efeito, indice = o Upgrade do Old Forge) -> indice do upgrade DESTE distrito que o
        /// faz, ou -1. No Old Forge, _ef[u] = u.</summary>
        readonly int[] _ef;
        /// <summary>Item que algum deposito do distrito da (papel 0 dos ajudantes): minerio no Old Forge, minerio e couro na Row.</summary>
        readonly bool[] _bruto;

        readonly float[] _clientT;
        float _serveT, _serveJ, _convT, _padDwell;
        Pad _padUnder;

        /// <summary>`items`/`recipes`/`blueprints`/`masterworks` = conteudo injetado (null = o do distrito); so o teste usa. `distrito` = o
        /// DistrictDef desta instancia (null = Distritos.OldForge); o Ajuste dele vale sobre `tuning` (o Imperio passa um Tuning novo por distrito).</summary>
        public Sim(Tuning tuning = null, ItemDef[] items = null, RecipeDef[] recipes = null, BlueprintDef[] blueprints = null, DistrictDef distrito = null,
            MasterworkDef[] masterworks = null)
        {
            Distrito = distrito ?? Distritos.OldForge;
            UpgradeDefs = Distrito.Upgrades;
            Bought = new bool[UpgradeDefs.Length]; UpgradeTime = new float[UpgradeDefs.Length];
            T = tuning ?? new Tuning();
            Distrito.Ajuste?.Invoke(T);
            Metas = new Metas(this);
            Anuncios = new Anuncios(this);
            _renome = new Renome(this);
            Encomendas = new Encomendas(this);
            _contratos = new Contratos(this);
            Compras = new Compras(this);
            Equipe = new Equipe(this);
            LiveOps = new LiveOps(this);
            ItemDefs = items ?? Distrito.Itens; RecipeDefs = recipes ?? Distrito.Receitas;
            int n = ItemDefs.Length;
            Projetos = new Projetos(this, blueprints ?? Distrito.Projetos);
            Hall = new Hall(this, masterworks ?? Distrito.Masterworks); Colecao = new Colecao(this, Distrito.MarcosColecao);   // B-CORE-08 por distrito
            MasterworkPending = s => Hall.Pendente(s) >= 0;   // B-CORE-08: projeto e condicoes da 1a pendente da familia
            Stock = new int[n]; SoldItems = new int[n]; Crafted = new int[n]; StockQ = Lotes(n); PecasComLiga = new int[n];
            Player = new Carrier(n) { Pos = Distrito.Inicio, Speed = Balance.PlayerSpeed, Cap = Balance.PlayerCap };
            VipIn = T.VipInterval(0);
            // 1o cliente: espada cedo (FirstClientAt); as outras linhas no intervalo delas (B-CORE-07: por item, a espada longa entrou)
            _clientT = new float[n];
            for (int i = (int)Item.Sword; i < n; i++) _clientT[i] = i == (int)Item.Sword ? T.FirstClientAt : HasClients((Item)i) ? T.ClientInterval[i] : 0f;
            for (int i = 0; i < UpgradeTime.Length; i++) UpgradeTime[i] = -1f;
            _ef = new int[Upgrades.Count];
            for (int e = 0; e < _ef.Length; e++) _ef[e] = Array.FindIndex(UpgradeDefs, d => (int)d.Efeito == e);
            // B-CORE-04: os ajudantes do upgrade, na ordem da tabela de workers do distrito
            var hire = new List<int>();
            for (int k = 0; k < Equipe.Defs.Length; k++) if (Equipe.Defs[k].Contrata != null && Equipe.Defs[k].Papel == WorkerRole.Porter) hire.Add(k);   // C-CORE-09: o Mensageiro (Courier) nao carrega
            _hireDef = hire.ToArray();
            _hireBy = Array.ConvertAll(_hireDef, k => Upgrades.IndexOf(Equipe.Defs[k].Contrata, UpgradeDefs));
            _hireRole = Array.ConvertAll(_hireDef, k => Equipe.Defs[k].Rota);
            // C-CORE-01: estacoes, pads e baus do DistrictDef (o Old Forge e' o layout que estava aqui, na mesma ordem: o indice e' o do save).
            // Bocas (FASE5 §2b): (inX, inY) = lado da ENTRADA; a SAIDA fica no lado oposto. A estacao faz a receita da familia dela (RecipeDefs)
            _bruto = new bool[n];
            foreach (StationDef d in Distrito.Estacoes)
            {
                Station s = Add(d.Kind, d.Pos.X, d.Pos.Y, d.Name, d.InX, d.InY, d.Abre == null ? -1 : Upgrades.IndexOf(d.Abre, UpgradeDefs), d.Familia);
                s.Id = d.Id; s.Arte = d.Arte;
                if (d.Kind == Kind.Deposit) { s.OutItem = d.Bruto; _bruto[(int)d.Bruto] = true; }
            }
            Deposit = Est("deposito"); FurnaceA = Est("fornalha"); FurnaceB = Est("fornalha2"); AnvilA = Est("bigorna"); AnvilB = Est("bigorna2");
            ShieldBench = Est("escudos"); ToolBench = Est("ferramentas"); Counter = Est("balcao"); JewelBench = Est("joalheria");
            JewelShop = Est("loja_joias"); LongAnvil = Est("bigorna_longa");
            EsteiraDe = Est(Distrito.EsteiraDe); EsteiraPara = Est(Distrito.EsteiraPara);
            // Pads fora das linhas de caminhada. FASE6: os pads de upgrades do menu seguem na lista, invisiveis (Pad.Current)
            foreach (PadDef p in Distrito.Pads)
                Pads.Add(new Pad { Slot = Pads.Count, Pos = p.Pos, Chain = Array.ConvertAll(p.Cadeia, id => (Upgrade)Upgrades.IndexOf(id, UpgradeDefs)) });
            foreach (MilestoneDef m in Distrito.Marcos) Chests.Add(new Chest { Index = Chests.Count, Pos = m.Pos, Gold = m.Gold, Label = m.Label });
            PreparaBancadas(); PreparaFamilias();   // C-CORE-06 (Sim.Bancada, Sim.Clientes): sem bancada configuravel nem familia, nada muda
            Recompute();
        }

        /// <summary>Estacao com este ID do StationDef, ou null.</summary>
        public Station Est(string id) => id == null ? null : Stations.Find(s => s.Id == id);

        /// <summary>C-CORE-01: o efeito generico `e` (o do upgrade do Old Forge) esta comprado neste distrito? Mochila e Botas sem upgrade
        /// proprio no distrito vem do ferreiro (Heroi).</summary>
        public bool Has(Upgrade e)
        {
            int i = _ef[(int)e];
            if (i >= 0) return Bought[i];
            return Heroi != null && (e == Upgrade.PlayerCapacity || e == Upgrade.PlayerSpeed) && Heroi.Has(e);
        }

        /// <summary>Algum upgrade deste distrito tem o efeito generico `e` (a View so monta o prop/decoracao de quem existe).</summary>
        public bool TemEfeito(Upgrade e) => _ef[(int)e] >= 0;

        /// <summary>Produto que vai a loja de joias (so onde ela existe).</summary>
        bool Joia(Item p) => p == Item.Jewel && JewelShop != null;

        /// <summary>Item que sai de deposito (o papel 0 dos ajudantes carrega).</summary>
        public bool Bruto(Item i) => (uint)i < (uint)_bruto.Length && _bruto[(int)i];
        /// <summary>Pilha de saida sem almoxarife: a do tipo (fornalha 4, bancada 3), ou 2 ciclos de quem faz varias pecas por ciclo (rebitadeira:
        /// 8, senao ela so termina com a saida vazia). No Old Forge (1 peca por ciclo), a de sempre.</summary>
        static int SaidaBase(Station s) => Math.Max(s.Kind == Kind.Furnace ? Balance.FurnaceOut : Balance.CrafterOut, s.Recipe.Qtd > 1 ? 2 * s.Recipe.Qtd : 0);

        /// <summary>Insumo feito numa estacao (nem bruto nem produto): o papel 1 dos ajudantes carrega. Old Forge: so o lingote.</summary>
        public bool Intermediario(Item i) => (uint)i < (uint)_bruto.Length && !_bruto[(int)i] && !IsProduct(i);

        Station Add(Kind kind, float x, float y, string name, float inX, float inY, int unlockBy = -1, string family = null)
        {
            var s = new Station { Index = Stations.Count, Kind = kind, Pos = new V2(x, y), Name = name, UnlockBy = unlockBy, Unlocked = unlockBy < 0 };
            s.Recipe = family == null ? null : Array.Find(RecipeDefs, r => r.Station == family);   // familia sem receita: nao produz (ContentValidator acusa)
            if (s.Produces)
            {
                s.Ins = new int[s.Recipe.Inputs.Length]; s.OutItem = s.Recipe.Output;
                s.Liga = Array.FindIndex(s.Recipe.Inputs, x => x.Qty == 0);   // C-CORE-05
                bool furnace = kind == Kind.Furnace;   // pilhas: geometria da estacao, nao da receita
                // C-CORE-01: entrada de 2x o maior insumo da receita (montagem do peitoral: 4 rebites -> 8); no Old Forge da a de sempre (4 e 6)
                int maxQ = 0;
                foreach ((Item _, int q) in s.Recipe.Inputs) maxQ = Math.Max(maxQ, q);
                s.InCap = Math.Max(furnace ? Balance.FurnaceIn : Balance.CrafterIn, 2 * maxQ); s.OutCap = SaidaBase(s);
            }
            // ponto da boca = personagem encostado no meio da face (centro a CharRadius da face)
            V2 side = new V2(inX, inY), half = Balance.StationHalf;
            V2 reach = side * (Math.Abs(inX) * half.X + Math.Abs(inY) * half.Y + Balance.CharRadius);
            s.InAt = s.Pos + reach;
            s.OutAt = s.Produces ? s.Pos - reach : s.InAt;
            Stations.Add(s);
            return s;
        }

        /// <summary>B-CORE-01: o ItemDef diz (handle fora da tabela = nao).</summary>
        public bool IsProduct(Item i) => (uint)i < (uint)ItemDefs.Length && ItemDefs[(int)i].Product;
        /// <summary>Linha aberta = alguma estacao aberta faz este produto (o Recompute abre a estacao pelo upgrade dela).</summary>
        public bool LineUnlocked(Item product)
        {
            if (!IsProduct(product)) return false;
            foreach (Station s in Stations) if (s.Unlocked && Faz(s, product)) return true;   // C-CORE-06: a bancada configuravel abre todas as receitas liberadas
            return false;
        }
        /// <summary>O balcao `counter` vende `i`? O principal vende espada/escudo/ferramenta; a loja de joias, so joia.</summary>
        public bool Sells(Station counter, Item i) => counter.Kind == Kind.Counter && IsProduct(i) && (counter == JewelShop) == Joia(i);
        public Station CounterFor(Item product) => Joia(product) ? JewelShop : Counter;
        public List<Client> QueueFor(Item product) => Joia(product) ? JewelQueue : Queue;
        /// <summary>Produto na mao de `c` que o balcao dele aceita agora (estoque com espaco), o mais adiantado primeiro; Item.Ore = nenhum.</summary>
        public Item Deliverable(Carrier c)
        {
            for (int k = ItemDefs.Length - 1; k >= (int)Item.Sword; k--) if (IsProduct((Item)k) && c.Held[k] > 0 && (Stock[k] < CounterCap || CotaAceita((Item)k))) return (Item)k;   // C-CORE-07: ou o patio
            return Item.Ore;
        }
        /// <summary>B-CORE-07: a linha tem cliente (produto com intervalo no Tuning.ClientInterval). Produto sem intervalo (so o teste injeta) nao.</summary>
        public bool HasClients(Item p) => IsProduct(p) && (int)p < T.ClientInterval.Length && T.ClientInterval[(int)p] > 0f;
        /// <summary>B-CORE-07: o upgrade `u` nao exige projeto, ou o projeto que ele exige ja e' posse.</summary>
        public bool TemProjeto(int u) { string b = UpgradeDefs[u].Blueprint; return b == null || Projetos.Tem(b); }
        /// <summary>Algum cliente na fila quer `product` e nao ha nenhum no estoque (FASE7 §3: o que destrava a fila sem compra direta).</summary>
        public bool Wanted(Item product) => Stock[(int)product] == 0 && QueueFor(product).Exists(c => c.Want == product);
        /// <summary>Fama: multiplicador (1 -> 0,4) do intervalo entre clientes, pela quantidade de vendas ate aqui.</summary>
        public float Fame => Math.Max(T.FameFloor, (float)Math.Pow(T.FamePer10Sales, Sales / 10));
        public float ClientInterval(Item product) => T.ClientInterval[(int)product] * Fame * (Has(Upgrade.CounterCapacity) ? T.VitrineClientMul : 1f)
            * (Joia(product) && Has(Upgrade.JewelVitrine) ? T.JewelVitrineClientMul : 1f);
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
            TickArcano(fast);   // C-CORE-08: dreno e sobrecarga dos altares, no tempo das estacoes (Sim.Arcano.cs; sem altar, nada)
            TickCotas(dt);   // C-CORE-07: prazo dos blocos do patio de cotas (Sim.Cotas.cs; sem patio, nada)
            TickRotas(dt);   // C-CORE-09: rotas e lote do Mensageiro (Sim.Exportacao.cs; sem cais nem patio, nada)
            Anuncios.Tick(dt);   // E-PLAT-02: recargas e o Supply Drop entrando nas fornalhas
            VipCooldown = Math.Max(0f, VipCooldown - dt);   // P14: no tempo de jogo; o Serve recarrega quando o chamado sai
            StrikeCooldown = Math.Max(0f, StrikeCooldown - dt);   // B-CORE-03: golpe de mestre, no tempo de jogo (o boost nao encurta)
            Clients(dt);
            Milestones();
            Projetos.Checa();   // B-CORE-07: marcos, Renome e conquista viram projeto (Ev.Blueprint)
            Colecao.Checa();    // B-CORE-08: marcos da colecao (Ev.Colecao), 1x pelo ID
            Encomendas.Tick(dt);   // B-CORE-05
            Contratos.Tick(dt, this);   // D-CORE-04: prazo-bonus e a conclusao (os da raiz, no tempo de jogo de qualquer distrito)
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

        /// <summary>C-CORE-01: Via no distrito com a parede lateral do Old Forge (DistrictDef.Parede); sem parede, o proprio alvo.</summary>
        public V2 Rota(V2 from, V2 target) => Distrito.Parede ? Via(from, target) : target;

        /// <summary>Move `c` em direcao a `target` desviando dos corpos; devolve true quando chegou (dentro de `reach`).</summary>
        public bool MoveTowards(Carrier c, V2 target, float dt, float reach)
        {
            V2 d = target - c.Pos;
            float len = d.Len;
            c.Moving = len > reach;
            if (!c.Moving) return true;
            V2 goal = Rota(c.Pos, target);
            V2 dir = Steer(c, goal);
            c.MoveTo(Collide(Clamp(c.Pos + dir * Math.Min(V2.Dist(c.Pos, goal), c.Speed * dt), Balance.WorldW - 0.3f)), dt);   // ponytail: ajudante usa o mapa inteiro; so mira estacao desbloqueada, entao nunca entra na rua fechada
            return false;
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
            if (c.Role == 0) return Bruto(i);   // C-CORE-01: minerio no Old Forge; minerio ou couro na Row
            if (c.Role == 1) return Intermediario(i);   // C-CORE-01: lingote no Old Forge; lingote, placa e rebite na Row
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
            item = s.Kind == Kind.Deposit ? s.OutItem : outZone ? s.OutItem : s.InItem;   // deposito: o bruto dele (C-CORE-01)
            if (c.Role == 3 && s.Index != c.Target) return 0;   // joalheiro so mexe no proprio alvo: nao abastece a bancada do caminho
            switch (s.Kind)
            {
                case Kind.Deposit: return CanPick(c, s.OutItem) ? 3 : 0;
                case Kind.Quota: return AcaoCota(c, out item);   // C-CORE-07: 5 = entrega no patio de cotas
                case Kind.Export: return AcaoExporta(c, out item);   // C-CORE-09: 6 = entrega no lote do Mensageiro
                case Kind.Counter:
                    for (int k = (int)Item.Sword; k < ItemDefs.Length; k++)   // B-CORE-07: todo produto (a espada longa entrou)
                        if (c.Held[k] > 0 && Sells(s, (Item)k) && Stock[k] < CounterCap) { item = (Item)k; return 4; }
                    return 0;
                default:
                    if (outZone) return s.Out > 0 && CanPick(c, s.OutItem) && PegaInsumo(c, s.OutItem) ? 2 : 0;   // row-pendencias: so o que cabe
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
            float step = act == 3 ? T.GrabTime : Balance.TransferTime;
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
                    case 5: EntregaCota(c, it); break;   // C-CORE-07: patio (bloco cheio fecha e paga)
                    case 6: EntregaExporta(c, it); break;   // C-CORE-09: lote do Mensageiro
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
            if (UpgradeDefs[(int)u].Luxury && (!ProductionComplete || !Bought[UpgradeDefs[(int)u].Requires])) return false;
            if (!TemProjeto((int)u)) return false;   // B-CORE-07: nivel IV-V e bigorna longa so com o projeto
            int workersBefore = Workers.Count;
            Bought[(int)u] = true;
            UpgradeTime[(int)u] = Time;
            UpgradesBought++;
            Recompute();
            Emit(Ev.Bought, (int)u, (int)Math.Min(int.MaxValue, T.Cost(u)), PadOf(u));   // ponytail: evento e' int; acima de 2,1 bi sai no teto (so o diario le)
            foreach (Station s in Stations) if (s.UnlockBy == (int)u) Emit(Ev.Unlocked, (int)s.OutItem, s.Index, s.Pos);
            if (Workers.Count > workersBefore) Emit(Ev.Hired, Workers.Count - 1, 0, Contratacao);
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
            UpgradeDef d = UpgradeDefs[u];
            return d.InMenu && !Bought[u] && (d.Requires < 0 || Bought[d.Requires]) && TemProjeto(u);
        }

        V2 PadOf(Upgrade u)
        {
            if (UpgradeDefs[(int)u].InMenu) return Player.Pos;   // menu: o efeito sai do jogador, nao do pad antigo
            foreach (Pad p in Pads) foreach (Upgrade c in p.Chain) if (c == u) return p.Pos;
            return Player.Pos;
        }

        /// <summary>Deriva TUDO dos flags Bought (idempotente: vale depois de Buy e depois de Load).</summary>
        public void Recompute()
        {
            // C-CORE-01: os efeitos pelo Has (no Old Forge, Has(u) = Bought[u]: os mesmos numeros, bit a bit)
            float ft = Has(Upgrade.FurnaceSpeed2) ? T.FurnaceTime2 : Has(Upgrade.FurnaceSpeed1) ? T.FurnaceTime1 : T.FurnaceTime0;
            float ht = Has(Upgrade.HammerSpeed) ? T.HammerTime1 : T.HammerTime0;
            foreach (Station s in Stations)
            {
                if (s.UnlockBy >= 0) s.Unlocked = Bought[s.UnlockBy];
                // B-CORE-03: nivel da familia = 1 + niveis comprados dela
                s.Level = 1;
                if (s.Produces) foreach (UpgradeDef d in UpgradeDefs) if (d.Station == s.Recipe.Family && Bought[(int)d.Up]) s.Level++;   // B-CORE-07: pela familia (bigorna longa = bigornas)
                // B-CORE-01: base do tipo (fornalha: Fole; bancada: Martelo veloz) x multiplicador da receita; a Lupa so na Joalheria. B-CORE-03:
                // cada nivel acima do 1 da +5% de velocidade (nivel 1 divide por 1: o tempo de antes, bit a bit)
                if (s.Produces) s.Time = (s.Kind == Kind.Furnace ? ft : ht) * (s.Recipe.TimeMul(T) * (s == JewelBench && Has(Upgrade.JewelSpeed) ? T.JewelSpeedMul : 1f))
                    / (1f + T.NivelEstacaoVelocidade * (s.Level - 1)) / Equipe.Ritmo(s);   // B-CORE-04: oficial e mestre de obras (ninguem = / 1f)
                if (s.Produces) s.OutCap = SaidaBase(s) + Equipe.Saida(s);   // B-CORE-04: almoxarife
            }
            // balcao evolutivo (FASE7 §2): +1 vaga por evolucao comprada; o corpo alarga junto (antes dos Solids)
            QueueCap = T.QueueCap0;
            for (int u = (int)Upgrade.Counter5; u <= (int)Upgrade.Counter8; u++) if (Has((Upgrade)u)) QueueCap++;
            if (JewelShop == null && Has(Upgrade.JewelVitrine)) QueueCap += Balance.JewelQueueCapUp - Balance.JewelQueueCap;   // C-CORE-06: a loja fina 3 -> 5 (vitrine de luxo)
            QueueCap = Math.Min(QueueCap, Balance.QueueCapMax);   // trava de geometria: um QueueCap0 maior no Tuning nao tira o balcao da oficina
            Counter.Half = new V2(Balance.CounterHalfX(QueueCap), Balance.StationHalf.Y);
            // corpos: estacao travada nao e' solida (o pad de compra fica no lugar dela); loja de joias sim (a view desenha a fachada fechada)
            Solids.Clear();
            foreach (Station s in Stations) if (s.Unlocked || s.Kind == Kind.Counter) Solids.Add(s.Body);
            foreach (ObstacleDef o in Distrito.Obstaculos) if (o.Requires < 0 || Bought[o.Requires]) Solids.Add(o.Body);
            MaxX = (Has(Upgrade.SideCorridor) ? Balance.WorldW : Distrito.Largura) - 0.3f;
            Player.Speed = Has(Upgrade.PlayerSpeed) ? Balance.PlayerSpeedUp : Balance.PlayerSpeed;
            Player.Cap = Has(Upgrade.PlayerCapacity) ? Balance.PlayerCapUp : Balance.PlayerCap;
            CounterCap = (Has(Upgrade.CounterCapacity) ? T.CounterCap1 : T.CounterCap0) + Equipe.Estoque;   // B-CORE-04: almoxarife
            JewelQueueCap = Has(Upgrade.JewelVitrine) ? Balance.JewelQueueCapUp : Balance.JewelQueueCap;
            HasConveyor = Has(Upgrade.Conveyor) && EsteiraDe != null && EsteiraPara != null;
            // um ajudante por upgrade de contratacao (papel em _hireRole); a lista so cresce, na ordem da compra (o Joalheiro nao exige o Ajudante 3)
            for (int k = 0; k < _hireBy.Length; k++)
            {
                if (_hireBy[k] < 0 || !Bought[_hireBy[k]]) continue;
                int role = _hireRole[k], want = 0, have = 0;
                for (int j = 0; j <= k; j++) if (_hireRole[j] == role && _hireBy[j] >= 0 && Bought[_hireBy[j]]) want++;
                foreach (Carrier w in Workers) if (w.Role == role) have++;
                if (have < want) Workers.Add(new Carrier(ItemDefs.Length) { Pos = Contratacao, Role = role, Wk = _hireDef[k] });
            }
            bool fast = Has(Upgrade.HelperSpeed);
            foreach (Carrier w in Workers)   // B-CORE-04: nivel do ajudante (nivel 1 = x 1f e + 0)
            {
                w.Speed = (fast ? Balance.WorkerSpeedUp : Balance.WorkerSpeed) * Equipe.Passo(w);
                w.Cap = (fast ? Balance.WorkerCapUp : Balance.WorkerCap) + Equipe.Carga(w);
            }
            RecomputeCotas();   // C-CORE-07: a mesa_real da a 2a vaga de contrato do Imperio
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
                    if (s.Out + s.Recipe.Qtd <= s.OutCap)   // C-CORE-01: o ciclo inteiro cabe na saida (rebitadeira: 4 de uma vez; Qtd 1 = Out < OutCap)
                    {
                        s.Busy = false; s.Progress = 0f; s.Blocked = false; s.Crafted++; Crafted[(int)s.OutItem] += s.Recipe.Qtd;
                        Quality l = Grade(s);   // B-CORE-03: acumulador e golpe
                        if (l == Quality.Masterwork) { Masterworks++; Hall.Registra(s); }   // vai ao Hall (B-CORE-08): nunca a pilha nem o balcao
                        else
                        {
                            Put(ref s.Out, s.OutQ, l);
                            for (int k = 1; k < s.Recipe.Qtd; k++) s.Out++;   // as outras pecas do ciclo: insumo, sempre Normal (Grade so da nivel a produto)
                            if (l > Quality.Normal) Emit(Ev.Graded, s.Index, (int)l, s.Pos);
                        }
                        Emit(Ev.Crafted, (int)s.OutItem, s.Index, s.Pos);
                        TerminaArcano(s);   // C-CORE-08: energizada, sequencia e sobrecarga do altar (Sim.Arcano.cs; fora de altar, nada)
                    }
                    else Stall(s, dt);
                }
            }
            if (s.Busy) return;
            if (Trocando(s, dt)) return;   // C-CORE-06: bancada configuravel trocando de receita (Sim.Bancada)
            for (int k = 0; k < s.Ins.Length; k++) if (s.Ins[k] < s.Recipe.Inputs[k].Qty) { s.StarveSeconds += dt; return; }   // so com a receita inteira
            if (s.Out + s.Recipe.Qtd > s.OutCap) { Stall(s, dt); return; }
            if (EsperaArcano(s)) return;   // C-CORE-08: altar Estavel sem espaco no medidor, ou em sobrecarga (Sim.Arcano.cs)
            for (int k = 0; k < s.Ins.Length; k++) s.Ins[k] -= s.Recipe.Inputs[k].Qty;
            s.Busy = true; s.Progress = 0f;
            ComecaArcano(s);   // C-CORE-08: a carga entra no medidor
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
                Station a = EsteiraDe, b = EsteiraPara;   // C-CORE-01: as pontas do DistrictDef (Old Forge: Fornalha -> Bigorna)
                if (a.Out <= 0 || !b.Accepts(a.OutItem)) { _convT = 0f; break; }
                a.Out--; b.Ins[b.InIndex(a.OutItem)]++;
                Emit(Ev.Deposited, (int)a.OutItem, 9, b.Pos);   // B = 9: veio pela esteira
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
            if (Distrito.Clientes != null) ChegamFamilias(dt);   // C-CORE-06: um relogio por familia (Sim.Clientes) no lugar do por produto
            else for (int k = (int)Item.Sword; k < ItemDefs.Length; k++)   // B-CORE-07: toda linha aberta com cliente (a espada longa entrou)
            {
                Item p = (Item)k;
                if (!LineUnlocked(p) || !HasClients(p)) continue;
                _clientT[(int)p] -= dt * BoostMul;   // FASE8: a demanda acompanha o boost
                if (_clientT[(int)p] > 0f) continue;
                _clientT[(int)p] += ClientInterval(p);
                bool j = Joia(p);
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

        public int PriceOf(Item item) => Joia(item) && Has(Upgrade.JewelVitrine) ? T.JewelPriceUp : T.Price[(int)item];

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
            Encomendas.OnVenda(want, l);   // B-CORE-05: conta para a vaga mais antiga que aceita a peca
            Contratos.OnVenda(ItemDefs[(int)want].Id, l);    // D-CORE-04: e para a etapa atual do contrato ativo (o da raiz)
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
                MilestoneDef m = Distrito.Marcos[c.Index];
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
                    if (Stock[(int)c.Want] <= 0 || !TemParaCliente(c)) continue;   // C-CORE-06: o colecionador so leva Fina ou melhor
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
                    if (c.Pack > 1) { Sell(c.Want, Slot(jewel, i)); c.Pack--; break; }   // C-CORE-06: o mercador leva 2, 1 por atendimento
                    q.RemoveAt(i);
                    Sell(c.Want, Slot(jewel, i));
                    Atendido(c);   // C-CORE-06: RichClients
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
            Renome.On(kind, a, b);  // B-CORE-06: bau aberto (a encomenda credita pelo tipo no Encomendas.Entregar)
            LiveOps.On(kind, a, b); // D-CORE-03/04: missoes do evento ativo e XP de encomenda do Pass
            // C-CORE-03: venda, VIP, encomenda e cofre de um distrito contam tambem nas metas, no evento e no Pass do Imperio (os da raiz), 1x;
            // o contador do proprio distrito segue (o Hall dele le). ponytail: compra, contratacao, qualidade e obra-prima ficam so no distrito
            // (indice de upgrade, worker, estacao e Hall nao traduzem para a raiz); entram com uma meta/missao que os peca por distrito
            if (Heroi != null && (kind == Ev.Sold || kind == Ev.VipServed || kind == Ev.OrderDone || kind == Ev.Offline))
            {
                int r = kind == Ev.Offline ? a : NaRaiz(a);
                Heroi.Metas.On(kind, r, b);
                if (r < 0 && kind == Ev.Sold) Heroi.Metas.RegistraItem("product_sold", ItemDefs[a].Id);   // raia H: o elmo conta pelo ID (ach_elmos100)
                Heroi.LiveOps.On(kind, r, b);
            }
        }

        /// <summary>C-CORE-03: o item `item` deste distrito na tabela da raiz, pelo ID (o que o Old Forge nao tem = -1: conta no total da meta ou
        /// missao, sem filtro de item).</summary>
        int NaRaiz(int item) => Items.IndexOf(ItemDefs[item].Id, Heroi.ItemDefs);

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
            for (int u = 0; u < UpgradeDefs.Length; u++)
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
            for (int i = 0; i < UpgradeDefs.Length; i++)
            {
                if (!IsProduction(i)) continue;   // luxo e Meta (B-CORE-03) nao mexem no cofre
                long c = T.Cost(i); int req = UpgradeDefs[i].Requires;
                top = Math.Max(top, c);
                // FASE7: so o que esta A VENDA (pre-requisito comprado). Sem isso o Balcao 5 (150, exige a Vitrine) baixava o cofre de
                // 1.060 para 300 aos 10 min; e o Joalheiro 2 (2.400, exige o Mineiro) segurava o teto do par em 4.800 (agora 2x Mineiro)
                if (!Bought[i] && (req < 0 || Bought[req]) && (best < 0 || c < best)) best = c;
            }
            return best >= 0 ? best : top;
        }

        /// <summary>Teto relativo do cofre: 2x o proximo upgrade travado.</summary>
        public long OfflineMaxGold() => (long)Math.Round(T.OfflineMaxNextUpgrades * CheapestLockedCost());

    }
}
