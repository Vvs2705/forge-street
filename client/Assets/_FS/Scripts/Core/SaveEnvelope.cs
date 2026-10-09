using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// A-CORE-01: envelope do save. Quatro linhas de cabecalho antes do payload do Sim.Save, nesta ordem (contrato fixo: versao futura
    /// so sobe o schema, nunca muda o cabecalho):
    ///   sha256=hex   SHA-256 (UTF-8) de tudo o que vem depois desta linha: 1 byte alterado = Corrupt
    ///   schema=N     Sim.Schema de quem gravou (maior que o deste build = NewerSchema)
    ///   content=X    versao do jogo que gravou (diagnostico)
    ///   at=unix      writtenAt
    /// O Sim.Load da 0.1-0.6 ignora chave desconhecida: um downgrade le o mesmo texto. Sem cabecalho = Legacy (saves da 0.1-0.6).
    /// Soma errada nunca vira estado inicial calado: Corrupt sai sem payload e quem chama decide (.bak, espelho, quarentena).
    /// </summary>
    public static class SaveEnvelope
    {
        public enum Status { Missing, Ok, Legacy, Corrupt, NewerSchema }   // Missing (nada gravado) e' o default
        public struct Result { public Status Status; public string Payload, Content; public int Schema; public long WrittenAt; }

        const string Sum = "sha256=";

        public static string Wrap(string payload, string content, long writtenAt)
        {
            string body = "schema=" + Sim.Schema + "\ncontent=" + content + "\nat=" + writtenAt.ToString(CultureInfo.InvariantCulture) + "\n" + payload;
            return Sum + Hash(body) + "\n" + body;
        }

        public static Result Unwrap(string text)
        {
            if (string.IsNullOrEmpty(text)) return new Result { Status = Status.Missing, Payload = "" };
            if (!text.StartsWith(Sum, StringComparison.Ordinal)) return new Result { Status = Status.Legacy, Payload = text };
            int nl = text.IndexOf('\n');
            string body = nl < 0 ? "" : text.Substring(nl + 1);
            string[] h = body.Split(new[] { '\n' }, 4);
            if (nl < 0 || text.Substring(Sum.Length, nl - Sum.Length) != Hash(body) || h.Length < 4
                || !int.TryParse(After(h[0], "schema="), NumberStyles.Integer, CultureInfo.InvariantCulture, out int schema))
                return new Result { Status = Status.Corrupt, Payload = "" };
            var r = new Result { Status = schema > Sim.Schema ? Status.NewerSchema : Status.Ok, Payload = h[3], Schema = schema, Content = After(h[1], "content=") };
            long.TryParse(After(h[2], "at="), NumberStyles.Integer, CultureInfo.InvariantCulture, out r.WrittenAt);
            return r;
        }

        static string After(string line, string key) => line.StartsWith(key, StringComparison.Ordinal) ? line.Substring(key.Length) : null;

        static string Hash(string s)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "").ToLowerInvariant();
        }
    }
}
