using System;
using System.Globalization;
using System.IO;
using System.Text;
using FS.Core;

namespace FS
{
    /// <summary>
    /// A-PLAT-02: save em arquivo, so System.IO (sem UnityEngine: o SaveTests roda no dotnet, como o Textos). Grava o .tmp com flush no
    /// disco e troca pelo principal; o anterior vira .bak. Le o principal, senao o .bak. Arquivo que nao abre no envelope (soma errada,
    /// vazio ou sem cabecalho: o arquivo sempre sai com envelope) vai para a quarentena (.corrupt-data) e nunca e' apagado nem
    /// sobrescrito. Schema mais novo para tudo: nao usa o .bak nem mexe em arquivo (quem chama nao grava na sessao).
    /// </summary>
    public static class SaveStore
    {
        public static void Write(string path, string text)
        {
            string tmp = path + ".tmp", bak = path + ".bak";
            byte[] bytes = new UTF8Encoding(false).GetBytes(text);
            using (var f = new FileStream(tmp, FileMode.Create, FileAccess.Write)) { f.Write(bytes, 0, bytes.Length); f.Flush(true); }
            if (!File.Exists(path)) { File.Move(tmp, path); return; }
            try { File.Replace(tmp, path, bak); }
            catch (Exception)   // ponytail: File.Replace nao vale em todo sistema de arquivos; a troca manual tem um buraco entre os Move que o Read cobre pelo .bak
            {
                if (File.Exists(path)) { File.Delete(bak); File.Move(path, bak); }   // Replace que falhou no meio pode ja ter levado o principal ao .bak
                File.Move(tmp, path);
            }
        }

        /// <summary>Principal, senao o .bak; `corrupt` = quantos foram para a quarentena. Sem arquivo: Missing; todos ruins: Corrupt.</summary>
        public static SaveEnvelope.Result Read(string path, out int corrupt)
        {
            corrupt = 0;
            foreach (string p in new[] { path, path + ".bak" })
            {
                if (!File.Exists(p)) continue;
                SaveEnvelope.Result r = SaveEnvelope.Unwrap(File.ReadAllText(p));
                if (r.Status == SaveEnvelope.Status.Ok || r.Status == SaveEnvelope.Status.NewerSchema) return r;
                File.Move(p, p + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
                corrupt++;
            }
            return new SaveEnvelope.Result { Status = corrupt > 0 ? SaveEnvelope.Status.Corrupt : SaveEnvelope.Status.Missing, Payload = "" };
        }
    }
}
