using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// D-CORE-01: calendario das metas. O nucleo nunca le relogio: recebe `nowUnix` (e o fuso do aparelho so na 1a vez, Metas.Agora).
    /// Dia local = virada as 04:00 no fuso GRAVADO na 1a abertura com metas (LIVEOPS_V1 §4.1: quem joga perto da meia-noite nao perde a
    /// diaria no meio da sessao). Fuso fixo: viajar ou trocar o fuso do aparelho nao abre dia novo; horario de verao desloca a virada em 1 h,
    /// sem pular nem repetir dia. Semana = segunda-feira 04:00 local (o dia 0, 1/1/1970, foi quinta).
    /// </summary>
    public static class Calendario
    {
        public const long Dia = 86400, Virada = 4 * 3600;
        public static long DiaLocal(long nowUnix, long fuso) => Piso(nowUnix + fuso - Virada, Dia);
        public static long Semana(long dia) => Piso(dia + 3, 7);
        static long Piso(long a, long b) => a >= 0 ? a / b : -((-a + b - 1) / b);
    }

    /// <summary>
    /// D-CORE-01/02 (LIVEOPS_V1 §4-§5): diarias, semanais, conquistas e login de 7 dias sobre o fluxo de Ev do Sim (gancho unico: Sim.Emit
    /// chama On). Contadores vitalicios por evento (`Cont`, chave "evento" ou "evento.filtro"); missao = contador - base do momento em que
    /// apareceu. Ledger = IDs de transacao pagos (d:{dia}:{slot}, w:{semana}:{slot}, a:{id}, l:{ciclo}:{dia}): pagar o mesmo ID de novo e' no-op,
    /// e o ID vai no mesmo save que o ouro, entao save/load ou reabrir nao paga 2x. Relogio para tras: o dia gravado e' o MAXIMO ja visto,
    /// nada abre nem reabre. Relogio para frente: aceito ate haver hora de servidor (decisao do D-CORE-01).
    /// Ouro = s x RateEma (multiplo de 5, piso das encomendas) FORA do GoldEarned: nao infla a taxa online, o cofre nem o VIP; o bot nao resgata,
    /// entao os numeros dele nao mudam. XP do Pass fica guardado (PassXp) ate o Pass existir (V9/P1); o Renome vai para o Sim.Renome (B-CORE-06)
    /// e o Metas.Renome fica so como "quanto veio das metas" (mt.acc: o retroativo de um save sem rn= o soma 1x).
    /// Save: linhas mt.* (Save/Load); save sem elas = metas zeradas, e a 1a Agora semeia as conquistas pelos contadores do Sim (retroativo).
    /// </summary>
    public sealed class Metas
    {
        public const int NDiarias = 3, NSemanais = 4;
        /// <summary>= Sim.HireBy.Length (MetasTests confere): as contratacoes do Old Forge.</summary>
        public const int AjudantesOldForge = 6;
        /// <summary>GDD §3.3 (nenhum sistema novo nos primeiros 10 min) e LIVEOPS_V1 §0.7: metas e login abrem com as Ferramentas (~10:46 no F2P).</summary>
        public const int AbreCom = (int)Upgrade.Tools;

        /// <summary>Gatilhos que existem hoje: os do DM §86 que o Sim emite (product_*, order_done = order_complete sem tipo, upgrade_buy,
        /// worker_hired, offline_claim, currency_*), os do diario (vip_served, chest_opened) e os 2 do D-CORE-02 que o proprio Metas emite.</summary>
        public static readonly string[] Eventos =
        {
            "product_sold", "product_crafted", "order_done", "upgrade_buy", "worker_hired", "offline_claim", "currency_source", "currency_sink",
            "vip_served", "chest_opened", "mission_complete", "login_claim",
            // B-CORE-05 (LIVEOPS_V1 §5.8): entrega por tipo (filtro = OrderType), Quality/Royal/Mixed e Rush no prazo (Encomendas.Entregar)
            "order_complete", "order_premium", "order_rush_prazo",
            "worker_upgrade",   // B-CORE-04 (LIVEOPS_V1 §5.6): filtro = nivel alcancado
        };

        public sealed class Slot { public MetaDef Def; public long Base, Alvo; }

        /// <summary>Uma linha do painel: o que falta, o ID de transacao e se ja foi paga.</summary>
        public struct Meta
        {
            public MetaDef Def; public long Feito, Alvo; public string Tx; public bool Paga;
            public bool Pronta => Def != null && !Paga && Feito >= Alvo;
        }

        readonly Sim _sim;
        public bool Iniciado;
        /// <summary>Fuso gravado (s a somar no UTC), maior dia local ja visto e o dia da 1a abertura (o login abre no dia seguinte, D1).</summary>
        public long Fuso, Dia, Inicio;
        public readonly Slot[] Diarias = new Slot[NDiarias], Semanais = new Slot[NSemanais];
        public readonly Dictionary<string, long> Cont = new Dictionary<string, long>();
        public readonly HashSet<string> Ledger = new HashSet<string>();
        /// <summary>Login: ciclo atual (1..), dias ja pagos nele (0..6) e o dia local do ultimo resgate.</summary>
        public int LoginCiclo = 1, LoginDias;
        public long LoginUltimo = long.MinValue;
        public long PassXp, Renome;
        readonly string[] _txD = new string[NDiarias], _txW = new string[NSemanais];

        public Metas(Sim sim) { _sim = sim; Rotulos(); }

        public long Semana => Calendario.Semana(Dia);
        public bool Aberto => _sim.Bought[AbreCom];

        public static string Chave(string evento, int filtro) => filtro < 0 ? evento : evento + "." + filtro.ToString(CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------ fluxo de eventos (gancho do Sim.Emit)

        /// <summary>Conta o evento. So mexe nos contadores das metas: o Sim segue identico com ou sem quem leia.</summary>
        public void On(Ev kind, int a, int b)
        {
            switch (kind)
            {
                case Ev.Sold: Soma("product_sold", a, 1); Soma("currency_source", -1, b); break;   // venda e unidade do VIP (b = preco x3)
                case Ev.Crafted: Soma("product_crafted", a, 1); break;
                case Ev.OrderDone: Soma("order_done", a, 1); break;
                case Ev.Bought: Soma("upgrade_buy", a, 1); Soma("currency_sink", -1, b); break;
                case Ev.Offline: Soma("offline_claim", -1, 1); break;
                case Ev.VipServed: Soma("vip_served", a, 1); break;
                // B-CORE-04: filtro = papel (WorkerRole; 0 = o ajudante do upgrade, cujo ouro ja contou no Ev.Bought); worker_upgrade: filtro = nivel novo
                case Ev.Hired: Soma("worker_hired", b, 1); if (b > 0) Soma("currency_sink", -1, _sim.Equipe.Preco(a, 1)); break;
                case Ev.WorkerUp: Soma("worker_upgrade", b, 1); Soma("currency_sink", -1, _sim.Equipe.Preco(a, b)); break;
                case Ev.ChestOpened: Soma("chest_opened", -1, 1); break;
            }
        }

        /// <summary>B-CORE-05: encomenda entregue (o Ev.OrderDone nao leva o tipo). So conta: o Sim segue identico.</summary>
        public void OnEncomenda(OrderType t, bool noPrazo)
        {
            Soma("order_complete", (int)t, 1);
            if (t == OrderType.Quality || t == OrderType.Royal || t == OrderType.Mixed) Soma("order_premium", -1, 1);
            if (t == OrderType.Rush && noPrazo) Soma("order_rush_prazo", -1, 1);
        }

        void Soma(string ev, int filtro, long n)
        {
            Add(ev, n);
            if (filtro >= 0) Add(Chave(ev, filtro), n);
        }

        void Add(string k, long n)
        {
            Cont.TryGetValue(k, out long v);
            Cont[k] = v + n;
        }

        /// <summary>Valor do contador da meta (Distintos: quantos produtos com pelo menos 1; em order_complete, quantos tipos fora o Event).</summary>
        public long Valor(MetaDef d)
        {
            if (d.Conta != Conta.Distintos) { Cont.TryGetValue(d.Chave, out long v); return v; }
            bool tipos = d.Evento == "order_complete";
            long n = 0;
            for (int i = tipos ? 0 : (int)Item.Sword; i <= (tipos ? (int)OrderType.Blueprint : (int)Item.Jewel); i++)
                if (Cont.TryGetValue(Chave(d.Evento, i), out long v) && v > 0) n++;
            return n;
        }

        // ------------------------------------------------------------------ calendario e rodizio

        /// <summary>
        /// A View chama ao abrir e a cada segundo. 1a vez: grava o fuso, o dia e semeia os contadores pelo Sim (save antigo ganha o retroativo
        /// das conquistas). Dia maior que o gravado: vira o dia (e a semana), limpa as vagas e tira do ledger os IDs dos periodos fechados (o
        /// dia so cresce, entao eles nunca voltam). Dia menor (relogio para tras): nada muda. Depois preenche as vagas vazias pelo rodizio.
        /// </summary>
        public void Agora(long nowUnix, long fuso)
        {
            if (!Iniciado)
            {
                Iniciado = true; Fuso = fuso;
                Dia = Inicio = Calendario.DiaLocal(nowUnix, fuso);
                Semear();
                Rotulos();
            }
            else
            {
                long hoje = Calendario.DiaLocal(nowUnix, Fuso), semana = Semana;
                if (hoje > Dia)
                {
                    Dia = hoje;
                    Limpa(Diarias, "d:");
                    if (Semana != semana) Limpa(Semanais, "w:");
                    Rotulos();
                }
            }
            Preenche();
        }

        void Rotulos()
        {
            for (int i = 0; i < NDiarias; i++) _txD[i] = "d:" + S(Dia) + ":" + i;
            for (int k = 0; k < NSemanais; k++) _txW[k] = "w:" + S(Semana) + ":" + k;
        }

        void Limpa(Slot[] slots, string tipo)
        {
            Array.Clear(slots, 0, slots.Length);
            string vale = tipo + S(tipo == "d:" ? Dia : Semana) + ":";
            Ledger.RemoveWhere(t => t.StartsWith(tipo, StringComparison.Ordinal) && !t.StartsWith(vale, StringComparison.Ordinal));
        }

        /// <summary>
        /// Rodizio sem sorteio (LIVEOPS_V1 §4.1): vaga diaria p = pool p a partir de (dia x 5 + p) mod tamanho; pool sem nenhuma aberta (a C, hoje)
        /// cai na A. Semanal k = (semana x 3 + k) mod tamanho. Pula a travada (Abre), a esgotada (nada mais a comprar/contratar) e a que repete
        /// contador com outra vaga do periodo. Vaga preenchida fica ate a virada: comprar algo no meio do dia nao troca a missao.
        /// </summary>
        void Preenche()
        {
            if (!Aberto) return;
            for (int p = 0; p < NDiarias; p++)
                if (Diarias[p] == null) Diarias[p] = Novo(Escolhe(MetasDefs.Diarias, p, Dia * 5 + p, Diarias) ?? Escolhe(MetasDefs.Diarias, 0, Dia * 5 + p, Diarias));
            for (int k = 0; k < NSemanais; k++)
                if (Semanais[k] == null) Semanais[k] = Novo(Escolhe(MetasDefs.Semanais, -1, Semana * 3 + k, Semanais));
        }

        MetaDef Escolhe(MetaDef[] defs, int pool, long inicio, Slot[] ocupadas)
        {
            MetaDef[] c = Array.FindAll(defs, d => pool < 0 || d.Pool == pool);
            for (int i = 0; i < c.Length; i++)
            {
                MetaDef d = c[(int)(((inicio + i) % c.Length + c.Length) % c.Length)];
                if (Livre(d) && !Array.Exists(ocupadas, s => s != null && s.Def.Chave == d.Chave)) return d;
            }
            return null;
        }

        bool Livre(MetaDef d)
        {
            if (d.Abre != null && !_sim.Bought[Upgrades.IndexOf(d.Abre)]) return false;
            switch (d.Evento)   // ponytail: "compre 3" com 2 a venda ainda aparece; o rodizio so pula quando nao ha mais nada
            {
                case "upgrade_buy": case "currency_sink": return _sim.UpgradesBought < Upgrades.Count;
                case "worker_hired": return _sim.Workers.Count < AjudantesOldForge || _sim.Equipe.FaltaContratar;   // B-CORE-04: ou um da Equipe
                case "worker_upgrade": return _sim.Equipe.FaltaNivel;
                case "chest_opened": return _sim.Chests.Exists(c => c.State < 2);
                // B-CORE-05: missao de tipo de encomenda so quando o tipo ja abre no rodizio (antes, nao fecharia na semana)
                case "order_complete": return _sim.Encomendas.TipoAberto((OrderType)d.Filtro);
                case "order_premium": return _sim.Encomendas.TipoAberto(OrderType.Quality) || _sim.Encomendas.TipoAberto(OrderType.Mixed);
                case "order_rush_prazo": return _sim.Encomendas.TipoAberto(OrderType.Rush);
                default: return true;
            }
        }

        /// <summary>Vaga nova: base = contador agora; alvo em s de renda congela a taxa de agora (min. 100 de ouro, com a taxa ainda zerada).</summary>
        Slot Novo(MetaDef d) => d == null ? null : new Slot
        {
            Def = d, Base = Valor(d),
            Alvo = d.Conta == Conta.Segundos ? Math.Max(100, 5 * (long)Math.Round(_sim.RateEma * d.Alvo / 5.0)) : d.Alvo,
        };

        // ------------------------------------------------------------------ consultas e resgate

        public Meta Diaria(int i) => Missao(Diarias[i], _txD[i]);
        public Meta Semanal(int k) => Missao(Semanais[k], _txW[k]);

        Meta Missao(Slot s, string tx) => s == null ? new Meta { Tx = tx } : new Meta
        {
            Def = s.Def, Alvo = s.Alvo, Feito = Math.Max(0, Math.Min(s.Alvo, Valor(s.Def) - s.Base)), Tx = tx, Paga = Ledger.Contains(tx),
        };

        public Meta Conquista(MetaDef d) => new Meta { Def = d, Alvo = d.Alvo, Feito = Math.Min(d.Alvo, Valor(d)), Tx = d.Tx, Paga = Ledger.Contains(d.Tx) };

        public long ResgatarDiaria(int i) => Iniciado ? Resgata(Diaria(i)) : -1;
        public long ResgatarSemanal(int k) => Iniciado ? Resgata(Semanal(k)) : -1;
        public long ResgatarConquista(string id)
        {
            MetaDef d = Array.Find(MetasDefs.Conquistas, x => x.Id == id);
            return d == null ? -1 : Resgata(Conquista(d));
        }

        long Resgata(Meta m)
        {
            if (!Aberto || !m.Pronta) return -1;   // antes das Ferramentas nada paga: a conquista fica para quando a tela abre
            long g = Paga(m.Tx, m.Def.OuroS, m.Def.Xp, m.Def.Renome);
            if (g >= 0 && m.Def.Tipo != TipoMeta.Conquista) Soma("mission_complete", (int)m.Def.Tipo, 1);
            return g;
        }

        /// <summary>Login nao punitivo (DM §135): 1 resgate por dia local, a partir do dia seguinte ao da 1a abertura; dia perdido so pausa o
        /// ciclo (nunca volta ao dia 1); depois do 7 comeca o ciclo seguinte.</summary>
        public bool LoginPronto => Iniciado && Aberto && Dia > Inicio && Dia > LoginUltimo;

        public long ResgatarLogin()
        {
            if (!LoginPronto) return -1;
            int d = LoginDias + 1;
            LoginDia r = MetasDefs.LoginDias[d - 1];
            long g = Paga("l:" + LoginCiclo + ":" + d, r.OuroS, 0, r.Renome);
            if (g < 0) return -1;
            LoginUltimo = Dia;
            Soma("login_claim", -1, 1);
            if (d == MetasDefs.LoginDias.Length) { LoginCiclo++; LoginDias = 0; }
            else LoginDias = d;
            return g;
        }

        /// <summary>Ouro de `s` segundos de renda agora: s x RateEma, multiplo de 5, piso das encomendas (LIVEOPS_V1 §0.8).</summary>
        public long Ouro(int s) => Math.Max(_sim.T.OrderRewardMin, 5 * (long)Math.Round(_sim.RateEma * s / 5.0));

        /// <summary>D-CORE-01: credito unico por ID. ID ja no ledger = nada muda e devolve -1; senao grava o ID e paga, no mesmo passo.</summary>
        public long Paga(string tx, int ouroS, int xp, int renome)
        {
            if (!Ledger.Add(tx)) return -1;
            long g = ouroS > 0 ? Ouro(ouroS) : 0;
            _sim.Gold += g;
            PassXp += xp; Renome += renome;
            _sim.Renome.Ganha(renome);   // B-CORE-06: o ledger acima ja garante 1x por ID
            return g;
        }

        /// <summary>Quantas dao para resgatar agora (badge do botao e aviso de meta concluida).</summary>
        public int Prontas()
        {
            if (!Iniciado || !Aberto) return 0;
            int n = LoginPronto ? 1 : 0;
            for (int i = 0; i < NDiarias; i++) if (Diaria(i).Pronta) n++;
            for (int k = 0; k < NSemanais; k++) if (Semanal(k).Pronta) n++;
            foreach (MetaDef d in MetasDefs.Conquistas) if (Conquista(d).Pronta) n++;
            return n;
        }

        // ------------------------------------------------------------------ retroativo

        /// <summary>Save antigo (0.1-0.7) chega sem contadores: o que o Sim ja conta vira o ponto de partida das conquistas. Max: o que o
        /// fluxo de Ev ja contou nesta sessao nunca diminui.</summary>
        void Semear()
        {
            Sim s = _sim;
            Max("product_sold", -1, s.Sales);
            for (int i = (int)Item.Sword; i <= (int)Item.Jewel; i++) Max("product_sold", i, s.SoldItems[i]);
            var feitos = new long[s.Stock.Length];
            foreach (Station st in s.Stations) if (st.Produces) feitos[(int)st.OutItem] += st.Crafted;
            for (int i = (int)Item.Ingot; i <= (int)Item.Jewel; i++) Max("product_crafted", i, feitos[i]);
            Max("order_done", -1, s.OrderCount);
            long gasto = 0;
            for (int u = 0; u < Upgrades.Count; u++) if (s.Bought[u]) { Max("upgrade_buy", u, 1); gasto += s.T.Cost(u); }
            Max("upgrade_buy", -1, s.UpgradesBought);
            Max("currency_sink", -1, gasto);
            Max("currency_source", -1, s.GoldEarned);
            // B-CORE-04: contratacoes por papel (os ajudantes do upgrade = papel 0) e niveis alcancados (filtro = nivel)
            Max("worker_hired", -1, s.Workers.Count + s.Equipe.Contratados());
            Max("worker_hired", (int)WorkerRole.Porter, s.Workers.Count);
            for (WorkerRole p = WorkerRole.Smith; p <= WorkerRole.Manager; p++) if (s.Equipe.Contratados(p) > 0) Max("worker_hired", (int)p, s.Equipe.Contratados(p));
            long subidos = 0;
            for (int n = 2; n <= 10; n++)
            {
                int k = 0;
                for (int w = 0; w < s.Equipe.Defs.Length; w++) if (s.Equipe.Nivel(w) >= n) k++;
                subidos += k;
                if (k > 0) Max("worker_upgrade", n, k);
            }
            Max("worker_upgrade", -1, subidos);
            Max("chest_opened", -1, s.Chests.FindAll(c => c.State == 2).Count);
        }

        void Max(string ev, int filtro, long v)
        {
            string k = Chave(ev, filtro);
            Cont.TryGetValue(k, out long a);
            Cont[k] = Math.Max(a, v);
        }

        // ------------------------------------------------------------------ save (linhas mt.*; o Sim.Load de antes ignora chave desconhecida)

        public void Save(StringBuilder sb)
        {
            if (!Iniciado) return;   // nunca aberto (bot, testes, save antigo nao reaberto): o texto do save fica igual ao de antes
            sb.Append("mt.cal=").Append(S(Fuso)).Append(',').Append(S(Dia)).Append(',').Append(S(Inicio)).Append('\n');
            sb.Append("mt.d=").Append(Slots(Diarias)).Append('\n');
            sb.Append("mt.w=").Append(Slots(Semanais)).Append('\n');
            sb.Append("mt.login=").Append(LoginCiclo).Append(',').Append(LoginDias).Append(',').Append(S(LoginUltimo)).Append('\n');
            sb.Append("mt.acc=").Append(S(PassXp)).Append(',').Append(S(Renome)).Append('\n');
            var keys = new List<string>(Cont.Keys);
            keys.Sort(StringComparer.Ordinal);
            sb.Append("mt.c=").Append(string.Join(",", keys.ConvertAll(k => k + ":" + S(Cont[k])))).Append('\n');
            var led = new List<string>(Ledger);
            led.Sort(StringComparer.Ordinal);
            sb.Append("mt.led=").Append(string.Join(",", led)).Append('\n');
        }

        static string Slots(Slot[] ss) => string.Join(",", Array.ConvertAll(ss, s => s == null ? "-" : s.Def.Id + ":" + S(s.Base) + ":" + S(s.Alvo)));

        /// <summary>Le as linhas mt.* do payload (lixo = valor padrao; ID de meta que sumiu dos dados = vaga vazia, o rodizio repoe).</summary>
        public void Load(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                if (!raw.StartsWith("mt.", StringComparison.Ordinal)) continue;
                int eq = raw.IndexOf('=');
                if (eq < 0) continue;
                string key = raw.Substring(0, eq).Trim(), val = raw.Substring(eq + 1).Trim();
                string[] p = val.Split(',');
                switch (key)
                {
                    case "mt.cal":
                        if (p.Length < 3) break;
                        Iniciado = true; Fuso = L(p[0], 0); Dia = L(p[1], 0); Inicio = Math.Min(Dia, L(p[2], Dia));
                        break;
                    case "mt.d": LeSlots(p, Diarias, TipoMeta.Diaria); break;
                    case "mt.w": LeSlots(p, Semanais, TipoMeta.Semanal); break;
                    case "mt.login":
                        if (p.Length < 3) break;
                        LoginCiclo = (int)Math.Max(1, Math.Min(int.MaxValue, L(p[0], 1)));
                        LoginDias = (int)Math.Max(0, Math.Min(MetasDefs.LoginDias.Length - 1, L(p[1], 0)));
                        LoginUltimo = L(p[2], long.MinValue);
                        break;
                    case "mt.acc":
                        if (p.Length >= 2) { PassXp = Math.Max(0, L(p[0], 0)); Renome = Math.Max(0, L(p[1], 0)); }
                        break;
                    case "mt.c":
                        foreach (string tok in p)
                        {
                            int c = tok.LastIndexOf(':');
                            if (c > 0) Cont[tok.Substring(0, c)] = Math.Max(0, L(tok.Substring(c + 1), 0));
                        }
                        break;
                    case "mt.led": foreach (string tok in p) if (tok.Length > 0) Ledger.Add(tok); break;
                }
            }
            LoginUltimo = Math.Min(LoginUltimo, Dia);   // lixo no futuro nao trava o login
            Rotulos();
        }

        static void LeSlots(string[] p, Slot[] slots, TipoMeta tipo)
        {
            for (int i = 0; i < Math.Min(p.Length, slots.Length); i++)
            {
                string[] f = p[i].Split(':');
                MetaDef d = f.Length == 3 ? MetasDefs.Achar(f[0]) : null;
                slots[i] = d == null || d.Tipo != tipo ? null : new Slot { Def = d, Base = Math.Max(0, L(f[1], 0)), Alvo = Math.Max(1, L(f[2], d.Alvo)) };
            }
        }

        static string S(long v) => v.ToString(CultureInfo.InvariantCulture);
        static long L(string s, long d) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : d;
    }
}
