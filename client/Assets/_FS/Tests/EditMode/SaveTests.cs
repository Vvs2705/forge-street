using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>A-CORE-01 (envelope: soma, schema, downgrade, migracao) e A-PLAT-02 (SaveStore numa pasta temporaria: troca atomica,
    /// recuperacao pelo .bak, quarentena, versao mais nova intacta, falha ao carregar nao sobrescreve).</summary>
    public class SaveTests
    {
        const float Dt = 1f / 30f;
        string _dir, _path;

        [SetUp]
        public void Pasta()
        {
            _dir = Path.Combine(Path.GetTempPath(), "fs_save_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _path = Path.Combine(_dir, "save.txt");
        }

        [TearDown]
        public void Limpa() => Directory.Delete(_dir, true);

        /// <summary>Save no formato da 0.6: o bot joga 5 min (VIP, encomenda, ajudantes com carga, maos cheias).</summary>
        static string Save06()
        {
            var s = new Sim();
            var bot = new Bot();
            for (float t = 0f; t < 300f; t += Dt) bot.Step(s, Dt);
            return s.Save(1000);
        }

        /// <summary>Envelope montado a mao (prova o formato: sha256 = SHA-256 hex do resto) e com qualquer schema.</summary>
        static string Envelope(int schema, string payload)
        {
            string body = "schema=" + schema + "\ncontent=9.9.9\nat=5\n" + payload;
            using (SHA256 sha = SHA256.Create())
                return "sha256=" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(body))).Replace("-", "").ToLowerInvariant() + "\n" + body;
        }

        static string Wrap(int gold)
        {
            var s = new Sim { Gold = gold };
            return SaveEnvelope.Wrap(s.Save(1), "0.7.0", 1);
        }

        static long Gold(SaveEnvelope.Result r) => Sim.Load(r.Payload).Gold;

        string[] Quarentena(string name) => Directory.GetFiles(_dir, name + "*.corrupt-*");

        // ------------------------------------------------------------------ envelope (nucleo)

        [Test]
        public void Envelope_IdaEVolta_Identica_EDowngradeLe()
        {
            string payload = Save06();
            Assert.AreEqual(Envelope(Sim.Schema, payload), SaveEnvelope.Wrap(payload, "9.9.9", 5), "cabecalho fixo e soma SHA-256 do resto");
            string text = SaveEnvelope.Wrap(payload, "0.7.0", 1234);
            SaveEnvelope.Result r = SaveEnvelope.Unwrap(text);
            Assert.AreEqual((SaveEnvelope.Status.Ok, payload), (r.Status, r.Payload));
            Assert.AreEqual((Sim.Schema, "0.7.0", 1234L), (r.Schema, r.Content, r.WrittenAt));
            Assert.AreEqual(payload, Sim.Load(r.Payload).Save(1000), "ida e volta identica");
            // downgrade: o laco chave=valor e' o mesmo da 0.6 (chave desconhecida cai no default); o texto inteiro carrega igual
            Assert.AreEqual(payload, Sim.Load(text).Save(1000));
        }

        [Test]
        public void Envelope_SavesAntigos_SaoLegacyECarregamIguais()
        {
            foreach (string old in new[] { CoreTests.SaveV01, CoreTests.SaveV02, CoreTests.Save20Flags, CoreTests.SaveV03, CoreTests.SaveAntesLeva11, Save06() })
            {
                SaveEnvelope.Result r = SaveEnvelope.Unwrap(old);
                Assert.AreEqual((SaveEnvelope.Status.Legacy, old), (r.Status, r.Payload), "sem cabecalho = Legacy, payload intocado");
                string direct = Sim.Load(old).Save(1);
                Assert.AreEqual(direct, Sim.Load(r.Payload).Save(1));
                SaveEnvelope.Result again = SaveEnvelope.Unwrap(SaveEnvelope.Wrap(direct, "0.7.0", 1));   // 1o save do build novo
                Assert.AreEqual((SaveEnvelope.Status.Ok, direct), (again.Status, again.Payload));
                Assert.AreEqual(direct, Sim.Load(again.Payload).Save(1), "estavel depois de migrar");
            }
        }

        [Test]
        public void Envelope_UmByteAlterado_Corrupt_SemPayload()
        {
            string text = SaveEnvelope.Wrap(Save06(), "0.7.0", 1);
            for (int i = "sha256=".Length; i < text.Length; i++)   // cada byte da soma, do cabecalho e do payload
            {
                char c = text[i] == '0' ? '1' : '0';
                SaveEnvelope.Result r = SaveEnvelope.Unwrap(text.Substring(0, i) + c + text.Substring(i + 1));
                Assert.AreEqual((SaveEnvelope.Status.Corrupt, ""), (r.Status, r.Payload), "byte " + i);
            }
            Assert.AreEqual(SaveEnvelope.Status.Corrupt, SaveEnvelope.Unwrap(text.Substring(0, text.Length - 1)).Status, "truncado");
            Assert.AreEqual(SaveEnvelope.Status.Corrupt, SaveEnvelope.Unwrap("sha256=").Status);
            Assert.AreEqual(SaveEnvelope.Status.Corrupt, SaveEnvelope.Unwrap("sha256=abc\nschema=1\n").Status);
            Assert.AreEqual(SaveEnvelope.Status.Missing, SaveEnvelope.Unwrap(null).Status);
        }

        [Test]
        public void Envelope_SchemaMaisNovo_NewerSchema()
        {
            SaveEnvelope.Result r = SaveEnvelope.Unwrap(Envelope(Sim.Schema + 1, Save06()));
            Assert.AreEqual((SaveEnvelope.Status.NewerSchema, Sim.Schema + 1, "9.9.9", 5L), (r.Status, r.Schema, r.Content, r.WrittenAt));
        }

        [Test]
        public void Migracao_TodaVersaoAnteriorTemPasso()
        {
            Assert.AreEqual(Sim.Schema - 1, Sim.Migrations.Length, "um passo por versao anterior");
            foreach (Func<string, string> m in Sim.Migrations) Assert.IsNotNull(m);
            StringAssert.StartsWith("v=" + Sim.Schema + "\n", new Sim().Save(1), "o payload grava o schema");
        }

        // ------------------------------------------------------------------ SaveStore (arquivo)

        [Test]
        public void Store_TrocaAtomica_AnteriorViraBak()
        {
            SaveStore.Write(_path, Wrap(10));
            Assert.IsFalse(File.Exists(_path + ".bak") || File.Exists(_path + ".tmp"));
            SaveStore.Write(_path, Wrap(20));
            Assert.AreEqual(Wrap(10), File.ReadAllText(_path + ".bak"), "o anterior vira .bak");
            Assert.IsFalse(File.Exists(_path + ".tmp"));
            SaveEnvelope.Result r = SaveStore.Read(_path, out int corrupt);
            Assert.AreEqual((SaveEnvelope.Status.Ok, 20, 0), (r.Status, Gold(r), corrupt));
            File.Delete(_path);   // queda entre os Move da troca manual: so sobrou o .bak
            r = SaveStore.Read(_path, out corrupt);
            Assert.AreEqual((SaveEnvelope.Status.Ok, 10, 0), (r.Status, Gold(r), corrupt));
            File.Delete(_path + ".bak");
            Assert.AreEqual(SaveEnvelope.Status.Missing, SaveStore.Read(_path, out corrupt).Status);
        }

        [Test]
        public void Store_PrincipalCorrompido_RecuperaPeloBak_EQuarentena()
        {
            SaveStore.Write(_path, Wrap(10));
            SaveStore.Write(_path, Wrap(20));
            byte[] bad = File.ReadAllBytes(_path);
            bad[bad.Length - 3] ^= 1;   // 1 bit no payload
            File.WriteAllBytes(_path, bad);
            SaveEnvelope.Result r = SaveStore.Read(_path, out int corrupt);
            Assert.AreEqual((SaveEnvelope.Status.Ok, 10, 1), (r.Status, Gold(r), corrupt), "volta pelo .bak");
            string[] q = Quarentena("save.txt");
            Assert.AreEqual(1, q.Length);
            CollectionAssert.AreEqual(bad, File.ReadAllBytes(q[0]), "o ruim fica intacto na quarentena");
            Assert.IsFalse(File.Exists(_path), "saiu do caminho");
            SaveStore.Write(_path, Wrap(30));
            Assert.AreEqual(30, Gold(SaveStore.Read(_path, out corrupt)));
            Assert.AreEqual(10, Gold(SaveEnvelope.Unwrap(File.ReadAllText(_path + ".bak"))), "o .bak bom nao vira o principal ruim");
            CollectionAssert.AreEqual(bad, File.ReadAllBytes(q[0]));
        }

        [Test]
        public void Store_FalhaAoCarregar_NaoSobrescreve()
        {
            // sem .bak: lixo, vazio e save sem envelope (o arquivo sempre sai com envelope) vao para a quarentena; o estado novo grava ao lado
            string[] junks = { "lixo", "", CoreTests.SaveV03 };
            for (int i = 0; i < junks.Length; i++)
            {
                string p = Path.Combine(_dir, "save" + i + ".txt");
                File.WriteAllText(p, junks[i]);
                SaveEnvelope.Result r = SaveStore.Read(p, out int corrupt);
                Assert.AreEqual((SaveEnvelope.Status.Corrupt, "", 1), (r.Status, r.Payload, corrupt), junks[i]);
                Assert.IsFalse(File.Exists(p), "saiu do caminho");
                SaveStore.Write(p, Wrap(0));
                string[] q = Quarentena("save" + i + ".txt");
                Assert.AreEqual(1, q.Length);
                Assert.AreEqual(junks[i], File.ReadAllText(q[0]), "o ruim nunca e' sobrescrito nem apagado");
            }
            File.WriteAllText(_path, "lixo");
            File.WriteAllText(_path + ".bak", "sha256=00\nschema=1\n");
            Assert.AreEqual(SaveEnvelope.Status.Corrupt, SaveStore.Read(_path, out int both).Status);
            Assert.AreEqual((2, 2), (both, Quarentena("save.txt").Length), "principal e .bak ruins: os dois na quarentena");
        }

        [Test]
        public void Store_SchemaMaisNovo_NaoUsaOBakNemMexeEmNada()
        {
            string newer = Envelope(Sim.Schema + 1, new Sim().Save(1));
            File.WriteAllText(_path, newer);
            File.WriteAllText(_path + ".bak", Wrap(10));
            SaveEnvelope.Result r = SaveStore.Read(_path, out int corrupt);
            Assert.AreEqual((SaveEnvelope.Status.NewerSchema, 0), (r.Status, corrupt));
            Assert.AreEqual((newer, Wrap(10)), (File.ReadAllText(_path), File.ReadAllText(_path + ".bak")), "nada mexido");
            Assert.AreEqual(0, Quarentena("save.txt").Length);
            File.WriteAllText(_path, "lixo");   // principal ruim e .bak de versao mais nova: o .bak fica intacto
            File.WriteAllText(_path + ".bak", newer);
            Assert.AreEqual(SaveEnvelope.Status.NewerSchema, SaveStore.Read(_path, out corrupt).Status);
            Assert.AreEqual((1, newer), (corrupt, File.ReadAllText(_path + ".bak")));
        }
    }
}
