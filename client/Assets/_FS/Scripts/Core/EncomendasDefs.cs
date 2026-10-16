using System;
using System.Collections.Generic;

namespace FS.Core
{
    /// <summary>
    /// B-CORE-05: o catalogo de encomendas que este build carrega (CONTEUDO_V1 §7, ECONOMIA_WAVE_B §3.1): os 7 tipos fixos do Old Forge (mais a
    /// variante Superior do Quality, que o Renome 3 libera e so abre quando a automacao ja faz Superior), as 9 de evento (vaga propria, so
    /// pontos), os 5 fregueses do Old Forge (FS-3: item e N fixos, +1 coracao) e, do C-CORE-05, os 7 da Armorer's Row e o freguês Brutus.
    /// Os dos distritos 3-6 entram com os itens deles (o ContentValidator recusa item, distrito ou fila que nao existem). IDs com prefixo ord_ (V22), nunca renomeados: o save grava o ID.
    /// </summary>
    public static partial class OrderDefs
    {
        public const string OldForge = "old_forge", Row = FS.Core.Distritos.ArmorersRowId;
        /// <summary>Filas de Blueprint (BlueprintDef.sources, B-CORE-07): a do tipo Blueprint e a do Royal.</summary>
        public const string FilaEncomenda = "encomenda", FilaRoyal = "royal";
        public static readonly string[] Filas = { FilaEncomenda, FilaRoyal };
        /// <summary>Distritos com encomendas no build (C-CORE-05: a Row entrou; o Encomendas le as do Sim.Distrito).</summary>
        public static readonly string[] Distritos =
        {
            OldForge, Row,
            // ---- artisan_quarter (C-CORE-06)
            FS.Core.Distritos.ArtisanQuarterId,
        };
        /// <summary>Base da escala dos knobs (a Common do catalogo): alvo x Tuning.OrderSeconds / 150 e premio x Tuning.OrderRewardSeconds / 10.</summary>
        public const float AlvoBase = 150f, PremioBase = 10f;
        /// <summary>A cada tantas Commons do distrito, a vaga vira a encomenda pessoal do proximo freguês (CONTEUDO_V1 §7.2).</summary>
        public const int FreguesACada = 7;

        const OrderType Common = OrderType.Common, Bulk = OrderType.Bulk, Qual = OrderType.Quality, Rush = OrderType.Rush, Mixed = OrderType.Mixed,
            Royal = OrderType.Royal, Blueprint = OrderType.Blueprint, Event = OrderType.Event;
        const Quality Fina = Quality.Fine, Superior = Quality.Superior;

        static OrderDef Evento(string id, string evento, int pontos = 15) => new OrderDef(id, Event, null, 150f, 3, 30, 0f, 0, 0, eventId: evento, eventPoints: pontos);

