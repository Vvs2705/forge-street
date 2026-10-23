#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace FS
{
    /// <summary>
    /// E-PLAT-04 (docs/IAP.md §5): a loja de verdade, Unity IAP 5 (Google Play Billing). So existe com a chave publica da Play no projeto
    /// (GooglePlayTangle, gerado em Services > In-App Purchasing > Receipt Validation Obfuscator): sem ela `Criar` devolve null e o release
    /// segue sem LOJA, como hoje. Fluxo de 2 passos do v5: OnPurchasePending -> recibo (o nucleo entrega e grava) -> Confirmar(tx) ->
    /// ConfirmPurchase. Nunca confirma antes do save: o pedido pago e nao confirmado volta no proximo Connect.
    /// </summary>
    public sealed class LojaUnity : IStore
    {
        readonly StoreController _store = UnityIAPServices.StoreController();
        readonly CrossPlatformValidator _validador;
        readonly Dictionary<string, PendingOrder> _pendentes = new Dictionary<string, PendingOrder>();
        ProductDef[] _produtos;
        Action<Recibo> _recibo;
        Action<string, string> _falha;
        Action<int> _fimRestaurar;
        int _restaurados;

        LojaUnity(byte[] chaveGoogle) => _validador = new CrossPlatformValidator(chaveGoogle, Application.identifier);

        /// <summary>A loja do release no Android, ou null sem o GooglePlayTangle (a chave RSA da Play ainda nao foi gerada no projeto).
        /// ponytail: o Tangle e' codigo gerado que so existe depois do obfuscator; por reflexao para o build compilar sem ele.</summary>
        public static LojaUnity Criar()
        {
            Type t = Type.GetType("UnityEngine.Purchasing.Security.GooglePlayTangle, Assembly-CSharp")
                ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.Purchasing.Security.GooglePlayTangle")).FirstOrDefault(x => x != null);
            var dados = t?.GetMethod("Data")?.Invoke(null, null) as byte[];
            return dados == null || dados.Length == 0 ? null : new LojaUnity(dados);
        }

        public async void Iniciar(ProductDef[] produtos, Action<Recibo> recibo, Action<string, string> falha)
        {
            _produtos = produtos; _recibo = recibo; _falha = falha;
            // todos os eventos ANTES do Connect: o pedido pago de uma abertura anterior chega logo
            _store.OnPurchasePending += Pendente;
            _store.OnPurchaseConfirmed += o => { if (o is FailedOrder f) Debug.LogWarning("IAP confirmar: " + f.FailureReason + " " + f.Details); };
            _store.OnPurchaseFailed += f => _falha(Produto(f.CartOrdered), f.FailureReason.ToString());
            _store.OnPurchaseDeferred += d => _falha(Produto(d.CartOrdered), "adiada");   // Pix/boleto pendente: o recibo vem quando pagar
            _store.OnStoreConnected += () => { _store.FetchProducts(Definicoes()); _store.FetchPurchases(); };
            _store.OnStoreDisconnected += d => Debug.LogWarning("IAP desconectada: " + d.Message);
            _store.OnProductsFetched += p => { };
            _store.OnProductsFetchFailed += f => Debug.LogWarning("IAP produtos: " + f.FailureReason);
            _store.OnPurchasesFetched += o => { };
            _store.OnPurchasesFetchFailed += f => Debug.LogWarning("IAP compras: " + f.Message);
            _store.OnAuthAccountChanged += () => { _store.FetchProducts(Definicoes()); _store.FetchPurchases(); };   // outra conta Google: caches vazios
            await _store.Connect();
        }

        List<ProductDefinition> Definicoes() => _produtos.Select(p => new ProductDefinition(p.Id,
            p.Tipo == TipoProduto.Consumivel ? ProductType.Consumable : p.Tipo == TipoProduto.Assinatura ? ProductType.Subscription : ProductType.NonConsumable)).ToList();

        static string Produto(ICart c) => c?.Items().FirstOrDefault()?.Product?.definition?.id;

        void Pendente(PendingOrder o)
        {
            string tx = o.Info.TransactionID;
            _pendentes[tx] = o;
            _restaurados++;
            _recibo(new Recibo { Tx = tx, Produto = Produto(o.CartOrdered), Payload = o.Info.Receipt, Assinatura = "" });
        }

        /// <summary>A assinatura da Google confere com a chave do app, e algum recibo dela e' deste produto e desta transacao.</summary>
        public bool Valido(Recibo r)
        {
            if (r == null || string.IsNullOrEmpty(r.Payload)) return false;
            try
            {
                return _validador.Validate(r.Payload).Any(p => p.productID == r.Produto && p.transactionID == r.Tx);
            }
            catch (IAPSecurityException) { return false; }
            catch (Exception e) { Debug.LogWarning("IAP recibo: " + e.Message); return false; }
        }

        public string Preco(string produto) => _store.GetProductById(produto)?.metadata?.localizedPriceString;

        public void Comprar(string produto)
        {
            Product p = _store.GetProductById(produto);
            if (p == null || !p.availableToPurchase) { _falha(produto, "indisponivel"); return; }
            _store.PurchaseProduct(p);
        }

        public void Confirmar(string tx)
        {
            if (tx != null && _pendentes.TryGetValue(tx, out PendingOrder o)) { _pendentes.Remove(tx); _store.ConfirmPurchase(o); }
        }

        /// <summary>Cada compra restaurada chega pelo OnPurchasePending (vira recibo); `fim` recebe quantas chegaram ate o callback.</summary>
        public void Restaurar(Action<int> fim)
        {
            _restaurados = 0; _fimRestaurar = fim;
            _store.RestoreTransactions((ok, erro) =>
            {
                if (!ok) Debug.LogWarning("IAP restaurar: " + erro);
                _fimRestaurar?.Invoke(_restaurados); _fimRestaurar = null;
            });
        }
    }
}
#endif
