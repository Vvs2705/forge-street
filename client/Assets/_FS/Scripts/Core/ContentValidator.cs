using System;
using System.Collections.Generic;

namespace FS.Core
{
    /// <summary>
    /// A-CORE-05 (DOCUMENTO_MESTRE §108): confere o conteudo do jogo e devolve as violacoes legiveis (lista vazia = ok). Roda no
    /// `dotnet test` (ContentValidatorTests). Cada regra e' uma funcao curta sobre os DADOS que recebe, nunca sobre a tabela direto. O
    /// UpgradeDef tem ID em string (A-CORE-04); itens e receitas sao dados (B-CORE-01) e entram pelo mundo (Sim com o conteudo injetado).
    /// ponytail: icone por ID, chave de analytics e localizacao nao existem no conteudo (nome e descricao sao texto pt-BR no
    /// Defs.cs); viram regras aqui quando o UpgradeDef/ItemDef ganharem esses campos.
    /// </summary>
    public static class ContentValidator
    {
        /// <summary>Maior nome de upgrade: Textos.DicaMax (26) menos "Compre os " (10), a dica mais curta do menu. A dica inteira e' do
        /// TextosTests: o Textos.cs fica na view e o Core nao o enxerga.</summary>
        public const int NameMax = 16;
        /// <summary>Os 15 do v0.1 seguem 50 x 1,30^tier; da 2a area em diante cada um tem preco proprio (Tuning.Cost).</summary>
        public const int FormulaTiers = (int)Upgrade.SideCorridor;

        /// <summary>Todas as regras. Sem argumentos = o conteudo real; o teste injeta uma COPIA quebrada no que quiser. `items`/`recipes` =
        /// conteudo do mundo (World); com `world` dado, valem os dele.</summary>
        public static List<string> Validate(UpgradeDef[] upgrades = null, long[] costs = null, int[] prices = null, MilestoneDef[] milestones = null, Sim world = null,
            ItemDef[] items = null, RecipeDef[] recipes = null, RenomeDef[] renome = null)
        {
            upgrades ??= Upgrades.All;
            costs ??= RealCosts();
            prices ??= Tuning.Padrao.Price;
            milestones ??= Balance.Milestones;
            world ??= World(items, recipes);
            var e = new List<string>();
            e.AddRange(Ids(upgrades));
            e.AddRange(Itens(world.ItemDefs));
            e.AddRange(Requisitos(upgrades));
            e.AddRange(Alcance(upgrades, world));
            e.AddRange(Luxo(upgrades, world));
            e.AddRange(Custos(costs));
            e.AddRange(Nomes(upgrades));
            e.AddRange(Receitas(prices, world));
            e.AddRange(Marcos(milestones, world));
            e.AddRange(Bocas(world));
            e.AddRange(Pisaveis(world));
            e.AddRange(Qualidade(upgrades, world));
            e.AddRange(Missoes(MetasDefs.Diarias, MetasDefs.Semanais, MetasDefs.Conquistas, MetasDefs.LoginDias, Tuning.Padrao));
            e.AddRange(NiveisRenome(renome ?? RenomeDefs.Niveis));
            e.AddRange(LiveOpsDefs.Validar(LiveOpsDefs.Eventos, LiveOpsDefs.Passes));   // D-CORE-03/04 (LIVEOPS_V1 §5.7)
            return e;
        }

        /// <summary>B-CORE-06 (GDD §23 #11): o 0 (limiar 0) e 10 niveis, na ordem, com limiar crescente; cada nivel de 1 a 10 libera algo; ID de
        /// desbloqueio em [a-z0-9_] e liberado por um nivel so (Renome.Libera nao fica ambiguo).</summary>
        public static IEnumerable<string> NiveisRenome(RenomeDef[] niveis)
        {
            if (niveis.Length != 11) yield return $"renome: {niveis.Length} niveis na tabela (0 e mais 10)";
            var ids = new HashSet<string>();
            for (int i = 0; i < niveis.Length; i++)
            {
                RenomeDef d = niveis[i];
                if (d.Nivel != i) yield return $"renome: nivel {d.Nivel} na posicao {i}";
                if (i == 0 && d.Limiar != 0) yield return $"renome: o nivel 0 comeca em {d.Limiar}, nao em 0";
                if (i > 0 && d.Limiar <= niveis[i - 1].Limiar) yield return $"renome: limiar do nivel {i} ({d.Limiar}) nao sobe do anterior ({niveis[i - 1].Limiar})";
                if (i > 0 && d.Libera.Length == 0) yield return $"renome: nivel {i} nao libera nada";
                foreach (string id in d.Libera)
                    if (string.IsNullOrEmpty(id) || !IdOk(id)) yield return $"renome: nivel {i} libera \"{id}\", fora de [a-z0-9_]";
                    else if (!ids.Add(id)) yield return $"renome: \"{id}\" liberado por mais de um nivel";
            }
        }

