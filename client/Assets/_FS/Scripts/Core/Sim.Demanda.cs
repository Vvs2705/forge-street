using System;

namespace FS.Core
{
    /// <summary>
    /// IA dos ajudantes prioriza a fila (BALANCE.md §49): quanto falta de cada produto para quem esta esperando. Pedido do dono: "os bonecos
    /// param de criar ou pegar itens aleatoriamente e comecam a priorizar a fila de pedidos dos clientes". Antes o papel 2 escolhia a fonte
    /// so por `Out * 10 - distancia` e o papel 1 so olhava o cliente da frente; agora os dois leem a Demanda.
    /// Funcao pura (so le o estado) e sem alocacao: roda nos ajudantes a cada tick, no celular.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Pesos da Demanda: cada unidade que um cliente da fila quer vale 1, +DemandaFrente no 1o da fila e ate +DemandaUrgencia
        /// quando a paciencia acaba; cada peca que falta numa encomenda aberta vale DemandaEncomenda, ate CounterCap pecas por vaga, mas so
        /// com cliente do item na fila (a encomenda so anda com venda; contando sozinha ela tirava o boneco da prateleira: Ideal -2%, §49).</summary>
        public const float DemandaFrente = 0.5f, DemandaUrgencia = 0.5f, DemandaEncomenda = 0.2f;
        /// <summary>Quanto 1 unidade de Demanda pesa na escolha: na fonte do papel 2 (contra Out x 10 - distancia: passa sempre a frente) e no
        /// destino do papel 1 (contra espaco x 4 + prateleira x 2 - distancia).</summary>
        public const float PesoDemandaProduto = 100f, PesoDemandaInsumo = 30f;

        /// <summary>
        /// Quanto falta do produto `it` para quem espera: clientes da fila dele que o querem (o pacote inteiro do VIP e do mercador; o 1o da
        /// fila e quem esta perto de ir embora pesam mais) e, havendo algum, as pecas que faltam nas encomendas abertas que o pedem, menos o estoque no balcao
        /// e o que os ajudantes ja levam na mao. Nunca negativa; 0 = ninguem espera (os ajudantes fazem o de antes: enchem a prateleira).
        /// Insumo (lingote, placa...) e item fora da tabela: 0.
        /// </summary>
        public float Demanda(Item it)
        {
            if (!IsProduct(it)) return 0f;
            int i = (int)it;
            float d = 0f;
            var q = QueueFor(it);
            for (int k = 0; k < q.Count; k++)
            {
                Client c = q[k];
                if (c.Want != it) continue;
                float urg = c.MaxPatience > 0f ? 1f - Math.Max(0f, c.Patience) / c.MaxPatience : 0f;
                d += Math.Max(1, c.Pack) * (1f + (k == 0 ? DemandaFrente : 0f) + DemandaUrgencia * urg);
            }
            if (d <= 0f) return 0f;   // a encomenda so anda com venda: sem cliente do item na fila ela nao pesa (sem fila = o de antes)
            if (Encomendas != null)
                foreach (Encomenda e in Encomendas.Vagas)
                    for (int k = 0; k < e.Itens.Length; k++)
                        if (e.Itens[k] == it) d += DemandaEncomenda * Math.Min(CounterCap, Math.Max(0, e.Alvo[k] - e.Feito[k]));
            int tem = Stock[i];
            foreach (Carrier w in Workers) tem += w.Held[i];
            return Math.Max(0f, d - tem);
        }
    }
}
