using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Ajudantes: o que cada um pega e leva (WorkerThink). Parte movida do Sim.cs sem mudar codigo.</summary>
    public sealed partial class Sim
    {
        // ------------------------------------------------------------------ ajudantes

        void WorkerThink(Carrier w, float dt)
        {
            if (w.State == 0)
            {
                if (w.Count >= w.Cap || (w.Count > 0 && w.IdleT > Balance.WorkerPatience)) { w.State = 1; w.Target = -1; w.IdleT = 0f; }
            }
            else if (w.Count == 0) { w.State = 0; w.Target = -1; w.IdleT = 0f; }

            Station t;
            if (w.Role == 3) t = w.State == 0 ? JewelerSource(w) : w.Item == Item.Jewel ? JewelShop : JewelBench;   // joalheiro: re-escolhe todo tick
            else
            {
                t = w.Target >= 0 ? Stations[w.Target] : null;
                bool valid = t != null && t.Unlocked && (w.State == 0 ? IsSourceFor(w, t) : IsDestFor(w, t));
                if (!valid) t = w.State == 0 ? PickSource(w) : PickDest(w);
            }
            w.Target = t != null ? t.Index : -1;
            if (t == null) { w.Moving = false; return; }
            MoveTowards(w, w.State == 0 ? t.OutAt : t.InAt, dt, Balance.WorkerReach);   // buscando -> saida da fonte; entregando -> entrada do destino
        }

        // C-CORE-01: os papeis pelo ITEM, nao pelo tipo da estacao (no Old Forge da o mesmo: so fornalha faz lingote, so bancada faz produto,
        // so fornalha recebe minerio e so bancada recebe lingote). Na Row a prensa e a rebitadeira (fornalhas) recebem lingote do papel 1, e o
        // papel 0 leva o bruto de qualquer deposito (minerio ou couro)
        bool IsSourceFor(Carrier w, Station s)
        {
            if (w.Role == 0) return s.Kind == Kind.Deposit && (w.Count == 0 || s.OutItem == w.Item);
            if (w.Count > 0 && s.OutItem != w.Item) return false;
            if (w.Role == 2 && !Fits(w, s)) return false;
            return s.Produces && (w.Role == 1 ? Intermediario(s.OutItem) : IsProduct(s.OutItem)) && (s.Out > 0 || w.Target == s.Index)
                && FonteDeInsumo(w, s);   // row-pendencias (Sim.Transporte.cs): so com 2+ insumos
        }

        /// <summary>Ajudante de produto: so busca o que ainda cabe na prateleira (sem a compra direta o estoque so desce com cliente
        /// na vaga; com a prateleira cheia ele parava no balcao e escudo/ferramenta encalhavam). Mesma regra do JewelerSource.</summary>
        bool Fits(Carrier w, Station s) => Stock[(int)s.OutItem] + w.Count < CounterCap || CabeNaCota(s.OutItem, w.Count);   // C-CORE-07: ou o patio

        bool IsDestFor(Carrier w, Station s)
        {
            // C-CORE-07 (CONTEUDO_V1 §2.4 regra 2): o produto vai ao patio quando ha faixa dele com vaga; senao ao balcao
            if (w.Role == 2) return s.Kind == Kind.Quota || s.Kind == Kind.Export ? Destino(w.Item) == s : Sells(s, w.Item) && Stock[(int)w.Item] < CounterCap && !CotaAceita(w.Item);   // C-CORE-09: ou o de exportacao
            return s.Accepts(w.Item);
        }

        Station PickSource(Carrier w)
        {
            if (w.Role == 0) return PickDeposit(w);
            Station best = null; float bestScore = float.MinValue;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked || !s.Produces || (w.Role == 1 ? !Intermediario(s.OutItem) : !IsProduct(s.OutItem))) continue;
                if (w.Count > 0 && s.OutItem != w.Item) continue;
                if (w.Role == 2 && !Fits(w, s)) continue;
                if (s.Out == 0 && w.Count == 0 && !(s.Busy && s.Progress > 0.5f)) continue;   // vale esperar o que esta quase pronto
                if (!FonteDeInsumo(w, s)) continue;   // row-pendencias (Sim.Transporte.cs): so com 2+ insumos
                float score = s.Out * 10f - V2.Dist(w.Pos, s.Pos) + (w.Role == 1 && Distrito.InsumoPorEspera ? 10f * Esperam(s.OutItem) : 0f);   // C-CORE-07
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        /// <summary>Papel 0: o deposito aberto (com o bruto que ja esta na mao) cujo item tem estacao aberta com mais espaco; empate pela
        /// distancia. Um deposito so (Old Forge): ele, sempre.</summary>
        Station PickDeposit(Carrier w)
        {
            Station best = Deposit; float bestScore = float.MinValue; int n = 0;
            foreach (Station s in Stations)
            {
                if (s.Kind != Kind.Deposit || !s.Unlocked || (w.Count > 0 && s.OutItem != w.Item)) continue;
                n++;
                int room = 0;
                foreach (Station d in Stations) { int k = d.InIndex(s.OutItem); if (d.Unlocked && k >= 0) room = Math.Max(room, d.InCap - d.Ins[k]); }
                float score = room * 4f - V2.Dist(w.Pos, s.Pos);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return n > 1 ? best : Deposit;
        }

        Station PickDest(Carrier w)
        {
            if (w.Role == 2) return Destino(w.Item);   // C-CORE-07: o patio ou o balcao
            Station best = null; float bestScore = float.MinValue;
            foreach (Station s in Stations)
            {
                int k = s.InIndex(w.Item);   // -1 = nao produz ou a receita nao usa
                if (!s.Unlocked || k < 0) continue;
                int room = s.InCap - s.Ins[k];
                if (room <= 0) continue;
                float score = room * 4f + Need(s) - V2.Dist(w.Pos, s.Pos);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        /// <summary>
        /// Joalheiro (papel 3): vazio (ou ja com joia), joia pronta e espaco na loja -> bancada de joias; senao, enquanto a
        /// bancada de joias ainda cabe lingote, a fornalha com mais lingote (a atual ganha empate: nao fica indo e voltando).
        /// Bancada abastecida = null: nao tira lingote das outras linhas. Action so deixa ele mexer no proprio alvo.
        /// </summary>
        Station JewelerSource(Carrier w)
        {
            bool jewel = w.Count > 0 && w.Item == Item.Jewel;
            if ((w.Count == 0 || jewel) && JewelBench.Out > 0 && Stock[(int)Item.Jewel] + w.Count < CounterCap) return JewelBench;
            if (jewel || JewelBench.In + w.Count >= JewelBench.InCap) return null;
            Station best = null; float bestScore = float.MinValue;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked || s.Kind != Kind.Furnace || (s.Out == 0 && !s.Busy)) continue;
                float score = s.Out * 10f - V2.Dist(w.Pos, s.Pos) + (s.Index == w.Target ? 5f : 0f);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        /// <summary>Quanto o balcao que vende o produto desta bancada precisa dele (cliente da frente esperando pesa muito).</summary>
        float Need(Station crafter)
        {
            if (crafter.Kind != Kind.Crafter) return 0f;
            Item it = crafter.OutItem;
            List<Client> q = QueueFor(it);
            float n = (CounterCap - Stock[(int)it]) * 2f;
            if (q.Count > 0 && q[0].Want == it && Stock[(int)it] == 0) n += 12f;
            return n;
        }
    }
}
