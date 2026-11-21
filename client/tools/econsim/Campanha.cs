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

    sealed class RunC
    {
        public string Perfil; public float[] Renome = Enumerable.Repeat(-1f, 11).ToArray(); public Dictionary<string, float> Charter = new Dictionary<string, float>(); public string Fim;
        // Ritmo por distrito: relogio da producao completa, segundos de relogio em cada condicao que faltava ao Charter depois dela
        // (renome / masterwork / contrato) e o ouro na carteira quando o Charter saiu (o que sobrou sem destino)
        public Dictionary<string, float> Prod = new Dictionary<string, float>(), Chegou = new Dictionary<string, float>();
        public Dictionary<string, Dictionary<string, float>> Falta = new Dictionary<string, Dictionary<string, float>>();
        public Dictionary<string, long> OuroCharter = new Dictionary<string, long>();
        // BALANCE §47: no fim de cada dia do F2P de referencia (20 min de relogio, LIVEOPS_V1 §1), somando os distritos abertos
        public int[] Bp = new int[Dias.Length], BpTot = new int[Dias.Length], Col = new int[Dias.Length], ColTot = new int[Dias.Length], Mw = new int[Dias.Length];
        // BALANCE §53: producao completa de cada distrito (h), ouro na carteira e degraus das Obras no Charter; no fim, as Obras e a carteira
        public Dictionary<string, float> Producao = new Dictionary<string, float>();
        public Dictionary<string, int> ObrasCharter = new Dictionary<string, int>();
        public int Degraus, Bonus; public long Gasto, RenomeObras, OuroFim;
    }

    /// <summary>BALANCE §53: false (--obras 0) = a campanha sem as Obras do Reino (o antes, no mesmo build).</summary>
    static bool _obras = true;

    static readonly int[] Dias = { 1, 2, 3, 5, 7, 10, 14, 21, 28, 35 };
    const float SegDia = 20f * 60f;

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
        // BALANCE §53: a espera entre a producao completa e o Charter, o ouro parado na carteira no Charter e as Obras do Reino
        string Ouro(double v) => v >= 1e6 ? (v / 1e6).ToString("0.0") + " mi" : v >= 1e3 ? (v / 1e3).ToString("0") + " mil" : v.ToString("0");
        Console.WriteLine();
        Console.WriteLine($"Obras do Reino: {(_obras ? "ligadas (Bot.Obras)" : "DESLIGADAS (--obras 0)")}. Por distrito: producao completa -> Charter (h), espera (h), ouro na carteira no Charter e degraus das Obras no Charter (media das partidas que chegaram)");
        Console.WriteLine("| perfil | " + string.Join(" | ", Distritos.All.Select(d => d.Id)) + " | no fim: degraus / ouro gasto nas obras / Renome delas / bonus de fundo / carteira |");
        Console.WriteLine("|" + string.Concat(Enumerable.Repeat("---|", Distritos.All.Length + 2)));
        foreach (BotProfile p in ps)
        {
            RunC[] g = runs.Where(r => r.Perfil == p.Name).ToArray();
            string Cel(DistrictDef d)
            {
                RunC[] c = g.Where(r => r.Charter.ContainsKey(d.Id) && r.Producao.ContainsKey(d.Id)).ToArray();
                if (c.Length == 0) return "--";
                return $"{H(c.Average(r => r.Producao[d.Id]))}->{H(c.Average(r => r.Charter[d.Id]))} ({H(c.Average(r => r.Charter[d.Id] - r.Producao[d.Id]))}); {Ouro(c.Average(r => (double)r.OuroCharter[d.Id]))}; {c.Average(r => r.ObrasCharter[d.Id]):0.#}";
            }
            Console.WriteLine($"| {p.Name} | " + string.Join(" | ", Distritos.All.Select(Cel)) +
                              $" | {g.Average(r => r.Degraus):0.#} / {Ouro(g.Average(r => (double)r.Gasto))} / {g.Average(r => r.RenomeObras):0} / +{g.Average(r => r.Bonus):0}% / {Ouro(g.Average(r => (double)r.OuroFim))} |");
        }
        // Ritmo: h no distrito (chegada -> Charter), producao completa, o que segurou o Charter depois dela e o ouro parado no Charter
        foreach (BotProfile p in ps)
        {
            RunC[] g = runs.Where(r => r.Perfil == p.Name).ToArray();
            Console.WriteLine();
            Console.WriteLine($"{p.Name}, ritmo por distrito (media de quem chegou ao Charter; h de relogio):");
            Console.WriteLine("| distrito | h no distrito | producao completa (h apos chegar) | esperando Renome (h) | esperando obra/Superiores (h) | esperando contrato (h) | ouro na carteira no Charter |");
            Console.WriteLine("|---|---|---|---|---|---|---|");
            foreach (DistrictDef d in Distritos.All)
            {
                RunC[] c = g.Where(r => r.Charter.ContainsKey(d.Id) && r.Chegou.ContainsKey(d.Id)).ToArray();
                if (c.Length == 0) continue;
                float F(RunC r, string k) => r.Falta.TryGetValue(d.Id, out var m) && m.TryGetValue(k, out float v) ? v : 0f;
                string Hh(Func<RunC, float> f) => (c.Average(f) / 3600f).ToString("0.0");
                Console.WriteLine($"| {d.Id} | {Hh(r => r.Charter[d.Id] - r.Chegou[d.Id])}{(c.Length < g.Length ? "*" : "")} | {Hh(r => r.Prod.TryGetValue(d.Id, out float t) ? t - r.Chegou[d.Id] : 0f)} | {Hh(r => F(r, "renome"))} | {Hh(r => F(r, "masterwork"))} | {Hh(r => F(r, "contrato"))} | {c.Average(r => (double)r.OuroCharter[d.Id]):N0} |");
            }
        }
        // BALANCE §47: Blueprints (posse / os dos distritos abertos), marcos da Colecao pagos / os dos abertos e obras no Hall, por dia de 20 min
        foreach (BotProfile p in ps)
        {
            RunC[] g = runs.Where(r => r.Perfil == p.Name).ToArray();
            Console.WriteLine();
            Console.WriteLine($"{p.Name}, por dia de 20 min (media das partidas; so os dias dentro das {horas} h):");
            Console.WriteLine("| dia | Blueprints (posse / dos abertos) | Colecao (marcos pagos / dos abertos) | obras no Hall |");
            Console.WriteLine("|---|---|---|---|");
            for (int k = 0; k < Dias.Length && Dias[k] * SegDia <= horas * 3600f; k++)
                Console.WriteLine($"| {Dias[k]} | {g.Average(r => r.Bp[k]):0.#} / {g.Average(r => r.BpTot[k]):0} | {g.Average(r => r.Col[k]):0.#} / {g.Average(r => r.ColTot[k]):0} | {g.Average(r => r.Mw[k]):0.#} |");
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
        int ads = 0, dia = 0;
        while (relogio < horas * 3600f)
        {
            if (dia < Dias.Length && relogio >= Dias[dia] * SegDia)
            {
                foreach (Sim d in imp.Abertos)
                {
                    r.Bp[dia] += d.Projetos.Count; r.BpTot[dia] += d.Projetos.Defs.Length; r.Mw[dia] += d.Hall.Count;
                    r.ColTot[dia] += d.Colecao.Marcos.Length; r.Col[dia] += d.Colecao.Marcos.Count(d.Colecao.Pago);
                }
                dia++;
            }
            Sim s = imp.Ativo;
            bot.Step(s, dt);
            Bot.Manager(imp);
            if (_obras) Bot.Obras(imp);   // BALANCE §53: o ouro que sobra, quando nada mais esta a venda
            imp.Fundo(dt);
            string at0 = s.Distrito?.Id ?? Distritos.OldForgeId;
            if (!r.Producao.ContainsKey(at0) && s.ProductionComplete) r.Producao[at0] = relogio;
            int vistos = pacote ? bot.AdsVip + bot.AdsBonus : bot.AdsTotal;
            relogio += dt + (vistos - ads) * anuncioSeg; ads = vistos;
            for (int n = 1; n <= 10; n++) if (r.Renome[n] < 0f && imp.Raiz.Renome.Nivel >= n) r.Renome[n] = relogio;
            if (relogio - conferido < 10f) continue;   // Charter e abertura: a cada 10 s de relogio, como um jogador que olha o mapa
            conferido = relogio;
            string id = s.Distrito?.Id ?? Distritos.OldForgeId;
            if (!r.Chegou.ContainsKey(id)) r.Chegou[id] = 0f;
            if (!r.Prod.ContainsKey(id) && s.ProductionComplete) r.Prod[id] = relogio;
            string falta = imp.CharterFalta(id);
            if (falta == "renome" || falta == "masterwork" || falta == "contrato")
            {
                if (!r.Falta.TryGetValue(id, out var m)) r.Falta[id] = m = new Dictionary<string, float>();
                m[falta] = (m.TryGetValue(falta, out float v) ? v : 0f) + 10f;
            }
            if (falta == null && imp.Charter(id)) { r.Charter[id] = relogio; r.OuroCharter[id] = s.Gold; r.ObrasCharter[id] = imp.Obras.Degraus; }
            string prox = Distritos.Find(id)?.Charter?.Proximo;
            if (prox != null && !imp.Aberto(prox) && imp.Abrir(prox) && imp.Trocar(prox)) { bot = Novo(p); ads = 0; r.Chegou[prox] = relogio; }
        }
        string at = imp.Ativo.Distrito?.Id ?? Distritos.OldForgeId;
        r.Degraus = imp.Obras.Degraus; r.Gasto = imp.Obras.Gasto; r.Bonus = imp.Obras.BonusPct; r.OuroFim = imp.Ativo.Gold;
        for (int n = 0; n < r.Degraus; n++) r.RenomeObras += Obras.RenomeDe(n);
        r.Fim = at + (imp.CharterFalta(at) is string f && f != "concedido" ? " (falta " + f + ")" : "");
        return r;
    }

    static Bot Novo(BotProfile p) { Bot b = Bot.From(p); b.Altares = _altares; return b; }
}
