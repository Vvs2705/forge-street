using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    public sealed partial class Station
    {
        /// <summary>C-CORE-06 (CONTEUDO_V1 §2.3 regra 1): bancada configuravel = mais de uma receita do distrito com a Station dela. `Receitas` =
        /// todas, na ordem da tabela (a 1a e' a de fabrica; null = estacao fixa, a de sempre); `AbreReceita` = indice do upgrade que libera cada
        /// uma (RecipeDef.Abre; -1 = do kit). A receita ativa e' o Recipe de sempre (insumos, produto, tempo, pilhas); `Proxima` = a que o
        /// jogador pediu (troca pendente) e `Parada` = s que faltam da troca. `Guardados` = insumos fora da receita ativa, por handle de item:
        /// ficam na entrada (ate o InCap de cada) e voltam quando uma receita os usa; nada se perde.</summary>
        public RecipeDef[] Receitas; public int[] AbreReceita, Guardados;
        public RecipeDef Proxima; public float Parada;
    }

    /// <summary>
    /// C-CORE-06: a bancada configuravel do Artisan Quarter (CONTEUDO_V1 §2.3 regras 1-2). Faz qualquer receita do distrito ja liberada; o
    /// jogador escolhe a ativa (toque na bancada: Configura). Trocar: a bancada termina a peca em curso, espera a saida esvaziar (a pilha e' de
    /// um produto so) e fica parada TempoTroca s (4 s; troca_rapida 1 s; o oficial da familia -1 s; minimo 1 s). Os insumos que a nova receita
    /// nao usa ficam guardados na entrada e servem na proxima troca. Carregadores e o ferreiro so entregam insumo da receita ativa (o Accepts de
    /// sempre). Sem sorteio: mesma sequencia de toques, mesmo resultado. Save: linha opcional ofcN= (so com a bancada fora do padrao).
    /// </summary>
    public sealed partial class Sim
    {
        public const float TrocaSeg = 4f, TrocaRapidaSeg = 1f, TrocaMinSeg = 1f, TrocaOficialSeg = 1f;
        public const string TrocaRapidaId = "troca_rapida";
        /// <summary>O distrito tem bancada configuravel (Old Forge e Row: nao; todo gancho volta na hora).</summary>
        public bool TemBancadas { get; private set; }
        /// <summary>Metrica do bot (fora do save, como o OrderGold): trocas de receita feitas nesta sessao.</summary>
        public int TrocasReceita;
        int _trocaRapida = -1;

        /// <summary>Do construtor, depois das estacoes: marca as configuraveis (entrada de 2x o maior insumo de todas as receitas dela: 6, o
        /// "ate 6 por tipo" do catalogo).</summary>
        void PreparaBancadas()
        {
            foreach (Station s in Stations)
            {
                if (!s.Produces) continue;
                RecipeDef[] rs = Array.FindAll(RecipeDefs, r => r.Station == s.Recipe.Station);
                if (rs.Length < 2) continue;
                s.Receitas = rs;
                s.AbreReceita = Array.ConvertAll(rs, r => r.Abre == null ? -1 : Upgrades.IndexOf(r.Abre, UpgradeDefs));
                s.Guardados = new int[ItemDefs.Length];
                int maxQ = 0;
                foreach (RecipeDef r in rs) foreach ((Item _, int q) in r.Inputs) maxQ = Math.Max(maxQ, q);
                s.InCap = Math.Max(Balance.CrafterIn, 2 * maxQ);
                TemBancadas = true;
            }
            _trocaRapida = Upgrades.IndexOf(TrocaRapidaId, UpgradeDefs);
        }

        /// <summary>A receita `k` da bancada `s` esta liberada: do kit, ou o upgrade dela comprado (ID que o distrito nao tem = nunca).</summary>
        public bool Liberada(Station s, int k) => s.Receitas[k].Abre == null || s.AbreReceita[k] >= 0 && Bought[s.AbreReceita[k]];

        /// <summary>A estacao faz `i` agora ou, se e' configuravel, tem a receita liberada: linha aberta, cliente, encomenda e Hall contam com
        /// ela. Estacao fixa: s.Produces e OutItem == i, como sempre.</summary>
        public bool Faz(Station s, Item i)
        {
            if (!s.Produces) return false;
            if (s.OutItem == i) return true;
            if (s.Receitas == null) return false;
            for (int k = 0; k < s.Receitas.Length; k++) if (s.Receitas[k].Output == i && Liberada(s, k)) return true;
            return false;
        }

        /// <summary>A estacao faz algum produto da familia de produto `familia` (Hall, validator).</summary>
        public bool FazFamilia(Station s, string familia)
        {
            for (int i = 0; i < ItemDefs.Length; i++) if (ItemDefs[i].Product && ItemDefs[i].Family == familia && Faz(s, (Item)i)) return true;
            return false;
        }

        /// <summary>As receitas que a estacao sabe fazer (configuravel: todas; fixa: a dela; sem receita: nenhuma). So o validator chama.</summary>
        public RecipeDef[] ReceitasDe(Station s) => s.Receitas ?? (s.Produces ? new[] { s.Recipe } : new RecipeDef[0]);

        /// <summary>s que a bancada fica parada na troca: 4 s, 1 s com a troca rapida, -1 s com o oficial da familia (Lucia), minimo 1 s.</summary>
        public float TempoTroca(Station s) =>
            Math.Max(TrocaMinSeg, (_trocaRapida >= 0 && Bought[_trocaRapida] ? TrocaRapidaSeg : TrocaSeg) - (Equipe.TemOficial(s) ? TrocaOficialSeg : 0f));

        /// <summary>
        /// Toque na bancada (View) ou o bot: pede a receita que faz `produto`. A ativa de novo cancela a troca pendente. Fora de bancada
        /// configuravel aberta, receita que ela nao tem, ainda nao liberada ou ja pedida: false e nada muda.
        /// </summary>
        public bool Configura(Station s, Item produto)
        {
            if (s == null || s.Receitas == null || !s.Unlocked) return false;
            int k = Array.FindIndex(s.Receitas, r => r.Output == produto);
            if (k < 0 || !Liberada(s, k)) return false;
            RecipeDef r = s.Receitas[k];
            if (r == s.Recipe) { bool tinha = s.Proxima != null; s.Proxima = null; return tinha; }
            if (s.Proxima == r) return false;
            s.Proxima = r;
            return true;
        }

        /// <summary>Do Sim.Produce, com a estacao sem peca: aplica a troca pendente (com a saida vazia) e segura a bancada nos s da troca.
        /// true = nao comeca peca neste tick.</summary>
        bool Trocando(Station s, float dt)
        {
            if (s.Receitas == null) return false;
            if (s.Proxima != null)
            {
                if (s.Out > 0) return true;   // a pilha e' de um produto so: espera recolherem a ultima peca da receita antiga
                Ativa(s, s.Proxima);
                s.Parada = TempoTroca(s);
                TrocasReceita++;
                Recompute();   // tempo da receita nova
            }
            if (s.Parada <= 0f) return false;
            s.Parada = Math.Max(0f, s.Parada - dt);
            return true;
        }

        /// <summary>Poe `r` como receita ativa: o que estava na entrada vai aos guardados e a entrada nova sai deles (todos <= InCap: nada
        /// sobra nem se perde).</summary>
        static void Ativa(Station s, RecipeDef r)
        {
            for (int k = 0; k < s.Ins.Length; k++) s.Guardados[(int)s.Recipe.Inputs[k].Item] += s.Ins[k];
            s.Recipe = r; s.OutItem = r.Output; s.Proxima = null;
            s.Ins = new int[r.Inputs.Length];
            for (int k = 0; k < r.Inputs.Length; k++)
            {
                int it = (int)r.Inputs[k].Item;
                s.Ins[k] = Math.Min(s.InCap, s.Guardados[it]);
                s.Guardados[it] -= s.Ins[k];
            }
            s.Liga = Array.FindIndex(r.Inputs, x => x.Qty == 0);
        }

        // ------------------------------------------------------------------ save (linha ofcN=receita,proxima,parada,guardados; so fora do padrao)

        void SalvaBancadas(StringBuilder sb)
        {
            if (!TemBancadas) return;
            foreach (Station s in Stations)
            {
                if (s.Receitas == null) continue;
                if (s.Recipe == s.Receitas[0] && s.Proxima == null && s.Parada <= 0f && Array.TrueForAll(s.Guardados, n => n == 0)) continue;
                sb.Append("ofc").Append(s.Index).Append('=').Append(ItemDefs[(int)s.Recipe.Output].Id).Append(',')
                  .Append(s.Proxima != null ? ItemDefs[(int)s.Proxima.Output].Id : "-").Append(',')
                  .Append(s.Parada.ToString("0.###", CultureInfo.InvariantCulture)).Append(',').Append(IdCounts(s.Guardados)).Append('\n');
            }
        }

        /// <summary>Antes das pilhas (stN= le a entrada da receita ativa): receita e troca de cada bancada. Lixo = padrao; receita que a
        /// bancada nao tem fica de fora (UnknownIds); parada ate 4 s; guardado ate o InCap e nunca de insumo da receita ativa.</summary>
        void CarregaBancadas(string text)
        {
            if (!TemBancadas) return;
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (!raw.StartsWith("ofc", StringComparison.Ordinal) || eq < 4) continue;
                int idx = I(raw.Substring(3, eq - 3), -1);
                if (idx < 0 || idx >= Stations.Count || Stations[idx].Receitas == null) continue;
                Station s = Stations[idx];
                string[] p = raw.Substring(eq + 1).Trim().Split(',');
                RecipeDef r = Receita(s, p[0]);
                if (r == null) { UnknownIds++; continue; }
                Ativa(s, r);
                s.Proxima = p.Length > 1 ? Receita(s, p[1]) : null;
                if (s.Proxima == s.Recipe) s.Proxima = null;
                s.Parada = p.Length > 2 ? Math.Max(0f, Math.Min(TrocaSeg, F(p[2], 0f))) : 0f;
                var g = new string[Math.Max(0, p.Length - 3)];
                Array.Copy(p, 3, g, 0, g.Length);
                Array.Clear(s.Guardados, 0, s.Guardados.Length);
                ReadIdCounts(this, g, s.Guardados);
                for (int i = 0; i < s.Guardados.Length; i++) s.Guardados[i] = s.InIndex((Item)i) >= 0 ? 0 : Math.Min(s.Guardados[i], s.InCap);
            }
        }

        /// <summary>Receita da bancada `s` que faz o item de ID `id`, ou null.</summary>
        RecipeDef Receita(Station s, string id)
        {
            int i = Items.IndexOf(id.Trim(), ItemDefs);
            return i < 0 ? null : Array.Find(s.Receitas, r => r.Output == (Item)i);
        }

        // ------------------------------------------------------------------ validator (ContentValidator.Distrito)

        /// <summary>Regras da bancada configuravel no mundo `w` do distrito `d`: a receita de fabrica (a 1a) e' do kit; toda receita liberada por
        /// upgrade aponta para um produtivo do distrito; as receitas de uma bancada sao do tipo dela e fazem produto.</summary>
        public static IEnumerable<string> ValidarBancadas(DistrictDef d, Sim w)
        {
            foreach (Station s in w.Stations)
            {
                if (s.Receitas == null) continue;
                string at = $"distrito {d.Id}: bancada {s.Id}:";
                if (s.Receitas[0].Abre != null) yield return $"{at} a receita de fabrica ({w.ItemDefs[(int)s.Receitas[0].Output].Id}) tem de ser do kit (Abre null)";
                for (int k = 0; k < s.Receitas.Length; k++)
                {
                    RecipeDef r = s.Receitas[k];
                    string id = w.ItemDefs[(int)r.Output].Id;
                    if (r.Abre != null && (s.AbreReceita[k] < 0 || !w.IsProduction(s.AbreReceita[k]))) yield return $"{at} receita {id} liberada por \"{r.Abre}\", que nao e' produtivo do distrito";
                    if (r.Kind != s.Kind || !w.IsProduct(r.Output)) yield return $"{at} receita {id} fora do tipo da bancada ou que nao faz produto";
                }
            }
            foreach (RecipeDef r in w.RecipeDefs)
                if (r.Abre != null && !w.Stations.Exists(s => s.Receitas != null && Array.IndexOf(s.Receitas, r) >= 0))
                    yield return $"distrito {d.Id}: receita {w.ItemDefs[(int)r.Output].Id} com Abre fora de bancada configuravel";
        }
    }
}
