using System.Collections.Generic;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// B-CORE-05 Orders 2.0: um cartao por vaga de encomenda (o da FASE9/v0.6d multiplicado). O 1o (420 x 104, acima do botao VIP) e' sempre
    /// a principal (a mais antiga, a dos chips "Bonus" e "Trocar"); as outras vagas e a de evento empilham acima dos chips, a 80%. Cada
    /// cartao: icone do 1o item, titulo (Textos.Encomenda: "Venda 5 espadas", "Urgente: 20 espadas", "Grunk: 40 espadas"), barra com a conta
    /// (Textos.BarraEncomenda: "3/20 · Fina+ · 3:12") e o premio de agora (no prazo do Rush, com o bonus). Encomenda nova entra pulando;
    /// cada venda dela da um pulso; entregue (Entregue, no Ev.OrderDone) cresce e some em 0,35 s e o lugar e' da seguinte.
    /// </summary>
    public sealed class CartoesEncomenda
    {
        sealed class Cartao
        {
            public RectTransform R; public CanvasGroup G; public Image Icon, Fill, Coin; public Text Titulo, Premio, Barra;
            public Encomenda E; public int Visto, PrazoS; public long Valor; public float T, Punch, Escala; public bool Out;
        }

        public const float W = 420f, H = 104f, Gap = 8f, Menor = 0.8f;
        const float PunchTime = 0.2f, OutTime = 0.35f;
        /// <summary>3 vagas (Renome 3) + a do evento.</summary>
        const int N = 4;
        readonly Cartao[] _c = new Cartao[N];
        readonly List<Encomenda> _ordem = new List<Encomenda>();

        /// <summary>Em `parent` (area segura), ancorados em (0, `top`), a `edge` px da borda; o 1o a 24 px e os outros acima de `livre` px
        /// (os chips de anuncio da principal).</summary>
        public CartoesEncomenda(RectTransform parent, float edge, float top, float livre, Sprite coin, Color coinTint)
        {
            float y = 24f;
            for (int k = 0; k < N; k++)
            {
                float esc = k == 0 ? 1f : Menor;
                _c[k] = Novo(parent, k, new Vector2(edge, y), esc, top, coin, coinTint);
                y += k == 0 ? H + Gap + livre : H * Menor + Gap;
            }
        }

        static Cartao Novo(RectTransform parent, int k, Vector2 pos, float esc, float top, Sprite coin, Color coinTint)
        {
            var c = new Cartao { Escala = esc };
            Image edge = Art.Panel(parent, k == 0 ? "Encomenda" : "Encomenda" + (k + 1), Art.HudEdge, new Vector2(0f, top), new Vector2(0f, top));
            edge.rectTransform.pivot = Vector2.zero; edge.rectTransform.sizeDelta = new Vector2(W, H);
            edge.rectTransform.anchoredPosition = pos;
            edge.raycastTarget = false;
            Image face = Art.Panel(edge.transform, "Fundo", Art.ComAlfa(Art.HudFace, 0.95f), Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(5f, 5f); face.rectTransform.offsetMax = new Vector2(-5f, -5f);
            face.raycastTarget = false;
            c.Icon = Art.Node(edge.transform, "Item", new Vector2(0.03f, 0.1f), new Vector2(0.22f, 0.9f)).gameObject.AddComponent<Image>();
            c.Icon.preserveAspect = true; c.Icon.raycastTarget = false;
            // titulo numa linha so: caixa de ~44 px (2 linhas nao cabem) e o best-fit encolhe "Variado: 6 ferramentas + 9 espadas" em vez de quebrar
            c.Titulo = Art.Outlined(Art.NewText(edge.transform, "Texto", 34, new Vector2(0.24f, 0.5f), new Vector2(0.98f, 0.92f), TextAnchor.MiddleLeft), 2f);
            c.Titulo.fontStyle = FontStyle.Bold;
            c.Titulo.resizeTextForBestFit = true; c.Titulo.resizeTextMinSize = 18; c.Titulo.resizeTextMaxSize = 34;
            c.Titulo.verticalOverflow = VerticalWrapMode.Truncate;
            Image bar = Art.Node(edge.transform, "Barra", new Vector2(0.25f, 0.29f), new Vector2(0.64f, 0.29f)).gameObject.AddComponent<Image>();
            bar.rectTransform.offsetMin = new Vector2(0f, -10f); bar.rectTransform.offsetMax = new Vector2(0f, 10f);   // 20 px de altura
            bar.color = new Color(0f, 0f, 0f, 0.6f); bar.raycastTarget = false;
            c.Fill = Art.Node(bar.transform, "Cheio", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            c.Fill.color = Art.Good; c.Fill.raycastTarget = false;
            c.Barra = Art.Outlined(Art.NewText(bar.transform, "Conta", 24, Vector2.zero, Vector2.one), 2f);
            c.Barra.fontStyle = FontStyle.Bold; c.Barra.horizontalOverflow = HorizontalWrapMode.Overflow;
            c.Coin = Art.Node(edge.transform, "Moeda", new Vector2(0.67f, 0.08f), new Vector2(0.77f, 0.5f)).gameObject.AddComponent<Image>();
            c.Coin.sprite = coin; c.Coin.color = coinTint; c.Coin.preserveAspect = true; c.Coin.raycastTarget = false;
            c.Premio = Art.Outlined(Art.NewText(edge.transform, "Premio", 32, new Vector2(0.78f, 0.04f), new Vector2(0.99f, 0.54f), TextAnchor.MiddleLeft), 2f);
            c.Premio.fontStyle = FontStyle.Bold; c.Premio.color = Art.Accent; c.Premio.horizontalOverflow = HorizontalWrapMode.Overflow;
            c.R = edge.rectTransform;
            c.R.localScale = Vector3.one * esc;
            c.G = edge.gameObject.AddComponent<CanvasGroup>();
            c.G.blocksRaycasts = false;
            c.R.gameObject.SetActive(false);
            return c;
        }

        /// <summary>Algum cartao saindo (entregue): o estado Transition do intersticial (Estados).</summary>
        public bool Saindo
        {
            get { foreach (Cartao c in _c) if (c.Out && c.R.gameObject.activeSelf) return true; return false; }
        }

        /// <summary>O 1o cartao mostra a principal (os chips de anuncio ficam sobre ele).</summary>
        public bool PrincipalNaTela(Sim s) => _c[0].E != null && !_c[0].Out && _c[0].E == s.Encomendas.Principal && _c[0].R.gameObject.activeSelf;

        /// <summary>Ev.OrderDone: o cartao da encomenda que acabou de sair das vagas mostra a conta cheia e comeca a sair. Devolve de onde as
        /// moedas voam (a moeda dele; nenhum na tela: a do 1o).</summary>
        public Vector3 Entregue(Sim s)
        {
            foreach (Cartao c in _c)
            {
                if (c.E == null || c.Out || s.Encomendas.Vagas.Contains(c.E)) continue;
                c.Barra.text = c.E.Total + "/" + c.E.Total; c.Fill.rectTransform.anchorMax = Vector2.one;   // a ultima venda e a entrega caem no mesmo tick
                c.Out = true; c.T = 0f;
                return c.Coin.rectTransform.position;
            }
            return _c[0].Coin.rectTransform.position;
        }

        /// <summary>Cada quadro (tempo real): principal no 1o, as outras vagas na ordem em que abriram, a de evento por ultimo.</summary>
        public void Refresh(Sim s, float dt)
        {
            _ordem.Clear();
            foreach (Encomenda e in s.Encomendas.Vagas) if (e.Tipo != OrderType.Event) _ordem.Add(e);
            foreach (Encomenda e in s.Encomendas.Vagas) if (e.Tipo == OrderType.Event) _ordem.Add(e);
            for (int k = 0; k < N; k++)
            {
                Cartao c = _c[k];
                c.T += dt; c.Punch = Mathf.Max(0f, c.Punch - dt);
                if (c.Out)
                {
                    float q = c.T / OutTime;
                    if (q < 1f) { c.R.localScale = Vector3.one * c.Escala * (1f + 0.25f * q); c.G.alpha = 1f - q; continue; }
                    c.Out = false; c.E = null; c.R.gameObject.SetActive(false);
                }
                Encomenda want = k < _ordem.Count ? _ordem[k] : null;
                if (want != c.E) Mostra(c, want);
                if (c.E == null) continue;
                int prog = c.E.Progresso, prazo = Mathf.CeilToInt(c.E.Prazo);
                if (prog != c.Visto || prazo != c.PrazoS)   // texto novo so quando muda (o cronometro do Rush, 1x por segundo)
                {
                    if (c.Visto >= 0 && prog != c.Visto) c.Punch = PunchTime;   // cada venda dela da um pulso
                    c.Visto = prog; c.PrazoS = prazo;
                    c.Barra.text = Textos.BarraEncomenda(c.E);
                    c.Fill.rectTransform.anchorMax = new Vector2(c.E.Total > 0 ? Mathf.Clamp01(prog / (float)c.E.Total) : 0f, 1f);
                }
                long valor = Textos.PremioEncomenda(c.E);   // o bonus do anuncio e o fim do prazo do Rush mudam o premio
                if (valor != c.Valor) { c.Valor = valor; c.Premio.text = "+" + valor; }
                c.R.localScale = Vector3.one * c.Escala * Game.Pop(c.T) * (1f + 0.12f * Mathf.Sin(Mathf.PI * c.Punch / PunchTime));
                c.G.alpha = 1f;
            }
        }

        static void Mostra(Cartao c, Encomenda e)
        {
            c.E = e; c.Visto = -1; c.PrazoS = -1; c.Valor = -1; c.T = 0f; c.Punch = 0f;
            if (e == null) { c.R.gameObject.SetActive(false); return; }
            Item it = e.Itens[0];
            Sprite s = Art.ItemArt(it, true);
            c.Icon.sprite = s != null ? s : Art.ItemSprite(it);
            c.Icon.color = s != null ? Color.white : Art.ItemColor[(int)it];
            c.Titulo.text = Textos.Encomenda(e);
            c.R.gameObject.SetActive(true);
        }
    }
}
