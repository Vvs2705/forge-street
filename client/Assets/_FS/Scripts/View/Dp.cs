using UnityEngine;

namespace FS
{
    /// <summary>
    /// dp -> px (copiado do ARKANA, Assets/_Arkana/Scripts/UI/Dp.cs, sem a dependencia do Balance de la).
    /// Numero que o DEDO sente vive em dp: fisico = dp * dpi/160. O Canvas do joystick usa escala 1 (px de tela = px
    /// de canvas), entao nao ha' segunda conversao.
    /// </summary>
    public static class Dp
    {
        public const float DpiPadrao = 160f;

        /// <summary>Override para teste/preview (0 = usa Screen.dpi).</summary>
        public static float DpiForcado = 0f;

        public static float Dpi
        {
            get
            {
                if (DpiForcado > 0f) return DpiForcado;
                float d = Screen.dpi;
                return d > 0f ? d : DpiPadrao;   // editor/headless devolve 0
            }
        }

        public static float Px(float dp) => PxCom(dp, Dpi);

        /// <summary>Conta pura (testavel): dp -> px numa dpi dada.</summary>
        public static float PxCom(float dp, float dpi) => dp * (dpi > 0f ? dpi : DpiPadrao) / DpiPadrao;

        /// <summary>Piso do alvo de toque.</summary>
        public const float AlvoMinimoDp = 48f;
    }
}
