using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// C-CORE-06 (CONTEUDO_V1 §2.3, §3-§12; BALANCE.md §37): o Artisan Quarter. A bancada de ourives configuravel (qualquer receita liberada;
    /// troca = termina a peca, espera a saida, 4 s parada; troca rapida 1 s, Lucia -1 s; insumo de fora fica guardado: nada se perde; o
    /// carregador so leva o insumo da receita ativa), os clientes por familia (rodizio fixo, unidades, paciencia, intervalo, qualidade minima,
    /// RichClients), o bot com a regra do catalogo, as encomendas, contratos, obras e o Charter do Quarter pelo caminho de sempre, o validator
    /// com defeitos injetados e os textos nos 3 idiomas. NUnit 3.5 do Unity: tipos exatos nas tuplas (long com L, float com f).
    /// </summary>
    public class ArtisanTests
    {
        const float Dt = 1f / 30f;
        static readonly DistrictDef Aq = Distritos.ArtisanQuarter;

        static Sim NovoAq() => new Sim(distrito: Aq);
        static int U(Sim s, string id) => Upgrades.IndexOf(id, s.UpgradeDefs);
        static Item I(Sim s, string id) => (Item)Items.IndexOf(id, s.ItemDefs);
        static int K(Sim s, string id) => WorkerDefs.IndexOf(id, s.Equipe.Defs);
        static void Compra(Sim s, params string[] ids) { foreach (string id in ids) Assert.IsTrue(s.Buy((Upgrade)U(s, id)), id); }
        static void Producao(Sim s) { for (int u = 0; u < s.UpgradeDefs.Length; u++) if (s.IsProduction(u)) s.Buy((Upgrade)u); }
        static void Ate(Sim s, Func<bool> ok, float max, string msg) { for (float t = 0f; t < max && !ok(); t += Dt) s.Tick(Dt, 0f, 0f); Assert.IsTrue(ok(), msg); }
        static int Total(Station b, Sim s, Item i) { int k = b.InIndex(i); return (k >= 0 ? b.Ins[k] : 0) + b.Guardados[(int)i]; }
        static DistrictDef Copia(DistrictDef d) => (DistrictDef)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(d, null);

        /// <summary>O proximo cliente da familia `f` (a fila e' esvaziada a cada tick: so o relogio das familias anda); o relogio de quando chegou.</summary>
        static (Client c, float t) Proximo(Sim s, int f, float max = 120f)
        {
            for (float t = 0f; t < max; t += Dt)
            {
                s.Queue.Clear();
                s.Tick(Dt, 0f, 0f);
                Client c = s.Queue.Find(x => x.Familia == f);
                if (c != null) return (c, s.Time);
            }
            Assert.Fail($"nenhum cliente da familia {f} em {max} s");
            return (null, 0f);
        }

        // ------------------------------------------------------------------ distrito, kit e chegada

        /// <summary>§2.3: kit = deposito de cobre, crisol de cobre, 1 bancada de ourives (fivela e cinzel) e a loja fina de 3 vagas (5 com a
        /// vitrine de luxo), estoque 10; 18 produtivos (x1,5 do §5.3), precos x3 do §3, abertura 430.000, Charter (producao, Renome 6, 3 obras),
        /// a Row passa a liberar o Quarter; o manager Valentim no Imperio.</summary>
        [Test]
        public void Kit_DoQuarter_NumerosDoDistrito()
        {
            Sim s = NovoAq();
            string[] kit = s.Stations.Where(x => x.Unlocked).Select(x => x.Id).ToArray();
            CollectionAssert.AreEqual(new[] { "deposito", "fornalha", "bancada_ourives", "balcao" }, kit);
            Assert.AreEqual((I(s, "minerio_cobre"), I(s, "lingote_cobre"), I(s, "fivela")), (s.Deposit.OutItem, s.FurnaceA.OutItem, s.Est("bancada_ourives").OutItem));
            Assert.AreEqual((3, 10), (s.QueueCap, s.CounterCap));
            Assert.AreEqual((18, 794565L), (Enumerable.Range(0, s.UpgradeDefs.Length).Count(s.IsProduction), Enumerable.Range(0, s.UpgradeDefs.Length).Where(s.IsProduction).Sum(u => s.T.Cost(u))));
            Assert.AreEqual((2160L, 180000L, 130000L, 36000L, 630000L), (s.T.Cost(0), s.T.Cost(17), s.T.Cost(18), s.T.Cost(21), s.T.Cost(24)));
            Assert.AreEqual((8.5f, 180f), (s.T.OrderRewardSeconds, s.T.OrderSeconds), "encomendas do Quarter: premio x 0,85 e alvo x 1,2");
            Assert.AreEqual((330, 1350, 12f), (s.T.Price[(int)I(s, "fivela")], s.T.Price[(int)I(s, "caixa_joias")], s.T.ClientInterval[(int)I(s, "colar")]));
            Compra(s, "vitrine_luxo");
            Assert.AreEqual(5, s.QueueCap, "a loja fina 3 -> 5");
            Assert.AreEqual((true, 1250L, 3, Distritos.RoyalArsenalId), (Aq.Charter.Producao, Aq.Charter.Renome, Aq.Charter.Masterworks, Aq.Charter.Proximo));
            Assert.AreEqual((Distritos.ArtisanQuarterId, 430000L), (Distritos.ArmorersRow.Charter.Proximo, Aq.Abertura));
            WorkerDef m = Imperio.ManagerDe(Aq.Id);
            Assert.AreEqual(("wk_valentim", 290000L, 5), (m.Id, m.Niveis[0], m.MaxNivel));
            Assert.AreEqual(6, new Sim(distrito: Distritos.ArmorersRow).QueueCap, "a Row segue com 6 vagas");
        }

        /// <summary>C-CORE-06: o jogador chega ao Quarter com a cadeia de Charters (Old Forge e Row abertos, em producao completa, com o Charter)
        /// e o fundo dos dois (600 + 1.800 = 2.400/min, CONTEUDO_V1 §5.3: "fundo ao chegar 2.400/min"); a abertura (430.000) paga.</summary>
        [Test]
        public void Chegada_CadeiaDeCharters_FundoDosDois()
        {
            Imperio imp = Imperio.Chegada(Aq);
            Sim row = imp.Distrito(Distritos.ArmorersRowId);
            Assert.AreEqual((3, true, true, true), (imp.Abertos.Count, imp.Charters.Contains(Distritos.OldForgeId), imp.Charters.Contains(Distritos.ArmorersRowId), imp.Ativo.Distrito == Aq));
            Assert.IsTrue(row.ProductionComplete && imp.Raiz.ProductionComplete);
            Assert.AreEqual(40.0, imp.FundoPorSegundo(imp.Raiz) + imp.FundoPorSegundo(row), 1e-9, "10/s do Old Forge + 30/s da Row");
            Assert.AreEqual(0L, imp.Ativo.Gold, "a abertura saiu da carteira");
            Imperio r = Imperio.Chegada(Distritos.ArmorersRow);
            Assert.AreEqual((2, 10.0), (r.Abertos.Count, r.FundoPorSegundo(r.Raiz)), "a Row chega como antes");
        }

        // ------------------------------------------------------------------ bancada configuravel (§2.3 regras 1-2)

        /// <summary>Regra 1: a bancada faz qualquer receita do distrito ja liberada (7 receitas; entrada de 6 por tipo); a linha abre com a
        /// receita (cliente e encomenda contam com ela); receita travada e a ja pedida recusam; pedir a ativa cancela a troca. Estacao fixa e o
        /// Old Forge: sem bancada configuravel.</summary>
        [Test]
        public void Bancada_FazQualquerReceitaLiberada()
        {
            Sim s = NovoAq();
            Station b = s.Est("bancada_ourives");
            Item fivela = I(s, "fivela"), cinzel = I(s, "cinzel"), castical = I(s, "castical");
            Assert.AreEqual((7, 6, true), (b.Receitas.Length, b.InCap, s.TemBancadas));
            Assert.IsNull(s.Est("fornalha").Receitas, "o crisol e' estacao fixa");
            Assert.AreEqual((true, true, false), (s.LineUnlocked(fivela), s.LineUnlocked(cinzel), s.LineUnlocked(castical)), "o kit: fivela e cinzel");
            Assert.IsFalse(s.Configura(b, castical), "receita travada");
            Assert.IsTrue(s.Configura(b, cinzel));
            Assert.IsFalse(s.Configura(b, cinzel), "ja pedida");
            Assert.IsTrue(s.Configura(b, fivela), "a ativa de novo cancela");
            Assert.IsNull(b.Proxima);
            Compra(s, "receita_castical");
            Assert.AreEqual((true, true), (s.LineUnlocked(castical), s.Configura(b, castical)));
            Assert.IsFalse(s.Configura(s.Est("bancada_ourives2"), fivela), "bancada fechada");
            var of = new Sim();
            Assert.AreEqual((false, false), (of.TemBancadas, of.Stations.Exists(x => x.Receitas != null)));
            Assert.IsFalse(of.Configura(of.AnvilA, Item.Sword));
        }

        /// <summary>Regra 1: trocar = a bancada termina a peca em curso, espera a saida esvaziar (a pilha e' de um produto so) e fica parada 4 s;
        /// a Lucia tira 1 s; a troca rapida deixa em 1 s (minimo 1 s, mesmo com a Lucia).</summary>
        [Test]
        public void Troca_TerminaAPecaEsperaASaidaEParaQuatroSegundos()
        {
            Sim s = NovoAq();
            Station b = s.Est("bancada_ourives");
            Item fivela = I(s, "fivela"), cinzel = I(s, "cinzel");
            b.In = 2;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(b.Busy, "fivela em curso");
            Assert.IsTrue(s.Configura(b, cinzel));
            Ate(s, () => b.Out == 1, 5f, "a fivela em curso termina");
            for (float t = 0f; t < 2f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((false, 1, fivela, 1), (b.Busy, b.Out, b.OutItem, b.In), "espera a saida: nao comeca outra fivela nem troca");
            b.Out = 0;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((cinzel, 1, 1), (b.OutItem, b.In, s.TrocasReceita), "trocou: o cobre que sobrou ficou na entrada");
            Assert.AreEqual(4f, s.TempoTroca(b));
            b.In = 2;
            for (float t = 0f; t < 3.8f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.IsFalse(b.Busy, "parada 4 s");
            for (float t = 0f; t < 0.4f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(b.Busy, "depois dos 4 s, o cinzel");
            Compra(s, "fole_ourives", "nivel_ourivesaria2");
            s.Gold = 22000;
            Assert.IsTrue(s.Equipe.Comprar(K(s, "wk_lucia")));
            Assert.AreEqual(3f, s.TempoTroca(b), "a Lucia: -1 s");
            Compra(s, "bancada_ourives2", "bancada_ourives3", "troca_rapida");
            Assert.AreEqual(1f, s.TempoTroca(b), "troca rapida: 1 s (minimo 1, mesmo com a Lucia)");
        }

        /// <summary>CONTEUDO_V1 §14, o teste negativo da mecanica: troca de receita nao perde insumo. Cobre, ouro e gema passam de receita em
        /// receita (fivela -> colar -> caixa -> fivela) e a soma da entrada com os guardados nunca muda; o que a receita nova nao usa fica
        /// guardado (a entrada nao aceita mais dele: o carregador so leva o da receita ativa) e volta do save igual.</summary>
        [Test]
        public void Troca_NaoPerdeInsumo()
        {
            Sim s = NovoAq();
            Compra(s, "veio_ouro", "lapidario", "receita_caixa");
            Station b = s.Est("bancada_ourives");
            Item cobre = I(s, "lingote_cobre"), ouro = I(s, "lingote_ouro"), gema = I(s, "gema");
            b.Ins[b.InIndex(cobre)] = 6; b.Guardados[(int)ouro] = 4; b.Guardados[(int)gema] = 3;   // o ouro e a gema de uma receita de antes
            void Troca(string produto)
            {
                Assert.IsTrue(s.Configura(b, I(s, produto)), produto);
                s.Tick(Dt, 0f, 0f);
                Assert.AreEqual(I(s, produto), b.OutItem, produto);
                Assert.AreEqual((6, 4, 3), (Total(b, s, cobre), Total(b, s, ouro), Total(b, s, gema)), "nada se perde: " + produto);
            }
            Troca("colar");
            Assert.AreEqual((4, 3, 6), (b.Ins[b.InIndex(ouro)], b.Ins[b.InIndex(gema)], b.Guardados[(int)cobre]), "o ouro e a gema voltaram; o cobre ficou guardado");
            Assert.IsFalse(b.Accepts(cobre), "a entrada so recebe o da receita ativa");
            Troca("caixa_joias");
            Assert.AreEqual(0, b.Guardados.Sum(), "a caixa usa os tres");
            Troca("fivela");
            Assert.AreEqual((6, 4, 3), (b.In, b.Guardados[(int)ouro], b.Guardados[(int)gema]));
            string txt = s.Save(0);
            StringAssert.Contains("ofc2=fivela,-,", txt);
            Sim l = Sim.Load(txt, distrito: Aq);
            Station lb = l.Est("bancada_ourives");
            Assert.AreEqual((I(l, "fivela"), 6, 4, 3), (lb.OutItem, lb.In, lb.Guardados[(int)ouro], lb.Guardados[(int)gema]), "o save guarda a receita e os guardados");
            Assert.AreEqual(s.Est("bancada_ourives").Parada, lb.Parada, 1e-3f);
        }

        /// <summary>Regra 2: os carregadores so entregam insumos da receita ativa: com as bancadas no cobre, o Dourado dos lingotes (a regra 1 do transporte, Sim.Transporte e BALANCE.md §39) nao busca o
        /// lingote de ouro (ficaria parado com ele na mao); a bancada no broche (ouro + cobre) passa a recebe-lo.</summary>
        [Test]
        public void Carregadores_SoEntregamOInsumoDaReceitaAtiva()
        {
            Sim s = NovoAq();
            Compra(s, "veio_ouro", "dourado2");   // sem o Dourado 1 (minerio): nada de cobre, a bancada fica parada
            Station b = s.Est("bancada_ourives"), co = s.Est("crisol_ouro");
            Item ouro = I(s, "lingote_ouro");
            co.Out = co.OutCap;
            for (float t = 0f; t < 20f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(co.OutCap, co.Out, "ninguem tirou o ouro do crisol");
            Assert.IsFalse(s.Workers.Exists(w => w.Held[(int)ouro] > 0), "nenhum Dourado com ouro na mao");
            Assert.IsTrue(s.Configura(b, I(s, "broche")));
            Ate(s, () => b.OutItem == I(s, "broche") && b.Ins[b.InIndex(ouro)] > 0, 60f, "o ouro chega a bancada do broche");
        }

        /// <summary>Old Forge e Row: nada da mecanica no save (ofcN=, fam=, ricos=) nem no Sim; o Client de sempre sem familia.</summary>
        [Test]
        public void OldForgeERow_SemBancadaNemFamilia()
        {
            foreach (DistrictDef d in new[] { Distritos.OldForge, Distritos.ArmorersRow })
            {
                var s = new Sim(distrito: d);
                Assert.AreEqual((false, (ClientDef[])null), (s.TemBancadas, d.Clientes), d.Id);
                for (float t = 0f; t < 20f; t += Dt) s.Tick(Dt, 0f, 0f);
                Assert.IsTrue(s.Queue.TrueForAll(c => c.Familia == -1 && c.MinQ == Quality.Normal), d.Id);
                string txt = s.Save(0);
                foreach (string k in new[] { "ofc", "\nfam=", "\nricos=" }) StringAssert.DoesNotContain(k, txt, d.Id);
            }
            Sim aq = NovoAq();
            StringAssert.DoesNotContain("ofc", aq.Save(0), "bancada no padrao: sem linha");
        }

        // ------------------------------------------------------------------ clientes por familia (§12)

        /// <summary>§12: o 1o cliente e' o viajante (fivela) no FirstClientAt; o rodizio fixo (n-esimo cliente -> n-esimo produto aberto da
        /// lista); o relogio de cada familia soma o intervalo da linha x o dela (viajante 12 s, mercador 15,6 s); paciencia do produto x a da
        /// familia; o mercador leva 2 (1 por atendimento). Nobre so com um produto dele aberto (o broche, com o veio de ouro).</summary>
        [Test]
        public void Familias_RodizioFixoUnidadesPacienciaIntervalo()
        {
            Sim s = NovoAq();
            Compra(s, "receita_castical");
            Item fivela = I(s, "fivela"), cinzel = I(s, "cinzel"), castical = I(s, "castical"), broche = I(s, "broche");
            (Client v1, float t1) = Proximo(s, 0);
            Assert.AreEqual((fivela, 1, Quality.Normal), (v1.Want, v1.Pack, v1.MinQ));
            Assert.AreEqual(s.T.FirstClientAt, t1, 0.05f, "o 1o cliente cedo");
            Assert.AreEqual(s.T.PatienceFor(fivela, s.RecipeDefs), v1.MaxPatience, 1e-4f);
            (Client v2, float t2) = Proximo(s, 0);
            (Client v3, _) = Proximo(s, 0);
            Assert.AreEqual((castical, fivela), (v2.Want, v3.Want), "viajante: fivela, castical, fivela...");
            Assert.AreEqual(12f, t2 - t1, 0.05f, "o intervalo da linha (12 s) x 1,0");
            var s2 = NovoAq();
            (Client m1, float tm1) = Proximo(s2, 1);
            (Client m2, float tm2) = Proximo(s2, 1);
            Assert.AreEqual((fivela, cinzel, 2), (m1.Want, m2.Want, m1.Pack), "mercador: fivela, cinzel (castical travado), 2 por visita");
            Assert.AreEqual(15.6f, tm1, 0.05f, "x1,3");
            Assert.AreEqual(15.6f, tm2 - tm1, 0.05f, "x1,3");
            Assert.AreEqual(s2.T.PatienceFor(fivela, s2.RecipeDefs) * 0.8f, m1.MaxPatience, 1e-3f);
            s2.Queue.Clear();
            s2.Stock[(int)fivela] = 2;
            s2.Queue.Add(m1);
            s2.Tick(Dt, 0f, 0f);
            Assert.AreEqual((1, 1, true), (s2.Sales, m1.Pack, s2.Queue.Contains(m1)), "1 por atendimento: fica na vaga");
            for (float t = 0f; t < 0.5f; t += Dt) s2.Tick(Dt, 0f, 0f);
            Assert.AreEqual((2, false), (s2.Sales, s2.Queue.Contains(m1)), "levou os 2");
            var s3 = NovoAq();
            for (float t = 0f; t < 90f; t += Dt) { s3.Queue.Clear(); s3.Tick(Dt, 0f, 0f); Assert.IsFalse(s3.Queue.Exists(c => c.Familia == 2), "nobre sem produto aberto"); }
            Compra(s3, "veio_ouro");
            (Client n1, _) = Proximo(s3, 2);
            Assert.AreEqual((broche, 1), (n1.Want, n1.Pack));
            Assert.AreEqual(s3.T.PatienceFor(broche, s3.RecipeDefs) * 1.8f, n1.MaxPatience, 1e-3f);
        }

        /// <summary>§12 e regra 5: o colecionador abre no Renome 2 e so com Fina na automacao, e so leva Fina ou melhor; nobres e colecionadores
        /// atendidos (o pacote todo) contam no RichClients (ClientesRicos), que vai e volta no save com o rodizio (fam=, ricos=).</summary>
        [Test]
        public void Colecionador_FinaOuMelhor_RichClientsNoSave()
        {
            var sf = NovoAq();
            sf.Renome.Ganha(70);
            for (float t = 0f; t < 60f; t += Dt) { sf.Queue.Clear(); sf.Tick(Dt, 0f, 0f); Assert.IsFalse(sf.Queue.Exists(c => c.Familia == 3), "sem Fina na automacao"); }
            Sim s = NovoAq();
            Compra(s, "fole_ourives", "nivel_ourivesaria2");
            for (float t = 0f; t < 60f; t += Dt) { s.Queue.Clear(); s.Tick(Dt, 0f, 0f); Assert.IsFalse(s.Queue.Exists(c => c.Familia == 3), "sem Renome 2"); }
            s.Renome.Ganha(70);
            (Client c1, _) = Proximo(s, 3);
            Assert.AreEqual((Quality.Fine, 1), (c1.MinQ, c1.Pack));
            // o atendimento num tick so (o relogio das familias e' deterministico: ninguem chega no meio); o broche, que so nobre e colecionador pedem
            Compra(s, "veio_ouro");
            Item broche = I(s, "broche");
            var col = new Client { Want = broche, Patience = 99f, MaxPatience = 99f, Pack = 1, MinQ = Quality.Fine, Familia = 3 };
            s.Queue.Clear(); s.Queue.Add(col); s.Stock[(int)broche] = 1;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((0, 0, true), (s.Sales, s.ClientesRicos, s.Queue.Contains(col)), "Normal no balcao: nao leva");
            s.StockQ[(int)broche][(int)Quality.Fine] = 1;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((1, 1), (s.Sales, s.ClientesRicos), "a Fina: levou e conta como rico");
            for (float t = 0f; t < 0.5f; t += Dt) { s.Queue.Clear(); s.Tick(Dt, 0f, 0f); }
            var nob = new Client { Want = broche, Patience = 99f, MaxPatience = 99f, Pack = 1, Familia = 2 };
            s.Queue.Clear(); s.Queue.Add(nob); s.Stock[(int)broche] = 1;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(2, s.ClientesRicos, "o nobre tambem");
            var via = new Client { Want = broche, Patience = 99f, MaxPatience = 99f, Pack = 1, Familia = 0 };
            for (float t = 0f; t < 0.5f; t += Dt) { s.Queue.Clear(); s.Tick(Dt, 0f, 0f); }
            s.Queue.Clear(); s.Queue.Add(via); s.Stock[(int)broche] = 1;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(2, s.ClientesRicos, "viajante nao e' rico");
            string txt = s.Save(0);
            StringAssert.Contains("\nricos=2\n", txt);
            StringAssert.Contains("\nfam=", txt);
            Sim l = Sim.Load(txt, distrito: Aq);
            Assert.AreEqual(2, l.ClientesRicos);
            StringAssert.Contains(txt.Split('\n').First(x => x.StartsWith("fam=")) + "\n", l.Save(0), "o rodizio de cada familia volta");
            Assert.AreEqual(0, Sim.Load(NovoAq().Save(0), distrito: Aq).ClientesRicos, "sem a linha: zero");
        }

        // ------------------------------------------------------------------ bot (§2.3 regra 4)

        /// <summary>Regra 4: "receita com mais clientes na fila; empate, maior ouro por insumo", refinada (BALANCE.md §37): a falta (fila menos
        /// estoque menos o que as outras bancadas fazem) tem de passar a da atual em Histerese; sem fila, a de maior ouro por insumo.</summary>
        [Test]
        public void Bot_ReceitaComMaisClientesNaFila()
        {
            Sim s = NovoAq();
            Compra(s, "receita_castical");
            Station b = s.Est("bancada_ourives");
            Item fivela = I(s, "fivela"), cinzel = I(s, "cinzel"), castical = I(s, "castical");
            var bot = new Bot { Histerese = 3 };
            void Fila(Item p, int n) { for (int k = 0; k < n; k++) s.Queue.Add(new Client { Want = p, Patience = 999f, MaxPatience = 999f }); }
            Fila(cinzel, 2);
            bot.Step(s, Dt);
            Assert.AreEqual((fivela, (RecipeDef)null), (b.OutItem, b.Proxima), "2 a mais nao paga a troca (histerese 3)");
            s.Queue.Clear(); Fila(castical, 3); Fila(cinzel, 2);
            bot.Step(s, Dt);
            Assert.AreEqual(castical, (b.Proxima ?? b.Recipe).Output, "o castical: 3 na fila");
            s.Queue.Clear();
            s.Stock[(int)castical] = 5;
            for (float t = 0f; t < 5f; t += Dt) { s.Queue.Clear(); bot.Step(s, Dt); }
            Assert.AreEqual(fivela, (b.Proxima ?? b.Recipe).Output, "sem fila e castical no estoque: a de maior ouro por insumo (fivela 330 > cinzel 285)");
            s.Stock[(int)fivela] = 5; s.Stock[(int)castical] = 0;
            for (float t = 0f; t < 5f; t += Dt) { s.Queue.Clear(); bot.Step(s, Dt); }
            Assert.AreEqual(castical, (b.Proxima ?? b.Recipe).Output, "empate cinzel x castical (falta 0): o castical, 290 por insumo contra 285");
            var of = new Sim();
            new Bot().Step(of, Dt);
            Assert.IsFalse(of.TemBancadas);
        }

        /// <summary>Criterio do ticket: o bot F2P, chegando como o jogador, fecha a producao completa do Quarter, vende os 7 produtos, troca de
        /// receita, atende ricos e o fundo do Old Forge e da Row entra a 2.400/min, fora do GoldEarned.</summary>
        [Test]
        public void BotF2P_ChegaAProducaoCompleta()
        {
            Imperio imp = Imperio.Chegada(Aq);
            Sim s = imp.Ativo; var b = new Bot();
            float t = 0f;
            for (; t < 100f * 60f && !s.ProductionComplete; t += Dt) { b.Step(s, Dt); imp.Fundo(Dt); }
            Assert.IsTrue(s.ProductionComplete, "producao completa em 100 min");
            for (int i = 0; i < s.ItemDefs.Length; i++) if (s.IsProduct((Item)i)) Assert.Greater(s.SoldItems[i], 0, s.ItemDefs[i].Id + " vendido");
            Assert.Greater(s.TrocasReceita, 0);
            Assert.Greater(s.ClientesRicos, 0);
            Assert.AreEqual(2400.0 * t / 60.0, imp.FundoGanho, 2400.0 * t / 60.0 * 0.01, "fundo = 40/s");
            Assert.AreEqual(0L, imp.Raiz.GoldEarned + imp.Distrito(Distritos.ArmorersRowId).GoldEarned, "o fundo nao entra no GoldEarned");
        }

        // ------------------------------------------------------------------ encomendas, contratos, obras e Charter

        /// <summary>§7: o rodizio do Quarter (Mixed x2) com as OrderDefs dele (a 1a = a Common, 225 de minimo); a Condessa Amara (4 colares
        /// Fina) e' a 7a Common quando o colar faz Fina; o premio escala com o OrderRewardSeconds do distrito (8,5).</summary>
        [Test]
        public void Encomendas_DoQuarter_RodizioEAmara()
        {
            CollectionAssert.AreEqual(new[] { OrderType.Common, OrderType.Mixed, OrderType.Quality, OrderType.Common, OrderType.Mixed, OrderType.Royal,
                OrderType.Rush, OrderType.Blueprint, OrderType.Common, OrderType.Bulk }, OrderDefs.Ciclo(Aq.Id));
            Sim s = NovoAq();
            s.FirstSaleTime = 0f;
            Ate(s, () => s.Encomendas.Vagas.Count == 1, s.T.OrderFirstDelay + 1f, "a 1a abriu");
            Encomenda e = s.Encomendas.Vagas[0];
            Assert.AreEqual(("ord_common_artisan_quarter", 225L, 8.5f), (e.Def.Id, e.Premio, s.T.OrderRewardSeconds));
            OrderDef amara = Array.Find(OrderDefs.All, d => d.Id == "ord_reg_amara");
            Assert.AreEqual((Aq.Id, "colar", 4, Quality.Fine, "cli_reg_amara"), (amara.District, amara.Item, amara.FixedN, amara.MinQuality, amara.ClientId));
            Assert.IsFalse(s.Encomendas.TipoAberto(OrderType.Mixed), "2 linhas: Mixed pede 3");
            Compra(s, "receita_castical");
            Assert.IsTrue(s.Encomendas.TipoAberto(OrderType.Mixed));
            Assert.IsFalse(s.Encomendas.TipoAberto(OrderType.Quality), "sem Fina");
            Compra(s, "fole_ourives", "nivel_ourivesaria2");
            Assert.IsTrue(s.Encomendas.TipoAberto(OrderType.Quality), "a bancada configuravel conta por toda receita (Fracao)");
        }

        /// <summary>§8 (V28): os contratos do Quarter abrem com ele no build (o castelo sem Renome; o dote da princesa no Renome 6, Fina).</summary>
        [Test]
        public void Contratos_DoQuarter_AbremNoBuild()
        {
            var s = new Sim();
            Compra(s, "escudos", "ferramentas");
            ContractDef obras = Array.Find(ContratosDefs.All, d => d.Id == "ctr_obras_castelo"), dote = Array.Find(ContratosDefs.All, d => d.Id == "ctr_dote_princesa");
            Assert.AreEqual((true, false), (s.Contratos.Aberto(obras), s.Contratos.Aberto(dote)));
            s.Renome.Ganha(1250);
            Assert.IsTrue(s.Contratos.Aberto(dote));
        }

        /// <summary>Golpe de mestre numa peca que o acumulador ja manda Superior (QAcc 99): Masterwork se ha obra pendente na familia.</summary>
        static Quality Golpe(Sim s, Station st)
        {
            st.Out = 0; Array.Clear(st.OutQ, 0, st.OutQ.Length);
            for (int k = 0; k < st.Ins.Length; k++) st.Ins[k] = st.Recipe.Inputs[k].Qty;
            st.QAcc = 99; s.StrikeCooldown = 0f; s.Player.Pos = st.InAt;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Strike(true), "golpe valendo na " + st.Name);
            int mw = s.Masterworks;
            for (int i = 0; i < 4000; i++)
            {
                s.Tick(Dt, 0f, 0f);
                if (s.Masterworks > mw) return Quality.Masterwork;
                if (st.Out > 0) return st.OutQ[(int)Quality.Superior] > 0 ? Quality.Superior : st.OutQ[(int)Quality.Fine] > 0 ? Quality.Fine : Quality.Normal;
            }
            Assert.Fail("a bancada nao fez a peca");
            return Quality.Normal;
        }

        /// <summary>
        /// Criterio do ticket: o Charter do Quarter (producao completa + Renome 6 + 3 obras DO QUARTER) e' alcancavel sem nada pago e sem injetar
        /// conteudo. No Quarter de verdade: producao completa (o projeto de mestre pelo marco), ourivesaria V, a Lucia no nivel 5 e a tecnica da
        /// fila de encomenda #1 (q 60 + 25 + 20 = 105: Superior pelo acumulador); os 3 projetos essenciais pelos marcos de venda; o RichClients
        /// do broche; golpe numa Superior da bancada configuravel em cada receita = as 3 obras no Hall do Quarter; o Charter sai 1x.
        /// </summary>
        [Test]
        public void Charter_DoQuarter_TresObrasPeloCaminhoDeSempre()
        {
            Imperio imp = Imperio.Chegada(Aq);
            Sim s = imp.Ativo;
            Producao(s);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(BlueprintSourceKind.Milestone, s.Projetos.Posses["bp_mestra_artisan"].Origem, "a producao completa da o projeto de mestre");
            Compra(s, "nivel_ourivesaria2", "nivel_ourivesaria3", "nivel_ourivesaria4", "nivel_ourivesaria5");
            int k = K(s, "wk_lucia");
            for (int n = 1; n <= 5; n++) { s.Gold = s.Equipe.Custo(k); Assert.IsTrue(s.Equipe.Comprar(k), "Lucia " + n); }
            Assert.AreEqual("bp_tecnica_ourivesaria", s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, Aq.Id, "enc:7")?.Id);
            Station b = s.Est("bancada_ourives");
            Assert.AreEqual((60, 25, 20, 105), (s.QualityBreakdown(b)[(int)QualitySource.StationLevel], s.QualityBreakdown(b)[(int)QualitySource.Worker],
                s.QualityBreakdown(b)[(int)QualitySource.Blueprint], s.QualityPoints(b)));
            Assert.AreEqual("renome", imp.CharterFalta(Aq.Id));
            imp.Raiz.Renome.Ganha(1250 - imp.Raiz.Renome.Total);
            Assert.AreEqual("masterwork", imp.CharterFalta(Aq.Id));
            s.SoldItems[(int)I(s, "fivela")] = 200; s.SoldItems[(int)I(s, "colar")] = 200; s.SoldItems[(int)I(s, "broche")] = 300;
            s.ClientesRicos = 100;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(new[] { "bp_mw_fivela_viajante", "bp_mw_colar_aurora", "bp_mw_broche_condessa" }.All(s.Projetos.Tem), "os marcos de venda dao os 3 projetos essenciais");
            long renome = imp.Raiz.Renome.Total;
            Assert.AreEqual(Quality.Masterwork, Golpe(s, b), "fivela");
            foreach (string p in new[] { "colar", "broche" })
            {
                Assert.IsTrue(s.Configura(b, I(s, p)), p);
                b.Out = 0;
                Ate(s, () => b.OutItem == I(s, p), 1f, "trocou para " + p);
                Assert.AreEqual(Quality.Masterwork, Golpe(s, b), p);
            }
            Assert.AreEqual((3, true, true, true), (s.Hall.Count, s.Hall.Tem("mw_fivela_viajante"), s.Hall.Tem("mw_colar_aurora"), s.Hall.Tem("mw_broche_condessa")));
            Assert.IsTrue(imp.Raiz.Renome.Total >= renome + 115, "35 + 40 + 40 de Renome das obras");
            Assert.IsNull(imp.CharterFalta(Aq.Id));
            Assert.IsTrue(imp.Charter(Aq.Id));
            Assert.IsFalse(imp.Charter(Aq.Id), "1x");
        }

        /// <summary>As condicoes novas no Hall: RichClients e GemsCut (as gemas que os lapidarios fizeram, Station.Crafted: no save) contam pelo
        /// contador do nucleo, nunca pago; a tecnica da lapidacao da +15 so no colar e na caixa.</summary>
        [Test]
        public void Obras_RicosGemasELapidacao()
        {
            Sim s = NovoAq();
            Compra(s, "veio_ouro", "lapidario", "lapidario2", "receita_caixa");
            MasterworkDef cond = Array.Find(s.Hall.Defs, d => d.Id == "mw_broche_condessa"), lag = Array.Find(s.Hall.Defs, d => d.Id == "mw_colar_lagrimas");
            Assert.AreEqual((CondicaoMw.ClientesRicos, 100, 0L), (cond.Condicoes[0].Kind, cond.Condicoes[0].N, s.Hall.Valor(cond.Condicoes[0])));
            s.ClientesRicos = 40;
            Assert.AreEqual(40L, s.Hall.Valor(cond.Condicoes[0]));
            s.Est("lapidario").Crafted = 600; s.Est("lapidario2").Crafted = 400;
            Assert.AreEqual((CondicaoMw.Feitos, 1000L), (lag.Condicoes[0].Kind, s.Hall.Valor(lag.Condicoes[0])));
            Assert.AreEqual(1000L, Sim.Load(s.Save(0), distrito: Aq).Hall.Valor(lag.Condicoes[0]), "o Crafted das estacoes vai no save");
            Assert.IsTrue(s.Projetos.Ganha("bp_tecnica_lapidacao", BlueprintSourceKind.OrderBlueprint));
            Station b = s.Est("bancada_ourives");
            int q = s.QualityPoints(b);
            Assert.IsTrue(s.Configura(b, I(s, "colar")));
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(q + 15, s.QualityPoints(b), "lapidacao: +15 no colar");
            Assert.AreEqual(0, new Sim().Projetos.TecnicaItem("espada"), "o Old Forge nao tem tecnica de produto");
        }

        // ------------------------------------------------------------------ validator, layout, textos e fotos

        /// <summary>O Quarter real passa no validator do distrito; defeitos injetados acusam: receita liberada por upgrade que nao existe,
        /// receita de fabrica travada, familia pedindo produto de outro distrito, a 1a familia fechada, RichClients onde nao ha rico e GemsCut de
        /// item que nenhuma estacao fixa faz.</summary>
        [Test]
        public void Validator_BancadaEFamilias_DefeitosInjetados()
        {
            CollectionAssert.IsEmpty(ContentValidator.Distrito(Aq).ToList());
            DistrictDef d = Copia(Aq);
            d.Receitas = Aq.Receitas.Select(r => r.Output == (Item)4 ? new RecipeDef(r.Station, r.Family, r.Kind, r.Output, r.TimeMul, r.Inputs) { Abre = "receita_fantasma" }
                : r.Output == (Item)2 ? new RecipeDef(r.Station, r.Family, r.Kind, r.Output, r.TimeMul, r.Inputs) { Abre = "veio_ouro" } : r).ToArray();
            List<string> v = Sim.ValidarBancadas(d, new Sim(distrito: d)).ToList();
            RowTests.Acusa(v, "bancada bancada_ourives: a receita de fabrica (fivela) tem de ser do kit");
            RowTests.Acusa(v, "receita castical liberada por \"receita_fantasma\", que nao e' produtivo do distrito");
            d = Copia(Aq);
            d.Clientes = new[] { Aq.Clientes[3], new ClientDef("guarda", 1, 1f, 1f, Quality.Normal, false, "espada"), Aq.Clientes[0] };
            v = Sim.ValidarFamilias(d, new Sim(distrito: d)).ToList();
            RowTests.Acusa(v, "cliente guarda: pede \"espada\", que nao e' produto com cliente no distrito");
            RowTests.Acusa(v, "a 1a familia tem de chegar desde o kit pedindo fivela");
            Sim row = ContentValidator.Mundo(Distritos.ArmorersRow);
            var obras = new[]
            {
                new MasterworkDef("mw_ricos_na_row", Distritos.ArmorersRowId, "elmo", "bp_mw_elmo_vigia", 30, 1, new CondicaoMasterwork(CondicaoMw.ClientesRicos, null, 10)),
                new MasterworkDef("mw_gemas_na_row", Distritos.ArmorersRowId, "maca", "bp_mw_maca_juiz", 30, 2, new CondicaoMasterwork(CondicaoMw.Feitos, "gema", 10)),
            };
            v = MasterworksDefs.Validar(obras, new MarcoColecaoDef[0], row.Projetos.Defs, Distritos.ArmorersRow.Upgrades, row).ToList();
            RowTests.Acusa(v, "masterwork mw_ricos_na_row: condicao ClientesRicos");
            RowTests.Acusa(v, "masterwork mw_gemas_na_row: condicao Feitos \"gema\"");
        }

        /// <summary>Foto do coordenador (padrao da Row): o rotulo de um deposito nao encosta no da estacao de cima na mesma coluna.</summary>
        [Test]
        public void Layout_RotuloDoDepositoNaoEncostaNoDaEstacaoDeCima()
        {
            foreach (StationDef dep in Aq.Estacoes.Where(x => x.Kind == Kind.Deposit))
                foreach (StationDef p in Aq.Estacoes.Where(x => x.Familia != null && x.Pos.Y > dep.Pos.Y && Math.Abs(x.Pos.X - dep.Pos.X) < 1.5f))
                    Assert.GreaterOrEqual(p.Pos.Y - 1.04f - (dep.Pos.Y + 1.35f), 0.37f, $"{dep.Id} x {p.Id}");
        }

        /// <summary>DM §109: toda chave montada por ID existe para o Quarter (melhoria, item, dica, estacao com o rotulo do distrito, obra,
        /// familia, freguesa, estandarte) e cabe nos 3 idiomas (nome <= 16, dica <= 26, cartao do menu com % e antes/depois <= 16, Hall e
        /// projetos); o painel da bancada configuravel e as condicoes novas no Hall.</summary>
        [Test]
        public void Textos_DoQuarterNosTresIdiomas()
        {
            Sim s = NovoAq();
            try
            {
                Textos.Distrito(Aq);
                var faltam = new List<string>();
                void Tem(string k) { if (!Loc.Tem(k)) faltam.Add(k); }
                foreach (UpgradeDef d in Aq.Upgrades) { Tem("up." + d.Id + ".nome"); Tem("up." + d.Id + ".desc"); }
                foreach (ItemDef d in Aq.Itens) { Tem("item." + d.Id); Tem("dica.balcao." + d.Id); Tem("dica.pegue." + d.Id); }
                foreach (StationDef d in Aq.Estacoes) Tem(Loc.Tem("est." + Aq.Id + "." + d.Id) ? "est." + Aq.Id + "." + d.Id : "est." + d.Id);
                foreach (MasterworkDef d in Aq.Masterworks) Tem("mw." + d.Id);
                foreach (string k in new[] { "distrito.artisan_quarter", "fam.ourivesaria", "cli.cli_reg_amara", "cos.cos_col_estandarte_artisan_quarter", "hall.c.8", "hall.c.9" }) Tem(k);
                CollectionAssert.IsEmpty(faltam);
                Compra(s, "veio_ouro", "lapidario", "receita_castical", "receita_calice", "receita_caixa");
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    for (int u = 0; u < Aq.Upgrades.Length; u++)
                    {
                        Assert.LessOrEqual(Textos.Nome(u).Length, ContentValidator.NameMax, $"{i}: {Textos.Nome(u)}");
                        foreach (string d in new[] { Textos.Placa(u), Textos.Menu(u) }) Assert.LessOrEqual(d.Length, Textos.DicaMax, $"{i}: {d}");
                        if (!Aq.Upgrades[u].InMenu) continue;
                        string b = Textos.Beneficio(s.T, u);
                        StringAssert.Contains("%", b, $"{i}: {b}");
                        Assert.LessOrEqual(b.Length, 2 * 14, $"{i}: {b}");
                        Assert.LessOrEqual(Textos.AntesDepois(s.T, u).Length, 16, $"{i}: {Textos.AntesDepois(s.T, u)}");
                    }
                    for (int it = 0; it < Aq.Itens.Length; it++)
                        foreach (Hint h in new[] { Hint.ProductToCounter, Hint.PickProducts, Hint.ClientWaiting })
                        {
                            string d = Textos.Dica(h, it, s);
                            Assert.LessOrEqual(d.Length, Textos.DicaMax, $"{i}: {d}");
                            StringAssert.DoesNotContain("[", d, $"{i}: chave faltando");
                        }
                    foreach (MasterworkDef d in s.Hall.Defs)
                    {
                        foreach (string t in new[] { Textos.NomeMasterwork(d), Textos.CondicaoMasterwork(s, d), Textos.ProjetoMasterwork(s, d) }) StringAssert.DoesNotContain("[", t, $"{i}: {d.Id}");
                        Assert.LessOrEqual(Textos.NomeMasterwork(d).Length, Textos.HallMax, $"{i}: {d.Id}");
                        Assert.LessOrEqual(Textos.CondicaoMasterwork(s, d).Length, Textos.HallDetalheMax, $"{i}: {Textos.CondicaoMasterwork(s, d)}");
                    }
                    foreach (BlueprintDef d in s.Projetos.Defs) StringAssert.DoesNotContain("[", Textos.ProjetoNome(s, d) + Textos.ProjetoLibera(s, d) + Textos.ProjetoFonte(s, d), $"{i}: {d.Id}");
                    foreach (MarcoColecaoDef m in s.Colecao.Marcos)
                        foreach (string t in new[] { Textos.MarcoColecao(s, m), Textos.PremioColecao(m), Textos.SeloColecao(s, m) })
                        {
                            StringAssert.DoesNotContain("[", t, $"{i}: {m.Id}");
                            Assert.LessOrEqual(t.Length, Textos.PremioMax, $"{i}: {t}");
                        }
                    Station bench = s.Est("bancada_ourives");
                    s.Configura(bench, I(s, "caixa_joias"));
                    foreach (string t in new[] { Textos.Bancada(s, bench), Loc.T("bancada.fazer", Textos.ItemNome((int)Textos.ProximaReceita(s, bench).Output, 1)), Loc.T("bancada.parada", 4) })
                    {
                        StringAssert.DoesNotContain("[", t, $"{i}: {t}");
                        foreach (string linha in t.Split('\n')) Assert.LessOrEqual(linha.Length, 40, $"{i}: {linha}");
                    }
                    s.Configura(bench, I(s, "fivela"));
                    Assert.AreEqual(Loc.T("est." + Aq.Id + ".deposito"), Textos.Estacao(s.Deposit.Index), "o rotulo do distrito no deposito");
                }
                Loc.Atual = Idioma.Pt;
                Assert.AreEqual(("Minério cobre", "Loja fina"), (Textos.Estacao(s.Deposit.Index), Textos.Estacao(s.Counter.Index)));
                Assert.AreEqual("Atenda 100 clientes ricos (0/100)", Textos.CondicaoMasterwork(s, Array.Find(s.Hall.Defs, d => d.Id == "mw_broche_condessa")));
                Assert.AreEqual("Lapide 1.000 gemas (0/1.000)", Textos.CondicaoMasterwork(s, Array.Find(s.Hall.Defs, d => d.Id == "mw_colar_lagrimas")));
                BlueprintDef lap = Array.Find(s.Projetos.Defs, d => d.Id == "bp_tecnica_lapidacao");
                Assert.AreEqual(("Técnica: colares, caixas de joias", "+0,15 de qualidade no produto"), (Textos.ProjetoNome(s, lap), Textos.ProjetoLibera(s, lap)));
                Assert.AreEqual(("Receita: fivela", "Fazer: cinzel"), (Textos.Bancada(s, s.Est("bancada_ourives")), Loc.T("bancada.fazer", Textos.ItemNome((int)Textos.ProximaReceita(s, s.Est("bancada_ourives")).Output, 1))));
                Assert.AreEqual(("Nobres +43%", "Troca de receita 75%"), (Textos.Beneficio(s.T, U(s, "vitrine_luxo")), Textos.Beneficio(s.T, U(s, "troca_rapida"))));
            }
            finally { Textos.Distrito(Distritos.OldForge); Loc.Atual = Idioma.Pt; }
            Assert.AreEqual("Depósito", Textos.Estacao(new Sim().Deposit.Index), "o Old Forge de volta");
        }

        /// <summary>Foto do coordenador (-distrito artisan_quarter -testsession -buy 25 -receitas fivela,colar,caixa_joias): cada bancada pede a
        /// receita da vez (a troca acontece nos primeiros ticks); receita travada ou que a bancada nao tem = o ID de volta (Falha na View).</summary>
        [Test]
        public void Fotos_ReceitasNoQuarter()
        {
            Sim s = Imperio.Chegada(Aq).Ativo;
            Assert.AreEqual("colar", Captura.Receitas(s, "fivela,colar"), "colar travado (sem o lapidario)");
            Producao(s);
            Assert.IsNull(Captura.Receitas(s, "fivela,colar,caixa_joias"));
            for (float t = 0f; t < 5f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((I(s, "fivela"), I(s, "colar"), I(s, "caixa_joias")), (s.Est("bancada_ourives").OutItem, s.Est("bancada_ourives2").OutItem, s.Est("bancada_ourives3").OutItem));
            Assert.AreEqual("espada", Captura.Receitas(s, "espada"));
        }
    }
}
