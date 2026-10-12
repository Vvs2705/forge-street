using System;
#if !FS_CORETESTS
using System.Collections.Generic;
using System.Threading.Tasks;
using FS.Core;
using UnityEngine;
#endif

namespace FS
{
    /// <summary>
    /// A-AUD-23 sem banco de sons (download e credito de IA pedem aval, P11): todo o audio e' sintetizado aqui, em C# puro (o coretests
    /// compila esta parte com FS_CORETESTS, SomTests), uma vez na abertura. Nada de tom puro: modos amortecidos inarmonicos (metal, sino,
    /// moeda), cordas dedilhadas (6 harmonicos), ruido filtrado com envelope (estalo, sopro, fogo, pedra) e fonte vozeada por formantes
    /// (murmurio). Efeitos e ambiente a 22 050 Hz, musica a 16 000 Hz; tudo mono e normalizado pelo pico (o nivel relativo e' o `pico`
    /// de cada som, o volume do grupo fica no Sfx).
    /// </summary>
    public static class Sintese
    {
        public const int Taxa = 22050, TaxaMusica = 16000;

        /// <summary>Nomes que o jogo toca (Game, StationView, Art). "martelo" = batida do compasso; "hammer" = golpe final da peca.</summary>
        public static readonly string[] Nomes =
            { "ore", "furnace", "hammer", "martelo", "coin", "upgrade", "hire", "client", "leave", "drop", "offline", "vip", "order", "click" };

        /// <summary>Variacoes por nome (o Sfx alterna): o que repete muito nao soa igual duas vezes seguidas.</summary>
        public static int Variacoes(string nome) => nome switch { "hammer" => 3, "martelo" => 3, "coin" => 3, "ore" => 2, "drop" => 2, _ => 1 };

        // ---------- efeitos ----------

