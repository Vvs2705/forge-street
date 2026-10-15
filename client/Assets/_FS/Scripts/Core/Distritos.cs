using System;

namespace FS.Core
{
    /// <summary>C-CORE-01: estacao do layout de um distrito. `Familia` = a RecipeDef.Station da receita que ela faz (null = deposito ou balcao);
    /// `Abre` = ID do upgrade que a constroi (null = do kit); `Bruto` = o item que o deposito da (so Kind.Deposit). (InX, InY) = lado da boca de
    /// ENTRADA; a de saida fica no lado oposto (Sim.Add). O Sim cria as estacoes na ordem da tabela: o indice e' o do save (stN=).</summary>
    public sealed class StationDef
    {
        public readonly string Id, Name, Familia, Abre;
        public readonly Kind Kind;
        public readonly V2 Pos;
        public readonly float InX, InY;
        public readonly Item Bruto;
        /// <summary>Folha de arte da estacao na View (Resources/Sprites); null = pelo tipo e pelo produto, como no Old Forge.</summary>
        public readonly string Arte;
        public StationDef(string id, Kind kind, float x, float y, string name, float inX, float inY, string abre = null, string familia = null, Item bruto = Item.Ore,
            string arte = null)
        {
            Id = id; Kind = kind; Pos = new V2(x, y); Name = name; InX = inX; InY = inY; Abre = abre; Familia = familia; Bruto = bruto; Arte = arte;
        }
    }

    /// <summary>C-CORE-01: pad no chao. `Cadeia` = IDs de upgrade vendidos ali em ordem (Ajudante 1/2/3); o indice do pad e' o da View e da dica.</summary>
    public sealed class PadDef
    {
        public readonly V2 Pos; public readonly string[] Cadeia;
        public PadDef(float x, float y, params string[] cadeia) { Pos = new V2(x, y); Cadeia = cadeia; }
    }

    /// <summary>C-CORE-02/03 (V5, V15; CONTEUDO_V1 §2): o Royal Charter do distrito, 1x e sem reset. Condicoes: producao completa, Renome do
    /// Imperio >= `Renome` (o limiar do nivel que libera "charter_&lt;id&gt;", ContentValidator.Charters) e `Masterworks` obras-primas DESTE
    /// distrito. Concede +100 de Renome (Renome.OnCharter), a renda de fundo do distrito e libera `Proximo` (null = o ultimo do build).</summary>
    public sealed class CharterDef
    {
        public readonly bool Producao; public readonly long Renome; public readonly int Masterworks; public readonly string Proximo;
        public CharterDef(bool producao, long renome, int masterworks, string proximo) { Producao = producao; Renome = renome; Masterworks = masterworks; Proximo = proximo; }
    }

