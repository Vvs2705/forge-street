#!/usr/bin/env python3
"""Checa o catalogo de conteudo da v1.0 (docs/CONTEUDO_V1.md, §16). Python stdlib.

Le as tabelas marcadas com `<!-- tabela: nome -->` e confere: IDs unicos; toda receita usa itens que alguem produz
(no mesmo distrito, ou importados no Grand Forge); contagens minimas; Blueprint essencial com fonte gratis permanente;
Masterwork sem condicao de anuncio/compra e com projeto/receita existentes; nomes en/es <= 16 caracteres.

    python client/tools/conteudo_check.py [caminho.md]      # sai 1 com a lista de erros
    python client/tools/conteudo_check.py --selftest        # prova vermelha: quebra uma copia e exige os erros
"""
import copy
import os
import re
import sys

DOC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "docs", "CONTEUDO_V1.md")
FREE = {"OrderBlueprint", "OrderRoyal", "Milestone", "Reputation", "Achievement", "Contract", "PassFree"}
SOURCES = FREE | {"Event", "Shop", "PassPremium"}          # Event: extra, nunca a unica de um essencial
CONDS = {"Counter", "StationLevel", "ReputationTier", "OrdersOfType", "OrdersOfProduct", "WorkerLevel", "AlloyPieces",
         "RichClients", "GemsCut", "QualitySold", "QuotaBlocks", "QuotaOnTime", "ContractsDone", "EnergizedPieces",
         "StableStreak", "ImportedUnits", "ExportLots", "DistrictCharters"}
PAGO = re.compile(r"an[uú]ncio|rewarded|\bads?\b|compra|iap|\bgems?\b|pago|loja|shop|premium", re.I)
PREFIXOS = ("rec_", "wk_", "ord_", "ctr_", "bp_", "mw_", "cli_", "col_")
ID_OK = re.compile(r"^[a-z0-9_]+$")


def parse(text):
    """{nome: [linha como dict cabecalho -> celula]}; tabelas com o mesmo nome se somam (ex.: upgrades por distrito)."""
    tabs, lines, i = {}, text.splitlines(), 0
    while i < len(lines):
        m = re.match(r"<!-- tabela: (\w+) -->", lines[i].strip())
        if not m:
            i += 1
            continue
        cells = lambda s: [c.strip().strip("`").strip() for c in s.strip().strip("|").split("|")]
        head = cells(lines[i + 1])
        rows = tabs.setdefault(m.group(1), [])
        i += 3                                             # marcador, cabecalho, separador
        while i < len(lines) and lines[i].startswith("|"):
            rows.append(dict(zip(head, cells(lines[i]))))
            i += 1
    return tabs


