using System;
using System.Collections.Generic;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// B-CORE-03 (GDD §9, ECONOMIA_WAVE_B §1, §9): qualidade sem sorteio. Acumulador por estacao (q = 25 -> exatamente 1 Fina a cada 4),
    /// QualityBreakdown explica 100% das pecas, golpe de mestre so do ferreiro (ajudante e offline: nada), Masterwork nunca no balcao nem no
    /// GoldEarned, balcao vende a melhor primeiro, niveis de estacao (ralo Meta) e save (antigo = tudo Normal; ida e volta no meio do acumulador).
    /// NUnit 3.5 do Unity: asserts separados, sem tupla int x long.
    /// </summary>
    public class QualidadeTests
    {
        const float Dt = 1f / 30f;

        /// <summary>Os 8 niveis de estacao (Meta), na ordem do enum.</summary>
        internal static readonly Upgrade[] Niveis =
        {
            Upgrade.AnvilsLv2, Upgrade.AnvilsLv3, Upgrade.ShieldsLv2, Upgrade.ShieldsLv3, Upgrade.ToolsLv2, Upgrade.ToolsLv3, Upgrade.JewelsLv2, Upgrade.JewelsLv3,
        };

        static void Run(Sim s, float seconds) { for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt, 0f, 0f); }

        /// <summary>Sim com a qualidade revelada (Ferramentas) e `q` pontos na Bigorna (nivel 2 com NivelEstacaoQ = q).</summary>
        internal static Sim ComQ(int q)
        {
            var s = new Sim(new Tuning { NivelEstacaoQ = q });
            s.Buy(Upgrade.Tools);
            s.Buy(Upgrade.AnvilsLv2);
            return s;
        }

        /// <summary>Faz uma peca na Bigorna (receita na entrada, saida vazia) e devolve o nivel com que ela entrou na pilha.</summary>
        internal static Quality Peca(Sim s)
        {
            Station a = s.AnvilA;
            a.Out = 0; Array.Clear(a.OutQ, 0, a.OutQ.Length);
            a.Ins[0] = a.Recipe.Inputs[0].Qty;
            for (int i = 0; i < 2000; i++)
            {
                s.Tick(Dt, 0f, 0f);
                if (a.Out > 0) return a.OutQ[(int)Quality.Superior] > 0 ? Quality.Superior : a.OutQ[(int)Quality.Fine] > 0 ? Quality.Fine : Quality.Normal;
            }
            Assert.Fail("a bigorna nao fez a peca");
            return Quality.Normal;
        }

        // ------------------------------------------------------------------ acumulador

        [Test]
        public void Acumulador_Q25_ExatamenteUmaFinaACadaQuatro()
        {
            Sim s = ComQ(25);
            Assert.AreEqual(25, s.QualityPoints(s.AnvilA));
            var seq = new List<Quality>();
            for (int i = 0; i < 40; i++) seq.Add(Peca(s));
            for (int i = 0; i < seq.Count; i++)
                Assert.AreEqual(i % 4 == 3 ? Quality.Fine : Quality.Normal, seq[i], $"peca {i + 1}: a cada 4, a 4a sai Fina (acumulador 25+25+25+25)");
            Assert.AreEqual(0, s.AnvilA.QAcc, "volta a zero depois de cada Fina");
            Assert.AreEqual(0, s.AnvilB.QAcc, "acumulador e' por estacao: a Bigorna 2 nao fez nada");
        }

        /// <summary>q = 115: parte inteira = Fina garantida; a fracao (15) da Superior exatamente floor(15 n / 100) vezes. Sem sorteio: 2 Sims iguais.</summary>
        [Test]
        public void Acumulador_Q115_TodaFinaOuMelhor_SuperiorPelaFracao_Deterministico()
        {
            Sim s = ComQ(115), t = ComQ(115);
            int sup = 0;
            for (int n = 1; n <= 60; n++)
            {
                Quality l = Peca(s);
                Assert.AreEqual(l, Peca(t), "mesma entrada, mesmo nivel");
                Assert.GreaterOrEqual((int)l, (int)Quality.Fine, $"peca {n}: Fina garantida");
                if (l == Quality.Superior) sup++;
                Assert.AreEqual(15 * n / 100, sup, $"peca {n}: Superiores = parte inteira de 0,15 n");
            }
        }

        /// <summary>
        /// GDD §23 item 5: o QualityBreakdown explica 100% das pecas. Numa partida do bot (niveis comprados no caminho), cada peca de produto de cada
        /// bancada sai no nivel que a soma das fontes (q antes do passo) da num acumulador refeito aqui; tem pontos o nivel da estacao e (B-CORE-07)
        /// a tecnica da familia, que o marco das 150 ferramentas da no caminho.
        /// </summary>
        [Test]
        public void QualityBreakdown_Explica100PorCentoDasPecas()
        {
            var s = new Sim(); var bot = new Bot();
            var acc = new int[s.Stations.Count]; var q = new int[s.Stations.Count];
            int pecas = 0, acima = 0, tecnica = 0;
            for (float t = 0f; t < 40f * 60f; t += Dt)
            {
                foreach (Station st in s.Stations)
                {
                    int[] b = s.QualityBreakdown(st);
                    q[st.Index] = s.QualityPoints(st);
                    int soma = 0;
                    foreach (int p in b) soma += p;
                    Assert.AreEqual(q[st.Index], soma, "q = soma das fontes");
                    int tec = st.Produces && s.IsProduct(st.OutItem) ? s.Projetos.Tecnica(st.Recipe.Family) : 0;
                    Assert.AreEqual(tec, b[(int)QualitySource.Blueprint], "tecnica = o projeto da familia");
                    if (tec > 0) tecnica++;
                    foreach (QualitySource f in new[] { QualitySource.Worker, QualitySource.Material, QualitySource.Buff }) Assert.AreEqual(0, b[(int)f], $"fonte {f} ainda nao existe");
                }
                bot.Step(s, Dt);
                var graded = new Dictionary<int, Quality>();
                foreach (SimEvent e in s.Events) if (e.Kind == Ev.Graded) graded[e.A] = (Quality)e.B;
                foreach (SimEvent e in s.Events)
                {
                    if (e.Kind != Ev.Crafted || !s.IsProduct((Item)e.A)) continue;
                    int k = e.B, nivel = q[k] / 100;
                    acc[k] += q[k] % 100;
                    if (acc[k] >= 100) { nivel++; acc[k] -= 100; }
                    Quality esperado = (Quality)Math.Min(nivel, (int)Quality.Superior);
                    Assert.AreEqual(esperado, graded.TryGetValue(k, out Quality l) ? l : Quality.Normal, $"{s.Stations[k].Name} em {s.Time:0.0}s");
                    pecas++;
                    if (esperado > Quality.Normal) acima++;
                }
            }
            TestContext.WriteLine($"40 min do bot: {pecas} pecas explicadas, {acima} acima de Normal, {tecnica} passos de bancada com tecnica");
            Assert.Greater(pecas, 500, "premissa: partida de verdade");
            Assert.Greater(acima, 30, "premissa: niveis de estacao comprados e pecas Fina");
            Assert.Greater(tecnica, 0, "premissa: a tecnica das ferramentas (marco de 150) entrou no caminho");
        }

        [Test]
        public void Fornalha_SemQualidade()
        {
            Sim s = ComQ(50);
            foreach (int p in s.QualityBreakdown(s.FurnaceA)) Assert.AreEqual(0, p, "lingote e' intermediario");
            s.FurnaceA.In = s.FurnaceA.InCap;
            Run(s, 20f);
            Assert.Greater(s.FurnaceA.Out, 0);
            Assert.AreEqual(0, s.FurnaceA.OutQ[1] + s.FurnaceA.OutQ[2]);
            Assert.AreEqual(0, s.FurnaceA.QAcc);
        }

        // ------------------------------------------------------------------ preco, balcao e VIP

        [Test]
        public void Preco_PorNivel_InteiroMeioParaCima()
        {
            var s = new Sim();
            int[] esperado = { 10, 15, 25, 25, 38, 63, 16, 24, 40, 60, 90, 150 };
            int k = 0;
            for (Item i = Item.Sword; i <= Item.Jewel; i++)
                for (Quality l = Quality.Normal; l <= Quality.Superior; l++) Assert.AreEqual(esperado[k++], s.PriceOf(i, l), $"{i} {l}");
            Assert.AreEqual(0, s.PriceOf(Item.Sword, Quality.Masterwork), "Masterwork nao vende");
            s.Buy(Upgrade.Jewelry); s.Buy(Upgrade.JewelVitrine);
            Assert.AreEqual(120, s.PriceOf(Item.Jewel, Quality.Fine), "com a Vitrine de joias: 80 / 120 / 200");
            Assert.AreEqual(200, s.PriceOf(Item.Jewel, Quality.Superior));
        }

        /// <summary>O balcao vende a melhor primeiro (Superior, Fina, Normal), cada uma no preco dela, e a receita soma no GoldEarned.</summary>
        [Test]
        public void Balcao_VendeAMelhorPrimeiro()
        {
            var s = new Sim();
            s.Stock[(int)Item.Sword] = 3; s.StockQ[(int)Item.Sword][(int)Quality.Fine] = 1; s.StockQ[(int)Item.Sword][(int)Quality.Superior] = 1;
            var precos = new List<int>();
            for (float t = 0f; t < 120f && precos.Count < 3; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events) if (e.Kind == Ev.Sold) precos.Add(e.B);
            }
            CollectionAssert.AreEqual(new[] { 25, 15, 10 }, precos, "Superior, Fina, Normal");
            Assert.AreEqual(50L, s.GoldEarned);
            Assert.AreEqual(20L, s.QualityGold, "premio de qualidade = acima do Normal (15 + 5)");
        }

        /// <summary>O VIP paga 3x o preco da peca que leva, com a qualidade, a melhor primeiro.</summary>
        [Test]
        public void Vip_PagaTresVezesOPrecoComAQualidade()
        {
            var s = new Sim();
            s.Tick(Dt, 0f, 0f);
            s.Queue.Clear();
            s.Queue.Add(new Client { Want = Item.Sword, Patience = 99f, MaxPatience = 99f, Vip = true, Pack = 2 });
            s.Stock[(int)Item.Sword] = 2; s.StockQ[(int)Item.Sword][(int)Quality.Superior] = 1;
            var precos = new List<int>();
            for (float t = 0f; t < 5f && precos.Count < 2; t += Dt)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events) if (e.Kind == Ev.Sold) precos.Add(e.B);
            }
            CollectionAssert.AreEqual(new[] { 75, 30 }, precos, "3 x 25 (Superior) e 3 x 10 (Normal)");
        }

        // ------------------------------------------------------------------ golpe de mestre

        /// <summary>Bigorna com a peca na entrada e o ferreiro parado na boca dela (qualidade revelada).</summary>
        static Sim NaBoca()
        {
            Sim s = ComQ(0);
            s.Player.Pos = s.AnvilA.InAt;
            s.AnvilA.Ins[0] = s.AnvilA.InCap;
            s.Tick(Dt, 0f, 0f);
            return s;
        }

        [Test]
        public void Golpe_FerreiroParadoNaBoca_SobeUmNivel_Recarga_ErrarNaoPune()
        {
            var cedo = new Sim();   // antes das Ferramentas: nem o golpe existe (revelacao, GDD §19)
            cedo.Player.Pos = cedo.AnvilA.InAt; cedo.AnvilA.Ins[0] = 1; cedo.Tick(Dt, 0f, 0f);
            Assert.IsNull(cedo.StrikeStation);
            Assert.IsFalse(cedo.Strike(true));

            Sim s = NaBoca();
            Assert.AreSame(s.AnvilA, s.StrikeStation);
            Assert.IsFalse(s.Strike(false), "errou");
            Assert.IsFalse(s.AnvilA.Strike, "errar nao pune: nada muda");
            Assert.AreEqual(0f, s.StrikeCooldown, "e nao gasta a recarga");
            Assert.IsTrue(s.Strike(true), "acertou");
            Assert.IsTrue(s.AnvilA.Strike);
            Assert.AreEqual(s.T.GolpeRecarga, s.StrikeCooldown);
            Assert.IsNull(s.StrikeStation, "na recarga o anel some");
            Assert.IsFalse(s.Strike(true), "um golpe por recarga");
            Quality l = Peca(s);
            Assert.AreEqual(Quality.Fine, l, "q = 0: a peca do golpe sai Normal + 1");
            Assert.IsFalse(s.AnvilA.Strike, "o golpe vale uma peca");
            Assert.AreEqual(Quality.Normal, Peca(s), "a seguinte volta ao acumulador");
            Assert.Greater(s.StrikeGold, 0L);
            // andando na boca: sem golpe
            Sim m = NaBoca();
            m.Tick(Dt, 1f, 0f);
            Assert.IsNull(m.StrikeStation, "so parado");
        }

        [Test]
        public void Golpe_RecargaDe20sNoTempoDeJogo()
        {
            Sim s = NaBoca();
            Assert.IsTrue(s.Strike(true));
            Run(s, s.T.GolpeRecarga - 1f);
            Assert.Greater(s.StrikeCooldown, 0f);
            Run(s, 1.1f);
            Assert.AreEqual(0f, s.StrikeCooldown);
        }

        /// <summary>Negativo (ECONOMIA_WAVE_B §9.3): ajudante na boca, cofre offline e bot que nao e' o Ideal nunca dao golpe.</summary>
        [Test]
        public void Golpe_AjudanteOfflineEBotComum_NenhumEfeito()
        {
            Sim s = ComQ(0);
            s.Buy(Upgrade.Helper1); s.Buy(Upgrade.Helper2);
            s.AnvilA.Ins[0] = s.AnvilA.InCap;
            s.Player.Pos = new V2(4.5f, 3.5f);
            foreach (Carrier w in s.Workers) { w.Pos = s.AnvilA.InAt; w.Moving = false; }
            s.Tick(Dt, 0f, 0f);
            foreach (Carrier w in s.Workers) w.Pos = s.AnvilA.InAt;
            Assert.IsNull(s.StrikeStation, "ajudante na boca nao abre o anel");
            Assert.IsFalse(s.Strike(true), "e o golpe nao pega");
            Assert.IsFalse(s.AnvilA.Strike);
            Assert.AreEqual(0, s.Strikes);

            s.RateEma = 10.0;
            string antes = Qualidade(s.Save(1));
            Assert.Greater(s.ApplyOffline(3600, 99), 0L, "premissa: o cofre pagou");
            Assert.AreEqual(antes, Qualidade(s.Save(1)), "o cofre so da ouro: nenhuma peca, golpe ou acumulador");

            foreach (BotProfile p in new[] { BotProfile.F2P, BotProfile.Slow, BotProfile.AdWatcher })
            {
                var b = new Sim(); var bot = Bot.From(p);
                for (float t = 0f; t < 25f * 60f; t += Dt) bot.Step(b, Dt);
                Assert.IsTrue(b.QualityRevealed, p.Name + ": premissa (Ferramentas compradas)");
                Assert.AreEqual(0, b.Strikes, p.Name + " nunca da golpe");
                Assert.AreEqual(0L, b.StrikeGold);
            }
            var ideal = new Sim(); var bi = Bot.Ideal();
            for (float t = 0f; t < 25f * 60f; t += Dt) bi.Step(ideal, Dt);
            Assert.Greater(ideal.Strikes, 0, "o Ideal (Perfect) acerta os golpes");
        }

        /// <summary>As linhas de qualidade do save (lotes, acumuladores, golpe).</summary>
        static string Qualidade(string save) =>
            string.Join("\n", Array.FindAll(save.Split('\n'), l => l.StartsWith("stocksq=") || l.StartsWith("holdsq=") || l.StartsWith("wksq=") || l.StartsWith("qst") || l.StartsWith("golpe=")));

        // ------------------------------------------------------------------ Masterwork (so a estrutura; projeto e condicoes no B-CORE-08)

        /// <summary>Sem projeto (B-CORE-08 nao ligou o MasterworkPending), o golpe numa Superior fica Superior e vende a 2,5x.</summary>
        [Test]
        public void Masterwork_SemProjeto_GolpeNaSuperiorFicaSuperior()
        {
            Sim s = ComQ(200);
            s.Player.Pos = s.AnvilA.InAt; s.AnvilA.Ins[0] = 1; s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(Quality.Superior, Peca(s), "q 200: Superior pela automacao");
            s.Player.Pos = s.AnvilA.InAt; s.AnvilA.Ins[0] = 1; s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Strike(true));
            Assert.AreEqual(Quality.Superior, Peca(s), "o golpe numa Superior sem projeto fica Superior");
            Assert.AreEqual(0, s.Masterworks);
        }

        /// <summary>
        /// Com Masterwork pendente, o golpe numa Superior faz a obra-prima: ela vai ao Hall (Masterworks), nunca a pilha, ao balcao nem ao
        /// GoldEarned. Partida do Ideal (golpe sempre) com q 200 e pendente sempre: toda peca feita esta vendida, no balcao, numa pilha, numa mao
        /// ou no Hall, e a receita e' exatamente a soma dos precos das vendidas (nenhuma a preco de Masterwork).
        /// </summary>
        [Test]
        public void Masterwork_NuncaNoBalcaoNemNoGoldEarned()
        {
            var s = new Sim(new Tuning { NivelEstacaoQ = 200 }) { MasterworkPending = st => true };
            var bot = Bot.Ideal();
            long vendas = 0; int mw = 0;
            for (float t = 0f; t < 30f * 60f; t += Dt)
            {
                bot.Step(s, Dt);
                foreach (SimEvent e in s.Events)
                {
                    if (e.Kind == Ev.Sold) { vendas += e.B; Assert.LessOrEqual(e.B, 3 * s.PriceOf((Item)e.A, Quality.Superior), "nenhuma venda acima da Superior (VIP 3x)"); }
                    if (e.Kind == Ev.Masterwork) mw++;
                }
            }
            Assert.Greater(s.Masterworks, 0, "premissa: houve obra-prima");
            Assert.AreEqual(mw, s.Masterworks, "uma Ev.Masterwork por obra-prima");
            Assert.AreEqual(vendas, s.GoldEarned, "a receita sao as vendas, nenhuma Masterwork");
            int noHall = 0;   // ponytail: o Hall (B-CORE-08) ainda nao guarda qual; a conta fecha no total
            for (int i = (int)Item.Sword; i <= (int)Item.Jewel; i++)
            {
                Assert.LessOrEqual(Vistas(s, i), s.Crafted[i], Items.All[i].Id);
                noHall += s.Crafted[i] - Vistas(s, i);
            }
            Assert.AreEqual(s.Masterworks, noHall, "toda peca feita esta vendida, no balcao, numa pilha ou mao; o que falta foi ao Hall");
        }

        static int Vistas(Sim s, int i)
        {
            int v = s.SoldItems[i] + s.Stock[i] + s.Player.Held[i];
            foreach (Station st in s.Stations) if (st.Produces && (int)st.OutItem == i) v += st.Out;
            foreach (Carrier w in s.Workers) v += w.Held[i];
            return v;
        }

        /// <summary>A obra-prima nao entra na pilha (Out igual) e sai como Ev.Masterwork, sem Ev.Graded.</summary>
        [Test]
        public void Masterwork_NaoEntraNaPilha()
        {
            Sim s = ComQ(200);
            s.MasterworkPending = st => st == s.AnvilA;
            s.Player.Pos = s.AnvilA.InAt; s.AnvilA.Ins[0] = 1; s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Strike(true));
            s.AnvilA.Out = 0; Array.Clear(s.AnvilA.OutQ, 0, s.AnvilA.OutQ.Length);
            bool evento = false;
            for (int i = 0; i < 2000 && !evento; i++)
            {
                s.Tick(Dt, 0f, 0f);
                foreach (SimEvent e in s.Events) if (e.Kind == Ev.Masterwork) { evento = true; Assert.AreEqual((int)Item.Sword, e.A); }
                foreach (SimEvent e in s.Events) Assert.AreNotEqual(Ev.Graded, e.Kind, "obra-prima nao e' peca da pilha");
            }
            Assert.IsTrue(evento);
            Assert.AreEqual(0, s.AnvilA.Out, "nada na pilha");
            Assert.AreEqual(1, s.Masterworks);
            Assert.AreEqual(0L, s.GoldEarned);
        }

        // ------------------------------------------------------------------ pecas com nivel em todo o caminho

        /// <summary>Pilha -> mao do ferreiro -> balcao: o nivel vai junto (a melhor primeiro) e o lote nunca passa do total.</summary>
        [Test]
        public void Peca_LevaONivelDaPilhaAMaoEAoBalcao()
        {
            var s = new Sim();
            s.AnvilA.Out = 3; s.AnvilA.OutQ[(int)Quality.Fine] = 1; s.AnvilA.OutQ[(int)Quality.Superior] = 1;
            s.Player.Pos = s.AnvilA.OutAt;
            Run(s, 1f);
            Assert.AreEqual(3, s.Player.Held[(int)Item.Sword]);
            Assert.AreEqual(1, s.Player.HeldQ[(int)Item.Sword][(int)Quality.Fine]);
            Assert.AreEqual(1, s.Player.HeldQ[(int)Item.Sword][(int)Quality.Superior]);
            Assert.AreEqual(0, s.AnvilA.OutQ[1] + s.AnvilA.OutQ[2]);
            s.Player.Pos = s.Counter.InAt;
            Run(s, 1f);
            Assert.AreEqual(3, s.Stock[(int)Item.Sword] + s.SoldItems[(int)Item.Sword]);
            int altas = s.StockQ[(int)Item.Sword][1] + s.StockQ[(int)Item.Sword][2];
            Assert.LessOrEqual(altas, s.Stock[(int)Item.Sword]);
            Assert.AreEqual(0, s.Player.HeldQ[(int)Item.Sword][1] + s.Player.HeldQ[(int)Item.Sword][2]);
        }

        [Test]
        public void Lote_FitCortaDeBaixoParaCima_TakeAMelhor()
        {
            var q = new int[Sim.Graus];
            q[1] = 2; q[2] = 2;
            Sim.Fit(3, q);
            Assert.AreEqual(1, q[1], "Fina cortada");
            Assert.AreEqual(2, q[2], "a Superior fica");
            int total = 3;
            Assert.AreEqual(Quality.Superior, Sim.Take(ref total, q));
            Assert.AreEqual(Quality.Superior, Sim.Take(ref total, q));
            Assert.AreEqual(Quality.Fine, Sim.Take(ref total, q));
            Assert.AreEqual(0, total);
            Sim.Put(ref total, q, Quality.Normal);
            Assert.AreEqual(Quality.Normal, Sim.Take(ref total, q));
        }

        // ------------------------------------------------------------------ niveis de estacao (Meta)

        [Test]
        public void NivelDeEstacao_CustoQualidadeVelocidadeERevelacao()
        {
            var s = new Sim();
            Assert.AreEqual(1, s.AnvilA.Level);
            Assert.IsFalse(s.MenuAvailable((int)Upgrade.AnvilsLv2), "antes das Ferramentas nao esta a venda");
            Assert.AreEqual(Cartao.Oculto, Textos.Estado(s, (int)Upgrade.AnvilsLv2), "e o cartao nem aparece (GDD §19: revelacao)");
            float t0 = s.AnvilA.Time;
            s.Buy(Upgrade.Tools);
            Assert.IsTrue(s.MenuAvailable((int)Upgrade.AnvilsLv2));
            Assert.AreEqual(Cartao.Travado, Textos.Estado(s, (int)Upgrade.AnvilsLv3), "o III aparece travado: Precisa: Nivel II");
            Assert.IsFalse(s.MenuAvailable((int)Upgrade.JewelsLv2), "o II da joalheria espera a Joalheria");
            Assert.AreEqual(Cartao.Oculto, Textos.Estado(s, (int)Upgrade.JewelsLv2));
            s.Buy(Upgrade.AnvilsLv2); s.Buy(Upgrade.AnvilsLv3);
            Assert.AreEqual(3, s.AnvilA.Level);
            Assert.AreEqual(3, s.AnvilB.Level, "a familia inteira (as 2 bigornas)");
            Assert.AreEqual(1, s.ShieldBench.Level);
            Assert.AreEqual(30, s.QualityPoints(s.AnvilA), "+15 por nivel");
            Assert.AreEqual(t0 / 1.1f, s.AnvilA.Time, 1e-5f, "+5% de velocidade por nivel");
            int[] custo = { 250, 750, 325, 950, 250, 750, 500, 1500 };
            for (int i = 0; i < Niveis.Length; i++)
            {
                Assert.AreEqual(custo[i], (int)s.T.Cost(Niveis[i]), Niveis[i].ToString());
                Assert.IsTrue(Upgrades.All[(int)Niveis[i]].Meta && Upgrades.All[(int)Niveis[i]].InMenu);
                Assert.IsFalse(Upgrades.IsProduction((int)Niveis[i]));
                Assert.AreEqual(2 + i % 2, Upgrades.StationLevel((int)Niveis[i]));
            }
        }

        /// <summary>Meta fica fora da producao completa e do teto offline (ECONOMIA_WAVE_B §0.7), como o luxo.</summary>
        [Test]
        public void Meta_ForaDaProducaoCompletaEDoTetoOffline()
        {
            var s = new Sim();
            for (int i = 0; i < Upgrades.Count; i++) if (Upgrades.IsProduction(i)) s.Buy((Upgrade)i);
            Assert.IsTrue(s.ProductionComplete, "sem nenhum nivel de estacao");
            long teto = s.OfflineMaxGold();
            foreach (Upgrade u in Niveis) s.Buy(u);
            Assert.AreEqual(teto, s.OfflineMaxGold(), "nivel de estacao nao mexe no cofre");
            Assert.IsTrue(s.Buy(Upgrade.WorkshopFacade), "o luxo nao espera o Meta");
        }

        /// <summary>Os 10 primeiros minutos ficam intocados (ECONOMIA_WAVE_B §0.6): o bot F2P joga igual com e sem qualidade, tick a tick.</summary>
        [Test]
        public void Revelacao_PrimeirosDezMinutos_IguaisSemQualidade()
        {
            var com = new Sim();
            int[] semNiveis = new int[16];   // B-CORE-07: os 16 (II-III e IV-V)
            for (int i = 0; i < semNiveis.Length; i++) semNiveis[i] = 1 << 30;
            var sem = new Sim(new Tuning { NivelEstacaoCusto = semNiveis, GolpeRecarga = 1e9f });
            string a = BalanceTests.Play(com, 600f), b = BalanceTests.Play(sem, 600f);
            Assert.AreEqual(b, a, "minuto a minuto");
            Assert.AreEqual(sem.Save(1), com.Save(1), "estado inteiro (compras e instantes, pilhas, maos, posicoes)");
            Assert.IsFalse(com.QualityRevealed, "o F2P compra as Ferramentas depois dos 10 min");
        }

        // ------------------------------------------------------------------ save

        /// <summary>Schema 3 da main antes do B-CORE-03 (sem stocksq=/holdsq=/wksq=/qstN=/golpe=): tudo Normal, acumuladores 0, sem golpe.</summary>
        internal const string SaveSchema3 = "v=3\nt=1500\ngold=800\nups=bigorna2:120,ajudante1:200,escudos:300,mochila:350,ajudante2:400,ferramentas:600,ajudante3:700\n" +
                                            "up=01111100010010000000000000000\nupp=\nst3=4,3,1,0.5,40\nst5=2,2,0,0,10\nstocks=espada:3,escudo:2\nsolds=espada:40,escudo:8\n" +
                                            "holds=lingote:2,espada:1\nwks=0:minerio:2;1:lingote:2;2:espada:1\nms=2,0,0,0\nema=5\nsaved=1000\nclaim=900\nm=19,48,600,0,3,2,4,10,7\npx=4.50,3.50\n";

        [Test]
        public void SaveAntigo_TudoNormal_AcumuladoresZero()
        {
            foreach (string old in new[] { SaveSchema3, CoreTests.SaveV08, CoreTests.SaveV03, CoreTests.SaveV01 })
            {
                Sim s = Sim.Load(old);
                for (int i = 0; i < s.Stock.Length; i++)
                {
                    Assert.AreEqual(0, s.StockQ[i][1] + s.StockQ[i][2], "balcao Normal");
                    Assert.AreEqual(0, s.Player.HeldQ[i][1] + s.Player.HeldQ[i][2], "mao Normal");
                    foreach (Carrier w in s.Workers) Assert.AreEqual(0, w.HeldQ[i][1] + w.HeldQ[i][2], "ajudante Normal");
                }
                foreach (Station st in s.Stations)
                {
                    Assert.AreEqual(0, st.QAcc);
                    Assert.IsFalse(st.Strike);
                    Assert.AreEqual(0, st.OutQ[1] + st.OutQ[2]);
                }
                Assert.AreEqual(0f, s.StrikeCooldown);
                Assert.AreEqual(0, s.UnknownIds);
            }
            Sim b = Sim.Load(SaveSchema3);
            Assert.AreEqual(3, b.Stock[(int)Item.Sword], "o total continua o de antes");
            Assert.IsTrue(b.QualityRevealed);
            string novo = b.Save(2000);
            StringAssert.Contains("stocksq=\n", novo);
            StringAssert.Contains("qst3=0,0,0,0\n", novo);
            Assert.AreEqual(novo, Sim.Load(novo).Save(2000), "estavel depois de migrar");
        }

        /// <summary>Save/load no meio do acumulador, com golpe valendo, recarga e pecas Fina/Superior em pilha, mao, ajudante e balcao: carrega
        /// identico e a sequencia de niveis continua igual a de quem nao salvou.</summary>
        [Test]
        public void SaveLoad_NoMeioDoAcumulador_ContinuaIgual()
        {
            var t = new Tuning { NivelEstacaoQ = 25 };
            Sim s = ComQ(25);
            Peca(s); Peca(s);
            Assert.AreEqual(50, s.AnvilA.QAcc, "premissa: no meio");
            s.Buy(Upgrade.Shields);
            s.ShieldBench.Out = 2; s.ShieldBench.OutQ[(int)Quality.Superior] = 1; s.ShieldBench.QAcc = 30;
            s.Stock[(int)Item.Tool] = 3; s.StockQ[(int)Item.Tool][(int)Quality.Fine] = 2;
            s.Player.Held[(int)Item.Sword] = 2; s.Player.HeldQ[(int)Item.Sword][(int)Quality.Fine] = 1;
            s.AnvilB.Strike = true; s.StrikeCooldown = 7.5f;
            string text = s.Save(10);
            StringAssert.Contains("stocksq=ferramenta:2:0\n", text);
            StringAssert.Contains("holdsq=espada:1:0\n", text);
            StringAssert.Contains("qst" + s.AnvilA.Index + "=50,0,0,0\n", text);
            StringAssert.Contains("qst" + s.AnvilB.Index + "=0,1,0,0\n", text);
            StringAssert.Contains("qst" + s.ShieldBench.Index + "=30,0,0,1\n", text);
            StringAssert.Contains("golpe=7.5\n", text);
            Sim b = Sim.Load(text, t);
            Assert.AreEqual(text, b.Save(10), "ida e volta identica");
            Assert.IsTrue(b.AnvilB.Strike);
            Assert.AreEqual(7.5f, b.StrikeCooldown);
            Assert.AreEqual(1, b.ShieldBench.OutQ[(int)Quality.Superior]);
            for (int i = 0; i < 8; i++) Assert.AreEqual(Peca(s), Peca(b), $"peca {i + 3}: o acumulador continua de onde parou");
            Sim c = Sim.Load(text, t);
            Assert.AreEqual(Quality.Normal, Peca(c), "50 + 25 = 75");
            Assert.AreEqual(Quality.Fine, Peca(c), "75 + 25 = 100: a 2a depois do load sai Fina");

            // a carga dos ajudantes volta com o nivel, cada um na ordem do wks=
            var h = new Sim();
            h.Buy(Upgrade.Helper1); h.Buy(Upgrade.Helper2); h.Buy(Upgrade.Shields); h.Buy(Upgrade.Helper3);
            Carrier w = h.Workers.Find(c => c.Role == 2);
            w.Held[(int)Item.Shield] = 2; w.HeldQ[(int)Item.Shield][(int)Quality.Superior] = 2;
            string ht = h.Save(10);
            StringAssert.Contains("wksq=;;escudo:0:2\n", ht);
            Sim hb = Sim.Load(ht);
            Assert.AreEqual(2, hb.Workers.Find(c => c.Role == 2).HeldQ[(int)Item.Shield][(int)Quality.Superior]);
            Assert.AreEqual(ht, hb.Save(10));
        }

        [Test]
        public void SaveLoad_LoteMaiorQueOTotal_Cortado()
        {
            var s = new Sim();
            string text = s.Save(1).Replace("stocksq=\n", "stocksq=espada:9:9,machado:1:1\n").Replace("golpe=0\n", "golpe=-4\n");
            Sim b = Sim.Load(text);
            Assert.AreEqual(0, b.StockQ[(int)Item.Sword][1] + b.StockQ[(int)Item.Sword][2], "sem espada no balcao, lote 0");
            Assert.AreEqual(0f, b.StrikeCooldown, "recarga negativa vira 0");
            Assert.AreEqual(0, b.UnknownIds, "o lote de item desconhecido nao conta de novo");
        }

        // ------------------------------------------------------------------ textos: barra da estacao, QualityBreakdown e cartao

        [Test]
        public void Barra_ProximaFinaEmN_EBreakdown()
        {
            try
            {
                Loc.Atual = Idioma.Pt;
                Sim s = ComQ(15);
                Station a = s.AnvilA;
                Assert.AreEqual("Fina em 7", Textos.Barra(s, a));
                a.QAcc = 84; Assert.AreEqual("Fina em 2", Textos.Barra(s, a));
                a.QAcc = 85; Assert.AreEqual("Próxima: Fina!", Textos.Barra(s, a));
                a.Strike = true; Assert.AreEqual("Golpe! Próxima +1", Textos.Barra(s, a));
                Assert.AreEqual("Só Normal", Textos.Barra(s, s.ToolBench), "nivel 1: q = 0");
                a.Strike = false; a.QAcc = 0;
                Assert.AreEqual("Nível da estação: +0,15\nTotal +0,15 · Fina em 7", Textos.Detalhe(s, a));
                Loc.Atual = Idioma.En;
                Assert.AreEqual("Fine in 7", Textos.Barra(s, a));
                Assert.AreEqual("Quality +0.15 · speed +5%", Textos.Beneficio(Tuning.Padrao, (int)Upgrade.AnvilsLv2));
                Assert.AreEqual("1 in 7 is Fine", Textos.AntesDepois(Tuning.Padrao, (int)Upgrade.AnvilsLv2));
                Loc.Atual = Idioma.Pt;
                Assert.AreEqual("Qualidade +0,15 · vel. +5%", Textos.Beneficio(Tuning.Padrao, (int)Upgrade.ShieldsLv3));
                Assert.AreEqual("1 em 7 sai Fina", Textos.AntesDepois(Tuning.Padrao, (int)Upgrade.AnvilsLv2), "GDD §19: nivel II");
                Assert.AreEqual("1 em 3 sai Fina", Textos.AntesDepois(Tuning.Padrao, (int)Upgrade.AnvilsLv3));
                Assert.AreEqual("Precisa: Nível II", Textos.Precisa(Upgrades.All[(int)Upgrade.AnvilsLv3].Requires));
            }
            finally { Loc.Atual = Idioma.Pt; }
        }
    }
}
