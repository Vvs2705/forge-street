using System;
using System.Collections.Generic;
using System.Reflection;

namespace FS.Core
{
    /// <summary>Os 8 templates do DM §29 (LIVEOPS_V1 §2.4). O Arcane Week fica fora de S1/S2: entra com o distrito 5.</summary>
    public enum Template { RoyalWeek, ForgeFrenzy, ArmorersChallenge, MasterworkHunt, BlacksmithFestival, RareOreRush, MerchantCaravan, ArcaneWeek }

    /// <summary>LIVEOPS_V1 §5.1: tipo de recompensa. Nao existe bau, chance nem sorteio: "nada aleatorio" e' estrutural (V4).</summary>
    public enum TipoPremio { Ouro, Renome, PassXp, Cosmetico, Blueprint, Conveniencia, Gems }

    /// <summary>Uma recompensa. Ouro: Valor = s de renda (x RateEma no credito, fora do GoldEarned); Renome/PassXp: quantidade; Cosmetico e
    /// Blueprint: Ref (posse = o ID de transacao no ledger); Conveniencia: Ref + cargas; Gems: Wave E. Blueprint sempre com Fallback (quem ja
    /// tem, ou sem o sistema de Blueprints, recebe o fallback no mesmo ID).</summary>
    public sealed class RewardDef
    {
        public readonly TipoPremio Tipo; public readonly int Valor; public readonly string Ref; public readonly RewardDef Fallback;
        public RewardDef(TipoPremio tipo, int valor, string refId = null, RewardDef fallback = null) { Tipo = tipo; Valor = valor; Ref = refId; Fallback = fallback; }
    }

    /// <summary>Missao de evento (MissionDef kind Event, LIVEOPS_V1 §5.2): gatilho da taxonomia, filtro por ID (item, melhoria ou tipo de
    /// encomenda; vazio = qualquer; varios = soma), alvo e pontos de evento. Compra e contratacao completam sozinhas sem nada a comprar.</summary>
    public sealed class MissaoEvento
    {
        public readonly string Id, Evento; public readonly string[] Filtro; public readonly long Alvo; public readonly int Pontos;
        public MissaoEvento(string id, string evento, long alvo, int pontos, params string[] filtro) { Id = id; Evento = evento; Alvo = alvo; Pontos = pontos; Filtro = filtro; }
        public bool Auto => Evento == "upgrade_buy" || Evento == "worker_any";
    }

    /// <summary>Modificador de evento sobre um knob do Tuning (nome exato do campo; LIVEOPS_V1 §2.3): x Valor, ou + Valor (Soma). Item = so o
    /// elemento daquele item num array por item (Price, ClientInterval); null = o campo inteiro.</summary>
    public sealed class Modificador
    {
        public readonly string Knob, Item; public readonly bool Soma; public readonly float Valor;
        public Modificador(string knob, float valor, string item = null, bool soma = false) { Knob = knob; Valor = valor; Item = item; Soma = soma; }
    }

    /// <summary>Limiar da trilha de pontos do evento: cruzou, credita (ID e:{evento}:{pontos}).</summary>
    public sealed class Marco
    {
        public readonly int Pontos; public readonly RewardDef[] Premios;
        public Marco(int pontos, params RewardDef[] premios) { Pontos = pontos; Premios = premios; }
    }

    /// <summary>
    /// D-CORE-03 (LIVEOPS_V1 §5.3, os 9 campos do DM §28): id (unico para sempre; reprise = id novo), inicio/fim (dias desde T0, 13:00 UTC),
    /// requisitos (sempre as Ferramentas + Abre/Renome/Encomendas; Requer = IDs de OrderDef/BlueprintDef que o evento pede: fora do build, o
    /// evento fica fora), missoes, recompensas
    /// (Trilha), pontos de evento (as missoes; a encomenda Event e' do Orders 2.0), visual (Tema), modificadores e ofertas (vazio ate a Wave E).
    /// </summary>
    public sealed class EventDef
    {
        public string Id, Temporada, Abre, Tema, Encomenda;
        public Template Template;
        public int Inicio, Dias = 6, Renome, Encomendas;
        public bool Reserva;
        public string[] Requer = new string[0], Ofertas = new string[0];
        public MissaoEvento[] Missoes;
        public Marco[] Trilha;
        public Modificador[] Modificadores = new Modificador[0];
        public long Comeca => LiveOpsDefs.T0 + Inicio * LiveOpsDefs.Dia;
        public long Termina => Comeca + Dias * LiveOpsDefs.Dia;
    }

    /// <summary>D-CORE-04 (LIVEOPS_V1 §3, §5.4): temporada do Masterwork Pass. Nivel n custa n x XpNivel acumulado; Free[n-1] e Premium[n-1]
    /// por nivel (premium so no dado ate a Wave E: LiveOpsDefs.PremiumLigado). XP de encomenda com teto por dia local (V20).</summary>
    public sealed class PassDef
    {
        public string Id, Temporada, Tema;
        public int Inicio, Dias = 35, XpNivel = 600, XpEncomenda = 10, TetoEncomendaDia = 100;
        public RewardDef[] Free, Premium;
        public int Niveis => Free.Length;
        public long XpTotal => (long)Niveis * XpNivel;
        public long Comeca => LiveOpsDefs.T0 + Inicio * LiveOpsDefs.Dia;
        public long Termina => Comeca + Dias * LiveOpsDefs.Dia;
    }

