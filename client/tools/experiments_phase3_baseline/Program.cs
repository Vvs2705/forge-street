using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using FS.Core;

static class Program
{
    const float Dt = 1f / 30f, Start = 41f * 60f, End = 60f * 60f;
    static string ExpectedSignature;
    static readonly List<Result> Results = new List<Result>();
    static StreamWriter MinuteCsv;
    static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = Ci;
        string output = Path.GetFullPath(args.Length > 0 ? args[0] : "Builds/validation_phase3");
        Directory.CreateDirectory(output);
        using (MinuteCsv = new StreamWriter(Path.Combine(output, "logistics_minutes.csv"), false, new UTF8Encoding(false)))
        {
            MinuteCsv.WriteLine("scenario,minute,sim_time,gold_earned,jewels_sold,swords_sold,shields_sold,tools_sold,jewel_starve_s,jewel_stall_s,furnace_a_in,furnace_a_out,furnace_b_in,furnace_b_out,buffer,transport_delivered");
            foreach (string name in new[] { "control", "belt_2s", "belt_3s", "belt_4s", "second_jeweler", "lateral_furnace", "control_repeat" })
                Results.Add(Run(name));
        }
        var control = Results[0];
        var repeat = Results.Last();
        Require(control.FinalSignature == repeat.FinalSignature, "Controle repetido divergiu.");
        Require(control.EndGoldEarned == 46377 && control.EndJewels == 269, "Controle divergiu da baseline publicada de 60 min.");
        string summaryHeader = "scenario,window_seconds,start_signature,final_signature,jewel_starve_pct,jewel_stall_pct,jewels_per_min,gold_per_min,other_sales_per_min,swords_per_min,shields_per_min,tools_per_min,jewel_change_pct,gold_change_pct,other_change_pct,gate,furnace_a_in_avg,furnace_a_out_avg,furnace_a_empty_out_pct,furnace_a_starve_pct,furnace_a_stall_pct,furnace_b_in_avg,furnace_b_out_avg,furnace_b_empty_out_pct,furnace_b_starve_pct,furnace_b_stall_pct,jewel_starve_with_ingot_source_pct,walk_pct,lateral_in_avg,lateral_out_avg,lateral_starve_pct,lateral_stall_pct,ingot_produced_per_min,ingot_transported,end_buffer,end_gold_earned,end_jewels";
        using (var summary = new StreamWriter(Path.Combine(output, "logistics_summary.csv"), false, new UTF8Encoding(false)))
        using (var log = new StreamWriter(Path.Combine(output, "logistics.log"), false, new UTF8Encoding(false)))
        {
            summary.WriteLine(summaryHeader);
            log.WriteLine("Frozen baseline; dt=1/30; identical complete Sim+Bot signature before every intervention; replay, no Save/Load clone.");
            log.WriteLine("Matched legacy control: GoldEarned=46377; jewels=269. Exact final control-repeat signature matches. Ingot and product conservation checked every tick.");
            foreach (var r in Results)
            {
                double j = 100 * (r.JewelsPerMin / control.JewelsPerMin - 1), g = 100 * (r.GoldPerMin / control.GoldPerMin - 1), o = 100 * (r.OtherPerMin / control.OtherPerMin - 1);
                bool pass = r.StarvePct <= 35 && j >= 15 && g >= 0 && o >= -5;
                summary.WriteLine(string.Join(",", new object[] { r.Name, F(r.Seconds), r.StartSignature, r.FinalSignature, F(r.StarvePct), F(r.StallPct), F(r.JewelsPerMin), F(r.GoldPerMin), F(r.OtherPerMin), F(r.Sold[2] / r.Minutes), F(r.Sold[3] / r.Minutes), F(r.Sold[4] / r.Minutes), F(j), F(g), F(o), pass ? "PASS" : "FAIL", F(r.AIn / r.N), F(r.AOut / r.N), F(100 * r.AEmpty / r.N), F(r.AStarve), F(r.AStall), F(r.BIn / r.N), F(r.BOut / r.N), F(100 * r.BEmpty / r.N), F(r.BStarve), F(r.BStall), F(r.JewelStarveSamples == 0 ? 0 : 100 * r.StarveSourceAvailable / r.JewelStarveSamples), F(r.WalkPct), F(r.LIn / r.N), F(r.LOut / r.N), F(r.LStarve), F(r.LStall), F(r.IngotsMade / r.Minutes), r.Delivered, r.Buffer, r.EndGoldEarned, r.EndJewels }));
                log.WriteLine($"{r.Name}: famine={F(r.StarvePct)}% jewels/min={F(r.JewelsPerMin)} ({F(j)}%) gold/min={F(r.GoldPerMin)} ({F(g)}%) other/min={F(r.OtherPerMin)} ({F(o)}%) gate={(pass ? "PASS" : "FAIL")} hash41={r.StartSignature} hash60={r.FinalSignature}");
            }
            log.WriteLine("No RNG; repeat is reproducibility, not independent population evidence. No Unity/player/human validation.");
        }
        foreach (var r in Results) Console.WriteLine($"{r.Name}: famine={F(r.StarvePct)}% jewels/min={F(r.JewelsPerMin)} gold/min={F(r.GoldPerMin)} other/min={F(r.OtherPerMin)}");
        Console.WriteLine("EXPERIMENT OK: signatures, baseline, conservation, capacity, no extra upgrades/offline; all scenarios completed.");
    }

    static Result Run(string name)
    {
        var s = new Sim(); var bot = new Bot();
        while (s.Time < Start) bot.Step(s, Dt);
        string signature = Signature(s, bot);
        if (ExpectedSignature == null) ExpectedSignature = signature;
        Require(signature == ExpectedSignature, "Estado/timers/bot aos 41 minutos divergem: " + name);
        Require(s.UpgradesBought == 20 && s.Bought.All(x => x) && s.PriceOf(Item.Jewel) == 80, "Contrato da baseline nao satisfeito.");
        var r = new Result { Name = name, StartSignature = signature, StartTime = s.Time };
        var initialSold = (int[])s.SoldItems.Clone();
        int initialEarned = s.GoldEarned, initialIngots = s.Crafted[(int)Item.Ingot], initialOffline = s.OfflineEarned;
        double initialStarve = s.JewelBench.StarveSeconds, initialStall = s.JewelBench.StallSeconds, initialWalk = s.WalkNoDecision;
        double aStarve = s.FurnaceA.StarveSeconds, aStall = s.FurnaceA.StallSeconds, bStarve = s.FurnaceB.StarveSeconds, bStall = s.FurnaceB.StallSeconds;
        Station lateral = null;
        var belt = name.StartsWith("belt_") ? new Belt(float.Parse(name.Substring(5, 1), Ci)) : null;
        if (name == "second_jeweler") s.Workers.Add(new Carrier { Pos = Sim.HireSpot, Role = 3, Speed = Balance.WorkerSpeedUp, Cap = Balance.WorkerCapUp });
        if (name == "lateral_furnace")
        {
            lateral = new Station { Index = s.Stations.Count, Kind = Kind.Furnace, Name = "Fornalha lateral experimental", Pos = new V2(10.2f, 4f), InItem = Item.Ore, OutItem = Item.Ingot, Need = 1, InCap = Balance.FurnaceIn, OutCap = Balance.FurnaceOut, Time = Balance.FurnaceTime2 };
            s.Stations.Add(lateral); s.ExperimentalLateralIndex = lateral.Index;
        }
        int nextMinute = 42;
        while (s.Time < End)
        {
            float starveBefore = s.JewelBench.StarveSeconds;
            bot.Step(s, Dt);
            r.N++; r.AIn += s.FurnaceA.In; r.AOut += s.FurnaceA.Out; r.BIn += s.FurnaceB.In; r.BOut += s.FurnaceB.Out;
            if (s.FurnaceA.Out == 0) r.AEmpty++;
            if (s.FurnaceB.Out == 0) r.BEmpty++;
            if (s.JewelBench.StarveSeconds > starveBefore) { r.JewelStarveSamples++; if (s.FurnaceA.Out + s.FurnaceB.Out + (lateral?.Out ?? 0) > 0) r.StarveSourceAvailable++; }
            if (lateral != null) { r.LIn += lateral.In; r.LOut += lateral.Out; }
            if (belt != null) belt.Step(s, Dt);
            VerifyConservation(s, belt?.Buffer ?? 0);
            Require(s.UpgradesBought == 20 && s.OfflineEarned == initialOffline, "Compra ou offline contaminou a janela.");
            if (s.Time >= nextMinute * 60f)
            {
                MinuteCsv.WriteLine(string.Join(",", new object[] { name, nextMinute, F(s.Time), s.GoldEarned, s.SoldItems[5], s.SoldItems[2], s.SoldItems[3], s.SoldItems[4], F(s.JewelBench.StarveSeconds), F(s.JewelBench.StallSeconds), s.FurnaceA.In, s.FurnaceA.Out, s.FurnaceB.In, s.FurnaceB.Out, belt?.Buffer ?? 0, belt?.Delivered ?? 0 }));
                nextMinute++;
            }
        }
        r.Seconds = s.Time - r.StartTime; r.Minutes = r.Seconds / 60.0;
        r.StarvePct = 100 * (s.JewelBench.StarveSeconds - initialStarve) / r.Seconds; r.StallPct = 100 * (s.JewelBench.StallSeconds - initialStall) / r.Seconds;
        r.AStarve = 100 * (s.FurnaceA.StarveSeconds - aStarve) / r.Seconds; r.AStall = 100 * (s.FurnaceA.StallSeconds - aStall) / r.Seconds;
        r.BStarve = 100 * (s.FurnaceB.StarveSeconds - bStarve) / r.Seconds; r.BStall = 100 * (s.FurnaceB.StallSeconds - bStall) / r.Seconds;
        r.WalkPct = 100 * (s.WalkNoDecision - initialWalk) / r.Seconds;
        r.Sold = s.SoldItems.Zip(initialSold, (end, start) => end - start).ToArray();
        r.JewelsPerMin = r.Sold[5] / r.Minutes; r.OtherPerMin = (r.Sold[2] + r.Sold[3] + r.Sold[4]) / r.Minutes;
        r.GoldPerMin = (s.GoldEarned - initialEarned) / r.Minutes;
        r.EndGoldEarned = s.GoldEarned; r.EndJewels = s.SoldItems[5]; r.IngotsMade = s.Crafted[1] - initialIngots;
        r.Buffer = belt?.Buffer ?? 0; r.Delivered = belt?.Delivered ?? 0;
        if (lateral != null) { r.LStarve = 100 * lateral.StarveSeconds / r.Seconds; r.LStall = 100 * lateral.StallSeconds / r.Seconds; }
        r.FinalSignature = Signature(s, bot);
        Require(s.GoldEarned - initialEarned == r.Sold[2] * 10 + r.Sold[3] * 25 + r.Sold[4] * 16 + r.Sold[5] * 80, "Receita nao bate com vendas.");
        return r;
    }

    sealed class Belt
    {
        readonly float Interval; float Timer;
        public int Buffer, Delivered;
        public Belt(float interval) { Interval = interval; }
        public void Step(Sim s, float dt)
        {
            while (Buffer < 2 && s.JewelBench.In + Buffer < s.JewelBench.InCap)
            {
                Station source = s.FurnaceA.Out >= s.FurnaceB.Out ? s.FurnaceA : s.FurnaceB;
                if (source.Out <= 0) break;
                source.Out--; Buffer++;
            }
            if (Buffer == 0 || s.JewelBench.In >= s.JewelBench.InCap) { Timer = 0f; return; }
            Timer += dt;
            if (Timer < Interval) return;
            Timer -= Interval; Buffer--; s.JewelBench.In++; Delivered++;
        }
    }

    static void VerifyConservation(Sim s, int buffer)
    {
        int present = buffer, committed = 0;
        foreach (Station st in s.Stations)
        {
            Require(st.In >= 0 && st.Out >= 0 && st.In <= st.InCap && st.Out <= st.OutCap, "Capacidade/estoque invalido.");
            if (st.Kind == Kind.Furnace) present += st.Out;
            if (st.Kind == Kind.Crafter) { present += st.In; if (st.Busy) committed += st.Need; }
        }
        foreach (Carrier c in s.Workers.Concat(new[] { s.Player })) { Require(c.Count >= 0 && c.Count <= c.Cap, "Carga invalida."); if (c.Item == Item.Ingot) present += c.Count; }
        int consumed = Enumerable.Range(2, 4).Sum(i => s.Crafted[i] * Balance.IngotsPer[i]);
        Require(s.Crafted[1] == present + committed + consumed, "Criacao/perda de lingotes.");
        for (int i = 2; i < 6; i++)
        {
            int product = s.Stock[i] + s.SoldItems[i] + s.Stations.Where(st => st.Kind == Kind.Crafter && (int)st.OutItem == i).Sum(st => st.Out) + s.Workers.Concat(new[] { s.Player }).Where(c => (int)c.Item == i).Sum(c => c.Count);
            Require(s.Crafted[i] == product, "Criacao/perda de produtos.");
        }
        Require(buffer >= 0 && buffer <= 2, "Buffer invalido.");
    }

    static string Signature(Sim sim, Bot bot)
    {
        var sb = new StringBuilder(); var ids = new Dictionary<object, int>(new Identity());
        WriteObject(sim, sb, ids); WriteObject(bot, sb, ids);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }
    static void WriteObject(object value, StringBuilder sb, Dictionary<object, int> ids)
    {
        if (value == null) { sb.Append("null;"); return; }
        Type t = value.GetType(); sb.Append(t.FullName).Append(':');
        if (value is float f) { sb.Append(BitConverter.SingleToInt32Bits(f).ToString("X8", Ci)).Append(';'); return; }
        if (value is double d) { sb.Append(BitConverter.DoubleToInt64Bits(d).ToString("X16", Ci)).Append(';'); return; }
        if (t.IsPrimitive || t.IsEnum || value is string) { sb.Append(Convert.ToString(value, Ci)).Append(';'); return; }
        if (!t.IsValueType) { if (ids.TryGetValue(value, out int id)) { sb.Append('@').Append(id).Append(';'); return; } ids.Add(value, ids.Count); }
        if (value is IEnumerable sequence) { sb.Append('['); foreach (object child in sequence) WriteObject(child, sb, ids); sb.Append(']'); return; }
        sb.Append('{'); foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name, StringComparer.Ordinal)) { sb.Append(field.Name).Append('='); WriteObject(field.GetValue(value), sb, ids); } sb.Append('}');
    }
    sealed class Identity : IEqualityComparer<object>
    {
        public new bool Equals(object a, object b) => ReferenceEquals(a, b);
        public int GetHashCode(object o) => RuntimeHelpers.GetHashCode(o);
    }
    static string F(double x) => x.ToString("0.####", Ci);
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    sealed class Result
    {
        public string Name, StartSignature, FinalSignature;
        public float StartTime;
        public double Seconds, Minutes, N, StarvePct, StallPct, JewelsPerMin, GoldPerMin, OtherPerMin, AIn, AOut, AEmpty, AStarve, AStall, BIn, BOut, BEmpty, BStarve, BStall, JewelStarveSamples, StarveSourceAvailable, WalkPct, LIn, LOut, LStarve, LStall;
        public int[] Sold;
        public int EndGoldEarned, EndJewels, IngotsMade, Delivered, Buffer;
    }
}
