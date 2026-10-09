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
    /// Anuncio premiado (rewarded) do LevelPlay atras de 3 chamadas: Init, Ready, Show. docs/LEVELPLAY.md.
    /// Placements do jogo = uma ad unit cada no painel: "vip" e "velocidade". Anuncio real so' em aparelho (Android/iOS).
    /// Anuncio SIMULADO (~5 s, "Anuncio de teste", recompensa no fim): no Editor e no Windows, com -fakeads, e em build de
    /// desenvolvimento quando o SDK nao tem anuncio pronto. Build de release sem anuncio pronto: Ready = false (o botao some).
    /// Diario: o evento Logged(evento, placement, detalhe) sai com ad_init, ad_request, ad_shown, ad_reward, ad_fail e
    /// ad_load_fail; o Game assina e grava no diario.csv.
    /// Flags: -fakeads (sempre simulado) e -adtestsuite (abre o Test Suite do LevelPlay; so' em build de desenvolvimento).
    /// No PC vem da linha de comando; no Android, do extra "unity" do intent (adb shell am start ... -e unity "-fakeads").
    /// </summary>
    public sealed class Ads : MonoBehaviour
    {
        public const string AppKey = "28953c84d";   // Android, app "Forge Street" no painel LevelPlay (nao e' segredo: vai no APK)
        public const string Vip = "vip", Velocidade = "velocidade";
        static readonly Dictionary<string, string> Units = new Dictionary<string, string>
        {
            [Vip] = "7dxdva13pxaovoae",          // rewarded_vip: recompensa "VIP" x1
            [Velocidade] = "gple3wvhz7xm3qk7",   // rewarded_velocidade: recompensa "Velocidade" x1
        };
        static readonly Dictionary<string, string> RewardName = new Dictionary<string, string> { [Vip] = "VIP", [Velocidade] = "Velocidade" };
        const float FakeSeconds = 5f, RewardGrace = 2f;

        /// <summary>(evento, placement, detalhe) para o diario. Sem virgulas: o diario e' CSV.</summary>
        public static event Action<string, string, string> Logged;

        static Ads _i;
        static bool _forceFake, _busy;
        sealed class Request { public string P; public Action Ok, Fail; public bool Done; }
        static Request _req;

        /// <summary>Idempotente. Chamar cedo (Game.Awake). No aparelho aplica a privacidade e inicia o SDK.</summary>
        public static void Init()
        {
            if (_i != null) return;
            _forceFake = Arg("-fakeads") != null;
            _i = new GameObject("Ads").AddComponent<Ads>();
            DontDestroyOnLoad(_i.gameObject);
            if (Live) _i.StartSdk();
            else Emit("ad_init", "simulado", _forceFake ? "fakeads" : Application.platform.ToString());
        }

        /// <summary>Anuncio na tela (do Show ate a recompensa/falha): o Game congela a simulacao e nao paga cofre na volta.</summary>
        public static bool Busy => _busy;

        /// <summary>True = SDK real ativo (aparelho Android/iOS, sem -fakeads).</summary>
        public static bool Live => !Application.isEditor && !_forceFake && Platform;

        /// <summary>Ha anuncio para este placement agora (real carregado, ou simulado onde ele vale). False = esconder o botao.</summary>
        public static bool Ready(string placement)
        {
            if (_i == null || _busy || !Units.ContainsKey(placement)) return false;
            return Real(placement) || FakeAllowed;
        }

        /// <summary>True = ha um anuncio REAL carregado para o placement (diagnostico; o jogo usa Ready).</summary>
        public static bool Real(string placement)
        {
#if UNITY_ANDROID || UNITY_IOS
            return Live && _ads.TryGetValue(placement, out LevelPlayRewardedAd ad) && ad != null && ad.IsAdReady();
#else
            return false;
#endif
        }

        /// <summary>
        /// Mostra o anuncio. Chama exatamente UM dos dois: onReward (assistiu ate o fim) ou onFail (sem anuncio, erro, ou
        /// fechou antes da recompensa). Os dois rodam na thread principal.
        /// </summary>
        public static void Show(string placement, Action onReward, Action onFail)
        {
            bool real = Real(placement);
            Emit("ad_request", placement, real ? "real" : "simulado");
            string why = _i == null ? "sem_init" : !Units.ContainsKey(placement) ? "placement_desconhecido" : _busy ? "ocupado"
                : !real && !FakeAllowed ? "sem_anuncio" : null;
            if (why != null) { Emit("ad_fail", placement, why); onFail?.Invoke(); return; }
            _busy = true;
            _req = new Request { P = placement, Ok = onReward, Fail = onFail };
#if UNITY_ANDROID || UNITY_IOS
            if (real) { _ads[placement].ShowAd(); return; }
#endif
            _i.StartCoroutine(_i.Fake(placement));
        }

        static bool Platform =>
#if UNITY_ANDROID || UNITY_IOS
            true;
#else
            false;
#endif

        // simulado vale fora do aparelho, com -fakeads e em build de desenvolvimento; release no aparelho, nunca
        static bool FakeAllowed => !Live || Debug.isDebugBuild;

        static void Finish(bool rewarded, string detail)
        {
            Request r = _req;
            if (r == null || r.Done)
            {
                if (rewarded) Emit("ad_reward_late", "", detail);   // chegou depois da janela de 2 s: nao paga, fica no diario
                return;
            }
            r.Done = true;
            _req = null;
            _busy = false;
            Emit(rewarded ? "ad_reward" : "ad_fail", r.P, detail);
            if (rewarded) r.Ok?.Invoke(); else r.Fail?.Invoke();
        }

        static void Emit(string evt, string a, string b)
        {
            a = (a ?? "").Replace(',', ' '); b = (b ?? "").Replace(',', ' ').Replace('\n', ' ');
            Debug.Log($"ADS {evt} {a} {b}");
            try { Logged?.Invoke(evt, a, b); }
            catch (Exception e) { Debug.LogException(e); }   // o diario nunca derruba o fluxo do anuncio
        }

        /// <summary>Valor da flag (null = ausente, "" = sem valor). Linha de comando + extra "unity" do intent no Android
        /// (so' em build de desenvolvimento: no release o intent e' de qualquer app e -fakeads daria recompensa sem anuncio).</summary>
        public static string Arg(string name)
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
            int i = a.IndexOf(name);
            if (i < 0) return null;
            return i + 1 < a.Count && !a[i + 1].StartsWith("-") ? a[i + 1] : "";
        }

        // ---------- simulado ----------

        IEnumerator Fake(string placement)
        {
            float scale = Time.timeScale;
            Time.timeScale = 0f;   // como o anuncio real (o app fica em pausa por baixo)
            GameObject ui = FakeUi(placement, out Text count);
            Emit("ad_shown", placement, "simulado");
            for (float t = FakeSeconds; t > 0f; t -= Time.unscaledDeltaTime)
            {
                count.text = Mathf.CeilToInt(t).ToString();
                yield return null;
            }
            Destroy(ui);
            Time.timeScale = scale;
            Finish(true, RewardName[placement] + " x1 simulado");
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
            sub.text = "Recompensa: " + RewardName[placement] + " x1";
            count = Art.NewText(root, "Contador", 200, new Vector2(0.3f, 0.36f), new Vector2(0.7f, 0.52f));
            count.fontStyle = FontStyle.Bold;
            Text foot = Art.NewText(root, "Rodape", 32, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.32f));
            foot.text = "Simulado: nenhum anúncio real foi pedido nem exibido";
            foot.color = Art.ComAlfa(Art.Ink, 0.6f);
            return go;
        }

        // ---------- SDK real (LevelPlay 9.5.1) ----------

