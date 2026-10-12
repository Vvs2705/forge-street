using System;
using System.Globalization;
using FS.Core;

namespace FS
{
    /// <summary>Estado do cartao de melhoria (A-UI-18); a ordem do enum e' a ordem da bandeja (compravel primeiro, travado no fim).</summary>
    public enum Cartao { Disponivel, SemOuro, Travado, Comprado }

    /// <summary>
    /// Textos curtos da HUD, sem UnityEngine (v0.6d, revisao de UX): o teste EditMode (TextosTests) le daqui e falha se alguma dica
    /// passar de DicaMax. Dica = 1 linha de ate 26 caracteres, sem preco: a frase natural quando cabe, senao a curta.
    /// A-UI-18: cartao de melhoria (estado, beneficio em %, antes → depois e "Faltam N").
    /// </summary>
    public static class Textos
    {
        public const int DicaMax = 26;
        public static readonly string[] Plural = { "minérios", "lingotes", "espadas", "escudos", "ferramentas", "joias" };

        public static string Dica(Hint h, int arg, Sim s)
        {
            switch (h)
            {
                case Hint.BuyPad: int u = s.Pads[arg].Current(s); return u < 0 ? "" : Placa(u);
                case Hint.BuyMenu: return Menu(arg);
                case Hint.ProductToCounter:
                    return arg == (int)Item.Jewel ? "Leve as joias à loja" : Cabe($"Leve {Artigo(Plural[arg])} {Plural[arg]} ao balcão", $"Leve {Plural[arg]} ao balcão");
                case Hint.IngotToCrafter: return "Leve os lingotes à bigorna";
                case Hint.OreToFurnace: return "Leve o minério à fornalha";
                case Hint.PickProducts:
                    string a = Artigo(Plural[arg]);
                    return Cabe($"Pegue {a} {Plural[arg]} pront{a}", $"Pegue {a} {Plural[arg]}");
                case Hint.PickIngots: return "Pegue lingotes na fornalha";
                case Hint.ClientWaiting: return $"Cliente quer {Balance.ItemName[arg]}!";
                case Hint.OpenChest: return "Abra o baú: " + s.Chests[arg].Label;
                default: return "Pegue minério no depósito";
            }
        }

        /// <summary>"Pise na placa da Bigorna 2"; nome comprido cai para "Pise na placa: X" e depois "Placa: X".</summary>
        public static string Placa(int u)
        {
            string n = Upgrades.All[u].Name;
            return Cabe($"Pise na placa d{Artigo(n)} {n}", "Pise na placa: " + n, "Placa: " + n);
        }

        /// <summary>"Compre o Fole em Melhorias"; nome comprido cai para "Compre a Mochila" (o botao Melhorias ja pulsa com o badge).</summary>
        public static string Menu(int u)
        {
            string n = Upgrades.All[u].Name;
            return Cabe($"Compre {Artigo(n)} {n} em Melhorias", $"Compre {Artigo(n)} {n}");
        }

        static readonly string[] Escala = { "\u00A0mi", "\u00A0bi", "\u00A0tri", "\u00A0qua", "\u00A0qui" };   // curtas (8 caracteres no maximo, "9,22 qui"); espaco que nao quebra linha

        /// <summary>Ouro da HUD: inteiro ate 999999; acima, ate 3 algarismos + escala ("1,23 mi", "12,3 bi", "5 bi") para caber
        /// na pilula numa linha (A-CORE-06: com long, 5000000000 quebrava em 2). Trunca: nunca mostra mais do que o jogador tem.</summary>
        public static string Ouro(long v)
        {
            if (v < 1000000) return v.ToString(System.Globalization.CultureInfo.InvariantCulture);
            long scale = 1000000; int k = 0;
            while (v / scale >= 1000 && k < Escala.Length - 1) { scale *= 1000; k++; }
            long q = v / scale;
            int dec = q >= 100 ? 0 : q >= 10 ? 1 : 2;   // inteiro (ex.: 512), sem float: 1150000 nao vira "1,14"
            string f = dec == 0 ? "" : ((v % scale) / (scale / (dec == 2 ? 100 : 10))).ToString().PadLeft(dec, '0').TrimEnd('0');
            return q + (f.Length > 0 ? "," + f : "") + Escala[k];
        }

        /// <summary>Titulo do cartao da encomenda: verbo + N + item no plural ("Venda 5 espadas").</summary>
        public static string Venda(int n, int item) => $"Venda {n} {Plural[item]}";

        /// <summary>Cartao travado do menu: "Precisa: Balcão 5" (curto, sem quebrar linha).</summary>
        public static string Precisa(int u) => "Precisa: " + Upgrades.All[u].Name.Replace(" vagas", "");

        /// <summary>A-UI-18: comprado, travado (pre-requisito do menu nao comprado), disponivel (da para pagar) ou sem ouro.</summary>
        public static Cartao Estado(Sim s, int u)
        {
            int req = Upgrades.All[u].Requires;
            if (s.Bought[u]) return Cartao.Comprado;
            if (req >= 0 && !s.Bought[req]) return Cartao.Travado;
            return s.Gold >= s.T.Cost(u) ? Cartao.Disponivel : Cartao.SemOuro;
        }

