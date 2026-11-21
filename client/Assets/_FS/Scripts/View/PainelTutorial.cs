using System;
using System.Globalization;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// FTUE (Tutorial.cs no nucleo): cartao de boas-vindas no centro (objetivo do jogo, COMECAR e "Pular tutorial"), painel do passo logo
    /// abaixo da faixa da HUD ("Passo N de 9", icone, texto curto, PULAR e, no ultimo passo, ENTENDI) e o cartao "o que e' isto" de cada
    /// sistema na 1a vez (um de cada vez, fecha com um toque). O painel do passo nao segura toque fora dos botoes (Em: o joystick ignora so
    /// eles); a boas-vindas e os cartoes sao modais (o Game para a oficina enquanto estao abertos, como na tela de idade, e o joystick trava).
    /// O texto da pilula da dica e a seta no mundo durante os passos saem do Game (Dica, Tutorial.Alvo + WorldView.ShowHint).
    /// Diario: tutorial_step (id, numero) a cada passo novo, tutorial_complete (s de jogo, passos) ao concluir jogando, tutorial_skip (id,
    /// numero) no Pular e tutorial_card (id) a cada cartao. Com `Bot` (-bot), COMECAR, ENTENDI e os cartoes se apertam sozinhos em 2 s.
    /// </summary>
    public sealed class PainelTutorial
    {
        /// <summary>Fracao da altura da area segura do painel do passo, logo abaixo da faixa da HUD (a camera reserva enquanto ele aparece).</summary>
        public const float Faixa = 0.09f;
        const float Auto = 2f;

        readonly Sim _raiz;
        readonly Action<string, string, string> _log;
        readonly RectTransform _passo, _modal, _boas, _cartao, _pularBtn, _okBtn, _texto;
        readonly Text _num, _corpo, _cTitulo, _cCorpo;
        readonly Image _icone;
        int _logado = -1;   // ultimo passo logado (-1 = nenhum nesta sessao)
        float _autoT;
        bool _cartaoNaTela;

        /// <summary>-bot: os toques do tutorial se apertam sozinhos.</summary>
        public bool Bot;
        public bool PainelVisivel => _passo.gameObject.activeSelf;
        /// <summary>Boas-vindas ou cartao na tela (modal: oficina parada, joystick travado).</summary>
        public bool Aberto => _modal.gameObject.activeSelf;

        /// <param name="safe">Area segura da HUD; `hudBand` = a faixa de cima (Game.HudBand): o painel do passo fica logo abaixo dela.</param>
        public PainelTutorial(RectTransform safe, float hudBand, Sim raiz, Action<string, string, string> log)
        {
            _raiz = raiz; _log = log;

            // ---------- painel do passo (no estilo das pilulas da HUD: aro de ouro e face de couro)
            _passo = Art.Node(safe, "Tutorial", new Vector2(0f, 1f - hudBand - Faixa), new Vector2(1f, 1f - hudBand - 0.004f));
            _passo.offsetMin = new Vector2(32f, 0f); _passo.offsetMax = new Vector2(-32f, 0f);
            Image edge = Art.Panel(_passo, "Borda", Art.HudEdge, Vector2.zero, Vector2.one);
            edge.raycastTarget = false;
            Image face = Art.Panel(edge.transform, "Fundo", Art.ComAlfa(Art.HudFace, 0.95f), Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(5f, 5f); face.rectTransform.offsetMax = new Vector2(-5f, -5f);
            face.raycastTarget = false;
            _icone = Art.Node(face.transform, "Icone", new Vector2(0.015f, 0.14f), new Vector2(0.135f, 0.86f)).gameObject.AddComponent<Image>();
            _icone.preserveAspect = true; _icone.raycastTarget = false;
            _num = Art.Outlined(Art.NewText(face.transform, "Numero", 26, new Vector2(0.15f, 0.64f), new Vector2(0.8f, 0.96f), TextAnchor.MiddleLeft), 1.5f);
            _num.fontStyle = FontStyle.Bold; _num.color = Art.Accent;
            _corpo = Art.Outlined(Art.NewText(face.transform, "Texto", 30, new Vector2(0.15f, 0.05f), new Vector2(0.8f, 0.66f), TextAnchor.MiddleLeft), 1.5f);
            _corpo.color = Art.Ink;
            _corpo.resizeTextForBestFit = true; _corpo.resizeTextMinSize = 20; _corpo.resizeTextMaxSize = 30;
            _corpo.verticalOverflow = VerticalWrapMode.Truncate;
            _texto = _corpo.rectTransform;
            Button pular = Art.NewButton(face.transform, Loc.T("tut.pular"), 28, MenuBar.Leather, new Vector2(0.815f, 0.1f), new Vector2(0.99f, 0.9f), Pular);
            Text pl = pular.GetComponentInChildren<Text>();
            pl.color = MenuBar.Cream;
            pl.resizeTextForBestFit = true; pl.resizeTextMinSize = 18; pl.resizeTextMaxSize = 28;
            _pularBtn = (RectTransform)pular.transform;
            Button ok = Art.NewButton(face.transform, Loc.T("tut.ok"), 28, Art.Gold, new Vector2(0.625f, 0.1f), new Vector2(0.8f, 0.9f), Confirmar);
            Text okl = ok.GetComponentInChildren<Text>();
            okl.resizeTextForBestFit = true; okl.resizeTextMinSize = 18; okl.resizeTextMaxSize = 28;
            _okBtn = (RectTransform)ok.transform;
            _passo.gameObject.SetActive(false);

            // ---------- modal: boas-vindas e cartao "o que e' isto". O veu segura os toques; tocar nele fecha o cartao (nao a boas-vindas)
            _modal = Art.Node(safe, "TutorialModal", Vector2.zero, Vector2.one);
            Art.Veu(_modal.gameObject);
            Button veu = _modal.gameObject.AddComponent<Button>();
            veu.transition = Selectable.Transition.None;
            veu.onClick.AddListener(() => { if (_cartaoNaTela) FecharCartao(); });

            Image box = Art.Panel(_modal, "BoasVindas", MenuBar.Leather, new Vector2(0.07f, 0.3f), new Vector2(0.93f, 0.7f));
            _boas = box.rectTransform;
            Image card = Art.Panel(box.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            card.rectTransform.offsetMin = new Vector2(6f, 14f); card.rectTransform.offsetMax = new Vector2(-6f, -6f);
            card.raycastTarget = false;
            Text titulo = Art.NewText(card.transform, "Titulo", 54, new Vector2(0.05f, 0.8f), new Vector2(0.95f, 0.97f));
            titulo.text = Loc.T("tut.boas.titulo");
            titulo.fontStyle = FontStyle.Bold;
            titulo.resizeTextForBestFit = true; titulo.resizeTextMinSize = 34; titulo.resizeTextMaxSize = 54;
            titulo.verticalOverflow = VerticalWrapMode.Truncate;
            Art.Cabecalho(titulo);
            Image star = Art.Node(card.transform, "Estrela", new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f)).gameObject.AddComponent<Image>();
            star.rectTransform.sizeDelta = new Vector2(96f, 96f);
            star.sprite = Art.Star(); star.color = Art.Gold; star.raycastTarget = false;
            Text corpo = Art.NewText(card.transform, "Corpo", 36, new Vector2(0.07f, 0.34f), new Vector2(0.93f, 0.6f));
            corpo.text = Loc.T("tut.boas.corpo");
            corpo.color = MenuBar.Brown;
            corpo.resizeTextForBestFit = true; corpo.resizeTextMinSize = 24; corpo.resizeTextMaxSize = 36;
            corpo.verticalOverflow = VerticalWrapMode.Truncate;
            Button comecar = Art.NewButton(card.transform, Loc.T("tut.comecar"), 48, MenuBar.Leather, new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.31f), Comecar);
            comecar.GetComponentInChildren<Text>().color = MenuBar.Cream;
            Button pularTudo = Art.NewButton(card.transform, Loc.T("tut.pular_tudo"), 30, MenuBar.Gray, new Vector2(0.28f, 0.02f), new Vector2(0.72f, 0.12f), Pular);
            pularTudo.GetComponentInChildren<Text>().color = MenuBar.Brown;

            Image cbox = Art.Panel(_modal, "Cartao", MenuBar.Leather, new Vector2(0.09f, 0.36f), new Vector2(0.91f, 0.64f));
            cbox.raycastTarget = false;   // o toque passa ao veu: fecha em qualquer lugar
            _cartao = cbox.rectTransform;
            Image cface = Art.Panel(cbox.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            cface.rectTransform.offsetMin = new Vector2(6f, 14f); cface.rectTransform.offsetMax = new Vector2(-6f, -6f);
            cface.raycastTarget = false;
            _cTitulo = Art.NewText(cface.transform, "Titulo", 48, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.96f));
            _cTitulo.fontStyle = FontStyle.Bold;
            _cTitulo.resizeTextForBestFit = true; _cTitulo.resizeTextMinSize = 30; _cTitulo.resizeTextMaxSize = 48;
            _cTitulo.verticalOverflow = VerticalWrapMode.Truncate;
            Art.Cabecalho(_cTitulo);
            _cCorpo = Art.NewText(cface.transform, "Corpo", 34, new Vector2(0.07f, 0.3f), new Vector2(0.93f, 0.7f));
            _cCorpo.color = MenuBar.Brown;
            _cCorpo.resizeTextForBestFit = true; _cCorpo.resizeTextMinSize = 22; _cCorpo.resizeTextMaxSize = 34;
            _cCorpo.verticalOverflow = VerticalWrapMode.Truncate;
            Button entendi = Art.NewButton(cface.transform, Loc.T("tut.ok"), 42, MenuBar.Leather, new Vector2(0.28f, 0.05f), new Vector2(0.72f, 0.25f), FecharCartao);
            entendi.GetComponentInChildren<Text>().color = MenuBar.Cream;
            _modal.gameObject.SetActive(false);
        }

        /// <summary>Poe o modal por cima dos paineis montados depois dele (Game.Awake, depois de todos); o painel do passo fica embaixo deles.</summary>
        public void PorCima() => _modal.SetAsLastSibling();

        /// <summary>
        /// Todo quadro: avanca o nucleo pelo distrito ativo, conclui o passo do menu quando `menuAberto`, loga, e mostra o que vale agora.
        /// `livre` = nenhum outro modal, anuncio ou tela de idade (so entao a boas-vindas ou um cartao novo aparece). Devolve true quando o
        /// painel do passo apareceu ou sumiu (o Game refaz o Fit: a camera reserva a faixa dele).
        /// </summary>
        public bool Refresh(Sim ativo, bool livre, bool menuAberto, float dt)
        {
            Tutorial t = _raiz.Tutorial;
            t.Atualiza(ativo);
            if (t.Passo == PassoTutorial.Menu && menuAberto) t.Confirmar();
            if ((int)t.Passo != _logado)
            {
                if (t.Ativo) _log("tutorial_step", t.Id, t.Numero.ToString(CultureInfo.InvariantCulture));
                else if (_logado >= 0 && !t.Pulou) _log("tutorial_complete", ativo.Time.ToString("0.0", CultureInfo.InvariantCulture), Tutorial.Total.ToString(CultureInfo.InvariantCulture));
                _logado = (int)t.Passo;
                Pinta(t);
            }
            bool boas = t.Passo == PassoTutorial.BoasVindas && (livre || Aberto);
            bool cartao = !t.Ativo && t.Cartao != null && (livre || _cartaoNaTela);
            if (cartao && !_cartaoNaTela && t.Cartao is Explica e)
            {
                _cTitulo.text = Loc.T("tut.cartao.titulo." + Tutorial.IdDe(e));
                _cCorpo.text = Loc.T("tut.cartao.corpo." + Tutorial.IdDe(e));
                _log("tutorial_card", Tutorial.IdDe(e), "");
            }
            _cartaoNaTela = cartao;
            if (_boas.gameObject.activeSelf != boas) _boas.gameObject.SetActive(boas);
            if (_cartao.gameObject.activeSelf != cartao) _cartao.gameObject.SetActive(cartao);
            if (Aberto != (boas || cartao)) { _modal.gameObject.SetActive(boas || cartao); if (boas || cartao) _modal.SetAsLastSibling(); }
            // -bot: COMECAR, ENTENDI e o cartao se apertam sozinhos (fotos e gravacao de cada passo)
            _autoT = Bot && (Aberto || t.Passo == PassoTutorial.Menu) ? _autoT + dt : 0f;
            if (_autoT >= Auto)
            {
                _autoT = 0f;
                if (boas) Comecar(); else if (cartao) FecharCartao(); else Confirmar();
            }
            bool painel = t.Ativo && t.Passo != PassoTutorial.BoasVindas;
            if (painel == PainelVisivel) return false;
            _passo.gameObject.SetActive(painel);
            return true;
        }

        /// <summary>Texto da pilula da dica no passo atual (null = fora dos passos: a dica de sempre).</summary>
        public string Dica()
        {
            Tutorial t = _raiz.Tutorial;
            return t.Ativo && t.Passo != PassoTutorial.BoasVindas ? Loc.T("tut.dica." + t.Id) : null;
        }

        /// <summary>O toque em `px` (tela) caiu num botao do painel do passo: o joystick nao abre.</summary>
        public bool Em(Vector2 px) =>
            PainelVisivel && (Dentro(_pularBtn, px) || _okBtn.gameObject.activeSelf && Dentro(_okBtn, px));

        /// <summary>fix/ui-fechar-ao-tocar-fora: o toque em `px` caiu no painel do passo (fundo ou botoes): nao e' "fora" (nao fecha a
        /// bandeja) nem do mundo (nao golpeia, nao escolhe receita).</summary>
        public bool NoPainel(Vector2 px) => PainelVisivel && Dentro(_passo, px);

        static bool Dentro(RectTransform r, Vector2 px) => RectTransformUtility.RectangleContainsScreenPoint(r, px, null);

        /// <summary>Voltar do Android: a boas-vindas comeca, o cartao fecha.</summary>
        public void Voltar() { if (_cartaoNaTela) FecharCartao(); else Comecar(); }

        void Pinta(Tutorial t)
        {
            if (!t.Ativo || t.Passo == PassoTutorial.BoasVindas) return;
            _num.text = Loc.T("tut.numero", t.Numero, Tutorial.Total);
            _corpo.text = Loc.T("tut.corpo." + t.Id);
            bool ok = t.Passo == PassoTutorial.Menu;   // o ultimo passo se fecha com ENTENDI (ou abrindo o menu)
            _okBtn.gameObject.SetActive(ok);
            _texto.anchorMax = new Vector2(ok ? 0.61f : 0.8f, 0.66f);
            Item it;
            switch (t.Passo)
            {
                case PassoTutorial.PegarMinerio: case PassoTutorial.Fornalha: it = Item.Ore; break;
                case PassoTutorial.PegarLingotes: case PassoTutorial.Bigorna: it = Item.Ingot; break;
                case PassoTutorial.PegarEspada: case PassoTutorial.Balcao: it = Item.Sword; break;
                default:
                    _icone.sprite = t.Passo == PassoTutorial.Andar ? Art.Ring() : Art.Star();
                    _icone.color = t.Passo == PassoTutorial.Andar ? MenuBar.Cream : Art.Gold;
                    return;
            }
            Sprite s = Art.ItemArt(it, true);
            _icone.sprite = s != null ? s : Art.ItemSprite(it);
            _icone.color = s != null ? Color.white : Art.ItemColor[(int)it];
        }

        void Comecar()
        {
            if (_raiz.Tutorial.Comecar()) _autoT = 0f;
        }

        void Confirmar()
        {
            _raiz.Tutorial.Confirmar();
            _autoT = 0f;
        }

        void Pular()
        {
            Tutorial t = _raiz.Tutorial;
            if (!t.Ativo) return;
            _log("tutorial_skip", t.Id, t.Numero.ToString(CultureInfo.InvariantCulture));
            t.Pular();
        }

        void FecharCartao()
        {
            _raiz.Tutorial.FecharCartao();
            _cartaoNaTela = false;
            _cartao.gameObject.SetActive(false);
            _modal.gameObject.SetActive(_boas.gameObject.activeSelf);
            _autoT = 0f;
        }
    }
}
