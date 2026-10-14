using FS.Core;
#if !FS_CORETESTS
using UnityEngine;
#endif

namespace FS
{
    /// <summary>Os 6 estados da fornalha (DM §54, A-ART-06). Cada um tem forma ou movimento proprio, nao so cor (DM §52): le numa foto em cinza.</summary>
    public enum Fornalha { Fria, Aquecendo, Ativa, SemMinerio, SaidaCheia, Overdrive }

    /// <summary>
    /// Fornalha desenhada a partir do Sim (A-ART-06; 1o passo do DM §127, sem refatoracao geral: so a fornalha saiu da WorldView).
    /// Estado() e Calor() sao puros (sem UnityEngine; o coretests compila so eles, com FS_CORETESTS, para o StationViewTests). O
    /// desenho le o que o Sim ja expoe (In, Out, OutCap, Busy, Progress, Crafted e o BoostLeft) mais o calor, que e' da View; serve as 2 fornalhas.
    /// A-ART-07 (DM §55): bigornas e bancadas tambem, so com o compasso do martelo. A peca (Station.Time) e' dividida em Batidas()
    /// iguais perto de BatidaS; em cada uma a arte achata, treme, solta faiscas e toca a martelada baixinho. A ultima e' o Ev.Crafted:
    /// golpe forte, mais faiscas, pop do item na pilha (WorldView) e "+1". Segue o Progress, entao o boost acelera o compasso junto.
    /// </summary>
    public sealed class StationView
    {
        /// <summary>s da rampa de brilho ao voltar a trabalhar (aquecendo) e s para esfriar parada: pausa curta nao refaz a rampa inteira.</summary>
        public const float AqueceS = 1f, EsfriaS = 2f;

        /// <summary>s alvo entre marteladas (o ritmo da v0.6a); a peca se divide no numero inteiro de batidas mais perto disto.</summary>
        public const float BatidaS = 0.45f;

        /// <summary>Batidas por peca de `tempo` s, a ultima no fim (Ev.Crafted): espada 5 s = 11 (0,45 s), com Martelo veloz 3 s = 7 (0,43 s). Minimo 2.</summary>
        public static int Batidas(float tempo) => System.Math.Max(2, (int)System.Math.Round(tempo / BatidaS, System.MidpointRounding.AwayFromZero));

        /// <summary>
        /// O progresso cruzou uma batida k/n (k = 1..n-1) de `antes` para `agora`. A n-esima (progresso 1) e' o Ev.Crafted e nao conta
        /// aqui; peca nova (progresso voltou) e pronta esperando vaga (parado em 1) nao batem. Quadro longo que pula batidas = 1 so.
        /// </summary>
        public static bool Batida(float antes, float agora, int n) => agora > antes && System.Math.Min((int)(agora * n), n - 1) > (int)(antes * n);

        /// <summary>Altura da martelada por bancada (cada familia soa diferente). fix/mix-audio: a joalheria era 1,5 (aguda e repetida); agora 1,25.</summary>
        public static float AlturaMartelo(Item produto) => produto == Item.Shield ? 0.8f : produto == Item.Tool ? 1.15f : produto == Item.Jewel ? 1.25f : 1f;

        /// <summary>
        /// Trabalhando (Busy com Progress &lt; 1): overdrive com o boost, aquecendo ate o calor chegar a 1, senao ativa. Parada: saida cheia
        /// (inclui a pronta esperando vaga, Busy com Progress 1; revisao da v0.6), fria se nunca fez lingote, senao sem minerio. O Sim so
        /// para uma fornalha aberta por falta de minerio ou por saida cheia (Sim.Produce), entao nao ha 7o caso.
        /// </summary>
        public static Fornalha Estado(Station s, bool boost, float calor)
        {
            if (s.Busy && s.Progress < 1f) return boost ? Fornalha.Overdrive : calor < 1f ? Fornalha.Aquecendo : Fornalha.Ativa;
            if (s.Out >= s.OutCap) return Fornalha.SaidaCheia;
            return s.Crafted == 0 ? Fornalha.Fria : Fornalha.SemMinerio;
        }

