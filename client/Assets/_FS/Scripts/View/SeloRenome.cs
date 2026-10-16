using System;
using System.Globalization;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// B-CORE-06 (GDD §19, DM §64): o Renome na HUD e' um SELO de nivel (estrela com o numero + nome), nunca um saldo: preso na borda de baixo
    /// da pilula de ouro, do lado direito, metade na faixa da HUD e metade na folga de 24 px que o balao do 1o da fila respeita (o "v0.x" do
    /// build dev fica a esquerda). Aparece no nivel 1 (revelacao progressiva, ~15-25 min). Toque abre o cartao (modal como as Metas): nivel,
    /// nome, barra ate o proximo com "total / limiar", o que o proximo libera (ou o que o ultimo deu) e de onde vem. Subiu de nivel = `subiu`
    /// (o Game faz o aviso "Renome N: nome!", som, vibracao e diario). Le o Sim.Renome a cada quadro, so repinta quando muda.
    /// </summary>
    public sealed class SeloRenome
    {
        static readonly Color Tinta = MenuBar.Brown;

        readonly Sim _sim;
        readonly Action<int> _subiu;
        readonly RectTransform _selo, _root;
        readonly Text _seloNum, _seloNome, _num, _titulo, _nome, _proximo, _conta, _libera;
        readonly Image _cheio;
        int _nivel = -1; long _total = -1;

        public bool Aberto => _root.gameObject.activeSelf;

        /// <param name="top">Faixa de cima da HUD (Game.BuildHud): o selo vai na borda de baixo da pilula de ouro (x 0,06..0,36).</param>
        public SeloRenome(RectTransform top, RectTransform safe, Sim sim, Action<int> subiu)
        {
            _sim = sim; _subiu = subiu;

            Image edge = Art.Panel(top, "SeloRenome", Art.Gold, new Vector2(0.36f, 0f), new Vector2(0.36f, 0f));
            _selo = edge.rectTransform;
            _selo.pivot = new Vector2(1f, 0.5f);
            _selo.sizeDelta = new Vector2(290f, 46f);
            _selo.anchoredPosition = new Vector2(-6f, 0f);
            Image face = Art.Panel(_selo, "Fundo", Art.HudFace, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(4f, 4f); face.rectTransform.offsetMax = new Vector2(-4f, -4f);
            face.raycastTarget = false;
            Button b = edge.gameObject.AddComponent<Button>();
            Art.Apertar(edge.gameObject);
            b.targetGraphic = edge;
            b.onClick.AddListener(() => { Sfx.Play("click"); Abrir(); });
            Image star = Art.Node(_selo, "Estrela", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).gameObject.AddComponent<Image>();
            star.rectTransform.sizeDelta = new Vector2(58f, 58f); star.rectTransform.anchoredPosition = new Vector2(24f, 2f);
            star.sprite = Art.Star(); star.color = Art.Gold; star.raycastTarget = false;
            _seloNum = Art.Outlined(Art.NewText(star.transform, "Nivel", 26, Vector2.zero, Vector2.one), 2f);
            _seloNum.fontStyle = FontStyle.Bold; _seloNum.horizontalOverflow = HorizontalWrapMode.Overflow;
            _seloNome = Art.NewText(_selo, "Nome", 26, new Vector2(0f, 0f), new Vector2(1f, 1f), TextAnchor.MiddleLeft);
            _seloNome.rectTransform.offsetMin = new Vector2(58f, 0f); _seloNome.rectTransform.offsetMax = new Vector2(-10f, 0f);
            _seloNome.fontStyle = FontStyle.Bold; _seloNome.color = Art.Accent;
            _seloNome.resizeTextForBestFit = true; _seloNome.resizeTextMinSize = 16; _seloNome.resizeTextMaxSize = 26;
            _seloNome.verticalOverflow = VerticalWrapMode.Truncate;
            _selo.gameObject.SetActive(false);

            // cartao modal: escurece e segura os toques (a oficina segue, o joystick trava: Game.Update)
            _root = Art.Node(safe, "Renome", Vector2.zero, Vector2.one);
            _root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            Image box = Art.Panel(_root, "Cartao", MenuBar.Leather, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.72f));
            Image card = Art.Panel(box.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            card.rectTransform.offsetMin = new Vector2(6f, 14f); card.rectTransform.offsetMax = new Vector2(-6f, -6f);
            card.raycastTarget = false;
            Transform f = card.transform;
            Image medal = Art.Node(f, "Estrela", new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f)).gameObject.AddComponent<Image>();
            medal.rectTransform.sizeDelta = new Vector2(150f, 150f);
            medal.sprite = Art.Star(); medal.color = Art.Gold; medal.raycastTarget = false;
            _num = Art.Outlined(Art.NewText(medal.transform, "Nivel", 60, Vector2.zero, Vector2.one), 3f);
            _num.fontStyle = FontStyle.Bold; _num.horizontalOverflow = HorizontalWrapMode.Overflow;
            _titulo = Art.NewText(f, "Titulo", 30, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.75f));
            _titulo.color = Art.ComAlfa(Tinta, 0.75f);
            _nome = Art.NewText(f, "Nome", 54, new Vector2(0.04f, 0.57f), new Vector2(0.96f, 0.69f));
            _nome.fontStyle = FontStyle.Bold; _nome.color = Tinta;
            _nome.resizeTextForBestFit = true; _nome.resizeTextMinSize = 30; _nome.resizeTextMaxSize = 54;
            _nome.verticalOverflow = VerticalWrapMode.Truncate;
            _proximo = Art.NewText(f, "Proximo", 30, new Vector2(0.06f, 0.49f), new Vector2(0.94f, 0.56f), TextAnchor.MiddleLeft);
            _proximo.color = Tinta;
            _proximo.resizeTextForBestFit = true; _proximo.resizeTextMinSize = 20; _proximo.resizeTextMaxSize = 30;
            _proximo.verticalOverflow = VerticalWrapMode.Truncate;
            Image bar = Art.Node(f, "Barra", new Vector2(0.06f, 0.41f), new Vector2(0.94f, 0.48f)).gameObject.AddComponent<Image>();
            bar.color = new Color(0f, 0f, 0f, 0.55f); bar.raycastTarget = false;
            _cheio = Art.Node(bar.transform, "Cheio", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            _cheio.color = Art.Gold; _cheio.raycastTarget = false;
            _conta = Art.Outlined(Art.NewText(bar.transform, "Conta", 28, Vector2.zero, Vector2.one), 2f);
            _conta.fontStyle = FontStyle.Bold; _conta.horizontalOverflow = HorizontalWrapMode.Overflow;
            _libera = Art.NewText(f, "Libera", 30, new Vector2(0.06f, 0.21f), new Vector2(0.94f, 0.39f), TextAnchor.UpperLeft);
            _libera.color = Tinta;
            _libera.resizeTextForBestFit = true; _libera.resizeTextMinSize = 20; _libera.resizeTextMaxSize = 30;
            _libera.verticalOverflow = VerticalWrapMode.Truncate;
            Text fontes = Art.NewText(f, "Fontes", 24, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.21f));
            fontes.text = Loc.T("renome.fontes");
            fontes.color = Art.ComAlfa(Tinta, 0.7f);
            fontes.resizeTextForBestFit = true; fontes.resizeTextMinSize = 16; fontes.resizeTextMaxSize = 24;
            fontes.verticalOverflow = VerticalWrapMode.Truncate;
            Button close = Art.NewButton(f, Loc.T("cfg.fechar"), 40, MenuBar.Leather, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.12f), Fechar);
            close.GetComponentInChildren<Text>().color = MenuBar.Cream;
            _root.gameObject.SetActive(false);
        }

        public void Abrir()
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _total = -1;
            Refresh();
        }

        public void Fechar() => _root.gameObject.SetActive(false);

        /// <summary>Todo quadro (Game.RefreshHud). Nivel novo: selo (aparece no 1) e, se subiu depois do 1o quadro, `subiu` (o save e o -renome
        /// de antes do 1o quadro nao viram aviso). Cartao aberto: repinta quando o total muda.</summary>
        public void Refresh()
        {
            Renome r = _sim.Renome;
            int n = r.Nivel;
            if (n != _nivel)
            {
                if (_nivel >= 0 && n > _nivel) _subiu(n);
                _nivel = n;
                _selo.gameObject.SetActive(n >= 1);
                _seloNum.text = n.ToString(CultureInfo.InvariantCulture);
                _seloNome.text = Textos.NomeRenome(n);
                _total = -1;
            }
            if (!Aberto || r.Total == _total) return;
            _total = r.Total;
            RenomeDef prox = r.Proximo;
            _num.text = n.ToString(CultureInfo.InvariantCulture);
            _titulo.text = Loc.T("renome.titulo", n);
            _nome.text = Textos.NomeRenome(n);
            _proximo.text = prox != null ? Loc.T("renome.proximo", Textos.NomeRenome(prox.Nivel)) : Loc.T("renome.max");
            long de = r.Atual.Limiar, ate = prox != null ? prox.Limiar : de;
            _cheio.rectTransform.anchorMax = new Vector2(prox != null ? Mathf.Clamp01((float)((r.Total - de) / (double)(ate - de))) : 1f, 1f);
            _conta.text = Textos.Qtd(r.Total) + (prox != null ? " / " + Textos.Qtd(ate) : "");
            _libera.text = prox != null ? Loc.T("renome.libera", Textos.LiberaRenome(prox.Nivel)) : Loc.T("renome.liberou", Textos.LiberaRenome(n));
        }
    }
}