    /// <summary>
    /// C-CORE-01 (GDD §6, CONTEUDO_V1 §2): um distrito em dados. Uma instancia de Sim por distrito (Sim(distrito: d)); a carteira, o Renome e o
    /// Charter ficam no Imperio. Itens, receitas, upgrades, workers e projetos sao as tabelas DELE (handle = posicao na tabela dele); o layout
    /// (estacoes, pads, obstaculos, baus, inicio do ferreiro, ponto de contratacao, largura antes do Corredor, parede lateral, esteira) sai daqui
    /// em vez de constantes do Sim. `Ajuste` = os numeros de economia do distrito por cima do Tuning da partida (preco, intervalo de cliente,
    /// custo por upgrade, vagas e estoque do balcao); null = o Tuning como esta (Old Forge). O Old Forge e' a tabela de sempre: assinatura
    /// identica ao byte (econsim, BalanceTests, save e bot; BALANCE.md §31).
    /// </summary>
    public sealed class DistrictDef
    {
        public string Id, Nome;
        public int Indice;
        public ItemDef[] Itens; public RecipeDef[] Receitas; public UpgradeDef[] Upgrades; public WorkerDef[] Workers; public BlueprintDef[] Projetos;
        public MasterworkDef[] Masterworks; public MarcoColecaoDef[] MarcosColecao;   // B-CORE-08: Hall e Collection do distrito
        public StationDef[] Estacoes; public PadDef[] Pads; public ObstacleDef[] Obstaculos; public MilestoneDef[] Marcos;
        public V2 Inicio, Contratacao;
        /// <summary>Limite direito do ferreiro (m) ate o efeito do Corredor (SideCorridor); sem Corredor no distrito, o mapa inteiro.</summary>
        public float Largura;
        /// <summary>Parede lateral do Old Forge (Balance.SideWall*): rota pela porta ou pelo arco (Sim.Via). false = sem parede.</summary>
        public bool Parede;
        /// <summary>Esteira: estacao de origem e de destino (IDs), ligada pelo efeito Conveyor.</summary>
        public string EsteiraDe, EsteiraPara;
        public Action<Tuning> Ajuste;
        /// <summary>Ouro para abrir o distrito depois do Charter do anterior (0 = aberto desde o inicio: o Old Forge).</summary>
        public long Abertura;
        public CharterDef Charter;
        /// <summary>Renda de fundo depois do Charter deste distrito: fracao da RateEma dele quando o jogador sai (manager nivel 1; CONTEUDO_V1 §1).</summary>
        public float FundoFator = 0.25f;
    }

    /// <summary>C-CORE-01: os distritos do build, na ordem do mapa. Old Forge = as tabelas de sempre; Armorer's Row = CONTEUDO_V1 §2.2/§3/§5.2.</summary>
    public static class Distritos
    {
        public const string OldForgeId = "old_forge", ArmorersRowId = "armorers_row";

