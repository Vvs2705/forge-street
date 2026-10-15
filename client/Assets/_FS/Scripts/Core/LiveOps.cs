using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// D-CORE-03/04 (LIVEOPS_V1 §2, §3, §5.8): eventos e Masterwork Pass sobre o relogio e o ledger das metas. O nucleo nao le relogio: a View
    /// chama Agora(nowUnix) depois da Metas.Agora (que semeia os contadores e o dia local). Relogio = MAIOR instante ja visto (relogio para tras
    /// nada abre nem reabre). Evento: o do calendario (13:00 UTC, LiveOpsDefs.T0) que esta pronto (LiveOpsDefs.Falta) e cujo requisito o
    /// jogador cumpriu (sempre as Ferramentas: o FTUE nao ve evento nem modificador). Ao entrar, os contadores das missoes zeram e os
    /// modificadores entram no Tuning pelo RemoteConfig.Valores (as MESMAS travas: evento nunca fura guardrail); ao sair, os knobs voltam.
    /// Pontos de evento = trilha nao gastavel: soma das missoes pagas (ID em:{evento}:{missao} no ledger das metas). Limiar cruzado credita 1x
    /// (e:{evento}:{pontos}) pelo Metas.Paga: ouro em s de renda FORA do GoldEarned, XP do Pass e Renome. Reentrar no mesmo evento nao paga de novo.
    /// Pass: a temporada do calendario; XP = o que as metas e as trilhas guardam (Metas.PassXp, delta desde XpVisto: retroativo 1x para quem ja
    /// tinha) + encomenda entregue (teto por dia local, V20). Nivel = XP / XpNivel; RESGATAR paga a free do nivel 1x (p:{passe}:{n}:f); a
    /// temporada seguinte paga sozinha o que ficou alcancado e nao resgatado. Premium nunca paga (LiveOpsDefs.PremiumLigado).
    /// Save: linhas ev.* e ps.* so depois da 1a Agora (bot e testes sem relogio: o save fica igual ao de antes).
    /// </summary>
    public sealed class LiveOps
    {
        readonly Sim _sim;
        /// <summary>Conteudo desta partida (padrao: LiveOpsDefs). O teste injeta um evento novo so com dados.</summary>
        public EventDef[] Eventos = LiveOpsDefs.Eventos;
        public PassDef[] Passes = LiveOpsDefs.Passes;
        /// <summary>Maior unix ja visto (0 = nunca: nada roda e nada vai no save).</summary>
        public long Relogio;
        /// <summary>Evento em que o jogador entrou (null = nenhum).</summary>
        public EventDef Evento;
        readonly Dictionary<string, long> _cont = new Dictionary<string, long>();
        long _dia = long.MinValue;
        Dictionary<FieldInfo, object> _antes;   // knobs de antes dos modificadores (null = nada aplicado)
        /// <summary>Passe da temporada (fica o ultimo ate a seguinte comecar: da para resgatar o que ficou), XP dele, quanto do Metas.PassXp ja
        /// entrou e o XP de encomenda no dia local DiaEnc.</summary>
        public PassDef Passe;
        public long Xp, XpVisto, XpEncHoje, DiaEnc = long.MinValue;
        bool _corre;
        /// <summary>O que aconteceu (a View drena: aviso, diario e currency_source). Tipo: event_enter, event_points (Arg = missao), event_complete
        /// (Arg = limiar), pass_level (Arg = nivel); Ouro = o que pagou.</summary>
        public readonly List<(string Tipo, string Id, string Arg, long Ouro)> Novidades = new List<(string, string, string, long)>();

        public LiveOps(Sim sim) { _sim = sim; }

        Metas M => _sim.Metas;

        // ------------------------------------------------------------------ relogio e calendario

        public void Agora(long nowUnix)
        {
            if (nowUnix > Relogio) Relogio = nowUnix;
            if (!M.Iniciado) return;
            EventDef e = _sim.T.Eventos ? NoCalendario(Relogio) : null;
            if (Evento != null && Evento != e) Sai();
            if (Evento == null && e != null && Requisitos(e)) Entra(e);
            if (Evento != null) Avanca();
            PassoDoPasse();
        }

        /// <summary>Evento pronto cuja janela contem `t`, ou null.</summary>
        public EventDef NoCalendario(long t) => Array.Find(Eventos, e => e.Comeca <= t && t < e.Termina && Falta(e) == null);

        public string Falta(EventDef e) => LiveOpsDefs.Falta(e, _sim.ItemDefs);

        /// <summary>Ferramentas (FTUE intocado) + o que o evento pede (B-CORE-08: o Blueprint do Requer possuido, ex. o da Primeira Lamina no Hunt
        /// da Sunblade; OrderDef no Requer so precisa existir, LiveOpsDefs.Falta). Tudo monotonico: quem entrou fica ate o fim.</summary>
        public bool Requisitos(EventDef e) => M.Aberto && (e.Abre == null || _sim.Bought[Upgrades.IndexOf(e.Abre)])
            && _sim.Renome.Nivel >= e.Renome && _sim.OrderCount >= e.Encomendas && Array.TrueForAll(e.Requer, r => ProjetosDefs.IndexOf(r) < 0 || _sim.Projetos.Tem(r));

        /// <summary>s ate o fim do evento ativo (0 = nenhum).</summary>
        public long Restante => Evento == null ? 0 : Math.Max(0, Evento.Termina - Relogio);

        /// <summary>Dev/foto (-evento): entra no evento pronto da janela sem esperar o requisito. Nada alem disso muda.</summary>
        public bool Forca(EventDef e)
        {
            if (!M.Iniciado || Evento != null || e == null || Falta(e) != null || Relogio < e.Comeca || Relogio >= e.Termina) return false;
            Entra(e);
            Avanca();
            return true;
        }

        void Entra(EventDef e)
        {
            Evento = e; _cont.Clear(); _dia = long.MinValue;
            Novidades.Add(("event_enter", e.Id, "", 0));
            M.Registra("event_enter");   // raia H: ach_eventos4 (reentrar no mesmo evento depois do load nao passa por aqui)
        }

        void Sai()
        {
            if (_antes != null)
            {
                foreach (KeyValuePair<FieldInfo, object> kv in _antes) kv.Key.SetValue(_sim.T, kv.Value);
                _antes = null;
                _sim.Recompute();
            }
            Evento = null; _cont.Clear();
        }

        /// <summary>Modificadores do evento sobre os knobs de AGORA (o Tuning ja com o RemoteConfig), pelas travas do RemoteConfig. Dois no mesmo
        /// knob compoem. O Recompute leva os tempos novos as estacoes.</summary>
        void Aplica(EventDef e)
        {
            var valores = new Dictionary<string, object>();
            _antes = new Dictionary<FieldInfo, object>();
            foreach (Modificador m in e.Modificadores)
            {
                FieldInfo f = LiveOpsDefs.Campo(m.Knob);
                if (!_antes.ContainsKey(f)) _antes[f] = f.GetValue(_sim.T);
                object v = valores.TryGetValue(m.Knob, out object ja) ? ja : Numeros(f.GetValue(_sim.T));
                if (v is List<object> l)
                {
                    int item = m.Item == null ? -1 : Items.IndexOf(m.Item, _sim.ItemDefs);
                    for (int i = 0; i < l.Count; i++) if (item < 0 || i == item) l[i] = Muda((double)l[i], m);
                }
                else v = Muda((double)v, m);
                valores[m.Knob] = v;
            }
            RemoteConfig.Valores(_sim.T, valores, false);
            _sim.Recompute();
        }

        static double Muda(double d, Modificador m) => m.Soma ? d + m.Valor : d * m.Valor;

        /// <summary>Valor do campo como o JSON do RemoteConfig o le: numero = double, array = lista nova de double.</summary>
        static object Numeros(object v)
        {
            if (!(v is Array a)) return Convert.ToDouble(v, CultureInfo.InvariantCulture);
            var l = new List<object>(a.Length);
            foreach (object o in a) l.Add(Convert.ToDouble(o, CultureInfo.InvariantCulture));
            return l;
        }

        // ------------------------------------------------------------------ missoes e trilha

        void Avanca()
        {
            EventDef e = Evento;
            if (_antes == null) Aplica(e);
            if (M.Dia > _dia) { _dia = M.Dia; Add("play_day", 1); }   // dia local com o evento aberto ("jogar em 3 dias")
            foreach (MissaoEvento x in e.Missoes)
                if ((Feito(x) >= x.Alvo || Esgotada(x)) && M.Paga(TxMissao(e, x), 0, 0, 0) >= 0)
                    Novidades.Add(("event_points", e.Id, x.Id, 0));
            int p = PontosDe(e);
            foreach (Marco m in e.Trilha)
            {
                if (p < m.Pontos) break;
                long g = Credita("e:" + e.Id + ":" + m.Pontos.ToString(CultureInfo.InvariantCulture), m.Premios, BlueprintSourceKind.Event);
                if (g >= 0) Novidades.Add(("event_complete", e.Id, m.Pontos.ToString(CultureInfo.InvariantCulture), g));
            }
        }

        public static string TxMissao(EventDef e, MissaoEvento x) => "em:" + e.Id + ":" + x.Id;

        /// <summary>Pontos do evento: as missoes pagas + as encomendas Event entregues nele (B-CORE-05: +15 cada, contadas enquanto o evento
        /// esta ativo; a trilha ja paga fica no ledger quando ele sai).</summary>
        public int PontosDe(EventDef e)
        {
            int p = e == Evento && _cont.TryGetValue(PontosEnc, out long enc) ? (int)Math.Min(int.MaxValue, enc) : 0;
            foreach (MissaoEvento x in e.Missoes) if (M.Ledger.Contains(TxMissao(e, x))) p += x.Pontos;
            return p;
        }

        const string PontosEnc = "encomenda_evento";

        /// <summary>B-CORE-05 (Encomendas.Entregar): encomenda Event do evento `evento` entregue. Os pontos entram na trilha no proximo Avanca.</summary>
        public void PontosEncomenda(string evento, int pontos)
        {
            if (Evento != null && Evento.Id == evento && pontos > 0) Add(PontosEnc, pontos);
        }

        /// <summary>B-CORE-05: encomenda entregue por tipo (o Ev.OrderDone nao leva o tipo): order_complete.{common|bulk|...} das missoes.</summary>
        public void OnEncomenda(OrderType t)
        {
            if (Evento != null) Conta("order_complete", t.ToString().ToLowerInvariant());
        }

        public int Pontos => Evento == null ? 0 : PontosDe(Evento);

        /// <summary>Proximo limiar da trilha do evento ativo (null = trilha completa ou nenhum evento).</summary>
        public Marco Proximo
        {
            get
            {
                if (Evento == null) return null;
                int p = Pontos;
                return Array.Find(Evento.Trilha, m => m.Pontos > p);
            }
        }

        /// <summary>Progresso da missao no evento ativo (desde a entrada; varios filtros somam), ate o alvo.</summary>
        public long Feito(MissaoEvento x)
        {
            if (Evento == null) return 0;
            long n = 0;
            if (x.Filtro.Length == 0) _cont.TryGetValue(x.Evento, out n);
            else foreach (string f in x.Filtro) n += _cont.TryGetValue(x.Evento + "." + f, out long v) ? v : 0;
            return Math.Min(n, x.Alvo);
        }

        public bool Paga(MissaoEvento x) => Evento != null && M.Ledger.Contains(TxMissao(Evento, x));

        /// <summary>exhaust = AutoComplete (LIVEOPS_V1 §5.2): compra/contratacao sem mais nada a comprar completa sozinha.</summary>
        bool Esgotada(MissaoEvento x)
        {
            if (x.Evento == "worker_any") return _sim.Workers.Count >= Metas.AjudantesOldForge && !_sim.Equipe.FaltaContratar && !_sim.Equipe.FaltaNivel;
            if (x.Evento != "upgrade_buy") return false;
            if (x.Filtro.Length == 0) return _sim.UpgradesBought >= Upgrades.Count;
            foreach (string f in x.Filtro) if (!_sim.Bought[Upgrades.IndexOf(f)]) return false;
            return true;
        }

        /// <summary>Fluxo de eventos do Sim (Sim.Emit). So conta com evento ativo (jogo online: o offline nao emite Ev de venda).</summary>
        public void On(Ev kind, int a, int b)
        {
            if (kind == Ev.OrderDone) XpEncomenda();
            if (Evento == null) return;
            switch (kind)
            {
                case Ev.Sold: Conta("product_sold", Item(a)); break;
                case Ev.Crafted: Conta("product_crafted", Item(a)); break;
                case Ev.OrderDone: Conta("order_done", Item(a)); break;
                case Ev.Bought: Conta("upgrade_buy", Upgrades.All[a].Id); break;
                case Ev.Offline: Conta("offline_claim", null); break;
                case Ev.VipServed: Conta("vip_served", Item(a)); break;
                case Ev.Hired: case Ev.WorkerUp: Conta("worker_any", null); break;   // B-CORE-04: contratou (upgrade ou Equipe) ou subiu nivel
                case Ev.ContractDone: Conta("contract_complete", _sim.Contratos.Defs[a].Id); break;   // D-CORE-04: contrato concluido (filtro = ID)
                case Ev.Graded:   // A = estacao, B = nivel (so acima de Normal)
                    string it = Item((int)_sim.Stations[a].OutItem);
                    Conta("quality_crafted", it);
                    if (b >= (int)Quality.Superior) Conta("quality_superior", it);
                    break;
            }
        }

        /// <summary>Golpe de mestre acertado (Sim.Strike) numa bancada que faz `item`.</summary>
        public void OnGolpe(int item) { if (Evento != null) Conta("master_strike", Item(item)); }

        string Item(int handle) => (uint)handle < (uint)_sim.ItemDefs.Length ? _sim.ItemDefs[handle].Id : null;

        void Conta(string ev, string filtro)
        {
            Add(ev, 1);
            if (filtro != null) Add(ev + "." + filtro, 1);
        }

        void Add(string k, long n) => _cont[k] = (_cont.TryGetValue(k, out long v) ? v : 0) + n;

        /// <summary>Paga as recompensas pelo Metas.Paga (1x por `tx`): ouro em s de renda, XP do Pass e Renome somados; cosmetico = a posse e' o
        /// proprio ID no ledger. B-CORE-08 (V20): Blueprint = posse nova pelo Projetos.Ganha (fonte `origem`, com o `tx`: fonte extra, a gratis
        /// permanente continua); quem ja tem (ou projeto que nao lista a fonte) recebe o fallback no mesmo ID. Devolve o ouro ou -1 (ja pago).</summary>
        long Credita(string tx, RewardDef[] rs, BlueprintSourceKind origem)
        {
            if (M.Ledger.Contains(tx)) return -1;   // antes da posse: o projeto tambem sai 1x pelo ID
            int ouro = 0, xp = 0, ren = 0;
            foreach (RewardDef r0 in rs)
            {
                if (r0.Tipo == TipoPremio.Blueprint && _sim.Projetos.Ganha(r0.Ref, origem, tx)) continue;
                RewardDef r = r0.Tipo == TipoPremio.Blueprint ? r0.Fallback : r0;
                if (r.Tipo == TipoPremio.Ouro) ouro += r.Valor;
                else if (r.Tipo == TipoPremio.PassXp) xp += r.Valor;
                else if (r.Tipo == TipoPremio.Renome) ren += r.Valor;
            }
            return M.Paga(tx, ouro, xp, ren);
        }

        // ------------------------------------------------------------------ Pass

        public PassDef PasseEm(long t) => Array.Find(Passes, p => p.Comeca <= t && t < p.Termina);

        /// <summary>Temporada nova (diferente da guardada): o que ficou alcancado e nao resgatado na anterior paga sozinho e o XP recomeca. Sem
        /// temporada (antes do T0, entre dados) o XP das metas espera no Metas.PassXp e entra na proxima (o retroativo).</summary>
        void PassoDoPasse()
        {
            PassDef p = PasseEm(Relogio);
            if (p != null && p != Passe)
            {
                if (Passe != null) for (int n = 1; n <= Nivel; n++) Resgatar(n);
                Passe = p; Xp = 0; XpEncHoje = 0;
            }
            _corre = p != null && p == Passe && _sim.T.Passe;
            if (!_corre) return;
            long d = M.PassXp - XpVisto;
            XpVisto = M.PassXp;
            if (d > 0) Ganha(d);
        }

        /// <summary>V20: encomenda entregue da XpEncomenda ate o teto do dia local (as metas abertas: depois das Ferramentas).</summary>
        void XpEncomenda()
        {
            if (!_corre || !M.Aberto) return;
            if (M.Dia != DiaEnc) { DiaEnc = M.Dia; XpEncHoje = 0; }
            long n = Math.Min(Passe.XpEncomenda, Passe.TetoEncomendaDia - XpEncHoje);
            if (n <= 0) return;
            XpEncHoje += n;
            Ganha(n);
        }

        /// <summary>Depois do ultimo nivel o XP nao acumula (sem nivel bonus na v1).</summary>
        public void Ganha(long n)
        {
            if (Passe == null || n <= 0) return;
            int antes = Nivel;
            Xp = Math.Min(Passe.XpTotal, Xp + n);
            for (int k = antes + 1; k <= Nivel; k++) Novidades.Add(("pass_level", Passe.Id, k.ToString(CultureInfo.InvariantCulture), 0));
        }

        public int Nivel => Passe == null ? 0 : (int)Math.Min(Passe.Niveis, Xp / Passe.XpNivel);

        public static string TxNivel(PassDef p, int n) => "p:" + p.Id + ":" + n.ToString(CultureInfo.InvariantCulture) + ":f";

        public bool Resgatado(int n) => Passe != null && M.Ledger.Contains(TxNivel(Passe, n));

        /// <summary>RESGATAR o nivel `n` (1..) da trilha free: alcancado, depois das Ferramentas, 1x pelo ID. Devolve o ouro ou -1.</summary>
        public long Resgatar(int n)
        {
            if (Passe == null || n < 1 || n > Nivel || !M.Aberto) return -1;
            return Credita(TxNivel(Passe, n), new[] { Passe.Free[n - 1] }, BlueprintSourceKind.PassFree);
        }

        /// <summary>Niveis alcancados e nao resgatados (badge).</summary>
        public int Prontos()
        {
            if (!M.Aberto) return 0;
            int k = 0;
            for (int n = 1; n <= Nivel; n++) if (!Resgatado(n)) k++;
            return k;
        }

        /// <summary>O Passe aparece: temporada no relogio (ou a ultima, ate a seguinte), flag ligada e depois das Ferramentas.</summary>
        public bool PasseVisivel => Passe != null && _sim.T.Passe && M.Aberto;

        // ------------------------------------------------------------------ save (linhas ev.* e ps.*; o Sim.Load de antes ignora)

        public void Save(StringBuilder sb)
        {
            if (Relogio == 0) return;
            sb.Append("ev.relogio=").Append(S(Relogio)).Append('\n');
            if (Evento != null)
            {
                sb.Append("ev.ativo=").Append(Evento.Id).Append(',').Append(S(_dia)).Append('\n');
                var keys = new List<string>(_cont.Keys);
                keys.Sort(StringComparer.Ordinal);
                sb.Append("ev.c=").Append(string.Join(",", keys.ConvertAll(k => k + ":" + S(_cont[k])))).Append('\n');
            }
            sb.Append("ps.passe=").Append(Passe != null ? Passe.Id : "-").Append(',').Append(S(Xp)).Append(',').Append(S(XpVisto)).Append(',')
              .Append(S(XpEncHoje)).Append(',').Append(S(DiaEnc)).Append('\n');
        }

        /// <summary>Le as linhas ev.*/ps.* (lixo = padrao; evento ou passe que sumiu dos dados = nenhum). Os modificadores voltam na 1a Agora.</summary>
        public void Load(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq < 0 || !(raw.StartsWith("ev.", StringComparison.Ordinal) || raw.StartsWith("ps.", StringComparison.Ordinal))) continue;
                string key = raw.Substring(0, eq).Trim(), val = raw.Substring(eq + 1).Trim();
                string[] p = val.Split(',');
                switch (key)
                {
                    case "ev.relogio": Relogio = Math.Max(0, L(val, 0)); break;
                    case "ev.ativo":
                        Evento = Array.Find(Eventos, e => e.Id == p[0]);
                        _dia = p.Length > 1 ? L(p[1], long.MinValue) : long.MinValue;
                        break;
                    case "ev.c":
                        foreach (string tok in p)
                        {
                            int c = tok.LastIndexOf(':');
                            if (c > 0) _cont[tok.Substring(0, c)] = Math.Max(0, L(tok.Substring(c + 1), 0));
                        }
                        break;
                    case "ps.passe":
                        if (p.Length < 5) break;
                        Passe = Array.Find(Passes, x => x.Id == p[0]);
                        Xp = Passe == null ? 0 : Math.Max(0, Math.Min(Passe.XpTotal, L(p[1], 0)));
                        XpVisto = Math.Max(0, L(p[2], 0)); XpEncHoje = Math.Max(0, L(p[3], 0)); DiaEnc = L(p[4], long.MinValue);
                        break;
                }
            }
            if (Evento == null) _cont.Clear();
        }

        static string S(long v) => v.ToString(CultureInfo.InvariantCulture);
        static long L(string s, long d) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : d;
    }
}
