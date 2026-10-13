using System;

namespace FS.Core
{
    /// <summary>B-CORE-07 (ECONOMIA_WAVE_B §8.5): o que a posse do Blueprint libera. Recipe = linha nova (o UpgradeDef.Blueprint da estacao);
    /// StationLevel = niveis IV-V das familias do alvo (UpgradeDef.Blueprint); Technique = q +Tuning.TecnicaQ na familia (Sim.QualityBreakdown);
    /// Masterwork = projeto da obra-prima (so o gancho: quem le e' o B-CORE-08, Projetos.Libera); Variant = cosmetico do item (so a marca);
    /// Specialization = Wave C.</summary>
    public enum BlueprintKind { Recipe, StationLevel, Technique, Masterwork, Variant, Specialization }

    /// <summary>De onde vem o Blueprint (ECONOMIA_WAVE_B §8.5, CONTEUDO_V1 §9). A ORDEM vale regra (ProjetosDefs): as 5 primeiras sao gratis e
    /// permanentes (as unicas que seguram um essencial); Contract e' gratis mas nunca segura essencial (contrato nunca bloqueia, GDD §11); de Event
    /// em diante sao "extra" (nunca a unica fonte de nenhum Blueprint, LIVEOPS_V1 §5.7 #5); PassPremium e Shop sao pagas. Ativas hoje no Sim:
    /// Milestone, Reputation e Achievement (Projetos.Checa); as filas de encomenda sao o gancho Projetos.GanhaDaFila (Orders 2.0, B-CORE-05) e o
    /// resto o Projetos.Ganha (contrato D-CORE-04, evento D-CORE-03, Pass, loja Wave E).</summary>
    public enum BlueprintSourceKind { Milestone, OrderBlueprint, OrderRoyal, Reputation, Achievement, Contract, Event, PassFree, PassPremium, Shop }

    /// <summary>Uma fonte. `Ref`: Milestone = ID do item vendido (ou Producao); OrderBlueprint/OrderRoyal = distrito da fila; Achievement = ID da
    /// conquista (MetasDefs); Contract/Event/Pass/Shop = ID da fonte. `N`: Milestone = quantas vendidas; fila = posicao (1 = a 1a); Reputation =
    /// nivel de Renome. `MinQ`: Milestone = qualidade minima da peca vendida (Normal = qualquer).</summary>
    public readonly struct BlueprintSource
    {
        /// <summary>Marco "producao completa" (Sim.ProductionComplete) no lugar do ID do item.</summary>
        public const string Producao = "producao";
        public readonly BlueprintSourceKind Kind; public readonly string Ref; public readonly int N; public readonly Quality MinQ;
        public BlueprintSource(BlueprintSourceKind kind, string reff, int n = 0, Quality minQ = Quality.Normal) { Kind = kind; Ref = reff; N = n; MinQ = minQ; }
    }

    /// <summary>
    /// B-CORE-07 (GDD §12, V9): Blueprint e' POSSE, nao token: ter libera uma coisa para sempre; sem saldo, troca nem gasto (Sim.Projetos guarda
    /// o conjunto de IDs com a origem). `Targets` = o que libera (familia de estacao, ID de item ou de Masterwork). `Essential` = trava distrito,
    /// Charter ou Masterwork exigida: precisa de uma fonte gratis permanente (ContentValidator.Blueprints). A posicao `N` das fontes de fila e' a
    /// fila do distrito: a proxima recompensa e' a pendente de menor posicao (Projetos.ProximoDaFila), sem sorteio.
    /// </summary>
    public sealed class BlueprintDef
    {
        public readonly string Id, District; public readonly BlueprintKind Kind; public readonly string[] Targets; public readonly bool Essential;
        public readonly BlueprintSource[] Sources;
        public BlueprintDef(string id, string district, BlueprintKind kind, string[] targets, bool essential, params BlueprintSource[] sources)
        { Id = id; District = district; Kind = kind; Targets = targets; Essential = essential; Sources = sources; }
    }

    /// <summary>
    /// O catalogo em jogo (CONTEUDO_V1 §9, ECONOMIA_WAVE_B §4): os 19 do Old Forge (os 18 do §4 + bp_rec_espada_longa). Os 82 dos distritos 2-6
    /// e do Imperio liberam familias, itens, Masterworks e contratos que ainda nao existem (o validator nao teria contra o que conferir): entram
    /// com a Wave C (C-CORE-01..05), na ordem do CONTEUDO_V1 §9.
    /// Desvio do catalogo (decidido por padrao, revisar): bp_bigorna_mestra e bp_bancada_mestra ganham o marco "producao completa" como 2a fonte
    /// gratis permanente, o padrao dos bp_mestra_* dos distritos 2-6. Sem ele os niveis IV-V esperavam as filas de encomenda (B-CORE-05) e nenhum
    /// essencial do Old Forge era alcancavel no bot F2P (GDD §23 #8). Pago (Shop/PassPremium) nao aparece: o Pass premium nunca da Blueprint (V20).
    /// </summary>
    public static class ProjetosDefs
    {
        public const string OldForge = "old_forge";
        /// <summary>Distritos que existem (as filas sao por distrito). ponytail: vira DistrictDef no C-CORE-01.</summary>
        public static readonly string[] Distritos = { OldForge };
        /// <summary>O catalogo da v1.0 inteiro (CONTEUDO_V1 §9): 101, 25 essenciais. Os que faltam aqui esperam a Wave C.</summary>
        public const int CatalogoV1 = 101, EssenciaisV1 = 25;

