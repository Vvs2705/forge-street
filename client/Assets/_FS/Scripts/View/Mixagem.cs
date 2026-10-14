using System;
using System.Collections.Generic;
using System.Globalization;
using FS.Core;

namespace FS
{
    /// <summary>Categoria de um som. A ordem e' a prioridade: a maior rouba a voz da menor.</summary>
    public enum Cat { Mundo, Acao, UI, Importante }

    /// <summary>
    /// fix/mix-audio (POCO, Vinicius depois de 5 min: "que barulheira, muitos sons ao mesmo tempo, uma zona sonora"). Politica de mix em C#
    /// puro: o Sfx (Unity) so toca o que ela decide, e o MixagemTests roda o bot pelo nucleo e mede antes/depois (docs/AUDIO.md). Cada som do
    /// jogo tem nome, clip, categoria, volume e intervalo (Regras). Regras gerais:
    /// - orcamento de `Vozes` vozes; teto por categoria (Mundo 2, Acao 2, UI 1, Importante 1); intervalo minimo por nome e por categoria;
    /// - Mundo (bancadas, esteira, fornalha, sino, ajudantes) so com a fonte na tela, atenuado pela distancia ao ferreiro (cheio ate `Perto`,
    ///   zero em `Longe`; abaixo de `Minimo` nem toca); Acao com posicao (venda, cliente que desistiu) atenua ate o piso de 35%;
    /// - Importante sempre toca: rouba a voz de menor prioridade (a mais antiga), o mesmo nome reinicia a propria voz e outro Importante soando
    ///   poe este na fila (toca quando aquele acaba: duas fanfarras nunca se atropelam); enquanto soa, musica, ambiente e Mundo descem 6 dB
    ///   (Duck) por ate 1 s e voltam em 0,3 s;
    /// - pitch: Mundo e Acao variam +-3%/+-2% numa sequencia fixa (o que repete nao soa igual), sem nenhum som subindo a cada repeticao.
    /// </summary>
    public sealed class Mixagem
    {
        public const int Vozes = 4;
        /// <summary>m: distancia ao ferreiro com volume cheio, com volume zero; ganho de distancia abaixo de Minimo = nem toca.</summary>
        public const float Perto = 2f, Longe = 6f, Minimo = 0.2f;
        /// <summary>Ducking: -6 dB por ate DuckMax s depois que um Importante comeca, voltando em DuckSolta s.</summary>
        public const float DuckGanho = 0.5f, DuckMax = 1f, DuckSolta = 0.3f;
        /// <summary>Musica (Sfx): 1 volta, depois MusicaPausa s so com o ambiente; entra em MusicaEntra s e sai nos ultimos MusicaSai s.</summary>
        public const float MusicaPausa = 30f, MusicaEntra = 0.4f, MusicaSai = 2.5f;

        // por categoria (indice = Cat): vozes ao mesmo tempo, s entre 2 da mesma, piso do ganho de distancia (1 = nao atenua), variacao de pitch
        public static readonly int[] Teto = { 2, 2, 1, 1 };
        public static readonly float[] GapCat = { 0.75f, 0f, 0f, 0f }, Piso = { 0f, 0.35f, 1f, 1f };
        static readonly float[] Jitter = { 0.03f, 0.02f, 0f, 0f };
        static readonly float[] Jit = { 0f, 0.7f, -0.9f, 0.3f, -0.4f, 1f, -0.6f };   // sequencia fixa (deterministica) em vez de sorteio

        public readonly struct Regra
        {
            public readonly string Clip; public readonly Cat Cat; public readonly float Vol, Gap, Pitch;
            public Regra(string clip, Cat cat, float vol, float gap = 0f, float pitch = 1f) { Clip = clip; Cat = cat; Vol = vol; Gap = gap; Pitch = pitch; }
        }

