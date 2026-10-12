using System;
using System.Globalization;
#if !FS_CORETESTS
using UnityEngine;
using UnityEngine.UI;
#endif

namespace FS
{
    /// <summary>Faixa de idade da tela da 1a abertura (E-PLAT-01); a ordem do enum e' a ordem dos botoes.</summary>
    public enum Faixa { Menor13 = 1, De13a17 = 2, Adulto = 3 }

    /// <summary>
    /// E-PLAT-01 (Documento Mestre s94, GDD s17): as regras de privacidade do anuncio num lugar so, sem UnityEngine (o coretests testa no
    /// dotnet). Faixa x aceite -> o que o AdsLevelPlay manda ao LevelPlay ANTES do Init (tabela em docs/LEVELPLAY.md s6):
    /// menor de 13 = COPPA, sem ID de publicidade e so redes autocertificadas do Familias; 13 a 17 = nada personalizado e sem ID de
    /// publicidade (ECA Digital, Lei 15.211/2025: sem perfilamento de adolescente); 18 ou mais = personalizado so se aceitou.
    /// "Nao vender" (CCPA) sempre que nao for 18+ com aceite. A resposta fica no PlayerPrefs (Chave) com a versao do texto.
    /// </summary>
    public static class Privacidade
    {
        /// <summary>Versao do texto da tela: resposta gravada com outra versao nao vale e a tela pergunta de novo. Mudou o texto pt-BR? Suba
        /// a versao (o PrivacidadeTests trava o Texto pelo hash e falha ate subir). DM §109: en/es podem mudar sem pedir de novo.</summary>
        public const int Versao = 1;
        public const string Chave = "fs_privacidade";   // PlayerPrefs, fora do save de progresso (como as Configuracoes)

        /// <summary>Botao da faixa no idioma atual ("Menos de 13", "13 a 17", "18 ou mais").</summary>
        public static string NomeFaixa(Faixa f) => Loc.T("priv.faixa." + (int)f);

        /// <summary>Tudo o que o jogador le antes de responder, em pt-BR em qualquer idioma (o hash disto trava a Versao no teste).</summary>
        public static string Texto
        {
            get
            {
                static string P(string k) => Loc.Em(Idioma.Pt, k);
                return string.Join("\n", P("priv.idade.titulo"), P("priv.idade.corpo"), string.Join("|", P("priv.faixa.1"), P("priv.faixa.2"),
                    P("priv.faixa.3")), P("priv.cons.titulo"), P("priv.cons.corpo"), P("priv.aceitar"), P("priv.recusar"), P("priv.rodape"), P("priv.revisao"));
            }
        }

        /// <summary>Coppa = SetCOPPA; Consentimento = SetGDPRConsent; NaoVender = SetCCPA (do_not_sell); SemId = SetMetaData
        /// "is_deviceid_optout"; Familias = SetMetaData "Google_Family_Self_Certified_SDKS". Aceite fora do 18+ nao conta.</summary>
        public static (bool Coppa, bool Consentimento, bool NaoVender, bool SemId, bool Familias) Flags(Faixa f, bool aceitou)
        {
            bool personalizado = f == Faixa.Adulto && aceitou, crianca = f == Faixa.Menor13;
            return (crianca, personalizado, !personalizado, f != Faixa.Adulto, crianca);
        }

        /// <summary>"versao|faixa|aceite" (ex.: "1|3|0"); aceite fora do 18+ grava 0.</summary>
        public static string Gravar(Faixa f, bool aceitou) =>
            Versao.ToString(CultureInfo.InvariantCulture) + "|" + (int)f + "|" + (f == Faixa.Adulto && aceitou ? "1" : "0");

        /// <summary>false = perguntar de novo: sem resposta, versao de outro texto ou valor que nao abre.</summary>
        public static bool Ler(string s, out Faixa f, out bool aceitou)
        {
            f = Faixa.Menor13; aceitou = false;
            string[] p = (s ?? "").Split('|');
            if (p.Length != 3 || p[0] != Versao.ToString(CultureInfo.InvariantCulture) || (p[2] != "0" && p[2] != "1")) return false;
            if (!int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out int n) || n < 1 || n > 3) return false;
            f = (Faixa)n;
            aceitou = f == Faixa.Adulto && p[2] == "1";
            return true;
        }

        /// <summary>Rodape da revisao (Configuracoes > Privacidade).</summary>
        public static string Atual(Faixa f, bool aceitou) =>
            Loc.T("priv.revisao", Loc.T(Flags(f, aceitou).Consentimento ? "priv.atual.sim" : "priv.atual.nao", NomeFaixa(f)));

        /// <summary>Parametro "a" do consent_answer no diario (sem virgula nem dado pessoal).</summary>
        public static string Diario(Faixa f) => f == Faixa.Menor13 ? "<13" : f == Faixa.De13a17 ? "13-17" : "18+";
    }

