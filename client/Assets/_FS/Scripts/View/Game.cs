using System;
using System.Collections;
using System.Globalization;
using System.IO;
using FS.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FS
{
    /// <summary>
    /// Fluxo do jogo, o unico MonoBehaviour de fluxo: carrega/grava o Sim (PlayerPrefs), aplica o cofre offline, le o
    /// joystick flutuante (e WASD no PC), avanca a simulacao em passos de ate 1/30 s, toca SFX pelos eventos, segue o
    /// jogador com a camera em retrato, monta a HUD por codigo e grava o diario de playtest
    /// (persistentDataPath/diario.csv). Nasce sozinho em qualquer cena.
    /// Flags de dev: -autoplay [min] (bot sem render, loga AUTOPLAY por minuto, sai 0/1) | -shot foto.png
    /// [-shotdelay s] [-shotchest] [-bot] [-menu] | -speed N | -reset (apaga o save) | -testsession (sem persistencia) |
    /// -record pasta [-recordsec S] [-recordfps F] (quadros 1080x1920 para os criativos, docs/CRIATIVOS.md) | -buyids 1,5,0@300 |
    /// -warmup S | -hold 0,3,3,3,0,0 | -stock 10,10,10 (fotos de validacao) | -fakeads | -adtest vip|velocidade (Ads.Show no
    /// inicio; docs/LEVELPLAY.md) | -vipnow (chama o VIP depois do warmup) | -boost N (N anuncios de velocidade antes do warmup).
    /// v0.5c (docs/FASE8_VIP_VELOCIDADE.md s5): botoes "Chamar VIP" e "Velocidade 2x/3x" nos cantos da barra de baixo (Ads.Show ->
    /// Sim.SummonVip / Sim.StartBoost), selo do boost com cronometro, aviso "Cliente VIP!" e diario vip_* / boost_start.
    /// </summary>
    public sealed class Game : MonoBehaviour
    {
        const string SaveKey = "fs.save";
        const float SaveEvery = 5f, MaxStep = 1f / 30f;
        const float Margin = 1.2f;                               // m alem do conteudo nas bordas: pad e rotulo nunca cortados (foto 01)
        // Largura visivel = oficina + margens (11,4 m): em retrato a camera cabe a oficina INTEIRA (coordenador,
        // 2026-10-06, fotos 05-08: com 7,6 m visiveis o mundo de 9 m cortava uma coluna de cada lado em qualquer clamp).
        // 2a area (docs/AREA2_JOALHERIA.md s1): mesma escala; a rua lateral entra seguindo o jogador depois do Corredor.
        const float VisibleWidth = Balance.WorkshopW + 2f * Margin;
        // fracao da tela reservada a faixa de cima; o mundo nao chega la. v0.5b (BENCHMARK P1-1): 0,15 -> 0,075 (~144 px na referencia),
        // so 2 pilulas (ouro | dica) sem painel cheio e sem o numero de versao
        const float HudBand = 0.075f;
        const float GoldRoll = 12f;                              // 1/s do numero rolando: ~0,3 s ate o valor novo (P0-5)
        // topo do conteudo: fila de clientes (pe em y 14) + balao grande do 1o da fila (v0.5: topo do anel em ~16,05 m) + "+10"
        const float ContentTop = Balance.WorldH + 2.1f;
        // Moedas da venda (BENCHMARK_VISUAL P0-5, versao visual): saem do cliente e voam ate a moeda da HUD; o numero so sobe quando
        // elas chegam (o ouro do Sim ja mudou: mostra Gold - _pending) e a moeda e o numero pulsam 1 -> 1,15 -> 1.
        const float CoinFlight = 0.4f, CoinGap = 0.04f, CoinPx = 56f, PunchTime = 0.2f;
        const int CoinPool = 30;

        Sim _sim;
        Bot _bot;
        WorldView _view;
        Joystick _joy;
        MenuBar _menu;
        Camera _cam;
        float _speed = 1f, _saveT, _hintT, _minuteT;
        bool _botDrive, _headless, _firstSaleLogged, _testSession, _started;
        string _diary, _sid;
        Vector2Int _screen;
        readonly System.Collections.Generic.List<(int u, float t)> _buyAt = new System.Collections.Generic.List<(int u, float t)>();   // -buyids id@t

        Canvas _joyCanvas;
        RectTransform _canvas, _labels, _safe, _panel, _fx;
        Text _gold, _hint, _panelTitle, _panelBody, _panelBtn;
        Action _panelAction;
        Image _coinIcon;
        sealed class Coin { public Image I; public Vector3 From; public float T; public int Value; }   // T < 0: esperando a vez
        readonly System.Collections.Generic.List<Coin> _coins = new System.Collections.Generic.List<Coin>();
        int _pending, _goldInt = -1; float _punchT, _goldShown = -1f;
        // v0.5c: botoes de anuncio, selo do boost e avisos curtos (VIP chegou / anuncio indisponivel)
        sealed class AdBtn { public Button B; public Image Face, Icon; public GameObject Video; public Text Label; }
        sealed class Banner { public RectTransform R; public CanvasGroup G; public Text T; public Image I; public float Age = 99f, Dur; }
        AdBtn _vipBtn, _speedBtn;
        RectTransform _boostSeal; Text _boostMul, _boostTime;
        Banner _vipBanner, _toast;
        bool _vipNow;
        static readonly Color AdFace = Art.Hex(0x3B2650), AdOff = Art.Hex(0x5A5560);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            _testSession = Arg("-testsession") != null || Arg("-autoplay") != null || Arg("-record") != null;
            if (Arg("-record") != null) Time.captureFramerate = Mathf.Max(1, (int)ArgF("-recordfps", 30f));   // relogio do jogo = quadro do video
            _sid = Guid.NewGuid().ToString("N").Substring(0, 8);
            _diary = Path.Combine(Application.persistentDataPath, "diario.csv");
            Sfx.Init(gameObject);
            // ponytail: fotos e smoke usam estado novo sem ler ou gravar o save/diario normal.
            if (!_testSession && Arg("-reset") != null) PlayerPrefs.DeleteKey(SaveKey);
            _sim = _testSession ? new Sim() : Sim.Load(PlayerPrefs.GetString(SaveKey, ""));
            DevArgs();
            Ads.Logged += (e, a, b) => Log(e, a, b);   // pedido, mostrado, recompensa e falha do anuncio vao para o diario
            Ads.Init();
            _bot = new Bot();
            string sp = Arg("-speed");
            if (!string.IsNullOrEmpty(sp) && float.TryParse(sp, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) _speed = Mathf.Clamp(v, 0.1f, 50f);

            _cam = new GameObject("Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Art.Bg;
            _cam.transform.position = new Vector3(Balance.WorkshopW / 2f, Balance.WorldH / 2f, -10f);
            BuildHud();
            _view = new GameObject("Mundo").AddComponent<WorldView>();
            _view.Init(_sim, _cam, _labels);
            _menu = new MenuBar(_safe, _sim, u => Bought(u, Upgrades.Cost(u), _sim.Player.Pos));
            _panel.SetAsLastSibling();   // o painel modal cobre a barra de melhorias
            if (Arg("-menu") != null) _menu.Toggle();   // foto com a fileira aberta
            _joy = new Joystick(_joyCanvas.transform);
            _joy.TopBand = HudBand;
            Fit();
            FollowCamera(100f);
        }

        void Start()
        {
            if (Arg("-autoplay") != null) { Autoplay(); return; }
            Log("session_start", Application.version, SystemInfo.deviceModel.Replace(",", " "));
            Offline();
            _started = true;
            _botDrive = Arg("-bot") != null;
            string shot = Arg("-shot");
            if (!string.IsNullOrEmpty(shot)) StartCoroutine(Shot(shot));
            string rec = Arg("-record");
            if (!string.IsNullOrEmpty(rec)) StartCoroutine(Record(rec));
            string ad = Ads.Arg("-adtest");   // Ads.Arg tambem le o extra "unity" do intent no Android
            if (!string.IsNullOrEmpty(ad)) StartCoroutine(AdTest(ad));
        }

        /// <summary>-adtest vip|velocidade (dev): mostra o anuncio no inicio; no aparelho espera ate 15 s o real carregar.</summary>
        IEnumerator AdTest(string p)
        {
            for (float t = 0f; Ads.Live && !Ads.Real(p) && t < 15f; t += Time.unscaledDeltaTime) yield return null;
            Ads.Show(p, () => ShowPanel("Anúncio de teste", $"Recompensa recebida: {p}", "OK", ClosePanel),
                () => ShowPanel("Anúncio de teste", $"Sem recompensa: {p}", "OK", ClosePanel));
        }

        /// <summary>Cofre: o claim e' idempotente pelo SavedAt do save (o Core recusa id repetido ou mais antigo).</summary>
        void Offline()
        {
            if (_sim.SavedAt <= 0) return;
            long elapsed = Now() - _sim.SavedAt;
            int gold = _sim.ApplyOffline(elapsed, _sim.SavedAt);
            if (gold <= 0) return;
            long shown = Math.Min(elapsed, (long)Balance.OfflineCapSeconds);
            Log("offline_claim", gold.ToString(), shown.ToString());
            Sfx.Play("offline");
            ShowPanel("Seu cofre rendeu", $"{gold} de ouro em {Clock(shown)} fora da oficina.\n(25% da produção, até 2 h. Limite do cofre: {_sim.OfflineMaxGold()} ouro)", "PEGAR", ClosePanel);
            Save();
        }

        void Update()
        {
            if (_headless) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Save(); Application.Quit(); return; }
            if (_screen.x != Screen.width || _screen.y != Screen.height) Fit();

            bool modal = _panel.gameObject.activeSelf;
            _joy.Blocked = modal;
            _joy.BottomPx = _menu.TopPx;
            _joy.Tick();
            Vector2 input = _joy.Direction;
            Keyboard k = Keyboard.current;
            if (k != null && !modal)
            {
                float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
                float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
                if (x != 0f || y != 0f) input = new Vector2(x, y);
            }

            float dt = Mathf.Min(Time.deltaTime, 0.1f) * _speed;
            while (dt > 0f)
            {
                float step = Mathf.Min(dt, MaxStep);
                dt -= step;
                if (_botDrive) _bot.Step(_sim, step);
                else _sim.Tick(step, input.x, input.y);
                TimedBuys();   // depois do Tick: o Ev.Bought fica em Events e o HandleEvents toca o som e o "+nome!"
                HandleEvents();
            }

            if (_vipNow && _sim.CanSummonVip) _vipNow = !_sim.SummonVip();
            _view.Refresh(Time.deltaTime);
            FollowCamera(Time.deltaTime);
            RefreshHud();
            _menu.Refresh(Time.deltaTime);

            _saveT += Time.deltaTime;
            if (_saveT >= SaveEvery) { _saveT = 0f; Save(); }
            _minuteT += Time.deltaTime * _speed;
            if (_minuteT >= 60f)
            {
                _minuteT -= 60f;
                float stall = 0f, starve = 0f;
                foreach (Station s in _sim.Stations) { stall += s.StallSeconds; starve += s.StarveSeconds; }
                Log("queue_length", _sim.Queue.Count.ToString(), _sim.MaxQueue.ToString());
                Log("bottleneck_seconds", stall.ToString("0", CultureInfo.InvariantCulture), starve.ToString("0", CultureInfo.InvariantCulture));
                Log("walk_no_decision", _sim.WalkNoDecision.ToString("0", CultureInfo.InvariantCulture), _sim.UpgradesBought.ToString());
            }
        }

        void HandleEvents()
        {
            foreach (SimEvent e in _sim.Events)
            {
                switch (e.Kind)
                {
                    case Ev.Picked: Sfx.Play(e.A == (int)Item.Ore ? "ore" : "drop", e.B < 0 ? 1f : 0.8f, 0.08f); break;
                    case Ev.Deposited: Sfx.Play("drop", 1.1f, 0.08f); break;
                    case Ev.Crafted:
                        Sfx.Play(e.A == (int)Item.Ingot ? "furnace" : "hammer", 1f, 0.12f);
                        Log("product_crafted", Balance.ItemName[e.A], _sim.Stations[e.B].Name);
                        break;
                    case Ev.Sold:
                        Sfx.Play("coin", 1f + 0.05f * (_sim.Sales % 5), 0.1f);
                        _view.Float(new V2(e.Pos.X, e.Pos.Y - 0.9f), "+" + e.B, Art.Accent, 44);   // v0.5: sobe pela frente do estande, fora do balao do 1o
                        _view.Sold(e.Pos, e.A == (int)Item.Jewel);   // v0.5b: balao estoura com coracao e brilhos
                        FlyCoins(e.Pos, e.B);
                        Log("product_sold", Balance.ItemName[e.A], e.B.ToString());
                        break;
                    case Ev.Bought: Bought(e.A, e.B, e.Pos); break;
                    case Ev.ClientArrived: Sfx.Play("client", 1f, 0.2f); break;
                    case Ev.ClientLeft:
                        if (e.B == 0) { Sfx.Play("leave"); _view.Float(e.Pos, "...", Art.Bad); }
                        else _view.TurnedAway(e.Pos, e.A == (int)Item.Jewel);   // fila cheia: aparece passando e vai embora
                        Log("client_left", Balance.ItemName[e.A], e.B == 0 ? "cansou" : "fila_cheia");
                        break;
                    case Ev.Bottleneck: Log("bottleneck", _sim.Stations[e.A].Name, "saida_cheia"); break;
                    case Ev.Hired: Sfx.Play("hire"); Log("worker_hired", e.A.ToString()); break;
                    case Ev.Unlocked: Log("station_unlock", _sim.Stations[e.B].Name); break;
                    // baus de marco (docs/AREA2_FASE2.md s3/s4): bateu = som de moeda agudo + o ferreiro comemora (o bau aparece
                    // pulsando no WorldView); abriu = moedas e "+ouro" como uma venda, e o bau some
                    case Ev.Milestone:
                        Sfx.Play("coin", 1.4f);
                        _view.Cheer();
                        Log("milestone", e.A.ToString(), e.B.ToString());
                        break;
                    case Ev.ChestOpened:
                        Sfx.Play("coin", 1f + 0.05f * (_sim.Sales % 5), 0.1f);
                        _view.Float(e.Pos, "+" + e.B, Art.Accent, 44);
                        FlyCoins(e.Pos, e.B);
                        Log("chest_opened", e.A.ToString(), e.B.ToString());
                        break;
                    // FASE8 (v0.5c): VIP entrou na vaga / levou o pacote (cada unidade ja saiu como Sold) / cansou
                    case Ev.VipArrived:
                        Sfx.Play("vip");
                        ShowBanner(_vipBanner, "Cliente VIP!", 2f);
                        _view.VipBurst(e.Pos, false);
                        Log("vip_arrived", Balance.ItemName[e.A], e.B.ToString());
                        break;
                    case Ev.VipServed:
                        Sfx.Play("offline", 1.15f);
                        _view.Float(new V2(e.Pos.X, e.Pos.Y - 1.2f), "+" + e.B, Art.Accent, 64);   // pela frente do estande: na fila ele subia para baixo da HUD
                        _view.VipBurst(e.Pos, true);
                        FlyCoins(e.Pos, e.B, 12);   // so visual: o ouro ja entrou unidade por unidade
                        Log("vip_served", Balance.ItemName[e.A], e.B.ToString());
                        break;
                    case Ev.VipLeft:
                        Sfx.Play("leave");
                        _view.Float(e.Pos, "...", Art.Bad, 44);
                        Log("vip_left", Balance.ItemName[e.A], e.B.ToString());
                        break;
                }
            }
            if (!_firstSaleLogged && _sim.FirstSaleTime >= 0f)
            {
                _firstSaleLogged = true;
                Log("first_sale", _sim.FirstSaleTime.ToString("0.0", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Compra (pad, bot ou toque no menu): som, "+nome!" no mundo e diario.</summary>
        void Bought(int u, int price, V2 pos)
        {
            Sfx.Play("upgrade");
            _view.Float(pos, Upgrades.All[u].Name + "!", Art.Good, 44);
            Log("upgrade_buy", ((Upgrade)u).ToString(), price.ToString());
        }

        // ---------- camera e HUD ----------

        void Fit()
        {
            _screen = new Vector2Int(Screen.width, Screen.height);
            AreaSegura.AncorarDentro(_safe, AreaSegura.Atual());
        }

        /// <summary>
        /// Ortografica em retrato com a oficina inteira visivel (VisibleWidth = oficina + margens). Antes do Corredor o
        /// limite direito e' a oficina: os clamps colapsam no centro dela e a camera fica parada (a rua escura e o arco
        /// aparecem na borda como teaser). Depois do Corredor segue o X do jogador presa ao mapa inteiro [0, WorldW] com
        /// `Margin` de folga (AREA2 s1). A faixa da HUD no topo e' reservada: o balcao, a fila e o "+10" nunca ficam
        /// embaixo do painel de ouro.
        /// </summary>
        void FollowCamera(float dt)
        {
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            float size = VisibleWidth / (2f * aspect);
            _cam.orthographicSize = size;
            float halfW = size * aspect, halfH = size;
            V2 p = _sim.Player.Pos;
            float right = _sim.Bought[(int)Upgrade.SideCorridor] ? Balance.WorldW : Balance.WorkshopW;
            float minX = halfW - Margin, maxX = right - halfW + Margin;
            float minY = halfH - Margin - 2f * halfH * MenuBar.Band;     // borda de baixo em -Margin, acima da barra de melhorias
            float maxY = ContentTop - halfH + 2f * halfH * HudBand;       // topo do conteudo encosta na base da faixa da HUD
            float tx = minX > maxX ? right / 2f : Mathf.Clamp(p.X, minX, maxX);
            float ty = minY > maxY ? (minY + maxY) / 2f : Mathf.Clamp(p.Y + 0.6f, minY, maxY);
            Vector3 target = new Vector3(tx, ty, -10f);
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, target, 1f - Mathf.Exp(-6f * dt));
        }

        void BuildHud()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = canvasGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight = 0.5f;
            _canvas = (RectTransform)canvasGo.transform;
            // vinheta (P1-6): bordas -30%, por cima do mundo e por baixo de rotulos, HUD e painel (1 quad de tela cheia)
            Image vig = Art.Node(_canvas, "Vinheta", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            vig.sprite = Art.Vignette(); vig.color = new Color(0.05f, 0.02f, 0f, 0.3f); vig.raycastTarget = false;
            _labels = Art.Node(_canvas, "Rotulos", Vector2.zero, Vector2.one);   // rotulos do mundo ficam ABAIXO da HUD e do painel modal
            _safe = Art.Node(_canvas, "AreaSegura", Vector2.zero, Vector2.one);

            // Faixa de cima (BENCHMARK P1-1): pilula de ouro (borda clara, fundo #2A1E14, moeda 3D saindo pela esquerda, numero
            // grande com contorno) e pilula da dica ao lado. Ocupa a faixa HudBand, que a camera reserva: nada do mundo passa aqui.
            RectTransform top = Art.Node(_safe, "Topo", new Vector2(0.02f, 1f - HudBand), new Vector2(0.98f, 0.995f));
            Image pill = Art.Panel(top, "PilulaOuro", Art.Hex(0xF2D9A0), new Vector2(0.06f, 0.1f), new Vector2(0.36f, 0.9f));
            Image pillIn = Art.Panel(pill.transform, "Fundo", Art.ComAlfa(Art.Hex(0x2A1E14), 0.95f), Vector2.zero, Vector2.one);
            pillIn.rectTransform.offsetMin = new Vector2(5f, 5f); pillIn.rectTransform.offsetMax = new Vector2(-5f, -5f);
            pill.raycastTarget = pillIn.raycastTarget = false;
            _coinIcon = Art.Node(top, "Moeda", new Vector2(0f, -0.05f), new Vector2(0.14f, 1.05f)).gameObject.AddComponent<Image>();
            Sprite moeda = Art.Icon("moeda", "icone");   // v0.5: moeda renderizada; sem a folha, o disco amarelo de sempre
            _coinIcon.sprite = moeda != null ? moeda : Art.Disc(); _coinIcon.color = moeda != null ? Color.white : Art.Accent;
            _coinIcon.preserveAspect = true; _coinIcon.raycastTarget = false;
            _gold = Art.Outlined(Art.NewText(pill.transform, "Ouro", 62, new Vector2(0.3f, 0f), new Vector2(0.98f, 1f), TextAnchor.MiddleLeft), 3f);
            _gold.fontStyle = FontStyle.Bold;
            _gold.rectTransform.pivot = new Vector2(0f, 0.5f);   // o pulso cresce a partir da moeda, sem empurrar o numero
            Image hintPill = Art.Panel(top, "PilulaDica", Art.ComAlfa(Art.Bg, 0.92f), new Vector2(0.385f, 0.1f), new Vector2(1f, 0.9f));
            hintPill.raycastTarget = false;
            _hint = Art.Outlined(Art.NewText(hintPill.transform, "Dica", 32, Vector2.zero, Vector2.one), 2f);
            _hint.rectTransform.offsetMin = new Vector2(18f, 4f); _hint.rectTransform.offsetMax = new Vector2(-18f, -4f);
            _hint.fontStyle = FontStyle.Bold; _hint.color = Art.Accent;
            _hint.resizeTextForBestFit = true; _hint.resizeTextMinSize = 22; _hint.resizeTextMaxSize = 32;   // 2 linhas no maximo
            _hint.verticalOverflow = VerticalWrapMode.Truncate;
            // versao: saiu do titulo; canto de baixo a esquerda, discreta (criativo: nenhuma)
            if (Arg("-record") == null)
            {
                Text ver = Art.NewText(_safe, "Versao", 20, new Vector2(0.005f, MenuBar.Band), new Vector2(0.17f, MenuBar.Band + 0.016f), TextAnchor.LowerLeft);
                ver.text = "v" + Application.version;
                ver.color = Art.ComAlfa(Art.Ink, 0.3f);
            }

            _panel = Art.Node(_safe, "Painel", Vector2.zero, Vector2.one);
            _panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            Image box = Art.Panel(_panel, "Caixa", Art.Pad, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.66f));
            _panelTitle = Art.NewText(box.transform, "Titulo", 60, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.95f));
            _panelTitle.fontStyle = FontStyle.Bold;
            _panelBody = Art.NewText(box.transform, "Corpo", 38, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.68f));
            Button ok = Art.NewButton(box.transform, "OK", 52, Art.Accent, new Vector2(0.22f, 0.07f), new Vector2(0.78f, 0.3f), () => _panelAction?.Invoke());
            _panelBtn = ok.GetComponentInChildren<Text>();
            _panel.gameObject.SetActive(false);
            BuildAdUi();
            _fx = Art.Node(_canvas, "Moedas", Vector2.zero, Vector2.one);   // depois da AreaSegura: as moedas passam por cima da faixa da HUD

            // Canvas do joystick em escala 1 (px = px): o dp do ARKANA vale direto.
            var joyGo = new GameObject("JoystickCanvas", typeof(Canvas));
            _joyCanvas = joyGo.GetComponent<Canvas>();
            _joyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _joyCanvas.sortingOrder = 2;
        }

        /// <summary>3-6 moedas saem do ponto da venda (acima do cliente) em fila de 0,04 s; o valor e' dividido entre elas.</summary>
        /// <param name="burst">&gt; 0: N moedas so de enfeite (o ouro ja foi contado, ex.: VIP servido); 0: 3-6 moedas que levam o valor.</param>
        void FlyCoins(V2 at, int value, int burst = 0)
        {
            int n = burst > 0 ? burst : Mathf.Clamp(value / 8, 3, 6), given = 0;
            Vector3 from = _cam.WorldToScreenPoint(new Vector3(at.X, at.Y + 0.8f, 0f));
            from.z = 0f;
            for (int i = 0; i < n; i++)
            {
                int share = burst > 0 ? 0 : i == n - 1 ? value - given : value / n;
                Coin c = null;
                foreach (Coin x in _coins) if (!x.I.enabled) { c = x; break; }
                if (c == null && _coins.Count < CoinPool)
                {
                    Image img = Art.Node(_fx, "Moeda", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).gameObject.AddComponent<Image>();
                    img.sprite = _coinIcon.sprite; img.color = _coinIcon.color; img.raycastTarget = false;
                    img.rectTransform.sizeDelta = Vector2.one * CoinPx;
                    _coins.Add(c = new Coin { I = img });
                }
                if (c == null) break;   // ponytail: pico de vendas sem moeda livre = o resto entra direto no numero
                given += share;
                c.I.enabled = true; c.From = from; c.T = -i * CoinGap; c.Value = share;
                c.I.rectTransform.position = from;
                c.I.transform.localScale = Vector3.zero;   // aparece quando chega a vez dela
            }
            _pending += given;
        }

        /// <summary>Voo em arco (quadratica, pico 18% da tela acima da reta) ate a moeda da HUD; chegou = soma no numero e pulsa.</summary>
        void TickCoins(float dt)
        {
            Vector3 to = _coinIcon.rectTransform.position;   // canvas overlay: posicao = px de tela
            foreach (Coin c in _coins)
            {
                if (!c.I.enabled) continue;
                c.T += dt;
                if (c.T < 0f) continue;
                float t = Mathf.Clamp01(c.T / CoinFlight), u = 1f - t;
                Vector3 mid = (c.From + to) * 0.5f + new Vector3(0f, Screen.height * 0.18f, 0f);
                c.I.rectTransform.position = u * u * c.From + 2f * u * t * mid + t * t * to;
                c.I.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, t);
                if (t < 1f) continue;
                c.I.enabled = false;
                _pending = Mathf.Max(0, _pending - c.Value);
                _punchT = PunchTime;
            }
            _punchT = Mathf.Max(0f, _punchT - dt);
            float s = 1f + 0.15f * Mathf.Sin(Mathf.PI * (1f - _punchT / PunchTime));
            _coinIcon.transform.localScale = _gold.transform.localScale = Vector3.one * s;
        }

        void RefreshHud()
        {
            TickCoins(Time.deltaTime);
            RefreshAdUi(Mathf.Min(Time.unscaledDeltaTime, 0.1f));   // tempo real (o simulado zera o timeScale), sem o salto do 1o quadro
            // numero rolando: persegue o valor (sobe na venda, desce na compra) em ~0,3 s em vez de pular
            int target = Mathf.Max(0, _sim.Gold - _pending);
            _goldShown = _goldShown < 0f ? target : Mathf.Lerp(target, _goldShown, Mathf.Exp(-GoldRoll * Time.deltaTime));
            if (Mathf.Abs(_goldShown - target) < 0.5f) _goldShown = target;
            int shown = Mathf.RoundToInt(_goldShown);
            if (shown != _goldInt) { _goldInt = shown; _gold.text = shown.ToString(); }
            _hintT -= Time.deltaTime;
            if (_hintT > 0f) return;
            _hintT = 0.25f;
            Hint h = _sim.CurrentHint();
            _hint.text = HintText(h, _sim.HintArg);
            _view.ShowHint(h, _sim.HintArg);   // seta no mundo sobre o alvo (P1-1)
        }

        string HintText(Hint h, int arg)
        {
            switch (h)
            {
                case Hint.BuyPad:
                    Pad p = _sim.Pads[arg];
                    int u = p.Current(_sim);
                    return u < 0 ? "" : $"Pise na placa: {Upgrades.All[u].Name} ({Upgrades.Cost(u) - p.Paid} de ouro)";
                case Hint.BuyMenu: return $"Toque em Melhorias: {Upgrades.All[arg].Name} ({Upgrades.Cost(arg)} de ouro)";
                case Hint.ProductToCounter: return arg == (int)Item.Jewel ? "Leve joias à loja de joias" : $"Leve {Balance.ItemName[arg]}s ao balcão";
                case Hint.IngotToCrafter: return "Leve os lingotes à bigorna";
                case Hint.OreToFurnace: return "Leve o minério à fornalha";
                case Hint.PickProducts: return $"Produto pronto: pegue {Balance.ItemName[arg]}s na bancada";
                case Hint.PickIngots: return "Pegue os lingotes na fornalha";
                case Hint.ClientWaiting: return $"Cliente esperando por {Balance.ItemName[arg]}!";
                case Hint.OpenChest: return $"Abra o baú: {_sim.Chests[arg].Label}";
                default: return "Pegue minério no depósito";
            }
        }

        // ---------- anuncios: Chamar VIP, Velocidade 2x/3x, selo do boost e avisos (v0.5c) ----------

        /// <summary>
        /// Botoes nos cantos da barra de baixo (fora da fila e do joystick; a base sobe 2,6% para a marca "Development Build" do APK de
        /// teste nao cobrir a legenda), selo do boost logo acima do botao de velocidade (no topo ele batia no balao do 1o da fila, que vai
        /// para a esquerda com 8 vagas) e 2 avisos.
        /// </summary>
        void BuildAdUi()
        {
            float band = MenuBar.Band;
            _vipBtn = AdButton("ChamarVip", new Vector2(0.02f, 0.026f), new Vector2(0.165f, band - 0.004f), Art.Icon("coroa", "icone") ?? Art.Star(), "VIP", OnVipAd);
            _speedBtn = AdButton("Velocidade", new Vector2(0.835f, 0.026f), new Vector2(0.98f, band - 0.004f), Art.FastForward(), "2×", OnSpeedAd);
            _speedBtn.Icon.color = Art.Accent;
            // selo do boost: borda de ouro, avanco rapido, "2x" grande e o cronometro
            Image seal = Art.Panel(_safe, "SeloBoost", Art.Gold, new Vector2(0.69f, band + 0.018f), new Vector2(0.98f, band + 0.07f));
            seal.raycastTarget = false;
            Image sealIn = Art.Panel(seal.transform, "Fundo", Art.ComAlfa(AdFace, 0.95f), Vector2.zero, Vector2.one);
            sealIn.rectTransform.offsetMin = new Vector2(5f, 5f); sealIn.rectTransform.offsetMax = new Vector2(-5f, -5f);
            sealIn.raycastTarget = false;
            Image ff = Art.Node(seal.transform, "Icone", new Vector2(0.05f, 0.2f), new Vector2(0.27f, 0.8f)).gameObject.AddComponent<Image>();
            ff.sprite = Art.FastForward(); ff.color = Art.Accent; ff.preserveAspect = true; ff.raycastTarget = false;
            _boostMul = Art.Outlined(Art.NewText(seal.transform, "Mult", 50, new Vector2(0.27f, 0f), new Vector2(0.58f, 1f)), 3f);
            _boostMul.fontStyle = FontStyle.Bold; _boostMul.color = Art.Accent;
            _boostTime = Art.Outlined(Art.NewText(seal.transform, "Tempo", 36, new Vector2(0.56f, 0f), new Vector2(0.97f, 1f)), 2f);
            _boostTime.fontStyle = FontStyle.Bold;
            _boostSeal = seal.rectTransform;
            _boostSeal.gameObject.SetActive(false);
            _vipBanner = MakeBanner("AvisoVip", new Vector2(0.14f, 0.63f), new Vector2(0.86f, 0.7f), Art.Icon("coroa", "icone"), 52);
            _toast = MakeBanner("AvisoAnuncio", new Vector2(0.15f, MenuBar.Band + 0.08f), new Vector2(0.85f, MenuBar.Band + 0.12f), null, 34);   // acima do selo do boost
        }

        /// <summary>Botao quadrado: borda de ouro, fundo roxo escuro, icone, legenda e o selo de video (anuncio) no canto de cima.</summary>
        AdBtn AdButton(string name, Vector2 min, Vector2 max, Sprite icon, string label, Action onClick)
        {
            var a = new AdBtn();
            Image edge = Art.Panel(_safe, name, Art.Gold, min, max);
            a.B = edge.gameObject.AddComponent<Button>();
            a.B.targetGraphic = edge;
            a.B.onClick.AddListener(() => onClick());
            a.Face = Art.Panel(edge.transform, "Face", AdFace, Vector2.zero, Vector2.one);
            a.Face.rectTransform.offsetMin = new Vector2(5f, 5f); a.Face.rectTransform.offsetMax = new Vector2(-5f, -5f);
            a.Face.raycastTarget = false;
            a.Icon = Art.Node(edge.transform, "Icone", new Vector2(0.16f, 0.36f), new Vector2(0.84f, 0.94f)).gameObject.AddComponent<Image>();
            a.Icon.sprite = icon; a.Icon.preserveAspect = true; a.Icon.raycastTarget = false;
            a.Label = Art.Outlined(Art.NewText(edge.transform, "Legenda", 32, new Vector2(0f, 0.03f), new Vector2(1f, 0.4f)), 2f);
            a.Label.fontStyle = FontStyle.Bold; a.Label.text = label;
            // selo de video: retangulo branco com "play" escuro, saindo pelo canto de cima a direita
            RectTransform v = Art.Node(edge.transform, "Video", new Vector2(1f, 1f), new Vector2(1f, 1f));
            v.sizeDelta = new Vector2(52f, 38f); v.anchoredPosition = new Vector2(-8f, -4f);
            Image vb = v.gameObject.AddComponent<Image>(); vb.sprite = Art.Box(); vb.type = Image.Type.Sliced; vb.pixelsPerUnitMultiplier = 2f;
            vb.color = Color.white; vb.raycastTarget = false;
            Image play = Art.Node(v, "Play", new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f)).gameObject.AddComponent<Image>();
            play.sprite = Art.Triangle(); play.color = Art.Bg; play.raycastTarget = false;   // "play" escuro e neutro (sem cor de marca)
            play.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
            a.Video = v.gameObject;
            edge.gameObject.SetActive(false);
            return a;
        }

        Banner MakeBanner(string name, Vector2 min, Vector2 max, Sprite icon, int size)
        {
            var b = new Banner();
            Image edge = Art.Panel(_safe, name, Art.Gold, min, max);
            edge.raycastTarget = false;
            Image face = Art.Panel(edge.transform, "Fundo", Art.ComAlfa(Art.Hex(0x2A1E14), 0.95f), Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(5f, 5f); face.rectTransform.offsetMax = new Vector2(-5f, -5f);
            face.raycastTarget = false;
            if (icon != null)
            {
                b.I = Art.Node(edge.transform, "Icone", new Vector2(0.02f, 0.05f), new Vector2(0.2f, 0.95f)).gameObject.AddComponent<Image>();
                b.I.sprite = icon; b.I.preserveAspect = true; b.I.raycastTarget = false;
            }
            b.T = Art.Outlined(Art.NewText(edge.transform, "Texto", size, new Vector2(icon != null ? 0.2f : 0.03f, 0f), new Vector2(0.97f, 1f)), 3f);
            b.T.fontStyle = FontStyle.Bold; b.T.color = Art.Accent;
            b.R = edge.rectTransform;
            b.G = edge.gameObject.AddComponent<CanvasGroup>();
            b.G.blocksRaycasts = false;
            edge.gameObject.SetActive(false);
            return b;
        }

        void ShowBanner(Banner b, string text, float dur)
        {
            b.T.text = text; b.Age = 0f; b.Dur = dur;
            b.R.gameObject.SetActive(true);
        }

        /// <summary>Pop 0 -> 1,1 -> 1 em 0,3 s, segura e some nos ultimos 0,4 s.</summary>
        static void TickBanner(Banner b, float dt)
        {
            if (!b.R.gameObject.activeSelf) return;
            b.Age += dt;
            if (b.Age >= b.Dur) { b.R.gameObject.SetActive(false); return; }
            float k = Mathf.Clamp01(b.Age / 0.3f);
            b.R.localScale = Vector3.one * (k < 0.6f ? Mathf.Lerp(0.3f, 1.1f, k / 0.6f) : Mathf.Lerp(1.1f, 1f, (k - 0.6f) / 0.4f));
            b.G.alpha = Mathf.Clamp01((b.Dur - b.Age) / 0.4f);
        }

        /// <summary>
        /// Cada quadro (tempo real: o simulado zera o timeScale): VIP so com Sim.CanSummonVip e anuncio pronto; velocidade com CanBoost e
        /// anuncio pronto mostra o proximo multiplicador, fora disso (sem boost e com recarga) fica cinza com o tempo; no 3x some (o selo
        /// diz tudo). Selo do boost com o multiplicador e o cronometro, pulsando.
        /// </summary>
        void RefreshAdUi(float dt)
        {
            bool vip = _sim.CanSummonVip && Ads.Ready(Ads.Vip);
            if (_vipBtn.B.gameObject.activeSelf != vip) _vipBtn.B.gameObject.SetActive(vip);
            bool active = _sim.BoostLeft > 0f, can = _sim.CanBoost && Ads.Ready(Ads.Velocidade), cool = !active && _sim.BoostCooldown > 0f;
            bool show = can || cool;
            if (_speedBtn.B.gameObject.activeSelf != show) _speedBtn.B.gameObject.SetActive(show);
            if (show)
            {
                _speedBtn.B.interactable = can;
                _speedBtn.Face.color = can ? AdFace : AdOff;
                _speedBtn.Icon.color = can ? Art.Accent : Art.ComAlfa(Art.Ink, 0.45f);
                _speedBtn.Video.SetActive(can);
                _speedBtn.Label.text = can ? (active ? "3×" : "2×") : Clock(_sim.BoostCooldown);
            }
            if (_boostSeal.gameObject.activeSelf != active) _boostSeal.gameObject.SetActive(active);
            if (active)
            {
                _boostMul.text = _sim.BoostMul.ToString("0") + "×";
                _boostTime.text = Clock(_sim.BoostLeft);
                _boostSeal.localScale = Vector3.one * (1f + 0.04f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
            }
            TickBanner(_vipBanner, dt);
            TickBanner(_toast, dt);
        }

        void OnVipAd() => Ads.Show(Ads.Vip, () =>
        {
            if (!_sim.SummonVip()) return;   // mudou entre o toque e o fim do anuncio: nada a dar
            Sfx.Play("upgrade");
            ShowBanner(_vipBanner, "VIP a caminho!", 1.6f);
        }, AdFail);

        void OnSpeedAd() => Ads.Show(Ads.Velocidade, () =>
        {
            float m = _sim.StartBoost();
            if (m <= 0f) return;
            Sfx.Play("upgrade", 1.2f);
            _view.Float(_sim.Player.Pos, $"Velocidade {m:0}×!", Art.Accent, 48);
            Log("boost_start", m.ToString("0", CultureInfo.InvariantCulture), Balance.BoostSeconds.ToString("0", CultureInfo.InvariantCulture));
        }, AdFail);

        void AdFail()
        {
            Sfx.Play("leave", 1.3f);
            ShowBanner(_toast, "Anúncio indisponível", 1.6f);
        }

        void ShowPanel(string title, string body, string button, Action action)
        {
            _panel.gameObject.SetActive(true);
            _panelTitle.text = title;
            _panelBody.text = body;
            _panelBtn.text = button;
            _panelAction = action;
        }

        void ClosePanel()
        {
            _panel.gameObject.SetActive(false);
            _panelAction = null;
        }

        // ---------- save, dev e diario ----------

        static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        void Save()
        {
            if (_sim == null || _headless || _testSession) return;
            PlayerPrefs.SetString(SaveKey, _sim.Save(Now()));
            PlayerPrefs.Save();
        }

        // Voltar de outro app tambem paga o cofre (antes so a abertura pagava: quem trocava de app perdia o tempo fora).
        // _started: a Unity chama OnApplicationPause(false) logo apos o Awake, e na abertura quem paga e' o Start.
        // ponytail: < 60 s fora nao paga nem mostra painel (perde no maximo 15 s de producao); vira Balance se o playtest pedir.
        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
            else if (_started && _sim != null && _sim.SavedAt > 0 && Now() - _sim.SavedAt >= 60) Offline();
        }
        void OnApplicationFocus(bool focus) { if (!focus) Save(); }
        void OnApplicationQuit() { Save(); }

        /// <summary>Fotos/teste (dev): -buy N compra os produtivos entre os N primeiros upgrades (ordem do tier) e depois os luxos, -gold G, -px x,y.</summary>
        void DevArgs()
        {
            if (int.TryParse(Arg("-buy"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                for (int pass = 0; pass < 2; pass++)   // o Buy recusa luxo antes da producao completa, e ha produtivos (Mineiro, Joalheiro 2) depois dos luxos
                    for (int i = 0; i < Math.Min(n, Upgrades.Count); i++)
                        if (Upgrades.IsLuxury(i) == (pass == 1)) _sim.Buy((Upgrade)i);
            // -buyids 1,5,0@300 (criativos): compra estes ids do enum Upgrade fora da ordem do -buy (ex.: 4 bancadas e 1 fornalha
            // sem fole); "id@t" compra quando o relogio do jogo passa de t s, na ordem dada, cobrando o preco (TimedBuys).
            foreach (string tok in (Arg("-buyids") ?? "").Split(','))
            {
                string[] p = tok.Split('@');
                if (!int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id < 0 || id >= Upgrades.Count) continue;
                if (p.Length == 1) _sim.Buy((Upgrade)id);
                else if (float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float at)) _buyAt.Add((id, at));
            }
            if (int.TryParse(Arg("-gold"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int g)) _sim.Gold = Math.Max(0, g);
            string[] px = (Arg("-px") ?? "").Split(',');
            if (px.Length == 2 && float.TryParse(px[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(px[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) _sim.Player.Pos = new V2(x, y);
            // -warmup S: simula S s antes do 1o quadro (jogador parado, sem bot): a fila e a fome ja estao montadas quando a gravacao comeca
            // v0.5c: -boost N = N anuncios de velocidade (1 = 2x, 2 = 3x) ANTES do warmup (warmup >= 60 = foto da recarga)
            if (int.TryParse(Arg("-boost"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int nb)) for (int i = 0; i < nb; i++) _sim.StartBoost();
            float warm = ArgF("-warmup", 0f);
            for (float t = 0f; t < warm; t += MaxStep) { _sim.Tick(MaxStep, 0f, 0f); TimedBuys(); }
            // fotos da v0.5, depois do warmup: -hold 0,3,3,3,0,0 = carga mista do ferreiro (por Item, ate o teto de cada tipo) e
            // -stock 10,10,10 = estoque do balcao (espada, escudo, ferramenta, joia; ate o teto). A fila do warmup compra ja no 1o tick.
            string[] hold = (Arg("-hold") ?? "").Split(','), stock = (Arg("-stock") ?? "").Split(',');
            for (int i = 0; i < Math.Min(hold.Length, 6); i++)
                if (int.TryParse(hold[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h)) _sim.Player.Held[i] = Math.Max(0, Math.Min(h, _sim.Player.Cap));
            for (int i = 0; i < Math.Min(stock.Length, 4); i++)
                if (int.TryParse(stock[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int s)) _sim.Stock[2 + i] = Math.Max(0, Math.Min(s, _sim.CounterCap));
            // -vipnow: chama o VIP depois do warmup (fila cheia = ele espera ao lado); se ainda nao pode, tenta a cada quadro (Update).
            // ponytail: cheat de dev, marca a 1a venda se ainda nao houve (CanSummonVip pede a 1a venda)
            if (Arg("-vipnow") != null)
            {
                if (_sim.FirstSaleTime < 0f) _sim.FirstSaleTime = _sim.Time;
                _vipNow = !_sim.SummonVip();
            }
        }

        /// <summary>-buyids id@t: compra quando _sim.Time passa de t e cobra o preco (sem ouro bastante fica em 0).</summary>
        void TimedBuys()
        {
            for (int i = 0; i < _buyAt.Count;)
            {
                if (_sim.Time < _buyAt[i].t) { i++; continue; }
                var u = (Upgrade)_buyAt[i].u;
                _buyAt.RemoveAt(i);
                // ponytail: cheat de dev, o Buy nao confere pre-requisito; a ordem da lista e' responsabilidade de quem grava
                if (_sim.Buy(u)) _sim.Gold = Math.Max(0, _sim.Gold - Upgrades.Cost(u));
            }
        }

        static float ArgF(string name, float def) => float.TryParse(Arg(name), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : def;

        static string Arg(string name)
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, name);
            if (i < 0) return null;
            return i + 1 < a.Length && !a[i + 1].StartsWith("-") ? a[i + 1] : "";
        }

        public static string Clock(long seconds) => seconds < 3600 ? $"{seconds / 60} min" : $"{seconds / 3600} h {(seconds % 3600) / 60:00} min";
        static string Clock(float s) => $"{(int)(s / 60f)}:{(int)(s % 60f):00}";

        IEnumerator Shot(string path)
        {
            string d = Arg("-shotdelay");
            float delay = !string.IsNullOrEmpty(d) && float.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 1f;
            if (Arg("-shotchest") != null)
            {
                // ponytail: captura o marco real antes de o bot abrir, sem depender do FPS da maquina.
                float until = Time.realtimeSinceStartup + 180f;
                while (!_sim.Chests.Exists(c => c.State == 1) && Time.realtimeSinceStartup < until) yield return null;
                if (!_sim.Chests.Exists(c => c.State == 1)) { Debug.LogError("SHOT: nenhum bau disponivel em 180 s"); Application.Quit(1); yield break; }
            }
            else yield return new WaitForSecondsRealtime(delay);
            Debug.Log($"SHOT t={_sim.Time:0.0} upgrades={_sim.UpgradesBought} nobres={_sim.JewelQueue.Count}/{_sim.JewelQueueCap} baus={string.Join(",", _sim.Chests.ConvertAll(c => c.State))}");
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
            yield return null;
            Application.Quit(0);
        }

        /// <summary>
        /// -record pasta [-recordsec S] [-recordfps F]: grava round(S x F) quadros (pasta/fNNNNN.jpg) e sai 0. O relogio do jogo anda
        /// 1/F s por quadro (Time.captureFramerate no Awake), entao maquina lenta nao muda o video; com -speed N cada quadro avanca
        /// N/F s de simulacao (F baixo = time-lapse). Supersample ate ~1920 px de altura: janela 540x960 sai 1080x1920. A janela
        /// precisa estar visivel (-batchmode/-nographics nao tem tela para capturar).
        /// </summary>
        IEnumerator Record(string dir)
        {
            Directory.CreateDirectory(dir);
            int frames = Mathf.RoundToInt(ArgF("-recordsec", 15f) * Time.captureFramerate);
            int k = Mathf.Max(1, Mathf.RoundToInt(1920f / Screen.height));
            var eof = new WaitForEndOfFrame();
            for (int n = 0; n < frames; n++)
            {
                yield return eof;
                Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture(k);
                // ponytail: JPG 95 em vez de PNG; 1080x1920 em PNG passa de 2 MB por quadro e 900 quadros pesam no disco
                File.WriteAllBytes(Path.Combine(dir, $"f{n:00000}.jpg"), tex.EncodeToJPG(95));
                if (n == 0) Debug.Log($"RECORD {tex.width}x{tex.height} x{k} {frames} quadros -> {dir}");
                Destroy(tex);
            }
            Debug.Log($"RECORD OK t={_sim.Time:0.0} fila={_sim.Queue.Count}/{_sim.QueueCap} upgrades={_sim.UpgradesBought} ouro={_sim.Gold}");
            Application.Quit(0);
        }

        /// <summary>Smoke do build: o bot joga N minutos (padrao 10) sem render nem relogio, loga por minuto e sai 0/1.</summary>
        void Autoplay()
        {
            _headless = true;
            string m = Arg("-autoplay");
            float minutes = !string.IsNullOrEmpty(m) && float.TryParse(m, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 10f;
            var sim = new Sim();
            var bot = new Bot();
            const float dt = 1f / 30f;
            int next = 1;
            for (float t = 0f; t < minutes * 60f; t += dt)
            {
                bot.Step(sim, dt);
                if (sim.Time < next * 60f) continue;
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "AUTOPLAY t={0} gold={1} earned={2} upgrades={3} sales={4} lost={5} away={6} walk={7:0}s ema={8:0.00}/s",
                    Clock(sim.Time), sim.Gold, sim.GoldEarned, sim.UpgradesBought, sim.Sales, sim.ClientsLost, sim.ClientsTurnedAway, sim.WalkNoDecision, sim.RateEma));
                next++;
            }
            for (int i = 0; i < Upgrades.Count; i++)
                if (sim.UpgradeTime[i] >= 0f) Debug.Log($"AUTOPLAY upgrade {(Upgrade)i} em {Clock(sim.UpgradeTime[i])}");
            bool ok = sim.FirstSaleTime > 0f && sim.FirstSaleTime < 90f && sim.UpgradesBought >= 1;
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "AUTOPLAY {0} venda1={1:0}s upgrades={2} ouro={3}", ok ? "OK" : "FALHOU", sim.FirstSaleTime, sim.UpgradesBought, sim.GoldEarned));
            Application.Quit(ok ? 0 : 1);
        }

        void Log(string evt, string a = "", string b = "")
        {
            if (_testSession) return;
            try { File.AppendAllText(_diary, $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss},{_sid},{evt},{_sim.Time.ToString("0.0", CultureInfo.InvariantCulture)},{a},{b}\n"); }
            catch (Exception) { } // ponytail: diario e' best-effort; disco cheio nunca derruba o jogo
        }
    }
}
