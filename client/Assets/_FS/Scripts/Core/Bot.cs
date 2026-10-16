using System;

namespace FS.Core
{
    /// <summary>
    /// Perfil do bot (A-CORE-07, Bot 2.0) = dados, nao subclasse. F2P = o humano de sempre (`new Bot()`, o dos BalanceTests): nunca assiste
    /// anuncio. Slow = o "lento" do BALANCE §18.3 (1,5 s de reacao, 60% de stick). AdWatcher = o humano que assiste todo anuncio liberado
    /// (Chamar VIP e Velocidade 2x, direto no Sim, como antes). AdWatcherE (flag AdsE, E-PLAT-02) = o mesmo humano assistindo tambem o Supply
    /// Drop e o bonus da encomenda, tudo pelo nucleo (Anuncios: ID de transacao e teto diario); Trocar e Cofre 2x ficam de fora (o bot nao
    /// escolhe encomenda nem sai do jogo).
    /// B-CORE-03: so o Ideal (o "Perfect" do ECONOMIA_WAVE_B §1.4) da o golpe de mestre, e acerta todo golpe que aparece; os outros nunca.
    /// </summary>
    public struct BotProfile
    {
        public string Name; public float Reaction, Stick; public bool Ads, AdsE, Strike;

        public static readonly BotProfile Ideal = new BotProfile { Name = "Ideal", Reaction = 0f, Stick = 1f, Strike = true };
        public static readonly BotProfile F2P = new BotProfile { Name = "F2P", Reaction = 0.7f, Stick = 0.85f };
        public static readonly BotProfile Slow = new BotProfile { Name = "Slow", Reaction = 1.5f, Stick = 0.6f };
        public static readonly BotProfile AdWatcher = new BotProfile { Name = "AdWatcher", Reaction = 0.7f, Stick = 0.85f, Ads = true };
        public static readonly BotProfile AdWatcherE = new BotProfile { Name = "AdWatcherE", Reaction = 0.7f, Stick = 0.85f, Ads = true, AdsE = true };
        public static readonly BotProfile[] All = { Ideal, F2P, Slow, AdWatcher, AdWatcherE };
    }

    /// <summary>
    /// Autoplay: joga sozinho com uma heuristica simples (compra mais barata que ja da para pagar, no menu na hora ou indo ao pad > entregar o que
    /// carrega > pegar produto pronto > pegar lingote pronto > buscar minerio). Serve para balancear a §3 do GDD
    /// (BalanceTests) e para o smoke `-autoplay` do build. Nao e' IA de jogo: e' um jogador mediano previsivel.
    /// </summary>
    public sealed partial class Bot
    {
        public const float Reach = 0.35f, ChestDetour = 3f;   // m: bau mais perto que isso desvia o caminho

        /// <summary>
        /// Handicap "humano" (HIPOTESE a calibrar no playtest com o diario): quando o alvo muda, para 0,7 s para "ler a
        /// tela"; e nao segura o analogico no maximo. Reaction = 0 e Stick = 1 e' o bot ideal (limite inferior de tempo).
        /// </summary>
        public float Reaction = BotProfile.F2P.Reaction, Stick = BotProfile.F2P.Stick;

        /// <summary>Assiste anuncio (AdWatcher): chama o VIP quando pode e liga o boost 2x quando a recarga deixa; com AdsE (AdWatcherE) tambem o
        /// Supply Drop e o bonus da encomenda, pelo Anuncios. Conta os anuncios vistos.</summary>
        public bool Ads, AdsE; public int AdsVip, AdsBoost, AdsSupply, AdsBonus;
        public int AdsTotal => AdsVip + AdsBoost + AdsSupply + AdsBonus;
        /// <summary>B-CORE-03: acerta o golpe de mestre sempre que o Sim oferece (ferreiro parado na boca de uma bancada, recarga zerada).</summary>
        public bool Strike;
        /// <summary>B-CORE-04: contrata/sobe nivel de worker antes da producao completa so quando custa ate tantos s da renda online; depois dela,
        /// qualquer preco (e' ralo). 0 = so depois da producao completa: na varredura (BALANCE.md §25) foi o que mais rendeu aos 60 e aos 90 min
        /// (worker comprado antes atrasa a producao mais do que devolve). Negativo = nunca (o bot de antes do B-CORE-04).</summary>
        public float WorkerSegundos = 0f;

        V2 _target; bool _has, _menu; float _wait;

        public static Bot Ideal() => From(BotProfile.Ideal);
        public static Bot From(BotProfile p) => new Bot { Reaction = p.Reaction, Stick = p.Stick, Ads = p.Ads, AdsE = p.AdsE, Strike = p.Strike };

