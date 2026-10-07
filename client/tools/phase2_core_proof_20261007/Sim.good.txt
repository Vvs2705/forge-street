using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    public struct SimEvent
    {
        public Ev Kind; public int A, B; public V2 Pos;
        public SimEvent(Ev kind, int a, int b, V2 pos) { Kind = kind; A = a; B = b; Pos = pos; }
    }

    public sealed class Station
    {
        public int Index; public Kind Kind; public V2 Pos; public string Name;
        public Item InItem, OutItem; public int Need = 1;
        public int UnlockBy = -1; public bool Unlocked = true;
        public int In, Out, InCap, OutCap;
        public float Time = 1f, Progress; public bool Busy, Blocked;
        public int Crafted;
        public float StarveSeconds, StallSeconds;   // metricas de gargalo (GDD §14 bottleneck_seconds)
        public bool Produces => Kind == Kind.Furnace || Kind == Kind.Crafter;
    }

    /// <summary>Jogador (Role = -1) ou ajudante (Role 0 minerio, 1 lingote, 2 produto, 3 joalheiro). Pilha homogenea.</summary>
    public sealed class Carrier
    {
        public V2 Pos; public float Speed; public int Cap; public int Role = -1;
        public Item Item; public int Count;
        public float ActT, IdleT; public bool Moving;
        public int State;            // ajudante: 0 buscando, 1 entregando
        public int Target = -1;      // ajudante: indice da estacao alvo (-1 = parado)
        public bool Has(Item i) => Count > 0 && Item == i;
    }

    public sealed class Client
    {
        public Item Want; public float Patience, MaxPatience;
    }

    /// <summary>Bau de marco (um por Balance.Milestones, mesmo Index). State: 0 nao batido, 1 disponivel, 2 aberto.</summary>
    public sealed class Chest
    {
        public int Index, State, Gold; public V2 Pos; public string Label;
    }

    /// <summary>Pad de upgrade: ficar em cima despeja ouro ate completar o preco. Um slot tem uma cadeia (ex.: Ajudante 1/2/3).</summary>
    public sealed class Pad
    {
        public int Slot; public V2 Pos; public Upgrade[] Chain; public int Paid; public double Acc;
        public bool Armed = true;   // depois de comprar, so volta a cobrar quando o jogador sai de cima (nao engole o proximo nivel)

        /// <summary>Upgrade a venda neste pad agora, ou -1 (cadeia esgotada ou pre-requisito faltando = pad invisivel).</summary>
        public int Current(Sim s)
        {
            foreach (Upgrade u in Chain)
            {
                if (s.Bought[(int)u]) continue;
                int req = Upgrades.All[(int)u].Requires;
                return req < 0 || s.Bought[req] ? (int)u : -1;
            }
            return -1;
        }
    }

    /// <summary>
    /// Simulacao deterministica da oficina, em metros e segundos, sem UnityEngine. Um `Tick(dt, inX, inY)` move o
    /// jogador, pensa os ajudantes, faz as trocas automaticas (pega/deposita/vende/paga pad), produz, anda a esteira,
    /// atende clientes e atualiza as metricas. A view le `Events` depois de cada tick. Nao ha aleatoriedade:
    /// mesma entrada, mesmo resultado (BalanceTests.Determinismo).
    /// </summary>
    public sealed class Sim
    {
        public float Time;
        public int Gold;
        public readonly bool[] Bought = new bool[Upgrades.Count];
        public readonly float[] UpgradeTime = new float[Upgrades.Count];
        public readonly List<Station> Stations = new List<Station>();
        public readonly List<Pad> Pads = new List<Pad>();
        public readonly Carrier Player = new Carrier { Pos = new V2(4.5f, 3.5f), Speed = Balance.PlayerSpeed, Cap = Balance.PlayerCap };
        public readonly List<Carrier> Workers = new List<Carrier>();
        public readonly List<Client> Queue = new List<Client>();        // fila do balcao principal (espada/escudo/ferramenta)
        public readonly List<Client> JewelQueue = new List<Client>();   // fila da loja de joias (2a area)
        public readonly int[] Stock = new int[6];                        // por Item; Stock[Jewel] fica na loja de joias
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public int CounterCap = Balance.CounterCap0, QueueCap = Balance.QueueCap0, JewelQueueCap = Balance.JewelQueueCap;
        public bool HasConveyor;
        public readonly List<Chest> Chests = new List<Chest>();          // baus de marco, na ordem de Balance.Milestones
        public readonly int[] SoldItems = new int[6];                    // vendas por Item (marcos "10 espadas", "10 joias")
        public float MaxX = Balance.WorkshopW - 0.3f;   // limite direito do jogador: a oficina ate o Corredor, o mapa inteiro depois

        // metricas de playtest (GDD §14/§15)
        public float FirstSaleTime = -1f, WalkNoDecision;
        public int Sales, GoldEarned, OfflineEarned, ClientsLost, ClientsTurnedAway, MaxQueue, UpgradesBought;
        public readonly int[] Crafted = new int[6];
        public double RateEma;                 // ouro/s online, media movel (base do offline)
        public long SavedAt, LastClaim;        // unix s; o claim offline e' idempotente por SavedAt

        public readonly Station Deposit, FurnaceA, FurnaceB, AnvilA, AnvilB, ShieldBench, ToolBench, Counter, JewelBench, JewelShop;
        public static readonly V2 HireSpot = new V2(8.3f, 1.5f);
        static readonly Upgrade[] HireBy = { Upgrade.Helper1, Upgrade.Helper2, Upgrade.Helper3, Upgrade.Jeweler };   // papel 0..3

        readonly float[] _clientT = { 0, 0, Balance.FirstClientAt, Balance.ClientInterval[3], Balance.ClientInterval[4], Balance.ClientInterval[5] };
        float _serveT, _serveJ, _convT, _padDwell;
        Pad _padUnder;

        public Sim()
        {
            for (int i = 0; i < Upgrades.Count; i++) UpgradeTime[i] = -1f;
            Deposit = Add(Kind.Deposit, 1.5f, 1.5f, "Depósito");
            FurnaceA = Add(Kind.Furnace, 1.5f, 5.5f, "Fornalha");
            FurnaceB = Add(Kind.Furnace, 4.5f, 5.5f, "Fornalha 2", (int)Upgrade.Furnace2);
            AnvilA = Add(Kind.Crafter, 1.5f, 9.5f, "Bigorna", -1, Item.Sword);
            AnvilB = Add(Kind.Crafter, 4.5f, 9.5f, "Bigorna 2", (int)Upgrade.Anvil2, Item.Sword);
            ShieldBench = Add(Kind.Crafter, 7.5f, 9.5f, "Escudos", (int)Upgrade.Shields, Item.Shield);
            ToolBench = Add(Kind.Crafter, 7.5f, 5.5f, "Ferramentas", (int)Upgrade.Tools, Item.Tool);
            Counter = Add(Kind.Counter, 4.5f, 13f, "Balcão");
            // 2a area: ANEXADAS ao fim (indices 8 e 9) para os indices e o save antigos nao mudarem
            JewelBench = Add(Kind.Crafter, 12f, 6.5f, "Joalheria", (int)Upgrade.Jewelry, Item.Jewel);
            JewelShop = Add(Kind.Counter, 12f, 11.5f, "Loja de joias", (int)Upgrade.Jewelry);

            // Pads fora das linhas de caminhada (coluna x=1,5 e diagonais ate o balcao): atravessar um pad nao pode gastar.
            Slot(0.5f, 6.5f, Upgrade.FurnaceSpeed1, Upgrade.FurnaceSpeed2);
            Slot(FurnaceB.Pos.X, FurnaceB.Pos.Y, Upgrade.Furnace2);
            Slot(AnvilB.Pos.X, AnvilB.Pos.Y, Upgrade.Anvil2);
            Slot(ShieldBench.Pos.X, ShieldBench.Pos.Y, Upgrade.Shields);
            Slot(ToolBench.Pos.X, ToolBench.Pos.Y, Upgrade.Tools);
            Slot(HireSpot.X, HireSpot.Y, Upgrade.Helper1, Upgrade.Helper2, Upgrade.Helper3);
            Slot(6.5f, 0.6f, Upgrade.HelperSpeed);
            Slot(4.5f, 0.6f, Upgrade.PlayerCapacity);
            Slot(2.8f, 0.6f, Upgrade.PlayerSpeed);
            Slot(0.5f, 7.8f, Upgrade.Conveyor);
            Slot(0.5f, 9.5f, Upgrade.HammerSpeed);
            Slot(6.5f, 13f, Upgrade.CounterCapacity);
            Slot(8.4f, 11.6f, Upgrade.SideCorridor);                 // 2a area, pads 12 e 13: lado direito da oficina, fora das linhas
            Slot(JewelBench.Pos.X, JewelBench.Pos.Y, Upgrade.Jewelry);
            Slot(14.2f, 8.5f, Upgrade.Jeweler);                      // fase 2, pads 14-16: rua lateral, fora da linha bancada -> loja (x=12)
            Slot(10.2f, 6.5f, Upgrade.JewelSpeed);
            Slot(14.2f, 11.5f, Upgrade.JewelVitrine);
            foreach (MilestoneDef m in Balance.Milestones) Chests.Add(new Chest { Index = Chests.Count, Pos = m.Pos, Gold = m.Gold, Label = m.Label });
            Recompute();
        }

        Station Add(Kind kind, float x, float y, string name, int unlockBy = -1, Item product = Item.Sword)
        {
            var s = new Station { Index = Stations.Count, Kind = kind, Pos = new V2(x, y), Name = name, UnlockBy = unlockBy, Unlocked = unlockBy < 0 };
            if (kind == Kind.Furnace) { s.InItem = Item.Ore; s.OutItem = Item.Ingot; s.InCap = Balance.FurnaceIn; s.OutCap = Balance.FurnaceOut; }
            if (kind == Kind.Crafter) { s.InItem = Item.Ingot; s.OutItem = product; s.Need = Balance.IngotsPer[(int)product]; s.InCap = Balance.CrafterIn; s.OutCap = Balance.CrafterOut; }
            Stations.Add(s);
            return s;
        }

        void Slot(float x, float y, params Upgrade[] chain) => Pads.Add(new Pad { Slot = Pads.Count, Pos = new V2(x, y), Chain = chain });

        public static bool IsProduct(Item i) => i >= Item.Sword;
        public bool LineUnlocked(Item product) => product switch
        {
            Item.Sword => true,
            Item.Shield => Bought[(int)Upgrade.Shields],
            Item.Tool => Bought[(int)Upgrade.Tools],
            Item.Jewel => Bought[(int)Upgrade.Jewelry],
            _ => false
        };
        /// <summary>O balcao `counter` vende `i`? O principal vende espada/escudo/ferramenta; a loja de joias, so joia.</summary>
        public bool Sells(Station counter, Item i) => counter.Kind == Kind.Counter && IsProduct(i) && (counter == JewelShop) == (i == Item.Jewel);
        public Station CounterFor(Item product) => product == Item.Jewel ? JewelShop : Counter;
        public List<Client> QueueFor(Item product) => product == Item.Jewel ? JewelQueue : Queue;
        /// <summary>Fama: multiplicador (1 -> 0,4) do intervalo entre clientes, pela quantidade de vendas ate aqui.</summary>
        public float Fame => Math.Max(Balance.FameFloor, (float)Math.Pow(Balance.FamePer10Sales, Sales / 10));
        public float ClientInterval(Item product) => Balance.ClientInterval[(int)product] * Fame * (Bought[(int)Upgrade.CounterCapacity] ? Balance.VitrineClientMul : 1f)
            * (product == Item.Jewel && Bought[(int)Upgrade.JewelVitrine] ? Balance.JewelVitrineClientMul : 1f);
        public V2 ClientSlot(int i) => new V2(Counter.Pos.X + 0.85f * i, Counter.Pos.Y + 1.0f);
        public V2 JewelSlot(int i) => new V2(JewelShop.Pos.X + 0.85f * (i - 2), JewelShop.Pos.Y + 1.0f);   // centrada na loja: i 0..4 = x 10,3..13,7 (coordenador)

        // ------------------------------------------------------------------ tick

        public void Tick(float dt, float inX, float inY)
        {
            Events.Clear();
            if (dt <= 0f) return;
            Time += dt;
            int earnedBefore = GoldEarned;

            MovePlayer(dt, inX, inY);
            foreach (Carrier w in Workers) WorkerThink(w, dt);
            Interact(Player, dt);
            foreach (Carrier w in Workers) Interact(w, dt);
            foreach (Station s in Stations) Produce(s, dt);
            Conveyor(dt);
            Clients(dt);
            Milestones();

            int cheapest = CheapestVisibleCost();
            // kill criterion do GDD §26 ("andar entre pilhas sem decisao"): anda de maos vazias sem nenhum pad pagavel = puro deslocamento
            if (Player.Moving && Player.Count == 0 && (cheapest < 0 || Gold < cheapest)) WalkNoDecision += dt;
            double inst = (GoldEarned - earnedBefore) / (double)dt;
            RateEma += (inst - RateEma) * Math.Min(1.0, dt / Balance.RateTau);
        }

        void MovePlayer(float dt, float inX, float inY)
        {
            float len = (float)Math.Sqrt(inX * inX + inY * inY);
            Player.Moving = len > 0.01f;
            if (!Player.Moving) return;
            if (len > 1f) { inX /= len; inY /= len; }
            Player.Pos = Clamp(new V2(Player.Pos.X + inX * Player.Speed * dt, Player.Pos.Y + inY * Player.Speed * dt), MaxX);
        }

        static V2 Clamp(V2 p, float maxX) => new V2(Math.Max(0.3f, Math.Min(maxX, p.X)), Math.Max(0.3f, Math.Min(Balance.WorldH - 0.3f, p.Y)));

        /// <summary>Move `c` em direcao a `target`; devolve true quando chegou (dentro de `reach`).</summary>
        public static bool MoveTowards(Carrier c, V2 target, float dt, float reach)
        {
            V2 d = target - c.Pos;
            float len = d.Len;
            c.Moving = len > reach;
            if (!c.Moving) return true;
            float step = Math.Min(len, c.Speed * dt);
            c.Pos = Clamp(c.Pos + d * (step / len), Balance.WorldW - 0.3f);   // ponytail: ajudante usa o mapa inteiro; so mira estacao desbloqueada, entao nunca entra na rua fechada
            return false;
        }

        // ------------------------------------------------------------------ ajudantes

        void WorkerThink(Carrier w, float dt)
        {
            if (w.State == 0)
            {
                if (w.Count >= w.Cap || (w.Count > 0 && w.IdleT > Balance.WorkerPatience)) { w.State = 1; w.Target = -1; w.IdleT = 0f; }
            }
            else if (w.Count == 0) { w.State = 0; w.Target = -1; w.IdleT = 0f; }

            Station t;
            if (w.Role == 3) t = w.State == 0 ? JewelerSource(w) : w.Item == Item.Jewel ? JewelShop : JewelBench;   // joalheiro: re-escolhe todo tick
            else
            {
                t = w.Target >= 0 ? Stations[w.Target] : null;
                bool valid = t != null && t.Unlocked && (w.State == 0 ? IsSourceFor(w, t) : IsDestFor(w, t));
                if (!valid) t = w.State == 0 ? PickSource(w) : PickDest(w);
            }
            w.Target = t != null ? t.Index : -1;
            if (t == null) { w.Moving = false; return; }
            MoveTowards(w, t.Pos, dt, Balance.StationRadius * 0.6f);
        }

        bool IsSourceFor(Carrier w, Station s)
        {
            if (w.Role == 0) return s.Kind == Kind.Deposit;
            if (w.Count > 0 && s.OutItem != w.Item) return false;
            return s.Produces && (w.Role == 1 ? s.Kind == Kind.Furnace : s.Kind == Kind.Crafter) && (s.Out > 0 || w.Target == s.Index);
        }

        bool IsDestFor(Carrier w, Station s)
        {
            if (w.Role == 2) return Sells(s, w.Item) && Stock[(int)w.Item] < CounterCap;
            return s.Produces && s.InItem == w.Item && s.In < s.InCap && (w.Role == 0 ? s.Kind == Kind.Furnace : s.Kind == Kind.Crafter);
        }

        Station PickSource(Carrier w)
        {
            if (w.Role == 0) return Deposit;
            Station best = null; float bestScore = float.MinValue;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked || !s.Produces || (w.Role == 1 ? s.Kind != Kind.Furnace : s.Kind != Kind.Crafter)) continue;
                if (w.Count > 0 && s.OutItem != w.Item) continue;
                if (s.Out == 0 && w.Count == 0 && !(s.Busy && s.Progress > 0.5f)) continue;   // vale esperar o que esta quase pronto
                float score = s.Out * 10f - V2.Dist(w.Pos, s.Pos);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        Station PickDest(Carrier w)
        {
            if (w.Role == 2) return CounterFor(w.Item);
            Station best = null; float bestScore = float.MinValue;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked || !s.Produces || s.InItem != w.Item || (w.Role == 0 ? s.Kind != Kind.Furnace : s.Kind != Kind.Crafter)) continue;
                int room = s.InCap - s.In;
                if (room <= 0) continue;
                float score = room * 4f + Need(s) - V2.Dist(w.Pos, s.Pos);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        /// <summary>
        /// Joalheiro (papel 3): vazio (ou ja com joia), joia pronta e espaco na loja -> bancada de joias; senao, enquanto a
        /// bancada de joias ainda cabe lingote, a fornalha com mais lingote (a atual ganha empate: nao fica indo e voltando).
        /// Bancada abastecida = null: nao tira lingote das outras linhas. Action so deixa ele mexer no proprio alvo.
        /// </summary>
        Station JewelerSource(Carrier w)
        {
            bool jewel = w.Count > 0 && w.Item == Item.Jewel;
            if ((w.Count == 0 || jewel) && JewelBench.Out > 0 && Stock[(int)Item.Jewel] + w.Count < CounterCap) return JewelBench;
            if (jewel || JewelBench.In + w.Count >= JewelBench.InCap) return null;
            Station best = null; float bestScore = float.MinValue;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked || s.Kind != Kind.Furnace || (s.Out == 0 && !s.Busy)) continue;
                float score = s.Out * 10f - V2.Dist(w.Pos, s.Pos) + (s.Index == w.Target ? 5f : 0f);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        /// <summary>Quanto o balcao que vende o produto desta bancada precisa dele (cliente da frente esperando pesa muito).</summary>
        float Need(Station crafter)
        {
            if (crafter.Kind != Kind.Crafter) return 0f;
            Item it = crafter.OutItem;
            List<Client> q = QueueFor(it);
            float n = (CounterCap - Stock[(int)it]) * 2f;
            if (q.Count > 0 && q[0].Want == it && Stock[(int)it] == 0) n += 12f;
            return n;
        }

        // ------------------------------------------------------------------ trocas automaticas

        public Station StationAt(V2 p)
        {
            Station best = null; float bd = Balance.StationRadius;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked) continue;
                float d = V2.Dist(p, s.Pos);
                if (d <= bd) { bd = d; best = s; }
            }
            return best;
        }

        public Pad PadAt(V2 p)
        {
            foreach (Pad pad in Pads)
                if (pad.Current(this) >= 0 && V2.Dist(p, pad.Pos) <= Balance.PadRadius) return pad;
            return null;
        }

        bool CanPick(Carrier c, Item i)
        {
            if (c.Count >= c.Cap || (c.Count > 0 && c.Item != i)) return false;
            if (c.Role == 0) return i == Item.Ore;
            if (c.Role == 1) return i == Item.Ingot;
            if (c.Role == 2) return IsProduct(i);
            if (c.Role == 3) return i == Item.Ingot || i == Item.Jewel;
            return true;
        }

        /// <summary>0 nada, 1 deposita na estacao, 2 pega da saida, 3 pega minerio, 4 abastece o balcao.</summary>
        int Action(Carrier c, Station s)
        {
            if (c.Role == 3 && s.Index != c.Target) return 0;   // joalheiro so mexe no proprio alvo: nao abastece a bancada do caminho
            switch (s.Kind)
            {
                case Kind.Deposit: return CanPick(c, Item.Ore) ? 3 : 0;
                case Kind.Counter: return c.Count > 0 && Sells(s, c.Item) && Stock[(int)c.Item] < CounterCap ? 4 : 0;
                default:
                    if (c.Has(s.InItem) && s.In < s.InCap) return 1;
                    return s.Out > 0 && CanPick(c, s.OutItem) ? 2 : 0;
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
            Station s = StationAt(c.Pos);
            int act = s != null ? Action(c, s) : 0;
            if (act == 0) { c.ActT = 0f; c.IdleT += dt; return; }
            c.IdleT = 0f;
            c.ActT += dt;
            float step = act == 3 ? Balance.GrabTime : Balance.TransferTime;
            while (c.ActT >= step)
            {
                c.ActT -= step;
                switch (act)
                {
                    case 1: c.Count--; s.In++; Emit(Ev.Deposited, (int)c.Item, c.Role, s.Pos); break;
                    case 2: s.Out--; c.Item = s.OutItem; c.Count++; Emit(Ev.Picked, (int)c.Item, c.Role, s.Pos); break;
                    case 3: c.Item = Item.Ore; c.Count++; Emit(Ev.Picked, (int)Item.Ore, c.Role, s.Pos); break;
                    case 4: c.Count--; Stock[(int)c.Item]++; Emit(Ev.Deposited, (int)c.Item, c.Role, s.Pos); break;
                }
                if (Action(c, s) != act) { c.ActT = 0f; break; }
            }
        }

        void Drain(Pad pad, float dt)
        {
            int u = pad.Current(this);
            if (u < 0) return;
            int cost = Upgrades.Cost(u);
            pad.Acc += cost / Balance.PadDrainSeconds * dt;
            int want = (int)pad.Acc;
            pad.Acc -= want;
            int step = Math.Min(want, Math.Min(Gold, cost - pad.Paid));
            if (step <= 0) return;
            Gold -= step;
            pad.Paid += step;
            if (pad.Paid >= cost) { pad.Paid = 0; pad.Acc = 0; pad.Armed = false; Buy((Upgrade)u); }
        }

        /// <summary>Compra direta (pad completo, teste ou cheat). Idempotente: ja comprado devolve false.</summary>
        public bool Buy(Upgrade u)
        {
            if (Bought[(int)u]) return false;
            int workersBefore = Workers.Count;
            Bought[(int)u] = true;
            UpgradeTime[(int)u] = Time;
            UpgradesBought++;
            Recompute();
            Emit(Ev.Bought, (int)u, Upgrades.Cost(u), PadOf(u));
            foreach (Station s in Stations) if (s.UnlockBy == (int)u) Emit(Ev.Unlocked, (int)s.OutItem, s.Index, s.Pos);
            if (Workers.Count > workersBefore) Emit(Ev.Hired, Workers.Count - 1, 0, HireSpot);
            return true;
        }

        V2 PadOf(Upgrade u)
        {
            foreach (Pad p in Pads) foreach (Upgrade c in p.Chain) if (c == u) return p.Pos;
            return Player.Pos;
        }

        /// <summary>Deriva TUDO dos flags Bought (idempotente: vale depois de Buy e depois de Load).</summary>
        public void Recompute()
        {
            float ft = Bought[(int)Upgrade.FurnaceSpeed2] ? Balance.FurnaceTime2 : Bought[(int)Upgrade.FurnaceSpeed1] ? Balance.FurnaceTime1 : Balance.FurnaceTime0;
            float ht = Bought[(int)Upgrade.HammerSpeed] ? Balance.HammerTime1 : Balance.HammerTime0;
            float jt = Balance.JewelTimeMul * (Bought[(int)Upgrade.JewelSpeed] ? Balance.JewelSpeedMul : 1f);
            foreach (Station s in Stations)
            {
                if (s.UnlockBy >= 0) s.Unlocked = Bought[s.UnlockBy];
                if (s.Kind == Kind.Furnace) s.Time = ft;
                if (s.Kind == Kind.Crafter) s.Time = ht * (s.OutItem == Item.Shield ? Balance.ShieldTimeMul : s.OutItem == Item.Jewel ? jt : 1f);
            }
            MaxX = (Bought[(int)Upgrade.SideCorridor] ? Balance.WorldW : Balance.WorkshopW) - 0.3f;
            Player.Speed = Bought[(int)Upgrade.PlayerSpeed] ? Balance.PlayerSpeedUp : Balance.PlayerSpeed;
            Player.Cap = Bought[(int)Upgrade.PlayerCapacity] ? Balance.PlayerCapUp : Balance.PlayerCap;
            CounterCap = Bought[(int)Upgrade.CounterCapacity] ? Balance.CounterCap1 : Balance.CounterCap0;
            QueueCap = Bought[(int)Upgrade.CounterCapacity] ? Balance.QueueCap1 : Balance.QueueCap0;
            JewelQueueCap = Bought[(int)Upgrade.JewelVitrine] ? Balance.JewelQueueCapUp : Balance.JewelQueueCap;
            HasConveyor = Bought[(int)Upgrade.Conveyor];
            // papel = indice do upgrade que contrata; a lista so cresce, na ordem da compra (o Joalheiro nao exige o Ajudante 3)
            for (int r = 0; r < HireBy.Length; r++)
                if (Bought[(int)HireBy[r]] && !Workers.Exists(w => w.Role == r)) Workers.Add(new Carrier { Pos = HireSpot, Role = r });
            bool fast = Bought[(int)Upgrade.HelperSpeed];
            foreach (Carrier w in Workers) { w.Speed = fast ? Balance.WorkerSpeedUp : Balance.WorkerSpeed; w.Cap = fast ? Balance.WorkerCapUp : Balance.WorkerCap; }
        }

        // ------------------------------------------------------------------ producao, esteira, clientes

        void Produce(Station s, float dt)
        {
            if (!s.Unlocked || !s.Produces) return;
            if (s.Busy)
            {
                if (s.Progress < 1f) s.Progress = Math.Min(1f, s.Progress + dt / s.Time);
                if (s.Progress >= 1f)
                {
                    if (s.Out < s.OutCap)
                    {
                        s.Out++; s.Busy = false; s.Progress = 0f; s.Blocked = false; s.Crafted++; Crafted[(int)s.OutItem]++;
                        Emit(Ev.Crafted, (int)s.OutItem, s.Index, s.Pos);
                    }
                    else Stall(s, dt);
                }
            }
            if (s.Busy) return;
            if (s.In < s.Need) { s.StarveSeconds += dt; return; }
            if (s.Out >= s.OutCap) { Stall(s, dt); return; }
            s.In -= s.Need; s.Busy = true; s.Progress = 0f;
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
            while (_convT >= Balance.ConveyorTime)
            {
                _convT -= Balance.ConveyorTime;
                if (FurnaceA.Out <= 0 || AnvilA.In >= AnvilA.InCap) { _convT = 0f; break; }
                FurnaceA.Out--; AnvilA.In++;
                Emit(Ev.Deposited, (int)Item.Ingot, 9, AnvilA.Pos);   // B = 9: veio pela esteira
            }
        }

        void Clients(float dt)
        {
            for (Item p = Item.Sword; p <= Item.Jewel; p++)
            {
                if (!LineUnlocked(p)) continue;
                _clientT[(int)p] -= dt;
                if (_clientT[(int)p] > 0f) continue;
                _clientT[(int)p] += ClientInterval(p);
                bool j = p == Item.Jewel;
                List<Client> q = QueueFor(p);
                int cap = j ? JewelQueueCap : QueueCap;
                float patience = j ? Balance.JewelPatience : Balance.Patience;
                if (q.Count < cap)
                {
                    q.Add(new Client { Want = p, Patience = patience, MaxPatience = patience });
                    Emit(Ev.ClientArrived, (int)p, q.Count, Slot(j, q.Count - 1));
                }
                else if (Stock[(int)p] > 0) Sell(p, Slot(j, cap));   // fila cheia mas o produto esta na vitrine: compra direto (sem isso a fila travava, ver BALANCE.md §9)
                else { ClientsTurnedAway++; Emit(Ev.ClientLeft, (int)p, 2, Slot(j, cap)); }
            }
            if (Queue.Count > MaxQueue) MaxQueue = Queue.Count;
            Serve(Queue, false, ref _serveT, dt);
            Serve(JewelQueue, true, ref _serveJ, dt);   // a loja de joias tem o proprio atendente (timer separado)
        }

        V2 Slot(bool jewel, int i) => jewel ? JewelSlot(i) : ClientSlot(i);

        public int PriceOf(Item item) => item == Item.Jewel && Bought[(int)Upgrade.JewelVitrine] ? Balance.JewelPriceUp : Balance.Price[(int)item];

        void Sell(Item want, V2 pos)
        {
            int price = PriceOf(want);
            Stock[(int)want]--; Gold += price; GoldEarned += price; Sales++; SoldItems[(int)want]++;
            if (FirstSaleTime < 0f) FirstSaleTime = Time;
            Emit(Ev.Sold, (int)want, price, pos);
        }

        /// <summary>
        /// Baus de marco (docs/AREA2_FASE2.md §3): o jogador em cima de um disponivel abre e ganha o ouro (uma vez: State 2
        /// vai no save); um marco batido so vira bau quando nao ha outro disponivel no mesmo lugar (um por vez na tela), e
        /// entao emite Milestone. O ouro do bau NAO entra em GoldEarned (nao infla a taxa online nem o cofre offline).
        /// </summary>
        void Milestones()
        {
            foreach (Chest c in Chests)
            {
                if (c.State != 1 || V2.Dist(Player.Pos, c.Pos) > Balance.PadRadius) continue;
                c.State = 2; Gold += c.Gold;
                Emit(Ev.ChestOpened, c.Index, c.Gold, c.Pos);
            }
            foreach (Chest c in Chests)
            {
                MilestoneDef m = Balance.Milestones[c.Index];
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
                    Item want = q[i].Want;
                    if (Stock[(int)want] <= 0) continue;
                    q.RemoveAt(i);
                    serveT = Balance.ServeTime;
                    Sell(want, Slot(jewel, i));
                    break;
                }
            }
            for (int i = q.Count - 1; i >= 0; i--)
            {
                q[i].Patience -= dt;
                if (q[i].Patience > 0f) continue;
                Emit(Ev.ClientLeft, (int)q[i].Want, 0, Slot(jewel, i));
                q.RemoveAt(i);
                ClientsLost++;
            }
        }

        void Emit(Ev kind, int a, int b, V2 pos) => Events.Add(new SimEvent(kind, a, b, pos));

        // ------------------------------------------------------------------ consultas para view, bot e dica

        public int CheapestVisibleCost()
        {
            int best = -1;
            foreach (Pad p in Pads)
            {
                int u = p.Current(this);
                if (u < 0) continue;
                int remaining = Upgrades.Cost(u) - p.Paid;
                if (best < 0 || remaining < best) best = remaining;
            }
            return best;
        }

        /// <summary>Pad visivel mais barato que o ouro atual ja paga inteiro, ou null.</summary>
        public Pad CheapestAffordablePad()
        {
            Pad best = null; int bestRemaining = int.MaxValue;
            foreach (Pad p in Pads)
            {
                int u = p.Current(this);
                if (u < 0) continue;
                int remaining = Upgrades.Cost(u) - p.Paid;
                if (remaining <= Gold && remaining < bestRemaining) { bestRemaining = remaining; best = p; }
            }
            return best;
        }

        public int HintArg;

        public Hint CurrentHint()
        {
            Pad pad = CheapestAffordablePad();
            if (pad != null) { HintArg = pad.Slot; return Hint.BuyPad; }
            foreach (Chest c in Chests) if (c.State == 1) { HintArg = c.Index; return Hint.OpenChest; }   // o 1o disponivel, na ordem dos marcos
            if (Player.Count > 0)
            {
                HintArg = (int)Player.Item;
                return IsProduct(Player.Item) ? Hint.ProductToCounter : Player.Item == Item.Ingot ? Hint.IngotToCrafter : Hint.OreToFurnace;
            }
            foreach (Station s in Stations) if (s.Unlocked && s.Kind == Kind.Crafter && s.Out > 0) { HintArg = (int)s.OutItem; return Hint.PickProducts; }
            foreach (Station s in Stations) if (s.Unlocked && s.Kind == Kind.Furnace && s.Out > 0) { HintArg = (int)Item.Ingot; return Hint.PickIngots; }
            if (Queue.Count > 0 && Stock[(int)Queue[0].Want] == 0) { HintArg = (int)Queue[0].Want; return Hint.ClientWaiting; }
            if (JewelQueue.Count > 0 && Stock[(int)Item.Jewel] == 0) { HintArg = (int)Item.Jewel; return Hint.ClientWaiting; }
            HintArg = (int)Item.Ore;
            return Hint.GrabOre;
        }

        // ------------------------------------------------------------------ offline

        /// <summary>
        /// Cofre offline (raia B §5 + decisao do coordenador 2026-10-06): 25% da taxa online medida (RateEma), teto de 2 h,
        /// e NUNCA mais que 2x o preco do upgrade mais barato ainda travado (tudo comprado: 2x o ultimo), para o cofre
        /// nao comprar o resto do jogo sozinho. Delta negativo da 0 e o claim e' idempotente por id (SavedAt do save):
        /// id igual ou mais antigo que o ultimo nunca paga de novo.
        /// </summary>
        public int ApplyOffline(long elapsedSeconds, long claimId)
        {
            if (elapsedSeconds <= 0 || claimId <= LastClaim) return 0;
            LastClaim = claimId;
            long capped = Math.Min(elapsedSeconds, (long)Balance.OfflineCapSeconds);
            int gold = (int)Math.Min(Math.Floor(RateEma * Balance.OfflineFactor * capped), OfflineMaxGold());
            if (gold <= 0) return 0;
            Gold += gold;
            OfflineEarned += gold;
            Emit(Ev.Offline, gold, (int)capped, Counter.Pos);
            return gold;
        }

        /// <summary>Preco do upgrade mais barato ainda nao comprado (o MENOR: a 2a area quebrou a ordem crescente por tier); tudo comprado = o ultimo.</summary>
        public int CheapestLockedCost()
        {
            int best = -1;
            for (int i = 0; i < Upgrades.Count; i++)
                if (!Bought[i] && (best < 0 || Upgrades.Cost(i) < best)) best = Upgrades.Cost(i);
            return best >= 0 ? best : Upgrades.Cost(Upgrades.Count - 1);
        }

        /// <summary>Teto relativo do cofre: 2x o proximo upgrade travado.</summary>
        public int OfflineMaxGold() => (int)Math.Round(Balance.OfflineMaxNextUpgrades * CheapestLockedCost());

        // ------------------------------------------------------------------ save/load (chave=valor; lixo vira estado inicial)

        public string Save(long nowUnix)
        {
            SavedAt = nowUnix;
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("v=1\n");
            sb.Append("t=").Append(Time.ToString("0.###", ci)).Append('\n');
            sb.Append("gold=").Append(Gold).Append('\n');
            var up = new StringBuilder();
            foreach (bool b in Bought) up.Append(b ? '1' : '0');
            sb.Append("up=").Append(up).Append('\n');
            sb.Append("ut=").Append(Join(UpgradeTime)).Append('\n');
            var pads = new StringBuilder();
            foreach (Pad p in Pads) pads.Append(p.Paid).Append(',');
            sb.Append("pads=").Append(pads).Append('\n');
            foreach (Station s in Stations)
                if (s.Produces) sb.Append("st").Append(s.Index).Append('=').Append(s.In).Append(',').Append(s.Out).Append(',').Append(s.Busy ? 1 : 0).Append(',').Append(s.Progress.ToString("0.###", ci)).Append(',').Append(s.Crafted).Append('\n');
            sb.Append("stock=").Append(Stock[2]).Append(',').Append(Stock[3]).Append(',').Append(Stock[4]).Append(',').Append(Stock[5]).Append('\n');
            sb.Append("sold=").Append(SoldItems[2]).Append(',').Append(SoldItems[3]).Append(',').Append(SoldItems[4]).Append(',').Append(SoldItems[5]).Append('\n');
            var ms = new StringBuilder();
            foreach (Chest c in Chests) { if (ms.Length > 0) ms.Append(','); ms.Append(c.State); }
            sb.Append("ms=").Append(ms).Append('\n');
            sb.Append("ema=").Append(RateEma.ToString("0.#####", ci)).Append('\n');
            sb.Append("saved=").Append(SavedAt).Append('\n');
            sb.Append("claim=").Append(LastClaim).Append('\n');
            sb.Append("m=").Append(FirstSaleTime.ToString("0.##", ci)).Append(',').Append(Sales).Append(',').Append(GoldEarned).Append(',').Append(OfflineEarned).Append(',')
              .Append(ClientsLost).Append(',').Append(ClientsTurnedAway).Append(',').Append(MaxQueue).Append(',').Append(WalkNoDecision.ToString("0.##", ci)).Append(',').Append(UpgradesBought).Append('\n');
            sb.Append("px=").Append(Player.Pos.ToString()).Append('\n');
            return sb.ToString();
        }

        static string Join(float[] v)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < v.Length; i++) { if (i > 0) sb.Append(','); sb.Append(v[i].ToString("0.##", CultureInfo.InvariantCulture)); }
            return sb.ToString();
        }

        public static Sim Load(string text)
        {
            var sim = new Sim();
            if (string.IsNullOrEmpty(text)) return sim;
            var ci = CultureInfo.InvariantCulture;
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw.Substring(0, eq).Trim(), val = raw.Substring(eq + 1).Trim();
                string[] parts = val.Split(',');
                switch (key)
                {
                    case "t": sim.Time = F(val, 0f); break;
                    case "gold": sim.Gold = Math.Max(0, I(val, 0)); break;
                    case "up": for (int i = 0; i < Math.Min(val.Length, Upgrades.Count); i++) sim.Bought[i] = val[i] == '1'; break;
                    case "ut": for (int i = 0; i < Math.Min(parts.Length, Upgrades.Count); i++) sim.UpgradeTime[i] = F(parts[i], -1f); break;
                    case "pads": for (int i = 0; i < Math.Min(parts.Length, sim.Pads.Count); i++) sim.Pads[i].Paid = Math.Max(0, I(parts[i], 0)); break;
                    case "stock": for (int i = 0; i < Math.Min(parts.Length, sim.Stock.Length - 2); i++) sim.Stock[2 + i] = Math.Max(0, I(parts[i], 0)); break;   // save antigo: 3 numeros, joia fica 0
                    case "sold": for (int i = 0; i < Math.Min(parts.Length, sim.SoldItems.Length - 2); i++) sim.SoldItems[2 + i] = Math.Max(0, I(parts[i], 0)); break;   // save antigo sem sold=: conta do zero
                    case "ms": for (int i = 0; i < Math.Min(parts.Length, sim.Chests.Count); i++) sim.Chests[i].State = Clamp0(I(parts[i], 0), 2); break;      // save antigo sem ms=: nenhum marco
                    case "ema": sim.RateEma = Math.Max(0.0, D(val, 0.0)); break;
                    case "saved": sim.SavedAt = L(val, 0); break;
                    case "claim": sim.LastClaim = L(val, 0); break;
                    case "m":
                        if (parts.Length >= 9)
                        {
                            sim.FirstSaleTime = F(parts[0], -1f); sim.Sales = I(parts[1], 0); sim.GoldEarned = I(parts[2], 0); sim.OfflineEarned = I(parts[3], 0);
                            sim.ClientsLost = I(parts[4], 0); sim.ClientsTurnedAway = I(parts[5], 0); sim.MaxQueue = I(parts[6], 0); sim.WalkNoDecision = F(parts[7], 0f); sim.UpgradesBought = I(parts[8], 0);
                        }
                        break;
                    case "px": if (parts.Length == 2) sim.Player.Pos = new V2(F(parts[0], 4.5f), F(parts[1], 3.5f)); break;   // clamp so' depois do Recompute (MaxX depende do Corredor)
                    default:
                        if (key.StartsWith("st") && parts.Length >= 4)
                        {
                            int idx = I(key.Substring(2), -1);
                            if (idx < 0 || idx >= sim.Stations.Count || !sim.Stations[idx].Produces) break;
                            Station s = sim.Stations[idx];
                            s.In = Clamp0(I(parts[0], 0), s.InCap); s.Out = Clamp0(I(parts[1], 0), s.OutCap);
                            s.Busy = I(parts[2], 0) == 1; s.Progress = Math.Max(0f, Math.Min(1f, F(parts[3], 0f)));
                            if (parts.Length >= 5) s.Crafted = Math.Max(0, I(parts[4], 0));
                        }
                        break;
                }
            }
            sim.UpgradesBought = 0;
            foreach (bool b in sim.Bought) if (b) sim.UpgradesBought++;
            sim.Recompute();
            for (int i = 2; i < sim.Stock.Length; i++) sim.Stock[i] = Math.Min(sim.Stock[i], sim.CounterCap);   // teto so' e' conhecido depois dos upgrades
            sim.Player.Pos = Clamp(sim.Player.Pos, sim.MaxX);
            foreach (Pad p in sim.Pads) if (p.Current(sim) < 0) p.Paid = 0;
            return sim;
        }

        static int Clamp0(int v, int max) => Math.Max(0, Math.Min(max, v));
        static int I(string s, int d) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        static long L(string s, long d) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : d;
        static float F(string s, float d) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : d;
        static double D(string s, double d) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && !double.IsNaN(v) && !double.IsInfinity(v) ? v : d;
    }
}
