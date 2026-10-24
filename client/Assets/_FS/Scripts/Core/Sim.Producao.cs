using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Sim em partes (o estado e o construtor ficam no Sim.cs): producao, esteira e clientes.</summary>
    public sealed partial class Sim
    {
        // ------------------------------------------------------------------ producao, esteira, clientes

        void Produce(Station s, float dt)
        {
            if (!s.Unlocked || !s.Produces) return;
            if (s.Busy)
            {
                if (s.Progress < 1f) s.Progress = Math.Min(1f, s.Progress + dt / s.Time);
                if (s.Progress >= 1f)
                {
                    if (s.Out + s.Recipe.Qtd <= s.OutCap)   // C-CORE-01: o ciclo inteiro cabe na saida (rebitadeira: 4 de uma vez; Qtd 1 = Out < OutCap)
                    {
                        s.Busy = false; s.Progress = 0f; s.Blocked = false; s.Crafted++; Crafted[(int)s.OutItem] += s.Recipe.Qtd;
                        Quality l = Grade(s);   // B-CORE-03: acumulador e golpe
                        if (l == Quality.Masterwork) { Masterworks++; Hall.Registra(s); }   // vai ao Hall (B-CORE-08): nunca a pilha nem o balcao
                        else
                        {
                            Put(ref s.Out, s.OutQ, l);
                            for (int k = 1; k < s.Recipe.Qtd; k++) s.Out++;   // as outras pecas do ciclo: insumo, sempre Normal (Grade so da nivel a produto)
                            if (l > Quality.Normal) Emit(Ev.Graded, s.Index, (int)l, s.Pos);
                        }
                        Emit(Ev.Crafted, (int)s.OutItem, s.Index, s.Pos);
                        TerminaArcano(s);   // C-CORE-08: energizada, sequencia e sobrecarga do altar (Sim.Arcano.cs; fora de altar, nada)
                    }
                    else Stall(s, dt);
                }
            }
            if (s.Busy) return;
            if (Trocando(s, dt)) return;   // C-CORE-06: bancada configuravel trocando de receita (Sim.Bancada)
            for (int k = 0; k < s.Ins.Length; k++) if (s.Ins[k] < s.Recipe.Inputs[k].Qty) { s.StarveSeconds += dt; return; }   // so com a receita inteira
            if (s.Out + s.Recipe.Qtd > s.OutCap) { Stall(s, dt); return; }
            if (EsperaArcano(s)) return;   // C-CORE-08: altar Estavel sem espaco no medidor, ou em sobrecarga (Sim.Arcano.cs)
            for (int k = 0; k < s.Ins.Length; k++) s.Ins[k] -= s.Recipe.Inputs[k].Qty;
            s.Busy = true; s.Progress = 0f;
            ComecaArcano(s);   // C-CORE-08: a carga entra no medidor
        }

        void Stall(Station s, float dt)
        {
            s.StallSeconds += dt;
            if (s.Blocked) return;
            s.Blocked = true;
            Emit(Ev.Bottleneck, s.Index, 0, s.Pos);
        }

        void Conveyor(float dt)
        {
            if (!HasConveyor) return;
            _convT += dt;
            while (_convT >= T.ConveyorTime)
            {
                _convT -= T.ConveyorTime;
                Station a = EsteiraDe, b = EsteiraPara;   // C-CORE-01: as pontas do DistrictDef (Old Forge: Fornalha -> Bigorna)
                if (a.Out <= 0 || !b.Accepts(a.OutItem)) { _convT = 0f; break; }
                a.Out--; b.Ins[b.InIndex(a.OutItem)]++;
                Emit(Ev.Deposited, (int)a.OutItem, 9, b.Pos);   // B = 9: veio pela esteira
            }
        }

        void Clients(float dt)
        {
            // FASE8: o relogio do VIP corre so depois da 1a venda; no zero sorteia o VIP se nao houver outro ativo (senao segura em 0 ate ele
            // sair). O sorteado pega a proxima vaga livre ANTES de quem chega neste tick: com a fila cheia ele espera, nao some.
            // Flag Vip desligada: o relogio para e nao sorteia; o VIP que ja estava sorteado (pendente do save) ainda entra e leva o pacote
            if (T.Vip && FirstSaleTime >= 0f)
            {
                VipIn = Math.Max(0f, VipIn - dt);
                if (VipIn <= 0f && !VipActive) SpawnVip(false);
            }
            if (_vipWant >= 0 && Queue.Count < QueueCap)
            {
                float pat = T.VipPatienceFor(_vipPack);   // pacote maior, mais tempo: o humano e' mais lento que o bot
                Queue.Add(new Client { Want = (Item)_vipWant, Patience = pat, MaxPatience = pat, Vip = true, Summoned = _vipSummoned, Pack = _vipPack, Paid = _vipPaid });
                if (!_vipQuiet) Emit(Ev.VipArrived, _vipWant, _vipPack, ClientSlot(Queue.Count - 1));
                _vipWant = -1; _vipPaid = 0; _vipQuiet = false; _vipSummoned = false;
            }
            if (Distrito.Clientes != null) ChegamFamilias(dt);   // C-CORE-06: um relogio por familia (Sim.Clientes) no lugar do por produto
            else for (int k = (int)Item.Sword; k < ItemDefs.Length; k++)   // B-CORE-07: toda linha aberta com cliente (a espada longa entrou)
            {
                Item p = (Item)k;
                if (!LineUnlocked(p) || !HasClients(p)) continue;
                _clientT[(int)p] -= dt * BoostMul;   // FASE8: a demanda acompanha o boost
                if (_clientT[(int)p] > 0f) continue;
                _clientT[(int)p] += ClientInterval(p);
                bool j = Joia(p);
                List<Client> q = QueueFor(p);
                int cap = j ? JewelQueueCap : QueueCap;
                float patience = T.PatienceFor(p, RecipeDefs) * (j ? 1f : Equipe.Paciencia);   // B-CORE-04: balconista (so o balcao principal)
                if (q.Count < cap)
                {
                    q.Add(new Client { Want = p, Patience = patience, MaxPatience = patience });
                    Emit(Ev.ClientArrived, (int)p, q.Count, Slot(j, q.Count - 1));
                }
                // fila cheia: vai embora sem comprar. FASE7 §3: a "compra direta da vitrine" (BALANCE §9) saiu, porque vendia sem cliente
                // visivel ("+10" numa vaga vazia; 28-30% das vendas do balcao em 45 min). A trava que ela resolvia agora se desfaz pela
                // paciencia por item, pelas vagas do balcao evolutivo e pela carga mista do ferreiro (CoreTests.FilaCheia_...)
                else { ClientsTurnedAway++; Emit(Ev.ClientLeft, (int)p, 2, Slot(j, cap)); }
            }
            if (Queue.Count > MaxQueue) MaxQueue = Queue.Count;
            Serve(Queue, false, ref _serveT, dt);
            Serve(JewelQueue, true, ref _serveJ, dt);   // a loja de joias tem o proprio atendente (timer separado)
        }

        V2 Slot(bool jewel, int i) => jewel ? JewelSlot(i) : ClientSlot(i);

        public int PriceOf(Item item) => Joia(item) && Has(Upgrade.JewelVitrine) ? T.JewelPriceUp : T.Price[(int)item];

        /// <summary>Vende 1 `want` do balcao: a melhor qualidade primeiro (B-CORE-03), ao PriceOf do nivel x `mul` (VIP = 3x). Masterwork nunca
        /// chega aqui (nao entra em lote: Grade a manda ao Hall).</summary>
        int Sell(Item want, V2 pos, int mul = 1)
        {
            Quality l = Take(ref Stock[(int)want], StockQ[(int)want]);
            Renome.OnVendaQualidade((int)l);   // B-CORE-06: +1 a cada 10 Superiores vendidas
            Projetos.OnVenda((int)want, l);    // B-CORE-07: marco "N Fina ou melhor"
            Colecao.OnVenda((int)want, l);     // B-CORE-08: aba Produtos (item x qualidade)
            int price = PriceOf(want, l) * mul;
            QualityGold += price - PriceOf(want, Quality.Normal) * mul;
            Gold += price; GoldEarned += price; Sales++; SoldItems[(int)want]++;
            Encomendas.OnVenda(want, l);   // B-CORE-05: conta para a vaga mais antiga que aceita a peca
            Contratos.OnVenda(ItemDefs[(int)want].Id, l);    // D-CORE-04: e para a etapa atual do contrato ativo (o da raiz)
            if (FirstSaleTime < 0f) FirstSaleTime = Time;
            Emit(Ev.Sold, (int)want, price, pos);
            return price;
        }

        /// <summary>
        /// Baus de marco (docs/archive/AREA2_FASE2.md §3): o jogador em cima de um disponivel abre e ganha o ouro (uma vez: State 2
        /// vai no save); um marco batido so vira bau quando nao ha outro disponivel no mesmo lugar (um por vez na tela), e
        /// entao emite Milestone. O ouro do bau NAO entra em GoldEarned (nao infla a taxa online nem o cofre offline).
        /// Flag Baus desligada: nenhum bau novo aparece; o que ja estava na tela ainda abre.
        /// </summary>
        void Milestones()
        {
            foreach (Chest c in Chests)
            {
                if (c.State != 1 || V2.Dist(Player.Pos, c.Pos) > Balance.PadRadius) continue;
                c.State = 2; Gold += c.Gold;
                Emit(Ev.ChestOpened, c.Index, c.Gold, c.Pos);
            }
            if (!T.Baus) return;
            foreach (Chest c in Chests)
            {
                MilestoneDef m = Distrito.Marcos[c.Index];
                if (c.State != 0 || (m.Item < 0 ? Sales : SoldItems[m.Item]) < m.Count || !ChestSpotFree(c.Pos)) continue;
                c.State = 1;
                Emit(Ev.Milestone, c.Index, c.Gold, c.Pos);
            }
        }

        /// <summary>Lugar livre para um bau novo: nenhum outro disponivel ali e o jogador fora de cima (senao o proximo abria no tick seguinte, sem ser visto).</summary>
        bool ChestSpotFree(V2 pos)
        {
            if (V2.Dist(Player.Pos, pos) <= Balance.PadRadius) return false;
            foreach (Chest c in Chests) if (c.State == 1 && V2.Dist(c.Pos, pos) < 0.01f) return false;
            return true;
        }

        void Serve(List<Client> q, bool jewel, ref float serveT, float dt)
        {
            serveT -= dt;
            if (serveT <= 0f)
            {
                // atende o primeiro da fila que da para atender: um cliente de escudo sem escudo no balcao nao trava quem quer espada
                for (int i = 0; i < q.Count; i++)
                {
                    Client c = q[i];
                    if (Stock[(int)c.Want] <= 0 || !TemParaCliente(c)) continue;   // C-CORE-06: o colecionador so leva Fina ou melhor
                    serveT = jewel ? Balance.ServeTime : Equipe.Atendimento;   // B-CORE-04: balconista no balcao principal
                    if (c.Vip)   // FASE8: uma unidade por atendimento a 3x o preco; fica na vaga ate levar o pacote todo
                    {
                        c.Paid += Sell(c.Want, Slot(jewel, i), T.VipPriceMul);
                        if (--c.Pack > 0) break;
                        q.RemoveAt(i);
                        if (c.Summoned) VipCooldown = T.Ad(Anuncio.Vip).Recarga;   // P14: a recarga conta de quando o chamado sai
                        Emit(Ev.VipServed, (int)c.Want, c.Paid, Slot(jewel, i));
                        break;
                    }
                    if (c.Pack > 1) { Sell(c.Want, Slot(jewel, i)); c.Pack--; break; }   // C-CORE-06: o mercador leva 2, 1 por atendimento
                    q.RemoveAt(i);
                    Sell(c.Want, Slot(jewel, i));
                    Atendido(c);   // C-CORE-06: RichClients
                    break;
                }
            }
            for (int i = q.Count - 1; i >= 0; i--)
            {
                Client c = q[i];
                c.Patience -= dt;
                if (c.Patience > 0f) continue;
                if (c.Vip) Emit(Ev.VipLeft, (int)c.Want, c.Pack, Slot(jewel, i));   // FASE8: o VIP fica fora do ClientsLost (metrica dos clientes normais)
                else { Emit(Ev.ClientLeft, (int)c.Want, 0, Slot(jewel, i)); ClientsLost++; }
                if (c.Summoned) VipCooldown = T.Ad(Anuncio.Vip).Recarga;   // P14: cansou tambem conta (sem isso, deixar ir embora furava a recarga)
                q.RemoveAt(i);
            }
        }

        internal void Emit(Ev kind, int a, int b, V2 pos)   // internal: a Equipe (B-CORE-04) emite a contratacao e o nivel; o Projetos (B-CORE-07), o Ev.Blueprint
        {
            Events.Add(new SimEvent(kind, a, b, pos));
            Metas.On(kind, a, b);   // D-CORE-02: tambem os de fora do Tick (menu, cofre)
            Renome.On(kind, a, b);  // B-CORE-06: bau aberto (a encomenda credita pelo tipo no Encomendas.Entregar)
            LiveOps.On(kind, a, b); // D-CORE-03/04: missoes do evento ativo e XP de encomenda do Pass
            // C-CORE-03: venda, VIP, encomenda e cofre de um distrito contam tambem nas metas, no evento e no Pass do Imperio (os da raiz), 1x;
            // o contador do proprio distrito segue (o Hall dele le). ponytail: compra, contratacao, qualidade e obra-prima ficam so no distrito
            // (indice de upgrade, worker, estacao e Hall nao traduzem para a raiz); entram com uma meta/missao que os peca por distrito
            if (Heroi != null && (kind == Ev.Sold || kind == Ev.VipServed || kind == Ev.OrderDone || kind == Ev.Offline))
            {
                int r = kind == Ev.Offline ? a : NaRaiz(a);
                Heroi.Metas.On(kind, r, b);
                if (r < 0 && kind == Ev.Sold) Heroi.Metas.RegistraItem("product_sold", ItemDefs[a].Id);   // raia H: o elmo conta pelo ID (ach_elmos100)
                Heroi.LiveOps.On(kind, r, b);
            }
        }

        /// <summary>C-CORE-03: o item `item` deste distrito na tabela da raiz, pelo ID (o que o Old Forge nao tem = -1: conta no total da meta ou
        /// missao, sem filtro de item).</summary>
        int NaRaiz(int item) => Items.IndexOf(ItemDefs[item].Id, Heroi.ItemDefs);
    }
}
