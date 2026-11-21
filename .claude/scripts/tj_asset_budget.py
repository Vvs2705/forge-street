#!/usr/bin/env python3
"""Time de Jogos — orçamento de polígonos e texturas (sem dependências externas).

Uso CLI:
  python .claude/scripts/tj_asset_budget.py <arquivo|pasta> [...] [--perfil mobile|pc] [--json]
Uso como hook (PreToolUse em `git commit`):
  python .claude/scripts/tj_asset_budget.py --hook   (lê payload em stdin, checa só arquivos staged)

Veredito por arquivo: PASS | FAIL | NAO_AVALIADO (formato sem parser, ex.: .fbx sem Blender).
Exit: 0 = tudo PASS/NAO_AVALIADO · 1 = algum FAIL (CLI) · 2 = algum FAIL (hook: bloqueia o commit).

Categoria pelo prefixo do nome (convenção do time): char_ hero_ npc_ prop_ env_ wpn_ vfx_ ui_.
Orçamentos padrão podem ser sobrescritos em `asset-budgets.json` na raiz do repo
(ou `.claude/time-jogos/asset-budgets.json`).
"""
from __future__ import annotations

import json
import os
import re
import struct
import subprocess
import sys
from pathlib import Path

DEFAULT = {
    "mobile": {
        "tris": {"hero": 20000, "char": 12000, "npc": 8000, "wpn": 5000, "prop": 3000,
                 "env": 10000, "vfx": 500, "default": 10000},
        "tex": {"hero": 2048, "char": 1024, "default": 1024, "ui": 2048, "env": 2048},
    },
    "pc": {
        "tris": {"hero": 60000, "char": 40000, "npc": 25000, "wpn": 15000, "prop": 8000,
                 "env": 30000, "vfx": 2000, "default": 30000},
        "tex": {"hero": 4096, "char": 2048, "default": 2048, "ui": 4096, "env": 4096},
    },
}
MESH_EXT = {".glb", ".gltf", ".obj", ".fbx", ".blend"}
TEX_EXT = {".png", ".jpg", ".jpeg", ".tga"}

for _s in (sys.stdout, sys.stderr):  # Git Bash/cp1252: nunca quebrar por acento
    if hasattr(_s, "reconfigure"):
        _s.reconfigure(encoding="utf-8", errors="replace")


def category(name: str) -> str:
    m = re.match(r"^(hero|char|npc|wpn|prop|env|vfx|ui)[_\-]", name.lower())
    return m.group(1) if m else "default"


def load_budgets(root: Path, perfil: str) -> dict:
    b = json.loads(json.dumps(DEFAULT[perfil]))
    for p in (root / "asset-budgets.json", root / ".claude" / "time-jogos" / "asset-budgets.json"):
        if p.exists():
            try:
                over = json.loads(p.read_text(encoding="utf-8")).get(perfil, {})
                for k in ("tris", "tex"):
                    b[k].update(over.get(k, {}))
            except Exception as exc:
                print(f"[asset-budget] aviso: {p} ilegível ({exc}); usando padrão", file=sys.stderr)
    return b


def glb_tris(path: Path) -> int:
    data = path.read_bytes()
    if path.suffix.lower() == ".gltf":
        doc = json.loads(data.decode("utf-8"))
    else:
        magic, _ver, _len = struct.unpack_from("<4sII", data, 0)
        if magic != b"glTF":
            raise ValueError("não é GLB")
        clen, ctype = struct.unpack_from("<II", data, 12)
        if ctype != 0x4E4F534A:
            raise ValueError("primeiro chunk não é JSON")
        doc = json.loads(data[20:20 + clen].decode("utf-8"))
    acc = doc.get("accessors", [])
    # conta por instância de nó (malha usada 3x conta 3x)
    uses: dict[int, int] = {}
    for node in doc.get("nodes", []):
        if "mesh" in node:
            uses[node["mesh"]] = uses.get(node["mesh"], 0) + 1
    total = 0
    for mi, mesh in enumerate(doc.get("meshes", [])):
        n = 0
        for prim in mesh.get("primitives", []):
            mode = prim.get("mode", 4)
            if "indices" in prim:
                count = acc[prim["indices"]]["count"]
            else:
                count = acc[prim["attributes"]["POSITION"]]["count"]
            if mode == 4:
                n += count // 3
            elif mode in (5, 6):
                n += max(count - 2, 0)
        total += n * max(uses.get(mi, 1), 1)
    return total


