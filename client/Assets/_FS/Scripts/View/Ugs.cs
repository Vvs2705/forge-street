using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using FS.Core;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace FS
{
    // A-PLAT-08/09, P8 = Unity Gaming Services (decisao do Vinicius em 2026-10-23; docs/REMOTECONFIG_ANALYTICS.md §3): projeto "Forge Street"
    // do Unity Cloud (organizacao vinicius_unity980, ambiente production). Nada inicia antes da tela de idade: o Game chama o Buscar e a Telemetria o
    // Iniciar so no Liberar. As travas do RemoteConfig e a fila do analytics continuam no nucleo e na Telemetria.

    static class Ugs
    {
        static Task _init;
        /// <summary>Ambiente do UGS (null = o padrao do projeto, production). So o build dev o troca, por -ugsenv nome (o Game le antes da
        /// 1a chamada): o teste das flags pelo painel publica no ambiente `teste` sem mexer no de producao.</summary>
        public static string Ambiente;
        /// <summary>O Remote Config sem o Authentication nao segue o environment-name do Core: pede o ID (painel > Ambientes).</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> Ids = new System.Collections.Generic.Dictionary<string, string>
        {
            { "teste", "98334f17-686d-4073-8fec-34faf3599119" },
        };
        public static string AmbienteId => Ambiente != null && Ids.TryGetValue(Ambiente, out string id) ? id : null;
        /// <summary>UnityServices.InitializeAsync uma vez por processo (analytics e config dividem).</summary>
        public static Task Iniciar()
        {
            if (_init != null) return _init;
            var op = new InitializationOptions();
            if (!string.IsNullOrEmpty(Ambiente)) op.SetOption("com.unity.services.core.environment-name", Ambiente);   // = SetEnvironmentName, sem o assembly Environments
            return _init = UnityServices.InitializeAsync(op);
        }
    }

    /// <summary>
    /// Analytics remoto pelo UGS. So com consentimento (18+ que aceitou, Privacidade.Flags): o UGS so coleta com o AnalyticsIntent concedido
    /// (EndUserConsent), e menor de 18 ou quem recusou fica so no diario local. Um evento no esquema do painel, `fs_evento`, com os campos
    /// do esquema v1 que o UGS nao tem (sessao, versao e ordem ja vem dele): fsNome, fsA, fsB (texto), fsT (s de jogo) e fsConfig (id@versao).
    /// </summary>
    public sealed class ProvedorUgs : IProvedorAnalytics
    {
        public const string Evento = "fs_evento";
        const int Espera = 256;
        readonly Queue<Evento> _espera = new Queue<Evento>();
        bool _pedido, _ligado;

        public async void Iniciar(bool consentimento)
        {
            if (!consentimento) return;
            _pedido = true;
            try
            {
                ConsentState c = EndUserConsent.GetConsentState();
                c.AnalyticsIntent = ConsentStatus.Granted;   // o consentimento do UGS (Developer Data framework) = o aceite da tela de idade
                EndUserConsent.SetConsentState(c);
                await Ugs.Iniciar();
                _ligado = true;
                while (_espera.Count > 0) Grava(_espera.Dequeue());
            }
            catch (Exception e) { Debug.LogWarning("UGS analytics: " + e.Message); _espera.Clear(); }
        }

        /// <summary>Sem consentimento: nada. Pedido e ainda iniciando: espera (teto de 256, como a fila da Telemetria).</summary>
        public void Enviar(Evento e)
        {
            if (_ligado) Grava(e);
            else if (_pedido && _espera.Count < Espera) _espera.Enqueue(e);
        }

        static void Grava(Evento e)
        {
            IAnalyticsService a = AnalyticsService.Instance;
            a.RecordEvent(new CustomEvent(Evento)
            {
                { "fsNome", e.Nome ?? "" }, { "fsA", e.A ?? "" }, { "fsB", e.B ?? "" }, { "fsT", e.T }, { "fsConfig", e.Config ?? "" },
            });
            if (EventoPadrao.Compra(e, out string produto, out double preco, out string moeda))
            {
                // o ID de transacao nao vai (dado de compra, docs/IAP.md); o painel le transactionName e o realCurrency gasto
                var t = new TransactionEvent
                {
                    TransactionName = produto, TransactionType = TransactionType.PURCHASE, ProductId = produto,
                    SpentRealCurrency = new TransactionRealCurrency { RealCurrencyType = moeda, RealCurrencyAmount = a.ConvertCurrencyToMinorUnits(moeda, preco) },
                };
                t.ReceivedItems.Add(new TransactionItem { ItemName = produto, ItemType = "iap", ItemAmount = 1 });
                a.RecordEvent(t);
            }
            else if (EventoPadrao.Impressao(e, out string placement, out string rede, out double? usd))
            {
                var i = new AdImpressionEvent
                {
                    AdCompletionStatus = AdCompletionStatus.Completed, AdProvider = Rede(rede), PlacementId = placement, PlacementName = placement,
                    PlacementType = placement == Ads.Intersticial ? AdPlacementType.INTERSTITIAL : AdPlacementType.REWARDED, AdSource = rede,
                };
                if (usd.HasValue) i.AdEcpmUsd = usd.Value * 1000.0;   // o ILRD do LevelPlay da a receita da impressao; o eCPM e' por mil
                a.RecordEvent(i);
            }
        }

        /// <summary>Rede do ILRD do LevelPlay ("ironSource", "UnityAds", "AdMob", ...) -> o enum do UGS; fora da lista (e "simulado") = Other.</summary>
        static AdProvider Rede(string r)
        {
            r = (r ?? "").ToLowerInvariant();
            if (r.Contains("ironsource")) return AdProvider.IrnSource;
            if (r.Contains("unity")) return AdProvider.UnityAds;
            if (r.Contains("admob") || r.Contains("google")) return AdProvider.AdMob;
            if (r.Contains("applovin")) return AdProvider.AppLovin;
            if (r.Contains("facebook") || r.Contains("meta")) return AdProvider.Facebook;
            if (r.Contains("vungle") || r.Contains("liftoff")) return AdProvider.Vungle;
            if (r.Contains("pangle")) return AdProvider.Pangle;
            if (r.Contains("inmobi")) return AdProvider.Inmobi;
            if (r.Contains("chartboost")) return AdProvider.ChartBoost;
            if (r.Contains("fyber") || r.Contains("digitalturbine")) return AdProvider.Fyber;
            return AdProvider.Other;
        }
    }

    /// <summary>
    /// RemoteConfig pelo UGS (opcao B do §3): o painel guarda o config inteiro na chave JSON `fs_config` (o mesmo JSON plano do doc, com id e
    /// versao). Ler = o cache local (o config vale na proxima abertura); Buscar baixa depois da tela de idade e grava no cache so o JSON que
    /// abre (RemoteConfigLocal.Guardar). As travas continuam no RemoteConfig.Aplicar do nucleo.
    /// </summary>
    public sealed class RemoteConfigUgs : IRemoteConfig
    {
        public const string Chave = "fs_config";
        readonly RemoteConfigLocal _local;

        public RemoteConfigUgs(RemoteConfigLocal local) { _local = local; }

        public string Ler(out string origem) => _local.Ler(out origem);

        public async void Buscar(Action<string, string, string> log)
        {
            try
            {
                await Ugs.Iniciar();
                if (Ugs.AmbienteId != null) RemoteConfigService.Instance.SetEnvironmentID(Ugs.AmbienteId);
                RuntimeConfig c = await RemoteConfigService.Instance.FetchConfigsAsync(new Vazio(), new Vazio());
                if (c == null || !c.HasKey(Chave)) { log("rc_fetch", "vazio", ""); return; }
                string json = c.GetJson(Chave);
                log("rc_fetch", _local.Guardar(json) ? "ok" : "invalido", json.Length.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception e) { log("rc_fetch", "falha", e.Message); }
        }

        struct Vazio { }
    }

    /// <summary>
    /// Cloud Save do UGS (docs/CLOUD_SAVE.md): a chave `save` do Player Data guarda o envelope inteiro. Login anonimo (o jogador do UGS e' um
    /// ID aleatorio deste aparelho; sem conta). Erros lancam: o SincroniaNuvem do nucleo segura (offline, timeout) e o jogo segue no local.
    /// </summary>
    public sealed class NuvemUgs : INuvemSave
    {
        public const string Chave = "save";

        static async Task Entrar()
        {
            await Ugs.Iniciar();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public async Task<string> Ler()
        {
            await Entrar();
            Dictionary<string, Unity.Services.CloudSave.Models.Item> d = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { Chave });
            return d.TryGetValue(Chave, out Unity.Services.CloudSave.Models.Item i) ? i.Value.GetAsString() : null;
        }

        public async Task Gravar(string texto)
        {
            await Entrar();
            await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { Chave, texto } });
        }
    }
}
