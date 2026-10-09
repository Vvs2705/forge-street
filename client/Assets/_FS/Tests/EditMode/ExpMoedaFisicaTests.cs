using System;
using System.Globalization;
using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>EXPERIMENTO B4 (branch exp/moeda-fisica, nao entra no jogo): moeda fisica no balcao x ouro direto. Bot humano 60 min.</summary>
    public class ExpMoedaFisicaTests
    {
        const float Dt = 1f / 30f;
        static string C(float s) => s < 0f ? "--:--" : $"{(int)(s / 60f)}:{(int)(s % 60f):00}";

        [Test]
        public void Exp_MoedaFisica_60Min()
        {
            TestContext.WriteLine("variante | 1a venda | upg 1/5/10/15 | producao completa | andar sem decisao (sem a ida a pilha) | ida a pilha | vendas | ganho | na pilha aos 60 min");
            foreach (int go in new[] { 0, 30, 60, 120 })
            {
                bool on = go > 0; Bot.PileGo = go;
                var s = new Sim { PhysicalCoins = on }; var bot = new Bot();
                for (float t = 0f; t < 3600f; t += Dt) bot.Step(s, Dt);
                var times = new System.Collections.Generic.List<float>();
                float prod = 0f; bool all = true;
                for (int i = 0; i < Upgrades.Count; i++)
                {
                    if (s.UpgradeTime[i] >= 0f) times.Add(s.UpgradeTime[i]);
                    if (!Upgrades.IsLuxury(i)) { all &= s.UpgradeTime[i] >= 0f; prod = Math.Max(prod, s.UpgradeTime[i]); }
                }
                times.Sort();
                string U(int k) => k <= times.Count ? C(times[k - 1]) : "--:--";
                TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} | {1} | {2}/{3}/{4}/{5} | {6} | {7:0}% ({8:0}%) | {9:0}% | {10} | {11} | {12}",
                    on ? "moeda fisica, recolhe com " + go : "ouro direto", C(s.FirstSaleTime), U(1), U(5), U(10), U(15), all ? C(prod) : "nao", 100f * s.WalkNoDecision / 3600f,
                    100f * (s.WalkNoDecision - bot.PileWalk) / 3600f, 100f * bot.PileWalk / 3600f, s.Sales, s.GoldEarned, s.Pile[0] + s.Pile[1]));
            }
        }
    }
}
