using System;
using System.Collections.Generic;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// Menu inferior de melhorias (docs/archive/FASE6_MENU_MELHORIAS.md s3): barra fixa embaixo com o botao "Melhorias"; aberto, uma
    /// fileira de cartoes rolavel na horizontal acima dela. Toque no cartao compra na hora (Sim.TryBuyMenu) e o Ev.Bought ja
    /// toca o som e solta o "+nome!" no Game. Cartao comprado some. DM §109: nome e textos do cartao pelo Loc (Textos).
    /// v0.5b (BENCHMARK_VISUAL P1-2): cartao creme com borda de couro e chanfro, icone renderizado grande (tools/ui_v05b_blender.py),
    /// preco num botao verde (da para comprar) ou cinza (nao da; vermelho lia como erro) e badge vermelho com o numero no botao "Melhorias".
    /// A-UI-18 (DM §65): nome, icone, beneficio em % ("Fornalha 38% mais rápida"), antes → depois ("4 → 2,5 s") e custo. Estados
    /// com sinal alem da cor: disponivel = botao verde em relevo que respira; sem ouro = cinza achatado + "Faltam N" na paleta da
    /// HUD; travado = esmaecido, sem moeda, "Precisa: X"; apertado = cartao encolhe e o botao afunda. Ordem: disponivel, sem ouro,
    /// travado (VALIDACAO_V05 #3), por preco dentro do grupo; reordena so ao abrir e depois de uma compra (aberta, nada pula
    /// embaixo do dedo so porque entrou ouro).
    /// B-CORE-04: cartao de worker na mesma fileira (W >= 0): retrato da folha do Lote 5A, nome proprio, o que a proxima compra faz (contratar ou
    /// subir nivel) e o preco; travado = "Precisa: Nível II" ou "Precisa: Renome 2"; ajudante sem o upgrade e nivel maximo nao aparecem.
    /// </summary>
    public sealed class MenuBar
    {
        public const float Band = 0.085f;   // fracao da tela da barra; a camera reserva essa faixa como reserva a de cima
        public const float Tray = 0.2f;     // fileira de cartoes, acima da barra, so quando aberta (~384 px: cartao de ~360); a camera desce (Enquadramento)
        const float CardW = 270f;           // px na referencia 1080x1920
        // relevo do botao de preco (px da face acima da base escura): disponivel convida ao toque, cinza achatado, apertado afunda
        const float Raised = 7f, Flat = 2f, PressScale = 0.94f, Breath = 0.04f;
        // paleta do cartao creme; o cartao de configuracoes (Game, v0.6b) usa a mesma
        public static readonly Color Cream = Art.Hex(0xFFF1D6), Leather = Art.Hex(0x5C3A1E), Brown = Art.Hex(0x3A2614),
            Green = Art.Good, GreenDark = Art.Hex(0x2F9A4A), Gray = Art.Hex(0xA49C92), GrayDark = Art.Hex(0x766E66);
        const string Arrow = "<color=#2F9A4A>→</color>";

        sealed class Card
        {
            public int U, Ord, W = -1, Nivel = -1; public Cartao Estado = (Cartao)(-1); public long Falta = -1; public bool Down;   // W: WorkerDef (B-CORE-04)
            public GameObject Go; public Apertavel Toque; public CanvasGroup Fade; public RectTransform TagBox;
            public Image Icon, Face, Edge, Coin; public Text Name, Effect, Detail, Price, Tag; public string Cost, Lock;
        }

        readonly Sim _sim, _raiz;   // C-CORE-02: _raiz = o Sim das metas (Imperio.Raiz); o resto e' do distrito ativo
        readonly Action<int> _bought;   // fora do Tick o Ev.Bought nao chega ao HandleEvents (o proximo Tick limpa): o Game toca som/efeito por aqui
        readonly Action<int, long> _worker;   // B-CORE-04: o mesmo para o worker (indice, preco pago)
        readonly RectTransform _bar, _tray, _badge, _content, _melhorias, _metasBadge;
        readonly ScrollRect _scroll;
        readonly Image _btn;
        readonly Text _btnText, _badgeText, _metasBadgeText;
        readonly GameObject _metas;
        readonly List<Card> _cards = new List<Card>();
        bool _open, _sort = true;
        float _t;
        int _ready, _prontas = -1;

        /// <param name="metas">D-CORE-02: toque no botao Metas (aparece com as Ferramentas, Metas.Aberto, ao lado de Melhorias).</param>
        /// <param name="worker">B-CORE-04: contratou ou subiu nivel pelo cartao (indice do WorkerDef, preco pago).</param>
        public MenuBar(RectTransform safe, Sim sim, Action<int> bought, Action metas, Action<int, long> worker, Sim raiz = null)
        {
            _sim = sim; _raiz = raiz ?? sim;
            _bought = bought;
            _worker = worker;
            _bar = Art.Node(safe, "Menu", new Vector2(0.02f, 0.005f), new Vector2(0.98f, Band));
            // A-UI-16: borda creme e face marrom como as pilulas da HUD (era o azul #2C3550); com compra pronta a face fica ouro
            Button b = Art.NewButton(_bar, Loc.T("menu.melhorias"), 48, Art.HudEdge, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f), Toggle);
            _melhorias = (RectTransform)b.transform;
            _btn = Art.Panel(b.transform, "Face", Art.HudFace, Vector2.zero, Vector2.one);
            _btn.rectTransform.offsetMin = new Vector2(7f, 7f); _btn.rectTransform.offsetMax = new Vector2(-7f, -7f);   // auditoria item 12: o botao principal tem o aro de ouro mais grosso
            _btn.raycastTarget = false;
            _btn.transform.SetAsFirstSibling();   // por baixo da legenda
            b.targetGraphic = _btn;               // o toque escurece a face, nao so a borda
            _btnText = b.GetComponentInChildren<Text>();
            _btnText.rectTransform.offsetMin = new Vector2(70f, 0f);   // espaco do martelo a esquerda
            Sprite ham = Art.Icon("melhoria_martelo", "icone");
            if (ham != null)
            {
                Image hi = Art.Node(b.transform, "Icone", new Vector2(0.06f, 0.08f), new Vector2(0.26f, 0.92f)).gameObject.AddComponent<Image>();
                hi.sprite = ham; hi.preserveAspect = true; hi.raycastTarget = false;
            }
            // badge vermelho (44-60 px) com o numero de cartoes compraveis, no canto de cima a direita do botao
            (_badge, _badgeText) = Badge(b.transform);
            // D-CORE-02: Metas no mesmo estilo, a direita; com ele visivel o Melhorias encolhe (0,18-0,60), entre os botoes de anuncio
            Button m = Art.NewButton(_bar, Loc.T("menu.metas"), 40, Art.HudEdge, new Vector2(0.62f, 0.08f), new Vector2(0.82f, 0.92f), metas);
            Text ml = m.GetComponentInChildren<Text>();
            ml.color = Art.HudEdge;
            ml.resizeTextForBestFit = true; ml.resizeTextMinSize = 24; ml.resizeTextMaxSize = 40;
            Image mf = Art.Panel(m.transform, "Face", Art.HudFace, Vector2.zero, Vector2.one);
            mf.rectTransform.offsetMin = new Vector2(5f, 5f); mf.rectTransform.offsetMax = new Vector2(-5f, -5f);
            mf.raycastTarget = false;
            mf.transform.SetAsFirstSibling();
            m.targetGraphic = mf;
            (_metasBadge, _metasBadgeText) = Badge(m.transform);
            _metas = m.gameObject;
            _metas.SetActive(false);

            _tray = Art.Panel(safe, "Fileira", Art.ComAlfa(Art.HudFace, 0.92f), new Vector2(0.02f, Band + 0.005f), new Vector2(0.98f, Band + Tray)).rectTransform;
            _scroll = _tray.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = true; _scroll.vertical = false; _scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform view = Art.Node(_tray, "Janela", Vector2.zero, Vector2.one);
            view.offsetMin = new Vector2(12f, 12f); view.offsetMax = new Vector2(-12f, -12f);
            view.gameObject.AddComponent<RectMask2D>();
            _content = Art.Node(view, "Cartoes", Vector2.zero, new Vector2(0f, 1f));
            _content.pivot = new Vector2(0f, 0.5f);
            var row = _content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 14f; row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = false; row.childForceExpandHeight = true;
            _content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.viewport = view; _scroll.content = _content;

            var menu = new List<int>();
            for (int u = 0; u < _sim.UpgradeDefs.Length; u++) if (_sim.UpgradeDefs[u].InMenu) menu.Add(u);   // C-CORE-01: os do distrito
            menu.Sort((a, c) => _sim.T.Cost(a).CompareTo(_sim.T.Cost(c)));
            for (int i = 0; i < menu.Count; i++) _cards.Add(BuildCard(_content, menu[i], i));   // Ord = posicao por preco, desempate dentro do estado
            for (int k = 0; k < sim.Equipe.Defs.Length; k++) _cards.Add(BuildCard(_content, -1, menu.Count + k, k));   // B-CORE-04: depois das melhorias
            _tray.gameObject.SetActive(false);
        }

        /// <summary>Disco vermelho com aro branco e numero, no canto de cima a direita do botao (escondido ate ter numero).</summary>
        static (RectTransform, Text) Badge(Transform button)
        {
            RectTransform r = Art.Node(button, "Badge", new Vector2(1f, 1f), new Vector2(1f, 1f));
            r.sizeDelta = new Vector2(62f, 62f); r.anchoredPosition = new Vector2(-14f, -10f);
            Image bd = r.gameObject.AddComponent<Image>();
            bd.sprite = Art.Disc(); bd.color = Art.Bad; bd.raycastTarget = false;
            Art.Aro(r);
            Text n = Art.Outlined(Art.NewText(r, "N", 36, Vector2.zero, Vector2.one), 1.5f);
            n.fontStyle = FontStyle.Bold;
            r.gameObject.SetActive(false);
            return (r, n);
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

        public bool IsOpen => _open;

        public void Toggle()
        {
            _open = !_open;
            _tray.gameObject.SetActive(_open);
            _t = 0f;
            if (!_open) return;
            _sort = true;   // abre com o compravel no comeco da fileira
            _scroll.StopMovement();
            _content.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Cartao: borda de couro (o chanfro e' a base 8 px mais grossa), face creme e, de cima para baixo, nome, icone, beneficio em %
        /// (2 linhas), antes → depois (ou "Faltam N" sem ouro) e botao de preco.
        /// </summary>
        Card BuildCard(RectTransform parent, int u, int ord, int w = -1)
        {
            UpgradeDef d = w < 0 ? _sim.UpgradeDefs[u] : null;
            var c = new Card { U = u, W = w, Ord = ord, Cost = w < 0 ? Textos.Ouro(_sim.T.Cost(u)) : "" };
            Image edge = Art.Panel(parent, w < 0 ? d.Name : _sim.Equipe.Defs[w].Id, Leather, Vector2.zero, Vector2.one);
            c.Go = edge.gameObject;
            c.Go.AddComponent<LayoutElement>().preferredWidth = CardW;
            c.Fade = c.Go.AddComponent<CanvasGroup>();
            Image face = Art.Panel(edge.transform, "Face", Cream, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(5f, 13f); face.rectTransform.offsetMax = new Vector2(-5f, -5f);
            face.raycastTarget = false;
            c.Toque = c.Go.AddComponent<Apertavel>();
            c.Toque.targetGraphic = edge;
            c.Toque.onClick.AddListener(() =>
            {
                long preco = w >= 0 ? _sim.Equipe.Custo(w) : 0;
                if (!(w >= 0 ? _sim.Equipe.Comprar(w) : _sim.TryBuyMenu((Upgrade)u))) return;
                if (w >= 0) _worker(w, preco); else _bought(u);
                _t = 0f; _sort = true;
            });
            c.Name = Art.NewText(face.transform, "Nome", 30, new Vector2(0.04f, 0.85f), new Vector2(0.96f, 0.98f));
            c.Name.fontStyle = FontStyle.Bold; c.Name.color = Brown;
            c.Name.resizeTextForBestFit = true; c.Name.resizeTextMinSize = 20; c.Name.resizeTextMaxSize = 30;
            c.Icon = Art.Node(face.transform, "Icone", new Vector2(0.18f, 0.58f), new Vector2(0.82f, 0.86f)).gameObject.AddComponent<Image>();
            c.Icon.preserveAspect = true; c.Icon.raycastTarget = false;
            c.Effect = Art.NewText(face.transform, "Beneficio", 30, new Vector2(0.03f, 0.37f), new Vector2(0.97f, 0.58f));
            c.Effect.fontStyle = FontStyle.Bold; c.Effect.color = Brown;
            c.Effect.resizeTextForBestFit = true; c.Effect.resizeTextMinSize = 18; c.Effect.resizeTextMaxSize = 30;
            c.Detail = Art.NewText(face.transform, "AntesDepois", 24, new Vector2(0.03f, 0.26f), new Vector2(0.97f, 0.37f));
            c.Detail.supportRichText = true; c.Detail.color = Leather;
            c.Detail.resizeTextForBestFit = true; c.Detail.resizeTextMinSize = 16; c.Detail.resizeTextMaxSize = 24;
            // "Faltam N" (so sem ouro) no lugar do antes → depois: pilula da HUD (borda creme, face marrom), texto e' o sinal, nao a cor
            Image tag = Art.Panel(face.transform, "Falta", Art.HudEdge, new Vector2(0.1f, 0.265f), new Vector2(0.9f, 0.365f));
            tag.raycastTarget = false;
            c.TagBox = tag.rectTransform;
            Image tagFace = Art.Panel(tag.transform, "Face", Art.HudFace, Vector2.zero, Vector2.one);
            tagFace.rectTransform.offsetMin = new Vector2(3f, 3f); tagFace.rectTransform.offsetMax = new Vector2(-3f, -3f);
            tagFace.raycastTarget = false;
            c.Tag = Art.NewText(tagFace.transform, "Valor", 24, Vector2.zero, Vector2.one);
            c.Tag.fontStyle = FontStyle.Bold; c.Tag.color = Art.HudEdge;
            c.Tag.resizeTextForBestFit = true; c.Tag.resizeTextMinSize = 14; c.Tag.resizeTextMaxSize = 24;
            // botao do preco: base escura (chanfro) + face verde/cinza, moeda a esquerda e valor com contorno
            c.Edge = Art.Panel(face.transform, "Preco", GreenDark, new Vector2(0.07f, 0.035f), new Vector2(0.93f, 0.25f));
            c.Edge.raycastTarget = false;
            c.Face = Art.Panel(c.Edge.transform, "Face", Green, Vector2.zero, Vector2.one);
            c.Face.rectTransform.offsetMin = new Vector2(0f, Raised);
            c.Face.raycastTarget = false;
            c.Coin = Art.Node(c.Face.transform, "Moeda", new Vector2(0.04f, 0.08f), new Vector2(0.34f, 0.92f)).gameObject.AddComponent<Image>();
            Sprite moeda = Art.Icon("moeda", "icone");
            c.Coin.sprite = moeda != null ? moeda : Art.Disc(); c.Coin.color = moeda != null ? Color.white : Art.Accent;
            c.Coin.preserveAspect = true; c.Coin.raycastTarget = false;
            c.Price = Art.Outlined(Art.NewText(c.Face.transform, "Valor", 36, new Vector2(0.3f, 0f), new Vector2(0.97f, 1f)), 2f);
            c.Price.fontStyle = FontStyle.Bold; c.Price.color = Color.white;
            c.Price.resizeTextForBestFit = true; c.Price.resizeTextMinSize = 16; c.Price.resizeTextMaxSize = 36;
            // best-fit so encolhe com Truncate (o NewText vem com Overflow); o texto do travado tem tamanho fixo (Mostra)
            c.Name.verticalOverflow = c.Effect.verticalOverflow = c.Detail.verticalOverflow = c.Tag.verticalOverflow = c.Price.verticalOverflow = VerticalWrapMode.Truncate;
            if (w >= 0) { Worker(c); return c; }
            c.Name.text = Textos.Nome(u);
            (c.Icon.sprite, c.Icon.color) = Icon(_sim.UpgradeDefs[u].Efeito);   // C-CORE-01: pelo efeito (Fole de couro = fole)
            c.Effect.text = Textos.Beneficio(_sim.T, u);
            c.Detail.text = Textos.AntesDepois(_sim.T, u).Replace("→", Arrow);
            if (d.Requires >= 0) c.Lock = Textos.Precisa(d.Requires);   // v0.6d: "Precisa: Balcão 5" (era "requer Balcão 5 vagas" em 2 linhas)
            return c;
        }

        /// <summary>B-CORE-04: textos do cartao do worker no nivel atual (refeitos quando ele sobe): nome proprio, retrato, o que a proxima
        /// compra faz, nivel antes → depois (ou o papel), preco e o requisito.</summary>
        void Worker(Card c)
        {
            WorkerDef d = _sim.Equipe.Defs[c.W];
            c.Nivel = _sim.Equipe.Nivel(c.W);
            c.Name.text = d.Nome;
            (c.Icon.sprite, c.Icon.color) = Retrato(d);
            c.Effect.text = Textos.BeneficioWorker(_sim, c.W);
            c.Detail.text = Textos.AntesDepoisWorker(_sim, c.W).Replace("→", Arrow);
            long custo = _sim.Equipe.Custo(c.W);
            c.Cost = custo >= 0 ? Textos.Ouro(custo) : "";
            c.Lock = Textos.PrecisaWorker(_sim, c.W);
        }

        /// <summary>Retrato do worker: 1o quadro do Idle de frente, na folha da skin com a escala do corpo no mundo (o quadro fica no cache da
        /// folha); sem folha, o disco dos ajudantes. O aviso da compra (Game) usa o mesmo.</summary>
        public static (Sprite, Color) Retrato(WorkerDef d)
        {
            if (!SpriteSheet.TryGet(d.Skin, out SpriteSheet sh)) return (Art.Disc(), Art.Worker);
            sh.Scale = WorldView.CharScale(d.Skin);
            return (sh.Frame("Idle", 0, 0f), Color.white);
        }

        /// <summary>Icone renderizado da melhoria (tools/ui_v05b_blender.py); sem a folha, a forma/cor procedural de antes.</summary>
        public static (Sprite, Color) Icon(Upgrade u)   // v0.6d: o aviso de compra do Game usa o mesmo icone
        {
            if ((int)u < 0) return (Art.Disc(), Art.Player);   // C-CORE-01: upgrade de distrito sem efeito generico
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
            UpgradeDef d = Upgrades.All[(int)u];
            if (d.Station != null) return ItemIcon(Array.Find(Recipes.All, r => r.Station == d.Station).Output);   // B-CORE-03: nivel de estacao = o produto da familia
            if (u == Upgrade.LongAnvil) return ItemIcon(Item.LongSword);   // B-CORE-07: a linha nova (aviso de compra do Game)
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

        /// <summary>Estado dos cartoes 4x/s (so reescreve o que mudou); a cada quadro o badge pulsa e, aberta, o toque e o respiro.</summary>
        public void Refresh(float dt)
        {
            if (_ready > 0) _badge.localScale = Vector3.one * (1f + 0.08f * Mathf.Abs(Mathf.Sin(Captura.Agora * 4f)));   // gravando: relogio do quadro
            if (_prontas > 0) _metasBadge.localScale = Vector3.one * (1f + 0.08f * Mathf.Abs(Mathf.Sin(Captura.Agora * 4f)));
            if (_open) Anima();
            _t -= dt;
            if (_t > 0f) return;
            _t = 0.25f;
            // D-CORE-02: revelacao progressiva (GDD §19, §3.3): o botao Metas so existe depois das Ferramentas
            bool metas = _raiz.Metas.Aberto;
            if (_metas.activeSelf != metas)
            {
                _metas.SetActive(metas);
                _melhorias.anchorMax = new Vector2(metas ? 0.6f : 0.82f, 0.92f);
            }
            int prontas = metas ? _raiz.Metas.Prontas() : 0;
            if (prontas != _prontas)
            {
                _prontas = prontas;
                _metasBadge.gameObject.SetActive(prontas > 0);
                _metasBadgeText.text = prontas.ToString();
            }
            int ready = 0;
            foreach (Card c in _cards)
            {
                if (c.W >= 0 && _sim.Equipe.Nivel(c.W) != c.Nivel) { Worker(c); c.Estado = (Cartao)(-1); }   // B-CORE-04: subiu de nivel (aqui ou no bot)
                Cartao e = c.W >= 0 ? Textos.EstadoWorker(_sim, c.W) : Textos.Estado(_sim, c.U);
                if (e == Cartao.Disponivel) ready++;
                if (e != c.Estado) Mostra(c, e);
                if (e != Cartao.SemOuro) continue;
                long f = (c.W >= 0 ? _sim.Equipe.Custo(c.W) : _sim.T.Cost(c.U)) - _sim.Gold;
                if (f != c.Falta) { c.Falta = f; c.Tag.text = Textos.Falta(f); }
            }
            if (_sort)
            {
                _sort = false;
                // estado, depois o preco de agora (o worker sobe de preco a cada nivel e entra entre as melhorias pelo custo), depois a ordem
                _cards.Sort((a, b) => a.Estado != b.Estado ? a.Estado.CompareTo(b.Estado)
                    : Preco(a) != Preco(b) ? Preco(a).CompareTo(Preco(b)) : a.Ord.CompareTo(b.Ord));
                for (int i = 0; i < _cards.Count; i++) _cards[i].Go.transform.SetSiblingIndex(i);
            }
            _ready = ready;
            _btn.color = ready > 0 ? Art.Accent : Art.HudFace;
            _btnText.color = ready > 0 ? Art.HudFace : Art.HudEdge;
            if (_badge.gameObject.activeSelf != ready > 0) _badge.gameObject.SetActive(ready > 0);
            _badgeText.text = ready.ToString();
        }

        long Preco(Card c) => c.W >= 0 ? _sim.Equipe.Custo(c.W) : _sim.T.Cost(c.U);

        /// <summary>Visual do estado, so quando ele muda. Comprado e oculto (nivel de estacao antes da revelacao) somem; travado esmaece e troca o
        /// preco por "Precisa: X".</summary>
        static void Mostra(Card c, Cartao e)
        {
            c.Estado = e;
            bool on = e != Cartao.Comprado && e != Cartao.Oculto;
            if (c.Go.activeSelf != on) c.Go.SetActive(on);
            if (!on) return;
            bool can = e == Cartao.Disponivel, locked = e == Cartao.Travado, poor = e == Cartao.SemOuro;
            c.Fade.alpha = locked ? 0.6f : 1f;
            c.Face.color = can ? Green : Gray;
            c.Edge.color = can ? GreenDark : GrayDark;
            c.Edge.rectTransform.localScale = Vector3.one;   // o respiro (Anima) so no disponivel
            c.Coin.enabled = !locked;
            c.Price.text = locked ? c.Lock : c.Cost;
            c.Price.rectTransform.anchorMin = new Vector2(locked ? 0.04f : 0.3f, 0f);
            // travado: 1 linha de 22 px sem best-fit (o best-fit preferia 2 linhas maiores)
            c.Price.resizeTextForBestFit = !locked;
            c.Price.fontSize = locked ? 22 : 36;
            c.Price.horizontalOverflow = locked ? HorizontalWrapMode.Overflow : HorizontalWrapMode.Wrap;
            c.Detail.enabled = !poor;
            c.TagBox.gameObject.SetActive(poor);
            c.Falta = -1;   // o Refresh reescreve o "Faltam N" logo depois
            Chanfro(c);
        }

        static void Chanfro(Card c) => c.Face.rectTransform.offsetMin = new Vector2(0f, c.Down ? 0f : c.Estado == Cartao.Disponivel ? Raised : Flat);

        /// <summary>A cada quadro com a fileira aberta: cartao apertado encolhe e o botao afunda; o preco do disponivel respira (2 Hz).</summary>
        void Anima()
        {
            float k = 1f + Breath * Mathf.Abs(Mathf.Sin(Captura.Agora * Mathf.PI * 2f));
            foreach (Card c in _cards)
            {
                if (c.Estado == Cartao.Comprado || c.Estado == Cartao.Oculto || (int)c.Estado < 0) continue;
                bool d = c.Toque.Apertado;
                if (d != c.Down)
                {
                    c.Down = d;
                    c.Go.transform.localScale = Vector3.one * (d ? PressScale : 1f);
                    Chanfro(c);
                }
                if (c.Estado == Cartao.Disponivel) c.Edge.rectTransform.localScale = Vector3.one * (d ? 1f : k);
            }
        }
    }
}
