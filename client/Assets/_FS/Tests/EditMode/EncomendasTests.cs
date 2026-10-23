using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>Atalhos das encomendas para os testes (as vagas sao publicas; o Tick e o OnVenda sao do nucleo).</summary>
    public static class EncTeste
    {
        const float Dt = 1f / 30f;

        /// <summary>Uma Common aberta na mao, na proxima posicao do rodizio (como a FASE9 abria: item, alvo, premio).</summary>
        public static Encomenda Poe(Sim s, Item it, int alvo, long premio, int feito = 0)
        {
            var e = new Encomenda(OrderDefs.Comum(OrderDefs.OldForge), s.Encomendas.Abertas++, new[] { it }, new[] { alvo }, premio, 0, 0f, null);
            e.Feito[0] = feito;
            s.Encomendas.Vagas.Add(e);
            return e;
        }

        /// <summary>A principal com tudo vendido (o proximo Tick entrega).</summary>
        public static void Completa(Sim s) => Completa(s.Encomendas.Principal);

        public static void Completa(Encomenda e) { for (int k = 0; k < e.Alvo.Length; k++) e.Feito[k] = e.Alvo[k]; }

        /// <summary>Esvazia as vagas, poe o rodizio na posicao `pos` e roda 1 tick: abre a encomenda daquela posicao (devolve a ultima vaga).</summary>
        public static Encomenda Abre(Sim s, int pos)
        {
            s.Encomendas.Vagas.Clear();
            s.Encomendas.Abertas = pos; s.Encomendas.OrderIn = 0f;
            s.Tick(Dt, 0f, 0f);
            return s.Encomendas.Vagas.Count == 0 ? null : s.Encomendas.Vagas[s.Encomendas.Vagas.Count - 1];
        }

        /// <summary>Vende 1 `it` de nivel `q` no balcao pelo caminho real (cliente na vaga, Sim.Sell): devolve quando vendeu.</summary>
        public static void Vende(Sim s, Item it, Quality q = Quality.Normal)
        {
            s.Stock[(int)it]++;
            if (q > Quality.Normal) s.StockQ[(int)it][(int)q]++;
            s.QueueFor(it).Insert(0, new Client { Want = it, Patience = 99f, MaxPatience = 99f });
            for (int k = 0; k < 60; k++)
            {
                s.Tick(Dt, 0f, 0f);
                if (s.Events.Exists(e => e.Kind == Ev.Sold)) return;
            }
            Assert.Fail("nao vendeu");
        }
    }

    /// <summary>
    /// B-CORE-05 Orders 2.0 (GDD §10, ECONOMIA_WAVE_B §3, §9): 1 teste por tipo (Common, Bulk, Quality, Rush, Mixed, Royal, Blueprint, Event)
    /// e o freguês, rodizio sem sorteio (mesma entrada = mesma sequencia; golden do ciclo), vagas pelo Renome, 1 peca conta para 1 vaga,
    /// premio 1x pelo ID de transacao, save (ida e volta, save do schema 3 de antes vira Common igual, lixo), os 10 primeiros minutos do
    /// bot iguais aos de antes (golden do main) e o validator. NUnit 3.5 do Unity: asserts separados e literais L no que e' long.
    /// </summary>
    public class EncomendasTests
    {
        const float Dt = 1f / 30f;

        /// <summary>Ferramentas compradas (tudo do Orders 2.0 aberto): linhas espada (10), escudo (25), ferramenta (16); ja vendeu; taxa 4/s.</summary>
        static Sim Oficina(params Upgrade[] mais)
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            foreach (Upgrade u in mais) s.Buy(u);
            s.FirstSaleTime = 1f; s.RateEma = 4.0;
            return s;
        }

        static int Count(Sim s, Ev kind) => s.Events.FindAll(e => e.Kind == kind).Count;

        static void Run(Sim s, float seconds) { for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt, 0f, 0f); }

        // ------------------------------------------------------------------ um por tipo

        [Test]
        public void Common_ComoADeAntes_ItemDoRodizio_NPelaRenda_PremioPelaTaxa()
        {
            Sim s = Oficina();
            Encomenda e = EncTeste.Abre(s, 1);
            Assert.AreEqual(OrderType.Common, e.Tipo);
            Assert.AreEqual("ord_common_old_forge", e.Def.Id);
            Assert.AreEqual(Item.Shield, e.Itens[0], "linhas[1 % 3]");
            Assert.AreEqual(8, e.Alvo[0], "150 s x 4/s / (3 linhas x 25)");
            Assert.AreEqual(40L, e.Premio, "10 s x 4/s");
            Assert.AreEqual(Quality.Normal, e.Def.MinQuality);
            long gold = s.Gold, earned = s.GoldEarned, rn = s.Renome.Total;
            EncTeste.Completa(e);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(1, Count(s, Ev.OrderDone));
            Assert.AreEqual(gold + 40L, s.Gold, "premio no ouro");
            Assert.AreEqual(earned, s.GoldEarned, "fora do GoldEarned (como o bau)");
            Assert.AreEqual(rn + 1L, s.Renome.Total, "Common: +1 de Renome");
            Assert.AreEqual(1, s.OrderCount);
        }

        [Test]
        public void Bulk_SoComDuasLinhasE5Entregues_QuantidadeGrande_Mais3DeRenome()
        {
            Sim s = Oficina();
            Assert.AreEqual(OrderType.Common, EncTeste.Abre(s, 2).Tipo, "4 entregues: Bulk travado vira Common na mesma posicao");
            s.OrderCount = 5;
            Encomenda e = EncTeste.Abre(s, 2);
            Assert.AreEqual(OrderType.Bulk, e.Tipo);
            Assert.AreEqual(Item.Tool, e.Itens[0]);
            Assert.AreEqual(33, e.Alvo[0], "400 s x 4 / (3 x 16) = 33,3");
            Assert.AreEqual(90L, e.Premio, "22 s x 4 = 88 -> 90");
            EncTeste.Completa(e);
            long rn = s.Renome.Total;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(rn + 3L, s.Renome.Total);
            Assert.AreEqual(1, s.Encomendas.PorTipo[(int)OrderType.Bulk]);
        }

        [Test]
        public void Quality_SoComLinhaQueFazFina_SoAFinaConta_NPelaFracao()
        {
            Sim s = Oficina();
            Assert.AreEqual(OrderType.Common, EncTeste.Abre(s, 3).Tipo, "sem qualidade em nenhuma linha: Common");
            s.Buy(Upgrade.AnvilsLv2);   // bigornas: q = 15 (1 Fina a cada ~7 espadas)
            Encomenda e = EncTeste.Abre(s, 3);
            Assert.AreEqual(OrderType.Quality, e.Tipo);
            Assert.AreEqual("ord_quality_old_forge", e.Def.Id, "a variante Superior so com a automacao fazendo Superior (q > 100)");
            Assert.AreEqual(Item.Sword, e.Itens[0], "a linha com q > 0");
            Assert.AreEqual(3, e.Alvo[0], "90 s x 4 / 30 x 0,15 = 1,8 -> o minimo 3");
            Assert.AreEqual(50L, e.Premio, "13 s x 4 = 52 -> 50");
            EncTeste.Vende(s, Item.Sword);
            Assert.AreEqual(0, e.Feito[0], "Normal nao conta");
            EncTeste.Vende(s, Item.Sword, Quality.Fine);
            Assert.AreEqual(1, e.Feito[0], "Fina conta");
            EncTeste.Vende(s, Item.Sword, Quality.Superior);
            Assert.AreEqual(2, e.Feito[0], "Superior tambem (ou melhor)");
            s.Renome.Ganha(180);   // Renome 3 libera a variante Superior, mas ninguem faz Superior sem o golpe: fica a Fina
            Assert.AreEqual("ord_quality_old_forge", EncTeste.Abre(s, 13).Def.Id);
        }

        [Test]
        public void Rush_NoPrazoDobraOPremio_ForaDoPrazoPagaONormal_NuncaMenos()
        {
            Sim s = Oficina();
            s.OrderCount = 8;
            Encomenda e = EncTeste.Abre(s, 6);
            Assert.AreEqual(OrderType.Rush, e.Tipo);
            Assert.AreEqual(Item.Sword, e.Itens[0]);
            Assert.AreEqual(40L, e.Premio);
            Assert.AreEqual(40L, e.PremioBonus, "no prazo: o premio dobra");
            Assert.AreEqual(240f, e.Prazo, 0.001f);
            long gold = s.Gold, rn = s.Renome.Total;
            EncTeste.Completa(e);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(gold + 80L, s.Gold, "no prazo: 2x");
            Assert.AreEqual(rn + 2L, s.Renome.Total, "+1 e +1 do prazo");

            Encomenda tarde = EncTeste.Abre(s, 16);
            Assert.AreEqual(OrderType.Rush, tarde.Tipo);
            Run(s, 241f);   // tempo de jogo; nenhuma venda de espada (sem estoque)
            Assert.AreEqual(0f, tarde.Prazo, "o prazo acabou");
            Assert.IsTrue(s.Encomendas.Vagas.Contains(tarde), "nunca falha: continua aberta");
            gold = s.Gold; rn = s.Renome.Total;
            EncTeste.Completa(tarde);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(gold + tarde.Premio, s.Gold, "fora do prazo: o premio normal, nunca menos");
            Assert.AreEqual(rn + 1L, s.Renome.Total);
        }

        [Test]
        public void Mixed_DoisProdutos_TresComQuatroLinhas_CadaUmNoSeu()
        {
            Sim s = Oficina();
            Assert.AreEqual(OrderType.Common, EncTeste.Abre(new Sim { FirstSaleTime = 1f }, 5).Tipo, "1 linha: Mixed travado");
            var cedo = new Sim { FirstSaleTime = 1f, OrderCount = 8 };   // 3 linhas (espada, escudo, joia) e 8 entregues, mas sem as Ferramentas
            cedo.Buy(Upgrade.Shields); cedo.Buy(Upgrade.SideCorridor); cedo.Buy(Upgrade.Jewelry);
            Assert.AreEqual(OrderType.Common, EncTeste.Abre(cedo, 5).Tipo, "antes das Ferramentas: so Common (os 10 primeiros minutos de sempre)");
            Assert.AreEqual(OrderType.Common, EncTeste.Abre(cedo, 2).Tipo, "nem o Bulk");
            Encomenda e = EncTeste.Abre(s, 5);
            Assert.AreEqual(OrderType.Mixed, e.Tipo);
            CollectionAssert.AreEqual(new[] { Item.Tool, Item.Sword }, e.Itens, "as 2 proximas linhas a partir de 5 % 3");
            CollectionAssert.AreEqual(new[] { 6, 9 }, e.Alvo, "70 s x 4 / (3 x 16) = 5,8; 70 x 4 / 30 = 9,3");
            Assert.AreEqual(70L, e.Premio, "18 s x 4 = 72 -> 70");
            EncTeste.Vende(s, Item.Shield);
            Assert.AreEqual(0, e.Progresso, "escudo nao esta no pedido");
            EncTeste.Vende(s, Item.Tool);
            Assert.AreEqual(1, e.Feito[0]);
            Assert.AreEqual(0, e.Feito[1]);
            e.Feito[0] = 6;
            Assert.IsFalse(e.Pronta, "falta a espada");
            e.Feito[1] = 9;
            Assert.IsTrue(e.Pronta);

            Sim q = Oficina(Upgrade.SideCorridor, Upgrade.Jewelry);
            Encomenda e4 = EncTeste.Abre(q, 5);
            Assert.AreEqual(3, e4.Itens.Length, "4 linhas: 3 produtos");
            CollectionAssert.AreEqual(new[] { Item.Shield, Item.Tool, Item.Jewel }, e4.Itens);
        }

        /// <summary>Esvazia a fila `fila` do Projetos (B-CORE-07) pelo proprio gancho das encomendas.</summary>
        static void Esgota(Sim s, BlueprintSourceKind fila)
        {
            for (int k = 0; s.Projetos.ProximoDaFila(fila) != null; k++) Assert.IsNotNull(s.Projetos.GanhaDaFila(fila, OrderDefs.OldForge, "teste:" + k));
        }

        [Test]
        public void Royal_NoRenome2_ComFina_MuitoRenome_EOProjetoDaFilaRoyal()
        {
            Sim s = Oficina(Upgrade.AnvilsLv2);
            s.Renome.Ganha(69);
            Assert.AreEqual(OrderType.Common, EncTeste.Abre(s, 9).Tipo, "Renome 1: Royal travado");
            s.Renome.Ganha(1);
            Encomenda e = EncTeste.Abre(s, 9);
            Assert.AreEqual(OrderType.Royal, e.Tipo);
            Assert.AreEqual(Quality.Fine, e.Def.MinQuality);
            Assert.AreEqual(Item.Sword, e.Itens[0], "a linha com Fina");
            Assert.AreEqual(5, e.Alvo[0], "200 s x 4 / 30 x 0,15 = 4 -> o minimo 5");
            Assert.AreEqual(150L, e.Premio, "32 s x 4 = 128 -> o minimo 150");
            Assert.AreEqual("bp_bancada_mestra", e.Blueprint, "o 1o da fila Royal do Projetos, mostrado no cartao");
            long rn = s.Renome.Total;
            EncTeste.Completa(e);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(rn + 10L, s.Renome.Total);
            Assert.IsTrue(s.Projetos.Tem("bp_bancada_mestra"), "o projeto vai na entrega");
            Assert.AreEqual(e.Tx, s.Projetos.Posses["bp_bancada_mestra"].Tx, "com o ID de transacao da encomenda");
            Assert.AreEqual(BlueprintSourceKind.OrderRoyal, s.Projetos.Posses["bp_bancada_mestra"].Origem);
            Assert.AreEqual("bp_mw_sunblade", EncTeste.Abre(s, 19).Blueprint, "a seguinte mostra o proximo da fila");
            Esgota(s, BlueprintSourceKind.OrderRoyal);
            Assert.IsNull(EncTeste.Abre(s, 29).Blueprint, "fila esgotada: so ouro e Renome");
            Assert.AreEqual(OrderType.Royal, s.Encomendas.Principal.Tipo);
        }

        [Test]
        public void Blueprint_NoRenome1_SoComProjetoPendente_SenaoCommon()
        {
            Sim s = Oficina();
            s.Renome.Ganha(20);
            Encomenda e = EncTeste.Abre(s, 7);
            Assert.AreEqual(OrderType.Blueprint, e.Tipo);
            Assert.AreEqual(Item.Shield, e.Itens[0]);
            Assert.AreEqual(8, e.Alvo[0]);
            Assert.AreEqual(25L, e.Premio, "5 s x 4 = 20 -> o minimo 25");
            Assert.AreEqual("bp_tecnica_espada", e.Blueprint, "o 1o da fila de encomenda do Projetos");
            long rn = s.Renome.Total;
            EncTeste.Completa(e);
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Projetos.Tem("bp_tecnica_espada"));
            Assert.AreEqual(BlueprintSourceKind.OrderBlueprint, s.Projetos.Posses["bp_tecnica_espada"].Origem);
            Assert.AreEqual(rn + 2L, s.Renome.Total);
            Assert.IsNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, OrderDefs.OldForge, e.Tx), "o mesmo ID nao da outro projeto");
            Esgota(s, BlueprintSourceKind.OrderBlueprint);
            Assert.AreEqual(OrderType.Common, EncTeste.Abre(s, 17).Tipo, "fila esgotada: vira Common");
            Assert.AreEqual(OrderType.Common, EncTeste.Abre(Oficina(), 7).Tipo, "Renome 0: travado");
        }

        [Test]
        public void Event_VagaPropria_SoPontosNoLiveOps_SaiQuandoOEventoAcaba()
        {
            Sim s = Oficina();
            s.OrderCount = 10;   // requisito do Forge Frenzy (D-CORE-03)
            EventDef ff = LiveOpsDefs.Eventos.First(x => x.Id == "ev_s1w2_forge_frenzy");
            s.Metas.Agora(ff.Comeca, 0); s.LiveOps.Agora(ff.Comeca);
            Assert.AreSame(ff, s.LiveOps.Evento);
            EncTeste.Abre(s, 1);
            Assert.AreEqual(2, s.Encomendas.Vagas.Count, "a Common e a do evento (vaga propria)");
            Encomenda e = s.Encomendas.Vagas.Find(v => v.Tipo == OrderType.Event);
            Assert.AreEqual(ff.Encomenda, e.Def.Id, "a encomenda do EventDef");
            Assert.AreEqual(OrderType.Common, s.Encomendas.Principal.Tipo, "a principal (anuncios) nunca e' a do evento");
            Assert.AreEqual(0L, e.Premio, "so pontos de evento");
            Assert.AreEqual(1, s.Encomendas.VagasMax, "nao conta nas vagas");
            long gold = s.Gold, rn = s.Renome.Total;
            EncTeste.Completa(e);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(15, s.LiveOps.Pontos, "15 pontos por entrega, na trilha do evento");
            Assert.AreEqual(gold, s.Gold);
            Assert.AreEqual(rn, s.Renome.Total);
            Run(s, 1f);
            Assert.IsTrue(s.Encomendas.Vagas.Exists(v => v.Tipo == OrderType.Event), "a seguinte do evento abre");
            s.LiveOps.Agora(ff.Termina);
            Assert.IsNull(s.LiveOps.Evento);
            s.Tick(Dt, 0f, 0f);
            Assert.IsFalse(s.Encomendas.Vagas.Exists(v => v.Tipo == OrderType.Event), "acabou o evento: a vaga sai sem pagar");
        }

        /// <summary>V27 (BALANCE.md §30.6): a peca vendida conta 1x na vaga normal mais antiga que a aceita e, a parte, 1x na encomenda Event
        /// (a Event nao passa mais fome atras das normais); nunca 2x na mesma categoria.</summary>
        [Test]
        public void Event_ContaAParteDasVagas_AMesmaPecaValeNasDuas()
        {
            Sim s = Oficina();
            s.OrderCount = 10;
            EventDef ff = LiveOpsDefs.Eventos.First(x => x.Id == "ev_s1w2_forge_frenzy");
            s.Metas.Agora(ff.Comeca, 0); s.LiveOps.Agora(ff.Comeca);
            EncTeste.Abre(s, 1);
            Encomenda c = s.Encomendas.Principal, e = s.Encomendas.Vagas.Find(v => v.Tipo == OrderType.Event);
            Item it = e.Itens[0];
            c.Itens[0] = it;   // as duas pedindo o mesmo item (o rodizio escolhe o da Common a parte)
            int fc = c.Feito.Sum(), fe = e.Feito.Sum();
            s.Encomendas.OnVenda(it, Quality.Normal);
            Assert.AreEqual((fc + 1, fe + 1), (c.Feito.Sum(), e.Feito.Sum()), "a mesma peca na Common e na Event");
        }

        [Test]
        public void Fregues_ACada7aCommon_OProximoQueDa_ComCoracao()
        {
            Sim s = Oficina();
            s.Encomendas.Comuns = 5;
            Assert.IsNull(EncTeste.Abre(s, 4).Def.ClientId, "a 6a Common");
            Encomenda e = EncTeste.Abre(s, 8);
            Assert.AreEqual("ord_reg_grunk", e.Def.Id, "a 7a: Orbo (ferramenta Fina) e Lirien (joia) ainda nao dao; Grunk da");
            Assert.AreEqual(OrderType.Bulk, e.Tipo, "o tipo do freguês");
            Assert.AreEqual(Item.Sword, e.Itens[0]);
            Assert.AreEqual(40, e.Alvo[0], "N fixo");
            Assert.AreEqual(3, s.Encomendas.Fregueses, "o rodizio dos fregueses segue do Grunk");
            EncTeste.Completa(e);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(1, s.Encomendas.Coracoes["cli_reg_grunk"]);
            Assert.AreEqual(1, Sim.Load(s.Save(1000)).Encomendas.Coracoes["cli_reg_grunk"], "coracoes no save");
            var cedo = new Sim { FirstSaleTime = 1f };
            cedo.Encomendas.Comuns = 6;
            Assert.IsNull(EncTeste.Abre(cedo, 4).Def.ClientId, "antes das Ferramentas: nada de freguês");
        }

        // ------------------------------------------------------------------ rodizio, vagas, peca, premio 1x

        /// <summary>Golden do ciclo do Old Forge com tudo aberto e o tipo travado virando Common; mesma entrada = mesma sequencia.</summary>
        [Test]
        public void Rodizio_SemSorteio_GoldenDoCiclo_MesmaEntradaMesmaSequencia()
        {
            CollectionAssert.AreEqual(new[] { OrderType.Common, OrderType.Common, OrderType.Bulk, OrderType.Quality, OrderType.Common, OrderType.Mixed, OrderType.Rush,
                OrderType.Blueprint, OrderType.Common, OrderType.Royal }, OrderDefs.Ciclo(OrderDefs.OldForge), "ECONOMIA_WAVE_B §3.2");
            string Seq(Sim s)
            {
                var t = new List<string>();
                for (int pos = 0; pos < 20; pos++)
                {
                    Encomenda e = EncTeste.Abre(s, pos);
                    t.Add(e.Tipo + ":" + string.Join("+", e.Itens) + ":" + string.Join("+", e.Alvo) + ":" + e.Premio);
                }
                return string.Join(" ", t);
            }
            Sim Tudo()
            {
                Sim s = Oficina(Upgrade.AnvilsLv2);
                s.OrderCount = 8; s.Renome.Ganha(70);
                return s;
            }
            string a = Seq(Tudo());
            Assert.AreEqual(a, Seq(Tudo()), "deterministico");
            StringAssert.StartsWith("Common:Sword:5:40 Common:Shield:8:40 Bulk:Tool:33:90 Quality:Sword:3:50 Common:Shield:8:40 Mixed:Tool+Sword:6+9:70 " +
                "Rush:Sword:20:40 Blueprint:Shield:8:25 Common:Tool:12:40 Royal:Sword:5:150 Common:Shield:8:40", a, "a 1a de todas: 5 (OrderFirstTarget)");

            // o bot: dois jogos iguais abrem a mesma sequencia de encomendas
            var b1 = new Sim(); var b2 = new Sim(); var x = new Bot(); var y = new Bot();
            var l1 = new List<string>(); var l2 = new List<string>();
            for (int k = 0; k < 40 * 60 * 30; k++)
            {
                x.Step(b1, Dt); y.Step(b2, Dt);
                foreach (SimEvent e in b1.Events) if (e.Kind == Ev.OrderNew) l1.Add(k + ":" + e.A + ":" + e.B);
                foreach (SimEvent e in b2.Events) if (e.Kind == Ev.OrderNew) l2.Add(k + ":" + e.A + ":" + e.B);
            }
            Assert.Greater(l1.Count, 10);
            CollectionAssert.AreEqual(l1, l2);
        }

        [Test]
        public void Vagas_UmaDuasNoRenome1TresNoRenome3_SoDepoisDasFerramentas()
        {
            var cedo = new Sim();
            cedo.Renome.Ganha(999);
            Assert.AreEqual(1, cedo.Encomendas.VagasMax, "antes das Ferramentas: 1 (os 10 primeiros minutos de sempre)");
            Sim s = Oficina();
            Assert.AreEqual(1, s.Encomendas.VagasMax);
            s.Renome.Ganha(20);
            Assert.AreEqual(2, s.Encomendas.VagasMax, "Renome 1");
            s.Renome.Ganha(110);
            Assert.AreEqual(2, s.Encomendas.VagasMax, "Renome 2");
            s.Renome.Ganha(50);
            Assert.AreEqual(3, s.Encomendas.VagasMax, "Renome 3");
            // enchem pelo relogio: a 1a na hora, cada outra OrderGap s depois; cheias, nada abre
            s.Encomendas.OrderIn = 0f;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(1, s.Encomendas.Vagas.Count);
            Run(s, s.T.OrderGap - 1f);
            Assert.AreEqual(1, s.Encomendas.Vagas.Count, "a 2a so OrderGap depois");
            Run(s, 1.5f);
            Assert.AreEqual(2, s.Encomendas.Vagas.Count);
            Run(s, s.T.OrderGap + 1f);
            Assert.AreEqual(3, s.Encomendas.Vagas.Count);
            Run(s, 60f);
            Assert.AreEqual(3, s.Encomendas.Vagas.Count, "cheias");
            Assert.AreEqual(3, s.Encomendas.Vagas.Select(v => v.Itens[0]).Distinct().Count(), "produto que ja esta numa vaga: a proxima linha");
        }

        [Test]
        public void CadaPeca_ContaParaUmaVagaSo_AMaisAntigaQueAceita()
        {
            Sim s = Oficina(Upgrade.AnvilsLv2);
            Encomenda velha = EncTeste.Poe(s, Item.Sword, 5, 40);
            var fina = new Encomenda(Array.Find(OrderDefs.All, d => d.Id == "ord_quality_old_forge"), s.Encomendas.Abertas++, new[] { Item.Sword }, new[] { 3 }, 50, 0, 0f, null);
            s.Encomendas.Vagas.Add(fina);
            EncTeste.Vende(s, Item.Sword, Quality.Fine);
            Assert.AreEqual(1, velha.Feito[0], "a Fina vai para a mais antiga que aceita");
            Assert.AreEqual(0, fina.Feito[0], "so uma");
            velha.Feito[0] = 4;
            EncTeste.Vende(s, Item.Sword);
            Assert.AreEqual(5, velha.Feito[0]);
            EncTeste.Vende(s, Item.Sword);
            Assert.AreEqual(0, fina.Feito[0], "Normal nao serve a Quality (a Common ja foi entregue)");
            EncTeste.Vende(s, Item.Sword, Quality.Fine);
            Assert.AreEqual(1, fina.Feito[0]);
        }

        [Test]
        public void Premio_IdDeTransacao_Entregar2xPaga1x_NemDepoisDeSaveLoad()
        {
            Sim s = Oficina();
            Encomenda e = EncTeste.Poe(s, Item.Sword, 5, 100);
            Assert.AreEqual("enc:0", e.Tx);
            Assert.AreEqual(-1L, s.Encomendas.Entregar(e), "incompleta: nada");
            EncTeste.Completa(e);
            long gold = s.Gold;
            Assert.AreEqual(100L, s.Encomendas.Entregar(e));
            Assert.AreEqual(-1L, s.Encomendas.Entregar(e), "a mesma encomenda de novo: nada");
            Assert.AreEqual(gold + 100L, s.Gold);
            Assert.AreEqual(100L, s.OrderGold);
            Assert.AreEqual(1, s.OrderCount);
            Sim b = Sim.Load(s.Save(1000));
            Run(b, 5f);
            Assert.AreEqual(s.Gold, b.Gold, "reabrir: a entregue nao volta nem paga");
            Assert.AreEqual(1, b.OrderCount);
        }

        // ------------------------------------------------------------------ save

        [Test]
        public void Save_IdaEVolta_TresVagasComMixedRushBonusEProjeto()
        {
            Sim s = Oficina(Upgrade.AnvilsLv2);
            s.Renome.Ganha(180); s.OrderCount = 8;
            s.Encomendas.Vagas.Clear();
            s.Encomendas.Abertas = 5; s.Encomendas.OrderIn = 0f;
            s.Tick(Dt, 0f, 0f);                     // Mixed
            Run(s, s.T.OrderGap + 0.5f);            // Rush
            s.Encomendas.Abertas = 9;
            Run(s, s.T.OrderGap + 0.5f);            // Royal com projeto
            Assert.AreEqual("Mixed Rush Royal", string.Join(" ", s.Encomendas.Vagas.Select(v => v.Tipo)));
            Assert.AreEqual(Item.Shield, s.Encomendas.Vagas[1].Itens[0], "Rush (6 % 3 = espada): a espada ja esta no Mixed, vai a proxima linha");
            s.Encomendas.Vagas[0].Feito[1] = 2; s.Encomendas.Vagas[0].Premio += 35;
            s.Encomendas.Vagas[1].Prazo = 123.5f;
            s.Encomendas.Coracoes["cli_reg_bento"] = 2;
            s.Encomendas.Pulos = 4;
            string text = s.Save(1000);
            StringAssert.Contains("\nenc=10,", text);
            StringAssert.Contains("\nenc.v=ord_mixed_old_forge,5,105,0,0,-,ferramenta:6:0,espada:9:2\n", text);
            StringAssert.Contains(",bp_bancada_mestra,espada:", text);
            Sim b = Sim.Load(text);
            Assert.AreEqual(text, b.Save(1000), "save estavel");
            Assert.AreEqual(3, b.Encomendas.Vagas.Count);
            Assert.AreEqual(105L, b.Encomendas.Vagas[0].Premio, "o bonus do anuncio volta");
            Assert.AreEqual(123.5f, b.Encomendas.Vagas[1].Prazo, 0.001f);
            Assert.AreEqual("bp_bancada_mestra", b.Encomendas.Vagas[2].Blueprint);
            Assert.AreEqual(10, b.Encomendas.Abertas);
            Assert.AreEqual(4, b.Encomendas.Pulos);
            Assert.AreEqual(2, b.Encomendas.Coracoes["cli_reg_bento"]);
            StringAssert.Contains("\nord=-1,0,0,0,8,", text, "espelho da 0.8: a principal e' Mixed (2 itens), fica de fora");
        }

        /// <summary>Save do schema 3 de antes (main com o E-PLAT-02, gerado pelo codigo daquela versao): 1 entregue, 1 trocada, a aberta (12
        /// espadas, premio 25, 5 vendidas) vira uma Common igual na posicao 2, e o rodizio segue dali.</summary>
        public const string SaveV3ComEncomenda =
            "v=3\nt=146.336\ngold=30\nups=fole:67.03,bigorna2:115.93\nup=11000000000000000000000000000\nupp=\nst1=1,0,1,0.48,14\nst2=0,0,0,0,0\nst3=0,0,0,0,10\n" +
            "st4=0,1,1,0.447,3\nst5=0,0,0,0,0\nst6=0,0,0,0,0\nst8=0,0,0,0,0\nstocks=\nsolds=espada:12\nholds=\nstocksq=\nholdsq=\nqst3=0,0,0,0\nqst4=0,0,0,0\n" +
            "qst5=0,0,0,0\nqst6=0,0,0,0\nqst8=0,0,0,0\ngolpe=0\nstock=0,0,0,0\nsold=12,0,0,0\nms=0,0,0,0\nema=0.77829\nsaved=1700000000\nclaim=0\n" +
            "m=22.7,12,120,0,0,8,4,54.5,2\npx=0.60,5.97\nhold=0,0,0,0,0,0\nvip=190.531,0,-1,0,0,0,0\nvipc=0\nord=2,12,5,25,1,0,1\nbst=1,0,0\n" +
            "ad.encomenda_troca=2,1,0\nrn=1,0\n";

        [Test]
        public void SaveDoSchema3_AEncomendaDeAntesViraCommonIgual_ORodizioSegue()
        {
            Sim s = Sim.Load(SaveV3ComEncomenda);
            Encomenda e = s.Encomendas.Principal;
            Assert.IsNotNull(e);
            Assert.AreEqual("ord_common_old_forge", e.Def.Id);
            Assert.AreEqual(2, e.Idx, "entregues + trocadas");
            Assert.AreEqual(Item.Sword, e.Itens[0]);
            Assert.AreEqual(12, e.Alvo[0]);
            Assert.AreEqual(5, e.Feito[0]);
            Assert.AreEqual(25L, e.Premio);
            Assert.AreEqual(3, s.Encomendas.Abertas);
            Assert.AreEqual(1, s.Encomendas.Pulos);
            Assert.AreEqual(1, s.OrderCount);
            Assert.AreEqual(1L, s.Renome.Total, "rn= do save");
            Assert.IsTrue(s.Anuncios.Pode(Anuncio.EncomendaTroca), "a chave do anuncio segue a mesma conta (Idx + 1 = 3 > 2, a paga)");
            string novo = s.Save(1700000100);
            StringAssert.Contains("\nord=2,12,5,25,1,0,1\n", novo, "o espelho da 0.8 sai igual");
            StringAssert.Contains("\nenc=3,3,0,0\n", novo);
            Assert.AreEqual(novo, Sim.Load(novo).Save(1700000100));
            // o rodizio continua da posicao 3 (sem as Ferramentas: Common de espada, a unica linha)
            EncTeste.Completa(e);
            s.Tick(Dt, 0f, 0f);
            s.Encomendas.OrderIn = 0f;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(3, s.Encomendas.Principal.Idx);
            // saves antigos de verdade, sem ord= nenhum: do zero
            Assert.AreEqual(0, Sim.Load(CoreTests.SaveV01).Encomendas.Vagas.Count);
            Assert.AreEqual(0, Sim.Load(CoreTests.SaveV03).Encomendas.Abertas);
        }

        [Test]
        public void Save_Lixo_IdDesconhecido_LinhaFechada_ViramOPadrao()
        {
            string baseTxt = new Sim().Save(10);
            Sim g = Sim.Load(baseTxt + "enc=x,-3\nenc.v=ord_nao_existe,1,10,0,0,-,espada:5:1\nenc.v=ord_common_old_forge,1,10,0,0,-,lanca:5:1\n" +
                "enc.v=ord_common_old_forge,abc,-10,0,x,-,joia:5:1\nenc.v=ord_common_old_forge,2,10,0,0,-,espada:9999:-4\nenc.cor=cli_x:-3,lixo\n");
            Assert.AreEqual(1, g.Encomendas.Vagas.Count, "so a de espada (joia sem joalheria nao volta)");
            Encomenda e = g.Encomendas.Vagas[0];
            Assert.AreEqual(OrderDefs.Comum(OrderDefs.OldForge).TargetMax, e.Alvo[0], "alvo no teto do OrderDef");
            Assert.AreEqual(0, e.Feito[0], "progresso negativo vira 0");
            Assert.AreEqual(3, g.Encomendas.Abertas, "o contador nunca fica atras de uma vaga aberta");
            Assert.AreEqual(2, g.UnknownIds, "OrderDef e item desconhecidos (save_ids_desconhecidos)");
            Assert.AreEqual(0, g.Encomendas.Coracoes["cli_x"]);
        }

        /// <summary>Os primeiros minutos do bot F2P (ate comprar as Ferramentas, 10:30) iguais ao main antes do Orders 2.0, tick a tick: o golden
        /// foi gerado pelo codigo de antes (mesmos eventos de encomenda e compra no mesmo tick, mesmo ouro, vendas e Renome no fim).</summary>
        [Test]
        public void PrimeirosDezMinutos_IguaisAosDeAntes_GoldenDoMain()
        {
            const string golden = "2011:4:0:50 2031:16:2:5 3400:17:2:25 3478:4:1:65 4000:16:2:12 5301:4:2:85 6790:17:2:25 6886:4:3:110 7390:16:2:9 8946:4:4:145 " +
                "10928:17:2:25 11124:4:5:185 11528:16:3:8 12563:4:6:240 13196:4:7:315 14259:17:3:25 14859:16:2:30 15588:4:8:410 18895:4:9:530";
            var s = new Sim(); var bot = new Bot();
            var ev = new List<string>();
            int tick = 0;
            while (!s.Bought[(int)Upgrade.Tools] && tick < 30000)
            {
                bot.Step(s, Dt); tick++;
                foreach (SimEvent e in s.Events)
                    if (e.Kind == Ev.OrderNew || e.Kind == Ev.OrderDone || e.Kind == Ev.Bought) ev.Add(tick + ":" + (int)e.Kind + ":" + e.A + ":" + e.B);
                Assert.LessOrEqual(s.Encomendas.Vagas.Count, 1, "1 vaga so");
                Assert.IsTrue(s.Encomendas.Vagas.TrueForAll(v => v.Tipo == OrderType.Common && v.Def.ClientId == null), "so Common");
            }
            Assert.AreEqual(18895, tick);
            Assert.AreEqual(golden, string.Join(" ", ev));
            Assert.AreEqual(5L, s.Gold);
            Assert.AreEqual(1730L, s.GoldEarned);
            Assert.AreEqual(116, s.Sales);
            Assert.AreEqual(4, s.OrderCount);
            Assert.AreEqual(100L, s.OrderGold);
            Assert.AreEqual(14L, s.Renome.Total);
            Assert.AreEqual("7.30,5.67", s.Player.Pos.ToString());
        }

        /// <summary>O bot F2P aceita e entrega os tipos novos sozinho (as vagas abrem pelo rodizio): em 100 min entrega Common, Bulk, Quality,
        /// Rush, Mixed, Royal e Blueprint (a fila do Projetos), e com 2 vagas entrega mais que antes.</summary>
        [Test]
        public void Bot_F2P_EntregaOsTiposNovos()
        {
            var s = new Sim(); var bot = new Bot();
            for (int k = 0; k < 100 * 60 * 30; k++) bot.Step(s, Dt);   // o Royal abre no Renome 2 (~60 min) e a 1a sai aos ~95
            foreach (OrderType t in new[] { OrderType.Common, OrderType.Bulk, OrderType.Quality, OrderType.Rush, OrderType.Mixed, OrderType.Royal, OrderType.Blueprint })
                Assert.Greater(s.Encomendas.PorTipo[(int)t], 0, t.ToString());
            Assert.GreaterOrEqual(s.OrderCount, 25, "ECONOMIA_WAVE_B §10: >= 25 em 60 min");
            long colecao = s.Colecao.Marcos.Where(m => s.Colecao.Pago(m)).Sum(m => (long)m.Renome)   // B-CORE-08: marcos da Collection e obras do Hall
                + s.Hall.Defs.Where(d => s.Hall.Tem(d.Id)).Sum(d => (long)d.Renome);
            Assert.AreEqual(s.Encomendas.RenomeGanho + 5L * s.Chests.Count(c => c.State == 2) + s.Renome.Superiores / RenomeDefs.SuperiorACada + colecao, s.Renome.Total,
                "o Renome vem das encomendas (pelo tipo), dos baus e da Collection");
        }

        // ------------------------------------------------------------------ validator

        [Test]
        public void Validator_ConteudoReal_Limpo_EDefeitosInjetados_Acusa()
        {
            Sim w = ContentValidator.World();
            Assert.IsEmpty(OrderDefs.Validar(OrderDefs.All, OrderDefs.Ciclos, w).ToList());
            OrderDef[] d =
            {
                new OrderDef("ord_reg_brutus", OrderType.Bulk, OrderDefs.OldForge, 0f, 30, 30, 22f, 60, 3, item: "lanca", fixedN: 30, clientId: "cli_reg_brutus"),
                new OrderDef("ord_quality_mw", OrderType.Quality, OrderDefs.OldForge, 90f, 3, 20, 13f, 40, 3, minQuality: Quality.Masterwork),
                new OrderDef("ord_rush_pune", OrderType.Rush, OrderDefs.OldForge, 150f, 3, 30, 10f, 25, 1, bonusRewardSeconds: 10f),
                new OrderDef("Ord-X", OrderType.Common, OrderDefs.OldForge, 150f, 3, 30, 10f, 25, 1),
                new OrderDef("ord_mixed_1", OrderType.Mixed, OrderDefs.OldForge, 70f, 2, 15, 18f, 60, 3, minLines: 3),
                new OrderDef("ord_ev_ouro", OrderType.Event, null, 150f, 3, 30, 10f, 25, 0, eventId: "ev_x", eventPoints: 20),
                new OrderDef("ord_bp_sem_fila", OrderType.Blueprint, OrderDefs.OldForge, 150f, 3, 30, 5f, 25, 2),
                new OrderDef("ord_fila_x", OrderType.Royal, OrderDefs.OldForge, 200f, 5, 40, 32f, 150, 10, blueprintQueue: "loja"),
                new OrderDef("ord_bulk_bairro_fantasma", OrderType.Bulk, "bairro_fantasma", 400f, 8, 80, 32f, 540, 3),
                new OrderDef("ord_n_invertido", OrderType.Common, OrderDefs.OldForge, 150f, 30, 3, 10f, 22, 1),
                new OrderDef("ord_n_invertido", OrderType.Common, OrderDefs.OldForge, 150f, 3, 30, 10f, 25, 1),
            };
            var v = ContentValidator.Validate(orders: d);
            void Acusa(string trecho) => Assert.IsTrue(v.Exists(m => m.Contains(trecho)), $"esperava \"{trecho}\" em:\n{string.Join("\n", v)}");
            Acusa("encomenda ord_reg_brutus: item \"lanca\" que nao existe no jogo");
            Acusa("encomenda ord_quality_mw: qualidade minima Masterwork");
            Acusa("encomenda ord_rush_pune: bonus sem prazo");
            Acusa("encomenda Ord-X: ID fora do formato");
            Acusa("encomenda ord_mixed_1: 1 produtos");
            Acusa("encomenda ord_ev_ouro: Event da so pontos");
            Acusa("encomenda ord_bp_sem_fila: Blueprint sem fila");
            Acusa("encomenda ord_fila_x: fila de Blueprint \"loja\"");
            Acusa("encomenda ord_bulk_bairro_fantasma: distrito \"bairro_fantasma\" que nao existe");   // C-CORE-06: o Quarter entrou
            Acusa("encomenda ord_n_invertido: N 30-3");
            Acusa("encomenda ord_n_invertido: premio 10 s; 22");
            Acusa("encomenda ord_n_invertido: ID repetido");
            Acusa("ciclo old_forge: Bulk sem OrderDef no distrito");
            var semCommon = OrderDefs.Validar(OrderDefs.All, new[] { (OrderDefs.OldForge, new[] { OrderType.Bulk, OrderType.Event }) }, w).ToList();
            Assert.IsTrue(semCommon.Exists(m => m.Contains("sem Common")), string.Join("\n", semCommon));
            Assert.IsTrue(semCommon.Exists(m => m.Contains("Event no ciclo")), string.Join("\n", semCommon));
        }
            [Test]
        public void TodasAsLinhasOcupadas_NaoRepeteOCartaoDeUmItemSo()
        {
            // coordenador: com uma Mixed nas 3 outras linhas, a Common nova repetia a espada da outra Common ("Venda 30 espadas" 2x na tela)
            var s = new Sim();
            for (int pass = 0; pass < 2; pass++) for (int i = 0; i < Math.Min(37, Upgrades.Count); i++) s.Buy((Upgrade)i);
            s.Renome.Ganha(180);
            for (float t = 0f; t < 780f; t += 1f / 30f)
            {
                s.Tick(1f / 30f, 0f, 0f);
                var unicos = s.Encomendas.Vagas.FindAll(v => v.Itens.Length == 1 && v.Def.ClientId == null && v.Def.Item == null);
                for (int a = 0; a < unicos.Count; a++) for (int b = a + 1; b < unicos.Count; b++)
                    Assert.AreNotEqual(unicos[a].Itens[0], unicos[b].Itens[0], $"t={t:0}: {unicos[a].Idx} e {unicos[b].Idx}");
            }
        }
}
}
