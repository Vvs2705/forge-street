using System;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Passos do tutorial guiado do 1o ciclo (a ordem e' a do save: so anexar no fim, antes do Fim).</summary>
    public enum PassoTutorial { BoasVindas, Andar, PegarMinerio, Fornalha, PegarLingotes, Bigorna, PegarEspada, Balcao, Melhoria, Menu, Fim }

    /// <summary>Cartoes "o que e' isto", 1x cada, depois do tutorial (o bit no save e' a posicao: so anexar no fim).</summary>
    public enum Explica { Ajudante, Fila, Vip, Qualidade, Encomendas, Metas, Distrito }

    /// <summary>
    /// FTUE (pedido do dono, 2026-11; GDD §Onboarding): tutorial guiado do 1o ciclo, puro e testavel. Cada passo avanca pelo estado REAL do
    /// Sim (nunca por tempo): andou 1,5 m, minerio na mao, minerio na fornalha, lingote na mao, lingote na bigorna, espada na mao, 1a venda,
    /// 1a melhoria; so a boas-vindas (Comecar) e o passo do menu (Confirmar, ou a View abrindo o menu) pedem toque. Pula para o passo mais
    /// adiantado que o estado ja cumpre (quem faz duas coisas no mesmo quadro nao fica preso) e nunca volta. Pular conclui na hora.
    /// Depois do Fim, um cartao curto por sistema na 1a vez que ele aparece (Explica), um de cada vez: a View mostra Cartao e chama
    /// FecharCartao no toque.
    /// Do Imperio: o da raiz (Sim.Tutorial); o distrito ativo vai em Atualiza(s). Save na linha tut= (passo,vistos,pulou), so na raiz.
    /// Save sem tut= de quem ja jogou (1a venda ou alguma compra) = concluido, sem tutorial nem cartao do que ja abriu (CarregaLegado).
    /// O nucleo nao muda de ritmo por ele: o bot e o econsim nunca chamam Atualiza.
    /// </summary>
    public sealed class Tutorial
    {
        /// <summary>Passos numerados na tela ("Passo N de 9"): do Andar ao Menu; a boas-vindas e' o cartao do centro.</summary>
        public const int Total = (int)PassoTutorial.Fim - 1;
        /// <summary>Metros andados que completam o passo Andar (pedido: ~1,5 m).</summary>
        public const float AndarMetros = 1.5f;
        /// <summary>Clientes na fila que mostram o cartao da fila/paciencia.</summary>
        public const int FilaCartao = 3;

        static readonly string[] Ids = { "boas_vindas", "andar", "minerio", "fornalha", "lingotes", "bigorna", "espada", "venda", "melhoria", "menu", "fim" };
        static readonly string[] IdsExplica = { "ajudante", "fila", "vip", "qualidade", "encomendas", "metas", "distrito" };
        static readonly int Todos = (1 << IdsExplica.Length) - 1;

        public PassoTutorial Passo { get; private set; }
        /// <summary>Concluido pelo "Pular tutorial" (diario).</summary>
        public bool Pulou { get; private set; }
        /// <summary>Bits dos Explica ja vistos.</summary>
        public int Vistos { get; private set; }
        /// <summary>Cartao "o que e' isto" na tela (null = nenhum).</summary>
        public Explica? Cartao { get; private set; }
        public bool Ativo => Passo < PassoTutorial.Fim;
        /// <summary>1..Total nos passos numerados; 0 na boas-vindas e no fim.</summary>
        public int Numero => Passo > PassoTutorial.BoasVindas && Passo < PassoTutorial.Fim ? (int)Passo : 0;
        public string Id => IdDe(Passo);
        public static string IdDe(PassoTutorial p) => Ids[(int)p];
        public static string IdDe(Explica e) => IdsExplica[(int)e];
        public bool Visto(Explica e) => (Vistos & (1 << (int)e)) != 0;

        V2 _de; bool _temDe;

        /// <summary>"Comecar" do cartao de boas-vindas.</summary>
        public bool Comecar()
        {
            if (Passo != PassoTutorial.BoasVindas) return false;
            Passo = PassoTutorial.Andar;
            return true;
        }

        /// <summary>"Entendi" do passo do menu (ou o menu aberto): fim do tutorial.</summary>
        public bool Confirmar()
        {
            if (Passo != PassoTutorial.Menu) return false;
            Passo = PassoTutorial.Fim;
            return true;
        }

        /// <summary>"Pular tutorial": conclui agora (os cartoes "o que e' isto" seguem valendo).</summary>
        public bool Pular()
        {
            if (!Ativo) return false;
            Passo = PassoTutorial.Fim; Pulou = true;
            return true;
        }

        /// <summary>Sem tutorial nem cartoes (fotos e smoke no -testsession).</summary>
        public void Desligar() { Passo = PassoTutorial.Fim; Vistos = Todos; Cartao = null; }

        /// <summary>
        /// Avanca pelo estado do distrito ativo `s` (true = o passo mudou neste chamado). Concluido: escolhe o proximo cartao (o 1o nao visto
        /// cujo sistema apareceu), so com nenhum na tela.
        /// </summary>
        public bool Atualiza(Sim s)
        {
            if (Ativo)
            {
                if (Passo == PassoTutorial.BoasVindas) return false;
                if (Passo == PassoTutorial.Andar && !_temDe) { _de = s.Player.Pos; _temDe = true; }
                for (int k = (int)PassoTutorial.Melhoria; k >= (int)PassoTutorial.Andar; k--)
                {
                    if (!Feito(s, (PassoTutorial)k)) continue;
                    if (k < (int)Passo) return false;
                    Passo = (PassoTutorial)(k + 1);
                    return true;
                }
                return false;
            }
            if (Cartao != null) return false;
            for (int e = 0; e < IdsExplica.Length; e++)
                if (!Visto((Explica)e) && Aberto(s, (Explica)e)) { Cartao = (Explica)e; break; }
            return false;
        }

        /// <summary>Toque no cartao: visto para sempre.</summary>
        public void FecharCartao()
        {
            if (Cartao is Explica e) Vistos |= 1 << (int)e;
            Cartao = null;
        }

        /// <summary>O estado de `s` ja cumpre o passo `p`?</summary>
        bool Feito(Sim s, PassoTutorial p)
        {
            switch (p)
            {
                case PassoTutorial.Andar: return Passo == PassoTutorial.Andar && _temDe && V2.Dist(s.Player.Pos, _de) >= AndarMetros;
                case PassoTutorial.PegarMinerio: return s.Deposit != null && s.Player.Has(s.Deposit.OutItem);
                case PassoTutorial.Fornalha: return Algum(s, Kind.Furnace);
                case PassoTutorial.PegarLingotes: { Station f = Fornalha(s); return f != null && s.Player.Has(f.OutItem); }
                case PassoTutorial.Bigorna: return Algum(s, Kind.Crafter);
                case PassoTutorial.PegarEspada: return Produto(s) >= 0;
                case PassoTutorial.Balcao: return s.Sales > 0 || s.FirstSaleTime >= 0f;
                case PassoTutorial.Melhoria: return s.UpgradesBought > 0;
                default: return false;
            }
        }

        static Station Fornalha(Sim s) => s.Stations.Find(x => x.Unlocked && x.Kind == Kind.Furnace);

        /// <summary>Alguma estacao aberta do tipo com insumo dentro, trabalhando ou com saida (o insumo chegou).</summary>
        static bool Algum(Sim s, Kind k)
        {
            foreach (Station x in s.Stations)
                if (x.Unlocked && x.Kind == k && x.Produces && (x.Busy || x.Out > 0 || Array.Exists(x.Ins, n => n > 0))) return true;
            return false;
        }

        /// <summary>Produto na mao do ferreiro (handle), ou -1.</summary>
        static int Produto(Sim s)
        {
            for (int i = 0; i < s.Player.Held.Length; i++) if (s.Player.Held[i] > 0 && s.IsProduct((Item)i)) return i;
            return -1;
        }

        /// <summary>O sistema do cartao `e` apareceu no distrito ativo `s`?</summary>
        public static bool Aberto(Sim s, Explica e)
        {
            switch (e)
            {
                case Explica.Ajudante: return s.Workers.Count > 0;
                case Explica.Fila: return s.Queue.Count >= FilaCartao;
                case Explica.Vip: return s.Queue.Exists(c => c.Vip);
                case Explica.Qualidade: return s.Heroi == null && s.QualityRevealed;
                case Explica.Encomendas: return s.Encomendas.Vagas.Count > 0;
                case Explica.Metas: return (s.Heroi ?? s).Metas.Aberto;
                case Explica.Distrito: return s.Heroi != null;
                default: return false;
            }
        }

        /// <summary>
        /// Alvo da seta no mundo durante o passo (o mesmo Hint da dica, para o WorldView.ShowHint) e o argumento dele; null = sem seta.
        /// Esperando a fornalha ou a bigorna terminar, a seta fica nela.
        /// </summary>
        public Hint? Alvo(Sim s, out int arg)
        {
            arg = 0;
            Station f = Fornalha(s);
            switch (Passo)
            {
                case PassoTutorial.Andar:
                case PassoTutorial.PegarMinerio: arg = s.Deposit != null ? (int)s.Deposit.OutItem : 0; return Hint.GrabOre;
                case PassoTutorial.Fornalha: arg = s.Deposit != null ? (int)s.Deposit.OutItem : 0; return Hint.OreToFurnace;
                case PassoTutorial.PegarLingotes:
                    arg = f != null ? (int)f.OutItem : 0;
                    return s.Stations.Exists(x => x.Unlocked && x.Kind == Kind.Furnace && x.Out > 0) ? Hint.PickIngots : Hint.OreToFurnace;
                case PassoTutorial.Bigorna: arg = f != null ? (int)f.OutItem : 0; return Hint.IngotToCrafter;
                case PassoTutorial.PegarEspada:
                {
                    Station b = s.Stations.Find(x => x.Unlocked && x.Kind == Kind.Crafter && x.Out > 0 && s.IsProduct(x.OutItem));
                    if (b == null) { arg = f != null ? (int)f.OutItem : 0; return Hint.IngotToCrafter; }
                    arg = (int)b.OutItem; return Hint.PickProducts;
                }
                case PassoTutorial.Balcao:
                {
                    int p = Produto(s);
                    if (p < 0) { Station b = s.Stations.Find(x => x.Unlocked && x.Kind == Kind.Crafter && s.IsProduct(x.OutItem)); p = b != null ? (int)b.OutItem : (int)Item.Sword; }
                    arg = p; return Hint.ProductToCounter;
                }
                case PassoTutorial.Melhoria:
                {
                    Pad best = null; long custo = long.MaxValue;
                    foreach (Pad p in s.Pads)
                    {
                        int u = p.Current(s);
                        if (u >= 0 && s.T.Cost(u) - p.Paid < custo) { custo = s.T.Cost(u) - p.Paid; best = p; }
                    }
                    if (best == null) return Hint.BuyMenu;
                    arg = best.Slot; return Hint.BuyPad;
                }
                case PassoTutorial.Menu: return Hint.BuyMenu;
                default: return null;
            }
        }

        // ------------------------------------------------------------------ save (linha tut=; o Load de antes ignora chave desconhecida)

        /// <summary>tut=passo,vistos,pulou. `progrediu` = 1a venda ou compra: grava mesmo no padrao (o Load sem a linha leria "ja jogava").</summary>
        public void Save(StringBuilder sb, bool progrediu)
        {
            if (Passo == PassoTutorial.BoasVindas && Vistos == 0 && !Pulou && !progrediu) return;
            sb.Append("tut=").Append(((int)Passo).ToString(CultureInfo.InvariantCulture)).Append(',').Append(Vistos.ToString(CultureInfo.InvariantCulture))
              .Append(',').Append(Pulou ? 1 : 0).Append('\n');
        }

        /// <summary>Le a linha tut= (lixo = padrao, passo preso entre BoasVindas e Fim). Sem a linha e `s` ja jogou: CarregaLegado.</summary>
        public void Load(string text, Sim s)
        {
            foreach (string raw in text.Split('\n'))
            {
                if (!raw.StartsWith("tut=", StringComparison.Ordinal)) continue;
                string[] p = raw.Substring(4).Trim().Split(',');
                Passo = (PassoTutorial)Math.Max(0, Math.Min((int)PassoTutorial.Fim, I(p[0])));
                Vistos = p.Length > 1 ? I(p[1]) & Todos : 0;
                Pulou = p.Length > 2 && I(p[2]) == 1;
                return;
            }
            if (s.FirstSaleTime >= 0f || s.Sales > 0 || s.UpgradesBought > 0) CarregaLegado(s);
        }

        /// <summary>Save de antes do tutorial de quem ja jogava: concluido, e o cartao de todo sistema que ja apareceu fica visto (fila e VIP
        /// tambem, se ja houve VIP: quem ja jogou ja os viu).</summary>
        void CarregaLegado(Sim s)
        {
            Passo = PassoTutorial.Fim;
            for (int e = 0; e < IdsExplica.Length; e++) if (Aberto(s, (Explica)e)) Vistos |= 1 << e;
            if (s.VipCount > 0) Vistos |= (1 << (int)Explica.Fila) | (1 << (int)Explica.Vip);
        }

        static int I(string v) => int.TryParse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
    }
}
