using System;

namespace FS.Core
{
    /// <summary>
    /// C-CORE-07 (CONTEUDO_V1 §2.4, §3, §3.1, §4, §5.4, §5.7, §6): o Royal Arsenal (distrito 4) em dados. Mecanica nova: o patio de cotas
    /// (Sim.Cotas.cs). Handles: 0 e 1 = minerio e lingote (papeis 0/1 dos ajudantes), produtos de 2 a 7 (o 2, kit do batedor, sai do kit: 1o
    /// cliente e VIP), insumos depois.
    /// </summary>
    public static partial class Distritos
    {
        public const string RoyalArsenalId = "royal_arsenal";

        const Item KitBatedor = (Item)2, KitGuarda = (Item)3, Insignia = (Item)4, EspadaGala = (Item)5, Alabarda = (Item)6, ArmaduraGala = (Item)7,
            Lamina = (Item)8, PlacaRA = (Item)9, LingoteOuro = (Item)10, CouroRA = (Item)11, MinerioOuro = (Item)12;

        static readonly ItemDef[] ItensArsenal =
        {
            new ItemDef("minerio", "minério", 1, false),
            new ItemDef("lingote", "lingote", 1, false),
            new ItemDef("kit_batedor", "kit do batedor", 4, true),
            new ItemDef("kit_guarda", "kit da guarda", 4, true),
            new ItemDef("insignia_real", "insígnia real", 4, true),
            new ItemDef("espada_gala", "espada de gala", 4, true),
            new ItemDef("alabarda_cerimonial", "alabarda de gala", 4, true),
            new ItemDef("armadura_gala", "armadura de gala", 4, true),
            new ItemDef("lamina", "lâmina", 4, false),
            new ItemDef("placa", "placa", 2, false),
            new ItemDef("lingote_ouro", "lingote de ouro", 3, false),
            new ItemDef("couro", "couro", 2, false),
            new ItemDef("minerio_ouro", "minério de ouro", 3, false),
        };

        /// <summary>§3.1: TimeMul = tempo base / tempo inicial do tipo (fornalha 4 s, bancada 5 s). Kits e cerimonial sao as familias de nivel e
        /// qualidade (§4); bancada de varios componentes = a montagem da Row (so comeca com a receita inteira).</summary>
        static readonly RecipeDef[] ReceitasArsenal =
        {
            new RecipeDef("fornalha", Kind.Furnace, Lingote, t => 1f, (Minerio, 1)),
            new RecipeDef("forja_laminas", Kind.Furnace, Lamina, t => 0.75f, (Lingote, 1)),
            new RecipeDef("prensa", Kind.Furnace, PlacaRA, t => 0.75f, (Lingote, 1)),
            new RecipeDef("crisol_ouro", Kind.Furnace, LingoteOuro, t => 1f, (MinerioOuro, 1)),
            new RecipeDef("bancada_kits", "kits", Kind.Crafter, KitBatedor, t => 1.2f, (Lamina, 1), (CouroRA, 2)),
            new RecipeDef("bancada_guarda", "kits", Kind.Crafter, KitGuarda, t => 1.6f, (Lamina, 1), (PlacaRA, 2), (CouroRA, 1)),
            new RecipeDef("mesa_gravador", "cerimonial", Kind.Crafter, Insignia, t => 1f, (LingoteOuro, 1), (CouroRA, 1)),
            new RecipeDef("forja_cerimonial", "cerimonial", Kind.Crafter, EspadaGala, t => 1.4f, (Lamina, 1), (LingoteOuro, 1)),
            new RecipeDef("forja_alabardas", "cerimonial", Kind.Crafter, Alabarda, t => 1.6f, (Lingote, 2), (LingoteOuro, 1)),
            new RecipeDef("montagem_gala", "cerimonial", Kind.Crafter, ArmaduraGala, t => 2.4f, (PlacaRA, 3), (LingoteOuro, 2), (CouroRA, 1)),
        };

        const string MestraArsenal = "bp_mestra_arsenal";

