using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FS.Core;
#if !FS_CORETESTS
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
#endif

namespace FS
{
    // A-PLAT-08/09 (DM §85-90, §129-130; docs/REMOTECONFIG_ANALYTICS.md): RemoteConfig e analytics atras de costura, com o provedor remoto
    // pendente da decisao P8. A parte pura (sem UnityEngine) o coretests compila e testa; o HTTP fica atras do #if.
    // Regra de rede: nada sai do aparelho antes da resposta da tela de idade (Game.Liberar chama o Buscar e o Telemetria.Liberar).

    /// <summary>Fonte do RemoteConfig. Ler = o JSON que vale NESTA abertura (o Sim nasce com ele; null = padroes do build). Buscar = pede o
    /// novo em segundo plano, sem bloquear; o bom vai para o cache e vale na PROXIMA abertura. log(evento, a, b) vai para o diario.</summary>
    public interface IRemoteConfig
    {
        string Ler(out string origem);
        void Buscar(Action<string, string, string> log);
    }

    /// <summary>
    /// A-PLAT-08: RemoteConfig local. `arquivo` (-rcfile, so build de desenvolvimento; relativo = dentro de `dir`, o persistentDataPath)
    /// vence tudo; senao o cache do ultimo config bom (remoteconfig.json, gravado pelo Guardar); senao os padroes embutidos (o Tuning).
    /// `cache` = false no -testsession/-autoplay/-record (fotos e smoke nao herdam config baixado). Buscar: nada (o HTTP e' o RemoteConfigHttp).
    /// </summary>
    public sealed class RemoteConfigLocal : IRemoteConfig
    {
        public const string Cache = "remoteconfig.json";
        readonly string _dir, _arquivo;
        readonly bool _cache;

        public RemoteConfigLocal(string dir, string arquivo, bool cache)
        {
            _dir = dir; _arquivo = string.IsNullOrEmpty(arquivo) ? null : arquivo; _cache = cache;
        }

        /// <summary>origem: rcfile | cache | padrao | rcfile_ausente | *_ilegivel. Nunca lanca.</summary>
        public string Ler(out string origem)
        {
            string p = _arquivo != null ? Path.Combine(_dir, _arquivo) : _cache ? Path.Combine(_dir, Cache) : null;
            origem = _arquivo != null ? "rcfile" : "cache";
            try { if (p != null && File.Exists(p)) return File.ReadAllText(p); }
            catch (Exception) { origem += "_ilegivel"; return null; }
            origem = _arquivo != null ? "rcfile_ausente" : "padrao";
            return null;
        }

        public void Buscar(Action<string, string, string> log) { }

        /// <summary>Grava `json` como o ultimo bom se ele abrir (RemoteConfig.Aplicar valido); troca atomica do SaveStore. false = ficou o anterior.</summary>
        public bool Guardar(string json)
        {
            if (!RemoteConfig.Aplicar(new Tuning(), json, false).Valido) return false;
            try { SaveStore.Write(Path.Combine(_dir, Cache), json); return true; }
            catch (Exception) { return false; }   // disco cheio: fica o cache anterior
        }
    }

    /// <summary>A-PLAT-08: anuncio desligado pelo RemoteConfig (Tuning.AnunciosLigados = false): o SDK nem inicia, nenhum botao, todo Show falha.</summary>
    public sealed class SemAnuncio : IAds
    {
        public bool Busy => false;
        public bool Ready(string placement) => false;
        public bool Loaded(string placement) => false;
        public void Show(string placement, Action onReward, Action onFail) => onFail?.Invoke();
        public bool InterstitialReady => false;
        public void ShowInterstitial(Action done) => done?.Invoke();
    }

    /// <summary>A-PLAT-09: um evento no esquema versionado que vai ao provedor remoto. Esquema 1: v, seq (ordem na sessao: o evento antes de
    /// uma excecao e' o seq anterior), nome, a, b (taxonomia do diario, DM §86), t (s de jogo), sessao (8 caracteres aleatorios por processo),
    /// app (Application.version) e config (Resultado.Rotulo do RemoteConfig, DM §89). Nada de ID de aparelho, conta, local ou de publicidade.</summary>
    public struct Evento
    {
        public const int Esquema = 1;
        public long Seq; public float T;
        public string Nome, A, B, Sessao, App, Config;

        public string ParaJson() => "{\"v\":" + Esquema.ToString(CultureInfo.InvariantCulture) + ",\"seq\":" + Seq.ToString(CultureInfo.InvariantCulture)
            + ",\"nome\":" + Json.Aspas(Nome) + ",\"a\":" + Json.Aspas(A) + ",\"b\":" + Json.Aspas(B) + ",\"t\":" + T.ToString("0.0", CultureInfo.InvariantCulture)
            + ",\"sessao\":" + Json.Aspas(Sessao) + ",\"app\":" + Json.Aspas(App) + ",\"config\":" + Json.Aspas(Config) + "}";
    }

    /// <summary>A-PLAT-09: provedor remoto de analytics e crash (decisao P8). Iniciar = 1x, depois da resposta da tela de idade;
    /// idPublicidade = pode usar o ID de publicidade (so 18+ com aceite; V21: menor de 18 nunca). Enviar = um evento (o SDK faz lote e reenvio).</summary>
    public interface IProvedorAnalytics
    {
        void Iniciar(bool idPublicidade);
        void Enviar(Evento e);
    }

