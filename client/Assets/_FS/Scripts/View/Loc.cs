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
            { "item.espada_longa", L("espada longa|espadas longas", "longsword|longswords", "espada larga|espadas largas") },   // B-CORE-07
            // C-CORE-01: Armorer's Row (CONTEUDO_V1 §3)
            { "item.elmo", L("elmo|elmos", "helm|helms", "yelmo|yelmos") },
            { "item.maca", L("maça|maças", "mace|maces", "maza|mazas") },
            { "item.grevas", L("grevas|grevas", "greaves|greaves", "grebas|grebas") },
            { "item.lanca", L("lança|lanças", "spear|spears", "lanza|lanzas") },
            { "item.manoplas", L("manoplas|manoplas", "gauntlets|gauntlets", "guanteletes|guanteletes") },
            { "item.machado_guerra", L("machado bélico|machados bélicos", "war axe|war axes", "hacha de guerra|hachas de guerra") },
            { "item.peitoral", L("peitoral|peitorais", "breastplate|breastplates", "peto|petos") },
            { "item.placa", L("placa|placas", "plate|plates", "placa|placas") },
            { "item.rebite", L("rebite|rebites", "rivet|rivets", "remache|remaches") },
            { "item.couro", L("couro|couros", "leather|leather", "cuero|cueros") },

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
            { "est.bigorna_longa", L("Bigorna longa", "Long Anvil", "Yunque largo") },   // B-CORE-07
            // C-CORE-01: estacoes da Armorer's Row (CONTEUDO_V1 §4; deposito, fornalha, fornalha2 e balcao sao as de cima)
            { "est.prensa", L("Prensa", "Press", "Prensa") },
            { "est.prensa2", L("Prensa 2", "Press 2", "Prensa 2") },
            { "est.rebitadeira", L("Rebitadeira", "Riveter", "Remachadora") },
            { "est.rebitadeira2", L("Rebitadeira 2", "Riveter 2", "Remachadora 2") },
            { "est.montagem_elmo", L("Montagem elmo", "Helm Bench", "Banco yelmos") },
            { "est.montagem_grevas", L("Montagem grevas", "Greave Bench", "Banco grebas") },
            { "est.montagem_manoplas", L("Mont. manoplas", "Gauntlet Bench", "Banco guantes") },
            { "est.montagem_peitoral", L("Mont. peitoral", "Plate Bench", "Banco petos") },
            { "est.bigorna_pesada", L("Bigorna pesada", "Heavy Anvil", "Yunque pesado") },
            { "est.forja_hastes", L("Forja de hastes", "Haft Forge", "Forja de astas") },
            { "est.forja_machados", L("Forja machados", "Axe Forge", "Forja hachas") },
            { "est.carroca_couro", L("Carroça couro", "Leather Cart", "Carro de cuero") },
            // C-CORE-01/02: distritos (CONTEUDO_V1 §2) e o mapa minimo (C-UI-20 completo depois)
            { "distrito.old_forge", L("Old Forge", "Old Forge", "Vieja Forja") },
            { "distrito.armorers_row", L("Rua dos Armeiros", "Armorer's Row", "Calle Armeros") },
            { "mapa.botao", L("MAPA", "MAP", "MAPA") },
            { "mapa.titulo", L("Distritos", "Districts", "Distritos") },
            { "mapa.aqui", L("{0}: você está aqui", "{0}: you are here", "{0}: estás aquí") },
            { "mapa.charter_pronto", L("{0}: Charter pronto!", "{0}: Charter ready!", "{0}: ¡Carta lista!") },
            { "mapa.falta.producao", L("{0}: falta a produção", "{0}: finish production", "{0}: falta producción") },
            { "mapa.falta.renome", L("{0}: falta Renome", "{0}: need Renown", "{0}: falta Renombre") },
            { "mapa.falta.masterwork", L("{0}: falta obra-prima", "{0}: need a Masterwork", "{0}: falta obra maestra") },
            { "mapa.aberto", L("{0}: aberto", "{0}: open", "{0}: abierto") },
            { "mapa.abrir", L("{0}: abrir por {1}", "{0}: open for {1}", "{0}: abrir por {1}") },
            { "mapa.fechado", L("{0}: fechado", "{0}: locked", "{0}: cerrado") },
            { "mapa.charter", L("Charter!", "Charter!", "¡Carta!") },
            { "mapa.btn_abrir", L("Abrir", "Open", "Abrir") },
            { "mapa.ir", L("Viajar", "Travel", "Viajar") },

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
            // B-CORE-03: niveis de estacao ("Familia II/III"); .curto = o "Precisa: X" do nivel III (o II fica oculto ate a revelacao)
            { "up.bigornas_nivel2.nome", L("Bigornas II", "Anvils II", "Yunques II") },
            { "up.bigornas_nivel2.curto", L("Nível II", "Level II", "Nivel II") },
            { "up.bigornas_nivel2.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.bigornas_nivel3.nome", L("Bigornas III", "Anvils III", "Yunques III") },
            { "up.bigornas_nivel3.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.escudos_nivel2.nome", L("Escudos II", "Shields II", "Escudos II") },
            { "up.escudos_nivel2.curto", L("Nível II", "Level II", "Nivel II") },
            { "up.escudos_nivel2.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.escudos_nivel3.nome", L("Escudos III", "Shields III", "Escudos III") },
            { "up.escudos_nivel3.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.ferramentas_nivel2.nome", L("Ferramentas II", "Tools II", "Herramientas II") },
            { "up.ferramentas_nivel2.curto", L("Nível II", "Level II", "Nivel II") },
            { "up.ferramentas_nivel2.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.ferramentas_nivel3.nome", L("Ferramentas III", "Tools III", "Herramientas III") },
            { "up.ferramentas_nivel3.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.joalheria_nivel2.nome", L("Joalheria II", "Jewelry II", "Joyería II") },
            { "up.joalheria_nivel2.curto", L("Nível II", "Level II", "Nivel II") },
            { "up.joalheria_nivel2.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.joalheria_nivel3.nome", L("Joalheria III", "Jewelry III", "Joyería III") },
            { "up.joalheria_nivel3.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            // B-CORE-07: niveis IV-V (com o projeto de mestre; sem ele o cartao fica oculto); .curto = o "Precisa: X" do nivel seguinte
            { "up.bigornas_nivel3.curto", L("Nível III", "Level III", "Nivel III") },
            { "up.bigornas_nivel4.nome", L("Bigornas IV", "Anvils IV", "Yunques IV") },
            { "up.bigornas_nivel4.curto", L("Nível IV", "Level IV", "Nivel IV") },
            { "up.bigornas_nivel4.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.bigornas_nivel5.nome", L("Bigornas V", "Anvils V", "Yunques V") },
            { "up.bigornas_nivel5.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.escudos_nivel3.curto", L("Nível III", "Level III", "Nivel III") },
            { "up.escudos_nivel4.nome", L("Escudos IV", "Shields IV", "Escudos IV") },
            { "up.escudos_nivel4.curto", L("Nível IV", "Level IV", "Nivel IV") },
            { "up.escudos_nivel4.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.escudos_nivel5.nome", L("Escudos V", "Shields V", "Escudos V") },
            { "up.escudos_nivel5.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.ferramentas_nivel3.curto", L("Nível III", "Level III", "Nivel III") },
            { "up.ferramentas_nivel4.nome", L("Ferramentas IV", "Tools IV", "Herramientas IV") },
            { "up.ferramentas_nivel4.curto", L("Nível IV", "Level IV", "Nivel IV") },
            { "up.ferramentas_nivel4.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.ferramentas_nivel5.nome", L("Ferramentas V", "Tools V", "Herramientas V") },
            { "up.ferramentas_nivel5.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.joalheria_nivel3.curto", L("Nível III", "Level III", "Nivel III") },
            { "up.joalheria_nivel4.nome", L("Joalheria IV", "Jewelry IV", "Joyería IV") },
            { "up.joalheria_nivel4.curto", L("Nível IV", "Level IV", "Nivel IV") },
            { "up.joalheria_nivel4.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.joalheria_nivel5.nome", L("Joalheria V", "Jewelry V", "Joyería V") },
            { "up.joalheria_nivel5.desc", L("Mais qualidade e velocidade", "More quality and speed", "Más calidad y velocidad") },
            { "up.bigorna_longa.nome", L("Bigorna longa", "Long Anvil", "Yunque largo") },
            { "up.bigorna_longa.desc", L("Nova linha: 2 lingotes, paga 40", "New line: 2 ingots, pays 40", "Nueva línea: 2 lingotes, paga 40") },
            // C-CORE-01: Armorer's Row (CONTEUDO_V1 §5.2)
            { "up.fole_couro.nome", L("Fole de couro", "Leather Bellows", "Fuelle de cuero") },
            { "up.fole_couro.desc", L("Fornalhas mais rápidas", "Faster furnaces", "Hornos más rápidos") },
            { "up.prensa2.nome", L("Prensa 2", "Press 2", "Prensa 2") },
            { "up.prensa2.desc", L("Mais placas por minuto", "More plates per minute", "Más placas por minuto") },
            { "up.ferraz1.nome", L("Ferraz 1", "Ferraz 1", "Ferraz 1") },
            { "up.ferraz1.desc", L("Leva minério e couro", "Carries ore and leather", "Lleva mineral y cuero") },
            { "up.bigorna_pesada.nome", L("Maças", "Maces", "Mazas") },
            { "up.bigorna_pesada.desc", L("Nova linha: maça, paga 120", "New line: mace, pays 120", "Nueva línea: maza, paga 120") },
            { "up.ferraz2.nome", L("Ferraz 2", "Ferraz 2", "Ferraz 2") },
            { "up.ferraz2.desc", L("Leva lingotes, placas e rebites", "Carries ingots, plates, rivets", "Lleva lingotes, placas y remaches") },
            { "up.trilho_placas.nome", L("Trilho de placas", "Plate Rail", "Riel de placas") },
            { "up.trilho_placas.desc", L("Prensa → elmos sem andar", "Press → helms, no walking", "Prensa → yelmos sin caminar") },
            { "up.carroca_curtidor.nome", L("Carroça curtidor", "Tanner's Cart", "Carro curtidor") },
            { "up.carroca_curtidor.desc", L("Couro e nova linha: grevas", "Leather and a new line: greaves", "Cuero y nueva línea: grebas") },
            { "up.forja_hastes.nome", L("Lanças", "Spears", "Lanzas") },
            { "up.forja_hastes.desc", L("Nova linha: lança, paga 70", "New line: spear, pays 70", "Nueva línea: lanza, paga 70") },
            { "up.fornalha_armeiro2.nome", L("Fornalha 2", "Furnace 2", "Horno 2") },
            { "up.fornalha_armeiro2.desc", L("Dobra os lingotes", "Doubles the ingots", "Duplica los lingotes") },
            { "up.ferraz3.nome", L("Ferraz 3", "Ferraz 3", "Ferraz 3") },
            { "up.ferraz3.desc", L("Leva produtos ao balcão", "Carries goods to the counter", "Lleva productos al mostrador") },
            { "up.montagem_manoplas.nome", L("Manoplas", "Gauntlets", "Guanteletes") },
            { "up.montagem_manoplas.desc", L("Nova linha: manoplas, paga 120", "New line: gauntlets, pays 120", "Nueva línea: guanteletes, paga 120") },
            { "up.forja_machados.nome", L("Machados", "War Axes", "Hachas") },
            { "up.forja_machados.desc", L("Nova linha: machado, paga 110", "New line: war axe, pays 110", "Nueva línea: hacha, paga 110") },
            { "up.fole_couro_duplo.nome", L("Fole duplo", "Twin Bellows", "Fuelle doble") },
            { "up.fole_couro_duplo.desc", L("Fornalhas ainda mais rápidas", "Even faster furnaces", "Hornos aún más rápidos") },
            { "up.ferraz4.nome", L("Ferraz 4", "Ferraz 4", "Ferraz 4") },
            { "up.ferraz4.desc", L("Mais um leva minério e couro", "Another one for ore and leather", "Otro más para mineral y cuero") },
            { "up.gabaritos.nome", L("Gabaritos", "Jigs", "Plantillas") },
            { "up.gabaritos.desc", L("Montagens e forjas mais rápidas", "Faster benches and forges", "Bancos y forjas más rápidos") },
            { "up.montagem_peitoral.nome", L("Peitorais", "Breastplates", "Petos") },
            { "up.montagem_peitoral.desc", L("Nova linha: peitoral, paga 230", "New line: breastplate, pays 230", "Nueva línea: peto, paga 230") },
            { "up.rebitadeira2.nome", L("Rebitadeira 2", "Riveter 2", "Remachadora 2") },
            { "up.rebitadeira2.desc", L("Mais rebites por minuto", "More rivets per minute", "Más remaches por minuto") },
            { "up.estandartes_armeiro.nome", L("Estandartes", "Banners", "Estandartes") },
            { "up.estandartes_armeiro.desc", L("Bandeiras na rua", "Banners on the street", "Banderas en la calle") },
            { "up.rua_calcada.nome", L("Rua calçada", "Paved Street", "Calle empedrada") },
            { "up.rua_calcada.desc", L("Pedras novas na rua", "New street stones", "Piedras nuevas en la calle") },
            { "up.manequins.nome", L("Manequins", "Mannequins", "Maniquíes") },
            { "up.manequins.desc", L("Armaduras em exposição", "Armor on display", "Armaduras en exhibición") },

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
            { "dica.balcao.espada_longa", L("Espadas longas ao balcão", "Put longswords on counter", "Lleva espadas largas") },
            { "dica.pegue.minerio", L("Pegue os minérios prontos", "Grab the ore", "Toma el mineral") },
            { "dica.pegue.lingote", L("Pegue os lingotes prontos", "Grab the finished ingots", "Toma los lingotes listos") },
            { "dica.pegue.espada", L("Pegue as espadas prontas", "Grab the finished swords", "Toma las espadas listas") },
            { "dica.pegue.escudo", L("Pegue os escudos prontos", "Grab the finished shields", "Toma los escudos listos") },
            { "dica.pegue.ferramenta", L("Pegue as ferramentas", "Grab the finished tools", "Toma las herramientas") },
            { "dica.pegue.joia", L("Pegue as joias prontas", "Grab the finished jewels", "Toma las joyas listas") },
            { "dica.pegue.espada_longa", L("Pegue as espadas longas", "Grab the longswords", "Toma las espadas largas") },
            // C-CORE-01: Armorer's Row (insumo pronto tambem: a dica cobre todo item do distrito)
            { "dica.balcao.elmo", L("Leve os elmos ao balcão", "Put helms on the counter", "Yelmos al mostrador") },
            { "dica.balcao.maca", L("Leve as maças ao balcão", "Put maces on the counter", "Mazas al mostrador") },
            { "dica.balcao.grevas", L("Leve as grevas ao balcão", "Put greaves on the counter", "Grebas al mostrador") },
            { "dica.balcao.lanca", L("Leve as lanças ao balcão", "Put spears on the counter", "Lanzas al mostrador") },
            { "dica.balcao.manoplas", L("Manoplas ao balcão", "Gauntlets to the counter", "Guanteletes al mostrador") },
            { "dica.balcao.machado_guerra", L("Machados ao balcão", "War axes to the counter", "Hachas al mostrador") },
            { "dica.balcao.peitoral", L("Peitorais ao balcão", "Breastplates to counter", "Petos al mostrador") },
            { "dica.balcao.placa", L("Leve as placas ao balcão", "Put plates on the counter", "Placas al mostrador") },
            { "dica.balcao.rebite", L("Leve os rebites ao balcão", "Put rivets on the counter", "Remaches al mostrador") },
            { "dica.balcao.couro", L("Leve o couro ao balcão", "Put leather on the counter", "Cuero al mostrador") },
            { "dica.pegue.elmo", L("Pegue os elmos prontos", "Grab the finished helms", "Toma los yelmos listos") },
            { "dica.pegue.maca", L("Pegue as maças prontas", "Grab the finished maces", "Toma las mazas listas") },
            { "dica.pegue.grevas", L("Pegue as grevas prontas", "Grab the finished greaves", "Toma las grebas listas") },
            { "dica.pegue.lanca", L("Pegue as lanças prontas", "Grab the finished spears", "Toma las lanzas listas") },
            { "dica.pegue.manoplas", L("Pegue as manoplas", "Grab the gauntlets", "Toma los guanteletes") },
            { "dica.pegue.machado_guerra", L("Pegue os machados", "Grab the war axes", "Toma las hachas") },
            { "dica.pegue.peitoral", L("Pegue os peitorais", "Grab the breastplates", "Toma los petos") },
            { "dica.pegue.placa", L("Pegue as placas prontas", "Grab the finished plates", "Toma las placas listas") },
            { "dica.pegue.rebite", L("Pegue os rebites prontos", "Grab the finished rivets", "Toma los remaches") },
            { "dica.pegue.couro", L("Pegue o couro", "Grab the leather", "Toma el cuero") },
            { "dica.cliente", L("Cliente quer {0}!", "Customer wants {1}!", "¡Piden {1}!") },   // {0} = item no singular, {1} = no plural
            { "dica.cliente.curta", L("Querem {1}!", "Wants {1}!", "¡Piden {1}!") },   // C-CORE-01: item de nome comprido ("machado bélico")
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
            // B-CORE-03 (GDD §19): nivel de estacao. {0} = pontos por nivel ("0,15"), {1} = velocidade ("5%"); depois: 1 em N sai Fina
            { "card.qualidade", L("Qualidade +{0} · vel. +{1}", "Quality +{0} · speed +{1}", "Calidad +{0} · vel. +{1}") },
            { "card.ad.fina", L("1 em {0} sai Fina", "1 in {0} is Fine", "1 de {0} sale Fina") },

            // ---------- qualidade (B-CORE-03): niveis, barra da estacao (QualityBreakdown ao tocar) e golpe de mestre ----------
            { "qual.0", L("Normal", "Normal", "Normal") },
            { "qual.1", L("Fina", "Fine", "Fina") },
            { "qual.2", L("Superior", "Superior", "Superior") },
            { "qual.3", L("Obra-prima", "Masterwork", "Obra maestra") },
            { "qual.em", L("{0} em {1}", "{0} in {1}", "{0} en {1}") },   // {0} = proximo nivel, {1} = pecas
            { "qual.agora", L("Próxima: {0}!", "Next: {0}!", "¡Próxima: {0}!") },
            { "qual.sempre", L("Sempre {0}", "Always {0}", "Siempre {0}") },
            { "qual.so_normal", L("Só Normal", "Normal only", "Solo Normal") },
            { "qual.golpe", L("Golpe! Próxima +1", "Strike! Next +1", "¡Golpe! Próxima +1") },
            { "qual.titulo", L("{0}: qualidade", "{0}: quality", "{0}: calidad") },   // {0} = estacao
            { "qual.fonte.0", L("Nível da estação", "Station level", "Nivel de la estación") },
            { "qual.fonte.1", L("Oficial", "Smith", "Oficial") },
            { "qual.fonte.2", L("Técnica", "Technique", "Técnica") },
            { "qual.fonte.3", L("Material", "Material", "Material") },
            { "qual.fonte.4", L("Bônus", "Bonus", "Bonificación") },
            { "qual.linha", L("{0}: +{1}", "{0}: +{1}", "{0}: +{1}") },
            { "qual.total", L("Total +{0} · {1}", "Total +{0} · {1}", "Total +{0} · {1}") },   // {1} = a barra ("Fina em 7")
            { "golpe.nome", L("GOLPE", "STRIKE", "GOLPE") },
            { "golpe.quase", L("Quase!", "Almost!", "¡Casi!") },
            { "aviso.golpe", L("Golpe de mestre!", "Master strike!", "¡Golpe maestro!") },
            { "fx.qualidade", L("{0}!", "{0}!", "¡{0}!") },

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
            // E-PLAT-02: avisos das recompensas novas ({0} no fim) e o "Trocar" do cartao da encomenda ({0} = quantas da proxima, com o icone dela)
            { "aviso.cofre2x", L("Cofre dobrado! +{0}", "Vault doubled! +{0}", "¡Cofre duplicado! +{0}") },
            { "aviso.bonus", L("Bônus na encomenda! +{0}", "Order bonus! +{0}", "¡Bono del pedido! +{0}") },
            { "aviso.troca", L("Encomenda trocada!", "Order swapped!", "¡Pedido cambiado!") },
            { "aviso.supply", L("Lingotes chegaram! +{0}", "Ingots delivered! +{0}", "¡Llegaron lingotes! +{0}") },
            { "enc.troca", L("Trocar: {0}", "Swap: {0}", "Cambiar: {0}") },
            // B-CORE-05 Orders 2.0: nome curto do tipo (OrderType), titulo "Tipo: 20 espadas", freguês (FS-3) e a qualidade minima na barra
            { "enc.tipo.0", L("Rua", "Street", "Calle") },
            { "enc.tipo.1", L("Grande", "Bulk", "Grande") },
            { "enc.tipo.2", L("Fina", "Fine", "Fina") },
            { "enc.tipo.3", L("Urgente", "Rush", "Urgente") },
            { "enc.tipo.4", L("Variado", "Mixed", "Variado") },
            { "enc.tipo.5", L("Real", "Royal", "Real") },
            { "enc.tipo.6", L("Projeto", "Plans", "Plano") },
            { "enc.tipo.7", L("Evento", "Event", "Evento") },
            { "enc.titulo", L("{0}: {1}", "{0}: {1}", "{0}: {1}") },
            { "enc.min", L("{0}+", "{0}+", "{0}+") },
            { "cli.cli_reg_orbo", L("Mestre Orbo", "Master Orbo", "Maestro Orbo") },
            { "cli.cli_reg_lirien", L("Lírien", "Lírien", "Lírien") },
            { "cli.cli_reg_grunk", L("Grunk", "Grunk", "Grunk") },
            { "cli.cli_reg_bento", L("Sir Bento", "Sir Bento", "Sir Bento") },
            { "cli.cli_reg_ines", L("Dona Inês", "Lady Inês", "Doña Inés") },
            { "cli.cli_reg_brutus", L("Sgt. Brutus", "Sgt. Brutus", "Sgt. Brutus") },   // C-CORE-05: freguês da Row (lança; "Sgto." passa do cartão em es)
            // E-PLAT-04: avisos da loja (1 linha da faixa, LocTests)
            { "aviso.compra_ok", L("Compra concluída!", "Purchase complete!", "¡Compra completada!") },
            { "aviso.compra_falhou", L("Compra não concluída", "Purchase not completed", "Compra no completada") },
            { "aviso.restaurado", L("Compras restauradas: {0}", "Purchases restored: {0}", "Compras restauradas: {0}") },

            { "aviso.meta_pronta", L("Meta concluída!", "Goal complete!", "¡Meta cumplida!") },
            { "aviso.meta", L("Meta resgatada! +{0}", "Goal claimed! +{0}", "¡Meta reclamada! +{0}") },
            { "aviso.meta_ok", L("Meta resgatada!", "Goal claimed!", "¡Meta reclamada!") },

            // ---------- Renome (B-CORE-06): nome do nivel no selo da HUD (<= 20), cartao, aviso ao subir (<= 30; LocTests) ----------
            { "renome.0", L("Aprendiz", "Apprentice", "Aprendiz") },
            { "renome.1", L("Ferreiro da rua", "Street Smith", "Herrero de calle") },
            { "renome.2", L("Oficina conhecida", "Known Workshop", "Taller conocido") },
            { "renome.3", L("Mestre artesão", "Master Artisan", "Maestro artesano") },
            { "renome.4", L("Fornecedor do reino", "Realm Supplier", "Proveedor real") },
            { "renome.5", L("Armeiro da Coroa", "Crown Armorer", "Armero real") },
            { "renome.6", L("Ourives da corte", "Court Goldsmith", "Orfebre de corte") },
            { "renome.7", L("Fornecedor real", "Royal Purveyor", "Proveedor regio") },
            { "renome.8", L("Mestre arcano", "Arcane Master", "Maestro arcano") },
            { "renome.9", L("Grão-mestre", "Grandmaster", "Gran maestro") },
            { "renome.10", L("Lenda do reino", "Realm Legend", "Leyenda real") },
            // o que o nivel libera (RenomeDefs.Niveis[n].Libera), em frase para o cartao
            { "renome.1.libera", L("2ª vaga de encomenda, encomendas de projeto e o Almoxarife", "2nd order slot, blueprint orders and the Quartermaster",
                "2.º hueco de pedido, pedidos de planos y el Intendente") },
            { "renome.2.libera", L("Encomendas reais, o Mestre de obras e o cliente colecionador", "Royal orders, the Foreman and the collector customer",
                "Pedidos reales, el Capataz y el cliente coleccionista") },
            { "renome.3.libera", L("3ª vaga, encomendas de peça Superior, o Hall na rua e o avental de mestre",
                "3rd slot, Superior piece orders, the street Hall and the master's apron",
                "3.er hueco, pedidos de pieza Superior, el Salón en la calle y el delantal de maestro") },
            { "renome.4.libera", L("O Royal Charter do Old Forge e o uniforme de mestre", "The Old Forge Royal Charter and the master's uniform",
                "La Carta Real de la Vieja Forja y el uniforme de maestro") },
            { "renome.5.libera", L("O Charter da Rua dos Armeiros, a Milícia do norte e o cinto do herói",
                "The Armorer's Row Charter, the North Militia and the hero's belt", "La Carta de la Calle Armeros, la Milicia norteña y el cinturón del héroe") },
            { "renome.6.libera", L("O Charter do Bairro Artesão e o Dote da princesa", "The Artisan Quarter Charter and the Princess Dowry",
                "La Carta del Barrio Artesano y la Dote de la princesa") },
            { "renome.7.libera", L("O Charter do Arsenal Real, o Desfile da coroação e o uniforme real",
                "The Royal Arsenal Charter, the Coronation parade and the royal uniform", "La Carta del Arsenal Real, el desfile de Coronación y el uniforme real") },
            { "renome.8.libera", L("O Charter da Fundição Arcana e a Vanguarda rúnica", "The Arcane Foundry Charter and the Rune Vanguard",
                "La Carta de la Fundición Arcana y la Vanguardia rúnica") },
            { "renome.9.libera", L("A Carta da Grande Forja e o Campeão do reino", "The Grand Forge Charter and the Realm Champion",
                "La Carta de la Gran Forja y el Campeón del reino") },
            { "renome.10.libera", L("A Coroa da Grande Forja, sua estátua na praça e o título de Lenda",
                "The Grand Forge Crown, your statue in the square and the Legend title", "La Corona de la Gran Forja, tu estatua en la plaza y el título de Leyenda") },
            { "renome.titulo", L("Renome {0}", "Renown {0}", "Renombre {0}") },
            { "renome.proximo", L("Próximo: {0}", "Next: {0}", "Siguiente: {0}") },
            { "renome.libera", L("Libera: {0}", "Unlocks: {0}", "Desbloquea: {0}") },
            { "renome.liberou", L("Você já tem: {0}", "You have: {0}", "Ya tienes: {0}") },
            { "renome.max", L("Nível máximo: o reino inteiro te conhece!", "Max level: the whole realm knows you!", "Nivel máximo: ¡todo el reino te conoce!") },
            { "renome.fontes", L("Sobe com encomendas, baús e metas. Nunca diminui.", "Grows with orders, chests and goals. Never goes down.",
                "Sube con pedidos, baúles y metas. Nunca baja.") },
            { "aviso.renome", L("Renome {0}: {1}!", "Renown {0}: {1}!", "¡Renombre {0}: {1}!") },   // {0} = nivel, {1} = nome (renome.N)

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
            { "meta.order_complete", L("Entregue {0} do tipo {1}", "Deliver {0} {1} order|Deliver {0} {1} orders", "Entrega {0} del tipo {1}") },   // B-CORE-05
            { "meta.order_tipos", L("Entregue os {0} tipos", "Deliver all {0} order types", "Entrega los {0} tipos") },
            { "meta.order_premium", L("Entregue {0} Fina/Real/Variado", "Deliver {0} Fine/Royal/Mixed", "Entrega {0} Fina/Real/Variado") },
            { "meta.order_rush_prazo", L("Entregue {0} urgente no prazo|Entregue {0} urgentes no prazo", "Deliver {0} rush order on time|Deliver {0} rush orders on time",
                "Entrega {0} urgente a tiempo|Entrega {0} urgentes a tiempo") },
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
            { "ins.mestre_oficio", L("Mestre de Ofício", "Master of Craft", "Maestro de Oficio") },
            // B-CORE-04: metas de worker ({0} = quantas; {1} = nivel ou papel)
            { "meta.worker_upgrade", L("Suba {0} nível de ajudante|Suba {0} níveis de ajudante", "Do {0} helper level-up|Do {0} helper level-ups",
                "Sube {0} nivel de ayudante|Sube {0} niveles de ayudante") },
            { "meta.worker_nivel", L("Leve {0} ajudante ao nível {1}|Leve {0} ajudantes ao nível {1}", "Get {0} helper to level {1}|Get {0} helpers to level {1}",
                "Lleva {0} ayudante al nivel {1}|Lleva {0} ayudantes al nivel {1}") },
            { "meta.worker_papel", L("Contrate {0} {1}", "Hire {0} {1}", "Contrata {0} {1}") },

            // ---------- workers (B-CORE-04): papel (singular|plural), cartao de contratar/subir (<= 28 / 16 / 18) e aviso (<= 30) ----------
            // nome proprio (Bruna, Tomé...) e' igual nos 3 idiomas (CONTEUDO_V1 §0.9): vem do WorkerDef
            { "wk.papel.0", L("carregador|carregadores", "porter|porters", "cargador|cargadores") },
            { "wk.papel.1", L("oficial|oficiais", "smith|smiths", "oficial|oficiales") },
            { "wk.papel.2", L("balconista|balconistas", "clerk|clerks", "dependiente|dependientes") },
            { "wk.papel.3", L("almoxarife|almoxarifes", "quartermaster|quartermasters", "intendente|intendentes") },
            { "wk.papel.4", L("mestre de obras|mestres de obras", "foreman|foremen", "capataz|capataces") },
            { "wk.papel.5", L("fundidor|fundidores", "smelter|smelters", "fundidor|fundidores") },
            { "wk.papel.6", L("mensageiro|mensageiros", "courier|couriers", "mensajero|mensajeros") },
            { "wk.papel.7", L("gerente|gerentes", "manager|managers", "gerente|gerentes") },
            { "card.wk.passo_carga", L("Velocidade {0} · carga +1", "Speed {0} · load +1", "Velocidad {0} · carga +1") },
            { "card.wk.paciencia", L("Paciência {0}", "Patience {0}", "Paciencia {0}") },
            { "card.wk.saida", L("Pilhas de saída +{0}", "Output piles +{0}", "Pilas de salida +{0}") },
            { "card.wk.estoque", L("Estoque +{0} por produto", "Stock +{0} per product", "Stock +{0} por producto") },
            { "card.ad.nivel", L("Nível {0} → {1}", "Level {0} → {1}", "Nivel {0} → {1}") },
            { "card.precisa_renome", L("Precisa: Renome {0}", "Needs Renown {0}", "Req.: Renombre {0}") },
            { "aviso.contratou", L("{0} na equipe!", "{0} joins the team!", "¡{0} en el equipo!") },
            { "aviso.worker_nivel", L("{0}: nível {1}!", "{0}: level {1}!", "¡{0}: nivel {1}!") },

            // ---------- LiveOps (D-CORE-03/04): eventos, Passe, missoes de evento e cosmeticos (Textos.Missao <= 32, Textos.Premios <= 44) ----------
            { "ev.ev_s1w1_royal_week", L("Semana Real", "Royal Week", "Semana Real") },
            { "ev.ev_s1w2_forge_frenzy", L("Frenesi da Forja", "Forge Frenzy", "Frenesí de la Forja") },
            { "ev.ev_s1w3_armorers_challenge", L("Desafio do Armeiro", "Armorer's Challenge", "Reto del Armero") },
            { "ev.ev_s1w4_hunt_sunblade", L("Caça: Lâmina Solar", "Hunt: King's Sunblade", "Caza: Hoja Solar") },
            { "ev.ev_s1w5_forge_frenzy_b", L("Frenesi Noturno", "Night Frenzy", "Frenesí Nocturno") },
            { "ev.ev_s2w1_blacksmith_festival", L("Festa dos Ferreiros", "Blacksmith Festival", "Fiesta de Herreros") },
            { "ev.ev_s2w2_rare_ore_rush", L("Corrida do Minério", "Rare Ore Rush", "Fiebre del Mineral") },
            { "ev.ev_s2w3_merchant_caravan", L("Caravana Mercante", "Merchant Caravan", "Caravana Mercante") },
            { "ev.ev_s2w4_hunt_ember_crown", L("Caça: Coroa de Brasa", "Hunt: Ember Crown", "Caza: Corona de Brasa") },
            { "ev.ev_s2w5_royal_week_b", L("Semana do Arauto", "Herald's Week", "Semana del Heraldo") },
            { "passe.pass_s1_royal_commission", L("Comissão Real", "Royal Commission", "Comisión Real") },
            { "passe.pass_s2_ember_festival", L("Festival da Brasa", "Ember Festival", "Festival de Brasas") },
            { "liveops.chip", L("{0} pts · {1}", "{0} pts · {1}", "{0} pts · {1}") },   // {0} = pontos, {1} = tempo (Textos.Tempo); <= 20
            { "liveops.passe.chip", L("Passe {0}", "Pass {0}", "Pase {0}") },            // {0} = nivel
            { "liveops.termina", L("Termina em {0}", "Ends in {0}", "Termina en {0}") },
            { "liveops.pontos", L("{0} / {1} pontos", "{0} / {1} points", "{0} / {1} puntos") },
            { "liveops.proximo", L("Próximo marco: {0}", "Next milestone: {0}", "Próximo hito: {0}") },
            { "liveops.completo", L("Trilha completa!", "Track complete!", "¡Ruta completa!") },
            { "liveops.missoes", L("Missões (os pontos entram sozinhos)", "Missions (points add up on their own)", "Misiones (los puntos suman solos)") },
            { "liveops.botao.passe", L("PASSE", "PASS", "PASE") },
            { "liveops.passe.titulo", L("Passe: {0}", "Pass: {0}", "Pase: {0}") },
            { "liveops.passe.nivel", L("Nível {0}", "Level {0}", "Nivel {0}") },
            { "liveops.passe.xp", L("{0} / {1} XP", "{0} / {1} XP", "{0} / {1} XP") },
            { "liveops.passe.max", L("Trilha grátis completa!", "Free track complete!", "¡Ruta gratis completa!") },
            { "liveops.passe.fim", L("Temporada encerrada: resgate o que alcançou", "Season over: claim what you reached", "Temporada terminada: reclama lo alcanzado") },
            { "liveops.passe.fontes", L("XP de diárias, semanais, encomendas e eventos", "XP from daily and weekly goals, orders and events",
                "XP de diarias, semanales, pedidos y eventos") },
            { "liveops.projeto", L("Projeto de obra-prima", "Masterwork blueprint", "Plano de obra maestra") },
            { "aviso.evento", L("Evento: {0}", "Event: {0}", "Evento: {0}") },
            { "aviso.evento_marco", L("Marco do evento: {0} pts!", "Event milestone: {0} pts!", "¡Hito del evento: {0} pts!") },
            { "aviso.passe", L("Passe: nível {0}!", "Pass: level {0}!", "¡Pase: nivel {0}!") },
            { "aviso.passe_ouro", L("Prêmio do Passe! +{0}", "Pass reward! +{0}", "¡Premio del Pase! +{0}") },
            { "aviso.passe_ok", L("Prêmio do Passe!", "Pass reward!", "¡Premio del Pase!") },
            { "tempo.dh", L("{0}d {1}h", "{0}d {1}h", "{0}d {1}h") },
            { "tempo.hm", L("{0}h {1}m", "{0}h {1}m", "{0}h {1}m") },
            { "tempo.m", L("{0}m", "{0}m", "{0}m") },
            { "meta.worker_any", L("Contrate ou evolua {0} ajudante|Contrate ou evolua {0} ajudantes", "Hire or level up {0} helper|Hire or level up {0} helpers",
                "Contrata o mejora {0} ayudante|Contrata o mejora {0} ayudantes") },
            { "meta.play_day", L("Jogue em {0} dia do evento|Jogue em {0} dias do evento", "Play on {0} day of the event|Play on {0} days of the event",
                "Juega {0} día del evento|Juega {0} días del evento") },
            { "missao.order_item", L("Entregue {0} encomenda de {1}|Entregue {0} encomendas de {1}", "Deliver {0} order of {1}|Deliver {0} orders of {1}",
                "Entrega {0} pedido de {1}|Entrega {0} pedidos de {1}") },
            { "missao.nivel", L("Suba {0} nível: {1}", "Level up {0}x: {1}", "Sube {0} nivel: {1}") },
            { "missao.fina", L("Fabrique {0} {1} (Fina+)", "Craft {0} {1} (Fine+)", "Fabrica {0} {1} (Fina+)") },
            { "missao.superior", L("Fabrique {0} {1} (Superior)", "Craft {0} {1} (Superior)", "Fabrica {0} {1} (Superior)") },
            { "missao.peca", L("peça|peças", "item|items", "pieza|piezas") },
            { "missao.golpe", L("Acerte {0} golpe de mestre|Acerte {0} golpes de mestre", "Land {0} master strike|Land {0} master strikes",
                "Acierta {0} golpe maestro|Acierta {0} golpes maestros") },
            { "missao.golpe_item", L("Acerte {0} golpe em {1}|Acerte {0} golpes em {1}", "Land {0} strike on {1}|Land {0} strikes on {1}",
                "Acierta {0} golpe en {1}|Acierta {0} golpes en {1}") },
            { "cos.cos_ev_s1w1_selo", L("Selo do Emissário", "Envoy's Seal", "Sello del Emisario") },
            { "cos.cos_ev_s1w1_tapete", L("Tapete real do balcão", "Royal counter rug", "Alfombra real") },
            { "cos.cos_ev_s1w2_emblema", L("Emblema da Chama Dupla", "Twin Flame Emblem", "Emblema Doble Llama") },
            { "cos.cos_ev_s1w2_fole", L("Fole de ferro batido", "Wrought-iron bellows", "Fuelle de hierro") },
            { "cos.cos_ev_s1w3_emblema", L("Emblema do Armeiro", "Armorer's Emblem", "Emblema del Armero") },
            { "cos.cos_ev_s1w3_manequim", L("Manequim com armadura", "Armored mannequin", "Maniquí con armadura") },
            { "cos.cos_ev_s1w4_tocha", L("Tocha do Caçador", "Hunter's Torch", "Antorcha del Cazador") },
            { "cos.cos_ev_s1w5_emblema", L("Emblema da Lua de Forja", "Forge Moon Emblem", "Emblema Luna de Forja") },
            { "cos.cos_ev_s1w5_lanterna", L("Lanterna do Frenesi", "Frenzy Lantern", "Farol del Frenesí") },
            { "cos.cos_ev_s2w1_emblema", L("Emblema da Bigorna Acesa", "Lit Anvil Emblem", "Emblema Yunque Ardiente") },
            { "cos.cos_ev_s2w1_guirlanda", L("Guirlanda da fornalha", "Furnace garland", "Guirnalda del horno") },
            { "cos.cos_ev_s2w2_emblema", L("Emblema do Veio de Brasa", "Ember Vein Emblem", "Emblema Veta de Brasa") },
            { "cos.cos_ev_s2w2_carrinho", L("Carrinho de minério", "Ember ore cart", "Carro de mineral") },
            { "cos.cos_ev_s2w3_emblema", L("Emblema da Caravana", "Caravan Emblem", "Emblema de Caravana") },
            { "cos.cos_ev_s2w3_balanca", L("Balança do mercador", "Merchant's scale", "Balanza del mercader") },
            { "cos.cos_ev_s2w4_vitrine", L("Vitrine de brasa", "Ember showcase", "Vitrina de brasa") },
            { "cos.cos_ev_s2w5_emblema", L("Emblema do Arauto", "Herald's Emblem", "Emblema del Heraldo") },
            { "cos.cos_ev_s2w5_flamula", L("Flâmula do arauto", "Herald's pennant", "Banderín del heraldo") },
            { "cos.cos_ps1_selo_comissao", L("Selo da Comissão", "Commission Seal", "Sello de la Comisión") },
            { "cos.cos_ps1_bandana_azul", L("Bandana azul-real", "Royal blue bandana", "Pañuelo azul real") },
            { "cos.cos_ps1_flamula_real", L("Flâmula real", "Royal pennant", "Banderín real") },
            { "cos.cos_ps1_tabardo_guarda", L("Tabardo da guarda", "Guard tabard", "Tabardo de guardia") },
            { "cos.cos_ps1_moldura_brasao", L("Moldura Brasão", "Crest frame", "Marco de blasón") },
            { "cos.cos_ps1_estandarte", L("Estandarte da Comissão", "Commission banner", "Estandarte de la Comisión") },
            { "cos.cos_ps2_selo_brasa", L("Selo da Brasa", "Ember Seal", "Sello de Brasa") },
            { "cos.cos_ps2_bandana_brasa", L("Bandana brasa", "Ember bandana", "Pañuelo brasa") },
            { "cos.cos_ps2_lanternas_brasa", L("Lanternas de brasa", "Ember lanterns", "Faroles de brasa") },
            { "cos.cos_ps2_avental", L("Avental chamuscado", "Singed apron", "Delantal chamuscado") },
            { "cos.cos_ps2_moldura_brasas", L("Moldura Brasas", "Embers frame", "Marco de brasas") },
            { "cos.cos_ps2_braseiro", L("Braseiro do Festival", "Festival brazier", "Brasero del Festival") },
            // ---------- projetos (B-CORE-07): painel, o que cada Blueprint libera e de onde vem (Textos.Projeto*) ----------
            { "proj.titulo", L("Projetos", "Blueprints", "Planos") },
            { "proj.conta", L("{0} de {1}", "{0} of {1}", "{0} de {1}") },
            { "proj.tem", L("TEM", "OWNED", "TIENES") },
            { "proj.falta", L("BLOQUEADO", "LOCKED", "BLOQUEADO") },
            { "proj.ou", L(" ou ", " or ", " o ") },
            { "proj.k.0", L("Receita: {0}", "Recipe: {0}", "Receta: {0}") },   // BlueprintKind
            { "proj.k.1", L("Mestre: {0}", "Master: {0}", "Maestro: {0}") },
            { "proj.k.2", L("Técnica: {0}", "Technique: {0}", "Técnica: {0}") },
            { "proj.k.3", L("Obra-prima: {0}", "Masterwork: {0}", "Obra maestra: {0}") },
            { "proj.k.4", L("Visual: {0}", "Look: {0}", "Estilo: {0}") },
            { "proj.k.5", L("Especialidade: {0}", "Specialty: {0}", "Especialidad: {0}") },
            { "proj.libera.0", L("Libera a linha de {0}", "Unlocks the {0} line", "Abre la línea de {0}") },
            { "proj.libera.1", L("Libera os níveis IV e V", "Unlocks levels IV and V", "Abre los niveles IV y V") },
            { "proj.libera.2", L("+{0} de qualidade na família", "+{0} quality for the family", "+{0} de calidad en la familia") },
            { "proj.libera.3", L("Projeto da obra-prima do Hall", "Masterwork plan for the Hall", "Plano de obra maestra del Salón") },
            { "proj.libera.4", L("Novo visual (cosmético)", "New look (cosmetic)", "Nuevo estilo (cosmético)") },
            { "proj.libera.5", L("Especialização", "Specialization", "Especialización") },
            { "proj.f.0", L("Venda {0} {1} ({2}/{0})", "Sell {0} {1} ({2}/{0})", "Vende {0} {1} ({2}/{0})") },   // BlueprintSourceKind; {1} = item no plural
            { "proj.f.fina", L("Venda {0} {1} Fina+ ({2}/{0})", "Sell {0} Fine+ {1} ({2}/{0})", "Vende {0} {1} Fina+ ({2}/{0})") },
            { "proj.f.producao", L("Produção completa", "Full production", "Producción completa") },
            { "proj.f.1", L("Encomenda de projeto nº {0}", "Blueprint order #{0}", "Pedido de plano n.º {0}") },
            { "proj.f.2", L("Encomenda real nº {0}", "Royal order #{0}", "Pedido real n.º {0}") },
            { "proj.f.3", L("Renome {0}", "Renown {0}", "Renombre {0}") },
            { "proj.f.4", L("Conquista: {0}", "Achievement: {0}", "Logro: {0}") },
            { "proj.f.5", L("Contrato real", "Royal contract", "Contrato real") },
            { "proj.f.6", L("Evento", "Event", "Evento") },
            { "proj.f.7", L("Passe", "Pass", "Pase") },
            { "proj.f.8", L("Passe premium", "Premium pass", "Pase prémium") },
            { "proj.f.9", L("Loja", "Shop", "Tienda") },
            { "aviso.projeto", L("Projeto novo!", "New blueprint!", "¡Plano nuevo!") },
            // as 10 obras-primas do Old Forge (CONTEUDO_V1 §10.1; o B-CORE-08 usa o mesmo nome)
            { "mw.mw_primeira_lamina", L("Primeira Lâmina", "First Blade", "Primera Hoja") },
            { "mw.mw_gume_brasa", L("Gume de Brasa", "Ember Edge", "Filo de Brasa") },
            { "mw.mw_sunblade", L("Lâmina Solar do Rei", "King's Sunblade", "Hoja Solar Real") },
            { "mw.mw_primeira_guarda", L("Escudo da Primeira Guarda", "First Guard", "Primera Guardia") },
            { "mw.mw_broquel", L("Broquel de Carvalho e Ferro", "Oak Buckler", "Broquel de Roble") },
            { "mw.mw_escudo_brasao", L("Escudo do Brasão", "Crest Shield", "Escudo Blasón") },
            { "mw.mw_martelo_fundador", L("Martelo do Fundador", "Founder's Hammer", "Mazo Fundador") },
            { "mw.mw_tenaz_eterna", L("Tenaz Eterna", "Eternal Tongs", "Tenaza Eterna") },
            { "mw.mw_ember_crown", L("Coroa de Brasa", "Ember Crown", "Corona de Brasa") },
            { "mw.mw_anel_mercador", L("Anel do Mercador", "Merchant's Ring", "Anillo Mercader") },

            // ---------- Hall e Collection (B-CORE-08): painel com 2 abas, condicoes, marcos, metas e aviso (larguras no MasterworksTests) ----------
            { "hall.titulo", L("Hall", "Hall", "Salón") },
            { "hall.aba", L("Obras-primas", "Masterworks", "Obras maestras") },
            { "hall.colecao", L("Coleção", "Collection", "Colección") },
            { "hall.no_hall", L("NO HALL", "IN THE HALL", "EN EL SALÓN") },
            { "hall.pronta", L("PRONTA", "READY", "LISTA") },
            { "hall.pronta.dica", L("Golpe numa peça Superior", "Strike a Superior piece", "Golpe en una pieza Superior") },
            { "hall.desde", L("No Hall desde {0}", "In the Hall since {0}", "En el Salón desde {0}") },
            { "hall.data", L("dd/MM/yyyy", "MM/dd/yyyy", "dd/MM/yyyy") },
            { "hall.sem_condicao", L("Sem condição extra", "No extra condition", "Sin condición extra") },
            { "hall.projeto", L("Projeto: {0}", "Blueprint: {0}", "Plano: {0}") },
            { "hall.projeto_tem", L("Projeto na mão", "Blueprint owned", "Plano en mano") },
            { "hall.bonus", L("Família completa: +{0}% no preço de {1}", "Full family: +{0}% on {1} price", "Familia completa: +{0}% en {1}") },
            { "hall.c.0", L("Venda {0} {1} ({2}/{0})", "Sell {0} {1} ({2}/{0})", "Vende {0} {1} ({2}/{0})") },   // CondicaoMw
            { "hall.c.1", L("{0} nível {1} ({2}/{1})", "{0} level {1} ({2}/{1})", "{0} nivel {1} ({2}/{1})") },
            { "hall.c.2", L("Renome {0} ({1}/{0})", "Renown {0} ({1}/{0})", "Renombre {0} ({1}/{0})") },
            { "hall.c.3", L("Entregue {0} encomendas de {1} ({2}/{0})", "Deliver {0} {1} orders ({2}/{0})", "Entrega {0} pedidos de {1} ({2}/{0})") },
            { "hall.c.4", L("Entregue {0} encomendas {1} ({2}/{0})", "Deliver {0} {1} orders ({2}/{0})", "Entrega {0} pedidos {1} ({2}/{0})") },
            { "hall.c.5", L("{0} no nível {1} ({2}/{1})", "{0} at level {1} ({2}/{1})", "{0} en nivel {1} ({2}/{1})") },
            { "hall.c.6", L("Venda {0} {1}+ ({2}/{0})", "Sell {0} {1}+ ({2}/{0})", "Vende {0} {1}+ ({2}/{0})") },
            { "hall.tipo.common", L("comuns", "common", "comunes") },   // OrderType (o Ref da condicao EncomendasTipo, sem caixa)
            { "hall.tipo.bulk", L("grandes", "bulk", "grandes") },
            { "hall.tipo.quality", L("de qualidade", "quality", "de calidad") },
            { "hall.tipo.rush", L("urgentes", "rush", "urgentes") },
            { "hall.tipo.mixed", L("mistas", "mixed", "mixtos") },
            { "hall.tipo.royal", L("reais", "royal", "reales") },
            { "hall.tipo.blueprint", L("de projeto", "blueprint", "de plano") },
            { "hall.tipo.event", L("de evento", "event", "de evento") },
            { "col.aba.0", L("Produtos", "Products", "Productos") },   // AbaColecao
            { "col.aba.1", L("Obras-primas", "Masterworks", "Obras maestras") },
            { "col.aba.2", L("Equipe", "Crew", "Equipo") },
            { "col.aba.3", L("Projetos", "Blueprints", "Planos") },
            { "col.marco", L("{0}: {1} de {2}", "{0}: {1} of {2}", "{0}: {1} de {2}") },
            { "col.resumo", L("{0} {1}", "{0} {1}", "{0} {1}") },   // {0} = aba, {1} = "feitas/total"
            { "col.feito", L("FEITO", "DONE", "HECHO") },
            { "col.dica.0", L("Venda cada produto em Normal, Fina e Superior", "Sell each product as Normal, Fine and Superior", "Vende cada producto Normal, Fina y Superior") },   // AbaColecao
            { "col.dica.1", L("Golpe de mestre numa peça Superior, com o projeto", "Master strike on a Superior piece, with the blueprint", "Golpe maestro en una pieza Superior, con el plano") },
            { "col.dica.2", L("Contrate e evolua a equipe", "Hire and level up your crew", "Contrata y sube de nivel a tu equipo") },
            { "col.dica.3", L("Junte projetos", "Collect blueprints", "Junta planos") },
            { "cos.cos_col_estandarte_old_forge", L("Estandarte do Old Forge", "Old Forge banner", "Estandarte del Old Forge") },
            { "aviso.colecao", L("Coleção: +{0} Renome!", "Collection: +{0} Renown!", "¡Colección: +{0} Renombre!") },
            { "meta.masterwork_unlock", L("Faça {0} obra-prima|Faça {0} obras-primas", "Make {0} masterwork|Make {0} masterworks",
                "Haz {0} obra maestra|Haz {0} obras maestras") },
            { "meta.masterwork_um", L("Obra-prima: {0}", "Masterwork: {0}", "Obra maestra: {0}") },
            { "meta.masterwork_familia", L("Faça {0} obras-primas de {1}", "Make {0} {1} masterworks", "Haz {0} obras maestras de {1}") },
            { "ins.hall_completo", L("Hall Completo", "Full Hall", "Salón Completo") },
            { "ins.sol_do_rei", L("Sol do Rei", "King's Sun", "Sol del Rey") },
            { "ins.coroa_de_brasa", L("Coroa de Brasa", "Ember Crown", "Corona de Brasa") },

            // ---------- mundo ----------
            { "mundo.rua", L("Rua\nlateral", "Side\nstreet", "Calle\nlateral") },
            { "mundo.joalheria_fechada", L("Joalheria — fechada", "Jewelry — closed", "Joyería — cerrada") },
            { "mundo.max", L("MAX", "MAX", "MÁX") },

            // ---------- paineis ----------
            { "ui.ok", L("OK", "OK", "OK") },
            { "cofre.titulo", L("Bem-vindo de volta!", "Welcome back!", "¡Hola de nuevo!") },
            { "cofre.corpo", L("Seu cofre guardou isso pra você", "Your vault saved this for you", "Tu cofre guardó esto para ti") },
            { "cofre.pegar", L("PEGAR", "COLLECT", "RECOGER") },
            { "cofre.dobrar", L("DOBRAR +{0}", "DOUBLE +{0}", "DUPLICAR +{0}") },   // E-PLAT-02: botao de anuncio do painel, com o que da ANTES de assistir

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
            { "cfg.loja", L("LOJA", "SHOP", "TIENDA") },

            // ---------- loja (E-PLAT-04/05, Loja.cs): nome por produto (iap.<id>.nome) e uma linha por posse (iap.posse.<id>) ----------
            { "loja.titulo", L("Loja", "Shop", "Tienda") },
            { "loja.comprado", L("COMPRADO", "OWNED", "COMPRADO") },
            { "loja.restaurar", L("RESTAURAR COMPRAS", "RESTORE PURCHASES", "RESTAURAR COMPRAS") },
            { "loja.falsa", L("Loja de teste: nenhum pagamento real", "Test store: no real payment", "Tienda de prueba: ningún pago real") },
            { "iap.iap_pacote_mestre.nome", L("Pacote do Mestre", "Master's Pack", "Paquete del Maestro") },
            { "iap.posse.remove_ads", L("Sem anúncio entre telas", "No ads between screens", "Sin anuncios entre pantallas") },
            { "iap.posse.sem_video", L("Velocidade, Lingotes e Trocar sem vídeo", "Speed, Ingots and Swap without video",
                "Velocidad, Lingotes y Cambiar sin video") },

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
            { "ads.cofre2x", L("Cofre 2×", "Vault 2×", "Cofre 2×") },   // E-PLAT-02: um "ads.<Id>" por placement do Tuning (LocTests)
            { "ads.encomenda_bonus", L("Bônus da encomenda", "Order bonus", "Bono del pedido") },
            { "ads.encomenda_troca", L("Trocar encomenda", "Swap order", "Cambiar pedido") },
            { "ads.supply", L("Lingotes", "Ingots", "Lingotes") },
            { "adtest.intersticial", L("Intersticial (sem recompensa)", "Interstitial (no reward)", "Intersticial (sin recompensa)") },
            { "adtest.titulo", L("Anúncio de teste", "Test ad", "Anuncio de prueba") },
            { "adtest.premio", L("Recompensa: {0} x1", "Reward: {0} x1", "Recompensa: {0} x1") },
            { "adtest.rodape", L("Simulado: nenhum anúncio real foi pedido nem exibido", "Simulated: no real ad was requested or shown",
                "Simulado: no se pidió ni se mostró ningún anuncio real") },
            { "adtest.recebida", L("Recompensa recebida: {0}", "Reward received: {0}", "Recompensa recibida: {0}") },
            { "adtest.sem", L("Sem recompensa: {0}", "No reward: {0}", "Sin recompensa: {0}") },
        };
    }
}
