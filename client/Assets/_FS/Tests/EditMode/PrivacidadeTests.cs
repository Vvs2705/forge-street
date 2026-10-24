using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>E-PLAT-01: faixa x aceite -> flags do LevelPlay (tabela de docs/LEVELPLAY.md s6), a resposta gravada e a versao do texto.
    /// Mudou o texto da tela? Suba Privacidade.Versao e o par (versao, hash) do Versao_TravaOTexto.</summary>
    public class PrivacidadeTests
    {
        static readonly Faixa[] Todas = { Faixa.Menor13, Faixa.De13a17, Faixa.Adulto };

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
        // ---- quando pedir a idade (GDD "Conflitos" item 1, decidido: movida para o 1o botao de anuncio) ----

        static readonly Estado[] Estados_ = (Estado[])System.Enum.GetValues(typeof(Estado));

        [Test]
        public void Pedido_1aAbertura_NadaAntesDoBotao_NemBotoesNemSdk()
        {
            var p = new PedidoIdade(false, false);
            Assert.IsFalse(p.Botoes, "sem resposta: nenhum botao de anuncio");
            foreach (Estado e in Estados_) Assert.IsFalse(p.Abrir(false, false, e), $"{e}: sem botao de anuncio, nenhuma tela");
            Assert.AreEqual(PassoSdk.Espera, Privacidade.Sdk(null, null), "sem resposta o SDK espera");
            Assert.IsFalse(p.Botoes);
        }

        [Test]
        public void Pedido_AbreNo1oBotao_Tocavel_1xPorSessao()
        {
            foreach (Estado e in Estados_)
            {
                Assert.AreEqual(Estados.Anuncia(e), new PedidoIdade(false, false).Abrir(true, false, e), $"HUD em {e}: so onde o botao e' tocavel");
                Assert.AreEqual(e == Estado.Modal, new PedidoIdade(false, false).Abrir(false, true, e), $"Cofre 2x em {e}: so com o painel aberto");
            }
            var p = new PedidoIdade(false, false);
            Assert.IsTrue(p.Abrir(true, false, Estado.Playing));
            Assert.IsFalse(p.Abrir(true, false, Estado.Playing), "abre 1x (a tela da 1a vez nao fecha sem resposta)");
            Assert.IsFalse(p.Botoes, "aberta e sem resposta: os botoes continuam escondidos");
            p.Responder();
            Assert.IsTrue(p.Botoes, "respondeu: os botoes aparecem");
            Assert.IsFalse(p.Abrir(true, true, Estado.Playing));
        }

        [Test]
        public void Pedido_Respondida_TesteEPrivacidadeArg_NaoAbremSozinha()
        {
            var r = new PedidoIdade(true, false);
            Assert.IsTrue(r.Botoes);
            Assert.IsFalse(r.Abrir(true, true, Estado.Playing), "respondida numa abertura anterior");
            var t = new PedidoIdade(false, true);
            Assert.IsTrue(t.Botoes, "-testsession: fotos e smoke com os botoes do anuncio simulado");
            Assert.IsFalse(t.Abrir(true, true, Estado.Playing), "-testsession nunca abre sozinha");
            var a = new PedidoIdade(false, false);
            a.Aberta();   // -privacidade [18] abriu no Awake
            Assert.IsFalse(a.Abrir(true, false, Estado.Playing));
            Assert.IsFalse(a.Botoes);
        }

        [Test]
        public void Gatilho_D6_NenhumPlacementAntesDa1aMelhoria()
        {
            var s = new FS.Core.Sim();
            foreach (FS.Core.Anuncio x in (FS.Core.Anuncio[])System.Enum.GetValues(typeof(FS.Core.Anuncio)))
                Assert.IsFalse(s.Anuncios.Pode(x), $"{x} antes da 1a melhoria: a tela nao abre");
            Assert.IsTrue(s.Buy(FS.Core.Upgrade.Tools));
            Assert.IsTrue(s.Anuncios.Pode(FS.Core.Anuncio.Velocidade), "1a melhoria: o 2x apareceria, a tela abre");
            Assert.IsTrue(new PedidoIdade(false, false).Abrir(s.Anuncios.Pode(FS.Core.Anuncio.Velocidade), false, Estado.Playing));
        }

        [Test]
        public void Sdk_ConsentimentoAntesDoInit()
        {
            string menor = Privacidade.Gravar(Faixa.Menor13, false), adulto = Privacidade.Gravar(Faixa.Adulto, true);
            Assert.AreEqual(PassoSdk.Espera, Privacidade.Sdk(null, null), "sem resposta: nada de Init");
            Assert.AreEqual(PassoSdk.Inicia, Privacidade.Sdk(null, menor), "1a resposta (qualquer faixa): Init");
            Assert.AreEqual(PassoSdk.Mantem, Privacidade.Sdk(menor, menor), "mesma resposta: nada muda");
            Assert.AreEqual(PassoSdk.Mantem, Privacidade.Sdk(menor, null));
            Assert.AreEqual(PassoSdk.Para, Privacidade.Sdk(menor, adulto), "mudou depois do Init: sem anuncio ate reabrir");
            Assert.IsFalse(Privacidade.Ler("", out _, out _), "PlayerPrefs vazio = sem resposta = Espera");
        }
    }
}
