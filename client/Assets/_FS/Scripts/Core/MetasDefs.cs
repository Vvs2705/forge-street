using System;

namespace FS.Core
{
    public enum TipoMeta { Diaria, Semanal, Conquista }

    /// <summary>Como o alvo conta: Unidades = o numero do dado; Segundos = s de renda, congelados em ouro (x RateEma) quando a missao aparece
    /// (LIVEOPS_V1 §4.1); Distintos = quantos produtos diferentes ja tiveram pelo menos 1 venda (so conquista, sem base de periodo).</summary>
    public enum Conta { Unidades, Segundos, Distintos }

    /// <summary>
    /// D-CORE-02: missao diaria, semanal ou conquista em dados (LIVEOPS_V1 §4.2-§4.5, §5.2, §5.5). `Evento` = nome da taxonomia (Metas.Eventos:
    /// DM §86 + diario + os 2 do D-CORE-02); `Filtro` = item (product_*, order_done, vip_served), upgrade (upgrade_buy) ou tipo (mission_complete:
    /// 0 diaria, 1 semanal); -1 = qualquer. `Abre` = ID do upgrade que libera a missao no rodizio (null = sempre). Recompensa: ouro em s de renda
    /// (fora do GoldEarned), XP do Pass e Renome guardados como contadores ate esses sistemas existirem (V9/P1), insignia = posse pelo ledger.
    /// </summary>
    public sealed class MetaDef
    {
        /// <summary>Chave = contador do Metas.Cont; Tx = ID de transacao da conquista (a:{id}; missao usa o do periodo). Ref = filtro por ID
        /// (raia H): item de outro distrito (product_sold.elmo, C-CORE-03), distrito do Charter ou marco da Collection; com Ref, Filtro = -1.</summary>
        public readonly string Id, Evento, Abre, Insignia, Chave, Tx, Ref;
        public readonly TipoMeta Tipo;
        public readonly int Pool, Filtro, OuroS, Xp, Renome;
        public readonly long Alvo;
        public readonly Conta Conta;

        public MetaDef(string id, TipoMeta tipo, int pool, string evento, int filtro, long alvo, string abre, int ouroS, int xp, int renome,
            Conta conta = Conta.Unidades, string insignia = null, string refId = null)
        {
            Id = id; Tipo = tipo; Pool = pool; Evento = evento; Filtro = filtro; Alvo = alvo; Abre = abre;
            OuroS = ouroS; Xp = xp; Renome = renome; Conta = conta; Insignia = insignia; Ref = refId;
            Chave = refId != null ? evento + "." + refId : Metas.Chave(evento, filtro);
            Tx = tipo == TipoMeta.Conquista ? "a:" + id : null;
        }
    }

    /// <summary>Recompensa do dia `Dia` (1..7) do ciclo de login (LIVEOPS_V1 §4.4).</summary>
    public sealed class LoginDia
    {
        public readonly int OuroS, Renome;
        public LoginDia(int ouroS, int renome) { OuroS = ouroS; Renome = renome; }
    }

    /// <summary>
    /// D-CORE-02: os modelos. So entram os que tem gatilho que o Sim (ou o Metas) emite HOJE; os do LIVEOPS_V1 com evento ✗ entram com o
    /// sistema deles (raia H: contrato, Charter, Collection, projetos, Renome, nivel de estacao, qualidade, golpe, evento e Passe ja entraram;
    /// esperam: a diaria do pool C, o golpe que faz Superior, e ach_workers20). Os extras sobre eventos que ja existem
    /// (marcados "novo") completam os minimos do GDD §23 #12.
    /// Renome ainda nao existe: "abre com Renome 1" usa a Joalheria (12-25 min ~ 23 min no F2P) e "Renome 2" a Vitrine de joias, ate o B-CORE-06.
    /// </summary>
    public static class MetasDefs
    {
        const string Ferr = "ferramentas", Joal = "joalheria", Corr = "corredor", Ren2 = "vitrine_joias";
        const int A = 0, B = 1;
        const string Sold = "product_sold", Craft = "product_crafted", Ord = "order_done", Buy = "upgrade_buy", Src = "currency_source",
            Sink = "currency_sink", Cofre = "offline_claim", Vip = "vip_served", Hire = "worker_hired", Bau = "chest_opened",
            Missao = "mission_complete", Login = "login_claim", Nivel = "worker_upgrade",
            Tipo = "order_complete", Premium = "order_premium", Rush = "order_rush_prazo", Mw = "masterwork_unlock", MwFamilia = "masterwork_family",
            // metas de sistemas (raia H): contratos (#64), Charter (#61), Collection (#63), projetos (#55), Renome, niveis de estacao, qualidade e golpe
            // (B-CORE-03), eventos e Passe (D-CORE-03/04)
            Ctr = "contract_complete", Charter = "district_unlock", Col = "collection_milestone", Bp = "blueprint_unlock", Ren = "reputation_level",
            Est = "station_level", Fina = "quality_crafted", Sup = "quality_superior", Golpe = "master_strike", EvFim = "event_complete",
            EvEntra = "event_enter", Passe = "pass_level";
        /// <summary>B-CORE-04: os workers do Old Forge (6 ajudantes + 7 da Equipe, LIVEOPS_V1 §5.5 ach_workers13).</summary>
        static readonly int WorkersOldForge = Array.FindAll(WorkerDefs.All, d => d.Distrito == WorkerDefs.OldForge).Length;
        const int Lingote = (int)Item.Ingot, Espada = (int)Item.Sword, Escudo = (int)Item.Shield, Ferram = (int)Item.Tool, Joia = (int)Item.Jewel;

