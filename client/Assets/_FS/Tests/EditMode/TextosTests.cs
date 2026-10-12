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