def check(t):
    e = []
    need = ["distritos", "itens", "receitas", "importacoes", "estacoes", "upgrades", "workers", "orders", "contratos",
            "blueprints", "masterworks", "colecao", "renome", "clientes", "fregueses"]
    for n in need:
        if not t.get(n):
            e.append(f"tabela '{n}' ausente ou vazia")
    if e:
        return e
    dnum = {r["id"]: int(r["#"]) for r in t["distritos"]}
    nums = lambda s: {int(x) for x in s.split(",") if x.strip().isdigit()}

    # 1. IDs: formato, unicos no tipo e entre os tipos com prefixo
    vistos = {}
    for tab in ["distritos", "itens", "receitas", "estacoes", "upgrades", "niveis", "workers", "orders", "contratos",
                "blueprints", "masterworks", "colecao", "clientes", "fregueses"]:
        ids = set()
        for r in t.get(tab, []):
            i = r["id"]
            if not ID_OK.match(i):
                e.append(f"{tab}: id '{i}' fora de [a-z0-9_]")
            if i in ids:
                e.append(f"{tab}: id '{i}' repetido")
            ids.add(i)
            if i.startswith(PREFIXOS):
                if i in vistos and vistos[i] != tab:
                    e.append(f"id '{i}' em {vistos[i]} e em {tab}")
                vistos[i] = tab

    # 2. receitas: todo insumo tem deposito ou receita no mesmo distrito (Grand Forge: ou e' importado)
    item = {r["id"]: r for r in t["itens"]}
    rec_out = {}
    for r in t["receitas"]:
        rec_out.setdefault(r["saída"], []).append(r)
    imp = {r["item"]: dnum.get(r["origem"]) for r in t["importacoes"]}
    gf = dnum.get("grand_forge")

    def produz(it, d):
        if it not in item:
            return False
        if item[it]["tipo"] == "Raw":
            return d in nums(item[it]["distritos"])
        return any(d in nums(r["distritos"]) for r in rec_out.get(it, []))

    for r in t["receitas"]:
        if r["saída"] not in item:
            e.append(f"{r['id']}: saída '{r['saída']}' não é item")
        for d in nums(r["distritos"]):
            for parte in r["insumos"].split("+"):
                it = parte.strip().split("×")[0].strip()
                if it not in item:
                    e.append(f"{r['id']}: insumo '{it}' não é item")
                elif not produz(it, d):
                    if d == gf and it in imp and imp[it] and produz(it, imp[it]):
                        continue
                    e.append(f"{r['id']}: insumo '{it}' ninguém produz no distrito {d}")
    for i, r in item.items():
        if r["tipo"] != "Raw" and i not in rec_out:
            e.append(f"item '{i}' ({r['tipo']}) sem receita")
    for i, o in imp.items():
        if not o or not produz(i, o):
            e.append(f"importação '{i}': não é produzido na origem")

    # 3. contagens minimas
    def faixa(nome, n, lo, hi=None):
        if n < lo or (hi is not None and n > hi):
            e.append(f"contagem: {nome} = {n} (pede {lo}{'–' + str(hi) if hi else '+'})")
    prods = [r for r in t["itens"] if r["tipo"] == "Product"]
    faixa("vendáveis", len(prods), 35, 45)
    faixa("workers", len(t["workers"]), 20, 30)
    faixa("encomendas", len(t["orders"]), 50)
    faixa("Royal Contracts", len(t["contratos"]), 12, 12)
    faixa("Blueprints", len(t["blueprints"]), 60)
    faixa("Masterworks", len(t["masterworks"]), 60, 60)
    for d in dnum:
        faixa(f"Masterworks de {d}", sum(1 for r in t["masterworks"] if r["distrito"] == d), 10, 10)

    # 4. Blueprints: fontes conhecidas; essencial com fonte gratis permanente
    bps = {r["id"] for r in t["blueprints"]}
    for r in t["blueprints"]:
        tipos = [s.strip().split(":")[0].strip() for s in r["fontes"].split(";") if s.strip()]
        for tp in tipos:
            if tp not in SOURCES:
                e.append(f"{r['id']}: fonte desconhecida '{tp}'")
        if "sim" in r["essencial"] and not any(tp in FREE for tp in tipos):
            e.append(f"{r['id']}: essencial sem fonte grátis permanente ({r['fontes']})")

    # 5. Masterworks: condicao sem anuncio/compra, tipos conhecidos, referencias existem
    recs = {r["id"] for r in t["receitas"]}
    for r in t["masterworks"]:
        c = r["condição especial"]
        if PAGO.search(c):
            e.append(f"{r['id']}: condição com anúncio/compra ({c})")
        for parte in c.split(";"):
            tp = re.split(r"[:>]", parte.strip())[0]
            if parte.strip() not in ("—", "") and tp not in CONDS:
                e.append(f"{r['id']}: condição desconhecida '{tp}'")
        if r["projeto"] not in bps:
            e.append(f"{r['id']}: projeto '{r['projeto']}' não é Blueprint")
        if r["receita"] not in recs:
            e.append(f"{r['id']}: receita '{r['receita']}' não existe")
        if r["distrito"] not in dnum:
            e.append(f"{r['id']}: distrito '{r['distrito']}' não existe")

    # 6. referencias de encomendas, contratos e fregueses
    for r in t["orders"]:
        p = r["produto"]
        if p != "rodízio das linhas" and p not in item:
            e.append(f"{r['id']}: produto '{p}' não é item")
    for r in t["contratos"]:
        for parte in r["pede"].split("+"):
            it = parte.strip().split("×")[0].strip()
            if it not in item:
                e.append(f"{r['id']}: pede '{it}' que não é item")
        if r["Blueprint"] not in bps:
            e.append(f"{r['id']}: Blueprint '{r['Blueprint']}' não existe")
    ords = {r["id"] for r in t["orders"]}
    for r in t["fregueses"]:
        if r["encomenda pessoal"] not in ords:
            e.append(f"{r['id']}: encomenda '{r['encomenda pessoal']}' não existe")

    # 7. nomes: en/es <= 16 em toda tabela; pt <= 16 em itens e upgrades (HUD, LocTests)
    for tab, rows in t.items():
        for r in rows:
            for col in ("en", "es") + (("pt",) if tab in ("itens", "upgrades") else ()):
                v = r.get(col)
                if v and v != "(Loc.cs)" and len(v) > 16:
                    e.append(f"{tab}: {r.get('id', r.get('nível', '?'))}.{col} '{v}' > 16")
    return e