        /// <summary>Diaria padrao: XP 100 + ouro 20 s (LIVEOPS_V1 §4.1).</summary>
        static MetaDef D(string id, int pool, string ev, int filtro, long alvo, string abre, Conta c = Conta.Unidades) =>
            new MetaDef(id, TipoMeta.Diaria, pool, ev, filtro, alvo, abre, 20, 100, 0, c);

        /// <summary>Semanal padrao: XP 350 + ouro 60 s + Renome 5.</summary>
        static MetaDef W(string id, string ev, int filtro, long alvo, string abre, Conta c = Conta.Unidades) =>
            new MetaDef(id, TipoMeta.Semanal, -1, ev, filtro, alvo, abre, 60, 350, 5, c);

        static MetaDef C(string id, string ev, int filtro, long alvo, int ouroS = 0, int xp = 0, int renome = 0, string insignia = null, Conta c = Conta.Unidades,
            string refId = null) =>
            new MetaDef(id, TipoMeta.Conquista, -1, ev, filtro, alvo, null, ouroS, xp, renome, c, insignia, refId);

        /// <summary>Pools A (volume) e B (gestao); a C (qualidade/Renome/evento) ainda nao tem gatilho e cai na A (LIVEOPS_V1 §4.1).</summary>
        public static readonly MetaDef[] Diarias =
        {
            D("dly_vende50", A, Sold, -1, 50, Ferr),
            D("dly_espadas30", A, Sold, Espada, 30, Ferr),
            D("dly_escudos20", A, Sold, Escudo, 20, Ferr),
            D("dly_ferram20", A, Sold, Ferram, 20, Ferr),
            D("dly_joias10", A, Sold, Joia, 10, Joal),
            D("dly_lingotes100", A, Craft, Lingote, 100, Ferr),
            D("dly_forja_espadas40", A, Craft, Espada, 40, Ferr),
            D("dly_forja_escudos20", A, Craft, Escudo, 20, Ferr),
            D("dly_forja_ferram60", A, Craft, Ferram, 60, Ferr),
            D("dly_forja_joias10", A, Craft, Joia, 10, Joal),
            D("dly_vende150", A, Sold, -1, 150, Joal),
            D("dly_vende300", A, Sold, -1, 300, Ren2),
            D("dly_renda5min", A, Src, -1, 300, Ferr, Conta.Segundos),
            D("dly_vende100", A, Sold, -1, 100, Corr),                 // novo
            D("dly_espadas60", A, Sold, Espada, 60, Corr),             // novo
            D("dly_ferram40", A, Sold, Ferram, 40, Corr),              // novo
            D("dly_lingotes200", A, Craft, Lingote, 200, Joal),        // novo
            D("dly_forja_espadas100", A, Craft, Espada, 100, Joal),    // novo
            D("dly_renda10min", A, Src, -1, 600, Joal, Conta.Segundos), // novo
            D("dly_enc2", B, Ord, -1, 2, Ferr),
            D("dly_enc3", B, Ord, -1, 3, Joal),
            D("dly_melhoria1", B, Buy, -1, 1, Ferr),
            D("dly_melhoria2", B, Buy, -1, 2, Ferr),
            D("dly_gasta10min", B, Sink, -1, 600, Ferr, Conta.Segundos),
            D("dly_cofre", B, Cofre, -1, 1, Ferr),
            D("dly_vip1", B, Vip, -1, 1, Ferr),                        // natural ou chamado: o caminho sem anuncio fecha no dia (validator)
            D("dly_vip2", B, Vip, -1, 2, Joal),
            D("dly_contrata", B, Hire, -1, 1, Ferr),
            D("dly_enc4", B, Ord, -1, 4, Ren2),                        // novo
            D("dly_melhoria3", B, Buy, -1, 3, Corr),                   // novo
            D("dly_cofre2", B, Cofre, -1, 2, Corr),                    // novo: as 2 sessoes do F2P de referencia
            D("dly_gasta5min", B, Sink, -1, 300, Ferr, Conta.Segundos), // novo
            D("dly_worker_nivel", B, Nivel, -1, 1, Ferr),              // B-CORE-04 (LIVEOPS_V1 §4.2 #24)
        };

