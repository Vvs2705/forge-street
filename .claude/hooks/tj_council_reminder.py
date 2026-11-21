#!/usr/bin/env python3
"""Time de Jogos — lembrete do Conselho.

UserPromptSubmit: se o pedido parece abrir uma feature/mecânica de jogo e não existe
ata do Conselho de hoje, injeta contexto pedindo para rodar /conselho ANTES de codar.
PreToolUse (Write|Edit): se o arquivo é código de jogo (Assets/**/*.cs, src/**) numa
branch feat/<slug> sem ata em docs/conselho/*<slug>*.md, avisa (não bloqueia).
Nunca bloqueia: é lembrete. Se não conseguir avaliar, fica em silêncio só quando
não há sinal de feature (não há o que lembrar).
"""
import json
import re
import subprocess
import sys
from datetime import date
from pathlib import Path

FEATURE_RX = re.compile(
    r"(?i)\b(nova feature|nova mec[aâ]nica|mec[aâ]nica|implement(a|e|ar)|adicion(a|e|ar)|cri(a|e|ar) (um|uma|o|a)|"
    r"quero que o jogo|feature|[ée]pico|sistema de|feiti[çc]o|combate|invent[aá]rio|multiplayer|loja)\b")
CODE_RX = re.compile(r"(^|/)(Assets/(?!.*(Editor|Tests)/).*\.cs|src/.*\.(ts|tsx|js|gd|cs))$", re.I)

for _s in (sys.stdout, sys.stderr):  # Git Bash/cp1252: nunca quebrar por acento
    if hasattr(_s, "reconfigure"):
        _s.reconfigure(encoding="utf-8", errors="replace")


def atas(root: Path) -> list[Path]:
    d = root / "docs" / "conselho"
    return list(d.glob("*.md")) if d.is_dir() else []


def emit_context(text: str) -> None:
    print(json.dumps({"hookSpecificOutput": {"hookEventName": "UserPromptSubmit", "additionalContext": text}},
                     ensure_ascii=False))


def main() -> int:
    try:
        p = json.loads(sys.stdin.buffer.read().decode("utf-8-sig") or "{}")
    except Exception:
        return 0
    root = Path(p.get("cwd") or ".")
    event = p.get("hook_event_name", "")
    if event == "UserPromptSubmit":
        prompt = p.get("prompt", "") or ""
        if prompt.strip().startswith("/") or not FEATURE_RX.search(prompt):
            return 0
        today = date.today().isoformat()
        if any(a.name.startswith(today) for a in atas(root)):
            return 0
        emit_context("[time-jogos] Pedido parece abrir feature/mecânica e não há ata do Conselho hoje em "
                     "docs/conselho/. Rode a skill /conselho com este pedido ANTES de editar código: ela debate, "
                     "decide (ADR) e já despacha as raias sem pausar. Ignore se for só pergunta/ajuste trivial.")
        return 0
    if event == "PreToolUse":
        fp = ((p.get("tool_input") or {}).get("file_path") or "").replace("\\", "/")
        if not fp or not CODE_RX.search(fp):
            return 0
        try:
            br = subprocess.run(["git", "branch", "--show-current"], cwd=root, capture_output=True,
                                text=True, timeout=5).stdout.strip()
        except Exception:
            return 0
        m = re.match(r"^(feat|feature)/(.+)$", br)
        if not m:
            return 0
        slug = m.group(2).lower()
        if any(slug in a.name.lower() for a in atas(root)):
            return 0
        print(f"[time-jogos] Branch {br} editando código sem ata em docs/conselho/*{slug}*.md — "
              f"rode /conselho (Modo A) para registrar decisão e critério de pronto.", file=sys.stderr)
        return 0
    return 0


if __name__ == "__main__":
    sys.exit(main())