        public static float[] Efeito(string nome, int v)
        {
            var g = new Rng(Semente(nome) + v * 7919);
            float[] b; float pico;
            switch (nome)
            {
                case "hammer":   // golpe final: estalo do aco + corpo medio (o alto-falante do celular nao tem grave) + bigorna cantando
                {
                    b = Buf(0.75f); float d = 1f + 0.025f * (v - 1);
                    Ruido(b, Taxa, g, 0f, 0.01f, 0.9f, 0f, 0.0018f, 11000f, 1800f);
                    Varre(b, Taxa, 0f, 230f * d, 150f * d, 0.9f, 0.06f, 0.25f);
                    Modo(b, Taxa, 0f, 430f * d, 0.45f, 0.035f);
                    Bigorna(b, Taxa, g, 0f, 1150f * d, 0.5f, 1f);
                    pico = 0.95f; break;
                }
                case "martelo":   // batida do compasso (tocada a 25%, altura por bancada): mais seca, o anel morre na metade
                {
                    b = Buf(0.4f); float d = 1f + 0.03f * (v - 1);
                    Ruido(b, Taxa, g, 0f, 0.008f, 0.7f, 0f, 0.0015f, 11000f, 2000f);
                    Varre(b, Taxa, 0f, 260f * d, 180f * d, 0.6f, 0.035f, 0.15f);
                    Bigorna(b, Taxa, g, 0f, 1150f * d, 0.45f, 0.55f);
                    pico = 0.9f; break;
                }
                case "furnace":   // lingote pronto: sopro do fole (ruido num passa-banda que abre e fecha) + ronco + 4 estalos
                {
                    b = Buf(0.7f);
                    float lo = 0f, bp = 0f, rl = 0f, ar = Polo(160f, Taxa);
                    for (int k = 0; k < b.Length; k++)
                    {
                        float t = k / (float)Taxa, x = g.B();
                        float fc = 250f + 650f * (float)Math.Sin(Math.PI * Math.Min(1f, t / 0.6f));
                        float f = 2f * (float)Math.Sin(Math.PI * fc / Taxa), hp = x - lo - 0.8f * bp;
                        bp += f * hp; lo += f * bp; rl += ar * (x - rl);
                        float env = Math.Min(1f, t / 0.12f) * (float)Math.Exp(-Math.Max(0f, t - 0.12f) / 0.2f);
                        b[k] = env * (0.8f * bp + 0.3f * lo + 1.5f * rl);
                    }
                    for (int i = 0; i < 4; i++) Ruido(b, Taxa, g, g.R(0.15f, 0.55f), 0.005f, g.R(0.2f, 0.6f), 0f, 0.0012f, 9000f, 2500f);
                    pico = 0.85f; break;
                }
                case "ore":   // minerio: 3 batidas de pedra em 40 ms (ruido curto + modo de pedra bem amortecido) e o baque
                {
                    b = Buf(0.2f); float s = v == 0 ? 1f : 1.12f;
                    for (int i = 0; i < 3; i++)
                    {
                        float at = i == 0 ? 0f : 0.012f * i + 0.01f * g.U(), a = i == 0 ? 1f : g.R(0.2f, 0.7f);
                        Ruido(b, Taxa, g, at, 0.015f, 0.8f * a, 0f, 0.004f, 5000f, 700f);
                        Modo(b, Taxa, at, g.R(900f, 1400f) * s, 0.35f * a, 0.018f);
                    }
                    Modo(b, Taxa, 0f, 190f * s, 0.7f, 0.03f);
                    pico = 0.85f; break;
                }
                case "drop":   // item no lugar / pegar lingote: "toc" de madeira
                {
                    b = Buf(0.16f); float s = v == 0 ? 1f : 1.07f;
                    Ruido(b, Taxa, g, 0f, 0.005f, 0.5f, 0f, 0.0012f, 8000f, 1200f);
                    Modo(b, Taxa, 0f, 520f * s, 1f, 0.035f);
                    Modo(b, Taxa, 0f, 1340f * s, 0.35f, 0.014f);
                    Modo(b, Taxa, 0f, 200f * s, 0.6f, 0.04f);
                    pico = 0.8f; break;
                }
                case "click":   // UI: tique curto
                    b = Buf(0.06f);
                    Ruido(b, Taxa, g, 0f, 0.003f, 0.6f, 0f, 0.0008f, 11000f, 2500f);
                    Modo(b, Taxa, 0f, 1900f, 0.8f, 0.008f);
                    Modo(b, Taxa, 0f, 3400f, 0.3f, 0.004f);
                    pico = 0.7f; break;
                case "coin":   // venda/bau: duas moedas tilintando (a 2a uma quarta acima, 55 ms depois)
                {
                    b = Buf(0.45f); float d = 1f + 0.02f * (v - 1);
                    Moeda(b, Taxa, g, 0f, 1760f * d, 1f);
                    Moeda(b, Taxa, g, 0.055f + 0.01f * v, 1760f * d * 1.335f, 0.75f);
                    pico = 0.65f; break;   // o som mais frequente (joalheria vende a cada 0,4 s), e o alto-falante realca 2-6 kHz
                }
                case "upgrade":   // compra: arpejo de alaude do-mi-sol-do + brilho de moeda
                {
                    b = Buf(0.9f); int[] n = { 72, 76, 79, 84 };
                    for (int i = 0; i < n.Length; i++) Corda(b, Taxa, g, 0.065f * i, Hz(n[i]), 1f - 0.1f * i, 0.4f);
                    Moeda(b, Taxa, g, 0.2f, 2093f, 0.3f);
                    pico = 0.85f; break;
                }
                case "hire":   // ajudante novo: tres cordas em quintas (re-la-re), mais lentas que a compra
                {
                    b = Buf(0.75f); int[] n = { 74, 81, 86 };
                    for (int i = 0; i < n.Length; i++) Corda(b, Taxa, g, 0.085f * i, Hz(n[i]), 0.9f, 0.35f);
                    pico = 0.8f; break;
                }
                case "client":   // cliente chegou: sininho da porta, duas batidas (a 2a mais fraca)
                    b = Buf(0.8f);
                    Sino(b, Taxa, g, 0f, 1568f, 1f);
                    Sino(b, Taxa, g, 0.12f, 1568f * 1.003f, 0.55f);
                    pico = 0.55f; break;
                case "leave":   // cliente desistiu / anuncio falhou: duas notas de flauta descendo, a 2a caindo no fim
                    b = Buf(0.7f);
                    Flauta(b, Taxa, g, 0f, Hz(69), 0.16f, 0.8f);
                    Flauta(b, Taxa, g, 0.2f, Hz(65), 0.28f, 0.8f, 0.94f);
                    pico = 0.45f; break;   // nota sustentada: RMS ~2x o dos outros no mesmo pico
                case "vip":   // VIP chegou: fanfarra de arauto la-la-REEE, a nota longa com a quarta embaixo
                    b = Buf(1.1f);
                    Trompa(b, Taxa, 0f, Hz(69), 0.1f, 0.8f);
                    Trompa(b, Taxa, 0.14f, Hz(69), 0.1f, 0.8f);
                    Trompa(b, Taxa, 0.28f, Hz(74), 0.5f, 1f);
                    Trompa(b, Taxa, 0.28f, Hz(69), 0.5f, 0.45f);
                    pico = 0.9f; break;
                case "order":   // encomenda entregue: acorde de re menor rasgado, re agudo e moeda ("ta-da")
                    b = Buf(1f);
                    Corda(b, Taxa, g, 0f, Hz(74), 0.8f, 0.5f);
                    Corda(b, Taxa, g, 0.03f, Hz(77), 0.7f, 0.5f);
                    Corda(b, Taxa, g, 0.06f, Hz(81), 0.7f, 0.5f);
                    Corda(b, Taxa, g, 0.22f, Hz(86), 1f, 0.6f);
                    Moeda(b, Taxa, g, 0.22f, 2640f, 0.5f);
                    pico = 0.85f; break;
                case "offline":   // cofre / VIP pago: tampa do bau e uma cascata de 7 moedas
                    b = Buf(1.1f);
                    Modo(b, Taxa, 0f, 150f, 0.8f, 0.06f);
                    Ruido(b, Taxa, g, 0f, 0.03f, 0.5f, 0.002f, 0.01f, 1500f, 100f);
                    for (int i = 0; i < 7; i++) Moeda(b, Taxa, g, 0.1f + 0.08f * i + 0.03f * g.U(), g.R(1500f, 2300f), 0.9f - 0.08f * i);
                    pico = 0.85f; break;
                default: throw new ArgumentException("som desconhecido: " + nome);
            }
            return Fecha(b, pico, 0.01f);
        }

