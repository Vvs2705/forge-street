# IAP — Forge Street (E-PLAT-04, E-PLAT-05, E-PLAT-07)

Atualizado: 2026-10-13 · Estado: **núcleo + loja falsa de desenvolvimento, sem SDK**. O pacote Unity IAP (`com.unity.purchasing`) não está instalado: baixar é download externo e pede o aval do Vinicius. Plugar o SDK = escrever uma implementação nova da interface `IStore` (§5); nada do núcleo muda.

Código: `client/Assets/_FS/Scripts/Core/Compras.cs` (catálogo, validator, ledger, estados, save), `client/Assets/_FS/Scripts/View/Loja.cs` (`IStore`, `LojaFalsa`, `PainelLoja`), `Game.cs` (fluxo). Testes: `Tests/EditMode/ComprasTests.cs` (16) e as linhas da loja no `LocTests` e no `CosturasTests`. Mutações `iap-*` no `client/tools/mutantes/mutantes.py`.

## 1. Catálogo

| ID (= ID no Play Console) | Tipo | Conteúdo (fixo, mostrado antes de pagar) | Preço | Estado |
|---|---|---|---|---|
| `iap_pacote_mestre` | não-consumível | `remove_ads`: veto do intersticial (`Anuncios.VetoIntersticial`); `sem_video`: Velocidade, Lingotes (Supply Drop) e Trocar encomenda **sem o vídeo**, com as mesmas recargas, teto diário e ledger | **placeholder** R$ 14,90 (`ProductDef.PrecoPlaceholder`; GDD §17.3 propõe US$ 2,99–4,99) | decidido por padrão, **revisar** com a P3 |

**Por que esse conteúdo (revisar):** o ticket pede o mínimo seguro sem 2ª moeda (V9) nem ouro/Renome/XP/Blueprint pagos (V20). O GDD §17.3 previa "cofre 2× permanente + skin de oficina" para o caso de o intersticial ficar desligado; o Cofre 2× é ouro direto, então ficou com vídeo, como o Chamar VIP e o bônus da encomenda. O `sem_video` cobre só as conveniências que a V20 lista para o premium (Supply, Velocidade, Trocar): quem compra ganha o que quem assiste aos anúncios já ganha, sem os ~30 s de cada vídeo; D6 (nada antes da 1ª melhoria), recarga e teto de 30/dia continuam. A skin entra quando existir cosmético (DM §48). Enquanto a flag do intersticial estiver desligada (V10), a linha "Sem anúncio entre telas" não promete nada visível: rever o texto ou o conteúdo na P3. Medido no econsim (`--pacote 1`, `BALANCE.md` §25): quem não compra fica idêntico ao byte; quem compra poupa o tempo dos vídeos (produção completa 41–47 min contra 45–52 min assistindo) e nenhum piso do §3 cai.

**Validator** (`Compras.Validar`, chamado pelo `ContentValidator.Validate`): ID `iap_[a-z0-9_]` único; preço de referência presente (placeholder até a Wave E); assinatura recusada (fora da v1.0, DM §43); conteúdo não vazio, quantidade fixa ≥ 1, sem item repetido; posse só das conhecidas (`Compras.Posses`) e nunca em consumível (a loja não restaura consumível); moeda só das à venda (`Compras.MoedasAVenda`, **vazia** até a Wave E; ouro nunca) e nunca em não-consumível (o restore daria de novo). "Nada aleatório pago" é estrutural: `TipoConteudo` só tem `Posse` e `Moeda` (não existe baú, chance nem sorteio).

## 2. Estados de uma compra

```
toque COMPRAR ── Compras.Iniciar ──> Iniciada (memória; iap_start) ── IStore.Comprar
     │                                          │
     │  falha/cancelada/adiada (iap_fail | iap_pending "adiada") <───┤
     │                                          ▼
     │                              recibo da loja ── Compras.Receber ── inválido? → null: nada muda, NÃO confirma (a loja devolve)
     │                                          ▼
     │                              Pendente (entregue no estado; iap_pending "nova")
     │                                          ▼
     │                              Compras.Concluir: o save GRAVA (Salvamento.Gravar = true) ── falhou? → fica Pendente (iap_fail "save")
     │                                          ▼
     │                              Entregue ── IStore.Confirmar ──> Confirmada (iap_delivered)
```

