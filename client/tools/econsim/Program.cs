using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS.Core;

/// <summary>
/// econsim (A-CORE-07): perfis x partidas x minutos com o Bot 2.0 sobre o FS.Core das fontes. Grava um CSV por minuto e imprime o resumo
/// (marcos, zona morta, ouro sem destino). Deterministico: o Sim nao sorteia nada, a unica "semente" e' o passo; a partida i de N roda a
/// 30 + 30*i/N ticks/s, fracionario (N = 1: 30, o passo dos BalanceTests; o bot e' caotico, por isso o BALANCE tira media de passos).
/// Uso: dotnet run -c Release --project client/tools/econsim -- --perfis Ideal,F2P,Slow,AdWatcher --minutos 60 --partidas 1 --saida econsim.csv
/// </summary>
static class EconSim
{
    const string Header = "perfil,ticks_s,min,ouro,renda_min,compras_min,upgrades,ouro_sem_destino,ouro_so_luxo,producao_completa_s,encomendas,premio_encomendas,vip_atendidos,anuncios_vip,anuncios_boost";
    static readonly string[] MarcoNome = { "1a venda", "Fole", "Esteira", "Corredor", "Ajud.3", "Joalheria", "Producao", "Fachada" };

    sealed class Run
    {
        public string Perfil, Csv; public float[] Marcos;
        public int Morta, MortaSeq, MortaIni, SemDestinoMin, Luxo, AdsVip, AdsBoost, Vips; public long PosProd, Parado; public double Segundos;
    }

    static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        BotProfile[] perfis = BotProfile.All; int minutos = 60, partidas = 1; string saida = "econsim.csv";
        for (int i = 0; i < args.Length; i += 2)
        {
            string v = i + 1 < args.Length ? args[i + 1] : null;
            bool ok = v != null;
            switch (args[i])
            {
                case "--perfis" when ok: perfis = Array.ConvertAll(v.Split(','), n => Array.Find(BotProfile.All, p => string.Equals(p.Name, n.Trim(), StringComparison.OrdinalIgnoreCase))); break;
                case "--minutos" when ok: ok = int.TryParse(v, out minutos) && minutos > 0; break;
                case "--partidas" when ok: ok = int.TryParse(v, out partidas) && partidas > 0; break;
                case "--saida" when ok: saida = v; break;
                default: ok = false; break;
            }
            if (!ok || perfis.Any(p => p.Name == null))
            {
                Console.Error.WriteLine("uso: econsim [--perfis " + string.Join(",", BotProfile.All.Select(p => p.Name)) + "] [--minutos 60] [--partidas 1] [--saida econsim.csv]");
                return 2;
            }
        }

        var jobs = new List<(BotProfile P, float Hz)>();
        foreach (BotProfile p in perfis) for (int i = 0; i < partidas; i++) jobs.Add((p, 30f + 30f * i / partidas));
        var runs = new Run[jobs.Count];
        var wall = Stopwatch.StartNew();
        Parallel.For(0, jobs.Count, k => runs[k] = Play(jobs[k].P, jobs[k].Hz, minutos));   // cada Sim e' isolado; a ordem do CSV e' a de jobs
        File.WriteAllText(saida, Header + Environment.NewLine + string.Concat(runs.Select(r => r.Csv)));

