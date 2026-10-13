using System;
using System.Globalization;
using System.IO;
using FS.Core;

namespace FS
{
    // A-PLAT-06 (Documento Mestre s129): so as 3 costuras que ja tem consumidor (o Game) e 2 implementacoes. RemoteConfig, crash e IAP
    // nascem com o fornecedor (decisao P8). Arquivo sem UnityEngine: o coretests compila e testa o Diario e o Salvamento no dotnet.

    /// <summary>Anuncio premiado. Real = AdsLevelPlay (aparelho); simulado = AdsSimulado (Editor, Windows, -fakeads e fallback do build de
    /// desenvolvimento sem anuncio carregado). Os dois saem do Ads.Init.</summary>
    public interface IAds
    {
        /// <summary>Do Show ate a recompensa/falha (o Game fica no estado Ad: simulacao congelada, voltar ignorado).</summary>
        bool Busy { get; }
        /// <summary>Ha anuncio para mostrar agora; false = esconder o botao.</summary>
        bool Ready(string placement);
        /// <summary>O anuncio desta implementacao ja carregou (o simulado, sempre): o -adtest espera o real por ele.</summary>
        bool Loaded(string placement);
        /// <summary>Chama exatamente UM: onReward (assistiu ate o fim) ou onFail (sem anuncio, erro ou fechou antes). Thread principal.</summary>
        void Show(string placement, Action onReward, Action onFail);
        /// <summary>E-PLAT-03: intersticial pronto. So existe com a flag Tuning.Interstitial ligada no Ads.Init (desligada: nada carrega).</summary>
        bool InterstitialReady { get; }
        /// <summary>E-PLAT-03: mostra o intersticial; `done` sempre, uma vez (mostrou e fechou, ou falhou). Thread principal.</summary>
        void ShowInterstitial(Action done);
    }

    /// <summary>Evento da taxonomia (Documento Mestre s86/s87): nome, t = Sim.Time (s de jogo acumulados no save) e 2 parametros.
    /// Hoje = Diario (CSV local); teste = memoria. Nada de dado pessoal: sem ID de aparelho, conta ou localizacao.</summary>
    public interface IAnalytics
    {
        void Log(string evt, float t, string a = "", string b = "");
    }

    /// <summary>Lugar que guarda o texto do save (envelope do SaveEnvelope). Hoje = SaveStore (arquivo + .bak) e SaveEspelho (PlayerPrefs);
    /// teste = memoria. Read: o que nao abre vai para a quarentena da propria implementacao (`corrupt` = quantos foram).</summary>
    public interface ISaveStorage
    {
        SaveEnvelope.Result Read(out int corrupt);
        void Write(string text);
        void Delete();
    }

    /// <summary>
    /// Diario de playtest (persistentDataPath/diario.csv, lido pelo client/tools/diario_report.py): utc,sessao,evento,t_jogo,a,b sem
    /// cabecalho nem aspas (virgula e quebra de linha nos parametros viram espaco). Best-effort: erro de disco nunca derruba o jogo.
    /// Path null = desligado (o Game so liga no build de desenvolvimento ou com -playtest). Rotacao: passou de `max` bytes (ao abrir ou
    /// escrevendo), fica so a cauda mais nova que cabe em max/2, em linhas inteiras: o arquivo nao cresce sem limite e continua um
    /// arquivo = um testador. Sessao = processo, id aleatorio de 8 caracteres (nao identifica o aparelho).
    /// </summary>
    public sealed class Diario : IAnalytics
    {
        public const long MaxBytes = 1 << 20;   // ~4 h de fim de jogo (~4 KB/min); um playtest da Camada 0 nunca chega perto
        readonly string _path, _sid = Guid.NewGuid().ToString("N").Substring(0, 8);
        readonly long _max;
        long _size;

        public Diario(string path, long max = MaxBytes)
        {
            _path = path; _max = max;
            if (path == null) return;
            try { _size = File.Exists(path) ? new FileInfo(path).Length : 0; if (_size > _max) Trim(); }
            catch (Exception) { }
        }

        public void Log(string evt, float t, string a = "", string b = "")
        {
            if (_path == null) return;
            string line = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) + "," + _sid + "," + evt + ","
                + t.ToString("0.0", CultureInfo.InvariantCulture) + "," + Limpa(a) + "," + Limpa(b) + "\n";
            try
            {
                File.AppendAllText(_path, line);
                if ((_size += line.Length) > _max) Trim();
            }
            catch (Exception) { }   // ponytail: diario e' best-effort; disco cheio nunca derruba o jogo
        }

        static string Limpa(string s) => (s ?? "").Replace(',', ' ').Replace('\n', ' ').Replace('\r', ' ');

