using System;
using System.Collections.Generic;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>IProvedorAnalytics de teste: guarda o que foi enviado; Lanca = SDK que da erro.</summary>
    sealed class ProvedorMemoria : IProvedorAnalytics
    {
        public readonly List<Evento> Enviados = new List<Evento>();
        public readonly List<bool> Inicios = new List<bool>();
        public bool Lanca;
        public void Iniciar(bool idPublicidade) { Inicios.Add(idPublicidade); if (Lanca) throw new InvalidOperationException("sdk"); }
        public void Enviar(Evento e) { if (Lanca) throw new InvalidOperationException("sdk"); Enviados.Add(e); }
    }

    /// <summary>
    /// A-PLAT-09: Telemetria (o IAnalytics do Game). Nada vai ao remoto antes da resposta da tela de idade; a fila tem teto; ID de publicidade
    /// so 18+ com aceite (V21); excecao com teto por sessao e sem caminho privado; esquema versionado que abre como JSON; provedor que lanca
    /// nao derruba. O diario local recebe tudo (o teto e a rotacao dele estao no CosturasTests).
    /// </summary>
    public class TelemetriaTests
    {
        AnalyticsMemoria _local;
        ProvedorMemoria _remoto;
        Telemetria _t;

        [SetUp]
        public void Novo()
        {
            _local = new AnalyticsMemoria(); _remoto = new ProvedorMemoria();
            _t = new Telemetria(_local, _remoto, "abcd1234", "0.7.0", "ab_preco@3", "C:/Users/Fulano/AppData/LocalLow/VStack/Forge Street");
        }

        [Test]
        public void AntesDaResposta_NadaSai_DepoisAFilaSaiEmOrdem()
        {
            _t.Log("session_start", 0f, "0.7.0", "x");
            _t.Log("tutorial_step", 3.5f, "pegar");
            Assert.AreEqual(0, _remoto.Enviados.Count, "antes da tela de idade nada sai do aparelho");
            Assert.AreEqual(0, _remoto.Inicios.Count, "nem o provedor inicia");
            Assert.AreEqual(2, _local.Linhas.Count, "o diario local recebe na hora");
            _t.Liberar(Faixa.Adulto, false);
            _t.Log("first_sale", 40f);
            Assert.AreEqual(3, _remoto.Enviados.Count);
            Assert.AreEqual(new[] { 1L, 2L, 3L }, _remoto.Enviados.ConvertAll(e => e.Seq).ToArray());
            Assert.AreEqual("tutorial_step", _remoto.Enviados[1].Nome);
            Assert.AreEqual("pegar", _remoto.Enviados[1].A);
            Assert.AreEqual(3.5f, _remoto.Enviados[1].T);
            Assert.AreEqual("ab_preco@3", _remoto.Enviados[2].Config);
            Assert.AreEqual("abcd1234", _remoto.Enviados[2].Sessao);
            Assert.AreEqual("0.7.0", _remoto.Enviados[2].App);
        }

        [Test]
        public void IdDePublicidade_So18ComAceite()
        {
            foreach (Faixa f in new[] { Faixa.Menor13, Faixa.De13a17, Faixa.Adulto })
                foreach (bool aceitou in new[] { false, true })
                {
                    var r = new ProvedorMemoria();
                    var t = new Telemetria(_local, r, "s", "v", "c");
                    t.Liberar(f, aceitou);
                    Assert.AreEqual(new[] { f == Faixa.Adulto && aceitou }, r.Inicios.ToArray(), f + " aceitou=" + aceitou);
                }
        }

        [Test]
        public void Liberar_UmaVezPorSessao()
        {
            _t.Liberar(Faixa.Adulto, true);
            _t.Liberar(Faixa.Menor13, false);   // mudou na revisao: vale na proxima abertura, como o anuncio
            Assert.AreEqual(new[] { true }, _remoto.Inicios.ToArray());
            Assert.IsTrue(_t.Liberada);
        }

        [Test]
        public void FilaCheia_DescartaOMaisVelho()
        {
            for (int i = 0; i < Telemetria.FilaMax + 10; i++) _t.Log("fps_bucket", i);
            Assert.AreEqual(10, _t.Descartados);
            _t.Liberar(Faixa.De13a17, false);
            Assert.AreEqual(Telemetria.FilaMax, _remoto.Enviados.Count);
            Assert.AreEqual(11L, _remoto.Enviados[0].Seq, "os 10 mais velhos cairam");
            Assert.AreEqual(Telemetria.FilaMax + 10, _local.Linhas.Count, "o diario local nao perde nada");
        }

        [Test]
        public void Excecao_TetoPorSessao_SemCaminhoPrivado_UmaLinhaDaPilha()
        {
            _t.Liberar(Faixa.Adulto, false);
            string msg = "IOException: Could not find file \"C:/Users/Fulano/AppData/LocalLow/VStack/Forge Street/save.txt\"";
            for (int i = 0; i < Telemetria.ExcecoesMax; i++) Assert.IsTrue(_t.Excecao(msg, "FS.SaveStore.Read ()\nFS.Game.Awake ()", 12f));
            Assert.IsFalse(_t.Excecao(msg, "x", 13f), "passou do teto: nada");
            Assert.AreEqual(Telemetria.ExcecoesMax, _remoto.Enviados.Count);
            Evento e = _remoto.Enviados[0];
            Assert.AreEqual("excecao", e.Nome);
            StringAssert.DoesNotContain("Fulano", e.A);
            StringAssert.Contains("~/save.txt", e.A);
            Assert.AreEqual("FS.SaveStore.Read ()", e.B);
            StringAssert.DoesNotContain("Fulano", string.Join("|", _local.Linhas), "nem no diario");
            _t.Excecao(new string('x', 500), null, 1f);
            Assert.AreEqual(Telemetria.ExcecoesMax, _remoto.Enviados.Count);
            Assert.IsTrue(new Telemetria(_local, _remoto, "s", "v", "c").Excecao(new string('x', 500), null, 1f), "outra sessao, outro teto");
        }

        [Test]
        public void Texto_Cortado_NoRemoto()
        {
            _t.Liberar(Faixa.Adulto, false);
            _t.Log("x", 0f, new string('a', 500), null);
            Assert.AreEqual(Telemetria.TextoMax, _remoto.Enviados[0].A.Length);
            Assert.AreEqual("", _remoto.Enviados[0].B);
        }

        [Test]
        public void Evento_EsquemaVersionado_AbreComoJson()
        {
            var e = new Evento { Seq = 7, Nome = "rc_aviso", A = "Price: preso: [2] 1000 -> 100, [3] \"x\"", B = "linha\nnova", T = 12.34f, Sessao = "abcd1234", App = "0.7.0", Config = "ab@3" };
            var o = (Dictionary<string, object>)Json.Ler(e.ParaJson());
            Assert.AreEqual((double)Evento.Esquema, o["v"]);
            Assert.AreEqual(7d, o["seq"]);
            Assert.AreEqual("rc_aviso", o["nome"]);
            Assert.AreEqual(e.A, o["a"]);
            Assert.AreEqual(e.B, o["b"]);
            Assert.AreEqual(12.3d, o["t"]);
            Assert.AreEqual("abcd1234", o["sessao"]);
            Assert.AreEqual("0.7.0", o["app"]);
            Assert.AreEqual("ab@3", o["config"]);
            Assert.AreEqual(9, o.Count, "esquema 1: so estes campos (nada de ID de aparelho, conta, local ou publicidade)");
        }

        [Test]
        public void ProvedorQueLanca_NaoDerrubaOJogo()
        {
            _remoto.Lanca = true;
            _t.Log("a", 0f);
            Assert.DoesNotThrow(() => _t.Liberar(Faixa.Adulto, true));
            Assert.DoesNotThrow(() => _t.Log("b", 1f));
            Assert.AreEqual(2, _local.Linhas.Count);
        }

        [Test]
        public void ProvedorNulo_EcoDoQueSeriaEnviado()
        {
            var eco = new List<string>();
            var t = new Telemetria(_local, new ProvedorNulo(eco.Add), "s", "v", "c");
            t.Log("session_start", 0f);
            Assert.AreEqual(0, eco.Count);
            t.Liberar(Faixa.Menor13, false);
            Assert.AreEqual(2, eco.Count);
            StringAssert.Contains("\"id_publicidade\":false", eco[0]);
            StringAssert.Contains("\"nome\":\"session_start\"", eco[1]);
            Assert.DoesNotThrow(() => new Telemetria(_local, new ProvedorNulo(), "s", "v", "c").Log("x", 0f));
        }
    }
}
