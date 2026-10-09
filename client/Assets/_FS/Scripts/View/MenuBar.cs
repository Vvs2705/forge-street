using System;
using System.Collections.Generic;
using System.Globalization;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// Menu inferior de melhorias (docs/FASE6_MENU_MELHORIAS.md s3): barra fixa embaixo com o botao "Melhorias"; aberto, uma
    /// fileira de cartoes rolavel na horizontal acima dela. Toque no cartao compra na hora (Sim.TryBuyMenu) e o Ev.Bought ja
    /// toca o som e solta o "+nome!" no Game. Cartao comprado some; ordem fixa por preco (nada pula embaixo do dedo).
    /// v0.5b (BENCHMARK_VISUAL P1-2): cartao creme com borda de couro e chanfro, icone renderizado grande (tools/ui_v05b_blender.py),
    /// efeito em numeros ("3 → 6"), preco num botao verde (da para comprar) ou cinza (nao da; vermelho lia como erro) e badge
    /// vermelho com o numero no botao "Melhorias".
    /// </summary>
    public sealed class MenuBar
    {
        public const float Band = 0.085f;   // fracao da tela da barra; a camera reserva essa faixa como reserva a de cima
        const float Tray = 0.2f;            // fileira de cartoes, acima da barra, so quando aberta (~384 px: cartao de ~360)
        const float CardW = 270f;           // px na referencia 1080x1920
        // paleta do cartao creme; o cartao de configuracoes (Game, v0.6b) usa a mesma
        public static readonly Color Cream = Art.Hex(0xFFF1D6), Leather = Art.Hex(0x5C3A1E), Brown = Art.Hex(0x3A2614),
            Green = Art.Good, GreenDark = Art.Hex(0x2F9A4A), Gray = Art.Hex(0xA49C92), GrayDark = Art.Hex(0x766E66);

        sealed class Card { public int U; public GameObject Go; public Image Icon, Btn, BtnEdge, Coin; public Text Name, Effect, Price; public CanvasGroup Fade; }

        readonly Sim _sim;
        readonly Action<int> _bought;   // fora do Tick o Ev.Bought nao chega ao HandleEvents (o proximo Tick limpa): o Game toca som/efeito por aqui
        readonly RectTransform _bar, _tray, _badge;
        readonly Image _btn;
        readonly Text _btnText, _badgeText;
        readonly List<Card> _cards = new List<Card>();
        bool _open;
        float _t;
        int _ready;

        public MenuBar(RectTransform safe, Sim sim, Action<int> bought)
        {
            _sim = sim;
            _bought = bought;
            _bar = Art.Node(safe, "Menu", new Vector2(0.02f, 0.005f), new Vector2(0.98f, Band));
            Button b = Art.NewButton(_bar, "Melhorias", 44, Art.Pad, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f), Toggle);
            _btn = (Image)b.targetGraphic;
            _btnText = b.GetComponentInChildren<Text>();
            _btnText.rectTransform.offsetMin = new Vector2(70f, 0f);   // espaco do martelo a esquerda
            Sprite ham = Art.Icon("melhoria_martelo", "icone");
            if (ham != null)
            {
                Image hi = Art.Node(_btn.transform, "Icone", new Vector2(0.06f, 0.08f), new Vector2(0.26f, 0.92f)).gameObject.AddComponent<Image>();
                hi.sprite = ham; hi.preserveAspect = true; hi.raycastTarget = false;
            }
            // badge vermelho (44-60 px) com o numero de cartoes compraveis, no canto de cima a direita do botao
            _badge = Art.Node(_btn.transform, "Badge", new Vector2(1f, 1f), new Vector2(1f, 1f));
            _badge.sizeDelta = new Vector2(62f, 62f); _badge.anchoredPosition = new Vector2(-14f, -10f);
            Image bd = _badge.gameObject.AddComponent<Image>();
            bd.sprite = Art.Disc(); bd.color = Art.Bad; bd.raycastTarget = false;
            Image ring = Art.Node(_badge, "Aro", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            ring.sprite = Art.Ring(); ring.color = Color.white; ring.raycastTarget = false;
            ring.rectTransform.offsetMin = new Vector2(-3f, -3f); ring.rectTransform.offsetMax = new Vector2(3f, 3f);
            _badgeText = Art.Outlined(Art.NewText(_badge, "N", 36, Vector2.zero, Vector2.one), 1.5f);
            _badgeText.fontStyle = FontStyle.Bold;
            _badge.gameObject.SetActive(false);

            _tray = Art.Panel(safe, "Fileira", Art.ComAlfa(Art.Hex(0x2A1E14), 0.92f), new Vector2(0.02f, Band + 0.005f), new Vector2(0.98f, Band + Tray)).rectTransform;
            var scroll = _tray.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform view = Art.Node(_tray, "Janela", Vector2.zero, Vector2.one);
            view.offsetMin = new Vector2(12f, 12f); view.offsetMax = new Vector2(-12f, -12f);
            view.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Art.Node(view, "Cartoes", Vector2.zero, new Vector2(0f, 1f));
            content.pivot = new Vector2(0f, 0.5f);
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 14f; row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = false; row.childForceExpandHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = view; scroll.content = content;

            var menu = new List<int>();
            for (int u = 0; u < Upgrades.Count; u++) if (Upgrades.All[u].InMenu) menu.Add(u);
            menu.Sort((a, c) => Upgrades.Cost(a).CompareTo(Upgrades.Cost(c)));
            foreach (int u in menu) _cards.Add(BuildCard(content, u));
            _tray.gameObject.SetActive(false);
        }

        /// <summary>Topo da area tocavel do menu em px de tela (barra, ou fileira quando aberta): o joystick nao comeca abaixo.</summary>
        public float TopPx
        {
            get
            {
                var c = new Vector3[4];
                (_open ? _tray : _bar).GetWorldCorners(c);   // canvas overlay: canto do mundo = px de tela
                return c[1].y;
            }
        }

        public void Toggle()
        {
            _open = !_open;
            _tray.gameObject.SetActive(_open);
            _t = 0f;
        }

        /// <summary>Cartao: borda de couro (o chanfro e' a base 8 px mais grossa), face creme, nome, icone, efeito e botao de preco.</summary>
        Card BuildCard(RectTransform parent, int u)
        {
            UpgradeDef d = Upgrades.All[u];
            var c = new Card { U = u };
            Image edge = Art.Panel(parent, d.Name, Leather, Vector2.zero, Vector2.one);
            c.Go = edge.gameObject;
            c.Go.AddComponent<LayoutElement>().preferredWidth = CardW;
            c.Fade = c.Go.AddComponent<CanvasGroup>();
            Image face = Art.Panel(edge.transform, "Face", Cream, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(5f, 13f); face.rectTransform.offsetMax = new Vector2(-5f, -5f);
            face.raycastTarget = false;
            var btn = c.Go.AddComponent<Button>();
            btn.targetGraphic = edge;
            btn.onClick.AddListener(() => { if (_sim.TryBuyMenu((Upgrade)u)) { _bought(u); _t = 0f; } });
            c.Name = Art.NewText(face.transform, "Nome", 30, new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.98f));
            c.Name.text = d.Name; c.Name.fontStyle = FontStyle.Bold; c.Name.color = Brown;
            c.Name.resizeTextForBestFit = true; c.Name.resizeTextMinSize = 20; c.Name.resizeTextMaxSize = 30;
            c.Icon = Art.Node(face.transform, "Icone", new Vector2(0.14f, 0.44f), new Vector2(0.86f, 0.84f)).gameObject.AddComponent<Image>();
            c.Icon.preserveAspect = true; c.Icon.raycastTarget = false;
            (c.Icon.sprite, c.Icon.color) = Icon((Upgrade)u);
            c.Effect = Art.NewText(face.transform, "Efeito", 36, new Vector2(0.03f, 0.27f), new Vector2(0.97f, 0.45f));
            c.Effect.supportRichText = true; c.Effect.fontStyle = FontStyle.Bold; c.Effect.color = Brown;
            c.Effect.resizeTextForBestFit = true; c.Effect.resizeTextMinSize = 20; c.Effect.resizeTextMaxSize = 36;
            c.Effect.text = Effect((Upgrade)u);
            // botao do preco: base escura (chanfro de 7 px) + face verde/cinza, moeda a esquerda e valor com contorno
            c.BtnEdge = Art.Panel(face.transform, "Preco", GreenDark, new Vector2(0.07f, 0.035f), new Vector2(0.93f, 0.25f));
            c.BtnEdge.raycastTarget = false;
            c.Btn = Art.Panel(c.BtnEdge.transform, "Face", Green, Vector2.zero, Vector2.one);
            c.Btn.rectTransform.offsetMin = new Vector2(0f, 7f);
            c.Btn.raycastTarget = false;
            c.Coin = Art.Node(c.Btn.transform, "Moeda", new Vector2(0.04f, 0.08f), new Vector2(0.34f, 0.92f)).gameObject.AddComponent<Image>();
            Sprite moeda = Art.Icon("moeda", "icone");
            c.Coin.sprite = moeda != null ? moeda : Art.Disc(); c.Coin.color = moeda != null ? Color.white : Art.Accent;
            c.Coin.preserveAspect = true; c.Coin.raycastTarget = false;
            c.Price = Art.Outlined(Art.NewText(c.Btn.transform, "Valor", 36, new Vector2(0.3f, 0f), new Vector2(0.97f, 1f)), 2f);
            c.Price.fontStyle = FontStyle.Bold; c.Price.color = Color.white;
            c.Price.resizeTextForBestFit = true; c.Price.resizeTextMinSize = 16; c.Price.resizeTextMaxSize = 36;
            // best-fit so encolhe com Truncate (o NewText vem com Overflow): "requer Balcao 5 vagas" cabe no botao em 2 linhas
            c.Name.verticalOverflow = c.Effect.verticalOverflow = c.Price.verticalOverflow = VerticalWrapMode.Truncate;
            return c;
        }

        /// <summary>Icone renderizado da melhoria (tools/ui_v05b_blender.py); sem a folha, a forma/cor procedural de antes.</summary>
        static (Sprite, Color) Icon(Upgrade u)
        {
            string art = u switch
            {
                Upgrade.FurnaceSpeed1 => "melhoria_fole", Upgrade.FurnaceSpeed2 => "melhoria_fole_duplo",
                Upgrade.PlayerCapacity => "melhoria_mochila", Upgrade.PlayerSpeed => "melhoria_botas",
                Upgrade.CounterCapacity => "melhoria_vitrine", Upgrade.HelperSpeed => "melhoria_cesto",
                Upgrade.HammerSpeed => "melhoria_martelo", Upgrade.JewelSpeed => "melhoria_lupa",
                Upgrade.Counter5 or Upgrade.Counter6 or Upgrade.Counter7 or Upgrade.Counter8 => "melhoria_sino",
                _ => null,
            };
            if (art != null && Art.Icon(art, "icone") is Sprite s) return (s, Color.white);
            return u switch
            {
                Upgrade.FurnaceSpeed1 or Upgrade.FurnaceSpeed2 => ItemIcon(Item.Ingot),
                Upgrade.HammerSpeed => ItemIcon(Item.Sword),
                Upgrade.JewelSpeed or Upgrade.JewelVitrine => ItemIcon(Item.Jewel),
                Upgrade.CounterCapacity or Upgrade.Counter5 or Upgrade.Counter6 or Upgrade.Counter7 or Upgrade.Counter8 => (Art.Star(), Art.Accent),
                Upgrade.HelperSpeed => (Art.Disc(), Art.Worker),
                _ => (Art.Disc(), Art.Player),
            };
        }
        static (Sprite, Color) ItemIcon(Item i) => Art.ItemArt(i, true) is Sprite s ? (s, Color.white) : (Art.ItemSprite(i), Art.ItemColor[(int)i]);

        /// <summary>Efeito em numeros, lidos do Balance (mudou o balanco, muda o cartao); sem numero simples, a frase de sempre.</summary>
        static string Effect(Upgrade u)
        {
            int n = (int)u - (int)Upgrade.Counter5 + 5;
            return u switch
            {
                Upgrade.FurnaceSpeed1 => Arrow(N(Balance.FurnaceTime0), N(Balance.FurnaceTime1) + " s"),
                Upgrade.FurnaceSpeed2 => Arrow(N(Balance.FurnaceTime1), N(Balance.FurnaceTime2) + " s"),
                Upgrade.PlayerCapacity => Arrow(N(Balance.PlayerCap), N(Balance.PlayerCapUp)),
                Upgrade.PlayerSpeed => Arrow(N(Balance.PlayerSpeed), N(Balance.PlayerSpeedUp) + " m/s"),
                Upgrade.CounterCapacity => "estoque " + Arrow(N(Balance.CounterCap0), N(Balance.CounterCap1)),
                Upgrade.HelperSpeed => "carga " + Arrow(N(Balance.WorkerCap), N(Balance.WorkerCapUp)),
                Upgrade.HammerSpeed => Arrow(N(Balance.HammerTime0), N(Balance.HammerTime1) + " s"),
                Upgrade.JewelSpeed => "tempo −" + N(100f * (1f - Balance.JewelSpeedMul)) + "%",
                Upgrade.JewelVitrine => "fila " + Arrow(N(Balance.JewelQueueCap), N(Balance.JewelQueueCapUp)),
                Upgrade.Counter5 or Upgrade.Counter6 or Upgrade.Counter7 or Upgrade.Counter8 => Arrow(N(n - 1), n + " vagas"),
                _ => Upgrades.All[(int)u].Desc,
            };
        }
        static string Arrow(string a, string b) => a + " <color=#2F9A4A>→</color> " + b;
        static string N(float v) => v.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');   // virgula decimal sem depender da cultura do aparelho

        /// <summary>Estado dos cartoes 4x/s; o badge pulsa a cada quadro quando ha o que comprar.</summary>
        public void Refresh(float dt)
        {
            if (_ready > 0) _badge.localScale = Vector3.one * (1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)));
            _t -= dt;
            if (_t > 0f) return;
            _t = 0.25f;
            int ready = 0;
            foreach (Card c in _cards)
            {
                UpgradeDef d = Upgrades.All[c.U];
                bool bought = _sim.Bought[c.U], locked = d.Requires >= 0 && !_sim.Bought[d.Requires];
                int cost = Upgrades.Cost(c.U);
                bool can = !bought && !locked && _sim.Gold >= cost;
                if (can) ready++;
                if (c.Go.activeSelf != !bought) c.Go.SetActive(!bought);
                if (bought) continue;
                c.Fade.alpha = locked ? 0.6f : 1f;
                c.Btn.color = can ? Green : Gray;
                c.BtnEdge.color = can ? GreenDark : GrayDark;
                c.Coin.enabled = !locked;
                c.Price.text = locked ? "requer " + Upgrades.All[d.Requires].Name : cost.ToString();
                c.Price.rectTransform.anchorMin = new Vector2(locked ? 0.04f : 0.3f, 0f);
            }
            _ready = ready;
            _btn.color = ready > 0 ? Art.Accent : Art.Pad;
            _btnText.color = ready > 0 ? Art.Bg : Art.Ink;
            if (_badge.gameObject.activeSelf != ready > 0) _badge.gameObject.SetActive(ready > 0);
            _badgeText.text = ready.ToString();
        }
    }
}
