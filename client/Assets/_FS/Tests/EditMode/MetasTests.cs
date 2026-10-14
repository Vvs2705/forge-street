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
            // semana: (semana x 3 + k) mod 14 a partir de 3; joias100 travada (Joalheria) -> a seguinte
            CollectionAssert.AreEqual(new[] { "wkl_melhoria10", "wkl_vip15", "wkl_cofre5", "wkl_diarias15" }, IdsW(a));
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
                Assert.AreEqual(0, ds[2].Pool, $"dia {d}: a C sem gatilho cai na A");
            }
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
            foreach (string ev in Metas.Eventos)
                Assert.Greater(s.Metas.Cont.TryGetValue(ev, out long v) ? v : 0, 0L, ev);
            var todos = new Sim();
            for (int u = 0; u < Upgrades.Count; u++) todos.Buy((Upgrade)u);
            Assert.AreEqual(Metas.AjudantesOldForge, todos.Workers.Count);
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
            Acusa(Valida(Com(MetasDefs.Diarias, Dly("dly_qualidade", "quality_crafted", -1, 10))), "que o Sim nao emite");
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
