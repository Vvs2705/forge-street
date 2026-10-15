using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// B-CORE-07 (GDD §12, §23 #8; ECONOMIA_WAVE_B §4, §8.5, §9): Blueprint e' posse (ganhar 2x nao duplica, nada de saldo no save), fontes de
    /// hoje (marco de venda, Fina ou melhor, producao completa, Renome, conquista resgatada) e ganchos (fila de encomenda, loja com ID de
    /// transacao), desbloqueio (niveis IV-V, bigorna longa e a linha da espada longa), tecnica no QualityBreakdown, save/load e save antigo, o
    /// validator ("receita essencial nunca so paga") e o bot F2P chegando aos essenciais e comprando nivel IV-V. Ouro e' long: literais L.
    /// </summary>
    public class ProjetosTests
    {
        const float Dt = 1f / 30f;

        /// <summary>Os 8 niveis IV-V (menu, Meta, com projeto), na ordem do enum.</summary>
        internal static readonly Upgrade[] NiveisMestre =
        {
            Upgrade.AnvilsLv4, Upgrade.AnvilsLv5, Upgrade.ShieldsLv4, Upgrade.ShieldsLv5, Upgrade.ToolsLv4, Upgrade.ToolsLv5, Upgrade.JewelsLv4, Upgrade.JewelsLv5,
        };

        static void Run(Sim s, float seconds) { for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt, 0f, 0f); }
        static int Count(Sim s, Ev kind) => s.Events.Count(e => e.Kind == kind);
        static int Idx(string id) => ProjetosDefs.IndexOf(id);
        static Pad PadFor(Sim s, Upgrade u) => s.Pads.Find(p => Array.IndexOf(p.Chain, u) >= 0);

        static BlueprintDef Def(string id, bool essencial, params BlueprintSource[] f) =>
            new BlueprintDef(id, ProjetosDefs.OldForge, BlueprintKind.Variant, new[] { "espada" }, essencial, f);

        static List<string> Valida(BlueprintDef[] defs, UpgradeDef[] ups = null) =>
            new List<string>(ContentValidator.Blueprints(defs, ups ?? Upgrades.All, ContentValidator.World()));

        static void Acusa(List<string> v, string trecho) =>
            Assert.IsTrue(v.Exists(m => m.Contains(trecho)), $"esperava \"{trecho}\" em:\n{string.Join("\n", v)}");

        [Test]
        public void Catalogo_OldForge_19ComOs3Essenciais_NaOrdemDoConteudo()
        {
            BlueprintDef[] a = ProjetosDefs.All;
            Assert.AreEqual(19, a.Length, "os 18 do ECONOMIA_WAVE_B §4 + bp_rec_espada_longa (CONTEUDO_V1 §9)");
            CollectionAssert.AreEqual(new[] { "bp_bigorna_mestra", "bp_bancada_mestra", "bp_mw_primeira_lamina" }, a.Where(d => d.Essential).Select(d => d.Id).ToArray());
            Assert.IsTrue(a.All(d => d.District == ProjetosDefs.OldForge && d.Id.StartsWith("bp_")));
            Assert.IsTrue(a.All(d => d.Sources.All(f => !ProjetosDefs.Paga(f.Kind))), "nada pago na Wave B (V9, V20)");
            Assert.AreEqual((101, 25), (ProjetosDefs.CatalogoV1, ProjetosDefs.EssenciaisV1), "o resto do catalogo entra com a Wave C");
            // fila de encomenda do Old Forge: #1..#7 sem buraco nem repeticao (a ordem e' a recompensa, sem sorteio)
            var fila = a.SelectMany(d => d.Sources.Where(f => f.Kind == BlueprintSourceKind.OrderBlueprint).Select(f => f.N)).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7 }, fila);
            Assert.IsEmpty(Valida(a));
        }

        /// <summary>Posse: ganhar 2x (o mesmo marco em dois Ticks, o gancho de novo, a mesma transacao) nao duplica nem emite de novo.</summary>
        [Test]
        public void Posse_Idempotente_GanharDuasVezesNaoDuplica()
        {
            var s = new Sim();
            s.SoldItems[(int)Item.Sword] = 100;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(1, Count(s, Ev.Blueprint));
            SimEvent e = s.Events.Find(x => x.Kind == Ev.Blueprint);
            Assert.AreEqual((Idx("bp_mw_primeira_lamina"), (int)BlueprintSourceKind.Milestone), (e.A, e.B), "A = indice no catalogo, B = fonte");
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((0, 1), (Count(s, Ev.Blueprint), s.Projetos.Count), "marco batido de novo: nada");
            Assert.IsFalse(s.Projetos.Ganha("bp_mw_primeira_lamina", BlueprintSourceKind.Milestone), "o gancho com o ID ja possuido: no-op");
            Assert.IsTrue(s.Projetos.Ganha("bp_tecnica_espada", BlueprintSourceKind.OrderBlueprint));
            Assert.IsFalse(s.Projetos.Ganha("bp_tecnica_espada", BlueprintSourceKind.OrderBlueprint));
            Assert.AreEqual(2, s.Projetos.Count);
            string bp = s.Save(1).Split('\n').First(l => l.StartsWith("bp="));
            Assert.AreEqual("bp=bp_tecnica_espada:OrderBlueprint,bp_mw_primeira_lamina:Milestone", bp, "posse = ID e origem, nunca saldo (GDD §23 #8)");
        }

        [Test]
        public void Ganchos_SoFonteListada_PagaSoComTx_TxNaoRepete()
        {
            var s = new Sim();
            Assert.IsFalse(s.Projetos.Ganha("bp_tecnica_espada", BlueprintSourceKind.Shop, "iap:1"), "loja nao e' fonte dele");
            Assert.IsFalse(s.Projetos.Ganha("bp_nao_existe", BlueprintSourceKind.Milestone));
            BlueprintSource loja = new BlueprintSource(BlueprintSourceKind.Shop, "pack_1"), marco = new BlueprintSource(BlueprintSourceKind.Milestone, "espada", 999);
            var t = new Sim(null, null, null, new[] { Def("bp_a", false, marco, loja), Def("bp_b", false, marco, loja) });
            Assert.IsFalse(t.Projetos.Ganha("bp_a", BlueprintSourceKind.Shop), "pago sem ID de transacao: recusa");
            Assert.IsTrue(t.Projetos.Ganha("bp_a", BlueprintSourceKind.Shop, "iap:42"));
            Assert.IsFalse(t.Projetos.Ganha("bp_b", BlueprintSourceKind.Shop, "iap:42"), "a mesma transacao nao paga 2x");
            Assert.AreEqual(("iap:42", BlueprintSourceKind.Shop), (t.Projetos.Posses["bp_a"].Tx, t.Projetos.Posses["bp_a"].Origem));
        }

        [Test]
        public void Fontes_MarcosFinaProducaoRenomeEConquista()
        {
            var s = new Sim();
            s.SoldItems[(int)Item.Tool] = 149; s.SoldItems[(int)Item.Jewel] = 100;
            s.Tick(Dt, 0f, 0f);
            Assert.IsFalse(s.Projetos.Tem("bp_tecnica_ferramenta"), "149 de 150");
            Assert.IsTrue(s.Projetos.Tem("bp_tecnica_joia"), "50 joias");
            Assert.IsFalse(s.Projetos.Tem("bp_mw_anel_mercador"), "100 joias Normais nao sao 100 Fina ou melhor");
            s.SoldItems[(int)Item.Tool] = 150;
            for (int i = 0; i < 99; i++) s.Projetos.OnVenda((int)Item.Jewel, i % 2 == 0 ? Quality.Fine : Quality.Superior);
            s.Projetos.OnVenda((int)Item.Jewel, Quality.Normal);
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Projetos.Tem("bp_tecnica_ferramenta"));
            Assert.IsFalse(s.Projetos.Tem("bp_mw_anel_mercador"), "99 Fina ou melhor");
            s.Projetos.OnVenda((int)Item.Jewel, Quality.Fine);
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Projetos.Tem("bp_mw_anel_mercador"));

            Assert.IsFalse(s.Projetos.Tem("bp_bigorna_mestra") || s.Projetos.Tem("bp_bancada_mestra"));
            s.Gold = 1000000;
            for (int u = 0; u < Upgrades.Count; u++) if (Upgrades.IsProduction(u)) s.Buy((Upgrade)u);
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Projetos.Tem("bp_bigorna_mestra") && s.Projetos.Tem("bp_bancada_mestra"), "producao completa da os projetos de mestre (desvio, revisar)");
            Assert.AreEqual(BlueprintSourceKind.Milestone, s.Projetos.Posses["bp_bigorna_mestra"].Origem, "a fila (#3) nao existe ainda: a origem e' o marco");

            s.Renome.Ganha(179);
            s.Tick(Dt, 0f, 0f);
            Assert.IsFalse(s.Projetos.Tem("bp_var_escudo_brasao"), "Renome 2");
            s.Renome.Ganha(1);
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Projetos.Tem("bp_var_escudo_brasao") && s.Projetos.Tem("bp_mw_escudo_brasao"), "Renome 3 (RenomeDefs.Niveis[3].Libera)");
            Assert.AreEqual(BlueprintSourceKind.Reputation, s.Projetos.Posses["bp_mw_escudo_brasao"].Origem);

            // conquista: so a RESGATADA (o ID a:{id} no ledger das metas), nunca so a completa
            var c = new Sim(null, null, null, new[] { Def("bp_c", false, new BlueprintSource(BlueprintSourceKind.Achievement, "ach_primeira_venda")) });
            c.Buy(Upgrade.Shields); c.Buy(Upgrade.Tools);   // as metas abrem com as Ferramentas
            c.Metas.Agora(1700000000, 0);
            c.Metas.On(Ev.Sold, (int)Item.Sword, 10);
            c.Tick(Dt, 0f, 0f);
            Assert.IsFalse(c.Projetos.Tem("bp_c"), "completa, sem resgatar");
            Assert.Greater(c.Metas.ResgatarConquista("ach_primeira_venda"), 0L);
            c.Tick(Dt, 0f, 0f);
            Assert.AreEqual(BlueprintSourceKind.Achievement, c.Projetos.Posses["bp_c"].Origem);
        }

        /// <summary>Gancho das encomendas (B-CORE-05): proxima da fila mostrada antes, sem sorteio; o ID da encomenda nao da 2 projetos; o que ja
        /// veio por outra fonte sai da fila; fila esgotada = null.</summary>
        [Test]
        public void Filas_ProximoEGanha_NaOrdem_TxIdempotente_Esgota()
        {
            var s = new Sim();
            Assert.AreEqual("bp_tecnica_espada", s.Projetos.ProximoDaFila(BlueprintSourceKind.OrderBlueprint).Id);
            Assert.AreEqual("bp_bancada_mestra", s.Projetos.ProximoDaFila(BlueprintSourceKind.OrderRoyal).Id);
            Assert.AreEqual("bp_tecnica_espada", s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, ProjetosDefs.OldForge, "ord:1").Id);
            Assert.IsNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, ProjetosDefs.OldForge, "ord:1"), "a mesma encomenda de novo (save/load)");
            Assert.IsNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, ProjetosDefs.OldForge, null), "sem ID de transacao");
            Assert.IsNull(s.Projetos.GanhaDaFila(BlueprintSourceKind.Shop, ProjetosDefs.OldForge, "x"), "so as duas filas");
            s.SoldItems[(int)Item.Sword] = 300;   // a espada longa (#7) vem pelo marco antes
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(BlueprintSourceKind.Milestone, s.Projetos.Posses["bp_rec_espada_longa"].Origem);
            var ordem = new List<string>();
            for (int k = 2; ; k++)
            {
                BlueprintDef d = s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, ProjetosDefs.OldForge, "ord:" + k);
                if (d == null) break;
                ordem.Add(d.Id);
                Assert.AreEqual("ord:" + k, s.Projetos.Posses[d.Id].Tx);
            }
            CollectionAssert.AreEqual(new[] { "bp_tecnica_escudo", "bp_bigorna_mestra", "bp_mw_gume_brasa", "bp_mw_primeira_guarda", "bp_mw_tenaz_eterna" }, ordem,
                "#2..#6; o #7 ja era posse");
            Assert.IsNull(s.Projetos.ProximoDaFila(BlueprintSourceKind.OrderBlueprint), "fila esgotada: a encomenda Blueprint vira Common");
            Assert.IsNull(s.Projetos.ProximoDaFila(BlueprintSourceKind.OrderRoyal, "armorers_row"), "distrito sem fila");
        }

        /// <summary>Niveis IV-V: so com o projeto de mestre (menu, compra direta e cartao); com ele, +15 de qualidade e +5% por nivel na familia toda.</summary>
        [Test]
        public void Desbloqueio_NiveisIVV_SoComOProjetoDaFamilia()
        {
            var s = new Sim { Gold = 1000000 };
            foreach (Upgrade u in new[] { Upgrade.Shields, Upgrade.Tools, Upgrade.HammerSpeed, Upgrade.AnvilsLv2, Upgrade.AnvilsLv3, Upgrade.ShieldsLv2, Upgrade.ShieldsLv3 })
                Assert.IsTrue(s.Buy(u), u.ToString());
            float t3 = s.AnvilA.Time;
            foreach (Upgrade u in NiveisMestre)
            {
                Assert.IsFalse(s.MenuAvailable((int)u), u + ": sem projeto fora do menu");
                Assert.IsFalse(s.TryBuyMenu(u) || s.Buy(u), u + ": nem menu nem compra direta");
            }
            Assert.AreEqual(1000000L, s.Gold);
            Assert.IsTrue(s.Projetos.Ganha("bp_bigorna_mestra", BlueprintSourceKind.OrderBlueprint));
            Assert.IsTrue(s.MenuAvailable((int)Upgrade.AnvilsLv4));
            Assert.IsFalse(s.MenuAvailable((int)Upgrade.AnvilsLv5), "o V exige o IV");
            Assert.IsFalse(s.MenuAvailable((int)Upgrade.ShieldsLv4), "escudos: outro projeto (bancada mestra)");
            Assert.IsTrue(s.TryBuyMenu(Upgrade.AnvilsLv4));
            Assert.AreEqual(1000000L - 800L, s.Gold, "IV = 1/5 do §1.5 (BALANCE.md §26)");
            Assert.IsTrue(s.TryBuyMenu(Upgrade.AnvilsLv5));
            Assert.AreEqual(1000000L - 2300L, s.Gold, "V = 1/6 do §1.5");
            Assert.AreEqual((5, 5, 60), (s.AnvilA.Level, s.AnvilB.Level, s.QualityPoints(s.AnvilA)), "bigornas no nivel V: q 0,60");
            Assert.AreEqual(t3 * 1.10f / 1.20f, s.AnvilA.Time, 1e-4f, "+5% de velocidade por nivel");
            Assert.AreEqual((3, 1500L), (s.ShieldBench.Level, s.T.Cost(Upgrade.AnvilsLv5)));
            long total = 0;
            foreach (Upgrade u in NiveisMestre) { total += s.T.Cost(u); Assert.Greater(s.T.Cost(u), s.T.Cost(Upgrades.All[(int)u].Requires), u + ": mais caro que o nivel anterior"); }
            Assert.AreEqual(12035L, total);
            Assert.IsFalse(Upgrades.IsProduction((int)Upgrade.AnvilsLv4) || Upgrades.IsProduction((int)Upgrade.LongAnvil), "Meta (V14)");
        }

        /// <summary>Espada longa: a bigorna longa (pad, Meta, 6.000) so aparece com o projeto da receita; a linha faz 1 espada longa de 2 lingotes,
        /// sobe com os niveis das bigornas (familia) e vende a 40 no balcao principal.</summary>
        [Test]
        public void EspadaLonga_SoComProjeto_LinhaFazEVende()
        {
            // so a espada longa tem cliente (os outros nao enchem a fila de 4 vagas no meio do teste)
            var s = new Sim(new Tuning { FirstClientAt = 9999f, ClientInterval = new[] { 0f, 0f, 9999f, 9999f, 9999f, 9999f, 12f } }) { Gold = 100000 };
            s.Buy(Upgrade.HammerSpeed); s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools); s.Buy(Upgrade.AnvilsLv2);
            Pad pad = PadFor(s, Upgrade.LongAnvil);
            Assert.AreEqual(-1, pad.Current(s), "sem o projeto o pad nao aparece");
            Assert.IsFalse(s.Buy(Upgrade.LongAnvil));
            Assert.IsFalse(s.LongAnvil.Unlocked || s.LineUnlocked(Item.LongSword));
            Assert.IsTrue(s.Projetos.Ganha("bp_rec_espada_longa", BlueprintSourceKind.OrderBlueprint));
            Assert.AreEqual((int)Upgrade.LongAnvil, pad.Current(s));
            Assert.AreEqual((6000L, false), (s.T.Cost(Upgrade.LongAnvil), s.ProductionComplete));
            Assert.IsTrue(s.Buy(Upgrade.LongAnvil));
            Assert.IsTrue(s.LongAnvil.Unlocked && s.LineUnlocked(Item.LongSword));
            Assert.AreEqual((Item.LongSword, Item.Ingot, 2, 2), (s.LongAnvil.OutItem, s.LongAnvil.InItem, s.LongAnvil.Need, s.LongAnvil.Level), "familia das bigornas: nivel II");
            Assert.AreEqual(s.T.HammerTime1 * 1.5f / 1.05f, s.LongAnvil.Time, 1e-4f);
            Assert.AreEqual((40, 60, 100), (s.PriceOf(Item.LongSword), s.PriceOf(Item.LongSword, Quality.Fine), s.PriceOf(Item.LongSword, Quality.Superior)));
            Assert.IsTrue(s.Sells(s.Counter, Item.LongSword) && !s.Sells(s.JewelShop, Item.LongSword));
            s.LongAnvil.In = 2;
            s.Workers.Clear();
            Run(s, s.LongAnvil.Time + 0.2f);
            Assert.AreEqual((1, 1), (s.LongAnvil.Out, s.Crafted[(int)Item.LongSword]));
            // balcao com espadas longas: o cliente dela chega (12 s no inicio) e compra
            s.Stock[(int)Item.LongSword] = 3;
            long g = s.Gold;
            int vendas = s.SoldItems[(int)Item.LongSword];
            Run(s, 40f);
            Assert.Greater(s.SoldItems[(int)Item.LongSword], vendas, "clientes de espada longa");
            Assert.Greater(s.Gold, g);
            // o ferreiro entrega a espada longa da mao no balcao
            s.Stock[(int)Item.LongSword] = 0; s.Player.Held[(int)Item.LongSword] = 2;
            Assert.AreEqual(Item.LongSword, s.Deliverable(s.Player));
        }

        [Test]
        public void Tecnica_SomaNaFamiliaToda_SemSorteio()
        {
            var s = new Sim();
            s.Buy(Upgrade.Anvil2); s.Buy(Upgrade.Shields);
            Assert.AreEqual(0, s.QualityBreakdown(s.AnvilA)[(int)QualitySource.Blueprint]);
            Assert.IsTrue(s.Projetos.Ganha("bp_tecnica_espada", BlueprintSourceKind.OrderBlueprint));
            foreach (Station st in new[] { s.AnvilA, s.AnvilB, s.LongAnvil })
                Assert.AreEqual((20, 20), (s.QualityBreakdown(st)[(int)QualitySource.Blueprint], s.QualityPoints(st)), st.Name + ": tecnica das bigornas");
            Assert.AreEqual(0, s.QualityPoints(s.ShieldBench), "escudos: outra tecnica");
            Assert.AreEqual(0, s.QualityPoints(s.FurnaceA), "lingote e' intermediario");
            // q 20: exatamente 1 Fina a cada 5 espadas, a mesma sequencia em dois Sims
            string Seq(Sim x)
            {
                x.Workers.Clear();
                var r = new System.Text.StringBuilder();
                for (int i = 0; i < 10; i++)
                {
                    x.AnvilA.In = 1; x.AnvilA.Out = 0; Array.Clear(x.AnvilA.OutQ, 0, x.AnvilA.OutQ.Length);
                    Run(x, x.AnvilA.Time + 0.1f);
                    r.Append(x.AnvilA.OutQ[(int)Quality.Fine]);
                }
                return r.ToString();
            }
            var o = new Sim(); o.Buy(Upgrade.Anvil2); o.Projetos.Ganha("bp_tecnica_espada", BlueprintSourceKind.OrderBlueprint);
            string a = Seq(s), b = Seq(o);
            Assert.AreEqual(a, b, "deterministico");
            Assert.AreEqual(2, a.Count(ch => ch == '1'), "q 20 = 1 Fina a cada 5: " + a);
        }

        [Test]
        public void Save_IdaEVolta_OrigemETx_SaveAntigoSemBp_ERetroativoDosMarcos()
        {
            var s = new Sim();
            s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, ProjetosDefs.OldForge, "ord:7");
            s.Projetos.Ganha("bp_bancada_mestra", BlueprintSourceKind.OrderRoyal);
            s.Projetos.OnVenda((int)Item.Jewel, Quality.Superior); s.Projetos.OnVenda((int)Item.Jewel, Quality.Fine);
            string text = s.Save(10);
            StringAssert.Contains("bp=bp_tecnica_espada:OrderBlueprint:ord:7,bp_bancada_mestra:OrderRoyal\n", text);
            StringAssert.Contains("bpq=joia:2\n", text);
            Sim b = Sim.Load(text);
            Assert.AreEqual((2, "ord:7", BlueprintSourceKind.OrderRoyal, 2), (b.Projetos.Count, b.Projetos.Posses["bp_tecnica_espada"].Tx,
                b.Projetos.Posses["bp_bancada_mestra"].Origem, b.Projetos.Finas[(int)Item.Jewel]));
            Assert.AreEqual(text, b.Save(10), "save estavel");
            Assert.IsNull(b.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, ProjetosDefs.OldForge, "ord:7"), "a transacao volta com o save: nao paga 2x");

            // save antigo (0.1-0.8, sem bp=/bpq=): nenhum projeto; o 1o Tick da o retroativo dos marcos que o save ja tem
            string antigo = string.Join("\n", text.Split('\n').Where(l => !l.StartsWith("bp=") && !l.StartsWith("bpq="))).Replace("solds=", "solds=espada:120,");
            Sim c = Sim.Load(antigo);
            Assert.AreEqual((0, 0), (c.Projetos.Count, c.Projetos.Finas[(int)Item.Jewel]));
            c.Tick(Dt, 0f, 0f);
            Assert.IsTrue(c.Projetos.Tem("bp_mw_primeira_lamina"), "120 espadas vendidas antes do B-CORE-07");
            Assert.AreEqual(1, Count(c, Ev.Blueprint));

            // lixo e downgrade: ID que o catalogo nao tem fica possuido (nunca se perde) e conta; origem lixo = Milestone
            Sim d = Sim.Load(text.Replace("bp=bp_tecnica_espada:OrderBlueprint:ord:7,", "bp=,bp_forja_runica:Shop:iap:9,bp_tecnica_espada:Xyz,"));
            Assert.AreEqual((3, 1, BlueprintSourceKind.Milestone), (d.Projetos.Count, d.UnknownIds, d.Projetos.Posses["bp_tecnica_espada"].Origem));
            StringAssert.Contains("bp_forja_runica:Shop:iap:9", d.Save(10), "regravar mantem a posse do build mais novo");
        }

        /// <summary>Validator: cada regra vermelha com um defeito numa copia; o caso do ticket e' o essencial so pago.</summary>
        [Test]
        public void Validator_EssencialSoPago_EAsOutrasRegras_Acusa()
        {
            BlueprintSource loja = new BlueprintSource(BlueprintSourceKind.Shop, "pack_1"), evento = new BlueprintSource(BlueprintSourceKind.Event, "ev_x"),
                contrato = new BlueprintSource(BlueprintSourceKind.Contract, "ctr_x"), marco = new BlueprintSource(BlueprintSourceKind.Milestone, "espada", 10);
            var a = new List<BlueprintDef>(ProjetosDefs.All)
            {
                Def("bp_so_pago", true, loja),
                Def("bp_essencial_contrato", true, contrato, loja),
                Def("bp_so_evento", false, evento),
                Def("bp_premium", false, marco, new BlueprintSource(BlueprintSourceKind.PassPremium, "pass_s1")),
                Def("bp_tecnica_espada", false, marco),
                Def("Bp_Maiuscula", false, marco),
                Def("bp_marco_lingote", false, new BlueprintSource(BlueprintSourceKind.Milestone, "lingote", 10)),
                Def("bp_fila_repetida", false, new BlueprintSource(BlueprintSourceKind.OrderBlueprint, ProjetosDefs.OldForge, 3)),
                Def("bp_renome_errado", false, new BlueprintSource(BlueprintSourceKind.Reputation, null, 11)),
                Def("bp_conquista", false, new BlueprintSource(BlueprintSourceKind.Achievement, "ach_nao_existe")),
                new BlueprintDef("bp_outro_distrito", "artisan_quarter", BlueprintKind.Variant, new[] { "espada" }, false, marco),
                new BlueprintDef("bp_familia", ProjetosDefs.OldForge, BlueprintKind.Technique, new[] { "forja_runica" }, false, marco),
                new BlueprintDef("bp_receita", ProjetosDefs.OldForge, BlueprintKind.Recipe, new[] { "espada" }, false, marco),
            };
            List<string> v = Valida(a.ToArray());
            Acusa(v, "blueprint bp_so_pago: essencial sem fonte gratis permanente (receita essencial nunca so paga)");
            Acusa(v, "blueprint bp_so_pago: so com fonte de evento, Pass ou loja");
            Acusa(v, "blueprint bp_essencial_contrato: essencial sem fonte gratis permanente");
            Acusa(v, "blueprint bp_so_evento: so com fonte de evento, Pass ou loja");
            Acusa(v, "blueprint bp_premium: Pass premium nunca da Blueprint (V20)");
            Acusa(v, "blueprint bp_tecnica_espada: ID repetido");
            Acusa(v, "blueprint Bp_Maiuscula: ID fora do formato");
            Acusa(v, "blueprint bp_marco_lingote: marco \"lingote\" x10 inalcancavel");
            Acusa(v, "blueprint bp_fila_repetida: posicao #3 repetida na fila OrderBlueprint de old_forge");
            Acusa(v, "blueprint bp_renome_errado: Renome 11");
            Acusa(v, "blueprint bp_conquista: conquista \"ach_nao_existe\" nao existe");
            Acusa(v, "blueprint bp_outro_distrito: distrito \"artisan_quarter\" nao existe");   // C-CORE-05: a Row tem projetos
            Acusa(v, "blueprint bp_familia: Technique \"forja_runica\" nao existe no jogo");
            Acusa(v, "blueprint bp_receita: Recipe \"espada\" nao existe no jogo (ou nenhum upgrade pede o projeto)");

            // Renome que nao bate com a tabela do Renome (o nivel 3 libera bp_var_escudo_brasao)
            BlueprintDef[] r = Array.ConvertAll(ProjetosDefs.All, d => d.Id == "bp_var_escudo_brasao"
                ? new BlueprintDef(d.Id, d.District, d.Kind, d.Targets, d.Essential, new BlueprintSource(BlueprintSourceKind.Reputation, null, 2)) : d);
            Acusa(Valida(r), "blueprint bp_var_escudo_brasao: Renome 2 (a tabela do Renome libera no 3)");

            // upgrade: projeto que nao existe, do tipo errado, e nivel IV sem projeto
            var ups = (UpgradeDef[])Upgrades.All.Clone();
            UpgradeDef l4 = ups[(int)Upgrade.AnvilsLv4], l5 = ups[(int)Upgrade.AnvilsLv5], la = ups[(int)Upgrade.LongAnvil];
            ups[(int)Upgrade.AnvilsLv4] = new UpgradeDef(l4.Up, l4.Id, l4.Requires, l4.Name, l4.Desc, menu: true, station: "bigorna");
            ups[(int)Upgrade.AnvilsLv5] = new UpgradeDef(l5.Up, l5.Id, l5.Requires, l5.Name, l5.Desc, menu: true, station: "bigorna", blueprint: "bp_tecnica_espada");
            ups[(int)Upgrade.LongAnvil] = new UpgradeDef(la.Up, la.Id, la.Requires, la.Name, la.Desc, meta: true, blueprint: "bp_sumiu");
            v = Valida(ProjetosDefs.All, ups);
            Acusa(v, "blueprint: AnvilsLv4 (nivel IV+) sem projeto");
            Acusa(v, "blueprint: AnvilsLv5 exige \"bp_tecnica_espada\", que nao libera nivel de bigorna");
            Acusa(v, "blueprint: LongAnvil exige \"bp_sumiu\", que nao existe");
            Acusa(v, "blueprint bp_rec_espada_longa: Recipe \"espada_longa\" nao existe no jogo (ou nenhum upgrade pede o projeto)");
            Assert.IsEmpty(Valida(ProjetosDefs.All), "o conteudo real passa");
        }

        /// <summary>View (Textos, puro): o cartao IV-V fica oculto sem o projeto; o painel diz o que e', o que libera e de onde vem (com o progresso
        /// do marco), sem chave faltando nos 3 idiomas.</summary>
        [Test]
        public void Textos_CartaoOcultoSemProjeto_EOPainel()
        {
            try
            {
                Loc.Atual = Idioma.Pt;
                var s = new Sim { Gold = 100000 };
                foreach (Upgrade u in new[] { Upgrade.Shields, Upgrade.Tools, Upgrade.AnvilsLv2, Upgrade.AnvilsLv3 }) s.Buy(u);
                Assert.AreEqual(Cartao.Oculto, Textos.Estado(s, (int)Upgrade.AnvilsLv4), "sem o projeto");
                s.Projetos.Ganha("bp_bigorna_mestra", BlueprintSourceKind.OrderBlueprint);
                Assert.AreEqual((Cartao.Disponivel, Cartao.Travado), (Textos.Estado(s, (int)Upgrade.AnvilsLv4), Textos.Estado(s, (int)Upgrade.AnvilsLv5)));
                s.SoldItems[(int)Item.Sword] = 37;
                BlueprintDef D(string id) => ProjetosDefs.All[Idx(id)];
                Assert.AreEqual(("Obra-prima: Primeira Lâmina", "Venda 100 espadas (37/100)"), (Textos.ProjetoNome(s, D("bp_mw_primeira_lamina")), Textos.ProjetoFonte(s, D("bp_mw_primeira_lamina"))));
                Assert.AreEqual(("Mestre: escudos, ferramentas, joias", "Encomenda real nº 1 ou Produção completa"),
                    (Textos.ProjetoNome(s, D("bp_bancada_mestra")), Textos.ProjetoFonte(s, D("bp_bancada_mestra"))));
                Assert.AreEqual(("Receita: espada longa", "Libera a linha de espadas longas"), (Textos.ProjetoNome(s, D("bp_rec_espada_longa")), Textos.ProjetoLibera(s, D("bp_rec_espada_longa"))));
                Assert.AreEqual(("Técnica: ferramentas", "+0,20 de qualidade na família"), (Textos.ProjetoNome(s, D("bp_tecnica_ferramenta")), Textos.ProjetoLibera(s, D("bp_tecnica_ferramenta"))));
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    foreach (BlueprintDef d in ProjetosDefs.All)
                    {
                        string t = Textos.ProjetoNome(s, d) + Textos.ProjetoLibera(s, d) + Textos.ProjetoFonte(s, d);
                        StringAssert.DoesNotContain("[", t, $"{i}: {d.Id}");
                    }
                }
            }
            finally { Loc.Atual = Idioma.Pt; }
        }

        /// <summary>GDD §23 #8: nenhum produtivo exige projeto (a producao completa nunca fica presa a Blueprint, pago ou nao); o bot F2P chega aos 3
        /// essenciais e usa os projetos (compra nivel IV-V) sem anuncio nem compra.</summary>
        [Test]
        public void Bot_F2P_ChegaAosEssenciais_EUsaOsProjetos()
        {
            Assert.IsTrue(Upgrades.All.All(u => !Upgrades.IsProduction((int)u.Up) || u.Blueprint == null), "produtivo nunca exige projeto");
            var s = new Sim(); var bot = new Bot();
            for (float t = 0f; t < 60f * 60f; t += Dt) bot.Step(s, Dt);
            TestContext.WriteLine("projetos aos 60 min: " + string.Join(", ", s.Projetos.Posses.Keys));
            Assert.IsTrue(s.ProductionComplete, "premissa");
            foreach (BlueprintDef d in ProjetosDefs.All.Where(x => x.Essential)) Assert.IsTrue(s.Projetos.Tem(d.Id), d.Id + ": essencial alcancavel no F2P");
            Assert.IsTrue(s.Bought[(int)Upgrade.AnvilsLv4], "o bot compra nivel IV quando o projeto chega");
            Assert.IsTrue(s.Projetos.Posses.Values.All(p => !ProjetosDefs.Paga(p.Origem)), "nada pago");
        }
    }
}
