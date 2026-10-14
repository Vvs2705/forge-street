using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// A-PLAT-08 (DM §88, §130; GDD §23 item 18): RemoteConfig no nucleo. Um JSON plano {"id": "...", "versao": N, "&lt;chave&gt;": valor}
    /// aplicado sobre um Tuning ANTES do Sim nascer (o Sim le o Tuning dele: config novo vale na proxima abertura). Chave = nome exato de um
    /// campo publico do Tuning (bool, int, long, float ou array deles com o MESMO tamanho do padrao) ou "Lista.id.Campo" para um elemento com
    /// Id de um array do Tuning (Placements.vip.Recarga, Placements.supply.Ligado): campo novo ja e' chaveavel sem mexer aqui. O resto (chave
    /// desconhecida, Id que o build nao tem, tipo errado, array de outro tamanho) e' ignorado e registrado: o config so AJUSTA o que o build ja
    /// tem, nunca cria item, placement, oferta, bau ou sorteio (V4: nada aleatorio pago). Travas, todas aqui:
    /// - faixa generica: cada numero fica em [padrao/10, padrao x 10] do BUILD (padrao 0 fica 0: preco do minerio, Masterwork que nao vende);
    /// - anuncio so mais RARO que o build: NuncaMenor (recargas, cooldown) nao desce do padrao; NuncaMaior (tetos e o quanto empilha) nao sobe;
    /// - Travas absolutas: offline &lt;= 7.200 s (P2), fator offline &lt;= 1, Cofre 2x no maximo dobra (P2); SoLiga nao desliga (D6);
    /// - intersticial (P3/V10): o remoto desliga; ligado so vale com `intersticialNoBuild` (o build que entra no A/B);
    /// - AnunciosLigados = false (chave mestra, DM §130) desliga todo placement e o intersticial.
    /// Valor fora da faixa e' PRESO e registrado em Resultado.Avisos. JSON que nao abre = nada muda (padroes). Valores absolutos: aplicar 2x = 1x.
    /// </summary>
    public static class RemoteConfig
    {
        /// <summary>Config maior que isto nem abre (o de hoje tem &lt; 2 KB).</summary>
        public const int MaxChars = 64 * 1024;

        /// <summary>Faixa absoluta (decisoes; intersecao com a generica). Chave exata ou com coringa no Id ("Placements.*.Valor").</summary>
        static readonly Dictionary<string, (double Min, double Max)> Travas = new Dictionary<string, (double, double)>
        {
            ["OfflineMaxSeconds"] = (0, Balance.OfflineCapSeconds),   // P2: 2 h e' trava do nucleo; um evento so encurta
            ["OfflineFactor"] = (0, 1),                                // offline nunca rende mais que online (P2: premium mexe na taxa) - revisar
            ["Placements.cofre2x.Valor"] = (0, 1),                     // P2: o Cofre 2x dobra o mesmo resgate uma vez, nunca mais
        };

        /// <summary>Nunca abaixo do padrao do build: as recargas por placement (P14/V11: VIP e Velocidade 5 min; Supply 10 min) e o cooldown do
        /// intersticial.</summary>
        static readonly string[] NuncaMenor = { "Placements.*.Recarga", nameof(Tuning.InterstitialCooldown) };

        /// <summary>Nunca acima do padrao do build: tetos (diario total e por placement, intersticial por sessao) e o quanto um anuncio empilha
        /// (Velocidade ate 3x, ferreiro ate 1,3x, pacote do VIP chamado ate 20).</summary>
        static readonly string[] NuncaMaior = { nameof(Tuning.AnunciosTetoDia), "Placements.*.TetoDia", nameof(Tuning.InterstitialMaxSessao),
            nameof(Tuning.BoostMax), nameof(Tuning.BoostPlayerMax), nameof(Tuning.VipSummonPackMax) };

        /// <summary>Bools que o remoto liga mas nao desliga: D6/V18 (anuncio so depois da 1a melhoria; sem ele o boost no segundo 0 furava o §3).</summary>
        static readonly string[] SoLiga = { nameof(Tuning.AnunciosDepoisDaPrimeiraMelhoria) };

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
            return Valores(t, o, intersticialNoBuild, r);
        }

        /// <summary>D-CORE-03: aplica `valores` (chave -> numero, bool ou lista, como o JSON le) sobre `t` com as MESMAS travas. Os modificadores
        /// de evento (LiveOps) passam por aqui: um evento nunca fura guardrail.</summary>
        public static Resultado Valores(Tuning t, IDictionary<string, object> valores, bool intersticialNoBuild, Resultado r = null)
        {
            r = r ?? new Resultado();
            var padrao = new Tuning();
            foreach (KeyValuePair<string, object> kv in valores)
            {
                if (kv.Key == "id" || kv.Key == "versao") continue;
                FieldInfo f = Campo(t, padrao, kv.Key, out object alvo, out object pad, out string coringa);
                string aviso = f == null ? "chave desconhecida (ignorada)" : Um(alvo, pad, f, kv.Key, coringa, kv.Value, intersticialNoBuild);
                if (aviso == null || aviso.StartsWith("preso", StringComparison.Ordinal)) r.Aplicadas++;
                if (aviso != null) r.Avisos.Add(kv.Key + ": " + aviso);
            }
            if (!t.AnunciosLigados)
            {
                t.Interstitial = false;
                foreach (AdPlacementDef p in t.Placements) p.Ligado = false;
            }
            return r;
        }

        /// <summary>O campo da `chave` e onde ele mora: no Tuning, ou no elemento de Id `id` do array `Lista` ("Lista.id.Campo"; o Id nao e'
        /// chave). `coringa` = "Lista.*.Campo" (as travas valem para todo Id). null = a chave nao existe neste build.</summary>
        static FieldInfo Campo(Tuning t, Tuning padrao, string chave, out object alvo, out object pad, out string coringa)
        {
            alvo = t; pad = padrao; coringa = chave;
            string[] p = chave.Split('.');
            if (p.Length == 1) return Publico(typeof(Tuning), chave);
            FieldInfo lista = p.Length == 3 && p[2] != "Id" ? Publico(typeof(Tuning), p[0]) : null;
            if (lista == null || !lista.FieldType.IsArray) return null;
            alvo = PorId((Array)lista.GetValue(t), p[1]); pad = PorId((Array)lista.GetValue(padrao), p[1]);
            coringa = p[0] + ".*." + p[2];
            return alvo == null || pad == null ? null : Publico(alvo.GetType(), p[2]);
        }

        static object PorId(Array a, string id)
        {
            foreach (object o in a) if (o != null && Publico(o.GetType(), "Id")?.GetValue(o) is string s && s == id) return o;
            return null;
        }

        static FieldInfo Publico(Type ty, string nome)
        {
            FieldInfo f = ty.GetField(nome, BindingFlags.Public | BindingFlags.Instance);
            return f == null || f.IsInitOnly ? null : f;
        }

        /// <summary>Um campo de `alvo` (padrao do build em `pad`). null = valeu como veio; "preso ..." = valeu preso na faixa; outro texto =
        /// ignorado (nada muda).</summary>
        static string Um(object alvo, object pad, FieldInfo f, string chave, string coringa, object val, bool intersticialNoBuild)
        {
            Type ty = f.FieldType;
            if (ty == typeof(bool))
            {
                if (!(val is bool b)) return "tipo (espera true/false)";
                string preso = null;
                if (b && chave == nameof(Tuning.Interstitial) && !intersticialNoBuild) { b = false; preso = "preso: true -> false (o build nao permite o intersticial)"; }
                if (!b && Array.IndexOf(SoLiga, chave) >= 0) { b = true; preso = "preso: false -> true (trava)"; }
                f.SetValue(alvo, b);
                return preso;
            }
            if (ty.IsArray)
            {
                Type et = ty.GetElementType();
                if (!Numero(et)) return "tipo nao suportado (elemento com Id: use " + chave + ".<id>.<campo>)";
                if (!(val is List<object> l)) return "tipo (espera array)";
                var pa = (Array)f.GetValue(pad);
                if (l.Count != pa.Length) return "tamanho " + l.Count + " (o build tem " + pa.Length + "; o config nao cria nem tira entrada)";
                Array na = Array.CreateInstance(et, pa.Length);
                var presos = new StringBuilder();
                for (int i = 0; i < pa.Length; i++)
                {
                    if (!(l[i] is double di) || double.IsNaN(di) || double.IsInfinity(di)) return "tipo (espera numeros)";
                    double c = Prende(chave, coringa, et, Convert.ToDouble(pa.GetValue(i), CultureInfo.InvariantCulture), di);
                    if (c != di) presos.Append(presos.Length > 0 ? ", " : "").Append('[').Append(i).Append("] ").Append(N(di)).Append(" -> ").Append(N(c));
                    na.SetValue(Convert.ChangeType(c, et, CultureInfo.InvariantCulture), i);
                }
                f.SetValue(alvo, na);   // array novo: o Tuning nunca divide array com o JSON nem com outro Tuning
                return presos.Length > 0 ? "preso: " + presos : null;
            }
            if (!Numero(ty)) return "tipo nao suportado";
            if (!(val is double dv) || double.IsNaN(dv) || double.IsInfinity(dv)) return "tipo (espera numero)";
            double cv = Prende(chave, coringa, ty, Convert.ToDouble(f.GetValue(pad), CultureInfo.InvariantCulture), dv);
            f.SetValue(alvo, Convert.ChangeType(cv, ty, CultureInfo.InvariantCulture));
            return cv != dv ? "preso: " + N(dv) + " -> " + N(cv) : null;
        }

        static bool Numero(Type ty) => ty == typeof(int) || ty == typeof(long) || ty == typeof(float);

        static bool Em(string[] lista, string chave, string coringa) => Array.IndexOf(lista, chave) >= 0 || Array.IndexOf(lista, coringa) >= 0;

        /// <summary>`d` na faixa da chave: [padrao/10, padrao x 10] (padrao 0 = so 0), NuncaMenor/NuncaMaior pelo padrao e a Trava absoluta
        /// (exata, senao a do coringa); inteiro arredonda.</summary>
        static double Prende(string chave, string coringa, Type ty, double padrao, double d)
        {
            double lo = Math.Min(padrao / 10, padrao * 10), hi = Math.Max(padrao / 10, padrao * 10);
            if (Em(NuncaMenor, chave, coringa)) lo = Math.Max(lo, padrao);
            if (Em(NuncaMaior, chave, coringa)) hi = Math.Min(hi, padrao);
            if (Travas.TryGetValue(chave, out (double Min, double Max) tr) || Travas.TryGetValue(coringa, out tr)) { lo = Math.Max(lo, tr.Min); hi = Math.Min(hi, tr.Max); }
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
            // 1e999: o .NET novo le infinito e o Mono da Unity recusa; aqui os dois recusam
            if (i == ini || !double.TryParse(s.Substring(ini, i - ini), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                || double.IsInfinity(d) || double.IsNaN(d)) throw Erro();
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
