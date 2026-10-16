using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>B-CORE-05 (GDD §10, DM §18): os 8 tipos de encomenda. O save grava o ID do OrderDef, nunca este numero; as metas filtram por
    /// ele (order_complete.N): tipo novo entra no FIM.</summary>
    public enum OrderType { Common, Bulk, Quality, Rush, Mixed, Royal, Blueprint, Event }

    /// <summary>
    /// B-CORE-05: encomenda em dados (ECONOMIA_WAVE_B §8.4, CONTEUDO_V1 §7). Alvo em s de renda da linha (`TargetSeconds`, preso em
    /// TargetMin..TargetMax; o freguês tem `FixedN` e `Item` fixos), premio em s da taxa online (`RewardSeconds`, minimo `RewardMin`) e Renome.
    /// Prazo so de bonus (`BonusDeadline` s de jogo: no prazo soma BonusRewardSeconds e BonusReputation); nao existe campo de penalidade.
    /// Abre (OrderDefs.Aberto): `MinLines` linhas abertas, `MinDelivered` entregues, o nivel de Renome que libera o ID (RenomeDefs), linha
    /// que ja faz a `MinQuality` sem o golpe e, no Blueprint, projeto pendente na `BlueprintQueue`. `Products` = teto do Mixed (pede
    /// min(Products, linhas - 1)). `District` null = distrito ativo (Event, vaga propria enquanto `EventId` esta ativo).
    /// </summary>
    public sealed class OrderDef
    {
        public readonly string Id, District, Item, ClientId, EventId, BlueprintQueue;
        public readonly OrderType Type;
        public readonly float TargetSeconds, RewardSeconds, BonusDeadline, BonusRewardSeconds;
        public readonly int TargetMin, TargetMax, RewardMin, Reputation, BonusReputation, Products, MinLines, MinDelivered, FixedN, EventPoints;
        public readonly Quality MinQuality;
        /// <summary>C-CORE-07: ID do upgrade do distrito que abre a encomenda (null = nenhum): a Royal do Arsenal que pede Normal com a mesa_real
        /// (nunca vira Common, CONTEUDO_V1 §2.4 regra 6).</summary>
        public readonly string Requer;

        public OrderDef(string id, OrderType type, string district, float targetSeconds, int targetMin, int targetMax, float rewardSeconds, int rewardMin,
            int reputation, Quality minQuality = Quality.Normal, int products = 1, float bonusDeadline = 0f, float bonusRewardSeconds = 0f,
            int bonusReputation = 0, string blueprintQueue = null, int minLines = 1, int minDelivered = 0, string item = null, int fixedN = 0,
            string clientId = null, string eventId = null, int eventPoints = 0, string requer = null)
        {
            Id = id; Type = type; District = district; TargetSeconds = targetSeconds; TargetMin = targetMin; TargetMax = targetMax;
            RewardSeconds = rewardSeconds; RewardMin = rewardMin; Reputation = reputation; MinQuality = minQuality; Products = products;
            BonusDeadline = bonusDeadline; BonusRewardSeconds = bonusRewardSeconds; BonusReputation = bonusReputation; BlueprintQueue = blueprintQueue;
            MinLines = minLines; MinDelivered = minDelivered; Item = item; FixedN = fixedN; ClientId = clientId; EventId = eventId; EventPoints = eventPoints;
            Requer = requer;
        }
    }

    /// <summary>Uma vaga aberta: o que pede (1 item; Mixed 2-3), quanto ja foi, o premio (o "bonus da encomenda" por anuncio soma no Premio),
    /// o prazo de bonus que resta e o Blueprint mostrado no cartao. Idx = posicao no rodizio (Event: na conta propria dos eventos).</summary>
    public sealed class Encomenda
    {
        public readonly OrderDef Def; public readonly int Idx;
        public readonly Item[] Itens; public readonly int[] Alvo, Feito;
        public long Premio; public readonly long PremioBonus;
        public float Prazo;
        public readonly string Blueprint;

        public Encomenda(OrderDef def, int idx, Item[] itens, int[] alvo, long premio, long premioBonus, float prazo, string blueprint)
        {
            Def = def; Idx = idx; Itens = itens; Alvo = alvo; Feito = new int[itens.Length]; Premio = premio; PremioBonus = premioBonus;
            Prazo = prazo; Blueprint = blueprint;
        }

        public OrderType Tipo => Def.Type;
        public int Renome => Def.Reputation;
        /// <summary>ID de transacao do premio: a vaga sai junto com o pagamento (Encomendas.Entregar), entao o mesmo ID nunca paga 2x.</summary>
        public string Tx => "enc:" + (Def.EventId != null ? Def.EventId + ":" : "") + Idx.ToString(CultureInfo.InvariantCulture);
        public bool Pronta { get { for (int k = 0; k < Alvo.Length; k++) if (Feito[k] < Alvo[k]) return false; return true; } }
        public int Total { get { int n = 0; foreach (int a in Alvo) n += a; return n; } }
        public int Progresso { get { int n = 0; foreach (int f in Feito) n += f; return n; } }

        /// <summary>Posicao do `item` que ainda falta e aceita a peca de nivel `q`, ou -1.</summary>
        public int Aceita(Item item, Quality q)
        {
            if (q < Def.MinQuality) return -1;
            for (int k = 0; k < Itens.Length; k++) if (Itens[k] == item && Feito[k] < Alvo[k]) return k;
            return -1;
        }
    }

    /// <summary>
    /// B-CORE-05 Orders 2.0 (GDD §10, ECONOMIA_WAVE_B §3): vagas de encomenda em rodizio deterministico, sem sorteio. Tipo = ciclo do
    /// distrito na posicao Abertas % 10 (tipo travado vira Common); produto = linhas[idx % linhas] como a de antes, pulando a que ja esta numa
    /// vaga; a cada 7a Common, o proximo freguês (FS-3) que der. Vagas: 1, 2 no Renome 1, 3 no Renome 3. Tudo que e' novo (tipos, vagas,
    /// fregueses) so depois das Ferramentas: os 10 primeiros minutos ficam os de antes, tick a tick (EncomendasTests). Cada peca vendida conta
    /// para no maximo uma vaga, a mais antiga que a aceita. Premio fora do GoldEarned (como o bau), Renome pelo tipo, Blueprint pela fila do
    /// Sim.Projetos (B-CORE-07) e pontos de evento pelo gancho do LiveOps; a vaga sai junto com o pagamento (ID de transacao `Encomenda.Tx`: entregar 2x nao paga 2x). Nunca falha nem
    /// pune: Rush fora do prazo paga o normal. Save: ord= (espelho da principal para o downgrade) e linhas enc*; save sem enc= migra a
    /// encomenda de antes para uma Common igual.
    /// </summary>
    public sealed class Encomendas
    {
        readonly Sim _sim;
        /// <summary>Vagas abertas, na ordem em que abriram (a 1a e' a mais antiga e a "principal" dos anuncios).</summary>
        public readonly List<Encomenda> Vagas = new List<Encomenda>();
        /// <summary>Posicao da proxima no rodizio (abertas + entregues + trocadas), Commons abertas (fregueses a cada 7a), proximo freguês,
        /// encomendas de evento abertas e trocadas por anuncio. Vao no save.</summary>
        public int Abertas, Comuns, Fregueses, Eventos, Pulos;
        /// <summary>s ate a proxima abrir: so anda depois da 1a venda e com vaga livre. Vai no save (ord=).</summary>
        public float OrderIn;
        /// <summary>Coracoes por freguês (FS-3, V17: trilha, nao moeda), +1 por encomenda pessoal entregue. Vai no save (enc.cor=).</summary>
        public readonly Dictionary<string, int> Coracoes = new Dictionary<string, int>();
        /// <summary>Metricas do bot (fora do save, como o OrderGold): Renome que as encomendas deram e entregues por tipo nesta sessao.</summary>
        public long RenomeGanho; public readonly int[] PorTipo = new int[Enum.GetValues(typeof(OrderType)).Length];
        /// <summary>A ultima paga (o Ev.OrderDone nao leva o tipo): o diario loga order_complete com ela. Fora do save.</summary>
        public Encomenda UltimaEntregue;
        // Ligacoes: a encomenda Event e' a do Sim.LiveOps.Evento (D-CORE-03: pontos pelo LiveOps.PontosEncomenda, tipos pelo OnEncomenda, XP do
        // Passe pelo Ev.OrderDone com o teto do dia); as filas de Blueprint sao as do Sim.Projetos (B-CORE-07: ProximoDaFila / GanhaDaFila)

        public Encomendas(Sim sim) { _sim = sim; OrderIn = sim.T.OrderFirstDelay; }

        /// <summary>A mais antiga fora de evento: a do cartao com os anuncios (bonus, trocar).</summary>
        public Encomenda Principal => Vagas.Find(e => e.Tipo != OrderType.Event);
        int Normais { get { int n = 0; foreach (Encomenda e in Vagas) if (e.Tipo != OrderType.Event) n++; return n; } }

        static readonly int Vaga2 = RenomeDefs.NivelQueLibera("vaga_encomenda_2"), Vaga3 = RenomeDefs.NivelQueLibera("vaga_encomenda_3");
        /// <summary>Quantas vagas (sem a de evento): 1; 2 no Renome 1; 3 no Renome 3 (RenomeDefs vaga_encomenda_*). So depois das Ferramentas.</summary>
        public int VagasMax => !_sim.QualityRevealed ? 1 : _sim.Renome.Nivel >= Vaga3 ? 3 : _sim.Renome.Nivel >= Vaga2 ? 2 : 1;

        // ------------------------------------------------------------------ tick e entrega

        /// <summary>Do Sim.Tick, depois dos clientes. Flag Encomendas desligada: as abertas ainda entregam e pagam; nenhuma nova abre.</summary>
        internal void Tick(float dt)
        {
            if (_sim.FirstSaleTime < 0f) return;
            foreach (Encomenda e in Vagas) if (e.Prazo > 0f) e.Prazo = Math.Max(0f, e.Prazo - dt);   // tempo de jogo: o offline nao conta
            string ev = _sim.LiveOps.Evento?.Id;   // D-CORE-03: o evento em que o jogador entrou (null = nenhum)
            for (int i = Vagas.Count - 1; i >= 0; i--) if (Vagas[i].Tipo == OrderType.Event && Vagas[i].Def.EventId != ev) Vagas.RemoveAt(i);   // o evento acabou: sai sem pagar
            bool entregou = false;
            for (int i = 0; i < Vagas.Count;) if (Vagas[i].Pronta) { Entregar(Vagas[i]); entregou = true; } else i++;
            if (entregou || !_sim.T.Encomendas) return;   // como antes: a proxima conta do tick seguinte
            if (ev != null && _sim.QualityRevealed && !Vagas.Exists(e => e.Tipo == OrderType.Event)) AbreEvento(ev);
            if (Normais >= VagasMax || (OrderIn -= dt) > 0f) return;
            OrderIn = 0f;
            Abre(Gera(Abertas, null), null);
            if (Normais < VagasMax) OrderIn = _sim.T.OrderGap;
        }

        /// <summary>Paga a vaga `e` se ainda esta aberta e completa: ouro (no prazo, com o bonus) fora do GoldEarned, Renome do tipo, Blueprint,
        /// pontos de evento e coracao do freguês. A vaga sai no mesmo passo: a 2a chamada (ou o mesmo ID depois de save/load) devolve -1.</summary>
        public long Entregar(Encomenda e)
        {
            if (!Vagas.Contains(e) || !e.Pronta) return -1;
            Vagas.Remove(e);
            bool prazo = e.Prazo > 0f;
            long ouro = e.Premio + (prazo ? e.PremioBonus : 0);
            int rn = e.Renome + (prazo ? e.Def.BonusReputation : 0);
            _sim.Gold += ouro; _sim.OrderGold += ouro; _sim.OrderCount++;
            _sim.Renome.Ganha(rn); RenomeGanho += rn; PorTipo[(int)e.Tipo]++; UltimaEntregue = e;
            _sim.OnEncomendaEntregue(e);   // C-CORE-07: a Royal do distrito conta para o Charter (Sim.RoyalEntregues)
            if (e.Blueprint != null) _sim.Projetos.GanhaDaFila(Fila(e.Def), e.Def.District, e.Tx);   // B-CORE-07: o pendente da fila (o mostrado, se ninguem o deu antes)
            if (e.Def.EventId != null) _sim.LiveOps.PontosEncomenda(e.Def.EventId, e.Def.EventPoints);
            if (e.Def.ClientId != null) { Coracoes.TryGetValue(e.Def.ClientId, out int c); Coracoes[e.Def.ClientId] = c + 1; }
            _sim.Metas.OnEncomenda(e.Tipo, prazo);
            _sim.LiveOps.OnEncomenda(e.Tipo);   // missoes order_complete do evento ativo (o XP do Passe vem do Ev.OrderDone, abaixo)
            if (_sim.Heroi != null) { _sim.Heroi.Metas.OnEncomenda(e.Tipo, prazo); _sim.Heroi.LiveOps.OnEncomenda(e.Tipo); }   // C-CORE-03: e nas do Imperio (a raiz)
            _sim.Emit(Ev.OrderDone, (int)e.Itens[0], (int)Math.Min(int.MaxValue, ouro), _sim.CounterFor(e.Itens[0]).Pos);   // joia: na loja de joias
            OrderIn = Math.Max(OrderIn, _sim.T.OrderGap);
            return ouro;
        }

        /// <summary>Do Sim.Sell: a peca vendida conta para a vaga mais antiga que a aceita (item que falta e nivel minimo), so uma.</summary>
        internal void OnVenda(Item item, Quality q)
        {
            foreach (Encomenda e in Vagas)
            {
                int k = e.Aceita(item, q);
                if (k >= 0) { e.Feito[k]++; return; }
            }
        }

        // ------------------------------------------------------------------ rodizio (sem sorteio)

        /// <summary>"Trocar encomenda" (so o Anuncios.Pagar chama): a principal sai sem pagar e entra, no mesmo lugar, a proxima do rodizio
        /// do zero, a que o botao mostrou (Proxima). Sem sorteio (P5).</summary>
        internal void Trocar()
        {
            Encomenda p = Principal;
            if (p == null) return;
            Pulos++;
            Abre(Gera(Abertas, p), p);
        }

        /// <summary>A que entra se o jogador trocar a principal agora (mostrada ANTES do anuncio); null sem principal.</summary>
        public Encomenda Proxima() => Principal == null ? null : Gera(Abertas, Principal);

        void Abre(Encomenda e, Encomenda no)
        {
            if (no != null) Vagas[Vagas.IndexOf(no)] = e; else Vagas.Add(e);
            Abertas++;
            if (e.Tipo == OrderType.Common || e.Def.ClientId != null) Comuns++;
            if (e.Def.ClientId != null) Fregueses = Array.IndexOf(Regulares(e.Def.District), e.Def) + 1;
            _sim.Emit(Ev.OrderNew, (int)e.Itens[0], e.Total, _sim.CounterFor(e.Itens[0]).Pos);
        }

        void AbreEvento(string ev)
        {
            OrderDef d = Array.Find(OrderDefs.All, x => x.EventId == ev);
            if (d == null) return;   // evento sem encomenda propria
            Encomenda e = Monta(d, Eventos, false, Linhas(), null);
            Vagas.Add(e);
            Eventos++;
            _sim.Emit(Ev.OrderNew, (int)e.Itens[0], e.Total, _sim.CounterFor(e.Itens[0]).Pos);
        }

        /// <summary>A encomenda da posicao `idx` do rodizio com o estado de agora, sem mexer em nada (a natural seguinte e a do "Trocar" saem
        /// daqui); `fora` = a vaga que ela substitui (nao conta como ocupada).</summary>
        Encomenda Gera(int idx, Encomenda fora)
        {
            List<Item> linhas = Linhas();
            string dist = _sim.Distrito.Id;   // C-CORE-05: as OrderDefs do distrito deste Sim
            OrderType[] ciclo = OrderDefs.Ciclo(dist);
            OrderType t = ciclo[idx % ciclo.Length];
            OrderDef d = null;
            foreach (OrderDef x in OrderDefs.All)   // a ultima aberta do tipo: a variante Superior do Quality vem depois da Fina
                if (x.District == dist && x.Type == t && x.ClientId == null && Aberto(x, linhas)) d = x;
            d ??= OrderDefs.Comum(dist);
            if (d.Type == OrderType.Common) d = Fregues(d, linhas) ?? d;
            return Monta(d, idx, idx == 0, linhas, fora);
        }

        /// <summary>A 7a, 14a... Common do distrito vira a encomenda do proximo freguês (em rodizio) que esta aberta; senao fica Common.</summary>
        OrderDef Fregues(OrderDef comum, List<Item> linhas)
        {
            if (!_sim.QualityRevealed || (Comuns + 1) % OrderDefs.FreguesACada != 0) return null;
            OrderDef[] regs = Regulares(comum.District);
            for (int k = 0; k < regs.Length; k++)
            {
                OrderDef r = regs[(Fregueses + k) % regs.Length];
                if (Aberto(r, linhas)) return r;
            }
            return null;
        }

        static OrderDef[] Regulares(string district) => Array.FindAll(OrderDefs.All, x => x.ClientId != null && x.District == district);

        /// <summary>O tipo `t` ja abre no distrito (metas: missao de tipo de encomenda so aparece quando da para cumprir).</summary>
        public bool TipoAberto(OrderType t)
        {
            List<Item> linhas = Linhas();
            return Array.Exists(OrderDefs.All, x => x.District == _sim.Distrito.Id && x.Type == t && x.ClientId == null && Aberto(x, linhas));
        }

        /// <summary>Condicoes do OrderDef com o estado de agora (ver OrderDef). Tudo que nao e' Common so depois das Ferramentas.</summary>
        bool Aberto(OrderDef d, List<Item> linhas)
        {
            if (d.Type != OrderType.Common && !_sim.QualityRevealed) return false;
            if (linhas.Count < d.MinLines || _sim.OrderCount < d.MinDelivered) return false;
            if (d.Requer != null && !_sim.Comprado(d.Requer)) return false;   // C-CORE-07
            if (RenomeDefs.NivelQueLibera(d.Id) >= 0 && !_sim.Renome.Libera(d.Id)) return false;
            if (!linhas.Exists(it => Fracao(it, d.MinQuality) > 0.0)) return false;
            if (d.Type == OrderType.Blueprint && Pendente(d) == null) return false;
            if (d.Item == null) return true;
            int i = Items.IndexOf(d.Item, _sim.ItemDefs);
            return i >= 0 && linhas.Contains((Item)i) && Fracao((Item)i, d.MinQuality) > 0.0;
        }

        /// <summary>B-CORE-07: o proximo Blueprint pendente da fila do OrderDef (null = sem fila ou esgotada).</summary>
        string Pendente(OrderDef d) => d.BlueprintQueue == null ? null : _sim.Projetos.ProximoDaFila(Fila(d), d.District)?.Id;

        static BlueprintSourceKind Fila(OrderDef d) => d.BlueprintQueue == OrderDefs.FilaRoyal ? BlueprintSourceKind.OrderRoyal : BlueprintSourceKind.OrderBlueprint;

        /// <summary>Linhas de produto abertas, na ordem dos itens (espada, escudo, ferramenta, joia): o rodizio de sempre.</summary>
        List<Item> Linhas()
        {
            var l = new List<Item>();
            for (int i = 0; i < _sim.ItemDefs.Length; i++) if (_sim.LineUnlocked((Item)i)) l.Add((Item)i);
            return l;
        }

        /// <summary>Fracao das pecas de `item` que a automacao ja faz no nivel `min` ou acima (melhor estacao da linha; o golpe nao conta:
        /// encomenda que so fecha com o golpe prenderia a vaga do F2P). Normal = 1; Fina com q = 15 = 0,15; Superior pede q > 100.</summary>
        double Fracao(Item item, Quality min)
        {
            if (min == Quality.Normal) return 1.0;
            int q = 0;
            foreach (Station s in _sim.Stations) if (s.Unlocked && _sim.Faz(s, item)) q = Math.Max(q, _sim.QualityPoints(s));   // C-CORE-06: a bancada configuravel conta por toda receita liberada
            return Math.Max(0.0, Math.Min(1.0, (q - ((int)min - 1) * 100) / 100.0));
        }

        /// <summary>
        /// Monta a vaga: produtos pelo rodizio das linhas a partir de idx % linhas (pulando a que ja esta noutra vaga e a que nao faz a qualidade
        /// minima; Mixed = as proximas min(Products, linhas - 1)), N = alvo x escala x RateEma x fracao da qualidade / (linhas x preco) no
        /// intervalo (a 1a de todas: OrderFirstTarget; freguês: N fixo) e premio = s x escala x RateEma, multiplo de 5, nunca abaixo do minimo.
        /// Escala = os knobs do Tuning sobre a Common (OrderSeconds / 150, OrderRewardSeconds / 10): com o padrao, a Common e' a de antes ao bit.
        /// </summary>
        Encomenda Monta(OrderDef d, int idx, bool primeira, List<Item> linhas, Encomenda fora)
        {
            Tuning T = _sim.T;
            int n = linhas.Count;
            Item[] itens;
            if (d.Item != null) itens = new[] { (Item)Items.IndexOf(d.Item, _sim.ItemDefs) };
            else
            {
                int quer = d.Type == OrderType.Mixed ? Math.Max(1, Math.Min(d.Products, n - 1)) : 1;
                var livres = new List<Item>(); var soVariada = new List<Item>(); var ocupadas = new List<Item>();
                for (int k = 0; k < n; k++)
                {
                    Item it = linhas[(idx + k) % n];
                    if (Fracao(it, d.MinQuality) <= 0.0) continue;
                    if (!Vagas.Exists(v => v != fora && Array.IndexOf(v.Itens, it) >= 0)) livres.Add(it);
                    else (Vagas.Exists(v => v != fora && v.Itens.Length == 1 && v.Itens[0] == it) ? ocupadas : soVariada).Add(it);
                }
                // todas ocupadas: repete primeiro o que so esta dentro de uma Mixed (dois cartoes "Venda 30 espadas" lado a lado confundem)
                livres.AddRange(soVariada); livres.AddRange(ocupadas);
                itens = livres.GetRange(0, Math.Min(quer, livres.Count)).ToArray();
            }
            var alvo = new int[itens.Length];
            double escala = d.TargetSeconds * (double)T.OrderSeconds / OrderDefs.AlvoBase;
            for (int k = 0; k < itens.Length; k++)
            {
                if (d.FixedN > 0) { alvo[k] = d.FixedN; continue; }
                if (primeira) { alvo[k] = T.OrderFirstTarget; continue; }
                double raw = escala * _sim.RateEma / (n * _sim.PriceOf(itens[k]));
                if (d.MinQuality > Quality.Normal) raw *= Fracao(itens[k], d.MinQuality);
                alvo[k] = Math.Max(d.TargetMin, Math.Min(d.TargetMax, (int)Math.Round(raw)));
            }
            bool bonus = d.BonusDeadline > 0f;
            return new Encomenda(d, idx, itens, alvo, Premio(d.RewardSeconds, d.RewardMin), bonus ? Premio(d.BonusRewardSeconds, d.RewardMin) : 0,
                d.BonusDeadline, Pendente(d));
        }

        /// <summary>`s` segundos (antes da escala) da taxa online, multiplo de 5, nunca abaixo de `min`. Event (s = 0): so pontos, nada de ouro.</summary>
        long Premio(float s, int min) => s <= 0f ? 0 : Math.Max(min, 5 * (long)Math.Round(_sim.RateEma * (s * (double)_sim.T.OrderRewardSeconds / OrderDefs.PremioBase) / 5.0));

        // ------------------------------------------------------------------ save (ord= espelho + linhas enc*; o Load de antes ignora enc*)

        public void Save(StringBuilder sb)
        {
            var ci = CultureInfo.InvariantCulture;
            Encomenda p = Principal;
            bool um = p != null && p.Itens.Length == 1;   // espelho para a 0.6-0.8: a principal de 1 item (Mixed e evento ficam de fora)
            // FASE9: encomenda (item, alvo, progresso, premio, entregues, relogio ate a proxima, trocadas)
            sb.Append("ord=").Append(um ? (int)p.Itens[0] : -1).Append(',').Append(um ? p.Alvo[0] : 0).Append(',').Append(um ? p.Feito[0] : 0).Append(',')
              .Append(um ? p.Premio : 0).Append(',').Append(_sim.OrderCount).Append(',').Append(OrderIn.ToString("0.###", ci)).Append(',').Append(Pulos).Append('\n');
            sb.Append("enc=").Append(Abertas).Append(',').Append(Comuns).Append(',').Append(Fregueses).Append(',').Append(Eventos).Append('\n');
            foreach (Encomenda e in Vagas)
            {
                sb.Append("enc.v=").Append(e.Def.Id).Append(',').Append(e.Idx).Append(',').Append(e.Premio).Append(',').Append(e.PremioBonus).Append(',')
                  .Append(e.Prazo.ToString("0.###", ci)).Append(',').Append(e.Blueprint ?? "-");
                for (int k = 0; k < e.Itens.Length; k++) sb.Append(',').Append(_sim.ItemDefs[(int)e.Itens[k]].Id).Append(':').Append(e.Alvo[k]).Append(':').Append(e.Feito[k]);
                sb.Append('\n');
            }
            if (Coracoes.Count == 0) return;
            var ids = new List<string>(Coracoes.Keys);
            ids.Sort(StringComparer.Ordinal);
            sb.Append("enc.cor=").Append(string.Join(",", ids.ConvertAll(id => id + ":" + Coracoes[id].ToString(ci)))).Append('\n');
        }

        /// <summary>Depois do Recompute (a linha tem de estar aberta). Lixo = padrao; vaga com OrderDef ou item que este build nao conhece fica
        /// de fora (UnknownIds). Save sem enc= (0.6-0.8): a encomenda de antes vira uma Common igual (mesmo item, alvo, progresso e premio) na
        /// posicao entregues + trocadas, e o rodizio segue dali.</summary>
        internal void Load(string text)
        {
            Tuning T = _sim.T;
            string[] ord = null; string[] enc = null;
            var vagas = new List<string[]>();
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw.Substring(0, eq).Trim(); string[] p = raw.Substring(eq + 1).Trim().Split(',');
                if (key == "ord") ord = p; else if (key == "enc") enc = p; else if (key == "enc.v") vagas.Add(p);
                else if (key == "enc.cor")
                    foreach (string tok in p)
                    {
                        int c = tok.LastIndexOf(':');
                        if (c > 0) Coracoes[tok.Substring(0, c)] = Math.Max(0, I(tok.Substring(c + 1), 0));
                    }
            }
            if (ord != null && ord.Length >= 6)   // FASE9; save antigo sem ord=: do zero
            {
                _sim.OrderCount = Math.Max(0, I(ord[4], 0));
                OrderIn = Math.Max(0f, Math.Min(T.OrderFirstDelay, F(ord[5], T.OrderFirstDelay)));
                if (ord.Length >= 7) Pulos = Math.Max(0, I(ord[6], 0));   // E-PLAT-02; save de antes: nenhuma trocada
            }
            if (enc == null)
            {
                Abertas = Comuns = _sim.OrderCount + Pulos;
                int it = ord != null && ord.Length >= 6 ? I(ord[0], -1) : -1, alvo = ord != null && ord.Length >= 6 ? Clamp(I(ord[1], 0), 0, T.OrderMax) : 0;
                if (alvo <= 0 || !_sim.IsProduct((Item)it) || !_sim.LineUnlocked((Item)it)) return;   // joia sem joalheria: nao volta; a contagem fica
                var e = new Encomenda(OrderDefs.Comum(_sim.Distrito.Id), Abertas, new[] { (Item)it }, new[] { alvo }, Math.Max(0, L(ord[3], 0)), 0, 0f, null);
                e.Feito[0] = Clamp(I(ord[2], 0), 0, alvo);
                Vagas.Add(e);
                Abertas++; Comuns++;
                return;
            }
            Abertas = Math.Max(0, I(enc[0], 0));
            if (enc.Length >= 4) { Comuns = Math.Max(0, I(enc[1], 0)); Fregueses = Math.Max(0, I(enc[2], 0)); Eventos = Math.Max(0, I(enc[3], 0)); }
            foreach (string[] p in vagas)
            {
                OrderDef d = p.Length >= 7 ? Array.Find(OrderDefs.All, x => x.Id == p[0].Trim()) : null;
                if (d == null) { _sim.UnknownIds++; continue; }
                var itens = new List<Item>(); var alvos = new List<int>(); var feitos = new List<int>();
                int teto = Math.Max(Math.Max(d.TargetMax, d.FixedN), T.OrderFirstTarget);
                bool ok = true;
                for (int k = 6; k < p.Length; k++)
                {
                    string[] f = p[k].Split(':');
                    int i = Items.IndexOf(f[0].Trim(), _sim.ItemDefs);
                    if (i < 0) { _sim.UnknownIds++; ok = false; break; }
                    int a = f.Length > 1 ? Clamp(I(f[1], 0), 0, teto) : 0;
                    if (a <= 0 || !_sim.IsProduct((Item)i) || !_sim.LineUnlocked((Item)i)) { ok = false; break; }
                    itens.Add((Item)i); alvos.Add(a); feitos.Add(f.Length > 2 ? Clamp(I(f[2], 0), 0, a) : 0);
                }
                if (!ok || itens.Count == 0) continue;
                string bp = p[5].Trim();
                var e = new Encomenda(d, Math.Max(0, I(p[1], 0)), itens.ToArray(), alvos.ToArray(), Math.Max(0, L(p[2], 0)), Math.Max(0, L(p[3], 0)),
                    Math.Max(0f, Math.Min(d.BonusDeadline, F(p[4], 0f))), bp.Length == 0 || bp == "-" ? null : bp);
                for (int k = 0; k < feitos.Count; k++) e.Feito[k] = feitos[k];
                Vagas.Add(e);
                if (e.Tipo != OrderType.Event) Abertas = Math.Max(Abertas, e.Idx + 1);   // lixo no contador nao repete posicao do rodizio
            }
        }

        static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
        static int I(string s, int d) => int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        static long L(string s, long d) => long.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : d;
        static float F(string s, float d) => float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : d;
    }
}