        /// <summary>Calor 0..1: sobe em AqueceS trabalhando, desce em EsfriaS parada.</summary>
        public static float Calor(float calor, bool trabalhando, float dt)
        {
            float c = calor + (trabalhando ? dt / AqueceS : -dt / EsfriaS);
            return c < 0f ? 0f : c > 1f ? 1f : c;
        }

#if !FS_CORETESTS
        // Pontos medidos no PNG (celula 1,95 m / 256 px, a partir do pivo): chamine e boca acesa (v0.6a), brasa pintada na boca (x -0,31
        // a -0,08, y -0,18 a -0,14; a tampa de cinza a cobre parada) e o vao de cima a direita, fora do corpo (x > 0,35), onde fica a
        // lampada (ART_BIBLE §6: canto superior direito). Lingotes que transbordam: 1 tombando no topo da pilha de saida (x 1,05, 2 fileiras
        // ate y -0,18) e 1 caido ao lado. ponytail: lampada e lingotes estimados sem foto; ajustar pela foto -shot.
        static readonly Vector2 Chimney = new Vector2(-0.21f, 0.8f), Mouth = new Vector2(-0.21f, -0.2f), Ash = new Vector2(-0.19f, -0.17f),
            LampAt = new Vector2(0.52f, 0.62f), SpillTop = new Vector2(1.14f, 0.0f), SpillFloor = new Vector2(1.52f, -0.5f);
        const float LuzM = 4f, LuzA = 0.3f, MouthS = 0.7f, GhostS = 0.42f, SpillTilt = 28f;
        const float SmokeEvery = 0.33f, HotEvery = 0.2f, SmokeLife = 1.1f, HotSparkLife = 0.5f;   // ativa 3 baforadas/s; overdrive 5/s + 3 faiscas cada
        // Overdrive (B-ART-08; na folha de contato da v0.7a era o estado mais fraco: em cinza, so uma boca um pouco maior que a da ativa).
        // Agora muda a FORMA: fogo de 0,85 x 1,1 m saindo da boca e subindo pela frente (nucleo quase branco, o mais claro da fornalha em
        // cinza), tremulando; anel de calor que se abre da boca de 0,45 a 1,9 m 2x/s; 3 faiscas de 0,34 m por baforada (eram 2 de 0,24).
        // No aparelho (~95 px/m) o fogo tem ~100 px de altura e o anel chega a ~180 px; na janela de 540 (47 px/m), metade. Conferido num
        // mock sobre a foto da v0.7a (cor e cinza); o -shot real e' do coordenador.
        const float FlameW = 0.85f, FlameH = 1.1f, CoreW = 0.4f, CoreH = 0.6f, PulseS = 0.5f, PulseFrom = 0.45f, PulseTo = 1.9f, HotSparkS = 0.34f;
        const int HotSparks = 3;
        static readonly Color Smoke = new Color(0.55f, 0.55f, 0.58f, 0.5f), Gray = new Color(0.5f, 0.5f, 0.5f, 1f), Banked = new Color(0.82f, 0.78f, 0.78f, 1f),
            Soot = new Color(0.07f, 0.06f, 0.06f, 0.85f), BulbOff = Art.Hex(0x7A1E26);

        // A-ART-07, bigorna/bancada: topo da bigorna e tampo das bancadas (medidos nos PNG na v0.6a, de onde saltam as faiscas). Batida:
        // achata 6% e treme 2 cm em 0,12 s, 4-6 faiscas, martelada a 45% do volume. Golpe final (Ev.Crafted): 10% e 4,5 cm em 0,2 s,
        // 9-12 faiscas maiores, "+1" sobre a pilha de saida; o som desse e' o do Game (Ev.Crafted). Timbre por bancada: escudo grave, joia fina.
        static readonly Vector2 AnvilTop = new Vector2(-0.18f, 0.45f), BenchTop = new Vector2(0f, 0.3f), OutPile = new Vector2(1.05f, -0.2f);
        const float HitS = 0.12f, HitAmp = 0.06f, HitShake = 0.02f, BlowS = 0.2f, BlowAmp = 0.1f, BlowShake = 0.045f, SparkLife = 0.3f;