        /// <summary>§5.4 na ordem do tier (= indice; o save grava o ID); luxo; niveis do §5.7 (kits e cerimonial II-V, IV-V com o projeto de
        /// mestre); o selo (Meta, q +25 no distrito). Como na Row (BALANCE.md §35 desvio 1), o nivel II e o selo pedem o ultimo produtivo
        /// (Fornalha 3): sao o ralo do rabo do distrito, fora da producao completa.</summary>
        static readonly UpgradeDef[] UpgradesArsenal =
        {
            U(0, "fole_real", -1, "Fole real", "Fornalhas mais rápidas", menu: true, efeito: Upgrade.FurnaceSpeed1),
            U(1, "bancada_kits2", -1, "Bancada kits 2", "Mais kits do batedor"),
            U(2, "cadete1", -1, "Cadete 1", "Leva minério às fornalhas"),
            U(3, "bancada_guarda", -1, "Kits da guarda", "Prensa e kit da guarda"),
            U(4, "cadete2", 2, "Cadete 2", "Leva lingotes e lâminas"),
            U(5, "patio_faixa2", -1, "Faixa do pátio 2", "2 produtos no pátio"),
            U(6, "crisol_real", -1, "Crisol real", "Ouro e nova linha: insígnia"),
            U(7, "forja_cerimonial", 6, "Espadas de gala", "Nova linha: espada de gala"),
            U(8, "fornalha_real2", -1, "Fornalha 2", "Dobra os lingotes"),
            U(9, "cadete3", 4, "Cadete 3", "Leva produtos ao pátio"),
            U(10, "forja_alabardas", 6, "Alabardas", "Nova linha: alabarda"),
            U(11, "mesa_real", 5, "Mesa real", "2ª vaga de contrato"),
            U(12, "montagem_gala", 7, "Armaduras gala", "Nova linha: armadura"),
            U(13, "fole_real_duplo", 0, "Fole duplo", "Fornalhas ainda mais rápidas", menu: true, efeito: Upgrade.FurnaceSpeed2),
            U(14, "cadete4", 9, "Cadete 4", "Mais um leva minério e couro"),
            U(15, "ferragens_reais", -1, "Ferragens reais", "Bancadas e forjas mais rápidas", menu: true, efeito: Upgrade.HammerSpeed),
            U(16, "patio_faixa3", 5, "Faixa do pátio 3", "3 produtos no pátio"),
            U(17, "fornalha_real3", 8, "Fornalha 3", "Mais lingotes"),
            U(18, "flamulas_reais", 17, "Flâmulas reais", "Flâmulas no pátio", luxo: true),
            U(19, "portao_real", 18, "Portão real", "Portão de honra", luxo: true),
            U(20, "torre_relogio", 19, "Torre do relógio", "O relógio do Arsenal", luxo: true),
            U(21, "nivel_kits2", 17, "Kits II", "Mais qualidade e velocidade", menu: true, station: "kits"),
            U(22, "nivel_kits3", 21, "Kits III", "Mais qualidade e velocidade", menu: true, station: "kits"),
            U(23, "nivel_kits4", 22, "Kits IV", "Mais qualidade e velocidade", menu: true, station: "kits", blueprint: MestraArsenal),
            U(24, "nivel_kits5", 23, "Kits V", "Mais qualidade e velocidade", menu: true, station: "kits", blueprint: MestraArsenal),
            U(25, "nivel_cerimonial2", 17, "Cerimonial II", "Mais qualidade e velocidade", menu: true, station: "cerimonial"),
            U(26, "nivel_cerimonial3", 25, "Cerimonial III", "Mais qualidade e velocidade", menu: true, station: "cerimonial"),
            U(27, "nivel_cerimonial4", 26, "Cerimonial IV", "Mais qualidade e velocidade", menu: true, station: "cerimonial", blueprint: MestraArsenal),
            U(28, "nivel_cerimonial5", 27, "Cerimonial V", "Mais qualidade e velocidade", menu: true, station: "cerimonial", blueprint: MestraArsenal),
            new UpgradeDef((Upgrade)29, "selo_arsenal", 17, "Selo do Arsenal", "Qualidade em todo o distrito", menu: true, meta: true, efeito: Nenhum),
        };

