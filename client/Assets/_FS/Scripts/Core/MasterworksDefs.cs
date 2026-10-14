using System;
using System.Collections.Generic;

namespace FS.Core
{
    /// <summary>B-CORE-08 (CONTEUDO_V1 §10.1): o tipo da condicao especial de uma Masterwork, sempre de jogo. Vendidos = Counter:item (pecas
    /// vendidas); NivelEstacao = StationLevel:familia (RecipeDef.Family); Renome = ReputationTier; EncomendasItem = OrdersOfProduct:item;
    /// EncomendasTipo = OrdersOfType:tipo (OrderType; contador order_complete do Orders 2.0, B-CORE-05); NivelWorker = WorkerLevel:wk;
    /// VendidosQualidade = QualitySold:item@nivel. Nenhum tipo fala de anuncio, compra ou Gem (GDD §13, V4); os do distrito 2 em diante
    /// (liga, cotas, energizadas, exportacao...) entram com a mecanica deles (Wave C).</summary>
    public enum CondicaoMw { Vendidos, NivelEstacao, Renome, EncomendasItem, EncomendasTipo, NivelWorker, VendidosQualidade }

    /// <summary>Uma condicao: `Ref` = item, familia de estacao, tipo de encomenda ou worker (Renome: nenhum); `N` = o alvo; `MinQ` = nivel
    /// minimo da peca vendida (so VendidosQualidade).</summary>
    public readonly struct CondicaoMasterwork
    {
        public readonly CondicaoMw Kind; public readonly string Ref; public readonly int N; public readonly Quality MinQ;
        public CondicaoMasterwork(CondicaoMw kind, string reff, int n, Quality minQ = Quality.Normal) { Kind = kind; Ref = reff; N = n; MinQ = minQ; }
    }

    /// <summary>
    /// B-CORE-08 (ECONOMIA_WAVE_B §8.6, CONTEUDO_V1 §10.1): a obra-prima em dados. `Familia` = familia de produto (ItemDef.Family: a espada
    /// longa e' da espada); `Blueprint` = o projeto (posse no Sim.Projetos); `Condicoes` = todas precisam valer; `Renome` = o credito da 1a vez
    /// (Renome.Credita mw:{id}); `Pedestal` = lugar no Hall do distrito (1-10). A ordem na tabela e' a de "pendente" dentro da familia
    /// (ECONOMIA_WAVE_B §1.3: a primeira com projeto e condicoes, ainda fora do Hall). ID com prefixo mw_ (V22), nunca renomeado: o save grava.
    /// </summary>
    public sealed class MasterworkDef
    {
        public readonly string Id, District, Familia, Blueprint; public readonly int Renome, Pedestal; public readonly CondicaoMasterwork[] Condicoes;
        public MasterworkDef(string id, string district, string familia, string blueprint, int renome, int pedestal, params CondicaoMasterwork[] condicoes)
        { Id = id; District = district; Familia = familia; Blueprint = blueprint; Renome = renome; Pedestal = pedestal; Condicoes = condicoes; }
    }

    /// <summary>B-CORE-08 (GDD §13, CONTEUDO_V1 §10.3): abas da Collection que ja tem contador no nucleo. Clientes (ClientDef, FS-3) e Distritos
    /// (Charter, Wave C) entram com o sistema deles.</summary>
    public enum AbaColecao { Produtos, Masterworks, Workers, Blueprints }

    /// <summary>Marco da Collection: `N` entradas da aba (contagem absoluta; o % do catalogo esta no comentario do dado) pagam `Renome` e o
    /// `Cosmetico` (posse = o ID de transacao col:{id} no ledger do Renome), 1x. Nunca ouro (GDD §13, CONTEUDO_V1 §10.3).</summary>
    public sealed class MarcoColecaoDef
    {
        public readonly string Id, Cosmetico; public readonly AbaColecao Aba; public readonly int N, Renome;
        public MarcoColecaoDef(string id, AbaColecao aba, int n, int renome, string cosmetico = null) { Id = id; Aba = aba; N = n; Renome = renome; Cosmetico = cosmetico; }
    }

