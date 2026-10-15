using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// C-CORE-05 (CONTEUDO_V1 §7, §10; BALANCE.md §33): a Armorer's Row com as encomendas dela em dados (o Encomendas le as OrderDefs do
    /// Sim.Distrito; o Old Forge segue com as dele), uma Masterwork da Row pelo caminho de sempre (golpe numa Superior, Hall do distrito), o
    /// validator com as obras da Row (C-CORE-05: passam com a liga; a mecanica esta no LigaTests) e o layout sem rotulo encostado. A economia
    /// afinada e a prova ao byte do Old Forge estao no BALANCE.md §33 (Row_Kit_BalcaoDe6EOsNumerosDoDistrito prende precos e custos).
    /// NUnit 3.5 do Unity: tipos exatos nas tuplas (long com L).
    /// </summary>
    public class RowTests
    {
        const float Dt = 1f / 30f;
        const OrderType Common = OrderType.Common, Bulk = OrderType.Bulk, Mixed = OrderType.Mixed, Rush = OrderType.Rush;
        static readonly DistrictDef Row = Distritos.ArmorersRow;

        static Item I(Sim s, string id) => (Item)Items.IndexOf(id, s.ItemDefs);
        static void Compra(Sim s, params string[] ids) { foreach (string id in ids) Assert.IsTrue(s.Buy((Upgrade)Upgrades.IndexOf(id, s.UpgradeDefs)), id); }

        /// <summary>Sim do distrito `d` com a 1a encomenda aberta (a 1a venda "ja foi": o relogio das encomendas anda).</summary>
        static Sim ComPrimeira(DistrictDef d, params string[] compras)
        {
            var s = new Sim(distrito: d);
            Compra(s, compras);
            s.FirstSaleTime = 0f;
            for (float t = 0f; t < s.T.OrderFirstDelay + 1f && s.Encomendas.Vagas.Count == 0; t += Dt) s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(1, s.Encomendas.Vagas.Count, "a 1a abriu");
            return s;
        }

        /// <summary>Os 10 lugares do rodizio (Encomendas.Proxima na posicao k, sem mexer em nada): o tipo que abre em cada um.</summary>
        static List<Encomenda> Rodizio(Sim s)
        {
            var r = new List<Encomenda>();
            int abertas = s.Encomendas.Abertas;
            for (int k = 0; k < 10; k++) { s.Encomendas.Abertas = k; r.Add(s.Encomendas.Proxima()); }
            s.Encomendas.Abertas = abertas;
            return r;
        }

        // ------------------------------------------------------------------ encomendas

        /// <summary>CONTEUDO_V1 §7: na Row o rodizio e' o dela (Bulk x2, "equipar a milicia"), com as OrderDefs dela e os itens dela; Quality e
        /// Royal (Fina) viram Common enquanto nenhuma linha faz Fina (sem nivel, liga nem Vulcana); a Blueprint ja abre (C-CORE-05: a fila de
        /// projetos da Row). A 7a Common e' o Sargento Brutus (30 lancas). A vaga volta do save pelo ID.</summary>
        [Test]
        public void Encomendas_DaRow_RodizioEItensDaRow()
        {
            Sim s = ComPrimeira(Row, "carroca_curtidor", "forja_hastes", "bigorna_pesada");   // 4 linhas: elmo, maca, grevas, lanca
            Encomenda p = s.Encomendas.Vagas[0];
            Assert.AreEqual(("ord_common_armorers_row", I(s, "elmo"), 5, 75L), (p.Def.Id, p.Itens[0], p.Alvo[0], p.Premio), "a 1a: Common da Row, 5 da 1a linha, minimo 75");
            List<Encomenda> r = Rodizio(s);
            CollectionAssert.AreEqual(new[] { Common, Bulk, Common, Common, Bulk, Mixed, Rush, OrderType.Blueprint, Common, Common }, r.Select(e => e.Tipo).ToArray());
            Assert.AreEqual(("ord_blueprint_armorers_row", "bp_tecnica_montagem"), (r[7].Def.Id, r[7].Blueprint), "o #1 da fila de encomenda da Row");
            foreach (Encomenda e in r)
            {
                Assert.AreEqual(Row.Id, e.Def.District, e.Def.Id);
                foreach (Item it in e.Itens) Assert.IsTrue(s.IsProduct(it) && s.LineUnlocked(it), $"{e.Def.Id}: {s.ItemDefs[(int)it].Id}");
            }
            Assert.AreEqual(("ord_bulk_armorers_row", 12), (r[1].Def.Id, r[1].Alvo[0]), "Bulk da Row: N minimo 12 (o do Old Forge e' 8)");
            Assert.AreEqual(3, r[5].Itens.Length, "Mixed: 3 produtos com 4 linhas");
            Assert.AreEqual((true, false), (s.Encomendas.TipoAberto(Bulk), s.Encomendas.TipoAberto(OrderType.Quality)), "metas de tipo: o Bulk da Row nao pede entregues");

            s.Encomendas.Comuns = 6; s.Encomendas.Abertas = 3;   // a proxima Common e' a 7a do distrito
            Encomenda f = s.Encomendas.Proxima();
            Assert.AreEqual(("ord_reg_brutus", I(s, "lanca"), 30), (f.Def.Id, f.Itens[0], f.Alvo[0]), "o freguês da Row");

            Sim l = Sim.Load(s.Save(0), distrito: Row);
            Assert.AreEqual(("ord_common_armorers_row", I(l, "elmo"), 5), (l.Encomendas.Vagas[0].Def.Id, l.Encomendas.Vagas[0].Itens[0], l.Encomendas.Vagas[0].Alvo[0]));
        }

        /// <summary>O Old Forge segue com as dele: mesma posicao do rodizio, OrderDef do Old Forge (antes das Ferramentas tudo e' Common, como
        /// sempre). A prova ao byte (econsim de 5 perfis e o F2P x 8) esta no BALANCE.md §33.</summary>
        [Test]
        public void Encomendas_OldForge_AsDeSempre()
        {
            Sim s = ComPrimeira(Distritos.OldForge);
            Assert.AreEqual(("ord_common_old_forge", Item.Sword, 5, 25L), (s.Encomendas.Vagas[0].Def.Id, s.Encomendas.Vagas[0].Itens[0], s.Encomendas.Vagas[0].Alvo[0], s.Encomendas.Vagas[0].Premio));
            foreach (Encomenda e in Rodizio(s)) Assert.AreEqual("ord_common_old_forge", e.Def.Id, "sem as Ferramentas, so Common");
            CollectionAssert.AreEqual(new[] { Common, Common, Bulk, OrderType.Quality, Common, Mixed, Rush, OrderType.Blueprint, Common, OrderType.Royal },
                OrderDefs.Ciclo(OrderDefs.OldForge), "o ciclo do Old Forge nao mudou");
        }

        /// <summary>Royal Contracts (#64) com a Row no build: depois do Ronda, o Vinte Guardas abre (o elmo e' produto da Row) e o Milicia do
        /// Norte espera o Renome 5; o cartao traduz o item pelo ID com qualquer distrito ativo na View (antes saia "20 elmo").</summary>
        [Test]
        public void Contratos_DaRow_AbremEOCartaoTraduzOElmo()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools); s.Renome.Ganha(180);
            s.Contratos.Concluidos.Add("ctr_ronda_vila");
            ContractDef d = s.Contratos.Oferta;
            Assert.AreEqual("ctr_vinte_guardas", d?.Id);
            try
            {
                foreach (DistrictDef ativo in new[] { Distritos.OldForge, Row })
                {
                    Textos.Distrito(ativo);
                    Assert.AreEqual("Pede: 20 espadas + 20 escudos + 20 elmos", Textos.ContratoPede(s, d), ativo.Id);
                    Loc.Atual = Idioma.Es;
                    StringAssert.EndsWith("+ 20 yelmos", Textos.ContratoPede(s, d), ativo.Id);
                    Loc.Atual = Idioma.Pt;
                }
            }
            finally { Textos.Distrito(Distritos.OldForge); Loc.Atual = Idioma.Pt; }
            s.Contratos.Concluidos.Add("ctr_vinte_guardas");
            Assert.IsNull(s.Contratos.Oferta, "o Milicia do Norte pede o Renome 5");
            s.Renome.Ganha(570);
            Assert.AreEqual("ctr_milicia_norte", s.Contratos.Oferta?.Id, "lanca, maca e grevas da Row");
        }

        // ------------------------------------------------------------------ Masterwork da Row

        static MasterworkDef ElmoVigia() => new MasterworkDef("mw_elmo_vigia", Row.Id, "elmo", "bp_mw_elmo_vigia", 30, 1);
        static BlueprintDef Projeto(string mw, params BlueprintSource[] f) => new BlueprintDef("bp_" + mw, Row.Id, BlueprintKind.Masterwork, new[] { mw }, false, f);
        static BlueprintSource Vendidos(string item, int n) => new BlueprintSource(BlueprintSourceKind.Milestone, item, n);

        /// <summary>O caminho de sempre no Sim da Row, com a obra e o projeto do catalogo injetados e q 200 no elmo (o modificador de evento faz
        /// as vezes da liga, que a Row ainda nao tem): golpe numa Superior da montagem vai ao Hall DA ROW 1x, credita o Renome pelo ID, conta no
        /// Sim.Masterworks (o que o Charter le) e nunca entra na pilha nem no GoldEarned; a 2a sai Superior.</summary>
        [Test]
        public void Masterwork_DaRow_PeloCaminhoDeSempre()
        {
            var q = new int[Row.Itens.Length];
            q[Items.IndexOf("elmo", Row.Itens)] = 200;
            var s = new Sim(new Tuning { QualidadeEvento = q }, blueprints: new[] { Projeto("mw_elmo_vigia", Vendidos("elmo", 100)) }, distrito: Row,
                masterworks: new[] { ElmoVigia() });
            Assert.IsTrue(s.Projetos.Ganha("bp_mw_elmo_vigia", BlueprintSourceKind.Milestone));
            Station m = s.Est("montagem_elmo");
            Assert.AreEqual(0, s.Hall.Pendente(m));
            long renome = s.Renome.Total, earned = s.GoldEarned;
            Assert.AreEqual(Quality.Masterwork, MasterworksTests.ComGolpe(s, m));
            SimEvent e = s.Events.Find(x => x.Kind == Ev.Masterwork);
            Assert.AreEqual(((int)I(s, "elmo"), 0), (e.A, e.B));
            Assert.AreEqual((1, 1, true, renome + 30L, earned), (s.Masterworks, s.Hall.Count, s.Renome.Ledger.Contains("mw:mw_elmo_vigia"), s.Renome.Total, s.GoldEarned));
            Assert.AreEqual(Quality.Superior, MasterworksTests.ComGolpe(s, m), "a 2a vez sai Superior");
            Assert.AreEqual((1, 1), (s.Masterworks, s.Hall.Count));
        }

        // ------------------------------------------------------------------ validator

        /// <summary>A Row com a obra e o projeto `mws`/`bps`, o `ajuste` e os `workers` (null = os da Row; o resto igual).</summary>
        internal static DistrictDef ComObras(MasterworkDef[] mws, BlueprintDef[] bps, Action<Tuning> ajuste = null, WorkerDef[] workers = null) => new DistrictDef
        {
            Id = Row.Id, Nome = Row.Nome, Indice = Row.Indice, Itens = Row.Itens, Receitas = Row.Receitas, Upgrades = Row.Upgrades, Workers = workers ?? Row.Workers,
            Projetos = bps, Masterworks = mws, MarcosColecao = Row.MarcosColecao, Estacoes = Row.Estacoes, Pads = Row.Pads, Obstaculos = Row.Obstaculos,
            Marcos = Row.Marcos, Inicio = Row.Inicio, Contratacao = Row.Contratacao, Largura = Row.Largura, Parede = Row.Parede, EsteiraDe = Row.EsteiraDe,
            EsteiraPara = Row.EsteiraPara, Ajuste = ajuste ?? Row.Ajuste, Abertura = Row.Abertura, Charter = Row.Charter, FundoFator = Row.FundoFator,
        };

        internal static void Acusa(List<string> v, string trecho) =>
            Assert.IsTrue(v.Exists(m => m.Contains(trecho)), $"esperava \"{trecho}\" em:\n{string.Join("\n", v)}");

        /// <summary>
        /// C-CORE-05 (CONTEUDO_V1 §10.2): com a liga, os niveis II-V e a Vulcana as 10 obras da Row passam no validator do distrito (q maximo
        /// 140 na montagem, 110 na forja de armas). Sem a liga (knob 0) a forja de armas para em 80: as 4 obras dela voltam a "Superior
        /// inalcancavel" e as montagens seguem (60 + 20 + Vulcana 30); sem a Vulcana, a Maca do Juiz (Vulcana nivel 5) e' inalcancavel. Obra
        /// declarada noutro distrito e' recusada no mundo da Row.
        /// </summary>
        [Test]
        public void Validator_ObrasDaRow_PassamComALiga()
        {
            CollectionAssert.IsEmpty(ContentValidator.Distrito(Row).ToList(), "a Row real, com as 10 obras, os 14 projetos e os marcos, passa");
            Assert.AreEqual((10, 14), (Row.Masterworks.Length, Row.Projetos.Length));
            Sim w = ContentValidator.Mundo(Row);
            Assert.AreEqual((140, 110), (MasterworksDefs.QMaximo("montagem", w.T, Row.Upgrades, Row.Projetos, w.Equipe.Defs, true),
                MasterworksDefs.QMaximo("forja_armas", w.T, Row.Upgrades, Row.Projetos, w.Equipe.Defs, true)), "§10.2: 60 + 20 + 30 (+ Vulcana 30)");

            List<string> v = ContentValidator.Distrito(ComObras(Row.Masterworks, Row.Projetos, t => { Row.Ajuste(t); t.LigaQ = 0; })).ToList();
            foreach (string m in new[] { "mw_lanca_milicia", "mw_machado_rei", "mw_maca_juiz", "mw_lanca_estandarte" }) Acusa(v, $"masterwork {m}: Superior inalcancavel");
            Assert.IsFalse(v.Exists(x => x.Contains("elmo") || x.Contains("peitoral") || x.Contains("grevas") || x.Contains("manoplas")), "montagens: 110 sem a liga");

            v = ContentValidator.Distrito(ComObras(Row.Masterworks, Row.Projetos, workers: Row.Workers.Where(k => k.Id != "wk_vulcana").ToArray())).ToList();
            Acusa(v, "mw_maca_juiz: condicao NivelWorker \"wk_vulcana\" >= 5: inalcancavel");

            var deOutro = new MasterworkDef("mw_elmo_vigia", Distritos.OldForgeId, "elmo", "bp_mw_elmo_vigia", 30, 1);
            Acusa(ContentValidator.Distrito(ComObras(new[] { deOutro }, new[] { Projeto("mw_elmo_vigia", Vendidos("elmo", 100)) })).ToList(),
                "masterwork mw_elmo_vigia: distrito \"old_forge\" nao existe (ou nao e' o do mundo)");
        }

        // ------------------------------------------------------------------ layout

        /// <summary>Foto do coordenador: o "Deposito" encostava no "Fornalha". A View (WorldView.RefreshStation) poe o nome do deposito (com arte)
        /// 1,35 m acima dele e o de quem produz 1,04 m abaixo (BarY - 0,3); a caixa do rotulo tem 44 px = 0,37 m (120 px/m, 9 m na largura).
        /// Na mesma coluna, os dois tem de ficar a mais que isso.</summary>
        [Test]
        public void Layout_RotuloDoDepositoNaoEncostaNoDaEstacaoDeCima()
        {
            foreach (StationDef dep in Row.Estacoes.Where(x => x.Kind == Kind.Deposit))
                foreach (StationDef p in Row.Estacoes.Where(x => x.Familia != null && x.Pos.Y > dep.Pos.Y && Math.Abs(x.Pos.X - dep.Pos.X) < 1.5f))
                    Assert.GreaterOrEqual(p.Pos.Y - 1.04f - (dep.Pos.Y + 1.35f), 0.37f, $"{dep.Id} x {p.Id}");
        }
    }
}
