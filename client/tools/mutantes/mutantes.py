"""Prova vermelha versionada do núcleo (A-CORE-08): mostra que `dotnet test client/tools/coretests` pega bugs de verdade.

Uso (da raiz do repo; Python 3 só com stdlib, precisa do `dotnet` no PATH):
    python client/tools/mutantes/mutantes.py              # roda todas as mutações (~10 min, uma por vez)
    python client/tools/mutantes/mutantes.py --lista      # lista as mutações e sai
    python client/tools/mutantes/mutantes.py --so off-teto-2h [--so ...]   # roda só essas
O script copia o núcleo (Scripts/Core), os testes EditMode, o View/Textos.cs e o projeto coretests para uma pasta temporária
(NUNCA toca nos arquivos do repositório), compila a cópia limpa e exige linha de base verde. Depois aplica UMA mutação textual
por vez (arquivo + trecho exato + substituição), recompila só o que mudou, roda a suíte inteira e classifica:
MORTO (algum teste falhou: bom), VIVO (tudo passou: falta teste) ou INVÁLIDO (não compila: a mutação está errada).
Sai ≠ 0 se houver VIVO não marcado, INVÁLIDO, `esperado_vivo` que passou a morrer (tire a marca) ou mutação desatualizada
(trecho sumiu ou aparece mais de uma vez no código: a lista falha antes de compilar, não ignora). Mutante VIVO conhecido
entra com `esperado_vivo="motivo"`: aparece na tabela como achado e não derruba a saída. Mudou o código de uma mutação?
Atualize o trecho aqui no mesmo PR.
"""
import argparse
import os
import shutil
import signal
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET

CLIENT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
# o que o FSCore.Tests.csproj compila (caminhos relativos a client/), mais o próprio projeto
COPIAR = ['Assets/_FS/Scripts/Core', 'Assets/_FS/Tests/EditMode', 'Assets/_FS/Scripts/View/Textos.cs', 'tools/coretests']
SIM, DEFS = 'Assets/_FS/Scripts/Core/Sim.cs', 'Assets/_FS/Scripts/Core/Defs.cs'


def mut(id, arquivo, trecho, troca, descricao, esperado_vivo=None):
    return dict(id=id, arquivo=arquivo, trecho=trecho, troca=troca, descricao=descricao, esperado_vivo=esperado_vivo)


MUTACOES = [
    # economia
    mut('eco-preco-ferramenta', DEFS, 'Price = { 0, 0, 10, 25, 16, 60 };', 'Price = { 0, 0, 10, 25, 18, 60 };',
        'preço de venda da ferramenta 16 -> 18'),
    mut('eco-custo-crescimento', DEFS, 'CostGrowth = 1.30f;', 'CostGrowth = 1.32f;',
        'custo de upgrade cresce 1,32x por tier em vez de 1,30x'),
    mut('eco-menu-nao-cobra', SIM, 'Gold -= cost;', ';',
        'compra pelo menu aplica o upgrade sem cobrar'),
    # offline
    mut('off-taxa', DEFS, 'OfflineFactor = 0.25f;', 'OfflineFactor = 0.3f;',
        'cofre offline paga 30% da taxa online em vez de 25%'),
    mut('off-teto-2h', DEFS, 'OfflineCapSeconds = 7200;', 'OfflineCapSeconds = 10800;',
        'teto do offline 2 h (7200 s) -> 3 h'),
    mut('off-claim-repete', SIM, 'claimId <= LastClaim', 'claimId < LastClaim',
        'claim offline com o mesmo id paga de novo (perde a idempotência)'),
    mut('off-teto-relativo', DEFS, 'OfflineMaxNextUpgrades = 2f;', 'OfflineMaxNextUpgrades = 3f;',
        'cofre limitado a 3x o upgrade travado mais barato em vez de 2x'),
    # encomendas
    mut('ord-premio-nao-pago', SIM, '(long)Gold + OrderReward)', '(long)Gold)',
        'encomenda entregue não paga o prêmio'),
    mut('ord-premio-segundos', DEFS, 'OrderRewardSeconds = 10f;', 'OrderRewardSeconds = 20f;',
        'prêmio da encomenda = 20 s da taxa online em vez de 10 s'),
    mut('ord-conta-qualquer-item', SIM, 'if ((int)want == OrderItem && OrderProgress < OrderTarget)', 'if (OrderProgress < OrderTarget)',
        'qualquer venda conta para a encomenda, não só a do item pedido'),
    mut('ord-contagem-entregues', SIM, 'OrderItem = -1; OrderCount++;', 'OrderItem = -1;',
        'encomenda entregue não soma em OrderCount (rodízio e alvo da 1a quebram)'),
    # save/load
    mut('save-saved-vira-claim', SIM, 'case "saved": sim.SavedAt = L(val, 0); break;', 'case "saved": sim.LastClaim = L(val, 0); break;',
        'load lê a chave saved= para o LastClaim (chave lida no campo errado)'),
    mut('save-ord-progresso', SIM, 'ordProgress = Clamp0(I(parts[2], 0), ordTarget);', 'ordProgress = Clamp0(I(parts[1], 0), ordTarget);',
        'load lê o progresso da encomenda do campo do alvo'),
    # VIP e boost
    mut('vip-multiplicador', SIM, 'Slot(jewel, i), Balance.VipPriceMul);', 'Slot(jewel, i), 2);',
        'VIP paga 2x o preço por unidade em vez de 3x'),
    mut('boost-multiplicador', SIM, 'BoostMul += 1f;', 'BoostMul += 2f;',
        '1o anúncio de velocidade já dá 3x (em vez de 2x)'),
    mut('boost-recarga', DEFS, 'BoostCooldownSeconds = 300f;', 'BoostCooldownSeconds = 240f;',
        'recarga do boost 5 min -> 4 min'),
    # fila e clientes
    mut('fila-vaga-a-mais', SIM, 'if (q.Count < cap)', 'if (q.Count <= cap)',
        'fila aceita um cliente além das vagas'),
    mut('fila-trava-na-frente', SIM, 'if (Stock[(int)c.Want] <= 0) continue;', 'if (Stock[(int)c.Want] <= 0) break;',
        'cliente sem estoque na frente trava quem quer outro produto'),
    mut('fila-paciencia-lenta', SIM, 'c.Patience -= dt;', 'c.Patience -= dt * 0.5f;',
        'paciência dos clientes acaba na metade da velocidade'),
]


