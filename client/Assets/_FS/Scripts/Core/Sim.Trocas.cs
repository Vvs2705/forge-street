using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Sim em partes (o estado e o construtor ficam no Sim.cs): trocas automaticas (pegar, depositar, vender, pagar pad).</summary>
    public sealed partial class Sim
    {
        // ------------------------------------------------------------------ trocas automaticas

        /// <summary>Estacao desbloqueada cuja zona (boca) contem `p`; `outZone` = boca de SAIDA (so recolhe), senao ENTRADA (so deposita).</summary>
        public Station StationAt(V2 p, out bool outZone)
        {
            Station best = null; float bd = Balance.MouthRadius; outZone = false;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked) continue;
                float dIn = V2.Dist(p, s.InAt), dOut = V2.Dist(p, s.OutAt);
                if (dIn <= bd) { bd = dIn; best = s; outZone = false; }
                if (s.Produces && dOut <= bd) { bd = dOut; best = s; outZone = true; }
            }
            return best;
        }

        public Pad PadAt(V2 p)
        {
            foreach (Pad pad in Pads)
                if (pad.Current(this) >= 0 && V2.Dist(p, pad.Pos) <= Balance.PadRadius) return pad;
            return null;
        }

        /// <summary>Cabe mais um `i` na mao? Teto POR TIPO. Jogador: qualquer tipo, sem bloqueio por misturar (FASE7 §1: mao cheia de
        /// lingote e entrada da bigorna cheia, ele ainda recolhe as espadas da saida). Ajudante: um tipo por vez, so o do papel dele.</summary>
        public bool CanPick(Carrier c, Item i)
        {
            if (c.Held[(int)i] >= c.Cap) return false;
            if (c.Role < 0) return true;
            if (c.Count > 0 && c.Item != i) return false;
            if (c.Role == 0) return Bruto(i);   // C-CORE-01: minerio no Old Forge; minerio ou couro na Row
            if (c.Role == 1) return Intermediario(i);   // C-CORE-01: lingote no Old Forge; lingote, placa e rebite na Row
            if (c.Role == 2) return IsProduct(i);
            if (c.Role == 3) return i == Item.Ingot || i == Item.Jewel;
            return true;
        }

        /// <summary>0 nada, 1 deposita na entrada, 2 pega da saida, 3 pega minerio, 4 abastece o balcao; `item` = o que passa de mao.
        /// Boca de entrada nunca recolhe e boca de saida nunca deposita (FASE5 §2b): da para abastecer sem tirar o que ja ficou pronto.
        /// Entrada recebe so os insumos da receita (B-CORE-01: o 1o da receita que esta na mao e cabe), saida entrega so o OutItem, balcao
        /// recebe os produtos que vende (FASE7 §1, pilha mista).</summary>
        int Action(Carrier c, Station s, bool outZone, out Item item)
        {
            item = s.Kind == Kind.Deposit ? s.OutItem : outZone ? s.OutItem : s.InItem;   // deposito: o bruto dele (C-CORE-01)
            if (c.Role == 3 && s.Index != c.Target) return 0;   // joalheiro so mexe no proprio alvo: nao abastece a bancada do caminho
            switch (s.Kind)
            {
                case Kind.Deposit: return CanPick(c, s.OutItem) ? 3 : 0;
                case Kind.Quota: return AcaoCota(c, out item);   // C-CORE-07: 5 = entrega no patio de cotas
                case Kind.Export: return AcaoExporta(c, out item);   // C-CORE-09: 6 = entrega no lote do Mensageiro
                case Kind.Counter:
                    for (int k = (int)Item.Sword; k < ItemDefs.Length; k++)   // B-CORE-07: todo produto (a espada longa entrou)
                        if (c.Held[k] > 0 && Sells(s, (Item)k) && Stock[k] < CounterCap) { item = (Item)k; return 4; }
                    return 0;
                default:
                    if (outZone) return s.Out > 0 && CanPick(c, s.OutItem) && PegaInsumo(c, s.OutItem) ? 2 : 0;   // row-pendencias: so o que cabe
                    for (int k = 0; k < s.Ins.Length && s.Produces; k++)
                        if (c.Has(s.Recipe.Inputs[k].Item) && s.Ins[k] < s.InCap) { item = s.Recipe.Inputs[k].Item; return 1; }
                    return 0;
            }
        }

        void Interact(Carrier c, float dt)
        {
            if (c.Role < 0)
            {
                Pad pad = PadAt(c.Pos);
                if (pad != _padUnder) { _padUnder = pad; _padDwell = 0f; }
                if (pad == null) foreach (Pad p in Pads) p.Armed = true;
                else
                {
                    _padDwell += dt;
                    if (pad.Armed && (!c.Moving || _padDwell >= Balance.PadDwell)) Drain(pad, dt);
                }
            }
            Station s = StationAt(c.Pos, out bool outZone);
            Item it = Item.Ore;
            int act = s != null ? Action(c, s, outZone, out it) : 0;
            if (act == 0) { c.ActT = 0f; c.IdleT += dt; return; }
            c.IdleT = 0f;
            c.ActT += dt;
            float step = act == 3 ? T.GrabTime : Balance.TransferTime;
            while (c.ActT >= step)
            {
                c.ActT -= step;
                switch (act)
                {
                    // B-CORE-03: a peca passa de mao com o nivel dela, a melhor primeiro (insumo e minerio sao sempre Normal)
                    case 1: Take(ref c.Held[(int)it], c.HeldQ[(int)it]); s.Ins[s.InIndex(it)]++; break;
                    case 2: Put(ref c.Held[(int)it], c.HeldQ[(int)it], Take(ref s.Out, s.OutQ)); break;
                    case 3: c.Held[(int)it]++; break;
                    case 4: Put(ref Stock[(int)it], StockQ[(int)it], Take(ref c.Held[(int)it], c.HeldQ[(int)it])); break;
                    case 5: EntregaCota(c, it); break;   // C-CORE-07: patio (bloco cheio fecha e paga)
                    case 6: EntregaExporta(c, it); break;   // C-CORE-09: lote do Mensageiro
                }
                Emit(act == 2 || act == 3 ? Ev.Picked : Ev.Deposited, (int)it, c.Role, s.Pos);
                if (Action(c, s, outZone, out it) != act) { c.ActT = 0f; break; }   // balcao: o proximo produto da mao segue no mesmo ritmo
            }
        }

        void Drain(Pad pad, float dt)
        {
            int u = pad.Current(this);
            if (u < 0) return;
            long cost = T.Cost(u);
            pad.Acc += cost / Balance.PadDrainSeconds * dt;
            long want = (long)pad.Acc;
            pad.Acc -= want;
            long step = Math.Min(want, Math.Min(Gold, cost - pad.Paid));
            if (step <= 0) return;
            Gold -= step;
            pad.Paid += step;
            if (pad.Paid >= cost) { pad.Paid = 0; pad.Acc = 0; pad.Armed = false; Buy((Upgrade)u); }
        }

        /// <summary>Compra direta (pad completo, teste ou cheat). Idempotente: ja comprado devolve false.</summary>
        public bool Buy(Upgrade u)
        {
            if (Bought[(int)u]) return false;
            if (UpgradeDefs[(int)u].Luxury && (!ProductionComplete || !Bought[UpgradeDefs[(int)u].Requires])) return false;
            if (!TemProjeto((int)u)) return false;   // B-CORE-07: nivel IV-V e bigorna longa so com o projeto
            int workersBefore = Workers.Count;
            Bought[(int)u] = true;
            UpgradeTime[(int)u] = Time;
            UpgradesBought++;
            Recompute();
            Emit(Ev.Bought, (int)u, (int)Math.Min(int.MaxValue, T.Cost(u)), PadOf(u));   // ponytail: evento e' int; acima de 2,1 bi sai no teto (so o diario le)
            foreach (Station s in Stations) if (s.UnlockBy == (int)u) Emit(Ev.Unlocked, (int)s.OutItem, s.Index, s.Pos);
            if (Workers.Count > workersBefore) Emit(Ev.Hired, Workers.Count - 1, 0, Contratacao);
            return true;
        }

        /// <summary>
        /// Compra pelo menu inferior (FASE6): so upgrade InMenu, nao comprado, com o pre-requisito comprado e Gold >= Cost. Cobra
        /// na hora (sem dreno) e aplica pelo MESMO Buy do pad (Ev.Bought na posicao do jogador). Senao devolve false e nada muda.
        /// Fora do Tick o evento entra em Events na hora; o proximo Tick limpa a lista.
        /// </summary>
        public bool TryBuyMenu(Upgrade u)
        {
            long cost = T.Cost(u);
            if (!MenuAvailable((int)u) || Gold < cost || !Buy(u)) return false;
            Gold -= cost;
            return true;
        }

        /// <summary>Upgrade do menu a venda agora (com ou sem ouro): InMenu, nao comprado, pre-requisito comprado e o projeto que ele exige (B-CORE-07).</summary>
        public bool MenuAvailable(int u)
        {
            UpgradeDef d = UpgradeDefs[u];
            return d.InMenu && !Bought[u] && (d.Requires < 0 || Bought[d.Requires]) && TemProjeto(u);
        }

        V2 PadOf(Upgrade u)
        {
            if (UpgradeDefs[(int)u].InMenu) return Player.Pos;   // menu: o efeito sai do jogador, nao do pad antigo
            foreach (Pad p in Pads) foreach (Upgrade c in p.Chain) if (c == u) return p.Pos;
            return Player.Pos;
        }

        /// <summary>Deriva TUDO dos flags Bought (idempotente: vale depois de Buy e depois de Load).</summary>
        public void Recompute()
        {
            // C-CORE-01: os efeitos pelo Has (no Old Forge, Has(u) = Bought[u]: os mesmos numeros, bit a bit)
            float ft = Has(Upgrade.FurnaceSpeed2) ? T.FurnaceTime2 : Has(Upgrade.FurnaceSpeed1) ? T.FurnaceTime1 : T.FurnaceTime0;
            float ht = Has(Upgrade.HammerSpeed) ? T.HammerTime1 : T.HammerTime0;
            foreach (Station s in Stations)
            {
                if (s.UnlockBy >= 0) s.Unlocked = Bought[s.UnlockBy];
                // B-CORE-03: nivel da familia = 1 + niveis comprados dela
                s.Level = 1;
                if (s.Produces) foreach (UpgradeDef d in UpgradeDefs) if (d.Station == s.Recipe.Family && Bought[(int)d.Up]) s.Level++;   // B-CORE-07: pela familia (bigorna longa = bigornas)
                // B-CORE-01: base do tipo (fornalha: Fole; bancada: Martelo veloz) x multiplicador da receita; a Lupa so na Joalheria. B-CORE-03:
                // cada nivel acima do 1 da +5% de velocidade (nivel 1 divide por 1: o tempo de antes, bit a bit)
                if (s.Produces) s.Time = (s.Kind == Kind.Furnace ? ft : ht) * (s.Recipe.TimeMul(T) * (s == JewelBench && Has(Upgrade.JewelSpeed) ? T.JewelSpeedMul : 1f))
                    / (1f + T.NivelEstacaoVelocidade * (s.Level - 1)) / Equipe.Ritmo(s);   // B-CORE-04: oficial e mestre de obras (ninguem = / 1f)
                if (s.Produces) s.OutCap = SaidaBase(s) + Equipe.Saida(s);   // B-CORE-04: almoxarife
            }
            // balcao evolutivo (FASE7 §2): +1 vaga por evolucao comprada; o corpo alarga junto (antes dos Solids)
            QueueCap = T.QueueCap0;
            for (int u = (int)Upgrade.Counter5; u <= (int)Upgrade.Counter8; u++) if (Has((Upgrade)u)) QueueCap++;
            if (JewelShop == null && Has(Upgrade.JewelVitrine)) QueueCap += Balance.JewelQueueCapUp - Balance.JewelQueueCap;   // C-CORE-06: a loja fina 3 -> 5 (vitrine de luxo)
            QueueCap = Math.Min(QueueCap, Balance.QueueCapMax);   // trava de geometria: um QueueCap0 maior no Tuning nao tira o balcao da oficina
            Counter.Half = new V2(Balance.CounterHalfX(QueueCap), Balance.StationHalf.Y);
            // corpos: estacao travada nao e' solida (o pad de compra fica no lugar dela); loja de joias sim (a view desenha a fachada fechada)
            Solids.Clear();
            foreach (Station s in Stations) if (s.Unlocked || s.Kind == Kind.Counter) Solids.Add(s.Body);
            foreach (ObstacleDef o in Distrito.Obstaculos) if (o.Requires < 0 || Bought[o.Requires]) Solids.Add(o.Body);
            MaxX = (Has(Upgrade.SideCorridor) ? Balance.WorldW : Distrito.Largura) - 0.3f;
            Player.Speed = Has(Upgrade.PlayerSpeed) ? Balance.PlayerSpeedUp : Balance.PlayerSpeed;
            Player.Cap = Has(Upgrade.PlayerCapacity) ? Balance.PlayerCapUp : Balance.PlayerCap;
            CounterCap = (Has(Upgrade.CounterCapacity) ? T.CounterCap1 : T.CounterCap0) + Equipe.Estoque;   // B-CORE-04: almoxarife
            JewelQueueCap = Has(Upgrade.JewelVitrine) ? Balance.JewelQueueCapUp : Balance.JewelQueueCap;
            HasConveyor = Has(Upgrade.Conveyor) && EsteiraDe != null && EsteiraPara != null;
            // um ajudante por upgrade de contratacao (papel em _hireRole); a lista so cresce, na ordem da compra (o Joalheiro nao exige o Ajudante 3)
            for (int k = 0; k < _hireBy.Length; k++)
            {
                if (_hireBy[k] < 0 || !Bought[_hireBy[k]]) continue;
                int role = _hireRole[k], want = 0, have = 0;
                for (int j = 0; j <= k; j++) if (_hireRole[j] == role && _hireBy[j] >= 0 && Bought[_hireBy[j]]) want++;
                foreach (Carrier w in Workers) if (w.Role == role) have++;
                if (have < want) Workers.Add(new Carrier(ItemDefs.Length) { Pos = Contratacao, Role = role, Wk = _hireDef[k] });
            }
            bool fast = Has(Upgrade.HelperSpeed);
            foreach (Carrier w in Workers)   // B-CORE-04: nivel do ajudante (nivel 1 = x 1f e + 0)
            {
                w.Speed = (fast ? Balance.WorkerSpeedUp : Balance.WorkerSpeed) * Equipe.Passo(w);
                w.Cap = (fast ? Balance.WorkerCapUp : Balance.WorkerCap) + Equipe.Carga(w);
            }
            RecomputeCotas();   // C-CORE-07: a mesa_real da a 2a vaga de contrato do Imperio
        }
    }
}