        /// <summary>Cartao sem ouro: quanto falta ("Faltam 120", "Falta 1", "Faltam 1,2 mi").</summary>
        public static string Falta(long n) => n == 1 ? "Falta 1" : "Faltam " + Ouro(n);

        /// <summary>
        /// A-UI-18 (DM §65): beneficio do cartao em %, lido do Balance e do Tuning `t` da partida (mudou o balanco, muda o cartao). Tempo menor = "N% mais rapida"
        /// (1 - depois/antes: 4 → 2,5 s = 38%); quantidade ou ritmo maior = "+N%" (depois/antes - 1). Cabe em 2 linhas do cartao.
        /// </summary>
        public static string Beneficio(Tuning t, int u)
        {
            switch ((Upgrade)u)
            {
                case Upgrade.FurnaceSpeed1: return "Fornalha " + Menos(t.FurnaceTime0, t.FurnaceTime1) + " mais rápida";
                case Upgrade.FurnaceSpeed2: return "Fornalha " + Menos(t.FurnaceTime1, t.FurnaceTime2) + " mais rápida";
                case Upgrade.HammerSpeed: return "Bancadas " + Menos(t.HammerTime0, t.HammerTime1) + " mais rápidas";
                case Upgrade.JewelSpeed: return "Joalheria " + Menos(1f, t.JewelSpeedMul) + " mais rápida";
                case Upgrade.PlayerCapacity: return "Carga " + Mais(Balance.PlayerCap, Balance.PlayerCapUp);
                case Upgrade.PlayerSpeed: return "Velocidade " + Mais(Balance.PlayerSpeed, Balance.PlayerSpeedUp);
                case Upgrade.HelperSpeed: return "Velocidade " + Mais(Balance.WorkerSpeed, Balance.WorkerSpeedUp);   // VALIDACAO_V05 #4: a carga vai no AntesDepois
                case Upgrade.CounterCapacity: return "Clientes " + Mais(t.VitrineClientMul, 1f);   // intervalo x0,7 = ritmo x1,43
                case Upgrade.JewelVitrine: return "Joia vale " + Mais(t.Price[(int)Item.Jewel], t.JewelPriceUp);
                case Upgrade.Counter5: case Upgrade.Counter6: case Upgrade.Counter7: case Upgrade.Counter8:
                    int n = u - (int)Upgrade.Counter5 + t.QueueCap0 + 1;
                    return "Vagas " + Mais(n - 1, n);
                default: return Upgrades.All[u].Desc;
            }
        }

        /// <summary>Linha de baixo do cartao: antes → depois em numeros (era o MenuBar.Effect); "" quando o antes depende de outra compra (Lupa).</summary>
        public static string AntesDepois(Tuning t, int u)
        {
            switch ((Upgrade)u)
            {
                case Upgrade.FurnaceSpeed1: return N(t.FurnaceTime0) + " → " + N(t.FurnaceTime1) + " s";
                case Upgrade.FurnaceSpeed2: return N(t.FurnaceTime1) + " → " + N(t.FurnaceTime2) + " s";
                case Upgrade.HammerSpeed: return N(t.HammerTime0) + " → " + N(t.HammerTime1) + " s";
                case Upgrade.PlayerCapacity: return N(Balance.PlayerCap) + " → " + N(Balance.PlayerCapUp);
                case Upgrade.PlayerSpeed: return N(Balance.PlayerSpeed) + " → " + N(Balance.PlayerSpeedUp) + " m/s";
                case Upgrade.HelperSpeed: return "carga " + N(Balance.WorkerCap) + " → " + N(Balance.WorkerCapUp);
                case Upgrade.CounterCapacity: return "estoque " + N(t.CounterCap0) + " → " + N(t.CounterCap1);
                case Upgrade.JewelVitrine: return "fila " + N(Balance.JewelQueueCap) + " → " + N(Balance.JewelQueueCapUp);
                case Upgrade.Counter5: case Upgrade.Counter6: case Upgrade.Counter7: case Upgrade.Counter8:
                    int n = u - (int)Upgrade.Counter5 + t.QueueCap0 + 1;
                    return N(n - 1) + " → " + n + " vagas";
                default: return "";   // ponytail: Lupa = x0,6 sobre o tempo que o Martelo veloz ja mudou; o % basta
            }
        }

        static string Menos(double antes, double depois) => Pct(1.0 - depois / antes) + "%";
        static string Mais(double antes, double depois) => "+" + Pct(depois / antes - 1.0) + "%";
        static long Pct(double f) => (long)Math.Round(100.0 * f, MidpointRounding.AwayFromZero);
        static string N(float v) => v.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');   // virgula decimal sem depender da cultura do aparelho

        // ponytail: genero pela 1a palavra (termina em a / as / os / es), so a Vitrine foge; nome novo que fuja tambem entra aqui
        static string Artigo(string n)
        {
            int sp = n.IndexOf(' ');
            string w = sp < 0 ? n : n.Substring(0, sp);
            char z = w[w.Length - 1], y = w.Length > 1 ? w[w.Length - 2] : ' ';
            if (z == 's') return y == 'a' ? "as" : "os";
            return z == 'a' || w == "Vitrine" ? "a" : "o";
        }

        static string Cabe(string a, string b, string c = null) => a.Length <= DicaMax ? a : b.Length <= DicaMax || c == null ? b : c;
    }
}
