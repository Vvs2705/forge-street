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
    /// validator com as obras da Row (as 10 do catalogo esperam a liga: Superior inalcancavel) e o layout sem rotulo encostado. A economia
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
        /// Royal (Fina) e Blueprint (sem fila) viram Common enquanto a Row nao tem qualidade nem projeto. A 7a Common e' o Sargento Brutus
        /// (30 lancas). A vaga volta do save pelo ID.</summary>
        [Test]
        public void Encomendas_DaRow_RodizioEItensDaRow()
        {
            Sim s = ComPrimeira(Row, "carroca_curtidor", "forja_hastes", "bigorna_pesada");   // 4 linhas: elmo, maca, grevas, lanca
            Encomenda p = s.Encomendas.Vagas[0];
            Assert.AreEqual(("ord_common_armorers_row", I(s, "elmo"), 5, 75L), (p.Def.Id, p.Itens[0], p.Alvo[0], p.Premio), "a 1a: Common da Row, 5 da 1a linha, minimo 75");
            List<Encomenda> r = Rodizio(s);
            CollectionAssert.AreEqual(new[] { Common, Bulk, Common, Common, Bulk, Mixed, Rush, Common, Common, Common }, r.Select(e => e.Tipo).ToArray());
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

        /// <summary>A Row com a obra e o projeto `mws`/`bps` (o resto igual).</summary>
        static DistrictDef ComObras(MasterworkDef[] mws, BlueprintDef[] bps) => new DistrictDef
        {
            Id = Row.Id, Nome = Row.Nome, Indice = Row.Indice, Itens = Row.Itens, Receitas = Row.Receitas, Upgrades = Row.Upgrades, Workers = Row.Workers,
            Projetos = bps, Masterworks = mws, MarcosColecao = Row.MarcosColecao, Estacoes = Row.Estacoes, Pads = Row.Pads, Obstaculos = Row.Obstaculos,
            Marcos = Row.Marcos, Inicio = Row.Inicio, Contratacao = Row.Contratacao, Largura = Row.Largura, Parede = Row.Parede, EsteiraDe = Row.EsteiraDe,
            EsteiraPara = Row.EsteiraPara, Ajuste = Row.Ajuste, Abertura = Row.Abertura, Charter = Row.Charter, FundoFator = Row.FundoFator,
        };

        static void Acusa(List<string> v, string trecho) =>
            Assert.IsTrue(v.Exists(m => m.Contains(trecho)), $"esperava \"{trecho}\" em:\n{string.Join("\n", v)}");

        /// <summary>
        /// Por que as obras da Row ficaram de fora (BALANCE.md §33): o validator do distrito confere as do Hall DELE no mundo dele, e as 8 do
        /// CONTEUDO_V1 §10.1 que o nucleo consegue escrever (AlloyPieces, das 2 de liga, nem existe como condicao) caem em "Superior
        /// inalcancavel": a Row nao tem fonte de qualidade (q maximo 0 na montagem e na forja de armas). Nivel de estacao e Vulcana tambem nao
        /// existem. Obra declarada noutro distrito e' recusada no mundo da Row.
        /// </summary>
        [Test]
        public void Validator_ObrasDaRow_EsperamALiga()
        {
            CollectionAssert.IsEmpty(ContentValidator.Distrito(Row).ToList(), "a Row real (sem obras) passa");
            CondicaoMasterwork C(CondicaoMw k, string r, int n) => new CondicaoMasterwork(k, r, n);
            MasterworkDef M(string id, string fam, int renome, int ped, params CondicaoMasterwork[] c) => new MasterworkDef(id, Row.Id, fam, "bp_" + id, renome, ped, c);
            MasterworkDef[] mws =
            {
                ElmoVigia(),
                M("mw_peitoral_bastiao", "peitoral", 35, 2, C(CondicaoMw.NivelEstacao, "montagem", 5)),
                M("mw_grevas_marcha", "grevas", 35, 3, C(CondicaoMw.EncomendasTipo, "bulk", 10)),
                M("mw_lanca_milicia", "lanca", 40, 5, C(CondicaoMw.Vendidos, "lanca", 1000)),
                M("mw_machado_rei", "machado_guerra", 35, 6, C(CondicaoMw.NivelEstacao, "forja_armas", 5)),
                M("mw_maca_juiz", "maca", 35, 7, C(CondicaoMw.NivelWorker, "wk_vulcana", 5)),
                M("mw_peitoral_rival", "peitoral", 50, 9, C(CondicaoMw.Renome, null, 6)),
                M("mw_lanca_estandarte", "lanca", 55, 10, C(CondicaoMw.EncomendasTipo, "royal", 10)),
            };
            BlueprintDef[] bps = mws.Select(m => Projeto(m.Id, Vendidos("elmo", 100))).ToArray();
            List<string> v = ContentValidator.Distrito(ComObras(mws, bps)).ToList();
            foreach (MasterworkDef m in mws) Acusa(v, $"masterwork {m.Id}: Superior inalcancavel na familia {m.Familia}");
            Acusa(v, "mw_peitoral_bastiao: condicao NivelEstacao \"montagem\" >= 5: inalcancavel");
            Acusa(v, "mw_machado_rei: condicao NivelEstacao \"forja_armas\" >= 5: inalcancavel");
            Acusa(v, "mw_maca_juiz: condicao NivelWorker \"wk_vulcana\" >= 5: inalcancavel");
            Assert.IsFalse(v.Exists(x => x.Contains("mw_grevas_marcha: condicao") || x.Contains("mw_lanca_milicia: condicao") || x.Contains("mw_peitoral_rival: condicao")),
                "Bulk, lancas vendidas e Renome 6 a Row ja conta");
            Sim w = ContentValidator.Mundo(Row);
            Assert.AreEqual((0, 0), (MasterworksDefs.QMaximo("montagem", w.T, Row.Upgrades, bps, w.Equipe.Defs), MasterworksDefs.QMaximo("forja_armas", w.T, Row.Upgrades, bps, w.Equipe.Defs)));

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
