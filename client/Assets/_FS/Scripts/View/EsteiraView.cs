using FS.Core;
#if !FS_CORETESTS
using UnityEngine;
#endif

namespace FS
{
    /// <summary>
    /// B-ART-08 (DM §56: "a esteira e' um grande antes/depois: manual -> sistema vivo"). O Sim move 1 lingote da Fornalha para a Bigorna
    /// por ConveyorTime (vezes o boost) num relogio que a View nao le; o que ela ve e' a entrega (Ev.Deposited com B = 9). A partir dela o
    /// lingote ATRAVESSA a faixa em 1 ciclo: sai da pilha da Fornalha na entrega e so entra na pilha da Bigorna quando chega (a WorldView
    /// tira os Levando da pilha de entrada). A faixa anda so com lingote em cima, na velocidade dele (len / ciclo x boost: ele fica parado em
    /// relacao a ela); divisas amarelas dao o sentido ate numa foto em cinza, cada divisa que passa pelo tambor e' um "clac" baixo, o lingote
    /// quente solta brasa e o tambor da chegada, poeira. Sem lingote em cima tudo para (velocidade 0, sem som nem particula).
    /// Parte pura (Velocidade, Entra, Anda, Avanca) no coretests (EsteiraViewTests); o desenho atras de #if !FS_CORETESTS, como a StationView.
    /// </summary>
    public sealed class EsteiraView
    {
        /// <summary>Lingotes em cima ao mesmo tempo: o Sim entrega 1 por ciclo e cada um cruza em 1 ciclo, entao 2 bastam; 3 = folga.</summary>
        public const int Max = 3;

        /// <summary>m/s da faixa: o lingote cruza `len` m em `ciclo` s (Tuning.ConveyorTime) dividido pelo `boost`; parada (0) sem lingote em cima.</summary>
        public static float Velocidade(float len, float ciclo, float boost, int levando) => levando > 0 && ciclo > 0f ? len * boost / ciclo : 0f;

        /// <summary>Entrega do Sim: um lingote no comeco da faixa (`p` = progresso 0..1 de cada vaga, &lt; 0 livre). Sem vaga, o mais adiantado chega ja.</summary>
        public static void Entra(float[] p)
        {
            int k = 0;
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i] < 0f) { k = i; break; }
                if (p[i] > p[k]) k = i;
            }
            p[k] = 0f;
        }

        /// <summary>Leva cada lingote `d` (fracao da faixa); quem passa de 1 chegou na Bigorna e libera a vaga. Devolve quantos seguem em cima.</summary>
        public static int Anda(float[] p, float d)
        {
            int n = 0;
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i] < 0f) continue;
                p[i] += d;
                if (p[i] >= 1f) p[i] = -1f; else n++;
            }
            return n;
        }

        /// <summary>Fase da faixa (0..passo m) depois de andar `d` m; `clac` = uma divisa passou pelo tambor (o som segue a distancia, nao o relogio).</summary>
        public static float Avanca(float fase, float d, float passo, out bool clac)
        {
            float f = fase + d;
            clac = f >= passo;
            return f % passo;
        }

