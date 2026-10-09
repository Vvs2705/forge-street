using System;
using System.Collections.Generic;

namespace FS.Core
{
    /// <summary>
    /// A-CORE-05 (DOCUMENTO_MESTRE §108): confere o conteudo do jogo e devolve as violacoes legiveis (lista vazia = ok). Roda no
    /// `dotnet test` (ContentValidatorTests). Cada regra e' uma funcao curta sobre os DADOS que recebe, nunca sobre a tabela direto:
    /// quando o conteudo virar dados com ID em string (A-CORE-04), troca so a fonte em Validate() e as regras ficam.
    /// ponytail: icone por ID, chave de analytics e localizacao nao existem no conteudo (nome e descricao sao texto pt-BR no
    /// Defs.cs); viram regras aqui quando o UpgradeDef/ItemDef ganharem esses campos (A-CORE-04, B-CORE-01).
    /// </summary>
    public static class ContentValidator
    {
        /// <summary>Maior nome de upgrade: Textos.DicaMax (26) menos "Compre os " (10), a dica mais curta do menu. A dica inteira e' do
        /// TextosTests: o Textos.cs fica na view e o Core nao o enxerga.</summary>
        public const int NameMax = 16;
        /// <summary>Os 15 do v0.1 seguem 50 x 1,30^tier; da 2a area em diante cada um tem preco proprio (Upgrades.Cost).</summary>
        public const int FormulaTiers = (int)Upgrade.SideCorridor;

        /// <summary>Todas as regras. Sem argumentos = o conteudo real; o teste injeta uma COPIA quebrada no que quiser.</summary>
        public static List<string> Validate(UpgradeDef[] upgrades = null, int[] costs = null, int[] prices = null, MilestoneDef[] milestones = null, Sim world = null)
        {
            upgrades ??= Upgrades.All;
            costs ??= RealCosts();
            prices ??= Balance.Price;
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
            return e;
        }

        public static int[] RealCosts()
        {
            var c = new int[Upgrades.Count];
            for (int i = 0; i < c.Length; i++) c[i] = Upgrades.Cost(i);
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

        /// <summary>Um por ID, na ordem do enum (= tier; o save e' por indice).</summary>
        public static IEnumerable<string> Ids(UpgradeDef[] all)
        {
            int n = Enum.GetValues(typeof(Upgrade)).Length;
            if (all.Length != n || Upgrades.Count != n) yield return $"id: tabela com {all.Length}, Upgrades.Count = {Upgrades.Count}, enum com {n}";
            var seen = new HashSet<Upgrade>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Id != (Upgrade)i) yield return $"id: tabela[{i}] e' {all[i].Id}, fora da ordem do enum (tier)";
                if (!seen.Add(all[i].Id)) yield return $"id: {all[i].Id} repetido";
            }
        }

        /// <summary>Pre-requisito existe e e' de tier menor (o que ja impede ciclo na tabela em ordem).</summary>
        public static IEnumerable<string> Requisitos(UpgradeDef[] all)
        {
            for (int i = 0; i < all.Length; i++)
            {
                int r = all[i].Requires;
                if (r < -1 || r >= all.Length) yield return $"requisito: {all[i].Id} exige {r}, que nao existe";
                else if (r >= i) yield return $"requisito: {all[i].Id} exige {all[r].Id}, de tier igual ou maior";
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
                Upgrade id = all[u].Id;
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
                if (d.InMenu) yield return $"luxo: {d.Id} no menu (luxo e' de pad, depois de toda a producao)";
                if (d.Requires < 0) yield return $"luxo: {d.Id} sem requisito (o Sim.Buy le Bought[Requires] do luxo)";
            }
            if (prod != Upgrades.ProductionCount) yield return $"luxo: {prod} produtivos marcados, Upgrades.ProductionCount = {Upgrades.ProductionCount}";
            foreach (Station s in w.Stations)
                if (s.UnlockBy >= 0 && s.UnlockBy < all.Length && all[s.UnlockBy].Luxury) yield return $"luxo: {all[s.UnlockBy].Id} abre a estacao {s.Name} (luxo nao produz)";
        }

        /// <summary>Custo positivo, multiplo de 5 (preco legivel) e subindo tier a tier na parte da formula.</summary>
        public static IEnumerable<string> Custos(int[] cost)
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
                if (string.IsNullOrWhiteSpace(d.Name)) yield return $"texto: {d.Id} sem nome";
                else if (d.Name.Length > NameMax) yield return $"texto: {d.Id} \"{d.Name}\" passa de {NameMax} caracteres (nao cabe na dica do menu)";
                if (string.IsNullOrWhiteSpace(d.Desc)) yield return $"texto: {d.Id} sem descricao";
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
