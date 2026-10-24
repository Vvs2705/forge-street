using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FS.Core;

/// <summary>
/// --campanha H (BALANCE.md §46): o Imperio inteiro do zero, como o jogador. O bot joga o distrito ativo; o Charter sai quando as condicoes
/// fecham (Imperio.CharterFalta, sem Forca); o distrito seguinte abre e vira o ativo assim que o ouro paga a abertura (o bot novo comeca la);
/// o fundo dos outros e os managers andam como no --distrito. Mede o relogio (jogo + anuncios, V19) do Renome 1..10 e de cada Charter, para
/// comparar com as h acumuladas do CONTEUDO_V1 §1. Perfis: os pedidos (--perfis) e o "Pagante" (o F2P que golpeia, assiste tudo e tem o
/// Pacote do Mestre: Velocidade e Supply sem video).
/// </summary>
static partial class EconSim
{
    static readonly BotProfile Pagante = new BotProfile { Name = "Pagante", Reaction = 0.7f, Stick = 0.85f, Ads = true, AdsE = true, Strike = true, StrikeCada = 2 };

    sealed class RunC { public string Perfil; public float[] Renome = Enumerable.Repeat(-1f, 11).ToArray(); public Dictionary<string, float> Charter = new Dictionary<string, float>(); public string Fim; }

    static int Campanha(BotProfile[] perfis, int horas, int partidas, float anuncioSeg, string knobs)
    {
        var ps = perfis.Concat(new[] { Pagante }).ToArray();
        var jobs = new List<(BotProfile P, float Hz)>();
        foreach (BotProfile p in ps) for (int i = 0; i < partidas; i++) jobs.Add((p, 30f + 30f * i / partidas));
        var runs = new RunC[jobs.Count];
        Parallel.For(0, jobs.Count, k => runs[k] = PlayC(jobs[k].P, jobs[k].Hz, horas, anuncioSeg, () => Knobs(knobs, new Tuning())));
        string H(float s) => s < 0f ? "--" : (s / 3600f).ToString("0.0");
        Console.WriteLine($"econsim --campanha: {ps.Length} perfis x {partidas} partidas x {horas} h de relogio (anuncio = {anuncioSeg:0.#} s; '--' = nao chegou)");
        Console.WriteLine("| perfil | " + string.Join(" | ", Enumerable.Range(1, 10).Select(n => $"Renome {n} (h)")) + " | onde parou |");
        Console.WriteLine("|" + string.Concat(Enumerable.Repeat("---|", 12)));
        foreach (BotProfile p in ps)
        {
            RunC[] g = runs.Where(r => r.Perfil == p.Name).ToArray();
            Console.WriteLine($"| {p.Name} | " + string.Join(" | ", Enumerable.Range(1, 10).Select(n => Media(g.Select(r => r.Renome[n]), g.Length, H))) +
                              $" | {string.Join("; ", g.Select(r => r.Fim).Distinct())} |");
        }
        Console.WriteLine();
        Console.WriteLine("| perfil | " + string.Join(" | ", Distritos.All.Select(d => $"Charter {d.Id} (h)")) + " |");
        Console.WriteLine("|" + string.Concat(Enumerable.Repeat("---|", Distritos.All.Length + 1)));
        foreach (BotProfile p in ps)
        {
            RunC[] g = runs.Where(r => r.Perfil == p.Name).ToArray();
            Console.WriteLine($"| {p.Name} | " + string.Join(" | ", Distritos.All.Select(d => Media(g.Select(r => r.Charter.TryGetValue(d.Id, out float t) ? t : -1f), g.Length, H))) + " |");
        }
        return 0;
    }

    /// <summary>Media de quem chegou; "*" = nem toda partida chegou.</summary>
    static string Media(IEnumerable<float> v, int n, Func<float, string> f)
    {
        float[] a = v.Where(x => x >= 0f).ToArray();
        return a.Length == 0 ? "--" : f(a.Average()) + (a.Length < n ? "*" : "");
    }

    static RunC PlayC(BotProfile p, float hz, int horas, float anuncioSeg, Func<Tuning> tuning)
    {
        var imp = new Imperio(new Sim(tuning()), tuning);
        var r = new RunC { Perfil = p.Name };
        bool pacote = p.Name == Pagante.Name;   // E-PLAT-05: Velocidade e Supply sem o video
        Bot bot = Novo(p);
        float dt = 1f / hz, relogio = 0f, conferido = 0f;
        int ads = 0;
        while (relogio < horas * 3600f)
        {
            Sim s = imp.Ativo;
            bot.Step(s, dt);
            Bot.Manager(imp);
            imp.Fundo(dt);
            int vistos = pacote ? bot.AdsVip + bot.AdsBonus : bot.AdsTotal;
            relogio += dt + (vistos - ads) * anuncioSeg; ads = vistos;
            for (int n = 1; n <= 10; n++) if (r.Renome[n] < 0f && imp.Raiz.Renome.Nivel >= n) r.Renome[n] = relogio;
            if (relogio - conferido < 10f) continue;   // Charter e abertura: a cada 10 s de relogio, como um jogador que olha o mapa
            conferido = relogio;
            string id = s.Distrito?.Id ?? Distritos.OldForgeId;
            if (imp.CharterFalta(id) == null && imp.Charter(id)) r.Charter[id] = relogio;
            string prox = Distritos.Find(id)?.Charter?.Proximo;
            if (prox != null && !imp.Aberto(prox) && imp.Abrir(prox) && imp.Trocar(prox)) { bot = Novo(p); ads = 0; }
        }
        string at = imp.Ativo.Distrito?.Id ?? Distritos.OldForgeId;
        r.Fim = at + (imp.CharterFalta(at) is string f && f != "concedido" ? " (falta " + f + ")" : "");
        return r;
    }

    static Bot Novo(BotProfile p) { Bot b = Bot.From(p); b.Altares = _altares; return b; }
}
