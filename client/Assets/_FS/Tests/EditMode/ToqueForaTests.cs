using System;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>fix/ui-fechar-ao-tocar-fora: o classificador do toque fora (tap fecha, arraste nao) e as regras de quem fecha.</summary>
    public class ToqueForaTests
    {
        const float R = 30f;   // raio em px (12 dp a 2,5x)

        [TearDown]
        public void Limpa() => ToqueFora.Bloqueios -= SempreBloqueia;

        static bool SempreBloqueia() => true;

        [Test]
        public void TapParadoECurto_EToque()
        {
            var g = new ToqueFora();
            g.Aperta(100f, 500f, 10f);
            Assert.IsTrue(g.Ativo);
            Assert.AreEqual(Gesto.Toque, g.Solta(104f, 497f, 10.2f, R), "5 px em 0,2 s = tap");
            Assert.IsFalse(g.Ativo, "o gesto termina ao soltar");
        }

        [Test]
        public void Arrastar_NaoEToque()
        {
            var g = new ToqueFora();
            g.Aperta(100f, 500f, 10f);
            Assert.AreEqual(Gesto.Arraste, g.Solta(100f + R + 1f, 500f, 10.1f, R), "joystick/rolagem: passou do raio");
        }

        [Test]
        public void IdaEVolta_ContaComoArraste()
        {
            var g = new ToqueFora();
            g.Aperta(100f, 500f, 10f);
            g.Move(220f, 500f, R);   // andou com o joystick e voltou ao ponto de partida
            Assert.AreEqual(Gesto.Arraste, g.Solta(101f, 500f, 10.3f, R), "o meio do gesto conta, nao so o fim");
        }

        [Test]
        public void SegurarMuito_NaoEToque()
        {
            var g = new ToqueFora();
            g.Aperta(100f, 500f, 10f);
            Assert.AreEqual(Gesto.Arraste, g.Solta(100f, 500f, 10f + ToqueFora.MaxSeg + 0.05f, R));
        }

        [Test]
        public void SoltarSemApertar_EnadaECancelaZera()
        {
            var g = new ToqueFora();
            Assert.AreEqual(Gesto.Nenhum, g.Solta(0f, 0f, 1f, R), "o dedo veio de dentro do painel: nao e' toque fora");
            g.Aperta(1f, 1f, 1f);
            g.Cancela();
            Assert.IsFalse(g.Ativo);
            Assert.AreEqual(Gesto.Nenhum, g.Solta(1f, 1f, 1.1f, R));
        }

        [Test]
        public void Menu_FechaSoJogandoComBandejaAberta()
        {
            foreach (Estado e in new[] { Estado.Playing, Estado.Transition })
            {
                Assert.IsTrue(ToqueFora.FechaMenu(e, true, false), e + ": bandeja aberta fecha");
                Assert.IsFalse(ToqueFora.FechaMenu(e, false, false), e + ": nada aberto, o toque e' do mundo");
            }
            foreach (Estado e in new[] { Estado.Modal, Estado.Ad, Estado.Paused, Estado.Boot, Estado.Loading })
                Assert.IsFalse(ToqueFora.FechaMenu(e, true, false), e + ": nao fecha (o modal por cima responde pelo veu)");
        }

        [Test]
        public void Menu_TutorialNaBandeja_NaoFecha()
        {
            Assert.IsFalse(ToqueFora.FechaMenu(Estado.Playing, true, true), "o passo do tutorial manda comprar pela bandeja");
        }

        [Test]
        public void Bloqueio_Generico_SeguraTudo()
        {
            Assert.IsFalse(ToqueFora.Bloqueado);
            ToqueFora.Bloqueios += SempreBloqueia;
            Assert.IsTrue(ToqueFora.Bloqueado);
            Assert.IsFalse(ToqueFora.FechaMenu(Estado.Playing, true, false), "cartao do tutorial/boas-vindas registrado: nao fecha");
            Assert.IsFalse(ToqueFora.FechaPainel(true, 0), "nem o painel informativo");
            ToqueFora.Bloqueios -= SempreBloqueia;
            Assert.IsFalse(ToqueFora.Bloqueado);
        }

        [Test]
        public void Painel_SoOInformativoSemOuroFecha()
        {
            Assert.IsTrue(ToqueFora.FechaPainel(true, 0), "detalhe da estacao, mapa sem acao, -adtest: so o OK");
            Assert.IsFalse(ToqueFora.FechaPainel(false, 0), "escolha (receita, modo do altar, acao do mapa): so pelo botao");
            Assert.IsFalse(ToqueFora.FechaPainel(true, 125000), "cofre: recompensa pendente nao some por toque fora");
        }
    }
}
