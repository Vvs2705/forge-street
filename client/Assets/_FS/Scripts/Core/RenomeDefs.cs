using System;

namespace FS.Core
{
    /// <summary>
    /// B-CORE-06: um nivel de Renome em dados (ECONOMIA_WAVE_B §8.7 `ReputationTierDef`). Nome pelo Loc (`renome.N`, e o que libera em texto
    /// `renome.N.libera`); `Libera` = IDs de desbloqueio que os outros sistemas consultam (Renome.Libera): ID do *Def quando ele existe
    /// (wk_, ord_, cli_, bp_, ctr_) e um ID estavel quando ainda nao ha Def (vaga_, hall_, heroi_, charter_). Nunca renomear um ID publicado.
    /// </summary>
    public sealed class RenomeDef
    {
        public readonly int Nivel;
        public readonly long Limiar;
        public readonly string[] Libera;
        public string NomeKey => "renome." + Nivel;

        public RenomeDef(int nivel, long limiar, params string[] libera) { Nivel = nivel; Limiar = limiar; Libera = libera; }
    }

    /// <summary>
    /// B-CORE-06 (CONTEUDO_V1 §11, ECONOMIA_WAVE_B §5): os 10 niveis (mais o 0, Aprendiz) e o Renome de cada fonte ativa. Niveis 0-4 no Old
    /// Forge (limiares medidos para o bot F2P: §10), 5-10 nos distritos 2-6 (modelo; calibrar no econsim quando os DistrictDef existirem).
    /// </summary>
    public static class RenomeDefs
    {
        /// <summary>Encomenda entregue (a de hoje e' sempre Common). Orders 2.0 (B-CORE-05): o Renome vem do OrderDef (§3.1: +1 a +10).</summary>
        public const int Encomenda = 1;
        /// <summary>Bau de marco aberto.</summary>
        public const int Bau = 5;
        /// <summary>+1 a cada tantas pecas Superiores vendidas (Renome.OnVendaQualidade).</summary>
        public const int SuperiorACada = 10;
        /// <summary>Royal Charter de um distrito (Wave C), 1x por distrito.</summary>
        public const int Charter = 100;

        public static readonly RenomeDef[] Niveis =
        {
            new RenomeDef(0, 0),
            new RenomeDef(1, 20, "vaga_encomenda_2", "ord_blueprint_old_forge", "wk_baltazar"),
            new RenomeDef(2, 70, "ord_royal_old_forge", "wk_otavio", "cli_colecionador"),
            new RenomeDef(3, 180, "vaga_encomenda_3", "ord_quality_old_forge_superior", "hall_rua", "heroi_estagio_2", "bp_mw_escudo_brasao",
                "bp_var_escudo_brasao", "ctr_ronda_vila"),
            new RenomeDef(4, 400, "charter_old_forge", "heroi_estagio_3"),
            new RenomeDef(5, 750, "charter_armorers_row", "ctr_milicia_norte", "ord_quality_armorers_row_superior", "bp_mw_elmo_liga", "heroi_estagio_4"),
            new RenomeDef(6, 1250, "charter_artisan_quarter", "ctr_dote_princesa", "ord_quality_artisan_quarter_superior", "bp_mw_broche_guilda"),
            new RenomeDef(7, 2000, "charter_royal_arsenal", "ctr_coroacao", "bp_mw_rainha_guerreira", "heroi_estagio_5"),
            new RenomeDef(8, 3000, "charter_arcane_foundry", "ctr_vanguarda", "ord_quality_arcane_foundry_superior", "bp_mw_escudo_aurora"),
            new RenomeDef(9, 4500, "charter_grand_forge", "ctr_campeao", "ord_royal_grand_forge_superior", "bp_mw_rainha_forjadora"),
            new RenomeDef(10, 6500, "bp_mw_coroa_granforja", "estatua_ferreiro_praca", "titulo_lenda"),
        };

        /// <summary>Nivel que libera `id` (-1 = nenhum nivel libera).</summary>
        public static int NivelQueLibera(string id) => Array.FindIndex(Niveis, d => Array.IndexOf(d.Libera, id) >= 0);
    }
}
