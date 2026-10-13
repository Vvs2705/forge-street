using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// B-CORE-06: Reputation ("Renome", GDD §14, V9): trilha monotonica, nunca moeda. Um total `long` que so sobe (Ganha ignora n &lt;= 0 e
    /// satura no teto) e o nivel derivado dos limiares (RenomeDefs). Nao existe gasto nem setter publico; nenhum caminho reduz (RenomeTests).
    /// Fontes ativas hoje (ECONOMIA_WAVE_B §5.1): encomenda entregue +1 e bau aberto +5 pelo fluxo de Ev (gancho unico: Sim.Emit chama On),
    /// peca Superior vendida (+1 a cada 10; o Sim.Sell chama OnVendaQualidade) e as metas (o Metas.Paga credita aqui depois do ledger dele).
    /// Ganchos prontos: Credita/OnCharter (Masterwork mw:, colecao col:, contrato ctr:, Charter charter:, com ID de transacao: 1x).
    /// Save: linha rn=total,superiores[,ids]. Save sem ela (0.1-0.7) = retroativo deterministico, 1x pelo ID rn:retro: encomendas entregues +
    /// 5 x baus abertos (GDD §14) + o Renome que as metas ja tinham guardado (mt.acc; sem pagar 2x: com rn= ele nao entra de novo). As
    /// Superiores vendidas antes do rn= nao entram: nenhum save guarda essa contagem (so o da main entre o B-CORE-03 e o B-CORE-06 as teve).
    /// Renome nao paga ouro: o Sim e o bot seguem identicos com ou sem ele.
    /// </summary>
    public sealed class Renome
    {
        public const string TxRetro = "rn:retro";
        /// <summary>A Masterwork nunca e' vendida (vai ao Hall): so a Superior chega aqui como "Superior ou melhor".</summary>
        public const int Superior = (int)Quality.Superior;

        readonly Sim _sim;
        /// <summary>IDs de transacao ja creditados (fontes 1x). As de fluxo (encomenda, bau, metas) nao passam por aqui: cada uma acontece 1x no Sim.</summary>
        public readonly HashSet<string> Ledger = new HashSet<string>();
        public long Total { get; private set; }
        /// <summary>Pecas Superiores vendidas desde o B-CORE-06 (a cada RenomeDefs.SuperiorACada, +1).</summary>
        public long Superiores { get; private set; }

        public Renome(Sim sim) { _sim = sim; }

        public int Nivel
        {
            get
            {
                for (int i = RenomeDefs.Niveis.Length - 1; i > 0; i--) if (Total >= RenomeDefs.Niveis[i].Limiar) return i;
                return 0;
            }
        }

        public RenomeDef Atual => RenomeDefs.Niveis[Nivel];
        /// <summary>Proximo nivel, ou null no ultimo.</summary>
        public RenomeDef Proximo => Nivel + 1 < RenomeDefs.Niveis.Length ? RenomeDefs.Niveis[Nivel + 1] : null;

        /// <summary>O nivel atual ja liberou `id` (RenomeDefs.Niveis[..].Libera)? ID que nenhum nivel libera = false.</summary>
        public bool Libera(string id)
        {
            int n = RenomeDefs.NivelQueLibera(id);
            return n >= 0 && n <= Nivel;
        }

        /// <summary>So sobe: n &lt;= 0 nao faz nada; satura em long.MaxValue em vez de virar negativo.</summary>
        public void Ganha(long n)
        {
            if (n <= 0) return;
            Total = n > long.MaxValue - Total ? long.MaxValue : Total + n;
        }

        /// <summary>Credito unico por ID de transacao: ID ja no ledger = nada muda e devolve false.</summary>
        public bool Credita(string tx, long n)
        {
            if (!Ledger.Add(tx)) return false;
            Ganha(n);
            return true;
        }

        /// <summary>Fluxo de eventos do Sim (Sim.Emit).</summary>
        public void On(Ev kind, int a, int b)
        {
            if (kind == Ev.OrderDone) Ganha(RenomeDefs.Encomenda);
            else if (kind == Ev.ChestOpened) Ganha(RenomeDefs.Bau);
        }

        /// <summary>Peca vendida no nivel `nivel` (Sim.Sell, B-CORE-03): Superior conta e cada 10 dao +1.</summary>
        public void OnVendaQualidade(int nivel)
        {
            if (nivel < Superior) return;
            if (++Superiores % RenomeDefs.SuperiorACada == 0) Ganha(1);
        }

        /// <summary>Royal Charter do distrito (Wave C): +100, 1x por distrito.</summary>
        public bool OnCharter(string distrito) => Credita("charter:" + distrito, RenomeDefs.Charter);

        /// <summary>Save 0.1-0.7 (sem rn=): encomendas entregues + 5 x baus abertos + o que as metas guardaram, 1x pelo ID.</summary>
        void Retro()
        {
            long baus = _sim.Chests.FindAll(c => c.State == 2).Count;
            Credita(TxRetro, _sim.OrderCount + RenomeDefs.Bau * baus + _sim.Metas.Renome);
        }

        public void Save(StringBuilder sb)
        {
            var ids = new List<string>(Ledger);
            ids.Sort(StringComparer.Ordinal);
            sb.Append("rn=").Append(S(Total)).Append(',').Append(S(Superiores));
            foreach (string id in ids) sb.Append(',').Append(id);
            sb.Append('\n');
        }

        /// <summary>Le a linha rn= (lixo = 0, negativo = 0). Sem ela: retroativo. Chamar depois do Metas.Load (o retroativo le o Metas.Renome).</summary>
        public void Load(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                if (!raw.StartsWith("rn=", StringComparison.Ordinal)) continue;
                string[] p = raw.Substring(3).Trim().Split(',');
                Total = Math.Max(0, L(p[0]));
                Superiores = p.Length > 1 ? Math.Max(0, L(p[1])) : 0;
                for (int i = 2; i < p.Length; i++) if (p[i].Length > 0) Ledger.Add(p[i]);
                return;
            }
            Retro();
        }

        static string S(long v) => v.ToString(CultureInfo.InvariantCulture);
        static long L(string s) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : 0;
    }
}
