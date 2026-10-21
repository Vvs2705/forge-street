namespace FS.Core
{
    /// <summary>C-CORE-08: politica do bot nos altares (CONTEUDO_V1 §2.5; BALANCE.md §40). Regra = a regra fixa abaixo; Estavel e Forcado = as
    /// duas politicas puras do portao "nenhum modo domina" (CONTEUDO_V1 §16: renda do Forcado entre 0,9x e 1,15x a do Estavel).</summary>
    public enum PoliticaArcana { Regra, Estavel, Forcado }

    public sealed partial class Bot
    {
        public PoliticaArcana Altares = PoliticaArcana.Regra;

        /// <summary>
        /// A regra fixa (decidida por padrao, revisar): "Forcado enquanto a proxima peca nao sobrecarrega". Em cada altar aberto, o modo da
        /// proxima peca e' Forcado se E + 2 x carga cabe no teto (100), senao Estavel: o bot le o medidor (o jogador ve a barra). Vale tambem
        /// com o altar trabalhando (o modo e' o da proxima peca; ate ela comecar E so cai): nunca entra em sobrecarga; com dreno de sobra
        /// (condensadores, Isis) fica no Forcado. O bot "toca" sem andar (como o menu). Deterministico. Sem altar: nada.
        /// </summary>
        void Arcano(Sim s)
        {
            if (!s.TemArcano) return;
            foreach (Station a in s.Stations)
            {
                if (!a.Unlocked || !s.Arcana(a)) continue;
                bool f = Altares == PoliticaArcana.Forcado || Altares == PoliticaArcana.Regra && a.Medidor + 2 * s.CargaPeca(a) <= s.T.ArcanoTeto;
                s.ModoArcano(a, f);
            }
        }
    }
}
