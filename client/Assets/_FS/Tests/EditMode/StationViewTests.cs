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
    }
}