        void Trim()
        {
            string[] lines = File.ReadAllLines(_path);
            int i = lines.Length; long keep = 0;
            while (i > 0 && keep + lines[i - 1].Length + 1 <= _max / 2) keep += lines[--i].Length + 1;
            File.WriteAllText(_path, i < lines.Length ? string.Join("\n", lines, i, lines.Length - i) + "\n" : "");
            _size = keep;
        }

        static readonly int[] RamGb = { 2, 3, 4, 6, 8, 12, 16 };

        /// <summary>s87 "memory class": a menor classe nominal que cabe a RAM informada (o Android informa menos: 8 GB sai ~7.400 MB).</summary>
        public static string FaixaRam(int mb)
        {
            foreach (int g in RamGb) if (mb <= g * 1024) return g + "GB";
            return "16GB+";
        }

        /// <summary>s87 "fps bucket": media de quadros de um minuto real (meta 60, POCO F4).</summary>
        public static string FaixaFps(float fps) => fps < 20f ? "<20" : fps < 30f ? "20-30" : fps < 45f ? "30-45" : fps < 55f ? "45-55" : "55+";
    }

    /// <summary>
    /// A-PLAT-02/06: carrega e grava o Sim por duas ISaveStorage, o arquivo e o espelho. Sem arquivo bom, o espelho (save da 0.1-0.6 =
    /// migracao; da 0.7 = copia). Nunca grava estado novo por cima de save que nao abriu: o ruim ja saiu do caminho (quarentena de cada
    /// storage), e versao mais nova ou disco ilegivel deixam a sessao sem gravar (Bloqueado).
    /// </summary>
    public sealed class Salvamento
    {
        readonly ISaveStorage _arquivo, _espelho;
        readonly IAnalytics _log;

        public Salvamento(ISaveStorage arquivo, ISaveStorage espelho, IAnalytics log) { _arquivo = arquivo; _espelho = espelho; _log = log; }

        /// <summary>Save de versao mais nova ou disco ilegivel: a sessao inteira nao grava.</summary>
        public bool Bloqueado { get; private set; }

        public Sim Carregar()
        {
            SaveEnvelope.Result r = default; int corrupt = 0; string err = null;
            try { r = _arquivo.Read(out corrupt); }
            catch (Exception e) { err = e.GetType().Name; Bloqueado = true; }
            bool espelho = r.Status == SaveEnvelope.Status.Missing || r.Status == SaveEnvelope.Status.Corrupt;
            if (espelho)
            {
                SaveEnvelope.Result p = _espelho.Read(out int c);
                corrupt += c;
                if (p.Status != SaveEnvelope.Status.Missing && p.Status != SaveEnvelope.Status.Corrupt) r = p;
            }
            bool ok = r.Status == SaveEnvelope.Status.Ok || r.Status == SaveEnvelope.Status.Legacy;
            Bloqueado |= r.Status == SaveEnvelope.Status.NewerSchema;
            Sim sim = Sim.Load(ok ? r.Payload : null);
            if (err != null) _log.Log("save_error", sim.Time, err);
            if (corrupt > 0) _log.Log("save_corrupt", sim.Time, corrupt.ToString(CultureInfo.InvariantCulture), r.Status.ToString());   // Ok/Legacy = recuperado; outro = estado novo
            if (r.Status == SaveEnvelope.Status.NewerSchema) _log.Log("save_newer", sim.Time, r.Schema.ToString(CultureInfo.InvariantCulture), r.Content);
            else if (espelho && ok) _log.Log("save_prefs", sim.Time, r.Status.ToString());   // Legacy = migrou o save da 0.6
            if (sim.UnknownIds > 0) _log.Log("save_ids_desconhecidos", sim.Time, sim.UnknownIds.ToString(CultureInfo.InvariantCulture), r.Content);   // A-CORE-04: conteudo de build mais novo
            return sim;
        }

        public void Gravar(Sim sim, long agora, string versao)
        {
            if (Bloqueado) return;
            string text = SaveEnvelope.Wrap(sim.Save(agora), versao, agora);
            if (sim.ClockWentBack > 0) _log.Log("clock_back", sim.Time, sim.ClockWentBack.ToString(CultureInfo.InvariantCulture), sim.SavedAt.ToString(CultureInfo.InvariantCulture));   // A-CORE-02
            try { _arquivo.Write(text); }
            catch (Exception e) { _log.Log("save_error", sim.Time, e.GetType().Name); }   // disco cheio nao derruba o jogo; o espelho segura o progresso
            _espelho.Write(text);   // espelho na chave antiga: um downgrade para a 0.6 le o mesmo texto
        }

        /// <summary>-reset (dev): apaga o save do arquivo, o .bak e o espelho; a quarentena fica.</summary>
        public void Apagar() { _arquivo.Delete(); _espelho.Delete(); }
    }
}
