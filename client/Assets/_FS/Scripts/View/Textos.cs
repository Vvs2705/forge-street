using System;
using System.Globalization;
using FS.Core;

namespace FS
{
    /// <summary>Estado do cartao de melhoria (A-UI-18); a ordem do enum e' a ordem da bandeja (compravel primeiro, travado no fim).</summary>
    public enum Cartao { Disponivel, SemOuro, Travado, Comprado }

    /// <summary>
    /// Textos curtos da HUD, sem UnityEngine (v0.6d, revisao de UX): o teste EditMode (TextosTests) le daqui e falha se alguma dica
    /// passar de DicaMax. Dica = 1 linha de ate 26 caracteres, sem preco: a frase natural quando cabe, senao a curta.
    /// A-UI-18: cartao de melhoria (estado, beneficio em %, antes → depois e "Faltam N").
    /// DM §109: as frases saem do Loc no idioma atual (LocTests confere as larguras nos 3); aqui so a escolha e os numeros.
    /// </summary>
    public static class Textos
    {
        public const int DicaMax = 26;
        /// <summary>ID de cada item (por handle) nas chaves item.*, dica.balcao.* e dica.pegue.*: o ItemDef.Id do nucleo (B-CORE-01).</summary>
        public static readonly string[] ItemId = Array.ConvertAll(Items.All, d => d.Id);
        /// <summary>ID de cada estacao por Station.Index (anexado no fim, nunca muda) na chave est.*. ponytail: vira StationDef.Id.</summary>
        public static readonly string[] EstacaoId = { "deposito", "fornalha", "fornalha2", "bigorna", "bigorna2", "escudos", "ferramentas", "balcao", "joalheria", "loja_joias" };

        public static string Dica(Hint h, int arg, Sim s)
        {
            switch (h)
            {
                case Hint.BuyPad: int u = s.Pads[arg].Current(s); return u < 0 ? "" : Placa(u);
                case Hint.BuyMenu: return Menu(arg);
                case Hint.ProductToCounter: return Loc.T("dica.balcao." + ItemId[arg]);
                case Hint.IngotToCrafter: return Loc.T("dica.lingote_bigorna");
                case Hint.OreToFurnace: return Loc.T("dica.minerio_fornalha");
                case Hint.PickProducts: return Loc.T("dica.pegue." + ItemId[arg]);
                case Hint.PickIngots: return Loc.T("dica.pegue_lingotes");
                case Hint.ClientWaiting: return Loc.T("dica.cliente", ItemNome(arg, 1), ItemNome(arg, 2));
                case Hint.OpenChest: return Loc.T("dica.bau", Bau(s.Chests[arg].Index));
                default: return Loc.T("dica.minerio");
            }
        }

        /// <summary>"Pise na placa da Bigorna 2"; nome comprido cai para "Pise na placa: X" e depois "Placa: X".</summary>
        public static string Placa(int u)
        {
            string n = Nome(u);
            return Cabe(Loc.T("dica.placa", n, Artigo("art.de.", n)), Loc.T("dica.placa.curta", n), Loc.T("dica.placa.min", n));
        }

        /// <summary>"Compre o Fole em Melhorias"; nome comprido cai para "Compre a Mochila" (o botao Melhorias ja pulsa com o badge) e,
        /// se nem assim couber (es: "Compra los Ayudantes ágiles"), "Compra: X".</summary>
        public static string Menu(int u)
        {
            string n = Nome(u), a = Artigo("art.o.", n);
            return Cabe(Loc.T("dica.menu", n, a), Loc.T("dica.menu.curta", n, a), Loc.T("dica.menu.min", n));
        }

        static readonly string[] _nome = new string[Upgrades.Count];
        static Idioma _nomeEm;

        /// <summary>Nome da melhoria no idioma atual (up.&lt;Id&gt;.nome). Guardado: a placa pergunta a cada quadro.</summary>
        public static string Nome(int u)
        {
            if (_nomeEm != Loc.Atual) { Array.Clear(_nome, 0, _nome.Length); _nomeEm = Loc.Atual; }
            return _nome[u] ??= Loc.T("up." + Upgrades.All[u].Id + ".nome");
        }

