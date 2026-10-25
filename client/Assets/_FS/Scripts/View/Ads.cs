using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID || UNITY_IOS
using Unity.Services.LevelPlay;
#endif

namespace FS
{
    /// <summary>
    /// Anuncio premiado (rewarded). docs/LEVELPLAY.md. A-PLAT-06: o Game so fala com o IAds que o Init devolve, e ha duas implementacoes:
    /// AdsLevelPlay (SDK real, so' no aparelho Android/iOS e sem -fakeads) e AdsSimulado (~5 s, "Anuncio de teste", recompensa no fim:
    /// Editor, Windows e -fakeads; tambem o fallback do LevelPlay em build de desenvolvimento quando o SDK nao tem anuncio pronto).
    /// Build de release no aparelho sem anuncio pronto: Ready = false (o botao some).
    /// Placements do jogo = os Id do Tuning.Placements (E-PLAT-02); no painel, uma ad unit rewarded por placement
    /// (AdsLevelPlay.Units). Intersticial (E-PLAT-03): so com a flag Tuning.Interstitial ligada no Init.
    /// Diario: ad_init, ad_request, ad_shown, ad_impression (receita da impressao, DM §95), ad_reward, ad_reward_late, ad_fail e ad_load_fail
    /// saem pelo `log` do Init (o Game grava; rewarded_start, rewarded_complete e ad_veto sao do Game).
    /// Este MonoBehaviour so' hospeda as corrotinas das duas implementacoes.
    /// Flags: -fakeads (sempre simulado) e -adtestsuite (abre o Test Suite do LevelPlay; so' em build de desenvolvimento).
    /// E-PLAT-01: o SDK so inicia depois da resposta da tela de idade (Privacidade.cs); sem resposta, ad_init "espera" e nenhum botao.
    /// No PC vem da linha de comando; no Android, do extra "unity" do intent (adb shell am start ... -e unity "-fakeads").
    /// </summary>
    public sealed class Ads : MonoBehaviour
    {
        public const string AppKey = "28bbd582d";   // Android, app "Forge Street" na conta LevelPlay da V-STACK (nao e' segredo: vai no APK)
        public const string Vip = "vip", Velocidade = "velocidade", Intersticial = "intersticial";
        /// <summary>E-PLAT-03: ad unit "interstitial_transicao" do painel; so' carrega com a flag Tuning.Interstitial ligada.</summary>
        public const string InterstitialUnit = "69ng97evbre6swm4";
        /// <summary>Placements de rewarded que o jogo conhece (os Id do Tuning, E-PLAT-02).</summary>
        internal static readonly string[] Placements = Array.ConvertAll(FS.Core.Tuning.Padrao.Placements, d => d.Id);
        static readonly Dictionary<string, string> RewardName = new Dictionary<string, string> { [Vip] = "VIP", [Velocidade] = "Velocidade" };
        internal static string Nome(string placement) => RewardName.TryGetValue(placement, out string n) ? n : placement;

        static IAds _i;
        static Action<string, string, string> _log;

        /// <summary>Idempotente. Chamar cedo (Game.Awake). `log`(evento, placement, detalhe) vai para o diario. No aparelho aplica a
        /// privacidade da resposta gravada e inicia o SDK; sem resposta, espera o Responder. `intersticial` = Tuning.Interstitial: desligada
        /// (o padrao), nenhuma implementacao carrega intersticial (o diario prova: ad_init intersticial desligado).</summary>
        public static IAds Init(Action<string, string, string> log, bool intersticial)
        {
            if (_i != null) return _i;
            _log = log;
            Ads host = new GameObject("Ads").AddComponent<Ads>();
            DontDestroyOnLoad(host.gameObject);
            bool fake = Arg("-fakeads") != null;
            Emit("ad_init", Intersticial, !intersticial ? "desligado" : InterstitialUnit.Length > 0 ? "ligado" : "ligado_sem_ad_unit");
#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor && !fake) return _i = new AdsLevelPlay(host, intersticial);
#endif
            Emit("ad_init", "simulado", fake ? "fakeads" : Application.platform.ToString());
            return _i = new AdsSimulado(host, intersticial);
        }

