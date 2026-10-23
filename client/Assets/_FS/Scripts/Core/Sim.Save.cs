using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Save/Load do Sim (chave=valor, schema 3 por ID, migracao dos saves antigos). Parte movida do Sim.cs sem mudar codigo.</summary>
    public sealed partial class Sim
    {
        // ------------------------------------------------------------------ save/load (chave=valor; lixo do payload vira estado inicial;
        // soma, schema e versao do jogo ficam no SaveEnvelope)

        /// <summary>A-CORE-01: versao do payload (linha v=). Subir = anexar o passo em Migrations (Migracao_TodaVersaoAnteriorTemPasso cobra).
        /// Campo novo que nao muda o significado de save antigo (hold=, vip=, ord=) nao sobe. 2 = A-CORE-04 (upgrades e parcial dos pads por ID):
        /// upgrade novo, em qualquer lugar do enum, nao sobe mais o schema. 3 = B-CORE-01 (estoque, vendas e maos por ID de item). B-CORE-03 so
        /// anexou chaves (stocksq=, holdsq=, wksq=, qstN=, golpe=): sem elas o save e' o de sempre, tudo Normal.</summary>
        public const int Schema = 3;
        /// <summary>Migracoes sobre o texto bruto, antes do parse: Migrations[i] leva o schema i+1 ao i+2. Schema 1 = 0.1-0.7; 2 = 0.8.</summary>
        public static readonly Func<string, string>[] Migrations = { V1ParaV2, V2ParaV3 };

        /// <summary>B-CORE-01: itens na ordem do enum Item ate o schema 2, CONGELADA: e' como o stock=/sold=/hold=/wk= por posicao se le (e o
        /// espelho que o save ainda grava para a 0.5). Nunca mude nem reordene; o ContentValidator.Itens cobra que sao as 6 primeiras da tabela.</summary>
        public static readonly string[] ItemIdsV2 = { "minerio", "lingote", "espada", "escudo", "ferramenta", "joia" };

        /// <summary>A-CORE-04: ordem do enum na 0.1-0.7 (schema 1), CONGELADA: e' como o up= daquelas versoes se le. Nunca mude nem reordene; o
        /// ContentValidator.Ids cobra que todos continuam na tabela.</summary>
        public static readonly string[] IdsV1 =
        {
            "fole", "bigorna2", "ajudante1", "escudos", "mochila", "ajudante2", "esteira", "fole_duplo", "botas", "ferramentas", "vitrine", "fornalha2",
            "ajudante3", "ajudantes_ageis", "martelo_veloz", "corredor", "joalheria", "joalheiro1", "lupa", "vitrine_joias", "fachada_nobre",
            "piso_oficina", "joalheria_real", "mineiro", "joalheiro2", "balcao5", "balcao6", "balcao7", "balcao8",
        };
        /// <summary>Pads do schema 1 por slot (cadeia de IDs, CONGELADA): o pads= da 0.1-0.7 e' o parcial por indice de slot.</summary>
        static readonly string[] PadsV1 =
        {
            "fole fole_duplo", "fornalha2", "bigorna2", "escudos", "ferramentas", "ajudante1 ajudante2 ajudante3", "ajudantes_ageis", "mochila", "botas",
            "esteira", "martelo_veloz", "vitrine", "corredor", "joalheria", "joalheiro1", "lupa", "vitrine_joias", "fachada_nobre", "piso_oficina",
            "joalheria_real", "mineiro", "joalheiro2",
        };

        /// <summary>
        /// Schema 1 -> 2: up= (flags na ordem IdsV1), ut= (instante por indice) e pads= (parcial por slot) viram ups=id:instante e upp=id:pago,
        /// pelas listas congeladas. O parcial do slot vai para o 1o upgrade nao comprado da cadeia (toda comprada: o ultimo; o Load devolve o
        /// ouro do pad que nao aparece). O resto do texto passa intacto; lixo segue adiante e o Load le com o default.
        /// </summary>
        static string V1ParaV2(string text)
        {
            string up = "", ut = "", pads = "";
            var rest = new StringBuilder();
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                string key = eq > 0 ? raw.Substring(0, eq).Trim() : "", val = eq > 0 ? raw.Substring(eq + 1).Trim() : "";
                if (key == "up") up = val; else if (key == "ut") ut = val; else if (key == "pads") pads = val; else rest.Append(raw).Append('\n');
            }
            string[] t = ut.Split(','), paid = pads.Split(',');
            var ups = new List<string>(); var bought = new HashSet<string>();
            for (int i = 0; i < Math.Min(up.Length, IdsV1.Length); i++)
                if (up[i] == '1') { bought.Add(IdsV1[i]); ups.Add(IdsV1[i] + ":" + (i < t.Length ? t[i].Trim() : "-1")); }
            var upp = new List<string>();
            for (int i = 0; i < Math.Min(paid.Length, PadsV1.Length); i++)
            {
                string[] chain = PadsV1[i].Split(' ');
                upp.Add((Array.Find(chain, c => !bought.Contains(c)) ?? chain[chain.Length - 1]) + ":" + paid[i].Trim());
            }
            return rest.Append("ups=").Append(string.Join(",", ups)).Append("\nupp=").Append(string.Join(",", upp)).Append('\n').ToString();
        }

        /// <summary>
        /// Schema 2 -> 3 (B-CORE-01): por posicao vira id:n pela lista congelada ItemIdsV2. stock=/sold= (a partir da espada) -> stocks=/solds=,
        /// hold= -> holds=, wk=papel:contagens;... -> wks=papel:id:n,...;... (o da 0.5.0, sem papel, vira ":id:n" e segue pela posicao do
        /// ajudante). O valor passa cru: o Load corta lixo, negativo e teto como antes. O resto do texto passa intacto.
        /// </summary>
        static string V2ParaV3(string text)
        {
            var sb = new StringBuilder();
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                string key = eq > 0 ? raw.Substring(0, eq).Trim() : "", val = eq > 0 ? raw.Substring(eq + 1).Trim() : "";
                if (key == "stock" || key == "sold") sb.Append(key).Append("s=").Append(IdTokens(val, 2));
                else if (key == "hold") sb.Append("holds=").Append(IdTokens(val, 0));
                else if (key == "wk")
                    sb.Append("wks=").Append(string.Join(";", Array.ConvertAll(val.Split(';'), w =>
                    {
                        int c = w.IndexOf(':');
                        return (c > 0 ? w.Substring(0, c) : "") + ":" + IdTokens(w.Substring(c + 1), 0);
                    })));
                else sb.Append(raw);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>"a,b,c" por posicao a partir do item `from` da lista congelada -> "id:a,id:b,id:c".</summary>
        static string IdTokens(string counts, int from)
        {
            string[] p = counts.Split(',');
            var t = new List<string>();
            for (int i = 0; i < p.Length && from + i < ItemIdsV2.Length; i++) t.Add(ItemIdsV2[from + i] + ":" + p[i].Trim());
            return string.Join(",", t);
        }

        /// <summary>A-CORE-04: IDs do save que este build nao conhece (conteudo de um build mais novo, depois de um downgrade): ficam de fora no
        /// Load (o parcial de pad volta ao ouro) e o Salvamento loga save_ids_desconhecidos. Nao vai no save: regravar os descarta.</summary>
        public int UnknownIds;

        /// <summary>Espelho por posicao para o downgrade (hold=/wk=): so as posicoes da lista congelada ItemIdsV2.</summary>
        static string EspelhoV2(int[] a) => string.Join(",", Array.ConvertAll(a, x => x.ToString(CultureInfo.InvariantCulture)), 0, Math.Min(a.Length, ItemIdsV2.Length));

        /// <summary>B-CORE-01: contagem por handle -> "id:n,..." (so o que nao e' zero).</summary>
        string IdCounts(int[] a)
        {
            var t = new List<string>();
            for (int i = 0; i < a.Length; i++) if (a[i] != 0) t.Add(ItemDefs[i].Id + ":" + a[i].ToString(CultureInfo.InvariantCulture));
            return string.Join(",", t);
        }

        /// <summary>B-CORE-01: "id:n,..." -> into[handle] = n (negativo e lixo = 0); ID que o conteudo nao tem conta em UnknownIds.</summary>
        static void ReadIdCounts(Sim sim, string[] tokens, int[] into)
        {
            foreach (string tok in tokens)
            {
                string[] kv = tok.Split(':');
                int i = Items.IndexOf(kv[0].Trim(), sim.ItemDefs);
                if (i >= 0) into[i] = Math.Max(0, kv.Length > 1 ? I(kv[1], 0) : 0);
                else if (kv[0].Trim().Length > 0) sim.UnknownIds++;
            }
        }

        /// <summary>B-CORE-03: lotes -> "id:fina:superior,..." (so os itens com Fina ou Superior), ja cortados ao total (Fit).</summary>
        string IdQ(int[] totals, int[][] q)
        {
            var t = new List<string>();
            for (int i = 0; i < q.Length; i++)
            {
                Fit(totals[i], q[i]);
                bool any = false;
                var tok = new StringBuilder(ItemDefs[i].Id);
                for (int l = 1; l < Graus; l++) { tok.Append(':').Append(q[i][l]); any |= q[i][l] > 0; }
                if (any) t.Add(tok.ToString());
            }
            return string.Join(",", t);
        }

        /// <summary>B-CORE-03: "id:fina:superior,..." -> into[handle][nivel] (negativo e lixo = 0; ID desconhecido fica de fora: o total dele ja
        /// contou no UnknownIds). O Load corta ao total depois (Fit).</summary>
        static void ReadIdQ(Sim sim, string[] tokens, int[][] into)
        {
            foreach (string tok in tokens)
            {
                string[] kv = tok.Split(':');
                int i = Items.IndexOf(kv[0].Trim(), sim.ItemDefs);
                if (i >= 0) for (int l = 1; l < Graus && l < kv.Length; l++) into[i][l] = Math.Max(0, I(kv[l], 0));
            }
        }

        public string Save(long nowUnix)
        {
            // A-CORE-02: SavedAt = maior instante ja visto. Relogio que voltou nao regrava o SavedAt para tras: adiantar -> resgatar -> voltar
            // -> adiantar nao gera claim novo, e voltar -> fechar -> acertar a hora nao paga tempo que nao passou
            long back = SavedAt - nowUnix;
            ClockWentBack = back > 0 && !_clockBehind ? back : 0;
            _clockBehind = back > 0;
            SavedAt = Math.Max(SavedAt, nowUnix);
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("v=").Append(Schema).Append('\n');
            sb.Append("t=").Append(Time.ToString("0.###", ci)).Append('\n');
            sb.Append("gold=").Append(Gold).Append('\n');
            // A-CORE-04: comprados por ID estavel (id:instante da compra em s de jogo; -1 = desconhecido), na ordem da tabela; o Load nao depende dela
            var ups = new List<string>();
            foreach (UpgradeDef d in UpgradeDefs) if (Bought[(int)d.Up]) ups.Add(d.Id + ":" + UpgradeTime[(int)d.Up].ToString("0.##", ci));
            sb.Append("ups=").Append(string.Join(",", ups)).Append('\n');
            // espelho para a 0.1-0.6 sem envelope (downgrade le o mesmo texto: Salvamento.Gravar): flags na ordem CONGELADA IdsV1, nunca na do
            // enum. O Load do schema 2 ignora; parcial de pad e instantes nao descem
            var up = new StringBuilder();
            foreach (string id in IdsV1) { int u = Upgrades.IndexOf(id, UpgradeDefs); up.Append(u >= 0 && Bought[u] ? '1' : '0'); }
            sb.Append("up=").Append(up).Append('\n');
            // parcial de pad pelo ID do upgrade que ele esta pagando: o 1o nao comprado da cadeia (toda comprada: o ultimo; o Load devolve)
            var upp = new List<string>();
            foreach (Pad p in Pads)
            {
                if (p.Paid <= 0) continue;
                int k = Array.FindIndex(p.Chain, c => !Bought[(int)c]);
                upp.Add(UpgradeDefs[(int)p.Chain[k < 0 ? p.Chain.Length - 1 : k]].Id + ":" + p.Paid);
            }
            sb.Append("upp=").Append(string.Join(",", upp)).Append('\n');
            foreach (Station s in Stations)
            {
                if (!s.Produces) continue;
                sb.Append("st").Append(s.Index).Append('=').Append(s.In).Append(',').Append(s.Out).Append(',').Append(s.Busy ? 1 : 0).Append(',').Append(s.Progress.ToString("0.###", ci)).Append(',').Append(s.Crafted);
                for (int k = 1; k < s.Ins.Length; k++) sb.Append(',').Append(s.Ins[k]);   // B-CORE-01: do 2o insumo em diante (receita de 1 insumo: a linha de sempre)
                sb.Append('\n');
            }
            // B-CORE-01 (schema 3): estoque, vendas e maos por ID de item (id:n, so o que nao e' zero). stock=, sold=, hold= e wk= ficam como
            // espelho por posicao do enum para o downgrade a 0.5 (como o up=); o Load do schema 3 nao os le
            sb.Append("stocks=").Append(IdCounts(Stock)).Append('\n');
            sb.Append("solds=").Append(IdCounts(SoldItems)).Append('\n');
            sb.Append("holds=").Append(IdCounts(Player.Held)).Append('\n');
            if (Workers.Count > 0) sb.Append("wks=").Append(string.Join(";", Workers.ConvertAll(w => w.Role + ":" + IdCounts(w.Held)))).Append('\n');
            // B-CORE-03: qualidade. Lote por ID de item "id:fina:superior" (so quem tem Fina ou Superior; Normal = o resto do stocks=/holds=/wks=,
            // que seguem sendo o TOTAL: a 0.8 e a 0.5 leem tudo Normal), um lote por ajudante na ordem do wks=, acumulador/golpe/pilha de cada
            // bancada por indice como o stN= (qstN=acc,golpe,fina,superior) e a recarga do golpe. Save sem estas chaves = tudo Normal, acumulador 0
            sb.Append("stocksq=").Append(IdQ(Stock, StockQ)).Append('\n');
            sb.Append("holdsq=").Append(IdQ(Player.Held, Player.HeldQ)).Append('\n');
            if (Workers.Count > 0) sb.Append("wksq=").Append(string.Join(";", Workers.ConvertAll(w => IdQ(w.Held, w.HeldQ)))).Append('\n');
            foreach (Station s in Stations)
            {
                if (!s.Produces || !IsProduct(s.OutItem)) continue;
                Fit(s.Out, s.OutQ);
                sb.Append("qst").Append(s.Index).Append('=').Append(s.QAcc).Append(',').Append(s.Strike ? 1 : 0);
                for (int l = 1; l < Graus; l++) sb.Append(',').Append(s.OutQ[l]);
                sb.Append('\n');
            }
            sb.Append("golpe=").Append(StrikeCooldown.ToString("0.###", ci)).Append('\n');
            if (Array.Exists(PecasComLiga, n => n > 0)) sb.Append("liga=").Append(IdCounts(PecasComLiga)).Append('\n');   // C-CORE-05: sem liga, o texto de antes
            SalvaBancadas(sb); SalvaFamilias(sb);   // C-CORE-06: ofcN=, fam= e ricos= (so no distrito que tem e fora do padrao)
            SaveCotas(sb);   // C-CORE-07: cota=/cotab=/cotap=/cotar= (so no distrito com patio e fora do padrao)
            SaveArcano(sb);  // C-CORE-08: arcN=/arce= (so no distrito com altar e fora do padrao)
            SaveRotas(sb);   // C-CORE-09: gf=/gfl=/gflq= (so no distrito com cais ou patio de exportacao e fora do padrao)
            sb.Append("stock=").Append(Stock[2]).Append(',').Append(Stock[3]).Append(',').Append(Stock[4]).Append(',').Append(Stock[5]).Append('\n');
            sb.Append("sold=").Append(SoldItems[2]).Append(',').Append(SoldItems[3]).Append(',').Append(SoldItems[4]).Append(',').Append(SoldItems[5]).Append('\n');
            var ms = new StringBuilder();
            foreach (Chest c in Chests) { if (ms.Length > 0) ms.Append(','); ms.Append(c.State); }
            sb.Append("ms=").Append(ms).Append('\n');
            sb.Append("ema=").Append(RateEma.ToString("0.#####", ci)).Append('\n');
            sb.Append("saved=").Append(SavedAt).Append('\n');
            sb.Append("claim=").Append(LastClaim).Append('\n');
            sb.Append("m=").Append(FirstSaleTime.ToString("0.##", ci)).Append(',').Append(Sales).Append(',').Append(GoldEarned).Append(',').Append(OfflineEarned).Append(',')
              .Append(ClientsLost).Append(',').Append(ClientsTurnedAway).Append(',').Append(MaxQueue).Append(',').Append(WalkNoDecision.ToString("0.##", ci)).Append(',').Append(UpgradesBought).Append('\n');
            sb.Append("px=").Append(Player.Pos.ToString()).Append('\n');
            // FASE7: o que esta na mao volta ao reabrir. Espelho por posicao para a 0.5 (jogador: quantos de cada item; ajudantes "papel:contagens"
            // separados por ';'); o schema 3 le holds=/wks= (B-CORE-01)
            // B-CORE-07: so as posicoes da lista congelada (ItemIdsV2): item anexado depois (espada longa) nao entra no espelho
            sb.Append("hold=").Append(EspelhoV2(Player.Held)).Append('\n');
            if (Workers.Count > 0) sb.Append("wk=").Append(string.Join(";", Workers.ConvertAll(w => w.Role + ":" + EspelhoV2(w.Held)))).Append('\n');
            // FASE8: relogio do VIP, VIPs sorteados e o VIP pendente (produto, unidades que faltam). A fila nao vai no save: o VIP que ja estava
            // na vaga volta como pendente e pega a 1a vaga ao reabrir, com paciencia cheia
            Client vip = Queue.Find(c => c.Vip);
            int vipWant = vip != null ? (int)vip.Want : _vipWant, vipPack = vip != null ? vip.Pack : _vipWant >= 0 ? _vipPack : 0;
            sb.Append("vip=").Append(VipIn.ToString("0.###", ci)).Append(',').Append(VipCount).Append(',').Append(vipWant).Append(',').Append(vipPack)
              .Append(',').Append(vip != null ? 1 : 0).Append(',').Append(vip != null ? vip.Paid : 0)   // na vaga?, ja pagou
              .Append(',').Append((vip != null ? vip.Summoned : _vipWant >= 0 && _vipSummoned) ? 1 : 0).Append('\n');   // P14: chamado? (a recarga comeca quando ele sair)
            sb.Append("vipc=").Append(VipCooldown.ToString("0.###", ci)).Append('\n');   // P14: recarga do Chamar VIP
            Encomendas.Save(sb);   // FASE9/B-CORE-05: ord= (espelho da principal para o downgrade) e as linhas enc*
            // A-CORE-02: velocidade (multiplicador, s restantes, s de recarga), no tempo de jogo
            sb.Append("bst=").Append(BoostMul.ToString("0", ci)).Append(',').Append(BoostLeft.ToString("0.###", ci)).Append(',')
              .Append(BoostCooldown.ToString("0.###", ci)).Append('\n');
            Anuncios.Save(sb);   // E-PLAT-02: linhas ad.* (so o que nao e' o padrao)
            if (Heroi == null) Renome.Save(sb);  // B-CORE-06: rn= (o Load de antes ignora chave desconhecida); C-CORE-03: o do distrito e' o da raiz
            Equipe.Save(sb);  // B-CORE-04: wkn= (so com alguem de nivel; senao o texto e' o de antes)
            Projetos.Save(sb);   // B-CORE-07: bp= e bpq= (idem)
            Hall.Save(sb); Colecao.Save(sb);   // B-CORE-08: hall= e colq= (so com algo dentro)
            _contratos.Save(sb);   // D-CORE-04: ctr= e ctr.v= (so com contrato; num distrito que nao e' a raiz, nunca)
            Metas.Save(sb);   // D-CORE-01/02: linhas mt.* (nada antes da 1a Agora)
            LiveOps.Save(sb); // D-CORE-03/04: linhas ev.*/ps.* (nada antes da 1a Agora)
            Compras.Save(sb);   // E-PLAT-04: linhas iap.* (so com compra)
            return sb.ToString();
        }

        /// <summary>Carrega o payload sobre `tuning` (null = padrao): os tetos do load (boost, VIP, encomenda) sao os do Tuning da partida.
        /// `table` = a tabela que resolve os IDs (A-CORE-04; null = Upgrades.All) e `items`/`recipes` = o conteudo (B-CORE-01; null = o padrao):
        /// so o teste injeta outro (B-CORE-07: `blueprints` idem). C-CORE-01: `distrito` = o DistrictDef do payload (null = Old Forge); os IDs
        /// resolvem na tabela dele; `heroi` = o Sim do Old Forge (Imperio: a Mochila dele vale no teto da mao que volta do save).</summary>
        public static Sim Load(string text, Tuning tuning = null, UpgradeDef[] table = null, ItemDef[] items = null, RecipeDef[] recipes = null,
            BlueprintDef[] blueprints = null, DistrictDef distrito = null, Sim heroi = null)
        {
            var sim = new Sim(tuning, items, recipes, blueprints, distrito) { Heroi = heroi };
            table ??= sim.UpgradeDefs;
            Tuning T = sim.T;   // os tetos abaixo
            if (string.IsNullOrEmpty(text)) return sim;
            // schema: sem v= (lixo, testes) = 1; maior que Schema (downgrade) = melhor esforco, como a 0.6 faz com o save da 0.7
            int v = 1;
            foreach (string raw in text.Split('\n')) if (raw.StartsWith("v=", StringComparison.Ordinal)) { v = Math.Max(1, I(raw.Substring(2), 1)); break; }
            for (; v < Schema; v++) text = Migrations[v - 1](text);
            sim.Equipe.Load(text);   // B-CORE-04: antes das pilhas (stN=): o teto da saida depende do almoxarife
            sim.CarregaBancadas(text);   // C-CORE-06: antes das pilhas (stN= e' a entrada da receita ativa)
            sim.Recompute();
            var ci = CultureInfo.InvariantCulture;
            string wk = "", wkq = "";   // carga dos ajudantes (e a qualidade dela): aplicada depois do Recompute (eles nascem dos flags)
            int vipWant = -1, vipPack = 0, vipPaid = 0; bool vipQuiet = false, vipSummoned = false;
            // VIP pendente e encomendas: so depois do Recompute (a linha do produto tem de estar aberta)
            long refund = 0;   // parcial de pad sem pad (ID desconhecido ou upgrade so de menu): volta ao ouro depois do gold=
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw.Substring(0, eq).Trim(), val = raw.Substring(eq + 1).Trim();
                string[] parts = val.Split(',');
                switch (key)
                {
                    case "t": sim.Time = F(val, 0f); break;
                    case "gold": sim.Gold = Math.Max(0, L(val, 0)); break;
                    // A-CORE-04: por ID (up=/ut=/pads= do schema 1 ja viraram isto no V1ParaV2; o espelho up= do schema 2 fica sem leitor). ID
                    // desconhecido fica de fora e conta em UnknownIds; o parcial vai ao pad cuja cadeia tem o upgrade
                    case "ups":
                        foreach (string tok in parts)
                        {
                            string[] kv = tok.Split(':');
                            int u = Upgrades.IndexOf(kv[0].Trim(), table);
                            if (u >= 0) { sim.Bought[u] = true; sim.UpgradeTime[u] = kv.Length > 1 ? F(kv[1], -1f) : -1f; }
                            else if (kv[0].Trim().Length > 0) sim.UnknownIds++;
                        }
                        break;
                    case "upp":
                        foreach (string tok in parts)
                        {
                            string[] kv = tok.Split(':');
                            long paid = kv.Length > 1 ? Math.Max(0, L(kv[1], 0)) : 0;
                            if (paid <= 0) continue;
                            int u = Upgrades.IndexOf(kv[0].Trim(), table);
                            Pad pad = u >= 0 ? sim.Pads.Find(p => Array.IndexOf(p.Chain, (Upgrade)u) >= 0) : null;
                            if (pad != null) pad.Paid += paid; else refund += paid;
                            if (u < 0) sim.UnknownIds++;
                        }
                        break;
                    // B-CORE-01: por ID de item (stock=/sold=/hold=/wk= por posicao ja viraram isto no V2ParaV3; no schema 3 sao so espelho)
                    case "stocks": ReadIdCounts(sim, parts, sim.Stock); break;       // save antigo com 3 produtos: joia fica 0
                    case "solds": ReadIdCounts(sim, parts, sim.SoldItems); break;    // save antigo sem sold=: conta do zero
                    case "ms": for (int i = 0; i < Math.Min(parts.Length, sim.Chests.Count); i++) sim.Chests[i].State = Clamp0(I(parts[i], 0), 2); break;      // save antigo sem ms=: nenhum marco
                    case "ema": sim.RateEma = Math.Max(0.0, D(val, 0.0)); break;
                    case "saved": sim.SavedAt = L(val, 0); break;
                    case "claim": sim.LastClaim = L(val, 0); break;
                    case "m":
                        if (parts.Length >= 9)
                        {
                            sim.FirstSaleTime = F(parts[0], -1f); sim.Sales = I(parts[1], 0); sim.GoldEarned = L(parts[2], 0); sim.OfflineEarned = L(parts[3], 0);
                            sim.ClientsLost = I(parts[4], 0); sim.ClientsTurnedAway = I(parts[5], 0); sim.MaxQueue = I(parts[6], 0); sim.WalkNoDecision = F(parts[7], 0f); sim.UpgradesBought = I(parts[8], 0);
                        }
                        break;
                    case "px": if (parts.Length == 2) sim.Player.Pos = new V2(F(parts[0], 4.5f), F(parts[1], 3.5f)); break;   // clamp so' depois do Recompute (MaxX depende do Corredor)
                    case "holds": ReadIdCounts(sim, parts, sim.Player.Held); break;   // save antigo sem hold=: maos vazias
                    case "wks": wk = val; break;
                    // B-CORE-03; save antigo sem estas chaves: tudo Normal, acumuladores 0, sem golpe valendo nem recarga
                    case "stocksq": ReadIdQ(sim, parts, sim.StockQ); break;
                    case "holdsq": ReadIdQ(sim, parts, sim.Player.HeldQ); break;
                    case "wksq": wkq = val; break;
                    case "golpe": sim.StrikeCooldown = Math.Max(0f, Math.Min(T.GolpeRecarga, F(val, 0f))); break;
                    case "liga": ReadIdCounts(sim, parts, sim.PecasComLiga); break;   // C-CORE-05; save sem liga= = nenhuma peca com liga
                    case "fam": case "ricos": sim.CarregaFamilias(key, val); break;   // C-CORE-06; save sem elas = rodizio do zero e nenhum rico
                    case "vip":   // FASE8; save antigo sem vip=: relogio novo (VipInterval(0)) e nenhum VIP pendente
                        if (parts.Length >= 4)
                        {
                            sim.VipIn = Math.Max(0f, Math.Min(T.VipMax, F(parts[0], sim.VipIn)));
                            sim.VipCount = Math.Max(0, I(parts[1], 0));
                            vipWant = I(parts[2], -1); vipPack = Clamp0(I(parts[3], 0), T.VipSummonPackMax);
                            if (parts.Length >= 6) { vipQuiet = I(parts[4], 0) == 1; vipPaid = Math.Max(0, I(parts[5], 0)); }   // save da 0.5.0: 4 campos
                            if (parts.Length >= 7) vipSummoned = I(parts[6], 0) == 1;   // P14; save de antes: natural (nao recarrega ao sair)
                        }
                        break;
                    case "vipc": sim.VipCooldown = Math.Max(0f, Math.Min(T.Ad(Anuncio.Vip).Recarga, F(val, 0f))); break;   // P14; save antigo sem vipc=: sem recarga
                    case "bst":   // A-CORE-02; save antigo sem bst=: sem boost e sem recarga (como antes). Sem tempo = sem boost; ativo = 2x ou 3x
                        if (parts.Length >= 3)
                        {
                            sim.BoostLeft = Math.Max(0f, Math.Min(T.Ad(Anuncio.Velocidade).Valor, F(parts[1], 0f)));
                            sim.BoostMul = sim.BoostLeft > 0f ? Math.Max(2f, Math.Min(T.BoostMax, (float)Math.Round(F(parts[0], 2f)))) : 1f;
                            sim.BoostCooldown = Math.Max(0f, Math.Min(T.Ad(Anuncio.Velocidade).Recarga, F(parts[2], 0f)));
                        }
                        break;
                    default:
                        if (key.StartsWith("st") && parts.Length >= 4)
                        {
                            int idx = I(key.Substring(2), -1);
                            if (idx < 0 || idx >= sim.Stations.Count || !sim.Stations[idx].Produces) break;
                            Station s = sim.Stations[idx];
                            s.In = Clamp0(I(parts[0], 0), s.InCap); s.Out = Clamp0(I(parts[1], 0), s.OutCap);
                            s.Busy = I(parts[2], 0) == 1; s.Progress = Math.Max(0f, Math.Min(1f, F(parts[3], 0f)));
                            if (parts.Length >= 5) s.Crafted = Math.Max(0, I(parts[4], 0));
                            for (int k = 1; k < s.Ins.Length && 4 + k < parts.Length; k++) s.Ins[k] = Clamp0(I(parts[4 + k], 0), s.InCap);   // B-CORE-01: 2o insumo em diante
                        }
                        else if (key.StartsWith("qst") && parts.Length >= 2)   // B-CORE-03: acumulador, golpe valendo e Fina/Superior da pilha
                        {
                            int idx = I(key.Substring(3), -1);
                            if (idx < 0 || idx >= sim.Stations.Count || !sim.Stations[idx].Produces) break;
                            Station s = sim.Stations[idx];
                            s.QAcc = Clamp0(I(parts[0], 0), 99); s.Strike = I(parts[1], 0) == 1;
                            for (int l = 1; l < Graus && l + 1 < parts.Length; l++) s.OutQ[l] = Math.Max(0, I(parts[l + 1], 0));
                        }
                        break;
                }
            }
            sim.Projetos.Load(text);   // B-CORE-07: antes do pad (o parcial da bigorna longa so fica com o projeto); sem bp= o 1o Tick da o retroativo dos marcos
            sim.Colecao.Load(text);    // B-CORE-08: colq= (Superiores por item; Normal e Fina saem do solds= e do bpq=)
            sim.LoadCotas(text);       // C-CORE-07: o patio de cotas (save sem as linhas = patio vazio)
            sim.LoadArcano(text);      // C-CORE-08: os medidores dos altares (save sem as linhas = vazios, Estavel)
            sim.LoadRotas(text);       // C-CORE-09: rotas e lote (save sem as linhas = rotas paradas e patio vazio)
            sim.Gold += refund;
            sim.UpgradesBought = 0;
            foreach (bool b in sim.Bought) if (b) sim.UpgradesBought++;
            sim.Recompute();
            for (int i = 0; i < sim.Stock.Length; i++)   // teto so' e' conhecido depois dos upgrades; insumo nao fica no balcao nem conta venda
                if (sim.IsProduct((Item)i)) sim.Stock[i] = Math.Min(sim.Stock[i], sim.CounterCap); else sim.Stock[i] = sim.SoldItems[i] = 0;
            for (int i = 0; i < sim.Player.Held.Length; i++) sim.Player.Held[i] = Math.Min(sim.Player.Held[i], sim.Player.Cap);   // teto por tipo (Mochila)
            for (int i = 0; i < sim.Stock.Length; i++) { Fit(sim.Stock[i], sim.StockQ[i]); Fit(sim.Player.Held[i], sim.Player.HeldQ[i]); }   // B-CORE-03: lote cabe no total
            foreach (Station s in sim.Stations) Fit(s.Out, s.OutQ);
            string[] ws = wk.Split(';'), wq = wkq.Split(';');
            for (int k = 0; k < ws.Length; k++)   // ajudante: so o 1o tipo valido para o papel (na ordem dos itens), ate o teto; lixo = mao vazia
            {
                // a lista e' recriada na ordem do HireBy, nao na da compra: vai para o proximo ajudante vazio do mesmo papel.
                // Save da 0.5.0 (sem papel; o V2ParaV3 o deixa ":id:n") segue a posicao na lista
                int colon = ws[k].IndexOf(':'), role = colon > 0 ? I(ws[k].Substring(0, colon), -9) : -9;
                Carrier w = colon > 0 ? sim.Workers.Find(c => c.Role == role && c.Count == 0) : k < sim.Workers.Count ? sim.Workers[k] : null;
                if (w == null) continue;
                var h = new int[w.Held.Length];
                ReadIdCounts(sim, ws[k].Substring(colon + 1).Split(','), h);
                for (int i = 0; i < h.Length; i++)
                {
                    int n = Clamp0(h[i], w.Cap);
                    if (n > 0 && w.Count == 0 && sim.CanPick(w, (Item)i)) w.Held[i] = n;
                }
                if (k < wq.Length) ReadIdQ(sim, wq[k].Split(','), w.HeldQ);   // B-CORE-03: o lote do mesmo ajudante (ordem do wks=)
                for (int i = 0; i < h.Length; i++) Fit(w.Held[i], w.HeldQ[i]);
            }
            if (vipPack > 0 && vipWant >= (int)Item.Sword && vipWant <= (int)Item.Tool && sim.LineUnlocked((Item)vipWant)) { sim._vipWant = vipWant; sim._vipPack = vipPack; sim._vipPaid = vipPaid; sim._vipQuiet = vipQuiet; sim._vipSummoned = vipSummoned; }
            sim.Encomendas.Load(text);   // B-CORE-05 (ord= e enc*): antes do Renome (o retroativo le o OrderCount)
            sim._contratos.Load(text);   // D-CORE-04: save sem ctr* = nenhum contrato
            sim.Player.Pos = sim.Collide(Clamp(sim.Player.Pos, sim.MaxX));   // save antigo com o jogador dentro de um corpo: sai pela borda
            // pad escondido devolve o parcial: o save da v0.3 com produtivos novos (Mineiro, Joalheiro 2) esconde o luxo ate completar
            // de novo, e o pad cujo upgrade foi para o menu (FASE6) nunca mais aparece
            foreach (Pad p in sim.Pads) if (p.Current(sim) < 0) { sim.Gold += p.Paid; p.Paid = 0; }
            sim.Metas.Load(text);   // D-CORE-01/02: save sem mt.* = metas zeradas
            sim.Anuncios.Load(text);   // E-PLAT-02: save sem ad.* = nenhum anuncio visto
            if (heroi == null) sim.Renome.Load(text);  // B-CORE-06: save sem rn= = retroativo (depois das metas: soma o Renome que elas guardaram); C-CORE-03: distrito = o da raiz
            sim.Hall.Load(text);    // B-CORE-08: depois do Renome (o mw: do ledger sem hall= volta registrado)
            sim.LiveOps.Load(text); // D-CORE-03/04: save sem ev.*/ps.* = nenhum evento, Pass do zero (o XP das metas entra 1x na 1a temporada)
            sim.Compras.Load(text);   // E-PLAT-04: save sem iap.* = nada comprado; posse so no Compras.Conectar (a loja valida)
            return sim;
        }

        static int Clamp0(int v, int max) => Math.Max(0, Math.Min(max, v));
        static int I(string s, int d) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        static long L(string s, long d) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : d;
        static float F(string s, float d) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : d;
        static double D(string s, double d) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && !double.IsNaN(v) && !double.IsInfinity(v) ? v : d;
    }
}