        public static readonly MetaDef[] Semanais =
        {
            W("wkl_enc25", Ord, -1, 25, Ferr),
            W("wkl_vende2000", Sold, -1, 2000, Ferr),
            W("wkl_lingote1500", Craft, Lingote, 1500, Ferr),
            W("wkl_melhoria10", Buy, -1, 10, Ferr),
            W("wkl_vip15", Vip, -1, 15, Ferr),
            W("wkl_cofre5", Cofre, -1, 5, Ferr),
            W("wkl_joias100", Sold, Joia, 100, Joal),
            W("wkl_diarias15", Missao, 0, 15, Ferr),
            W("wkl_espadas500", Sold, Espada, 500, Ferr),              // novo
            W("wkl_escudos300", Sold, Escudo, 300, Ferr),              // novo
            W("wkl_ferram300", Sold, Ferram, 300, Ferr),               // novo
            W("wkl_renda1h", Src, -1, 3600, Ferr, Conta.Segundos),     // novo
            W("wkl_forja_joias100", Craft, Joia, 100, Joal),           // novo
            W("wkl_gasta30min", Sink, -1, 1800, Ferr, Conta.Segundos), // novo
            // B-CORE-05: os de tipo de encomenda (LIVEOPS_V1 §4.3 #9-11); so entram no rodizio com o tipo aberto (Metas.Livre)
            W("wkl_enc_premium5", Premium, -1, 5, Ferr),
            W("wkl_bulk3", Tipo, (int)OrderType.Bulk, 3, Ferr),
            W("wkl_royal2", Tipo, (int)OrderType.Royal, 2, Ferr),
            // metas de sistemas (raia H, LIVEOPS_V1 §4.3 #12-14): so entram no rodizio com um contrato a cumprir / o tipo Quality aberto (Metas.Livre)
            W("wkl_contrato1", Ctr, -1, 1, Ferr),
            W("wkl_superior20", Sup, -1, 20, Ferr),
            W("wkl_golpe50", Golpe, -1, 50, Ferr),
        };