def obj_tris(path: Path) -> int:
    n = 0
    with path.open("r", encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if line.startswith("f "):
                n += max(len(line.split()) - 3, 0)
    return n


def image_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as fh:
        head = fh.read(32)
        if head[:8] == b"\x89PNG\r\n\x1a\n":
            return struct.unpack(">II", head[16:24])
        if path.suffix.lower() == ".tga":
            return struct.unpack("<HH", head[12:16])
        fh.seek(0)
        data = fh.read()
    if data[:2] == b"\xff\xd8":
        i = 2
        while i < len(data):
            if data[i] != 0xFF:
                i += 1
                continue
            marker = data[i + 1]
            if marker in (0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF):
                h, w = struct.unpack(">HH", data[i + 5:i + 9])
                return w, h
            seg = struct.unpack(">H", data[i + 2:i + 4])[0]
            i += 2 + seg
    raise ValueError("formato de imagem não reconhecido")


def check(path: Path, budgets: dict) -> dict:
    ext = path.suffix.lower()
    cat = category(path.name)
    r = {"arquivo": str(path), "categoria": cat}
    try:
        if ext in (".glb", ".gltf"):
            tris, lim = glb_tris(path), budgets["tris"].get(cat, budgets["tris"]["default"])
        elif ext == ".obj":
            tris, lim = obj_tris(path), budgets["tris"].get(cat, budgets["tris"]["default"])
        elif ext in (".fbx", ".blend"):
            r.update(veredito="NAO_AVALIADO",
                     motivo="formato binário sem parser; exporte GLB ou rode tools/blender/budget_report.py")
            return r
        elif ext in TEX_EXT:
            w, h = image_size(path)
            lim = budgets["tex"].get(cat, budgets["tex"]["default"])
            r.update(medida=f"{w}x{h}", limite=f"{lim}px",
                     veredito="PASS" if max(w, h) <= lim else "FAIL")
            if (w & (w - 1)) or (h & (h - 1)):
                r["aviso"] = "dimensão não é potência de 2 (compressão ASTC/ETC2 menos eficiente)"
            return r
        else:
            r.update(veredito="NAO_AVALIADO", motivo="extensão fora do escopo")
            return r
        r.update(medida=f"{tris} tris", limite=f"{lim} tris", veredito="PASS" if tris <= lim else "FAIL")
    except Exception as exc:
        r.update(veredito="NAO_AVALIADO", motivo=f"erro ao ler: {exc}")
    return r


def collect(targets: list[str]) -> list[Path]:
    out = []
    for t in targets:
        p = Path(t)
        if p.is_dir():
            out += [q for q in p.rglob("*") if q.suffix.lower() in MESH_EXT | TEX_EXT]
        elif p.exists() and p.suffix.lower() in MESH_EXT | TEX_EXT:
            out.append(p)
    return out


def main(argv: list[str]) -> int:
    perfil = "mobile"
    if "--perfil" in argv:
        perfil = argv[argv.index("--perfil") + 1]
    hook = "--hook" in argv
    as_json = "--json" in argv
    root = Path.cwd()
    if hook:
        try:
            payload = json.loads(sys.stdin.buffer.read().decode("utf-8-sig") or "{}")
        except Exception:
            return 0
        cmd = (payload.get("tool_input") or {}).get("command", "") or ""
        if not re.search(r"\bgit\b[^\n]*\bcommit\b", cmd):
            return 0
        root = Path(payload.get("cwd") or os.getcwd())
        try:
            staged = subprocess.run(["git", "diff", "--cached", "--name-only", "--diff-filter=AM"], cwd=root,
                                    capture_output=True, text=True, timeout=10).stdout.splitlines()
        except Exception as exc:
            print(f"[asset-budget] NÃO AVALIADO: {exc}", file=sys.stderr)
            return 0
        files = [root / f for f in staged if Path(f).suffix.lower() in MESH_EXT | TEX_EXT
                 and not f.lower().startswith(("library/", "temp/"))]
        # Ignora texturas de UI/screenshots de evidência
        files = [f for f in files if "qa/evidence" not in f.as_posix()]
    else:
        targets = [a for a in argv if not a.startswith("--") and a not in ("mobile", "pc")]
        files = collect(targets or ["."])
    budgets = load_budgets(root, perfil)
    results = [check(f, budgets) for f in files if f.exists()]
    fails = [r for r in results if r["veredito"] == "FAIL"]
    if as_json:
        print(json.dumps({"perfil": perfil, "resultados": results}, ensure_ascii=False, indent=2))
    elif results and not hook:
        for r in results:
            print(f"{r['veredito']:13} {r.get('medida', '-'):>14} / {r.get('limite', '-'):<12} {r['arquivo']}"
                  + (f"  ({r.get('motivo') or r.get('aviso')})" if r.get('motivo') or r.get('aviso') else ""))
    if hook:
        na = [r for r in results if r["veredito"] == "NAO_AVALIADO"]
        if na:
            print("[asset-budget] NÃO AVALIADO: " + ", ".join(Path(r["arquivo"]).name for r in na), file=sys.stderr)
        if fails:
            print("[asset-budget] COMMIT BLOQUEADO — acima do orçamento (" + perfil + "):\n  - " +
                  "\n  - ".join(f"{Path(r['arquivo']).name}: {r['medida']} > {r['limite']}" for r in fails) +
                  "\nGere LOD/decimate (tools/blender/) ou ajuste asset-budgets.json com aprovação do technical-director.",
                  file=sys.stderr)
            return 2
        return 0
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
