using System;

namespace FS
{
    /// <summary>fix/ui-fechar-ao-tocar-fora: o que um gesto que comecou fora do painel do topo foi, quando o dedo sai.</summary>
    public enum Gesto { Nenhum, Toque, Arraste }

    /// <summary>
    /// Toque fora do painel do topo fecha (pedido do Vinicius, 2026-11-21). Um so mecanismo para todos os paineis: o veu dos modais
    /// (VeuFecha, em Metas, Projetos, Hall, LiveOps, Loja, Renome, configuracoes e o painel do Game) e a bandeja de melhorias (Game).
    /// TAP curto e parado fecha; arrastar (joystick, rolagem) nao. Sem UnityEngine: o coretests testa no dotnet.
    /// </summary>
    public sealed class ToqueFora
    {
        public const float RaioDp = 12f;    // ~ o drag threshold do EventSystem em dp: passou disso o dedo esta arrastando
        public const float MaxSeg = 0.45f;  // segurar mais que isso e' pressionar, nao tocar

        /// <summary>Gancho generico de excecao (tutorial, cartao de boas-vindas, o que vier): enquanto devolver true, nenhum toque fora fecha
        /// nada. Quem registra soma o seu predicado (Bloqueios += ...) e tira ao sair; nao depende do nome de quem bloqueia.</summary>
        public static event Func<bool> Bloqueios;

        public static bool Bloqueado
        {
            get
            {
                if (Bloqueios == null) return false;
                foreach (Func<bool> b in Bloqueios.GetInvocationList()) if (b()) return true;
                return false;
            }
        }

        float _x, _y, _t;
        bool _arrastou;

        /// <summary>Ha um gesto que comecou fora e ainda nao terminou (o toque esta reservado para fechar, nao age no mundo).</summary>
        public bool Ativo { get; private set; }

        public void Aperta(float x, float y, float t)
        {
            Ativo = true; _arrastou = false;
            _x = x; _y = y; _t = t;
        }

        /// <summary>Posicao do dedo durante o gesto: passou do raio uma vez, ja e' arraste (ir e voltar ao ponto nao vira toque).</summary>
        public void Move(float x, float y, float raioPx)
        {
            if (!Ativo || _arrastou) return;
            float dx = x - _x, dy = y - _y;
            if (dx * dx + dy * dy > raioPx * raioPx) _arrastou = true;
        }

        public Gesto Solta(float x, float y, float t, float raioPx)
        {
            if (!Ativo) return Gesto.Nenhum;
            Move(x, y, raioPx);
            Ativo = false;
            return !_arrastou && t - _t <= MaxSeg ? Gesto.Toque : Gesto.Arraste;
        }

        public void Cancela() => Ativo = false;

        /// <summary>Bandeja de melhorias aberta: TAP no mundo a fecha e o toque nao age no mundo. Nao fecha por baixo de modal (o veu do
        /// modal responde), fora do jogo (anuncio, pausa, boot), no passo do tutorial que manda comprar pela bandeja (dica BuyMenu antes da
        /// 1a venda) nem com um bloqueio registrado.</summary>
        public static bool FechaMenu(Estado e, bool menu, bool tutorialNoMenu) =>
            menu && !tutorialNoMenu && (e == Estado.Playing || e == Estado.Transition) && !Bloqueado;

        /// <summary>Painel do Game (cofre, bancada, altar, mapa, -adtest): so o informativo (o botao so fecha) sem ouro fecha pelo toque fora;
        /// escolha (receita, modo do altar, acao do mapa) e recompensa pendente (cofre) so pelo botao, para nao perder a escolha.</summary>
        public static bool FechaPainel(bool informativo, long ouro) => informativo && ouro <= 0 && !Bloqueado;
    }
}
