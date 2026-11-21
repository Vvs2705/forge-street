using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>
    /// IA dos ajudantes prioriza a fila (BALANCE.md §51, Sim.Demanda.cs). Pedido do dono: "os bonecos param de criar ou pegar itens
    /// aleatoriamente e comecam a priorizar a fila de pedidos dos clientes". Os que mudam de comportamento foram vistos VERMELHOS com o
    /// Sim.Ajudantes.cs de antes (papel 2 por Out x 10 - distancia; papel 1 so com o cliente da frente).
    /// </summary>
    public class AjudantesFilaTests
    {
        const float Dt = 1f / 30f;

        static void Run(Sim s, float seconds) { for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt, 0f, 0f); }

        /// <summary>Old Forge com os 3 ajudantes e os escudos, o jogador num canto e a fila zerada (sem cliente chegando no meio do teste).</summary>
        static Sim Oficina()
        {
            var s = new Sim { Gold = 100000 };
            Assert.IsTrue(s.Buy(Upgrade.Helper1) && s.Buy(Upgrade.Helper2) && s.Buy(Upgrade.Helper3) && s.Buy(Upgrade.Shields));
            s.Player.Pos = new V2(1f, 1f);
            s.Queue.Clear();
            return s;
        }

        static Client Quer(Item it, float paciencia = 600f) => new Client { Want = it, Patience = paciencia, MaxPatience = paciencia };

        [Test]
        public void Demanda_ContaAFila_PesaAFrenteEAUrgencia_TiraOEstoque()
        {
            Sim s = Oficina();
            Assert.AreEqual(0f, s.Demanda(Item.Shield), "sem fila: 0");
            s.Queue.Add(Quer(Item.Shield));
            s.Queue.Add(Quer(Item.Sword));
            Client tarde = Quer(Item.Shield); tarde.Patience = tarde.MaxPatience / 2f;
            s.Queue.Add(tarde);
            // escudo: o da frente 1 + 0,5; o 3o com metade da paciencia 1 + 0,25
            Assert.AreEqual(2.75f, s.Demanda(Item.Shield), 1e-4f);
            s.Stock[(int)Item.Shield] = 1;
            Assert.AreEqual(1.75f, s.Demanda(Item.Shield), 1e-4f, "o estoque do balcao desconta");
            s.Workers.Find(w => w.Role == 2).Held[(int)Item.Shield] = 2;
            Assert.AreEqual(0f, s.Demanda(Item.Shield), "o que o ajudante ja leva desconta; nunca negativa");
            Assert.AreEqual(1f, s.Demanda(Item.Sword), 1e-4f, "a espada no 2o lugar: 1");
            Assert.AreEqual(0f, s.Demanda(Item.Ingot), "insumo nao tem demanda");
        }

        /// <summary>A bigorna com a saida cheia de espadas (Out 3) e 1 escudo pronto; o cliente da fila quer escudo e nao ha nenhum no
        /// balcao. Antes: Out x 10 - distancia mandava buscar espada (30 contra 10) e o cliente esperava. Agora: o escudo.</summary>
        [Test]
        public void Papel2_BuscaOProdutoQueOClienteEspera_NaoOMaisEstocado()
        {
            Sim s = Oficina();
            Carrier w = s.Workers.Find(c => c.Role == 2);
            s.AnvilA.Out = 3; s.ShieldBench.Out = 1;
            s.Stock[(int)Item.Sword] = 1;
            s.Queue.Add(Quer(Item.Shield));
            Run(s, Dt);
            Assert.AreEqual(s.ShieldBench.Index, w.Target, "vai a bancada de escudos");
            for (int i = 0; i < 600 && w.Held[(int)Item.Shield] == 0; i++) Run(s, Dt);
            Assert.AreEqual(0, w.Held[(int)Item.Sword], "nao pegou espada");
            Assert.Greater(w.Held[(int)Item.Shield], 0, "pegou o escudo");
            for (int i = 0; i < 900 && s.SoldItems[(int)Item.Shield] == 0; i++) Run(s, Dt);
            Assert.AreEqual(1, s.SoldItems[(int)Item.Shield], "o cliente do escudo foi atendido");
        }

        /// <summary>Lingote na mao do papel 1; o da frente quer espada e 3 atras querem escudo (balcao vazio). Antes so o da frente pesava
        /// (+12 a bigorna); agora a bancada cujo produto tem mais demanda.</summary>
        [Test]
        public void Papel1_LevaLingoteABancadaDoProdutoMaisPedido()
        {
            Sim s = Oficina();
            Carrier w = s.Workers.Find(c => c.Role == 1);
            w.Held[(int)Item.Ingot] = w.Cap;
            s.Queue.Add(Quer(Item.Sword));
            for (int i = 0; i < 3; i++) s.Queue.Add(Quer(Item.Shield));
            Run(s, Dt);
            Assert.AreEqual(s.ShieldBench.Index, w.Target, "leva o lingote a bancada de escudos");
        }

        /// <summary>Sem fila e sem encomenda (Demanda 0 em tudo): o de antes. Papel 2 busca a maior pilha (Out x 10 - distancia) e o papel 1
        /// leva o lingote a bancada com a prateleira mais vazia.</summary>
        [Test]
        public void SemFila_ComportamentoDeAntes()
        {
            Sim s = Oficina();
            Carrier p2 = s.Workers.Find(c => c.Role == 2), p1 = s.Workers.Find(c => c.Role == 1);
            s.AnvilA.Out = 3; s.ShieldBench.Out = 1;
            p1.Held[(int)Item.Ingot] = p1.Cap;
            s.Stock[(int)Item.Sword] = 0; s.Stock[(int)Item.Shield] = s.CounterCap - 1;   // prateleira de espada vazia: (CounterCap) x 2 a mais
            Run(s, Dt);
            Assert.AreEqual(0, s.Encomendas.Vagas.Count, "sem encomenda (abre depois da 1a venda)");
            Assert.AreEqual(0f, s.Demanda(Item.Sword) + s.Demanda(Item.Shield));
            Assert.AreEqual(s.AnvilA.Index, p2.Target, "papel 2: a bigorna, que tem mais pecas prontas");
            Assert.AreEqual(s.AnvilA.Index, p1.Target, "papel 1: a bigorna, cuja prateleira esta mais vazia");
        }

        /// <summary>Histerese: indo buscar espada sem ninguem esperando, chega um cliente de escudo: troca 1x para o escudo e nao volta,
        /// mesmo com um cliente de espada chegando depois (o alvo com demanda so cai quando fica invalido). Antes: nunca trocava.</summary>
        [Test]
        public void Papel2_TrocaUmaVezQuandoChegaDemanda_ENaoFicaIndoEVoltando()
        {
            Sim s = Oficina();
            Carrier w = s.Workers.Find(c => c.Role == 2);
            s.AnvilA.Out = 3; s.ShieldBench.Out = 1;
            s.ShieldBench.Busy = true; s.ShieldBench.Progress = 0f;
            Run(s, Dt);
            Assert.AreEqual(s.AnvilA.Index, w.Target, "sem fila: a bigorna");
            s.Queue.Add(Quer(Item.Shield));
            int trocas = 0, alvo = w.Target;
            for (int i = 0; i < 90 && w.Count == 0; i++)
            {
                if (i == 20) { s.Queue.Add(Quer(Item.Sword)); s.Queue.Add(Quer(Item.Sword)); }
                Run(s, Dt);
                if (w.Target != alvo) { trocas++; alvo = w.Target; }
            }
            Assert.AreEqual(1, trocas, "uma troca so");
            Assert.AreEqual(s.ShieldBench.Index, alvo, "e para o escudo");
        }

        /// <summary>A encomenda so pesa com cliente do item na fila (ela so anda com venda): sem fila, nada muda; com o cliente, soma 0,2 por
        /// peca que falta (ate a prateleira).</summary>
        [Test]
        public void Demanda_EncomendaSoPesaComClienteNaFila()
        {
            Sim s = Oficina();
            var def = new OrderDef("teste_escudos", OrderType.Common, null, 60f, 1, 50, 60f, 1, 1);
            s.Encomendas.Vagas.Add(new Encomenda(def, 0, new[] { Item.Shield }, new[] { 50 }, 0L, 0L, 0f, null));
            Assert.AreEqual(0f, s.Demanda(Item.Shield), "sem cliente: a encomenda sozinha nao tira o boneco da prateleira");
            s.Queue.Add(Quer(Item.Shield));
            Assert.AreEqual(1.5f + Sim.DemandaEncomenda * s.CounterCap, s.Demanda(Item.Shield), 1e-4f);
        }
    }
}
