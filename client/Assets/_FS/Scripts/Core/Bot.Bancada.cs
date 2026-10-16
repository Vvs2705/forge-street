using System;

namespace FS.Core
{
    /// <summary>C-CORE-06: politica do bot nas bancadas configuraveis. Regra = a do catalogo (CONTEUDO_V1 §2.3 regra 4) refinada; Volume e Margem
    /// = as duas politicas puras do portao "margem x volume" (BALANCE.md §37): so as receitas do que as familias nao ricas pedem (fivela,
    /// cinzel, castical) ou so as do que os ricos pedem (colar, broche, calice, caixa), enquanto houver alguma liberada; senao, todas.</summary>
    public enum PoliticaBancada { Regra, Volume, Margem }

    public sealed partial class Bot
    {
        public PoliticaBancada Bancadas = PoliticaBancada.Regra;
        /// <summary>Quanto a receita nova tem de faltar a mais que a atual para valer a troca (a troca custa 4 s e a pilha esvaziar). Medido no
        /// BALANCE.md §37.</summary>
        public int Histerese = 3;

        /// <summary>
        /// "Receita com mais clientes na fila; empate, maior ouro por insumo", refinada (decidido por padrao, revisar): para cada bancada sem troca
        /// pendente, falta(p) = unidades que a fila quer de p - estoque de p - o que as OUTRAS bancadas ja tem ou fazem de p (saida + a peca da
        /// vez); a bancada pede a receita liberada de maior falta (empate: maior preco por insumo) quando ela passa a da atual em `Histerese` ou
        /// mais. Sem cliente nenhum, enche a prateleira que estiver mais vazia, a de maior margem primeiro. O bot "toca" sem andar (como o menu).
        /// </summary>
        void ConfiguraBancadas(Sim s)
        {
            if (!s.TemBancadas) return;
            foreach (Station b in s.Stations)
            {
                if (b.Receitas == null || !b.Unlocked || b.Proxima != null) continue;
                bool restrito = false;
                for (int k = 0; k < b.Receitas.Length; k++) restrito |= s.Liberada(b, k) && NaPolitica(s, b.Receitas[k].Output);
                int atual = Falta(s, b, b.Recipe.Output), melhor = -1, nMelhor = int.MinValue; double oMelhor = 0;
                for (int k = 0; k < b.Receitas.Length; k++)
                {
                    RecipeDef r = b.Receitas[k];
                    if (!s.Liberada(b, k) || restrito && !NaPolitica(s, r.Output)) continue;
                    int n = Falta(s, b, r.Output);
                    double o = OuroPorInsumo(s, r);
                    if (n > nMelhor || n == nMelhor && o > oMelhor) { melhor = k; nMelhor = n; oMelhor = o; }
                }
                bool foraDaPolitica = restrito && !NaPolitica(s, b.Recipe.Output);
                if (melhor >= 0 && b.Receitas[melhor] != b.Recipe && (nMelhor >= atual + Histerese || foraDaPolitica)) s.Configura(b, b.Receitas[melhor].Output);
            }
        }

        /// <summary>O produto `p` esta na politica (Regra: todos; Volume: pedido por familia nao rica; Margem: por familia rica com lista).</summary>
        bool NaPolitica(Sim s, Item p)
        {
            if (Bancadas == PoliticaBancada.Regra) return true;
            string id = s.ItemDefs[(int)p].Id;
            foreach (ClientDef c in s.Distrito.Clientes ?? new ClientDef[0])
                if (c.Pede != null && c.Rico == (Bancadas == PoliticaBancada.Margem) && Array.IndexOf(c.Pede, id) >= 0) return true;
            return false;
        }

        static int Falta(Sim s, Station b, Item p)
        {
            int n = -s.Stock[(int)p];
            foreach (Client c in s.Queue) if (c.Want == p) n += Math.Max(1, c.Pack);
            foreach (Station o in s.Stations)
                if (o != b && o.Unlocked && o.Receitas != null && (o.Proxima ?? o.Recipe).Output == p) n -= o.Out + 1;
            return n;
        }

        static double OuroPorInsumo(Sim s, RecipeDef r)
        {
            int q = 0;
            foreach ((Item _, int n) in r.Inputs) q += n;
            return s.PriceOf(r.Output) / (double)Math.Max(1, q);
        }
    }
}
