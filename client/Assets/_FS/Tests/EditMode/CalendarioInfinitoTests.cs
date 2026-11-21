using System;
using System.Linq;
using FS.Core;
using NUnit.Framework;

namespace FS.Tests
{
    /// <summary>
    /// Calendario de LiveOps infinito (2026-11-21, known issue P1 "o calendario acaba em 2027-01-11"): depois do dia 70 o ciclo de 10 semanas
    /// (S1 + S2 e os 10 eventos) se repete com ID novo ({id}_r{c}), deterministico, um evento por vez, temporadas emendadas, e a reprise entra,
    /// paga de novo e sobrevive ao save como o evento em dados. NUnit 3.5: tipos exatos.
    /// </summary>
    public class CalendarioInfinitoTests
    {
        const long Dia = LiveOpsDefs.Dia, H = 3600, Fuso = -3 * 3600;
        static EventDef FF => LiveOpsDefs.Eventos.First(e => e.Id == "ev_s1w2_forge_frenzy");

        static bool Pronto(EventDef e) => LiveOpsDefs.Falta(e, Items.All) == null;

        [Test]
        public void DepoisDoDia70_PassNuncaVazio_EventosRepetemOCiclo()
        {
            Assert.AreEqual(LiveOpsDefs.CicloDias, LiveOpsDefs.Passes.Max(p => p.Inicio + p.Dias), "o ciclo = o fim do calendario em dados");
            Assert.That(LiveOpsDefs.Eventos.All(LiveOpsDefs.Repete), "todo evento em dados cabe no ciclo");
            for (long t = LiveOpsDefs.T0; t < LiveOpsDefs.T0 + 1000 * Dia; t += 6 * H)
            {
                PassDef p = LiveOpsDefs.PasseEm(LiveOpsDefs.Passes, t);
                Assert.IsNotNull(p, "sem Pass no dia " + (t - LiveOpsDefs.T0) / Dia);
                int c = LiveOpsDefs.CicloDe(t);
                long t0 = t - c * LiveOpsDefs.CicloDias * Dia;
                Assert.AreEqual(LiveOpsDefs.PasseEm(LiveOpsDefs.Passes, t0).Id + (c > 0 ? LiveOpsDefs.Sufixo(c) : ""), p.Id);
                EventDef e = LiveOpsDefs.EventoEm(LiveOpsDefs.Eventos, t, Pronto), e0 = LiveOpsDefs.EventoEm(LiveOpsDefs.Eventos, t0, Pronto);
                Assert.AreEqual(e0 == null ? null : e0.Id + (c > 0 ? LiveOpsDefs.Sufixo(c) : ""), e?.Id, "dia " + (t - LiveOpsDefs.T0) / Dia);
            }
            Assert.IsNotNull(LiveOpsDefs.EventoEm(LiveOpsDefs.Eventos, LiveOpsDefs.T0 + (70 + 7) * Dia + H, Pronto), "a 2a semana do ciclo 1 tem evento");
        }

        [Test]
        public void CalendarioGerado_Valida_E_MesmaInstancia()
        {
            Assert.IsEmpty(LiveOpsDefs.ValidarCalendario(LiveOpsDefs.Eventos, LiveOpsDefs.Passes, 30).ToList());
            EventDef r = LiveOpsDefs.Reprise(FF, 3);
            Assert.AreSame(r, LiveOpsDefs.Reprise(FF, 3));
            Assert.AreSame(r, LiveOpsDefs.EventoPorId(LiveOpsDefs.Eventos, "ev_s1w2_forge_frenzy_r3"));
            Assert.AreEqual(("ev_s1w2_forge_frenzy", 3, FF.Inicio + 210), (r.Base, r.Ciclo, r.Inicio));
            Assert.IsNull(LiveOpsDefs.EventoPorId(LiveOpsDefs.Eventos, "ev_nao_existe_r2"));
            Assert.IsNull(LiveOpsDefs.EventoPorId(LiveOpsDefs.Eventos, "ev_s1w2_forge_frenzy_rx"));
            Assert.AreSame(LiveOpsDefs.Reprise(LiveOpsDefs.Passes[1], 2), LiveOpsDefs.PassePorId(LiveOpsDefs.Passes, "pass_s2_ember_festival_r2"));
            // o validator acusa sobreposicao no calendario gerado
            var mau = LiveOpsDefs.Eventos.Select(e => e).ToArray();
            var x = new EventDef { Id = "ev_teste_sobrepoe", Template = Template.ForgeFrenzy, Temporada = FF.Temporada, Inicio = FF.Inicio + 1, Missoes = FF.Missoes, Trilha = FF.Trilha };
            Assert.That(LiveOpsDefs.ValidarCalendario(mau.Concat(new[] { x }).ToArray(), LiveOpsDefs.Passes, 2).Any(m => m.Contains("sobrepoe")));
        }