#if UNITY_ANDROID || UNITY_IOS
        static readonly Dictionary<string, LevelPlayRewardedAd> _ads = new Dictionary<string, LevelPlayRewardedAd>();
        static readonly Dictionary<string, float> _retry = new Dictionary<string, float>();
        float _initRetry = 10f;
        bool _testSuite;

        void StartSdk()
        {
            // Privacidade ANTES do Init (docs/LEVELPLAY.md s5). Conservador ate existir tela de consentimento:
            // sem consentimento GDPR (anuncio nao personalizado), "nao vender" CCPA, e COPPA false = painel "Not directed".
            LevelPlayPrivacySettings.SetCOPPA(false);
            LevelPlayPrivacySettings.SetGDPRConsent(false);
            LevelPlayPrivacySettings.SetCCPA(true);
            if (Debug.isDebugBuild)
            {
                LevelPlay.SetAdaptersDebug(true);
                _testSuite = Arg("-adtestsuite") != null;
                if (_testSuite) LevelPlay.SetMetaData("is_test_suite", "enable");   // so' em dev; antes do Init
            }
            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(AppKey);
        }

        void OnInitSuccess(LevelPlayConfiguration config)
        {
            Emit("ad_init", "ok", LevelPlay.PluginVersion);
            if (Debug.isDebugBuild) LevelPlay.ValidateIntegration();   // relatorio no logcat (tag IntegrationHelper)
            if (_testSuite) LevelPlay.LaunchTestSuite();
            foreach (KeyValuePair<string, string> u in Units)
            {
                if (_ads.ContainsKey(u.Key)) continue;
                string p = u.Key;
                var ad = new LevelPlayRewardedAd(u.Value);
                ad.OnAdLoaded += _ => _retry[p] = 30f;
                ad.OnAdLoadFailed += e => LoadFailed(p, e);
                ad.OnAdDisplayed += info => Emit("ad_shown", p, info?.AdNetwork);
                ad.OnAdDisplayFailed += (info, e) => { Finish(false, $"exibicao {e?.ErrorCode} {e?.ErrorMessage}"); Load(p); };
                ad.OnAdRewarded += (info, r) => Finish(true, $"{r?.Name} x{r?.Amount} {info?.AdNetwork}");
                ad.OnAdClosed += _ => { Load(p); StartCoroutine(ClosedWithoutReward(_req)); };
                _ads[p] = ad;
                _retry[p] = 30f;
                Load(p);   // pre-carga de proposito: Ready precisa do anuncio pronto antes de o botao aparecer
            }
        }

        void OnInitFailed(LevelPlayInitError e)
        {
            Emit("ad_init", "falha", $"{e?.ErrorCode} {e?.ErrorMessage}");
            Invoke(nameof(RetryInit), _initRetry);
            _initRetry = Mathf.Min(_initRetry * 2f, 300f);
        }

        void RetryInit() => LevelPlay.Init(AppKey);

        static void Load(string p)
        {
            if (_ads.TryGetValue(p, out LevelPlayRewardedAd ad) && ad != null && !ad.IsAdReady()) ad.LoadAd();
        }

        void LoadFailed(string p, LevelPlayAdError e)
        {
            float wait = _retry.TryGetValue(p, out float w) ? w : 30f;
            Emit("ad_load_fail", p, $"{e?.ErrorCode} {e?.ErrorMessage} retry {wait:0}s");
            _retry[p] = Mathf.Min(wait * 2f, 300f);
            StartCoroutine(LoadLater(p, wait));
        }

        static IEnumerator LoadLater(string p, float wait)
        {
            yield return new WaitForSecondsRealtime(wait);
            Load(p);
        }

        // OnAdRewarded pode chegar DEPOIS do OnAdClosed (callbacks assincronos): espera um pouco antes de chamar de falha
        static IEnumerator ClosedWithoutReward(Request r)
        {
            yield return new WaitForSecondsRealtime(RewardGrace);
            if (_req == r) Finish(false, "fechou_sem_recompensa");   // so' o pedido que fechou (um novo Show nao e' afetado)
        }

        void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            foreach (LevelPlayRewardedAd ad in _ads.Values) ad?.DestroyAd();
            _ads.Clear();
        }
#else
        void StartSdk() { }
#endif
    }
}
