using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using FS.Core;

namespace FS.Tests
{
    /// <summary>
    /// GDD §164 item 1 (Core polido): o nucleo roda em passo fixo e so le os toques, entao a mesma gravacao de toques reproduz a mesma
    /// partida. Um jogador roteirizado (Random com semente: segue o Bot.Decide com distracoes, toca no menu, no worker, no golpe e no contrato)
    /// grava cada tick como uma linha de texto; um Sim novo le o texto e repete. ponytail: a gravacao vive so no teste; um replay de
    /// verdade (bug report do Closed) usa o mesmo formato quando precisar.
    /// </summary>
    public class ReplayTests
    {
        const float Dt = BalanceTests.Dt;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Joga `seconds` com toques sorteados (semente `seed`) e devolve a gravacao: uma linha por tick, "x y" e os toques do tick.</summary>
        static string Gravar(Sim s, float seconds, int seed)
        {
            var rng = new Random(seed);
            var sb = new StringBuilder();
            V2 alvo = s.Player.Pos; float distraido = 0f;
            for (float t = 0f; t < seconds; t += Dt)
            {
                var toques = new List<string>();
                int menu = s.CheapestAffordableMenu();
                if (menu >= 0 && rng.NextDouble() < 0.02) toques.Add("M" + menu);
                int wk = s.Equipe.MaisBarato();
                if (wk >= 0 && rng.NextDouble() < 0.01) toques.Add("W" + wk);
                if (rng.NextDouble() < 0.05) toques.Add(rng.NextDouble() < 0.7 ? "S1" : "S0");
                if (s.Contratos.Oferta != null && rng.NextDouble() < 0.01) toques.Add("C");
                if ((distraido -= Dt) <= 0f && rng.NextDouble() < 0.004)   // ~1 distracao a cada 8 s: vai a uma estacao qualquer por 0,5-2 s
                {
                    Station st = s.Stations[rng.Next(s.Stations.Count)];
                    alvo = rng.NextDouble() < 0.5 ? st.InAt : st.OutAt;
                    distraido = 0.5f + (float)rng.NextDouble() * 1.5f;
                }
                V2 d = distraido <= 0f ? Bot.Decide(s)
                    : V2.Dist(s.Player.Pos, alvo) < Bot.Reach ? new V2(0f, 0f) : s.Steer(s.Player, s.Rota(s.Player.Pos, alvo));
                float x = d.X * (0.6f + (float)rng.NextDouble() * 0.4f), y = d.Y * (0.6f + (float)rng.NextDouble() * 0.4f);   // dedo humano: nunca o stick cheio e reto
                sb.Append(x.ToString("R", Inv)).Append(' ').Append(y.ToString("R", Inv));
                foreach (string k in toques) sb.Append(' ').Append(k);
                sb.Append('\n');
                Aplicar(s, x, y, toques);
            }
            return sb.ToString();
        }

        /// <summary>Repete uma gravacao num Sim (o mesmo caminho do Gravar: toques antes do Tick, como a View).</summary>
        static void Reproduzir(Sim s, string gravacao)
        {
            foreach (string linha in gravacao.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = linha.Split(' ');
                var toques = new List<string>();
                for (int i = 2; i < p.Length; i++) toques.Add(p[i]);
                Aplicar(s, float.Parse(p[0], Inv), float.Parse(p[1], Inv), toques);
            }
        }

        static void Aplicar(Sim s, float x, float y, List<string> toques)
        {
            foreach (string k in toques)
                switch (k[0])
                {
                    case 'M': s.TryBuyMenu((Upgrade)int.Parse(k.Substring(1), Inv)); break;
                    case 'W': s.Equipe.Comprar(int.Parse(k.Substring(1), Inv)); break;
                    case 'S': s.Strike(k[1] == '1'); break;
                    case 'C': s.Contratos.Aceitar(); break;
                }
            s.Tick(Dt, x, y);
        }

        static string Foto(Sim s) => $"t={s.Time.ToString("R", Inv)} ouro={s.Gold} ganho={s.GoldEarned} vendas={s.Sales} perdidos={s.ClientsLost} " +
            $"compras={s.UpgradesBought} pos={s.Player.Pos}\n{s.Save(1)}";

        [Test]
        public void Replay_MesmosToques_MesmaPartida()
        {
            var a = new Sim();
            string gravacao = Gravar(a, 20 * 60f, 7);
            Assert.Greater(a.UpgradesBought, 5, "o roteiro joga de verdade (compra pads e menu), senao o replay nao prova nada");
            Assert.Greater(a.Sales, 50);
            TestContext.WriteLine($"20 min roteirizados: {a.UpgradesBought} compras, {a.Sales} vendas, {gravacao.Length / 1024} KB de gravacao");

            var b = new Sim();
            Reproduzir(b, gravacao);
            Assert.AreEqual(Foto(a), Foto(b), "a gravacao em texto reproduz a partida ao byte (estado e save)");

            var c = new Sim();   // e de novo: o replay nao depende de nada fora do Sim (relogio, ordem de criacao)
            Reproduzir(c, gravacao);
            Assert.AreEqual(Foto(b), Foto(c));
        }

        [Test]
        public void Replay_UmToqueDiferente_OutraPartida()
        {
            var a = new Sim();
            string gravacao = Gravar(a, 5 * 60f, 11);
            int i = gravacao.IndexOf('\n', gravacao.Length / 3) + 1;   // um tick no 1o terco: joystick para o lado oposto
            int fim = gravacao.IndexOf('\n', i);
            string mexida = gravacao.Substring(0, i) + "-1 -1" + gravacao.Substring(fim);
            var b = new Sim();
            Reproduzir(b, mexida);
            Assert.AreNotEqual(Foto(a), Foto(b), "o replay le os toques: mudar um tick muda a partida (o teste de cima nao passa por acaso)");
        }
    }
}
