using System;
using System.Collections.Generic;
using System.Globalization;
using FS.Core;

namespace FS
{
    /// <summary>
    /// G-UA-02 (docs/CRIATIVOS_BANCO_V1.md a; Documento Mestre s78): presets de captura dos criativos. `-preset nome` expande para as
    /// flags da tabela e a linha de comando vence (Arg): `-preset vip -recordsec 10` troca so a duracao. A pasta do `-record` vem sempre
    /// da linha de comando. A parte pura (tabela, Arg, compras do -buyids, -offline) roda no coretests (CapturaTests monta cada preset no
    /// Core); o relogio da HUD fica atras do #if, como no StationView.
    /// </summary>
    public static class Captura
    {
        /// <summary>Semente do UnityEngine.Random com -record ou -preset: fumaca, faiscas e poeira iguais em 2 execucoes.</summary>
        public const int Semente = 78;

        // producao ate o Martelo veloz + balcao de 8 vagas, sem a rua lateral
        const string Oficina = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,25,26,27,28";

        /// <summary>Nome -> flags. Os tempos sairam do Core com o mesmo warmup do Game.DevArgs (jogador parado) e o mesmo passo de 1/30 s.</summary>
        public static readonly (string Nome, string Flags)[] Presets =
        {
            ("start", "-bot -recordsec 4"),                                                           // save novo, o bot comeca a jogar
            ("huge_queue", "-buyids 10,25,26,27,28 -warmup 45 -px 4.5,8.2 -recordfull -recordsec 8"),  // fila 8/8 sem producao, cheia ate 15 s
            ("first_worker", "-bot -buyids 0,1,3,4,9 -hold 0,3,3,3,0,0 -recordhire -recordsec 10"),    // FS-C-11: o bot contrata aos 29,7 s
            ("conveyor", "-bot -buyids 0,1,2,3,4,5,6 -warmup 20 -cam 7 -recordsec 12"),               // camera perto seguindo o ferreiro
            ("vip", "-buyids 1,2,3,5,6,10,12,14 -warmup 200 -px 4.5,8.2 -vipnow -recordsec 30"),      // FS-C-24: 1a venda real aos 18,5 s
            ("order", "-buyids 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14 -warmup 60 -px 4.5,8.2 -recordorder -recordsec 20"),   // 5 espadas: 59 s -> 76,9 s
            ("offline", "-buyids " + Oficina + " -warmup 120 -px 4.5,8.2 -offline 7200 -recordsec 4"),   // +1380 (teto = 2x o Corredor)
            ("full_district", "-buy 29 -warmup 120 -px 4.5,8.2 -recordsec 15"),                       // interino ate a Wave C: a oficina inteira
            ("district_unlock", "-buyids " + Oficina + ",15@100 -warmup 98 -px 8,8.2 -recordsec 8"),   // interino: Corredor aos 100 s (ouro 704)
        };

        public static string Nomes => string.Join(" ", Array.ConvertAll(Presets, p => p.Nome));

        /// <summary>Flags do preset, ou null se o nome nao existe.</summary>
        public static string Flags(string nome)
        {
            foreach (var p in Presets) if (p.Nome == nome) return p.Flags;
            return null;
        }

        /// <summary>Valor de `flag` em `args`: null = ausente, "" = sem valor (o seguinte comeca com '-'). O Ads.Arg usa o mesmo.</summary>
        public static string Valor(IList<string> args, string flag)
        {
            int i = args.IndexOf(flag);
            if (i < 0) return null;
            return i + 1 < args.Count && !args[i + 1].StartsWith("-", StringComparison.Ordinal) ? args[i + 1] : "";
        }

        /// <summary>Flag da linha de comando `cmd`; se ela nao tem, a do `-preset` (nome desconhecido = nenhuma; o Game.DevArgs sai 1).</summary>
        public static string Arg(IList<string> cmd, string flag)
        {
            string v = Valor(cmd, flag);
            if (v != null) return v;
            string p = Flags(Valor(cmd, "-preset"));
            return p == null ? null : Valor(p.Split(' '), flag);
        }

