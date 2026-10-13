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
        internal static string Envelope(int schema, string payload)
        {
            string body = "schema=" + schema + "\ncontent=9.9.9\nat=5\n" + payload;
            using (SHA256 sha = SHA256.Create())
                return "sha256=" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(body))).Replace("-", "").ToLowerInvariant() + "\n" + body;
        }

        internal static string Wrap(int gold)
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

        // ------------------------------------------------------------------ A-CORE-04: save por ID estavel

        /// <summary>Partida de 25 min do bot (ate a Joalheria: ajudantes, VIP, encomenda) com parcial em TODOS os pads: visivel, do menu e de
        /// cadeia ja comprada.</summary>
        static Sim Partida25()
        {
            // os schemas 1 e 2 sao de antes dos niveis de estacao (B-CORE-03): o bot joga sem eles para o estado caber nos formatos antigos
            int[] semNiveis = new int[16];   // B-CORE-07: os 16 (II-III e IV-V)
            for (int i = 0; i < semNiveis.Length; i++) semNiveis[i] = 1 << 30;
            var s = new Sim(new Tuning { NivelEstacaoCusto = semNiveis }); var bot = new Bot();
            for (float t = 0f; t < 25f * 60f; t += Dt) bot.Step(s, Dt);
            foreach (Pad p in s.Pads) if (p.Chain[0] != Upgrade.LongAnvil) p.Paid = 10 + p.Slot;   // B-CORE-07: o pad da bigorna longa nao existe no schema 1-2 (e sem o projeto esconde)
            return s;
        }

        /// <summary>B-CORE-01: o mesmo estado no formato do schema 2 (0.8): v=2 e estoque, vendas e maos so por posicao (stock=/sold=/hold=/wk=,
        /// que o schema 3 ainda grava como espelho), sem as chaves por ID.</summary>
        static string ComoSchema2(string v3) =>
            string.Join("\n", Array.FindAll(v3.Split('\n'), l => !l.StartsWith("stocks=") && !l.StartsWith("solds=") && !l.StartsWith("holds=") && !l.StartsWith("wks=")))
                .Replace("v=" + Sim.Schema + "\n", "v=2\n");

        /// <summary>O mesmo estado no formato do schema 1 (0.1-0.7): v=1 e up=/ut=/pads= por indice do enum e do slot no lugar de ups=/upp=.</summary>
        static string ComoSchema1(Sim s, string v3)
        {
            string rest = string.Join("\n", Array.FindAll(ComoSchema2(v3).Split('\n'), l => !l.StartsWith("v=") && !l.StartsWith("ups=") && !l.StartsWith("up=") && !l.StartsWith("upp=")));
            string up = string.Concat(Array.ConvertAll(s.Bought, b => b ? "1" : "0"));
            string ut = string.Join(",", Array.ConvertAll(s.UpgradeTime, t => t.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)));
            string pads = string.Concat(s.Pads.ConvertAll(p => p.Paid + ","));
            return "v=1\nup=" + up + "\nut=" + ut + "\npads=" + pads + "\n" + rest;
        }

        /// <summary>
        /// Aceite do A-CORE-04: o save grava IDs, entao a ordem da tabela (e do enum) nao entra. "Build novo" = a tabela em outra ordem,
        /// injetada no Load: (1) as mesmas definicoes em outras posicoes carregam identico; (2) com o enum renumerado (cada ID noutro indice,
        /// como se alguem reordenasse o enum), cada compra e o instante dela vao para o indice novo do mesmo ID. O espelho up= (ordem
        /// congelada do schema 1) esta no texto e nao pode ser lido: se fosse, (2) ficaria vermelho.
        /// </summary>
        [Test]
        public void SavePorId_TabelaReordenada_CarregaOMesmoProgresso()
        {
            Sim s = Partida25();
            string text = s.Save(1000);
            StringAssert.StartsWith("v=" + Sim.Schema + "\n", text);
            Sim a = Sim.Load(text);
            CollectionAssert.AreEqual(s.Bought, a.Bought);
            Assert.Greater(a.UpgradesBought, 10, "premissa: progresso de verdade");

            int n = Upgrades.Count;
            var invertida = new UpgradeDef[n];
            for (int i = 0; i < n; i++) invertida[n - 1 - i] = Upgrades.All[i];
            Assert.AreEqual(a.Save(1000), Sim.Load(text, null, invertida).Save(1000), "(1) mesmas definicoes, outra ordem: identico");

            var renumerada = new UpgradeDef[n];
            for (int i = 0; i < n; i++)
            {
                UpgradeDef d = Upgrades.All[i];
                renumerada[n - 1 - i] = new UpgradeDef((Upgrade)(n - 1 - i), d.Id, -1, d.Name, d.Desc, d.Luxury, d.InMenu);
            }
            Sim b = Sim.Load(text, null, renumerada);
            for (int j = 0; j < n; j++)
                Assert.AreEqual((a.Bought[n - 1 - j], a.UpgradeTime[n - 1 - j]), (b.Bought[j], b.UpgradeTime[j]), renumerada[j].Id + ": a compra segue o ID, nao o indice");
            Assert.AreEqual((a.UpgradesBought, 0), (b.UpgradesBought, b.UnknownIds));
        }

        [Test]
        public void Migracao_V1ParaV2_MesmoEstadoNosDoisFormatos_CarregaIgual()
        {
            Sim s = Partida25();
            string v3 = s.Save(1000), v1 = ComoSchema1(s, v3);
            StringAssert.DoesNotContain("ups=", v1);
            StringAssert.DoesNotContain("stocks=", v1);
            Assert.AreEqual(Sim.Load(v3).Save(1000), Sim.Load(v1).Save(1000), "flags, instantes e parcial de cada pad (o do menu e o da cadeia comprada voltam ao ouro)");
            Assert.AreEqual(Sim.Load(v3).Gold, Sim.Load(v1).Gold);
        }

        /// <summary>B-CORE-01: o schema 2 (estoque, vendas e maos por posicao) e o 3 (por ID) do mesmo estado carregam identicos, numa partida com
        /// estoque, vendas e carga em todas as maos (ferreiro e ajudantes).</summary>
        [Test]
        public void Migracao_V2ParaV3_MesmoEstadoNosDoisFormatos_CarregaIgual()
        {
            Sim s = Partida25();
            Assert.IsTrue(s.SoldItems[(int)Item.Sword] > 0 && s.SoldItems[(int)Item.Shield] > 0 && s.Workers.Count >= 3, "premissa: vendas e ajudantes de verdade");
            s.Player.Held[(int)Item.Ore] = 1; s.Player.Held[(int)Item.Jewel] = 2; s.Stock[(int)Item.Tool] = 2;
            Array.Clear(s.Workers[0].Held, 0, s.Workers[0].Held.Length); s.Workers[0].Held[(int)Item.Ore] = 1;   // papel 0: minerio
            string v3 = s.Save(1000), v2 = ComoSchema2(v3);
            StringAssert.Contains("stocks=", v3);
            StringAssert.Contains("wks=0:minerio:1", v3);
            StringAssert.DoesNotContain("holds=", v2);
            Sim a = Sim.Load(v3), b = Sim.Load(v2);
            Assert.AreEqual(a.Save(1000), b.Save(1000));
            CollectionAssert.AreEqual(s.Stock, b.Stock);
            CollectionAssert.AreEqual(s.SoldItems, b.SoldItems);
            CollectionAssert.AreEqual(s.Player.Held, b.Player.Held);
            for (int k = 0; k < a.Workers.Count; k++) CollectionAssert.AreEqual(a.Workers[k].Held, b.Workers[k].Held, "ajudante " + k);
        }

        [Test]
        public void SavePorId_IdDesconhecido_FicaDeFora_ContaEDevolveOParcial()
        {
            var s = new Sim { Gold = 100 };
            s.Buy(Upgrade.Anvil2);
            string text = s.Save(5);
            StringAssert.Contains("ups=bigorna2:0\n", text);
            StringAssert.Contains("upp=\n", text);
            // conteudo de um build mais novo (downgrade): um upgrade que este build nao tem, com parcial; e parcial de um upgrade so de menu
            text = text.Replace("ups=bigorna2:0\n", "ups=forja_runica:12.5,bigorna2:0,\n").Replace("upp=\n", "upp=forja_runica:30,balcao5:20\n");
            Sim b = Sim.Load(text);
            Assert.AreEqual((true, 1, 2, 150L), (b.Bought[(int)Upgrade.Anvil2], b.UpgradesBought, b.UnknownIds, b.Gold), "o resto carrega; os 50 do parcial sem pad voltam ao ouro");
            StringAssert.DoesNotContain("forja_runica", b.Save(5), "regravar descarta o desconhecido");
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
            Assert.AreEqual((SaveEnvelope.Status.Ok, 20L, 0), (r.Status, Gold(r), corrupt));
            File.Delete(_path);   // queda entre os Move da troca manual: so sobrou o .bak
            r = SaveStore.Read(_path, out corrupt);
            Assert.AreEqual((SaveEnvelope.Status.Ok, 10L, 0), (r.Status, Gold(r), corrupt));
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
            Assert.AreEqual((SaveEnvelope.Status.Ok, 10L, 1), (r.Status, Gold(r), corrupt), "volta pelo .bak");
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