        public static readonly OrderDef[] All = Junta(new[]
        {
            // Old Forge: alvo (s de renda), N min-max, premio (s; min), Renome. Desvio do catalogo (revisar): Bulk, Quality, Mixed e Royal pagam 70%
            // dos segundos do CONTEUDO_V1 §7.2 (32/18/30/25/45 -> 22/13/21/18/32): com o catalogo o ouro de encomendas ia a 13,6% da venda aos
            // 60 min no F2P (portao 4-12%, ECONOMIA_WAVE_B §10); com 70%, 11,3% (BALANCE.md §28). Common, Rush e Blueprint como no catalogo
            new OrderDef("ord_common_old_forge", Common, OldForge, 150f, 3, 30, 10f, 25, 1),
            new OrderDef("ord_bulk_old_forge", Bulk, OldForge, 400f, 8, 80, 22f, 60, 3, minLines: 2, minDelivered: 5),
            new OrderDef("ord_quality_old_forge", Qual, OldForge, 90f, 3, 20, 13f, 40, 3, minQuality: Fina),
            new OrderDef("ord_quality_old_forge_superior", Qual, OldForge, 90f, 3, 20, 21f, 75, 3, minQuality: Superior),
            new OrderDef("ord_rush_old_forge", Rush, OldForge, 150f, 3, 30, 10f, 25, 1, bonusDeadline: 240f, bonusRewardSeconds: 10f, bonusReputation: 1, minDelivered: 8),
            new OrderDef("ord_mixed_old_forge", Mixed, OldForge, 70f, 2, 15, 18f, 60, 3, products: 3, minLines: 3),
            new OrderDef("ord_royal_old_forge", Royal, OldForge, 200f, 5, 40, 32f, 150, 10, minQuality: Fina, blueprintQueue: FilaRoyal),
            new OrderDef("ord_blueprint_old_forge", Blueprint, OldForge, 150f, 3, 30, 5f, 25, 2, blueprintQueue: FilaEncomenda),
            // eventos (LIVEOPS_V1 §2.2): 15 pontos por entrega (20 fechava a trilha numa sessao; 10, so no dia 6: BALANCE §28.6), sem ouro nem Renome.
            // A Event passa fome atras das vagas normais (OnVenda: a mais antiga que aceita); com 3 vagas antes do evento (Renome 3 em ~1:40 com
            // a Collection do B-CORE-08) ela rende metade: Caravan 12, Hunts 17/20 (100 pontos de golpe inalcancaveis sem golpe), RW-B 15 (BALANCE §30.6)
            Evento("ord_ev_royal_week", "ev_s1w1_royal_week"),
            Evento("ord_ev_forge_frenzy", "ev_s1w2_forge_frenzy"),
            Evento("ord_ev_armorers_challenge", "ev_s1w3_armorers_challenge"),
            Evento("ord_ev_hunt_sunblade", "ev_s1w4_hunt_sunblade", 17),   // 20 fecha no dia 3; 15, no dia 6
            Evento("ord_ev_blacksmith_festival", "ev_s2w1_blacksmith_festival"),
            Evento("ord_ev_rare_ore_rush", "ev_s2w2_rare_ore_rush"),
            Evento("ord_ev_merchant_caravan", "ev_s2w3_merchant_caravan", 12),   // so encomendas nas missoes: 15 fecha no dia 3, 10 nunca (3 vagas; BALANCE §30.6)
            Evento("ord_ev_hunt_ember_crown", "ev_s2w4_hunt_ember_crown", 20),   // 15 nao fecha em 6 dias
            Evento("ord_ev_arcane_week", "ev_arcane_week"),
            Evento("ord_ev_forge_frenzy_b", "ev_s1w5_forge_frenzy_b"),   // reservas: sem a encomenda Event a trilha parava em 700 de 1.000
            Evento("ord_ev_royal_week_b", "ev_s2w5_royal_week_b"),   // 10 (§28.7, 2 vagas) nao fecha em 6 dias com 3 vagas (BALANCE §30.6)
            // fregueses do Old Forge (o premio e o Renome do tipo; N fixo)
            new OrderDef("ord_reg_orbo", Qual, OldForge, 0f, 5, 5, 13f, 40, 3, minQuality: Fina, item: "ferramenta", fixedN: 5, clientId: "cli_reg_orbo"),
            new OrderDef("ord_reg_lirien", Common, OldForge, 0f, 8, 8, 10f, 25, 1, item: "joia", fixedN: 8, clientId: "cli_reg_lirien"),
            new OrderDef("ord_reg_grunk", Bulk, OldForge, 0f, 40, 40, 22f, 60, 3, item: "espada", fixedN: 40, clientId: "cli_reg_grunk"),
            new OrderDef("ord_reg_bento", Common, OldForge, 0f, 10, 10, 10f, 25, 1, item: "escudo", fixedN: 10, clientId: "cli_reg_bento"),
            new OrderDef("ord_reg_ines", Qual, OldForge, 0f, 4, 4, 13f, 40, 3, minQuality: Fina, item: "joia", fixedN: 4, clientId: "cli_reg_ines"),
            // C-CORE-05, Armorer's Row (CONTEUDO_V1 §7.2): "abre: distrito aberto" (sem minimo de linhas ou entregues; Mixed pede as 3 linhas).
            // Quality e Royal pedem Fina e Blueprint a fila do distrito: abrem com a fonte de qualidade (niveis, liga, Vulcana) e os projetos da
            // Row; a variante Superior do Quality (Renome 5) quando a automacao ja faz Superior (21 s como a do Old Forge; minimo 75 x 3).
            // Mesmo desvio do Old Forge (revisar): Bulk, Quality, Mixed, Royal e o Brutus (Bulk) pagam 70% dos segundos do §7.2 (45/18/25/45/32
            // -> 32/13/18/32/22): com o catalogo e as 3 vagas do Renome do Imperio o ouro de encomendas ia a 15,3% da venda (portao 4-12%);
            // com 70%, 12,2% (e 6,1% com a vaga unica do Renome proprio da Row, hoje; BALANCE.md §33)
            new OrderDef("ord_common_armorers_row", Common, Row, 150f, 3, 30, 10f, 75, 1),
            new OrderDef("ord_bulk_armorers_row", Bulk, Row, 600f, 12, 120, 32f, 180, 4),
            new OrderDef("ord_quality_armorers_row", Qual, Row, 90f, 3, 20, 13f, 120, 3, minQuality: Fina),
            new OrderDef("ord_quality_armorers_row_superior", Qual, Row, 90f, 3, 20, 21f, 225, 3, minQuality: Superior),
            new OrderDef("ord_rush_armorers_row", Rush, Row, 150f, 3, 30, 10f, 75, 1, bonusDeadline: 240f, bonusRewardSeconds: 10f, bonusReputation: 1),
            new OrderDef("ord_mixed_armorers_row", Mixed, Row, 70f, 2, 15, 18f, 180, 3, products: 3, minLines: 3),
            new OrderDef("ord_royal_armorers_row", Royal, Row, 200f, 5, 40, 32f, 450, 10, minQuality: Fina, blueprintQueue: FilaRoyal),
            new OrderDef("ord_blueprint_armorers_row", Blueprint, Row, 150f, 3, 30, 5f, 75, 2, blueprintQueue: FilaEncomenda),
            new OrderDef("ord_reg_brutus", Bulk, Row, 0f, 30, 30, 22f, 180, 3, item: "lanca", fixedN: 30, clientId: "cli_reg_brutus"),
        },
            // ---- artisan_quarter (C-CORE-06): Distritos.ArtisanQuarter.cs
            OrdensArtisan());