        readonly WorldView _w; readonly Station _s; readonly Transform _root, _lamp; readonly SpriteRenderer _base;
        readonly SpriteRenderer _glow, _mouth, _ash, _ghost, _ring, _halo, _bulb, _spillTop, _spillFloor, _flame, _core, _pulse;
        readonly Color _ghostC;
        readonly bool _baked;
        readonly int _front;
        float _calor, _puffT;
        // bigorna/bancada: escala e posicao de repouso da arte, progresso do quadro anterior, relogio do golpe (-1 parado)
        readonly Vector3 _rest, _restPos; readonly Vector2 _top; readonly float _pitch;
        float _prog, _hitT = -1f; bool _blow;
        /// <summary>Estado do ultimo quadro (linha SHOT do -shot).</summary>
        public Fornalha Atual { get; private set; }

        public StationView(WorldView w, Station s, Transform root, SpriteRenderer art, bool baked)
        {
            _w = w; _s = s; _root = root; _base = art; _baked = baked;
            _front = WorldView.Depth(s.Pos.Y);
            if (s.Kind == Kind.Crafter)   // A-ART-07: so o compasso, sobre a arte que ja existe
            {
                _rest = art.transform.localScale; _restPos = art.transform.localPosition;
                _top = s.OutItem == Item.Sword ? AnvilTop : BenchTop;
                _pitch = AlturaMartelo(s.OutItem);
                _prog = s.Busy ? s.Progress : 0f;   // save carregado no meio da peca: nao bate de saida
                return;
            }
            _calor = s.Busy && s.Progress < 1f ? 1f : 0f;   // save carregado trabalhando: sem rampa
            // poca de luz quente no chao (P1-6) e boca acesa por cima da arte (v0.6a)
            _glow = Art.NewSprite(root, "Luz", Art.Glow(), Art.ComAlfa(WorldView.GlowColor, 0f), WorldView.GlowOrder, new Vector2(0f, 0.2f), Vector2.one * LuzM);
            _mouth = Art.NewSprite(root, "Boca", Art.Glow(), Art.ComAlfa(WorldView.SparkB, 0f), _front + 2, Mouth, Vector2.one * MouthS);   // sobre a cinza
            // overdrive: lingua de fogo (laranja + nucleo claro) sobre a boca e o anel de calor (ligados so no Refresh)
            _flame = Art.NewSprite(root, "Chama", Art.Glow(), Art.ComAlfa(WorldView.SparkB, 0.95f), _front + 3, Mouth, new Vector2(FlameW, FlameH));
            _core = Art.NewSprite(root, "ChamaNucleo", Art.Glow(), Color.Lerp(WorldView.SparkA, Color.white, 0.55f), _front + 4, Mouth, new Vector2(CoreW, CoreH));
            _pulse = Art.NewSprite(root, "AnelCalor", Art.Ring(), WorldView.SparkA, _front + 3, Mouth, Vector2.one * PulseFrom);
            _flame.enabled = _core.enabled = _pulse.enabled = false;
            // fria/sem minerio: tampa de cinza sobre a brasa pintada (boca apagada); sem minerio: minerio fantasma boiando num anel tracejado
            _ash = Art.NewSprite(root, "Cinza", Art.Rounded(), Soot, _front + 1, Ash, new Vector2(0.32f, 0.12f));
            _ring = Art.NewSprite(root, "FaltaAnel", Art.DashedRing(), Art.ComAlfa(Art.Ink, 0.8f), _front + 2, Mouth, Vector2.one * 0.64f);
            _ghost = Art.NewSprite(root, "FaltaMinerio", null, Color.white, _front + 3, Mouth, Vector2.one);
            Art.PaintItem(_ghost, s.InItem, false, GhostS);
            _ghostC = _ghost.color;
            // saida cheia: lampada vermelha presa no canto (suporte, halo, aro escuro, bulbo) e 2 lingotes transbordando da pilha
            _lamp = new GameObject("Lampada").transform;
            _lamp.SetParent(root, false);
            _lamp.localPosition = LampAt;
            Art.NewSprite(_lamp, "Suporte", Art.Square(), Art.Bg, _front + 2, new Vector2(-0.13f, 0f), new Vector2(0.18f, 0.05f));
            _halo = Art.NewSprite(_lamp, "Halo", Art.Glow(), Art.ComAlfa(Art.Bad, 0f), _front + 2, Vector2.zero, Vector2.one * 1.05f);
            Art.NewSprite(_lamp, "Aro", Art.Disc(), Art.Bg, _front + 3, Vector2.zero, Vector2.one * 0.36f);
            _bulb = Art.NewSprite(_lamp, "Bulbo", Art.Disc(), Art.Bad, _front + 4, Vector2.zero, Vector2.one * 0.26f);
            _spillTop = Art.NewSprite(root, "Transborda", null, Color.white, 6, SpillTop, Vector2.one, SpillTilt);
            _spillFloor = Art.NewSprite(root, "Transborda", null, Color.white, 6, SpillFloor, Vector2.one, -12f);
            Art.PaintItem(_spillTop, s.OutItem, false, WorldView.PileS);
            Art.PaintItem(_spillFloor, s.OutItem, false, WorldView.PileS);
        }

