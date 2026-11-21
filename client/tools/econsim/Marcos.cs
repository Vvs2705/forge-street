using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FS.Core;

/// <summary>
/// --marcos pasta (PROJETO.md §8, regua S/E de 2026-11-21): saves prontos dos marcos para o playtest humano no build Windows, sem ninguem
/// jogar 30 h ate la. O bot (perfil do --perfis, padrao F2PGolpe: o F2P que golpeia) joga a campanha do Imperio do zero, como o --campanha,
/// e grava o save do Imperio inteiro (envelope do SaveEnvelope, o mesmo que a View grava em save.txt) em cada marco:
///   01_old_forge_completo   a producao completa do Old Forge
///   NN_charter_&lt;distrito&gt;  logo depois de cada Charter concedido (o Royal Charter que abre o proximo)
///   NN_carta_mais_30min     30 min de relogio depois do Charter da Grand Forge (a Carta)
///   NN_tudo_comprado        todas as melhorias de todos os distritos abertos compradas (o fim de jogo que o Vinicius jogou)
/// Cada save e' relido (SaveEnvelope.Unwrap + Imperio.Load) e conferido: Charters, distritos abertos e Renome iguais ao da partida; senao
/// sai 1. Nada aqui muda o jogo nem o econsim de antes. O `at=` do envelope e' o agora: ao abrir dias depois, o cofre offline paga ate 2 h
/// (o teto do nucleo), como para qualquer jogador.
/// </summary>
static partial class EconSim
{
    static int Marcos(string pasta, BotProfile perfil, int horas, float anuncioSeg, string knobs, string versao)
    {
        Directory.CreateDirectory(pasta);
        Func<Tuning> tuning = () => Knobs(knobs, new Tuning());
        var imp = new Imperio(new Sim(tuning()), tuning);
        Bot bot = Novo(perfil);
        const float hz = 30f;
        float dt = 1f / hz, relogio = 0f, conferido = 0f, carta = -1f;
        int ads = 0, n = 0, falhas = 0;
        var feitos = new HashSet<string>();
        var linhas = new List<string>();
        long agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        void Grava(string nome)
        {
            if (!feitos.Add(nome)) return;
            n++;
            string arq = Path.Combine(pasta, $"{n:00}_{nome}.save.txt");
            string texto = SaveEnvelope.Wrap(imp.Save(agora), versao, agora);
            File.WriteAllText(arq, texto, new System.Text.UTF8Encoding(false));
            // confere: o que a View leria
            SaveEnvelope.Result r = SaveEnvelope.Unwrap(File.ReadAllText(arq));
            Imperio lido = r.Status == SaveEnvelope.Status.Ok ? Imperio.Load(r.Payload, tuning, tuning()) : null;
            bool ok = lido != null && lido.Charters.SetEquals(imp.Charters) && lido.Abertos.Count == imp.Abertos.Count
                      && lido.Raiz.Renome.Total == imp.Raiz.Renome.Total;
            if (!ok) falhas++;
            string ativo = imp.Ativo.Distrito?.Id ?? Distritos.OldForgeId;
            linhas.Add($"| {n:00}_{nome} | {relogio / 3600f:0.0} | {ativo} | {imp.Raiz.Renome.Nivel} ({imp.Raiz.Renome.Total}) | {imp.Charters.Count} | {imp.Abertos.Count} | {Ouro(imp.Ativo.Gold)} | {(ok ? "ok" : "FALHOU")} |");
        }

        bool TudoComprado() => imp.Abertos.Count == Distritos.All.Length && imp.Abertos.All(s => s.Bought.All(b => b));

        while (relogio < horas * 3600f)
        {
            Sim s = imp.Ativo;
            bot.Step(s, dt);
            Bot.Manager(imp);
            imp.Fundo(dt);
            relogio += dt + (bot.AdsTotal - ads) * anuncioSeg; ads = bot.AdsTotal;
            if (relogio - conferido < 10f) continue;   // como o --campanha: o jogador olha o mapa a cada 10 s
            conferido = relogio;
            if (imp.Raiz.ProductionComplete) Grava("old_forge_completo");
            string id = s.Distrito?.Id ?? Distritos.OldForgeId;
            if (imp.CharterFalta(id) == null && imp.Charter(id))
            {
                Grava("charter_" + id);
                if (Distritos.Find(id)?.Charter?.Proximo == null) carta = relogio;
            }
            string prox = Distritos.Find(id)?.Charter?.Proximo;
            if (prox != null && !imp.Aberto(prox) && imp.Abrir(prox) && imp.Trocar(prox)) { bot = Novo(perfil); ads = 0; }
            if (carta >= 0f && relogio - carta >= 1800f) Grava("carta_mais_30min");
            if (TudoComprado()) { Grava("tudo_comprado"); break; }
        }
        if (!feitos.Contains("tudo_comprado")) Grava("fim_" + horas + "h");   // nao chegou ao "tudo comprado": o fim da partida, com o nome dizendo isso

        Console.WriteLine($"econsim --marcos: perfil {perfil.Name}, ate {horas} h de relogio (anuncio = {anuncioSeg:0.#} s), versao {versao}, pasta {pasta}");
        Console.WriteLine("| save | relogio (h) | distrito ativo | Renome (total) | Charters | abertos | ouro | relido |");
        Console.WriteLine("|---|---|---|---|---|---|---|---|");
        foreach (string l in linhas) Console.WriteLine(l);
        File.WriteAllLines(Path.Combine(pasta, "LEIA.md"), new[] {
            "# Saves de marco do Forge Street", "",
            $"Gerados pelo econsim (perfil {perfil.Name}) na versao {versao}. Roteiro: docs/ROTEIRO_PLAYTEST_MARCOS.md no repositorio.", "",
            "Com o jogo fechado: arraste um .save.txt sobre usar_save.bat; do_zero.bat = jogo como na primeira vez (marco 00); voltar.bat = devolve o seu save.",
            "Ordem minima (cerca de 1 h): 00 (do_zero.bat), 01_old_forge_completo, o ultimo charter e o tudo_comprado (ou fim_). Os outros charters sao opcionais.", "",
            "| save | relogio (h) | distrito ativo | Renome (total) | Charters | abertos | ouro | relido |", "|---|---|---|---|---|---|---|---|" }.Concat(linhas));
        return falhas == 0 ? 0 : 1;
    }

    static string Ouro(long g) => g >= 1_000_000 ? (g / 1e6).ToString("0.0", CultureInfo.InvariantCulture) + " mi" : g.ToString("N0", CultureInfo.InvariantCulture);
}