        /// <summary>BALANCE.md §38: os produtivos do §5.4 x 1,35 (multiplo de 5 ate 10 mil, 2 algarismos acima; 1.600.725 -> 2.155.265), o desvio
        /// da Row (x 1,55; V28); o luxo, os niveis do §5.7 e o selo (110.000) como no catalogo.</summary>
        static readonly long[] CustosArsenal =
        {
            5830, 7580, 9855, 13000, 16000, 22000, 28000, 36000, 47000, 62000, 81000, 100000, 140000, 180000, 230000, 300000, 390000, 500000,   // produtivos
            380000, 490000, 590000,                                                                                                          // luxo
            32000, 95000, 250000, 570000, 39000, 120000, 310000, 700000,                                                                     // niveis (Meta)
            110000,                                                                                                                          // selo (Meta)
        };

        static readonly long[] TurmaCadetes = { 6750, 11000, 17000, 28000 };   // §6: niveis 2-5 da turma (como a Ferraz da Row)
        static WorkerDef Cadete(int n, int rota) => new WorkerDef("wk_cadete" + n, "Cadetes", WorkerRole.Porter, Specialty.Carrying, RoyalArsenalId, null,
            "cadete" + n, rota, 0, null, TurmaCadetes, "ajudante", "velocidade", "carga");

        /// <summary>§6: o Intendente Matias (Quartermaster; "Matias" no cartao, o nome do catalogo passa de 16): saidas +2 e estoque como o
        /// almoxarife do Old Forge e, no patio, o prazo-bonus dos blocos 180 -> 240 s (Sim.CotaPrazo). Depois da 2a faixa do patio.</summary>
        static readonly WorkerDef Matias = new WorkerDef("wk_matias", "Matias", WorkerRole.Quartermaster, Specialty.None, RoyalArsenalId, null, null, -1,
            68000, "patio_faixa2", new long[] { 17000, 27000, 44000, 70000, 110000, 180000, 290000, 460000, 730000 }, "almoxarife", "prazo", "saida");

        /// <summary>§12: as familias do Arsenal (Sim.Clientes, as do Quarter). Guarda pede os kits (1 un, x1,0); nobre a insignia e a alabarda
        /// (rico, x1,8 de paciencia, x2,5 de intervalo); o emissario real as pecas de gala na Fina ou melhor (2 un, x2,0, x2,0) e o
        /// colecionador (todo distrito, Renome 2) qualquer produto na Fina.</summary>
        static readonly ClientDef[] ClientesArsenal =
        {
            new ClientDef("guarda", 1, 1f, 1f, Quality.Normal, false, "kit_batedor", "kit_guarda"),
            new ClientDef("nobre", 1, 1.8f, 2.5f, Quality.Normal, true, "insignia_real", "alabarda_cerimonial"),
            new ClientDef("emissario", 2, 2f, 2f, Quality.Fine, false, "espada_gala", "armadura_gala", "insignia_real"),
            new ClientDef("colecionador", 1, 2f, 3f, Quality.Fine, true),
        };

        // C-ART-13 parte 2: estacoes com folhas proprias do Tripo (ASSETS.md §15); a prensa usa a da Row, o crisol e o deposito de ouro os do
        // Quarter; o deposito de minerio e a fornalha seguem pelo tipo, e a carroca do couro e' a do Old Forge

