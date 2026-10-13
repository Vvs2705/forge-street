using FS.Core;

namespace FS
{
    /// <summary>
    /// B-CORE-03 (GDD §18-§19): partes puras da qualidade na View, testadas no coretests (StationViewTests). O anel do golpe de mestre fecha
    /// sobre o alvo em voltas de Ciclo s; o toque acerta a ate Tuning.GolpeJanela/2 s do instante em que ele passa pelo alvo (0,8 s = metade
    /// da volta). O Sim so recebe acertou/errou (Sim.Strike). O desenho (anel, barra e brilho das pecas) fica na WorldView.
    /// </summary>
    public static class QualidadeView
    {
        /// <summary>s de uma volta do anel e a escala do anel de fora no comeco e no fim dela (1 = em cima do alvo).</summary>
        public const float Ciclo = 1.6f, De = 1.8f, Ate = 0.6f;

        /// <summary>Escala do anel de fora `t` s depois de o anel aparecer: fecha de De a Ate e recomeca.</summary>
        public static float Escala(float t) => De - (De - Ate) * (t % Ciclo) / Ciclo;

        /// <summary>Instante da volta (s) em que o anel de fora passa pelo alvo.</summary>
        public static float NoAlvo => Ciclo * (De - 1f) / (De - Ate);

        /// <summary>O toque `t` s depois de o anel aparecer acerta: a ate janela/2 do instante no alvo, em toda volta.</summary>
        public static bool Acerto(float t, float janela) => System.Math.Abs(t % Ciclo - NoAlvo) <= janela / 2f;

        /// <summary>Nivel da k-esima peca contada da ponta que sai primeiro (topo da pilha, frente do balcao): as Superiores, depois as Finas.
        /// `q` = lote (Sim.OutQ/StockQ).</summary>
        public static Quality Nivel(int k, int[] q) => k < q[(int)Quality.Superior] ? Quality.Superior : k < q[(int)Quality.Superior] + q[(int)Quality.Fine] ? Quality.Fine : Quality.Normal;
    }
}