    /// <summary>
    /// D-PROD-01/02 (LIVEOPS_V1 §2-§3): o calendario de 8 semanas + 2 reservas e o Pass S1/S2, em dados; e as regras do validator (§5.7).
    /// Evento novo entra so com dados (LiveOpsTests). Gatilho que o nucleo ainda nao emite (Orders 2.0, niveis de worker) ou knob que o Tuning
    /// ainda nao tem deixa o evento FORA (LiveOps.Falta: o calendario pula a semana) ate o sistema chegar; nunca e' erro.
    /// </summary>
    public static class LiveOpsDefs
    {
        public const long Dia = 86400;
        /// <summary>T0 da Season 1: 2026-11-02 13:00 UTC (segunda, 10:00 de Brasilia). DECISAO "datas" do D-PROD-01 pendente: padrao do time, revisar.</summary>
        public const long T0 = 1793624400;
        /// <summary>V9/P1, LIVEOPS_V1 §3.5: a trilha premium so existe no dado ate a Wave E (sem IAP nem Gems). O validator recusa ligada.</summary>
        public const bool PremiumLigado = false;
        /// <summary>Encomendas por dia de 20 min do F2P de referencia na conta estatica do Pass: o portao da Wave B (>= 25 em 60 min, LIVEOPS_V1 §3.3).</summary>
        public const double EncomendasDiaF2P = 8;

        /// <summary>Gatilhos que o nucleo emite hoje para missao de evento (LiveOps.On; play_day = dias distintos com o evento aberto, o "jogar em 3
        /// dias" do LIVEOPS_V1 §2.4; quality_* pelo Ev.Graded; master_strike pelo golpe de mestre; worker_any = contratou ou subiu nivel de um
        /// worker, o "contratar ou subir de nivel" do §2.4, B-CORE-04).</summary>
        public static readonly string[] Gatilhos =
        {
            "product_sold", "product_crafted", "order_done", "upgrade_buy", "offline_claim", "vip_served", "worker_any",
            "quality_crafted", "quality_superior", "master_strike", "play_day",
            "order_complete",   // B-CORE-05: entrega por tipo (LiveOps.OnEncomenda; filtro = TiposEncomenda)
            "contract_complete",   // D-CORE-04: Royal Contract concluido (filtro = ID ctr_*; vazio = qualquer): a Royal Week ganha pontos por ele so com a missao no dado
        };
        /// <summary>Ganchos: gatilhos do LIVEOPS_V1 §4.6 que nascem com o sistema deles (vazio: o order_complete chegou com o Orders 2.0).</summary>
        public static readonly string[] Futuros = new string[0];
        /// <summary>Tipos de encomenda do Orders 2.0 (filtro do order_complete; ECONOMIA_WAVE_B §3).</summary>
        public static readonly string[] TiposEncomenda = { "common", "bulk", "quality", "mixed", "rush", "royal", "event", "blueprint" };

        /// <summary>LIVEOPS_V1 §2.3: os knobs que um evento pode tocar (campo do Tuning), x ou +, e a faixa (sempre do lado do jogador). Fora
        /// daqui e' proibido: offline (P2), anuncio e recargas (V11), custo de upgrade. GrabTime ainda e' const do Balance e QualidadeEvento nao
        /// existe: evento que os usa fica fora ate virarem campo do Tuning.</summary>
        public static readonly (string Knob, bool Soma, float Min, float Max)[] Knobs =
        {
            ("ClientInterval", false, 0.75f, 1f), ("PatienceBase", false, 1f, 1.3f), ("VipMin", false, 0.7f, 1f), ("VipMax", false, 0.7f, 1f),
            ("Price", false, 1f, 1.25f), ("JewelPriceUp", false, 1f, 1.25f),
            ("FurnaceTime0", false, 0.8f, 1f), ("FurnaceTime1", false, 0.8f, 1f), ("FurnaceTime2", false, 0.8f, 1f),
            ("HammerTime0", false, 0.85f, 1f), ("HammerTime1", false, 0.85f, 1f), ("ShieldTimeMul", false, 0.85f, 1f), ("JewelTimeMul", false, 0.85f, 1f),
            ("GrabTime", false, 0.7f, 1f), ("OrderRewardSeconds", false, 1f, 1.3f), ("QualidadeEvento", true, 0f, 25f), ("GolpeRecarga", false, 0.75f, 1f),
        };

        static RewardDef Ouro(int s) => new RewardDef(TipoPremio.Ouro, s);
        static RewardDef Xp(int n) => new RewardDef(TipoPremio.PassXp, n);
        static RewardDef Ren(int n) => new RewardDef(TipoPremio.Renome, n);
        static RewardDef Cos(string id) => new RewardDef(TipoPremio.Cosmetico, 0, id);
        static RewardDef Bp(string id) => new RewardDef(TipoPremio.Blueprint, 0, id, Ouro(60));
        static MissaoEvento M(string id, string ev, long alvo, int pontos, params string[] filtro) => new MissaoEvento(id, ev, alvo, pontos, filtro);

        /// <summary>LIVEOPS_V1 §2.2: a trilha comum (135 s de ouro, 8 de Renome, 500 de XP do Pass, 2 cosmeticos); no 450 o cosmetico menor (nos
        /// Hunts, o projeto da Masterwork com fallback de 60 s).</summary>
        static Marco[] Trilha(RewardDef r450, string cos1000) => new[]
        {
            new Marco(50, Ouro(30)), new Marco(100, Xp(50)), new Marco(175, Ren(3)), new Marco(250, Ouro(45)), new Marco(350, Xp(100)),
            new Marco(450, r450), new Marco(575, Ouro(60)), new Marco(700, Xp(150)), new Marco(850, Ren(5)), new Marco(1000, Xp(200), Cos(cos1000)),
        };

        static Modificador[] Vip75() => new[] { new Modificador("VipMin", 0.75f), new Modificador("VipMax", 0.75f) };
        static Modificador[] Frenesi() => new[] { new Modificador("ClientInterval", 0.8f), new Modificador("PatienceBase", 1.15f) };
        static Modificador[] Fornalha(float k) => new[] { new Modificador("FurnaceTime0", k), new Modificador("FurnaceTime1", k), new Modificador("FurnaceTime2", k) };
        static T[] Mais<T>(T[] a, params T[] b) { var r = new T[a.Length + b.Length]; a.CopyTo(r, 0); b.CopyTo(r, a.Length); return r; }

