using System.Collections.Generic;

namespace FS.Core
{
    /// <summary>
    /// C-CORE-07: a regra fixa do bot no patio de cotas (CONTEUDO_V1 §2.4; BALANCE.md §38): "a Coroa compra o que a rua procura menos". As faixas
    /// abertas ficam com as linhas de produto abertas de MAIOR intervalo de cliente no balcao (Tuning.ClientInterval; empate: a ordem dos itens),
    /// tantas quantas faixas, deixando sempre pelo menos uma linha so para o balcao. A faixa que ja tem uma delas fica; a que tem outra (ou
    /// nenhuma) passa para a que falta, a partir do proximo bloco (EscolheCota: o bloco em curso termina). Deterministico. Sem patio: nada.
    /// </summary>
    public sealed partial class Bot
    {
        static void Cotas(Sim s)
        {
            int n = s.FaixasAbertas;
            if (n == 0) return;
            var linhas = new List<int>();
            for (int i = (int)Item.Sword; i < s.ItemDefs.Length; i++) if (s.LineUnlocked((Item)i)) linhas.Add(i);
            linhas.Sort((a, b) => s.T.ClientInterval[b] != s.T.ClientInterval[a] ? s.T.ClientInterval[b].CompareTo(s.T.ClientInterval[a]) : a.CompareTo(b));
            var quer = linhas.GetRange(0, System.Math.Max(0, System.Math.Min(n, linhas.Count - 1)));
            for (int k = 0; k < n; k++) quer.Remove(s.Faixas[k].Proximo);   // quem ja tem uma das que valem fica com ela
            for (int k = 0; k < n; k++)
            {
                FaixaCota f = s.Faixas[k];
                if (f.Proximo >= 0 && linhas.IndexOf(f.Proximo) < System.Math.Min(n, linhas.Count - 1)) continue;
                if (quer.Count > 0) { if (s.EscolheCota(k, quer[0])) quer.RemoveAt(0); }
                else if (f.Proximo >= 0) s.EscolheCota(k, -1);
            }
        }
    }
}