        /// <summary>
        /// Um quadro. Forma + movimento por estado: fria = boca tampada, nada se mexe; aquecendo = boca acesa crescendo e luz abrindo, sem
        /// fumaca; ativa = boca e luz pulsando + 3 baforadas/s; sem minerio = boca tampada + minerio fantasma boiando no anel girando;
        /// saida cheia = lampada pulsando (1,3 Hz, suave: sem flash) + lingotes transbordando, sem faisca nem fumaca; overdrive = boca e luz
        /// maiores, pulso no dobro, chama alta saindo da boca, anel de calor abrindo 2x/s + 5 baforadas/s com 3 faiscas grandes cada (B-ART-08).
        /// `pulse` = o pulso vermelho da pilha travada (WorldView).
        /// </summary>
        public void Refresh(float dt, bool boost, float pulse)
        {
            if (_s.Kind == Kind.Crafter) { Bate(dt); return; }
            bool work = _s.Busy && _s.Progress < 1f;
            _calor = Calor(_calor, work, Mathf.Min(dt, 0.1f));   // quadro longo (carregamento) nao pula a rampa
            Fornalha e = Atual = Estado(_s, boost, _calor);
            float c = _calor, w7 = Time.time * 7f + _s.Index;
            Color tint = e switch
            {
                Fornalha.Fria => WorldView.Dimmed,
                Fornalha.SemMinerio => Gray,
                Fornalha.Aquecendo => Color.Lerp(WorldView.Dimmed, Color.white, c),
                Fornalha.SaidaCheia => Banked,
                _ => Color.white,
            };
            _base.color = _baked ? tint : Art.StationColor(_s) * tint;

            float ga = 0f, gs = 1f;   // poca de luz: apagada parada, so um resto travada
            if (e == Fornalha.Aquecendo) { ga = LuzA * c; gs = 0.5f + 0.5f * c; }
            else if (e == Fornalha.Ativa) ga = LuzA * (0.8f + 0.2f * Mathf.Sin(w7));
            else if (e == Fornalha.Overdrive) { ga = LuzA * 1.5f * (0.85f + 0.15f * Mathf.Sin(w7 * 2f)); gs = 1.25f; }
            else if (e == Fornalha.SaidaCheia) ga = LuzA * 0.35f;
            _glow.color = Art.ComAlfa(WorldView.GlowColor, ga);
            _glow.transform.localScale = Vector3.one * (LuzM * gs);

            bool over = e == Fornalha.Overdrive;
            _mouth.enabled = work;
            if (work)
            {
                float ma = over ? 0.85f + 0.15f * Mathf.Sin(w7 * 2f) : e == Fornalha.Aquecendo ? 0.2f + 0.4f * c : 0.5f + 0.25f * Mathf.Sin(w7);
                _mouth.color = Art.ComAlfa(over ? Color.Lerp(WorldView.SparkB, WorldView.SparkA, 0.5f) : WorldView.SparkB, ma);
                _mouth.transform.localScale = Vector3.one * (MouthS * (over ? 1.4f : e == Fornalha.Aquecendo ? 0.35f + 0.65f * c : 1f));
            }
            _flame.enabled = _core.enabled = _pulse.enabled = over;
            if (over)
            {
                // chama: tremula em 2 senos fora de fase (estica e afina junto), com a base presa na boca; anel: abre rapido e some
                float t = Time.time + _s.Index * 0.37f, fl = 1f + 0.12f * Mathf.Sin(t * 13f) + 0.06f * Mathf.Sin(t * 31f);
                _flame.transform.localScale = new Vector3(FlameW * (2f - fl), FlameH * fl, 1f);
                _flame.transform.localPosition = new Vector3(Mouth.x, Mouth.y + 0.3f * FlameH * fl, 0f);
                _core.transform.localScale = new Vector3(CoreW * (2f - fl), CoreH * fl, 1f);
                _core.transform.localPosition = new Vector3(Mouth.x, Mouth.y + 0.25f * CoreH * fl, 0f);
                float k = Mathf.Repeat(t, PulseS) / PulseS;
                _pulse.transform.localScale = Vector3.one * Mathf.Lerp(PulseFrom, PulseTo, 1f - (1f - k) * (1f - k));
                _pulse.color = Art.ComAlfa(WorldView.SparkA, 0.9f * (1f - k));
            }
            // tampa de cinza: inteira parada sem brasa, some com o calor no aquecendo (a brasa pintada "acende")
            float ash = e == Fornalha.Fria || e == Fornalha.SemMinerio ? 1f : e == Fornalha.Aquecendo ? 1f - c : 0f;
            _ash.enabled = ash > 0.01f;
            if (_ash.enabled) _ash.color = Art.ComAlfa(Soot, Soot.a * ash);

            bool fome = e == Fornalha.SemMinerio;
            _ghost.enabled = _ring.enabled = fome;
            if (fome)
            {
                float b = Mathf.Sin(Time.time * 3f + _s.Index);
                _ghost.transform.localPosition = new Vector3(Mouth.x, Mouth.y + 0.06f * b, 0f);
                _ghost.color = Art.ComAlfa(_ghostC, 0.45f + 0.2f * b);
                _ring.transform.localRotation = Quaternion.Euler(0f, 0f, -45f * Time.time);
            }

            bool cheia = e == Fornalha.SaidaCheia;
            if (_lamp.gameObject.activeSelf != cheia) _lamp.gameObject.SetActive(cheia);
            _spillTop.enabled = _spillFloor.enabled = cheia;
            if (cheia)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
                _lamp.localScale = Vector3.one * (1f + 0.12f * k);
                _halo.color = Art.ComAlfa(Art.Bad, 0.2f + 0.5f * k);
                _bulb.color = Color.Lerp(BulbOff, Art.Bad, k);
                _spillTop.color = _spillFloor.color = Color.Lerp(Art.ItemTint(_s.OutItem), Art.Bad, 0.5f * pulse);   // mesmo pulso da pilha
                _spillTop.transform.localRotation = Quaternion.Euler(0f, 0f, SpillTilt + 6f * Mathf.Sin(Time.time * 5f));   // balanca na beira
            }