        const string S1 = "s1_royal_commission", S2 = "s2_ember_festival";

        /// <summary>LIVEOPS_V1 §2.4: os 10 eventos (8 do calendario + 2 reservas), 6 dias cada, sem 2 ao mesmo tempo. Missoes somam 700 pontos.</summary>
        public static readonly EventDef[] Eventos =
        {
            new EventDef
            {
                Id = "ev_s1w1_royal_week", Template = Template.RoyalWeek, Temporada = S1, Inicio = 0, Renome = 1,
                Missoes = new[]
                {
                    M("vip5", "vip_served", 5, 100), M("royal2", "order_complete", 2, 120, "royal"), M("vende300", "product_sold", 300, 60),
                    M("fina40", "quality_crafted", 40, 100), M("escudos50", "product_sold", 50, 60, "escudo"), M("dias3", "play_day", 3, 100),
                    M("cofre2", "offline_claim", 2, 60), M("golpe10", "master_strike", 10, 100),
                },
                Trilha = Trilha(Cos("cos_ev_s1w1_selo"), "cos_ev_s1w1_tapete"), Encomenda = "ord_ev_royal_week", Tema = "royal", Modificadores = Vip75(),
            },
            new EventDef
            {
                Id = "ev_s1w2_forge_frenzy", Template = Template.ForgeFrenzy, Temporada = S1, Inicio = 7, Encomendas = 10,
                Missoes = new[]
                {
                    M("vende500", "product_sold", 500, 120), M("lingotes300", "product_crafted", 300, 80, "lingote"), M("enc8", "order_done", 8, 100),
                    M("espadas100", "product_sold", 100, 60, "espada"), M("ferram60", "product_sold", 60, 60, "ferramenta"), M("vip2", "vip_served", 2, 60),
                    M("melhoria3", "upgrade_buy", 3, 80), M("dias3", "play_day", 3, 140),
                },
                Trilha = Trilha(Cos("cos_ev_s1w2_emblema"), "cos_ev_s1w2_fole"), Encomenda = "ord_ev_forge_frenzy", Tema = "frenesi", Modificadores = Frenesi(),
            },
            new EventDef
            {
                Id = "ev_s1w3_armorers_challenge", Template = Template.ArmorersChallenge, Temporada = S1, Inicio = 14, Renome = 1,
                Missoes = new[]
                {
                    // "escudos ou pecas de armadura": as armaduras (distrito 2, Wave C) entram no filtro quando o ItemDef delas existir
                    M("escudos150", "product_sold", 150, 100, "escudo"), M("forja_escudos100", "product_crafted", 100, 80, "escudo"),
                    M("enc_escudo4", "order_done", 4, 100, "escudo"), M("fina_escudos20", "quality_crafted", 20, 100, "escudo"),
                    M("nivel_escudos", "upgrade_buy", 1, 80, "escudos_nivel2", "escudos_nivel3"), M("golpe_escudos5", "master_strike", 5, 90, "escudo"),
                    M("vende300", "product_sold", 300, 50), M("dias3", "play_day", 3, 100),
                },
                Trilha = Trilha(Cos("cos_ev_s1w3_emblema"), "cos_ev_s1w3_manequim"), Encomenda = "ord_ev_armorers_challenge", Tema = "armeiro",
                Modificadores = new[] { new Modificador("Price", 1.2f, "escudo"), new Modificador("ShieldTimeMul", 0.9f) },
            },
            new EventDef
            {
                Id = "ev_s1w4_hunt_sunblade", Template = Template.MasterworkHunt, Temporada = S1, Inicio = 21, Renome = 1, Requer = new[] { "bp_mw_primeira_lamina" },
                Missoes = new[]
                {
                    M("espadas400", "product_sold", 400, 70, "espada"), M("superior_espadas10", "quality_superior", 10, 120, "espada"),
                    M("golpe_espadas15", "master_strike", 15, 100, "espada"), M("royal2", "order_complete", 2, 100, "royal"),
                    M("quality2", "order_complete", 2, 90, "quality"), M("nivel_bigornas", "upgrade_buy", 1, 70, "bigornas_nivel2", "bigornas_nivel3"),
                    M("vende1000", "product_sold", 1000, 50), M("dias3", "play_day", 3, 100),
                },
                Trilha = Trilha(Bp("bp_mw_sunblade"), "cos_ev_s1w4_tocha"), Encomenda = "ord_ev_hunt_sunblade", Tema = "hunt",
                Modificadores = new[] { new Modificador("QualidadeEvento", 15f, "espada", true), new Modificador("GolpeRecarga", 0.75f) },
            },
            new EventDef
            {
                Id = "ev_s1w5_forge_frenzy_b", Template = Template.ForgeFrenzy, Temporada = S1, Inicio = 28, Abre = "joalheria", Reserva = true,
                Missoes = new[]
                {
                    M("vende400", "product_sold", 400, 100), M("joias40", "product_sold", 40, 100, "joia"), M("lingotes250", "product_crafted", 250, 80, "lingote"),
                    M("enc8", "order_done", 8, 120), M("escudos80", "product_sold", 80, 80, "escudo"), M("vip2", "vip_served", 2, 60),
                    M("cofre2", "offline_claim", 2, 60), M("dias3", "play_day", 3, 100),
                },
                Trilha = Trilha(Cos("cos_ev_s1w5_emblema"), "cos_ev_s1w5_lanterna"), Encomenda = "ord_ev_forge_frenzy_b", Tema = "frenesi_noite", Modificadores = Frenesi(),
            },
            new EventDef
            {
                Id = "ev_s2w1_blacksmith_festival", Template = Template.BlacksmithFestival, Temporada = S2, Inicio = 35, Encomendas = 10,
                Missoes = new[]
                {
                    // "contratar ou subir de nivel 1 worker" (B-CORE-04); sem ninguem a contratar nem nivel a subir, completa sozinha
                    M("lingotes400", "product_crafted", 400, 100, "lingote"), M("vende400", "product_sold", 400, 80), M("dias3", "play_day", 3, 100),
                    M("golpe5", "master_strike", 5, 80), M("contrata1", "worker_any", 1, 80), M("enc6", "order_done", 6, 100),
                    M("cofre2", "offline_claim", 2, 60), M("vip3", "vip_served", 3, 100),
                },
                Trilha = Trilha(Cos("cos_ev_s2w1_emblema"), "cos_ev_s2w1_guirlanda"), Encomenda = "ord_ev_blacksmith_festival", Tema = "festival",
                Modificadores = Mais(Fornalha(0.85f), new Modificador("ClientInterval", 0.9f)),
            },
            new EventDef
            {
                Id = "ev_s2w2_rare_ore_rush", Template = Template.RareOreRush, Temporada = S2, Inicio = 42, Renome = 1,
                Missoes = new[]
                {
                    M("lingotes600", "product_crafted", 600, 150, "lingote"), M("vende300", "product_sold", 300, 80), M("bulk2", "order_complete", 2, 100, "bulk"),
                    M("melhoria2", "upgrade_buy", 2, 60), M("ferram60", "product_sold", 60, 80, "ferramenta"), M("contrata1", "worker_any", 1, 80),
                    M("dias3", "play_day", 3, 150),
                },
                Trilha = Trilha(Cos("cos_ev_s2w2_emblema"), "cos_ev_s2w2_carrinho"), Encomenda = "ord_ev_rare_ore_rush", Tema = "minerio",
                Modificadores = Mais(new[] { new Modificador("GrabTime", 0.7f) }, Fornalha(0.9f)),
            },
            new EventDef
            {
                Id = "ev_s2w3_merchant_caravan", Template = Template.MerchantCaravan, Temporada = S2, Inicio = 49, Renome = 1, Encomendas = 5,
                Requer = new[] { "ord_bulk_old_forge" },
                Missoes = new[]
                {
                    M("bulk2", "order_complete", 2, 140, "bulk"), M("mixed2", "order_complete", 2, 120, "mixed"), M("enc10", "order_done", 10, 80),
                    M("vende500", "product_sold", 500, 60), M("escudos80", "product_sold", 80, 60, "escudo"), M("cofre2", "offline_claim", 2, 60),
                    M("vip2", "vip_served", 2, 80), M("dias3", "play_day", 3, 100),
                },
                Trilha = Trilha(Cos("cos_ev_s2w3_emblema"), "cos_ev_s2w3_balanca"), Encomenda = "ord_ev_merchant_caravan", Tema = "caravana",
                Modificadores = new[] { new Modificador("OrderRewardSeconds", 1.25f) },
            },
            new EventDef
            {
                Id = "ev_s2w4_hunt_ember_crown", Template = Template.MasterworkHunt, Temporada = S2, Inicio = 56, Abre = "joalheria", Renome = 1,
                Missoes = new[]
                {
                    M("joias80", "product_sold", 80, 70, "joia"), M("fina_joias20", "quality_crafted", 20, 120, "joia"), M("golpe_joias10", "master_strike", 10, 100, "joia"),
                    M("royal2", "order_complete", 2, 100, "royal"), M("nivel_joalheria", "upgrade_buy", 1, 70, "joalheria_nivel2", "joalheria_nivel3"),
                    M("vende800", "product_sold", 800, 50), M("enc_joia3", "order_done", 3, 90, "joia"), M("dias3", "play_day", 3, 100),
                },
                Trilha = Trilha(Bp("bp_mw_ember_crown"), "cos_ev_s2w4_vitrine"), Encomenda = "ord_ev_hunt_ember_crown", Tema = "hunt_brasa",
                Modificadores = new[] { new Modificador("QualidadeEvento", 15f, "joia", true), new Modificador("GolpeRecarga", 0.75f) },
            },
            new EventDef
            {
                Id = "ev_s2w5_royal_week_b", Template = Template.RoyalWeek, Temporada = S2, Inicio = 63, Renome = 1, Reserva = true,
                Missoes = new[]
                {
                    M("vip5", "vip_served", 5, 120), M("vende300", "product_sold", 300, 60), M("escudos60", "product_sold", 60, 80, "escudo"),
                    M("espadas100", "product_sold", 100, 80, "espada"), M("enc6", "order_done", 6, 120), M("dias3", "play_day", 3, 100),
                    M("cofre2", "offline_claim", 2, 60), M("lingotes200", "product_crafted", 200, 80, "lingote"),
                },
                Trilha = Trilha(Cos("cos_ev_s2w5_emblema"), "cos_ev_s2w5_flamula"), Encomenda = "ord_ev_royal_week_b", Tema = "royal_tarde", Modificadores = Vip75(),
            },
        };

