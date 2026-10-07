# Forge Street — Art Bible v0.1 (PROPOSTO, 2026-10-06)

Status: PROPOSTO pela raia 2 (concept-art lead + character 3D + art director), ainda não aprovado nem medido no aparelho. Regra de jogo vem do `GDD.md` §11 ("stylized low-poly premium, câmera oblíqua, silhuetas grandes, produção filmável"). A view atual (`client/Assets/_FS/Scripts/View/Art.cs`) já lê cada item por **cor + forma**; esta bíblia herda esses matizes para que HUD, playtest e criativos não mudem de leitura quando a arte entrar. Lista de assets e prompts: `ASSETS.md`.

## 1. Pilares visuais

1. **Lê em 3 s num vídeo 9:16:** minério marrom → lingote prata → espada azul → cliente feliz; se um quadro parado não conta a cadeia, a arte falhou.
2. **Brinquedo de forja:** volumes gordos e chanfrados, 2 tons + 1 brilho por material; nada que pareça foto ou PBR espelhado.
3. **Calor contra frio:** oficina âmbar/laranja, rua azul-pedra; o olho vai para a brasa e para o ouro.
4. **Estado é forma, não só cor:** "travada" = pilha transbordando + lâmpada vermelha; "fome" = apagada e cinza; daltonismo coberto por silhueta.
5. **IP próprio:** nenhum traje, símbolo ou criatura que remeta a franquia; o que só existe aqui: bandana vermelha do ferreiro, anel de ouro dos pads, martelo-pilão sobre a bigorna, heráldica "bigorna + chama".

## 2. Paleta por material

| Material | Base | Sombra | Luz | Onde |
|---|---|---|---|---|
| Ferro / aço | `#8C96A6` | `#4F5866` | `#D5DCE6` | bigorna, lingote, lâminas, armaduras, pinças |
| Brasa | `#FF7A1F` | `#C1272D` | `#FFD166` | boca da fornalha, faíscas, lâmpada "travada", único emissivo |
| Madeira | `#8A5A32` | `#5C3A1E` | `#B8824F` | bancadas, balcão, carrinho, cabos, roletes |
| Pedra quente (oficina) | `#4A413A` | `#2E2824` | `#6B5F55` | chão da oficina, corpo da fornalha, pads |
| Pedra fria (rua) | `#55607A` | `#3A4256` | `#7F95B5` | rua, muro, exterior (continua o `#232A3B` atual, mais azul) |
| Couro | `#7A4A2A` | `#4E2D18` | `#A86D45` | avental, correia da esteira, cintos, mochilas |
| Ouro | `#E0B23A` | `#9C7520` | `#FFE08A` | balcão, anel do pad, moedas; `#FFE08A` = `Art.Accent` atual |
| Pele (3 tons + goblin) | `#F2C9A4` / `#C98E62` / `#7A4B2E` / `#6FA85C` | −20% valor | +15% valor | elenco diverso por padrão |
| Tinta / fundo | `#EEF1F8` | `#121622` | — | rótulos e HUD (`Art.Ink` / `Art.Bg` atuais) |

## 3. Paleta por item (precisa ler a 1 cm de tela)

| Item | Matiz | Base | Detalhe | Forma (silhueta) | Tamanho no mundo |
|---|---|---|---|---|---|
| Minério | marrom | `#8A6A4A` | veios `#FF7A1F` | pedra irregular de 5–6 faces | 0,30 m |
| Lingote | prata | `#C8D0DC` | topo `#EEF1F8` | barra trapezoidal chanfrada, deitada | 0,35 × 0,15 m |
| Espada | azul-aço | `#7FC4FF` | guarda `#E0B23A`, cabo `#5C3A1E` | triângulo longo com guarda larga | 0,55 m |
| Escudo | vermelho | `#F2545B` | umbo ouro, aro `#8C96A6` | disco com umbo central | 0,45 m |
| Ferramenta | verde | `#4CD964` | cabeça `#8C96A6` | martelo em T (cruz) | 0,40 m |

Regras: cinco matizes distintos (marrom, prata, azul, vermelho, verde) **e** cinco formas distintas; a cor nunca é o único sinal (vermelho/verde se separam por disco vs. cruz). O mesmo ícone serve pilha no chão, pilha na cabeça, balão do cliente e HUD. Teste: renderizar cada item a 24 px e 32 px, em cor e em preto; se não lê em 24 px, muda a **forma**, não a cor.

## 4. Proporções dos personagens

