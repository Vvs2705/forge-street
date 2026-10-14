using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>O contrato ativo: etapa atual, pecas da etapa ja entregues e o prazo-bonus que resta (s de jogo). Campos publicos: o save e os
    /// testes leem e escrevem.</summary>
    public sealed class Contrato
    {
        public readonly ContractDef Def; public int Etapa, Feito; public float Prazo;
        public Contrato(ContractDef def, float prazo) { Def = def; Prazo = prazo; }
        public bool Pronto => Etapa >= Def.Etapas.Length;
        public ContractStage Atual => Def.Etapas[Math.Min(Etapa, Def.Etapas.Length - 1)];
        /// <summary>ID de transacao do premio (ledger das metas e Projetos): o mesmo ID nunca paga 2x.</summary>
        public string Tx => "ctr:" + Def.Id;
        /// <summary>Pecas entregues no total (as etapas fechadas + a atual), para a barra.</summary>
        public int Progresso { get { int n = Feito; for (int k = 0; k < Etapa && k < Def.Etapas.Length; k++) n += Def.Etapas[k].N; return n; } }
    }

    /// <summary>
    /// D-CORE-04 Royal Contracts (GDD §11, CONTEUDO_V1 §8, V25): objetivo de medio prazo em paralelo ao loop. Oferta sem sorteio: o 1o da lista
    /// (Defs) nao concluido e nao ativo e' o Proximo; e' Oferta quando abre (flag Tuning.Contratos, Ferramentas, distrito no build, Renome que a
    /// tabela do Renome pede e itens que este build vende). 1 ativo por vez (Vagas). Progresso por etapa pelas vendas reais (Sim.Sell -&gt;
    /// OnVenda, o caminho das encomendas: so o item da etapa atual, no nivel minimo ou melhor; cada peca conta 1). Concluir paga 1x pelo ID
    /// ctr:{id} no ledger das metas (Metas.Paga: ouro em s da taxa de agora FORA do GoldEarned, Renome) e o Blueprint pelo Projetos.Ganha
    /// (fonte Contract, mesmo ID); Ev.ContractDone para o diario, as metas (contract_complete) e as missoes de evento (Royal Week). Abandonar
    /// nao pune (nada pago, o progresso recomeca). Contrato nunca bloqueia nada (V25): ninguem le os concluidos alem do Charter (Concluidos.Count).
    /// Save: linhas ctr= (concluidos, na ordem; ID que este build nao conhece fica) e ctr.v= (o ativo), so quando existem: sem contrato o texto
    /// e' o de antes; save sem elas = nenhum. Nada aqui le relogio: o prazo anda com o dt do Sim (relogio para tras nao reabre nem paga).
    /// ponytail: a peca vendida no balcao conta para o contrato (e para a encomenda) em vez de ir a mesa do contrato; o seletor de cota
    /// 0/25/50/100% por linha do CONTEUDO_V1 §8 e' o upgrade quando a decisao "vender ou cumprir" precisar de peso.
    /// </summary>
    public sealed class Contratos
    {
        /// <summary>ponytail: teto 1; a mesa_real (CONTEUDO_V1 §8, distrito 4) da a 2a vaga com o C-CORE-05.</summary>
        public const int Vagas = 1;

        readonly Sim _sim;
        /// <summary>Catalogo desta partida (padrao ContratosDefs.All; so o teste injeta outro). Ev.ContractDone.A = indice aqui.</summary>
        public ContractDef[] Defs = ContratosDefs.All;
        public Contrato Ativo;
        /// <summary>IDs concluidos, na ordem (o contador do Charter, C-CORE-03: Concluidos.Count). ID de um build mais novo fica.</summary>
        public readonly List<string> Concluidos = new List<string>();
        /// <summary>Metricas de sessao (fora do save, como o OrderGold).</summary>
        public int Aceitos, Abandonados; public long OuroGanho;

        public Contratos(Sim sim) { _sim = sim; }

        public bool Concluido(string id) => Concluidos.Contains(id);

        /// <summary>O proximo da lista fixa (nao concluido, nao ativo), aberto ou nao; null = acabaram.</summary>
        public ContractDef Proximo => Array.Find(Defs, d => !Concluido(d.Id) && (Ativo == null || Ativo.Def != d));

        /// <summary>O contrato que da para aceitar agora: o Proximo, com a vaga livre e aberto.</summary>
        public ContractDef Oferta { get { ContractDef d = Proximo; return d != null && Ativo == null && Aberto(d) ? d : null; } }

        /// <summary>Flag ligada, depois das Ferramentas, distrito no build, Renome que a tabela pede e todo item das etapas vendido neste build.</summary>
        public bool Aberto(ContractDef d)
        {
            if (!_sim.T.Contratos || !_sim.QualityRevealed || Array.IndexOf(OrderDefs.Distritos, d.District) < 0) return false;
            if (d.Renome >= 0 && !_sim.Renome.Libera(d.Id)) return false;
            foreach (ContractStage e in d.Etapas) { int i = Items.IndexOf(e.Item, _sim.ItemDefs); if (i < 0 || !_sim.IsProduct((Item)i)) return false; }
            return true;
        }

        public bool Aceitar()
        {
            ContractDef d = Oferta;
            if (d == null) return false;
            Ativo = new Contrato(d, d.BonusMin * 60f); Aceitos++;
            return true;
        }

        /// <summary>Sem penalidade: nada foi pago; o contrato volta a lista e o progresso recomeca.</summary>
        public bool Abandonar()
        {
            if (Ativo == null) return false;
            Ativo = null; Abandonados++;
            return true;
        }

        /// <summary>Dev/foto (-contrato id): ativa `id` (null = o Proximo) sem requisito nem flag; recusa concluido, ja ativo ou desconhecido.</summary>
        public bool Forca(string id)
        {
            ContractDef d = id == null ? Proximo : Array.Find(Defs, x => x.Id == id);
            if (d == null || Concluido(d.Id) || Ativo != null) return false;
            Ativo = new Contrato(d, d.BonusMin * 60f); Aceitos++;
            return true;
        }

        /// <summary>Do Sim.Tick, depois das encomendas: o prazo-bonus anda no tempo de jogo (o offline nao conta); completo = paga.</summary>
        internal void Tick(float dt)
        {
            if (Ativo == null) return;
            if (Ativo.Prazo > 0f) Ativo.Prazo = Math.Max(0f, Ativo.Prazo - dt);
            if (Ativo.Pronto) Concluir();
        }

        /// <summary>Do Sim.Sell: a peca vendida conta para a etapa atual se e' o item dela no nivel minimo ou melhor.</summary>
        internal void OnVenda(Item item, Quality q)
        {
            if (Ativo == null || Ativo.Pronto) return;
            ContractStage e = Ativo.Atual;
            if (_sim.ItemDefs[(int)item].Id != e.Item || q < e.MinQ) return;
            if (++Ativo.Feito >= e.N) { Ativo.Etapa++; Ativo.Feito = 0; }
        }

        /// <summary>Paga o ativo completo 1x: ouro (no prazo x1,5) e Renome (+5 no prazo) pelo ledger das metas (ID = Tx), o Blueprint pelo
        /// Projetos (so se o BlueprintDef lista o contrato). Devolve o ouro; -1 = o ID ja estava no ledger (nada paga de novo).</summary>
        long Concluir()
        {
            Contrato c = Ativo; ContractDef d = c.Def;
            Ativo = null;
            if (!Concluido(d.Id)) Concluidos.Add(d.Id);
            bool prazo = c.Prazo > 0f;
            long g = _sim.Metas.Paga(c.Tx, prazo ? (int)Math.Round(d.OuroSeg * ContratosDefs.BonusOuro) : d.OuroSeg, 0, d.Reputacao + (prazo ? ContratosDefs.BonusRenome : 0));
            if (g < 0) return -1;
            OuroGanho += g;
            if (d.Blueprint != null) _sim.Projetos.Ganha(d.Blueprint, BlueprintSourceKind.Contract, c.Tx);
            _sim.Emit(Ev.ContractDone, Array.IndexOf(Defs, d), (int)Math.Min(int.MaxValue, g), _sim.Player.Pos);
            return g;
        }

        // ------------------------------------------------------------------ save (ctr= e ctr.v=, so quando existem; o Load de antes ignora)

        public void Save(StringBuilder sb)
        {
            if (Concluidos.Count > 0) sb.Append("ctr=").Append(string.Join(",", Concluidos)).Append('\n');
            if (Ativo != null)
                sb.Append("ctr.v=").Append(Ativo.Def.Id).Append(',').Append(Ativo.Etapa).Append(',').Append(Ativo.Feito).Append(',')
                  .Append(Ativo.Prazo.ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');
        }

        /// <summary>Le ctr= e ctr.v= (lixo = padrao). Concluido que este build nao conhece fica (e conta em UnknownIds); ativo desconhecido ou ja
        /// concluido fica de fora; etapa, feito e prazo presos ao que o ContractDef permite.</summary>
        internal void Load(string text)
        {
            string[] v = null;
            foreach (string raw in text.Split('\n'))
            {
                if (raw.StartsWith("ctr=", StringComparison.Ordinal))
                    foreach (string tok in raw.Substring(4).Trim().Split(','))
                    {
                        string id = tok.Trim();
                        if (id.Length == 0 || Concluido(id)) continue;
                        Concluidos.Add(id);
                        if (ContratosDefs.IndexOf(id, Defs) < 0) _sim.UnknownIds++;
                    }
                else if (raw.StartsWith("ctr.v=", StringComparison.Ordinal)) v = raw.Substring(6).Trim().Split(',');
            }
            if (v == null || v.Length < 4) return;
            ContractDef d = Array.Find(Defs, x => x.Id == v[0].Trim());
            if (d == null) { _sim.UnknownIds++; return; }
            if (Concluido(d.Id)) return;
            var c = new Contrato(d, Math.Max(0f, Math.Min(d.BonusMin * 60f, F(v[3], 0f))));
            c.Etapa = Clamp(I(v[1], 0), 0, d.Etapas.Length - 1);
            c.Feito = Clamp(I(v[2], 0), 0, c.Atual.N - 1);
            Ativo = c;
        }

        static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
        static int I(string s, int d) => int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        static float F(string s, float d) => float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : d;
    }
}
