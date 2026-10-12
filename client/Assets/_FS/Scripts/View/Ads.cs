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
    /// Placements do jogo = uma ad unit cada no painel: "vip" e "velocidade".
    /// Diario: ad_init, ad_request, ad_shown, ad_reward, ad_reward_late, ad_fail e ad_load_fail saem pelo `log` do Init (o Game grava).
    /// Este MonoBehaviour so' hospeda as corrotinas das duas implementacoes.
    /// Flags: -fakeads (sempre simulado) e -adtestsuite (abre o Test Suite do LevelPlay; so' em build de desenvolvimento).
    /// No PC vem da linha de comando; no Android, do extra "unity" do intent (adb shell am start ... -e unity "-fakeads").
    /// </summary>
    public sealed class Ads : MonoBehaviour
    {
        public const string AppKey = "28953c84d";   // Android, app "Forge Street" no painel LevelPlay (nao e' segredo: vai no APK)
        public const string Vip = "vip", Velocidade = "velocidade";
        internal static readonly Dictionary<string, string> RewardName = new Dictionary<string, string> { [Vip] = "VIP", [Velocidade] = "Velocidade" };

        static IAds _i;
        static Action<string, string, string> _log;

        /// <summary>Idempotente. Chamar cedo (Game.Awake). `log`(evento, placement, detalhe) vai para o diario. No aparelho aplica a
        /// privacidade e inicia o SDK.</summary>
        public static IAds Init(Action<string, string, string> log)
        {
            if (_i != null) return _i;
            _log = log;
            Ads host = new GameObject("Ads").AddComponent<Ads>();
            DontDestroyOnLoad(host.gameObject);
            bool fake = Arg("-fakeads") != null;
#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor && !fake) return _i = new AdsLevelPlay(host);
#endif
            Emit("ad_init", "simulado", fake ? "fakeads" : Application.platform.ToString());
            return _i = new AdsSimulado(host);
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
        bool _busy;

        public AdsSimulado(MonoBehaviour host) => _host = host;

        public bool Busy => _busy;
        public bool Loaded(string placement) => Ads.RewardName.ContainsKey(placement);
        public bool Ready(string placement) => !_busy && Loaded(placement);

        public void Show(string placement, Action onReward, Action onFail)
        {
            Ads.Emit("ad_request", placement, "simulado");
            string why = !Loaded(placement) ? "placement_desconhecido" : _busy ? "ocupado" : null;
            if (why != null) { Ads.Emit("ad_fail", placement, why); onFail?.Invoke(); return; }
            _busy = true;
            _host.StartCoroutine(Play(placement, onReward));
        }

        IEnumerator Play(string placement, Action onReward)
        {
            float scale = Time.timeScale;
            Time.timeScale = 0f;   // como o anuncio real (o app fica em pausa por baixo)
            GameObject ui = FakeUi(placement, out Text count);
            Ads.Emit("ad_shown", placement, "simulado");
            for (float t = Seconds; t > 0f; t -= Time.unscaledDeltaTime)
            {
                count.text = Mathf.CeilToInt(t).ToString();
                yield return null;
            }
            UnityEngine.Object.Destroy(ui);
            Time.timeScale = scale;
            _busy = false;
            Ads.Emit("ad_reward", placement, Ads.RewardName[placement] + " x1 simulado");
            onReward?.Invoke();
        }

        static GameObject FakeUi(string placement, out Text count)
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
            title.text = "Anúncio de teste";
            title.fontStyle = FontStyle.Bold;
            title.color = Art.Accent;
            Text sub = Art.NewText(root, "Recompensa", 44, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.62f));
            sub.text = "Recompensa: " + Ads.RewardName[placement] + " x1";
            count = Art.NewText(root, "Contador", 200, new Vector2(0.3f, 0.36f), new Vector2(0.7f, 0.52f));
            count.fontStyle = FontStyle.Bold;
            Text foot = Art.NewText(root, "Rodape", 32, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.32f));
            foot.text = "Simulado: nenhum anúncio real foi pedido nem exibido";
            foot.color = Art.ComAlfa(Art.Ink, 0.6f);
            return go;
        }
    }

