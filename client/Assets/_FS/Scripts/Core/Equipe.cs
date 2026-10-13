using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>B-CORE-04 (GDD §8, DM §22): os 8 papeis. Porter = os ajudantes que carregam (Sim.Workers, Carrier); os outros ficam no posto e
    /// mudam um numero do Sim (Equipe). Smelter, Courier e Manager sao dos distritos 2-6 (Wave C): o formato aceita, o nucleo ainda nao tem a
    /// regra deles (o ContentValidator.Workers recusa no build).</summary>
    public enum WorkerRole { Porter, Smith, Clerk, Quartermaster, Foreman, Smelter, Courier, Manager }

    /// <summary>Especialidade (ECONOMIA_WAVE_B §8.3; Arcane do CONTEUDO_V1 §6).</summary>
    public enum Specialty { None, Swords, Shields, Tools, Jewels, Armor, Carrying, Service, Arcane }

    /// <summary>
    /// B-CORE-04 (ECONOMIA_WAVE_B §2 e §8.3, CONTEUDO_V1 §6): worker fixo e nomeado, em dados; nenhum traco sorteado, o jogador escolhe quem
    /// contrata. Porter: o upgrade `Contrata` contrata (o pad de sempre) e o Carrier anda na rota `Rota` (0 minerio, 1 lingote, 2 produto, 3
    /// joalheiro). Os outros: a Equipe contrata por `Contratacao` de ouro com o upgrade `Requer` comprado e o Renome liberando o ID (RenomeDefs).
    /// `Niveis` = custo de cada nivel, do 2 ao maximo: categoria Meta (V14), fora da producao completa e do teto offline. `Station` = a familia
    /// do oficial (a da RecipeDef.Station). O efeito e' a regra do papel (Equipe) com os numeros do WorkerDefs; `Tracos` (1-2, DM §23) sao
    /// os IDs que o cartao mostra. ID com prefixo wk_ (V22): o save grava (wkn=), nunca renomear.
    /// </summary>
    public sealed class WorkerDef
    {
        public readonly string Id, Nome, Distrito, Station, Contrata, Requer, Skin;
        public readonly WorkerRole Papel;
        public readonly Specialty Especialidade;
        public readonly int Rota;
        public readonly long Contratacao;
        public readonly long[] Niveis;
        public readonly string[] Tracos;
        public int MaxNivel => Niveis.Length + 1;

        public WorkerDef(string id, string nome, WorkerRole papel, Specialty especialidade, string distrito, string station, string contrata, int rota,
            long contratacao, string requer, long[] niveis, string skin, params string[] tracos)
        {
            Id = id; Nome = nome; Papel = papel; Especialidade = especialidade; Distrito = distrito; Station = station; Contrata = contrata; Rota = rota;
            Contratacao = contratacao; Requer = requer; Niveis = niveis; Skin = skin; Tracos = tracos;
        }
    }

    /// <summary>
    /// B-CORE-04: os 13 workers do Old Forge (CONTEUDO_V1 §6; o manager Anselmo vem com o Charter, C-CORE) e os numeros das regras de cada
    /// papel (ECONOMIA_WAVE_B §2.2-2.3). Os dos distritos 2-6 entram com os DistrictDef: o validator recusa distrito, estacao e upgrade que o
    /// build nao tem.
    /// </summary>
    public static class WorkerDefs
    {
        public const string OldForge = "old_forge";
        // Porter: +6% de passo por nivel acima do 1; +1 de carga em cada nivel desta lista (sobre os 2 -> 4 do "Ajudantes ageis")
        public const float PorterVel = 0.06f;
        public static readonly int[] PorterCargaEm = { 3, 5 };
        // Smith: q +15 na familia (+25 do nivel 5); velocidade da familia +5% no nivel 1 e +3% por nivel acima
        public const int SmithQ = 15, SmithQ5 = 25;
        public const float SmithVel = 0.05f, SmithVelNivel = 0.03f;
        // Clerk (balcao principal): paciencia +10% no nivel 1 e +5% por nivel acima (nivel 5: +30%); 0,40 -> 0,32 s entre duas vendas
        public const float ClerkPaciencia = 0.10f, ClerkPacienciaNivel = 0.05f, ClerkAtende = 0.32f;
        // Quartermaster: saida da fornalha 4 -> 6 e das bancadas 3 -> 5; estoque do balcao +1 por produto a cada nivel acima do 1 (+2 no 3,
        // +4 no 5 como o §2.3; o §2.3 dava so no 3 e no 5, e os niveis 2 e 4 nao fariam nada: decidido por padrao, revisar)
        public const int QmSaida = 2, QmEstoqueNivel = 1;
        // Foreman: q +10 em toda bancada do distrito; velocidade das bancadas +5% no nivel 1 e +2% por nivel acima (nivel 5: +13%)
        public const int ForemanQ = 10;
        public const float ForemanVel = 0.05f, ForemanVelNivel = 0.02f;
        /// <summary>Carga extra do Porter no nivel maximo: a View pre-aloca a pilha na cabeca.</summary>
        public static int CargaMax => PorterCargaEm.Length;

        static readonly long[] Carregador = { 250, 400, 640, 1025 };   // §2.2: 250 x 1,6^(L-2), multiplo de 5

        static WorkerDef P(string id, string nome, string upgrade, int rota, Specialty esp) =>
            new WorkerDef(id, nome, WorkerRole.Porter, esp, OldForge, null, upgrade, rota, 0, null, Carregador, "ajudante", "velocidade", "carga");

        static WorkerDef S(string id, string nome, Specialty esp, string station, string requer, long contratacao, params long[] niveis) =>
            new WorkerDef(id, nome, WorkerRole.Smith, esp, OldForge, station, null, -1, contratacao, requer, niveis, "oficial", "qualidade", "velocidade");

        /// <summary>Os 6 ajudantes primeiro, na ordem do antigo Sim.HireBy (o Load recria a lista de Workers nela: nunca reordene); worker novo
        /// entra no fim.</summary>
        public static readonly WorkerDef[] All =
        {
            P("wk_ajudante1", "Pipo", "ajudante1", 0, Specialty.Carrying),
            P("wk_ajudante2", "Lia", "ajudante2", 1, Specialty.Carrying),
            P("wk_ajudante3", "Caio", "ajudante3", 2, Specialty.Carrying),
            P("wk_joalheiro1", "Nina", "joalheiro1", 3, Specialty.Jewels),
            P("wk_mineiro", "Brás", "mineiro", 0, Specialty.Carrying),
            P("wk_joalheiro2", "Teo", "joalheiro2", 3, Specialty.Jewels),
            S("wk_bruna", "Bruna", Specialty.Swords, "bigorna", "bigornas_nivel2", 1200, 300, 480, 770, 1230),
            S("wk_tome", "Tomé", Specialty.Shields, "escudos", "escudos_nivel2", 1200, 300, 480, 770, 1230),
            S("wk_iara", "Iara", Specialty.Tools, "ferramentas", "ferramentas_nivel2", 1200, 300, 480, 770, 1230),
            S("wk_joaquim", "Joaquim", Specialty.Jewels, "joalheria", "joalheria_nivel2", 2400, 600, 960, 1535, 2460),
            new WorkerDef("wk_celeste", "Celeste", WorkerRole.Clerk, Specialty.Service, OldForge, null, null, -1, 1500, "balcao6",
                new long[] { 375, 600, 960, 1535 }, "balconista", "paciencia", "atendimento"),
            new WorkerDef("wk_baltazar", "Baltazar", WorkerRole.Quartermaster, Specialty.None, OldForge, null, null, -1, 2500, null,   // Renome 1 (RenomeDefs)
                new long[] { 625, 1000, 1600, 2560 }, "almoxarife", "saida", "estoque"),
            new WorkerDef("wk_otavio", "Otávio", WorkerRole.Foreman, Specialty.None, OldForge, null, null, -1, 4000, null,         // Renome 2
                new long[] { 1000, 1600, 2560, 4095 }, "mestre_obras", "qualidade", "velocidade"),
        };

        /// <summary>Indice do worker com este ID, ou -1.</summary>
        public static int IndexOf(string id, WorkerDef[] table = null) => Array.FindIndex(table ?? All, d => d.Id == id);
    }

    /// <summary>
    /// B-CORE-04: a equipe da partida (um por Sim, como o Renome): quem foi contratado, o nivel de cada um, a compra (contratar ou subir nivel,
    /// em ouro, sem sorteio) e o efeito de cada papel, que o Sim le nos ganchos (Recompute, QualityBreakdown, Clients, Serve). Sem ninguem
    /// contratado e todo Porter no nivel 1 cada efeito e' o neutro exato (x 1f, + 0): o Sim e o bot saem identicos ao de antes (BALANCE.md §24).
    /// Revelacao: nada a venda antes das Ferramentas (ECONOMIA_WAVE_B §0.6) nem com a flag Tuning.Workers desligada (quem ja esta contratado
    /// continua). Save: linha wkn=id:nivel (so quem tem nivel); save sem ela = ninguem contratado e os ajudantes no nivel 1.
    /// </summary>
    public sealed class Equipe
    {
        readonly Sim _sim;
        public readonly WorkerDef[] Defs = WorkerDefs.All;
        readonly int[] _nivel, _contrata;   // nivel (0 = nao contratado; Porter: 0 = nivel 1); upgrade que contrata o Porter (-1 = Equipe)
        /// <summary>Metrica do bot (fora do save, como o OrderGold): ouro gasto em contratacao e nivel.</summary>
        public long Gasto;

        public Equipe(Sim sim)
        {
            _sim = sim;
            _nivel = new int[Defs.Length];
            _contrata = Array.ConvertAll(Defs, d => d.Contrata == null ? -1 : Upgrades.IndexOf(d.Contrata));
        }

        public bool Aberta => _sim.T.Workers && _sim.QualityRevealed;
        public bool Contratado(int k) => _contrata[k] >= 0 ? _sim.Bought[_contrata[k]] : _nivel[k] > 0;
        /// <summary>Nivel de `k` (0 = nao contratado).</summary>
        public int Nivel(int k) => Contratado(k) ? Math.Max(1, _nivel[k]) : 0;

        /// <summary>Requisito de contratacao: o upgrade Requer comprado e, se algum nivel de Renome libera o ID, esse nivel alcancado.</summary>
        public bool Liberado(int k)
        {
            WorkerDef d = Defs[k];
            int u = d.Requer == null ? -1 : Upgrades.IndexOf(d.Requer);
            if (d.Requer != null && (u < 0 || !_sim.Bought[u])) return false;
            return RenomeDefs.NivelQueLibera(d.Id) < 0 || _sim.Renome.Libera(d.Id);
        }

        /// <summary>Preco da proxima compra de `k`: contratar ou o proximo nivel; -1 = nada (Porter ainda sem o upgrade, ou nivel maximo).</summary>
        public long Custo(int k)
        {
            WorkerDef d = Defs[k];
            if (!Contratado(k)) return _contrata[k] >= 0 ? -1 : d.Contratacao;
            int n = Nivel(k);
            return n >= d.MaxNivel ? -1 : d.Niveis[n - 1];
        }

        /// <summary>Ouro da compra que levou `k` ao nivel `nivel` (1 = contratacao; o ajudante = 0, o upgrade e' que paga); 0 fora da tabela.</summary>
        public long Preco(int k, int nivel)
        {
            if (k < 0 || k >= Defs.Length) return 0;
            WorkerDef d = Defs[k];
            return nivel == 1 ? d.Contratacao : nivel >= 2 && nivel - 2 < d.Niveis.Length ? d.Niveis[nivel - 2] : 0;
        }

        /// <summary>A venda agora, com ou sem ouro (cartao e bot): aberta, com preco e, para contratar, liberado.</summary>
        public bool AVenda(int k) => Aberta && Custo(k) >= 0 && (Contratado(k) || Liberado(k));

        /// <summary>Contrata `k` ou sobe 1 nivel, cobrando na hora. Fora de AVenda ou sem ouro devolve false e nada muda. Emite Ev.Hired (A =
        /// indice do worker, B = papel, nunca 0: o Porter do upgrade sai com B = 0) ou Ev.WorkerUp (A = indice, B = nivel novo).</summary>
        public bool Comprar(int k)
        {
            long c = Custo(k);
            if (!AVenda(k) || _sim.Gold < c) return false;
            bool contrata = !Contratado(k);
            _sim.Gold -= c; Gasto += c;
            _nivel[k] = contrata ? 1 : Nivel(k) + 1;
            _sim.Recompute();
            if (contrata) _sim.Emit(Ev.Hired, k, (int)Defs[k].Papel, Sim.HireSpot);
            else _sim.Emit(Ev.WorkerUp, k, _nivel[k], Sim.HireSpot);
            return true;
        }

        /// <summary>A compra de worker mais barata que o ouro atual ja paga, ou -1 (o bot decide se vale: Bot.Worker).</summary>
        public int MaisBarato()
        {
            int best = -1;
            for (int k = 0; k < Defs.Length; k++)
                if (AVenda(k) && Custo(k) <= _sim.Gold && (best < 0 || Custo(k) < Custo(best))) best = k;
            return best;
        }

        /// <summary>Ainda ha quem contratar no build (metas worker_hired) e algum nivel a subir (worker_upgrade), contratado ou nao.</summary>
        public bool FaltaContratar
        {
            get { for (int k = 0; k < Defs.Length; k++) if (_contrata[k] < 0 && Nivel(k) == 0) return true; return false; }
        }
        public bool FaltaNivel
        {
            get { for (int k = 0; k < Defs.Length; k++) if (Nivel(k) < Defs[k].MaxNivel) return true; return false; }
        }
        /// <summary>Contratados pela Equipe (os do upgrade contam no Sim.Workers).</summary>
        public int Contratados(WorkerRole? papel = null)
        {
            int n = 0;
            for (int k = 0; k < Defs.Length; k++) if (_contrata[k] < 0 && Nivel(k) > 0 && (papel == null || Defs[k].Papel == papel)) n++;
            return n;
        }

        // ------------------------------------------------------------------ efeitos (ganchos do Sim): neutros exatos sem ninguem

        /// <summary>Nivel do 1o contratado do papel (0 = nenhum). ponytail: o Old Forge tem 1 balconista, 1 almoxarife e 1 mestre de obras; 2
        /// do mesmo papel no distrito pedem somar (Wave C).</summary>
        int NivelPapel(WorkerRole p)
        {
            for (int k = 0; k < Defs.Length; k++) if (Defs[k].Papel == p && Nivel(k) > 0) return Nivel(k);
            return 0;
        }

        /// <summary>Oficial contratado da familia da estacao, ou -1.</summary>
        int Oficial(Station s)
        {
            if (!s.Produces) return -1;
            for (int k = 0; k < Defs.Length; k++) if (Defs[k].Papel == WorkerRole.Smith && Defs[k].Station == s.Recipe.Station && Nivel(k) > 0) return k;
            return -1;
        }

        /// <summary>Multiplicador do passo do ajudante `w` (1 no nivel 1).</summary>
        public float Passo(Carrier w) => w.Wk < 0 ? 1f : 1f + WorkerDefs.PorterVel * (Nivel(w.Wk) - 1);

        /// <summary>Carga extra do ajudante `w` pelo nivel.</summary>
        public int Carga(Carrier w)
        {
            if (w.Wk < 0) return 0;
            int n = Nivel(w.Wk), c = 0;
            foreach (int l in WorkerDefs.PorterCargaEm) if (n >= l) c++;
            return c;
        }

        /// <summary>QualitySource.Worker: o oficial da familia (+15; +25 no nivel 5).</summary>
        public int QualidadeOficial(Station s)
        {
            int k = Oficial(s);
            return k < 0 ? 0 : Nivel(k) >= 5 ? WorkerDefs.SmithQ5 : WorkerDefs.SmithQ;
        }

        /// <summary>QualitySource.Buff: o mestre de obras em toda bancada (+10).</summary>
        public int QualidadeMestre(Station s) => s.Kind == Kind.Crafter && NivelPapel(WorkerRole.Foreman) > 0 ? WorkerDefs.ForemanQ : 0;

        /// <summary>Divisor do tempo da estacao: 1 + oficial da familia + mestre de obras (bancadas). Ninguem = 1f (o tempo de antes, bit a bit).</summary>
        public float Ritmo(Station s)
        {
            float r = 1f;
            int k = Oficial(s), m = NivelPapel(WorkerRole.Foreman);
            if (k >= 0) r += WorkerDefs.SmithVel + WorkerDefs.SmithVelNivel * (Nivel(k) - 1);
            if (m > 0 && s.Kind == Kind.Crafter) r += WorkerDefs.ForemanVel + WorkerDefs.ForemanVelNivel * (m - 1);
            return r;
        }

        /// <summary>Multiplicador da paciencia de quem chega ao balcao principal (balconista).</summary>
        public float Paciencia
        {
            get { int n = NivelPapel(WorkerRole.Clerk); return n == 0 ? 1f : 1f + WorkerDefs.ClerkPaciencia + WorkerDefs.ClerkPacienciaNivel * (n - 1); }
        }

        /// <summary>s entre duas vendas no balcao principal: Balance.ServeTime, ou o do balconista.</summary>
        public float Atendimento => NivelPapel(WorkerRole.Clerk) > 0 ? WorkerDefs.ClerkAtende : Balance.ServeTime;

        /// <summary>Pilha de saida extra (almoxarife) em toda estacao que produz.</summary>
        public int Saida(Station s) => s.Produces && NivelPapel(WorkerRole.Quartermaster) > 0 ? WorkerDefs.QmSaida : 0;

        /// <summary>Estoque extra por produto nos balcoes (almoxarife acima do nivel 1).</summary>
        public int Estoque => Math.Max(0, NivelPapel(WorkerRole.Quartermaster) - 1) * WorkerDefs.QmEstoqueNivel;

        // ------------------------------------------------------------------ save (linha wkn=; o Sim.Load de antes ignora chave desconhecida)

        public void Save(StringBuilder sb)
        {
            var t = new List<string>();
            for (int k = 0; k < Defs.Length; k++) if (_nivel[k] > 0) t.Add(Defs[k].Id + ":" + _nivel[k].ToString(CultureInfo.InvariantCulture));
            if (t.Count > 0) sb.Append("wkn=").Append(string.Join(",", t)).Append('\n');   // ninguem com nivel: o texto do save fica o de antes
        }

        /// <summary>Le a linha wkn= (lixo e negativo = 0, acima do maximo = maximo; ID que o build nao tem conta em Sim.UnknownIds). Chamar antes
        /// das pilhas (stN=): o teto da saida depende do almoxarife.</summary>
        public void Load(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                if (!raw.StartsWith("wkn=", StringComparison.Ordinal)) continue;
                foreach (string tok in raw.Substring(4).Trim().Split(','))
                {
                    string[] kv = tok.Split(':');
                    int k = WorkerDefs.IndexOf(kv[0].Trim(), Defs);
                    if (k < 0) { if (kv[0].Trim().Length > 0) _sim.UnknownIds++; continue; }
                    int n = kv.Length > 1 && int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
                    _nivel[k] = Math.Max(0, Math.Min(Defs[k].MaxNivel, n));
                }
            }
        }
    }
}