        // ---------- ambiente ----------

        /// <summary>Fogo da forja (4 s em laco): ronco que respira (~2 Hz), chiado fino e estalos esparsos (~7/s, quase todos fracos).</summary>
        public static float[] Fogo()
        {
            const int R = Taxa; const float L = 4f, X = 0.3f;
            float[] b = Buf(L + X); var g = new Rng(17);
            float l1 = 0f, l2 = 0f, hs = 0f, hz = 0f, a1 = Polo(400f, R), a2 = Polo(800f, R), ah = Polo(5000f, R), hh = Polo(1800f, R);
            float m0 = g.U(), m1 = g.U(); const int Seg = R / 2;
            for (int k = 0; k < b.Length; k++)
            {
                if (k % Seg == 0) { m0 = m1; m1 = g.U(); }
                float u = k % Seg / (float)Seg; u = u * u * (3f - 2f * u);
                float mod = 0.55f + 0.45f * (m0 + (m1 - m0) * u), x = g.B();
                l1 += a1 * (x - l1); l2 += a2 * (l1 - l2);
                hs += ah * (x - hs); hz += hh * (hs - hz);
                b[k] = mod * 3f * l2 + 0.25f * (0.6f + 0.4f * mod) * (hs - hz);
            }
            for (int i = 0, n = (int)(L * 7f); i < n; i++)
            {
                float at = g.U() * L, a = 0.15f + 0.85f * g.U() * g.U() * g.U(), fc = g.R(1500f, 3500f);
                for (int j = 0, m = 1 + (int)(g.U() * 3f); j < m; j++) Ruido(b, R, g, at + 0.004f * j, 0.006f, a * g.R(0.5f, 1f), 0f, 0.0012f, 7000f, fc);
            }
            return Fecha(Laco(b, (int)(L * R), (int)(X * R)), 0.9f, 0f);
        }

        /// <summary>
        /// Murmurio da rua (6 s em laco): 7 "vozes" (fonte vozeada + sopro por 2 formantes) falando em silabas de 0,09-0,24 s, frases
        /// com pausas, passa-baixa em ~2,2 kHz no fim: gente a uns metros, sem palavra.
        /// </summary>
        public static float[] Rua()
        {
            const int R = Taxa; const float L = 6f, X = 0.4f;
            float[] b = Buf(L + X), env = new float[b.Length]; var g = new Rng(29); float fim = b.Length / (float)R;
            for (int v = 0; v < 7; v++)
            {
                Array.Clear(env, 0, env.Length);
                for (float t = g.R(0f, 1f); t < fim; t += g.R(0.35f, 1.4f))   // frase de 2-7 silabas, depois pausa
                    for (int s = 0, n = 2 + (int)(g.U() * 6f); s < n && t < fim; s++)
                    {
                        float len = g.R(0.09f, 0.24f), a = g.R(0.4f, 1f);
                        int s0 = (int)(t * R), sl = (int)(len * R);
                        for (int j = 0; j < sl && s0 + j < env.Length; j++) env[s0 + j] = Math.Max(env[s0 + j], a * (float)Math.Sin(Math.PI * j / sl));
                        t += len + g.R(0.01f, 0.06f);
                    }
                float f0 = g.R(95f, 230f), ph = 0f, ent = g.R(0f, 6.28f), F1 = g.R(450f, 800f), F2 = g.R(1100f, 2000f);
                float k1 = 2f * (float)Math.Sin(Math.PI * F1 / R), k2 = 2f * (float)Math.Sin(Math.PI * F2 / R), l1 = 0f, b1 = 0f, l2 = 0f, b2 = 0f;
                for (int k = 0; k < b.Length; k++)
                {
                    if (env[k] <= 0f) continue;   // ponytail: filtro parado no silencio; a silaba seguinte comeca com o resto (inaudivel)
                    ph += f0 * (1f + 0.1f * (float)Math.Sin(2.0 * Math.PI * 0.7 * k / R + ent)) / R;
                    if (ph >= 1f) ph -= 1f;
                    float src = 0.6f * (2f * ph - 1f) + 0.4f * g.B();
                    float h1 = src - l1 - 0.35f * b1; b1 += k1 * h1; l1 += k1 * b1;
                    float h2 = src - l2 - 0.35f * b2; b2 += k2 * h2; l2 += k2 * b2;
                    b[k] += env[k] * (b1 + 0.6f * b2);
                }
            }
            float p1 = 0f, p2 = 0f, lp = Polo(2200f, R);
            for (int k = 0; k < b.Length; k++) { p1 += lp * (b[k] - p1); p2 += lp * (p1 - p2); b[k] = p2; }
            return Fecha(Laco(b, (int)(L * R), (int)(X * R)), 0.9f, 0f);
        }