        /// <summary>Item no singular (n == 1) ou plural.</summary>
        public static string ItemNome(int item, long n) => Loc.P("item." + ItemId[item], n);

        /// <summary>Rotulo da estacao no mundo, pelo Station.Index.</summary>
        public static string Estacao(int index) => Loc.T("est." + EstacaoId[index]);

        /// <summary>Rotulo do bau de marco `i` (Balance.Milestones): "15 espadas!", "50 vendas!".</summary>
        public static string Bau(int i)
        {
            MilestoneDef m = Balance.Milestones[i];
            return m.Item < 0 ? Loc.P("bau.vendas", m.Count, m.Count) : Loc.T("bau.item", m.Count, ItemNome(m.Item, m.Count));
        }

        static readonly string[] Escala = { "num.mi", "num.bi", "num.tri", "num.qua", "num.qui" };   // curtas (8 caracteres no maximo, "9,22 qui")

        /// <summary>Ouro da HUD: inteiro ate 999999; acima, ate 3 algarismos + escala ("1,23 mi", "12,3 bi", "5 bi") para caber
        /// na pilula numa linha (A-CORE-06: com long, 5000000000 quebrava em 2). Trunca: nunca mostra mais do que o jogador tem.
        /// DM §109: decimal e escala do idioma (num.*): pt-BR "1,23 mi", en "1.23M", es "1,23 M".</summary>
        public static string Ouro(long v)
        {
            if (v < 1000000) return v.ToString(CultureInfo.InvariantCulture);
            long scale = 1000000; int k = 0;
            while (v / scale >= 1000 && k < Escala.Length - 1) { scale *= 1000; k++; }
            long q = v / scale;
            int dec = q >= 100 ? 0 : q >= 10 ? 1 : 2;   // inteiro (ex.: 512), sem float: 1150000 nao vira "1,14"
            string f = dec == 0 ? "" : ((v % scale) / (scale / (dec == 2 ? 100 : 10))).ToString(CultureInfo.InvariantCulture).PadLeft(dec, '0').TrimEnd('0');
            return q.ToString(CultureInfo.InvariantCulture) + (f.Length > 0 ? Loc.T("num.dec") + f : "") + Loc.T(Escala[k]);
        }

        /// <summary>Titulo do cartao da encomenda: verbo + N + item no plural ("Venda 5 espadas").</summary>
        public static string Venda(int n, int item) => Loc.T("order.venda", n, ItemNome(item, n));

        /// <summary>Cartao travado do menu: "Precisa: Balcão 5" (curto, sem quebrar linha; up.&lt;Id&gt;.curto quando o nome nao cabe).</summary>
        public static string Precisa(int u)
        {
            string curto = "up." + Upgrades.All[u].Id + ".curto";
            return Loc.T("card.precisa", Loc.Tem(curto) ? Loc.T(curto) : Nome(u));
        }

        /// <summary>A-UI-18: comprado, travado (pre-requisito do menu nao comprado), disponivel (da para pagar) ou sem ouro.</summary>
        public static Cartao Estado(Sim s, int u)
        {
            int req = Upgrades.All[u].Requires;
            if (s.Bought[u]) return Cartao.Comprado;
            if (req >= 0 && !s.Bought[req]) return Cartao.Travado;
            return s.Gold >= s.T.Cost(u) ? Cartao.Disponivel : Cartao.SemOuro;
        }

        /// <summary>Cartao sem ouro: quanto falta ("Faltam 120", "Falta 1", "Faltam 1,2 mi").</summary>
        public static string Falta(long n) => Loc.P("card.falta", n, Ouro(n));