        /// <summary>O layout que estava no construtor do Sim, sem mudar nenhum numero. Pads de upgrade do menu (Fole, Mochila, Botas, Vitrine,
        /// Ajudantes ageis, Martelo veloz, Lupa, Vitrine de joias) seguem na lista, invisiveis (Pad.Current): o indice do pad e' o da View e da dica.</summary>
        public static readonly DistrictDef OldForge = new DistrictDef
        {
            Id = OldForgeId, Nome = "Old Forge", Indice = 0,
            Itens = Items.All, Receitas = Recipes.All, Upgrades = FS.Core.Upgrades.All, Workers = WorkerDefs.All, Projetos = ProjetosDefs.All,
            Masterworks = MasterworksDefs.All, MarcosColecao = MasterworksDefs.Marcos,
            // Bocas (FASE5 §2b): (inX, inY) = lado da ENTRADA. Toda estacao que produz: entrada a esquerda, saida a direita. A 2a area foi
            // ANEXADA (8 e 9) e a bigorna longa (10, B-CORE-07) tambem: os indices e o save antigos nao mudam
            Estacoes = new[]
            {
                new StationDef("deposito", Kind.Deposit, 1.5f, 1.5f, "Depósito", 0f, 1f),            // zona unica acima (lado das fornalhas)
                new StationDef("fornalha", Kind.Furnace, 1.5f, 5.5f, "Fornalha", -1f, 0f, null, "fornalha"),
                new StationDef("fornalha2", Kind.Furnace, 4.5f, 5.5f, "Fornalha 2", -1f, 0f, "fornalha2", "fornalha"),
                new StationDef("bigorna", Kind.Crafter, 1.5f, 9.5f, "Bigorna", -1f, 0f, null, "bigorna"),
                new StationDef("bigorna2", Kind.Crafter, 4.5f, 9.5f, "Bigorna 2", -1f, 0f, "bigorna2", "bigorna"),
                new StationDef("escudos", Kind.Crafter, 7.5f, 9.5f, "Escudos", -1f, 0f, "escudos", "escudos"),
                new StationDef("ferramentas", Kind.Crafter, 7.5f, 5.5f, "Ferramentas", -1f, 0f, "ferramentas", "ferramentas"),
                new StationDef("balcao", Kind.Counter, 4.5f, 13f, "Balcão", 0f, -1f),                 // zona unica embaixo (fila fica em cima)
                new StationDef("joalheria", Kind.Crafter, 12f, 6.5f, "Joalheria", -1f, 0f, "joalheria", "joalheria"),
                new StationDef("loja_joias", Kind.Counter, 12f, 11.5f, "Loja de joias", 0f, -1f, "joalheria"),
                new StationDef("bigorna_longa", Kind.Crafter, 5.0f, 1.5f, "Bigorna longa", -1f, 0f, "bigorna_longa", "bigorna_longa"),   // fileira do Deposito
            },
            // Pads fora das linhas de caminhada (coluna x=1,5 e diagonais ate o balcao): atravessar um pad nao pode gastar
            Pads = new[]
            {
                new PadDef(0.5f, 6.5f, "fole", "fole_duplo"),
                new PadDef(4.5f, 5.5f, "fornalha2"),
                new PadDef(4.5f, 9.5f, "bigorna2"),
                new PadDef(7.5f, 9.5f, "escudos"),
                new PadDef(7.5f, 5.5f, "ferramentas"),
                new PadDef(8.3f, 1.5f, "ajudante1", "ajudante2", "ajudante3"),
                new PadDef(6.5f, 0.6f, "ajudantes_ageis"),
                new PadDef(4.5f, 0.6f, "mochila"),
                new PadDef(2.8f, 0.6f, "botas"),
                new PadDef(0.5f, 11.2f, "esteira"),            // era (0,5; 7,8): corredor da parede entre as entradas da Fornalha e da Bigorna (BALANCE §15)
                new PadDef(8.4f, 3.4f, "martelo_veloz"),       // era (0,5; 9,5): rotulo sobre o da Bigorna (FASE4 §3, BALANCE §14)
                new PadDef(6.5f, 13f, "vitrine"),
                new PadDef(8.4f, 11.6f, "corredor"),           // 2a area, pads 12 e 13: lado direito da oficina, fora das linhas
                new PadDef(12f, 6.5f, "joalheria"),
                new PadDef(14.2f, 8.5f, "joalheiro1"),         // fase 2, pads 14-16: rua lateral, fora da linha bancada -> loja (x=12)
                new PadDef(14.2f, 6.8f, "lupa"),               // era (10,2; 6,5): boca da porta lateral (FASE5 §2, BALANCE §13)
                new PadDef(14.2f, 11.5f, "vitrine_joias"),
                new PadDef(6.5f, 3.2f, "fachada_nobre"),       // luxo, pads 17-19: so depois de todos os produtivos
                new PadDef(4.5f, 3.2f, "piso_oficina"),
                new PadDef(14.2f, 5.1f, "joalheria_real"),
                new PadDef(2.8f, 0.6f, "mineiro"),             // fase 4, pads 20-21; no lugar das Botas (menu, pad invisivel)
                new PadDef(12f, 3.5f, "joalheiro2"),           // rua lateral embaixo da Joalheria
                new PadDef(5.0f, 1.5f, "bigorna_longa"),       // B-CORE-07, pad 22: no lugar da estacao (so com o projeto)
            },
            Obstaculos = Balance.Obstacles, Marcos = Balance.Milestones,
            Inicio = new V2(4.5f, 3.5f), Contratacao = Sim.HireSpot, Largura = Balance.WorkshopW, Parede = true,
            EsteiraDe = "fornalha", EsteiraPara = "bigorna",
            Charter = new CharterDef(true, 400, 1, ArmorersRowId),   // V15: producao completa + Renome 4 (400) + 1 Masterwork
        };

