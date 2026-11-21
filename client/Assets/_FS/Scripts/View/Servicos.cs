using System;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Text;
using FS.Core;

namespace FS
{
    // A-PLAT-06 (Documento Mestre s129): so as 3 costuras que ja tem consumidor (o Game) e 2 implementacoes. RemoteConfig e analytics
    // remoto: Plataforma.cs; loja (IAP): Loja.cs; crash nasce com o fornecedor (decisao P8). Arquivo sem UnityEngine: o coretests compila e testa o Diario e o Salvamento no dotnet.

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
    /// gravando), fica so a cauda mais nova que cabe em max/2, em linhas inteiras: o arquivo nao cresce sem limite e continua um
    /// arquivo = um testador. Sessao = processo, id aleatorio de 8 caracteres (nao identifica o aparelho).
    /// fix/gc-alocacao-por-quadro: as linhas vao para um StringBuilder reutilizado e o disco recebe o lote a cada FlushS (ou BufferMax
    /// caracteres), e no Flush explicito (o Game chama ao ir para o segundo plano, ao perder o foco, ao sair e ao trocar de cena). Antes era
    /// 1 File.AppendAllText (FileStream novo, I/O sincrono) e ~10 strings por evento, 5-15 por segundo no build dev do soak.
    /// </summary>
    public sealed class Diario : IAnalytics
    {
        public const long MaxBytes = 1 << 20;   // ~4 h de fim de jogo (~4 KB/min); um playtest da Camada 0 nunca chega perto
        public const double FlushS = 5.0;       // o que um kill do sistema pode levar (o segundo plano ja gravou tudo)
        public const int BufferMax = 16 * 1024; // caracteres: rajada grande grava antes dos 5 s
        readonly string _path, _sid = Guid.NewGuid().ToString("N").Substring(0, 8);
        readonly long _max;
        readonly Func<double> _relogio;
        readonly StringBuilder _buf;   // capacidade cheia de uma vez (so com o diario ligado): o lote nao realoca pedacos ao crescer
        readonly object _trava = new object();
        long _size;
        double _ultimoFlush;
        long _seg = long.MinValue;
        string _utc;

        /// <summary>Id da sessao (processo): o mesmo nas linhas do diario e nos eventos da Telemetria.</summary>
        public string Sessao => _sid;

        /// <summary>false = path null (release no aparelho, -testsession): Log nao faz nada, e quem chama pode nem montar os parametros.</summary>
        public bool Ativo => _path != null;

        /// <param name="relogio">segundos monotonicos para o lote (teste); null = Stopwatch.</param>
        public Diario(string path, long max = MaxBytes, Func<double> relogio = null)
        {
            _path = path; _max = max;
            if (relogio == null) { var sw = Stopwatch.StartNew(); relogio = () => sw.Elapsed.TotalSeconds; }
            _relogio = relogio;
            if (path == null) return;
            _buf = new StringBuilder(BufferMax + 1024);
            _ultimoFlush = _relogio();
            try { _size = File.Exists(path) ? new FileInfo(path).Length : 0; if (_size > _max) Trim(); }
            catch (Exception) { }
        }

        public void Log(string evt, float t, string a = "", string b = "")
        {
            if (_path == null) return;
            lock (_trava)
            {
                // mesma linha de antes: utc,sessao,evento,t(0.0),a,b\n; o carimbo do segundo e' reaproveitado (5-15 eventos por segundo)
                DateTime agora = DateTime.UtcNow;
                long seg = agora.Ticks / TimeSpan.TicksPerSecond;
                if (seg != _seg) { _seg = seg; _utc = agora.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture); }
                _buf.Append(_utc).Append(',').Append(_sid).Append(',').Append(evt).Append(',')
                    .Append(t.ToString("0.0", CultureInfo.InvariantCulture)).Append(',');
                Limpa(a); _buf.Append(','); Limpa(b); _buf.Append('\n');
                if (_buf.Length >= BufferMax || _relogio() - _ultimoFlush >= FlushS) Grava();
            }
        }

        /// <summary>Grava o lote pendente. Chame antes de o processo poder morrer (segundo plano, sair) ou de ler o arquivo.</summary>
        public void Flush()
        {
            if (_path == null) return;
            lock (_trava) Grava();
        }

