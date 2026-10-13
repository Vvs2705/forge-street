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
/// V19: tudo em tempo de RELOGIO. Cada anuncio assistido (AdWatcher) gasta --anuncio-seg s de relogio com o jogo parado, como no aparelho:
/// os minutos do CSV, os marcos e a tabela de pisos do §3 contam esse tempo. --anuncio-seg 0 = o tempo de jogo (o econsim de antes).
/// Uso: dotnet run -c Release --project client/tools/econsim -- --perfis Ideal,F2P,Slow,AdWatcher --minutos 60 --partidas 1 --anuncio-seg 30 --saida econsim.csv
/// B-CORE-03: --tuning "GolpeRecarga=30;NivelEstacaoQ=25" troca knobs do Tuning (varredura sem recompilar); a 3a tabela mostra o premio de
/// qualidade, o golpe e os niveis de estacao.
/// </summary>
static class EconSim
{
    const string Header = "perfil,ticks_s,min,ouro,renda_min,compras_min,upgrades,ouro_sem_destino,ouro_so_luxo,producao_completa_s,encomendas,premio_encomendas,vip_atendidos,anuncios_vip,anuncios_boost";
    static readonly string[] MarcoNome = { "1a venda", "Fole", "Esteira", "Corredor", "Ajud.3", "Joalheria", "Producao", "Fachada" };
    /// <summary>Pisos do §3 que o BalanceTests.Bot_Primeiros10Minutos cobra (30% do GDD no Fole, 50% nos demais), em s.</summary>
    static readonly (string Nome, Upgrade U, float Piso)[] Pisos =
    {
        ("Fole", Upgrade.FurnaceSpeed1, 45f), ("2a bigorna", Upgrade.Anvil2, 105f), ("Ajudante", Upgrade.Helper1, 135f),
        ("Escudos", Upgrade.Shields, 165f), ("Esteira", Upgrade.Conveyor, 255f),
    };

    sealed class Run
    {
        public string Perfil, Csv; public float[] Marcos, Pisos;
        public int Morta, MortaSeq, MortaIni, SemDestinoMin, Luxo, AdsVip, AdsBoost, AdsSupply, AdsBonus, Vips; public long PosProd, Parado, Encomendas; public double Segundos;
        // B-CORE-03: % da receita acima do preco Normal (premio de qualidade) e o que o golpe somou; niveis de estacao comprados, gasto e o 1o
        public double Quali, Golpe; public int Niveis, Golpes; public long NiveisGasto; public float Nivel1;
    }

    /// <summary>--tuning "GolpeRecarga=30;NivelEstacaoCusto=500|1500|650|1900|500|1500|1000|3000": campos publicos do Tuning
    /// (int, long, float, bool e listas deles com |) por cima do padrao, para varrer knob sem recompilar. Campo ou valor ruim = null.</summary>
    static Tuning Knobs(string spec, Tuning t)
    {
        if (string.IsNullOrEmpty(spec)) return t;
        foreach (string kv in spec.Split(';'))
        {
            string[] p = kv.Split('=');
            System.Reflection.FieldInfo f = p.Length == 2 ? typeof(Tuning).GetField(p[0].Trim()) : null;
            if (f == null) return null;
            Type el = f.FieldType.IsArray ? f.FieldType.GetElementType() : f.FieldType;
            try
            {
                object[] vals = p[1].Split('|').Select(x => Convert.ChangeType(x.Trim(), el, CultureInfo.InvariantCulture)).ToArray();
                if (!f.FieldType.IsArray) f.SetValue(t, vals[0]);
                else { var a = Array.CreateInstance(el, vals.Length); for (int i = 0; i < vals.Length; i++) a.SetValue(vals[i], i); f.SetValue(t, a); }
            }
            catch (FormatException) { return null; }
        }
        return t;
    }

    static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        BotProfile[] perfis = BotProfile.All; int minutos = 60, partidas = 1; string saida = "econsim.csv", knobs = null;
        float anuncioSeg = 30f;   // V19: ~30 s por anuncio premiado (o LevelPlay nao mostra o tempo; medir no POCO e ajustar aqui)
        for (int i = 0; i < args.Length; i += 2)
        {
            string v = i + 1 < args.Length ? args[i + 1] : null;
            bool ok = v != null;
            switch (args[i])
            {
                case "--tuning" when ok: ok = Knobs(v, new Tuning()) != null; knobs = v; break;
                case "--perfis" when ok: perfis = Array.ConvertAll(v.Split(','), n => Array.Find(BotProfile.All, p => string.Equals(p.Name, n.Trim(), StringComparison.OrdinalIgnoreCase))); break;
                case "--minutos" when ok: ok = int.TryParse(v, out minutos) && minutos > 0; break;
                case "--partidas" when ok: ok = int.TryParse(v, out partidas) && partidas > 0; break;
                case "--anuncio-seg" when ok: ok = float.TryParse(v, out anuncioSeg) && anuncioSeg >= 0f; break;
                case "--saida" when ok: saida = v; break;
                default: ok = false; break;
            }
            if (!ok || perfis.Any(p => p.Name == null))
            {
                Console.Error.WriteLine("uso: econsim [--perfis " + string.Join(",", BotProfile.All.Select(p => p.Name)) + "] [--minutos 60] [--partidas 1] [--anuncio-seg 30] [--saida econsim.csv] [--tuning Knob=v;Lista=1|2|3]");
                return 2;
            }
        }

