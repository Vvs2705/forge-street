using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FS.Core;
#if !FS_CORETESTS
using UnityEngine;
using UnityEngine.UI;
#endif

namespace FS
{
    // E-PLAT-04/05 (docs/IAP.md): a loja da plataforma atras de uma costura. Hoje so a LojaFalsa (build de desenvolvimento com -lojafalsa);
    // o Unity IAP entra como outra implementacao quando o pacote for baixado (aval do Vinicius). Sem loja (release sem SDK) o botao LOJA
    // nem aparece. A parte pura (IStore, LojaFalsa) o coretests compila e testa; o painel fica atras do #if.

    /// <summary>Loja da plataforma. Callbacks na thread principal. `recibo` = compra paga: nova, paga e nao confirmada de uma abertura anterior
    /// (a loja reentrega no Iniciar) ou restaurada; `falha`(produto, motivo) = recusada, cancelada ou "adiada" (pagamento pendente: o recibo
    /// vem depois). A loja e' tambem o validador do recibo dela (IValidaRecibo).</summary>
    public interface IStore : IValidaRecibo
    {
        void Iniciar(ProductDef[] produtos, Action<Recibo> recibo, Action<string, string> falha);
        /// <summary>Preco localizado que a loja devolveu ("R$ 14,90"); null = ainda nao carregou (o botao fica apagado).</summary>
        string Preco(string produto);
        void Comprar(string produto);
        /// <summary>Confirma (Google: acknowledge/consume) a transacao ja entregue e GRAVADA. Repetir e' inofensivo.</summary>
        void Confirmar(string tx);
        /// <summary>Reenvia pelo `recibo` os nao-consumiveis do jogador e chama `fim`(quantos).</summary>
        void Restaurar(Action<int> fim);
    }

    /// <summary>
    /// E-PLAT-04: loja FALSA de desenvolvimento (nunca no release: o Game so a cria com Debug.isDebugBuild e -lojafalsa). Guarda as compras
    /// como a conta da loja guardaria, num arquivo proprio (lojafalsa.txt no persistentDataPath; null = so memoria, -testsession): -reset apaga
    /// o save e nao a loja, entao "reinstalar" + RESTAURAR devolve o Pacote do Mestre. Assina cada recibo com HMAC-SHA256 de uma chave de DEV
    /// (o real e' a assinatura da Google, docs/IAP.md) sobre "fs-dev|app|produto|tx". Cenarios (-lojafalsa X): interrompe = paga e o recibo
    /// nao chega nesta sessao (o app "morreu" antes de gravar; chega no proximo Iniciar); duplica = o mesmo recibo 2x; recusa = falha.
    /// </summary>
    public sealed class LojaFalsa : IStore
    {
        public enum Cenario { Normal, Interrompe, Duplica, Recusa }
        public const string Arquivo = "lojafalsa.txt", App = "br.com.vstack.forgestreet";
        static readonly byte[] Chave = Encoding.UTF8.GetBytes("fs-lojafalsa-dev");   // ponytail: chave de DEV no codigo; so assina recibo de teste

        sealed class Compra { public string Tx, Produto; public bool Confirmada; }

        readonly string _arquivo;
        readonly Cenario _cenario;
        readonly List<Compra> _compras = new List<Compra>();
        Action<Recibo> _recibo;
        Action<string, string> _falha;

        public LojaFalsa(string arquivo, Cenario cenario = Cenario.Normal)
        {
            _arquivo = arquivo; _cenario = cenario;
            if (arquivo == null) return;
            try
            {
                if (!File.Exists(arquivo)) return;
                foreach (string l in File.ReadAllLines(arquivo))
                {
                    string[] p = l.Split(',');
                    if (p.Length == 3) _compras.Add(new Compra { Tx = p[0], Produto = p[1], Confirmada = p[2] == "1" });
                }
            }
            catch (Exception) { }   // arquivo de dev ilegivel = conta vazia
        }

