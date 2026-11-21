using System;
using FS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// 2026-11-21 (BALANCE §53): painel das Obras do Reino e dos Decretos da semana, modal como o dos projetos (escurece e segura os toques; a
    /// oficina segue). Em cima, o proximo degrau (Textos.ObraNome), o custo em ouro e em minutos de renda, o Renome dele, o Prestigio do Reino
    /// e o botao CONSTRUIR (verde com ouro; cinza com "Faltam X"). Embaixo, os 3 decretos com o progresso, o premio e o selo CUMPRIDO. So le o
    /// Imperio; constroi pelo Obras.Construir (o Game loga, grava e avisa). Repinta 4x/s com o painel aberto.
    /// ponytail: sem arte da Capital ainda (os props das obras ficam para a leva de arte); o botao OBRAS da HUD abre este painel.
    /// </summary>
    public sealed class PainelObras
    {
        sealed class Linha { public Text Nome, Prog; public Image SeloFace; public Text Selo; public bool? Pago; }

        static readonly Color Fundo = Art.Hex(0xF3DFB8), Tinta = MenuBar.Brown;

        readonly Imperio _imp;
        readonly Action<int> _construiu;
        readonly RectTransform _root;
        readonly Text _conta, _obra, _custo, _premio, _bonus, _vira, _btnTexto;
        readonly Button _btn;
        readonly Image _btnFace;
        readonly Linha[] _linhas = new Linha[Decretos.N];
        float _t;

        public bool Aberto => _root.gameObject.activeSelf;

        /// <summary>`construiu(degrau)`: depois de cada degrau pago (log, save e aviso no Game).</summary>
        public PainelObras(RectTransform safe, Imperio imp, Action<int> construiu)
        {
            _imp = imp; _construiu = construiu;
            _root = Art.Node(safe, "Obras", Vector2.zero, Vector2.one);
            Art.Veu(_root.gameObject);
            Image edge = Art.Panel(_root, "Cartao", MenuBar.Leather, new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.93f));
            Image face = Art.Panel(edge.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(6f, 14f); face.rectTransform.offsetMax = new Vector2(-6f, -6f);
            face.raycastTarget = false;
            Transform f = face.transform;

            Text title = Art.NewText(f, "Titulo", 58, new Vector2(0.05f, 0.925f), new Vector2(0.65f, 0.995f), TextAnchor.MiddleLeft);
            title.text = Loc.T("obras.titulo");
            title.fontStyle = FontStyle.Bold; title.color = Tinta;
            Art.Cabecalho(title);
            _conta = Art.NewText(f, "Conta", 34, new Vector2(0.6f, 0.925f), new Vector2(0.95f, 0.995f), TextAnchor.MiddleRight);
            _conta.fontStyle = FontStyle.Bold; _conta.color = Art.Accent;
            Art.Outlined(_conta, 2f);

            Image obra = Art.Panel(f, "Obra", Fundo, new Vector2(0.03f, 0.56f), new Vector2(0.97f, 0.905f));
            obra.raycastTarget = false;
            Transform o = obra.transform;
            _obra = Texto(o, "Nome", 38, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.97f), true);
            _custo = Texto(o, "Custo", 28, new Vector2(0.04f, 0.61f), new Vector2(0.96f, 0.78f), false);
            _premio = Texto(o, "Premio", 28, new Vector2(0.04f, 0.45f), new Vector2(0.96f, 0.61f), false);
            _bonus = Texto(o, "Bonus", 24, new Vector2(0.04f, 0.27f), new Vector2(0.96f, 0.45f), false);
            _btn = Art.NewButton(o, Loc.T("obras.construir"), 38, MenuBar.Green, new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.25f), Construir);
            _btnFace = _btn.GetComponent<Image>();
            _btnTexto = _btn.GetComponentInChildren<Text>();

            Text dt = Art.NewText(f, "Decretos", 40, new Vector2(0.05f, 0.49f), new Vector2(0.6f, 0.55f), TextAnchor.MiddleLeft);
            dt.text = Loc.T("dcr.titulo");
            dt.fontStyle = FontStyle.Bold; dt.color = Tinta;
            _vira = Texto(f, "Vira", 24, new Vector2(0.55f, 0.49f), new Vector2(0.95f, 0.55f), false);
            _vira.alignment = TextAnchor.MiddleRight;
            for (int k = 0; k < Decretos.N; k++)
            {
                float y1 = 0.48f - k * 0.12f, y0 = y1 - 0.11f;
                Image bg = Art.Panel(f, "Decreto" + k, Fundo, new Vector2(0.03f, y0), new Vector2(0.97f, y1));
                bg.raycastTarget = false;
                var l = new Linha
                {
                    Nome = Texto(bg.transform, "Nome", 28, new Vector2(0.03f, 0.5f), new Vector2(0.7f, 0.96f), true),
                    Prog = Texto(bg.transform, "Prog", 22, new Vector2(0.03f, 0.05f), new Vector2(0.7f, 0.5f), false),
                };
                l.SeloFace = Art.Selo(bg.transform, new Vector2(0.72f, 0.2f), new Vector2(0.97f, 0.8f), out l.Selo);
                _linhas[k] = l;
            }

            Button close = Art.NewButton(f, Loc.T("cfg.fechar"), 40, MenuBar.Leather, new Vector2(0.3f, 0.015f), new Vector2(0.7f, 0.1f), Fechar);
            close.GetComponentInChildren<Text>().color = MenuBar.Cream;
            _root.gameObject.SetActive(false);
        }

        static Text Texto(Transform t, string nome, int size, Vector2 min, Vector2 max, bool negrito)
        {
            Text x = Art.NewText(t, nome, size, min, max, TextAnchor.MiddleLeft);
            x.color = negrito ? Tinta : MenuBar.Leather;
            if (negrito) x.fontStyle = FontStyle.Bold;
            x.resizeTextForBestFit = true; x.resizeTextMinSize = 14; x.resizeTextMaxSize = size;
            x.verticalOverflow = VerticalWrapMode.Truncate;
            return x;
        }

        public void Abrir()
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _t = 0f;
            Refresh(0f);
        }

        public void Fechar() => _root.gameObject.SetActive(false);

        void Construir()
        {
            int n = _imp.Obras.Degraus;
            if (_imp.Obras.Construir(n)) _construiu?.Invoke(n);
            _t = 0f;
            Refresh(0f);
        }

        /// <summary>4x/s com o painel aberto: o degrau, o ouro que falta e os decretos.</summary>
        public void Refresh(float dt)
        {
            if (!Aberto) return;
            _t -= dt;
            if (_t > 0f) return;
            _t = 0.25f;
            Obras ob = _imp.Obras;
            int n = ob.Degraus;
            long custo = ob.Proximo, ouro = _imp.Ativo.Gold;
            _conta.text = Loc.P("obras.feitas", n, n);
            _obra.text = Textos.ObraNome(n);
            _custo.text = Loc.T("obras.custo", Textos.Ouro(custo), Obras.Minutos(n));
            _premio.text = Loc.T("obras.premio", Obras.RenomeDe(n));
            _bonus.text = Loc.T("obras.bonus", ob.BonusPct, Obras.BonusTeto);
            bool pode = ob.Aberta && ouro >= custo;
            _btn.interactable = pode;
            _btnFace.color = pode ? MenuBar.Green : MenuBar.Gray;
            _btnTexto.text = pode ? Loc.T("obras.construir") : Loc.T("obras.falta", Textos.Ouro(Math.Max(0, custo - ouro)));

            Decretos d = _imp.Decretos;
            _vira.text = d.Aberta ? Loc.T("dcr.vira", Textos.Tempo(d.Restante)) : "";
            for (int k = 0; k < Decretos.N; k++)
            {
                Linha l = _linhas[k];
                Decretos.Slot v = d.Aberta ? d.Slots[k] : null;
                l.Nome.text = Textos.Decreto(v);
                l.Prog.text = v == null ? "" : d.Feito(v) + "/" + v.Alvo + "  ·  " + Loc.T("dcr.premio", v.Def.Renome, v.Def.Xp);
                bool pago = v != null && v.Pago;
                l.SeloFace.gameObject.SetActive(v != null);
                if (pago == l.Pago) continue;
                l.Pago = pago;
                Art.PintaSelo(l.SeloFace, l.Selo, pago ? Loc.T("dcr.cumprido") : Loc.T("obras.falta", ""), pago ? MenuBar.Green : MenuBar.Gray, pago ? Art.Check() : Art.Lock());
            }
        }
    }
}