        /// <summary>
        /// B-CORE-03 (ECONOMIA_WAVE_B §1): um preco por nivel de Quality, Normal = 100%, crescente nos que vendem e 0 so na Masterwork (nao vende,
        /// vai ao Hall); q por nivel de estacao positivo. Nivel de estacao (Meta) com fonte conhecida: a familia dele tem bancada que faz produto
        /// no mundo; no menu, nunca luxo nem abrindo estacao, e a cadeia de niveis da mesma familia (o 2 exige um produtivo; o 3, o 2).
        /// </summary>
        public static IEnumerable<string> Qualidade(UpgradeDef[] all, Sim w)
        {
            int[] pct = w.T.QualidadePct;
            int n = (int)Quality.Masterwork + 1;
            if (pct == null || pct.Length != n) { yield return $"qualidade: {pct?.Length ?? 0} precos para {n} niveis"; yield break; }
            if (pct[0] != 100) yield return $"qualidade: Normal a {pct[0]}% (tem de ser 100%)";
            for (int l = 1; l < Sim.Graus; l++) if (pct[l] <= pct[l - 1]) yield return $"qualidade: {(Quality)l} a {pct[l]}% nao sobe de {(Quality)(l - 1)} ({pct[l - 1]}%)";
            if (pct[n - 1] != 0) yield return $"qualidade: Masterwork a {pct[n - 1]}% (nao vende: 0)";
            if (w.T.NivelEstacaoQ <= 0) yield return $"qualidade: {w.T.NivelEstacaoQ} pontos por nivel de estacao";
            foreach (UpgradeDef d in all)
            {
                if (!d.Meta) continue;
                if (!w.Stations.Exists(s => s.Produces && s.Recipe.Station == d.Station && w.IsProduct(s.OutItem)))
                    yield return $"qualidade: {d.Up} e' nivel de \"{d.Station}\", familia sem bancada de produto (fonte desconhecida)";
                if (!d.InMenu || d.Luxury) yield return $"qualidade: {d.Up} (Meta) fora do menu ou marcado como luxo";
                if (w.Stations.Exists(s => s.UnlockBy == (int)d.Up)) yield return $"qualidade: {d.Up} (Meta) abre estacao";
                UpgradeDef r = d.Requires >= 0 && d.Requires < all.Length ? all[d.Requires] : null;
                if (r == null || r.Meta && r.Station != d.Station) yield return $"qualidade: {d.Up} fora da cadeia de niveis de \"{d.Station}\"";
            }
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
                        : d.Evento == "product_sold" || d.Evento == "product_crafted" || d.Evento == "order_done" || d.Evento == "vip_served" ? Items.All.Length - 1 : -1;
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

        /// <summary>Mundo com tudo comprado: todos os corpos possiveis (estacoes abertas, decoracao do luxo, balcao de 8 vagas). `items`/`recipes`
        /// = conteudo injetado (null = o padrao).</summary>
        public static Sim World(ItemDef[] items = null, RecipeDef[] recipes = null)
        {
            var w = new Sim(null, items, recipes);
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
                else if (!IdOk(id)) yield return $"id: \"{id}\" ({all[i].Up}) fora de [a-z0-9_]";
                else if (!ids.Add(id)) yield return $"id: \"{id}\" repetido";
            }
            foreach (string id in Sim.IdsV1)
                if (!ids.Contains(id)) yield return $"id: \"{id}\" do schema 1 sumiu da tabela (save da 0.1-0.7 perde a compra; renomear ou apagar pede passo em Sim.Migrations)";
        }

