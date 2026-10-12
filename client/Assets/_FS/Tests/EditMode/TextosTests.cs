using System.Collections.Generic;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>v0.6d (revisao de UX): toda dica da HUD cabe numa linha da pilula (Textos.DicaMax) com qualquer upgrade, item e bau.</summary>
    public class TextosTests
    {
        [Test]
        public void Dicas_CabemNumaLinha()
        {
            var s = new Sim();
            var all = new List<string>();
            for (int u = 0; u < Upgrades.Count; u++) { all.Add(Textos.Placa(u)); all.Add(Textos.Menu(u)); }
            for (int i = 0; i < Balance.ItemName.Length; i++)
                foreach (Hint h in new[] { Hint.ProductToCounter, Hint.PickProducts, Hint.ClientWaiting }) all.Add(Textos.Dica(h, i, s));
            for (int c = 0; c < s.Chests.Count; c++) all.Add(Textos.Dica(Hint.OpenChest, c, s));
            foreach (Hint h in new[] { Hint.GrabOre, Hint.OreToFurnace, Hint.PickIngots, Hint.IngotToCrafter }) all.Add(Textos.Dica(h, 0, s));
            foreach (string t in all) Assert.LessOrEqual(t.Length, Textos.DicaMax, t);
            Assert.AreEqual("Pise na placa da Bigorna 2", Textos.Placa((int)Upgrade.Anvil2));
            Assert.AreEqual("Compre o Fole em Melhorias", Textos.Menu((int)Upgrade.FurnaceSpeed1));
        }

        /// <summary>A-UI-18: todo cartao do menu tem beneficio em % que cabe em 2 linhas e o antes \u2192 depois em 1; numeros do Balance.</summary>
        [Test]
        public void Cartao_BeneficioEmPorcento()
        {
            Assert.AreEqual("Fornalha 38% mais r\u00E1pida", Textos.Beneficio((int)Upgrade.FurnaceSpeed1));   // 4 \u2192 2,5 s
            Assert.AreEqual("Fornalha 36% mais r\u00E1pida", Textos.Beneficio((int)Upgrade.FurnaceSpeed2));
            Assert.AreEqual("Bancadas 40% mais r\u00E1pidas", Textos.Beneficio((int)Upgrade.HammerSpeed));
            Assert.AreEqual("Carga +100%", Textos.Beneficio((int)Upgrade.PlayerCapacity));
            Assert.AreEqual("Velocidade +42%", Textos.Beneficio((int)Upgrade.HelperSpeed));   // 2,4 \u2192 3,4 m/s
            Assert.AreEqual("Clientes +43%", Textos.Beneficio((int)Upgrade.CounterCapacity));
            Assert.AreEqual("Vagas +25%", Textos.Beneficio((int)Upgrade.Counter5));
            Assert.AreEqual("4 \u2192 2,5 s", Textos.AntesDepois((int)Upgrade.FurnaceSpeed1));
            Assert.AreEqual("carga 2 \u2192 4", Textos.AntesDepois((int)Upgrade.HelperSpeed));
            Assert.AreEqual("7 \u2192 8 vagas", Textos.AntesDepois((int)Upgrade.Counter8));
            for (int u = 0; u < Upgrades.Count; u++)
            {
                if (!Upgrades.All[u].InMenu) continue;
                string b = Textos.Beneficio(u);
                StringAssert.Contains("%", b, Upgrades.All[u].Name);
                Assert.LessOrEqual(b.Length, 2 * 14, b);   // 2 linhas de ~14 caracteres a 26 px no cartao de 270 px
                Assert.LessOrEqual(Textos.AntesDepois(u).Length, 16, Upgrades.All[u].Name);
            }
            Assert.AreEqual("Faltam 120", Textos.Falta(120));
            Assert.AreEqual("Falta 1", Textos.Falta(1));
        }

        /// <summary>A-UI-18: estados do cartao com o Sim real; o enum e' a ordem da bandeja (VALIDACAO_V05 #3: travado nao vem antes do compravel).</summary>
        [Test]
        public void Cartao_EstadosEOrdem()
        {
            var s = new Sim();
            s.Gold = Upgrades.Cost(Upgrade.CounterCapacity);
            Assert.AreEqual(Cartao.Disponivel, Textos.Estado(s, (int)Upgrade.CounterCapacity));
            Assert.AreEqual(Cartao.Disponivel, Textos.Estado(s, (int)Upgrade.FurnaceSpeed1), "mais barato que a Vitrine");
            Assert.AreEqual(Cartao.SemOuro, Textos.Estado(s, (int)Upgrade.HammerSpeed));
            Assert.AreEqual(Cartao.Travado, Textos.Estado(s, (int)Upgrade.Counter8), "precisa do Balc\u00E3o 7");
            Assert.AreEqual(Cartao.Travado, Textos.Estado(s, (int)Upgrade.FurnaceSpeed2), "precisa do Fole, mesmo com ouro");
            Assert.IsTrue(s.TryBuyMenu(Upgrade.FurnaceSpeed1));
            Assert.AreEqual(Cartao.Comprado, Textos.Estado(s, (int)Upgrade.FurnaceSpeed1));
            Assert.AreEqual(Cartao.Disponivel, Textos.Estado(s, (int)Upgrade.FurnaceSpeed2), "destravou com o Fole");
            s.Gold = Upgrades.Cost(Upgrade.FurnaceSpeed2) - 1;
            Assert.AreEqual(Cartao.SemOuro, Textos.Estado(s, (int)Upgrade.FurnaceSpeed2));
            Assert.Less((int)Cartao.Disponivel, (int)Cartao.SemOuro);
            Assert.Less((int)Cartao.SemOuro, (int)Cartao.Travado);
        }

        static string O(long v) => Textos.Ouro(v).Replace('\u00A0', ' ');   // a HUD usa espaco que nao quebra linha

        [Test]
        public void Ouro_CabeNaPilula_EscalaPtBrTruncada()
        {
            Assert.AreEqual("0", O(0));
            Assert.AreEqual("999999", O(999999));
            Assert.AreEqual("1 mi", O(1000000));
            Assert.AreEqual("1,15 mi", O(1150000));   // sem float: nao vira 1,14
            Assert.AreEqual("1,05 mi", O(1059999));   // trunca, nunca arredonda para cima
            Assert.AreEqual("12,3 mi", O(12399999));
            Assert.AreEqual("999 mi", O(999999999));
            Assert.AreEqual("5 bi", O(5000000000));
            Assert.AreEqual("9,22 qui", O(long.MaxValue));
            for (long v = 1; v > 0 && v < long.MaxValue / 7; v *= 7) Assert.LessOrEqual(O(v).Length, 8, v.ToString());
        }
    }
}
