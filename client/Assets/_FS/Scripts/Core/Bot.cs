using System;

namespace FS.Core
{
    /// <summary>
    /// Perfil do bot (A-CORE-07, Bot 2.0) = dados, nao subclasse. F2P = o humano de sempre (`new Bot()`, o dos BalanceTests): nunca assiste
    /// anuncio. Slow = o "lento" do BALANCE §18.3 (1,5 s de reacao, 60% de stick). AdWatcher = o humano que assiste todo anuncio liberado.
    /// </summary>
    public struct BotProfile
    {
        public string Name; public float Reaction, Stick; public bool Ads;

        public static readonly BotProfile Ideal = new BotProfile { Name = "Ideal", Reaction = 0f, Stick = 1f };
        public static readonly BotProfile F2P = new BotProfile { Name = "F2P", Reaction = 0.7f, Stick = 0.85f };
        public static readonly BotProfile Slow = new BotProfile { Name = "Slow", Reaction = 1.5f, Stick = 0.6f };
        public static readonly BotProfile AdWatcher = new BotProfile { Name = "AdWatcher", Reaction = 0.7f, Stick = 0.85f, Ads = true };
        public static readonly BotProfile[] All = { Ideal, F2P, Slow, AdWatcher };
    }

    /// <summary>
    /// Autoplay: joga sozinho com uma heuristica simples (compra mais barata que ja da para pagar, no menu na hora ou indo ao pad > entregar o que
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
        public float Reaction = BotProfile.F2P.Reaction, Stick = BotProfile.F2P.Stick;

        /// <summary>Assiste anuncio (AdWatcher): chama o VIP quando pode e liga o boost 2x quando a recarga deixa. Conta os anuncios vistos.</summary>
        public bool Ads; public int AdsVip, AdsBoost;

        V2 _target; bool _has, _menu; float _wait;

        public static Bot Ideal() => From(BotProfile.Ideal);
        public static Bot From(BotProfile p) => new Bot { Reaction = p.Reaction, Stick = p.Stick, Ads = p.Ads };

        /// <summary>
        /// Um tick do jogo com o bot no joystick. Menu inferior (FASE6): mesma politica do pad (a compra mais barata que ja da
        /// para pagar), so que sem andar: para, "le a tela" (Reaction, como em toda troca de alvo) e toca. Compra depois do
        /// Tick para o Ev.Bought ficar em Events como o do pad.
        /// </summary>
        public void Step(Sim s, float dt)
        {
            if (Ads)
            {
                if (s.SummonVip()) AdsVip++;                                  // o Sim recusa fora de CanSummonVip
                if (s.BoostLeft <= 0f && s.StartBoost() > 0f) AdsBoost++;    // ponytail: so o 1o anuncio (2x); o 2o (3x) e' mais uma condicao
            }
            int menu = s.CheapestAffordableMenu();
            bool buy = menu >= 0;
            V2 t = buy ? s.Player.Pos : Target(s);
            if (!_has || buy != _menu || (!buy && V2.Dist(t, _target) > 0.01f)) { _target = t; _menu = buy; _has = true; _wait = Reaction; }
            if (_wait > 0f || buy)
            {
                _wait -= dt;
                s.Tick(dt, 0f, 0f);
                if (buy && _wait <= 0f) s.TryBuyMenu((Upgrade)menu);
                return;
            }
            V2 d = Toward(s, t);
            s.Tick(dt, d.X * Stick, d.Y * Stick);
        }

        /// <summary>Direcao do joystick (comprimento 0..1) para o alvo atual, sem handicap; zero quando chegou.</summary>
        public static V2 Decide(Sim s) => Toward(s, Target(s));

        static V2 Toward(Sim s, V2 target)
        {
            V2 d = target - s.Player.Pos;
            float len = d.Len;
            return len <= Reach ? new V2(0f, 0f) : s.Steer(s.Player.Pos, Sim.Via(s.Player.Pos, target));   // porta/arco da parede e contorno das estacoes
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

            // carga mista (FASE7 §1): produto na mao vai ao balcao (o mais adiantado primeiro: joia -> loja de joias); insumo vai a
            // ENTRADA com espaco, lingote antes de minerio. Insumo sem destino nao prende mais o ferreiro: ele segue recolhendo o que
            // estiver pronto e ainda couber na mao (teto por tipo), em vez de esperar na fila da entrada
            // FASE7 §3: sem a compra direta, esperar no balcao com o estoque daquele produto cheio trava (a fila enche de quem quer outro
            // produto); produto que o balcao nao aceita fica na mao e o bot vai buscar o que a fila pede
            Item deliver = s.Deliverable(p);
            if (deliver != Item.Ore) return s.CounterFor(deliver).InAt;
            for (Item i = Item.Ingot; i >= Item.Ore; i--)
            {
                Station dest = p.Has(i) ? BestInput(s, i, p.Pos) : null;
                if (dest != null) return dest.InAt;
            }

            Station pick = BestOutput(s, Kind.Crafter, p);   // produto pronto -> boca de SAIDA
            if (pick != null) return pick.OutAt;
            pick = BestOutput(s, Kind.Furnace, p);
            if (pick != null && BestInput(s, Item.Ingot, pick.Pos) != null) return pick.OutAt;
            if (s.CanPick(p, Item.Ore) && BestInput(s, Item.Ore, p.Pos) != null) return s.Deposit.OutAt;
            if (chest != null) return chest.Pos;
            if (p.Count > 0) return Sim.IsProduct(p.Item) ? s.CounterFor(p.Item).InAt : p.Item == Item.Ore ? s.FurnaceA.InAt : s.AnvilA.InAt;   // tudo cheio e nada a recolher: espera onde vai caber
            // fornalhas cheias e nada pronto: espera onde o proximo lingote vai sair
            Station soon = null;
            foreach (Station st in s.Stations)
                if (st.Unlocked && st.Kind == Kind.Furnace && st.Busy && (soon == null || st.Progress > soon.Progress)) soon = st;
            return soon != null ? soon.OutAt : s.FurnaceA.OutAt;
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
                float score = (st.InCap - st.In) * 3f + need - V2.Dist(from, st.InAt);
                if (score > bestScore) { bestScore = score; best = st; }
            }
            return best;
        }

        static Station BestOutput(Sim s, Kind kind, Carrier p)
        {
            Station best = null; float bestScore = float.MinValue;
            foreach (Station st in s.Stations)
            {
                if (!st.Unlocked || st.Kind != kind || st.Out <= 0 || !s.CanPick(p, st.OutItem)) continue;   // teto daquele tipo na mao
                if (kind == Kind.Crafter && s.Stock[(int)st.OutItem] >= s.CounterCap && st.Out < st.OutCap) continue; // balcao cheio desse produto: deixa na bancada
                float score = st.Out * 5f - V2.Dist(p.Pos, st.OutAt) + (kind == Kind.Crafter && s.Wanted(st.OutItem) ? 12f : 0f);   // a fila pede e o estoque nao tem
                if (score > bestScore) { bestScore = score; best = st; }
            }
            return best;
        }
    }
}