def aplicar(texto, m):
    """Texto com a mutação, ou ValueError se o trecho não aparece exatamente uma vez (mutação desatualizada)."""
    trecho, troca = m['trecho'], m['troca']
    if '\r\n' in texto:   # trecho de várias linhas escrito com \n vale no arquivo CRLF (git autocrlf no Windows)
        trecho, troca = trecho.replace('\n', '\r\n'), troca.replace('\n', '\r\n')
    n = texto.count(trecho)
    if n != 1:
        raise ValueError(f"mutação desatualizada '{m['id']}': trecho aparece {n}x em {m['arquivo']} (precisa ser 1): {m['trecho']!r}")
    return texto.replace(trecho, troca)


def ler(caminho):
    with open(caminho, encoding='utf-8', newline='') as f:
        return f.read()


def escrever(caminho, texto):
    with open(caminho, 'w', encoding='utf-8', newline='') as f:
        f.write(texto)


def rodar(cmd, timeout=None):
    """(código de saída ou None no timeout, saída). No timeout mata a árvore toda (o testhost é filho do dotnet)."""
    kw = {} if os.name == 'nt' else {'start_new_session': True}
    p = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding='utf-8', errors='replace', **kw)
    try:
        out, _ = p.communicate(timeout=timeout)
        return p.returncode, out
    except subprocess.TimeoutExpired:
        if os.name == 'nt':
            subprocess.run(['taskkill', '/F', '/T', '/PID', str(p.pid)], capture_output=True)
        else:
            os.killpg(p.pid, signal.SIGKILL)
        out, _ = p.communicate()
        return None, out


def compilar(proj):
    rc, out = rodar(['dotnet', 'build', proj, '--no-restore', '-nologo', '-v', 'q'])
    erro = next((l.strip() for l in out.splitlines() if ': error ' in l), out.strip()[-300:])
    return rc == 0, erro


def testar(proj, res, timeout=None):
    """(código, testes que falharam). Lê o TRX em vez da saída do console (não depende do idioma do dotnet)."""
    shutil.rmtree(res, ignore_errors=True)
    rc, out = rodar(['dotnet', 'test', proj, '--no-build', '-nologo', '--logger', 'trx;LogFileName=r.trx', '--results-directory', res], timeout)
    falhas = []
    trx = os.path.join(res, 'r.trx')
    if os.path.exists(trx):
        falhas = sorted(e.get('testName') for e in ET.parse(trx).iter() if e.tag.endswith('UnitTestResult') and e.get('outcome') == 'Failed')
    return rc, falhas, out


