using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// D-CORE-01/02: calendario (virada as 04:00 no fuso gravado, semana na segunda), rodizio sem sorteio, ledger (pagar 2x = 1x), relogio para
    /// tras, save/load e reabrir no meio, login nao punitivo, save antigo sem metas, ouro fora do GoldEarned, o bot igual com ou sem metas e as
    /// regras do validator. Asserts separados (NUnit 3.5 do Unity: int != long em tupla).
    /// </summary>
    public class MetasTests
    {
        const long Fuso = -3 * 3600;   // Brasilia
        const long D = 20000;          // 2024-10-04, sexta; a segunda seguinte e' o dia 20003

        /// <summary>Instante unix do dia local `dia` as `hora` do relogio do aparelho (fuso Brasilia).</summary>
        static long Em(long dia, double hora) => dia * Calendario.Dia + (long)(hora * 3600) - Fuso;

        /// <summary>Oficina com as Ferramentas (as metas abrem nelas) e taxa de 2 ouro/s.</summary>
        static Sim Aberta(params Upgrade[] mais)
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            foreach (Upgrade u in mais) s.Buy(u);
            s.RateEma = 2.0;
            return s;
        }

        static string[] Ids(Sim s) => Enumerable.Range(0, Metas.NDiarias).Select(i => s.Metas.Diaria(i).Def?.Id).ToArray();
        static string[] IdsW(Sim s) => Enumerable.Range(0, Metas.NSemanais).Select(k => s.Metas.Semanal(k).Def?.Id).ToArray();

        /// <summary>Completa a vaga diaria `i` pelo fluxo de eventos (o mesmo On que o Sim.Emit chama).</summary>
        static void Completa(Sim s, Metas.Meta m)
        {
            MetaDef d = m.Def;
            string[] p = d.Chave.Split('.');
            int f = p.Length > 1 ? int.Parse(p[1]) : d.Evento == "upgrade_buy" ? 0 : (int)Item.Sword;
            Ev ev = d.Evento switch
            {
                "product_sold" => Ev.Sold, "product_crafted" => Ev.Crafted, "order_done" => Ev.OrderDone, "upgrade_buy" => Ev.Bought,
                "offline_claim" => Ev.Offline, "vip_served" => Ev.VipServed, "worker_hired" => Ev.Hired, "chest_opened" => Ev.ChestOpened,
                "currency_source" => Ev.Sold, "currency_sink" => Ev.Bought, "worker_upgrade" => Ev.WorkerUp, _ => throw new ArgumentException(d.Evento),
            };
            long falta = m.Alvo - m.Feito;
            bool soma = d.Evento.StartsWith("currency_", StringComparison.Ordinal);
            for (long k = 0; k < (soma ? 1 : falta); k++) s.Metas.On(ev, f, soma ? (int)falta : 10);
        }

        [Test]
        public void Calendario_ViradaAs4hNoFusoGravado_SemanaNaSegunda()
        {
            Assert.AreEqual(D - 1, Calendario.DiaLocal(Em(D, 3.99), Fuso), "03:59 ainda e' o dia anterior");
            Assert.AreEqual(D, Calendario.DiaLocal(Em(D, 4), Fuso));
            Assert.AreEqual(D, Calendario.DiaLocal(Em(D + 1, 3.5), Fuso));
            Assert.AreEqual(DayOfWeek.Monday, DateTimeOffset.FromUnixTimeSeconds((D + 3) * Calendario.Dia).DayOfWeek);
            Assert.AreEqual(Calendario.Semana(D), Calendario.Semana(D + 2), "sexta a domingo: mesma semana");
            Assert.AreEqual(Calendario.Semana(D) + 1, Calendario.Semana(D + 3), "segunda abre semana nova");
            Assert.AreEqual(-1L, Calendario.DiaLocal(0, 0), "antes da virada do dia 0: dia -1 (piso, nao truncamento)");

            // o fuso e' o da 1a abertura: trocar o fuso do aparelho depois (viagem, trapaca) nao muda a virada
            Sim s = Aberta();
            s.Metas.Agora(Em(D, 12), Fuso);
            s.Metas.Agora(Em(D, 12), 12 * 3600);
            Assert.AreEqual(Fuso, s.Metas.Fuso);
            Assert.AreEqual(D, s.Metas.Dia);
        }

        [Test]
        public void Rodizio_DeterministicoSemSorteio_GoldenDoDia()
        {
            Sim a = Aberta(), b = Aberta();
            a.Metas.Agora(Em(D, 10), Fuso);
            b.Metas.Agora(Em(D, 23), Fuso);
            // pool A = (dia x 5) mod 19 -> 3; B = (dia x 5 + 1) mod 14 -> 13 (B-CORE-04: dly_worker_nivel no fim da B); a C (vazia) cai na A em
            // (dia x 5 + 2) mod 19 -> 5
            CollectionAssert.AreEqual(new[] { "dly_ferram20", "dly_worker_nivel", "dly_lingotes100" }, Ids(a));
            CollectionAssert.AreEqual(Ids(a), Ids(b), "mesmo dia, mesmas diarias");
            // semana: (semana x 3 + k) mod 20 a partir de 11 (raia H: 20 semanais); forja_joias100 travada (Joalheria); a Mixed ja abre (3 linhas);
            // Bulk, Royal, contrato, Superior e golpe fechados (Metas.Livre) -> as seguintes, sem repetir contador
            CollectionAssert.AreEqual(new[] { "wkl_renda1h", "wkl_gasta30min", "wkl_enc_premium5", "wkl_enc25" }, IdsW(a));
            a.Metas.Agora(Em(D + 1, 10), Fuso);
            // travadas puladas: vende150 e vende300 (Joalheria, Vitrine de joias); B = (20001 x 5 + 1) mod 14 -> 4
            CollectionAssert.AreEqual(new[] { "dly_forja_ferram60", "dly_gasta10min", "dly_renda5min" }, Ids(a));
        }

        [Test]
        public void Rodizio_60Dias_SempreCheioSemRepetirContadorEPoolCerto()
        {
            Sim s = Aberta(Upgrade.SideCorridor, Upgrade.Jewelry, Upgrade.JewelVitrine);
            for (long d = D; d < D + 60; d++)
            {
                s.Metas.Agora(Em(d, 9), Fuso);
                var ds = Enumerable.Range(0, Metas.NDiarias).Select(i => s.Metas.Diaria(i).Def).ToArray();
                var ws = Enumerable.Range(0, Metas.NSemanais).Select(k => s.Metas.Semanal(k).Def).ToArray();
                Assert.IsFalse(ds.Contains(null) || ws.Contains(null), $"dia {d}: vaga vazia");
                Assert.AreEqual(3, ds.Select(x => x.Chave).Distinct().Count(), $"dia {d}: contador repetido");
                Assert.AreEqual(4, ws.Select(x => x.Chave).Distinct().Count(), $"semana do dia {d}: contador repetido");
                Assert.AreEqual(0, ds[0].Pool, $"dia {d}");
                Assert.AreEqual(1, ds[1].Pool, $"dia {d}");
                Assert.AreEqual(2, ds[2].Pool, $"dia {d}: pool C (com a Joalheria, o Renome; sem Fina ainda, so ele)");
                Assert.AreEqual("dly_renome5", ds[2].Id, $"dia {d}");
            }
        }

        /// <summary>
        /// LIVEOPS_V1 §4.1-4.2 (#25-34): o slot C. Sem nada aberto cai na A; com a Joalheria, o Renome (feito = Renome ganho desde a vaga, paga
        /// 1x); a 1a peca Fina de um item abre as de Fina (dele e a geral); com um evento ativo, so as 2 de evento, alternando por dia, e a de
        /// pontos fecha pelos pontos do evento.
        /// </summary>
        [Test]
        public void PoolC_RenomeFinaEEvento_GoldenERegras()
        {
            Sim s = Aberta(Upgrade.Jewelry);
            s.Metas.Agora(Em(D, 10), Fuso);
            Assert.AreEqual("dly_renome5", Ids(s)[2]);
            Metas.Meta m = s.Metas.Diaria(2);
            Assert.AreEqual(0L, m.Feito);
            s.Renome.Ganha(5);
            Assert.IsTrue(s.Metas.Diaria(2).Pronta, "Renome ganho desde a vaga");
            long ouro = s.Gold;
            Assert.Greater(s.Metas.ResgatarDiaria(2), 0L);
            Assert.AreEqual(-1L, s.Metas.ResgatarDiaria(2), "paga 1x");
            Assert.Greater(s.Gold, ouro);

            s.Metas.On(Ev.Graded, s.ShieldBench.Index, (int)Quality.Fine);   // a 1a Fina (escudo): abre dly_fina10 e dly_fina_escudos10
            var c = new List<string>();
            for (long d = D + 1; d <= D + 10; d++) { s.Metas.Agora(Em(d, 10), Fuso); c.Add(Ids(s)[2]); }
            CollectionAssert.IsSubsetOf(new[] { "dly_fina10", "dly_fina_escudos10" }, c, "as de Fina entram no rodizio da C");
            Assert.IsFalse(c.Contains("dly_fina_joias5"), "sem Fina de joia, a de joias espera");
            CollectionAssert.AreEqual(GoldenC, c);

            EventDef ev = LiveOpsDefs.Eventos.First(e => e.Id.EndsWith("_forge_frenzy", StringComparison.Ordinal));
            long t = ev.Comeca + 3600;
            s.LiveOps.Agora(t);
            Assert.IsTrue(s.LiveOps.Forca(ev) || s.LiveOps.Evento == ev);
            var e2 = new List<string>();
            for (int k = 0; k < 4; k++) { s.Metas.Agora(t + k * Calendario.Dia, Fuso); s.LiveOps.Agora(t + k * Calendario.Dia); e2.Add(Ids(s)[2]); }
            Assert.IsTrue(e2.All(x => x == "dly_evento100" || x == "dly_enc_evento"), string.Join(",", e2));
            for (int k = 1; k < e2.Count; k++) Assert.AreNotEqual(e2[k - 1], e2[k], "alterna por dia");
        }

        // (dia x 7 + 2) mod 10 sobre a C (Metas.Passo: com 10 o passo 5 so visitaria 2 posicoes), pulando as travadas (superior e golpes sem a
        // Quality, joias sem Fina de joia, fina30 sem a Vitrine, evento)
        static readonly string[] GoldenC = { "dly_fina10", "dly_renome5", "dly_renome5", "dly_fina10", "dly_fina10", "dly_renome5", "dly_fina_escudos10",
            "dly_fina10", "dly_renome5", "dly_renome5" };

        /// <summary>dly_golpe_superior1 (LIVEOPS_V1 §4.2 #27) e ach_golpe_superior50: a diaria so entra na C com a Quality aberta e depois da 1a
        /// Fina; o golpe numa Superior (q 200) fecha a diaria e soma 1 na conquista; um golpe que so faz Fina (q 0) nao.</summary>
        [Test]
        public void GolpeSuperior_DiariaEConquista_PeloGolpeReal()
        {
            Sim s = QualidadeTests.ComQ(200);
            s.RateEma = 2.0;
            long dia = D;
            bool Aparece(Sim x)
            {
                for (long fim = dia + 30; dia < fim; dia++) { x.Metas.Agora(Em(dia, 10), Fuso); if (Ids(x)[2] == "dly_golpe_superior1") return true; }
                return false;
            }
            Assert.IsFalse(Aparece(s), "sem nenhuma Fina ainda, espera");
            Assert.AreEqual(Quality.Superior, QualidadeTests.Peca(s));   // a 1a Superior conta como Fina ou melhor
            Assert.IsTrue(Aparece(s), "com a Quality aberta e uma Fina, entra no rodizio da C");
            Assert.IsFalse(s.Metas.Diaria(2).Pronta);
            Assert.AreEqual(Quality.Superior, MasterworksTests.ComGolpe(s, s.AnvilA));
            Assert.IsTrue(s.Metas.Diaria(2).Pronta, "o golpe saiu Superior");
            Assert.Greater(s.Metas.ResgatarDiaria(2), 0L);
            Assert.AreEqual(1L, s.Metas.Conquista(A("ach_golpe_superior50")).Feito);

            Sim f = QualidadeTests.ComQ(0);
            Assert.AreEqual(Quality.Fine, MasterworksTests.ComGolpe(f, f.AnvilA));
            f.Metas.Agora(Em(D, 10), Fuso);
            Assert.AreEqual(0L, f.Metas.Conquista(A("ach_golpe_superior50")).Feito, "golpe que so faz Fina nao conta");
        }

        [Test]
        public void PrimeirosMinutos_AntesDasFerramentas_NadaAbreNemPaga()
        {
            var s = new Sim();
            s.Buy(Upgrade.FurnaceSpeed1);
            s.Metas.Agora(Em(D, 10), Fuso);
            s.Metas.On(Ev.Sold, (int)Item.Sword, 10);
            Assert.IsNull(s.Metas.Diaria(0).Def);
            Assert.AreEqual(0, s.Metas.Prontas());
            Assert.AreEqual(-1L, s.Metas.ResgatarConquista("ach_primeira_venda"), "conquista feita antes fica para quando a tela abre");
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            s.Metas.Agora(Em(D, 10), Fuso);
            Assert.IsNotNull(s.Metas.Diaria(0).Def, "abriu nas Ferramentas, no mesmo dia");
            Assert.Greater(s.Metas.ResgatarConquista("ach_primeira_venda"), 0L);
        }

        [Test]
        public void Ledger_PagarOMesmoIdDuasVezes_PagaUma()
        {
            Sim s = Aberta();
            long g0 = s.Gold;
            long g = s.Metas.Paga("d:1:0", 20, 100, 2);
            Assert.AreEqual(40L, g, "20 s x 2/s");
            Assert.AreEqual(-1L, s.Metas.Paga("d:1:0", 20, 100, 2));
            Assert.AreEqual(g0 + 40, s.Gold);
            Assert.AreEqual(100L, s.Metas.PassXp);
            Assert.AreEqual(2L, s.Metas.Renome);
            Assert.AreEqual(25L, new Sim().Metas.Ouro(20), "taxa zerada: piso das encomendas");
        }

        [Test]
        public void Diaria_ResgatarDuasVezes_PagaUma_ForaDoGoldEarned()
        {
            Sim s = Aberta();
            s.Metas.Agora(Em(D, 10), Fuso);
            Metas.Meta m = s.Metas.Diaria(0);
            Assert.IsFalse(m.Pronta);
            Assert.AreEqual(-1L, s.Metas.ResgatarDiaria(0), "sem progresso nao paga");
            Completa(s, m);
            Assert.IsTrue(s.Metas.Diaria(0).Pronta);
            long gold = s.Gold, earned = s.GoldEarned; double ema = s.RateEma;
            long g = s.Metas.ResgatarDiaria(0);
            Assert.AreEqual(40L, g);
            Assert.AreEqual(gold + 40, s.Gold);
            Assert.AreEqual(earned, s.GoldEarned, "fora do GoldEarned: nao infla a taxa nem o cofre");
            Assert.AreEqual(ema, s.RateEma);
            Assert.AreEqual(-1L, s.Metas.ResgatarDiaria(0));
            Assert.AreEqual(gold + 40, s.Gold);
            Assert.IsTrue(s.Metas.Diaria(0).Paga);
            Assert.AreEqual(1L, s.Metas.Cont["mission_complete.0"]);
        }

        [Test]
        public void Virada_DiaNovoTrocaAsDiarias_SemanaSoNaSegunda_LedgerSoDoPeriodo()
        {
            Sim s = Aberta();
            s.Metas.Agora(Em(D, 10), Fuso);
            Completa(s, s.Metas.Diaria(0));
            Completa(s, s.Metas.Semanal(0));
            Assert.Greater(s.Metas.ResgatarDiaria(0), 0L);
            Assert.Greater(s.Metas.ResgatarSemanal(0), 0L);
            string[] semana = IdsW(s);
            s.Metas.Agora(Em(D + 1, 3.9), Fuso);
            Assert.AreEqual(D, s.Metas.Dia, "03:54: ainda o dia de ontem");
            Assert.IsTrue(s.Metas.Diaria(0).Paga);
            s.Metas.Agora(Em(D + 1, 4), Fuso);
            Assert.AreEqual(D + 1, s.Metas.Dia);
            Assert.IsFalse(s.Metas.Diaria(0).Paga, "dia novo: vaga nova");
            Assert.IsFalse(s.Metas.Ledger.Contains("d:" + D + ":0"), "o ID do dia fechado sai do ledger (o dia so cresce)");
            Assert.IsTrue(s.Metas.Semanal(0).Paga, "sabado ainda e' a mesma semana");
            CollectionAssert.AreEqual(semana, IdsW(s));
            s.Metas.Agora(Em(D + 3, 4), Fuso);   // segunda 04:00
            Assert.IsFalse(s.Metas.Semanal(0).Paga, "semana nova");
            Assert.IsFalse(s.Metas.Ledger.Any(t => t.StartsWith("w:" + Calendario.Semana(D) + ":", StringComparison.Ordinal)));
        }

        [Test]
        public void RelogioParaTras_NaoReabreNemPagaDeNovo()
        {
            Sim s = Aberta();
            s.Metas.Agora(Em(D, 10), Fuso);
            s.Metas.Agora(Em(D + 1, 10), Fuso);
            Completa(s, s.Metas.Diaria(1));
            long g = s.Metas.ResgatarDiaria(1);
            Assert.Greater(g, 0L);
            Assert.Greater(s.Metas.ResgatarLogin(), -1L);
            string[] hoje = Ids(s);
            long gold = s.Gold;
            s.Metas.Agora(Em(D - 5, 10), Fuso);   // relogio 6 dias para tras
            Assert.AreEqual(D + 1, s.Metas.Dia, "o dia gravado e' o maximo");
            CollectionAssert.AreEqual(hoje, Ids(s), "nada abre");
            Assert.AreEqual(-1L, s.Metas.ResgatarDiaria(1), "nada reabre");
            Assert.IsFalse(s.Metas.LoginPronto);
            s.Metas.Agora(Em(D + 1, 23), Fuso);   // acertou a hora: o mesmo dia
            Assert.AreEqual(-1L, s.Metas.ResgatarDiaria(1));
            Assert.IsFalse(s.Metas.LoginPronto);
            Assert.AreEqual(gold, s.Gold);
            // e pelo save: fechar com o relogio para tras e reabrir nao muda nada
            Sim r = Sim.Load(s.Save(Em(D - 5, 10)));
            r.Metas.Agora(Em(D - 5, 10), Fuso);
            Assert.AreEqual(D + 1, r.Metas.Dia);
            Assert.AreEqual(-1L, r.Metas.ResgatarDiaria(1));
            Assert.IsFalse(r.Metas.LoginPronto);
        }

        [Test]
        public void SaveLoad_NoMeio_ProgressoEPagamentoSobrevivem()
        {
            Sim s = Aberta();
            s.Metas.Agora(Em(D, 10), Fuso);
            Metas.Meta m = s.Metas.Diaria(0);
            s.Metas.On(Ev.Sold, (int)Item.Tool, 10);   // dly_ferram20: 1 de 20
            s.Metas.On(Ev.Sold, (int)Item.Tool, 10);
            Completa(s, s.Metas.Diaria(2));
            Assert.Greater(s.Metas.ResgatarDiaria(2), 0L);
            Sim r = Sim.Load(s.Save(Em(D, 11)));
            r.Metas.Agora(Em(D, 11), Fuso);
            Assert.IsTrue(r.Metas.Iniciado);
            CollectionAssert.AreEqual(Ids(s), Ids(r));
            Assert.AreEqual(m.Def.Id, r.Metas.Diaria(0).Def.Id);
            Assert.AreEqual(2L, r.Metas.Diaria(0).Feito, "progresso no meio volta igual");
            Assert.AreEqual(s.Metas.Diaria(2).Alvo, r.Metas.Diaria(2).Alvo);
            Assert.IsTrue(r.Metas.Diaria(2).Paga);
            Assert.AreEqual(-1L, r.Metas.ResgatarDiaria(2), "pago antes do save: nao paga de novo");
            Assert.AreEqual(s.Gold, r.Gold);
            Completa(r, r.Metas.Diaria(0));
            Assert.Greater(r.Metas.ResgatarDiaria(0), 0L, "o que ficou pela metade fecha depois do load");
            Assert.AreEqual(s.Save(Em(D, 11)), Sim.Load(s.Save(Em(D, 11))).Save(Em(D, 11)), "save -> load -> save igual (mt.* incluso)");
        }

        [Test]
        public void Reabrir_MesmoSaveDuasVezes_OuMatarAntesDeGravar_NuncaPagaDuas()
        {
            Sim s = Aberta();
            s.Metas.Agora(Em(D, 10), Fuso);
            Completa(s, s.Metas.Diaria(0));
            string antes = s.Save(Em(D, 10));
            long g = s.Metas.ResgatarDiaria(0);
            string depois = s.Save(Em(D, 10));
            foreach (string txt in new[] { depois, depois })   // reabrir 2x o mesmo save
            {
                Sim r = Sim.Load(txt);
                r.Metas.Agora(Em(D, 12), Fuso);
                Assert.AreEqual(-1L, r.Metas.ResgatarDiaria(0));
                Assert.AreEqual(s.Gold, r.Gold);
            }
            // matou o app entre o credito e a gravacao: o save velho nao tem nem o ouro nem o ID; paga uma vez so
            Sim v = Sim.Load(antes);
            v.Metas.Agora(Em(D, 12), Fuso);
            Assert.AreEqual(s.Gold - g, v.Gold);
            Assert.AreEqual(g, v.Metas.ResgatarDiaria(0));
            Assert.AreEqual(-1L, v.Metas.ResgatarDiaria(0));
            Assert.AreEqual(s.Gold, v.Gold);
        }

        [Test]
        public void Conquista_PagaUmaVez_ComSaveLoad()
        {
            Sim s = Aberta();
            s.Metas.Agora(Em(D, 10), Fuso);
            s.Metas.On(Ev.Sold, (int)Item.Sword, 10);
            Assert.IsTrue(s.Metas.Conquista(MetasDefs.Achar("ach_primeira_venda")).Pronta);
            long g = s.Metas.ResgatarConquista("ach_primeira_venda");
            Assert.AreEqual(60L, g, "30 s x 2/s");
            Assert.AreEqual(-1L, s.Metas.ResgatarConquista("ach_primeira_venda"));
            Sim r = Sim.Load(s.Save(Em(D, 10)));
            Assert.AreEqual(-1L, r.Metas.ResgatarConquista("ach_primeira_venda"));
            Assert.AreEqual(-1L, r.Metas.ResgatarConquista("ach_nao_existe"));
            Assert.IsTrue(r.Metas.Ledger.Contains("a:ach_primeira_venda"));
        }

        [Test]
        public void Login_DiaPerdidoSoPausa_NuncaVoltaAoDia1_CicloSeguinteDepoisDo7()
        {
            Sim s = Aberta();
            s.Metas.Agora(Em(D, 10), Fuso);
            Assert.IsFalse(s.Metas.LoginPronto, "D0: o login abre no dia seguinte");
            s.Metas.Agora(Em(D + 1, 10), Fuso);
            Assert.AreEqual(40L, s.Metas.ResgatarLogin(), "dia 1: ouro 20 s");
            Assert.AreEqual(-1L, s.Metas.ResgatarLogin(), "1 por dia");
            s.Metas.Agora(Em(D + 5, 10), Fuso);   // perdeu 3 dias
            long renome = s.Metas.Renome;
            Assert.AreEqual(0L, s.Metas.ResgatarLogin(), "dia 2 (Renome), nao o dia 1");
            Assert.AreEqual(renome + 2, s.Metas.Renome);
            Assert.AreEqual(2, s.Metas.LoginDias);
            Assert.IsTrue(s.Metas.Ledger.Contains("l:1:2"));
            for (long d = D + 6; d < D + 11; d++) { s.Metas.Agora(Em(d, 10), Fuso); Assert.Greater(s.Metas.ResgatarLogin(), -1L, $"dia {d}"); }
            Assert.AreEqual(2, s.Metas.LoginCiclo, "depois do 7, ciclo 2");
            Assert.AreEqual(0, s.Metas.LoginDias);
            Assert.IsTrue(s.Metas.Ledger.Contains("l:1:7"), "a decoracao do ciclo 1 fica como posse no ledger");
            Assert.AreEqual(7L, s.Metas.Cont["login_claim"]);
            Sim r = Sim.Load(s.Save(Em(D + 10, 11)));
            r.Metas.Agora(Em(D + 10, 11), Fuso);
            Assert.AreEqual(-1L, r.Metas.ResgatarLogin(), "reabrir no mesmo dia nao paga outro");
            r.Metas.Agora(Em(D + 11, 11), Fuso);
            Assert.AreEqual(40L, r.Metas.ResgatarLogin(), "ciclo 2, dia 1");
            Assert.IsTrue(r.Metas.Ledger.Contains("l:2:1"));
        }

        [Test]
        public void SaveAntigo_SemMetas_ZeradoEConquistasRetroativas()
        {
            var bot = new Bot();
            var s = new Sim();
            for (float t = 0f; t < 15 * 60f; t += 1f / 30f) bot.Step(s, 1f / 30f);
            string velho = s.Save(1000);
            StringAssert.DoesNotContain("mt.", velho, "metas nunca abertas nao mudam o save");
            Sim r = Sim.Load(velho);
            Assert.IsFalse(r.Metas.Iniciado);
            Assert.IsEmpty(r.Metas.Ledger);
            Assert.AreEqual(0L, r.Metas.PassXp);
            r.Metas.Agora(Em(D, 10), Fuso);
            Assert.IsTrue(r.Metas.Iniciado);
            Assert.AreEqual((long)s.Sales, r.Metas.Cont["product_sold"], "retroativo pelos contadores do Sim");
            Assert.AreEqual((long)s.UpgradesBought, r.Metas.Cont["upgrade_buy"]);
            Assert.AreEqual(s.GoldEarned, r.Metas.Cont["currency_source"]);
            Assert.IsTrue(r.Metas.Conquista(MetasDefs.Achar("ach_vende100")).Pronta);
            Assert.IsTrue(r.Metas.Conquista(MetasDefs.Achar("ach_esteira")).Pronta, "comprou a Esteira antes das metas");
            for (int i = 0; i < Metas.NDiarias; i++) Assert.AreEqual(0L, r.Metas.Diaria(i).Feito, "diaria comeca do zero");
            StringAssert.Contains("mt.cal=", r.Save(Em(D, 10)));
        }

        [Test]
        public void Bot_ComMetasAbertas_JogaIgualAoSemMetas()
        {
            Sim a = new Sim(), b = new Sim();
            Bot ba = new Bot(), bb = new Bot();
            const float dt = 1f / 30f;
            for (int k = 0; k < 20 * 60 * 30; k++)
            {
                ba.Step(a, dt); bb.Step(b, dt);
                if (k % 30 == 0) b.Metas.Agora(Em(D, 10) + k / 30, Fuso);
            }
            Assert.IsTrue(b.Metas.Iniciado);
            Assert.AreEqual(a.Gold, b.Gold);
            Assert.AreEqual(a.GoldEarned, b.GoldEarned);
            Assert.AreEqual(a.Sales, b.Sales);
            Assert.AreEqual(a.UpgradesBought, b.UpgradesBought);
            Assert.AreEqual(a.RateEma, b.RateEma);
            Assert.AreEqual(a.Player.Pos.ToString(), b.Player.Pos.ToString());
        }

        /// <summary>Todo gatilho da taxonomia conta de verdade no jogo (bot de 45 min + cofre + resgates), e o numero de ajudantes bate com o Sim.</summary>
        [Test]
        public void Eventos_TodoGatilhoContaNoJogo()
        {
            var s = new Sim(); var bot = new Bot();
            s.Renome.Ganha(180);   // D-CORE-04: o ronda da vila (Renome 3) abre com as Ferramentas; o bot aceita e cumpre vendendo (contract_complete)
            for (float t = 0f; t < 45 * 60f; t += 1f / 30f) bot.Step(s, 1f / 30f);
            s.ApplyOffline(3600, 1);
            s.Metas.Agora(Em(D, 10), Fuso);
            s.Metas.Agora(Em(D + 1, 10), Fuso);
            s.Metas.ResgatarLogin();
            Completa(s, s.Metas.Diaria(0));
            s.Metas.ResgatarDiaria(0);
            s.Gold += 1000;   // B-CORE-04: o bot so sobe nivel depois da producao completa (~47 min); aqui o nivel 2 do Ajudante, como o jogador no menu
            Assert.IsTrue(s.Equipe.Comprar(WorkerDefs.IndexOf("wk_ajudante1")), "worker_upgrade: nivel 2 do Ajudante");
            s.T.NivelEstacaoQ = 200; s.Recompute();   // B-CORE-08: masterwork_*: uma obra-prima de verdade (Superior + projeto + golpe)
            s.Projetos.Ganha("bp_mw_primeira_lamina", BlueprintSourceKind.Milestone);
            Assert.AreEqual(Quality.Masterwork, MasterworksTests.ComGolpe(s, s.AnvilA));
            // raia H: Superior de verdade (Ev.Graded: o golpe numa Superior sem projeto), o Charter do Old Forge (producao, Renome 4, a obra),
            // marcos da Collection (os projetos), nivel III, o evento do T0 com a trilha inteira e o nivel 30 do Passe resgatado
            Assert.AreEqual(Quality.Superior, MasterworksTests.ComGolpe(s, s.AnvilA));
            for (int u = 0; u < s.UpgradeDefs.Length; u++) if (s.IsProduction(u)) s.Buy((Upgrade)u);
            s.Buy(Upgrade.AnvilsLv2); s.Buy(Upgrade.AnvilsLv3);
            s.Renome.Ganha(400);
            Assert.IsTrue(new Imperio(s).Charter(Distritos.OldForgeId), "Charter do Old Forge pelo caminho real");
            foreach (BlueprintDef d in s.Projetos.Defs) s.Projetos.Ganha(d.Id, d.Sources[0].Kind);
            s.Tick(1f / 30f, 0f, 0f);   // a Collection paga os marcos alcancados
            s.LiveOps.Agora(LiveOpsDefs.T0 + 3600);
            Assert.IsNotNull(s.LiveOps.Evento, "a Royal Week do T0");
            s.LiveOps.PontosEncomenda(s.LiveOps.Evento.Id, 1000);   // a trilha inteira pela encomenda Event
            s.LiveOps.Agora(LiveOpsDefs.T0 + 3601);
            s.LiveOps.Ganha(s.LiveOps.Passe.XpTotal);
            s.LiveOps.Resgatar(s.LiveOps.Passe.Niveis);
            MarcoColecaoDef pago = Array.Find(s.Colecao.Marcos, s.Colecao.Pago);
            foreach (string ev in Metas.Eventos)   // contador do mt.c ou derivado do nucleo (Metas.Valor)
            {
                var sonda = new MetaDef("ach_sonda", TipoMeta.Conquista, -1, ev, ev == "station_level" ? 3 : -1, 1, null, 0, 0, 0,
                    refId: ev == "district_unlock" ? Distritos.OldForgeId : ev == "collection_milestone" ? pago?.Id : null);
                Assert.Greater(s.Metas.Valor(sonda), 0L, ev);
            }
            var todos = new Sim();
            for (int u = 0; u < Upgrades.Count; u++) todos.Buy((Upgrade)u);
            Assert.AreEqual(Metas.AjudantesOldForge, todos.Workers.Count);
        }

        // ------------------------------------------------------------------ metas de sistemas (raia H)

        static MetaDef A(string id) => MetasDefs.Achar(id);

        /// <summary>A conquista `id` esta pronta e paga 1x: a 2a vez e depois do save/load devolvem -1 (o ID ficou no ledger). Devolve o ouro.</summary>
        static long UmaVez(Sim s, string id)
        {
            Assert.IsTrue(s.Metas.Conquista(A(id)).Pronta, id + " pronta");
            long g = s.Metas.ResgatarConquista(id);
            Assert.GreaterOrEqual(g, 0L, id);
            Assert.AreEqual(-1L, s.Metas.ResgatarConquista(id), id + ": 2a vez");
            Sim r = Sim.Load(s.Save(Em(D, 12)));
            Assert.IsTrue(r.Metas.Conquista(A(id)).Paga, id + ": paga depois do load");
            Assert.AreEqual(-1L, r.Metas.ResgatarConquista(id), id + ": depois do load");
            return g;
        }

        /// <summary>A vaga em que a semanal `id` cai, virando a semana a partir do dia `dia` (ate `max` semanas; `dia` fica na semana achada), ou -1.</summary>
        static int SemanaCom(Sim s, string id, ref long dia, int max)
        {
            for (int w = 0; w < max; w++, dia += 7)
            {
                s.Metas.Agora(Em(dia, 10), Fuso);
                int k = Array.IndexOf(IdsW(s), id);
                if (k >= 0) return k;
            }
            return -1;
        }

        /// <summary>
        /// Raia H (C-CORE-03 + D-CORE-04): o contrato da raiz cumprido com elmos vendidos na Rua dos Armeiros conta nas conquistas do Imperio
        /// (contract_complete) e o elmo conta pelo ID (product_sold.elmo, sem virar espada); cada uma paga 1x e o save do Imperio leva o contador.
        /// </summary>
        [Test]
        public void ContratoEElmo_VendidosNaRua_ContamNaRaiz_PagamUmaVez()
        {
            Imperio imp = Imperio.Chegada(Distritos.ArmorersRow);
            Sim raiz = imp.Raiz, row = imp.Ativo;
            raiz.RateEma = 2.0;
            raiz.Metas.Agora(Em(D, 10), Fuso);
            raiz.Contratos.Defs = new[] { new ContractDef("ctr_elmos", OrderDefs.OldForge, new[] { new ContractStage("elmo", 2, Quality.Normal) }, 10f, 1, 15, null) };
            Assert.IsTrue(raiz.Contratos.Forca("ctr_elmos"));
            Item elmo = (Item)Items.IndexOf("elmo", row.ItemDefs);
            Assert.AreEqual(0L, raiz.Metas.Conquista(A("ach_contrato1")).Feito);
            EncTeste.Vende(row, elmo);
            EncTeste.Vende(row, elmo);
            Assert.IsTrue(raiz.Contratos.Concluido("ctr_elmos"), "a venda da Row cumpriu o contrato da raiz");
            Assert.AreEqual(1L, raiz.Metas.Conquista(A("ach_contratos12")).Feito, "1 de 12");
            Assert.AreEqual(2L, raiz.Metas.Conquista(A("ach_elmos100")).Feito, "o elmo conta pelo ID na raiz");
            Assert.AreEqual(0L, raiz.Metas.Conquista(A("ach_espadas_venda100")).Feito, "e nao vira espada (handle 2 da Row)");
            long xp = raiz.Metas.PassXp;
            Assert.AreEqual(0L, UmaVez(raiz, "ach_contrato1"), "XP da conquista, sem ouro");
            Assert.AreEqual(xp + Array.Find(MetasDefs.Conquistas, d => d.Id == "ach_contrato1").Xp, raiz.Metas.PassXp);
            for (int k = 0; k < 98; k++) EncTeste.Vende(row, elmo);
            UmaVez(raiz, "ach_elmos100");
            Assert.IsFalse(raiz.Metas.Conquista(A("ach_contratos12")).Pronta);
            Imperio r = Imperio.Load(imp.Save(1));
            Assert.AreEqual(100L, r.Raiz.Metas.Conquista(A("ach_elmos100")).Feito, "product_sold.elmo no mt.c");
            Assert.AreEqual(-1L, r.Raiz.Metas.ResgatarConquista("ach_elmos100"));
        }

        /// <summary>Raia H: a semanal de contrato so entra no rodizio com um contrato a cumprir (o ronda pede Renome 3); fecha pelo Ev.ContractDone
        /// e paga 1x (save/load inclusive).</summary>
        [Test]
        public void SemanalDeContrato_SoComUmACumprir_FechaPeloContrato_UmaVez()
        {
            Sim s = Aberta();
            long dia = D;
            Assert.AreEqual(-1, SemanaCom(s, "wkl_contrato1", ref dia, 25), "sem contrato a cumprir nunca entra");
            s.Renome.Ganha(180);
            Assert.IsNotNull(s.Contratos.Oferta, "o ronda abriu");
            int k = SemanaCom(s, "wkl_contrato1", ref dia, 25);
            Assert.GreaterOrEqual(k, 0, "com o ronda aberto entra");
            Assert.IsFalse(s.Metas.Semanal(k).Pronta);
            Assert.IsTrue(s.Contratos.Aceitar());
            s.Contratos.Ativo.Etapa = s.Contratos.Ativo.Def.Etapas.Length;
            s.Tick(1f / 30f, 0f, 0f);
            Assert.IsTrue(s.Metas.Semanal(k).Pronta, "o contrato cumprido fechou a semanal");
            Assert.Greater(s.Metas.ResgatarSemanal(k), 0L);
            Assert.AreEqual(-1L, s.Metas.ResgatarSemanal(k));
            Sim r = Sim.Load(s.Save(Em(dia, 11)));
            r.Metas.Agora(Em(dia, 11), Fuso);
            Assert.IsTrue(r.Metas.Semanal(k).Paga);
            Assert.AreEqual(-1L, r.Metas.ResgatarSemanal(k), "reabrir nao paga de novo");
        }

        /// <summary>Raia H (B-CORE-03): Superior pela automacao (Ev.Graded) e golpe de mestre de verdade (Sim.Strike) pagam as conquistas 1x; as
        /// semanais de Superior e de golpe so entram no rodizio com a Quality aberta (uma linha ja faz Fina).</summary>
        [Test]
        public void QualidadeEGolpe_PeloCaminhoReal_PagamUmaVez_SemanaisSoComAQualityAberta()
        {
            long dia = D;
            Sim sem = Aberta();
            Assert.IsFalse(sem.Encomendas.TipoAberto(OrderType.Quality));
            Assert.AreEqual(-1, SemanaCom(sem, "wkl_superior20", ref dia, 25), "sem a Quality aberta");
            Assert.AreEqual(-1, SemanaCom(sem, "wkl_golpe50", ref dia, 25));
            Sim s = QualidadeTests.ComQ(200);   // Ferramentas e Bigornas II com q 200: toda espada sai Superior
            s.RateEma = 2.0;
            s.Metas.Agora(Em(D, 10), Fuso);
            Assert.IsFalse(s.Metas.Conquista(A("ach_primeira_fina")).Pronta);
            Assert.AreEqual(Quality.Superior, QualidadeTests.Peca(s));
            Assert.AreEqual(1L, s.Metas.Conquista(A("ach_primeira_superior")).Feito, "Ev.Graded Superior");
            Assert.AreEqual(1L, s.Metas.Conquista(A("ach_primeira_fina")).Feito, "Superior conta como Fina ou melhor");
            Assert.AreEqual(0L, s.Metas.Conquista(A("ach_golpe1")).Feito);
            Assert.AreEqual(Quality.Superior, MasterworksTests.ComGolpe(s, s.AnvilA), "sem projeto, o golpe numa Superior fica Superior");
            Assert.AreEqual(1L, s.Metas.Conquista(A("ach_golpe1")).Feito, "Sim.Strike");
            Assert.AreEqual(2L, s.Metas.Conquista(A("ach_fina100")).Feito);
            s.Metas.On(Ev.Graded, s.ShieldBench.Index, (int)Quality.Fine);   // o On que o Sim.Emit chama: uma Fina de escudo
            Assert.AreEqual(3L, s.Metas.Conquista(A("ach_fina100")).Feito);
            Assert.AreEqual(2L, s.Metas.Conquista(A("ach_superior1k")).Feito, "Fina nao e' Superior");
            long renome = s.Renome.Total;
            UmaVez(s, "ach_primeira_superior");
            Assert.AreEqual(renome + 3L, s.Renome.Total);
            Assert.Greater(UmaVez(s, "ach_golpe1"), 0L);
            Assert.Greater(UmaVez(s, "ach_primeira_fina"), 0L);
            Assert.IsTrue(s.Encomendas.TipoAberto(OrderType.Quality));
            dia = D;
            Assert.GreaterOrEqual(SemanaCom(s, "wkl_superior20", ref dia, 25), 0);
            Assert.GreaterOrEqual(SemanaCom(s, "wkl_golpe50", ref dia, 25), 0);
        }

        /// <summary>
        /// Raia H: as derivadas do estado do nucleo (nada novo no save): projetos, nivel de estacao, Renome, marco da Collection e Charter. Feitas
        /// antes da 1a abertura das metas, chegam pelo retroativo; cada uma paga 1x; conquista de Renome nunca da Renome.
        /// </summary>
        [Test]
        public void Derivadas_ProjetosEstacaoRenomeColecaoECharter_RetroativasUmaVez()
        {
            Sim s = QualidadeTests.ComQ(200);
            s.RateEma = 2.0;
            BlueprintDef[] bps = s.Projetos.Defs;
            for (int i = 0; i < 9; i++) Assert.IsTrue(s.Projetos.Ganha(bps[i].Id, bps[i].Sources[0].Kind));
            s.Metas.Agora(Em(D, 10), Fuso);
            Assert.AreEqual(9L, s.Metas.Conquista(A("ach_bp10")).Feito, "retroativo: os projetos que ja tinha");
            Assert.IsFalse(s.Metas.Conquista(A("ach_bp10")).Pronta);
            Assert.IsTrue(s.Projetos.Ganha(bps[9].Id, bps[9].Sources[0].Kind));
            Assert.Greater(UmaVez(s, "ach_bp1"), 0L);
            long rn = s.Renome.Total;
            UmaVez(s, "ach_bp10");
            Assert.AreEqual(rn + 5L, s.Renome.Total);
            Assert.IsFalse(s.Metas.Conquista(A("ach_bp18")).Pronta);
            // nivel de estacao: Bigornas II nao e' o nivel 3; a meta das 5 pede as 4 familias com nivel V
            Assert.IsFalse(s.Metas.Conquista(A("ach_estacao_n3")).Pronta, "so o nivel II");
            s.Buy(Upgrade.AnvilsLv3);
            UmaVez(s, "ach_estacao_n3");
            Assert.AreEqual(4L, A("ach_estacoes_n5").Alvo);
            Assert.AreEqual(0L, s.Metas.Conquista(A("ach_estacoes_n5")).Feito);
            // Renome: o nivel 4 fecha as duas; nenhuma da Renome (sem laco)
            Assert.IsFalse(s.Metas.Conquista(A("ach_renome1")).Pronta);
            s.Renome.Ganha(RenomeDefs.Niveis[4].Limiar);
            UmaVez(s, "ach_renome1");
            rn = s.Renome.Total;
            UmaVez(s, "ach_renome4");
            Assert.AreEqual(rn, s.Renome.Total, "conquista de Renome nunca da Renome");
            // Collection: o marco de 10 projetos paga e nao conta; Produtos 100% (as 15 produto x qualidade) conta
            s.Projetos.Ganha("bp_mw_primeira_lamina", BlueprintSourceKind.Milestone);
            s.Tick(1f / 30f, 0f, 0f);
            Assert.IsTrue(s.Colecao.Pago(Array.Find(s.Colecao.Marcos, m => m.Id == "col_bp_old_forge_50")));
            Assert.IsFalse(s.Metas.Conquista(A("ach_colecao_produtos")).Pronta, "outro marco nao conta");
            for (int i = 0; i < s.ItemDefs.Length; i++)
                if (s.IsProduct((Item)i)) { s.SoldItems[i] = 3; s.Projetos.Finas[i] = 2; s.Colecao.Superiores[i] = 1; }
            s.Tick(1f / 30f, 0f, 0f);
            UmaVez(s, "ach_colecao_produtos");
            // Charter do Old Forge pelo Imperio (producao completa, Renome 4, 1 obra-prima): o Renome.OnCharter grava charter:old_forge
            for (int u = 0; u < s.UpgradeDefs.Length; u++) if (s.IsProduction(u)) s.Buy((Upgrade)u);
            Assert.AreEqual(Quality.Masterwork, MasterworksTests.ComGolpe(s, s.AnvilA));
            var imp = new Imperio(s);
            Assert.IsFalse(s.Metas.Conquista(A("ach_charter1")).Pronta);
            Assert.IsTrue(imp.Charter(Distritos.OldForgeId));
            UmaVez(s, "ach_charter1");
        }

        /// <summary>Raia H: save do #64 com o contrato concluido e as metas nunca abertas (sem mt.*): a conquista chega pelo retroativo (ctr=).</summary>
        [Test]
        public void SaveAntigo_ContratoConcluido_ConquistaPeloRetroativo()
        {
            Sim s = Aberta();
            s.Renome.Ganha(180);
            Assert.IsTrue(s.Contratos.Aceitar());
            s.Contratos.Ativo.Etapa = s.Contratos.Ativo.Def.Etapas.Length;
            s.Tick(1f / 30f, 0f, 0f);
            string velho = s.Save(1000);
            StringAssert.DoesNotContain("mt.", velho);
            StringAssert.Contains("ctr=ctr_ronda_vila", velho);
            Sim r = Sim.Load(velho);
            r.Metas.Agora(Em(D, 10), Fuso);
            UmaVez(r, "ach_contrato1");
        }

        /// <summary>ach_distritos3 (3 distritos abertos) e ach_mw30 (30 obras no Imperio) derivam do ledger do Renome (charter:, mw:): um save
        /// com 2 Charters e 30 obras e nenhuma meta aberta ganha as duas pelo retroativo, 1x; com 1 Charter e 29 obras, nenhuma.</summary>
        [Test]
        public void SaveAntigo_DistritosEObrasDoImperio_ConquistaPeloRetroativo()
        {
            Assert.That(Distritos.All.Length, Is.AtLeast(3), "premissa: 3 distritos no build");
            long obras = 0; foreach (DistrictDef d in Distritos.All) obras += d.Masterworks.Length;
            Assert.That(obras, Is.AtLeast(30), "premissa: 30 obras alcancaveis no build");
            Sim s = Aberta();
            s.Renome.OnCharter(Distritos.OldForgeId);
            for (int i = 0; i < 29; i++) s.Renome.Credita(Hall.Prefixo + "mw_teste_" + i, 0);
            s.Tick(1f / 30f, 0f, 0f);
            Assert.IsFalse(s.Metas.Conquista(A("ach_distritos3")).Pronta, "2 abertos");
            Assert.IsFalse(s.Metas.Conquista(A("ach_mw30")).Pronta, "29 obras");
            s.Renome.OnCharter("armorers_row");
            s.Renome.Credita(Hall.Prefixo + "mw_teste_29", 0);
            string velho = s.Save(1000);
            StringAssert.DoesNotContain("mt.", velho);
            Sim r = Sim.Load(velho);
            r.Metas.Agora(Em(D, 10), Fuso);
            UmaVez(r, "ach_distritos3");
            UmaVez(r, "ach_mw30");
        }

        [Test]
        public void Volume_E_ConteudoReal_SemViolacao()
        {
            Assert.GreaterOrEqual(MetasDefs.Diarias.Length, 30);
            Assert.GreaterOrEqual(MetasDefs.Semanais.Length, 12);
            Assert.GreaterOrEqual(MetasDefs.Conquistas.Length, 60);
            Assert.IsEmpty(ContentValidator.Missoes(MetasDefs.Diarias, MetasDefs.Semanais, MetasDefs.Conquistas, MetasDefs.LoginDias, Tuning.Padrao).ToList());
        }

        static List<string> Valida(MetaDef[] dly = null, MetaDef[] wkl = null, MetaDef[] ach = null, LoginDia[] login = null) =>
            ContentValidator.Missoes(dly ?? MetasDefs.Diarias, wkl ?? MetasDefs.Semanais, ach ?? MetasDefs.Conquistas, login ?? MetasDefs.LoginDias, Tuning.Padrao).ToList();

        static MetaDef[] Com(MetaDef[] defs, MetaDef extra) => defs.Concat(new[] { extra }).ToArray();

        static void Acusa(List<string> erros, string trecho) => Assert.IsTrue(erros.Exists(e => e.Contains(trecho)), $"esperava \"{trecho}\" em: {string.Join(" | ", erros)}");

        [Test]
        public void Validator_DefeitosInjetados_Acusa()
        {
            MetaDef Dly(string id, string ev, int filtro, long alvo, int ouro = 20, int xp = 100, int pool = 0, string abre = "ferramentas") =>
                new MetaDef(id, TipoMeta.Diaria, pool, ev, filtro, alvo, abre, ouro, xp, 0);
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_vende50", "product_sold", -1, 10))), "ID repetido");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_contrato", "royal_contract_complete", -1, 10))), "que o Sim nao emite");   // o nome do LIVEOPS_V1 antigo
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_perde", "client_left", -1, 10))), "pune ou exige anuncio");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_rico", "product_sold", -1, 10, ouro: 300))), "fora da faixa");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_quebrado", "product_sold", -1, 10, ouro: 22))), "fora da faixa");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_vazia", "product_sold", -1, 10, ouro: 0, xp: 0))), "sem recompensa");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_vip5", "vip_served", -1, 5))), "nao fecham sem anuncio");
            Acusa(Valida(wkl: Com(MetasDefs.Semanais, new MetaDef("wkl_vip40", TipoMeta.Semanal, -1, "vip_served", -1, 40, null, 60, 350, 5))), "nao fecham sem anuncio");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_item9", "product_sold", 9, 10))), "filtro 9");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_abre", "product_sold", -1, 10, abre: "nada"))), "nao e' upgrade");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_zero", "product_sold", -1, 0))), "alvo 0");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_pool", "product_sold", -1, 10, pool: -1))), "pool -1");
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("ach_errado", "product_sold", -1, 10))), "fora do formato");
            Acusa(Valida(MetasDefs.Diarias.Take(29).ToArray()), "pede pelo menos 30");
            Acusa(Valida(ach: MetasDefs.Conquistas.Take(59).ToArray()), "pede pelo menos 60");
            Acusa(Valida(MetasDefs.Diarias.Where(d => d.Pool != 1).ToArray()), "pool B com 0");
            Acusa(Valida(login: MetasDefs.LoginDias.Take(6).ToArray()), "6 dias");
            Acusa(Valida(login: Enumerable.Repeat(new LoginDia(60, 0), 7).ToArray(), dly: MetasDefs.Diarias.Select(d =>
                new MetaDef(d.Id, d.Tipo, d.Pool, d.Evento, d.Filtro, d.Alvo, d.Abre, 30, d.Xp, 0)).ToArray(), wkl: MetasDefs.Semanais.Select(d =>
                new MetaDef(d.Id, d.Tipo, d.Pool, d.Evento, d.Filtro, d.Alvo, d.Abre, 90, d.Xp, d.Renome)).ToArray()), "acima de 180");
            // raia H: alvo dentro do conteudo do build (requiresContent), filtro por ID so onde vale e que exista, Renome que nao da Renome
            MetaDef Ach(string id, string ev, int filtro, long alvo, int renome = 0, string refId = null) =>
                new MetaDef(id, TipoMeta.Conquista, -1, ev, filtro, alvo, null, 0, 50, renome, refId: refId);
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_c13", "contract_complete", -1, 13))), "acima do que o build tem (12)");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_bp99", "blueprint_unlock", -1, 99))), "acima do que o build tem");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_ev11", "event_enter", -1, 11))), "acima do que o build tem (10)");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_n5x5", "station_level", 5, 5))), "acima do que o build tem (4)");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_ch2", "district_unlock", -1, 2, refId: "old_forge"))), "acima do que o build tem (1)");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_n1", "station_level", 1, 1))), "filtro 1 invalido");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_q9", "quality_crafted", 9, 1))), "filtro 9 invalido");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_rn", "reputation_level", -1, 2, renome: 5))), "laco");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_ch", "district_unlock", -1, 1, refId: "nada"))), "ID \"nada\" invalido");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_ch0", "district_unlock", -1, Distritos.All.Length + 1))), $"acima do que o build tem ({Distritos.All.Length})");   // sem Ref = distritos abertos
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_mwx", "masterwork_empire", -1, 999))), "acima do que o build tem");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_col", "collection_milestone", -1, 1, refId: "col_x"))), "ID \"col_x\" invalido");
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_lingote", "product_sold", -1, 1, refId: "lingote"))), "ID \"lingote\" invalido");   // insumo
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_elmo2", "product_sold", 2, 1, refId: "elmo"))), "ID \"elmo\" invalido");   // ID e filtro
            Acusa(Valida(ach: Com(MetasDefs.Conquistas, Ach("ach_ref", "order_done", -1, 1, refId: "elmo"))), "ID \"elmo\" invalido");   // ID fora do lugar
        }

        [Test]
        public void Textos_DasMetas_CabemNosTresIdiomasEExistem()
        {
            try
            {
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    foreach (MetaDef d in MetasDefs.Diarias.Concat(MetasDefs.Semanais).Concat(MetasDefs.Conquistas))
                    {
                        string t = Textos.Meta(d, d.Conta == Conta.Segundos ? 9990000 : d.Alvo);
                        StringAssert.DoesNotContain("[", t, $"{i}: {d.Id}");
                        Assert.LessOrEqual(t.Length, Textos.MetaMax, $"{i}: {d.Id} \"{t}\"");
                        string p = Textos.Premio(d.OuroS > 0 ? 9990000 : 0, d.Xp, d.Renome, d.Insignia);
                        StringAssert.DoesNotContain("[", p, $"{i}: {d.Id}");
                        Assert.LessOrEqual(p.Length, Textos.PremioMax, $"{i}: {d.Id} \"{p}\"");
                    }
                }
                Loc.Atual = Idioma.Pt;
                Assert.AreEqual("Venda 50 produtos", Textos.Meta(MetasDefs.Achar("dly_vende50"), 50));
                Assert.AreEqual("Fabrique 1 escudo", Textos.Meta(MetasDefs.Achar("ach_primeiro_escudo"), 1));
                Assert.AreEqual("Ganhe 1.500 em vendas", Textos.Meta(MetasDefs.Achar("dly_renda5min"), 1500));
                Assert.AreEqual("Compre: Esteira", Textos.Meta(MetasDefs.Achar("ach_esteira"), 1));
                Assert.AreEqual("+120 de ouro  ·  +100 XP", Textos.Premio(120, 100, 0, null));
                Loc.Atual = Idioma.En;
                Assert.AreEqual("Deliver 2 orders", Textos.Meta(MetasDefs.Achar("dly_enc2"), 2));
                Assert.AreEqual("Earn 1,500 from sales", Textos.Meta(MetasDefs.Achar("dly_renda5min"), 1500));
                Loc.Atual = Idioma.Es;
                Assert.AreEqual("Atiende a 1 VIP", Textos.Meta(MetasDefs.Achar("dly_vip1"), 1));
            }
            finally { Loc.Atual = Idioma.Pt; }
        }
    }
}
