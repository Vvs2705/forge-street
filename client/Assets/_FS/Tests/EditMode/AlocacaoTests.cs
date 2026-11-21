using System;
using System.IO;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// fix/gc-alocacao-por-quadro (soak de 60 min, docs/SOAK.md: ~14 GC/min no build dev). O Diario grava em lote e nao perde linha no Flush; as
    /// chaves pre-montadas (Colecao.Tx, Metas.Chave) sao o mesmo texto de antes (vao ao ledger e ao save); e, no dotnet, os caminhos quentes
    /// (Mixagem.Pedir com Importante na fila, Sim.Wanted, Colecao.Pago, Metas.Chave) nao alocam mais nada por chamada.
    /// </summary>
    public class AlocacaoTests
    {
        string _dir;

        string Pasta()
        {
            _dir = Path.Combine(Path.GetTempPath(), "fs_lote_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            return Path.Combine(_dir, "diario.csv");
        }

        [TearDown]
        public void Limpa() { if (_dir != null) Directory.Delete(_dir, true); _dir = null; }

        [Test]
        public void Diario_EmLote_FlushGravaTodasAsLinhasNaOrdem()
        {
            string p = Pasta();
            double agora = 0;
            var d = new Diario(p, Diario.MaxBytes, () => agora);
            Assert.IsTrue(d.Ativo);
            const int N = 200;   // ~13 KB: abaixo do BufferMax (16 K caracteres), so o relogio ou o Flush gravam
            for (int i = 0; i < N; i++) d.Log("product_sold", i * 0.5f, "espada,longa", i.ToString());
            Assert.IsFalse(File.Exists(p) && new FileInfo(p).Length > 0, "antes dos 5 s e sem Flush: nada no disco (1 escrita por lote)");
            d.Flush();
            string[] l = File.ReadAllLines(p);
            Assert.AreEqual(N, l.Length, "nenhuma linha perdida no Flush");
            for (int i = 0; i < N; i++)
            {
                string[] c = l[i].Split(',');
                Assert.AreEqual(6, c.Length, l[i]);
                Assert.IsTrue(DateTime.TryParseExact(c[0], "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _), c[0]);
                Assert.AreEqual(d.Sessao, c[1]);
                // o mesmo texto da linha de antes (concat + Replace): t com 1 casa e a virgula do parametro virando espaco
                string antes = "product_sold," + (i * 0.5f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ",espada longa," + i;
                Assert.AreEqual(antes, string.Join(",", c, 2, 4));
            }
            d.Flush();
            Assert.AreEqual(N, File.ReadAllLines(p).Length, "Flush sem nada pendente nao duplica");
        }

        [Test]
        public void Diario_PassaramCincoSegundos_GravaSemFlush()
        {
            string p = Pasta();
            double agora = 0;
            var d = new Diario(p, Diario.MaxBytes, () => agora);
            d.Log("a", 1f);
            d.Log("b", 2f);
            agora = Diario.FlushS;
            d.Log("c", 3f);   // esta leva o lote (a, b, c)
            string[] l = File.ReadAllLines(p);
            Assert.AreEqual(3, l.Length);
            StringAssert.EndsWith(",c,3.0,,", l[2]);
        }

        [Test]
        public void Telemetria_SemLeitor_NaoAtiva_ComDiarioOuRemoto_Ativa()
        {
            var off = new Diario(null);
            Assert.IsFalse(off.Ativo);
            Assert.IsFalse(new Telemetria(off, new ProvedorNulo(null), "s", "v", "c").Ativo, "diario desligado e remoto mudo: ninguem le");
            Assert.IsTrue(new Telemetria(off, new ProvedorNulo(_ => { }), "s", "v", "c").Ativo, "-analyticslog: o eco le");
            Assert.IsTrue(new Telemetria(off, new ProvedorMemoria(), "s", "v", "c").Ativo, "provedor de verdade (UGS): le depois da tela de idade");
            Assert.IsTrue(new Telemetria(new Diario(Pasta()), new ProvedorNulo(null), "s", "v", "c").Ativo, "diario ligado (build dev)");
        }

        [Test]
        public void Colecao_TxPreMontado_IgualAoPrefixoMaisId()
        {
            foreach (MarcoColecaoDef m in MasterworksDefs.Marcos) Assert.AreEqual(Colecao.Prefixo + m.Id, Colecao.Tx(m), m.Id);
            var s = new Sim(new Tuning());
            foreach (MarcoColecaoDef m in s.Colecao.Marcos) Assert.AreEqual("col:" + m.Id, Colecao.Tx(m), "o ID de transacao do ledger/save");
        }

        [Test]
        public void Metas_ChaveDoCache_IgualAConcatenacao()
        {
            foreach (string ev in new[] { "product_sold", "quality_crafted", "worker_hired", "x" })
                for (int f = -3; f < 1100; f++)
                {
                    string antes = f < 0 ? ev : ev + "." + f.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    Assert.AreEqual(antes, Metas.Chave(ev, f));
                    Assert.AreEqual(antes, Metas.Chave(ev, f), "2a vez (do cache)");
                }
        }

#if FS_CORETESTS
        // So no dotnet: o GC.GetAllocatedBytesForCurrentThread do Mono do Unity nao e' preciso o bastante para cravar 0.
        static long Aloca(Action a, int n = 1000)
        {
            a();   // aquece (cache, JIT)
            long antes = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < n; i++) a();
            return GC.GetAllocatedBytesForCurrentThread() - antes;
        }

        [Test]
        public void CaminhosQuentes_NaoAlocamPorChamada()
        {
            var m = new Mixagem(_ => 10f);
            Assert.IsTrue(m.Pedir("compra", 0f, out _));
            Assert.IsFalse(m.Pedir("vip", 0.1f, out _), "outro Importante soando: vai para a fila");
            long pedir = Aloca(() => m.Pedir("vip", 0.2f, out _));

            var s = new Sim(new Tuning());
            long wanted = Aloca(() => s.Wanted(Item.Sword));
            MarcoColecaoDef marco = s.Colecao.Marcos[0];
            long pago = Aloca(() => s.Colecao.Pago(marco));
            long chave = Aloca(() => Metas.Chave("product_sold", 3));
            TestContext.Progress.WriteLine($"ALOCACAO bytes/1000 chamadas: Pedir={pedir} Wanted={wanted} Pago={pago} Chave={chave}");
            Assert.AreEqual(0L, pedir, "Mixagem.Pedir com o Importante ja na fila (era a closure do Exists)");
            Assert.AreEqual(0L, wanted, "Sim.Wanted (era a closure do Exists)");
            Assert.AreEqual(0L, pago, "Colecao.Pago (era Prefixo + Id por marco por passo)");
            Assert.AreEqual(0L, chave, "Metas.Chave (era evento + \".\" + filtro por evento)");
        }
#endif
    }
}
