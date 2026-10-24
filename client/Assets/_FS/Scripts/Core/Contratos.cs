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
    /// D-CORE-04 Royal Contracts (GDD §11, CONTEUDO_V1 §8, V25): objetivo de medio prazo em paralelo ao loop, do Imperio: moram no Sim da raiz
    /// (o Old Forge, como Renome e metas nesta leva) e os outros distritos chegam a eles pelo Sim.Heroi (Sim.Contratos). Oferta sem sorteio: o 1o da lista
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
        /// <summary>1 vaga; a mesa_real (CONTEUDO_V1 §8, major unlock do distrito 4) da a 2a (MesaReal, Ativo2: C-CORE-07).</summary>
        public const int Vagas = 1;
        /// <summary>C-CORE-07: a mesa_real foi comprada no Royal Arsenal (o Sim dele marca no Recompute, a cada load): 2 contratos ao mesmo tempo.
        /// Nao vai no save (vem do upgrade).</summary>
        public bool MesaReal;
        public int VagasMax => Vagas + (MesaReal ? 1 : 0);

        readonly Sim _sim;
        /// <summary>Catalogo desta partida (padrao ContratosDefs.All; so o teste injeta outro). Ev.ContractDone.A = indice aqui.</summary>
        public ContractDef[] Defs = ContratosDefs.All;
        /// <summary>O ativo e o da 2a vaga (C-CORE-07, so com a mesa_real; null = vaga livre). Quando o 1o sai, o 2o passa a ser o 1o.</summary>
        public Contrato Ativo, Ativo2;
        /// <summary>IDs concluidos, na ordem (o contador do Charter, C-CORE-03: Concluidos.Count). ID de um build mais novo fica.</summary>
        public readonly List<string> Concluidos = new List<string>();
        /// <summary>Metricas de sessao (fora do save, como o OrderGold).</summary>
        public int Aceitos, Abandonados; public long OuroGanho;

        public Contratos(Sim sim) { _sim = sim; }

        public bool Concluido(string id) => Concluidos.Contains(id);

        /// <summary>O proximo da lista fixa (nao concluido, nao ativo), aberto ou nao; null = acabaram.</summary>
        public ContractDef Proximo => Array.Find(Defs, d => !Concluido(d.Id) && (Ativo == null || Ativo.Def != d) && (Ativo2 == null || Ativo2.Def != d));

        /// <summary>O contrato que da para aceitar agora: o Proximo, com uma vaga livre e aberto.</summary>
        public ContractDef Oferta { get { ContractDef d = Proximo; return Livre(d) ? d : null; } }

        /// <summary>Bot/econsim (BALANCE §41 achado 15): o 1o da lista que daria para aceitar agora e que `serve` aceita, fora da ordem fixa
        /// (o bot de um distrito so pega o que ele vende). O jogador continua vendo so a Oferta.</summary>
        public ContractDef OfertaQue(Predicate<ContractDef> serve) => Array.Find(Defs, d => serve(d) && Livre(d));

        /// <summary>Da para aceitar `d` agora: vaga livre, nao concluido, nao ativo e aberto.</summary>
        bool Livre(ContractDef d) => d != null && (Ativo == null || MesaReal && Ativo2 == null) && !Concluido(d.Id) && Ativo?.Def != d && Ativo2?.Def != d && Aberto(d);

        /// <summary>Flag ligada, depois das Ferramentas, distrito no build, Renome que a tabela pede e todo item das etapas vendido neste build.</summary>
        public bool Aberto(ContractDef d)
        {
            if (!_sim.T.Contratos || !_sim.QualityRevealed || Array.IndexOf(OrderDefs.Distritos, d.District) < 0) return false;
            if (d.Renome >= 0 && !_sim.Renome.Libera(d.Id)) return false;
            foreach (ContractStage e in d.Etapas) if (!ContratosDefs.NoBuild(e.Item, _sim)) return false;   // C-CORE-05: o elmo e' da Row
            return true;
        }

        public bool Aceitar() => Aceitar(Oferta);

        /// <summary>Aceita `d` se da (Livre); o bot passa o da OfertaQue.</summary>
        public bool Aceitar(ContractDef d)
        {
            if (!Livre(d)) return false;
            if (Ativo == null) Ativo = new Contrato(d, d.BonusMin * 60f); else Ativo2 = new Contrato(d, d.BonusMin * 60f);   // C-CORE-07: a 2a vaga
            Aceitos++;
            return true;
        }

        /// <summary>Sem penalidade: nada foi pago; o contrato volta a lista e o progresso recomeca. `segundo` = o da 2a vaga (C-CORE-07).</summary>
        public bool Abandonar(bool segundo = false)
        {
            if ((segundo ? Ativo2 : Ativo) == null) return false;
            if (segundo) Ativo2 = null; else { Ativo = Ativo2; Ativo2 = null; }
            Abandonados++;
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

        /// <summary>Do Sim.Tick do distrito `ativo` (so ele anda), depois das encomendas: o prazo-bonus anda no tempo de jogo (o offline nao
        /// conta); completo = paga.</summary>
        internal void Tick(float dt, Sim ativo)
        {
            if (Ativo2 != null && Ativo2.Prazo > 0f) Ativo2.Prazo = Math.Max(0f, Ativo2.Prazo - dt);   // C-CORE-07: a 2a vaga
            if (Ativo2 != null && Ativo2.Pronto) Concluir(ativo, true);
            if (Ativo == null) return;
            if (Ativo.Prazo > 0f) Ativo.Prazo = Math.Max(0f, Ativo.Prazo - dt);
            if (Ativo.Pronto) Concluir(ativo);
        }

        /// <summary>Do Sim.Sell de qualquer distrito: a peca `item` (ID do ItemDef: o handle muda de distrito para distrito) conta para a etapa
        /// atual se e' o item dela no nivel minimo ou melhor. C-CORE-07: so para um (o 1o que aceita; 1 peca conta 1).</summary>
        internal void OnVenda(string item, Quality q)
        {
            if (!Conta(Ativo, item, q)) Conta(Ativo2, item, q);
        }

        static bool Conta(Contrato c, string item, Quality q)
        {
            if (c == null || c.Pronto) return false;
            ContractStage e = c.Atual;
            if (item != e.Item || q < e.MinQ) return false;
            if (++c.Feito >= e.N) { c.Etapa++; c.Feito = 0; }
            return true;
        }

        /// <summary>Paga o ativo completo 1x: ouro (no prazo x1,5) e Renome (+5 no prazo) pelo ledger das metas (ID = Tx), o Blueprint pelo
        /// Projetos (so se o BlueprintDef lista o contrato). Ev.ContractDone na raiz (metas, eventos) e no distrito `ativo` (a View le os eventos
        /// dele: aviso e diario). Devolve o ouro; -1 = o ID ja estava no ledger (nada paga de novo).</summary>
        long Concluir(Sim ativo, bool segundo = false)
        {
            Contrato c = segundo ? Ativo2 : Ativo; ContractDef d = c.Def;
            if (segundo) Ativo2 = null; else { Ativo = Ativo2; Ativo2 = null; }   // C-CORE-07: o 2o sobe para a 1a vaga
            if (!Concluido(d.Id)) Concluidos.Add(d.Id);
            bool prazo = c.Prazo > 0f;
            long g = _sim.Metas.Paga(c.Tx, prazo ? (int)Math.Round(d.OuroSeg * ContratosDefs.BonusOuro) : d.OuroSeg, 0, d.Reputacao + (prazo ? ContratosDefs.BonusRenome : 0));
            if (g < 0) return -1;
            OuroGanho += g;
            if (d.Blueprint != null) _sim.Projetos.Ganha(d.Blueprint, BlueprintSourceKind.Contract, c.Tx);
            int i = Array.IndexOf(Defs, d), b = (int)Math.Min(int.MaxValue, g);
            _sim.Emit(Ev.ContractDone, i, b, _sim.Player.Pos);
            if (ativo != _sim) ativo.Events.Add(new SimEvent(Ev.ContractDone, i, b, ativo.Player.Pos));   // o ouro vai a carteira pelo Imperio.Fundo
            return g;
        }

        // ------------------------------------------------------------------ save (ctr= e ctr.v=, so quando existem; o Load de antes ignora)

        public void Save(StringBuilder sb)
        {
            if (Concluidos.Count > 0) sb.Append("ctr=").Append(string.Join(",", Concluidos)).Append('\n');
            if (Ativo != null) Linha(sb, "ctr.v=", Ativo);
            if (Ativo2 != null) Linha(sb, "ctr.v2=", Ativo2);   // C-CORE-07: a 2a vaga (so com a mesa_real; o Load de antes ignora)
        }

        static void Linha(StringBuilder sb, string chave, Contrato c) =>
            sb.Append(chave).Append(c.Def.Id).Append(',').Append(c.Etapa).Append(',').Append(c.Feito).Append(',')
              .Append(c.Prazo.ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');

        /// <summary>Le ctr= e ctr.v= (lixo = padrao). Concluido que este build nao conhece fica (e conta em UnknownIds); ativo desconhecido ou ja
        /// concluido fica de fora; etapa, feito e prazo presos ao que o ContractDef permite.</summary>
        internal void Load(string text)
        {
            string[] v = null, v2 = null;
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
                else if (raw.StartsWith("ctr.v2=", StringComparison.Ordinal)) v2 = raw.Substring(7).Trim().Split(',');   // C-CORE-07
            }
            Ativo = Le(v);
            Contrato b = Le(v2);
            if (b != null && b.Def != Ativo?.Def) { if (Ativo == null) Ativo = b; else Ativo2 = b; }   // so a 2a sem a 1a: sobe
        }

        Contrato Le(string[] v)
        {
            if (v == null || v.Length < 4) return null;
            ContractDef d = Array.Find(Defs, x => x.Id == v[0].Trim());
            if (d == null) { _sim.UnknownIds++; return null; }
            if (Concluido(d.Id)) return null;
            var c = new Contrato(d, Math.Max(0f, Math.Min(d.BonusMin * 60f, F(v[3], 0f))));
            c.Etapa = Clamp(I(v[1], 0), 0, d.Etapas.Length - 1);
            c.Feito = Clamp(I(v[2], 0), 0, c.Atual.N - 1);
            return c;
        }

        static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
        static int I(string s, int d) => int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        static float F(string s, float d) => float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : d;
    }
}