        // ---------- musica ----------

        public const int Compassos = 16;
        public const float Seminima = 0.6f;   // 100 bpm
        public static float MusicaS => Compassos * 3f * Seminima;

        /// <summary>
        /// Musica de fundo (DM s71, "medieval estilizado, nao musica cansativa"): 16 compassos de 3/4 a 100 bpm em re dorico (28,8 s,
        /// laco sem emenda: a cauda do fim volta para o comeco). Acordes Dm C Dm G | F C G Dm (o Sol maior e' a cor do dorico), alaude
        /// no baixo-quinta-terca. Parte A: melodia no alaude + tambor de moldura (DUM no 1, tec no 3). Parte B: flauta em notas longas,
        /// so o DUM e um bordao re-la que entra e sai: o respiro que deixa o laco nao cansar.
        /// </summary>
        public static float[] Musica()
        {
            const int R = TaxaMusica; const float Q = Seminima, E = Q / 2f, Bar = 3f * Q;
            int L = (int)(MusicaS * R);
            var b = new float[L + 4 * R]; var g = new Rng(71);
            int[][] acorde = { new[] { 50, 57, 65 }, new[] { 48, 55, 64 }, new[] { 50, 57, 65 }, new[] { 43, 50, 59 },
                               new[] { 41, 48, 57 }, new[] { 48, 55, 64 }, new[] { 43, 50, 59 }, new[] { 50, 57, 62 } };
            for (int c = 0; c < Compassos; c++)
            {
                int[] a = acorde[c % 8]; float t0 = c * Bar; bool partA = c < 8;
                Corda(b, R, g, t0 + 0.005f * g.U(), Hz(a[0]), 0.55f * g.R(0.9f, 1f), 0.7f);
                Corda(b, R, g, t0 + Q + 0.01f * g.U(), Hz(a[1]), 0.3f * g.R(0.85f, 1f), 0.45f);
                Corda(b, R, g, t0 + 2f * Q + 0.01f * g.U(), Hz(a[2]), 0.28f * g.R(0.85f, 1f), 0.45f);
                Modo(b, R, t0, 95f, partA ? 0.4f : 0.3f, 0.12f);   // DUM: pele grave (fone) + 160 Hz e batida (alto-falante)
                Modo(b, R, t0, 160f, partA ? 0.25f : 0.18f, 0.06f);
                Ruido(b, R, g, t0, 0.03f, partA ? 0.2f : 0.14f, 0f, 0.01f, 800f, 40f);
                if (!partA) continue;
                Ruido(b, R, g, t0 + 2f * Q, 0.03f, 0.12f, 0f, 0.008f, 6000f, 1500f);   // tec na borda
                Modo(b, R, t0 + 2f * Q, 420f, 0.1f, 0.03f);
            }
            // melodia A (alaude) e B (flauta): pares (nota MIDI, colcheias); 0 = pausa. 6 colcheias por compasso
            int[] melA = { 69, 2, 74, 2, 76, 1, 77, 1, /**/ 76, 2, 72, 2, 74, 1, 76, 1, /**/ 77, 2, 76, 1, 74, 1, 69, 2, /**/ 71, 2, 74, 2, 67, 2,
                           69, 2, 72, 2, 77, 1, 76, 1, /**/ 76, 2, 79, 1, 76, 1, 72, 2, /**/ 71, 2, 67, 1, 69, 1, 71, 1, 72, 1, /**/ 74, 6 };
            int[] melB = { 77, 4, 76, 2, /**/ 76, 4, 74, 2, /**/ 74, 4, 72, 2, /**/ 71, 6, /**/ 72, 4, 69, 2, /**/ 67, 4, 72, 2, /**/ 71, 4, 72, 2, /**/ 74, 4, 0, 2 };
            float tm = 0f;
            for (int i = 0; i < melA.Length; i += 2)
            {
                if (melA[i] > 0) Corda(b, R, g, tm + 0.008f * g.U(), Hz(melA[i]), 0.5f * g.R(0.85f, 1f), 0.55f);
                tm += melA[i + 1] * E;
            }
            tm = 8f * Bar;
            for (int i = 0; i < melB.Length; i += 2)
            {
                if (melB[i] > 0) Flauta(b, R, g, tm, Hz(melB[i]), melB[i + 1] * E - 0.05f, 0.2f);
                tm += melB[i + 1] * E;
            }
            // bordao re3 + la3 so na parte B, numa janela sen^2 (zero nas pontas: nao precisa casar fase no laco)
            int b0 = (int)(8f * Bar * R), bn = L - b0;
            for (int k = 0; k < bn; k++)
            {
                double t = k / (double)R, w = Math.Sin(Math.PI * k / bn);
                b[b0 + k] += (float)(0.05 * w * w * (Math.Sin(2 * Math.PI * 146.83 * t) + 0.7 * Math.Sin(2 * Math.PI * 220.0 * t) + 0.2 * Math.Sin(2 * Math.PI * 293.66 * t)));
            }
            return Fecha(Dobra(b, L), 0.9f, 0f);   // a cauda do ultimo compasso soa por cima do primeiro
        }

