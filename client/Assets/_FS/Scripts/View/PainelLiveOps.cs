using System;
using System.Collections.Generic;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// D-CORE-03/04: o LiveOps na HUD. Dois chips presos na borda de baixo da pilula da dica (como o selo do Renome na do ouro): a direita o do
    /// evento ativo ("1.000 pts · 3d 4h"), a esquerda o do Passe ("Passe 12", ponto vermelho com nivel para resgatar). O toque abre o cartao do
    /// evento (nome, tempo, pontos ate o proximo marco e o premio dele, e as missoes, que pagam sozinhas) ou o painel do Passe (temporada,
    /// nivel, XP e a trilha free com RESGATAR), modais como as Metas. Quem paga e grava e' o Sim.LiveOps; aqui so desenha e avisa o Game
    /// (`resgatou`: nivel, ouro) para o som, o aviso, o diario e o save. Repinta 4x/s; as linhas do Passe so quando o estado muda.
    /// </summary>
    public sealed class PainelLiveOps
    {
        sealed class LinhaMissao { public GameObject Go; public Text Nome, Conta, Pontos; public Image Cheio; }
        sealed class LinhaNivel { public GameObject Go; public RewardDef Premio; public Text Nome, Texto, BText; public Image BFace, Prem; public Button B; public int Estado = -1, EstPrem = -1; public long Ouro = -1; }

        static readonly Color Fundo = Art.Hex(0xF3DFB8), Tinta = MenuBar.Brown;
        const int MaxMissoes = LiveOpsDefs.MaxMissoes;   // o validator recusa evento com mais
        const float LinhaPx = 120f;

        readonly Sim _sim;
        readonly Action<int, long> _resgatou;
        readonly RectTransform _chipEv, _chipPs, _ev, _ps, _lista;
        readonly Text _chipEvTxt, _chipPsTxt, _evNome, _evTempo, _evConta, _evProx, _psTitulo, _psNivel, _psTempo, _psConta, _psFontes;
        readonly Image _chipPsPonto, _evCheio, _psCheio;
        readonly ScrollRect _scroll;
        readonly LinhaMissao[] _missoes = new LinhaMissao[MaxMissoes];
        readonly List<LinhaNivel> _niveis = new List<LinhaNivel>();
        PassDef _montado;
        float _t;

        public bool Aberto => _ev.gameObject.activeSelf || _ps.gameObject.activeSelf;

        /// <param name="top">Faixa de cima da HUD (Game.BuildHud): os chips vao na borda de baixo da pilula da dica (x 0,385..0,875).</param>
        public PainelLiveOps(RectTransform top, RectTransform safe, Sim sim, Action<int, long> resgatou)
        {
            _sim = sim; _resgatou = resgatou;
            (_chipEv, _chipEvTxt) = Chip(top, "ChipEvento", new Vector2(0.875f, 0f), new Vector2(1f, 0.5f), 290f, AbrirEvento);
            (_chipPs, _chipPsTxt) = Chip(top, "ChipPasse", new Vector2(0.385f, 0f), new Vector2(0f, 0.5f), 190f, AbrirPasse);
            _chipPsPonto = Art.Node(_chipPs, "Ponto", Vector2.one, Vector2.one).gameObject.AddComponent<Image>();
            _chipPsPonto.rectTransform.sizeDelta = new Vector2(26f, 26f); _chipPsPonto.rectTransform.anchoredPosition = new Vector2(-4f, -4f);
            _chipPsPonto.sprite = Art.Disc(); _chipPsPonto.color = Art.Bad; _chipPsPonto.raycastTarget = false;
            Art.Aro(_chipPsPonto.transform);

            // cartao do evento
            _ev = Modal(safe, "Evento", new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.88f), out Transform f);
            _evNome = Texto(f, "Nome", 50, 0.04f, 0.9f, 0.96f, 0.985f, TextAnchor.MiddleCenter, true);
            _evTempo = Texto(f, "Tempo", 28, 0.04f, 0.85f, 0.96f, 0.9f);
            _evCheio = Barra(f, 0.06f, 0.785f, 0.94f, 0.84f, Art.Gold, out _evConta);
            _evProx = Texto(f, "Proximo", 28, 0.04f, 0.72f, 0.96f, 0.78f);
            Text cab = Texto(f, "Cabecalho", 24, 0.04f, 0.665f, 0.96f, 0.71f);
            cab.text = Loc.T("liveops.missoes");
            for (int i = 0; i < MaxMissoes; i++)
            {
                float y1 = 0.655f - i * 0.063f;
                Image bg = Art.Panel(f, "Missao", Fundo, new Vector2(0.03f, y1 - 0.057f), new Vector2(0.97f, y1));
                bg.raycastTarget = false;
                var m = new LinhaMissao { Go = bg.gameObject };
                m.Nome = Texto(bg.transform, "Nome", 26, 0.02f, 0f, 0.6f, 1f, TextAnchor.MiddleLeft, true);
                m.Cheio = Barra(bg.transform, 0.62f, 0.2f, 0.85f, 0.8f, Art.Gold, out m.Conta);
                m.Pontos = Texto(bg.transform, "Pontos", 26, 0.86f, 0f, 0.99f, 1f, TextAnchor.MiddleCenter, true);
                m.Pontos.color = MenuBar.GreenDark;
                _missoes[i] = m;
            }
            Button passe = Art.NewButton(f, Loc.T("liveops.botao.passe"), 40, MenuBar.Green, new Vector2(0.06f, 0.025f), new Vector2(0.46f, 0.11f), () => { Fechar(); AbrirPasse(); });
            passe.GetComponentInChildren<Text>().color = Color.white;
            Button fecha = Art.NewButton(f, Loc.T("cfg.fechar"), 40, MenuBar.Leather, new Vector2(0.54f, 0.025f), new Vector2(0.94f, 0.11f), Fechar);
            fecha.GetComponentInChildren<Text>().color = MenuBar.Cream;

            // painel do Passe
            _ps = Modal(safe, "Passe", new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.93f), out Transform g);
            _psTitulo = Texto(g, "Titulo", 46, 0.04f, 0.925f, 0.96f, 0.995f, TextAnchor.MiddleCenter, true);
            Art.Cabecalho(_psTitulo);
            _psNivel = Texto(g, "Nivel", 36, 0.04f, 0.87f, 0.5f, 0.925f, TextAnchor.MiddleLeft, true);
            _psTempo = Texto(g, "Tempo", 26, 0.5f, 0.87f, 0.96f, 0.925f, TextAnchor.MiddleRight);
            _psCheio = Barra(g, 0.04f, 0.825f, 0.96f, 0.865f, Art.Gold, out _psConta);
            _psFontes = Texto(g, "Fontes", 22, 0.04f, 0.78f, 0.96f, 0.82f);
            RectTransform box = Art.Node(g, "Lista", new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.775f));
            _scroll = box.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false; _scroll.vertical = true; _scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform view = Art.Node(box, "Janela", Vector2.zero, Vector2.one);
            view.gameObject.AddComponent<RectMask2D>();
            view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // area de arrasto entre as linhas
            _scroll.viewport = view;
            _lista = Art.Node(view, "Niveis", new Vector2(0f, 1f), new Vector2(1f, 1f));
            _lista.pivot = new Vector2(0.5f, 1f);
            var col = _lista.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 10f; col.childControlWidth = col.childControlHeight = true; col.childForceExpandWidth = true; col.childForceExpandHeight = false;
            _lista.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.content = _lista;
            Button fechaPs = Art.NewButton(g, Loc.T("cfg.fechar"), 40, MenuBar.Leather, new Vector2(0.3f, 0.015f), new Vector2(0.7f, 0.1f), Fechar);
            fechaPs.GetComponentInChildren<Text>().color = MenuBar.Cream;
        }

        static (RectTransform, Text) Chip(RectTransform top, string nome, Vector2 ancora, Vector2 pivo, float largura, Action toque)
        {
            Image edge = Art.Panel(top, nome, Art.Gold, ancora, ancora);
            RectTransform r = edge.rectTransform;
            r.pivot = pivo; r.sizeDelta = new Vector2(largura, 46f); r.anchoredPosition = new Vector2(pivo.x > 0.5f ? -6f : 6f, 0f);
            Image face = Art.Panel(r, "Fundo", Art.HudFace, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(4f, 4f); face.rectTransform.offsetMax = new Vector2(-4f, -4f);
            face.raycastTarget = false;
            Button b = edge.gameObject.AddComponent<Button>();
            Art.Apertar(edge.gameObject);
            b.targetGraphic = edge;
            b.onClick.AddListener(() => { Sfx.Play("click"); toque(); });
            Image star = Art.Node(r, "Estrela", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).gameObject.AddComponent<Image>();
            star.rectTransform.sizeDelta = new Vector2(40f, 40f); star.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            star.sprite = Art.Star(); star.color = Art.Accent; star.raycastTarget = false;
            Text t = Art.NewText(r, "Texto", 24, Vector2.zero, Vector2.one, TextAnchor.MiddleLeft);
            t.rectTransform.offsetMin = new Vector2(46f, 0f); t.rectTransform.offsetMax = new Vector2(-8f, 0f);
            t.fontStyle = FontStyle.Bold; t.color = Art.Accent;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 14; t.resizeTextMaxSize = 24;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            r.gameObject.SetActive(false);
            return (r, t);
        }

        static RectTransform Modal(RectTransform safe, string nome, Vector2 min, Vector2 max, out Transform face)
        {
            RectTransform root = Art.Node(safe, nome, Vector2.zero, Vector2.one);
            Art.Veu(root.gameObject);   // escurece e segura os toques
            Image edge = Art.Panel(root, "Cartao", MenuBar.Leather, min, max);
            Image card = Art.Panel(edge.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            card.rectTransform.offsetMin = new Vector2(6f, 14f); card.rectTransform.offsetMax = new Vector2(-6f, -6f);   // chanfro de couro embaixo
            card.raycastTarget = false;
            face = card.transform;
            root.gameObject.SetActive(false);
            return root;
        }

        static Text Texto(Transform f, string nome, int size, float x0, float y0, float x1, float y1, TextAnchor a = TextAnchor.MiddleCenter, bool negrito = false)
        {
            Text t = Art.NewText(f, nome, size, new Vector2(x0, y0), new Vector2(x1, y1), a);
            t.color = Tinta;
            if (negrito) t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = Math.Max(12, size * 3 / 5); t.resizeTextMaxSize = size;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        static Image Barra(Transform f, float x0, float y0, float x1, float y1, Color cor, out Text conta)
        {
            return Art.Barra(f, new Vector2(x0, y0), new Vector2(x1, y1), cor, 24, out conta);
        }

        public void AbrirEvento()
        {
            if (_sim.LiveOps.Evento == null) return;
            _ev.gameObject.SetActive(true);
            _ev.SetAsLastSibling();
            PintaEvento();
        }

        /// <summary>DM §86 pass_view: o Game loga cada abertura da tela do Passe.</summary>
        public Action PasseAberto;

        public void AbrirPasse()
        {
            if (_sim.LiveOps.Passe == null) return;
            PasseAberto?.Invoke();
            _ps.gameObject.SetActive(true);
            _ps.SetAsLastSibling();
            _scroll.StopMovement(); _lista.anchoredPosition = Vector2.zero;
            PintaPasse();
        }

        public void Fechar() { _ev.gameObject.SetActive(false); _ps.gameObject.SetActive(false); }

        /// <summary>Todo quadro (Game.Update), repinta 4x/s: chips (evento ativo; Passe depois das Ferramentas) e o modal aberto.</summary>
        public void Refresh(float dt)
        {
            _t -= dt;
            if (_t > 0f) return;
            _t = 0.25f;
            LiveOps lo = _sim.LiveOps;
            _chipEv.gameObject.SetActive(lo.Evento != null);
            if (lo.Evento != null) _chipEvTxt.text = Loc.T("liveops.chip", Textos.Qtd(lo.Pontos), Textos.Tempo(lo.Restante));
            bool ps = lo.PasseVisivel;
            _chipPs.gameObject.SetActive(ps);
            if (ps) { _chipPsTxt.text = Loc.T("liveops.passe.chip", lo.Nivel); _chipPsPonto.enabled = lo.Prontos() > 0; }
            if (_ev.gameObject.activeSelf) PintaEvento();
            if (_ps.gameObject.activeSelf) PintaPasse();
        }

        void PintaEvento()
        {
            LiveOps lo = _sim.LiveOps;
            EventDef e = lo.Evento;
            if (e == null) { _ev.gameObject.SetActive(false); return; }   // acabou com o cartao aberto
            _evNome.text = Textos.NomeEvento(e);
            _evTempo.text = Loc.T("liveops.termina", Textos.Tempo(lo.Restante));
            int p = lo.Pontos, de = 0;
            Marco prox = lo.Proximo;
            foreach (Marco m in e.Trilha) if (m.Pontos <= p) de = m.Pontos;
            int ate = prox != null ? prox.Pontos : de;
            _evCheio.rectTransform.anchorMax = new Vector2(prox == null ? 1f : Mathf.Clamp01((p - de) / (float)Math.Max(1, ate - de)), 1f);
            _evConta.text = Loc.T("liveops.pontos", Textos.Qtd(p), Textos.Qtd(ate));
            _evProx.text = prox != null ? Loc.T("liveops.proximo", Textos.Premios(prox.Premios, _sim)) : Loc.T("liveops.completo");
            for (int i = 0; i < MaxMissoes; i++)
            {
                LinhaMissao m = _missoes[i];
                MissaoEvento x = i < e.Missoes.Length ? e.Missoes[i] : null;
                m.Go.SetActive(x != null);
                if (x == null) continue;
                bool paga = lo.Paga(x);
                long feito = paga ? x.Alvo : lo.Feito(x);
                m.Nome.text = Textos.Missao(x);
                m.Cheio.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(feito / (float)x.Alvo), 1f);
                m.Cheio.color = paga ? Art.Good : Art.Gold;
                m.Conta.text = paga ? Loc.T("metas.feito") : Textos.Qtd(feito) + "/" + Textos.Qtd(x.Alvo);
                m.Pontos.text = "+" + Textos.Qtd(x.Pontos);
            }
        }

        /// <summary>Uma linha por nivel da temporada (montadas uma vez; outra temporada remonta).</summary>
        void Monta(PassDef p)
        {
            foreach (LinhaNivel l in _niveis) UnityEngine.Object.Destroy(l.Go);
            _niveis.Clear();
            // LIVEOPS_V1 §3.5: a coluna premium so com a flag (desligada: so a trilha free, sem vitrine do que nao se compra)
            RewardDef[] prem = _sim.T.PassePremium ? p.Premium : null;
            for (int n = 1; n <= p.Niveis; n++) _niveis.Add(NovaLinha(n, p.Free[n - 1], prem?[n - 1]));
            _montado = p;
        }

        LinhaNivel NovaLinha(int n, RewardDef premio, RewardDef prem)
        {
            var l = new LinhaNivel { Premio = premio };
            Image bg = Art.Panel(_lista, "Nivel", Fundo, Vector2.zero, Vector2.one);
            bg.raycastTarget = false;
            bg.gameObject.AddComponent<LayoutElement>().preferredHeight = LinhaPx;
            l.Go = bg.gameObject;
            // interface v2: o icone do premio a esquerda (moeda, Renome, XP, cosmetico, projeto)
            Image ic = Art.Node(bg.transform, "Icone", new Vector2(0.02f, 0.14f), new Vector2(0.13f, 0.86f)).gameObject.AddComponent<Image>();
            ic.sprite = Art.IconePremio(premio.Tipo, out Color cor);
            ic.color = cor; ic.preserveAspect = true; ic.raycastTarget = false;
            float x1 = prem != null ? 0.53f : 0.68f;
            l.Nome = Texto(bg.transform, "Nome", 32, 0.15f, 0.5f, x1, 0.95f, TextAnchor.MiddleLeft, true);
            l.Nome.text = Loc.T("liveops.passe.nivel", n);
            l.Texto = Texto(bg.transform, "Premio", 26, 0.15f, 0.06f, x1, 0.5f, TextAnchor.MiddleLeft);
            if (prem != null)   // o premio premium do nivel: dourado quando ja pago (sozinho, LiveOps.PremiumAlcancado), apagado antes
            {
                Image moldura = Art.Panel(bg.transform, "Premium", Art.Gold, new Vector2(0.55f, 0.1f), new Vector2(0.68f, 0.9f));
                moldura.raycastTarget = false;
                l.Prem = Art.Node(moldura.transform, "Icone", new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f)).gameObject.AddComponent<Image>();
                l.Prem.sprite = Art.IconePremio(prem.Tipo, out Color pc);
                l.Prem.color = pc; l.Prem.preserveAspect = true; l.Prem.raycastTarget = false;
            }
            l.Texto.color = MenuBar.Leather;
            l.B = Art.NewButton(bg.transform, Loc.T("metas.resgatar"), 30, MenuBar.Green, new Vector2(0.71f, 0.15f), new Vector2(0.97f, 0.85f), () => Resgata(n));
            l.BFace = l.B.GetComponent<Image>();
            l.BText = l.B.GetComponentInChildren<Text>();
            l.BText.color = Color.white;
            l.BText.resizeTextForBestFit = true; l.BText.resizeTextMinSize = 18; l.BText.resizeTextMaxSize = 30;
            Art.Outlined(l.BText, 2f);
            return l;
        }

        void Resgata(int n)
        {
            long g = _sim.LiveOps.Resgatar(n);
            if (g < 0) return;
            _resgatou(n, g);
            PintaPasse();
        }

        void PintaPasse()
        {
            LiveOps lo = _sim.LiveOps;
            PassDef p = lo.Passe;
            if (p == null) { _ps.gameObject.SetActive(false); return; }
            if (_montado != p) Monta(p);
            int nv = lo.Nivel;
            bool acabou = lo.PasseEm(lo.Relogio) != p;
            _psTitulo.text = Loc.T("liveops.passe.titulo", Textos.NomePasse(p));
            _psNivel.text = Loc.T("liveops.passe.nivel", nv);
            _psTempo.text = acabou ? "" : Loc.T("liveops.termina", Textos.Tempo(Math.Max(0, p.Termina - lo.Relogio)));
            _psFontes.text = Loc.T(acabou ? "liveops.passe.fim" : "liveops.passe.fontes");
            bool max = nv >= p.Niveis;
            long base0 = (long)nv * p.XpNivel;
            _psCheio.rectTransform.anchorMax = new Vector2(max ? 1f : Mathf.Clamp01((lo.Xp - base0) / (float)p.XpNivel), 1f);
            _psConta.text = max ? Loc.T("liveops.passe.max") : Loc.T("liveops.passe.xp", Textos.Qtd(lo.Xp), Textos.Qtd(base0 + p.XpNivel));
            for (int n = 1; n <= _niveis.Count; n++)
            {
                LinhaNivel l = _niveis[n - 1];
                int estado = n > nv ? 0 : lo.Resgatado(n) ? 2 : 1;   // travado, RESGATAR, FEITO
                long ouro = l.Premio.Tipo == TipoPremio.Ouro ? _sim.Metas.Ouro(l.Premio.Valor) : 0;
                if (estado == l.Estado && ouro == l.Ouro) continue;
                l.Estado = estado; l.Ouro = ouro;
                l.Texto.text = Textos.Premios(new[] { l.Premio }, _sim);
                l.Nome.color = estado == 0 ? Art.ComAlfa(Tinta, 0.5f) : Tinta;
                l.B.gameObject.SetActive(estado > 0);
                l.B.interactable = estado == 1;
                l.BFace.color = estado == 1 ? MenuBar.Green : MenuBar.Gray;
                l.BText.text = Loc.T(estado == 1 ? "metas.resgatar" : "metas.feito");
            }
            for (int n = 1; n <= _niveis.Count; n++)
            {
                LinhaNivel l = _niveis[n - 1];
                if (l.Prem == null) continue;
                int ep = _sim.Metas.Ledger.Contains(LiveOps.TxPremium(p, n)) ? 1 : 0;
                if (ep == l.EstPrem) continue;
                l.EstPrem = ep;
                l.Prem.transform.parent.GetComponent<Image>().color = ep == 1 ? Art.Gold : Art.ComAlfa(MenuBar.Gray, 0.6f);
                l.Prem.color = ep == 1 ? Color.white : Art.ComAlfa(Color.white, 0.45f);
            }
        }
    }
}
