"""No Placeholder Gate (DM §178, GDD §164 item 21): o build de release não pode ter texto de debug, ícone temporário, material
genérico do Unity, item de teste, sprite de teste, som faltando nem arte de loja provisória.

Uso (da raiz do repo, depois do `client/tools/verify.sh full`, que deixa o client/Builds/win/ForgeStreet.exe):
    python client/tools/placeholder_gate.py            # roda o .exe COM gráfico (janela 540x960) e as conferências estáticas
    python client/tools/placeholder_gate.py --estatico # só as conferências estáticas (sem abrir o jogo)
Cada rodada abre o jogo num distrito ou num painel com -testsession -shot (nunca toca no save), e o Game loga ao sair a linha
"PLACEHOLDERS arte=N [...] som=M [...]": toda folha, arte de UI e textura de chão que caiu no procedural e todo efeito sem gravação.
Sai 1 se houver qualquer falta. Relatório em client/Builds/verify/placeholder_gate.txt.
"""

import argparse
import os
import re
import subprocess
import sys

CLIENT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
EXE = os.path.join(CLIENT, "Builds", "win", "ForgeStreet.exe")
OUT = os.path.join(CLIENT, "Builds", "verify")
RES = os.path.join(CLIENT, "Assets", "_FS", "Resources")
DISTRITOS = [
    "armorers_row",
    "artisan_quarter",
    "royal_arsenal",
    "arcane_foundry",
    "grand_forge",
]
# (nome, args): o mundo de cada distrito e os painéis que pedem ícones (com -buy 9: Ferramentas e metas abertas)
RODADAS = (
    [
        ("old_forge", []),
        ("heroi_estagio5", ["-renome", "2000"]),
    ]  # Renome 7: o herói no último estágio
    + [(d, ["-distrito", d]) for d in DISTRITOS]
    + [
        (p, ["-buy", "9", "-" + p] + extra)
        for p, extra in [
            ("metas", []),
            ("pass", []),
            ("eventocard", ["-evento", "ev_s1w2_forge_frenzy"]),
            ("hall", []),
            ("projetos", ["todos"]),
            ("mapa", []),
            ("settings", []),
            ("loja", ["-lojafalsa"]),
        ]
    ]
)
# texto que não pode estar no nome de um asset que vai para o build (Resources entra inteiro)
SUSPEITO = re.compile(
    r"(^|[_\-.])(test|teste|temp|tmp|placeholder|dummy|mock|debug)([_\-.]|$)", re.I
)
# o Default-Material embutido do Unity (guid fixo dos recursos internos)
MATERIAL_PADRAO = re.compile(r"fileID: 10303, guid: 0000000000000000f000000000000000")


def rodar(nome, args):
    log = os.path.join(OUT, "gate_" + nome + ".log")
    png = os.path.join(OUT, "gate_" + nome + ".png")
    cmd = [
        EXE,
        "-screen-fullscreen",
        "0",
        "-screen-width",
        "540",
        "-screen-height",
        "960",
        "-testsession",
        "-shot",
        png,
        "-shotdelay",
        "2",
        "-logFile",
        log,
    ] + args
    try:
        rc = subprocess.run(cmd, timeout=180).returncode
    except subprocess.TimeoutExpired:
        return nome, None, set(), set(), "timeout"
    texto = (
        open(log, encoding="utf-8", errors="replace").read()
        if os.path.exists(log)
        else ""
    )
    m = re.search(r"PLACEHOLDERS arte=\d+ \[([^\]]*)\] som=\d+ \[([^\]]*)\]", texto)
    if not m:
        return nome, rc, set(), set(), "sem a linha PLACEHOLDERS (ver " + log + ")"
    return nome, rc, set(m.group(1).split()), set(m.group(2).split()), None


def estatico():
    achados = []
    for raiz, _, arqs in os.walk(RES):
        for a in arqs:
            if not a.endswith(".meta") and SUSPEITO.search(os.path.splitext(a)[0]):
                achados.append(
                    "asset suspeito no build: "
                    + os.path.relpath(os.path.join(raiz, a), CLIENT)
                )
    for raiz, _, arqs in os.walk(os.path.join(CLIENT, "Assets", "_FS")):
        for a in arqs:
            if a.endswith((".unity", ".prefab", ".mat")):
                p = os.path.join(raiz, a)
                if MATERIAL_PADRAO.search(
                    open(p, encoding="utf-8", errors="replace").read()
                ):
                    achados.append(
                        "material padrão do Unity em " + os.path.relpath(p, CLIENT)
                    )
    return achados


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument(
        "--estatico", action="store_true", help="só as conferências estáticas"
    )
    a = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    linhas, arte, som, ruins = [], {}, set(), 0
    if not a.estatico:
        if not os.path.exists(EXE):
            print("falta " + EXE + ": rode client/tools/verify.sh full antes")
            return 2
        for nome, args in RODADAS:
            nome, rc, fa, fs, erro = rodar(nome, args)
            if erro or rc != 0:
                ruins += 1
                erro = erro or f"rc={rc}"
            for f in fa:
                arte.setdefault(f, []).append(nome)
            som |= fs
            linhas.append(
                f"{nome:16} rc={rc} arte={len(fa)} som={len(fs)}"
                + (f"  ERRO: {erro}" if erro else "")
            )
            print(linhas[-1], flush=True)
    est = estatico()
    rel = (
        ["# No Placeholder Gate", ""]
        + linhas
        + ["", f"## Arte no procedural ({len(arte)})"]
    )
    rel += [
        f"- {f}  (em: {', '.join(sorted(set(v)))})" for f, v in sorted(arte.items())
    ]
    rel += ["", f"## Efeitos sintetizados ({len(som)})"] + [
        f"- {s}" for s in sorted(som)
    ]
    rel += ["", f"## Estático ({len(est)})"] + [f"- {e}" for e in est]
    open(os.path.join(OUT, "placeholder_gate.txt"), "w", encoding="utf-8").write(
        "\n".join(rel) + "\n"
    )
    print("\n".join(rel[len(linhas) + 2 :]))
    total = len(arte) + len(som) + len(est) + ruins
    print(
        f"\nNo Placeholder Gate: {'OK' if total == 0 else 'FALHOU'} ({len(arte)} arte, {len(som)} som, {len(est)} estático, {ruins} rodada com erro)"
    )
    return 0 if total == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