    /// <summary>
    /// B-CORE-08: as 10 Masterworks do Old Forge (ECONOMIA_WAVE_B §6 = CONTEUDO_V1 §10.1, sem mudanca) e os marcos da Collection que o build
    /// alcanca. As 50 dos distritos 2-6 usam familias, estacoes e workers que ainda nao existem (o validator recusaria): entram com a Wave C,
    /// no mesmo formato, como os Blueprints deles (ProjetosDefs). Os marcos da v1.0 acima do que o Old Forge tem (col_produtos_25 = 29 de 114,
    /// col_bp_25 = 26 de 101...) tambem: so entra marco alcancavel (validator).
    /// </summary>
    public static class MasterworksDefs
    {
        /// <summary>Familia completa no Hall: +5% fixo no preco de venda dela (GDD §13, FS-6; col_mw_familia do CONTEUDO_V1 §10.3). Revisar.</summary>
        public const int BonusFamiliaPct = 5;
        /// <summary>A meta da v1.0 (CONTEUDO_V1 §10.1): 10 por distrito.</summary>
        public const int CatalogoV1 = 60, PorDistrito = 10;
        /// <summary>Tipo de encomenda da condicao EncomendasTipo: o OrderType do Orders 2.0 pelo nome (sem caixa: "mixed"); null = nao existe.</summary>
        public static OrderType? Tipo(string r) => r != null && Enum.TryParse(r, true, out OrderType t) && Enum.IsDefined(typeof(OrderType), t) ? t : (OrderType?)null;

        static CondicaoMasterwork Vendidos(string item, int n) => new CondicaoMasterwork(CondicaoMw.Vendidos, item, n);
        static CondicaoMasterwork Nivel(string familia, int n) => new CondicaoMasterwork(CondicaoMw.NivelEstacao, familia, n);
        static CondicaoMasterwork Renome(int nivel) => new CondicaoMasterwork(CondicaoMw.Renome, null, nivel);
        static CondicaoMasterwork EncomendasDe(string item, int n) => new CondicaoMasterwork(CondicaoMw.EncomendasItem, item, n);
        static CondicaoMasterwork EncomendasTipo(string tipo, int n) => new CondicaoMasterwork(CondicaoMw.EncomendasTipo, tipo, n);
        static CondicaoMasterwork Worker(string wk, int n) => new CondicaoMasterwork(CondicaoMw.NivelWorker, wk, n);
        static MasterworkDef M(string id, string familia, int renome, int pedestal, params CondicaoMasterwork[] c) =>
            new MasterworkDef(id, ProjetosDefs.OldForge, familia, "bp_" + id, renome, pedestal, c);

        /// <summary>Na ordem do CONTEUDO_V1 §10.1 (= a do pedestal e a de "pendente" dentro da familia). Base de todas: peca que ja sairia
        /// Superior + golpe de mestre + o projeto; a condicao soma algo a isso.</summary>
        public static readonly MasterworkDef[] All =
        {
            M("mw_primeira_lamina", "espada", 25, 1),
            M("mw_gume_brasa", "espada", 30, 2, Nivel("bigorna", 5)),
            M("mw_sunblade", "espada", 50, 3, Vendidos("espada", 1000)),
            M("mw_primeira_guarda", "escudo", 30, 4, EncomendasDe("escudo", 10)),
            M("mw_broquel", "escudo", 25, 5, Nivel("escudos", 5)),
            M("mw_escudo_brasao", "escudo", 30, 6, Worker("wk_tome", 5)),
            M("mw_martelo_fundador", "ferramenta", 25, 7, Nivel("ferramentas", 5)),
            M("mw_tenaz_eterna", "ferramenta", 25, 8, EncomendasTipo("mixed", 5)),
            M("mw_ember_crown", "joia", 50, 9, Renome(4)),
            M("mw_anel_mercador", "joia", 30, 10),
        };

        /// <summary>
        /// Os marcos que o Old Forge alcanca: os degraus do ECONOMIA_WAVE_B §7 (50% e 100% de cada aba no Old Forge: 15 produto x qualidade, 26
        /// worker x contratado/nivel maximo, 19 projetos) e os do CONTEUDO_V1 §10.3 que ja cabem (col_mw_5/10, col_workers_25 = 15 de 58).
        /// col_mw_1 ("abre o Hall") nao e' marco: o Hall abre com a 1a obra (Hall.Aberto). Ordem = a do painel. Degraus do §7 pela metade
        /// (5/10, nao 10/20): o F2P paga os 7 antes das 2 h e, com o Renome por tipo do Orders 2.0, 10/20 trazia o Renome 3 para 1:22 e o 4 para
        /// 2:48 (pisos 1:30 e 3:00); col_workers_25 segue o catalogo (+10). BALANCE §30.6.
        /// </summary>
        public static readonly MarcoColecaoDef[] Marcos =
        {
            new MarcoColecaoDef("col_produtos_old_forge_50", AbaColecao.Produtos, 8, 5),
            new MarcoColecaoDef("col_produtos_old_forge_100", AbaColecao.Produtos, 15, 10),
            new MarcoColecaoDef("col_mw_5", AbaColecao.Masterworks, 5, 20),
            new MarcoColecaoDef("col_mw_10", AbaColecao.Masterworks, 10, 50, "cos_col_estandarte_old_forge"),
            new MarcoColecaoDef("col_workers_old_forge_50", AbaColecao.Workers, 13, 5),
            new MarcoColecaoDef("col_workers_25", AbaColecao.Workers, 15, 10),
            new MarcoColecaoDef("col_workers_old_forge_100", AbaColecao.Workers, 26, 10),
            new MarcoColecaoDef("col_bp_old_forge_50", AbaColecao.Blueprints, 10, 5),
            new MarcoColecaoDef("col_bp_old_forge_100", AbaColecao.Blueprints, 19, 10),
        };

