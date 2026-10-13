using System;
using System.Collections.Generic;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// D-CORE-02: painel das metas, modal como as Configuracoes (escurece e segura os toques; a oficina segue, o joystick trava). De cima para
    /// baixo: titulo e ouro, faixa do login de 7 dias (toque no de hoje), abas Diarias / Semanais / Conquistas e a lista rolavel (frase da
    /// meta, premio, barra "x/y" e RESGATAR ou FEITO). Quem paga e grava o ID e' o Sim.Metas; aqui so desenha e avisa o Game (`pago`: tipo,
    /// id, ouro) para o som, o aviso, o diario e o save. Linhas montadas uma vez por aba; o estado repinta 4x/s so no que mudou.
    /// </summary>
    public sealed class PainelMetas
    {
        sealed class Linha
        {
            public int Slot = -1; public MetaDef Fixa;   // vaga (diaria/semanal) ou a conquista
            public MetaDef Def; public long Feito = -1; public int Estado = -1;   // ultimo pintado
            public GameObject Go, Barra; public Text Nome, Premio, Conta, BText; public Image Cheio, BFace; public Button B;
        }

        sealed class Dia { public Image Face; public Text Rotulo, Premio; public int Estado = -1; }

        static readonly Color Fundo = Art.Hex(0xF3DFB8), Tinta = MenuBar.Brown;
        const float LinhaPx = 165f;

        readonly Sim _sim;
        readonly Action<string, string, long> _pago;
        readonly RectTransform _root, _view;
        readonly ScrollRect _scroll;
        readonly RectTransform[] _listas = new RectTransform[3];
        readonly List<Linha>[] _linhas = { new List<Linha>(), new List<Linha>(), new List<Linha>() };
        readonly Image[] _abas = new Image[3], _pontos = new Image[3];
        readonly Text[] _abasTxt = new Text[3];
        readonly Dia[] _dias = new Dia[MetasDefs.LoginDias.Length];
        readonly Text _ouro, _login;
        int _aba = -1, _loginDias = -1; bool _loginPronto; long _ouroVisto = -1;
        float _t;

        public bool Aberto => _root.gameObject.activeSelf;

        public PainelMetas(RectTransform safe, Sim sim, Action<string, string, long> pago)
        {
            _sim = sim; _pago = pago;
            _root = Art.Node(safe, "Metas", Vector2.zero, Vector2.one);
            _root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            Image edge = Art.Panel(_root, "Cartao", MenuBar.Leather, new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.93f));
            Image face = Art.Panel(edge.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(6f, 14f); face.rectTransform.offsetMax = new Vector2(-6f, -6f);
            face.raycastTarget = false;
            Transform f = face.transform;

            Text title = Art.NewText(f, "Titulo", 58, new Vector2(0.05f, 0.925f), new Vector2(0.6f, 0.995f), TextAnchor.MiddleLeft);
            title.text = Loc.T("metas.titulo");
            title.fontStyle = FontStyle.Bold; title.color = Tinta;
            Image coin = Art.Node(f, "Moeda", new Vector2(0.6f, 0.935f), new Vector2(0.68f, 0.985f)).gameObject.AddComponent<Image>();
            Sprite moeda = Art.Icon("moeda", "icone");
            coin.sprite = moeda != null ? moeda : Art.Disc(); coin.color = moeda != null ? Color.white : Art.Accent;
            coin.preserveAspect = true; coin.raycastTarget = false;
            _ouro = Art.Outlined(Art.NewText(f, "Ouro", 44, new Vector2(0.69f, 0.925f), new Vector2(0.97f, 0.995f), TextAnchor.MiddleLeft), 2f);
            _ouro.fontStyle = FontStyle.Bold; _ouro.color = Art.Accent;

            // login de 7 dias: 7 caixas lado a lado; a de hoje fica verde e o toque resgata
            _login = Art.NewText(f, "Login", 28, new Vector2(0.04f, 0.875f), new Vector2(0.96f, 0.92f));
            _login.color = Art.ComAlfa(Tinta, 0.85f);
            _login.resizeTextForBestFit = true; _login.resizeTextMinSize = 18; _login.resizeTextMaxSize = 28;
            _login.verticalOverflow = VerticalWrapMode.Truncate;
            float w = 0.94f / _dias.Length;
            for (int i = 0; i < _dias.Length; i++)
            {
                float x = 0.03f + w * i;
                Image e = Art.Panel(f, "Dia" + (i + 1), MenuBar.Leather, new Vector2(x + 0.004f, 0.775f), new Vector2(x + w - 0.004f, 0.87f));
                var d = new Dia { Face = Art.Panel(e.transform, "Face", Fundo, Vector2.zero, Vector2.one) };
                d.Face.rectTransform.offsetMin = new Vector2(4f, 6f); d.Face.rectTransform.offsetMax = new Vector2(-4f, -4f);
                d.Face.raycastTarget = false;
                d.Rotulo = Art.NewText(e.transform, "Rotulo", 22, new Vector2(0.04f, 0.5f), new Vector2(0.96f, 0.95f));
                d.Rotulo.text = Loc.T("metas.login.dia", i + 1);
                d.Rotulo.fontStyle = FontStyle.Bold;
                d.Premio = Art.NewText(e.transform, "Premio", 22, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.5f));
                d.Premio.fontStyle = FontStyle.Bold;
                d.Premio.resizeTextForBestFit = true; d.Premio.resizeTextMinSize = 12; d.Premio.resizeTextMaxSize = 22;
                d.Premio.verticalOverflow = VerticalWrapMode.Truncate;
                Button b = e.gameObject.AddComponent<Button>();
                b.targetGraphic = e;
                b.onClick.AddListener(Login);
                _dias[i] = d;
            }

            string[] nomes = { "metas.diarias", "metas.semanais", "metas.conquistas" };
            for (int k = 0; k < 3; k++)
            {
                int aba = k;
                float x0 = 0.03f + 0.32f * k;
                Button b = Art.NewButton(f, Loc.T(nomes[k]), 34, MenuBar.Leather, new Vector2(x0, 0.69f), new Vector2(x0 + 0.3f, 0.76f), () => Aba(aba));
                _abas[k] = b.GetComponent<Image>();
                _abasTxt[k] = b.GetComponentInChildren<Text>();
                _abasTxt[k].resizeTextForBestFit = true; _abasTxt[k].resizeTextMinSize = 20; _abasTxt[k].resizeTextMaxSize = 34;
                _pontos[k] = Art.Node(b.transform, "Ponto", new Vector2(1f, 1f), new Vector2(1f, 1f)).gameObject.AddComponent<Image>();
                _pontos[k].rectTransform.sizeDelta = new Vector2(30f, 30f); _pontos[k].rectTransform.anchoredPosition = new Vector2(-6f, -6f);
                _pontos[k].sprite = Art.Disc(); _pontos[k].color = Art.Bad; _pontos[k].raycastTarget = false;
            }

            RectTransform box = Art.Node(f, "Lista", new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.68f));
            _scroll = box.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false; _scroll.vertical = true; _scroll.movementType = ScrollRect.MovementType.Clamped;
            _view = Art.Node(box, "Janela", Vector2.zero, Vector2.one);
            _view.gameObject.AddComponent<RectMask2D>();
            _view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // area de arrasto entre as linhas
            _scroll.viewport = _view;

            Button close = Art.NewButton(f, Loc.T("cfg.fechar"), 40, MenuBar.Leather, new Vector2(0.3f, 0.015f), new Vector2(0.7f, 0.1f), Fechar);
            close.GetComponentInChildren<Text>().color = MenuBar.Cream;
            _root.gameObject.SetActive(false);
        }

        /// <param name="aba">0 diarias, 1 semanais, 2 conquistas.</param>
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
            if (_listas[k] == null) Monta(k);
            for (int i = 0; i < 3; i++)
            {
                if (_listas[i] != null) _listas[i].gameObject.SetActive(i == k);
                _abas[i].color = i == k ? MenuBar.Leather : MenuBar.Gray;
                _abasTxt[i].color = i == k ? MenuBar.Cream : Tinta;
            }
            if (k != _aba) { _scroll.content = _listas[k]; _scroll.StopMovement(); _listas[k].anchoredPosition = Vector2.zero; }
            _aba = k;
            _t = 0f;
            Refresh(0f);
            if (k == 2) Ordena();
        }

        void Monta(int k)
        {
            RectTransform c = Art.Node(_view, "Aba" + k, new Vector2(0f, 1f), new Vector2(1f, 1f));
            c.pivot = new Vector2(0.5f, 1f);
            var col = c.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 12f; col.childControlWidth = col.childControlHeight = true; col.childForceExpandWidth = true; col.childForceExpandHeight = false;
            c.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listas[k] = c;
            if (k == 2) foreach (MetaDef d in MetasDefs.Conquistas) _linhas[k].Add(NovaLinha(c, -1, d));
            else for (int i = 0; i < (k == 0 ? Metas.NDiarias : Metas.NSemanais); i++) _linhas[k].Add(NovaLinha(c, i, null));
        }

        Linha NovaLinha(RectTransform parent, int slot, MetaDef fixa)
        {
            var l = new Linha { Slot = slot, Fixa = fixa };
            Image bg = Art.Panel(parent, "Linha", Fundo, Vector2.zero, Vector2.one);
            bg.raycastTarget = false;
            bg.gameObject.AddComponent<LayoutElement>().preferredHeight = LinhaPx;
            l.Go = bg.gameObject;
            Transform t = bg.transform;
            l.Nome = Art.NewText(t, "Nome", 34, new Vector2(0.03f, 0.55f), new Vector2(0.68f, 0.95f), TextAnchor.MiddleLeft);
            l.Nome.fontStyle = FontStyle.Bold; l.Nome.color = Tinta;
            l.Nome.resizeTextForBestFit = true; l.Nome.resizeTextMinSize = 22; l.Nome.resizeTextMaxSize = 34;
            l.Nome.verticalOverflow = VerticalWrapMode.Truncate;
            l.Premio = Art.NewText(t, "Premio", 24, new Vector2(0.03f, 0.31f), new Vector2(0.68f, 0.55f), TextAnchor.MiddleLeft);
            l.Premio.color = MenuBar.Leather;
            l.Premio.resizeTextForBestFit = true; l.Premio.resizeTextMinSize = 16; l.Premio.resizeTextMaxSize = 24;
            l.Premio.verticalOverflow = VerticalWrapMode.Truncate;
            Image bar = Art.Node(t, "Barra", new Vector2(0.03f, 0.08f), new Vector2(0.68f, 0.28f)).gameObject.AddComponent<Image>();
            bar.color = new Color(0f, 0f, 0f, 0.55f); bar.raycastTarget = false;
            l.Barra = bar.gameObject;
            l.Cheio = Art.Node(bar.transform, "Cheio", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            l.Cheio.color = Art.Good; l.Cheio.raycastTarget = false;
            l.Conta = Art.Outlined(Art.NewText(bar.transform, "Conta", 22, Vector2.zero, Vector2.one), 2f);
            l.Conta.fontStyle = FontStyle.Bold; l.Conta.horizontalOverflow = HorizontalWrapMode.Overflow;
            l.B = Art.NewButton(t, Loc.T("metas.resgatar"), 30, MenuBar.Green, new Vector2(0.71f, 0.2f), new Vector2(0.97f, 0.8f), () => Resgata(l));
            l.BFace = l.B.GetComponent<Image>();
            l.BText = l.B.GetComponentInChildren<Text>();
            l.BText.color = Color.white;
            l.BText.resizeTextForBestFit = true; l.BText.resizeTextMinSize = 18; l.BText.resizeTextMaxSize = 30;
            Art.Outlined(l.BText, 2f);
            return l;
        }

        Metas.Meta Le(Linha l) => l.Fixa != null ? _sim.Metas.Conquista(l.Fixa) : _aba == 0 ? _sim.Metas.Diaria(l.Slot) : _sim.Metas.Semanal(l.Slot);

        void Resgata(Linha l)
        {
            MetaDef def = Le(l).Def;
            long g = l.Fixa != null ? _sim.Metas.ResgatarConquista(l.Fixa.Id) : _aba == 0 ? _sim.Metas.ResgatarDiaria(l.Slot) : _sim.Metas.ResgatarSemanal(l.Slot);
            if (g < 0) return;
            _pago(l.Fixa != null ? "achievement" : _aba == 0 ? "daily" : "weekly", def.Id, g);
            Refresh(0f);
            if (_aba == 2) Ordena();
        }

        void Login()
        {
            long g = _sim.Metas.ResgatarLogin();
            if (g < 0) return;
            _pago("login", _sim.Metas.LoginDias == 0 ? "7" : _sim.Metas.LoginDias.ToString(), g);
            Refresh(0f);
        }

        /// <summary>Conquistas: as de resgatar primeiro, depois as em andamento, as feitas no fim (so ao abrir a aba e depois de um resgate).</summary>
        void Ordena()
        {
            List<Linha> ls = _linhas[2];
            var ordem = new List<Linha>(ls);
            ordem.Sort((a, b) => Peso(a).CompareTo(Peso(b)) != 0 ? Peso(a).CompareTo(Peso(b)) : ls.IndexOf(a).CompareTo(ls.IndexOf(b)));
            for (int i = 0; i < ordem.Count; i++) ordem[i].Go.transform.SetSiblingIndex(i);
        }

        static int Peso(Linha l) => l.Estado == 2 ? 0 : l.Estado == 3 ? 2 : 1;

        /// <summary>4x/s com o painel aberto (tempo real): ouro, login, pontos das abas e as linhas da aba aberta.</summary>
        public void Refresh(float dt)
        {
            if (!Aberto) return;
            _t -= dt;
            if (_t > 0f) return;
            _t = 0.25f;
            Metas m = _sim.Metas;
            if (_sim.Gold != _ouroVisto) { _ouroVisto = _sim.Gold; _ouro.text = Textos.Ouro(_sim.Gold); }
            if (m.LoginDias != _loginDias || m.LoginPronto != _loginPronto)
            {
                _loginDias = m.LoginDias; _loginPronto = m.LoginPronto;
                _login.text = Loc.T(_loginPronto ? "metas.login.pronto" : "metas.login.amanha");
                for (int i = 0; i < _dias.Length; i++) PintaDia(_dias[i], i);
            }
            int d = 0, w = 0, a = 0;
            for (int i = 0; i < Metas.NDiarias; i++) if (m.Diaria(i).Pronta) d++;
            for (int k = 0; k < Metas.NSemanais; k++) if (m.Semanal(k).Pronta) w++;
            foreach (MetaDef c in MetasDefs.Conquistas) if (m.Conquista(c).Pronta) a++;
            _pontos[0].enabled = d > 0; _pontos[1].enabled = w > 0; _pontos[2].enabled = a > 0;
            if (_aba >= 0) foreach (Linha l in _linhas[_aba]) Pinta(l, Le(l));
        }

        /// <summary>Caixa do dia `i` do ciclo: paga (cinza), a de hoje pronta (verde) ou por vir (creme); premio do dia no ouro de agora.</summary>
        void PintaDia(Dia d, int i)
        {
            int estado = i < _loginDias ? 2 : i == _loginDias && _loginPronto ? 1 : 0;
            LoginDia r = MetasDefs.LoginDias[i];
            d.Premio.text = r.OuroS > 0 ? "+" + Textos.Qtd(_sim.Metas.Ouro(r.OuroS)) : Loc.T("meta.premio.renome", r.Renome);
            if (estado == d.Estado) return;
            d.Estado = estado;
            d.Face.color = estado == 2 ? MenuBar.Gray : estado == 1 ? MenuBar.Green : Fundo;
            d.Rotulo.color = d.Premio.color = estado == 0 ? Tinta : Color.white;
        }

        void Pinta(Linha l, Metas.Meta m)
        {
            int estado = m.Def == null ? 0 : m.Paga ? 3 : m.Pronta ? 2 : 1;
            if (m.Def == l.Def && m.Feito == l.Feito && estado == l.Estado) return;
            l.Def = m.Def; l.Feito = m.Feito; l.Estado = estado;
            l.Barra.SetActive(m.Def != null);
            l.B.gameObject.SetActive(estado >= 2);
            if (m.Def == null)
            {
                l.Nome.text = Loc.T("metas.vazia");
                l.Premio.text = "";
                return;
            }
            l.Nome.text = Textos.Meta(m.Def, m.Alvo);
            l.Premio.text = Textos.Premio(m.Def.OuroS > 0 ? _sim.Metas.Ouro(m.Def.OuroS) : 0, m.Def.Xp, m.Def.Renome, m.Def.Insignia);
            l.Conta.text = Textos.Qtd(m.Feito) + "/" + Textos.Qtd(m.Alvo);
            l.Cheio.rectTransform.anchorMax = new Vector2(m.Alvo > 0 ? Mathf.Clamp01(m.Feito / (float)m.Alvo) : 0f, 1f);
            l.B.interactable = estado == 2;
            l.BFace.color = estado == 2 ? MenuBar.Green : MenuBar.Gray;
            l.BText.text = Loc.T(estado == 2 ? "metas.resgatar" : "metas.feito");
        }
    }
}
