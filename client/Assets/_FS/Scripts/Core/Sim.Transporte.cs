namespace FS.Core
{
    /// <summary>
    /// row-pendencias (BALANCE.md §39): o papel 1 dos ajudantes num distrito com 2+ insumos feitos em estacao (a Row: lingote, placa,
    /// rebite e liga). Medido na Row (F2P, 150 min): o Ferraz do papel 1 passava 30-70% do tempo parado com placas na mao (toda montagem
    /// com a entrada de placas cheia, esperando rebite) e ate 40 min seguidos esperando na boca de uma estacao parada (a fornalha de liga
    /// sem lingote), enquanto as fornalhas ficavam ~5.900 s travadas. Duas regras, so com 2+ insumos (o Old Forge tem um, o lingote: la
    /// esperar com ele na mao nao prende outro tipo, e o jogo fica ao byte):
    /// 1. so pega o que cabe nas entradas abertas que usam o insumo (e nao vai a fonte do que nao cabe em lugar nenhum);
    /// 2. so espera na fonte alvo enquanto ela trabalha (parada e sem saida = escolhe outra).
    /// </summary>
    public sealed partial class Sim
    {
        int _insumos = -1;   // quantos itens sao Intermediario (montado no 1o uso: o _bruto sai das estacoes do construtor)

        bool VariosInsumos
        {
            get
            {
                if (_insumos < 0) { _insumos = 0; for (int i = 0; i < ItemDefs.Length; i++) if (Intermediario((Item)i)) _insumos++; }
                return _insumos >= 2;
            }
        }

        /// <summary>Quantos `i` ainda cabem nas entradas das estacoes abertas que o usam.</summary>
        int Cabe(Item i)
        {
            int n = 0;
            foreach (Station d in Stations) { int k = d.InIndex(i); if (d.Unlocked && k >= 0) n += d.InCap - d.Ins[k]; }
            return n;
        }

        /// <summary>Regra 1 na boca de saida: o ajudante do papel 1 pega mais um `i`? (os outros papeis e o jogador: sempre)</summary>
        bool PegaInsumo(Carrier c, Item i) => c.Role != 1 || !VariosInsumos || c.Held[(int)i] < Cabe(i);

        /// <summary>Regras 1 e 2 na escolha da fonte do papel 1 (os outros papeis: sempre).</summary>
        bool FonteDeInsumo(Carrier w, Station s) => w.Role != 1 || !VariosInsumos || ((w.Count > 0 || Cabe(s.OutItem) > 0) && (s.Out > 0 || s.Busy));
    }
}