#if !FS_CORETESTS
    /// <summary>
    /// E-PLAT-01: a tela, por cima de tudo na area segura, na paleta da HUD (borda HudEdge, face HudFace). Passo 1 = as 3 faixas como
    /// botoes iguais, uma embaixo da outra, na ordem da idade e nenhuma pre-marcada. "18 ou mais" vai ao passo 2 = Recusar e Aceitar lado a
    /// lado, mesmo tamanho, cor e fonte (sem dark pattern). Botao de baixo: VOLTAR no passo 2, CANCELAR na revisao (Configuracoes) e nada
    /// no passo 1 da 1a abertura (a pergunta e' obrigatoria). Quem grava e inicia o SDK e' o `done` (Game.Responder -> Ads.Responder).
    /// </summary>
    public sealed class TelaPrivacidade
    {
        readonly RectTransform _root;
        readonly Text _title, _body, _foot, _backLabel;
        readonly GameObject _idade, _cons, _back;
        readonly Action<Faixa, bool> _done;
        string _atual;   // null = 1a abertura; senao, o rodape da revisao
        bool _passo2;

        public bool Aberta => _root.gameObject.activeSelf;

        public TelaPrivacidade(RectTransform safe, Action<Faixa, bool> done)
        {
            _done = done;
            _root = Art.Node(safe, "Privacidade", Vector2.zero, Vector2.one);
            _root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);   // escurece e segura os toques do resto
            Image edge = Art.Panel(_root, "Cartao", Art.HudEdge, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.85f));
            Image face = Art.Panel(edge.transform, "Face", Art.HudFace, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(6f, 6f); face.rectTransform.offsetMax = new Vector2(-6f, -6f);
            face.raycastTarget = false;
            Transform f = face.transform;
            _title = Art.NewText(f, "Titulo", 56, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f));
            _title.fontStyle = FontStyle.Bold; _title.color = Art.HudEdge;
            _title.resizeTextForBestFit = true; _title.resizeTextMinSize = 40; _title.resizeTextMaxSize = 56;
            _title.verticalOverflow = VerticalWrapMode.Truncate;
            _body = Art.NewText(f, "Corpo", 34, new Vector2(0.07f, 0.64f), new Vector2(0.93f, 0.85f));
            _body.resizeTextForBestFit = true; _body.resizeTextMinSize = 24; _body.resizeTextMaxSize = 34;
            _body.verticalOverflow = VerticalWrapMode.Truncate;
            _idade = Art.Node(f, "Idade", Vector2.zero, Vector2.one).gameObject;
            for (int i = 0; i < 3; i++)
            {
                var fx = (Faixa)(i + 1);
                float y = 0.49f - 0.14f * i;
                Art.NewButton(_idade.transform, Privacidade.NomeFaixa(fx), 44, Art.HudEdge, new Vector2(0.15f, y), new Vector2(0.85f, y + 0.12f),
                    () => { if (fx == Faixa.Adulto) Passo(true); else Fim(fx, false); });
            }
            _cons = Art.Node(f, "Consentimento", Vector2.zero, Vector2.one).gameObject;
            Art.NewButton(_cons.transform, Loc.T("priv.recusar"), 44, Art.HudEdge, new Vector2(0.06f, 0.3f), new Vector2(0.48f, 0.44f), () => Fim(Faixa.Adulto, false));
            Art.NewButton(_cons.transform, Loc.T("priv.aceitar"), 44, Art.HudEdge, new Vector2(0.52f, 0.3f), new Vector2(0.94f, 0.44f), () => Fim(Faixa.Adulto, true));
            _foot = Art.NewText(f, "Rodape", 28, new Vector2(0.06f, 0.115f), new Vector2(0.94f, 0.2f));
            _foot.color = Art.ComAlfa(Art.Ink, 0.75f);
            _foot.resizeTextForBestFit = true; _foot.resizeTextMinSize = 20; _foot.resizeTextMaxSize = 28;
            _foot.verticalOverflow = VerticalWrapMode.Truncate;
            Button back = Art.NewButton(f, Loc.T("priv.voltar"), 36, MenuBar.Leather, new Vector2(0.3f, 0.015f), new Vector2(0.7f, 0.1f), Voltar);   // ~113 px: alvo de toque de 48 dp
            _backLabel = back.GetComponentInChildren<Text>();
            _backLabel.color = MenuBar.Cream;
            _back = back.gameObject;
            _root.gameObject.SetActive(false);
        }

        /// <param name="atual">null = 1a abertura (sem CANCELAR); senao o rodape da revisao (Privacidade.Atual).</param>
        /// <param name="passo2">dev (-privacidade 18): abre direto no consentimento, para a foto.</param>
        public void Abrir(string atual, bool passo2 = false)
        {
            _atual = atual;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Passo(passo2);
        }

        /// <summary>Voltar (botao de baixo e o voltar do Android): passo 2 -> passo 1; revisao fecha sem mudar; 1a abertura fica.</summary>
        public void Voltar()
        {
            if (_passo2) Passo(false);
            else if (_atual != null) _root.gameObject.SetActive(false);
        }

        void Passo(bool consentimento)
        {
            _passo2 = consentimento;
            _title.text = Loc.T(consentimento ? "priv.cons.titulo" : "priv.idade.titulo");
            _body.text = Loc.T(consentimento ? "priv.cons.corpo" : "priv.idade.corpo");
            _body.rectTransform.anchorMin = new Vector2(0.07f, consentimento ? 0.47f : 0.64f);   // o texto do consentimento e' mais longo
            _idade.SetActive(!consentimento);
            _cons.SetActive(consentimento);
            _foot.text = _atual ?? Loc.T("priv.rodape");
            _back.SetActive(consentimento || _atual != null);
            _backLabel.text = Loc.T(consentimento ? "priv.voltar" : "priv.cancelar");
        }

        void Fim(Faixa f, bool aceitou)
        {
            _root.gameObject.SetActive(false);
            _done(f, aceitou);
        }
    }
#endif
}