        /// <summary>
        /// Um tick do jogo com o bot no joystick. Menu inferior (FASE6): mesma politica do pad (a compra mais barata que ja da
        /// para pagar), so que sem andar: para, "le a tela" (Reaction, como em toda troca de alvo) e toca. Compra depois do
        /// Tick para o Ev.Bought ficar em Events como o do pad.
        /// </summary>
        public void Step(Sim s, float dt)
        {
            if (Ads && AdsE)   // E-PLAT-02: tudo pelo nucleo (oferta -> recompensa, como a View), com o teto diario
            {
                if (Assiste(s, Anuncio.Vip)) AdsVip++;
                if (s.BoostLeft <= 0f && Assiste(s, Anuncio.Velocidade)) AdsBoost++;
                if (Assiste(s, Anuncio.Supply)) AdsSupply++;
                if (Assiste(s, Anuncio.EncomendaBonus)) AdsBonus++;
            }
            else if (Ads)
            {
                if (s.SummonVip()) AdsVip++;                                 // o Sim recusa fora de CanSummonVip
                if (s.BoostLeft <= 0f && s.StartBoost() > 0f) AdsBoost++;    // ponytail: so o 1o anuncio (2x); o 2o (3x) e' mais uma condicao
            }
            if (Strike && !EsperaSuperior(s)) s.Strike(true);   // o Sim recusa fora de StrikeStation (parado na boca, recarga zerada); acerto perfeito, sem tempo de anel
            if (s.Contratos.Oferta != null) s.Contratos.Aceitar();   // D-CORE-04: aceita o contrato oferecido (ja tem o Renome) e vende como sempre
            int menu = s.CheapestAffordableMenu(), wk = Worker(s, menu);
            bool buy = menu >= 0 || wk >= 0;   // B-CORE-04: o worker se compra como o menu (para, le a tela, toca)
            V2 t = buy ? s.Player.Pos : Target(s);
            if (!_has || buy != _menu || (!buy && V2.Dist(t, _target) > 0.01f)) { _target = t; _menu = buy; _has = true; _wait = Reaction; }
            if (_wait > 0f || buy)
            {
                _wait -= dt;
                s.Tick(dt, 0f, 0f);
                if (buy && _wait <= 0f) { if (wk >= 0) s.Equipe.Comprar(wk); else s.TryBuyMenu((Upgrade)menu); }
                return;
            }
            V2 d = Toward(s, t);
            s.Tick(dt, d.X * Stick, d.Y * Stick);
        }

        /// <summary>
        /// B-CORE-04: worker que vale comprar agora, ou -1: o mais barato que o ouro paga (Equipe.MaisBarato), se custa ate WorkerSegundos da
        /// renda online (depois da producao completa, qualquer preco) e nenhuma melhoria pagavel (menu `menu` ou pad) custa o mesmo ou menos.
        /// ponytail: "vale" = barato perante a renda, sem estimar retorno; o retorno medido esta no BALANCE.md §25.
        /// </summary>
        int Worker(Sim s, int menu)
        {
            if (WorkerSegundos < 0f) return -1;
            int k = s.Equipe.MaisBarato();
            if (k < 0) return -1;
            long c = s.Equipe.Custo(k);
            if (!s.ProductionComplete && c > s.RateEma * WorkerSegundos) return -1;
            if (menu >= 0 && s.T.Cost(menu) <= c) return -1;
            Pad pad = s.CheapestAffordablePad();
            return pad != null && s.T.Cost(pad.Current(s)) - pad.Paid <= c ? -1 : k;
        }

        /// <summary>C-CORE-03: com a producao completa do distrito ativo, sobe o manager mais barato que a carteira paga (chamar depois do Step;
        /// devolve se subiu). ponytail: sem estimar retorno nem esperar o luxo mais caro (o manager pode atrasar um luxo; o efeito medido esta no
        /// BALANCE.md §34); vira prioridade por retorno se o bot precisar escolher melhor.</summary>
        public static bool Manager(Imperio imp)
        {
            if (!imp.Ativo.ProductionComplete) return false;
            string id = null; long c = long.MaxValue;
            foreach (DistrictDef d in Distritos.All)
            {
                long k = imp.CustoManager(d.Id);
                if (k >= 0 && k < c) { c = k; id = d.Id; }
            }
            return id != null && imp.SobeManager(id, imp.Manager(id) + 1);
        }

        /// <summary>B-CORE-08 (ECONOMIA_WAVE_B §1.4): o Perfect "espera a proxima Superior para tentar a Masterwork". Com Masterwork pendente na
        /// familia da bancada e Superior ao alcance da automacao (q >= 100), so golpeia a peca que ja sairia Superior; sem isso, todo golpe (o de antes).</summary>
        static bool EsperaSuperior(Sim s)
        {
            Station st = s.StrikeStation;
            return st != null && s.QualityPoints(st) >= 100 && s.Hall.Pendente(st) >= 0 && s.Hall.ProximaPeca(st) != Quality.Superior;
        }

