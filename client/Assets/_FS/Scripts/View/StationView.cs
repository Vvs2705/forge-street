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
    /// </summary>
    public sealed class StationView
    {
        /// <summary>s da rampa de brilho ao voltar a trabalhar (aquecendo) e s para esfriar parada: pausa curta nao refaz a rampa inteira.</summary>
        public const float AqueceS = 1f, EsfriaS = 2f;

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
        const float SmokeEvery = 0.33f, HotEvery = 0.2f, SmokeLife = 1.1f, HotSparkLife = 0.4f;   // ativa 3 baforadas/s; overdrive 5/s + 2 faiscas cada
        static readonly Color Smoke = new Color(0.55f, 0.55f, 0.58f, 0.5f), Gray = new Color(0.5f, 0.5f, 0.5f, 1f), Banked = new Color(0.82f, 0.78f, 0.78f, 1f),
            Soot = new Color(0.07f, 0.06f, 0.06f, 0.85f), BulbOff = Art.Hex(0x7A1E26);

        readonly WorldView _w; readonly Station _s; readonly Transform _root, _lamp; readonly SpriteRenderer _base;
        readonly SpriteRenderer _glow, _mouth, _ash, _ghost, _ring, _halo, _bulb, _spillTop, _spillFloor;
        readonly Color _ghostC;
        readonly bool _baked;
        readonly int _front;
        float _calor, _puffT;
        /// <summary>Estado do ultimo quadro (linha SHOT do -shot).</summary>
        public Fornalha Atual { get; private set; }

        public StationView(WorldView w, Station s, Transform root, SpriteRenderer art, bool baked)
        {
            _w = w; _s = s; _root = root; _base = art; _baked = baked;
            _front = WorldView.Depth(s.Pos.Y);
            _calor = s.Busy && s.Progress < 1f ? 1f : 0f;   // save carregado trabalhando: sem rampa
            // poca de luz quente no chao (P1-6) e boca acesa por cima da arte (v0.6a)
            _glow = Art.NewSprite(root, "Luz", Art.Glow(), Art.ComAlfa(WorldView.GlowColor, 0f), WorldView.GlowOrder, new Vector2(0f, 0.2f), Vector2.one * LuzM);
            _mouth = Art.NewSprite(root, "Boca", Art.Glow(), Art.ComAlfa(WorldView.SparkB, 0f), _front + 2, Mouth, Vector2.one * MouthS);   // sobre a cinza
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
        /// maiores, pulso no dobro + 5 baforadas/s com faiscas saindo da boca. `pulse` = o pulso vermelho da pilha travada (WorldView).
        /// </summary>
        public void Refresh(float dt, bool boost, float pulse)
        {
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

            _mouth.enabled = work;
            if (work)
            {
                bool hot = e == Fornalha.Overdrive;
                float ma = hot ? 0.85f + 0.15f * Mathf.Sin(w7 * 2f) : e == Fornalha.Aquecendo ? 0.2f + 0.4f * c : 0.5f + 0.25f * Mathf.Sin(w7);
                _mouth.color = Art.ComAlfa(hot ? Color.Lerp(WorldView.SparkB, WorldView.SparkA, 0.5f) : WorldView.SparkB, ma);
                _mouth.transform.localScale = Vector3.one * (MouthS * (hot ? 1.4f : e == Fornalha.Aquecendo ? 0.35f + 0.65f * c : 1f));
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
            bool over = e == Fornalha.Overdrive;
            _puffT = over ? HotEvery : SmokeEvery;
            if (!_w.Ambiente(_root, over ? 3 : 1)) return;
            int order = _front + 2;   // na frente da arte; quem passa na frente da estacao cobre
            Vector3 o = _root.localPosition + (Vector3)Chimney;
            float dx = Random.Range(-0.08f, 0.12f);
            _w.Spawn(Art.Disc(), Smoke, o, o + new Vector3(dx, 0.5f, 0f), o + new Vector3(dx * 2.5f + 0.1f, 1f, 0f), SmokeLife, 0.2f, 0.6f, true, 0f, null, order);
            if (!over) return;
            Vector3 m = _root.localPosition + (Vector3)Mouth;
            for (int i = 0; i < 2; i++)   // faiscas extras: saltam da boca em arco e caem na frente dela
            {
                float a = Random.Range(0.15f, 0.85f) * Mathf.PI;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(0.4f, 0.7f);
                _w.Spawn(Art.Sparkle(), WorldView.SparkA, m, m + d * 0.8f + new Vector3(0f, 0.1f, 0f), m + new Vector3(d.x * 1.2f, d.y * 0.5f - 0.3f, 0f),
                    HotSparkLife, 0.24f, 0.06f, true, 360f, WorldView.SparkB, order + 1);
            }
        }
#endif
    }
}
