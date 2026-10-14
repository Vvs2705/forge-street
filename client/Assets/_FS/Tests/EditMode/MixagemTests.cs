using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>
    /// fix/mix-audio: cada regra da Mixagem (orcamento, teto e prioridade, fila dos Importantes, intervalos, distancia e tela, ducking, musica
    /// com pausa) e a MEDIDA sem Unity: o bot joga pelo nucleo, os mesmos gatilhos da View (Mixagem.DoEvento, compasso da StationView,
    /// clac da EsteiraView, compra, worker e Renome do Game) passam pela politica antiga (6 vozes em rodizio, intervalo so por nome) e pela
    /// nova, e a tabela sai no docs/AUDIO.md (rodar com --logger "console;verbosity=detailed" para ver).
    /// </summary>
    public class MixagemTests
    {
        const float Dt = 1f / 30f, Aspecto = 9f / 20f;   // POCO (1080 x 2400), largura padrao da camera (V23)

        static Dictionary<string, float> _dur;

        /// <summary>s de cada clip, como o AudioClip.length do Sfx.</summary>
        static float Dur(string clip)
        {
            if (_dur == null)
            {
                _dur = new Dictionary<string, float>();
                foreach (string n in Sintese.Nomes) _dur[n] = Sintese.Efeito(n, 0).Length / (float)Sintese.Taxa;
            }
            return _dur[clip];
        }

        static Mixagem Longos() => new Mixagem(_ => 10f);   // todo clip dura 10 s: as vozes ficam ocupadas durante o teste

        // ---------------------------------------------------------------- regras

        static readonly V2 Perto = new V2(0f, 0f);
        static float Gap(string som) => Mixagem.Regras[som].Gap;
        static float GapCat(Cat c) => Mixagem.GapCat[(int)c] + 0.001f;   // um pouco mais que o intervalo da categoria

        [Test]
        public void Orcamento_TetoPorCategoriaEUIRoubaMundo()
        {
            var m = Longos();
            float gm = GapCat(Cat.Mundo), ga = GapCat(Cat.Acao), t = 2f * gm;
            Assert.IsTrue(m.Pedir("martelo", 0f, out _, Perto));
            Assert.IsTrue(m.Pedir("peca", gm, out _, Perto));
            Assert.IsFalse(m.Pedir("lingote", t, out _, Perto), "Mundo no teto (2): o 3o espera a vez");
            Assert.IsTrue(m.Pedir("venda", t, out _));
            Assert.IsTrue(m.Pedir("minerio", t + ga, out _));
            Assert.IsFalse(m.Pedir("largar", t + 2f * ga, out _), "Acao no teto (2)");
            Assert.AreEqual(Mixagem.Vozes, m.Soando(t + 3f * ga), "orcamento cheio");
            Assert.IsTrue(m.Pedir("click", t + 3f * ga, out Mixagem.Toque q), "clique sem voz livre rouba a de menor prioridade");
            Assert.AreEqual(0, q.Voz, "rouba a voz 0 (Mundo, a mais antiga)");
            Assert.AreEqual(Cat.UI, m.CatDa(0));
            Assert.AreEqual(1, m.Total.Cortados);
            Assert.IsTrue(m.Pedir("click", t + 3f * ga + Gap("click") + 0.01f, out Mixagem.Toque q2), "UI no teto (1): o clique novo troca o velho");
            Assert.AreEqual(0, q2.Voz);
            Assert.AreEqual(1, m.Total.Reinicios);
            Assert.AreEqual(Mixagem.Vozes, m.Total.Pico, "nunca passa do orcamento");
        }

        [Test]
        public void Importante_RoubaAMenorPrioridadeENuncaECortado()
        {
            var m = Longos();
            float gm = GapCat(Cat.Mundo), ga = GapCat(Cat.Acao);
            m.Pedir("martelo", 0f, out _, Perto);   // voz 0: Mundo
            m.Pedir("venda", 0f, out _);            // voz 1: Acao
            m.Pedir("peca", gm, out _, Perto);      // voz 2: Mundo
            m.Pedir("minerio", gm + ga, out _);     // voz 3: Acao
            Assert.AreEqual(Mixagem.Vozes, m.Soando(gm + ga));
            Assert.IsTrue(m.Pedir("compra", gm + 2f * ga, out Mixagem.Toque q));
            Assert.AreEqual(0, q.Voz, "rouba o Mundo mais antigo, nao a Acao");
            Assert.AreEqual(Cat.Importante, q.Cat);
            for (float t = gm + 0.1f; t < 8f; t += 0.3f)   // nada de menor prioridade tira o Importante da voz
            {
                m.Pedir("venda", t, out _); m.Pedir("martelo", t, out _, Perto); m.Pedir("click", t, out _);
            }
            Assert.AreEqual(Cat.Importante, m.CatDa(0));
            Assert.AreEqual(0, m.Total.CortadosImportante);
        }

        [Test]
        public void Importante_OutroSoandoVaiParaAFila_OMesmoReinicia()
        {
            var m = new Mixagem(c => c == "upgrade" ? 1f : 1.1f);
            Assert.IsTrue(m.Pedir("compra", 0f, out Mixagem.Toque a));
            Assert.IsFalse(m.Pedir("renome", 0.2f, out _), "outro Importante soando: espera");
            Assert.IsFalse(m.Pedir("renome", 0.3f, out _), "o mesmo na fila nao entra 2 vezes");
            Assert.AreEqual(1, m.Total.Fila);
            Assert.IsFalse(m.Fila(0.9f, out _), "a compra ainda soa");
            Assert.IsTrue(m.Fila(1.01f, out Mixagem.Toque r), "acabou: a fanfarra do Renome entra");
            Assert.AreEqual("vip", r.Clip);
            Assert.IsFalse(m.Fila(1.02f, out _), "fila vazia");
            Assert.IsTrue(m.Pedir("renome", 1.5f, out Mixagem.Toque b), "o mesmo nome por cima reinicia");
            Assert.AreEqual(r.Voz, b.Voz);
            Assert.AreEqual(1, m.Total.Reinicios);
            Assert.AreEqual(0, m.Total.Cortados + m.Total.Barrados);
            m.Pedir("compra", 1.6f, out _);
            m.Cala();
            Assert.IsFalse(m.Fila(5f, out _), "efeitos desligados: a fila esvazia");
            Assert.AreEqual(0, m.Soando(1.7f));
        }

        [Test]
        public void Intervalo_PorNomeDeQualquerFonteEPorCategoria()
        {
            var m = Longos();
            float gm = GapCat(Cat.Mundo), gb = Gap("martelo"), t = gm + Math.Max(gm, gb) + 0.01f;
            Assert.IsTrue(m.Pedir("martelo", 0f, out _, Perto, 1f));
            Assert.IsFalse(m.Pedir("martelo", gb - 0.05f, out _, new V2(1f, 0f), 0.8f), "outra bancada dentro do intervalo do martelo: barrada");
            Assert.IsFalse(m.Pedir("peca", gm - 0.05f, out _, Perto), "Mundo colado em outro Mundo: barrado");
            Assert.IsTrue(m.Pedir("peca", gm, out _, Perto));
            m.Cala();
            Assert.IsTrue(m.Pedir("martelo", t, out _, Perto), "passou o intervalo");
            Assert.IsTrue(m.Pedir("venda", 10f, out _));
            Assert.IsFalse(m.Pedir("venda", 10f + Gap("venda") - 0.05f, out _), "vendas dentro do intervalo viram 1 moeda");
            Assert.IsTrue(m.Pedir("venda", 10f + Gap("venda") + 0.01f, out _));
            Assert.AreEqual(3, m.Total.Barrados);
        }

        [Test]
        public void Distancia_EForaDaTela()
        {
            var m = Longos();
            m.Ouvinte = new V2(0f, 0f);
            float vol = Mixagem.Regras["peca"].Vol, d = (Mixagem.Perto + Mixagem.Longe) / 2f;
            Assert.IsTrue(m.Pedir("peca", 0f, out Mixagem.Toque q, new V2(Mixagem.Perto * 0.75f, 0f)));
            Assert.AreEqual(vol, q.Vol, 1e-6f, "ate Perto: volume cheio");
            m.Cala();
            Assert.IsTrue(m.Pedir("peca", 10f, out q, new V2(0f, d)));
            Assert.AreEqual(vol * 0.5f, q.Vol, 1e-5f, "no meio entre Perto e Longe: metade");
            float longe = Mixagem.Longe - 0.5f * Mixagem.Minimo * (Mixagem.Longe - Mixagem.Perto);   // ganho = Minimo / 2
            Assert.IsFalse(m.Pedir("peca", 20f, out _, new V2(longe, 0f)), "ganho abaixo do minimo: nem toca");
            m.Tela(new V2(20f, 0f), 4.5f, 10f);   // camera longe do ferreiro: a bancada ao lado dele esta fora da tela
            Assert.IsFalse(m.Pedir("peca", 30f, out _, new V2(1f, 0f)), "Mundo fora da tela: mudo");
            Assert.IsTrue(m.Pedir("venda", 30f, out q, new V2(30f, 0f)), "Acao nao depende da tela");
            Assert.AreEqual(Mixagem.Regras["venda"].Vol * Mixagem.Piso[(int)Cat.Acao], q.Vol, 1e-6f, "venda longe: o piso da Acao");
            Assert.IsTrue(m.Pedir("compra", 30.1f, out q, new V2(30f, 0f)));
            Assert.AreEqual(Mixagem.Regras["compra"].Vol, q.Vol, 1e-6f, "Importante nao atenua");
            Assert.AreEqual(2, m.Total.Barrados);
        }

        [Test]
        public void Duck_ImportanteAbaixaMusicaAmbienteEMundo()
        {
            var m = new Mixagem(c => c == "upgrade" ? 0.9f : c == "offline" ? 3f : 0.4f);
            Assert.AreEqual(1f, m.Duck(0f), 1e-6f);
            m.Pedir("martelo", 0f, out _, new V2(0f, 0f));
            Assert.AreEqual(1f, m.Duck(0.1f), 1e-6f, "Mundo nao abaixa nada");
            m.Pedir("compra", 1f, out _);
            Assert.AreEqual(Mixagem.DuckGanho, m.Duck(1f), 1e-6f, "-6 dB na hora");
            Assert.AreEqual(Mixagem.DuckGanho, m.Duck(1.85f), 1e-6f, "enquanto soa");
            Assert.AreEqual((1f + Mixagem.DuckGanho) / 2f, m.Duck(1.9f + Mixagem.DuckSolta / 2f), 1e-4f, "volta em DuckSolta s");
            Assert.AreEqual(1f, m.Duck(1.9f + Mixagem.DuckSolta), 1e-6f);
            m.Pedir("cofre", 5f, out _);   // clip de 3 s: o ducking dura no maximo DuckMax
            Assert.AreEqual(Mixagem.DuckGanho, m.Duck(5f + Mixagem.DuckMax - 0.01f), 1e-6f);
            Assert.AreEqual(1f, m.Duck(5f + Mixagem.DuckMax + Mixagem.DuckSolta + 0.01f), 1e-6f);
        }

        [Test]
        public void Eventos_MapaSemAgudoRepetido()
        {
            string Som(Ev k, int a, int b) => Mixagem.DoEvento(new SimEvent(k, a, b, new V2(0f, 0f)), out _);
            Assert.AreEqual("minerio", Som(Ev.Picked, (int)Item.Ore, -1));
            Assert.AreEqual("pegar", Som(Ev.Picked, (int)Item.Ingot, -1));
            Assert.AreEqual("ajudante", Som(Ev.Picked, (int)Item.Ore, 0), "ajudante e' Mundo");
            Assert.AreEqual("largar", Som(Ev.Deposited, (int)Item.Ore, -1));
            Assert.IsNull(Som(Ev.Deposited, (int)Item.Ingot, 9), "esteira: o clac ja fala");
            Assert.AreEqual("sino", Som(Ev.ClientArrived, (int)Item.Sword, 1));
            Assert.IsNull(Som(Ev.ClientArrived, (int)Item.Sword, 2), "sino so com a fila vazia");
            Assert.IsNull(Som(Ev.ClientLeft, (int)Item.Sword, 2), "fila cheia: nem entrou");
            Assert.IsNull(Som(Ev.Bought, 0, 10), "compra toca no Game.Bought");
            Assert.AreEqual("encomenda", Som(Ev.OrderDone, (int)Item.Sword, 50));
            for (int i = 0; i < 12; i++)
            {
                Assert.AreEqual("venda", Mixagem.DoEvento(new SimEvent(Ev.Sold, (int)Item.Sword, 10 + i, new V2(0f, 0f)), out float p));
                Assert.AreEqual(1f, p, 0f, "a moeda nao sobe a cada venda");
            }
            foreach (KeyValuePair<string, Mixagem.Regra> r in Mixagem.Regras)
            {
                Assert.GreaterOrEqual(Array.IndexOf(Sintese.Nomes, r.Value.Clip), 0, r.Key + ": clip existe");
                Assert.That(r.Value.Vol, Is.InRange(0.1f, 0.85f), r.Key + ": volume");
                if (r.Value.Cat == Cat.Mundo || r.Value.Cat == Cat.Acao) Assert.LessOrEqual(r.Value.Pitch, 1.25f, r.Key + ": repetido e agudo");
                if (r.Value.Cat == Cat.Importante) Assert.AreEqual(0f, r.Value.Gap, 0f, r.Key + ": Importante nunca barrado");
            }
            Assert.AreEqual(Cat.Importante, Mixagem.De("order").Cat, "nome de clip de outra raia ainda toca, como Importante");
            Assert.AreEqual(Cat.UI, Mixagem.De("click").Cat);
            Assert.IsNull(Mixagem.De("nada").Clip);
        }

        [Test]
        public void Musica_UmaVoltaEPausaComEntradaESaida()
        {
            float v = Sintese.MusicaS;
            Assert.AreEqual(0f, Mixagem.MusicaGanho(0f, v), 1e-6f);
            Assert.AreEqual(1f, Mixagem.MusicaGanho(Mixagem.MusicaEntra, v), 1e-6f);
            Assert.AreEqual(1f, Mixagem.MusicaGanho(v / 2f, v), 1e-6f);
            Assert.AreEqual(0.5f, Mixagem.MusicaGanho(v - Mixagem.MusicaSai / 2f, v), 1e-4f, "sai suave no fim da volta");
            Assert.AreEqual(0f, Mixagem.MusicaGanho(v + 10f, v), 1e-6f, "pausa");
            Assert.GreaterOrEqual(Mixagem.MusicaPausa, 20f);
        }

        [Test]
        public void SomLog_LinhaPorMinutoZera()
        {
            var m = Longos();
            m.Pedir("compra", 0f, out _);
            m.Pedir("venda", 0.1f, out _);
            m.Pedir("venda", 0.2f, out _);
            Assert.AreEqual("SOM min=1 plays=2 pico=2 cortados=0 barrados=1 fila=0 porCategoria=Importante:1,Acao:1,Mundo:0,UI:0", m.Minuto(1));
            m.Pedir("click", 61f, out _);
            Assert.AreEqual("SOM min=2 plays=1 pico=1 cortados=0 barrados=0 fila=0 porCategoria=Importante:0,Acao:0,Mundo:0,UI:1", m.Minuto(2));
            Assert.AreEqual(3, m.Total.Plays);
        }

        // ---------------------------------------------------------------- medida antes/depois pelo bot

        /// <summary>A politica que o Sfx tinha ate a 0.6 (frozen): 6 vozes em rodizio, cheia rouba a da vez sem olhar prioridade; intervalo so por clip.</summary>
        sealed class Antiga
        {
            const int N = 6;
            readonly float[] _fim = new float[N];
            readonly Cat[] _cat = new Cat[N];
            readonly Dictionary<string, float> _last = new Dictionary<string, float>();
            int _voz;
            public readonly Medida M;
            public Antiga(Medida m) { M = m; }

            public void Play(string clip, float pitch, float gap, Cat cat, float t)
            {
                if (_last.TryGetValue(clip, out float l) && t - l < gap) return;
                _last[clip] = t;
                int v = -1;
                for (int i = 0; i < N && v < 0; i++) if (_fim[(_voz + i) % N] <= t) v = (_voz + i) % N;
                if (v < 0) { v = _voz; M.Cortados++; if (_cat[v] == Cat.Importante) M.CortadosImportante++; }
                _voz = (v + 1) % N;
                _fim[v] = t + Dur(clip) / pitch; _cat[v] = cat;
                M.Plays++; M.PorCat[(int)cat]++;
                M.Pico = Math.Max(M.Pico, Soando(t));
            }

            public int Soando(float t)
            {
                int n = 0;
                foreach (float f in _fim) if (f > t) n++;
                return n;
            }
        }

        /// <summary>Os gatilhos do Game.HandleEvents ate a 0.6 (frozen), com a categoria que a politica nova da a cada um.</summary>
        static void Antes(SimEvent e, Sim sim, float t, Antiga a)
        {
            switch (e.Kind)
            {
                case Ev.Picked: a.Play(e.A == (int)Item.Ore ? "ore" : "drop", e.B < 0 ? 1f : 0.8f, 0.08f, e.B < 0 ? Cat.Acao : Cat.Mundo, t); break;
                case Ev.Deposited: a.Play("drop", 1.1f, 0.08f, e.B < 0 ? Cat.Acao : Cat.Mundo, t); break;
                case Ev.Crafted: a.Play(e.A == (int)Item.Ingot ? "furnace" : "hammer", 1f, 0.12f, Cat.Mundo, t); break;
                case Ev.Sold: a.Play("coin", 1f + 0.05f * (sim.Sales % 5), 0.1f, Cat.Acao, t); break;
                case Ev.Graded: a.Play("coin", e.B == (int)Quality.Superior ? 1.6f : 1.35f, 0.15f, Cat.Acao, t); break;
                case Ev.Masterwork: a.Play("order", 1f, 0.05f, Cat.Importante, t); break;
                case Ev.Blueprint: a.Play("order", 1.2f, 0.05f, Cat.Importante, t); break;
                case Ev.ClientArrived: a.Play("client", 1f, 0.2f, Cat.Mundo, t); break;
                case Ev.ClientLeft: if (e.B == 0) a.Play("leave", 1f, 0.05f, Cat.Acao, t); break;
                case Ev.Hired: if (e.B == 0) a.Play("hire", 1f, 0.05f, Cat.Importante, t); break;
                case Ev.Milestone: a.Play("coin", 1.4f, 0.05f, Cat.Importante, t); break;
                case Ev.ChestOpened: a.Play("coin", 1f + 0.05f * (sim.Sales % 5), 0.1f, Cat.Importante, t); break;
                case Ev.VipArrived: a.Play("vip", 1f, 0.05f, Cat.Importante, t); break;
                case Ev.VipServed: a.Play("offline", 1.15f, 0.05f, Cat.Importante, t); break;
                case Ev.VipLeft: a.Play("leave", 1f, 0.05f, Cat.Importante, t); break;
                case Ev.OrderDone: a.Play("order", 1f, 0.05f, Cat.Importante, t); break;
            }
        }

        sealed class Medida
        {
            public float Min; public int Plays, Pico, Cortados, CortadosImportante, Fila, Amostras, Tres;
            public readonly int[] PorCat = new int[4];
            public string PorSom = "";
            public float PorMin => Plays / Min;
            public float Pct3 => 100f * Tres / Math.Max(1, Amostras);
            public void Amostra(int soando) { Amostras++; if (soando >= 3) Tres++; }
        }

        /// <summary>
        /// O bot (F2P, o humano dos BalanceTests) joga `minutos` a 30 ticks/s; `equipe` = a oficina cheia do -equipe (Captura.Equipe: produção
        /// completa e os 13 workers). Cada gatilho vai para as 2 politicas no mesmo instante; a cada tick conta quantas vozes soam.
        /// </summary>
        static (Medida antes, Medida depois) Roda(float minutos, bool equipe)
        {
            var sim = new Sim(new Tuning());
            if (equipe) Captura.Equipe(sim);
            var bot = new Bot();
            var mix = new Mixagem(Dur);
            var old = new Antiga(new Medida { Min = minutos });
            var d = new Medida { Min = minutos };
            var prog = new float[sim.Stations.Count];
            for (int i = 0; i < prog.Length; i++) prog[i] = sim.Stations[i].Busy ? sim.Stations[i].Progress : 0f;
            float[] est = { -1f, -1f, -1f };   // EsteiraView: lingotes na faixa, fase das divisas, comprimento e passo
            float fase = 0f, len = V2.Dist(sim.FurnaceA.Pos, sim.AnvilA.Pos) - 1.2f, passo = len / Math.Max(1, (int)Math.Round(len / 0.7f));
            var estPos = new V2((sim.FurnaceA.Pos.X + sim.AnvilA.Pos.X) / 2f + 0.45f, (sim.FurnaceA.Pos.Y + sim.AnvilA.Pos.Y) / 2f);
            int renome = sim.Renome.Nivel;
            for (int k = 0, n = (int)(minutos * 60f / Dt); k < n; k++)
            {
                float t0 = sim.Time;
                bot.Step(sim, Dt);
                float t = sim.Time;
                float direita = sim.Bought[(int)Upgrade.SideCorridor] ? Balance.WorldW : Balance.WorkshopW;
                mix.Ouvinte = sim.Player.Pos;
                mix.Tela(Enquadramento.Alvo(sim.Player.Pos, Aspecto, Enquadramento.Largura, direita, 0.1f, 0.12f), Enquadramento.Largura / 2f, Enquadramento.Largura / 2f / Aspecto);
                while (mix.Fila(t, out _)) { }
                foreach (SimEvent e in sim.Events)
                {
                    string som = Mixagem.DoEvento(e, out float p);
                    if (som != null) mix.Pedir(som, t, out _, e.Pos, p);
                    Antes(e, sim, t, old);
                    if (e.Kind == Ev.Bought) { mix.Pedir("compra", t, out _); old.Play("upgrade", 1f, 0.05f, Cat.Importante, t); }   // Game.Bought
                    else if ((e.Kind == Ev.Hired && e.B != 0) || e.Kind == Ev.WorkerUp)   // Game.WorkerComprado
                    {
                        bool novo = sim.Equipe.Nivel(e.A) <= 1;
                        mix.Pedir(novo ? "contrata" : "compra", t, out _);
                        old.Play(novo ? "hire" : "upgrade", 1f, 0.05f, Cat.Importante, t);
                    }
                    if (e.Kind == Ev.Deposited && e.B == 9 && sim.HasConveyor) EsteiraView.Entra(est);
                }
                if (sim.Renome.Nivel > renome) { renome = sim.Renome.Nivel; mix.Pedir("renome", t, out _); old.Play("vip", 0.9f, 0.05f, Cat.Importante, t); }   // Game.SubiuRenome
                for (int i = 0; i < prog.Length; i++)   // StationView.Bate: o compasso das bancadas na tela
                {
                    Station s = sim.Stations[i];
                    float pr = s.Busy ? s.Progress : 0f;
                    if (s.Kind == Kind.Crafter && StationView.Batida(prog[i], pr, StationView.Batidas(s.Time)) && mix.NaTela(s.Pos))
                    {
                        mix.Pedir("martelo", t, out _, s.Pos, StationView.AlturaMartelo(s.OutItem));
                        old.Play("martelo", s.OutItem == Item.Jewel ? 1.5f : StationView.AlturaMartelo(s.OutItem), 0.1f, Cat.Mundo, t);
                    }
                    prog[i] = pr;
                }
                if (sim.HasConveyor)   // EsteiraView.Refresh: um clac por divisa que passa no tambor, so com lingote em cima
                {
                    float dt = t - t0, ciclo = sim.T.ConveyorTime, boost = sim.BoostMul;
                    int lev = EsteiraView.Anda(est, dt * boost / ciclo);
                    fase = EsteiraView.Avanca(fase, EsteiraView.Velocidade(len, ciclo, boost, lev) * dt, passo, out bool clac);
                    if (lev > 0 && clac && mix.NaTela(estPos))
                    {
                        mix.Pedir("esteira", t, out _, estPos, 0.9f + 0.1f * boost);
                        old.Play("esteira", 0.9f + 0.1f * boost, 0.1f, Cat.Mundo, t);
                    }
                }
                d.Amostra(mix.Soando(t));
                old.M.Amostra(old.Soando(t));
            }
            Mixagem.Conta c = mix.Total;
            d.Plays = c.Plays; d.Pico = c.Pico; d.Cortados = c.Cortados; d.CortadosImportante = c.CortadosImportante; d.Fila = c.Fila;
            Array.Copy(c.PorCat, d.PorCat, 4);
            foreach (KeyValuePair<string, int> kv in mix.PorSom) d.PorSom += string.Format(CultureInfo.InvariantCulture, " {0}={1:0.0}", kv.Key, kv.Value / minutos);
            return (old.M, d);
        }

        static string Linha(string nome, Medida a, Medida d)
        {
            string PorCat(Medida m) => string.Format(CultureInfo.InvariantCulture, "{0:0}/{1:0}/{2:0}",
                m.PorCat[(int)FS.Cat.Importante] / m.Min, m.PorCat[(int)FS.Cat.Acao] / m.Min, m.PorCat[(int)FS.Cat.Mundo] / m.Min);
            return string.Format(CultureInfo.InvariantCulture, "| {0} | {1:0} -> {2:0} | {3} -> {4} | {5:0.0}% -> {6:0.0}% | {7} -> {8} | {9} -> {10} ({13} na fila) | {11} -> {12} |",
                nome, a.PorMin, d.PorMin, a.Pico, d.Pico, a.Pct3, d.Pct3, a.Cortados, d.Cortados, a.CortadosImportante, d.CortadosImportante, PorCat(a), PorCat(d), d.Fila);
        }

        /// <summary>
        /// A medida do docs/AUDIO.md (meta do coordenador: ≤ 40–60 sons/min no começo, raramente ≥ 3 ao mesmo tempo, Importante nunca cortado).
        /// Cenarios: bot F2P 5 e 30 min do jogo novo; a oficina cheia (-equipe) 5 min.
        /// </summary>
        [Test]
        public void Medida_AntesDepoisPeloBot()
        {
            var sb = new StringBuilder();
            sb.AppendLine("| cenario | sons/min | pico de vozes | tempo com >= 3 | cortados | Importante cortado | por min Importante/Acao/Mundo |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            var (a5, d5) = Roda(5f, false);
            var (a30, d30) = Roda(30f, false);
            var (ae, de) = Roda(5f, true);
            sb.AppendLine(Linha("bot F2P, 5 min", a5, d5));
            sb.AppendLine(Linha("bot F2P, 30 min", a30, d30));
            sb.AppendLine(Linha("oficina cheia (-equipe), 5 min", ae, de));
            sb.AppendLine("por som/min (nova): F2P 5 min:" + d5.PorSom).AppendLine("F2P 30 min:" + d30.PorSom).AppendLine("cheia:" + de.PorSom);
            TestContext.WriteLine(sb.ToString());

            Assert.LessOrEqual(d5.PorMin, 60f, "comeco: ate 60 sons/min");
            foreach (Medida d in new[] { d5, d30, de })
            {
                Assert.AreEqual(0, d.CortadosImportante, "Importante nunca cortado");
                Assert.LessOrEqual(d.Pico, Mixagem.Vozes);
                Assert.LessOrEqual(d.Pct3, 5f, "raramente 3 ou mais ao mesmo tempo");
            }
            Assert.LessOrEqual(d30.PorMin, 80f, "30 min (mais estacoes, ajudantes e a joalheria): ate 80 sons/min");
            Assert.LessOrEqual(de.PorMin, 110f, "oficina cheia: ate 110 sons/min");
            Assert.Greater(a5.PorMin, 2f * d5.PorMin, "a antiga tocava mais que o dobro (a medida ve a diferenca)");
        }
    }
}
