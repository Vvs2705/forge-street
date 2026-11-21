using System;
using System.Globalization;
using System.Text;

namespace FS.Core
{
    /// <summary>Uma obra publica do Reino (nome em Loc "obra.{id}"): Etapas degraus seguidos, a ultima fecha a obra.</summary>
    public sealed class ObraDef
    {
        public readonly string Id;
        public ObraDef(string id) { Id = id; }
    }

    /// <summary>
    /// Obras do Reino (Conselho do TIME DE JOGOS, 2026-11-21; known issue P1 "fim de jogo sem destino para o ouro"): o ralo de ouro sem fim.
    /// - Abre no Charter do Old Forge (Imperio.Charters), antes da Carta: todo distrito seguinte ja tem para onde mandar o ouro parado.
    /// - Degrau n (0..): obra Defs[(n / Etapas) % 10], etapa n % Etapas + 1, nivel n / 30 + 1 (a 11a obra e' a Ponte de novo, nivel II). Sempre
    ///   ha um proximo degrau; tudo deterministico (sem sorteio).
    /// - Custo(n) = (MinBase + MinPasso x n) minutos da RENDA ALVO do distrito mais avancado aberto (CONTEUDO_V1 §1: 2.400/min no Old Forge, x3
    ///   por distrito), multiplo de 5. A referencia e' do conteudo, nao a RateEma: trocar para um distrito barato, ficar parado ou ligar o 2x
    ///   nao muda o preco (nada a explorar, e o numero na tela nao treme).
    /// - Recompensa permanente e visivel, sem moeda nova e sem ouro direto (o ralo nunca vira fonte): Renome (RenomeEtapa por degrau e
    ///   RenomeObra a mais ao fechar a obra, 1x pelo ID obra:{n} no ledger do Renome) e o Prestigio do Reino: +BonusPct% na renda de fundo dos
    ///   distritos fora de foco, 2 x raiz(n) arredondado para baixo, teto 20% (curva decrescente; o teto no degrau 100). O fundo segue < 1 vez a
    ///   taxa e o offline segue com o teto de 2 h (o bonus mexe na taxa, nunca no tempo).
    /// - Save: linha obras=N do Imperio (so com N > 0; sem ela = 0, o save antigo abre igual).
    /// </summary>
    public sealed class Obras
    {
        public const int Etapas = 3, MinBase = 15, MinPasso = 5, RenomeEtapa = 10, RenomeObra = 30, BonusTeto = 20;
        /// <summary>Ouro/min alvo do Old Forge no Charter (CONTEUDO_V1 §1; o mesmo do Imperio.Chegada); cada distrito x3.</summary>
        public const double RendaOF = 2400;
        public const string Prefixo = "obra:";

        /// <summary>As 10 obras do rodizio, na ordem (nomes originais em Loc).</summary>
        public static readonly ObraDef[] Defs =
        {
            new ObraDef("obra_ponte"), new ObraDef("obra_poco"), new ObraDef("obra_muralha"), new ObraDef("obra_aqueduto"), new ObraDef("obra_farol"),
            new ObraDef("obra_mercado"), new ObraDef("obra_torre_sinos"), new ObraDef("obra_biblioteca"), new ObraDef("obra_catedral"), new ObraDef("obra_porto"),
        };

        readonly Imperio _imp;
        /// <summary>Degraus construidos (o proximo e' o de indice Degraus).</summary>
        public int Degraus { get; private set; }
        /// <summary>Metrica (fora do save): ouro gasto nas obras nesta sessao.</summary>
        public long Gasto;

        public Obras(Imperio imp) { _imp = imp; }

        /// <summary>Abre com o Charter do Old Forge.</summary>
        public bool Aberta => _imp.Charters.Contains(Distritos.OldForgeId);

        public static ObraDef ObraDe(int n) => Defs[n / Etapas % Defs.Length];
        public static int EtapaDe(int n) => n % Etapas + 1;
        public static int NivelDe(int n) => n / (Etapas * Defs.Length) + 1;
        public static long Minutos(int n) => MinBase + (long)MinPasso * n;
        public static int RenomeDe(int n) => RenomeEtapa + (EtapaDe(n) == Etapas ? RenomeObra : 0);
        public static int Bonus(int degraus) => Math.Min(BonusTeto, (int)Math.Floor(2.0 * Math.Sqrt(Math.Max(0, degraus)) + 1e-9));

        /// <summary>% a mais na renda de fundo com os degraus de agora (Imperio.FundoFator).</summary>
        public int BonusPct => Bonus(Degraus);

        /// <summary>Ouro/min alvo do distrito mais avancado aberto: RendaOF x 3^Indice.</summary>
        public double RendaRef
        {
            get
            {
                int i = 0;
                foreach (Sim s in _imp.Abertos) i = Math.Max(i, s.Distrito.Indice);
                return RendaOF * Math.Pow(3, i);
            }
        }

        /// <summary>Ouro do degrau `n` agora (multiplo de 5; satura em long).</summary>
        public long Custo(int n)
        {
            double c = RendaRef * Minutos(n);
            return c >= long.MaxValue / 2 ? long.MaxValue / 2 : Math.Max(5, 5 * (long)Math.Round(c / 5.0));
        }

        public long Proximo => Custo(Degraus);

        /// <summary>Constroi o degrau `degrau` pagando da carteira (o Gold do ativo). ID de transacao = o indice: so o proximo, 1x (repetir o toque
        /// ou o mesmo degrau depois de save/load devolve false e nada muda); fechada ou sem ouro, idem.</summary>
        public bool Construir(int degrau)
        {
            if (!Aberta || degrau != Degraus) return false;
            long c = Custo(degrau);
            if (_imp.Ativo.Gold < c) return false;
            _imp.Ativo.Gold -= c;
            Gasto += c;
            Degraus++;
            _imp.Raiz.Renome.Credita(Prefixo + degrau.ToString(CultureInfo.InvariantCulture), RenomeDe(degrau));
            return true;
        }

        public void Save(StringBuilder sb)
        {
            if (Degraus > 0) sb.Append("obras=").Append(Degraus.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        /// <summary>Valor da linha obras= (lixo ou negativo = 0).</summary>
        public void Load(string val)
        {
            Degraus = int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? Math.Max(0, n) : 0;
        }
    }
}
