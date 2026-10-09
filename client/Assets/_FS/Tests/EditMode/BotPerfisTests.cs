using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>
    /// A-CORE-07, Bot 2.0: perfil = dados (BotProfile). O humano dos BalanceTests e' o F2P (sem anuncio) e ele e o Ideal mantem a
    /// assinatura; Slow e AdWatcher sao os perfis novos (o econsim roda os 4).
    /// </summary>
    public class BotPerfisTests
    {
        static Sim Play(Bot bot, float seconds)
        {
            var s = new Sim();
            BalanceTests.Play(s, seconds, bot);
            return s;
        }

        /// <summary>Quando saiu o ultimo produtivo; nao completou = infinito.</summary>
        static float Producao(Sim s)
        {
            if (!s.ProductionComplete) return float.MaxValue;
            float t = 0f;
            for (int i = 0; i < Upgrades.Count; i++) if (!Upgrades.IsLuxury(i)) t = System.Math.Max(t, s.UpgradeTime[i]);
            return t;
        }

        [Test]
        public void PerfisAntigos_MesmaAssinatura_HumanoEOF2P()
        {
            Bot h = new Bot(), i = Bot.Ideal();
            Assert.AreEqual((0.7f, 0.85f, false), (h.Reaction, h.Stick, h.Ads), "humano = F2P: 0,7 s, 85% de stick, sem anuncio");
            Assert.AreEqual((0f, 1f, false), (i.Reaction, i.Stick, i.Ads), "ideal sem handicap e sem anuncio");
            Assert.AreEqual(BalanceTests.Report(Play(new Bot(), 240f)), BalanceTests.Report(Play(Bot.From(BotProfile.F2P), 240f)), "new Bot() e' o perfil F2P ao tick");
        }

        [Test]
        public void Slow_ChegaAProducaoCompletaDepoisDoHumano()
        {
            Sim human = Play(new Bot(), 3600f), slow = Play(Bot.From(BotProfile.Slow), 3600f);
            TestContext.WriteLine($"producao completa: humano {BalanceTests.Clock(Producao(human))} | lento {(slow.ProductionComplete ? BalanceTests.Clock(Producao(slow)) : "nao em 60 min")}");
            Assert.IsTrue(human.ProductionComplete, "o humano completa em 60 min");
            Assert.Greater(Producao(slow), Producao(human), "o lento completa depois (ou nao completa)");
        }

        [Test]
        public void AdWatcher_UsaVipEBoostEm30Min_NaoFicaMaisLentoQueOF2P()
        {
            Bot ad = Bot.From(BotProfile.AdWatcher);
            Sim f2p = Play(new Bot(), 1800f), adSim = Play(ad, 1800f);
            TestContext.WriteLine($"30 min: anuncios VIP={ad.AdsVip} boost={ad.AdsBoost} | upgrades {adSim.UpgradesBought} x {f2p.UpgradesBought} | receita {adSim.GoldEarned} x {f2p.GoldEarned}");
            Assert.GreaterOrEqual(ad.AdsVip, 1, "chamou o VIP");
            Assert.GreaterOrEqual(ad.AdsBoost, 1, "ligou o boost");
            Assert.GreaterOrEqual(adSim.UpgradesBought, f2p.UpgradesBought, "nao compra menos que o F2P");
            Assert.Greater(adSim.GoldEarned, f2p.GoldEarned, "fatura mais que o F2P");
        }
    }
}
