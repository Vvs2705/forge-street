#!/usr/bin/env python3
"""Gera docs/politica/index.html (pagina unica pt/en/es para hospedar) a partir dos 3 privacidade.*.md.

Uso: python3 client/tools/politica_html.py [--check]
  --check: sai 1 se o index.html do repo nao bate com o gerado (para rodar depois de mexer num .md).
Sem dependencia externa: so o subconjunto de Markdown que os 3 textos usam (titulos, listas, tabelas, negrito, codigo, citacao).
"""
import html, re, sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2] / "docs" / "politica"
LINGUAS = [("pt-BR", "Português", "privacidade.pt-BR.md"), ("en", "English", "privacidade.en.md"), ("es", "Español", "privacidade.es.md")]


def inline(t):
    t = html.escape(t, quote=False)
    t = re.sub(r"`([^`]+)`", r"<code>\1</code>", t)
    t = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", t)
    return t


def md(texto):
    out, lista, tabela = [], False, []

    def fecha():
        nonlocal lista, tabela
        if lista:
            out.append("</ul>"); lista = False
        if tabela:
            cab, *corpo = [l for l in tabela if not re.match(r"^\|[\s|:-]+\|$", l)]
            cel = lambda l: [c.strip() for c in l.strip().strip("|").split("|")]
            out.append("<table><thead><tr>" + "".join(f"<th>{inline(c)}</th>" for c in cel(cab)) + "</tr></thead><tbody>")
            out.extend(["<tr>" + "".join(f"<td>{inline(c)}</td>" for c in cel(l)) + "</tr>" for l in corpo])
            out.append("</tbody></table>"); tabela = []

    cit = []
    for l in texto.splitlines():
        if l.startswith(">"):
            cit.append(l.lstrip("> ").lstrip("#").strip()); continue
        if cit:
            out.append('<aside class="rascunho">' + "<br>".join(inline(c) for c in cit if c) + "</aside>"); cit = []
        if l.startswith("|"):
            if lista: fecha()
            tabela.append(l); continue
        if tabela: fecha()
        m = re.match(r"^(#{1,3}) (.*)", l)
        if m:
            fecha(); n = len(m.group(1)) + 1; out.append(f"<h{n}>{inline(m.group(2))}</h{n}>"); continue
        if l.startswith("- "):
            if not lista: out.append("<ul>"); lista = True
            out.append(f"<li>{inline(l[2:])}</li>"); continue
        fecha()
        if l.strip(): out.append(f"<p>{inline(l)}</p>")
    fecha()
    return "\n".join(out)


def gerar():
    abas = "".join(f'<a href="#{c}" lang="{c}">{n}</a>' for c, n, _ in LINGUAS)
    secoes = "\n".join(f'<section id="{c}" lang="{c}">\n{md((RAIZ / f).read_text(encoding="utf-8"))}\n</section>' for c, _, f in LINGUAS)
    return f"""<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex">
<title>Forge Street — Privacidade</title>
<!-- GERADO por client/tools/politica_html.py a partir de docs/politica/privacidade.*.md. Nao edite a mao. -->
<style>
:root {{ --fundo:#faf7f2; --texto:#2b2622; --fraco:#6b625a; --linha:#e3dcd2; --marca:#b5531c; --aviso:#fff1e0; }}
@media (prefers-color-scheme: dark) {{ :root {{ --fundo:#1d1a17; --texto:#ece6de; --fraco:#a99f94; --linha:#3a342e; --marca:#e98a4f; --aviso:#3a2a1a; }} }}
* {{ box-sizing:border-box; }}
body {{ margin:0; background:var(--fundo); color:var(--texto); font:16px/1.6 system-ui,-apple-system,"Segoe UI",Roboto,sans-serif; }}
nav {{ position:sticky; top:0; background:var(--fundo); border-bottom:1px solid var(--linha); padding:12px 16px; display:flex; gap:16px; }}
nav a {{ color:var(--marca); font-weight:600; text-decoration:none; }}
main {{ max-width:760px; margin:0 auto; padding:16px; }}
section, h2, h3 {{ scroll-margin-top:56px; }}
section {{ padding-bottom:48px; border-bottom:1px solid var(--linha); margin-bottom:32px; }}
h2 {{ font-size:1.6em; line-height:1.25; margin:24px 0 8px; }}
h3 {{ font-size:1.15em; margin:28px 0 8px; }}
table {{ border-collapse:collapse; width:100%; display:block; overflow-x:auto; }}
th, td {{ border:1px solid var(--linha); padding:8px; text-align:left; vertical-align:top; }}
code {{ font-size:.92em; }}
.rascunho {{ background:var(--aviso); border-left:4px solid var(--marca); padding:12px; margin:16px 0; }}
</style>
</head>
<body>
<nav>{abas}</nav>
<main>
{secoes}
</main>
</body>
</html>
"""


if __name__ == "__main__":
    alvo, novo = RAIZ / "index.html", gerar()
    if "--check" in sys.argv:
        ok = alvo.exists() and alvo.read_text(encoding="utf-8") == novo
        print("politica_html: OK" if ok else "politica_html: index.html desatualizado (rode sem --check)")
        sys.exit(0 if ok else 1)
    alvo.write_text(novo, encoding="utf-8", newline="\n")
    print(f"politica_html: {alvo} ({len(novo)} bytes)")