        /// <summary>Valor da flag -lojafalsa: interrompe | duplica | recusa; outro (ou vazio) = normal.</summary>
        public static Cenario Ler(string flag) =>
            flag == "interrompe" ? Cenario.Interrompe : flag == "duplica" ? Cenario.Duplica : flag == "recusa" ? Cenario.Recusa : Cenario.Normal;

        /// <summary>Compras que a "conta" guarda e ainda nao foram confirmadas (o Unity IAP as reentrega na abertura).</summary>
        public int NaoConfirmadas => _compras.FindAll(c => !c.Confirmada).Count;

        public void Iniciar(ProductDef[] produtos, Action<Recibo> recibo, Action<string, string> falha)
        {
            _recibo = recibo; _falha = falha;
            foreach (Compra c in _compras.ToArray()) if (!c.Confirmada) _recibo(Assina(c.Tx, c.Produto));
        }

        public string Preco(string produto) => Compras.Def(produto)?.PrecoPlaceholder;

        public void Comprar(string produto)
        {
            ProductDef d = Compras.Def(produto);
            if (d == null || _cenario == Cenario.Recusa) { _falha(produto, d == null ? "produto_desconhecido" : "recusada"); return; }
            Compra c = d.Tipo == TipoProduto.Consumivel ? null : _compras.Find(x => x.Produto == produto);
            if (c == null)   // ja e' dele: como a Google (ITEM_ALREADY_OWNED), devolve o recibo que ja existe
            {
                _compras.Add(c = new Compra { Tx = "fake." + Guid.NewGuid().ToString("N").Substring(0, 12), Produto = produto });
                Grava();
                if (_cenario == Cenario.Interrompe) return;   // pago; o app "morre" antes de receber: o recibo volta no proximo Iniciar
            }
            Recibo r = Assina(c.Tx, produto);
            _recibo(r);
            if (_cenario == Cenario.Duplica) _recibo(r);
        }

        public void Confirmar(string tx)
        {
            Compra c = _compras.Find(x => x.Tx == tx);
            if (c == null || c.Confirmada) return;
            c.Confirmada = true;
            Grava();
        }

        public void Restaurar(Action<int> fim)
        {
            int n = 0;
            foreach (Compra c in _compras.ToArray())
            {
                ProductDef d = Compras.Def(c.Produto);
                if (d == null || d.Tipo == TipoProduto.Consumivel) continue;
                _recibo(Assina(c.Tx, c.Produto));
                n++;
            }
            fim(n);
        }

        public bool Valido(Recibo r) => r != null && r.Payload == Carga(App, r.Produto, r.Tx) && r.Assinatura == Hmac(r.Payload);

        /// <summary>Recibo assinado com a chave de dev (`app` diferente = recibo de outro app, que o Valido recusa).</summary>
        public static Recibo Assina(string tx, string produto, string app = App)
        {
            string carga = Carga(app, produto, tx);
            return new Recibo { Tx = tx, Produto = produto, Payload = carga, Assinatura = Hmac(carga) };
        }

        static string Carga(string app, string produto, string tx) => "fs-dev|" + app + "|" + produto + "|" + tx;

        static string Hmac(string s)
        {
            using (var h = new HMACSHA256(Chave))
                return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s ?? ""))).Replace("-", "").ToLowerInvariant();
        }

        void Grava()
        {
            if (_arquivo == null) return;
            var sb = new StringBuilder();
            foreach (Compra c in _compras) sb.Append(c.Tx).Append(',').Append(c.Produto).Append(',').Append(c.Confirmada ? '1' : '0').Append('\n');
            try { SaveStore.Write(_arquivo, sb.ToString()); }
            catch (Exception) { }   // disco de dev cheio: a compra fica so na memoria desta sessao
        }
    }

