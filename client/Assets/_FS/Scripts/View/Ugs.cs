using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Unity.Services.Analytics;
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
        /// <summary>UnityServices.InitializeAsync uma vez por processo (analytics e config dividem).</summary>
        public static Task Iniciar() => _init ?? (_init = UnityServices.InitializeAsync());
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

        static void Grava(Evento e) => AnalyticsService.Instance.RecordEvent(new CustomEvent(Evento)
        {
            { "fsNome", e.Nome ?? "" }, { "fsA", e.A ?? "" }, { "fsB", e.B ?? "" }, { "fsT", e.T }, { "fsConfig", e.Config ?? "" },
        });
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
                RuntimeConfig c = await RemoteConfigService.Instance.FetchConfigsAsync(new Vazio(), new Vazio());
                if (c == null || !c.HasKey(Chave)) { log("rc_fetch", "vazio", ""); return; }
                string json = c.GetJson(Chave);
                log("rc_fetch", _local.Guardar(json) ? "ok" : "invalido", json.Length.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception e) { log("rc_fetch", "falha", e.Message); }
        }

        struct Vazio { }
    }
}
