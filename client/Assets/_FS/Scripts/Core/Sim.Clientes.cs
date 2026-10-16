using System;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>
    /// C-CORE-06 (CONTEUDO_V1 §12, DM §20): familia de cliente em dados. `Unidades` por visita (mercador e emissario: 2), `Paciencia` e
    /// `Intervalo` multiplicam a paciencia do produto (Tuning.PatienceFor) e o intervalo da linha (Sim.ClientInterval), `Minima` = qualidade
    /// minima da peca que ele leva (colecionador e emissario: Fina), `Rico` = conta no RichClients (nobre, colecionador), `Pede` = o rodizio
    /// fixo de pedidos (n-esimo cliente da familia no distrito -> n-esimo produto aberto da lista; null = todo produto do distrito). Abre: o
    /// nivel de Renome que libera "cli_" + Id (RenomeDefs: cli_colecionador no 2); sem nivel, sempre. Sem sorteio.
    /// </summary>
    public sealed class ClientDef
    {
        public readonly string Id;
        public readonly int Unidades;
        public readonly float Paciencia, Intervalo;
        public readonly Quality Minima;
        public readonly bool Rico;
        public readonly string[] Pede;
        public ClientDef(string id, int unidades, float paciencia, float intervalo, Quality minima, bool rico, params string[] pede)
        {
            Id = id; Unidades = unidades; Paciencia = paciencia; Intervalo = intervalo; Minima = minima; Rico = rico; Pede = pede.Length == 0 ? null : pede;
        }
        public string RenomeId => "cli_" + Id;
    }

    /// <summary>
    /// C-CORE-06: clientes por familia (generico; o Artisan Quarter e' o 1o a declarar). Distrito com DistrictDef.Clientes troca o relogio por
    /// produto do Sim.Clients por um relogio por familia: no zero chega o proximo cliente dela, pedindo o produto da vez no rodizio (so linha
    /// aberta com cliente e, com qualidade minima, que a automacao ja faz nesse nivel), e o relogio soma o intervalo daquela linha x o da
    /// familia (ricos x0,7 com a vitrine). Na fila: paciencia do produto x a da familia (x o balconista), o pacote de `Unidades` (1 por
    /// atendimento, como o VIP) e so a peca na qualidade minima serve. Ricos atendidos (o pacote inteiro) contam em ClientesRicos (o RichClients
    /// do §2.3, condicao de Masterwork). Save: linhas opcionais fam= (posicao do rodizio de cada familia) e ricos= (so quando passam de 0).
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>RichClients (CONTEUDO_V1 §2.3 regra 5): nobres e colecionadores atendidos neste distrito. Vai no save (ricos=).</summary>
        public int ClientesRicos;
        float[] _famT; int[] _famN; int[][] _famPede;

        /// <summary>Do construtor: relogio e rodizio de cada familia (a 1a chega no FirstClientAt, como o 1o cliente de sempre).</summary>
        void PreparaFamilias()
        {
            ClientDef[] fs = Distrito.Clientes;
            if (fs == null) return;
            _famT = new float[fs.Length]; _famN = new int[fs.Length];
            _famPede = new int[fs.Length][];
            for (int f = 0; f < fs.Length; f++)
            {
                if (fs[f].Pede != null) _famPede[f] = Array.ConvertAll(fs[f].Pede, id => Items.IndexOf(id, ItemDefs));
                else { var l = new System.Collections.Generic.List<int>(); for (int i = 0; i < ItemDefs.Length; i++) if (IsProduct((Item)i)) l.Add(i); _famPede[f] = l.ToArray(); }
                int p0 = Array.Find(_famPede[f], i => i >= 0 && HasClients((Item)i));
                _famT[f] = f == 0 ? T.FirstClientAt : T.ClientInterval[p0] * fs[f].Intervalo;
            }
        }

        /// <summary>A familia `f` ja chega: o Renome libera (quando algum nivel a lista).</summary>
        bool FamiliaAberta(int f)
        {
            string id = Distrito.Clientes[f].RenomeId;
            return RenomeDefs.NivelQueLibera(id) < 0 || Renome.Libera(id);
        }

        /// <summary>O produto `i` entra no rodizio da familia `f`: linha aberta com cliente e, com qualidade minima, feito nesse nivel.</summary>
        bool PedeAgora(int f, int i) => i >= 0 && LineUnlocked((Item)i) && HasClients((Item)i) && FazNoNivel((Item)i, Distrito.Clientes[f].Minima);

        /// <summary>O produto do proximo cliente da familia `f` (n-esimo cliente -> n-esimo produto aberto da lista), ou -1 (familia fechada).</summary>
        int Pedido(int f)
        {
            if (!FamiliaAberta(f)) return -1;
            int[] l = _famPede[f]; int n = 0;
            foreach (int i in l) if (PedeAgora(f, i)) n++;
            if (n == 0) return -1;
            int alvo = _famN[f] % n;
            foreach (int i in l) if (PedeAgora(f, i) && alvo-- == 0) return i;
            return -1;
        }

        /// <summary>Alguma estacao aberta que faz `i` ja tira o nivel `min` pela automacao (Normal: sempre; Fina: q > 0; Superior: q > 100).
        /// ponytail: o q da bancada configuravel e' o da receita ativa (a familia e' a mesma; so a tecnica de produto muda).</summary>
        bool FazNoNivel(Item i, Quality min)
        {
            if (min <= Quality.Normal) return true;
            foreach (Station s in Stations) if (s.Unlocked && Faz(s, i) && QualityPoints(s) > ((int)min - 1) * 100) return true;
            return false;
        }

        /// <summary>Do Sim.Clients no lugar do relogio por produto (distrito com familias).</summary>
        void ChegamFamilias(float dt)
        {
            ClientDef[] fs = Distrito.Clientes;
            for (int f = 0; f < fs.Length; f++)
            {
                int p = Pedido(f);
                if (p < 0) continue;
                _famT[f] -= dt * BoostMul;   // FASE8: a demanda acompanha o boost
                if (_famT[f] > 0f) continue;
                Item it = (Item)p;
                _famT[f] += ClientInterval(it) * fs[f].Intervalo * (fs[f].Rico && Has(Upgrade.JewelVitrine) ? T.JewelVitrineClientMul : 1f);
                _famN[f]++;
                float pat = T.PatienceFor(it, RecipeDefs) * Equipe.Paciencia * fs[f].Paciencia;
                if (Queue.Count < QueueCap)
                {
                    Queue.Add(new Client { Want = it, Patience = pat, MaxPatience = pat, Pack = fs[f].Unidades, MinQ = fs[f].Minima, Familia = f });
                    Emit(Ev.ClientArrived, p, Queue.Count, ClientSlot(Queue.Count - 1));
                }
                else { ClientsTurnedAway++; Emit(Ev.ClientLeft, p, 2, ClientSlot(QueueCap)); }
            }
        }

        /// <summary>Sim.Serve: o balcao tem a peca que o cliente `c` aceita (qualidade minima; Normal = qualquer).</summary>
        bool TemParaCliente(Client c)
        {
            int[] q = StockQ[(int)c.Want];
            if (c.MinQ <= Quality.Normal) return true;
            int n = 0;
            for (int l = (int)c.MinQ; l < Graus; l++) n += q[l];
            return n > 0;
        }

        /// <summary>Sim.Serve: o cliente levou o pacote inteiro; rico conta no RichClients.</summary>
        void Atendido(Client c)
        {
            if (c.Familia >= 0 && Distrito.Clientes != null && Distrito.Clientes[c.Familia].Rico) ClientesRicos++;
        }

        // ------------------------------------------------------------------ save (fam= e ricos=, so com algo; o Load de antes ignora)

        void SalvaFamilias(StringBuilder sb)
        {
            var ci = CultureInfo.InvariantCulture;
            if (_famN != null && Array.Exists(_famN, n => n > 0)) sb.Append("fam=").Append(string.Join(",", Array.ConvertAll(_famN, n => n.ToString(ci)))).Append('\n');
            if (ClientesRicos > 0) sb.Append("ricos=").Append(ClientesRicos.ToString(ci)).Append('\n');
        }

        void CarregaFamilias(string key, string val)
        {
            if (key == "ricos") ClientesRicos = Math.Max(0, I(val, 0));
            else if (key == "fam" && _famN != null)
            {
                string[] p = val.Split(',');
                for (int f = 0; f < Math.Min(p.Length, _famN.Length); f++) _famN[f] = Math.Max(0, I(p[f], 0));
            }
        }

        // ------------------------------------------------------------------ validator (ContentValidator.Distrito)

        /// <summary>Familias do distrito: ID [a-z0-9_] unico, 1+ unidades, paciencia e intervalo positivos, qualidade minima ate Superior, todo
        /// pedido e' produto do distrito com cliente (intervalo no Tuning); a 1a familia sempre aberta e pedindo o produto do 1o cliente (o
        /// handle 2, do kit).</summary>
        public static System.Collections.Generic.IEnumerable<string> ValidarFamilias(DistrictDef d, Sim w)
        {
            if (d.Clientes == null) yield break;
            var ids = new System.Collections.Generic.HashSet<string>();
            for (int f = 0; f < d.Clientes.Length; f++)
            {
                ClientDef c = d.Clientes[f];
                string at = $"distrito {d.Id}: cliente {c.Id}:";
                if (string.IsNullOrEmpty(c.Id) || !Array.TrueForAll(c.Id.ToCharArray(), x => x >= 'a' && x <= 'z' || x >= '0' && x <= '9' || x == '_')) yield return $"{at} ID fora de [a-z0-9_]";
                else if (!ids.Add(c.Id)) yield return $"{at} ID repetido";
                if (c.Unidades < 1 || c.Paciencia <= 0f || c.Intervalo <= 0f) yield return $"{at} {c.Unidades} unidades, paciencia x{c.Paciencia}, intervalo x{c.Intervalo}";
                if (c.Minima > Quality.Superior) yield return $"{at} qualidade minima {c.Minima} (Masterwork nunca e' vendida)";
                foreach (string id in c.Pede ?? new string[0])
                {
                    int i = Items.IndexOf(id, w.ItemDefs);
                    if (i < 0 || !w.IsProduct((Item)i) || !w.HasClients((Item)i)) yield return $"{at} pede \"{id}\", que nao e' produto com cliente no distrito";
                }
            }
            ClientDef p = d.Clientes.Length > 0 ? d.Clientes[0] : null;
            if (p == null || RenomeDefs.NivelQueLibera(p.RenomeId) >= 0 || p.Minima > Quality.Normal || p.Pede != null && Array.IndexOf(p.Pede, d.Itens[2].Id) < 0)
                yield return $"distrito {d.Id}: a 1a familia tem de chegar desde o kit pedindo {d.Itens[2].Id} (o 1o cliente)";
        }
    }
}
