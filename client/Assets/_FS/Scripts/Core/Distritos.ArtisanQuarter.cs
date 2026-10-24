namespace FS.Core
{
    /// <summary>
    /// C-CORE-06: o Bairro Artesao (Artisan Quarter, distrito 3) em dados — CONTEUDO_V1 §2.3, §3, §3.1, §4, §5.3, §5.7, §6, §7, §9, §10, §12.
    /// Mecanica nova: a bancada de ourives configuravel (Sim.Bancada: qualquer receita liberada do distrito, troca de 4 s, insumo de fora fica
    /// guardado) e os clientes por familia (Sim.Clientes: viajante, mercador, nobre e colecionador, com rodizio fixo de pedidos). Tudo o que e'
    /// so deste distrito mora aqui (Distritos, OrderDefs, ProjetosDefs, MasterworksDefs: classes parciais); as listas compartilhadas
    /// (Distritos.All, OrderDefs.Distritos/Ciclos/All, ProjetosDefs.Distritos, Imperio.Managers) apontam para ca num bloco marcado.
    /// </summary>
    public static partial class Distritos
    {
        public const string ArtisanQuarterId = "artisan_quarter";

        // Handles (ContentValidator.Layout): 0 = o bruto do kit (minerio de cobre), 1 = o intermediario dele (lingote de cobre), 2 = o 1o produto
        // (fivela, o 1o cliente cedo); produtos de 2 a 8; os insumos do ouro e das gemas depois (abrem com o veio de ouro e o lapidario)
        const Item AqMinCobre = (Item)0, AqLingCobre = (Item)1, AqFivela = (Item)2, AqCinzel = (Item)3, AqCastical = (Item)4, AqBroche = (Item)5,
            AqCalice = (Item)6, AqColar = (Item)7, AqCaixa = (Item)8, AqMinOuro = (Item)9, AqLingOuro = (Item)10, AqGemaBruta = (Item)11, AqGema = (Item)12;

        static readonly ItemDef[] ItensArtisan =
        {
            new ItemDef("minerio_cobre", "minério de cobre", 3, false),
            new ItemDef("lingote_cobre", "lingote de cobre", 3, false),
            new ItemDef("fivela", "fivela", 3, true),
            new ItemDef("cinzel", "cinzel", 3, true),
            new ItemDef("castical", "castiçal", 3, true),
            new ItemDef("broche", "broche", 3, true),
            new ItemDef("calice", "cálice", 3, true),
            new ItemDef("colar", "colar", 3, true),
            new ItemDef("caixa_joias", "caixa de joias", 3, true),
            new ItemDef("minerio_ouro", "minério de ouro", 3, false),
            new ItemDef("lingote_ouro", "lingote de ouro", 3, false),
            new ItemDef("gema_bruta", "gema bruta", 3, false),
            new ItemDef("gema", "gema", 3, false),
        };

        /// <summary>§3.1: TimeMul = tempo base / tempo inicial do tipo (crisol e lapidario: fornalha de 4 s; bancada: 5 s). As 7 receitas da
        /// bancada de ourives tem a mesma Station ("ourivesaria", que e' tambem a familia de nivel, tecnica e oficial): e' isso que a faz
        /// configuravel (Sim.Bancada). A 1a e' a de fabrica; `Abre` = o upgrade que libera a receita (null = do kit: fivela e cinzel).</summary>
        static readonly RecipeDef[] ReceitasArtisan =
        {
            new RecipeDef("crisol_cobre", Kind.Furnace, AqLingCobre, t => 0.75f, (AqMinCobre, 1)),
            new RecipeDef("crisol_ouro", Kind.Furnace, AqLingOuro, t => 1f, (AqMinOuro, 1)),
            new RecipeDef("lapidario", Kind.Furnace, AqGema, t => 1.5f, (AqGemaBruta, 1)),
            new RecipeDef("ourivesaria", Kind.Crafter, AqFivela, t => 0.6f, (AqLingCobre, 1)),
            new RecipeDef("ourivesaria", Kind.Crafter, AqCinzel, t => 0.8f, (AqLingCobre, 2)),
            new RecipeDef("ourivesaria", Kind.Crafter, AqCastical, t => 1.2f, (AqLingCobre, 3)) { Abre = "receita_castical" },
            new RecipeDef("ourivesaria", Kind.Crafter, AqBroche, t => 1f, (AqLingOuro, 1), (AqLingCobre, 1)) { Abre = "veio_ouro" },
            new RecipeDef("ourivesaria", Kind.Crafter, AqCalice, t => 1.4f, (AqLingOuro, 2)) { Abre = "receita_calice" },
            new RecipeDef("ourivesaria", Kind.Crafter, AqColar, t => 1.6f, (AqLingOuro, 1), (AqGema, 1)) { Abre = "lapidario" },
            new RecipeDef("ourivesaria", Kind.Crafter, AqCaixa, t => 2f, (AqLingCobre, 1), (AqLingOuro, 1), (AqGema, 1)) { Abre = "receita_caixa" },
        };

        const string MestraArtisan = "bp_mestra_artisan", Ourivesaria = "ourivesaria";

        /// <summary>CONTEUDO_V1 §5.3 na ordem do tier (= indice); os luxos e os niveis da ourivesaria (§5.7, Meta, menu; IV-V com o projeto de
        /// mestre) depois. Efeitos genericos: o fole (crisois e lapidario, que sao fornalhas), a lupa (bancadas) e a vitrine de luxo (a
        /// "vitrine de joias" do Old Forge: a loja fina 3 -> 5 vagas e os ricos x0,7 no intervalo; Sim.Recompute e Sim.Clientes).</summary>
        static readonly UpgradeDef[] UpgradesArtisan =
        {
            U(0, "fole_ourives", -1, "Fole de ourives", "Crisóis mais rápidos", menu: true, efeito: Upgrade.FurnaceSpeed1),
            U(1, "bancada_ourives2", -1, "Bancada 2", "Mais uma bancada de ourives"),
            U(2, "dourado1", -1, "Dourado 1", "Leva minérios aos crisóis"),
            U(3, "receita_castical", -1, "Castiçais", "Receita nas bancadas: castiçal"),
            U(4, "veio_ouro", -1, "Veio de ouro", "Ouro e receita: broche"),
            U(5, "dourado2", 2, "Dourado 2", "Leva lingotes às bancadas"),
            U(6, "vitrine_luxo", -1, "Vitrine de luxo", "Loja com 5 vagas e mais nobres", menu: true, efeito: Upgrade.JewelVitrine),
            U(7, "receita_calice", 4, "Cálices", "Receita nas bancadas: cálice"),
            U(8, "crisol_cobre2", -1, "Crisol 2", "Dobra os lingotes de cobre"),
            U(9, "dourado3", 5, "Dourado 3", "Leva peças à loja"),
            U(10, "lapidario", 4, "Lapidário", "Gemas e receita: colar"),
            U(11, "bancada_ourives3", 1, "Bancada 3", "Mais uma bancada de ourives"),
            U(12, "receita_caixa", 10, "Caixas de joias", "Receita: caixa de joias"),
            U(13, "fole_ourives_duplo", 0, "Fole duplo", "Crisóis ainda mais rápidos", menu: true, efeito: Upgrade.FurnaceSpeed2),
            U(14, "lupa_ourives", -1, "Lupa de ourives", "Bancadas mais rápidas", menu: true, efeito: Upgrade.HammerSpeed),
            U(15, "lapidario2", 10, "Lapidário 2", "Dobra as gemas"),
            U(16, "crisol_ouro2", 4, "Crisol de ouro 2", "Dobra os lingotes de ouro"),
            U(17, "troca_rapida", 11, "Troca rápida", "Trocar a receita leva 1 s", menu: true),
            U(18, "fachada_dourada", 17, "Fachada dourada", "Fachada de ouro na rua", luxo: true),
            U(19, "lanternas_rua", 18, "Lanternas", "Lanternas na rua", luxo: true),
            U(20, "fonte_bairro", 19, "Fonte do bairro", "Uma fonte no bairro", luxo: true),
            U(21, "nivel_ourivesaria2", 0, "Ourivesaria II", "Mais qualidade e velocidade", menu: true, station: Ourivesaria),
            U(22, "nivel_ourivesaria3", 21, "Ourivesaria III", "Mais qualidade e velocidade", menu: true, station: Ourivesaria),
            U(23, "nivel_ourivesaria4", 22, "Ourivesaria IV", "Mais qualidade e velocidade", menu: true, station: Ourivesaria, blueprint: MestraArtisan),
            U(24, "nivel_ourivesaria5", 23, "Ourivesaria V", "Mais qualidade e velocidade", menu: true, station: Ourivesaria, blueprint: MestraArtisan),
        };

        /// <summary>C-CORE-06 (BALANCE.md §37): os produtivos do §5.3 x 1,5 (o catalogo: 1.440 x 1,30^tier = 1.440 ... 120.000; aqui 2.160 ...
        /// 180.000, multiplo de 5 ate 10 mil e 2 algarismos acima, a regra do §1); o luxo como no catalogo; os niveis do §5.7 x 1,5 tambem
        /// (ourivesaria 36.000/105.000/290.000/630.000; o catalogo: 24.000/71.000/190.000/420.000): com a regra de transporte da main (#76) a
        /// renda subiu e o Charter do Ideal caia para 3:54 h (janela 4-6 h). Com o preco x 3 (Ajuste) a producao completa do F2P sai de 1:34 h
        /// para ~1 h e a renda sobe a ~16 mil/min.</summary>
        static readonly long[] CustosArtisan =
        {
            2160, 2805, 3655, 4750, 6175, 8020, 10000, 14000, 18000, 23000, 30000, 39000, 51000, 66000, 86000, 110000, 140000, 180000,   // produtivos
            130000, 160000, 200000,                                                                                                 // luxo
            36000, 105000, 290000, 630000,                                                                                          // niveis (Meta)
        };

        /// <summary>§6: niveis 2-10 da familia Dourado (2.250 x 1,6^(L-2); a turma inteira sobe junto no catalogo, aqui cada corpo).</summary>
        static readonly long[] TurmaDourado = { 2250, 3600, 5760, 9215, 15000, 24000, 38000, 60000, 97000 };
        static WorkerDef Dourado(int n, int rota) => new WorkerDef("wk_dourado" + n, "Dourado", WorkerRole.Porter, Specialty.Carrying, ArtisanQuarterId, null,
            "dourado" + n, rota, 0, null, TurmaDourado, "ajudante", "velocidade", "carga");

        /// <summary>§6: a oficial das bancadas de ourives (Smith, regra de sempre: q +15, +25 no nivel 5, e o passo da familia) que tambem tira
        /// 1 s da troca de receita (Sim.TempoTroca, minimo 1 s). Contratacao e niveis do catalogo, em ouro; ponytail: a folha do oficial.</summary>
        static readonly WorkerDef Lucia = new WorkerDef("wk_lucia", "Lúcia", WorkerRole.Smith, Specialty.Jewels, ArtisanQuarterId, Ourivesaria, null, -1,
            22000, "nivel_ourivesaria2", new long[] { 5500, 8800, 14000, 23000, 36000, 58000, 92000, 150000, 240000 }, "oficial", "qualidade", "troca");

        /// <summary>§12: as familias do Quarter. Mercador e viajante pedem as pecas baratas e rapidas, chegam mais e esperam pouco; nobre e
        /// colecionador (os "ricos" do RichClients) pedem as caras, chegam a cada 2,5-3x o intervalo e esperam 1,8-2x mais; o mercador leva 2;
        /// o colecionador quer qualquer produto do distrito na Fina ou melhor e abre no Renome 2 (cli_colecionador, RenomeDefs).</summary>
        static readonly ClientDef[] ClientesArtisan =
        {
            new ClientDef("viajante", 1, 1f, 1f, Quality.Normal, false, "fivela", "castical"),
            new ClientDef("mercador", 2, 0.8f, 1.3f, Quality.Normal, false, "fivela", "cinzel", "castical"),
            new ClientDef("nobre", 1, 1.8f, 2.5f, Quality.Normal, true, "colar", "broche", "calice", "caixa_joias"),
            new ClientDef("colecionador", 1, 2f, 3f, Quality.Fine, true),
        };

        // ponytail: arte emprestada (como a Row): a bancada de ourives = a joalheria do Old Forge; crisois e lapidario pelo tipo (fornalha)
        const string ArteOurives = "bancada_ourives";   // C-ART-13 parte 2: folhas do Tripo (ASSETS.md §15)

        public static readonly DistrictDef ArtisanQuarter = new DistrictDef
        {
            Id = ArtisanQuarterId, Nome = "Artisan Quarter", Indice = 2,
            Itens = ItensArtisan, Receitas = ReceitasArtisan, Upgrades = UpgradesArtisan, Projetos = ProjetosDefs.ArtisanDefs,
            Masterworks = MasterworksDefs.ArtisanDefs, MarcosColecao = MasterworksDefs.MarcosArtisan,
            Workers = new[] { Dourado(1, 0), Dourado(2, 1), Dourado(3, 2), Lucia },
            Clientes = ClientesArtisan,
            // Fileiras de baixo para cima (depositos 1 -> crisois 4 -> lapidarios 6,5 -> bancadas 9 -> loja 13), colunas de 3 m, entrada a
            // esquerda. O kit (as 4 primeiras) e' o do §2.3: deposito de cobre, crisol de cobre, 1 bancada de ourives e a loja fina. Os IDs
            // deposito/fornalha/balcao sao os papeis fixos do Sim; o rotulo deles vem de est.artisan_quarter.* (Textos.Distrito)
            Estacoes = new[]
            {
                new StationDef("deposito", Kind.Deposit, 1.5f, 1.0f, "Minério cobre", 0f, 1f, null, null, AqMinCobre, arte: "deposito_cobre"),
                new StationDef("fornalha", Kind.Furnace, 1.5f, 4.0f, "Crisol cobre", -1f, 0f, null, "crisol_cobre", arte: "crisol"),
                new StationDef("bancada_ourives", Kind.Crafter, 1.5f, 9.0f, "Bancada ourives", -1f, 0f, null, Ourivesaria, arte: ArteOurives),
                new StationDef("balcao", Kind.Counter, 4.5f, 13f, "Loja fina", 0f, -1f, arte: "loja_rica"),
                new StationDef("bancada_ourives2", Kind.Crafter, 4.5f, 9.0f, "Bancada 2", -1f, 0f, "bancada_ourives2", Ourivesaria, arte: ArteOurives),
                new StationDef("deposito_ouro", Kind.Deposit, 7.5f, 1.0f, "Minério ouro", 0f, 1f, "veio_ouro", null, AqMinOuro, arte: "deposito_ouro"),
                new StationDef("crisol_ouro", Kind.Furnace, 7.5f, 4.0f, "Crisol ouro", -1f, 0f, "veio_ouro", "crisol_ouro", arte: "crisol"),
                new StationDef("crisol_cobre2", Kind.Furnace, 4.5f, 4.0f, "Crisol cobre 2", -1f, 0f, "crisol_cobre2", "crisol_cobre", arte: "crisol"),
                new StationDef("deposito_gemas", Kind.Deposit, 13.5f, 1.0f, "Gemas brutas", 0f, 1f, "lapidario", null, AqGemaBruta, arte: "deposito_gemas"),
                new StationDef("lapidario", Kind.Furnace, 10.5f, 6.5f, "Lapidário", -1f, 0f, "lapidario", "lapidario", arte: "lapidario"),
                new StationDef("bancada_ourives3", Kind.Crafter, 7.5f, 9.0f, "Bancada 3", -1f, 0f, "bancada_ourives3", Ourivesaria, arte: ArteOurives),
                new StationDef("lapidario2", Kind.Furnace, 13.5f, 6.5f, "Lapidário 2", -1f, 0f, "lapidario2", "lapidario", arte: "lapidario"),
                new StationDef("crisol_ouro2", Kind.Furnace, 10.5f, 4.0f, "Crisol ouro 2", -1f, 0f, "crisol_ouro2", "crisol_ouro", arte: "crisol"),
            },
            Pads = new[]
            {
                new PadDef(4.5f, 9.0f, "bancada_ourives2"),
                new PadDef(10.5f, 1.5f, "dourado1", "dourado2", "dourado3"),
                new PadDef(10.5f, 9.0f, "receita_castical", "receita_calice", "receita_caixa"),
                new PadDef(7.5f, 4.0f, "veio_ouro"),
                new PadDef(4.5f, 4.0f, "crisol_cobre2"),
                new PadDef(10.5f, 6.5f, "lapidario"),
                new PadDef(7.5f, 9.0f, "bancada_ourives3"),
                new PadDef(13.5f, 6.5f, "lapidario2"),
                new PadDef(10.5f, 4.0f, "crisol_ouro2"),
                new PadDef(12.6f, 12.6f, "fachada_dourada", "lanternas_rua", "fonte_bairro"),
            },
            Obstaculos = new ObstacleDef[0], Marcos = new MilestoneDef[0],
            Inicio = new V2(4.5f, 2.6f), Contratacao = new V2(10.5f, 1.5f), Largura = Balance.WorldW, Parede = false,
            Ajuste = t =>
            {
                // §3 precos (Normal) x 3, desvio do catalogo (revisar; BALANCE.md §37): como na Row (V28), o Quarter e' preso no transporte
                // (3 Dourado com carga 2 levando minerio, lingote e peca; a bancada parada na troca), nao nas bancadas: ~12 pecas/min contra
                // ~56 do modelo do §1. A relacao entre os produtos e' a do §3
                t.Price = new[] { 0, 0, 330, 570, 870, 720, 900, 1050, 1350, 0, 0, 0, 0 };
                // §12: um intervalo "da linha" so (12 s) para as 7: o x1,3 do mercador, o x2,5 do nobre e o x3 do colecionador valem como o
                // catalogo diz (os ricos chegam 2,5-3x mais espacados). Desvio (revisar): com o intervalo do modelo por linha (60 / un/min do §1
                // "mix") o nobre, com 4 produtos no rodizio, chegava a cada ~26 s e os ricos davam 20% da receita (BALANCE.md §37)
                t.ClientInterval = new[] { 0f, 0f, 12f, 12f, 12f, 12f, 12f, 12f, 12f, 0f, 0f, 0f, 0f };
                t.CustoTabela = CustosArtisan;
                // §7.2 x 0,85 no premio de toda encomenda do Quarter (alem dos 70% de Bulk/Quality/Mixed/Royal/freguesa): com 7 linhas o N cai no
                // minimo e o ouro de encomendas ia a 12,9% da venda (portao 4-12%); e o alvo x 1,2 (180 s de renda na Common): com o transporte da
                // main (#76) o Renome 6 chegava aos 224 min no Ideal (o Charter abaixo das 4 h; BALANCE.md §37)
                t.OrderRewardSeconds = 8.5f; t.OrderSeconds = 180f;
                t.QueueCap0 = 3; t.CounterCap0 = t.CounterCap1 = 10;
                t.OrderRewardMin = 225;   // §7.2: Common do Quarter, 225 de minimo (x9 o do Old Forge)
            },
            Abertura = 430000,
            Charter = new CharterDef(true, 1250, 3, RoyalArsenalId),   // §2.3: producao completa + Renome 6 (1.250) + 3 Masterworks; o proximo e' o Arsenal (C-CORE-07)
        };
    }

    public static partial class OrderDefs
    {
        /// <summary>C-CORE-06 (CONTEUDO_V1 §7.2): os 7 tipos do Quarter, a variante Superior do Quality (Renome 6, RenomeDefs) e a Condessa
        /// Amara (4 colares Fina). Metodo, nao campo: o All (EncomendasDefs.cs) le daqui e a ordem dos inicializadores entre arquivos de uma
        /// classe parcial nao e' garantida. Mesmo desvio do Old Forge e da Row (revisar): Bulk, Quality, Mixed, Royal e a freguesa pagam 70%
        /// dos segundos do §7.2.</summary>
        static OrderDef[] OrdensArtisan() => new[]
        {
            new OrderDef("ord_common_artisan_quarter", Common, FS.Core.Distritos.ArtisanQuarterId, 150f, 3, 30, 10f, 225, 1),
            new OrderDef("ord_bulk_artisan_quarter", Bulk, FS.Core.Distritos.ArtisanQuarterId, 400f, 8, 80, 22f, 540, 3),
            new OrderDef("ord_quality_artisan_quarter", Qual, FS.Core.Distritos.ArtisanQuarterId, 90f, 3, 20, 18f, 360, 4, minQuality: Fina),
            new OrderDef("ord_quality_artisan_quarter_superior", Qual, FS.Core.Distritos.ArtisanQuarterId, 90f, 3, 20, 29f, 675, 4, minQuality: Superior),
            new OrderDef("ord_rush_artisan_quarter", Rush, FS.Core.Distritos.ArtisanQuarterId, 150f, 3, 30, 10f, 225, 1, bonusDeadline: 240f, bonusRewardSeconds: 10f, bonusReputation: 1),
            new OrderDef("ord_mixed_artisan_quarter", Mixed, FS.Core.Distritos.ArtisanQuarterId, 70f, 2, 10, 21f, 540, 4, products: 3, minLines: 3),
            new OrderDef("ord_royal_artisan_quarter", Royal, FS.Core.Distritos.ArtisanQuarterId, 200f, 5, 40, 32f, 1350, 10, minQuality: Fina, blueprintQueue: FilaRoyal),
            new OrderDef("ord_blueprint_artisan_quarter", Blueprint, FS.Core.Distritos.ArtisanQuarterId, 150f, 3, 30, 5f, 225, 2, blueprintQueue: FilaEncomenda),
            new OrderDef("ord_reg_amara", Qual, FS.Core.Distritos.ArtisanQuarterId, 0f, 4, 4, 13f, 360, 3, minQuality: Fina, item: "colar", fixedN: 4, clientId: "cli_reg_amara"),
        };

        /// <summary>§7.1: Mixed x2 no Quarter (3 produtos).</summary>
        static OrderType[] CicloArtisan() => new[] { Common, Mixed, Qual, Common, Mixed, Royal, Rush, Blueprint, Common, Bulk };
    }

    public static partial class ProjetosDefs
    {
        static BlueprintDef Aq(string id, BlueprintKind kind, string alvo, bool essencial, params BlueprintSource[] fontes) =>
            new BlueprintDef(id, FS.Core.Distritos.ArtisanQuarterId, kind, alvo.Split(','), essencial, fontes);
        static BlueprintSource FilaAq(int n) => new BlueprintSource(BlueprintSourceKind.OrderBlueprint, FS.Core.Distritos.ArtisanQuarterId, n);
        static BlueprintSource RoyalAq(int n) => new BlueprintSource(BlueprintSourceKind.OrderRoyal, FS.Core.Distritos.ArtisanQuarterId, n);

        /// <summary>
        /// C-CORE-06: os 14 do Artisan Quarter (CONTEUDO_V1 §9), na ordem do catalogo; DistrictDef.Projetos do Quarter. Essenciais com fonte
        /// gratis permanente: o de mestre (L4-L5: fila Royal #1 ou a producao completa) e os projetos das 3 obras que o Charter pede (fivela do
        /// viajante: 200 fivelas; colar da aurora: fila #3 ou 200 colares; broche da condessa: Royal #2 ou 300 broches). A tecnica da lapidacao
        /// e' de produto (colar e caixa: Tuning.TecnicaItemQ). ponytail: a 2a fonte do bp_mw_caixa_rainha (Contract: ctr_dote_princesa) fica de
        /// fora, como o peitoral do rival da Row: o contrato e' do Imperio e da o bp_var_dote na raiz.
        /// </summary>
        public static readonly BlueprintDef[] ArtisanDefs =
        {
            Aq("bp_tecnica_ourivesaria", BlueprintKind.Technique, "ourivesaria", false, FilaAq(1)),
            Aq("bp_tecnica_lapidacao", BlueprintKind.Technique, "colar,caixa_joias", false, FilaAq(2)),
            Aq("bp_mestra_artisan", BlueprintKind.StationLevel, "ourivesaria", true, RoyalAq(1), Producao()),
            Aq("bp_var_vitrine_cristal", BlueprintKind.Variant, "fivela", false, Vendidos("fivela", 300)),
            Aq("bp_mw_fivela_viajante", BlueprintKind.Masterwork, "mw_fivela_viajante", true, Vendidos("fivela", 200)),
            Aq("bp_mw_colar_aurora", BlueprintKind.Masterwork, "mw_colar_aurora", true, FilaAq(3), Vendidos("colar", 200)),
            Aq("bp_mw_broche_condessa", BlueprintKind.Masterwork, "mw_broche_condessa", true, RoyalAq(2), Vendidos("broche", 300)),
            Aq("bp_mw_cinzel_mestre", BlueprintKind.Masterwork, "mw_cinzel_mestre", false, Vendidos("cinzel", 300)),
            Aq("bp_mw_castical_vigilia", BlueprintKind.Masterwork, "mw_castical_vigilia", false, FilaAq(4)),
            Aq("bp_mw_caixa_segredos", BlueprintKind.Masterwork, "mw_caixa_segredos", false, FilaAq(5)),
            Aq("bp_mw_calice_banquete", BlueprintKind.Masterwork, "mw_calice_banquete", false, Vendidos("calice", 200)),
            Aq("bp_mw_colar_lagrimas", BlueprintKind.Masterwork, "mw_colar_lagrimas", false, RoyalAq(3)),
            Aq("bp_mw_broche_guilda", BlueprintKind.Masterwork, "mw_broche_guilda", false, Renome(6)),
            Aq("bp_mw_caixa_rainha", BlueprintKind.Masterwork, "mw_caixa_rainha", false, RoyalAq(4)),
        };
    }

    public static partial class MasterworksDefs
    {
        static CondicaoMasterwork Ricos(int n) => new CondicaoMasterwork(CondicaoMw.ClientesRicos, null, n);
        static CondicaoMasterwork Feitos(string item, int n) => new CondicaoMasterwork(CondicaoMw.Feitos, item, n);
        static MasterworkDef Aq(string id, string familia, int renome, int pedestal, params CondicaoMasterwork[] c) =>
            new MasterworkDef(id, FS.Core.Distritos.ArtisanQuarterId, familia, "bp_" + id, renome, pedestal, c);

        /// <summary>C-CORE-06: as 10 do Artisan Quarter (CONTEUDO_V1 §10.1), DistrictDef.Masterworks do Quarter. Superior alcancavel (§10.2):
        /// ourivesaria 60 (niveis) + 20 (tecnica) + 25 (Lucia no nivel 5) = 105, e a lapidacao +15 no colar e na caixa. Condicoes novas:
        /// RichClients (nobres e colecionadores atendidos no distrito) e GemsCut (Feitos: gemas lapidadas). O Charter pede 3: as essenciais
        /// sao a fivela do viajante, o colar da aurora (ourivesaria V) e o broche da condessa (100 ricos).</summary>
        public static readonly MasterworkDef[] ArtisanDefs =
        {
            Aq("mw_fivela_viajante", "fivela", 35, 1),
            Aq("mw_colar_aurora", "colar", 40, 2, Nivel("ourivesaria", 5)),
            Aq("mw_broche_condessa", "broche", 40, 3, Ricos(100)),
            Aq("mw_cinzel_mestre", "cinzel", 40, 4, Worker("wk_lucia", 5)),
            Aq("mw_castical_vigilia", "castical", 40, 5, Vendidos("castical", 800)),
            Aq("mw_caixa_segredos", "caixa_joias", 45, 6, EncomendasTipo("mixed", 10)),
            Aq("mw_calice_banquete", "calice", 40, 7, Vendidos("calice", 600)),
            Aq("mw_colar_lagrimas", "colar", 45, 8, Feitos("gema", 1000)),
            Aq("mw_broche_guilda", "broche", 50, 9, Renome(7)),
            Aq("mw_caixa_rainha", "caixa_joias", 60, 10, new CondicaoMasterwork(CondicaoMw.VendidosQualidade, "caixa_joias", 50, Quality.Superior)),
        };

        /// <summary>C-CORE-06: os marcos da Collection do Quarter, os degraus da Row com as contas dele (Produtos 21 = 7 x 3; Workers 8 = 4 x
        /// contratado/nivel maximo; Blueprints 14; Masterworks 10 = o col_mw_distrito, so o estandarte).</summary>
        public static readonly MarcoColecaoDef[] MarcosArtisan =
        {
            new MarcoColecaoDef("col_produtos_artisan_quarter_50", AbaColecao.Produtos, 11, 5),
            new MarcoColecaoDef("col_produtos_artisan_quarter_100", AbaColecao.Produtos, 21, 10),
            new MarcoColecaoDef("col_mw_distrito_artisan_quarter", AbaColecao.Masterworks, 10, 0, "cos_col_estandarte_artisan_quarter"),
            new MarcoColecaoDef("col_workers_artisan_quarter_50", AbaColecao.Workers, 4, 5),
            new MarcoColecaoDef("col_workers_artisan_quarter_100", AbaColecao.Workers, 8, 10),
            new MarcoColecaoDef("col_bp_artisan_quarter_50", AbaColecao.Blueprints, 7, 5),
            new MarcoColecaoDef("col_bp_artisan_quarter_100", AbaColecao.Blueprints, 14, 10),
        };
    }
}
