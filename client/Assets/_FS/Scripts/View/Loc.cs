using System.Collections.Generic;
using System.Globalization;

namespace FS
{
    /// <summary>Idiomas do jogo (DM §109); a ordem e' a coluna da tabela do Loc.</summary>
    public enum Idioma { Pt, En, Es }

    /// <summary>
    /// DM §109 (preparar desde ja pt-BR, en, es; nao colocar texto em codigo): todo texto que o jogador le sai de Loc.T("chave", args).
    /// C# puro, sem UnityEngine (o coretests testa no LocTests). Chave estavel `dominio.item` -> L(pt-BR, en, es): o L obriga os 3 na
    /// compilacao e chave repetida derruba a carga (Add). Frase inteira por chave; {0} {1} = string.Format invariante (a ordem das
    /// palavras e' do idioma, um idioma pode ignorar um argumento). Plural minimo "singular|plural" (Loc.P: n == 1 escolhe o 1o, nos 3
    /// idiomas). Nomes de melhoria pelo ID do nucleo (up.&lt;UpgradeDef.Id&gt;.nome/.desc); item e estacao por ID da view (Textos.ItemId,
    /// Textos.EstacaoId) ate o ItemDef/StationDef trazerem o deles. O nucleo segue com o Name pt-BR so para log e validator.
    /// Idioma: -lang (dev, fotos) > Configuracoes (PlayerPrefs fs_idioma) > aparelho (Portuguese -> pt, Spanish -> es, o resto -> en).
    /// Numeros: pt-BR "1,23 mi"; en "1.23M"; es "1,23 M" (letras da notacao curta dos jogos idle, B = 10^9; ver num.*).
    /// ponytail: tabela em codigo, sem o com.unity.localization (pacote para baixar); vira CSV/XLIFF quando entrar tradutor de fora ou
    /// o 4o idioma. Chave que falta aparece como [chave] (o LocTests pega antes).
    /// </summary>
    public static class Loc
    {
        public static Idioma Atual = Idioma.Pt;
        static readonly string[] Codigos = { "pt", "en", "es" };

        public static string Codigo => CodigoDe(Atual);
        public static string CodigoDe(Idioma i) => Codigos[(int)i];

        public static string T(string chave) => Em(Atual, chave);
        public static string T(string chave, params object[] args) => string.Format(CultureInfo.InvariantCulture, T(chave), args);

        /// <summary>"singular|plural": n == 1 pega o 1o; sem "|" vale o texto todo. Depois formata com `args`.</summary>
        public static string P(string chave, long n, params object[] args)
        {
            string s = T(chave);
            int bar = s.IndexOf('|');
            if (bar >= 0) s = n == 1 ? s.Substring(0, bar) : s.Substring(bar + 1);
            return args.Length == 0 ? s : string.Format(CultureInfo.InvariantCulture, s, args);
        }

        public static string Em(Idioma i, string chave) => Tabela.TryGetValue(chave, out string[] t) ? t[(int)i] : "[" + chave + "]";
        public static bool Tem(string chave) => Tabela.ContainsKey(chave);
        public static IEnumerable<KeyValuePair<string, string[]>> Todas => Tabela;

        /// <summary>"pt", "pt-BR", "en", "en-US", "es", "es-419"... (maiuscula tanto faz); outro ou vazio = false.</summary>
        public static bool Ler(string codigo, out Idioma i)
        {
            i = Idioma.Pt;
            if (string.IsNullOrEmpty(codigo) || codigo.Length < 2) return false;
            int k = System.Array.IndexOf(Codigos, codigo.Substring(0, 2).ToLowerInvariant());
            if (k < 0 || (codigo.Length > 2 && codigo[2] != '-' && codigo[2] != '_')) return false;
            i = (Idioma)k;
            return true;
        }

        /// <summary>Application.systemLanguage.ToString(): Portuguese -> pt, Spanish -> es, qualquer outro -> en.</summary>
        public static Idioma DoSistema(string nome) => nome == "Portuguese" ? Idioma.Pt : nome == "Spanish" ? Idioma.Es : Idioma.En;

