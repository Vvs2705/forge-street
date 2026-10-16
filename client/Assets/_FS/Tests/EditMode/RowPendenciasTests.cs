using System;
using System.Linq;
using System.Text.RegularExpressions;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// row-pendencias (BALANCE.md §39): o transporte de insumos da Rua dos Armeiros. Com 2+ insumos (lingote, placa, rebite, liga) o papel 1
    /// so pega o que cabe nas entradas e nao espera na boca de estacao parada (Sim.Transporte.cs); no Old Forge (um insumo, o lingote) tudo
    /// como antes. O Ferraz 4 leva insumos (papel 1) e um save antigo com ele no papel 0 carrega. A rebitadeira faz 4 rebites em 32 s e a 2a
    /// faz falta; o preco das linhas novas no texto e' o do jogo. NUnit 3.5 do Unity: tipos exatos nas tuplas.
    /// </summary>
    public class RowPendenciasTests
    {
        const float Dt = 1f / 30f;
        static readonly DistrictDef Row = Distritos.ArmorersRow;

        static Sim NovaRow() => new Sim(distrito: Row);
        static int U(Sim s, string id) => Upgrades.IndexOf(id, s.UpgradeDefs);
        static Item I(Sim s, string id) => (Item)Items.IndexOf(id, s.ItemDefs);
        static Station E(Sim s, string id) => s.Est(id);
        static void Compra(Sim s, params string[] ids) { foreach (string id in ids) Assert.IsTrue(s.Buy((Upgrade)U(s, id)), id); }

        /// <summary>Ate o ajudante pegar algo (ou `max` s).</summary>
        static void AtePegar(Sim s, Carrier w, float max) { for (float t = 0f; t < max && w.Count == 0; t += Dt) s.Tick(Dt, 0f, 0f); }

        /// <summary>Regra 1: toda montagem aberta com a entrada de placas cheia; o Ferraz 2 (papel 1) encostado na prensa com 4 placas prontas
        /// nao pega placa (nao cabe em lugar nenhum) e vai buscar o rebite que falta. Com 1 vaga de placa, pega 1 (nao a mao cheia). No Old
        /// Forge, com a bigorna cheia, o ajudante do lingote pega a mao cheia como sempre.</summary>
        [Test]
        public void Transporte_PapelUm_SoPegaOQueCabe_OldForgeComoAntes()
        {
            Sim s = NovaRow();
            Compra(s, "ferraz1", "ferraz2");
            Carrier f2 = s.Workers.Find(w => w.Role == 1);
            Item placa = I(s, "placa"), rebite = I(s, "rebite");
            Station elmo = E(s, "montagem_elmo"), prensa = E(s, "prensa"), reb = E(s, "rebitadeira");
            elmo.Ins[elmo.InIndex(placa)] = elmo.InCap;
            prensa.Out = 4; reb.Out = 4;
            f2.Pos = prensa.OutAt;
            AtePegar(s, f2, 10f);
            Assert.AreEqual((0, true), (f2.Held[(int)placa], f2.Held[(int)rebite] > 0), "placa sem destino fica na prensa; o rebite que falta vai");

            Sim t = NovaRow();
            Compra(t, "ferraz1", "ferraz2");
            Carrier g = t.Workers.Find(w => w.Role == 1);
            Station m = E(t, "montagem_elmo"), p = E(t, "prensa");
            m.Ins[m.InIndex(placa)] = m.InCap - 1;
            p.Out = 4;
            g.Pos = p.OutAt;
            AtePegar(t, g, 10f);
            for (float x = 0f; x < 0.5f; x += Dt) t.Tick(Dt, 0f, 0f);
            Assert.AreEqual((1, 2), (g.Held[(int)placa], g.Cap), "1 vaga de placa: pega 1, nao a mao cheia");

            var of = new Sim();
            of.Buy(Upgrade.Helper1); of.Buy(Upgrade.Helper2);
            Carrier a2 = of.Workers.Find(w => w.Role == 1);
            of.AnvilA.In = of.AnvilA.InCap;
            of.FurnaceA.Out = 4;
            a2.Pos = of.FurnaceA.OutAt;
            AtePegar(of, a2, 10f);
            for (float x = 0f; x < 0.5f; x += Dt) of.Tick(Dt, 0f, 0f);
            Assert.AreEqual(a2.Cap, a2.Held[(int)Item.Ingot], "Old Forge: bigorna cheia e o ajudante pega a mao cheia de lingote (como antes)");
        }

        /// <summary>Regra 2: o Ferraz 2 com alvo numa fonte parada (a fornalha de liga sem lingote e sem saida) troca de fonte e leva o rebite
        /// pronto. No Old Forge o ajudante com alvo na fornalha parada espera nela, como antes.</summary>
        [Test]
        public void Transporte_PapelUm_NaoEsperaNaFonteParada_OldForgeComoAntes()
        {
            Sim s = NovaRow();
            Compra(s, "ferraz1", "ferraz2", "fornalha_armeiro2", "fornalha_liga");
            Carrier f2 = s.Workers.Find(w => w.Role == 1);
            Station liga = E(s, "fornalha_liga"), reb = E(s, "rebitadeira");
            Assert.AreEqual((0, false, 0), (liga.Out, liga.Busy, liga.In));
            reb.Out = 4;
            f2.Pos = liga.OutAt; f2.Target = liga.Index; f2.State = 0;
            AtePegar(s, f2, 15f);
            Assert.AreEqual((true, reb.Index), (f2.Held[(int)I(s, "rebite")] > 0, f2.Target), "saiu da fornalha de liga parada e pegou o rebite");

            var of = new Sim();
            of.Buy(Upgrade.Helper1); of.Buy(Upgrade.Helper2); of.Buy(Upgrade.Furnace2);
            Carrier a2 = of.Workers.Find(w => w.Role == 1);
            of.FurnaceB.Out = 4;
            a2.Pos = of.FurnaceA.OutAt; a2.Target = of.FurnaceA.Index; a2.State = 0;
            for (float x = 0f; x < 3f; x += Dt) { of.Tick(Dt, 0f, 0f); of.FurnaceA.In = 0; }   // a fornalha A sem minerio: parada
            Assert.AreEqual((of.FurnaceA.Index, 0), (a2.Target, a2.Count), "Old Forge: o alvo parado segura o ajudante (como antes)");
        }

        /// <summary>O Ferraz 4 entra no papel 1 (insumos) com o Ferraz 2. Save de antes (o 4o no papel 0, com couro na mao) carrega: 4 Ferraz,
        /// os papeis de agora, o Ferraz 1 com a mao dele e o couro do 4o fica de fora (a mao que nao tem dono no papel novo); nada desconhecido.</summary>
        [Test]
        public void Ferraz4_NosInsumos_SaveAntigoComPapel0Carrega()
        {
            Sim s = NovaRow();
            Compra(s, "ferraz1", "ferraz2", "ferraz3", "ferraz4");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 1 }, s.Workers.ConvertAll(w => w.Role));
            string txt = s.Save(1000);
            string antigo = Regex.Replace(txt, "^wks=.*$", "wks=0:minerio:2;1:;2:;0:couro:2", RegexOptions.Multiline);
            Assert.AreNotEqual(txt, antigo);
            Sim l = Sim.Load(antigo, distrito: Row);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 1 }, l.Workers.ConvertAll(w => w.Role));
            Assert.AreEqual((2, 0, 0), (l.Workers[0].Held[(int)Item.Ore], l.Workers.Sum(w => w.Held[(int)I(l, "couro")]), l.UnknownIds));
            Assert.IsTrue(l.CanPick(l.Workers[3], I(l, "rebite")) && !l.CanPick(l.Workers[3], I(l, "couro")), "o 4o leva insumo, nao bruto");
        }

        /// <summary>O bot F2P chegando como o jogador: a Rebitadeira 2 (o ultimo produtivo) faz pelo menos 1/4 dos ciclos de rebite nos 30 min
        /// depois da producao completa (com os 4 s do catalogo ela fazia 4 ciclos em 150 min), e a liga chega a pelo menos 10% das pecas
        /// (na main, ~4%: o papel 1 parado com placa na mao).</summary>
        [Test]
        public void Bot_F2P_Rebitadeira2FazFalta_ELigaChega()
        {
            Imperio imp = Imperio.Chegada(Row);
            Sim s = imp.Ativo; var b = new Bot();
            for (float t = 0f; t < 150f * 60f && !s.ProductionComplete; t += Dt) { b.Step(s, Dt); imp.Fundo(Dt); }
            Assert.IsTrue(s.ProductionComplete, "producao completa em 150 min");
            Station r1 = E(s, "rebitadeira"), r2 = E(s, "rebitadeira2");
            int c1 = r1.Crafted, c2 = r2.Crafted;
            for (float t = 0f; t < 30f * 60f; t += Dt) { b.Step(s, Dt); imp.Fundo(Dt); }
            int d1 = r1.Crafted - c1, d2 = r2.Crafted - c2;
            int pecas = Enumerable.Range(0, s.ItemDefs.Length).Where(i => s.IsProduct((Item)i)).Sum(i => s.Crafted[i]);
            TestContext.WriteLine($"30 min depois da producao: rebitadeira {d1} ciclos, rebitadeira 2 {d2}; liga em {s.PecasComLiga.Sum()} de {pecas} pecas");
            Assert.GreaterOrEqual(d2 * 3, d1, "a 2a faz pelo menos 1/4 dos ciclos");
            Assert.GreaterOrEqual(s.PecasComLiga.Sum() * 10, pecas, "liga em pelo menos 10% das pecas");
        }

        /// <summary>"Nova linha: maca, paga N" mostra o preco Normal do jogo (era o do catalogo, 1/3,5 do preco desde o #66), nos 3 idiomas.</summary>
        [Test]
        public void Textos_NovaLinha_PagaOPrecoDoJogo()
        {
            Sim s = NovaRow();
            int vistos = 0;
            try
            {
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    foreach (UpgradeDef d in s.UpgradeDefs)
                    {
                        Match m = Regex.Match(Loc.T("up." + d.Id + ".desc"), @"(paga|pays) (\d+)");
                        if (!m.Success) continue;
                        Station st = s.Stations.Find(x => x.UnlockBy == (int)d.Up && x.Produces);
                        Assert.IsNotNull(st, d.Id);
                        Assert.AreEqual(s.PriceOf(st.OutItem), int.Parse(m.Groups[2].Value), $"{i}: {d.Id}");
                        vistos++;
                    }
                }
            }
            finally { Loc.Atual = Idioma.Pt; }
            Assert.AreEqual(15, vistos, "maca, lanca, manoplas, machado e peitoral nos 3 idiomas");
        }
    }
}
