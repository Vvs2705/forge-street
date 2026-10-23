using System;
using System.Collections.Generic;
using System.Globalization;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>G-UA-02: o -preset expande para flags (a linha de comando vence), o -buyids id@t nao faz compra impossivel e cada preset
    /// montado no Core (como o Game.DevArgs) mostra o que promete. Quebrou depois de mexer no Balance? Recalibre o preset em Captura.cs.</summary>
    public class CapturaTests
    {
        const float Step = 1f / 30f;   // = Game.MaxStep

        [Test]
        public void Preset_ExpandeELinhaDeComandoVence()
        {
            string[] cmd = { "-preset", "vip", "-recordsec", "3", "-record", "C:/q/vip" };
            Assert.AreEqual("3", Captura.Arg(cmd, "-recordsec"));            // a linha de comando vence o preset (30)
            Assert.AreEqual("200", Captura.Arg(cmd, "-warmup"));             // do preset
            Assert.AreEqual("", Captura.Arg(cmd, "-vipnow"));                // flag sem valor
            Assert.AreEqual("C:/q/vip", Captura.Arg(cmd, "-record"));
            Assert.IsNull(Captura.Arg(cmd, "-gold"));
            Assert.IsNull(Captura.Arg(new[] { "-preset", "nao_existe" }, "-warmup"));
            Assert.IsNull(Captura.Arg(new[] { "-bot" }, "-warmup"));
            Assert.AreEqual("", Captura.Arg(new[] { "-bot", "-cam", "7" }, "-bot"));   // o seguinte comeca com '-'
            Assert.IsNull(Captura.Flags("nao_existe"));
        }

        [Test]
        public void Tabela_NomesUnicosEDuracao()
        {
            var nomes = new HashSet<string>();
            foreach (var p in Captura.Presets)
            {
                Assert.IsTrue(nomes.Add(p.Nome), p.Nome);
                string[] a = p.Flags.Split(' ');
                Assert.IsNull(Captura.Valor(a, "-record"), p.Nome + ": a pasta vem da linha de comando");
                Assert.Greater(F(Captura.Valor(a, "-recordsec")), 0f, p.Nome);
            }
            Assert.AreEqual(13, nomes.Count);   // os 9 do G-UA-02 que nao dependem de Wave + os 4 da sequencia B (G-UA-03)
        }

        [Test]
        public void BuyIds_CompraImpossivelEErro()
        {
            var s = new Sim();
            List<(int u, float t)> at = Captura.Compras(s, "2,x,99,12@1,5@2,0@3");
            Assert.IsTrue(s.Bought[(int)Upgrade.Helper1]);                   // sem "@": estado montado, de graca
            Assert.AreEqual(3, at.Count);
            s.Time = 1.5f;
            StringAssert.Contains("sem Ajudante 2", Captura.Vence(s, at));   // Ajudante 3 antes do 2: pre-requisito (antes comprava)
            Assert.IsFalse(s.Bought[(int)Upgrade.Helper3]);
            s.Time = 2.5f;
            StringAssert.Contains("ouro 0", Captura.Vence(s, at));           // sem ouro (antes o ouro ia a 0 e comprava)
            Assert.IsFalse(s.Bought[(int)Upgrade.Helper2]);
            s.Gold = s.T.Cost(Upgrade.FurnaceSpeed1) + 7;
            Assert.IsNull(Captura.Vence(s, at));                              // 0@3 ainda nao venceu
            s.Time = 3.5f;
            Assert.IsNull(Captura.Vence(s, at));
            Assert.IsTrue(s.Bought[(int)Upgrade.FurnaceSpeed1]);
            Assert.AreEqual(7L, s.Gold);                                      // cobra o preco
            Assert.AreEqual(0, at.Count);
            StringAssert.Contains("ja comprado", Captura.PodeComprar(s, (int)Upgrade.Helper1));
            s.Bought[(int)Upgrade.JewelVitrine] = true;                       // pre-requisito do luxo; a producao ainda nao esta completa
            StringAssert.Contains("luxo", Captura.PodeComprar(s, (int)Upgrade.WorkshopFacade));
        }

        [Test]
        public void Offline_SoDe60sAoTeto()
        {
            Assert.AreEqual(7200, Captura.OfflineSegundos("7200"));
            Assert.AreEqual(60, Captura.OfflineSegundos("60"));
            foreach (string v in new[] { "59", "7201", "", null, "2h", "-5" }) Assert.AreEqual(-1, Captura.OfflineSegundos(v), v);
        }

        /// <summary>Cada preset montado como no jogo: as compras id@t pagam no instante, e o que o -recordfull, -vipnow, -offline,
        /// -recordorder e -recordhire conferem no jogo vale aqui.</summary>
        [Test]
        public void Presets_MostramOQuePrometem()
        {
            foreach (var p in Captura.Presets)
            {
                string[] a = p.Flags.Split(' ');
                Sim s = Monta(a, out List<(int u, float t)> at, out string erro);
                Assert.IsNull(erro, p.Nome);
                if (Captura.Valor(a, "-recordfull") != null)
                {
                    s.Tick(Step, 0f, 0f);   // o 1o quadro
                    Assert.AreEqual(s.QueueCap, s.Queue.Count, p.Nome + ": fila cheia no 1o quadro");
                }
                if (Captura.Valor(a, "-vipnow") != null) Assert.IsTrue(s.FirstSaleTime >= 0f && s.CanSummonVip, p.Nome + ": venda real no warmup");
                if (Captura.Valor(a, "-offline") is string off)
                    Assert.Greater(s.ApplyOffline(Captura.OfflineSegundos(off), s.LastClaim + 1), 0L, p.Nome + ": cofre com ouro");
                if (Captura.Valor(a, "-recordorder") != null)
                {
                    Assert.IsNotNull(s.Encomendas.Principal, p.Nome + ": cartao no 1o quadro");
                    float fim = s.Time + F(Captura.Valor(a, "-recordsec")) - 2f;   // entregue com 2 s de folga para o aviso
                    bool entregue = false;
                    while (s.Time < fim && !entregue) { s.Tick(Step, 0f, 0f); entregue = s.Events.Exists(e => e.Kind == Ev.OrderDone); }
                    Assert.IsTrue(entregue, p.Nome + ": encomenda entregue na gravacao");
                }
                if (Captura.Valor(a, "-recordhire") != null)
                {
                    var bot = new Bot();
                    bool hired = false;
                    while (s.Time < 600f && !hired) { bot.Step(s, Step); hired = s.Events.Exists(e => e.Kind == Ev.Hired); }
                    Assert.IsTrue(hired, p.Nome + ": o bot contrata antes de t = 600 s");
                }
                // compras id@t depois do warmup (district_unlock) vencem dentro da gravacao, pagas
                float ate = s.Time + F(Captura.Valor(a, "-recordsec"));
                while (at.Count > 0 && erro == null && s.Time < ate) { s.Tick(Step, 0f, 0f); erro = Captura.Vence(s, at); }
                Assert.IsNull(erro, p.Nome);
                Assert.AreEqual(0, at.Count, p.Nome + ": compra id@t fora da gravacao");
            }
        }

        /// <summary>O estado do Game.DevArgs: -buy N, -buyids (Captura.Compras), -px, warmup com o jogador parado e as compras id@t, -hold.</summary>
        static Sim Monta(string[] a, out List<(int u, float t)> at, out string erro)
        {
            var s = new Sim();
            if (int.TryParse(Captura.Valor(a, "-buy"), out int n))
                for (int pass = 0; pass < 2; pass++)
                    for (int i = 0; i < Math.Min(n, Upgrades.Count); i++)
                        if (Upgrades.IsLuxury(i) == (pass == 1)) s.Buy((Upgrade)i);
            at = Captura.Compras(s, Captura.Valor(a, "-buyids"));
            string[] px = (Captura.Valor(a, "-px") ?? "").Split(',');
            if (px.Length == 2) s.Player.Pos = new V2(F(px[0]), F(px[1]));
            erro = null;
            float warm = F(Captura.Valor(a, "-warmup"));
            for (float t = 0f; t < warm && erro == null; t += Step) { s.Tick(Step, 0f, 0f); erro = Captura.Vence(s, at); }
            string[] hold = (Captura.Valor(a, "-hold") ?? "").Split(',');
            for (int i = 0; i < Math.Min(hold.Length, 6); i++)
                if (int.TryParse(hold[i], out int h)) s.Player.Held[i] = Math.Max(0, Math.Min(h, s.Player.Cap));
            return s;
        }

        static float F(string v) => string.IsNullOrEmpty(v) ? 0f : float.Parse(v, CultureInfo.InvariantCulture);

        /// <summary>B-CORE-03 -qualidade e -golpe (fotos) montam o que prometem: qualidade revelada, Fina e Superior nas pilhas e no balcao, o
        /// ferreiro na Bigorna; com -golpe o anel aparece (Sim.StrikeStation).</summary>
        [Test]
        public void Qualidade_EGolpe_MontamAFoto()
        {
            var s = new Sim();
            Captura.Qualidade(s, false);
            s.Tick(Step, 0f, 0f);
            Assert.IsTrue(s.QualityRevealed);
            Assert.AreEqual(3, s.AnvilA.Level);
            Assert.AreEqual(1, s.AnvilA.OutQ[(int)Quality.Superior]);
            Assert.AreEqual(1, s.StockQ[(int)Item.Shield][(int)Quality.Fine]);
            Assert.IsNull(s.StrikeStation, "-qualidade: sem anel (a Bigorna nao tem peca)");
            var g = new Sim();
            Captura.Qualidade(g, true);
            g.Tick(Step, 0f, 0f);
            Assert.AreSame(g.AnvilA, g.StrikeStation, "-golpe: o anel aparece na Bigorna");
        }
    }
}