        /// <summary>
        /// Todo som do jogo. Volume pelo RMS de 50 ms de cada clip (docs/AUDIO.md §2) para cair no alvo da categoria: Importante -14 dBFS,
        /// Acao -19, UI -21, Mundo -25 (perto); clip agudo (mais de 60% da energia acima de 2 kHz: moeda, sino, cofre, clique) leva -3 dB, porque
        /// o alto-falante do celular e o ouvido realcam 2-6 kHz. Gap = s minimos entre 2 do mesmo nome (de qualquer bancada/fonte).
        /// </summary>
        public static readonly Dictionary<string, Regra> Regras = new Dictionary<string, Regra>
        {
            // Importante: compra, contratacao, VIP, encomenda, Renome, meta, cofre, projeto, obra-prima, bau, marco, anuncio
            ["compra"] = new Regra("upgrade", Cat.Importante, 0.82f),
            ["boost"] = new Regra("upgrade", Cat.Importante, 0.8f, 0f, 1.2f),
            ["contrata"] = new Regra("hire", Cat.Importante, 0.81f),
            ["vip"] = new Regra("vip", Cat.Importante, 0.69f),
            ["renome"] = new Regra("vip", Cat.Importante, 0.69f, 0f, 0.9f),
            ["encomenda"] = new Regra("order", Cat.Importante, 0.76f),
            ["obra"] = new Regra("order", Cat.Importante, 0.76f),
            ["projeto"] = new Regra("order", Cat.Importante, 0.72f, 0f, 1.2f),
            ["cofre"] = new Regra("offline", Cat.Importante, 0.6f),
            ["vipPago"] = new Regra("offline", Cat.Importante, 0.6f, 0f, 1.15f),
            ["meta"] = new Regra("coin", Cat.Importante, 0.62f, 0f, 1.3f),
            ["marco"] = new Regra("coin", Cat.Importante, 0.6f, 0f, 1.4f),
            ["bau"] = new Regra("coin", Cat.Importante, 0.66f),
            ["supply"] = new Regra("furnace", Cat.Importante, 0.71f),
            ["vipFoi"] = new Regra("leave", Cat.Importante, 0.55f),
            ["falhou"] = new Regra("leave", Cat.Importante, 0.5f, 0f, 1.3f),
            // Acao do jogador: o que ele pega e larga, a venda (o "pagamento" do loop), a peca Fina/Superior, o golpe de mestre
            ["minerio"] = new Regra("ore", Cat.Acao, 0.57f, 0.45f),
            ["pegar"] = new Regra("drop", Cat.Acao, 0.46f, 0.45f),
            ["largar"] = new Regra("drop", Cat.Acao, 0.46f, 0.45f, 1.1f),
            ["venda"] = new Regra("coin", Cat.Acao, 0.37f, 1.2f),     // vendas a menos de 1,2 s viram 1 moeda; mesma altura sempre (subia a cada venda)
            ["fina"] = new Regra("coin", Cat.Acao, 0.33f, 6f, 1.15f),  // era 1,35/1,6 a cada peca: agudo repetido; com quase tudo Fina, 1 a cada 6 s
            ["golpe"] = new Regra("hammer", Cat.Acao, 0.57f, 0.1f, 1.25f),
            ["perdeu"] = new Regra("leave", Cat.Acao, 0.4f, 3f),
            // UI
            ["click"] = new Regra("click", Cat.UI, 0.55f, 0.05f),
            // Mundo: textura de fundo, nunca no lugar de um aviso
            ["martelo"] = new Regra("martelo", Cat.Mundo, 0.2f, 3f),   // compasso de qualquer bancada (1 batida a cada 0,45 s): 1 martelada a cada 3 s, longe some
            ["peca"] = new Regra("hammer", Cat.Mundo, 0.29f, 2f),      // golpe final da peca (+3 dB: o acento do compasso)
            ["lingote"] = new Regra("furnace", Cat.Mundo, 0.2f, 2f),
            ["esteira"] = new Regra("esteira", Cat.Mundo, 0.2f, 3f),     // era ~4 por s (1 por divisa)
            ["sino"] = new Regra("client", Cat.Mundo, 0.2f, 2f),         // so quem chega a fila vazia (DoEvento); +2 dB sobre o alvo: e' aviso
            // ajudante: pega/larga de todos os ajudantes e workers; 1 a cada 4 s, so perto do ferreiro
            ["ajudante"] = new Regra("drop", Cat.Mundo, 0.16f, 4f, 0.8f),
        };

