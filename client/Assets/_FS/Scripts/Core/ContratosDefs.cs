using System;
using System.Collections.Generic;

namespace FS.Core
{
    /// <summary>Uma etapa de entrega do contrato: `N` pecas do item `Item` (ID do ItemDef) no nivel `MinQ` ou melhor. As etapas sao
    /// sequenciais: so a atual conta (o cartao mostra "etapa x/N").</summary>
    public readonly struct ContractStage
    {
        public readonly string Item; public readonly int N; public readonly Quality MinQ;
        public ContractStage(string item, int n, Quality minQ = Quality.Normal) { Item = item; N = n; MinQ = minQ; }
    }

    /// <summary>
    /// D-CORE-04 (GDD §11, CONTEUDO_V1 §8): Royal Contract em dados. `District` = distrito de origem no catalogo (abre quando o distrito existe
    /// no build); o nivel de Renome que libera e' o da tabela do Renome (RenomeDefs.Niveis[..].Libera lista o ID; -1 = nenhum). Premio na
    /// conclusao: `OuroSeg` segundos da taxa online de agora (Metas.Ouro: multiplo de 5, piso das encomendas, FORA do GoldEarned), `Reputacao`
    /// de Renome e o `Blueprint` (posse, V9) pelo Projetos.Ganha com o ID de transacao ctr:{id}. Prazo so de bonus (`BonusMin` minutos de jogo
    /// ativo desde o aceite: no prazo, ouro x1,5 e Renome +5); nunca falha, nunca expira. Sem Gems (V9). IDs ctr_* nunca renomeados: o save grava.
    /// </summary>
    public sealed class ContractDef
    {
        public readonly string Id, District, Blueprint;
        public readonly ContractStage[] Etapas;
        public readonly float BonusMin;
        public readonly int OuroSeg, Reputacao;

        public ContractDef(string id, string district, ContractStage[] etapas, float bonusMin, int ouroSeg, int reputacao, string blueprint)
        { Id = id; District = district; Etapas = etapas; BonusMin = bonusMin; OuroSeg = ouroSeg; Reputacao = reputacao; Blueprint = blueprint; }

        /// <summary>Nivel de Renome que libera (RenomeDefs), -1 = nenhum.</summary>
        public int Renome => RenomeDefs.NivelQueLibera(Id);
        public int Total { get { int n = 0; foreach (ContractStage e in Etapas) n += e.N; return n; } }
    }

    /// <summary>
    /// D-CORE-04: os 12 Royal Contracts da v1.0 (CONTEUDO_V1 §8), na ordem da lista = a ordem da oferta (sem sorteio). So o 1o e' do Old Forge
    /// (Renome 3); os outros abrem com o distrito deles (Wave C) e ficam dormentes ate la (Contratos.Aberto). Os bp_var_* que eles dao sao do
    /// "imperio" (CONTEUDO_V1 §9) e entram no ProjetosDefs com a Wave C: ate la o Projetos.Ganha e' no-op (o validator so cobra o Blueprint que
    /// existe no build). Bau cosmetico (cos_ctr_*) fica de fora ate existir cosmetico de oficina (so o ID no catalogo).
    /// </summary>
    public static class ContratosDefs
    {
        public const int CatalogoV1 = 12;
        /// <summary>No prazo-bonus: ouro x1,5 e Renome +5 (CONTEUDO_V1 §8).</summary>
        public const float BonusOuro = 1.5f; public const int BonusRenome = 5;
        /// <summary>Maior nivel de Renome (GDD §14): requisito acima disso e' inalcancavel.</summary>
        public const int RenomeMax = 10;
        /// <summary>Distritos do catalogo (CONTEUDO_V1 §2). ponytail: vira DistrictDef no C-CORE-01; os que existem no build sao OrderDefs.Distritos.</summary>
        public static readonly string[] DistritosCatalogo = { "old_forge", "armorers_row", "artisan_quarter", "royal_arsenal", "arcane_foundry", "grand_forge" };

        const Quality Fina = Quality.Fine;

