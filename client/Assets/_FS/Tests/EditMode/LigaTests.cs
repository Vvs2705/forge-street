using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// C-CORE-05 (CONTEUDO_V1 §2.2 regra 2, §5.2, §5.7, §6, §9, §10; BALANCE.md §35): a liga na Rua dos Armeiros. A fornalha de liga faz 2
    /// lingotes -> 1 liga; toda montagem e forja tem o slot opcional (a peca nao espera por ele) e a peca com liga soma q +30 (Material) e conta
    /// em PecasComLiga (o AlloyPieces do catalogo, no save); a Vulcana (Smelter) se contrata em ouro depois da fornalha, acelera a fornalha de
    /// liga e da qualidade as montagens; os niveis II-V da Row (IV-V com o projeto de mestre); as 10 obras e o Charter da Row (2 obras) pelo
    /// caminho de sempre. NUnit 3.5 do Unity: tipos exatos nas tuplas (long com L).
    /// </summary>
    public class LigaTests
    {
        const float Dt = 1f / 30f;
        static readonly DistrictDef Row = Distritos.ArmorersRow;

        static Sim NovaRow() => new Sim(distrito: Row);
        static int U(Sim s, string id) => Upgrades.IndexOf(id, s.UpgradeDefs);
        static Item I(Sim s, string id) => (Item)Items.IndexOf(id, s.ItemDefs);
        static void Compra(Sim s, params string[] ids) { foreach (string id in ids) Assert.IsTrue(s.Buy((Upgrade)U(s, id)), id); }
        static int Q(Sim s, Station st, QualitySource f) => s.QualityBreakdown(st)[(int)f];
        static int K(Sim s, string id) => WorkerDefs.IndexOf(id, s.Equipe.Defs);
        static void Producao(Sim s) { for (int u = 0; u < s.UpgradeDefs.Length; u++) if (s.IsProduction(u)) s.Buy((Upgrade)u); }

        /// <summary>Uma peca de `st` com a receita na entrada (e 1 liga se `liga`), ate sair; devolve o nivel dela.</summary>
        static Quality Peca(Sim s, Station st, bool liga)
        {
            st.Out = 0; Array.Clear(st.OutQ, 0, st.OutQ.Length);
            for (int k = 0; k < st.Ins.Length; k++) st.Ins[k] = st.Recipe.Inputs[k].Qty;
            if (liga) st.Ins[st.Liga] = 1;
            for (int i = 0; i < 4000 && st.Out == 0; i++) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(1, st.Out, "a bancada fez a peca");
            return st.OutQ[(int)Quality.Superior] > 0 ? Quality.Superior : st.OutQ[(int)Quality.Fine] > 0 ? Quality.Fine : Quality.Normal;
        }

        /// <summary>Golpe de mestre numa peca com liga que o acumulador ja manda Superior (QAcc 99): Masterwork se ha obra pendente.</summary>
        static Quality GolpeComLiga(Sim s, Station st)
        {
            st.Out = 0; Array.Clear(st.OutQ, 0, st.OutQ.Length);
            for (int k = 0; k < st.Ins.Length; k++) st.Ins[k] = st.Recipe.Inputs[k].Qty;
            st.Ins[st.Liga] = 1; st.QAcc = 99;
            s.StrikeCooldown = 0f; s.Player.Pos = st.InAt;
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

        // ------------------------------------------------------------------ liga

        /// <summary>§5.2: a fornalha de liga e' produtiva (tier 11, 13.000 = 8.600 x 1,55), pede a Fornalha 2 e abre a estacao; 2 lingotes -> 1
        /// liga em 6 s (1,5 x a fornalha); o Fole de couro tambem a acelera (e' fornalha).</summary>
        [Test]
        public void FornalhaDeLiga_DoisLingotesUmaLigaEmSeisSegundos()
        {
            Sim s = NovaRow();
            Station f = s.Est("fornalha_liga");
            int u = U(s, "fornalha_liga");
            Assert.AreEqual((11, 13000L, "fornalha_armeiro2", true), (u, s.T.Cost(u), s.UpgradeDefs[s.UpgradeDefs[u].Requires].Id, s.IsProduction(u)));
            Assert.IsFalse(f.Unlocked);
            Pad pad = s.Pads.Find(x => Array.IndexOf(x.Chain, (Upgrade)u) >= 0);
            Assert.AreEqual((-1, f.Pos.X, f.Pos.Y), (pad.Current(s), pad.Pos.X, pad.Pos.Y), "pede a Fornalha 2; o pad e' no lugar da estacao");
            Compra(s, "fornalha_armeiro2");
            Assert.AreEqual(u, pad.Current(s));
            Compra(s, "fornalha_liga");
            Assert.AreEqual((true, I(s, "liga"), 6f), (f.Unlocked, f.OutItem, f.Time));
            f.In = 2;
            for (float t = 0f; t < 6.05f; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((1, 0), (f.Out, f.In));
            Compra(s, "fole_couro");
            Assert.AreEqual(3.75f, f.Time, 1e-4f, "o Fole de couro: 2,5 x 1,5");
            Assert.IsTrue(s.Intermediario(I(s, "liga")), "o papel 1 (Ferraz 2) e o ferreiro levam a liga");
        }

        /// <summary>§2.2 regra 2: toda montagem e forja tem o slot (Qty 0); a peca nao espera por ele; com liga no slot o QualityBreakdown mostra
        /// Material +30 e a peca leva a liga quando sai (PecasComLiga +1); sem liga, nada muda. q 30 = 1 Fina a cada 4 pecas com liga, sem sorteio.</summary>
        [Test]
        public void Montagem_LigaOpcional_Q30EConta()
        {
            Sim s = NovaRow();
            foreach (Station st in s.Stations.Where(x => x.Produces && s.IsProduct(x.OutItem)))
                Assert.AreEqual(I(s, "liga"), st.Recipe.Inputs[st.Liga].Item, st.Id + ": slot de liga");
            Assert.AreEqual((-1, -1), (s.Est("prensa").Liga, s.Est("fornalha").Liga), "insumo nao tem slot");
            Station m = s.Est("montagem_elmo");
            Item elmo = I(s, "elmo");
            int placa = m.InIndex(I(s, "placa")), rebite = m.InIndex(I(s, "rebite"));
            m.Ins[placa] = 2; m.Ins[rebite] = 2;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(m.Busy, "sem liga: comeca do mesmo jeito");
            Assert.AreEqual(0, Q(s, m, QualitySource.Material));
            m.Ins[m.Liga] = 1;   // chegou durante a peca: vale para ela (a barra ja mostra)
            Assert.AreEqual((30, 30), (Q(s, m, QualitySource.Material), s.QualityPoints(m)));
            for (int i = 0; i < 400 && m.Out == 0; i++) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual((1, 0, 1, 30), (m.Out, m.Ins[m.Liga], s.PecasComLiga[(int)elmo], m.QAcc), "a liga saiu com a peca");
            Assert.AreEqual(Quality.Normal, Peca(s, m, false), "sem liga: q 0");
            Assert.AreEqual((1, 30), (s.PecasComLiga[(int)elmo], m.QAcc), "nada muda sem liga");
            Assert.AreEqual(Quality.Normal, Peca(s, m, true));
            Assert.AreEqual(Quality.Normal, Peca(s, m, true));
            Assert.AreEqual(Quality.Fine, Peca(s, m, true), "a 4a com liga: 30 x 4 = 120");
            Assert.AreEqual(4, s.PecasComLiga[(int)elmo]);
        }

        /// <summary>O contador vai e volta no save (linha liga=, so com alguma peca); o save do Old Forge nao muda (nenhuma estacao tem slot).</summary>
        [Test]
        public void PecasComLiga_SaveEOldForgeIntocado()
        {
            Sim s = NovaRow();
            s.PecasComLiga[(int)I(s, "manoplas")] = 50; s.PecasComLiga[(int)I(s, "elmo")] = 7;
            string txt = s.Save(0);
            StringAssert.Contains("liga=elmo:7,manoplas:50\n", txt);
            Sim l = Sim.Load(txt, distrito: Row);
            Assert.AreEqual((50, 7), (l.PecasComLiga[(int)I(l, "manoplas")], l.PecasComLiga[(int)I(l, "elmo")]));
            Assert.AreEqual(0, Sim.Load(NovaRow().Save(0), distrito: Row).PecasComLiga.Sum(), "sem a linha: zero");
            var of = new Sim();
            Assert.IsFalse(of.Stations.Exists(x => x.Liga >= 0));
            StringAssert.DoesNotContain("liga=", of.Save(0));
        }

        // ------------------------------------------------------------------ Vulcana

        /// <summary>§6: a Vulcana (Smelter) se contrata em ouro (7.200) com a fornalha de liga, como a equipe do Old Forge; opera a fornalha de
        /// liga (6 -> 4 s, +3%/nivel) e da q +15 nas montagens (25 no nivel 5, 30 no 10), nunca nas forjas; nivel 10, no save.</summary>
        [Test]
        public void Vulcana_ContrataEmOuro_FornalhaEMontagens()
        {
            Sim s = NovaRow();
            int k = K(s, "wk_vulcana");
            WorkerDef d = s.Equipe.Defs[k];
            Assert.AreEqual((WorkerRole.Smelter, 7200L, 10, "fornalha_liga", "montagem"), (d.Papel, d.Contratacao, d.MaxNivel, d.Requer, d.Station));
            Assert.IsFalse(s.Equipe.AVenda(k), "antes da fornalha de liga");
            Compra(s, "fornalha_armeiro2", "fornalha_liga", "bigorna_pesada");
            Assert.AreEqual((true, 7200L), (s.Equipe.AVenda(k), s.Equipe.Custo(k)));
            s.Gold = 7199;
            Assert.IsFalse(s.Equipe.Comprar(k));
            s.Gold = 7200;
            Assert.IsTrue(s.Equipe.Comprar(k));
            Assert.AreEqual((0L, 1), (s.Gold, s.Equipe.Nivel(k)));
            Station m = s.Est("montagem_peitoral"), p = s.Est("bigorna_pesada"), f = s.Est("fornalha_liga"), forn = s.Est("fornalha");
            Assert.AreEqual((15, 0), (Q(s, m, QualitySource.Worker), Q(s, p, QualitySource.Worker)));
            Assert.AreEqual((4f, 4f), (f.Time, forn.Time), "a fornalha de liga 6 -> 4 s; a de minerio nao muda");
            for (int n = 2; n <= 10; n++)
            {
                s.Gold = s.Equipe.Custo(k);
                Assert.IsTrue(s.Equipe.Comprar(k), "nivel " + n);
                if (n == 5) Assert.AreEqual(25, Q(s, m, QualitySource.Worker));
            }
            Assert.AreEqual((10, 30, -1L), (s.Equipe.Nivel(k), Q(s, m, QualitySource.Worker), s.Equipe.Custo(k)));
            Assert.AreEqual(6f / 1.77f, f.Time, 1e-4f, "+3% por nivel acima do 1");
            Sim l = Sim.Load(s.Save(0), distrito: Row);
            Assert.AreEqual((10, 30), (l.Equipe.Nivel(k), Q(l, l.Est("montagem_elmo"), QualitySource.Worker)));
        }

        // ------------------------------------------------------------------ niveis e projetos

        /// <summary>§5.7: niveis da montagem e da forja de armas no menu, Meta (fora da producao completa), +15 por nivel na familia toda; o II
        /// pede a Rebitadeira 2, o ultimo produtivo (desvio, BALANCE.md §35: os niveis sao o ralo do rabo da Row); IV-V so com o
        /// bp_mestra_armorers, que a producao completa da (fonte gratis permanente; a fila Royal da Row e' a outra).</summary>
        [Test]
        public void Niveis_DaRow_IIaVComOProjetoDeMestre()
        {
            Sim s = NovaRow();
            int l2 = U(s, "nivel_montagem2"), f2 = U(s, "nivel_forja_armas2"), l4 = U(s, "nivel_montagem4");
            Assert.AreEqual((4300L, 13000L, 35000L, 78000L, 64000L), (s.T.Cost(l2), s.T.Cost(l2 + 1), s.T.Cost(l4), s.T.Cost(l4 + 1), s.T.Cost(U(s, "nivel_forja_armas5"))));
            Compra(s, "fole_couro", "bigorna_pesada", "fornalha_armeiro2", "fornalha_liga");
            Assert.AreEqual((false, false, false), (s.MenuAvailable(l2), s.MenuAvailable(f2), s.IsProduction(l2)), "nada de nivel no meio da producao");
            Compra(s, "rebitadeira2");
            Assert.AreEqual((true, true), (s.MenuAvailable(l2), s.MenuAvailable(f2)));
            Compra(s, "nivel_montagem2", "nivel_montagem3");
            Station m = s.Est("montagem_elmo"), g = s.Est("montagem_peitoral"), p = s.Est("bigorna_pesada");
            Assert.AreEqual((3, 3, 1, 30), (m.Level, g.Level, p.Level, Q(s, m, QualitySource.StationLevel)));
            Assert.IsFalse(s.Buy((Upgrade)l4), "IV sem o projeto de mestre");
            Producao(s);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(BlueprintSourceKind.Milestone, s.Projetos.Posses["bp_mestra_armorers"].Origem, "a producao completa da o projeto");
            Compra(s, "nivel_montagem4", "nivel_montagem5", "nivel_forja_armas2", "nivel_forja_armas3", "nivel_forja_armas4", "nivel_forja_armas5");
            Assert.AreEqual((5, 5, 60, 60), (m.Level, p.Level, Q(s, m, QualitySource.StationLevel), Q(s, p, QualitySource.StationLevel)));
            Assert.IsTrue(s.ProductionComplete, "os niveis sao Meta");
        }

        /// <summary>§9: a fila de encomenda Blueprint da Row da as tecnicas (#1 montagem, #2 forja de armas: q +20 na familia) e o projeto do
        /// peitoral do bastiao (#3); a fila Royal, o de mestre (#1). O Old Forge nao ve as filas da Row.</summary>
        [Test]
        public void Filas_DaRow_TecnicasEMestre()
        {
            Sim s = NovaRow();
            Assert.AreEqual("bp_tecnica_montagem", s.Projetos.ProximoDaFila(BlueprintSourceKind.OrderBlueprint, Row.Id)?.Id);
            Assert.AreEqual("bp_mestra_armorers", s.Projetos.ProximoDaFila(BlueprintSourceKind.OrderRoyal, Row.Id)?.Id);
            Assert.AreEqual("bp_tecnica_montagem", s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, Row.Id, "enc:1")?.Id);
            Assert.AreEqual("bp_tecnica_forja_armas", s.Projetos.GanhaDaFila(BlueprintSourceKind.OrderBlueprint, Row.Id, "enc:2")?.Id);
            Assert.AreEqual("bp_mw_peitoral_bastiao", s.Projetos.ProximoDaFila(BlueprintSourceKind.OrderBlueprint, Row.Id)?.Id);
            Assert.AreEqual((20, 20), (Q(s, s.Est("montagem_elmo"), QualitySource.Blueprint), Q(s, s.Est("forja_hastes"), QualitySource.Blueprint)));
            Assert.IsNull(new Sim().Projetos.ProximoDaFila(BlueprintSourceKind.OrderRoyal, Row.Id), "o Old Forge nao tem os projetos da Row");
        }

        // ------------------------------------------------------------------ obras e Charter

        /// <summary>
        /// Criterio do ticket: o Charter da Row (producao completa + Renome 5 + 2 obras DA ROW) e' alcancavel sem nada pago e sem injetar
        /// conteudo. Na Row de verdade: producao completa, montagem V, a Vulcana nivel 1 e a liga (q 60 + 15 + 30 = 105: Superior pelo
        /// acumulador); os projetos pelos marcos (100 elmos, 150 peitorais); golpe numa Superior com liga = Elmo do Primeiro Vigia e Peitoral do
        /// Bastiao no Hall da Row, Renome pelo ID; o Charter sai 1x.
        /// </summary>
        [Test]
        public void Charter_DaRow_DuasObrasPeloCaminhoDeSempre()
        {
            Imperio imp = Imperio.Chegada(Row);
            Sim s = imp.Ativo;
            Producao(s);
            s.Tick(Dt, 0f, 0f);   // o projeto de mestre (marco da producao)
            Compra(s, "nivel_montagem2", "nivel_montagem3", "nivel_montagem4", "nivel_montagem5");
            int k = K(s, "wk_vulcana");
            s.Gold = 7200;
            Assert.IsTrue(s.Equipe.Comprar(k));
            Station elmo = s.Est("montagem_elmo"), peit = s.Est("montagem_peitoral");
            elmo.Ins[elmo.Liga] = 1;
            Assert.AreEqual((60, 15, 30, 105), (Q(s, elmo, QualitySource.StationLevel), Q(s, elmo, QualitySource.Worker), Q(s, elmo, QualitySource.Material), s.QualityPoints(elmo)));
            Assert.AreEqual("renome", imp.CharterFalta(Row.Id));
            imp.Raiz.Renome.Ganha(750 - imp.Raiz.Renome.Total);
            Assert.AreEqual("masterwork", imp.CharterFalta(Row.Id));
            s.SoldItems[(int)I(s, "elmo")] = 100; s.SoldItems[(int)I(s, "peitoral")] = 150;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Projetos.Tem("bp_mw_elmo_vigia") && s.Projetos.Tem("bp_mw_peitoral_bastiao"), "os marcos de venda dao os 2 projetos essenciais");
            long renome = imp.Raiz.Renome.Total;
            Assert.AreEqual(Quality.Masterwork, GolpeComLiga(s, elmo));
            Assert.AreEqual(Quality.Masterwork, GolpeComLiga(s, peit));
            Assert.AreEqual((2, true, true), (s.Hall.Count, s.Hall.Tem("mw_elmo_vigia"), s.Hall.Tem("mw_peitoral_bastiao")));
            Assert.IsTrue(imp.Raiz.Renome.Total >= renome + 65, "30 + 35 de Renome das obras (e os marcos da Collection da Row)");
            Assert.IsNull(imp.CharterFalta(Row.Id));
            Assert.IsTrue(imp.Charter(Row.Id));
            Assert.IsFalse(imp.Charter(Row.Id), "1x");
        }

        /// <summary>As condicoes novas no Hall: AlloyPieces (PecasComLiga) e Vulcana nivel 5 contam pelo contador do nucleo, nunca pago.</summary>
        [Test]
        public void Obras_CondicoesDeLigaEVulcana()
        {
            Sim s = NovaRow();
            MasterworkDef man = Array.Find(s.Hall.Defs, d => d.Id == "mw_manoplas_vivas"), juiz = Array.Find(s.Hall.Defs, d => d.Id == "mw_maca_juiz");
            Assert.AreEqual((CondicaoMw.PecasComLiga, 50, 0L), (man.Condicoes[0].Kind, man.Condicoes[0].N, s.Hall.Valor(man.Condicoes[0])));
            s.PecasComLiga[(int)I(s, "manoplas")] = 50;
            Assert.AreEqual(50L, s.Hall.Valor(man.Condicoes[0]));
            Assert.IsTrue(s.Projetos.Ganha("bp_mw_manoplas_vivas", BlueprintSourceKind.Milestone));
            Assert.IsTrue(s.Hall.Liberada(man));
            Assert.AreEqual(0L, s.Hall.Valor(juiz.Condicoes[0]));
            Compra(s, "fornalha_armeiro2", "fornalha_liga");
            int k = K(s, "wk_vulcana");
            for (int n = 1; n <= 5; n++) { s.Gold = s.Equipe.Custo(k); s.Equipe.Comprar(k); }
            Assert.AreEqual(5L, s.Hall.Valor(juiz.Condicoes[0]));
        }

        /// <summary>Foto do coordenador (-distrito armorers_row -equipe): na Row, os produtivos dela (a fornalha de liga), a turma Ferraz no
        /// nivel 3 e a Vulcana contratada; o ouro volta ao que era.</summary>
        [Test]
        public void Fotos_EquipeNaRow()
        {
            Sim s = Imperio.Chegada(Row).Ativo;
            s.Gold = 123;
            Captura.Equipe(s);
            Assert.AreEqual((true, true, 123L), (s.ProductionComplete, s.Est("fornalha_liga").Unlocked, s.Gold));
            Assert.AreEqual((1, 3), (s.Equipe.Nivel(K(s, "wk_vulcana")), s.Equipe.Nivel(K(s, "wk_ferraz4"))));
        }

        // ------------------------------------------------------------------ validator e textos

        /// <summary>As regras novas do validator: o slot (Qty 0) so 1 por receita, so em produto e ao lado de um obrigatorio; o fundidor precisa
        /// da fornalha que opera (o Requer) e de uma familia com bancada de produto.</summary>
        [Test]
        public void Validator_SlotDeLigaEFundidor()
        {
            RecipeDef[] rs = Row.Receitas.Select(r => r.Output == I(NovaRow(), "elmo")
                ? new RecipeDef(r.Station, r.Family, r.Kind, r.Output, r.TimeMul, r.Inputs.Concat(new[] { (I(NovaRow(), "couro"), 0) }).ToArray())
                : r.Station == "prensa" ? new RecipeDef("prensa", Kind.Furnace, r.Output, r.TimeMul, (Item.Ingot, 1), (I(NovaRow(), "liga"), 0)) : r).ToArray();
            var w = new Sim(recipes: rs, distrito: Row);
            for (int u = 0; u < w.Bought.Length; u++) w.Bought[u] = true;
            w.Recompute();
            List<string> v = ContentValidator.Receitas(w.T.Price, w).ToList();
            RowTests.Acusa(v, "receita: elmo com insumo opcional (0) fora da regra");
            RowTests.Acusa(v, "receita: placa com insumo opcional (0) fora da regra");
            Assert.IsEmpty(ContentValidator.Receitas(ContentValidator.Mundo(Row).T.Price, ContentValidator.Mundo(Row)).ToList(), "a Row real passa");

            Sim mundo = ContentValidator.Mundo(Row);
            var torto = new WorkerDef("wk_vulcana", "Vulcana", WorkerRole.Smelter, Specialty.Armor, Row.Id, "fornalha", null, -1, 7200, "prensa2",
                new long[] { 1800 }, "oficial", "liga");
            v = ContentValidator.Workers(new[] { torto }, mundo, new RenomeDef[0]).ToList();
            RowTests.Acusa(v, "worker wk_vulcana: fundidor da familia \"fornalha\", sem bancada de produto");
            RowTests.Acusa(v, "worker wk_vulcana: fundidor sem a fornalha \"prensa2\"");
            Assert.IsEmpty(ContentValidator.Workers(Row.Workers, mundo, new RenomeDef[0]).ToList());
        }

        /// <summary>DM §109: tudo o que a liga trouxe tem texto nos 3 idiomas e cabe (melhorias e itens no DistritosTests; aqui as obras e a
        /// condicao no Hall, os projetos, os marcos da Collection e o cartao da Vulcana em todo nivel).</summary>
        [Test]
        public void Textos_DaLigaNosTresIdiomas()
        {
            Sim s = NovaRow();
            Compra(s, "fornalha_armeiro2", "fornalha_liga");
            int k = K(s, "wk_vulcana");
            try
            {
                Textos.Distrito(Row);
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    foreach (MasterworkDef d in s.Hall.Defs)
                    {
                        foreach (string t in new[] { Textos.NomeMasterwork(d), Textos.CondicaoMasterwork(s, d), Textos.ProjetoMasterwork(s, d) })
                            StringAssert.DoesNotContain("[", t, $"{i}: {d.Id}");
                        Assert.LessOrEqual(Textos.NomeMasterwork(d).Length, Textos.HallMax, $"{i}: {d.Id}");
                        Assert.LessOrEqual(Textos.CondicaoMasterwork(s, d).Length, Textos.HallDetalheMax, $"{i}: {Textos.CondicaoMasterwork(s, d)}");
                    }
                    foreach (BlueprintDef d in s.Projetos.Defs)
                        StringAssert.DoesNotContain("[", Textos.ProjetoNome(s, d) + Textos.ProjetoLibera(s, d) + Textos.ProjetoFonte(s, d), $"{i}: {d.Id}");
                    foreach (MarcoColecaoDef m in s.Colecao.Marcos)
                        foreach (string t in new[] { Textos.MarcoColecao(s, m), Textos.PremioColecao(m), Textos.SeloColecao(s, m) })
                        {
                            StringAssert.DoesNotContain("[", t, $"{i}: {m.Id}");
                            Assert.LessOrEqual(t.Length, Textos.PremioMax, $"{i}: {t}");
                        }
                    Sim t2 = NovaRow();
                    Compra(t2, "fornalha_armeiro2");
                    Assert.LessOrEqual(Textos.PrecisaWorker(t2, k).Length, 18, $"{i}: {Textos.PrecisaWorker(t2, k)}");
                    for (int n = 0; n < s.Equipe.Defs[k].MaxNivel; n++)
                    {
                        Sim c = NovaRow();
                        Compra(c, "fornalha_armeiro2", "fornalha_liga");
                        for (int j = 0; j < n; j++) { c.Gold = c.Equipe.Custo(k); c.Equipe.Comprar(k); }
                        foreach (string t in new[] { Textos.BeneficioWorker(c, k), Textos.AntesDepoisWorker(c, k), Textos.AvisoWorker(c.Equipe.Defs[k], n + 1) })
                            StringAssert.DoesNotContain("[", t, $"{i}: nivel {n + 1}");
                        Assert.LessOrEqual(Textos.BeneficioWorker(c, k).Length, 2 * 14, $"{i}: {Textos.BeneficioWorker(c, k)}");
                        Assert.LessOrEqual(Textos.AntesDepoisWorker(c, k).Length, 16, $"{i}: {Textos.AntesDepoisWorker(c, k)}");
                        Assert.LessOrEqual(Textos.AvisoWorker(c.Equipe.Defs[k], n + 1).Length, Textos.AvisoMax, $"{i}");
                    }
                }
                Loc.Atual = Idioma.Pt;
                MasterworkDef man = Array.Find(s.Hall.Defs, d => d.Id == "mw_manoplas_vivas");
                Assert.AreEqual("Faça 50 manoplas com liga (0/50)", Textos.CondicaoMasterwork(s, man));
                Assert.AreEqual("Montagens nível 5 (1/5)", Textos.CondicaoMasterwork(s, Array.Find(s.Hall.Defs, d => d.Id == "mw_peitoral_bastiao")));
                Assert.AreEqual(("Técnica: montagens", "Mestre: montagens, forjas de armas"),
                    (Textos.ProjetoNome(s, s.Projetos.Defs[0]), Textos.ProjetoNome(s, s.Projetos.Defs[2])));
                Assert.AreEqual("Venda 100 elmos (0/100)", Textos.ProjetoFonte(s, Array.Find(s.Projetos.Defs, d => d.Id == "bp_mw_elmo_vigia")));
                StringAssert.EndsWith(" ou Venda 150 peitorais (0/150)", Textos.ProjetoFonte(s, Array.Find(s.Projetos.Defs, d => d.Id == "bp_mw_peitoral_bastiao")),
                    "o peitoral (handle 8) com o nome dele, nao o do Old Forge");
                Assert.AreEqual(("Qualidade +0,15 · vel. +50%", "Fundidor"), (Textos.BeneficioWorker(s, k), Textos.AntesDepoisWorker(s, k)));
                Assert.AreEqual("Precisa: Liga", Textos.PrecisaWorker(NovaRow(), k));
            }
            finally { Textos.Distrito(Distritos.OldForge); Loc.Atual = Idioma.Pt; }
        }
    }
}