        /// <summary>Posicao da Masterwork com este ID em `table` (null = All), ou -1.</summary>
        public static int IndexOf(string id, MasterworkDef[] table = null) => Array.FindIndex(table ?? All, d => d.Id == id);

        /// <summary>Prefixos de ref que nomeiam anuncio, compra ou moeda paga: nunca condicao de Masterwork (GDD §13).</summary>
        static readonly string[] Pagos = { "ad_", "ads", "anuncio", "rewarded", "iap", "compra", "shop", "loja", "gems", "pass_premium" };

        static bool IdOk(string id, string prefixo) => id != null && id.StartsWith(prefixo, StringComparison.Ordinal) &&
            Array.TrueForAll(id.ToCharArray(), c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_');

        /// <summary>Pontos de qualidade maximos da familia de estacao `fam` neste conteudo: niveis (UpgradeDef.Station) + tecnica (algum Blueprint
        /// Technique dela) + oficial no nivel 5 + mestre de obras (ECONOMIA_WAVE_B §1.2; CONTEUDO_V1 §10.2).</summary>
        public static int QMaximo(string fam, Tuning t, UpgradeDef[] ups, BlueprintDef[] bps, WorkerDef[] wks)
        {
            int niveis = Array.FindAll(ups, u => u.Station == fam).Length;
            return niveis * t.NivelEstacaoQ
                + (Array.Exists(bps, b => b.Kind == BlueprintKind.Technique && Array.IndexOf(b.Targets, fam) >= 0) ? t.TecnicaQ : 0)
                + (Array.Exists(wks, w => w.Papel == WorkerRole.Smith && w.Station == fam) ? WorkerDefs.SmithQ5 : 0)
                + (Array.Exists(wks, w => w.Papel == WorkerRole.Foreman) ? WorkerDefs.ForemanQ : 0);
        }

        /// <summary>
        /// B-CORE-08 (ECONOMIA_WAVE_B §8.6, GDD §23 #9-#10): ID mw_ unico num distrito que existe; familia de produto que alguma estacao do mundo
        /// faz, com Superior alcancavel (q maximo > 100); projeto que existe, e' do tipo Masterwork, aponta para ela e tem fonte gratis
        /// permanente; Renome positivo; pedestal 1-10 unico no distrito; cada condicao de jogo e com alvo que existe (item, familia, nivel,
        /// tipo de encomenda, worker) e nunca de anuncio ou compra. Todo projeto de Masterwork aponta para uma que existe. Marco: col_ unico,
        /// alcancavel neste build (N ate o total da aba), com recompensa (Renome ou cosmetico cos_), nunca ouro.
        /// </summary>
        public static IEnumerable<string> Validar(MasterworkDef[] defs, MarcoColecaoDef[] marcos, BlueprintDef[] bps, UpgradeDef[] ups, Sim w)
        {
            var ids = new HashSet<string>(); var pedestais = new HashSet<string>();
            int Produto(string id) { int i = Items.IndexOf(id, w.ItemDefs); return i >= 0 && w.IsProduct((Item)i) ? i : -1; }
            foreach (MasterworkDef d in defs)
            {
                string at = $"masterwork {d.Id}:";
                if (!IdOk(d.Id, "mw_")) yield return $"{at} ID fora do formato mw_[a-z0-9_]";
                else if (!ids.Add(d.Id)) yield return $"{at} ID repetido";
                if (Array.IndexOf(ProjetosDefs.Distritos, d.District) < 0) yield return $"{at} distrito \"{d.District}\" nao existe";
                // familia: estacoes do mundo que fazem um produto dela; Superior alcancavel em pelo menos uma (q maximo > 100)
                List<Station> st = w.Stations.FindAll(s => s.Produces && w.IsProduct(s.OutItem) && w.ItemDefs[(int)s.OutItem].Family == d.Familia);
                if (st.Count == 0) yield return $"{at} familia \"{d.Familia}\" sem produto feito no mundo";
                else if (!st.Exists(s => QMaximo(s.Recipe.Family, w.T, ups, bps, w.Equipe.Defs) > 100)) yield return $"{at} Superior inalcancavel na familia {d.Familia} (q maximo <= 100)";
                int b = ProjetosDefs.IndexOf(d.Blueprint, bps);
                if (b < 0) yield return $"{at} projeto \"{d.Blueprint}\" nao existe";
                else if (bps[b].Kind != BlueprintKind.Masterwork || Array.IndexOf(bps[b].Targets, d.Id) < 0) yield return $"{at} projeto \"{d.Blueprint}\" nao e' o dela";
                else if (!Array.Exists(bps[b].Sources, f => ProjetosDefs.Permanente(f.Kind))) yield return $"{at} projeto sem fonte gratis permanente (evento, contrato ou pago nunca sao a unica)";
                if (d.Renome <= 0) yield return $"{at} Renome {d.Renome}";
                if (d.Pedestal < 1 || d.Pedestal > PorDistrito || !pedestais.Add(d.District + "#" + d.Pedestal)) yield return $"{at} pedestal {d.Pedestal} fora de 1-{PorDistrito} ou repetido no distrito";
                foreach (CondicaoMasterwork c in d.Condicoes ?? new CondicaoMasterwork[0])
                {
                    string ac = $"{at} condicao {c.Kind} \"{c.Ref}\" >= {c.N}:";
                    string r = (c.Ref ?? "").ToLowerInvariant();
                    if (Array.Exists(Pagos, p => r.StartsWith(p, StringComparison.Ordinal)) || Array.Exists(w.T.Placements, p => p.Id == r))
                    { yield return $"{ac} anuncio ou compra nunca e' condicao (GDD §13)"; continue; }
                    bool ok = c.N > 0;
                    switch (c.Kind)
                    {
                        case CondicaoMw.Vendidos: case CondicaoMw.EncomendasItem: ok &= Produto(c.Ref) >= 0; break;
                        case CondicaoMw.VendidosQualidade: ok &= Produto(c.Ref) >= 0 && c.MinQ >= Quality.Fine && c.MinQ <= Quality.Superior; break;
                        case CondicaoMw.NivelEstacao: ok &= c.N >= 2 && c.N <= 1 + Array.FindAll(ups, u => u.Station == c.Ref).Length && w.Stations.Exists(s => s.Produces && s.Recipe.Family == c.Ref); break;
                        case CondicaoMw.Renome: ok &= c.N < RenomeDefs.Niveis.Length; break;
                        case CondicaoMw.EncomendasTipo: ok &= Tipo(c.Ref) != null && Tipo(c.Ref) != OrderType.Event; break;   // evento e' temporario
                        case CondicaoMw.NivelWorker: int k = WorkerDefs.IndexOf(c.Ref, w.Equipe.Defs); ok &= k >= 0 && c.N >= 2 && c.N <= w.Equipe.Defs[k].MaxNivel; break;
                        default: ok = false; break;   // tipo que o nucleo nao conhece
                    }
                    if (!ok) yield return $"{ac} inalcancavel (alvo, nivel ou tipo que o build nao tem)";
                }
            }
            foreach (BlueprintDef bp in bps)
                if (bp.Kind == BlueprintKind.Masterwork) foreach (string t in bp.Targets) if (IndexOf(t, defs) < 0) yield return $"masterwork: o projeto {bp.Id} aponta para \"{t}\", que nao existe";
            var cols = new HashSet<string>();
            foreach (MarcoColecaoDef m in marcos)
            {
                string at = $"colecao {m.Id}:";
                if (!IdOk(m.Id, "col_")) yield return $"{at} ID fora do formato col_[a-z0-9_]";
                else if (!cols.Add(m.Id)) yield return $"{at} ID repetido";
                if (m.N <= 0 || m.N > w.Colecao.Total(m.Aba)) yield return $"{at} {m.N} entradas de {m.Aba} (o build tem {w.Colecao.Total(m.Aba)}: marco inalcancavel espera o conteudo)";
                if (m.Renome < 0 || m.Renome == 0 && m.Cosmetico == null) yield return $"{at} sem recompensa (Renome ou cosmetico; nunca ouro)";
                if (m.Cosmetico != null && !IdOk(m.Cosmetico, "cos_")) yield return $"{at} cosmetico \"{m.Cosmetico}\" fora do formato cos_";
            }
        }
    }
}