        public static readonly DistrictDef RoyalArsenal = new DistrictDef
        {
            Id = RoyalArsenalId, Nome = "Royal Arsenal", Indice = 3,
            Itens = ItensArsenal, Receitas = ReceitasArsenal, Upgrades = UpgradesArsenal, Projetos = ProjetosDefs.ArsenalDefs,
            Masterworks = MasterworksDefs.ArsenalDefs, MarcosColecao = MasterworksDefs.MarcosArsenal,
            Clientes = ClientesArsenal,
            // ponytail: a turma Cadetes (§6) sao 4 Porters, como a Ferraz; o 4o leva o bruto (minerio, couro, ouro)
            Workers = new[] { Cadete(1, 0), Cadete(2, 1), Cadete(3, 2), Cadete(4, 0), Matias },
            // Fileiras (§2.4; entrada -> saida de baixo para cima, colunas de 3 m, a grade da Row): depositos 1 -> fornalhas 4 -> lamina e placa
            // 6,5 -> kits 9 -> cerimonial 11 -> balcao e patio 13. Entrada a esquerda; o ouro na coluna da direita (crisol, mesa do gravador)
            Estacoes = new[]
            {
                new StationDef("deposito", Kind.Deposit, 1.5f, 1.0f, "Depósito", 0f, 1f),
                new StationDef("fornalha", Kind.Furnace, 1.5f, 4.0f, "Fornalha", -1f, 0f, null, "fornalha"),
                new StationDef("forja_laminas", Kind.Furnace, 1.5f, 6.5f, "Forja lâminas", -1f, 0f, null, "forja_laminas", arte: "forja_laminas"),
                new StationDef("bancada_kits", Kind.Crafter, 1.5f, 9.0f, "Bancada kits", -1f, 0f, null, "bancada_kits", arte: "bancada_kits"),
                new StationDef("balcao", Kind.Counter, 4.5f, 13f, "Balcão", 0f, -1f),
                new StationDef("patio_cotas", Kind.Quota, 10.5f, 13f, "Pátio de cotas", 0f, -1f, arte: "patio_cotas"),
                new StationDef("carroca_couro", Kind.Deposit, 4.5f, 1.0f, "Carroça couro", 0f, 1f, null, null, CouroRA, "carroca"),
                new StationDef("bancada_kits2", Kind.Crafter, 4.5f, 9.0f, "Bancada kits 2", -1f, 0f, "bancada_kits2", "bancada_kits", arte: "bancada_kits"),
                new StationDef("prensa", Kind.Furnace, 4.5f, 6.5f, "Prensa", -1f, 0f, "bancada_guarda", "prensa", arte: "prensa"),
                new StationDef("bancada_guarda", Kind.Crafter, 7.5f, 9.0f, "Bancada guarda", -1f, 0f, "bancada_guarda", "bancada_guarda", arte: "bancada_guarda"),
                new StationDef("deposito_ouro", Kind.Deposit, 10.5f, 1.0f, "Minério ouro", 0f, 1f, "crisol_real", null, MinerioOuro, arte: "deposito_ouro"),
                new StationDef("crisol_ouro", Kind.Furnace, 10.5f, 4.0f, "Crisol ouro", -1f, 0f, "crisol_real", "crisol_ouro", arte: "crisol"),
                new StationDef("mesa_gravador", Kind.Crafter, 10.5f, 11.0f, "Mesa gravador", -1f, 0f, "crisol_real", "mesa_gravador", arte: "mesa_gravador"),
                new StationDef("forja_cerimonial", Kind.Crafter, 1.5f, 11.0f, "Forja cerimonial", -1f, 0f, "forja_cerimonial", "forja_cerimonial", arte: "forja_cerimonial"),
                new StationDef("fornalha2", Kind.Furnace, 4.5f, 4.0f, "Fornalha 2", -1f, 0f, "fornalha_real2", "fornalha"),
                new StationDef("forja_alabardas", Kind.Crafter, 4.5f, 11.0f, "Forja alabardas", -1f, 0f, "forja_alabardas", "forja_alabardas", arte: "forja_alabardas"),
                new StationDef("montagem_gala", Kind.Crafter, 7.5f, 11.0f, "Montagem gala", -1f, 0f, "montagem_gala", "montagem_gala", arte: "montagem_gala"),
                new StationDef("fornalha3", Kind.Furnace, 7.5f, 4.0f, "Fornalha 3", -1f, 0f, "fornalha_real3", "fornalha"),
            },
            Pads = new[]
            {
                new PadDef(4.5f, 9.0f, "bancada_kits2"),
                new PadDef(7.5f, 1.4f, "cadete1", "cadete2", "cadete3", "cadete4"),
                new PadDef(7.5f, 9.0f, "bancada_guarda"),
                new PadDef(13.4f, 12.4f, "patio_faixa2", "patio_faixa3"),   // ao lado do patio
                new PadDef(10.5f, 4.0f, "crisol_real"),
                new PadDef(1.5f, 11.0f, "forja_cerimonial"),
                new PadDef(4.5f, 4.0f, "fornalha_real2"),
                new PadDef(4.5f, 11.0f, "forja_alabardas"),
                new PadDef(13.4f, 9.8f, "mesa_real"),
                new PadDef(7.5f, 11.0f, "montagem_gala"),
                new PadDef(7.5f, 4.0f, "fornalha_real3"),
                new PadDef(13.4f, 7.2f, "flamulas_reais", "portao_real", "torre_relogio"),
            },
            Obstaculos = new ObstacleDef[0], Marcos = new MilestoneDef[0],
            Inicio = new V2(4.5f, 2.6f), Contratacao = new V2(7.5f, 1.4f), Largura = Balance.WorldW, Parede = false,
            Ajuste = t =>
            {
                // §3 precos (Normal) x 2,4, o desvio da Row (V28; BALANCE.md §38): o Arsenal e' preso no transporte (1 cadete para lingote, lamina,
                // placa e ouro), nao na demanda; com as familias do §12 (o rodizio divide o relogio de cada uma) e o bot pela procura delas, o x 2,8
                // de antes fazia a producao completa do F2P em 54 min (portao 55-70). A relacao entre os produtos e' a do §3. §2.4 balcao de 4
                // vagas com estoque 10 (sem Vitrine no distrito)
                t.Price = new[] { 0, 0, 1705, 2880, 1320, 1970, 2330, 4560, 0, 0, 0, 0, 0 };
                // s entre clientes no inicio = 60 / (un/min do modelo na producao completa, §1 "mix"), como a Row
                t.ClientInterval = new[] { 0f, 0f, 5.4f, 14.3f, 4.3f, 12.5f, 6.8f, 15.8f, 0f, 0f, 0f, 0f, 0f };
                t.CustoTabela = CustosArsenal;
                t.QueueCap0 = 4; t.CounterCap0 = t.CounterCap1 = 10;
                t.OrderRewardMin = 675;   // §7.2: Common do Arsenal, 675 de minimo (x27 o do Old Forge)
            },
            Abertura = 1300000,
            // §2.4: producao completa + Renome 7 (2.000) + 3 Masterworks + (1 Royal Contract OU 10 encomendas Royal no distrito); C-CORE-09: o
            // proximo e' a Grand Forge (provisorio: com a Arcane Foundry na main, o Arsenal libera a Foundry e ela a Grand Forge)
            Charter = new CharterDef(true, 2000, 3, GrandForgeId, contratos: 1),
            Selo = "selo_arsenal",
            InsumoPorEspera = true,   // BALANCE.md §38: o cadete do insumo leva primeiro o que as bancadas esperam (sem isso o ouro para no crisol)
        };
    }

