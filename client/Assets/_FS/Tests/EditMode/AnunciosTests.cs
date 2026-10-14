using System;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// E-PLAT-02/03 (GDD §17, §23 item 15): placements de rewarded por config (Tuning.Placements), recompensa 1x por ID de transacao aplicada
    /// pelo nucleo, recarga e teto diario no save (relogio para tras e reabrir nao liberam), Trocar mostra a proxima certa, Supply Drop exato,
    /// D6 e flag por placement; intersticial: flag desligada = nunca, e com ela ligada os 5 vetos (tutorial, acao, cooldown, cap, remove_ads).
    /// </summary>
    public class AnunciosTests
    {
        const float Dt = 1f / 30f;
        const long Now = 1000000;

        static void Run(Sim s, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt) s.Tick(Dt, 0f, 0f);
        }

        static readonly Anuncio[] Todos = (Anuncio[])Enum.GetValues(typeof(Anuncio));

        /// <summary>Todo placement pode: 1a melhoria (D6, salvo `liberado` false), ja vendeu, VIP natural longe, encomenda ativa (5 espadas,
        /// premio 100) e um resgate do cofre de 400 nesta sessao.</summary>
        static Sim Pronto(Tuning t = null, bool liberado = true)
        {
            Sim s = liberado ? TuningTests.Liberado(t) : new Sim(t);
            s.FirstSaleTime = 1f; s.VipIn = 200f;
            EncTeste.Poe(s, Item.Sword, 5, 100);
            s.LastClaim = 777; s.LastClaimGold = 400;
            return s;
        }

        static long Assiste(Sim s, Anuncio a) => s.Anuncios.Pagar(s.Anuncios.Oferta(a));

        [Test]
        public void CadaPlacement_Paga1xPorId_NemDepoisDeReabrir()
        {
            foreach (Anuncio a in Todos)
            {
                Sim s = Pronto();
                string tx = s.Anuncios.Oferta(a);
                Assert.IsNotNull(tx, a + ": oferta");
                StringAssert.StartsWith(s.T.Ad(a).Id + ":", tx, a + ": ID = placement:chave");
                Assert.GreaterOrEqual(s.Anuncios.Pagar(tx), 0L, a + ": paga");
                Assert.AreEqual(-1L, s.Anuncios.Pagar(tx), a + ": o mesmo ID nao paga de novo");
                Sim b = Sim.Load(s.Save(Now));
                Assert.AreEqual(s.Anuncios.Pago[(int)a], b.Anuncios.Pago[(int)a], a + ": a chave paga vai no save");
                Assert.AreEqual(-1L, b.Anuncios.Pagar(tx), a + ": nem depois de reabrir");
                Assert.AreEqual(1, s.Anuncios.HojeTotal(), a + ": conta 1 no dia");
            }
        }

        [Test]
        public void OfertaQueMudouEntreOToqueEARecompensa_NaoPaga()
        {
            Sim s = Pronto();
            string tx = s.Anuncios.Oferta(Anuncio.EncomendaBonus);
            EncTeste.Completa(s);
            s.Tick(Dt, 0f, 0f);   // entregue (o jogo para no anuncio, mas o nucleo nao confia nisso)
            Assert.AreEqual(-1L, s.Anuncios.Pagar(tx), "a encomenda do toque ja foi entregue");
            Assert.AreEqual(-1L, s.Anuncios.Pagar(null), "sem oferta");
            Assert.AreEqual(-1L, s.Anuncios.Pagar("supply:99"), "ID inventado");
            Assert.AreEqual(0, s.Anuncios.HojeTotal());
        }

        [Test]
        public void D6EFlag_NenhumPlacementAntesDaPrimeiraMelhoria_NemDesligado()
        {
            Sim cedo = Pronto(liberado: false);
            foreach (Anuncio a in Todos) Assert.IsFalse(cedo.Anuncios.Pode(a), a + ": antes da 1a melhoria (D6)");
            foreach (Anuncio a in Todos)
            {
                var t = new Tuning();
                t.Ad(a).Ligado = false;
                Sim s = Pronto(t);
                Assert.IsNull(s.Anuncios.Oferta(a), a + ": desligado no Tuning");
                foreach (Anuncio o in Todos) if (o != a) Assert.IsTrue(s.Anuncios.Pode(o), a + " desligado nao trava " + o);
            }
        }

        [Test]
        public void VipEVelocidade_PeloNucleo_MesmasRegrasDoSim()
        {
            Sim s = Pronto();
            Assert.AreEqual(2L, s.Anuncios.Previa(Anuncio.Vip), "mostra o pacote 2x");
            Assert.AreEqual(2L, Assiste(s, Anuncio.Vip));
            Assert.IsTrue(s.VipActive, "o VIP chamado vem");
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.Vip), "com ele na rua: recusa (CanSummonVip)");
            Assert.AreEqual(2L, s.Anuncios.Previa(Anuncio.Velocidade));
            Assert.AreEqual(2L, Assiste(s, Anuncio.Velocidade), "1o: 2x");
            Assert.AreEqual(s.T.Ad(Anuncio.Velocidade).Valor, s.BoostLeft, "por Ad(Velocidade).Valor s");
            Assert.AreEqual(3L, s.Anuncios.Previa(Anuncio.Velocidade), "o proximo, mostrado antes: 3x");
            Assert.AreEqual(3L, Assiste(s, Anuncio.Velocidade));
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.Velocidade), "teto 3x");
        }

        [Test]
        public void Cofre2x_DobraAqueleResgate_1PorResgate()
        {
            var s = TuningTests.Liberado();
            s.RateEma = 1.0;
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.Cofre), "sem resgate nesta sessao: nada");
            long g = s.ApplyOffline(3600, 10);
            Assert.Greater(g, 0L);
            Assert.AreEqual(g, s.Anuncios.Previa(Anuncio.Cofre), "mostra o valor antes");
            long gold = s.Gold, earned = s.GoldEarned;
            Assert.AreEqual(g, Assiste(s, Anuncio.Cofre));
            Assert.AreEqual(gold + g, s.Gold, "dobrou aquele resgate");
            Assert.AreEqual(earned, s.GoldEarned, "fora do GoldEarned, como o cofre");
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.Cofre), "1 por resgate");
            Assert.AreEqual(0L, s.ApplyOffline(3600, 10), "o mesmo claim nao paga de novo");
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.Cofre), "nem reabre o 2x");
            Assert.Greater(s.ApplyOffline(3600, 20), 0L);
            Assert.IsTrue(s.Anuncios.Pode(Anuncio.Cofre), "resgate novo: 2x novo");
            Assert.IsFalse(Sim.Load(s.Save(Now)).Anuncios.Pode(Anuncio.Cofre), "reabrir: o painel daquele resgate ja passou");
        }

        [Test]
        public void BonusDaEncomenda_Mais50NoPremio_PagoNaEntrega_1PorEncomenda()
        {
            Sim s = Pronto();
            Assert.AreEqual(50L, s.Anuncios.Previa(Anuncio.EncomendaBonus), "mostra +50 antes (50% de 100)");
            Assert.AreEqual(50L, Assiste(s, Anuncio.EncomendaBonus));
            Assert.AreEqual(150L, s.Encomendas.Principal.Premio);
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.EncomendaBonus), "1 por encomenda");
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.EncomendaTroca), "trocar perderia o bonus ja visto");
            Assert.AreEqual(150L, Sim.Load(s.Save(Now)).Encomendas.Principal.Premio, "o premio com bonus vai no save");
            long gold = s.Gold, earned = s.GoldEarned;
            EncTeste.Completa(s);
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(gold + 150, s.Gold, "paga premio + bonus na entrega");
            Assert.AreEqual(earned, s.GoldEarned, "fora do GoldEarned, como o premio");
            s.Encomendas.OrderIn = 0f;
            s.Tick(Dt, 0f, 0f);
            Assert.IsTrue(s.Anuncios.Pode(Anuncio.EncomendaBonus), "a encomenda seguinte tem o dela");
            Assert.AreEqual(15L, s.Anuncios.Previa(Anuncio.EncomendaBonus), "premio minimo 25: +12,5 vira 15 (multiplo de 5)");
        }

        /// <summary>B-CORE-05 (achado do AdWatcherE, BALANCE.md §24/§28): o bonus vale pelo menos EncomendaBonusMinSeg s da renda de agora (o
        /// anuncio custa ~30 s de relogio); 50% do premio quando e' mais.</summary>
        [Test]
        public void BonusDaEncomenda_NuncaMenosQue60sDeRenda()
        {
            Sim s = Pronto();
            s.RateEma = 10.0;
            Assert.AreEqual(600L, s.Anuncios.Previa(Anuncio.EncomendaBonus), "60 s x 10/s > 50% de 100");
            s.Encomendas.Principal.Premio = 2000;
            Assert.AreEqual(1000L, s.Anuncios.Previa(Anuncio.EncomendaBonus), "50% do premio, quando passa dos 60 s");
            Assert.AreEqual(1000L, Assiste(s, Anuncio.EncomendaBonus));
            Assert.AreEqual(3000L, s.Encomendas.Principal.Premio);
            var t = new Tuning { EncomendaBonusMinSeg = 0f };
            Sim k = Pronto(t);
            k.RateEma = 10.0;
            Assert.AreEqual(50L, k.Anuncios.Previa(Anuncio.EncomendaBonus), "knob zerado: so os 50%");
        }

        [Test]
        public void TrocarEncomenda_EntraAProximaDoRodizio_AMostradaAntes_SemSorteio()
        {
            Sim s = Pronto();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            s.RateEma = 4.0;
            s.OrderCount = 2;
            Encomenda e = EncTeste.Abre(s, 2);   // Bulk travado (2 entregues): Common
            Assert.AreEqual(Item.Tool, e.Itens[0], "a natural de indice 2: espada, escudo, FERRAMENTA");
            Encomenda prox = s.Anuncios.ProximaEncomenda();
            Assert.AreEqual(Item.Sword, prox.Itens[0], "mostra a seguinte do rodizio");
            e.Feito[0] = 2;
            Assert.AreEqual(1L, Assiste(s, Anuncio.EncomendaTroca));
            Encomenda nova = s.Encomendas.Principal;
            Assert.AreEqual(prox.Itens[0], nova.Itens[0], "entrou a mostrada");
            Assert.AreEqual(prox.Alvo[0], nova.Alvo[0]);
            Assert.AreEqual(prox.Premio, nova.Premio);
            Assert.AreEqual(prox.Tipo, nova.Tipo);
            Assert.AreEqual(0, nova.Feito[0], "do zero");
            Assert.AreEqual(1, s.Encomendas.Vagas.Count, "no lugar da trocada");
            Assert.AreEqual(2, s.OrderCount, "pulada nao conta como entregue");
            Assert.AreEqual(1, s.Encomendas.Pulos);
            Assert.AreEqual(1, Sim.Load(s.Save(Now)).Encomendas.Pulos, "puladas no save (ord=, 7o campo)");
            Assert.IsTrue(s.Anuncios.Pode(Anuncio.EncomendaTroca), "a nova encomenda tem a troca dela");
            EncTeste.Completa(s);
            s.Tick(Dt, 0f, 0f);
            s.Encomendas.OrderIn = 0f;
            s.Tick(Dt, 0f, 0f);
            Assert.AreEqual(Item.Shield, s.Encomendas.Principal.Itens[0], "o rodizio segue da trocada: indice 4 = escudo");
            var a = Pronto(); var b = Pronto();
            Assert.AreEqual(a.Anuncios.ProximaEncomenda().Itens[0], b.Anuncios.ProximaEncomenda().Itens[0], "sem sorteio: o mesmo estado mostra a mesma");

            // B-CORE-05: com 2 vagas a troca e' so da principal (a mais antiga), no lugar dela; a outra fica
            Sim d = Pronto();
            d.Buy(Upgrade.Shields); d.Buy(Upgrade.Tools);
            d.Renome.Ganha(20);
            EncTeste.Poe(d, Item.Shield, 8, 40);
            Encomenda outra = d.Encomendas.Vagas[1];
            Encomenda vem = d.Anuncios.ProximaEncomenda();
            Assert.AreEqual(Item.Tool, vem.Itens[0], "indice 2 e' ferramenta; a espada da principal sai, o escudo esta na outra vaga");
            Assert.AreEqual(1L, Assiste(d, Anuncio.EncomendaTroca));
            Assert.AreEqual(Item.Tool, d.Encomendas.Vagas[0].Itens[0]);
            Assert.AreSame(outra, d.Encomendas.Vagas[1]);
        }

        [Test]
        public void SupplyDrop_60sDasFornalhas_MostradoAntes_EntregaExata()
        {
            Assert.AreEqual(15L, Pronto().Anuncios.Previa(Anuncio.Supply), "1 fornalha a 4 s: 15 lingotes");
            Sim f = Pronto(); f.Buy(Upgrade.FurnaceSpeed1);
            Assert.AreEqual(24L, f.Anuncios.Previa(Anuncio.Supply), "Fole: 2,5 s -> 24");
            Sim d = Pronto(); d.Buy(Upgrade.FurnaceSpeed1); d.Buy(Upgrade.FurnaceSpeed2); d.Buy(Upgrade.Furnace2);
            Assert.AreEqual(74L, d.Anuncios.Previa(Anuncio.Supply), "2 fornalhas a 1,6 s: 37 + 37");
            Sim s = Pronto();
            Assert.AreEqual(15L, Assiste(s, Anuncio.Supply));
            Assert.AreEqual(15, Sim.Load(s.Save(Now)).Anuncios.Lingotes, "o que falta entregar vai no save");
            int got = 0;
            for (int i = 0; i < 100 && s.Anuncios.Lingotes > 0; i++) { s.Tick(Dt, 0f, 0f); got += s.FurnaceA.Out; s.FurnaceA.Out = 0; }
            Assert.AreEqual(15, got, "entra pela saida da fornalha, ate o teto dela a cada tick");
            Assert.AreEqual(0, s.Crafted[(int)Item.Ingot], "entrega, nao producao (nao conta como forjado)");
        }

        [Test]
        public void Supply_Recarga10MinNoTempoDeJogo_ReabrirNaoLibera()
        {
            Sim s = Pronto();
            Assert.Greater(Assiste(s, Anuncio.Supply), 0L);
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.Supply), "na recarga");
            Run(s, 599f);
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.Supply), "599 s: ainda na recarga");
            string text = s.Save(Now);
            Sim b = Sim.Load(text);
            Assert.AreEqual(s.Anuncios.Recarga[(int)Anuncio.Supply], b.Anuncios.Recarga[(int)Anuncio.Supply], 1e-3f);
            Assert.IsFalse(b.Anuncios.Pode(Anuncio.Supply), "reabrir nao libera");
            Assert.AreEqual(text, b.Save(Now), "save estavel");
            Run(b, 1f + Dt);
            Assert.IsTrue(b.Anuncios.Pode(Anuncio.Supply), "600 s de jogo: libera");
        }

        [Test]
        public void TetoDiario_TotalEPorPlacement_RelogioParaTrasEReabrirNaoZeram()
        {
            var t = new Tuning { AnunciosTetoDia = 2 };
            Sim s = Pronto(t);
            long meioDia = 20 * Calendario.Dia + 12 * 3600;   // dia local 20 (fuso 0)
            s.Metas.Agora(meioDia, 0);
            Assert.Greater(Assiste(s, Anuncio.Cofre), 0L);
            Assert.Greater(Assiste(s, Anuncio.Supply), 0L);
            Assert.AreEqual(2, s.Anuncios.HojeTotal());
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.EncomendaBonus), "teto total do dia: 2");
            Assert.IsNull(s.Anuncios.Oferta(Anuncio.Velocidade), "vale para todo placement");
            s.Metas.Agora(meioDia - 3 * Calendario.Dia, 0);
            Assert.IsFalse(s.Anuncios.Pode(Anuncio.EncomendaBonus), "relogio para tras nao reabre o dia");
            Sim b = Sim.Load(s.Save(Now), t);
            b.Metas.Agora(meioDia + 3600, 0);
            Assert.IsFalse(b.Anuncios.Pode(Anuncio.EncomendaBonus), "reabrir no mesmo dia nao zera");
            b.Metas.Agora(meioDia + Calendario.Dia, 0);
            Assert.IsTrue(b.Anuncios.Pode(Anuncio.EncomendaBonus), "dia seguinte: zera");
            Assert.AreEqual(0, b.Anuncios.HojeTotal());

            Sim x = Pronto();
            for (int i = 0; i < x.T.Ad(Anuncio.EncomendaTroca).TetoDia; i++) Assert.AreEqual(1L, Assiste(x, Anuncio.EncomendaTroca), "troca " + i);
            Assert.IsFalse(x.Anuncios.Pode(Anuncio.EncomendaTroca), "teto do placement: 5 trocas por dia");
            Assert.IsTrue(x.Anuncios.Pode(Anuncio.EncomendaBonus), "o teto de um placement nao trava os outros");
        }

        [Test]
        public void Save_LinhasAd_SoComAnuncioVisto_IdaEVolta_Lixo()
        {
            Sim s = Pronto();
            StringAssert.DoesNotContain("\nad.", s.Save(Now), "sem anuncio visto: o save fica como antes");
            Assiste(s, Anuncio.Supply); Assiste(s, Anuncio.EncomendaBonus);
            string text = s.Save(Now);
            StringAssert.Contains("\nad.supply=1,1,600\n", text);
            StringAssert.Contains("\nad.lingotes=15\n", text);
            Sim b = Sim.Load(text);
            Assert.AreEqual(text, b.Save(Now), "save estavel");
            Sim g = Sim.Load(text + "ad.supply=x,-3,99999\nad.lingotes=-5\nad.nada=1,2,3\nad.vip=1\n");
            Assert.AreEqual(0L, g.Anuncios.Pago[(int)Anuncio.Supply], "lixo vira o padrao");
            Assert.AreEqual(0, g.Anuncios.Hoje[(int)Anuncio.Supply]);
            Assert.AreEqual(600f, g.Anuncios.Recarga[(int)Anuncio.Supply], "recarga no teto do knob");
            Assert.AreEqual(0, g.Anuncios.Lingotes);
        }

        // ------------------------------------------------------------------ intersticial (E-PLAT-03)

        /// <summary>Estado Transition de verdade (tabela do Estados): encomenda saindo, sem painel, anuncio nem pausa.</summary>
        static bool Transicao(bool painel = false) => Estados.De(true, true, false, false, painel, true) == Estado.Transition;

        static Sim Inter(bool tutorialFeito = true)
        {
            var s = new Sim(new Tuning { Interstitial = true });
            if (tutorialFeito) s.Buy(Upgrade.Tools);
            return s;
        }

        [Test]
        public void Intersticial_FlagDesligadaPorPadrao_Nunca()
        {
            var s = new Sim();
            s.Buy(Upgrade.Tools);
            Assert.IsFalse(s.T.Interstitial, "P3/V10: desligada por padrao");
            foreach (bool tr in new[] { true, false })
                foreach (bool acao in new[] { true, false })
                    Assert.AreEqual("flag", s.Anuncios.VetoIntersticial(tr, acao, 1e6f), $"transicao={tr} acao={acao}");
            Assert.IsNull(Inter().Anuncios.VetoIntersticial(Transicao(), false, 1e6f), "ligada, na transicao e sem acao: pode");
        }

        [Test]
        public void Intersticial_Veto_Tutorial()
        {
            Assert.AreEqual("tutorial", Inter(tutorialFeito: false).Anuncios.VetoIntersticial(Transicao(), false, 1e6f), "antes das Ferramentas");
        }

        [Test]
        public void Intersticial_Veto_Acao_ESoNaTransicao()
        {
            Sim s = Inter();
            Assert.AreEqual("acao", s.Anuncios.VetoIntersticial(Transicao(), true, 1e6f), "toque, compra ou golpe em curso");
            Assert.AreEqual("acao", s.Anuncios.VetoIntersticial(Transicao(painel: true), false, 1e6f), "painel aberto: Modal, nao Transition");
            Assert.AreEqual("acao", s.Anuncios.VetoIntersticial(false, false, 1e6f), "jogando (Playing): nunca");
        }

        [Test]
        public void Intersticial_Veto_Cooldown()
        {
            Sim s = Inter();
            s.Anuncios.IntersticialMostrado(1000f);
            Assert.AreEqual("cooldown", s.Anuncios.VetoIntersticial(Transicao(), false, 1000f + s.T.InterstitialCooldown - 1f));
            Assert.IsNull(s.Anuncios.VetoIntersticial(Transicao(), false, 1000f + s.T.InterstitialCooldown));
        }

        [Test]
        public void Intersticial_Veto_CapPorSessao()
        {
            Sim s = Inter();
            float t = 0f;
            for (int i = 0; i < s.T.InterstitialMaxSessao; i++) { t += 1000f; Assert.IsNull(s.Anuncios.VetoIntersticial(Transicao(), false, t), "o " + i); s.Anuncios.IntersticialMostrado(t); }
            Assert.AreEqual("cap", s.Anuncios.VetoIntersticial(Transicao(), false, t + 1000f), "teto da sessao");
            Assert.IsNull(Sim.Load(s.Save(Now), s.T).Anuncios.VetoIntersticial(Transicao(), false, 1e6f), "sessao nova (processo novo): zera");
        }

        [Test]
        public void Intersticial_Veto_RemoveAds()
        {
            Sim s = Inter();
            s.Anuncios.RemoveAds = true;
            Assert.AreEqual("remove_ads", s.Anuncios.VetoIntersticial(Transicao(), false, 1e6f));
            Assert.IsTrue(s.Anuncios.Pode(Anuncio.Supply), "remove_ads nao tira o rewarded (opcional)");
        }
    }
}
