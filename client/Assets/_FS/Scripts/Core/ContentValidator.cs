using System;
using System.Collections.Generic;

namespace FS.Core
{
    /// <summary>
    /// A-CORE-05 (DOCUMENTO_MESTRE §108): confere o conteudo do jogo e devolve as violacoes legiveis (lista vazia = ok). Roda no
    /// `dotnet test` (ContentValidatorTests). Cada regra e' uma funcao curta sobre os DADOS que recebe, nunca sobre a tabela direto:
    /// quando o conteudo virar dados (B-CORE-01), troca so a fonte em Validate() e as regras ficam. O UpgradeDef ja tem ID em string (A-CORE-04).
    /// ponytail: icone por ID, chave de analytics e localizacao nao existem no conteudo (nome e descricao sao texto pt-BR no
    /// Defs.cs); viram regras aqui quando o UpgradeDef/ItemDef ganharem esses campos (B-CORE-01).
    /// </summary>
    public static class ContentValidator
    {
        /// <summary>Maior nome de upgrade: Textos.DicaMax (26) menos "Compre os " (10), a dica mais curta do menu. A dica inteira e' do
        /// TextosTests: o Textos.cs fica na view e o Core nao o enxerga.</summary>
        public const int NameMax = 16;
        /// <summary>Os 15 do v0.1 seguem 50 x 1,30^tier; da 2a area em diante cada um tem preco proprio (Tuning.Cost).</summary>
        public const int FormulaTiers = (int)Upgrade.SideCorridor;

        /// <summary>Todas as regras. Sem argumentos = o conteudo real; o teste injeta uma COPIA quebrada no que quiser.</summary>
        public static List<string> Validate(UpgradeDef[] upgrades = null, long[] costs = null, int[] prices = null, MilestoneDef[] milestones = null, Sim world = null)
        {
            upgrades ??= Upgrades.All;
            costs ??= RealCosts();
            prices ??= Tuning.Padrao.Price;
            milestones ??= Balance.Milestones;
            world ??= World();
            var e = new List<string>();
            e.AddRange(Ids(upgrades));
            e.AddRange(Requisitos(upgrades));
            e.AddRange(Alcance(upgrades, world));
            e.AddRange(Luxo(upgrades, world));
            e.AddRange(Custos(costs));
            e.AddRange(Nomes(upgrades));
            e.AddRange(Receitas(prices, world));
            e.AddRange(Marcos(milestones, world));
            e.AddRange(Bocas(world));
            e.AddRange(Pisaveis(world));
            e.AddRange(Missoes(MetasDefs.Diarias, MetasDefs.Semanais, MetasDefs.Conquistas, MetasDefs.LoginDias, Tuning.Padrao));
            return e;
        }

        /// <summary>D-CORE-02: gatilho punitivo ou de anuncio nunca vira meta (LIVEOPS_V1 §5.2).</summary>
        public static readonly string[] EventosProibidos = { "client_left", "bottleneck", "vip_left", "boost_start", "rewarded_offer", "rewarded_start", "rewarded_complete", "ad_reward" };
        /// <summary>Minimos do GDD §23 #12 e teto de recompensa por tipo (ouro em s de renda, XP do Pass, Renome).</summary>
        static readonly (TipoMeta tipo, string prefixo, int minimo, int ouro, int xp, int renome)[] Faixas =
        {
            (TipoMeta.Diaria, "dly_", 30, 30, 150, 5), (TipoMeta.Semanal, "wkl_", 12, 90, 400, 10), (TipoMeta.Conquista, "ach_", 60, 60, 200, 10),
        };
        /// <summary>Jogo ativo de referencia (LIVEOPS_V1 §1/§6): diaria fecha em 15 min; semana do F2P = 7 x 20 min. Orcamento de ouro de LiveOps: 180 s/dia.</summary>
        public const int MinutosDiaria = 15, MinutosSemana = 7 * 20, OrcamentoDia = 180;