        /// <summary>
        /// A-UI-18 (DM §65): beneficio do cartao em %, lido do Balance e do Tuning `t` da partida (mudou o balanco, muda o cartao). Tempo menor = "N% mais rapida"
        /// (1 - depois/antes: 4 → 2,5 s = 38%); quantidade ou ritmo maior = "+N%" (depois/antes - 1). Cabe em 2 linhas do cartao.
        /// </summary>
        public static string Beneficio(Tuning t, int u)
        {
            switch ((Upgrade)u)
            {
                case Upgrade.FurnaceSpeed1: return Loc.T("card.fornalha", Menos(t.FurnaceTime0, t.FurnaceTime1));
                case Upgrade.FurnaceSpeed2: return Loc.T("card.fornalha", Menos(t.FurnaceTime1, t.FurnaceTime2));
                case Upgrade.HammerSpeed: return Loc.T("card.bancadas", Menos(t.HammerTime0, t.HammerTime1));
                case Upgrade.JewelSpeed: return Loc.T("card.joalheria", Menos(1f, t.JewelSpeedMul));
                case Upgrade.PlayerCapacity: return Loc.T("card.carga", Mais(Balance.PlayerCap, Balance.PlayerCapUp));
                case Upgrade.PlayerSpeed: return Loc.T("card.velocidade", Mais(Balance.PlayerSpeed, Balance.PlayerSpeedUp));
                case Upgrade.HelperSpeed: return Loc.T("card.velocidade", Mais(Balance.WorkerSpeed, Balance.WorkerSpeedUp));   // VALIDACAO_V05 #4: a carga vai no AntesDepois
                case Upgrade.CounterCapacity: return Loc.T("card.clientes", Mais(t.VitrineClientMul, 1f));   // intervalo x0,7 = ritmo x1,43
                case Upgrade.JewelVitrine: return Loc.T("card.joia", Mais(t.Price[(int)Item.Jewel], t.JewelPriceUp));
                case Upgrade.Counter5: case Upgrade.Counter6: case Upgrade.Counter7: case Upgrade.Counter8:
                    int n = u - (int)Upgrade.Counter5 + t.QueueCap0 + 1;
                    return Loc.T("card.vagas", Mais(n - 1, n));
                default: return Loc.T("up." + Upgrades.All[u].Id + ".desc");
            }
        }

        /// <summary>Linha de baixo do cartao: antes → depois em numeros (era o MenuBar.Effect); "" quando o antes depende de outra compra (Lupa).</summary>
        public static string AntesDepois(Tuning t, int u)
        {
            switch ((Upgrade)u)
            {
                case Upgrade.FurnaceSpeed1: return Loc.T("card.ad.s", N(t.FurnaceTime0), N(t.FurnaceTime1));
                case Upgrade.FurnaceSpeed2: return Loc.T("card.ad.s", N(t.FurnaceTime1), N(t.FurnaceTime2));
                case Upgrade.HammerSpeed: return Loc.T("card.ad.s", N(t.HammerTime0), N(t.HammerTime1));
                case Upgrade.PlayerCapacity: return Loc.T("card.ad.n", N(Balance.PlayerCap), N(Balance.PlayerCapUp));
                case Upgrade.PlayerSpeed: return Loc.T("card.ad.ms", N(Balance.PlayerSpeed), N(Balance.PlayerSpeedUp));
                case Upgrade.HelperSpeed: return Loc.T("card.ad.carga", N(Balance.WorkerCap), N(Balance.WorkerCapUp));
                case Upgrade.CounterCapacity: return Loc.T("card.ad.estoque", N(t.CounterCap0), N(t.CounterCap1));
                case Upgrade.JewelVitrine: return Loc.T("card.ad.fila", N(Balance.JewelQueueCap), N(Balance.JewelQueueCapUp));
                case Upgrade.Counter5: case Upgrade.Counter6: case Upgrade.Counter7: case Upgrade.Counter8:
                    int n = u - (int)Upgrade.Counter5 + t.QueueCap0 + 1;
                    return Loc.T("card.ad.vagas", N(n - 1), N(n));
                default: return "";   // ponytail: Lupa = x0,6 sobre o tempo que o Martelo veloz ja mudou; o % basta
            }
        }

        static string Menos(double antes, double depois) => Pct(1.0 - depois / antes) + "%";
        static string Mais(double antes, double depois) => "+" + Pct(depois / antes - 1.0) + "%";
        static long Pct(double f) => (long)Math.Round(100.0 * f, MidpointRounding.AwayFromZero);
        static string N(float v) => v.ToString("0.#", CultureInfo.InvariantCulture).Replace(".", Loc.T("num.dec"));   // decimal do idioma, sem depender da cultura do aparelho