    public static partial class OrderDefs
    {
        /// <summary>C-CORE-07 (CONTEUDO_V1 §7.2): "abre: distrito aberto". Mesmo desvio do Old Forge e da Row (revisar): Bulk, Quality, Mixed,
        /// Royal e o Tiberio pagam 70% dos segundos do §7.2 (32/18/25/50/18 -> 22/13/18/35/13); com o catalogo o ouro de encomendas ia a ~12,8% da
        /// venda (portao 4-12%; BALANCE.md §38). A Royal que pede Normal com a mesa_real vem ANTES da Fina (o rodizio pega a ultima aberta do
        /// tipo): com Fina na linha, a Fina; sem, a Normal, nunca a Common (§2.4 regra 6). Metodo, nao campo (como o OrdensArtisan): o All le
        /// daqui e a ordem dos inicializadores entre arquivos de uma classe parcial nao e' garantida.</summary>
        static OrderDef[] OrdensArsenal() => new[]
        {
            new OrderDef("ord_common_royal_arsenal", Common, Arsenal, 150f, 3, 30, 10f, 675, 1),
            new OrderDef("ord_bulk_royal_arsenal", Bulk, Arsenal, 400f, 8, 80, 22f, 1620, 3),
            new OrderDef("ord_quality_royal_arsenal", Qual, Arsenal, 90f, 3, 20, 13f, 1080, 3, minQuality: Fina),
            new OrderDef("ord_rush_royal_arsenal", Rush, Arsenal, 150f, 3, 30, 10f, 675, 1, bonusDeadline: 240f, bonusRewardSeconds: 10f, bonusReputation: 1),
            new OrderDef("ord_mixed_royal_arsenal", Mixed, Arsenal, 70f, 2, 15, 18f, 1620, 3, products: 3, minLines: 3),
            new OrderDef("ord_royal_royal_arsenal_mesa", Royal, Arsenal, 250f, 6, 50, 35f, 4050, 12, blueprintQueue: FilaRoyal, requer: Sim.MesaReal),
            new OrderDef("ord_royal_royal_arsenal", Royal, Arsenal, 250f, 6, 50, 35f, 4050, 12, minQuality: Fina, blueprintQueue: FilaRoyal),
            new OrderDef("ord_blueprint_royal_arsenal", Blueprint, Arsenal, 150f, 3, 30, 5f, 675, 2, blueprintQueue: FilaEncomenda),
            new OrderDef("ord_reg_tiberio", Qual, Arsenal, 0f, 6, 6, 13f, 1080, 3, minQuality: Fina, item: "insignia_real", fixedN: 6, clientId: "cli_reg_tiberio"),
        };
    }

