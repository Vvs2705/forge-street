"""Relatorio do playtest Camada 0 a partir de um ou mais diario.csv do Forge Street: funil, upgrades x bot e portoes.

Uso:
  python client/tools/diario_report.py                       # le o diario do build Windows deste PC
  python client/tools/diario_report.py pasta_ou_csv [...]    # um arquivo por testador (pasta = todos os *.csv dela)
  python client/tools/diario_report.py --autoteste           # prova o calculo e cada portao com dados sinteticos

Coleta (um arquivo = um testador; o nome do arquivo e' o testador, nunca junte dois aparelhos num arquivo):
  Android (build dev): adb pull /sdcard/Android/data/br.com.vstack.forgestreet/files/diario.csv playtest/testador01.csv
  Windows:             %USERPROFILE%/AppData/LocalLow/V-STACK/Forge Street/diario.csv

Colunas (Game.Log: sem cabecalho, virgula crua, sem aspas): utc, sessao, evento, t_jogo, a, b
  t_jogo = Sim.Time, segundos de jogo ACUMULADOS no save (atravessa sessoes; o tempo offline nao conta)
  session_start a=versao b=aparelho | first_sale a=s (repete em toda sessao depois da 1a venda: vale a 1a linha)
  upgrade_buy a=Upgrade (nome do enum) b=preco | product_crafted a=item b=estacao | product_sold a=item b=preco
  client_left a=item b=cansou|fila_cheia | bottleneck a=estacao b=saida_cheia | worker_hired a=indice
  station_unlock a=estacao | offline_claim a=ouro b=s | milestone, chest_opened a=bau b=ouro
  a cada 60 s de sessao: queue_length a=fila b=fila_max | bottleneck_seconds a=travada b=fome (s somados nas estacoes,
  ZERAM a cada sessao) | walk_no_decision a=s andando sem decisao (vai no save: acumulado) b=upgrades comprados
"""
import glob
import math
import os
import statistics
import sys
import tempfile
from collections import Counter, defaultdict

PADRAO = os.path.join(os.environ.get("USERPROFILE", ""), "AppData", "LocalLow", "V-STACK", "Forge Street", "diario.csv")

# (enum do upgrade_buy, nome na tela, bot humano m:ss) na ordem de compra do bot: docs/BALANCE.md sec.15.3 (Dt 1/30,
# vigente; sec.16 v0.4.1: producao completa igual, 43:37) e luxo sec.15.4 (14/18/22 mil, aplicado)
BOT = [
    ("FurnaceSpeed1", "Fole", "1:07"), ("Anvil2", "2a bigorna", "2:17"), ("Helper1", "Ajudante", "2:59"),
    ("Shields", "Escudos", "4:00"), ("PlayerCapacity", "Mochila", "5:27"), ("Helper2", "Ajudante 2", "6:52"),
    ("Conveyor", "Esteira", "7:20"), ("FurnaceSpeed2", "Fole duplo", "8:17"), ("PlayerSpeed", "Botas", "9:38"),
    ("Tools", "Ferramentas", "10:55"), ("CounterCapacity", "Vitrine", "12:40"), ("SideCorridor", "Corredor", "13:26"),
    ("Furnace2", "2a fornalha", "13:49"), ("Helper3", "Ajudante 3", "16:41"), ("HelperSpeed", "Ajudantes ageis", "20:20"),
    ("Jewelry", "Joalheria", "23:01"), ("HammerSpeed", "Martelo veloz", "25:21"), ("Jeweler", "Joalheiro", "27:10"),
    ("Miner", "Mineiro", "30:04"), ("Jeweler2", "Joalheiro 2", "32:16"), ("JewelSpeed", "Lupa", "36:50"),
    ("JewelVitrine", "Vitrine de joias", "43:37"), ("WorkshopFacade", "Fachada nobre", "52:20"),
    ("WorkshopFloor", "Piso de oficina", "63:22"), ("JewelryDecor", "Joalheria real", "76:56"),
]
BOT_VENDA1 = "0:22"                                   # 1a venda do bot humano, BALANCE sec.15.2
GDD3 = {"FurnaceSpeed1": "2:30", "Anvil2": "3:30", "Helper1": "4:30", "Shields": "5:30", "Conveyor": "8:30"}  # fim da janela, GDD sec.3 (= BALANCE sec.15.2)
FATOR = (1.3, 2.0)                                    # HIPOTESE do BALANCE sec.2: pessoa 1,3-2x mais lenta que o bot humano
NUM = {"first_sale": "a", "walk_no_decision": "a", "bottleneck_seconds": "ab"}   # campos numericos que o relatorio usa


