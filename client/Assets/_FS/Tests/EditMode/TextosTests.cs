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
    }
}