- **Idempotente pelo ID de transação** (o ledger é o mesmo padrão do `Metas`/`Anuncios`): recibo repetido, restore ou reentrega da loja voltam como `Entregue` (nada a entregar, só confirmar de novo).
- **App morto entre pagar e gravar:** nada da compra está no disco; a loja guarda a compra paga e não confirmada e a reentrega no próximo `Iniciar` → entra como nova.
- **App morto entre gravar e confirmar:** o save tem a linha `iap.tx=…,e,…` (entregue, falta confirmar); a próxima abertura só confirma (`Concluir` sem gravar de novo), com ou sem reentrega da loja.
- **Posse = re-derivada dos recibos válidos** (`Compras.Conectar` depois do load, `Receber` a cada recibo): não existe booleano no save. Save editado com `remove_ads=1`, recibo forjado ou recibo de outra transação não liga nada; sem loja (release sem SDK) nenhum recibo vale. O `Sim.Load` sozinho (bot, econsim, testes) não valida recibo: nenhuma posse.
- **Relógio:** nada em `Compras` lê relógio; voltar o relógio não tira nem duplica compra.

**Save** (sem mudar o schema): uma linha por transação, só com compra (save sem elas = nada comprado; o texto de quem não compra fica igual ao de antes):

```
iap.tx=<tx [A-Za-z0-9._:-], até 512>,<produto>,<e|c>,<carga base64>,<assinatura base64>
```

`e` = entregue, falta confirmar (o `Pendente` sai como `e`: o save que o leva é o que grava a entrega); `c` = confirmada. Lixo é ignorado linha a linha.

**Tetos conhecidos (ponytail):** sem servidor, um recibo válido copiado de outro save vale (compartilhamento de recibo) e um reembolso não revoga a posse já gravada. Os dois pedem validação no servidor ou Real-time Developer Notifications da Google (Wave E/G, se o volume justificar).

## 3. Exploits cobertos (E-PLAT-07, DM §133)

| Ataque / falha | Teste (`ComprasTests`) |
|---|---|
| Compra interrompida (app morto entre pagar e salvar) | `Exploit_CompraInterrompida_AppMortoEntrePagarESalvar_ChegaNaProximaAbertura` |
| App morto entre gravar e confirmar | `Exploit_AppMortoDepoisDoSave_AntesDaConfirmacao_ReabrirSoConfirma` |
| Save que falha não confirma | `SaveQueFalha_NadaConfirmado_ATentativaSeguinteConfirma` (+ `CosturasTests`: `Gravar` devolve false) |
| Compra duplicada (o mesmo recibo 2×) | `Exploit_CompraDuplicada_MesmoRecibo2x_Entrega1x` |
| Restore 2×, restore depois de reinstalar | `Exploit_Restore2x_EDepoisDeReinstalar_DevolveOPacoteUmaVez` |
| Recibo de outro produto, de outro app, forjado, de outra transação, ID fora do formato, sem loja | `Exploit_ReciboDeOutroProdutoOutroAppForjadoOuSemLoja_NaoEntrega` |
| Relógio para trás | `Exploit_RelogioParaTras_NaoTiraNemDuplicaACompra` |
| Save editado (entitlement sem recibo) | `SaveEditado_EntitlementSemReciboValido_NaoVale` |
| Recompensa de anúncio duplicada | já em `AnunciosTests`/`ExploitTests` (mesmo ID, reabrir, oferta que mudou); novo: `Exploit_AnuncioDuplicado_ViradaDoDiaEReabrirNaoReabremOIdPago` (o dia novo zera o teto, não o ID pago) e o toque duplo no `sem_video` |

## 4. View e desenvolvimento

- `IStore` (`Loja.cs`): `Iniciar(produtos, recibo, falha)`, `Preco`, `Comprar`, `Confirmar`, `Restaurar(fim)`; é também o `IValidaRecibo` (cada loja valida a assinatura dela).
- **Sem loja = sem botão:** o Game só cria a loja no build de desenvolvimento com `-lojafalsa`; o release sem SDK não mostra LOJA. A chave `Tuning.Loja` (RemoteConfig, A-PLAT-08) esconde a loja sem rebuild; recibo, restore e posse seguem.
- **Painel:** LOJA no meio da fileira de baixo das Configurações (só com loja). Cartão do produto com o conteúdo listado, o botão com o preço **que a loja devolveu** (sem preço = "…" apagado; comprado = COMPRADO), RESTAURAR COMPRAS e, na loja falsa, o rodapé "Loja de teste: nenhum pagamento real". Textos pt/en/es no `Loc` (`loja.*`, `iap.<id>.nome`, `iap.posse.<id>`, `aviso.compra_*`, `aviso.restaurado`).
- **Flags (só build dev):** `-lojafalsa` (normal), `-lojafalsa interrompe` (paga e o recibo só chega na próxima abertura), `-lojafalsa duplica` (o mesmo recibo 2×), `-lojafalsa recusa` (falha); `-loja` abre o painel. A loja falsa guarda as compras em `persistentDataPath/lojafalsa.txt` (a "conta"); `-reset` apaga o save e não a conta, então reinstalar + RESTAURAR devolve o pacote. No `-testsession` ela fica só na memória.
- **Diário/analytics** (pelo `IAnalytics`/Telemetria, remoto só depois da tela de idade): `iap_start` (produto, preço da loja), `iap_pending` (produto, nova|repetida|adiada), `iap_delivered` (produto, confirmada), `iap_restore` (pedido | fim, n), `iap_fail` (produto, recibo_invalido|recusada|…; ou "save"). O ID de transação **não** vai ao diário nem ao analytics (dado de compra). Anúncio dispensado pelo pacote: `ad_paid` com o sufixo `sem_video`.