#if UNITY_ANDROID || UNITY_IOS
    /// <summary>SDK real (LevelPlay 9.5.1). Em build de desenvolvimento sem anuncio carregado (emulador, conta sem fill), o AdsSimulado
    /// entra no lugar; no release, nunca.</summary>
    sealed class AdsLevelPlay : IAds
    {
        static readonly Dictionary<string, string> Units = new Dictionary<string, string>
        {
            [Ads.Vip] = "7dxdva13pxaovoae",          // rewarded_vip: recompensa "VIP" x1
            [Ads.Velocidade] = "gple3wvhz7xm3qk7",   // rewarded_velocidade: recompensa "Velocidade" x1
        };
        const float RewardGrace = 2f;
        sealed class Request { public string P; public Action Ok, Fail; public bool Done; }

        readonly MonoBehaviour _host;
        readonly AdsSimulado _fake;   // so' em build de desenvolvimento
        readonly Dictionary<string, LevelPlayRewardedAd> _ads = new Dictionary<string, LevelPlayRewardedAd>();
        readonly Dictionary<string, float> _retry = new Dictionary<string, float>();
        Request _req;
        float _initRetry = 10f;
        bool _testSuite;

        public AdsLevelPlay(MonoBehaviour host)
        {
            _host = host;
            if (Debug.isDebugBuild) _fake = new AdsSimulado(host);
            // Privacidade ANTES do Init (docs/LEVELPLAY.md s5). Conservador ate existir tela de consentimento:
            // sem consentimento GDPR (anuncio nao personalizado), "nao vender" CCPA, e COPPA false = painel "Not directed".
            LevelPlayPrivacySettings.SetCOPPA(false);
            LevelPlayPrivacySettings.SetGDPRConsent(false);
            LevelPlayPrivacySettings.SetCCPA(true);
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

        public bool Busy => _req != null || (_fake != null && _fake.Busy);
        public bool Loaded(string p) => _ads.TryGetValue(p, out LevelPlayRewardedAd ad) && ad != null && ad.IsAdReady();
        public bool Ready(string p) => !Busy && Units.ContainsKey(p) && (Loaded(p) || _fake != null);

        public void Show(string p, Action onReward, Action onFail)
        {
            bool real = Loaded(p);
            if (!real && _fake != null && !Busy) { _fake.Show(p, onReward, onFail); return; }   // dev sem anuncio carregado: o simulado
            Ads.Emit("ad_request", p, real ? "real" : "simulado");
            string why = !Units.ContainsKey(p) ? "placement_desconhecido" : Busy ? "ocupado" : !real ? "sem_anuncio" : null;
            if (why != null) { Ads.Emit("ad_fail", p, why); onFail?.Invoke(); return; }
            _req = new Request { P = p, Ok = onReward, Fail = onFail };
            _ads[p].ShowAd();
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
            foreach (KeyValuePair<string, string> u in Units)
            {
                if (_ads.ContainsKey(u.Key)) continue;
                string p = u.Key;
                var ad = new LevelPlayRewardedAd(u.Value);
                ad.OnAdLoaded += _ => _retry[p] = 30f;
                ad.OnAdLoadFailed += e => LoadFailed(p, e);
                ad.OnAdDisplayed += info => Ads.Emit("ad_shown", p, info?.AdNetwork);
                ad.OnAdDisplayFailed += (info, e) => { Finish(false, $"exibicao {e?.ErrorCode} {e?.ErrorMessage}"); Load(p); };
                ad.OnAdRewarded += (info, r) => Finish(true, $"{r?.Name} x{r?.Amount} {info?.AdNetwork}");
                ad.OnAdClosed += _ => { Load(p); _host.StartCoroutine(ClosedWithoutReward(_req)); };
                _ads[p] = ad;
                _retry[p] = 30f;
                Load(p);   // pre-carga de proposito: Ready precisa do anuncio pronto antes de o botao aparecer
            }
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

        void Load(string p)
        {
            if (_ads.TryGetValue(p, out LevelPlayRewardedAd ad) && ad != null && !ad.IsAdReady()) ad.LoadAd();
        }

        void LoadFailed(string p, LevelPlayAdError e)
        {
            float wait = _retry.TryGetValue(p, out float w) ? w : 30f;
            Ads.Emit("ad_load_fail", p, $"{e?.ErrorCode} {e?.ErrorMessage} retry {wait:0}s");
            _retry[p] = Mathf.Min(wait * 2f, 300f);
            _host.StartCoroutine(LoadLater(p, wait));
        }

        IEnumerator LoadLater(string p, float wait)
        {
            yield return new WaitForSecondsRealtime(wait);
            Load(p);
        }

        // OnAdRewarded pode chegar DEPOIS do OnAdClosed (callbacks assincronos): espera um pouco antes de chamar de falha
        IEnumerator ClosedWithoutReward(Request r)
        {
            yield return new WaitForSecondsRealtime(RewardGrace);
            if (_req == r) Finish(false, "fechou_sem_recompensa");   // so' o pedido que fechou (um novo Show nao e' afetado)
        }

        public void Destroy()
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            foreach (LevelPlayRewardedAd ad in _ads.Values) ad?.DestroyAd();
            _ads.Clear();
        }
    }
#endif
}