            // fumaca so ativa e overdrive (aquecendo ainda nao fumega; travada e parada nunca); a 1a baforada sai assim que chega la
            if (e != Fornalha.Ativa && e != Fornalha.Overdrive) { _puffT = 0f; return; }
            if ((_puffT -= dt) > 0f) return;
            _puffT = over ? HotEvery : SmokeEvery;
            if (!_w.Ambiente(_root, over ? 1 + HotSparks : 1)) return;
            int order = _front + 2;   // na frente da arte; quem passa na frente da estacao cobre
            Vector3 o = _root.localPosition + (Vector3)Chimney;
            float dx = Random.Range(-0.08f, 0.12f);
            _w.Spawn(Art.Disc(), Smoke, o, o + new Vector3(dx, 0.5f, 0f), o + new Vector3(dx * 2.5f + 0.1f, 1f, 0f), SmokeLife, 0.2f, 0.6f, true, 0f, null, order);
            if (!over) return;
            Vector3 m = _root.localPosition + (Vector3)Mouth;
            for (int i = 0; i < HotSparks; i++)   // faiscas extras: saltam da boca em arco e caem na frente dela
            {
                float a = Random.Range(0.15f, 0.85f) * Mathf.PI;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(0.55f, 0.95f);
                _w.Spawn(Art.Sparkle(), WorldView.SparkA, m, m + d * 0.8f + new Vector3(0f, 0.1f, 0f), m + new Vector3(d.x * 1.2f, d.y * 0.5f - 0.3f, 0f),
                    HotSparkLife, HotSparkS, 0.08f, true, 360f, WorldView.SparkB, order + 3);   // por cima da chama
            }
        }