def seg(mmss):
    m, s = mmss.split(":")
    return int(m) * 60 + int(s)


def mmss(s):
    return "-" if s is None else f"{int(s) // 60}:{int(s) % 60:02d}"


def mediana(v):
    return statistics.median(v) if v else None


def pct(cond, pool):
    pool = list(pool)
    return 100 * sum(1 for x in pool if cond(x)) / len(pool) if pool else None


def parse(bruta):
    """Linha do diario -> (sessao, evento, t_jogo, a, b) ou None. O jogo grava best-effort: linha cortada, colada na
    seguinte, campo a mais/menos ou numero podre e' lixo contado pelo chamador, nunca excecao."""
    r = bruta.split(",")
    if len(r) != 6 or not r[1] or not r[2]:
        return None
    try:
        t = float(r[3])
        a, b = (float(x) if k in NUM.get(r[2], "") else x for k, x in zip("ab", r[4:]))
    except ValueError:
        return None
    if not all(math.isfinite(x) for x in (t, a, b) if isinstance(x, float)):
        return None
    return r[1], r[2], t, a, b


def ler(caminhos):
    """-> (linhas, ignoradas). linha = (testador, sessao, evento, t_jogo, a, b); testador = caminho do arquivo."""
    linhas, ruins = [], 0
    for c in caminhos:
        for arq in sorted(glob.glob(os.path.join(c, "*.csv"))) if os.path.isdir(c) else [c]:
            with open(arq, encoding="utf-8", errors="replace") as f:
                texto = f.read().replace("\0", "")   # escrita cortada (bateria, kill) pode deixar NUL no fim
            for bruta in texto.splitlines():
                if bruta.strip():
                    p = parse(bruta)
                    if p:
                        linhas.append((arq,) + p)
                    else:
                        ruins += 1
    return linhas, ruins


def analisar(linhas):
    tst = defaultdict(lambda: {"venda1": None, "upg": {}, "walk": [], "fim": 0.0, "offline": False})
    ses = defaultdict(lambda: {"t0": math.inf, "t1": 0.0, "gargalo": []})
    saiu, travou = Counter(), Counter()
    for arq, sid, ev, t, a, b in linhas:
        T, S = tst[arq], ses[(arq, sid)]
        S["t0"], S["t1"], T["fim"] = min(S["t0"], t), max(S["t1"], t), max(T["fim"], t)
        if ev == "first_sale" and T["venda1"] is None:
            T["venda1"] = a
        elif ev == "upgrade_buy":
            T["upg"].setdefault(a, t)   # arquivo e' append-only: a 1a compra e' a que vale
        elif ev == "walk_no_decision":
            T["walk"].append((t, a))
        elif ev == "bottleneck_seconds":
            S["gargalo"].append((t, a, b))
        elif ev == "client_left":
            saiu[(b, a)] += 1
        elif ev == "bottleneck":
            travou[a] += 1
        elif ev == "offline_claim":
            T["offline"] = True

    # Por minuto de jogo. walk e t_jogo sao acumulados no save, entao a razao entre duas amostras vale ate atravessando
    # sessoes; o save a cada 5 s pode voltar alguns segundos no reload (amostra com delta negativo e' pulada).
    # travada/fome zeram a cada sessao: o delta comeca no t_jogo de entrada da sessao.
    minuto = defaultdict(lambda: {"andar": [], "travada": [], "fome": []})

    def faixa(t):
        return max(1, min(15, round(t / 60)))   # ponytail: 15 = "15+"; abra mais faixas se o playtest passar de 15 min

    for T in tst.values():
        pt, pw = 0.0, 0.0
        for t, w in sorted(T["walk"]):
            if t - pt >= 1 and w >= pw:
                minuto[faixa(t)]["andar"].append(100 * (w - pw) / (t - pt))
            pt, pw = t, w
    for S in ses.values():
        pt, ps, pf = S["t0"], 0.0, 0.0
        for t, st, fo in S["gargalo"]:
            if t - pt >= 1 and st >= ps and fo >= pf:
                minuto[faixa(t)]["travada"].append(60 * (st - ps) / (t - pt))
                minuto[faixa(t)]["fome"].append(60 * (fo - pf) / (t - pt))
            pt, ps, pf = t, st, fo
    return {"tst": tst, "ses": ses, "saiu": saiu, "travou": travou, "minuto": minuto}


