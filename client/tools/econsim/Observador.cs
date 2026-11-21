using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FS.Core;

/// <summary>
/// --observador arquivo.md (PROJETO.md §7/§8, regua S/E de 2026-11-21): o bot joga a campanha do Imperio como o --marcos e, em vez de
/// gravar saves, OBSERVA o que o Vinicius reclamou e o placar nao media. So le o nucleo; nada aqui muda o jogo.
///   1. Ajudantes parados (queixas 1 e 6): % do tempo de cada ajudante sem andar e % parado ENQUANTO alguma estacao do distrito estava com
///      fome (produtora aberta, sem trabalhar e com a saida livre): esse segundo numero e' o "parado sem motivo" que o jogador ve.
///   2. Estacoes: % trabalhando, com fome (StarveSeconds) e travada de saida cheia (StallSeconds), por distrito.
///   3. Compras sem efeito sentido (queixas 3 e 7): para cada melhoria comprada, a renda/min (GoldEarned) nos 120 s de jogo antes e depois.
///      Variacao abaixo de 3% = "sem efeito sentido" na economia (luxo conta: desde 2026-11-21 ele mexe no preco).
/// Os numeros sao do bot (o jogador humano anda diferente); servem para achar o problema e conferir a correcao, nao para dar nota E.
/// </summary>
static partial class EconSim
{
    const float JanelaS = 120f, SemEfeito = 0.03f;

    sealed class Olhar
    {
        public string Id;
        public float Seg;
        public readonly Dictionary<string, (float total, float parado, float paradoComFome)> Ajudantes = new Dictionary<string, (float, float, float)>();
        public readonly List<(float t, long ganho)> Renda = new List<(float, long)>();
        public readonly List<(float t, int up)> Compras = new List<(float, int)>();
        public Sim Sim;
    }

