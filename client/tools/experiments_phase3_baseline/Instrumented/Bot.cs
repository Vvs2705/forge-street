using System;

namespace FS.Core
{
    /// <summary>
    /// Autoplay: joga sozinho com uma heuristica simples (pad mais barato que ja da para pagar > entregar o que
    /// carrega > pegar produto pronto > pegar lingote pronto > buscar minerio). Serve para balancear a §3 do GDD
    /// (BalanceTests) e para o smoke `-autoplay` do build. Nao e' IA de jogo: e' um jogador mediano previsivel.
    /// </summary>
    public sealed class Bot
    {
        public const float Reach = 0.35f, ChestDetour = 3f;   // m: bau mais perto que isso desvia o caminho

        /// <summary>
        /// Handicap "humano" (HIPOTESE a calibrar no playtest com o diario): quando o alvo muda, para 0,7 s para "ler a
        /// tela"; e nao segura o analogico no maximo. Reaction = 0 e Stick = 1 e' o bot ideal (limite inferior de tempo).
        /// </summary>
        public float Reaction = 0.7f, Stick = 0.85f;

        V2 _target; bool _has; float _wait;

        public static Bot Ideal() => new Bot { Reaction = 0f, Stick = 1f };

        /// <summary>Um tick do jogo com o bot no joystick.</summary>
        public void Step(Sim s, float dt)
        {
            V2 t = Target(s);
            if (!_has || V2.Dist(t, _target) > 0.01f) { _target = t; _has = true; _wait = Reaction; }
            if (_wait > 0f) { _wait -= dt; s.Tick(dt, 0f, 0f); return; }
            V2 d = Toward(s, t);
            s.Tick(dt, d.X * Stick, d.Y * Stick);
        }

        /// <summary>Direcao do joystick (comprimento 0..1) para o alvo atual, sem handicap; zero quando chegou.</summary>
        public static V2 Decide(Sim s) => Toward(s, Target(s));

        static V2 Toward(Sim s, V2 target)
        {
            V2 d = target - s.Player.Pos;
            float len = d.Len;
            return len <= Reach ? new V2(0f, 0f) : d * (1f / len);
        }

        public static V2 Target(Sim s)
        {
            Carrier p = s.Player;
            Pad pad = s.CheapestAffordablePad();
            if (pad != null) return pad.Pos;
            Chest chest = null;   // bau disponivel mais perto: abre quando passa perto ou quando nao tem nada melhor (nao e' otimo)
            foreach (Chest c in s.Chests)
                if (c.State == 1 && (chest == null || V2.Dist(p.Pos, c.Pos) < V2.Dist(p.Pos, chest.Pos))) chest = c;
            if (chest != null && V2.Dist(p.Pos, chest.Pos) < ChestDetour) return chest.Pos;

            if (p.Count > 0)
            {
                if (Sim.IsProduct(p.Item)) return s.CounterFor(p.Item).Pos;   // joia -> loja de joias, o resto -> balcao
                Station dest = BestInput(s, p.Item, p.Pos);
                if (dest != null) return dest.Pos;
                return p.Item == Item.Ore ? s.FurnaceA.Pos : s.AnvilA.Pos;   // tudo cheio: espera na fila da primeira
            }

            Station pick = BestOutput(s, Kind.Crafter, p.Pos);
            if (pick != null) return pick.Pos;
            pick = BestOutput(s, Kind.Furnace, p.Pos);
            if (pick != null && BestInput(s, Item.Ingot, pick.Pos) != null) return pick.Pos;
            if (BestInput(s, Item.Ore, p.Pos) != null) return s.Deposit.Pos;
            if (chest != null) return chest.Pos;
            // fornalhas cheias e nada pronto: espera onde o proximo lingote vai sair
            Station soon = null;
            foreach (Station st in s.Stations)
                if (st.Unlocked && st.Kind == Kind.Furnace && st.Busy && (soon == null || st.Progress > soon.Progress)) soon = st;
            return soon != null ? soon.Pos : s.FurnaceA.Pos;
        }

        /// <summary>Estacao desbloqueada que aceita `item`, com mais espaco e mais necessidade; empate pela distancia.</summary>
        static Station BestInput(Sim s, Item item, V2 from)
        {
            Station best = null; float bestScore = float.MinValue;
            foreach (Station st in s.Stations)
            {
                if (!st.Unlocked || !st.Produces || st.InItem != item || st.In >= st.InCap) continue;
                float need = 0f;
                if (st.Kind == Kind.Crafter)
                {
                    var q = s.QueueFor(st.OutItem);
                    need = (s.CounterCap - s.Stock[(int)st.OutItem]) * 2f;
                    if (q.Count > 0 && q[0].Want == st.OutItem && s.Stock[(int)st.OutItem] == 0) need += 12f;
                }
                float score = (st.InCap - st.In) * 3f + need - V2.Dist(from, st.Pos);
                if (score > bestScore) { bestScore = score; best = st; }
            }
            return best;
        }

        static Station BestOutput(Sim s, Kind kind, V2 from)
        {
            Station best = null; float bestScore = float.MinValue;
            foreach (Station st in s.Stations)
            {
                if (st.Index == s.ExperimentalLateralIndex) continue;
                if (!st.Unlocked || st.Kind != kind || st.Out <= 0) continue;
                if (kind == Kind.Crafter && s.Stock[(int)st.OutItem] >= s.CounterCap && st.Out < st.OutCap) continue; // balcao cheio desse produto: deixa na bancada
                float score = st.Out * 5f - V2.Dist(from, st.Pos);
                if (score > bestScore) { bestScore = score; best = st; }
            }
            return best;
        }
    }
}
