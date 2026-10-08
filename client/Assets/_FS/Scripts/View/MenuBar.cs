using System;
using System.Collections.Generic;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// Menu inferior de melhorias (docs/FASE6_MENU_MELHORIAS.md s3): barra fixa embaixo com o botao "Melhorias"; aberto, uma
    /// fileira de cartoes rolavel na horizontal acima dela. Toque no cartao compra na hora (Sim.TryBuyMenu) e o Ev.Bought ja
    /// toca o som e solta o "+nome!" no Game. Cartao comprado some; ordem fixa por preco (nada pula embaixo do dedo).
    /// </summary>
    public sealed class MenuBar
    {
        public const float Band = 0.085f;   // fracao da tela da barra; a camera reserva essa faixa como reserva a de cima
        const float Tray = 0.17f;           // fileira de cartoes, acima da barra, so quando aberta
        const float CardW = 290f;           // px na referencia 1080x1920

        sealed class Card { public int U; public GameObject Go; public Image Bg, Icon; public Text Name, Desc, Price; }

        readonly Sim _sim;
        readonly Action<int> _bought;   // fora do Tick o Ev.Bought nao chega ao HandleEvents (o proximo Tick limpa): o Game toca som/efeito por aqui
        readonly RectTransform _bar, _tray;
        readonly Image _btn;
        readonly Text _btnText;
        readonly List<Card> _cards = new List<Card>();
        bool _open;
        float _t;

        public MenuBar(RectTransform safe, Sim sim, Action<int> bought)
        {
            _sim = sim;
            _bought = bought;
            _bar = Art.Node(safe, "Menu", new Vector2(0.02f, 0.005f), new Vector2(0.98f, Band));
            Button b = Art.NewButton(_bar, "Melhorias", 44, Art.Pad, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f), Toggle);
            _btn = (Image)b.targetGraphic;
            _btnText = b.GetComponentInChildren<Text>();

            _tray = Art.Panel(safe, "Fileira", Art.ComAlfa(Art.Bg, 0.92f), new Vector2(0.02f, Band + 0.005f), new Vector2(0.98f, Band + Tray)).rectTransform;
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

        Card BuildCard(RectTransform parent, int u)
        {
            UpgradeDef d = Upgrades.All[u];
            var c = new Card { U = u };
            c.Bg = Art.Panel(parent, d.Name, Art.Pad, Vector2.zero, Vector2.one);
            c.Go = c.Bg.gameObject;
            c.Go.AddComponent<LayoutElement>().preferredWidth = CardW;
            var btn = c.Go.AddComponent<Button>();
            btn.targetGraphic = c.Bg;
            btn.onClick.AddListener(() => { if (_sim.TryBuyMenu((Upgrade)u)) { _bought(u); _t = 0f; } });
            Image back = Art.Node(c.Bg.transform, "FundoIcone", new Vector2(0.05f, 0.6f), new Vector2(0.3f, 0.95f)).gameObject.AddComponent<Image>();
            back.sprite = Art.Disc(); back.color = Art.ComAlfa(Art.Bg, 0.85f); back.preserveAspect = true; back.raycastTarget = false;   // icone le em cartao dourado e escuro
            RectTransform ic = Art.Node(back.transform, "Icone", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            c.Icon = ic.gameObject.AddComponent<Image>();
            c.Icon.preserveAspect = true; c.Icon.raycastTarget = false;
            (c.Icon.sprite, c.Icon.color) = Icon((Upgrade)u);
            c.Name = Art.NewText(c.Bg.transform, "Nome", 34, new Vector2(0.33f, 0.6f), new Vector2(0.97f, 0.94f), TextAnchor.MiddleLeft);
            c.Name.text = d.Name; c.Name.fontStyle = FontStyle.Bold;
            c.Desc = Art.NewText(c.Bg.transform, "Efeito", 26, new Vector2(0.06f, 0.27f), new Vector2(0.94f, 0.6f), TextAnchor.UpperLeft);
            c.Desc.text = d.Desc;
            c.Price = Art.NewText(c.Bg.transform, "Preco", 34, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.27f));
            c.Price.fontStyle = FontStyle.Bold;
            return c;
        }

        /// <summary>Icone simples: forma/cor do que a melhoria acelera (item: arte `icone` da v0.5, mascara se faltar a folha).</summary>
        static (Sprite, Color) Icon(Upgrade u) => u switch
        {
            Upgrade.FurnaceSpeed1 or Upgrade.FurnaceSpeed2 => ItemIcon(Item.Ingot),
            Upgrade.HammerSpeed => ItemIcon(Item.Sword),
            Upgrade.JewelSpeed or Upgrade.JewelVitrine => ItemIcon(Item.Jewel),
            Upgrade.CounterCapacity or Upgrade.Counter5 or Upgrade.Counter6 or Upgrade.Counter7 or Upgrade.Counter8 => (Art.Star(), Art.Accent),   // balcao (FASE7)
            Upgrade.HelperSpeed => (Art.Disc(), Art.Worker),
            _ => (Art.Disc(), Art.Player),   // mochila, botas: o ferreiro
        };
        static (Sprite, Color) ItemIcon(Item i) => Art.ItemArt(i, true) is Sprite s ? (s, Color.white) : (Art.ItemSprite(i), Art.ItemColor[(int)i]);

        /// <summary>Estado dos cartoes 4x/s e o destaque do botao quando ha o que comprar.</summary>
        public void Refresh(float dt)
        {
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
                c.Bg.color = can ? Art.Accent : locked ? Art.ComAlfa(Art.Pad, 0.6f) : Art.Pad;
                Color ink = can ? Art.Bg : locked ? Art.ComAlfa(Art.Ink, 0.45f) : Art.Ink;
                c.Name.color = c.Desc.color = ink;
                c.Icon.color = Art.ComAlfa(c.Icon.color, locked ? 0.35f : 1f);
                c.Price.text = locked ? "requer " + Upgrades.All[d.Requires].Name : cost + " de ouro";
                c.Price.color = can ? Art.Bg : locked ? Art.ComAlfa(Art.Ink, 0.45f) : Art.Bad;
            }
            _btn.color = ready > 0 ? Art.Accent : Art.Pad;
            _btnText.color = ready > 0 ? Art.Bg : Art.Ink;
            _btnText.text = ready > 0 ? $"Melhorias ({ready})" : "Melhorias";
        }
    }
}
