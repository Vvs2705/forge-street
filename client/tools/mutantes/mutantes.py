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
COPIAR = ['Assets/_FS/Scripts/Core', 'Assets/_FS/Tests/EditMode', 'Assets/_FS/Scripts/View/Textos.cs', 'Assets/_FS/Scripts/View/SaveStore.cs',
          'Assets/_FS/Scripts/View/StationView.cs', 'Assets/_FS/Scripts/View/Servicos.cs', 'Assets/_FS/Scripts/View/Estado.cs',
          'Assets/_FS/Scripts/View/Captura.cs', 'Assets/_FS/Scripts/View/Som.cs', 'Assets/_FS/Scripts/View/Privacidade.cs',
          'Assets/_FS/Scripts/View/EsteiraView.cs', 'Assets/_FS/Scripts/View/Enquadramento.cs', 'Assets/_FS/Scripts/View/Loc.cs',
          'Assets/_FS/Scripts/View/QualidadeView.cs', 'Assets/_FS/Scripts/View/Plataforma.cs', 'Assets/_FS/Scripts/View/Loja.cs',
          'tools/coretests']
SIM, DEFS, CV = 'Assets/_FS/Scripts/Core/Sim.cs', 'Assets/_FS/Scripts/Core/Defs.cs', 'Assets/_FS/Scripts/Core/ContentValidator.cs'
METAS = 'Assets/_FS/Scripts/Core/Metas.cs'
ANUNCIOS = 'Assets/_FS/Scripts/Core/Anuncios.cs'
RENOME = 'Assets/_FS/Scripts/Core/Renome.cs'
ENC, ENCD = 'Assets/_FS/Scripts/Core/Encomendas.cs', 'Assets/_FS/Scripts/Core/EncomendasDefs.cs'
COMPRAS = 'Assets/_FS/Scripts/Core/Compras.cs'
PROJ, PDEFS = 'Assets/_FS/Scripts/Core/Projetos.cs', 'Assets/_FS/Scripts/Core/ProjetosDefs.cs'
EQUIPE, BOT = 'Assets/_FS/Scripts/Core/Equipe.cs', 'Assets/_FS/Scripts/Core/Bot.cs'
LIVEOPS, LIVEOPSDEFS = 'Assets/_FS/Scripts/Core/LiveOps.cs', 'Assets/_FS/Scripts/Core/LiveOpsDefs.cs'


def mut(id, arquivo, trecho, troca, descricao, esperado_vivo=None):
    return dict(id=id, arquivo=arquivo, trecho=trecho, troca=troca, descricao=descricao, esperado_vivo=esperado_vivo)


