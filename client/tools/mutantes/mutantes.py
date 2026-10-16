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
          'Assets/_FS/Scripts/View/Mixagem.cs',
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
MW, MWDEFS = 'Assets/_FS/Scripts/Core/Masterworks.cs', 'Assets/_FS/Scripts/Core/MasterworksDefs.cs'
CTR, CTRD = 'Assets/_FS/Scripts/Core/Contratos.cs', 'Assets/_FS/Scripts/Core/ContratosDefs.cs'
MIX = 'Assets/_FS/Scripts/View/Mixagem.cs'
DIST, TEXTOS = 'Assets/_FS/Scripts/Core/Distritos.cs', 'Assets/_FS/Scripts/View/Textos.cs'
# ---- artisan_quarter (C-CORE-06)
BANC, CLI, BOTB = 'Assets/_FS/Scripts/Core/Sim.Bancada.cs', 'Assets/_FS/Scripts/Core/Sim.Clientes.cs', 'Assets/_FS/Scripts/Core/Bot.Bancada.cs'
AQ, IMP = 'Assets/_FS/Scripts/Core/Distritos.ArtisanQuarter.cs', 'Assets/_FS/Scripts/Core/Imperio.cs'
TRANSP, LOC = 'Assets/_FS/Scripts/Core/Sim.Transporte.cs', 'Assets/_FS/Scripts/View/Loc.cs'
COTAS, BOTCOTAS = 'Assets/_FS/Scripts/Core/Sim.Cotas.cs', 'Assets/_FS/Scripts/Core/Bot.Cotas.cs'   # C-CORE-07
RADIST, IMP = 'Assets/_FS/Scripts/Core/Distritos.RoyalArsenal.cs', 'Assets/_FS/Scripts/Core/Imperio.cs'
# ---- arcane_foundry (C-CORE-08)
ARC, BOTARC = 'Assets/_FS/Scripts/Core/Sim.Arcano.cs', 'Assets/_FS/Scripts/Core/Bot.Arcano.cs'
AFDIST = 'Assets/_FS/Scripts/Core/Distritos.ArcaneFoundry.cs'


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
    # metas de sistemas (raia H): contrato, Charter, Collection, projetos, Renome, nivel de estacao, qualidade, golpe, evento e Passe
    mut('meta-graded-nao-conta', METAS, 'Soma("quality_crafted", it, 1);', ';',
        'peça Fina/Superior fabricada (Ev.Graded) não conta nas metas de qualidade'),
    mut('meta-superior-pela-fina', METAS, 'if (b >= (int)Quality.Superior) Soma("quality_superior", it, 1);', 'if (b >= (int)Quality.Fine) Soma("quality_superior", it, 1);',
        'peça Fina conta como Superior'),
    mut('meta-golpe-nao-conta', SIM, '            Metas.Registra("master_strike", (int)s.OutItem);', ';',
        'golpe de mestre acertado (Sim.Strike) não conta nas metas'),
    mut('meta-elmo-sem-id', SIM, 'if (r < 0 && kind == Ev.Sold) Heroi.Metas.RegistraItem("product_sold", ItemDefs[a].Id);', ';',
        'venda de item que a raiz não tem (o elmo da Row) não conta pelo ID nas metas do Império'),
    mut('meta-charter-ledger-errado', METAS, 'case "district_unlock": return _sim.Renome.Ledger.Contains("charter:" + d.Ref) ? 1 : 0;',
        'case "district_unlock": return Ledger.Contains("charter:" + d.Ref) ? 1 : 0;',
        'conquista do Charter lê o ledger das metas (o charter: está no do Renome): nunca fecha'),
    mut('meta-colecao-qualquer-marco', METAS, 'return _sim.Renome.Ledger.Contains(Colecao.Prefixo + d.Ref) ? 1 : 0;',
        'return Array.Exists(_sim.Colecao.Marcos, _sim.Colecao.Pago) ? 1 : 0;',
        'qualquer marco da Collection fecha a conquista de Produtos 100%'),
    mut('meta-bp-um-a-mais', METAS, 'case "blueprint_unlock": return _sim.Projetos.Count;', 'case "blueprint_unlock": return _sim.Projetos.Count + 1;',
        'conquista de projetos conta 1 a mais (o 1o fecha sem projeto)'),
    mut('meta-renome-pelo-total', METAS, 'case "reputation_level": return _sim.Renome.Nivel;', 'case "reputation_level": return _sim.Renome.Total;',
        'conquista de nível de Renome conta o total (Renome 1 com 5 pontos)'),
    mut('meta-estacao-nivel-2', METAS, 'if (_sim.Bought[u] && nv >= d.Filtro && ups[u].Station != null)', 'if (_sim.Bought[u] && nv >= 2 && ups[u].Station != null)',
        'qualquer nível de estação conta como o nível pedido (o II fecha o "nível 3")'),
    mut('meta-evento-primeiro-limiar', METAS, 'e.Trilha[e.Trilha.Length - 1].Pontos', 'e.Trilha[0].Pontos',
        'trilha de evento "fechada" no 1o limiar'),
    mut('meta-passe-nivel-1', METAS, 'Ledger.Contains(LiveOps.TxNivel(p, p.Niveis))', 'Ledger.Contains(LiveOps.TxNivel(p, 1))',
        'trilha free do Passe "fechada" no nível 1'),
    mut('meta-entrada-sem-conta', LIVEOPS, 'M.Registra("event_enter");', ';',
        'entrar num evento não conta (ach_eventos4 nunca anda)'),
    mut('meta-contrato-sempre-livre', METAS, 'case "contract_complete": return _sim.Contratos.Ativo != null || _sim.Contratos.Oferta != null;',
        'case "contract_complete": return true;',
        'semanal de contrato entra no rodízio sem contrato a cumprir'),
    mut('meta-qualidade-sempre-livre', METAS, 'case "master_strike": return _sim.Encomendas.TipoAberto(OrderType.Quality);', 'case "master_strike": return true;',
        'semanais de Superior e golpe entram antes da Quality aberta'),
    mut('meta-semear-sem-contrato', METAS, 'Max("contract_complete", -1, s.Contratos.Concluidos.Count);', ';',
        'save antigo com contrato concluído não ganha o retroativo'),
    mut('meta-validator-sem-teto', CV, 'if (tipo == TipoMeta.Conquista && d.Alvo > conteudo) yield return', 'if (false) yield return',
        'validator aceita conquista acima do conteúdo do build (13 contratos, 5 famílias no nível V)'),
    mut('meta-validator-renome-laco', CV, 'if (d.Evento == "reputation_level" && d.Renome > 0) yield return', 'if (false) yield return',
        'validator aceita conquista de Renome que dá Renome (laço)'),
    mut('meta-validator-ref', CV, '(d.Filtro >= 0 || !RefExiste(d))', '(d.Filtro >= 0)',
        'validator aceita filtro por ID que não existe (distrito, marco, insumo)'),
    mut('meta-royal-week-sem-contrato', LIVEOPSDEFS, 'M("contrato1", "contract_complete", 1, 100)', 'M("golpe10", "master_strike", 10, 100)',
        'a Royal Week volta sem a missão do Royal Contract'),
    mut('meta-missoes-sem-teto', LIVEOPSDEFS, 'else if (e.Missoes.Length > MaxMissoes) yield return', 'else if (false) yield return',
        'validator aceita evento com mais missões do que o cartão mostra (8)'),
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
    mut('enc-caravan-pontos-15', ENCD, '"ev_s2w3_merchant_caravan", 12)', '"ev_s2w3_merchant_caravan", 15)',
        'Merchant Caravan a 15 pontos: com 3 vagas o F2P de referência fecha a trilha no dia 3 (BALANCE §30.6)'),
    mut('enc-sunblade-pontos-20', ENCD, '"ev_s1w4_hunt_sunblade", 17)', '"ev_s1w4_hunt_sunblade", 20)',
        'Hunt da Sunblade a 20 pontos: fecha no dia 3 (BALANCE §30.6)'),
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
    # mix de audio (fix/mix-audio): as regras que tiram a "barulheira"
    mut('mix-sem-teto-categoria', MIX, 'if (n >= Teto[(int)cat]) return cat == Cat.UI ? ui : -1;', 'if (false) return -1;',
        'sem teto por categoria: o 3o som de Mundo ocupa a voz livre'),
    mut('mix-rouba-sem-prioridade', MIX, '_v[i].Cat < _v[v].Cat ||', '_v[i].Cat > _v[v].Cat ||',
        'Importante rouba a Acao em vez do Mundo mais antigo'),
    mut('mix-importante-atropela', MIX, 'if (i >= 0 && _v[i].Som != som)', 'if (false)',
        'Importante sem fila: corta o outro Importante que soa'),
    mut('mix-gap-nome', MIX, '&& t - u < r.Gap) return Barra();', '&& t - u < 0f) return Barra();',
        'sem intervalo por nome: martelo de cada bancada e cada venda tocam'),
    mut('mix-gap-categoria', MIX, 'if (t - _ultCat[c] < GapCat[c]) return Barra();', 'if (false) return Barra();',
        'sem intervalo por categoria: sons de Mundo colados'),
    mut('mix-mundo-fora-da-tela', MIX, 'if (r.Cat == Cat.Mundo && !NaTela(pos.Value)) return Barra();', 'if (false) return Barra();',
        'Mundo fora da tela ainda toca'),
    mut('mix-distancia', MIX, 'if (g < Minimo) return Barra();', 'if (g < 0f) return Barra();',
        'Mundo longe do ferreiro toca (mudo, mas ocupa voz)'),
    mut('mix-sem-ducking', MIX, 'if (t <= _duckFim) return DuckGanho;', 'if (t <= _duckFim) return 1f;',
        'Importante nao abaixa musica, ambiente e Mundo'),
    mut('mix-sino-sempre', MIX, 'case Ev.ClientArrived: return e.B == 1 ? "sino" : null;', 'case Ev.ClientArrived: return "sino";',
        'sino a cada cliente, nao so com a fila vazia'),
    mut('mix-moeda-sobe', MIX, 'case Ev.Sold: return "venda";', 'case Ev.Sold: pitch = 1f + 0.05f * (e.B % 5); return "venda";',
        'moeda da venda volta a subir de altura a cada venda'),
    mut('bp-mestre-sem-producao', PDEFS, 'true, FilaEncomenda(3), Producao()),', 'true, FilaEncomenda(3)),',
        'projeto de mestre das bigornas só pela fila de encomenda (inalcançável no F2P de hoje)'),
    # Masterworks e Collection (B-CORE-08)
    mut('mw-sem-projeto', MW, 'public bool Liberada(MasterworkDef d) => _sim.Projetos.Tem(d.Blueprint) && ', 'public bool Liberada(MasterworkDef d) => ',
        'Masterwork sai sem o projeto (Blueprint)'),
    mut('mw-sem-condicao', MW, 'Array.TrueForAll(d.Condicoes, c => Valor(c) >= c.N)', 'true',
        'Masterwork sai sem a condição especial'),
    mut('mw-entra-2x', MW, 'if (Defs[i].Familia == f && !Tem(Defs[i].Id) && Liberada(Defs[i])) return i;', 'if (Defs[i].Familia == f && Liberada(Defs[i])) return i;',
        'a mesma Masterwork sai de novo (a 2a vez devia sair Superior e vender)'),
    mut('mw-renome-sem-tx', MW, '_sim.Renome.Credita(Tx(Defs[k]), Defs[k].Renome);', '_sim.Renome.Ganha(Defs[k].Renome);',
        'Renome da obra sem ID de transação (o ledger não guarda mw:)'),
    mut('mw-familia-pelo-item', MW, '_sim.ItemDefs[(int)s.OutItem].Family : null;', '_sim.ItemDefs[(int)s.OutItem].Id : null;',
        'família da peça pelo item (a espada longa perde as obras da espada)'),
    mut('mw-bonus-sem-familia-completa', MW, 'if (d.Familia == f) { if (!Tem(d.Id)) return 0; alguma = true; }', 'if (d.Familia == f) alguma = true;',
        '+5% no preço sem a família completa no Hall'),
    mut('mw-hall-load-ignora', MW, 'if (!raw.StartsWith("hall=", StringComparison.Ordinal)) continue;', 'if (!raw.StartsWith("hallx=", StringComparison.Ordinal)) continue;',
        'o Load perde a data do Hall'),
    mut('mw-metas-nao-conta', METAS, 'Soma("masterwork_unlock", b, 1);', ';',
        'conquistas de Masterwork não contam'),
    mut('col-paga-2x', MW, 'if (Pago(m) || Feitas(m.Aba) < m.N) continue;', 'if (Feitas(m.Aba) < m.N) continue;',
        'marco da coleção pago (e avisado) de novo a cada Tick'),
    mut('col-abre-no-ftue', MW, 'if (!_sim.QualityRevealed) return false;', ';',
        'coleção abre antes das Ferramentas (primeiros 10 min)'),
    mut('col-superior-nao-conta', MW, 'if (l == Quality.Superior && (uint)item < (uint)Superiores.Length) Superiores[item]++;', ';',
        'Superior vendida não conta na aba Produtos'),
    mut('mw-bot-nao-espera', BOT, 'if (Strike && !EsperaSuperior(s)) s.Strike(true);', 'if (Strike) s.Strike(true);',
        'o Ideal golpeia a peça que não sairia Superior mesmo com Masterwork pendente'),
    mut('mw-hunt-sem-posse', LIVEOPS, 'if (r0.Tipo == TipoPremio.Blueprint && _sim.Projetos.Ganha(r0.Ref, origem, tx)) continue;', ';',
        'o Hunt nunca dá o projeto da Masterwork (só o fallback de ouro)'),
    mut('mw-hunt-sem-requisito', LIVEOPS, ' && Array.TrueForAll(e.Requer, r => ProjetosDefs.IndexOf(r) < 0 || _sim.Projetos.Tem(r));', ';',
        'o Hunt entra sem o projeto que o evento pede'),
    mut('mw-validator-pago', MWDEFS, 'if (Array.Exists(Pagos, p => r.StartsWith(p, StringComparison.Ordinal)) || Array.Exists(w.T.Placements, p => p.Id == r))', 'if (false)',
        'validator aceita condição de anúncio ou compra'),
    mut('mw-validator-sem-permanente', MWDEFS, 'else if (!Array.Exists(bps[b].Sources, f => ProjetosDefs.Permanente(f.Kind)))', 'else if (false)',
        'validator aceita projeto de Masterwork só de evento'),
    # Royal Contracts (D-CORE-04)
    mut('ctr-concluido-volta', CTR, 'if (!Concluido(d.Id)) Concluidos.Add(d.Id);', ';',
        'contrato concluído não entra em Concluidos (volta à oferta; o Charter não o conta)'),
    mut('ctr-sem-bonus', CTR, 'bool prazo = c.Prazo > 0f;', 'bool prazo = false;',
        'prazo-bônus nunca paga (ouro x1,5 e Renome +5 somem)'),
    mut('ctr-item-qualquer', CTR, 'if (item != e.Item || q < e.MinQ) return false;', 'if (q < e.MinQ) return false;',
        'qualquer item vendido conta para a etapa'),
    mut('ctr-qualidade-ignorada', CTR, 'if (item != e.Item || q < e.MinQ) return false;', 'if (item != e.Item) return false;',
        'peça Normal conta numa etapa Fina'),
    mut('ctr-oferta-sem-renome', CTR, 'if (d.Renome >= 0 && !_sim.Renome.Libera(d.Id)) return false;', ';',
        'contrato abre sem o Renome da tabela'),
    mut('ctr-duas-vagas', CTR, 'return d != null && (Ativo == null || MesaReal && Ativo2 == null) && Aberto(d) ? d : null;', 'return d != null && Aberto(d) ? d : null;',
        'oferta com a vaga ocupada (2 ativos)'),
    mut('ctr-abandono-pune', CTR, 'Abandonados++;', 'Abandonados++; _sim.Gold = 0;',
        'abandonar zera o ouro (abandono com penalidade)'),
    mut('ctr-save-sem-feito', CTR, ".Append(c.Etapa).Append(',').Append(c.Feito).Append(',')", ".Append(c.Etapa).Append(',').Append(0).Append(',')",
        'save grava feito = 0 (o progresso da etapa some ao reabrir)'),
    mut('ctr-prazo-parado', CTR, 'if (Ativo.Prazo > 0f) Ativo.Prazo = Math.Max(0f, Ativo.Prazo - dt);', ';',
        'prazo-bônus não anda (sempre no prazo)'),
    mut('ctr-bp-nao-ganha', CTR, 'if (d.Blueprint != null) _sim.Projetos.Ganha(d.Blueprint, BlueprintSourceKind.Contract, c.Tx);', ';',
        'contrato não dá o Blueprint'),
    mut('ctr-flag-ignorada', CTR, 'if (!_sim.T.Contratos || !_sim.QualityRevealed', 'if (!_sim.QualityRevealed',
        'flag Contratos desligada ainda oferece'),
    mut('ctr-antes-das-ferramentas', CTR, 'if (!_sim.T.Contratos || !_sim.QualityRevealed', 'if (!_sim.T.Contratos',
        'contrato abre antes das Ferramentas (fura os 10 primeiros minutos)'),
    mut('ctr-etapa-nao-fecha', CTR, 'if (++c.Feito >= e.N) { c.Etapa++; c.Feito = 0; }', 'if (++c.Feito > e.N) { c.Etapa++; c.Feito = 0; }',
        'etapa só fecha com N + 1 peças'),
    mut('ctr-validator-item', CTRD, 'if (!noBuild) continue;', 'continue;',
        'validator não confere o item das etapas nos contratos do build'),
    mut('ctr-raiz-ignorada', SIM, 'public Contratos Contratos => Heroi != null ? Heroi.Contratos : _contratos;', 'public Contratos Contratos => _contratos;',
        'contrato de outro distrito do Império é o próprio, não o da raiz (venda lá não conta)'),
    mut('ctr-validator-essencial', CTRD, "if (bps[b].Essential && !Array.Exists(bps[b].Sources, f => ProjetosDefs.Permanente(f.Kind)))", "if (false)",
        'validator aceita Blueprint essencial só por contrato'),
    # C-CORE-05: a liga na Rua dos Armeiros (LigaTests, RowTests; BALANCE.md §35)
    mut('liga-sem-q', SIM, 'b[(int)QualitySource.Material] = TemLiga(s) ? T.LigaQ : 0;', 'b[(int)QualitySource.Material] = 0;',
        'a liga no slot não soma qualidade'),
    mut('liga-nao-consome', SIM, 'if (TemLiga(s)) { s.Ins[s.Liga]--; PecasComLiga[(int)s.OutItem]++; }', 'if (TemLiga(s)) { PecasComLiga[(int)s.OutItem]++; }',
        'a peça leva a qualidade da liga sem gastar a liga'),
    mut('liga-sem-contador', SIM, 'if (TemLiga(s)) { s.Ins[s.Liga]--; PecasComLiga[(int)s.OutItem]++; }', 'if (TemLiga(s)) { s.Ins[s.Liga]--; }',
        'peça com liga não conta em PecasComLiga (AlloyPieces)'),
    mut('liga-sem-slot', SIM, 's.Liga = Array.FindIndex(s.Recipe.Inputs, x => x.Qty == 0);', 's.Liga = -1;',
        'montagem e forja sem o slot de liga'),
    mut('liga-save-sem-linha', SIM, 'if (Array.Exists(PecasComLiga, n => n > 0)) sb.Append("liga=")', 'if (false) sb.Append("liga=")',
        'save sem as peças com liga'),
    mut('liga-load-ignora', SIM, 'case "liga": ReadIdCounts(sim, parts, sim.PecasComLiga); break;', 'case "liga": break;',
        'load ignora a linha liga='),
    mut('liga-vulcana-sem-nivel', EQUIPE, 'return n >= 10 ? WorkerDefs.SmelterQ10 : n >= 5 ? WorkerDefs.SmelterQ5 : WorkerDefs.SmelterQ;', 'return WorkerDefs.SmelterQ;',
        'a Vulcana não sobe a qualidade nos níveis 5 e 10'),
    mut('liga-vulcana-sem-ritmo', EQUIPE, 'if (f >= 0 && s.Produces && s.Recipe.Family == Defs[f].Requer) r += ', 'if (false) r += ',
        'a Vulcana não acelera a fornalha de liga'),
    mut('liga-vulcana-toda-familia', EQUIPE, 'if (k < 0 || !s.Produces || Defs[k].Station != s.Recipe.Family) return 0;', 'if (k < 0 || !s.Produces) return 0;',
        'a Vulcana dá qualidade em toda bancada (forjas também)'),
    mut('liga-cond-sem-valor', MW, 'case CondicaoMw.PecasComLiga: return i < 0 ? 0 : _sim.PecasComLiga[i];', 'case CondicaoMw.PecasComLiga: return 0;',
        'a condição AlloyPieces da obra-prima nunca anda'),
    mut('liga-validator-slot', CV, 'if (Array.FindAll(r.Inputs, x => x.Qty == 0).Length > (w.IsProduct(r.Output) && Array.Exists(r.Inputs, x => x.Qty > 0) ? 1 : 0))', 'if (false)',
        'validator aceita insumo opcional repetido ou em insumo'),
    mut('liga-validator-fundidor', CV, 'if (!w.Stations.Exists(s => s.Produces && s.Kind == Kind.Furnace && s.Recipe.Family == d.Requer))', 'if (false)',
        'validator aceita fundidor sem a fornalha que ele opera'),
    mut('liga-qmax-sem-liga', MWDEFS, '+ (liga ? t.LigaQ : 0);', '+ 0;',
        'o q máximo da família ignora a liga (as obras da Row voltam a Superior inalcançável)'),
    mut('row-niveis-na-producao', DIST, 'U(21, "nivel_montagem2", 17, ', 'U(21, "nivel_montagem2", 0, ',
        'o nível II da montagem no meio da produção (o desvio do §35 desfeito)'),
    mut('row-sem-projetos', DIST, 'Projetos = ProjetosDefs.RowDefs,', 'Projetos = new BlueprintDef[0],',
        'a Row sem os 14 projetos dela'),
    mut('row-vulcana-sem-requisito', DIST, '7200, "fornalha_liga", new long[]', '7200, null, new long[]',
        'a Vulcana à venda antes da fornalha de liga'),
    mut('row-texto-item-do-oldforge', TEXTOS, 'static string ItemDe(Sim s, int i, long n) => Loc.P("item." + s.ItemDefs[i].Id, n);',
        'static string ItemDe(Sim s, int i, long n) => Loc.P("item." + ItemId[Math.Min(i, ItemId.Length - 1)], n);',
        'projeto e Hall da Row com o nome do item do Old Forge (o handle 8 virava espada longa)'),
    # ---- artisan_quarter (C-CORE-06): bancada configuravel, clientes por familia e o Quarter (ArtisanTests; BALANCE.md §37)
    mut('aq-troca-perde-insumo', BANC, 'for (int k = 0; k < s.Ins.Length; k++) s.Guardados[(int)s.Recipe.Inputs[k].Item] += s.Ins[k];', ';',
        'a troca de receita joga fora o que estava na entrada'),
    mut('aq-troca-sem-parada', BANC, 's.Parada = TempoTroca(s);', 's.Parada = 0f;',
        'a bancada troca de receita sem ficar parada'),
    mut('aq-troca-nao-espera-saida', BANC, 'if (s.Out > 0) return true;', 'if (false) return true;',
        'troca com a pilha da receita antiga na saida (as pecas viram o produto novo)'),
    mut('aq-sem-troca-no-produce', SIM, 'if (Trocando(s, dt)) return;', ';',
        'o Produce nunca aplica a troca pedida'),
    mut('aq-troca-rapida-ignorada', BANC, '(_trocaRapida >= 0 && Bought[_trocaRapida] ? TrocaRapidaSeg : TrocaSeg)', 'TrocaSeg',
        'a troca rapida nao encurta a troca'),
    mut('aq-lucia-sem-troca', BANC, '(Equipe.TemOficial(s) ? TrocaOficialSeg : 0f)', '0f',
        'a Lucia nao tira 1 s da troca'),
    mut('aq-receita-sem-trava', BANC, 'public bool Liberada(Station s, int k) => s.Receitas[k].Abre == null || s.AbreReceita[k] >= 0 && Bought[s.AbreReceita[k]];',
        'public bool Liberada(Station s, int k) => true;', 'toda receita da bancada liberada desde o kit'),
    mut('aq-linha-so-ativa', SIM, 'if (s.Unlocked && Faz(s, product)) return true;', 'if (s.Unlocked && s.Produces && s.OutItem == product) return true;',
        'a linha so abre com a receita ativa (cliente e encomenda nao veem as outras)'),
    mut('aq-entrada-pequena', BANC, 's.InCap = Math.Max(Balance.CrafterIn, 2 * maxQ);', 's.InCap = Balance.CrafterIn;',
        'a entrada da bancada fica em 4 (o castical pede 3; o catalogo diz ate 6 por tipo)'),
    mut('aq-save-sem-bancada', BANC, 'sb.Append("ofc").Append(s.Index)', 'if (false) sb.Append("ofc").Append(s.Index)',
        'save sem a receita e os guardados da bancada'),
    mut('aq-load-ignora-bancada', SIM, 'sim.CarregaBancadas(text);', ';',
        'load ignora a linha ofcN='),
    mut('aq-familia-sem-rodizio', CLI, 'int alvo = _famN[f] % n;', 'int alvo = 0;',
        'a familia pede sempre o 1o produto da lista (sem rodizio)'),
    mut('aq-familia-sem-intervalo', CLI, '_famT[f] += ClientInterval(it) * fs[f].Intervalo', '_famT[f] += ClientInterval(it)',
        'o x1,3 / x2,5 / x3 da familia nao vale no intervalo'),
    mut('aq-familia-sem-paciencia', CLI, 'float pat = T.PatienceFor(it, RecipeDefs) * Equipe.Paciencia * fs[f].Paciencia;', 'float pat = T.PatienceFor(it, RecipeDefs) * Equipe.Paciencia;',
        'a paciencia da familia nao vale'),
    mut('aq-mercador-leva-1', CLI, 'Pack = fs[f].Unidades,', 'Pack = 1,',
        'o mercador leva 1 por visita'),
    mut('aq-pacote-de-uma-vez', SIM, 'if (c.Pack > 1) { Sell(c.Want, Slot(jewel, i)); c.Pack--; break; }', ';',
        'o mercador sai depois da 1a peca (o pacote de 2 nao vale)'),
    mut('aq-colecionador-aceita-normal', CLI, 'if (c.MinQ <= Quality.Normal) return true;', 'return true;',
        'o colecionador leva a peca Normal'),
    mut('aq-ricos-sem-contador', CLI, 'Distrito.Clientes[c.Familia].Rico) ClientesRicos++;', 'Distrito.Clientes[c.Familia].Rico) { }',
        'nobre e colecionador atendidos nao contam no RichClients'),
    mut('aq-ricos-save', CLI, 'if (ClientesRicos > 0) sb.Append("ricos=")', 'if (false) sb.Append("ricos=")',
        'save sem o RichClients'),
    mut('aq-familia-sem-renome', CLI, 'return RenomeDefs.NivelQueLibera(id) < 0 || Renome.Libera(id);', 'return true;',
        'o colecionador chega antes do Renome 2'),
    mut('aq-familia-qualidade-livre', CLI, 'if (min <= Quality.Normal) return true;', 'return true;',
        'o colecionador chega sem Fina na automacao (prende a vaga)'),
    mut('aq-bot-sem-histerese', BOTB, '(nMelhor >= atual + Histerese || foraDaPolitica)', '(nMelhor > atual || foraDaPolitica)',
        'o bot troca de receita por 1 cliente a mais'),
    mut('aq-bot-empate-sem-ouro', BOTB, 'if (n > nMelhor || n == nMelhor && o > oMelhor)', 'if (n > nMelhor)',
        'empate na fila sem o desempate por ouro por insumo'),
    mut('aq-tecnica-item', PROJ, 'public int TecnicaItem(string item) => Libera(BlueprintKind.Technique, item) ? _sim.T.TecnicaItemQ : 0;',
        'public int TecnicaItem(string item) => 0;', 'a tecnica da lapidacao nao soma no colar e na caixa'),
    mut('aq-ricos-cond-sem-valor', MW, 'case CondicaoMw.ClientesRicos: return _sim.ClientesRicos;', 'case CondicaoMw.ClientesRicos: return 0;',
        'a condicao RichClients da obra-prima nunca anda'),
    mut('aq-gemas-sem-valor', MW, 'f += (long)s.Crafted * s.Recipe.Qtd;', ';',
        'a condicao GemsCut da obra-prima nunca anda'),
    mut('aq-chegada-so-anterior', IMP, 'cadeia.Insert(0, x);', 'if (cadeia.Count == 0) cadeia.Insert(0, x);',
        'a chegada ao Quarter so abre a Row (sem o Old Forge no Charter)'),
    mut('aq-validator-bancada', BANC, 'if (s.Receitas[0].Abre != null) yield return', 'if (false) yield return',
        'validator aceita a receita de fabrica travada'),
    mut('aq-validator-familia', CLI, 'if (i < 0 || !w.IsProduct((Item)i) || !w.HasClients((Item)i)) yield return', 'if (false) yield return',
        'validator aceita familia pedindo produto que o distrito nao tem'),
    mut('aq-vitrine-sem-vagas', SIM, 'if (JewelShop == null && Has(Upgrade.JewelVitrine)) QueueCap += Balance.JewelQueueCapUp - Balance.JewelQueueCap;', ';',
        'a vitrine de luxo nao abre as vagas 4 e 5 da loja fina'),
    mut('aq-preco-do-catalogo', AQ, 't.Price = new[] { 0, 0, 330, 570, 870, 720, 900, 1050, 1350, 0, 0, 0, 0 };', 't.Price = new[] { 0, 0, 110, 190, 290, 240, 300, 350, 450, 0, 0, 0, 0 };',
        'o Quarter com os precos do catalogo (o desvio x3 desfeito)'),
    mut('aq-custo-do-catalogo', AQ, '2160, 2805, 3655, 4750, 6175, 8020, 10000, 14000, 18000, 23000, 30000, 39000, 51000, 66000, 86000, 110000, 140000, 180000,',
        '1440, 1870, 2435, 3165, 4115, 5345, 6950, 9035, 12000, 15000, 20000, 26000, 34000, 44000, 57000, 74000, 96000, 120000,',
        'o Quarter com os custos do catalogo (o desvio x1,5 desfeito)'),
    mut('aq-niveis-do-catalogo', AQ, '36000, 105000, 290000, 630000,', '24000, 71000, 190000, 420000,',
        'os niveis da ourivesaria do catalogo (o Charter do Ideal abaixo das 4 h)'),
    mut('aq-alvo-de-encomenda', AQ, 't.OrderRewardSeconds = 8.5f; t.OrderSeconds = 180f;', 't.OrderRewardSeconds = 8.5f;',
        'a encomenda do Quarter com o alvo de sempre (150 s: o Renome 6 cedo demais)'),
    mut('aq-row-sem-proximo', DIST, 'Charter = new CharterDef(true, 750, 2, ArtisanQuarterId),', 'Charter = new CharterDef(true, 750, 2, null),',
        'o Charter da Row nao libera o Quarter'),
    mut('aq-rotulo-sem-distrito', TEXTOS, '_est = Array.ConvertAll(d.Estacoes, x => Loc.Tem("est." + d.Id + "." + x.Id) ? d.Id + "." + x.Id : x.Id);',
        '_est = Array.ConvertAll(d.Estacoes, x => x.Id);', 'o deposito do Quarter com o rotulo do Old Forge'),
    # ---- armorers_row: pendências (row-pendencias, BALANCE.md §39)
    mut('rowp-ferraz4-papel0', DIST, 'Ferraz(4, 1), Vulcana', 'Ferraz(4, 0), Vulcana',
        'o Ferraz 4 de volta ao bruto (papel 0)'),
    mut('rowp-pega-a-mao-cheia', TRANSP, '|| c.Held[(int)i] < Cabe(i);', '|| true;',
        'o papel 1 pega a mão cheia mesmo sem vaga nas entradas'),
    mut('rowp-fonte-sem-destino', TRANSP, '((w.Count > 0 || Cabe(s.OutItem) > 0) && (s.Out > 0 || s.Busy))', '(s.Out > 0 || s.Busy)',
        'o papel 1 vai à fonte do insumo que não cabe em lugar nenhum (fica com a placa na mão)'),
    mut('rowp-espera-fonte-parada', TRANSP, '((w.Count > 0 || Cabe(s.OutItem) > 0) && (s.Out > 0 || s.Busy))', '(w.Count > 0 || Cabe(s.OutItem) > 0)',
        'o papel 1 espera na boca da estação parada (a fornalha de liga sem lingote)'),
    mut('rowp-regras-no-oldforge', TRANSP, 'return _insumos >= 2;', 'return _insumos >= 1;',
        'as regras novas valem no Old Forge (um insumo): muda o jogo de lá'),
    mut('rowp-sem-gancho-boca', SIM, '&& CanPick(c, s.OutItem) && PegaInsumo(c, s.OutItem) ? 2 : 0;', '&& CanPick(c, s.OutItem) ? 2 : 0;',
        'a boca de saída ignora o que cabe'),
    mut('rowp-sem-gancho-escolha', SIM, 'if (!FonteDeInsumo(w, s)) continue;', ';',
        'a escolha de fonte ignora as regras do papel 1'),
    mut('rowp-sem-gancho-alvo', SIM, '(s.Out > 0 || w.Target == s.Index)\n                && FonteDeInsumo(w, s);', '(s.Out > 0 || w.Target == s.Index);',
        'o alvo do papel 1 segue valendo na estação parada'),
    mut('rowp-rebite-catalogo', DIST, 'Rebite, 4, t => 8f,', 'Rebite, 4, t => 1f,',
        'rebite em 4 s (o catálogo): uma rebitadeira dá conta e a 2ª não faz falta'),
    mut('rowp-preco-x3-5', DIST, 't.Price = new[] { 0, 0, 335, 290, 310, 170, 290, 265, 550, 0, 0, 0, 0 };',
        't.Price = new[] { 0, 0, 490, 420, 455, 245, 420, 385, 805, 0, 0, 0, 0 };',
        'o preço da Row de antes (x 3,5)'),
    mut('rowp-custo-x1-55', DIST, '530, 690, 890, 1160, 1505, 1960, 2545, 3310, 4305, 5600, 7275, 9460, 12000, 17000, 21000, 28000, 35000, 46000,',
        '745, 970, 1255, 1635, 2125, 2760, 3590, 4665, 6070, 7890, 10000, 13000, 17000, 23000, 29000, 39000, 50000, 65000,',
        'o custo da Row de antes (x 1,55)'),
    mut('rowp-texto-preco-catalogo', LOC, '"Nova linha: peitoral, paga 550"', '"Nova linha: peitoral, paga 230"',
        'o texto da linha nova com o preço do catálogo (o de antes)'),
    # ---- royal_arsenal (C-CORE-07): patio de cotas, mesa real, Charter, selo, transporte e o bot (ArsenalTests; BALANCE.md §38)
    mut('ra-teto-para-baixo', COTAS, 'static long Teto(long a, long b) => (a + b - 1) / b;', 'static long Teto(long a, long b) => a / b;',
        'o pagamento do bloco arredonda para baixo (fora do prazo paga menos que 1,3x)'),
    mut('ra-prazo-pune', COTAS, 'long pago = Teto(f.Soma * T.CotaPct, 100);', 'long pago = Teto(f.Soma * (prazo ? T.CotaPct : 100), 100);',
        'fora do prazo o bloco paga 1,0x (o prazo vira penalidade)'),
    mut('ra-prazo-sem-bonus', COTAS, 'if (prazo) pago = Teto(pago * (100 + T.CotaBonusPct), 100);', ';',
        'no prazo o bloco nao paga os +25%'),
    mut('ra-prazo-sempre', COTAS, 'bool prazo = f.Decorrido <= CotaPrazo;', 'bool prazo = true;',
        'todo bloco conta como no prazo'),
    mut('ra-matias-sem-prazo', COTAS, 'public float CotaPrazo => Equipe.Contratados(WorkerRole.Quartermaster) > 0 ? T.CotaPrazoQm : T.CotaPrazo;',
        'public float CotaPrazo => T.CotaPrazo;', 'o Matias nao estica o prazo para 240 s'),
    mut('ra-bloco-sem-passo', COTAS, 'public int TamanhoCota(int p) => Math.Min(T.CotaBlocoMax, T.CotaBloco0 + T.CotaBlocoPasso * CotaBlocos[p]);',
        'public int TamanhoCota(int p) => Math.Min(T.CotaBlocoMax, T.CotaBloco0);', 'o bloco nao cresce +2 a cada fechado'),
    mut('ra-bloco-sem-teto', COTAS, 'public int TamanhoCota(int p) => Math.Min(T.CotaBlocoMax, T.CotaBloco0 + T.CotaBlocoPasso * CotaBlocos[p]);',
        'public int TamanhoCota(int p) => T.CotaBloco0 + T.CotaBlocoPasso * CotaBlocos[p];', 'o bloco passa de 30'),
    mut('ra-troca-na-hora', COTAS, 'if (f.Pecas == 0) Abre(f);', 'Abre(f);',
        'trocar o produto descarta o bloco em curso (a troca nao espera o proximo bloco)'),
    mut('ra-cota-nao-e-venda', COTAS, 'Gold += pago; GoldEarned += pago;', 'Gold += pago;',
        'o pagamento do bloco fica fora do GoldEarned (nao e venda)'),
    mut('ra-cota-sem-renome', COTAS, 'Renome.Ganha(T.CotaRenome + (prazo ? T.CotaRenomePrazo : 0));', ';',
        'o bloco fechado nao da Renome'),
    mut('ra-cota-sem-contador', COTAS, 'CotaBlocos[p]++;', ';',
        'QuotaBlocks nao conta (o bloco nao cresce e a obra da patrulha nunca abre)'),
    mut('ra-cota-sem-encomenda', COTAS, 'Encomendas.OnVenda((Item)p, q); ', '',
        'a peca vendida a Coroa nao conta para a encomenda'),
    mut('ra-carregador-balcao', COTAS, 'public Station Destino(Item p) => CotaAceita(p) ? Patio : CounterFor(p);',
        'public Station Destino(Item p) => CounterFor(p);', 'o carregador leva tudo ao balcao, mesmo com faixa'),
    mut('ra-faixas-sem-upgrade', COTAS, 'return 1 + (_uF2 >= 0 && Bought[_uF2] ? 1 : 0) + (_uF3 >= 0 && Bought[_uF3] ? 1 : 0);', 'return 1;',
        'patio_faixa2/3 nao abrem faixa'),
    mut('ra-offline-anda-prazo', SIM, 'OfflineEarned += gold;', 'OfflineEarned += gold; TickCotas(capped);',
        'o offline anda o prazo dos blocos'),
    mut('ra-offline-fecha', SIM, 'OfflineEarned += gold;', 'OfflineEarned += gold; for (int k = 0; k < FaixasAbertas; k++) if (Faixas[k].Pecas > 0) FechaCota(k);',
        'o offline fecha o bloco em curso'),
    mut('ra-save-sem-cotab', COTAS, 'if (Array.Exists(CotaBlocos, x => x > 0)) sb.Append("cotab=")', 'if (false) sb.Append("cotab=")',
        'o save nao grava os blocos fechados por produto'),
    mut('ra-load-sem-faixas', COTAS, 'f.Produto = Produto(p[0]); f.Proximo = Produto(p[1]);', 'f.Produto = -1; f.Proximo = -1;',
        'o load perde o produto das faixas'),
    mut('ra-mesa-sem-vaga', COTAS, 'if (_uMesa >= 0 && Bought[_uMesa]) Contratos.MesaReal = true;', ';',
        'a mesa_real nao da a 2a vaga de contrato'),
    mut('ra-contrato-uma-vaga', CTR, '(Ativo == null || MesaReal && Ativo2 == null)', '(Ativo == null)',
        'com a mesa real ainda so 1 contrato por vez'),
    mut('ra-royal-vira-common', ENC, 'if (d.Requer != null && !_sim.Comprado(d.Requer)) return false;', ';',
        'a Royal Normal da mesa real abre sem a mesa'),
    mut('ra-royal-todo-distrito', COTAS, 'if (e.Tipo == OrderType.Royal && Distrito.Charter != null && Distrito.Charter.Contratos > 0) RoyalEntregues++;',
        'if (e.Tipo == OrderType.Royal) RoyalEntregues++;', 'a Royal conta para o Charter em distrito que nao pede contrato'),
    mut('ra-charter-sem-contrato', IMP, 'if (c.Contratos > 0 && Raiz.Contratos.Concluidos.Count < c.Contratos && s.RoyalEntregues < c.Contratos * CharterDef.RoyalPorContrato)',
        'if (false)', 'o Charter do Arsenal sai sem contrato nem Royal'),
    mut('ra-charter-sem-royal', IMP, '&& s.RoyalEntregues < c.Contratos * CharterDef.RoyalPorContrato)', ')',
        'as 10 Royal nao valem o contrato (o contrato bloqueia o Charter)'),
    mut('ra-selo-sem-q', COTAS, 'return _uSelo >= 0 && Bought[_uSelo] ? T.SeloQ : 0;', 'return 0;',
        'o selo do Arsenal nao da qualidade'),
    mut('ra-qmax-sem-selo', MWDEFS, 'static int SeloQ(Sim w, UpgradeDef[] ups) => w.Distrito.Selo != null && Upgrades.IndexOf(w.Distrito.Selo, ups) >= 0 ? w.T.SeloQ : 0;',
        'static int SeloQ(Sim w, UpgradeDef[] ups) => 0;', 'o q maximo ignora o selo (as obras do Arsenal viram Superior inalcancavel)'),
    mut('ra-validator-patios', CV, 'if (patios > 1) yield return', 'if (false) yield return',
        'o validator aceita 2 patios'),
    mut('ra-validator-cota-sem-patio', MWDEFS, 'ok &= Produto(c.Ref) >= 0 && w.Patio != null; break;', 'ok &= Produto(c.Ref) >= 0; break;',
        'obra com condicao de cota aceita em distrito sem patio'),
    mut('ra-carga-sem-prioridade', SIM, '+ (w.Role == 1 && Distrito.InsumoPorEspera ? 10f * Esperam(s.OutItem) : 0f)', '',
        'o cadete de insumo nao prioriza o que as estacoes esperam'),
    mut('ra-bot-sem-faixa', BOT, 'Cotas(s);   // C-CORE-07', ';   // C-CORE-07',
        'o bot nunca escolhe as faixas'),
    mut('ra-bot-mais-procura', BOTCOTAS, 'procura[a].CompareTo(procura[b])', 'procura[b].CompareTo(procura[a])',
        'o bot poe nas faixas as linhas de MAIS procura'),
    mut('ra-bot-sem-linha-livre', BOTCOTAS, 'var quer = linhas.GetRange(0, System.Math.Max(0, System.Math.Min(n, linhas.Count - 1)));',
        'var quer = linhas.GetRange(0, System.Math.Min(n, linhas.Count));', 'o bot nao deixa linha so para o balcao'),
    mut('ra-quarter-sem-arsenal', AQ, 'Charter = new CharterDef(true, 1250, 3, RoyalArsenalId),', 'Charter = new CharterDef(true, 1250, 3, null),',
        'o Charter do Quarter nao libera o Arsenal'),
    mut('ra-procura-sem-familia', COTAS, 'if (fs == null) return 1f / ClientInterval(i);', 'if (true) return 1f / ClientInterval(i);',
        'a procura do bot ignora as familias do distrito (volta ao intervalo do produto)'),
    mut('ra-procura-sem-rodizio', COTAS, 'foreach (int j in _famPede[f]) if (PedeAgora(f, j)) soma += ClientInterval((Item)j);', 'soma = ClientInterval(i);',
        'a procura nao divide o relogio da familia pelos produtos do rodizio'),
    mut('ra-emissario-sem-fina', RADIST, 'new ClientDef("emissario", 2, 2f, 2f, Quality.Fine, false,', 'new ClientDef("emissario", 2, 2f, 2f, Quality.Normal, false,',
        'o emissario real aceita qualquer qualidade'),
    mut('ra-sem-familias', RADIST, 'Clientes = ClientesArsenal,', '',
        'o Arsenal volta ao relogio por produto (sem as familias do §12)'),
    mut('ra-preco-do-catalogo', RADIST, 't.Price = new[] { 0, 0, 1705, 2880, 1320, 1970, 2330, 4560, 0, 0, 0, 0, 0 };',
        't.Price = new[] { 0, 0, 710, 1200, 550, 820, 970, 1900, 0, 0, 0, 0, 0 };', 'os precos do Arsenal voltam ao catalogo (sem o x2,4)'),
    # ---- arcane_foundry (C-CORE-08): o medidor dos altares (Sim.Arcano.cs, Bot.Arcano.cs) e os dados da Foundry (Distritos.ArcaneFoundry.cs)
    mut('af-estavel-sem-espaco', ARC, '(s.Pausa > 0f || !s.Forcado && s.Medidor + CargaPeca(s) > T.ArcanoTeto)', '(s.Pausa > 0f)',
        'o Estavel comeca a peca sem espaco no medidor (passa de 100)'),
    mut('af-pausa-nao-segura', ARC, '(s.Pausa > 0f || !s.Forcado && s.Medidor + CargaPeca(s) > T.ArcanoTeto)', '(!s.Forcado && s.Medidor + CargaPeca(s) > T.ArcanoTeto)',
        'o altar em sobrecarga comeca a peca (a pausa nao segura)'),
    mut('af-forcado-carga-simples', ARC, 's.Medidor += s.Forcado ? 2 * c : c;', 's.Medidor += c;',
        'o Forcado soma 1x a carga (nao 2x)'),
    mut('af-estavel-carga-dobrada', ARC, 's.Medidor += s.Forcado ? 2 * c : c;', 's.Medidor += 2 * c;',
        'o Estavel tambem soma 2x a carga'),
    mut('af-energiza-sempre', ARC, 's.Energizada = s.Forcado; s.Medidor', 's.Energizada = true; s.Medidor',
        'a peca Estavel tambem sai energizada'),
    mut('af-sem-sobrecarga', ARC, 's.Sobrecarrega = s.Forcado && s.Medidor > T.ArcanoTeto;', 's.Sobrecarrega = false;',
        'o Forcado passa de 100 e nunca sobrecarrega'),
    mut('af-sobrecarga-perde-peca', ARC, 's.Pausa = T.SobrecargaSeg; Sobrecargas++; }', 's.Pausa = T.SobrecargaSeg; Sobrecargas++; s.Out = Math.Max(0, s.Out - 1); }',
        'a sobrecarga joga fora a peca que terminou'),
    mut('af-sobrecarga-perde-insumo', ARC, 's.Pausa = T.SobrecargaSeg; Sobrecargas++; }', 's.Pausa = T.SobrecargaSeg; Sobrecargas++; Array.Clear(s.Ins, 0, s.Ins.Length); }',
        'a sobrecarga joga fora o insumo que esperava na entrada'),
    mut('af-sobrecarga-sem-pausa', ARC, 's.Pausa = T.SobrecargaSeg;', 's.Pausa = 0f;',
        'a sobrecarga nao pausa o altar'),
    mut('af-pausa-sem-reset', ARC, '{ s.Pausa = 0f; s.Medidor = T.SobrecargaE; s.RestoDreno = 0f; }', '{ s.Pausa = 0f; s.RestoDreno = 0f; }',
        'depois da pausa o medidor nao volta a 40'),
    mut('af-sequencia-nao-zera', ARC, '{ s.Sequencia = 0; s.Pausa', '{ s.Pausa',
        'a sobrecarga nao zera a StableStreak'),
    mut('af-estavel-zera-sequencia', ARC, 'else s.Sequencia++;', 'else s.Sequencia++; } else { s.Sequencia = 0;',
        'a peca Estavel zera a StableStreak'),
    mut('af-troca-na-peca-em-curso', ARC, 'if (!s.Busy) s.Energizada = forcado;', 's.Energizada = forcado;',
        'trocar o modo no meio da peca muda a peca em curso'),
    mut('af-energizada-sem-q', ARC, 'int QualidadeEnergizada(Station s) => s.Energizada && Arcana(s) ? T.EnergizadaQ : 0;', 'int QualidadeEnergizada(Station s) => 0;',
        'a peca energizada nao ganha q +20'),
    mut('af-breakdown-sem-energizada', SIM, 'b[(int)QualitySource.Energizada] = QualidadeEnergizada(s);', ';',
        'o QualityBreakdown nao tem a fonte energizada'),
    mut('af-energizada-nao-conta', ARC, 'PecasEnergizadas[(int)s.OutItem] += s.Recipe.Qtd;', ';',
        'EnergizedPieces nunca anda'),
    mut('af-dreno-parado', ARC, 'float x = d * dt + s.RestoDreno;', 'float x = 0f;',
        'o medidor nunca cai'),
    mut('af-dreno-sem-condensador', ARC, 'if (_uCond >= 0 && Bought[_uCond]) d += T.DrenoCondensador;', ';',
        'o 1o condensador nao mexe no dreno'),
    mut('af-dreno-sem-isis', ARC, 'if (n > 0) d += T.DrenoIsis + T.DrenoIsisPasso * ((n - 1) / 2);', 'if (n > 0) d += T.DrenoIsis;',
        'o nivel da Isis nao sobe o dreno'),
    mut('af-offline-drena', SIM, 'OfflineEarned += gold;', 'OfflineEarned += gold; TickArcano(capped);',
        'o offline drena o medidor e anda a pausa'),
    mut('af-save-sem-medidor', ARC, 'if (!Arcana(s) || s.Medidor == 0 && !s.Forcado', 'if (true || s.Medidor == 0 && !s.Forcado',
        'o save nao grava os altares (arcN=)'),
    mut('af-save-sem-arce', ARC, 'if (Array.Exists(PecasEnergizadas, n => n > 0)) sb.Append("arce=")', 'if (false) sb.Append("arce=")',
        'o save nao grava EnergizedPieces'),
    mut('af-load-ignora', SIM, 'sim.LoadArcano(text);', ';',
        'o load ignora os medidores'),
    mut('af-load-energizada-parado', ARC, 's.Energizada = s.Busy ? p[2].Trim() == "1" : s.Forcado;', 's.Energizada = p[2].Trim() == "1";',
        'o load deixa energizada a peca de um altar parado no Estavel'),
    mut('af-isis-sem-altar', EQUIPE, ' || Defs[k].Especialidade == Specialty.Arcane && _sim.Arcana(s)', '',
        'a Isis nao vale em nenhum altar'),
    mut('af-qmax-sem-arcano', MWDEFS, '+ SeloQ(w, ups) + w.QMaxArcano(s) > 100', '+ SeloQ(w, ups) > 100',
        'o validator nao conta a energizada e a Isis no Superior dos altares'),
    mut('af-validator-carga-limite', ARC, 'else if (c[i] < 0 || 100 * c[i] > w.T.ArcanoTeto) yield return', 'else if (false) yield return',
        'o validator aceita carga acima de 100 (o Estavel travaria)'),
    mut('af-validator-carga-insumo', ARC, 'if (!w.IsProduct((Item)i)) yield return', 'if (false) yield return',
        'o validator aceita carga em insumo'),
    mut('af-validator-dreno', ARC, 'if (w.T.DrenoBase <= 0) yield return', 'if (false) yield return',
        'o validator aceita dreno base 0'),
    mut('af-validator-isis', CV, 'if (d.Station != null || !w.TemArcano) yield return', 'if (false) yield return',
        'o validator aceita a oficial arcana com familia'),
    mut('af-validator-energizadas', MWDEFS, 'ok &= pe >= 0 && w.Stations.Exists(s => w.Arcana(s) && (int)s.OutItem == pe); break;', 'ok &= pe >= 0; break;',
        'o validator aceita EnergizedPieces fora de altar'),
    mut('af-validator-sequencia', MWDEFS, 'ok &= w.Stations.Exists(s => s.Id == c.Ref && w.Arcana(s)); break;', 'ok &= true; break;',
        'o validator aceita StableStreak fora de altar'),
    mut('af-mw-energizadas-parada', MW, 'case CondicaoMw.PecasEnergizadas: return i < 0 ? 0 : _sim.PecasEnergizadas[i];', 'case CondicaoMw.PecasEnergizadas: return 0;',
        'a condicao EnergizedPieces da obra-prima nunca anda'),
    mut('af-mw-sequencia-parada', MW, 'return a == null ? 0 : a.Sequencia;', 'return 0;',
        'a condicao StableStreak da obra-prima nunca anda'),
    mut('af-marco-energizadas', PROJ, 'if (f.Ref == BlueprintSource.Energizadas) return _sim.EnergizadasTotal >= f.N;', 'if (f.Ref == BlueprintSource.Energizadas) return false;',
        'o marco das 500 energizadas nunca da o projeto'),
    mut('af-bot-sem-altar', BOT, 'Arcano(s);   // C-CORE-08', ';   // C-CORE-08',
        'o bot nunca mexe no modo dos altares'),
    mut('af-bot-sempre-forcado', BOTARC, 'Altares == PoliticaArcana.Regra && a.Medidor + 2 * s.CargaPeca(a) <= s.T.ArcanoTeto', 'Altares == PoliticaArcana.Regra',
        'a regra do bot fica sempre no Forcado (sobrecarrega)'),
    mut('af-bot-so-parado', BOTARC, 'if (!a.Unlocked || !s.Arcana(a)) continue;', 'if (!a.Unlocked || a.Busy || !s.Arcana(a)) continue;',
        'o bot so escolhe o modo com o altar parado (a peca seguida sobrecarrega)'),
    mut('af-arsenal-sem-foundry', RADIST, 'Charter = new CharterDef(true, 2000, 3, ArcaneFoundryId, contratos: 1),', 'Charter = new CharterDef(true, 2000, 3, null, contratos: 1),',
        'o Charter do Arsenal nao libera a Foundry'),
    mut('af-charter-um-contrato', AFDIST, 'Charter = new CharterDef(true, 3000, 4, null, contratos: 2),', 'Charter = new CharterDef(true, 3000, 4, null, contratos: 1),',
        'o Charter da Foundry pede 1 contrato (10 Royal)'),
    mut('af-sem-familias', AFDIST, 'Clientes = ClientesArcane,', '',
        'a Foundry volta ao relogio por produto (sem as familias do §12)'),
    mut('af-ciclo-do-catalogo', AFDIST, 'new[] { Qual, Royal, Qual, Bulk, Mixed, Rush, Qual, Blueprint, Common, Royal };',
        'new[] { Qual, Common, Qual, Bulk, Mixed, Rush, Qual, Blueprint, Common, Royal };', 'o rodizio da Foundry volta ao do catalogo (Royal x1)'),
    mut('af-sem-insumo-por-espera', AFDIST, 'InsumoPorEspera = true,', 'InsumoPorEspera = false,',
        'o carregador de insumo da Foundry nao olha o que os altares esperam'),
    mut('af-sem-manager', IMP, 'new WorkerDef("wk_sibila",', 'new WorkerDef("wk_sibila_x",',
        'a Foundry sem a Mestra Sibila'),
    mut('af-carga-trocada', AFDIST, 't.CargaArcana = new[] { 0, 0, 15, 12, 18, 20, 25, 22, 30, 0, 0, 0, 0, 0 };',
        't.CargaArcana = new[] { 0, 0, 12, 15, 18, 20, 25, 22, 30, 0, 0, 0, 0, 0 };', 'lanterna e anel com as cargas trocadas'),
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
