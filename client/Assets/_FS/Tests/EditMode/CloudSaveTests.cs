using FS.Core;
using System.Threading.Tasks;
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

        /// <summary>Espera o Task numa thread sem o contexto do Unity: .Result na thread principal do EditMode trava (o await do timeout volta
        /// para o contexto que o proprio .Result esta segurando). No dotnet nao ha contexto e o .Result direto passa.</summary>
        static T Sinc<T>(System.Func<Task<T>> f) => Task.Run(f).Result;

        /// <summary>Nuvem de teste: Texto = o que esta la; Falha = lanca (sem rede); Trava = nunca responde.</summary>
        sealed class NuvemFalsa : INuvemSave
        {
            public string Texto; public bool Falha, Trava; public int Gravacoes;
            public Task<string> Ler() => Trava ? new TaskCompletionSource<string>().Task : Falha ? Task.FromException<string>(new System.Net.WebException()) : Task.FromResult(Texto);
            public Task Gravar(string t)
            {
                if (Trava) return new TaskCompletionSource<bool>().Task;
                if (Falha) return Task.FromException(new System.Net.WebException());
                Texto = t; Gravacoes++;
                return Task.CompletedTask;
            }
        }

        [Test]
        public void Sincronia_PrimeiraVezSobe_DepoisBaixaOQueTemMaisProgresso()
        {
            var n = new NuvemFalsa();
            string local = Save(100, 10);
            var r = Sinc(() => new SincroniaNuvem(n).Abrir(local, 0));
            Assert.AreEqual((CloudSave.Acao.Subir, (string)null), r);
            Assert.AreEqual(local, n.Texto, "a nuvem recebe o envelope inteiro");
            n.Texto = Save(900, 5);   // outro aparelho jogou mais
            r = Sinc(() => new SincroniaNuvem(n).Abrir(local, 0));
            Assert.AreEqual(CloudSave.Acao.Baixar, r.Acao);
            Assert.AreEqual(n.Texto, r.Texto, "o Game grava este texto e recarrega");
        }

        [Test]
        public void Sincronia_Offline_NuncaLancaNemTrava()
        {
            string local = Save(100, 10);
            var falha = new SincroniaNuvem(new NuvemFalsa { Falha = true });
            Assert.AreEqual(CloudSave.Acao.Nada, Sinc(() => falha.Abrir(local, 0)).Acao);
            Assert.AreEqual("WebException", falha.Erro);
            var lenta = new SincroniaNuvem(new NuvemFalsa { Trava = true }, 50);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Assert.AreEqual(CloudSave.Acao.Nada, Sinc(() => lenta.Abrir(local, 0)).Acao);
            Assert.Less(sw.ElapsedMilliseconds, 5000, "o timeout solta a abertura");
            Assert.AreEqual("timeout", lenta.Erro);
            Assert.IsFalse(Sinc(() => lenta.Subir(local, 100)));
        }

        [Test]
        public void Sincronia_SobeNoMaximo1PorIntervalo()
        {
            var n = new NuvemFalsa();
            var s = new SincroniaNuvem(n);
            Assert.IsTrue(Sinc(() => s.Subir(Save(1, 1), 0)));
            Assert.IsFalse(Sinc(() => s.Subir(Save(2, 2), 30)), "dentro de 60 s: espera o proximo save");
            Assert.IsTrue(Sinc(() => s.Subir(Save(3, 3), 60)));
            Assert.AreEqual(2, n.Gravacoes);
            n.Falha = true;
            Assert.IsFalse(Sinc(() => s.Subir(Save(4, 4), 200)), "sem rede: false, e o proximo save tenta de novo");
            n.Falha = false;
            Assert.IsTrue(Sinc(() => s.Subir(Save(5, 5), 201)));
        }
    }
}
