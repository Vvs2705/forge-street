using System;

namespace FS.Core
{
    /// <summary>
    /// C-CORE-08 (CONTEUDO_V1 §2.5, §3, §3.1, §4, §5.5, §5.7, §6, §12): a Arcane Foundry (distrito 5) em dados. Mecanica nova: o medidor de
    /// carga dos altares (Sim.Arcano.cs; Tuning.CargaArcana no Ajuste). Handles: 0 e 1 = minerio e lingote (papeis 0/1 dos ajudantes),
    /// produtos de 2 a 8 (o 2, lanterna arcana, sai do kit: 1o cliente e VIP), insumos e brutos depois.
    /// </summary>
    public static partial class Distritos
    {
        public const string ArcaneFoundryId = "arcane_foundry";

        const Item AfLanterna = (Item)2, AfAnel = (Item)3, AfAmuleto = (Item)4, AfLamina = (Item)5, AfEscudo = (Item)6, AfCajado = (Item)7,
            AfCeleste = (Item)8, AfCristal = (Item)9, AfLingRunico = (Item)10, AfLiga = (Item)11, AfCristalBruto = (Item)12, AfMinRunico = (Item)13;

        static readonly ItemDef[] ItensArcane =
        {
            new ItemDef("minerio", "minério", 1, false),
            new ItemDef("lingote", "lingote", 1, false),
            new ItemDef("lanterna_arcana", "lanterna arcana", 5, true),
            new ItemDef("anel_cristal", "anel de cristal", 5, true),
            new ItemDef("amuleto_arcano", "amuleto arcano", 5, true),
            new ItemDef("lamina_runica", "lâmina rúnica", 5, true),
            new ItemDef("escudo_runico", "escudo rúnico", 5, true),
            new ItemDef("cajado_liga", "cajado de liga", 5, true),
            new ItemDef("liga_celeste", "liga celeste", 5, true),
            new ItemDef("cristal", "cristal", 5, false),
            new ItemDef("lingote_runico", "lingote rúnico", 5, false),
            new ItemDef("liga", "liga", 2, false),
            new ItemDef("cristal_bruto", "cristal bruto", 5, false),
            new ItemDef("minerio_runico", "minério rúnico", 5, false),
        };

        /// <summary>§3.1: TimeMul = tempo base / tempo inicial do tipo (fornalha 4 s, bancada 5 s). Os altares e o cadinho sao bancadas
        /// (Kind.Crafter) das familias altar_cristal e altar_runa (§4): o medidor (Sim.Arcano) e' a regra a mais, pela carga do produto.</summary>
        static readonly RecipeDef[] ReceitasArcane =
        {
            new RecipeDef("fornalha", Kind.Furnace, Lingote, t => 1f, (Minerio, 1)),
            new RecipeDef("cuba_cristal", Kind.Furnace, AfCristal, t => 1.25f, (AfCristalBruto, 1)),
            new RecipeDef("fornalha_runica", Kind.Furnace, AfLingRunico, t => 1.25f, (AfMinRunico, 1)),
            new RecipeDef("fornalha_liga", Kind.Furnace, AfLiga, t => 1.5f, (Lingote, 2)),
            new RecipeDef("altar_lanternas", "altar_cristal", Kind.Crafter, AfLanterna, t => 1f, (Lingote, 1), (AfCristal, 1)),
            new RecipeDef("altar_aneis", "altar_cristal", Kind.Crafter, AfAnel, t => 0.8f, (AfCristal, 2)),
            new RecipeDef("altar_amuletos", "altar_cristal", Kind.Crafter, AfAmuleto, t => 1.2f, (AfLingRunico, 1), (AfCristal, 1)),
            new RecipeDef("cadinho_celeste", "altar_cristal", Kind.Crafter, AfCeleste, t => 2f, (AfLingRunico, 1), (AfCristal, 1), (AfLiga, 1)),
            new RecipeDef("altar_laminas", "altar_runa", Kind.Crafter, AfLamina, t => 1.4f, (AfLingRunico, 2), (Lingote, 1)),
            new RecipeDef("altar_escudos", "altar_runa", Kind.Crafter, AfEscudo, t => 1.8f, (AfLingRunico, 2), (Lingote, 2)),
            new RecipeDef("altar_cajados", "altar_runa", Kind.Crafter, AfCajado, t => 1.6f, (AfLiga, 1), (AfCristal, 1), (AfLingRunico, 1)),
        };