        /// <summary>Regra do nome; nome que nao esta na tabela mas e' um clip (codigo de outra raia ainda com o nome do clip) toca como
        /// Importante (o "click", como UI). Desconhecido: Clip null (nao toca).</summary>
        public static Regra De(string som) =>
            som != null && Regras.TryGetValue(som, out Regra r) ? r
            : Array.IndexOf(Sintese.Nomes, som) >= 0 ? new Regra(som, Cat.Importante, 0.6f) : default;

        /// <summary>
        /// O som de um evento do Sim: o Game.HandleEvents e a medida do MixagemTests usam este mesmo mapa. Compra e contratacao/nivel de worker
        /// tocam no Game (Bought, WorkerComprado), porque o menu chama os dois fora do Tick. `pitch` multiplica o da regra.
        /// </summary>
        public static string DoEvento(SimEvent e, out float pitch)
        {
            pitch = 1f;
            switch (e.Kind)
            {
                case Ev.Picked: return e.B >= 0 ? "ajudante" : e.A == (int)Item.Ore ? "minerio" : "pegar";   // B = papel de quem pegou (-1 ferreiro)
                case Ev.Deposited: return e.B == 9 ? null : e.B >= 0 ? "ajudante" : "largar";              // B = 9: a esteira (o clac dela ja fala)
                case Ev.Crafted: return e.A == (int)Item.Ingot ? "lingote" : "peca";
                case Ev.Sold: return "venda";
                case Ev.Graded: pitch = e.B == (int)Quality.Superior ? 1.08f : 1f; return "fina";
                case Ev.Masterwork: return "obra";
                case Ev.Blueprint: return "projeto";
                case Ev.ClientArrived: return e.B == 1 ? "sino" : null;   // B = tamanho da fila com ele: so o 1o (a fila estava vazia)
                case Ev.ClientLeft: return e.B == 0 ? "perdeu" : null;    // B = 2: fila cheia, nem entrou
                case Ev.Hired: return e.B == 0 ? "contrata" : null;       // B != 0: worker da Equipe (Game.WorkerComprado)
                case Ev.Milestone: return "marco";
                case Ev.ChestOpened: return "bau";
                case Ev.VipArrived: return "vip";
                case Ev.VipServed: return "vipPago";
                case Ev.VipLeft: return "vipFoi";
                case Ev.OrderDone: return "encomenda";
                default: return null;
            }
        }

        /// <summary>O que o Sfx toca: voz do pool (0..Vozes-1), clip, volume ja com a distancia (o Duck o Sfx aplica no Mundo), pitch, categoria.</summary>
        public readonly struct Toque
        {
            public readonly int Voz; public readonly string Clip; public readonly float Vol, Pitch; public readonly Cat Cat;
            public Toque(int voz, string clip, float vol, float pitch, Cat cat) { Voz = voz; Clip = clip; Vol = vol; Pitch = pitch; Cat = cat; }
        }

        /// <summary>Contagem para a medida e o -somlog: tocados, barrados (intervalo, teto, distancia, fora da tela), cortados (voz soando trocada
        /// por outro som; Importante nunca), reinicios (o mesmo nome por cima dele mesmo), fila (Importante que esperou outro acabar).</summary>
        public sealed class Conta
        {
            public int Plays, Barrados, Cortados, CortadosImportante, Reinicios, Fila, Pico;
            public int[] PorCat = new int[4];
            public Conta Copia() { var c = (Conta)MemberwiseClone(); c.PorCat = (int[])PorCat.Clone(); return c; }
        }

        struct Voz { public string Som; public Cat Cat; public float Ini, Fim; }

        readonly Func<string, float> _dur;
        readonly Voz[] _v = new Voz[Vozes];
        readonly Dictionary<string, float> _ultimo = new Dictionary<string, float>();
        readonly float[] _ultCat = { -99f, -99f, -99f, -99f };
        readonly List<(string Som, float Pitch)> _fila = new List<(string, float)>();
        float _x0 = float.MinValue, _x1 = float.MaxValue, _y0 = float.MinValue, _y1 = float.MaxValue, _duckIni = -99f, _duckFim = -99f;
        int _j, _picoMin;
        public readonly Conta Total = new Conta();
        /// <summary>Tocados por nome desde o comeco (a medida do docs/AUDIO.md).</summary>
        public readonly Dictionary<string, int> PorSom = new Dictionary<string, int>();
        Conta _base = new Conta();   // Total no ultimo Minuto()
        /// <summary>Posicao do ferreiro (quem ouve), atualizada a cada quadro.</summary>
        public V2 Ouvinte;

