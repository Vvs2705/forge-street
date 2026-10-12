using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>A-ART-06: as 6 decisoes da fornalha (StationView.Estado) com o Sim real, e a rampa de 1 s do calor.</summary>
    public class StationViewTests
    {
        [Test]
        public void Fornalha_SeisEstados()
        {
            var sim = new Sim();
            Station f = sim.FurnaceA;
            Assert.AreEqual(Fornalha.Fria, StationView.Estado(f, false, 0f), "nova, nunca acesa");

            f.In = 1;   // 1 minerio: volta a trabalhar depois de parada
            sim.Tick(0.1f, 0f, 0f);
            Assert.IsTrue(f.Busy);
            float calor = StationView.Calor(0f, true, 0.5f);
            Assert.AreEqual(Fornalha.Aquecendo, StationView.Estado(f, false, calor), "meio segundo da rampa");
            calor = StationView.Calor(calor, true, 0.5f);
            Assert.AreEqual(Fornalha.Ativa, StationView.Estado(f, false, calor), "rampa de 1 s completa");
            Assert.AreEqual(Fornalha.Overdrive, StationView.Estado(f, true, 0f), "boost ativo trabalhando, mesmo aquecendo");

            sim.StartBoost();
            for (int i = 0; i < 100 && f.Busy; i++) sim.Tick(0.1f, 0f, 0f);
            Assert.AreEqual(1, f.Out);
            Assert.AreEqual(Fornalha.SemMinerio, StationView.Estado(f, sim.BoostLeft > 0f, 1f), "ja fez lingote e o minerio acabou: fome, nao fria");

            f.In = f.InCap;
            for (int i = 0; i < 600 && f.Out < f.OutCap; i++) sim.Tick(0.1f, 0f, 0f);
            Assert.IsTrue(f.Blocked && !f.Busy && f.In > 0);
            Assert.AreEqual(Fornalha.SaidaCheia, StationView.Estado(f, true, 1f), "travada vence o boost e o minerio na grelha");
            f.Busy = true; f.Progress = 1f;   // pronta esperando vaga (revisao da v0.6): trava, nao trabalho
            Assert.AreEqual(Fornalha.SaidaCheia, StationView.Estado(f, false, 1f));

            Assert.AreEqual(0f, StationView.Calor(1f, false, StationView.EsfriaS), "esfria em EsfriaS parada");
        }

        /// <summary>A-ART-07: a peca da bigorna se divide em batidas iguais e a ultima e' o Ev.Crafted, um compasso depois da penultima.</summary>
        [Test]
        public void Bigorna_CompassoFechaNoCrafted()
        {
            Assert.AreEqual(11, StationView.Batidas(Balance.HammerTime0), "espada 5 s: 0,45 s");
            Assert.AreEqual(7, StationView.Batidas(Balance.HammerTime1), "Martelo veloz 3 s: 0,43 s");
            Assert.AreEqual(2, StationView.Batidas(0.1f));
            Assert.IsFalse(StationView.Batida(0.95f, 1f, 11), "pronta esperando vaga nao bate");
            Assert.IsFalse(StationView.Batida(0.95f, 0.02f, 11), "peca nova nao bate");

            var sim = new Sim();
            Station a = sim.AnvilA;
            a.In = a.InCap;
            int n = StationView.Batidas(a.Time), batidas = 0, pecas = 0;
            float antes = 0f, ultima = -1f, ritmo = a.Time / n;
            for (int i = 0; i < 400 && pecas < 2; i++)   // 0,05 s por tick, 2 espadas em ~10 s
            {
                sim.Tick(0.05f, 0f, 0f);
                float agora = a.Busy ? a.Progress : 0f;
                if (StationView.Batida(antes, agora, n)) { batidas++; ultima = sim.Time; }
                antes = agora;
                foreach (SimEvent e in sim.Events)
                {
                    if (e.Kind != Ev.Crafted || e.B != a.Index) continue;
                    pecas++;
                    Assert.AreEqual(ritmo, sim.Time - ultima, 0.06f, "golpe final um compasso depois da ultima batida");
                    Assert.AreEqual(pecas * (n - 1), batidas, "n-1 batidas por peca");
                }
            }
            Assert.AreEqual(2, pecas);
        }
    }
}