        const string MestraArcane = "bp_mestra_arcane", AltarCristal = "altar_cristal", AltarRuna = "altar_runa";

        /// <summary>§5.5 na ordem do tier (= indice; o save grava o ID); luxo; niveis do §5.7 (altar_cristal e altar_runa II-V, IV-V com o
        /// projeto de mestre). Efeitos genericos: o fole (fornalha, cubas, fornos runicos e a fornalha de liga, que sao fornalhas) e o foco de
        /// cristal (os altares, que sao bancadas: o "Martelo veloz"); os condensadores mexem no dreno (Sim.Dreno, pelo ID). Como na Row e no
        /// Arsenal, o nivel II pede o ultimo produtivo (Cuba 3): os niveis sao o ralo do rabo do distrito, fora da producao completa.</summary>
        static readonly UpgradeDef[] UpgradesArcane =
        {
            U(0, "fole_arcano", -1, "Fole arcano", "Fornalhas e cubas mais rápidas", menu: true, efeito: Upgrade.FurnaceSpeed1),
            U(1, "altar_aneis", -1, "Anéis de cristal", "Nova linha: anel de cristal"),
            U(2, "runa1", -1, "Aprendiz 1", "Leva minério e cristal bruto"),
            U(3, "cuba2", -1, "Cuba 2", "Dobra os cristais"),
            U(4, "runa2", 2, "Aprendiz 2", "Leva lingotes e cristais"),
            U(5, "condensador", -1, "Condensador", "Medidor dos altares cai mais rápido", menu: true),
            U(6, "veio_runico", -1, "Veio rúnico", "Minério rúnico e amuleto"),
            U(7, "altar_laminas", 6, "Lâminas rúnicas", "Nova linha: lâmina rúnica"),
            U(8, "fornalha_runica2", 6, "Forno rúnico 2", "Dobra os lingotes rúnicos"),
            U(9, "runa3", 4, "Aprendiz 3", "Leva produtos ao balcão"),
            U(10, "fornalha_liga_arcana", 6, "Liga arcana", "Liga e nova linha: cajado"),
            U(11, "altar_escudos", 7, "Escudos rúnicos", "Nova linha: escudo rúnico"),
            U(12, "fole_arcano_duplo", 0, "Fole duplo", "Fornalhas e cubas ainda mais rápidas", menu: true, efeito: Upgrade.FurnaceSpeed2),
            U(13, "cadinho_celeste", 10, "Liga celeste", "Nova linha: liga celeste"),
            U(14, "runa4", 9, "Aprendiz 4", "Leva rúnico e liga aos altares"),
            U(15, "condensador2", 5, "Condensador 2", "Medidor cai ainda mais rápido", menu: true),
            U(16, "foco_cristal", -1, "Foco de cristal", "Altares mais rápidos", menu: true, efeito: Upgrade.HammerSpeed),
            U(17, "cuba3", 3, "Cuba 3", "Mais cristais"),
            U(18, "vitrais", 17, "Vitrais", "Vitrais na fundição", luxo: true),
            U(19, "runas_piso", 18, "Runas no piso", "Runas acesas no chão", luxo: true),
            U(20, "observatorio", 19, "Observatório", "O observatório da Torre", luxo: true),
            U(21, "nivel_altar_cristal2", 17, "Cristal II", "Mais qualidade e velocidade", menu: true, station: AltarCristal),
            U(22, "nivel_altar_cristal3", 21, "Cristal III", "Mais qualidade e velocidade", menu: true, station: AltarCristal),
            U(23, "nivel_altar_cristal4", 22, "Cristal IV", "Mais qualidade e velocidade", menu: true, station: AltarCristal, blueprint: MestraArcane),
            U(24, "nivel_altar_cristal5", 23, "Cristal V", "Mais qualidade e velocidade", menu: true, station: AltarCristal, blueprint: MestraArcane),
            U(25, "nivel_altar_runa2", 17, "Runa II", "Mais qualidade e velocidade", menu: true, station: AltarRuna),
            U(26, "nivel_altar_runa3", 25, "Runa III", "Mais qualidade e velocidade", menu: true, station: AltarRuna),
            U(27, "nivel_altar_runa4", 26, "Runa IV", "Mais qualidade e velocidade", menu: true, station: AltarRuna, blueprint: MestraArcane),
            U(28, "nivel_altar_runa5", 27, "Runa V", "Mais qualidade e velocidade", menu: true, station: AltarRuna, blueprint: MestraArcane),
        };