        /// <summary>As OrderDefs do build, na ordem dos distritos: cada distrito novo traz as dele num metodo do arquivo dele (classe parcial).</summary>
        static OrderDef[] Junta(params OrderDef[][] partes)
        {
            var l = new List<OrderDef>();
            foreach (OrderDef[] p in partes) l.AddRange(p);
            return l.ToArray();
        }

        /// <summary>Rodizio de cada distrito (posicao = Encomendas.Abertas % 10; sem peso, sem sorteio). Event fica fora (vaga propria).</summary>
        public static readonly (string District, OrderType[] Cycle)[] Ciclos =
        {
            (OldForge, new[] { Common, Common, Bulk, Qual, Common, Mixed, Rush, Blueprint, Common, Royal }),
            (Row, new[] { Common, Bulk, Qual, Common, Bulk, Mixed, Rush, Blueprint, Common, Royal }),   // §7.1: Bulk x2 ("equipar a milicia")
            // ---- artisan_quarter (C-CORE-06)
            (FS.Core.Distritos.ArtisanQuarterId, CicloArtisan()),
        };

        public static OrderType[] Ciclo(string district) => Array.Find(Ciclos, c => c.District == district).Cycle;

        /// <summary>A Common do distrito: o que um tipo travado vira.</summary>
        public static OrderDef Comum(string district) => Array.Find(All, d => d.District == district && d.Type == OrderType.Common && d.ClientId == null);

