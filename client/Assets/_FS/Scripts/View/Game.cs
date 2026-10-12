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
    /// Fluxo do jogo, o unico MonoBehaviour de fluxo: carrega/grava o Sim (Salvamento: save.txt pelo SaveStore + espelho no PlayerPrefs), aplica o cofre offline, le o
    /// joystick flutuante (e WASD no PC), avanca a simulacao em passos de ate 1/30 s, toca SFX pelos eventos, segue o
    /// jogador com a camera em retrato, monta a HUD por codigo e grava o diario de playtest
    /// (persistentDataPath/diario.csv). Nasce sozinho em qualquer cena.
    /// A-PLAT-06: fala com os servicos so pelas costuras IAds, IAnalytics e ISaveStorage (Servicos.cs). O diario (Diario) so grava no
    /// build de desenvolvimento ou com -playtest (release no aparelho: nada) e roda a partir de 1 MB.
    /// Flags de dev: -autoplay [min] (bot sem render, loga AUTOPLAY por minuto, sai 0/1) | -shot foto.png
    /// [-shotdelay s] [-shotchest] [-shotorder] [-bot] [-menu] | -speed N | -reset (apaga o save) | -testsession (sem persistencia) |
    /// -record pasta [-recordsec S] [-recordfps F] (quadros 1080x1920 para os criativos, docs/CRIATIVOS.md) | -buyids 1,5,0@300 |
    /// -warmup S | -hold 0,3,3,3,0,0 | -stock 10,10,10 (fotos de validacao) | -fakeads | -adtest vip|velocidade (IAds.Show no
    /// inicio; docs/LEVELPLAY.md) | -vipnow (chama o VIP depois do warmup) | -boost N (N anuncios de velocidade antes do warmup) |
    /// -cam W (largura visivel em m, 6..11,4; experimento de camera mais perto) | -settings [on|off] (abre as configuracoes; on/off
    /// liga/desliga Som e Vibracao antes, gravando como o toque) | -cofre N (mostra o painel do cofre com N de ouro, sem dar o ouro) |
    /// -playtest (liga o diario num build de release; o dev ja grava) | -privacidade [18] (abre a tela de idade mesmo com resposta ou no
    /// -testsession, onde a resposta nao e' gravada; 18 = direto no consentimento).
    /// G-UA-02 (Captura.cs, docs/CRIATIVOS.md): -preset nome (alias de flags; a linha de comando vence) | -recordhire (o 1o quadro e' o da
    /// 1a contratacao) | -recordfull (fila cheia no 1o quadro ou erro) | -recordorder (o -record mantem o cartao da encomenda) | -offline S
    /// (cofre real de S s fora, 60..7200). Captura que mostraria algo impossivel (compra id@t sem ouro ou pre-requisito, -vipnow depois de
    /// um warmup sem venda) loga o erro e sai 1 (Falha).
    /// Build dev: tambem pelo intent do Android (Arg).
    /// v0.5c (docs/FASE8_VIP_VELOCIDADE.md s5): botoes "Chamar VIP" e "Velocidade 2x/3x" nos cantos da barra de baixo (IAds.Show ->
    /// Sim.SummonVip / Sim.StartBoost), selo do boost com cronometro, aviso "Cliente VIP!" e diario vip_* / boost_start.
    /// v0.6c (docs/FASE9_ENCOMENDAS.md s6): cartao da encomenda acima do botao VIP, aviso "Encomenda entregue!" e diario order_*.
    /// v0.6d (revisao de UX, docs/VALIDACAO_V06A.md s v0.6d): dica de 1 linha (Textos), botoes a 32 px das bordas, cartao "Venda 5
    /// espadas", compra no aviso da encomenda, moedas por baixo da dica, chaves e cofre no cartao creme.
    /// </summary>
    public sealed class Game : MonoBehaviour
    {
        const string SaveKey = "fs.save";
        const float SaveEvery = 5f, MaxStep = 1f / 30f;
        // Largura visivel (A-ART-03, P12 decidido por padrao): 9 m, a camera segue o ferreiro e puxa a fila para cima (Enquadramento).
        // Era 11,4 m = oficina inteira + margens (o -cam 11.4 volta a ela).
        const float VisibleWidth = Enquadramento.Largura;
        // fracao da tela reservada a faixa de cima; o mundo nao chega la. v0.5b (BENCHMARK P1-1): 0,15 -> 0,075 (~144 px na referencia),
        // so 2 pilulas (ouro | dica) sem painel cheio e sem o numero de versao
        const float HudBand = 0.075f;
        const float GoldRoll = 12f;                              // 1/s do numero rolando: ~0,3 s ate o valor novo (P0-5)
        // Moedas da venda (BENCHMARK_VISUAL P0-5, versao visual): saem do cliente e voam ate a moeda da HUD; o numero so sobe quando
        // elas chegam (o ouro do Sim ja mudou: mostra Gold - _pending) e a moeda e o numero pulsam 1 -> 1,15 -> 1.
        const float CoinFlight = 0.4f, CoinGap = 0.04f, CoinPx = 56f, PunchTime = 0.2f;
        const int CoinPool = 30;
        const int BigSale = 60;   // A-AUD-22: venda grande = >= 60 de ouro numa venda normal (6 espadas; hoje so a joia, 60/80)

        Sim _sim;
        Bot _bot;
        WorldView _view;
        Joystick _joy;
        MenuBar _menu;
        Camera _cam;
        float _speed = 1f, _saveT, _hintT, _minuteT, _adHold, _visW = VisibleWidth, _backT;
        bool _botDrive, _headless, _firstSaleLogged, _testSession, _started, _pausedByAd, _paused;
        IAds _ads;
        IAnalytics _diario;
        Salvamento _save;
        // A-PLAT-06 (taxonomia s86/s87): s em primeiro plano da sessao, quadros e s reais da janela de fps, passos do tutorial ja logados,
        // oferta de anuncio ja contada nesta aparicao do botao e ouro que entrou/saiu desde a ultima linha currency_* (por origem/destino)
        float _sessionT, _fpsT; int _fpsN, _tutorial; bool _vipOffer, _speedOffer;
        readonly System.Collections.Generic.Dictionary<string, long> _source = new System.Collections.Generic.Dictionary<string, long>(),
            _sink = new System.Collections.Generic.Dictionary<string, long>();
        Vector2Int _screen;
        System.Collections.Generic.List<(int u, float t)> _buyAt = new System.Collections.Generic.List<(int u, float t)>();   // -buyids id@t (Captura.Compras)
        bool _hired;   // G-UA-02 -recordhire: ja houve Ev.Hired ao vivo

        Canvas _joyCanvas;
        RectTransform _canvas, _labels, _safe, _panel, _fx;
        Text _gold, _hint, _panelTitle, _panelBody, _panelBtn, _panelGold;
        RectTransform _panelRow;   // v0.6d: moeda + "+N" do cofre no painel
        Hint _hintH; int _hintArg = -1, _hintU;   // v0.6d: a dica so e' remontada quando muda (sem string nova a cada 0,25 s)
        RectTransform _cfg;   // v0.6b: cartao de configuracoes (Som, Vibracao, versao)
        TelaPrivacidade _priv;   // E-PLAT-01: idade + consentimento (Privacidade.cs)
        Action _panelAction;
        Image _coinIcon;
        sealed class Coin { public Image I; public Vector3 From; public float T; public int Value; }   // T < 0: esperando a vez
        readonly System.Collections.Generic.List<Coin> _coins = new System.Collections.Generic.List<Coin>();
        int _pending; long _goldInt = -1; float _punchT; double _goldShown = -1;   // A-CORE-06: double/long, float perdia unidade acima de 16 mi
        // v0.5c: botoes de anuncio, selo do boost e avisos curtos (VIP chegou / anuncio indisponivel)
        sealed class AdBtn { public Button B; public Image Face, Icon; public GameObject Video; public Text Label; }
        sealed class Banner { public RectTransform R; public CanvasGroup G; public Text T; public Image I; public float Age; }
        AdBtn _vipBtn, _speedBtn;
        RectTransform _boostSeal; Text _boostMul, _boostTime;
        // A-UI-19: um aviso so (VIP, encomenda, compra, anuncio), em fila, no lugar da pilula da dica: a faixa da HUD que a camera
        // reserva, entao nunca cobre estacao, fila nem balao (o balao do 1o da fila fica 24 px abaixo dela)
        Banner _notice;
        readonly System.Collections.Generic.Queue<(string text, Sprite icon, Color tint)> _notices = new System.Collections.Generic.Queue<(string, Sprite, Color)>(NoticeMax + 1);
        const int NoticeMax = 3;              // esperando; cheia, o mais velho cai
        const float NoticeTime = 1.4f;        // s por aviso (pop 0,3 s, some nos ultimos 0,4 s)
        const string Delivered = "Encomenda entregue! +";   // o -shotorder espera este aviso
        Sprite _crown;
        bool _vipNow;
        // v0.6c: cartao da encomenda (FASE9) e o estado da animacao (_orderKey = OrderCount da encomenda na tela)
        RectTransform _order; CanvasGroup _orderG; Image _orderIcon, _orderFill, _orderCoin; Text _orderText, _orderPrize, _orderBar;
        int _orderKey = -1, _orderSeen = -1; float _orderT, _orderPunch; bool _orderOut;
        static readonly Color AdFace = Art.HudFace, AdOff = MenuBar.GrayDark;   // A-UI-16: era roxo #3B2650 / #5A5560; a borda de ouro e o ▶ marcam o anuncio
        // v0.6d: botoes de anuncio e cartoes de baixo a 32 px das bordas da area segura, com largura fixa; topo dos botoes de anuncio
        const float Edge = 32f, AdW = 140f, AdTop = MenuBar.Band - 0.004f;

        /// <summary>A-PLAT-07: o estado do jogo, so derivado dos flags (Estados.De; tabela em Estado.cs). O voltar, o Save e os anuncios
        /// consultam aqui.</summary>
        Estado EstadoAtual => Estados.De(_sim != null, _started, _ads != null && _ads.Busy, _paused, Painel || Config || Priv,
            _order != null && _orderOut && _order.gameObject.activeSelf);
        bool Painel => _panel != null && _panel.gameObject.activeSelf;
        bool Config => _cfg != null && _cfg.gameObject.activeSelf;
        bool Priv => _priv != null && _priv.Aberta;

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
            // G-UA-02: fumaca, faiscas e poeira sorteiam no UnityEngine.Random; semente fixa = os mesmos quadros em 2 execucoes
            if (Arg("-record") != null || Arg("-preset") != null) UnityEngine.Random.InitState(Captura.Semente);
            // A-PLAT-06: diario so no build de desenvolvimento (o build de playtest de hoje, APK dev e Windows) ou com -playtest (release
            // do PC); release no aparelho nao grava (nao tem linha de comando, e o extra do intent so vale no dev). Teste: nunca.
            string dir = Application.persistentDataPath;
            _diario = new Diario(!_testSession && (Debug.isDebugBuild || Arg("-playtest") != null) ? Path.Combine(dir, "diario.csv") : null);
            _save = new Salvamento(new SaveStore(Path.Combine(dir, "save.txt")), new SaveEspelho(SaveKey), _diario);
            Sfx.Init(gameObject);
            // ponytail: fotos e smoke usam estado novo sem ler ou gravar o save/diario normal.
            if (!_testSession && Arg("-reset") != null) _save.Apagar();
            _sim = _testSession ? new Sim() : _save.Carregar();
            DevArgs();
            // -cam W (dev, P2-3 do BENCHMARK_VISUAL): largura visivel em m, 6..11,4 (padrao 9; 11,4 = a camera antiga, fotos de antes)
            _visW = Mathf.Clamp(ArgF("-cam", VisibleWidth), 6f, Enquadramento.OficinaInteira);
            string cfg = Arg("-settings");
            if (cfg == "on" || cfg == "off") { SetSom(cfg == "on"); SetVibra(cfg == "on"); }   // fotos e teste de persistencia
            _ads = Ads.Init((e, a, b) => Log(e, a, b));   // pedido, mostrado, recompensa e falha do anuncio vao para o diario
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
            _menu = new MenuBar(_safe, _sim, u => Bought(u, _sim.T.Cost(u), _sim.Player.Pos));
            _cfg.SetAsLastSibling();     // configuracoes e painel modal cobrem a barra de melhorias
            _panel.SetAsLastSibling();
            // E-PLAT-01: sem resposta (ou com texto novo) a tela de idade abre por cima de tudo e o SDK de anuncio espera (Ads.Responder).
            // Fotos e smoke (-testsession, -record, -autoplay) nao a veem, salvo com -privacidade [18]
            _priv = new TelaPrivacidade(_safe, Responder);
            string priv = Arg("-privacidade");
            if (priv != null || (!_testSession && !Ads.Resposta(out _, out _))) _priv.Abrir(null, priv == "18");
            if (Arg("-menu") != null) _menu.Toggle();   // foto com a fileira aberta
            if (cfg != null) _cfg.gameObject.SetActive(true);
            _joy = new Joystick(_joyCanvas.transform);
            _joy.TopBand = HudBand;
            Fit();
            FollowCamera(100f);
        }

        void Start()
        {
            if (_headless) return;   // Falha ja no Awake (preset desconhecido, compra impossivel no warmup): nada grava
            if (Arg("-autoplay") != null) { Autoplay(); return; }
            Log("session_start", Application.version, SystemInfo.deviceModel);
            // s87: contexto da sessao, uma linha por campo (o build id e' o GUID do build, nao do aparelho; nada de ID, conta ou local)
            Log("session_info", "build", string.IsNullOrEmpty(Application.buildGUID) ? "editor" : Application.buildGUID);
            Log("session_info", "os", SystemInfo.operatingSystem);
            Log("session_info", "ram", Diario.FaixaRam(SystemInfo.systemMemorySize));
            _started = true;   // A-PLAT-07: Loading -> Playing; o save grava daqui em diante (o resgate do cofre logo abaixo ja grava)
            Offline();
            if (int.TryParse(Arg("-cofre"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cofre) && cofre > 0) ShowVault(cofre);   // foto do painel
            // G-UA-02 -offline S: o cofre de verdade, como na volta do jogador depois de S s fora (o -cofre N so desenha o painel com N)
            string off = Arg("-offline");
            if (off != null)
            {
                long fora = Captura.OfflineSegundos(off);
                if (fora < 0) { Falha($"OFFLINE: '{off}' fora de 60..{Balance.OfflineCapSeconds} s"); return; }
                if (PagaCofre(fora, _sim.LastClaim + 1) <= 0) { Falha("OFFLINE: cofre vazio (o warmup nao teve vendas)"); return; }
            }
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
            for (float t = 0f; !_ads.Loaded(p) && t < 15f; t += Time.unscaledDeltaTime) yield return null;
            _ads.Show(p, () => ShowPanel("Anúncio de teste", $"Recompensa recebida: {p}", "OK", ClosePanel),
                () => ShowPanel("Anúncio de teste", $"Sem recompensa: {p}", "OK", ClosePanel));
        }

        /// <summary>Cofre: o claim e' idempotente pelo SavedAt do save (o Core recusa id repetido ou mais antigo).</summary>
        void Offline()
        {
            if (_sim.SavedAt <= 0) return;
            long elapsed = Now() - _sim.SavedAt;
            if (elapsed < 60) return;   // mesma regra da volta de pausa: abrir de novo em < 1 min mostrava "31 de ouro em 0 min fora" (POCO F4)
            PagaCofre(elapsed, _sim.SavedAt);
        }

        /// <summary>Paga o cofre de `elapsed` s fora (claim idempotente por `claim`), mostra o painel e devolve o ouro (0 = nada).</summary>
        long PagaCofre(long elapsed, long claim)
        {
            long gold = _sim.ApplyOffline(elapsed, claim);
            if (gold <= 0) return 0;
            long shown = Math.Min(elapsed, _sim.T.OfflineCap);
            Log("offline_claim", gold.ToString(), shown.ToString());
            Source("cofre", gold);
            Sfx.Play("offline");
            ShowVault(gold);
            Save();
            return gold;
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
                // voltar do Android (revisao da v0.6; A-PLAT-07 pelo estado): fecha o que estiver aberto, de cima para baixo; no anuncio
                // (o simulado recebe o voltar) nada; so com nada aberto grava e sai
                if (Priv) _priv.Voltar();   // E-PLAT-01: consentimento -> idade; revisao fecha; 1a abertura fica
                else switch (Estados.AoVoltar(EstadoAtual, Painel, _menu.IsOpen))
                {
                    case Voltar.FechaPainel: ClosePanel(); break;
                    case Voltar.FechaConfig: _cfg.gameObject.SetActive(false); break;
                    case Voltar.FechaMenu: _menu.Toggle(); break;
                    case Voltar.GravaESai: Save(); Application.Quit(); return;
                }
            }
            if (_screen.x != Screen.width || _screen.y != Screen.height) Fit();

            bool modal = Painel || Config || Priv;
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
            bool ad = EstadoAtual == Estado.Ad;
            _adHold = ad ? _adHold + Mathf.Min(Time.unscaledDeltaTime, 0.1f) : 0f;
            // E-PLAT-01: a tela de idade tambem para a oficina (na 1a abertura o cliente nao vai embora enquanto o jogador le); o -bot
            // segue jogando por baixo (smoke do emulador num APK recem-instalado)
            float dt = (ad && _adHold < 15f) || (Priv && !_botDrive) ? 0f : Mathf.Min(Time.deltaTime, 0.1f) * _speed;
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
            Sfx.Tick(_sim, Time.unscaledDeltaTime);   // A-AUD-23: musica e ambiente que cresce com estacoes trabalhando e fila
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
                FlushGold();
            }
            // s87: sessao em s de primeiro plano (o teto de 1 s por quadro tira o salto da volta do segundo plano) e faixa de fps por
            // minuto real (a janela zera na volta da pausa)
            _sessionT += Mathf.Min(Time.unscaledDeltaTime, 1f);
            _fpsN++; _fpsT += Time.unscaledDeltaTime;
            if (_fpsT >= 60f)
            {
                float fps = _fpsN / _fpsT;
                Log("fps_bucket", Diario.FaixaFps(fps), fps.ToString("0", CultureInfo.InvariantCulture));
                _fpsN = 0; _fpsT = 0f;
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
                        // A-AUD-22: venda comum nao vibra; so a grande, no maximo 1 a cada 1,5 s (a loja de joias atende a cada 0,4 s).
                        // A unidade do VIP (preco x3) fica de fora: o pacote vibra uma vez no VipServed
                        if (e.B >= BigSale && e.B <= _sim.PriceOf((Item)e.A)) Ajustes.Pulso(20, 90, 1.5f);
                        _view.SaleFloat(new V2(e.Pos.X, e.Pos.Y - 0.9f), e.B);   // v0.5: pela frente do estande; v0.6d: vendas a < 0,4 s somam num "+N" so
                        _view.Sold(e.Pos, e.A == (int)Item.Jewel);   // v0.5b: balao estoura com coracao e brilhos
                        FlyCoins(e.Pos, e.B);
                        Log("product_sold", Balance.ItemName[e.A], e.B.ToString());
                        Source(e.B > _sim.PriceOf((Item)e.A) ? "vip" : "venda", e.B);   // a unidade do VIP sai a preco x3
                        break;
                    case Ev.Bought: Bought(e.A, e.B, e.Pos); break;
                    case Ev.ClientArrived: Sfx.Play("client", 1f, 0.2f); break;
                    case Ev.ClientLeft:
                        if (e.B == 0) { Sfx.Play("leave"); _view.Float(e.Pos, "...", Art.Bad); }
                        else _view.TurnedAway(e.Pos, e.A == (int)Item.Jewel);   // fila cheia: aparece passando e vai embora
                        Log("client_left", Balance.ItemName[e.A], e.B == 0 ? "cansou" : "fila_cheia");
                        break;
                    case Ev.Bottleneck: Log("bottleneck", _sim.Stations[e.A].Name, "saida_cheia"); break;
                    case Ev.Hired: Sfx.Play("hire"); Log("worker_hired", e.A.ToString()); _hired = true; break;
                    case Ev.Unlocked: Log("station_unlock", _sim.Stations[e.B].Name); break;
                    // baus de marco (docs/archive/AREA2_FASE2.md s3/s4): bateu = som de moeda agudo + o ferreiro comemora (o bau aparece
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
                        Source("bau", e.B);
                        break;
                    // FASE8 (v0.5c): VIP entrou na vaga / levou o pacote (cada unidade ja saiu como Sold) / cansou
                    case Ev.VipArrived:
                        Sfx.Play("vip");
                        Notice("Cliente VIP!", _crown, Color.white);
                        _view.VipBurst(e.Pos, false);
                        Log("vip_arrived", Balance.ItemName[e.A], e.B.ToString());
                        break;
                    case Ev.VipServed:
                        Sfx.Play("offline", 1.15f);
                        Ajustes.Pulso(40, 200);
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
                        Source("encomenda", e.B);
                        Ajustes.Pulso(40, 200);
                        if (_order == null) break;   // criativo: sem cartao, o ouro entra direto no numero
                        Sfx.Play("order");
                        OrderText(_sim.OrderTarget, _sim.OrderTarget);   // a ultima venda e a entrega caem no mesmo tick: mostra o 5/5
                        _orderOut = true; _orderT = 0f;
                        Notice(Delivered + e.B, _coinIcon.sprite, _coinIcon.color);
                        FlyCoins(_orderCoin.rectTransform.position, e.B);
                        break;
                }
            }
            if (!_firstSaleLogged && _sim.FirstSaleTime >= 0f)
            {
                _firstSaleLogged = true;
                Log("first_sale", _sim.FirstSaleTime.ToString("0.0", CultureInfo.InvariantCulture));
                // tutorial = as dicas da 1a volta ate a 1a venda (RefreshHud); so fecha na sessao em que ele aconteceu
                if (_tutorial > 0) Log("tutorial_complete", _sim.FirstSaleTime.ToString("0.0", CultureInfo.InvariantCulture), _tutorial.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Compra (pad, bot ou toque no menu): som, aviso "Nome!" e diario. v0.6d: o aviso e' o mesmo do "Encomenda entregue!", com o
        /// icone da melhoria (era um "+nome!" verde no mundo, que se misturava aos "+N" das vendas). Criativo (sem cartao): o flutuante de antes.
        /// </summary>
        void Bought(int u, long price, V2 pos)
        {
            Sfx.Play("upgrade");
            // A-AUD-22: compra = pulso medio; compra que abre estacao (o Ev.Unlocked do mesmo tick) ou a rua lateral = o mais forte
            bool opens = u == (int)Upgrade.SideCorridor;
            foreach (Station s in _sim.Stations) opens |= s.UnlockBy == u;
            Ajustes.Pulso(opens ? 60 : 30, opens ? 255 : 150);
            if (_order == null) _view.Float(pos, Upgrades.All[u].Name + "!", Art.Good, 44);
            else
            {
                Sprite icon; Color tint;
                if (Upgrades.All[u].InMenu) (icon, tint) = MenuBar.Icon((Upgrade)u);
                else icon = _view.PadIcon(u, out tint, out _);
                Notice(Upgrades.All[u].Name + "!", icon, tint);
            }
            Log("upgrade_buy", ((Upgrade)u).ToString(), price.ToString());
            Count(_sink, Upgrades.IsLuxury(u) ? "luxo" : "upgrade", price);
        }

        // s86 currency_source/currency_sink: o ouro soma por origem/destino e sai numa linha por chave a cada minuto de jogo e na pausa/saida
        // (venda por venda dobraria o diario: a joalheria vende a cada 0,4 s)
        void Source(string origem, long gold) => Count(_source, origem, gold);

        static void Count(System.Collections.Generic.Dictionary<string, long> d, string key, long gold)
        {
            d.TryGetValue(key, out long v);
            d[key] = v + gold;
        }

        void FlushGold()
        {
            foreach (var kv in _source) Log("currency_source", kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture));
            foreach (var kv in _sink) Log("currency_sink", kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture));
            _source.Clear(); _sink.Clear();
        }

        // ---------- camera e HUD ----------

        float _hudFrac, _barFrac, _trayFrac;   // A-ART-03: fracao da altura da tela sob a faixa de cima, a barra e a barra + bandeja (Fit)

        void Fit()
        {
            _screen = new Vector2Int(Screen.width, Screen.height);
            AreaSegura.AncorarDentro(_safe, AreaSegura.Atual());
            Rect sa = Screen.safeArea;
            _view.HudBottomPx = sa.yMin + sa.height * (1f - HudBand);   // v0.6d: o balao do 1o da fila fica 24 px abaixo da faixa de cima
            // pela area segura, como a HUD: com recorte no topo (POCO) a faixa desce e a camera reserva o que ela cobre de verdade
            float h = Mathf.Max(1, Screen.height);
            _hudFrac = 1f - _view.HudBottomPx / h;
            _barFrac = (sa.yMin + sa.height * MenuBar.Band) / h;
            _trayFrac = (sa.yMin + sa.height * (MenuBar.Band + MenuBar.Tray)) / h;
        }

        /// <summary>
        /// Ortografica em retrato, _visW m na largura (9 m; -cam muda). O alvo sai do Enquadramento (puro, testado no coretests): X segue
        /// o jogador preso ao mapa andavel com margem (antes do Corredor a oficina, depois a rua; AREA2 s1); Y fica o mais alto possivel
        /// (fila e balao na base da faixa da HUD) deixando 1,5 m sob o pe do ferreiro, e com a bandeja de Melhorias aberta desce ate ela
        /// cobrir so a fileira do Deposito. O que nao cabe da fila vira o balao preso abaixo da HUD (WorldView.OnScreen).
        /// </summary>
        void FollowCamera(float dt)
        {
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            _cam.orthographicSize = _visW / (2f * aspect);
            float right = _sim.Bought[(int)Upgrade.SideCorridor] ? Balance.WorldW : Balance.WorkshopW;
            V2 a = Enquadramento.Alvo(_sim.Player.Pos, aspect, _visW, right, _hudFrac, _barFrac, _menu.IsOpen ? _trayFrac : 0f);
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, new Vector3(a.X, a.Y, -10f), 1f - Mathf.Exp(-6f * dt));
        }

        void BuildHud()
        {
            // G-UA-02: gravando, sem EventSystem: o mouse parado sobre um botao mudava a cor dele (hover) entre 2 execucoes
            if (Arg("-record") == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
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
            Image pill = Art.Panel(top, "PilulaOuro", Art.HudEdge, new Vector2(0.06f, 0.1f), new Vector2(0.36f, 0.9f));
            Image pillIn = Art.Panel(pill.transform, "Fundo", Art.ComAlfa(Art.HudFace, 0.95f), Vector2.zero, Vector2.one);
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
            // A-UI-16: borda creme e face marrom como a pilula de ouro (era o azul-marinho Art.Bg)
            Vector2 hintMin = new Vector2(0.385f, 0.1f), hintMax = new Vector2(Arg("-record") == null ? 0.875f : 1f, 0.9f);   // v0.6b: a engrenagem fica a direita (criativo: sem ela)
            Image hintPill = Art.Panel(top, "PilulaDica", Art.HudEdge, hintMin, hintMax);
            Image hintIn = Art.Panel(hintPill.transform, "Fundo", Art.ComAlfa(Art.HudFace, 0.95f), Vector2.zero, Vector2.one);
            hintIn.rectTransform.offsetMin = new Vector2(5f, 5f); hintIn.rectTransform.offsetMax = new Vector2(-5f, -5f);
            hintPill.raycastTarget = hintIn.raycastTarget = false;
            // v0.6d: 1 linha so, ~32 px. A caixa tem 48 px de altura: 2 linhas nao cabem e o best-fit encolhe ate caber numa; os textos
            // (Textos.Dica) tem no maximo 26 caracteres, sem preco
            _hint = Art.Outlined(Art.NewText(hintPill.transform, "Dica", 32, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f)), 2f);
            _hint.rectTransform.offsetMin = new Vector2(18f, -24f); _hint.rectTransform.offsetMax = new Vector2(-18f, 24f);
            _hint.fontStyle = FontStyle.Bold; _hint.color = Art.Accent;
            _hint.resizeTextForBestFit = true; _hint.resizeTextMinSize = 22; _hint.resizeTextMaxSize = 32;
            _hint.verticalOverflow = VerticalWrapMode.Truncate;
            _notice = MakeNotice(top, hintMin, hintMax);
            _crown = Art.Icon("coroa", "icone");
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
            RefreshAdUi();
            float real = Mathf.Min(Captura.Passo, 0.1f);   // tempo real (o simulado zera o timeScale; gravando, o do quadro), sem o salto do 1o quadro
            RefreshOrder(real);
            TickNotice(real);
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
            if (h != _hintH || arg != _hintArg || u != _hintU)
            {
                // s86 tutorial_step: cada dica nova antes da 1a venda (a dica inicial e' o tutorial); a = dica, b = passo
                if (_sim.FirstSaleTime < 0f && (h != _hintH || _tutorial == 0)) Log("tutorial_step", h.ToString(), (++_tutorial).ToString(CultureInfo.InvariantCulture));
                _hintH = h; _hintArg = arg; _hintU = u; _hint.text = Textos.Dica(h, arg, _sim);
            }
            _view.ShowHint(h, arg);   // seta no mundo sobre o alvo (P1-1)
        }

        // ---------- anuncios: Chamar VIP, Velocidade 2x/3x, selo do boost e avisos (v0.5c) ----------

        /// <summary>
        /// Botoes nos cantos da barra de baixo (fora da fila e do joystick; a base sobe 2,6% para a marca "Development Build" do APK de
        /// teste nao cobrir a legenda), selo do boost logo acima do botao de velocidade (no topo ele batia no balao do 1o da fila, que vai
        /// para a esquerda com 8 vagas). Os avisos foram para a faixa da HUD (A-UI-19, MakeNotice).
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
        }

        /// <summary>Botao quadrado: borda de ouro, fundo marrom da HUD, icone, legenda e o selo de video (anuncio) no canto de cima.
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
            play.sprite = Art.Triangle(); play.color = Art.HudFace; play.raycastTarget = false;   // "play" escuro e neutro (sem cor de marca)
            play.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
            a.Video = v.gameObject;
            edge.gameObject.SetActive(false);
            return a;
        }

        /// <summary>
        /// A-UI-19: o aviso ocupa o retangulo da pilula da dica (`min`/`max` em `top`), por cima dela e com face opaca; a dica volta quando
        /// ele some. Borda de ouro (evento) contra a creme da dica, icone a esquerda e 1 linha de texto como a dica. Antes eram 3 avisos
        /// soltos: VIP a 63-70% da altura e encomenda a 55-62% atravessavam a oficina; o do anuncio ficava sobre o Deposito.
        /// </summary>
        Banner MakeNotice(RectTransform top, Vector2 min, Vector2 max)
        {
            var b = new Banner();
            Image edge = Art.Panel(top, "Aviso", Art.Gold, min, max);
            edge.raycastTarget = false;
            Image face = Art.Panel(edge.transform, "Fundo", Art.HudFace, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(5f, 5f); face.rectTransform.offsetMax = new Vector2(-5f, -5f);
            face.raycastTarget = false;
            b.I = Art.Node(edge.transform, "Icone", new Vector2(0.03f, 0.1f), new Vector2(0.19f, 0.9f)).gameObject.AddComponent<Image>();
            b.I.preserveAspect = true; b.I.raycastTarget = false;
            b.T = Art.Outlined(Art.NewText(edge.transform, "Texto", 36, new Vector2(0.2f, 0.5f), new Vector2(0.97f, 0.5f)), 2f);
            b.T.rectTransform.offsetMin = new Vector2(0f, -24f); b.T.rectTransform.offsetMax = new Vector2(0f, 24f);   // 48 px: 1 linha, como a dica
            b.T.fontStyle = FontStyle.Bold; b.T.color = Art.Accent;
            b.T.resizeTextForBestFit = true; b.T.resizeTextMinSize = 22; b.T.resizeTextMaxSize = 36;
            b.T.verticalOverflow = VerticalWrapMode.Truncate;
            b.R = edge.rectTransform;
            b.G = edge.gameObject.AddComponent<CanvasGroup>();
            b.G.blocksRaycasts = false;
            edge.gameObject.SetActive(false);
            return b;
        }

        /// <summary>A-UI-19: entra na fila (cheia, o mais velho cai); sem `icon` o texto usa a largura toda.</summary>
        void Notice(string text, Sprite icon = null, Color tint = default)
        {
            if (_notices.Count >= NoticeMax) _notices.Dequeue();
            _notices.Enqueue((text, icon, tint));
        }

        /// <summary>Um aviso por vez: acabou o da tela, entra o proximo da fila. Pop 0 -> 1,1 -> 1 em 0,3 s, segura e some nos ultimos 0,4 s.</summary>
        void TickNotice(float dt)
        {
            Banner b = _notice;
            if (!b.R.gameObject.activeSelf)
            {
                if (_notices.Count == 0) return;
                (string text, Sprite icon, Color tint) = _notices.Dequeue();
                b.T.text = text; b.Age = 0f;
                b.I.enabled = icon != null; b.I.sprite = icon; b.I.color = tint;
                b.T.rectTransform.anchorMin = new Vector2(icon != null ? 0.2f : 0.03f, 0.5f);
                b.R.gameObject.SetActive(true);
            }
            b.Age += dt;
            if (b.Age >= NoticeTime) { b.R.gameObject.SetActive(false); return; }
            b.R.localScale = Vector3.one * Pop(b.Age);
            b.G.alpha = Mathf.Clamp01((NoticeTime - b.Age) / 0.4f);
        }

        static float Pop(float age)
        {
            float k = Mathf.Clamp01(age / 0.3f);
            return k < 0.6f ? Mathf.Lerp(0.3f, 1.1f, k / 0.6f) : Mathf.Lerp(1.1f, 1f, (k - 0.6f) / 0.4f);
        }

        /// <summary>
        /// Cada quadro (tempo real: o simulado zera o timeScale): VIP com Sim.CanSummonVip e anuncio pronto; na recarga de 5 min (P14, sem VIP
        /// na rua) fica cinza com o tempo, como a velocidade. Velocidade com CanBoost e anuncio pronto mostra o proximo multiplicador, fora disso
        /// (sem boost e com recarga) fica cinza com o tempo; no 3x some (o selo diz tudo). Flag desligada (Tuning) esconde o botao; antes da 1a
        /// melhoria (D6) os dois Can* recusam e nao ha recarga, entao os dois ficam escondidos (e sem rewarded_offer).
        /// Selo do boost com o multiplicador e o cronometro, pulsando.
        /// </summary>
        void RefreshAdUi()
        {
            bool vip = _sim.CanSummonVip && _ads.Ready(Ads.Vip), vipCool = _sim.T.Vip && _sim.T.VipChamado && !_sim.VipActive && _sim.VipCooldown > 0f;
            if (_vipBtn.B.gameObject.activeSelf != (vip || vipCool)) _vipBtn.B.gameObject.SetActive(vip || vipCool);
            if (vip || vipCool) Paint(_vipBtn, vip, Color.white, vip ? "VIP" : Clock(_sim.VipCooldown));
            bool active = _sim.BoostLeft > 0f, can = _sim.CanBoost && _ads.Ready(Ads.Velocidade), cool = _sim.T.Velocidade && !active && _sim.BoostCooldown > 0f;
            // s86 rewarded_offer: uma por aparicao do botao disponivel (cinza da recarga nao e' oferta), contada quando da para tocar
            // (A-PLAT-07: Estados.Anuncia; por baixo de painel ela espera o painel fechar)
            bool on = Estados.Anuncia(EstadoAtual);
            if (!vip) _vipOffer = false;
            else if (on && !_vipOffer) { _vipOffer = true; Log("rewarded_offer", Ads.Vip); }
            if (!can) _speedOffer = false;
            else if (on && !_speedOffer) { _speedOffer = true; Log("rewarded_offer", Ads.Velocidade, active ? "3x" : "2x"); }
            bool show = can || cool;
            if (_speedBtn.B.gameObject.activeSelf != show) _speedBtn.B.gameObject.SetActive(show);
            if (show) Paint(_speedBtn, can, Art.Accent, can ? (active ? "→3×" : "2×") : Clock(_sim.BoostCooldown));   // v0.6d: com boost, o PROXIMO nivel ("3×" lia como o atual)
            if (_boostSeal.gameObject.activeSelf != active) _boostSeal.gameObject.SetActive(active);
            if (active)
            {
                _boostMul.text = _sim.BoostMul.ToString("0") + "×";
                _boostTime.text = Clock(_sim.BoostLeft);
                _boostSeal.localScale = Vector3.one * (1f + 0.04f * Mathf.Abs(Mathf.Sin(Captura.Agora * 3f)));
            }
        }

        /// <summary>Botao de anuncio pronto (face da HUD, icone em `on`, selo de video) ou na recarga (cinza, sem video, legenda = cronometro).</summary>
        static void Paint(AdBtn b, bool can, Color on, string label)
        {
            b.B.interactable = can;
            b.Face.color = can ? AdFace : AdOff;
            b.Icon.color = can ? on : Art.ComAlfa(Art.Ink, 0.45f);
            b.Video.SetActive(can);
            b.Label.text = label;
        }

        // ---------- encomenda (v0.6c, docs/FASE9_ENCOMENDAS.md) ----------

        /// <summary>
        /// Cartao no estilo da pilula de ouro, no canto de baixo a esquerda (espelho do selo do boost, acima do botao VIP): icone do item,
        /// "Venda 5 espadas", barra com "3/5" e o premio com a moeda. Sob a pilula de ouro ele cobriria o balao do 1o da fila (8 vagas) e as
        /// moedas da rua, o mesmo motivo que tirou o selo do boost do topo. Criativo (-record): sem cartao nem aviso, salvo com -recordorder
        /// (G-UA-02, preset order).
        /// v0.6d: 420 x 104 px a 32 px da borda e 24 px acima do botao VIP (era 0,02-0,40 da largura, colado nele); titulo com verbo,
        /// a conta "3/5" em cima da barra de 20 px.
        /// </summary>
        void BuildOrderUi()
        {
            if (Arg("-record") != null && Arg("-recordorder") == null) return;
            Image edge = Art.Panel(_safe, "Encomenda", Art.HudEdge, new Vector2(0f, AdTop), new Vector2(0f, AdTop));
            edge.rectTransform.pivot = Vector2.zero; edge.rectTransform.sizeDelta = new Vector2(420f, 104f);
            edge.rectTransform.anchoredPosition = new Vector2(Edge, 24f);
            edge.raycastTarget = false;
            Image face = Art.Panel(edge.transform, "Fundo", Art.ComAlfa(Art.HudFace, 0.95f), Vector2.zero, Vector2.one);
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
        }

        /// <summary>Cartao pelo estado do Sim: encomenda nova entra pulando como o aviso, cada venda dela da um pulso e, entregue (o Ev.OrderDone
        /// liga _orderOut), cresce e some em 0,35 s.</summary>
        void RefreshOrder(float dt)
        {
            if (_order == null) return;
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

        // A-PLAT-07: o toque so pede anuncio jogando (Estados.Anuncia); o -adtest (dev) chama o IAds direto
        void OnVipAd()
        {
            if (!Estados.Anuncia(EstadoAtual)) return;
            _ads.Show(Ads.Vip, () =>
            {
                if (!_sim.SummonVip()) return;   // mudou entre o toque e o fim do anuncio: nada a dar
                Sfx.Play("upgrade");
                Notice("VIP a caminho!", _crown, Color.white);
            }, AdFail);
        }

        void OnSpeedAd()
        {
            if (!Estados.Anuncia(EstadoAtual)) return;
            _ads.Show(Ads.Velocidade, () =>
            {
                float m = _sim.StartBoost();
                if (m <= 0f) return;
                Sfx.Play("upgrade", 1.2f);
                _view.Float(_sim.Player.Pos, $"Velocidade {m:0}×!", Art.Accent, 48);
                Log("boost_start", m.ToString("0", CultureInfo.InvariantCulture), _sim.T.BoostSeconds.ToString("0", CultureInfo.InvariantCulture));
            }, AdFail);
        }

        void AdFail()
        {
            Sfx.Play("leave", 1.3f);
            Notice("Anúncio indisponível");
        }

        // ---------- configuracoes: som e vibracao (v0.6b, BENCHMARK_VISUAL P2-4; a versao saiu da HUD para ca, P1-1) ----------

        /// <summary>
        /// Engrenagem na faixa de cima (a direita da dica, mesmo estilo da pilula de ouro) abre o cartao creme do menu: Musica, Efeitos
        /// (A-AUD-23: eram uma chave Som so), Vibracao (so no Android; no PC aparece apagada), a versao e FECHAR. Nao e' pausa: como o
        /// painel do cofre, trava o joystick e a oficina segue trabalhando (o ferreiro para). Criativo (-record): sem engrenagem.
        /// </summary>
        void BuildSettings(RectTransform top)
        {
            if (Arg("-record") == null)
            {
                Image gear = Art.Panel(top, "Engrenagem", Art.HudEdge, new Vector2(0.89f, 0.1f), new Vector2(1f, 0.9f));
                Image gearIn = Art.Panel(gear.transform, "Fundo", Art.ComAlfa(Art.HudFace, 0.95f), Vector2.zero, Vector2.one);
                gearIn.rectTransform.offsetMin = new Vector2(5f, 5f); gearIn.rectTransform.offsetMax = new Vector2(-5f, -5f);
                gearIn.raycastTarget = false;
                Image icon = Art.Node(gear.transform, "Icone", new Vector2(0.17f, 0.17f), new Vector2(0.83f, 0.83f)).gameObject.AddComponent<Image>();
                icon.sprite = Art.Gear(); icon.color = Art.HudEdge; icon.preserveAspect = true; icon.raycastTarget = false;
                Button gb = gear.gameObject.AddComponent<Button>();
                gb.targetGraphic = gear;
                gb.onClick.AddListener(() => { Sfx.Play("click"); _cfg.gameObject.SetActive(true); });
            }

            _cfg = Art.Node(_safe, "Configuracoes", Vector2.zero, Vector2.one);
            _cfg.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);   // escurece e segura os toques do resto
            Image edge = Art.Panel(_cfg, "Cartao", MenuBar.Leather, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f));   // 3 chaves: 0,34 -> 0,40 da tela
            Image face = Art.Panel(edge.transform, "Face", MenuBar.Cream, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(6f, 14f); face.rectTransform.offsetMax = new Vector2(-6f, -6f);   // chanfro de couro embaixo
            face.raycastTarget = false;
            Text title = Art.NewText(face.transform, "Titulo", 60, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.97f));
            title.text = "Configurações"; title.fontStyle = FontStyle.Bold; title.color = MenuBar.Brown;
            Chave(face.transform, "Música", 0.67f, () => Ajustes.Musica, v => SetSom("musica", v), true);
            Chave(face.transform, "Efeitos", 0.51f, () => Ajustes.Efeitos, v => SetSom("efeitos", v), true);
            Chave(face.transform, "Vibração", 0.35f, () => Ajustes.Vibra, v => { SetVibra(v); if (v) Ajustes.Confirmar(); }, Ajustes.TemVibra);
            Text ver = Art.NewText(face.transform, "Versao", 32, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.32f));
            ver.text = "Forge Street v" + Application.version; ver.color = Art.ComAlfa(MenuBar.Brown, 0.65f);
            // E-PLAT-01: PRIVACIDADE reabre a tela de idade para rever ou revogar (CANCELAR fecha sem mudar)
            Button priv = Art.NewButton(face.transform, "PRIVACIDADE", 40, MenuBar.Leather, new Vector2(0.05f, 0.05f), new Vector2(0.49f, 0.2f), () =>
            {
                _cfg.gameObject.SetActive(false);
                _priv.Abrir(Ads.Resposta(out Faixa f, out bool a) ? Privacidade.Atual(f, a) : Privacidade.Rodape);
            });
            priv.GetComponentInChildren<Text>().color = MenuBar.Cream;
            Button close = Art.NewButton(face.transform, "FECHAR", 40, MenuBar.Leather, new Vector2(0.51f, 0.05f), new Vector2(0.95f, 0.2f), () => _cfg.gameObject.SetActive(false));
            close.GetComponentInChildren<Text>().color = MenuBar.Cream;
            _cfg.gameObject.SetActive(false);
        }

        // v0.6d (revisao de UX): LIGADO = texto #1F3B12 de 30 px no verde #5BD16B; DESLIGADO = branco no #8C7B6B; o rotulo fica #3B2A1A
        // nos dois (so a Vibracao do PC continua apagada)
        static readonly Color OnFace = Art.Hex(0x5BD16B), OnInk = Art.Hex(0x1F3B12), OffFace = Art.Hex(0x8C7B6B), OffEdge = Art.Hex(0x6B5D50), RowInk = Art.Hex(0x3B2A1A);

        /// <summary>Linha "rotulo + chave" (0,14 do cartao): pilula verde (LIGADO) ou cinza (DESLIGADO) com chanfro e bolinha branca; o texto
        /// diz o estado. O toque faz "click" (so se os Efeitos ficaram ligados).</summary>
        void Chave(Transform parent, string label, float y, Func<bool> get, Action<bool> set, bool enabled)
        {
            Text l = Art.NewText(parent, label, 50, new Vector2(0.07f, y), new Vector2(0.48f, y + 0.14f), TextAnchor.MiddleLeft);
            l.text = label; l.fontStyle = FontStyle.Bold; l.color = RowInk;
            Image edge = Art.Panel(parent, label + " Chave", MenuBar.GreenDark, new Vector2(0.5f, y + 0.01f), new Vector2(0.93f, y + 0.13f));
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
            b.onClick.AddListener(() => { set(!get()); Paint(); Sfx.Play("click"); });
            Paint();
            if (enabled) return;
            // sem vibrador (PC): apagada e sem toque
            b.interactable = false;
            edge.gameObject.AddComponent<CanvasGroup>().alpha = 0.45f;
            l.color = Art.ComAlfa(RowInk, 0.45f);
        }

        void SetSom(bool on) { Ajustes.Som = on; Log("settings", "som", on ? "1" : "0"); }   // -settings on|off: musica e efeitos juntos
        void SetSom(string qual, bool on)
        {
            if (qual == "musica") Ajustes.Musica = on; else Ajustes.Efeitos = on;
            Log("settings", qual, on ? "1" : "0");
        }
        void SetVibra(bool on) { Ajustes.Vibra = on; Log("settings", "vibra", on ? "1" : "0"); }

        /// <summary>E-PLAT-01: resposta da tela de idade -> Ads (grava fora do -testsession e inicia o SDK) e o diario (faixa e
        /// personalizado sim/nao; nada de dado pessoal).</summary>
        void Responder(Faixa f, bool aceitou)
        {
            Ads.Responder(f, aceitou, !_testSession);
            Log("consent_answer", Privacidade.Diario(f), Privacidade.Flags(f, aceitou).Consentimento ? "sim" : "nao");
        }

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

        /// <summary>A-PLAT-02/06: o Salvamento grava arquivo + espelho; versao mais nova ou disco ilegivel deixam a sessao sem gravar.
        /// A-PLAT-07: Boot/Loading nao gravam (Estados.Grava): um foco perdido antes do Start avancava o SavedAt e comia o cofre.</summary>
        void Save()
        {
            if (_headless || _testSession || !Estados.Grava(EstadoAtual)) return;
            _save.Gravar(_sim, Now(), Application.version);
        }

        // Voltar de outro app tambem paga o cofre (antes so a abertura pagava: quem trocava de app perdia o tempo fora).
        // _started: a Unity chama OnApplicationPause(false) logo apos o Awake, e na abertura quem paga e' o Start.
        // ponytail: < 60 s fora nao paga nem mostra painel (perde no maximo 15 s de producao); vira Balance se o playtest pedir.
        // _pausedByAd: o anuncio real tira o app da frente; a volta dele nao e' "tempo fora" (nao mostra "Seu cofre rendeu")
        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _pausedByAd = EstadoAtual == Estado.Ad;   // antes do _paused: Ad vence Paused (o anuncio real tira o app da frente)
                _paused = true;
                Save(); EndSession("pausa");
            }
            else
            {
                _paused = false;
                _fpsN = 0; _fpsT = 0f;   // a janela de fps nao mede o tempo fora
                if (_started && !_pausedByAd && _sim != null && _sim.SavedAt > 0 && Now() - _sim.SavedAt >= 60) Offline();
            }
        }
        void OnApplicationFocus(bool focus) { if (!focus) Save(); }
        void OnApplicationQuit() { Save(); EndSession("saiu"); }

        /// <summary>s86/s87 session_end: a = s em primeiro plano desde o session_start, b = pausa|saiu. Sai a cada ida ao segundo plano
        /// (o sistema pode matar o app la sem aviso), entao vale a ultima da sessao; o ouro pendente (currency_*) sai junto.</summary>
        void EndSession(string why)
        {
            if (!_started) return;
            FlushGold();
            Log("session_end", _sessionT.ToString("0", CultureInfo.InvariantCulture), why);
        }

        /// <summary>Fotos/teste (dev): -buy N compra os produtivos entre os N primeiros upgrades (ordem do tier) e depois os luxos, -gold G, -px x,y.</summary>
        void DevArgs()
        {
            string preset = Arg("-preset");
            if (preset != null)
            {
                if (Captura.Flags(preset) == null) { Falha($"PRESET desconhecido '{preset}' (validos: {Captura.Nomes})"); return; }
                Debug.Log($"PRESET {preset}: {Captura.Flags(preset)}");
            }
            if (int.TryParse(Arg("-buy"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                for (int pass = 0; pass < 2; pass++)   // o Buy recusa luxo antes da producao completa, e ha produtivos (Mineiro, Joalheiro 2) depois dos luxos
                    for (int i = 0; i < Math.Min(n, Upgrades.Count); i++)
                        if (Upgrades.IsLuxury(i) == (pass == 1)) _sim.Buy((Upgrade)i);
            // -buyids 1,5,0@300 (criativos): compra estes ids do enum Upgrade fora da ordem do -buy (ex.: 4 bancadas e 1 fornalha
            // sem fole); "id@t" compra quando o relogio do jogo passa de t s, na ordem dada, cobrando o preco (TimedBuys).
            _buyAt = Captura.Compras(_sim, Arg("-buyids"));
            if (long.TryParse(Arg("-gold"), NumberStyles.Integer, CultureInfo.InvariantCulture, out long g)) _sim.Gold = Math.Max(0, g);   // A-CORE-06: aceita > int (foto da HUD)
            string[] px = (Arg("-px") ?? "").Split(',');
            if (px.Length == 2 && float.TryParse(px[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(px[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) _sim.Player.Pos = new V2(x, y);
            // -warmup S: simula S s antes do 1o quadro (jogador parado, sem bot): a fila e a fome ja estao montadas quando a gravacao comeca
            // v0.5c: -boost N = N anuncios de velocidade (1 = 2x, 2 = 3x) ANTES do warmup (warmup >= 60 = foto da recarga)
            if (int.TryParse(Arg("-boost"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int nb)) for (int i = 0; i < nb; i++) _sim.StartBoost();
            float warm = ArgF("-warmup", 0f);
            for (float t = 0f; t < warm && !_headless; t += MaxStep) { _sim.Tick(MaxStep, 0f, 0f); TimedBuys(); }
            if (_headless) return;   // compra id@t impossivel no warmup (Falha)
            // fotos da v0.5, depois do warmup: -hold 0,3,3,3,0,0 = carga mista do ferreiro (por Item, ate o teto de cada tipo) e
            // -stock 10,10,10 = estoque do balcao (espada, escudo, ferramenta, joia; ate o teto). A fila do warmup compra ja no 1o tick.
            string[] hold = (Arg("-hold") ?? "").Split(','), stock = (Arg("-stock") ?? "").Split(',');
            for (int i = 0; i < Math.Min(hold.Length, 6); i++)
                if (int.TryParse(hold[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h)) _sim.Player.Held[i] = Math.Max(0, Math.Min(h, _sim.Player.Cap));
            for (int i = 0; i < Math.Min(stock.Length, 4); i++)
                if (int.TryParse(stock[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int s)) _sim.Stock[2 + i] = Math.Max(0, Math.Min(s, _sim.CounterCap));
            // -vipnow: chama o VIP depois do warmup (fila cheia = ele espera ao lado); se ainda nao pode, tenta a cada quadro (Update).
            // G-UA-02: nao marca mais a 1a venda falsa (o CanSummonVip pede uma de verdade). Depois de um -warmup sem venda real e' erro
            // (o VIP nao viria no comeco, preset vip); sem warmup ele espera a 1a venda real (FS-C-26: -bot -vipnow)
            if (Arg("-vipnow") != null)
            {
                if (warm > 0f && _sim.FirstSaleTime < 0f) { Falha($"VIPNOW: o warmup de {warm:0} s nao teve venda real"); return; }
                _vipNow = !_sim.SummonVip();
            }
        }

        /// <summary>-buyids id@t: compra quando _sim.Time passa de t e cobra o preco (Captura.Vence). G-UA-02: sem ouro ou sem
        /// pre-requisito naquele instante e' erro e sai 1 (antes o ouro ia a 0 e o pre-requisito era ignorado).</summary>
        void TimedBuys()
        {
            string erro = Captura.Vence(_sim, _buyAt);
            if (erro == null) return;
            _buyAt.Clear();   // um erro so
            Falha("BUYIDS " + erro);
        }

        /// <summary>G-UA-02: captura que mostraria algo impossivel (ou preset errado) loga o erro, para tudo como o autoplay e sai 1.</summary>
        void Falha(string msg)
        {
            Debug.LogError(msg);
            _headless = true;   // Update, Save e Start param
            StopAllCoroutines();
            Application.Quit(1);
        }

        static float ArgF(string name, float def) => float.TryParse(Arg(name), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : def;

        // build de desenvolvimento: Ads.Args tambem le o extra "unity" do intent, entao as flags valem no aparelho
        // (adb shell am start -n br.com.vstack.forgestreet/com.unity3d.player.UnityPlayerGameActivity -e unity "-cam 9"). Release: so linha de comando.
        // G-UA-02: o que a linha de comando nao tem vem do -preset (Captura.Arg)
        static string Arg(string name) => Captura.Arg(Ads.Args(), name);

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
                // v0.6c: espera o aviso da 1a encomenda entregue (ate 180 s) e fotografa -shotdelay s depois (moedas no ar, cartao saindo).
                // A-UI-19: o aviso divide a fila com os outros; espera ele estar na tela
                float until = Time.realtimeSinceStartup + 180f;
                while (_order != null && !(_notice.R.gameObject.activeSelf && _notice.T.text.StartsWith(Delivered, StringComparison.Ordinal))
                    && Time.realtimeSinceStartup < until) yield return null;
                yield return new WaitForSecondsRealtime(delay);
            }
            else yield return new WaitForSecondsRealtime(delay);
            Debug.Log($"SHOT t={_sim.Time:0.0} upgrades={_sim.UpgradesBought} nobres={_sim.JewelQueue.Count}/{_sim.JewelQueueCap} baus={string.Join(",", _sim.Chests.ConvertAll(c => c.State))} som={Ajustes.Som} vibra={Ajustes.Vibra} fornalhas={_view.Fornalhas()}");   // A-ART-06: estado desenhado
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
            yield return null;
            Application.Quit(0);
        }

        /// <summary>
        /// -record pasta [-recordsec S] [-recordfps F]: grava round(S x F) quadros (pasta/fNNNNN.jpg) e sai 0. O relogio do jogo anda
        /// 1/F s por quadro (Time.captureFramerate no Awake), entao maquina lenta nao muda o video; com -speed N cada quadro avanca
        /// N/F s de simulacao (F baixo = time-lapse). Supersample ate ~1920 px de altura: janela 540x960 sai 1080x1920. A janela
        /// precisa estar visivel (-batchmode/-nographics nao tem tela para capturar). ponytail: o unico File/Directory do Game; ferramenta
        /// de dev dos criativos, nao e' servico (A-PLAT-06 so costura save, analytics e anuncio).
        /// </summary>
        IEnumerator Record(string dir)
        {
            Directory.CreateDirectory(dir);
            // G-UA-02 -recordhire (preset first_worker): o 1o quadro e' o da 1a contratacao ao vivo (Ev.Hired); ate t = 600 s de jogo
            if (Arg("-recordhire") != null)
            {
                while (!_hired && _sim.Time < 600f) yield return null;
                if (!_hired) { Falha("RECORD: nenhuma contratacao ate t=600 s (-recordhire)"); yield break; }
            }
            int frames = Mathf.RoundToInt(ArgF("-recordsec", 15f) * Time.captureFramerate);
            int k = Mathf.Max(1, Mathf.RoundToInt(1920f / Screen.height));
            var eof = new WaitForEndOfFrame();
            for (int n = 0; n < frames; n++)
            {
                yield return eof;
                // G-UA-02 -recordfull (preset huge_queue): a fila tem de estar cheia ja no 1o quadro
                if (n == 0 && Arg("-recordfull") != null && _sim.Queue.Count < _sim.QueueCap)
                {
                    Falha($"RECORD: fila {_sim.Queue.Count}/{_sim.QueueCap} no 1o quadro (-recordfull)");
                    yield break;
                }
                Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture(k);
                // ponytail: JPG 95 em vez de PNG; 1080x1920 em PNG passa de 2 MB por quadro e 900 quadros pesam no disco
                File.WriteAllBytes(Path.Combine(dir, $"f{n:00000}.jpg"), tex.EncodeToJPG(95));
                if (n == 0) Debug.Log($"RECORD {tex.width}x{tex.height} x{k} {frames} quadros t={_sim.Time:0.0} -> {dir}");
                Destroy(tex);
            }
            Debug.Log($"RECORD OK t={_sim.Time:0.0} fila={_sim.Queue.Count}/{_sim.QueueCap} upgrades={_sim.UpgradesBought} ouro={_sim.Gold} fornalhas={_view.Fornalhas()}");
            string preset = Arg("-preset");
            if (preset != null) Debug.Log($"PRESET {preset} ok");
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

        /// <summary>Diario (IAnalytics) com t = Sim.Time; no -testsession o Diario nasce desligado.</summary>
        void Log(string evt, string a = "", string b = "") => _diario.Log(evt, _sim != null ? _sim.Time : 0f, a, b);
    }
}