        /// <summary>CONTEUDO_V1 §5.5 (produtivos 13.000 ... 1.100.000 = 4.789.000), o luxo e os niveis do §5.7 (110.000/320.000/850.000/1.900.000
        /// por familia) como no catalogo. BALANCE.md §40 mede.</summary>
        static readonly long[] CustosArcane =
        {
            13000, 17000, 22000, 28000, 37000, 48000, 63000, 81000, 110000, 140000, 180000, 230000, 300000, 390000, 510000, 660000, 860000, 1100000,   // produtivos
            1100000, 1500000, 1800000,                                                                                                            // luxo
            110000, 320000, 850000, 1900000, 110000, 320000, 850000, 1900000,                                                                     // niveis (Meta)
        };

        static readonly long[] TurmaAprendizes = { 20000, 32000, 52000, 83000 };   // §6: niveis 2-5 da turma (como os Cadetes do Arsenal)
        static WorkerDef Aprendiz(int n, int rota) => new WorkerDef("wk_aprendiz" + n, "Aprendizes", WorkerRole.Porter, Specialty.Carrying, ArcaneFoundryId, null,
            "runa" + n, rota, 0, null, TurmaAprendizes, "ajudante", "velocidade", "carga");

        /// <summary>§6: Isis, a oficial arcana (Smith, especialidade Arcane): o oficial de sempre (q +15, +25 no nivel 5, e o passo) em TODO altar
        /// das duas familias (Equipe.Oficial: Station null + Specialty.Arcane) e o dreno dos altares +1/s, +0,5 nos niveis 3, 5, 7 e 9 (Sim.Dreno).
        /// Depois das laminas runicas. Niveis do catalogo (48.000 x 1,6^(L-2), 2 algarismos para baixo, como a lista do §6).</summary>
        static readonly WorkerDef Isis = new WorkerDef("wk_isis", "Ísis", WorkerRole.Smith, Specialty.Arcane, ArcaneFoundryId, null, null, -1,
            190000, "altar_laminas", new long[] { 48000, 76000, 120000, 190000, 310000, 500000, 800000, 1200000, 2000000 }, "oficial", "dreno", "qualidade");

        /// <summary>§12: as familias da Foundry (Sim.Clientes). Viajante pede lanterna e anel (1 un, x1,0); aventureiro as pecas runicas e o anel
        /// (1 un, paciencia x0,9); nobre o amuleto, a liga celeste e o cajado (rico: x1,8 de paciencia, x2,5 de intervalo); o colecionador (todo
        /// distrito, Renome 2) qualquer produto na Fina.</summary>
        static readonly ClientDef[] ClientesArcane =
        {
            new ClientDef("viajante", 1, 1f, 1f, Quality.Normal, false, "lanterna_arcana", "anel_cristal"),
            new ClientDef("aventureiro", 1, 0.9f, 1f, Quality.Normal, false, "lamina_runica", "escudo_runico", "anel_cristal"),
            new ClientDef("nobre", 1, 1.8f, 2.5f, Quality.Normal, true, "amuleto_arcano", "liga_celeste", "cajado_liga"),
            new ClientDef("colecionador", 1, 2f, 3f, Quality.Fine, true),
        };

        // Arte das estacoes (Tripo, ASSETS.md §20): as 3 cubas dividem a folha cuba_cristal e os 2 fornos runicos a fornalha_runica; a fornalha
        // de liga segue com a da Row

