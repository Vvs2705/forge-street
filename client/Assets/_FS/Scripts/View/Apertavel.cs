using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// Botao que diz se o dedo esta nele (A-UI-18: o cartao de melhoria afunda no toque). O Selectable ja guarda o estado, mas so a
    /// subclasse le. Arrastar a fileira solta o botao (o input module manda o "up" quando o arrasto comeca): rolar nao afunda o cartao.
    /// </summary>
    public sealed class Apertavel : Button
    {
        public bool Apertado => IsPressed();
    }
}
