using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// FTUE (Tutorial.cs): o tutorial guiado avanca pelo estado real do Sim (bot jogando, nunca por tempo), pular conclui, save novo comeca
    /// na boas-vindas, save antigo de quem ja vendeu nunca mostra, round-trip da linha tut=, cartoes "o que e' isto" um de cada vez e o veto
    /// do intersticial nos passos.
    /// </summary>
    public class TutorialTests
    {
        const float Dt = 1f / 30f;

        /// <summary>Joga o bot ate `ate` (ou o limite de s), chamando o Atualiza como a View; devolve os passos na ordem em que entraram.</summary>
        static List<PassoTutorial> Joga(Sim s, Bot bot, PassoTutorial ate, float limite = 900f)
        {
            var vistos = new List<PassoTutorial> { s.Tutorial.Passo };
            for (float t = 0f; t < limite && s.Tutorial.Passo < ate; t += Dt)
            {
                bot.Step(s, Dt);
                if (!s.Tutorial.Atualiza(s)) continue;
                PassoTutorial p = s.Tutorial.Passo;
                vistos.Add(p);
                // cada passo so entra com o anterior cumprido de verdade
                if (p > PassoTutorial.Balcao) Assert.Greater(s.Sales, 0, p + ": depois da 1a venda");
                if (p > PassoTutorial.Melhoria) Assert.Greater(s.UpgradesBought, 0, p + ": depois da 1a melhoria");
            }
            return vistos;
        }

        [Test]
        public void SaveNovo_ComecaNaBoasVindas()
        {
            var s = new Sim();
            Assert.AreEqual(PassoTutorial.BoasVindas, s.Tutorial.Passo);
            Assert.IsTrue(s.Tutorial.Ativo);
            Assert.AreEqual(0, s.Tutorial.Numero);
            StringAssert.DoesNotContain("tut=", s.Save(1), "save novo sem progresso: o texto de antes");
            Assert.AreEqual(PassoTutorial.BoasVindas, Sim.Load(s.Save(1)).Tutorial.Passo, "reabrir sem ter comecado: de novo a boas-vindas");
            Assert.AreEqual(PassoTutorial.BoasVindas, Sim.Load("v=3\ngold=0\n").Tutorial.Passo, "save sem venda nem compra: tutorial do zero");
        }

        [Test]
        public void BoasVindas_EsperaOToque_EOPassoNaoAndaPorTempo()
        {
            var s = new Sim();
            var bot = new Bot();
            for (float t = 0f; t < 60f; t += Dt) { bot.Step(s, Dt); s.Tutorial.Atualiza(s); }
            Assert.AreEqual(PassoTutorial.BoasVindas, s.Tutorial.Passo, "sem o Comecar, nada anda");
            var parado = new Sim();
            Assert.IsTrue(parado.Tutorial.Comecar());
            for (float t = 0f; t < 120f; t += Dt) { parado.Tick(Dt, 0f, 0f); parado.Tutorial.Atualiza(parado); }
            Assert.AreEqual(PassoTutorial.Andar, parado.Tutorial.Passo, "2 min parado: continua no Andar (estado, nao tempo)");
            Assert.AreEqual(1, parado.Tutorial.Numero);
            for (float t = 0f; t < 1f; t += Dt) { parado.Tick(Dt, 1f, 0f); parado.Tutorial.Atualiza(parado); }
            Assert.Greater(parado.Tutorial.Passo, PassoTutorial.Andar, "andou 1,5 m: passou do Andar");
        }

        [Test]
        public void Sequencia_AvancaComOBotJogando_AteOFim()
        {
            var s = new Sim();
            var bot = new Bot();
            Assert.IsTrue(s.Tutorial.Comecar());
            List<PassoTutorial> vistos = Joga(s, bot, PassoTutorial.Menu);
            Assert.AreEqual(PassoTutorial.Menu, s.Tutorial.Passo, "o 1o ciclo inteiro fecha jogando: " + string.Join(",", vistos));
            for (int i = 1; i < vistos.Count; i++) Assert.Greater((int)vistos[i], (int)vistos[i - 1], "nunca volta: " + string.Join(",", vistos));
            foreach (PassoTutorial p in new[] { PassoTutorial.PegarMinerio, PassoTutorial.Fornalha, PassoTutorial.PegarLingotes, PassoTutorial.Bigorna,
                PassoTutorial.PegarEspada, PassoTutorial.Balcao, PassoTutorial.Melhoria, PassoTutorial.Menu })
                CollectionAssert.Contains(vistos, p, "cada passo do ciclo aparece: " + string.Join(",", vistos));
            Assert.AreEqual(Tutorial.Total, s.Tutorial.Numero, "Passo 9 de 9");
            Assert.IsTrue(s.Tutorial.Ativo);
            Assert.IsTrue(s.Tutorial.Confirmar(), "Entendi (ou o menu aberto)");
            Assert.AreEqual(PassoTutorial.Fim, s.Tutorial.Passo);
            Assert.IsFalse(s.Tutorial.Ativo);
            Assert.IsFalse(s.Tutorial.Pulou);
            Assert.IsFalse(s.Tutorial.Confirmar(), "uma vez so");
        }

        [Test]
        public void Alvo_DaSetaPorPasso()
        {
            var s = new Sim();
            Assert.IsNull(s.Tutorial.Alvo(s, out _), "boas-vindas: sem seta");
            s.Tutorial.Comecar();
            Assert.AreEqual(Hint.GrabOre, s.Tutorial.Alvo(s, out int a));
            Assert.AreEqual((int)Item.Ore, a);
            Joga(s, new Bot(), PassoTutorial.Fornalha);
            Assert.AreEqual(Hint.OreToFurnace, s.Tutorial.Alvo(s, out _));
            Joga(s, new Bot(), PassoTutorial.Balcao);
            Assert.AreEqual(Hint.ProductToCounter, s.Tutorial.Alvo(s, out int p));
            Assert.IsTrue(s.IsProduct((Item)p), "a seta vai ao balcao do produto");
            Joga(s, new Bot(), PassoTutorial.Melhoria);
            Hint? h = s.Tutorial.Alvo(s, out int slot);
            Assert.IsTrue(h == Hint.BuyPad || h == Hint.BuyMenu, h.ToString());
            if (h == Hint.BuyPad) Assert.GreaterOrEqual(s.Pads[slot].Current(s), 0, "a placa visivel mais barata");
        }

        [Test]
        public void Pular_ConcluiNaHora_EVaiNoSave()
        {
            var s = new Sim();
            Assert.IsTrue(s.Tutorial.Pular(), "pular ja na boas-vindas");
            Assert.IsFalse(s.Tutorial.Ativo);
            Assert.IsTrue(s.Tutorial.Pulou);
            Assert.IsFalse(s.Tutorial.Pular(), "uma vez so");
            Sim b = Sim.Load(s.Save(1));
            Assert.AreEqual(PassoTutorial.Fim, b.Tutorial.Passo, "reabrir: nada de tutorial");
            Assert.IsTrue(b.Tutorial.Pulou);
            var m = new Sim();
            m.Tutorial.Comecar();
            Joga(m, new Bot(), PassoTutorial.Bigorna);
            Assert.IsTrue(m.Tutorial.Pular(), "pular no meio");
            Assert.AreEqual(PassoTutorial.Fim, m.Tutorial.Passo);
        }

        [Test]
        public void SaveAntigo_ComVendaOuCompra_NuncaMostra()
        {
            var s = new Sim();
            var bot = new Bot();
            for (float t = 0f; t < 300f; t += Dt) bot.Step(s, Dt);
            Assert.GreaterOrEqual(s.FirstSaleTime, 0f, "premissa: vendeu");
            string antigo = string.Join("\n", s.Save(1000).Split('\n').Where(l => !l.StartsWith("tut=")));
            Sim b = Sim.Load(antigo);
            Assert.IsFalse(b.Tutorial.Ativo, "save de antes do tutorial com venda: concluido");
            b.Tutorial.Atualiza(b);
            Assert.IsNull(b.Tutorial.Cartao, "nem cartao do que ja estava aberto");
            Assert.IsFalse(Sim.Load(CoreTests.SaveV03).Tutorial.Ativo, "save da v0.3");
            var c = new Sim();
            c.Gold = 1000; c.Buy(Upgrade.FurnaceSpeed1);
            string soCompra = string.Join("\n", c.Save(1000).Split('\n').Where(l => !l.StartsWith("tut=")));
            Assert.IsFalse(Sim.Load(soCompra).Tutorial.Ativo, "so uma compra, sem venda: tambem ja jogava");
        }

        [Test]
        public void RoundTrip_PassoVistosEPulou()
        {
            var s = new Sim();
            s.Tutorial.Comecar();
            Joga(s, new Bot(), PassoTutorial.PegarLingotes);
            PassoTutorial meio = s.Tutorial.Passo;
            Sim b = Sim.Load(s.Save(1000));
            Assert.AreEqual(meio, b.Tutorial.Passo, "volta no mesmo passo");
            Assert.AreEqual(s.Save(1000), b.Save(1000), "save -> load -> save igual (tut=)");
            b.Tutorial.Pular();
            b.Gold = 100000; b.Buy(Upgrade.Helper1);
            b.Tutorial.Atualiza(b);
            Assert.AreEqual(Explica.Ajudante, b.Tutorial.Cartao);
            b.Tutorial.FecharCartao();
            Sim c = Sim.Load(b.Save(2000));
            Assert.AreEqual(PassoTutorial.Fim, c.Tutorial.Passo);
            Assert.IsTrue(c.Tutorial.Pulou);
            Assert.IsTrue(c.Tutorial.Visto(Explica.Ajudante));
            Assert.AreEqual(b.Tutorial.Vistos, c.Tutorial.Vistos);
            Assert.AreEqual(PassoTutorial.BoasVindas, Sim.Load("v=3\ngold=5\ntut=lixo,x\n").Tutorial.Passo, "lixo = padrao");
            Assert.AreEqual(PassoTutorial.Fim, Sim.Load("v=3\ntut=99,0,0\n").Tutorial.Passo, "passo fora da faixa fica no Fim");
        }

        [Test]
        public void Cartoes_SoDepoisDoTutorial_UmDeCadaVez()
        {
            var s = new Sim();
            s.Gold = 1000000;
            s.Buy(Upgrade.Helper1); s.Buy(Upgrade.Tools);
            s.Tutorial.Atualiza(s);
            Assert.IsNull(s.Tutorial.Cartao, "nos passos: nenhum cartao");
            s.Tutorial.Pular();
            s.Tutorial.Atualiza(s);
            Assert.AreEqual(Explica.Ajudante, s.Tutorial.Cartao);
            s.Tutorial.Atualiza(s);
            Assert.AreEqual(Explica.Ajudante, s.Tutorial.Cartao, "nao empilha: o da tela fica ate o toque");
            s.Tutorial.FecharCartao();
            s.Tutorial.Atualiza(s);
            Assert.AreEqual(Explica.Qualidade, s.Tutorial.Cartao, "o proximo, so depois do toque");
            s.Tutorial.FecharCartao();
            s.Tutorial.Atualiza(s);
            Assert.AreNotEqual(Explica.Ajudante, s.Tutorial.Cartao, "visto nao volta");
            Assert.AreNotEqual(Explica.Qualidade, s.Tutorial.Cartao, "visto nao volta");
            var d = new Sim();
            d.Tutorial.Desligar();
            d.Gold = 1000000; d.Buy(Upgrade.Helper1);
            d.Tutorial.Atualiza(d);
            Assert.IsNull(d.Tutorial.Cartao, "desligado (fotos): nenhum cartao");
        }

        /// <summary>Os textos dos passos e cartoes existem nos 3 idiomas e cabem: a dica na pilula de 1 linha (Textos.DicaMax), o corpo do
        /// passo em 2 linhas do painel (64), o cartao em ~4 linhas (120) e "Passo 9 de 9" na linha de cima.</summary>
        [Test]
        public void Textos_TresIdiomas_CabemNoCelular()
        {
            foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
            {
                for (PassoTutorial p = PassoTutorial.Andar; p < PassoTutorial.Fim; p++)
                {
                    string dica = Loc.Em(i, "tut.dica." + Tutorial.IdDe(p)), corpo = Loc.Em(i, "tut.corpo." + Tutorial.IdDe(p));
                    StringAssert.DoesNotStartWith("[", dica, $"{i} {p}: dica");
                    StringAssert.DoesNotStartWith("[", corpo, $"{i} {p}: corpo");
                    Assert.LessOrEqual(dica.Length, Textos.DicaMax, $"{i}: \"{dica}\"");
                    Assert.LessOrEqual(corpo.Length, 64, $"{i}: \"{corpo}\"");
                }
                foreach (Explica e in System.Enum.GetValues(typeof(Explica)))
                {
                    string t = Loc.Em(i, "tut.cartao.titulo." + Tutorial.IdDe(e)), c = Loc.Em(i, "tut.cartao.corpo." + Tutorial.IdDe(e));
                    StringAssert.DoesNotStartWith("[", t, $"{i} {e}");
                    StringAssert.DoesNotStartWith("[", c, $"{i} {e}");
                    Assert.LessOrEqual(t.Length, 22, $"{i}: \"{t}\"");
                    Assert.LessOrEqual(c.Length, 120, $"{i}: \"{c}\"");
                }
                Assert.LessOrEqual(string.Format(Loc.Em(i, "tut.numero"), 9, 9).Length, 14, i.ToString());
                foreach (string k in new[] { "tut.pular", "tut.ok" }) Assert.LessOrEqual(Loc.Em(i, k).Length, 9, $"{i}: {k}");
                Assert.LessOrEqual(Loc.Em(i, "tut.boas.corpo").Length, 130, i.ToString());
            }
        }

        [Test]
        public void Intersticial_VetadoNosPassos()
        {
            var s = new Sim(new Tuning { Interstitial = true });
            s.Buy(Upgrade.Tools);
            bool tr = Estados.De(true, true, false, false, false, true) == Estado.Transition;
            Assert.AreEqual("tutorial", s.Anuncios.VetoIntersticial(tr, false, 1e6f), "passos guiados: nenhum anuncio");
            s.Tutorial.Pular();
            Assert.IsNull(s.Anuncios.VetoIntersticial(tr, false, 1e6f), "concluido: valem so os outros vetos");
        }
    }
}