        /// <summary>DM §95: detalhe do ad_impression = "receita_usd precisao rede" (ILRD do LevelPlay 9.5.1, por impressao); receita ausente =
        /// campo vazio.</summary>
        internal static string Receita(double? usd, string precisao, string rede) =>
            (usd.HasValue ? usd.Value.ToString("0.##########", System.Globalization.CultureInfo.InvariantCulture) : "") + " " + precisao + " " + rede;

        /// <summary>E-PLAT-01: a resposta gravada (PlayerPrefs) com a versao atual do texto; false = a tela pergunta.</summary>
        public static bool Resposta(out Faixa f, out bool aceitou) => Privacidade.Ler(PlayerPrefs.GetString(Privacidade.Chave, ""), out f, out aceitou);

        /// <summary>E-PLAT-01: grava a resposta (gravar = false no -testsession) e passa ao SDK. A 1a resposta inicia o SDK; mudar depois
        /// do Init vale por completo ao reabrir o jogo (AdsLevelPlay.Iniciar).</summary>
        public static void Responder(Faixa f, bool aceitou, bool gravar)
        {
            if (gravar) { PlayerPrefs.SetString(Privacidade.Chave, Privacidade.Gravar(f, aceitou)); PlayerPrefs.Save(); }
#if UNITY_ANDROID || UNITY_IOS
            (_i as AdsLevelPlay)?.Iniciar(f, aceitou);
#endif
        }

        internal static void Emit(string evt, string a, string b)
        {
            a = (a ?? "").Replace(',', ' '); b = (b ?? "").Replace(',', ' ').Replace('\n', ' ');
            Debug.Log($"ADS {evt} {a} {b}");
            try { _log?.Invoke(evt, a, b); }
            catch (Exception e) { Debug.LogException(e); }   // o diario nunca derruba o fluxo do anuncio
        }

        /// <summary>Valor da flag (null = ausente, "" = sem valor), so da linha de comando (Args); o Game.Arg ainda olha o -preset.</summary>
        public static string Arg(string name) => Captura.Valor(Args(), name);

        /// <summary>Linha de comando + extra "unity" do intent no Android (so' em build de desenvolvimento: no release o intent e' de
        /// qualquer app e -fakeads daria recompensa sem anuncio).</summary>
        public static List<string> Args()
        {
            var a = new List<string>(Environment.GetCommandLineArgs());
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Debug.isDebugBuild)
            try
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject act = up.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject intent = act.Call<AndroidJavaObject>("getIntent"))
                {
                    string extra = intent.Call<string>("getStringExtra", "unity");
                    if (!string.IsNullOrEmpty(extra)) a.AddRange(extra.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch (Exception) { }   // sem intent legivel = sem flags
#endif
            return a;
        }

#if UNITY_ANDROID || UNITY_IOS
        void OnDestroy() => (_i as AdsLevelPlay)?.Destroy();
#endif
    }

    /// <summary>Anuncio SIMULADO: tela "Anuncio de teste" por ~5 s com o tempo do jogo parado (como o real, que pausa o app) e a
    /// recompensa no fim. Nunca pede nem mostra anuncio real.</summary>
    sealed class AdsSimulado : IAds
    {
        const float Seconds = 5f;
        readonly MonoBehaviour _host;
        readonly bool _intersticial;
        bool _busy;

        public AdsSimulado(MonoBehaviour host, bool intersticial) { _host = host; _intersticial = intersticial; }

        public bool Busy => _busy;
        public bool Loaded(string placement) => Array.IndexOf(Ads.Placements, placement) >= 0;
        public bool Ready(string placement) => !_busy && Loaded(placement);
        public bool InterstitialReady => _intersticial && !_busy;   // flag desligada: nunca

        public void Show(string placement, Action onReward, Action onFail)
        {
            Ads.Emit("ad_request", placement, "simulado");
            string why = !Loaded(placement) ? "placement_desconhecido" : _busy ? "ocupado" : null;
            if (why != null) { Ads.Emit("ad_fail", placement, why); onFail?.Invoke(); return; }
            _busy = true;
            _host.StartCoroutine(Play(placement, onReward, true));
        }