    public static partial class ProjetosDefs
    {
        static BlueprintDef A(string id, BlueprintKind kind, string alvo, bool essencial, params BlueprintSource[] fontes) =>
            new BlueprintDef(id, FS.Core.Distritos.RoyalArsenalId, kind, alvo.Split(','), essencial, fontes);
        static BlueprintSource FilaArsenal(int n) => new BlueprintSource(BlueprintSourceKind.OrderBlueprint, FS.Core.Distritos.RoyalArsenalId, n);
        static BlueprintSource FilaRoyalArsenal(int n) => new BlueprintSource(BlueprintSourceKind.OrderRoyal, FS.Core.Distritos.RoyalArsenalId, n);

        /// <summary>
        /// C-CORE-07: os 14 do Royal Arsenal (CONTEUDO_V1 §9), na ordem do catalogo. Essenciais com fonte gratis permanente: o de mestre (L4-L5) e
        /// os projetos das 3 obras que o Charter pede (kit do capitao, kit da patrulha, alabarda da coroacao). ponytail (como a Row): a 2a fonte do
        /// bp_mw_grande_selo (Contract: ctr_guarda_real) fica de fora: o contrato e' do Imperio e da o bp_var_guarda_real; fica a fila Royal #4.
        /// O estandarte real (Variant) aponta para a alabarda de gala (o catalogo nao nomeia o item) e vem dos 50 blocos de cota fechados.
        /// </summary>
        public static readonly BlueprintDef[] ArsenalDefs =
        {
            A("bp_tecnica_kits", BlueprintKind.Technique, "kits", false, FilaArsenal(1)),
            A("bp_tecnica_cerimonial", BlueprintKind.Technique, "cerimonial", false, FilaArsenal(2)),
            A("bp_mestra_arsenal", BlueprintKind.StationLevel, "kits,cerimonial", true, FilaRoyalArsenal(1), Producao()),
            A("bp_var_estandarte_real", BlueprintKind.Variant, "alabarda_cerimonial", false, new BlueprintSource(BlueprintSourceKind.Milestone, BlueprintSource.Cotas, 50)),
            A("bp_mw_kit_capitao", BlueprintKind.Masterwork, "mw_kit_capitao", true, Vendidos("kit_guarda", 100)),
            A("bp_mw_kit_patrulha", BlueprintKind.Masterwork, "mw_kit_patrulha", true, FilaArsenal(3), Vendidos("kit_batedor", 300)),
            A("bp_mw_alabarda_coroacao", BlueprintKind.Masterwork, "mw_alabarda_coroacao", true, FilaRoyalArsenal(2)),
            A("bp_mw_espada_gala_rei", BlueprintKind.Masterwork, "mw_espada_gala_rei", false, FilaArsenal(4)),
            A("bp_mw_armadura_desfile", BlueprintKind.Masterwork, "mw_armadura_desfile", false, FilaRoyalArsenal(3)),
            A("bp_mw_insignia_leao", BlueprintKind.Masterwork, "mw_insignia_leao", false, Vendidos("insignia_real", 300)),
            A("bp_mw_kit_cerco", BlueprintKind.Masterwork, "mw_kit_cerco", false, FilaArsenal(5)),
            A("bp_mw_alabarda_sentinela", BlueprintKind.Masterwork, "mw_alabarda_sentinela", false, Vendidos("alabarda_cerimonial", 200)),
            A("bp_mw_rainha_guerreira", BlueprintKind.Masterwork, "mw_rainha_guerreira", false, Renome(7)),
            A("bp_mw_grande_selo", BlueprintKind.Masterwork, "mw_grande_selo", false, FilaRoyalArsenal(4)),
        };
    }

