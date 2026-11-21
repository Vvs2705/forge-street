using System;
using System.Linq;
using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>Testes de outra mecanica (receita, qualidade, liga, altar, ajudantes...) que deixam a estacao produzir sozinha: a fabrica com
    /// operador em toda estacao que produz, aberta ou nao (a que o teste abre depois ja vem operada). E' o jogo de antes do operador
    /// obrigatorio para o que o teste mede; a regra do operador tem os testes dela (OperadorTests).</summary>
    public static class Fabrica
    {
        public static Sim ComOperadores(this Sim s)
        {
            foreach (Station st in s.Stations) if (st.Produces) st.Operador = 1;
            return s;
        }
    }

    /// <summary>
    /// Estacao precisa de operador (Vinicius, 2026-11-21; Sim.Operador.cs): sem operador a estacao so produz com o ferreiro na boca; com ele,
    /// produz sozinha; o pad e o toque cobram e gravam no save; save de antes ganha operador em toda estacao aberta; a flag desligada e' o jogo
    /// de antes. NUnit 3.5: tipos exatos (int com int, long com long).
    /// </summary>
    public class OperadorTests
    {
        const float Dt = 1f / 30f;

        /// <summary>Ferreiro longe de toda boca (o inicio do Old Forge, no meio da oficina).</summary>
        static Sim Longe(Tuning t = null)
        {
            var s = new Sim(t);
            s.Player.Pos = s.Distrito.Inicio;
            return s;
        }

        static void Roda(Sim s, float segundos) { for (float t = 0f; t < segundos; t += Dt) s.Tick(Dt, 0f, 0f); }

        [Test]
        public void SemOperador_EstacaoNaoProduzSozinha()
        {
            Sim s = Longe();
            Assert.IsTrue(s.PrecisaOperador(s.FurnaceA), "a Fornalha do kit comeca sem operador");
            Assert.IsFalse(s.FerreiroNaBoca(s.FurnaceA));
            s.FurnaceA.In = 4;
            Roda(s, 20f);
            Assert.AreEqual(0, s.FurnaceA.Crafted, "sem operador e sem o ferreiro, nada sai");
            Assert.AreEqual(4, s.FurnaceA.In, "o minerio fica na entrada");
            Assert.IsFalse(s.OperandoAgora(s.FurnaceA));
        }

        [Test]
        public void SemOperador_ProduzComOFerreiroNaBoca_EPausaQuandoEleSai()
        {
            Sim s = Longe();
            s.FurnaceA.In = 4;
            s.Player.Pos = s.FurnaceA.OutAt;
            Assert.IsTrue(s.FerreiroNaBoca(s.FurnaceA));
            Assert.IsTrue(s.OperandoAgora(s.FurnaceA));
            Roda(s, 2f);   // metade do ciclo de 4 s
            float meio = s.FurnaceA.Progress;
            Assert.IsTrue(s.FurnaceA.Busy);
            Assert.That(meio, Is.InRange(0.4f, 0.6f));
            s.Player.Pos = s.Distrito.Inicio;
            Roda(s, 10f);
            Assert.AreEqual(meio, s.FurnaceA.Progress, 1e-6f, "longe, o ciclo para onde estava");
            Assert.AreEqual(0, s.FurnaceA.Crafted);
            s.Player.Pos = s.FurnaceA.InAt;   // a boca de entrada tambem vale
            Roda(s, 2.2f);
            Assert.AreEqual(1, s.FurnaceA.Crafted, "de volta na boca, o lingote sai no ritmo de sempre");
        }

        [Test]
        public void ComOperador_ProduzSozinha()
        {
            Sim s = Longe();
            s.FurnaceA.Operador = 1;
            s.FurnaceA.In = 4;
            Assert.IsFalse(s.PrecisaOperador(s.FurnaceA));
            Roda(s, 8.5f);
            Assert.AreEqual(2, s.FurnaceA.Crafted, "4 s por lingote, como antes");
        }

        [Test]
        public void ComOperador_NiveisEOficialSeguemSomandoVelocidade()
        {
            Sim a = Longe(), b = Longe();
            a.FurnaceA.Operador = b.FurnaceA.Operador = 1;
            b.Buy(Upgrade.FurnaceSpeed1);   // Fole: 4 s -> 2,5 s
            a.FurnaceA.In = b.FurnaceA.In = 4;
            Roda(a, 8.5f); Roda(b, 8.5f);
            Assert.AreEqual((2, 3), (a.FurnaceA.Crafted, b.FurnaceA.Crafted), "o operador nao mexe no tempo: o Fole acelera como sempre");
        }

        [Test]
        public void Contratar_PeloToque_CobraEGravaNoSave()
        {
            Sim s = Longe();
            long custo = s.CustoOperador(s.FurnaceA);
            Assert.AreEqual(10L, custo, "FTUE: o operador da Fornalha custa 1 espada");
            s.Gold = custo - 1;
            Assert.IsFalse(s.ContratarOperador(s.FurnaceA), "sem ouro, recusa");
            Assert.AreEqual((custo - 1, 0), (s.Gold, s.FurnaceA.Operador));
            s.Gold = custo + 5;
            Assert.IsTrue(s.ContratarOperador(s.FurnaceA));
            Assert.AreEqual((5L, 1), (s.Gold, s.FurnaceA.Operador));
            Assert.IsTrue(s.Events.Exists(e => e.Kind == Ev.OperadorContratado && e.A == s.FurnaceA.Index));
            Assert.IsFalse(s.ContratarOperador(s.FurnaceA), "contratado nao contrata de novo");

            string save = s.Save(1);
            StringAssert.Contains("ops=fornalha:1\n", save);
            Sim l = Sim.Load(save);
            Assert.AreEqual((1, 0), (l.FurnaceA.Operador, l.AnvilA.Operador), "o save novo guarda quem tem e quem nao tem");
            Assert.AreEqual(save, l.Save(1), "estavel");
        }

        [Test]
        public void Contratar_PeloPad_DrenaOuroComoOPadDeUpgrade_EGuardaOParcial()
        {
            Sim s = Longe();
            Station f = s.FurnaceA;
            Assert.AreEqual(f, s.OperadorPadAt(f.OperadorPos), "o pad do operador aparece ao lado da estacao");
            Assert.IsNull(s.PadAt(f.OperadorPos), "fora de todo pad de upgrade");
            Assert.IsNull(s.StationAt(f.OperadorPos, out bool _), "e fora das bocas (forjar a mao nao paga sem querer)");
            s.Gold = 4;
            s.Player.Pos = f.OperadorPos;
            Roda(s, 2f);
            Assert.AreEqual((0L, 4L, 0), (s.Gold, f.OperadorPago, f.Operador), "paga o que tem e espera o resto");
            Sim l = Sim.Load(s.Save(1));
            Assert.AreEqual((4L, 0L), (l.FurnaceA.OperadorPago, l.Gold), "o parcial vai no save");
            l.Gold = 20; l.Player.Pos = f.OperadorPos;
            Roda(l, 2f);
            Assert.AreEqual((1, 14L, 0L), (l.FurnaceA.Operador, l.Gold, l.FurnaceA.OperadorPago), "completa os 10 e some");
            Assert.IsNull(l.OperadorPadAt(f.OperadorPos));
        }

        [Test]
        public void CustoPorEstacao_EmTodoDistrito()
        {
            foreach (DistrictDef d in Distritos.All)
            {
                var s = new Sim(distrito: d);
                foreach (Station st in s.Stations.Where(x => x.Produces))
                {
                    long c = s.CustoOperador(st);
                    Assert.GreaterOrEqual(c, d.OperadorKit, $"{d.Id}/{st.Id}");
                    Assert.AreEqual(0L, c % 5, $"{d.Id}/{st.Id}: multiplo de 5");
                    if (st.UnlockBy >= 0) Assert.Less(c, s.T.Cost(st.UnlockBy), $"{d.Id}/{st.Id}: o operador custa menos que a estacao");
                }
            }
            var of = new Sim();
            Assert.AreEqual((10L, 10L), (of.CustoOperador(of.FurnaceA), of.CustoOperador(of.AnvilA)), "FTUE: Fornalha e Bigorna baratas");
        }

        /// <summary>O save de antes do operador (sem ops=) e o do build publicado 0.5.1: toda estacao aberta ganha operador.</summary>
        [Test]
        public void SaveAntigo_TodaEstacaoAbertaGanhaOperador()
        {
            var a = new Sim();
            a.Buy(Upgrade.Anvil2); a.Buy(Upgrade.Shields);
            string atual = string.Join("\n", a.Save(1).Split('\n').Where(l => !l.StartsWith("ops=", StringComparison.Ordinal)));   // schema 3 de antes
            Sim m = Sim.Load(atual);
            foreach (Station st in m.Stations.Where(x => x.Produces))
                Assert.AreEqual(st.Unlocked ? 1 : 0, st.Operador, "schema 3 sem ops=: " + st.Id);
            Assert.AreEqual(0, m.UnknownIds);

            const string v051 = "v=1\nt=3600.008\ngold=9586\nup=11111111111111111111100111111\nut=67.03,137.1,179.98,240.36,327.74,408.89,437.55,500.13,600.61,700.15,813.62,960.22,1141.01,1350.03,1702.14,952.82,1578.1,1819.84,2375.57,2772.36,3266.94,-1,-1,1977.61,2104.31,843.72,864.08,888.27,914.53\npads=0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,\nst1=6,2,1,0.563,1611\nst2=6,4,0,0,1122\nst3=4,3,0,0,431\nst4=0,0,0,0,102\nst5=1,1,1,0.193,460\nst6=0,0,0,0,426\nst8=1,1,0,0,420\nstock=9,0,0,0\nsold=521,459,426,419\nms=2,2,2,2\nema=28.73473\nsaved=1000\nclaim=0\nm=22.7,1825,54901,0,7,3134,8,513.46,27\npx=7.55,10.15\nhold=0,0,0,0,0,0\nwk=0:4,0,0,0,0,0;1:0,0,0,0,0,0;2:0,0,0,0,0,0;3:0,0,0,0,0,0;0:0,0,0,0,0,0;3:0,0,0,0,0,0\nvip=52.351,11,-1,0,0,0\n";
            Sim p = Sim.Load(v051);
            Assert.AreEqual(9586L, p.Gold, "a migracao nao cobra nada");
            foreach (Station st in p.Stations.Where(x => x.Produces))
                Assert.AreEqual(st.Unlocked ? 1 : 0, st.Operador, "0.5.1: " + st.Id);
            Assert.AreEqual(7, p.Stations.Count(x => x.Produces && x.Operador == 1), "as 7 estacoes abertas na 0.5.1 aos 60 min");
            Assert.IsNull(p.OperadorPagavel(), "nada a contratar: a fabrica continua como estava");
            string novo = p.Save(2000);
            StringAssert.Contains("ops=fornalha:1,fornalha2:1,", novo);
            Assert.AreEqual(novo, Sim.Load(novo).Save(2000), "o save migrado e' estavel");

            Assert.AreEqual(0, Sim.Load("").FurnaceA.Operador, "jogo novo (sem save) comeca sem operador");
        }

        [Test]
        public void SaveNovo_EstacaoAbertaDepoisDaMigracaoPrecisaContratar()
        {
            var a = new Sim();
            string velho = string.Join("\n", a.Save(1).Split('\n').Where(l => !l.StartsWith("ops=", StringComparison.Ordinal)));
            Sim m = Sim.Load(velho);
            m.Buy(Upgrade.Anvil2);
            Assert.IsTrue(m.PrecisaOperador(m.AnvilB), "a estacao comprada depois da migracao segue a regra nova");
            Assert.AreEqual(1, Sim.Load(m.Save(1)).AnvilA.Operador);
            Assert.AreEqual(0, Sim.Load(m.Save(1)).AnvilB.Operador, "e o save novo nao a migra de novo");
        }

        [Test]
        public void FlagDesligada_ComportamentoDeAntes()
        {
            var t = new Tuning { OperadorObrigatorio = false };
            Sim s = Longe(t);
            Assert.IsFalse(s.PrecisaOperador(s.FurnaceA));
            Assert.IsTrue(s.OperandoAgora(s.FurnaceA));
            s.FurnaceA.In = 4;
            Roda(s, 8.5f);
            Assert.AreEqual(2, s.FurnaceA.Crafted, "produz sozinha, como antes");
            s.Gold = 100;
            Assert.IsFalse(s.ContratarOperador(s.FurnaceA), "nada a contratar");
            Assert.IsNull(s.OperadorPadAt(s.FurnaceA.OperadorPos), "o pad some");
            Assert.IsNull(s.OperadorPagavel());

            Sim plain = new Sim(new Tuning { OperadorObrigatorio = false }), off = new Sim(new Tuning { OperadorObrigatorio = false });
            BalanceTests.Play(plain, 300f); BalanceTests.Play(off, 300f);
            Assert.AreEqual(plain.Save(1), off.Save(1), "deterministico com a flag desligada");
            Assert.AreEqual(0, off.Stations.Count(x => x.Operador > 0), "o bot nao contrata ninguem com a flag desligada");
        }

        [Test]
        public void RemoteConfig_DesligaAFlag()
        {
            var t = new Tuning();
            Assert.IsTrue(t.OperadorObrigatorio, "ligada por padrao");
            RemoteConfig.Resultado r = RemoteConfig.Aplicar(t, "{\"id\":\"rollback\",\"versao\":1,\"OperadorObrigatorio\":false}", false);
            Assert.IsTrue(r.Valido);
            Assert.AreEqual(1, r.Aplicadas);
            Assert.IsFalse(t.OperadorObrigatorio);
        }

        [Test]
        public void Textos_NosTresIdiomas_CabemNaPilula()
        {
            Idioma antes = Loc.Atual;
            try
            {
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    foreach (string k in new[] { "operador.placa", "operador.parada", "operador.mao", "operador.contratado" })
                    {
                        Assert.IsTrue(Loc.Tem(k), k);
                        Assert.LessOrEqual(Loc.T(k).Length, Textos.DicaMax, $"{i}: {Loc.T(k)}");
                    }
                    Assert.LessOrEqual(Loc.T("operador.placa.preco", "1,2 mi").Length, Textos.DicaMax, i.ToString());
                }
                Loc.Atual = Idioma.Pt;
                Assert.AreEqual("Contratar operador", Loc.T("operador.placa"));
            }
            finally { Loc.Atual = antes; }
        }

        [Test]
        public void Bot_ForjaAMaoAPrimeiraEspada_EContrataLogoDepois()
        {
            var s = new Sim();
            var bot = new Bot();
            float op1 = -1f;
            for (float t = 0f; t < 180f; t += Dt)
            {
                bot.Step(s, Dt);
                if (op1 < 0f && s.Stations.Exists(x => x.Operador > 0)) op1 = s.Time;
            }
            Assert.That(s.FirstSaleTime, Is.InRange(1f, 60f), "a 1a espada, forjada a mao, sai em < 60 s");
            Assert.That(op1, Is.InRange(s.FirstSaleTime, 60f), "o 1o operador logo depois da 1a venda");
            Assert.AreEqual((1, 1), (s.FurnaceA.Operador, s.AnvilA.Operador), "Fornalha e Bigorna com operador em 3 min");
        }

        [Test]
        public void Bot_ContrataTodosOsOperadoresDoOldForge_SemTravar()
        {
            var s = new Sim();
            BalanceTests.Play(s, 3600f);
            foreach (Station st in s.Stations.Where(x => x.Produces && x.Unlocked))
                Assert.AreEqual(1, st.Operador, "aberta e sem operador aos 60 min: " + st.Id);
            Assert.IsTrue(s.ProductionComplete, "producao completa em 60 min");
        }
    }
}
