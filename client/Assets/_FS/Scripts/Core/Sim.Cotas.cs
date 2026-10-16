using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>C-CORE-07: uma faixa do patio de cotas. `Produto` = o do bloco em curso (-1 = faixa vazia); `Proximo` = o do proximo bloco (a
    /// troca so vale nele). `Pecas` de `Tamanho`, `Soma` dos PriceOf(peca, nivel) na entrega, `PorQ` = quantas de cada nivel (a venda no
    /// fechamento conta cada uma) e `Decorrido` = s de jogo desde a 1a peca do bloco (o prazo-bonus).</summary>
    public sealed class FaixaCota
    {
        public int Produto = -1, Proximo = -1, Pecas, Tamanho;
        public long Soma;
        public float Decorrido;
        public readonly int[] PorQ = new int[Sim.Graus];
        public bool Vazia => Produto < 0 && Proximo < 0 && Pecas == 0;
    }

    /// <summary>
    /// C-CORE-07 (CONTEUDO_V1 §2.4 regras 1-6): o patio de cotas do Royal Arsenal, deterministico (V7). O patio (Kind.Quota) tem 1 faixa no kit e
    /// mais uma por `patio_faixa2`/`patio_faixa3`; cada faixa recebe um produto escolhido pelo jogador (EscolheCota) e abre um bloco dele: 10 pecas
    /// no 1o, +2 a cada bloco fechado do mesmo produto, ate 30 (Tuning.CotaBloco*). Carregador leva a peca ao patio quando ha faixa com aquele
    /// produto e vaga no bloco, senao ao balcao (Sim.Fits/IsDestFor/PickDest); o ferreiro entrega onde encostar. Bloco cheio fecha na hora: a
    /// Coroa paga Tuning.CotaPct% (130) da soma dos PriceOf das pecas, arredondado para CIMA (nunca menos que 1,3x), e e' VENDA: GoldEarned,
    /// RateEma, vendas, Renome das Superiores, projetos, Collection, encomenda, contrato e Ev.Sold por peca (decidido por padrao, revisar: sem a
    /// encomenda, a vaga da linha que vai ao patio nunca andava); +2 de Renome. Fechado ate CotaPrazo s (180; 240 com o almoxarife, wk_matias) da 1a peca: mais CotaBonusPct% (25) e +1 de Renome (prazo so
    /// de bonus: fora dele paga o normal). Contadores por produto: CotaBlocos (QuotaBlocks) e CotaNoPrazo (QuotaOnTime). Nada aqui le relogio: o
    /// prazo anda com o dt do Tick (offline e cofre nunca fecham bloco nem andam o prazo).
    /// Save: linhas opcionais cota= (as faixas), cotab=/cotap= (contadores por ID) e cotar= (Royal entregues, o Charter): so gravadas quando
    /// diferentes do padrao; save sem elas = patio vazio. Distrito sem patio: tudo no-op (Old Forge e Row ao byte).
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>As faixas na ordem (a 1a e' a do kit); FaixasAbertas = quantas valem agora.</summary>
        public readonly FaixaCota[] Faixas = { new FaixaCota(), new FaixaCota(), new FaixaCota() };
        /// <summary>Upgrades que abrem a 2a e a 3a faixa (CONTEUDO_V1 §5.4) e o major unlock do distrito 4 (2a vaga de contrato).</summary>
        public const string Faixa2 = "patio_faixa2", Faixa3 = "patio_faixa3", MesaReal = "mesa_real";
        /// <summary>C-CORE-07: encomendas Royal entregues NESTE distrito (o "ou 10 Royal" do Charter, CharterDef.Contratos). So conta onde o
        /// Charter pede contrato (distritos 4-6). Vai no save (cotar=).</summary>
        public int RoyalEntregues;
        /// <summary>O ultimo bloco pago fechou no prazo (o aviso do Ev.QuotaBlock na View). Fora do save.</summary>
        public bool CotaUltimaNoPrazo;

        Station _patio; bool _patioOk; int _uF2 = -2, _uF3, _uMesa, _uSelo;
        int[] _cotaBlocos, _cotaPrazo;

        /// <summary>A estacao Kind.Quota do distrito, ou null (Old Forge, Row).</summary>
        public Station Patio { get { if (!_patioOk) { _patio = Stations.Find(s => s.Kind == Kind.Quota); _patioOk = true; } return _patio; } }
        /// <summary>QuotaBlocks e QuotaOnTime do catalogo, por handle de item.</summary>
        public int[] CotaBlocos => _cotaBlocos ??= new int[ItemDefs.Length];
        public int[] CotaNoPrazo => _cotaPrazo ??= new int[ItemDefs.Length];
        /// <summary>Blocos fechados no total (marco do bp_var_estandarte_real).</summary>
        public int CotasFechadas { get { int n = 0; foreach (int b in CotaBlocos) n += b; return n; } }

        void IdsCota()
        {
            if (_uF2 != -2) return;
            _uF2 = Upgrades.IndexOf(Faixa2, UpgradeDefs); _uF3 = Upgrades.IndexOf(Faixa3, UpgradeDefs); _uMesa = Upgrades.IndexOf(MesaReal, UpgradeDefs);
            _uSelo = Distrito.Selo == null ? -1 : Upgrades.IndexOf(Distrito.Selo, UpgradeDefs);
        }

        /// <summary>O upgrade com este ID foi comprado neste distrito.</summary>
        public bool Comprado(string id) { int u = Upgrades.IndexOf(id, UpgradeDefs); return u >= 0 && Bought[u]; }

        /// <summary>Faixas que valem agora: 1 do kit + patio_faixa2 + patio_faixa3; 0 sem patio.</summary>
        public int FaixasAbertas
        {
            get
            {
                if (Patio == null || !Patio.Unlocked) return 0;
                IdsCota();
                return 1 + (_uF2 >= 0 && Bought[_uF2] ? 1 : 0) + (_uF3 >= 0 && Bought[_uF3] ? 1 : 0);
            }
        }

        /// <summary>s do prazo-bonus: CotaPrazo, ou CotaPrazoQm com o almoxarife do distrito contratado (wk_matias, CONTEUDO_V1 §6).</summary>
        public float CotaPrazo => Equipe.Contratados(WorkerRole.Quartermaster) > 0 ? T.CotaPrazoQm : T.CotaPrazo;

        /// <summary>A regra do bot (Bot.Cotas): clientes/s que pedem `i` no balcao. Sem familias, 1 / intervalo do produto; com familias
        /// (Sim.Clientes), o rodizio de cada familia aberta passa pelos produtos que ela pede agora, e cada um leva Unidades / (intervalo da
        /// familia x soma dos intervalos desses produtos). Linha fechada ou sem cliente: 0.</summary>
        public float Procura(Item i)
        {
            if (!LineUnlocked(i) || !HasClients(i)) return 0f;
            ClientDef[] fs = Distrito.Clientes;
            if (fs == null) return 1f / ClientInterval(i);
            float r = 0f;
            for (int f = 0; f < fs.Length; f++)
            {
                if (!FamiliaAberta(f) || Array.IndexOf(_famPede[f], (int)i) < 0 || !PedeAgora(f, (int)i)) continue;
                float soma = 0f;
                foreach (int j in _famPede[f]) if (PedeAgora(f, j)) soma += ClientInterval((Item)j);
                r += fs[f].Unidades / (fs[f].Intervalo * soma);
            }
            return r;
        }

        /// <summary>Tamanho do proximo bloco do produto `p`: CotaBloco0 + CotaBlocoPasso por bloco ja fechado dele, ate CotaBlocoMax.</summary>
        public int TamanhoCota(int p) => Math.Min(T.CotaBlocoMax, T.CotaBloco0 + T.CotaBlocoPasso * CotaBlocos[p]);

        /// <summary>Faixa aberta com o produto `p` no bloco em curso e vaga para mais `n` pecas alem das ja a caminho (-1 = nenhuma).</summary>
        int FaixaPara(Item p, int n = 0)
        {
            int f = FaixasAbertas;
            for (int k = 0; k < f; k++) if (Faixas[k].Produto == (int)p && Faixas[k].Tamanho - Faixas[k].Pecas > n) return k;
            return -1;
        }

        /// <summary>O patio recebe 1 `p` agora (regra 2: senao vai ao balcao).</summary>
        public bool CotaAceita(Item p) => Patio != null && FaixaPara(p) >= 0;

        /// <summary>Carregador com `n` na mao ainda cabe no bloco de `p` (o ajudante de produto busca o que tem destino).</summary>
        bool CabeNaCota(Item p, int n) => Patio != null && FaixaPara(p, n) >= 0;

        /// <summary>Para onde vai o produto `p` de quem carrega: o patio se aceita, senao o balcao dele.</summary>
        public Station Destino(Item p) => CotaAceita(p) ? Patio : CounterFor(p);

        /// <summary>
        /// O jogador escolhe o produto da faixa `k` (-1 = deixar vazia): produto de linha aberta do distrito, aberta neste build, em no maximo uma
        /// faixa. Bloco sem peca troca na hora; com peca, a troca vale para o proximo bloco (regra 4: as pecas ja entregues ficam no bloco em
        /// curso). Devolve false e nada muda fora disso.
        /// </summary>
        public bool EscolheCota(int k, int produto)
        {
            if (k < 0 || k >= FaixasAbertas) return false;
            if (produto >= 0 && (!IsProduct((Item)produto) || !LineUnlocked((Item)produto))) return false;
            for (int j = 0; j < Faixas.Length; j++)
                if (j != k && produto >= 0 && (Faixas[j].Produto == produto || Faixas[j].Proximo == produto)) return false;
            FaixaCota f = Faixas[k];
            f.Proximo = produto;
            if (f.Pecas == 0) Abre(f);
            return true;
        }

        /// <summary>O bloco seguinte da faixa: o produto escolhido, vazio, com o tamanho de agora (prazo zerado).</summary>
        void Abre(FaixaCota f)
        {
            f.Produto = f.Proximo; f.Pecas = 0; f.Soma = 0; f.Decorrido = 0f; Array.Clear(f.PorQ, 0, f.PorQ.Length);
            f.Tamanho = f.Produto >= 0 ? TamanhoCota(f.Produto) : 0;
        }

        /// <summary>Do Sim.Action: produto na mao de `c` que o patio aceita agora (o mais adiantado primeiro), 5 = entrega no patio; senao 0.</summary>
        int AcaoCota(Carrier c, out Item item)
        {
            item = Item.Ore;
            for (int k = ItemDefs.Length - 1; k >= (int)Item.Sword; k--)
                if (c.Held[k] > 0 && IsProduct((Item)k) && CotaAceita((Item)k)) { item = (Item)k; return 5; }
            return 0;
        }

        /// <summary>Do Sim.Interact: 1 peca de `it` da mao de `c` vai ao bloco da faixa dela (com o nivel e o preco de agora); bloco cheio fecha.</summary>
        void EntregaCota(Carrier c, Item it)
        {
            int k = FaixaPara(it);
            if (k < 0) return;
            FaixaCota f = Faixas[k];
            Quality l = Take(ref c.Held[(int)it], c.HeldQ[(int)it]);
            f.Pecas++; f.PorQ[(int)l]++; f.Soma += PriceOf(it, l);
            if (f.Pecas >= f.Tamanho) FechaCota(k);
        }

        /// <summary>A Coroa paga o bloco da faixa `k` (regra 3) e abre o proximo (regra 4). Ev.Sold por peca (B = a parte dela; a soma = o pago) e
        /// Ev.QuotaBlock (A = item, B = ouro).</summary>
        void FechaCota(int k)
        {
            FaixaCota f = Faixas[k];
            int p = f.Produto;
            bool prazo = f.Decorrido <= CotaPrazo;
            long pago = Teto(f.Soma * T.CotaPct, 100);
            if (prazo) pago = Teto(pago * (100 + T.CotaBonusPct), 100);
            string id = ItemDefs[p].Id;
            V2 pos = Patio.Pos;
            long resto = pago;
            int n = 0;
            for (int l = Graus - 1; l >= 0; l--)
                for (int i = 0; i < f.PorQ[l]; i++)
                {
                    Quality q = (Quality)l;
                    Renome.OnVendaQualidade(l); Projetos.OnVenda(p, q); Colecao.OnVenda(p, q); Encomendas.OnVenda((Item)p, q); Contratos.OnVenda(id, q);
                    long parte = ++n == f.Pecas ? resto : Math.Min(resto, pago * PriceOf((Item)p, q) / Math.Max(1, f.Soma));
                    resto -= parte;
                    Emit(Ev.Sold, p, (int)Math.Min(int.MaxValue, parte), pos);
                }
            QualityGold += f.Soma - (long)f.Pecas * PriceOf((Item)p, Quality.Normal);   // ponytail: o premio de qualidade sem o x1,3 da Coroa
            Gold += pago; GoldEarned += pago; Sales += f.Pecas; SoldItems[p] += f.Pecas;
            if (FirstSaleTime < 0f) FirstSaleTime = Time;
            CotaBlocos[p]++;
            if (prazo) CotaNoPrazo[p]++;
            Renome.Ganha(T.CotaRenome + (prazo ? T.CotaRenomePrazo : 0));
            CotaUltimaNoPrazo = prazo;
            Emit(Ev.QuotaBlock, p, (int)Math.Min(int.MaxValue, pago), pos);
            Abre(f);
        }

        static long Teto(long a, long b) => (a + b - 1) / b;

        /// <summary>Do Sim.Tick: o prazo de cada bloco com peca anda no tempo de jogo.</summary>
        void TickCotas(float dt)
        {
            if (Patio == null) return;
            foreach (FaixaCota f in Faixas) if (f.Pecas > 0) f.Decorrido += dt;
        }

        /// <summary>Do Sim.Recompute: a mesa_real (major unlock do distrito 4) da a 2a vaga de Royal Contract do Imperio (Contratos.MesaReal).</summary>
        void RecomputeCotas()
        {
            IdsCota();
            if (_uMesa >= 0 && Bought[_uMesa]) Contratos.MesaReal = true;
        }

        /// <summary>QualitySource.Buff do selo do distrito (DistrictDef.Selo comprado): q +SeloQ em toda estacao que faz produto.</summary>
        int QualidadeSelo(Station s)
        {
            if (Distrito.Selo == null) return 0;
            IdsCota();
            return _uSelo >= 0 && Bought[_uSelo] ? T.SeloQ : 0;
        }

        /// <summary>Estacoes abertas paradas por falta de `i` (a receita pede mais do que ha na entrada): o papel 1 leva primeiro o que mais falta
        /// (DistrictDef.InsumoPorEspera; o "so o que cabe" e' a regra do Sim.Transporte.cs).</summary>
        int Esperam(Item i)
        {
            int n = 0;
            foreach (Station d in Stations) { int k = d.InIndex(i); if (d.Unlocked && !d.Busy && k >= 0 && d.Ins[k] < d.Recipe.Inputs[k].Qty) n++; }
            return n;
        }

        /// <summary>Do Encomendas.Entregar: a Royal entregue conta para o Charter do distrito que pede contrato (CharterDef.Contratos).</summary>
        internal void OnEncomendaEntregue(Encomenda e)
        {
            if (e.Tipo == OrderType.Royal && Distrito.Charter != null && Distrito.Charter.Contratos > 0) RoyalEntregues++;
        }

        // ------------------------------------------------------------------ save (linhas opcionais; save sem elas = patio vazio)

        string IdOuTraco(int h) => h >= 0 && h < ItemDefs.Length ? ItemDefs[h].Id : "-";

        void SaveCotas(StringBuilder sb)
        {
            if (Patio == null) return;
            var ci = CultureInfo.InvariantCulture;
            if (!Array.TrueForAll(Faixas, f => f.Vazia))
            {
                var t = new List<string>();
                foreach (FaixaCota f in Faixas)
                    t.Add(IdOuTraco(f.Produto) + ":" + IdOuTraco(f.Proximo) + ":" + f.Pecas + ":" + f.Tamanho + ":" + f.Soma + ":" + f.Decorrido.ToString("0.###", ci)
                        + ":" + string.Join(":", Array.ConvertAll(f.PorQ, x => x.ToString(ci))));
                sb.Append("cota=").Append(string.Join(",", t)).Append('\n');
            }
            if (Array.Exists(CotaBlocos, x => x > 0)) sb.Append("cotab=").Append(IdCounts(CotaBlocos)).Append('\n');
            if (Array.Exists(CotaNoPrazo, x => x > 0)) sb.Append("cotap=").Append(IdCounts(CotaNoPrazo)).Append('\n');
            if (RoyalEntregues > 0) sb.Append("cotar=").Append(RoyalEntregues.ToString(ci)).Append('\n');
        }

        /// <summary>Le cota=/cotab=/cotap=/cotar= (lixo = padrao; produto que o distrito nao vende = faixa vazia; pecas abaixo do tamanho e
        /// tamanho na faixa do Tuning; prazo nao negativo).</summary>
        void LoadCotas(string text)
        {
            if (Patio == null) return;
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw.Substring(0, eq).Trim(), val = raw.Substring(eq + 1).Trim();
                switch (key)
                {
                    case "cotab": ReadIdCounts(this, val.Split(','), CotaBlocos); break;
                    case "cotap": ReadIdCounts(this, val.Split(','), CotaNoPrazo); break;
                    case "cotar": RoyalEntregues = Math.Max(0, I(val, 0)); break;
                    case "cota":
                        string[] fs = val.Split(',');
                        for (int k = 0; k < Faixas.Length && k < fs.Length; k++)
                        {
                            string[] p = fs[k].Split(':');
                            if (p.Length < 6 + Graus) continue;
                            FaixaCota f = Faixas[k];
                            f.Produto = Produto(p[0]); f.Proximo = Produto(p[1]);
                            f.Tamanho = f.Produto < 0 ? 0 : Math.Max(T.CotaBloco0, Math.Min(T.CotaBlocoMax, I(p[3], T.CotaBloco0)));
                            f.Pecas = f.Produto < 0 ? 0 : Clamp0(I(p[2], 0), f.Tamanho - 1);
                            f.Soma = f.Pecas == 0 ? 0 : Math.Max(0, L(p[4], 0));
                            f.Decorrido = f.Pecas == 0 ? 0f : Math.Max(0f, F(p[5], 0f));
                            int soma = 0;
                            for (int l = 0; l < Graus; l++) { f.PorQ[l] = Clamp0(I(p[6 + l], 0), f.Pecas - soma); soma += f.PorQ[l]; }
                            f.PorQ[0] += f.Pecas - soma;   // o que faltou e' Normal (a soma das pecas fecha)
                        }
                        break;
                }
            }
        }

        int Produto(string id) { int i = Items.IndexOf(id.Trim(), ItemDefs); return i >= 0 && IsProduct((Item)i) ? i : -1; }
    }
}
