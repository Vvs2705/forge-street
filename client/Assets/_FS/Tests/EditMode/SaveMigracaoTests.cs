using System;
using System.Linq;
using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>
    /// Saves reais das 3 versoes publicadas (Releases v0.4.0, v0.4.1 e v0.5.1 no GitHub): o nucleo de cada tag (git archive) jogado pelo bot
    /// dele por 25 e 60 min e salvo com Save(1000). O build de hoje abre cada um sem perder progresso (tempo, ouro, compras, vendas por
    /// produto), o save novo fica estavel e o jogo continua. ponytail: so as versoes que sairam; os schemas intermediarios da main ja tem
    /// teste proprio (CoreTests, EncomendasTests, SaveTests).
    /// </summary>
    public class SaveMigracaoTests
    {
        static readonly TestCaseData[] Publicadas =
        {
            new TestCaseData("v0.4.0", "v=1\nt=1500.022\ngold=1631\nup=1111111111111101100000000\nut=67.03,137.1,179.91,240.26,327.64,412.75,440.85,498,578.48,655.03,760.1,829.09,1001.54,1220.22,-1,806.32,1381.05,-1,-1,-1,-1,-1,-1,-1,-1\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,\nst1=2,2,1,0.104,634\nst2=4,4,0,0,115\nst3=4,2,1,0.4,228\nst4=0,1,1,0.387,127\nst5=4,1,1,0.44,119\nst6=0,0,1,0.993,111\nst8=2,2,1,0.467,10\nstock=0,0,0,0\nsold=352,118,111,8\nms=2,2,0,2\nema=13.75645\nsaved=1000\nclaim=0\nm=22.7,589,8726,0,1,801,6,453.58,16\npx=3.95,11.30\n").SetName("Migra_v0_4_0_25min"),
            new TestCaseData("v0.4.0", "v=1\nt=3600.008\ngold=12523\nup=1111111111111111111110011\nut=67.03,137.1,179.91,240.26,327.64,412.75,440.85,498,578.48,655.03,760.1,829.09,1001.54,1220.22,1521.18,806.32,1381.05,1630.76,2210.88,2617.7,3140.31,-1,-1,1804.85,1936.92\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,\nst1=5,3,1,0,1864\nst2=6,1,1,0.625,1263\nst3=4,1,1,0.789,873\nst4=0,0,0,0,187\nst5=0,0,0,0,368\nst6=4,0,1,0.722,464\nst8=2,0,1,0.861,424\nstock=0,0,0,0\nsold=1058,368,463,424\nms=2,2,2,2\nema=28.62551\nsaved=1000\nclaim=0\nm=22.7,2313,57088,0,1,2731,6,1064.03,23\npx=2.55,9.68\n").SetName("Migra_v0_4_0_60min"),
            new TestCaseData("v0.4.1", "v=1\nt=1500.022\ngold=1631\nup=1111111111111101100000000\nut=67.03,137.1,179.91,240.26,327.64,412.75,440.85,498,578.48,655.03,760.1,829.09,1001.54,1220.22,-1,806.32,1381.05,-1,-1,-1,-1,-1,-1,-1,-1\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,\nst1=2,2,1,0.104,634\nst2=4,4,0,0,115\nst3=4,2,1,0.4,228\nst4=0,1,1,0.387,127\nst5=4,1,1,0.44,119\nst6=0,0,1,0.993,111\nst8=2,2,1,0.467,10\nstock=0,0,0,0\nsold=352,118,111,8\nms=2,2,0,2\nema=13.75645\nsaved=1000\nclaim=0\nm=22.7,589,8726,0,1,801,6,453.58,16\npx=3.95,11.30\n").SetName("Migra_v0_4_1_25min"),
            new TestCaseData("v0.4.1", "v=1\nt=3600.008\ngold=12546\nup=1111111111111111111110011\nut=67.03,137.1,179.91,240.26,327.64,412.75,440.85,498,578.48,655.03,760.1,829.09,1001.54,1220.22,1521.18,806.32,1381.05,1630.76,2209.24,2617.7,3145.53,-1,-1,1805.21,1940.55\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,\nst1=5,0,1,0.563,1856\nst2=6,4,0,0,1248\nst3=4,1,1,0.544,875\nst4=0,0,0,0,176\nst5=0,0,0,0,363\nst6=0,1,0,0,444\nst8=3,0,1,0.722,433\nstock=0,0,0,0\nsold=1050,363,441,432\nms=2,2,2,2\nema=27.3554\nsaved=1000\nclaim=0\nm=22.7,2286,57111,0,1,2758,6,1054.73,23\npx=4.20,11.91\n").SetName("Migra_v0_4_1_60min"),
            new TestCaseData("v0.5.1", "v=1\nt=1500.022\ngold=1017\nup=11111111111111010000000001111\nut=67.03,137.1,179.98,240.36,327.74,408.89,437.55,500.13,600.61,700.15,813.62,960.22,1141.01,1350.03,-1,952.82,-1,-1,-1,-1,-1,-1,-1,-1,-1,843.72,864.08,888.27,914.53\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,\nst1=6,4,0,0,534\nst2=6,4,0,0,91\nst3=4,3,0,0,120\nst4=4,3,0,0,81\nst5=4,0,1,0.818,147\nst6=4,3,0,0,103\nst8=0,0,0,0,0\nstock=10,0,9,0\nsold=181,147,91,0\nms=2,2,0,2\nema=6.8262\nsaved=1000\nclaim=0\nm=22.7,419,7347,0,2,882,8,315.04,19\npx=4.94,11.50\nhold=0,0,3,0,0,0\nwk=0:1,0,0,0,0,0;1:0,1,0,0,0,0;2:0,0,1,0,0,0\nvip=319.852,5,3,3,1,0\n").SetName("Migra_v0_5_1_25min"),
            new TestCaseData("v0.5.1", "v=1\nt=3600.008\ngold=9586\nup=11111111111111111111100111111\nut=67.03,137.1,179.98,240.36,327.74,408.89,437.55,500.13,600.61,700.15,813.62,960.22,1141.01,1350.03,1702.14,952.82,1578.1,1819.84,2375.57,2772.36,3266.94,-1,-1,1977.61,2104.31,843.72,864.08,888.27,914.53\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,\nst1=6,2,1,0.563,1611\nst2=6,4,0,0,1122\nst3=4,3,0,0,431\nst4=0,0,0,0,102\nst5=1,1,1,0.193,460\nst6=0,0,0,0,426\nst8=1,1,0,0,420\nstock=9,0,0,0\nsold=521,459,426,419\nms=2,2,2,2\nema=28.73473\nsaved=1000\nclaim=0\nm=22.7,1825,54901,0,7,3134,8,513.46,27\npx=7.55,10.15\nhold=0,0,0,0,0,0\nwk=0:4,0,0,0,0,0;1:0,0,0,0,0,0;2:0,0,0,0,0,0;3:0,0,0,0,0,0;0:0,0,0,0,0,0;3:0,0,0,0,0,0\nvip=52.351,11,-1,0,0,0\n").SetName("Migra_v0_5_1_60min"),
        };

        static string Campo(string save, string k) => save.Split('\n').First(l => l.StartsWith(k + "=", StringComparison.Ordinal)).Substring(k.Length + 1);

        [TestCaseSource(nameof(Publicadas))]
        public void SavePublicado_AbreSemPerderProgresso_EContinua(string versao, string velho)
        {
            Sim s = Sim.Load(velho);
            Assert.AreEqual(0, s.UnknownIds, versao + ": nada do save ficou sem dono");
            Assert.AreEqual(float.Parse(Campo(velho, "t"), System.Globalization.CultureInfo.InvariantCulture), s.Time, 1e-3f, "tempo de jogo");
            Assert.AreEqual(long.Parse(Campo(velho, "gold")), s.Gold, "ouro");
            Assert.AreEqual(Campo(velho, "up").Count(c => c == '1'), s.UpgradesBought, "as compras (o indice antigo vira ID no schema 3)");
            long[] sold = Campo(velho, "sold").Split(',').Select(long.Parse).ToArray();
            Item[] ordem = { Item.Sword, Item.Shield, Item.Tool, Item.Jewel };   // sold= do v=1: espada, escudo, ferramenta, joia
            for (int i = 0; i < sold.Length; i++) Assert.AreEqual(sold[i], (long)s.SoldItems[(int)ordem[i]], $"vendas de {ordem[i]}");
            Assert.Greater(s.Renome.Total, 0L, "o Renome chega pelo retroativo (encomendas e baus do save antigo)");

            string novo = s.Save(2000);
            Assert.AreEqual(novo, Sim.Load(novo).Save(2000), "o save migrado e' estavel");

            long ganho = s.GoldEarned; var bot = new Bot();
            for (float t = 0f; t < 120f; t += BalanceTests.Dt) bot.Step(s, BalanceTests.Dt);
            Assert.Greater(s.GoldEarned, ganho, "o jogo continua vendendo depois da migracao");
        }
    }
}