#if !FS_CORETESTS
        // Mesma faixa de antes (0,45 m a direita da linha Fornalha -> Bigorna, 1,2 m mais curta que a distancia: 2,8 m), agora borracha
        // escura entre 2 trilhos, tambor nas pontas (a divisa some embaixo dele) e divisas a cada ~0,7 m: a 3x e 30 fps a faixa anda 0,28 m
        // por quadro, menos da metade do passo, entao nao parece andar para tras. Lingote de 0,3 m (~28 px no aparelho), como a pilha.
        // Cores da grelha das bocas (ferro escuro e barra) e divisas no amarelo da HUD.
        const float Largura = 0.34f, PassoAlvo = 0.7f, LingoteS = 0.3f, BrasaCada = 0.22f, PoCada = 0.45f, ClacVol = 0.2f;
        static readonly Color Faixa = WorldView.GrateC, Trilho = WorldView.GrateBar, Divisa = Art.ComAlfa(Art.Accent, 0.6f);

        readonly WorldView _w; readonly Sim _sim; readonly Transform _root;
        readonly SpriteRenderer[] _divisas, _lingotes, _calor;
        readonly float[] _p = new float[Max];
        readonly float _len, _passo;
        float _fase, _simT, _brasaT, _poT;
        /// <summary>Lingotes em cima da faixa agora: a WorldView os tira da pilha de entrada da Bigorna ate chegarem.</summary>
        public int Levando { get; private set; }

        public EsteiraView(WorldView w, Sim sim, Transform parent)
        {
            _w = w; _sim = sim; _simT = sim.Time;
            Vector3 a = WorldView.W(sim.FurnaceA.Pos), b = WorldView.W(sim.AnvilA.Pos);   // ponytail: Fornalha embaixo e Bigorna em cima na mesma coluna (Sim), faixa vertical
            _len = Vector3.Distance(a, b) - 1.2f;
            int n = Mathf.Max(1, Mathf.RoundToInt(_len / PassoAlvo));
            _passo = _len / n;
            _root = new GameObject("Esteira").transform;
            _root.SetParent(parent, false);
            _root.localPosition = (a + b) * 0.5f + new Vector3(0.45f, 0f, 0f);
            // no chao (abaixo de quem anda, ordenado pelo pe): faixa 1, trilhos e divisas 2, tambores e calor 3, lingote 4, brasa e poeira 5
            Art.NewSprite(_root, "Faixa", Art.Square(), Faixa, 1, Vector2.zero, new Vector2(Largura, _len));
            for (int s = -1; s <= 1; s += 2) Art.NewSprite(_root, "Trilho", Art.Square(), Trilho, 2, new Vector2(s * (Largura / 2f + 0.02f), 0f), new Vector2(0.05f, _len));
            _divisas = new SpriteRenderer[n];
            for (int i = 0; i < n; i++) _divisas[i] = Art.NewSprite(_root, "Divisa", Art.Triangle(), Divisa, 2, Vector2.zero, new Vector2(0.22f, 0.16f));   // ponta para a Bigorna
            for (int s = -1; s <= 1; s += 2) Art.NewSprite(_root, "Tambor", Art.Rounded(), Trilho, 3, new Vector2(0f, s * _len / 2f), new Vector2(Largura + 0.12f, 0.16f));
            _lingotes = new SpriteRenderer[Max]; _calor = new SpriteRenderer[Max];
            for (int i = 0; i < Max; i++)
            {
                _p[i] = -1f;
                _calor[i] = Art.NewSprite(_root, "Calor", Art.Glow(), Color.clear, 3, Vector2.zero, Vector2.one * 0.6f);
                _lingotes[i] = Art.NewSprite(_root, "Lingote", null, Color.white, 4, Vector2.zero, Vector2.one);
                Art.PaintItem(_lingotes[i], Item.Ingot, false, LingoteS);
                _lingotes[i].enabled = _calor[i].enabled = false;
            }
            Pos(0f);
            _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Um quadro, ANTES das estacoes (a pilha da Bigorna desconta o Levando deste quadro). Relogio = Sim.Time: segue o -speed, para no
        /// anuncio e o boost entra pelo BoostMul. ponytail: as entregas vem do Sim.Events, que guarda so o ultimo tick; com mais de 1 tick por
        /// quadro (-speed alto ou &lt; 30 fps) uma entrega pode passar sem lingote na faixa (aparece direto na pilha, como antes); a 60 e a 30
        /// fps do -record e' 1 tick por quadro.
        /// </summary>
        public void Refresh()
        {
            bool on = _sim.HasConveyor, tick = _sim.Time != _simT;
            float dt = Mathf.Clamp(_sim.Time - _simT, 0f, 0.5f);
            _simT = _sim.Time;
            if (_root.gameObject.activeSelf != on) _root.gameObject.SetActive(on);
            if (!on) return;
            if (tick)   // quadro sem tick (anuncio) ainda ve os eventos do ultimo: nao conta 2 vezes
                foreach (SimEvent e in _sim.Events) if (e.Kind == Ev.Deposited && e.B == 9) Entra(_p);
            float ciclo = _sim.T.ConveyorTime, boost = _sim.BoostMul;
            Levando = Anda(_p, dt * boost / ciclo);
            _fase = Avanca(_fase, Velocidade(_len, ciclo, boost, Levando) * dt, _passo, out bool clac);
            Pos(_fase);
            for (int i = 0; i < Max; i++)
            {
                bool vai = _p[i] >= 0f;
                _lingotes[i].enabled = _calor[i].enabled = vai;
                if (!vai) continue;
                var at = new Vector3(0f, (_p[i] - 0.5f) * _len, 0f);
                _lingotes[i].transform.localPosition = _calor[i].transform.localPosition = at;
                _calor[i].color = Art.ComAlfa(WorldView.SparkB, 0.45f * (1f - _p[i]));   // quente na saida da fornalha, esfria ate a bigorna
            }
            if (Levando == 0) { _brasaT = _poT = 0f; return; }
            if (clac && _w.NaTela(_root)) Sfx.Play("esteira", 0.9f + 0.1f * boost, 0.1f, ClacVol);   // 4 por travessia: ~4/s a 1x, ate 10/s no 3x
            Vector3 o = _root.localPosition;
            if ((_brasaT -= dt) <= 0f)
            {
                _brasaT = BrasaCada;
                int q = -1;   // brasa do lingote mais quente (o que acabou de sair)
                for (int i = 0; i < Max; i++) if (_p[i] >= 0f && (q < 0 || _p[i] < _p[q])) q = i;
                if (_p[q] < 0.7f && _w.Ambiente(_root, 1))
                {
                    Vector3 c = o + new Vector3(Random.Range(-0.08f, 0.08f), (_p[q] - 0.5f) * _len, 0f);
                    float dx = Random.Range(-0.15f, 0.15f);
                    _w.Spawn(Art.Sparkle(), WorldView.SparkA, c, c + new Vector3(dx, 0.3f, 0f), c + new Vector3(dx * 2f, 0.5f, 0f), 0.4f, 0.14f, 0.04f, true, 240f, WorldView.SparkB, 5);
                }
            }
            if ((_poT -= dt) <= 0f)
            {
                _poT = PoCada;
                if (!_w.Ambiente(_root, 1)) return;
                Vector3 c = o + new Vector3(0f, _len / 2f, 0f);   // tambor da chegada
                float s = Random.value < 0.5f ? -1f : 1f;
                _w.Spawn(Art.Disc(), WorldView.Dust, c, c + new Vector3(s * 0.2f, 0.06f, 0f), c + new Vector3(s * 0.35f, 0.1f, 0f), 0.5f, 0.1f, 0.3f, true, 0f, null, 5);
            }
        }

        /// <summary>Divisas na fase `f`: a i-esima em i x passo + f a partir da ponta da Fornalha (a que passa do fim volta embaixo do tambor).</summary>
        void Pos(float f)
        {
            for (int i = 0; i < _divisas.Length; i++) _divisas[i].transform.localPosition = new Vector3(0f, -_len / 2f + i * _passo + f, 0f);
        }
#endif
    }
}
