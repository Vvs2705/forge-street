using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// Obras do Reino e Decretos semanais (Conselho do TIME DE JOGOS, 2026-11-21; BALANCE.md §53): o ralo de ouro sem fim que abre no Charter
    /// do Old Forge (custo em minutos da renda alvo do distrito mais avancado, crescendo por degrau; Renome e bonus de fundo com teto, nunca
    /// ouro), e os 3 decretos por semana desde o LiveOpsDefs.T0 (Renome e XP do Pass, 1x). NUnit 3.5: tipos exatos, nada de tupla int x long.
    /// </summary>
    public class ObrasTests
    {
        const string OF = Distritos.OldForgeId;
        static readonly DistrictDef Row = Distritos.ArmorersRow;
        const long Fuso = -3 * 3600;

        static Imperio Chegada() => Imperio.Chegada(Row);

        // ------------------------------------------------------------------ Obras

        [Test]
        public void Obras_FechadasAteOCharterDoOldForge()
        {
            var imp = new Imperio(new Sim());
            imp.Raiz.Gold = 10000000;
            Assert.IsFalse(imp.Obras.Aberta);
            Assert.IsFalse(imp.Obras.Construir(0), "antes do Charter nada se constroi");
            Assert.AreEqual(10000000L, imp.Raiz.Gold);
            imp.Forca(OF);
            Assert.IsTrue(imp.Obras.Aberta);
            Assert.IsTrue(imp.Obras.Construir(0));
        }

        /// <summary>Custo = (15 + 5n) min da renda alvo do distrito mais avancado aberto (2.400/min x 3^indice): trocar de foco, ficar parado
        /// ou a RateEma do ativo nao mudam o preco.</summary>
        [Test]
        public void Custo_MinutosDaRendaAlvoDoMaisAvancado_CresceComODegrau()
        {
            Imperio imp = Chegada();
            Assert.AreEqual(7200.0, imp.Obras.RendaRef, 1e-9, "a Row aberta: 2.400 x 3");
            Assert.AreEqual(108000L, imp.Obras.Custo(0));
            Assert.AreEqual(144000L, imp.Obras.Custo(1));
            imp.Ativo.RateEma = 1.0;
            Assert.IsTrue(imp.Trocar(OF));
            Assert.AreEqual(108000L, imp.Obras.Custo(0), "foco no Old Forge, mesmo preco");
            for (int n = 0; n < 2000; n++) Assert.Greater(imp.Obras.Custo(n + 1), imp.Obras.Custo(n), "degrau " + n);
        }

        [Test]
        public void Construir_SoOProximo_1x_PagaRenomeNuncaOuro()
        {
            Imperio imp = Chegada();
            Sim a = imp.Ativo;
            long r0 = imp.Raiz.Renome.Total;
            a.Gold = imp.Obras.Custo(0) + imp.Obras.Custo(1) + imp.Obras.Custo(2) + 7;
            Assert.IsFalse(imp.Obras.Construir(1), "so o proximo");
            Assert.IsTrue(imp.Obras.Construir(0));
            Assert.IsFalse(imp.Obras.Construir(0), "o mesmo degrau 2x nao paga 2x");
            Assert.IsTrue(imp.Obras.Construir(1));
            Assert.IsTrue(imp.Obras.Construir(2));
            Assert.AreEqual(7L, a.Gold, "gasta exatamente o custo; nenhum ouro volta");
            Assert.AreEqual(3, imp.Obras.Degraus);
            Assert.AreEqual(r0 + 10 + 10 + 40, imp.Raiz.Renome.Total, "10 por etapa e +30 ao fechar a obra");
            Assert.IsFalse(imp.Obras.Construir(3), "sem ouro");
            Assert.AreEqual(3, imp.Obras.Degraus);
        }

        /// <summary>Sempre ha um proximo degrau: rodizio das 10 obras x 3 etapas, nivel que sobe a cada volta, deterministico.</summary>
        [Test]
        public void SempreHaProximo_RodizioDeterministico()
        {
            Assert.AreEqual(10, Obras.Defs.Length);
            Assert.AreEqual(Obras.Defs.Length, Obras.Defs.Select(d => d.Id).Distinct().Count());
            Assert.AreEqual("obra_ponte", Obras.ObraDe(0).Id);
            Assert.AreEqual((1, 1), (Obras.EtapaDe(0), Obras.NivelDe(0)));
            Assert.AreEqual((3, 1), (Obras.EtapaDe(29), Obras.NivelDe(29)));
            Assert.AreEqual("obra_ponte", Obras.ObraDe(30).Id);
            Assert.AreEqual((1, 2), (Obras.EtapaDe(30), Obras.NivelDe(30)));
            Assert.AreEqual(Obras.ObraDe(1234567).Id, Obras.ObraDe(1234567).Id);
            Assert.AreEqual(Obras.Defs[1234567 / 3 % 10].Id, Obras.ObraDe(1234567).Id);
        }

        /// <summary>Prestigio do Reino: +floor(2 raiz(n))% no fundo, curva decrescente, teto 20% (degrau 100); o fundo segue abaixo da taxa.</summary>
        [Test]
        public void Bonus_CurvaDecrescente_Teto20_FundoAbaixoDaTaxa()
        {
            Assert.AreEqual(new[] { 0, 2, 2, 3, 4 }, Enumerable.Range(0, 5).Select(Obras.Bonus).ToArray());
            Assert.AreEqual(20, Obras.Bonus(100));
            Assert.AreEqual(20, Obras.Bonus(1000000));
            for (int n = 1; n < 400; n++)
            {
                Assert.GreaterOrEqual(Obras.Bonus(n), Obras.Bonus(n - 1), "nunca cai");
                Assert.LessOrEqual(Obras.Bonus(n + 1) - Obras.Bonus(n), Obras.Bonus(n) - Obras.Bonus(n - 1) + 1, "passo de no maximo +1");
            }
            Imperio imp = Chegada();
            double antes = imp.FundoFator(OF);
            imp.Ativo.Gold = long.MaxValue / 4;
            for (int n = 0; n < 4; n++) Assert.IsTrue(imp.Obras.Construir(n));
            Assert.AreEqual(antes * 1.04, imp.FundoFator(OF), 1e-12, "4 degraus = +4%");
            for (int n = 2; n <= 5; n++) { imp.Ativo.Gold += imp.CustoManager(OF); Assert.IsTrue(imp.SobeManager(OF, n)); }
            while (imp.Obras.BonusPct < Obras.BonusTeto) { imp.Ativo.Gold = long.MaxValue / 4; Assert.IsTrue(imp.Obras.Construir(imp.Obras.Degraus)); }
            Assert.AreEqual(0.25 * 1.6 * 1.2, imp.FundoFator(OF), 1e-12);
            Assert.Less(imp.FundoFator(OF), 1.0);
        }

        [Test]
        public void Save_LinhaObras_SaveAntigoAbreIgual()
        {
            Imperio imp = Chegada();
            string antigo = imp.Save(1000);
            StringAssert.DoesNotContain("obras=", antigo, "sem degrau: o save de antes");
            Assert.AreEqual(0, Imperio.Load(antigo).Obras.Degraus);
            imp.Ativo.Gold = imp.Obras.Custo(0) + imp.Obras.Custo(1);
            Assert.IsTrue(imp.Obras.Construir(0)); Assert.IsTrue(imp.Obras.Construir(1));
            long ren = imp.Raiz.Renome.Total;
            string txt = imp.Save(1000);
            StringAssert.Contains("obras=2\n", txt);
            Imperio b = Imperio.Load(txt);
            Assert.AreEqual(2, b.Obras.Degraus);
            Assert.AreEqual(ren, b.Raiz.Renome.Total, "o Renome das obras nao paga de novo");
            Assert.AreEqual(imp.Obras.Custo(2), b.Obras.Proximo);
            Assert.AreEqual(0, Imperio.Load(txt.Replace("obras=2", "obras=lixo")).Obras.Degraus, "lixo = 0");
        }

        /// <summary>O bot so manda ouro para as Obras com nada mais a venda e guardando o manager mais barato que falta e a abertura seguinte.</summary>
        [Test]
        public void Bot_ObrasSoComNadaAComprar_GuardaAReserva()
        {
            Imperio imp = Chegada();
            Sim a = imp.Ativo;
            a.Gold = long.MaxValue / 4;
            Assert.IsFalse(Bot.NadaAComprar(a), "a Row acabou de abrir");
            Assert.IsFalse(Bot.Obras(imp));
            Assert.AreEqual(0, imp.Obras.Degraus);
            Assert.IsTrue(imp.Trocar(OF));   // o Old Forge da Chegada: producao completa; o resto (luxo, menu, equipe) vai a mao
            Sim of = imp.Ativo;
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (BlueprintDef d in of.Projetos.Defs) of.Projetos.Ganha(d.Id, d.Sources[0].Kind);
                for (int u = 0; u < of.UpgradeDefs.Length; u++) of.Buy((Upgrade)u);
                of.Renome.Ganha(100000);
                for (int k = 0; k < of.Equipe.Defs.Length; k++) while (of.Equipe.AVenda(k) && of.Equipe.Comprar(k)) { }
            }
            Assume.That(Bot.NadaAComprar(of), "Old Forge sem nada a venda");
            long reserva = Bot.Reserva(imp);
            Assert.AreEqual(-1L, imp.CustoManager(Row.Id), "a Row sem Charter nao tem manager");
            Assert.AreEqual(imp.CustoManager(OF), reserva, "o manager mais barato que falta; o proximo do Old Forge (a Row) ja esta aberto");
            of.Gold = reserva + imp.Obras.Proximo - 1;
            Assert.IsFalse(Bot.Obras(imp), "a reserva fica");
            of.Gold += 1;
            Assert.IsTrue(Bot.Obras(imp));
            Assert.AreEqual(reserva, of.Gold);
        }

        /// <summary>Todo nome de obra e de decreto nos 3 idiomas, e as linhas do painel montam (Textos puro).</summary>
        [Test]
        public void Textos_ObrasEDecretos_NosTresIdiomas()
        {
            Idioma antes = Loc.Atual;
            try
            {
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    foreach (ObraDef d in Obras.Defs) Assert.IsTrue(Loc.Tem("obra." + d.Id), d.Id);
                    foreach (DecretoDef d in Decretos.DeObras.Concat(Decretos.Pool)) Assert.IsTrue(Loc.Tem("dcr." + d.Id), d.Id);
                    StringAssert.DoesNotContain("[", Textos.ObraNome(31));
                    StringAssert.EndsWith(" II · " + (i == Idioma.En ? "stage" : "etapa") + " 2/3", Textos.ObraNome(31));
                    var v = new Decretos.Slot { Def = Decretos.Pool[3], Ref = "espada", Alvo = 400 };
                    StringAssert.Contains("400", Textos.Decreto(v));
                    StringAssert.DoesNotContain("[", Textos.Decreto(v));
                }
            }
            finally { Loc.Atual = antes; }
            Assert.AreEqual("XIV", Textos.Romano(14));
        }

        // ------------------------------------------------------------------ Decretos

        [Test]
        public void Decretos_TresPorSemana_Distintos_UmDeObras_Deterministicos()
        {
            for (long s = -20; s < 600; s++)
            {
                DecretoDef[] d = Decretos.DaSemana(s);
                Assert.AreEqual(3, d.Length);
                Assert.AreEqual(3, d.Select(x => x.Id).Distinct().Count(), "semana " + s);
                Assert.AreEqual(ContaDecreto.Obras, d[0].Conta);
                Assert.AreNotEqual(ContaDecreto.Obras, d[1].Conta);
                Assert.AreEqual(d.Select(x => x.Id).ToArray(), Decretos.DaSemana(s).Select(x => x.Id).ToArray());
            }
            var vistos = new HashSet<string>();
            for (long s = 0; s < 5; s++) foreach (DecretoDef d in Decretos.DaSemana(s)) vistos.Add(d.Id);
            Assert.AreEqual(Decretos.DeObras.Length + Decretos.Pool.Length, vistos.Count, "o rodizio passa por todos em 5 semanas");
            Assert.AreEqual(0L, Decretos.SemanaDe(LiveOpsDefs.T0));
            Assert.AreEqual(-1L, Decretos.SemanaDe(LiveOpsDefs.T0 - 1));
            Assert.AreEqual(1L, Decretos.SemanaDe(LiveOpsDefs.T0 + 7 * LiveOpsDefs.Dia));
            foreach (DecretoDef d in Decretos.DeObras.Concat(Decretos.Pool)) Assert.That(d.Renome > 0 && d.Xp > 0, d.Id + ": Renome e XP, nunca ouro");
        }

        static void Agora(Imperio imp, long t)
        {
            imp.Raiz.Metas.Agora(t, Fuso);
            imp.Raiz.LiveOps.Agora(t);
            imp.Fundo(0f);
        }

        [Test]
        public void Decretos_SemanaDoT0_ObraConcluida_PagaRenomeEXp1x_SaveLoad()
        {
            Imperio imp = Chegada();
            imp.Raiz.Buy(Upgrade.Tools);
            imp.Fundo(0f);
            Assert.IsFalse(imp.Decretos.Aberta, "sem relogio nada roda");
            long t = LiveOpsDefs.T0 + 7 * LiveOpsDefs.Dia * 10 + 3600;   // semana 10: Obras[0] = 1 etapa
            Agora(imp, t);
            Assert.AreEqual(10L, imp.Decretos.Semana);
            Decretos.Slot v = imp.Decretos.Slots[0];
            Assert.AreEqual("dcr_obra1", v.Def.Id);
            Assert.AreEqual(0L, imp.Decretos.Feito(v));
            long ren = imp.Raiz.Renome.Total, xp = imp.Raiz.Metas.PassXp;
            imp.Ativo.Gold = imp.Obras.Proximo;
            Assert.IsTrue(imp.Obras.Construir(0));
            Agora(imp, t + 60);
            Assert.IsTrue(v.Pago);
            Assert.AreEqual(ren + Obras.RenomeDe(0) + v.Def.Renome, imp.Raiz.Renome.Total);
            Assert.AreEqual(xp + v.Def.Xp, imp.Raiz.Metas.PassXp);
            Assert.AreEqual(1, imp.Decretos.Novidades.Count);
            Agora(imp, t + 120);
            Assert.AreEqual(xp + v.Def.Xp, imp.Raiz.Metas.PassXp, "1x");

            string txt = imp.Save(t + 120);
            StringAssert.Contains("dcr=10;dcr_obra1:-:0:1:1;", txt);
            Imperio b = Imperio.Load(txt);
            Agora(b, t + 180);
            Assert.AreEqual(xp + v.Def.Xp, b.Raiz.Metas.PassXp, "depois do load nao paga de novo");
            Assert.IsTrue(b.Decretos.Slots[0].Pago);
            Assert.AreEqual(v.Def.Id, b.Decretos.Slots[0].Def.Id);

            Agora(b, t + 7 * LiveOpsDefs.Dia);
            Assert.AreEqual(11L, b.Decretos.Semana);
            Assert.AreEqual("dcr_obra2", b.Decretos.Slots[0].Def.Id);
            Assert.IsFalse(b.Decretos.Slots[0].Pago);
            Assert.AreEqual(1L, b.Decretos.Slots[0].Base, "a base e' o contador de agora");
            Assert.IsFalse(b.Raiz.Metas.Ledger.Any(x => x.StartsWith("dc:10:", StringComparison.Ordinal)), "a semana fechada sai do ledger");
            Agora(b, t);   // relogio para tras
            Assert.AreEqual(11L, b.Decretos.Semana, "nada reabre");
        }

        /// <summary>Vendas do distrito ativo contam no decreto da raiz (C-CORE-03); o de renda congela o alvo pela renda alvo.</summary>
        [Test]
        public void Decretos_VendaDoDistritoAtivo_E_RendaCongelada()
        {
            Imperio imp = Chegada();
            imp.Raiz.Buy(Upgrade.Tools);
            long t = LiveOpsDefs.T0 + 7 * LiveOpsDefs.Dia * 2 + 3600;   // semana 2: Pool[4] (renda) e Pool[0] (vendas)
            Agora(imp, t);
            DecretoDef[] d = Decretos.DaSemana(2);
            Assert.AreEqual(new[] { "dcr_renda2h", "dcr_vende3000" }, new[] { d[1].Id, d[2].Id });
            Decretos.Slot renda = imp.Decretos.Slots[1], vende = imp.Decretos.Slots[2];
            Assert.AreEqual(7200L * 120, renda.Alvo, "120 min da renda alvo da Row (7.200/min)");
            for (int k = 0; k < 3000; k++) imp.Ativo.Metas.On(Ev.Sold, 0, 1);   // so o distrito: nao conta
            Assert.AreEqual(0L, imp.Decretos.Feito(vende));
            imp.Raiz.Metas.On(Ev.Sold, 0, 288);   // o Sim.Emit do distrito sobe isto na raiz
            Assert.AreEqual(1L, imp.Decretos.Feito(vende));
            Assert.AreEqual(288L, imp.Decretos.Feito(renda));
        }
    }
}
