using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// A-CORE-03: `Tuning` de instancia (o que o RemoteConfig e os eventos ajustam) e feature flags. Padrao = assinatura de sempre (os
    /// BalanceTests e o econsim provam ao tick); um knob mexido muda o resultado; o teto offline de 2 h e' trava; cada flag desligada para o
    /// sistema sem quebrar o save (o que ja estava em andamento termina, o estado vai e volta intacto e religar continua de onde parou).
    /// </summary>
    public class TuningTests
    {
        const float Dt = 1f / 30f;
        const long Now = 1000000;

        static void Run(Sim s, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt, 0f, 0f);
        }

        static int Count(Sim s, Ev kind) => s.Events.FindAll(e => e.Kind == kind).Count;

        /// <summary>Roda e conta um evento em todos os ticks.</summary>
        static int RunCount(Sim s, float seconds, Ev kind)
        {
            int n = 0;
            for (float t = 0f; t < seconds; t += Dt) { s.Tick(Dt, 0f, 0f); n += Count(s, kind); }
            return n;
        }

        /// <summary>O save vai e volta com o mesmo Tuning sem perder nada (texto identico).</summary>
        static void SaveEstavel(Sim s, Tuning t)
        {
            string text = s.Save(Now);
            Assert.AreEqual(text, Sim.Load(text, t).Save(Now), "save estavel com a flag desligada");
        }

        [Test]
        public void Padrao_FlagsLigadas_IntersticialDesligado_EPadraoEhCopia()
        {
            var t = new Tuning();
            Assert.IsTrue(t.Encomendas && t.Vip && t.VipChamado && t.Velocidade && t.Baus, "sistemas de sempre ligados");
            Assert.IsFalse(t.Interstitial, "P3/V10: intersticial so como flag, desligado por padrao");
            Tuning.Padrao.Price[(int)Item.Sword] = 999;
            Assert.AreEqual(10, Tuning.Padrao.Price[(int)Item.Sword], "Padrao e' copia nova: mexer nele nao muda o padrao de ninguem");
            Assert.AreEqual(10, new Sim().T.Price[(int)Item.Sword]);
            var a = new Sim(); var b = new Sim(new Tuning());
            BalanceTests.Play(a, 120f); BalanceTests.Play(b, 120f);
            Assert.AreEqual(BalanceTests.Report(a), BalanceTests.Report(b), "new Sim() = new Sim(new Tuning()) ao tick");
        }

        [Test]
        public void TuningAlterado_PrecoDaEspada_MudaOResultado()
        {
            var t = new Tuning(); t.Price[(int)Item.Sword] = 20;
            Sim padrao = new Sim(), caro = new Sim(t);
            Bot bp = new Bot(), bc = new Bot();
            int first = -1;
            for (float s = 0f; s < 120f; s += Dt)
            {
                bp.Step(padrao, Dt);
                bc.Step(caro, Dt);
                foreach (SimEvent e in caro.Events) if (e.Kind == Ev.Sold && first < 0) first = e.B;
            }
            Assert.AreEqual(20, first, "a espada vende pelo preco do Tuning");
            Assert.AreEqual(10, padrao.PriceOf(Item.Sword));
            Assert.Greater(caro.GoldEarned, padrao.GoldEarned, "o mesmo bot fatura mais com a espada a 20");
            t.CostBase = 100;
            Assert.AreEqual((100L, 50L), (caro.T.Cost(Upgrade.FurnaceSpeed1), padrao.T.Cost(Upgrade.FurnaceSpeed1)), "custo base do Tuning da partida");
        }

        /// <summary>Guardrail: offline com teto de 2 h. O Tuning pode encurtar (evento), nunca passar das 7.200 s da trava do nucleo.</summary>
        [Test]
        public void Offline_TuningAcimaDe2h_FicaNaTrava_AbaixoEncurta()
        {
            Sim Rate1(Tuning t)
            {
                var s = new Sim(t);
                for (int i = 0; i < Upgrades.Count; i++) s.Buy((Upgrade)i);   // teto relativo (2x 9.000) nao interfere
                s.RateEma = 1.0;
                return s;
            }
            var longo = new Tuning { OfflineMaxSeconds = 86400 };
            Assert.AreEqual(7200L, longo.OfflineCap, "24 h pedidas: vale a trava de 2 h");
            Assert.AreEqual(1800, Rate1(longo).ApplyOffline(8 * 3600, 1), "8 h fora com o Tuning de 24 h paga 2 h (25% de 1 ouro/s x 7.200)");
            Assert.AreEqual(1800, Rate1(null).ApplyOffline(8 * 3600, 1), "padrao: 2 h");
            Assert.AreEqual(900, Rate1(new Tuning { OfflineMaxSeconds = 3600 }).ApplyOffline(8 * 3600, 1), "encurtar para 1 h vale");
        }

        [Test]
        public void Flag_Encomendas_Desligada_NaoAbreNova_AAbertaEntrega_SaveIntacto()
        {
            var off = new Tuning { Encomendas = false };
            var s = new Sim(off) { FirstSaleTime = 1f };
            Assert.AreEqual(0, RunCount(s, 120f, Ev.OrderNew), "nenhuma encomenda nova");
            Assert.AreEqual((-1, 0, off.OrderFirstDelay), (s.OrderItem, s.OrderCount, s.OrderIn), "relogio parado");
            SaveEstavel(s, off);
            Sim on = Sim.Load(s.Save(Now));   // religada: continua de onde parou
            Assert.AreEqual(1, RunCount(on, off.OrderFirstDelay + 1f, Ev.OrderNew), "religar abre a 1a no horario");
            s.OrderItem = (int)Item.Sword; s.OrderTarget = 5; s.OrderProgress = 5; s.OrderReward = 100;   // aberta antes de desligar
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((1, 100L, -1), (Count(s, Ev.OrderDone), s.OrderGold, s.OrderItem), "a aberta ainda entrega e paga");
            Assert.AreEqual(0, RunCount(s, 60f, Ev.OrderNew), "e a seguinte nao vem");
        }

        [Test]
        public void Flag_Vip_Desligada_NaoSorteiaNemChama_OPendenteEntra_SaveIntacto()
        {
            var off = new Tuning { Vip = false };
            var s = new Sim(off) { FirstSaleTime = 1f };
            float clock = s.VipIn;
            Assert.AreEqual(0, RunCount(s, 400f, Ev.VipArrived), "nenhum VIP natural");
            Assert.AreEqual((clock, 0), (s.VipIn, s.VipCount), "relogio parado");
            Assert.IsFalse(s.CanSummonVip || s.SummonVip(), "sem chamar por anuncio");
            SaveEstavel(s, off);
            var d = new Sim { FirstSaleTime = 1f, VipIn = 200f };   // VIP chamado antes de desligar, ainda sem vaga
            Assert.IsTrue(d.SummonVip());
            Sim p = Sim.Load(d.Save(Now), off);
            Assert.AreEqual(1, RunCount(p, 1f, Ev.VipArrived), "o pendente do save entra e leva o pacote");
        }

        [Test]
        public void Flag_VipChamado_Desligada_SoONatural()
        {
            var off = new Tuning { VipChamado = false };
            var s = new Sim(off) { FirstSaleTime = 1f, VipIn = 200f };
            Assert.IsFalse(s.CanSummonVip || s.SummonVip(), "sem o anuncio de chamar");
            SaveEstavel(s, off);
            s.VipIn = 0.1f;
            Assert.AreEqual(1, RunCount(s, 1f, Ev.VipArrived), "o natural continua");
        }

        [Test]
        public void Flag_Velocidade_Desligada_NaoLiga_OAtivoAcabaNoTempo_SaveIntacto()
        {
            var off = new Tuning { Velocidade = false };
            var s = new Sim(off);
            Assert.AreEqual((false, 0f, 1f), (s.CanBoost, s.StartBoost(), s.BoostMul), "sem boost");
            var d = new Sim();
            d.StartBoost();   // anuncio visto antes de desligar
            Sim b = Sim.Load(d.Save(Now), off);
            Assert.AreEqual((2f, false, 0f), (b.BoostMul, b.CanBoost, b.StartBoost()), "o ativo continua; nao sobe para 3x");
            SaveEstavel(b, off);
            Run(b, off.BoostSeconds + 0.1f);
            Assert.AreEqual((1f, 0f), (b.BoostMul, b.BoostLeft), "acaba no tempo dele");
        }

        [Test]
        public void Flag_Baus_Desligada_NenhumBauNovo_OdaTelaAbre_SaveIntacto()
        {
            var off = new Tuning { Baus = false };
            var s = new Sim(off);
            s.SoldItems[(int)Item.Sword] = 15;   // marco "15 espadas" batido
            Assert.AreEqual(0, RunCount(s, 1f, Ev.Milestone), "nenhum bau aparece");
            Assert.AreEqual(0, s.Chests[0].State);
            SaveEstavel(s, off);
            Sim on = Sim.Load(s.Save(Now));
            Assert.AreEqual(1, RunCount(on, 1f, Ev.Milestone), "religar mostra o bau do marco ja batido");
            s.Chests[0].State = 1; s.Player.Pos = s.Chests[0].Pos;   // estava na tela antes de desligar
            Assert.AreEqual(1, RunCount(s, 0.1f, Ev.ChestOpened), "o da tela ainda abre");
        }
    }
}