    /// <summary>P8 pendente: descarta tudo. `eco` (dev, -analyticslog) recebe o JSON do que seria enviado (o Game manda ao Player.log).</summary>
    public sealed class ProvedorNulo : IProvedorAnalytics
    {
        readonly Action<string> _eco;
        public ProvedorNulo(Action<string> eco = null) { _eco = eco; }
        public void Iniciar(bool idPublicidade) => _eco?.Invoke("{\"iniciar\":{\"id_publicidade\":" + (idPublicidade ? "true" : "false") + "}}");
        public void Enviar(Evento e) => _eco?.Invoke(e.ParaJson());
    }

    /// <summary>
    /// A-PLAT-09: o IAnalytics do Game. Todo evento vai ao `local` (o Diario: dev ou -playtest, 1 MB com rotacao; no release, nada) e ao
    /// provedor remoto so DEPOIS do Liberar (resposta da tela de idade, gravada ou nova): antes fica numa fila em memoria de FilaMax (cheia, o
    /// mais velho cai e conta em Descartados). Excecao nao tratada (Application.logMessageReceived) vira o evento "excecao" ate ExcecoesMax por
    /// sessao, sem os `caminhos` privados (o persistentDataPath do Windows tem o nome do usuario). Provedor que lanca nunca derruba o jogo.
    /// </summary>
    public sealed class Telemetria : IAnalytics
    {
        public const int FilaMax = 256;      // ~1a abertura inteira ate a resposta (session_info + tutorial + fps por minuto)
        public const int ExcecoesMax = 5;    // um erro em loop nao enche o diario nem a cota do provedor
        public const int TextoMax = 120;     // a e b no remoto
        readonly IAnalytics _local;
        readonly IProvedorAnalytics _remoto;
        readonly Queue<Evento> _fila = new Queue<Evento>();
        readonly string _sessao, _app, _config;
        readonly string[] _caminhos;
        long _seq;

        public bool Liberada { get; private set; }
        public int Descartados { get; private set; }
        public int Excecoes { get; private set; }

        public Telemetria(IAnalytics local, IProvedorAnalytics remoto, string sessao, string app, string config, params string[] caminhos)
        {
            _local = local; _remoto = remoto; _sessao = sessao; _app = app; _config = config; _caminhos = caminhos;
        }

        public void Log(string evt, float t, string a = "", string b = "")
        {
            _local.Log(evt, t, a, b);
            var e = new Evento { Seq = ++_seq, Nome = evt, A = Corta(a), B = Corta(b), T = t, Sessao = _sessao, App = _app, Config = _config };
            if (Liberada) { Envia(e); return; }
            if (_fila.Count >= FilaMax) { _fila.Dequeue(); Descartados++; }
            _fila.Enqueue(e);
        }

        /// <summary>Resposta da tela de idade: inicia o provedor e manda a fila, 1x por sessao (mudar a resposta vale na proxima abertura, como
        /// no anuncio).</summary>
        public void Liberar(Faixa f, bool aceitou)
        {
            if (Liberada) return;
            Liberada = true;
            try { _remoto.Iniciar(Privacidade.Flags(f, aceitou).Consentimento); }
            catch (Exception) { }
            while (_fila.Count > 0) Envia(_fila.Dequeue());
        }

        /// <summary>Excecao nao tratada -> evento "excecao" (a = mensagem, b = 1a linha da pilha), ate ExcecoesMax; false = passou do teto.</summary>
        public bool Excecao(string msg, string pilha, float t)
        {
            if (Excecoes >= ExcecoesMax) return false;
            Excecoes++;
            string topo = (pilha ?? "").Split('\n')[0];
            Log("excecao", t, Corta(Anonimo(msg)), Corta(Anonimo(topo)));
            return true;
        }

        void Envia(Evento e)
        {
            try { _remoto.Enviar(e); }
            catch (Exception) { }   // ponytail: best-effort, como o Diario; o SDK do P8 guarda e reenvia o lote dele
        }

        string Anonimo(string s)
        {
            s = s ?? "";
            foreach (string c in _caminhos) if (!string.IsNullOrEmpty(c)) s = s.Replace(c, "~");
            return s;
        }

        static string Corta(string s) => s == null ? "" : s.Length <= TextoMax ? s : s.Substring(0, TextoMax);
    }

#if !FS_CORETESTS
    /// <summary>
    /// A-PLAT-08: provedor HTTP generico do RemoteConfig, DESLIGADO (Url vazia = nenhuma requisicao). Ler = o local (rcfile, cache, padrao).
    /// Buscar: GET na Url (so https) com timeout de TimeoutS, em corrotina (nunca bloqueia o boot); o JSON que abre vira o cache e vale na
    /// proxima abertura; falha, timeout ou JSON ruim = fica o anterior. Ligar = preencher a Url (decisao P8; atualizar docs/PRIVACIDADE.md §2).
    /// </summary>
    public sealed class RemoteConfigHttp : IRemoteConfig
    {
        public const string Url = "";
        public const int TimeoutS = 5;
        readonly RemoteConfigLocal _local;
        readonly MonoBehaviour _host;

        public RemoteConfigHttp(RemoteConfigLocal local, MonoBehaviour host) { _local = local; _host = host; }

        public string Ler(out string origem) => _local.Ler(out origem);

        public void Buscar(Action<string, string, string> log)
        {
            if (!Url.StartsWith("https://", StringComparison.Ordinal)) return;
            _host.StartCoroutine(Get(log));
        }

        IEnumerator Get(Action<string, string, string> log)
        {
            using (UnityWebRequest w = UnityWebRequest.Get(Url))
            {
                w.timeout = TimeoutS;
                yield return w.SendWebRequest();
                if (w.result != UnityWebRequest.Result.Success) { log("rc_fetch", "falha", w.error); yield break; }
                log("rc_fetch", _local.Guardar(w.downloadHandler.text) ? "ok" : "invalido", w.downloadHandler.text.Length.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
#endif
}