    static int Observador(string saida, BotProfile perfil, int horas, float anuncioSeg, string knobs)
    {
        Func<Tuning> tuning = () => Knobs(knobs, new Tuning());
        var imp = new Imperio(new Sim(tuning()), tuning);
        Bot bot = Novo(perfil);
        const float hz = 30f;
        float dt = 1f / hz, relogio = 0f, conferido = 0f, amostra = 0f;
        int ads = 0;
        var olhos = new Dictionary<string, Olhar>();

        Olhar De(Sim s)
        {
            string id = s.Distrito?.Id ?? Distritos.OldForgeId;
            if (!olhos.TryGetValue(id, out Olhar o)) olhos[id] = o = new Olhar { Id = id };
            o.Sim = s;
            return o;
        }

        while (relogio < horas * 3600f)
        {
            Sim s = imp.Ativo;
            bot.Step(s, dt);
            Olhar o = De(s);
            o.Seg += dt;
            foreach (SimEvent e in s.Events) if (e.Kind == Ev.Bought) o.Compras.Add((s.Time, e.A));
            bool fome = s.Stations.Any(x => x.Produces && x.Unlocked && !x.Busy && x.Out < x.OutCap);
            for (int i = 0; i < s.Workers.Count; i++)
            {
                Carrier w = s.Workers[i];
                string nome = Ajudante(w, i);
                o.Ajudantes.TryGetValue(nome, out var a);
                bool parado = !w.Moving;
                o.Ajudantes[nome] = (a.total + dt, a.parado + (parado ? dt : 0f), a.paradoComFome + (parado && fome ? dt : 0f));
            }
            if ((amostra += dt) >= 10f) { amostra = 0f; o.Renda.Add((s.Time, s.GoldEarned)); }
            Bot.Manager(imp);
            imp.Fundo(dt);
            relogio += dt + (bot.AdsTotal - ads) * anuncioSeg; ads = bot.AdsTotal;
            if (relogio - conferido < 10f) continue;   // como o --campanha e o --marcos: o mapa a cada 10 s
            conferido = relogio;
            string id = s.Distrito?.Id ?? Distritos.OldForgeId;
            if (imp.CharterFalta(id) == null) imp.Charter(id);
            string prox = Distritos.Find(id)?.Charter?.Proximo;
            if (prox != null && !imp.Aberto(prox) && imp.Abrir(prox) && imp.Trocar(prox)) { bot = Novo(perfil); ads = 0; }
            if (imp.Abertos.Count == Distritos.All.Length && imp.Abertos.All(x => x.Bought.All(b => b))) break;   // tudo comprado: nada mais a observar
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Bot observador do Forge Street").AppendLine();
        sb.AppendLine($"Perfil {perfil.Name}, ate {horas} h de relogio (anuncio = {anuncioSeg:0.#} s), parou com {relogio / 3600f:0.0} h. So le o nucleo; numeros do bot, nao do jogador.").AppendLine();

        sb.AppendLine("## 1. Ajudantes parados").AppendLine();
        sb.AppendLine("\"Parado com fome\" = sem andar enquanto alguma estacao produtora do distrito estava sem trabalhar com a saida livre.").AppendLine();
        sb.AppendLine("| distrito | ajudante | tempo no distrito (min) | parado | parado com fome |").AppendLine("|---|---|---|---|---|");
        foreach (Olhar o in olhos.Values)
            foreach (var kv in o.Ajudantes.OrderByDescending(k => k.Value.paradoComFome / Math.Max(1f, k.Value.total)))
                sb.AppendLine($"| {o.Id} | {kv.Key} | {kv.Value.total / 60f:0} | {Pct(kv.Value.parado, kv.Value.total)} | {Pct(kv.Value.paradoComFome, kv.Value.total)} |");

        sb.AppendLine().AppendLine("## 2. Estacoes produtoras").AppendLine();
        sb.AppendLine("| distrito | estacao | nivel | trabalhando | com fome | travada (saida cheia) |").AppendLine("|---|---|---|---|---|---|");
        foreach (Olhar o in olhos.Values)
        {
            float t = Math.Max(1f, o.Sim.Time);
            foreach (Station x in o.Sim.Stations.Where(x => x.Produces && x.Unlocked))
            {
                float livre = Math.Max(0f, t - x.StarveSeconds - x.StallSeconds);
                sb.AppendLine($"| {o.Id} | {x.Name} | {x.Level} | {Pct(livre, t)} | {Pct(x.StarveSeconds, t)} | {Pct(x.StallSeconds, t)} |");
            }
        }

        sb.AppendLine().AppendLine($"## 3. Compras e renda ({JanelaS:0} s de jogo antes x depois)").AppendLine();
        sb.AppendLine($"Variacao abaixo de {SemEfeito * 100:0}% = **sem efeito sentido** na renda. Sem janela completa (comprada no fim do distrito) = n/d.").AppendLine();
        sb.AppendLine("| distrito | melhoria | min de jogo | renda/min antes | depois | variacao | |").AppendLine("|---|---|---|---|---|---|---|");
        int semEfeito = 0, medidas = 0;
        foreach (Olhar o in olhos.Values)
            foreach (var (t, up) in o.Compras)
            {
                UpgradeDef d = up >= 0 && up < o.Sim.UpgradeDefs.Length ? o.Sim.UpgradeDefs[up] : null;
                string nome = d == null ? up.ToString(CultureInfo.InvariantCulture) : d.Name + (d.Luxury ? " (luxo)" : "");
                double antes = Taxa(o.Renda, t - JanelaS, t), depois = Taxa(o.Renda, t, t + JanelaS);
                if (antes < 0 || depois < 0) { sb.AppendLine($"| {o.Id} | {nome} | {t / 60f:0} | n/d | n/d | n/d | |"); continue; }
                medidas++;
                double v = antes > 0 ? depois / antes - 1 : (depois > 0 ? 1 : 0);
                bool nada = Math.Abs(v) < SemEfeito;
                if (nada) semEfeito++;
                sb.AppendLine($"| {o.Id} | {nome} | {t / 60f:0} | {antes:0} | {depois:0} | {v * 100:+0;-0;0}% | {(nada ? "**sem efeito sentido**" : "")} |");
            }
        sb.AppendLine().AppendLine($"Resumo: {semEfeito} de {medidas} compras medidas sem efeito sentido na renda.");

        string texto = sb.ToString();
        Console.Write(texto);
        if (!string.IsNullOrEmpty(saida)) File.WriteAllText(saida, texto, new UTF8Encoding(false));
        return 0;
    }

    static string Ajudante(Carrier w, int i) =>
        (w.Wk >= 0 ? "equipe " : "ajudante ") + (w.Role switch { 0 => "minerio", 1 => "lingote", 2 => "produto", 3 => "joalheiro", _ => "?" }) + $" #{i + 1}";

    static string Pct(float parte, float total) => (100f * parte / Math.Max(1e-3f, total)).ToString("0", CultureInfo.InvariantCulture) + "%";

    /// <summary>Ouro/min entre `de` e `ate` (tempo de jogo do distrito) pelas amostras de 10 s; -1 se a janela nao cabe nas amostras.</summary>
    static double Taxa(List<(float t, long ganho)> r, float de, float ate)
    {
        if (r.Count < 2 || de < r[0].t || ate > r[r.Count - 1].t) return -1;
        var a = r.Last(x => x.t <= de); var b = r.First(x => x.t >= ate);
        return b.t > a.t ? (b.ganho - a.ganho) * 60.0 / (b.t - a.t) : -1;
    }
}