def main():
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    ap = argparse.ArgumentParser(description='Prova vermelha do núcleo: mutações textuais numa cópia, uma por vez.')
    ap.add_argument('--lista', action='store_true', help='lista as mutações e sai')
    ap.add_argument('--so', action='append', metavar='ID', help='roda só esta mutação (pode repetir)')
    a = ap.parse_args()

    ids = [m['id'] for m in MUTACOES]
    assert len(ids) == len(set(ids)), 'id de mutação repetido'
    if a.lista:
        for m in MUTACOES:
            extra = f"  [esperado_vivo: {m['esperado_vivo']}]" if m['esperado_vivo'] else ''
            print(f"{m['id']:26} {os.path.basename(m['arquivo']):8} {m['descricao']}{extra}")
        return 0
    desconhecidos = [i for i in a.so or [] if i not in ids]
    if desconhecidos:
        print('id desconhecido: ' + ', '.join(desconhecidos) + ' (veja --lista)')
        return 2
    sel = [m for m in MUTACOES if not a.so or m['id'] in a.so]

    # 1) toda mutação tem de bater com o código atual antes de gastar tempo compilando
    erros = []
    for m in sel:
        try:
            aplicar(ler(os.path.join(CLIENT, m['arquivo'])), m)
        except ValueError as e:
            erros.append(str(e))
    if erros:
        print('\n'.join(erros) + '\nAtualize o trecho em client/tools/mutantes/mutantes.py.')
        return 2

    t0 = time.time()
    tmp = tempfile.mkdtemp(prefix='fs_mutantes_')
    try:
        copia = os.path.join(tmp, 'client')
        for rel in COPIAR:
            src, dst = os.path.join(CLIENT, rel), os.path.join(copia, rel)
            if os.path.isdir(src):
                shutil.copytree(src, dst, ignore=shutil.ignore_patterns('bin', 'obj', 'TestResults', '*.meta'))
            else:
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                shutil.copy2(src, dst)
        proj, res = os.path.join(copia, 'tools', 'coretests'), os.path.join(tmp, 'res')

        # 2) linha de base: a cópia limpa compila e fica verde (restore e obj/ servem para todas as mutações)
        print(f'cópia em {tmp}; compilando a linha de base...', flush=True)
        rc, out = rodar(['dotnet', 'restore', proj, '-nologo', '-v', 'q'])
        ok, erro = compilar(proj) if rc == 0 else (False, out.strip()[-300:])
        if not ok:
            print('linha de base NÃO compila: ' + erro)
            return 3
        tb = time.time()
        rc, falhas, out = testar(proj, res)
        base = time.time() - tb
        if rc != 0:
            print('linha de base NÃO está verde: ' + (', '.join(falhas) or out.strip()[-500:]))
            return 3
        print(f'linha de base verde ({base:.0f} s de testes). {len(sel)} mutações, uma por vez:', flush=True)
        limite = max(120.0, 5 * base)   # mutação que trava um loop: MORTO por timeout

        linhas, ruins = [], 0
        for m in sel:
            arq = os.path.join(copia, m['arquivo'])
            original = ler(arq)
            tm = time.time()
            try:
                escrever(arq, aplicar(original, m))
                ok, erro = compilar(proj)
                if not ok:
                    res_m, det = 'INVÁLIDO', erro
                else:
                    rc, falhas, _ = testar(proj, res, limite)
                    if rc is None:
                        res_m, det = 'MORTO', f'timeout de {limite:.0f} s'
                    elif rc != 0:
                        res_m = 'MORTO'
                        nomes = [f.split('.')[-1] for f in falhas]
                        det = ', '.join(nomes[:2]) + (f' (+{len(nomes) - 2})' if len(nomes) > 2 else '') if nomes else 'testhost caiu'
                    else:
                        res_m, det = 'VIVO', m['descricao']
            finally:
                escrever(arq, original)   # a próxima mutação parte da cópia limpa
            if res_m == 'VIVO' and m['esperado_vivo']:
                res_m, det = 'VIVO*', m['esperado_vivo']
            elif res_m == 'MORTO' and m['esperado_vivo']:
                det = 'agora MORRE: tire o esperado_vivo. ' + det
                ruins += 1
            if res_m in ('VIVO', 'INVÁLIDO'):
                ruins += 1
            linhas.append((m['id'], res_m, time.time() - tm, det))
            print(f'  {m["id"]:26} {res_m:9} {time.time() - tm:4.0f} s  {det}', flush=True)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    print('\n' + f'{"mutação":26} {"resultado":9} {"tempo":>6}  detalhe')
    for i, r, s, d in linhas:
        print(f'{i:26} {r:9} {s:4.0f} s  {d}')
    cont = {k: sum(1 for l in linhas if l[1] == k) for k in ('MORTO', 'VIVO', 'VIVO*', 'INVÁLIDO')}
    print(f"\n{cont['MORTO']} mortos, {cont['VIVO']} vivos, {cont['VIVO*']} vivos esperados (achados), {cont['INVÁLIDO']} inválidos;"
          f' {time.time() - t0:.0f} s no total.')
    if cont['VIVO*']:
        print('VIVO* = mutante conhecido sem teste que o pegue (esperado_vivo): é achado de QA, não está coberto.')
    return 1 if ruins else 0


if __name__ == '__main__':
    sys.exit(main())