        /// <summary>`duracao(clip)` = s do clip (Sfx: AudioClip.length; teste: Sintese.Efeito); a voz fica ocupada duracao/pitch.</summary>
        public Mixagem(Func<string, float> duracao) { _dur = duracao; }

        /// <summary>Area da camera no mundo (centro e meias medidas); NaTela usa 10% de folga, como o WorldView.NaTela.</summary>
        public void Tela(V2 centro, float meiaL, float meiaA)
        {
            _x0 = centro.X - 1.2f * meiaL; _x1 = centro.X + 1.2f * meiaL;
            _y0 = centro.Y - 1.2f * meiaA; _y1 = centro.Y + 1.2f * meiaA;
        }

        public bool NaTela(V2 p) => p.X >= _x0 && p.X <= _x1 && p.Y >= _y0 && p.Y <= _y1;

        /// <summary>
        /// Pede o som `som` no instante `t` (s), da posicao `pos` (null = sem lugar: UI, Importante, o ferreiro). true = tocar `q` agora;
        /// false = barrado, ou (Importante) na fila: sai pelo Fila() quando o Importante que soa acabar.
        /// </summary>
        public bool Pedir(string som, float t, out Toque q, V2? pos = null, float pitch = 1f)
        {
            q = default;
            Regra r = De(som);
            if (r.Clip == null) return false;
            int c = (int)r.Cat;
            float g = 1f;
            if (pos.HasValue && Piso[c] < 1f)
            {
                if (r.Cat == Cat.Mundo && !NaTela(pos.Value)) return Barra();
                g = Piso[c] + (1f - Piso[c]) * Clamp01((Longe - V2.Dist(pos.Value, Ouvinte)) / (Longe - Perto));
                if (g < Minimo) return Barra();
            }
            if (r.Cat == Cat.Importante)
            {
                int i = Soando(Cat.Importante, t);
                if (i >= 0 && _v[i].Som != som)
                {
                    if (!_fila.Exists(f => f.Som == som)) { _fila.Add((som, pitch)); Total.Fila++; }
                    return false;
                }
                return Toca(som, r, pitch, g, t, i >= 0 ? i : Escolhe(r.Cat, t), out q);
            }
            if (_ultimo.TryGetValue(som, out float u) && t - u < r.Gap) return Barra();
            if (t - _ultCat[c] < GapCat[c]) return Barra();
            return Toca(som, r, pitch, g, t, Escolhe(r.Cat, t), out q);
        }

        /// <summary>Um quadro: o 1o Importante da fila, se nenhum outro soa agora (o Sfx chama ate dar false).</summary>
        public bool Fila(float t, out Toque q)
        {
            q = default;
            if (_fila.Count == 0 || Soando(Cat.Importante, t) >= 0) return false;
            (string som, float pitch) = _fila[0];
            _fila.RemoveAt(0);
            return Toca(som, De(som), pitch, 1f, t, Escolhe(Cat.Importante, t), out q);
        }

        /// <summary>Ganho 0,5..1 de musica, ambiente e Mundo agora: -6 dB enquanto um Importante soa (ate DuckMax s), voltando em DuckSolta s.</summary>
        public float Duck(float t)
        {
            if (t < _duckIni) return 1f;
            if (t <= _duckFim) return DuckGanho;
            float k = (t - _duckFim) / DuckSolta;
            return k >= 1f ? 1f : DuckGanho + (1f - DuckGanho) * k;
        }

        /// <summary>Ganho 0..1 da musica `t` s depois do comeco de uma volta de `volta` s: sobe em MusicaEntra, desce nos ultimos MusicaSai, 0 na pausa.</summary>
        public static float MusicaGanho(float t, float volta) => t < 0f || t >= volta ? 0f : Math.Min(1f, Math.Min(t / MusicaEntra, (volta - t) / MusicaSai));