def resumo(t):
    prods = sum(1 for r in t["itens"] if r["tipo"] == "Product")
    ess = sum(1 for r in t["blueprints"] if "sim" in r["essencial"])
    return (f"{len(t['distritos'])} distritos · {prods} vendáveis · {len(t['receitas'])} receitas · {len(t['upgrades'])} upgrades · "
            f"{len(t['workers'])} workers · {len(t['orders'])} encomendas · {len(t['contratos'])} contratos · "
            f"{len(t['blueprints'])} Blueprints ({ess} essenciais) · {len(t['masterworks'])} Masterworks")


def selftest(t):
    """Prova vermelha: cada quebra tem de gerar o erro esperado; o conteudo real tem de passar."""
    assert check(t) == [], check(t)
    casos = []

    def quebra(desc, f, esperado):
        c = copy.deepcopy(t)
        f(c)
        erros = check(c)
        assert any(esperado in x for x in erros), f"{desc}: esperava '{esperado}', veio {erros[:3]}"
        casos.append(desc)

    ess = next(i for i, r in enumerate(t["blueprints"]) if "sim" in r["essencial"])
    quebra("essencial só pago", lambda c: c["blueprints"][ess].update(fontes="Shop: pacote"), "sem fonte grátis")
    quebra("essencial só de evento", lambda c: c["blueprints"][ess].update(fontes="Event: ev_x"), "sem fonte grátis")
    quebra("Masterwork com anúncio", lambda c: c["masterworks"][0].update(**{"condição especial": "Counter:anuncio>=1"}), "anúncio/compra")
    quebra("id repetido", lambda c: c["workers"].append(dict(c["workers"][0])), "repetido")
    quebra("insumo sem produtor", lambda c: c["receitas"][1].update(insumos="cristal×1"), "ninguém produz")
    quebra("poucos vendáveis", lambda c: c.update(itens=[r for r in c["itens"] if r["tipo"] != "Product" or r["id"] == "espada"]), "vendáveis")
    quebra("nome en longo", lambda c: c["itens"][0].update(en="a" * 17), "> 16")
    quebra("projeto inexistente", lambda c: c["masterworks"][0].update(projeto="bp_nada"), "não é Blueprint")
    print(f"selftest OK: {len(casos)} quebras pegas ({', '.join(casos)})")


def main(argv):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")         # console do Windows (cp1252) nao mostra acento
    args = [a for a in argv if not a.startswith("--")]
    with open(args[0] if args else DOC, encoding="utf-8") as f:
        t = parse(f.read())
    if "--selftest" in argv:
        selftest(t)
        return 0
    erros = check(t)
    for x in erros:
        print("ERRO:", x)
    print(("FALHOU: %d erro(s). " % len(erros) if erros else "OK: ") + (resumo(t) if t.get("itens") else ""))
    return 1 if erros else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