        // ---------------------------------------------------------------- Armorer's Row (CONTEUDO_V1 §2.2, §3, §3.1, §4, §5.2, §6)
        // Handles: 0 e 1 sao minerio e lingote como no Old Forge (deposito, papeis 0/1 dos ajudantes); produtos de 2 a 8; insumos depois
        // (C-CORE-05: a liga anexada no fim, 12)
        const Item Minerio = Item.Ore, Lingote = Item.Ingot, Elmo = (Item)2, Maca = (Item)3, Grevas = (Item)4, Lanca = (Item)5, Manoplas = (Item)6,
            Machado = (Item)7, Peitoral = (Item)8, Placa = (Item)9, Rebite = (Item)10, Couro = (Item)11, Liga = (Item)12;

        static readonly ItemDef[] ItensArmeiros =
        {
            new ItemDef("minerio", "minério", 1, false),
            new ItemDef("lingote", "lingote", 1, false),
            new ItemDef("elmo", "elmo", 2, true),
            new ItemDef("maca", "maça", 2, true),
            new ItemDef("grevas", "grevas", 2, true),
            new ItemDef("lanca", "lança", 2, true),
            new ItemDef("manoplas", "manoplas", 2, true),
            new ItemDef("machado_guerra", "machado bélico", 2, true),
            new ItemDef("peitoral", "peitoral", 2, true),
            new ItemDef("placa", "placa", 2, false),
            new ItemDef("rebite", "rebite", 2, false),
            new ItemDef("couro", "couro", 2, false),
            new ItemDef("liga", "liga", 2, false),   // C-CORE-05 (CONTEUDO_V1 §3: Intermediate, tier 2)
        };

        /// <summary>TimeMul = tempo base da tabela §3.1 / tempo inicial do tipo (fornalha 4 s, bancada 5 s). Montagem = bancada que so comeca com
        /// todos os componentes (o Sim ja faz: Produce espera a receita inteira). C-CORE-05 (§2.2 regra 2): toda montagem e forja tem o slot
        /// opcional de liga (Qty 0, RecipeDef.Inputs): com liga, a peca soma q +30; a fornalha de liga faz 2 lingotes -> 1 liga em 6 s.</summary>
        static readonly RecipeDef[] ReceitasArmeiros =
        {
            new RecipeDef("fornalha", Kind.Furnace, Lingote, t => 1f, (Minerio, 1)),
            new RecipeDef("prensa", Kind.Furnace, Placa, t => 0.75f, (Lingote, 1)),
            new RecipeDef("rebitadeira", "rebitadeira", Kind.Furnace, Rebite, 4, t => 1f, (Lingote, 1)),
            new RecipeDef("montagem_elmo", "montagem", Kind.Crafter, Elmo, t => 1.2f, (Placa, 2), (Rebite, 2), (Liga, 0)),
            new RecipeDef("bigorna_pesada", "forja_armas", Kind.Crafter, Maca, t => 1.2f, (Lingote, 2), (Rebite, 2), (Liga, 0)),
            new RecipeDef("montagem_grevas", "montagem", Kind.Crafter, Grevas, t => 1.2f, (Placa, 2), (Couro, 1), (Liga, 0)),
            new RecipeDef("forja_hastes", "forja_armas", Kind.Crafter, Lanca, t => 0.8f, (Lingote, 1), (Couro, 1), (Liga, 0)),
            new RecipeDef("montagem_manoplas", "montagem", Kind.Crafter, Manoplas, t => 1.4f, (Placa, 1), (Rebite, 2), (Couro, 1), (Liga, 0)),
            new RecipeDef("forja_machados", "forja_armas", Kind.Crafter, Machado, t => 1.2f, (Lingote, 2), (Couro, 1), (Liga, 0)),
            new RecipeDef("montagem_peitoral", "montagem", Kind.Crafter, Peitoral, t => 1.8f, (Placa, 3), (Rebite, 4), (Couro, 1), (Liga, 0)),
            new RecipeDef("fornalha_liga", Kind.Furnace, Liga, t => 1.5f, (Lingote, 2)),
        };

