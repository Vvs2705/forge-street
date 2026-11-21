using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>Quem grava o save real (Sessao.Persiste): o -bot sozinho no aparelho do testador nao pode trocar o progresso dele pelo do bot.</summary>
    public class SessaoTests
    {
        [Test]
        public void Jogo_normal_grava()
        {
            Assert.IsFalse(FS.Sessao.Teste(false, false, false));
            Assert.IsTrue(FS.Sessao.Persiste(false, false, false));
        }

        [Test]
        public void Testsession_autoplay_e_record_nao_gravam()
        {
            Assert.IsTrue(FS.Sessao.Teste(true, false, false));
            Assert.IsTrue(FS.Sessao.Teste(false, true, false));
            Assert.IsTrue(FS.Sessao.Teste(false, false, true));
            Assert.IsFalse(FS.Sessao.Persiste(true, false, false));
            Assert.IsFalse(FS.Sessao.Persiste(true, true, true));   // -botgrava nao abre o save de uma sessao de teste
        }

        [Test]
        public void Bot_sozinho_nao_grava()
        {
            Assert.IsFalse(FS.Sessao.Persiste(false, true, false));
        }

        [Test]
        public void Bot_com_botgrava_grava()
        {
            Assert.IsTrue(FS.Sessao.Persiste(false, true, true));
        }

        [Test]
        public void Botgrava_sem_bot_e_jogo_normal()
        {
            Assert.IsTrue(FS.Sessao.Persiste(false, false, true));
        }
    }
}
