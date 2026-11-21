namespace FS.Core
{
    /// <summary>Bot em partes: as Obras do Reino (2026-11-21).</summary>
    public sealed partial class Bot
    {
        /// <summary>
        /// Obras do Reino: constroi o proximo degrau so quando nada mais do distrito ativo esta a venda (producao completa, nenhum pad, nada no menu,
        /// nenhum worker a contratar ou subir) e o ouro que sobra depois do degrau ainda cobre a reserva: o proximo nivel de manager mais barato
        /// (o Bot.Manager compra quando a carteira paga) e a abertura do proximo distrito ainda fechado. Chamar depois do Bot.Manager; devolve se
        /// construiu. ponytail: 1 degrau por chamada; o econsim chama a cada passo, entao o ouro parado vira degraus em poucos passos.
        /// </summary>
        public static bool Obras(Imperio imp)
        {
            Obras o = imp.Obras;
            Sim s = imp.Ativo;
            if (!o.Aberta || !NadaAComprar(s)) return false;
            long c = o.Proximo;
            return s.Gold - c >= Reserva(imp) && o.Construir(o.Degraus);
        }

        /// <summary>Nada do distrito `s` a venda agora (pad, menu ou Equipe) e a producao completa.</summary>
        public static bool NadaAComprar(Sim s)
        {
            if (!s.ProductionComplete) return false;
            foreach (Pad p in s.Pads) if (p.Current(s) >= 0) return false;
            for (int u = 0; u < s.UpgradeDefs.Length; u++) if (s.MenuAvailable(u)) return false;
            for (int k = 0; k < s.Equipe.Defs.Length; k++) if (s.Equipe.AVenda(k)) return false;
            return true;
        }

        /// <summary>Ouro que o bot guarda antes de mandar para as Obras: o manager mais barato que falta + a abertura do proximo distrito fechado.</summary>
        public static long Reserva(Imperio imp)
        {
            long mgr = long.MaxValue;
            foreach (DistrictDef d in Distritos.All)
            {
                long k = imp.CustoManager(d.Id);
                if (k >= 0 && k < mgr) mgr = k;
            }
            string prox = Distritos.Find(imp.Ativo.Distrito.Id)?.Charter?.Proximo;
            DistrictDef pd = prox == null || imp.Aberto(prox) ? null : Distritos.Find(prox);
            return (mgr == long.MaxValue ? 0 : mgr) + (pd?.Abertura ?? 0);
        }
    }
}
