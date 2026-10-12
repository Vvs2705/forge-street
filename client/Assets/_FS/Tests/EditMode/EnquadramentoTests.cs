using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>A-ART-03 (P12): camera a 9 m com o ferreiro >= 9% da largura e a fila legivel em 9:16, 9:20 (POCO 1080x2400) e 10:16.</summary>
    public class EnquadramentoTests
    {
        // faixas sem recorte de area segura: Game.HudBand, MenuBar.Band e MenuBar.Band + MenuBar.Tray
        const float Hud = 0.075f, Barra = 0.085f, Bandeja = 0.285f, Eps = 0.01f;
        // ferreiro no Idle S, medido na folha (Resources/Sprites/ferreiro/Idle.png): 74,5 px / (109,87 ppu x CharPpuMul 0,76)
        const float FerreiroAltura = 0.89f;
        const float RotuloFornalha = 4.4f;   // nome e barra embaixo da fileira das fornalhas (y 5,5)
        static readonly float[] Aspectos = { 540f / 960f, 432f / 960f, 600f / 960f };

        /// <summary>Mundo visivel entre a barra (ou a bandeja) e a faixa da HUD, com a camera no alvo.</summary>
        static V2 Area(V2 ferreiro, float aspecto, bool bandeja, out float esq, out float dir, float largura = Enquadramento.Largura)
        {
            V2 c = Enquadramento.Alvo(ferreiro, aspecto, largura, Balance.WorkshopW, Hud, Barra, bandeja ? Bandeja : 0f);
            float meiaA = largura / 2f / aspecto, alt = 2f * meiaA;
            esq = c.X - largura / 2f; dir = c.X + largura / 2f;
            return new V2(c.Y - meiaA + alt * (bandeja ? Bandeja : Barra), c.Y + meiaA - alt * Hud);   // X = base, Y = topo
        }

        static Sim Balcao8()
        {
            var s = new Sim();
            foreach (Upgrade u in new[] { Upgrade.CounterCapacity, Upgrade.Counter5, Upgrade.Counter6, Upgrade.Counter7, Upgrade.Counter8 }) s.Buy(u);
            Assert.AreEqual(Balance.QueueCapMax, s.QueueCap);
            return s;
        }

        [Test]
        public void Largura_Ferreiro9PorCento_Fila8CabeNaHorizontal()
        {
            Assert.GreaterOrEqual(FerreiroAltura / Enquadramento.Largura, 0.09f);
            Sim s = Balcao8();
            float fim = s.ClientSlot(Balance.QueueCapMax - 1).X + Balance.CharRadius, inicio = s.ClientSlot(0).X - Balance.CharRadius;
            foreach (float a in Aspectos)
                foreach (V2 p in new[] { s.FurnaceA.InAt, s.ToolBench.OutAt, s.Counter.InAt })   // ponta esquerda, ponta direita e meio
                {
                    Area(p, a, false, out float esq, out float dir);
                    Assert.LessOrEqual(esq, inicio, $"aspecto {a}, ferreiro {p.X}");
                    Assert.GreaterOrEqual(dir, fim, $"aspecto {a}, ferreiro {p.X}");
                }
        }

        [Test]
        public void DaFornalhaParaCima_FilaEBalaoInteiros()
        {
            Sim s = Balcao8();
            foreach (float a in Aspectos)
                foreach (V2 p in new[] { s.FurnaceA.InAt, s.AnvilA.InAt, s.Counter.InAt })
                {
                    V2 v = Area(p, a, false, out _, out _);
                    Assert.GreaterOrEqual(v.Y, Enquadramento.Topo - Eps, $"fila sob a HUD: aspecto {a}, ferreiro y {p.Y}");
                    Assert.GreaterOrEqual(p.Y - v.X, Enquadramento.Folga - Eps, $"pe do ferreiro: aspecto {a}, y {p.Y}");
                }
        }

        [Test]
        public void NoDeposito_FerreiroComFolga_E920CabeTudo()
        {
            Sim s = Balcao8();
            foreach (float a in Aspectos)
            {
                V2 v = Area(s.Deposit.InAt, a, false, out _, out _);
                Assert.GreaterOrEqual(s.Deposit.InAt.Y - v.X, Enquadramento.Folga - Eps, $"aspecto {a}");
                // 9:16 e 10:16 nao cabem Deposito e fila juntos a 9 m: o balao fica preso abaixo da HUD (WorldView.OnScreen)
                if (a < 0.5f) Assert.GreaterOrEqual(v.Y, Enquadramento.Topo - Eps, "9:20 (POCO) mostra a fila inteira");
            }
        }

        [Test]
        public void BandejaAberta_SoCobreAFileiraDoDeposito_SemEsconderOFerreiro()
        {
            Sim s = Balcao8();
            foreach (float a in Aspectos)
            {
                foreach (V2 p in new[] { s.Deposit.InAt, s.FurnaceA.InAt, s.AnvilA.InAt })
                    Assert.LessOrEqual(Area(p, a, true, out _, out _).X, Enquadramento.PisoBandeja + Eps, $"aspecto {a}, ferreiro y {p.Y}");
                // no balcao a cabeca vence o piso; mesmo assim o rotulo da Fornalha fica acima da bandeja
                V2 v = Area(s.Counter.InAt, a, true, out _, out _);
                Assert.LessOrEqual(s.Counter.InAt.Y + Enquadramento.Cabeca, v.Y + Eps, $"cabeca sob a HUD: aspecto {a}");
                Assert.LessOrEqual(v.X, RotuloFornalha, $"aspecto {a}");
            }
        }

        [Test]
        public void Cam114_No920_CentradaComoAntes()
        {
            float a = 432f / 960f;
            V2 baixo = Enquadramento.Alvo(new V2(1.5f, 2.15f), a, Enquadramento.OficinaInteira, Balance.WorkshopW, Hud, Barra);
            V2 alto = Enquadramento.Alvo(new V2(4.5f, 12.35f), a, Enquadramento.OficinaInteira, Balance.WorkshopW, Hud, Barra);
            Assert.AreEqual(Balance.WorkshopW / 2f, baixo.X, 1e-4f);
            Assert.AreEqual(baixo.Y, alto.Y, 1e-4f);   // a tela comprida tem altura sobrando: a camera nao se mexe
        }
    }
}
