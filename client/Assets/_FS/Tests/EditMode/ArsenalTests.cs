using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// C-CORE-07 (CONTEUDO_V1 §2.4, §5.4, §7, §8, §10, §14; BALANCE.md §38): o Royal Arsenal e o patio de cotas. Bloco de 10 (+2 por bloco fechado,
    /// ate 30); a Coroa paga 1,3 x a soma dos precos (para cima) e e' venda; no prazo +25% e +1 de Renome; fora dele, nunca menos que 1,3x; o
    /// offline nunca fecha bloco nem anda o prazo; o carregador leva ao patio quando ha faixa e vaga; a troca vale no proximo bloco; o save vai e
    /// volta. A mesa_real da a 2a vaga de contrato e a Royal do Arsenal nunca vira Common; o Charter pelo caminho real (3 obras e 1 contrato OU 10
    /// Royal). Nunca fixa indice nem posicao do distrito no mapa (Distritos.Find). NUnit 3.5 do Unity: tipos exatos nas tuplas (long com L).
    /// </summary>
    public class ArsenalTests
    {
        const float Dt = 1f / 30f;
        static DistrictDef RA => Distritos.Find(Distritos.RoyalArsenalId);

        static Sim Novo() => new Sim(distrito: RA);
        static int U(Sim s, string id) => Upgrades.IndexOf(id, s.UpgradeDefs);
        static int I(Sim s, string id) => Items.IndexOf(id, s.ItemDefs);
        static void Compra(Sim s, params string[] ids) { foreach (string id in ids) Assert.IsTrue(s.Buy((Upgrade)U(s, id)), id); }
        static void Producao(Sim s) { for (int u = 0; u < s.UpgradeDefs.Length; u++) if (s.IsProduction(u)) s.Buy((Upgrade)u); }

        /// <summary>O ferreiro na boca do patio com `n` pecas de `item` (nivel `q`) entrega tudo; devolve os eventos dos ticks.</summary>
        static List<SimEvent> Entrega(Sim s, int item, int n, Quality q = Quality.Normal)
        {
            s.Player.Held[item] = n; s.Player.HeldQ[item][(int)Quality.Fine] = 0; s.Player.HeldQ[item][(int)Quality.Superior] = 0;
            if (q > Quality.Normal) s.Player.HeldQ[item][(int)q] = n;
            s.Player.Pos = s.Patio.InAt;
            var ev = new List<SimEvent>();
            for (int i = 0; i < 2000 && s.Player.Held[item] > 0; i++) { s.Tick(Dt, 0f, 0f); ev.AddRange(s.Events); }
            for (int i = 0; i < 3; i++) { s.Tick(Dt, 0f, 0f); ev.AddRange(s.Events); }
            Assert.AreEqual(0, s.Player.Held[item], "entregou tudo");
            return ev;
        }

        static long Teto(long a, long b) => (a + b - 1) / b;

        // ------------------------------------------------------------------ dados

        /// <summary>O kit do §2.4 (deposito, carroca de couro, fornalha, forja de laminas, bancada de kits, patio com 1 faixa, balcao de 4), os
        /// numeros do Ajuste (BALANCE.md §38), a abertura, o Charter (Renome 7, 3 obras, 1 contrato) e o encadeamento do mapa.</summary>
        [Test]
        public void Kit_OsNumerosEOEncadeamento()
        {
            Sim s = Novo();
            CollectionAssert.AreEquivalent(new[] { "deposito", "carroca_couro", "fornalha", "forja_laminas", "bancada_kits", "patio_cotas", "balcao" },
                s.Stations.Where(x => x.Unlocked).Select(x => x.Id).ToArray());
            Assert.AreEqual((1, 4, 10), (s.FaixasAbertas, s.QueueCap, s.CounterCap));
            Assert.AreEqual((1990, 5320), (s.PriceOf((Item)I(s, "kit_batedor")), s.PriceOf((Item)I(s, "armadura_gala"))));
            Assert.AreEqual((5830L, 500000L, 110000L), (s.T.Cost(U(s, "fole_real")), s.T.Cost(U(s, "fornalha_real3")), s.T.Cost(U(s, "selo_arsenal"))));
            Assert.AreEqual(18, Enumerable.Range(0, s.UpgradeDefs.Length).Count(s.IsProduction));
            Assert.AreEqual((1300000L, 2000L, 3, 1), (RA.Abertura, RA.Charter.Renome, RA.Charter.Masterworks, RA.Charter.Contratos));
            Assert.IsNull(RA.Charter.Proximo, "o ultimo distrito deste build");
            Assert.IsTrue(Array.Exists(Distritos.All, d => d.Charter?.Proximo == RA.Id), "algum Charter libera o Arsenal");
            Assert.AreEqual(Array.IndexOf(Distritos.All, RA), RA.Indice, "indice = posicao no mapa");
            Assert.AreEqual("wk_leonor", Imperio.ManagerDe(RA.Id).Id);
            CollectionAssert.IsEmpty(ContentValidator.Distrito(RA).ToList(), "o validator do distrito passa");
        }

        // ------------------------------------------------------------------ patio de cotas

        /// <summary>Regra 2: 10 no 1o bloco, +2 a cada bloco fechado do mesmo produto, ate 30.</summary>
        [Test]
        public void Bloco_DezMaisDoisAteTrinta()
        {
            Sim s = Novo();
            int kb = I(s, "kit_batedor");
            foreach ((int fechados, int tam) in new[] { (0, 10), (1, 12), (5, 20), (10, 30), (15, 30) })
            {
                s.CotaBlocos[kb] = fechados;
                Assert.AreEqual(tam, s.TamanhoCota(kb), $"{fechados} fechados");
            }
        }

        /// <summary>Regra 3: o bloco cheio fecha na hora e a Coroa paga 1,3 x a soma dos PriceOf (para cima), +25% no prazo, e e' VENDA (GoldEarned,
        /// vendas, Ev.Sold por peca somando o pago, a encomenda e o contrato contam); +2 de Renome (+1 no prazo); contadores QuotaBlocks e
        /// QuotaOnTime; o proximo bloco do mesmo produto tem 12.</summary>
        [Test]
        public void Bloco_PagaUmVirgulaTresEEVenda()
        {
            Sim s = Novo();
            int kb = I(s, "kit_batedor");
            Assert.IsTrue(s.EscolheCota(0, kb));
            var enc = new Encomenda(OrderDefs.Comum(RA.Id), 0, new[] { (Item)kb }, new[] { 10 }, 675, 0, 0f, null);
            s.Encomendas.Vagas.Add(enc);
            long ouro = s.Gold, ganho = s.GoldEarned, renome = s.Renome.Total;
            List<SimEvent> ev = Entrega(s, kb, 10);
            long esperado = Teto(Teto(10L * 1990 * s.T.CotaPct, 100) * (100 + s.T.CotaBonusPct), 100);   // no prazo
            Assert.AreEqual((esperado + 675, esperado, 1), (s.Gold - ouro, s.GoldEarned - ganho, s.OrderCount), "a venda e a encomenda (fora do GoldEarned)");
            Assert.AreEqual(4L, s.Renome.Total - renome, "+2 do bloco, +1 do prazo e +1 da Common entregue");
            Assert.AreEqual((1, 1, 10, 10), (s.CotaBlocos[kb], s.CotaNoPrazo[kb], s.SoldItems[kb], s.Sales));
            List<SimEvent> vendas = ev.FindAll(e => e.Kind == Ev.Sold);
            Assert.AreEqual((10, esperado), (vendas.Count, vendas.Sum(e => (long)e.B)), "uma venda por peca, a soma e' o pago");
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.QuotaBlock && e.A == kb && e.B == esperado));
            Assert.AreEqual(10, enc.Feito[0], "a encomenda conta a peca vendida a Coroa");
            Assert.AreEqual((kb, 0, 12), (s.Faixas[0].Produto, s.Faixas[0].Pecas, s.Faixas[0].Tamanho), "o proximo bloco do mesmo produto, com 12");
            Assert.IsTrue(s.FirstSaleTime >= 0f, "e' a 1a venda do distrito");
        }

        /// <summary>CONTEUDO_V1 §14 (negativo): o prazo e' so bonus. Fora dele o bloco paga o normal e nunca menos que 1,3 x a soma (o arredondamento
        /// e' para cima, a soma com Fina nao e' multipla de 100); o almoxarife (wk_matias) estica o prazo de 180 para 240 s.</summary>
        [Test]
        public void Bloco_ForaDoPrazo_NuncaMenosQueUmVirgulaTres()
        {
            Sim s = Novo();
            int kb = I(s, "kit_batedor");
            s.EscolheCota(0, kb);
            Entrega(s, kb, 9);
            Entrega(s, kb, 1, Quality.Fine);   // 9 x 1990 + 2985: x1,3 = 27.163,5
            Assert.AreEqual(1, s.CotaBlocos[kb], "o 1o (no prazo) fechou");
            for (int k = 0; k < 11; k++) Entrega(s, kb, 1, k == 0 ? Quality.Fine : Quality.Normal);   // 2o bloco: 12 pecas, a Fina primeiro
            Assert.AreEqual((11, 1), (s.Faixas[0].Pecas, s.CotaBlocos[kb]));
            s.Faixas[0].Decorrido = s.CotaPrazo + 1f;   // passou do prazo
            long ouro = s.GoldEarned, renome = s.Renome.Total;
            long soma2 = 11L * s.PriceOf((Item)kb, Quality.Normal) + s.PriceOf((Item)kb, Quality.Fine);
            Entrega(s, kb, 1);
            long pago = s.GoldEarned - ouro;
            Assert.AreEqual(Teto(soma2 * 130, 100), pago, "fora do prazo: 1,3x para cima");
            Assert.IsTrue(pago * 100 >= soma2 * 130, $"nunca menos que 1,3x ({pago} de {soma2})");
            Assert.AreEqual((2, 1, 2L), (s.CotaBlocos[kb], s.CotaNoPrazo[kb], s.Renome.Total - renome), "fora do prazo: +2 de Renome, sem o bonus");
            Assert.IsTrue(soma2 * 130 % 100 != 0, "a conta nao era redonda (o arredondamento para cima importa)");

            Assert.AreEqual(180f, s.CotaPrazo);
            Compra(s, "patio_faixa2");
            int m = WorkerDefs.IndexOf("wk_matias", s.Equipe.Defs);
            s.Gold = s.Equipe.Custo(m);
            Assert.IsTrue(s.Equipe.Comprar(m));
            Assert.AreEqual(240f, s.CotaPrazo, "com o Matias");
            Entrega(s, kb, 13);
            s.Faixas[0].Decorrido = 200f;   // fora dos 180, dentro dos 240
            ouro = s.GoldEarned;
            Entrega(s, kb, 1);
            Assert.AreEqual((2, Teto(Teto(14L * 1990 * 130, 100) * 125, 100)), (s.CotaNoPrazo[kb], s.GoldEarned - ouro), "no prazo do Matias");
        }

        /// <summary>CONTEUDO_V1 §0.4 e §14 (negativo): o offline nunca fecha bloco nem anda o prazo (o cofre paga pela taxa, como sempre).</summary>
        [Test]
        public void Offline_NuncaFechaBlocoNemAndaOPrazo()
        {
            Sim s = Novo();
            int kb = I(s, "kit_batedor");
            s.EscolheCota(0, kb);
            Entrega(s, kb, 9);
            float t = s.Faixas[0].Decorrido;
            s.RateEma = 500;
            Assert.IsTrue(s.ApplyOffline(7200, 1) > 0, "o cofre pagou");
            Assert.AreEqual((9, 0, t), (s.Faixas[0].Pecas, s.CotaBlocos[kb], s.Faixas[0].Decorrido));
        }

        /// <summary>Regra 2: o carregador de produto leva ao patio quando ha faixa daquele produto com vaga; senao, ao balcao.</summary>
        [Test]
        public void Carregador_PatioSeHaFaixaSenaoBalcao()
        {
            foreach (bool faixa in new[] { true, false })
            {
                Sim s = Novo();
                Compra(s, "cadete1", "cadete2", "cadete3");
                int kb = I(s, "kit_batedor");
                if (faixa) s.EscolheCota(0, kb);
                Assert.AreEqual(faixa ? s.Patio : s.Counter, s.Destino((Item)kb));
                Station bk = s.Est("bancada_kits");
                bk.Out = 3;
                for (int i = 0; i < 30 * 40; i++) s.Tick(Dt, 0f, 0f);
                Assert.AreEqual(faixa, s.Faixas[0].Pecas > 0, "pecas no patio");
                Assert.AreEqual(!faixa, s.Stock[kb] + s.SoldItems[kb] > 0, "pecas no balcao");
            }
        }

        /// <summary>Regra 4: trocar o produto de uma faixa com peca vale no proximo bloco (as pecas ficam e o bloco termina); faixa vazia troca na
        /// hora; o mesmo produto em 2 faixas, faixa fechada e linha fechada recusam.</summary>
        [Test]
        public void Troca_ValeNoProximoBloco()
        {
            Sim s = Novo();
            Compra(s, "bancada_guarda", "patio_faixa2");
            int kb = I(s, "kit_batedor"), kg = I(s, "kit_guarda"), ins = I(s, "insignia_real");
            Assert.IsTrue(s.EscolheCota(0, kb));
            Entrega(s, kb, 3);
            Assert.IsTrue(s.EscolheCota(0, kg));
            Assert.AreEqual((kb, kg, 3), (s.Faixas[0].Produto, s.Faixas[0].Proximo, s.Faixas[0].Pecas), "o bloco em curso segue");
            Assert.IsTrue(s.CotaAceita((Item)kb) && !s.CotaAceita((Item)kg));
            Assert.IsFalse(s.EscolheCota(1, kb), "kit do batedor ainda esta na faixa 1");
            Assert.IsFalse(s.EscolheCota(1, kg), "kit da guarda e' o proximo da faixa 1");
            Assert.IsFalse(s.EscolheCota(2, kb), "a 3a faixa nao foi comprada");
            Assert.IsFalse(s.EscolheCota(1, ins), "a insignia nao tem linha aberta");
            Entrega(s, kb, 7);
            Assert.AreEqual((kg, 0, 10, 1), (s.Faixas[0].Produto, s.Faixas[0].Pecas, s.Faixas[0].Tamanho, s.CotaBlocos[kb]), "fechou e trocou");
            Assert.IsTrue(s.EscolheCota(1, kb));
            Assert.AreEqual((kb, 12), (s.Faixas[1].Produto, s.Faixas[1].Tamanho), "faixa vazia: na hora, com o tamanho de agora");
            Assert.IsTrue(s.EscolheCota(1, -1));
            Assert.IsTrue(s.Faixas[1].Vazia);
        }

        /// <summary>Linhas opcionais do save: o Arsenal novo e o Old Forge nao gravam nada do patio; faixas, contadores e Royal entregues vao e
        /// voltam; save sem as linhas = patio vazio; lixo = padrao.</summary>
        [Test]
        public void Save_IdaEVolta_ELinhasOpcionais()
        {
            StringAssert.DoesNotContain("cota", Novo().Save(100));
            StringAssert.DoesNotContain("cota", new Sim().Save(100));
            Sim s = Novo();
            Compra(s, "bancada_guarda");
            int kb = I(s, "kit_batedor"), kg = I(s, "kit_guarda");
            s.EscolheCota(0, kb);
            Entrega(s, kb, 2, Quality.Fine);
            Entrega(s, kb, 1);
            s.EscolheCota(0, kg);
            s.CotaBlocos[kg] = 2; s.CotaNoPrazo[kg] = 1; s.RoyalEntregues = 4;
            string t = s.Save(100);
            Sim l = Sim.Load(t, distrito: RA);
            FaixaCota f = l.Faixas[0];
            Assert.AreEqual((kb, kg, 3, 10), (f.Produto, f.Proximo, f.Pecas, f.Tamanho));
            Assert.AreEqual((s.Faixas[0].Soma, 2, 1), (f.Soma, f.PorQ[(int)Quality.Fine], f.PorQ[(int)Quality.Normal]));
            Assert.AreEqual((2, 1, 4), (l.CotaBlocos[kg], l.CotaNoPrazo[kg], l.RoyalEntregues));
            Assert.AreEqual(t, l.Save(100), "regrava igual");
            string sem = string.Join("\n", t.Split('\n').Where(x => !x.StartsWith("cota", StringComparison.Ordinal)));
            Sim v = Sim.Load(sem, distrito: RA);
            Assert.IsTrue(v.Faixas.All(x => x.Vazia) && v.CotasFechadas == 0 && v.RoyalEntregues == 0, "save sem as linhas: patio vazio");
            Sim z = Sim.Load(sem + "cota=lixo,::,a:b:c\ncotab=kit_guarda:x\ncotar=-3\n", distrito: RA);
            Assert.IsTrue(z.Faixas.All(x => x.Vazia) && z.CotasFechadas == 0 && z.RoyalEntregues == 0, "lixo = padrao");
        }

        // ------------------------------------------------------------------ transporte, bot e selo

        /// <summary>BALANCE.md §38 (DistrictDef.CargaComDestino): o cadete de insumo nao busca o lingote que ninguem recebe e vai ao ouro que a
        /// mesa do gravador espera (sem a regra, a fornalha cheia ganhava pela pilha).</summary>
        [Test]
        public void Transporte_InsumoSoComDestino()
        {
            Assert.IsTrue(RA.CargaComDestino);
            Assert.IsFalse(Distritos.OldForge.CargaComDestino || Distritos.ArmorersRow.CargaComDestino, "Old Forge e Row: a regra de sempre");
            Sim s = Novo();
            Compra(s, "cadete1", "cadete2", "crisol_real");
            Station forja = s.Est("forja_laminas"), crisol = s.Est("crisol_ouro");
            forja.Ins[0] = forja.InCap; forja.Busy = true;
            s.Est("fornalha").Out = 4; crisol.Out = 1;
            s.Tick(Dt, 0f, 0f);
            Carrier c = s.Workers.Find(w => w.Role == 1);
            Assert.AreEqual(crisol.Index, c.Target, "o ouro, que a mesa espera");
        }

        /// <summary>A regra fixa do bot (Bot.Cotas.cs): as faixas ficam com as linhas de menor procura no balcao (maior intervalo de cliente),
        /// deixando uma linha so para o balcao.</summary>
        [Test]
        public void Bot_FaixasNasLinhasDeMenorProcura()
        {
            Sim s = Novo();
            Producao(s);
            Bot.From(BotProfile.F2P).Step(s, Dt);
            int[] quer = { I(s, "armadura_gala"), I(s, "kit_guarda"), I(s, "espada_gala") };
            CollectionAssert.AreEquivalent(quer, s.Faixas.Select(f => f.Proximo).ToArray());
            Sim um = Novo();
            Bot.From(BotProfile.F2P).Step(um, Dt);
            Assert.IsTrue(um.Faixas[0].Vazia, "com uma linha so, ela fica no balcao");
        }

        /// <summary>§5.4 / §10.2: o selo do Arsenal soma q +25 (Buff) em toda estacao de produto do distrito; o Old Forge nao tem selo.</summary>
        [Test]
        public void Selo_Q25NoDistrito()
        {
            Sim s = Novo();
            Producao(s);
            Station bk = s.Est("bancada_kits"), mesa = s.Est("mesa_gravador");
            Assert.AreEqual(0, s.QualityBreakdown(bk)[(int)QualitySource.Buff]);
            Compra(s, "selo_arsenal");
            Assert.AreEqual((25, 25, 0), (s.QualityBreakdown(bk)[(int)QualitySource.Buff], s.QualityBreakdown(mesa)[(int)QualitySource.Buff],
                s.QualityBreakdown(s.Est("forja_laminas"))[(int)QualitySource.Buff]), "produto sim, insumo nao");
            Assert.IsNull(Distritos.OldForge.Selo);
        }

        // ------------------------------------------------------------------ mesa real, encomendas e Charter

        /// <summary>Regra 6: a mesa_real (major unlock) da a 2a vaga de Royal Contract do Imperio; a peca conta para um contrato so; quando o 1o
        /// fecha, o 2o sobe; as duas vagas voltam do save do Imperio.</summary>
        [Test]
        public void MesaReal_SegundaVagaDeContrato()
        {
            Imperio imp = Imperio.Chegada(RA);
            Sim s = imp.Ativo; Contratos c = imp.Raiz.Contratos;
            imp.Raiz.Renome.Ganha(1300);
            c.Concluidos.AddRange(new[] { "ctr_ronda_vila", "ctr_vinte_guardas", "ctr_milicia_norte", "ctr_obras_castelo", "ctr_dote_princesa" });
            Assert.AreEqual("ctr_guarda_real", c.Oferta?.Id);
            Assert.IsTrue(c.Aceitar());
            Assert.IsNull(c.Oferta, "1 vaga sem a mesa real");
            Producao(s);
            Assert.IsTrue(c.MesaReal);
            Assert.AreEqual("ctr_batedores", c.Oferta?.Id);
            Assert.IsTrue(c.Aceitar());
            Assert.AreEqual(("ctr_guarda_real", "ctr_batedores"), (c.Ativo.Def.Id, c.Ativo2.Def.Id));
            Imperio l = Imperio.Load(imp.Save(100));
            Assert.AreEqual(("ctr_guarda_real", "ctr_batedores", true), (l.Raiz.Contratos.Ativo.Def.Id, l.Raiz.Contratos.Ativo2.Def.Id, l.Raiz.Contratos.MesaReal));
            int kb = I(s, "kit_batedor");
            s.EscolheCota(0, kb);
            Entrega(s, kb, 10);
            Assert.AreEqual((0, 10), (c.Ativo.Feito, c.Ativo2.Feito), "o kit do batedor e' do 2o (a etapa do 1o e' o kit da guarda)");
            c.Ativo.Etapa = c.Ativo.Def.Etapas.Length;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(("ctr_batedores", true), (c.Ativo?.Def.Id, c.Ativo2 == null), "o 1o pagou, o 2o subiu");
            Assert.IsTrue(c.Concluido("ctr_guarda_real"));
        }

        /// <summary>Regra 6 e §7.1: o rodizio do Arsenal comeca pela Royal. Sem Fina e sem a mesa ela vira Common; com a mesa_real, a Royal pede
        /// Normal (nunca vira Common); com Fina na linha, a Royal Fina (a do catalogo). A Royal entregue conta para o Charter do Arsenal, nunca
        /// no distrito sem contrato no Charter.</summary>
        [Test]
        public void Royal_NuncaViraCommonComAMesa_EContaNoCharter()
        {
            Sim s = Novo();
            s.FirstSaleTime = 0f;
            for (float t = 0f; t < s.T.OrderFirstDelay + 1f && s.Encomendas.Vagas.Count == 0; t += Dt) s.Tick(Dt, 0f, 0f);
            Encomenda Primeira() { s.Encomendas.Abertas = 0; return s.Encomendas.Proxima(); }
            Assert.AreEqual(OrderType.Common, Primeira().Tipo, "sem Fina e sem a mesa");
            Compra(s, "patio_faixa2", "mesa_real");
            Encomenda r = Primeira();
            Assert.AreEqual(("ord_royal_royal_arsenal_mesa", Quality.Normal, "bp_mestra_arsenal"), (r.Def.Id, r.Def.MinQuality, r.Blueprint));
            Compra(s, "fornalha_real2", "fornalha_real3", "nivel_kits2");
            Assert.AreEqual("ord_royal_royal_arsenal", Primeira().Def.Id, "com Fina na linha, a Royal do catalogo");

            OrderDef royal = Array.Find(OrderDefs.All, d => d.Id == "ord_royal_royal_arsenal");
            var e = new Encomenda(royal, 3, new[] { (Item)I(s, "kit_batedor") }, new[] { 6 }, 4050, 0, 0f, null);
            e.Feito[0] = 6; s.Encomendas.Vagas.Add(e);
            Assert.IsTrue(s.Encomendas.Entregar(e) > 0);
            Assert.AreEqual(1, s.RoyalEntregues);
            var row = new Sim(distrito: Distritos.ArmorersRow);
            OrderDef rr = Array.Find(OrderDefs.All, d => d.Id == "ord_royal_armorers_row");
            var er = new Encomenda(rr, 9, new[] { (Item)2 }, new[] { 5 }, 450, 0, 0f, null);
            er.Feito[0] = 5; row.Encomendas.Vagas.Add(er);
            row.Encomendas.Entregar(er);
            Assert.AreEqual(0, row.RoyalEntregues, "o Charter da Row nao pede contrato");
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
        /// O Charter do Arsenal pelo caminho real: producao completa, niveis V, as tecnicas pela fila de encomenda e o selo (q 60 + 20 + 25 = 105:
        /// Superior pelo acumulador), os projetos pelos marcos de venda, o Matias no 5 e 20 blocos de kit do batedor; golpe numa Superior = Kit do
        /// Capitao, Kit da Patrulha e Insignia do Leao no Hall. Falta o contrato: 9 Royal nao bastam, a 10a fecha; ou 1 contrato concluido. 1x.
        /// </summary>
        [Test]
        public void Charter_PeloCaminhoReal_ContratoOuDezRoyal()
        {
            foreach (bool comContrato in new[] { false, true })
            {
                Imperio imp = Imperio.Chegada(RA);
                Sim s = imp.Ativo;
                Producao(s);
                s.Tick(Dt, 0f, 0f);   // o projeto de mestre (marco da producao)
                Compra(s, "nivel_kits2", "nivel_kits3", "nivel_kits4", "nivel_kits5", "nivel_cerimonial2", "nivel_cerimonial3", "nivel_cerimonial4",
                    "nivel_cerimonial5", "selo_arsenal");
                Assert.IsNotNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, RA.Id, "enc:1"));
                Assert.IsNotNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, RA.Id, "enc:2"));
                Assert.AreEqual((105, 105), (s.QualityPoints(s.Est("bancada_kits")), s.QualityPoints(s.Est("mesa_gravador"))));
                int m = WorkerDefs.IndexOf("wk_matias", s.Equipe.Defs);
                for (int n = 0; n < 5; n++) { s.Gold = s.Equipe.Custo(m); Assert.IsTrue(s.Equipe.Comprar(m)); }
                Assert.AreEqual("renome", imp.CharterFalta(RA.Id));
                imp.Raiz.Renome.Ganha(2000 - imp.Raiz.Renome.Total);
                Assert.AreEqual("masterwork", imp.CharterFalta(RA.Id));
                s.SoldItems[I(s, "kit_guarda")] = 100; s.SoldItems[I(s, "kit_batedor")] = 300; s.SoldItems[I(s, "insignia_real")] = 300;
                s.CotaBlocos[I(s, "kit_batedor")] = 20;
                s.Tick(Dt, 0f, 0f);
                foreach (string st in new[] { "bancada_guarda", "bancada_kits", "mesa_gravador" }) Assert.AreEqual(Quality.Masterwork, Golpe(s, s.Est(st)), st);
                Assert.AreEqual((3, true, true, true), (s.Hall.Count, s.Hall.Tem("mw_kit_capitao"), s.Hall.Tem("mw_kit_patrulha"), s.Hall.Tem("mw_insignia_leao")));
                Assert.AreEqual("contrato", imp.CharterFalta(RA.Id));
                if (comContrato) imp.Raiz.Contratos.Concluidos.Add("ctr_guarda_real");
                else
                {
                    s.RoyalEntregues = CharterDef.RoyalPorContrato - 1;
                    Assert.AreEqual("contrato", imp.CharterFalta(RA.Id), "9 Royal nao bastam");
                    s.RoyalEntregues++;
                }
                Assert.IsNull(imp.CharterFalta(RA.Id));
                Assert.IsTrue(imp.Charter(RA.Id));
                Assert.IsFalse(imp.Charter(RA.Id), "1x");
            }
        }

        // ------------------------------------------------------------------ validator e layout

        static DistrictDef Copia(Action<DistrictDef> f)
        {
            DistrictDef d = RA;
            var c = new DistrictDef
            {
                Id = d.Id, Nome = d.Nome, Indice = d.Indice, Itens = d.Itens, Receitas = d.Receitas, Upgrades = d.Upgrades, Workers = d.Workers,
                Projetos = d.Projetos, Masterworks = d.Masterworks, MarcosColecao = d.MarcosColecao, Estacoes = d.Estacoes, Pads = d.Pads,
                Obstaculos = d.Obstaculos, Marcos = d.Marcos, Inicio = d.Inicio, Contratacao = d.Contratacao, Largura = d.Largura, Parede = d.Parede,
                EsteiraDe = d.EsteiraDe, EsteiraPara = d.EsteiraPara, Ajuste = d.Ajuste, Abertura = d.Abertura, Charter = d.Charter,
                FundoFator = d.FundoFator, Selo = d.Selo, CargaComDestino = d.CargaComDestino,
            };
            f(c);
            return c;
        }

        /// <summary>As regras novas do validator: no maximo 1 patio; sem o selo, kits e cerimonial nao chegam a Superior (q 80); obra e projeto de
        /// cota so onde ha patio; encomenda que pede upgrade que o distrito nao tem.</summary>
        [Test]
        public void Validator_DefeitosInjetados_Acusa()
        {
            DistrictDef dois = Copia(c => c.Estacoes = RA.Estacoes.Append(new StationDef("patio2", Kind.Quota, 13.5f, 4f, "Pátio 2", 0f, -1f)).ToArray());
            RowTests.Acusa(ContentValidator.Layout(dois).ToList(), "2 patios de cotas");
            List<string> v = ContentValidator.Distrito(Copia(c => c.Selo = null)).ToList();
            RowTests.Acusa(v, "masterwork mw_kit_capitao: Superior inalcancavel");
            RowTests.Acusa(v, "masterwork mw_insignia_leao: Superior inalcancavel");

            Sim row = ContentValidator.Mundo(Distritos.ArmorersRow);
            var mw = new MasterworkDef("mw_teste", Distritos.ArmorersRowId, "elmo", "bp_mw_teste", 30, 1, new CondicaoMasterwork(CondicaoMw.CotaBlocos, "elmo", 5));
            var bp = new BlueprintDef("bp_mw_teste", Distritos.ArmorersRowId, BlueprintKind.Masterwork, new[] { "mw_teste" }, false,
                new BlueprintSource(BlueprintSourceKind.Milestone, BlueprintSource.Cotas, 50));
            RowTests.Acusa(MasterworksDefs.Validar(new[] { mw }, new MarcoColecaoDef[0], new[] { bp }, row.UpgradeDefs, row).ToList(), "condicao CotaBlocos \"elmo\" >= 5: inalcancavel");
            RowTests.Acusa(ContentValidator.Blueprints(new[] { bp }, row.UpgradeDefs, row).ToList(), "marco \"cotas\" x50 inalcancavel");
            Assert.IsEmpty(ContentValidator.Blueprints(RA.Projetos, RA.Upgrades, ContentValidator.Mundo(RA)).ToList());

            var ord = new OrderDef("ord_teste_royal_arsenal", OrderType.Royal, RA.Id, 250f, 6, 50, 35f, 4050, 12, requer: "nao_existe");
            RowTests.Acusa(OrderDefs.Validar(new[] { ord }, new (string, OrderType[])[0], new Sim()).ToList(), "requer \"nao_existe\"");
            Assert.IsEmpty(OrderDefs.Validar(OrderDefs.All, OrderDefs.Ciclos, new Sim()).ToList());
        }

        /// <summary>O rotulo de cada deposito (1,35 m acima dele) nao encosta no da estacao de cima na mesma coluna (1,04 m abaixo dela; caixa de
        /// 0,37 m), como na Row.</summary>
        [Test]
        public void Layout_RotuloDoDepositoNaoEncosta()
        {
            foreach (StationDef dep in RA.Estacoes.Where(x => x.Kind == Kind.Deposit))
                foreach (StationDef p in RA.Estacoes.Where(x => x.Familia != null && x.Pos.Y > dep.Pos.Y && Math.Abs(x.Pos.X - dep.Pos.X) < 1.5f))
                    Assert.GreaterOrEqual(p.Pos.Y - 1.04f - (dep.Pos.Y + 1.35f), 0.37f, $"{dep.Id} x {p.Id}");
        }

        // ------------------------------------------------------------------ textos

        /// <summary>DM §109: tudo o que o Arsenal trouxe tem texto nos 3 idiomas e cabe: melhorias (nome <= 16, placa e menu <= 26, cartao do menu
        /// com % e o do selo com os pontos), itens e dicas, estacoes, distrito, obras e condicoes (cota, prazo, contratos), projetos (o marco dos
        /// blocos), marcos da Collection, as linhas do patio e o freguês.</summary>
        [Test]
        public void Textos_DoArsenalNosTresIdiomas()
        {
            Sim s = Novo();
            try
            {
                Textos.Distrito(RA);
                var faltam = new List<string>();
                void Tem(string k) { if (!Loc.Tem(k)) faltam.Add(k); }
                foreach (UpgradeDef d in RA.Upgrades) { Tem("up." + d.Id + ".nome"); Tem("up." + d.Id + ".desc"); }
                foreach (ItemDef d in RA.Itens) { Tem("item." + d.Id); Tem("dica.balcao." + d.Id); Tem("dica.pegue." + d.Id); }
                foreach (StationDef d in RA.Estacoes) Tem("est." + d.Id);
                foreach (MasterworkDef d in RA.Masterworks) Tem("mw." + d.Id);
                Tem("distrito." + RA.Id); Tem("cli.cli_reg_tiberio"); Tem("cos.cos_col_estandarte_royal_arsenal");
                CollectionAssert.IsEmpty(faltam);
                Sim w = ContentValidator.Mundo(RA);
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    for (int u = 0; u < RA.Upgrades.Length; u++)
                    {
                        Assert.LessOrEqual(Textos.Nome(u).Length, ContentValidator.NameMax, $"{i}: {Textos.Nome(u)}");
                        foreach (string d in new[] { Textos.Placa(u), Textos.Menu(u) }) Assert.LessOrEqual(d.Length, Textos.DicaMax, $"{i}: {d}");
                        if (!RA.Upgrades[u].InMenu) continue;
                        string b = Textos.Beneficio(s.T, u);
                        StringAssert.Contains(RA.Upgrades[u].Id == RA.Selo ? "+0" : "%", b, $"{i}: {b}");
                        Assert.LessOrEqual(b.Length, 2 * 14, $"{i}: {b}");
                        Assert.LessOrEqual(Textos.AntesDepois(s.T, u).Length, 16, $"{i}: {Textos.AntesDepois(s.T, u)}");
                    }
                    for (int it = 0; it < RA.Itens.Length; it++)
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
                    foreach (int p in new[] { I(w, "alabarda_cerimonial"), I(w, "armadura_gala") })
                    {
                        w.Faixas[0].Produto = p; w.Faixas[0].Proximo = I(w, "kit_batedor"); w.Faixas[0].Pecas = 29; w.Faixas[0].Tamanho = 30; w.Faixas[0].Decorrido = 1f;
                        string t = Textos.Faixa(w, 0);
                        StringAssert.DoesNotContain("[", t, $"{i}");
                        Assert.LessOrEqual(t.Length, 64, $"{i}: {t}");
                    }
                    Assert.LessOrEqual(Textos.AvisoCota(99999999, true).Length, Textos.AvisoMax, i.ToString());
                }
                Loc.Atual = Idioma.Pt;
                Sim c = Novo();
                c.EscolheCota(0, I(c, "kit_batedor"));
                Entrega(c, I(c, "kit_batedor"), 3);
                StringAssert.StartsWith("Faixa 1: 3/10 kits do batedor · 2:5", Textos.Faixa(c, 0));
                Assert.AreEqual("Faixa 1: vazia (toque)", Textos.Faixa(Novo(), 0));
                Assert.AreEqual("Feche 20 blocos de kits do batedor (0/20)", Textos.CondicaoMasterwork(c, Array.Find(c.Hall.Defs, d => d.Id == "mw_kit_patrulha")));
                Assert.AreEqual("Cumpra 3 contratos reais (0/3)", Textos.CondicaoMasterwork(c, Array.Find(c.Hall.Defs, d => d.Id == "mw_grande_selo")));
                Assert.AreEqual("Feche 50 blocos de cota (0/50)", Textos.ProjetoFonte(c, Array.Find(c.Projetos.Defs, d => d.Id == "bp_var_estandarte_real")));
                Assert.AreEqual("Qualidade +0,25 no distrito", Textos.Beneficio(c.T, U(c, "selo_arsenal")));
                Assert.AreEqual("Pede: 10 espadas + 10 alabardas + 5 armaduras · Fina+",
                    Textos.ContratoPede(c, Array.Find(ContratosDefs.All, d => d.Id == "ctr_coroacao")), "o pedido longo cai para o nome curto");
                Assert.AreEqual("Pede: 30 kits do batedor + 30 lanças", Textos.ContratoPede(c, Array.Find(ContratosDefs.All, d => d.Id == "ctr_batedores")));
            }
            finally { Textos.Distrito(Distritos.OldForge); Loc.Atual = Idioma.Pt; }
        }
    }
}