        static Sim Aberta()
        {
            var s = new Sim();
            s.Buy(Upgrade.Shields); s.Buy(Upgrade.Tools);
            s.OrderCount = 10;
            s.Renome.Ganha(RenomeDefs.Niveis[1].Limiar);
            s.RateEma = 2.0;
            return s;
        }

        static void Agora(Sim s, long t) { s.Metas.Agora(t, Fuso); s.LiveOps.Agora(t); }

        /// <summary>Um ano depois do T0 o jogador entra na reprise do Forge Frenzy, a trilha paga de novo (ID novo), o save guarda a reprise e o
        /// Pass da reprise corre; a encomenda Event e' a da origem.</summary>
        [Test]
        public void Reprise_Entra_PagaDeNovo_SaveLoad_PassCorre()
        {
            Sim s = Aberta();
            Agora(s, FF.Comeca + H);
            Assert.AreSame(FF, s.LiveOps.Evento);
            s.LiveOps.Ganha(0);
            EventDef r = LiveOpsDefs.Reprise(FF, 5);
            Agora(s, r.Comeca + H);
            Assert.AreSame(r, s.LiveOps.Evento, "entra na reprise do ciclo 5");
            Assert.AreEqual("ev_s1w2_forge_frenzy_r5", s.LiveOps.Evento.Id);
            Assert.AreEqual("pass_s1_royal_commission_r5", s.LiveOps.Passe.Id);
            Assert.IsTrue(s.LiveOps.PasseVisivel);
            Assert.AreEqual(OrderDefs.All.First(o => o.EventId == FF.Id).EventId, r.Base, "a encomenda Event e' a da origem");
            s.LiveOps.Ganha(s.LiveOps.Passe.XpNivel * 2);
            Assert.AreEqual(2, s.LiveOps.Nivel);
            Assert.That(s.LiveOps.Resgatar(1) >= 0, "a reprise do Pass paga (ID novo)");

            Sim b = Sim.Load(s.Save(r.Comeca + 2 * H));
            b.Metas.Agora(r.Comeca + 2 * H, Fuso); b.LiveOps.Agora(r.Comeca + 2 * H);
            Assert.AreSame(r, b.LiveOps.Evento, "o save guarda a reprise");
            Assert.AreEqual(2, b.LiveOps.Nivel);
            Assert.IsTrue(b.LiveOps.Resgatado(1));
            Agora(b, r.Termina + H);
            Assert.IsNull(b.LiveOps.Evento, "a reprise sai no fim da janela dela");
        }

        /// <summary>A troca do Pass S2 (fim do dia 70) para a reprise da S1 paga sozinha o nivel alcancado e nao resgatado, como entre S1 e S2.</summary>
        [Test]
        public void FimDaS2_ReprisePagaOQueFicou()
        {
            Sim s = Aberta();
            PassDef s2 = LiveOpsDefs.Passes[1];
            Agora(s, s2.Comeca + H);
            Assert.AreSame(s2, s.LiveOps.Passe);
            s.LiveOps.Ganha(s2.XpNivel);
            Assert.AreEqual(1, s.LiveOps.Prontos());
            Agora(s, s2.Termina + H);
            Assert.AreEqual("pass_s1_royal_commission_r1", s.LiveOps.Passe.Id, "2027-01-11 em diante: nunca sem Pass");
            Assert.IsTrue(s.Metas.Ledger.Contains(LiveOps.TxNivel(s2, 1)), "o nivel 1 da S2 pagou sozinho");
            Assert.AreEqual(0L, s.LiveOps.Xp);
        }
    }
}
