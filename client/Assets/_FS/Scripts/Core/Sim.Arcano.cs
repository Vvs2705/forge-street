using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    public sealed partial class Station
    {
        /// <summary>C-CORE-08 (CONTEUDO_V1 §2.5): o medidor E do altar em centesimos (0-10.000 = 0-100; so o Forcado passa do teto), o modo do
        /// jogador (`Forcado`; false = Estavel, o padrao), `Energizada` = a peca em curso (parado: a proxima) sai energizada, `Sobrecarrega` = a
        /// peca em curso passou o teto e o altar pausa quando ela terminar, `Pausa` = s que faltam da sobrecarga, `Sequencia` = StableStreak
        /// (pecas energizadas seguidas sem sobrecarga) e `RestoDreno` = a fracao do dreno que ainda nao virou centesimo (fora do save).</summary>
        public int Medidor, Sequencia; public bool Forcado, Energizada, Sobrecarrega; public float Pausa, RestoDreno;
    }

    /// <summary>
    /// C-CORE-08 (CONTEUDO_V1 §2.5 regras 1-6): o medidor de carga dos altares da Arcane Foundry, deterministico (V7: sem sorteio, mesma entrada
    /// e mesmo resultado). Altar = estacao cujo produto tem carga no Tuning.CargaArcana (o Ajuste do distrito; null nos outros: tudo no-op e
    /// Old Forge, Row, Quarter e Arsenal ao byte). E cai `Dreno` por s, continuo (base 3/s; condensador +2; condensador2 +2; a oficial arcana,
    /// wk_isis, +1 no nivel 1 e +0,5 nos 3, 5, 7 e 9). Estavel (padrao): a peca so comeca com E + carga <= 100 (senao o altar espera,
    /// "estabilizando") e soma a carga; nunca sobrecarrega. Forcado: comeca assim que ha insumo, soma 2 x carga e sai energizada (q +20,
    /// QualitySource.Energizada); se E passou de 100 a peca termina e o altar entra em sobrecarga (pausa de 10 s, depois E = 40); nada se
    /// perde. O jogador escolhe o modo (ModoArcano: toque no altar); a troca vale na proxima peca. Contadores: PecasEnergizadas (EnergizedPieces,
    /// por produto) e Station.Sequencia (StableStreak por altar: energizadas seguidas sem sobrecarga; zera so na sobrecarga, a peca Estavel nao
    /// conta nem zera). O dreno e a pausa andam no tempo das estacoes (o boost acelera); o offline nunca mexe no medidor.
    /// Save: linhas opcionais arcN= (o altar fora do padrao) e arce= (os contadores); save sem elas = medidor vazio, Estavel.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Upgrades do dreno (CONTEUDO_V1 §5.5): cada um +2/s em todos os altares.</summary>
        public const string Condensador = "condensador", Condensador2 = "condensador2";
        /// <summary>Metrica do bot (fora do save, como o OrderGold): sobrecargas nesta sessao.</summary>
        public int Sobrecargas;

        int[] _energ;
        int _uCond = -2, _uCond2, _isis;

        /// <summary>EnergizedPieces do catalogo: pecas energizadas feitas, por handle de item (save arce=).</summary>
        public int[] PecasEnergizadas => _energ ??= new int[ItemDefs.Length];
        public int EnergizadasTotal { get { int n = 0; foreach (int e in PecasEnergizadas) n += e; return n; } }

        /// <summary>A estacao e' altar: faz produto com carga no Tuning.CargaArcana.</summary>
        public bool Arcana(Station s) => T.CargaArcana != null && s.Produces && (int)s.OutItem < T.CargaArcana.Length && T.CargaArcana[(int)s.OutItem] > 0;
        /// <summary>O distrito tem altar (a View, o bot e o econsim perguntam).</summary>
        public bool TemArcano => T.CargaArcana != null && Stations.Exists(Arcana);
        /// <summary>Carga da peca do altar em centesimos (0 fora de altar).</summary>
        public int CargaPeca(Station s) => Arcana(s) ? 100 * T.CargaArcana[(int)s.OutItem] : 0;

        void IdsArcano()
        {
            if (_uCond != -2) return;
            _uCond = Upgrades.IndexOf(Condensador, UpgradeDefs); _uCond2 = Upgrades.IndexOf(Condensador2, UpgradeDefs);
            _isis = Array.FindIndex(Equipe.Defs, d => d.Papel == WorkerRole.Smith && d.Especialidade == Specialty.Arcane);
        }

        /// <summary>Centesimos por s que E cai em todo altar: a base, os condensadores e a oficial arcana pelo nivel (+1; +0,5 nos niveis 3, 5,
        /// 7 e 9: o nivel 10 e' o maximo, +3).</summary>
        public int Dreno
        {
            get
            {
                IdsArcano();
                int d = T.DrenoBase;
                if (_uCond >= 0 && Bought[_uCond]) d += T.DrenoCondensador;
                if (_uCond2 >= 0 && Bought[_uCond2]) d += T.DrenoCondensador;
                int n = _isis < 0 ? 0 : Equipe.Nivel(_isis);
                if (n > 0) d += T.DrenoIsis + T.DrenoIsisPasso * ((n - 1) / 2);
                return d;
            }
        }

        /// <summary>Toque no altar (View) ou o bot: o modo da proxima peca (Forcado ou Estavel). A peca em curso termina como comecou. Fora de
        /// altar aberto ou no mesmo modo: false e nada muda.</summary>
        public bool ModoArcano(Station s, bool forcado)
        {
            if (s == null || !s.Unlocked || !Arcana(s) || s.Forcado == forcado) return false;
            s.Forcado = forcado;
            if (!s.Busy) s.Energizada = forcado;
            return true;
        }

        /// <summary>Do Sim.Produce, com a receita inteira na entrada e a saida com espaco: o altar segura a peca (sobrecarga em curso, ou
        /// Estavel sem espaco no medidor: "estabilizando"). Fora de altar: false.</summary>
        bool EsperaArcano(Station s) => Arcana(s) && (s.Pausa > 0f || !s.Forcado && s.Medidor + CargaPeca(s) > T.ArcanoTeto);

        /// <summary>Do Sim.Produce, quando a peca comeca: a carga entra no medidor (2x no Forcado, que energiza e marca a sobrecarga se E passou
        /// do teto).</summary>
        void ComecaArcano(Station s)
        {
            if (!Arcana(s)) return;
            int c = CargaPeca(s);
            s.Energizada = s.Forcado; s.Medidor += s.Forcado ? 2 * c : c;
            s.Sobrecarrega = s.Forcado && s.Medidor > T.ArcanoTeto;
        }

        /// <summary>Do Sim.Produce, depois do Grade da peca pronta: conta a energizada, a sequencia e a sobrecarga (pausa); a proxima peca leva o
        /// modo de agora.</summary>
        void TerminaArcano(Station s)
        {
            if (!Arcana(s)) return;
            if (s.Energizada)
            {
                PecasEnergizadas[(int)s.OutItem] += s.Recipe.Qtd;
                if (s.Sobrecarrega) { s.Sequencia = 0; s.Pausa = T.SobrecargaSeg; Sobrecargas++; }
                else s.Sequencia++;
            }
            s.Sobrecarrega = false;
            s.Energizada = s.Forcado;
        }

        /// <summary>QualitySource.Energizada: q +EnergizadaQ na peca energizada do altar (a em curso; parado, a proxima).</summary>
        int QualidadeEnergizada(Station s) => s.Energizada && Arcana(s) ? T.EnergizadaQ : 0;

        /// <summary>Do Sim.Tick (no tempo das estacoes): o dreno continuo de cada altar aberto e a pausa da sobrecarga (no fim, E = SobrecargaE).</summary>
        void TickArcano(float dt)
        {
            if (T.CargaArcana == null) return;
            int d = Dreno;
            foreach (Station s in Stations)
            {
                if (!s.Unlocked || !Arcana(s)) continue;
                if (s.Pausa > 0f)
                {
                    s.Pausa -= dt;
                    if (s.Pausa <= 0f) { s.Pausa = 0f; s.Medidor = T.SobrecargaE; s.RestoDreno = 0f; }
                    continue;
                }
                if (s.Medidor <= 0) continue;
                float x = d * dt + s.RestoDreno;
                int k = (int)x;
                s.RestoDreno = x - k;
                s.Medidor = Math.Max(0, s.Medidor - k);
                if (s.Medidor == 0) s.RestoDreno = 0f;
            }
        }

        /// <summary>Validator (ContentValidator.MasterworksDefs): q que o altar soma no maximo alem do QMaximo da familia: a energizada e a
        /// oficial arcana no nivel 5 (CONTEUDO_V1 §10.2: niveis 60 + tecnica 20 + energizada 20 + Isis 25 = 125).</summary>
        public int QMaxArcano(Station s)
        {
            if (!Arcana(s)) return 0;
            IdsArcano();
            return T.EnergizadaQ + (_isis >= 0 ? WorkerDefs.SmithQ5 : 0);
        }

        // ------------------------------------------------------------------ save (linhas opcionais; save sem elas = medidor vazio, Estavel)

        void SaveArcano(StringBuilder sb)
        {
            if (T.CargaArcana == null) return;
            var ci = CultureInfo.InvariantCulture;
            foreach (Station s in Stations)
            {
                if (!Arcana(s) || s.Medidor == 0 && !s.Forcado && !s.Energizada && !s.Sobrecarrega && s.Pausa <= 0f && s.Sequencia == 0) continue;
                sb.Append("arc").Append(s.Index).Append('=').Append(s.Medidor).Append(',').Append(s.Forcado ? 1 : 0).Append(',').Append(s.Energizada ? 1 : 0)
                  .Append(',').Append(s.Sobrecarrega ? 1 : 0).Append(',').Append(s.Pausa.ToString("0.###", ci)).Append(',').Append(s.Sequencia).Append('\n');
            }
            if (Array.Exists(PecasEnergizadas, n => n > 0)) sb.Append("arce=").Append(IdCounts(PecasEnergizadas)).Append('\n');
        }

        /// <summary>Le arcN= e arce= (lixo = padrao; medidor e sequencia nao negativos, medidor ate 3x o teto, pausa ate SobrecargaSeg; a peca
        /// so fica energizada ou em sobrecarga pendente se o altar esta trabalhando, senao segue o modo).</summary>
        void LoadArcano(string text)
        {
            if (T.CargaArcana == null) return;
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq < 4 || !raw.StartsWith("arc", StringComparison.Ordinal)) continue;
                string key = raw.Substring(0, eq), val = raw.Substring(eq + 1).Trim();
                if (key == "arce") { ReadIdCounts(this, val.Split(','), PecasEnergizadas); continue; }
                int idx = I(key.Substring(3), -1);
                if (idx < 0 || idx >= Stations.Count || !Arcana(Stations[idx])) continue;
                Station s = Stations[idx];
                string[] p = val.Split(',');
                if (p.Length < 6) continue;
                s.Medidor = Math.Max(0, Math.Min(3 * T.ArcanoTeto, I(p[0], 0)));
                s.Forcado = p[1].Trim() == "1";
                s.Energizada = s.Busy ? p[2].Trim() == "1" : s.Forcado;
                s.Sobrecarrega = s.Busy && s.Energizada && p[3].Trim() == "1";
                s.Pausa = Math.Max(0f, Math.Min(T.SobrecargaSeg, F(p[4], 0f)));
                s.Sequencia = Math.Max(0, I(p[5], 0));
            }
        }

        // ------------------------------------------------------------------ validator (ContentValidator.Distrito)

        /// <summary>Regras do medidor no mundo `w` do distrito `d`: carga do tamanho da tabela de itens e so em produto; carga de 1 a 100 (com
        /// mais, o Estavel nunca comecaria); todo altar e' bancada (Kind.Crafter: faz produto, golpe e dica de bancada); dreno base positivo
        /// (sem ele o Estavel parava para sempre) e a sobrecarga devolve E abaixo do teto.</summary>
        public static IEnumerable<string> ValidarArcano(DistrictDef d, Sim w)
        {
            int[] c = w.T.CargaArcana;
            if (c == null) yield break;
            string at = $"distrito {d.Id}: altar:";
            if (c.Length != w.ItemDefs.Length) yield return $"{at} carga com {c.Length} entradas (o distrito tem {w.ItemDefs.Length} itens)";
            for (int i = 0; i < Math.Min(c.Length, w.ItemDefs.Length); i++)
            {
                if (c[i] == 0) continue;
                string id = w.ItemDefs[i].Id;
                if (!w.IsProduct((Item)i)) yield return $"{at} carga em {id}, que nao e' produto";
                else if (c[i] < 0 || 100 * c[i] > w.T.ArcanoTeto) yield return $"{at} carga {c[i]} em {id} (de 1 a {w.T.ArcanoTeto / 100}: o Estavel nunca comecaria)";
            }
            foreach (Station s in w.Stations)
                if (w.Arcana(s) && s.Kind != Kind.Crafter) yield return $"{at} {s.Id} e' {s.Kind} (altar e' bancada: Kind.Crafter)";
            if (!w.TemArcano) yield return $"{at} carga sem altar no distrito";
            if (w.T.DrenoBase <= 0) yield return $"{at} dreno base {w.T.DrenoBase} (o Estavel parava para sempre)";
            if (w.T.SobrecargaE < 0 || w.T.SobrecargaE >= w.T.ArcanoTeto || w.T.SobrecargaSeg <= 0f) yield return $"{at} sobrecarga {w.T.SobrecargaSeg} s -> E {w.T.SobrecargaE}";
        }
    }
}