        /// <summary>LIVEOPS_V1 §3.4, trilha free (S1 e S2 iguais em tipo e valor; so os 6 cosmeticos mudam de tema): ouro 1.170 s em 17 niveis,
        /// Renome +44 em 7, cosmetico nos 4, 10, 15, 20, 25 e 30.</summary>
        static RewardDef[] Free(params string[] cos) => new[]
        {
            Ouro(30), Ren(3), Ouro(30), Cos(cos[0]), Ren(5), Ouro(45), Ouro(45), Ren(5), Ouro(45), Cos(cos[1]),
            Ouro(60), Ren(5), Ouro(60), Ouro(60), Cos(cos[2]), Ouro(60), Ren(8), Ouro(75), Ouro(75), Cos(cos[3]),
            Ouro(75), Ren(8), Ouro(90), Ouro(90), Cos(cos[4]), Ouro(90), Ren(10), Ouro(120), Ouro(120), Cos(cos[5]),
        };

        /// <summary>LIVEOPS_V1 §3.4, trilha premium (DESLIGADA ate a Wave E): so cosmetico/decoracao (C/D), conveniencia em cargas (V: Supply
        /// Drop, Trocar encomenda, Velocidade) e Gems (G, valor decidido com o mapa fonte -> ralo da Wave E). Nunca ouro, Renome, XP nem Blueprint.</summary>
        static RewardDef[] Premium(params string[] cos)
        {
            const string Padrao = "CVDVGCVDVCDVCVGDCVDCVDCVGDCVDC";
            (string, int)[] conv = { ("conv_supply", 1), ("conv_trocar", 1), ("conv_velocidade", 1), ("conv_supply", 1), ("conv_trocar", 1),
                ("conv_velocidade", 1), ("conv_supply", 2), ("conv_trocar", 2), ("conv_velocidade", 2), ("conv_supply", 2) };
            var r = new RewardDef[Padrao.Length];
            int c = 0, v = 0;
            for (int n = 0; n < r.Length; n++)
                r[n] = Padrao[n] == 'V' ? new RewardDef(TipoPremio.Conveniencia, conv[v].Item2, conv[v++].Item1)
                    : Padrao[n] == 'G' ? new RewardDef(TipoPremio.Gems, 0) : Cos(cos[c++]);
            return r;
        }

