using System;
using System.Linq;
using System.Text;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// C-CORE-03 (GDD §8, §11; CONTEUDO_V1 §1, §6; P2): managers (nivel 2-5 por ouro, fundo 25 -> 40% da taxa online e offline, nunca o teto de
    /// 2 h; compra 1x por (distrito, nivel); save mgr= opcional) e as camadas do Imperio (venda, encomenda, golpe e cofre do distrito contam no
    /// Renome, nas metas e no evento da raiz, 1x). A prova ao byte do Old Forge (econsim) esta no BALANCE.md §34. NUnit 3.5: tipos exatos.
    /// </summary>
    public class ImperioTests
    {
        const float Dt = 1f / 30f;
        const string OF = Distritos.OldForgeId;
        static readonly DistrictDef Row = Distritos.ArmorersRow;

        /// <summary>Old Forge em producao completa a 2.400/min (RateEma 40/s), Charter dele forcado, Row aberta e ativa.</summary>
        static Imperio Chegada() => Imperio.Chegada(Row);

        static long Cont(Sim s, string k) => s.Metas.Cont.TryGetValue(k, out long v) ? v : 0;

        // ------------------------------------------------------------------ managers

        /// <summary>CONTEUDO_V1 §1/§6: o manager vem com o Charter (nivel 1 = 25%); os niveis 2-5 custam 32/64/130/260 mil (Old Forge) e o fundo
        /// sobe a 30/33/36/40% da taxa. Em todo nivel o fundo e' menor que o mesmo distrito ativo (a taxa inteira).</summary>
        [Test]
        public void Manager_FundoPorNivel_25a40_AtivoSempreMaior()
        {
            Imperio imp = Chegada();
            Sim row = imp.Ativo, raiz = imp.Raiz;
            Assert.AreEqual((1, 0, -1L), (imp.Manager(OF), imp.Manager(Row.Id), imp.CustoManager(Row.Id)), "manager so com o Charter");
            double[] pct = { 0.25, 0.30, 0.33, 0.36, 0.40 };
            long[] custo = { 0, 32000, 64000, 130000, 260000 };
            for (int n = 1; n <= 5; n++)
            {
                if (n > 1)
                {
                    Assert.AreEqual(custo[n - 1], imp.CustoManager(OF));
                    row.Gold = custo[n - 1];
                    Assert.IsTrue(imp.SobeManager(OF, n), "nivel " + n);
                    Assert.AreEqual((n, 0L), (imp.Manager(OF), row.Gold), "paga da carteira");
                }
                Assert.AreEqual(pct[n - 1], imp.FundoFator(OF), 1e-9, "nivel " + n);
                Assert.AreEqual(40.0 * pct[n - 1], imp.FundoPorSegundo(raiz), 1e-9);
                Assert.Less(imp.FundoPorSegundo(raiz), raiz.RateEma, "fundo < o mesmo distrito ativo, nivel " + n);
            }
            Assert.AreEqual(-1L, imp.CustoManager(OF), "nivel 5 e' o maximo");
            row.Gold = 1000000;
            Assert.IsFalse(imp.SobeManager(OF, 6));
            row.Gold = 0;
            for (int i = 0; i < 60; i++) imp.Fundo(1f);
            Assert.AreEqual((960L, 0L), (row.Gold, raiz.GoldEarned), "40% de 2.400/min, fora do GoldEarned");
        }

        /// <summary>O invariante vale para todo distrito do build e todo nivel: fracao em (0, 1) (o ativo rende a taxa inteira) e cada distrito
        /// tem o seu manager em dados, nivel maximo 5. Com o FundoFator do catalogo (25%) a faixa e' 25-40%.</summary>
        [Test]
        public void Manager_TodoDistritoTodoNivel_FundoMenorQueAtivo()
        {
            foreach (DistrictDef d in Distritos.All)
            {
                WorkerDef m = Imperio.ManagerDe(d.Id);
                Assert.IsNotNull(m, d.Id);
                Assert.AreEqual((WorkerRole.Manager, 5, d.Id), (m.Papel, m.MaxNivel, m.Distrito));
                for (int n = 1; n <= m.MaxNivel; n++)
                {
                    double f = d.FundoFator * Imperio.FundoNivel[n - 1];
                    Assert.That(f > 0.0 && f < 1.0, $"{d.Id} nivel {n}: {f}");
                    if (d.FundoFator == 0.25f) Assert.That(f >= 0.25 - 1e-9 && f <= 0.40 + 1e-9, $"{d.Id} nivel {n}: {f}");
                }
            }
            Assert.AreEqual(Imperio.Managers.Length, Imperio.Managers.Select(m => m.Id).Distinct().Count(), "ID unico");
        }

        /// <summary>P2: o manager mexe na taxa, nunca no tempo: nivel 5 fora 10 h paga 40% x 7.200 s (nem o knob passa das 2 h); 1x por claim;
        /// relogio para tras (Δt &lt;= 0) nao paga nada.</summary>
        [Test]
        public void Manager_Offline_TetoDeDuasHorasIntocado()
        {
            Imperio imp = Chegada();
            Sim row = imp.Ativo;
            for (int n = 2; n <= 5; n++) { row.Gold = imp.CustoManager(OF); Assert.IsTrue(imp.SobeManager(OF, n)); }
            row.Gold = 0;
            Assert.AreEqual(0L, imp.ApplyOffline(-3600, 10), "relogio para tras");
            Assert.AreEqual(0L, imp.ApplyOffline(0, 11));
            Assert.AreEqual(115200L, imp.ApplyOffline(36000, 12), "16/s x 7.200 s");
            Assert.AreEqual(0L, imp.ApplyOffline(36000, 12), "mesmo claim");
            imp.Raiz.T.OfflineMaxSeconds = 999999;
            Assert.AreEqual(115200L, imp.ApplyOffline(36000, 13), "o knob nao passa das 2 h");
            Assert.AreEqual(57600L, imp.ApplyOffline(3600, 14), "1 h = metade");
        }

        /// <summary>ID de transacao = (distrito, nivel): so o proximo nivel, 1x; sem Charter, sem ouro ou pulando nivel nada muda. Depois de
        /// save/load o mesmo nivel tambem recusa.</summary>
        [Test]
        public void Manager_Compra1xPorId_SemCharterSemOuroNaoPaga()
        {
            Imperio imp = Chegada();
            Sim row = imp.Ativo;
            row.Gold = 10000000;
            Assert.IsFalse(imp.SobeManager(Row.Id, 2), "a Row ainda sem Charter");
            row.Gold = 31999;
            Assert.IsFalse(imp.SobeManager(OF, 2), "sem ouro");
            Assert.AreEqual((1, 31999L), (imp.Manager(OF), row.Gold));
            row.Gold = 100000;
            Assert.IsFalse(imp.SobeManager(OF, 3), "pular nivel");
            Assert.IsTrue(imp.SobeManager(OF, 2));
            Assert.IsFalse(imp.SobeManager(OF, 2), "o mesmo ID de novo (toque duplo)");
            Assert.AreEqual((2, 68000L), (imp.Manager(OF), row.Gold), "pagou 1x");
            Imperio back = Imperio.Load(imp.Save(5000));
            back.Ativo.Gold = 100000;
            Assert.IsFalse(back.SobeManager(OF, 2), "o mesmo ID depois do load");
            Assert.AreEqual((2, 100000L), (back.Manager(OF), back.Ativo.Gold));
        }

        /// <summary>Save: mgr= so com manager acima do 1 (Imperio de hoje grava o texto de antes), ida e volta; save sem a linha = nivel 1;
        /// lixo fica de fora, nivel acima do maximo corta, distrito de build mais novo volta no save.</summary>
        [Test]
        public void Manager_Save_IdaEVolta_SaveAntigoELixo()
        {
            Imperio imp = Chegada();
            string antes = imp.Save(9000);
            StringAssert.DoesNotContain("mgr=", antes, "nivel 1: o texto de antes do C-CORE-03");
            Assert.AreEqual(1, Imperio.Load(antes).Manager(OF), "save antigo = nivel 1");
            imp.Ativo.Gold = 96000;
            Assert.IsTrue(imp.SobeManager(OF, 2) && imp.SobeManager(OF, 3));
            string text = imp.Save(9000);
            StringAssert.Contains("\nmgr=old_forge:3\n", text);
            Imperio back = Imperio.Load(text);
            Assert.AreEqual((3, 0.33), (back.Manager(OF), Math.Round(back.FundoFator(OF), 9)));
            Assert.AreEqual(text, back.Save(9000), "regrava igual");
            Imperio lixo = Imperio.Load(text.Replace("mgr=old_forge:3", "mgr=old_forge:xx,armorers_row:9,dst_novo:4,,:2"));
            Assert.AreEqual((1, 0), (lixo.Manager(OF), lixo.Manager(Row.Id)), "lixo = nivel 1; a Row sem Charter = sem manager");
            lixo.Charters.Add(Row.Id);
            Assert.AreEqual(5, lixo.Manager(Row.Id), "acima do maximo corta em 5");
            StringAssert.Contains("dst_novo:4", lixo.Save(9000), "distrito de build mais novo volta no save");
        }

        /// <summary>O bot (econsim) sobe o manager so com a producao completa do distrito ativo e o ouro na carteira.</summary>
        [Test]
        public void Bot_SobeManagerSoDepoisDaProducaoCompleta()
        {
            Imperio imp = Chegada();
            Sim row = imp.Ativo;
            row.Gold = 10000000;
            Assert.IsFalse(Bot.Manager(imp), "a Row ainda em producao");
            for (int i = 0; i < row.UpgradeDefs.Length; i++) if (row.IsProduction(i)) Assert.IsTrue(row.Buy((Upgrade)i), row.UpgradeDefs[i].Id);
            row.Gold = 31999;
            Assert.IsFalse(Bot.Manager(imp), "sem ouro");
            row.Gold = 32000;
            Assert.IsTrue(Bot.Manager(imp));
            Assert.AreEqual((2, 0L), (imp.Manager(OF), row.Gold));
        }

        // ------------------------------------------------------------------ camadas do Imperio

        /// <summary>A Row joga (bot Ideal, com golpe) com o evento Royal Week ativo na raiz: cada venda conta 1x nas metas e no evento da raiz
        /// (nenhum filtro de item do Old Forge sobe: o elmo e' o handle 2, o da espada), as encomendas pelo tipo e no order_done, o Renome
        /// delas sobe o da raiz (o mesmo objeto), o golpe e o cofre tambem. O contador do proprio distrito segue (o Hall dele le).</summary>
        [Test]
        public void Camadas_VendaEncomendaGolpeECofreDaRow_ContamNaRaizUmaVez()
        {
            Imperio imp = Chegada();
            Sim row = imp.Ativo, raiz = imp.Raiz;
            Assert.AreSame(raiz.Renome, row.Renome, "o Renome do distrito e' o do Imperio");
            EventDef rw = LiveOpsDefs.Eventos.First(e => e.Id.EndsWith("_royal_week", StringComparison.Ordinal));
            raiz.Metas.Agora(rw.Comeca + 3600, 0); raiz.LiveOps.Agora(rw.Comeca + 3600);
            if (raiz.LiveOps.Evento == null) Assert.IsTrue(raiz.LiveOps.Forca(rw));
            // raia H: a Royal Week trocou os 10 golpes pelo contrato; o contador do evento conta o golpe com ou sem a missao no dado
            MissaoEvento vende = rw.Missoes.First(x => x.Id == "vende300"), golpe = new MissaoEvento("golpe10", "master_strike", 10, 100),
                cofre = rw.Missoes.First(x => x.Id == "cofre2");
            long sold0 = Cont(raiz, "product_sold"), espada0 = Cont(raiz, "product_sold.2"), done0 = Cont(raiz, "order_done"), rn0 = raiz.Renome.Total;
            Bot b = Bot.Ideal();
            for (float t = 0f; t < 1200f; t += Dt) { b.Step(row, Dt); imp.Fundo(Dt); }
            Assert.That(row.Sales > 0 && row.OrderCount > 0 && row.Strikes > 0, $"vendas {row.Sales}, encomendas {row.OrderCount}, golpes {row.Strikes}");
            Assert.AreEqual((long)row.Sales, Cont(raiz, "product_sold") - sold0, "cada venda da Row 1x nas metas da raiz");
            Assert.AreEqual((long)row.Sales, Cont(row, "product_sold"), "o contador do distrito segue");
            Assert.AreEqual(espada0, Cont(raiz, "product_sold.2"), "o elmo nao vira espada");
            Assert.AreEqual((long)row.OrderCount, Cont(raiz, "order_done") - done0);
            long tipos = Enum.GetValues(typeof(OrderType)).Cast<int>().Sum(t => Cont(raiz, Metas.Chave("order_complete", t)));
            Assert.AreEqual((long)row.OrderCount, tipos, "o tipo da encomenda tambem");
            Assert.AreEqual(row.Encomendas.RenomeGanho, raiz.Renome.Total - rn0, "o Renome das encomendas da Row sobe o da raiz, 1x");
            Assert.AreEqual((Math.Min(row.Sales, 300L), Math.Min(row.Strikes, 10L)), (raiz.LiveOps.Feito(vende), raiz.LiveOps.Feito(golpe)), "evento da raiz");
            Assert.AreEqual((long)row.Strikes, Cont(raiz, "master_strike"), "e o golpe nas metas da raiz (raia H)");
            Assert.AreEqual(0L, raiz.LiveOps.Feito(cofre));
            long cofreMeta = Cont(raiz, "offline_claim");
            Assert.Greater(imp.ApplyOffline(3600, 99), 0L);
            Assert.AreEqual((cofreMeta + 1, 1L), (Cont(raiz, "offline_claim"), raiz.LiveOps.Feito(cofre)), "o cofre da Row conta na raiz");
        }

        /// <summary>O Old Forge sozinho (sem Heroi) nao repassa nada: o Renome e as metas sao os dele, como antes.</summary>
        [Test]
        public void Camadas_OldForgeSozinho_ComoAntes()
        {
            var s = new Sim();
            Assert.IsNull(s.Heroi);
            var b = new Bot();
            for (float t = 0f; t < 300f; t += Dt) b.Step(s, Dt);
            Assert.AreEqual((long)s.Sales, Cont(s, "product_sold"));
            StringAssert.Contains("\nrn=", s.Save(10), "a raiz grava o Renome");
        }

        /// <summary>Save: o payload do distrito nao leva rn= (o Renome e' o da raiz); um payload antigo com rn= nao sobrescreve o Renome do
        /// Imperio; a obra-prima de outro distrito no ledger do Imperio nao entra no Hall do distrito (nem vira ID desconhecido).</summary>
        [Test]
        public void Camadas_SaveDoDistrito_SemRenomeProprio_LedgerSoDoDistrito()
        {
            Imperio imp = Chegada();
            imp.Raiz.Renome.Ganha(123);
            imp.Raiz.Renome.Credita("mw:" + MasterworksDefs.All[0].Id, 0);
            string text = imp.Save(9000);
            string linha = text.Split('\n').First(l => l.StartsWith("dist.armorers_row=", StringComparison.Ordinal));
            string payload = Encoding.UTF8.GetString(Convert.FromBase64String(linha.Substring("dist.armorers_row=".Length)));
            StringAssert.DoesNotContain("\nrn=", payload);
            StringAssert.DoesNotContain("hall=", payload);
            string antigo = text.Replace(linha, "dist.armorers_row=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(payload + "rn=999,0\n")));
            Imperio back = Imperio.Load(antigo);
            Assert.AreEqual((123L, 0, 0), (back.Raiz.Renome.Total, back.Ativo.Hall.Feitas.Count, back.Ativo.UnknownIds), "rn= do distrito antigo nao pisa no Imperio");
            Assert.AreEqual(1, back.Raiz.Hall.Count, "a obra-prima do Old Forge volta no Hall dele");
        }

        /// <summary>Charter: a obra-prima que pede e' a do Hall (vai no save); o contador da sessao nao volta do save.</summary>
        [Test]
        public void Charter_ObraPrimaDoHall_ValeDepoisDoLoad()
        {
            var raiz = new Sim();
            for (int i = 0; i < Upgrades.Count; i++) if (Upgrades.IsProduction(i)) raiz.Buy((Upgrade)i);
            raiz.Renome.Ganha(Distritos.OldForge.Charter.Renome);
            raiz.Hall.Feitas[MasterworksDefs.All[0].Id] = 1;
            Imperio back = Imperio.Load(new Imperio(raiz).Save(100));
            Assert.AreEqual(0, back.Raiz.Masterworks, "o contador da sessao nao vai no save");
            Assert.IsNull(back.CharterFalta(OF));
        }
    }
}
