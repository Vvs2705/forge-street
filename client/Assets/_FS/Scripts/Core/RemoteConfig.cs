using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// A-PLAT-08 (DM §88, §130; GDD §23 item 18): RemoteConfig no nucleo. Um JSON plano {"id": "...", "versao": N, "&lt;campo do Tuning&gt;": valor}
    /// aplicado sobre um Tuning ANTES do Sim nascer (o Sim le o Tuning dele: config novo vale na proxima abertura). Chave = nome exato de um
    /// campo publico do Tuning (bool, int, long, float ou array deles com o MESMO tamanho do padrao): campo novo no Tuning ja e' chaveavel sem
    /// mexer aqui. O resto (chave desconhecida, tipo errado, array de outro tamanho) e' ignorado e registrado: o config so AJUSTA o que o build
    /// ja tem, nunca cria item, oferta, bau ou sorteio (V4: nada aleatorio pago). Travas, todas aqui:
    /// - faixa generica: cada numero fica em [padrao/10, padrao x 10] do BUILD (padrao 0 fica 0: preco do minerio, Masterwork que nao vende);
    /// - Travas (por nome de campo, somam com a generica): offline &lt;= 7.200 s (P2), fator offline &lt;= 1, recargas de anuncio &gt;= as decididas e
    ///   tetos &lt;= os decididos (o remoto so deixa o anuncio mais RARO que o build, nunca mais frequente); SoLiga nao desliga;
    /// - intersticial (P3/V10): o remoto desliga; ligado so vale com `intersticialNoBuild` (o build que entra no A/B);
    /// - Anuncios = false (chave mestra, DM §130) desliga toda flag de anuncio.
    /// Valor fora da faixa e' PRESO e registrado em Resultado.Avisos. JSON que nao abre = nada muda (padroes). Valores absolutos: aplicar 2x = 1x.
    /// </summary>
    public static class RemoteConfig
    {
        /// <summary>Config maior que isto nem abre (o de hoje tem &lt; 2 KB).</summary>
        public const int MaxChars = 64 * 1024;

        /// <summary>Faixa absoluta por campo (decisoes; intersecao com a generica). Campo que nao existe no Tuning ainda (os do
        /// feat/anuncios-config) so passa a valer quando existir.</summary>
        static readonly Dictionary<string, (double Min, double Max)> Travas = new Dictionary<string, (double, double)>
        {
            ["OfflineMaxSeconds"] = (0, Balance.OfflineCapSeconds),   // P2: 2 h e' trava do nucleo; um evento so encurta
            ["OfflineFactor"] = (0, 1),                                // offline nunca rende mais que online (P2: premium mexe na taxa) - revisar
            ["VipChamadoRecarga"] = (300, double.MaxValue),            // P14/V11: 5 min entre "Chamar VIP"
            ["BoostCooldownSeconds"] = (300, double.MaxValue),         // GDD §17.1: 5 min contados do fim do boost
            ["AnunciosTetoDia"] = (0, 30),                             // E-PLAT-02: teto diario total de rewarded
            ["InterstitialCooldown"] = (180, double.MaxValue),         // E-PLAT-03: s de sessao entre dois intersticiais
            ["InterstitialMaxSessao"] = (0, 3),                        // E-PLAT-03: por sessao
        };

        /// <summary>Bools que o remoto liga mas nao desliga: D6/V18 (anuncio so depois da 1a melhoria; sem ele o boost no segundo 0 furava o §3).</summary>
        static readonly string[] SoLiga = { nameof(Tuning.AnunciosDepoisDaPrimeiraMelhoria) };

        /// <summary>Flags que Anuncios = false desliga (por nome: a que nao existir no Tuning e' pulada).</summary>
        static readonly string[] FlagsDeAnuncio = { "VipChamado", "Velocidade", nameof(Tuning.Interstitial) };

        /// <summary>O que a aplicacao fez: Valido = o JSON abriu; Id/Versao do config ("padrao@0" sem config); Aplicadas = chaves que valeram
        /// (mesmo presas); Avisos = "chave: motivo" de cada valor preso ou ignorado.</summary>
        public sealed class Resultado
        {
            public bool Valido;
            public string Id = "padrao";
            public long Versao;
            public int Aplicadas;
            public readonly List<string> Avisos = new List<string>();
            /// <summary>"id@versao": o snapshot do config que vai em todo evento (DM §89).</summary>
            public string Rotulo => Id + "@" + Versao.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Aplica `json` sobre `t` (null ou vazio = sem config, nada muda). `intersticialNoBuild` = este build pode ligar o intersticial.</summary>
        public static Resultado Aplicar(Tuning t, string json, bool intersticialNoBuild)
        {
            var r = new Resultado();
            if (string.IsNullOrWhiteSpace(json)) return r;
            if (!(Json.Ler(json) is Dictionary<string, object> o)) { r.Avisos.Add("json: nao abre (valem os padroes)"); return r; }
            r.Valido = true;
            if (o.TryGetValue("id", out object id) && id is string s && s.Length > 0) r.Id = s.Length > 40 ? s.Substring(0, 40) : s;
            else r.Id = "sem_id";
            if (o.TryGetValue("versao", out object v) && v is double d && d >= 0 && d < long.MaxValue) r.Versao = (long)d;
            var padrao = new Tuning();
            foreach (KeyValuePair<string, object> kv in o)
            {
                if (kv.Key == "id" || kv.Key == "versao") continue;
                FieldInfo f = typeof(Tuning).GetField(kv.Key, BindingFlags.Public | BindingFlags.Instance);
                string aviso = f == null || f.IsInitOnly ? "chave desconhecida (ignorada)" : Um(t, padrao, f, kv.Value, intersticialNoBuild);
                if (aviso == null || aviso.StartsWith("preso", StringComparison.Ordinal)) r.Aplicadas++;
                if (aviso != null) r.Avisos.Add(kv.Key + ": " + aviso);
            }
            if (!t.Anuncios)
                foreach (string n in FlagsDeAnuncio) typeof(Tuning).GetField(n, BindingFlags.Public | BindingFlags.Instance)?.SetValue(t, false);
            return r;
        }

        /// <summary>Um campo. null = valeu como veio; "preso ..." = valeu preso na faixa; outro texto = ignorado (nada muda).</summary>
        static string Um(Tuning t, Tuning padrao, FieldInfo f, object val, bool intersticialNoBuild)
        {
            Type ty = f.FieldType;
            if (ty == typeof(bool))
            {
                if (!(val is bool b)) return "tipo (espera true/false)";
                string preso = null;
                if (b && f.Name == nameof(Tuning.Interstitial) && !intersticialNoBuild) { b = false; preso = "preso: true -> false (o build nao permite o intersticial)"; }
                if (!b && Array.IndexOf(SoLiga, f.Name) >= 0) { b = true; preso = "preso: false -> true (trava)"; }
                f.SetValue(t, b);
                return preso;
            }
            if (ty.IsArray)
            {
                Type et = ty.GetElementType();
                if (!Numero(et)) return "tipo nao suportado";
                if (!(val is List<object> l)) return "tipo (espera array)";
                var pa = (Array)f.GetValue(padrao);
                if (l.Count != pa.Length) return "tamanho " + l.Count + " (o build tem " + pa.Length + "; o config nao cria nem tira entrada)";
                Array na = Array.CreateInstance(et, pa.Length);
                var presos = new StringBuilder();
                for (int i = 0; i < pa.Length; i++)
                {
                    if (!(l[i] is double di) || double.IsNaN(di) || double.IsInfinity(di)) return "tipo (espera numeros)";
                    double c = Prende(f.Name, et, Convert.ToDouble(pa.GetValue(i), CultureInfo.InvariantCulture), di);
                    if (c != di) presos.Append(presos.Length > 0 ? ", " : "").Append('[').Append(i).Append("] ").Append(N(di)).Append(" -> ").Append(N(c));
                    na.SetValue(Convert.ChangeType(c, et, CultureInfo.InvariantCulture), i);
                }
                f.SetValue(t, na);   // array novo: o Tuning nunca divide array com o JSON nem com outro Tuning
                return presos.Length > 0 ? "preso: " + presos : null;
            }
            if (!Numero(ty)) return "tipo nao suportado";
            if (!(val is double dv) || double.IsNaN(dv) || double.IsInfinity(dv)) return "tipo (espera numero)";
            double cv = Prende(f.Name, ty, Convert.ToDouble(f.GetValue(padrao), CultureInfo.InvariantCulture), dv);
            f.SetValue(t, Convert.ChangeType(cv, ty, CultureInfo.InvariantCulture));
            return cv != dv ? "preso: " + N(dv) + " -> " + N(cv) : null;
        }

        static bool Numero(Type ty) => ty == typeof(int) || ty == typeof(long) || ty == typeof(float);

        /// <summary>`d` na faixa do campo: [padrao/10, padrao x 10] (padrao 0 = so 0) intersecao com a Trava; inteiro arredonda.</summary>
        static double Prende(string campo, Type ty, double padrao, double d)
        {
            double lo = Math.Min(padrao / 10, padrao * 10), hi = Math.Max(padrao / 10, padrao * 10);
            if (Travas.TryGetValue(campo, out (double Min, double Max) tr)) { lo = Math.Max(lo, tr.Min); hi = Math.Min(hi, tr.Max); }
            if (ty != typeof(float)) { lo = Math.Ceiling(lo); hi = Math.Floor(hi); d = Math.Round(d, MidpointRounding.AwayFromZero); }   // 2,5 -> 3 conta como preso
            return Math.Min(hi, Math.Max(lo, d));
        }

        static string N(double d) => d.ToString("0.####", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// JSON minimo do RemoteConfig e do esquema de eventos (o nucleo nao tem dependencia; o JsonUtility da Unity nao le dicionario).
    /// Ler: objeto = Dictionary&lt;string, object&gt;, array = List&lt;object&gt;, numero = double, string, bool, null. Texto que nao abre (sintaxe,
    /// lixo no fim, mais de 32 niveis ou de RemoteConfig.MaxChars) = null. Aspas: o texto entre aspas e escapado.
    /// </summary>
    public static class Json
    {
        public static object Ler(string s)
        {
            if (s == null || s.Length > RemoteConfig.MaxChars) return null;
            int i = 0;
            try
            {
                object v = Valor(s, ref i, 0);
                Espaco(s, ref i);
                return i == s.Length ? v : null;
            }
            catch (FormatException) { return null; }
        }

        public static string Aspas(string s)
        {
            var sb = new StringBuilder(s == null ? 2 : s.Length + 2).Append('"');
            foreach (char c in s ?? "")
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }

        static void Espaco(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static FormatException Erro() => new FormatException();

        static object Valor(string s, ref int i, int nivel)
        {
            if (nivel > 32) throw Erro();
            Espaco(s, ref i);
            if (i >= s.Length) throw Erro();
            char c = s[i];
            if (c == '{')
            {
                var o = new Dictionary<string, object>();
                i++; Espaco(s, ref i);
                if (i < s.Length && s[i] == '}') { i++; return o; }
                while (true)
                {
                    Espaco(s, ref i);
                    if (i >= s.Length || s[i] != '"') throw Erro();
                    string k = Texto(s, ref i);
                    Espaco(s, ref i);
                    if (i >= s.Length || s[i++] != ':') throw Erro();
                    o[k] = Valor(s, ref i, nivel + 1);
                    Espaco(s, ref i);
                    if (i >= s.Length) throw Erro();
                    if (s[i] == ',') { i++; continue; }
                    if (s[i++] == '}') return o;
                    throw Erro();
                }
            }
            if (c == '[')
            {
                var l = new List<object>();
                i++; Espaco(s, ref i);
                if (i < s.Length && s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Valor(s, ref i, nivel + 1));
                    Espaco(s, ref i);
                    if (i >= s.Length) throw Erro();
                    if (s[i] == ',') { i++; continue; }
                    if (s[i++] == ']') return l;
                    throw Erro();
                }
            }
            if (c == '"') return Texto(s, ref i);
            if (Palavra(s, ref i, "true")) return true;
            if (Palavra(s, ref i, "false")) return false;
            if (Palavra(s, ref i, "null")) return null;
            int ini = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (i == ini || !double.TryParse(s.Substring(ini, i - ini), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) throw Erro();
            return d;
        }

        static bool Palavra(string s, ref int i, string w)
        {
            if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) return false;
            i += w.Length;
            return true;
        }

        static string Texto(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++;   // a aspa de abertura
            while (true)
            {
                if (i >= s.Length) throw Erro();
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) throw Erro();
                char e = s[i++];
                switch (e)
                {
                    case '"': case '\\': case '/': sb.Append(e); break;
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 > s.Length || !int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int u)) throw Erro();
                        sb.Append((char)u); i += 4; break;
                    default: throw Erro();
                }
            }
        }
    }
}
