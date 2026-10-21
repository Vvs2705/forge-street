using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// B-CORE-07: os Blueprints ("projetos", GDD §12, V9) da partida. POSSE, nunca saldo: um conjunto de IDs com a origem de cada um (e o ID de
    /// transacao quando vem de loja, Pass, encomenda ou contrato). Ganhar de novo e' no-op (o proprio ID e' a idempotencia; a transacao tambem
    /// nao paga 2x). Nao existe gasto nem troca: a posse so libera (Libera/Tem).
    /// Fontes ativas hoje (Checa, a cada Tick): marco de venda ou de producao completa, nivel de Renome e conquista resgatada (Metas, a:{id}).
    /// Ganchos: GanhaDaFila (encomendas Blueprint/Royal, B-CORE-05) e Ganha (contrato, evento, Pass, loja), so para fonte que o BlueprintDef lista.
    /// Save: linhas bp= (id:origem[:tx], na ordem do catalogo) e bpq= (vendas Fina ou melhor por item, para o marco com MinQ). Sem elas
    /// (0.1-0.8): nenhum projeto e o Checa da o retroativo dos marcos no 1o Tick (SoldItems e Renome ja vem do save).
    /// </summary>
    public sealed class Projetos
    {
        public struct Posse { public BlueprintSourceKind Origem; public string Tx; }

        readonly Sim _sim;
        /// <summary>O catalogo desta partida (padrao ProjetosDefs.All; so o teste injeta outro). Ev.Blueprint.A = indice aqui.</summary>
        public readonly BlueprintDef[] Defs;
        /// <summary>Possuidos por ID. Fica tambem o ID que o catalogo nao conhece (save de um build mais novo): posse nunca se perde no downgrade.</summary>
        public readonly Dictionary<string, Posse> Posses = new Dictionary<string, Posse>();
        /// <summary>Vendas Fina ou melhor, por item (o marco "100 joias Fina ou melhor"). Conta do B-CORE-07 em diante.</summary>
        public readonly int[] Finas;

        public Projetos(Sim sim, BlueprintDef[] defs)
        {
            _sim = sim;
            Defs = defs ?? ProjetosDefs.All;
            Finas = new int[sim.ItemDefs.Length];
        }

        public int Count => Posses.Count;
        public bool Tem(string id) => id != null && Posses.ContainsKey(id);

        /// <summary>A posse de algum Blueprint do tipo `kind` libera `alvo` (familia, item ou Masterwork). B-CORE-08 le os projetos de Masterwork aqui.</summary>
        public bool Libera(BlueprintKind kind, string alvo)
        {
            foreach (BlueprintDef d in Defs) if (d.Kind == kind && Array.IndexOf(d.Targets, alvo) >= 0 && Tem(d.Id)) return true;
            return false;
        }

        /// <summary>Pontos de qualidade da tecnica da familia (QualitySource.Blueprint): Tuning.TecnicaQ com o projeto, 0 sem.</summary>
        public int Tecnica(string familia) => familia != null && Libera(BlueprintKind.Technique, familia) ? _sim.T.TecnicaQ : 0;
        /// <summary>C-CORE-06: a tecnica de um produto (alvo = ID do item: bp_tecnica_lapidacao no colar e na caixa), Tuning.TecnicaItemQ; 0 sem.</summary>
        public int TecnicaItem(string item) => Libera(BlueprintKind.Technique, item) ? _sim.T.TecnicaItemQ : 0;

        /// <summary>Peca vendida (Sim.Sell): conta as Fina ou melhor.</summary>
        public void OnVenda(int item, Quality l)
        {
            if (l >= Quality.Fine && (uint)item < (uint)Finas.Length) Finas[item]++;
        }

        /// <summary>
        /// Gancho das fontes que o Sim nao ve (contrato, evento, Pass, loja; encomenda pelo GanhaDaFila): so se o BlueprintDef lista a fonte
        /// `origem`; paga (loja, Pass premium) so com `tx`. Idempotente pelo ID do Blueprint e pelo `tx`. Devolve true so quando a posse e' nova.
        /// </summary>
        public bool Ganha(string id, BlueprintSourceKind origem, string tx = null)
        {
            int i = ProjetosDefs.IndexOf(id, Defs);
            if (i < 0 || !Array.Exists(Defs[i].Sources, f => f.Kind == origem)) return false;
            if (ProjetosDefs.Paga(origem) && string.IsNullOrEmpty(tx)) return false;
            return Registra(i, origem, tx);
        }

        /// <summary>Proximo pendente da fila `fila` (OrderBlueprint ou OrderRoyal) do `distrito`: o de menor posicao ainda nao possuido, ou null
        /// (fila esgotada: a encomenda Blueprint vira Common e a Royal paga so ouro e Renome). O cartao da encomenda mostra ANTES (sem sorteio).</summary>
        public BlueprintDef ProximoDaFila(BlueprintSourceKind fila, string distrito = ProjetosDefs.OldForge)
        {
            BlueprintDef best = null; int n = int.MaxValue;
            foreach (BlueprintDef d in Defs)
            {
                if (Tem(d.Id)) continue;
                foreach (BlueprintSource f in d.Sources)
                    if (f.Kind == fila && f.Ref == distrito && f.N < n) { n = f.N; best = d; }
            }
            return best;
        }

        /// <summary>Encomenda Blueprint/Royal entregue (B-CORE-05): ganha o proximo da fila com o ID de transacao da encomenda. A mesma `tx` de
        /// novo (save/load, reabrir) nao da outro. Devolve o ganho, ou null (fila esgotada ou tx ja usada).</summary>
        public BlueprintDef GanhaDaFila(BlueprintSourceKind fila, string distrito, string tx)
        {
            if (fila != BlueprintSourceKind.OrderBlueprint && fila != BlueprintSourceKind.OrderRoyal || string.IsNullOrEmpty(tx)) return null;
            BlueprintDef d = ProximoDaFila(fila, distrito);
            return d != null && Registra(Array.IndexOf(Defs, d), fila, tx) ? d : null;
        }

        /// <summary>A cada Tick: os marcos (venda por item, Fina ou melhor, producao completa), o Renome e a conquista resgatada. A 1a fonte
        /// cumprida (na ordem do BlueprintDef) vira a origem. ponytail: um Sim = o Old Forge; o C-CORE-01 filtra pelo distrito do Sim.</summary>
        public void Checa()
        {
            for (int i = 0; i < Defs.Length; i++)
            {
                if (Posses.ContainsKey(Defs[i].Id)) continue;
                foreach (BlueprintSource f in Defs[i].Sources)
                    if (Cumpre(f)) { Registra(i, f.Kind, null); break; }
            }
        }

        bool Cumpre(BlueprintSource f)
        {
            switch (f.Kind)
            {
                case BlueprintSourceKind.Milestone:
                    if (f.Ref == BlueprintSource.Producao) return _sim.ProductionComplete;
                    if (f.Ref == BlueprintSource.Cotas) return _sim.CotasFechadas >= f.N;   // C-CORE-07
                    if (f.Ref == BlueprintSource.Energizadas) return _sim.EnergizadasTotal >= f.N;   // C-CORE-08
                    int it = Items.IndexOf(f.Ref, _sim.ItemDefs);
                    return it >= 0 && (f.MinQ >= Quality.Fine ? Finas[it] : _sim.SoldItems[it]) >= f.N;
                case BlueprintSourceKind.Reputation: return _sim.Renome.Nivel >= f.N;
                case BlueprintSourceKind.Achievement: return _sim.Metas.Ledger.Contains("a:" + f.Ref);   // a conquista ja resgatada (MetaDef.Tx)
                default: return false;   // filas, contrato, evento, Pass e loja: pelos ganchos
            }
        }

        bool Registra(int i, BlueprintSourceKind origem, string tx)
        {
            if (Posses.ContainsKey(Defs[i].Id)) return false;
            if (tx != null) foreach (Posse p in Posses.Values) if (p.Tx == tx) return false;
            Posses[Defs[i].Id] = new Posse { Origem = origem, Tx = tx };
            _sim.Emit(Ev.Blueprint, i, (int)origem, _sim.Player.Pos);
            return true;
        }

        // ------------------------------------------------------------------ save (o Sim.Load de antes ignora chave desconhecida)

        public void Save(StringBuilder sb)
        {
            var t = new List<string>();
            foreach (BlueprintDef d in Defs) if (Posses.TryGetValue(d.Id, out Posse p)) t.Add(Token(d.Id, p));
            foreach (KeyValuePair<string, Posse> kv in Posses) if (ProjetosDefs.IndexOf(kv.Key, Defs) < 0) t.Add(Token(kv.Key, kv.Value));
            sb.Append("bp=").Append(string.Join(",", t)).Append('\n');
            var q = new List<string>();
            for (int i = 0; i < Finas.Length; i++) if (Finas[i] > 0) q.Add(_sim.ItemDefs[i].Id + ":" + Finas[i].ToString(CultureInfo.InvariantCulture));
            sb.Append("bpq=").Append(string.Join(",", q)).Append('\n');
        }

        static string Token(string id, Posse p) => id + ":" + p.Origem + (string.IsNullOrEmpty(p.Tx) ? "" : ":" + p.Tx);

        /// <summary>Le bp= e bpq= (lixo = fora; origem desconhecida = Milestone; ID que o catalogo nao tem fica possuido e conta em UnknownIds).</summary>
        public void Load(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                if (raw.StartsWith("bp=", StringComparison.Ordinal))
                    foreach (string tok in raw.Substring(3).Trim().Split(','))
                    {
                        string[] f = tok.Split(new[] { ':' }, 3);
                        string id = f[0].Trim();
                        if (id.Length == 0 || Posses.ContainsKey(id)) continue;
                        Enum.TryParse(f.Length > 1 ? f[1] : "", out BlueprintSourceKind origem);
                        Posses[id] = new Posse { Origem = origem, Tx = f.Length > 2 && f[2].Length > 0 ? f[2] : null };
                        if (ProjetosDefs.IndexOf(id, Defs) < 0) _sim.UnknownIds++;
                    }
                else if (raw.StartsWith("bpq=", StringComparison.Ordinal))
                    foreach (string tok in raw.Substring(4).Trim().Split(','))
                    {
                        string[] kv = tok.Split(':');
                        int i = Items.IndexOf(kv[0].Trim(), _sim.ItemDefs);
                        if (i >= 0 && kv.Length > 1 && int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) Finas[i] = Math.Max(0, n);
                    }
            }
        }
    }
}