MUTACOES = [
    # economia
    mut('eco-preco-ferramenta', DEFS, 'Price = { 0, 0, 10, 25, 16, 60, 40 };', 'Price = { 0, 0, 10, 25, 18, 60, 40 };',
        'preço de venda da ferramenta 16 -> 18'),
    mut('eco-custo-crescimento', DEFS, 'CostGrowth = 1.30f;', 'CostGrowth = 1.32f;',
        'custo de upgrade cresce 1,32x por tier em vez de 1,30x'),
    mut('eco-menu-nao-cobra', SIM, 'Gold -= cost;', ';',
        'compra pelo menu aplica o upgrade sem cobrar'),
    # receitas em dados (B-CORE-01)
    mut('rec-tempo-sem-mult', SIM, 's.Recipe.TimeMul(T) * (s == JewelBench', '1f * (s == JewelBench',
        'estação ignora o multiplicador de tempo da receita (escudo e joia no tempo da espada)'),
    mut('rec-consome-so-o-1o', SIM, 'for (int k = 0; k < s.Ins.Length; k++) s.Ins[k] -= s.Recipe.Inputs[k].Qty;', 's.Ins[0] -= s.Recipe.Inputs[0].Qty;',
        'receita de 2 insumos só consome o 1o'),
    mut('rec-nao-espera-o-2o', SIM, 'for (int k = 0; k < s.Ins.Length; k++) if (s.Ins[k] < s.Recipe.Inputs[k].Qty) { s.StarveSeconds', 'for (int k = 0; k < 1; k++) if (s.Ins[k] < s.Recipe.Inputs[k].Qty) { s.StarveSeconds',
        'estação começa sem o 2o insumo da receita'),
    mut('rec-paciencia-qtd', DEFS, 't += StartTime(ri) * qty;', 't += StartTime(ri);',
        'paciência conta 1 de cada insumo, não a quantidade da receita (escudo e joia esperam menos)'),
    mut('rec-validator-insumo-orfao', CV, 'if (!Made(item)) yield return', 'if (false) yield return',
        'validator aceita receita com insumo que nenhuma estação produz'),
    # offline
    mut('off-taxa', DEFS, 'OfflineFactor = 0.25f;', 'OfflineFactor = 0.3f;',
        'cofre offline paga 30% da taxa online em vez de 25%'),
    mut('off-teto-2h', DEFS, 'OfflineCapSeconds = 7200;', 'OfflineCapSeconds = 10800;',
        'teto do offline 2 h (7200 s) -> 3 h'),
    mut('off-claim-repete', SIM, 'claimId <= LastClaim', 'claimId < LastClaim',
        'claim offline com o mesmo id paga de novo (perde a idempotência)'),
    mut('off-teto-relativo', DEFS, 'OfflineMaxNextUpgrades = 2f;', 'OfflineMaxNextUpgrades = 3f;',
        'cofre limitado a 3x o upgrade travado mais barato em vez de 2x'),
    mut('off-trava-tuning', DEFS, 'OfflineCap => Math.Min(OfflineMaxSeconds, Balance.OfflineCapSeconds);', 'OfflineCap => OfflineMaxSeconds;',
        'Tuning passa da trava de 2 h do offline (A-CORE-03)'),
    # encomendas (FASE9; Orders 2.0, B-CORE-05)
    mut('ord-premio-nao-pago', ENC, '_sim.Gold += ouro; _sim.OrderGold += ouro;', '_sim.OrderGold += ouro;',
        'encomenda entregue não paga o prêmio'),
    mut('ord-premio-segundos', DEFS, 'OrderRewardSeconds = 10f;', 'OrderRewardSeconds = 20f;',
        'prêmio da encomenda = 20 s da taxa online em vez de 10 s'),
    mut('ord-conta-qualquer-item', ENC, 'if (Itens[k] == item && Feito[k] < Alvo[k]) return k;', 'if (Feito[k] < Alvo[k]) return k;',
        'qualquer venda conta para a encomenda, não só a do item pedido'),
    mut('ord-contagem-entregues', ENC, '_sim.OrderGold += ouro; _sim.OrderCount++;', '_sim.OrderGold += ouro;',
        'encomenda entregue não soma em OrderCount (tipos que abrem por entregues e o retroativo do Renome quebram)'),
    mut('enc-paga-2x', ENC, 'if (!Vagas.Contains(e) || !e.Pronta) return -1;', 'if (!e.Pronta) return -1;',
        'entregar a mesma encomenda 2x paga 2x (o ID de transação perde a idempotência)'),
    mut('enc-qualidade-ignorada', ENC, 'if (q < Def.MinQuality) return -1;\n', '',
        'peça Normal conta para a encomenda que pede Fina (Quality, Royal)'),
    mut('enc-peca-conta-2x', ENC, 'if (k >= 0) { e.Feito[k]++; return; }', 'if (k >= 0) e.Feito[k]++;',
        'uma peça vendida conta para todas as vagas que a aceitam (era para 1, a mais antiga)'),
    mut('enc-rush-pune', ENC, 'long ouro = e.Premio + (prazo ? e.PremioBonus : 0);', 'long ouro = e.Premio + (prazo ? e.PremioBonus : e.Def.BonusDeadline > 0f ? -e.Premio / 2 : 0);',
        'Rush fora do prazo paga menos que o normal (o prazo é só bônus)'),
    mut('enc-prazo-sem-fim', ENC, 'if (e.Prazo > 0f) e.Prazo = Math.Max(0f, e.Prazo - dt);', ';',
        'o prazo do Rush nunca acaba (o bônus fica para sempre)'),
    mut('enc-tipo-sem-entregues', ENC, 'if (linhas.Count < d.MinLines || _sim.OrderCount < d.MinDelivered) return false;', 'if (linhas.Count < d.MinLines) return false;',
        'Bulk e Rush abrem sem as encomendas entregues que pedem'),
    mut('enc-tipo-antes-das-ferramentas', ENC, 'if (d.Type != OrderType.Common && !_sim.QualityRevealed) return false;', '',
        'tipos novos antes das Ferramentas (furam os 10 primeiros minutos)'),
    mut('enc-vagas-antes-das-ferramentas', ENC, 'VagasMax => !_sim.QualityRevealed ? 1 :', 'VagasMax => false ? 1 :',
        'vagas extras do Renome antes das Ferramentas'),
    mut('enc-vaga-renome1', ENC, '_sim.Renome.Nivel >= Vaga2 ? 2 : 1;', '1;',
        'o Renome 1 não abre a 2a vaga'),
    mut('enc-rodizio-repete-produto', ENC, '(Vagas.Exists(v => v != fora && Array.IndexOf(v.Itens, it) >= 0) ? ocupadas : livres).Add(it);', 'livres.Add(it);',
        'o rodízio não pula o produto que já está noutra vaga'),
    mut('enc-troca-outra', ENC, 'Abre(Gera(Abertas, p), p);', 'Abre(Gera(Abertas + 1, p), p);',
        '"Trocar encomenda" entra outra que não a mostrada antes (P5)'),
    mut('enc-ciclo', ENCD, '(OldForge, new[] { Common, Common, Bulk, Qual,', '(OldForge, new[] { Common, Bulk, Common, Qual,',
        'ciclo do Old Forge fora do ECONOMIA_WAVE_B §3.2'),
    mut('enc-n-sem-fracao', ENC, 'if (d.MinQuality > Quality.Normal) raw *= Fracao(itens[k], d.MinQuality);', '',
        'encomenda de Fina pede o N de todas as peças (só 15% saem Fina: a vaga fica presa)'),
    mut('enc-bp-nao-ganha', ENC, 'if (e.Blueprint != null) _sim.Projetos.GanhaDaFila(Fila(e.Def), e.Def.District, e.Tx);', ';',
        'encomenda Blueprint/Royal entregue não dá o projeto da fila (B-CORE-07)'),
    mut('enc-evento-sem-pontos', ENC, 'if (e.Def.EventId != null) _sim.LiveOps.PontosEncomenda(e.Def.EventId, e.Def.EventPoints);', ';',
        'encomenda Event entregue não dá os 15 pontos na trilha do evento (D-CORE-03)'),
    mut('enc-evento-sem-tipo', ENC, '_sim.LiveOps.OnEncomenda(e.Tipo);', ';',
        'entrega não conta nas missões order_complete do evento (Royal Week, Merchant Caravan)'),
    mut('enc-renome-fixo', ENC, 'int rn = e.Renome + (prazo ? e.Def.BonusReputation : 0);', 'int rn = 1;',
        'toda encomenda dá +1 de Renome (o tipo não conta)'),
    mut('enc-validator-item', ENCD, 'if (i < 0 || !w.IsProduct((Item)i) || !w.Stations.Exists(s => s.Produces && s.OutItem == (Item)i)) yield return', 'if (false) yield return',
        'validator aceita encomenda com item que não existe no jogo'),
    mut('enc-bonus-anuncio-min', DEFS, 'EncomendaBonusMinSeg = 60f;', 'EncomendaBonusMinSeg = 45f;',
        '"Bônus da encomenda" vale menos que 60 s de renda (achado do AdWatcherE)'),
    # save/load
    mut('save-saved-vira-claim', SIM, 'case "saved": sim.SavedAt = L(val, 0); break;', 'case "saved": sim.LastClaim = L(val, 0); break;',
        'load lê a chave saved= para o LastClaim (chave lida no campo errado)'),
    mut('save-ord-progresso', ENC, 'e.Feito[0] = Clamp(I(ord[2], 0), 0, alvo);', 'e.Feito[0] = Clamp(I(ord[1], 0), 0, alvo);',
        'load do save de antes (ord=) lê o progresso da encomenda do campo do alvo'),
    mut('save-enc-premio', ENC, 'Math.Max(0, L(p[2], 0)), Math.Max(0, L(p[3], 0)),', 'Math.Max(0, L(p[3], 0)), Math.Max(0, L(p[3], 0)),',
        'load das vagas (enc.v=) perde o prêmio com o bônus do anúncio'),
    mut('save-id-por-posicao', DEFS, 'return d != null ? (int)d.Up : -1;', 'return Array.IndexOf(table ?? All, d);',
        'ID do save resolvido pela posição na tabela, não pelo upgrade (A-CORE-04: reordenar a tabela troca o progresso)'),
    mut('save-v1-ordem', SIM, '"fole", "bigorna2", "ajudante1",', '"fole", "ajudante1", "bigorna2",',
        'ordem congelada do schema 1 trocada: o up= da 0.1-0.7 migra para os upgrades errados'),
    mut('save-v2-ordem-itens', SIM, '"minerio", "lingote", "espada", "escudo", "ferramenta", "joia" };', '"minerio", "lingote", "escudo", "espada", "ferramenta", "joia" };',
        'ordem congelada dos itens do schema 2 trocada: stock=/hold= da 0.8 migram para os itens errados (B-CORE-01)'),
    mut('save-maos-por-id-ignoradas', SIM, 'case "holds": ReadIdCounts(sim, parts, sim.Player.Held); break;', 'case "holds": break;',
        'load do schema 3 perde o que está na mão do ferreiro (B-CORE-01)'),
    # VIP e boost
    mut('vip-multiplicador', SIM, 'Slot(jewel, i), T.VipPriceMul);', 'Slot(jewel, i), 2);',
        'VIP paga 2x o preço por unidade em vez de 3x'),
    mut('boost-multiplicador', SIM, 'BoostMul += 1f;', 'BoostMul += 2f;',
        '1o anúncio de velocidade já dá 3x (em vez de 2x)'),
    mut('boost-recarga', DEFS, 'Id = "velocidade", Recarga = 300f', 'Id = "velocidade", Recarga = 240f',
        'recarga do boost 5 min -> 4 min'),
    mut('vip-chamado-recarga', DEFS, 'Id = "vip", Recarga = 300f', 'Id = "vip", Recarga = 240f',
        'recarga do Chamar VIP 5 min -> 4 min (P14)'),
    mut('anuncio-antes-da-melhoria', DEFS, 'AnunciosDepoisDaPrimeiraMelhoria = true;', 'AnunciosDepoisDaPrimeiraMelhoria = false;',
        'Velocidade e Chamar VIP liberados antes da 1a melhoria (D6)'),
    # anuncios por placement e intersticial (E-PLAT-02/03)
    mut('ad-paga-2x', ANUNCIOS, 'Pago[i] = k;', ';',
        'rewarded paga mas nao grava a chave: o mesmo ID de transacao paga de novo'),
    mut('ad-supply-recarga', DEFS, 'Id = "supply", Recarga = 600f', 'Id = "supply", Recarga = 540f',
        'recarga do Supply Drop 10 min -> 9 min'),
    mut('ad-teto-dia', ANUNCIOS, 'HojeTotal() >= _sim.T.AnunciosTetoDia', 'HojeTotal() > _sim.T.AnunciosTetoDia',
        'teto diario total de rewarded deixa passar 1 a mais'),
    mut('ad-troca-proxima', ENC, 'public Encomenda Proxima() => Principal == null ? null : Gera(Abertas, Principal);',
        'public Encomenda Proxima() => Principal == null ? null : Gera(Abertas + 1, Principal);',
        '"Trocar encomenda" mostra outra que nao a que entra'),
    mut('ad-inter-tutorial', ANUNCIOS, '            : !_sim.Bought[(int)Upgrade.Tools] ? "tutorial"\n', '',
        'intersticial sem o veto do tutorial (antes das Ferramentas)'),
    mut('ad-inter-flag', DEFS, 'public bool Interstitial;', 'public bool Interstitial = true;',
        'intersticial ligado por padrao (P3/V10 manda desligado)'),
    # fila e clientes
    mut('fila-vaga-a-mais', SIM, 'if (q.Count < cap)', 'if (q.Count <= cap)',
        'fila aceita um cliente além das vagas'),
    mut('fila-trava-na-frente', SIM, 'if (Stock[(int)c.Want] <= 0) continue;', 'if (Stock[(int)c.Want] <= 0) break;',
        'cliente sem estoque na frente trava quem quer outro produto'),
    mut('fila-paciencia-lenta', SIM, 'c.Patience -= dt;', 'c.Patience -= dt * 0.5f;',
        'paciência dos clientes acaba na metade da velocidade'),
    # qualidade (B-CORE-03)
    mut('qual-acumulador-sem-resto', SIM, 'if (s.QAcc >= 100) { l++; s.QAcc -= 100; }', 'if (s.QAcc >= 100) { l++; s.QAcc = 0; }',
        'acumulador zera ao passar de 100 em vez de guardar o resto (q = 15 perde Finas)'),
    mut('qual-acumulador-limiar', SIM, 's.QAcc >= 100', 's.QAcc > 100',
        'acumulador sobe de nível só acima de 100 (q = 25 vira 1 Fina a cada 5)'),
    mut('qual-preco-superior', DEFS, 'QualidadePct = { 100, 150, 250, 0 };', 'QualidadePct = { 100, 150, 200, 0 };',
        'Superior vende a 2x em vez de 2,5x'),
    mut('qual-balcao-vende-pior', SIM, 'for (int l = Graus - 1; l > 0; l--) if (q[l] > 0) { q[l]--; return (Quality)l; }',
        'for (int l = 1; l < Graus; l++) if (q[l] > 0) { q[l]--; return (Quality)l; }',
        'lote entrega a Fina antes da Superior (o balcão não vende a melhor primeiro)'),
    mut('qual-golpe-pelo-ajudante', SIM, 'Station s = StationAt(Player.Pos, out bool outZone);',
        'Station s = StationAt(Workers.Count > 0 ? Workers[0].Pos : Player.Pos, out bool outZone);',
        'golpe de mestre vale com o ajudante na boca da bancada (só o ferreiro pode)'),
    mut('qual-golpe-andando', SIM, '|| Player.Moving) return null;', ') return null;',
        'golpe de mestre com o ferreiro andando (só parado na boca)'),
    mut('qual-golpe-antes-da-revelacao', SIM, 'if (!QualityRevealed || StrikeCooldown', 'if (StrikeCooldown',
        'golpe de mestre antes das Ferramentas (fura os 10 primeiros minutos)'),
    mut('qual-golpe-sem-recarga', SIM, 'StrikeCooldown = T.GolpeRecarga;', ';',
        'golpe de mestre sem a recarga de 20 s'),
    mut('qual-golpe-erro-acerta', SIM, 'if (s == null || !hit) return false;', 'if (s == null) return false;',
        'errar o anel conta como acerto'),
    mut('qual-masterwork-sem-projeto', SIM, 'if (l == (int)Quality.Superior && MasterworkPending != null && MasterworkPending(s)) return Quality.Masterwork;',
        'if (l == (int)Quality.Superior) return Quality.Masterwork;',
        'golpe numa Superior vira Masterwork sem projeto nem condição (B-CORE-08)'),
    mut('qual-masterwork-no-balcao', SIM, 'if (l == Quality.Masterwork) { Masterworks++;', 'if (l == Quality.Masterwork) { Put(ref s.Out, s.OutQ, Quality.Superior); Masterworks++;',
        'a obra-prima também entra na pilha e vai ao balcão (vendida como Superior)'),
    mut('qual-meta-na-producao', DEFS, 'IsProduction(int u) => !All[u].Luxury && !All[u].Meta;', 'IsProduction(int u) => !All[u].Luxury;',
        'nível de estação (Meta) entra na produção completa e no teto offline'),
    mut('qual-nivel-sem-velocidade', SIM, '/ (1f + T.NivelEstacaoVelocidade * (s.Level - 1)) / Equipe.Ritmo(s);', '/ 1f / Equipe.Ritmo(s);',
        'nível de estação não acelera a família'),
    mut('qual-save-acumulador', SIM, 's.QAcc = Clamp0(I(parts[0], 0), 99);', 's.QAcc = 0;',
        'load perde o acumulador de qualidade (save no meio)'),
    mut('qual-validator-crescente', CV, 'if (pct[l] <= pct[l - 1]) yield return', 'if (false) yield return',
        'validator aceita preço de qualidade que não sobe de nível a nível'),
    # metas (D-CORE-01/02)
    mut('meta-ledger-paga-2x', METAS, 'if (!Ledger.Add(tx)) return -1;', 'Ledger.Add(tx);',
        'o ledger grava o ID mas paga de novo o mesmo ID de transação'),
    mut('meta-relogio-volta', METAS, 'if (hoje > Dia)', 'if (hoje != Dia)',
        'relógio para trás vira o dia (reabre período fechado)'),
    mut('meta-rodizio-salto', METAS, 'Escolhe(MetasDefs.Diarias, p, Dia * 5 + p, Diarias)', 'Escolhe(MetasDefs.Diarias, p, Dia * 4 + p, Diarias)',
        'rodízio das diárias com outro salto por dia (sai do LIVEOPS_V1 §4.1)'),
    mut('meta-login-punitivo', METAS, 'int d = LoginDias + 1;', 'int d = Dia - LoginUltimo > 1 ? 1 : LoginDias + 1;',
        'login com dia perdido volta ao dia 1 (DM §135 proíbe)'),
    mut('meta-ouro-no-earned', METAS, '_sim.Gold += g;', '_sim.Gold += g; _sim.GoldEarned += g;',
        'ouro das metas entra no GoldEarned (infla a taxa online e o cofre)'),
    mut('meta-antes-das-ferramentas', METAS, 'if (!Aberto || !m.Pronta) return -1;', 'if (!m.Pronta) return -1;',
        'meta paga antes das Ferramentas (mexe nos primeiros 10 min, GDD §3.3)'),
    # Renome (B-CORE-06)
    mut('renome-ganho-negativo', RENOME, 'if (n <= 0) return;', 'if (n == 0) return;',
        'Ganha com valor negativo desconta (o Renome deixa de ser monotônico)'),
    mut('renome-ledger-2x', RENOME, 'if (!Ledger.Add(tx)) return false;', 'Ledger.Add(tx);',
        'Credita paga de novo um ID de transação já creditado (Charter, Masterwork, retroativo)'),
    mut('renome-retro-sempre', RENOME, 'if (p[i].Length > 0) Ledger.Add(p[i]);\n                return;',
        'if (p[i].Length > 0) Ledger.Add(p[i]);\n                break;',
        'o retroativo roda também no save novo (com rn=): soma encomendas e baús de novo'),
    mut('renome-limiar-estrito', RENOME, 'if (Total >= RenomeDefs.Niveis[i].Limiar) return i;', 'if (Total > RenomeDefs.Niveis[i].Limiar) return i;',
        'o nível só sobe depois de passar do limiar (20 de Renome ainda é nível 0)'),
    mut('renome-metas-nao-credita', METAS, '_sim.Renome.Ganha(renome);', '_sim.Renome.Ganha(0);',
        'meta resgatada guarda o Renome só no Metas e não credita no Renome real'),
    mut('renome-superior-nao-conta', SIM, 'Renome.OnVendaQualidade((int)l);', 'Renome.OnVendaQualidade(0);',
        'peça Superior vendida no balcão não conta para o Renome'),
    # compras in-app (E-PLAT-04/05/07)
    mut('iap-recibo-repetido-entrega', COMPRAS, 'if (_ledger.TryGetValue(r.Tx, out Registro g))', 'if (_ledger.TryGetValue(r.Tx + "#", out Registro g))',
        'o mesmo recibo (duplicado, restore) vira compra nova: entrega e grava de novo'),
    mut('iap-confirma-sem-save', COMPRAS, 'if (Pendentes && !gravar()) return feitos;', 'if (Pendentes) gravar();',
        'confirma a compra à loja mesmo com o save que falhou (app morto = pago e perdido)'),
    mut('iap-posse-sem-recibo', COMPRAS, ' || Validador == null || !Validador.Valido(g.R)) continue;', ') continue;',
        'posse sai de qualquer linha iap.tx do save, sem validar o recibo (save editado liga o Pacote)'),
    mut('iap-load-confirmada', COMPRAS, 'E = p[2] == "c" ? EstadoCompra.Confirmada : EstadoCompra.Entregue,', 'E = EstadoCompra.Confirmada,',
        'save gravado antes da confirmação volta como confirmado (nunca confirma se a loja não reenviar)'),
    mut('iap-validator-moeda', COMPRAS, 'if (Array.IndexOf(MoedasAVenda, c.Id) < 0) yield return', 'if (false) yield return',
        'validator aceita produto com ouro ou moeda que não está à venda (V9/V20)'),
    mut('iap-sem-video-todos', COMPRAS, '=> Tem(SemVideo) && Array.IndexOf(SemVideoCobre, a) >= 0;', '=> Tem(SemVideo);',
        'Pacote do Mestre dispensa o vídeo também no VIP, no Cofre 2x e no bônus (ouro direto, fora da V20)'),
    mut('iap-remove-ads-nao-chega', COMPRAS, '_sim.Anuncios.RemoveAds = Tem(RemoveAds);', ';',
        'a posse remove_ads não chega ao veto do intersticial'),
    # workers (B-CORE-04)
    mut('wk-nivel1-passo', EQUIPE, '1f + WorkerDefs.PorterVel * (Nivel(w.Wk) - 1)', '1f + WorkerDefs.PorterVel * Nivel(w.Wk)',
        'o ajudante no nível 1 já anda 6% mais rápido (o jogo deixa de ser o de antes)'),
    mut('wk-carga-nivel', EQUIPE, 'foreach (int l in WorkerDefs.PorterCargaEm) if (n >= l) c++;', 'foreach (int l in WorkerDefs.PorterCargaEm) if (n > l) c++;',
        'a carga extra do ajudante chega um nível depois'),
    mut('wk-oficial-outra-familia', EQUIPE, 'Defs[k].Station == s.Recipe.Family && Nivel(k) > 0', 'Nivel(k) > 0',
        'o oficial vale em toda bancada, não só na da família dele'),
    mut('wk-mestre-na-fornalha', EQUIPE, 'if (m > 0 && s.Kind == Kind.Crafter) r +=', 'if (m > 0) r +=',
        'o mestre de obras acelera também a fornalha'),
    mut('wk-atendimento-sempre', EQUIPE, '=> NivelPapel(WorkerRole.Clerk) > 0 ? WorkerDefs.ClerkAtende : Balance.ServeTime;', '=> WorkerDefs.ClerkAtende;',
        'o balcão atende no ritmo da balconista sem ela'),
    mut('wk-comprar-sem-cobrar', EQUIPE, '_sim.Gold -= c; Gasto += c;', 'Gasto += c;',
        'contratar e subir nível não cobram o ouro'),
    mut('wk-renome-ignorado', EQUIPE, 'return RenomeDefs.NivelQueLibera(d.Id) < 0 || _sim.Renome.Libera(d.Id);', 'return true;',
        'almoxarife e mestre de obras contratáveis sem o Renome 1/2'),
    mut('wk-revelacao', EQUIPE, 'public bool Aberta => _sim.T.Workers && _sim.QualityRevealed;', 'public bool Aberta => _sim.T.Workers;',
        'workers à venda antes das Ferramentas (mexe nos primeiros 10 min)'),
    mut('wk-save-sem-nivel', EQUIPE, 'if (t.Count > 0) sb.Append("wkn=")', 'if (false) sb.Append("wkn=")',
        'o save perde o nível e a contratação dos workers'),
    mut('wk-load-depois-da-pilha', SIM, 'depende do almoxarife\n            sim.Recompute();', 'depende do almoxarife','o load corta a pilha de saída no teto sem o almoxarife (perde peças)'),
    mut('wk-validator-custo-crescente', CV, 'else if (i > 0 && d.Niveis[i] <= d.Niveis[i - 1])', 'else if (false)',
        'validator aceita nível de worker que não fica mais caro'),
    mut('wk-bot-antes-da-producao', BOT, 'if (!s.ProductionComplete && c > s.RateEma * WorkerSegundos) return -1;', ';',
        'o bot compra worker antes da produção completa (atrasa a curva produtiva)'),
    # eventos e Passe (D-CORE-03/04)
    mut('liveops-relogio-volta', LIVEOPS, 'if (nowUnix > Relogio) Relogio = nowUnix;', 'Relogio = nowUnix;',
        'relógio para trás reabre o evento (o relógio gravado deixa de ser o máximo)'),
    mut('liveops-ftue', LIVEOPS, 'public bool Requisitos(EventDef e) => M.Aberto && (', 'public bool Requisitos(EventDef e) => (',
        'evento entra antes das Ferramentas (modificador nos primeiros 10 min, GDD §3.3)'),
    mut('liveops-knob-nao-volta', LIVEOPS, 'foreach (KeyValuePair<FieldInfo, object> kv in _antes) kv.Key.SetValue(_sim.T, kv.Value);',
        'foreach (KeyValuePair<FieldInfo, object> kv in _antes) { }',
        'o modificador do evento fica no Tuning depois do fim'),
    mut('liveops-limiar-2x', LIVEOPS, 'Credita("e:" + e.Id + ":" + m.Pontos.ToString(CultureInfo.InvariantCulture), m.Premios)',
        'Credita("e:" + e.Id + ":" + m.Pontos.ToString(CultureInfo.InvariantCulture) + Relogio, m.Premios)',
        'limiar do evento com ID de transação que muda: paga de novo a cada passo'),
    mut('liveops-golpe-nao-conta', SIM, 'LiveOps.OnGolpe((int)s.OutItem);', ';',
        'golpe de mestre não conta na missão master_strike'),
    mut('liveops-teto-encomenda', LIVEOPS, 'long n = Math.Min(Passe.XpEncomenda, Passe.TetoEncomendaDia - XpEncHoje);', 'long n = Passe.XpEncomenda;',
        'XP de encomenda no Passe sem o teto de 100 por dia (V20)'),
    mut('liveops-retro-2x', LIVEOPS, '            XpVisto = M.PassXp;\n', '',
        'o XP das metas entra no Passe de novo a cada passo (o retroativo deixa de ser 1x)'),
    mut('liveops-fim-sem-pendentes', LIVEOPS, 'if (Passe != null) for (int n = 1; n <= Nivel; n++) Resgatar(n);', ';',
        'fim da temporada perde o nível alcançado e não resgatado'),
    mut('liveops-premium-pago', LIVEOPSDEFS, 'if (r.Tipo != TipoPremio.Cosmetico && r.Tipo != TipoPremio.Conveniencia && r.Tipo != TipoPremio.Gems)', 'if (false)',
        'validator aceita ouro, Renome, XP ou Blueprint no premium (V20)'),
    mut('liveops-sobreposicao', LIVEOPSDEFS, 'if (k > 0 && e.Inicio < evs[k - 1].Inicio + evs[k - 1].Dias)', 'if (false)',
        'validator aceita dois eventos ao mesmo tempo'),
    mut('liveops-knob-proibido', LIVEOPSDEFS, 'int i = Array.FindIndex(Knobs, kn => kn.Knob == m.Knob);', 'int i = 0;',
        'validator aceita knob fora da tabela do §2.3 (offline, anúncio, custo)'),
    mut('liveops-fechamento', LIVEOPSDEFS, 'if (dia * (p.Dias - 7) < p.XpTotal)', 'if (false)',
        'validator aceita trilha free que o F2P não fecha'),
    mut('liveops-sem-play-day', LIVEOPSDEFS, 'if (e.Dias >= 3 && !Array.Exists(', 'if (false && !Array.Exists(',
        'validator aceita evento sem a "jogue em 3 dias": a trilha fecha numa sessão (LIVEOPS_V1 §2.2)'),
    mut('enc-evento-pontos-20', ENCD, 'int pontos = 15)', 'int pontos = 20)',
        'encomenda Event volta a 20 pontos: a trilha fecha cedo demais'),
    # Blueprints (B-CORE-07)
    mut('bp-posse-duplica', PROJ, 'if (Posses.ContainsKey(Defs[i].Id)) return false;\n            if (tx != null)', 'if (tx != null)',
        'ganhar o mesmo Blueprint de novo regrava e emite outra vez (posse vira contador)'),
    mut('bp-tx-repete', PROJ, 'if (tx != null) foreach (Posse p in Posses.Values) if (p.Tx == tx) return false;', ';',
        'a mesma transação (encomenda, loja) dá um 2o Blueprint'),
    mut('bp-pago-sem-tx', PROJ, 'if (ProjetosDefs.Paga(origem) && string.IsNullOrEmpty(tx)) return false;', ';',
        'loja/Pass premium dão Blueprint sem ID de transação'),
    mut('bp-fonte-nao-listada', PROJ, 'if (i < 0 || !Array.Exists(Defs[i].Sources, f => f.Kind == origem)) return false;', 'if (i < 0) return false;',
        'o gancho dá Blueprint por uma fonte que o BlueprintDef não lista (ex.: loja)'),
    mut('bp-marco-fina-conta-tudo', PROJ, '(f.MinQ >= Quality.Fine ? Finas[it] : _sim.SoldItems[it])', '_sim.SoldItems[it]',
        'marco "N Fina ou melhor" conta as peças Normais'),
    mut('bp-conquista-sem-resgate', PROJ, 'return _sim.Metas.Ledger.Contains("a:" + f.Ref);', 'return true;',
        'conquista dá o Blueprint sem ser resgatada'),
    mut('bp-save-perde-tx', PROJ, '(string.IsNullOrEmpty(p.Tx) ? "" : ":" + p.Tx)', '""',
        'o save perde o ID de transação da posse (a encomenda paga de novo depois do load)'),
    mut('bp-load-ignora', PROJ, 'if (raw.StartsWith("bp=", StringComparison.Ordinal))', 'if (raw.StartsWith("bpx=", StringComparison.Ordinal))',
        'load perde os Blueprints possuídos'),
    mut('bp-menu-sem-projeto', SIM, '(d.Requires < 0 || Bought[d.Requires]) && TemProjeto(u);', '(d.Requires < 0 || Bought[d.Requires]);',
        'nível IV-V à venda no menu sem o projeto de mestre'),
    mut('bp-compra-sem-projeto', SIM, 'if (!TemProjeto((int)u)) return false;', ';',
        'compra direta (pad/cheat) de nível IV-V e bigorna longa sem o projeto'),
    mut('bp-pad-sem-projeto', SIM, 'if (!s.TemProjeto((int)u)) return -1;', ';',
        'o pad da bigorna longa aparece sem o projeto da receita'),
    mut('bp-tecnica-zero', SIM, 'b[(int)QualitySource.Blueprint] = Projetos.Tecnica(s.Recipe.Family);', 'b[(int)QualitySource.Blueprint] = 0;',
        'Blueprint de técnica não soma qualidade'),
    mut('bp-tecnica-pela-estacao', SIM, 'Projetos.Tecnica(s.Recipe.Family)', 'Projetos.Tecnica(s.Recipe.Station)',
        'técnica pela estação, não pela família (a bigorna longa fica sem a das bigornas)'),
    mut('bp-nivel-pela-estacao', SIM, 'd.Station == s.Recipe.Family && Bought[(int)d.Up]', 'd.Station == s.Recipe.Station && Bought[(int)d.Up]',
        'nível de estação pela estação, não pela família (a bigorna longa não sobe com as bigornas)'),
    mut('bp-validator-essencial-pago', CV, 'if (d.Essential && !Array.Exists(d.Sources, f => ProjetosDefs.Permanente(f.Kind)))', 'if (false)',
        'validator aceita Blueprint essencial só pago (receita essencial nunca só paga)'),
    mut('bp-permanente-contrato', PDEFS, 'k <= BlueprintSourceKind.Achievement;', 'k <= BlueprintSourceKind.Contract;',
        'contrato segura essencial sozinho (contrato nunca bloqueia, GDD §11)'),
    mut('bp-mestre-sem-producao', PDEFS, 'true, FilaEncomenda(3), Producao()),', 'true, FilaEncomenda(3)),',
        'projeto de mestre das bigornas só pela fila de encomenda (inalcançável no F2P de hoje)'),
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