        static ContractDef C(string id, string distrito, Quality q, float bonusMin, int ouroSeg, int renome, string bp, params (string item, int n)[] pede)
        {
            var e = new ContractStage[pede.Length];
            for (int k = 0; k < e.Length; k++) e[k] = new ContractStage(pede[k].item, pede[k].n, q);
            return new ContractDef(id, distrito, e, bonusMin, ouroSeg, renome, bp);
        }

        public static readonly ContractDef[] All =
        {
            // id, distrito, qualidade minima, prazo-bonus (min ativos), ouro (s da taxa), Renome, Blueprint, etapas
            C("ctr_ronda_vila", "old_forge", Quality.Normal, 90f, 300, 15, "bp_var_ronda", ("espada", 30), ("escudo", 20), ("ferramenta", 10)),
            C("ctr_vinte_guardas", "armorers_row", Quality.Normal, 120f, 400, 20, "bp_var_guardas", ("espada", 20), ("escudo", 20), ("elmo", 20)),
            C("ctr_milicia_norte", "armorers_row", Quality.Normal, 120f, 450, 20, "bp_var_milicia", ("lanca", 30), ("maca", 20), ("grevas", 20)),
            C("ctr_obras_castelo", "artisan_quarter", Quality.Normal, 150f, 500, 25, "bp_var_obras", ("ferramenta", 60), ("cinzel", 20), ("castical", 10)),
            C("ctr_dote_princesa", "artisan_quarter", Fina, 150f, 600, 30, "bp_var_dote", ("colar", 10), ("caixa_joias", 10), ("broche", 20)),
            C("ctr_guarda_real", "royal_arsenal", Quality.Normal, 150f, 600, 30, "bp_var_guarda_real", ("kit_guarda", 20), ("elmo", 20), ("insignia_real", 10)),
            C("ctr_batedores", "royal_arsenal", Quality.Normal, 120f, 500, 25, "bp_var_batedores", ("kit_batedor", 30), ("lanca", 30)),
            C("ctr_coroacao", "royal_arsenal", Fina, 180f, 750, 35, "bp_var_coroacao", ("espada_gala", 10), ("alabarda_cerimonial", 10), ("armadura_gala", 5)),
            C("ctr_torre_magos", "arcane_foundry", Quality.Normal, 180f, 700, 35, "bp_var_torre", ("lanterna_arcana", 20), ("anel_cristal", 20), ("cajado_liga", 10)),
            C("ctr_vanguarda", "arcane_foundry", Fina, 240f, 900, 40, "bp_var_vanguarda", ("lamina_runica", 20), ("escudo_runico", 20), ("peitoral", 20)),
            C("ctr_sinos_catedral", "grand_forge", Quality.Normal, 240f, 900, 40, "bp_var_sinos", ("sino_catedral", 6), ("estatua_bronze", 2), ("relicario_real", 5)),
            C("ctr_campeao", "grand_forge", Fina, 300f, 1200, 40, "bp_var_campeao", ("panoplia_campeao", 5), ("lamina_estelar", 5), ("coroa_reino", 1)),
        };

        /// <summary>Posicao do contrato com este ID em `table` (null = All), ou -1.</summary>
        public static int IndexOf(string id, ContractDef[] table = null) => Array.FindIndex(table ?? All, d => d.Id == id);

