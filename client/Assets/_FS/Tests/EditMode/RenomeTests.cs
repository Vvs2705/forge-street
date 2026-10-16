using System;
using System.Linq;
using System.Reflection;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// B-CORE-06 (GDD §14, §23 #11): niveis por limiar, monotonico (nenhum caminho reduz, sem API de gasto), fontes de hoje pelo fluxo do Sim
    /// (encomenda +1, bau +5, Superior, metas sem pagar 2x), ganchos (Charter), retroativo do save antigo 1x, save/load, o bot igual com
    /// qualquer Renome e o validator. Renome e' long: literais L e asserts separados (NUnit 3.5 do Unity: int != long em tupla).
    /// </summary>
    public class RenomeTests
    {
        const float Dt = 1f / 30f;

        static Sim Bot(float minutos)
        {
            var s = new Sim();
            var bot = new Bot();
            for (float t = 0f; t < minutos * 60f; t += Dt) bot.Step(s, Dt);
            return s;
        }

        static long BausAbertos(Sim s) => s.Chests.Count(c => c.State == 2);

        static string SemRn(string save) => string.Join("\n", save.Split('\n').Where(l => !l.StartsWith("rn=", StringComparison.Ordinal)));

        [Test]
        public void Niveis_PorLimiar_GoldenDoConteudoV1()
        {
            CollectionAssert.AreEqual(new long[] { 0, 20, 70, 180, 400, 750, 1250, 2000, 3000, 4200, 6500 }, RenomeDefs.Niveis.Select(d => d.Limiar).ToArray(),
                "CONTEUDO_V1 §11 / ECONOMIA_WAVE_B §5.2");
            var r = new Sim().Renome;
            Assert.AreEqual(0, r.Nivel);
            Assert.AreEqual(20L, r.Proximo.Limiar);
            r.Ganha(19);
            Assert.AreEqual(0, r.Nivel, "19");
            r.Ganha(1);
            Assert.AreEqual(1, r.Nivel, "20: o limiar ja conta");
            r.Ganha(49);
            Assert.AreEqual(1, r.Nivel, "69");
            r.Ganha(1);
            Assert.AreEqual(2, r.Nivel, "70");
            r.Ganha(6499 - 70);
            Assert.AreEqual(9, r.Nivel, "6499");
            r.Ganha(1);
            Assert.AreEqual(10, r.Nivel, "6500");
            Assert.IsNull(r.Proximo, "10 e' o ultimo");
            Assert.AreEqual("renome.10", r.Atual.NomeKey);
        }

        [Test]
        public void Libera_PeloNivel()
        {
            var r = new Sim().Renome;
            Assert.IsFalse(r.Libera("ord_blueprint_old_forge"));
            Assert.IsFalse(r.Libera("wk_baltazar"));
            r.Ganha(20);
            Assert.IsTrue(r.Libera("ord_blueprint_old_forge"));
            Assert.IsTrue(r.Libera("vaga_encomenda_2"));
            Assert.IsFalse(r.Libera("ord_royal_old_forge"), "Royal e' do nivel 2");
            Assert.IsFalse(r.Libera("charter_old_forge"));
            r.Ganha(400 - 20);
            Assert.IsTrue(r.Libera("charter_old_forge"), "Charter do Old Forge no 4 (V15)");
            Assert.IsTrue(r.Libera("wk_otavio"), "o que niveis abaixo liberam continua liberado");
            Assert.IsFalse(r.Libera("charter_armorers_row"));
            Assert.IsFalse(r.Libera("nao_existe"));
            Assert.AreEqual(3, RenomeDefs.NivelQueLibera("bp_mw_escudo_brasao"));
            Assert.AreEqual(-1, RenomeDefs.NivelQueLibera("nao_existe"));
        }

        [Test]
        public void Monotonico_NenhumCaminhoReduz()
        {
            var r = new Sim().Renome;
            r.Ganha(50);
            r.Ganha(0);
            r.Ganha(-5);
            r.Ganha(long.MinValue);
            Assert.AreEqual(50L, r.Total, "Ganha so soma");
            Assert.IsTrue(r.Credita("mw:teste", -30));
            Assert.AreEqual(50L, r.Total, "credito negativo nao desconta");
            r.Ganha(long.MaxValue);
            r.Ganha(long.MaxValue);
            Assert.AreEqual(long.MaxValue, r.Total, "satura, nao vira negativo");
            Assert.AreEqual(10, r.Nivel);

            // load: lixo e negativo viram 0 (nunca negativo)
            Assert.AreEqual(0L, Sim.Load("v=3\nrn=-50,-3,rn:retro\n").Renome.Total);
            Assert.AreEqual(0L, Sim.Load("v=3\nrn=-50,-3,rn:retro\n").Renome.Superiores);
            Assert.AreEqual(0L, Sim.Load("v=3\nrn=abc\n").Renome.Total);

            // jogo de verdade: tick a tick, nunca cai
            var s = new Sim();
            var bot = new Bot();
            long antes = 0;
            for (float t = 0f; t < 25 * 60f; t += Dt)
            {
                bot.Step(s, Dt);
                Assert.GreaterOrEqual(s.Renome.Total, antes, $"t = {s.Time:0.0}");
                antes = s.Renome.Total;
            }
            Assert.Greater(s.Renome.Total, 0L, "premissa: o bot ganhou Renome");
        }

        /// <summary>Sem API de gasto: o total e o contador nao tem setter publico, e os metodos publicos sao so estes (um "Gasta" novo fica vermelho aqui).</summary>
        [Test]
        public void SemApiDeGasto()
        {
            Type t = typeof(Renome);
            Assert.IsNull(t.GetProperty(nameof(Renome.Total)).GetSetMethod(), "Total sem setter publico");
            Assert.IsNull(t.GetProperty(nameof(Renome.Superiores)).GetSetMethod());
            string[] publicos = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName).Select(m => m.Name).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(new[] { "Credita", "Ganha", "Libera", "Load", "On", "OnCharter", "OnVendaQualidade", "Save" }, publicos);
            Assert.IsEmpty(t.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == typeof(long) || f.FieldType == typeof(int)),
                "nenhum numero publico gravavel");
        }

        /// <summary>Fontes de hoje pelo fluxo do Sim: encomenda entregue pelo tipo (B-CORE-05: Common +1 ... Royal +10), bau aberto +5, +1 a cada
        /// 10 Superiores vendidas; nada mais muda o total.</summary>
        [Test]
        public void Fontes_EncomendaBauESuperior_PeloFluxoDoSim()
        {
            Sim s = Bot(30);
            Assert.Greater(s.OrderCount, 0, "premissa: encomendas entregues");
            Assert.Greater(BausAbertos(s), 0L, "premissa: baus abertos");
            Assert.Greater(s.Encomendas.RenomeGanho, (long)s.OrderCount, "premissa: tipos com mais de +1 (Bulk, Quality, Mixed)");
            Assert.AreEqual(s.Encomendas.RenomeGanho + 5L * BausAbertos(s) + s.Renome.Superiores / RenomeDefs.SuperiorACada, s.Renome.Total);
        }

        /// <summary>A venda de verdade no balcao (Sim.Sell) conta a Superior, nao a Fina nem a Normal.</summary>
        [Test]
        public void Superior_VendidaNoBalcao_Conta()
        {
            var s = new Sim();
            s.Stock[(int)Item.Sword] = 3; s.StockQ[(int)Item.Sword][(int)Quality.Fine] = 1; s.StockQ[(int)Item.Sword][(int)Quality.Superior] = 1;
            int vendas = 0;
            for (float t = 0f; t < 120f && vendas < 3; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events) if (e.Kind == Ev.Sold) vendas++;
            }
            Assert.AreEqual(3, vendas, "premissa: as 3 vendidas");
            Assert.AreEqual(1L, s.Renome.Superiores);
            Assert.AreEqual(0L, s.Renome.Total, "1 Superior: ainda nao da +1");
        }

        /// <summary>As metas creditam no Renome real 1x por ID (o ledger delas), e o save com rn= nao soma de novo o mt.acc.</summary>
        [Test]
        public void Metas_CreditamNoRenomeReal_SemPagar2x()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            s.Metas.Agora(20000L * Calendario.Dia, 0);   // abre as metas: o save leva as linhas mt.*
            Assert.Greater(s.Metas.Paga("a:teste_renome", 20, 100, 3), 0L);
            Assert.AreEqual(-1L, s.Metas.Paga("a:teste_renome", 20, 100, 3), "mesmo ID: nada");
            Assert.AreEqual(3L, s.Renome.Total);
            Assert.AreEqual(3L, s.Metas.Renome);
            string text = s.Save(1000);
            StringAssert.Contains("mt.acc=", text);
            StringAssert.Contains("rn=3,0\n", text);
            Sim r = Sim.Load(text);
            Assert.AreEqual(3L, r.Renome.Total, "rn= presente: o mt.acc nao entra de novo");
            Assert.AreEqual(text, r.Save(1000), "save estavel");
            Assert.AreEqual(-1L, r.Metas.Paga("a:teste_renome", 20, 100, 3), "o ID volta do save");
            Assert.AreEqual(3L, r.Renome.Total);
        }

        /// <summary>Save da 0.1-0.7 (sem rn=): encomendas + 5 x baus abertos + o Renome que as metas ja tinham, 1x pelo ID rn:retro.</summary>
        [Test]
        public void Retroativo_SaveAntigo_UmaVez()
        {
            const string velho = "v=1\nt=900\ngold=50\nms=2,2,1,0\nord=-1,0,0,0,7,30\n";
            Sim a = Sim.Load(velho);
            Assert.AreEqual(7, a.OrderCount);
            Assert.AreEqual(7L + 5L * 2L, a.Renome.Total, "7 encomendas + 2 baus abertos (o disponivel nao conta)");
            Assert.IsTrue(a.Renome.Ledger.Contains(Renome.TxRetro));
            Assert.IsFalse(a.Renome.Credita(Renome.TxRetro, 17), "o mesmo ID nao credita de novo");
            Assert.AreEqual(17L, a.Renome.Total);
            string novo = a.Save(1000);
            StringAssert.Contains("rn=17,0," + Renome.TxRetro + "\n", novo);
            Sim b = Sim.Load(novo);
            Assert.AreEqual(17L, b.Renome.Total, "reabrir o save migrado nao paga de novo");
            Assert.AreEqual(novo, b.Save(1000), "estavel");
            Assert.AreEqual(17L, Sim.Load(velho).Renome.Total, "deterministico");

            // main de antes do B-CORE-06 (metas com Renome guardado no mt.acc): entra 1x
            Assert.AreEqual(20L, Sim.Load(velho + "mt.acc=100,3\n").Renome.Total);
            // saves antigos de verdade: sem encomenda (ms= so)
            Assert.AreEqual(15L, Sim.Load(CoreTests.Save20Flags).Renome.Total, "ms=2,2,1,2");
            Assert.AreEqual(20L, Sim.Load(CoreTests.SaveV03).Renome.Total, "ms=2,2,2,2");
            Assert.AreEqual(0L, Sim.Load(CoreTests.SaveV01).Renome.Total);
        }

        /// <summary>O retroativo de um jogo do bot sem a linha rn= da exatamente o que o jogo ao vivo deu; o save novo (com rn=) nao ganha retroativo.</summary>
        [Test]
        public void Retroativo_IgualAoAoVivo_ESaveNovoNaoGanha()
        {
            Sim s = Bot(10);   // B-CORE-05: antes das Ferramentas toda encomenda e' Common (+1), como nos saves sem rn=
            Assert.Greater(s.Renome.Total, 0L, "premissa");
            string text = s.Save(1000);
            Sim novo = Sim.Load(text);
            Assert.AreEqual(s.Renome.Total, novo.Renome.Total, "save novo: o total do rn=, sem retroativo por cima");
            Assert.IsFalse(novo.Renome.Ledger.Contains(Renome.TxRetro));
            Sim velho = Sim.Load(SemRn(text));
            Assert.AreEqual(s.Renome.Total - s.Renome.Superiores / RenomeDefs.SuperiorACada, velho.Renome.Total,
                "o retroativo reproduz o ao vivo (menos as Superiores: nenhum save antigo as conta)");
            Assert.IsTrue(velho.Renome.Ledger.Contains(Renome.TxRetro));
            Assert.AreEqual(0L, Sim.Load("").Renome.Total, "jogo novo: nada");
        }

        [Test]
        public void Ganchos_SuperiorACada10_CharterUmaVezPorDistrito_SobrevivemAoSave()
        {
            var s = new Sim();
            Renome r = s.Renome;
            for (int i = 0; i < 50; i++) { r.OnVendaQualidade(0); r.OnVendaQualidade(1); }
            Assert.AreEqual(0L, r.Total, "Normal e Fina nao contam");
            for (int i = 0; i < 9; i++) r.OnVendaQualidade(Renome.Superior);
            Assert.AreEqual(0L, r.Total, "9 Superiores");
            r.OnVendaQualidade(Renome.Superior);
            Assert.AreEqual(1L, r.Total, "a 10a da +1");
            for (int i = 0; i < 15; i++) r.OnVendaQualidade(Renome.Superior);
            Assert.AreEqual(2L, r.Total, "25 Superiores = +2");
            Assert.IsTrue(r.OnCharter("old_forge"));
            Assert.IsFalse(r.OnCharter("old_forge"), "1x por distrito");
            Assert.AreEqual(102L, r.Total);
            Assert.IsTrue(r.OnCharter("armorers_row"));
            Assert.AreEqual(202L, r.Total);

            Sim b = Sim.Load(s.Save(1));
            Assert.AreEqual(25L, b.Renome.Superiores);
            Assert.IsFalse(b.Renome.OnCharter("old_forge"), "o ID do Charter volta do save");
            for (int i = 0; i < 5; i++) b.Renome.OnVendaQualidade(Renome.Superior);
            Assert.AreEqual(203L, b.Renome.Total, "a 30a Superior conta a partir do contador salvo");
        }

        /// <summary>Renome nao paga ouro: ele so libera vagas e tipos de encomenda (B-CORE-05), e isso so depois das Ferramentas. Ate la o bot
        /// joga igual com o Renome no maximo desde o comeco.</summary>
        [Test]
        public void Bot_ComQualquerRenome_JogaIgual()
        {
            Sim a = new Sim(), b = new Sim();
            b.Renome.Ganha(long.MaxValue);
            Bot ba = new Bot(), bb = new Bot();
            for (int k = 0; k < 10 * 60 * 30; k++) { ba.Step(a, Dt); bb.Step(b, Dt); }
            Assert.IsFalse(a.Bought[(int)Upgrade.Tools], "premissa: ainda sem as Ferramentas");
            Assert.AreEqual(a.Gold, b.Gold);
            Assert.AreEqual(a.GoldEarned, b.GoldEarned);
            Assert.AreEqual(a.Sales, b.Sales);
            Assert.AreEqual(a.OrderCount, b.OrderCount);
            Assert.AreEqual(a.UpgradesBought, b.UpgradesBought);
            Assert.AreEqual(a.RateEma, b.RateEma);
            Assert.AreEqual(a.Player.Pos.ToString(), b.Player.Pos.ToString());
        }

        [Test]
        public void Validator_DefeitosInjetados_Acusa()
        {
            Assert.IsEmpty(ContentValidator.NiveisRenome(RenomeDefs.Niveis).ToList());
            RenomeDef[] n = (RenomeDef[])RenomeDefs.Niveis.Clone();
            n[0] = new RenomeDef(0, 5);
            n[3] = new RenomeDef(3, 70, "hall_rua");                 // nao sobe do 2
            n[5] = new RenomeDef(5, 750);                            // nao libera nada
            n[6] = new RenomeDef(7, 1250, "Charter-6");              // fora de ordem e ID fora do formato
            n[8] = new RenomeDef(8, 3000, "wk_baltazar");            // ja liberado no 1
            var v = ContentValidator.Validate(renome: n);
            void Acusa(string trecho) => Assert.IsTrue(v.Exists(m => m.Contains(trecho)), $"esperava \"{trecho}\" em:\n{string.Join("\n", v)}");
            Acusa("renome: o nivel 0 comeca em 5");
            Acusa("renome: limiar do nivel 3 (70) nao sobe");
            Acusa("renome: nivel 5 nao libera nada");
            Acusa("renome: nivel 7 na posicao 6");
            Acusa("renome: nivel 6 libera \"Charter-6\"");
            Acusa("renome: \"wk_baltazar\" liberado por mais de um nivel");
            Assert.IsTrue(ContentValidator.NiveisRenome(RenomeDefs.Niveis.Take(10).ToArray()).Any(m => m.Contains("10 niveis na tabela")), "faltando o 10");
        }
    }
}
