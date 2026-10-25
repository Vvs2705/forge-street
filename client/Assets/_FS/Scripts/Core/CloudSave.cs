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
}