        /// <summary>LIVEOPS_V1 §3.1: 2 temporadas de 35 dias emendadas (a semana 5 e' a folga), 30 niveis x 600 XP.</summary>
        public static readonly PassDef[] Passes =
        {
            new PassDef
            {
                Id = "pass_s1_royal_commission", Temporada = S1, Inicio = 0, Tema = "royal",
                Free = Free("cos_ps1_selo_comissao", "cos_ps1_bandana_azul", "cos_ps1_flamula_real", "cos_ps1_tabardo_guarda", "cos_ps1_moldura_brasao", "cos_ps1_estandarte"),
                Premium = Premium("cos_ps1p_gibao_azul", "cos_ps1p_flamula_leao", "cos_ps1p_martelo_bronze", "cos_ps1p_tapete_corte", "cos_ps1p_pajem",
                    "cos_ps1p_busto_rei", "cos_ps1p_moedas_selo", "cos_ps1p_lanternas_azuis", "cos_ps1p_arauto", "cos_ps1p_toldo_real", "cos_ps1p_reverencia",
                    "cos_ps1p_base_marmore", "cos_ps1p_rastro_petalas", "cos_ps1p_bigorna_brasao", "cos_ps1p_fornalha_friso", "cos_ps1p_estandarte_duplo", "cos_ps1p_armadura_gala"),
            },
            new PassDef
            {
                Id = "pass_s2_ember_festival", Temporada = S2, Inicio = 35, Tema = "brasa",
                Free = Free("cos_ps2_selo_brasa", "cos_ps2_bandana_brasa", "cos_ps2_lanternas_brasa", "cos_ps2_avental", "cos_ps2_moldura_brasas", "cos_ps2_braseiro"),
                Premium = Premium("cos_ps2p_gibao_chamuscado", "cos_ps2p_flamula_fenix", "cos_ps2p_martelo_rubro", "cos_ps2p_tapete_cinzas", "cos_ps2p_aprendiz",
                    "cos_ps2p_braseiro_pedra", "cos_ps2p_moedas_brasa", "cos_ps2p_lanternas_festival", "cos_ps2p_mercadora", "cos_ps2p_toldo_laranja",
                    "cos_ps2p_martelo_alto", "cos_ps2p_base_obsidiana", "cos_ps2p_rastro_faiscas", "cos_ps2p_bigorna_rubra", "cos_ps2p_fornalha_brasa",
                    "cos_ps2p_arco_fitas", "cos_ps2p_coroa_fogo"),
            },
        };

        /// <summary>Campo publico e gravavel do Tuning com este nome (o knob existe), ou null.</summary>
        public static FieldInfo Campo(string knob)
        {
            FieldInfo f = knob == null ? null : typeof(Tuning).GetField(knob, BindingFlags.Public | BindingFlags.Instance);
            return f == null || f.IsInitOnly ? null : f;
        }

        /// <summary>Indice do filtro `id` do gatilho `evento` no conteudo (melhoria pelo Upgrades, tipo de encomenda pela lista, item pelo
        /// `items`), ou -1.</summary>
        public static int Indice(string evento, string id, ItemDef[] items) =>
            evento == "upgrade_buy" ? Upgrades.IndexOf(id) : evento == "order_complete" ? Array.IndexOf(TiposEncomenda, id)
            : evento == "contract_complete" ? ContratosDefs.IndexOf(id) : Items.IndexOf(id, items);

        /// <summary>Por que o evento ainda nao pode rodar neste conteudo (gatilho, filtro, knob ou requisito de um sistema que nao existe), ou
        /// null = pronto. O calendario so ativa evento pronto.</summary>
        public static string Falta(EventDef e, ItemDef[] items)
        {
            foreach (MissaoEvento x in e.Missoes)
            {
                if (Array.IndexOf(Gatilhos, x.Evento) < 0) return "gatilho " + x.Evento;
                foreach (string f in x.Filtro) if (Indice(x.Evento, f, items) < 0) return "filtro " + f;
            }
            foreach (Modificador m in e.Modificadores) if (Campo(m.Knob) == null) return "knob " + m.Knob;
            foreach (string r in e.Requer) if (!Existe(r)) return "requisito " + r;
            return null;
        }

        /// <summary>O ID de sistema que o evento pede ja existe no build: OrderDef (B-CORE-05) ou BlueprintDef (B-CORE-07).</summary>
        static bool Existe(string id) => Array.Exists(OrderDefs.All, d => d.Id == id) || ProjetosDefs.IndexOf(id, ProjetosDefs.All) >= 0;

