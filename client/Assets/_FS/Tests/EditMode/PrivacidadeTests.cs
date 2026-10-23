using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>E-PLAT-01: faixa x aceite -> flags do LevelPlay (tabela de docs/LEVELPLAY.md s6), a resposta gravada e a versao do texto.
    /// Mudou o texto da tela? Suba Privacidade.Versao e o par (versao, hash) do Versao_TravaOTexto.</summary>
    public class PrivacidadeTests
    {
        static readonly Faixa[] Todas = { Faixa.Menor13, Faixa.De13a17, Faixa.Adulto };

        /// <summary>GDD "Conflitos" 1: a tela de idade abre so quando o 1o botao de anuncio aparece (sem resposta, fora de foto/smoke), ou com
        /// a flag; e no jogo de verdade a 1a venda sai antes disso (a tela nunca fica antes do 1o toque) e o SDK so inicia depois da resposta.</summary>
        [Test]
        public void TelaDeIdade_NoPrimeiroBotaoDeAnuncio_DepoisDaPrimeiraVenda()
        {
            Assert.IsFalse(Privacidade.AbreTela(false, false, false, false), "antes do 1o anuncio: nao abre");
            Assert.IsTrue(Privacidade.AbreTela(false, false, true, false), "1o anuncio, sem resposta: abre");
            Assert.IsFalse(Privacidade.AbreTela(true, false, true, false), "ja respondida (versao atual): nao abre");
            Assert.IsFalse(Privacidade.AbreTela(false, true, true, false), "foto e smoke: nao abre");
            Assert.IsTrue(Privacidade.AbreTela(true, true, false, true), "-privacidade: sempre");
            var s = new Sim(); var bot = new Bot();
            float venda = -1f, anuncio = -1f;
            for (float t = 0f; t < 300f && anuncio < 0f; t += BalanceTests.Dt)
            {
                bot.Step(s, BalanceTests.Dt);
                if (venda < 0f && s.FirstSaleTime >= 0f) venda = s.Time;
                if (anuncio < 0f && s.AdsUnlocked) anuncio = s.Time;
            }
            TestContext.WriteLine($"1a venda {venda:0.0} s; 1o botao de anuncio (a tela) {anuncio:0.0} s");
            Assert.That(venda, Is.InRange(0f, 90f), "1a venda < 90 s sem a tela no caminho");
            Assert.Greater(anuncio, venda, "a tela vem depois da 1a venda");
        }

        [Test]
        public void Flags_FaixaXAceite()
        {
            // (Coppa, Consentimento GDPR, NaoVender CCPA, SemId is_deviceid_optout, Familias Google_Family_Self_Certified_SDKS)
            var casos = new (Faixa faixa, bool aceitou, (bool, bool, bool, bool, bool) flags)[]
            {
                (Faixa.Menor13, false, (true, false, true, true, true)),
                (Faixa.Menor13, true, (true, false, true, true, true)),     // aceite de crianca nao conta
                (Faixa.De13a17, false, (false, false, true, true, false)),
                (Faixa.De13a17, true, (false, false, true, true, false)),   // adolescente: nada personalizado (ECA Digital)
                (Faixa.Adulto, false, (false, false, true, false, false)),
                (Faixa.Adulto, true, (false, true, false, false, false)),   // o unico personalizado
            };
            foreach (var c in casos)
                Assert.AreEqual(c.flags, Privacidade.Flags(c.faixa, c.aceitou), $"{c.faixa} aceitou={c.aceitou}");
        }

        [Test]
        public void Flags_Invariantes()
        {
            foreach (Faixa f in Todas)
                foreach (bool a in new[] { false, true })
                {
                    var x = Privacidade.Flags(f, a);
                    Assert.AreEqual(f == Faixa.Adulto && a, x.Consentimento, $"{f} {a}: personalizado so 18+ com aceite");
                    Assert.AreEqual(!x.Consentimento, x.NaoVender, $"{f} {a}: do_not_sell sempre que nao personalizado");
                    Assert.AreEqual(f == Faixa.Menor13, x.Coppa, $"{f} {a}: COPPA so menor de 13");
                    Assert.AreEqual(f != Faixa.Adulto, x.SemId, $"{f} {a}: menor de 18 sem ID de publicidade");
                    Assert.AreEqual(x.Coppa, x.Familias, $"{f} {a}: Familias junto com COPPA");
                }
        }

        [Test]
        public void Gravar_LerIdaEVolta()
        {
            foreach (Faixa f in Todas)
                foreach (bool a in new[] { false, true })
                {
                    string s = Privacidade.Gravar(f, a);
                    Assert.IsTrue(Privacidade.Ler(s, out Faixa lida, out bool aceitou), s);
                    Assert.AreEqual((f, f == Faixa.Adulto && a), (lida, aceitou), s);
                }
            Assert.AreEqual(Privacidade.Versao + "|2|0", Privacidade.Gravar(Faixa.De13a17, true), "aceite de menor grava 0");
        }

        [Test]
        public void Ler_SemRespostaVersaoOutraOuLixo_PerguntaDeNovo()
        {
            int v = Privacidade.Versao;
            string[] ruins =
            {
                null, "", "lixo", $"{v - 1}|3|1", $"{v + 1}|3|1", $"{v}|0|0", $"{v}|4|0", $"{v}|3|2", $"{v}|3", $"{v}|3|1|0",
                $"{v}|+3|1", $"{v}| 3|1", $" {v}|3|1", $"{v}|3|sim",
            };
            foreach (string s in ruins)
            {
                Assert.IsFalse(Privacidade.Ler(s, out Faixa f, out bool a), s ?? "null");
                Assert.AreEqual((Faixa.Menor13, false), (f, a), (s ?? "null") + ": sem resposta = o mais restrito");
            }
        }

        [Test]
        public void Versao_TravaOTexto()
        {
            // FNV-1a 32 bits do texto que o jogador le. Falhou? O texto mudou: suba Privacidade.Versao (todo mundo responde de novo) e
            // copie o hash novo da mensagem para ca
            uint h = 2166136261;
            foreach (char ch in Privacidade.Texto) { h ^= ch; h *= 16777619; }
            Assert.AreEqual((1, 0x0F40FC08u), (Privacidade.Versao, h), $"texto mudou: hash 0x{h:X8}");
        }

        [Test]
        public void Diario_ERodapeDaRevisao()
        {
            Assert.AreEqual(new[] { "<13", "13-17", "18+" }, System.Array.ConvertAll(Todas, Privacidade.Diario));
            StringAssert.Contains("18 ou mais, anúncios personalizados", Privacidade.Atual(Faixa.Adulto, true));
            StringAssert.Contains("13 a 17, anúncios não personalizados", Privacidade.Atual(Faixa.De13a17, true));
        }
    }
}
