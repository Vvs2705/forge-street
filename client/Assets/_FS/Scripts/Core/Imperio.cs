using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// C-CORE-02 (GDD §6, §11; CONTEUDO_V1 §1–§2; V5, V9, V15, V25, P2): a camada Imperio sobre um Sim por distrito.
    /// - Carteira (V9): saldo por ID de moeda, so "ouro" ate a Wave E. O ouro mora no Gold do Sim ATIVO (o que a View e o bot mexem); ao trocar,
    ///   o saldo passa inteiro ao novo, e o Fundo recolhe o que cair num inativo (meta e Renome da raiz pagando com outro distrito ativo).
    /// - Raiz = o Old Forge: Renome, metas, eventos, projetos e compras ficam nele. C-CORE-03: o Renome do distrito E' o da raiz (Sim.Renome) e
    ///   venda, VIP, encomenda, golpe e cofre do distrito contam tambem nas metas, no evento e no Pass da raiz, 1x (Sim.Emit/Strike, Encomendas).
    /// - Charter (V5/V15): 1x por distrito (ID charter:&lt;id&gt; no Renome), sem reset; libera o proximo (abertura paga em ouro, Abrir).
    /// - Renda de fundo: distrito com Charter e fora de foco rende FundoFator(id) = DistrictDef.FundoFator (o manager nivel 1, 25%) x FundoNivel
    ///   do manager (C-CORE-03: niveis 2-5 = 30/33/36/40%) x a RateEma dele quando o jogador saiu (fora do GoldEarned, como o cofre). Offline: a
    ///   mesma taxa com o teto de 2 h do nucleo (P2: o manager mexe na taxa, nunca no tempo).
    /// - Save: o payload da raiz como sempre (a carteira no gold= dela) e, so depois do 1o Charter, as linhas imp= (ativo), charter= e
    ///   dist.&lt;id&gt;= (payload do distrito em base64) e, so com manager acima do nivel 1, mgr=id:nivel. Save sem elas = Imperio de 1
    ///   distrito (o save de hoje, sem perda); Imperio de 1
    ///   distrito grava o texto de sempre, ao byte.
    /// </summary>
    public sealed class Imperio
    {
        public const string Ouro = "ouro";
        /// <summary>Moedas da carteira (V9). Gems entram na Wave E com mapa fonte -> ralo.</summary>
        public static readonly string[] Moedas = { Ouro };

        /// <summary>C-CORE-03 (GDD §8, CONTEUDO_V1 §1 e §6): o manager de cada distrito, em dados. Vem com o Charter (nivel 1, contratacao 0);
        /// Niveis = ouro dos niveis 2-5 (20/40/80/160 min da renda alvo do distrito); nome proprio, igual nos 3 idiomas. ponytail: os managers dos
        /// distritos 3-6 (Valentim, Leonor, Sibila, Aurelio) entram com o DistrictDef deles; distrito sem manager aqui fica no nivel 1.</summary>
        public static readonly WorkerDef[] Managers =
        {
            new WorkerDef("wk_anselmo", "Mestre Anselmo", WorkerRole.Manager, Specialty.None, Distritos.OldForgeId, null, null, 0, 0, null,
                new long[] { 32000, 64000, 130000, 260000 }, null),
            new WorkerDef("wk_gertrudes", "Dona Gertrudes", WorkerRole.Manager, Specialty.None, Distritos.ArmorersRowId, null, null, 0, 0, null,
                new long[] { 96000, 190000, 380000, 770000 }, null),
            // ---- artisan_quarter (C-CORE-06): CONTEUDO_V1 §6
            new WorkerDef("wk_valentim", "Valentim", WorkerRole.Manager, Specialty.None, Distritos.ArtisanQuarterId, null, null, 0, 0, null,
                new long[] { 290000, 580000, 1200000, 2300000 }, null),
            // ---- royal_arsenal (C-CORE-07): CONTEUDO_V1 §6
            new WorkerDef("wk_leonor", "Capitã Leonor", WorkerRole.Manager, Specialty.None, Distritos.RoyalArsenalId, null, null, 0, 0, null,
                new long[] { 860000, 1700000, 3400000, 6900000 }, null),
            // ---- arcane_foundry (C-CORE-08): CONTEUDO_V1 §6
            new WorkerDef("wk_sibila", "Mestra Sibila", WorkerRole.Manager, Specialty.None, Distritos.ArcaneFoundryId, null, null, 0, 0, null,
                new long[] { 2600000, 5200000, 10000000, 21000000 }, null),
            // ---- grand_forge (C-CORE-09): CONTEUDO_V1 §6 (o Grão-mestre Aurélio; "Aurélio" no cartao, o nome passa de 16)
            new WorkerDef("wk_aurelio", "Aurélio", WorkerRole.Manager, Specialty.None, Distritos.GrandForgeId, null, null, 0, 0, null,
                new long[] { 7800000, 16000000, 31000000, 62000000 }, null),
        };
        /// <summary>Fundo por nivel do manager (1-5) sobre o DistrictDef.FundoFator (o nivel 1): 25% -> 30/33/36/40% da taxa (CONTEUDO_V1 §1).
        /// Sempre &lt; 1 vez a taxa: o distrito ativo rende mais que ele mesmo em fundo em todo nivel (ImperioTests).</summary>
        public static readonly double[] FundoNivel = { 1.0, 1.2, 1.32, 1.44, 1.6 };

        public static WorkerDef ManagerDe(string id) => Array.Find(Managers, m => m.Distrito == id);

        public readonly Sim Raiz;
        public Sim Ativo { get; private set; }
        /// <summary>Distritos abertos, na ordem do mapa (Distritos.All).</summary>
        public readonly List<Sim> Abertos = new List<Sim>();
        /// <summary>IDs dos distritos com o Charter concedido.</summary>
        public readonly HashSet<string> Charters = new HashSet<string>();
        /// <summary>Metrica (fora do save): ouro de fundo creditado nesta sessao, online e offline.</summary>
        public long FundoGanho;
        readonly Func<Tuning> _tuning;
        readonly Dictionary<Sim, double> _fundoAcc = new Dictionary<Sim, double>();
        readonly List<string> _desconhecidos = new List<string>();   // dist.<id>= de um build mais novo: volta no save
        readonly Dictionary<string, int> _managers = new Dictionary<string, int>();   // C-CORE-03: nivel > 1 por distrito (ID de build mais novo fica)
        // ---- grand_forge (C-CORE-09): o que as rotas do Mensageiro devem ao fundo de cada origem neste minuto (fora do save: so com o jogo aberto)
        readonly Dictionary<Sim, double> _divida = new Dictionary<Sim, double>();
        float _janela;
        /// <summary>Metrica (fora do save): ouro de fundo que as rotas tiraram das origens nesta sessao.</summary>
        public double FundoDescontado;

        /// <summary>C-CORE-09 (CONTEUDO_V1 §2.6 regra 2): um importado chegou a Grand Forge. PriceOf(item, Normal) da origem x `pct`% sai da renda
        /// de fundo do distrito de origem neste minuto (Fundo: do que ele creditaria; o que passar disso no minuto, perdoado: nunca abaixo de 0).
        /// Origem fechada ou sem o item: nada.</summary>
        internal void Importou(string origem, string item, int pct)
        {
            Sim o = Distrito(origem);
            int i = o == null ? -1 : Items.IndexOf(item, o.ItemDefs);
            if (i < 0) return;
            _divida.TryGetValue(o, out double d);
            _divida[o] = d + o.PriceOf((Item)i, Quality.Normal) * pct / 100.0;
        }

        /// <summary>`tuning` = Tuning novo da partida (com o RemoteConfig) para cada distrito aberto; null = padrao.</summary>
        public Imperio(Sim raiz, Func<Tuning> tuning = null)
        {
            Raiz = raiz; Ativo = raiz; _tuning = tuning ?? (() => new Tuning());
            Abertos.Add(raiz);
            raiz.Imp = this;   // C-CORE-09: as rotas da Grand Forge descontam do fundo (Importou) e a condicao DistrictCharters le os Charters
        }

        /// <summary>
        /// Dev, teste e econsim: o Imperio como o jogador chega ao distrito `d` (o Old Forge = um Imperio novo): cada distrito da cadeia de
        /// Charters ate ele (Old Forge -> Row -> ...) aberto, em producao completa e com o Charter (Forca: as condicoes nao sao medidas),
        /// rendendo o do Charter no modelo do CONTEUDO_V1 §1 (`rendaOF` ouro/min no Old Forge, 2.400, x3 por distrito: a Row a 7.200; a base do
        /// fundo), a abertura de `d` paga e o foco em `d`. Mochila e Botas do ferreiro valem (o Old Forge em producao completa).
        /// </summary>
        public static Imperio Chegada(DistrictDef d, Func<Tuning> tuning = null, double rendaOF = 2400)
        {
            tuning ??= () => new Tuning();
            var raiz = new Sim(tuning());
            var imp = new Imperio(raiz, tuning);
            if (d == null || imp.Aberto(d.Id)) return imp;
            // C-CORE-06: a cadeia inteira (antes, so o Old Forge: a Row era a unica que chegava). Para a Row da o de antes, passo a passo
            var cadeia = new List<DistrictDef>();
            for (DistrictDef x = Array.Find(Distritos.All, o => o.Charter?.Proximo == d.Id); x != null; x = Array.Find(Distritos.All, o => o.Charter?.Proximo == x.Id))
                cadeia.Insert(0, x);
            foreach (DistrictDef x in cadeia)
            {
                if (!imp.Aberto(x.Id)) { raiz.Gold += x.Abertura; imp.Abrir(x.Id); }
                Sim sx = imp.Distrito(x.Id);
                for (int i = 0; i < sx.UpgradeDefs.Length; i++) if (sx.IsProduction(i)) sx.Buy((Upgrade)i);
                sx.RateEma = rendaOF * Math.Pow(3, x.Indice) / 60.0;
                imp.Forca(x.Id);
            }
            raiz.Gold += d.Abertura;
            imp.Abrir(d.Id);
            imp.Trocar(d.Id);
            return imp;
        }

        public long Saldo(string moeda) => moeda == Ouro ? Ativo.Gold : 0;
        public Sim Distrito(string id) => Abertos.Find(s => s.Distrito.Id == id);
        public bool Aberto(string id) => Distrito(id) != null;

        /// <summary>O que falta para o Charter do distrito `id` (null = pode): ja concedido, distrito fechado, producao, Renome do Imperio
        /// (o da raiz) e Masterworks DO distrito.</summary>
        public string CharterFalta(string id)
        {
            Sim s = Distrito(id);
            CharterDef c = s?.Distrito.Charter;
            if (c == null) return "fechado";
            if (Charters.Contains(id)) return "concedido";
            if (c.Producao && !s.ProductionComplete) return "producao";
            if (Raiz.Renome.Total < c.Renome) return "renome";
            if (Math.Max(s.Masterworks, s.Hall.Count) < c.Masterworks && (c.Superiores <= 0 || s.Colecao.Superiores.Sum() < c.Superiores))
                return "masterwork";   // C-CORE-03: o Hall vai no save (o contador nao); BALANCE §48: ou as Superiores vendidas
            if (c.Contratos > 0 && Raiz.Contratos.Concluidos.Count < c.Contratos && s.RoyalEntregues < c.Contratos * CharterDef.RoyalPorContrato)
                return "contrato";   // ---- royal_arsenal (C-CORE-07): N contratos OU 10 x N encomendas Royal no distrito (V25: nunca bloqueia)
            return null;
        }

        /// <summary>Concede o Charter (1x, sem reset: nada do distrito muda) e o +100 de Renome (Renome.OnCharter, ID charter:&lt;id&gt;).
        /// Fora de CharterFalta == null devolve false.</summary>
        public bool Charter(string id)
        {
            if (CharterFalta(id) != null) return false;
            Concede(id);
            return true;
        }

        /// <summary>Dev e teste (-distrito, econsim --distrito): o Charter sem as condicoes. Nao paga Renome.</summary>
        public void Forca(string id) => Charters.Add(id);

        void Concede(string id)
        {
            Charters.Add(id);
            Raiz.Renome.OnCharter(id);
        }

        /// <summary>Distrito liberado para abrir: o Charter de um distrito que aponta para ele foi concedido.</summary>
        public bool Liberado(string id)
        {
            foreach (string c in Charters) if (Distritos.Find(c)?.Charter?.Proximo == id) return true;
            return false;
        }

        /// <summary>Abre o distrito `id` pagando a abertura da carteira (CONTEUDO_V1 §1: 90 min de renda do anterior). Liberado, fechado e com
        /// ouro; senao false e nada muda. Nao troca o foco (Trocar).</summary>
        public bool Abrir(string id)
        {
            DistrictDef d = Distritos.Find(id);
            if (d == null || !Raiz.T.DistritosNovos || Aberto(id) || !Liberado(id) || Ativo.Gold < d.Abertura) return false;   // DM §130
            Ativo.Gold -= d.Abertura;
            Adiciona(new Sim(_tuning(), distrito: d));
            return true;
        }

        void Adiciona(Sim s)
        {
            s.Heroi = Raiz;   // Mochila e Botas do ferreiro
            s.Imp = this;     // C-CORE-09
            s.Recompute();
            Abertos.Add(s);
            Abertos.Sort((a, b) => a.Distrito.Indice.CompareTo(b.Distrito.Indice));
        }

        /// <summary>Foco no distrito `id` (aberto): a carteira passa inteira, o que sai congela a RateEma (base do fundo) e o que entra
        /// recalcula (Mochila e Botas do ferreiro).</summary>
        public bool Trocar(string id)
        {
            Sim novo = Distrito(id);
            if (novo == null || novo == Ativo) return false;
            novo.Gold += Ativo.Gold; Ativo.Gold = 0;
            Ativo = novo;
            novo.Recompute();
            return true;
        }

        /// <summary>Ouro/s de fundo do distrito `s` (0 = ativo ou sem Charter): FundoFator x a RateEma dele (congelada desde que saiu).</summary>
        public double FundoPorSegundo(Sim s) => s == Ativo || !Charters.Contains(s.Distrito.Id) ? 0.0 : s.RateEma * FundoFator(s.Distrito.Id);

        /// <summary>C-CORE-03: fracao da taxa que o distrito `id` rende em fundo com o manager de agora (0 = sem Charter).</summary>
        public double FundoFator(string id) => Manager(id) == 0 ? 0.0 : Distritos.Find(id).FundoFator * FundoNivel[Manager(id) - 1];

        /// <summary>C-CORE-03: nivel do manager do distrito `id`: 0 = sem Charter (sem manager), 1 = o que vem com ele, ate 5.</summary>
        public int Manager(string id) => !Charters.Contains(id) ? 0 : _managers.TryGetValue(id, out int n) ? n : 1;

        /// <summary>C-CORE-03: ouro do proximo nivel do manager de `id` (-1 = sem manager, no maximo ou sem dados).</summary>
        public long CustoManager(string id)
        {
            int n = Manager(id); WorkerDef m = ManagerDe(id);
            return m == null || n < 1 || n >= m.MaxNivel ? -1 : m.Niveis[n - 1];
        }

        /// <summary>C-CORE-03: sobe o manager de `id` ao `nivel` pagando da carteira. ID de transacao = (distrito, nivel): so o proximo nivel, 1x
        /// (repetir o toque ou o mesmo nivel depois de save/load devolve false e nada muda); sem Charter, no maximo ou sem ouro, idem.</summary>
        public bool SobeManager(string id, int nivel)
        {
            long c = CustoManager(id);
            if (c < 0 || nivel != Manager(id) + 1 || Ativo.Gold < c) return false;
            Ativo.Gold -= c;
            _managers[id] = nivel;
            return true;
        }

        /// <summary>A cada passo do jogo (depois do Tick do ativo): credita o fundo dos inativos na carteira, em ouro inteiro (resto acumula), e
        /// recolhe ouro que caiu num inativo. Fora do GoldEarned (nao infla a RateEma do ativo nem o cofre).</summary>
        public void Fundo(float dt)
        {
            Recolhe();
            if ((_janela += dt) >= 60f) { _janela -= 60f; _divida.Clear(); }   // C-CORE-09: o que as rotas nao tiraram no minuto, perdoado
            foreach (Sim s in Abertos)
            {
                if (s == Ativo) continue;
                double rate = FundoPorSegundo(s);
                if (rate <= 0.0 || dt <= 0f) continue;
                _fundoAcc.TryGetValue(s, out double acc);
                acc += rate * dt;
                if (_divida.TryGetValue(s, out double dv) && dv > 0.0) { double p = Math.Min(dv, acc); acc -= p; _divida[s] = dv - p; FundoDescontado += p; }   // C-CORE-09
                long whole = (long)Math.Floor(acc);
                _fundoAcc[s] = acc - whole;
                Ativo.Gold += whole; FundoGanho += whole;
            }
        }

        /// <summary>Ouro que caiu num distrito fora de foco (meta, Renome ou evento da raiz) vai a carteira (o Gold do ativo).</summary>
        void Recolhe()
        {
            foreach (Sim s in Abertos) if (s != Ativo && s.Gold != 0) { Ativo.Gold += s.Gold; s.Gold = 0; }
        }

        /// <summary>Cofre offline do Imperio: o do ativo (Sim.ApplyOffline, a regra de sempre) + o fundo de cada inativo com Charter, cada um
        /// FundoFator x RateEma x min(Δt, teto de 2 h), idempotente pelo `claimId` (o LastClaim de cada distrito). Δt &lt;= 0 = 0.</summary>
        public long ApplyOffline(long elapsedSeconds, long claimId)
        {
            long total = Ativo.ApplyOffline(elapsedSeconds, claimId);
            if (elapsedSeconds <= 0) return total;
            foreach (Sim s in Abertos)
            {
                if (s == Ativo || claimId <= s.LastClaim) continue;
                s.LastClaim = claimId;
                long g = (long)Math.Floor(FundoPorSegundo(s) * Math.Min(elapsedSeconds, s.T.OfflineCap));
                if (g <= 0) continue;
                Ativo.Gold += g; FundoGanho += g; total += g;
            }
            return total;
        }

        // ------------------------------------------------------------------ save

        /// <summary>A carteira vai no gold= da raiz (o downgrade le o ouro); 1 distrito sem Charter = o texto do Sim.Save, ao byte.</summary>
        public string Save(long nowUnix)
        {
            Recolhe();
            long w = Ativo.Gold;
            Ativo.Gold = 0; Raiz.Gold = w;
            var sb = new StringBuilder(Raiz.Save(nowUnix));
            if (Abertos.Count > 1 || Charters.Count > 0 || _desconhecidos.Count > 0)
            {
                sb.Append("imp=").Append(Ativo.Distrito.Id).Append('\n');
                var ch = new List<string>(Charters);
                ch.Sort(StringComparer.Ordinal);
                sb.Append("charter=").Append(string.Join(",", ch)).Append('\n');
                if (_managers.Count > 0)
                {
                    var mg = new List<string>();
                    foreach (KeyValuePair<string, int> kv in _managers) mg.Add(kv.Key + ":" + kv.Value.ToString(CultureInfo.InvariantCulture));
                    mg.Sort(StringComparer.Ordinal);
                    sb.Append("mgr=").Append(string.Join(",", mg)).Append('\n');
                }
                foreach (Sim s in Abertos)
                    if (s != Raiz) sb.Append("dist.").Append(s.Distrito.Id).Append('=').Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Save(nowUnix)))).Append('\n');
                foreach (string l in _desconhecidos) sb.Append(l).Append('\n');
            }
            Raiz.Gold = 0; Ativo.Gold = w;
            return sb.ToString();
        }

        /// <summary>Le o payload (save de hoje = 1 distrito, o Old Forge). Distrito que o build nao tem fica guardado e volta no save; base64 ruim
        /// = distrito fechado (a raiz nunca se perde). `tuning` = Tuning novo da partida por distrito (null = padrao); `raiz` = o do Old Forge
        /// (null = um do `tuning`).</summary>
        public static Imperio Load(string text, Func<Tuning> tuning = null, Tuning raiz = null)
        {
            tuning ??= () => new Tuning();
            var imp = new Imperio(Sim.Load(text, raiz ?? tuning()), tuning);
            if (string.IsNullOrEmpty(text)) return imp;
            string ativo = null;
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw.Substring(0, eq).Trim(), val = raw.Substring(eq + 1).Trim();
                if (key == "imp") ativo = val;
                else if (key == "charter") foreach (string c in val.Split(',')) { if (c.Trim().Length > 0) imp.Charters.Add(c.Trim()); }
                else if (key == "mgr")   // C-CORE-03: lixo fica de fora; nivel entre 2 e o maximo
                    foreach (string p in val.Split(','))
                    {
                        int c = p.IndexOf(':');
                        if (c > 0 && int.TryParse(p.Substring(c + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 1)
                            imp._managers[p.Substring(0, c).Trim()] = Math.Min(n, FundoNivel.Length);
                    }
                else if (key.StartsWith("dist.", StringComparison.Ordinal))
                {
                    DistrictDef d = Distritos.Find(key.Substring(5));
                    if (d == null || d == imp.Raiz.Distrito) { imp._desconhecidos.Add(raw.Trim()); continue; }
                    string payload;
                    try { payload = Encoding.UTF8.GetString(Convert.FromBase64String(val)); }
                    catch (FormatException) { continue; }
                    if (!imp.Aberto(d.Id)) imp.Adiciona(Sim.Load(payload, tuning(), distrito: d, heroi: imp.Raiz));
                }
            }
            if (ativo != null) imp.Trocar(ativo);
            return imp;
        }
    }
}