    public static partial class MasterworksDefs
    {
        static CondicaoMasterwork Blocos(string item, int n) => new CondicaoMasterwork(CondicaoMw.CotaBlocos, item, n);
        static CondicaoMasterwork NoPrazo(string item, int n) => new CondicaoMasterwork(CondicaoMw.CotaNoPrazo, item, n);
        static MasterworkDef A(string id, string familia, int renome, int pedestal, params CondicaoMasterwork[] c) =>
            new MasterworkDef(id, Distritos.RoyalArsenalId, familia, "bp_" + id, renome, pedestal, c);

        /// <summary>C-CORE-07: as 10 do Royal Arsenal (CONTEUDO_V1 §10.1), DistrictDef.Masterworks dele. Superior alcancavel pelo §10.2: kits e
        /// cerimonial = niveis 60 + tecnica 20 + selo_arsenal 25 = 105. O Charter pede 3: as essenciais sao o kit do capitao, o kit da patrulha
        /// (20 blocos de kit do batedor no patio) e a alabarda da coroacao (cerimonial nivel V).</summary>
        public static readonly MasterworkDef[] ArsenalDefs =
        {
            A("mw_kit_capitao", "kit_guarda", 40, 1),
            A("mw_kit_patrulha", "kit_batedor", 45, 2, Blocos("kit_batedor", 20)),
            A("mw_alabarda_coroacao", "alabarda_cerimonial", 45, 3, Nivel("cerimonial", 5)),
            A("mw_espada_gala_rei", "espada_gala", 45, 4, NoPrazo("espada_gala", 10)),
            A("mw_armadura_desfile", "armadura_gala", 50, 5, Vendidos("armadura_gala", 300)),
            A("mw_insignia_leao", "insignia_real", 45, 6, Worker("wk_matias", 5)),
            A("mw_kit_cerco", "kit_guarda", 50, 7, EncomendasTipo("royal", 15)),
            A("mw_alabarda_sentinela", "alabarda_cerimonial", 50, 8, Blocos("alabarda_cerimonial", 30)),
            A("mw_rainha_guerreira", "armadura_gala", 60, 9, Renome(8)),
            A("mw_grande_selo", "insignia_real", 70, 10, new CondicaoMasterwork(CondicaoMw.Contratos, null, 3)),
        };

        /// <summary>C-CORE-07: os marcos da Collection do Arsenal, os degraus da Row com as contas dele (Produtos 18 = 6 x 3; Workers 10 = 5 x
        /// contratado/nivel maximo; Blueprints 14; as 10 obras = o col_mw_distrito, so o estandarte). Renome 5/10 como os da Row.</summary>
        public static readonly MarcoColecaoDef[] MarcosArsenal =
        {
            new MarcoColecaoDef("col_produtos_royal_arsenal_50", AbaColecao.Produtos, 9, 5),
            new MarcoColecaoDef("col_produtos_royal_arsenal_100", AbaColecao.Produtos, 18, 10),
            new MarcoColecaoDef("col_mw_distrito_royal_arsenal", AbaColecao.Masterworks, 10, 0, "cos_col_estandarte_royal_arsenal"),
            new MarcoColecaoDef("col_workers_royal_arsenal_50", AbaColecao.Workers, 5, 5),
            new MarcoColecaoDef("col_workers_royal_arsenal_100", AbaColecao.Workers, 10, 10),
            new MarcoColecaoDef("col_bp_royal_arsenal_50", AbaColecao.Blueprints, 7, 5),
            new MarcoColecaoDef("col_bp_royal_arsenal_100", AbaColecao.Blueprints, 14, 10),
        };
    }
}