        public static readonly DistrictDef ArcaneFoundry = new DistrictDef
        {
            Id = ArcaneFoundryId, Nome = "Arcane Foundry", Indice = 4,
            Itens = ItensArcane, Receitas = ReceitasArcane, Upgrades = UpgradesArcane, Projetos = ProjetosDefs.ArcaneDefs,
            Masterworks = MasterworksDefs.ArcaneDefs, MarcosColecao = MasterworksDefs.MarcosArcane,
            Clientes = ClientesArcane,
            // ponytail: a turma Aprendizes (§6) sao 4 Porters, como os Cadetes; o 4o leva os insumos (runico e liga aos altares: papel 1)
            Workers = new[] { Aprendiz(1, 0), Aprendiz(2, 1), Aprendiz(3, 2), Aprendiz(4, 1), Isis },
            // Fileiras (§2.5; entrada -> saida de baixo para cima, colunas de 3 m, a grade do Arsenal): depositos 1 -> fornalha, cubas e fornos
            // runicos 4 -> fornalha de liga e cuba 3 6,5 -> altares de cristal 9 -> altares runicos 11 -> balcao 13. O kit (as 6 primeiras) e' o do
            // §2.5: deposito de minerio, deposito de cristal bruto, fornalha, cuba de cristal, altar de lanternas e balcao
            Estacoes = new[]
            {
                new StationDef("deposito", Kind.Deposit, 1.5f, 1.0f, "Depósito", 0f, 1f),
                new StationDef("deposito_cristal", Kind.Deposit, 4.5f, 1.0f, "Cristal bruto", 0f, 1f, null, null, AfCristalBruto, arte: "deposito_cristal"),
                new StationDef("fornalha", Kind.Furnace, 1.5f, 4.0f, "Fornalha", -1f, 0f, null, "fornalha"),
                new StationDef("cuba_cristal", Kind.Furnace, 4.5f, 4.0f, "Cuba de cristal", -1f, 0f, null, "cuba_cristal", arte: "cuba_cristal"),
                new StationDef("altar_lanternas", Kind.Crafter, 1.5f, 9.0f, "Altar lanternas", -1f, 0f, null, "altar_lanternas", arte: "altar_lanternas"),
                new StationDef("balcao", Kind.Counter, 4.5f, 13f, "Balcão", 0f, -1f, arte: "balcao"),
                new StationDef("altar_aneis", Kind.Crafter, 4.5f, 9.0f, "Altar de anéis", -1f, 0f, "altar_aneis", "altar_aneis", arte: "altar_aneis"),
                new StationDef("cuba2", Kind.Furnace, 7.5f, 4.0f, "Cuba 2", -1f, 0f, "cuba2", "cuba_cristal", arte: "cuba_cristal"),
                new StationDef("deposito_runico", Kind.Deposit, 10.5f, 1.0f, "Veio rúnico", 0f, 1f, "veio_runico", null, AfMinRunico, arte: "deposito_runico"),
                new StationDef("fornalha_runica", Kind.Furnace, 10.5f, 4.0f, "Forno rúnico", -1f, 0f, "veio_runico", "fornalha_runica", arte: "fornalha_runica"),
                new StationDef("altar_amuletos", Kind.Crafter, 7.5f, 9.0f, "Altar amuletos", -1f, 0f, "veio_runico", "altar_amuletos", arte: "altar_amuletos"),
                new StationDef("altar_laminas", Kind.Crafter, 1.5f, 11.0f, "Altar lâminas", -1f, 0f, "altar_laminas", "altar_laminas", arte: "altar_laminas"),
                new StationDef("fornalha_runica2", Kind.Furnace, 13.5f, 4.0f, "Forno rúnico 2", -1f, 0f, "fornalha_runica2", "fornalha_runica", arte: "fornalha_runica"),
                new StationDef("fornalha_liga", Kind.Furnace, 1.5f, 6.5f, "Fornalha liga", -1f, 0f, "fornalha_liga_arcana", "fornalha_liga", arte: FornalhaLiga),
                new StationDef("altar_cajados", Kind.Crafter, 7.5f, 11.0f, "Altar cajados", -1f, 0f, "fornalha_liga_arcana", "altar_cajados", arte: "altar_cajados"),
                new StationDef("altar_escudos", Kind.Crafter, 4.5f, 11.0f, "Altar escudos", -1f, 0f, "altar_escudos", "altar_escudos", arte: "altar_escudos"),
                new StationDef("cadinho_celeste", Kind.Crafter, 10.5f, 9.0f, "Cadinho celeste", -1f, 0f, "cadinho_celeste", "cadinho_celeste", arte: "cadinho_celeste"),
                new StationDef("cuba3", Kind.Furnace, 7.5f, 6.5f, "Cuba 3", -1f, 0f, "cuba3", "cuba_cristal", arte: "cuba_cristal"),
            },
            Pads = new[]
            {
                new PadDef(4.5f, 9.0f, "altar_aneis"),
                new PadDef(7.5f, 1.4f, "runa1", "runa2", "runa3", "runa4"),
                new PadDef(7.5f, 4.0f, "cuba2"),
                new PadDef(10.5f, 4.0f, "veio_runico"),
                new PadDef(1.5f, 11.0f, "altar_laminas"),
                new PadDef(13.5f, 4.0f, "fornalha_runica2"),
                new PadDef(1.5f, 6.5f, "fornalha_liga_arcana"),
                new PadDef(4.5f, 11.0f, "altar_escudos"),
                new PadDef(10.5f, 9.0f, "cadinho_celeste"),
                new PadDef(7.5f, 6.5f, "cuba3"),
                new PadDef(13.4f, 7.2f, "vitrais", "runas_piso", "observatorio"),
            },
            Obstaculos = new ObstacleDef[0], Marcos = new MilestoneDef[0],
            Inicio = new V2(4.5f, 2.6f), Contratacao = new V2(7.5f, 1.4f), Largura = Balance.WorldW, Parede = false,
            Ajuste = t =>
            {
                // §3 precos (Normal), a relacao entre os produtos do catalogo. BALANCE.md §40 mede o fator
                t.Price = new[] { 0, 0, 3900, 3900, 4680, 6240, 7800, 8060, 8580, 0, 0, 0, 0, 0 };
                // s entre clientes no inicio = 60 / (un/min do modelo na producao completa, §1 "mix"), como a Row e o Arsenal
                t.ClientInterval = new[] { 0f, 0f, 8.1f, 2.6f, 4.4f, 11.3f, 14.6f, 13.0f, 16.2f, 0f, 0f, 0f, 0f, 0f };
                t.CustoTabela = CustosArcane;
                t.QueueCap0 = 4; t.CounterCap0 = t.CounterCap1 = 10;   // decidido por padrao (o §2.5 nao diz): o balcao do Arsenal
                t.OrderRewardMin = 2025;   // §7.2: Common da Foundry, 2.025 de minimo (x81 o do Old Forge)
                // §2.5 regra 5: carga por peca (lanterna 15, anel 12, amuleto 18, lamina 20, escudo 25, cajado 22, liga celeste 30)
                t.CargaArcana = new[] { 0, 0, 15, 12, 18, 20, 25, 22, 30, 0, 0, 0, 0, 0 };
            },
            Abertura = 3900000,
            // §2.5: producao completa + Renome 8 (3.000) + 4 Masterworks + (2 Royal Contracts OU 20 encomendas Royal no distrito); o proximo e' a
            // Grand Forge (C-CORE-09)
            Charter = new CharterDef(true, 3000, 4, GrandForgeId, contratos: 2),
            InsumoPorEspera = true,   // o do Arsenal: 4 insumos (lingote, cristal, runico, liga) para o papel 1 (BALANCE.md §40)
        };
    }