        public void ShowInterstitial(Action done)
        {
            Ads.Emit("ad_request", Ads.Intersticial, "simulado");
            if (!InterstitialReady) { Ads.Emit("ad_fail", Ads.Intersticial, _intersticial ? "ocupado" : "desligado"); done?.Invoke(); return; }
            _busy = true;
            _host.StartCoroutine(Play(Ads.Intersticial, done, false));
        }

        IEnumerator Play(string placement, Action end, bool rewarded)
        {
            float scale = Time.timeScale;
            Time.timeScale = 0f;   // como o anuncio real (o app fica em pausa por baixo)
            GameObject ui = FakeUi(placement, rewarded, out Text count);
            Ads.Emit("ad_shown", placement, "simulado");
            Ads.Emit("ad_impression", placement, Ads.Receita(0, "simulado", "simulado"));   // simulado nao rende nada
            for (float t = Seconds; t > 0f; t -= Time.unscaledDeltaTime)
            {
                count.text = Mathf.CeilToInt(t).ToString();
                yield return null;
            }
            UnityEngine.Object.Destroy(ui);
            Time.timeScale = scale;
            _busy = false;
            if (rewarded) Ads.Emit("ad_reward", placement, Ads.Nome(placement) + " x1 simulado");
            end?.Invoke();
        }

        static GameObject FakeUi(string placement, bool rewarded, out Text count)
        {
            var go = new GameObject("AnuncioTeste", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 1000;   // acima da HUD, do painel e do joystick
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight = 0.5f;
            RectTransform root = Art.Node(go.transform, "Fundo", Vector2.zero, Vector2.one);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.94f);   // bloqueia o toque no jogo
            Text title = Art.NewText(root, "Titulo", 72, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.72f));
            title.text = Loc.T("adtest.titulo");
            title.fontStyle = FontStyle.Bold;
            title.color = Art.Accent;
            Text sub = Art.NewText(root, "Recompensa", 44, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.62f));
            sub.text = rewarded ? Loc.T("adtest.premio", Loc.T("ads." + placement)) : Loc.T("adtest.intersticial");   // ads.<Id do placement> (o nome do diario e' o Ads.Nome)
            count = Art.NewText(root, "Contador", 200, new Vector2(0.3f, 0.36f), new Vector2(0.7f, 0.52f));
            count.fontStyle = FontStyle.Bold;
            Text foot = Art.NewText(root, "Rodape", 32, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.32f));
            foot.text = Loc.T("adtest.rodape");
            foot.color = Art.ComAlfa(Art.Ink, 0.6f);
            return go;
        }
    }