        public static long XpTrilha(EventDef e) => Soma(e, TipoPremio.PassXp);

        /// <summary>Soma de um tipo de recompensa na trilha (o Blueprint conta o fallback).</summary>
        static long Soma(EventDef e, TipoPremio tipo)
        {
            long n = 0;
            foreach (Marco m in e.Trilha) foreach (RewardDef r in m.Premios) { RewardDef x = r.Tipo == TipoPremio.Blueprint ? r.Fallback ?? r : r; if (x.Tipo == tipo) n += x.Valor; }
            return n;
        }

        /// <summary>
        /// LIVEOPS_V1 §5.4, regra de fechamento: XP por dia do F2P de referencia = 3 diarias + 4 semanais / 7 (o MENOR XP de cada lista, dos
        /// MetasDefs) + encomendas (ate o teto do dia) + o XP das trilhas dos eventos da temporada / dias da temporada (`soProntos` = so os que
        /// rodam neste build). O conteudo real, nao uma tabela a parte.
        /// </summary>
        public static double XpDiaF2P(PassDef p, EventDef[] eventos, double encomendasDia, bool soProntos = false)
        {
            int dly = int.MaxValue, wkl = int.MaxValue;
            foreach (MetaDef d in MetasDefs.Diarias) dly = Math.Min(dly, d.Xp);
            foreach (MetaDef d in MetasDefs.Semanais) wkl = Math.Min(wkl, d.Xp);
            double xp = Metas.NDiarias * dly + Metas.NSemanais * wkl / 7.0 + Math.Min(p.TetoEncomendaDia, encomendasDia * p.XpEncomenda);
            foreach (EventDef e in eventos)
                if (e.Temporada == p.Temporada && (!soProntos || Falta(e, Items.All) == null)) xp += XpTrilha(e) / (double)p.Dias;
            return xp;
        }

        /// <summary>Dia da temporada em que o F2P de referencia fecha a trilha free.</summary>
        public static double DiaFechamento(PassDef p, EventDef[] eventos, double encomendasDia, bool soProntos = false) =>
            p.XpTotal / XpDiaF2P(p, eventos, encomendasDia, soProntos);