        static bool IdOk(string id) => Array.TrueForAll(id.ToCharArray(), c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_');

        /// <summary>B-CORE-01: ID estavel nao vazio, so [a-z0-9_], unico (o save grava, schema 3); as primeiras posicoes sao as do enum Item e
        /// do espelho do save (Sim.ItemIdsV2, congelada); nome e tier 1-5 (GDD §7).</summary>
        public static IEnumerable<string> Itens(ItemDef[] all)
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < all.Length; i++)
            {
                string id = all[i].Id;
                if (string.IsNullOrEmpty(id) || !IdOk(id)) yield return $"item {i}: ID \"{id}\" vazio ou fora de [a-z0-9_]";
                else if (!ids.Add(id)) yield return $"item: \"{id}\" repetido";
                if (i < Sim.ItemIdsV2.Length && id != Sim.ItemIdsV2[i])
                    yield return $"item {i}: \"{id}\" no lugar de \"{Sim.ItemIdsV2[i]}\" (o enum Item e o espelho do save sao essa posicao; renomear pede passo em Sim.Migrations)";
                if (string.IsNullOrWhiteSpace(all[i].Name)) yield return $"item: {id} sem nome";
                if (all[i].Tier < 1 || all[i].Tier > 5) yield return $"item: {id} com tier {all[i].Tier} (1-5)";
            }
            if (all.Length < Sim.ItemIdsV2.Length) yield return $"item: tabela com {all.Length}, o enum Item tem {Sim.ItemIdsV2.Length}";
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
                if (d.Luxury) for (int i = 0; i < n; i++) if (!all[i].Luxury && !all[i].Meta && !bought[i]) return false;   // luxo espera so os produtivos
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
                if (!d.Luxury) { if (!d.Meta) prod++; continue; }   // Meta (B-CORE-03) nao e' produtivo nem luxo
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

        /// <summary>
        /// B-CORE-01, cadeia em dados (w.RecipeDefs): o deposito faz minerio; estacao que produz troca os insumos da receita da familia dela
        /// pelo produto; balcao vende produto. Fornalha e bancada tem receita do tipo delas; toda receita tem estacao e insumo; todo insumo e'
        /// produzido por alguem e sai do minerio sem ciclo; produto tem preco e estacao; o que e' produzido tem destino.
        /// </summary>
        public static IEnumerable<string> Receitas(int[] price, Sim w)
        {
            string Name(Item i) => (uint)i < (uint)w.ItemDefs.Length ? w.ItemDefs[(int)i].Id : ((int)i).ToString();
            bool Made(Item i) => w.Stations.Exists(s => s.Kind == Kind.Deposit ? i == Item.Ore : s.Produces && s.OutItem == i);
            bool Used(Item i) => w.Stations.Exists(s => s.InIndex(i) >= 0);
            bool Sold(Item i) => w.Stations.Exists(s => w.Sells(s, i));
            for (int k = 0; k < w.ItemDefs.Length; k++)
            {
                Item i = (Item)k;
                if (w.IsProduct(i) && (k >= price.Length || price[k] <= 0)) yield return $"receita: {Name(i)} sem preco";
                if (w.IsProduct(i) && !Made(i)) yield return $"receita: {Name(i)} sem estacao que produz";
                if (Made(i) && !Used(i) && !Sold(i)) yield return $"receita: {Name(i)} produzido sem comprador (nenhuma estacao usa, nenhum balcao vende)";
            }
            foreach (Station s in w.Stations)
                if (!s.Produces && (s.Kind == Kind.Furnace || s.Kind == Kind.Crafter)) yield return $"receita: {s.Name} sem receita (nenhuma da familia dela)";
                else if (s.Produces && s.Recipe.Kind != s.Kind) yield return $"receita: {s.Name} ({s.Kind}) com receita de {s.Recipe.Kind}";
            // fecho a partir do minerio: o que as estacoes fazem quando todos os insumos ja sao alcancaveis (pega o ciclo)
            var reach = new HashSet<Item>();
            if (Made(Item.Ore)) reach.Add(Item.Ore);
            for (bool grew = true; grew;)
            {
                grew = false;
                foreach (Station s in w.Stations)
                    if (s.Produces && !reach.Contains(s.OutItem) && Array.TrueForAll(s.Recipe.Inputs, x => reach.Contains(x.Item))) { reach.Add(s.OutItem); grew = true; }
            }
            foreach (RecipeDef r in w.RecipeDefs)
            {
                if (!w.Stations.Exists(s => s.Recipe == r)) yield return $"receita: {Name(r.Output)} sem estacao da familia {r.Station}";
                if (r.Inputs.Length == 0) yield return $"receita: {Name(r.Output)} sem insumo";
                foreach ((Item item, int qty) in r.Inputs)
                {
                    if (qty <= 0) yield return $"receita: {Name(r.Output)} pede {qty} de {Name(item)}";
                    if (!Made(item)) yield return $"receita: {Name(r.Output)} usa {Name(item)}, que nenhuma estacao produz";
                    else if (!reach.Contains(item)) yield return $"receita: {Name(r.Output)} usa {Name(item)}, que nunca sai do minerio (ciclo)";
                }
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
                bool item = m.Item == -1 || w.IsProduct((Item)m.Item) && w.Stations.Exists(s => s.Produces && (int)s.OutItem == m.Item);
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
