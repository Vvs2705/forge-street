using System;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>E-PLAT-02: os placements de rewarded (GDD §17.1), na ordem do Tuning.Placements. Placement novo entra no FIM (o save grava pelo Id).</summary>
    public enum Anuncio { Vip, Velocidade, Cofre, EncomendaBonus, EncomendaTroca, Supply }

    /// <summary>E-PLAT-02: knobs de um placement (RemoteConfig = o Tuning; a fonte remota e' a A-PLAT-08). Id = nome no IAds, no diario e no
    /// save. Recarga em s de jogo (0 = sem); TetoDia 0 = so o teto total (Tuning.AnunciosTetoDia); Valor = o que da (Tuning.Placements).</summary>
    public sealed class AdPlacementDef
    {
        public string Id; public bool Ligado = true; public float Recarga; public int TetoDia; public float Valor;
    }

    /// <summary>
    /// E-PLAT-02/03: rewarded por placement e o veto do intersticial, no nucleo. Fluxo da View: Pode(a) mostra o botao com Previa(a) (o que da,
    /// ANTES de assistir); o toque pede Oferta(a) = ID de transacao "id:chave"; so o onReward do IAds chama Pagar(tx). Pagar aplica 1x: a chave
    /// do placement so anda para frente (vistos do VIP/Velocidade/Supply, claimId do cofre, indice da encomenda) e a ultima paga vai no save,
    /// entao o mesmo ID nunca paga de novo (callback repetido, save/load, reabrir). Oferta que mudou entre o toque e a recompensa (encomenda
    /// entregue, VIP que chegou) recusa (-1) e nada muda. Teto diario por placement e total no dia local das metas (Metas.Dia so cresce:
    /// relogio para tras nao reabre o dia; o bot e os testes sem Metas.Agora jogam um dia so). Recarga em s de jogo, no save. D6: nada antes
    /// da 1a melhoria. Anuncio nunca e' necessario: o F2P (sem nenhum) fecha tudo (BalanceTests).
    /// </summary>
    public sealed class Anuncios
    {
        public static readonly int N = Enum.GetValues(typeof(Anuncio)).Length;
        readonly Sim _sim;
        /// <summary>Por placement: ultima chave paga, vistos no HojeDia e s de recarga (VIP e Velocidade usam as do Sim, VipCooldown e
        /// BoostCooldown, que contam da saida do chamado e do fim do boost).</summary>
        public readonly long[] Pago = new long[N];
        public readonly int[] Hoje = new int[N];
        public readonly float[] Recarga = new float[N];
        public long HojeDia;
        /// <summary>Supply Drop pago e ainda nao entregue: entra nas saidas das fornalhas abertas conforme ha espaco (Tick).</summary>
        public int Lingotes;
        /// <summary>Entitlement "remove_ads": placeholder ate a IAP da Wave E (E-PLAT-04 deriva do recibo). Tira so o intersticial; o rewarded
        /// continua opcional. Nao vai no save.</summary>
        public bool RemoveAds;
        /// <summary>E-PLAT-03: intersticiais mostrados nesta sessao (processo) e o instante do ultimo, em s de sessao. Nao vao no save.</summary>
        public int IntersticiaisSessao;
        float _ultimoIntersticial = float.NegativeInfinity;

        public Anuncios(Sim sim) { _sim = sim; }

        AdPlacementDef Def(Anuncio a) => _sim.T.Ad(a);

        /// <summary>Chave da oferta de agora (0 = nenhuma): contador para VIP, Velocidade e Supply; claimId do ultimo resgate no Cofre; indice
        /// da encomenda ativa + 1 no bonus e no trocar.</summary>
        long Chave(Anuncio a)
        {
            switch (a)
            {
                case Anuncio.Cofre: return _sim.LastClaimGold > 0 ? _sim.LastClaim : 0;
                case Anuncio.EncomendaBonus: case Anuncio.EncomendaTroca: return _sim.OrderItem >= 0 ? _sim.OrderIndex + 1 : 0;
                default: return Pago[(int)a] + 1;
            }
        }

        /// <summary>Vistos hoje, no total (dia das metas maior que o gravado = dia novo, contadores zerados).</summary>
        public int HojeTotal()
        {
            if (_sim.Metas.Dia > HojeDia) return 0;
            int n = 0;
            foreach (int h in Hoje) n += h;
            return n;
        }

        int HojeDe(Anuncio a) => _sim.Metas.Dia > HojeDia ? 0 : Hoje[(int)a];

        /// <summary>O botao do placement aparece agora (sem contar se o SDK tem anuncio pronto: isso e' o IAds.Ready).</summary>
        public bool Pode(Anuncio a)
        {
            AdPlacementDef d = Def(a);
            long k = Chave(a);
            if (!d.Ligado || !_sim.AdsUnlocked || k <= Pago[(int)a]) return false;
            if (HojeTotal() >= _sim.T.AnunciosTetoDia || (d.TetoDia > 0 && HojeDe(a) >= d.TetoDia)) return false;
            switch (a)
            {
                case Anuncio.Vip: return _sim.CanSummonVip;
                case Anuncio.Velocidade: return _sim.CanBoost;
                case Anuncio.EncomendaTroca: return Pago[(int)Anuncio.EncomendaBonus] < k;   // a troca perderia o bonus ja visto desta encomenda
                case Anuncio.Supply: return Recarga[(int)a] <= 0f && Previa(a) > 0;
                default: return Previa(a) > 0;
            }
        }

        /// <summary>O que o placement da agora, para o botao mostrar ANTES de assistir: VIP = x do pacote; Velocidade = o multiplicador que fica
        /// (2 ou 3); Cofre = ouro a mais; bonus = ouro a mais no premio da encomenda; Supply = lingotes; Trocar = 1 (a encomenda: ProximaEncomenda).</summary>
        public long Previa(Anuncio a)
        {
            float v = Def(a).Valor;
            switch (a)
            {
                case Anuncio.Vip: return (long)v;
                case Anuncio.Velocidade: return _sim.BoostLeft > 0f ? (long)_sim.BoostMul + 1 : 2;
                case Anuncio.Cofre: return (long)Math.Round(_sim.LastClaimGold * (double)v);
                case Anuncio.EncomendaBonus: return _sim.OrderItem < 0 ? 0 : Math.Max(5, 5 * (long)Math.Round(_sim.OrderReward * (double)v / 5.0, MidpointRounding.AwayFromZero));
                case Anuncio.Supply:
                    long n = 0;   // v s de producao de cada fornalha aberta, no tempo de ciclo de agora (Fole e Fole duplo contam)
                    foreach (Station s in _sim.Stations) if (s.Unlocked && s.Kind == Kind.Furnace) n += (long)(v / s.Time);
                    return n;
                default: return 1;
            }
        }

        /// <summary>"Trocar encomenda": a que entra se o jogador assistir (P5: sem sorteio, mostrada antes; = a natural seguinte do rodizio).</summary>
        public (Item item, int target, int reward) ProximaEncomenda() => _sim.OrderAt(_sim.OrderIndex + 1);

        /// <summary>ID de transacao da oferta de agora ("id:chave"), ou null se o placement nao pode.</summary>
        public string Oferta(Anuncio a) => Pode(a) ? Def(a).Id + ":" + Chave(a).ToString(CultureInfo.InvariantCulture) : null;

        /// <summary>Aplica a recompensa da oferta `tx` (pedida no toque), 1x. Devolve o que deu (a Previa: ouro, lingotes, multiplicador) ou -1:
        /// ID ja pago, oferta que mudou ou regra que nao deixa mais (nada muda).</summary>
        public long Pagar(string tx)
        {
            if (tx == null) return -1;
            for (int i = 0; i < N; i++)
            {
                var a = (Anuncio)i;
                if (Oferta(a) != tx) continue;
                long k = Chave(a), v = Previa(a);
                switch (a)
                {
                    case Anuncio.Vip: if (!_sim.SummonVip()) return -1; break;
                    case Anuncio.Velocidade: v = (long)_sim.StartBoost(); if (v <= 0) return -1; break;
                    case Anuncio.Cofre: _sim.Gold += v; break;   // como o cofre: fora do GoldEarned (nao infla a taxa online)
                    case Anuncio.EncomendaBonus: _sim.OrderReward += (int)v; break;   // pago na entrega, junto com o premio
                    case Anuncio.EncomendaTroca: _sim.TrocarEncomenda(); break;
                    case Anuncio.Supply: Lingotes += (int)v; Recarga[i] = Def(a).Recarga; break;
                }
                Pago[i] = k;
                if (_sim.Metas.Dia > HojeDia) { Array.Clear(Hoje, 0, N); HojeDia = _sim.Metas.Dia; }
                Hoje[i]++;
                return v;
            }
            return -1;
        }

        /// <summary>Do Sim.Tick: recargas no tempo de jogo e o Supply Drop entrando nas saidas das fornalhas abertas, ate o teto de cada uma.</summary>
        internal void Tick(float dt)
        {
            for (int i = 0; i < N; i++) if (Recarga[i] > 0f) Recarga[i] = Math.Max(0f, Recarga[i] - dt);
            if (Lingotes <= 0) return;
            foreach (Station s in _sim.Stations)
            {
                if (!s.Unlocked || s.Kind != Kind.Furnace) continue;
                int n = Math.Min(Lingotes, s.OutCap - s.Out);
                if (n > 0) { s.Out += n; Lingotes -= n; }
            }
        }

        // ------------------------------------------------------------------ intersticial (E-PLAT-03)

        /// <summary>
        /// GDD §17.2 (V10/P3): por que NAO mostrar o intersticial agora (null = pode). Flag Tuning.Interstitial desligada (o padrao) = sempre
        /// "flag". Ligada, os 5 vetos: "remove_ads" (entitlement), "tutorial" (antes das Ferramentas), "acao" (fora do estado Transition, ou com
        /// acao em curso: toque/joystick, compra, golpe; painel aberto ja nao e' Transition), "cooldown" (s de sessao desde o ultimo) e "cap"
        /// (por sessao). `agora` = s de sessao.
        /// </summary>
        public string VetoIntersticial(bool transicao, bool acao, float agora) =>
            !_sim.T.Interstitial ? "flag"
            : RemoveAds ? "remove_ads"
            : !_sim.Bought[(int)Upgrade.Tools] ? "tutorial"
            : !transicao || acao ? "acao"
            : agora - _ultimoIntersticial < _sim.T.InterstitialCooldown ? "cooldown"
            : IntersticiaisSessao >= _sim.T.InterstitialMaxSessao ? "cap"
            : null;

        public void IntersticialMostrado(float agora) { IntersticiaisSessao++; _ultimoIntersticial = agora; }

        // ------------------------------------------------------------------ save (linhas ad.*; o Sim.Load de antes ignora chave desconhecida)

        /// <summary>So o que nao e' o padrao: sem anuncio visto o texto do save fica igual ao de antes.</summary>
        public void Save(StringBuilder sb)
        {
            var ci = CultureInfo.InvariantCulture;
            for (int i = 0; i < N; i++)
                if (Pago[i] != 0 || Hoje[i] != 0 || Recarga[i] > 0f)
                    sb.Append("ad.").Append(_sim.T.Placements[i].Id).Append('=').Append(Pago[i].ToString(ci)).Append(',').Append(Hoje[i].ToString(ci))
                      .Append(',').Append(Recarga[i].ToString("0.###", ci)).Append('\n');
            if (HojeDia != 0) sb.Append("ad.dia=").Append(HojeDia.ToString(ci)).Append('\n');
            if (Lingotes > 0) sb.Append("ad.lingotes=").Append(Lingotes.ToString(ci)).Append('\n');
        }

        /// <summary>Le as linhas ad.* (lixo = padrao; Id que sumiu = ignorado; recarga no teto do knob).</summary>
        public void Load(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                if (!raw.StartsWith("ad.", StringComparison.Ordinal)) continue;
                int eq = raw.IndexOf('=');
                if (eq < 0) continue;
                string key = raw.Substring(3, eq - 3).Trim(), val = raw.Substring(eq + 1).Trim();
                if (key == "dia") { HojeDia = L(val, 0); continue; }
                if (key == "lingotes") { Lingotes = (int)Math.Max(0, Math.Min(100000, L(val, 0))); continue; }
                int i = Array.FindIndex(_sim.T.Placements, d => d.Id == key);
                string[] p = val.Split(',');
                if (i < 0 || p.Length < 3) continue;
                Pago[i] = Math.Max(0, L(p[0], 0));
                Hoje[i] = (int)Math.Max(0, Math.Min(int.MaxValue, L(p[1], 0)));
                Recarga[i] = float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) && !float.IsNaN(r)
                    ? Math.Max(0f, Math.Min(_sim.T.Placements[i].Recarga, r)) : 0f;
            }
        }

        static long L(string s, long d) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : d;
    }
}