        var jobs = new List<(BotProfile P, float Hz)>();
        foreach (BotProfile p in perfis) for (int i = 0; i < partidas; i++) jobs.Add((p, 30f + 30f * i / partidas));
        var runs = new Run[jobs.Count];
        var wall = Stopwatch.StartNew();
        Parallel.For(0, jobs.Count, k => runs[k] = Play(jobs[k].P, jobs[k].Hz, minutos, anuncioSeg, Knobs(knobs, new Tuning())));   // cada Sim e' isolado (Tuning proprio); a ordem do CSV e' a de jobs
        if (knobs != null) Console.WriteLine("tuning: " + knobs);
        File.WriteAllText(saida, Header + Environment.NewLine + string.Concat(runs.Select(r => r.Csv)));

        Console.WriteLine($"econsim: {perfis.Length} perfis x {partidas} partidas x {minutos} min de relogio (anuncio = {anuncioSeg:0.#} s) em {wall.Elapsed.TotalSeconds:0.0} s -> {Path.GetFullPath(saida)}");
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
        // B-CORE-03 (ECONOMIA_WAVE_B §10): premio de qualidade e golpe em % da receita de vendas; niveis de estacao (ralo Meta)
        Console.WriteLine();
        Console.WriteLine("| perfil | premio de qualidade (% da receita) | golpe de mestre (% da receita; golpes) | niveis de estacao (comprados/8; gasto; 1o) |");
        Console.WriteLine("|---|---|---|---|");
        foreach (BotProfile p in perfis)
        {
            Run[] g = runs.Where(r => r.Perfil == p.Name).ToArray();
            float n1 = g.Where(r => r.Nivel1 >= 0f).Select(r => r.Nivel1).DefaultIfEmpty(-1f).Average();
            Console.WriteLine($"| {p.Name} | {g.Average(r => r.Quali):0.0}% | {g.Average(r => r.Golpe):0.0}% ({g.Average(r => r.Golpes):0}) | {g.Average(r => r.Niveis):0.#} ({g.Average(r => r.NiveisGasto):0}; {Clock(n1)}) |");
        }
        // V19: pisos do §3 em relogio; % = partidas que compraram ANTES do piso (o BalanceTests reprova) ou nao compraram
        Console.WriteLine();
        Console.WriteLine($"| perfil | {string.Join(" | ", Pisos.Select(x => $"{x.Nome} (piso {Clock(x.Piso)})"))} |");
        Console.WriteLine("|" + string.Concat(Enumerable.Repeat("---|", Pisos.Length + 1)));
        foreach (BotProfile p in perfis)
        {
            Run[] g = runs.Where(r => r.Perfil == p.Name).ToArray();
            Console.WriteLine($"| {p.Name} | " + string.Join(" | ", Pisos.Select((x, c) =>
            {
                float[] v = g.Select(r => r.Pisos[c]).Where(t => t >= 0f).ToArray();
                int abaixo = g.Count(r => r.Pisos[c] < x.Piso);
                return (v.Length == 0 ? "--:--" : Clock(v.Average())) + $" ({100.0 * abaixo / g.Length:0}% abaixo)";
            })) + " |");
        }
        // E-PLAT-02: so os perfis que assistem; o teto e' por dia (o bot joga 1 dia: Metas.Agora nunca roda)
        BotProfile[] comAds = perfis.Where(p => p.Ads).ToArray();
        if (comAds.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"| perfil | anuncios VIP | Velocidade | Supply Drop | bonus da encomenda | total (teto {Knobs(knobs, new Tuning()).AnunciosTetoDia}/dia) | premio das encomendas |");
            Console.WriteLine("|---|---|---|---|---|---|---|");
            foreach (BotProfile p in comAds)
            {
                Run[] g = runs.Where(r => r.Perfil == p.Name).ToArray();
                Console.WriteLine($"| {p.Name} | {g.Average(r => r.AdsVip):0.#} | {g.Average(r => r.AdsBoost):0.#} | {g.Average(r => r.AdsSupply):0.#} | {g.Average(r => r.AdsBonus):0.#} | " +
                                  $"{g.Average(r => r.AdsVip + r.AdsBoost + r.AdsSupply + r.AdsBonus):0.#} | {g.Average(r => r.Encomendas):0} |");
            }
        }
        return 0;
    }

    static Run Play(BotProfile p, float hz, int minutos, float anuncioSeg, Tuning t)
    {
        var clock = Stopwatch.StartNew();
        var s = new Sim(t); Bot bot = Bot.From(p); float dt = 1f / hz;
        var r = new Run { Perfil = p.Name };
        var csv = new StringBuilder();
        long earned0 = 0, goldAtProd = -1;
        int bought0 = 0, seq = 0, ads = 0;
        // V19: relogio = s.Time + pausa (s de anuncio, jogo parado); `rel` = instante de relogio de cada compra e da 1a venda (-1 = ainda nao)
        float pausa = 0f, venda1 = -1f;
        var rel = new float[Upgrades.Count];
        for (int i = 0; i < rel.Length; i++) rel[i] = -1f;
        for (int m = 1; m <= minutos; m++)
        {
            while (s.Time + pausa < m * 60f)
            {
                int before = s.UpgradesBought;
                bot.Step(s, dt);
                pausa += (bot.AdsTotal - ads) * anuncioSeg;   // o anuncio vem antes do Tick do mesmo passo
                ads = bot.AdsTotal;
                foreach (SimEvent e in s.Events) if (e.Kind == Ev.VipServed) r.Vips++;
                if (venda1 < 0f && s.FirstSaleTime >= 0f) venda1 = s.FirstSaleTime + pausa;
                if (s.UpgradesBought != before)
                    for (int i = 0; i < rel.Length; i++) if (s.Bought[i] && rel[i] < 0f) rel[i] = s.UpgradeTime[i] + pausa;
                if (goldAtProd < 0 && s.UpgradesBought != before && s.ProductionComplete) goldAtProd = s.Gold;
            }
            long gold = s.Gold, earned = s.GoldEarned;
            int compras = s.UpgradesBought - bought0;
            // depois da producao so ralo fica a venda: luxo (pad) ou Meta (menu, B-CORE-03); "ouro_so_luxo" = ouro com um ralo a venda
            bool prod = !s.ProductionComplete, luxo = !prod && (s.Pads.Exists(pad => pad.Current(s) >= 0) || Enumerable.Range(0, Upgrades.Count).Any(s.MenuAvailable));
            if (prod && compras == 0) { r.Morta++; if (++seq > r.MortaSeq) { r.MortaSeq = seq; r.MortaIni = m - seq; } } else seq = 0;
            if (!prod && !luxo) r.SemDestinoMin++;
            float tProd = Producao(s, rel);
            csv.AppendLine(string.Join(",", p.Name, hz.ToString("0.###"), m, gold, earned - earned0, compras, s.UpgradesBought, !prod && !luxo ? gold : 0, luxo ? gold : 0,
                tProd < 0f ? "" : tProd.ToString("0.0"), s.OrderCount, s.OrderGold, r.Vips, bot.AdsVip, bot.AdsBoost));
            earned0 = earned; bought0 = s.UpgradesBought;
        }
        float U(Upgrade u) => rel[(int)u];
        r.Marcos = new[] { venda1, U(Upgrade.FurnaceSpeed1), U(Upgrade.Conveyor), U(Upgrade.SideCorridor), U(Upgrade.Helper3), U(Upgrade.Jewelry), Producao(s, rel), U(Upgrade.WorkshopFacade) };
        r.Pisos = Array.ConvertAll(Pisos, x => U(x.U));
        long luxSpent = 0;
        float tp = Producao(s, rel);
        for (int i = 0; i < Upgrades.Count; i++)   // ralo gasto depois da producao: luxo e Meta (B-CORE-03) comprados dali em diante
        {
            if (Upgrades.IsLuxury(i) && s.Bought[i]) r.Luxo++;
            if (!Upgrades.IsProduction(i) && s.Bought[i] && tp >= 0f && rel[i] >= tp) luxSpent += s.T.Cost(i);
        }
        r.Nivel1 = -1f;
        for (int i = 0; i < Upgrades.Count; i++)
            if (Upgrades.All[i].Meta && s.Bought[i]) { r.Niveis++; r.NiveisGasto += s.T.Cost(i); r.Nivel1 = r.Nivel1 < 0f ? rel[i] : Math.Min(r.Nivel1, rel[i]); }
        r.Quali = 100.0 * s.QualityGold / Math.Max(1, s.GoldEarned);
        r.Golpe = 100.0 * s.StrikeGold / Math.Max(1, s.GoldEarned);
        r.Golpes = s.Strikes;
        long paid = s.Pads.Sum(pad => (long)pad.Paid);   // pad de luxo pago pela metade tambem e' ouro gasto
        r.PosProd = goldAtProd < 0 ? 0 : s.Gold + luxSpent + paid - goldAtProd;
        r.Parado = s.Gold; r.AdsVip = bot.AdsVip; r.AdsBoost = bot.AdsBoost; r.AdsSupply = bot.AdsSupply; r.AdsBonus = bot.AdsBonus; r.Csv = csv.ToString();
        r.Encomendas = s.OrderGold;
        r.Segundos = clock.Elapsed.TotalSeconds;
        return r;
    }

    /// <summary>Quando saiu o ultimo produtivo, no relogio de `rel`; -1 = ainda nao.</summary>
    static float Producao(Sim s, float[] rel)
    {
        if (!s.ProductionComplete) return -1f;
        float t = 0f;
        for (int i = 0; i < Upgrades.Count; i++) if (Upgrades.IsProduction(i)) t = Math.Max(t, rel[i]);
        return t;
    }

    static string Clock(float s) => s < 0f ? "--:--" : $"{(int)(s / 60f)}:{(int)(s % 60f):00}";
}
