using System;
using System.Threading.Tasks;

namespace FS.Core
{
    /// <summary>
    /// Cloud Save (UGS, atras da flag Tuning.CloudSave, desligada): decide o que fazer com o save do aparelho e o da nuvem, os dois no texto
    /// inteiro do envelope (SaveEnvelope, Save 2.0). Sobe o arquivo inteiro; baixa so o que tem MAIS progresso (Renome total, que nunca cai e
    /// ja conta os Charters) e, no empate, o mais novo (at= do envelope). Nunca baixa o que nao abre (corrompido, sem cabecalho) e nunca
    /// sobrescreve o save de um build mais novo (NewerSchema): o aparelho velho fica com o dele ate atualizar.
    /// </summary>
    public static class CloudSave
    {
        public enum Acao { Nada, Subir, Baixar }

        public static Acao Resolver(string local, string remoto)
        {
            SaveEnvelope.Result l = SaveEnvelope.Unwrap(local), r = SaveEnvelope.Unwrap(remoto);
            if (r.Status == SaveEnvelope.Status.NewerSchema || l.Status == SaveEnvelope.Status.NewerSchema) return Acao.Nada;
            bool lOk = l.Status == SaveEnvelope.Status.Ok, rOk = r.Status == SaveEnvelope.Status.Ok;
            if (!rOk) return lOk ? Acao.Subir : Acao.Nada;
            if (!lOk) return Acao.Baixar;   // aparelho sem save (instalacao nova) ou com save que nao abre: a nuvem vale
            if (local == remoto) return Acao.Nada;
            long pl = Progresso(l.Payload), pr = Progresso(r.Payload);
            if (pr != pl) return pr > pl ? Acao.Baixar : Acao.Subir;
            return r.WrittenAt > l.WrittenAt ? Acao.Baixar : Acao.Subir;
        }

        /// <summary>Renome total do Imperio (o da raiz: o Renome e' um so).</summary>
        public static long Progresso(string payload) => Imperio.Load(payload).Raiz.Renome.Total;
    }

    /// <summary>A nuvem do save (UGS Cloud Save no View; fake nos testes). Ler = o texto do envelope ou null (nada la); qualquer erro lanca.</summary>
    public interface INuvemSave
    {
        Task<string> Ler();
        Task Gravar(string texto);
    }

    /// <summary>
    /// A ponte entre o save local e a INuvemSave, tolerante a offline: nada lanca e nada espera mais que TimeoutMs (a abertura nunca trava;
    /// sem rede = Nada e o jogo segue no local). Abrir = 1x por sessao, depois da tela de idade: resolve e, se for Subir, ja sobe; se for
    /// Baixar, devolve o texto da nuvem para o Game gravar e recarregar. Subir = depois de cada save, no maximo 1 a cada IntervaloS.
    /// </summary>
    public sealed class SincroniaNuvem
    {
        public const int TimeoutMs = 8000;
        public const double IntervaloS = 60;
        readonly INuvemSave _nuvem;
        readonly int _timeout;
        double _ultimo = double.NegativeInfinity;
        public string Erro { get; private set; }

        public SincroniaNuvem(INuvemSave nuvem, int timeoutMs = TimeoutMs) { _nuvem = nuvem; _timeout = timeoutMs; }

        public async Task<(CloudSave.Acao Acao, string Texto)> Abrir(string local, double agoraS)
        {
            string remoto = null;
            if (!await Tenta(async () => remoto = await _nuvem.Ler())) return (CloudSave.Acao.Nada, null);
            CloudSave.Acao a = CloudSave.Resolver(local, remoto);
            if (a == CloudSave.Acao.Subir && !await Sobe(local, agoraS)) return (CloudSave.Acao.Nada, null);
            return (a, a == CloudSave.Acao.Baixar ? remoto : null);
        }

        /// <summary>true = subiu agora; false = dentro do intervalo, sem rede ou erro (o proximo save tenta de novo).</summary>
        public Task<bool> Subir(string texto, double agoraS) => agoraS - _ultimo < IntervaloS ? Task.FromResult(false) : Sobe(texto, agoraS);

        async Task<bool> Sobe(string texto, double agoraS)
        {
            if (string.IsNullOrEmpty(texto)) return false;
            bool ok = await Tenta(() => _nuvem.Gravar(texto));
            if (ok) _ultimo = agoraS;
            return ok;
        }

        async Task<bool> Tenta(Func<Task> f)
        {
            try
            {
                Task t = f();
                if (await Task.WhenAny(t, Task.Delay(_timeout)) != t) { Erro = "timeout"; return false; }
                await t;
                Erro = null;
                return true;
            }
            catch (Exception e) { Erro = e.GetType().Name; return false; }
        }
    }
}
