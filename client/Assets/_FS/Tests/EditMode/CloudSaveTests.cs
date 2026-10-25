using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>Cloud Save: o resolver entre o save do aparelho e o da nuvem (docs/CLOUD_SAVE.md). Maior progresso (Renome) vence; empate = o
    /// mais novo; nunca baixa o que nao abre nem sobrescreve o save de um build mais novo.</summary>
    public class CloudSaveTests
    {
        static string Save(long renome, long at)
        {
            var imp = new Imperio(new Sim());
            imp.Raiz.Renome.Ganha(renome);
            return SaveEnvelope.Wrap(imp.Save(at), "0.7.0", at);
        }

        [Test]
        public void MaiorProgressoVence_MesmoMaisVelho()
        {
            Assert.AreEqual(CloudSave.Acao.Baixar, CloudSave.Resolver(Save(100, 50), Save(500, 10)), "a nuvem tem mais Renome: baixa");
            Assert.AreEqual(CloudSave.Acao.Subir, CloudSave.Resolver(Save(500, 10), Save(100, 50)), "o aparelho tem mais: sobe por cima");
            Assert.AreEqual(500, CloudSave.Progresso(SaveEnvelope.Unwrap(Save(500, 1)).Payload));
        }

        [Test]
        public void EmpateDeProgresso_OMaisNovoVence()
        {
            Assert.AreEqual(CloudSave.Acao.Baixar, CloudSave.Resolver(Save(200, 10), Save(200, 50)));
            Assert.AreEqual(CloudSave.Acao.Subir, CloudSave.Resolver(Save(200, 50), Save(200, 10)));
            Assert.AreEqual(CloudSave.Acao.Nada, CloudSave.Resolver(Save(200, 10), Save(200, 10)), "o mesmo texto: nada a fazer");
        }

        [Test]
        public void NuvemVaziaOuQueNaoAbre_Sobe_AparelhoVazio_Baixa()
        {
            string bom = Save(300, 20);
            Assert.AreEqual(CloudSave.Acao.Subir, CloudSave.Resolver(bom, null), "1a vez: a nuvem esta vazia");
            Assert.AreEqual(CloudSave.Acao.Subir, CloudSave.Resolver(bom, bom.Replace("at=20", "at=21")), "nuvem com soma errada nunca desce");
            Assert.AreEqual(CloudSave.Acao.Subir, CloudSave.Resolver(bom, "gold=999999"), "sem cabecalho (legado) nao desce");
            Assert.AreEqual(CloudSave.Acao.Baixar, CloudSave.Resolver("", bom), "instalacao nova: a nuvem vale");
            Assert.AreEqual(CloudSave.Acao.Baixar, CloudSave.Resolver(bom.Replace("at=20", "at=21"), bom), "save local corrompido: a nuvem vale");
            Assert.AreEqual(CloudSave.Acao.Nada, CloudSave.Resolver(null, null));
        }

        [Test]
        public void BuildMaisNovo_NuncaSobrescrito()
        {
            string novo = SaveTests.Envelope(Sim.Schema + 1, "gold=1\n");
            Assert.AreEqual(CloudSave.Acao.Nada, CloudSave.Resolver(Save(9999, 99), novo), "a nuvem e' de um build mais novo: nao sobe por cima");
            Assert.AreEqual(CloudSave.Acao.Nada, CloudSave.Resolver(novo, Save(1, 1)), "o aparelho e' mais novo que a nuvem entende: nao baixa");
        }

        [Test]
        public void FlagDesligadaPorPadrao() => Assert.IsFalse(new Tuning().CloudSave);
    }
}