        /// <summary>-buyids 1,5,0@300 ou fornalha2,bigorna2@300: compra ja os ids sem "@" (estado montado, de graca, como sempre foi) e devolve os "id@t" na ordem
        /// dada. Token invalido ou id fora do enum e' ignorado.</summary>
        public static List<(int u, float t)> Compras(Sim s, string buyids)
        {
            var at = new List<(int u, float t)>();
            foreach (string tok in (buyids ?? "").Split(','))
            {
                string[] p = tok.Split('@');
                int id = int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int k) ? k : Upgrades.IndexOf(p[0].Trim());   // indice ou ID estavel (A-CORE-04)
                if (id < 0 || id >= Upgrades.Count) continue;
                if (p.Length == 1) s.Buy((Upgrade)id);
                else if (float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float t)) at.Add((id, t));
            }
            return at;
        }

        /// <summary>Faz as compras "id@t" vencidas (s.Time >= t) cobrando o preco. A 1a impossivel (PodeComprar) sai da lista e volta como
        /// erro, sem comprar: criativo nao mostra compra impossivel.</summary>
        public static string Vence(Sim s, List<(int u, float t)> at)
        {
            for (int i = 0; i < at.Count;)
            {
                if (s.Time < at[i].t) { i++; continue; }
                (int u, float t) = at[i];
                at.RemoveAt(i);
                string nao = PodeComprar(s, u);
                if (nao != null) return $"-buyids {u}@{t.ToString(CultureInfo.InvariantCulture)}: {nao}";
                s.Buy((Upgrade)u);
                s.Gold -= s.T.Cost(u);
            }
            return null;
        }

        /// <summary>Por que a compra `u` nao seria possivel agora no jogo (pad ou menu), ou null (antes o -buyids id@t, sem ouro, levava o
        /// ouro a 0 e ignorava o pre-requisito).</summary>
        public static string PodeComprar(Sim s, int u)
        {
            UpgradeDef d = Upgrades.All[u];
            if (s.Bought[u]) return d.Name + " ja comprado";
            if (d.Requires >= 0 && !s.Bought[d.Requires]) return d.Name + " sem " + Upgrades.All[d.Requires].Name;
            if (d.Luxury && !s.ProductionComplete) return d.Name + ": luxo antes da producao completa";
            long c = s.T.Cost(u);
            return s.Gold < c ? $"{d.Name} custa {c}, ouro {s.Gold}" : null;
        }

        /// <summary>
        /// B-CORE-03 -qualidade [-golpe] (fotos): compra a producao ate as Ferramentas (revela a qualidade) e os niveis II-III das Bigornas e II
        /// dos Escudos e das Ferramentas; poe 1 Fina e 1 Superior no topo de cada pilha de saida e na frente de cada produto do balcao (4 no
        /// estoque), o acumulador das bancadas a 60% e o ferreiro parado na boca de entrada da Bigorna. `golpe`: entrada da Bigorna cheia e
        /// recarga zerada, entao o anel aparece (Sim.StrikeStation).
        /// </summary>
        public static void Qualidade(Sim s, bool golpe)
        {
            for (int i = 0; i <= (int)Upgrade.Tools; i++) s.Buy((Upgrade)i);
            foreach (Upgrade u in new[] { Upgrade.AnvilsLv2, Upgrade.AnvilsLv3, Upgrade.ShieldsLv2, Upgrade.ToolsLv2 }) s.Buy(u);
            foreach (Station st in s.Stations)
            {
                if (!st.Unlocked || st.Kind != Kind.Crafter) continue;
                st.Out = st.OutCap; st.OutQ[(int)Quality.Fine] = 1; st.OutQ[(int)Quality.Superior] = 1; st.QAcc = 60;
            }
            for (Item i = Item.Sword; i <= Item.Tool; i++)
            {
                if (!s.LineUnlocked(i)) continue;
                s.Stock[(int)i] = Math.Min(4, s.CounterCap); s.StockQ[(int)i][(int)Quality.Fine] = 1; s.StockQ[(int)i][(int)Quality.Superior] = 1;
            }
            s.Player.Pos = s.AnvilA.InAt;
            if (!golpe) return;
            s.AnvilA.Ins[0] = s.AnvilA.InCap;
            s.StrikeCooldown = 0f;
        }

        /// <summary>
        /// B-CORE-04 -equipe (fotos): a oficina toda ate o Joalheiro 2 e os niveis II das 4 familias (o requisito dos oficiais), Balcao 6, Renome
        /// 2, os 6 ajudantes no nivel 3 (carga +1) e os 7 da Equipe contratados, cada um no posto (WorldView.Posto). O ouro volta ao que era:
        /// o -gold da linha de comando vem depois (cartoes de nivel disponiveis na foto com -menu).
        /// </summary>
        public static void Equipe(Sim s)
        {
            foreach (string id in new[] { "fole", "bigorna2", "ajudante1", "escudos", "mochila", "ajudante2", "esteira", "fole_duplo", "botas", "ferramentas",
                "vitrine", "fornalha2", "ajudante3", "ajudantes_ageis", "martelo_veloz", "corredor", "joalheria", "joalheiro1", "mineiro", "joalheiro2",
                "balcao5", "balcao6", "bigornas_nivel2", "escudos_nivel2", "ferramentas_nivel2", "joalheria_nivel2" })
                s.Buy((Upgrade)Upgrades.IndexOf(id));
            s.Renome.Ganha(RenomeDefs.Niveis[2].Limiar - s.Renome.Total);
            long ouro = s.Gold;
            for (int k = 0; k < s.Equipe.Defs.Length; k++)
                for (int n = s.Equipe.Nivel(k); n < (s.Equipe.Defs[k].Papel == WorkerRole.Porter ? 3 : 1); n++)
                {
                    s.Gold = s.Equipe.Custo(k);
                    s.Equipe.Comprar(k);
                }
            s.Gold = ouro;
            s.Player.Pos = new V2(4.5f, 7.5f);   // no meio da oficina: oficiais, balconista e mestre de obras na tela
        }

        /// <summary>-offline S: s fora para o cofre real (Sim.ApplyOffline); -1 = fora de 60 s (abaixo o jogo nao paga) ao teto de 2 h.</summary>
        public static long OfflineSegundos(string v) =>
            long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out long s) && s >= 60 && s <= Balance.OfflineCapSeconds ? s : -1;

#if !FS_CORETESTS
        /// <summary>Relogio das animacoes da HUD que correm em tempo real (aviso, cartao da encomenda, pulsos do selo e do menu; o anuncio
        /// simulado zera o timeScale). Gravando, e' o relogio do quadro (frameCount x captureDeltaTime): nao depende da maquina.</summary>
        public static float Agora => UnityEngine.Time.captureDeltaTime > 0f ? UnityEngine.Time.frameCount * UnityEngine.Time.captureDeltaTime : UnityEngine.Time.unscaledTime;
        public static float Passo => UnityEngine.Time.captureDeltaTime > 0f ? UnityEngine.Time.captureDeltaTime : UnityEngine.Time.unscaledDeltaTime;
#endif
    }
}