#if UNITY_ANDROID || UNITY_IOS
    /// <summary>SDK real (LevelPlay 9.5.1). Em build de desenvolvimento sem anuncio carregado (emulador, conta sem fill), o AdsSimulado
    /// entra no lugar; no release, nunca. Um LevelPlayRewardedAd por ad unit (placements que dividem a unit dividem o anuncio carregado); o
    /// pedido lembra o placement, que e' o que o nucleo paga. Intersticial so com a flag ligada e com ad unit (Ads.InterstitialUnit).</summary>
    sealed class AdsLevelPlay : IAds
    {
        /// <summary>Placement -> ad unit (E-PLAT-02: uma unit por placement). O nome da recompensa do painel nao importa: o nucleo aplica
        /// pelo placement do pedido (Anuncios.Pagar).</summary>
        static readonly Dictionary<string, string> Units = new Dictionary<string, string>
        {
            [Ads.Vip] = "bagjqgdg52eedh66",          // rewarded_vip: recompensa "VIP" x1
            [Ads.Velocidade] = "3xcxwsgu2wz215oe",   // rewarded_velocidade: recompensa "Velocidade" x1
            ["cofre2x"] = "yuwuxatngj5nre45",         // rewarded_cofre2x: recompensa "Cofre2x" x1
            ["encomenda_bonus"] = "qfifdr4hjqu6rqb6", // rewarded_encomenda_bonus: recompensa "EncomendaBonus" x1
            ["encomenda_troca"] = "25cuxs1aio7p3wbh", // rewarded_encomenda_troca: recompensa "EncomendaTroca" x1
            ["supply"] = "os8pzvpcedohuk31",          // rewarded_supply: recompensa "Supply" x1
        };
        const float RewardGrace = 2f;
        sealed class Request { public string P; public Action Ok, Fail; public bool Done; }

        readonly MonoBehaviour _host;
        readonly AdsSimulado _fake;   // so' em build de desenvolvimento
        readonly bool _intersticial;
        readonly Dictionary<string, LevelPlayRewardedAd> _ads = new Dictionary<string, LevelPlayRewardedAd>();   // por ad unit
        readonly Dictionary<string, float> _retry = new Dictionary<string, float>();
        LevelPlayInterstitialAd _inter;
        Action _interDone;
        Request _req;
        string _shown;   // placement do ultimo Show (o ILRD chega depois e e' por ad unit)
        float _initRetry = 10f;
        bool _testSuite, _parado;
        string _resposta;   // Privacidade.Gravar da resposta aplicada no Init; null = SDK nao iniciado (nada do LevelPlay foi tocado)

        public AdsLevelPlay(MonoBehaviour host, bool intersticial)
        {
            _host = host;
            _intersticial = intersticial;
            if (Debug.isDebugBuild) _fake = new AdsSimulado(host, intersticial);
            if (Ads.Resposta(out Faixa f, out bool aceitou)) Iniciar(f, aceitou);
            else Ads.Emit("ad_init", "espera", "consentimento");   // a tela de idade chama o Ads.Responder
        }

        /// <summary>
        /// E-PLAT-01: privacidade da resposta e LevelPlay.Init, uma vez por processo. Ordem da doc (Regulation advanced settings, SDK 9.5):
        /// metadados e LevelPlayPrivacySettings ANTES do Init; tabela em docs/LEVELPLAY.md s6. Resposta diferente depois do Init: GDPR e
        /// CCPA vao na hora, mas COPPA, ID de publicidade e Familias so valem antes do Init e o anuncio ja carregado veio com a resposta
        /// antiga, entao nenhum anuncio ate reabrir o jogo (Ready e Loaded = false).
        /// </summary>
        public void Iniciar(Faixa faixa, bool aceitou)
        {
            var f = Privacidade.Flags(faixa, aceitou);
            string resposta = Privacidade.Gravar(faixa, aceitou);
            PassoSdk passo = Privacidade.Sdk(_resposta, resposta);   // PrivacidadeTests: sem resposta, nunca Inicia
            if (passo == PassoSdk.Espera || passo == PassoSdk.Mantem) return;
            if (passo == PassoSdk.Para)
            {
                LevelPlayPrivacySettings.SetGDPRConsent(f.Consentimento);
                LevelPlayPrivacySettings.SetCCPA(f.NaoVender);
                _parado = true;
                Debug.Log($"ADS privacidade mudou para {Privacidade.Diario(faixa)} gdpr={f.Consentimento} ccpa={f.NaoVender}: sem anuncio ate reabrir");
                return;
            }
            _resposta = resposta;
            if (f.SemId) LevelPlay.SetMetaData("is_deviceid_optout", "true");                // o SDK nao le o ID de publicidade (AAID)
            if (f.Familias) LevelPlay.SetMetaData("Google_Family_Self_Certified_SDKS", "true");   // so redes autocertificadas do Familias
            LevelPlayPrivacySettings.SetCOPPA(f.Coppa);
            LevelPlayPrivacySettings.SetGDPRConsent(f.Consentimento);
            LevelPlayPrivacySettings.SetCCPA(f.NaoVender);
            Debug.Log($"ADS privacidade {Privacidade.Diario(faixa)} coppa={f.Coppa} gdpr={f.Consentimento} ccpa={f.NaoVender} aaid_optout={f.SemId} familias={f.Familias}");
            if (Debug.isDebugBuild)
            {
                LevelPlay.SetAdaptersDebug(true);
                _testSuite = Ads.Arg("-adtestsuite") != null;
                if (_testSuite) LevelPlay.SetMetaData("is_test_suite", "enable");   // so' em dev; antes do Init
            }
            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(Ads.AppKey);
        }

        bool InterBusy => _interDone != null;
        public bool Busy => _req != null || InterBusy || (_fake != null && _fake.Busy);
        public bool Loaded(string p) => !_parado && Units.TryGetValue(p, out string u) && _ads.TryGetValue(u, out LevelPlayRewardedAd ad) && ad != null && ad.IsAdReady();
        // sem resposta (SDK parado) ou resposta mudada nesta sessao: botao escondido, inclusive o fallback simulado do dev
        public bool Ready(string p) => _resposta != null && !_parado && !Busy && Units.ContainsKey(p) && (Loaded(p) || _fake != null);
        bool InterLoaded => !_parado && _inter != null && _inter.IsAdReady();
        public bool InterstitialReady => _intersticial && _resposta != null && !_parado && !Busy && (InterLoaded || _fake != null);

        public void Show(string p, Action onReward, Action onFail)
        {
            bool real = Loaded(p);
            if (!real && _fake != null && !Busy) { _fake.Show(p, onReward, onFail); return; }   // dev sem anuncio carregado: o simulado
            Ads.Emit("ad_request", p, real ? "real" : "simulado");
            string why = !Units.ContainsKey(p) ? "placement_desconhecido" : Busy ? "ocupado" : !real ? "sem_anuncio" : null;
            if (why != null) { Ads.Emit("ad_fail", p, why); onFail?.Invoke(); return; }
            _req = new Request { P = p, Ok = onReward, Fail = onFail };
            _shown = p;
            _ads[Units[p]].ShowAd();
        }

        /// <summary>E-PLAT-03: so com a flag ligada (sem ela o _inter nem existe); dev sem intersticial carregado = o simulado.</summary>
        public void ShowInterstitial(Action done)
        {
            bool real = _intersticial && InterLoaded;
            if (!real && _intersticial && _fake != null && !Busy) { _fake.ShowInterstitial(done); return; }
            Ads.Emit("ad_request", Ads.Intersticial, "real");
            string why = !_intersticial ? "desligado" : Busy ? "ocupado" : !real ? "sem_anuncio" : null;
            if (why != null) { Ads.Emit("ad_fail", Ads.Intersticial, why); done?.Invoke(); return; }
            _interDone = done ?? (() => { });
            _shown = Ads.Intersticial;
            _inter.ShowAd();
        }

        void InterFim(string falha)
        {
            Action d = _interDone;
            if (d == null) return;
            _interDone = null;
            if (falha != null) Ads.Emit("ad_fail", Ads.Intersticial, falha);
            _inter?.LoadAd();
            d();
        }

        void Finish(bool rewarded, string detail)
        {
            Request r = _req;
            if (r == null || r.Done)
            {
                if (rewarded) Ads.Emit("ad_reward_late", "", detail);   // chegou depois da janela de 2 s: nao paga, fica no diario
                return;
            }
            r.Done = true;
            _req = null;
            Ads.Emit(rewarded ? "ad_reward" : "ad_fail", r.P, detail);
            if (rewarded) r.Ok?.Invoke(); else r.Fail?.Invoke();
        }

        void OnInitSuccess(LevelPlayConfiguration config)
        {
            Ads.Emit("ad_init", "ok", LevelPlay.PluginVersion);
            if (Debug.isDebugBuild) LevelPlay.ValidateIntegration();   // relatorio no logcat (tag IntegrationHelper)
            if (_testSuite) LevelPlay.LaunchTestSuite();
            foreach (KeyValuePair<string, string> pu in Units)
            {
                string u = pu.Value, p0 = pu.Key;   // p0 = 1o placement da unit: nome no diario da carga
                if (_ads.ContainsKey(u)) continue;
                var ad = new LevelPlayRewardedAd(u);
                ad.OnAdLoaded += _ => _retry[u] = 30f;
                ad.OnAdLoadFailed += e => LoadFailed(u, p0, e);
                ad.OnAdDisplayed += info => Ads.Emit("ad_shown", _req?.P ?? p0, info?.AdNetwork);
                ad.OnAdImpressionDataReady += d => Ads.Emit("ad_impression", _shown ?? p0, Ads.Receita(d?.Revenue, d?.Precision, d?.AdNetwork));   // DM §95
                ad.OnAdDisplayFailed += (info, e) => { Finish(false, $"exibicao {e?.ErrorCode} {e?.ErrorMessage}"); Load(u); };
                ad.OnAdRewarded += (info, r) => Finish(true, $"{r?.Name} x{r?.Amount} {info?.AdNetwork}");
                ad.OnAdClosed += _ => { Load(u); _host.StartCoroutine(ClosedWithoutReward(_req)); };
                _ads[u] = ad;
                _retry[u] = 30f;
                Load(u);   // pre-carga de proposito: Ready precisa do anuncio pronto antes de o botao aparecer
            }
            // E-PLAT-03: flag desligada (o padrao) = nenhum LevelPlayInterstitialAd, nenhum load (o Init ja logou "ad_init intersticial desligado")
            if (!_intersticial || Ads.InterstitialUnit.Length == 0 || _inter != null) return;
            _inter = new LevelPlayInterstitialAd(Ads.InterstitialUnit);
            _inter.OnAdLoadFailed += e => Ads.Emit("ad_load_fail", Ads.Intersticial, $"{e?.ErrorCode} {e?.ErrorMessage}");
            _inter.OnAdDisplayed += info => Ads.Emit("ad_shown", Ads.Intersticial, info?.AdNetwork);
            _inter.OnAdImpressionDataReady += d => Ads.Emit("ad_impression", Ads.Intersticial, Ads.Receita(d?.Revenue, d?.Precision, d?.AdNetwork));
            _inter.OnAdDisplayFailed += (info, e) => InterFim($"exibicao {e?.ErrorCode} {e?.ErrorMessage}");
            _inter.OnAdClosed += _ => InterFim(null);
            _inter.LoadAd();
        }

        void OnInitFailed(LevelPlayInitError e)
        {
            Ads.Emit("ad_init", "falha", $"{e?.ErrorCode} {e?.ErrorMessage}");
            _host.StartCoroutine(RetryInit(_initRetry));
            _initRetry = Mathf.Min(_initRetry * 2f, 300f);
        }

        static IEnumerator RetryInit(float wait)
        {
            yield return new WaitForSecondsRealtime(wait);
            LevelPlay.Init(Ads.AppKey);
        }

        void Load(string unit)
        {
            if (_ads.TryGetValue(unit, out LevelPlayRewardedAd ad) && ad != null && !ad.IsAdReady()) ad.LoadAd();
        }

        void LoadFailed(string unit, string p, LevelPlayAdError e)
        {
            float wait = _retry.TryGetValue(unit, out float w) ? w : 30f;
            Ads.Emit("ad_load_fail", p, $"{e?.ErrorCode} {e?.ErrorMessage} retry {wait:0}s");
            _retry[unit] = Mathf.Min(wait * 2f, 300f);
            _host.StartCoroutine(LoadLater(unit, wait));
        }

        IEnumerator LoadLater(string unit, float wait)
        {
            yield return new WaitForSecondsRealtime(wait);
            Load(unit);
        }

        // OnAdRewarded pode chegar DEPOIS do OnAdClosed (callbacks assincronos): espera um pouco antes de chamar de falha
        IEnumerator ClosedWithoutReward(Request r)
        {
            yield return new WaitForSecondsRealtime(RewardGrace);
            if (_req == r) Finish(false, "fechou_sem_recompensa");   // so' o pedido que fechou (um novo Show nao e' afetado)
        }

        public void Destroy()
        {
            if (_resposta == null) return;   // SDK nunca iniciou: nada do LevelPlay a tocar
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            foreach (LevelPlayRewardedAd ad in _ads.Values) ad?.DestroyAd();
            _ads.Clear();
            _inter?.DestroyAd();
            _inter = null;
        }
    }
#endif
}
