using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FS
{
    /// <summary>
    /// fix/ui-fechar-ao-tocar-fora: a bandeja de melhorias nao e' modal (o joystick segue vivo com ela aberta), entao nao tem veu. Com ela
    /// aberta, um toque que comeca no mundo ou na HUD vazia (acima da bandeja e fora de qualquer botao/painel de UI) fica reservado para
    /// ela: nao vai ao Game.Toque (golpe, faixa, lote, estacao). Soltou parado e rapido = fecha a bandeja; arrastou = foi o joystick, a
    /// bandeja fica. A regra de quando pode fechar e' a ToqueFora.FechaMenu (testada no dotnet).
    /// </summary>
    public sealed class BandejaFora
    {
        readonly ToqueFora _gesto = new ToqueFora();
        readonly List<RaycastResult> _hits = new List<RaycastResult>();
        PointerEventData _ped;
        EventSystem _pedEs;

        /// <summary>Chamado todo quadro sem modal. Devolve true quando o toque deste quadro e' da bandeja (o Game nao o passa ao mundo).</summary>
        public bool Tick(Pointer p, bool podeFechar, float topoBandejaPx, Action fechar)
        {
            if (p == null) { _gesto.Cancela(); return false; }
            Vector2 pos = p.position.ReadValue();
            float raio = Dp.Px(ToqueFora.RaioDp);
            if (p.press.wasPressedThisFrame)
            {
                _gesto.Cancela();
                if (!podeFechar || pos.y <= topoBandejaPx || SobreUi(pos)) return false;
                _gesto.Aperta(pos.x, pos.y, Time.unscaledTime);
                return true;
            }
            if (!_gesto.Ativo) return false;
            if (p.press.isPressed) { _gesto.Move(pos.x, pos.y, raio); return true; }
            if (_gesto.Solta(pos.x, pos.y, Time.unscaledTime, raio) == Gesto.Toque && podeFechar) fechar();
            return true;
        }

        /// <summary>Um modal abriu por cima no meio do gesto: o soltar e' dele, nao da bandeja.</summary>
        public void Cancela() => _gesto.Cancela();

        /// <summary>O toque caiu num botao/cartao de UI (HUD, rotulos do mundo com botao, a propria bandeja): e' dele, nao e' "fora".</summary>
        bool SobreUi(Vector2 pos)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            if (_ped == null || _pedEs != es) { _ped = new PointerEventData(es); _pedEs = es; }
            _ped.position = pos;
            _hits.Clear();
            es.RaycastAll(_ped, _hits);
            return _hits.Count > 0;
        }
    }
}
