#!/usr/bin/env python3
"""Time de Jogos — portão de merge (substitui `gh pr merge --auto`).

Por quê: em repositório privado no plano gratuito do GitHub não há proteção de branch
(HTTP 403 "Upgrade to GitHub Pro"), então `gh pr merge --squash --auto` mergeia NA HORA,
mesmo com CI vermelho (defeito D11 da validação F6). Este script é o portão.

Uso: python .claude/scripts/tj_merge_gate.py <numero-do-pr> [--repo dono/nome] [--timeout-min 30] [--simular]

1. Espera os checks terminarem (`gh pr checks --watch`).
2. Lê o estado de cada check. Zero checks = NÃO AVALIADO → recusa o merge (CI que não rodou não é CI verde).
3. Algum check ≠ success/skipped → recusa (exit 1) e lista quais.
4. Tudo verde → `gh pr ready` (se draft) e `gh pr merge --squash --delete-branch`.
Exit: 0 mergeado · 1 recusado por check vermelho · 3 NÃO AVALIADO (sem checks / erro do gh).
"""
from __future__ import annotations

import json
import subprocess
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def gh(*args: str, timeout: int = 120) -> subprocess.CompletedProcess:
    return subprocess.run(["gh", *args], capture_output=True, text=True, encoding="utf-8",
                          errors="replace", timeout=timeout)


def main(argv: list[str]) -> int:
    pos = [a for a in argv if not a.startswith("--")]
    if not pos:
        print(__doc__)
        return 3
    pr = pos[0]
    repo = []
    if "--repo" in argv:
        repo = ["--repo", argv[argv.index("--repo") + 1]]
    tmin = int(argv[argv.index("--timeout-min") + 1]) if "--timeout-min" in argv else 30
    simular = "--simular" in argv
    try:
        gh("pr", "checks", pr, *repo, "--watch", "--interval", "20", timeout=tmin * 60)
    except subprocess.TimeoutExpired:
        print(f"NÃO AVALIADO: checks não terminaram em {tmin} min — merge recusado.")
        return 3
    r = gh("pr", "checks", pr, *repo, "--json", "name,state,bucket")
    if r.returncode not in (0, 8) or not r.stdout.strip():
        msg = (r.stderr or r.stdout).strip()
        print(f"NÃO AVALIADO: não consegui ler os checks ({msg}) — merge recusado.")
        return 3
    checks = json.loads(r.stdout)
    if not checks:
        print("NÃO AVALIADO: o PR não tem nenhum check (CI não rodou?) — merge recusado.")
        return 3
    bad = [c for c in checks if c.get("bucket") not in ("pass", "skipping")]
    for c in checks:
        print(f"  {c.get('bucket', '?'):8} {c.get('name')}")
    if bad:
        print(f"RECUSADO: {len(bad)} check(s) não verdes: " + ", ".join(c["name"] for c in bad))
        return 1
    if simular:
        print("SIMULAÇÃO: todos verdes — mergearia agora.")
        return 0
    view = gh("pr", "view", pr, *repo, "--json", "isDraft,state")
    info = json.loads(view.stdout or "{}")
    if info.get("state") == "MERGED":
        print("Já mergeado.")
        return 0
    if info.get("isDraft"):
        gh("pr", "ready", pr, *repo)
    m = gh("pr", "merge", pr, *repo, "--squash", "--delete-branch")
    print((m.stdout + m.stderr).strip())
    return 0 if m.returncode == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