        // ---------- mix do ambiente (DM s70: "quando o negocio cresce, o audio tambem cresce") ----------

        /// <summary>
        /// Alvo 0..1 das 4 camadas do ambiente: fogo (sempre, mais forte com mais estacoes trabalhando), fogo2 (2a camada mais grave, so
        /// da metade para cima), rua e rua2 (idem com clientes nas filas). `ativas` = fornalhas/bancadas produzindo agora (7 no maximo),
        /// `fila` = clientes nas duas filas (ate 13).
        /// </summary>
        public static (float fogo, float fogo2, float rua, float rua2) Camadas(int ativas, int fila)
        {
            float f = Clamp01(ativas / 6f), r = Clamp01(fila / 10f);
            return (0.5f + 0.5f * f, Clamp01(2f * f - 1f), 0.4f + 0.6f * r, Clamp01(2f * r - 1f));   // parado -> cheio: ~+9,5 dB
        }

        // ---------- blocos ----------

        static readonly float[] BigR = { 1f, 2.08f, 2.83f, 3.91f, 5.15f }, BigA = { 1f, 0.55f, 0.5f, 0.3f, 0.2f }, BigT = { 0.38f, 0.22f, 0.18f, 0.11f, 0.07f };
        static readonly float[] MoeR = { 1f, 1.47f, 2.09f, 2.56f }, MoeA = { 1f, 0.7f, 0.5f, 0.35f }, MoeT = { 0.22f, 0.15f, 0.1f, 0.07f };
        static readonly float[] SinR = { 1f, 1.73f, 2.33f, 3.03f }, SinA = { 1f, 0.5f, 0.35f, 0.2f }, SinT = { 0.35f, 0.22f, 0.15f, 0.1f };

        /// <summary>Bigorna: 5 parciais inarmonicos (o "tiiin" do aco) com 1% de desvio aleatorio cada; `tau` escala a duracao.</summary>
        static void Bigorna(float[] b, int rate, Rng g, float at, float f, float amp, float tau)
        {
            for (int i = 0; i < BigR.Length; i++) Modo(b, rate, at, f * BigR[i] * (1f + 0.01f * g.B()), amp * BigA[i], tau * BigT[i]);
        }

        static void Moeda(float[] b, int rate, Rng g, float at, float f, float amp)
        {
            Ruido(b, rate, g, at, 0.004f, 0.4f * amp, 0f, 0.001f, 11000f, 3000f);
            for (int i = 0; i < MoeR.Length; i++) Modo(b, rate, at, f * MoeR[i], amp * MoeA[i], MoeT[i]);
        }

        static void Sino(float[] b, int rate, Rng g, float at, float f, float amp)
        {
            Ruido(b, rate, g, at, 0.003f, 0.3f * amp, 0f, 0.001f, 11000f, 2500f);
            for (int i = 0; i < SinR.Length; i++) Modo(b, rate, at, f * SinR[i], amp * SinA[i], SinT[i]);
        }

        /// <summary>Corda dedilhada (alaude/harpa): 6 harmonicos levemente inarmonicos, dedilhada a 22% do cavalete, agudos morrendo antes, e o estalo da unha.</summary>
        static void Corda(float[] b, int rate, Rng g, float at, float f, float amp, float tau)
        {
            Ruido(b, rate, g, at, 0.006f, 0.12f * amp, 0f, 0.0015f, 9000f, 1500f);
            for (int n = 1; n <= 6; n++)
                Modo(b, rate, at, f * n * (1f + 0.0003f * n * n), amp * (float)Math.Abs(Math.Sin(n * Math.PI * 0.22)) / n, tau / (1f + 0.6f * (n - 1)));
        }

