using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FS.Core
{
    /// <summary>E-PLAT-04: tipo do produto na loja. Assinatura existe no dado, mas o validator recusa (fora da v1.0, DM §43).</summary>
    public enum TipoProduto { Consumivel, NaoConsumivel, Assinatura }

    /// <summary>O que uma compra entrega. Nao existe Bau, Aleatorio nem Chance: "nada aleatorio pago" (V4, ECA Digital) e' estrutural.
    /// Posse = desbloqueio permanente, re-derivado do recibo; Moeda = quantidade fixa de uma moeda A VENDA (nenhuma ate a Wave E, V9).</summary>
    public enum TipoConteudo { Posse, Moeda }

    public struct ItemCompra
    {
        public TipoConteudo Tipo; public string Id; public long Qtd;
        public static ItemCompra Posse(string id) => new ItemCompra { Tipo = TipoConteudo.Posse, Id = id, Qtd = 1 };
    }

    /// <summary>E-PLAT-04/05: produto em dados. Id estavel com prefixo iap_ (= o ID no Play Console; nunca renomear). Conteudo fixo, que a
    /// loja mostra antes de pagar. PrecoPlaceholder: so a loja falsa e o doc usam; a tela mostra o preco que a LOJA devolve (preco real =
    /// decisao da Wave E).</summary>
    public sealed class ProductDef
    {
        public string Id; public TipoProduto Tipo; public ItemCompra[] Conteudo; public string PrecoPlaceholder;
    }

    /// <summary>Recibo da loja: ID de transacao (Google: purchaseToken; loja falsa: fake.*), produto, a carga assinada e a assinatura.</summary>
    public sealed class Recibo { public string Tx, Produto, Payload, Assinatura; }

    /// <summary>Ponto de validacao do recibo (DM §93): a loja falsa confere a assinatura de dev; o Unity IAP, a da Google (CrossPlatformValidator)
    /// ou um servidor (docs/IAP.md). Recibo que nao passa nao entrega nem vira posse.</summary>
    public interface IValidaRecibo { bool Valido(Recibo r); }

    /// <summary>Uma transacao. Iniciada (do toque ate a loja responder) fica so na memoria (Compras.Iniciada); Pendente = entregue no estado,
    /// o save ainda nao gravou; Entregue = gravado, falta confirmar a loja; Confirmada = a loja ja sabe.</summary>
    public enum EstadoCompra { Iniciada, Pendente, Entregue, Confirmada }

    /// <summary>
    /// E-PLAT-04/05/07 (GDD §17.3, DM §42, §93, §133; docs/IAP.md): compras in-app no nucleo, sem SDK. Ledger por ID de transacao (como o
    /// do Metas e o do Anuncios): o mesmo recibo nunca entrega 2x (callback repetido, restore, reabrir). Ordem (Concluir): entrega no estado
    /// (Pendente) -> o save GRAVA -> confirma a loja. App morto antes do save = a loja reentrega o recibo na proxima abertura (nao confirmado);
    /// morto entre o save e a confirmacao = o save ja tem a entrega (Entregue) e a abertura seguinte so confirma. Posse dos nao-consumiveis
    /// RE-DERIVADA dos recibos validos (Conectar/Receber): nao existe booleano no save; save editado sem recibo valido nao liga nada, e sem
    /// loja (build sem SDK) nenhum recibo vale. Nada aqui le relogio: relogio para tras nao tira nem duplica compra.
    /// Save: linhas iap.tx=tx,produto,e|c,carga64,assinatura64 (so com compra; save sem elas = nada comprado).
    /// </summary>
    public sealed class Compras
    {
        public const string RemoveAds = "remove_ads", SemVideo = "sem_video";
        /// <summary>Posses que o jogo sabe entregar: remove_ads = veto do intersticial (Anuncios.VetoIntersticial); sem_video = os placements de
        /// SemVideoCobre dao a recompensa sem o video, com as MESMAS recargas, tetos e ledger. Cosmetico entra aqui com o sistema dele (DM §48).</summary>
        public static readonly string[] Posses = new[] { RemoveAds, SemVideo }.Concat(LiveOpsDefs.Passes.Select(p => PossePremium(p.Id))).ToArray();
        /// <summary>LIVEOPS_V1 §3.5: a posse da trilha premium de uma temporada do Pass (o produto iap_{pass}).</summary>
        public static string PossePremium(string passe) => "premium_" + passe;
        /// <summary>Moedas que uma compra pode dar: nenhuma ate a Wave E (V9: so o ouro ativo; Gems entram com o mapa fonte -> ralo do
        /// BALANCE.md). Ouro nunca (V20: premium sem ouro, Renome, XP nem Blueprint).</summary>
        public static readonly string[] MoedasAVenda = new string[0];
        /// <summary>V20 (conveniencias do premium): Velocidade, Trocar e Supply Drop. VIP, Cofre 2x e bonus da encomenda dao ouro direto: so com
        /// video (decidido por padrao, revisar com a P3).</summary>
        public static readonly Anuncio[] SemVideoCobre = { Anuncio.Velocidade, Anuncio.EncomendaTroca, Anuncio.Supply };

        /// <summary>E-PLAT-05 (decidido por padrao, revisar com a P3): Pacote do Mestre, nao-consumivel, sem 2a moeda. Tira o intersticial (se a
        /// flag um dia ligar) e da Velocidade, Lingotes e Trocar sem video. Preco = placeholder (GDD §17.3: US$ 2,99-4,99 proposto).</summary>
        public static readonly ProductDef[] Catalogo =
        {
            new ProductDef { Id = "iap_pacote_mestre", Tipo = TipoProduto.NaoConsumivel, PrecoPlaceholder = "R$ 14,90",
                Conteudo = new[] { ItemCompra.Posse(RemoveAds), ItemCompra.Posse(SemVideo) } },
        };

        /// <summary>LIVEOPS_V1 §3.5 (Wave E, atras do Tuning.PassePremium): o premium de cada temporada, nao-consumivel, compra direta sem moeda
        /// intermediaria. Preco = placeholder pela faixa de mercado dos passes (US$ 8,99-9,99, GDD §17.3, BENCHMARK_MERCADO §8): revisar na P3.</summary>
        public static readonly ProductDef[] CatalogoPasse = Array.ConvertAll(LiveOpsDefs.Passes, p => new ProductDef
        {
            Id = "iap_" + p.Id, Tipo = TipoProduto.NaoConsumivel, PrecoPlaceholder = "R$ 44,90", Conteudo = new[] { ItemCompra.Posse(PossePremium(p.Id)) },
        });

        /// <summary>Todo produto conhecido (o que a loja busca e restaura, a venda ou nao) e o que o validator confere.</summary>
        public static readonly ProductDef[] Todos = Catalogo.Concat(CatalogoPasse).ToArray();

        /// <summary>O que a loja oferece agora: o Pacote do Mestre e, com a flag, o premium das temporadas.</summary>
        public static ProductDef[] AVenda(Tuning t) => t.PassePremium ? Catalogo.Concat(CatalogoPasse).ToArray() : Catalogo;

        /// <summary>Qualquer produto conhecido, a venda ou nao: o recibo do Pass comprado continua valendo com a flag desligada.</summary>
        public static ProductDef Def(string id) => Array.Find(Catalogo, d => d.Id == id) ?? Array.Find(CatalogoPasse, d => d.Id == id);

        sealed class Registro { public Recibo R; public EstadoCompra E; }

        readonly Sim _sim;
        readonly Dictionary<string, Registro> _ledger = new Dictionary<string, Registro>();
        readonly HashSet<string> _possuidos = new HashSet<string>(), _posses = new HashSet<string>();

        /// <summary>A loja que valida os recibos (null = build sem loja: nenhum recibo vale, nada a comprar).</summary>
        public IValidaRecibo Validador { get; private set; }
        /// <summary>Produto com compra em curso nesta sessao (null = nenhum): uma por vez.</summary>
        public string Iniciada { get; private set; }

        public Compras(Sim sim) { _sim = sim; }

        /// <summary>A View chama depois do Load com a loja da sessao: re-deriva as posses dos recibos gravados.</summary>
        public void Conectar(IValidaRecibo v) { Validador = v; Derivar(); }

        public bool Tem(string posse) => _posses.Contains(posse);
        /// <summary>Nao-consumivel com recibo valido.</summary>
        public bool Possui(string produto) => _possuidos.Contains(produto);
        public bool SemVideoEm(Anuncio a) => Tem(SemVideo) && Array.IndexOf(SemVideoCobre, a) >= 0;
        public EstadoCompra? Estado(string tx) => tx != null && _ledger.TryGetValue(tx, out Registro g) ? g.E : (EstadoCompra?)null;
        public int Transacoes => _ledger.Count;

        public bool Pendentes
        {
            get
            {
                foreach (Registro g in _ledger.Values) if (g.E == EstadoCompra.Pendente) return true;
                return false;
            }
        }

        /// <summary>Ha loja, o produto existe, nenhuma compra em curso e o nao-consumivel ainda nao e' do jogador.</summary>
        public bool PodeComprar(string id)
        {
            ProductDef d = Def(id);
            return Validador != null && d != null && Iniciada == null && !(d.Tipo != TipoProduto.Consumivel && Possui(id));
        }

        /// <summary>Toque em comprar (iap_start): marca a compra em curso. false = nao pode (a View nem chama a loja).</summary>
        public bool Iniciar(string id)
        {
            if (!PodeComprar(id)) return false;
            Iniciada = id;
            return true;
        }

        /// <summary>A loja recusou, o jogador cancelou ou o pagamento ficou para depois: libera o botao (o recibo, se vier, chega pelo Receber).</summary>
        public void Falhou(string id) { if (Iniciada == id) Iniciada = null; }

        /// <summary>
        /// Recibo da loja: compra nova, paga e nao confirmada de uma abertura anterior, ou restaurada. null = invalido (sem loja, ID de transacao
        /// fora do formato, produto fora do catalogo, assinatura ou carga que nao bate): nada muda e NAO se confirma (a loja devolve o dinheiro).
        /// Pendente = entregue agora, falta gravar (Concluir). Entregue = este ID ja tinha sido entregue (repetido, restore): nada a mais, so
        /// confirmar de novo (a loja so reenvia o que nao ficou confirmado). O recibo novo do mesmo ID substitui o gravado (posse volta).
        /// </summary>
        public EstadoCompra? Receber(Recibo r)
        {
            ProductDef d = r == null ? null : Def(r.Produto);
            if (r != null && Iniciada == r.Produto) Iniciada = null;   // a loja respondeu a compra em curso (valida ou nao)
            if (d == null || !TxOk(r.Tx) || Validador == null || !Validador.Valido(r)) return null;
            if (_ledger.TryGetValue(r.Tx, out Registro g))
            {
                g.R = r;
                if (g.E == EstadoCompra.Confirmada) g.E = EstadoCompra.Entregue;
            }
            else _ledger[r.Tx] = g = new Registro { R = r, E = EstadoCompra.Pendente };   // ponytail: Moeda (Wave E) credita aqui, 1x por este ledger, quando a carteira com IDs existir
            Derivar();
            return g.E;
        }

        /// <summary>
        /// Fecha as compras: havendo Pendente, `gravar` (o save do jogo) tem de devolver true ANTES de qualquer confirmacao; save que falhou =
        /// nada confirmado (fica Pendente; se o app morrer, a loja reentrega). Depois `confirmar` (a loja) cada Entregue, inclusive os que vieram
        /// do save de uma abertura que morreu antes de confirmar. Devolve os recibos confirmados agora.
        /// </summary>
        public List<Recibo> Concluir(Func<bool> gravar, Action<string> confirmar)
        {
            var feitos = new List<Recibo>();
            if (Pendentes && !gravar()) return feitos;
            foreach (Registro g in Ordem())
            {
                if (g.E == EstadoCompra.Pendente) g.E = EstadoCompra.Entregue;
                if (g.E != EstadoCompra.Entregue) continue;
                confirmar(g.R.Tx);
                g.E = EstadoCompra.Confirmada;
                feitos.Add(g.R);
            }
            return feitos;
        }

        /// <summary>Posses = conteudo dos nao-consumiveis com recibo VALIDO pela loja desta sessao. Mais nada liga o remove_ads.</summary>
        void Derivar()
        {
            _possuidos.Clear(); _posses.Clear();
            foreach (Registro g in _ledger.Values)
            {
                ProductDef d = Def(g.R.Produto);
                if (d == null || d.Tipo == TipoProduto.Consumivel || Validador == null || !Validador.Valido(g.R)) continue;
                _possuidos.Add(d.Id);
                foreach (ItemCompra c in d.Conteudo) if (c.Tipo == TipoConteudo.Posse) _posses.Add(c.Id);
            }
            _sim.Anuncios.RemoveAds = Tem(RemoveAds);
        }

        List<Registro> Ordem()
        {
            var l = new List<Registro>(_ledger.Values);
            l.Sort((a, b) => string.CompareOrdinal(a.R.Tx, b.R.Tx));
            return l;
        }

        /// <summary>ID de transacao: 1-512 caracteres [A-Za-z0-9._:-] (o purchaseToken da Google cabe; virgula e quebra de linha nunca).</summary>
        static bool TxOk(string s) => !string.IsNullOrEmpty(s) && s.Length <= 512
            && Array.TrueForAll(s.ToCharArray(), c => c < 128 && (char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == ':' || c == '-'));

        static bool IdOk(string s) => !string.IsNullOrEmpty(s) && Array.TrueForAll(s.ToCharArray(), c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_');

        // ------------------------------------------------------------------ validator (ContentValidator.Validate chama)

        /// <summary>E-PLAT-04/05: ID iap_[a-z0-9_] unico; preco de referencia marcado (placeholder ate a Wave E); assinatura fora (DM §43);
        /// conteudo fixo e visivel (nao vazio, quantidade >= 1, sem repetir); posse so das conhecidas e nunca num consumivel (a loja nao
        /// restaura consumivel); moeda so das A VENDA (nenhuma ate a Wave E; ouro nunca) e nunca num nao-consumivel (o restore daria de novo).</summary>
        public static IEnumerable<string> Validar(ProductDef[] defs)
        {
            var ids = new HashSet<string>();
            foreach (ProductDef d in defs)
            {
                string at = "iap " + d.Id;
                if (d.Id == null || !d.Id.StartsWith("iap_", StringComparison.Ordinal) || !IdOk(d.Id)) yield return $"{at}: ID fora de iap_[a-z0-9_]";
                else if (!ids.Add(d.Id)) yield return $"{at}: ID repetido";
                if (d.Tipo == TipoProduto.Assinatura) yield return $"{at}: assinatura fora da v1.0 (DM §43)";
                if (string.IsNullOrEmpty(d.PrecoPlaceholder)) yield return $"{at}: sem preco de referencia (placeholder ate a Wave E)";
                if (d.Conteudo == null || d.Conteudo.Length == 0) { yield return $"{at}: sem conteudo (o jogador ve o que leva antes de pagar)"; continue; }
                var vistos = new HashSet<string>();
                foreach (ItemCompra c in d.Conteudo)
                {
                    if (c.Qtd < 1) yield return $"{at}: {c.Id} com quantidade {c.Qtd} (fixa, >= 1)";
                    if (!vistos.Add(c.Tipo + ":" + c.Id)) yield return $"{at}: {c.Id} repetido";
                    if (c.Tipo == TipoConteudo.Posse)
                    {
                        if (Array.IndexOf(Posses, c.Id) < 0) yield return $"{at}: posse \"{c.Id}\" desconhecida";
                        if (d.Tipo == TipoProduto.Consumivel) yield return $"{at}: posse {c.Id} num consumivel (a loja nao restaura consumivel)";
                    }
                    else
                    {
                        if (Array.IndexOf(MoedasAVenda, c.Id) < 0) yield return $"{at}: moeda \"{c.Id}\" nao esta a venda (V9: nenhuma ate a Wave E; ouro nunca, V20)";
                        if (d.Tipo != TipoProduto.Consumivel) yield return $"{at}: moeda {c.Id} num nao-consumivel (o restore daria de novo)";
                    }
                }
            }
        }

        // ------------------------------------------------------------------ save (linhas iap.*; o Sim.Load de antes ignora chave desconhecida)

        /// <summary>Uma linha por transacao, em ordem de ID; Pendente sai como "e": o save que a leva e' o que grava a entrega.</summary>
        public void Save(StringBuilder sb)
        {
            foreach (Registro g in Ordem())
                sb.Append("iap.tx=").Append(g.R.Tx).Append(',').Append(g.R.Produto).Append(',').Append(g.E == EstadoCompra.Confirmada ? 'c' : 'e')
                  .Append(',').Append(B64(g.R.Payload)).Append(',').Append(B64(g.R.Assinatura)).Append('\n');
        }

        /// <summary>Le as linhas iap.tx= (lixo = ignorada). A posse so volta no Conectar, com a loja validando cada recibo.</summary>
        public void Load(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                if (!raw.StartsWith("iap.tx=", StringComparison.Ordinal)) continue;
                string[] p = raw.Substring(7).Trim().Split(',');
                if (p.Length < 5 || !TxOk(p[0]) || (p[2] != "e" && p[2] != "c")) continue;
                _ledger[p[0]] = new Registro
                {
                    R = new Recibo { Tx = p[0], Produto = p[1], Payload = D64(p[3]), Assinatura = D64(p[4]) },
                    E = p[2] == "c" ? EstadoCompra.Confirmada : EstadoCompra.Entregue,
                };
            }
            Derivar();
        }

        static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? ""));

        static string D64(string s)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); }
            catch (FormatException) { return ""; }
        }
    }
}