    public static partial class OrderDefs
    {
        /// <summary>C-CORE-08 (CONTEUDO_V1 §7.2): os 7 tipos da Foundry, a variante Superior do Quality (Renome 8, RenomeDefs) e a Arquimaga Zelia
        /// (3 cajados Fina). "abre: distrito aberto". Metodo, nao campo (como o OrdensArsenal): o All le daqui e a ordem dos inicializadores entre
        /// arquivos de uma classe parcial nao e' garantida.</summary>
        static OrderDef[] OrdensArcane() => new[]
        {
            new OrderDef("ord_common_arcane_foundry", Common, FS.Core.Distritos.ArcaneFoundryId, 150f, 3, 30, 10f, 2025, 1),
            new OrderDef("ord_bulk_arcane_foundry", Bulk, FS.Core.Distritos.ArcaneFoundryId, 400f, 8, 80, 22f, 4860, 3),
            new OrderDef("ord_quality_arcane_foundry", Qual, FS.Core.Distritos.ArcaneFoundryId, 90f, 3, 20, 13f, 3240, 4, minQuality: Fina),
            new OrderDef("ord_quality_arcane_foundry_superior", Qual, FS.Core.Distritos.ArcaneFoundryId, 90f, 3, 20, 21f, 6075, 4, minQuality: Superior),
            new OrderDef("ord_rush_arcane_foundry", Rush, FS.Core.Distritos.ArcaneFoundryId, 150f, 3, 30, 10f, 2025, 1, bonusDeadline: 240f, bonusRewardSeconds: 10f, bonusReputation: 1),
            new OrderDef("ord_mixed_arcane_foundry", Mixed, FS.Core.Distritos.ArcaneFoundryId, 70f, 2, 15, 18f, 4860, 3, products: 3, minLines: 3),
            new OrderDef("ord_royal_arcane_foundry", Royal, FS.Core.Distritos.ArcaneFoundryId, 240f, 5, 40, 32f, 12150, 10, minQuality: Fina, blueprintQueue: FilaRoyal),
            new OrderDef("ord_blueprint_arcane_foundry", Blueprint, FS.Core.Distritos.ArcaneFoundryId, 150f, 3, 30, 5f, 2025, 2, blueprintQueue: FilaEncomenda),
            new OrderDef("ord_reg_zelia", Qual, FS.Core.Distritos.ArcaneFoundryId, 0f, 3, 3, 13f, 3240, 3, minQuality: Fina, item: "cajado_liga", fixedN: 3, clientId: "cli_reg_zelia"),
        };

