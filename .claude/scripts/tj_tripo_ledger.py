#!/usr/bin/env python3
"""Time de Jogos — ledger de créditos do Tripo3D (docs/LEDGER_TRIPO.md).

CLI:
  python .claude/scripts/tj_tripo_ledger.py add --conta A --creditos 30 --task <id> --asset <caminho> --nota "texto"
  python .claude/scripts/tj_tripo_ledger.py saldo --conta A --inicial 600      (define saldo inicial conhecido)
  python .claude/scripts/tj_tripo_ledger.py resumo
Hook (PostToolUse, matcher mcp__.*tripo.*): python .claude/scripts/tj_tripo_ledger.py --hook
  Registra automaticamente toda chamada de ferramenta MCP com 'tripo' no nome, com créditos
  "?" quando a resposta não informar — nunca inventa número. Conta ativa lida de
  docs/LEDGER_TRIPO.md (linha `Conta ativa: X`).
Nunca grava login, senha, chave ou token: só conta (A/B), task id, créditos e asset.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from datetime import datetime
from pathlib import Path

HEADER = """# LEDGER Tripo3D

Regra: usar a **conta A** até acabar, depois a **conta B**. Saldo nas duas zerado → pedir ao usuário.
Nunca registrar e-mail, senha, chave ou token aqui.

Conta ativa: A
Saldo inicial A: ?
Saldo inicial B: ?

| Data/hora | Conta | Créditos | Task ID | Asset | Origem | Nota |
|---|---|---|---|---|---|---|
"""

for _s in (sys.stdout, sys.stderr):  # Git Bash/cp1252: nunca quebrar por acento
    if hasattr(_s, "reconfigure"):
        _s.reconfigure(encoding="utf-8", errors="replace")


def ledger_path() -> Path:
    here = Path.cwd()
    for base in (here, *here.parents):
        if (base / "docs").is_dir():
            return base / "docs" / "LEDGER_TRIPO.md"
    return here / "docs" / "LEDGER_TRIPO.md"


def ensure(p: Path) -> str:
    if not p.exists():
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(HEADER, encoding="utf-8")
    return p.read_text(encoding="utf-8")


def append(conta: str, creditos: str, task: str, asset: str, origem: str, nota: str) -> None:
    p = ledger_path()
    ensure(p)
    row = f"| {datetime.now():%Y-%m-%d %H:%M} | {conta} | {creditos} | {task or '-'} | {asset or '-'} | {origem} | {nota or ''} |\n"
    with p.open("a", encoding="utf-8") as fh:
        fh.write(row)


def active_account(text: str) -> str:
    m = re.search(r"^Conta ativa:\s*([AB])", text, re.M)
    return m.group(1) if m else "A"


def resumo() -> int:
    p = ledger_path()
    text = ensure(p)
    tot: dict[str, float] = {"A": 0.0, "B": 0.0}
    unknown = 0
    for line in text.splitlines():
        cells = [c.strip() for c in line.strip("|").split("|")]
        if len(cells) >= 3 and cells[1] in tot:
            try:
                tot[cells[1]] += float(cells[2])
            except ValueError:
                unknown += 1
    ini = {k: re.search(rf"^Saldo inicial {k}:\s*(\S+)", text, re.M) for k in "AB"}
    for k in "AB":
        i = ini[k].group(1) if ini[k] else "?"
        rest = f"{float(i) - tot[k]:.0f}" if i not in ("?", None) else "?"
        print(f"Conta {k}: gasto {tot[k]:.0f} · inicial {i} · restante {rest}")
    print(f"Conta ativa: {active_account(text)} · lançamentos sem crédito informado: {unknown}")
    return 0


def hook() -> int:
    try:
        p = json.loads(sys.stdin.buffer.read().decode("utf-8-sig") or "{}")
    except Exception:
        return 0
    name = p.get("tool_name", "")
    if "tripo" not in name.lower():
        return 0
    resp = json.dumps(p.get("tool_response", ""), ensure_ascii=False)
    task = re.search(r'"task_id"\s*:\s*"([^"]+)"', resp)
    cred = re.search(r'"(?:credits?|consumed_credits|cost)"\s*:\s*([0-9.]+)', resp)
    conta = active_account(ensure(ledger_path()))
    append(conta, cred.group(1) if cred else "?", task.group(1) if task else "", "", f"hook:{name}",
           "créditos não informados pela resposta" if not cred else "")
    return 0


def main() -> int:
    if "--hook" in sys.argv:
        return hook()
    ap = argparse.ArgumentParser()
    sub = ap.add_subparsers(dest="cmd", required=True)
    a = sub.add_parser("add")
    a.add_argument("--conta", choices=["A", "B"], required=True)
    a.add_argument("--creditos", required=True)
    a.add_argument("--task", default="")
    a.add_argument("--asset", default="")
    a.add_argument("--nota", default="")
    s = sub.add_parser("saldo")
    s.add_argument("--conta", choices=["A", "B"], required=True)
    s.add_argument("--inicial", required=True)
    c = sub.add_parser("ativa")
    c.add_argument("conta", choices=["A", "B"])
    sub.add_parser("resumo")
    args = ap.parse_args()
    if args.cmd == "add":
        append(args.conta, args.creditos, args.task, args.asset, "manual", args.nota)
        return resumo()
    p = ledger_path()
    text = ensure(p)
    if args.cmd == "saldo":
        text = re.sub(rf"^Saldo inicial {args.conta}:.*$", f"Saldo inicial {args.conta}: {args.inicial}", text, flags=re.M)
        p.write_text(text, encoding="utf-8")
        return resumo()
    if args.cmd == "ativa":
        p.write_text(re.sub(r"^Conta ativa:.*$", f"Conta ativa: {args.conta}", text, flags=re.M), encoding="utf-8")
        return resumo()
    return resumo()


if __name__ == "__main__":
    sys.exit(main())
