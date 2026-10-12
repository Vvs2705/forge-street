using System;
using System.Globalization;
using System.IO;
using System.Text;
using FS.Core;
#if !FS_CORETESTS
using UnityEngine;
#endif

namespace FS
{
    /// <summary>
    /// A-PLAT-02: save em arquivo, so System.IO (sem UnityEngine: o SaveTests roda no dotnet, como o Textos). Grava o .tmp com flush no
    /// disco e troca pelo principal; o anterior vira .bak. Le o principal, senao o .bak. Arquivo que nao abre no envelope (soma errada,
    /// vazio ou sem cabecalho: o arquivo sempre sai com envelope) vai para a quarentena (.corrupt-data) e nunca e' apagado nem
    /// sobrescrito. Schema mais novo para tudo: nao usa o .bak nem mexe em arquivo (quem chama nao grava na sessao).
    /// A-PLAT-06: a instancia (new SaveStore(caminho)) e' o ISaveStorage do arquivo; o Salvamento junta ela com o SaveEspelho.
    /// </summary>
    public sealed class SaveStore : ISaveStorage
    {
        readonly string _path;

        public SaveStore(string path) => _path = path;

        public SaveEnvelope.Result Read(out int corrupt) => Read(_path, out corrupt);
        public void Write(string text) => Write(_path, text);
        /// <summary>-reset: principal e .bak; a quarentena fica.</summary>
        public void Delete() { File.Delete(_path); File.Delete(_path + ".bak"); }

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

#if !FS_CORETESTS
    /// <summary>
    /// A-PLAT-06: espelho do save no PlayerPrefs, na chave da 0.1-0.6 (fs.save): o save da 0.6 migra daqui (sem envelope = Legacy, vale)
    /// e um downgrade para a 0.6 le o mesmo texto. O ruim vai para a chave .corrupt (ponytail: 1 vaga de quarentena).
    /// </summary>
    public sealed class SaveEspelho : ISaveStorage
    {
        readonly string _key;

        public SaveEspelho(string key) => _key = key;

        public SaveEnvelope.Result Read(out int corrupt)
        {
            string raw = PlayerPrefs.GetString(_key, "");
            SaveEnvelope.Result r = SaveEnvelope.Unwrap(raw);
            corrupt = r.Status == SaveEnvelope.Status.Corrupt ? 1 : 0;
            if (corrupt > 0) PlayerPrefs.SetString(_key + ".corrupt", raw);
            return r;
        }

        public void Write(string text) { PlayerPrefs.SetString(_key, text); PlayerPrefs.Save(); }
        public void Delete() => PlayerPrefs.DeleteKey(_key);
    }
#endif
}
