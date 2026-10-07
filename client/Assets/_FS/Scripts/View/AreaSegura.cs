using UnityEngine;

namespace FS
{
    /// <summary>
    /// Margens da area segura em px (notch, camera, barra de gestos). Copiado do ARKANA
    /// (Assets/_Arkana/Scripts/UI/AreaSegura.cs). Origem Unity: (0,0) e' o canto inferior esquerdo da tela.
    /// </summary>
    public struct Margens
    {
        public float Esq, Topo, Dir, Baixo;
        public Margens(float esq, float topo, float dir, float baixo) { Esq = esq; Topo = topo; Dir = dir; Baixo = baixo; }
    }

    public static class AreaSegura
    {
        /// <summary>Conta pura: recorte seguro + tamanho da tela -> margens (nunca negativas).</summary>
        public static Margens Calcular(Rect safe, Vector2 tela)
        {
            return new Margens(
                Mathf.Max(safe.xMin, 0f),
                Mathf.Max(tela.y - safe.yMax, 0f),
                Mathf.Max(tela.x - safe.xMax, 0f),
                Mathf.Max(safe.yMin, 0f));
        }

        public static Margens Atual() => Calcular(Screen.safeArea, new Vector2(Screen.width, Screen.height));

        /// <summary>Ancora um no' de tela cheia (anchors 0..1) para dentro da area segura.</summary>
        public static void AncorarDentro(RectTransform rt, Margens m)
        {
            float w = Mathf.Max(1f, Screen.width), h = Mathf.Max(1f, Screen.height);
            rt.anchorMin = new Vector2(m.Esq / w, m.Baixo / h);
            rt.anchorMax = new Vector2(1f - m.Dir / w, 1f - m.Topo / h);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Estica ate' as bordas do pai (tela cheia).</summary>
        public static void Esticar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
