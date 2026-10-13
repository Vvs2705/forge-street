namespace FS
{
    /// <summary>
    /// A-PLAT-07 (Documento Mestre s128): estado do jogo, derivado dos flags que o Game ja tem, num lugar so (Estados.De). Quem consulta:
    /// o voltar do Android (Estados.AoVoltar), o save (Estados.Grava) e o anuncio premiado (Estados.Anuncia). Sem UnityEngine: o
    /// coretests testa a tabela no dotnet.
    /// </summary>
    public enum Estado
    {
        Boot,        // Awake antes de carregar o save
        Loading,     // save carregado e HUD montada, Start ainda nao rodou (o cofre offline e' pago no Start)
        Playing,     // jogando; a bandeja de melhorias aberta continua Playing (nao trava o joystick)
        Modal,       // painel (cofre, -adtest) ou configuracoes por cima: joystick travado, a oficina segue
        Ad,          // do pedido do anuncio ate a recompensa/falha: simulacao congelada
        Paused,      // app em segundo plano (OnApplicationPause) que nao foi o anuncio
        Transition,  // encomenda entregue, cartao saindo (~0,35 s): o unico lugar do intersticial (E-PLAT-03: Anuncios.VetoIntersticial, flag desligada)
    }

    /// <summary>O que o voltar do Android faz.</summary>
    public enum Voltar { Ignora, FechaPainel, FechaConfig, FechaMenu, GravaESai }

    public static class Estados
    {
        /// <summary>Prioridade = ordem dos testes: Boot, Loading, Ad (o anuncio real tambem pausa o app), Paused, Modal, Transition, Playing.</summary>
        public static Estado De(bool carregado, bool iniciado, bool anuncio, bool pausado, bool modal, bool transicao) =>
            !carregado ? Estado.Boot : !iniciado ? Estado.Loading : anuncio ? Estado.Ad : pausado ? Estado.Paused
            : modal ? Estado.Modal : transicao ? Estado.Transition : Estado.Playing;

        /// <summary>Fecha de cima para baixo (painel, configuracoes, bandeja); so com nada aberto grava e sai. Anuncio (o simulado recebe
        /// o voltar), Boot, Loading e Paused ignoram.</summary>
        public static Voltar AoVoltar(Estado e, bool painel, bool menu) =>
            e == Estado.Modal ? (painel ? Voltar.FechaPainel : Voltar.FechaConfig)
            : e != Estado.Playing && e != Estado.Transition ? Voltar.Ignora
            : menu ? Voltar.FechaMenu : Voltar.GravaESai;

        /// <summary>Boot e Loading nao gravam: o Save avanca o SavedAt e, antes do Start, comeria o cofre offline.</summary>
        public static bool Grava(Estado e) => e != Estado.Boot && e != Estado.Loading;

        /// <summary>Anuncio premiado so jogando: nem por cima de painel, nem com outro anuncio, nem fora da frente.</summary>
        public static bool Anuncia(Estado e) => e == Estado.Playing || e == Estado.Transition;
    }
}