        static bool IdOk(string id) => Array.TrueForAll(id.ToCharArray(), c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_');

        /// <summary>
        /// Regras do ContentValidator (GDD §23 #7, V25): 12 no catalogo; ID ctr_[a-z0-9_] unico; distrito do catalogo; etapas com item, N &gt; 0
        /// e qualidade ate Superior (Masterwork nunca e' alvo), e item vendido neste build quando o distrito e' do build; ouro e Renome
        /// positivos, prazo nao negativo; requisito de Renome alcancavel (&lt;= 10) e todo ctr_ que a tabela do Renome libera existe; Blueprint em
        /// bp_[a-z0-9_] e, quando existe no build, lista o contrato como fonte Contract e, se essencial, tem fonte gratis permanente (regra do
        /// #55: essencial nunca so por contrato). Contrato nunca bloqueia (V25): fonte Contract de Blueprint aponta para contrato que existe e
        /// nenhuma melhoria exige Blueprint que so vem de contrato.
        /// </summary>
        public static IEnumerable<string> Validar(ContractDef[] defs, BlueprintDef[] bps, RenomeDef[] renome, UpgradeDef[] upgrades, Sim w)
        {
            if (defs.Length != CatalogoV1) yield return $"contrato: {defs.Length} no catalogo (CONTEUDO_V1 §8: {CatalogoV1})";
            var ids = new HashSet<string>();
            foreach (ContractDef d in defs)
            {
                string at = $"contrato {d.Id}:";
                if (string.IsNullOrEmpty(d.Id) || !d.Id.StartsWith("ctr_", StringComparison.Ordinal) || !IdOk(d.Id)) yield return $"{at} ID fora do formato ctr_[a-z0-9_]";
                else if (!ids.Add(d.Id)) yield return $"{at} ID repetido";
                if (Array.IndexOf(DistritosCatalogo, d.District) < 0) yield return $"{at} distrito \"{d.District}\" fora do catalogo";
                bool noBuild = Array.IndexOf(OrderDefs.Distritos, d.District) >= 0;
                if (d.Etapas == null || d.Etapas.Length == 0) yield return $"{at} sem etapa";
                foreach (ContractStage e in d.Etapas ?? new ContractStage[0])
                {
                    if (string.IsNullOrEmpty(e.Item) || !IdOk(e.Item) || e.N <= 0) yield return $"{at} etapa \"{e.Item}\" x{e.N}";
                    if (e.MinQ > Quality.Superior) yield return $"{at} qualidade minima {e.MinQ} (Masterwork nunca e' alvo)";
                    if (!noBuild) continue;
                    int i = Items.IndexOf(e.Item, w.ItemDefs);
                    if (i < 0 || !w.IsProduct((Item)i) || !w.Stations.Exists(s => s.Produces && s.OutItem == (Item)i)) yield return $"{at} item \"{e.Item}\" que nao existe no jogo (ou nao e' vendido)";
                }
                if (d.OuroSeg <= 0 || d.Reputacao <= 0) yield return $"{at} premio {d.OuroSeg} s; Renome {d.Reputacao} (positivos)";
                if (d.BonusMin < 0f) yield return $"{at} prazo-bonus negativo";
                int nivel = Array.FindIndex(renome, r => Array.IndexOf(r.Libera, d.Id) >= 0);
                if (nivel > RenomeMax) yield return $"{at} Renome {nivel} inalcancavel (maximo {RenomeMax})";
                if (d.Blueprint == null) continue;
                if (!d.Blueprint.StartsWith("bp_", StringComparison.Ordinal) || !IdOk(d.Blueprint)) yield return $"{at} Blueprint \"{d.Blueprint}\" fora do formato bp_[a-z0-9_]";
                int b = ProjetosDefs.IndexOf(d.Blueprint, bps);
                if (b < 0) continue;   // ainda nao esta no build (Wave C): o gancho fica no-op
                if (!Array.Exists(bps[b].Sources, f => f.Kind == BlueprintSourceKind.Contract && f.Ref == d.Id)) yield return $"{at} Blueprint \"{d.Blueprint}\" nao lista o contrato como fonte";
                if (bps[b].Essential && !Array.Exists(bps[b].Sources, f => ProjetosDefs.Permanente(f.Kind)))
                    yield return $"{at} Blueprint essencial \"{d.Blueprint}\" so por contrato (contrato nunca bloqueia, GDD §11)";
            }
            foreach (RenomeDef r in renome)
                foreach (string id in r.Libera)
                    if (id.StartsWith("ctr_", StringComparison.Ordinal) && !ids.Contains(id)) yield return $"renome: nivel {r.Nivel} libera \"{id}\", contrato que o build nao tem";
            foreach (BlueprintDef b in bps)
            {
                foreach (BlueprintSource f in b.Sources)
                    if (f.Kind == BlueprintSourceKind.Contract && !ids.Contains(f.Ref)) yield return $"blueprint {b.Id}: contrato \"{f.Ref}\" nao existe";
                if (b.Sources.Length > 0 && Array.TrueForAll(b.Sources, f => f.Kind == BlueprintSourceKind.Contract) && Array.Exists(upgrades, u => u.Blueprint == b.Id))
                    yield return $"blueprint {b.Id}: so por contrato e uma melhoria o exige (contrato nunca bloqueia, V25)";
            }
        }
    }
}
