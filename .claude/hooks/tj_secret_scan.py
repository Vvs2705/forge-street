#!/usr/bin/env python3
"""Time de Jogos — PreToolUse (Bash|PowerShell): bloqueia `git commit` com segredo staged.

Lê o payload do Claude Code em stdin. Se o comando contiver `git commit`, examina
`git diff --cached` (linhas adicionadas) e nomes de arquivos staged. Segredo
encontrado -> exit 2 (bloqueia) com o motivo em stderr.
Se não conseguir rodar o git, NÃO aprova em silêncio: avisa "NÃO AVALIADO" em
stderr e deixa passar (exit 0) — o hook de CI (gitleaks) é a segunda barreira.
"""
import json
import re
import subprocess
import sys

PATTERNS = [
    ("chave privada", re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH |DSA |PGP )?PRIVATE KEY-----")),  # tj-allow-secret
    ("token GitHub", re.compile(r"\bgh[pousr]_[A-Za-z0-9]{36,}\b")),
    ("chave AWS", re.compile(r"\bAKIA[0-9A-Z]{16}\b")),
    ("chave Google", re.compile(r"\bAIza[0-9A-Za-z_\-]{35}\b")),
    ("chave Anthropic/OpenAI", re.compile(r"\bsk-(?:ant-)?[A-Za-z0-9_\-]{32,}\b")),
    ("token Slack", re.compile(r"\bxox[baprs]-[A-Za-z0-9\-]{10,}\b")),
    ("JWT service role", re.compile(r"\beyJ[A-Za-z0-9_\-]{20,}\.eyJ[A-Za-z0-9_\-]{20,}\.[A-Za-z0-9_\-]{20,}")),
    ("licença Unity (.ulf)", re.compile(r"<DeveloperData Value=|<License id=\"Terms\"")),  # tj-allow-secret
    ("atribuição de segredo", re.compile(
        r"(?i)\b(?:api[_-]?key|secret|password|passwd|senha|token|private[_-]?key|tripo[_-]?key|unity[_-]?(?:password|serial))\b"
        r"\s*[:=]\s*['\"][^'\"\s]{12,}['\"]")),
]
FORBIDDEN_FILES = re.compile(r"(^|/)(\.env(\..*)?|.*\.ulf|.*\.pem|.*\.p12|.*\.keystore|.*\.jks|id_rsa.*|credentials\.json)$", re.I)
ALLOW_FILES = re.compile(r"(^|/)\.env\.example$|(^|/)\.env\.sample$", re.I)

for _s in (sys.stdout, sys.stderr):  # Git Bash/cp1252: nunca quebrar por acento
    if hasattr(_s, "reconfigure"):
        _s.reconfigure(encoding="utf-8", errors="replace")


def main() -> int:
    try:
        payload = json.loads(sys.stdin.buffer.read().decode("utf-8-sig") or "{}")
    except Exception:
        return 0
    cmd = (payload.get("tool_input") or {}).get("command", "") or ""
    if not re.search(r"\bgit\b[^\n]*\bcommit\b", cmd):
        return 0
    cwd = payload.get("cwd") or None
    try:
        names = subprocess.run(["git", "diff", "--cached", "--name-only"], cwd=cwd,
                               capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=10)
        diff = subprocess.run(["git", "diff", "--cached", "-U0", "--no-color"], cwd=cwd,
                              capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=10)
    except Exception as exc:  # git ausente/timeout
        print(f"[time-jogos] secret-scan NÃO AVALIADO: {exc}", file=sys.stderr)
        return 0
    if names.returncode != 0:
        print("[time-jogos] secret-scan NÃO AVALIADO: git diff falhou (fora de repo?)", file=sys.stderr)
        return 0
    problems = []
    for f in names.stdout.splitlines():
        if FORBIDDEN_FILES.search(f) and not ALLOW_FILES.search(f):
            problems.append(f"arquivo proibido staged: {f}")
    current = "?"
    for line in diff.stdout.splitlines():
        if line.startswith("+++ "):
            current = line[6:] if line.startswith("+++ b/") else line[4:]
            continue
        if not line.startswith("+") or line.startswith("+++"):
            continue
        if "tj-allow-secret" in line:  # falso positivo documentado na própria linha
            continue
        for label, rx in PATTERNS:
            if rx.search(line):
                problems.append(f"{label} em {current}")
                break
    if problems:
        uniq = sorted(set(problems))
        print("[time-jogos] COMMIT BLOQUEADO — possível segredo:\n  - " + "\n  - ".join(uniq[:15]) +
              "\nRemova do stage (git restore --staged <arq>), mova para variável de ambiente/secret do GitHub."
              "\nFalso positivo? adicione o comentário `tj-allow-secret` na linha.", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