def humanos(tst, k):
    return [T["upg"][k] for T in tst.values() if k in T["upg"]]


def portoes(r):
    """-> [(chave, status, texto)]. status: PASSA | FALHA | SEM DADOS | MANUAL."""
    tst = list(r["tst"].values())
    out = []

    def g(chave, v, passa, texto, fonte):
        st = "SEM DADOS" if v is None else "PASSA" if passa(v) else "FALHA"
        out.append((chave, st, f"{texto}: {'-' if v is None else f'{v:.0f}%'} (n={len(tst)})  ({fonte})"))

    g("venda1", pct(lambda T: T["venda1"] is not None and T["venda1"] < 90, tst), lambda v: v >= 90,
      "1a venda < 90 s para >= 90% (quem nunca vendeu conta como falha)", "GDD sec.15")
    g("escudos", pct(lambda T: "Shields" in T["upg"], tst), lambda v: v >= 70,
      "compram a 2a linha (Escudos) >= 70%", "README Proximos passos 2; o 'sem dica' nao sai do diario")
    g("andar", mediana([100 * w / t for t, w in (max(T["walk"]) for T in tst if T["walk"]) if t > 0]), lambda v: v < 50,
      "andar sem decisao < 50% do tempo de jogo (mediana por testador)",
      "GDD sec.26 CANCELAR 'andar entre pilhas sem decisao'; limiar do portao do bot, BALANCE sec.4")

    # sec.3: mediana humana dentro da janela <=> >= 50% compram ate o fim dela. Quem nao comprou conta como atrasado, desde
    # que tenha jogado ate o fim da janela (quem saiu antes nao e' julgado).
    p3 = {k: pct(lambda T: T["upg"].get(k, math.inf) <= seg(fim), [T for T in tst if k in T["upg"] or T["fim"] >= seg(fim)])
          for k, fim in GDD3.items()}
    nomes = {k: n for k, n, _ in BOT}
    det = ", ".join(f"{nomes[k]} {GDD3[k]} {'-' if v is None else f'{v:.0f}%'}" for k, v in p3.items())
    com = [v for v in p3.values() if v is not None]
    st = "SEM DADOS" if not com else "PASSA" if min(com) >= 50 else "FALHA"
    out.append(("gdd3", st, f">= 50% compram ate o fim da janela: {det}  (GDD sec.3)"))

    # ponytail: fator so com marcos do bot <= 10:00 (a janela que todo testador joga); depois disso so os rapidos chegam
    # e a mediana de quem comprou puxaria o fator para baixo.
    v1 = mediana([T["venda1"] for T in tst if T["venda1"] is not None])
    raz = ([v1 / seg(BOT_VENDA1)] if v1 is not None else []) + \
          [mediana(humanos(r["tst"], k)) / seg(b) for k, _, b in BOT if seg(b) <= 600 and humanos(r["tst"], k)]
    f = mediana(raz)
    st = "SEM DADOS" if f is None else "PASSA" if FATOR[0] <= f <= FATOR[1] else "FALHA"
    lado = "" if st != "FALHA" else " (pessoa mais lenta que 2x)" if f > FATOR[1] else " (pessoa mais rapida que 1,3x)"
    out.append(("fator", st, f"HIPOTESE pessoa 1,3-2x mais lenta que o bot (mediana das razoes, marcos do bot <= 10:00): "
                             f"{'-' if f is None else f'{f:.2f}x'}{lado}  (BALANCE sec.2)"))
    out.append(("gargalo", "MANUAL", ">= 50% dizem qual estacao era o gargalo: perguntar ao testador "
                                     "(README Proximos passos 2; GDD sec.26 CONTINUAR 'identificam gargalos')"))
    return out


