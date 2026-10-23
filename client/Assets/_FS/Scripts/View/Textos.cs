using System;
using System.Globalization;
using FS.Core;

namespace FS
{
    /// <summary>Estado do cartao de melhoria (A-UI-18); a ordem do enum e' a ordem da bandeja (compravel primeiro, travado no fim). Oculto (B-CORE-03):
    /// nivel de estacao antes da revelacao (GDD §19: nao explicar sistema futuro cedo).</summary>
    public enum Cartao { Disponivel, SemOuro, Travado, Comprado, Oculto }

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
        public static readonly string[] EstacaoId = { "deposito", "fornalha", "fornalha2", "bigorna", "bigorna2", "escudos", "ferramentas", "balcao", "joalheria", "loja_joias", "bigorna_longa" };

        // C-CORE-01: o distrito ativo (Distrito). Melhorias, itens e estacoes do MUNDO (dica, placa, cartao, rotulo, encomenda) saem daqui; as
        // metas, missoes e projetos sao do Imperio (o Old Forge nesta leva): ItemId/NomeOF. Padrao = o Old Forge (os testes de sempre).
        static UpgradeDef[] _ups = Upgrades.All;
        static string[] _itens = ItemId, _est = EstacaoId;
        static string _selo;   // C-CORE-07: o DistrictDef.Selo do distrito ativo (o cartao dele mostra os pontos)

        /// <summary>O Game chama ao montar a cena com o distrito ativo.</summary>
        public static void Distrito(DistrictDef d)
        {
            _ups = d.Upgrades;
            _itens = Array.ConvertAll(d.Itens, x => x.Id);
            // C-CORE-06: o deposito, a fornalha e o balcao do Quarter (IDs fixos do Sim) com o rotulo dele: est.artisan_quarter.deposito = "Minério cobre"
            _est = Array.ConvertAll(d.Estacoes, x => Loc.Tem("est." + d.Id + "." + x.Id) ? d.Id + "." + x.Id : x.Id);
            _nome = new string[_ups.Length];
            _selo = d.Selo;
        }

        /// <summary>C-CORE-06: o painel da bancada configuravel: a receita ativa e a troca em curso ou pendente (a linha de cima do toque).</summary>
        public static string Bancada(Sim s, Station st)
        {
            string r = Loc.T("bancada.receita", ItemNome((int)st.OutItem, 1));
            if (st.Proxima != null) return r + "\n" + Loc.T("bancada.pendente", ItemNome((int)st.Proxima.Output, 1), N(s.TempoTroca(st)));
            return st.Parada > 0f ? r + "\n" + Loc.T("bancada.parada", N((float)Math.Ceiling(st.Parada))) : r;
        }

        /// <summary>C-CORE-06: o botao do painel: a proxima receita liberada depois da ativa (ou da pendente), em rodizio; null = so uma.</summary>
        public static RecipeDef ProximaReceita(Sim s, Station st)
        {
            if (st.Receitas == null) return null;
            int a = Array.IndexOf(st.Receitas, st.Proxima ?? st.Recipe);
            for (int k = 1; k < st.Receitas.Length; k++)
            {
                int i = (a + k) % st.Receitas.Length;
                if (s.Liberada(st, i) && st.Receitas[i] != (st.Proxima ?? st.Recipe)) return st.Receitas[i];
            }
            return null;
        }

