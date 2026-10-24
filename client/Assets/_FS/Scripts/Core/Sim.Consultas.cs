using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Sim em partes (o estado e o construtor ficam no Sim.cs): consultas para view, bot e dica; offline.</summary>
    public sealed partial class Sim
    {
        // ------------------------------------------------------------------ consultas para view, bot e dica

        public long CheapestVisibleCost()
        {
            long best = -1;
            foreach (Pad p in Pads)
            {
                int u = p.Current(this);
                if (u < 0) continue;
                long remaining = T.Cost(u) - p.Paid;
                if (best < 0 || remaining < best) best = remaining;
            }
            return best;
        }

        /// <summary>Pad visivel mais barato que o ouro atual ja paga inteiro, ou null.</summary>
        public Pad CheapestAffordablePad()
        {
            Pad best = null; long bestRemaining = long.MaxValue;
            foreach (Pad p in Pads)
            {
                int u = p.Current(this);
                if (u < 0) continue;
                long remaining = T.Cost(u) - p.Paid;
                if (remaining <= Gold && remaining < bestRemaining) { bestRemaining = remaining; best = p; }
            }
            return best;
        }

        /// <summary>
        /// Upgrade do menu que e' a compra mais barata que o ouro atual ja paga, ou -1 (nenhum pagavel, ou um pad visivel
        /// pagavel custa menos). Empate com pad: o menu, que nao precisa andar. Mesma regra da dica e do bot.
        /// </summary>
        public int CheapestAffordableMenu()
        {
            int best = -1;
            for (int u = 0; u < UpgradeDefs.Length; u++)
                if (MenuAvailable(u) && T.Cost(u) <= Gold && (best < 0 || T.Cost(u) < T.Cost(best))) best = u;
            Pad pad = best >= 0 ? CheapestAffordablePad() : null;
            return pad != null && T.Cost(pad.Current(this)) - pad.Paid < T.Cost(best) ? -1 : best;
        }

        public int HintArg;

        public Hint CurrentHint()
        {
            int menu = CheapestAffordableMenu();
            if (menu >= 0) { HintArg = menu; return Hint.BuyMenu; }
            Pad pad = CheapestAffordablePad();
            if (pad != null) { HintArg = pad.Slot; return Hint.BuyPad; }
            foreach (Chest c in Chests) if (c.State == 1) { HintArg = c.Index; return Hint.OpenChest; }   // o 1o disponivel, na ordem dos marcos
            // FASE7: pilha mista. Produto na mao primeiro; depois produto pronto que ainda cabe na mao (vale com lingote na mao: e' o
            // que destrava a bancada com a saida cheia); so entao o insumo na mao
            // FASE7 §3: produto que o balcao nao aceita (estoque cheio) nao segura a dica no balcao; produto que a fila pede vem antes
            Item deliver = Deliverable(Player);
            if (deliver != Item.Ore) { HintArg = (int)deliver; return Hint.ProductToCounter; }
            for (int pass = 0; pass < 2; pass++)
                foreach (Station s in Stations)
                    if (s.Unlocked && s.Kind == Kind.Crafter && s.Out > 0 && CanPick(Player, s.OutItem) && (pass == 1 || Wanted(s.OutItem))) { HintArg = (int)s.OutItem; return Hint.PickProducts; }
            Item raw = Player.Has(Item.Ingot) ? Item.Ingot : Item.Ore;   // insumo mais adiantado na mao
            if (Player.Has(raw)) { HintArg = (int)raw; return raw == Item.Ingot ? Hint.IngotToCrafter : Hint.OreToFurnace; }
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
        public long ApplyOffline(long elapsedSeconds, long claimId)
        {
            if (elapsedSeconds <= 0 || claimId <= LastClaim) return 0;
            LastClaim = claimId;
            long capped = Math.Min(elapsedSeconds, T.OfflineCap);   // A-CORE-03: o Tuning so encurta; 2 h e' trava (Balance.OfflineCapSeconds)
            long gold = (long)Math.Min(Math.Floor(RateEma * T.OfflineFactor * capped), OfflineMaxGold());
            LastClaimGold = Math.Max(0, gold);   // E-PLAT-02: o "Cofre 2x" dobra este resgate (1x por claimId)
            if (gold <= 0) return 0;
            Gold += gold;
            OfflineEarned += gold;
            Emit(Ev.Offline, (int)Math.Min(int.MaxValue, gold), (int)capped, Counter.Pos);
            return gold;
        }

        /// <summary>Menor produtivo nao comprado e a venda (pre-requisito comprado); producao completa = o produtivo mais caro (hoje a Vitrine de joias, 9.000:
        /// e' o "ultimo" de antes, mas nao muda quando um produtivo mais barato e' anexado ao enum). Luxo e Meta nao alteram o cofre.</summary>
        public long CheapestLockedCost()
        {
            long best = -1, top = 0;
            for (int i = 0; i < UpgradeDefs.Length; i++)
            {
                if (!IsProduction(i)) continue;   // luxo e Meta (B-CORE-03) nao mexem no cofre
                long c = T.Cost(i); int req = UpgradeDefs[i].Requires;
                top = Math.Max(top, c);
                // FASE7: so o que esta A VENDA (pre-requisito comprado). Sem isso o Balcao 5 (150, exige a Vitrine) baixava o cofre de
                // 1.060 para 300 aos 10 min; e o Joalheiro 2 (2.400, exige o Mineiro) segurava o teto do par em 4.800 (agora 2x Mineiro)
                if (!Bought[i] && (req < 0 || Bought[req]) && (best < 0 || c < best)) best = c;
            }
            return best >= 0 ? best : top;
        }

        /// <summary>Teto relativo do cofre: 2x o proximo upgrade travado.</summary>
        public long OfflineMaxGold() => (long)Math.Round(T.OfflineMaxNextUpgrades * CheapestLockedCost());
    }
}