        public static readonly MetaDef[] Conquistas =
        {
            // Crafting (vendas e fabricacao)
            C("ach_primeira_forja", Craft, Espada, 1, ouroS: 30),
            C("ach_primeira_venda", Sold, -1, 1, ouroS: 30),
            C("ach_vende100", Sold, -1, 100, xp: 50),
            C("ach_vende1k", Sold, -1, 1000, xp: 100, renome: 5),
            C("ach_vende10k", Sold, -1, 10000, xp: 200, renome: 10),
            C("ach_vende100k", Sold, -1, 100000, insignia: "mao_de_ouro"),
            C("ach_espadas500", Craft, Espada, 500, xp: 100),
            C("ach_escudos500", Craft, Escudo, 500, xp: 100),
            C("ach_ferram500", Craft, Ferram, 500, xp: 100),
            C("ach_joias200", Craft, Joia, 200, xp: 100),
            C("ach_lingotes5k", Craft, Lingote, 5000, xp: 100, renome: 5),
            C("ach_quatro_linhas", Sold, -1, 4, renome: 5, c: Conta.Distintos),
            C("ach_vende500", Sold, -1, 500, xp: 50),                  // novo
            C("ach_vende50k", Sold, -1, 50000, xp: 200),               // novo
            C("ach_primeiro_escudo", Craft, Escudo, 1, ouroS: 30),     // novo
            C("ach_primeira_ferramenta", Craft, Ferram, 1, ouroS: 30), // novo
            C("ach_primeira_joia", Craft, Joia, 1, ouroS: 30),         // novo
            C("ach_lingotes500", Craft, Lingote, 500, xp: 50),         // novo
            C("ach_lingotes50k", Craft, Lingote, 50000, xp: 150),      // novo
            C("ach_espadas5k", Craft, Espada, 5000, xp: 150),          // novo
            C("ach_espadas_venda100", Sold, Espada, 100, xp: 50),      // novo
            C("ach_espadas_venda1k", Sold, Espada, 1000, xp: 100),     // novo
            C("ach_espadas_venda10k", Sold, Espada, 10000, xp: 150),   // novo
            C("ach_escudos_venda100", Sold, Escudo, 100, xp: 50),      // novo
            C("ach_escudos_venda1k", Sold, Escudo, 1000, xp: 100),     // novo
            C("ach_ferram_venda100", Sold, Ferram, 100, xp: 50),       // novo
            C("ach_ferram_venda1k", Sold, Ferram, 1000, xp: 100),      // novo
            C("ach_joias_venda100", Sold, Joia, 100, xp: 50),          // novo
            C("ach_joias_venda1k", Sold, Joia, 1000, xp: 150),         // novo
            // Orders
            C("ach_enc1", Ord, -1, 1, ouroS: 30),
            C("ach_enc10", Ord, -1, 10, xp: 50),                       // novo
            C("ach_enc50", Ord, -1, 50, xp: 100),
            C("ach_enc250", Ord, -1, 250, xp: 150, renome: 5),
            C("ach_enc500", Ord, -1, 500, xp: 150),                    // novo
            C("ach_enc1000", Ord, -1, 1000, xp: 200, insignia: "livro_caixa"),
            // B-CORE-05 (LIVEOPS_V1 §5.5 #35-38)
            C("ach_enc_royal1", Tipo, (int)OrderType.Royal, 1, xp: 100),
            C("ach_enc_tipos7", Tipo, -1, 7, xp: 150, renome: 10, c: Conta.Distintos),
            C("ach_rush_prazo10", Rush, -1, 10, xp: 100),
            C("ach_bulk25", Tipo, (int)OrderType.Bulk, 25, xp: 100),
            // Workers
            C("ach_ajudante1", Hire, -1, 1, ouroS: 30),
            C("ach_ajudante3", Hire, -1, 3, xp: 50),                   // novo
            C("ach_ajudantes6", Hire, -1, Metas.AjudantesOldForge, xp: 100, renome: 5),
            // B-CORE-04 (LIVEOPS_V1 §5.5 #41-44): filtro de worker_hired = papel; de worker_upgrade = nivel alcancado (cada worker chega 1x)
            C("ach_oficial1", Hire, (int)WorkerRole.Smith, 1, xp: 50),
            C("ach_worker_n5", Nivel, 5, 1, xp: 100),
            C("ach_workers13", Hire, -1, WorkersOldForge, xp: 150, renome: 10),
            C("ach_workers13_n5", Nivel, 5, WorkersOldForge, insignia: "mestre_oficio"),
            // B-CORE-08 (LIVEOPS_V1 §4.5 #20-25): Masterworks registradas no Hall. O Renome ja vem da obra (Hall): a conquista da so XP ou insignia.
            // ach_mw30/60 contam as do Imperio (masterwork_empire; requiresContent: o validator recusa alvo acima das do build)
            C("ach_mw1", Mw, -1, 1, xp: 100),
            C("ach_mw5", Mw, -1, 5, xp: 150),
            C("ach_mw10_oldforge", Mw, -1, 10, xp: 200, insignia: "hall_completo"),
            C("ach_mw30", "masterwork_empire", -1, 30, xp: 200),   // LIVEOPS_V1 #26 (la, +Renome 10: aqui so XP, a obra ja deu o Renome)
            C("ach_mw60", "masterwork_empire", -1, 60, insignia: "lenda_da_forja"),   // LIVEOPS_V1 #27
            C("ach_familia_espada", MwFamilia, Espada, 3, xp: 100),
            C("ach_sunblade", Mw, MasterworksDefs.IndexOf("mw_sunblade"), 1, insignia: "sol_do_rei"),
            C("ach_ember_crown", Mw, MasterworksDefs.IndexOf("mw_ember_crown"), 1, insignia: "coroa_de_brasa"),
            // Automation (melhorias)
            C("ach_melhoria1", Buy, -1, 1, ouroS: 30),                 // novo
            C("ach_melhorias10", Buy, -1, 10, xp: 50),                 // novo
            C("ach_melhorias20", Buy, -1, 20, xp: 100),                // novo
            C("ach_esteira", Buy, (int)Upgrade.Conveyor, 1, ouroS: 45),
            C("ach_corredor", Buy, (int)Upgrade.SideCorridor, 1, xp: 50),      // novo
            C("ach_joalheria", Buy, (int)Upgrade.Jewelry, 1, xp: 50),
            C("ach_vitrine", Buy, (int)Upgrade.CounterCapacity, 1, xp: 50),    // novo
            C("ach_fornalha2", Buy, (int)Upgrade.Furnace2, 1, xp: 50),         // novo
            C("ach_mineiro", Buy, (int)Upgrade.Miner, 1, xp: 50),
            C("ach_balcao8", Buy, (int)Upgrade.Counter8, 1, xp: 50),
            // luxo so com toda a producao (Sim.Buy): sem ela so cabem os niveis de estacao (Meta, B-CORE-03), entao ProductionCount + Meta
            // compras = producao completa (nunca antes; quem pula um nivel fecha na 1a compra de luxo). B-CORE-07: so os Meta sem projeto (II-III);
            // com projeto (bigorna longa pelo marco de 300 espadas) ela pode fechar 1 compra antes
            C("ach_producao_completa", Buy, -1, Upgrades.ProductionCount + Array.FindAll(Upgrades.All, u => u.Meta && u.Blueprint == null).Length, xp: 200, renome: 10),
            C("ach_luxo1", Buy, (int)Upgrade.WorkshopFacade, 1, xp: 50),
            C("ach_luxo3", Buy, (int)Upgrade.JewelryDecor, 1, insignia: "oficina_nobre"),
            // Economy
            C("ach_ouro10k", Src, -1, 10000, xp: 50),
            C("ach_ouro100k", Src, -1, 100000, xp: 100),               // novo
            C("ach_ouro1m", Src, -1, 1000000, xp: 200, renome: 10),
            C("ach_ouro10m", Src, -1, 10000000, xp: 200),              // novo
            C("ach_gasto1k", Sink, -1, 1000, ouroS: 30),               // novo
            C("ach_gasto50k", Sink, -1, 50000, xp: 100),
            C("ach_gasto500k", Sink, -1, 500000, xp: 150),             // novo
            C("ach_cofre1", Cofre, -1, 1, ouroS: 30),                  // novo
            C("ach_cofre30", Cofre, -1, 30, xp: 100),
            C("ach_cofre100", Cofre, -1, 100, xp: 150),                // novo
            C("ach_bau1", Bau, -1, 1, ouroS: 30),                      // novo
            C("ach_baus4", Bau, -1, 4, xp: 50),
            C("ach_vip1", Vip, -1, 1, ouroS: 30),                      // novo
            C("ach_vip10", Vip, -1, 10, xp: 50),
            C("ach_vip50", Vip, -1, 50, xp: 100),                      // novo
            C("ach_vip100", Vip, -1, 100, xp: 150, renome: 5),
            // LiveOps
            C("ach_login7", Login, -1, 7, xp: 50),
            C("ach_login30", Login, -1, 30, xp: 100),                  // novo
            C("ach_diarias1", Missao, 0, 1, ouroS: 30),                // novo
            C("ach_diarias30", Missao, 0, 30, xp: 100),
            C("ach_diarias100", Missao, 0, 100, xp: 150),              // novo
            C("ach_semanais1", Missao, 1, 1, xp: 50),                  // novo
            C("ach_semanais10", Missao, 1, 10, xp: 150),
            C("ach_semanais50", Missao, 1, 50, xp: 200),               // novo
            // metas de sistemas (raia H, LIVEOPS_V1 §4.5): os gatilhos que esperavam um sistema que ja existe. Conquista de Renome nunca da
            // Renome; a de Charter, Collection e Masterwork da so XP ou insignia (a fonte ja deu o Renome). Districts:
            C("ach_charter1", Charter, -1, 1, xp: 200, refId: Distritos.OldForgeId),   // a insignia e' do proprio Charter (GDD §11)
            C("ach_distritos3", Charter, -1, 3, xp: 200),   // LIVEOPS_V1 #55 (la, +Renome 10: aqui so XP, o Charter ja deu o Renome)
            C("ach_distritos6", Charter, -1, 6, insignia: "senhor_das_ruas"),   // LIVEOPS_V1 #56
            C("ach_elmos100", Sold, -1, 100, xp: 100, refId: "elmo"),                   // a venda da Rua dos Armeiros chega pela raiz (C-CORE-03)
            C("ach_contrato1", Ctr, -1, 1, xp: 150),
            C("ach_contratos12", Ctr, -1, 12, insignia: "arsenal_do_reino"),            // 9 dos 12 abrem com os distritos 3-6 (Wave C)
            // Quality (B-CORE-03): Fina+ / Superior+ fabricada (Ev.Graded) e golpe de mestre acertado (Sim.Strike)
            C("ach_primeira_fina", Fina, -1, 1, ouroS: 30),
            C("ach_primeira_superior", Sup, -1, 1, xp: 50, renome: 3),
            C("ach_fina100", Fina, -1, 100, xp: 100),
            C("ach_superior1k", Sup, -1, 1000, insignia: "olho_de_mestre"),
            C("ach_golpe1", Golpe, -1, 1, ouroS: 30),
            C("ach_golpe100", Golpe, -1, 100, xp: 100, renome: 5),
            // Blueprints (B-CORE-07), niveis de estacao (filtro = nivel), Renome (nivel) e Collection (marco por ID)
            C("ach_bp1", Bp, -1, 1, ouroS: 30),
            C("ach_bp10", Bp, -1, 10, xp: 100, renome: 5),
            C("ach_bp18", Bp, -1, 18, xp: 200, insignia: "arquivista"),
            C("ach_estacao_n3", Est, 3, 1, xp: 50),
            C("ach_estacoes_n5", Est, 5, Estacoes(5), xp: 200, insignia: "oficina_de_ferro"),
            C("ach_renome1", Ren, -1, 1, xp: 50),
            C("ach_renome4", Ren, -1, 4, insignia: "fornecedor_do_reino"),
            C("ach_colecao_produtos", Col, -1, 1, xp: 100, refId: "col_produtos_old_forge_100"),
            // LiveOps (D-CORE-03/04): trilha de evento fechada, eventos em que entrou, trilha free do Passe fechada (o nivel 30 resgatado)
            C("ach_evento1", EvFim, -1, 1, xp: 100),
            C("ach_eventos4", EvEntra, -1, 4, xp: 150),
            C("ach_passe_free", Passe, -1, 1, insignia: "veterano_de_temporada"),
        };