        /// <summary>Quantas vozes soam em `t`.</summary>
        public int Soando(float t)
        {
            int n = 0;
            foreach (Voz v in _v) if (v.Fim > t) n++;
            return n;
        }

        /// <summary>Categoria da voz `i` (o Sfx aplica o Duck nas de Mundo).</summary>
        public Cat CatDa(int i) => _v[i].Cat;

        /// <summary>Efeitos desligados: nada soa e a fila esvazia (nao toca atrasado quando religar).</summary>
        public void Cala()
        {
            for (int i = 0; i < Vozes; i++) _v[i].Fim = 0f;
            _fila.Clear();
        }

        /// <summary>Linha do -somlog do minuto `n` (desde a chamada anterior) e zera a conta do minuto.</summary>
        public string Minuto(int n)
        {
            Conta a = _base, b = Total;
            int D(Cat c) => b.PorCat[(int)c] - a.PorCat[(int)c];
            string s = string.Format(CultureInfo.InvariantCulture, "SOM min={0} plays={1} pico={2} cortados={3} barrados={4} fila={5} porCategoria=Importante:{6},Acao:{7},Mundo:{8},UI:{9}",
                n, b.Plays - a.Plays, _picoMin, b.Cortados - a.Cortados, b.Barrados - a.Barrados, b.Fila - a.Fila,
                D(Cat.Importante), D(Cat.Acao), D(Cat.Mundo), D(Cat.UI));
            _base = Total.Copia(); _picoMin = 0;
            return s;
        }

        /// <summary>Voz para um som de `cat`: com a categoria no teto, nenhuma (o clique novo troca o velho); senao uma livre; sem livre, a de
        /// menor prioridade abaixo de `cat` (a mais antiga). -1 = nao toca.</summary>
        int Escolhe(Cat cat, float t)
        {
            int n = 0, livre = -1, ui = -1;
            for (int i = 0; i < Vozes; i++)
            {
                if (_v[i].Fim <= t) { if (livre < 0) livre = i; continue; }
                if (_v[i].Cat == cat) { n++; ui = i; }
            }
            if (n >= Teto[(int)cat]) return cat == Cat.UI ? ui : -1;
            if (livre >= 0) return livre;
            int v = -1;
            for (int i = 0; i < Vozes; i++)
                if (_v[i].Cat < cat && (v < 0 || _v[i].Cat < _v[v].Cat || (_v[i].Cat == _v[v].Cat && _v[i].Ini < _v[v].Ini))) v = i;
            return v;
        }

        bool Toca(string som, Regra r, float pitch, float g, float t, int i, out Toque q)
        {
            q = default;
            if (i < 0) return Barra();
            if (_v[i].Fim > t)
            {
                if (_v[i].Som == som) Total.Reinicios++;
                else { Total.Cortados++; if (_v[i].Cat == Cat.Importante) Total.CortadosImportante++; }
            }
            int c = (int)r.Cat;
            float p = r.Pitch * pitch * (1f + Jitter[c] * Jit[_j++ % Jit.Length]);
            float fim = t + _dur(r.Clip) / p;
            _v[i] = new Voz { Som = som, Cat = r.Cat, Ini = t, Fim = fim };
            _ultimo[som] = t;
            _ultCat[c] = t;
            if (r.Cat == Cat.Importante) { _duckIni = t; _duckFim = Math.Min(fim, t + DuckMax); }
            int s = Soando(t);
            Total.Plays++; Total.PorCat[c]++;
            PorSom.TryGetValue(som, out int ps); PorSom[som] = ps + 1;
            Total.Pico = Math.Max(Total.Pico, s); _picoMin = Math.Max(_picoMin, s);
            q = new Toque(i, r.Clip, r.Vol * g, p, r.Cat);
            return true;
        }

        int Soando(Cat cat, float t)
        {
            for (int i = 0; i < Vozes; i++) if (_v[i].Fim > t && _v[i].Cat == cat) return i;
            return -1;
        }

        bool Barra() { Total.Barrados++; return false; }

        static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : x;
    }
}
