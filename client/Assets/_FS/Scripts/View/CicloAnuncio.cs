using System;

namespace FS
{
    /// <summary>
    /// fix/anuncio-sem-retorno: vigia do pedido de anuncio. O LevelPlay pode nunca chamar Closed nem DisplayFailed (rede caiu, a atividade
    /// do anuncio morreu); sem isto o pedido ficava aberto e o jogo preso em Estado.Ad pelo resto da sessao (sem anuncio, voltar ignorado,
    /// cofre nunca pago na volta). Conta so o tempo com o jogo NA FRENTE (o anuncio de verdade tira o foco do app): passou do limite com
    /// o jogo visivel e sem resposta, nada esta tocando por cima e o pedido vira falha "sem_retorno". Sem UnityEngine: testado no dotnet.
    /// </summary>
    public sealed class VigiaAnuncio
    {
        public const float LimiteSeg = 90f;   // o maior anuncio (video + tela final) cabe com folga; so conta o tempo com o jogo na frente
        const float MaxPasso = 0.5f;          // a volta do segundo plano nao conta de uma vez

        float _frente;
        public bool Ativo { get; private set; }

        public void Inicia() { Ativo = true; _frente = 0f; }
        public void Para() => Ativo = false;

        /// <summary>true UMA vez, quando estourou (e para de vigiar).</summary>
        public bool Tick(float dt, bool naFrente)
        {
            if (!Ativo) return false;
            if (naFrente) _frente += Math.Min(Math.Max(dt, 0f), MaxPasso);
            if (_frente < LimiteSeg) return false;
            Ativo = false;
            return true;
        }
    }

    public static class CicloAnuncio
    {
        public const long ForaMinS = 60;            // a regra de sempre: menos que isso fora nao paga nem mostra o cofre
        public const long ForaMinNoAnuncioS = 120;  // saiu do app no meio do anuncio: depois disso foi "tempo fora", nao o anuncio

        /// <summary>Volta do segundo plano: paga o cofre? Antes, sair do app (Home) no meio de um anuncio e ficar 1 h fora perdia a hora,
        /// porque toda pausa em Estado.Ad era "o anuncio" e o autosave seguinte avancava o SavedAt.</summary>
        public static bool PagaCofreNaVolta(bool iniciado, bool pausaNoAnuncio, long foraS) =>
            iniciado && foraS >= (pausaNoAnuncio ? ForaMinNoAnuncioS : ForaMinS);
    }
}