def relatorio(linhas, ruins=0):
    r = analisar(linhas)
    tst, ses = r["tst"], r["ses"]
    n = len(tst)
    durs = [S["t1"] - S["t0"] for S in ses.values()]
    jogo = sum(durs)
    s = ["Forge Street - playtest Camada 0", f"testadores (arquivos): {n} | sessoes: {len(ses)} | "
         f"linhas ignoradas (truncadas/malformadas): {ruins}", ""]

    v1 = [T["venda1"] for T in tst.values() if T["venda1"] is not None]
    m1 = mediana(v1)
    s += [f"1a VENDA (first_sale; bot humano {BOT_VENDA1}, BALANCE sec.15.2)",
          f"  venderam {len(v1)}/{n} | mediana {mmss(m1)} | < 90 s: {sum(1 for v in v1 if v < 90)}/{n}"
          f" | humano/bot {'-' if m1 is None else f'{m1 / seg(BOT_VENDA1):.2f}x'}", ""]

    s += ["UPGRADES (upgrade_buy no t_jogo acumulado; bot humano BALANCE sec.15.3/sec.15.4; n = testadores que compraram)",
          f"  {'upgrade':<18} {'n':>7} {'humano':>7} {'bot':>6} {'h/bot':>6} {'GDD sec.3':>9}"]
    for k, nome, b in BOT:
        h = humanos(tst, k)
        if h or k in GDD3:
            m = mediana(h)
            s.append(f"  {nome:<18} {f'{len(h)}/{n}':>7} {mmss(m):>7} {b:>6} "
                     f"{'-' if m is None else f'{m / seg(b):.2f}x':>6} {GDD3.get(k, ''):>9}")
    s.append("  (upgrade fora da tabela = ninguem comprou)")

    s += ["", "CLIENTES QUE SAIRAM (client_left; bot humano 10 min: cansou 1, fila_cheia 65 - BALANCE sec.15.2)"]
    for motivo in ("cansou", "fila_cheia"):
        itens = {i: c for (m, i), c in r["saiu"].items() if m == motivo}
        tot = sum(itens.values())
        por10 = f"{tot / (jogo / 600):.1f}" if jogo > 0 else "-"
        s.append(f"  {motivo:<10} {tot:>4} ({por10} por 10 min de jogo) "
                 + ", ".join(f"{i} {c}" for i, c in sorted(itens.items(), key=lambda x: -x[1])))

    s += ["", "POR MINUTO DE JOGO (mediana das amostras de 60 s; travada/fome = s parados por minuto, somados nas estacoes)",
          f"  {'min':>4} {'amostras':>8} {'andar s/ decisao':>17} {'travada s':>10} {'fome s':>7}"]
    fmt = lambda v, u="": "-" if v is None else f"{v:.0f}{u}"
    for m in sorted(r["minuto"]):
        d = r["minuto"][m]
        s.append(f"  {str(m) if m < 15 else '15+':>4} {max(len(d['andar']), len(d['travada'])):>8} "
                 f"{fmt(mediana(d['andar']), '%'):>17} {fmt(mediana(d['travada'])):>10} {fmt(mediana(d['fome'])):>7}")
    s.append("  travou com saida cheia (evento bottleneck): "
             + (", ".join(f"{e} {c}" for e, c in r["travou"].most_common()) or "-"))

    s += ["", "SESSOES (duracao em t_jogo; GDD sec.2: 3-12 min)",
          f"  mediana {mmss(mediana(durs))} | min {mmss(min(durs) if durs else None)} | max {mmss(max(durs) if durs else None)}"
          f" | jogo total {mmss(jogo)}",
          f"  voltaram e pegaram o cofre (offline_claim): {sum(1 for T in tst.values() if T['offline'])}/{n} testadores"
          " (GDD sec.26 ITERAR 'pouca volta offline')", "", "PORTOES"]
    s += [f"  [{st}] {texto}" for _, st, texto in portoes(r)]
    s.append("  nao medidos aqui: tutorial (sem evento), D1/D7 e sessoes/dia (utc existe; Camada 0 nao mede retencao), "
             "rewarded, CPI, payer, crash-free (GDD sec.15/sec.26)")
    return "\n".join(s)


