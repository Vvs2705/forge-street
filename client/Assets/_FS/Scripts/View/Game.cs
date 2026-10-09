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
    /// Fluxo do jogo, o unico MonoBehaviour de fluxo: carrega/grava o Sim (save.txt pelo SaveStore + espelho no PlayerPrefs), aplica o cofre offline, le o
    /// joystick flutuante (e WASD no PC), avanca a simulacao em passos de ate 1/30 s, toca SFX pelos eventos, segue o
    /// jogador com a camera em retrato, monta a HUD por codigo e grava o diario de playtest
    /// (persistentDataPath/diario.csv). Nasce sozinho em qualquer cena.
    /// Flags de dev: -autoplay [min] (bot sem render, loga AUTOPLAY por minuto, sai 0/1) | -shot foto.png
    /// [-shotdelay s] [-shotchest] [-shotorder] [-bot] [-menu] | -speed N | -reset (apaga o save) | -testsession (sem persistencia) |
    /// -record pasta [-recordsec S] [-recordfps F] (quadros 1080x1920 para os criativos, docs/CRIATIVOS.md) | -buyids 1,5,0@300 |
    /// -warmup S | -hold 0,3,3,3,0,0 | -stock 10,10,10 (fotos de validacao) | -fakeads | -adtest vip|velocidade (Ads.Show no
    /// inicio; docs/LEVELPLAY.md) | -vipnow (chama o VIP depois do warmup) | -boost N (N anuncios de velocidade antes do warmup) |
    /// -cam W (largura visivel em m, 6..11,4; experimento de camera mais perto) | -settings [on|off] (abre as configuracoes; on/off
    /// liga/desliga Som e Vibracao antes, gravando como o toque) | -cofre N (mostra o painel do cofre com N de ouro, sem dar o ouro).
    /// Build dev: tambem pelo intent do Android (Arg).
    /// v0.5c (docs/FASE8_VIP_VELOCIDADE.md s5): botoes "Chamar VIP" e "Velocidade 2x/3x" nos cantos da barra de baixo (Ads.Show ->
    /// Sim.SummonVip / Sim.StartBoost), selo do boost com cronometro, aviso "Cliente VIP!" e diario vip_* / boost_start.
    /// v0.6c (docs/FASE9_ENCOMENDAS.md s6): cartao da encomenda acima do botao VIP, aviso "Encomenda entregue!" e diario order_*.
    /// v0.6d (revisao de UX, docs/VALIDACAO_V06A.md s v0.6d): dica de 1 linha (Textos), botoes a 32 px das bordas, cartao "Venda 5
    /// espadas", compra no aviso da encomenda, moedas por baixo da dica, chaves e cofre no cartao creme.
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
        float _speed = 1f, _saveT, _hintT, _minuteT, _adHold, _visW = VisibleWidth, _backT;
        bool _botDrive, _headless, _firstSaleLogged, _testSession, _started, _pausedByAd;
        bool _noSave;   // A-PLAT-02: save de versao mais nova ou disco ilegivel = a sessao nao grava por cima
        string _diary, _sid, _savePath;
        Vector2Int _screen;
        readonly System.Collections.Generic.List<(int u, float t)> _buyAt = new System.Collections.Generic.List<(int u, float t)>();   // -buyids id@t

        Canvas _joyCanvas;
        RectTransform _canvas, _labels, _safe, _panel, _fx;
        Text _gold, _hint, _panelTitle, _panelBody, _panelBtn, _panelGold;
        RectTransform _panelRow;   // v0.6d: moeda + "+N" do cofre no painel
        Hint _hintH; int _hintArg = -1, _hintU;   // v0.6d: a dica so e' remontada quando muda (sem string nova a cada 0,25 s)
        RectTransform _cfg;   // v0.6b: cartao de configuracoes (Som, Vibracao, versao)
        Action _panelAction;
        Image _coinIcon;
        sealed class Coin { public Image I; public Vector3 From; public float T; public int Value; }   // T < 0: esperando a vez
        readonly System.Collections.Generic.List<Coin> _coins = new System.Collections.Generic.List<Coin>();
        int _pending; long _goldInt = -1; float _punchT; double _goldShown = -1;   // A-CORE-06: double/long, float perdia unidade acima de 16 mi
        // v0.5c: botoes de anuncio, selo do boost e avisos curtos (VIP chegou / anuncio indisponivel)
        sealed class AdBtn { public Button B; public Image Face, Icon; public GameObject Video; public Text Label; }
        sealed class Banner { public RectTransform R; public CanvasGroup G; public Text T; public Image I; public float Age = 99f, Dur; }
        AdBtn _vipBtn, _speedBtn;
        RectTransform _boostSeal; Text _boostMul, _boostTime;
        Banner _vipBanner, _toast;
        bool _vipNow;
        // v0.6c: cartao da encomenda (FASE9), aviso de entregue e o estado da animacao (_orderKey = OrderCount da encomenda na tela)
        RectTransform _order; CanvasGroup _orderG; Image _orderIcon, _orderFill, _orderCoin; Text _orderText, _orderPrize, _orderBar;
        Banner _orderBanner;
        int _orderKey = -1, _orderSeen = -1; float _orderT, _orderPunch; bool _orderOut;
        static readonly Color AdFace = Art.Hex(0x3B2650), AdOff = Art.Hex(0x5A5560);
        // v0.6d: botoes de anuncio e cartoes de baixo a 32 px das bordas da area segura, com largura fixa; topo dos botoes de anuncio
        const float Edge = 32f, AdW = 140f, AdTop = MenuBar.Band - 0.004f;

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
            _savePath = Path.Combine(Application.persistentDataPath, "save.txt");
            Sfx.Init(gameObject);
            // ponytail: fotos e smoke usam estado novo sem ler ou gravar o save/diario normal.
            if (!_testSession && Arg("-reset") != null) { PlayerPrefs.DeleteKey(SaveKey); File.Delete(_savePath); File.Delete(_savePath + ".bak"); }
            if (_testSession) _sim = new Sim(); else LoadSave();
            DevArgs();
            // -cam W (P2-3 do BENCHMARK_VISUAL, experimento de playtest): largura visivel em m; abaixo de 11,4 a camera segue o jogador
            _visW = Mathf.Clamp(ArgF("-cam", VisibleWidth), 6f, VisibleWidth);
            string cfg = Arg("-settings");
            if (cfg == "on" || cfg == "off") { SetSom(cfg == "on"); SetVibra(cfg == "on"); }   // fotos e teste de persistencia
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
            _cfg.SetAsLastSibling();     // configuracoes e painel modal cobrem a barra de melhorias
            _panel.SetAsLastSibling();
            if (Arg("-menu") != null) _menu.Toggle();   // foto com a fileira aberta
            if (cfg != null) _cfg.gameObject.SetActive(true);
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
            if (int.TryParse(Arg("-cofre"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cofre) && cofre > 0) ShowVault(cofre);   // foto do painel
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
            if (elapsed < 60) return;   // mesma regra da volta de pausa: abrir de novo em < 1 min mostrava "31 de ouro em 0 min fora" (POCO F4)
            long gold = _sim.ApplyOffline(elapsed, _sim.SavedAt);
            if (gold <= 0) return;
            long shown = Math.Min(elapsed, (long)Balance.OfflineCapSeconds);
            Log("offline_claim", gold.ToString(), shown.ToString());
            Sfx.Play("offline");
            ShowVault(gold);
            Save();
        }

        /// <summary>v0.6d: cartao creme do cofre: titulo, "+N" grande com a moeda, uma linha e PEGAR (saiu a frase dos 25% e do teto).</summary>
        void ShowVault(long gold) => ShowPanel("Bem-vindo de volta!", "Seu cofre guardou isso pra você", "PEGAR", ClosePanel, gold);

        void Update()
        {
            if (_headless) return;
            // Input.GetKeyDown: com o GameActivity o voltar do Android nao chega ao Input System, so ao Input Manager legado (Setup: Both)
            bool back = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
            back |= Input.GetKeyDown(KeyCode.Escape);   // sem o legado (handler 1) o Input.* lanca toda chamada e derruba o Update inteiro
#endif
            // POCO F4: um voltar as vezes chegava em 2 quadros (legado e Input System): fechava a bandeja e logo depois saia do jogo
            if (back && Time.unscaledTime >= _backT)
            {
                _backT = Time.unscaledTime + 0.4f;
                // voltar do Android (revisao da v0.6): fecha o que estiver aberto, de cima para baixo; so com nada aberto grava e sai
                if (_panel.gameObject.activeSelf) ClosePanel();
                else if (_cfg.gameObject.activeSelf) _cfg.gameObject.SetActive(false);
                else if (_menu.IsOpen) _menu.Toggle();
                else { Save(); Application.Quit(); return; }
            }
            if (_screen.x != Screen.width || _screen.y != Screen.height) Fit();

            bool modal = _panel.gameObject.activeSelf || _cfg.gameObject.activeSelf;
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

            // anuncio pedido = jogo parado (paciencia, boost e relogio do VIP): o boost nao acaba no segundo antes de o anuncio real
            // cobrir a tela. ponytail: teto de 15 s de quadros parados por anuncio, para um SDK que nunca responde nao travar o jogo
            _adHold = Ads.Busy ? _adHold + Mathf.Min(Time.unscaledDeltaTime, 0.1f) : 0f;
            float dt = Ads.Busy && _adHold < 15f ? 0f : Mathf.Min(Time.deltaTime, 0.1f) * _speed;
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
                        _view.Crafted(e.B);   // v0.6a: o item novo da pilha de saida da pop
                        Log("product_crafted", Balance.ItemName[e.A], _sim.Stations[e.B].Name);
                        break;
                    case Ev.Sold:
                        Sfx.Play("coin", 1f + 0.05f * (_sim.Sales % 5), 0.1f);
                        Ajustes.Pulso(15, 0.25f);   // v0.6b: no maximo 4 por segundo (o VIP vende varias unidades no mesmo quadro)
                        _view.SaleFloat(new V2(e.Pos.X, e.Pos.Y - 0.9f), e.B);   // v0.5: pela frente do estande; v0.6d: vendas a < 0,4 s somam num "+N" so
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
                        Ajustes.Pulso(40);
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
                    // FASE9 (v0.6c): a encomenda nova entra pelo estado do Sim (RefreshOrder, vale tambem para a do save); entregue = som,
                    // aviso e moedas saindo do cartao (o premio ja entrou no Gold, fora do GoldEarned, como o bau)
                    case Ev.OrderNew: Log("order_new", Balance.ItemName[e.A], e.B.ToString()); break;
                    case Ev.OrderDone:
                        Log("order_done", Balance.ItemName[e.A], e.B.ToString());
                        if (_order == null) break;   // criativo: sem cartao, o ouro entra direto no numero
                        Sfx.Play("upgrade");
                        OrderText(_sim.OrderTarget, _sim.OrderTarget);   // a ultima venda e a entrega caem no mesmo tick: mostra o 5/5
                        _orderOut = true; _orderT = 0f;
                        ShowBanner(_orderBanner, $"Encomenda entregue! +{e.B}", 2f, _coinIcon.sprite, _coinIcon.color);
                        FlyCoins(_orderCoin.rectTransform.position, e.B);
                        break;
                }
            }
            if (!_firstSaleLogged && _sim.FirstSaleTime >= 0f)
            {
                _firstSaleLogged = true;
                Log("first_sale", _sim.FirstSaleTime.ToString("0.0", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Compra (pad, bot ou toque no menu): som, aviso "Nome!" e diario. v0.6d: o aviso e' o mesmo banner do "Encomenda entregue!", com o
        /// icone da melhoria (era um "+nome!" verde no mundo, que se misturava aos "+N" das vendas). Criativo (sem banner): o flutuante de antes.
        /// </summary>
        void Bought(int u, long price, V2 pos)
        {
            Sfx.Play("upgrade");
            Ajustes.Pulso(25);
            if (_orderBanner == null) _view.Float(pos, Upgrades.All[u].Name + "!", Art.Good, 44);
            else
            {
                Sprite icon; Color tint;
                if (Upgrades.All[u].InMenu) (icon, tint) = MenuBar.Icon((Upgrade)u);
                else icon = _view.PadIcon(u, out tint, out _);
                ShowBanner(_orderBanner, Upgrades.All[u].Name + "!", 2f, icon, tint);
            }
            Log("upgrade_buy", ((Upgrade)u).ToString(), price.ToString());
        }

        // ---------- camera e HUD ----------

        void Fit()
        {
            _screen = new Vector2Int(Screen.width, Screen.height);
            AreaSegura.AncorarDentro(_safe, AreaSegura.Atual());
            Rect sa = Screen.safeArea;
            _view.HudBottomPx = sa.yMin + sa.height * (1f - HudBand);   // v0.6d: o balao do 1o da fila fica 24 px abaixo da faixa de cima
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
            float size = _visW / (2f * aspect);
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
            RectTransform top = Art.Node(_safe, "Topo", new Vector2(0f, 1f - HudBand), new Vector2(1f, 0.995f));
            top.offsetMin = new Vector2(Edge, 0f); top.offsetMax = new Vector2(-Edge, 0f);   // v0.6d: 32 px das bordas (era 2%, ~22 px)
            Image pill = Art.Panel(top, "PilulaOuro", Art.Hex(0xF2D9A0), new Vector2(0.06f, 0.1f), new Vector2(0.36f, 0.9f));
            Image pillIn = Art.Panel(pill.transform, "Fundo", Art.ComAlfa(Art.Hex(0x2A1E14), 0.95f), Vector2.zero, Vector2.one);
            pillIn.rectTransform.offsetMin = new Vector2(5f, 5f); pillIn.rectTransform.offsetMax = new Vector2(-5f, -5f);
            pill.raycastTarget = pillIn.raycastTarget = false;
            _coinIcon = Art.Node(top, "Moeda", new Vector2(0f, -0.05f), new Vector2(0.14f, 1.05f)).gameObject.AddComponent<Image>();
            Sprite moeda = Art.Icon("moeda", "icone");   // v0.5: moeda renderizada; sem a folha, o disco amarelo de sempre
            _coinIcon.sprite = moeda != null ? moeda : Art.Disc(); _coinIcon.color = moeda != null ? Color.white : Art.Accent;
            _coinIcon.preserveAspect = true; _coinIcon.raycastTarget = false;
            // moedas voando (FlyCoins): por cima da pilula de ouro e da moeda, onde chegam, e por baixo da dica e do resto da HUD (v0.6d:
            // eram a ultima camada do canvas e cobriam a dica no arco)
            _fx = Art.Node(top, "Moedas", Vector2.zero, Vector2.one);
            _gold = Art.Outlined(Art.NewText(pill.transform, "Ouro", 62, new Vector2(0.3f, 0f), new Vector2(0.98f, 1f), TextAnchor.MiddleLeft), 3f);
            _gold.fontStyle = FontStyle.Bold;
            _gold.resizeTextForBestFit = true; _gold.resizeTextMinSize = 36; _gold.resizeTextMaxSize = 62;   // "1,23 mi" encolhe em vez de quebrar
            _gold.rectTransform.pivot = new Vector2(0f, 0.5f);   // o pulso cresce a partir da moeda, sem empurrar o numero
            Image hintPill = Art.Panel(top, "PilulaDica", Art.ComAlfa(Art.Bg, 0.92f), new Vector2(0.385f, 0.1f), new Vector2(Arg("-record") == null ? 0.875f : 1f, 0.9f));   // v0.6b: a engrenagem fica a direita (criativo: sem ela)
            hintPill.raycastTarget = false;
            // v0.6d: 1 linha so, ~32 px. A caixa tem 48 px de altura: 2 linhas nao cabem e o best-fit encolhe ate caber numa; os textos
            // (Textos.Dica) tem no maximo 26 caracteres, sem preco
            _hint = Art.Outlined(Art.NewText(hintPill.transform, "Dica", 32, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f)), 2f);
            _hint.rectTransform.offsetMin = new Vector2(18f, -24f); _hint.rectTransform.offsetMax = new Vector2(-18f, 24f);
            _hint.fontStyle = FontStyle.Bold; _hint.color = Art.Accent;
            _hint.resizeTextForBestFit = true; _hint.resizeTextMinSize = 22; _hint.resizeTextMaxSize = 32;
            _hint.verticalOverflow = VerticalWrapMode.Truncate;
            // versao: so no build dev (o playtest usa); release so nas configuracoes (v0.6b, P1-1); criativo nenhuma. v0.6d: pequena, logo
            // abaixo da pilula de ouro, na folga de 24 px que o balao do 1o da fila respeita (no canto de baixo batia no cartao e no VIP)
            if (Arg("-record") == null && Debug.isDebugBuild)
            {
                Text ver = Art.NewText(_safe, "Versao", 18, new Vector2(0f, 1f - HudBand), new Vector2(0f, 1f - HudBand), TextAnchor.UpperLeft);
                ver.rectTransform.pivot = new Vector2(0f, 1f);
                ver.rectTransform.sizeDelta = new Vector2(240f, 22f); ver.rectTransform.anchoredPosition = new Vector2(Edge + 8f, -1f);
                ver.text = "v" + Application.version;
                ver.color = Art.ComAlfa(Art.Ink, 0.8f);
                Art.Outlined(ver, 1.5f);   // le sobre a pedra e a grama
            }

            BuildSettings(top);

            // painel modal (cofre; no dev tambem o -adtest). v0.6d: cartao creme das configuracoes; "+N" grande com a moeda so no cofre
            _panel = Art.Node(_safe, "Painel", Vector2.zero, Vector2.one);
            _panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            Image box = Art.Panel(_panel, "Caixa", MenuBar.Leather, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.65f));
            Image card = Art.Panel(box.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            card.rectTransform.offsetMin = new Vector2(6f, 14f); card.rectTransform.offsetMax = new Vector2(-6f, -6f);   // chanfro de couro embaixo
            card.raycastTarget = false;
            _panelTitle = Art.NewText(card.transform, "Titulo", 56, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.96f));
            _panelTitle.fontStyle = FontStyle.Bold; _panelTitle.color = MenuBar.Brown;
            _panelRow = Art.Node(card.transform, "Ouro", new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f));
            _panelRow.sizeDelta = new Vector2(300f, 90f);
            Image coin = Art.Node(_panelRow, "Moeda", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).gameObject.AddComponent<Image>();
            coin.rectTransform.sizeDelta = new Vector2(84f, 84f); coin.rectTransform.anchoredPosition = new Vector2(42f, 0f);
            coin.sprite = _coinIcon.sprite; coin.color = _coinIcon.color; coin.preserveAspect = true; coin.raycastTarget = false;
            _panelGold = Art.Outlined(Art.NewText(_panelRow, "Valor", 64, Vector2.zero, Vector2.one, TextAnchor.MiddleLeft), 3f);
            _panelGold.rectTransform.offsetMin = new Vector2(96f, 0f);
            _panelGold.fontStyle = FontStyle.Bold; _panelGold.color = Art.Accent; _panelGold.horizontalOverflow = HorizontalWrapMode.Overflow;
            _panelBody = Art.NewText(card.transform, "Corpo", 26, new Vector2(0.06f, 0.33f), new Vector2(0.94f, 0.46f));
            _panelBody.color = Art.ComAlfa(MenuBar.Brown, 0.8f);
            Button ok = Art.NewButton(card.transform, "OK", 46, MenuBar.Leather, new Vector2(0.25f, 0.06f), new Vector2(0.75f, 0.26f), () => _panelAction?.Invoke());
            _panelBtn = ok.GetComponentInChildren<Text>();
            _panelBtn.color = MenuBar.Cream;
            _panel.gameObject.SetActive(false);
            BuildAdUi();
            BuildOrderUi();

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
            Vector3 from = _cam.WorldToScreenPoint(new Vector3(at.X, at.Y + 0.8f, 0f));
            FlyCoins(new Vector3(from.x, from.y, 0f), value, burst);
        }

        /// <summary>O mesmo saindo de um ponto da tela em px (v0.6c: o premio da encomenda sai da moeda do cartao).</summary>
        void FlyCoins(Vector3 from, int value, int burst = 0)
        {
            int n = burst > 0 ? burst : Mathf.Clamp(value / 8, 3, 6), given = 0;
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
            RefreshOrder(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
            // numero rolando: persegue o valor (sobe na venda, desce na compra) em ~0,3 s em vez de pular
            long target = Math.Max(0, _sim.Gold - _pending);
            _goldShown = _goldShown < 0 ? target : target + (_goldShown - target) * Math.Exp(-GoldRoll * Time.deltaTime);   // = Mathf.Lerp(target, shown, e)
            if (Math.Abs(_goldShown - target) < 0.5) _goldShown = target;
            long shown = (long)Math.Round(_goldShown);
            if (shown != _goldInt) { _goldInt = shown; _gold.text = Textos.Ouro(shown); }
            _hintT -= Time.deltaTime;
            if (_hintT > 0f) return;
            _hintT = 0.25f;
            Hint h = _sim.CurrentHint();
            int arg = _sim.HintArg, u = h == Hint.BuyPad ? _sim.Pads[arg].Current(_sim) : -1;   // a placa pode trocar de upgrade com a mesma dica
            if (h != _hintH || arg != _hintArg || u != _hintU) { _hintH = h; _hintArg = arg; _hintU = u; _hint.text = Textos.Dica(h, arg, _sim); }
            _view.ShowHint(h, arg);   // seta no mundo sobre o alvo (P1-1)
        }

        // ---------- anuncios: Chamar VIP, Velocidade 2x/3x, selo do boost e avisos (v0.5c) ----------

        /// <summary>
        /// Botoes nos cantos da barra de baixo (fora da fila e do joystick; a base sobe 2,6% para a marca "Development Build" do APK de
        /// teste nao cobrir a legenda), selo do boost logo acima do botao de velocidade (no topo ele batia no balao do 1o da fila, que vai
        /// para a esquerda com 8 vagas) e 2 avisos.
        /// </summary>
        void BuildAdUi()
        {
            _vipBtn = AdButton("ChamarVip", false, Art.Icon("coroa", "icone") ?? Art.Star(), "VIP", OnVipAd);
            _speedBtn = AdButton("Velocidade", true, Art.FastForward(), "2×", OnSpeedAd);
            _speedBtn.Icon.color = Art.Accent;
            // selo do boost: borda de ouro, avanco rapido, "2x" grande e o cronometro. v0.6d: 24 px acima do botao de velocidade e a 32 px
            // da borda, espelho do cartao da encomenda
            Image seal = Art.Panel(_safe, "SeloBoost", Art.Gold, new Vector2(1f, AdTop), new Vector2(1f, AdTop));
            seal.rectTransform.pivot = new Vector2(1f, 0f); seal.rectTransform.sizeDelta = new Vector2(313f, 100f);
            seal.rectTransform.anchoredPosition = new Vector2(-Edge, 24f);
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

        /// <summary>Botao quadrado: borda de ouro, fundo roxo escuro, icone, legenda e o selo de video (anuncio) no canto de cima.
        /// v0.6d: 140 px de largura a 32 px da borda (esquerda ou `right`) da area segura (era 14,5% da tela a 2%, ~22 px).</summary>
        AdBtn AdButton(string name, bool right, Sprite icon, string label, Action onClick)
        {
            var a = new AdBtn();
            float ax = right ? 1f : 0f;
            Image edge = Art.Panel(_safe, name, Art.Gold, new Vector2(ax, 0.026f), new Vector2(ax, AdTop));
            edge.rectTransform.offsetMin = new Vector2(right ? -Edge - AdW : Edge, 0f);
            edge.rectTransform.offsetMax = new Vector2(right ? -Edge : Edge + AdW, 0f);
            a.B = edge.gameObject.AddComponent<Button>();
            a.B.targetGraphic = edge;
            a.B.onClick.AddListener(() => onClick());
            a.Face = Art.Panel(edge.transform, "Face", AdFace, Vector2.zero, Vector2.one);
            a.Face.rectTransform.offsetMin = new Vector2(5f, 5f); a.Face.rectTransform.offsetMax = new Vector2(-5f, -5f);
            a.Face.raycastTarget = false;
            a.Icon = Art.Node(edge.transform, "Icone", new Vector2(0.1f, 0.36f), new Vector2(0.7f, 0.94f)).gameObject.AddComponent<Image>();   // a esquerda do selo
            a.Icon.sprite = icon; a.Icon.preserveAspect = true; a.Icon.raycastTarget = false;
            a.Label = Art.Outlined(Art.NewText(edge.transform, "Legenda", 32, new Vector2(0f, 0.03f), new Vector2(1f, 0.4f)), 2f);
            a.Label.fontStyle = FontStyle.Bold; a.Label.text = label;
            // selo de video: retangulo branco 44 x 32 com "play" escuro, 8 px para dentro do canto de cima a direita (v0.6d: saia pela borda)
            RectTransform v = Art.Node(edge.transform, "Video", new Vector2(1f, 1f), new Vector2(1f, 1f));
            v.pivot = new Vector2(1f, 1f); v.sizeDelta = new Vector2(44f, 32f); v.anchoredPosition = new Vector2(-8f, -8f);
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

        void ShowBanner(Banner b, string text, float dur, Sprite icon = null, Color tint = default)
        {
            b.T.text = text; b.Age = 0f; b.Dur = dur;
            if (icon != null && b.I != null) { b.I.sprite = icon; b.I.color = tint; }   // v0.6d: o aviso da encomenda tambem anuncia compras
            b.R.gameObject.SetActive(true);
        }

        /// <summary>Pop 0 -> 1,1 -> 1 em 0,3 s, segura e some nos ultimos 0,4 s.</summary>
        static void TickBanner(Banner b, float dt)
        {
            if (!b.R.gameObject.activeSelf) return;
            b.Age += dt;
            if (b.Age >= b.Dur) { b.R.gameObject.SetActive(false); return; }
            b.R.localScale = Vector3.one * Pop(b.Age);
            b.G.alpha = Mathf.Clamp01((b.Dur - b.Age) / 0.4f);
        }

        static float Pop(float age)
        {
            float k = Mathf.Clamp01(age / 0.3f);
            return k < 0.6f ? Mathf.Lerp(0.3f, 1.1f, k / 0.6f) : Mathf.Lerp(1.1f, 1f, (k - 0.6f) / 0.4f);
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
                _speedBtn.Label.text = can ? (active ? "→3×" : "2×") : Clock(_sim.BoostCooldown);   // v0.6d: com boost, o PROXIMO nivel ("3×" lia como o atual)
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

        // ---------- encomenda (v0.6c, docs/FASE9_ENCOMENDAS.md) ----------

        /// <summary>
        /// Cartao no estilo da pilula de ouro, no canto de baixo a esquerda (espelho do selo do boost, acima do botao VIP): icone do item,
        /// "Venda 5 espadas", barra com "3/5" e o premio com a moeda. Sob a pilula de ouro ele cobriria o balao do 1o da fila (8 vagas) e as
        /// moedas da rua, o mesmo motivo que tirou o selo do boost do topo. Criativo (-record): sem cartao nem aviso.
        /// v0.6d: 420 x 104 px a 32 px da borda e 24 px acima do botao VIP (era 0,02-0,40 da largura, colado nele); titulo com verbo,
        /// a conta "3/5" em cima da barra de 20 px.
        /// </summary>
        void BuildOrderUi()
        {
            if (Arg("-record") != null) return;
            Image edge = Art.Panel(_safe, "Encomenda", Art.Hex(0xF2D9A0), new Vector2(0f, AdTop), new Vector2(0f, AdTop));
            edge.rectTransform.pivot = Vector2.zero; edge.rectTransform.sizeDelta = new Vector2(420f, 104f);
            edge.rectTransform.anchoredPosition = new Vector2(Edge, 24f);
            edge.raycastTarget = false;
            Image face = Art.Panel(edge.transform, "Fundo", Art.ComAlfa(Art.Hex(0x2A1E14), 0.95f), Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(5f, 5f); face.rectTransform.offsetMax = new Vector2(-5f, -5f);
            face.raycastTarget = false;
            _orderIcon = Art.Node(edge.transform, "Item", new Vector2(0.03f, 0.1f), new Vector2(0.22f, 0.9f)).gameObject.AddComponent<Image>();
            _orderIcon.preserveAspect = true; _orderIcon.raycastTarget = false;
            // titulo numa linha so: caixa de ~44 px (2 linhas nao cabem) e o best-fit encolhe "Venda 30 ferramentas" em vez de quebrar
            _orderText = Art.Outlined(Art.NewText(edge.transform, "Texto", 34, new Vector2(0.24f, 0.5f), new Vector2(0.98f, 0.92f), TextAnchor.MiddleLeft), 2f);
            _orderText.fontStyle = FontStyle.Bold;
            _orderText.resizeTextForBestFit = true; _orderText.resizeTextMinSize = 22; _orderText.resizeTextMaxSize = 34;
            _orderText.verticalOverflow = VerticalWrapMode.Truncate;
            Image bar = Art.Node(edge.transform, "Barra", new Vector2(0.25f, 0.29f), new Vector2(0.64f, 0.29f)).gameObject.AddComponent<Image>();
            bar.rectTransform.offsetMin = new Vector2(0f, -10f); bar.rectTransform.offsetMax = new Vector2(0f, 10f);   // 20 px de altura
            bar.color = new Color(0f, 0f, 0f, 0.6f); bar.raycastTarget = false;
            _orderFill = Art.Node(bar.transform, "Cheio", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            _orderFill.color = Art.Good; _orderFill.raycastTarget = false;
            _orderBar = Art.Outlined(Art.NewText(bar.transform, "Conta", 24, Vector2.zero, Vector2.one), 2f);
            _orderBar.fontStyle = FontStyle.Bold; _orderBar.horizontalOverflow = HorizontalWrapMode.Overflow;
            _orderCoin = Art.Node(edge.transform, "Moeda", new Vector2(0.67f, 0.08f), new Vector2(0.77f, 0.5f)).gameObject.AddComponent<Image>();
            _orderCoin.sprite = _coinIcon.sprite; _orderCoin.color = _coinIcon.color; _orderCoin.preserveAspect = true; _orderCoin.raycastTarget = false;
            _orderPrize = Art.Outlined(Art.NewText(edge.transform, "Premio", 32, new Vector2(0.78f, 0.04f), new Vector2(0.99f, 0.54f), TextAnchor.MiddleLeft), 2f);
            _orderPrize.fontStyle = FontStyle.Bold; _orderPrize.color = Art.Accent; _orderPrize.horizontalOverflow = HorizontalWrapMode.Overflow;
            _order = edge.rectTransform;
            _orderG = edge.gameObject.AddComponent<CanvasGroup>();
            _orderG.blocksRaycasts = false;
            _order.gameObject.SetActive(false);
            _orderBanner = MakeBanner("AvisoEncomenda", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.62f), Art.Icon("moeda", "icone"), 42);   // logo abaixo do aviso do VIP
        }

        /// <summary>Cartao pelo estado do Sim: encomenda nova entra pulando como o aviso, cada venda dela da um pulso e, entregue (o Ev.OrderDone
        /// liga _orderOut), cresce e some em 0,35 s.</summary>
        void RefreshOrder(float dt)
        {
            if (_order == null) return;
            TickBanner(_orderBanner, dt);
            _orderT += dt; _orderPunch = Mathf.Max(0f, _orderPunch - dt);
            int it = _sim.OrderItem;
            if (it >= 0 && _orderKey != _sim.OrderCount)
            {
                _orderKey = _sim.OrderCount; _orderOut = false; _orderT = 0f; _orderSeen = -1;
                Sprite s = Art.ItemArt((Item)it, true);
                _orderIcon.sprite = s != null ? s : Art.ItemSprite((Item)it);
                _orderIcon.color = s != null ? Color.white : Art.ItemColor[it];
                _orderText.text = Textos.Venda(_sim.OrderTarget, it);
                _orderPrize.text = "+" + _sim.OrderReward;
                _order.gameObject.SetActive(true);
            }
            if (!_order.gameObject.activeSelf) return;
            if (_orderOut)
            {
                float k = _orderT / 0.35f;
                if (k >= 1f) { _order.gameObject.SetActive(false); return; }
                _order.localScale = Vector3.one * (1f + 0.25f * k);
                _orderG.alpha = 1f - k;
                return;
            }
            if (_sim.OrderProgress != _orderSeen)
            {
                if (_orderSeen >= 0) _orderPunch = PunchTime;
                _orderSeen = _sim.OrderProgress;
                OrderText(_orderSeen, _sim.OrderTarget);
            }
            _order.localScale = Vector3.one * Pop(_orderT) * (1f + 0.12f * Mathf.Sin(Mathf.PI * _orderPunch / PunchTime));
            _orderG.alpha = 1f;
        }

        void OrderText(int done, int target)
        {
            _orderBar.text = $"{done}/{target}";
            _orderFill.rectTransform.anchorMax = new Vector2(target > 0 ? Mathf.Clamp01(done / (float)target) : 0f, 1f);
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

        // ---------- configuracoes: som e vibracao (v0.6b, BENCHMARK_VISUAL P2-4; a versao saiu da HUD para ca, P1-1) ----------

        /// <summary>
        /// Engrenagem na faixa de cima (a direita da dica, mesmo estilo da pilula de ouro) abre o cartao creme do menu: Som, Vibracao
        /// (so no Android; no PC aparece apagada), a versao e FECHAR. Nao e' pausa: como o painel do cofre, trava o joystick e a
        /// oficina segue trabalhando (o ferreiro para). Criativo (-record): sem engrenagem.
        /// </summary>
        void BuildSettings(RectTransform top)
        {
            if (Arg("-record") == null)
            {
                Image gear = Art.Panel(top, "Engrenagem", Art.Hex(0xF2D9A0), new Vector2(0.89f, 0.1f), new Vector2(1f, 0.9f));
                Image gearIn = Art.Panel(gear.transform, "Fundo", Art.ComAlfa(Art.Hex(0x2A1E14), 0.95f), Vector2.zero, Vector2.one);
                gearIn.rectTransform.offsetMin = new Vector2(5f, 5f); gearIn.rectTransform.offsetMax = new Vector2(-5f, -5f);
                gearIn.raycastTarget = false;
                Image icon = Art.Node(gear.transform, "Icone", new Vector2(0.17f, 0.17f), new Vector2(0.83f, 0.83f)).gameObject.AddComponent<Image>();
                icon.sprite = Art.Gear(); icon.color = Art.Hex(0xF2D9A0); icon.preserveAspect = true; icon.raycastTarget = false;
                Button gb = gear.gameObject.AddComponent<Button>();
                gb.targetGraphic = gear;
                gb.onClick.AddListener(() => { Sfx.Play("drop"); _cfg.gameObject.SetActive(true); });
            }

            _cfg = Art.Node(_safe, "Configuracoes", Vector2.zero, Vector2.one);
            _cfg.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);   // escurece e segura os toques do resto
            Image edge = Art.Panel(_cfg, "Cartao", MenuBar.Leather, new Vector2(0.1f, 0.33f), new Vector2(0.9f, 0.67f));
            Image face = Art.Panel(edge.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(6f, 14f); face.rectTransform.offsetMax = new Vector2(-6f, -6f);   // chanfro de couro embaixo
            face.raycastTarget = false;
            Text title = Art.NewText(face.transform, "Titulo", 60, new Vector2(0.05f, 0.8f), new Vector2(0.95f, 0.97f));
            title.text = "Configurações"; title.fontStyle = FontStyle.Bold; title.color = MenuBar.Brown;
            Chave(face.transform, "Som", 0.58f, () => Ajustes.Som, v => { SetSom(v); if (v) Sfx.Play("coin"); }, true);
            Chave(face.transform, "Vibração", 0.38f, () => Ajustes.Vibra, v => { SetVibra(v); if (v) Ajustes.Confirmar(); }, Ajustes.TemVibra);
            Text ver = Art.NewText(face.transform, "Versao", 32, new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.35f));
            ver.text = "Forge Street v" + Application.version; ver.color = Art.ComAlfa(MenuBar.Brown, 0.65f);
            Button close = Art.NewButton(face.transform, "FECHAR", 46, MenuBar.Leather, new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.22f), () => _cfg.gameObject.SetActive(false));
            close.GetComponentInChildren<Text>().color = MenuBar.Cream;
            _cfg.gameObject.SetActive(false);
        }

        // v0.6d (revisao de UX): LIGADO = texto #1F3B12 de 30 px no verde #5BD16B; DESLIGADO = branco no #8C7B6B; o rotulo fica #3B2A1A
        // nos dois (so a Vibracao do PC continua apagada)
        static readonly Color OnFace = Art.Hex(0x5BD16B), OnInk = Art.Hex(0x1F3B12), OffFace = Art.Hex(0x8C7B6B), OffEdge = Art.Hex(0x6B5D50), RowInk = Art.Hex(0x3B2A1A);

        /// <summary>Linha "rotulo + chave": pilula verde (LIGADO) ou cinza (DESLIGADO) com chanfro e bolinha branca; o texto diz o estado.</summary>
        void Chave(Transform parent, string label, float y, Func<bool> get, Action<bool> set, bool enabled)
        {
            Text l = Art.NewText(parent, label, 50, new Vector2(0.07f, y), new Vector2(0.48f, y + 0.17f), TextAnchor.MiddleLeft);
            l.text = label; l.fontStyle = FontStyle.Bold; l.color = RowInk;
            Image edge = Art.Panel(parent, label + " Chave", MenuBar.GreenDark, new Vector2(0.5f, y + 0.01f), new Vector2(0.93f, y + 0.16f));
            Image face = Art.Panel(edge.transform, "Face", OnFace, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(0f, 7f); face.raycastTarget = false;
            Image knob = Art.Node(face.transform, "Bolinha", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            knob.sprite = Art.Disc(); knob.color = Color.white; knob.preserveAspect = true; knob.raycastTarget = false;
            Text state = Art.NewText(face.transform, "Estado", 30, Vector2.zero, Vector2.one);
            state.fontStyle = FontStyle.Bold;
            Button b = edge.gameObject.AddComponent<Button>();
            b.targetGraphic = edge;
            void Paint()
            {
                bool on = get();
                edge.color = on ? MenuBar.GreenDark : OffEdge;
                face.color = on ? OnFace : OffFace;
                state.color = on ? OnInk : Color.white;
                knob.rectTransform.anchorMin = new Vector2(on ? 0.72f : 0.03f, 0.12f);
                knob.rectTransform.anchorMax = new Vector2(on ? 0.97f : 0.28f, 0.88f);
                state.rectTransform.anchorMin = new Vector2(on ? 0.03f : 0.28f, 0f);
                state.rectTransform.anchorMax = new Vector2(on ? 0.72f : 0.97f, 1f);
                state.text = on ? "LIGADO" : "DESLIGADO";
            }
            b.onClick.AddListener(() => { set(!get()); Paint(); });
            Paint();
            if (enabled) return;
            // sem vibrador (PC): apagada e sem toque
            b.interactable = false;
            edge.gameObject.AddComponent<CanvasGroup>().alpha = 0.45f;
            l.color = Art.ComAlfa(RowInk, 0.45f);
        }

        void SetSom(bool on) { Ajustes.Som = on; Log("settings", "som", on ? "1" : "0"); }
        void SetVibra(bool on) { Ajustes.Vibra = on; Log("settings", "vibra", on ? "1" : "0"); }

        /// <param name="gold">&gt; 0: linha da moeda com "+gold" grande (cofre); 0: so o texto.</param>
        void ShowPanel(string title, string body, string button, Action action, long gold = 0)
        {
            _panel.gameObject.SetActive(true);
            _panelTitle.text = title;
            _panelBody.text = body;
            _panelBtn.text = button;
            _panelAction = action;
            _panelRow.gameObject.SetActive(gold > 0);
            if (gold <= 0) return;
            _panelGold.text = "+" + gold;
            _panelRow.sizeDelta = new Vector2(96f + _panelGold.preferredWidth, 90f);   // moeda + numero centrados juntos
        }

        void ClosePanel()
        {
            _panel.gameObject.SetActive(false);
            _panelAction = null;
        }

        // ---------- save, dev e diario ----------

        static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// A-PLAT-02: arquivo (principal, senao .bak; o que nao abre vai para a quarentena); sem arquivo bom, a chave antiga do PlayerPrefs
        /// (save da 0.1-0.6 = migracao; da 0.7 = espelho). Nunca grava estado novo por cima de save que nao abriu: o ruim ja saiu do
        /// caminho (espelho ruim fica em fs.save.corrupt), e versao mais nova ou disco ilegivel deixam a sessao sem gravar.
        /// </summary>
        void LoadSave()
        {
            SaveEnvelope.Result r = default; int corrupt = 0; string err = null;
            try { r = SaveStore.Read(_savePath, out corrupt); }
            catch (Exception e) { err = e.GetType().Name; _noSave = true; }
            bool prefs = r.Status == SaveEnvelope.Status.Missing || r.Status == SaveEnvelope.Status.Corrupt;
            if (prefs)
            {
                string raw = PlayerPrefs.GetString(SaveKey, "");
                SaveEnvelope.Result p = SaveEnvelope.Unwrap(raw);
                if (p.Status == SaveEnvelope.Status.Corrupt) { PlayerPrefs.SetString(SaveKey + ".corrupt", raw); corrupt++; }   // ponytail: 1 vaga de quarentena
                else if (p.Status != SaveEnvelope.Status.Missing) r = p;
            }
            bool ok = r.Status == SaveEnvelope.Status.Ok || r.Status == SaveEnvelope.Status.Legacy;
            _noSave |= r.Status == SaveEnvelope.Status.NewerSchema;
            _sim = Sim.Load(ok ? r.Payload : null);
            if (err != null) Log("save_error", err);
            if (corrupt > 0) Log("save_corrupt", corrupt.ToString(), r.Status.ToString());   // Ok/Legacy = recuperado; outro = estado novo
            if (r.Status == SaveEnvelope.Status.NewerSchema) Log("save_newer", r.Schema.ToString(), r.Content);
            else if (prefs && ok) Log("save_prefs", r.Status.ToString());   // Legacy = migrou o save da 0.6
        }

        void Save()
        {
            if (_sim == null || _headless || _testSession || _noSave) return;
            long now = Now();
            string text = SaveEnvelope.Wrap(_sim.Save(now), Application.version, now);
            if (_sim.ClockWentBack > 0) Log("clock_back", _sim.ClockWentBack.ToString(), _sim.SavedAt.ToString());   // A-CORE-02: s atras, SavedAt mantido
            try { SaveStore.Write(_savePath, text); }
            catch (Exception e) { Log("save_error", e.GetType().Name); }   // disco cheio nao derruba o jogo; o espelho segura o progresso
            PlayerPrefs.SetString(SaveKey, text);   // espelho na chave antiga: um downgrade para a 0.6 le o mesmo texto
            PlayerPrefs.Save();
        }

        // Voltar de outro app tambem paga o cofre (antes so a abertura pagava: quem trocava de app perdia o tempo fora).
        // _started: a Unity chama OnApplicationPause(false) logo apos o Awake, e na abertura quem paga e' o Start.
        // ponytail: < 60 s fora nao paga nem mostra painel (perde no maximo 15 s de producao); vira Balance se o playtest pedir.
        // _pausedByAd: o anuncio real tira o app da frente; a volta dele nao e' "tempo fora" (nao mostra "Seu cofre rendeu")
        void OnApplicationPause(bool paused)
        {
            if (paused) { _pausedByAd = Ads.Busy; Save(); }
            else if (_started && !_pausedByAd && _sim != null && _sim.SavedAt > 0 && Now() - _sim.SavedAt >= 60) Offline();
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
            if (long.TryParse(Arg("-gold"), NumberStyles.Integer, CultureInfo.InvariantCulture, out long g)) _sim.Gold = Math.Max(0, g);   // A-CORE-06: aceita > int (foto da HUD)
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

        // build de desenvolvimento: Ads.Arg tambem le o extra "unity" do intent, entao as flags valem no aparelho
        // (adb shell am start -n br.com.vstack.forgestreet/com.unity3d.player.UnityPlayerGameActivity -e unity "-cam 9"). Release: so linha de comando
        static string Arg(string name) => Ads.Arg(name);

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
            else if (Arg("-shotorder") != null)
            {
                // v0.6c: espera o aviso da 1a encomenda entregue (ate 180 s) e fotografa -shotdelay s depois (moedas no ar, cartao saindo)
                float until = Time.realtimeSinceStartup + 180f;
                while (_orderBanner != null && !_orderBanner.R.gameObject.activeSelf && Time.realtimeSinceStartup < until) yield return null;
                yield return new WaitForSecondsRealtime(delay);
            }
            else yield return new WaitForSecondsRealtime(delay);
            Debug.Log($"SHOT t={_sim.Time:0.0} upgrades={_sim.UpgradesBought} nobres={_sim.JewelQueue.Count}/{_sim.JewelQueueCap} baus={string.Join(",", _sim.Chests.ConvertAll(c => c.State))} som={Ajustes.Som} vibra={Ajustes.Vibra}");
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
