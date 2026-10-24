using System;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// B-CORE-08: o Hall das obras-primas e a Collection, modal como o de projetos (escurece e segura os toques; a oficina segue, o joystick
    /// trava). Abas "Obras-primas" (uma linha por MasterworkDef: nome, condicao com o progresso ou a data no Hall, o projeto e o selo NO HALL /
    /// PRONTA / BLOQUEADO; em cima, as familias completas com o +5%) e "Coleção" (o resumo das 4 abas e uma linha por marco: o que pede, o
    /// premio e FEITO ou o progresso). So le o Sim.Hall e o Sim.Colecao; os textos saem do Textos/Loc. Estado repinta 4x/s so no que mudou.
    /// Entrada: o 5o botao do painel de metas, ao lado de Projetos (aparece com a Collection, no 1o projeto de Masterwork); -hall [colecao].
    /// </summary>
    public sealed class PainelHall
    {
        sealed class Linha
        {
            public MasterworkDef Mw; public MarcoColecaoDef Marco;
            public Text Nome, Det1, Det2, Selo; public Image SeloFace;
            public string V1, V2, VS;
        }

        static readonly Color Fundo = Art.Hex(0xF3DFB8), Tinta = MenuBar.Brown;
        const float LinhaPx = 190f;

        readonly Sim _sim;
        readonly RectTransform _root, _view;
        readonly ScrollRect _scroll;
        readonly Text _conta, _resumo;
        readonly Image[] _abas = new Image[2];
        readonly Text[] _abasTxt = new Text[2];
        readonly RectTransform[] _listas = new RectTransform[2];
        readonly Linha[][] _linhas = new Linha[2][];
        int _aba = -1;
        string _contaV, _resumoV;
        float _t;

        public bool Aberto => _root.gameObject.activeSelf;

        public PainelHall(RectTransform safe, Sim sim)
        {
            _sim = sim;
            _root = Art.Node(safe, "Hall", Vector2.zero, Vector2.one);
            Art.Veu(_root.gameObject);
            Image edge = Art.Panel(_root, "Cartao", MenuBar.Leather, new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.93f));
            Image face = Art.Panel(edge.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(6f, 14f); face.rectTransform.offsetMax = new Vector2(-6f, -6f);
            face.raycastTarget = false;
            Transform f = face.transform;

            Text title = Art.NewText(f, "Titulo", 58, new Vector2(0.05f, 0.925f), new Vector2(0.65f, 0.995f), TextAnchor.MiddleLeft);
            title.text = Loc.T("hall.titulo");
            title.fontStyle = FontStyle.Bold; title.color = Tinta;
            Art.Cabecalho(title);
            _conta = Art.NewText(f, "Conta", 40, new Vector2(0.65f, 0.925f), new Vector2(0.95f, 0.995f), TextAnchor.MiddleRight);
            _conta.fontStyle = FontStyle.Bold; _conta.color = Art.Accent;   // na faixa de couro do cabecalho
            Art.Outlined(_conta, 2f);

            string[] nomes = { "hall.aba", "hall.colecao" };
            for (int k = 0; k < 2; k++)
            {
                int aba = k;
                float x0 = 0.03f + 0.48f * k;
                Button b = Art.NewButton(f, Loc.T(nomes[k]), 34, MenuBar.Leather, new Vector2(x0, 0.84f), new Vector2(x0 + 0.46f, 0.91f), () => Aba(aba));
                _abas[k] = b.GetComponent<Image>();
                _abasTxt[k] = b.GetComponentInChildren<Text>();
                _abasTxt[k].resizeTextForBestFit = true; _abasTxt[k].resizeTextMinSize = 20; _abasTxt[k].resizeTextMaxSize = 34;
            }
            _resumo = Art.NewText(f, "Resumo", 24, new Vector2(0.04f, 0.785f), new Vector2(0.96f, 0.835f));
            _resumo.color = Art.ComAlfa(Tinta, 0.85f);
            _resumo.resizeTextForBestFit = true; _resumo.resizeTextMinSize = 14; _resumo.resizeTextMaxSize = 24;
            _resumo.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform box = Art.Node(f, "Lista", new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.78f));
            _scroll = box.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false; _scroll.vertical = true; _scroll.movementType = ScrollRect.MovementType.Clamped;
            _view = Art.Node(box, "Janela", Vector2.zero, Vector2.one);
            _view.gameObject.AddComponent<RectMask2D>();
            _view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // area de arrasto entre as linhas
            _scroll.viewport = _view;
            for (int k = 0; k < 2; k++) Monta(k);

            Button close = Art.NewButton(f, Loc.T("cfg.fechar"), 40, MenuBar.Leather, new Vector2(0.3f, 0.015f), new Vector2(0.7f, 0.1f), Fechar);
            close.GetComponentInChildren<Text>().color = MenuBar.Cream;
            _root.gameObject.SetActive(false);
        }

        /// <summary>GDD §164 item 10 ("abrem a colecao sem dica"): o Game loga cada aba que o jogador passa a ver (0 obras-primas, 1 colecao).</summary>
        public Action<int> AbaVista;

        /// <param name="aba">0 obras-primas, 1 colecao.</param>
        public void Abrir(int aba = 0)
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _aba = -1;
            Aba(aba);
        }

        public void Fechar() => _root.gameObject.SetActive(false);

        void Aba(int k)
        {
            for (int i = 0; i < 2; i++)
            {
                _listas[i].gameObject.SetActive(i == k);
                _abas[i].color = i == k ? MenuBar.Leather : MenuBar.Gray;
                _abasTxt[i].color = i == k ? MenuBar.Cream : Tinta;
            }
            if (k != _aba) { _scroll.content = _listas[k]; _scroll.StopMovement(); _listas[k].anchoredPosition = Vector2.zero; AbaVista?.Invoke(k); }
            _aba = k;
            _contaV = _resumoV = null;
            _t = 0f;
            Refresh(0f);
        }

        void Monta(int k)
        {
            RectTransform c = Art.Node(_view, "Aba" + k, new Vector2(0f, 1f), new Vector2(1f, 1f));
            c.pivot = new Vector2(0.5f, 1f);
            var col = c.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 12f; col.childControlWidth = col.childControlHeight = true; col.childForceExpandWidth = true; col.childForceExpandHeight = false;
            c.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listas[k] = c;
            int n = k == 0 ? _sim.Hall.Defs.Length : _sim.Colecao.Marcos.Length;
            _linhas[k] = new Linha[n];
            for (int i = 0; i < n; i++)
                _linhas[k][i] = NovaLinha(c, k == 0 ? new Linha { Mw = _sim.Hall.Defs[i] } : new Linha { Marco = _sim.Colecao.Marcos[i] });
            c.gameObject.SetActive(false);
        }

        /// <summary>Nome (fixo, no idioma da sessao), 2 linhas de detalhe e o selo; o Refresh pinta o que muda.</summary>
        Linha NovaLinha(RectTransform parent, Linha l)
        {
            Image bg = Art.Panel(parent, "Linha", Fundo, Vector2.zero, Vector2.one);
            bg.raycastTarget = false;
            bg.gameObject.AddComponent<LayoutElement>().preferredHeight = LinhaPx;
            Transform t = bg.transform;
            l.Nome = Art.NewText(t, "Nome", 32, new Vector2(0.03f, 0.64f), new Vector2(0.7f, 0.96f), TextAnchor.MiddleLeft);
            l.Nome.text = l.Mw != null ? Textos.NomeMasterwork(l.Mw) : Textos.MarcoColecao(_sim, l.Marco);
            l.Nome.fontStyle = FontStyle.Bold; l.Nome.color = Tinta;
            l.Det1 = Art.NewText(t, "Detalhe", 24, new Vector2(0.03f, 0.36f), new Vector2(0.97f, 0.64f), TextAnchor.MiddleLeft);
            l.Det1.color = MenuBar.Leather;
            l.Det2 = Art.NewText(t, "Projeto", 22, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.36f), TextAnchor.MiddleLeft);
            l.Det2.color = Art.ComAlfa(Tinta, 0.8f);
            foreach (Text x in new[] { l.Nome, l.Det1, l.Det2 })
            {
                x.resizeTextForBestFit = true; x.resizeTextMinSize = 14; x.resizeTextMaxSize = x.fontSize;
                x.verticalOverflow = VerticalWrapMode.Truncate;
            }
            l.SeloFace = Art.Selo(t, new Vector2(0.68f, 0.66f), new Vector2(0.97f, 0.94f), out l.Selo);   // auditoria item 10: fita com icone
            return l;
        }

        /// <summary>4x/s com o painel aberto (tempo real): contagem, resumo e as linhas da aba aberta.</summary>
        public void Refresh(float dt)
        {
            if (!Aberto) return;
            _t -= dt;
            if (_t > 0f) return;
            _t = 0.25f;
            string conta = _aba == 0 ? Loc.T("proj.conta", _sim.Hall.Count, _sim.Hall.Defs.Length) : null;
            string resumo = _aba == 0 ? Textos.BonusHall(_sim) : Textos.ResumoColecao(_sim);
            if (_aba == 0 && resumo.Length == 0) resumo = Loc.T("hall.pronta.dica");
            if (_aba == 1) conta = Loc.T("proj.conta", Pagos(), _sim.Colecao.Marcos.Length);
            if (conta != _contaV) { _contaV = conta; _conta.text = conta; }
            if (resumo != _resumoV) { _resumoV = resumo; _resumo.text = resumo; }
            foreach (Linha l in _linhas[_aba]) Pinta(l);
        }

        int Pagos()
        {
            int n = 0;
            foreach (MarcoColecaoDef m in _sim.Colecao.Marcos) if (_sim.Colecao.Pago(m)) n++;
            return n;
        }

        void Pinta(Linha l)
        {
            string d1, d2, selo; Color cor; Sprite icone;
            if (l.Mw != null)
            {
                d1 = Textos.CondicaoMasterwork(_sim, l.Mw);
                d2 = Textos.ProjetoMasterwork(_sim, l.Mw);
                selo = Textos.SeloMasterwork(_sim, l.Mw);
                cor = _sim.Hall.Tem(l.Mw.Id) ? MenuBar.Green : _sim.Hall.Liberada(l.Mw) ? Art.Gold : MenuBar.Gray;
                icone = _sim.Hall.Tem(l.Mw.Id) ? Art.Check() : _sim.Hall.Liberada(l.Mw) ? Art.Star() : Art.Lock();
            }
            else
            {
                d1 = Textos.PremioColecao(l.Marco);
                d2 = Loc.T("col.dica." + (int)l.Marco.Aba);   // a dica da categoria do marco (era a mesma frase em toda linha)
                selo = Textos.SeloColecao(_sim, l.Marco);
                cor = _sim.Colecao.Pago(l.Marco) ? MenuBar.Green : MenuBar.Gray;
                icone = _sim.Colecao.Pago(l.Marco) ? Art.Check() : Art.Lock();
            }
            if (d1 != l.V1) { l.V1 = d1; l.Det1.text = d1; }
            if (d2 != l.V2) { l.V2 = d2; l.Det2.text = d2; }
            if (selo == l.VS) return;
            l.VS = selo;
            Art.PintaSelo(l.SeloFace, l.Selo, selo, cor, icone);
        }
    }
}
