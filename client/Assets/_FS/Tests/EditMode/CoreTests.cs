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

        [Test]
        public void Capacidade_LimitaPilha()
        {
            var s = new Sim();
            StandAt(s, s.Deposit.InAt, 10f);
            Assert.AreEqual(Balance.PlayerCap, s.Player.Count);
            s.Buy(Upgrade.PlayerCapacity);
            Run(s, 10f);
            Assert.AreEqual(Balance.PlayerCapUp, s.Player.Count, "mochila sobe o teto");
            // pilha homogenea: com minerio na mao e fornalha cheia, nao pega lingote (nunca mistura)
            s.FurnaceA.In = Balance.FurnaceIn; s.FurnaceA.Out = 2; s.FurnaceA.Busy = true; s.FurnaceA.Progress = 0f;
            StandAt(s, s.FurnaceA.OutAt, 2f);
            Assert.AreEqual((Item.Ore, Balance.PlayerCapUp), (s.Player.Item, s.Player.Count), "segurou o minerio, nao misturou lingote");
            Assert.AreEqual(2, s.FurnaceA.Out);
            // esvaziando a mao na entrada, pega os lingotes na saida
            s.FurnaceA.In = 0;
            StandAt(s, s.FurnaceA.InAt, 2f);
            StandAt(s, s.FurnaceA.OutAt, 1f);
            Assert.AreEqual(Item.Ingot, s.Player.Item, "depositou os 6 e passou a pegar lingotes");
            Assert.GreaterOrEqual(s.Player.Count, 2);
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
            s.Buy(Upgrade.CounterCapacity);
            Run(s, 30f);
            bool shieldAsked = false;
            foreach (Client c in s.Queue) shieldAsked |= c.Want == Item.Shield;
            Assert.IsTrue(shieldAsked, "linha de escudos traz cliente de escudo");
            Assert.LessOrEqual(s.Queue.Count, Balance.QueueCap1);
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
            Pad pad = s.Pads[0];
            Assert.AreEqual((int)Upgrade.FurnaceSpeed1, pad.Current(s));
            int cost = Upgrades.Cost(Upgrade.FurnaceSpeed1);
            StandAt(s, pad.Pos, 2f);
            Assert.AreEqual((0, 20), (s.Gold, pad.Paid), "despeja o que tem e guarda o parcial");
            Assert.IsFalse(s.Bought[(int)Upgrade.FurnaceSpeed1]);
            s.Gold = cost;
            Run(s, 2f);
            Assert.IsTrue(s.Bought[(int)Upgrade.FurnaceSpeed1], "completou o preco");
            Assert.AreEqual(20, s.Gold, "so cobrou o que faltava; o proximo nivel da cadeia NAO engole o troco");
            Assert.AreEqual(0, pad.Paid, "pad zera depois da compra");
            // sai de cima e volta: o pad rearma e passa a cobrar o fole duplo
            StandAt(s, new V2(4.5f, 3.5f), 0.5f);
            StandAt(s, pad.Pos, 1f);
            Assert.AreEqual((0, 20), (s.Gold, pad.Paid), "rearmou");
            // atravessar andando nao gasta (dwell): entra por um lado do pad e sai pelo outro sem parar
            var walk = new Sim(); walk.Gold = 50;
            Pad p = walk.Pads[(int)Upgrade.PlayerCapacity - 4 + 3];   // pad da mochila, (4,5, 0,6): da para cruzar na horizontal
            walk.Player.Pos = new V2(p.Pos.X - 1.0f, p.Pos.Y);
            Run(walk, 0.7f, 1f, 0f);                                 // 2,1 m a 3 m/s: cruza o diametro inteiro
            Assert.Greater(walk.Player.Pos.X, p.Pos.X + Balance.PadRadius, "saiu do outro lado");
            Assert.AreEqual(50, walk.Gold, "passar por cima sem parar nao paga");
            // parar em cima paga na hora
            StandAt(walk, p.Pos, 0.2f);
            Assert.Less(walk.Gold, 50, "parado paga");
            Assert.AreEqual(Balance.FurnaceTime1, s.FurnaceA.Time, "efeito aplicado");
            Assert.AreEqual((int)Upgrade.FurnaceSpeed2, pad.Current(s), "a cadeia avanca para o proximo nivel");
            Assert.IsFalse(s.Buy(Upgrade.FurnaceSpeed1), "comprar de novo e' no-op");
        }

        [Test]
        public void Curva_DeCusto_Dos23Upgrades_20ProdutivosELuxo()
        {
            Assert.AreEqual(20, Upgrades.ProductionCount);
            Assert.AreEqual(23, Upgrades.Count);
            Assert.AreEqual(23, Upgrades.All.Length);
            Assert.AreEqual(23, Enum.GetValues(typeof(Upgrade)).Length);
            Assert.AreEqual((11000, 14000, 18000), (Upgrades.Cost(Upgrade.WorkshopFacade), Upgrades.Cost(Upgrade.WorkshopFloor), Upgrades.Cost(Upgrade.JewelryDecor)));
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
            // todo pad acaba visivel: pre-requisitos sao alcancaveis comprando em ordem de tier
            var s = new Sim();
            for (int i = 0; i < Upgrades.Count; i++)
            {
                bool visible = false;
                foreach (Pad p in s.Pads) visible |= p.Current(s) == i;
                Assert.IsTrue(visible, $"{(Upgrade)i} precisa estar num pad visivel quando chega a vez dele");
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
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.SideCorridor), s.ApplyOffline(7200, 4), "o MENOR travado (Corredor 690), nao o proximo tier (2a fornalha 895)");
            for (int i = 0; i <= (int)Upgrade.Jewelry; i++) s.Buy((Upgrade)i);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.Jeweler), s.ApplyOffline(7200, 5), "fase 2: o menor travado e' o Joalheiro (2200)");
            for (int i = 0; i < Upgrades.Count; i++) s.Buy((Upgrade)i);
            Assert.AreEqual(2 * Upgrades.Cost(Upgrade.JewelVitrine), s.ApplyOffline(7200, 6), "tudo comprado: 2x o ultimo (Vitrine de joias 9000 = 18000)");
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
            s.Gold = 0; s.Player.Item = Item.Ore; s.Player.Count = 2;
            Run(s, 2f, 0f, 1f);
            Assert.AreEqual(2f, s.WalkNoDecision, 0.1f, "carregando e' trabalho, nao indecisao");
            s.Player.Count = 0;
            Run(s, 2f, 0f, 0f);
            Assert.AreEqual(2f, s.WalkNoDecision, 0.1f, "parado nao conta");
        }

        [Test]
        public void Dica_SegueOEstado()
        {
            var s = new Sim();
            Assert.AreEqual(Hint.GrabOre, s.CurrentHint());
            s.Player.Item = Item.Ore; s.Player.Count = 2;
            Assert.AreEqual(Hint.OreToFurnace, s.CurrentHint());
            s.Player.Count = 0; s.FurnaceA.Out = 1;
            Assert.AreEqual(Hint.PickIngots, s.CurrentHint());
            s.Gold = 100000;
            Assert.AreEqual(Hint.BuyPad, s.CurrentHint());
            Assert.AreEqual(0, s.HintArg, "aponta o pad mais barato (fole)");
        }

        /// <summary>
        /// Regressao (achado da medicao de 45 min, BALANCE.md §9): fila cheia de clientes de escudo sem escudo, vitrine cheia
        /// de espada, jogador com espada na mao = ninguem mais comprava (os timers travavam em fase e todo cliente de espada
        /// dava com a fila cheia). Cliente que acha o produto na vitrine compra direto.
        /// </summary>
        [Test]
        public void FilaCheia_ProdutoNaVitrine_ClienteCompraDireto_NaoTrava()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields);
            for (int i = 0; i < Balance.QueueCap0; i++) s.Queue.Add(new Client { Want = Item.Shield, Patience = 999f, MaxPatience = 999f });
            s.Stock[(int)Item.Sword] = s.CounterCap;
            Run(s, Balance.FirstClientAt + 0.1f);   // 1o cliente de espada chega com a fila cheia
            Assert.AreEqual(1, s.Sales, "comprou da vitrine sem entrar na fila");
            Assert.AreEqual((s.CounterCap - 1, 0), (s.Stock[(int)Item.Sword], s.ClientsTurnedAway));
            Assert.AreEqual(Balance.QueueCap0, s.Queue.Count, "a fila de escudo continua la");
            s.Stock[(int)Item.Sword] = 0;
            Run(s, Balance.ClientInterval[(int)Item.Sword] + 0.1f);
            Assert.Greater(s.ClientsTurnedAway, 0, "sem o produto na vitrine, fila cheia continua mandando embora");
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
            s.Player.Pos = new V2(4.5f, 4.5f);   // faixa livre ate o fim da rua (a carroca fica em y 3,0)
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
            s.Player.Item = Item.Jewel; s.Player.Count = 2;
            Assert.AreEqual((Hint.ProductToCounter, (int)Item.Jewel), (s.CurrentHint(), s.HintArg), "dica: leve joias (a view traduz para a loja de joias)");
            StandAt(s, s.Counter.InAt, 1f);
            Assert.AreEqual((2, 0), (s.Player.Count, s.Stock[(int)Item.Jewel]), "balcao principal recusa joia");
            StandAt(s, s.JewelShop.InAt, 1f);
            Assert.AreEqual((0, 2), (s.Player.Count, s.Stock[(int)Item.Jewel]), "loja de joias aceita");
            s.Player.Item = Item.Sword; s.Player.Count = 2;
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
            StringAssert.Contains("up=00100000000000011000000\n", text);
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
            Assert.AreEqual((190, 8), (s.Gold, s.UpgradesBought));
            Assert.IsFalse(s.Bought[(int)Upgrade.SideCorridor] || s.Bought[(int)Upgrade.Jewelry]);
            Assert.IsFalse(s.JewelBench.Unlocked || s.JewelShop.Unlocked || s.LineUnlocked(Item.Jewel), "2a area fechada");
            Assert.AreEqual((5, 1, 0, 0), (s.Stock[2], s.Stock[3], s.Stock[4], s.Stock[5]));
            Assert.AreEqual((30, (int)Upgrade.PlayerSpeed), (s.Pads[8].Paid, s.Pads[8].Current(s)), "pads antigos nos mesmos indices (8 = Botas)");
            Assert.AreEqual(Balance.WorkshopW - 0.3f, s.Player.Pos.X, 1e-3f, "sem Corredor o save nao poe o jogador na rua");
            Assert.AreEqual((int)Upgrade.SideCorridor, PadFor(s, Upgrade.SideCorridor).Current(s));
            string again = s.Save(1001);
            StringAssert.Contains("up=11111111000000000000000\n", again);
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
            Assert.AreEqual((int)Upgrade.JewelSpeed, PadFor(s, Upgrade.JewelSpeed).Current(s));
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
            Assert.AreEqual((sword, Balance.QueueCap1), (s.ClientInterval(Item.Sword), s.QueueCap), "o balcao principal nao muda");
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
                int queue = direct ? s.JewelQueueCap : 1;
                for (int i = 0; i < queue; i++) s.JewelQueue.Add(new Client { Want = Item.Jewel, Patience = 999f, MaxPatience = 999f });
                s.Stock[(int)Item.Jewel] = 1;
                int gold = s.Gold, earned = s.GoldEarned;
                s.Tick(Dt, 0f, 0f);
                Assert.AreEqual((gold + price, earned + price, 1, 1, 0), (s.Gold, s.GoldEarned, s.Sales, s.SoldItems[(int)Item.Jewel], s.Stock[(int)Item.Jewel]));
                Assert.AreEqual(direct ? queue : queue - 1, s.JewelQueue.Count, "compra direta preserva a fila cheia; atendimento remove o nobre");
                Assert.AreEqual(1, Count(s, Ev.Sold), "uma venda, sem pagamento duplicado");
                foreach (SimEvent e in s.Events)
                    if (e.Kind == Ev.Sold)
                    {
                        Assert.AreEqual(((int)Item.Jewel, price), (e.A, e.B));
                        Assert.IsTrue(At(e.Pos, s.JewelSlot(direct ? queue : 0)), "evento identifica o caminho fila/direta");
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
            s.Player.Item = Item.Ore; s.Player.Count = 2;
            Assert.AreEqual(Hint.OreToFurnace, s.CurrentHint());
            s.Chests[3].State = 1; s.Chests[2].State = 1;
            Assert.AreEqual((Hint.OpenChest, 2), (s.CurrentHint(), s.HintArg), "bau disponivel vem antes do que carrega; o 1o na ordem dos marcos");
            s.Gold = 100000;
            Assert.AreEqual(Hint.BuyPad, s.CurrentHint(), "pad pagavel continua na frente");
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
            for (int i = 0; i < Upgrades.ProductionCount; i++) s.Buy((Upgrade)i);
            return s;
        }

        [Test]
        public void Luxo_ExigeTodos20Produtivos_MesmoVitrineForaDaOrdem()
        {
            for (int missing = 0; missing < Upgrades.ProductionCount; missing++)
            {
                var s = Rich();
                if (missing != (int)Upgrade.JewelVitrine) s.Buy(Upgrade.JewelVitrine);
                for (int i = 0; i < Upgrades.ProductionCount; i++) if (i != missing) s.Buy((Upgrade)i);
                Assert.IsFalse(s.ProductionComplete, "falta " + (Upgrade)missing);
                foreach (Upgrade u in Luxury)
                {
                    Assert.AreEqual(-1, PadFor(s, u).Current(s), "pad bloqueado: " + (Upgrade)missing);
                    Assert.IsFalse(s.Buy(u), "nem compra direta burla os 20 produtivos");
                }
            }
            var full = CompleteProduction();
            Assert.IsTrue(full.ProductionComplete);
            Assert.AreEqual((int)Upgrade.WorkshopFacade, full.Pads[17].Current(full));
            Assert.AreEqual(-1, full.Pads[18].Current(full));
            Assert.AreEqual(-1, full.Pads[19].Current(full));
            Assert.IsFalse(full.Buy(Upgrade.WorkshopFloor));
            Assert.IsFalse(full.Buy(Upgrade.JewelryDecor));
            Assert.AreEqual((20, 10), (full.Pads.Count, full.Stations.Count), "pads anexados, estacoes preservadas");
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
            int budget = 11000 + 14000 + 18000 + 333;
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
            Assert.AreEqual((333, 0, 23, 3), (s.Gold, s.GoldEarned, s.UpgradesBought, bought), "43000 de ralo, sem receita ou compra duplicada");
        }

        [Test]
        public void Save_Antigo20Flags17Pads_ELuxoIdaEVolta()
        {
            const string old = "v=1\nt=3600\ngold=7000\nup=11111111111111111110\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,450,\nstock=2,3,1,4\nsold=200,40,30,10\nms=2,2,1,2\nema=17\nsaved=500\nclaim=400\npx=12,9\n";
            Sim s = Sim.Load(old);
            Assert.AreEqual((19, 20, 450), (s.UpgradesBought, s.Pads.Count, s.Pads[16].Paid));
            Assert.IsFalse(s.ProductionComplete);
            foreach (Upgrade u in Luxury) { Assert.IsFalse(s.Bought[(int)u]); Assert.AreEqual(0, PadFor(s, u).Paid); }
            Assert.AreEqual((2, 3, 1, 4), (s.Stock[2], s.Stock[3], s.Stock[4], s.Stock[5]));
            s.Buy(Upgrade.JewelVitrine); s.Buy(Upgrade.WorkshopFacade); s.Buy(Upgrade.WorkshopFloor);
            s.Pads[19].Paid = 750;
            s.Pads[16].Paid = 0;
            string text = s.Save(501);
            StringAssert.Contains("up=11111111111111111111110\n", text);
            Sim again = Sim.Load(text);
            Assert.AreEqual(text, again.Save(501), "flags, ouro, marcos e pad parcial fazem roundtrip estavel");
            Assert.AreEqual((22, 750, (int)Upgrade.JewelryDecor), (again.UpgradesBought, again.Pads[19].Paid, again.Pads[19].Current(again)));
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
            Assert.AreEqual(4, luxury.Workers.Count, "luxo nao contrata");
        }

        [Test]
        public void Offline_IgnoraLuxo_TetoProdutivo18000_ClaimMaxIdempotente()
        {
            int[] bought = { 0, 11, 17, 20 }, expected = { 100, 1380, 4400, 18000 };
            for (int j = 0; j < bought.Length; j++)
            {
                var s = new Sim(); s.RateEma = 100;
                for (int i = 0; i < bought[j]; i++) s.Buy((Upgrade)i);
                Assert.AreEqual(expected[j], s.OfflineMaxGold(), "curva produtiva preservada");
                Assert.AreEqual(expected[j], s.ApplyOffline(long.MaxValue, 1));
            }
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
            StringAssert.Contains("up=11111111111111111000000\n", again);
            StringAssert.Contains("ms=0,0,0,0\n", again);
            Run(s, 0.1f);
            CollectionAssert.AreEqual(new[] { 0, 1, 0, 0 }, s.Chests.ConvertAll(c => c.State),
                "783 vendas: o bau de 50 aparece no 1o tick (o de 200 espera ele abrir; espadas por item contam do zero)");
        }
    }
}