        void Grava()
        {
            _ultimoFlush = _relogio();
            if (_buf.Length == 0) return;
            int n = _buf.Length;
            try
            {
                File.AppendAllText(_path, _buf.ToString());
                if ((_size += n) > _max) Trim();
            }
            catch (Exception) { }   // ponytail: diario e' best-effort; disco cheio nunca derruba o jogo (o lote que falhou cai, como a linha caia)
            _buf.Clear();
        }

        // o Replace de antes, sem string nova: virgula e quebra de linha viram espaco
        void Limpa(string s)
        {
            if (s == null) return;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                _buf.Append(c == ',' || c == '\n' || c == '\r' ? ' ' : c);
            }
        }

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

        /// <summary>Cloud Save: o envelope do ultimo save gravado nesta sessao; antes do 1o, o que abriu (null = estado novo ou legado).</summary>
        public string Atual { get; private set; }

        /// <param name="tuning">o Tuning da partida, ja com o RemoteConfig (A-PLAT-08); null = padrao.</param>
        public Sim Carregar(Tuning tuning = null) => CarregarImperio(tuning).Raiz;

        /// <summary>C-CORE-02: o mesmo, como Imperio (o save de hoje = 1 distrito; `novo` = Tuning de cada outro distrito aberto).</summary>
        public Imperio CarregarImperio(Tuning tuning = null, Func<Tuning> novo = null)
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
            Imperio imp = Imperio.Load(ok ? r.Payload : null, novo, tuning);
            if (r.Status == SaveEnvelope.Status.Ok) Atual = SaveEnvelope.Wrap(r.Payload, r.Content, r.WrittenAt);
            Sim sim = imp.Raiz;
            if (err != null) _log.Log("save_error", sim.Time, err);
            if (corrupt > 0) _log.Log("save_corrupt", sim.Time, corrupt.ToString(CultureInfo.InvariantCulture), r.Status.ToString());   // Ok/Legacy = recuperado; outro = estado novo
            if (r.Status == SaveEnvelope.Status.NewerSchema) _log.Log("save_newer", sim.Time, r.Schema.ToString(CultureInfo.InvariantCulture), r.Content);
            else if (espelho && ok) _log.Log("save_prefs", sim.Time, r.Status.ToString());   // Legacy = migrou o save da 0.6
            if (sim.UnknownIds > 0) _log.Log("save_ids_desconhecidos", sim.Time, sim.UnknownIds.ToString(CultureInfo.InvariantCulture), r.Content);   // A-CORE-04: conteudo de build mais novo
            return imp;
        }

        /// <summary>true = o arquivo gravou (E-PLAT-04: so entao a compra e' confirmada a loja). false = sessao bloqueada ou erro no arquivo:
        /// o espelho segura o progresso, mas a proxima abertura le o arquivo antigo (troca atomica), entao a compra nao conta como gravada.</summary>
        public bool Gravar(Sim sim, long agora, string versao) => Grava(sim, sim.Save(agora), agora, versao);

        /// <summary>C-CORE-02: o Imperio inteiro (1 distrito = o texto do Sim.Save, ao byte).</summary>
        public bool Gravar(Imperio imp, long agora, string versao) => Grava(imp.Raiz, imp.Save(agora), agora, versao);

        bool Grava(Sim sim, string payload, long agora, string versao)
        {
            if (Bloqueado) return false;
            string text = Atual = SaveEnvelope.Wrap(payload, versao, agora);
            if (sim.ClockWentBack > 0) _log.Log("clock_back", sim.Time, sim.ClockWentBack.ToString(CultureInfo.InvariantCulture), sim.SavedAt.ToString(CultureInfo.InvariantCulture));   // A-CORE-02
            bool ok = true;
            try { _arquivo.Write(text); }
            catch (Exception e) { ok = false; _log.Log("save_error", sim.Time, e.GetType().Name); }   // disco cheio nao derruba o jogo; o espelho segura o progresso
            _espelho.Write(text);   // espelho na chave antiga: um downgrade para a 0.6 le o mesmo texto
            return ok;
        }

        /// <summary>Cloud Save (Baixar): o envelope da nuvem vira o save do aparelho (arquivo com .bak + espelho); quem chama recarrega a cena.</summary>
        public void GravarTexto(string envelope)
        {
            if (Bloqueado || string.IsNullOrEmpty(envelope)) return;
            _arquivo.Write(envelope);
            _espelho.Write(envelope);
            Atual = envelope;
        }

        /// <summary>-reset (dev): apaga o save do arquivo, o .bak e o espelho; a quarentena fica.</summary>
        public void Apagar() { _arquivo.Delete(); _espelho.Delete(); }
    }
}