## 5. Como plugar o Unity IAP (quando houver aval para o download)

1. **Aval do Vinicius** para baixar `com.unity.purchasing` (Package Manager). Conferir a versão contra o Unity 6000.3 e o `mainTemplate.gradle` (Google Play Billing Library 7+, exigida pela Play desde 2025); rodar `verify.sh full` e o build Android (16 KB, targetSdk 36).
2. Criar `LojaUnity : IStore` em arquivo novo (atrás de `#if UNITY_PURCHASING` ou do pacote no asmdef), nada no núcleo:
   - `Iniciar`: inicializar o IAP **depois da resposta da tela de idade** (em `Game.Liberar`, como o LevelPlay) com os `Compras.Catalogo` (`iap_pacote_mestre` como `ProductType.NonConsumable`).
   - Compra paga (`ProcessPurchase` na 4.x / `OnPurchasePending` na 5.x): montar `Recibo { Tx = transactionID, Produto = definition.id, Payload = product.receipt, Assinatura = "" }`, chamar o `recibo` e devolver **Pending** (nunca Complete). Guardar o `Product`/pedido por `Tx` para o `Confirmar`.
   - `Confirmar(tx)` → `ConfirmPendingPurchase` (4.x) / `ConfirmPurchase` (5.x).
   - Pagamento adiado (Google "pending", ex.: boleto/Pix pendente; `IsPurchasedProductDeferred` na 4.x): `falha(produto, "adiada")`, sem entregar; o recibo chega quando pagar.
   - `Restaurar` → `RestoreTransactions` (Google e Apple), `fim(n)` no callback.
   - `Valido(r)`: `CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier).Validate(r.Payload)` e conferir que algum recibo validado tem `productID == r.Produto` e `transactionID == r.Tx` (e o pacote do app). Gerar os *Tangle* em Services > In-App Purchasing > Receipt Validation Obfuscator com a chave pública da Play.
   - `Preco` → `product.metadata.localizedPriceString`.
3. No `Game.Awake`: `_loja = new LojaUnity(...)` no lugar do `null` do release (a falsa continua só com `-lojafalsa` no dev).
4. Teste: trilha interna com *license testers* (compra de teste sem cobrança), os 4 cenários do §3 à mão no aparelho e o `verify.sh android-emulator`.

**O que o Vinicius cria no Play Console** (nada disso existe hoje): perfil de pagamentos (conta de comerciante) vinculado à conta de desenvolvedor; um AAB com o Billing enviado a uma trilha de teste (o produto só pode ser criado depois disso); o produto único **`iap_pacote_mestre`** (não-consumível, preço da Wave E, título e descrição pt/en/es com o conteúdo do §1); *license testers*; copiar a **chave pública RSA** (Monetização > Configuração) para o obfuscator; opcional: Real-time Developer Notifications (reembolso). Decisões dele: preço (Wave E), conteúdo final do pacote (P3), validação no servidor (custo).

## 6. Checklist de privacidade quando o SDK entrar (`docs/PRIVACIDADE.md`)

- §1.1: "O Forge Street não tem … compra" deixa de valer; listar as linhas `iap.tx` do save (recibo e ID de transação, só no aparelho).
- §2.2 Data Safety: **Informações financeiras > Histórico de compras** passa a ser coletado também pelo jogo (pela biblioteca da Play), finalidade "Funcionalidade do app"; resolver a D-PRIV-6. Pagamento (cartão) é da Google: o jogo não vê.
- §2.5/§3.1 IARC: "compras digitais" = **Sim** (sem loot box: conteúdo fixo e visível, V4); refazer o questionário. Ficha da loja: "Contém compras no app".
- §7 ECA Digital: nada aleatório pago (estrutural, validator); compra de menor passa pela aprovação dos pais da própria Play (Family Link); **revisar** se a loja deve sumir para menores de 13 (hoje não some).
- Se a validação for em servidor: novo destino de dado (recibo) a declarar e a política de privacidade a atualizar.

## 7. Pendências (revisar)

- Conteúdo e preço do Pacote do Mestre (P3, Wave E); texto "Sem anúncio entre telas" com a flag desligada.
- `sem_video` cobre Velocidade, Lingotes e Trocar; VIP, Cofre 2× e bônus ficam com vídeo (ouro direto, V20).
- Moeda (`gems`) como conteúdo: a entrega entra com a carteira com IDs (Wave E), no `Compras.Receber`, 1× pelo mesmo ledger.
- Reembolso/revogação e compartilhamento de recibo (§2, tetos).