        static BlueprintSource Vendidos(string item, int n, Quality minQ = Quality.Normal) => new BlueprintSource(BlueprintSourceKind.Milestone, item, n, minQ);
        static BlueprintSource Producao() => new BlueprintSource(BlueprintSourceKind.Milestone, BlueprintSource.Producao);
        static BlueprintSource FilaEncomenda(int n) => new BlueprintSource(BlueprintSourceKind.OrderBlueprint, OldForge, n);
        static BlueprintSource FilaRoyal(int n) => new BlueprintSource(BlueprintSourceKind.OrderRoyal, OldForge, n);
        static BlueprintSource Renome(int nivel) => new BlueprintSource(BlueprintSourceKind.Reputation, null, nivel);
        static BlueprintSource Evento(string id) => new BlueprintSource(BlueprintSourceKind.Event, id);
        static BlueprintDef B(string id, BlueprintKind kind, string alvo, bool essencial, params BlueprintSource[] fontes) =>
            new BlueprintDef(id, OldForge, kind, alvo.Split(','), essencial, fontes);

        /// <summary>Na ordem do CONTEUDO_V1 §9 (a ordem do painel de projetos). ID com prefixo bp_ (V22), nunca renomeado depois de publicado: o
        /// save grava o ID (bp=). Alvo de nivel e tecnica = familia da receita (RecipeDef.Family).</summary>
        public static readonly BlueprintDef[] All =
        {
            B("bp_tecnica_espada", BlueprintKind.Technique, "bigorna", false, FilaEncomenda(1)),
            B("bp_tecnica_escudo", BlueprintKind.Technique, "escudos", false, FilaEncomenda(2)),
            B("bp_bigorna_mestra", BlueprintKind.StationLevel, "bigorna", true, FilaEncomenda(3), Producao()),
            B("bp_bancada_mestra", BlueprintKind.StationLevel, "escudos,ferramentas,joalheria", true, FilaRoyal(1), Producao()),
            B("bp_tecnica_ferramenta", BlueprintKind.Technique, "ferramentas", false, Vendidos("ferramenta", 150)),
            B("bp_tecnica_joia", BlueprintKind.Technique, "joalheria", false, Vendidos("joia", 50)),
            B("bp_var_espada_larga", BlueprintKind.Variant, "espada", false, Vendidos("espada", 500)),
            B("bp_var_escudo_brasao", BlueprintKind.Variant, "escudo", false, Renome(3)),
            B("bp_rec_espada_longa", BlueprintKind.Recipe, "espada_longa", false, FilaEncomenda(7), Vendidos("espada", 300)),
            B("bp_mw_primeira_lamina", BlueprintKind.Masterwork, "mw_primeira_lamina", true, Vendidos("espada", 100)),
            B("bp_mw_gume_brasa", BlueprintKind.Masterwork, "mw_gume_brasa", false, FilaEncomenda(4)),
            B("bp_mw_sunblade", BlueprintKind.Masterwork, "mw_sunblade", false, FilaRoyal(2), Evento("ev_s1w4_hunt_sunblade")),
            B("bp_mw_primeira_guarda", BlueprintKind.Masterwork, "mw_primeira_guarda", false, FilaEncomenda(5)),
            B("bp_mw_broquel", BlueprintKind.Masterwork, "mw_broquel", false, Vendidos("escudo", 300)),
            B("bp_mw_escudo_brasao", BlueprintKind.Masterwork, "mw_escudo_brasao", false, Renome(3)),
            B("bp_mw_martelo_fundador", BlueprintKind.Masterwork, "mw_martelo_fundador", false, Vendidos("ferramenta", 300)),
            B("bp_mw_tenaz_eterna", BlueprintKind.Masterwork, "mw_tenaz_eterna", false, FilaEncomenda(6)),
            B("bp_mw_ember_crown", BlueprintKind.Masterwork, "mw_ember_crown", false, FilaRoyal(3), Evento("ev_s2w4_hunt_ember_crown")),
            B("bp_mw_anel_mercador", BlueprintKind.Masterwork, "mw_anel_mercador", false, Vendidos("joia", 100, Quality.Fine)),
        };

        /// <summary>Posicao do Blueprint com este ID em `table` (null = All), ou -1.</summary>
        public static int IndexOf(string id, BlueprintDef[] table = null) => Array.FindIndex(table ?? All, d => d.Id == id);

        /// <summary>Gratis e sempre alcancavel (fila de encomenda, fila Royal, marco, Renome, conquista): todo essencial tem uma.</summary>
        public static bool Permanente(BlueprintSourceKind k) => k <= BlueprintSourceKind.Achievement;
        /// <summary>Temporaria ou paga (evento, Pass, loja): nunca a unica fonte de nenhum Blueprint.</summary>
        public static bool Extra(BlueprintSourceKind k) => k >= BlueprintSourceKind.Event;
        /// <summary>Paga: so com ID de transacao (Projetos.Ganha).</summary>
        public static bool Paga(BlueprintSourceKind k) => k == BlueprintSourceKind.PassPremium || k == BlueprintSourceKind.Shop;
    }
}
