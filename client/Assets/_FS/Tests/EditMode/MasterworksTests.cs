using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// B-CORE-08 (GDD §9, §13, §23 #9-#10; ECONOMIA_WAVE_B §6-§9; V13): Masterwork = golpe numa peca que ja sairia Superior, com o projeto e as
    /// condicoes, vai ao Hall 1x (nunca pilha, balcao nem GoldEarned), credita o Renome 1x pelo ID; ajudante e offline nunca fazem; a espada
    /// longa e' da familia espada; familia completa = +5% no preco. Collection derivada dos contadores, marcos 1x com retroativo. Hunts do
    /// LiveOps com o projeto como fonte extra. Save, validator, metas, bot e textos. Ouro e Renome sao long: literais L.
    /// </summary>
    public class MasterworksTests
    {
        const float Dt = 1f / 30f;

        static int Idx(string id) => MasterworksDefs.IndexOf(id);
        static int Count(Sim s, Ev kind) => s.Events.Count(e => e.Kind == kind);

        /// <summary>q 200 na Bigorna (toda peca sai Superior pela automacao) com o projeto da Primeira Lamina.</summary>
        static Sim ComProjeto(string bp = "bp_mw_primeira_lamina")
        {
            Sim s = QualidadeTests.ComQ(200);
            if (bp != null) Assert.IsTrue(s.Projetos.Ganha(bp, ProjetosDefs.All[ProjetosDefs.IndexOf(bp)].Sources[0].Kind));
            return s;
        }

        /// <summary>
        /// Uma peca na bancada `st` com o golpe de mestre do ferreiro (parado na boca de entrada, receita na entrada, saida vazia): devolve o nivel
        /// com que ela saiu; Masterwork = foi ao Hall (nada na pilha). MetasTests usa para a obra-prima de verdade.
        /// </summary>
        internal static Quality ComGolpe(Sim s, Station st)
        {
            st.Out = 0; Array.Clear(st.OutQ, 0, st.OutQ.Length);
            for (int k = 0; k < st.Ins.Length; k++) st.Ins[k] = st.Recipe.Inputs[k].Qty;
            s.StrikeCooldown = 0f;
            s.Player.Pos = st.InAt;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Strike(true), "golpe valendo na " + st.Name);
            int mw = s.Masterworks;
            for (int i = 0; i < 4000; i++)
            {
                s.Tick(Dt, 0f, 0f);
                if (s.Masterworks > mw) { Assert.AreEqual(0, st.Out, "obra-prima nao entra na pilha"); return Quality.Masterwork; }
                if (st.Out > 0) return st.OutQ[(int)Quality.Superior] > 0 ? Quality.Superior : st.OutQ[(int)Quality.Fine] > 0 ? Quality.Fine : Quality.Normal;
            }
            Assert.Fail("a bancada nao fez a peca");
            return Quality.Normal;
        }

        // ------------------------------------------------------------------ dados

        [Test]
        public void Catalogo_OldForge_10_ComPedestalProjetoECondicao_ValidatorVerde()
        {
            MasterworkDef[] a = MasterworksDefs.All;
            Assert.AreEqual(10, a.Length, "as 10 do Old Forge (ECONOMIA_WAVE_B §6); as 50 dos distritos 2-6 entram com a Wave C");
            CollectionAssert.AreEqual(Enumerable.Range(1, 10).ToArray(), a.Select(d => d.Pedestal).ToArray());
            Assert.IsTrue(a.All(d => d.Id.StartsWith("mw_") && d.District == ProjetosDefs.OldForge && d.Blueprint == "bp_" + d.Id));
            CollectionAssert.AreEqual(new[] { 25, 30, 50, 30, 25, 30, 25, 25, 50, 30 }, a.Select(d => d.Renome).ToArray(), "Renome do §6");
            CollectionAssert.AreEqual(new[] { "espada", "espada", "espada", "escudo", "escudo", "escudo", "ferramenta", "ferramenta", "joia", "joia" }, a.Select(d => d.Familia).ToArray());
            Assert.AreEqual((MasterworksDefs.CatalogoV1, 5), (60, MasterworksDefs.BonusFamiliaPct));
            Assert.IsEmpty(MasterworksDefs.Validar(a, MasterworksDefs.Marcos, ProjetosDefs.All, Upgrades.All, ContentValidator.World()).ToList());
            Assert.IsEmpty(ContentValidator.Validate(), "o conteudo inteiro segue verde");
            Assert.AreEqual("espada", Items.All[(int)Item.LongSword].Family, "a espada longa e' da familia espada (CONTEUDO_V1 §3)");
            Assert.IsTrue(Items.All.Where(i => i.Id != "espada_longa").All(i => i.Family == i.Id));
        }

        // ------------------------------------------------------------------ o golpe que vira obra-prima

        [Test]
        public void GolpeNaSuperior_ComProjeto_VaiAoHall1x_CreditaRenome_NuncaPilhaNemGoldEarned()
        {
            Sim s = ComProjeto();
            Assert.AreEqual(Quality.Superior, QualidadeTests.Peca(s), "q 200: Superior pela automacao, sem golpe");
            long renome = s.Renome.Total, earned = s.GoldEarned;
            Assert.AreEqual(-1, s.Hall.Pendente(s.ShieldBench), "familia escudo: nenhuma pendente");
            Assert.AreEqual(Idx("mw_primeira_lamina"), s.Hall.Pendente(s.AnvilA));
            Assert.AreEqual(Quality.Masterwork, ComGolpe(s, s.AnvilA));
            SimEvent e = s.Events.Find(x => x.Kind == Ev.Masterwork);
            Assert.AreEqual(((int)Item.Sword, Idx("mw_primeira_lamina")), (e.A, e.B), "A = item, B = indice no Hall");
            Assert.AreEqual((1, true, renome + 25L), (s.Hall.Count, s.Hall.Tem("mw_primeira_lamina"), s.Renome.Total), "Renome +25 da Primeira Lamina");
            Assert.IsTrue(s.Renome.Ledger.Contains("mw:mw_primeira_lamina"), "credito pelo ID de transacao");
            Assert.AreEqual(earned, s.GoldEarned, "nunca no GoldEarned");
            Assert.AreEqual(0, s.Stock[(int)Item.Sword] + s.Player.Held[(int)Item.Sword], "nem balcao nem mao");
            // a 2a vez sob as mesmas condicoes: a proxima da familia (Gume de Brasa) nao tem projeto -> Superior, e vende
            Assert.AreEqual(-1, s.Hall.Pendente(s.AnvilA));
            Assert.AreEqual(Quality.Superior, ComGolpe(s, s.AnvilA), "a 2a vez sai Superior");
            Assert.AreEqual((1, renome + 25L), (s.Hall.Count, s.Renome.Total), "nada de novo");
        }

        [Test]
        public void SemProjeto_OuSemCondicao_FicaSuperior_ComCondicao_Masterwork()
        {
            Sim s = ComProjeto(null);
            Assert.AreEqual(Quality.Superior, ComGolpe(s, s.AnvilA), "sem projeto: Superior");
            // Gume de Brasa: projeto na mao, mas a Bigorna no nivel 2 (pede 5); a Primeira Lamina (antes na lista) sem projeto
            Assert.IsTrue(s.Projetos.Ganha("bp_mw_gume_brasa", BlueprintSourceKind.OrderBlueprint));
            MasterworkDef gume = MasterworksDefs.All[Idx("mw_gume_brasa")];
            Assert.AreEqual((false, 2L), (s.Hall.Liberada(gume), s.Hall.Valor(gume.Condicoes[0])));
            Assert.AreEqual(Quality.Superior, ComGolpe(s, s.AnvilA), "condicao faltando: Superior");
            Assert.IsTrue(s.Projetos.Ganha("bp_bigorna_mestra", BlueprintSourceKind.OrderBlueprint));
            foreach (Upgrade u in new[] { Upgrade.AnvilsLv3, Upgrade.AnvilsLv4, Upgrade.AnvilsLv5 }) Assert.IsTrue(s.Buy(u), u.ToString());
            Assert.AreEqual(5L, s.Hall.Valor(gume.Condicoes[0]));
            Assert.AreEqual(Quality.Masterwork, ComGolpe(s, s.AnvilA));
            Assert.IsTrue(s.Hall.Tem("mw_gume_brasa"));
            Assert.IsFalse(s.Hall.Tem("mw_primeira_lamina"), "a da frente na lista sem projeto nao trava a seguinte");
        }

        [Test]
        public void EspadaLonga_DaFamiliaEspada_FazAObraPrimaDaEspada()
        {
            Sim s = ComProjeto();
            Assert.IsTrue(s.Projetos.Ganha("bp_rec_espada_longa", BlueprintSourceKind.OrderBlueprint));
            Assert.IsTrue(s.Buy(Upgrade.HammerSpeed) && s.Buy(Upgrade.LongAnvil));
            Assert.AreEqual(Idx("mw_primeira_lamina"), s.Hall.Pendente(s.LongAnvil));
            Assert.AreEqual(Quality.Masterwork, ComGolpe(s, s.LongAnvil));
            Assert.AreEqual(((int)Item.LongSword, Idx("mw_primeira_lamina")), (s.Events.Find(x => x.Kind == Ev.Masterwork).A, s.Events.Find(x => x.Kind == Ev.Masterwork).B));
            Assert.IsTrue(s.Hall.Tem("mw_primeira_lamina"));
        }

        /// <summary>V13: so a mao do ferreiro. Ajudantes produzindo Superiores com o projeto e o cofre offline nunca registram nada.</summary>
        [Test]
        public void Ajudante_EOffline_NuncaFazem()
        {
            Sim s = ComProjeto();
            for (int u = 0; u < Upgrades.Count; u++) if (Upgrades.IsProduction(u)) s.Buy((Upgrade)u);   // os 6 ajudantes trabalham
            s.Player.Pos = new V2(4.5f, 3.5f);
            for (float t = 0f; t < 600f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.Greater(s.Colecao.Superiores[(int)Item.Sword], 0, "premissa: Superiores vendidas pelos ajudantes");
            Assert.AreEqual((0, 0), (s.Masterworks, s.Hall.Count), "sem golpe, nenhuma");
            s.ApplyOffline(7200, 1);
            Assert.AreEqual((0, 0), (s.Masterworks, s.Hall.Count), "o cofre nunca da Masterwork");
        }

        [Test]
        public void FamiliaCompleta_Mais5PorCentoNoPreco_SoDaFamilia()
        {
            var umaSo = new[] { new MasterworkDef("mw_teste", ProjetosDefs.OldForge, "espada", "bp_mw_primeira_lamina", 10, 1) };
            Sim s = new Sim(new Tuning { NivelEstacaoQ = 200 }, masterworks: umaSo);
            s.Buy(Upgrade.Tools); s.Buy(Upgrade.AnvilsLv2);
            s.Projetos.Ganha("bp_mw_primeira_lamina", BlueprintSourceKind.Milestone);
            Assert.AreEqual((10, 25, 40), (s.PriceOf(Item.Sword, Quality.Normal), s.PriceOf(Item.Shield, Quality.Normal), s.PriceOf(Item.LongSword, Quality.Normal)), "sem bonus: o de antes");
            Assert.AreEqual(Quality.Masterwork, ComGolpe(s, s.AnvilA));
            Assert.AreEqual(5, s.Hall.BonusPct(Item.Sword));
            Assert.AreEqual((11, 26, 25, 42, 105), (s.PriceOf(Item.Sword, Quality.Normal), s.PriceOf(Item.Sword, Quality.Superior), s.PriceOf(Item.Shield, Quality.Normal),
                s.PriceOf(Item.LongSword, Quality.Normal), s.PriceOf(Item.LongSword, Quality.Superior)), "+5% na familia espada (meio para cima); escudo igual");
            Assert.AreEqual(0, s.PriceOf(Item.Sword, Quality.Masterwork), "Masterwork nunca vende");
            Assert.AreEqual(0, new Sim().Hall.BonusPct(Item.Sword), "real: 3 espadas, nenhuma no Hall");
        }

        // ------------------------------------------------------------------ save

        [Test]
        public void Save_IdaEVolta_NaoPagaDeNovo_SaveAntigoVazio_LedgerRepara()
        {
            Sim s = ComProjeto();
            s.LiveOps.Relogio = 1792000000;   // a data da 1a vez vem do relogio do LiveOps
            ComGolpe(s, s.AnvilA);
            string text = s.Save(10);
            StringAssert.Contains("hall=mw_primeira_lamina:1792000000\n", text);
            Sim b = Sim.Load(text, new Tuning { NivelEstacaoQ = 200 });
            Assert.AreEqual((1, 1792000000L, s.Renome.Total), (b.Hall.Count, b.Hall.Feitas["mw_primeira_lamina"], b.Renome.Total));
            Assert.AreEqual(text, b.Save(10), "save estavel");
            Assert.AreEqual(Quality.Superior, ComGolpe(b, b.AnvilA), "depois do load ela nao sai de novo");
            Assert.AreEqual(s.Renome.Total, b.Renome.Total);
            // save antigo (sem hall= e sem o mw: no ledger): Hall vazio; sem hall= mas com o mw: no rn=: volta registrada, sem pagar
            string semHall = string.Join("\n", text.Split('\n').Where(l => !l.StartsWith("hall=")));
            Sim c = Sim.Load(semHall);
            Assert.AreEqual((1, 0L), (c.Hall.Count, c.Hall.Feitas["mw_primeira_lamina"]), "o ledger do Renome repara");
            Assert.AreEqual(s.Renome.Total, c.Renome.Total);
            Sim d = Sim.Load(semHall.Replace(",mw:mw_primeira_lamina", "").Replace("mw:mw_primeira_lamina,", ""));
            Assert.AreEqual(0, d.Hall.Count, "save antigo: Hall vazio");
            Assert.IsFalse(d.Save(10).Contains("hall="), "Hall vazio nao grava a linha (texto de antes)");
            // downgrade: ID que o catalogo nao tem fica e conta; data lixo = 0
            Sim e = Sim.Load(text.Replace("hall=mw_primeira_lamina:1792000000", "hall=mw_elmo_vigia:5,mw_primeira_lamina:xx"));
            Assert.AreEqual((1, 1, 0L), (e.Hall.Count, e.UnknownIds, e.Hall.Feitas["mw_primeira_lamina"]));
            StringAssert.Contains("mw_elmo_vigia:5", e.Save(10));
        }

        // ------------------------------------------------------------------ Collection

        [Test]
        public void Colecao_DerivadaDosContadores_AbreNoProjeto_MarcosPagam1x()
        {
            var s = new Sim();
            Assert.AreEqual((15, 10, 26, 19), (s.Colecao.Total(AbaColecao.Produtos), s.Colecao.Total(AbaColecao.Masterworks), s.Colecao.Total(AbaColecao.Workers),
                s.Colecao.Total(AbaColecao.Blueprints)));
            // produto x qualidade: Normal = vendidas - Fina ou melhor; Fina = Finas - Superiores; Superior = Superiores
            s.SoldItems[(int)Item.Sword] = 5; s.Projetos.Finas[(int)Item.Sword] = 2; s.Colecao.Superiores[(int)Item.Sword] = 2;
            s.SoldItems[(int)Item.Shield] = 3; s.Projetos.Finas[(int)Item.Shield] = 1;
            Assert.AreEqual((3L, 0L, 2L), (s.Colecao.Vendidas((int)Item.Sword, Quality.Normal), s.Colecao.Vendidas((int)Item.Sword, Quality.Fine),
                s.Colecao.Vendidas((int)Item.Sword, Quality.Superior)));
            Assert.AreEqual(4, s.Colecao.Feitas(AbaColecao.Produtos), "espada Normal e Superior, escudo Normal e Fina");
            for (int i = (int)Item.Sword; i <= (int)Item.LongSword; i++) { s.SoldItems[i] = 9; s.Projetos.Finas[i] = 4; s.Colecao.Superiores[i] = 1; }
            Assert.AreEqual(15, s.Colecao.Feitas(AbaColecao.Produtos));
            s.Projetos.Ganha("bp_mw_anel_mercador", BlueprintSourceKind.Milestone);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((false, 0), (s.Colecao.Aberta, Count(s, Ev.Colecao)), "projeto sem as Ferramentas: fechada (FTUE, GDD §19)");
            var t = new Sim();
            t.Buy(Upgrade.Shields); t.Buy(Upgrade.Tools);
            Assert.IsFalse(t.Colecao.Aberta, "Ferramentas sem projeto de Masterwork: fechada");
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            long r0 = s.Renome.Total;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Colecao.Aberta);
            Assert.AreEqual(2, Count(s, Ev.Colecao), "os 2 marcos de Produtos (8 e 15) pagam no 1o Tick aberto (retroativo)");
            Assert.AreEqual(r0 + 15L, s.Renome.Total, "+5 e +10 (degraus do Old Forge pela metade: BALANCE §30.6)");
            Assert.IsTrue(s.Renome.Ledger.Contains("col:col_produtos_old_forge_100"));
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((0, r0 + 15L), (Count(s, Ev.Colecao), s.Renome.Total), "nunca 2x");
            // save/load nao paga de novo; Superior por item vai no colq=
            string text = s.Save(10);
            StringAssert.Contains("colq=espada:1,escudo:1,ferramenta:1,joia:1,espada_longa:1\n", text);
            Sim b = Sim.Load(text);
            b.Tick(Dt, 0f, 0f);
            Assert.AreEqual((0, s.Renome.Total, 15), (Count(b, Ev.Colecao), b.Renome.Total, b.Colecao.Feitas(AbaColecao.Produtos)));
            Assert.AreEqual(1, b.Colecao.Superiores[(int)Item.Jewel]);
        }

        [Test]
        public void Colecao_VendaDeVerdade_ContaSuperior_EAbaWorkersEProjetos()
        {
            Sim s = QualidadeTests.ComQ(200);
            s.Stock[(int)Item.Sword] = 1; s.StockQ[(int)Item.Sword][(int)Quality.Superior] = 1;
            s.Queue.Add(new Client { Want = Item.Sword, Patience = 60f, MaxPatience = 60f });
            for (int i = 0; i < 60 && s.SoldItems[(int)Item.Sword] == 0; i++) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((1, 1, 1), (s.SoldItems[(int)Item.Sword], s.Projetos.Finas[(int)Item.Sword], s.Colecao.Superiores[(int)Item.Sword]));
            Assert.AreEqual(1, s.Colecao.Feitas(AbaColecao.Produtos), "so a Superior da espada");
            // workers: contratado conta 1 e nivel maximo outro (o ajudante do upgrade conta como contratado)
            s.Buy(Upgrade.Helper1);
            Assert.AreEqual(1, s.Colecao.Feitas(AbaColecao.Workers));
            Captura.Equipe(s);
            Assert.Greater(s.Colecao.Feitas(AbaColecao.Workers), 1);
            Assert.AreEqual(0, s.Colecao.Feitas(AbaColecao.Blueprints));
            s.Projetos.Ganha("bp_tecnica_espada", BlueprintSourceKind.OrderBlueprint);
            Assert.AreEqual(1, s.Colecao.Feitas(AbaColecao.Blueprints));
        }

        // ------------------------------------------------------------------ LiveOps: os Hunts

        static long Fuso => -3 * 3600;

        /// <summary>Hunt so com o que existe hoje (os 2 de verdade tambem esperam o order_complete e o knob QualidadeEvento): o requisito e' o projeto
        /// da Primeira Lamina e o limiar 50 da o projeto da Sunblade (fallback de 60 s).</summary>
        static EventDef Hunt() => new EventDef
        {
            Id = "ev_s1d6_hunt_teste", Template = Template.MasterworkHunt, Temporada = "s1_royal_commission", Inicio = 6, Dias = 1,
            Requer = new[] { "bp_mw_primeira_lamina" },
            Missoes = new[] { new MissaoEvento("vende5", "product_sold", 5, 700) },
            Trilha = new[] { new Marco(50, new RewardDef(TipoPremio.Blueprint, 0, "bp_mw_sunblade", new RewardDef(TipoPremio.Ouro, 60))), new Marco(700, new RewardDef(TipoPremio.PassXp, 100)) },
            Modificadores = new[] { new Modificador("PatienceBase", 1.1f) },
        };

        static Sim Aberta()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            s.RateEma = 2.0;
            return s;
        }

        static void Agora(Sim s, long t) { s.Metas.Agora(t, Fuso); s.LiveOps.Agora(t); }

        [Test]
        public void Hunts_RodamNoCalendario_OProjetoERequisitoDoJogador_EOKnobDeQualidadeEntra()
        {
            var s = new Sim();
            EventDef[] hunts = LiveOpsDefs.Eventos.Where(x => x.Template == Template.MasterworkHunt).ToArray();
            Assert.AreEqual(2, hunts.Length);
            foreach (EventDef e in hunts) Assert.IsNull(s.LiveOps.Falta(e), e.Id + ": order_complete (Orders 2.0) e QualidadeEvento (B-CORE-08) existem");
            EventDef outro = Hunt();
            outro.Requer = new[] { "ord_bulk_old_forge" };
            Assert.IsNull(s.LiveOps.Falta(outro), "OrderDef no Requer: so precisa existir");
            outro.Requer = new[] { "ord_nao_existe" };
            Assert.AreEqual("requisito ord_nao_existe", s.LiveOps.Falta(outro));
            // o Hunt da Sunblade: requisito Renome 1 + o projeto da Primeira Lamina; dentro, a espada ganha +15 de qualidade (QualidadeEvento)
            EventDef sun = hunts.First(e => e.Id.EndsWith("sunblade"));
            Sim r = Aberta();
            r.Renome.Ganha(RenomeDefs.Niveis[1].Limiar);
            r.Buy(Upgrade.AnvilsLv2);
            int q0 = r.QualityPoints(r.AnvilA);
            Agora(r, sun.Comeca + 3600);
            Assert.IsNull(r.LiveOps.Evento, "sem o projeto da Primeira Lamina");
            r.Projetos.Ganha("bp_mw_primeira_lamina", BlueprintSourceKind.Milestone);
            Agora(r, sun.Comeca + 3601);
            Assert.AreSame(sun, r.LiveOps.Evento);
            Assert.AreEqual((q0 + 15, 0), (r.QualityPoints(r.AnvilA), r.QualityPoints(r.ToolBench)),
                "+15 so na espada");
            Agora(r, sun.Termina);
            Assert.AreEqual(q0, r.QualityPoints(r.AnvilA), "o knob volta no fim");
            // RemoteConfig: QualidadeEvento (padrao 0) so ate a trava de 25
            var t = new Tuning();
            RemoteConfig.Aplicar(t, "{\"QualidadeEvento\": [0, 0, 99, 0, 0, 0, 0]}", false);
            Assert.AreEqual(25, t.QualidadeEvento[(int)Item.Sword]);
        }

        [Test]
        public void Hunt_RodaComOProjeto_DaOProjetoDaSunbladeComoFonteExtra_SemFallback_1x()
        {
            EventDef h = Hunt();
            Assert.IsEmpty(LiveOpsDefs.Validar(LiveOpsDefs.Eventos.Concat(new[] { h }).ToArray(), LiveOpsDefs.Passes).ToList());
            Sim s = Aberta();
            s.LiveOps.Eventos = new[] { h };
            Agora(s, h.Comeca + 3600);
            Assert.IsNull(s.LiveOps.Evento, "sem o projeto da Primeira Lamina (requisito do jogador) nao entra");
            s.Projetos.Ganha("bp_mw_primeira_lamina", BlueprintSourceKind.Milestone);
            Agora(s, h.Comeca + 3601);
            Assert.AreSame(h, s.LiveOps.Evento);
            long gold = s.Gold;
            for (int k = 0; k < 5; k++) s.LiveOps.On(Ev.Sold, (int)Item.Sword, 10);
            Agora(s, h.Comeca + 3700);
            Assert.IsTrue(s.Projetos.Tem("bp_mw_sunblade"));
            Assert.AreEqual((BlueprintSourceKind.Event, "e:ev_s1d6_hunt_teste:50"), (s.Projetos.Posses["bp_mw_sunblade"].Origem, s.Projetos.Posses["bp_mw_sunblade"].Tx));
            Assert.AreEqual(gold, s.Gold, "o projeto saiu: sem o fallback de 60 s");
            Assert.IsTrue(s.Metas.Ledger.Contains("e:ev_s1d6_hunt_teste:50"), "o limiar pagou (1x pelo ID)");
            Agora(s, h.Comeca + 3800);
            Assert.AreEqual(gold, s.Gold, "nunca 2x");
            // quem ja tinha o projeto (fila Royal, a fonte gratis permanente) recebe o fallback no mesmo ID
            Sim r = Aberta();
            r.LiveOps.Eventos = new[] { h };
            r.Projetos.Ganha("bp_mw_primeira_lamina", BlueprintSourceKind.Milestone);
            r.Projetos.Ganha("bp_mw_sunblade", BlueprintSourceKind.OrderRoyal);
            Agora(r, h.Comeca + 3600);
            long g0 = r.Gold;
            for (int k = 0; k < 5; k++) r.LiveOps.On(Ev.Sold, (int)Item.Sword, 10);
            Agora(r, h.Comeca + 3700);
            Assert.AreEqual(g0 + 120L, r.Gold, "60 s x 2/s");
            Assert.AreEqual(BlueprintSourceKind.OrderRoyal, r.Projetos.Posses["bp_mw_sunblade"].Origem, "a posse de antes fica");
        }

        // ------------------------------------------------------------------ metas

        [Test]
        public void Metas_ConquistasDeMasterwork_ContamNoHall()
        {
            Sim s = ComProjeto();
            MetaDef mw1 = MetasDefs.Achar("ach_mw1"), fam = MetasDefs.Achar("ach_familia_espada"), sun = MetasDefs.Achar("ach_sunblade");
            Assert.AreEqual((0L, 0L), (s.Metas.Conquista(mw1).Feito, s.Metas.Conquista(fam).Feito));
            ComGolpe(s, s.AnvilA);
            Assert.AreEqual((1L, 1L, 0L), (s.Metas.Conquista(mw1).Feito, s.Metas.Conquista(fam).Feito, s.Metas.Conquista(sun).Feito));
            Assert.IsTrue(s.Metas.Conquista(mw1).Pronta);
            Assert.AreEqual(1L, s.Metas.Cont["masterwork_unlock." + Idx("mw_primeira_lamina")]);
            // o validator recusa conquista de mais Masterworks do que o build tem (ach_mw30/60 esperam os distritos)
            var ach = MetasDefs.Conquistas.Concat(new[] { new MetaDef("ach_mw30", TipoMeta.Conquista, -1, "masterwork_unlock", -1, 30, null, 0, 200, 10) }).ToArray();
            Assert.IsTrue(ContentValidator.Missoes(MetasDefs.Diarias, MetasDefs.Semanais, ach, MetasDefs.LoginDias, Tuning.Padrao).Any(e => e.Contains("acima das 10 Masterworks")));
        }

        // ------------------------------------------------------------------ validator

        static List<string> Valida(MasterworkDef[] mws = null, MarcoColecaoDef[] marcos = null, BlueprintDef[] bps = null, UpgradeDef[] ups = null) =>
            MasterworksDefs.Validar(mws ?? MasterworksDefs.All, marcos ?? MasterworksDefs.Marcos, bps ?? ProjetosDefs.All, ups ?? Upgrades.All, ContentValidator.World()).ToList();

        static void Acusa(List<string> v, string trecho) =>
            Assert.IsTrue(v.Exists(m => m.Contains(trecho)), $"esperava \"{trecho}\" em:\n{string.Join("\n", v)}");

        static MasterworkDef[] Mais(params MasterworkDef[] extra) => MasterworksDefs.All.Concat(extra).ToArray();
        static MasterworkDef Mw(string id, string fam, string bp, int pedestal, params CondicaoMasterwork[] c) => new MasterworkDef(id, ProjetosDefs.OldForge, fam, bp, 20, pedestal, c);

        [Test]
        public void Validator_DefeitosInjetados_Acusa()
        {
            Acusa(Valida(Mais(Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1))), "ID repetido");
            Acusa(Valida(Mais(Mw("Obra X", "espada", "bp_mw_primeira_lamina", 1))), "fora do formato");
            Acusa(Valida(Mais(Mw("mw_elmo_vigia", "elmo", "bp_mw_elmo_vigia", 1))), "familia \"elmo\" sem produto");   // distrito 2: espera a Wave C
            Acusa(Valida(Mais(Mw("mw_x", "espada", "bp_nao_existe", 1))), "projeto \"bp_nao_existe\" nao existe");
            Acusa(Valida(Mais(Mw("mw_x", "espada", "bp_tecnica_espada", 1))), "nao e' o dela");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1), Mw("mw_gume_brasa", "espada", "bp_mw_gume_brasa", 1) }), "pedestal 1");
            Acusa(Valida(Mais(Mw("mw_x", "espada", "bp_mw_primeira_lamina", 11))), "pedestal 11");
            Acusa(Valida(new[] { new MasterworkDef("mw_primeira_lamina", "old_forge", "espada", "bp_mw_primeira_lamina", 0, 1) }), "Renome 0");
            Acusa(Valida(new[] { new MasterworkDef("mw_primeira_lamina", "armorers_row", "espada", "bp_mw_primeira_lamina", 25, 1) }), "distrito \"armorers_row\"");
            // condicoes: anuncio ou compra nunca; alvo que o build nao tem; tipo desconhecido
            CondicaoMasterwork C(CondicaoMw k, string r, int n) => new CondicaoMasterwork(k, r, n);
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C(CondicaoMw.Vendidos, "ad_reward", 1)) }), "anuncio ou compra");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C(CondicaoMw.EncomendasTipo, "iap_pack", 1)) }), "anuncio ou compra");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C(CondicaoMw.Vendidos, "vip", 1)) }), "anuncio ou compra");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C(CondicaoMw.Vendidos, "elmo", 100)) }), "inalcancavel");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C(CondicaoMw.NivelEstacao, "bigorna", 6)) }), "inalcancavel");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C(CondicaoMw.NivelWorker, "wk_vulcana", 5)) }), "inalcancavel");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C(CondicaoMw.Renome, null, 11)) }), "inalcancavel");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C(CondicaoMw.EncomendasTipo, "event", 5)) }), "inalcancavel");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C((CondicaoMw)99, "espada", 5)) }), "inalcancavel");
            Acusa(Valida(new[] { Mw("mw_primeira_lamina", "espada", "bp_mw_primeira_lamina", 1, C(CondicaoMw.Vendidos, "espada", 0)) }), "inalcancavel");
            // projeto so de evento (sem fonte gratis permanente)
            BlueprintDef[] bps = ProjetosDefs.All.Select(b => b.Id != "bp_mw_sunblade" ? b
                : new BlueprintDef(b.Id, b.District, b.Kind, b.Targets, false, new BlueprintSource(BlueprintSourceKind.Event, "ev_s1w4_hunt_sunblade"))).ToArray();
            Acusa(Valida(bps: bps), "sem fonte gratis permanente");
            // projeto de Masterwork que aponta para uma que nao existe
            Acusa(Valida(MasterworksDefs.All.Where(m => m.Id != "mw_anel_mercador").ToArray()), "aponta para \"mw_anel_mercador\"");
            // Superior inalcancavel: sem os niveis da joalheria a familia joia para em 20 + 25 + 10 = 55
            Acusa(Valida(ups: Upgrades.All.Select(u => u.Station == "joalheria" ? new UpgradeDef(u.Up, u.Id, u.Requires, u.Name, u.Desc, u.Luxury, u.InMenu) : u).ToArray()), "Superior inalcancavel na familia joia");
            Assert.AreEqual(115, MasterworksDefs.QMaximo("bigorna", new Tuning(), Upgrades.All, ProjetosDefs.All, WorkerDefs.All), "CONTEUDO_V1 §10.2: 60 + 20 + 25 + 10");
            // marcos
            Acusa(Valida(marcos: new[] { new MarcoColecaoDef("col_produtos_25", AbaColecao.Produtos, 29, 10) }), "marco inalcancavel");
            Acusa(Valida(marcos: new[] { new MarcoColecaoDef("col_vazio", AbaColecao.Produtos, 2, 0) }), "sem recompensa");
            Acusa(Valida(marcos: new[] { new MarcoColecaoDef("col_a", AbaColecao.Produtos, 2, 5), new MarcoColecaoDef("col_a", AbaColecao.Workers, 2, 5) }), "ID repetido");
            Acusa(Valida(marcos: new[] { new MarcoColecaoDef("marco_x", AbaColecao.Produtos, 2, 5) }), "fora do formato col_");
            Acusa(Valida(marcos: new[] { new MarcoColecaoDef("col_b", AbaColecao.Produtos, 2, 0, "estandarte") }), "cosmetico");
        }

        // ------------------------------------------------------------------ bot

        /// <summary>O Ideal (golpe) faz Masterwork quando o projeto e as condicoes existem e a automacao chega a Superior; F2P e Slow (sem golpe)
        /// nunca. Aqui q 60 por nivel (no nivel III, q 120: Fina sempre e Superior a cada 5 pecas) e os projetos sem condicao dados no inicio.</summary>
        [Test]
        public void Bot_IdealFaz_F2PESlowNunca()
        {
            foreach (BotProfile p in new[] { BotProfile.Ideal, BotProfile.F2P, BotProfile.Slow })
            {
                var s = new Sim(new Tuning { NivelEstacaoQ = 60 });
                s.Projetos.Ganha("bp_mw_primeira_lamina", BlueprintSourceKind.Milestone);
                s.Projetos.Ganha("bp_mw_anel_mercador", BlueprintSourceKind.Milestone);
                Bot bot = Bot.From(p);
                long earned = 0;
                for (float t = 0f; t < 45 * 60f; t += Dt)
                {
                    bot.Step(s, Dt);
                    foreach (SimEvent e in s.Events) if (e.Kind == Ev.Sold) earned += e.B;
                }
                Assert.AreEqual(earned, s.GoldEarned, p.Name + ": a receita sao as vendas, nenhuma Masterwork");
                if (p.Strike) Assert.Greater(s.Hall.Count, 0, "o Ideal registra");
                else Assert.AreEqual((0, 0), (s.Hall.Count, s.Masterworks), p.Name + " nao golpeia");
                Assert.AreEqual(s.Hall.Count, s.Masterworks, "toda obra-prima do bot tem MasterworkDef");
            }
        }

        /// <summary>ECONOMIA_WAVE_B §1.4: com Masterwork pendente e Superior ao alcance (q >= 100), o Ideal so golpeia a peca que ja sairia Superior;
        /// sem pendente, todo golpe (o de antes).</summary>
        [Test]
        public void Bot_Ideal_EsperaAPecaSuperiorComPendente()
        {
            int Golpeia(bool projeto, int acc)
            {
                Sim s = QualidadeTests.ComQ(120);   // q 120: Fina sempre; Superior quando o acumulador passa de 100
                if (projeto) s.Projetos.Ganha("bp_mw_primeira_lamina", BlueprintSourceKind.Milestone);
                s.AnvilA.Ins[0] = s.AnvilA.Recipe.Inputs[0].Qty;
                s.AnvilA.QAcc = acc;
                s.Player.Pos = s.AnvilA.InAt;
                Bot.Ideal().Step(s, Dt);
                return s.Strikes;
            }
            Assert.AreEqual(Quality.Fine, new Func<Quality>(() => { Sim s = QualidadeTests.ComQ(120); return s.Hall.ProximaPeca(s.AnvilA); })());
            Assert.AreEqual(0, Golpeia(true, 0), "pendente e a proxima sai Fina: espera");
            Assert.AreEqual(1, Golpeia(true, 80), "a proxima ja sairia Superior: golpeia (vira Masterwork)");
            Assert.AreEqual(1, Golpeia(false, 0), "sem pendente: golpeia como antes");
        }

        // ------------------------------------------------------------------ textos

        [Test]
        public void Textos_DoHallEDaColecao_NosTresIdiomas()
        {
            Sim s = ComProjeto();
            ComGolpe(s, s.AnvilA);
            try
            {
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    foreach (MasterworkDef d in MasterworksDefs.All)
                    {
                        foreach (string t in new[] { Textos.NomeMasterwork(d), Textos.SeloMasterwork(s, d), Textos.CondicaoMasterwork(s, d), Textos.ProjetoMasterwork(s, d) })
                            StringAssert.DoesNotContain("[", t, $"{i}: {d.Id}");
                        Assert.LessOrEqual(Textos.NomeMasterwork(d).Length, Textos.HallMax, $"{i}: {d.Id}");
                        Assert.LessOrEqual(Textos.CondicaoMasterwork(s, d).Length, Textos.HallDetalheMax, $"{i}: {Textos.CondicaoMasterwork(s, d)}");
                    }
                    foreach (MarcoColecaoDef m in MasterworksDefs.Marcos)
                        foreach (string t in new[] { Textos.MarcoColecao(s, m), Textos.PremioColecao(m), Textos.SeloColecao(s, m) })
                        {
                            StringAssert.DoesNotContain("[", t, $"{i}: {m.Id}");
                            Assert.LessOrEqual(t.Length, Textos.PremioMax, $"{i}: {t}");
                        }
                    StringAssert.DoesNotContain("[", Textos.ResumoColecao(s));
                    Assert.LessOrEqual(Loc.T("aviso.colecao", 50).Length, Textos.AvisoMax, i.ToString());
                    Assert.LessOrEqual(Loc.T("hall.titulo").Length, 12, "aba do painel de metas");
                }
                Loc.Atual = Idioma.Pt;
                Assert.AreEqual("Sem condição extra", Textos.CondicaoMasterwork(s, MasterworksDefs.All[Idx("mw_anel_mercador")]));
                Assert.AreEqual("Bigorna nível 5 (2/5)", Textos.CondicaoMasterwork(s, MasterworksDefs.All[Idx("mw_gume_brasa")]));
                Assert.AreEqual("Entregue 5 encomendas mistas (0/5)", Textos.CondicaoMasterwork(s, MasterworksDefs.All[Idx("mw_tenaz_eterna")]));
                Assert.AreEqual("Tomé no nível 5 (0/5)", Textos.CondicaoMasterwork(s, MasterworksDefs.All[Idx("mw_escudo_brasao")]));
                Assert.AreEqual(("NO HALL", "BLOQUEADO"), (Textos.SeloMasterwork(s, MasterworksDefs.All[0]), Textos.SeloMasterwork(s, MasterworksDefs.All[1])));
                Assert.AreEqual("Projeto: Venda 300 escudos (0/300)", Textos.ProjetoMasterwork(s, MasterworksDefs.All[Idx("mw_broquel")]));
                Assert.AreEqual("Projeto na mão", Textos.ProjetoMasterwork(s, MasterworksDefs.All[0]));
                Assert.AreEqual("Produtos: 8 de 15", Textos.MarcoColecao(s, MasterworksDefs.Marcos[0]));
                Assert.AreEqual("+50 Renome  ·  Estandarte do Old Forge", Textos.PremioColecao(MasterworksDefs.Marcos[3]));
                Assert.AreEqual("Obra-prima: Lâmina Solar do Rei", Textos.Meta(MetasDefs.Achar("ach_sunblade"), 1));
                Assert.AreEqual("Faça 3 obras-primas de espada", Textos.Meta(MetasDefs.Achar("ach_familia_espada"), 3));
            }
            finally { Loc.Atual = Idioma.Pt; }
        }
    }
}