        /// <summary>
        /// D-CORE-02 (LIVEOPS_V1 §5.7): IDs unicos e no formato do tipo; volume minimo; evento que existe (Metas.Eventos) e nunca punitivo ou de
        /// anuncio; filtro, alvo, Abre e conta coerentes; recompensa dentro da faixa do tipo (ouro multiplo de 5) e nunca vazia; VIP (avanca com o
        /// anuncio "Chamar VIP") com alvo que fecha so com os VIPs naturais no tempo do tipo; pools A e B com 3+ abertas logo depois das
        /// Ferramentas; login de 7 dias com recompensa todo dia; orcamento de ouro por dia de calendario.
        /// </summary>
        public static IEnumerable<string> Missoes(MetaDef[] diarias, MetaDef[] semanais, MetaDef[] conquistas, LoginDia[] login, Tuning t)
        {
            var ids = new HashSet<string>();
            MetaDef[][] listas = { diarias, semanais, conquistas };
            for (int k = 0; k < listas.Length; k++)
            {
                var (tipo, prefixo, minimo, ouro, xp, renome) = Faixas[k];
                if (listas[k].Length < minimo) yield return $"meta: {listas[k].Length} do tipo {tipo}, o GDD §23 #12 pede pelo menos {minimo}";
                foreach (MetaDef d in listas[k])
                {
                    string at = $"meta {d.Id}:";
                    if (d.Tipo != tipo) yield return $"{at} tipo {d.Tipo} na lista de {tipo}";
                    if (d.Id == null || !d.Id.StartsWith(prefixo, StringComparison.Ordinal) || !Array.TrueForAll(d.Id.ToCharArray(), c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_'))
                        yield return $"{at} ID fora do formato {prefixo}[a-z0-9_]";
                    else if (!ids.Add(d.Id)) yield return $"{at} ID repetido";
                    if (Array.IndexOf(EventosProibidos, d.Evento) >= 0) yield return $"{at} gatilho {d.Evento} pune ou exige anuncio";
                    else if (Array.IndexOf(Metas.Eventos, d.Evento) < 0) yield return $"{at} gatilho {d.Evento} que o Sim nao emite";
                    int max = d.Evento == "upgrade_buy" ? Upgrades.Count - 1 : d.Evento == "mission_complete" ? 1
                        : d.Evento == "product_sold" || d.Evento == "product_crafted" || d.Evento == "order_done" || d.Evento == "vip_served" ? (int)Item.Jewel : -1;
                    if (d.Filtro < (d.Evento == "mission_complete" ? 0 : -1) || d.Filtro > max) yield return $"{at} filtro {d.Filtro} invalido para {d.Evento}";
                    if (d.Alvo <= 0) yield return $"{at} alvo {d.Alvo}";
                    if (d.Abre != null && Upgrades.IndexOf(d.Abre) < 0) yield return $"{at} abre com \"{d.Abre}\", que nao e' upgrade";
                    if (d.Conta == Conta.Segundos && !d.Evento.StartsWith("currency_", StringComparison.Ordinal)) yield return $"{at} alvo em segundos de renda fora de currency_*";
                    if (d.Conta == Conta.Distintos && (tipo != TipoMeta.Conquista || d.Evento != "product_sold")) yield return $"{at} Distintos so em conquista de venda";
                    if ((d.Pool >= 0 && d.Pool <= 2) != (tipo == TipoMeta.Diaria)) yield return $"{at} pool {d.Pool} (so diaria tem pool A/B/C)";
                    if (d.OuroS < 0 || d.OuroS > ouro || d.OuroS % 5 != 0 || d.Xp < 0 || d.Xp > xp || d.Renome < 0 || d.Renome > renome)
                        yield return $"{at} recompensa fora da faixa de {tipo} (ouro {d.OuroS} s <= {ouro} e x5, XP {d.Xp} <= {xp}, Renome {d.Renome} <= {renome})";
                    if (d.OuroS == 0 && d.Xp == 0 && d.Renome == 0 && d.Insignia == null) yield return $"{at} sem recompensa";
                    // "Chamar VIP" (anuncio) so acelera: o alvo fecha so com os naturais (1 a cada VipMax s no pior caso)
                    int teto = (int)((tipo == TipoMeta.Diaria ? MinutosDiaria : MinutosSemana) * 60f / t.VipMax);
                    if (d.Evento == "vip_served" && tipo != TipoMeta.Conquista && d.Alvo > teto)
                        yield return $"{at} {d.Alvo} VIPs nao fecham sem anuncio (naturais no tempo de referencia: {teto})";
                }
            }
            for (int pool = 0; pool <= 1; pool++)
            {
                int abertas = Array.FindAll(diarias, d => d.Pool == pool && (d.Abre == null || d.Abre == "ferramentas")).Length;
                if (abertas < 3) yield return $"meta: pool {"AB"[pool]} com {abertas} diarias abertas logo depois das Ferramentas (precisa de 3)";
            }
            if (login.Length != 7) yield return $"login: {login.Length} dias no ciclo (7)";
            foreach (LoginDia l in login)
                if (l.OuroS < 0 || l.OuroS > 60 || l.OuroS % 5 != 0 || l.Renome < 0 || l.Renome > 5 || l.OuroS + l.Renome == 0) yield return $"login: dia com ouro {l.OuroS} s e Renome {l.Renome} fora da faixa";
            int Mais(MetaDef[] ds) { int m = 0; foreach (MetaDef d in ds) m = Math.Max(m, d.OuroS); return m; }
            int loginOuro = 0;
            foreach (LoginDia l in login) loginOuro += l.OuroS;
            double dia = Metas.NDiarias * Mais(diarias) + Metas.NSemanais * Mais(semanais) / 7.0 + loginOuro / 7.0;
            if (dia > OrcamentoDia) yield return $"meta: ouro de LiveOps {dia:0} s por dia, acima de {OrcamentoDia} (LIVEOPS_V1 §1)";
        }

        public static long[] RealCosts()
        {
            var c = new long[Upgrades.Count]; var t = new Tuning();
            for (int i = 0; i < c.Length; i++) c[i] = t.Cost(i);
            return c;
        }

        /// <summary>Mundo com tudo comprado: todos os corpos possiveis (estacoes abertas, decoracao do luxo, balcao de 8 vagas).</summary>
        public static Sim World()
        {
            var w = new Sim();
            for (int u = 0; u < Upgrades.Count; u++) w.Bought[u] = true;
            w.Recompute();
            return w;
        }

        /// <summary>Um por enum, na ordem do enum (= indice do Sim e tier do custo). A-CORE-04: ID estavel nao vazio, so [a-z0-9_], unico; e todo
        /// ID do schema 1 (Sim.IdsV1, como o save da 0.1-0.7 migra) continua na tabela. O golden do ContentValidatorTests pega o resto.</summary>
        public static IEnumerable<string> Ids(UpgradeDef[] all)
        {
            int n = Enum.GetValues(typeof(Upgrade)).Length;
            if (all.Length != n || Upgrades.Count != n) yield return $"id: tabela com {all.Length}, Upgrades.Count = {Upgrades.Count}, enum com {n}";
            var seen = new HashSet<Upgrade>();
            var ids = new HashSet<string>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Up != (Upgrade)i) yield return $"id: tabela[{i}] e' {all[i].Up}, fora da ordem do enum (tier)";
                if (!seen.Add(all[i].Up)) yield return $"id: {all[i].Up} repetido";
                string id = all[i].Id;
                if (string.IsNullOrEmpty(id)) yield return $"id: {all[i].Up} sem ID estavel";
                else if (!Array.TrueForAll(id.ToCharArray(), c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_')) yield return $"id: \"{id}\" ({all[i].Up}) fora de [a-z0-9_]";
                else if (!ids.Add(id)) yield return $"id: \"{id}\" repetido";
            }
            foreach (string id in Sim.IdsV1)
                if (!ids.Contains(id)) yield return $"id: \"{id}\" do schema 1 sumiu da tabela (save da 0.1-0.7 perde a compra; renomear ou apagar pede passo em Sim.Migrations)";
        }

        /// <summary>Pre-requisito existe e e' de tier menor (o que ja impede ciclo na tabela em ordem).</summary>
        public static IEnumerable<string> Requisitos(UpgradeDef[] all)
        {
            for (int i = 0; i < all.Length; i++)
            {
                int r = all[i].Requires;
                if (r < -1 || r >= all.Length) yield return $"requisito: {all[i].Up} exige {r}, que nao existe";
                else if (r >= i) yield return $"requisito: {all[i].Up} exige {all[r].Up}, de tier igual ou maior";
            }
        }

        /// <summary>
        /// Fecho transitivo a partir do inicio (nada comprado), com as regras do Sim: a venda com o requisito comprado, no menu
        /// (MenuAvailable) ou num pad depois dos anteriores da cadeia (Pad.Current), e luxo so com toda a producao (Sim.Buy). O que
        /// sobra nunca abre: ciclo, requisito inalcancavel, produtivo que exige luxo, cadeia de pad fora de ordem ou sem pad nenhum.
        /// </summary>
        public static IEnumerable<string> Alcance(UpgradeDef[] all, Sim w)
        {
            int n = all.Length;
            var bought = new bool[n];
            bool OnSale(int u)
            {
                UpgradeDef d = all[u];
                if (d.Requires < -1 || d.Requires >= n || d.Requires >= 0 && !bought[d.Requires]) return false;
                if (d.Luxury) for (int i = 0; i < n; i++) if (!all[i].Luxury && !bought[i]) return false;
                if (d.InMenu) return true;
                foreach (Pad p in w.Pads)
                    foreach (Upgrade c in p.Chain)
                    {
                        if ((int)c == u) return true;
                        if ((int)c >= n || !bought[(int)c]) break;   // o pad mostra so o 1o nao comprado da cadeia
                    }
                return false;
            }
            for (bool grew = true; grew;)
            {
                grew = false;
                for (int u = 0; u < n; u++) if (!bought[u] && OnSale(u)) bought[u] = grew = true;
            }
            for (int u = 0; u < n; u++)
            {
                if (bought[u]) continue;
                Upgrade id = all[u].Up;
                bool hasPad = w.Pads.Exists(p => Array.IndexOf(p.Chain, id) >= 0);
                yield return all[u].InMenu || hasPad
                    ? $"alcance: {id} nunca fica a venda a partir do inicio (ciclo, requisito inalcancavel, produtivo que exige luxo ou cadeia do pad)"
                    : $"alcance: {id} sem pad e fora do menu, nunca a venda";
            }
        }

        /// <summary>Luxo (FASE3): marca bate com Upgrades.ProductionCount, so em pad, sempre com requisito e nunca abre estacao.</summary>
        public static IEnumerable<string> Luxo(UpgradeDef[] all, Sim w)
        {
            int prod = 0;
            foreach (UpgradeDef d in all)
            {
                if (!d.Luxury) { prod++; continue; }
                if (d.InMenu) yield return $"luxo: {d.Up} no menu (luxo e' de pad, depois de toda a producao)";
                if (d.Requires < 0) yield return $"luxo: {d.Up} sem requisito (o Sim.Buy le Bought[Requires] do luxo)";
            }
            if (prod != Upgrades.ProductionCount) yield return $"luxo: {prod} produtivos marcados, Upgrades.ProductionCount = {Upgrades.ProductionCount}";
            foreach (Station s in w.Stations)
                if (s.UnlockBy >= 0 && s.UnlockBy < all.Length && all[s.UnlockBy].Luxury) yield return $"luxo: {all[s.UnlockBy].Up} abre a estacao {s.Name} (luxo nao produz)";
        }

        /// <summary>Custo positivo, multiplo de 5 (preco legivel) e subindo tier a tier na parte da formula.</summary>
        public static IEnumerable<string> Custos(long[] cost)
        {
            for (int i = 0; i < cost.Length; i++)
            {
                if (cost[i] <= 0) yield return $"custo: {(Upgrade)i} = {cost[i]}";
                else if (cost[i] % 5 != 0) yield return $"custo: {(Upgrade)i} = {cost[i]}, fora do multiplo de 5";
                if (i > 0 && i < FormulaTiers && cost[i] <= cost[i - 1]) yield return $"custo: {(Upgrade)i} = {cost[i]} nao sobe do tier anterior ({cost[i - 1]})";
            }
        }

        public static IEnumerable<string> Nomes(UpgradeDef[] all)
        {
            foreach (UpgradeDef d in all)
            {
                if (string.IsNullOrWhiteSpace(d.Name)) yield return $"texto: {d.Up} sem nome";
                else if (d.Name.Length > NameMax) yield return $"texto: {d.Up} \"{d.Name}\" passa de {NameMax} caracteres (nao cabe na dica do menu)";
                if (string.IsNullOrWhiteSpace(d.Desc)) yield return $"texto: {d.Up} sem descricao";
            }
        }

        /// <summary>Cadeia de itens: o deposito faz minerio, estacao que produz troca InItem por OutItem, balcao vende produto.</summary>
        public static IEnumerable<string> Receitas(int[] price, Sim w)
        {
            bool Made(Item i) => w.Stations.Exists(s => s.Kind == Kind.Deposit ? i == Item.Ore : s.Produces && s.OutItem == i);
            bool Used(Item i) => w.Stations.Exists(s => s.Produces && s.InItem == i);
            bool Sold(Item i) => w.Stations.Exists(s => w.Sells(s, i));
            foreach (Item i in Enum.GetValues(typeof(Item)))
            {
                if (Sim.IsProduct(i) && ((int)i >= price.Length || price[(int)i] <= 0)) yield return $"receita: {i} sem preco";
                if (Sim.IsProduct(i) && !Made(i)) yield return $"receita: {i} sem estacao que produz";
                if (Made(i) && !Used(i) && !Sold(i)) yield return $"receita: {i} produzido sem comprador (nenhuma estacao usa, nenhum balcao vende)";
            }
            foreach (Station s in w.Stations)
            {
                if (!s.Produces) continue;
                if (s.Need <= 0) yield return $"receita: {s.Name} sem insumo (Need {s.Need})";
                if (!Made(s.InItem)) yield return $"receita: {s.Name} usa {s.InItem}, que nenhuma estacao produz";
            }
        }

        /// <summary>Bau de marco: paga ouro, tem rotulo e a condicao da para bater (vender um produto que alguma estacao faz).</summary>
        public static IEnumerable<string> Marcos(MilestoneDef[] ms, Sim w)
        {
            for (int k = 0; k < ms.Length; k++)
            {
                MilestoneDef m = ms[k];
                if (m.Gold <= 0) yield return $"marco {k}: ouro {m.Gold}";
                if (string.IsNullOrWhiteSpace(m.Label)) yield return $"marco {k}: sem rotulo";
                bool item = m.Item == -1 || Sim.IsProduct((Item)m.Item) && w.Stations.Exists(s => s.Produces && (int)s.OutItem == m.Item);
                if (m.Count <= 0 || !item) yield return $"marco {k}: condicao inalcancavel (vender {m.Count} do item {m.Item})";
            }
        }

        /// <summary>Bocas (FASE5 §2b), no mundo com tudo comprado: dentro das paredes, fora de outro corpo, longe dos pads e baus e
        /// sem zona sobreposta a de outra estacao. O pad no lugar da propria estacao some quando ela abre.</summary>
        public static IEnumerable<string> Bocas(Sim w)
        {
            var marks = new List<(string name, V2 pos)>();
            foreach (Pad p in PadsInWorld(w)) marks.Add(("pad " + p.Chain[0], p.Pos));
            foreach (Chest c in w.Chests) marks.Add(("bau " + c.Index, c.Pos));
            foreach (Station st in w.Stations)
                foreach (V2 m in st.Produces ? new[] { st.InAt, st.OutAt } : new[] { st.InAt })
                {
                    string at = $"boca: {st.Name} ({m})";
                    if (!InWalls(m)) yield return at + " fora das paredes";
                    if (Clearance(w, m) < Balance.CharRadius - 1e-4f) yield return at + " dentro de um corpo";
                    foreach (var (name, pos) in marks)
                        if (V2.Dist(pos, st.Pos) >= 0.01f && V2.Dist(m, pos) < Balance.MouthRadius + Balance.PadRadius) yield return $"{at} na zona do {name}";
                    foreach (Station o in w.Stations)
                        if (o != st && Math.Min(V2.Dist(m, o.InAt), V2.Dist(m, o.OutAt)) <= 2f * Balance.MouthRadius) yield return $"{at} com a zona sobreposta a de {o.Name}";
                }
        }

        /// <summary>Onde alguem pisa (pad que aparece no mundo, bau, ponto de contratacao) cabe o personagem, dentro das paredes; vaga de
        /// cliente nao cai DENTRO de corpo (pode encostar).</summary>
        public static IEnumerable<string> Pisaveis(Sim w)
        {
            var spots = new List<(string name, V2 pos)>();
            foreach (Pad p in PadsInWorld(w))
                if (!w.Stations.Exists(st => V2.Dist(st.Pos, p.Pos) < 0.01f)) spots.Add(("pad " + p.Chain[0], p.Pos));
            foreach (Chest c in w.Chests) spots.Add(("bau " + c.Index, c.Pos));
            spots.Add(("ponto de contratacao", Sim.HireSpot));
            foreach (var (name, p) in spots)
            {
                if (!InWalls(p)) yield return $"pisavel: {name} ({p}) fora das paredes";
                if (Clearance(w, p) < Balance.CharRadius) yield return $"pisavel: {name} ({p}) dentro de um corpo";
            }
            for (int i = 0; i <= Balance.QueueCapMax; i++) if (Within(w, w.ClientSlot(i))) yield return $"pisavel: vaga {i} do balcao dentro de um corpo";
            for (int i = 0; i <= Balance.JewelQueueCapUp; i++) if (Within(w, w.JewelSlot(i))) yield return $"pisavel: vaga {i} da loja de joias dentro de um corpo";
        }

        /// <summary>Pads que ainda podem aparecer no mundo: cadeia com pelo menos um upgrade fora do menu.</summary>
        static List<Pad> PadsInWorld(Sim w) => w.Pads.FindAll(p => Array.Exists(p.Chain, u => !Upgrades.All[(int)u].InMenu));

        static bool InWalls(V2 p) => p.X >= 0.3f && p.X <= Balance.WorldW - 0.3f && p.Y >= 0.3f && p.Y <= Balance.WorldH - 0.3f;   // limites do Sim.Clamp

        /// <summary>Menor distancia de `p` a um corpo solido (0 = dentro).</summary>
        static float Clearance(Sim w, V2 p)
        {
            float d = float.MaxValue;
            foreach (Box b in w.Solids) d = Math.Min(d, b.Dist(p));
            return d;
        }

        static bool Within(Sim w, V2 p) => w.Solids.Exists(b => Math.Abs(p.X - b.Pos.X) < b.Half.X && Math.Abs(p.Y - b.Pos.Y) < b.Half.Y);
    }
}