#if !FS_CORETESTS
    /// <summary>
    /// E-PLAT-04/05: painel da loja, modal como o das Metas (escurece e segura os toques; a oficina segue, o joystick trava). Um cartao por
    /// produto do catalogo com o conteudo FIXO listado antes de pagar e o botao com o preco que a LOJA devolveu (sem preco = loja carregando:
    /// botao apagado; ja comprado = COMPRADO); RESTAURAR COMPRAS embaixo; na loja falsa o rodape avisa que nada e' cobrado. Quem compra,
    /// grava e confirma e' o Game (`comprar`, `restaurar`); aqui so desenha. ponytail: 1 produto hoje; mais de 2 pedem a rolagem da Shop (E-UI-21).
    /// </summary>
    public sealed class PainelLoja
    {
        sealed class Linha { public ProductDef D; public Button B; public Image Face; public Text Rotulo; }

        static readonly Color Fundo = Art.Hex(0xF3DFB8);
        readonly Sim _sim;
        readonly IStore _loja;
        readonly RectTransform _root;
        readonly List<Linha> _linhas = new List<Linha>();

        public bool Aberto => _root.gameObject.activeSelf;

        public PainelLoja(RectTransform safe, Sim sim, IStore loja, Action<string> comprar, Action restaurar)
        {
            _sim = sim; _loja = loja;
            _root = Art.Node(safe, "Loja", Vector2.zero, Vector2.one);
            Art.Veu(_root.gameObject);
            Image edge = Art.Panel(_root, "Cartao", MenuBar.Leather, new Vector2(0.06f, 0.2f), new Vector2(0.94f, 0.8f));
            Image face = Art.Panel(edge.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(6f, 14f); face.rectTransform.offsetMax = new Vector2(-6f, -6f);
            face.raycastTarget = false;
            Transform f = face.transform;
            Text title = Art.NewText(f, "Titulo", 58, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
            title.text = Loc.T("loja.titulo"); title.fontStyle = FontStyle.Bold; title.color = MenuBar.Brown;
            Art.Cabecalho(title);
            ProductDef[] cat = Compras.AVenda(_sim.T);   // o premium do Pass so com o Tuning.PassePremium (LIVEOPS_V1 §3.5)
            float h = 0.5f / cat.Length;
            for (int i = 0; i < cat.Length; i++)
            {
                ProductDef d = cat[i];
                float top = 0.86f - h * i;
                Image bg = Art.Panel(f, d.Id, Fundo, new Vector2(0.04f, top - h + 0.01f), new Vector2(0.96f, top));
                bg.raycastTarget = false;
                // auditoria item 5 (loja vazia): a arte pintada do produto a esquerda (Resources/UI/<id>, icones_premio.py) e o selo do que ele e'
                Sprite arte = Art.UI(d.Id);
                float x0 = arte != null ? 0.27f : 0.04f;
                if (arte != null)
                {
                    Image img = Art.Node(bg.transform, "Arte", new Vector2(0.02f, 0.06f), new Vector2(0.25f, 0.94f)).gameObject.AddComponent<Image>();
                    img.sprite = arte; img.preserveAspect = true; img.raycastTarget = false;
                }
                // selo na base da arte (sem arte, no canto de cima): nao rouba a largura do nome
                Image selo = arte != null ? Art.Panel(bg.transform, "Selo", Art.Gold, new Vector2(0.01f, 0.02f), new Vector2(0.26f, 0.28f))
                    : Art.Panel(bg.transform, "Selo", Art.Gold, new Vector2(0.72f, 0.80f), new Vector2(0.985f, 0.97f));
                selo.raycastTarget = false;
                Text st = Art.Outlined(Art.NewText(selo.transform, "Texto", 26, Vector2.zero, Vector2.one), 2f);
                st.text = Loc.T(Array.IndexOf(Compras.CatalogoPasse, d) >= 0 ? "loja.selo.temporada" : "loja.selo.permanente");
                st.color = Color.white; st.resizeTextForBestFit = true; st.resizeTextMinSize = 18; st.resizeTextMaxSize = 30;
                Text nome = Art.NewText(bg.transform, "Nome", 44, new Vector2(x0, 0.78f), new Vector2(arte != null ? 0.96f : 0.71f, 0.97f), TextAnchor.MiddleLeft);
                string chave = "iap." + d.Id + ".nome";
                nome.text = Loc.T(chave); nome.fontStyle = FontStyle.Bold; nome.color = MenuBar.Brown;
                nome.resizeTextForBestFit = true; nome.resizeTextMinSize = 24; nome.resizeTextMaxSize = 44;
                nome.horizontalOverflow = HorizontalWrapMode.Wrap; nome.verticalOverflow = VerticalWrapMode.Truncate;
                Text itens = Art.NewText(bg.transform, "Conteudo", 30, new Vector2(x0 + 0.01f, 0.3f), new Vector2(0.96f, 0.76f), TextAnchor.UpperLeft);
                itens.text = Conteudo(d); itens.color = Art.ComAlfa(MenuBar.Brown, 0.85f);
                itens.resizeTextForBestFit = true; itens.resizeTextMinSize = 20; itens.resizeTextMaxSize = 30;
                itens.verticalOverflow = VerticalWrapMode.Truncate;
                var l = new Linha { D = d };
                l.B = Art.NewButton(bg.transform, "", 40, MenuBar.Green, new Vector2(Mathf.Max(0.25f, x0 + 0.04f), 0.04f), new Vector2(0.92f, 0.26f), () => comprar(d.Id));
                l.Face = l.B.GetComponent<Image>();
                l.Rotulo = Art.Outlined(l.B.GetComponentInChildren<Text>(), 2f);
                l.Rotulo.color = Color.white;
                l.Rotulo.resizeTextForBestFit = true; l.Rotulo.resizeTextMinSize = 24; l.Rotulo.resizeTextMaxSize = 40;
                _linhas.Add(l);
            }
            Button rest = Art.NewButton(f, Loc.T("loja.restaurar"), 36, MenuBar.Leather, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.31f), restaurar);
            rest.GetComponentInChildren<Text>().color = MenuBar.Cream;
            if (loja is LojaFalsa)
            {
                Text aviso = Art.NewText(f, "Teste", 24, new Vector2(0.05f, 0.125f), new Vector2(0.95f, 0.19f));
                aviso.text = Loc.T("loja.falsa"); aviso.color = Art.ComAlfa(MenuBar.Brown, 0.7f);
            }
            Button close = Art.NewButton(f, Loc.T("cfg.fechar"), 40, MenuBar.Leather, new Vector2(0.3f, 0.015f), new Vector2(0.7f, 0.11f), Fechar);
            close.GetComponentInChildren<Text>().color = MenuBar.Cream;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Uma linha "• texto" por item (Loc iap.posse.&lt;id&gt;). ponytail: Moeda (Wave E) ganha a linha "+N moeda" com a carteira.</summary>
        static string Conteudo(ProductDef d)
        {
            var sb = new StringBuilder();
            foreach (ItemCompra c in d.Conteudo) if (c.Tipo == TipoConteudo.Posse) sb.Append(sb.Length > 0 ? "\n• " : "• ").Append(Loc.T("iap.posse." + c.Id));
            return sb.ToString();
        }

        public void Abrir()
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Refresh();
        }

        public void Fechar() => _root.gameObject.SetActive(false);

        /// <summary>Cada quadro com o painel aberto: COMPRADO (nao-consumivel do jogador), "…" (compra em curso ou preco ainda nao veio) ou o
        /// preco da loja num botao verde.</summary>
        public void Refresh()
        {
            if (!Aberto) return;
            foreach (Linha l in _linhas)
            {
                bool tem = _sim.Compras.Possui(l.D.Id);
                string preco = _loja.Preco(l.D.Id);
                bool pode = !tem && preco != null && _sim.Compras.PodeComprar(l.D.Id);
                l.B.interactable = pode;
                l.Face.color = pode ? MenuBar.Green : MenuBar.Gray;
                l.Rotulo.text = tem ? Loc.T("loja.comprado") : pode ? preco : "…";
            }
        }
    }
#endif
}
