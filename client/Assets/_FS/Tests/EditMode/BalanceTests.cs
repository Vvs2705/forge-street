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

            // Limites folgados (FASE7: carga mista, balcao 4->8 e venda so com cliente na vaga; medido 2026-10-08: corredor 14:46,
            // joalheria 26:44, 9,0 joias/min, ouro/min 371 -> 792; BALANCE.md §17). Tempos x1,3 arredondados para cima em 10 s; piso 70%.
            Assert.That(corridor, Is.InRange(1f, 1160f), "Corredor ate ~medido + 30%");
            Assert.That(jewelry, Is.InRange(corridor, 2090f), "Joalheria depois do Corredor, ate ~medido + 30%");
            Assert.Greater(jewelsPerMin, 6.3f, "a linha de joias anda (medido 9,0/min; piso 70%)");
            Assert.Greater(after, before, "a Joalheria aumenta a renda (medido +113%)");
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
            int jewels = 0, nextMinute = 1, vipArrived = 0, vipServed = 0, vipLeft = 0, vipGold = 0;   // FASE8: VIP natural (o bot nao assiste anuncio)
            float starveAtJewelry = 0f, starveAtJeweler = 0f, starveAtLupa = 0f;
            var log = new StringBuilder();
            for (float t = 0f; t < 60f * 60f; t += Dt)
            {
                bot.Step(s, Dt);
                foreach (SimEvent e in s.Events)
                {
                    if (e.Kind == Ev.Sold && e.A == (int)Item.Jewel) jewels++;
                    if (e.Kind == Ev.VipArrived) vipArrived++;
                    if (e.Kind == Ev.VipServed) { vipServed++; vipGold += e.B; }
                    if (e.Kind == Ev.VipLeft) vipLeft++;
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
            for (int i = 0; i < Upgrades.Count; i++) if (!Upgrades.IsLuxury(i)) { all &= s.UpgradeTime[i] >= 0f; lastBuy = Math.Max(lastBuy, s.UpgradeTime[i]); }
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
            float vipShare = 100f * vipGold / Math.Max(1, s.GoldEarned);
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, "VIP: sorteados={0} entraram={1} levaram={2} cansaram={3} ouro={4} ({5:0.0}% da receita)", s.VipCount, vipArrived, vipServed, vipLeft, vipGold, vipShare));
            // FASE9: encomendas entregues, premio (fora do GoldEarned) como % da receita de vendas, minutos por encomenda depois da 1a venda
            float orderShare = 100f * s.OrderGold / Math.Max(1, s.GoldEarned);
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, "encomendas: entregues={0} premio={1} ({2:0.0}% da receita) | {3:0.0} min por encomenda | aberta: {4} {5}/{6}",
                s.OrderCount, s.OrderGold, orderShare, (end - s.FirstSaleTime) / 60f / Math.Max(1, s.OrderCount), s.OrderItem, s.OrderProgress, s.OrderTarget));
            Assert.That(orderShare, Is.InRange(3f, 10f), "FASE9: premio das encomendas entre 3% e 10% da receita");
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "joalheiro={0} lupa={1} vitrine de joias={2} | producao completa={3} | joias/min: joalheria->joalheiro={4:0.0} joalheiro->lupa={5:0.0} depois da producao={6:0.0} | " +
                "bancada de joias sem lingote: antes do joalheiro={7:0}% joalheiro->lupa={8:0}% joalheiro->60 min={9:0}% | joias={10} | baus abertos: {11}",
                Clock(tJeweler), Clock(tLupa), Clock(tVitrine), all ? Clock(lastBuy) : "nao", before, afterJeweler, afterAll, starveBefore, starveJewelerToLupa, starveAfter, jewels,
                string.Join(" ", Array.ConvertAll(chestOpen, Clock))));

            // Limites folgados (FASE7; medido 2026-10-08: balcao 5-8 vagas 13:45-14:43, joalheiro 31:42, mineiro 34:35, joalheiro 2 36:47,
            // lupa 41:21, vitrine 48:24 = producao completa; baus 2:53 / 7:26 / 28:57 / 14:42; joias/min 4,3 -> 14,8; sem lingote joalheiro ->
            // 60 min 9%; ouro/min 55-60 = 1551). Tempos medidos x1,3 arredondados para cima em 10 s (a Vitrine de joias no teto do teste,
            // 60 min); pisos ~70% do medido (BALANCE.md §17).
            Assert.IsTrue(all, "nenhum dos 26 produtivos inalcancavel em 60 min");
            Assert.That(tJeweler, Is.InRange(tJewelry, 2480f), "Joalheiro depois da Joalheria, ate ~medido + 30%");
            Assert.That(U(Upgrade.Miner), Is.InRange(tJeweler, 2700f), "Mineiro depois do Joalheiro, ate ~medido + 30%");
            Assert.That(U(Upgrade.Jeweler2), Is.InRange(U(Upgrade.Miner), 2870f), "Joalheiro 2 depois do Mineiro, ate ~medido + 30%");
            Assert.That(tLupa, Is.InRange(tJewelry, 3230f), "Lupa ate ~medido + 30%");
            Assert.That(tVitrine, Is.InRange(tJewelry, 3600f), "Vitrine de joias dentro dos 60 min");
            Assert.That(U(Upgrade.Counter8), Is.InRange(U(Upgrade.CounterCapacity), 1150f), "as 4 vagas do balcao logo depois da Vitrine (medido 14:43)");
            float[] chestMax = { 230f, 580f, 2260f, 1150f };
            for (int i = 0; i < chestOpen.Length; i++)
                Assert.That(chestOpen[i], Is.InRange(1f, chestMax[i]), $"bau {i} ({s.Chests[i].Label}) aberto ate ~medido + 30%");
            Assert.Greater(afterAll, before, "joalheiro + par + lupa + vitrine aumentam as joias/min (medido 4,3 -> 14,8)");
            Assert.Greater(afterAll, 10.3f, "piso ~70% do medido (14,8 joias/min)");
            Assert.LessOrEqual(starveAfter, 35f, "criterio da fase 4: bancada de joias sem lingote <= 35% (medido 9%)");
            Assert.Greater((earned[60] - earned[55]) / 5f, 1085f, "piso ~70% do ouro/min medido no fim (1551)");
            // FASE8 (BALANCE.md §18; medido 2026-10-08: 11 VIPs sorteados, 11 levaram o pacote, 0 cansaram, 3.177 ouro = 6,0% da receita)
            Assert.GreaterOrEqual(vipServed, 8, "VIP natural a cada 4-6 min depois da 1a venda e o bot atende (medido 11; piso ~70%)");
            Assert.LessOrEqual(vipLeft, 1, "a paciencia de 120 s cobre o pacote (medido 0 cansaram)");
            Assert.That(vipShare, Is.InRange(4.2f, 10f), "o VIP vale 5-10% da receita (alvo do contrato; medido 6,0%, piso ~70%)");
        }

        /// <summary>
        /// Fase 3 (docs/FASE3_LUXO.md): o bot humano joga 90 min duas vezes no mesmo passo, com e sem os pads de luxo.
        /// Luxo e' so ralo: os 22 produtivos saem no mesmo tick, as partidas sao identicas ate a 1a compra de luxo e, dali
        /// em diante, ouro/min e vendas/min so diferem pelo desvio do bot ate o pad (medido +0,5% / +0,1%). O saldo final
        /// difere exatamente pelo preco do luxo (54.000 desde a Leva 11). Limites folgados (medido + 30%, BALANCE.md §15): portao de regressao.
        /// </summary>
        [Test]
        public void Bot_90Minutos_Luxo()
        {
            const int M = 90;
            var lux = new Sim(); var plain = new Sim();
            plain.Pads.RemoveAll(p => Upgrades.IsLuxury((int)p.Chain[0]));   // baseline: luxo nunca a venda
            Assert.AreEqual(lux.Pads.Count - 3, plain.Pads.Count);
            Bot bl = new Bot(), bp = new Bot();
            int[] earnedL = new int[M + 1], earnedP = new int[M + 1], salesL = new int[M + 1], salesP = new int[M + 1], goldL = new int[M + 1], goldP = new int[M + 1];
            for (int next = 1; next <= M;)
            {
                bl.Step(lux, Dt); bp.Step(plain, Dt);
                if (lux.Time < next * 60f) continue;
                earnedL[next] = lux.GoldEarned; earnedP[next] = plain.GoldEarned; salesL[next] = lux.Sales; salesP[next] = plain.Sales; goldL[next] = lux.Gold; goldP[next] = plain.Gold;
                next++;
            }
            float tProd = 0f;
            for (int i = 0; i < Upgrades.Count; i++)
            {
                if (Upgrades.IsLuxury(i)) continue;
                Assert.GreaterOrEqual(lux.UpgradeTime[i], 0f, "produtivo comprado: " + (Upgrade)i);
                Assert.AreEqual(plain.UpgradeTime[i], lux.UpgradeTime[i], "luxo nao mexe na curva produtiva: " + (Upgrade)i);
                tProd = Math.Max(tProd, lux.UpgradeTime[i]);
            }
            float tF = lux.UpgradeTime[(int)Upgrade.WorkshopFacade], tW = lux.UpgradeTime[(int)Upgrade.WorkshopFloor], tJ = lux.UpgradeTime[(int)Upgrade.JewelryDecor];
            int mp = (int)Math.Ceiling(tProd / 60f);
            float gL = (earnedL[M] - earnedL[mp]) / (float)(M - mp), gP = (earnedP[M] - earnedP[mp]) / (float)(M - mp);
            float sL = (salesL[M] - salesL[mp]) / (float)(M - mp), sP = (salesP[M] - salesP[mp]) / (float)(M - mp);
            var windows = new StringBuilder("janela | ouro/min luxo|sem | saldo luxo|sem | vendas/min luxo|sem\n");
            for (int m = 5; m <= M; m += 5)
                windows.AppendLine($"  {m - 5}-{m}: {(earnedL[m] - earnedL[m - 5]) / 5}|{(earnedP[m] - earnedP[m - 5]) / 5}  {goldL[m]}|{goldP[m]}  {(salesL[m] - salesL[m - 5]) / 5f:0.0}|{(salesP[m] - salesP[m - 5]) / 5f:0.0}");
            TestContext.WriteLine(Report(lux));
            TestContext.WriteLine(windows.ToString());
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "producao completa={0} | fachada={1} (+{2:0.0} min) piso={3} (+{4:0.0}) joalheria real={5} (+{6:0.0}) | {7}-{8} min: ouro/min {9:0.0} x {10:0.0} ({11:+0.0;-0.0}%), vendas/min {12:0.00} x {13:0.00} ({14:+0.0;-0.0}%) | 90 min: receita {15} x {16}, saldo {17} x {18}",
                Clock(tProd), Clock(tF), (tF - tProd) / 60f, Clock(tW), (tW - tProd) / 60f, Clock(tJ), (tJ - tProd) / 60f, mp, M, gL, gP, 100f * (gL / gP - 1f), sL, sP, 100f * (sL / sP - 1f),
                lux.GoldEarned, plain.GoldEarned, lux.Gold, plain.Gold));

            // medido 2026-10-08 (FASE7; luxo 14.000/18.000/22.000): producao 48:24, fachada 56:57, piso 68:09, joalheria real 82:08; tempos
            // x1,3 arredondados para cima em 10 s; o 3o fica no teto de 90 min pedido pelo contrato (BALANCE.md §17)
            Assert.That(tF, Is.InRange(tProd, 4450f), "Fachada nobre depois da producao completa, ate ~medido + 30%");
            Assert.That(tW, Is.InRange(tF, 5320f), "Piso de oficina depois da Fachada, ate ~medido + 30%");
            Assert.That(tJ, Is.InRange(tW, M * 60f), "Joalheria real depois do Piso, dentro dos 90 min");
            for (int m = 1; m * 60f <= tF; m++)
                Assert.AreEqual((earnedP[m], salesP[m]), (earnedL[m], salesL[m]), $"identicas ate a 1a compra de luxo (minuto {m})");
            Assert.That(gL / gP, Is.InRange(0.97f, 1.03f), "luxo nao altera ouro/min (medido +0,5%)");
            Assert.That(sL / sP, Is.InRange(0.97f, 1.03f), "luxo nao altera vendas/min (medido +0,1%)");
            int luxuryCost = Upgrades.Cost(Upgrade.WorkshopFacade) + Upgrades.Cost(Upgrade.WorkshopFloor) + Upgrades.Cost(Upgrade.JewelryDecor);
            Assert.AreEqual(plain.Gold + (lux.GoldEarned - plain.GoldEarned) + (lux.OrderGold - plain.OrderGold) - luxuryCost, lux.Gold,
                "o luxo cobra exatamente o preco, uma vez, sem devolver ouro (premio das encomendas fica fora do GoldEarned)");
            Assert.AreEqual((Upgrades.Count, Upgrades.ProductionCount), (lux.UpgradesBought, plain.UpgradesBought));
            Assert.AreEqual(plain.OfflineMaxGold(), lux.OfflineMaxGold(), "luxo nao mexe no teto offline");
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

        /// <summary>
        /// Revisao v0.5 #1: a promessa idle e' a oficina vender sozinha. Bot joga 25 min e larga: o Ajudante 3 parava no balcao com espada
        /// e a prateleira de espada cheia, e escudo/ferramenta encalhavam (medido: 80 vendas e 107 perdidos em 15 min; corrigido: 209 e 59).
        /// Portao: os ajudantes sozinhos fazem pelo menos 150 vendas.
        /// </summary>
        [Test]
        public void Ocioso_15Min_AjudantesVendemSozinhos()
        {
            var s = new Sim();
            Play(s, 1500f);   // bot monta a oficina (Ajudante 3 sai aos ~19 min) e larga o jogo
            Assert.IsTrue(s.Bought[(int)Upgrade.Helper3], "Ajudante 3 comprado");
            int sales = s.Sales;
            for (float t = 0f; t < 900f; t += Dt) s.Tick(Dt, 0f, 0f);
            TestContext.WriteLine($"ocioso 15 min depois de 25 min de bot: vendas={s.Sales - sales} perdidos={s.ClientsLost} estoque={string.Join(",", s.Stock)}");
            Assert.GreaterOrEqual(s.Sales - sales, 150);
        }
    }
}