        /// <summary>§7.1: Quality x3 na Foundry (a peca energizada).</summary>
        static OrderType[] CicloArcane() => new[] { Qual, Royal, Qual, Bulk, Mixed, Rush, Qual, Blueprint, Common, Royal };
    }

    public static partial class ProjetosDefs
    {
        static BlueprintDef Af(string id, BlueprintKind kind, string alvo, bool essencial, params BlueprintSource[] fontes) =>
            new BlueprintDef(id, FS.Core.Distritos.ArcaneFoundryId, kind, alvo.Split(','), essencial, fontes);
        static BlueprintSource FilaAf(int n) => new BlueprintSource(BlueprintSourceKind.OrderBlueprint, FS.Core.Distritos.ArcaneFoundryId, n);
        static BlueprintSource RoyalAf(int n) => new BlueprintSource(BlueprintSourceKind.OrderRoyal, FS.Core.Distritos.ArcaneFoundryId, n);

        /// <summary>
        /// C-CORE-08: os 14 da Arcane Foundry (CONTEUDO_V1 §9), na ordem do catalogo. Essenciais com fonte gratis permanente: o de mestre (L4-L5
        /// das duas familias: fila Royal #1 ou a producao completa) e os projetos das 4 obras que o Charter pede (lanterna eterna, anel das
        /// estrelas, amuleto do oraculo, lamina da tempestade). A aura das lanternas (Variant) vem das 500 pecas energizadas (feitas: o contador
        /// do nucleo, BlueprintSource.Energizadas). ponytail (como o Arsenal): a 2a fonte do bp_mw_amuleto_mare (Event: ev_arcane_week, que o
        /// build ainda nao tem como evento) fica de fora; fica a fila Royal #4.
        /// </summary>
        public static readonly BlueprintDef[] ArcaneDefs =
        {
            Af("bp_tecnica_cristal", BlueprintKind.Technique, "altar_cristal", false, FilaAf(1)),
            Af("bp_tecnica_runa", BlueprintKind.Technique, "altar_runa", false, FilaAf(2)),
            Af("bp_mestra_arcane", BlueprintKind.StationLevel, "altar_cristal,altar_runa", true, RoyalAf(1), Producao()),
            Af("bp_var_aura", BlueprintKind.Variant, "lanterna_arcana", false, new BlueprintSource(BlueprintSourceKind.Milestone, BlueprintSource.Energizadas, 500)),
            Af("bp_mw_lanterna_eterna", BlueprintKind.Masterwork, "mw_lanterna_eterna", true, Vendidos("lanterna_arcana", 150)),
            Af("bp_mw_anel_estrelas", BlueprintKind.Masterwork, "mw_anel_estrelas", true, FilaAf(3), Vendidos("anel_cristal", 300)),
            Af("bp_mw_amuleto_oraculo", BlueprintKind.Masterwork, "mw_amuleto_oraculo", true, RoyalAf(2), Vendidos("amuleto_arcano", 200)),
            Af("bp_mw_lamina_tempestade", BlueprintKind.Masterwork, "mw_lamina_tempestade", true, FilaAf(4), Vendidos("lamina_runica", 150)),
            Af("bp_mw_escudo_runas", BlueprintKind.Masterwork, "mw_escudo_runas", false, RoyalAf(3)),
            Af("bp_mw_cajado_arcano", BlueprintKind.Masterwork, "mw_cajado_arcano", false, Vendidos("cajado_liga", 150)),
            Af("bp_mw_liga_primordial", BlueprintKind.Masterwork, "mw_liga_primordial", false, FilaAf(5)),
            Af("bp_mw_lamina_silencio", BlueprintKind.Masterwork, "mw_lamina_silencio", false, Vendidos("lamina_runica", 300)),
            Af("bp_mw_escudo_aurora", BlueprintKind.Masterwork, "mw_escudo_aurora", false, Renome(8)),
            Af("bp_mw_amuleto_mare", BlueprintKind.Masterwork, "mw_amuleto_mare", false, RoyalAf(4)),
        };
    }