        /// <summary>Flauta doce: seno + 2o e 3o harmonicos fracos + sopro (ruido entre f e 3f), ataque de 50 ms, vibrato depois de 0,2 s;
        /// `glide` = altura no fim da nota / no comeco.</summary>
        static void Flauta(float[] b, int rate, Rng g, float at, float f, float dur, float amp, float glide = 1f)
        {
            int i0 = (int)(at * rate), n = Math.Min(b.Length - i0, (int)((dur + 0.2f) * rate));
            double ph = 0; float y = 0f, z = 0f, a = Polo(f * 3f, rate), h = Polo(f, rate);
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)rate;
                float env = Math.Min(1f, t / 0.05f) * (t < dur ? 1f : (float)Math.Exp(-(t - dur) / 0.04f));
                float vib = 1f + 0.006f * Clamp01((t - 0.2f) / 0.3f) * (float)Math.Sin(2 * Math.PI * 5.2 * t);
                ph += 2 * Math.PI * f * (1f + (glide - 1f) * Clamp01(t / dur)) * vib / rate;
                y += a * (g.B() - y); z += h * (y - z);
                b[i0 + k] += amp * env * ((float)(Math.Sin(ph) + 0.22 * Math.Sin(2 * ph) + 0.08 * Math.Sin(3 * ph)) + 0.25f * (y - z));
            }
        }

        /// <summary>Trompa de arauto: harmonicos com brilho que abre no ataque e assenta (o "metal" do brass), vibrato so na nota longa.</summary>
        static void Trompa(float[] b, int rate, float at, float f, float dur, float amp)
        {
            int i0 = (int)(at * rate), n = Math.Min(b.Length - i0, (int)((dur + 0.25f) * rate));
            double ph = 0;
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)rate;
                float env = Math.Min(1f, t / 0.025f) * (t < dur ? 1f - 0.2f * Clamp01(t / 0.15f) : 0.8f * (float)Math.Exp(-(t - dur) / 0.05f));
                float bri = 0.45f + 0.35f * Clamp01(t / 0.04f) - 0.1f * Clamp01(t / 0.2f);
                float vib = dur > 0.3f ? 1f + 0.005f * Clamp01((t - 0.15f) / 0.2f) * (float)Math.Sin(2 * Math.PI * 5.5 * t) : 1f;
                ph += 2 * Math.PI * f * vib / rate;
                double s = 0, w = 1;
                for (int hm = 1; hm <= 10 && hm * f < rate * 0.45f; hm++) { s += w * Math.Sin(hm * ph); w *= bri; }
                b[i0 + k] += amp * env * (float)s * 0.35f;
            }
        }

        /// <summary>Soma amp·e^(-t/tau)·sen(2πft) a partir de `at` s: ressonador recursivo (sem Math.Sin por amostra), ate -60 dB.</summary>
        static void Modo(float[] b, int rate, float at, float f, float amp, float tau)
        {
            if (f >= rate * 0.45f) return;   // perto de Nyquist: so dobraria (aliasing)
            int i0 = (int)(at * rate), n = Math.Min(b.Length - i0, (int)(tau * rate * 6.9f));
            double w = 2.0 * Math.PI * f / rate, r = Math.Exp(-1.0 / (tau * rate)), c = 2.0 * r * Math.Cos(w), r2 = r * r;
            double y0 = 0.0, y1 = amp * r * Math.Sin(w);
            for (int k = 0; k < n; k++)
            {
                b[i0 + k] += (float)y0;
                double y2 = c * y1 - r2 * y0; y0 = y1; y1 = y2;
            }
        }

        /// <summary>Ruido branco com envelope (ataque linear `att`, queda e^(-t/tau)) por um passa-baixa `lp` e um passa-alta `hp` de 1 polo.</summary>
        static void Ruido(float[] b, int rate, Rng g, float at, float dur, float amp, float att, float tau, float lp, float hp)
        {
            int i0 = (int)(at * rate), n = Math.Min(b.Length - i0, (int)(dur * rate));
            float a = Polo(lp, rate), h = Polo(hp, rate), y = 0f, z = 0f;
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)rate;
                y += a * (g.B() - y); z += h * (y - z);
                b[i0 + k] += amp * (att > 0f ? Math.Min(1f, t / att) : 1f) * (float)Math.Exp(-t / tau) * (y - z);
            }
        }

        /// <summary>Seno com a frequencia indo de f0 a f1 (exponencial) e queda e^(-t/tau): o "tum" do corpo da batida.</summary>
        static void Varre(float[] b, int rate, float at, float f0, float f1, float amp, float tau, float dur)
        {
            int i0 = (int)(at * rate), n = Math.Min(b.Length - i0, (int)(dur * rate));
            double ph = 0;
            for (int k = 0; k < n; k++)
            {
                ph += 2 * Math.PI * f0 * Math.Pow(f1 / f0, k / (double)n) / rate;
                b[i0 + k] += amp * (float)(Math.Sin(ph) * Math.Exp(-k / (tau * rate)));
            }
        }

        /// <summary>Laco sem emenda (ruido): os X que passam de L entram em cross-fade de potencia constante nos X primeiros (o[0] continua o[L-1]).</summary>
        public static float[] Laco(float[] b, int L, int X)
        {
            var o = new float[L];
            Array.Copy(b, o, L);
            for (int i = 0; i < X; i++) { double w = Math.PI / 2 * i / X; o[i] = (float)(b[i] * Math.Sin(w) + b[L + i] * Math.Cos(w)); }
            return o;
        }

        /// <summary>Laco sem emenda (notas): o que passa de L soma no comeco, entao a nota que soava no fim continua por cima do inicio.</summary>
        public static float[] Dobra(float[] b, int L)
        {
            var o = new float[L];
            Array.Copy(b, o, L);
            for (int i = L; i < b.Length; i++) o[i - L] += b[i];
            return o;
        }

        /// <summary>Normaliza ao `pico` e, se `fade` &gt; 0 s, zera o fim em rampa (sem estalo quando o clip acaba).</summary>
        static float[] Fecha(float[] b, float pico, float fade)
        {
            float m = 0f;
            foreach (float x in b) m = Math.Max(m, Math.Abs(x));
            float k = m > 0f ? pico / m : 0f;
            int nf = (int)(fade * Taxa);
            for (int i = 0; i < b.Length; i++) b[i] *= k * (i >= b.Length - nf ? (b.Length - 1 - i) / (float)nf : 1f);
            return b;
        }

        static float[] Buf(float s) => new float[(int)(s * Taxa)];
        static float Polo(float fc, int rate) => 1f - (float)Math.Exp(-2.0 * Math.PI * fc / rate);
        static float Hz(int midi) => 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0);
        static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : x;

        /// <summary>FNV-1a do nome: o string.GetHashCode muda a cada processo no .NET novo, e o som tem de sair igual sempre.</summary>
        static int Semente(string s)
        {
            uint h = 2166136261u;
            foreach (char c in s) h = (h ^ c) * 16777619u;
            return (int)h;
        }

        /// <summary>xorshift32: barato por amostra, deterministico, sem alocacao.</summary>
        sealed class Rng
        {
            uint _s;
            public Rng(int seed) { _s = (uint)seed * 2654435761u | 1u; }
            public float U() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return (_s >> 8) / 16777216f; }   // [0, 1)
            public float B() => U() * 2f - 1f;
            public float R(float a, float b) => a + (b - a) * U();
        }
    }