def autoteste():
    def jogador(venda1, compras, andar, minutos, sessoes=1):
        """Linhas CSV de um testador: walk acumulado no save; travada 5 s/min e fome 10 s/min, que zeram por sessao."""
        out, t, w = [], 0.0, 0.0
        por = minutos * 60 / sessoes
        for k in range(sessoes):
            sid, t0 = f"s{k}", t
            ln = lambda ev, tt, a="", b="": out.append(f"2026-10-07T10:00:00,{sid},{ev},{tt:.1f},{a},{b}")
            ln("session_start", t, "0.4.1", "POCO F4")
            for u, tu in compras.items():
                if t0 <= tu < t0 + por:
                    ln("upgrade_buy", tu, u, "100")
            if venda1 is not None and t0 + por > venda1:
                ln("first_sale", max(t, venda1), f"{venda1:.1f}")   # o jogo repete em toda sessao depois da 1a venda
            ln("client_left", t + 5, "espada", "cansou")
            ln("client_left", t + 6, "escudo", "fila_cheia")
            ln("bottleneck", t + 7, "Fornalha", "saida_cheia")
            for m in range(1, int(por // 60) + 1):
                ln("walk_no_decision", t0 + 60 * m, f"{w + andar * 60 * m:.0f}", "3")
                ln("bottleneck_seconds", t0 + 60 * m, f"{5 * m}", f"{10 * m}")
            w, t = w + andar * por, t0 + por
            if k:
                ln("offline_claim", t0, "50", "600")
        return out

    rapido = lambda: jogador(40, {"FurnaceSpeed1": 100, "Anvil2": 180, "Helper1": 250, "Shields": 300, "Conveyor": 480}, 0.2, 10)
    lento = lambda: jogador(120, {"FurnaceSpeed1": 200, "Anvil2": 400}, 0.6, 9)

    with tempfile.TemporaryDirectory() as d:
        def rodar(arquivos, lixo=()):
            for f in glob.glob(os.path.join(d, "*.csv")):
                os.remove(f)
            for i, ls in enumerate(arquivos):
                with open(os.path.join(d, f"t{i:02d}.csv"), "w", encoding="utf-8", newline="\n") as f:
                    f.write("\n".join(ls + (list(lixo) if i == 0 else [])) + "\n")
            linhas, ruins = ler([d])
            r = analisar(linhas)
            return r, {c: st for c, st, _ in portoes(r)}, relatorio(linhas, ruins), ruins

        # 10 rapidos: tudo PASSA; fator = mediana(40/22, 100/67, 180/137, 250/179, 300/240, 480/440) = 1,36x
        lixo = ["2026-10-07T10:00:00,s0,upgrade_bu",                       # cortada no meio
                "2026-10-07T10:00:00,s0,first_sale,abc,5.0,",              # t_jogo podre
                "2026-10-07T10:00:00,s0,first_sale,9.0,nan,",              # numero nao finito
                "2026-10-07T10:00:00,s0,product_sold,\0\0\0\0",            # NUL de escrita cortada
                "2026-10-07T10:00:00,s0,walk_no_decision,60.0,1,3,2026-10-07T10:00:01,s0,product_sold,61.0,espada,10"]  # colada
        r, p, texto, ruins = rodar([rapido() for _ in range(10)], lixo)
        assert ruins == 5, ruins
        assert p == {"venda1": "PASSA", "escudos": "PASSA", "andar": "PASSA", "gdd3": "PASSA", "fator": "PASSA", "gargalo": "MANUAL"}, p
        assert "1.36x" in texto and "andar sem decisao < 50% do tempo de jogo (mediana por testador): 20%" in texto
        assert r["minuto"][3]["travada"] == [5.0] * 10 and r["minuto"][3]["fome"] == [10.0] * 10
        assert mediana(r["minuto"][5]["andar"]) == 20.0

        # 2 sessoes por testador: first_sale repete (conta 1x), travada/fome zeram por sessao (delta continua 5/10),
        # walk acumulado atravessa a sessao (continua 20%), offline_claim aparece
        r, p, texto, _ = rodar([jogador(40, {"Shields": 300}, 0.2, 10, sessoes=2) for _ in range(3)])
        assert len(r["ses"]) == 6 and all(T["venda1"] == 40.0 for T in r["tst"].values())
        assert all(v == 5.0 for m in r["minuto"].values() for v in m["travada"])
        assert all(abs(v - 20) < 1e-6 for m in r["minuto"].values() for v in m["andar"])
        assert "cofre (offline_claim): 3/3" in texto and "mediana 5:00" in texto

        # 10 lentos: tudo FALHA; fator = mediana(120/22, 200/67, 400/137) = 2,99x
        r, p, texto, _ = rodar([lento() for _ in range(10)])
        assert p == {"venda1": "FALHA", "escudos": "FALHA", "andar": "FALHA", "gdd3": "FALHA", "fator": "FALHA", "gargalo": "MANUAL"}, p
        assert "2.99x (pessoa mais lenta que 2x)" in texto

        # fronteiras: 9 rapidos + 1 lento = 90% vende < 90 s (PASSA); 7 + 3 = 70% escudos (PASSA) e 70% venda (FALHA)
        p = rodar([rapido() for _ in range(9)] + [lento()])[1]
        assert p["venda1"] == "PASSA" and p["escudos"] == "PASSA"
        p = rodar([rapido() for _ in range(7)] + [lento() for _ in range(3)])[1]
        assert p["venda1"] == "FALHA" and p["escudos"] == "PASSA"
        p = rodar([rapido() for _ in range(6)] + [lento() for _ in range(4)])[1]
        assert p["escudos"] == "FALHA"

        # sec.3: quem nao comprou mas jogou ate o fim da janela conta como atrasado; quem saiu antes nao e' julgado
        p = rodar([rapido() for _ in range(2)] + [jogador(40, {}, 0.2, 3) for _ in range(3)])[1]
        assert p["gdd3"] == "FALHA"   # jogaram 3:00 sem Fole: Fole 2:30 = 2 de 5 (40%)
        p = rodar([rapido() for _ in range(2)] + [jogador(40, {}, 0.2, 2) for _ in range(3)])[1]
        assert p["gdd3"] == "PASSA"   # jogaram 2:00: nenhuma janela do sec.3 terminou para eles

        # vazio: SEM DADOS, nunca excecao
        p = rodar([])[1]
        assert p == {"venda1": "SEM DADOS", "escudos": "SEM DADOS", "andar": "SEM DADOS", "gdd3": "SEM DADOS",
                     "fator": "SEM DADOS", "gargalo": "MANUAL"}, p

        print(rodar([rapido() for _ in range(7)] + [lento() for _ in range(3)])[2])
    print("\nautoteste OK")


if __name__ == "__main__":
    sys.stdout.reconfigure(errors="replace")   # nomes do jogo tem acento; console cp1252 nao derruba o relatorio
    args = sys.argv[1:]
    if args == ["--autoteste"]:
        autoteste()
        sys.exit(0)
    caminhos = args or [PADRAO]
    faltando = [c for c in caminhos if not os.path.exists(c)]
    if faltando:
        print("nao encontrei:", ", ".join(faltando))
        sys.exit(2)
    print(relatorio(*ler(caminhos)))