        /// <summary>
        /// Regras do ContentValidator (ECONOMIA_WAVE_B §8.4): ID ord_ unico em [a-z0-9_]; distrito que existe (Event: sem distrito, com evento e
        /// pontos, sem ouro nem Renome); N min &lt;= max (freguês: N fixo dentro); qualidade minima ate Superior (Masterwork nunca e' alvo); bonus
        /// so com prazo (nao existe penalidade); premio positivo e multiplo de 5; Mixed com 2+ produtos e 3+ linhas; fila de Blueprint que
        /// existe (o tipo Blueprint tem fila); item fixo que existe no distrito dele e e' vendido la (freguês com cli_; `w` = o mundo do Old Forge,
        /// os outros distritos pelo ContentValidator.Mundo). Ciclo de distrito que existe, com Common, sem Event e so com tipos que tem OrderDef no
        /// distrito.
        /// </summary>
        public static IEnumerable<string> Validar(OrderDef[] defs, (string District, OrderType[] Cycle)[] ciclos, Sim w)
        {
            var ids = new HashSet<string>();
            Sim MundoDe(string district)
            {
                DistrictDef dd = district == w.Distrito.Id ? null : FS.Core.Distritos.Find(district);
                return dd == null ? w : ContentValidator.Mundo(dd);
            }
            foreach (OrderDef d in defs)
            {
                string at = $"encomenda {d.Id}:";
                if (string.IsNullOrEmpty(d.Id) || !d.Id.StartsWith("ord_", StringComparison.Ordinal) || !Array.TrueForAll(d.Id.ToCharArray(), c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_'))
                    yield return $"{at} ID fora do formato ord_[a-z0-9_]";
                else if (!ids.Add(d.Id)) yield return $"{at} ID repetido";
                if (d.Type == OrderType.Event)
                {
                    if (d.District != null || d.EventId == null || d.EventPoints <= 0) yield return $"{at} Event pede evento e pontos, sem distrito";
                    if (d.RewardSeconds != 0f || d.RewardMin != 0 || d.Reputation != 0) yield return $"{at} Event da so pontos de evento (sem ouro nem Renome)";
                }
                else
                {
                    if (Array.IndexOf(Distritos, d.District) < 0) yield return $"{at} distrito \"{d.District}\" que nao existe no jogo";
                    if (d.EventId != null) yield return $"{at} evento fora do tipo Event";
                    if (d.RewardSeconds <= 0f || d.RewardMin <= 0 || d.RewardMin % 5 != 0) yield return $"{at} premio {d.RewardSeconds} s; {d.RewardMin} (positivo, minimo x5)";
                }
                if (d.TargetMin < 1 || d.TargetMin > d.TargetMax) yield return $"{at} N {d.TargetMin}-{d.TargetMax}";
                if (d.FixedN > 0 ? d.FixedN < d.TargetMin || d.FixedN > d.TargetMax : d.TargetSeconds <= 0f) yield return $"{at} alvo sem segundos de renda nem N fixo dentro do intervalo";
                if (d.MinQuality > Quality.Superior) yield return $"{at} qualidade minima {d.MinQuality} (Masterwork nunca e' alvo)";
                if (d.BonusDeadline < 0f || d.BonusRewardSeconds < 0f || d.BonusReputation < 0 || d.Reputation < 0) yield return $"{at} prazo, bonus ou Renome negativo";
                if (d.BonusDeadline <= 0f && (d.BonusRewardSeconds > 0f || d.BonusReputation > 0)) yield return $"{at} bonus sem prazo";
                if (d.Type == OrderType.Mixed ? d.Products < 2 || d.MinLines < 3 : d.Products != 1) yield return $"{at} {d.Products} produtos (Mixed: 2+ com 3+ linhas; os outros: 1)";
                if (d.BlueprintQueue != null && Array.IndexOf(Filas, d.BlueprintQueue) < 0) yield return $"{at} fila de Blueprint \"{d.BlueprintQueue}\" que nao existe";
                if (d.Type == OrderType.Blueprint && d.BlueprintQueue == null) yield return $"{at} Blueprint sem fila";
                if (d.Item != null)
                {
                    Sim wd = MundoDe(d.District);   // C-CORE-05: a lanca do Brutus e' item da Row
                    int i = Items.IndexOf(d.Item, wd.ItemDefs);
                    if (i < 0 || !wd.IsProduct((Item)i) || !wd.Stations.Exists(s => wd.Faz(s, (Item)i))) yield return $"{at} item \"{d.Item}\" que nao existe no jogo (ou nao e' vendido)";
                    if (d.FixedN <= 0) yield return $"{at} item fixo sem N fixo";
                }
                if (d.ClientId != null && (d.Item == null || !d.ClientId.StartsWith("cli_", StringComparison.Ordinal))) yield return $"{at} freguês \"{d.ClientId}\" sem item fixo ou fora de cli_";
            }
            foreach ((string district, OrderType[] cycle) in ciclos)
            {
                if (Array.IndexOf(Distritos, district) < 0) yield return $"ciclo: distrito \"{district}\" que nao existe";
                if (cycle == null || Array.IndexOf(cycle, OrderType.Common) < 0) yield return $"ciclo {district}: sem Common (o que um tipo travado vira)";
                if (cycle == null) continue;
                foreach (OrderType t in cycle)
                    if (t == OrderType.Event) yield return $"ciclo {district}: Event no ciclo (tem vaga propria)";
                    else if (!Array.Exists(defs, d => d.District == district && d.Type == t && d.ClientId == null)) yield return $"ciclo {district}: {t} sem OrderDef no distrito";
            }
            foreach (string district in Distritos)
                if (!Array.Exists(ciclos, c => c.District == district)) yield return $"ciclo: distrito {district} sem ciclo";
        }
    }
}