    public static partial class MasterworksDefs
    {
        static CondicaoMasterwork Energizadas(string item, int n) => new CondicaoMasterwork(CondicaoMw.PecasEnergizadas, item, n);
        static CondicaoMasterwork Sequencia(string altar, int n) => new CondicaoMasterwork(CondicaoMw.SequenciaEstavel, altar, n);
        static MasterworkDef Af(string id, string familia, int renome, int pedestal, params CondicaoMasterwork[] c) =>
            new MasterworkDef(id, Distritos.ArcaneFoundryId, familia, "bp_" + id, renome, pedestal, c);

        /// <summary>C-CORE-08: as 10 da Arcane Foundry (CONTEUDO_V1 §10.1), DistrictDef.Masterworks dela. Superior alcancavel pelo §10.2: altares
        /// = niveis 60 + tecnica 20 + energizada 20 + Isis 25 = 125. Condicoes novas: EnergizedPieces (PecasEnergizadas) e StableStreak
        /// (SequenciaEstavel, pelo ID do altar). O Charter pede 4: as essenciais sao a lanterna eterna, o anel das estrelas (100 energizadas
        /// seguidas sem sobrecarga no altar de aneis), o amuleto do oraculo (100 amuletos energizados) e a lamina da tempestade (runa nivel V).</summary>
        public static readonly MasterworkDef[] ArcaneDefs =
        {
            Af("mw_lanterna_eterna", "lanterna_arcana", 45, 1),
            Af("mw_anel_estrelas", "anel_cristal", 50, 2, Sequencia("altar_aneis", 100)),
            Af("mw_amuleto_oraculo", "amuleto_arcano", 50, 3, Energizadas("amuleto_arcano", 100)),
            Af("mw_lamina_tempestade", "lamina_runica", 50, 4, Nivel("altar_runa", 5)),
            Af("mw_escudo_runas", "escudo_runico", 55, 5, Vendidos("escudo_runico", 500)),
            Af("mw_cajado_arcano", "cajado_liga", 55, 6, Worker("wk_isis", 5)),
            Af("mw_liga_primordial", "liga_celeste", 55, 7, Energizadas("liga_celeste", 200)),
            Af("mw_lamina_silencio", "lamina_runica", 60, 8, Sequencia("altar_laminas", 300)),
            Af("mw_escudo_aurora", "escudo_runico", 70, 9, Renome(9)),
            Af("mw_amuleto_mare", "amuleto_arcano", 60, 10, EncomendasTipo("quality", 20)),
        };

        /// <summary>C-CORE-08: os marcos da Collection da Foundry, os degraus do Quarter com as contas dela (Produtos 21 = 7 x 3; Workers 10 = 5 x
        /// contratado/nivel maximo; Blueprints 14; as 10 obras = o col_mw_distrito, so o estandarte). Renome 5/10 como os outros.</summary>
        public static readonly MarcoColecaoDef[] MarcosArcane =
        {
            new MarcoColecaoDef("col_produtos_arcane_foundry_50", AbaColecao.Produtos, 11, 5),
            new MarcoColecaoDef("col_produtos_arcane_foundry_100", AbaColecao.Produtos, 21, 10),
            new MarcoColecaoDef("col_mw_distrito_arcane_foundry", AbaColecao.Masterworks, 10, 0, "cos_col_estandarte_arcane_foundry"),
            new MarcoColecaoDef("col_workers_arcane_foundry_50", AbaColecao.Workers, 5, 5),
            new MarcoColecaoDef("col_workers_arcane_foundry_100", AbaColecao.Workers, 10, 10),
            new MarcoColecaoDef("col_bp_arcane_foundry_50", AbaColecao.Blueprints, 7, 5),
            new MarcoColecaoDef("col_bp_arcane_foundry_100", AbaColecao.Blueprints, 14, 10),
        };
    }
}
