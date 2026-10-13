using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// B-CORE-07: painel dos projetos (Blueprints), modal como o das metas (escurece e segura os toques; a oficina segue, o joystick trava). Titulo
    /// com "N de 19" e uma linha por Blueprint do catalogo, na ordem dele: o que e' (Textos.ProjetoNome), o que libera, de onde vem (com o
    /// progresso do marco) e o selo TEM / BLOQUEADO. So le o Sim.Projetos; os textos saem do Textos/Loc. Estado repinta 4x/s so no que mudou.
    /// ponytail: abre pela flag -projetos; o botao na HUD espera a decisao do lugar (a barra de baixo ja tem Melhorias e Metas). O aviso
    /// "Projeto novo!" avisa cada posse nova.
    /// </summary>
    public sealed class PainelProjetos
    {
        sealed class Linha
        {
            public BlueprintDef D; public bool? Tem; public string Fonte;
            public Text Fontes, Selo; public Image SeloFace;
        }

        static readonly Color Fundo = Art.Hex(0xF3DFB8), Tinta = MenuBar.Brown;
        const float LinhaPx = 190f;

        readonly Sim _sim;
        readonly RectTransform _root;
        readonly Text _conta;
        readonly Linha[] _linhas;
        int _vistos = -1;
        float _t;

        public bool Aberto => _root.gameObject.activeSelf;

        public PainelProjetos(RectTransform safe, Sim sim)
        {
            _sim = sim;
            _root = Art.Node(safe, "Projetos", Vector2.zero, Vector2.one);
            _root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            Image edge = Art.Panel(_root, "Cartao", MenuBar.Leather, new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.93f));
            Image face = Art.Panel(edge.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(6f, 14f); face.rectTransform.offsetMax = new Vector2(-6f, -6f);
            face.raycastTarget = false;
            Transform f = face.transform;

            Text title = Art.NewText(f, "Titulo", 58, new Vector2(0.05f, 0.925f), new Vector2(0.65f, 0.995f), TextAnchor.MiddleLeft);
            title.text = Loc.T("proj.titulo");
            title.fontStyle = FontStyle.Bold; title.color = Tinta;
            _conta = Art.NewText(f, "Conta", 40, new Vector2(0.65f, 0.925f), new Vector2(0.95f, 0.995f), TextAnchor.MiddleRight);
            _conta.fontStyle = FontStyle.Bold; _conta.color = MenuBar.Leather;

            RectTransform box = Art.Node(f, "Lista", new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.91f));
            var scroll = box.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform view = Art.Node(box, "Janela", Vector2.zero, Vector2.one);
            view.gameObject.AddComponent<RectMask2D>();
            view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // area de arrasto entre as linhas
            RectTransform c = Art.Node(view, "Linhas", new Vector2(0f, 1f), new Vector2(1f, 1f));
            c.pivot = new Vector2(0.5f, 1f);
            var col = c.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 12f; col.childControlWidth = col.childControlHeight = true; col.childForceExpandWidth = true; col.childForceExpandHeight = false;
            c.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = view; scroll.content = c;

            BlueprintDef[] defs = _sim.Projetos.Defs;
            _linhas = new Linha[defs.Length];
            for (int i = 0; i < defs.Length; i++) _linhas[i] = NovaLinha(c, defs[i]);

            Button close = Art.NewButton(f, Loc.T("cfg.fechar"), 40, MenuBar.Leather, new Vector2(0.3f, 0.015f), new Vector2(0.7f, 0.1f), Fechar);
            close.GetComponentInChildren<Text>().color = MenuBar.Cream;
            _root.gameObject.SetActive(false);
        }

        public void Abrir()
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _t = 0f;
            Refresh(0f);
        }

        public void Fechar() => _root.gameObject.SetActive(false);

        /// <summary>Nome e o que libera nao mudam (montados uma vez, no idioma da sessao); fonte e selo no Refresh.</summary>
        Linha NovaLinha(RectTransform parent, BlueprintDef d)
        {
            var l = new Linha { D = d };
            Image bg = Art.Panel(parent, "Linha", Fundo, Vector2.zero, Vector2.one);
            bg.raycastTarget = false;
            bg.gameObject.AddComponent<LayoutElement>().preferredHeight = LinhaPx;
            Transform t = bg.transform;
            Text nome = Art.NewText(t, "Nome", 32, new Vector2(0.03f, 0.64f), new Vector2(0.7f, 0.96f), TextAnchor.MiddleLeft);
            nome.text = Textos.ProjetoNome(_sim, d);
            nome.fontStyle = FontStyle.Bold; nome.color = Tinta;
            Text libera = Art.NewText(t, "Libera", 24, new Vector2(0.03f, 0.38f), new Vector2(0.97f, 0.64f), TextAnchor.MiddleLeft);
            libera.text = Textos.ProjetoLibera(_sim, d);
            libera.color = MenuBar.Leather;
            l.Fontes = Art.NewText(t, "Fonte", 22, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.38f), TextAnchor.MiddleLeft);
            l.Fontes.color = Art.ComAlfa(Tinta, 0.8f);
            foreach (Text x in new[] { nome, libera, l.Fontes })
            {
                x.resizeTextForBestFit = true; x.resizeTextMinSize = 14; x.resizeTextMaxSize = x.fontSize;
                x.verticalOverflow = VerticalWrapMode.Truncate;
            }
            l.SeloFace = Art.Panel(t, "Selo", MenuBar.Gray, new Vector2(0.72f, 0.66f), new Vector2(0.97f, 0.94f));
            l.SeloFace.raycastTarget = false;
            l.Selo = Art.Outlined(Art.NewText(l.SeloFace.transform, "Texto", 24, Vector2.zero, Vector2.one), 2f);
            l.Selo.fontStyle = FontStyle.Bold; l.Selo.color = Color.white;
            l.Selo.resizeTextForBestFit = true; l.Selo.resizeTextMinSize = 14; l.Selo.resizeTextMaxSize = 24;
            return l;
        }

        /// <summary>4x/s com o painel aberto (tempo real): contagem, progresso das fontes e o selo de cada linha.</summary>
        public void Refresh(float dt)
        {
            if (!Aberto) return;
            _t -= dt;
            if (_t > 0f) return;
            _t = 0.25f;
            Projetos p = _sim.Projetos;
            if (p.Count != _vistos) { _vistos = p.Count; _conta.text = Loc.T("proj.conta", p.Count, p.Defs.Length); }
            foreach (Linha l in _linhas)
            {
                bool tem = p.Tem(l.D.Id);
                string fonte = Textos.ProjetoFonte(_sim, l.D);
                if (fonte != l.Fonte) { l.Fonte = fonte; l.Fontes.text = fonte; }
                if (tem == l.Tem) continue;
                l.Tem = tem;
                l.SeloFace.color = tem ? MenuBar.Green : MenuBar.Gray;
                l.Selo.text = Loc.T(tem ? "proj.tem" : "proj.falta");
            }
        }
    }
}