        // ponytail: estacoes sem arte propria usam a da familia mais proxima: montagens = bancada de escudos (armadura), forjas = bigorna;
        // prensa e rebitadeira = fornalha (pelo tipo); carroca do couro = a carroca da rua lateral do Old Forge
        const string Escudos = "bancada_escudos", Bigorna = "bigorna";

        const Upgrade Nenhum = (Upgrade)(-1);
        static UpgradeDef U(int i, string id, int requer, string nome, string desc, bool menu = false, bool luxo = false, Upgrade efeito = Nenhum,
            string station = null, string blueprint = null) =>
            new UpgradeDef((Upgrade)i, id, requer, nome, desc, luxo, menu, station, blueprint: blueprint, efeito: efeito);
        const string Mestra = "bp_mestra_armorers";

        /// <summary>CONTEUDO_V1 §5.2 na ordem do tier (= indice; o save grava o ID). C-CORE-05: a `fornalha_liga` (tier 11) voltou ao lugar
        /// dela; depois dos luxos, os niveis de estacao do §5.7 (Meta, menu): montagem e forja de armas II-V (IV-V com o projeto de mestre da
        /// Row). Desvio do §5.7 (revisar; BALANCE.md §35): o nivel II das duas familias pede a Rebitadeira 2, o ultimo produtivo, e nao a 1a
        /// estacao da familia. Com o Renome do Imperio na chegada (>= 500: 3 vagas e a encomenda Royal) o projeto de mestre vinha antes da
        /// producao e o bot comprava os IV-V (205 mil) no meio dela: producao completa do F2P 56 -> 76 min (janela 55-70); assim, 59 min, e os
        /// niveis viram o ralo do rabo da Row, com a Vulcana e o luxo.</summary>
        static readonly UpgradeDef[] UpgradesArmeiros =
        {
            U(0, "fole_couro", -1, "Fole de couro", "Fornalhas mais rápidas", menu: true, efeito: Upgrade.FurnaceSpeed1),
            U(1, "prensa2", -1, "Prensa 2", "Mais placas por minuto"),
            U(2, "ferraz1", -1, "Ferraz 1", "Leva minério às fornalhas"),
            U(3, "bigorna_pesada", -1, "Maças", "Nova linha: maça"),
            U(4, "ferraz2", 2, "Ferraz 2", "Leva lingotes às prensas"),
            U(5, "trilho_placas", -1, "Trilho de placas", "Prensa → elmos sem andar", efeito: Upgrade.Conveyor),
            U(6, "carroca_curtidor", -1, "Carroça curtidor", "Couro e nova linha: grevas"),
            U(7, "forja_hastes", 6, "Lanças", "Nova linha: lança"),
            U(8, "fornalha_armeiro2", -1, "Fornalha 2", "Dobra os lingotes"),
            U(9, "ferraz3", 4, "Ferraz 3", "Leva produtos ao balcão"),
            U(10, "montagem_manoplas", 6, "Manoplas", "Nova linha: manoplas"),
            U(11, "fornalha_liga", 8, "Fornalha de liga", "Liga nas peças: qualidade +30"),
            U(12, "forja_machados", 6, "Machados", "Nova linha: machado bélico"),
            U(13, "fole_couro_duplo", 0, "Fole duplo", "Fornalhas ainda mais rápidas", menu: true, efeito: Upgrade.FurnaceSpeed2),
            U(14, "ferraz4", 9, "Ferraz 4", "Mais um leva minério e couro"),
            U(15, "gabaritos", -1, "Gabaritos", "Montagens e forjas mais rápidas", menu: true, efeito: Upgrade.HammerSpeed),
            U(16, "montagem_peitoral", 10, "Peitorais", "Nova linha: peitoral"),
            U(17, "rebitadeira2", -1, "Rebitadeira 2", "Mais rebites por minuto"),
            U(18, "estandartes_armeiro", 17, "Estandartes", "Bandeiras na rua", luxo: true),
            U(19, "rua_calcada", 18, "Rua calçada", "Pedras novas na rua", luxo: true),
            U(20, "manequins", 19, "Manequins", "Armaduras em exposição", luxo: true),
            U(21, "nivel_montagem2", 17, "Montagens II", "Mais qualidade e velocidade", menu: true, station: "montagem"),
            U(22, "nivel_montagem3", 21, "Montagens III", "Mais qualidade e velocidade", menu: true, station: "montagem"),
            U(23, "nivel_montagem4", 22, "Montagens IV", "Mais qualidade e velocidade", menu: true, station: "montagem", blueprint: Mestra),
            U(24, "nivel_montagem5", 23, "Montagens V", "Mais qualidade e velocidade", menu: true, station: "montagem", blueprint: Mestra),
            U(25, "nivel_forja_armas2", 17, "Forjas II", "Mais qualidade e velocidade", menu: true, station: "forja_armas"),
            U(26, "nivel_forja_armas3", 25, "Forjas III", "Mais qualidade e velocidade", menu: true, station: "forja_armas"),
            U(27, "nivel_forja_armas4", 26, "Forjas IV", "Mais qualidade e velocidade", menu: true, station: "forja_armas", blueprint: Mestra),
            U(28, "nivel_forja_armas5", 27, "Forjas V", "Mais qualidade e velocidade", menu: true, station: "forja_armas", blueprint: Mestra),
        };