        Console.WriteLine($"econsim: {perfis.Length} perfis x {partidas} partidas x {minutos} min em {wall.Elapsed.TotalSeconds:0.0} s -> {Path.GetFullPath(saida)}");
        Console.WriteLine("(* = nem toda partida chegou; zona morta = minutos sem compra antes da producao completa; pos-producao = ouro ganho depois dela, so luxo ou nada a comprar)");
        Console.WriteLine($"| perfil | {string.Join(" | ", MarcoNome)} | zona morta (min; maior seq.) | pos-producao | luxo | saldo final | min sem destino | VIP (anuncios VIP/boost) | s/partida |");
        Console.WriteLine("|" + string.Concat(Enumerable.Repeat("---|", MarcoNome.Length + 8)));
        foreach (BotProfile p in perfis)
        {
            Run[] g = runs.Where(r => r.Perfil == p.Name).ToArray();
            var cols = Enumerable.Range(0, MarcoNome.Length).Select(c =>
            {
                float[] v = g.Select(r => r.Marcos[c]).Where(t => t >= 0f).ToArray();
                return v.Length == 0 ? "--:--" : Clock(v.Average()) + (v.Length < g.Length ? "*" : "");
            });
            Run seq = g.OrderByDescending(r => r.MortaSeq).First();
            Console.WriteLine($"| {p.Name} | {string.Join(" | ", cols)} | {g.Average(r => r.Morta):0.#} ({seq.MortaSeq} a partir do min {seq.MortaIni}) | {g.Average(r => r.PosProd):0} | " +
                              $"{g.Average(r => r.Luxo):0.#}/3 | {g.Average(r => r.Parado):0} | {g.Average(r => r.SemDestinoMin):0.#} | {g.Average(r => r.Vips):0.#} ({g.Average(r => r.AdsVip):0.#}/{g.Average(r => r.AdsBoost):0.#}) | {g.Average(r => r.Segundos):0.0} |");
        }
        return 0;
    }

    static Run Play(BotProfile p, float hz, int minutos)
    {
        var clock = Stopwatch.StartNew();
        var s = new Sim(); Bot bot = Bot.From(p); float dt = 1f / hz;
        var r = new Run { Perfil = p.Name };
        var csv = new StringBuilder();
        long earned0 = 0, goldAtProd = -1;
        int bought0 = 0, seq = 0;
        for (int m = 1; m <= minutos; m++)
        {
            while (s.Time < m * 60f)
            {
                int before = s.UpgradesBought;
                bot.Step(s, dt);
                foreach (SimEvent e in s.Events) if (e.Kind == Ev.VipServed) r.Vips++;
                if (goldAtProd < 0 && s.UpgradesBought != before && s.ProductionComplete) goldAtProd = s.Gold;
            }
            long gold = s.Gold, earned = s.GoldEarned;
            int compras = s.UpgradesBought - bought0;
            bool prod = !s.ProductionComplete, luxo = !prod && s.Pads.Exists(pad => pad.Current(s) >= 0);   // depois da producao so o luxo fica a venda
            if (prod && compras == 0) { r.Morta++; if (++seq > r.MortaSeq) { r.MortaSeq = seq; r.MortaIni = m - seq; } } else seq = 0;
            if (!prod && !luxo) r.SemDestinoMin++;
            float tProd = Producao(s);
            csv.AppendLine(string.Join(",", p.Name, hz.ToString("0.###"), m, gold, earned - earned0, compras, s.UpgradesBought, !prod && !luxo ? gold : 0, luxo ? gold : 0,
                tProd < 0f ? "" : tProd.ToString("0.0"), s.OrderCount, s.OrderGold, r.Vips, bot.AdsVip, bot.AdsBoost));
            earned0 = earned; bought0 = s.UpgradesBought;
        }
        float U(Upgrade u) => s.UpgradeTime[(int)u];
        r.Marcos = new[] { s.FirstSaleTime, U(Upgrade.FurnaceSpeed1), U(Upgrade.Conveyor), U(Upgrade.SideCorridor), U(Upgrade.Helper3), U(Upgrade.Jewelry), Producao(s), U(Upgrade.WorkshopFacade) };
        long luxSpent = 0;
        for (int i = 0; i < Upgrades.Count; i++) if (Upgrades.IsLuxury(i) && s.Bought[i]) { r.Luxo++; luxSpent += Upgrades.Cost(i); }
        long paid = s.Pads.Sum(pad => (long)pad.Paid);   // pad de luxo pago pela metade tambem e' ouro gasto
        r.PosProd = goldAtProd < 0 ? 0 : s.Gold + luxSpent + paid - goldAtProd;
        r.Parado = s.Gold; r.AdsVip = bot.AdsVip; r.AdsBoost = bot.AdsBoost; r.Csv = csv.ToString();
        r.Segundos = clock.Elapsed.TotalSeconds;
        return r;
    }

    /// <summary>Quando saiu o ultimo produtivo; -1 = ainda nao.</summary>
    static float Producao(Sim s)
    {
        if (!s.ProductionComplete) return -1f;
        float t = 0f;
        for (int i = 0; i < Upgrades.Count; i++) if (!Upgrades.IsLuxury(i)) t = Math.Max(t, s.UpgradeTime[i]);
        return t;
    }

    static string Clock(float s) => s < 0f ? "--:--" : $"{(int)(s / 60f)}:{(int)(s % 60f):00}";
}
