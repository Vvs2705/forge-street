using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// A-PLAT-08: RemoteConfig no nucleo (RemoteConfig.Aplicar sobre o Tuning) e a fonte local (RemoteConfigLocal). Cada trava com valor fora
    /// da faixa, override parcial, JSON invalido = padroes, aplicar 2x = 1x, nada cria conteudo (chave nova, array maior) e a chave mestra.
    /// </summary>
    public class RemoteConfigTests
    {
        /// <summary>Todos os campos publicos do Tuning num texto (arrays e placements por extenso): Tuning igual = texto igual.</summary>
        static string Dump(object t)
        {
            var sb = new StringBuilder();
            foreach (FieldInfo f in t.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object v = f.GetValue(t);
                sb.Append(f.Name).Append('=');
                if (v is Array a) foreach (object e in a) sb.Append(e is AdPlacementDef ? "{" + Dump(e) + "}" : Convert.ToString(e, CultureInfo.InvariantCulture)).Append(';');
                else sb.Append(Convert.ToString(v, CultureInfo.InvariantCulture));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static RemoteConfig.Resultado Aplica(Tuning t, string json, bool intersticial = false) => RemoteConfig.Aplicar(t, json, intersticial);

        static string Avisos(RemoteConfig.Resultado r) => string.Join(" | ", r.Avisos);

        // ------------------------------------------------------------------ sem config, JSON invalido, parcial, idempotente

        [Test]
        public void SemConfig_NadaMuda_RotuloPadrao()
        {
            foreach (string json in new[] { null, "", "   " })
            {
                var t = new Tuning();
                RemoteConfig.Resultado r = Aplica(t, json);
                Assert.IsFalse(r.Valido);
                Assert.AreEqual(0, r.Avisos.Count, Avisos(r));
                Assert.AreEqual("padrao@0", r.Rotulo);
                Assert.AreEqual(Dump(new Tuning()), Dump(t));
            }
        }

        [Test]
        public void JsonInvalido_ValemOsPadroes()
        {
            string fundo = new string('[', 40) + new string(']', 40);
            string enorme = "{\"id\":\"" + new string('x', RemoteConfig.MaxChars) + "\"}";
            foreach (string json in new[] { "{", "[1,2]", "42", "{\"OfflineFactor\": }", "{\"OfflineFactor\": 0.3} lixo", "{'OfflineFactor': 0.3}",
                         "{\"OfflineFactor\": 0.3,}", "{\"Price\": [1,2,]}", "{\"id\": \"a\\q\"}", "{\"OfflineFactor\": 1e999}", fundo, enorme })
            {
                var t = new Tuning();
                RemoteConfig.Resultado r = Aplica(t, json);
                Assert.IsFalse(r.Valido, json.Length > 60 ? json.Substring(0, 60) : json);
                StringAssert.StartsWith("json:", Avisos(r));
                Assert.AreEqual(Dump(new Tuning()), Dump(t), "JSON que nao abre nao muda nada");
            }
        }

        [Test]
        public void OverrideParcial_SoAsChavesDoJson()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"id\": \"ab_preco\", \"versao\": 3, \"OfflineFactor\": 0.3, \"Price\": [0, 0, 12, 25, 16, 60], \"Encomendas\": false}");
            Assert.IsTrue(r.Valido);
            Assert.AreEqual("ab_preco@3", r.Rotulo);
            Assert.AreEqual(3L, r.Versao);
            Assert.AreEqual(3, r.Aplicadas);
            Assert.AreEqual(0, r.Avisos.Count, Avisos(r));
            Assert.AreEqual(0.3f, t.OfflineFactor);
            Assert.AreEqual(12, t.Price[(int)Item.Sword]);
            Assert.IsFalse(t.Encomendas);
            var esperado = new Tuning { OfflineFactor = 0.3f, Encomendas = false };
            esperado.Price[(int)Item.Sword] = 12;
            Assert.AreEqual(Dump(esperado), Dump(t), "o resto fica no padrao do build");
            Assert.AreEqual(10, new Tuning().Price[(int)Item.Sword], "o array do config e' copia: o padrao de ninguem muda");
        }

        [Test]
        public void SemIdNemVersao_RotuloSemId()
        {
            RemoteConfig.Resultado r = Aplica(new Tuning(), "{\"OfflineFactor\": 0.3}");
            Assert.AreEqual("sem_id@0", r.Rotulo);
        }

        [Test]
        public void AplicarDuasVezes_MesmoTuning()
        {
            const string json = "{\"id\": \"x\", \"versao\": 1, \"CostBase\": 60, \"OfflineMaxSeconds\": 9000, \"Placements.vip.Recarga\": 10, \"Price\": [0, 0, 1000, 25, 16, 60]}";
            var uma = new Tuning(); var duas = new Tuning();
            RemoteConfig.Resultado r1 = Aplica(uma, json);
            Aplica(duas, json); RemoteConfig.Resultado r2 = Aplica(duas, json);
            Assert.AreEqual(Dump(uma), Dump(duas));
            Assert.AreEqual(Avisos(r1), Avisos(r2), "os mesmos presos nas duas");
            Assert.AreEqual(60, duas.CostBase);
        }

        // ------------------------------------------------------------------ travas (cada guardrail com valor fora da faixa)

        [Test]
        public void Trava_Offline_NuncaPassaDe2h_EncurtarVale()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"OfflineMaxSeconds\": 9000}");
            Assert.AreEqual(Balance.OfflineCapSeconds, t.OfflineMaxSeconds);
            Assert.AreEqual(7200L, t.OfflineCap);
            StringAssert.Contains("OfflineMaxSeconds: preso: 9000 -> 7200", Avisos(r));
            Assert.AreEqual(1, r.Aplicadas, "preso tambem vale (na faixa)");

            t = new Tuning();
            r = Aplica(t, "{\"OfflineMaxSeconds\": 3600}");
            Assert.AreEqual(3600, t.OfflineMaxSeconds, "um evento pode encurtar");
            Assert.AreEqual(0, r.Avisos.Count, Avisos(r));
        }

        [Test]
        public void Trava_FatorOffline_NuncaRendeMaisQueOnline()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"OfflineFactor\": 2}");
            Assert.AreEqual(1f, t.OfflineFactor);
            StringAssert.Contains("OfflineFactor: preso: 2 -> 1", Avisos(r));
        }

        [Test]
        public void Trava_RecargasDeAnuncio_NuncaAbaixoDoBuild()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"Placements.vip.Recarga\": 60, \"Placements.velocidade.Recarga\": 0, \"Placements.supply.Recarga\": 300, \"InterstitialCooldown\": 30}");
            Assert.AreEqual(300f, t.Ad(Anuncio.Vip).Recarga, "P14/V11: 5 min");
            Assert.AreEqual(300f, t.Ad(Anuncio.Velocidade).Recarga, "GDD §17.1: 5 min do fim do boost");
            Assert.AreEqual(600f, t.Ad(Anuncio.Supply).Recarga, "GDD §17.1: 10 min");
            Assert.AreEqual(180f, t.InterstitialCooldown);
            string a = Avisos(r);
            StringAssert.Contains("Placements.vip.Recarga: preso: 60 -> 300", a);
            StringAssert.Contains("Placements.velocidade.Recarga: preso: 0 -> 300", a);
            StringAssert.Contains("Placements.supply.Recarga: preso: 300 -> 600", a);
            StringAssert.Contains("InterstitialCooldown: preso: 30 -> 180", a);
            Assert.AreEqual(4, r.Aplicadas);

            t = new Tuning();
            r = Aplica(t, "{\"Placements.vip.Recarga\": 600, \"InterstitialCooldown\": 600}");
            Assert.AreEqual(600f, t.Ad(Anuncio.Vip).Recarga, "mais raro vale");
            Assert.AreEqual(600f, t.InterstitialCooldown);
            Assert.AreEqual(0, r.Avisos.Count, Avisos(r));
            Assert.AreEqual(300f, new Tuning().Ad(Anuncio.Vip).Recarga, "o placement do config e' do Tuning dele: o padrao de ninguem muda");
        }

        [Test]
        public void Trava_TetosDeAnuncio_NuncaAcimaDoBuild()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"AnunciosTetoDia\": 100, \"Placements.encomenda_troca.TetoDia\": 0, \"InterstitialMaxSessao\": 10, "
                + "\"BoostMax\": 5, \"BoostPlayerMax\": 2, \"VipSummonPackMax\": 50, \"Placements.cofre2x.TetoDia\": 3}");
            Assert.AreEqual(30, t.AnunciosTetoDia, "teto diario total");
            Assert.AreEqual(1, t.Ad(Anuncio.EncomendaTroca).TetoDia, "0 seria sem teto proprio: o menor teto que ainda e' teto");
            Assert.AreEqual(3, t.InterstitialMaxSessao);
            Assert.AreEqual(3f, t.BoostMax, "o 2o anuncio sobe a 3x, nunca mais");
            Assert.AreEqual(1.3f, t.BoostPlayerMax);
            Assert.AreEqual(20, t.VipSummonPackMax);
            Assert.AreEqual(0, t.Ad(Anuncio.Cofre).TetoDia, "padrao 0 (so o teto total) fica 0");
            Assert.AreEqual(7, r.Avisos.Count, Avisos(r));

            t = new Tuning();
            r = Aplica(t, "{\"AnunciosTetoDia\": 10, \"Placements.encomenda_troca.TetoDia\": 2, \"InterstitialMaxSessao\": 1}");
            Assert.AreEqual(10, t.AnunciosTetoDia, "menos anuncio vale");
            Assert.AreEqual(2, t.Ad(Anuncio.EncomendaTroca).TetoDia);
            Assert.AreEqual(1, t.InterstitialMaxSessao);
            Assert.AreEqual(0, r.Avisos.Count, Avisos(r));
        }

        [Test]
        public void Trava_ValorDoPlacement_NaFaixa_CofreNoMaximoDobra()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"Placements.cofre2x.Valor\": 3, \"Placements.velocidade.Valor\": 6000, \"Placements.encomenda_bonus.Valor\": 0.25, "
                + "\"Placements.supply.Ligado\": false}");
            Assert.AreEqual(1f, t.Ad(Anuncio.Cofre).Valor, "P2: dobra o mesmo resgate uma vez");
            Assert.AreEqual(600f, t.Ad(Anuncio.Velocidade).Valor, "60 s x 10");
            Assert.AreEqual(0.25f, t.Ad(Anuncio.EncomendaBonus).Valor);
            Assert.IsFalse(t.Ad(Anuncio.Supply).Ligado, "um placement desliga sozinho");
            Assert.IsTrue(t.Ad(Anuncio.Vip).Ligado);
            StringAssert.Contains("Placements.cofre2x.Valor: preso: 3 -> 1", Avisos(r));
            Assert.AreEqual(2, r.Avisos.Count, Avisos(r));
            Assert.AreEqual(4, r.Aplicadas);
        }

        [Test]
        public void Placement_IdOuCampoQueOBuildNaoTem_Ignorado()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"Placements.bau_pago.Ligado\": true, \"Placements.vip.Chance\": 1, \"Placements.vip.Id\": \"x\", \"Placements.vip\": 1, "
                + "\"Price.2.Valor\": 1, \"Placements\": [], \"Placements.vip.Recarga.x\": 1}");
            Assert.AreEqual(0, r.Aplicadas);
            Assert.AreEqual(7, r.Avisos.Count, Avisos(r));
            Assert.AreEqual(Dump(new Tuning()), Dump(t), "o config nao cria placement nem renomeia");
        }

        [Test]
        public void Trava_D6_AnuncioDepoisDaPrimeiraMelhoria_NaoDesliga()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"AnunciosDepoisDaPrimeiraMelhoria\": false}");
            Assert.IsTrue(t.AnunciosDepoisDaPrimeiraMelhoria);
            StringAssert.Contains("AnunciosDepoisDaPrimeiraMelhoria: preso: false -> true", Avisos(r));
        }

        [Test]
        public void Trava_Intersticial_RemotoDesliga_LigarSoComOBuild()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"Interstitial\": true}", intersticial: false);
            Assert.IsFalse(t.Interstitial, "P3/V10: o build de hoje nao permite");
            StringAssert.Contains("Interstitial: preso: true -> false", Avisos(r));

            t = new Tuning();
            Aplica(t, "{\"Interstitial\": true}", intersticial: true);
            Assert.IsTrue(t.Interstitial, "o build do A/B permite");

            t = new Tuning { Interstitial = true };
            r = Aplica(t, "{\"Interstitial\": false}", intersticial: true);
            Assert.IsFalse(t.Interstitial, "desligar sempre vale");
            Assert.AreEqual(0, r.Avisos.Count, Avisos(r));
        }

        [Test]
        public void ChaveMestra_AnunciosFalse_DesligaTodoAnuncio()
        {
            Assert.IsTrue(TuningTests.Liberado().CanBoost, "premissa: com o padrao, depois da 1a melhoria, o boost aparece");
            var t = new Tuning();
            Aplica(t, "{\"AnunciosLigados\": false, \"Interstitial\": true, \"Placements.velocidade.Ligado\": true}", intersticial: true);
            Assert.IsFalse(t.AnunciosLigados);
            Assert.IsFalse(t.Interstitial, "a chave mestra vence a flag do mesmo JSON");
            Sim s = TuningTests.Liberado(t);
            Assert.IsFalse(s.CanBoost, "rewarded some");
            Assert.IsFalse(s.CanSummonVip);
            foreach (Anuncio a in Enum.GetValues(typeof(Anuncio))) Assert.IsFalse(t.Ad(a).Ligado || s.Anuncios.Pode(a), a.ToString());
            Assert.AreEqual("flag", s.Anuncios.VetoIntersticial(true, false, 1000f), "intersticial vetado pela flag");
        }

        [Test]
        public void FaixaGenerica_DezVezesParaCadaLado_PadraoZeroFicaZero()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"CostBase\": 5000, \"FirstClientAt\": 0.1, \"Price\": [5, 0, 1000, 25, 16, 60], \"QualidadePct\": [100, 150, 250, 100]}");
            Assert.AreEqual(500, t.CostBase, "50 x 10");
            Assert.AreEqual(0.8f, t.FirstClientAt, "8 / 10");
            Assert.AreEqual(0, t.Price[(int)Item.Ore], "minerio nao se vende: padrao 0 fica 0");
            Assert.AreEqual(100, t.Price[(int)Item.Sword], "10 x 10");
            Assert.AreEqual(0, t.QualidadePct[3], "Masterwork nunca e' vendida (vai ao Hall)");
            string a = Avisos(r);
            StringAssert.Contains("CostBase: preso: 5000 -> 500", a);
            StringAssert.Contains("Price: preso: [0] 5 -> 0, [2] 1000 -> 100", a);
            StringAssert.Contains("QualidadePct: preso: [3] 100 -> 0", a);
        }

        [Test]
        public void Inteiro_Arredonda_ERegistra()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"QueueCap0\": 4.5}");
            Assert.AreEqual(5, t.QueueCap0);
            StringAssert.Contains("QueueCap0: preso: 4.5 -> 5", Avisos(r));
        }

        [Test]
        public void NadaCriaConteudo_ChaveNovaEArrayDeOutroTamanho_Ignorados()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"id\": \"x\", \"bau_aleatorio_pago\": true, \"Chance\": 0.5, \"Price\": [0, 0, 10, 25, 16, 60, 99], \"QualidadePct\": [100]}");
            Assert.IsTrue(r.Valido);
            Assert.AreEqual(0, r.Aplicadas);
            Assert.AreEqual(4, r.Avisos.Count, Avisos(r));
            StringAssert.Contains("bau_aleatorio_pago: chave desconhecida", Avisos(r));
            StringAssert.Contains("Price: tamanho 7 (o build tem 6", Avisos(r));
            Assert.AreEqual(Dump(new Tuning()), Dump(t), "o config so ajusta o que o build tem");
        }

        [Test]
        public void TipoErrado_Ignorado()
        {
            var t = new Tuning();
            RemoteConfig.Resultado r = Aplica(t, "{\"OfflineFactor\": \"alto\", \"Encomendas\": 1, \"Price\": 5, \"CounterSlotCost\": [150, \"x\", 200, 225], \"Vip\": null}");
            Assert.AreEqual(5, r.Avisos.Count, Avisos(r));
            Assert.AreEqual(0, r.Aplicadas);
            Assert.AreEqual(Dump(new Tuning()), Dump(t));
        }

        /// <summary>Campo novo do Tuning e' chaveavel sem mexer no RemoteConfig, desde que seja bool, int, long, float, array deles ou array de
        /// elementos com Id cujos campos sejam desses (Placements). Falhou = entrou um campo de outro tipo: ensine o RemoteConfig.Aplicar.</summary>
        [Test]
        public void TodoCampoDoTuning_EhChaveavel()
        {
            var tipos = new HashSet<Type> { typeof(bool), typeof(int), typeof(long), typeof(float), typeof(int[]), typeof(long[]), typeof(float[]) };
            foreach (FieldInfo f in typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.IsFalse(f.IsInitOnly, f.Name + " e' readonly");
                if (tipos.Contains(f.FieldType)) continue;
                Type et = f.FieldType.GetElementType();
                Assert.IsTrue(et != null && et.GetField("Id")?.FieldType == typeof(string), f.Name + " (" + f.FieldType.Name + ") nao e' chaveavel pelo RemoteConfig");
                foreach (FieldInfo e in et.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    Assert.IsTrue(e.Name == "Id" || tipos.Contains(e.FieldType), f.Name + ".<id>." + e.Name + " (" + e.FieldType.Name + ") nao e' chaveavel");
                foreach (object o in (Array)f.GetValue(new Tuning()))
                {
                    string id = (string)et.GetField("Id").GetValue(o);
                    Assert.IsFalse(string.IsNullOrEmpty(id) || id.Contains("."), f.Name + ": Id vazio ou com ponto nao vira chave");
                }
            }
        }

        // ------------------------------------------------------------------ JSON

        [Test]
        public void Json_EscapesEIdaEVolta()
        {
            var o = (Dictionary<string, object>)Json.Ler("{\"id\": \"a\\\"b\\\\c\\u00e7\\n\", \"l\": [1, -2.5e1, true, null, {}], \"v\": 0}");
            Assert.AreEqual("a\"b\\c\u00e7\n", o["id"]);
            var l = (List<object>)o["l"];
            Assert.AreEqual(1d, l[0]); Assert.AreEqual(-25d, l[1]); Assert.AreEqual(true, l[2]); Assert.IsNull(l[3]);
            foreach (string s in new[] { "", "a,b", "c:\\x\\y \"z\"", "linha\nnova\ttab\u0001", "ação" })
                Assert.AreEqual(s, Json.Ler(Json.Aspas(s)), s);
        }

        // ------------------------------------------------------------------ fonte local (RemoteConfigLocal)

        string _dir;

        [SetUp] public void Pasta() { _dir = Path.Combine(Path.GetTempPath(), "fs_rc_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_dir); }
        [TearDown] public void Limpa() { if (_dir != null) Directory.Delete(_dir, true); _dir = null; }

        [Test]
        public void Local_RcfileVenceCache_CacheVencePadrao_TestSessionSemCache()
        {
            Assert.IsNull(new RemoteConfigLocal(_dir, null, true).Ler(out string o));
            Assert.AreEqual("padrao", o);
            File.WriteAllText(Path.Combine(_dir, RemoteConfigLocal.Cache), "{\"id\": \"cache\"}");
            File.WriteAllText(Path.Combine(_dir, "dev.json"), "{\"id\": \"dev\"}");
            Assert.AreEqual("{\"id\": \"cache\"}", new RemoteConfigLocal(_dir, null, true).Ler(out o));
            Assert.AreEqual("cache", o);
            Assert.AreEqual("{\"id\": \"dev\"}", new RemoteConfigLocal(_dir, "dev.json", true).Ler(out o), "-rcfile relativo = no persistentDataPath");
            Assert.AreEqual("rcfile", o);
            Assert.AreEqual("{\"id\": \"dev\"}", new RemoteConfigLocal(_dir, Path.Combine(_dir, "dev.json"), true).Ler(out o), "-rcfile absoluto");
            Assert.IsNull(new RemoteConfigLocal(_dir, "nao.json", true).Ler(out o), "rcfile que nao existe = padroes, sem cair no cache");
            Assert.AreEqual("rcfile_ausente", o);
            Assert.IsNull(new RemoteConfigLocal(_dir, "", false).Ler(out o), "-testsession: sem cache");
            Assert.AreEqual("padrao", o);
        }

        [Test]
        public void Local_Guardar_SoOConfigBom_FicaOAnterior()
        {
            var rc = new RemoteConfigLocal(_dir, null, true);
            Assert.IsFalse(rc.Guardar("{lixo"));
            Assert.IsFalse(File.Exists(Path.Combine(_dir, RemoteConfigLocal.Cache)));
            Assert.IsTrue(rc.Guardar("{\"id\": \"v1\", \"versao\": 1}"));
            Assert.IsFalse(rc.Guardar("<html>erro 502</html>"));
            Assert.AreEqual("{\"id\": \"v1\", \"versao\": 1}", rc.Ler(out string o), "o ultimo bom");
            Assert.AreEqual("cache", o);
        }

        [Test]
        public void SemAnuncio_NadaPronto_ShowFalha()
        {
            var a = new SemAnuncio();
            bool falhou = false, deu = false;
            a.Show("vip", () => deu = true, () => falhou = true);
            Assert.IsTrue(falhou && !deu && !a.Ready("vip") && !a.Loaded("vip") && !a.Busy);
        }
    }
}
