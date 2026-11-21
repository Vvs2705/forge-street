using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>fix/anuncio-sem-retorno: o vigia do pedido de anuncio sem resposta do SDK e o cofre na volta de uma pausa no anuncio.</summary>
    public class CicloAnuncioTests
    {
        [Test]
        public void SemRetorno_ComOJogoNaFrente_EstouraNoLimite()
        {
            var v = new VigiaAnuncio();
            v.Inicia();
            Assert.IsTrue(v.Ativo);
            bool estourou = false;
            int quadros = 0;
            for (; quadros < 10000 && !estourou; quadros++) estourou = v.Tick(1f / 30f, true);
            Assert.IsTrue(estourou, "SDK sem Closed/DisplayFailed: o pedido nao pode ficar aberto para sempre");
            Assert.AreEqual(VigiaAnuncio.LimiteSeg, quadros / 30f, 0.1f, "estoura no limite, nao antes");
            Assert.IsFalse(v.Ativo);
            Assert.IsFalse(v.Tick(1f, true), "estoura uma vez so");
        }

        [Test]
        public void AnuncioNaFrente_NaoConta()
        {
            var v = new VigiaAnuncio();
            v.Inicia();
            for (int i = 0; i < 30 * 600; i++) Assert.IsFalse(v.Tick(1f / 30f, false), "10 min com o anuncio por cima (app sem foco): e' o anuncio tocando");
            Assert.IsTrue(v.Ativo);
        }

        [Test]
        public void VoltaDoSegundoPlano_NaoContaDeUmaVez()
        {
            var v = new VigiaAnuncio();
            v.Inicia();
            Assert.IsFalse(v.Tick(3600f, true), "o 1o quadro depois de 1 h fora traz um dt enorme: conta no maximo meio segundo");
        }

        [Test]
        public void Parado_NaoEstoura()
        {
            var v = new VigiaAnuncio();
            v.Inicia();
            v.Para();
            Assert.IsFalse(v.Ativo);
            for (int i = 0; i < 1000; i++) Assert.IsFalse(v.Tick(1f, true), "a resposta chegou: o vigia nao age mais");
        }

        [Test]
        public void Cofre_PausaNormal_PagaDepoisDe60s()
        {
            Assert.IsFalse(CicloAnuncio.PagaCofreNaVolta(true, false, 59));
            Assert.IsTrue(CicloAnuncio.PagaCofreNaVolta(true, false, 60));
            Assert.IsFalse(CicloAnuncio.PagaCofreNaVolta(false, false, 3600), "antes do Start quem paga e' a abertura");
        }

        [Test]
        public void Cofre_SaiuNoMeioDoAnuncio_UmaHoraFora_Paga()
        {
            Assert.IsTrue(CicloAnuncio.PagaCofreNaVolta(true, true, 3600), "Home no meio do anuncio + 1 h fora: a hora nao pode se perder");
            Assert.IsFalse(CicloAnuncio.PagaCofreNaVolta(true, true, 45), "a pausa do proprio anuncio (video de 30-45 s) nao e' tempo fora");
            Assert.IsFalse(CicloAnuncio.PagaCofreNaVolta(true, true, CicloAnuncio.ForaMinNoAnuncioS - 1));
            Assert.IsTrue(CicloAnuncio.PagaCofreNaVolta(true, true, CicloAnuncio.ForaMinNoAnuncioS));
        }
    }
}