        // ponytail: genero pela 1a palavra do nome no idioma (termina em a / as / os / es; serve ao pt-BR e ao es), so a Vitrine foge;
        // nome novo que fuja tambem entra aqui. O en nao usa artigo nos modelos.
        static string Artigo(string chave, string n)
        {
            int sp = n.IndexOf(' ');
            string w = sp < 0 ? n : n.Substring(0, sp);
            char z = w[w.Length - 1], y = w.Length > 1 ? w[w.Length - 2] : ' ';
            string g = z == 's' ? (y == 'a' ? "fp" : "mp") : z == 'a' || w == "Vitrine" ? "f" : "m";
            return Loc.T(chave + g);
        }

        static string Cabe(string a, string b, string c = null) => a.Length <= DicaMax ? a : b.Length <= DicaMax || c == null ? b : c;

        // ---------- metas (D-CORE-02): a frase sai do evento + filtro + alvo do MetaDef (sem texto por meta), nos 3 idiomas ----------

        /// <summary>Linha do painel de metas (1 linha) e o premio embaixo dela (MetasTests confere os 3 idiomas).</summary>
        public const int MetaMax = 32, PremioMax = 44;

        /// <summary>"Venda 50 produtos", "Fabrique 100 lingotes", "Compre: Esteira", "Ganhe 1.500 em vendas". `alvo` = o da vaga (o de s de
        /// renda ja vem em ouro, congelado quando a missao apareceu).</summary>
        public static string Meta(MetaDef d, long alvo)
        {
            string n = Qtd(alvo);
            switch (d.Evento)
            {
                case "product_sold":
                    return d.Conta == Conta.Distintos ? Loc.T("meta.linhas", n) : Loc.T("meta.vende", n, d.Filtro < 0 ? Loc.P("meta.produto", alvo) : ItemNome(d.Filtro, alvo));
                case "product_crafted": return Loc.T("meta.fabrica", n, ItemNome(d.Filtro, alvo));
                case "upgrade_buy": return d.Filtro >= 0 ? Loc.T("meta.compra_um", Nome(d.Filtro)) : Loc.P("meta.compra", alvo, n);
                case "currency_source": return Loc.T("meta.ganha", n);
                case "currency_sink": return Loc.T("meta.gasta", n);
                case "mission_complete": return Loc.P(d.Filtro == 0 ? "meta.diarias" : "meta.semanais", alvo, n);
                default: return Loc.P("meta." + d.Evento, alvo, n);   // order_done, offline_claim, vip_served, worker_hired, chest_opened, login_claim
            }
        }

        /// <summary>"+120 de ouro  ·  +100 XP  ·  +5 Renome" (o que existir); insignia pelo nome.</summary>
        public static string Premio(long ouro, int xp, int renome, string insignia)
        {
            string s = "";
            void Mais(string p) => s += (s.Length > 0 ? "  ·  " : "") + p;
            if (ouro > 0) Mais(Loc.T("meta.premio.ouro", Qtd(ouro)));
            if (xp > 0) Mais(Loc.T("meta.premio.xp", xp));
            if (renome > 0) Mais(Loc.T("meta.premio.renome", renome));
            if (insignia != null) Mais(Loc.T("meta.premio.insignia", Loc.T("ins." + insignia)));
            return s;
        }

        /// <summary>B-CORE-06: nome do nivel de Renome (selo da HUD &lt;= RenomeMax, cartao) e o aviso ao subir (&lt;= AvisoMax; LocTests).</summary>
        public const int RenomeMax = 20, AvisoMax = 30;
        public static string NomeRenome(int nivel) => Loc.T("renome." + nivel);
        public static string AvisoRenome(int nivel) => Loc.T("aviso.renome", nivel, NomeRenome(nivel));
        /// <summary>O que o nivel libera, em frase (cartao do Renome).</summary>
        public static string LiberaRenome(int nivel) => Loc.T("renome." + nivel + ".libera");

        /// <summary>Quantidade com separador de milhar do idioma ("1.500", "1,500"); de 1 milhao em diante, a escala do Ouro ("1,23 mi").</summary>
        public static string Qtd(long v) => v >= 1000000 ? Ouro(v) : v.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", Loc.T("num.mil"));
    }
}