        /// <summary>Raia H: estacoes (familias) do Old Forge com nivel `nivel` a venda (o alvo de "todas no nivel 5").</summary>
        public static int Estacoes(int nivel)
        {
            var f = new System.Collections.Generic.HashSet<string>();
            foreach (UpgradeDef u in Upgrades.All) if (u.Station != null && Upgrades.StationLevel((int)u.Up) >= nivel) f.Add(u.Station);
            return f.Count;
        }

        /// <summary>Login de 7 dias (LIVEOPS_V1 §4.4). Dia 7: a decoracao do ciclo (cos_login_*) e' posse pelo ledger (l:{ciclo}:7) e entra quando
        /// houver cosmetico; ate la ele paga o ouro de 60 s que o desenho ja previa do ciclo 5 em diante (nada prometido que nao aparece).</summary>
        public static readonly LoginDia[] LoginDias =
        {
            new LoginDia(20, 0), new LoginDia(0, 2), new LoginDia(30, 0), new LoginDia(0, 2), new LoginDia(40, 0), new LoginDia(0, 3), new LoginDia(60, 0),
        };
        public static readonly string[] LoginCosmeticos = { "cos_login_sino", "cos_login_placa", "cos_login_barril", "cos_login_lanterna" };

        public static MetaDef Achar(string id) => Array.Find(Diarias, d => d.Id == id) ?? Array.Find(Semanais, d => d.Id == id) ?? Array.Find(Conquistas, d => d.Id == id);
    }
}