        public static string Dica(Hint h, int arg, Sim s)
        {
            switch (h)
            {
                case Hint.BuyPad: int u = s.Pads[arg].Current(s); return u < 0 ? "" : Placa(u);
                case Hint.BuyMenu: return Menu(arg);
                case Hint.ProductToCounter: return Loc.T("dica.balcao." + _itens[arg]);
                case Hint.IngotToCrafter: return Loc.T("dica.lingote_bigorna");
                case Hint.OreToFurnace: return Loc.T("dica.minerio_fornalha");
                case Hint.PickProducts: return Loc.T("dica.pegue." + _itens[arg]);
                case Hint.PickIngots: return Loc.T("dica.pegue_lingotes");
                case Hint.ClientWaiting: return Cabe(Loc.T("dica.cliente", ItemNome(arg, 1), ItemNome(arg, 2)), Loc.T("dica.cliente.curta", ItemNome(arg, 1), ItemNome(arg, 2)));
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

        static string[] _nome = new string[Upgrades.Count];
        static Idioma _nomeEm;

        /// <summary>Nome da melhoria `u` do distrito ativo no idioma atual (up.&lt;Id&gt;.nome). Guardado: a placa pergunta a cada quadro.</summary>
        public static string Nome(int u)
        {
            if (_nomeEm != Loc.Atual) { Array.Clear(_nome, 0, _nome.Length); _nomeEm = Loc.Atual; }
            return _nome[u] ??= Loc.T("up." + _ups[u].Id + ".nome");
        }

        /// <summary>Melhoria do Old Forge (indice da tabela de sempre): metas e missoes, que sao do Imperio.</summary>
        public static string NomeOF(int u) => Loc.T("up." + Upgrades.All[u].Id + ".nome");

        /// <summary>Item do distrito ativo no singular (n == 1) ou plural.</summary>
        public static string ItemNome(int item, long n) => Loc.P("item." + _itens[item], n);
        /// <summary>Item do Old Forge (metas, missoes e projetos).</summary>
        static string ItemNomeOF(int item, long n) => Loc.P("item." + ItemId[item], n);

        /// <summary>Rotulo da estacao no mundo, pelo Station.Index.</summary>
        public static string Estacao(int index) => Loc.T("est." + _est[index]);

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

        /// <summary>B-CORE-05: nome curto do tipo de encomenda (enc.tipo.N): "Grande", "Urgente"...</summary>
        public static string TipoEncomenda(int tipo) => Loc.T("enc.tipo." + tipo);

        /// <summary>B-CORE-05: titulo do cartao. Common de 1 item = o de sempre ("Venda 5 espadas"); freguês = "Grunk: 40 espadas"; os outros
        /// = "Urgente: 20 espadas", "Variado: 6 ferramentas + 9 espadas" (o best-fit do cartao encolhe a linha).</summary>
        public static string Encomenda(Encomenda e)
        {
            if (e.Def.ClientId == null && e.Tipo == OrderType.Common && e.Itens.Length == 1) return Venda(e.Alvo[0], (int)e.Itens[0]);
            var p = new string[e.Itens.Length];
            for (int k = 0; k < p.Length; k++) p[k] = e.Alvo[k].ToString(CultureInfo.InvariantCulture) + " " + ItemNome((int)e.Itens[k], e.Alvo[k]);
            string quem = e.Def.ClientId != null ? Loc.T("cli." + e.Def.ClientId) : TipoEncomenda((int)e.Tipo);
            return Loc.T("enc.titulo", quem, string.Join(" + ", p));
        }

        /// <summary>B-CORE-05: a conta na barra do cartao: "3/20", com a qualidade minima ("0/5 · Fina+") e o prazo de bonus ("3/20 · 3:12").</summary>
        public static string BarraEncomenda(Encomenda e)
        {
            string s = e.Progresso.ToString(CultureInfo.InvariantCulture) + "/" + e.Total.ToString(CultureInfo.InvariantCulture);
            if (e.Def.MinQuality > Quality.Normal) s += " · " + Loc.T("enc.min", Qualidade(e.Def.MinQuality));
            if (e.Prazo > 0f) s += " · " + ((int)e.Prazo / 60).ToString(CultureInfo.InvariantCulture) + ":" + ((int)e.Prazo % 60).ToString("00", CultureInfo.InvariantCulture);
            return s;
        }

        /// <summary>B-CORE-05: o que a entrega paga agora (no prazo, com o bonus do Rush), para o "+N" do cartao.</summary>
        public static long PremioEncomenda(Encomenda e) => e.Premio + (e.Prazo > 0f ? e.PremioBonus : 0);

        /// <summary>Cartao travado do menu: "Precisa: Balcão 5" (curto, sem quebrar linha; up.&lt;Id&gt;.curto quando o nome nao cabe).</summary>
        public static string Precisa(int u)
        {
            string curto = "up." + _ups[u].Id + ".curto";
            return Loc.T("card.precisa", Loc.Tem(curto) ? Loc.T(curto) : Nome(u));
        }

        /// <summary>A-UI-18: comprado, travado (pre-requisito do menu nao comprado), disponivel (da para pagar) ou sem ouro. B-CORE-07: sem o projeto
        /// que ele exige, oculto (o painel de projetos mostra de onde ele vem).</summary>
        public static Cartao Estado(Sim s, int u)
        {
            int req = s.UpgradeDefs[u].Requires;
            if (s.Bought[u]) return Cartao.Comprado;
            if (!s.TemProjeto(u)) return Cartao.Oculto;
            if (req >= 0 && !s.Bought[req]) return s.UpgradeDefs[u].Meta && !s.UpgradeDefs[req].Meta ? Cartao.Oculto : Cartao.Travado;   // o nivel II espera a revelacao
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
            switch (_ups[u].Efeito)   // C-CORE-01: pelo efeito (o Fole de couro tem o texto do Fole); no Old Forge, o proprio upgrade
            {
                case Upgrade.FurnaceSpeed1: return Loc.T("card.fornalha", Menos(t.FurnaceTime0, t.FurnaceTime1));
                case Upgrade.FurnaceSpeed2: return Loc.T("card.fornalha", Menos(t.FurnaceTime1, t.FurnaceTime2));
                case Upgrade.HammerSpeed: return Loc.T("card.bancadas", Menos(t.HammerTime0, t.HammerTime1));
                case Upgrade.JewelSpeed: return Loc.T("card.joalheria", Menos(1f, t.JewelSpeedMul));
                case Upgrade.PlayerCapacity: return Loc.T("card.carga", Mais(Balance.PlayerCap, Balance.PlayerCapUp));
                case Upgrade.PlayerSpeed: return Loc.T("card.velocidade", Mais(Balance.PlayerSpeed, Balance.PlayerSpeedUp));
                case Upgrade.HelperSpeed: return Loc.T("card.velocidade", Mais(Balance.WorkerSpeed, Balance.WorkerSpeedUp));   // VALIDACAO_V05 #4: a carga vai no AntesDepois
                case Upgrade.CounterCapacity: return Loc.T("card.clientes", Mais(t.VitrineClientMul, 1f));   // intervalo x0,7 = ritmo x1,43
                case Upgrade.JewelVitrine: return _ups == Upgrades.All ? Loc.T("card.joia", Mais(t.Price[(int)Item.Jewel], t.JewelPriceUp))
                    : Loc.T("card.ricos", Mais(t.JewelVitrineClientMul, 1f));   // C-CORE-06: a vitrine de luxo do Quarter (ricos x0,7 no intervalo)
                case Upgrade.Counter5: case Upgrade.Counter6: case Upgrade.Counter7: case Upgrade.Counter8:
                    int n = (int)_ups[u].Efeito - (int)Upgrade.Counter5 + t.QueueCap0 + 1;
                    return Loc.T("card.vagas", Mais(n - 1, n));
                default:   // B-CORE-03, GDD §19: "Qualidade +0,15 · vel. +5%"
                    if (_ups[u].Id == Sim.TrocaRapidaId) return Loc.T("card.troca", Menos(Sim.TrocaSeg, Sim.TrocaRapidaSeg));   // C-CORE-06
                    if (_ups[u].Id == Sim.Condensador || _ups[u].Id == Sim.Condensador2) return Loc.T("card.dreno", N(t.DrenoCondensador / 100f));   // C-CORE-08
                    return _ups[u].Station != null ? Loc.T("card.qualidade", Pontos(t.NivelEstacaoQ), Pct(t.NivelEstacaoVelocidade) + "%")
                        : _ups[u].Id == _selo ? Loc.T("card.selo", Pontos(t.SeloQ))   // C-CORE-07
                        : Loc.T("up." + _ups[u].Id + ".desc");
            }
        }

        /// <summary>B-CORE-03: pontos de qualidade (centesimos) como o jogo mostra: 15 -> "0,15" (decimal do idioma).</summary>
        public static string Pontos(int q) => (q / 100).ToString(CultureInfo.InvariantCulture) + Loc.T("num.dec") + (q % 100).ToString("00", CultureInfo.InvariantCulture);

        /// <summary>Nome do nivel de qualidade no idioma (Fina, Superior...).</summary>
        public static string Qualidade(Quality l) => Loc.T("qual." + (int)l);

        /// <summary>
        /// Barra de qualidade da estacao (GDD §19): o que sai a seguir pelo acumulador. Golpe valendo = "Golpe! Proxima +1"; q = 0 = "So Normal";
        /// fracao zero = "Sempre Fina"; senao "Fina em 7" (pecas ate o acumulador passar de 100) ou "Proxima: Fina!" na proxima.
        /// </summary>
        public static string Barra(Sim s, Station st)
        {
            if (st.Strike) return Loc.T("qual.golpe");
            int q = s.QualityPoints(st), frac = q % 100, piso = Math.Min(q / 100, (int)Quality.Superior);
            if (q == 0) return Loc.T("qual.so_normal");
            if (frac == 0 || piso == (int)Quality.Superior) return Loc.T("qual.sempre", Qualidade((Quality)piso));
            int n = (100 - st.QAcc + frac - 1) / frac;   // pecas ate acc >= 100 (arredonda para cima)
            Quality prox = (Quality)(piso + 1);
            return n <= 1 ? Loc.T("qual.agora", Qualidade(prox)) : Loc.T("qual.em", Qualidade(prox), n);
        }

        /// <summary>QualityBreakdown ao tocar na barra: uma linha por fonte (o nivel da estacao sempre; as outras so quando existem) e o total com
        /// a barra. Explica 100% das pecas: a soma das linhas e' o q do acumulador.</summary>
        public static string Detalhe(Sim s, Station st)
        {
            int[] b = s.QualityBreakdown(st);
            var linhas = new System.Collections.Generic.List<string>();
            for (int f = 0; f < b.Length; f++)
                if (b[f] != 0 || f == (int)QualitySource.StationLevel) linhas.Add(Loc.T("qual.linha", Loc.T("qual.fonte." + f), Pontos(b[f])));
            linhas.Add(Loc.T("qual.total", Pontos(s.QualityPoints(st)), Barra(s, st)));
            return string.Join("\n", linhas);
        }

        /// <summary>Linha de baixo do cartao: antes → depois em numeros (era o MenuBar.Effect); "" quando o antes depende de outra compra (Lupa).</summary>
        public static string AntesDepois(Tuning t, int u)
        {
            switch (_ups[u].Efeito)
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
                    int n = (int)_ups[u].Efeito - (int)Upgrade.Counter5 + t.QueueCap0 + 1;
                    return Loc.T("card.ad.vagas", N(n - 1), N(n));
                default:
                    // B-CORE-03: depois da compra, so pelo nivel da estacao (as outras fontes ainda nao existem): L2 = 1 em 7, L3 = 1 em 3.
                    // ponytail: a frase vale para q < 100; o nivel que passar de 1,00 (L4-L5 com oficial, B-CORE-04/07) pede outra
                    if (_ups[u].Id == Sim.TrocaRapidaId) return Loc.T("card.ad.s", N(Sim.TrocaSeg), N(Sim.TrocaRapidaSeg));   // C-CORE-06: troca de receita
                    if (_ups[u].Id == Sim.Condensador || _ups[u].Id == Sim.Condensador2)   // C-CORE-08: o dreno base, +2 por condensador ja comprado antes
                    {
                        float d0 = (t.DrenoBase + (_ups[u].Id == Sim.Condensador2 ? t.DrenoCondensador : 0)) / 100f;
                        return Loc.T("card.ad.dreno", N(d0), N(d0 + t.DrenoCondensador / 100f));
                    }
                    if (_ups[u].Station == null) return "";   // ponytail: Lupa = x0,6 sobre o tempo que o Martelo veloz ja mudou; o % basta
                    int q = (NivelEstacao(u) - 1) * t.NivelEstacaoQ;
                    return Loc.T("card.ad.fina", (long)Math.Round(100.0 / q, MidpointRounding.AwayFromZero));
            }
        }

        /// <summary>Upgrades.StationLevel na tabela do distrito ativo.</summary>
        static int NivelEstacao(int u)
        {
            int n = 1;
            for (int r = u; r >= 0 && _ups[r].Station != null; r = _ups[r].Requires) n++;
            return n;
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
                case "product_sold":   // raia H: Ref = item de outro distrito pelo ID ("Venda 100 elmos")
                    return d.Conta == Conta.Distintos ? Loc.T("meta.linhas", n) : Loc.T("meta.vende", n, d.Ref != null ? Loc.P("item." + d.Ref, alvo)
                        : d.Filtro < 0 ? Loc.P("meta.produto", alvo) : ItemNomeOF(d.Filtro, alvo));
                case "product_crafted": return Loc.T("meta.fabrica", n, ItemNomeOF(d.Filtro, alvo));
                case "upgrade_buy": return d.Filtro >= 0 ? Loc.T("meta.compra_um", NomeOF(d.Filtro)) : Loc.P("meta.compra", alvo, n);
                case "currency_source": return Loc.T("meta.ganha", n);
                case "currency_sink": return Loc.T("meta.gasta", n);
                case "mission_complete": return Loc.P(d.Filtro == 0 ? "meta.diarias" : "meta.semanais", alvo, n);
                case "order_complete":   // B-CORE-05: por tipo, ou "os 7 tipos" (Distintos)
                    return d.Conta == Conta.Distintos ? Loc.T("meta.order_tipos", n) : Loc.P("meta.order_complete", alvo, n, TipoEncomenda(d.Filtro));
                // B-CORE-04: worker_hired com filtro = papel ("Contrate 1 oficial"); worker_upgrade com filtro = nivel ("Leve 13 ajudantes ao nivel 5")
                case "worker_hired" when d.Filtro > 0: return Loc.T("meta.worker_papel", n, Loc.P("wk.papel." + d.Filtro, alvo));
                case "worker_upgrade" when d.Filtro >= 2: return Loc.P("meta.worker_nivel", alvo, n, d.Filtro);
                // B-CORE-08: "Obra-prima: Lamina Solar do Rei" (filtro = indice no Hall); "Faca 3 obras-primas de espada" (filtro = item da familia)
                case "masterwork_unlock" when d.Filtro >= 0: return Loc.T("meta.masterwork_um", Loc.T("mw." + MasterworksDefs.All[d.Filtro].Id));
                case "masterwork_family": return Loc.T("meta.masterwork_familia", n, ItemNome(d.Filtro, 1));
                // metas de sistemas (raia H): as frases das missoes de evento (qualidade, golpe), o Charter pelo distrito, a Collection pela aba
                case "quality_crafted": case "quality_superior":
                    return Loc.T(d.Evento == "quality_crafted" ? "missao.fina" : "missao.superior", n, d.Filtro >= 0 ? ItemNomeOF(d.Filtro, alvo) : Loc.P("missao.peca", alvo));
                case "master_strike": return Loc.P("missao.golpe", alvo, n);
                case "district_unlock" when d.Ref == null: return Loc.P("meta.distritos", alvo, n);   // quantos abertos (ach_distritos3)
                case "district_unlock": return Loc.T("meta.district_unlock", Loc.T("distrito." + d.Ref));
                case "collection_milestone":
                    MarcoColecaoDef m = Array.Find(MasterworksDefs.Marcos, x => x.Id == d.Ref);
                    return Loc.T("meta.collection_milestone", m != null ? Loc.T("col.aba." + (int)m.Aba) : d.Ref);
                case "station_level": return Loc.P("meta.station_level", alvo, n, d.Filtro);
                default: return Loc.P("meta." + d.Evento, alvo, n);   // order_done, offline_claim, vip_served, worker_hired, worker_upgrade, chest_opened, login_claim,
                                                                     // contract_complete, blueprint_unlock, reputation_level, event_complete, event_enter, pass_level
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

        // ---------- LiveOps (D-CORE-03/04): nome do evento e do Passe, frase da missao (<= MetaMax), recompensa (<= PremioMax) e tempo ----------

        /// <summary>Nome do evento (aviso "Evento: X" &lt;= AvisoMax) e o chip da HUD ("1.000 pts · 6d 23h").</summary>
        public const int EventoMax = 22, ChipMax = 20;
        public static string NomeEvento(EventDef e) => Loc.T("ev." + e.Id);
        public static string NomePasse(PassDef p) => Loc.T("passe." + p.Id);

        /// <summary>"Venda 100 espadas", "Fabrique 20 escudos (Fina+)", "Acerte 5 golpes em escudos", "Suba 1 nível: Escudos", "Jogue em 3 dias do
        /// evento": a frase sai do gatilho + filtro + alvo, como a das metas.</summary>
        public static string Missao(MissaoEvento x)
        {
            string n = Qtd(x.Alvo);
            int item = x.Filtro.Length == 1 ? Items.IndexOf(x.Filtro[0]) : -1;
            string coisa = item >= 0 ? ItemNomeOF(item, x.Alvo) : Loc.P("meta.produto", x.Alvo);
            switch (x.Evento)
            {
                case "product_sold": return Loc.T("meta.vende", n, coisa);
                case "product_crafted": return Loc.T("meta.fabrica", n, coisa);
                case "order_done": return item >= 0 ? Loc.P("missao.order_item", x.Alvo, n, ItemNomeOF(item, 2)) : Loc.P("meta.order_done", x.Alvo, n);
                case "upgrade_buy":
                    if (x.Filtro.Length == 0) return Loc.P("meta.compra", x.Alvo, n);
                    int u = Upgrades.IndexOf(x.Filtro[0]);
                    return Upgrades.All[u].Station != null ? Loc.T("missao.nivel", n, Loc.T("est." + Upgrades.All[u].Station)) : Loc.T("meta.compra_um", NomeOF(u));
                case "quality_crafted": case "quality_superior":
                    return Loc.T(x.Evento == "quality_crafted" ? "missao.fina" : "missao.superior", n, item >= 0 ? coisa : Loc.P("missao.peca", x.Alvo));
                case "master_strike": return item >= 0 ? Loc.P("missao.golpe_item", x.Alvo, n, ItemNomeOF(item, 2)) : Loc.P("missao.golpe", x.Alvo, n);
                case "order_complete":   // B-CORE-05: "Entregue 2 do tipo Real" (filtro = LiveOpsDefs.TiposEncomenda, o nome minusculo do OrderType)
                    return Enum.TryParse(x.Filtro[0], true, out OrderType t) ? Loc.P("meta.order_complete", x.Alvo, n, TipoEncomenda((int)t)) : Loc.P("meta.order_done", x.Alvo, n);
                default: return Loc.P("meta." + x.Evento, x.Alvo, n);   // offline_claim, vip_served, worker_hired, play_day
            }
        }

        /// <summary>"+60 de ouro  ·  +200 XP  ·  Tapete real do balcão": ouro no ouro de agora (s x taxa); cosmetico pelo nome; Blueprint pelo
        /// tipo. Conveniencia e Gems so no premium (desligado: nunca aparece).</summary>
        public static string Premios(RewardDef[] rs, Sim s)
        {
            string t = "";
            foreach (RewardDef r in rs)
            {
                string p = r.Tipo == TipoPremio.Ouro ? Loc.T("meta.premio.ouro", Qtd(s.Metas.Ouro(r.Valor)))
                    : r.Tipo == TipoPremio.PassXp ? Loc.T("meta.premio.xp", r.Valor)
                    : r.Tipo == TipoPremio.Renome ? Loc.T("meta.premio.renome", r.Valor)
                    : r.Tipo == TipoPremio.Cosmetico ? Loc.T("cos." + r.Ref)
                    : r.Tipo == TipoPremio.Blueprint ? Loc.T("liveops.projeto") : null;
                if (p != null) t += (t.Length > 0 ? "  ·  " : "") + p;
            }
            return t;
        }

        /// <summary>"6d 23h", "5h 12m", "8m" (o que falta do evento ou da temporada).</summary>
        public static string Tempo(long s) => s >= 86400 ? Loc.T("tempo.dh", s / 86400, s % 86400 / 3600)
            : s >= 3600 ? Loc.T("tempo.hm", s / 3600, s % 3600 / 60) : Loc.T("tempo.m", Math.Max(1, s / 60));

        /// <summary>B-CORE-06: nome do nivel de Renome (selo da HUD &lt;= RenomeMax, cartao) e o aviso ao subir (&lt;= AvisoMax; LocTests).</summary>
        public const int RenomeMax = 20, AvisoMax = 30;
        public static string NomeRenome(int nivel) => Loc.T("renome." + nivel);
        public static string AvisoRenome(int nivel) => Loc.T("aviso.renome", nivel, NomeRenome(nivel));
        /// <summary>O que o nivel libera, em frase (cartao do Renome).</summary>
        public static string LiberaRenome(int nivel) => Loc.T("renome." + nivel + ".libera");

        // ---------- workers (B-CORE-04): cartao de contratar / subir nivel na fileira do menu e o aviso ----------

        /// <summary>Estado do cartao do worker `k`: oculto antes da revelacao (Equipe.Aberta) e no ajudante ainda sem o upgrade; comprado = nivel
        /// maximo (o cartao some); travado = requisito de contratacao faltando.</summary>
        public static Cartao EstadoWorker(Sim s, int k)
        {
            Equipe e = s.Equipe;
            if (!e.Aberta || e.Defs[k].Contrata != null && !e.Contratado(k)) return Cartao.Oculto;
            long c = e.Custo(k);
            if (c < 0) return Cartao.Comprado;
            if (!e.Contratado(k) && !e.Liberado(k)) return Cartao.Travado;
            return s.Gold >= c ? Cartao.Disponivel : Cartao.SemOuro;
        }

        /// <summary>Papel no singular, com maiuscula ("Oficial", "Mestre de obras").</summary>
        public static string Papel(WorkerRole p) => Maiuscula(Loc.P("wk.papel." + (int)p, 1));

        static string Maiuscula(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        /// <summary>O que a proxima compra de `k` faz (contratar ou o proximo nivel), em % ou pontos; cabe em 2 linhas do cartao (LocTests).</summary>
        public static string BeneficioWorker(Sim s, int k)
        {
            Equipe e = s.Equipe;
            int n = e.Nivel(k) + 1;   // nivel depois da compra (1 = contratar)
            switch (e.Defs[k].Papel)
            {
                case WorkerRole.Porter:
                    string v = "+" + Pct(WorkerDefs.PorterVel) + "%";
                    return Array.IndexOf(WorkerDefs.PorterCargaEm, n) >= 0 ? Loc.T("card.wk.passo_carga", v) : Loc.T("card.velocidade", v);
                case WorkerRole.Smith:
                    if (n == 1) return Loc.T("card.qualidade", Pontos(WorkerDefs.SmithQ), Pct(WorkerDefs.SmithVel) + "%");
                    return n == 5 ? Loc.T("card.qualidade", Pontos(WorkerDefs.SmithQ5 - WorkerDefs.SmithQ), Pct(WorkerDefs.SmithVelNivel) + "%")
                        : Loc.T("card.velocidade", "+" + Pct(WorkerDefs.SmithVelNivel) + "%");
                case WorkerRole.Clerk: return Loc.T("card.wk.paciencia", "+" + Pct(n == 1 ? WorkerDefs.ClerkPaciencia : WorkerDefs.ClerkPacienciaNivel) + "%");
                case WorkerRole.Quartermaster: return n == 1 ? Loc.T("card.wk.saida", WorkerDefs.QmSaida) : Loc.T("card.wk.estoque", WorkerDefs.QmEstoqueNivel);
                case WorkerRole.Smelter:   // C-CORE-05: q nas montagens (+15, +10 no 5, +5 no 10) e o ritmo da fornalha de liga
                    if (n == 1 || n == 5 || n == 10)
                        return Loc.T("card.qualidade", Pontos(n == 1 ? WorkerDefs.SmelterQ : n == 5 ? WorkerDefs.SmelterQ5 - WorkerDefs.SmelterQ : WorkerDefs.SmelterQ10 - WorkerDefs.SmelterQ5),
                            Pct(n == 1 ? WorkerDefs.SmelterVel : WorkerDefs.SmelterVelNivel) + "%");
                    return Loc.T("card.velocidade", "+" + Pct(WorkerDefs.SmelterVelNivel) + "%");
                case WorkerRole.Courier: return Loc.T("card.wk.mensageiro", s.T.ImportNivel, s.T.ExportNivel);   // C-CORE-09: o Felix, por nivel
                default:   // mestre de obras
                    return n == 1 ? Loc.T("card.qualidade", Pontos(WorkerDefs.ForemanQ), Pct(WorkerDefs.ForemanVel) + "%")
                        : Loc.T("card.velocidade", "+" + Pct(WorkerDefs.ForemanVelNivel) + "%");
            }
        }

        /// <summary>Linha de baixo do cartao: contratar = o papel (o oficial: a estacao da familia); subir = "Nivel 2 → 3".</summary>
        public static string AntesDepoisWorker(Sim s, int k)
        {
            WorkerDef d = s.Equipe.Defs[k];
            int n = s.Equipe.Nivel(k);
            if (n > 0) return Loc.T("card.ad.nivel", n, n + 1);
            int st = d.Papel == WorkerRole.Smith ? s.Stations.FindIndex(x => x.Produces && x.Recipe.Station == d.Station) : -1;
            return st >= 0 ? Estacao(st) : Papel(d.Papel);
        }

        /// <summary>Cartao travado: "Precisa: Nível II" (o upgrade do requisito) ou "Precisa: Renome 2".</summary>
        public static string PrecisaWorker(Sim s, int k)
        {
            WorkerDef d = s.Equipe.Defs[k];
            int u = d.Requer == null ? -1 : Upgrades.IndexOf(d.Requer, s.UpgradeDefs);
            return u >= 0 && !s.Bought[u] ? Precisa(u) : Loc.T("card.precisa_renome", RenomeDefs.NivelQueLibera(d.Id));
        }

        /// <summary>Aviso da compra: "Bruna na equipe!" ou "Bruna: nível 2!" (&lt;= AvisoMax).</summary>
        public static string AvisoWorker(WorkerDef d, int nivel) => nivel <= 1 ? Loc.T("aviso.contratou", d.Nome) : Loc.T("aviso.worker_nivel", d.Nome, nivel);

        // ---------- projetos (B-CORE-07): nome, o que libera e de onde vem, nos 3 idiomas (o PainelProjetos so desenha) ----------

        /// <summary>"Técnica: espadas", "Mestre: escudos, ferramentas, joias", "Obra-prima: Primeira Lâmina", "Receita: espada longa".</summary>
        public static string ProjetoNome(Sim s, BlueprintDef d) => Loc.T("proj.k." + (int)d.Kind, Alvos(s, d, 1));

        /// <summary>O que a posse libera: a linha, os niveis IV-V, os pontos da tecnica, o projeto da obra-prima ou o visual.</summary>
        public static string ProjetoLibera(Sim s, BlueprintDef d) => d.Kind == BlueprintKind.Technique
            ? (Items.IndexOf(d.Targets[0], s.ItemDefs) >= 0 ? Loc.T("proj.libera.2i", Pontos(s.T.TecnicaItemQ)) : Loc.T("proj.libera.2", Pontos(s.T.TecnicaQ)))   // C-CORE-06: tecnica de produto
            : d.Kind == BlueprintKind.Recipe ? Loc.T("proj.libera.0", Alvos(s, d, 2)) : Loc.T("proj.libera." + (int)d.Kind);

        /// <summary>De onde vem: as fontes na ordem do BlueprintDef ("Venda 100 espadas (37/100) ou Encomenda de projeto nº 7").</summary>
        public static string ProjetoFonte(Sim s, BlueprintDef d) => string.Join(Loc.T("proj.ou"), Array.ConvertAll(d.Sources, f => Fonte(s, f)));

        static string Alvos(Sim s, BlueprintDef d, long n) => string.Join(", ", Array.ConvertAll(d.Targets, t => Alvo(s, d.Kind, t, n)));

        /// <summary>Familia (nivel, tecnica) = o nome dela (fam.*, C-CORE-05: as que juntam varios produtos, como as montagens) ou o produto
        /// da receita base dela no plural; Masterwork pelo nome; receita e visual = o item.</summary>
        static string Alvo(Sim s, BlueprintKind k, string t, long n)
        {
            if (k == BlueprintKind.Masterwork) return Loc.T("mw." + t);
            if (k == BlueprintKind.StationLevel || k == BlueprintKind.Technique)
            {
                if (Loc.Tem("fam." + t)) return Loc.T("fam." + t);
                int it = Items.IndexOf(t, s.ItemDefs);
                if (it >= 0) return ItemDe(s, it, 2);   // C-CORE-06: tecnica de produto (lapidacao: colares, caixas de joias)
                RecipeDef r = Array.Find(s.RecipeDefs, x => x.Family == t);
                return r != null ? ItemDe(s, (int)r.Output, 2) : t;
            }
            int i = Items.IndexOf(t, s.ItemDefs);
            return i >= 0 ? ItemDe(s, i, n) : t;
        }

        /// <summary>C-CORE-05: o item `i` da tabela do Sim `s` (o do distrito do painel), no singular ou plural. No Old Forge = o ItemNomeOF.</summary>
        static string ItemDe(Sim s, int i, long n) => Loc.P("item." + s.ItemDefs[i].Id, n);

        static string Fonte(Sim s, BlueprintSource f)
        {
            switch (f.Kind)
            {
                case BlueprintSourceKind.Milestone:
                    if (f.Ref == BlueprintSource.Producao) return Loc.T("proj.f.producao");
                    if (f.Ref == BlueprintSource.Cotas) return Loc.T("proj.f.cotas", Qtd(f.N), Qtd(Math.Min(f.N, s.CotasFechadas)));   // C-CORE-07
                    if (f.Ref == BlueprintSource.Energizadas) return Loc.T("proj.f.energizadas", Qtd(f.N), Qtd(Math.Min(f.N, s.EnergizadasTotal)));   // C-CORE-08
                    if (f.Ref == BlueprintSource.Lotes) return Loc.T("proj.f.lotes", Qtd(f.N), Qtd(Math.Min(f.N, s.ExportLots)));     // C-CORE-09
                    int i = Items.IndexOf(f.Ref, s.ItemDefs);
                    if (i < 0) return f.Ref;
                    bool fina = f.MinQ >= Quality.Fine;
                    long feito = Math.Min(f.N, fina ? s.Projetos.Finas[i] : s.SoldItems[i]);
                    return Loc.T(fina ? "proj.f.fina" : "proj.f.0", Qtd(f.N), ItemDe(s, i, f.N), Qtd(feito));
                case BlueprintSourceKind.Achievement:
                    MetaDef m = MetasDefs.Achar(f.Ref);
                    return Loc.T("proj.f.4", m != null ? Meta(m, m.Alvo) : f.Ref);
                default: return Loc.T("proj.f." + (int)f.Kind, f.N);   // filas: a posicao; Renome: o nivel; o resto sem numero
            }
        }

        // ---------- Hall e Collection (B-CORE-08): o PainelHall so desenha; MasterworksTests confere os 3 idiomas ----------

        /// <summary>Linha do Hall (&lt;= HallMax) e linhas de detalhe (condicao, projeto: 1 linha de 24 px com best fit, &lt;= HallDetalheMax).</summary>
        public const int HallMax = 28, HallDetalheMax = 64;

        public static string NomeMasterwork(MasterworkDef d) => Loc.T("mw." + d.Id);

        /// <summary>Selo da linha: NO HALL, PRONTA (projeto e condicoes: falta o golpe numa Superior) ou BLOQUEADO.</summary>
        public static string SeloMasterwork(Sim s, MasterworkDef d) =>
            Loc.T(s.Hall.Tem(d.Id) ? "hall.no_hall" : s.Hall.Liberada(d) ? "hall.pronta" : "proj.falta");

        /// <summary>A condicao especial com o progresso ("Bigorna nível 5 (3/5)"; varias: " · "); sem nenhuma, "Sem condição extra". Registrada:
        /// "No Hall desde 13/10/2026" (data 0 = sem relogio: so "NO HALL" no selo, a linha fica a da condicao).</summary>
        public static string CondicaoMasterwork(Sim s, MasterworkDef d)
        {
            if (s.Hall.Feitas.TryGetValue(d.Id, out long u) && u > 0)
                return Loc.T("hall.desde", DateTimeOffset.FromUnixTimeSeconds(u).ToString(Loc.T("hall.data"), CultureInfo.InvariantCulture));
            if (d.Condicoes.Length == 0) return Loc.T("hall.sem_condicao");
            return string.Join(" · ", Array.ConvertAll(d.Condicoes, c => Condicao(s, c)));
        }

        static string Condicao(Sim s, CondicaoMasterwork c)
        {
            string n = Qtd(c.N), v = Qtd(Math.Min(c.N, s.Hall.Valor(c)));
            int i = Items.IndexOf(c.Ref, s.ItemDefs);
            string item = i >= 0 ? ItemDe(s, i, c.N) : c.Ref;
            switch (c.Kind)
            {
                case CondicaoMw.NivelEstacao: return Loc.T("hall.c.1", Loc.Tem("est." + c.Ref) ? Loc.T("est." + c.Ref) : Loc.Tem("fam." + c.Ref) ? Maiuscula(Loc.T("fam." + c.Ref)) : c.Ref, n, v);
                case CondicaoMw.Renome: return Loc.T("hall.c.2", n, v);
                case CondicaoMw.EncomendasItem: return Loc.T("hall.c.3", n, i >= 0 ? ItemDe(s, i, 1) : c.Ref, v);
                case CondicaoMw.PecasComLiga: return Loc.T("hall.c.7", n, item, v);   // C-CORE-05
                case CondicaoMw.CotaBlocos: return Loc.T("hall.c.10", n, item, v);     // C-CORE-07
                case CondicaoMw.CotaNoPrazo: return Loc.T("hall.c.11", n, item, v);
                case CondicaoMw.Contratos: return Loc.T("hall.c.12", n, v);
                case CondicaoMw.PecasEnergizadas: return Loc.T("hall.c.energizadas", n, item, v);   // C-CORE-08
                case CondicaoMw.SequenciaEstavel: return Loc.T("hall.c.sequencia", Loc.Tem("est." + c.Ref) ? Loc.T("est." + c.Ref) : c.Ref, n, v);
                case CondicaoMw.Importados: return Loc.T("hall.c.13", n, v);   // C-CORE-09
                case CondicaoMw.Lotes: return Loc.T("hall.c.14", n, v);
                case CondicaoMw.Charters: return Loc.T("hall.c.15", n, v);
                case CondicaoMw.EncomendasTipo: return Loc.T("hall.c.4", n, Loc.Tem("hall.tipo." + c.Ref) ? Loc.T("hall.tipo." + c.Ref) : c.Ref, v);
                case CondicaoMw.NivelWorker:
                    int k = WorkerDefs.IndexOf(c.Ref, s.Equipe.Defs);
                    return Loc.T("hall.c.5", k >= 0 ? s.Equipe.Defs[k].Nome : c.Ref, n, v);
                case CondicaoMw.VendidosQualidade: return Loc.T("hall.c.6", n, item + " " + Qualidade(c.MinQ), v);
                case CondicaoMw.ClientesRicos: return Loc.T("hall.c.8", n, v);   // C-CORE-06: RichClients
                case CondicaoMw.Feitos: return Loc.T("hall.c.9", n, item, v);    // C-CORE-06: GemsCut
                default: return Loc.T("hall.c.0", n, item, v);
            }
        }

        /// <summary>O projeto: "Projeto na mão" ou "Projeto: Venda 100 espadas (37/100)" (as fontes, como no painel de projetos).</summary>
        public static string ProjetoMasterwork(Sim s, MasterworkDef d)
        {
            if (s.Projetos.Tem(d.Blueprint)) return Loc.T("hall.projeto_tem");
            BlueprintDef b = Array.Find(s.Projetos.Defs, x => x.Id == d.Blueprint);
            return Loc.T("hall.projeto", b != null ? ProjetoFonte(s, b) : d.Blueprint);
        }

        /// <summary>Familias completas no Hall (o +5% no preco), uma por linha; vazio = nenhuma.</summary>
        public static string BonusHall(Sim s)
        {
            // uma linha por porcentagem ("+5% no preco de espadas, escudos, ..."): 4 familias em 4 linhas ficavam ilegiveis no aparelho
            var t = new System.Collections.Generic.SortedDictionary<int, System.Collections.Generic.List<string>>();
            for (int i = 0; i < s.ItemDefs.Length && i < ItemId.Length; i++)
            {
                int pct = s.ItemDefs[i].Family == s.ItemDefs[i].Id ? s.Hall.BonusPct((Item)i) : 0;
                if (pct <= 0) continue;
                if (!t.ContainsKey(pct)) t[pct] = new System.Collections.Generic.List<string>();
                t[pct].Add(ItemNome(i, 2));
            }
            var linhas = new System.Collections.Generic.List<string>();
            foreach (var kv in t) linhas.Add(Loc.T("hall.bonus", kv.Key, string.Join(", ", kv.Value)));
            return string.Join("\n", linhas);
        }

        public static string AbaColecao(AbaColecao a) => Loc.T("col.aba." + (int)a);

        /// <summary>"Produtos 6/15 · Obras-primas 1/10 · Equipe 9/26 · Projetos 10/19" (o cabecalho da aba).</summary>
        public static string ResumoColecao(Sim s) => string.Join("  ·  ", Array.ConvertAll((AbaColecao[])Enum.GetValues(typeof(AbaColecao)),
            a => Loc.T("col.resumo", AbaColecao(a), s.Colecao.Feitas(a) + "/" + s.Colecao.Total(a))));

        /// <summary>Linha do marco: "Produtos: 8 de 15".</summary>
        public static string MarcoColecao(Sim s, MarcoColecaoDef m) => Loc.T("col.marco", AbaColecao(m.Aba), Qtd(m.N), Qtd(s.Colecao.Total(m.Aba)));

        /// <summary>O premio do marco: "+10 Renome" e o cosmetico pelo nome.</summary>
        public static string PremioColecao(MarcoColecaoDef m)
        {
            string p = Premio(0, 0, m.Renome, null);
            return m.Cosmetico == null ? p : p + (p.Length > 0 ? "  ·  " : "") + Loc.T("cos." + m.Cosmetico);
        }

        /// <summary>Selo do marco: FEITO ou "feitas/N".</summary>
        public static string SeloColecao(Sim s, MarcoColecaoDef m) =>
            s.Colecao.Pago(m) ? Loc.T("col.feito") : Qtd(Math.Min(m.N, s.Colecao.Feitas(m.Aba))) + "/" + Qtd(m.N);
        // ---------- Royal Contracts (D-CORE-04): aba do painel de metas (nome, pedido, etapa e premio) ----------

        public static string ContratoNome(ContractDef d) => Loc.T("ctr." + d.Id);

        /// <summary>"Pede: 30 espadas + 20 escudos + 10 ferramentas" (as etapas na ordem; "Fina+" na qualidade minima; item que o build nao
        /// tem sai pelo ID, so no "proximo" de um distrito por vir).</summary>
        public static string ContratoPede(Sim s, ContractDef d)
        {
            string t = Pede(d, false);
            return t.Length <= PedeMax ? t : Pede(d, true);
        }

        /// <summary>C-CORE-07: a barra do pedido (22 px, ~60 colunas). Nao cabendo (a Coroacao do Arsenal: 3 itens "de gala" e Fina), o nome curto do
        /// item (itemc.*, quando existe) e a qualidade minima 1x no fim: "Pede: 10 espadas + 10 alabardas + 5 armaduras · Fina+".</summary>
        public const int PedeMax = 60;

        static string Pede(ContractDef d, bool curto)
        {
            var p = new string[d.Etapas.Length];
            // C-CORE-06: a mesma qualidade minima em todas as etapas sai 1x no fim (o dote da princesa passava da linha com "Fina+" em cada item);
            // C-CORE-07: no curto (nao coube), o nome curto do item e a maior qualidade minima 1x no fim
            bool uma = Array.TrueForAll(d.Etapas, e => e.MinQ == d.Etapas[0].MinQ) && d.Etapas[0].MinQ > Quality.Normal;
            Quality q = Quality.Normal;
            for (int k = 0; k < p.Length; k++)
            {
                ContractStage e = d.Etapas[k];
                string nome = curto && Loc.Tem("itemc." + e.Item) ? Loc.P("itemc." + e.Item, e.N) : ItemOuId(e.Item, e.N);
                p[k] = Qtd(e.N) + " " + nome + (e.MinQ > Quality.Normal && !uma && !curto ? " " + Loc.T("enc.min", Qualidade(e.MinQ)) : "");
                if (e.MinQ > q) q = e.MinQ;
            }
            return Loc.T("ctr.pede", string.Join(" + ", p)) + (uma || curto && q > Quality.Normal ? " · " + Loc.T("enc.min", Qualidade(q)) : "");
        }

        /// <summary>A barra do ativo: "Etapa 2/3: 7/20 escudos", com a qualidade minima e o prazo-bonus como na encomenda.</summary>
        public static string ContratoEtapa(Sim s, Contrato c)
        {
            ContractStage e = c.Atual;
            string t = Loc.T("ctr.etapa", Math.Min(c.Etapa + 1, c.Def.Etapas.Length), c.Def.Etapas.Length, Qtd(c.Feito) + "/" + Qtd(e.N) + " " + ItemOuId(e.Item, e.N));
            if (e.MinQ > Quality.Normal) t += " · " + Loc.T("enc.min", Qualidade(e.MinQ));
            if (c.Prazo > 0f) t += " · " + ((int)c.Prazo / 60).ToString(CultureInfo.InvariantCulture) + ":" + ((int)c.Prazo % 60).ToString("00", CultureInfo.InvariantCulture);
            return t;
        }

        /// <summary>O que paga agora (no prazo, com o bonus): ouro de s da taxa de agora, Renome e o projeto (Textos.Premio).</summary>
        public static string ContratoPremio(Sim s, ContractDef d, bool noPrazo)
        {
            int seg = noPrazo ? (int)Math.Round(d.OuroSeg * ContratosDefs.BonusOuro) : d.OuroSeg;
            string t = Premio(s.Metas.Ouro(seg), 0, d.Reputacao + (noPrazo ? ContratosDefs.BonusRenome : 0), null);
            return d.Blueprint != null ? t + "  ·  " + Loc.T("ctr.projeto") : t;
        }

        /// <summary>Nome do item pelo ID (C-CORE-05: o elmo da Row no cartao do contrato da raiz; pelo indice saia "20 elmo" ou o item do distrito
        /// ativo); item sem chave (distrito por vir) sai pelo ID.</summary>
        static string ItemOuId(string id, long n) => Loc.Tem("item." + id) ? Loc.P("item." + id, n) : id;

        /// <summary>Quantidade com separador de milhar do idioma ("1.500", "1,500"); de 1 milhao em diante, a escala do Ouro ("1,23 mi").</summary>
        public static string Qtd(long v) => v >= 1000000 ? Ouro(v) : v.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", Loc.T("num.mil"));

        // ---- royal_arsenal (C-CORE-07): o patio de cotas (rotulo do patio, uma linha por faixa; o toque troca o produto da faixa)

        /// <summary>Linha da faixa `k`: "Faixa 1: 3/10 kits do batedor · 2:31 → insígnias reais"; sem produto, "Faixa 1: vazia (toque)". O prazo
        /// (m:ss) so com peca no bloco e ainda dentro dele; a seta = o produto do proximo bloco quando o jogador trocou.</summary>
        public static string Faixa(Sim s, int k)
        {
            FaixaCota f = s.Faixas[k];
            if (f.Produto < 0) return Loc.T("cota.faixa", k + 1, f.Proximo >= 0 ? ItemNome(f.Proximo, 2) : Loc.T("cota.vazia"));
            string t = Loc.T("cota.bloco", f.Pecas, f.Tamanho, ItemNome(f.Produto, f.Tamanho));
            float resta = s.CotaPrazo - f.Decorrido;
            if (f.Pecas > 0 && resta > 0f) t += Loc.T("cota.prazo", ((int)resta / 60).ToString(CultureInfo.InvariantCulture) + ":" + ((int)resta % 60).ToString("00", CultureInfo.InvariantCulture));
            if (f.Proximo != f.Produto) t += Loc.T("cota.proximo", f.Proximo >= 0 ? ItemNome(f.Proximo, 2) : Loc.T("cota.vazia"));
            return Loc.T("cota.faixa", k + 1, t);
        }

        /// <summary>O produto que o toque na faixa `k` escolhe: o proximo produto de linha aberta (na ordem dos itens) depois do escolhido agora
        /// que nao esta noutra faixa; depois do ultimo, vazia (-1); vazia volta ao 1o.</summary>
        public static int ProximoDaFaixa(Sim s, int k)
        {
            int atual = s.Faixas[k].Proximo, n = s.ItemDefs.Length;
            for (int i = atual + 1; i < n; i++)
            {
                if (i < (int)Item.Sword || !s.IsProduct((Item)i) || !s.LineUnlocked((Item)i)) continue;
                bool usado = false;
                for (int j = 0; j < s.Faixas.Length; j++) if (j != k && (s.Faixas[j].Produto == i || s.Faixas[j].Proximo == i)) usado = true;
                if (!usado) return i;
            }
            return -1;
        }

        /// <summary>Aviso do bloco pago (Ev.QuotaBlock): "Coroa pagou 1,2 mi!" (no prazo: "No prazo! ...").</summary>
        public static string AvisoCota(long ouro, bool prazo) => Loc.T(prazo ? "aviso.cota_prazo" : "aviso.cota", Ouro(ouro));

        // ---- arcane_foundry (C-CORE-08): o medidor do altar (a linha embaixo dele e o painel do toque, que troca o modo)

        /// <summary>O altar Estavel com a receita inteira na entrada esperando o medidor cair ("estabilizando", azul).</summary>
        public static bool Estabilizando(Sim s, Station st)
        {
            if (st.Forcado || st.Busy || st.Pausa > 0f || st.Medidor + s.CargaPeca(st) <= s.T.ArcanoTeto) return false;
            for (int k = 0; k < st.Ins.Length; k++) if (st.Ins[k] < st.Recipe.Inputs[k].Qty) return false;
            return true;
        }

        /// <summary>"Estável · 45/100", "Forçado · 82/100", "Estabilizando · 95/100" ou "Sobrecarga! 7 s" (o E inteiro, de 0 a 100+).</summary>
        public static string Medidor(Sim s, Station st)
        {
            if (st.Pausa > 0f) return Loc.T("arc.sobrecarga", N((float)Math.Ceiling(st.Pausa)));
            string e = (st.Medidor / 100).ToString(CultureInfo.InvariantCulture);
            return Estabilizando(s, st) ? Loc.T("arc.estabilizando", e) : Loc.T("arc.medidor", Loc.T(st.Forcado ? "arc.forcado" : "arc.estavel"), e);
        }

        // ---- grand_forge (C-CORE-09): as rotas (linha embaixo do cais) e o lote do Mensageiro (embaixo do patio de exportacao; o toque troca o produto)

        /// <summary>"Rotas: 6/min · 123 chegaram".</summary>
        public static string Rotas(Sim s) => Loc.T("rotas.linha", s.CapacidadeRotas, Qtd(s.ImportedUnits));

        /// <summary>"Lote: 7/10 sinos da catedral · 0:42" (o que falta para o Mensageiro partir); no automatico "auto: " antes do produto (o mais
        /// estocado agora).</summary>
        public static string Lote(Sim s)
        {
            int alvo = s.AlvoLote, n = s.TamanhoLote;
            string p = alvo >= 0 ? ItemNome(alvo, n) : "-";
            if (s.LoteProduto < 0) p = Loc.T("lote.auto", p);
            int resta = Math.Max(0, (int)Math.Ceiling(s.PeriodoLote - s.ExportT));
            return Loc.T("lote.linha", s.LoteTotal + "/" + n, p, (resta / 60).ToString(CultureInfo.InvariantCulture) + ":" + (resta % 60).ToString("00", CultureInfo.InvariantCulture));
        }

        /// <summary>O produto que o toque no lote escolhe: automatico -> o 1o produto de linha aberta -> ... -> o ultimo -> automatico (-1).</summary>
        public static int ProximoLote(Sim s)
        {
            for (int i = s.LoteProduto + 1; i < s.ItemDefs.Length; i++)
                if (i >= (int)Item.Sword && s.IsProduct((Item)i) && s.LineUnlocked((Item)i)) return i;
            return -1;
        }

        /// <summary>Aviso da partida (Ev.ExportLot): "Mensageiro levou 1,2 mi!".</summary>
        public static string AvisoLote(long ouro) => Loc.T("aviso.lote", Ouro(ouro));
    }
}
