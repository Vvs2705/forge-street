using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// C-CORE-08 (CONTEUDO_V1 §2.5, §5.5, §7, §8, §10, §14; BALANCE.md §40): a Arcane Foundry e o medidor de carga dos altares. E cai 3/s
    /// (condensadores +2, Isis +1 a +3); Estavel so comeca com E + carga <= 100 e nunca sobrecarrega; Forcado soma 2 x carga, energiza (q +20) e,
    /// passando de 100, termina a peca e pausa 10 s (E = 40) sem perder nada; sem sorteio; o offline nunca mexe no medidor; o save vai e volta.
    /// O Charter pelo caminho real (4 obras e 2 contratos OU 20 Royal). Nunca fixa indice nem posicao do distrito no mapa (Distritos.Find).
    /// NUnit 3.5 do Unity: tipos exatos nas tuplas (long com L, float com f).
    /// </summary>
    public class ArcaneTests
    {
        const float Dt = 1f / 30f, Q = 0.25f;   // Q: passo exato em binario (o dreno de 3/s anda 75 centesimos por passo)
        static DistrictDef AF => Distritos.Find(Distritos.ArcaneFoundryId);

        static Sim Novo() => new Sim(distrito: AF);
        static int U(Sim s, string id) => Upgrades.IndexOf(id, s.UpgradeDefs);
        static int I(Sim s, string id) => Items.IndexOf(id, s.ItemDefs);
        static void Compra(Sim s, params string[] ids) { foreach (string id in ids) Assert.IsTrue(s.Buy((Upgrade)U(s, id)), id); }
        static void Producao(Sim s) { for (int u = 0; u < s.UpgradeDefs.Length; u++) if (s.IsProduction(u)) s.Buy((Upgrade)u); }
        static void Enche(Station st, int vezes = 1) { for (int k = 0; k < st.Ins.Length; k++) st.Ins[k] = st.Recipe.Inputs[k].Qty * vezes; }

        /// <summary>Uma peca do altar do comeco ao fim (a entrada com 1 receita, a saida vazia); devolve o nivel dela.</summary>
        static Quality Peca(Sim s, Station st)
        {
            st.Out = 0; Array.Clear(st.OutQ, 0, st.OutQ.Length);
            Enche(st);
            int feitas = st.Crafted;
            for (int i = 0; i < 4000 && st.Crafted == feitas; i++) s.Tick(Q, 0f, 0f);
            Assert.AreEqual(feitas + 1, st.Crafted, "o altar fez a peca");
            return st.OutQ[(int)Quality.Superior] > 0 ? Quality.Superior : st.OutQ[(int)Quality.Fine] > 0 ? Quality.Fine : Quality.Normal;
        }

        /// <summary>Roda ate o altar passar de `feitas` pecas (no maximo 1.000 s de jogo).</summary>
        static void Termina(Sim s, Station st, int feitas)
        {
            for (int i = 0; i < 4000 && st.Crafted == feitas; i++) s.Tick(Q, 0f, 0f);
            Assert.Greater(st.Crafted, feitas, "a peca terminou");
        }

        static int Isis(Sim s, int nivel)
        {
            int k = WorkerDefs.IndexOf("wk_isis", s.Equipe.Defs);
            while (s.Equipe.Nivel(k) < nivel) { s.Gold = s.Equipe.Custo(k); Assert.IsTrue(s.Equipe.Comprar(k)); }
            return k;
        }

        // ------------------------------------------------------------------ dados

        /// <summary>O kit do §2.5 (deposito, cristal bruto, fornalha, cuba, altar de lanternas e balcao), os numeros do Ajuste (BALANCE.md §40), a
        /// carga por peca (§2.5 regra 5), a abertura, o Charter (Renome 8, 4 obras, 2 contratos), o encadeamento do mapa e as familias do §12.</summary>
        [Test]
        public void Kit_OsNumerosEOEncadeamento()
        {
            Sim s = Novo();
            CollectionAssert.AreEquivalent(new[] { "deposito", "deposito_cristal", "fornalha", "cuba_cristal", "altar_lanternas", "balcao" },
                s.Stations.Where(x => x.Unlocked).Select(x => x.Id).ToArray());
            Assert.AreEqual((4, 10), (s.QueueCap, s.CounterCap));
            Assert.AreEqual((3900, 8580), (s.PriceOf((Item)I(s, "lanterna_arcana")), s.PriceOf((Item)I(s, "liga_celeste"))));
            Assert.AreEqual((13000L, 1100000L, 1900000L), (s.T.Cost(U(s, "fole_arcano")), s.T.Cost(U(s, "cuba3")), s.T.Cost(U(s, "nivel_altar_runa5"))));
            Assert.AreEqual(18, Enumerable.Range(0, s.UpgradeDefs.Length).Count(s.IsProduction));
            CollectionAssert.AreEqual(new[] { 15, 12, 18, 20, 25, 22, 30 },
                new[] { "lanterna_arcana", "anel_cristal", "amuleto_arcano", "lamina_runica", "escudo_runico", "cajado_liga", "liga_celeste" }.Select(id => s.T.CargaArcana[I(s, id)]).ToArray());
            Assert.AreEqual((3900000L, 3000L, 4, 2), (AF.Abertura, AF.Charter.Renome, AF.Charter.Masterworks, AF.Charter.Contratos));
            Assert.AreEqual(AF.Id, Distritos.RoyalArsenal.Charter.Proximo, "o Charter do Arsenal libera a Foundry");
            Assert.AreEqual(Array.IndexOf(Distritos.All, AF), AF.Indice, "indice = posicao no mapa");
            CollectionAssert.AreEqual(new[] { "viajante", "aventureiro", "nobre", "colecionador" }, AF.Clientes.Select(c => c.Id).ToArray(), "§12");
            ClientDef av = AF.Clientes[1], nb = AF.Clientes[2];
            Assert.AreEqual((1, 0.9f, 1f, false), (av.Unidades, av.Paciencia, av.Intervalo, av.Rico), "aventureiro");
            CollectionAssert.AreEqual(new[] { "lamina_runica", "escudo_runico", "anel_cristal" }, av.Pede);
            Assert.AreEqual((1.8f, 2.5f, true), (nb.Paciencia, nb.Intervalo, nb.Rico), "nobre");
            CollectionAssert.AreEqual(new[] { "amuleto_arcano", "liga_celeste", "cajado_liga" }, nb.Pede);
            Assert.AreEqual("wk_sibila", Imperio.ManagerDe(AF.Id).Id);
            Assert.IsTrue(AF.InsumoPorEspera, "o carregador de insumo leva o que os altares esperam (BALANCE.md §40)");
            Assert.IsTrue(s.TemArcano);
            CollectionAssert.AreEquivalent(new[] { "altar_lanternas", "altar_aneis", "altar_amuletos", "cadinho_celeste", "altar_laminas", "altar_escudos", "altar_cajados" },
                s.Stations.Where(s.Arcana).Select(x => x.Id).ToArray(), "os 7 altares (e so eles) tem medidor");
            CollectionAssert.IsEmpty(ContentValidator.Distrito(AF).ToList(), "o validator do distrito passa");
        }

        /// <summary>Os outros distritos nao tem altar: o Tuning padrao sem carga, nenhuma estacao com medidor e o save sem as linhas arc.</summary>
        [Test]
        public void OutrosDistritos_SemAltar()
        {
            Assert.IsNull(Tuning.Padrao.CargaArcana);
            foreach (DistrictDef d in Distritos.All.Where(x => x != AF))
            {
                Sim s = ContentValidator.Mundo(d);
                Assert.IsFalse(s.TemArcano || s.Stations.Exists(s.Arcana), d.Id);
                Assert.IsFalse(s.Save(100).Split('\n').Any(l => l.StartsWith("arc", StringComparison.Ordinal)), d.Id);
            }
        }

        // ------------------------------------------------------------------ o medidor

        /// <summary>Regra 2: E cai `dreno` por s, continuo: base 3/s, condensador +2, condensador2 +2, Isis +1 no nivel 1 e +0,5 nos niveis 3, 5, 7 e
        /// 9 (maximo +3); nunca abaixo de 0.</summary>
        [Test]
        public void Dreno_BaseCondensadoresEIsis()
        {
            Sim s = Novo();
            Station l = s.Est("altar_lanternas");
            l.Medidor = 5000;
            for (int i = 0; i < 4; i++) s.Tick(Q, 0f, 0f);
            Assert.AreEqual((300, 4700), (s.Dreno, l.Medidor), "3/s");
            Compra(s, "condensador");
            Assert.AreEqual(500, s.Dreno);
            Compra(s, "condensador2", "veio_runico", "altar_laminas");
            Assert.AreEqual(700, s.Dreno);
            int k = Isis(s, 1);
            Assert.AreEqual(800, s.Dreno, "Isis no nivel 1: +1");
            foreach ((int nivel, int d) in new[] { (2, 800), (3, 850), (4, 850), (5, 900), (7, 950), (9, 1000), (10, 1000) })
            {
                Isis(s, nivel);
                Assert.AreEqual(d, s.Dreno, $"Isis no nivel {nivel}");
            }
            Assert.AreEqual(10, s.Equipe.Nivel(k));
            l.Medidor = 150;
            s.Tick(Q, 0f, 0f);
            Assert.AreEqual(0, l.Medidor, "nunca abaixo de 0");
        }

        /// <summary>§2.5 regra 5: no kit, o Estavel e o tempo da lanterna empatam (5 s, dreno 3, carga 15): alimentado sem parar, o altar nunca
        /// espera o medidor e faz 12 lanternas em 60 s.</summary>
        [Test]
        public void Kit_EstavelEmpataComALanterna()
        {
            Sim s = Novo();
            Station l = s.Est("altar_lanternas");
            Assert.AreEqual(5f, l.Time, 1e-4f);
            int esperou = 0;
            for (int i = 0; i < 60 * 4; i++)
            {
                Enche(l, 2); l.Out = 0;
                s.Tick(Q, 0f, 0f);
                if (!l.Busy && l.Pausa <= 0f) esperou++;
                Assert.LessOrEqual(l.Medidor, s.T.ArcanoTeto);
            }
            Assert.AreEqual(0, esperou);
            Assert.GreaterOrEqual(l.Crafted, 11, "uma lanterna a cada 5 s");
        }

        /// <summary>CONTEUDO_V1 §14 (negativo) e regra 3: o Estavel so comeca com E + carga <= 100 (senao espera, "estabilizando") e nunca
        /// sobrecarrega: 10 min alimentado, com o medidor alto no comeco, sem nenhuma sobrecarga, pausa ou peca energizada.</summary>
        [Test]
        public void Estavel_SoComecaComEspaco_ENuncaSobrecarrega()
        {
            Sim s = Novo();
            Station l = s.Est("altar_lanternas");
            l.Medidor = 9000;
            Enche(l, 2);
            int espera = 0;
            for (; !l.Busy; espera++)
            {
                Assert.Less(espera, 400, "o medidor cai (3/s: 100 s no maximo)");
                bool cabe = l.Medidor + s.CargaPeca(l) <= s.T.ArcanoTeto;
                s.Tick(Q, 0f, 0f);
                Assert.AreEqual(cabe, l.Busy, $"comeca so com espaco (E {l.Medidor})");
            }
            Assert.Greater(espera, 1, "esperou o medidor cair");
            Assert.LessOrEqual(l.Medidor, s.T.ArcanoTeto);
            Assert.IsFalse(l.Energizada || l.Sobrecarrega);
            Compra(s, "foco_cristal", "nivel_altar_cristal2", "nivel_altar_cristal3");   // altar mais rapido que o dreno: o Estavel segura
            for (int i = 0; i < 600 * 4; i++)
            {
                Enche(l, 2); l.Out = 0;
                s.Tick(Q, 0f, 0f);
                Assert.IsTrue(l.Medidor <= s.T.ArcanoTeto && l.Pausa <= 0f, $"passo {i}: E {l.Medidor}");
            }
            Assert.AreEqual((0, 0, 0), (s.Sobrecargas, s.EnergizadasTotal, l.Sequencia));
            Assert.Less(l.Crafted, 600 / l.Time, "o dreno segurou o ritmo (3/s x carga 15 = 1 peca a cada 5 s)");
        }

        /// <summary>CONTEUDO_V1 §14 (negativo) e regra 4: o Forcado comeca com insumo, soma 2 x carga e energiza; passou de 100, a peca TERMINA (sai
        /// energizada) e o altar pausa 10 s, depois E = 40; o insumo que esperava fica na entrada e vira a proxima peca: nada se perde.</summary>
        [Test]
        public void Forcado_SobrecarregadoNaoPerdePeca()
        {
            Sim s = Novo();
            Station l = s.Est("altar_lanternas");
            int lanterna = I(s, "lanterna_arcana");
            Assert.IsTrue(s.ModoArcano(l, true));
            Assert.IsFalse(s.ModoArcano(l, true), "ja estava no Forcado");
            l.Medidor = 9000;
            Enche(l, 2);
            s.Tick(Q, 0f, 0f);
            Assert.AreEqual((true, true, true), (l.Busy, l.Energizada, l.Sobrecarrega), "comecou, energizada, vai sobrecarregar");
            Assert.AreEqual(9000 + 2 * 1500 - 75, l.Medidor, "2 x carga (e 1 passo de dreno)");
            Termina(s, l, 0);
            Assert.AreEqual((1, 1, 1, 0), (l.Out, s.PecasEnergizadas[lanterna], s.Sobrecargas, l.Sequencia), "a peca saiu");
            Assert.AreEqual(1, l.Ins[0], "o insumo da 2a peca segue na entrada");
            Assert.Greater(l.Pausa, 9f);
            for (int i = 0; i < 39; i++) { s.Tick(Q, 0f, 0f); Assert.IsFalse(l.Busy, "em sobrecarga nao comeca"); }
            Assert.AreEqual((0f, 4000), (l.Pausa, l.Medidor), "depois da pausa, E = 40");
            s.Tick(Q, 0f, 0f);
            Assert.AreEqual((true, false, 0, 7000 - 75), (l.Busy, l.Sobrecarrega, l.Ins[0], l.Medidor), "a 2a peca: 40 + 30, sem sobrecarga");
            Termina(s, l, 1);
            Assert.AreEqual((2, 2, 1), (l.Out, s.PecasEnergizadas[lanterna], l.Sequencia), "2 insumos, 2 pecas");
        }

        /// <summary>Regra 4 e 6: a peca energizada soma q +20 (QualitySource.Energizada) e conta em EnergizedPieces; StableStreak conta as
        /// energizadas seguidas sem sobrecarga, a peca Estavel nao conta nem zera e a sobrecarga zera; a troca de modo vale na proxima peca.</summary>
        [Test]
        public void Energizada_Q20_ESequencia()
        {
            Sim s = Novo();
            Station l = s.Est("altar_lanternas");
            int lanterna = I(s, "lanterna_arcana");
            Assert.AreEqual(0, s.QualityBreakdown(l)[(int)QualitySource.Energizada]);
            s.ModoArcano(l, true);
            Assert.AreEqual((20, 20), (s.QualityBreakdown(l)[(int)QualitySource.Energizada], s.QualityPoints(l)), "parado, mostra a proxima");
            var q = new List<Quality>();
            for (int n = 0; n < 5; n++) q.Add(Peca(s, l));
            CollectionAssert.AreEqual(new[] { Quality.Normal, Quality.Normal, Quality.Normal, Quality.Normal, Quality.Fine }, q, "q 20: 1 Fina a cada 5");
            Assert.AreEqual((5, 5), (s.PecasEnergizadas[lanterna], l.Sequencia));

            s.ModoArcano(l, false);
            Assert.AreEqual(Quality.Normal, Peca(s, l));
            Assert.AreEqual((5, 5), (s.PecasEnergizadas[lanterna], l.Sequencia), "a Estavel nao conta nem zera");

            Enche(l); l.Out = 0;
            s.Tick(Q, 0f, 0f);
            Assert.IsTrue(l.Busy && !l.Energizada);
            s.ModoArcano(l, true);   // no meio da peca Estavel: ela termina Estavel
            Assert.IsFalse(l.Energizada);
            Termina(s, l, 6);
            Assert.AreEqual((5, true), (s.PecasEnergizadas[lanterna], l.Energizada), "a de agora saiu Estavel; a proxima vem energizada");

            l.Medidor = 9000;
            Peca(s, l);
            Assert.AreEqual((6, 0, 1), (s.PecasEnergizadas[lanterna], l.Sequencia, s.Sobrecargas), "a sobrecarga zera a sequencia");

            Assert.IsFalse(s.Projetos.Tem("bp_var_aura"));
            s.PecasEnergizadas[I(s, "anel_cristal")] = 494;
            s.Tick(Q, 0f, 0f);
            Assert.IsTrue(s.Projetos.Tem("bp_var_aura"), "a aura das lanternas: 500 pecas energizadas (o marco)");
        }

        /// <summary>V7 (sem sorteio) e §0.4: duas partidas com a mesma entrada (o bot F2P, 12 min, com os altares) dao o mesmo save; o offline nunca
        /// mexe no medidor nem anda a pausa.</summary>
        [Test]
        public void SemSorteio_EOfflineNaoMexeNoMedidor()
        {
            string Joga()
            {
                Sim s = Novo();
                s.Gold = 2000000;
                Bot b = Bot.From(BotProfile.F2P);
                for (int i = 0; i < 12 * 60 * 30; i++) b.Step(s, Dt);
                Assert.Greater(s.EnergizadasTotal, 0, "o bot energizou");
                return s.Save(100);
            }
            Assert.AreEqual(Joga(), Joga());

            Sim o = Novo();
            Station l = o.Est("altar_lanternas");
            l.Medidor = 5000; l.Pausa = 5f;
            o.RateEma = 500;
            Assert.IsTrue(o.ApplyOffline(7200, 1) > 0, "o cofre pagou");
            Assert.AreEqual((5000, 5f), (l.Medidor, l.Pausa));
        }

        /// <summary>Linhas opcionais do save: a Foundry nova nao grava nada do medidor; medidor, modo, peca energizada e sobrecarga pendente (com o
        /// altar trabalhando), pausa, sequencia e EnergizedPieces vao e voltam; save sem as linhas = medidor vazio e Estavel; lixo = padrao.</summary>
        [Test]
        public void Save_IdaEVolta_ELinhasOpcionais()
        {
            static bool TemArc(string t) => t.Split('\n').Any(x => x.StartsWith("arc", StringComparison.Ordinal));
            Assert.IsFalse(TemArc(Novo().Save(100)));
            Sim s = Novo();
            Compra(s, "altar_aneis");
            Station l = s.Est("altar_lanternas"), a = s.Est("altar_aneis");
            s.ModoArcano(l, true); l.Medidor = 4321; l.Sequencia = 7;
            a.Busy = true; a.Progress = 0.5f; a.Energizada = true; a.Sobrecarrega = true; a.Medidor = 12000;   // forcada, e o jogador voltou ao Estavel
            s.PecasEnergizadas[I(s, "anel_cristal")] = 9;
            string t = s.Save(100);
            Sim r = Sim.Load(t, distrito: AF);
            Station rl = r.Est("altar_lanternas"), ra = r.Est("altar_aneis");
            Assert.AreEqual((4321, true, true, 7), (rl.Medidor, rl.Forcado, rl.Energizada, rl.Sequencia));
            Assert.AreEqual((12000, false, true, true, true), (ra.Medidor, ra.Forcado, ra.Energizada, ra.Sobrecarrega, ra.Busy));
            Assert.AreEqual(9, r.PecasEnergizadas[I(r, "anel_cristal")]);
            Assert.AreEqual(t, r.Save(100), "regrava igual");
            l.Pausa = 3.5f;
            Assert.AreEqual(3.5f, Sim.Load(s.Save(100), distrito: AF).Est("altar_lanternas").Pausa);

            string sem = string.Join("\n", t.Split('\n').Where(x => !x.StartsWith("arc", StringComparison.Ordinal)));
            Sim v = Sim.Load(sem, distrito: AF);
            Assert.IsTrue(v.Stations.Where(v.Arcana).All(x => x.Medidor == 0 && !x.Forcado && !x.Energizada && x.Sequencia == 0) && v.EnergizadasTotal == 0, "sem as linhas: padrao");
            Sim z = Sim.Load(sem + "arc4=lixo\narce=anel_cristal:x\narc999=1,1,1,1,1,1\narc0=1,1,1,1,1,1\n", distrito: AF);
            Assert.IsTrue(z.Stations.Where(z.Arcana).All(x => x.Medidor == 0 && !x.Forcado) && z.EnergizadasTotal == 0, "lixo = padrao");
            Assert.IsFalse(TemArc(z.Save(100)));
            Station p = Sim.Load(sem + "arc" + l.Index + "=5000,0,1,1,0,0\n", distrito: AF).Est("altar_lanternas");
            Assert.AreEqual((5000, false, false, false), (p.Medidor, p.Forcado, p.Energizada, p.Sobrecarrega), "parado e Estavel: nao fica energizada");
        }

        // ------------------------------------------------------------------ Isis, o bot e as fontes de qualidade

        /// <summary>§6: Isis (Smith, especialidade Arcane) e' a oficial de TODO altar (as duas familias): q +15 (+25 no nivel 5) nos altares, nada
        /// nas fornalhas; e o dreno +1.</summary>
        [Test]
        public void Isis_OficialEmTodoAltar()
        {
            Sim s = Novo();
            Producao(s);
            Station l = s.Est("altar_lanternas"), b = s.Est("altar_laminas"), cuba = s.Est("cuba_cristal");
            Assert.AreEqual(0, s.QualityBreakdown(l)[(int)QualitySource.Worker]);
            Isis(s, 1);
            Assert.AreEqual((15, 15, 0), (s.QualityBreakdown(l)[(int)QualitySource.Worker], s.QualityBreakdown(b)[(int)QualitySource.Worker],
                s.QualityBreakdown(cuba).Sum()), "cristal e runa, nao a cuba");
            Isis(s, 5);
            Assert.AreEqual(25, s.QualityBreakdown(s.Est("cadinho_celeste"))[(int)QualitySource.Worker]);
        }

        /// <summary>A regra fixa do bot (Bot.Arcano.cs): Forcado enquanto a proxima peca nao sobrecarrega (E + 2 x carga <= 100), senao Estavel;
        /// as politicas puras do portao (Estavel, Forcado). Jogando 20 min com tudo comprado, a regra nunca sobrecarrega.</summary>
        [Test]
        public void Bot_ForcadoSoSemSobrecarga()
        {
            Sim s = Novo();
            Station l = s.Est("altar_lanternas");
            Bot b = Bot.From(BotProfile.F2P);
            b.Step(s, Dt);
            Assert.IsTrue(l.Forcado, "E 0 + 30 cabe");
            l.Medidor = 7100;
            b.Step(s, Dt);
            Assert.IsFalse(l.Forcado, "71 + 30 passa de 100");
            b.Altares = PoliticaArcana.Forcado; b.Step(s, Dt);
            Assert.IsTrue(l.Forcado);
            b.Altares = PoliticaArcana.Estavel; l.Medidor = 0; b.Step(s, Dt);
            Assert.IsFalse(l.Forcado);

            Sim p = Novo();
            Producao(p);
            p.Gold = 0;
            Bot r = Bot.From(BotProfile.F2P);
            for (int i = 0; i < 20 * 60 * 30; i++) r.Step(p, Dt);
            Assert.Greater(p.EnergizadasTotal, 100);
            Assert.AreEqual(0, p.Sobrecargas, "a regra nunca entra em sobrecarga");
        }

        // ------------------------------------------------------------------ encomendas, contratos e Charter

        /// <summary>§7.1 (desvio, BALANCE.md §40): Quality x3 e Royal x2 no rodizio da Foundry; a Zelia (3 cajados Fina); a variante Superior do
        /// Quality no Renome 8; a Royal entregue conta para o Charter da Foundry.</summary>
        [Test]
        public void Encomendas_DaFoundry_RodizioEZelia()
        {
            CollectionAssert.AreEqual(new[] { OrderType.Quality, OrderType.Royal, OrderType.Quality, OrderType.Bulk, OrderType.Mixed, OrderType.Rush,
                OrderType.Quality, OrderType.Blueprint, OrderType.Common, OrderType.Royal }, OrderDefs.Ciclo(AF.Id));
            OrderDef zelia = Array.Find(OrderDefs.All, d => d.Id == "ord_reg_zelia");
            Assert.AreEqual((AF.Id, "cajado_liga", 3, Quality.Fine, "cli_reg_zelia"), (zelia.District, zelia.Item, zelia.FixedN, zelia.MinQuality, zelia.ClientId));
            Assert.AreEqual(8, RenomeDefs.NivelQueLibera("ord_quality_arcane_foundry_superior"));
            Assert.AreEqual(2025, OrderDefs.Comum(AF.Id).RewardMin);
            Assert.AreEqual((240f, 32f), (Array.Find(OrderDefs.All, d => d.Id == "ord_royal_arcane_foundry").TargetSeconds,
                Array.Find(OrderDefs.All, d => d.Id == "ord_royal_arcane_foundry").RewardSeconds), "a Royal mais lenta e a 70% (BALANCE.md §40)");
            Sim s = Novo();
            OrderDef royal = Array.Find(OrderDefs.All, d => d.Id == "ord_royal_arcane_foundry");
            var e = new Encomenda(royal, 1, new[] { (Item)I(s, "lanterna_arcana") }, new[] { 5 }, 12150, 0, 0f, null);
            e.Feito[0] = 5; s.Encomendas.Vagas.Add(e);
            Assert.IsTrue(s.Encomendas.Entregar(e) > 0);
            Assert.AreEqual(1, s.RoyalEntregues);
        }

        /// <summary>§8 (V28): os contratos da Foundry abrem com ela no build (a torre dos magos sem Renome; a vanguarda runica no Renome 8, Fina).</summary>
        [Test]
        public void Contratos_DaFoundry_AbremNoBuild()
        {
            var s = new Sim();
            foreach (string id in new[] { "escudos", "ferramentas" }) Assert.IsTrue(s.Buy((Upgrade)Upgrades.IndexOf(id, s.UpgradeDefs)));
            ContractDef torre = Array.Find(ContratosDefs.All, d => d.Id == "ctr_torre_magos"), vang = Array.Find(ContratosDefs.All, d => d.Id == "ctr_vanguarda");
            Assert.AreEqual((true, false), (s.Contratos.Aberto(torre), s.Contratos.Aberto(vang)));
            s.Renome.Ganha(3000);
            Assert.IsTrue(s.Contratos.Aberto(vang));
        }

        /// <summary>Golpe de mestre numa peca que o acumulador ja manda Superior (QAcc 99): Masterwork se ha obra pendente.</summary>
        static Quality Golpe(Sim s, Station st)
        {
            st.Out = 0; Array.Clear(st.OutQ, 0, st.OutQ.Length);
            Enche(st);
            st.QAcc = 99; s.StrikeCooldown = 0f; s.Player.Pos = st.InAt;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Strike(true), "golpe valendo no " + st.Name);
            int mw = s.Masterworks;
            for (int i = 0; i < 4000; i++)
            {
                s.Tick(Dt, 0f, 0f);
                if (s.Masterworks > mw) return Quality.Masterwork;
                if (st.Out > 0) return st.OutQ[(int)Quality.Superior] > 0 ? Quality.Superior : Quality.Normal;
            }
            Assert.Fail("o altar nao fez a peca");
            return Quality.Normal;
        }

        /// <summary>
        /// O Charter da Foundry pelo caminho real: producao completa, niveis V das duas familias, as tecnicas pela fila de encomenda e a Isis no 5
        /// (q 60 + 20 + 25 = 105: Superior pelo acumulador, sem precisar do Forcado), os projetos pelos marcos de venda, 100 energizadas seguidas no
        /// altar de aneis e 100 amuletos energizados; golpe numa Superior = as 4 obras essenciais no Hall. Falta o contrato: 1 contrato ou 19 Royal
        /// nao bastam; a 20a Royal (ou 2 contratos) fecha. 1x.
        /// </summary>
        [Test]
        public void Charter_PeloCaminhoReal_DoisContratosOuVinteRoyal()
        {
            foreach (bool comContrato in new[] { false, true })
            {
                Imperio imp = Imperio.Chegada(AF);
                Sim s = imp.Ativo;
                Producao(s);
                s.Tick(Dt, 0f, 0f);   // o projeto de mestre (marco da producao)
                Compra(s, "nivel_altar_cristal2", "nivel_altar_cristal3", "nivel_altar_cristal4", "nivel_altar_cristal5", "nivel_altar_runa2", "nivel_altar_runa3",
                    "nivel_altar_runa4", "nivel_altar_runa5");
                Assert.IsNotNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, AF.Id, "enc:1"));
                Assert.IsNotNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, AF.Id, "enc:2"));
                Isis(s, 5);
                Assert.AreEqual((105, 105), (s.QualityPoints(s.Est("altar_lanternas")), s.QualityPoints(s.Est("altar_laminas"))));
                Assert.AreEqual("renome", imp.CharterFalta(AF.Id));
                imp.Raiz.Renome.Ganha(3000 - imp.Raiz.Renome.Total);
                Assert.AreEqual("masterwork", imp.CharterFalta(AF.Id));
                s.SoldItems[I(s, "lanterna_arcana")] = 150; s.SoldItems[I(s, "anel_cristal")] = 300; s.SoldItems[I(s, "amuleto_arcano")] = 200;
                s.SoldItems[I(s, "lamina_runica")] = 150;
                s.Est("altar_aneis").Sequencia = 100; s.PecasEnergizadas[I(s, "amuleto_arcano")] = 100;
                s.Tick(Dt, 0f, 0f);
                foreach (string st in new[] { "altar_lanternas", "altar_aneis", "altar_amuletos", "altar_laminas" }) Assert.AreEqual(Quality.Masterwork, Golpe(s, s.Est(st)), st);
                Assert.AreEqual((4, true, true), (s.Hall.Count, s.Hall.Tem("mw_anel_estrelas"), s.Hall.Tem("mw_lamina_tempestade")));
                Assert.AreEqual("contrato", imp.CharterFalta(AF.Id));
                imp.Raiz.Contratos.Concluidos.Add("ctr_torre_magos");
                s.RoyalEntregues = 2 * CharterDef.RoyalPorContrato - 1;
                Assert.AreEqual("contrato", imp.CharterFalta(AF.Id), "1 contrato ou 19 Royal nao bastam");
                if (comContrato) imp.Raiz.Contratos.Concluidos.Add("ctr_vanguarda");
                else s.RoyalEntregues++;
                Assert.IsNull(imp.CharterFalta(AF.Id));
                Assert.IsTrue(imp.Charter(AF.Id));
                Assert.IsFalse(imp.Charter(AF.Id), "1x");
            }
        }

        // ------------------------------------------------------------------ validator e layout

        static DistrictDef Copia(Action<DistrictDef> f)
        {
            DistrictDef d = AF;
            var c = new DistrictDef
            {
                Id = d.Id, Nome = d.Nome, Indice = d.Indice, Itens = d.Itens, Receitas = d.Receitas, Upgrades = d.Upgrades, Workers = d.Workers,
                Projetos = d.Projetos, Masterworks = d.Masterworks, MarcosColecao = d.MarcosColecao, Estacoes = d.Estacoes, Pads = d.Pads,
                Obstaculos = d.Obstaculos, Marcos = d.Marcos, Inicio = d.Inicio, Contratacao = d.Contratacao, Largura = d.Largura, Parede = d.Parede,
                EsteiraDe = d.EsteiraDe, EsteiraPara = d.EsteiraPara, Ajuste = d.Ajuste, Abertura = d.Abertura, Charter = d.Charter,
                FundoFator = d.FundoFator, Selo = d.Selo, InsumoPorEspera = d.InsumoPorEspera, Clientes = d.Clientes,
            };
            f(c);
            return c;
        }

        static DistrictDef ComAjuste(Action<Tuning> extra) => Copia(c => c.Ajuste = t => { AF.Ajuste(t); extra(t); });

        /// <summary>As regras novas do validator: carga so em produto, de 1 a 100 e do tamanho da tabela; dreno base positivo; a oficial arcana sem
        /// familia e uma so; sem a energizada e a Isis os altares nao chegam a Superior (q 80); obra e projeto de pecas energizadas ou de sequencia
        /// so onde ha altar.</summary>
        [Test]
        public void Validator_DefeitosInjetados_Acusa()
        {
            RowTests.Acusa(ContentValidator.Distrito(ComAjuste(t => t.CargaArcana[9] = 5)).ToList(), "carga em cristal, que nao e' produto");
            RowTests.Acusa(ContentValidator.Distrito(ComAjuste(t => t.CargaArcana[2] = 101)).ToList(), "carga 101 em lanterna_arcana (de 1 a 100");
            RowTests.Acusa(ContentValidator.Distrito(ComAjuste(t => t.CargaArcana = new[] { 0, 0, 15 })).ToList(), "carga com 3 entradas");
            RowTests.Acusa(ContentValidator.Distrito(ComAjuste(t => t.DrenoBase = 0)).ToList(), "dreno base 0");

            WorkerDef isis = Array.Find(AF.Workers, w => w.Id == "wk_isis");
            var comFamilia = new WorkerDef("wk_isis", "Ísis", WorkerRole.Smith, Specialty.Arcane, AF.Id, "altar_cristal", null, -1, 190000, "altar_laminas", isis.Niveis, "oficial", "dreno");
            var outra = new WorkerDef("wk_isis2", "Ísis 2", WorkerRole.Smith, Specialty.Arcane, AF.Id, null, null, -1, 190000, "altar_laminas", isis.Niveis, "oficial", "dreno");
            RowTests.Acusa(ContentValidator.Distrito(Copia(c => c.Workers = AF.Workers.Select(w => w.Id == "wk_isis" ? comFamilia : w).ToArray())).ToList(), "oficial arcana com familia");
            RowTests.Acusa(ContentValidator.Distrito(Copia(c => c.Workers = AF.Workers.Append(outra).ToArray())).ToList(), "segunda oficial arcana");
            // sem a Isis: niveis 60 + tecnica 20 + energizada 20 = 100, nao passa de 100
            RowTests.Acusa(ContentValidator.Distrito(Copia(c => c.Workers = AF.Workers.Where(w => w.Id != "wk_isis").ToArray())).ToList(),
                "masterwork mw_lanterna_eterna: Superior inalcancavel");

            Sim row = ContentValidator.Mundo(Distritos.ArmorersRow);
            var mw = new MasterworkDef("mw_teste", Distritos.ArmorersRowId, "elmo", "bp_mw_teste", 30, 1,
                new CondicaoMasterwork(CondicaoMw.PecasEnergizadas, "elmo", 5), new CondicaoMasterwork(CondicaoMw.SequenciaEstavel, "montagem_elmo", 5));
            var bp = new BlueprintDef("bp_mw_teste", Distritos.ArmorersRowId, BlueprintKind.Masterwork, new[] { "mw_teste" }, false,
                new BlueprintSource(BlueprintSourceKind.Milestone, BlueprintSource.Energizadas, 500));
            List<string> v = MasterworksDefs.Validar(new[] { mw }, new MarcoColecaoDef[0], new[] { bp }, row.UpgradeDefs, row).ToList();
            RowTests.Acusa(v, "condicao PecasEnergizadas \"elmo\" >= 5: inalcancavel");
            RowTests.Acusa(v, "condicao SequenciaEstavel \"montagem_elmo\" >= 5: inalcancavel");
            RowTests.Acusa(ContentValidator.Blueprints(new[] { bp }, row.UpgradeDefs, row).ToList(), "marco \"energizadas\" x500 inalcancavel");
            Assert.IsEmpty(ContentValidator.Blueprints(AF.Projetos, AF.Upgrades, ContentValidator.Mundo(AF)).ToList());
        }

        /// <summary>O rotulo de cada deposito (1,35 m acima dele) nao encosta no da estacao de cima na mesma coluna (1,04 m abaixo dela; caixa de
        /// 0,37 m), como na Row e no Arsenal.</summary>
        [Test]
        public void Layout_RotuloDoDepositoNaoEncosta()
        {
            foreach (StationDef dep in AF.Estacoes.Where(x => x.Kind == Kind.Deposit))
                foreach (StationDef p in AF.Estacoes.Where(x => x.Familia != null && x.Pos.Y > dep.Pos.Y && Math.Abs(x.Pos.X - dep.Pos.X) < 1.5f))
                    Assert.GreaterOrEqual(p.Pos.Y - 1.04f - (dep.Pos.Y + 1.35f), 0.37f, $"{dep.Id} x {p.Id}");
        }

        // ------------------------------------------------------------------ textos

        /// <summary>DM §109: tudo o que a Foundry trouxe tem texto nos 3 idiomas e cabe: melhorias (nome <= 16, placa e menu <= 26, cartao do menu
        /// com % e o dos condensadores com o dreno), itens e dicas, estacoes, distrito, obras e condicoes (energizadas, sequencia), projetos (o marco
        /// das energizadas), marcos da Collection, o medidor do altar e a freguesa.</summary>
        [Test]
        public void Textos_DaFoundryNosTresIdiomas()
        {
            Sim s = Novo();
            try
            {
                Textos.Distrito(AF);
                var faltam = new List<string>();
                void Tem(string k) { if (!Loc.Tem(k)) faltam.Add(k); }
                foreach (UpgradeDef d in AF.Upgrades) { Tem("up." + d.Id + ".nome"); Tem("up." + d.Id + ".desc"); }
                foreach (ItemDef d in AF.Itens) { Tem("item." + d.Id); Tem("dica.balcao." + d.Id); Tem("dica.pegue." + d.Id); }
                foreach (StationDef d in AF.Estacoes) Tem("est." + d.Id);
                foreach (MasterworkDef d in AF.Masterworks) Tem("mw." + d.Id);
                foreach (string k in new[] { "distrito." + AF.Id, "cli.cli_reg_zelia", "cos.cos_col_estandarte_arcane_foundry", "fam.altar_cristal", "fam.altar_runa",
                    "qual.fonte.5", "hall.c.energizadas", "hall.c.sequencia", "proj.f.energizadas", "arc.botao_forcado", "arc.botao_estavel" }) Tem(k);
                CollectionAssert.IsEmpty(faltam);
                Sim w = ContentValidator.Mundo(AF);
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    for (int u = 0; u < AF.Upgrades.Length; u++)
                    {
                        Assert.LessOrEqual(Textos.Nome(u).Length, ContentValidator.NameMax, $"{i}: {Textos.Nome(u)}");
                        foreach (string d in new[] { Textos.Placa(u), Textos.Menu(u) }) Assert.LessOrEqual(d.Length, Textos.DicaMax, $"{i}: {d}");
                        if (!AF.Upgrades[u].InMenu) continue;
                        string b = Textos.Beneficio(s.T, u);
                        bool cond = AF.Upgrades[u].Id.StartsWith("condensador", StringComparison.Ordinal);
                        StringAssert.Contains(cond ? "/s" : "%", b, $"{i}: {b}");
                        Assert.LessOrEqual(b.Length, 2 * 14, $"{i}: {b}");
                        Assert.LessOrEqual(Textos.AntesDepois(s.T, u).Length, 16, $"{i}: {Textos.AntesDepois(s.T, u)}");
                    }
                    for (int it = 0; it < AF.Itens.Length; it++)
                        foreach (Hint h in new[] { Hint.ProductToCounter, Hint.PickProducts, Hint.ClientWaiting })
                        {
                            string d = Textos.Dica(h, it, s);
                            Assert.LessOrEqual(d.Length, Textos.DicaMax, $"{i}: {d}");
                            StringAssert.DoesNotContain("[", d, $"{i}: chave faltando");
                        }
                    foreach (MasterworkDef d in w.Hall.Defs)
                    {
                        foreach (string t in new[] { Textos.NomeMasterwork(d), Textos.CondicaoMasterwork(w, d), Textos.ProjetoMasterwork(w, d) })
                            StringAssert.DoesNotContain("[", t, $"{i}: {d.Id}");
                        Assert.LessOrEqual(Textos.NomeMasterwork(d).Length, Textos.HallMax, $"{i}: {d.Id}");
                        Assert.LessOrEqual(Textos.CondicaoMasterwork(w, d).Length, Textos.HallDetalheMax, $"{i}: {Textos.CondicaoMasterwork(w, d)}");
                    }
                    foreach (BlueprintDef d in w.Projetos.Defs)
                        StringAssert.DoesNotContain("[", Textos.ProjetoNome(w, d) + Textos.ProjetoLibera(w, d) + Textos.ProjetoFonte(w, d), $"{i}: {d.Id}");
                    foreach (MarcoColecaoDef m in w.Colecao.Marcos)
                        foreach (string t in new[] { Textos.MarcoColecao(w, m), Textos.PremioColecao(m), Textos.SeloColecao(w, m) })
                        {
                            StringAssert.DoesNotContain("[", t, $"{i}: {m.Id}");
                            Assert.LessOrEqual(t.Length, Textos.PremioMax, $"{i}: {t}");
                        }
                    Station c = w.Est("cadinho_celeste");
                    c.Medidor = 12345; c.Forcado = true;
                    foreach (string t in new[] { Textos.Medidor(w, c), Textos.Medidor(w, w.Est("altar_lanternas")) })
                    {
                        StringAssert.DoesNotContain("[", t, $"{i}: {t}");
                        Assert.LessOrEqual(t.Length, 30, $"{i}: {t}");
                    }
                    c.Forcado = false; c.Medidor = 0;
                }
                Loc.Atual = Idioma.Pt;
                Sim e = Novo();
                Station l = e.Est("altar_lanternas");
                Assert.AreEqual("Estável · 0/100", Textos.Medidor(e, l));
                l.Medidor = 9050; Enche(l);
                Assert.AreEqual("Estabilizando · 90/100", Textos.Medidor(e, l));
                e.ModoArcano(l, true);
                Assert.AreEqual("Forçado · 90/100", Textos.Medidor(e, l));
                l.Pausa = 6.2f;
                Assert.AreEqual("Sobrecarga! 7 s", Textos.Medidor(e, l));
                Assert.AreEqual("Energize 100 amuletos arcanos (0/100)", Textos.CondicaoMasterwork(e, Array.Find(e.Hall.Defs, d => d.Id == "mw_amuleto_oraculo")));
                Assert.AreEqual("Altar de anéis: 100 energizadas seguidas (0/100)", Textos.CondicaoMasterwork(e, Array.Find(e.Hall.Defs, d => d.Id == "mw_anel_estrelas")));
                Assert.AreEqual("Energize 500 peças (0/500)", Textos.ProjetoFonte(e, Array.Find(e.Projetos.Defs, d => d.Id == "bp_var_aura")));
                Assert.AreEqual(("Medidor cai +2/s", "3 → 5/s", "5 → 7/s"), (Textos.Beneficio(e.T, U(e, "condensador")), Textos.AntesDepois(e.T, U(e, "condensador")),
                    Textos.AntesDepois(e.T, U(e, "condensador2"))));
            }
            finally { Textos.Distrito(Distritos.OldForge); Loc.Atual = Idioma.Pt; }
        }
    }
}