        static bool IdOk(string id, string prefixo) => id != null && id.StartsWith(prefixo, StringComparison.Ordinal) &&
            Array.TrueForAll(id.ToCharArray(), c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_');

        /// <summary>
        /// LIVEOPS_V1 §5.7 (regras cruzadas): IDs unicos e no formato; cosmetico numa fonte so (evento, Pass, login); temporadas de 28-35 dias
        /// emendadas; todo nivel com recompensa free (nada obrigatorio so no premium); premium so cosmetico/conveniencia/Gems e desligado ate a
        /// Wave E; regra de fechamento; evento dentro da temporada, sem sobreposicao (um slot), requisito de D0/D1; missao com gatilho conhecido e
        /// nunca punitivo ou de anuncio, filtro que existe, VIP que fecha sem anuncio, <= 2 encomendas de cada tipo especial, pontos >= 70% do
        /// ultimo limiar; trilha crescente e nas faixas; knob na tabela do §2.3 e na faixa; sem oferta ate a Wave E; reserva so com conteudo de
        /// hoje; orcamento de ouro de LiveOps por dia (com o das metas).
        /// </summary>
        public static IEnumerable<string> Validar(EventDef[] eventos, PassDef[] passes, bool premiumLigado = PremiumLigado)
        {
            var ids = new HashSet<string>();
            var cos = new HashSet<string>(MetasDefs.LoginCosmeticos);
            if (premiumLigado) yield return "passe: trilha premium ligada antes da Wave E (sem IAP nem Gems, V9/P1)";
            var ps = new List<PassDef>(passes);
            ps.Sort((a, b) => a.Inicio.CompareTo(b.Inicio));
            double ouroPasse = 0;
            for (int k = 0; k < ps.Count; k++)
            {
                PassDef p = ps[k];
                string at = $"passe {p.Id}:";
                if (!IdOk(p.Id, "pass_")) yield return $"{at} ID fora do formato pass_[a-z0-9_]";
                else if (!ids.Add(p.Id)) yield return $"{at} ID repetido";
                if (p.Dias < 28 || p.Dias > 35) yield return $"{at} {p.Dias} dias (temporada de 28-35, DM §33)";
                if (k > 0 && p.Inicio != ps[k - 1].Inicio + ps[k - 1].Dias) yield return $"{at} nao emenda com {ps[k - 1].Id} (sobreposicao ou buraco)";
                if (p.XpNivel <= 0 || p.XpEncomenda <= 0 || p.TetoEncomendaDia < p.XpEncomenda) yield return $"{at} XP por nivel/encomenda/teto invalido";
                if (p.Free == null || p.Free.Length == 0) { yield return $"{at} sem niveis"; continue; }
                if (p.Premium != null && p.Premium.Length != p.Free.Length) yield return $"{at} premium com {p.Premium.Length} niveis e free com {p.Free.Length}";
                long ouro = 0;
                for (int n = 0; n < p.Free.Length; n++)
                {
                    RewardDef r = p.Free[n];
                    if (r == null) { yield return $"{at} nivel {n + 1} sem recompensa free (nada obrigatorio so no premium)"; continue; }
                    foreach (string erro in Recompensa(r, $"{at} free {n + 1}", cos, TipoPremio.Ouro, TipoPremio.Renome, TipoPremio.Cosmetico, TipoPremio.Blueprint)) yield return erro;
                    if (r.Tipo == TipoPremio.Ouro) ouro += r.Valor;
                    if (r.Tipo == TipoPremio.Blueprint && r.Fallback?.Tipo == TipoPremio.Ouro) ouro += r.Fallback.Valor;
                }
                ouroPasse = Math.Max(ouroPasse, ouro / (double)Math.Max(1, p.Dias));
                for (int n = 0; p.Premium != null && n < p.Premium.Length; n++)
                {
                    RewardDef r = p.Premium[n];
                    if (r == null) continue;
                    if (r.Tipo != TipoPremio.Cosmetico && r.Tipo != TipoPremio.Conveniencia && r.Tipo != TipoPremio.Gems)
                        yield return $"{at} premium {n + 1} da {r.Tipo}: o pago nunca progride (so cosmetico, conveniencia e Gems; V20)";
                    else if (r.Tipo == TipoPremio.Cosmetico && !(IdOk(r.Ref, "cos_") && cos.Add(r.Ref))) yield return $"{at} premium {n + 1}: cosmetico \"{r.Ref}\" fora do formato ou repetido";
                    else if (r.Tipo == TipoPremio.Conveniencia && (r.Valor <= 0 || !IdOk(r.Ref, "conv_"))) yield return $"{at} premium {n + 1}: conveniencia sem conv_ ou sem carga";
                }
                double dia = XpDiaF2P(p, eventos, EncomendasDiaF2P);
                if (dia * (p.Dias - 7) < p.XpTotal)
                    yield return $"{at} o F2P de referencia faz {dia:0} XP/dia e nao fecha {p.XpTotal} XP em {p.Dias - 7} dias (LIVEOPS_V1 §5.4)";
            }

            var evs = new List<EventDef>(eventos);
            evs.Sort((a, b) => a.Inicio.CompareTo(b.Inicio));
            double ouroEvento = 0;
            for (int k = 0; k < evs.Count; k++)
            {
                EventDef e = evs[k];
                string at = $"evento {e.Id}:";
                if (!IdOk(e.Id, "ev_")) yield return $"{at} ID fora do formato ev_[a-z0-9_]";
                else if (!ids.Add(e.Id)) yield return $"{at} ID repetido (unico para sempre: reprise = ID novo)";
                PassDef s = passes.Length == 0 ? null : Array.Find(passes, p => p.Temporada == e.Temporada);
                if (s == null) yield return $"{at} temporada \"{e.Temporada}\" sem PassDef";
                else if (e.Inicio < s.Inicio || e.Inicio + e.Dias > s.Inicio + s.Dias) yield return $"{at} fora da janela da temporada {e.Temporada}";
                if (e.Dias < 1 || e.Dias > 7) yield return $"{at} {e.Dias} dias (1-7; o calendario usa 6 + 1 de respiro)";
                if (k > 0 && e.Inicio < evs[k - 1].Inicio + evs[k - 1].Dias) yield return $"{at} sobrepoe {evs[k - 1].Id} (um evento por vez)";
                if (e.Abre != null && Upgrades.IndexOf(e.Abre) < 0) yield return $"{at} abre com \"{e.Abre}\", que nao e' melhoria";
                if (e.Renome < 0 || e.Renome > 1 || e.Encomendas < 0 || e.Encomendas > 10) yield return $"{at} requisito fora do D0/D1 (Renome <= 1, encomendas <= 10)";
                if (e.Ofertas.Length > 0) yield return $"{at} oferta antes da Wave E (sem IAP; nunca pontos, XP, ouro, Renome nem Blueprint)";
                if (e.Reserva && Falta(e, Items.All) != null) yield return $"{at} reserva com o que ainda nao existe ({Falta(e, Items.All)}): so conteudo do distrito 1 de hoje";
                if (e.Encomenda != null && !Array.Exists(OrderDefs.All, d => d.Id == e.Encomenda && d.Type == OrderType.Event && d.EventId == e.Id))
                    yield return $"{at} encomenda \"{e.Encomenda}\" nao e' a OrderDef Event deste evento (B-CORE-05)";

                var local = new HashSet<string>();
                var especiais = new Dictionary<string, long>();
                int pontos = 0;
                if (e.Missoes == null || e.Missoes.Length == 0) yield return $"{at} sem missoes";
                foreach (MissaoEvento x in e.Missoes ?? new MissaoEvento[0])
                {
                    string am = $"{at} missao {x.Id}:";
                    if (!IdOk(x.Id, "") || x.Id.Length == 0) yield return $"{am} ID fora de [a-z0-9_]";
                    else if (!local.Add(x.Id)) yield return $"{am} ID repetido no evento";
                    if (Array.IndexOf(ContentValidator.EventosProibidos, x.Evento) >= 0) yield return $"{am} gatilho {x.Evento} pune ou exige anuncio";
                    else if (Array.IndexOf(Gatilhos, x.Evento) < 0 && Array.IndexOf(Futuros, x.Evento) < 0) yield return $"{am} gatilho {x.Evento} desconhecido";
                    if (x.Alvo <= 0 || x.Pontos <= 0) yield return $"{am} alvo {x.Alvo} e pontos {x.Pontos}";
                    pontos += x.Pontos;
                    bool semFiltro = x.Evento == "play_day" || x.Evento == "offline_claim" || x.Evento == "worker_any";
                    if (semFiltro && x.Filtro.Length > 0) yield return $"{am} {x.Evento} nao tem filtro";
                    foreach (string f in x.Filtro)   // item que ainda nao existe (armaduras da Wave C) entra no filtro junto com o ItemDef dele
                        if (Indice(x.Evento, f, Items.All) < 0) yield return $"{am} filtro \"{f}\" nao existe";
                    if (x.Evento == "order_complete" && x.Filtro.Length == 0) yield return $"{am} order_complete pede o tipo de encomenda";
                    if (x.Evento == "order_complete" && x.Filtro.Length > 0 && x.Filtro[0] != "common" && x.Filtro[0] != "event")
                        especiais[x.Filtro[0]] = (especiais.TryGetValue(x.Filtro[0], out long v) ? v : 0) + x.Alvo;
                    int vips = (int)(e.Dias * 20 * 60f / Tuning.Padrao.VipMax);
                    if (x.Evento == "vip_served" && x.Alvo > vips) yield return $"{am} {x.Alvo} VIPs nao fecham sem anuncio ({vips} naturais em {e.Dias} dias de 20 min)";
                }
                foreach (KeyValuePair<string, long> kv in especiais)
                    if (kv.Value > 2) yield return $"{at} {kv.Value} encomendas {kv.Key} (no maximo 2 de cada tipo especial: 1 a cada 10 no rodizio)";

                long xp = 0, ren = 0;
                int ant = 0;
                if (e.Trilha == null || e.Trilha.Length == 0) { yield return $"{at} sem trilha"; continue; }
                foreach (Marco m in e.Trilha)
                {
                    if (m.Pontos <= ant) yield return $"{at} limiar {m.Pontos} nao sobe do anterior ({ant})";
                    ant = m.Pontos;
                    if (m.Premios.Length == 0) yield return $"{at} limiar {m.Pontos} sem recompensa";
                    foreach (RewardDef r in m.Premios)
                        foreach (string erro in Recompensa(r, $"{at} limiar {m.Pontos}", cos, TipoPremio.Ouro, TipoPremio.PassXp, TipoPremio.Renome, TipoPremio.Cosmetico, TipoPremio.Blueprint)) yield return erro;
                }
                xp = XpTrilha(e); ren = Soma(e, TipoPremio.Renome);
                ouroEvento = Math.Max(ouroEvento, Soma(e, TipoPremio.Ouro) / 7.0);   // um evento por semana
                if (xp > 500 || ren > 10) yield return $"{at} trilha com {xp} XP e {ren} de Renome (teto 500 e 10 por evento)";
                if (pontos < 0.7 * ant) yield return $"{at} missoes somam {pontos} pontos, menos de 70% do ultimo limiar ({ant})";
                if (e.Dias >= 3 && !Array.Exists(e.Missoes ?? new MissaoEvento[0], x => x.Evento == "play_day" && x.Alvo >= 3))
                    yield return $"{at} sem a missao \"jogue em 3 dias\" (play_day >= 3): a trilha fecharia numa sessao (LIVEOPS_V1 §2.2)";

                foreach (Modificador m in e.Modificadores)
                {
                    int i = Array.FindIndex(Knobs, kn => kn.Knob == m.Knob);
                    string am = $"{at} modificador {m.Knob}:";
                    if (i < 0) { yield return $"{am} fora dos knobs de evento (LIVEOPS_V1 §2.3: offline, anuncio, recarga e custo sao proibidos)"; continue; }
                    var (_, soma, min, max) = Knobs[i];
                    if (m.Soma != soma) yield return $"{am} {(soma ? "e' de somar" : "e' de multiplicar")}";
                    else if (m.Valor < min || m.Valor > max) yield return $"{am} {m.Valor} fora da faixa {min}-{max} (sempre do lado do jogador)";
                    FieldInfo f = Campo(m.Knob);
                    if (m.Item != null && (Items.IndexOf(m.Item) < 0 || f != null && !f.FieldType.IsArray)) yield return $"{am} item \"{m.Item}\" sem array por item";
                }
            }

            // orcamento de ouro de LiveOps por dia de calendario (LIVEOPS_V1 §1): metas (como no ContentValidator.Missoes) + Pass + 1 evento por semana
            int maxD = 0, maxW = 0, login = 0;
            foreach (MetaDef d in MetasDefs.Diarias) maxD = Math.Max(maxD, d.OuroS);
            foreach (MetaDef d in MetasDefs.Semanais) maxW = Math.Max(maxW, d.OuroS);
            foreach (LoginDia l in MetasDefs.LoginDias) login += l.OuroS;
            double total = Metas.NDiarias * maxD + Metas.NSemanais * maxW / 7.0 + login / 7.0 + ouroPasse + ouroEvento;
            if (total > ContentValidator.OrcamentoDia) yield return $"liveops: ouro de LiveOps {total:0} s por dia, acima de {ContentValidator.OrcamentoDia} (LIVEOPS_V1 §1)";
        }

        /// <summary>Uma recompensa gratis: tipo permitido no lugar, valor na faixa (ouro <= 120 s e x5; Renome <= 10; XP <= 250), cosmetico
        /// numa fonte so e Blueprint com fallback de ouro.</summary>
        static IEnumerable<string> Recompensa(RewardDef r, string at, HashSet<string> cos, params TipoPremio[] permitidos)
        {
            if (Array.IndexOf(permitidos, r.Tipo) < 0) { yield return $"{at}: {r.Tipo} nao e' recompensa gratis aqui"; yield break; }
            switch (r.Tipo)
            {
                case TipoPremio.Ouro: if (r.Valor <= 0 || r.Valor > 120 || r.Valor % 5 != 0) yield return $"{at}: ouro {r.Valor} s (1-120, x5)"; break;
                case TipoPremio.Renome: if (r.Valor <= 0 || r.Valor > 10) yield return $"{at}: Renome {r.Valor} (1-10)"; break;
                case TipoPremio.PassXp: if (r.Valor <= 0 || r.Valor > 250) yield return $"{at}: XP {r.Valor} (1-250)"; break;
                case TipoPremio.Cosmetico: if (!IdOk(r.Ref, "cos_") || !cos.Add(r.Ref)) yield return $"{at}: cosmetico \"{r.Ref}\" fora do formato ou em mais de uma fonte"; break;
                case TipoPremio.Blueprint:
                    if (!IdOk(r.Ref, "bp_")) yield return $"{at}: Blueprint \"{r.Ref}\" fora do formato bp_";
                    if (r.Fallback == null || r.Fallback.Tipo != TipoPremio.Ouro || r.Fallback.Valor <= 0 || r.Fallback.Valor > 120 || r.Fallback.Valor % 5 != 0)
                        yield return $"{at}: Blueprint sem fallback de ouro (quem ja tem recebe ouro no mesmo ID)";
                    break;
            }
        }
    }
}
