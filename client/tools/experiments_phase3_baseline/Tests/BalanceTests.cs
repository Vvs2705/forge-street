using System;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>
    /// O Bot joga os primeiros minutos e precisa bater a §3 do GDD. O bot e' mais rapido que um humano (nao erra o
    /// joystick, nao le a tela), entao ele tem de chegar em cada marco ANTES do tempo do GDD. Os numeros medidos
    /// vao para docs/BALANCE.md; o teste e' o portao que impede uma mudanca de Balance de quebrar a curva.
    /// </summary>
    public class BalanceTests
    {
        public const float Dt = 1f / 30f;

        public static string Clock(float s) => s < 0f ? "--:--" : $"{(int)(s / 60f)}:{(int)(s % 60f):00}";

        /// <summary>Roda o bot por `seconds` e devolve o relatorio minuto a minuto (o mesmo que o -autoplay do build loga).</summary>
        public static string Play(Sim s, float seconds, Bot bot = null, float dt = Dt)
        {
            bot = bot ?? new Bot();
            var sb = new StringBuilder();
            int nextMinute = 1;
            for (float t = 0f; t < seconds; t += dt)
            {
                bot.Step(s, dt);
                if (s.Time < nextMinute * 60f) continue;
                sb.AppendLine(Line(s));
                nextMinute++;
            }
            return sb.ToString();
        }

        public static string Line(Sim s) => string.Format(CultureInfo.InvariantCulture,
            "AUTOPLAY t={0} gold={1} earned={2} upgrades={3} sales={4} lost={5} away={6} maxq={7} walk={8:0}s ema={9:0.00}/s",
            Clock(s.Time), s.Gold, s.GoldEarned, s.UpgradesBought, s.Sales, s.ClientsLost, s.ClientsTurnedAway, s.MaxQueue, s.WalkNoDecision, s.RateEma);

        public static string Report(Sim s)
        {
            var sb = new StringBuilder();
            sb.AppendLine("primeira venda: " + Clock(s.FirstSaleTime));
            for (int i = 0; i < Upgrades.Count; i++)
                sb.AppendLine($"  {Clock(s.UpgradeTime[i])}  {(Upgrade)i} ({Upgrades.Cost(i)} ouro)");
            foreach (Station st in s.Stations)
                if (st.Produces) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0,-12} fome={1,5:0}s travada={2,5:0}s feitos={3}", st.Name, st.StarveSeconds, st.StallSeconds, st.Crafted));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  andar sem decisao: {0:0}s de {1:0}s ({2:0}%)", s.WalkNoDecision, s.Time, 100f * s.WalkNoDecision / Math.Max(1f, s.Time)));
            sb.AppendLine($"  clientes perdidos={s.ClientsLost} sem vaga={s.ClientsTurnedAway} fila max={s.MaxQueue} vendas={s.Sales}");
            var pads = new StringBuilder("  pads parciais:");
            foreach (Pad p in s.Pads) if (p.Paid > 0) pads.Append($" {(p.Current(s) >= 0 ? ((Upgrade)p.Current(s)).ToString() : "?")}={p.Paid}");
            sb.AppendLine(pads.ToString());
            return sb.ToString();
        }

        /// <summary>
        /// Marco da §3 (fim da janela, em s) e a fracao minima aceita para o bot humano. O bot e' 1,3-2x mais rapido que
        /// uma pessoa, e a diferenca e' maior no 1o minuto (aprender o joystick, achar as estacoes): por isso o fole
        /// aceita 30% e os demais 50%. HIPOTESE: calibrar com os `upgrade_buy` do diario.csv do playtest.
        /// </summary>
        static void Marco(Sim s, Upgrade u, float gddSeconds, float minFraction, string label)
        {
            Assert.That(s.UpgradeTime[(int)u], Is.InRange(gddSeconds * minFraction, gddSeconds), $"§3 {label}: {u} em {Clock(s.UpgradeTime[(int)u])}, GDD ate {Clock(gddSeconds)}");
        }

        [Test]
        public void Bot_Primeiros10Minutos_BatemASecao3()
        {
            var ideal = new Sim();
            Play(ideal, 600f, Bot.Ideal());
            TestContext.WriteLine("--- bot ideal (sem handicap): limite inferior ---");
            TestContext.WriteLine(Report(ideal));

            var s = new Sim();
            string log = Play(s, 600f);
            TestContext.WriteLine("--- bot humano (0,7 s de reacao, 85% de stick) ---");
            TestContext.WriteLine(log);
            TestContext.WriteLine(Report(s));

            Assert.That(s.FirstSaleTime, Is.InRange(1f, 60f), "§15: 1a venda em <90 s (bot: <60 s)");
            Marco(s, Upgrade.FurnaceSpeed1, 150f, 0.30f, "1:30-2:30 fole");
            Marco(s, Upgrade.Anvil2, 210f, 0.50f, "2:30-3:30 2a bigorna");
            Marco(s, Upgrade.Helper1, 270f, 0.50f, "3:30-4:30 ajudante");
            Marco(s, Upgrade.Shields, 330f, 0.50f, "4:30-5:30 escudos");
            Marco(s, Upgrade.Conveyor, 510f, 0.50f, "8:30 esteira");
            Assert.Less(s.UpgradesBought, Upgrades.Count, "em 10 min nao pode ter comprado tudo: sobra razao para voltar");
            Assert.Less(s.WalkNoDecision / s.Time, 0.5f, "kill criterion: 'andar entre pilhas sem decisao' nao pode dominar");
        }

        /// <summary>
        /// 2a area (docs/AREA2_JOALHERIA.md): o bot humano joga 45 min e MEDE quando compra Corredor e Joalheria, quantas
        /// joias vende e o ouro/min antes e depois. Os limites sao folgados (medido + ~30%, ver BALANCE.md §9): protegem
        /// contra regressao, nao fixam a curva.
        /// </summary>
        [Test]
        public void Bot_45Minutos_SegundaArea_CorredorEJoalheria()
        {
            var s = new Sim(); var bot = new Bot();
            var earned = new int[46];   // GoldEarned no fim de cada minuto
            int jewels = 0, jewelGold = 0, jewelTired = 0, jewelAway = 0, nextMinute = 1;
            var log = new StringBuilder();
            for (float t = 0f; t < 45f * 60f; t += Dt)
            {
                bot.Step(s, Dt);
                foreach (SimEvent e in s.Events)
                {
                    if (e.A != (int)Item.Jewel) continue;
                    if (e.Kind == Ev.Sold) { jewels++; jewelGold += e.B; }
                    if (e.Kind == Ev.ClientLeft) { if (e.B == 2) jewelAway++; else jewelTired++; }
                }
                if (s.Time < nextMinute * 60f || nextMinute > 45) continue;
                earned[nextMinute] = s.GoldEarned;
                log.AppendLine(Line(s) + $" joias={jewels}");
                nextMinute++;
            }
            float corridor = s.UpgradeTime[(int)Upgrade.SideCorridor], jewelry = s.UpgradeTime[(int)Upgrade.Jewelry];
            TestContext.WriteLine(log.ToString());
            TestContext.WriteLine(Report(s));
            // ouro/min em janelas de 5 min: antes do Corredor, antes da Joalheria, depois da Joalheria, ultimos 5 min
            float PerMin(int m0, int m1) => m1 > m0 && m0 >= 0 && m1 <= 45 ? (earned[m1] - earned[m0]) / (float)(m1 - m0) : -1f;
            int mc = corridor < 0f ? 45 : (int)(corridor / 60f), mj = jewelry < 0f ? 45 : (int)(jewelry / 60f);
            float before = PerMin(Math.Max(0, mj - 5), mj), after = PerMin(mj + 1, Math.Min(45, mj + 6));
            float jewelsPerMin = jewelry < 0f ? 0f : jewels / ((45f * 60f - jewelry) / 60f);
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "corredor={0} joalheria={1} | joias: vendidas={2} ({3} ouro = {4:0}% do ganho depois da joalheria, {5:0.0}/min) cansaram={6} sem vaga={7} feitas={8}\n" +
                "ouro/min: 5 min antes do corredor={9:0} | 5 min antes da joalheria={10:0} | 5 min depois da joalheria={11:0} | min 40-45={12:0}",
                Clock(corridor), Clock(jewelry), jewels, jewelGold, jewelry < 0f ? 0f : 100f * jewelGold / Math.Max(1, earned[45] - earned[Math.Min(45, mj)]), jewelsPerMin,
                jewelTired, jewelAway, s.Crafted[(int)Item.Jewel], PerMin(Math.Max(0, mc - 5), mc), before, after, PerMin(40, 45)));

            // Limites folgados (custos 690/1515, medido 2026-10-07: corredor 15:18, joalheria 26:02, 5,2 joias/min, ouro/min 516 -> 925; BALANCE.md §9)
            Assert.That(corridor, Is.InRange(1f, 20f * 60f), "Corredor ate ~medido + 30%");
            Assert.That(jewelry, Is.InRange(corridor, 34f * 60f), "Joalheria depois do Corredor, ate ~medido + 30%");
            Assert.Greater(jewelsPerMin, 3.6f, "a linha de joias anda (medido 5,2/min; piso 70%)");
            Assert.Greater(after, before, "a Joalheria aumenta a renda (medido +79%)");
        }

        /// <summary>
        /// Fase 2 (docs/AREA2_FASE2.md): o bot humano joga 60 min e MEDE quando compra Joalheiro/Lupa/Vitrine de joias,
        /// quando abre cada bau, joias/min e % da bancada de joias sem lingote antes e depois do Joalheiro, e ouro/min por
        /// janela de 5 min. Limites folgados (medido + 30%, BALANCE.md §10): portao de regressao, nao fixam a curva.
        /// </summary>
        [Test]
        public void Bot_60Minutos_Fase2()
        {
            var s = new Sim(); var bot = new Bot();
            var earned = new int[61]; var jewelsAt = new int[61];
            var chestOpen = new float[s.Chests.Count];
            for (int i = 0; i < chestOpen.Length; i++) chestOpen[i] = -1f;
            int jewels = 0, nextMinute = 1;
            float starveAtJewelry = 0f, starveAtJeweler = 0f, starveAtLupa = 0f;
            var log = new StringBuilder();
            for (float t = 0f; t < 60f * 60f; t += Dt)
            {
                bot.Step(s, Dt);
                foreach (SimEvent e in s.Events)
                {
                    if (e.Kind == Ev.Sold && e.A == (int)Item.Jewel) jewels++;
                    if (e.Kind == Ev.ChestOpened) { chestOpen[e.A] = s.Time; log.AppendLine($"  {Clock(s.Time)} bau {e.A} ({s.Chests[e.A].Label}) aberto, +{e.B}"); }
                    if (e.Kind == Ev.Milestone) log.AppendLine($"  {Clock(s.Time)} marco {e.A} ({s.Chests[e.A].Label}) bateu");
                    if (e.Kind == Ev.Bought && e.A == (int)Upgrade.Jewelry) starveAtJewelry = s.JewelBench.StarveSeconds;
                    if (e.Kind == Ev.Bought && e.A == (int)Upgrade.Jeweler) starveAtJeweler = s.JewelBench.StarveSeconds;
                    if (e.Kind == Ev.Bought && e.A == (int)Upgrade.JewelSpeed) starveAtLupa = s.JewelBench.StarveSeconds;
                }
                if (s.Time < nextMinute * 60f || nextMinute > 60) continue;
                earned[nextMinute] = s.GoldEarned; jewelsAt[nextMinute] = jewels;
                if (nextMinute % 5 == 0) log.AppendLine(Line(s) + $" joias={jewels}");
                nextMinute++;
            }
            float U(Upgrade u) => s.UpgradeTime[(int)u];
            float tJewelry = U(Upgrade.Jewelry), tJeweler = U(Upgrade.Jeweler), tLupa = U(Upgrade.JewelSpeed), tVitrine = U(Upgrade.JewelVitrine), end = 3600f;
            float lastBuy = 0f; bool all = true;
            for (int i = 0; i < Upgrades.Count; i++) { all &= s.UpgradeTime[i] >= 0f; lastBuy = Math.Max(lastBuy, s.UpgradeTime[i]); }
            // joias/min por fase (minutos inteiros), % da bancada de joias sem lingote antes/depois do Joalheiro
            float JewelsPerMin(float t0, float t1) { int m0 = (int)Math.Ceiling(t0 / 60f), m1 = (int)(t1 / 60f); return m1 > m0 ? (jewelsAt[m1] - jewelsAt[m0]) / (float)(m1 - m0) : -1f; }
            float before = JewelsPerMin(tJewelry, tJeweler), afterJeweler = JewelsPerMin(tJeweler, tLupa), afterAll = JewelsPerMin(Math.Max(tLupa, tVitrine), end);
            float starveBefore = 100f * (starveAtJeweler - starveAtJewelry) / Math.Max(1f, tJeweler - tJewelry);
            float starveJewelerToLupa = 100f * (starveAtLupa - starveAtJeweler) / Math.Max(1f, tLupa - tJeweler);
            float starveAfter = 100f * (s.JewelBench.StarveSeconds - starveAtJeweler) / Math.Max(1f, end - tJeweler);
            var windows = new StringBuilder("ouro/min por janela de 5 min:");
            for (int m = 5; m <= 60; m += 5) windows.Append($" {m - 5}-{m}={(earned[m] - earned[m - 5]) / 5}");
            TestContext.WriteLine(log.ToString());
            TestContext.WriteLine(Report(s));
            TestContext.WriteLine(windows.ToString());
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "joalheiro={0} lupa={1} vitrine de joias={2} | tudo comprado={3} | joias/min: joalheria->joalheiro={4:0.0} joalheiro->lupa={5:0.0} depois de tudo={6:0.0} | " +
                "bancada de joias sem lingote: antes do joalheiro={7:0}% joalheiro->lupa={8:0}% joalheiro->60 min={9:0}% | joias={10} | baus abertos: {11}",
                Clock(tJeweler), Clock(tLupa), Clock(tVitrine), all ? Clock(lastBuy) : "nao", before, afterJeweler, afterAll, starveBefore, starveJewelerToLupa, starveAfter, jewels,
                string.Join(" ", Array.ConvertAll(chestOpen, Clock))));

            // Limites folgados (custos 2200/5500/9000, joia 80 com Vitrine; medido 2026-10-07: joalheiro 25:25, lupa 31:12,
            // vitrine 40:41 = tudo comprado; baus 2:24 / 6:20 / 24:02 / 12:18; joias/min 5,7 -> 7,2; ouro/min 55-60 = 1095.
            // Tempos medidos x1,3 arredondados para cima em 10 s; pisos ~70% do medido (BALANCE.md §10.7).
            Assert.IsTrue(all, "nenhum upgrade inalcancavel em 60 min");
            Assert.That(tJeweler, Is.InRange(tJewelry, 1990f), "Joalheiro depois da Joalheria, ate ~medido + 30%");
            Assert.That(tLupa, Is.InRange(tJewelry, 2440f), "Lupa ate ~medido + 30%");
            Assert.That(tVitrine, Is.InRange(tJewelry, 3180f), "Vitrine de joias ate ~medido + 30%");
            float[] chestMax = { 190f, 500f, 1880f, 960f };
            for (int i = 0; i < chestOpen.Length; i++)
                Assert.That(chestOpen[i], Is.InRange(1f, chestMax[i]), $"bau {i} ({s.Chests[i].Label}) aberto ate ~medido + 30%");
            Assert.Greater(afterAll, before, "joalheiro + lupa + vitrine aumentam as joias/min (medido 5,7 -> 7,2)");
            Assert.Greater(afterAll, 5.0f, "piso ~70% do medido (7,2 joias/min)");
            Assert.Greater((earned[60] - earned[55]) / 5f, 766f, "piso ~70% do ouro/min medido no fim (1095)");
        }

        [Test]
        public void Determinismo_MesmaEntradaMesmoResultado()
        {
            var a = new Sim(); var b = new Sim();
            string la = Play(a, 240f), lb = Play(b, 240f);
            Assert.AreEqual(la, lb, "log minuto a minuto identico (ouro, vendas, perdidos, caminhada, ema)");
            Assert.AreEqual(a.Player.Pos.ToString(), b.Player.Pos.ToString());
            Assert.AreEqual(a.Save(1), b.Save(1));
            Assert.AreEqual(Report(a), Report(b));
        }

        [Test]
        public void Offline_DepoisDe10Min_RendeAlgoMasNaoMaisQueOTeto()
        {
            var s = new Sim();
            Play(s, 600f);
            Assert.Greater(s.RateEma, 0.1, "taxa online medida existe");
            int raw = (int)Math.Floor(s.RateEma * Balance.OfflineFactor * Balance.OfflineCapSeconds);
            int gold = s.ApplyOffline(8 * 3600, 1);
            Assert.AreEqual(Math.Min(raw, s.OfflineMaxGold()), gold);
            Assert.LessOrEqual(gold, 2 * s.CheapestLockedCost(), "o cofre nunca compra o resto do jogo sozinho");
            TestContext.WriteLine($"offline 8 h depois de 10 min de jogo: {gold} ouro (taxa {s.RateEma:0.00}/s; pela taxa seriam {raw}; proximo upgrade travado custa {s.CheapestLockedCost()})");
        }
    }
}