#if !FS_CORETESTS
    /// <summary>
    /// "Mixer leve" (A-AUD-23; o AudioMixer de asset exige o Editor): 3 grupos por codigo. Efeitos = pool de 6 AudioSource (sem fonte nova
    /// por evento; cheia, rouba a mais antiga; cada voz com a sua altura, o que o PlayOneShot numa fonte so nao fazia), Ambiente = 4 lacos
    /// (fogo, fogo grave, rua, rua densa) com volume seguindo o Sim, Musica = 1 laco com fade. Efeitos e ambiente obedecem a chave Efeitos;
    /// a musica, a chave Musica (Ajustes). Desligado = fonte parada (nada mixando), nao volume 0. Os efeitos sao sintetizados no Init (a
    /// abertura toca o cofre); musica e ambiente numa thread e entram quando ficam prontos.
    /// </summary>
    public static class Sfx
    {
        // ponytail: volumes dos grupos chutados sem ouvir; calibrar no POCO (alto-falante e fone) e no PC
        // (RMS dos clips: efeitos ~0,14 a 0,55 de volume; musica 0,12; fogo 0,21; rua 0,11 -> musica ~-7 dB e ambiente -13 dB parado / -4 dB
        // cheio abaixo dos efeitos)
        const float MusicaVol = 0.3f, AmbienteVol = 0.15f, FadeMusica = 1.5f, Suave = 0.8f;
        const int Vozes = 6;
        static readonly AudioSource[] _fx = new AudioSource[Vozes], _amb = new AudioSource[4];
        static readonly float[] _ambPitch = { 1f, 0.82f, 1f, 1.08f }, _ambVol = new float[4];
        static AudioSource _musica;
        static int _voz;
        static bool _musicaOn, _ambOn, _fxOn = true;
        static Task<float[][]> _fundo;

        sealed class Som { public AudioClip[] V; public int K; public float Last = -99f; }
        static readonly Dictionary<string, Som> Sons = new Dictionary<string, Som>();

        public static void Init(GameObject host)
        {
            _musicaOn = _ambOn = false; _fxOn = true; _voz = 0;   // Editor sem domain reload: estaticos da sessao anterior
            for (int i = 0; i < Vozes; i++) _fx[i] = Fonte(host, false);
            for (int i = 0; i < _amb.Length; i++) { _amb[i] = Fonte(host, true); _amb[i].pitch = _ambPitch[i]; }
            _musica = Fonte(host, true);
            foreach (string n in Sintese.Nomes)
            {
                var v = new AudioClip[Sintese.Variacoes(n)];
                for (int k = 0; k < v.Length; k++) v[k] = Clip(n + k, Sintese.Efeito(n, k), Sintese.Taxa);
                Sons[n] = new Som { V = v };
            }
            _fundo = Task.Run(() => new[] { Sintese.Fogo(), Sintese.Rua(), Sintese.Musica() });
        }

        /// <summary>Toca no maximo uma vez por `minGap` segundos por nome (varias marteladas no mesmo quadro viram uma), alternando as variacoes.</summary>
        public static void Play(string name, float pitch = 1f, float minGap = 0.05f, float vol = 0.55f)
        {
            if (!Ajustes.Efeitos || !Sons.TryGetValue(name, out Som s)) return;   // Efeitos desligado = nada toca
            if (Time.unscaledTime - s.Last < minGap) return;
            s.Last = Time.unscaledTime;
            AudioSource src = Livre();
            src.clip = s.V[s.K];
            s.K = (s.K + 1) % s.V.Length;
            src.pitch = pitch;
            src.volume = vol;
            src.Play();
        }

        /// <summary>
        /// Um quadro (Game.Update): liga/desliga os grupos pelas chaves, faz o fade da musica e leva o ambiente ate o alvo das
        /// Sintese.Camadas (estacoes produzindo agora e clientes nas filas, lidos do Sim) em ~1 s.
        /// </summary>
        public static void Tick(Sim sim, float dt)
        {
            if (_fundo != null && _fundo.IsCompleted) Fundo();
            bool fx = Ajustes.Efeitos;
            if (fx != _fxOn)
            {
                _fxOn = fx;
                if (!fx) foreach (AudioSource s in _fx) s.Stop();   // desligou no meio de um som: corta
            }

            bool m = Ajustes.Musica && _musica.clip != null;
            if (m != _musicaOn)
            {
                _musicaOn = m;
                if (m) { _musica.volume = 0f; _musica.Play(); } else _musica.Stop();
            }
            if (m) _musica.volume = Mathf.MoveTowards(_musica.volume, MusicaVol, dt * MusicaVol / FadeMusica);

            bool a = fx && _amb[0].clip != null;
            if (a != _ambOn)
            {
                _ambOn = a;
                for (int i = 0; i < _amb.Length; i++)
                {
                    if (!a) { _amb[i].Stop(); continue; }
                    _ambVol[i] = _amb[i].volume = 0f;
                    _amb[i].Play();
                    if (i % 2 == 1) _amb[i].timeSamples = _amb[i].clip.samples / 2;   // a 2a camada noutro ponto do laco: soma, nao dobra
                }
            }
            if (!a) return;
            int ativas = 0;
            foreach (Station s in sim.Stations) if (s.Unlocked && s.Produces && s.Busy && s.Progress < 1f) ativas++;
            var (f1, f2, r1, r2) = Sintese.Camadas(ativas, sim.Queue.Count + sim.JewelQueue.Count);
            float k = Mathf.Min(1f, dt * Suave);
            Mix(0, f1, k); Mix(1, f2, k); Mix(2, r1, k); Mix(3, r2, k);
        }

        static void Mix(int i, float alvo, float k) => _amb[i].volume = AmbienteVol * (_ambVol[i] += (alvo - _ambVol[i]) * k);

        /// <summary>Musica e ambiente prontos na thread: viram clip aqui (a API de audio so no thread principal). Falhou: segue sem eles.</summary>
        static void Fundo()
        {
            Task<float[][]> t = _fundo;
            _fundo = null;
            if (t.Status != TaskStatus.RanToCompletion) { Debug.LogWarning("SOM fundo nao gerado: " + t.Exception?.GetBaseException().Message); return; }
            float[][] r = t.Result;
            _amb[0].clip = _amb[1].clip = Clip("fogo", r[0], Sintese.Taxa);
            _amb[2].clip = _amb[3].clip = Clip("rua", r[1], Sintese.Taxa);
            _musica.clip = Clip("musica", r[2], Sintese.TaxaMusica);
        }

        /// <summary>Voz livre do pool, a partir da seguinte a ultima usada; todas ocupadas = a mais antiga (ponytail: rodizio, sem prioridade
        /// por som; 6 vozes + a espera por nome bastaram no bot; prioridade se o VIP for cortado por moedas).</summary>
        static AudioSource Livre()
        {
            for (int i = 0; i < Vozes; i++)
            {
                int j = (_voz + i) % Vozes;
                if (_fx[j].isPlaying) continue;
                _voz = (j + 1) % Vozes;
                return _fx[j];
            }
            AudioSource o = _fx[_voz];
            _voz = (_voz + 1) % Vozes;
            return o;
        }

        static AudioSource Fonte(GameObject host, bool loop)
        {
            var s = host.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            return s;
        }

        static AudioClip Clip(string name, float[] data, int rate)
        {
            AudioClip c = AudioClip.Create(name, data.Length, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
#endif
}