**3 cabeças (chibi), não 2,5.** Altura em cabeças de 0,40 m; largura de ombros ≈ 1 cabeça; mãos e botas 1,5× o natural; pescoço curto; T-pose com braços ≤ 10° da horizontal.

- Leitura: a câmera a 60° comprime a altura em ~15% e, no tamanho real da view (ferreiro ≈ 35 px numa tela de 540 px de largura ≈ 0,6 cm em 6,5"), só cabeça + ombros + 1 prop leem. Cabeça = 1/3 da altura mantém chapéu, barba e elmo legíveis; em 7 cabeças o personagem vira um palito com cabeça de 5 px.
- Custo de rig: o rig do Tripo + clips do Mixamo entram sem retoque em 3 cabeças (LICOES 2026-09-30); em 2,5 cabeças os braços ficam tão curtos que `Walking` e `Cheering` enfiam a mão na cabeça e cada clip pede correção manual.
- Geração: "chibi, 3 heads tall" é um estilo que o Tripo reproduz de forma estável; proporção realista varia a cada geração e quebra o elenco.

| Papel | Altura | Observação |
|---|---|---|
| Ferreiro | 1,20 m | referência do elenco |
| Ajudante (base única) | 1,05 m | menor que o jogador: hierarquia visível |
| Guerreira / Elfa / Mago | 1,20 / 1,25 / 1,25 m | mago chega a 1,60 com o chapéu |
| Anão / Mercador goblin | 0,95 / 1,00 m | largura 0,70 m (anão), mochila até 1,30 m (goblin) |
| Cavaleiro | 1,30 m | pluma vertical até 1,55 m |

Estações têm footprint 1,5 × 1,1 m (`Sim`): nenhum personagem cobre uma estação; a pilha na cabeça cresce até 6 itens (0,34 m cada) sem passar da altura do mago.

## 5. Silhueta por papel (teste: preto a 30% de escala, acerto ≥ 4/5)

| Papel | Assinatura que lê em preto | Cor dominante | Prop (Blender, 0 crédito) |
|---|---|---|---|
| Ferreiro (jogador) | ombros largos, avental até o joelho, bandana, martelo grande no cinto | couro + bandana `#C1272D` + camisa creme | martelo |
| Ajudante base | menor, touca redonda, avental curto, mangas enroladas | túnica `#D9CBB0`; touca na cor do papel | — |
| Ajudante 1 (minério) | cesto de vime nas costas, acima da cabeça | marrom `#8A6A4A` | cesto |
| Ajudante 2 (lingote) | pinças compridas na mão, luvas claras | prata `#C8D0DC` | pinças |
| Ajudante 3 (produto) | bandeja à frente do corpo | azul `#7FC4FF` | bandeja |
| Cliente: Guerreira | rabo de cavalo alto, escudo redondo nas costas, ombreiras | túnica `#5B7F9E`, escudo `#F2545B` | — |
| Cliente: Mago | chapéu cônico alto (a forma mais legível de cima), cajado com gema | roxo `#6E4FA3`, gema `#4CD964` | — |
| Cliente: Anão | baixo e largo, barba em cunha trançada, capacete redondo com nasal | ferro + barba `#B0522A` | — |
| Cliente: Elfa | orelhas longas, capuz pontudo, arco nas costas (arco de círculo) | musgo `#4F7A4A`, capa `#2E4A3C` | — |
| Cliente: Mercador goblin | mochila maior que ele, chapéu de aba larga, nariz comprido | pele `#6FA85C`, mochila `#8A5A32` | — |
| Cliente: Cavaleiro | armadura completa, elmo fechado com pluma vertical, escudo losango | aço + tabardo `#1F4E8C` + pluma `#F2545B` | — |

O elenco varia em três eixos ao mesmo tempo: altura (0,95–1,55 m com adereço), largura (anão, goblin) e ponto alto (chapéu, pluma, cesto, capuz). Clientes nunca repetem a cor dominante do ferreiro nem de um item (sem túnica verde-item ou vermelha-escudo lisa).

## 6. Linguagem das estações

| Estação | Forma | Cor | "Travada" (saída cheia) | "Fome" (sem entrada) |
|---|---|---|---|---|
| Depósito | carrinho de mina com pilha de minério, trilho curto | madeira + minério | — (nunca trava) | pilha baixa |
| Fornalha | corpo de pedra quadrado, boca em arco, chaminé curta, fole lateral | pedra quente + brasa | lâmpada vermelha pulsa, lingotes transbordam, fumaça escura | boca apagada e cinza, sem fumaça, fole parado |
| Bigorna | bigorna de ferro sobre toco de carvalho, martelo-pilão por cima, balde | ferro + madeira | espadas caem da pilha, lâmpada vermelha | pilão erguido e parado, bigorna dessaturada |
| Bancada de escudos | bancada com rack de 3 escudos, torno, pano vermelho | madeira + `#F2545B` | pilha de escudos transborda, lâmpada | rack vazio, pano caído |
| Bancada de ferramentas | bancada com rebolo, rack de martelos e pinças, pano verde | madeira + `#4CD964` | idem | idem |
| Balcão | balcão de madeira, toldo listrado ouro/creme, bandeja de moedas | madeira + ouro | fila cheia: clientes de braços cruzados (animação), sem lâmpada | — |
| Pad de upgrade | placa de pedra redonda no chão com anel de ouro | pedra + ouro | — | anel cinza se não dá para pagar; enche de ouro ao pagar (como hoje) |
| Esteira | roletes de madeira + correia de couro, módulo reto de 1 m | madeira + couro | — | correia parada |

Regras: ocupada = emissivo/animação ligados; ociosa = desligados; a lâmpada vermelha é o sinal universal de "travada" e fica sempre no canto superior direito da estação; "fome" é sempre dessaturar + apagar. Barra de progresso e preço do pad continuam procedurais na HUD.

## 7. Câmera e luz

- **Jogo:** top-down oblíqua, ortográfica, inclinada **60° em relação ao chão** (30° da vertical). O `Sim` continua em planta (X,Y); personagens e estações aparecem a 60° (mostram rosto, chapéu e boca da fornalha), o chão fica em planta pura: convenção 2.5D, nada muda em `Sim`, `Game` ou joystick.
- **Luz-chave quente** `#FF8A3D` vinda de baixo-esquerda (lado das fornalhas) + **preenchimento frio** `#7F95B5` vindo de cima (rua) + ambiente alta (0,6): nenhum preto fechado. Clima se faz com cor, nunca tirando luz (lição Arkana 2026-08-18).
- Sombra: elipse escura sob cada personagem e prop (já existe no greybox); sem sombra projetada longa.
- A luz é fixa e fica gravada no sprite: consistência de graça; direção nova de luz = re-render.
- **Criativo 9:16:** mesma cena e mesmos modelos no Blender, câmera livre a 45–60°, bloom só na brasa, 3 s: close da bigorna com faíscas → recuo até a rua inteira.

## 8. O que NÃO fazer (e o que fazer no lugar)

- Hiper-detalhe e micro-ornamento → 1 detalhe por peça, com ≥ 0,05 m (some em ASTC 6×6 numa tela de 6,5").
- Pelos, cabelo fio a fio, barba em mechas → cabelo e barba como volume sólido chanfrado.
- Tecidos finos, capas com simulação → capas rígidas com 2–3 dobras modeladas.
- Metal PBR espelhado (lê como cinza) → metal pintado em 2 tons + brilho fixo.
- Vidro e transparência → vidro opaco claro com brilho pintado.
- Texto em textura → placas em branco; texto só na HUD.
- Proporção realista em cliente "importante" → 3 cabeças para todo o elenco.
- Escuridão "de clima" → cor fria e luz baixa, mas tudo visível.
- Cor como único sinal → cor + forma + posição (travada/fome).
- Trajes, brasões ou criaturas de franquia → heráldica própria (bigorna + chama) e elenco com assinatura do §5.

## 9. Três referências (descritas, não copiadas)

1. **Brinquedo de madeira pintado:** peças de jogo de tabuleiro premium, chanfro de 2 mm, cor chapada com verniz; cada figura lê pela cabeça grande e por um único adereço.
2. **Diorama de argila sob lamparina:** volumes gordos de bordas suaves, luz quente pontual no centro e fundo azulado; a cena inteira cabe numa mesa.
3. **Low-poly facetado moderno:** cor por face, gradiente vertical sutil (base escura, topo claro), zero textura detalhada; a leitura vem só da forma.

## 10. Validação e proveniência

- Aprovação olhando a **foto na câmera do jogo no aparelho** (`-shot` do APK), nunca no editor; silhuetas em preto a 30%; ícones a 24 px.
- Cada asset gerado entra em `docs/PROVENIENCIA.md` (criar na 1ª geração) com ferramenta, **plano pago** (Tripo gratuito = uso não comercial), prompt, data e SHA-256; sem a linha, o asset não entra em build comercial.
