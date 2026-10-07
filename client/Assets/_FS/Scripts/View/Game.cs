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
    /// [-shotdelay s] [-shotchest] [-bot] | -speed N | -reset (apaga o save) | -testsession (sem persistencia).
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
        const float HudBand = 0.15f;                             // fracao da tela reservada ao painel de cima; o mundo nao chega la (fotos 02-04)
        const float ContentTop = Balance.WorldH + 1.6f;         // topo do conteudo: fila de clientes + balao + "+10"

        Sim _sim;
        Bot _bot;
        WorldView _view;
        Joystick _joy;
        Camera _cam;
        float _speed = 1f, _saveT, _hintT, _minuteT;
        bool _botDrive, _headless, _firstSaleLogged, _testSession;
        string _diary, _sid;
        Vector2Int _screen;

        Canvas _joyCanvas;
        RectTransform _canvas, _labels, _safe, _panel;
        Text _gold, _hint, _panelTitle, _panelBody, _panelBtn;
        Action _panelAction;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            _testSession = Arg("-testsession") != null || Arg("-autoplay") != null;
            _sid = Guid.NewGuid().ToString("N").Substring(0, 8);
            _diary = Path.Combine(Application.persistentDataPath, "diario.csv");
            Sfx.Init(gameObject);
            // ponytail: fotos e smoke usam estado novo sem ler ou gravar o save/diario normal.
            if (!_testSession && Arg("-reset") != null) PlayerPrefs.DeleteKey(SaveKey);
            _sim = _testSession ? new Sim() : Sim.Load(PlayerPrefs.GetString(SaveKey, ""));
            DevArgs();
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
            _botDrive = Arg("-bot") != null;
            string shot = Arg("-shot");
            if (!string.IsNullOrEmpty(shot)) StartCoroutine(Shot(shot));
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
                HandleEvents();
            }

            _view.Refresh(Time.deltaTime);
            FollowCamera(Time.deltaTime);
            RefreshHud();

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
                        _view.Float(e.Pos, "+" + e.B, Art.Accent);
                        Log("product_sold", Balance.ItemName[e.A], e.B.ToString());
                        break;
                    case Ev.Bought:
                        Sfx.Play("upgrade");
                        _view.Float(e.Pos, Upgrades.All[e.A].Name + "!", Art.Good, 44);
                        Log("upgrade_buy", ((Upgrade)e.A).ToString(), e.B.ToString());
                        break;
                    case Ev.ClientArrived: Sfx.Play("client", 1f, 0.2f); break;
                    case Ev.ClientLeft:
                        if (e.B == 0) { Sfx.Play("leave"); _view.Float(e.Pos, "...", Art.Bad); }
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
                        Log("chest_opened", e.A.ToString(), e.B.ToString());
                        break;
                }
            }
            if (!_firstSaleLogged && _sim.FirstSaleTime >= 0f)
            {
                _firstSaleLogged = true;
                Log("first_sale", _sim.FirstSaleTime.ToString("0.0", CultureInfo.InvariantCulture));
            }
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
            float minY = halfH - Margin;                                  // borda de baixo em -Margin
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
            _labels = Art.Node(_canvas, "Rotulos", Vector2.zero, Vector2.one);   // rotulos do mundo ficam ABAIXO da HUD e do painel modal
            _safe = Art.Node(_canvas, "AreaSegura", Vector2.zero, Vector2.one);

            // Painel de cima com 2 linhas (ouro | dica). Ocupa a faixa HudBand da tela, que a camera reserva: nada do mundo passa aqui.
            Image band = Art.Panel(_safe, "Topo", Art.ComAlfa(Art.Bg, 0.88f), new Vector2(0.02f, 1f - HudBand + 0.01f), new Vector2(0.98f, 0.99f));
            band.raycastTarget = false;
            Image coin = Art.Node(band.transform, "Moeda", new Vector2(0.03f, 0.56f), new Vector2(0.1f, 0.94f)).gameObject.AddComponent<Image>();
            coin.sprite = Art.Disc(); coin.color = Art.Accent; coin.preserveAspect = true; coin.raycastTarget = false;
            _gold = Art.NewText(band.transform, "Ouro", 64, new Vector2(0.12f, 0.5f), new Vector2(0.6f, 1f), TextAnchor.MiddleLeft);
            _gold.fontStyle = FontStyle.Bold;
            Text title = Art.NewText(band.transform, "Titulo", 30, new Vector2(0.55f, 0.5f), new Vector2(0.97f, 1f), TextAnchor.MiddleRight);
            title.text = "Forge Street v" + Application.version;
            title.color = Art.ComAlfa(Art.Ink, 0.55f);
            _hint = Art.NewText(band.transform, "Dica", 36, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.5f));
            _hint.color = Art.Accent;

            _panel = Art.Node(_safe, "Painel", Vector2.zero, Vector2.one);
            _panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            Image box = Art.Panel(_panel, "Caixa", Art.Pad, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.66f));
            _panelTitle = Art.NewText(box.transform, "Titulo", 60, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.95f));
            _panelTitle.fontStyle = FontStyle.Bold;
            _panelBody = Art.NewText(box.transform, "Corpo", 38, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.68f));
            Button ok = Art.NewButton(box.transform, "OK", 52, Art.Accent, new Vector2(0.22f, 0.07f), new Vector2(0.78f, 0.3f), () => _panelAction?.Invoke());
            _panelBtn = ok.GetComponentInChildren<Text>();
            _panel.gameObject.SetActive(false);

            // Canvas do joystick em escala 1 (px = px): o dp do ARKANA vale direto.
            var joyGo = new GameObject("JoystickCanvas", typeof(Canvas));
            _joyCanvas = joyGo.GetComponent<Canvas>();
            _joyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _joyCanvas.sortingOrder = 2;
        }

        void RefreshHud()
        {
            _gold.text = _sim.Gold.ToString();
            _hintT -= Time.deltaTime;
            if (_hintT > 0f) return;
            _hintT = 0.25f;
            _hint.text = HintText(_sim.CurrentHint(), _sim.HintArg);
        }

        string HintText(Hint h, int arg)
        {
            switch (h)
            {
                case Hint.BuyPad:
                    Pad p = _sim.Pads[arg];
                    int u = p.Current(_sim);
                    return u < 0 ? "" : $"Pise no pad: {Upgrades.All[u].Name} ({Upgrades.Cost(u) - p.Paid} de ouro)";
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

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationFocus(bool focus) { if (!focus) Save(); }
        void OnApplicationQuit() { Save(); }

        /// <summary>Fotos/teste (dev): -buy N compra os N primeiros upgrades na ordem do tier, -gold G, -px x,y.</summary>
        void DevArgs()
        {
            if (int.TryParse(Arg("-buy"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                for (int i = 0; i < Math.Min(n, Upgrades.Count); i++) _sim.Buy((Upgrade)i);
            if (int.TryParse(Arg("-gold"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int g)) _sim.Gold = Math.Max(0, g);
            string[] px = (Arg("-px") ?? "").Split(',');
            if (px.Length == 2 && float.TryParse(px[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(px[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) _sim.Player.Pos = new V2(x, y);
        }

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
