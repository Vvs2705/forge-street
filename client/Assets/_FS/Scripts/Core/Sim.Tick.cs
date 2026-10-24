using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Sim em partes (o estado e o construtor ficam no Sim.cs): o passo da simulacao (jogador, ajudantes, trocas, producao, clientes, metricas).</summary>
    public sealed partial class Sim
    {
        // ------------------------------------------------------------------ tick

        public void Tick(float dt, float inX, float inY)
        {
            Events.Clear();
            if (dt <= 0f) return;
            Time += dt;
            long earnedBefore = GoldEarned;
            // FASE8: o boost acelera estacoes, esteira, ajudantes e a chegada de clientes; o ferreiro (andar e maos) no maximo 1,3x, para o
            // joystick nao fugir do controle. Paciencia, atendimento, relogio do VIP e o proprio boost correm no tempo de jogo. Sem boost, fast = hand = dt
            float fast = dt * BoostMul, hand = dt * Math.Min(BoostMul, T.BoostPlayerMax);

            MovePlayer(hand, inX, inY);
            foreach (Carrier w in Workers) WorkerThink(w, fast);
            Interact(Player, hand);
            foreach (Carrier w in Workers) Interact(w, fast);
            foreach (Station s in Stations) Produce(s, fast);
            Conveyor(fast);
            TickArcano(fast);   // C-CORE-08: dreno e sobrecarga dos altares, no tempo das estacoes (Sim.Arcano.cs; sem altar, nada)
            TickCotas(dt);   // C-CORE-07: prazo dos blocos do patio de cotas (Sim.Cotas.cs; sem patio, nada)
            TickRotas(dt);   // C-CORE-09: rotas e lote do Mensageiro (Sim.Exportacao.cs; sem cais nem patio, nada)
            Anuncios.Tick(dt);   // E-PLAT-02: recargas e o Supply Drop entrando nas fornalhas
            VipCooldown = Math.Max(0f, VipCooldown - dt);   // P14: no tempo de jogo; o Serve recarrega quando o chamado sai
            StrikeCooldown = Math.Max(0f, StrikeCooldown - dt);   // B-CORE-03: golpe de mestre, no tempo de jogo (o boost nao encurta)
            Clients(dt);
            Milestones();
            Projetos.Checa();   // B-CORE-07: marcos, Renome e conquista viram projeto (Ev.Blueprint)
            Colecao.Checa();    // B-CORE-08: marcos da colecao (Ev.Colecao), 1x pelo ID
            Encomendas.Tick(dt);   // B-CORE-05
            Contratos.Tick(dt, this);   // D-CORE-04: prazo-bonus e a conclusao (os da raiz, no tempo de jogo de qualquer distrito)
            if (BoostLeft > 0f) { if ((BoostLeft -= dt) <= 0f) { BoostLeft = 0f; BoostMul = 1f; BoostCooldown = T.Ad(Anuncio.Velocidade).Recarga; } }   // recarga conta do fim
            else BoostCooldown = Math.Max(0f, BoostCooldown - dt);

            long cheapest = CheapestVisibleCost();
            // kill criterion do GDD §26 ("andar entre pilhas sem decisao"): anda de maos vazias sem nenhum pad pagavel = puro deslocamento
            if (Player.Moving && Player.Count == 0 && (cheapest < 0 || Gold < cheapest)) WalkNoDecision += dt;
            // FASE8: ouro por segundo de FABRICA (dt x boost): a taxa do cofre offline e do pacote do VIP nao aprende o boost. ponytail: o
            // ferreiro so vai a 1,3x, entao no boost a taxa sai um pouco abaixo da real; congelar a media deixava ela velha para quem vive no boost
            double inst = (GoldEarned - earnedBefore) / (double)fast;
            RateEma += (inst - RateEma) * Math.Min(1.0, dt / Balance.RateTau);
        }

        void MovePlayer(float dt, float inX, float inY)
        {
            float len = (float)Math.Sqrt(inX * inX + inY * inY);
            Player.Moving = len > 0.01f;
            if (len > 1f) { inX /= len; inY /= len; }
            V2 p = Player.Moving ? new V2(Player.Pos.X + inX * Player.Speed * dt, Player.Pos.Y + inY * Player.Speed * dt) : Player.Pos;
            Player.MoveTo(Collide(Clamp(p, MaxX)), dt);   // parado tambem: estacao comprada embaixo do jogador o empurra para fora
        }

        static V2 Clamp(V2 p, float maxX) => new V2(Math.Max(0.3f, Math.Min(maxX, p.X)), Math.Max(0.3f, Math.Min(Balance.WorldH - 0.3f, p.Y)));

        /// <summary>
        /// Personagem (circulo de CharRadius em `p`) contra os corpos solidos: empurra para fora pela normal da caixa, entao o
        /// que sobra do passo e' a tangente (desliza na face e contorna a quina). Centro dentro da caixa sai pelo lado mais
        /// perto; empate sai por baixo (na frente, na view).
        /// </summary>
        public V2 Collide(V2 p)
        {
            float r = Balance.CharRadius;
            for (int pass = 0; pass < 2; pass++)   // ponytail: 2 passes bastam no mapa esparso; vao estreito entre 2 corpos pede solver iterativo
                foreach (Box b in Solids)
                {
                    V2 q = b.Closest(p), d = p - q;
                    float len = d.Len;
                    if (len >= r) continue;
                    if (len > 1e-5f) { p = q + d * (r / len); continue; }
                    float down = p.Y - (b.Pos.Y - b.Half.Y), up = b.Pos.Y + b.Half.Y - p.Y, left = p.X - (b.Pos.X - b.Half.X), right = b.Pos.X + b.Half.X - p.X;
                    float m = Math.Min(Math.Min(down, up), Math.Min(left, right));
                    p = m == down ? new V2(p.X, b.Pos.Y - b.Half.Y - r) : m == up ? new V2(p.X, b.Pos.Y + b.Half.Y + r)
                      : m == left ? new V2(b.Pos.X - b.Half.X - r, p.Y) : new V2(b.Pos.X + b.Half.X + r, p.Y);
                }
            return p;
        }

        /// <summary>
        /// Direcao (unitaria) de quem anda sozinho rumo a `target` (ajudante e bot; o jogador humano nao): encostado num corpo
        /// que fica na frente da linha reta ate o alvo, contorna pelas quinas (caixa inflada pelo raio) no sentido de menor
        /// caminho, que soma quina a quina ate o alvo ficar visivel. Sem isso o empurrao so anula o passo e ele trava atras da
        /// estacao. Empate: anti-horario.
        /// Impasse (Leva 4; econsim: Ideal a 36,36 ticks/s parado aos 31 min na quina da Ferramentas): quem bate de frente numa quina
        /// fica na curva dela, a ~0,10 m (r(raiz 2 - 1)) do canto da caixa inflada; mirar esse canto o leva para FORA da faixa de
        /// contato, de la a reta ao alvo volta para a mesma quina e o Collide o devolve ao mesmo ponto (vai-e-volta sem fim). Com
        /// `who` andando sem progresso ha Balance.StuckSeconds, quem esta a menos de r do canto ja conta como na quina e mira o ponto
        /// seguinte da rota. So age no impasse: fora dele a rota e' a de sempre (o antigo "ja na quina" de 0,05 m nunca disparava,
        /// encostado nunca chega a menos de 0,09 m do canto), entao os numeros do bot nao mudam.
        /// </summary>
        public V2 Steer(Carrier who, V2 target)
        {
            V2 from = who.Pos, d = target - from;
            float dl = d.Len;
            if (dl < 1e-5f) return new V2(0f, 0f);
            float r = Balance.CharRadius;
            // FASE7: todos os corpos encostados que barram a linha (antes: so o 1o da lista), e rota cuja quina cai dentro de OUTRO corpo
            // (vao mais estreito que o personagem) e' descartada: na boca da Loja de joias, entre a loja e os pedestais da Joalheria real
            // (vaos de 0,1-0,2 m), o bot ia e voltava no vao para sempre. ponytail: resolve vao entre 2 corpos; labirinto pede grafo de waypoints.
            V2 bestStep = d; float bestLen = float.MaxValue;
            bool stuck = who.StuckT >= Balance.StuckSeconds;   // ponytail: ligado sempre contorna melhor toda quina, mas mexe na curva do bot (BALANCE inteiro de novo)
            foreach (Box b in Solids)
            {
                V2 n = from - b.Closest(from), shrunk = b.Half + new V2(r - 0.02f, r - 0.02f);
                if (n.Len > r + 0.01f || !Crosses(from, target, b.Pos, shrunk)) continue;
                V2 h = b.Half + new V2(r, r);
                int face = Math.Abs(n.Y) >= Math.Abs(n.X) ? (n.Y < 0f ? 0 : 2) : (n.X > 0f ? 1 : 3);   // 0 baixo, 1 direita, 2 cima, 3 esquerda
                for (int turn = 1; turn <= 3; turn += 2)   // quinas em ordem anti-horaria; +1 = anti-horario, +3 = horario
                {
                    int i = turn == 1 ? (face + 1) % 4 : face;
                    V2 step = Corner(b.Pos, h, i) - from;
                    bool open = Fits(from + step, b);
                    if (stuck && step.Len < r)   // ja na quina: rumo ao ponto seguinte da rota (a proxima quina, ou o alvo se ele ja aparece dali)
                    {
                        V2 c = Corner(b.Pos, h, i), next = Crosses(c, target, b.Pos, shrunk) ? Corner(b.Pos, h, (i + turn) % 4) : target;
                        step = next - from; open = Fits(next, b);
                    }
                    float len = V2.Dist(from, Corner(b.Pos, h, i));
                    for (int k = 0; k < 4 && Crosses(Corner(b.Pos, h, i), target, b.Pos, shrunk); k++)
                    {
                        int j = (i + turn) % 4;
                        len += V2.Dist(Corner(b.Pos, h, i), Corner(b.Pos, h, j));
                        i = j;
                        open &= Fits(Corner(b.Pos, h, i), b);
                    }
                    len += V2.Dist(Corner(b.Pos, h, i), target);
                    if (open && len < bestLen) { bestLen = len; bestStep = step; }
                }
            }
            return bestStep * (1f / bestStep.Len);
        }

        /// <summary>O personagem cabe em `p` (quina de rota do Steer) sem entrar em outro corpo que nao `except`?</summary>
        bool Fits(V2 p, Box except)
        {
            foreach (Box o in Solids)
                if (!(o.Pos.X == except.Pos.X && o.Pos.Y == except.Pos.Y) && o.Dist(p) < Balance.CharRadius - 0.01f) return false;
            return true;
        }

        /// <summary>Quina `i` (0 baixo-esquerda, 1 baixo-direita, 2 cima-direita, 3 cima-esquerda) da caixa de centro `c` e meia-medida `h`.</summary>
        static V2 Corner(V2 c, V2 h, int i) => new V2(i == 1 || i == 2 ? c.X + h.X : c.X - h.X, i >= 2 ? c.Y + h.Y : c.Y - h.Y);

        /// <summary>O segmento a -> b atravessa a caixa (centro c, meia-medida h)? Teste das faixas.</summary>
        static bool Crosses(V2 a, V2 b, V2 c, V2 h)
        {
            float t0 = 0f, t1 = 1f;
            return Slab(a.X - c.X, b.X - a.X, h.X, ref t0, ref t1) && Slab(a.Y - c.Y, b.Y - a.Y, h.Y, ref t0, ref t1);
        }

        static bool Slab(float p, float d, float h, ref float t0, ref float t1)
        {
            if (Math.Abs(d) < 1e-6f) return p > -h && p < h;
            float u0 = (-h - p) / d, u1 = (h - p) / d;
            if (u0 > u1) { float x = u0; u0 = u1; u1 = x; }
            t0 = Math.Max(t0, u0); t1 = Math.Min(t1, u1);
            return t0 < t1;
        }

        /// <summary>
        /// Ponto intermediario para cruzar a parede lateral (x 9,0-9,6): alvo do outro lado = o meio da abertura (porta ou
        /// arco) com o caminho mais curto, na altura em que a reta cruzaria, dentro da faixa livre; alvo do mesmo lado = o alvo.
        /// ponytail: regra de 2 aberturas, sem pathfinding; mais paredes pedem grafo de waypoints.
        /// </summary>
        public static V2 Via(V2 from, V2 target)
        {
            float x = (Balance.SideWallX0 + Balance.SideWallX1) / 2f;
            if (!(from.X < x && target.X > x) && !(from.X > x && target.X < x)) return target;
            // ja no meio do vao (so da para estar em x 9,3 dentro de uma abertura): segue direto. Sem isso, quem chega ao ponto
            // de passagem com x 9,29999 (arredondamento) mira o proprio lugar, o Steer devolve direcao zero e trava ali (Leva 11)
            if (Math.Abs(from.X - x) < 0.01f) return target;
            float cross = from.Y + (target.Y - from.Y) * (x - from.X) / (target.X - from.X), m = Balance.CharRadius + 0.1f;
            V2 best = target; float bestLen = float.MaxValue;
            for (int i = 0; i + 1 < Balance.SideWallOpenings.Length; i += 2)
            {
                var p = new V2(x, Math.Max(Balance.SideWallOpenings[i] + m, Math.Min(Balance.SideWallOpenings[i + 1] - m, cross)));
                float len = V2.Dist(from, p) + V2.Dist(p, target);
                if (len < bestLen) { bestLen = len; best = p; }
            }
            return best;
        }

        /// <summary>C-CORE-01: Via no distrito com a parede lateral do Old Forge (DistrictDef.Parede); sem parede, o proprio alvo.</summary>
        public V2 Rota(V2 from, V2 target) => Distrito.Parede ? Via(from, target) : target;

        /// <summary>Move `c` em direcao a `target` desviando dos corpos; devolve true quando chegou (dentro de `reach`).</summary>
        public bool MoveTowards(Carrier c, V2 target, float dt, float reach)
        {
            V2 d = target - c.Pos;
            float len = d.Len;
            c.Moving = len > reach;
            if (!c.Moving) return true;
            V2 goal = Rota(c.Pos, target);
            V2 dir = Steer(c, goal);
            c.MoveTo(Collide(Clamp(c.Pos + dir * Math.Min(V2.Dist(c.Pos, goal), c.Speed * dt), Balance.WorldW - 0.3f)), dt);   // ponytail: ajudante usa o mapa inteiro; so mira estacao desbloqueada, entao nunca entra na rua fechada
            return false;
        }
    }
}
