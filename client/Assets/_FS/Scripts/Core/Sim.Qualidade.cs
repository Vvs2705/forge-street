using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Sim em partes (o estado e o construtor ficam no Sim.cs): qualidade (B-CORE-03, docs/ECONOMIA_WAVE_B.md §1).</summary>
    public sealed partial class Sim
    {
        // ------------------------------------------------------------------ qualidade (B-CORE-03, docs/ECONOMIA_WAVE_B.md §1)

        /// <summary>Niveis que circulam numa pilha (Normal, Fina, Superior): tamanho dos lotes OutQ/HeldQ/StockQ. A Masterwork nunca entra num.</summary>
        public const int Graus = (int)Quality.Masterwork;
        /// <summary>Um lote (Fina/Superior por nivel) por item.</summary>
        public static int[][] Lotes(int items)
        {
            var q = new int[items][];
            for (int i = 0; i < items; i++) q[i] = new int[Graus];
            return q;
        }

        /// <summary>Fina e Superior de um lote nunca passam do `total` (Normal = o resto): corta de baixo para cima, a melhor fica. Vale depois
        /// de alguem mexer so no total (load com teto, -stock, teste).</summary>
        public static void Fit(int total, int[] q)
        {
            for (int l = Graus - 1; l > 0; l--) { q[l] = Clamp0(q[l], total); total -= q[l]; }
        }

        /// <summary>Tira 1 de `total` (pilha, mao ou balcao) e devolve o nivel dela: a melhor primeiro (Superior, Fina, Normal).</summary>
        public static Quality Take(ref int total, int[] q)
        {
            Fit(total, q);
            total--;
            for (int l = Graus - 1; l > 0; l--) if (q[l] > 0) { q[l]--; return (Quality)l; }
            return Quality.Normal;
        }

        /// <summary>Poe 1 peca de nivel `l` no lote.</summary>
        public static void Put(ref int total, int[] q, Quality l)
        {
            total++;
            if (l > Quality.Normal) q[(int)l]++;
        }

        /// <summary>Revelacao progressiva (GDD §19): qualidade, niveis de estacao e golpe de mestre so depois das Ferramentas (~11 min). Os 10
        /// primeiros minutos ficam intocados (ECONOMIA_WAVE_B §0.6).</summary>
        public bool QualityRevealed => _ef[(int)Upgrade.Tools] < 0 || Has(Upgrade.Tools);   // C-CORE-01: distrito sem Ferramentas = ja revelada (o ferreiro vem do Old Forge)

        /// <summary>Pontos de qualidade (centesimos) da estacao, por fonte (QualitySource). A soma e' o q do acumulador (QualityPoints): o que
        /// esta aqui explica 100% das pecas. Fornalha, deposito e balcao: tudo 0 (lingote e' intermediario).</summary>
        public int[] QualityBreakdown(Station s)
        {
            var b = new int[(int)QualitySource.Energizada + 1];   // uma por fonte (a ultima do enum)
            if (!s.Produces || !IsProduct(s.OutItem)) return b;
            b[(int)QualitySource.StationLevel] = (s.Level - 1) * T.NivelEstacaoQ;
            b[(int)QualitySource.Worker] = Equipe.QualidadeOficial(s);   // B-CORE-04: oficial da familia
            b[(int)QualitySource.Buff] = Equipe.QualidadeMestre(s)       // B-CORE-04: mestre de obras
                + ((int)s.OutItem < T.QualidadeEvento.Length ? T.QualidadeEvento[(int)s.OutItem] : 0)   // B-CORE-08: modificador do evento (Hunt)
                + QualidadeSelo(s);   // C-CORE-07: o selo do distrito (selo_arsenal, q +25)
            b[(int)QualitySource.Blueprint] = Projetos.Tecnica(s.Recipe.Family)   // B-CORE-07: tecnica da familia (posse do projeto)
                + Projetos.TecnicaItem(ItemDefs[(int)s.OutItem].Id);   // C-CORE-06: e a de produto (lapidacao: colar e caixa)
            b[(int)QualitySource.Material] = TemLiga(s) ? T.LigaQ : 0;   // C-CORE-05: liga no slot = a proxima peca leva uma
            b[(int)QualitySource.Energizada] = QualidadeEnergizada(s);   // C-CORE-08: a peca do altar no Forcado
            return b;
        }

        /// <summary>C-CORE-05: a estacao tem slot de liga com pelo menos 1 dentro.</summary>
        public static bool TemLiga(Station s) => s.Liga >= 0 && s.Ins[s.Liga] > 0;

        /// <summary>q da estacao: soma do QualityBreakdown.</summary>
        public int QualityPoints(Station s)
        {
            int q = 0;
            foreach (int p in QualityBreakdown(s)) q += p;
            return q;
        }

        /// <summary>Preco de `item` no nivel `l`: preco base x Tuning.QualidadePct, inteiro e meio para cima (espada 10/15/25, escudo 25/38/63).
        /// Masterwork = 0 (nao vende). B-CORE-08: familia completa no Hall, +5% (Hall.BonusPct) na mesma conta (sem bonus, o de antes ao byte).</summary>
        public int PriceOf(Item item, Quality l) => (int)(((long)PriceOf(item) * T.QualidadePct[(int)l] * (100 + Hall.BonusPct(item)) + 5000) / 10000);

        /// <summary>
        /// Nivel da peca que sai agora da estacao (§1.3): parte inteira de q = nivel garantido; a fracao enche o acumulador e, ao passar de 100,
        /// esta peca sobe um nivel. A automacao para em Superior. Golpe valendo: +1 nivel; numa Superior com Masterwork pendente, Masterwork.
        /// Sem sorteio: mesma sequencia de compras e golpes, mesma sequencia de niveis.
        /// </summary>
        Quality Grade(Station s)
        {
            if (!IsProduct(s.OutItem)) return Quality.Normal;
            int q = QualityPoints(s), l = q / 100;
            // C-CORE-05: a liga que somou no q sai do slot com a peca. ponytail: consome na SAIDA da peca, nao no inicio (CONTEUDO_V1 §2.2):
            // assim a peca em andamento nao precisa de estado no save; o que o jogador ve na barra (QualityBreakdown) e' o que a peca leva
            if (TemLiga(s)) { s.Ins[s.Liga]--; PecasComLiga[(int)s.OutItem]++; }
            s.QAcc += q % 100;
            if (s.QAcc >= 100) { l++; s.QAcc -= 100; }
            l = Math.Min(l, (int)Quality.Superior);
            if (!s.Strike) return (Quality)l;
            s.Strike = false;
            GolpeSuperior(s, l + 1);   // dly_golpe_superior1: a peca do golpe saiu Superior (ou obra-prima)
            if (l == (int)Quality.Superior && MasterworkPending != null && MasterworkPending(s)) return Quality.Masterwork;
            int up = Math.Min(l + 1, (int)Quality.Superior);
            StrikeGold += PriceOf(s.OutItem, (Quality)up) - PriceOf(s.OutItem, (Quality)l);
            return (Quality)up;
        }

        /// <summary>Golpe que faz Superior (LIVEOPS_V1 §4.2 #27, §4.5 #19): a peca golpeada sai Superior ou melhor (`nivel` = o de antes + 1).
        /// So conta nas metas (a do Imperio tambem, como o master_strike): o Sim segue identico.</summary>
        void GolpeSuperior(Station s, int nivel)
        {
            if (nivel < (int)Quality.Superior) return;
            Metas.Registra("master_strike_superior", (int)s.OutItem);
            Heroi?.Metas.Registra("master_strike_superior", NaRaiz((int)s.OutItem));
        }

        /// <summary>
        /// Bancada onde o ferreiro pode dar o golpe de mestre agora, ou null: qualidade revelada, recarga zerada e o ferreiro PARADO na boca de
        /// entrada de uma bancada aberta que tem a proxima peca (trabalhando ou com a receita inteira na entrada) e ainda sem golpe valendo. So o
        /// ferreiro: ajudante, offline e cofre nao passam por aqui.
        /// </summary>
        public Station StrikeStation
        {
            get
            {
                if (!QualityRevealed || StrikeCooldown > 0f || Player.Moving) return null;
                Station s = StationAt(Player.Pos, out bool outZone);
                if (s == null || outZone || s.Kind != Kind.Crafter || !s.Produces || !IsProduct(s.OutItem) || s.Strike) return null;
                if (s.Busy) return s;
                for (int k = 0; k < s.Ins.Length; k++) if (s.Ins[k] < s.Recipe.Inputs[k].Qty) return null;
                return s;
            }
        }

        /// <summary>O anel do golpe (View): `hit` = o toque caiu na janela de Tuning.GolpeJanela. Acertou com StrikeStation valendo: a proxima peca
        /// daquela bancada sobe 1 nivel e a recarga comeca. Errou ou fora de hora: nada muda (errar nao pune, sem recarga) e devolve false.</summary>
        public bool Strike(bool hit)
        {
            Station s = StrikeStation;
            if (s == null || !hit) return false;
            s.Strike = true;
            StrikeCooldown = T.GolpeRecarga;
            Strikes++;
            LiveOps.OnGolpe((int)s.OutItem);   // D-CORE-03: master_strike das missoes de evento
            Heroi?.LiveOps.OnGolpe(NaRaiz((int)s.OutItem));   // C-CORE-03: e no evento do Imperio
            Metas.Registra("master_strike", (int)s.OutItem);   // raia H: e nas metas (as do Imperio tambem, como a venda)
            Heroi?.Metas.Registra("master_strike", NaRaiz((int)s.OutItem));
            return true;
        }
    }
}