        static bool Assiste(Sim s, Anuncio a) => s.Anuncios.Pagar(s.Anuncios.Oferta(a)) >= 0;

        /// <summary>Direcao do joystick (comprimento 0..1) para o alvo atual, sem handicap; zero quando chegou.</summary>
        public static V2 Decide(Sim s) => Toward(s, Target(s));

        static V2 Toward(Sim s, V2 target)
        {
            V2 d = target - s.Player.Pos;
            float len = d.Len;
            return len <= Reach ? new V2(0f, 0f) : s.Steer(s.Player, s.Rota(s.Player.Pos, target));   // porta/arco da parede e contorno das estacoes
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
            // C-CORE-01: todo insumo na mao, o mais adiantado (maior handle) primeiro; no Old Forge sao so lingote e minerio, nessa ordem
            for (int k = p.Held.Length - 1; k >= 0; k--)
            {
                Station dest = !s.IsProduct((Item)k) && p.Held[k] > 0 ? BestInput(s, (Item)k, p.Pos) : null;
                if (dest != null) return dest.InAt;
            }

            Station pick = BestOutput(s, Kind.Crafter, p);   // produto pronto -> boca de SAIDA
            if (pick != null) return pick.OutAt;
            pick = BestOutput(s, Kind.Furnace, p);   // insumo pronto que tem para onde ir (lingote; na Row tambem placa e rebite)
            if (pick != null) return pick.OutAt;
            Station dep = BestDeposit(s, p);
            if (dep != null) return dep.OutAt;
            if (chest != null) return chest.Pos;
            if (p.Count > 0) return s.IsProduct(p.Item) ? s.CounterFor(p.Item).InAt : Esperar(s, p.Item).InAt;   // tudo cheio e nada a recolher: espera onde vai caber
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
                int k = st.InIndex(item);   // B-CORE-01: a receita usa `item` e a entrada dele cabe
                if (!st.Unlocked || k < 0 || st.Ins[k] >= st.InCap) continue;
                float need = 0f;
                if (st.Kind == Kind.Crafter)
                {
                    var q = s.QueueFor(st.OutItem);
                    need = (s.CounterCap - s.Stock[(int)st.OutItem]) * 2f;
                    if (q.Count > 0 && q[0].Want == st.OutItem && s.Stock[(int)st.OutItem] == 0) need += 12f;
                }
                float score = (st.InCap - st.Ins[k]) * 3f + need - V2.Dist(from, st.InAt);
                if (score > bestScore) { bestScore = score; best = st; }
            }
            return best;
        }

        /// <summary>C-CORE-01: deposito aberto cujo bruto cabe na mao e tem estacao com espaco; o bruto que o ferreiro tem menos na mao primeiro,
        /// empate pela ordem das estacoes. Um deposito so (Old Forge): o minerio, como antes.</summary>
        static Station BestDeposit(Sim s, Carrier p)
        {
            Station best = null;
            foreach (Station st in s.Stations)
                if (st.Kind == Kind.Deposit && st.Unlocked && s.CanPick(p, st.OutItem) && BestInput(s, st.OutItem, p.Pos) != null
                    && (best == null || p.Held[(int)st.OutItem] < p.Held[(int)best.OutItem])) best = st;
            return best;
        }

        /// <summary>Onde esperar com o insumo `i` na mao e tudo cheio: a 1a estacao que o usa (Old Forge: minerio na Fornalha, lingote na Bigorna).</summary>
        static Station Esperar(Sim s, Item i) => s.Stations.Find(st => st.InIndex(i) >= 0) ?? s.FurnaceA;

        static Station BestOutput(Sim s, Kind kind, Carrier p)
        {
            Station best = null; float bestScore = float.MinValue;
            foreach (Station st in s.Stations)
            {
                if (!st.Unlocked || st.Kind != kind || st.Out <= 0 || !s.CanPick(p, st.OutItem)) continue;   // teto daquele tipo na mao
                if (kind == Kind.Crafter && s.Stock[(int)st.OutItem] >= s.CounterCap && st.Out < st.OutCap) continue; // balcao cheio desse produto: deixa na bancada
                if (!s.IsProduct(st.OutItem) && BestInput(s, st.OutItem, st.Pos) == null) continue;   // insumo sem entrada com espaco: fica (C-CORE-01)
                float score = st.Out * 5f - V2.Dist(p.Pos, st.OutAt) + (kind == Kind.Crafter && s.Wanted(st.OutItem) ? 12f : 0f);   // a fila pede e o estoque nao tem
                if (score > bestScore) { bestScore = score; best = st; }
            }
            return best;
        }
    }
}
