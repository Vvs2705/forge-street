"""Junta o pss.csv e as linhas SOAK do logcat do soak_emulador.sh em soak.csv e da o veredito (docs/SOAK.md).

uso: python soak_relatorio.py SAIDA        sai 1 se o PSS ou o heap mono crescerem > 10% depois do minuto 10, ou com excecao no logcat
"""

import re
import sys
from pathlib import Path

PKG = "br.com.vstack.forgestreet"
CRESCIMENTO = 0.10
AQUECIMENTO = 10


def crescimento(serie, janela=5):
    """Mediana dos ultimos `janela` minutos sobre a dos `janela` minutos logo depois do aquecimento (None = serie curta). Mediana de
    janela, e nao pico sobre um ponto: o heap mono sobe e desce com o GC (1 coleta a cada ~4 s), e o pico de 1 amostra reprovava um
    patamar estavel (1o soak: 24,6 -> pico 26,5 = +10,2%, com o fim em 25,8)."""
    depois = serie[AQUECIMENTO - 1 :]
    if len(depois) < 2 * janela:
        return None
    ini = sorted(depois[:janela])[janela // 2]
    fim = sorted(depois[-janela:])[janela // 2]
    return None if ini <= 0 else fim / ini - 1


def main(out):
    out = Path(out)
    pss = [
        float(linha.split(",")[1])
        for linha in (out / "pss.csv").read_text().splitlines()[1:]
    ]
    log = (out / "logcat.txt").read_text(encoding="utf-8", errors="replace")
    soak = [
        dict(kv.split("=") for kv in m.group(1).split())
        for m in re.finditer(r"SOAK (t=.*)", log)
    ]
    ls = log.splitlines()
    erros = [
        linha
        for i, linha in enumerate(ls)
        if re.search(
            r"Unity +: .*Exception|ANR in " + PKG + "|>>> " + PKG + " <<<", linha
        )
        or ("FATAL EXCEPTION" in linha and PKG in "".join(ls[i + 1 : i + 2]))
    ]
    linhas = ["min,pss_mb,fps,mono_mb,reservado_mb,gc0"]
    for i, p in enumerate(pss):
        s = soak[i] if i < len(soak) else {}
        linhas.append(
            f"{i + 1},{p},{s.get('fps', '')},{s.get('mono_mb', '')},{s.get('reservado_mb', '')},{s.get('gc0', '')}"
        )
    (out / "soak.csv").write_text("\n".join(linhas) + "\n")
    mono = [float(s["mono_mb"]) for s in soak]
    fps = [float(s["fps"]) for s in soak]
    gp, gm = crescimento(pss), crescimento(mono)
    sys.stdout.write(
        f"minutos: {len(pss)} | PSS {pss[0] if pss else 0:.0f} -> {pss[-1] if pss else 0:.0f} MB (pico {max(pss, default=0):.0f})"
        f" | mono {mono[0] if mono else 0:.1f} -> {mono[-1] if mono else 0:.1f} MB"
        f" | fps medio {sum(fps) / len(fps) if fps else 0:.1f} (min {min(fps, default=0):.1f})"
        f" | GCs {soak[-1]['gc0'] if soak else '?'} | excecoes {len(erros)}\n"
    )
    ruins = []
    if gp is None or gm is None:
        ruins.append(f"serie curta (< {AQUECIMENTO + 1} min) ou sem linhas SOAK")
    else:
        sys.stdout.write(
            f"crescimento depois do minuto {AQUECIMENTO}: PSS {gp:+.1%}, mono {gm:+.1%} (teto {CRESCIMENTO:.0%})\n"
        )
        if gp > CRESCIMENTO:
            ruins.append("PSS cresceu")
        if gm > CRESCIMENTO:
            ruins.append("heap mono cresceu")
    if erros:
        ruins.append("excecao no logcat: " + erros[0][:160])
    if (out / "morreu.txt").exists():
        ruins.append((out / "morreu.txt").read_text().strip())
    sys.stdout.write(("REPROVADO: " + "; ".join(ruins) if ruins else "APROVADO") + "\n")
    return 1 if ruins else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
