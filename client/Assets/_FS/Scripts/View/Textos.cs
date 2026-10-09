using FS.Core;

namespace FS
{
    /// <summary>
    /// Textos curtos da HUD, sem UnityEngine (v0.6d, revisao de UX): o teste EditMode (TextosTests) le daqui e falha se alguma dica
    /// passar de DicaMax. Dica = 1 linha de ate 26 caracteres, sem preco: a frase natural quando cabe, senao a curta.
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
