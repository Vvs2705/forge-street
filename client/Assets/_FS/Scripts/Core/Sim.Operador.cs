using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    public sealed partial class Station
    {
        /// <summary>Estacao precisa de operador (decisao do Vinicius, 2026-11-21): nivel do operador desta estacao (0 = ninguem; 1 = contratado).
        /// Sem operador (e com Tuning.OperadorObrigatorio) ela so produz com o ferreiro na boca (Sim.OperandoAgora). Vai no save (ops=).</summary>
        public int Operador;
        /// <summary>Ouro ja pago no pad do operador (parcial, como o Pad.Paid; vai no save) e a fracao do dreno que ainda nao virou ouro (fora).</summary>
        public long OperadorPago; public double OperadorAcc;
        /// <summary>Onde fica o pad "Contratar operador" desta estacao (Sim.PosOperador: ao lado do corpo, fora das bocas, dos pads e dos corpos).</summary>
        public V2 OperadorPos;
    }

    /// <summary>
    /// Estacao precisa de operador (Vinicius, 2026-11-21: "nenhuma estacao trabalha sozinha; cada estacao precisa de um funcionario trabalhando").
    /// Toda estacao que produz (Station.Produces, nos 6 distritos) tem um operador (Station.Operador). Sem ele ela so produz enquanto o ferreiro
    /// esta numa boca dela (o mesmo alcance de entregar e pegar, Balance.MouthRadius), no ritmo de sempre; o ciclo pausa quando ele sai e
    /// continua quando volta. Com operador, produz sozinha como antes. Os niveis da estacao (II-V), o oficial, o mestre de obras e os boosts
    /// seguem somando velocidade e qualidade (o operador nao mexe em nenhum numero: e' a chave de ligar).
    /// Contratar: pad no chao ao lado da estacao (Station.OperadorPos), que cobra como os pads de upgrade (Balance.PadDrainSeconds, PadDwell), ou
    /// ContratarOperador (toque/menu: cobra na hora). Custo por estacao em dados (DistrictDef.OperadorKit/OperadorFracao/Operadores; CustoOperador).
    /// Flag de rollback: Tuning.OperadorObrigatorio (ligada; desligada = o jogo de antes, ao byte, e o pad some).
    /// Save: linha ops=id:nivel[:pago],... sempre gravada; save SEM ela (0.5.1 e o schema 3 de antes) = toda estacao aberta ganha operador
    /// (ninguem perde progresso). ponytail: nivel do operador so vai a 1; o +saida por nivel fica para quando houver o que comprar nele.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Nivel maximo do operador hoje (1 = contratado). O save aceita mais (build mais novo), cortado aqui.</summary>
        public const int OperadorMax = 1;

        Station _opUnder; float _opDwell;

        /// <summary>Esta estacao precisa de operador e ainda nao tem: flag ligada, produz, aberta, nenhum contratado. A View mostra o pad
        /// "Contratar operador" (Station.OperadorPos, CustoOperador) e a estacao "parada" quando isto e' true.</summary>
        public bool PrecisaOperador(Station s) => T.OperadorObrigatorio && s.Produces && s.Unlocked && s.Operador <= 0;

        /// <summary>O ferreiro esta numa boca da estacao (entrada ou saida), no alcance de entregar e pegar.</summary>
        public bool FerreiroNaBoca(Station s) =>
            V2.Dist(Player.Pos, s.InAt) <= Balance.MouthRadius || (s.Produces && V2.Dist(Player.Pos, s.OutAt) <= Balance.MouthRadius);

        /// <summary>A estacao trabalha agora: tem operador (ou a flag esta desligada), ou o ferreiro esta na boca dela. A View usa para o
        /// "aprendiz"/operador presente (Operador &gt; 0) e o ferreiro forjando a mao (PrecisaOperador e FerreiroNaBoca).</summary>
        public bool OperandoAgora(Station s) => !PrecisaOperador(s) || FerreiroNaBoca(s);

        /// <summary>Ouro para contratar o operador de `s` (o total; o pad ja pagou s.OperadorPago). Explicito no DistrictDef.Operadores, senao
        /// OperadorKit para quem abre desde o inicio e OperadorFracao x o custo do upgrade que abre a estacao (multiplo de 5, no minimo o Kit).</summary>
        public long CustoOperador(Station s)
        {
            if (Distrito.Operadores != null)
                foreach ((string id, long custo) in Distrito.Operadores) if (id == s.Id) return custo;
            if (s.UnlockBy < 0) return Distrito.OperadorKit;
            long c = (long)Math.Round(Distrito.OperadorFracao * T.Cost(s.UnlockBy) / 5.0) * 5;
            return Math.Max(Distrito.OperadorKit, c);
        }

        /// <summary>Contratacao pelo toque/menu: cobra o que falta na hora (Gold &gt;= CustoOperador - pago). Senao false e nada muda.</summary>
        public bool ContratarOperador(Station s)
        {
            if (s == null || !PrecisaOperador(s)) return false;
            long falta = CustoOperador(s) - s.OperadorPago;
            if (Gold < falta) return false;
            Gold -= falta;
            Contrata(s);
            return true;
        }

        void Contrata(Station s)
        {
            s.Operador = 1; s.OperadorPago = 0; s.OperadorAcc = 0;
            Emit(Ev.OperadorContratado, s.Index, (int)Math.Min(int.MaxValue, CustoOperador(s)), s.OperadorPos);
        }

        /// <summary>Estacao cujo pad de operador (visivel: PrecisaOperador) contem `p`, ou null.</summary>
        public Station OperadorPadAt(V2 p)
        {
            foreach (Station s in Stations)
                if (PrecisaOperador(s) && V2.Dist(p, s.OperadorPos) <= Balance.PadRadius) return s;
            return null;
        }

        /// <summary>Estacao sem operador mais barata cujo restante o ouro ja paga inteiro, ou null (o bot vai ao pad dela antes de tudo).</summary>
        public Station OperadorPagavel()
        {
            Station best = null; long bestFalta = long.MaxValue;
            foreach (Station s in Stations)
            {
                if (!PrecisaOperador(s)) continue;
                long falta = CustoOperador(s) - s.OperadorPago;
                if (falta <= Gold && falta < bestFalta) { bestFalta = falta; best = s; }
            }
            return best;
        }

        /// <summary>O ferreiro em cima do pad do operador: paga como o pad de upgrade (parado na hora; andando, so depois do PadDwell).</summary>
        void InteractOperador(Carrier c, float dt)
        {
            Station s = OperadorPadAt(c.Pos);
            if (s != _opUnder) { _opUnder = s; _opDwell = 0f; }
            if (s == null) return;
            _opDwell += dt;
            if (c.Moving && _opDwell < Balance.PadDwell) return;
            long cost = CustoOperador(s);
            s.OperadorAcc += cost / Balance.PadDrainSeconds * dt;
            long want = (long)s.OperadorAcc;
            s.OperadorAcc -= want;
            long step = Math.Min(want, Math.Min(Gold, cost - s.OperadorPago));
            if (step <= 0) return;
            Gold -= step;
            s.OperadorPago += step;
            if (s.OperadorPago >= cost) Contrata(s);
        }

        /// <summary>
        /// Onde fica o pad do operador de `s`: a 1a posicao, ao lado do corpo (perpendicular as bocas, depois na diagonal), dentro das paredes, a
        /// CharRadius de todo corpo possivel (estacoes, balcao com 8 vagas, obstaculos), a 1 m (MouthRadius + PadRadius) de toda boca e a 2 x
        /// PadRadius dos pads de upgrade que aparecem no mundo, dos baus e do ponto de contratacao. Nenhuma serve = a de maior folga (o ContentValidator acusa).
        /// </summary>
        V2 PosOperador(Station s)
        {
            var corpos = new List<Box>();
            foreach (Station o in Stations)
                corpos.Add(o.Kind == Kind.Counter && o == Counter ? new Box(o.Pos, new V2(Balance.CounterHalfX(Balance.QueueCapMax), Balance.StationHalf.Y)) : o.Body);
            foreach (ObstacleDef ob in Distrito.Obstaculos) corpos.Add(ob.Body);
            var pontos = new List<(V2 P, float Min)>();
            foreach (Station o in Stations)
            {
                pontos.Add((o.InAt, Balance.MouthRadius + Balance.PadRadius));
                if (o.Produces) pontos.Add((o.OutAt, Balance.MouthRadius + Balance.PadRadius));
            }
            foreach (PadDef p in Distrito.Pads)   // o pad no lugar de uma estacao some quando ela abre; o de upgrade so de menu nunca aparece
                if (!Stations.Exists(o => V2.Dist(o.Pos, p.Pos) < 0.01f) && Array.Exists(p.Cadeia, id => { int u = Upgrades.IndexOf(id, UpgradeDefs); return u >= 0 && !UpgradeDefs[u].InMenu; }))
                    pontos.Add((p.Pos, 2f * Balance.PadRadius));
            foreach (MilestoneDef m in Distrito.Marcos) pontos.Add((m.Pos, 2f * Balance.PadRadius));
            pontos.Add((Distrito.Contratacao, 2f * Balance.PadRadius));
            foreach (Station o in Stations) if (o != s && o.Produces && o.Index < s.Index) pontos.Add((o.OperadorPos, 2f * Balance.PadRadius));
            V2 boca = s.InAt - s.Pos, eixo = boca * (1f / Math.Max(1e-5f, boca.Len)), perp = new V2(-eixo.Y, eixo.X);
            float meio = Math.Abs(perp.X) * s.Half.X + Math.Abs(perp.Y) * s.Half.Y;
            V2 best = s.Pos + perp * (-(meio + 0.8f)); float bestFolga = float.MinValue;
            foreach (float d in new[] { 0.8f, 1.0f, 1.3f, 0.5f })
                foreach (V2 dir in new[] { perp * -1f, perp, (perp * -1f + eixo) * 0.7071f, (perp * -1f - eixo) * 0.7071f, (perp + eixo) * 0.7071f, (perp - eixo) * 0.7071f })
                {
                    V2 c = s.Pos + dir * (meio + d);
                    float folga = Math.Min(Math.Min(c.X - 0.6f, Balance.WorldW - 0.6f - c.X), Math.Min(c.Y - 0.6f, Balance.WorldH - 0.6f - c.Y));   // o pad inteiro no mapa
                    foreach (Box b in corpos) folga = Math.Min(folga, b.Dist(c) - Balance.CharRadius);
                    foreach ((V2 p, float min) in pontos) folga = Math.Min(folga, V2.Dist(c, p) - min);
                    if (folga >= 0f) return c;
                    if (folga > bestFolga) { bestFolga = folga; best = c; }
                }
            return best;
        }

        void PreparaOperadores()
        {
            foreach (Station s in Stations) if (s.Produces) s.OperadorPos = PosOperador(s);
        }

        // ------------------------------------------------------------------ save (linha ops=; sem ela = save de antes: toda aberta ganha operador)

        void SalvaOperadores(StringBuilder sb)
        {
            var t = new List<string>();
            foreach (Station s in Stations)
            {
                if (!s.Produces || (s.Operador <= 0 && s.OperadorPago <= 0)) continue;
                string tok = s.Id + ":" + s.Operador.ToString(CultureInfo.InvariantCulture);
                if (s.OperadorPago > 0) tok += ":" + s.OperadorPago.ToString(CultureInfo.InvariantCulture);
                t.Add(tok);
            }
            sb.Append("ops=").Append(string.Join(",", t)).Append('\n');
        }

        /// <summary>Depois do Recompute (as estacoes abertas ja sao conhecidas). Parcial de estacao que nao precisa mais (aberta com operador,
        /// fechada, ou ID que o build nao tem) volta ao ouro.</summary>
        void CarregaOperadores(string text)
        {
            string val = null;
            foreach (string raw in text.Split('\n'))
                if (raw.StartsWith("ops=", StringComparison.Ordinal)) { val = raw.Substring(4).Trim(); break; }
            if (val == null)   // save de antes do operador: ninguem perde a producao que tinha
            {
                foreach (Station s in Stations) if (s.Produces && s.Unlocked) s.Operador = 1;
                return;
            }
            foreach (string tok in val.Split(','))
            {
                string[] kv = tok.Split(':');
                string id = kv[0].Trim();
                if (id.Length == 0) continue;
                Station s = Est(id);
                if (s == null || !s.Produces) { UnknownIds++; if (kv.Length > 2) Gold += Math.Max(0, L(kv[2], 0)); continue; }
                s.Operador = kv.Length > 1 ? Clamp0(I(kv[1], 0), OperadorMax) : 0;
                long pago = kv.Length > 2 ? Math.Max(0, L(kv[2], 0)) : 0;
                if (s.Operador > 0 || !s.Unlocked) { Gold += pago; pago = 0; }
                s.OperadorPago = Math.Min(pago, Math.Max(0, CustoOperador(s) - 1));
                Gold += pago - s.OperadorPago;
            }
        }
    }
}
