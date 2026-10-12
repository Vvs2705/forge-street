using System;
using System.Collections.Generic;
using System.IO;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>IAnalytics de teste: guarda "evento,a,b" em memoria.</summary>
    sealed class AnalyticsMemoria : IAnalytics
    {
        public readonly List<string> Linhas = new List<string>();
        public void Log(string evt, float t, string a = "", string b = "") => Linhas.Add(evt + "," + a + "," + b);
    }

    /// <summary>ISaveStorage de teste: um texto em memoria. Corrompido vai para a Quarentena (como o arquivo e o espelho); SoEnvelope =
    /// como o arquivo, que sempre grava com envelope (sem ele e' lixo); Erro = disco ilegivel (Read e Write lancam).</summary>
    sealed class SaveMemoria : ISaveStorage
    {
        public string Texto, Quarentena;
        public bool SoEnvelope;
        public Exception Erro;

        public SaveEnvelope.Result Read(out int corrupt)
        {
            if (Erro != null) throw Erro;
            SaveEnvelope.Result r = SaveEnvelope.Unwrap(Texto);
            bool bad = r.Status == SaveEnvelope.Status.Corrupt || (SoEnvelope && r.Status == SaveEnvelope.Status.Legacy);
            corrupt = bad ? 1 : 0;
            if (!bad) return r;
            Quarentena = Texto; Texto = null;
            return new SaveEnvelope.Result { Status = SaveEnvelope.Status.Corrupt, Payload = "" };
        }

        public void Write(string text) { if (Erro != null) throw Erro; Texto = text; }
        public void Delete() => Texto = null;
    }

    /// <summary>A-PLAT-06: Salvamento sobre duas ISaveStorage em memoria (a regra que era o Game.LoadSave/Save) e o Diario (formato,
    /// desligado no release, rotacao, faixas de RAM e fps).</summary>
    public class CosturasTests
    {
        SaveMemoria _arq, _esp;
        AnalyticsMemoria _log;
        Salvamento _s;

        [SetUp]
        public void Novo()
        {
            _arq = new SaveMemoria { SoEnvelope = true }; _esp = new SaveMemoria(); _log = new AnalyticsMemoria();
            _s = new Salvamento(_arq, _esp, _log);
        }

        static long Gold(string text) => Sim.Load(SaveEnvelope.Unwrap(text).Payload).Gold;

        // ------------------------------------------------------------------ Salvamento

        [Test]
        public void Salvamento_SemSave_EstadoNovo_GravaArquivoEEspelhoIguais()
        {
            Sim sim = _s.Carregar();
            Assert.AreEqual(0, _log.Linhas.Count, string.Join(" | ", _log.Linhas));
            sim.Gold = 77;
            _s.Gravar(sim, 1000, "0.7.0");
            Assert.AreEqual(_arq.Texto, _esp.Texto, "espelho = arquivo (downgrade le o mesmo texto)");
            Assert.AreEqual(SaveEnvelope.Status.Ok, SaveEnvelope.Unwrap(_arq.Texto).Status);
            Assert.AreEqual(77L, Gold(_arq.Texto));
        }

        [Test]
        public void Salvamento_ArquivoBom_IgnoraOEspelho()
        {
            _arq.Texto = SaveTests.Wrap(10); _esp.Texto = SaveTests.Wrap(20);
            Assert.AreEqual(10L, _s.Carregar().Gold);
            Assert.AreEqual(0, _log.Linhas.Count, string.Join(" | ", _log.Linhas));
        }

        [Test]
        public void Salvamento_ArquivoRuim_VoltaPeloEspelho_ELoga()
        {
            _arq.Texto = "lixo"; _esp.Texto = SaveTests.Wrap(20);
            Assert.AreEqual(20L, _s.Carregar().Gold);
            Assert.AreEqual("lixo", _arq.Quarentena, "o ruim fica na quarentena");
            CollectionAssert.AreEqual(new[] { "save_corrupt,1,Ok", "save_prefs,Ok," }, _log.Linhas);
            Assert.IsFalse(_s.Bloqueado, "o ruim saiu do caminho: a sessao grava");
        }

        [Test]
        public void Salvamento_EspelhoDa06_Migra()
        {
            _esp.Texto = CoreTests.SaveV03;   // sem envelope = Legacy
            Assert.AreEqual(Sim.Load(CoreTests.SaveV03).Save(1), _s.Carregar().Save(1), "carrega igual ao save direto");
            CollectionAssert.AreEqual(new[] { "save_prefs,Legacy," }, _log.Linhas);
        }

        [Test]
        public void Salvamento_TudoRuim_EstadoNovo_SemSobrescreverAQuarentena()
        {
            _arq.Texto = "lixo"; _esp.Texto = "sha256=00\nschema=1\n";
            Assert.AreEqual(0L, _s.Carregar().Gold);
            CollectionAssert.AreEqual(new[] { "save_corrupt,2,Corrupt" }, _log.Linhas);
            _s.Gravar(new Sim(), 1, "0.7.0");
            Assert.AreEqual(("lixo", "sha256=00\nschema=1\n"), (_arq.Quarentena, _esp.Quarentena), "quarentena intacta");
        }

        [Test]
        public void Salvamento_VersaoMaisNova_SessaoNaoGrava()
        {
            string newer = SaveTests.Envelope(Sim.Schema + 1, new Sim().Save(1));
            _arq.Texto = newer; _esp.Texto = SaveTests.Wrap(20);
            _s.Carregar();
            Assert.IsTrue(_s.Bloqueado);
            _s.Gravar(new Sim { Gold = 5 }, 1000, "0.7.0");
            Assert.AreEqual((newer, SaveTests.Wrap(20)), (_arq.Texto, _esp.Texto), "nada gravado por cima");
            StringAssert.StartsWith("save_newer," + (Sim.Schema + 1), _log.Linhas[0]);
        }

        [Test]
        public void Salvamento_IdDesconhecido_CarregaORestoELoga()
        {
            var s = new Sim { Gold = 9 };
            s.Buy(Upgrade.Anvil2);
            _arq.Texto = SaveEnvelope.Wrap(s.Save(1).Replace("ups=bigorna2:0", "ups=bigorna2:0,forja_runica:3"), "0.9.0", 1);   // A-CORE-04: downgrade de conteudo
            Sim sim = _s.Carregar();
            Assert.AreEqual((9L, true), (sim.Gold, sim.Bought[(int)Upgrade.Anvil2]));
            CollectionAssert.AreEqual(new[] { "save_ids_desconhecidos,1,0.9.0" }, _log.Linhas);
            Assert.IsFalse(_s.Bloqueado, "mesmo schema: a sessao grava");
        }

        [Test]
        public void Salvamento_DiscoIlegivel_CarregaOEspelho_ENaoGrava()
        {
            _arq.Erro = new IOException(); _esp.Texto = SaveTests.Wrap(20);
            Assert.AreEqual(20L, _s.Carregar().Gold);
            Assert.IsTrue(_s.Bloqueado);
            CollectionAssert.AreEqual(new[] { "save_error,IOException,", "save_prefs,Ok," }, _log.Linhas);
            _s.Gravar(new Sim(), 1000, "0.7.0");
            Assert.AreEqual(SaveTests.Wrap(20), _esp.Texto, "o espelho tambem nao muda");
        }

        [Test]
        public void Salvamento_ErroAoGravarOArquivo_EspelhoSeguraOProgresso()
        {
            _s.Carregar();
            _arq.Erro = new IOException();
            _s.Gravar(new Sim { Gold = 33 }, 1000, "0.7.0");
            Assert.AreEqual(33L, Gold(_esp.Texto));
            CollectionAssert.AreEqual(new[] { "save_error,IOException," }, _log.Linhas);
        }

        [Test]
        public void Salvamento_Apagar_LimpaOsDois()
        {
            _arq.Texto = SaveTests.Wrap(10); _esp.Texto = SaveTests.Wrap(10);
            _s.Apagar();
            Assert.AreEqual((null as string, null as string), (_arq.Texto, _esp.Texto));
        }

        // ------------------------------------------------------------------ Diario

        string _dir;

        string Pasta()
        {
            _dir = Path.Combine(Path.GetTempPath(), "fs_diario_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            return Path.Combine(_dir, "diario.csv");
        }

        [TearDown]
        public void Limpa() { if (_dir != null) Directory.Delete(_dir, true); _dir = null; }

        [Test]
        public void Diario_Linha_SeisCampos_SemVirgulaNosParametros()
        {
            string p = Pasta();
            var d = new Diario(p);
            d.Log("session_info", 12.34f, "os", "Android OS 13, API-33\nx");
            d.Log("first_sale", 40f, "40.0");
            string[] l = File.ReadAllLines(p);
            Assert.AreEqual(2, l.Length);
            string[] c = l[0].Split(',');
            Assert.AreEqual(6, c.Length, l[0]);
            Assert.AreEqual(8, c[1].Length, "sessao: 8 caracteres aleatorios");
            CollectionAssert.AreEqual(new[] { "session_info", "12.3", "os", "Android OS 13  API-33 x" }, new[] { c[2], c[3], c[4], c[5] });
            Assert.AreEqual(c[1], l[1].Split(',')[1], "mesma sessao no mesmo processo");
            StringAssert.EndsWith(",first_sale,40.0,40.0,", l[1]);
        }

        [Test]
        public void Diario_Desligado_NaoGravaNada()
        {
            string p = Pasta();
            new Diario(null).Log("session_start", 0f, "0.7.0", "x");
            Assert.IsFalse(File.Exists(p));
            Assert.DoesNotThrow(() => new Diario(Path.Combine(_dir, "nao", "existe.csv")).Log("x", 0f), "pasta que nao existe: best-effort");
        }

        [Test]
        public void Diario_PassouDoTeto_FicaACaudaMaisNova()
        {
            string p = Pasta();
            const int Max = 1000;
            var old = new List<string>();
            for (int i = 0; i < 40; i++) old.Add($"2026-10-12T10:00:00,velha000,product_sold,{i}.0,espada,10");   // ~50 B cada, ~2 KB
            File.WriteAllText(p, string.Join("\n", old) + "\n");
            var d = new Diario(p, Max);
            string[] l = File.ReadAllLines(p);
            Assert.LessOrEqual(new FileInfo(p).Length, Max / 2, "ao abrir: cortou ate a metade do teto");
            Assert.AreEqual(old[old.Count - 1], l[l.Length - 1], "a mais nova fica");
            Assert.AreEqual(old[old.Count - l.Length], l[0], "linhas inteiras, na ordem");
            for (int i = 0; i < 200; i++)
            {
                d.Log("product_sold", i, "espada", "10");
                Assert.LessOrEqual(new FileInfo(p).Length, Max, "escrevendo: nunca passa do teto");
            }
            l = File.ReadAllLines(p);
            StringAssert.EndsWith(",product_sold,199.0,espada,10", l[l.Length - 1]);
            foreach (string x in l) Assert.AreEqual(6, x.Split(',').Length, x);
        }

        [Test]
        public void Diario_Faixas_RamEFps()
        {
            // s87: classe nominal (o Android informa menos que a nominal) e fps medio do minuto
            CollectionAssert.AreEqual(new[] { "2GB", "4GB", "6GB", "8GB", "8GB", "12GB", "16GB+" },
                new[] { Diario.FaixaRam(1800), Diario.FaixaRam(3700), Diario.FaixaRam(5600), Diario.FaixaRam(7400), Diario.FaixaRam(8192),
                        Diario.FaixaRam(11000), Diario.FaixaRam(32768) });
            CollectionAssert.AreEqual(new[] { "<20", "20-30", "30-45", "45-55", "55+", "55+" },
                new[] { Diario.FaixaFps(12f), Diario.FaixaFps(29.9f), Diario.FaixaFps(30f), Diario.FaixaFps(50f), Diario.FaixaFps(55f), Diario.FaixaFps(60f) });
        }
    }

    /// <summary>A-PLAT-07: a tabela dos estados (prioridade dos flags) e o que voltar, save e anuncio fazem em cada um. O roteiro do voltar
    /// da V06A (s141/s161: painel, configuracoes, bandeja, tela limpa sai) vale em Playing e Transition; no anuncio o voltar e' ignorado.</summary>
    public class EstadosTests
    {
        static readonly Estado[] Todos = (Estado[])Enum.GetValues(typeof(Estado));

        [Test]
        public void De_PrioridadeDosFlags()
        {
            Assert.AreEqual(Estado.Boot, Estados.De(false, true, true, true, true, true), "sem save carregado nada mais conta");
            Assert.AreEqual(Estado.Loading, Estados.De(true, false, true, true, true, true), "antes do Start");
            Assert.AreEqual(Estado.Ad, Estados.De(true, true, true, true, true, true), "o anuncio real tambem pausa o app: Ad vence Paused");
            Assert.AreEqual(Estado.Paused, Estados.De(true, true, false, true, true, true));
            Assert.AreEqual(Estado.Modal, Estados.De(true, true, false, false, true, true));
            Assert.AreEqual(Estado.Transition, Estados.De(true, true, false, false, false, true));
            Assert.AreEqual(Estado.Playing, Estados.De(true, true, false, false, false, false));
        }

        [Test]
        public void Voltar_FechaDeCimaParaBaixo_SoSaiComTelaLimpa()
        {
            foreach (Estado e in new[] { Estado.Playing, Estado.Transition })
            {
                Assert.AreEqual(Voltar.FechaMenu, Estados.AoVoltar(e, false, true), e + ": bandeja aberta");
                Assert.AreEqual(Voltar.GravaESai, Estados.AoVoltar(e, false, false), e + ": tela limpa");
            }
            Assert.AreEqual(Voltar.FechaPainel, Estados.AoVoltar(Estado.Modal, true, true), "painel (cofre) por cima da bandeja: fecha o painel");
            Assert.AreEqual(Voltar.FechaConfig, Estados.AoVoltar(Estado.Modal, false, true), "configuracoes");
            foreach (Estado e in new[] { Estado.Ad, Estado.Boot, Estado.Loading, Estado.Paused })
                foreach (bool painel in new[] { false, true })
                    foreach (bool menu in new[] { false, true })
                        Assert.AreEqual(Voltar.Ignora, Estados.AoVoltar(e, painel, menu), $"{e} painel={painel} menu={menu}: nunca sai nem fecha");
        }

        [Test]
        public void SaveEAnuncio_PorEstado()
        {
            foreach (Estado e in Todos)
            {
                Assert.AreEqual(e != Estado.Boot && e != Estado.Loading, Estados.Grava(e), "grava: " + e);
                Assert.AreEqual(e == Estado.Playing || e == Estado.Transition, Estados.Anuncia(e), "anuncia: " + e);
            }
            Assert.AreEqual(7, Todos.Length, "estado novo = decidir voltar, save e anuncio dele aqui");
        }
    }
}