        /// <summary>C-CORE-05 (BALANCE.md §33): os produtivos do §5.2 x 1,55 (multiplo de 5 ate 10 mil, 2 algarismos acima, regra do §1; a fornalha
        /// de liga 8.600 -> 13.000); o luxo e os niveis de estacao (§5.7: montagem 4.300/13.000/35.000/78.000, forja de armas 3.500/11.000/28.000/
        /// 64.000) como no catalogo. Com o preco x 3,5 (Ajuste) a producao completa do F2P cai de 1:31 h para ~1 h e a renda sobe a ~4.800/min.</summary>
        static readonly long[] CustosArmeiros =
        {
            745, 970, 1255, 1635, 2125, 2760, 3590, 4665, 6070, 7890, 10000, 13000, 17000, 23000, 29000, 39000, 50000, 65000,   // produtivos
            42000, 54000, 66000,                                                                                                // luxo
            4300, 13000, 35000, 78000, 3500, 11000, 28000, 64000,                                                               // niveis (Meta)
        };

        static readonly long[] Turma = { 750, 1200, 1920, 3070 };   // §6: niveis 2-5 da turma Ferraz (6-10 com o resto do distrito)
        static WorkerDef Ferraz(int n, int rota) => new WorkerDef("wk_ferraz" + n, "Ferraz", WorkerRole.Porter, Specialty.Carrying, ArmorersRowId, null,
            "ferraz" + n, rota, 0, null, Turma, "ajudante", "velocidade", "carga");

        /// <summary>C-CORE-05 (CONTEUDO_V1 §6): a Fundidora (Smelter) que opera a fornalha de liga (o Requer, Equipe.Ritmo: 6 -> 4 s, +3%/nivel) e
        /// da q +15/+25/+30 nas montagens (Station = a familia). Contratacao e niveis do catalogo (1.800 x 1,6^(L-2)), em ouro, como a equipe do
        /// Old Forge. ponytail: a folha do oficial ate a arte dela existir.</summary>
        static readonly WorkerDef Vulcana = new WorkerDef("wk_vulcana", "Vulcana", WorkerRole.Smelter, Specialty.Armor, ArmorersRowId, "montagem", null, -1,
            7200, "fornalha_liga", new long[] { 1800, 2880, 4610, 7375, 12000, 19000, 30000, 48000, 77000 }, "oficial", "liga", "qualidade");

