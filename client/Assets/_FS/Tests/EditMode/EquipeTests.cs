using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// B-CORE-04 (GDD §8, ECONOMIA_WAVE_B §2, CONTEUDO_V1 §6): WorkerDef em dados, os 6 ajudantes migrados com o mesmo comportamento no nivel 1,
    /// a regra de cada papel (Porter, Smith, Clerk, Quartermaster, Foreman), a compra (revelacao, requisito, ouro, nivel maximo), o save (wkn=
    /// opcional, save antigo igual), as metas, o validator e o bot (identico sem os workers novos; contrata depois da producao completa).
    /// NUnit 3.5 do Unity: tipos exatos nos dois lados (long com L, float com f).
    /// </summary>
    public class EquipeTests
    {
        const float Dt = 1f / 30f;

        static int K(string id) => WorkerDefs.IndexOf(id);

        /// <summary>Producao ate as Ferramentas (a revelacao): Fole, Bigorna 2, Ajudante 1 e 2, Escudos, Mochila, Esteira, Fole duplo, Botas.</summary>
        static Sim Aberta(params Upgrade[] mais)
        {
            var s = new Sim();
            for (int i = 0; i <= (int)Upgrade.Tools; i++) s.Buy((Upgrade)i);
            foreach (Upgrade u in mais) s.Buy(u);
            return s;
        }

        static void Compra(Sim s, string id, int vezes = 1)
        {
            for (int i = 0; i < vezes; i++)
            {
                s.Gold += s.Equipe.Custo(K(id));
                Assert.IsTrue(s.Equipe.Comprar(K(id)), $"{id}: compra {i + 1}");
            }
        }

        // ------------------------------------------------------------------ dados e migracao

        [Test]
        public void Golden_IdsNuncaMudam_E_OsTrezeDoOldForge()
        {
            CollectionAssert.AreEqual(new[] { "wk_ajudante1", "wk_ajudante2", "wk_ajudante3", "wk_joalheiro1", "wk_mineiro", "wk_joalheiro2",
                "wk_bruna", "wk_tome", "wk_iara", "wk_joaquim", "wk_celeste", "wk_baltazar", "wk_otavio" }, WorkerDefs.All.Select(d => d.Id).ToArray(),
                "o save grava o ID (wkn=): renomear ou reordenar os ajudantes pede passo em Sim.Migrations; worker novo entra no fim");
            var papeis = WorkerDefs.All.Select(d => d.Papel).Distinct().ToArray();
            CollectionAssert.AreEquivalent(new[] { WorkerRole.Porter, WorkerRole.Smith, WorkerRole.Clerk, WorkerRole.Quartermaster, WorkerRole.Foreman }, papeis,
                "Old Forge: carregador, oficial, balconista, almoxarife e mestre de obras (fundidor, mensageiro e gerente nos distritos)");
            Assert.IsEmpty(ContentValidator.Workers(WorkerDefs.All, ContentValidator.World(), RenomeDefs.Niveis).ToList());
        }

        [Test]
        public void Migracao_OsSeisAjudantes_MesmoUpgradeRotaEOrdem_NivelUmIgualAntes()
        {
            WorkerDef[] p = WorkerDefs.All.Where(d => d.Papel == WorkerRole.Porter).ToArray();
            CollectionAssert.AreEqual(new[] { "ajudante1", "ajudante2", "ajudante3", "joalheiro1", "mineiro", "joalheiro2" }, p.Select(d => d.Contrata).ToArray(),
                "a ordem do antigo Sim.HireBy");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0, 3 }, p.Select(d => d.Rota).ToArray(), "o antigo Sim.HireRole");
            var s = new Sim();
            for (int u = 0; u < Upgrades.Count; u++) s.Buy((Upgrade)u);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0, 3 }, s.Workers.Select(w => w.Role).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, s.Workers.Select(w => w.Wk).ToArray(), "cada ajudante sabe qual WorkerDef ele e'");
            foreach (Carrier w in s.Workers)
            {
                Assert.AreEqual(1, s.Equipe.Nivel(w.Wk));
                Assert.AreEqual(Balance.WorkerSpeedUp, w.Speed, "nivel 1 = o passo de antes, bit a bit");
                Assert.AreEqual(Balance.WorkerCapUp, w.Cap);
            }
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, Sim.Load(s.Save(1)).Workers.Select(w => w.Wk + 1).ToArray(), "do save, na mesma ordem");
        }

        [Test]
        public void SemNinguem_TodoEfeitoENeutro()
        {
            Sim s = Aberta();
            var e = s.Equipe;
            foreach (Station st in s.Stations)
            {
                Assert.AreEqual(1f, e.Ritmo(st), st.Name);
                Assert.AreEqual(0, e.Saida(st) + e.QualidadeOficial(st) + e.QualidadeMestre(st), st.Name);
            }
            Assert.AreEqual(1f, e.Paciencia);
            Assert.AreEqual(Balance.ServeTime, e.Atendimento);
            Assert.AreEqual(0, e.Estoque);
            Assert.AreEqual((Balance.FurnaceOut, Balance.CrafterOut, s.T.CounterCap0), (s.FurnaceA.OutCap, s.AnvilA.OutCap, s.CounterCap));
        }

        // ------------------------------------------------------------------ compra

        [Test]
        public void Revelacao_NadaAVendaAntesDasFerramentas_NemComAFlagDesligada()
        {
            var s = new Sim();
            s.Buy(Upgrade.Helper1);
            s.Gold = 100000;
            for (int k = 0; k < WorkerDefs.All.Length; k++) Assert.IsFalse(s.Equipe.AVenda(k), WorkerDefs.All[k].Id + " antes das Ferramentas");
            Assert.AreEqual(-1, s.Equipe.MaisBarato());
            Assert.IsFalse(s.Equipe.Comprar(K("wk_ajudante1")));
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            Assert.IsTrue(s.Equipe.AVenda(K("wk_ajudante1")), "ajudante contratado: o nivel 2 abre com as Ferramentas");
            Assert.IsFalse(s.Equipe.AVenda(K("wk_mineiro")), "ajudante sem o upgrade: o pad contrata, a Equipe nao");
            Assert.AreEqual(-1L, s.Equipe.Custo(K("wk_mineiro")));
            var off = new Sim(new Tuning { Workers = false });
            off.Buy(Upgrade.Helper1); off.Buy(Upgrade.Shields); off.Buy(Upgrade.Tools); off.Gold = 100000;
            Assert.IsFalse(off.Equipe.AVenda(K("wk_ajudante1")), "flag desligada: nada novo a venda");
        }

        [Test]
        public void Comprar_CobraNaHora_SemOuroOuNoMaximoNadaMuda()
        {
            Sim s = Aberta();
            int k = K("wk_ajudante1");
            s.Gold = 249;
            Assert.IsFalse(s.Equipe.Comprar(k), "sem ouro");
            Assert.AreEqual((249L, 1), (s.Gold, s.Equipe.Nivel(k)));
            CollectionAssert.AreEqual(new long[] { 250, 400, 640, 1025 }, WorkerDefs.All[k].Niveis, "ECONOMIA_WAVE_B §2.2");
            long gasto = 0;
            for (int n = 2; n <= 5; n++)
            {
                long c = s.Equipe.Custo(k);
                gasto += c;
                s.Gold = c;
                Assert.IsTrue(s.Equipe.Comprar(k));
                Assert.AreEqual((0L, n), (s.Gold, s.Equipe.Nivel(k)));
            }
            Assert.AreEqual(2315L, gasto);
            Assert.AreEqual(gasto, s.Equipe.Gasto);
            Assert.AreEqual(-1L, s.Equipe.Custo(k), "nivel 5 = o maximo no Old Forge");
            s.Gold = 100000;
            Assert.IsFalse(s.Equipe.Comprar(k));
            Assert.AreEqual(100000L, s.Gold);
        }

        [Test]
        public void Requisito_UpgradeERenome_TravamAContratacao()
        {
            Sim s = Aberta(Upgrade.CounterCapacity, Upgrade.Counter5);
            s.Gold = 100000;
            Assert.IsFalse(s.Equipe.AVenda(K("wk_bruna")), "Bruna espera as Bigornas II");
            Assert.IsFalse(s.Equipe.AVenda(K("wk_celeste")), "Celeste espera o Balcao 6");
            Assert.IsFalse(s.Equipe.AVenda(K("wk_baltazar")), "Baltazar espera o Renome 1");
            Assert.IsFalse(s.Equipe.AVenda(K("wk_otavio")), "Otavio espera o Renome 2");
            s.Buy(Upgrade.AnvilsLv2); s.Buy(Upgrade.Counter6);
            s.Renome.Ganha(20);
            Assert.IsTrue(s.Equipe.AVenda(K("wk_bruna")));
            Assert.IsTrue(s.Equipe.AVenda(K("wk_celeste")));
            Assert.IsTrue(s.Equipe.AVenda(K("wk_baltazar")));
            Assert.IsFalse(s.Equipe.AVenda(K("wk_otavio")));
            Assert.IsFalse(s.Equipe.AVenda(K("wk_tome")), "Tome: Escudos II ainda nao");
            s.Renome.Ganha(50);
            Assert.IsTrue(s.Equipe.AVenda(K("wk_otavio")));
            Assert.AreEqual(K("wk_ajudante1"), s.Equipe.MaisBarato(), "a mais barata que o ouro paga: o nivel 2 do Ajudante (250; empate = o 1o da tabela)");
        }

        // ------------------------------------------------------------------ regras de cada papel

        [Test]
        public void Carregador_PassoMaisSeisPorNivel_CargaNosNiveisTresECinco()
        {
            Sim s = Aberta();
            Carrier w = s.Workers.Find(c => c.Wk == K("wk_ajudante1"));
            Assert.AreEqual((Balance.WorkerSpeed, Balance.WorkerCap), (w.Speed, w.Cap));
            Compra(s, "wk_ajudante1");
            Assert.AreEqual(Balance.WorkerSpeed * 1.06f, w.Speed, 1e-6, "nivel 2: +6%");
            Assert.AreEqual(Balance.WorkerCap, w.Cap);
            Compra(s, "wk_ajudante1");
            Assert.AreEqual(Balance.WorkerCap + 1, w.Cap, "nivel 3: carga +1");
            Compra(s, "wk_ajudante1", 2);
            Assert.AreEqual(Balance.WorkerSpeed * 1.24f, w.Speed, 1e-5, "nivel 5: +24%");
            Assert.AreEqual(Balance.WorkerCap + 2, w.Cap, "nivel 5: carga +2");
            s.Buy(Upgrade.HelperSpeed);
            Assert.AreEqual(Balance.WorkerSpeedUp * 1.24f, w.Speed, 1e-5, "por cima do Ajudantes ageis");
            Assert.AreEqual(Balance.WorkerCapUp + 2, w.Cap);
            Carrier w2 = s.Workers.Find(c => c.Wk == K("wk_ajudante2"));
            Assert.AreEqual((Balance.WorkerSpeedUp, Balance.WorkerCapUp), (w2.Speed, w2.Cap), "o nivel e' de cada ajudante");
            Assert.LessOrEqual(w.Cap, Balance.WorkerCapUp + WorkerDefs.CargaMax, "a View pre-aloca a pilha por isto");
        }

        [Test]
        public void Oficial_QualidadeEVelocidade_SoNaFamiliaDele()
        {
            Sim s = Aberta(Upgrade.AnvilsLv2);
            float anvil = s.AnvilA.Time, shield = s.ShieldBench.Time;
            Compra(s, "wk_bruna");
            Assert.AreEqual(15, s.QualityBreakdown(s.AnvilA)[(int)QualitySource.Worker]);
            Assert.AreEqual(15, s.QualityBreakdown(s.AnvilB)[(int)QualitySource.Worker], "as duas bigornas");
            Assert.AreEqual(0, s.QualityBreakdown(s.ShieldBench)[(int)QualitySource.Worker]);
            Assert.AreEqual(15 + 15, s.QualityPoints(s.AnvilA), "nivel II (15) + oficial (15): o QualityBreakdown explica tudo");
            Assert.AreEqual(anvil / 1.05f, s.AnvilA.Time, 1e-6, "velocidade da familia +5%");
            Assert.AreEqual(shield, s.ShieldBench.Time);
            Compra(s, "wk_bruna", 3);
            Assert.AreEqual(15, s.QualityBreakdown(s.AnvilA)[(int)QualitySource.Worker], "nivel 4: ainda +15");
            Compra(s, "wk_bruna");
            Assert.AreEqual(25, s.QualityBreakdown(s.AnvilA)[(int)QualitySource.Worker], "nivel 5: +25");
            Assert.AreEqual(anvil / 1.17f, s.AnvilA.Time, 1e-5, "+5% e +3% por nivel = +17%");
            Assert.AreEqual(0, s.QualityBreakdown(s.FurnaceA).Sum(), "a fornalha nao tem qualidade");
        }

        [Test]
        public void MestreDeObras_RenomeDois_BuffEmTodaBancada()
        {
            Sim s = Aberta();
            s.Renome.Ganha(70);
            float furnace = s.FurnaceA.Time, tool = s.ToolBench.Time;
            Compra(s, "wk_otavio");
            foreach (Station st in s.Stations.Where(x => x.Unlocked && x.Kind == Kind.Crafter))
                Assert.AreEqual(10, s.QualityBreakdown(st)[(int)QualitySource.Buff], st.Name);
            Assert.AreEqual(furnace, s.FurnaceA.Time, "a fornalha nao e' bancada");
            Assert.AreEqual(tool / 1.05f, s.ToolBench.Time, 1e-6);
            Compra(s, "wk_otavio", 4);
            Assert.AreEqual(tool / 1.13f, s.ToolBench.Time, 1e-5, "nivel 5: +13%");
        }

        [Test]
        public void Balconista_PacienciaEAtendimento_SoNoBalcaoPrincipal()
        {
            Sim s = Aberta(Upgrade.CounterCapacity, Upgrade.Counter5, Upgrade.Counter6);
            Compra(s, "wk_celeste");
            Assert.AreEqual(1.1f, s.Equipe.Paciencia, 1e-6);
            Assert.AreEqual(0.32f, s.Equipe.Atendimento);
            s.Queue.Clear();
            for (float t = 0f; t < 30f && s.Queue.Count == 0; t += Dt) s.Tick(Dt, 0f, 0f);
            Client c = s.Queue[0];
            Assert.AreEqual(s.T.PatienceFor(c.Want, s.RecipeDefs) * 1.1f, c.MaxPatience, 1e-4, "quem chega ao balcao: +10%");
            Compra(s, "wk_celeste", 4);
            Assert.AreEqual(1.3f, s.Equipe.Paciencia, 1e-6, "nivel 5: +30%");

            int Vendas(bool celeste)
            {
                Sim v = Aberta(Upgrade.CounterCapacity, Upgrade.Counter5, Upgrade.Counter6);
                if (celeste) Compra(v, "wk_celeste");
                v.Queue.Clear();
                for (int i = 0; i < 8; i++) v.Queue.Add(new Client { Want = Item.Sword, Patience = 999f, MaxPatience = 999f });
                v.Stock[(int)Item.Sword] = 10;
                int n = 0;
                for (int i = 0; i < 200; i++) { v.Tick(0.01f, 0f, 0f); n += v.Events.Count(e => e.Kind == Ev.Sold && e.A == (int)Item.Sword); }
                return n;
            }
            int sem = Vendas(false), com = Vendas(true);
            TestContext.WriteLine($"vendas de espada em 2 s: sem balconista {sem}, com {com}");
            Assert.That(sem, Is.InRange(5, 6), "0,40 s entre vendas");
            Assert.That(com, Is.InRange(7, 8), "0,32 s entre vendas");
        }

        [Test]
        public void Almoxarife_RenomeUm_SaidasMaioresEEstoqueAcimaDoNivelUm()
        {
            Sim s = Aberta(Upgrade.CounterCapacity);
            s.Renome.Ganha(20);
            Compra(s, "wk_baltazar");
            Assert.AreEqual((6, 5, 5), (s.FurnaceA.OutCap, s.AnvilA.OutCap, s.ToolBench.OutCap), "fornalha 4 -> 6, bancadas 3 -> 5");
            Assert.AreEqual(s.T.CounterCap1, s.CounterCap, "nivel 1: o estoque nao muda");
            Compra(s, "wk_baltazar");
            Assert.AreEqual(s.T.CounterCap1 + 1, s.CounterCap);
            Compra(s, "wk_baltazar", 3);
            Assert.AreEqual(s.T.CounterCap1 + 4, s.CounterCap, "nivel 5: +4 por produto (o +2 no 3 e +4 no 5 do §2.3)");
        }

        // ------------------------------------------------------------------ save

        [Test]
        public void Save_IdaEVolta_NiveisEEfeitosIguais()
        {
            Sim s = Aberta(Upgrade.AnvilsLv2, Upgrade.CounterCapacity, Upgrade.Counter5, Upgrade.Counter6);
            s.Renome.Ganha(70);
            Compra(s, "wk_ajudante1", 2); Compra(s, "wk_bruna", 3); Compra(s, "wk_celeste"); Compra(s, "wk_baltazar", 5); Compra(s, "wk_otavio", 2);
            s.FurnaceA.Out = 6; s.AnvilA.Out = 5;   // pilhas no teto do almoxarife
            string text = s.Save(1);
            StringAssert.Contains("wkn=wk_ajudante1:3,wk_bruna:3,wk_celeste:1,wk_baltazar:5,wk_otavio:2\n", text);
            Sim b = Sim.Load(text);
            for (int k = 0; k < WorkerDefs.All.Length; k++) Assert.AreEqual(s.Equipe.Nivel(k), b.Equipe.Nivel(k), WorkerDefs.All[k].Id);
            Assert.AreEqual((6, 5), (b.FurnaceA.Out, b.AnvilA.Out), "as pilhas no teto do almoxarife voltam inteiras (Equipe le antes do stN=)");
            Assert.AreEqual(s.CounterCap, b.CounterCap);
            Assert.AreEqual(s.AnvilA.Time, b.AnvilA.Time);
            Assert.AreEqual(s.QualityPoints(s.AnvilA), b.QualityPoints(b.AnvilA));
            Assert.AreEqual(s.Workers.Find(w => w.Wk == 0).Speed, b.Workers.Find(w => w.Wk == 0).Speed);
            Assert.AreEqual(text, b.Save(1), "regravar nao muda nada");
        }

        [Test]
        public void Save_Antigo_SemWkn_NinguemContratadoEAjudantesNoNivelUm()
        {
            var s = new Sim();
            for (int u = 0; u <= (int)Upgrade.Helper3; u++) s.Buy((Upgrade)u);
            s.Workers[0].Held[(int)Item.Ore] = 2; s.Workers[2].Held[(int)Item.Shield] = 1;
            string v3 = s.Save(1);
            StringAssert.DoesNotContain("wkn=", v3, "ninguem com nivel: o texto do save e' o de antes do B-CORE-04");
            Sim b = Sim.Load(v3);
            Assert.AreEqual(3, b.Workers.Count);
            for (int k = 0; k < 3; k++) CollectionAssert.AreEqual(s.Workers[k].Held, b.Workers[k].Held, $"ajudante {k}");
            Assert.IsTrue(b.Workers.TrueForAll(w => b.Equipe.Nivel(w.Wk) == 1 && w.Speed == Balance.WorkerSpeed && w.Cap == Balance.WorkerCap));
            Assert.AreEqual(0, b.Equipe.Contratados());
            // schema 2 (wk= por posicao, a 0.8) e o da 0.5.0 (sem papel): o V2ParaV3 de sempre, os ajudantes com o WorkerDef certo
            string v2 = string.Join("\n", v3.Split('\n').Where(l => !l.StartsWith("holds=") && !l.StartsWith("wks=") && !l.StartsWith("stocks=") && !l.StartsWith("solds=")))
                .Replace("v=3", "v=2");
            Sim c = Sim.Load(v2);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, c.Workers.Select(w => w.Wk).ToArray());
            for (int k = 0; k < 3; k++) CollectionAssert.AreEqual(s.Workers[k].Held, c.Workers[k].Held, $"schema 2: ajudante {k}");
        }

        [Test]
        public void Save_LixoNoWkn_CortaNoMaximo_IdDesconhecidoConta()
        {
            Sim s = Aberta();
            string text = s.Save(1) + "wkn=wk_bruna:99,wk_sumiu:2,wk_tome:-3,wk_ajudante1:x,lixo\n";
            Sim b = Sim.Load(text);
            Assert.AreEqual(5, b.Equipe.Nivel(K("wk_bruna")), "acima do maximo = o maximo");
            Assert.AreEqual(0, b.Equipe.Nivel(K("wk_tome")), "negativo = nao contratado");
            Assert.AreEqual(1, b.Equipe.Nivel(K("wk_ajudante1")), "lixo = nivel 1 do ajudante");
            Assert.AreEqual(2, b.UnknownIds, "wk_sumiu e lixo: conteudo de um build mais novo");
        }

        // ------------------------------------------------------------------ metas (D-CORE-02) e eventos

        [Test]
        public void Eventos_EMetas_ContratacaoPorPapelENivelAlcancado()
        {
            Sim s = Aberta(Upgrade.AnvilsLv2);
            long sink = s.Metas.Cont.TryGetValue("currency_sink", out long v0) ? v0 : 0;
            Compra(s, "wk_bruna");
            Assert.IsTrue(s.Events.Exists(e => e.Kind == Ev.Hired && e.A == K("wk_bruna") && e.B == (int)WorkerRole.Smith));
            Assert.AreEqual(1L, s.Metas.Cont["worker_hired." + (int)WorkerRole.Smith]);
            Compra(s, "wk_bruna", 4);
            Assert.IsTrue(s.Events.Exists(e => e.Kind == Ev.WorkerUp && e.A == K("wk_bruna") && e.B == 5));
            Assert.AreEqual(4L, s.Metas.Cont["worker_upgrade"]);
            Assert.AreEqual(1L, s.Metas.Cont["worker_upgrade.5"]);
            Assert.AreEqual(1200L + 300 + 480 + 770 + 1230, s.Metas.Cont["currency_sink"] - sink, "contratacao e niveis sao ralo de ouro");
            s.Metas.Agora(20000L * 86400 + 12 * 3600, 0);
            Assert.IsTrue(s.Metas.Conquista(MetasDefs.Achar("ach_oficial1")).Pronta);
            Assert.IsTrue(s.Metas.Conquista(MetasDefs.Achar("ach_worker_n5")).Pronta);
            Assert.IsFalse(s.Metas.Conquista(MetasDefs.Achar("ach_workers13")).Pronta);
            Assert.AreEqual(13L, MetasDefs.Achar("ach_workers13_n5").Alvo);
            // retroativo de um save sem metas: os contadores saem do Sim
            Sim r = Sim.Load(string.Join("\n", s.Save(1).Split('\n').Where(l => !l.StartsWith("mt."))));
            r.Metas.Agora(20000L * 86400 + 12 * 3600, 0);
            Assert.AreEqual(1L, r.Metas.Cont["worker_hired." + (int)WorkerRole.Smith]);
            Assert.AreEqual(1L, r.Metas.Cont["worker_upgrade.5"]);
        }

        // ------------------------------------------------------------------ validator

        static List<string> Valida(params WorkerDef[] extra) =>
            ContentValidator.Workers(WorkerDefs.All.Concat(extra).ToArray(), ContentValidator.World(), RenomeDefs.Niveis).ToList();

        static void Acusa(List<string> erros, string trecho) => Assert.IsTrue(erros.Exists(e => e.Contains(trecho)), $"esperava \"{trecho}\" em: {string.Join(" | ", erros)}");

        static WorkerDef Novo(string id, WorkerRole papel = WorkerRole.Clerk, string distrito = WorkerDefs.OldForge, string station = null, string requer = null,
            long contratacao = 1000, long[] niveis = null, string contrata = null, int rota = -1, string[] tracos = null) =>
            new WorkerDef(id, "Teste", papel, Specialty.None, distrito, station, contrata, rota, contratacao, requer, niveis ?? new long[] { 100, 200 }, "oficial",
                tracos ?? new[] { "a" });

        [Test]
        public void Validator_DefeitosInjetados_Acusa()
        {
            Acusa(Valida(Novo("wk_bruna")), "ID repetido");
            Acusa(Valida(Novo("bruna2")), "fora do formato wk_");
            Acusa(Valida(Novo("wk_tres", tracos: new[] { "a", "b", "c" })), "3 tracos");
            Acusa(Valida(Novo("wk_caro", niveis: new long[] { 300, 300 })), "nao sobe");
            Acusa(Valida(Novo("wk_quebrado", niveis: new long[] { 102 })), "multiplo de 5");
            Acusa(Valida(Novo("wk_vulcana", WorkerRole.Smith, "fundicao_fantasma", "fornalha_liga", "fornalha_liga")), "distrito \"fundicao_fantasma\"");
            Acusa(Valida(Novo("wk_vulcana", WorkerRole.Smith, "fundicao_fantasma", "fornalha_liga", "fornalha_liga")), "familia \"fornalha_liga\", sem bancada");
            Acusa(Valida(Novo("wk_vulcana", WorkerRole.Smith, "fundicao_fantasma", "fornalha_liga", "fornalha_liga")), "requisito \"fornalha_liga\"");
            Acusa(Valida(Novo("wk_felix", WorkerRole.Courier)), "sem regra no nucleo");
            Acusa(Valida(Novo("wk_bruna2", WorkerRole.Smith, station: "bigorna")), "segundo oficial da familia \"bigorna\"");
            Acusa(Valida(Novo("wk_pipo2", WorkerRole.Porter, contratacao: 0, contrata: "ajudante1", rota: 0)), "ja contrata outro worker");
            Acusa(Valida(Novo("wk_sem", WorkerRole.Porter, contratacao: 0, contrata: "nenhum", rota: 0)), "sem upgrade que contrata");
            Acusa(Valida(Novo("wk_gratis", contratacao: 0)), "contratacao 0");
            var renome = RenomeDefs.Niveis.Select(d => d.Nivel == 3 ? new RenomeDef(3, d.Limiar, d.Libera.Concat(new[] { "wk_fantasma" }).ToArray()) : d).ToArray();
            Acusa(ContentValidator.Workers(WorkerDefs.All, ContentValidator.World(), renome).ToList(), "\"wk_fantasma\", worker que o build nao tem");
            Acusa(ContentValidator.Validate(workers: WorkerDefs.All.Concat(new[] { Novo("wk_bruna") }).ToArray()), "ID repetido");
        }

        // ------------------------------------------------------------------ textos do cartao (pt, en, es)

        [Test]
        public void Cartao_TextosCabemNosTresIdiomas_EmTodoNivel()
        {
            Sim s = Aberta();   // todo worker da Equipe travado: o "Precisa: ..." de cada um
            try
            {
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    for (int k = 0; k < WorkerDefs.All.Length; k++)
                    {
                        Sim t = Aberta(Upgrade.AnvilsLv2, Upgrade.ShieldsLv2, Upgrade.ToolsLv2, Upgrade.CounterCapacity, Upgrade.Counter5, Upgrade.Counter6,
                            Upgrade.Helper3, Upgrade.SideCorridor, Upgrade.Jewelry, Upgrade.Jeweler, Upgrade.Miner, Upgrade.Jeweler2, Upgrade.JewelsLv2);
                        WorkerDef d = WorkerDefs.All[k];
                        if (d.Papel != WorkerRole.Porter)
                        {
                            Assert.AreEqual(Cartao.Travado, Textos.EstadoWorker(s, k), d.Id);
                            Assert.LessOrEqual(Textos.PrecisaWorker(s, k).Length, 18, $"{i}: {d.Id} {Textos.PrecisaWorker(s, k)}");
                            StringAssert.DoesNotContain("-1", Textos.PrecisaWorker(s, k), d.Id);
                        }
                        t.Renome.Ganha(70);
                        for (int n = t.Equipe.Nivel(k); n < d.MaxNivel; n++)
                        {
                            foreach (string x in new[] { Textos.BeneficioWorker(t, k), Textos.AntesDepoisWorker(t, k), Textos.AvisoWorker(d, n + 1) })
                                StringAssert.DoesNotContain("[", x, $"{i}: {d.Id}");
                            Assert.LessOrEqual(Textos.BeneficioWorker(t, k).Length, 2 * 14, $"{i}: {d.Id} {Textos.BeneficioWorker(t, k)}");
                            Assert.LessOrEqual(Textos.AntesDepoisWorker(t, k).Length, 16, $"{i}: {d.Id} {Textos.AntesDepoisWorker(t, k)}");
                            Assert.LessOrEqual(Textos.AvisoWorker(d, n + 1).Length, Textos.AvisoMax, $"{i}: {d.Id}");
                            Assert.LessOrEqual(d.Nome.Length, ContentValidator.NameMax);
                            Compra(t, d.Id);
                        }
                        Assert.AreEqual(Cartao.Comprado, Textos.EstadoWorker(t, k), "nivel maximo: o cartao some");
                    }
                }
                Loc.Atual = Idioma.Pt;
                Sim p = Aberta(Upgrade.AnvilsLv2);
                p.Gold = 5000;
                Assert.AreEqual("Qualidade +0,15 · vel. +5%", Textos.BeneficioWorker(p, K("wk_bruna")));
                Assert.AreEqual("Bigorna", Textos.AntesDepoisWorker(p, K("wk_bruna")));
                Assert.AreEqual("Precisa: Nível II", Textos.PrecisaWorker(p, K("wk_tome")));
                Assert.AreEqual("Precisa: Renome 2", Textos.PrecisaWorker(p, K("wk_otavio")));
                Assert.AreEqual("Velocidade +6%", Textos.BeneficioWorker(p, K("wk_ajudante1")));
                Assert.AreEqual("Nível 1 → 2", Textos.AntesDepoisWorker(p, K("wk_ajudante1")));
                Assert.AreEqual(Cartao.Disponivel, Textos.EstadoWorker(p, K("wk_bruna")));
                Assert.AreEqual(Cartao.Travado, Textos.EstadoWorker(p, K("wk_otavio")));
                Assert.AreEqual(Cartao.Oculto, Textos.EstadoWorker(p, K("wk_mineiro")), "ajudante sem o upgrade: o cartao nao aparece");
                Assert.AreEqual(Cartao.Oculto, Textos.EstadoWorker(new Sim(), K("wk_ajudante1")), "antes das Ferramentas");
                Assert.AreEqual("Bruna: nível 2!", Textos.AvisoWorker(WorkerDefs.All[K("wk_bruna")], 2));
            }
            finally { Loc.Atual = Idioma.Pt; }
        }

        [Test]
        public void Captura_Equipe_OsTrezeNoPosto_SemMexerNoOuro()
        {
            var s = new Sim();
            s.Gold = 77;
            Captura.Equipe(s);
            Assert.AreEqual(77L, s.Gold, "o -gold da foto vem depois");
            Assert.AreEqual(7, s.Equipe.Contratados(), "oficiais, balconista, almoxarife e mestre de obras");
            Assert.AreEqual(Metas.AjudantesOldForge, s.Workers.Count);
            Assert.IsTrue(s.Workers.TrueForAll(w => s.Equipe.Nivel(w.Wk) == 3 && w.Cap == Balance.WorkerCapUp + 1), "ajudantes no nivel 3, carga +1");
        }

        // ------------------------------------------------------------------ bot e economia (BALANCE.md §25)

        static (Sim s, string log) Joga(Tuning t, float minutos, float workerSeg = 0f)
        {
            var s = new Sim(t);
            var bot = new Bot { WorkerSegundos = workerSeg };
            return (s, BalanceTests.Play(s, minutos * 60f, bot));
        }

        [Test]
        public void Bot_SemOsWorkersNovos_IdenticoAoDeAntes_E_ComEles_SoDepoisDaProducao()
        {
            // "antes" = flag desligada (nada da Equipe a venda). Com a flag ligada e o bot que nunca compra worker: o mesmo jogo, tick a tick
            (Sim off, string logOff) = Joga(new Tuning { Workers = false }, 55f);   // B-CORE-07: a bigorna longa (6.000, ~45 min) leva a producao completa a ~49:55; 55 min deixa o 1o worker chegar
            (Sim never, string logNever) = Joga(new Tuning(), 55f, -1f);
            Assert.AreEqual(logOff, logNever, "ganchos neutros sem ninguem novo");
            Assert.AreEqual(off.Save(1), never.Save(1));
            // o bot padrao (WorkerSegundos 0): a curva produtiva e' a mesma; o 1o worker so depois da producao completa
            (Sim on, string logOn) = Joga(new Tuning(), 55f);
            Assert.IsTrue(off.ProductionComplete, "premissa: producao completa em 55 min");
            float prod = Enumerable.Range(0, Upgrades.Count).Where(Upgrades.IsProduction).Max(u => off.UpgradeTime[u]);
            for (int u = 0; u < Upgrades.Count; u++)
                if (off.UpgradeTime[u] <= prod) Assert.AreEqual(off.UpgradeTime[u], on.UpgradeTime[u], "curva produtiva e niveis de estacao: " + (Upgrade)u);
            string[] a = logOff.Split('\n'), b = logOn.Split('\n');
            for (int m = 0; m < (int)(prod / 60f); m++) Assert.AreEqual(a[m], b[m], $"minuto {m + 1}, antes da producao completa");
            Assert.Greater(on.Equipe.Gasto, 0L, "depois da producao completa o bot compra worker");
        }

        [Test]
        public void Bot_90Min_WorkersComoRalo_MaisRendaEOuroComDestino()
        {
            (Sim off, string _) = Joga(new Tuning { Workers = false }, 90f);
            var s = new Sim();
            var bot = new Bot();
            var earned = new long[91]; var gold = new long[91];
            int next = 1;
            for (float t = 0f; t < 90f * 60f; t += Dt)
            {
                bot.Step(s, Dt);
                if (s.Time < next * 60f || next > 90) continue;
                earned[next] = s.GoldEarned; gold[next] = s.Gold;
                next++;
            }
            float renda = (earned[90] - earned[80]) / 10f;
            TestContext.WriteLine($"90 min: contratados {s.Equipe.Contratados()}, gasto {s.Equipe.Gasto}, renda 80-90 {renda:0}/min, saldo aos 60 {gold[60]}, " +
                $"receita {s.GoldEarned} (sem workers {off.GoldEarned})");
            for (int k = 0; k < WorkerDefs.All.Length; k++) TestContext.WriteLine($"  {WorkerDefs.All[k].Id}: nivel {s.Equipe.Nivel(k)}");
            Assert.GreaterOrEqual(s.Equipe.Contratados(), 5, "oficiais, balconista e almoxarife (o mestre de obras pede Renome 2)");
            Assert.Greater(s.GoldEarned, off.GoldEarned, "os workers pagam o que custam dentro dos 90 min");
            Assert.LessOrEqual(gold[60], 10000L, "ECONOMIA_WAVE_B §10: ouro sem destino aos 60 min <= 10 mil");
            Assert.LessOrEqual(100.0 * s.QualityGold / s.GoldEarned, 35.0, "premio de qualidade <= 35%");
        }
    }
}
