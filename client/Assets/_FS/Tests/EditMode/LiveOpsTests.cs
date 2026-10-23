using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// D-CORE-03/04 (LIVEOPS_V1 §2, §3, §5.7-§5.9): calendario as 13:00 UTC, evento novo so com dados, requisito (FTUE intocado), modificadores
    /// pelo RemoteConfig (travas) que voltam no fim, cofre igual, pontos e limiares 1x (save/load, matar antes de gravar, reset), relogio para
    /// tras, Pass (retroativo 1x, teto de encomenda, RESGATAR 1x, premium nunca, fim de temporada), save antigo e as regras do validator.
    /// Asserts separados com tipo exato (NUnit 3.5 do Unity).
    /// </summary>
    public class LiveOpsTests
    {
        const long Fuso = -3 * 3600;
        const long H = 3600, Dia = LiveOpsDefs.Dia;

        static EventDef Evt(string fim) => LiveOpsDefs.Eventos.First(e => e.Id.EndsWith(fim, StringComparison.Ordinal));
        static EventDef FF => Evt("_forge_frenzy");
        static EventDef Armeiro => Evt("_armorers_challenge");
        static PassDef S1 => LiveOpsDefs.Passes[0];
        static PassDef S2 => LiveOpsDefs.Passes[1];

        /// <summary>Ferramentas compradas, 10 encomendas, Renome 1 e taxa de 2 ouro/s: cumpre o requisito de todo evento do D0/D1 sem Joalheria.</summary>
        static Sim Aberta()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            s.OrderCount = 10;
            s.Renome.Ganha(RenomeDefs.Niveis[1].Limiar);
            s.RateEma = 2.0;
            return s;
        }

        /// <summary>A ordem da View: metas (semeia e vira o dia local) e depois o LiveOps.</summary>
        static void Agora(Sim s, long t) { s.Metas.Agora(t, Fuso); s.LiveOps.Agora(t); }

        static void Vende(Sim s, Item i, int n) { for (int k = 0; k < n; k++) s.LiveOps.On(Ev.Sold, (int)i, 10); }

        static MissaoEvento M(string id, string ev, long alvo, int pontos, params string[] f) => new MissaoEvento(id, ev, alvo, pontos, f);

        /// <summary>Copia rasa de um EventDef (o teste quebra a copia, nunca o dado real).</summary>
        static EventDef Copia(EventDef e, Action<EventDef> muda = null)
        {
            var c = new EventDef
            {
                Id = e.Id, Temporada = e.Temporada, Abre = e.Abre, Tema = e.Tema, Encomenda = e.Encomenda, Template = e.Template, Inicio = e.Inicio,
                Dias = e.Dias, Renome = e.Renome, Encomendas = e.Encomendas, Reserva = e.Reserva, Requer = e.Requer, Ofertas = e.Ofertas,
                Missoes = e.Missoes, Trilha = e.Trilha, Modificadores = e.Modificadores,
            };
            muda?.Invoke(c);
            return c;
        }

        /// <summary>O criterio do D-CORE-03: um evento so de dados, na folga de 1 dia entre a Royal Week e o Forge Frenzy.</summary>
        static EventDef Novo() => new EventDef
        {
            Id = "ev_s1d6_teste_dados", Template = Template.ForgeFrenzy, Temporada = "s1_royal_commission", Inicio = 6, Dias = 1,
            Missoes = new[] { M("vende5", "product_sold", 5, 700) },
            Trilha = new[] { new Marco(50, new RewardDef(TipoPremio.Ouro, 30)), new Marco(700, new RewardDef(TipoPremio.PassXp, 100), new RewardDef(TipoPremio.Cosmetico, 0, "cos_ev_teste_dados")) },
            Modificadores = new[] { new Modificador("PatienceBase", 1.1f) },
        };

        // ------------------------------------------------------------------ calendario e dados

        [Test]
        public void Calendario_13hUtc_JanelasDosEventosEDasTemporadas()
        {
            DateTimeOffset t0 = DateTimeOffset.FromUnixTimeSeconds(LiveOpsDefs.T0);
            Assert.AreEqual(13, t0.Hour, "virada as 13:00 UTC (10:00 de Brasilia)");
            Assert.AreEqual(DayOfWeek.Monday, t0.DayOfWeek);
            Sim s = Aberta();
            Agora(s, FF.Comeca - 1);
            Assert.IsNull(s.LiveOps.Evento, "um segundo antes: nada");
            Agora(s, FF.Comeca);
            Assert.AreSame(FF, s.LiveOps.Evento);
            Assert.AreEqual(6 * Dia, s.LiveOps.Restante);
            Agora(s, FF.Termina - 1);
            Assert.AreSame(FF, s.LiveOps.Evento);
            Agora(s, FF.Termina);
            Assert.IsNull(s.LiveOps.Evento, "6 dias e o 7o de respiro");
            Assert.IsNull(s.LiveOps.PasseEm(LiveOpsDefs.T0 - 1));
            Assert.AreSame(S1, s.LiveOps.PasseEm(LiveOpsDefs.T0));
            Assert.AreSame(S1, s.LiveOps.PasseEm(LiveOpsDefs.T0 + 35 * Dia - 1));
            Assert.AreSame(S2, s.LiveOps.PasseEm(LiveOpsDefs.T0 + 35 * Dia), "temporadas emendadas");
            // a Royal Week esperava o Orders 2.0 (order_complete Royal, B-CORE-05): agora abre o calendario
            Sim r = Aberta();
            Agora(r, LiveOpsDefs.T0 + H);
            Assert.AreSame(Evt("_royal_week"), r.LiveOps.Evento);
            Assert.IsNull(r.LiveOps.Falta(Evt("_rare_ore_rush")), "o Rare Ore Rush roda: o GrabTime virou campo do Tuning");
        }

        [Test]
        public void Dados_DezEventosComOsNoveCampos_MissoesSomam700_ReservasProntas()
        {
            EventDef[] evs = LiveOpsDefs.Eventos;
            Assert.AreEqual(10, evs.Length);
            Assert.AreEqual(10, evs.Select(e => e.Id).Distinct().Count());
            foreach (EventDef e in evs)
            {
                Assert.AreEqual(700, e.Missoes.Sum(x => x.Pontos), e.Id);
                Assert.AreEqual(1000, e.Trilha.Last().Pontos, e.Id);
                Assert.AreEqual(6, e.Dias, e.Id);
                Assert.IsNotNull(e.Tema, e.Id);
                Assert.IsNotEmpty(e.Modificadores, e.Id);
                Assert.IsEmpty(e.Ofertas, e.Id);
                Assert.AreEqual(500L, LiveOpsDefs.XpTrilha(e), e.Id);
            }
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(Template)).Cast<Template>().Where(t => t != Template.ArcaneWeek), evs.Select(e => e.Template).Distinct());
            Assert.AreEqual(2, evs.Count(e => e.Reserva));
            var s = new Sim();
            CollectionAssert.AreEqual(new[] { "ev_s1w1_royal_week", "ev_s1w2_forge_frenzy", "ev_s1w3_armorers_challenge", "ev_s1w4_hunt_sunblade", "ev_s1w5_forge_frenzy_b",
                "ev_s2w1_blacksmith_festival", "ev_s2w2_rare_ore_rush", "ev_s2w3_merchant_caravan", "ev_s2w4_hunt_ember_crown", "ev_s2w5_royal_week_b" }, evs.Where(e => s.LiveOps.Falta(e) == null).Select(e => e.Id),
                "os 10 rodam (B-CORE-08: os Hunts com o QualidadeEvento; o Rare Ore Rush com o GrabTime no Tuning)");
            Assert.IsEmpty(LiveOpsDefs.Validar(LiveOpsDefs.Eventos, LiveOpsDefs.Passes).ToList());
            Assert.AreEqual(30, S1.Niveis);
            Assert.AreEqual(18000L, S1.XpTotal);
            Assert.AreEqual(1170, S1.Free.Where(r => r.Tipo == TipoPremio.Ouro).Sum(r => r.Valor), "LIVEOPS_V1 §3.4");
            Assert.AreEqual(44, S1.Free.Where(r => r.Tipo == TipoPremio.Renome).Sum(r => r.Valor));
            Assert.AreEqual(6, S2.Free.Count(r => r.Tipo == TipoPremio.Cosmetico));
        }

        [Test]
        public void EventoNovo_SoComDados_ValidaEntraPagaESai()
        {
            EventDef novo = Novo();
            EventDef[] evs = LiveOpsDefs.Eventos.Concat(new[] { novo }).ToArray();
            Assert.IsEmpty(LiveOpsDefs.Validar(evs, LiveOpsDefs.Passes).ToList(), "o validator aceita o evento novo");
            Sim s = Aberta();
            s.LiveOps.Eventos = evs;
            Agora(s, novo.Comeca + H);
            Assert.AreSame(novo, s.LiveOps.Evento);
            Assert.AreEqual(33f, s.T.PatienceBase, 1e-4f, "modificador do dado");
            long gold = s.Gold, earned = s.GoldEarned;
            Vende(s, Item.Sword, 5);
            Agora(s, novo.Comeca + 2 * H);
            Assert.AreEqual(700, s.LiveOps.Pontos);
            Assert.AreEqual(gold + 60, s.Gold, "limiar 50: 30 s x 2/s");
            Assert.AreEqual(earned, s.GoldEarned, "fora do GoldEarned");
            Assert.IsTrue(s.Metas.Ledger.Contains("e:ev_s1d6_teste_dados:700"), "cosmetico: a posse e' o ID");
            Assert.AreEqual(100L, s.Metas.PassXp);
            Assert.AreEqual(100L, s.LiveOps.Xp, "o XP da trilha entra no Pass");
            Agora(s, novo.Termina);
            Assert.AreSame(FF, s.LiveOps.Evento, "o Forge Frenzy comeca quando o novo acaba");
            Assert.AreEqual(30f * 1.15f, s.T.PatienceBase, 1e-3f, "o knob voltou a 30 antes do x1,15 do seguinte (nao acumula)");
        }

        // ------------------------------------------------------------------ requisito e modificadores

        [Test]
        public void Requisito_ForaDoFtue_BotPrimeiros10MinIguaisTickATick()
        {
            // evento sem requisito alem das Ferramentas e com modificador forte, ativo desde o 1o segundo
            EventDef forte = Copia(Novo(), e => { e.Dias = 1; e.Modificadores = new[] { new Modificador("Price", 1.25f), new Modificador("ClientInterval", 0.75f) }; });
            Sim a = new Sim(), b = new Sim();
            b.LiveOps.Eventos = new[] { forte };
            Bot ba = new Bot(), bb = new Bot();
            const float dt = 1f / 30f;
            for (int k = 0; k < 10 * 60 * 30; k++)
            {
                ba.Step(a, dt); bb.Step(b, dt);
                if (k % 30 == 0) Agora(b, forte.Comeca + k / 30);
                if (a.Gold != b.Gold || a.Sales != b.Sales) Assert.Fail($"tick {k}: ouro {a.Gold} x {b.Gold}");
            }
            Assert.IsFalse(b.Bought[(int)Upgrade.Tools], "o F2P compra as Ferramentas depois dos 10 min");
            Assert.IsNull(b.LiveOps.Evento);
            Assert.AreEqual(a.Player.Pos.ToString(), b.Player.Pos.ToString());
            b.Buy(Upgrade.Shields); b.Buy(Upgrade.Tools);
            Agora(b, forte.Comeca + 700);
            Assert.AreSame(forte, b.LiveOps.Evento, "com as Ferramentas entra");
            Assert.AreEqual(13, b.T.Price[(int)Item.Sword], "10 x 1,25 arredonda como o RemoteConfig");
        }

        [Test]
        public void Requisito_RenomeEEncomendas_SoEntraQuemCumpre()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            Agora(s, FF.Comeca + H);
            Assert.IsNull(s.LiveOps.Evento, "Forge Frenzy pede 10 encomendas");
            s.OrderCount = 10;
            Agora(s, FF.Comeca + 2 * H);
            Assert.AreSame(FF, s.LiveOps.Evento);
            var r = new Sim();
            r.Buy(Upgrade.Shields); r.Buy(Upgrade.Tools);
            Agora(r, Armeiro.Comeca + H);
            Assert.IsNull(r.LiveOps.Evento, "Armorer's pede Renome 1");
            r.Renome.Ganha(20);
            Agora(r, Armeiro.Comeca + 2 * H);
            Assert.AreSame(Armeiro, r.LiveOps.Evento);
        }

        [Test]
        public void Modificadores_PeloRemoteConfig_VoltamNoFim_SobreOConfigRemoto()
        {
            Tuning t = new Tuning();
            RemoteConfig.Aplicar(t, "{\"id\":\"ab\",\"versao\":1,\"Price\":[0,0,10,30,16,60,40]}", false);
            var s = new Sim(t);
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            s.Renome.Ganha(20);
            Agora(s, Armeiro.Comeca + H);
            Assert.AreSame(Armeiro, s.LiveOps.Evento);
            Assert.AreEqual(36, s.T.Price[(int)Item.Shield], "x1,2 sobre o preco do config remoto (30)");
            Assert.AreEqual(10, s.T.Price[(int)Item.Sword], "so o escudo");
            Assert.AreEqual(1.35f, s.T.ShieldTimeMul, 1e-4f);
            Assert.AreEqual(5f * 1.35f, s.ShieldBench.Time, 1e-3f, "o Recompute leva o tempo novo a bancada");
            Agora(s, Armeiro.Termina);
            Assert.IsNull(s.LiveOps.Evento);
            Assert.AreEqual(30, s.T.Price[(int)Item.Shield], "volta ao do config, nao ao do build");
            Assert.AreEqual(1.5f, s.T.ShieldTimeMul);
            Assert.AreEqual(7.5f, s.ShieldBench.Time, 1e-3f);
        }

        [Test]
        public void Modificadores_EventoNuncaFuraGuardrail_EOValidatorRecusa()
        {
            EventDef mau = Copia(Novo(), e => e.Modificadores = new[] { new Modificador("OfflineMaxSeconds", 10f), new Modificador("VipMin", 0.01f) });
            Sim s = Aberta();
            s.LiveOps.Eventos = new[] { mau };
            Agora(s, mau.Comeca + H);
            Assert.AreSame(mau, s.LiveOps.Evento);
            Assert.AreEqual(Balance.OfflineCapSeconds, s.T.OfflineMaxSeconds, "trava de 2 h do RemoteConfig");
            Assert.AreEqual(24f, s.T.VipMin, 1e-4f, "faixa generica: padrao / 10");
            List<string> v = LiveOpsDefs.Validar(new[] { mau }, LiveOpsDefs.Passes).ToList();
            Assert.IsTrue(v.Exists(x => x.Contains("OfflineMaxSeconds") && x.Contains("proibidos")), string.Join(" | ", v));
            Assert.IsTrue(v.Exists(x => x.Contains("VipMin") && x.Contains("fora da faixa")), string.Join(" | ", v));
        }

        [Test]
        public void Cofre_DuranteOEvento_PagaIgualAoDeFora()
        {
            Sim dentro = Aberta(), fora = Aberta();
            Agora(dentro, FF.Comeca + H);
            Assert.AreSame(FF, dentro.LiveOps.Evento);
            dentro.RateEma = fora.RateEma = 3.0;
            long g = fora.ApplyOffline(3600, 1);
            Assert.Greater(g, 0L);
            Assert.AreEqual(g, dentro.ApplyOffline(3600, 1), "modificador so online");
        }

        // ------------------------------------------------------------------ pontos e trilha

        [Test]
        public void Pontos_MissaoELimiar_UmaVez_SaveLoad_MatarAntesDeGravar_Reset()
        {
            Sim s = Aberta();
            long t = FF.Comeca + H;
            Agora(s, t);
            Assert.AreEqual(1L, s.LiveOps.Feito(FF.Missoes.First(x => x.Id == "dias3")), "o dia de hoje conta");
            Vende(s, Item.Sword, 10);
            string antes = s.Save(t);
            long gold = s.Gold;
            s.LiveOps.On(Ev.VipServed, (int)Item.Sword, 30); s.LiveOps.On(Ev.VipServed, (int)Item.Sword, 30);
            Agora(s, t + 10);
            Assert.AreEqual(60, s.LiveOps.Pontos, "vip2 = 60");
            Assert.AreEqual(gold + 60, s.Gold, "limiar 50");
            Assert.AreEqual(100, s.LiveOps.Proximo.Pontos);
            Assert.IsTrue(s.Metas.Ledger.Contains("em:ev_s1w2_forge_frenzy:vip2"));
            Assert.IsTrue(s.Metas.Ledger.Contains("e:ev_s1w2_forge_frenzy:50"));
            Agora(s, t + 20);
            Assert.AreEqual(gold + 60, s.Gold, "de novo: nada");

            Sim r = Sim.Load(s.Save(t + 20));
            Agora(r, t + 30);
            Assert.AreSame(FF, r.LiveOps.Evento);
            Assert.AreEqual(60, r.LiveOps.Pontos);
            Assert.AreEqual(s.Gold, r.Gold);
            Assert.AreEqual(10L, r.LiveOps.Feito(FF.Missoes.First(x => x.Id == "espadas100")), "progresso no meio volta");
            Assert.AreEqual(s.Save(t + 20), Sim.Load(s.Save(t + 20)).Save(t + 20), "save -> load -> save igual (ev.*/ps.*)");

            // matou o app entre o credito e a gravacao: o save velho nao tem ouro nem ID; paga uma vez so
            Sim v = Sim.Load(antes);
            Agora(v, t + 30);
            v.LiveOps.On(Ev.VipServed, (int)Item.Sword, 30); v.LiveOps.On(Ev.VipServed, (int)Item.Sword, 30);
            Agora(v, t + 40); Agora(v, t + 50);
            Assert.AreEqual(s.Gold, v.Gold);

            // reset (DM §133): o evento sai e volta com o mesmo id: contadores do zero, nada pago de novo
            r.T.Eventos = false;
            Agora(r, t + 40);
            Assert.IsNull(r.LiveOps.Evento);
            Assert.AreEqual(Tuning.Padrao.PatienceBase, r.T.PatienceBase, "flag desligada devolve os knobs");
            r.T.Eventos = true;
            long g2 = r.Gold;
            Agora(r, t + 50);
            Assert.AreSame(FF, r.LiveOps.Evento);
            Assert.AreEqual(0L, r.LiveOps.Feito(FF.Missoes.First(x => x.Id == "espadas100")));
            Assert.AreEqual(60, r.LiveOps.Pontos);
            Assert.AreEqual(g2, r.Gold);
        }

        /// <summary>GDD §13 / A-PLAT-09: event_enter e event_complete (e os pontos) chegam ao analytics remoto pela Telemetria, so depois da tela
        /// de idade e na ordem, com o evento no `a` e o limiar no `b`: a mesma linha que o Game loga (LiveOps.Diario).</summary>
        [Test]
        public void Telemetria_EntradaEMarcoDoEvento_ChegamAoRemoto()
        {
            var remoto = new ProvedorMemoria();
            var t = new Telemetria(new AnalyticsMemoria(), remoto, "abcd1234", "0.7.0", "base@1");
            Sim s = Aberta();
            long agora = FF.Comeca + H;
            void Drena() { foreach (var n in s.LiveOps.Novidades) { var (a, b) = s.LiveOps.Diario(n); t.Log(n.Tipo, s.Time, a, b); } s.LiveOps.Novidades.Clear(); }
            Agora(s, agora); Drena();
            s.LiveOps.On(Ev.VipServed, (int)Item.Sword, 30); s.LiveOps.On(Ev.VipServed, (int)Item.Sword, 30);
            Agora(s, agora + 10); Drena();
            Assert.IsEmpty(remoto.Enviados, "antes da tela de idade nada sai");
            t.Liberar(Faixa.Adulto, false);
            Evento entra = remoto.Enviados.Single(e => e.Nome == "event_enter");
            Evento marco = remoto.Enviados.Single(e => e.Nome == "event_complete");
            Assert.AreEqual(FF.Id, entra.A);
            Assert.AreEqual((FF.Id, "50"), (marco.A, marco.B), "o limiar cruzado");
            Assert.Less(entra.Seq, marco.Seq);
            Assert.AreEqual("60", remoto.Enviados.Last(e => e.Nome == "event_points").B, "o total de pontos (vip2 = 60)");
        }

        [Test]
        public void RelogioParaTras_NaoReabreONemPagaDeNovo()
        {
            Sim s = Aberta();
            Agora(s, FF.Comeca + H);
            Agora(s, FF.Termina + H);
            Assert.IsNull(s.LiveOps.Evento);
            Agora(s, FF.Comeca + 2 * H);   // relogio voltou para dentro da janela
            Assert.IsNull(s.LiveOps.Evento, "o relogio gravado e' o maximo");
            Assert.AreEqual(FF.Termina + H, s.LiveOps.Relogio);
            Sim r = Sim.Load(s.Save(FF.Comeca));
            Agora(r, FF.Comeca + 2 * H);
            Assert.IsNull(r.LiveOps.Evento, "e pelo save");
        }

        [Test]
        public void CompraEContratacao_SemNadaAComprar_CompletamSozinhas()
        {
            var s = new Sim();
            foreach (BlueprintDef d in s.Projetos.Defs) s.Projetos.Ganha(d.Id, d.Sources[0].Kind);   // B-CORE-07: niveis IV-V e bigorna longa
            for (int pass = 0; pass < 2; pass++)   // o luxo so depois da producao completa
                for (int u = 0; u < Upgrades.Count; u++) if (Upgrades.IsLuxury(u) == (pass == 1)) s.Buy((Upgrade)u);
            Assert.AreEqual(Upgrades.Count, s.UpgradesBought);
            s.OrderCount = 10; s.RateEma = 2.0;
            Agora(s, FF.Comeca + H);
            Assert.IsTrue(s.LiveOps.Paga(FF.Missoes.First(x => x.Id == "melhoria3")), "nada mais a comprar");
            EventDef fest = Evt("_blacksmith_festival");
            Agora(s, fest.Comeca + H);
            Assert.AreSame(fest, s.LiveOps.Evento);
            MissaoEvento contrata = fest.Missoes.First(x => x.Id == "contrata1");
            Assert.IsFalse(s.LiveOps.Paga(contrata), "ainda ha Equipe a contratar e nivel a subir (B-CORE-04)");
            s.Gold = 100000;
            Assert.IsTrue(s.Equipe.Comprar(Enumerable.Range(0, s.Equipe.Defs.Length).First(s.Equipe.AVenda)));
            Agora(s, fest.Comeca + 2 * H);
            Assert.IsTrue(s.LiveOps.Paga(contrata), "contratar ou subir de nivel conta");
            Assert.IsFalse(s.LiveOps.Paga(fest.Missoes.First(x => x.Id == "golpe5")));
        }

        [Test]
        public void QualidadeEGolpe_ContamNasMissoes()
        {
            Sim s = Aberta();
            Agora(s, Armeiro.Comeca + H);
            s.LiveOps.On(Ev.Graded, s.ShieldBench.Index, (int)Quality.Fine);
            s.LiveOps.On(Ev.Graded, s.AnvilA.Index, (int)Quality.Superior);
            Assert.AreEqual(1L, s.LiveOps.Feito(Armeiro.Missoes.First(x => x.Id == "fina_escudos20")), "so o escudo");
            s.Player.Pos = s.ShieldBench.InAt;
            s.ShieldBench.Ins[0] = s.ShieldBench.InCap;
            s.Tick(1f / 30f, 0f, 0f);
            Assert.IsTrue(s.Strike(true), "golpe de verdade no Sim");
            Assert.AreEqual(1L, s.LiveOps.Feito(Armeiro.Missoes.First(x => x.Id == "golpe_escudos5")));
        }

        // ------------------------------------------------------------------ Orders 2.0 (B-CORE-05)

        /// <summary>Entrega pelo caminho real (Encomendas.Entregar no Tick) uma encomenda `ordId` de 1 espada.</summary>
        static Encomenda Entrega(Sim s, string ordId)
        {
            OrderDef d = OrderDefs.All.First(o => o.Id == ordId);
            var e = new Encomenda(d, s.Encomendas.Abertas++, new[] { Item.Sword }, new[] { 1 }, d.Type == OrderType.Event ? 0 : 25, 0, 0f, null);
            s.Encomendas.Vagas.Add(e);
            EncTeste.Completa(e);
            s.Tick(1f / 30f, 0f, 0f);
            Assert.IsFalse(s.Encomendas.Vagas.Contains(e), ordId + " entregue");
            return e;
        }

        [Test]
        public void OrdersDois_RoyalWeekRoda_OrderCompletePorTipo_EncomendaEventDaPontos_XpDoPasseUmaVez()
        {
            Sim s = Aberta();
            s.FirstSaleTime = 1f;
            EventDef rw = Evt("_royal_week");
            Assert.IsNull(s.LiveOps.Falta(rw), "o Orders 2.0 deu o order_complete");
            Agora(s, rw.Comeca + H);
            Assert.AreSame(rw, s.LiveOps.Evento);
            MissaoEvento royal2 = rw.Missoes.First(x => x.Id == "royal2");
            Entrega(s, "ord_common_old_forge");
            Assert.AreEqual(0L, s.LiveOps.Feito(royal2), "Common nao conta como Royal");
            Entrega(s, "ord_royal_old_forge");
            Entrega(s, "ord_royal_old_forge");
            Assert.AreEqual(2L, s.LiveOps.Feito(royal2));
            Assert.AreEqual(30L, s.LiveOps.Xp, "XP do Passe: 10 por entrega, 1x (o Ev.OrderDone)");
            Agora(s, rw.Comeca + H + 1);
            Assert.IsTrue(s.LiveOps.Paga(royal2));
            int pontos = s.LiveOps.Pontos;
            Assert.AreEqual(royal2.Pontos, pontos);
            long xp = s.LiveOps.Xp;
            s.Tick(1f / 30f, 0f, 0f);   // tick sem entrega: abre
            Encomenda ev = s.Encomendas.Vagas.Find(v => v.Tipo == OrderType.Event);
            Assert.IsNotNull(ev, "a vaga da encomenda do evento abriu sozinha");
            Assert.AreEqual(rw.Encomenda, ev.Def.Id);
            EncTeste.Completa(ev);
            s.Tick(1f / 30f, 0f, 0f);
            Assert.AreEqual(pontos + 15, s.LiveOps.Pontos, "+15 da encomenda Event");
            Assert.AreEqual(xp + 10L, s.LiveOps.Xp, "a Event e' uma entrega: 10 de XP, nao dobra");
            Sim r = Sim.Load(s.Save(rw.Comeca + H + 2));
            Agora(r, rw.Comeca + H + 3);
            Assert.AreEqual(pontos + 15, r.LiveOps.Pontos, "os pontos da Event vao no save (ev.c)");
            xp = s.LiveOps.Xp;
            for (int k = 0; k < 9; k++) Entrega(s, "ord_common_old_forge");
            Assert.AreEqual(xp + 60L, s.LiveOps.Xp, "teto de 100 por dia local contando a Event (4 + 6 entregas pagam XP)");
        }

        [Test]
        public void OrdersDois_MerchantCaravanRoda_BulkEMixed_PremioX125_VoltaNoFim()
        {
            Sim s = Aberta();
            s.FirstSaleTime = 1f;
            EventDef mc = Evt("_merchant_caravan");
            Assert.IsNull(s.LiveOps.Falta(mc), "requer a ord_bulk_old_forge, que existe");
            float antes = s.T.OrderRewardSeconds;
            Agora(s, mc.Comeca + H);
            Assert.AreSame(mc, s.LiveOps.Evento);
            Assert.AreEqual(antes * 1.25f, s.T.OrderRewardSeconds, 0.0001f, "premio das encomendas x1,25 (RemoteConfig.Valores)");
            Entrega(s, "ord_bulk_old_forge");
            Entrega(s, "ord_bulk_old_forge");
            Entrega(s, "ord_mixed_old_forge");
            Assert.AreEqual(2L, s.LiveOps.Feito(mc.Missoes.First(x => x.Id == "bulk2")));
            Assert.AreEqual(1L, s.LiveOps.Feito(mc.Missoes.First(x => x.Id == "mixed2")));
            Assert.AreEqual(3L, s.LiveOps.Feito(mc.Missoes.First(x => x.Id == "enc10")), "order_done conta todas");
            Agora(s, mc.Termina);
            Assert.IsNull(s.LiveOps.Evento);
            Assert.AreEqual(antes, s.T.OrderRewardSeconds, "o knob volta no fim");
        }

        /// <summary>Bot F2P por `seg` s de jogo com o relogio do LiveOps andando de `t0` (1x por segundo, como a View e o econsim).</summary>
        static void Joga(Sim s, Bot bot, long t0, float seg)
        {
            float ini = s.Time; int ult = -1;
            while (s.Time - ini < seg)
            {
                bot.Step(s, 1f / 30f);
                int k = (int)(s.Time - ini);
                if (k != ult) { ult = k; Agora(s, t0 + k); }
            }
        }

        /// <summary>
        /// LIVEOPS_V1 §2.2/§6: a trilha de evento fecha no dia 4-5 de 6, nunca numa sessao. Para cada evento que roda: (a) o bot F2P jogando
        /// 60 min seguidos no 1o dia nao chega aos 1.000 (a missao play_day segura 100 e a encomenda Event da 15); (b) o F2P de referencia
        /// (§1: 2 sessoes de 10 min por dia, cofre entre elas, sem anuncio nem golpe) fecha no dia 4 ou 5. Antes do evento o jogador ja jogou
        /// 20 min por dia desde o T0, ate 2 h (a Royal Week e' o D0: comeca do zero). Os 7 eventos em paralelo (cada Sim tem o seu Tuning).
        /// </summary>
        [Test]
        public void TrilhaDeEvento_NuncaNumaSessaoDe60Min_OF2PDeReferenciaFechaNoDia4a5()
        {
            EventDef[] evs = LiveOpsDefs.Eventos.Where(e => LiveOpsDefs.Falta(e, Items.All) == null).ToArray();
            Assert.AreEqual(10, evs.Length);   // B-CORE-08: os 2 Hunts; o Rare Ore Rush com o GrabTime no Tuning
            var aq = new Sim(); var ab = new Bot();
            while (aq.Time < 2 * 3600f) ab.Step(aq, 1f / 30f);
            string jogou = aq.Save(LiveOpsDefs.T0);
            var sessao = new int[evs.Length]; var dia = new int[evs.Length];
            System.Threading.Tasks.Parallel.For(0, evs.Length, i =>
            {
                EventDef e = evs[i];
                Sim Inicio() => e.Inicio == 0 ? new Sim() : Sim.Load(jogou);
                Sim s = Inicio();
                Joga(s, new Bot(), e.Comeca + H, 3600f);
                sessao[i] = s.LiveOps.Evento == e ? s.LiveOps.Pontos : -1;
                Sim r = Inicio(); var b = new Bot();
                long ultimo = e.Comeca - H;
                dia[i] = 7;
                for (int d = 0; d < 6 && dia[i] == 7; d++)
                    foreach (long h in new[] { H, 8 * H })
                    {
                        long t = e.Comeca + d * Dia + h;
                        r.ApplyOffline(t - ultimo, t);
                        Joga(r, b, t, 600f);
                        ultimo = t + 600;
                        if (r.LiveOps.Evento == e && r.LiveOps.Proximo == null) { dia[i] = d + 1; break; }
                    }
            });
            // "evento: pontos em 60 min seguidos (-1 = nao entrou) / dia em que a referencia fecha (7 = nao fecha)"
            string tabela = string.Join(" | ", evs.Select((e, i) => $"{e.Id}: {sessao[i]} / dia {dia[i]}"));
            TestContext.WriteLine(tabela);
            Assert.IsTrue(evs.Select((e, i) => i).All(i => sessao[i] >= 1 && sessao[i] < 1000 && dia[i] >= 4 && dia[i] <= 5), tabela);
        }

        /// <summary>
        /// Raia H (LIVEOPS_V1 §4.5, LiveOps): trilha de evento fechada = o ultimo limiar pago (999 pontos nao bastam), eventos em que entrou (1 por
        /// evento: reabrir o save no meio nao conta outra entrada) e a trilha free do Passe = o nivel 30 resgatado (alcancar nao basta; o fim da
        /// temporada resgata sozinho). Cada uma paga 1x pelo ledger.
        /// </summary>
        [Test]
        public void Conquistas_EventoEPasse_PeloLedger_UmaVez()
        {
            Sim s = Aberta();
            MetaDef fim = MetasDefs.Achar("ach_evento1"), entra = MetasDefs.Achar("ach_eventos4"), passe = MetasDefs.Achar("ach_passe_free");
            Agora(s, FF.Comeca + H);
            Assert.AreSame(FF, s.LiveOps.Evento);
            Assert.AreEqual(1L, s.Metas.Conquista(entra).Feito);
            Sim r = Sim.Load(s.Save(FF.Comeca + H));
            Agora(r, FF.Comeca + 2 * H);
            Assert.AreSame(FF, r.LiveOps.Evento);
            Assert.AreEqual(1L, r.Metas.Conquista(entra).Feito, "reabrir o mesmo evento nao conta outra entrada");
            s.LiveOps.PontosEncomenda(FF.Id, 999);
            Agora(s, FF.Comeca + H + 1);
            Assert.AreEqual(0L, s.Metas.Conquista(fim).Feito, "999 pontos: falta o ultimo limiar");
            s.LiveOps.PontosEncomenda(FF.Id, 1);
            Agora(s, FF.Comeca + H + 2);
            Assert.IsTrue(s.Metas.Conquista(fim).Pronta);
            long xp = s.Metas.PassXp;
            Assert.AreEqual(0L, s.Metas.ResgatarConquista(fim.Id), "XP 100, sem ouro");
            Assert.AreEqual(-1L, s.Metas.ResgatarConquista(fim.Id));
            Assert.AreEqual(xp + 100L, s.Metas.PassXp);
            Agora(s, Armeiro.Comeca + H);
            Assert.AreSame(Armeiro, s.LiveOps.Evento);
            Assert.AreEqual(2L, s.Metas.Conquista(entra).Feito);
            s.LiveOps.Ganha(S1.XpTotal);
            Assert.AreEqual(30, s.LiveOps.Nivel);
            for (int n = 1; n < 30; n++) s.LiveOps.Resgatar(n);
            Assert.IsFalse(s.Metas.Conquista(passe).Pronta, "o nivel 30 ainda nao resgatado");
            Assert.GreaterOrEqual(s.LiveOps.Resgatar(30), 0L);
            Assert.IsTrue(s.Metas.Conquista(passe).Pronta);
            Assert.AreEqual(0L, s.Metas.ResgatarConquista(passe.Id), "insignia: posse pelo ID");
            Assert.AreEqual(-1L, s.Metas.ResgatarConquista(passe.Id));
            Sim r2 = Sim.Load(s.Save(Armeiro.Comeca + 2 * H));
            Assert.IsTrue(r2.Metas.Conquista(passe).Paga, "a insignia vai no save");
            Assert.AreEqual(-1L, r2.Metas.ResgatarConquista(passe.Id));
        }

        // ------------------------------------------------------------------ Pass

        [Test]
        public void Passe_XpDasMetas_RetroativoUmaVez_EsperaAteATemporada()
        {
            Sim s = Aberta();
            s.Metas.Agora(LiveOpsDefs.T0 - Dia, Fuso);
            s.Metas.PassXp = 1300;   // XP que as metas ja guardavam (#47)
            s.LiveOps.Agora(LiveOpsDefs.T0 - Dia);
            Assert.IsNull(s.LiveOps.Passe, "antes do T0: o XP espera");
            Assert.AreEqual(0L, s.LiveOps.XpVisto);
            Agora(s, LiveOpsDefs.T0 + H);
            Assert.AreSame(S1, s.LiveOps.Passe);
            Assert.AreEqual(1300L, s.LiveOps.Xp, "retroativo");
            Assert.AreEqual(2, s.LiveOps.Nivel);
            Sim r = Sim.Load(s.Save(LiveOpsDefs.T0 + H));
            Agora(r, LiveOpsDefs.T0 + 2 * H);
            Assert.AreEqual(1300L, r.LiveOps.Xp, "reabrir nao paga de novo");
            r.Metas.PassXp += 100;
            Agora(r, LiveOpsDefs.T0 + 3 * H);
            Assert.AreEqual(1400L, r.LiveOps.Xp);
            Sim velho = Sim.Load(s.Save(LiveOpsDefs.T0 + H).Replace("ps.passe=", "lixo="));   // save sem ps.*
            velho.Metas.PassXp = 1300;
            Agora(velho, LiveOpsDefs.T0 + 2 * H);
            Assert.AreEqual(1300L, velho.LiveOps.Xp, "save antigo: o XP das metas entra uma vez");
        }

        [Test]
        public void Passe_XpDeEncomenda_Teto100PorDiaLocal()
        {
            Sim s = Aberta();
            long t = LiveOpsDefs.T0 + H;
            Agora(s, t);
            for (int k = 0; k < 15; k++) s.LiveOps.On(Ev.OrderDone, (int)Item.Sword, 25);
            Assert.AreEqual(100L, s.LiveOps.Xp, "10 por encomenda ate 100 no dia (V20)");
            Agora(s, t + Dia);
            s.LiveOps.On(Ev.OrderDone, (int)Item.Sword, 25);
            Assert.AreEqual(110L, s.LiveOps.Xp, "dia local novo");
            var cedo = new Sim();
            Agora(cedo, t);
            cedo.LiveOps.On(Ev.OrderDone, (int)Item.Sword, 25);
            Assert.AreEqual(0L, cedo.LiveOps.Xp, "antes das Ferramentas o Pass nem aparece");
            Assert.IsFalse(cedo.LiveOps.PasseVisivel);
        }

        [Test]
        public void Passe_ResgatarUmaVez_TetoNoNivel30_PremiumNuncaPaga()
        {
            Sim s = Aberta();
            long t = LiveOpsDefs.T0 + H;
            Agora(s, t);
            s.LiveOps.Ganha(4 * 600);
            Assert.AreEqual(4, s.LiveOps.Nivel);
            Assert.AreEqual(4, s.LiveOps.Prontos());
            long gold = s.Gold, earned = s.GoldEarned, ren = s.Renome.Total;
            Assert.AreEqual(60L, s.LiveOps.Resgatar(1), "ouro 30 s x 2/s");
            Assert.AreEqual(-1L, s.LiveOps.Resgatar(1));
            Assert.AreEqual(-1L, s.LiveOps.Resgatar(5), "nao alcancado");
            Assert.AreEqual(0L, s.LiveOps.Resgatar(2));
            Assert.AreEqual(ren + 3, s.Renome.Total, "Renome +3");
            Assert.AreEqual(0L, s.LiveOps.Resgatar(4), "cosmetico: posse pelo ID");
            Assert.IsTrue(s.Metas.Ledger.Contains("p:pass_s1_royal_commission:4:f"));
            Assert.AreEqual(gold + 60, s.Gold);
            Assert.AreEqual(earned, s.GoldEarned);
            Assert.AreEqual(1, s.LiveOps.Prontos());
            Sim r = Sim.Load(s.Save(t));
            Agora(r, t + 10);
            Assert.AreEqual(-1L, r.LiveOps.Resgatar(1), "reabrir nao paga de novo");
            r.LiveOps.Ganha(1000000);
            Assert.AreEqual(18000L, r.LiveOps.Xp, "sem nivel bonus");
            Assert.AreEqual(30, r.LiveOps.Nivel);
            for (int n = 1; n <= 30; n++) r.LiveOps.Resgatar(n);
            Assert.IsFalse(r.Metas.Ledger.Any(x => x.EndsWith(":p", StringComparison.Ordinal)), "premium desligado ate a Wave E");
        }

        /// <summary>LIVEOPS_V1 §3.5 (atras do Tuning.PassePremium): o premium da temporada e' um produto da loja; com a flag desligada nao fica a
        /// venda e nada premium paga, mesmo com o recibo; ligada, paga sozinho todo nivel ja alcancado (comprou no meio = na hora), 1x pelo ID
        /// p:{pass}:{n}:p, sem ouro, XP nem Renome; save/load nao repete; a temporada que acaba paga o que faltou.</summary>
        [Test]
        public void PassePremium_AtrasDaFlag_PagaOsAlcancadosUmaVez_SemProgressao()
        {
            Sim s = Aberta();
            var loja = new LojaFalsa(null);
            s.Compras.Conectar(loja);
            loja.Iniciar(Compras.Todos, r => { s.Compras.Receber(r); s.Compras.Concluir(() => true, loja.Confirmar); }, (p, m) => { });
            string id = "iap_" + S1.Id;
            long t = S1.Comeca + H;
            Agora(s, t);
            s.LiveOps.Ganha(3 * S1.XpNivel);
            Assert.AreEqual(3, s.LiveOps.Nivel);
            Assert.IsFalse(Compras.AVenda(s.T).Any(d => d.Id == id), "flag desligada: o Pass nao esta a venda");
            Assert.IsTrue(s.Compras.Iniciar(id)); loja.Comprar(id);   // recibo que chegou mesmo assim (restore de outro aparelho)
            Assert.IsTrue(s.Compras.Tem(Compras.PossePremium(S1.Id)));
            Agora(s, t + 10);
            Assert.IsFalse(s.LiveOps.Premium);
            Assert.IsFalse(s.Metas.Ledger.Contains(LiveOps.TxPremium(S1, 1)), "flag desligada: nenhum nivel premium");

            s.T.PassePremium = true;
            Assert.IsTrue(Compras.AVenda(s.T).Any(d => d.Id == id));
            long gold = s.Gold, xp = s.Metas.PassXp, nivel = s.Renome.Nivel;
            Agora(s, t + 20);
            for (int n = 1; n <= 3; n++) Assert.IsTrue(s.Metas.Ledger.Contains(LiveOps.TxPremium(S1, n)), "alcancado: " + n);
            Assert.IsFalse(s.Metas.Ledger.Contains(LiveOps.TxPremium(S1, 4)));
            Assert.AreEqual((gold, xp, nivel), (s.Gold, s.Metas.PassXp, (long)s.Renome.Nivel), "o pago nunca progride");
            Assert.AreEqual(0, s.LiveOps.Prontos() - 3, "a trilha free continua no botao, a parte");

            s.LiveOps.Ganha(S1.XpNivel);
            Agora(s, t + 30);
            Assert.IsTrue(s.Metas.Ledger.Contains(LiveOps.TxPremium(S1, 4)), "subiu de nivel com o premium: paga na hora");
            Sim r = Sim.Load(s.Save(t + 30));
            r.Compras.Conectar(loja); r.T.PassePremium = true;
            long g2 = r.Gold;
            Agora(r, t + 40);
            Assert.AreEqual(g2, r.Gold);
            Assert.AreEqual(s.Save(t + 30), Sim.Load(s.Save(t + 30)).Save(t + 30), "save -> load -> save igual");

            r.LiveOps.Ganha(S1.XpNivel);   // nivel 5 alcancado no ultimo instante da temporada
            r.T.Passe = false;             // nem o XP corre mais: o fim da temporada paga o premium que faltou
            Agora(r, S1.Termina + H);
            Assert.IsTrue(r.Metas.Ledger.Contains(LiveOps.TxPremium(S1, 5)), "a temporada que acaba paga o premium alcancado");
        }

        [Test]
        public void Passe_FimDaTemporada_PagaOAlcancado_EOXpRecomeca()
        {
            Sim s = Aberta();
            Agora(s, LiveOpsDefs.T0 + H);
            s.LiveOps.Ganha(3 * 600 + 50);
            s.LiveOps.Resgatar(1);
            long gold = s.Gold;
            Agora(s, S2.Comeca + H);
            Assert.AreSame(S2, s.LiveOps.Passe);
            Assert.AreEqual(0L, s.LiveOps.Xp);
            Assert.IsTrue(s.Metas.Ledger.Contains("p:pass_s1_royal_commission:3:f"), "o nivel 3 nao resgatado pagou sozinho");
            Assert.AreEqual(gold + 60, s.Gold, "nivel 3 = ouro 30 s");
            Assert.IsFalse(s.Metas.Ledger.Contains("p:pass_s1_royal_commission:4:f"), "nao alcancado nao paga");
        }

        [Test]
        public void Save_BotSemRelogioIgual_SaveAntigoSemLinhas()
        {
            var bot = new Bot();
            var s = new Sim();
            for (float t = 0f; t < 12 * 60f; t += 1f / 30f) bot.Step(s, 1f / 30f);
            string txt = s.Save(1000);
            StringAssert.DoesNotContain("\nev.", txt, "sem relogio o save fica igual ao de antes");
            StringAssert.DoesNotContain("\nps.", txt);
            Sim r = Sim.Load(txt);
            Assert.AreEqual(0L, r.LiveOps.Relogio);
            Assert.IsNull(r.LiveOps.Evento);
            Assert.IsNull(r.LiveOps.Passe);
        }

        [Test]
        public void Fechamento_F2PFechaATrilhaFreePertoDoDia28()
        {
            double xp = LiveOpsDefs.XpDiaF2P(S1, LiveOpsDefs.Eventos, LiveOpsDefs.EncomendasDiaF2P);
            Assert.AreEqual(651.4, xp, 0.1, "LIVEOPS_V1 §3.3: 300 + 200 + 80 + 2.500/35");
            double desenho = LiveOpsDefs.DiaFechamento(S1, LiveOpsDefs.Eventos, LiveOpsDefs.EncomendasDiaF2P);
            double hoje = LiveOpsDefs.DiaFechamento(S1, LiveOpsDefs.Eventos, 30 / 3.0, true);   // 30 encomendas em 60 min no F2P (econsim, Orders 2.0), so os eventos prontos
            Assert.That(desenho, Is.InRange(26.0, 30.0), $"desenho: dia {desenho:0.0}");
            Assert.That(hoje, Is.InRange(26.0, 30.0), $"hoje: dia {hoje:0.0}");
            Assert.That(LiveOpsDefs.DiaFechamento(S2, LiveOpsDefs.Eventos, LiveOpsDefs.EncomendasDiaF2P), Is.InRange(26.0, 30.0));
        }

        // ------------------------------------------------------------------ validator

        static List<string> Valida(EventDef[] evs = null, PassDef[] ps = null, bool premium = false) =>
            LiveOpsDefs.Validar(evs ?? LiveOpsDefs.Eventos, ps ?? LiveOpsDefs.Passes, premium).ToList();

        static EventDef[] Troca(EventDef velho, EventDef novo) => LiveOpsDefs.Eventos.Select(e => e == velho ? novo : e).ToArray();

        static PassDef CopiaPasse(PassDef p, Action<PassDef> muda)
        {
            var c = new PassDef { Id = p.Id, Temporada = p.Temporada, Tema = p.Tema, Inicio = p.Inicio, Dias = p.Dias, XpNivel = p.XpNivel,
                XpEncomenda = p.XpEncomenda, TetoEncomendaDia = p.TetoEncomendaDia, Free = (RewardDef[])p.Free.Clone(), Premium = (RewardDef[])p.Premium.Clone() };
            muda(c);
            return c;
        }

        static void Acusa(List<string> erros, string trecho) => Assert.IsTrue(erros.Exists(e => e.Contains(trecho)), $"esperava \"{trecho}\" em: {string.Join(" | ", erros)}");

        [Test]
        public void Validator_DefeitosInjetados_Acusa()
        {
            Acusa(Valida(LiveOpsDefs.Eventos.Concat(new[] { Copia(FF, e => e.Inicio = 70) }).ToArray()), "ID repetido");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Inicio = 3))), "sobrepoe");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Inicio = 33))), "fora da janela");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Modificadores = new[] { new Modificador("OfflineFactor", 2f) }))), "proibidos");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Modificadores = new[] { new Modificador("Price", 1.5f) }))), "fora da faixa");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Modificadores = new[] { new Modificador("VipMin", 1.2f) }))), "fora da faixa");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Ofertas = new[] { "of_pontos" }))), "oferta antes da Wave E");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Missoes = FF.Missoes.Concat(new[] { M("perde", "client_left", 3, 10) }).ToArray()))), "pune ou exige anuncio");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Missoes = FF.Missoes.Concat(new[] { M("x", "nada_disso", 3, 10) }).ToArray()))), "desconhecido");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Missoes = FF.Missoes.Concat(new[] { M("x", "product_sold", 3, 10, "machado") }).ToArray()))), "filtro \"machado\"");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Missoes = FF.Missoes.Concat(new[] { M("vips", "vip_served", 40, 10) }).ToArray()))), "nao fecham sem anuncio");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Missoes = FF.Missoes.Concat(new[] { M("royal3", "order_complete", 3, 10, "royal") }).ToArray()))), "no maximo 2");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Missoes = FF.Missoes.Take(2).ToArray()))), "menos de 70%");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Trilha = FF.Trilha.Reverse().ToArray()))), "nao sobe");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Renome = 2))), "D0/D1");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Trilha = FF.Trilha.Concat(new[] { new Marco(1100, new RewardDef(TipoPremio.Gems, 50)) }).ToArray()))), "nao e' recompensa gratis");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Trilha = FF.Trilha.Concat(new[] { new Marco(1100, new RewardDef(TipoPremio.Blueprint, 0, "bp_x")) }).ToArray()))), "sem fallback");
            EventDef ffb = Evt("_forge_frenzy_b");
            Acusa(Valida(Troca(ffb, Copia(ffb, e => e.Trilha = FF.Trilha))), "mais de uma fonte");
            Acusa(Valida(Troca(ffb, Copia(ffb, e => e.Requer = new[] { "bp_que_nao_existe" }))), "reserva com o que ainda nao existe");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Encomenda = "ord_ev_royal_week"))), "nao e' a OrderDef Event deste evento");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Missoes = FF.Missoes.Where(x => x.Evento != "play_day").ToArray()))), "jogue em 3 dias");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Encomenda = "ord_common_old_forge"))), "nao e' a OrderDef Event deste evento");
            Acusa(Valida(Troca(FF, Copia(FF, e => e.Missoes = FF.Missoes.Concat(new[] { M("ctr1", "contract_complete", 1, 10) }).ToArray()))), "9 missoes");   // raia H: 8 linhas
            Acusa(Valida(ps: new[] { CopiaPasse(S1, p => p.Premium[1] = new RewardDef(TipoPremio.Ouro, 30)), S2 }), "o pago nunca progride");
            Acusa(Valida(ps: new[] { CopiaPasse(S1, p => p.Premium[1] = new RewardDef(TipoPremio.Blueprint, 0, "bp_x", new RewardDef(TipoPremio.Ouro, 60))), S2 }), "o pago nunca progride");
            Acusa(Valida(ps: new[] { CopiaPasse(S1, p => p.Premium[1] = new RewardDef(TipoPremio.PassXp, 100)), S2 }), "o pago nunca progride");
            Acusa(Valida(ps: new[] { CopiaPasse(S1, p => p.Free[4] = null), S2 }), "nada obrigatorio so no premium");
            Acusa(Valida(ps: new[] { CopiaPasse(S1, p => p.Free[4] = new RewardDef(TipoPremio.Gems, 10)), S2 }), "nao e' recompensa gratis");
            Acusa(Valida(ps: new[] { CopiaPasse(S1, p => p.Dias = 40), S2 }), "40 dias");
            Acusa(Valida(ps: new[] { CopiaPasse(S1, p => p.XpNivel = 1000), S2 }), "nao fecha");
            Acusa(Valida(ps: new[] { S1, CopiaPasse(S2, p => p.Inicio = 40) }), "nao emenda");
            Acusa(Valida(premium: true), "premium ligada antes da Wave E");
            Acusa(Valida(ps: new[] { CopiaPasse(S1, p => p.Free = p.Free.Select(r => r.Tipo == TipoPremio.Ouro ? new RewardDef(TipoPremio.Ouro, 120) : r).ToArray()), S2 }), "acima de 180");
        }

        // ------------------------------------------------------------------ textos (View)

        [Test]
        public void Textos_DosEventosEDoPasse_ExistemECabemNosTresIdiomas()
        {
            var s = new Sim();
            try
            {
                foreach (Idioma i in new[] { Idioma.Pt, Idioma.En, Idioma.Es })
                {
                    Loc.Atual = i;
                    foreach (EventDef e in LiveOpsDefs.Eventos)
                    {
                        string nome = Textos.NomeEvento(e);
                        StringAssert.DoesNotContain("[", nome, $"{i}: {e.Id}");
                        Assert.LessOrEqual(nome.Length, Textos.EventoMax, $"{i}: {nome}");
                        foreach (MissaoEvento x in e.Missoes)
                        {
                            if (s.LiveOps.Falta(e) != null) continue;   // so o que aparece hoje (order_complete ganha frase com o Orders 2.0)
                            string m = Textos.Missao(x);
                            StringAssert.DoesNotContain("[", m, $"{i}: {e.Id}/{x.Id}");
                            Assert.LessOrEqual(m.Length, Textos.MetaMax, $"{i}: {e.Id}/{x.Id} \"{m}\"");
                        }
                        foreach (Marco mc in e.Trilha)
                        {
                            string p = Textos.Premios(mc.Premios, s);
                            StringAssert.DoesNotContain("[", p, $"{i}: {e.Id}/{mc.Pontos}");
                            Assert.LessOrEqual(p.Length, Textos.PremioMax, $"{i}: {e.Id}/{mc.Pontos} \"{p}\"");
                        }
                    }
                    foreach (PassDef p in LiveOpsDefs.Passes)
                    {
                        StringAssert.DoesNotContain("[", Textos.NomePasse(p), $"{i}: {p.Id}");
                        foreach (RewardDef r in p.Free)
                        {
                            string t = Textos.Premios(new[] { r }, s);
                            StringAssert.DoesNotContain("[", t, $"{i}: {p.Id}");
                            Assert.LessOrEqual(t.Length, Textos.PremioMax, $"{i}: {p.Id} \"{t}\"");
                        }
                    }
                    foreach (long seg in new[] { 59L, 3599L, 86399L, 6 * Dia })
                        Assert.LessOrEqual(Loc.T("liveops.chip", Textos.Qtd(1000), Textos.Tempo(seg)).Length, Textos.ChipMax, $"{i}: chip");
                    Assert.LessOrEqual(Loc.T("aviso.evento", Textos.NomeEvento(Evt("_hunt_ember_crown"))).Length, Textos.AvisoMax, i.ToString());
                    Assert.LessOrEqual(Loc.T("aviso.evento_marco", 1000).Length, Textos.AvisoMax, i.ToString());
                    Assert.LessOrEqual(Loc.T("aviso.passe", 30).Length, Textos.AvisoMax, i.ToString());
                }
                Loc.Atual = Idioma.Pt;
                Assert.AreEqual("Jogue em 3 dias do evento", Textos.Missao(FF.Missoes.First(x => x.Id == "dias3")));
                Assert.AreEqual("Venda 100 espadas", Textos.Missao(FF.Missoes.First(x => x.Id == "espadas100")));
                Assert.AreEqual("Acerte 5 golpes em escudos", Textos.Missao(Armeiro.Missoes.First(x => x.Id == "golpe_escudos5")));
                Assert.AreEqual("Suba 1 nível: Escudos", Textos.Missao(Armeiro.Missoes.First(x => x.Id == "nivel_escudos")));
                Assert.AreEqual("6d 0h", Textos.Tempo(6 * Dia));
                Assert.AreEqual("Cumpra 1 contrato real", Textos.Missao(Evt("_royal_week").Missoes.First(x => x.Id == "contrato1")));   // raia H
                Loc.Atual = Idioma.En;
                Assert.AreEqual("Craft 20 shields (Fine+)", Textos.Missao(Armeiro.Missoes.First(x => x.Id == "fina_escudos20")));
                Assert.AreEqual("Fulfill 1 Royal Contract", Textos.Missao(Evt("_royal_week").Missoes.First(x => x.Id == "contrato1")));
            }
            finally { Loc.Atual = Idioma.Pt; }
        }
    }
}