        /// <summary>-lang (dev) vence a escolha das Configuracoes, que vence o aparelho.</summary>
        public static Idioma Escolher(string flag, string preferencia, string sistema) =>
            Ler(flag, out Idioma i) || Ler(preferencia, out i) ? i : DoSistema(sistema);

        static string[] L(string pt, string en, string es) => new[] { pt, en, es };

        const string Nb = " ";   // espaco que nao quebra linha ("1,23 mi" numa linha so)

        static readonly Dictionary<string, string[]> Tabela = new Dictionary<string, string[]>
        {
            // ---------- idioma (Configuracoes) ----------
            { "idioma.nome", L("Português", "English", "Español") },   // cada um no proprio idioma (Em(i, ...))

            // ---------- numeros (Textos.Ouro e Textos.N) ----------
            { "num.dec", L(",", ".", ",") },
            { "num.mil", L(".", ",", ".") },   // separador de milhar (Textos.Qtd)
            { "num.mi", L(Nb + "mi", "M", Nb + "M") },
            { "num.bi", L(Nb + "bi", "B", Nb + "B") },
            { "num.tri", L(Nb + "tri", "T", Nb + "T") },
            { "num.qua", L(Nb + "qua", "Qa", Nb + "Qa") },
            { "num.qui", L(Nb + "qui", "Qi", Nb + "Qi") },

            // ---------- artigos pelo genero do nome (Textos.Genero): m, f, mp, fp; o en nao usa ----------
            { "art.o.m", L("o", "the", "el") },
            { "art.o.f", L("a", "the", "la") },
            { "art.o.mp", L("os", "the", "los") },
            { "art.o.fp", L("as", "the", "las") },
            { "art.de.m", L("do", "the", "del") },
            { "art.de.f", L("da", "the", "de la") },
            { "art.de.mp", L("dos", "the", "de los") },
            { "art.de.fp", L("das", "the", "de las") },

            // ---------- itens (ordem do enum Item; Textos.ItemId) ----------
            { "item.minerio", L("minério|minérios", "ore|ore", "mineral|minerales") },
            { "item.lingote", L("lingote|lingotes", "ingot|ingots", "lingote|lingotes") },
            { "item.espada", L("espada|espadas", "sword|swords", "espada|espadas") },
            { "item.escudo", L("escudo|escudos", "shield|shields", "escudo|escudos") },
            { "item.ferramenta", L("ferramenta|ferramentas", "tool|tools", "herramienta|herramientas") },
            { "item.joia", L("joia|joias", "jewel|jewels", "joya|joyas") },

            // ---------- estacoes (rotulo no mundo; Textos.EstacaoId = Station.Index) ----------
            { "est.deposito", L("Depósito", "Deposit", "Depósito") },
            { "est.fornalha", L("Fornalha", "Furnace", "Horno") },
            { "est.fornalha2", L("Fornalha 2", "Furnace 2", "Horno 2") },
            { "est.bigorna", L("Bigorna", "Anvil", "Yunque") },
            { "est.bigorna2", L("Bigorna 2", "Anvil 2", "Yunque 2") },
            { "est.escudos", L("Escudos", "Shields", "Escudos") },
            { "est.ferramentas", L("Ferramentas", "Tools", "Herramientas") },
            { "est.balcao", L("Balcão", "Counter", "Mostrador") },
            { "est.joalheria", L("Joalheria", "Jewelry", "Joyería") },
            { "est.loja_joias", L("Loja de joias", "Jewel Shop", "Tienda de joyas") },

            // ---------- melhorias: nome (<= 16, ContentValidator.NameMax) e descricao; .curto = o "Precisa: X" do cartao travado ----------
            { "up.fole.nome", L("Fole", "Bellows", "Fuelle") },
            { "up.fole.desc", L("Fornalha mais rápida", "Faster furnace", "Horno más rápido") },
            { "up.bigorna2.nome", L("Bigorna 2", "Anvil 2", "Yunque 2") },
            { "up.bigorna2.desc", L("Mais espadas por minuto", "More swords per minute", "Más espadas por minuto") },
            { "up.ajudante1.nome", L("Ajudante", "Helper", "Ayudante") },
            { "up.ajudante1.desc", L("Leva minério à fornalha", "Carries ore to the furnace", "Lleva mineral al horno") },
            { "up.escudos.nome", L("Escudos", "Shields", "Escudos") },
            { "up.escudos.desc", L("Nova linha: 2 lingotes, paga 25", "New line: 2 ingots, pays 25", "Nueva línea: 2 lingotes, paga 25") },
            { "up.mochila.nome", L("Mochila", "Backpack", "Mochila") },
            { "up.mochila.desc", L("Carrega 6 em vez de 3", "Carry 6 instead of 3", "Lleva 6 en vez de 3") },
            { "up.ajudante2.nome", L("Ajudante 2", "Helper 2", "Ayudante 2") },
            { "up.ajudante2.desc", L("Leva lingotes às bancadas", "Carries ingots to the workbenches", "Lleva lingotes a las mesas de trabajo") },
            { "up.esteira.nome", L("Esteira", "Conveyor", "Cinta") },
            { "up.esteira.desc", L("Fornalha → bigorna sem andar", "Furnace → anvil, no walking", "Horno → yunque sin caminar") },
            { "up.fole_duplo.nome", L("Fole duplo", "Double Bellows", "Fuelle doble") },
            { "up.fole_duplo.desc", L("Fornalha ainda mais rápida", "Even faster furnace", "Horno aún más rápido") },
            { "up.botas.nome", L("Botas", "Boots", "Botas") },
            { "up.botas.desc", L("Você anda mais rápido", "You walk faster", "Caminas más rápido") },
            { "up.ferramentas.nome", L("Ferramentas", "Tools", "Herramientas") },
            { "up.ferramentas.desc", L("Nova linha: 1 lingote, paga 16", "New line: 1 ingot, pays 16", "Nueva línea: 1 lingote, paga 16") },
            { "up.vitrine.nome", L("Vitrine", "Display Case", "Vitrina") },
            { "up.vitrine.desc", L("Mais estoque e mais clientes", "More stock, more customers", "Más stock y más clientes") },
            { "up.fornalha2.nome", L("Fornalha 2", "Furnace 2", "Horno 2") },
            { "up.fornalha2.desc", L("Dobra os lingotes", "Doubles the ingots", "Duplica los lingotes") },
            { "up.ajudante3.nome", L("Ajudante 3", "Helper 3", "Ayudante 3") },
            { "up.ajudante3.desc", L("Leva produtos ao balcão", "Carries goods to the counter", "Lleva productos al mostrador") },
            { "up.ajudantes_ageis.nome", L("Ajudantes ágeis", "Swift Helpers", "Ayudantes ágiles") },
            { "up.ajudantes_ageis.desc", L("Mais rápidos e carregam 4", "Faster, and carry 4", "Más rápidos y llevan 4") },
            { "up.martelo_veloz.nome", L("Martelo veloz", "Swift Hammer", "Martillo veloz") },
            { "up.martelo_veloz.desc", L("Bancadas mais rápidas", "Faster crafting", "Fabricación más rápida") },
            { "up.corredor.nome", L("Corredor", "Passage", "Pasaje") },
            { "up.corredor.desc", L("Abre a rua lateral", "Opens the side street", "Abre la calle lateral") },
            { "up.joalheria.nome", L("Joalheria", "Jewelry", "Joyería") },
            { "up.joalheria.desc", L("Nova linha: 2 lingotes, paga 60", "New line: 2 ingots, pays 60", "Nueva línea: 2 lingotes, paga 60") },
            { "up.joalheiro1.nome", L("Joalheiro", "Jeweler", "Joyero") },
            { "up.joalheiro1.desc", L("Ajudante leva lingotes à joalheria e joias à loja", "A helper takes ingots to the jewelry and jewels to the shop",
                "Un ayudante lleva lingotes a la joyería y joyas a la tienda") },
            { "up.lupa.nome", L("Lupa", "Loupe", "Lupa") },
            { "up.lupa.desc", L("Joalheria mais rápida", "Faster jewelry", "Joyería más rápida") },
            { "up.vitrine_joias.nome", L("Vitrine de joias", "Jewel Display", "Vitrina de joyas") },
            { "up.vitrine_joias.desc", L("Nobres pagam 80 e a fila cresce", "Nobles pay 80 and the line grows", "Los nobles pagan 80 y la fila crece") },
            { "up.fachada_nobre.nome", L("Fachada nobre", "Noble Facade", "Fachada noble") },
            { "up.fachada_nobre.desc", L("Entrada dourada e bandeirolas", "Golden entrance and bunting", "Entrada dorada y banderines") },
            { "up.piso_oficina.nome", L("Piso de oficina", "Workshop Floor", "Piso de taller") },
            { "up.piso_oficina.desc", L("Ladrilhos para valorizar a oficina", "Tiles to dress up the workshop", "Baldosas para lucir el taller") },
            { "up.joalheria_real.nome", L("Joalheria real", "Royal Jewelry", "Joyería real") },
            { "up.joalheria_real.desc", L("Tapetes e adornos para a joalheria", "Rugs and ornaments for the jewelry", "Alfombras y adornos para la joyería") },
            { "up.mineiro.nome", L("Mineiro", "Miner", "Minero") },
            { "up.mineiro.desc", L("Mais um ajudante leva minério às fornalhas", "One more helper carries ore to the furnaces",
                "Otro ayudante lleva mineral a los hornos") },
            { "up.joalheiro2.nome", L("Joalheiro 2", "Jeweler 2", "Joyero 2") },
            { "up.joalheiro2.desc", L("Mais um ajudante na joalheria", "One more helper at the jewelry", "Otro ayudante en la joyería") },
            { "up.balcao5.nome", L("Balcão 5 vagas", "Counter 5", "Mostrador 5") },
            { "up.balcao5.curto", L("Balcão 5", "Counter 5", "5 puestos") },
            { "up.balcao5.desc", L("Atende 5 clientes ao mesmo tempo", "Serves 5 customers at once", "Atiende a 5 clientes a la vez") },
            { "up.balcao6.nome", L("Balcão 6 vagas", "Counter 6", "Mostrador 6") },
            { "up.balcao6.curto", L("Balcão 6", "Counter 6", "6 puestos") },
            { "up.balcao6.desc", L("Atende 6 clientes ao mesmo tempo", "Serves 6 customers at once", "Atiende a 6 clientes a la vez") },
            { "up.balcao7.nome", L("Balcão 7 vagas", "Counter 7", "Mostrador 7") },
            { "up.balcao7.curto", L("Balcão 7", "Counter 7", "7 puestos") },
            { "up.balcao7.desc", L("Atende 7 clientes ao mesmo tempo", "Serves 7 customers at once", "Atiende a 7 clientes a la vez") },
            { "up.balcao8.nome", L("Balcão 8 vagas", "Counter 8", "Mostrador 8") },
            { "up.balcao8.curto", L("Balcão 8", "Counter 8", "8 puestos") },
            { "up.balcao8.desc", L("Atende 8 clientes ao mesmo tempo", "Serves 8 customers at once", "Atiende a 8 clientes a la vez") },

            // ---------- dica da HUD (1 linha, <= Textos.DicaMax = 26 em todo idioma; LocTests) ----------
            { "dica.minerio", L("Pegue minério no depósito", "Grab ore at the deposit", "Toma mineral del depósito") },
            { "dica.minerio_fornalha", L("Leve o minério à fornalha", "Bring ore to the furnace", "Lleva el mineral al horno") },
            { "dica.pegue_lingotes", L("Pegue lingotes na fornalha", "Grab ingots at the furnace", "Toma lingotes del horno") },
            { "dica.lingote_bigorna", L("Leve os lingotes à bigorna", "Bring ingots to the anvil", "Lleva lingotes al yunque") },
            { "dica.balcao.minerio", L("Leve os minérios ao balcão", "Put ore on the counter", "Lleva mineral al mostrador") },
            { "dica.balcao.lingote", L("Leve os lingotes ao balcão", "Put ingots on the counter", "Lingotes al mostrador") },
            { "dica.balcao.espada", L("Leve as espadas ao balcão", "Put swords on the counter", "Lleva espadas al mostrador") },
            { "dica.balcao.escudo", L("Leve os escudos ao balcão", "Put shields on the counter", "Lleva escudos al mostrador") },
            { "dica.balcao.ferramenta", L("Leve ferramentas ao balcão", "Put tools on the counter", "Herramientas al mostrador") },
            { "dica.balcao.joia", L("Leve as joias à loja", "Take jewels to the shop", "Lleva joyas a la tienda") },
            { "dica.pegue.minerio", L("Pegue os minérios prontos", "Grab the ore", "Toma el mineral") },
            { "dica.pegue.lingote", L("Pegue os lingotes prontos", "Grab the finished ingots", "Toma los lingotes listos") },
            { "dica.pegue.espada", L("Pegue as espadas prontas", "Grab the finished swords", "Toma las espadas listas") },
            { "dica.pegue.escudo", L("Pegue os escudos prontos", "Grab the finished shields", "Toma los escudos listos") },
            { "dica.pegue.ferramenta", L("Pegue as ferramentas", "Grab the finished tools", "Toma las herramientas") },
            { "dica.pegue.joia", L("Pegue as joias prontas", "Grab the finished jewels", "Toma las joyas listas") },
            { "dica.cliente", L("Cliente quer {0}!", "Customer wants {1}!", "¡Piden {1}!") },   // {0} = item no singular, {1} = no plural
            { "dica.bau", L("Abra o baú: {0}", "Open the chest: {0}", "Abre el baúl: {0}") },   // {0} = rotulo do bau
            // placa no chao: {0} = nome, {1} = "de + artigo" (art.de.*); a 1a que cabe em 26 vence (Textos.Cabe)
            { "dica.placa", L("Pise na placa {1} {0}", "Step on the {0} pad", "Pisa la placa {1} {0}") },
            { "dica.placa.curta", L("Pise na placa: {0}", "Step on: {0}", "Pisa la placa: {0}") },
            { "dica.placa.min", L("Placa: {0}", "Pad: {0}", "Placa: {0}") },
            // melhoria do menu: {0} = nome, {1} = artigo (art.o.*)
            { "dica.menu", L("Compre {1} {0} em Melhorias", "Buy {0} in Upgrades", "Compra {1} {0} en Mejoras") },
            { "dica.menu.curta", L("Compre {1} {0}", "Buy {0}", "Compra {1} {0}") },
            { "dica.menu.min", L("Compre: {0}", "Buy {0}", "Compra: {0}") },

            // ---------- bau de marco (rotulo no mundo e na dica) ----------
            { "bau.item", L("{0} {1}!", "{0} {1}!", "¡{0} {1}!") },   // {0} = quantas, {1} = item no plural
            { "bau.vendas", L("{0} venda!|{0} vendas!", "{0} sale!|{0} sales!", "¡{0} venta!|¡{0} ventas!") },

            // ---------- cartao de melhoria (A-UI-18): beneficio (<= 28, 2 linhas), antes -> depois (<= 16), falta, travado (<= 18) ----------
            { "card.fornalha", L("Fornalha {0} mais rápida", "Furnace {0} faster", "Horno {0} más rápido") },
            { "card.bancadas", L("Bancadas {0} mais rápidas", "Crafting {0} faster", "Fabricación {0} más rápida") },
            { "card.joalheria", L("Joalheria {0} mais rápida", "Jewelry {0} faster", "Joyería {0} más rápida") },
            { "card.carga", L("Carga {0}", "Load {0}", "Carga {0}") },
            { "card.velocidade", L("Velocidade {0}", "Speed {0}", "Velocidad {0}") },
            { "card.clientes", L("Clientes {0}", "Customers {0}", "Clientes {0}") },
            { "card.joia", L("Joia vale {0}", "Jewel worth {0}", "Joya vale {0}") },
            { "card.vagas", L("Vagas {0}", "Slots {0}", "Puestos {0}") },
            { "card.ad.s", L("{0} → {1} s", "{0} → {1} s", "{0} → {1} s") },
            { "card.ad.ms", L("{0} → {1} m/s", "{0} → {1} m/s", "{0} → {1} m/s") },
            { "card.ad.n", L("{0} → {1}", "{0} → {1}", "{0} → {1}") },
            { "card.ad.carga", L("carga {0} → {1}", "load {0} → {1}", "carga {0} → {1}") },
            { "card.ad.estoque", L("estoque {0} → {1}", "stock {0} → {1}", "stock {0} → {1}") },
            { "card.ad.fila", L("fila {0} → {1}", "line {0} → {1}", "fila {0} → {1}") },
            { "card.ad.vagas", L("{0} → {1} vagas", "{0} → {1} slots", "{0} → {1} puestos") },
            { "card.falta", L("Falta {0}|Faltam {0}", "Need {0}", "Falta {0}|Faltan {0}") },
            { "card.precisa", L("Precisa: {0}", "Needs {0}", "Requiere {0}") },

            // ---------- HUD, avisos e encomenda ----------
            { "menu.melhorias", L("Melhorias", "Upgrades", "Mejoras") },
            { "hud.vip", L("VIP", "VIP", "VIP") },
            { "order.venda", L("Venda {0} {1}", "Sell {0} {1}", "Vende {0} {1}") },   // {0} = quantas, {1} = item no plural certo
            { "aviso.vip", L("Cliente VIP!", "VIP customer!", "¡Cliente VIP!") },
            { "aviso.vip_vem", L("VIP a caminho!", "VIP on the way!", "¡VIP en camino!") },
            { "aviso.entregue", L("Encomenda entregue! +{0}", "Order delivered! +{0}", "¡Pedido entregado! +{0}") },   // {0} no FIM (o -shotorder procura o comeco)
            { "aviso.compra", L("{0}!", "{0}!", "¡{0}!") },
            { "aviso.anuncio_off", L("Anúncio indisponível", "Ad unavailable", "Anuncio no disponible") },
            { "fx.velocidade", L("Velocidade {0}×!", "Speed {0}×!", "¡Velocidad {0}×!") },

            { "aviso.meta_pronta", L("Meta concluída!", "Goal complete!", "¡Meta cumplida!") },
            { "aviso.meta", L("Meta resgatada! +{0}", "Goal claimed! +{0}", "¡Meta reclamada! +{0}") },
            { "aviso.meta_ok", L("Meta resgatada!", "Goal claimed!", "¡Meta reclamada!") },

            // ---------- metas (D-CORE-02): botao, painel, login e a frase de cada meta (Textos.Meta, Textos.Premio; <= 32 / 44) ----------
            { "menu.metas", L("Metas", "Goals", "Metas") },
            { "metas.titulo", L("Metas", "Goals", "Metas") },
            { "metas.diarias", L("Diárias", "Daily", "Diarias") },
            { "metas.semanais", L("Semanais", "Weekly", "Semanales") },
            { "metas.conquistas", L("Conquistas", "Achievements", "Logros") },
            { "metas.resgatar", L("RESGATAR", "CLAIM", "RECLAMAR") },
            { "metas.feito", L("FEITO", "DONE", "HECHO") },
            { "metas.vazia", L("Nova meta amanhã", "New goal tomorrow", "Nueva meta mañana") },
            { "metas.login.pronto", L("Presente diário: toque no de hoje!", "Daily gift: tap today's!", "Regalo diario: ¡toca el de hoy!") },
            { "metas.login.amanha", L("Presente diário: volte amanhã", "Daily gift: come back tomorrow", "Regalo diario: vuelve mañana") },
            { "metas.login.dia", L("Dia {0}", "Day {0}", "Día {0}") },
            { "meta.produto", L("produto|produtos", "product|products", "producto|productos") },
            { "meta.vende", L("Venda {0} {1}", "Sell {0} {1}", "Vende {0} {1}") },   // {0} = quantas, {1} = item ou "produtos"
            { "meta.linhas", L("Venda os {0} produtos", "Sell all {0} products", "Vende los {0} productos") },
            { "meta.fabrica", L("Fabrique {0} {1}", "Craft {0} {1}", "Fabrica {0} {1}") },
            { "meta.compra_um", L("Compre: {0}", "Buy: {0}", "Compra: {0}") },
            { "meta.compra", L("Compre {0} melhoria|Compre {0} melhorias", "Buy {0} upgrade|Buy {0} upgrades", "Compra {0} mejora|Compra {0} mejoras") },
            { "meta.ganha", L("Ganhe {0} em vendas", "Earn {0} from sales", "Gana {0} en ventas") },
            { "meta.gasta", L("Gaste {0} em melhorias", "Spend {0} on upgrades", "Gasta {0} en mejoras") },
            { "meta.diarias", L("Conclua {0} diária|Conclua {0} diárias", "Complete {0} daily goal|Complete {0} daily goals",
                "Completa {0} meta diaria|Completa {0} metas diarias") },
            { "meta.semanais", L("Conclua {0} semanal|Conclua {0} semanais", "Complete {0} weekly goal|Complete {0} weekly goals",
                "Completa {0} meta semanal|Completa {0} metas semanales") },
            { "meta.order_done", L("Entregue {0} encomenda|Entregue {0} encomendas", "Deliver {0} order|Deliver {0} orders", "Entrega {0} pedido|Entrega {0} pedidos") },
            { "meta.offline_claim", L("Abra o cofre {0} vez|Abra o cofre {0} vezes", "Open the vault {0} time|Open the vault {0} times",
                "Abre el cofre {0} vez|Abre el cofre {0} veces") },
            { "meta.vip_served", L("Atenda {0} VIP|Atenda {0} VIPs", "Serve {0} VIP|Serve {0} VIPs", "Atiende a {0} VIP|Atiende a {0} VIP") },
            { "meta.worker_hired", L("Contrate {0} ajudante|Contrate {0} ajudantes", "Hire {0} helper|Hire {0} helpers", "Contrata {0} ayudante|Contrata {0} ayudantes") },
            { "meta.chest_opened", L("Abra {0} baú|Abra {0} baús", "Open {0} chest|Open {0} chests", "Abre {0} baúl|Abre {0} baúles") },
            { "meta.login_claim", L("Pegue o presente {0} dia|Pegue o presente {0} dias", "Claim the daily gift {0} time|Claim the daily gift {0} times",
                "Recoge el regalo {0} día|Recoge el regalo {0} días") },
            { "meta.premio.ouro", L("+{0} de ouro", "+{0} gold", "+{0} de oro") },
            { "meta.premio.xp", L("+{0} XP", "+{0} XP", "+{0} XP") },
            { "meta.premio.renome", L("+{0} Renome", "+{0} Renown", "+{0} Renombre") },
            { "meta.premio.insignia", L("Insígnia: {0}", "Badge: {0}", "Insignia: {0}") },
            { "ins.mao_de_ouro", L("Mão de Ouro", "Golden Hand", "Mano de Oro") },
            { "ins.livro_caixa", L("Livro-Caixa Cheio", "Full Ledger", "Libro Mayor Lleno") },
            { "ins.oficina_nobre", L("Oficina Nobre", "Noble Workshop", "Taller Noble") },

            // ---------- mundo ----------
            { "mundo.rua", L("Rua\nlateral", "Side\nstreet", "Calle\nlateral") },
            { "mundo.joalheria_fechada", L("Joalheria — fechada", "Jewelry — closed", "Joyería — cerrada") },
            { "mundo.max", L("MAX", "MAX", "MÁX") },

            // ---------- paineis ----------
            { "ui.ok", L("OK", "OK", "OK") },
            { "cofre.titulo", L("Bem-vindo de volta!", "Welcome back!", "¡Hola de nuevo!") },
            { "cofre.corpo", L("Seu cofre guardou isso pra você", "Your vault saved this for you", "Tu cofre guardó esto para ti") },
            { "cofre.pegar", L("PEGAR", "COLLECT", "RECOGER") },

            // ---------- configuracoes ----------
            { "cfg.titulo", L("Configurações", "Settings", "Ajustes") },
            { "cfg.musica", L("Música", "Music", "Música") },
            { "cfg.efeitos", L("Efeitos", "Effects", "Efectos") },
            { "cfg.vibracao", L("Vibração", "Vibration", "Vibración") },
            { "cfg.idioma", L("Idioma", "Language", "Idioma") },
            { "cfg.idioma.reabrir", L("Vale ao abrir o jogo de novo", "Applies when you reopen the game", "Se aplica al volver a abrir el juego") },
            { "cfg.ligado", L("LIGADO", "ON", "ACTIVADO") },
            { "cfg.desligado", L("DESLIGADO", "OFF", "DESACTIVADO") },
            { "cfg.versao", L("Forge Street v{0}", "Forge Street v{0}", "Forge Street v{0}") },
            { "cfg.privacidade", L("PRIVACIDADE", "PRIVACY", "PRIVACIDAD") },
            { "cfg.fechar", L("FECHAR", "CLOSE", "CERRAR") },

            // ---------- privacidade (E-PLAT-01). O hash do pt-BR trava a Privacidade.Versao; en/es podem mudar sem pedir de novo ----------
            { "priv.idade.titulo", L("Qual é a sua idade?", "How old are you?", "¿Qué edad tienes?") },
            { "priv.idade.corpo", L("Serve só para seguir as regras de anúncio de cada idade. Não pedimos nome nem data de nascimento.",
                "It's only used to follow the ad rules for each age. We don't ask for your name or date of birth.",
                "Solo sirve para seguir las reglas de anuncios de cada edad. No pedimos tu nombre ni tu fecha de nacimiento.") },
            { "priv.faixa.1", L("Menos de 13", "Under 13", "Menos de 13") },
            { "priv.faixa.2", L("13 a 17", "13 to 17", "De 13 a 17") },
            { "priv.faixa.3", L("18 ou mais", "18 or older", "18 o más") },
            { "priv.cons.titulo", L("Anúncios personalizados?", "Personalized ads?", "¿Anuncios personalizados?") },
            { "priv.cons.corpo", L("Os anúncios do jogo são opcionais e dão bônus. Se você aceitar, as redes de anúncio podem usar o ID de "
                    + "publicidade do aparelho para escolher anúncios do seu interesse. Se recusar, os anúncios continuam, só que genéricos.",
                "The game's ads are optional and give bonuses. If you accept, ad networks may use your device's advertising ID to "
                    + "choose ads that match your interests. If you decline, you'll still see ads, just generic ones.",
                "Los anuncios del juego son opcionales y dan recompensas. Si aceptas, las redes de anuncios pueden usar el ID de publicidad "
                    + "del dispositivo para elegir anuncios de tu interés. Si rechazas, seguirás viendo anuncios, pero genéricos.") },
            { "priv.aceitar", L("Aceitar", "Accept", "Aceptar") },
            { "priv.recusar", L("Recusar", "Decline", "Rechazar") },
            { "priv.rodape", L("Dá para mudar depois em Configurações > Privacidade.", "You can change this later in Settings > Privacy.",
                "Puedes cambiarlo luego en Ajustes > Privacidad.") },
            { "priv.revisao", L("Resposta atual: {0}. A mudança vale por completo quando você abrir o jogo de novo.",
                "Current answer: {0}. The change fully applies when you reopen the game.",
                "Respuesta actual: {0}. El cambio se aplica por completo al volver a abrir el juego.") },
            { "priv.atual.sim", L("{0}, anúncios personalizados", "{0}, personalized ads", "{0}, anuncios personalizados") },   // {0} = faixa
            { "priv.atual.nao", L("{0}, anúncios não personalizados", "{0}, non-personalized ads", "{0}, anuncios no personalizados") },
            { "priv.voltar", L("VOLTAR", "BACK", "VOLVER") },
            { "priv.cancelar", L("CANCELAR", "CANCEL", "CANCELAR") },

            // ---------- anuncio simulado e -adtest (build de desenvolvimento) ----------
            { "ads.vip", L("VIP", "VIP", "VIP") },
            { "ads.velocidade", L("Velocidade", "Speed", "Velocidad") },
            { "adtest.titulo", L("Anúncio de teste", "Test ad", "Anuncio de prueba") },
            { "adtest.premio", L("Recompensa: {0} x1", "Reward: {0} x1", "Recompensa: {0} x1") },
            { "adtest.rodape", L("Simulado: nenhum anúncio real foi pedido nem exibido", "Simulated: no real ad was requested or shown",
                "Simulado: no se pidió ni se mostró ningún anuncio real") },
            { "adtest.recebida", L("Recompensa recebida: {0}", "Reward received: {0}", "Recompensa recibida: {0}") },
            { "adtest.sem", L("Sem recompensa: {0}", "No reward: {0}", "Sin recompensa: {0}") },
        };
    }
}
