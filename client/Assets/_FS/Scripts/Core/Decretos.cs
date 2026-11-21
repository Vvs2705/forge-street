using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Como o decreto conta: degraus das Obras; um contador vitalicio das metas da raiz (Metas.Cont); vendas do item mais avancado do
    /// distrito ativo (fixado quando o decreto aparece); ou ouro de venda em s da renda alvo (Obras.RendaRef), congelado quando aparece.</summary>
    public enum ContaDecreto { Obras, Contador, Item, Renda }

    /// <summary>Decreto semanal (nome em Loc "dcr.{id}", com o alvo). Paga Renome e XP do Pass, nunca ouro (o ralo das Obras nao vira fonte).</summary>
    public sealed class DecretoDef
    {
        public readonly string Id, Chave; public readonly ContaDecreto Conta; public readonly long Alvo; public readonly int Renome, Xp;
        public DecretoDef(string id, ContaDecreto conta, string chave, long alvo, int renome = 20, int xp = 200)
        { Id = id; Conta = conta; Chave = chave; Alvo = alvo; Renome = renome; Xp = xp; }
    }

    /// <summary>
    /// Decretos semanais (Conselho do TIME DE JOGOS, 2026-11-21): 3 metas por semana, abertas junto com as Obras (Charter do Old Forge).
    /// - Semana = semanas inteiras desde o LiveOpsDefs.T0 (segunda 13:00 UTC) no relogio do LiveOps da raiz (o maior instante ja visto: relogio
    ///   para tras nada reabre). Sem relogio (bot, testes sem Agora) nada roda.
    /// - Rodizio sem sorteio: vaga 0 = Obras[s % 2] (1 ou 2 etapas); vagas 1 e 2 = Pool[(2s) % 5] e Pool[(2s + 1) % 5], sempre distintas.
    /// - Base = o contador quando o decreto aparece (como as semanais das Metas); feito = contador - base, ate o alvo. Concluido paga sozinho
    ///   (como as missoes de evento) pelo Metas.Paga com o ID dc:{semana}:{vaga}: 1x, Renome no Sim.Renome e XP no Metas.PassXp (o Pass pega).
    /// - Save: linha dcr=semana;id:ref:base:alvo:pago;... do Imperio (sem ela = nenhuma semana vista; a 1a Agora monta a semana).
    /// </summary>
    public sealed class Decretos
    {
        public const int N = 3;
        public const long Semana7 = 7 * LiveOpsDefs.Dia;

        public static readonly DecretoDef[] DeObras =
        {
            new DecretoDef("dcr_obra1", ContaDecreto.Obras, null, 1),
            new DecretoDef("dcr_obra2", ContaDecreto.Obras, null, 2, 30, 300),
        };
        public static readonly DecretoDef[] Pool =
        {
            new DecretoDef("dcr_vende3000", ContaDecreto.Contador, "product_sold", 3000),
            new DecretoDef("dcr_enc30", ContaDecreto.Contador, "order_done", 30),
            new DecretoDef("dcr_vip20", ContaDecreto.Contador, "vip_served", 20),
            new DecretoDef("dcr_item400", ContaDecreto.Item, "product_sold", 400),
            new DecretoDef("dcr_renda2h", ContaDecreto.Renda, "currency_source", 7200),
        };

        public sealed class Slot { public DecretoDef Def; public string Ref; public long Base, Alvo; public bool Pago; }

        readonly Imperio _imp;
        public long Semana = long.MinValue;
        public readonly Slot[] Slots = new Slot[N];
        /// <summary>Decreto concluido e pago (a View drena: aviso). (id, vaga).</summary>
        public readonly List<(string Id, int Vaga)> Novidades = new List<(string, int)>();

        public Decretos(Imperio imp) { _imp = imp; }

        Sim Raiz => _imp.Raiz;
        public bool Aberta => _imp.Obras.Aberta && Raiz.LiveOps.Relogio > 0;

        public static long SemanaDe(long t) { long d = t - LiveOpsDefs.T0; return d >= 0 ? d / Semana7 : -((-d + Semana7 - 1) / Semana7); }

        /// <summary>s ate a virada da semana (0 = fechado).</summary>
        public long Restante => Aberta ? Math.Max(0, LiveOpsDefs.T0 + (Semana + 1) * Semana7 - Raiz.LiveOps.Relogio) : 0;

        public static DecretoDef[] DaSemana(long s)
        {
            int Mod(long a, int m) => (int)((a % m + m) % m);
            return new[] { DeObras[Mod(s, DeObras.Length)], Pool[Mod(2 * s, Pool.Length)], Pool[Mod(2 * s + 1, Pool.Length)] };
        }

        public static string Tx(long semana, int vaga) => "dc:" + semana.ToString(CultureInfo.InvariantCulture) + ":" + vaga.ToString(CultureInfo.InvariantCulture);

        /// <summary>Do Imperio.Fundo (a cada passo): semana nova monta as 3 vagas; vaga concluida paga.</summary>
        public void Tick()
        {
            if (!Aberta) return;
            long s = SemanaDe(Raiz.LiveOps.Relogio);
            if (s > Semana) Monta(s);
            for (int k = 0; k < N; k++)
            {
                Slot v = Slots[k];
                if (v == null || v.Pago || Feito(v) < v.Alvo) continue;
                v.Pago = true;
                if (Raiz.Metas.Paga(Tx(Semana, k), 0, v.Def.Xp, v.Def.Renome) >= 0) Novidades.Add((v.Def.Id, k));
            }
        }

        void Monta(long s)
        {
            Semana = s;
            string vale = Tx(s, 0).Substring(0, Tx(s, 0).Length - 1);
            Raiz.Metas.Ledger.RemoveWhere(t => t.StartsWith("dc:", StringComparison.Ordinal) && !t.StartsWith(vale, StringComparison.Ordinal));
            DecretoDef[] ds = DaSemana(s);
            for (int k = 0; k < N; k++)
            {
                var v = new Slot { Def = ds[k], Ref = ds[k].Conta == ContaDecreto.Item ? ItemDoAtivo() : null, Alvo = ds[k].Alvo };
                if (ds[k].Conta == ContaDecreto.Renda) v.Alvo = Math.Max(100, 5 * (long)Math.Round(_imp.Obras.RendaRef / 60.0 * ds[k].Alvo / 5.0));
                v.Base = Valor(v);
                Slots[k] = v;
            }
        }

        /// <summary>O produto mais avancado do distrito ativo (o ultimo da tabela dele que vai ao balcao).</summary>
        string ItemDoAtivo()
        {
            Sim a = _imp.Ativo;
            for (int i = a.ItemDefs.Length - 1; i >= 0; i--) if (a.IsProduct((Item)i)) return a.ItemDefs[i].Id;
            return a.ItemDefs[a.ItemDefs.Length - 1].Id;
        }

        long Valor(Slot v)
        {
            Metas m = Raiz.Metas;
            switch (v.Def.Conta)
            {
                case ContaDecreto.Obras: return _imp.Obras.Degraus;
                case ContaDecreto.Item:   // C-CORE-03: item da raiz pelo indice, o de outro distrito pelo ID (Metas.RegistraItem)
                    int i = Items.IndexOf(v.Ref, Raiz.ItemDefs);
                    return m.Cont.TryGetValue(i >= 0 ? Metas.Chave(v.Def.Chave, i) : v.Def.Chave + "." + v.Ref, out long n) ? n : 0;
                default: return m.Cont.TryGetValue(v.Def.Chave, out long c) ? c : 0;
            }
        }

        public long Feito(Slot v) => v == null ? 0 : Math.Max(0, Math.Min(v.Alvo, Valor(v) - v.Base));

        public void Save(StringBuilder sb)
        {
            if (Semana == long.MinValue) return;
            sb.Append("dcr=").Append(S(Semana));
            foreach (Slot v in Slots)
                sb.Append(';').Append(v == null ? "-" : v.Def.Id + ":" + (v.Ref ?? "-") + ":" + S(v.Base) + ":" + S(v.Alvo) + ":" + (v.Pago ? "1" : "0"));
            sb.Append('\n');
        }

        /// <summary>Valor da linha dcr= (lixo = nenhuma semana; decreto que sumiu dos dados = vaga vazia ate a semana seguinte).</summary>
        public void Load(string val)
        {
            string[] p = val.Split(';');
            if (!long.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long s)) return;
            Semana = s;
            for (int k = 0; k < N; k++)
            {
                string[] f = k + 1 < p.Length ? p[k + 1].Split(':') : new string[0];
                DecretoDef d = f.Length == 5 ? Array.Find(DeObras, x => x.Id == f[0]) ?? Array.Find(Pool, x => x.Id == f[0]) : null;
                Slots[k] = d == null ? null : new Slot { Def = d, Ref = f[1] == "-" ? null : f[1], Base = Math.Max(0, L(f[2])), Alvo = Math.Max(1, L(f[3])), Pago = f[4] == "1" };
            }
        }

        static string S(long v) => v.ToString(CultureInfo.InvariantCulture);
        static long L(string s) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : 0;
    }
}
