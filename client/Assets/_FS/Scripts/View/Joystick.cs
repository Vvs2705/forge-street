using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// Logica PURA do joystick, copiada do ARKANA (JoystickVirtual.cs / JoystickLogica). Entrada: deslocamento do dedo em
    /// px a partir do centro e o raio util. Saida: direcao com comprimento 0..1 com deadzone REESCALADA e curva: cruzou a
    /// zona morta, sai de 0 e nao de 12% da velocidade (sem degrau).
    /// </summary>
    public sealed class JoystickLogica
    {
        public float Deadzone;
        public float Curva;

        public JoystickLogica(float deadzone, float curva) { Deadzone = deadzone; Curva = curva; }

        public Vector2 Direcao(Vector2 deslocPx, float raioPx)
        {
            if (raioPx <= 0f) return Vector2.zero;
            Vector2 v = deslocPx / raioPx;
            float len = v.magnitude;
            if (len > 1f) { v /= len; len = 1f; }
            if (len < Deadzone || len <= 0f) return Vector2.zero;
            float util = (len - Deadzone) / (1f - Deadzone);
            float mag = Mathf.Pow(Mathf.Clamp01(util), Curva <= 0f ? 1f : Curva);
            return v / len * mag;
        }
    }

    /// <summary>
    /// Joystick FLUTUANTE de um dedo: toque em qualquer lugar fora da faixa da HUD de cima e a base nasce ali;
    /// arrasta para andar; solta e some. 2026-11 (Vinicius: "o icone ocupa espaco, pode deixar oculto, apenas sinalizar"): a base e o miolo
    /// ficam INVISIVEIS (Visivel = false); o toque so e' sinalizado por um anel pequeno que nasce no dedo e some em SinalDur s. Le o Input System direto (Pointer.current: mouse no PC, toque no Android),
    /// sem EventSystem, porque o alvo e' a tela inteira. Vive num Canvas de escala 1 (px = px) para usar dp.
    /// </summary>
    public sealed class Joystick
    {
        public Vector2 Direction { get; private set; }
        public bool Active { get; private set; }

        /// <summary>Fracao de cima da tela reservada a HUD: toque ali nao abre o joystick.</summary>
        public float TopBand = 0.16f;
        public float BottomPx;   // topo da barra de melhorias (px): toque abaixo e' do menu, nao do joystick
        public bool Blocked;   // painel modal aberto
        /// <summary>B-CORE-03: toque que comeca aqui (px) e' de outro alvo (anel do golpe, barra de qualidade), nao abre o joystick.</summary>
        public System.Func<Vector2, bool> Ignora;

        readonly JoystickLogica _logic = new JoystickLogica(0.12f, 1f);
        readonly RectTransform _base, _knob;
        readonly Image _ring;
        readonly RectTransform _sinal; readonly Image _sinalImg;
        Vector2 _anchor;
        float _sinalT = -1f;
        /// <summary>Mostrar a base e o miolo (o visual antigo). Desligado por padrao: so o sinal do toque.</summary>
        public bool Visivel;
        const float SinalDur = 0.35f;
        float RadiusPx => Dp.Px(52f);

        public Joystick(Transform canvasEscala1)
        {
            float side = Dp.Px(132f), knob = Dp.Px(50f);
            _base = Art.Node(canvasEscala1, "Joystick", Vector2.zero, Vector2.zero);
            _base.pivot = new Vector2(0.5f, 0.5f);
            _base.sizeDelta = new Vector2(side, side);
            Image fundo = _base.gameObject.AddComponent<Image>();
            // A-UI-16: marrom e creme da HUD (eram azul-escuro e branco-azulado)
            fundo.sprite = Art.Disc(); fundo.color = Art.ComAlfa(Art.HudFace, 0.35f); fundo.raycastTarget = false;
            RectTransform anel = Art.Node(_base, "Anel", Vector2.zero, Vector2.one);
            _ring = anel.gameObject.AddComponent<Image>();
            _ring.sprite = Art.Ring(); _ring.color = Art.ComAlfa(Art.HudEdge, 0.45f); _ring.raycastTarget = false;
            _knob = Art.Node(_base, "Miolo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _knob.sizeDelta = new Vector2(knob, knob);
            Image miolo = _knob.gameObject.AddComponent<Image>();
            miolo.sprite = Art.Disc(); miolo.color = Art.ComAlfa(MenuBar.Cream, 0.9f); miolo.raycastTarget = false;
            _base.gameObject.SetActive(false);
            _sinal = Art.Node(canvasEscala1, "SinalToque", Vector2.zero, Vector2.zero);
            _sinal.pivot = new Vector2(0.5f, 0.5f);
            _sinal.sizeDelta = new Vector2(Dp.Px(30f), Dp.Px(30f));
            _sinalImg = _sinal.gameObject.AddComponent<Image>();
            _sinalImg.sprite = Art.Ring(); _sinalImg.color = Art.ComAlfa(MenuBar.Cream, 0f); _sinalImg.raycastTarget = false;
            _sinal.gameObject.SetActive(false);
        }

        /// <summary>Anel do toque: cresce de 0,6x a 1,2x e some em SinalDur s (unscaled: pausa e boost nao mexem nele).</summary>
        void TickSinal()
        {
            if (_sinalT < 0f) return;
            _sinalT += Time.unscaledDeltaTime;
            float f = Mathf.Clamp01(_sinalT / SinalDur);
            _sinal.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.2f, f);
            _sinalImg.color = Art.ComAlfa(MenuBar.Cream, 0.7f * (1f - f));
            if (f >= 1f) { _sinalT = -1f; _sinal.gameObject.SetActive(false); }
        }

        public void Tick()
        {
            TickSinal();
            Pointer p = Pointer.current;
            if (p == null || Blocked) { Release(); return; }
            Vector2 pos = p.position.ReadValue();
            bool pressed = p.press.isPressed;
            if (!Active)
            {
                if (!p.press.wasPressedThisFrame || pos.y > Screen.height * (1f - TopBand) || pos.y < BottomPx || (Ignora != null && Ignora(pos))) return;
                Active = true;
                _anchor = pos;
                _base.anchoredPosition = pos;
                _base.gameObject.SetActive(Visivel);
                _sinal.anchoredPosition = pos; _sinal.gameObject.SetActive(true); _sinalT = 0f;
            }
            if (!pressed) { Release(); return; }
            Vector2 d = pos - _anchor;
            Direction = _logic.Direcao(d, RadiusPx);
            _knob.anchoredPosition = Vector2.ClampMagnitude(d, RadiusPx) * 0.7f;
        }

        void Release()
        {
            if (!Active && Direction == Vector2.zero) return;
            Active = false;
            Direction = Vector2.zero;
            _knob.anchoredPosition = Vector2.zero;
            _base.gameObject.SetActive(false);
        }
    }
}
