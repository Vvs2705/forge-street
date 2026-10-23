namespace FS.Core
{
    /// <summary>
    /// C-CORE-09 (CONTEUDO_V1 §2.6, §3, §3.1, §4, §5.6, §5.7, §6, §7, §9, §10, §12): a Grand Forge (distrito 6, o ultimo) em dados. Mecanica nova:
    /// as rotas de importacao e os lotes de exportacao do Mensageiro (Sim.Exportacao.cs). Handles: 0 e 1 = minerio de cobre e barra de bronze (o
    /// bruto do kit e o que o crisol do kit faz dele, como o Quarter), produtos de 2 a 7 (o 2, sino, sai do kit: 1o cliente e VIP), os insumos
    /// do ferro e da cratera depois e os importados no fim (insumo aqui; produto no distrito de origem).
    /// Desvio do §3.1 (revisar): sem os 4 importados da Arcane Foundry (lanterna arcana, anel de cristal, lamina e escudo runicos): a raia
    /// fechou antes da Foundry entrar no build; a lamina estelar e' so aco (sem rota) e relicario, coroa e panoplia pedem os importados de antes.
    /// </summary>
    public static partial class Distritos
    {
        public const string GrandForgeId = "grand_forge";

        const Item GfMinCobre = (Item)0, GfBronze = (Item)1, GfSino = (Item)2, GfEstatua = (Item)3, GfLaminaEstelar = (Item)4, GfRelicario = (Item)5,
            GfCoroa = (Item)6, GfPanoplia = (Item)7, GfMinerio = (Item)8, GfLingote = (Item)9, GfPedra = (Item)10, GfAco = (Item)11, GfPeitoral = (Item)12,
            GfColar = (Item)13, GfCaixa = (Item)14, GfInsignia = (Item)15, GfEspadaGala = (Item)16;

        static readonly ItemDef[] ItensGrandForge =
        {
            new ItemDef("minerio_cobre", "minério de cobre", 3, false),
            new ItemDef("bronze", "barra de bronze", 5, false),
            new ItemDef("sino_catedral", "sino da catedral", 5, true),
            new ItemDef("estatua_bronze", "estátua brônzea", 5, true),
            new ItemDef("lamina_estelar", "lâmina estelar", 5, true),
            new ItemDef("relicario_real", "relicário real", 5, true),
            new ItemDef("coroa_reino", "coroa do reino", 5, true),
            new ItemDef("panoplia_campeao", "panóplia campeã", 5, true),
            new ItemDef("minerio", "minério", 1, false),
            new ItemDef("lingote", "lingote", 1, false),
            new ItemDef("pedra_estelar", "pedra estelar", 5, false),
            new ItemDef("aco_estelar", "aço estelar", 5, false),
            // os importados (rotas do Mensageiro, §3.1): o mesmo ID do distrito de origem, insumo aqui
            new ItemDef("peitoral", "peitoral", 2, false),
            new ItemDef("colar", "colar", 3, false),
            new ItemDef("caixa_joias", "caixa de joias", 3, false),
            new ItemDef("insignia_real", "insígnia real", 4, false),
            new ItemDef("espada_gala", "espada de gala", 4, false),
        };

        /// <summary>§3.1: TimeMul = tempo base / tempo inicial do tipo (crisol, fornalha e forja estelar: 4 s; fundicoes e bancadas: 5 s). Fundicao
        /// e sintese sao as familias de nivel e qualidade (§4); bancada de varios insumos so comeca com a receita inteira (a montagem da Row).</summary>
        static readonly RecipeDef[] ReceitasGrandForge =
        {
            new RecipeDef("crisol_bronze", Kind.Furnace, GfBronze, t => 0.75f, (GfMinCobre, 1)),
            new RecipeDef("fornalha", Kind.Furnace, GfLingote, t => 1f, (GfMinerio, 1)),
            new RecipeDef("forja_estelar", Kind.Furnace, GfAco, t => 1.5f, (GfLingote, 1), (GfPedra, 1)),
            new RecipeDef("fundicao_sinos", "fundicao", Kind.Crafter, GfSino, t => 3.6f, (GfBronze, 8)),
            new RecipeDef("fundicao_estatuas", "fundicao", Kind.Crafter, GfEstatua, t => 3.2f, (GfBronze, 6), (GfAco, 1)),
            new RecipeDef("forja_sintese", "sintese", Kind.Crafter, GfLaminaEstelar, t => 2f, (GfAco, 2)),
            new RecipeDef("bancada_relicarios", "sintese", Kind.Crafter, GfRelicario, t => 2.4f, (GfCaixa, 1), (GfBronze, 2)),
            new RecipeDef("bancada_coroas", "sintese", Kind.Crafter, GfCoroa, t => 2.4f, (GfColar, 1), (GfInsignia, 1)),
            new RecipeDef("bancada_panoplia", "sintese", Kind.Crafter, GfPanoplia, t => 2.8f, (GfPeitoral, 1), (GfEspadaGala, 1), (GfAco, 1)),
        };

        const string MestraGrand = "bp_mestra_grand";

        /// <summary>§5.6 na ordem do tier (= indice; o save grava o ID); luxo; niveis do §5.7 (sintese e fundicao II-V, IV-V com o projeto de mestre);
        /// a bencao (Meta, q +25 no distrito: DistrictDef.Selo). Como a Row e o Arsenal, o nivel II e a bencao pedem o ultimo produtivo (Crisol 3):
        /// sao o ralo do rabo do distrito. A forja de sintese pede o mensageiro (o §5.6 pede tambem a cratera; o UpgradeDef tem um requisito so).</summary>
        static readonly UpgradeDef[] UpgradesGrandForge =
        {
            U(0, "fole_grande", -1, "Fole da catedral", "Crisóis e fornalhas", menu: true, efeito: Upgrade.FurnaceSpeed1),
            U(1, "crisol_bronze2", -1, "Crisol 2", "Mais barras de bronze"),
            U(2, "estrela1", -1, "Estrela 1", "Leva minérios aos crisóis"),
            U(3, "cratera_estelar", -1, "Cratera estelar", "Aço e nova linha: estátua", efeito: Upgrade.Conveyor),
            U(4, "estrela2", 2, "Estrela 2", "Leva bronze e aço"),
            U(5, Sim.Mensageiro, -1, "Mensageiro", "Rotas e exportação"),
            U(6, "forja_sintese", 5, "Lâmina estelar", "Nova linha: lâmina estelar"),
            U(7, "fornalha_grande2", 3, "Fornalha 2", "Dobra os lingotes"),
            U(8, "estrela3", 4, "Estrela 3", "Leva produtos ao balcão"),
            U(9, "bancada_relicarios", 5, "Relicários", "Nova linha: relicário"),
            U(10, Sim.Cais2, 5, "Cais 2", "Rotas: +6 por minuto"),
            U(11, "bancada_coroas", 5, "Coroas", "Nova linha: coroa"),
            U(12, "fole_grande_duplo", 0, "Fole duplo", "Crisóis ainda mais rápidos", menu: true, efeito: Upgrade.FurnaceSpeed2),
            U(13, "estrela4", 8, "Estrela 4", "Mais um leva bronze e aço"),
            U(14, "bancada_panoplia", 10, "Panóplias", "Nova linha: panóplia"),
            U(15, Sim.Exportacao2, 5, "Exportação 2", "Lote de 20 a cada 60 s"),
            U(16, "martelos_catedral", -1, "Martelos mestres", "Fundições e bancadas", menu: true, efeito: Upgrade.HammerSpeed),
            U(17, "crisol_bronze3", 1, "Crisol 3", "Mais barras de bronze"),
            U(18, "vitral_forja", 17, "Vitral da forja", "Vitral na catedral", luxo: true),
            U(19, "praca_sinos", 18, "Praça dos sinos", "Sinos na praça", luxo: true),
            U(20, "estatua_heroi", 19, "Estátua do herói", "Sua estátua na praça", luxo: true),
            U(21, "nivel_sintese2", 17, "Síntese II", "Mais qualidade e velocidade", menu: true, station: "sintese"),
            U(22, "nivel_sintese3", 21, "Síntese III", "Mais qualidade e velocidade", menu: true, station: "sintese"),
            U(23, "nivel_sintese4", 22, "Síntese IV", "Mais qualidade e velocidade", menu: true, station: "sintese", blueprint: MestraGrand),
            U(24, "nivel_sintese5", 23, "Síntese V", "Mais qualidade e velocidade", menu: true, station: "sintese", blueprint: MestraGrand),
            U(25, "nivel_fundicao2", 17, "Fundição II", "Mais qualidade e velocidade", menu: true, station: "fundicao"),
            U(26, "nivel_fundicao3", 25, "Fundição III", "Mais qualidade e velocidade", menu: true, station: "fundicao"),
            U(27, "nivel_fundicao4", 26, "Fundição IV", "Mais qualidade e velocidade", menu: true, station: "fundicao", blueprint: MestraGrand),
            U(28, "nivel_fundicao5", 27, "Fundição V", "Mais qualidade e velocidade", menu: true, station: "fundicao", blueprint: MestraGrand),
            new UpgradeDef((Upgrade)29, "bencao_forja", 17, "Bênção da forja", "Qualidade em todo o distrito", menu: true, meta: true, efeito: Nenhum),
        };

        /// <summary>BALANCE.md §41: os produtivos do §5.6 x 0,9 (2 algarismos significativos, meio para cima, a regra do §1; 14.501.000 -> 13.136.000),
        /// com o preco x 0,85 (Ajuste) a producao completa do F2P fica em ~1 h com o fundo da cadeia inteira; o luxo, os niveis do §5.7 e a
        /// bencao (970.000) como no catalogo.</summary>
        static readonly long[] CustosGrandForge =
        {
            35000, 46000, 59000, 77000, 99000, 130000, 170000, 220000, 290000, 370000, 490000, 630000, 820000, 1100000, 1400000, 1800000, 2300000, 3100000,   // produtivos
            3400000, 4400000, 5300000,                                                                                                                       // luxo
            350000, 1000000, 2800000, 6300000, 290000, 860000, 2300000, 5200000,                                                                             // niveis (Meta)
            970000,                                                                                                                                          // bencao (Meta)
        };

        static readonly long[] TurmaEstrela = { 61000, 97000, 160000, 250000 };   // §6: niveis 2-5 da turma (como a Ferraz e os Cadetes)
        static WorkerDef Estrela(int n, int rota) => new WorkerDef("wk_estrela" + n, "Estrela", WorkerRole.Porter, Specialty.Carrying, GrandForgeId, null,
            "estrela" + n, rota, 0, null, TurmaEstrela, "ajudante", "velocidade", "carga");

        /// <summary>§6: o Mensageiro Felix (Courier) vem com o upgrade `mensageiro` (o major unlock) e nao carrega: as rotas (+Tuning.ImportNivel
        /// un/min) e o lote (+Tuning.ExportNivel pecas) sobem a cada nivel acima do 1 (Sim.Exportacao.cs). Niveis 2-10 = 150.000 x 1,6^(L-2).</summary>
        static readonly WorkerDef Felix = new WorkerDef("wk_felix", "Félix", WorkerRole.Courier, Specialty.Carrying, GrandForgeId, null, Sim.Mensageiro, -1,
            0, null, new long[] { 150000, 240000, 380000, 610000, 980000, 1600000, 2500000, 4000000, 6400000 }, "felix", "rotas", "lote");

        /// <summary>§12: as familias da Grand Forge. O mercador pede o sino e a estatua (2 un, paciencia x0,8, intervalo x1,3; a 1a, sempre aberta);
        /// o nobre o relicario, a estatua e a coroa (rico, x1,8, x2,5); o emissario real a coroa, a panoplia e a lamina estelar na Fina ou melhor
        /// (2 un, x2,0, x2,0) e o colecionador (todo distrito, Renome 2) qualquer produto na Fina.</summary>
        static readonly ClientDef[] ClientesGrandForge =
        {
            new ClientDef("mercador", 2, 0.8f, 1.3f, Quality.Normal, false, "sino_catedral", "estatua_bronze"),
            new ClientDef("nobre", 1, 1.8f, 2.5f, Quality.Normal, true, "relicario_real", "estatua_bronze", "coroa_reino"),
            new ClientDef("emissario", 2, 2f, 2f, Quality.Fine, false, "coroa_reino", "panoplia_campeao", "lamina_estelar"),
            new ClientDef("colecionador", 1, 2f, 3f, Quality.Fine, true),
        };

        // ponytail: arte emprestada (como a Row): crisois e deposito de cobre do Quarter, a cratera pela pilha de gemas, as fundicoes pela forja de
        // armas, a forja estelar pela de laminas, as bancadas de sintese pelas do Arsenal, o cais pela carroca e o patio de exportacao pelo de cotas

        public static readonly DistrictDef GrandForge = new DistrictDef
        {
            // o 6o e ultimo do mapa: a Arcane Foundry libera a Grand Forge
            Id = GrandForgeId, Nome = "Grand Forge", Indice = 5,
            Itens = ItensGrandForge, Receitas = ReceitasGrandForge, Upgrades = UpgradesGrandForge, Projetos = ProjetosDefs.GrandForgeDefs,
            Masterworks = MasterworksDefs.GrandForgeDefs, MarcosColecao = MasterworksDefs.MarcosGrandForge,
            Clientes = ClientesGrandForge,
            // ponytail: a turma Estrela (§6) sao 4 Porters, como a Ferraz. Desvio do §6 (revisar; BALANCE.md §41): o 4o leva os brutos (papel 0:
            // cobre, ferro e pedra estelar), nao "importados -> bancadas": o Mensageiro ja entrega os importados na entrada (Sim.Exportacao.cs) e,
            // com um carregador de bruto so, o ferro e a pedra nunca saiam (o cobre ganhava sempre) e a cadeia do aco parava
            Workers = new[] { Estrela(1, 0), Estrela(2, 1), Estrela(3, 2), Estrela(4, 0), Felix },
            // Fileiras (§2.6; entrada -> saida de baixo para cima, colunas de 3 m, a grade da Row): depositos e cais 1 -> crisois, fornalhas e forja
            // estelar 4 -> crisol 3 6,5 -> fundicoes e forja de sintese 9 -> bancadas de sintese 11 -> balcao e patio de exportacao 13. Entrada a
            // esquerda; o cobre na coluna 1, o ferro e a cratera a direita
            Estacoes = new[]
            {
                new StationDef("deposito", Kind.Deposit, 1.5f, 1.0f, "Minério cobre", 0f, 1f, null, null, GfMinCobre, arte: "deposito_cobre"),
                new StationDef("fornalha", Kind.Furnace, 1.5f, 4.0f, "Crisol bronze", -1f, 0f, null, "crisol_bronze", arte: "crisol"),
                new StationDef("fundicao_sinos", Kind.Crafter, 1.5f, 9.0f, "Fundição sinos", -1f, 0f, null, "fundicao_sinos", arte: "fundicao_sinos"),
                new StationDef("balcao", Kind.Counter, 4.5f, 13f, "Balcão", 0f, -1f),
                new StationDef("crisol_bronze2", Kind.Furnace, 4.5f, 4.0f, "Crisol 2", -1f, 0f, "crisol_bronze2", "crisol_bronze", arte: "crisol"),
                new StationDef("deposito_ferro", Kind.Deposit, 7.5f, 1.0f, "Minério", 0f, 1f, "cratera_estelar", null, GfMinerio),
                new StationDef("fornalha_ferro", Kind.Furnace, 7.5f, 4.0f, "Fornalha", -1f, 0f, "cratera_estelar", "fornalha"),
                new StationDef("deposito_estelar", Kind.Deposit, 10.5f, 1.0f, "Cratera estelar", 0f, 1f, "cratera_estelar", null, GfPedra, arte: "cratera_estelar"),
                new StationDef("forja_estelar", Kind.Furnace, 10.5f, 4.0f, "Forja estelar", -1f, 0f, "cratera_estelar", "forja_estelar", arte: "forja_estelar"),
                new StationDef("fundicao_estatuas", Kind.Crafter, 4.5f, 9.0f, "Fundição estátua", -1f, 0f, "cratera_estelar", "fundicao_estatuas", arte: "fundicao_estatuas"),
                new StationDef("cais", Kind.Import, 13.5f, 1.0f, "Cais", 0f, 1f, Sim.Mensageiro, arte: "cais"),
                new StationDef("patio_exportacao", Kind.Export, 10.5f, 13f, "Pátio exportação", 0f, -1f, Sim.Mensageiro, arte: "patio_exportacao"),
                new StationDef("forja_sintese", Kind.Crafter, 7.5f, 9.0f, "Forja síntese", -1f, 0f, "forja_sintese", "forja_sintese", arte: "forja_sintese"),
                new StationDef("fornalha_ferro2", Kind.Furnace, 13.5f, 4.0f, "Fornalha 2", -1f, 0f, "fornalha_grande2", "fornalha"),
                new StationDef("bancada_relicarios", Kind.Crafter, 1.5f, 11.0f, "Banca relicário", -1f, 0f, "bancada_relicarios", "bancada_relicarios", arte: "bancada_relicarios"),
                new StationDef("bancada_coroas", Kind.Crafter, 4.5f, 11.0f, "Bancada coroas", -1f, 0f, "bancada_coroas", "bancada_coroas", arte: "bancada_coroas"),
                new StationDef("bancada_panoplia", Kind.Crafter, 7.5f, 11.0f, "Banca panóplia", -1f, 0f, "bancada_panoplia", "bancada_panoplia", arte: "bancada_panoplia"),
                new StationDef("crisol_bronze3", Kind.Furnace, 4.5f, 6.5f, "Crisol 3", -1f, 0f, "crisol_bronze3", "crisol_bronze", arte: "crisol"),
            },
            Pads = new[]
            {
                new PadDef(4.5f, 4.0f, "crisol_bronze2"),
                new PadDef(4.5f, 1.2f, "estrela1", "estrela2", "estrela3", "estrela4"),
                new PadDef(7.5f, 4.0f, "cratera_estelar"),
                new PadDef(13.5f, 1.0f, Sim.Mensageiro),   // no lugar do cais
                new PadDef(7.5f, 9.0f, "forja_sintese"),
                new PadDef(13.5f, 4.0f, "fornalha_grande2"),
                new PadDef(1.5f, 11.0f, "bancada_relicarios"),
                new PadDef(13.4f, 7.2f, Sim.Cais2),
                new PadDef(4.5f, 11.0f, "bancada_coroas"),
                new PadDef(7.5f, 11.0f, "bancada_panoplia"),
                new PadDef(13.4f, 12.4f, Sim.Exportacao2),   // ao lado do patio de exportacao
                new PadDef(4.5f, 6.5f, "crisol_bronze3"),
                new PadDef(13.4f, 9.8f, "vitral_forja", "praca_sinos", "estatua_heroi"),
            },
            Obstaculos = new ObstacleDef[0], Marcos = new MilestoneDef[0],
            Inicio = new V2(4.5f, 2.6f), Contratacao = new V2(4.5f, 1.2f), Largura = Balance.WorldW, Parede = false,
            // Desvio do §5.6 (revisar; BALANCE.md §41): a cratera estelar traz o trilho fornalha -> forja estelar (o efeito Conveyor, como o
            // trilho de placas da Row): sem ele o lingote ficava na fornalha e lamina, estatua e panoplia quase nao saiam
            EsteiraDe = "fornalha_ferro", EsteiraPara = "forja_estelar",
            Ajuste = t =>
            {
                // §3 precos (Normal) x 0,85, desvio do catalogo (revisar; BALANCE.md §41): ao contrario da Row, do Quarter e do Arsenal, a Grand
                // Forge nao e' presa no transporte na renda: a coroa sai so de importados (o Mensageiro entrega na entrada) e o lote paga x1,25;
                // com o catalogo a renda nos 10 min depois da producao ia a ~+30% da alvo. A relacao entre os produtos e' a do §3
                t.Price = new[] { 0, 0, 19600, 20400, 17900, 17000, 18700, 24700, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                // s entre clientes no inicio = 60 / (un/min do modelo na producao completa, §1 "mix"), como o Arsenal
                t.ClientInterval = new[] { 0f, 0f, 10.7f, 13.6f, 37.5f, 42.9f, 42.9f, 50f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
                t.CustoTabela = CustosGrandForge;
                t.QueueCap0 = 4; t.CounterCap0 = t.CounterCap1 = 10;
                t.OrderRewardMin = 6075;   // §7.2: Common da Grand Forge, 6.075 de minimo (x243 o do Old Forge)
                // §7.2 alvo x 1/3 e premio x 0,35 em toda encomenda da Grand Forge (revisar; BALANCE.md §41): com 6 linhas e as lentas (relicario,
                // lamina, panoplia) a encomenda de 20 pecas prendia a vaga e saiam ~6 por hora; a Carta (30 Royal, Renome 9) passava das 9 h no
                // Ideal. Com N menor sai o triplo, no mesmo ouro (5-7% da venda)
                t.OrderSeconds = 50f; t.OrderRewardSeconds = 3.5f;
            },
            Abertura = 12000000,
            // §2.6: a Carta da Grande Forja = producao completa + Renome 9 (4.200, BALANCE.md §41; o catalogo: 4.500) + 5 Masterworks + (3 Royal Contracts OU 30 encomendas Royal no
            // distrito); o ultimo distrito (sem proximo)
            Charter = new CharterDef(true, 4200, 5, null, contratos: 3),
            Selo = "bencao_forja",
            InsumoPorEspera = true,   // como o Arsenal: o carregador de insumo leva primeiro o que as estacoes esperam (BALANCE.md §41)            // §3.1 (a ordem e' o desempate): o peitoral da Row, colar e caixa de joias do Quarter, insignia e espada de gala do Arsenal
            Importados = new[]
            {
                ("peitoral", ArmorersRowId), ("colar", ArtisanQuarterId), ("caixa_joias", ArtisanQuarterId), ("insignia_real", RoyalArsenalId), ("espada_gala", RoyalArsenalId),
            },
        };
    }

    public static partial class OrderDefs
    {
        /// <summary>C-CORE-09 (CONTEUDO_V1 §7.2): os 7 tipos da Grand Forge, a Royal Superior (Renome 9, RenomeDefs: "Royal da Grand Forge pede
        /// Superior") e o Embaixador Kael (6 sinos). Mesmo desvio do Old Forge, da Row e do Arsenal (revisar): Bulk, Quality, Mixed, Royal e o Kael
        /// pagam 70% dos segundos do §7.2 (32/18/25/60/32 -> 22/13/18/42/22); a Royal Superior paga 1,6x a Fina, como as variantes Superiores.
        /// Metodo, nao campo: o All le daqui e a ordem dos inicializadores entre arquivos de uma classe parcial nao e' garantida.</summary>
        static OrderDef[] OrdensGrandForge() => new[]
        {
            new OrderDef("ord_common_grand_forge", Common, FS.Core.Distritos.GrandForgeId, 150f, 3, 30, 10f, 6075, 1),
            new OrderDef("ord_bulk_grand_forge", Bulk, FS.Core.Distritos.GrandForgeId, 400f, 8, 80, 22f, 14580, 3),
            new OrderDef("ord_quality_grand_forge", Qual, FS.Core.Distritos.GrandForgeId, 90f, 3, 20, 13f, 9720, 3, minQuality: Fina),
            new OrderDef("ord_rush_grand_forge", Rush, FS.Core.Distritos.GrandForgeId, 150f, 3, 30, 10f, 6075, 1, bonusDeadline: 240f, bonusRewardSeconds: 10f, bonusReputation: 1),
            new OrderDef("ord_mixed_grand_forge", Mixed, FS.Core.Distritos.GrandForgeId, 70f, 2, 15, 18f, 14580, 3, products: 3, minLines: 3),
            new OrderDef("ord_royal_grand_forge", Royal, FS.Core.Distritos.GrandForgeId, 300f, 3, 20, 42f, 36450, 15, minQuality: Fina, blueprintQueue: FilaRoyal),
            new OrderDef("ord_royal_grand_forge_superior", Royal, FS.Core.Distritos.GrandForgeId, 300f, 3, 20, 67f, 58320, 15, minQuality: Superior, blueprintQueue: FilaRoyal),
            new OrderDef("ord_blueprint_grand_forge", Blueprint, FS.Core.Distritos.GrandForgeId, 150f, 3, 30, 5f, 6075, 2, blueprintQueue: FilaEncomenda),
            new OrderDef("ord_reg_kael", Bulk, FS.Core.Distritos.GrandForgeId, 0f, 6, 6, 22f, 14580, 3, item: "sino_catedral", fixedN: 6, clientId: "cli_reg_kael"),
        };

        /// <summary>§7.1: Royal x3 e Mixed x2 na Grand Forge ("elite").</summary>
        static OrderType[] CicloGrandForge() => new[] { Royal, Mixed, Bulk, Qual, Royal, Common, Mixed, Blueprint, Rush, Royal };
    }

    public static partial class ProjetosDefs
    {
        static BlueprintDef Gf(string id, BlueprintKind kind, string alvo, bool essencial, params BlueprintSource[] fontes) =>
            new BlueprintDef(id, FS.Core.Distritos.GrandForgeId, kind, alvo.Split(','), essencial, fontes);
        static BlueprintSource FilaGf(int n) => new BlueprintSource(BlueprintSourceKind.OrderBlueprint, FS.Core.Distritos.GrandForgeId, n);
        static BlueprintSource RoyalGf(int n) => new BlueprintSource(BlueprintSourceKind.OrderRoyal, FS.Core.Distritos.GrandForgeId, n);

        /// <summary>
        /// C-CORE-09: os 14 da Grand Forge (CONTEUDO_V1 §9), na ordem do catalogo. Essenciais com fonte gratis permanente: o de mestre (L4-L5: fila
        /// Royal #1 ou a producao completa) e os projetos das 5 obras que a Carta pede (sino primeiro, estatua do fundador, lamina do cometa,
        /// relicario da luz e coroa dos seis). O sino dourado (Variant) vem dos 100 lotes de exportacao (BlueprintSource.Lotes). ponytail (como a
        /// Row e o Arsenal): a 2a fonte do bp_mw_coroa_granforja (Contract: ctr_campeao) fica de fora; fica o Renome 10.
        /// </summary>
        public static readonly BlueprintDef[] GrandForgeDefs =
        {
            Gf("bp_tecnica_sintese", BlueprintKind.Technique, "sintese", false, FilaGf(1)),
            Gf("bp_tecnica_fundicao", BlueprintKind.Technique, "fundicao", false, FilaGf(2)),
            Gf("bp_mestra_grand", BlueprintKind.StationLevel, "sintese,fundicao", true, RoyalGf(1), Producao()),
            Gf("bp_var_sino_ouro", BlueprintKind.Variant, "sino_catedral", false, new BlueprintSource(BlueprintSourceKind.Milestone, BlueprintSource.Lotes, 100)),
            Gf("bp_mw_sino_primeiro", BlueprintKind.Masterwork, "mw_sino_primeiro", true, Vendidos("sino_catedral", 30)),
            Gf("bp_mw_estatua_fundador", BlueprintKind.Masterwork, "mw_estatua_fundador", true, FilaGf(3), Vendidos("estatua_bronze", 30)),
            Gf("bp_mw_lamina_cometa", BlueprintKind.Masterwork, "mw_lamina_cometa", true, RoyalGf(2)),
            Gf("bp_mw_relicario_luz", BlueprintKind.Masterwork, "mw_relicario_luz", true, FilaGf(4), Vendidos("relicario_real", 30)),
            Gf("bp_mw_coroa_seis", BlueprintKind.Masterwork, "mw_coroa_seis", true, RoyalGf(3)),
            Gf("bp_mw_panoplia_lenda", BlueprintKind.Masterwork, "mw_panoplia_lenda", false, FilaGf(5)),
            Gf("bp_mw_sino_tempo", BlueprintKind.Masterwork, "mw_sino_tempo", false, Vendidos("sino_catedral", 200)),
            Gf("bp_mw_rainha_forjadora", BlueprintKind.Masterwork, "mw_rainha_forjadora", false, Renome(9)),
            Gf("bp_mw_lamina_estelar_rei", BlueprintKind.Masterwork, "mw_lamina_estelar_rei", false, RoyalGf(4)),
            Gf("bp_mw_coroa_granforja", BlueprintKind.Masterwork, "mw_coroa_granforja", false, Renome(10)),
        };
    }

    public static partial class MasterworksDefs
    {
        static CondicaoMasterwork Importados(int n) => new CondicaoMasterwork(CondicaoMw.Importados, null, n);
        static CondicaoMasterwork Lotes(int n) => new CondicaoMasterwork(CondicaoMw.Lotes, null, n);
        static MasterworkDef Gf(string id, string familia, int renome, int pedestal, params CondicaoMasterwork[] c) =>
            new MasterworkDef(id, Distritos.GrandForgeId, familia, "bp_" + id, renome, pedestal, c);

        /// <summary>Charters dos outros distritos que a Coroa dos Seis Distritos pede (DistrictCharters, §10.1: 5 = todos os anteriores).
        /// = o indice da Grand Forge (o teste confere).</summary>
        public const int ChartersCoroaSeis = 5;

        /// <summary>C-CORE-09: as 10 da Grand Forge (CONTEUDO_V1 §10.1), DistrictDef.Masterworks dela. Superior alcancavel pelo §10.2: sintese e
        /// fundicao = niveis 60 + tecnica 20 + bencao_forja 25 = 105 (ponytail: sem o +30 do aco estelar do §10.2, que nao e' regra do §2.6). A
        /// Carta pede 5; as essenciais sao o sino primeiro, a estatua do fundador (fundicao V), a lamina do cometa (500 importados), o relicario
        /// da luz (sintese V) e a coroa dos seis (os Charters dos outros distritos).</summary>
        public static readonly MasterworkDef[] GrandForgeDefs =
        {
            Gf("mw_sino_primeiro", "sino_catedral", 50, 1),
            Gf("mw_estatua_fundador", "estatua_bronze", 60, 2, Nivel("fundicao", 5)),
            Gf("mw_lamina_cometa", "lamina_estelar", 60, 3, Importados(500)),
            Gf("mw_relicario_luz", "relicario_real", 60, 4, Nivel("sintese", 5)),
            Gf("mw_coroa_seis", "coroa_reino", 70, 5, new CondicaoMasterwork(CondicaoMw.Charters, null, ChartersCoroaSeis)),
            Gf("mw_panoplia_lenda", "panoplia_campeao", 70, 6, Lotes(200)),
            Gf("mw_sino_tempo", "sino_catedral", 70, 7, Vendidos("sino_catedral", 500)),
            Gf("mw_rainha_forjadora", "estatua_bronze", 80, 8, Renome(9)),
            Gf("mw_lamina_estelar_rei", "lamina_estelar", 80, 9, Worker("wk_felix", 10)),
            Gf("mw_coroa_granforja", "coroa_reino", 100, 10, Renome(10)),
        };

        /// <summary>C-CORE-09: os marcos da Collection da Grand Forge, os degraus do Arsenal com as contas dela (Produtos 18 = 6 x 3; Workers 10 =
        /// 5 x contratado/nivel maximo; Blueprints 14; as 10 obras = o col_mw_distrito, so o estandarte).</summary>
        public static readonly MarcoColecaoDef[] MarcosGrandForge =
        {
            new MarcoColecaoDef("col_produtos_grand_forge_50", AbaColecao.Produtos, 9, 5),
            new MarcoColecaoDef("col_produtos_grand_forge_100", AbaColecao.Produtos, 18, 10),
            new MarcoColecaoDef("col_mw_distrito_grand_forge", AbaColecao.Masterworks, 10, 0, "cos_col_estandarte_grand_forge"),
            new MarcoColecaoDef("col_workers_grand_forge_50", AbaColecao.Workers, 5, 5),
            new MarcoColecaoDef("col_workers_grand_forge_100", AbaColecao.Workers, 10, 10),
            new MarcoColecaoDef("col_bp_grand_forge_50", AbaColecao.Blueprints, 7, 5),
            new MarcoColecaoDef("col_bp_grand_forge_100", AbaColecao.Blueprints, 14, 10),
        };
    }
}
