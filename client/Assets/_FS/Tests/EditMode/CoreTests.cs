using System;
using System.Collections.Generic;
using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>Regras do nucleo. Cada teste foi provado VERMELHO uma vez reintroduzindo o defeito (ver README, "Prova de vermelho").</summary>
    public class CoreTests
    {
        const float Dt = 1f / 30f;

        static void Run(Sim s, float seconds, float x = 0f, float y = 0f)
        {
            for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt, x, y);
        }

        /// <summary>Teleporta o jogador para a estacao e fica parado la por `seconds`.</summary>
        static void StandAt(Sim s, V2 pos, float seconds)
        {
            s.Player.Pos = pos;
            Run(s, seconds);
        }

        static int Count(Sim s, Ev kind)
        {
            int n = 0;
            foreach (SimEvent e in s.Events) if (e.Kind == kind) n++;
            return n;
        }

        /// <summary>Poe na mao do jogador SO `n` de `item` (FASE7: a pilha e' por tipo).</summary>
        static void Hold(Sim s, Item item, int n)
        {
            Array.Clear(s.Player.Held, 0, s.Player.Held.Length);
            s.Player.Held[(int)item] = n;
        }

        static Sim Rich(int gold = 100000)
        {
            var s = new Sim();
            s.Gold = gold;
            return s;
        }

        [Test]
        public void Cadeia_MinerioLingoteEspada_VendePagaOuro()
        {
            var s = new Sim();
            StandAt(s, s.Deposit.InAt, 2f);
            Assert.AreEqual((Item.Ore, 3), (s.Player.Item, s.Player.Count), "3 minerios do deposito");
            StandAt(s, s.FurnaceA.InAt, 1f);
            Assert.AreEqual(0, s.Player.Count, "depositou os 3 na entrada da fornalha");
            StandAt(s, s.FurnaceA.OutAt, 14f);            // 3 lingotes a 4 s + pegar na saida
            Assert.AreEqual((Item.Ingot, 3), (s.Player.Item, s.Player.Count), "pegou os 3 lingotes sozinho");
            StandAt(s, s.AnvilA.InAt, 1f);
            StandAt(s, s.AnvilA.OutAt, 3f * Balance.HammerTime0 + 2f);   // 3 espadas
            Assert.AreEqual((Item.Sword, 3), (s.Player.Item, s.Player.Count), "pegou 3 espadas");
            int gold = s.Gold;
            StandAt(s, s.Counter.InAt, 3f);               // 3 clientes de espada ja esperam (8 s, 14 s, 20 s)
            Assert.AreEqual(gold + 3 * Balance.Price[(int)Item.Sword], s.Gold, "cada cliente pagou uma espada");
            Assert.AreEqual(3, s.Sales);
            Assert.Greater(s.FirstSaleTime, 0f);
            Assert.Less(s.FirstSaleTime, 90f, "GDD §15: primeira venda em <90 s");
            Assert.AreEqual((3, 3), (s.Crafted[(int)Item.Ingot], s.Crafted[(int)Item.Sword]), "metrica product_crafted");
        }

        /// <summary>FASE7 §1: o teto (3; 6 com a Mochila) vale POR TIPO e o ferreiro mistura tipos; o ajudante continua com um tipo por vez.</summary>
        [Test]
        public void Capacidade_TetoPorTipo_JogadorMistura_AjudanteNao()
        {
            var s = new Sim();
            StandAt(s, s.Deposit.InAt, 10f);
            Assert.AreEqual(Balance.PlayerCap, s.Player.Count);
            s.Buy(Upgrade.PlayerCapacity);
            Run(s, 10f);
            Assert.AreEqual(Balance.PlayerCapUp, s.Player.Count, "mochila sobe o teto");
            // minerio no teto e fornalha cheia: recolhe os lingotes tambem (cada tipo tem o proprio teto)
            s.FurnaceA.In = Balance.FurnaceIn; s.FurnaceA.Out = Balance.FurnaceOut; s.FurnaceA.Busy = false;
            StandAt(s, s.FurnaceA.OutAt, 2f);
            Assert.AreEqual((Balance.PlayerCapUp, Balance.FurnaceOut, 0), (s.Player.Held[(int)Item.Ore], s.Player.Held[(int)Item.Ingot], s.FurnaceA.Out), "6 minerios + 4 lingotes");
            Assert.AreEqual(Item.Ingot, s.Player.Item, "Item = o mais adiantado na mao");
            s.FurnaceA.Out = Balance.FurnaceOut;
            StandAt(s, s.FurnaceA.OutAt, 2f);
            Assert.AreEqual((Balance.PlayerCapUp, 2), (s.Player.Held[(int)Item.Ingot], s.FurnaceA.Out), "teto de lingote: 6, sobram 2 na saida");
            Assert.AreEqual(2 * Balance.PlayerCapUp, s.Player.Count);
            // ajudante: pilha homogenea (so o jogador mistura)
            s.Buy(Upgrade.Helper1); s.Buy(Upgrade.Helper2); s.Buy(Upgrade.Helper3); s.Buy(Upgrade.Shields);
            Carrier w = s.Workers.Find(c => c.Role == 2);
            w.Held[(int)Item.Sword] = 1;
            Assert.IsFalse(s.CanPick(w, Item.Shield), "ajudante de produto com espada nao pega escudo");
            Assert.IsTrue(s.CanPick(w, Item.Sword));
            Hold(s, Item.Sword, Balance.PlayerCapUp);
            Assert.IsTrue(s.CanPick(s.Player, Item.Shield) && !s.CanPick(s.Player, Item.Sword), "jogador: escudo cabe, espada no teto nao");
        }

        /// <summary>
        /// Revisao v0.5 #1: sem a compra direta o estoque so desce com cliente na vaga. O Ajudante 3 pegava espada com a prateleira de
        /// espada cheia e parava no balcao (68% do tempo no bot de 45 min) enquanto escudo e ferramenta encalhavam. So busca o que cabe.
        /// </summary>
        [Test]
        public void Ajudante3_NaoBuscaProdutoComPrateleiraCheia()
        {
            var s = Rich();
            Assert.IsTrue(s.Buy(Upgrade.Helper1) && s.Buy(Upgrade.Helper2) && s.Buy(Upgrade.Helper3) && s.Buy(Upgrade.Shields));
            Carrier w = s.Workers.Find(c => c.Role == 2);
            s.AnvilA.Out = 3; s.ShieldBench.Out = 2;
            s.Player.Pos = new V2(1f, 1f);
            for (int i = 0; i < 40; i++) { s.Stock[(int)Item.Sword] = s.CounterCap; Run(s, 0.1f); }   // prateleira de espada sempre cheia
            Assert.AreEqual(0, w.Held[(int)Item.Sword], "espada com a prateleira cheia fica na bigorna");
            Assert.IsTrue(w.Held[(int)Item.Shield] > 0 || w.Target == s.ShieldBench.Index, "vai buscar o escudo, que cabe");
        }

        /// <summary>
        /// Revisao v0.5 #2: o save grava os ajudantes na ordem da compra e o Recompute recria na ordem do HireBy. Joalheiro antes do
        /// Ajudante 3 desalinhava as listas e a carga sumia ao reabrir. Agora cada entrada leva o papel.
        /// </summary>
        [Test]
        public void Save_CargaDosAjudantes_ContratadosForaDeOrdem()
        {
            var s = Rich();
            foreach (Upgrade u in new[] { Upgrade.Helper1, Upgrade.Helper2, Upgrade.SideCorridor, Upgrade.Jewelry, Upgrade.Jeweler, Upgrade.Helper3 })
                Assert.IsTrue(s.Buy(u), u.ToString());
            s.Workers.Find(c => c.Role == 2).Held[(int)Item.Sword] = 2;
            s.Workers.Find(c => c.Role == 3).Held[(int)Item.Ingot] = 1;
            Sim b = Sim.Load(s.Save(1000));
            Assert.AreEqual(2, b.Workers.Find(c => c.Role == 2).Held[(int)Item.Sword], "Ajudante 3 volta com as espadas");
            Assert.AreEqual(1, b.Workers.Find(c => c.Role == 3).Held[(int)Item.Ingot], "Joalheiro volta com o lingote");
        }

        /// <summary>
        /// FASE7 §1, a trava do Vinicius (POCO F4, v0.4.1): lingotes no teto, entrada da bigorna cheia e saida com espadas. Com a pilha
        /// homogenea ele nao pegava as espadas, a bigorna nao produzia e nada destravava. Agora recolhe as espadas por cima dos lingotes
        /// e vende no balcao. So usa API que ja existia (bocas, Tick, Has, Count): prova vermelha no core antigo, sem adaptar.
        /// </summary>
        [Test]
        public void Carga_LingotesNoTeto_BigornaCheia_PegaEspadasEVende()
        {
            var s = new Sim();
            s.FurnaceA.Out = Balance.PlayerCap;
            StandAt(s, s.FurnaceA.OutAt, 1f);
            Assert.AreEqual((Item.Ingot, Balance.PlayerCap), (s.Player.Item, s.Player.Count), "lingotes no teto");
            s.AnvilA.In = s.AnvilA.InCap; s.AnvilA.Out = s.AnvilA.OutCap;   // entrada cheia e saida cheia: a bigorna parou
            StandAt(s, s.AnvilA.InAt, 1f);
            Assert.AreEqual(Balance.PlayerCap, s.Player.Count, "entrada cheia: nenhum lingote entra");
            StandAt(s, s.AnvilA.OutAt, 1f);
            Assert.IsTrue(s.Player.Has(Item.Sword), "recolhe as espadas com os lingotes na mao (antes travava aqui)");
            Assert.AreEqual(0, s.AnvilA.Out, "as 3 espadas sairam da bigorna");
            StandAt(s, s.Counter.InAt, Balance.FirstClientAt + 1f);
            Assert.IsFalse(s.Player.Has(Item.Sword), "as espadas foram para o balcao");
            Assert.AreEqual(Balance.PlayerCap, s.Player.Count, "os lingotes continuam na mao");
            Assert.Greater(s.Sales, 0, "o cliente de espada comprou");
            Assert.AreEqual(s.Sales * Balance.Price[(int)Item.Sword], s.Gold);
        }

        [Test]
        public void Gargalo_FornalhaLenta_AcumulaFila_UpgradeResolve()
        {
            var baseSim = new Sim();
            baseSim.FurnaceA.In = Balance.FurnaceIn;
            Run(baseSim, 12f);
            int filaBase = baseSim.FurnaceA.In;
            Assert.Greater(filaBase, 2, "fornalha a 4 s deixa minerio esperando");
            Assert.Greater(baseSim.AnvilA.StarveSeconds, 5f, "a bigorna passa fome enquanto a fornalha e' o gargalo");

            var fast = new Sim();
            fast.Buy(Upgrade.FurnaceSpeed1);
            fast.FurnaceA.In = Balance.FurnaceIn;
            Run(fast, 12f);
            Assert.Less(fast.FurnaceA.In, filaBase, "o fole encurta a fila da fornalha");

            // saida cheia trava a estacao e emite gargalo uma vez
            var blocked = new Sim();
            blocked.FurnaceA.In = Balance.FurnaceIn;
            int bottlenecks = 0;
            for (float t = 0f; t < 40f; t += Dt) { blocked.Tick(Dt, 0f, 0f); bottlenecks += Count(blocked, Ev.Bottleneck); }
            Assert.AreEqual(Balance.FurnaceOut, blocked.FurnaceA.Out, "pilha de saida tem teto");
            Assert.AreEqual(1, bottlenecks, "um evento de gargalo ao travar");
            Assert.Greater(blocked.FurnaceA.StallSeconds, 10f);
        }

        [Test]
        public void Worker_CompletaOCicloSozinho()
        {
            var s = new Sim();
            s.Buy(Upgrade.Helper1);
            Assert.AreEqual(1, s.Workers.Count);
            Run(s, 60f);
            Assert.Greater(s.Crafted[(int)Item.Ingot], 3, "ajudante 1 levou minerio e a fornalha fundiu sem o jogador");

            s.Buy(Upgrade.Helper2);
            s.Buy(Upgrade.Helper3);
            Run(s, 120f);
            Assert.Greater(s.Crafted[(int)Item.Sword], 3, "ajudante 2 levou lingotes a bigorna");
            Assert.Greater(s.Sales, 0, "ajudante 3 abasteceu o balcao e o cliente pagou, jogador parado");
            Assert.Greater(s.Gold, 0);
        }

        [Test]
        public void Cliente_PagaEVaiEmbora_OuCansaESai()
        {
            var s = new Sim();
            Run(s, Balance.FirstClientAt + 0.1f);
            Assert.AreEqual(1, s.Queue.Count, "1o cliente chegou");
            s.Stock[(int)Item.Sword] = 1;
            Run(s, 0.5f);
            Assert.AreEqual(0, s.Queue.Count, "atendido saiu da fila");
            Assert.AreEqual(Balance.Price[(int)Item.Sword], s.Gold);

            var bored = new Sim();
            Run(bored, Balance.FirstClientAt + 0.1f);
            Assert.AreEqual(1, bored.Queue.Count);
            Run(bored, Balance.PatienceFor(Item.Sword) + 0.5f);
            Assert.AreEqual(1, bored.ClientsLost, "sem estoque, cansou e foi embora (friccao, nao derrota)");
            Assert.AreEqual(0, bored.Gold);
        }

        [Test]
        public void Fila_Cheia_ClienteNaoEntra_EEscudoSoDepoisDoUpgrade()
        {
            var s = new Sim();
            Run(s, 60f);
            Assert.AreEqual(Balance.QueueCap0, s.Queue.Count, "fila tem teto");
            Assert.Greater(s.ClientsTurnedAway, 0);
            foreach (Client c in s.Queue) Assert.AreEqual(Item.Sword, c.Want, "sem escudos desbloqueados ninguem pede escudo");
            s.Buy(Upgrade.Shields);
            s.Buy(Upgrade.CounterCapacity); s.Buy(Upgrade.Counter5); s.Buy(Upgrade.Counter6);   // FASE7: a fila cresce no balcao, nao na Vitrine
            Run(s, 30f);
            bool shieldAsked = false;
            foreach (Client c in s.Queue) shieldAsked |= c.Want == Item.Shield;
            Assert.IsTrue(shieldAsked, "linha de escudos traz cliente de escudo");
            Assert.AreEqual(6, s.QueueCap, "Balcao 5 e 6 vagas");
            Assert.LessOrEqual(s.Queue.Count, s.QueueCap);
        }

        [Test]
        public void Escudo_Consome2Lingotes_EPaga25()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields);
            s.ShieldBench.In = 3;
            Run(s, Balance.HammerTime0 * Balance.ShieldTimeMul + 0.2f);
            Assert.AreEqual((1, 1), (s.ShieldBench.In, s.ShieldBench.Out), "2 lingotes viraram 1 escudo, sobrou 1");
            Run(s, 10f);
            Assert.AreEqual(1, s.ShieldBench.Out, "1 lingote nao basta para outro escudo");
            Assert.AreEqual(25, Balance.Price[(int)Item.Shield]);
        }

        [Test]
        public void Pad_DrenaOuro_ECompra_AplicaEfeito()
        {
            var s = new Sim();
            s.Gold = 20;
            Pad pad = s.Pads[5];   // cadeia da contratacao (Fole e Mochila foram para o menu, FASE6)
            Assert.AreEqual((int)Upgrade.Helper1, pad.Current(s));
            int cost = Upgrades.Cost(Upgrade.Helper1);
            StandAt(s, pad.Pos, 2f);
            Assert.AreEqual((0, 20), (s.Gold, pad.Paid), "despeja o que tem e guarda o parcial");
            Assert.IsFalse(s.Bought[(int)Upgrade.Helper1]);
            s.Gold = cost;
            Run(s, 2f);
            Assert.IsTrue(s.Bought[(int)Upgrade.Helper1], "completou o preco");
            Assert.AreEqual(20, s.Gold, "so cobrou o que faltava; o proximo nivel da cadeia NAO engole o troco");
            Assert.AreEqual(0, pad.Paid, "pad zera depois da compra");
            // sai de cima e volta: o pad rearma e passa a cobrar o Ajudante 2
            StandAt(s, new V2(4.5f, 3.5f), 0.5f);
            StandAt(s, pad.Pos, 1f);
            Assert.AreEqual((0, 20), (s.Gold, pad.Paid), "rearmou");
            // atravessar andando nao gasta (dwell): entra por um lado do pad e sai pelo outro sem parar
            var walk = new Sim(); walk.Gold = 50;
            Pad p = PadFor(walk, Upgrade.Anvil2);                    // pad da 2a bigorna (4,5; 9,5), estacao ainda fechada: da para cruzar na horizontal
            walk.Player.Pos = new V2(p.Pos.X - 1.0f, p.Pos.Y);
            Run(walk, 0.7f, 1f, 0f);                                 // 2,1 m a 3 m/s: cruza o diametro inteiro
            Assert.Greater(walk.Player.Pos.X, p.Pos.X + Balance.PadRadius, "saiu do outro lado");
            Assert.AreEqual(50, walk.Gold, "passar por cima sem parar nao paga");
            // parar em cima paga na hora
            StandAt(walk, p.Pos, 0.2f);
            Assert.Less(walk.Gold, 50, "parado paga");
            Assert.AreEqual(1, s.Workers.Count, "efeito aplicado");
            Assert.AreEqual((int)Upgrade.Helper2, pad.Current(s), "a cadeia avanca para o proximo nivel");
            Assert.IsFalse(s.Buy(Upgrade.Helper1), "comprar de novo e' no-op");
        }

        [Test]
        public void Curva_DeCusto_Dos25Upgrades_22ProdutivosELuxo()
        {
            Assert.AreEqual(26, Upgrades.ProductionCount);
            Assert.AreEqual(29, Upgrades.Count);
            Assert.AreEqual(29, Upgrades.All.Length);
            Assert.AreEqual(29, Enum.GetValues(typeof(Upgrade)).Length);
            Assert.AreEqual((3000, 2400), (Upgrades.Cost(Upgrade.Miner), Upgrades.Cost(Upgrade.Jeweler2)), "fase 4: medido na Leva 10 (BALANCE.md §14)");
            Assert.AreEqual((14000, 18000, 22000), (Upgrades.Cost(Upgrade.WorkshopFacade), Upgrades.Cost(Upgrade.WorkshopFloor), Upgrades.Cost(Upgrade.JewelryDecor)), "luxo: Leva 11 (BALANCE.md §15)");
            Assert.AreEqual((690, 1515), (Upgrades.Cost(Upgrade.SideCorridor), Upgrades.Cost(Upgrade.Jewelry)), "2a area fora da formula (coordenador 2026-10-07)");
            Assert.AreEqual((2200, 5500, 9000), (Upgrades.Cost(Upgrade.Jeweler), Upgrades.Cost(Upgrade.JewelSpeed), Upgrades.Cost(Upgrade.JewelVitrine)), "fase 2: custos aprovados (AREA2_FASE2 §6)");
            foreach (Upgrade u in new[] { Upgrade.Jeweler, Upgrade.JewelSpeed, Upgrade.JewelVitrine })
                Assert.AreEqual((int)Upgrade.Jewelry, Upgrades.All[(int)u].Requires, $"{u} requer a Joalheria");
            var ids = new HashSet<Upgrade>();
            for (int i = 0; i < Upgrades.Count; i++)
            {
                Assert.AreEqual((Upgrade)i, Upgrades.All[i].Id, "tabela na ordem do enum (tier)");
                Assert.IsTrue(ids.Add(Upgrades.All[i].Id));
                int req = Upgrades.All[i].Requires;
                if (req >= 0) Assert.Less(req, i, "pre-requisito e' sempre de tier menor");
            }
            Assert.AreEqual(Balance.CostBase, Upgrades.Cost(0));
            for (int i = 1; i < Upgrades.Count; i++)
            {
                Assert.AreEqual(0, Upgrades.Cost(i) % 5, "preco legivel");
                if (i >= (int)Upgrade.SideCorridor) continue;   // so os 15 do v0.1 seguem a formula
                double ratio = Upgrades.Cost(i) / (double)Upgrades.Cost(i - 1);
                Assert.That(ratio, Is.InRange(1.15, 1.5), $"tier {i}: {Upgrades.Cost(i - 1)} -> {Upgrades.Cost(i)}");
            }
            Assert.Greater(Upgrades.Cost(Upgrade.Jewelry), Upgrades.Cost(Upgrade.SideCorridor), "a Joalheria custa mais que o Corredor que ela exige");
            // todo upgrade acaba a venda (pad visivel, ou o menu se for InMenu): pre-requisitos alcancaveis em ordem de tier e depois os luxos
            var s = new Sim();
            var order = new List<int>();
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < Upgrades.Count; i++) if (Upgrades.IsLuxury(i) == (pass == 1)) order.Add(i);
            foreach (int i in order)
            {
                bool visible = false;
                foreach (Pad p in s.Pads) visible |= p.Current(s) == i;
                Assert.AreEqual(!Upgrades.All[i].InMenu, visible, $"{(Upgrade)i}: pad visivel quando chega a vez dele, so se nao for do menu");
                Assert.AreEqual(Upgrades.All[i].InMenu, s.MenuAvailable(i), $"{(Upgrade)i}: a venda no menu so se for InMenu");
                s.Buy((Upgrade)i);
            }
            foreach (Pad p in s.Pads) Assert.AreEqual(-1, p.Current(s), "com tudo comprado, nenhum pad sobra");
        }

        [Test]
        public void Offline_Teto2h_25Porcento_NegativoZero_Idempotente()
        {
            var s = new Sim();
            for (int i = 0; i < Upgrades.Count; i++) s.Buy((Upgrade)i);   // tudo comprado: o teto relativo (2x o ultimo = 18000) nao interfere aqui
            s.RateEma = 1.0;   // 1 ouro/s online
            Assert.AreEqual(900, s.ApplyOffline(3600, 100), "25% da taxa");
            Assert.AreEqual(0, s.ApplyOffline(3600, 100), "mesmo id de claim nao paga de novo");
            Assert.AreEqual(0, s.ApplyOffline(3600, 90), "id mais antigo (save restaurado) nao paga");
            Assert.AreEqual(0, s.ApplyOffline(-5, 200), "relogio voltou: zero");
            Assert.AreEqual(1800, s.ApplyOffline(10 * 3600, 200), "teto de 2 h");
            Assert.AreEqual(2700, s.Gold);
            Assert.AreEqual(2700, s.OfflineEarned);
            Assert.AreEqual(0, s.GoldEarned, "offline nao infla a taxa online");
        }

        [Test]
        public void Offline_NuncaPagaMaisQue2xOUpgradeMaisBaratoTravado()
        {
            var s = new Sim();
            s.RateEma = 1000.0;   // taxa absurda: so o teto relativo segura
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.FurnaceSpeed1), s.ApplyOffline(7200, 1), "nada comprado: 2x o fole");
            s.Buy(Upgrade.FurnaceSpeed1);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.Anvil2), s.ApplyOffline(7200, 2), "fole comprado: 2x a 2a bigorna");
            s.Buy(Upgrade.Anvil2); s.Buy(Upgrade.Helper1); s.Buy(Upgrade.Shields);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.PlayerCapacity), s.ApplyOffline(7200, 3), "o mais barato ainda travado");
            for (int i = 0; i <= (int)Upgrade.CounterCapacity; i++) s.Buy((Upgrade)i);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.Counter5), s.ApplyOffline(7200, 4), "FASE7: com a Vitrine, o Balcao 5 (150) e' o menor travado");
            BuyCounterTiers(s);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.SideCorridor), s.ApplyOffline(7200, 5), "o MENOR travado (Corredor 690), nao o proximo tier (2a fornalha 895)");
            for (int i = 0; i <= (int)Upgrade.Jewelry; i++) s.Buy((Upgrade)i);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.Jeweler), s.ApplyOffline(7200, 6), "fase 2: o menor travado e' o Joalheiro (2200)");
            for (int i = 0; i < Upgrades.Count; i++) s.Buy((Upgrade)i);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.JewelVitrine), s.ApplyOffline(7200, 7), "tudo comprado: 2x o ultimo (Vitrine de joias 9000 = 18000)");
            var low = new Sim(); low.RateEma = 0.01;   // taxa baixa: vale a taxa, nao o teto
            Assert.AreEqual((int)Math.Floor(0.01 * Balance.OfflineFactor * Balance.OfflineCapSeconds), low.ApplyOffline(7200, 1));
        }

        [Test]
        public void Save_IdaEVolta_ELixoViraInicial()
        {
            var s = new Sim();
            s.Buy(Upgrade.FurnaceSpeed1); s.Buy(Upgrade.Helper1);
            s.Gold = 123; s.Stock[(int)Item.Sword] = 2; s.FurnaceA.In = 4; s.FurnaceA.Out = 1; s.FurnaceA.Busy = true; s.FurnaceA.Progress = 0.5f;
            s.Pads[2].Paid = 7; s.RateEma = 0.75; s.Sales = 9; s.FirstSaleTime = 33.5f; s.LastClaim = 50;
            Run(s, 1f);
            string text = s.Save(1000);
            Sim b = Sim.Load(text);
            Assert.AreEqual(s.Gold, b.Gold);
            Assert.AreEqual(s.Time, b.Time, 0.01f);
            Assert.IsTrue(b.Bought[(int)Upgrade.FurnaceSpeed1] && b.Bought[(int)Upgrade.Helper1]);
            Assert.AreEqual(Balance.FurnaceTime1, b.FurnaceA.Time, "efeitos re-derivados dos flags");
            Assert.AreEqual(1, b.Workers.Count, "ajudante volta");
            Assert.AreEqual((s.FurnaceA.In, s.FurnaceA.Out, true), (b.FurnaceA.In, b.FurnaceA.Out, b.FurnaceA.Busy));
            Assert.AreEqual(s.FurnaceA.Progress, b.FurnaceA.Progress, 0.01f);
            Assert.AreEqual(2, b.Stock[(int)Item.Sword]);
            Assert.AreEqual(7, b.Pads[2].Paid);
            Assert.AreEqual(s.RateEma, b.RateEma, 1e-4);
            Assert.AreEqual((1000L, 50L), (b.SavedAt, b.LastClaim));
            Assert.AreEqual((9, 33.5f, 2), (b.Sales, b.FirstSaleTime, b.UpgradesBought));
            Assert.AreEqual(text, b.Save(1000), "save estavel");

            foreach (string junk in new[] { null, "", "lixo", "gold=abc\nup=xyz\nst1=a,b,c,d\n=\n===", "gold=-50\nstock=99,99,99\nema=NaN\nst1=99,99,1,7" })
            {
                Sim g = Sim.Load(junk);
                Assert.GreaterOrEqual(g.Gold, 0, junk);
                Assert.LessOrEqual(g.Stock[2], Balance.CounterCap0);
                Assert.LessOrEqual(g.FurnaceA.In, Balance.FurnaceIn);
                Assert.IsFalse(double.IsNaN(g.RateEma));
                Assert.AreEqual(0, g.UpgradesBought);
            }
        }

        [Test]
        public void Esteira_MoveLingotesSemCaminhar()
        {
            var s = new Sim();
            s.Buy(Upgrade.Conveyor);
            s.FurnaceA.Out = 3;
            Run(s, 3.5f);
            Assert.AreEqual(0, s.FurnaceA.Out);
            Assert.Greater(s.AnvilA.In + (s.AnvilA.Busy ? 1 : 0) + s.AnvilA.Out, 2, "os 3 lingotes chegaram a bigorna sem ninguem andar");
            var without = new Sim();
            without.FurnaceA.Out = 3;
            Run(without, 3.5f);
            Assert.AreEqual(3, without.FurnaceA.Out, "sem esteira o lingote fica na fornalha");
        }

        [Test]
        public void Metrica_AndarSemDecisao_SoContaSemPadPagavel()
        {
            var s = new Sim();
            Run(s, 2f, 1f, 0f);
            Assert.AreEqual(2f, s.WalkNoDecision, 0.1f, "pobre, de maos vazias e andando: puro deslocamento");
            s.Gold = 100000;
            Run(s, 2f, -1f, 0f);
            Assert.AreEqual(2f, s.WalkNoDecision, 0.1f, "com pad pagavel, andar e' decisao (ir comprar)");
            s.Gold = 0; Hold(s, Item.Ore, 2);
            Run(s, 2f, 0f, 1f);
            Assert.AreEqual(2f, s.WalkNoDecision, 0.1f, "carregando e' trabalho, nao indecisao");
            Hold(s, Item.Ore, 0);
            Run(s, 2f, 0f, 0f);
            Assert.AreEqual(2f, s.WalkNoDecision, 0.1f, "parado nao conta");
        }

        [Test]
        public void Dica_SegueOEstado()
        {
            var s = new Sim();
            Assert.AreEqual(Hint.GrabOre, s.CurrentHint());
            Hold(s, Item.Ore, 2);
            Assert.AreEqual(Hint.OreToFurnace, s.CurrentHint());
            Hold(s, Item.Ore, 0); s.FurnaceA.Out = 1;
            Assert.AreEqual(Hint.PickIngots, s.CurrentHint());
            // FASE7: lingote na mao e espada pronta = pegar a espada (destrava a bigorna); com espada na mao = levar ao balcao
            Hold(s, Item.Ingot, Balance.PlayerCap); s.AnvilA.Out = 1;
            Assert.AreEqual((Hint.PickProducts, (int)Item.Sword), (s.CurrentHint(), s.HintArg));
            s.Player.Held[(int)Item.Sword] = 1;
            Assert.AreEqual((Hint.ProductToCounter, (int)Item.Sword), (s.CurrentHint(), s.HintArg), "produto na mao antes do insumo");
            s.Player.Held[(int)Item.Sword] = 0; s.AnvilA.Out = 0;
            Assert.AreEqual(Hint.IngotToCrafter, s.CurrentHint());
            s.FurnaceA.Out = 0;
            s.Gold = 100000;
            Assert.AreEqual(Hint.BuyMenu, s.CurrentHint());
            Assert.AreEqual((int)Upgrade.FurnaceSpeed1, s.HintArg, "aponta a compra mais barata (fole, no menu)");
        }

        /// <summary>
        /// FASE7 §3 (relato do Vinicius na v0.4.1: "varias vendas no balcao mesmo sem ter ninguem pedindo"): toda venda sai de um
        /// cliente que estava numa vaga. A compra direta da vitrine (fila cheia + produto no estoque = "+10" numa vaga vazia, 15-19%
        /// das vendas do balcao em 10 min e 28-30% em 45 min) saiu. Invariante tick a tick: vendas = clientes que sairam da fila sem
        /// cansar. So usa API que ja existia: prova vermelha no core antigo, sem adaptar.
        /// </summary>
        [Test]
        public void Venda_SempreDeClienteNaVaga_SemCompraDireta()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields);
            for (int i = 0; i < Balance.QueueCap0; i++) s.Queue.Add(new Client { Want = Item.Shield, Patience = 999f, MaxPatience = 999f });
            s.Stock[(int)Item.Sword] = s.CounterCap;
            int served = 0, sold = 0;
            for (float t = 0f; t < 30f; t += Dt)
            {
                int before = s.Queue.Count;
                s.Tick(Dt, 0f, 0f);
                int tired = 0;
                foreach (SimEvent e in s.Events) if (e.Kind == Ev.ClientLeft && e.B == 0) tired++;
                served += before + Count(s, Ev.ClientArrived) - tired - s.Queue.Count;
                sold += Count(s, Ev.Sold);
            }
            Assert.Greater(s.ClientsTurnedAway, 0, "clientes de espada chegaram com a fila cheia e foram embora");
            Assert.AreEqual(served, sold, "toda venda tem um cliente que saiu da vaga");
            Assert.AreEqual(0, s.Sales, "fila cheia de quem quer escudo: a espada do estoque nao se vende sozinha");
        }

        /// <summary>
        /// FASE7 §3: o cenario da trava de BALANCE §9 (fila cheia de quem quer escudo, estoque cheio de espada, ferreiro com espada na
        /// mao) sem a compra direta. Destrava por tres caminhos: a paciencia (os de escudo cansam e o de espada entra), uma vaga a mais
        /// (o de espada entra na 5a vaga) e a carga mista (o ferreiro recolhe os escudos por cima das espadas e atende a fila).
        /// </summary>
        [Test]
        public void FilaCheiaDeEscudo_EspadaNoEstoque_DestravaPelaPacienciaPelasVagasEPelaCargaMista()
        {
            Sim Trap(float patience)
            {
                var t = new Sim();
                t.Buy(Upgrade.Shields);
                for (int i = 0; i < Balance.QueueCap0; i++) t.Queue.Add(new Client { Want = Item.Shield, Patience = patience, MaxPatience = patience });
                t.Stock[(int)Item.Sword] = t.CounterCap;
                return t;
            }
            var a = Trap(Balance.PatienceFor(Item.Shield));
            Run(a, Balance.PatienceFor(Item.Shield) + 2f * Balance.ClientInterval[(int)Item.Sword] + 1f);
            Assert.Greater(a.SoldItems[(int)Item.Sword], 0, "paciencia: os de escudo cansam, a vaga abre e a espada vende");
            var b = Trap(999f);
            b.Buy(Upgrade.CounterCapacity); b.Buy(Upgrade.Counter5);
            Run(b, Balance.FirstClientAt + 0.5f);
            Assert.AreEqual(1, b.SoldItems[(int)Item.Sword], "vaga a mais: o 1o cliente de espada entra e compra sem ninguem cansar");
            var c = Trap(999f);
            Hold(c, Item.Sword, Balance.PlayerCap);
            c.ShieldBench.Out = 2;
            Assert.AreEqual((Hint.PickProducts, (int)Item.Shield), (c.CurrentHint(), c.HintArg), "dica: o escudo que a fila pede, nao 'leve espadas ao balcao'");
            StandAt(c, c.ShieldBench.OutAt, 1f);
            Assert.AreEqual((Balance.PlayerCap, 2), (c.Player.Held[(int)Item.Sword], c.Player.Held[(int)Item.Shield]), "escudos por cima das espadas");
            StandAt(c, c.Counter.InAt, 2f);
            Assert.AreEqual(2, c.SoldItems[(int)Item.Shield], "carga mista: a fila de escudo foi atendida");
        }

        // ------------------------------------------------------------------ 2a area (docs/AREA2_JOALHERIA.md)

        static Pad PadFor(Sim s, Upgrade u)
        {
            foreach (Pad p in s.Pads) if (Array.IndexOf(p.Chain, u) >= 0) return p;
            return null;
        }

        static bool At(V2 a, V2 b) => V2.Dist(a, b) < 0.01f;

        [Test]
        public void Corredor_SoltaOJogadorNaRuaLateral()
        {
            var s = new Sim();
            s.Player.Pos = new V2(4.5f, 6.5f);   // faixa da porta lateral: livre ate o fim da rua (parede solida fora das aberturas)
            Run(s, 5f, 1f, 0f);
            Assert.AreEqual(Balance.WorkshopW - 0.3f, s.Player.Pos.X, 1e-3f, "sem Corredor o jogador para na parede da oficina");
            Assert.AreEqual((int)Upgrade.SideCorridor, PadFor(s, Upgrade.SideCorridor).Current(s), "pad do Corredor a venda dentro da oficina");
            Assert.LessOrEqual(PadFor(s, Upgrade.SideCorridor).Pos.X, s.MaxX, "e alcancavel antes de comprar");
            s.Buy(Upgrade.SideCorridor);
            Run(s, 5f, 1f, 0f);
            Assert.AreEqual(14.7f, s.Player.Pos.X, 1e-3f, "com Corredor chega ao fim da rua (WorldW - 0,3)");
            Assert.AreEqual(14.7f, Sim.Load(s.Save(1)).Player.Pos.X, 0.01f, "o limite volta do save");
        }

        [Test]
        public void Joalheria_FechadaSemUpgrade_2LingotesViram1Joia()
        {
            var s = new Sim();
            Assert.IsFalse(s.JewelBench.Unlocked || s.JewelShop.Unlocked || s.LineUnlocked(Item.Jewel), "fechada no inicio");
            Assert.AreEqual(-1, PadFor(s, Upgrade.Jewelry).Current(s), "pad da Joalheria invisivel sem o Corredor");
            s.Buy(Upgrade.SideCorridor);
            Assert.IsFalse(s.JewelBench.Unlocked, "o Corredor sozinho nao abre a Joalheria");
            Assert.AreEqual((int)Upgrade.Jewelry, PadFor(s, Upgrade.Jewelry).Current(s), "teaser: o pad aparece depois do Corredor");
            s.JewelBench.In = 3;
            Run(s, 12f);
            Assert.AreEqual((3, 0), (s.JewelBench.In, s.JewelBench.Out), "fechada nao produz");
            s.Buy(Upgrade.Jewelry);
            Assert.IsTrue(s.JewelBench.Unlocked && s.JewelShop.Unlocked && s.LineUnlocked(Item.Jewel));
            float t = Balance.HammerTime0 * Balance.JewelTimeMul;
            Assert.AreEqual(t, s.JewelBench.Time, 1e-4f);
            Run(s, t - 0.3f);
            Assert.AreEqual((1, 0), (s.JewelBench.In, s.JewelBench.Out), "consumiu 2 lingotes e ainda martela");
            Run(s, 0.5f);
            Assert.AreEqual((1, 1), (s.JewelBench.In, s.JewelBench.Out), "2 lingotes viraram 1 joia em HammerTime0 x JewelTimeMul");
            Assert.AreEqual(1, s.Crafted[(int)Item.Jewel]);
            Run(s, 2f * t);
            Assert.AreEqual(1, s.JewelBench.Out, "1 lingote nao basta para outra joia");
            s.Buy(Upgrade.HammerSpeed);
            Assert.AreEqual(Balance.HammerTime1 * Balance.JewelTimeMul, s.JewelBench.Time, 1e-4f, "martelo veloz vale para a joalheria");
        }

        [Test]
        public void Joia_SoNaLojaDeJoias_EspadaNuncaLa()
        {
            var s = new Sim();
            s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry);
            Assert.IsTrue(s.Sells(s.JewelShop, Item.Jewel) && s.Sells(s.Counter, Item.Sword) && s.Sells(s.Counter, Item.Tool));
            Assert.IsFalse(s.Sells(s.JewelShop, Item.Sword) || s.Sells(s.JewelShop, Item.Shield) || s.Sells(s.JewelShop, Item.Tool));
            Assert.IsFalse(s.Sells(s.Counter, Item.Jewel) || s.Sells(s.JewelBench, Item.Jewel) || s.Sells(s.JewelShop, Item.Ingot));
            Assert.AreSame(s.JewelShop, s.CounterFor(Item.Jewel));
            Hold(s, Item.Jewel, 2);
            Assert.AreEqual((Hint.ProductToCounter, (int)Item.Jewel), (s.CurrentHint(), s.HintArg), "dica: leve joias (a view traduz para a loja de joias)");
            StandAt(s, s.Counter.InAt, 1f);
            Assert.AreEqual((2, 0), (s.Player.Count, s.Stock[(int)Item.Jewel]), "balcao principal recusa joia");
            StandAt(s, s.JewelShop.InAt, 1f);
            Assert.AreEqual((0, 2), (s.Player.Count, s.Stock[(int)Item.Jewel]), "loja de joias aceita");
            Hold(s, Item.Sword, 2);
            StandAt(s, s.JewelShop.InAt, 1f);
            Assert.AreEqual((2, 0), (s.Player.Count, s.Stock[(int)Item.Sword]), "espada nunca na loja de joias");
        }

        [Test]
        public void FilaDaJoalheria_CompraPor60_Teto3_PacienciaDaJoia()
        {
            var s = new Sim();
            s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry);
            Run(s, Balance.ClientInterval[(int)Item.Jewel] + 0.1f);
            Assert.AreEqual(1, s.JewelQueue.Count, "1o nobre chegou aos 14 s");
            Assert.AreEqual((Item.Jewel, Balance.PatienceFor(Item.Jewel)), (s.JewelQueue[0].Want, s.JewelQueue[0].MaxPatience));
            foreach (Client c in s.Queue) Assert.AreNotEqual(Item.Jewel, c.Want, "joia nunca na fila do balcao");
            s.Stock[(int)Item.Jewel] = 1;
            int gold = s.Gold, sold = 0;
            for (float t = 0f; t < 0.5f; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events)
                    if (e.Kind == Ev.Sold && e.A == (int)Item.Jewel) { sold++; Assert.AreEqual(60, e.B); Assert.IsTrue(At(e.Pos, s.JewelSlot(0)), "evento na vaga da loja de joias"); }
            }
            Assert.AreEqual((1, gold + 60, 0), (sold, s.Gold, s.JewelQueue.Count), "comprou a joia por 60 e saiu");

            // Vitrine: nobre a cada 9,8 s. Sem joia, a fila para em 3, o 4o nem entra e o 1o cansa aos 14 s + paciencia da joia
            var q = new Sim();
            q.Buy(Upgrade.CounterCapacity); q.Buy(Upgrade.SideCorridor); q.Buy(Upgrade.Jewelry);
            int max = 0, away = 0; float tired = -1f;
            for (float t = 0f; t < Balance.ClientInterval[(int)Item.Jewel] + Balance.PatienceFor(Item.Jewel) + 5f; t += Dt)
            {
                q.Tick(Dt, 0f, 0f);
                max = Math.Max(max, q.JewelQueue.Count);
                foreach (SimEvent e in q.Events)
                {
                    if (e.Kind != Ev.ClientLeft || e.A != (int)Item.Jewel) continue;
                    if (e.B == 2) away++;
                    else if (tired < 0f) tired = q.Time;
                }
            }
            Assert.AreEqual(Balance.JewelQueueCap, max, "fila de joias tem teto 3");
            Assert.Greater(away, 0, "o 4o nobre nao entrou");
            Assert.AreEqual(Balance.ClientInterval[(int)Item.Jewel] + Balance.PatienceFor(Item.Jewel), tired, 0.1f, "paciencia da joia");
        }

        [Test]
        public void Ajudantes_Papel1AbasteceAJoalheria_Papel2LevaJoiaALoja()
        {
            var s = new Sim();
            s.Buy(Upgrade.Helper1); s.Buy(Upgrade.Helper2); s.Buy(Upgrade.Helper3); s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry);
            s.JewelBench.Out = 3;
            s.FurnaceA.Out = 4;
            s.AnvilA.In = Balance.CrafterIn; s.AnvilA.Out = Balance.CrafterOut;   // bigorna cheia e travada: o lingote so pode ir a joalheria
            int jewelAtShop = 0, jewelElsewhere = 0, ingotAtBench = 0;
            for (float t = 0f; t < 40f; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events)
                {
                    if (e.Kind != Ev.Deposited) continue;
                    if (e.A == (int)Item.Jewel) { if (e.B == 2 && At(e.Pos, s.JewelShop.Pos)) jewelAtShop++; else jewelElsewhere++; }
                    if (e.A == (int)Item.Ingot && e.B == 1 && At(e.Pos, s.JewelBench.Pos)) ingotAtBench++;
                }
            }
            Assert.GreaterOrEqual(jewelAtShop, 2, "ajudante 3 levou joias a loja de joias");
            Assert.AreEqual(0, jewelElsewhere, "joia nunca vai ao balcao principal");
            Assert.GreaterOrEqual(ingotAtBench, 2, "ajudante 2 levou lingotes a bancada de joias");
        }

        [Test]
        public void Save_SegundaArea_17Flags_EstoqueDeJoia_IdaEVolta()
        {
            var s = new Sim();
            s.Buy(Upgrade.Helper1); s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry);
            s.Stock[(int)Item.Jewel] = 2; s.JewelBench.In = 3; s.JewelBench.Out = 1;
            s.Player.Pos = new V2(13f, 6f);
            string text = s.Save(5);
            StringAssert.Contains("up=00100000000000011000000000000\n", text);
            StringAssert.Contains("stock=0,0,0,2\n", text);
            StringAssert.Contains("st8=3,1,", text, "bancada de joias = estacao 8");
            Sim b = Sim.Load(text);
            Assert.IsTrue(b.Bought[(int)Upgrade.SideCorridor] && b.Bought[(int)Upgrade.Jewelry] && b.Bought[(int)Upgrade.Helper1]);
            Assert.AreEqual(3, b.UpgradesBought);
            Assert.IsTrue(b.JewelBench.Unlocked && b.JewelShop.Unlocked, "re-derivado dos flags");
            Assert.AreEqual(Balance.HammerTime0 * Balance.JewelTimeMul, b.JewelBench.Time, 1e-4f);
            Assert.AreEqual(2, b.Stock[(int)Item.Jewel]);
            Assert.AreEqual((3, 1), (b.JewelBench.In, b.JewelBench.Out));
            Assert.AreEqual(13f, b.Player.Pos.X, 0.01f, "na rua lateral continua na rua");
            Assert.AreEqual(text, b.Save(5), "save estavel");
        }

        [Test]
        public void Save_Antigo15Flags_Stock3_CarregaComASegundaAreaFechada()
        {
            // save real do v0.1 (formato de antes da 2a area): 15 flags, 12 pads, stock com 3 numeros; px fora da oficina (adulterado)
            const string old = "v=1\nt=600\ngold=190\nup=111111110000000\nut=53,119,196,259,334,429,498,562,-1,-1,-1,-1,-1,-1,-1\npads=0,0,0,0,0,0,0,0,30,0,0,0,\n" +
                               "st1=6,4,1,0.5,143\nst2=0,0,0,0,0\nst3=4,3,0,0,64\nst4=4,3,0,0,14\nst5=2,1,0,0,26\nst6=0,0,0,0,0\nstock=5,1,0\nema=4.54\nsaved=1000\nclaim=900\n" +
                               "m=19,101,1385,0,23,50,4,193,8\npx=13.00,5.00\n";
            Sim s = null;
            Assert.DoesNotThrow(() => s = Sim.Load(old));
            Assert.AreEqual((190 + 30, 8), (s.Gold, s.UpgradesBought), "os 30 pagos nas Botas (pad 8, hoje no menu) voltam ao ouro");
            Assert.IsFalse(s.Bought[(int)Upgrade.SideCorridor] || s.Bought[(int)Upgrade.Jewelry]);
            Assert.IsFalse(s.JewelBench.Unlocked || s.JewelShop.Unlocked || s.LineUnlocked(Item.Jewel), "2a area fechada");
            Assert.AreEqual((5, 1, 0, 0), (s.Stock[2], s.Stock[3], s.Stock[4], s.Stock[5]));
            Assert.AreEqual((0, -1, Upgrade.PlayerSpeed), (s.Pads[8].Paid, s.Pads[8].Current(s), s.Pads[8].Chain[0]), "pads antigos nos mesmos indices (8 = Botas, hoje no menu: invisivel)");
            Assert.AreEqual(Balance.WorkshopW - 0.3f, s.Player.Pos.X, 1e-3f, "sem Corredor o save nao poe o jogador na rua");
            Assert.AreEqual((int)Upgrade.SideCorridor, PadFor(s, Upgrade.SideCorridor).Current(s));
            string again = s.Save(1001);
            StringAssert.Contains("up=11111111000000000000000000000\n", again);
            StringAssert.Contains("stock=5,1,0,0\n", again);
            Run(s, 60f);
            Assert.AreEqual(0, s.JewelQueue.Count, "nenhum nobre com a area fechada");
        }

        // ------------------------------------------------------------------ 2a area, fase 2 (docs/AREA2_FASE2.md)

        static int[] Roles(Sim s) => s.Workers.ConvertAll(w => w.Role).ToArray();

        [Test]
        public void Joalheiro_Papel3_AbasteceSoABancadaDeJoias_ELevaJoiaALoja()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools); s.Buy(Upgrade.SideCorridor);
            Assert.AreEqual(-1, PadFor(s, Upgrade.Jeweler).Current(s), "pad do Joalheiro invisivel sem a Joalheria");
            s.Buy(Upgrade.Jewelry);
            Assert.AreEqual((int)Upgrade.Jeweler, PadFor(s, Upgrade.Jeweler).Current(s));
            s.Buy(Upgrade.Jeweler);
            CollectionAssert.AreEqual(new[] { 3 }, Roles(s), "papel 3 mesmo sem os Ajudantes 1-3");
            s.FurnaceA.Out = 4;      // lingotes prontos; a bancada de ferramentas (vazia) fica no caminho fornalha -> joalheria
            s.JewelBench.Out = 2;    // joias prontas
            int ingotAtBench = 0, jewelAtShop = 0, wrong = 0;
            for (float t = 0f; t < 40f; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events)
                {
                    if (e.B != 3) continue;
                    if (e.Kind == Ev.Deposited)
                    {
                        if (e.A == (int)Item.Ingot && At(e.Pos, s.JewelBench.Pos)) ingotAtBench++;
                        else if (e.A == (int)Item.Jewel && At(e.Pos, s.JewelShop.Pos)) jewelAtShop++;
                        else wrong++;
                    }
                    if (e.Kind == Ev.Picked && !(e.A == (int)Item.Ingot && At(e.Pos, s.FurnaceA.Pos)) && !(e.A == (int)Item.Jewel && At(e.Pos, s.JewelBench.Pos))) wrong++;
                }
            }
            Assert.GreaterOrEqual(ingotAtBench, 4, "levou os 4 lingotes da fornalha a bancada de joias");
            Assert.GreaterOrEqual(jewelAtShop, 2, "levou as joias prontas a loja de joias");
            Assert.AreEqual(0, wrong, "nunca abastece outra bancada nem pega outra coisa");
            Assert.AreEqual(0, s.ToolBench.In, "passou pela bancada de ferramentas sem largar lingote");
            s.Buy(Upgrade.Helper1); s.Buy(Upgrade.HelperSpeed);
            Carrier j = s.Workers[0];
            Assert.AreEqual((3, Balance.WorkerSpeedUp, Balance.WorkerCapUp), (j.Role, j.Speed, j.Cap), "Ajudantes ageis valem para o joalheiro");
        }

        [Test]
        public void Joalheiro_NaoAtrapalhaOsPapeis0a2()
        {
            var s = new Sim();
            s.Buy(Upgrade.Helper1); s.Buy(Upgrade.Helper2); s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry); s.Buy(Upgrade.Jeweler); s.Buy(Upgrade.Helper3);
            CollectionAssert.AreEqual(new[] { 0, 1, 3, 2 }, Roles(s), "o Ajudante 3 comprado depois do Joalheiro continua papel 2 (lista na ordem da compra)");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, Roles(Sim.Load(s.Save(1))), "do save: papel = upgrade");

            // bancada de joias abastecida: o joalheiro nao tira lingote das outras linhas
            var full = new Sim();
            full.Buy(Upgrade.SideCorridor); full.Buy(Upgrade.Jewelry); full.Buy(Upgrade.Jeweler);
            full.JewelBench.In = full.JewelBench.InCap; full.JewelBench.Busy = true; full.JewelBench.Progress = 0f;
            full.FurnaceA.Out = 4;
            Run(full, 4f);
            Assert.AreEqual((4, 0), (full.FurnaceA.Out, full.Workers[0].Count), "com a bancada cheia ele espera, os lingotes ficam para as bigornas");

            // com os 4 ajudantes e o jogador parado, as linhas de espada e de joia andam sozinhas
            int sword = s.SoldItems[(int)Item.Sword];
            Run(s, 180f);
            Assert.Greater(s.SoldItems[(int)Item.Sword], sword + 3, "papeis 0-2 seguem fechando o ciclo da espada");
            Assert.Greater(s.Crafted[(int)Item.Jewel], 0, "e a joalheria produz");
        }

        [Test]
        public void Lupa_BancadaDeJoias_x06_SoNaJoalheria()
        {
            var s = new Sim();
            s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry);
            float anvil = s.AnvilA.Time, jewel = s.JewelBench.Time;
            Assert.AreEqual((-1, true), (PadFor(s, Upgrade.JewelSpeed).Current(s), s.MenuAvailable((int)Upgrade.JewelSpeed)), "a venda no menu, sem pad");
            s.Buy(Upgrade.JewelSpeed);
            float t = Balance.HammerTime0 * Balance.JewelTimeMul * Balance.JewelSpeedMul;
            Assert.AreEqual(jewel * 0.6f, s.JewelBench.Time, 1e-4f, "x0,6");
            Assert.AreEqual(t, s.JewelBench.Time, 1e-4f);
            Assert.AreEqual(anvil, s.AnvilA.Time, "a bigorna nao muda");
            s.JewelBench.In = 2;
            Run(s, t - 0.3f);
            Assert.AreEqual(0, s.JewelBench.Out, "ainda martela");
            Run(s, 0.5f);
            Assert.AreEqual(1, s.JewelBench.Out, "joia em 6 s em vez de 10");
            s.Buy(Upgrade.HammerSpeed);
            Assert.AreEqual(Balance.HammerTime1 * Balance.JewelTimeMul * Balance.JewelSpeedMul, s.JewelBench.Time, 1e-4f, "soma com o Martelo veloz");
            Assert.AreEqual(s.JewelBench.Time, Sim.Load(s.Save(1)).JewelBench.Time, 1e-4f, "re-derivado do save");
        }

        [Test]
        public void VitrineDeJoias_Fila5_NobresX07_SoNaJoalheria()
        {
            var s = new Sim();
            s.Buy(Upgrade.CounterCapacity); s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry);
            float jewel = s.ClientInterval(Item.Jewel), sword = s.ClientInterval(Item.Sword);
            Assert.AreEqual(Balance.JewelQueueCap, s.JewelQueueCap);
            s.Buy(Upgrade.JewelVitrine);
            Assert.AreEqual(5, s.JewelQueueCap, "fila 3 -> 5");
            Assert.AreEqual(jewel * 0.7f, s.ClientInterval(Item.Jewel), 1e-4f, "nobres x0,7 (soma com a Vitrine)");
            Assert.AreEqual((sword, Balance.QueueCap0), (s.ClientInterval(Item.Sword), s.QueueCap), "o balcao principal nao muda");
            int max = 0;
            for (float t = 0f; t < 60f; t += Dt) { s.Tick(Dt, 0f, 0f); max = Math.Max(max, s.JewelQueue.Count); }
            Assert.AreEqual(5, max, "sem joia, a fila de nobres enche ate 5");
            Assert.AreEqual(5, Sim.Load(s.Save(1)).JewelQueueCap, "re-derivado do save");
        }

        [Test]
        public void VitrineDeJoias_Preco60Antes80Depois_FilaEDireta_EventoEReceita()
        {
            void Check(bool upgraded, bool direct)
            {
                var s = new Sim();
                s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry);
                if (upgraded) s.Buy(Upgrade.JewelVitrine);
                int price = upgraded ? 80 : 60;
                if (direct)
                    while (s.Time + Dt < Balance.ClientInterval[(int)Item.Jewel]) s.Tick(Dt, 0f, 0f);
                int queue = direct ? s.JewelQueueCap : 1;   // direct = um nobre chega com a fila cheia (FASE7: vai embora; nao ha compra direta)
                for (int i = 0; i < queue; i++) s.JewelQueue.Add(new Client { Want = Item.Jewel, Patience = 999f, MaxPatience = 999f });
                s.Stock[(int)Item.Jewel] = 1;
                int gold = s.Gold, earned = s.GoldEarned;
                s.Tick(Dt, 0f, 0f);
                Assert.AreEqual((gold + price, earned + price, 1, 1, 0), (s.Gold, s.GoldEarned, s.Sales, s.SoldItems[(int)Item.Jewel], s.Stock[(int)Item.Jewel]));
                Assert.AreEqual((queue - 1, direct ? 1 : 0), (s.JewelQueue.Count, s.ClientsTurnedAway), "o atendimento remove o nobre da vaga; quem chega com a fila cheia vai embora");
                Assert.AreEqual(1, Count(s, Ev.Sold), "uma venda, sem pagamento duplicado");
                foreach (SimEvent e in s.Events)
                    if (e.Kind == Ev.Sold)
                    {
                        Assert.AreEqual(((int)Item.Jewel, price), (e.A, e.B));
                        Assert.IsTrue(At(e.Pos, s.JewelSlot(0)), "a venda sai da vaga do nobre atendido");
                    }
                Assert.AreEqual((10, 25, 16), (s.PriceOf(Item.Sword), s.PriceOf(Item.Shield), s.PriceOf(Item.Tool)), "outras linhas nao mudam");
                Assert.AreEqual(price, Sim.Load(s.Save(1)).PriceOf(Item.Jewel), "preco re-derivado do upgrade salvo");
            }
            Check(false, false); Check(true, false);
            Check(false, true); Check(true, true);
        }

        [Test]
        public void Marco_DisparaUmaVez_BauPagaUmaVez_UmPorLugar()
        {
            var s = new Sim();
            Assert.AreEqual(Balance.Milestones.Length, s.Chests.Count);
            for (int i = 0; i < s.Chests.Count; i++)
                Assert.AreEqual((i, 0, Balance.Milestones[i].Gold, Balance.Milestones[i].Label), (s.Chests[i].Index, s.Chests[i].State, s.Chests[i].Gold, s.Chests[i].Label));
            MilestoneDef m0 = Balance.Milestones[0];
            s.SoldItems[(int)Item.Sword] = m0.Count - 1; s.Stock[(int)Item.Sword] = 1;   // a proxima venda de espada bate o marco
            int fired = 0;
            for (float t = 0f; t < Balance.FirstClientAt + 5f; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events)
                    if (e.Kind == Ev.Milestone) { fired++; Assert.AreEqual((0, m0.Gold), (e.A, e.B)); Assert.IsTrue(At(e.Pos, m0.Pos)); }
            }
            Assert.AreEqual(1, fired, "a venda que completou o marco dispara uma vez so");
            Assert.AreEqual((1, m0.Count), (s.Chests[0].State, s.SoldItems[(int)Item.Sword]));

            s.Sales = Balance.Milestones[1].Count;                       // 50 vendas: mesmo lugar do bau 0, que ainda esta na tela
            s.SoldItems[(int)Item.Jewel] = Balance.Milestones[2].Count;  // 10 joias: outro lugar (rua lateral)
            Run(s, 1f);
            Assert.AreEqual((1, 0, 1), (s.Chests[0].State, s.Chests[1].State, s.Chests[2].State), "um bau por vez em cada lugar");

            int gold = s.Gold, earned = s.GoldEarned;
            var opened = new int[s.Chests.Count];
            void Walk(V2 p, float seconds)
            {
                s.Player.Pos = p;
                for (float t = 0f; t < seconds; t += Dt) { s.Tick(Dt, 0f, 0f); foreach (SimEvent e in s.Events) if (e.Kind == Ev.ChestOpened) { opened[e.A]++; Assert.AreEqual(s.Chests[e.A].Gold, e.B); } }
            }
            Walk(m0.Pos, 3f);
            Assert.AreEqual(new[] { 1, 0, 0, 0 }, opened, "pisou: abre o bau 0 uma vez, parado em cima nao paga de novo");
            Assert.AreEqual((2, 0), (s.Chests[0].State, s.Chests[1].State), "aberto; o proximo do mesmo lugar espera o jogador sair de cima");
            Assert.AreEqual(gold + m0.Gold, s.Gold);
            Walk(new V2(4.5f, 3.5f), 1f);
            Assert.AreEqual(1, s.Chests[1].State, "saiu: o bau 1 aparece no mesmo lugar");
            Walk(m0.Pos, 1f);
            Assert.AreEqual(new[] { 1, 1, 0, 0 }, opened, "voltando abre o bau 1 (agora no lugar), nunca o 0 de novo");
            Assert.AreEqual(gold + m0.Gold + Balance.Milestones[1].Gold, s.Gold);
            Assert.AreEqual(earned, s.GoldEarned, "ouro de bau nao e' renda online (nao infla a taxa nem o offline)");
            Assert.AreEqual(1, s.Chests[2].State, "o bau da rua so abre na rua");
        }

        [Test]
        public void Dica_BauDisponivel_LogoDepoisDoPad_EFilaDeJoiasNoChao()
        {
            var s = new Sim();
            Hold(s, Item.Ore, 2);
            Assert.AreEqual(Hint.OreToFurnace, s.CurrentHint());
            s.Chests[3].State = 1; s.Chests[2].State = 1;
            Assert.AreEqual((Hint.OpenChest, 2), (s.CurrentHint(), s.HintArg), "bau disponivel vem antes do que carrega; o 1o na ordem dos marcos");
            s.Gold = 100000;
            Assert.AreEqual(Hint.BuyMenu, s.CurrentHint(), "compra pagavel (aqui o Fole, do menu) continua na frente");
            s.Gold = 0; s.Chests[2].State = 2; s.Chests[3].State = 2;
            Assert.AreEqual(Hint.OreToFurnace, s.CurrentHint(), "aberto: some da dica");
            // fila de joias centrada na loja (coordenador): as 5 vagas da Vitrine de joias ficam dentro do chao
            Assert.IsTrue(At(s.JewelSlot(2), new V2(s.JewelShop.Pos.X, s.JewelShop.Pos.Y + 1f)), "vaga do meio na frente da loja");
            Assert.AreEqual((10.3f, 13.7f), (s.JewelSlot(0).X, s.JewelSlot(4).X));
            Assert.Less(s.JewelSlot(Balance.JewelQueueCapUp).X, Balance.WorldW, "ate a vaga da compra direta (depois da ultima) cabe no mapa");
        }

        [Test]
        public void Save_Marcos_IdaEVolta_NaoPagaDeNovo()
        {
            var s = new Sim();
            s.Chests[0].State = 2; s.Chests[1].State = 1; s.SoldItems[(int)Item.Sword] = 40; s.SoldItems[(int)Item.Jewel] = 3; s.Sales = 60;
            string text = s.Save(7);
            StringAssert.Contains("ms=2,1,0,0\n", text);
            StringAssert.Contains("sold=40,0,0,3\n", text);
            Sim b = Sim.Load(text);
            CollectionAssert.AreEqual(new[] { 2, 1, 0, 0 }, b.Chests.ConvertAll(c => c.State));
            Assert.AreEqual((40, 3), (b.SoldItems[(int)Item.Sword], b.SoldItems[(int)Item.Jewel]));
            Assert.AreEqual(text, b.Save(7), "save estavel");
            b.Player.Pos = b.Chests[1].Pos;
            Run(b, 1f);
            Assert.AreEqual((Balance.Milestones[1].Gold, 2), (b.Gold, b.Chests[1].State), "o disponivel do save paga uma vez");
            Sim c = Sim.Load(b.Save(8));
            c.Player.Pos = c.Chests[1].Pos;
            Run(c, 1f);
            Assert.AreEqual(Balance.Milestones[1].Gold, c.Gold, "recarregar depois de abrir nao paga de novo");
            Sim junk = Sim.Load("ms=9,-1,abc\nsold=-5,x\n");
            CollectionAssert.AreEqual(new[] { 2, 0, 0, 0 }, junk.Chests.ConvertAll(ch => ch.State), "lixo vira 0..2");
            Assert.AreEqual(0, junk.SoldItems[(int)Item.Sword]);
        }

        // ------------------------------------------------------------------ fase 3: luxo sem bonus produtivo

        static readonly Upgrade[] Luxury = { Upgrade.WorkshopFacade, Upgrade.WorkshopFloor, Upgrade.JewelryDecor };

        static Sim CompleteProduction()
        {
            var s = Rich();
            for (int i = 0; i < Upgrades.Count; i++) if (!Upgrades.IsLuxury(i)) s.Buy((Upgrade)i);
            return s;
        }

        [Test]
        public void Luxo_ExigeTodosOsProdutivos_MesmoVitrineForaDaOrdem()
        {
            for (int missing = 0; missing < Upgrades.Count; missing++)
            {
                if (Upgrades.IsLuxury(missing)) continue;
                var s = Rich();
                if (missing != (int)Upgrade.JewelVitrine) s.Buy(Upgrade.JewelVitrine);
                for (int i = 0; i < Upgrades.Count; i++) if (i != missing && !Upgrades.IsLuxury(i)) s.Buy((Upgrade)i);
                Assert.IsFalse(s.ProductionComplete, "falta " + (Upgrade)missing);
                foreach (Upgrade u in Luxury)
                {
                    Assert.AreEqual(-1, PadFor(s, u).Current(s), "pad bloqueado: " + (Upgrade)missing);
                    Assert.IsFalse(s.Buy(u), "nem compra direta burla os 22 produtivos");
                }
            }
            var full = CompleteProduction();
            Assert.IsTrue(full.ProductionComplete);
            Assert.AreEqual((int)Upgrade.WorkshopFacade, full.Pads[17].Current(full));
            Assert.AreEqual(-1, full.Pads[18].Current(full));
            Assert.AreEqual(-1, full.Pads[19].Current(full));
            Assert.IsFalse(full.Buy(Upgrade.WorkshopFloor));
            Assert.IsFalse(full.Buy(Upgrade.JewelryDecor));
            Assert.AreEqual((22, 10), (full.Pads.Count, full.Stations.Count), "pads anexados, estacoes preservadas");
            Assert.AreEqual((6.5f, 3.2f), (full.Pads[17].Pos.X, full.Pads[17].Pos.Y));
            Assert.AreEqual((4.5f, 3.2f), (full.Pads[18].Pos.X, full.Pads[18].Pos.Y));
            Assert.AreEqual((14.2f, 5.1f), (full.Pads[19].Pos.X, full.Pads[19].Pos.Y));
            Assert.IsTrue(full.Buy(Upgrade.WorkshopFacade));
            Assert.AreEqual((int)Upgrade.WorkshopFloor, full.Pads[18].Current(full));
            Assert.IsTrue(full.Buy(Upgrade.WorkshopFloor));
            Assert.AreEqual((int)Upgrade.JewelryDecor, full.Pads[19].Current(full));
        }

        /// <summary>
        /// Divida tecnica (FASE3_LUXO, "Pendencia tecnica"): luxo e' marca por upgrade (UpgradeDef.Luxury), nao "ID >= 20".
        /// Simula um produtivo ANEXADO depois dos luxos trocando, so durante o teste, a definicao do ultimo ID (22) por uma
        /// produtiva que requer o Joalheiro: pad visivel antes da producao completa, compra direta, entra em
        /// ProductionComplete e no teto offline, e o luxo passa a espera-lo. Com a regra antiga por ID, fica vermelho.
        /// </summary>
        [Test]
        public void Marca_ProdutivoAnexadoDepoisDosLuxos_ETratadoComoProdutivo()
        {
            Assert.AreEqual(Upgrades.ProductionCount, Array.FindAll(Upgrades.All, d => !d.Luxury).Length, "ProductionCount = quantos sem marca de luxo");
            foreach (Upgrade u in Luxury) Assert.IsTrue(Upgrades.IsLuxury((int)u), u + " e' luxo");
            int sim = (int)Upgrade.JewelryDecor;
            UpgradeDef real = Upgrades.All[sim];
            Upgrades.All[sim] = new UpgradeDef(Upgrade.JewelryDecor, (int)Upgrade.Jeweler, "Produtivo anexado", "simulado");
            try
            {
                var s = Rich();
                for (int i = 0; i <= (int)Upgrade.Jeweler; i++) s.Buy((Upgrade)i);
                Assert.AreEqual(sim, PadFor(s, Upgrade.JewelryDecor).Current(s), "pad visivel antes da producao completa");
                for (int i = 0; i < Upgrades.Count; i++) if (i != sim && !Upgrades.IsLuxury(i)) s.Buy((Upgrade)i);
                Assert.IsFalse(s.ProductionComplete, "falta o produtivo anexado");
                Assert.AreEqual(-1, PadFor(s, Upgrade.WorkshopFacade).Current(s), "luxo espera o anexado");
                Assert.IsFalse(s.Buy(Upgrade.WorkshopFacade));
                Assert.AreEqual(Upgrades.Cost(sim), s.CheapestLockedCost(), "entra no teto offline como menor travado");
                Assert.IsTrue(s.Buy(Upgrade.JewelryDecor), "compra direta sem producao completa");
                Assert.IsTrue(s.ProductionComplete);
                Assert.IsTrue(s.Buy(Upgrade.WorkshopFacade), "agora o luxo abre");
            }
            finally { Upgrades.All[sim] = real; }
            Assert.IsTrue(Upgrades.IsLuxury(sim), "definicao real restaurada");
        }

        [Test]
        public void Luxo_PadsCobramUmaVez_ParcialETroco_RearmamAoSair()
        {
            var s = CompleteProduction();
            int luxury = 0;
            foreach (Upgrade u in Luxury) luxury += Upgrades.Cost(u);
            int budget = luxury + 333;
            s.Gold = budget;
            int bought = 0;
            foreach (Upgrade u in Luxury)
            {
                Pad p = PadFor(s, u);
                s.Player.Pos = p.Pos;
                s.Workers.Clear(); s.Tick(Dt, 0f, 0f);
                Assert.Greater(p.Paid, 0, "guarda pagamento parcial");
                int goldAfterPartial = s.Gold, partial = p.Paid;
                for (int ticks = 0; !s.Bought[(int)u] && ticks < 90; ticks++)
                {
                    s.Workers.Clear(); s.Tick(Dt, 0f, 0f);   // isola pagamento de vendas da oficina
                    foreach (SimEvent e in s.Events)
                        if (e.Kind == Ev.Bought)
                        {
                            bought++;
                            Assert.AreEqual(((int)u, Upgrades.Cost(u)), (e.A, e.B));
                            Assert.IsTrue(At(e.Pos, p.Pos));
                        }
                }
                Assert.IsTrue(s.Bought[(int)u]);
                Assert.AreEqual(goldAfterPartial - (Upgrades.Cost(u) - partial), s.Gold, "so cobra o restante");
                Assert.AreEqual(0, p.Paid);
                Assert.IsFalse(p.Armed, "tick de compra desarma o pad");
                int gold = s.Gold;
                s.Player.Pos = new V2(11f, 2f);
                s.Workers.Clear(); s.Tick(Dt, 0f, 0f);
                Assert.IsTrue(p.Armed, "sair rearma");
                s.Player.Pos = p.Pos;
                s.Workers.Clear(); s.Tick(Dt, 0f, 0f);
                Assert.AreEqual(gold, s.Gold, "voltar ao pad comprado nao cobra");
                s.Events.Clear();
                Assert.IsFalse(s.Buy(u), "compra repetida e' no-op");
                Assert.AreEqual(0, Count(s, Ev.Bought));
            }
            Assert.AreEqual((333, 0, Upgrades.Count, 3), (s.Gold, s.GoldEarned, s.UpgradesBought, bought), "o preco do luxo inteiro de ralo, sem receita ou compra duplicada");
        }

        [Test]
        public void Save_Antigo20Flags17Pads_ELuxoIdaEVolta()
        {
            const string old = "v=1\nt=3600\ngold=7000\nup=11111111111111111110\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,450,\nstock=2,3,1,4\nsold=200,40,30,10\nms=2,2,1,2\nema=17\nsaved=500\nclaim=400\npx=12,9\n";
            Sim s = Sim.Load(old);
            Assert.AreEqual((19, 22, 0, 7000 + 450), (s.UpgradesBought, s.Pads.Count, s.Pads[16].Paid, s.Gold), "os 450 da Vitrine de joias (pad 16, hoje no menu) voltam ao ouro");
            Assert.IsFalse(s.ProductionComplete);
            foreach (Upgrade u in Luxury) { Assert.IsFalse(s.Bought[(int)u]); Assert.AreEqual(0, PadFor(s, u).Paid); }
            Assert.AreEqual((2, 3, 1, 4), (s.Stock[2], s.Stock[3], s.Stock[4], s.Stock[5]));
            s.Buy(Upgrade.JewelVitrine); s.Buy(Upgrade.Miner); s.Buy(Upgrade.Jeweler2);
            Assert.IsFalse(s.Buy(Upgrade.WorkshopFacade), "FASE7: o luxo espera tambem as 4 vagas do balcao");
            BuyCounterTiers(s); s.Buy(Upgrade.WorkshopFacade); s.Buy(Upgrade.WorkshopFloor);
            s.Pads[19].Paid = 750;
            s.Pads[16].Paid = 0;
            string text = s.Save(501);
            StringAssert.Contains("up=11111111111111111111110111111\n", text);
            Sim again = Sim.Load(text);
            Assert.AreEqual(text, again.Save(501), "flags, ouro, marcos e pad parcial fazem roundtrip estavel");
            Assert.AreEqual((28, 750, (int)Upgrade.JewelryDecor), (again.UpgradesBought, again.Pads[19].Paid, again.Pads[19].Current(again)));
            Assert.AreEqual((400L, 501L), (again.LastClaim, again.SavedAt));
        }

        [Test]
        public void Luxo_NaoMudaProducaoDemandaReceitaOuCapacidade()
        {
            var plain = CompleteProduction();
            var luxury = Sim.Load(plain.Save(1));
            foreach (Upgrade u in Luxury) luxury.Buy(u);
            foreach (Item item in new[] { Item.Sword, Item.Shield, Item.Tool, Item.Jewel })
                Assert.AreEqual((plain.PriceOf(item), plain.ClientInterval(item)), (luxury.PriceOf(item), luxury.ClientInterval(item)));
            Assert.AreEqual((plain.Player.Speed, plain.Player.Cap, plain.CounterCap, plain.QueueCap, plain.JewelQueueCap, plain.HasConveyor, plain.MaxX),
                            (luxury.Player.Speed, luxury.Player.Cap, luxury.CounterCap, luxury.QueueCap, luxury.JewelQueueCap, luxury.HasConveyor, luxury.MaxX));
            for (int i = 0; i < plain.Stations.Count; i++)
            {
                Station a = plain.Stations[i], b = luxury.Stations[i];
                Assert.AreEqual((a.Time, a.Need, a.InCap, a.OutCap, a.Unlocked), (b.Time, b.Need, b.InCap, b.OutCap, b.Unlocked));
            }
            // a unica diferenca permitida e' a geometria (pedestais e postes solidos desviam ajudantes: FASE5 §2); igualada aqui
            // para isolar o efeito economico, que tem de ser zero
            Assert.AreEqual(plain.Solids.Count + 6, luxury.Solids.Count, "4 pedestais + 2 postes da fachada viram corpos");
            plain.Solids.Clear(); plain.Solids.AddRange(luxury.Solids);
            Run(plain, 180f); Run(luxury, 180f);   // mesmo jogador parado, mesmos ajudantes trabalhando
            CollectionAssert.AreEqual(plain.Crafted, luxury.Crafted);
            CollectionAssert.AreEqual(plain.Stock, luxury.Stock);
            CollectionAssert.AreEqual(plain.SoldItems, luxury.SoldItems);
            Assert.AreEqual((plain.Gold, plain.GoldEarned, plain.Sales, plain.ClientsLost, plain.ClientsTurnedAway, plain.RateEma),
                            (luxury.Gold, luxury.GoldEarned, luxury.Sales, luxury.ClientsLost, luxury.ClientsTurnedAway, luxury.RateEma));
            Assert.AreEqual(6, luxury.Workers.Count, "luxo nao contrata (4 ajudantes + Mineiro + Joalheiro 2)");
        }

        [Test]
        public void Offline_IgnoraLuxo_TetoProdutivo18000_ClaimMaxIdempotente()
        {
            // 20 = falta o par da fase 4: 2x Mineiro (FASE7: o teto so conta o que esta a venda; o Joalheiro 2 exige o Mineiro)
            int[] bought = { 0, 11, 17, 20, Upgrades.Count }, expected = { 100, 1380, 4400, 6000, 18000 };
            for (int j = 0; j < bought.Length; j++)
            {
                var s = new Sim(); s.RateEma = 100;
                for (int i = 0; i < bought[j]; i++) s.Buy((Upgrade)i);
                if (s.Bought[(int)Upgrade.CounterCapacity]) BuyCounterTiers(s);   // FASE7: as vagas saem logo depois da Vitrine
                Assert.AreEqual(expected[j], s.OfflineMaxGold(), "curva produtiva preservada");
                Assert.AreEqual(expected[j], s.ApplyOffline(long.MaxValue, 1));
            }
            var vit = new Sim(); vit.RateEma = 100;
            for (int i = 0; i <= (int)Upgrade.PlayerSpeed; i++) vit.Buy((Upgrade)i);   // o estado do bot aos 10 min
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.Tools), vit.OfflineMaxGold(), "FASE7: o Balcao 5 (150) exige a Vitrine, nao esta a venda e nao baixa o cofre");
            for (int i = 0; i <= (int)Upgrade.CounterCapacity; i++) vit.Buy((Upgrade)i);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.Counter5), vit.OfflineMaxGold(), "FASE7: Vitrine sem as vagas = 2x o Balcao 5");
            var full = CompleteProduction(); full.Gold = 0; full.RateEma = 100;
            Assert.AreEqual(18000, full.ApplyOffline(8 * 3600, 10), "luxos ainda travados nao abaixam o teto para 12000");
            foreach (Upgrade u in Luxury)
            {
                full.Buy(u);
                Assert.AreEqual((9000, 18000), (full.CheapestLockedCost(), full.OfflineMaxGold()), "luxo comprado nao aumenta o teto");
            }
            Sim loaded = Sim.Load(full.Save(11));
            Assert.AreEqual(0, loaded.ApplyOffline(7200, 10), "reload nao duplica claim");
            Assert.AreEqual(18000, loaded.ApplyOffline(long.MaxValue, long.MaxValue));
            int gold = loaded.Gold;
            Assert.AreEqual(0, loaded.ApplyOffline(long.MaxValue, long.MaxValue));
            Assert.AreEqual(0, loaded.ApplyOffline(7200, 11));
            Assert.AreEqual(gold, loaded.Gold);
        }

        [Test]
        public void Save_Antigo17Flags_SemMarcos_Carrega()
        {
            // save da v0.2 (antes da fase 2): 17 flags, 14 pads, sem ms= nem sold=
            const string v02 = "v=1\nt=1800\ngold=900\nup=11111111111111111\nut=53,119,196,259,334,429,498,562,647,724,817,1037,1201,1403,918,1562,1704\n" +
                               "pads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,\nst1=6,4,1,0.5,900\nst8=2,1,0,0,20\nstock=5,1,0,2\nema=16.3\nsaved=1000\nclaim=900\n" +
                               "m=19,783,12196,0,23,50,4,193,17\npx=13.00,5.00\n";
            Sim s = null;
            Assert.DoesNotThrow(() => s = Sim.Load(v02));
            Assert.AreEqual((900, 17), (s.Gold, s.UpgradesBought));
            Assert.IsFalse(s.Bought[(int)Upgrade.Jeweler] || s.Bought[(int)Upgrade.JewelSpeed] || s.Bought[(int)Upgrade.JewelVitrine]);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, Roles(s), "sem joalheiro");
            Assert.AreEqual((Balance.JewelQueueCap, Balance.HammerTime1 * Balance.JewelTimeMul), (s.JewelQueueCap, s.JewelBench.Time));
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, s.Chests.ConvertAll(c => c.State), "nenhum marco");
            Assert.AreEqual((int)Upgrade.Jeweler, PadFor(s, Upgrade.Jeweler).Current(s), "pad novo visivel (Joalheria comprada) e zerado");
            Assert.AreEqual(13f, s.Player.Pos.X, 0.01f);
            string again = s.Save(1001);
            StringAssert.Contains("up=11111111111111111000000000000\n", again);
            StringAssert.Contains("ms=0,0,0,0\n", again);
            Run(s, 0.1f);
            CollectionAssert.AreEqual(new[] { 0, 1, 0, 0 }, s.Chests.ConvertAll(c => c.State),
                "783 vendas: o bau de 50 aparece no 1o tick (o de 200 espera ele abrir; espadas por item contam do zero)");
        }

        // ------------------------------------------------------------------ fase 5: paciencia por item, fisica e bocas (docs/FASE5_FISICA_PACIENCIA.md)

        [Test]
        public void Paciencia_PorItem_ProporcionalAProducaoInicial()
        {
            Assert.AreEqual((57f, 76.5f, 57f, 84f), (Balance.PatienceFor(Item.Sword), Balance.PatienceFor(Item.Shield), Balance.PatienceFor(Item.Tool), Balance.PatienceFor(Item.Jewel)),
                "30 + 3 x (fornalha 4 s x lingotes + bancada 5 s x multiplicador)");
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools); s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry);
            var seen = new HashSet<Item>();
            float shieldIn = -1f, shieldOut = -1f;
            for (float t = 0f; t < 120f; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (Client c in s.Queue) { seen.Add(c.Want); Assert.AreEqual(Balance.PatienceFor(c.Want), c.MaxPatience, "fila do balcao: paciencia do proprio item"); }
                foreach (Client c in s.JewelQueue) { seen.Add(c.Want); Assert.AreEqual(Balance.PatienceFor(Item.Jewel), c.MaxPatience); }
                foreach (SimEvent e in s.Events)
                {
                    if (e.A != (int)Item.Shield) continue;
                    if (e.Kind == Ev.ClientArrived && shieldIn < 0f) shieldIn = s.Time;
                    if (e.Kind == Ev.ClientLeft && e.B == 0 && shieldOut < 0f) shieldOut = s.Time;
                }
            }
            Assert.AreEqual(4, seen.Count, "espada, escudo, ferramenta e joia passaram pelas filas");
            Assert.AreEqual(Balance.PatienceFor(Item.Shield), shieldOut - shieldIn, 0.1f, "o 1o cliente de escudo, sem escudo, cansa em 76,5 s");
        }

        /// <summary>Todos os corpos possiveis: estacoes desbloqueadas, loja e a decoracao inteira (luxo comprado).</summary>
        static Sim AllBodies()
        {
            var s = CompleteProduction();
            foreach (Upgrade u in Luxury) s.Buy(u);
            s.Workers.Clear();
            return s;
        }

        /// <summary>Menor distancia de `p` a um corpo solido (0 = dentro).</summary>
        static float Inside(Sim s, V2 p)
        {
            float worst = float.MaxValue;
            foreach (Box b in s.Solids) worst = Math.Min(worst, b.Dist(p));
            return worst;
        }

        [Test]
        public void Fisica_EmpurrandoContraQualquerCorpo_NuncaEntra()
        {
            var s = AllBodies();
            Assert.AreEqual(s.Stations.Count + Balance.Obstacles.Length, s.Solids.Count, "10 estacoes + carroca, 3 segmentos da parede, 4 pedestais e 2 postes");
            var dirs = new[] { new V2(1f, 0f), new V2(-1f, 0f), new V2(0f, 1f), new V2(0f, -1f), new V2(0.7f, 0.7f), new V2(-0.7f, 0.7f), new V2(0.7f, -0.7f), new V2(-0.7f, -0.7f) };
            foreach (Box b in s.Solids.ToArray())
                foreach (V2 d in dirs)
                {
                    s.Player.Pos = b.Pos + d * 1.5f;
                    for (int i = 0; i < 90; i++)
                    {
                        s.Tick(Dt, -d.X, -d.Y);
                        Assert.GreaterOrEqual(Inside(s, s.Player.Pos), Balance.CharRadius - 1e-3f, $"empurrando {d} contra o corpo em {b.Pos}: dentro em {s.Player.Pos}");
                    }
                }
        }

        /// <summary>
        /// FASE7: com a Joalheria real, a Loja de joias e os 2 pedestais da frente deixam vaos de 0,1-0,2 m (menores que o personagem)
        /// dos dois lados da boca. O Steer escolhia a quina do vao e quem saia da loja rumo a oficina ficava parado ali (bot de 90 min,
        /// aos 81 min, depois da carga mista). Rota cuja quina nao cabe o personagem e' descartada.
        /// </summary>
        [Test]
        public void Steer_BocaDaLojaDeJoias_EntreLojaEPedestais_SaiDoVao()
        {
            var s = AllBodies();
            foreach (V2 start in new[] { new V2(11.40f, 10.79f), new V2(11.40f, 10.85f), s.JewelShop.InAt, new V2(12.6f, 10.85f) })
            {
                var c = new Carrier { Pos = start, Speed = Balance.PlayerSpeed };
                bool arrived = false;
                for (float t = 0f; t < 15f && !arrived; t += Dt) arrived = s.MoveTowards(c, s.ShieldBench.OutAt, Dt, Bot.Reach);
                Assert.IsTrue(arrived, $"de {start} ate a saida dos Escudos: parou em {c.Pos}");
            }
        }

        [Test]
        public void Fisica_DeslizaNaQuina_EAjudanteContornaEstacaoNoCaminho()
        {
            var s = new Sim();
            s.Buy(Upgrade.Anvil2); s.Buy(Upgrade.Furnace2);
            Box b = s.FurnaceB.Body;
            s.Player.Pos = new V2(3.2f, 5.6f);   // a esquerda da Fornalha 2, indo para a direita e um pouco para cima
            Run(s, 1.5f, 1f, 0.4f);
            Assert.Greater(s.Player.Pos.X, b.Pos.X + b.Half.X, "deslizou pela face e contornou a quina (sem deslize parava em x 3,6)");
            Assert.GreaterOrEqual(b.Dist(s.Player.Pos), Balance.CharRadius - 1e-3f);
            // ajudante encostado embaixo do centro da fornalha, alvo exatamente atras dela: contorna e chega
            var w = new Carrier { Pos = new V2(b.Pos.X, b.Pos.Y - b.Half.Y - Balance.CharRadius), Speed = Balance.WorkerSpeed };
            V2 target = new V2(b.Pos.X, b.Pos.Y + b.Half.Y + 1f);
            bool arrived = false;
            for (int i = 0; i < 300 && !arrived; i++)
            {
                arrived = s.MoveTowards(w, target, Dt, Balance.WorkerReach);
                Assert.GreaterOrEqual(Inside(s, w.Pos), Balance.CharRadius - 1e-3f, "ajudante nunca dentro de um corpo");
            }
            Assert.IsTrue(arrived, $"ajudante contornou a fornalha e chegou (parou em {w.Pos})");
        }

        [Test]
        public void Bocas_EntradaSoDeposita_SaidaSoRecolhe()
        {
            var s = new Sim();
            s.FurnaceA.Out = 2;
            Hold(s, Item.Ore, 3);
            StandAt(s, s.FurnaceA.InAt, 2f);
            Assert.AreEqual((0, 2), (s.Player.Count, s.FurnaceA.Out), "entrada: depositou os 3 e nao recolheu os 2 lingotes prontos");
            int fornalha = s.FurnaceA.In + (s.FurnaceA.Busy ? 1 : 0);
            Hold(s, Item.Ore, 3);
            StandAt(s, s.FurnaceA.OutAt, 1f);
            Assert.AreEqual(3, s.Player.Held[(int)Item.Ore], "saida: com minerio na mao nao deposita");
            Assert.AreEqual(fornalha, s.FurnaceA.In + (s.FurnaceA.Busy ? 1 : 0));
            Assert.AreEqual((2, 0), (s.Player.Held[(int)Item.Ingot], s.FurnaceA.Out), "saida: recolhe com minerio na mao (FASE7, pilha mista)");
            StandAt(s, s.FurnaceA.Pos + new V2(3f, 0f), 0.1f);
            Assert.AreEqual(5, s.Player.Count, "longe das bocas nada acontece");
        }

        /// <summary>Tabela das bocas (API da view): lado da ENTRADA por estacao; a SAIDA e' o oposto. Deposito/balcoes: zona unica.</summary>
        [Test]
        public void Bocas_Tabela_LadosOpostos_ForaDeCorposEPads()
        {
            var s = AllBodies();
            var side = new Dictionary<Station, V2>
            {
                { s.Deposit, new V2(0, 1) }, { s.FurnaceA, new V2(-1, 0) }, { s.FurnaceB, new V2(-1, 0) }, { s.AnvilA, new V2(-1, 0) }, { s.AnvilB, new V2(-1, 0) },
                { s.ShieldBench, new V2(-1, 0) }, { s.ToolBench, new V2(-1, 0) }, { s.Counter, new V2(0, -1) }, { s.JewelBench, new V2(-1, 0) }, { s.JewelShop, new V2(0, -1) },
            };
            foreach (Station st in s.Stations)
            {
                V2 d = st.InAt - st.Pos;
                Assert.AreEqual((Math.Sign(side[st].X), Math.Sign(side[st].Y)), (Math.Sign(Math.Round(d.X, 3)), Math.Sign(Math.Round(d.Y, 3))), st.Name + ": lado da entrada");
                if (st.Produces) Assert.IsTrue(At(st.OutAt - st.Pos, d * -1f), st.Name + ": saida no lado oposto");
                else Assert.IsTrue(At(st.OutAt, st.InAt), st.Name + ": zona unica");
                foreach (V2 m in new[] { st.InAt, st.OutAt })
                {
                    Assert.AreEqual(Balance.CharRadius, st.Body.Dist(m), 1e-4f, st.Name + ": boca = encostado na face");
                    Assert.GreaterOrEqual(Inside(s, m), Balance.CharRadius - 1e-4f, st.Name + ": boca fora de outro corpo");
                    foreach (Pad p in PadsInWorld(s))
                        if (!At(p.Pos, st.Pos)) Assert.GreaterOrEqual(V2.Dist(m, p.Pos), Balance.MouthRadius + Balance.PadRadius, $"{st.Name}: zona da boca encosta no pad {p.Chain[0]}");
                    foreach (Station o in s.Stations)
                        if (o != st) Assert.Greater(Math.Min(V2.Dist(m, o.InAt), V2.Dist(m, o.OutAt)), 2f * Balance.MouthRadius, $"{st.Name}: zona sobreposta a de {o.Name}");
                }
            }
        }

        [Test]
        public void Fila_VagasEPadsEBaus_ForaDosCorpos()
        {
            var s = AllBodies();
            // vaga = onde o cliente pisa: nao pode cair DENTRO de um corpo (a da compra direta do balcao, x 9,6, fica na quina da parede)
            bool Within(V2 p) => s.Solids.Exists(b => Math.Abs(p.X - b.Pos.X) < b.Half.X && Math.Abs(p.Y - b.Pos.Y) < b.Half.Y);
            for (int i = 0; i <= Balance.QueueCapMax; i++) Assert.IsFalse(Within(s.ClientSlot(i)), $"vaga {i} do balcao");
            for (int i = 0; i <= Balance.JewelQueueCapUp; i++) Assert.IsFalse(Within(s.JewelSlot(i)), $"vaga {i} da loja de joias");
            foreach (Pad p in PadsInWorld(s))   // pad no lugar de estacao some quando ela abre; pad so de menu nunca aparece (o da Vitrine fica embaixo do balcao de 8 vagas)
                if (s.Stations.TrueForAll(st => !At(st.Pos, p.Pos))) Assert.GreaterOrEqual(Inside(s, p.Pos), Balance.CharRadius, $"pad {p.Chain[0]} pisavel");
            foreach (Chest c in s.Chests) Assert.GreaterOrEqual(Inside(s, c.Pos), Balance.CharRadius, "bau pisavel");
            Assert.GreaterOrEqual(Inside(s, Sim.HireSpot), Balance.CharRadius, "ajudante nasce fora dos corpos");
        }

        [Test]
        public void Ajudantes_CompletamCiclos_10Min_SemTravar()
        {
            var s = CompleteProduction();
            s.Player.Pos = new V2(4.5f, 3.5f);
            int n = s.Workers.Count;
            var moved = new int[n];
            var last = new int[n];
            for (int k = 0; k < n; k++) last[k] = s.Workers[k].Count;
            int sales = s.Sales;
            for (int minute = 1; minute <= 10; minute++)
            {
                Array.Clear(moved, 0, n);
                for (float t = 0f; t < 60f; t += Dt)
                {
                    s.Tick(Dt, 0f, 0f);
                    for (int k = 0; k < n; k++)
                    {
                        Carrier w = s.Workers[k];
                        if (w.Count != last[k]) { moved[k]++; last[k] = w.Count; }
                        Assert.GreaterOrEqual(Inside(s, w.Pos), Balance.CharRadius - 1e-3f, $"ajudante {k} (papel {w.Role}) dentro de um corpo");
                    }
                }
                for (int k = 0; k < n; k++) Assert.Greater(moved[k], 4, $"minuto {minute}: ajudante {k} (papel {s.Workers[k].Role}) travou");
            }
            Assert.Greater(s.Sales - sales, 250, "jogador parado: os ajudantes fecham o ciclo e vendem (FASE7, sem compra direta: 288; antes > 300)");
        }

        [Test]
        public void Save_JogadorDentroDeUmCorpo_SaiPelaBorda()
        {
            // save de antes da fisica: o jogador parado no centro da fornalha (antes era o lugar de interagir)
            const string old = "v=1\nt=600\ngold=190\nup=11111111000000000000000\npx=1.50,5.50\n";
            Sim s = Sim.Load(old);
            Assert.GreaterOrEqual(Inside(s, s.Player.Pos), Balance.CharRadius - 1e-3f, $"saiu do corpo (em {s.Player.Pos})");
            Assert.AreEqual(1.5f, s.Player.Pos.X, 1e-3f, "sai pelo lado mais perto (empate: por baixo)");
            Assert.AreEqual(s.Save(1), Sim.Load(s.Save(1)).Save(1), "save estavel depois de sair");
        }

        // ------------------------------------------------------------------ fase 4: Mineiro + Joalheiro 2 (docs/FASE4_MINERIO.md)

        [Test]
        public void Requisitos_Joalheiro_Mineiro_Joalheiro2_ProdutivosAnexados()
        {
            Assert.AreEqual(((int)Upgrade.Jeweler, (int)Upgrade.Miner), (Upgrades.All[(int)Upgrade.Miner].Requires, Upgrades.All[(int)Upgrade.Jeweler2].Requires));
            Assert.IsFalse(Upgrades.IsLuxury((int)Upgrade.Miner) || Upgrades.IsLuxury((int)Upgrade.Jeweler2), "produtivos, mesmo com ID depois dos luxos");
            Assert.AreEqual((23, 24), ((int)Upgrade.Miner, (int)Upgrade.Jeweler2), "anexados ao fim do enum");
            Assert.AreEqual((20, 21), (PadFor(new Sim(), Upgrade.Miner).Slot, PadFor(new Sim(), Upgrade.Jeweler2).Slot), "pads anexados ao fim");
            var s = Rich();
            s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry);
            Assert.AreEqual((-1, -1), (PadFor(s, Upgrade.Miner).Current(s), PadFor(s, Upgrade.Jeweler2).Current(s)), "sem Joalheiro: os dois escondidos");
            s.Buy(Upgrade.Jeweler);
            Assert.AreEqual(((int)Upgrade.Miner, -1), (PadFor(s, Upgrade.Miner).Current(s), PadFor(s, Upgrade.Jeweler2).Current(s)), "Joalheiro abre o Mineiro");
            s.Buy(Upgrade.Miner);
            Assert.AreEqual((-1, (int)Upgrade.Jeweler2), (PadFor(s, Upgrade.Miner).Current(s), PadFor(s, Upgrade.Jeweler2).Current(s)), "Mineiro abre o Joalheiro 2");
            Assert.AreEqual(new V2(2.8f, 0.6f).ToString(), PadFor(s, Upgrade.Miner).Pos.ToString(), "embaixo do Deposito, fora da rota ate a Fornalha");
            Assert.AreEqual(new V2(12f, 3.5f).ToString(), PadFor(s, Upgrade.Jeweler2).Pos.ToString(), "rua lateral, embaixo da Joalheria");
        }

        [Test]
        public void Luxo_EsperaOs26Produtivos_InclusiveParEVagasDoBalcao()
        {
            var s = Rich();
            for (int i = 0; i <= (int)Upgrade.JewelVitrine; i++) s.Buy((Upgrade)i);
            Assert.IsFalse(s.ProductionComplete, "os 20 de antes nao bastam");
            Assert.AreEqual(-1, PadFor(s, Upgrade.WorkshopFacade).Current(s));
            Assert.IsFalse(s.Buy(Upgrade.WorkshopFacade));
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.Counter5), s.OfflineMaxGold(), "teto offline: 2x o menor travado (Balcao 5)");
            BuyCounterTiers(s);
            Assert.IsFalse(s.ProductionComplete || s.Buy(Upgrade.WorkshopFacade), "as 4 vagas sem o par tambem nao");
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.Miner), s.OfflineMaxGold(), "teto offline: 2x o menor a venda (Mineiro; o Joalheiro 2 exige o Mineiro)");
            s.Buy(Upgrade.Miner);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.Jeweler2), s.OfflineMaxGold(), "com o Mineiro: 2x o Joalheiro 2");
            Assert.IsFalse(s.ProductionComplete || s.Buy(Upgrade.WorkshopFacade), "so o Mineiro tambem nao");
            s.Buy(Upgrade.Jeweler2);
            Assert.IsTrue(s.ProductionComplete);
            Assert.AreEqual(18000, s.OfflineMaxGold(), "producao completa: 2x o produtivo mais caro (Vitrine de joias)");
            Assert.AreEqual((int)Upgrade.WorkshopFacade, PadFor(s, Upgrade.WorkshopFacade).Current(s));
            Assert.IsTrue(s.Buy(Upgrade.WorkshopFacade));
        }

        [Test]
        public void Mineiro_Papel0Extra_SoCarregaMinerioAsFornalhas()
        {
            var s = new Sim();
            s.Buy(Upgrade.Furnace2); s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry); s.Buy(Upgrade.Jeweler);
            s.Buy(Upgrade.Miner);
            CollectionAssert.AreEqual(new[] { 3, 0 }, Roles(s), "Mineiro = papel 0, mesmo sem o Ajudante 1");
            s.Buy(Upgrade.Helper1);
            CollectionAssert.AreEqual(new[] { 3, 0, 0 }, Roles(s), "com o Ajudante 1: dois papeis 0");
            Carrier miner = s.Workers[1];
            Assert.AreEqual((Balance.WorkerSpeed, Balance.WorkerCap), (miner.Speed, miner.Cap));
            int ore = 0, wrong = 0;
            for (float t = 0f; t < 60f; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events)
                {
                    if (e.B != 0 || (e.Kind != Ev.Picked && e.Kind != Ev.Deposited)) continue;
                    bool ok = e.A == (int)Item.Ore && (e.Kind == Ev.Picked ? At(e.Pos, s.Deposit.Pos) : At(e.Pos, s.FurnaceA.Pos) || At(e.Pos, s.FurnaceB.Pos));
                    if (ok) ore++; else wrong++;
                }
                Assert.IsTrue(miner.Count == 0 || miner.Item == Item.Ore, "o Mineiro so carrega minerio");
            }
            Assert.AreEqual(0, wrong, "papel 0 nunca pega/entrega outra coisa");
            Assert.Greater(ore, 30, "os dois papeis 0 levaram minerio do deposito as fornalhas");
            s.Buy(Upgrade.HelperSpeed);
            Assert.AreEqual((Balance.WorkerSpeedUp, Balance.WorkerCapUp), (miner.Speed, miner.Cap), "Ajudantes ageis valem para o Mineiro");
            CollectionAssert.AreEqual(new[] { 0, 3, 0 }, Roles(Sim.Load(s.Save(1))), "do save: um ajudante por upgrade de contratacao");
        }

        [Test]
        public void Joalheiro2_Papel3Extra_AbasteceAJoalheriaELevaJoias()
        {
            var s = new Sim();
            s.Buy(Upgrade.SideCorridor); s.Buy(Upgrade.Jewelry); s.Buy(Upgrade.Jeweler); s.Buy(Upgrade.Miner); s.Buy(Upgrade.Jeweler2);
            CollectionAssert.AreEqual(new[] { 3, 0, 3 }, Roles(s), "Joalheiro 2 = 2o papel 3");
            var moves = new int[3]; var last = new int[3];
            for (float t = 0f; t < 120f; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                for (int k = 0; k < 3; k++)
                {
                    Carrier w = s.Workers[k];
                    if (w.Count != last[k]) { moves[k]++; last[k] = w.Count; }
                    if (w.Role == 3) Assert.IsTrue(w.Count == 0 || w.Item == Item.Ingot || w.Item == Item.Jewel, "joalheiro so leva lingote e joia");
                }
            }
            Assert.Greater(moves[0], 5, "Joalheiro trabalha");
            Assert.Greater(moves[2], 5, "Joalheiro 2 trabalha");
            Assert.Greater(s.SoldItems[(int)Item.Jewel], 3, "joias chegaram a loja e foram vendidas, jogador parado");
            Assert.AreEqual(0, s.ToolBench.In + s.AnvilB.In, "nao abastece outras bancadas");
        }

        [Test]
        public void Save_Antigo23Flags_CarregaComOParNovoTravadoEDevolveParcialDoLuxo()
        {
            // save da v0.3: 20 produtivos + Fachada + Piso, 20 pads, Joalheria real com 750 pagos
            const string v03 = "v=1\nt=4800\ngold=9000\nup=11111111111111111111110\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,750,\nstock=2,3,1,4\nsold=300,60,40,90\nms=2,2,2,2\nema=17\nsaved=900\nclaim=800\npx=12,9\n";
            Sim s = null;
            Assert.DoesNotThrow(() => s = Sim.Load(v03));
            Assert.AreEqual((22, 22), (s.UpgradesBought, s.Pads.Count));
            Assert.IsTrue(s.Bought[(int)Upgrade.WorkshopFacade] && s.Bought[(int)Upgrade.WorkshopFloor], "luxo comprado continua comprado (decoracao fica)");
            Assert.IsFalse(s.Bought[(int)Upgrade.Miner] || s.Bought[(int)Upgrade.Jeweler2]);
            Assert.IsFalse(s.ProductionComplete, "o par novo reabre a producao");
            Assert.AreEqual(((int)Upgrade.Miner, -1, -1), (PadFor(s, Upgrade.Miner).Current(s), PadFor(s, Upgrade.Jeweler2).Current(s), PadFor(s, Upgrade.JewelryDecor).Current(s)));
            Assert.AreEqual((9750, 0), (s.Gold, PadFor(s, Upgrade.JewelryDecor).Paid), "o parcial do luxo escondido volta para o ouro, nao some");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, Roles(s));
            StringAssert.Contains("up=11111111111111111111110000000\n", s.Save(901));
        }

        [Test]
        public void Pad_MarteloVeloz_LongeDaBigornaDasBocasEDasLinhas()
        {
            var s = AllBodies();
            Pad hammer = PadFor(s, Upgrade.HammerSpeed);
            Assert.AreEqual((10, new V2(8.4f, 3.4f).ToString()), (hammer.Slot, hammer.Pos.ToString()), "mesmo indice (save), posicao nova");
            foreach (Station st in s.Stations)
                Assert.GreaterOrEqual(V2.Dist(hammer.Pos, st.Pos), 1.5f, $"rotulo do pad longe do rotulo de {st.Name} (era 1,0 m da Bigorna)");
            // fora das linhas boca -> boca (por onde o jogador anda entre estacoes): a sugestao (8,4; 7,5) ficava entre as saidas de Ferramentas e Escudos
            var mouths = new List<V2>();
            foreach (Station st in s.Stations) { mouths.Add(st.InAt); mouths.Add(st.OutAt); }
            for (int i = 0; i < mouths.Count; i++)
                for (int j = i + 1; j < mouths.Count; j++)
                    Assert.Greater(SegDist(hammer.Pos, mouths[i], mouths[j]), Balance.PadRadius, $"pad na linha {mouths[i]} -> {mouths[j]}");
        }

        /// <summary>FASE5 §2 (decisao do coordenador): parede direita da oficina solida, so passa pela porta lateral e pelo arco.</summary>
        [Test]
        public void Parede_SoPassaPelaPortaEPeloArco_AjudanteUsaAAbertura()
        {
            Assert.AreEqual((9.0f, 9.6f), (Balance.SideWallX0, Balance.SideWallX1));
            CollectionAssert.AreEqual(new[] { 5.6f, 7.4f, 11.6f, 13.4f }, Balance.SideWallOpenings, "porta lateral e arco (pares y0, y1)");
            var s = new Sim();
            s.Buy(Upgrade.SideCorridor);
            foreach (float y in new[] { 2f, 4f, 9.5f, 11f, 13.6f })   // fora das aberturas: para na parede
            {
                s.Player.Pos = new V2(7.5f, y);
                Run(s, 2f, 1f, 0f);
                Assert.AreEqual(Balance.SideWallX0 - Balance.CharRadius, s.Player.Pos.X, 0.01f, $"y {y}: parede solida");
            }
            foreach (float y in new[] { 6.5f, 12.5f })   // nas aberturas: atravessa
            {
                s.Player.Pos = new V2(7.5f, y);
                Run(s, 2f, 1f, 0f);
                Assert.Greater(s.Player.Pos.X, 12f, $"y {y}: passou pela abertura");
            }
            // ajudante da oficina para a rua com o alvo atras da parede: vai pela abertura de menor caminho e chega
            foreach (V2 from in new[] { new V2(8.3f, 1.5f), new V2(8.0f, 10f), new V2(4.5f, 12f) })
            {
                var w = new Carrier { Pos = from, Speed = Balance.WorkerSpeed };
                V2 target = new V2(12f, 3.5f);
                bool arrived = false; float minY = 99f, maxY = -99f;
                for (int i = 0; i < 900 && !arrived; i++)
                {
                    arrived = s.MoveTowards(w, target, Dt, Balance.WorkerReach);
                    if (Math.Abs(w.Pos.X - 9.3f) < 0.3f) { minY = Math.Min(minY, w.Pos.Y); maxY = Math.Max(maxY, w.Pos.Y); }
                    Assert.GreaterOrEqual(Inside(s, w.Pos), Balance.CharRadius - 1e-3f);
                }
                Assert.IsTrue(arrived, $"de {from}: chegou (parou em {w.Pos})");
                Assert.IsTrue(minY >= 5.6f && maxY <= 7.4f, $"de {from}: cruzou x 9,3 pela porta lateral (y {minY}-{maxY})");
            }
            Assert.AreEqual(new V2(9.3f, 11.95f).ToString(), Sim.Via(new V2(4.5f, 12.35f), new V2(12f, 10.85f)).ToString(), "balcao -> loja de joias: pelo arco");
            Assert.AreEqual(new V2(9.3f, 5.95f).ToString(), Sim.Via(new V2(5.4f, 5.5f), new V2(12f, 5.85f)).ToString(), "fornalha 2 -> joalheria: pela porta");
            Assert.AreEqual(new V2(12f, 5.85f).ToString(), Sim.Via(new V2(10f, 2f), new V2(12f, 5.85f)).ToString(), "mesmo lado: direto");
            // Leva 11: quem chega ao ponto de passagem com x 9,29999 (arredondamento) nao pode mirar o proprio lugar (travava ali)
            Assert.AreEqual(new V2(11.1f, 6.5f).ToString(), Sim.Via(new V2(9.2999f, 6.29f), new V2(11.1f, 6.5f)).ToString(), "no meio do vao: direto");
        }

        // ------------------------------------------------------------------ fase 6: menu inferior de melhorias (docs/FASE6_MENU_MELHORIAS.md)

        static readonly Upgrade[] Menu =
        {
            Upgrade.FurnaceSpeed1, Upgrade.PlayerCapacity, Upgrade.FurnaceSpeed2, Upgrade.PlayerSpeed, Upgrade.CounterCapacity,
            Upgrade.HelperSpeed, Upgrade.HammerSpeed, Upgrade.JewelSpeed, Upgrade.JewelVitrine,
        };

        /// <summary>Pads que ainda podem aparecer no mundo: cadeia com pelo menos um upgrade fora do menu.</summary>
        static List<Pad> PadsInWorld(Sim s) => s.Pads.FindAll(p => Array.Exists(p.Chain, u => !Upgrades.All[(int)u].InMenu));

        /// <summary>Estado derivado dos flags (o que um upgrade pode mudar), para comparar duas compras.</summary>
        static string Derived(Sim s) => string.Join("|", s.Player.Speed, s.Player.Cap, s.CounterCap, s.QueueCap, s.JewelQueueCap, s.HasConveyor, s.MaxX,
            s.PriceOf(Item.Jewel), s.ClientInterval(Item.Sword), s.ClientInterval(Item.Jewel), s.UpgradesBought, s.Solids.Count,
            string.Join(",", s.Stations.ConvertAll(st => st.Time + "/" + st.Unlocked)), string.Join(",", s.Workers.ConvertAll(w => w.Role + "/" + w.Speed + "/" + w.Cap)),
            string.Join("", Array.ConvertAll(s.Bought, b => b ? "1" : "0")));

        /// <summary>Compra (direto) a cadeia de pre-requisitos de `u`, sem `u`.</summary>
        static void BuyRequirements(Sim s, Upgrade u)
        {
            int req = Upgrades.All[(int)u].Requires;
            if (req < 0) return;
            BuyRequirements(s, (Upgrade)req);
            s.Buy((Upgrade)req);
        }

        [Test]
        public void Menu_OsNoveDoContrato_SemPadVisivel_PadsContinuamNaLista()
        {
            var menu = new List<Upgrade>(Menu); menu.AddRange(CounterTiers);
            CollectionAssert.AreEquivalent(menu, Array.ConvertAll(Array.FindAll(Upgrades.All, d => d.InMenu), d => d.Id), "Fole, Fole duplo, Mochila, Botas, Martelo veloz, Vitrine, Ajudantes ageis, Lupa, Vitrine de joias + as 4 vagas do balcao (FASE7)");
            foreach (Upgrade u in CounterTiers) Assert.IsNull(PadFor(new Sim(), u), u + ": nasceu no menu, sem pad");
            foreach (Upgrade u in Menu) Assert.IsFalse(Upgrades.IsLuxury((int)u), u + " e' produtivo");
            var s = Rich();
            Assert.AreEqual((22, 14), (s.Pads.Count, PadsInWorld(s).Count), "os 8 pads do menu ficam na lista (save por slot), invisiveis");
            foreach (Upgrade u in Menu) Assert.IsNotNull(PadFor(s, u), u + ": slot antigo preservado");
            // em toda a progressao (produtivos em ordem de tier, depois luxo), nenhum pad mostra upgrade do menu
            var order = new List<int>();
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < Upgrades.Count; i++) if (Upgrades.IsLuxury(i) == (pass == 1)) order.Add(i);
            foreach (int i in order)
            {
                foreach (Pad p in s.Pads)
                {
                    int cur = p.Current(s);
                    Assert.IsFalse(cur >= 0 && Upgrades.All[cur].InMenu, $"pad {p.Slot} mostra {(Upgrade)cur}, que e' do menu");
                    if (!PadsInWorld(s).Contains(p)) Assert.AreEqual(-1, cur, $"pad {p.Slot} so de menu fica invisivel para sempre");
                }
                s.Buy((Upgrade)i);
            }
            // parado com muito ouro em cima do pad antigo do Fole: nao drena, nao compra
            var t = Rich();
            Pad fole = PadFor(t, Upgrade.FurnaceSpeed1);
            StandAt(t, fole.Pos, 2f);
            Assert.AreEqual((100000, 0, false), (t.Gold, fole.Paid, t.Bought[(int)Upgrade.FurnaceSpeed1]));
            Assert.IsNull(t.PadAt(fole.Pos));
        }

        [Test]
        public void TryBuyMenu_Ouro_PreRequisito_Idempotente_PadRecusado_Evento()
        {
            var s = new Sim();
            int fole = Upgrades.Cost(Upgrade.FurnaceSpeed1);
            s.Gold = fole - 1;
            Assert.IsFalse(s.TryBuyMenu(Upgrade.FurnaceSpeed1), "ouro insuficiente");
            Assert.AreEqual((fole - 1, false, 0), (s.Gold, s.Bought[(int)Upgrade.FurnaceSpeed1], s.Events.Count));
            s.Gold = 100000;
            Assert.IsFalse(s.TryBuyMenu(Upgrade.FurnaceSpeed2), "Fole duplo sem o Fole");
            Assert.IsFalse(s.TryBuyMenu(Upgrade.HelperSpeed), "Ajudantes ageis sem o Ajudante");
            Assert.IsFalse(s.TryBuyMenu(Upgrade.JewelSpeed) || s.TryBuyMenu(Upgrade.JewelVitrine), "Lupa e Vitrine de joias sem a Joalheria");
            for (int i = 0; i < Upgrades.Count; i++)
                if (!Upgrades.All[i].InMenu) Assert.IsFalse(s.TryBuyMenu((Upgrade)i), "upgrade de pad (ou luxo) nao se compra no menu: " + (Upgrade)i);
            Assert.AreEqual((100000, 0, 0), (s.Gold, s.UpgradesBought, s.Events.Count), "recusa nao muda nada");

            s.Player.Pos = new V2(3f, 3f);
            s.Gold = fole;
            Assert.IsTrue(s.TryBuyMenu(Upgrade.FurnaceSpeed1), "ouro exato compra");
            Assert.AreEqual((0, true, 1, s.Time), (s.Gold, s.Bought[(int)Upgrade.FurnaceSpeed1], s.UpgradesBought, s.UpgradeTime[(int)Upgrade.FurnaceSpeed1]), "cobra o preco inteiro na hora, sem dreno");
            Assert.AreEqual(1, Count(s, Ev.Bought));
            SimEvent e = s.Events.Find(x => x.Kind == Ev.Bought);
            Assert.AreEqual(((int)Upgrade.FurnaceSpeed1, fole), (e.A, e.B), "A = upgrade, B = preco");
            Assert.IsTrue(At(e.Pos, s.Player.Pos), "posicao = jogador, nao o pad antigo");

            s.Events.Clear(); s.Gold = 100000;
            Assert.IsFalse(s.TryBuyMenu(Upgrade.FurnaceSpeed1), "ja comprado: no-op");
            Assert.AreEqual((100000, 1, 0), (s.Gold, s.UpgradesBought, s.Events.Count));
            Assert.IsTrue(s.TryBuyMenu(Upgrade.FurnaceSpeed2), "pre-requisito comprado abre o proximo");
            Assert.AreEqual(100000 - Upgrades.Cost(Upgrade.FurnaceSpeed2), s.Gold);
            Run(s, 1f);
            Assert.AreEqual(100000 - Upgrades.Cost(Upgrade.FurnaceSpeed2), s.Gold, "nada drena depois (sem pad parcial)");
        }

        [Test]
        public void TryBuyMenu_AplicaOMesmoEfeitoDaCompraPeloPad()
        {
            foreach (Upgrade u in Menu)
            {
                Sim viaMenu = Rich(), viaPad = Rich();
                BuyRequirements(viaMenu, u); BuyRequirements(viaPad, u);
                string before = Derived(viaMenu);
                Assert.IsTrue(viaMenu.TryBuyMenu(u), u + " compravel no menu");
                Assert.IsTrue(viaPad.Buy(u), u + " pelo caminho do pad (Drain -> Buy)");
                Assert.AreNotEqual(before, Derived(viaMenu), u + ": efeito aplicado");
                Assert.AreEqual(Derived(viaPad), Derived(viaMenu), u + ": mesmo efeito do pad");
                Assert.AreEqual(viaPad.Gold - Upgrades.Cost(u), viaMenu.Gold, u + ": cobra o preco uma vez");
                Assert.AreEqual(Derived(viaMenu), Derived(Sim.Load(viaMenu.Save(1))), u + ": re-derivado do save");
            }
        }

        [Test]
        public void Save_Antigo_ParcialEmPadQueVirouMenu_VoltaParaOOuro()
        {
            // save de antes da Leva 11: parcial em 8 pads que viraram menu (slots 0, 6, 7, 8, 10, 11, 15, 16) e na Esteira (slot 9, continua pad)
            const string old = "v=1\nt=1500\ngold=1000\nup=0010000000000001100000000\npads=40,0,0,0,0,0,100,120,30,55,900,600,0,0,0,5000,8000,0,0,0,0,0,\npx=4.5,3.5\n";
            Sim s = Sim.Load(old);
            Assert.AreEqual(1000 + 40 + 100 + 120 + 30 + 900 + 600 + 5000 + 8000, s.Gold, "o parcial dos pads do menu volta inteiro para o ouro");
            Assert.AreEqual(55, PadFor(s, Upgrade.Conveyor).Paid, "o parcial de pad que continua no mundo fica no pad");
            foreach (Upgrade u in Menu) Assert.AreEqual(0, PadFor(s, u).Paid, u + ": pad do menu zerado");
            string again = s.Save(2);
            StringAssert.Contains("pads=0,0,0,0,0,0,0,0,0,55,0,0,0,0,0,0,0,0,0,0,0,0,\n", again);
            Assert.AreEqual(again, Sim.Load(again).Save(2), "save estavel: o ouro devolvido nao se repete no proximo Load");
            Assert.IsTrue(s.TryBuyMenu(Upgrade.FurnaceSpeed1), "o ouro devolvido compra no menu");
            Sim rich = Sim.Load("gold=2147483000\npads=5000,\n");
            Assert.AreEqual((int.MaxValue, 0), (rich.Gold, rich.Pads[0].Paid), "teto int.MaxValue, sem estouro");
        }

        [Test]
        public void Dica_BuyMenu_QuandoOMaisBaratoPagavelEDoMenu_BuyPadQuandoEDoPad()
        {
            var s = new Sim();
            int fole = Upgrades.Cost(Upgrade.FurnaceSpeed1), anvil = Upgrades.Cost(Upgrade.Anvil2);
            Assert.Less(fole, anvil, "premissa: Fole (menu) 50 < 2a bigorna (pad) 65");
            s.Gold = fole - 1;
            Assert.AreEqual(Hint.GrabOre, s.CurrentHint(), "nada pagavel");
            s.Gold = anvil;
            Assert.AreEqual((Hint.BuyMenu, (int)Upgrade.FurnaceSpeed1), (s.CurrentHint(), s.HintArg), "os dois pagaveis: o mais barato e' do menu");
            Assert.AreEqual("Fole", Upgrades.All[s.HintArg].Name, "HintArg = indice do upgrade (a view le Upgrades.All)");
            s.Pads[2].Paid = anvil - fole;   // empate: 2a bigorna com o mesmo restante do Fole
            Assert.AreEqual((Hint.BuyMenu, (int)Upgrade.FurnaceSpeed1), (s.CurrentHint(), s.HintArg), "empate: o menu, que nao precisa andar");
            s.Pads[2].Paid = anvil - fole + 1;
            Assert.AreEqual((Hint.BuyPad, 2, -1), (s.CurrentHint(), s.HintArg, s.CheapestAffordableMenu()), "pad com restante menor: BuyPad (slot)");
            s.Pads[2].Paid = 0;
            s.Buy(Upgrade.FurnaceSpeed1);
            s.Gold = 100000;
            Assert.AreEqual((Hint.BuyPad, 2), (s.CurrentHint(), s.HintArg), "Fole comprado: a 2a bigorna (65) e' a mais barata");
            s.Buy(Upgrade.Anvil2); s.Buy(Upgrade.Helper1); s.Buy(Upgrade.Shields);   // pad mais barato agora: Ajudante 2 (185) > Mochila (145)
            Hold(s, Item.Ore, 2); s.Chests[0].State = 1;
            Assert.AreEqual((Hint.BuyMenu, (int)Upgrade.PlayerCapacity), (s.CurrentHint(), s.HintArg), "mesma prioridade do pad: na frente do bau e do que carrega");
            s.Gold = 0;
            Assert.AreEqual(Hint.OpenChest, s.CurrentHint());
        }

        [Test]
        public void Bot_CompraNoMenuSemAndar_MesmaPoliticaDoPad()
        {
            foreach (Bot bot in new[] { new Bot(), Bot.Ideal() })
            {
                var s = new Sim();
                V2 start = s.Player.Pos;
                s.Gold = Upgrades.Cost(Upgrade.FurnaceSpeed1);   // 50: so o Fole (menu) e' pagavel
                int ticks = 0, bought = 0;
                while (!s.Bought[(int)Upgrade.FurnaceSpeed1] && ticks < 90) { bot.Step(s, Dt); ticks++; bought += Count(s, Ev.Bought); }
                Assert.IsTrue(s.Bought[(int)Upgrade.FurnaceSpeed1], "comprou no menu");
                Assert.AreEqual((start.ToString(), 0, 1), (s.Player.Pos.ToString(), s.Gold, bought), "parado, sem ir ao pad antigo; Ev.Bought fica em Events depois do Step");
                Assert.LessOrEqual(ticks * Dt, bot.Reaction + 2f * Dt, "so o tempo de ler a tela (Reaction)");
            }
            // pad mais barato que o menu: o bot anda ate o pad, como antes
            var t = new Sim();
            t.Buy(Upgrade.FurnaceSpeed1);
            t.Gold = Upgrades.Cost(Upgrade.Anvil2);   // 65: 2a bigorna (pad); Mochila (menu) custa 145
            var b = new Bot();
            for (int i = 0; i < 300 && !t.Bought[(int)Upgrade.Anvil2]; i++) b.Step(t, Dt);
            Assert.IsTrue(t.Bought[(int)Upgrade.Anvil2], "foi ao pad e pagou");
            Assert.LessOrEqual(V2.Dist(t.Player.Pos, PadFor(t, Upgrade.Anvil2).Pos), Balance.PadRadius + Balance.CharRadius, "andou ate o pad");
        }

        [Test]
        public void Bocas_Laterais_FornalhaBigornaJoalheria_LongeDosPadsQueFicam()
        {
            var s = AllBodies();
            var expected = new (Station st, V2 inAt, V2 outAt)[]
            {
                (s.FurnaceA, new V2(0.6f, 5.5f), new V2(2.4f, 5.5f)), (s.AnvilA, new V2(0.6f, 9.5f), new V2(2.4f, 9.5f)), (s.JewelBench, new V2(11.1f, 6.5f), new V2(12.9f, 6.5f)),
            };
            var pads = PadsInWorld(s);
            foreach (var (st, inAt, outAt) in expected)
            {
                Assert.AreEqual((inAt.ToString(), outAt.ToString()), (st.InAt.ToString(), st.OutAt.ToString()), st.Name + ": entrada a esquerda, saida a direita (pilhas da view)");
                foreach (V2 m in new[] { st.InAt, st.OutAt })
                {
                    Assert.GreaterOrEqual(Inside(s, m), Balance.CharRadius - 1e-4f, st.Name + ": boca fora de parede, obstaculo e outra estacao");
                    foreach (Pad p in pads)
                        if (!At(p.Pos, st.Pos)) Assert.GreaterOrEqual(V2.Dist(m, p.Pos), 1.5f, $"{st.Name}: boca {m} perto do pad {p.Chain[0]} ({p.Pos})");
                    foreach (Chest c in s.Chests) Assert.GreaterOrEqual(V2.Dist(m, c.Pos), Balance.MouthRadius + Balance.PadRadius, st.Name + ": boca no bau");
                    for (int i = 0; i <= Balance.QueueCapMax; i++) Assert.GreaterOrEqual(V2.Dist(m, s.ClientSlot(i)), 2f, st.Name + ": boca na fila do balcao");
                    for (int i = 0; i <= Balance.JewelQueueCapUp; i++) Assert.GreaterOrEqual(V2.Dist(m, s.JewelSlot(i)), 2f, st.Name + ": boca na fila da loja de joias");
                    Assert.GreaterOrEqual(V2.Dist(m, Sim.HireSpot), 2f, st.Name + ": boca no ponto de contratacao");
                }
            }
            // os trajetos do inicio (deposito -> fornalha -> bigorna -> balcao, e o corredor da parede entre as duas entradas) nao
            // passam por cima de pad que fica no mundo: a Esteira saiu de (0,5; 7,8), no corredor, para (0,5; 11,2)
            Assert.AreEqual((9, new V2(0.5f, 11.2f).ToString()), (PadFor(s, Upgrade.Conveyor).Slot, PadFor(s, Upgrade.Conveyor).Pos.ToString()), "Esteira: mesmo slot (save), lugar novo");
            var trips = new[] { (s.Deposit.OutAt, s.FurnaceA.InAt), (s.FurnaceA.InAt, s.AnvilA.InAt), (s.FurnaceA.OutAt, s.AnvilA.InAt), (s.FurnaceA.OutAt, s.AnvilB.InAt), (s.AnvilA.OutAt, s.Counter.InAt) };
            foreach (Pad p in pads)
                if (s.Stations.TrueForAll(st => !At(st.Pos, p.Pos)))
                    foreach (var (a, b) in trips) Assert.Greater(SegDist(p.Pos, a, b), Balance.PadRadius, $"pad {p.Chain[0]} na linha {a} -> {b}");
            // funciona: lingote entra pela esquerda da Joalheria e a joia sai pela direita; a esteira segue sem zona
            var j = new Sim();
            j.Buy(Upgrade.SideCorridor); j.Buy(Upgrade.Jewelry); j.Buy(Upgrade.Conveyor);
            Hold(j, Item.Ingot, 2);
            StandAt(j, j.JewelBench.InAt, Balance.HammerTime0 * Balance.JewelTimeMul + 1f);
            Assert.AreEqual((0, 1), (j.Player.Count, j.JewelBench.Out), "depositou na entrada, a joia ficou na saida");
            StandAt(j, j.JewelBench.OutAt, 0.5f);
            Assert.AreEqual((Item.Jewel, 1), (j.Player.Item, j.Player.Count), "recolheu na saida");
            j.FurnaceA.Out = 2;
            StandAt(j, new V2(6f, 2f), 2.5f);
            Assert.AreEqual(0, j.FurnaceA.Out, "esteira Fornalha -> Bigorna sem ninguem nas bocas");
        }

        // ------------------------------------------------------------------ FASE7: balcao evolutivo e carga no save (docs/FASE7_CARGA_BALCAO.md)

        static readonly Upgrade[] CounterTiers = { Upgrade.Counter5, Upgrade.Counter6, Upgrade.Counter7, Upgrade.Counter8 };

        static void BuyCounterTiers(Sim s) { foreach (Upgrade u in CounterTiers) s.Buy(u); }

        [Test]
        public void Balcao_4a8Vagas_EmCadeiaNoMenu_ExigeVitrine_VitrineSoEstoqueEClientes()
        {
            Assert.AreEqual((25, 26, 27, 28), ((int)Upgrade.Counter5, (int)Upgrade.Counter6, (int)Upgrade.Counter7, (int)Upgrade.Counter8), "anexados ao fim do enum (save por indice)");
            int req = (int)Upgrade.CounterCapacity;
            foreach (Upgrade u in CounterTiers)
            {
                UpgradeDef d = Upgrades.All[(int)u];
                Assert.IsTrue(d.InMenu && !d.Luxury, u + ": menu e produtivo");
                Assert.AreEqual(req, d.Requires, u + ": em cadeia (o 1o exige a Vitrine)");
                req = (int)u;
            }
            CollectionAssert.AreEqual(new[] { 150, 175, 200, 225 }, Array.ConvertAll(CounterTiers, u => Upgrades.Cost(u)), "BALANCE §17");
            var s = Rich();
            Assert.AreEqual((Balance.QueueCap0, false), (s.QueueCap, s.MenuAvailable((int)Upgrade.Counter5)), "4 vagas; Balcao 5 so depois da Vitrine");
            float sword = s.ClientInterval(Item.Sword);
            Assert.IsTrue(s.TryBuyMenu(Upgrade.CounterCapacity));
            Assert.AreEqual((Balance.QueueCap0, Balance.CounterCap1), (s.QueueCap, s.CounterCap), "Vitrine: estoque 5 -> 10, a fila nao muda");
            Assert.AreEqual(sword * Balance.VitrineClientMul, s.ClientInterval(Item.Sword), 1e-4f, "Vitrine: clientes x0,7");
            Assert.AreEqual("Mais estoque e mais clientes", Upgrades.All[(int)Upgrade.CounterCapacity].Desc);
            Assert.IsFalse(s.TryBuyMenu(Upgrade.Counter6), "fora da ordem nao compra");
            for (int k = 0; k < CounterTiers.Length; k++)
            {
                Assert.IsTrue(s.TryBuyMenu(CounterTiers[k]), CounterTiers[k].ToString());
                Assert.AreEqual(Balance.QueueCap0 + 1 + k, s.QueueCap, "+1 vaga por evolucao");
            }
            Assert.AreEqual((Balance.QueueCapMax, Balance.QueueCapMax), (s.QueueCap, Sim.Load(s.Save(1)).QueueCap), "8 vagas, re-derivadas do save");
            int max = 0;
            for (float t = 0f; t < 60f; t += Dt) { s.Tick(Dt, 0f, 0f); max = Math.Max(max, s.Queue.Count); }
            Assert.AreEqual(Balance.QueueCapMax, max, "sem produto, 8 clientes esperam lado a lado");
            // save da v0.4.1 (25 flags) com a Vitrine: 4 vagas (a Vitrine nao da mais fila) e o Balcao 5 a venda
            Sim old = Sim.Load("v=1\nt=900\ngold=500\nup=1111111111100000000000000\n");
            Assert.AreEqual((Balance.QueueCap0, Balance.CounterCap1, true), (old.QueueCap, old.CounterCap, old.MenuAvailable((int)Upgrade.Counter5)));
            StringAssert.Contains("up=11111111111000000000000000000\n", old.Save(2));
        }

        [Test]
        public void Balcao_VagasCentradas_CorpoCresce_SemEngolirPadsBocasERotas()
        {
            var s = AllBodies();   // tudo comprado: luxo e decoracao inteira; o laco testa 4..8 vagas
            for (int n = Balance.QueueCap0; n <= Balance.QueueCapMax; n++)
            {
                for (int k = 0; k < CounterTiers.Length; k++) s.Bought[(int)CounterTiers[k]] = k < n - Balance.QueueCap0;
                s.Recompute();
                Box body = s.Counter.Body;
                Assert.AreEqual(n, s.QueueCap);
                Assert.AreEqual(Balance.SlotStep * n / 2f + Balance.CounterEnd, body.Half.X, 1e-4f, "largura = vagas x 0,85 + pontas");
                Assert.AreEqual(Balance.StationHalf.Y, body.Half.Y, 1e-4f, "profundidade nao muda");
                Assert.IsTrue(s.Solids.Exists(b => At(b.Pos, body.Pos) && Math.Abs(b.Half.X - body.Half.X) < 1e-4f), "o corpo solido acompanha");
                float mid = 0f;
                for (int i = 0; i < n; i++)
                {
                    V2 v = s.ClientSlot(i);
                    mid += v.X / n;
                    Assert.AreEqual(s.Counter.Pos.Y + 1f, v.Y, 1e-4f);
                    if (i > 0) Assert.AreEqual(Balance.SlotStep, v.X - s.ClientSlot(i - 1).X, 1e-4f);
                    Assert.That(v.X, Is.InRange(body.Pos.X - body.Half.X, body.Pos.X + body.Half.X), $"{n} vagas: vaga {i} em cima do balcao");
                }
                Assert.AreEqual(s.Counter.Pos.X, mid, 1e-3f, "vagas centradas no balcao");
                Assert.That(s.ClientSlot(n).X, Is.InRange(0.3f, Balance.SideWallX0), "a vaga de quem vai embora (depois da ultima) fica na oficina");
                Assert.GreaterOrEqual(Balance.SideWallX0 - (body.Pos.X + body.Half.X), 2f * Balance.CharRadius + 0.1f, "passa entre o balcao e a parede ate o arco");
                Assert.AreEqual(new V2(4.5f, 12.35f).ToString(), s.Counter.InAt.ToString(), "boca no meio da face de baixo");
                Assert.GreaterOrEqual(Inside(s, s.Counter.InAt), Balance.CharRadius - 1e-4f, "boca fora de corpo");
                foreach (Pad p in PadsInWorld(s))
                    if (s.Stations.TrueForAll(st => !At(st.Pos, p.Pos))) Assert.GreaterOrEqual(body.Dist(p.Pos), Balance.PadRadius, $"{n} vagas: o balcao engole o pad {p.Chain[0]}");
                foreach (Chest c in s.Chests) Assert.GreaterOrEqual(body.Dist(c.Pos), Balance.CharRadius, $"{n} vagas: bau {c.Index} pisavel");
                foreach (Station st in s.Stations)
                    if (st != s.Counter) foreach (V2 m in new[] { st.InAt, st.OutAt }) Assert.GreaterOrEqual(body.Dist(m), Balance.CharRadius, $"{st.Name}: boca dentro do balcao");
                foreach (Station st in new[] { s.AnvilA, s.AnvilB, s.ShieldBench, s.ToolBench })   // bancada -> balcao continua em linha reta
                    for (float t = 0f; t <= 1f; t += 0.05f)
                        Assert.GreaterOrEqual(body.Dist(st.OutAt + (s.Counter.InAt - st.OutAt) * t), Balance.CharRadius - 1e-3f, $"{n} vagas: rota {st.Name} -> balcao");
            }
        }

        /// <summary>FASE7 (benchmark A3): a carga da mao vai no save; reabrir nao perde o que o ferreiro e os ajudantes carregam.</summary>
        [Test]
        public void Save_CargaDaMao_JogadorMistaEAjudantes_IdaEVolta_SaveAntigoMaosVazias()
        {
            var s = new Sim();
            s.Buy(Upgrade.PlayerCapacity); s.Buy(Upgrade.Helper1); s.Buy(Upgrade.Helper2); s.Buy(Upgrade.Helper3);
            s.Player.Held[(int)Item.Ore] = 2; s.Player.Held[(int)Item.Ingot] = 6; s.Player.Held[(int)Item.Sword] = 1;
            s.Workers[0].Held[(int)Item.Ore] = 2; s.Workers[2].Held[(int)Item.Shield] = 1;
            string text = s.Save(10);
            StringAssert.Contains("hold=2,6,1,0,0,0\n", text);
            StringAssert.Contains("wk=0:2,0,0,0,0,0;1:0,0,0,0,0,0;2:0,0,0,1,0,0\n", text);
            Sim b = Sim.Load(text);
            CollectionAssert.AreEqual(s.Player.Held, b.Player.Held, "pilha mista do ferreiro volta");
            for (int k = 0; k < 3; k++) CollectionAssert.AreEqual(s.Workers[k].Held, b.Workers[k].Held, $"ajudante {k}");
            Assert.AreEqual(text, b.Save(10), "save estavel");
            // lixo: acima do teto corta no teto; ajudante so com o 1o tipo valido para o papel
            Sim g = Sim.Load(text.Replace("hold=2,6,1,0,0,0", "hold=99,-3,x,1").Replace("wk=0:2,0,0,0,0,0;1:0,0,0,0,0,0;", "wk=0:9,9,0,0,0,0;1:0,0,1,0,0,0;"));
            CollectionAssert.AreEqual(new[] { Balance.PlayerCapUp, 0, 0, 1, 0, 0 }, g.Player.Held, "teto por tipo; negativo e texto viram 0");
            CollectionAssert.AreEqual(new[] { Balance.WorkerCap, 0, 0, 0, 0, 0 }, g.Workers[0].Held, "ajudante de minerio: um tipo, ate o teto");
            Assert.AreEqual(0, g.Workers[1].Count, "ajudante de lingote nao volta com espada");
            Sim v050 = Sim.Load(text.Replace("wk=0:2,0,0,0,0,0;1:0,0,0,0,0,0;2:", "wk=2,0,0,0,0,0;0,0,0,0,0,0;"));
            for (int k = 0; k < 3; k++) CollectionAssert.AreEqual(s.Workers[k].Held, v050.Workers[k].Held, $"save da 0.5.0 (sem papel): ajudante {k} pela posicao");
            Sim old = Sim.Load("v=1\nt=600\ngold=190\nup=1111111100000000000000000\npx=4.5,3.5\n");
            Assert.IsTrue(old.Player.Count == 0 && old.Workers.TrueForAll(w => w.Count == 0), "save antigo sem hold=/wk=: maos vazias, como antes");
        }

        // ------------------------------------------------------------------ FASE8: VIP e velocidade por anuncio (docs/FASE8_VIP_VELOCIDADE.md)

        /// <summary>Partida que ja fez a 1a venda (o relogio do VIP so anda depois dela), sem mexer no resto.</summary>
        static Sim Sold() { var s = new Sim(); s.FirstSaleTime = 1f; return s; }

        // ------------------------------------------------------------------ FASE9: encomendas (docs/FASE9_ENCOMENDAS.md)

        /// <summary>Roda tick a tick ate `kind` aparecer (no maximo `seconds`); `stock` mantem a prateleira cheia desse item.</summary>
        static bool RunUntil(Sim s, Ev kind, float seconds, Item? stock = null)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                if (stock.HasValue) s.Stock[(int)stock.Value] = s.CounterCap;
                s.Tick(Dt, 0f, 0f);
                if (Count(s, kind) > 0) return true;
            }
            return false;
        }

        /// <summary>FASE9 §1: o relogio da encomenda so anda depois da 1a venda; a 1a (5 espadas) chega 45 s depois dela.</summary>
        [Test]
        public void Encomenda_Primeira_5Espadas_45sDepoisDa1aVenda()
        {
            var n = new Sim();
            Run(n, 120f);
            Assert.AreEqual((-1, Balance.OrderFirstDelay), (n.OrderItem, n.OrderIn), "sem venda o relogio nao anda");
            var s = Sold();
            Run(s, Balance.OrderFirstDelay - 1f);
            Assert.AreEqual(-1, s.OrderItem, "aos 44 s ainda nao");
            Assert.IsTrue(RunUntil(s, Ev.OrderNew, 2f), "OrderNew aos 45 s");
            Assert.AreEqual(((int)Item.Sword, Balance.OrderFirstTarget, 0), (s.OrderItem, s.OrderTarget, s.OrderProgress));
            Assert.GreaterOrEqual(s.OrderReward, Balance.OrderRewardMin);
            Assert.AreEqual(0, s.OrderReward % 5, "premio redondo");
        }

        /// <summary>FASE9 §1: so conta venda feita depois do inicio; entregue paga o premio FORA do GoldEarned (como o bau: nao infla a taxa
        /// online nem o cofre) e a proxima chega 20 s depois.</summary>
        [Test]
        public void Encomenda_ContaSoVendaDepoisDoInicio_PremioForaDoGoldEarned_ProximaEm20s()
        {
            var s = Sold();
            Assert.IsTrue(RunUntil(s, Ev.OrderNew, 60f, Item.Sword));
            int before = s.SoldItems[(int)Item.Sword];
            Assert.Greater(before, 0, "vendeu espada antes da encomenda");
            Assert.AreEqual(0, s.OrderProgress, "venda de antes nao conta");
            int reward = s.OrderReward, gold = 0, earned = 0;
            bool done = false;
            for (float t = 0f; t < 600f && !done; t += Dt)
            {
                s.Stock[(int)Item.Sword] = s.CounterCap;
                gold = s.Gold; earned = s.GoldEarned;
                s.Tick(Dt, 0f, 0f);
                done = Count(s, Ev.OrderDone) == 1;
            }
            Assert.IsTrue(done, "entregou");
            Assert.AreEqual(reward, (s.Gold - gold) - (s.GoldEarned - earned), "premio no ouro, fora do GoldEarned");
            Assert.GreaterOrEqual(s.SoldItems[(int)Item.Sword] - before, Balance.OrderFirstTarget);
            Assert.AreEqual((-1, 1), (s.OrderItem, s.OrderCount));
            Run(s, Balance.OrderGap - 1f);
            Assert.AreEqual(-1, s.OrderItem, "intervalo de 20 s");
            Assert.IsTrue(RunUntil(s, Ev.OrderNew, 2f), "a 2a chega");
        }

        /// <summary>FASE9 §2: rodizio so entre linhas abertas (espada, escudo, ferramenta, joia); a partir da 2a, N = 150 s de clientes daquela
        /// linha (OrderSeconds / ClientInterval), entre 3 e 30; premio ~ 10 s da taxa online.</summary>
        [Test]
        public void Encomenda_RodizioDeLinhasAbertas_TamanhoPelaDemanda_PremioPelaTaxa()
        {
            var s = Rich();
            Assert.IsTrue(s.Buy(Upgrade.Shields) && s.Buy(Upgrade.Tools));
            s.FirstSaleTime = 1f; s.RateEma = 4.0;
            var seen = new List<int>();
            for (int k = 1; k <= 6; k++)
            {
                s.OrderItem = -1; s.OrderCount = k; s.OrderIn = 0f;
                s.Tick(Dt, 0f, 0f);
                seen.Add(s.OrderItem);
                int want = Math.Max(Balance.OrderMin, Math.Min(Balance.OrderMax, (int)Math.Round(Balance.OrderSeconds / s.ClientInterval((Item)s.OrderItem))));
                Assert.AreEqual(want, s.OrderTarget, $"tamanho da encomenda {k}");
                Assert.AreEqual(Math.Max(Balance.OrderRewardMin, 5 * (int)Math.Round(4.0 * Balance.OrderRewardSeconds / 5.0)), s.OrderReward);
            }
            CollectionAssert.AreEqual(new[] { 3, 4, 2, 3, 4, 2 }, seen, "escudo, ferramenta, espada...; sem joia (joalheria fechada)");
        }

        /// <summary>FASE9 §3: a encomenda vai no save (item, alvo, progresso, premio, contagem, relogio). Save antigo sem ord= abre do zero; lixo
        /// vira o padrao; item de linha fechada nao volta.</summary>
        [Test]
        public void Save_Encomenda_IdaEVolta_SaveAntigo_Lixo()
        {
            var s = Sold();
            Assert.IsTrue(RunUntil(s, Ev.OrderNew, 60f));
            s.OrderProgress = 3;
            string text = s.Save(10);
            StringAssert.Contains("ord=2,5,3," + s.OrderReward + ",0,", text);
            Sim b = Sim.Load(text);
            Assert.AreEqual((2, 5, 3, s.OrderReward, 0), (b.OrderItem, b.OrderTarget, b.OrderProgress, b.OrderReward, b.OrderCount));
            Assert.AreEqual(text, b.Save(10), "save estavel");
            string old = string.Join("\n", Array.FindAll(text.Split('\n'), l => !l.StartsWith("ord=")));
            Sim o = Sim.Load(old);
            Assert.AreEqual((-1, 0, Balance.OrderFirstDelay), (o.OrderItem, o.OrderCount, o.OrderIn), "save antigo: do zero");
            Sim g = Sim.Load(old + "ord=x,-4,99,-1,-2,abc\n");
            Assert.AreEqual((-1, 0, Balance.OrderFirstDelay), (g.OrderItem, g.OrderCount, g.OrderIn), "lixo vira o padrao");
            g = Sim.Load(old + "ord=5,10,2,100,3,0\n");
            Assert.AreEqual((-1, 3), (g.OrderItem, g.OrderCount), "joia sem joalheria nao volta; a contagem fica");
            g = Sim.Load(old + "ord=2,5,99,100,0,0\n");
            Assert.AreEqual(5, g.OrderProgress, "progresso no teto do alvo");
        }

        static Client Vip(Sim s) => s.Queue.Find(c => c.Vip);

        /// <summary>Zera o relogio e roda um tick: o VIP NATURAL e' sorteado e entra (fila com vaga).</summary>
        static Client NaturalVip(Sim s) { s.VipIn = 0f; s.Tick(Dt, 0f, 0f); return Vip(s); }

        /// <summary>
        /// FASE8 §1: o relogio do VIP fica parado ate a 1a venda; dali o VIP chega quando o intervalo sorteado (4-6 min, deterministico) zera.
        /// Com a fila cheia ele espera fora dela (nao some) e pega a proxima vaga livre antes de quem chega depois.
        /// </summary>
        [Test]
        public void Vip_SoDepoisDa1aVenda_ChegaEntre4e6Min_EsperaAVagaSemSumir()
        {
            float min = float.MaxValue, max = 0f;
            for (int n = 0; n < 200; n++) { min = Math.Min(min, Sim.VipInterval(n)); max = Math.Max(max, Sim.VipInterval(n)); }
            Assert.That(min, Is.InRange(Balance.VipMin, Balance.VipMin + 10f), "sorteio cobre 4 min");
            Assert.That(max, Is.InRange(Balance.VipMax - 10f, Balance.VipMax), "sorteio cobre 6 min");
            var s = new Sim();
            float clock = s.VipIn;
            Assert.AreEqual(Sim.VipInterval(0), clock);
            Run(s, Balance.VipMax + 30f);   // sem estoque ninguem compra
            Assert.AreEqual((clock, false, 0), (s.VipIn, s.VipActive, s.VipCount), "antes da 1a venda o relogio fica parado");
            s.Stock[(int)Item.Sword] = 1;
            Run(s, 0.5f);
            float t0 = s.FirstSaleTime;
            Assert.Greater(t0, 0f, "1a venda");
            s.Queue.Clear();
            for (int i = 0; i < s.QueueCap; i++) s.Queue.Add(new Client { Want = Item.Sword, Patience = 999f, MaxPatience = 999f });   // fila cheia de quem nao cansa
            while (!s.VipActive && s.Time < t0 + Balance.VipMax + 1f) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(t0 + clock, s.Time, 0.5f, "chega quando o intervalo sorteado zera, contado da 1a venda");
            Assert.AreEqual(1, s.VipCount);
            Assert.That(s.VipIn, Is.InRange(Balance.VipMin, Balance.VipMax), "o proximo ja foi sorteado");
            Run(s, 30f);
            Assert.IsTrue(s.VipActive && Vip(s) == null && s.Queue.Count == s.QueueCap, "fila cheia: espera fora da fila e nao some");
            s.Stock[(int)Item.Sword] = 1;   // um cliente compra e libera a vaga
            int arrived = 0; SimEvent ev = default;
            for (float t = 0f; t < 1f; t += Dt) { s.Tick(Dt, 0f, 0f); foreach (SimEvent e in s.Events) if (e.Kind == Ev.VipArrived) { arrived++; ev = e; } }
            Client vip = Vip(s);
            Assert.AreEqual(1, arrived, "entrou na vaga que abriu");
            Assert.IsNotNull(vip);
            Assert.AreEqual(((int)Item.Sword, Balance.VipPackMin), (ev.A, ev.B), "so a espada esta aberta; renda baixa = pacote minimo");
            Assert.AreEqual(s.ClientSlot(s.Queue.IndexOf(vip)).ToString(), ev.Pos.ToString(), "evento na vaga dele");
            Assert.AreEqual((108f, 150f, 210f), (Balance.VipPatienceFor(3), Balance.VipPatienceFor(10), Balance.VipPatienceFor(20)), "paciencia do VIP = 90 s + 6 s por unidade");
            Assert.AreEqual((Balance.VipPatienceFor(Balance.VipPackMin), Balance.VipPackMin), (vip.MaxPatience, vip.Pack));
            Assert.LessOrEqual(s.Queue.Count, s.QueueCap);
        }

        /// <summary>
        /// FASE8 §1: o VIP leva o pacote uma unidade por atendimento, cada uma a 3x o preco (Sold com B = 3x), e sai com VipServed (B = total).
        /// O pacote acompanha a renda (~7% do ouro de 300 s a 3x o preco), de 3 ate a prateleira; o produto roda entre as linhas do balcao.
        /// </summary>
        [Test]
        public void Vip_PacoteA3xPorUnidade_UmPorAtendimento_TamanhoPelaRenda_ProdutoAberto()
        {
            var s = Sold();
            Assert.AreEqual((Item.Sword, Balance.VipPackMin), (NaturalVip(s).Want, Vip(s).Pack));
            s.Stock[(int)Item.Sword] = Balance.VipPackMin;
            var paid = new List<int>(); int served = -1;
            for (float t = 0f; t < 3f; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events) { if (e.Kind == Ev.Sold) paid.Add(e.B); if (e.Kind == Ev.VipServed) served = e.B; }
            }
            int unit = Balance.VipPriceMul * Balance.Price[(int)Item.Sword];
            CollectionAssert.AreEqual(new[] { unit, unit, unit }, paid, "3 unidades, uma por atendimento, 3x o preco");
            Assert.AreEqual((3 * unit, 3 * unit, 3), (served, s.Gold, s.Sales), "VipServed com o total; ouro e vendas contam");
            Assert.IsFalse(s.VipActive, "levou o pacote e foi embora");
            int Pack(double rate, bool vitrine, bool extra = false)
            {
                var p = Sold(); p.RateEma = rate;
                if (vitrine) p.Buy(Upgrade.CounterCapacity);
                if (!extra) return NaturalVip(p).Pack;
                p.VipIn = Balance.VipMax; Assert.IsTrue(p.SummonVip()); p.Tick(Dt, 0f, 0f);
                return Vip(p).Pack;
            }
            double four = 4.0 * unit / (Balance.VipShare * 300.0);   // renda em que o natural pede 4 espadas (7% de 300 s, a 3x)
            Assert.AreEqual(Balance.VipPackMin, Pack(0.0, false), "renda baixa: 3");
            Assert.AreEqual(4, Pack(four, false), "4 espadas = 7% de 300 s de renda, a 3x");
            Assert.AreEqual(Balance.CounterCap0, Pack(1000.0, false), "teto: a prateleira (5)");
            Assert.AreEqual(Balance.CounterCap1, Pack(1000.0, true), "com a Vitrine, 10");
            Assert.AreEqual((6, 8, Balance.VipSummonPackMax), (Pack(0.0, true, true), Pack(four, true, true), Pack(1000.0, true, true)), "chamado: 2x o natural, ate 20");
            Assert.AreEqual((6, 2 * Balance.CounterCap0), (Pack(0.0, false, true), Pack(1000.0, false, true)), "chamado passa da prateleira: 2x3 = 6 e 2x5 = 10 sem a Vitrine");
            var r = Sold();
            r.Buy(Upgrade.Shields); r.Buy(Upgrade.Tools); r.Buy(Upgrade.SideCorridor); r.Buy(Upgrade.Jewelry);
            var wants = new List<Item>();
            for (int k = 0; k < 4; k++) { wants.Add(NaturalVip(r).Want); r.Queue.RemoveAll(c => c.Vip); }
            CollectionAssert.AreEqual(new[] { Item.Sword, Item.Shield, Item.Tool, Item.Sword }, wants, "rodizio entre as linhas abertas do balcao (a joia e' da loja de joias)");
        }

        /// <summary>FASE8 §1: o VIP que nao recebe o pacote na paciencia (120 s, tempo de jogo) vai embora com VipLeft (B = unidades que
        /// faltaram), pagou so o que levou e fica fora do ClientsLost (metrica dos clientes normais).</summary>
        [Test]
        public void Vip_CansaPelaPaciencia_PagaSoOQueLevou_ForaDoClientsLost()
        {
            var s = Sold();
            NaturalVip(s);
            s.Stock[(int)Item.Sword] = 1;
            Run(s, 1f);
            int unit = Balance.VipPriceMul * Balance.Price[(int)Item.Sword];
            Assert.AreEqual((Balance.VipPackMin - 1, unit), (Vip(s).Pack, s.Gold), "levou 1 do pacote");
            int left = -1, lost = 0; float leftAt = -1f, pat = Balance.VipPatienceFor(Balance.VipPackMin);
            for (float t = 0f; t < pat; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events)
                {
                    if (e.Kind == Ev.VipLeft) { left = e.B; leftAt = s.Time; }
                    if (e.Kind == Ev.ClientLeft && e.B == 0) lost++;
                }
            }
            Assert.AreEqual(Balance.VipPackMin - 1, left, "VipLeft com as unidades que faltaram");
            Assert.AreEqual(pat, leftAt, 0.1f, "90 + 6 x 3 = 108 s desde que entrou na vaga");
            Assert.IsFalse(s.VipActive);
            Assert.Greater(lost, 0, "os clientes normais de espada tambem cansaram");
            Assert.AreEqual(lost, s.ClientsLost, "ClientsLost conta so os normais");
            Assert.AreEqual(unit, s.Gold, "pagou so a unidade que levou");
        }

        /// <summary>FASE8 §2 (decisao do coordenador): "chamar o VIP" (anuncio) so depois da 1a venda, sem VIP ativo (esperando vaga ou na fila) e
        /// com o natural a mais de 60 s. O chamado e' um VIP EXTRA: 2x o pacote do natural (ate a prateleira) e o relogio natural nao muda.</summary>
        [Test]
        public void SummonVip_VipExtra_PacoteDobrado_NaoMexeNoRelogio_SoSemVipAtivoEComONaturalAMaisDe60s()
        {
            var s = new Sim();
            s.Buy(Upgrade.CounterCapacity);   // prateleira de 10: o dobro cabe
            Assert.IsFalse(s.CanSummonVip || s.SummonVip(), "antes da 1a venda nao tem VIP");
            s.FirstSaleTime = 1f; s.VipIn = Balance.VipSummonMinIn;
            Assert.IsFalse(s.CanSummonVip || s.SummonVip(), "o natural chega em 60 s: nao gasta o anuncio");
            s.VipIn = 200f;
            Assert.IsTrue(s.CanSummonVip && s.SummonVip());
            Assert.AreEqual((200f, 1, true), (s.VipIn, s.VipCount, s.VipActive), "VIP extra agora; o relogio natural fica como estava");
            Assert.IsFalse(s.SummonVip(), "esperando vaga: recusa");
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(Balance.VipSummonPackMul * Balance.VipPackMin, Vip(s).Pack, "pacote 2x o do natural (3 -> 6)");
            Assert.IsFalse(s.CanSummonVip || s.SummonVip(), "VIP na fila: recusa");
            s.Stock[(int)Item.Sword] = s.CounterCap;
            Run(s, 3f);
            Assert.IsFalse(s.VipActive, "levou o pacote");
            Assert.AreEqual(6 * Balance.VipPriceMul * Balance.Price[(int)Item.Sword], s.Gold, "6 espadas a 3x");
            Assert.IsTrue(s.CanSummonVip, "sem VIP e com o natural longe: pode de novo");
            float due = s.Time + s.VipIn;
            Assert.AreEqual(200f - s.Time, s.VipIn, 0.05f, "o relogio natural andou so o tempo de jogo");
            while (!s.VipActive && s.Time < due + 1f) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(due, s.Time, 0.1f, "o natural chega no horario de antes do anuncio");
            Assert.AreEqual(2, s.VipCount);
        }

        /// <summary>
        /// FASE8 §3: 1 anuncio = 2x por 60 s; o 2o com o boost ativo sobe para 3x e reinicia os 60 s; teto 3x; expira no tempo de jogo. Acelera
        /// estacoes, ajudantes e a chegada de clientes; o ferreiro no maximo 1,3x; paciencia e relogio do VIP no tempo de jogo; a taxa do cofre
        /// offline aprende o ouro por segundo de fabrica (o offline nao paga boost).
        /// </summary>
        [Test]
        public void Boost_2xPor60s_2oAnuncioSobePara3x_TetoEExpira_OQueAcelera()
        {
            var s = new Sim();
            Assert.AreEqual((1f, 0f, 0f, true), (s.BoostMul, s.BoostLeft, s.BoostCooldown, s.CanBoost));
            Assert.AreEqual((2f, Balance.BoostSeconds), (s.StartBoost(), s.BoostLeft), "1 anuncio: 2x por 60 s");
            Assert.IsTrue(s.CanBoost, "no 2x da para subir");
            Run(s, 30f);
            Assert.AreEqual(Balance.BoostSeconds - 30f, s.BoostLeft, 0.05f, "corre no tempo de jogo");
            Assert.AreEqual((3f, Balance.BoostSeconds), (s.StartBoost(), s.BoostLeft), "2o anuncio com o boost ativo: 3x e reinicia os 60 s");
            Assert.IsFalse(s.CanBoost, "teto 3x");
            Run(s, 1f);
            float left = s.BoostLeft;
            Assert.AreEqual((0f, 3f, left), (s.StartBoost(), s.BoostMul, s.BoostLeft), "no 3x recusa e nada muda");
            Run(s, Balance.BoostSeconds - 2f);
            Assert.AreEqual((3f, 0f), (s.BoostMul, s.BoostCooldown), "a recarga so comeca no fim do boost");
            Run(s, 2f);
            Assert.AreEqual((1f, 0f), (s.BoostMul, s.BoostLeft), "expirou");
            Assert.AreEqual(Balance.BoostCooldownSeconds - 1f, s.BoostCooldown, 0.1f, "recarga de 5 min contada do fim");
            Assert.AreEqual((false, 0f, 1f), (s.CanBoost, s.StartBoost(), s.BoostMul), "na recarga recusa");
            Run(s, Balance.BoostCooldownSeconds - 2f);
            Assert.IsFalse(s.CanBoost, "ainda na recarga (no tempo de jogo)");
            Run(s, 1.5f);
            Assert.AreEqual((0f, true), (s.BoostCooldown, s.CanBoost), "recarga zerada: libera");
            Assert.AreEqual(2f, s.StartBoost());

            Sim[] Trio() { var m = new[] { new Sim(), new Sim(), new Sim() }; m[1].StartBoost(); m[2].StartBoost(); m[2].StartBoost(); return m; }   // 1x, 2x, 3x
            Sim[] f = Trio();
            foreach (Sim x in f) { x.FurnaceA.In = Balance.FurnaceIn; Run(x, Balance.FurnaceTime0 + 0.5f); }
            Assert.AreEqual((1, 2, 3), (f[0].Crafted[(int)Item.Ingot], f[1].Crafted[(int)Item.Ingot], f[2].Crafted[(int)Item.Ingot]), "estacao: 2x e 3x");
            Sim[] p = Trio();
            foreach (Sim x in p) Run(x, 0.5f, 1f, 0f);
            float dx = p[0].Player.Pos.X - 4.5f;
            Assert.AreEqual(Balance.BoostPlayerMax, (p[1].Player.Pos.X - 4.5f) / dx, 1e-3f, "ferreiro a 1,3x no boost 2x");
            Assert.AreEqual(Balance.BoostPlayerMax, (p[2].Player.Pos.X - 4.5f) / dx, 1e-3f, "e no 3x tambem: no maximo 1,3x");
            Sim[] w = Trio();
            foreach (Sim x in w) { x.Buy(Upgrade.Helper1); Run(x, 0.5f); }
            float wd = V2.Dist(w[0].Workers[0].Pos, Sim.HireSpot);
            Assert.AreEqual(2f, V2.Dist(w[1].Workers[0].Pos, Sim.HireSpot) / wd, 0.02f, "ajudante 2x");
            Assert.AreEqual(3f, V2.Dist(w[2].Workers[0].Pos, Sim.HireSpot) / wd, 0.02f, "ajudante 3x");
            Sim[] c = Trio();
            var arrivals = new int[3];
            for (int k = 0; k < 3; k++)
                for (float t = 0f; t < 30f; t += Dt) { c[k].Tick(Dt, 0f, 0f); arrivals[k] += Count(c[k], Ev.ClientArrived) + Count(c[k], Ev.ClientLeft); }
            Assert.AreEqual((4, 9, 14), (arrivals[0], arrivals[1], arrivals[2]), "chegada de clientes: 8 s e 6 s entre eles, divididos por 2 e 3");
            Assert.AreEqual((0, 0), (c[2].ClientsLost, c[2].Queue.Count - c[2].QueueCap), "fila cheia e ninguem cansou: paciencia no tempo de jogo");
            Assert.AreEqual(Balance.PatienceFor(Item.Sword) - (30f - Balance.FirstClientAt / 3f), c[2].Queue[0].Patience, 0.15f, "o 1o esperou 27 s de jogo, nao 82 s de fabrica");
            var v = Sold(); v.VipIn = 200f; v.StartBoost(); v.StartBoost();
            Run(v, 10f);
            Assert.AreEqual(190f, v.VipIn, 0.05f, "relogio do VIP no tempo de jogo");
            var o = new Sim();
            o.StartBoost();
            o.Stock[(int)Item.Sword] = 1; o.Queue.Add(new Client { Want = Item.Sword, Patience = 99f, MaxPatience = 99f });
            o.Tick(Dt, 0f, 0f);
            Assert.AreEqual(Balance.Price[(int)Item.Sword] / (2.0 * Balance.RateTau), o.RateEma, 1e-6, "taxa do cofre = ouro por segundo de fabrica (metade no 2x)");
        }

        /// <summary>FASE8 §4: o relogio do VIP, os VIPs sorteados e o VIP pendente vao no save (o que estava na vaga volta pendente com o que
        /// falta do pacote); o boost nao vai. Save antigo (sem vip=) carrega com o relogio novo e sem VIP; lixo vira o padrao.</summary>
        [Test]
        public void Save_RelogioEVipPendente_IdaEVolta_BoostNaoVai_SaveAntigo()
        {
            var s = Sold();
            s.Buy(Upgrade.Shields);
            Run(s, 3f);
            Assert.AreEqual(Sim.VipInterval(0) - 3f, s.VipIn, 0.05f, "relogio andando");
            Assert.IsTrue(s.SummonVip());   // sorteado, ainda sem vaga (sem Tick)
            s.StartBoost(); s.StartBoost();
            string text = s.Save(10);
            StringAssert.Contains("vip=" + s.VipIn.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ",1,2,6,0,0\n", text);   // chamado: 2x3, passa da prateleira de 5; ainda fora da vaga, nada pago
            StringAssert.DoesNotContain("boost", text);
            Sim b = Sim.Load(text);
            Assert.AreEqual(s.VipIn, b.VipIn, 1e-3f);
            Assert.AreEqual((1, true), (b.VipCount, b.VipActive), "VIP pendente volta");
            Assert.AreEqual((1f, 0f), (b.BoostMul, b.BoostLeft), "o boost some ao fechar");
            Assert.AreEqual(text, b.Save(10), "save estavel");
            b.Tick(Dt, 0f, 0f);
            Assert.AreEqual((Item.Sword, 6), (Vip(b).Want, Vip(b).Pack), "o pendente pega a 1a vaga ao reabrir, com o pacote do chamado");
            Assert.AreEqual(1, Count(b, Ev.VipArrived), "nunca tinha entrado: chega agora, com aviso");
            b.Stock[(int)Item.Sword] = 1;
            Run(b, 1f);
            Assert.AreEqual(5, Vip(b).Pack);
            Sim c = Sim.Load(b.Save(20));
            Assert.IsTrue(c.VipActive && Vip(c) == null, "a fila nao vai no save: o VIP da vaga volta pendente");
            c.Tick(Dt, 0f, 0f);
            Assert.AreEqual(5, Vip(c).Pack, "com o que falta do pacote");
            Assert.AreEqual((0, Vip(b).Paid), (Count(c, Ev.VipArrived), Vip(c).Paid), "revisao v0.5 #5: ja estava na vaga, sem novo aviso/vip_arrived e com o que ja pagou");
            Assert.Greater(Vip(c).Paid, 0);
            Assert.AreEqual(Balance.VipPatienceFor(5) - Dt, Vip(c).Patience, 1e-3f, "paciencia cheia, pelo que falta");
            var k = new Sim();
            k.StartBoost(); Run(k, Balance.BoostSeconds + 1f);
            Assert.Greater(k.BoostCooldown, 0f);
            Sim kb = Sim.Load(k.Save(1));
            Assert.AreEqual((0f, true), (kb.BoostCooldown, kb.CanBoost), "a recarga nao vai no save (FASE8 §3)");
            string old = text.Substring(0, text.IndexOf("vip="));   // save da FASE7
            Sim o = Sim.Load(old);
            Assert.AreEqual((Sim.VipInterval(0), 0, false), (o.VipIn, o.VipCount, o.VipActive), "save antigo: relogio novo, sem VIP");
            Run(o, 1f);
            Assert.Less(o.VipIn, Sim.VipInterval(0), "ja tinha vendido: o relogio anda");
            Sim g = Sim.Load(old + "vip=abc,-5,4,2\n");
            Assert.AreEqual((Sim.VipInterval(0), 0, false), (g.VipIn, g.VipCount, g.VipActive), "lixo vira o padrao; ferramenta sem a linha aberta nao vira VIP");
            g = Sim.Load(old + "vip=99999,7,5,99\n");
            Assert.AreEqual((Balance.VipMax, 7, false), (g.VipIn, g.VipCount, g.VipActive), "relogio no teto de 6 min; joia nao e' do balcao");
            g = Sim.Load(old + "vip=-1,7,3,99\n");
            g.Tick(Dt, 0f, 0f);
            Assert.AreEqual((Item.Shield, Balance.VipSummonPackMax), (Vip(g).Want, Vip(g).Pack), "escudo aberto; pacote no teto de 20 (o do chamado)");
        }

        static float SegDist(V2 p, V2 a, V2 b)
        {
            V2 ab = b - a; float l2 = ab.X * ab.X + ab.Y * ab.Y;
            float t = l2 < 1e-6f ? 0f : Math.Max(0f, Math.Min(1f, ((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / l2));
            return V2.Dist(p, a + ab * t);
        }
    }
}