        public static readonly DistrictDef ArmorersRow = new DistrictDef
        {
            Id = ArmorersRowId, Nome = "Armorer's Row", Indice = 1,
            Itens = ItensArmeiros, Receitas = ReceitasArmeiros, Upgrades = UpgradesArmeiros, Projetos = ProjetosDefs.RowDefs,
            // C-CORE-05: as 10 obras da Row (CONTEUDO_V1 §10.1) e os marcos da Collection dela; Superior alcancavel com a liga (BALANCE.md §35)
            Masterworks = MasterworksDefs.RowDefs, MarcosColecao = MasterworksDefs.MarcosRow,
            // ponytail: a turma Ferraz (§6, 1 WorkerDef com 4 corpos) sao 4 Porters ate o campo `crew` existir; o 4o leva o bruto (minerio ou couro)
            Workers = new[] { Ferraz(1, 0), Ferraz(2, 1), Ferraz(3, 2), Ferraz(4, 0), Vulcana },
            // Fileiras (deposito 1 -> fornalhas 4 -> prensas 6,5 -> montagens 9 -> forjas 11 -> balcao 13), colunas de 3 m; entrada a esquerda.
            // C-CORE-05: deposito de 1,5 para 1 m: o rotulo dele (1,35 m acima) encostava no da Fornalha (1,04 m abaixo dela); descer as
            // fornalhas poria o pad da Fornalha 2 sob o rotulo da Prensa 2
            Estacoes = new[]
            {
                new StationDef("deposito", Kind.Deposit, 1.5f, 1.0f, "Depósito", 0f, 1f),
                new StationDef("fornalha", Kind.Furnace, 1.5f, 4.0f, "Fornalha", -1f, 0f, null, "fornalha"),
                new StationDef("prensa", Kind.Furnace, 1.5f, 6.5f, "Prensa", -1f, 0f, null, "prensa"),
                new StationDef("rebitadeira", Kind.Furnace, 7.5f, 6.5f, "Rebitadeira", -1f, 0f, null, "rebitadeira"),
                new StationDef("montagem_elmo", Kind.Crafter, 1.5f, 9.0f, "Montagem elmo", -1f, 0f, null, "montagem_elmo", arte: Escudos),
                new StationDef("balcao", Kind.Counter, 4.5f, 13f, "Balcão", 0f, -1f),
                new StationDef("prensa2", Kind.Furnace, 4.5f, 6.5f, "Prensa 2", -1f, 0f, "prensa2", "prensa"),
                new StationDef("bigorna_pesada", Kind.Crafter, 1.5f, 11.0f, "Bigorna pesada", -1f, 0f, "bigorna_pesada", "bigorna_pesada", arte: Bigorna),
                new StationDef("carroca_couro", Kind.Deposit, 13.5f, 1.5f, "Carroça couro", 0f, 1f, "carroca_curtidor", null, Couro, "carroca"),
                new StationDef("montagem_grevas", Kind.Crafter, 4.5f, 9.0f, "Montagem grevas", -1f, 0f, "carroca_curtidor", "montagem_grevas", arte: Escudos),
                new StationDef("forja_hastes", Kind.Crafter, 4.5f, 11.0f, "Forja de hastes", -1f, 0f, "forja_hastes", "forja_hastes", arte: Bigorna),
                new StationDef("fornalha2", Kind.Furnace, 4.5f, 4.0f, "Fornalha 2", -1f, 0f, "fornalha_armeiro2", "fornalha"),
                new StationDef("montagem_manoplas", Kind.Crafter, 7.5f, 9.0f, "Mont. manoplas", -1f, 0f, "montagem_manoplas", "montagem_manoplas", arte: Escudos),
                new StationDef("forja_machados", Kind.Crafter, 7.5f, 11.0f, "Forja machados", -1f, 0f, "forja_machados", "forja_machados", arte: Bigorna),
                new StationDef("montagem_peitoral", Kind.Crafter, 10.5f, 9.0f, "Mont. peitoral", -1f, 0f, "montagem_peitoral", "montagem_peitoral", arte: Escudos),
                new StationDef("rebitadeira2", Kind.Furnace, 10.5f, 6.5f, "Rebitadeira 2", -1f, 0f, "rebitadeira2", "rebitadeira"),
                // C-CORE-05: na fileira das fornalhas, sobre a rebitadeira (como fornalha -> prensa na coluna 1); anexada no fim (indice do save)
                new StationDef("fornalha_liga", Kind.Furnace, 7.5f, 4.0f, "Fornalha liga", -1f, 0f, "fornalha_liga", "fornalha_liga"),
            },
            Pads = new[]
            {
                new PadDef(4.5f, 6.5f, "prensa2"),
                new PadDef(8.3f, 1.5f, "ferraz1", "ferraz2", "ferraz3", "ferraz4"),
                new PadDef(1.5f, 11.0f, "bigorna_pesada"),
                new PadDef(10.2f, 12.6f, "trilho_placas"),
                new PadDef(13.5f, 1.5f, "carroca_curtidor"),
                new PadDef(4.5f, 11.0f, "forja_hastes"),
                new PadDef(4.5f, 4.0f, "fornalha_armeiro2"),
                new PadDef(7.5f, 9.0f, "montagem_manoplas"),
                new PadDef(7.5f, 11.0f, "forja_machados"),
                new PadDef(10.5f, 9.0f, "montagem_peitoral"),
                new PadDef(10.5f, 6.5f, "rebitadeira2"),
                new PadDef(12.6f, 12.6f, "estandartes_armeiro", "rua_calcada", "manequins"),
                new PadDef(7.5f, 4.0f, "fornalha_liga"),   // C-CORE-05: no lugar da estacao
            },
            Obstaculos = new ObstacleDef[0], Marcos = new MilestoneDef[0],
            Inicio = new V2(4.5f, 2.6f), Contratacao = new V2(8.3f, 1.5f), Largura = Balance.WorldW, Parede = false,
            EsteiraDe = "prensa", EsteiraPara = "montagem_elmo",
            Ajuste = t =>
            {
                // §3 precos (Normal) x 3,5, desvio do catalogo (revisar; BALANCE.md §33): o modelo do §1 supoe a vazao das estacoes (~38 un/min
                // na producao completa); medida, a Row vende ~12 un/min, presa no transporte de lingote, placa, rebite e couro (4 Ferraz com carga
                // 2, montagem de 2-3 componentes), nao na demanda nem nas bancadas. A relacao entre os produtos e' a do §3. §2.2 balcao de 6 vagas
                // com estoque 10 (sem Vitrine no distrito)
                t.Price = new[] { 0, 0, 490, 420, 455, 245, 420, 385, 805, 0, 0, 0, 0 };
                // s entre clientes no inicio = 60 / (un/min do modelo na producao completa, §1 "mix"); a demanda sobra (metade do intervalo, o dobro
                // ou fila de 8 nao mudam a renda: BALANCE.md §33)
                t.ClientInterval = new[] { 0f, 0f, 11.5f, 11.5f, 11.5f, 7.7f, 13.6f, 11.5f, 17.1f, 0f, 0f, 0f, 0f };
                t.CustoTabela = CustosArmeiros;
                t.QueueCap0 = 6; t.CounterCap0 = t.CounterCap1 = 10;
                t.OrderRewardMin = 75;   // §7.2: Common da Row, 75 de minimo (x3 o do Old Forge)
            },
            Abertura = 140000,
            Charter = new CharterDef(true, 750, 2, null),   // §2.2: producao completa + Renome 5 (750) + 2 Masterworks; proximo distrito fora do build
        };

        public static readonly DistrictDef[] All = { OldForge, ArmorersRow };

        /// <summary>Distrito com este ID, ou null.</summary>
        public static DistrictDef Find(string id) => Array.Find(All, d => d.Id == id);
    }
}
