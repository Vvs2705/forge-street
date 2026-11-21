using System;
using FS.Core;

namespace FS
{
    /// <summary>
    /// A-ART-03 (P12, decidido por padrao em 2026-10-12): para onde a camera ortografica em retrato aponta. Conta pura, sem UnityEngine: o
    /// coretests (EnquadramentoTests) confere as 3 proporcoes. O Game so passa o ferreiro, a tela e as faixas da HUD e aplica o lerp.
    /// Largura 9 m (era 11,4 = oficina inteira + margens): o ferreiro (0,89 m de altura no Idle S) vai a 9,9% da largura da tela; e' a menor
    /// largura em que a fila de 8 vagas cabe inteira na horizontal com o ferreiro na boca da Fornalha (x 0,6), porque a camera segue o X dele
    /// presa a [meia largura - Margem, oficina - meia largura + Margem] e a borda direita (9 - 1,2 = 7,8) e' o fim da vaga 8.
    /// Vertical: a camera fica o mais alto possivel (fila e balao do 1o encostados na faixa da HUD) desde que sobre `Folga` sob o pe do
    /// ferreiro. Com o ferreiro da fileira das fornalhas para cima (y >= 5,5) a fila e o balao cabem inteiros em 9:16, 9:20 e 10:16; mais
    /// embaixo (Deposito) so o 9:20 cabe tudo, e nos outros o balao fica preso abaixo da HUD (WorldView.OnScreen) como indicador.
    /// Sempre puxa para cima, nao so com cliente na fila: so com cliente a camera subia e descia 3,2 m (9:16, ferreiro na fornalha) a
    /// cada fila vazia.
    /// Bandeja de Melhorias aberta: a camera desce ate a bandeja cobrir so y menor que `PisoBandeja` (a fileira do Deposito, o que ela ja
    /// cobria no 9:16 a 11,4 m), mas nunca esconde a cabeca do ferreiro sob a HUD.
    /// </summary>
    public static class Enquadramento
    {
        public const float Largura = 9f;              // m visiveis na largura (padrao); o -cam vai de 6 a OficinaInteira
        public const float OficinaInteira = 11.4f;    // a camera antiga: oficina (9 m) + 2 margens, fixa antes do Corredor
        public const float Margem = 1.2f;             // m alem do conteudo nas bordas: pad e rotulo nunca cortados (foto 01)
        public const float Folga = 1.5f;              // m sempre visiveis sob o pe do ferreiro (~0,5 s andando, menos o atraso do lerp)
        public const float Cabeca = 1.2f;             // m acima do pe que nunca vao para baixo da HUD (cabeca a 0,89 m + folga)
        public const float PisoBandeja = 3f;          // bandeja aberta cobre no maximo o mundo abaixo daqui (Deposito em y 1,5)
        // topo do conteudo: fila de clientes (pe em y 14) + balao grande do 1o da fila (v0.5: topo do anel em ~16,05 m) + "+10"
        public const float Topo = Balance.WorldH + 2.1f;

        /// <summary>
        /// Centro da camera no mundo. `aspecto` = largura / altura da tela; `largura` = m visiveis; `direita` = fim do mapa andavel (oficina
        /// ou, depois do Corredor, a rua); `hud`, `barra` e `bandeja` = fracao da altura da tela coberta pela faixa de cima, pela barra de
        /// Melhorias e pela barra + bandeja aberta (0 = fechada), cada uma medida da sua borda.
        /// </summary>
        public static V2 Alvo(V2 ferreiro, float aspecto, float largura, float direita, float hud, float barra, float bandeja = 0f)
        {
            float meiaL = largura / 2f, meiaA = meiaL / aspecto, alt = 2f * meiaA;
            float minX = meiaL - Margem, maxX = direita - meiaL + Margem;
            float x = minX > maxX ? direita / 2f : Math.Max(minX, Math.Min(ferreiro.X, maxX));
            float baixo = meiaA - alt * barra, cima = meiaA - alt * hud;   // do centro ate a base / o topo da area de jogo
            float minY = baixo - Margem, maxY = Topo - cima;                  // sem vazio alem da Margem embaixo nem acima da fila
            float y = Math.Min(maxY, ferreiro.Y - Folga + baixo);
            if (bandeja > 0f) y = Math.Min(y, PisoBandeja + meiaA - alt * bandeja);
            y = Math.Max(y, ferreiro.Y + Cabeca - cima);
            y = minY > maxY ? (minY + maxY) / 2f : Math.Max(minY, Math.Min(y, maxY));
            return new V2(x, y);
        }
    }
}
