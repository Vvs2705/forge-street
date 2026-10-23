using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// A-AUD-23: os sons sintetizados saem validos (finitos, normalizados, sem estalo no fim) e iguais toda vez, os lacos emendam sem
    /// salto e o ambiente cresce com o negocio. Para OUVIR sem o Unity: Som_GravaWav (Explicit) grava tudo em .wav.
    /// </summary>
    public class SomTests
    {
        /// <summary>AUDIO.md §9: os efeitos CC0 da Kenney (Resources/Audio/&lt;som&gt;/&lt;som&gt;_k.wav) trocam o sintetizado sem recalibrar a Mixagem: WAV mono
        /// 16 bits a 22 050 Hz, sem clipar, curto, e o RMS da janela de 50 ms mais forte a 1 dB do clip da Sintese que substitui.</summary>
        [Test]
        public void Cc0_NoNivelDoSintetizado()
        {
            string dir = AudioDir();
            if (!Directory.Exists(dir)) Assert.Ignore("audio fora do alcance: " + dir);
            string[] sons = Directory.GetDirectories(dir);
            Assert.That(sons.Length, Is.GreaterThanOrEqualTo(7), "hammer, martelo, ore, drop, esteira, click e coin");
            foreach (string pasta in sons)
            {
                string n = Path.GetFileName(pasta);
                CollectionAssert.Contains(Sintese.Nomes, n, "pasta com nome de som da Sintese");
                string[] wavs = Directory.GetFiles(pasta, "*.wav");
                Assert.IsNotEmpty(wavs, n);
                double alvo = Rms50(Sintese.Efeito(n, 0));
                foreach (string f in wavs)
                {
                    byte[] b = File.ReadAllBytes(f);
                    Assert.AreEqual(1, BitConverter.ToInt16(b, 22), f + ": mono");
                    Assert.AreEqual(Sintese.Taxa, BitConverter.ToInt32(b, 24), f + ": taxa");
                    Assert.AreEqual(16, BitConverter.ToInt16(b, 34), f + ": 16 bits");
                    float[] x = new float[(b.Length - 44) / 2];
                    for (int i = 0; i < x.Length; i++) x[i] = BitConverter.ToInt16(b, 44 + 2 * i) / 32768f;
                    Assert.That(x.Length / (float)Sintese.Taxa, Is.InRange(0.005f, 1.5f), f + ": duracao");
                    Assert.Less(Pico(x, f), 0.99f, f + ": sem clipar");
                    Assert.AreEqual(alvo, Rms50(x), 1.0, f + ": RMS 50 ms (dB) do sintetizado");
                }
            }
        }

        static double Rms50(float[] x)
        {
            int n = Sintese.Taxa / 20, passo = Sintese.Taxa / 200;
            double melhor = 0;
            for (int i = 0; i <= Math.Max(0, x.Length - n); i += passo)
            {
                double s = 0; int m = Math.Min(n, x.Length - i);
                for (int k = 0; k < m; k++) s += x[i + k] * x[i + k];
                melhor = Math.Max(melhor, s / m);
            }
            return 10 * Math.Log10(Math.Max(melhor, 1e-12));
        }

        static string AudioDir([System.Runtime.CompilerServices.CallerFilePath] string aqui = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(aqui), "../../Resources/Audio"));

        [Test]
        public void Efeitos_ValidosEDeterministicos()
        {
            foreach (string n in Sintese.Nomes)
                for (int v = 0; v < Sintese.Variacoes(n); v++)
                {
                    float[] b = Sintese.Efeito(n, v);
                    Assert.Greater(b.Length, Sintese.Taxa / 50, n);
                    Assert.That(Pico(b, n + v), Is.InRange(0.4f, 0.96f), n + v + ": normalizado pelo pico");
                    Assert.AreEqual(0f, b[b.Length - 1], 1e-6f, n + v + ": termina em zero (sem estalo)");
                    CollectionAssert.AreEqual(b, Sintese.Efeito(n, v), n + v + ": mesma semente, mesmo som");
                }
            CollectionAssert.AreNotEqual(Sintese.Efeito("hammer", 0), Sintese.Efeito("hammer", 1), "variacoes diferem");
            Assert.Throws<ArgumentException>(() => Sintese.Efeito("nada", 0));
        }

        /// <summary>Os dois jeitos de fechar o laco, num seno que nao cabe inteiro em L (sem eles a emenda salta ~1): a ultima amostra emenda
        /// na primeira com um passo do tamanho dos de dentro.</summary>
        [Test]
        public void Lacos_EmendamSemSalto()
        {
            const int L = 1000, X = 200;
            var seno = new float[L + X];
            for (int i = 0; i < seno.Length; i++) seno[i] = (float)Math.Sin(2 * Math.PI * i / 97.3);
            Assert.Greater(Math.Abs(seno[0] - seno[L - 1]), 0.9f, "o seno cru salta na emenda");
            Emenda(Sintese.Laco(seno, L, X), "cross-fade");

            var nota = new float[L + X];   // nota que comeca 73 amostras antes do fim (3/4 de ciclo: perto do pico na emenda) e soa alem dele
            for (int i = L - 73; i < nota.Length; i++) nota[i] = (float)(Math.Sin(2 * Math.PI * (i - L + 73) / 97.3) * Math.Exp(-(i - L + 73) / 400.0));
            Assert.Greater(Math.Abs(nota[L - 1]), 0.5f, "a nota crua some de repente na emenda");
            Emenda(Sintese.Dobra(nota, L), "cauda");

            Real(Sintese.Fogo(), 4 * Sintese.Taxa, "fogo");
            Real(Sintese.Rua(), 6 * Sintese.Taxa, "rua");
            Real(Sintese.Musica(), (int)(Sintese.MusicaS * Sintese.TaxaMusica), "musica");
        }

        static void Emenda(float[] o, string n)
        {
            float passo = 0f;
            for (int i = 1; i < o.Length; i++) passo = Math.Max(passo, Math.Abs(o[i] - o[i - 1]));
            Assert.LessOrEqual(Math.Abs(o[0] - o[o.Length - 1]), passo * 1.01f, n);
        }

        static void Real(float[] b, int len, string n)
        {
            Assert.AreEqual(len, b.Length, n);
            Assert.That(Pico(b, n), Is.InRange(0.85f, 0.91f), n + ": normalizado");
        }

        [Test]
        public void Ambiente_CresceComONegocio()
        {
            var (f, f2, r, r2) = Sintese.Camadas(0, 0);
            Assert.AreEqual(0.5f, f, 1e-6f, "oficina parada: fogo baixo");
            Assert.AreEqual(0.4f, r, 1e-6f, "fila vazia: rua de fundo");
            Assert.AreEqual(0f, f2 + r2, 1e-6f, "camadas de densidade so com movimento");
            for (int a = 0; a <= 8; a++)
                for (int q = 0; q <= 14; q++)
                {
                    var c = Sintese.Camadas(a, q);
                    var mais = Sintese.Camadas(a + 1, q + 1);
                    Assert.That(mais.fogo >= c.fogo && mais.fogo2 >= c.fogo2 && mais.rua >= c.rua && mais.rua2 >= c.rua2, $"{a},{q}: nunca diminui");
                    Assert.That(c.fogo <= 1f && c.fogo2 <= 1f && c.rua <= 1f && c.rua2 <= 1f, $"{a},{q}: teto 1");
                }
            var cheio = Sintese.Camadas(7, 13);
            Assert.AreEqual(1f, cheio.fogo2, 1e-6f, "7 estacoes trabalhando: as duas camadas de fogo");
            Assert.AreEqual(1f, cheio.rua2, 1e-6f, "filas cheias: as duas camadas de rua");
        }

        /// <summary>
        /// Para ouvir sem o Unity: grava cada som em .wav (16 bits, mono) e imprime duracao, memoria e tempo de geracao.
        /// dotnet test client/tools/coretests --filter Name=Som_GravaWav --logger "console;verbosity=detailed"
        /// Pasta: variavel FS_SONS, senao %TEMP%/fs_sons.
        /// </summary>
        [Test, Explicit("grava arquivos; so quando pedido")]
        public void Som_GravaWav()
        {
            string dir = Environment.GetEnvironmentVariable("FS_SONS") ?? Path.Combine(Path.GetTempPath(), "fs_sons");
            Directory.CreateDirectory(dir);
            var log = new StringBuilder();
            long total = 0;
            var sw = Stopwatch.StartNew();
            foreach (string n in Sintese.Nomes)
                for (int v = 0; v < Sintese.Variacoes(n); v++)
                {
                    float[] b = Sintese.Efeito(n, v);
                    total += b.Length;
                    Wav(Path.Combine(dir, $"sfx_{n}_{v}.wav"), b, Sintese.Taxa);
                    log.AppendLine(string.Format(CultureInfo.InvariantCulture, "sfx_{0}_{1}: {2:0.00} s", n, v, b.Length / (double)Sintese.Taxa));
                }
            log.AppendLine(string.Format(CultureInfo.InvariantCulture, "efeitos: {0:0.0} s de audio, {1} KB em float, gerados em {2:0} ms (com a gravacao)",
                total / (double)Sintese.Taxa, total * 4 / 1024, sw.Elapsed.TotalMilliseconds));
            Laco(dir, log, "amb_fogo", Sintese.Fogo, Sintese.Taxa);
            Laco(dir, log, "amb_rua", Sintese.Rua, Sintese.Taxa);
            Laco(dir, log, "musica", Sintese.Musica, Sintese.TaxaMusica);
            log.AppendLine("pasta: " + dir);
            TestContext.WriteLine(log.ToString());
        }

        static void Laco(string dir, StringBuilder log, string nome, Func<float[]> gera, int rate)
        {
            var sw = Stopwatch.StartNew();
            float[] b = gera();
            double ms = sw.Elapsed.TotalMilliseconds;
            Wav(Path.Combine(dir, nome + ".wav"), b, rate);
            float[] dois = new float[b.Length * 2];   // 2 voltas seguidas: a emenda se ouve aqui
            Array.Copy(b, dois, b.Length); Array.Copy(b, 0, dois, b.Length, b.Length);
            Wav(Path.Combine(dir, nome + "_2voltas.wav"), dois, rate);
            log.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1:0.0} s a {2} Hz, {3} KB em float, gerado em {4:0} ms", nome,
                b.Length / (double)rate, rate, b.Length * 4 / 1024, ms));
        }

        static float Pico(float[] b, string n)
        {
            float m = 0f;
            foreach (float x in b)
            {
                Assert.IsFalse(float.IsNaN(x) || float.IsInfinity(x), n + ": amostra invalida");
                m = Math.Max(m, Math.Abs(x));
            }
            Assert.LessOrEqual(m, 1f, n + ": sem clipar");
            return m;
        }

        static void Wav(string path, float[] b, int rate)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + b.Length * 2); w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(b.Length * 2);
                foreach (float x in b) w.Write((short)Math.Round(Math.Max(-1f, Math.Min(1f, x)) * 32767));
            }
        }
    }
}
