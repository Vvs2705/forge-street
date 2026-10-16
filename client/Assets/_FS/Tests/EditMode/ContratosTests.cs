using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// D-CORE-04 Royal Contracts (GDD §11, §23 #7; CONTEUDO_V1 §8; V25): os 12 em dados, oferta deterministica (Ferramentas, Renome da tabela,
    /// distrito do build, 1 vaga, flag), progresso por etapa pelas vendas reais (so o item da etapa, qualidade minima), conclusao paga 1x pelo
    /// ID ctr:{id} (ouro da taxa fora do GoldEarned, Renome, Blueprint pela fonte Contract), prazo so de bonus, abandono sem penalidade, save
    /// (ida e volta, antigo, lixo, ID desconhecido), relogio para tras, missao de evento contract_complete, o bot F2P e o validator com defeitos
    /// injetados. NUnit 3.5 do Unity: tuplas de tipo exato e literais L no que e' long.
    /// </summary>
    public class ContratosTests
    {
        const float Dt = 1f / 30f;
        const long Agora = 1800000000L;

        /// <summary>Ferramentas compradas, ja vendeu, taxa 4/s e Renome 3 (180): o ronda da vila esta aberto.</summary>
        static Sim Oficina(BlueprintDef[] bps = null)
        {
            var s = new Sim(null, null, null, bps);
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            s.FirstSaleTime = 1f; s.RateEma = 4.0;
            s.Renome.Ganha(180);
            return s;
        }

        static int Count(Sim s, Ev kind) => s.Events.Count(e => e.Kind == kind);
        static void Run(Sim s, float seconds) { for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt, 0f, 0f); }
        static ContractDef Def(string id, string distrito, Quality q, float bonus, int ouro, int renome, string bp, params (string, int)[] pede) =>
            new ContractDef(id, distrito, pede.Select(p => new ContractStage(p.Item1, p.Item2, q)).ToArray(), bonus, ouro, renome, bp);
        /// <summary>O bp_var_ronda do catalogo (imperio, Wave C) injetado no Old Forge com a fonte Contract: o gancho do Projetos ligado.</summary>
        static BlueprintDef[] ComRonda() => new List<BlueprintDef>(ProjetosDefs.All)
        {
            new BlueprintDef("bp_var_ronda", ProjetosDefs.OldForge, BlueprintKind.Variant, new[] { "espada" }, false, new BlueprintSource(BlueprintSourceKind.Contract, "ctr_ronda_vila")),
        }.ToArray();
        static void Acusa(List<string> v, string trecho) => Assert.IsTrue(v.Exists(m => m.Contains(trecho)), $"esperava \"{trecho}\" em:\n{string.Join("\n", v)}");

        [Test]
        public void Catalogo_12_IdsUnicos_SoORondaNoOldForge_RequisitoPelaTabelaDoRenome_ValidatorLimpo()
        {
            ContractDef[] a = ContratosDefs.All;
            Assert.AreEqual(12, a.Length);
            Assert.AreEqual(12, a.Select(d => d.Id).Distinct().Count());
            Assert.IsTrue(a.All(d => d.Id.StartsWith("ctr_") && d.OuroSeg > 0 && d.Reputacao >= 15 && d.Reputacao <= 40 && d.Blueprint.StartsWith("bp_var_") && d.BonusMin > 0f));
            Assert.IsTrue(a.All(d => Array.IndexOf(ContratosDefs.DistritosCatalogo, d.District) >= 0));
            Assert.AreEqual("ctr_ronda_vila", a[0].Id);
            Assert.AreEqual(OrderDefs.OldForge, a[0].District);
            Assert.AreEqual(60, a[0].Total, "30 espadas + 20 escudos + 10 ferramentas");
            Assert.AreEqual(1, a.Count(d => d.District == OrderDefs.OldForge), "so o 1o e' do Old Forge; os outros esperam a Wave C");
            CollectionAssert.AreEqual(new[] { 3, -1, 5, -1, 6, -1, -1, 7, -1, 8, -1, 9 }, a.Select(d => d.Renome).ToArray(), "requisito = o nivel que lista o ID (RenomeDefs)");
            Assert.IsTrue(a.All(d => d.Etapas.All(e => e.MinQ <= Quality.Fine)), "Normal ou Fina (o golpe nunca e' obrigatorio)");
            Assert.IsEmpty(ContratosDefs.Validar(a, ProjetosDefs.All, RenomeDefs.Niveis, Upgrades.All, ContentValidator.World()).ToList());
            Assert.IsEmpty(ContentValidator.Validate());
        }

        [Test]
        public void Oferta_SemSorteio_SoDepoisDasFerramentasENoRenome3_UmaVaga_FlagDesligaAOferta()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields);
            s.Renome.Ganha(180);
            Assert.AreEqual("ctr_ronda_vila", s.Contratos.Proximo.Id, "o 1o da lista, aberto ou nao");
            Assert.IsNull(s.Contratos.Oferta, "antes das Ferramentas nada abre (GDD §3.3)");
            s.Buy(Upgrade.Tools);
            Assert.AreEqual("ctr_ronda_vila", s.Contratos.Oferta.Id);
            var b = new Sim();
            b.Buy(Upgrade.Shields); b.Buy(Upgrade.Tools); b.Renome.Ganha(179);
            Assert.IsNull(b.Contratos.Oferta, "Renome 2: o ronda pede o 3 (RenomeDefs)");
            b.Renome.Ganha(1);
            Assert.AreEqual("ctr_ronda_vila", b.Contratos.Oferta.Id);
            s.T.Contratos = false;
            Assert.IsNull(s.Contratos.Oferta, "flag desligada: nada novo");
            Assert.IsFalse(s.Contratos.Aceitar());
            s.T.Contratos = true;
            Assert.IsFalse(s.Contratos.Abandonar(), "nada ativo");
            Assert.IsTrue(s.Contratos.Aceitar());
            Assert.AreEqual(1, s.Contratos.Aceitos);
            Assert.AreEqual("ctr_ronda_vila", s.Contratos.Ativo.Def.Id);
            Assert.AreEqual(90f * 60f, s.Contratos.Ativo.Prazo, "prazo-bonus de 90 min de jogo");
            Assert.AreEqual("ctr_vinte_guardas", s.Contratos.Proximo.Id, "o proximo da lista fixa");
            Assert.IsNull(s.Contratos.Oferta, "1 por vez");
            Assert.IsFalse(s.Contratos.Aceitar());
            Assert.IsTrue(s.Contratos.Aberto(s.Contratos.Proximo), "C-CORE-05: a Rua dos Armeiros entrou no build (o elmo e' dela): o vinte_guardas abre");
            Assert.IsFalse(s.Contratos.Forca("ctr_batedores"), "vaga ocupada nem forcado");
            s.Contratos.Abandonar();
            Assert.IsTrue(s.Contratos.Forca("ctr_batedores"), "dev: forca um fora do build (so para a foto)");
            Assert.AreEqual("ctr_batedores", s.Contratos.Ativo.Def.Id);
            Sim d = Oficina();   // dois abertos no mesmo distrito: a vaga e' uma so
            d.Contratos.Defs = new[] { Def("ctr_a", OrderDefs.OldForge, Quality.Normal, 0f, 10, 15, null, ("espada", 1)), Def("ctr_b", OrderDefs.OldForge, Quality.Normal, 0f, 10, 15, null, ("escudo", 1)) };
            Assert.IsTrue(d.Contratos.Aceitar());
            Assert.AreEqual("ctr_b", d.Contratos.Proximo.Id);
            Assert.IsTrue(d.Contratos.Aberto(d.Contratos.Proximo));
            Assert.IsNull(d.Contratos.Oferta, "1 por vez mesmo com o proximo aberto (Contratos.Vagas = 1)");
        }

        [Test]
        public void Progresso_PorEtapa_SoOItemDaEtapaAtual_CadaPecaConta1_EtapaFechaNoN()
        {
            Sim s = Oficina();
            s.Contratos.Aceitar();
            Contrato c = s.Contratos.Ativo;
            EncTeste.Vende(s, Item.Shield);
            Assert.AreEqual((0, 0), (c.Etapa, c.Feito), "etapa 1 pede espada: escudo nao conta");
            EncTeste.Vende(s, Item.Sword);
            Assert.AreEqual((0, 1), (c.Etapa, c.Feito));
            EncTeste.Vende(s, Item.Sword, Quality.Fine);
            Assert.AreEqual((0, 2), (c.Etapa, c.Feito), "Fina conta numa etapa Normal");
            c.Feito = 29;
            EncTeste.Vende(s, Item.Sword);
            Assert.AreEqual((1, 0), (c.Etapa, c.Feito), "a 30a espada fecha a etapa");
            Assert.AreEqual(30, c.Progresso);
            EncTeste.Vende(s, Item.Sword);
            Assert.AreEqual((1, 0), (c.Etapa, c.Feito), "etapa 2 pede escudo: espada nao conta mais");
            EncTeste.Vende(s, Item.Shield);
            Assert.AreEqual((1, 1), (c.Etapa, c.Feito));
            Assert.AreEqual(31, c.Progresso);
            Assert.IsFalse(c.Pronto);
            Assert.AreEqual(0, Count(s, Ev.ContractDone));
        }

        [Test]
        public void QualidadeMinima_SoFinaOuMelhorConta_UltimaPecaConcluiNoMesmoTick()
        {
            Sim s = Oficina();
            s.Contratos.Defs = new[] { Def("ctr_fina", OrderDefs.OldForge, Quality.Fine, 0f, 1, 15, null, ("espada", 2)) };
            Assert.IsTrue(s.Contratos.Aceitar(), "sem nivel na tabela do Renome: abre com as Ferramentas");
            Contrato c = s.Contratos.Ativo;
            EncTeste.Vende(s, Item.Sword);
            Assert.AreEqual(0, c.Feito, "Normal nao conta numa etapa Fina");
            EncTeste.Vende(s, Item.Sword, Quality.Fine);
            Assert.AreEqual(1, c.Feito);
            long rn = s.Renome.Total;
            EncTeste.Vende(s, Item.Sword, Quality.Superior);
            Assert.IsTrue(c.Pronto);
            Assert.IsNull(s.Contratos.Ativo, "a venda que fecha a ultima etapa conclui no mesmo tick");
            Assert.AreEqual(1, Count(s, Ev.ContractDone));
            Assert.AreEqual(25L, s.Contratos.OuroGanho, "1 s da taxa = o piso das encomendas (OrderRewardMin); prazo 0 = sem bonus");
            Assert.AreEqual(rn + 15L, s.Renome.Total);
            CollectionAssert.AreEqual(new[] { "ctr_fina" }, s.Contratos.Concluidos);
        }

        [Test]
        public void Conclusao_PagaOuroDaTaxaForaDoGoldEarned_RenomeEBlueprint_1xPorTx_NemDepoisDeSaveLoad()
        {
            BlueprintDef[] bps = ComRonda();
            Sim s = Oficina(bps);
            s.Metas.Agora(Agora, 0);   // o ledger vai no save
            Assert.IsEmpty(ContratosDefs.Validar(ContratosDefs.All, bps, RenomeDefs.Niveis, Upgrades.All, ContentValidator.World()).ToList(), "o bp_var_ronda com a fonte Contract passa");
            s.Contratos.Aceitar();
            Contrato c = s.Contratos.Ativo;
            Assert.AreEqual("ctr:ctr_ronda_vila", c.Tx);
            long gold = s.Gold, earned = s.GoldEarned, rn = s.Renome.Total;
            c.Etapa = 3;   // tudo entregue: a conclusao e' do Tick
            s.Tick(Dt, 0f, 0f);
            Assert.IsNull(s.Contratos.Ativo);
            Assert.AreEqual(1, Count(s, Ev.ContractDone));
            Assert.AreEqual(gold + 1800L, s.Gold, "300 s x 4/s x 1,5 (no prazo) = 1.800");
            Assert.AreEqual(earned, s.GoldEarned, "fora do GoldEarned (como o bau e a encomenda)");
            Assert.AreEqual(rn + 20L, s.Renome.Total, "+15 e +5 do prazo");
            Assert.AreEqual(1800L, s.Contratos.OuroGanho);
            CollectionAssert.AreEqual(new[] { "ctr_ronda_vila" }, s.Contratos.Concluidos);
            Assert.IsTrue(s.Metas.Ledger.Contains("ctr:ctr_ronda_vila"), "ID de transacao no ledger das metas");
            Assert.IsTrue(s.Projetos.Tem("bp_var_ronda"));
            Assert.AreEqual(BlueprintSourceKind.Contract, s.Projetos.Posses["bp_var_ronda"].Origem);
            Assert.AreEqual("ctr:ctr_ronda_vila", s.Projetos.Posses["bp_var_ronda"].Tx);
            s.Metas.Cont.TryGetValue("contract_complete", out long n);
            Assert.AreEqual(1L, n, "gatilho das metas");
            Assert.AreEqual("ctr_vinte_guardas", s.Contratos.Proximo.Id, "concluido sai da lista");
            Assert.IsFalse(s.Contratos.Forca("ctr_ronda_vila"), "concluido nao volta nem forcado");
            Assert.AreEqual(-1L, s.Metas.Paga("ctr:ctr_ronda_vila", 300, 0, 15), "o mesmo ID nunca paga 2x");
            Sim b = Sim.Load(s.Save(Agora + 100), null, null, null, null, bps);
            Run(b, 5f);
            Assert.AreEqual(s.Gold, b.Gold, "reabrir nao paga de novo");
            CollectionAssert.AreEqual(new[] { "ctr_ronda_vila" }, b.Contratos.Concluidos);
            Assert.IsNull(b.Contratos.Ativo);
            Assert.AreEqual("ctr_vinte_guardas", b.Contratos.Proximo.Id);
            Assert.AreEqual(-1L, b.Metas.Paga("ctr:ctr_ronda_vila", 300, 0, 15));
            Assert.IsTrue(b.Projetos.Tem("bp_var_ronda"));
            Assert.AreEqual(0, Count(b, Ev.ContractDone));
        }

        [Test]
        public void SemOBlueprintNoBuild_PagaOuroERenome_EOGanchoFicaNoOp()
        {
            Sim s = Oficina();   // ProjetosDefs.All de hoje: sem bp_var_ronda (Wave C)
            s.Contratos.Aceitar();
            s.Contratos.Ativo.Etapa = 3;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(1800L, s.Contratos.OuroGanho);
            Assert.IsFalse(s.Projetos.Tem("bp_var_ronda"), "Blueprint que o build nao tem: no-op, sem posse fantasma");
            Assert.IsFalse(s.Projetos.Posses.Values.Any(p => p.Origem == BlueprintSourceKind.Contract || p.Tx == "ctr:ctr_ronda_vila"));
        }

        [Test]
        public void PrazoBonus_AndaNoTempoDeJogo_ForaDoPrazoPagaONormal_NuncaMenos()
        {
            Sim s = Oficina();
            s.Contratos.Aceitar();
            Contrato c = s.Contratos.Ativo;
            float p0 = c.Prazo;
            Run(s, 10f);
            Assert.AreEqual(p0 - 10f, c.Prazo, 0.05f, "o prazo anda com o dt do Sim (offline nao conta)");
            c.Prazo = 0f;   // fora do prazo
            s.RateEma = 4.0;
            long gold = s.Gold, rn = s.Renome.Total;
            c.Etapa = 3;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(gold + 1200L, s.Gold, "300 s x 4/s, sem o bonus; nunca menos que isso");
            Assert.AreEqual(rn + 15L, s.Renome.Total, "+15 sem os +5 do prazo");
            Assert.AreEqual(1, s.Contratos.Concluidos.Count);
        }

        [Test]
        public void Abandonar_SemPenalidade_NadaPago_VoltaAOferta_ProgressoDoZero()
        {
            Sim s = Oficina();
            s.Contratos.Aceitar();
            s.Contratos.Ativo.Etapa = 1; s.Contratos.Ativo.Feito = 12;
            s.Gold = 500;
            long gold = s.Gold, rn = s.Renome.Total;
            Assert.IsTrue(s.Contratos.Abandonar());
            Assert.IsNull(s.Contratos.Ativo);
            Assert.AreEqual(gold, s.Gold);
            Assert.AreEqual(rn, s.Renome.Total);
            Assert.AreEqual(1, s.Contratos.Abandonados);
            Assert.IsEmpty(s.Contratos.Concluidos);
            Assert.AreEqual("ctr_ronda_vila", s.Contratos.Oferta.Id, "volta a lista, na mesma posicao");
            Assert.IsTrue(s.Contratos.Aceitar());
            Assert.AreEqual((0, 0), (s.Contratos.Ativo.Etapa, s.Contratos.Ativo.Feito), "do zero");
            Run(s, 5f);
            Assert.AreEqual(gold, s.Gold);
            Assert.AreEqual(0, Count(s, Ev.ContractDone));
        }

        // ------------------------------------------------------------------ save

        [Test]
        public void Save_IdaEVolta_AtivoComEtapaFeitoPrazo_ConcluidosNaOrdem_IdDesconhecidoFica()
        {
            Sim s = Oficina();
            s.Contratos.Concluidos.Add("ctr_futuro_x");   // concluido num build mais novo
            s.Contratos.Aceitar();
            s.Contratos.Ativo.Etapa = 1; s.Contratos.Ativo.Feito = 7; s.Contratos.Ativo.Prazo = 1234.5f;
            string save = s.Save(1000);
            StringAssert.Contains("\nctr=ctr_futuro_x\n", save);
            StringAssert.Contains("\nctr.v=ctr_ronda_vila,1,7,1234.5\n", save);
            Sim b = Sim.Load(save);
            Assert.AreEqual(1, b.UnknownIds, "o concluido que este build nao conhece conta e fica");
            CollectionAssert.AreEqual(new[] { "ctr_futuro_x" }, b.Contratos.Concluidos);
            Contrato c = b.Contratos.Ativo;
            Assert.AreEqual("ctr_ronda_vila", c.Def.Id);
            Assert.AreEqual((1, 7), (c.Etapa, c.Feito));
            Assert.AreEqual(1234.5f, c.Prazo, 0.001f);
            Assert.AreEqual(37, c.Progresso);
            StringAssert.Contains("\nctr=ctr_futuro_x\n", b.Save(2000), "regravar mantem o ID desconhecido");
            EncTeste.Vende(b, Item.Shield);
            Assert.AreEqual((1, 8), (c.Etapa, c.Feito), "segue de onde parou");
        }

        [Test]
        public void SaveAntigo_SemLinhasCtr_Nenhum_ESemContratoOTextoEODeAntes_Lixo_ViraOPadrao()
        {
            string semCtr = Oficina().Save(1);
            StringAssert.DoesNotContain("\nctr", semCtr, "sem contrato o save e' identico ao de antes do D-CORE-04");
            Sim b = Sim.Load(semCtr);
            Assert.IsNull(b.Contratos.Ativo);
            Assert.IsEmpty(b.Contratos.Concluidos);
            Assert.AreEqual(0, b.UnknownIds);
            Sim c = Sim.Load(semCtr + "ctr.v=ctr_ronda_vila,99,-5,abc\n");
            Assert.AreEqual((2, 0), (c.Contratos.Ativo.Etapa, c.Contratos.Ativo.Feito), "etapa presa a ultima, feito ao padrao");
            Assert.AreEqual(0f, c.Contratos.Ativo.Prazo);
            Sim d = Sim.Load(semCtr + "ctr.v=ctr_nao_existe,0,0,0\n");
            Assert.IsNull(d.Contratos.Ativo, "ativo desconhecido fica de fora");
            Assert.AreEqual(1, d.UnknownIds);
            Sim e = Sim.Load(semCtr + "ctr=ctr_ronda_vila\nctr.v=ctr_ronda_vila,0,0,0\n");
            Assert.IsNull(e.Contratos.Ativo, "ja concluido nao volta a ativo (nem paga 2x)");
            Assert.AreEqual("ctr_vinte_guardas", e.Contratos.Proximo.Id);
            Sim f = Sim.Load(semCtr + "ctr.v=ctr_ronda_vila,0,0,999999\n");
            Assert.AreEqual(5400f, f.Contratos.Ativo.Prazo, "prazo preso ao do ContractDef");
            Sim g = Sim.Load(semCtr + "ctr.v=ctr_ronda_vila,0,40,10\n");
            Assert.AreEqual(29, g.Contratos.Ativo.Feito, "feito preso a N - 1 da etapa");
        }

        [Test]
        public void RelogioParaTras_NaoReabreNemPaga()
        {
            Sim s = Oficina();
            s.Metas.Agora(Agora, 0); s.LiveOps.Agora(Agora);
            s.Contratos.Aceitar();
            s.Contratos.Ativo.Etapa = 3;
            s.Tick(Dt, 0f, 0f);
            long gold = s.Gold, rn = s.Renome.Total;
            s.Metas.Agora(Agora - 3 * 86400, 0); s.LiveOps.Agora(Agora - 3 * 86400);
            Run(s, 5f);
            Assert.AreEqual(gold, s.Gold);
            Assert.AreEqual(rn, s.Renome.Total);
            Assert.IsNull(s.Contratos.Ativo);
            Assert.AreEqual("ctr_vinte_guardas", s.Contratos.Proximo.Id, "o concluido nao reabre");
            Assert.AreEqual(0, Count(s, Ev.ContractDone));
            Sim b = Sim.Load(s.Save(Agora - 3 * 86400));
            b.Metas.Agora(Agora - 86400, 0);
            Run(b, 5f);
            Assert.AreEqual(gold, b.Gold);
            Assert.IsTrue(b.Contratos.Concluido("ctr_ronda_vila"));
            Assert.AreEqual(-1L, b.Metas.Paga("ctr:ctr_ronda_vila", 300, 0, 15));
        }

        /// <summary>C-CORE-02: os contratos sao do Imperio (os da raiz, o Old Forge). Num outro distrito o Sim.Contratos e' o da raiz: a venda de
        /// la conta pelo ID do item, o prazo anda no tick dele, a conclusao paga na raiz (metas, ledger) e o aviso chega ao distrito ativo; o
        /// payload do distrito nunca leva linha ctr*.</summary>
        [Test]
        public void Imperio_OutroDistrito_UsaOsDaRaiz_VendaContaPeloId_SaveSoNaRaiz()
        {
            Imperio imp = Imperio.Chegada(Distritos.ArmorersRow);
            Sim raiz = imp.Raiz, row = imp.Ativo;
            Assert.AreNotSame(raiz, row);
            Assert.AreSame(raiz.Contratos, row.Contratos, "o Heroi (a raiz) da os contratos");
            raiz.Contratos.Defs = new[] { Def("ctr_elmos", OrderDefs.OldForge, Quality.Normal, 10f, 1, 15, null, ("elmo", 2)) };
            Assert.IsTrue(raiz.Contratos.Forca("ctr_elmos"));
            Item elmo = (Item)Items.IndexOf("elmo", row.ItemDefs);
            float p0 = raiz.Contratos.Ativo.Prazo;
            EncTeste.Vende(row, elmo);
            Assert.AreEqual(1, raiz.Contratos.Ativo.Feito, "o elmo vendido na Row conta (pelo ID)");
            Assert.Less(raiz.Contratos.Ativo.Prazo, p0, "o prazo anda no tick do distrito ativo");
            StringAssert.DoesNotContain("\nctr", row.Save(1), "o payload do distrito nao leva contrato");
            StringAssert.Contains("\nctr.v=ctr_elmos,0,1,", raiz.Save(1));
            long rn = raiz.Renome.Total;
            EncTeste.Vende(row, elmo);
            Assert.AreEqual(1, Count(row, Ev.ContractDone), "o aviso chega ao distrito ativo (a View le os eventos dele)");
            CollectionAssert.AreEqual(new[] { "ctr_elmos" }, raiz.Contratos.Concluidos);
            Assert.AreEqual(rn + 20L, raiz.Renome.Total, "Renome da raiz: +15 e +5 do prazo");
            Assert.IsTrue(raiz.Metas.Ledger.Contains("ctr:ctr_elmos"));
            raiz.Metas.Cont.TryGetValue("contract_complete", out long n);
            Assert.AreEqual(1L, n, "as metas da raiz contam");
        }

        // ------------------------------------------------------------------ eventos, bot e validator

        [Test]
        public void LiveOps_MissaoContractComplete_ContaNoEventoAtivo_ARoyalWeekPagaPeloContrato()
        {
            Assert.Contains("contract_complete", LiveOpsDefs.Gatilhos);
            // raia H: a Royal Week de verdade tem a missao (no lugar dos 10 golpes de mestre; 8 linhas no cartao) e paga 100 pontos pelo contrato
            EventDef rw = Array.Find(LiveOpsDefs.Eventos, e => e.Id == "ev_s1w1_royal_week");
            MissaoEvento contrato = rw.Missoes.First(x => x.Id == "contrato1");
            Assert.AreEqual(("contract_complete", 1L, 100), (contrato.Evento, contrato.Alvo, contrato.Pontos));
            Assert.IsFalse(rw.Missoes.Any(x => x.Evento == "master_strike"));
            Assert.IsNull(LiveOpsDefs.Falta(rw, Items.All));
            Sim w = Oficina();
            long tw = rw.Comeca + 3600;
            w.Metas.Agora(tw, 0); w.LiveOps.Agora(tw);
            Assert.AreSame(rw, w.LiveOps.Evento);
            Assert.IsTrue(w.Contratos.Aceitar());
            w.Contratos.Ativo.Etapa = 3;
            w.Tick(Dt, 0f, 0f);
            Assert.AreEqual(1L, w.LiveOps.Feito(contrato));
            int antes = w.LiveOps.Pontos;
            w.LiveOps.Agora(tw + 10);
            Assert.IsTrue(w.LiveOps.Paga(contrato));
            Assert.AreEqual(antes + 100, w.LiveOps.Pontos, "pontos de evento, nunca moeda (V16)");
            // filtro pelo ID do contrato (evento injetado)
            var ev = new EventDef
            {
                Id = "ev_teste_contrato", Template = Template.RoyalWeek, Temporada = "s1_teste", Inicio = 0, Renome = 1, Tema = "royal",
                Missoes = new[] { new MissaoEvento("ctr1", "contract_complete", 1, 100), new MissaoEvento("ronda", "contract_complete", 1, 50, "ctr_ronda_vila") },
                Trilha = new[] { new Marco(100, new RewardDef(TipoPremio.Renome, 3)) },
            };
            Assert.IsNull(LiveOpsDefs.Falta(ev, Items.All), "gatilho e filtro (ID do contrato) existem no nucleo");
            Sim s = Oficina();
            s.LiveOps.Eventos = new[] { ev };
            long t = ev.Comeca + 3600;
            s.Metas.Agora(t, 0); s.LiveOps.Agora(t);
            Assert.AreSame(ev, s.LiveOps.Evento);
            s.Contratos.Aceitar();
            s.Contratos.Ativo.Etapa = 3;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(1L, s.LiveOps.Feito(ev.Missoes[1]), "filtro pelo ID");
            s.LiveOps.Agora(t + 10);
            Assert.AreEqual(150, s.LiveOps.PontosDe(ev), "as duas missoes pagas: pontos de evento, nunca moeda (V16)");
        }

        [Test]
        public void Bot_F2P_AceitaDepoisDasFerramentasComRenome3_ECumpreVendendo_SemContratoAPartidaEIdentica()
        {
            var s = new Sim(); var bot = new Bot();
            s.Renome.Ganha(180);
            float aceite = -1f, fim = -1f;
            for (float t = 0f; t < 30f * 60f && fim < 0f; t += Dt)
            {
                bot.Step(s, Dt);
                if (aceite < 0f && s.Contratos.Ativo != null) aceite = s.Time;
                if (s.Events.Exists(e => e.Kind == Ev.ContractDone)) fim = s.Time;
            }
            Assert.Greater(aceite, 0f, "aceitou");
            Assert.GreaterOrEqual(aceite, s.UpgradeTime[(int)Upgrade.Tools], "so depois das Ferramentas");
            Assert.Greater(fim, aceite, "cumpriu o ronda vendendo (30 espadas, 20 escudos, 10 ferramentas) em menos de 30 min");
            Assert.AreEqual(1, s.Contratos.Concluidos.Count);
            Assert.Greater(s.Contratos.OuroGanho, 0L);
            // sem o contrato (flag) a partida do F2P de 60 min e' identica ao byte (o F2P chega ao Renome 3 depois de 1,5 h: §32 do BALANCE)
            var com = new Sim(); var sem = new Sim(new Tuning { Contratos = false });
            var b1 = new Bot(); var b2 = new Bot();
            for (float t = 0f; t < 60f * 60f; t += Dt) { b1.Step(com, Dt); b2.Step(sem, Dt); }
            Assert.AreEqual(0, com.Contratos.Aceitos);
            Assert.AreEqual(sem.Save(1), com.Save(1), "mesmo save: nada muda sem o Renome 3");
        }

        [Test]
        public void Validator_DefeitosInjetados_Acusa()
        {
            ContractDef[] d =
            {
                Def("Ctr-X", OrderDefs.OldForge, Quality.Normal, 0f, 10, 15, null, ("espada", 5)),
                Def("ctr_lanca", OrderDefs.OldForge, Quality.Normal, 0f, 10, 15, null, ("item_fantasma", 5)),
                Def("ctr_lanca", OrderDefs.OldForge, Quality.Normal, 0f, 10, 15, null, ("espada", 0)),
                Def("ctr_mw", OrderDefs.OldForge, Quality.Masterwork, 0f, 10, 15, null, ("espada", 5)),
                Def("ctr_sem_premio", OrderDefs.OldForge, Quality.Normal, -1f, 0, 0, null, ("espada", 5)),
                Def("ctr_lua", "lua", Quality.Normal, 0f, 10, 15, "Bp-X", ("espada", 5)),
                Def("ctr_sem_etapa", OrderDefs.OldForge, Quality.Normal, 0f, 10, 15, null),
                Def("ctr_bp_sem_fonte", OrderDefs.OldForge, Quality.Normal, 0f, 10, 15, "bp_tecnica_espada", ("espada", 5)),
                Def("ctr_bp_essencial", OrderDefs.OldForge, Quality.Normal, 0f, 10, 15, "bp_so_contrato", ("espada", 5)),
                Def("ctr_alto", OrderDefs.OldForge, Quality.Normal, 0f, 10, 15, null, ("espada", 5)),
                Def("ctr_armorers", "armorers_row", Quality.Normal, 0f, 10, 15, "bp_var_x", ("elmo", 5)),   // fora do build: dormente, sem erro de item
            };
            BlueprintDef[] bps = new List<BlueprintDef>(ProjetosDefs.All)
            {
                new BlueprintDef("bp_so_contrato", ProjetosDefs.OldForge, BlueprintKind.StationLevel, new[] { "bigorna" }, true, new BlueprintSource(BlueprintSourceKind.Contract, "ctr_bp_essencial")),
                new BlueprintDef("bp_ctr_fantasma", ProjetosDefs.OldForge, BlueprintKind.Variant, new[] { "espada" }, false, new BlueprintSource(BlueprintSourceKind.Contract, "ctr_nao_existe")),
            }.ToArray();
            RenomeDef[] renome = new List<RenomeDef>(RenomeDefs.Niveis) { new RenomeDef(11, 99999, "ctr_alto", "ctr_sumiu") }.ToArray();
            var ups = (UpgradeDef[])Upgrades.All.Clone();
            UpgradeDef o = ups[(int)Upgrade.AnvilsLv4];
            ups[(int)Upgrade.AnvilsLv4] = new UpgradeDef(o.Up, o.Id, o.Requires, o.Name, o.Desc, o.Luxury, o.InMenu, o.Station, o.Meta, "bp_so_contrato");
            List<string> v = ContratosDefs.Validar(d, bps, renome, ups, ContentValidator.World()).ToList();
            Acusa(v, "contrato: 11 no catalogo (CONTEUDO_V1 §8: 12)");
            Acusa(v, "contrato Ctr-X: ID fora do formato ctr_[a-z0-9_]");
            Acusa(v, "contrato ctr_lanca: item \"item_fantasma\" que nao existe no jogo");   // a lanca e' da Row (C-CORE-05); o cinzel, do Quarter (C-CORE-06)
            Acusa(v, "contrato ctr_lanca: ID repetido");
            Acusa(v, "contrato ctr_lanca: etapa \"espada\" x0");
            Acusa(v, "contrato ctr_mw: qualidade minima Masterwork");
            Acusa(v, "contrato ctr_sem_premio: premio 0 s; Renome 0");
            Acusa(v, "contrato ctr_sem_premio: prazo-bonus negativo");
            Acusa(v, "contrato ctr_lua: distrito \"lua\" fora do catalogo");
            Acusa(v, "contrato ctr_lua: Blueprint \"Bp-X\" fora do formato");
            Acusa(v, "contrato ctr_sem_etapa: sem etapa");
            Acusa(v, "contrato ctr_bp_sem_fonte: Blueprint \"bp_tecnica_espada\" nao lista o contrato como fonte");
            Acusa(v, "contrato ctr_bp_essencial: Blueprint essencial \"bp_so_contrato\" so por contrato");
            Acusa(v, "contrato ctr_alto: Renome 11 inalcancavel (maximo 10)");
            Acusa(v, "renome: nivel 11 libera \"ctr_sumiu\", contrato que o build nao tem");
            Acusa(v, "blueprint bp_ctr_fantasma: contrato \"ctr_nao_existe\" nao existe");
            Acusa(v, "blueprint bp_so_contrato: so por contrato e uma melhoria o exige (contrato nunca bloqueia, V25)");
            Assert.IsFalse(v.Exists(m => m.StartsWith("contrato ctr_armorers:")), string.Join("\n", v));
            Acusa(ContentValidator.Validate(contracts: d), "contrato Ctr-X: ID fora do formato");
        }
    }
}