        /// <summary>Bigorna/bancada, um quadro: bate quando o Progress cruza uma batida e anima o golpe em curso (achata + tremidinha que morre).</summary>
        void Bate(float dt)
        {
            float p = _s.Busy ? _s.Progress : 0f;
            if (Batida(_prog, p, Batidas(_s.Time))) Golpe(false);
            _prog = p;
            if (_hitT < 0f) return;
            _hitT += dt;
            float k = Mathf.Clamp01(_hitT / (_blow ? BlowS : HitS));
            float q = (_blow ? BlowAmp : HitAmp) * Mathf.Sin(Mathf.PI * k);
            float sh = (_blow ? BlowShake : HitShake) * (1f - k) * Mathf.Sin(k * Mathf.PI * 6f);
            _base.transform.localScale = new Vector3(_rest.x * (1f + q), _rest.y * (1f - q), _rest.z);
            _base.transform.localPosition = _restPos + new Vector3(sh, 0f, 0f);
            if (k < 1f) return;
            _hitT = -1f;
            _base.transform.localScale = _rest; _base.transform.localPosition = _restPos;
        }

        /// <summary>Ev.Crafted desta bancada (WorldView.Crafted): golpe final, faiscas maiores e "+1"; a pop do item e o som sao da WorldView e do Game.</summary>
        public void Feito() => Golpe(true);

        void Golpe(bool blow)
        {
            _hitT = 0f; _blow = blow;
            int n = blow ? Random.Range(9, 13) : Random.Range(4, 7);
            if (!_w.NaTela(_root)) return;   // fora da camera: so a escala (barata), sem som, faisca nem "+1"
            if (blow) _w.Float(new V2(_s.Pos.X + OutPile.x, _s.Pos.Y + OutPile.y), "+1", Art.Ink, 32);
            else Sfx.Play("martelo", _pitch, _s.Pos);   // fix/mix-audio: intervalo, distancia e volume na Mixagem
            if (!_w.Ambiente(_root, n)) return;
            int order = _front + 2;   // na frente da arte; quem passa na frente da estacao cobre
            Vector3 o = _root.localPosition + (Vector3)_top;
            float s0 = blow ? 0.34f : 0.26f, r = blow ? 0.95f : 0.65f;   // 0,26 m (~25 px no aparelho) e' o menor que a foto da v0.6a leu
            for (int i = 0; i < n; i++)   // leque para cima, cada uma em arco que cai
            {
                float a = Random.Range(0.12f, 0.88f) * Mathf.PI;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(0.55f, 1f) * r;
                _w.Spawn(Art.Sparkle(), WorldView.SparkA, o, o + d * 0.8f + new Vector3(0f, 0.1f, 0f), o + new Vector3(d.x * 1.2f, d.y * 0.5f - 0.25f, 0f),
                    SparkLife, s0, 0.06f, true, 360f, WorldView.SparkB, order);
            }
        }
#endif
    }
}
