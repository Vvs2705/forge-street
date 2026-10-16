using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// C-CORE-09 (CONTEUDO_V1 §2.6 regras 1-3): as rotas de importacao e os lotes de exportacao do Mensageiro (wk_felix) na Grand Forge,
    /// deterministicos (V7). Nada aqui le relogio: as duas coisas andam com o dt do Tick, entao o offline e o cofre nunca importam nem exportam.
    /// - Rotas (Kind.Import, o cais; abre com o mensageiro): capacidade Tuning.ImportCap un/min (+ImportCais2 com o cais2, +ImportNivel por nivel
    ///   do Felix acima do 1, ate ImportMax); uma unidade a cada 60/capacidade s, sempre para a rota (DistrictDef.Importados) cuja bancada de
    ///   destino tem menos daquele importado na entrada (empate em rodizio pela ordem do §3.1, a partir da rota seguinte a ultima: desvio, BALANCE.md
    ///   §41; com a ordem fixa o colar ganhava todo empate). ponytail: o Mensageiro entrega direto na entrada
    ///   da bancada (o cais e' o ponto de chegada, sem pilha nem carregador; revisar: o catalogo poe a estrela4 levando os importados do cais);
    ///   sem entrada com espaco, ele espera (nao importa a toa). Cada unidade chama o Imperio.Importou: PriceOf(item, Normal) x ImportCustoPct%
    ///   sai da renda de fundo do distrito de origem no minuto, nunca abaixo de 0. Contador ImportedUnits (na entrega: a entrada so tem uso).
    /// - Lote (Kind.Export, o patio de exportacao): o jogador escolhe o produto (EscolheLote; -1 = "o mais estocado" no balcao, o padrao); o
    ///   carregador de produto e o ferreiro levam ao patio quando ele aceita (o produto do lote e vaga; Sim.Cotas: CotaAceita/Destino), senao ao
    ///   balcao. A cada ExportPeriodo s (ExportPeriodo2 com o patio_exportacao2) o Mensageiro parte com o que tiver, ate TamanhoLote pecas,
    ///   pagando ExportPct% (125) da soma dos PriceOf(peca, nivel), arredondado para cima: e' VENDA (GoldEarned, RateEma, vendas, Renome das
    ///   Superiores, projetos, Collection, encomenda, contrato, Ev.Sold por peca). Vazio nao parte nem conta. Contador ExportLots.
    /// Save: linhas opcionais gf= (produto do lote, relogios e contadores) e gfl=/gflq= (o lote no patio): so no distrito com cais ou patio e
    /// fora do padrao; save sem elas = rotas paradas e patio vazio. Distrito sem cais nem patio: tudo no-op (os distritos 1-4 ao byte).
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Upgrades do §5.6: o major unlock (contrata o Felix, abre o cais e o patio), o 2o cais e o patio de exportacao 2.</summary>
        public const string Mensageiro = "mensageiro", Cais2 = "cais2", Exportacao2 = "patio_exportacao2";
        /// <summary>O Imperio deste Sim (o construtor e o Adiciona dele); null = Sim solto (teste): as rotas nao descontam de ninguem e a
        /// condicao DistrictCharters fica em 0.</summary>
        public Imperio Imp;
        /// <summary>ImportedUnits e ExportLots do catalogo (condicoes de Masterwork e o marco do bp_var_sino_ouro). Vao no save (gf=).</summary>
        public int ImportedUnits, ExportLots;
        /// <summary>O produto do lote escolhido pelo jogador; -1 = o mais estocado no balcao (AlvoLote). Vai no save.</summary>
        public int LoteProduto = -1;
        /// <summary>s desde o ultimo importado e desde a ultima partida do Mensageiro (tempo de jogo). Vao no save.</summary>
        public float ImportT, ExportT;

        Station _cais, _exportacao; bool _gfOk;
        int _uCais2 = -2, _uExp2 = -2;
        int[] _lote, _impH, _porRota; int[][] _loteQ;

        void AchaGf()
        {
            if (_gfOk) return;
            _cais = Stations.Find(s => s.Kind == Kind.Import); _exportacao = Stations.Find(s => s.Kind == Kind.Export); _gfOk = true;
        }

        /// <summary>O cais (Kind.Import) e o patio de exportacao (Kind.Export) do distrito, ou null (distritos 1-4).</summary>
        public Station Cais { get { AchaGf(); return _cais; } }
        public Station Exportacao { get { AchaGf(); return _exportacao; } }
        /// <summary>O lote no patio, por item (com os niveis em LoteQ, como uma pilha).</summary>
        public int[] Lote => _lote ??= new int[ItemDefs.Length];
        public int[][] LoteQ => _loteQ ??= Lotes(ItemDefs.Length);
        /// <summary>Importados por rota (metrica do econsim: o fundo de cada origem; fora do save).</summary>
        public int[] PorRota => _porRota ??= new int[Distrito.Importados?.Length ?? 0];

        bool Tem(ref int u, string id) { if (u == -2) u = Upgrades.IndexOf(id, UpgradeDefs); return u >= 0 && Bought[u]; }
        bool TemCais2 => Tem(ref _uCais2, Cais2);
        bool TemExp2 => Tem(ref _uExp2, Exportacao2);

        /// <summary>Nivel do Mensageiro (o WorkerDef Courier do distrito; 0 = nao contratado).</summary>
        public int NivelMensageiro
        {
            get { for (int k = 0; k < Equipe.Defs.Length; k++) if (Equipe.Defs[k].Papel == WorkerRole.Courier) return Equipe.Nivel(k); return 0; }
        }

        /// <summary>Unidades por minuto das rotas agora (0 = sem cais aberto ou sem o Felix).</summary>
        public int CapacidadeRotas
        {
            get
            {
                if (Cais == null || !Cais.Unlocked) return 0;
                int n = NivelMensageiro;
                return n == 0 ? 0 : Math.Min(T.ImportMax, T.ImportCap + (TemCais2 ? T.ImportCais2 : 0) + T.ImportNivel * (n - 1));
            }
        }

        /// <summary>s entre duas partidas do Mensageiro e o teto do lote agora.</summary>
        public float PeriodoLote => TemExp2 ? T.ExportPeriodo2 : T.ExportPeriodo;
        public int TamanhoLote => (TemExp2 ? T.ExportLote2 : T.ExportLote) + T.ExportNivel * Math.Max(0, NivelMensageiro - 1);
        /// <summary>Pecas no lote (todos os produtos).</summary>
        public int LoteTotal { get { int n = 0; foreach (int x in Lote) n += x; return n; } }

        /// <summary>O produto que o lote recebe agora: o escolhido (com a linha aberta) ou, no automatico, o mais estocado no balcao (empate: a
        /// ordem dos itens); -1 = nenhum.</summary>
        public int AlvoLote
        {
            get
            {
                if (LoteProduto >= 0) return LineUnlocked((Item)LoteProduto) ? LoteProduto : -1;
                int best = -1;
                for (int i = (int)Item.Sword; i < ItemDefs.Length; i++) if (LineUnlocked((Item)i) && (best < 0 || Stock[i] > Stock[best])) best = i;
                return best;
            }
        }

        /// <summary>O patio de exportacao aberto recebe mais `n` + 1 de `p` (o produto do lote e vaga alem das ja a caminho).</summary>
        bool ExportaCabe(Item p, int n) => Exportacao != null && Exportacao.Unlocked && (int)p == AlvoLote && TamanhoLote - LoteTotal > n;
        public bool ExportaAceita(Item p) => ExportaCabe(p, 0);

        /// <summary>O jogador escolhe o produto do lote (-1 = o mais estocado): produto de linha aberta do distrito. As pecas ja no lote ficam (o
        /// Mensageiro leva o que tiver). Sem patio aberto ou produto invalido: false e nada muda.</summary>
        public bool EscolheLote(int produto)
        {
            if (Exportacao == null || !Exportacao.Unlocked) return false;
            if (produto >= 0 && (!IsProduct((Item)produto) || !LineUnlocked((Item)produto))) return false;
            LoteProduto = produto;
            return true;
        }

        /// <summary>Do Sim.Action: produto na mao de `c` que o patio de exportacao aceita agora (o mais adiantado primeiro), 6 = entrega; senao 0.</summary>
        int AcaoExporta(Carrier c, out Item item)
        {
            item = Item.Ore;
            for (int k = ItemDefs.Length - 1; k >= (int)Item.Sword; k--)
                if (c.Held[k] > 0 && IsProduct((Item)k) && ExportaAceita((Item)k)) { item = (Item)k; return 6; }
            return 0;
        }

        /// <summary>Do Sim.Interact: 1 peca de `it` da mao de `c` vai ao lote, com o nivel dela.</summary>
        void EntregaExporta(Carrier c, Item it)
        {
            if (!ExportaAceita(it)) return;
            Put(ref Lote[(int)it], LoteQ[(int)it], Take(ref c.Held[(int)it], c.HeldQ[(int)it]));
        }

        /// <summary>Do Sim.Tick: as rotas (uma unidade a cada 60/capacidade s; sem destino, o Mensageiro espera) e a partida do lote.</summary>
        void TickRotas(float dt)
        {
            if (Cais == null && Exportacao == null) return;
            int cap = CapacidadeRotas;
            if (cap > 0)
            {
                float passo = 60f / cap;
                ImportT += dt;
                while (ImportT >= passo)
                {
                    if (!Importa()) { ImportT = passo; break; }
                    ImportT -= passo;
                }
            }
            if (Exportacao == null || !Exportacao.Unlocked) return;
            ExportT += dt;
            if (ExportT < PeriodoLote) return;
            ExportT -= PeriodoLote;
            if (LoteTotal > 0) Parte();   // vazio nao parte nem conta ciclo (§14)
        }

        /// <summary>Handle do importado da rota `r` neste distrito (-1 = o distrito nao tem o item).</summary>
        int ImportHandle(int r)
        {
            if (_impH == null) _impH = Array.ConvertAll(Distrito.Importados ?? new (string, string)[0], x => Items.IndexOf(x.Item, ItemDefs));
            return _impH[r];
        }

        /// <summary>Uma unidade chega pela rota cuja bancada de destino (aberta, com espaco) tem menos daquele importado na entrada; empate em
        /// rodizio (a partir de ImportedUnits % rotas, na ordem da tabela). false = nenhuma rota tem para onde entregar.</summary>
        bool Importa()
        {
            var rotas = Distrito.Importados;
            if (rotas == null) return false;
            int best = -1, bestK = -1, bestN = int.MaxValue; Station bestS = null;
            for (int j = 0; j < rotas.Length; j++)
            {
                int r = (j + ImportedUnits) % rotas.Length, i = ImportHandle(r);
                if (i < 0) continue;
                foreach (Station s in Stations)
                {
                    int k = s.InIndex((Item)i);
                    if (!s.Unlocked || k < 0 || s.Ins[k] >= s.InCap || s.Ins[k] >= bestN) continue;
                    best = r; bestS = s; bestK = k; bestN = s.Ins[k];
                }
            }
            if (best < 0) return false;
            bestS.Ins[bestK]++; ImportedUnits++; PorRota[best]++;
            Imp?.Importou(rotas[best].Origem, rotas[best].Item, T.ImportCustoPct);
            Emit(Ev.Deposited, ImportHandle(best), 10, bestS.Pos);   // B = 10: veio pela rota do Mensageiro
            return true;
        }

        /// <summary>O Mensageiro parte com o lote (regra 3): ExportPct% da soma dos PriceOf, para cima; Ev.Sold por peca (B = a parte dela; a soma
        /// = o pago) e Ev.ExportLot (A = pecas, B = ouro).</summary>
        void Parte()
        {
            V2 pos = Exportacao.Pos;
            long soma = 0, normal = 0;
            int total = 0;
            for (int p = 0; p < Lote.Length; p++)
            {
                if (Lote[p] == 0) continue;
                Fit(Lote[p], LoteQ[p]);
                int resto = Lote[p];
                for (int l = 1; l < Graus; l++) { soma += (long)LoteQ[p][l] * PriceOf((Item)p, (Quality)l); resto -= LoteQ[p][l]; }
                soma += (long)resto * PriceOf((Item)p, Quality.Normal);
                normal += (long)Lote[p] * PriceOf((Item)p, Quality.Normal);
                total += Lote[p];
            }
            long pago = (soma * T.ExportPct + 99) / 100, falta = pago;
            int n = 0;
            for (int p = 0; p < Lote.Length; p++)
            {
                string id = ItemDefs[p].Id;
                while (Lote[p] > 0)
                {
                    Quality q = Take(ref Lote[p], LoteQ[p]);
                    Renome.OnVendaQualidade((int)q); Projetos.OnVenda(p, q); Colecao.OnVenda(p, q); Encomendas.OnVenda((Item)p, q); Contratos.OnVenda(id, q);
                    long parte = ++n == total ? falta : Math.Min(falta, pago * PriceOf((Item)p, q) / Math.Max(1, soma));
                    falta -= parte;
                    SoldItems[p]++;
                    Emit(Ev.Sold, p, (int)Math.Min(int.MaxValue, parte), pos);
                }
            }
            QualityGold += soma - normal;   // ponytail: o premio de qualidade sem o x1,25 do lote (como o patio de cotas)
            Gold += pago; GoldEarned += pago; Sales += total;
            if (FirstSaleTime < 0f) FirstSaleTime = Time;
            ExportLots++;
            Emit(Ev.ExportLot, total, (int)Math.Min(int.MaxValue, pago), pos);
        }

        /// <summary>O importado `i` chega por rota neste distrito (o validator e a View).</summary>
        public bool Importado(Item i) => Distrito.Importados != null && (uint)i < (uint)ItemDefs.Length && Array.Exists(Distrito.Importados, r => r.Item == ItemDefs[(int)i].Id);

        // ------------------------------------------------------------------ save (linhas opcionais; save sem elas = rotas paradas e patio vazio)

        void SaveRotas(StringBuilder sb)
        {
            if (Cais == null && Exportacao == null) return;
            var ci = CultureInfo.InvariantCulture;
            if (LoteProduto >= 0 || ImportT > 0f || ExportT > 0f || ImportedUnits > 0 || ExportLots > 0)
                sb.Append("gf=").Append(IdOuTraco(LoteProduto)).Append(',').Append(ImportT.ToString("0.###", ci)).Append(',').Append(ExportT.ToString("0.###", ci))
                  .Append(',').Append(ImportedUnits.ToString(ci)).Append(',').Append(ExportLots.ToString(ci)).Append('\n');
            if (LoteTotal == 0) return;
            sb.Append("gfl=").Append(IdCounts(Lote)).Append('\n');
            string q = IdQ(Lote, LoteQ);
            if (q.Length > 0) sb.Append("gflq=").Append(q).Append('\n');
        }

        /// <summary>Le gf=/gfl=/gflq= (lixo = padrao; produto que o distrito nao vende = automatico; relogios dentro do passo e do periodo; o lote
        /// so com produtos, ate o tamanho de agora).</summary>
        void LoadRotas(string text)
        {
            if (Cais == null && Exportacao == null) return;
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw.Substring(0, eq).Trim(), val = raw.Substring(eq + 1).Trim();
                string[] p = val.Split(',');
                switch (key)
                {
                    case "gf":
                        if (p.Length < 5) break;
                        LoteProduto = Produto(p[0]);
                        ImportT = Math.Max(0f, Math.Min(60f, F(p[1], 0f)));
                        ExportT = Math.Max(0f, Math.Min(T.ExportPeriodo, F(p[2], 0f)));
                        ImportedUnits = Math.Max(0, I(p[3], 0)); ExportLots = Math.Max(0, I(p[4], 0));
                        break;
                    case "gfl": ReadIdCounts(this, p, Lote); break;
                    case "gflq": ReadIdQ(this, p, LoteQ); break;
                }
            }
            int cabe = TamanhoLote;
            for (int i = 0; i < Lote.Length; i++)
            {
                Lote[i] = IsProduct((Item)i) ? Math.Min(Lote[i], cabe) : 0;
                cabe -= Lote[i];
                Fit(Lote[i], LoteQ[i]);
            }
        }

        // ------------------------------------------------------------------ validator (ContentValidator.Distrito)

        /// <summary>Rotas e patios do distrito: no maximo 1 cais e 1 patio de exportacao; rota = item da tabela do distrito, insumo aqui (nao
        /// produto) e usado por alguma receita, vindo de outro distrito do build que o vende; rotas sem cais e cais sem rotas acusam; o Mensageiro
        /// (Courier) so onde ha cais.</summary>
        public static IEnumerable<string> ValidarRotas(DistrictDef d, Sim w)
        {
            string at = $"distrito {d.Id}:";
            int cais = Array.FindAll(d.Estacoes, s => s.Kind == Kind.Import).Length, exp = Array.FindAll(d.Estacoes, s => s.Kind == Kind.Export).Length;
            if (cais > 1 || exp > 1) yield return $"{at} {cais} cais e {exp} patios de exportacao (no maximo 1 de cada)";
            bool rotas = d.Importados != null && d.Importados.Length > 0;
            if (rotas != (cais > 0)) yield return $"{at} {(rotas ? "rotas sem cais" : "cais sem rotas")} (o Mensageiro entrega pelo cais)";
            if (Array.Exists(d.Workers, k => k.Papel == WorkerRole.Courier) && cais == 0) yield return $"{at} Mensageiro (Courier) sem cais";
            foreach ((string item, string origem) in d.Importados ?? new (string, string)[0])
            {
                int i = Items.IndexOf(item, d.Itens);
                DistrictDef o = FS.Core.Distritos.Find(origem);
                if (i < 0 || d.Itens[i].Product) yield return $"{at} importado \"{item}\" fora da tabela do distrito ou vendido aqui (e' insumo)";
                else if (!Array.Exists(d.Receitas, r => Array.Exists(r.Inputs, x => x.Item == (Item)i))) yield return $"{at} importado \"{item}\" que nenhuma receita usa";
                if (o == null || o == d || !Array.Exists(o.Itens, x => x.Product && x.Id == item)) yield return $"{at} importado \"{item}\" de \"{origem}\", que nao e' outro distrito do build que o vende";
            }
        }
    }
}
