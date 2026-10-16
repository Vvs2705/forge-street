using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// C-CORE-09 (CONTEUDO_V1 §2.6, §5.6, §7, §8, §10, §14; BALANCE.md §41): a Grand Forge e o Mensageiro. Rotas de 6 un/min (+6 com o cais 2, +2
    /// por nivel do Felix, teto 30), uma unidade a cada 60/capacidade s para a bancada com menos daquele importado; cada uma desconta da renda
    /// de fundo da origem no minuto, nunca abaixo de 0, e so com o jogo aberto. Lote a cada 90 s (60 com o patio 2) com ate 10 pecas (20; +2 por
    /// nivel), 1,25 x a soma dos precos (para cima), venda; vazio nao parte nem conta. A Carta pelo caminho real (5 obras e 3 contratos OU 30
    /// Royal). Nunca fixa indice nem posicao do distrito no mapa (Distritos.Find). NUnit 3.5 do Unity: tipos exatos nas tuplas (long com L).
    /// </summary>
    public class GrandForgeTests
    {
        const float Dt = 1f / 30f;
        static DistrictDef GF => Distritos.Find(Distritos.GrandForgeId);

        static Sim Novo() => new Sim(distrito: GF);
        static int U(Sim s, string id) => Upgrades.IndexOf(id, s.UpgradeDefs);
        static int I(Sim s, string id) => Items.IndexOf(id, s.ItemDefs);
        static void Compra(Sim s, params string[] ids) { foreach (string id in ids) Assert.IsTrue(s.Buy((Upgrade)U(s, id)), id); }
        static void Producao(Sim s) { for (int u = 0; u < s.UpgradeDefs.Length; u++) if (s.IsProduction(u)) s.Buy((Upgrade)u); }
        static int Felix(Sim s) => Array.FindIndex(s.Equipe.Defs, d => d.Papel == WorkerRole.Courier);
        static void SobeFelix(Sim s, int nivel) { int f = Felix(s); while (s.Equipe.Nivel(f) < nivel) { s.Gold = s.Equipe.Custo(f); Assert.IsTrue(s.Equipe.Comprar(f)); } }
        static void Roda(Sim s, float seg) { for (float t = 0f; t < seg - 1e-4f; t += Dt) s.Tick(Dt, 0f, 0f); }
        static long Teto(long a, long b) => (a + b - 1) / b;

        /// <summary>Ferreiro na boca do patio de exportacao com `n` pecas de `item` (nivel `q`): entrega o que o lote aceitar.</summary>
        static void Entrega(Sim s, int item, int n, Quality q = Quality.Normal)
        {
            s.Player.Held[item] = n; s.Player.HeldQ[item][(int)Quality.Fine] = 0; s.Player.HeldQ[item][(int)Quality.Superior] = 0;
            if (q > Quality.Normal) s.Player.HeldQ[item][(int)q] = n;
            s.Player.Pos = s.Exportacao.InAt;
            for (int i = 0; i < 2000 && s.Player.Held[item] > 0 && s.ExportaAceita((Item)item); i++) s.Tick(Dt, 0f, 0f);
        }

        // ------------------------------------------------------------------ dados

        /// <summary>O kit do §2.6 (deposito de cobre, crisol de bronze, fundicao de sinos, balcao), os numeros do Ajuste (BALANCE.md §41), a
        /// abertura, a Carta (Renome 9, 5 obras, 3 contratos), o ultimo do mapa, os importados do §3.1, o Felix e as familias do §12.</summary>
        [Test]
        public void Kit_OsNumerosEOEncadeamento()
        {
            Sim s = Novo();
            CollectionAssert.AreEquivalent(new[] { "deposito", "fornalha", "fundicao_sinos", "balcao" }, s.Stations.Where(x => x.Unlocked).Select(x => x.Id).ToArray());
            Assert.AreEqual((4, 10), (s.QueueCap, s.CounterCap));
            Assert.AreEqual((19600, 24700), (s.PriceOf((Item)I(s, "sino_catedral")), s.PriceOf((Item)I(s, "panoplia_campeao"))));
            Assert.AreEqual((35000L, 3100000L, 970000L), (s.T.Cost(U(s, "fole_grande")), s.T.Cost(U(s, "crisol_bronze3")), s.T.Cost(U(s, "bencao_forja"))));
            Assert.AreEqual(18, Enumerable.Range(0, s.UpgradeDefs.Length).Count(s.IsProduction));
            Assert.AreEqual((12000000L, 4200L, 5, 3), (GF.Abertura, GF.Charter.Renome, GF.Charter.Masterworks, GF.Charter.Contratos));
            int i = Array.IndexOf(Distritos.All, GF);
            Assert.AreEqual((Distritos.All.Length - 1, i), (i, GF.Indice), "o ultimo do mapa; indice = posicao");
            Assert.IsNull(GF.Charter.Proximo, "a Carta da Grande Forja nao libera ninguem");
            Assert.AreEqual(GF.Id, Distritos.All[i - 1].Charter.Proximo, "o anterior do mapa libera a Grand Forge");
            CollectionAssert.AreEqual(new[] { "peitoral", "colar", "caixa_joias", "insignia_real", "espada_gala" }, GF.Importados.Select(x => x.Item).ToArray(), "§3.1, na ordem");
            foreach ((string item, string origem) in GF.Importados)
                Assert.IsTrue(Distritos.Find(origem).Itens.Any(x => x.Id == item && x.Product) && !s.IsProduct((Item)I(s, item)), item + ": produto la, insumo aqui");
            CollectionAssert.AreEqual(new[] { "mercador", "nobre", "emissario", "colecionador" }, GF.Clientes.Select(c => c.Id).ToArray(), "§12");
            Assert.AreEqual((2, Quality.Fine), (GF.Clientes[2].Unidades, GF.Clientes[2].Minima), "emissario: 2 un, Fina");
            Assert.AreEqual(("wk_felix", WorkerRole.Courier, Sim.Mensageiro), (s.Equipe.Defs[Felix(s)].Id, s.Equipe.Defs[Felix(s)].Papel, s.Equipe.Defs[Felix(s)].Contrata));
            Assert.AreEqual("wk_aurelio", Imperio.ManagerDe(GF.Id).Id);
            Assert.AreEqual(GF.Indice, MasterworksDefs.ChartersCoroaSeis, "a Coroa dos Seis pede os Charters de todos os distritos antes da Grand Forge");
            CollectionAssert.IsEmpty(ContentValidator.Distrito(GF).ToList(), "o validator do distrito passa");
        }

        /// <summary>O mensageiro contrata o Felix (nivel 1) e abre o cais e o patio; ele nao carrega (nenhum ajudante a mais) e o trilho da cratera
        /// leva o lingote da fornalha a forja estelar.</summary>
        [Test]
        public void Mensageiro_ContrataOFelixQueNaoCarrega_ETrilhoDaCratera()
        {
            Sim s = Novo();
            Assert.AreEqual((0, 0), (s.NivelMensageiro, s.CapacidadeRotas));
            Assert.IsFalse(s.Cais.Unlocked || s.Exportacao.Unlocked);
            Compra(s, "estrela1", Sim.Mensageiro);
            Assert.AreEqual((1, 1), (s.NivelMensageiro, s.Workers.Count), "o Felix nao vira carregador");
            Assert.IsTrue(s.Cais.Unlocked && s.Exportacao.Unlocked);
            Assert.IsFalse(s.HasConveyor);
            Compra(s, "cratera_estelar");
            Assert.IsTrue(s.HasConveyor);
            s.Est("fornalha_ferro").Out = 2;
            Roda(s, 2.5f);
            Assert.AreEqual(2, s.Est("forja_estelar").Ins[0] + (s.Est("forja_estelar").Busy ? 1 : 0), "o trilho levou os 2 lingotes");
        }

        // ------------------------------------------------------------------ rotas

        /// <summary>Regra 1: 6 un/min com o mensageiro, +6 com o cais 2, +2 por nivel do Felix acima do 1, teto 30.</summary>
        [Test]
        public void Rotas_SeisMaisCaisMaisFelix_Teto30()
        {
            Sim s = Novo();
            Compra(s, Sim.Mensageiro);
            Assert.AreEqual(6, s.CapacidadeRotas);
            Compra(s, Sim.Cais2);
            Assert.AreEqual(12, s.CapacidadeRotas);
            SobeFelix(s, 3);
            Assert.AreEqual(16, s.CapacidadeRotas);
            SobeFelix(s, 10);
            Assert.AreEqual(30, s.CapacidadeRotas, "6 + 6 + 18 = o teto");
            s.T.ImportCap = 20;
            Assert.AreEqual(30, s.CapacidadeRotas, "nunca passa do teto");
        }

        /// <summary>Regra 1: uma unidade a cada 60/capacidade s (10 s com 6/min), entregue na entrada da bancada que tem menos daquele importado;
        /// empate em rodizio pela ordem do §3.1; sem bancada com espaco, o Mensageiro espera (nada importado a toa). ImportedUnits conta.</summary>
        [Test]
        public void Rotas_UmaACadaDezSegundos_ParaQuemTemMenos()
        {
            Sim s = Novo();
            Compra(s, "bancada_relicarios", "bancada_coroas");
            Station rel = s.Est("bancada_relicarios"), cor = s.Est("bancada_coroas");
            cor.Out = cor.OutCap;   // a coroa parada (saida cheia) nao consome o que chega
            int colar = cor.InIndex((Item)I(s, "colar")), ins = cor.InIndex((Item)I(s, "insignia_real")), caixa = rel.InIndex((Item)I(s, "caixa_joias"));
            Roda(s, 30f);
            Assert.AreEqual(0, s.ImportedUnits, "sem o mensageiro, nada chega");
            Compra(s, Sim.Mensageiro);
            Roda(s, 9.9f);
            Assert.AreEqual(0, s.ImportedUnits, "a 1a so depois de 10 s");
            Roda(s, 20.2f);
            Assert.AreEqual((3, 1, 1, 1), (s.ImportedUnits, cor.Ins[colar], cor.Ins[ins], rel.Ins[caixa]), "3 em 30 s, uma de cada rota (empate em rodizio)");
            Roda(s, 10f);
            Assert.AreEqual((1, 2), (cor.Ins[colar], cor.Ins[ins]), "a 4a: o rodizio comeca depois da ultima (a insignia), nao no colar");
            cor.Ins[colar] = 0; cor.Ins[ins] = cor.InCap; rel.Ins[caixa] = 2;
            Roda(s, 10f);
            Assert.AreEqual((1, 2), (cor.Ins[colar], rel.Ins[caixa]), "o colar, que tem menos");
            cor.Ins[colar] = cor.InCap; rel.Ins[caixa] = rel.InCap;
            int antes = s.ImportedUnits;
            Roda(s, 60f);
            Assert.AreEqual(antes, s.ImportedUnits, "tudo cheio: o Mensageiro espera");
            rel.Ins[caixa] = 0;
            Roda(s, Dt);
            Assert.AreEqual((antes + 1, 1), (s.ImportedUnits, rel.Ins[caixa]), "abriu espaco: a que estava esperando chega na hora");
        }

        /// <summary>Regra 2 e §14 (negativo): cada importado desconta PriceOf(item, Normal) x 25% da renda de fundo da origem (o Quarter, no
        /// colar) no minuto; com o custo acima do fundo do minuto, a carteira nunca perde ouro pelo fundo e o que passa e' perdoado (nunca abaixo
        /// de 0).</summary>
        [Test]
        public void Rotas_DescontamDoFundoDaOrigem_NuncaAbaixoDeZero()
        {
            foreach (bool caro in new[] { false, true })
            {
                Imperio imp = Imperio.Chegada(GF);
                Sim s = imp.Ativo, q = imp.Distrito(Distritos.ArtisanQuarterId);
                if (caro) s.T.ImportCustoPct = 100000;
                Compra(s, Sim.Mensageiro, "bancada_coroas");
                Station cor = s.Est("bancada_coroas");
                int colar = cor.InIndex((Item)I(s, "colar"));
                double porMin = imp.FundoPorSegundo(q) * 60.0, custo = q.PriceOf((Item)I(q, "colar"), Quality.Normal) * s.T.ImportCustoPct / 100.0;
                long g0 = imp.FundoGanho;
                for (float t = 0f; t < 59.9f; t += Dt)
                {
                    cor.Ins[colar] = 0;   // so o colar recebe (a insignia esta cheia): toda unidade e' do Quarter
                    cor.Ins[cor.InIndex((Item)I(s, "insignia_real"))] = cor.InCap;
                    s.Tick(Dt, 0f, 0f);
                    long depoisTick = s.Gold;
                    imp.Fundo(Dt);
                    Assert.GreaterOrEqual(s.Gold, depoisTick, "o fundo nunca tira ouro da carteira");
                }
                Assert.AreEqual(5, s.ImportedUnits, "6/min: a 1a aos 10 s, a 5a aos 50 s");
                if (!caro) Assert.AreEqual(5 * custo, imp.FundoDescontado, 1.0, "5 colares x 25% do preco no Quarter");
                else Assert.LessOrEqual(imp.FundoDescontado, porMin + 1.0, "nunca mais que o fundo do minuto");
                Assert.GreaterOrEqual(imp.FundoGanho - g0, 0L);
                if (!caro) continue;
                cor.Ins[colar] = cor.InCap; cor.Out = cor.OutCap;   // as rotas param (a coroa cheia e parada): o que passou do minuto foi perdoado
                double todos = imp.Abertos.Sum(o => imp.FundoPorSegundo(o)) * 60.0;
                long g1 = imp.FundoGanho;
                for (float t = 0f; t < 120f; t += Dt) { s.Tick(Dt, 0f, 0f); imp.Fundo(Dt); }
                Assert.GreaterOrEqual(imp.FundoGanho - g1, 0.85 * 2 * todos, "2 min: o fundo de todos volta (a divida nao passa do minuto)");
            }
        }

        /// <summary>§0.4 e §14 (negativo): o offline nunca importa nem exporta (o cofre paga pela taxa, como sempre; o fundo dos outros pela
        /// taxa deles).</summary>
        [Test]
        public void Offline_NuncaImportaNemExporta()
        {
            Imperio imp = Imperio.Chegada(GF);
            Sim s = imp.Ativo;
            Compra(s, Sim.Mensageiro, "bancada_coroas");
            int sino = I(s, "sino_catedral");
            s.Lote[sino] = 5; s.ExportT = 80f; s.ImportT = 9f; s.RateEma = 500;
            Station cor = s.Est("bancada_coroas");
            Assert.IsTrue(imp.ApplyOffline(7200, 1) > 0, "o cofre pagou");
            Assert.AreEqual((0, 0, 5, 0, 0), (s.ImportedUnits, s.ExportLots, s.Lote[sino], cor.Ins[0], cor.Ins[1]));
            Assert.AreEqual((80f, 9f), (s.ExportT, s.ImportT), "os relogios nao andam fora do jogo");
        }

        // ------------------------------------------------------------------ lote de exportacao

        /// <summary>Regra 3: a cada 90 s o Mensageiro parte com o lote e paga 1,25 x a soma dos PriceOf(peca, nivel), para cima; e' VENDA
        /// (GoldEarned, vendas, Ev.Sold por peca somando o pago, encomenda) e Ev.ExportLot; ExportLots conta; o lote fica vazio.</summary>
        [Test]
        public void Lote_PartePagaUmVirgulaVinteECinco_EEVenda()
        {
            Sim s = Novo();
            Compra(s, Sim.Mensageiro);
            int sino = I(s, "sino_catedral");
            Assert.IsTrue(s.EscolheLote(sino));
            var enc = new Encomenda(OrderDefs.Comum(GF.Id), 0, new[] { (Item)sino }, new[] { 10 }, 6075, 0, 0f, null);
            s.Encomendas.Vagas.Add(enc);
            Entrega(s, sino, 9);
            Entrega(s, sino, 1, Quality.Fine);
            Assert.AreEqual((10, 0), (s.LoteTotal, s.Player.Held[sino]), "o lote leva 10");
            long soma = 9L * s.PriceOf((Item)sino, Quality.Normal) + s.PriceOf((Item)sino, Quality.Fine), esperado = Teto(soma * 125, 100);
            long ganho = s.GoldEarned; int vendas = s.Sales;
            var ev = new List<SimEvent>();
            for (int i = 0; i < 30 * 95 && s.ExportLots == 0; i++) { s.Tick(Dt, 0f, 0f); ev.AddRange(s.Events); }
            Assert.AreEqual((1, esperado, vendas + 10, 0), (s.ExportLots, s.GoldEarned - ganho, s.Sales, s.LoteTotal));
            List<SimEvent> v = ev.FindAll(e => e.Kind == Ev.Sold && e.Pos.X == s.Exportacao.Pos.X && e.Pos.Y == s.Exportacao.Pos.Y);
            Assert.AreEqual((10, esperado), (v.Count, v.Sum(e => (long)e.B)), "uma venda por peca, a soma e' o pago");
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.ExportLot && e.A == 10 && e.B == esperado));
            Assert.AreEqual((10, 1), (enc.Feito[0], s.OrderCount), "a encomenda conta a peca exportada");
        }

        /// <summary>§14 (negativo): lote vazio nao parte nem conta ciclo; com 1 peca, parte com 1.</summary>
        [Test]
        public void Lote_VazioNaoParteNemConta()
        {
            Sim s = Novo();
            Compra(s, Sim.Mensageiro);
            long ganho = s.GoldEarned;
            Roda(s, 3 * 90f + 1f);
            Assert.AreEqual((0, ganho), (s.ExportLots, s.GoldEarned), "3 periodos vazios: nada");
            Entrega(s, I(s, "sino_catedral"), 1);
            Roda(s, 91f);
            Assert.AreEqual(1, s.ExportLots, "com 1 peca, parte");
        }

        /// <summary>Regra 3: lote de 10 a cada 90 s; o patio de exportacao 2: 20 a cada 60 s; +2 pecas por nivel do Felix acima do 1. O produto do
        /// lote: o escolhido (linha aberta) ou o mais estocado no balcao; produto sem linha ou inexistente recusa.</summary>
        [Test]
        public void Lote_TamanhoPeriodoEProduto()
        {
            Sim s = Novo();
            Assert.IsFalse(s.EscolheLote(I(s, "sino_catedral")), "sem patio aberto");
            Compra(s, Sim.Mensageiro);
            Assert.AreEqual((10, 90f), (s.TamanhoLote, s.PeriodoLote));
            Compra(s, Sim.Exportacao2);
            Assert.AreEqual((20, 60f), (s.TamanhoLote, s.PeriodoLote));
            SobeFelix(s, 4);
            Assert.AreEqual(26, s.TamanhoLote);
            int sino = I(s, "sino_catedral"), est = I(s, "estatua_bronze");
            Assert.IsFalse(s.EscolheLote(est), "a estatua ainda nao tem linha");
            Assert.IsFalse(s.EscolheLote(I(s, "colar")), "importado nao e' produto aqui");
            Assert.AreEqual((-1, sino), (s.LoteProduto, s.AlvoLote), "automatico: o unico");
            Compra(s, "cratera_estelar");
            s.Stock[est] = 3; s.Stock[sino] = 2;
            Assert.AreEqual(est, s.AlvoLote, "automatico: o mais estocado no balcao");
            Assert.IsTrue(s.EscolheLote(sino));
            Assert.IsTrue(s.ExportaAceita((Item)sino) && !s.ExportaAceita((Item)est));
            Assert.AreEqual(s.Exportacao, s.Destino((Item)sino));
            Assert.AreEqual(s.Counter, s.Destino((Item)est));
        }

        /// <summary>Regra 3: o carregador de produto leva ao patio de exportacao quando o lote aceita; senao, ao balcao.</summary>
        [Test]
        public void Carregador_LoteSeAceitaSenaoBalcao()
        {
            foreach (bool lote in new[] { true, false })
            {
                Sim s = Novo();
                Compra(s, "estrela1", "estrela2", "estrela3", Sim.Mensageiro);
                int sino = I(s, "sino_catedral");
                if (!lote) s.Lote[sino] = s.TamanhoLote;   // cheio
                s.Est("fundicao_sinos").Out = 3;
                Roda(s, 40f);
                Assert.AreEqual(lote ? 3 : s.TamanhoLote, s.Lote[sino], "pecas no lote");
                Assert.AreEqual(!lote, s.Stock[sino] + s.SoldItems[sino] > 0, "pecas no balcao");
            }
        }

        /// <summary>Linhas opcionais do save: a Grand Forge nova e o Old Forge nao gravam nada das rotas; produto do lote, relogios, contadores e o
        /// lote (com os niveis) vao e voltam; save sem as linhas = padrao; lixo = padrao.</summary>
        [Test]
        public void Save_IdaEVolta_ELinhasOpcionais()
        {
            StringAssert.DoesNotContain("gf", Novo().Save(100));
            StringAssert.DoesNotContain("gf", new Sim().Save(100));
            Sim s = Novo();
            Compra(s, Sim.Mensageiro, "cratera_estelar");
            int sino = I(s, "sino_catedral"), est = I(s, "estatua_bronze");
            s.EscolheLote(est);
            s.Lote[sino] = 3; s.LoteQ[sino][(int)Quality.Superior] = 1; s.Lote[est] = 2;
            s.ImportedUnits = 7; s.ExportLots = 4; s.ImportT = 3.5f; s.ExportT = 61.25f;
            string t = s.Save(100);
            Sim l = Sim.Load(t, distrito: GF);
            Assert.AreEqual((est, 7, 4, 3.5f, 61.25f), (l.LoteProduto, l.ImportedUnits, l.ExportLots, l.ImportT, l.ExportT));
            Assert.AreEqual((3, 1, 2), (l.Lote[sino], l.LoteQ[sino][(int)Quality.Superior], l.Lote[est]));
            Assert.AreEqual(t, l.Save(100), "regrava igual");
            string sem = string.Join("\n", t.Split('\n').Where(x => !x.StartsWith("gf", StringComparison.Ordinal)));
            Sim v = Sim.Load(sem, distrito: GF);
            Assert.AreEqual((-1, 0, 0, 0), (v.LoteProduto, v.ImportedUnits, v.ExportLots, v.LoteTotal), "save sem as linhas: o padrao");
            Sim z = Sim.Load(sem + "gf=colar,x,-5,-3,lixo\ngfl=sino_catedral:999,colar:4\n", distrito: GF);
            Assert.AreEqual((-1, 0f, 0f, 0, 0), (z.LoteProduto, z.ImportT, z.ExportT, z.ImportedUnits, z.ExportLots), "lixo = padrao");
            Assert.AreEqual((z.TamanhoLote, 0), (z.Lote[sino], z.Lote[I(z, "colar")]), "o lote ate o tamanho, so produto");
        }

        // ------------------------------------------------------------------ encomendas e a Carta

        /// <summary>§7.1: o rodizio da Grand Forge comeca pela Royal (Royal x3 e Mixed x2); sem Fina ela vira Common. A Royal Superior abre com o
        /// Renome 9; os contratos da Grand Forge (sinos da catedral, campeao do reino) abrem com o distrito no build.</summary>
        [Test]
        public void Encomendas_RoyalPrimeiro_EContratosNoBuild()
        {
            CollectionAssert.AreEqual(new[] { OrderType.Royal, OrderType.Mixed, OrderType.Bulk, OrderType.Quality, OrderType.Royal, OrderType.Common,
                OrderType.Mixed, OrderType.Blueprint, OrderType.Rush, OrderType.Royal }, OrderDefs.Ciclo(GF.Id));
            Sim s = Novo();
            s.FirstSaleTime = 0f;
            for (float t = 0f; t < s.T.OrderFirstDelay + 1f && s.Encomendas.Vagas.Count == 0; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(OrderType.Common, s.Encomendas.Vagas[0].Tipo, "sem Fina a Royal vira Common");
            Assert.AreEqual(9, RenomeDefs.NivelQueLibera("ord_royal_grand_forge_superior"));
            OrderDef kael = Array.Find(OrderDefs.All, d => d.Id == "ord_reg_kael");
            Assert.AreEqual(("sino_catedral", 6, "cli_reg_kael", GF.Id), (kael.Item, kael.FixedN, kael.ClientId, kael.District));
            Imperio imp = Imperio.Chegada(GF);
            imp.Raiz.Renome.Ganha(GF.Charter.Renome);
            foreach (string id in new[] { "ctr_sinos_catedral", "ctr_campeao" })
                Assert.IsTrue(imp.Raiz.Contratos.Aberto(Array.Find(ContratosDefs.All, d => d.Id == id)), id);
        }

        /// <summary>Golpe de mestre numa peca que o acumulador ja manda Superior (QAcc 99): Masterwork se ha obra pendente.</summary>
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
                if (st.Out > 0) return st.OutQ[(int)Quality.Superior] > 0 ? Quality.Superior : Quality.Normal;
            }
            Assert.Fail("a bancada nao fez a peca");
            return Quality.Normal;
        }

        /// <summary>
        /// A Carta da Grande Forja pelo caminho real: producao completa, niveis V, as tecnicas pela fila de encomenda e a bencao (q 60 + 20 + 25 =
        /// 105: Superior pelo acumulador), os projetos pelos marcos de venda e pela fila Royal, 500 importados e os Charters dos outros distritos;
        /// golpe numa Superior = o sino primeiro, a estatua do fundador, a lamina do cometa, o relicario da luz e a coroa dos seis no Hall. Falta o
        /// contrato: 29 Royal nao bastam, a 30a fecha; ou 3 contratos concluidos. 1x.
        /// </summary>
        [Test]
        public void Carta_PeloCaminhoReal_TresContratosOuTrintaRoyal()
        {
            foreach (bool comContrato in new[] { false, true })
            {
                Imperio imp = Imperio.Chegada(GF);
                Sim s = imp.Ativo;
                Producao(s);
                s.Tick(Dt, 0f, 0f);   // o projeto de mestre (marco da producao)
                Compra(s, "nivel_sintese2", "nivel_sintese3", "nivel_sintese4", "nivel_sintese5", "nivel_fundicao2", "nivel_fundicao3", "nivel_fundicao4",
                    "nivel_fundicao5", "bencao_forja");
                Assert.IsNotNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, GF.Id, "enc:1"));
                Assert.IsNotNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, GF.Id, "enc:2"));
                Assert.AreEqual((105, 105), (s.QualityPoints(s.Est("fundicao_sinos")), s.QualityPoints(s.Est("bancada_coroas"))));
                Assert.AreEqual("renome", imp.CharterFalta(GF.Id));
                imp.Raiz.Renome.Ganha(GF.Charter.Renome - imp.Raiz.Renome.Total);
                Assert.AreEqual("masterwork", imp.CharterFalta(GF.Id));
                s.SoldItems[I(s, "sino_catedral")] = 30; s.SoldItems[I(s, "estatua_bronze")] = 30; s.SoldItems[I(s, "relicario_real")] = 30;
                Assert.AreEqual("bp_mw_lamina_cometa", s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderRoyal, GF.Id, "enc:3")?.Id, "a Royal #1 (o de mestre) ja era posse");
                Assert.AreEqual("bp_mw_coroa_seis", s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderRoyal, GF.Id, "enc:4")?.Id);
                s.ImportedUnits = 500;
                Assert.AreEqual(GF.Indice, imp.Charters.Count, "os Charters dos outros distritos (a chegada)");
                s.Tick(Dt, 0f, 0f);
                foreach (string st in new[] { "fundicao_sinos", "fundicao_estatuas", "forja_sintese", "bancada_relicarios", "bancada_coroas" })
                    Assert.AreEqual(Quality.Masterwork, Golpe(s, s.Est(st)), st);
                Assert.AreEqual(5, s.Hall.Count);
                CollectionAssert.IsSubsetOf(new[] { "mw_sino_primeiro", "mw_estatua_fundador", "mw_lamina_cometa", "mw_relicario_luz", "mw_coroa_seis" },
                    s.Hall.Defs.Where(d => s.Hall.Tem(d.Id)).Select(d => d.Id).ToArray());
                Assert.AreEqual("contrato", imp.CharterFalta(GF.Id));
                if (comContrato)
                {
                    imp.Raiz.Contratos.Concluidos.AddRange(new[] { "ctr_ronda_vila", "ctr_vinte_guardas" });
                    Assert.AreEqual("contrato", imp.CharterFalta(GF.Id), "2 contratos nao bastam");
                    imp.Raiz.Contratos.Concluidos.Add("ctr_milicia_norte");
                }
                else
                {
                    s.RoyalEntregues = 3 * CharterDef.RoyalPorContrato - 1;
                    Assert.AreEqual("contrato", imp.CharterFalta(GF.Id), "29 Royal nao bastam");
                    s.RoyalEntregues++;
                }
                Assert.IsNull(imp.CharterFalta(GF.Id));
                Assert.IsTrue(imp.Charter(GF.Id));
                Assert.IsFalse(imp.Charter(GF.Id), "1x");
            }
        }

        // ------------------------------------------------------------------ validator e layout

        static DistrictDef Copia(Action<DistrictDef> f)
        {
            DistrictDef d = GF;
            var c = new DistrictDef
            {
                Id = d.Id, Nome = d.Nome, Indice = d.Indice, Itens = d.Itens, Receitas = d.Receitas, Upgrades = d.Upgrades, Workers = d.Workers,
                Projetos = d.Projetos, Masterworks = d.Masterworks, MarcosColecao = d.MarcosColecao, Estacoes = d.Estacoes, Pads = d.Pads,
                Obstaculos = d.Obstaculos, Marcos = d.Marcos, Inicio = d.Inicio, Contratacao = d.Contratacao, Largura = d.Largura, Parede = d.Parede,
                EsteiraDe = d.EsteiraDe, EsteiraPara = d.EsteiraPara, Ajuste = d.Ajuste, Abertura = d.Abertura, Charter = d.Charter,
                FundoFator = d.FundoFator, Selo = d.Selo, InsumoPorEspera = d.InsumoPorEspera, Clientes = d.Clientes, Importados = d.Importados,
            };
            f(c);
            return c;
        }

        /// <summary>As regras novas do validator: no maximo 1 cais e 1 patio de exportacao; rota sem cais; importado que nenhuma receita usa, que
        /// e' produto aqui ou que vem de distrito que nao o vende; obra de importados ou lotes e o marco dos lotes onde nao ha cais nem patio;
        /// a Coroa dos Seis pedindo mais Charters do que o build tem; o mensageiro sem upgrade.</summary>
        [Test]
        public void Validator_DefeitosInjetados_Acusa()
        {
            Sim w = ContentValidator.Mundo(GF);
            RowTests.Acusa(Sim.ValidarRotas(Copia(c => c.Estacoes = GF.Estacoes.Append(new StationDef("cais2", Kind.Import, 13.5f, 6.5f, "Cais 2", 0f, 1f)).ToArray()), w).ToList(), "2 cais");
            RowTests.Acusa(Sim.ValidarRotas(Copia(c => c.Estacoes = GF.Estacoes.Where(x => x.Kind != Kind.Import).ToArray()), w).ToList(), "rotas sem cais");
            RowTests.Acusa(Sim.ValidarRotas(Copia(c => c.Importados = new[] { ("lingote", Distritos.ArmorersRowId) }), w).ToList(), "\"lingote\" de \"armorers_row\", que nao e' outro distrito do build que o vende");
            RowTests.Acusa(Sim.ValidarRotas(Copia(c => c.Importados = new[] { ("sino_catedral", Distritos.RoyalArsenalId) }), w).ToList(), "vendido aqui");
            RowTests.Acusa(Sim.ValidarRotas(Copia(c => { c.Itens = GF.Itens.Append(new ItemDef("joia", "joia", 3, false)).ToArray(); c.Importados = new[] { ("joia", Distritos.OldForgeId) }; }), w).ToList(),
                "que nenhuma receita usa");

            Sim row = ContentValidator.Mundo(Distritos.ArmorersRow);
            var mws = new[]
            {
                new MasterworkDef("mw_teste", Distritos.ArmorersRowId, "elmo", "bp_mw_teste", 30, 1, new CondicaoMasterwork(CondicaoMw.Importados, null, 5)),
                new MasterworkDef("mw_teste2", Distritos.ArmorersRowId, "elmo", "bp_mw_teste", 30, 2, new CondicaoMasterwork(CondicaoMw.Lotes, null, 5)),
                new MasterworkDef("mw_teste3", Distritos.ArmorersRowId, "elmo", "bp_mw_teste", 30, 3, new CondicaoMasterwork(CondicaoMw.Charters, null, Distritos.All.Length)),
            };
            var bp = new BlueprintDef("bp_mw_teste", Distritos.ArmorersRowId, BlueprintKind.Masterwork, new[] { "mw_teste", "mw_teste2", "mw_teste3" }, false,
                new BlueprintSource(BlueprintSourceKind.Milestone, BlueprintSource.Lotes, 100));
            List<string> v = MasterworksDefs.Validar(mws, new MarcoColecaoDef[0], new[] { bp }, row.UpgradeDefs, row).ToList();
            RowTests.Acusa(v, "condicao Importados \"\" >= 5: inalcancavel");
            RowTests.Acusa(v, "condicao Lotes \"\" >= 5: inalcancavel");
            RowTests.Acusa(v, $"condicao Charters \"\" >= {Distritos.All.Length}: inalcancavel");
            RowTests.Acusa(ContentValidator.Blueprints(new[] { bp }, row.UpgradeDefs, row).ToList(), "marco \"lotes\" x100 inalcancavel");
            Assert.IsEmpty(ContentValidator.Blueprints(GF.Projetos, GF.Upgrades, w).ToList());

            WorkerDef semUp = new WorkerDef("wk_felix", "Félix", WorkerRole.Courier, Specialty.Carrying, GF.Id, null, "nao_existe", -1, 0, null, new long[] { 150000 }, "mensageiro", "rotas");
            RowTests.Acusa(ContentValidator.Workers(new[] { semUp }, w, new RenomeDef[0]).ToList(), "mensageiro sem upgrade que contrata");
            RowTests.Acusa(Sim.ValidarRotas(Copia(c => { c.Estacoes = GF.Estacoes.Where(x => x.Kind != Kind.Import).ToArray(); c.Importados = null; }), w).ToList(), "Mensageiro (Courier) sem cais");
        }

        /// <summary>O rotulo de cada deposito (1,35 m acima dele) nao encosta no da estacao de cima na mesma coluna (1,04 m abaixo dela; caixa de
        /// 0,37 m), como na Row.</summary>
        [Test]
        public void Layout_RotuloDoDepositoNaoEncosta()
        {
            foreach (StationDef dep in GF.Estacoes.Where(x => x.Kind == Kind.Deposit || x.Kind == Kind.Import))
                foreach (StationDef p in GF.Estacoes.Where(x => x.Familia != null && x.Pos.Y > dep.Pos.Y && Math.Abs(x.Pos.X - dep.Pos.X) < 1.5f))
                    Assert.GreaterOrEqual(p.Pos.Y - 1.04f - (dep.Pos.Y + 1.35f), 0.37f, $"{dep.Id} x {p.Id}");
        }

        // ------------------------------------------------------------------ textos

        /// <summary>DM §109: tudo o que a Grand Forge trouxe tem texto nos 3 idiomas e cabe: melhorias, itens e dicas, estacoes, distrito, obras e
        /// condicoes (importados, lotes, Charters), projetos (o marco dos lotes), marcos da Collection, as linhas das rotas e do lote, o aviso da
        /// partida, o cartao do Felix e o freguês.</summary>
        [Test]
        public void Textos_DaGrandForgeNosTresIdiomas()
        {
            Sim s = Novo();
            try
            {
                Textos.Distrito(GF);
                var faltam = new List<string>();
                void Tem(string k) { if (!Loc.Tem(k)) faltam.Add(k); }
                foreach (UpgradeDef d in GF.Upgrades) { Tem("up." + d.Id + ".nome"); Tem("up." + d.Id + ".desc"); }
                foreach (ItemDef d in GF.Itens) { Tem("item." + d.Id); Tem("dica.balcao." + d.Id); Tem("dica.pegue." + d.Id); }
                foreach (StationDef d in GF.Estacoes) Tem("est." + d.Id);
                foreach (MasterworkDef d in GF.Masterworks) Tem("mw." + d.Id);
                Tem("distrito." + GF.Id); Tem("cli.cli_reg_kael"); Tem("cos.cos_col_estandarte_grand_forge"); Tem("mapa.falta.contrato");
                CollectionAssert.IsEmpty(faltam);
                Sim w = ContentValidator.Mundo(GF);
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    for (int u = 0; u < GF.Upgrades.Length; u++)
                    {
                        Assert.LessOrEqual(Textos.Nome(u).Length, ContentValidator.NameMax, $"{i}: {Textos.Nome(u)}");
                        foreach (string d in new[] { Textos.Placa(u), Textos.Menu(u) }) Assert.LessOrEqual(d.Length, Textos.DicaMax, $"{i}: {d}");
                        if (!GF.Upgrades[u].InMenu) continue;
                        string b = Textos.Beneficio(s.T, u);
                        StringAssert.Contains(GF.Upgrades[u].Id == GF.Selo ? "+0" : "%", b, $"{i}: {b}");
                        Assert.LessOrEqual(b.Length, 2 * 14, $"{i}: {b}");
                        Assert.LessOrEqual(Textos.AntesDepois(s.T, u).Length, 16, $"{i}: {Textos.AntesDepois(s.T, u)}");
                    }
                    for (int it = 0; it < GF.Itens.Length; it++)
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
                    w.ImportedUnits = 99999;
                    foreach (int p in new[] { -1, I(w, "panoplia_campeao"), I(w, "lamina_estelar") })
                    {
                        w.LoteProduto = p; w.Lote[I(w, "sino_catedral")] = 25;
                        foreach (string t in new[] { Textos.Lote(w), Textos.Rotas(w) })
                        {
                            StringAssert.DoesNotContain("[", t, $"{i}");
                            Assert.LessOrEqual(t.Length, 64, $"{i}: {t}");
                        }
                    }
                    Assert.LessOrEqual(Textos.AvisoLote(99999999).Length, Textos.AvisoMax, i.ToString());
                    int f = Felix(w);
                    StringAssert.DoesNotContain("[", Textos.BeneficioWorker(w, f), i.ToString());
                    foreach (ContractDef c in ContratosDefs.All.Where(c => c.District == GF.Id))
                        Assert.LessOrEqual(Textos.ContratoPede(w, c).Length, Textos.PedeMax, $"{i}: {Textos.ContratoPede(w, c)}");
                }
                Loc.Atual = Idioma.Pt;
                Sim c2 = Novo();
                Compra(c2, Sim.Mensageiro);
                Entrega(c2, I(c2, "sino_catedral"), 3);
                StringAssert.StartsWith("Lote: 3/10 auto: sinos da catedral · 1:", Textos.Lote(c2));
                Assert.AreEqual("Rotas: 6/min · 0 chegaram", Textos.Rotas(c2));
                Assert.AreEqual("Receba 500 importados (0/500)", Textos.CondicaoMasterwork(c2, Array.Find(c2.Hall.Defs, d => d.Id == "mw_lamina_cometa")));
                Assert.AreEqual("Envie 200 lotes (0/200)", Textos.CondicaoMasterwork(c2, Array.Find(c2.Hall.Defs, d => d.Id == "mw_panoplia_lenda")));
                Assert.AreEqual("Envie 100 lotes de exportação (0/100)", Textos.ProjetoFonte(c2, Array.Find(c2.Projetos.Defs, d => d.Id == "bp_var_sino_ouro")));
                Assert.AreEqual("Rotas +2/min · lote +2", Textos.BeneficioWorker(c2, Felix(c2)));
                Assert.AreEqual(I(c2, "sino_catedral"), Textos.ProximoLote(c2), "o toque: automatico -> a 1a linha");
                c2.EscolheLote(I(c2, "sino_catedral"));
                Assert.AreEqual(-1, Textos.ProximoLote(c2), "depois da ultima linha: automatico");
            }
            finally { Textos.Distrito(Distritos.OldForge); Loc.Atual = Idioma.Pt; }
        }
    }
}
