using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// B-CORE-08 (GDD §9, §13; V13): o Hall das obras-primas. Golpe de mestre numa peca que ja sairia Superior, com o projeto (Sim.Projetos) e as
    /// condicoes da 1a Masterwork pendente da familia, vira essa Masterwork (Sim.Grade pelo gancho MasterworkPending): vai ao Hall, nunca a
    /// pilha, ao balcao nem ao GoldEarned. Cada uma entra 1x (a 2a vez sai Superior e vende); a 1a credita o Renome dela pelo ID de transacao
    /// mw:{id} (Renome.Credita). Ajudante, manager e offline nunca dao o golpe, entao nunca fazem uma. Bonus (GDD §13, FS-6; revisar): familia
    /// completa no Hall = +5% fixo no preco de venda dela (Sim.PriceOf). Nao e' moeda nem saldo (V9): so o registro.
    /// Save: linha hall=id:unix (unix = relogio do LiveOps na 1a vez; 0 = sem relogio, bot e testes). Sem ela: Hall vazio, e o mw: que o
    /// ledger do Renome ja tiver volta como registrada (nada paga de novo).
    /// </summary>
    public sealed class Hall
    {
        public const string Prefixo = "mw:";
        readonly Sim _sim;
        /// <summary>O catalogo desta partida (padrao MasterworksDefs.All; so o teste injeta outro). Ev.Masterwork.B = indice aqui.</summary>
        public readonly MasterworkDef[] Defs;
        /// <summary>Registradas: ID -> unix da 1a vez. Fica tambem o ID que o catalogo nao conhece (save de um build mais novo).</summary>
        public readonly Dictionary<string, long> Feitas = new Dictionary<string, long>();

        public Hall(Sim sim, MasterworkDef[] defs) { _sim = sim; Defs = defs ?? MasterworksDefs.All; }

        public static string Tx(MasterworkDef d) => Prefixo + d.Id;
        public bool Tem(string id) => id != null && Feitas.ContainsKey(id);
        /// <summary>Quantas do catalogo estao no Hall.</summary>
        public int Count { get { int n = 0; foreach (MasterworkDef d in Defs) if (Tem(d.Id)) n++; return n; } }
        /// <summary>O Hall abre com a 1a obra (col_mw_1 do CONTEUDO_V1 §10.3); antes, a aba e' a vitrine dos projetos (Colecao.Aberta).</summary>
        public bool Aberto => Count > 0;

        /// <summary>Familia de produto da peca da bancada `s` (null = nao faz produto).</summary>
        public string Familia(Station s) => s.Produces && _sim.IsProduct(s.OutItem) ? _sim.ItemDefs[(int)s.OutItem].Family : null;

        /// <summary>Indice da 1a Masterwork da familia da peca de `s` (ordem da tabela) com projeto e condicoes, ainda fora do Hall; -1 = nenhuma.</summary>
        public int Pendente(Station s)
        {
            string f = Familia(s);
            if (f == null) return -1;
            for (int i = 0; i < Defs.Length; i++) if (Defs[i].Familia == f && !Tem(Defs[i].Id) && Liberada(Defs[i])) return i;
            return -1;
        }

        /// <summary>Projeto possuido e todas as condicoes valendo.</summary>
        public bool Liberada(MasterworkDef d) => _sim.Projetos.Tem(d.Blueprint) && Array.TrueForAll(d.Condicoes, c => Valor(c) >= c.N);

        /// <summary>Onde a condicao esta agora (o painel mostra "valor / N"). Tudo contador do nucleo; nada pago (MasterworksDefs.Validar).</summary>
        public long Valor(CondicaoMasterwork c)
        {
            int i = Items.IndexOf(c.Ref, _sim.ItemDefs);
            switch (c.Kind)
            {
                case CondicaoMw.Vendidos: return i < 0 ? 0 : _sim.SoldItems[i];
                case CondicaoMw.VendidosQualidade: return i < 0 ? 0 : _sim.Colecao.VendidasAoMenos(i, c.MinQ);
                case CondicaoMw.NivelEstacao:
                    int n = 0;
                    foreach (Station s in _sim.Stations) if (s.Produces && s.Recipe.Family == c.Ref) n = Math.Max(n, s.Level);
                    return n;
                case CondicaoMw.Renome: return _sim.Renome.Nivel;
                case CondicaoMw.EncomendasItem: return Contador("order_done", i);
                case CondicaoMw.EncomendasTipo: OrderType? t = MasterworksDefs.Tipo(c.Ref); return t == null ? 0 : Contador("order_complete", (int)t.Value);
                case CondicaoMw.NivelWorker: int k = WorkerDefs.IndexOf(c.Ref, _sim.Equipe.Defs); return k < 0 ? 0 : _sim.Equipe.Nivel(k);
                case CondicaoMw.PecasComLiga: return i < 0 ? 0 : _sim.PecasComLiga[i];   // C-CORE-05
                case CondicaoMw.ClientesRicos: return _sim.ClientesRicos;   // C-CORE-06: RichClients
                case CondicaoMw.Feitos:   // C-CORE-06: GemsCut (as gemas que os lapidarios fizeram; o contador da estacao vai no save)
                    long f = 0;
                    foreach (Station s in _sim.Stations) if (s.Produces && s.Receitas == null && (int)s.OutItem == i) f += (long)s.Crafted * s.Recipe.Qtd;
                    return f;
                case CondicaoMw.CotaBlocos: return i < 0 ? 0 : _sim.CotaBlocos[i];       // C-CORE-07
                case CondicaoMw.CotaNoPrazo: return i < 0 ? 0 : _sim.CotaNoPrazo[i];
                case CondicaoMw.Contratos: return _sim.Contratos.Concluidos.Count;
                case CondicaoMw.PecasEnergizadas: return i < 0 ? 0 : _sim.PecasEnergizadas[i];   // C-CORE-08
                case CondicaoMw.SequenciaEstavel: Station a = _sim.Est(c.Ref); return a == null ? 0 : a.Sequencia;
                case CondicaoMw.Importados: return _sim.ImportedUnits;   // C-CORE-09
                case CondicaoMw.Lotes: return _sim.ExportLots;
                case CondicaoMw.Charters: return _sim.Imp?.Charters.Count ?? 0;
                default: return 0;
            }
        }

        /// <summary>Contador vitalicio das metas (o Sim.Emit conta sempre; save nas linhas mt.*).</summary>
        long Contador(string ev, int filtro) => filtro >= 0 && _sim.Metas.Cont.TryGetValue(Metas.Chave(ev, filtro), out long v) ? v : 0;

        /// <summary>Nivel da proxima peca de `s` sem golpe (a mesma conta do Sim.Grade, sem mexer no acumulador): o bot Ideal espera a Superior.</summary>
        public Quality ProximaPeca(Station s)
        {
            int q = _sim.QualityPoints(s);
            return (Quality)Math.Min(q / 100 + (s.QAcc + q % 100 >= 100 ? 1 : 0), (int)Quality.Superior);
        }

        /// <summary>Sim.Produce: a peca de `s` saiu Masterwork. Registra a pendente (1x: ela deixa de ser pendente), credita o Renome dela 1x
        /// pelo ID e emite Ev.Masterwork (A = item, B = indice; -1 = obra-prima sem MasterworkDef, so o gancho de teste).</summary>
        internal void Registra(Station s)
        {
            int k = Pendente(s);
            if (k >= 0)
            {
                Feitas[Defs[k].Id] = _sim.LiveOps.Relogio;
                _sim.Renome.Credita(Tx(Defs[k]), Defs[k].Renome);
            }
            _sim.Emit(Ev.Masterwork, (int)s.OutItem, k, s.Pos);
        }

        /// <summary>+5% (MasterworksDefs.BonusFamiliaPct) no preco do `item` quando todas as Masterworks da familia dele estao no Hall; senao 0.</summary>
        public int BonusPct(Item item)
        {
            if ((uint)item >= (uint)_sim.ItemDefs.Length) return 0;
            string f = _sim.ItemDefs[(int)item].Family;
            bool alguma = false;
            foreach (MasterworkDef d in Defs)
                if (d.Familia == f) { if (!Tem(d.Id)) return 0; alguma = true; }
            return alguma ? MasterworksDefs.BonusFamiliaPct : 0;
        }

        // ------------------------------------------------------------------ save (o Sim.Load de antes ignora chave desconhecida)

        public void Save(StringBuilder sb)
        {
            if (Feitas.Count == 0) return;   // ninguem no Hall: o texto do save fica o de antes
            var t = new List<string>();
            foreach (MasterworkDef d in Defs) if (Feitas.TryGetValue(d.Id, out long u)) t.Add(d.Id + ":" + u.ToString(CultureInfo.InvariantCulture));
            foreach (KeyValuePair<string, long> kv in Feitas) if (MasterworksDefs.IndexOf(kv.Key, Defs) < 0) t.Add(kv.Key + ":" + kv.Value.ToString(CultureInfo.InvariantCulture));
            sb.Append("hall=").Append(string.Join(",", t)).Append('\n');
        }

        /// <summary>Le hall= (lixo = fora; data ruim = 0; ID que o catalogo nao tem fica e conta em UnknownIds). Chamar depois do Renome.Load: o
        /// mw: do ledger sem linha no hall= volta registrado (data 0).</summary>
        public void Load(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                if (!raw.StartsWith("hall=", StringComparison.Ordinal)) continue;
                foreach (string tok in raw.Substring(5).Trim().Split(','))
                {
                    string[] kv = tok.Split(':');
                    string id = kv[0].Trim();
                    if (id.Length == 0 || Feitas.ContainsKey(id)) continue;
                    Feitas[id] = kv.Length > 1 && long.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long u) ? Math.Max(0, u) : 0;
                    if (MasterworksDefs.IndexOf(id, Defs) < 0) _sim.UnknownIds++;
                }
            }
            // C-CORE-03: o ledger e' o do Imperio (o Renome da raiz): so volta a obra-prima deste distrito (a de build mais novo segue no rn=)
            foreach (string tx in _sim.Renome.Ledger)
                if (tx.StartsWith(Prefixo, StringComparison.Ordinal) && !Feitas.ContainsKey(tx.Substring(Prefixo.Length))
                    && MasterworksDefs.IndexOf(tx.Substring(Prefixo.Length), Defs) >= 0) Feitas[tx.Substring(Prefixo.Length)] = 0;
        }
    }

    /// <summary>
    /// B-CORE-08 (GDD §13, DM §34; CONTEUDO_V1 §10.3): a Collection, DERIVADA dos contadores do nucleo. Abas: Produtos (produto x Normal, Fina,
    /// Superior com pelo menos 1 vendida), Masterworks (no Hall), Workers (contratado; nivel maximo) e Blueprints (possuidos). Abre com o 1o
    /// projeto de Masterwork (~100 espadas), depois das Ferramentas (FTUE intocado). Marco alcancado paga 1x o Renome dele (e o cosmetico =
    /// a posse do ID) pelo ID de transacao col:{id} no ledger do Renome: save/load, reabrir e save antigo nunca pagam 2x; nada de ouro nem saldo.
    /// Produto x qualidade sai dos contadores que ja existem: Normal = vendidas (Sim.SoldItems) - Fina ou melhor (Projetos.Finas), Fina =
    /// Finas - Superiores, Superior = Superiores (o unico contador novo, por item). Save: linha colq=id:n (so com alguma Superior vendida;
    /// sem ela, 0: o save antigo conta a Superior de antes como Fina; ponytail: nenhum save guardava Superior por item). Os marcos ja
    /// alcancados pagam no 1o Tick depois de aberta (o retroativo, 1x).
    /// </summary>
    public sealed class Colecao
    {
        public const string Prefixo = "col:";
        readonly Sim _sim;
        public readonly MarcoColecaoDef[] Marcos;
        /// <summary>Pecas Superiores vendidas por item, do B-CORE-08 em diante.</summary>
        public readonly int[] Superiores;

        public Colecao(Sim sim, MarcoColecaoDef[] marcos)
        {
            _sim = sim;
            Marcos = marcos ?? MasterworksDefs.Marcos;
            Superiores = new int[sim.ItemDefs.Length];
        }

        public static string Tx(MarcoColecaoDef m) => Prefixo + m.Id;
        public bool Pago(MarcoColecaoDef m) => _sim.Renome.Ledger.Contains(Tx(m));

        /// <summary>Depois das Ferramentas e com algum projeto de Masterwork (GDD §13, §19: "Collection no 1o projeto de Masterwork").</summary>
        public bool Aberta
        {
            get
            {
                if (!_sim.QualityRevealed) return false;
                foreach (BlueprintDef d in _sim.Projetos.Defs) if (d.Kind == BlueprintKind.Masterwork && _sim.Projetos.Tem(d.Id)) return true;
                return false;
            }
        }

        /// <summary>Sim.Sell: uma peca do nivel `l` vendida (so a Superior conta aqui; o resto o Sim e o Projetos ja contam).</summary>
        public void OnVenda(int item, Quality l)
        {
            if (l == Quality.Superior && (uint)item < (uint)Superiores.Length) Superiores[item]++;
        }

        /// <summary>Vendidas do `item` no nivel `min` ou acima.</summary>
        public long VendidasAoMenos(int item, Quality min) =>
            min >= Quality.Superior ? Superiores[item] : min == Quality.Fine ? _sim.Projetos.Finas[item] : _sim.SoldItems[item];

        /// <summary>Vendidas do `item` exatamente no nivel `l` (Normal, Fina, Superior).</summary>
        public long Vendidas(int item, Quality l) =>
            Math.Max(0, VendidasAoMenos(item, l) - (l < Quality.Superior ? VendidasAoMenos(item, l + 1) : 0));

        /// <summary>Entradas possiveis da aba neste conteudo.</summary>
        public int Total(AbaColecao a)
        {
            switch (a)
            {
                case AbaColecao.Produtos:
                    int p = 0;
                    for (int i = 0; i < _sim.ItemDefs.Length; i++) if (_sim.IsProduct((Item)i)) p++;
                    return p * Sim.Graus;
                case AbaColecao.Masterworks: return _sim.Hall.Defs.Length;
                case AbaColecao.Workers: return 2 * _sim.Equipe.Defs.Length;
                default: return _sim.Projetos.Defs.Length;
            }
        }

        /// <summary>Entradas ja feitas da aba (derivadas dos contadores; o unico novo e o colq=).</summary>
        public int Feitas(AbaColecao a)
        {
            int n = 0;
            switch (a)
            {
                case AbaColecao.Produtos:
                    for (int i = 0; i < Superiores.Length; i++)
                        if (_sim.IsProduct((Item)i)) for (Quality l = Quality.Normal; l <= Quality.Superior; l++) if (Vendidas(i, l) > 0) n++;
                    return n;
                case AbaColecao.Masterworks: return _sim.Hall.Count;
                case AbaColecao.Workers:
                    for (int k = 0; k < _sim.Equipe.Defs.Length; k++)
                    {
                        int l = _sim.Equipe.Nivel(k);
                        n += (l > 0 ? 1 : 0) + (l >= _sim.Equipe.Defs[k].MaxNivel ? 1 : 0);
                    }
                    return n;
                default:
                    foreach (BlueprintDef d in _sim.Projetos.Defs) if (_sim.Projetos.Tem(d.Id)) n++;
                    return n;
            }
        }

        /// <summary>A cada Tick (Sim.Tick): aberta, paga 1x cada marco alcancado (Renome.Credita col:{id}) e emite Ev.Colecao.</summary>
        public void Checa()
        {
            if (!Aberta) return;
            for (int i = 0; i < Marcos.Length; i++)
            {
                MarcoColecaoDef m = Marcos[i];
                if (Pago(m) || Feitas(m.Aba) < m.N) continue;
                _sim.Renome.Credita(Tx(m), m.Renome);
                _sim.Emit(Ev.Colecao, i, m.Renome, _sim.Player.Pos);
            }
        }

        // ------------------------------------------------------------------ save

        public void Save(StringBuilder sb)
        {
            var t = new List<string>();
            for (int i = 0; i < Superiores.Length; i++) if (Superiores[i] > 0) t.Add(_sim.ItemDefs[i].Id + ":" + Superiores[i].ToString(CultureInfo.InvariantCulture));
            if (t.Count > 0) sb.Append("colq=").Append(string.Join(",", t)).Append('\n');   // nenhuma Superior vendida: o texto do save fica o de antes
        }

        /// <summary>Le colq= (lixo e negativo = 0; ID que o conteudo nao tem fica de fora).</summary>
        public void Load(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                if (!raw.StartsWith("colq=", StringComparison.Ordinal)) continue;
                foreach (string tok in raw.Substring(5).Trim().Split(','))
                {
                    string[] kv = tok.Split(':');
                    int i = Items.IndexOf(kv[0].Trim(), _sim.ItemDefs);
                    if (i >= 0 && kv.Length > 1) Superiores[i] = int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? Math.Max(0, v) : 0;
                }
            }
        }
    }
}
