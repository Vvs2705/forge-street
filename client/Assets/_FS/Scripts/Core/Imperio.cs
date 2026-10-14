using System;
using System.Collections.Generic;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// C-CORE-02 (GDD §6, §11; CONTEUDO_V1 §1–§2; V5, V9, V15, V25, P2): a camada Imperio sobre um Sim por distrito.
    /// - Carteira (V9): saldo por ID de moeda, so "ouro" ate a Wave E. O ouro mora no Gold do Sim ATIVO (o que a View e o bot mexem); ao trocar,
    ///   o saldo passa inteiro ao novo, e o Fundo recolhe o que cair num inativo (meta e Renome da raiz pagando com outro distrito ativo).
    /// - Raiz = o Old Forge: Renome, metas, eventos, projetos e compras ficam nele nesta leva (as camadas sobem ao Imperio na proxima).
    /// - Charter (V5/V15): 1x por distrito (ID charter:&lt;id&gt; no Renome), sem reset; libera o proximo (abertura paga em ouro, Abrir).
    /// - Renda de fundo: distrito com Charter e fora de foco rende DistrictDef.FundoFator x a RateEma dele quando o jogador saiu (fora do
    ///   GoldEarned, como o cofre). Offline: a mesma taxa com o teto de 2 h do nucleo (P2: o manager mexe na taxa, nunca no tempo).
    /// - Save: o payload da raiz como sempre (a carteira no gold= dela) e, so depois do 1o Charter, as linhas imp= (ativo), charter= e
    ///   dist.&lt;id&gt;= (payload do distrito em base64). Save sem elas = Imperio de 1 distrito (o save de hoje, sem perda); Imperio de 1
    ///   distrito grava o texto de sempre, ao byte.
    /// </summary>
    public sealed class Imperio
    {
        public const string Ouro = "ouro";
        /// <summary>Moedas da carteira (V9). Gems entram na Wave E com mapa fonte -> ralo.</summary>
        public static readonly string[] Moedas = { Ouro };

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

        /// <summary>`tuning` = Tuning novo da partida (com o RemoteConfig) para cada distrito aberto; null = padrao.</summary>
        public Imperio(Sim raiz, Func<Tuning> tuning = null)
        {
            Raiz = raiz; Ativo = raiz; _tuning = tuning ?? (() => new Tuning());
            Abertos.Add(raiz);
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
            if (s.Masterworks < c.Masterworks) return "masterwork";
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
            if (d == null || Aberto(id) || !Liberado(id) || Ativo.Gold < d.Abertura) return false;
            Ativo.Gold -= d.Abertura;
            Adiciona(new Sim(_tuning(), distrito: d));
            return true;
        }

        void Adiciona(Sim s)
        {
            s.Heroi = Raiz;   // Mochila e Botas do ferreiro
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
        public double FundoPorSegundo(Sim s) => s == Ativo || !Charters.Contains(s.Distrito.Id) ? 0.0 : s.RateEma * s.Distrito.FundoFator;

        /// <summary>A cada passo do jogo (depois do Tick do ativo): credita o fundo dos inativos na carteira, em ouro inteiro (resto acumula), e
        /// recolhe ouro que caiu num inativo. Fora do GoldEarned (nao infla a RateEma do ativo nem o cofre).</summary>
        public void Fundo(float dt)
        {
            Recolhe();
            foreach (Sim s in Abertos)
            {
                if (s == Ativo) continue;
                double rate = FundoPorSegundo(s);
                if (rate <= 0.0 || dt <= 0f) continue;
                _fundoAcc.TryGetValue(s, out double acc);
                acc += rate * dt;
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
                foreach (Sim s in Abertos)
                    if (s != Raiz) sb.Append("dist.").Append(s.Distrito.Id).Append('=').Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Save(nowUnix)))).Append('\n');
                foreach (string l in _desconhecidos) sb.Append(l).Append('\n');
            }
            Raiz.Gold = 0; Ativo.Gold = w;
            return sb.ToString();
        }

        /// <summary>Le o payload (save de hoje = 1 distrito, o Old Forge). Distrito que o build nao tem fica guardado e volta no save; base64 ruim
        /// = distrito fechado (a raiz nunca se perde). `tuning` = Tuning novo da partida por distrito (null = padrao).</summary>
        public static Imperio Load(string text, Func<Tuning> tuning = null)
        {
            tuning ??= () => new Tuning();
            var imp = new Imperio(Sim.Load(text, tuning()), tuning);
            if (string.IsNullOrEmpty(text)) return imp;
            string ativo = null;
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw.Substring(0, eq).Trim(), val = raw.Substring(eq + 1).Trim();
                if (key == "imp") ativo = val;
                else if (key == "charter") foreach (string c in val.Split(',')) { if (c.Trim().Length > 0) imp.Charters.Add(c.Trim()); }
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
