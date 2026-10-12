using System.Collections.Generic;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>B-ART-08: a esteira viva (EsteiraView) segue o Sim. A faixa anda na velocidade do lingote (o boost acelera os dois juntos),
    /// o "clac" segue a distancia, o lingote cruza em 1 ciclo a partir da entrega e tudo para sem lingote em cima.</summary>
    public class EsteiraViewTests
    {
        [Test]
        public void Faixa_VelocidadeSegueORitmoEParaSemCarga()
        {
            Assert.AreEqual(2.8f, EsteiraView.Velocidade(2.8f, 1f, 1f, 1), 1e-5f, "1x: cruza 2,8 m em 1 s (ConveyorTime)");
            Assert.AreEqual(8.4f, EsteiraView.Velocidade(2.8f, 1f, 3f, 1), 1e-5f, "3x: o boost acelera a faixa como acelera o Sim");
            Assert.AreEqual(1.4f, EsteiraView.Velocidade(2.8f, 2f, 1f, 2), 1e-5f, "ciclo maior = faixa mais lenta; 2 lingotes nao somam");
            Assert.AreEqual(0f, EsteiraView.Velocidade(2.8f, 1f, 3f, 0), "sem lingote em cima: parada, mesmo no boost");

            float fase = 0f; int clacs = 0;
            for (int i = 0; i < 31; i++) { fase = EsteiraView.Avanca(fase, 2.8f / 30f, 0.7f, out bool c); if (c) clacs++; }
            Assert.AreEqual(4, clacs, "1 clac por divisa que passa no tambor: 4 numa travessia de 2,8 m, em qualquer velocidade");
            Assert.AreEqual(0.35f, EsteiraView.Avanca(0.35f, 0f, 0.7f, out bool parado), 1e-6f);
            Assert.IsFalse(parado, "parada nao faz clac");

            var cheia = new[] { 0.5f, 0.9f, 0.2f };
            EsteiraView.Entra(cheia);
            Assert.AreEqual(0f, cheia[1], "sem vaga: o mais adiantado chega e o novo entra no lugar dele");
        }

        [Test]
        public void Esteira_LingoteCruzaEmUmCicloDoSimEParaSemCarga()
        {
            foreach (int anuncios in new[] { 0, 2 })   // 1x e 3x
            {
                var sim = new Sim();
                sim.Buy(Upgrade.Conveyor);   // 1a melhoria: o boost ja vale (D6)
                for (int i = 0; i < anuncios; i++) sim.StartBoost();
                Assert.AreEqual(1f + anuncios, sim.BoostMul);
                sim.FurnaceA.Out = 3;   // 3 lingotes prontos, Bigorna vazia, Fornalha sem minerio: a esteira leva os 3 e para
                var p = new float[EsteiraView.Max];
                for (int i = 0; i < p.Length; i++) p[i] = -1f;
                const float dt = 1f / 30f;   // passo do Game (MaxStep) e do -record a 30 fps: 1 tick por quadro
                float ciclo = sim.T.ConveyorTime, viagem = ciclo / sim.BoostMul;
                var saiu = new List<float>(); var chegou = new List<float>();
                int levando = 0;
                for (int k = 0; k < 30 * 6; k++)
                {
                    sim.Tick(dt, 0f, 0f);
                    foreach (SimEvent e in sim.Events) if (e.Kind == Ev.Deposited && e.B == 9) { EsteiraView.Entra(p); saiu.Add(sim.Time); }
                    int antes = 0;
                    foreach (float x in p) if (x >= 0f) antes++;
                    levando = EsteiraView.Anda(p, dt * sim.BoostMul / ciclo);
                    for (int i = levando; i < antes; i++) chegou.Add(sim.Time);
                    Assert.LessOrEqual(levando, 2, $"{anuncios}: 1 entrega por ciclo, 1 ciclo de viagem");
                    Assert.LessOrEqual(sim.AnvilA.In - levando, chegou.Count, $"{anuncios}: a pilha da Bigorna (In - Levando) nao mostra lingote que ainda nao chegou");
                }
                Assert.AreEqual(3, saiu.Count, $"{anuncios}: o Sim levou os 3");
                Assert.AreEqual(3, chegou.Count, $"{anuncios}: os 3 chegaram na faixa");
                for (int i = 0; i < 3; i++) Assert.AreEqual(viagem, chegou[i] - saiu[i], dt * 1.01f, $"{anuncios}: lingote {i} cruza em 1 ciclo / boost");
                Assert.AreEqual(0, levando, $"{anuncios}: sem lingote na Fornalha a faixa esvazia");
                Assert.AreEqual(0f, EsteiraView.Velocidade(2.8f, ciclo, sim.BoostMul, levando), $"{anuncios}: e para");
            }
        }
    }
}
