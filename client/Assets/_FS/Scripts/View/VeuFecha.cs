using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FS
{
    /// <summary>
    /// fix/ui-fechar-ao-tocar-fora: o veu escuro dos paineis modais (Art.Veu) fecha o painel com um TAP fora do cartao. O cartao tem a
    /// propria Image com raycast, entao so o toque fora dele chega aqui; arrastar (rolagem que comecou no veu) nao fecha. O veu segura o
    /// toque: nada por baixo (mundo, joystick, HUD) recebe o toque que fechou. Quem nao fecha por toque fora (idade/consentimento) nao
    /// passa o Fecha; quem fecha so as vezes (o painel do Game: escolha, cofre) passa o Pode.
    /// Sem IDragHandler (Conselho do #187): o veu e' pai do cartao, e um handler de arraste nele virava o pointerDrag de todo botao do
    /// cartao fora de ScrollRect; o InputSystemUIInputModule cancela o clique quando pointerPress != pointerDrag, entao um escorregao de
    /// 10 px no Fechar/Pegar/OK nao clicava. O gesto e' decidido no soltar pela posicao do aperto, a do soltar e o tempo.
    /// </summary>
    public sealed class VeuFecha : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action Fecha;
        public Func<bool> Pode;

        readonly ToqueFora _gesto = new ToqueFora();

        /// <summary>O cartao e' filho do veu: um toque em texto/fundo do cartao (sem handler proprio) sobe pela hierarquia ate aqui. So conta
        /// o toque que caiu NO veu (o raycast acertou o proprio no), nunca o que veio de dentro do cartao (pego pelo -testetoque).</summary>
        public void OnPointerDown(PointerEventData e)
        {
            if (e.pointerCurrentRaycast.gameObject != gameObject) { _gesto.Cancela(); return; }
            _gesto.Aperta(e.pressPosition.x, e.pressPosition.y, Time.unscaledTime);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (_gesto.Solta(e.position.x, e.position.y, Time.unscaledTime, Dp.Px(ToqueFora.RaioDp)) != Gesto.Toque) return;
            if (Fecha == null || ToqueFora.Bloqueado || (Pode != null && !Pode())) return;
            Fecha();
        }

        void OnDisable() => _gesto.Cancela();
    }
}
