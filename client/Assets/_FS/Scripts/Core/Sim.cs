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
    }
}
