# Main Menu — extração fiel da referência

Fonte imutável: `exemplo.png`, 1448 × 1086 (4:3).

## Montagem

O menu foi preparado por `Tools > Castleveil > Apply Reference Main Menu`.
O comando importa apenas os PNGs derivados, cria `Animation/Campfire.anim`,
`Animation/Campfire.controller`, `Prefabs/MainMenuCampfire.prefab` e monta a
composição na cena MainMenu. O Canvas antigo permanece desativado para reversão.
Não executar o antigo `Setup Main Menu` para aplicar esta apresentação.

- Sprites Single / Full Rect; Point; sem mipmaps; sem compressão; resolução original.
- Canvas Overlay, escala uniforme, composição 4:3 centralizada. Outras proporções
  usam margens pretas para não cortar ou deformar a arte.
- Iniciar Jogo, Configurações e Sair usam o MainMenuController existente.
- Continuar permanece desabilitado. Os textos fazem parte dos PNGs.
- Navegação vertical explícita entre os três botões ativos; sem tint automático
  para preservar a aparência da imagem de referência.
- O prefab da fogueira é UI (Image + Animator), destinado a um Canvas.

## Fidelidade e limites da extração

A imagem não possui camadas nem pixels do cenário oculto. Estes assets são
recortes de composição: logo/botões incluem o contexto da imagem em seus
retângulos; personagem/fogueira usam máscaras poligonais com contexto nas bordas.
O background tem transparência nas regiões extraídas. Não é um cenário limpo
reconstruído, nem são recortes sem fundo próprios para reposicionamento livre.
Não mover as peças separadamente se for necessário preservar a composição.

A montagem das peças na posição registrada em `layout.json` reproduz exatamente
os pixels da referência no frame 0. Nenhuma fonte, cor, proporção ou desenho foi
substituído. `Animation/menu_reconstruction.png` é a prova dessa recomposição.

A fogueira tem 12 frames / 10 FPS (1,2 s), com cintilação suave e periódica apenas
nos pixels quentes da região da chama/brasas. Mantém o contorno e os troncos;
não simula uma nova chama nem um swing/deformação. O frame final do clip retorna
ao primeiro, com Loop Time habilitado. O Animator usa tempo não escalado para
que a apresentação do menu não dependa do timeScale do gameplay.
`Animation/campfire_preview.gif` serve para revisão; a paleta reduzida do GIF
não é usada no Unity. PNGs preservam as cores completas.

## Regenerar

`Assets/_Game/Scripts/Editor/ExtractMainMenuArt.py` requer Python, Pillow e numpy.
Não instala dependências no Unity. Execute somente se desejar regenerar os PNGs
nos caminhos listados em layout.json, e depois aplique o comando Editor acima.
A ferramenta Editor atualiza exclusivamente sua apresentação gerada e os assets
de animação/prefab; ajustes manuais dentro dessa apresentação serão substituídos.

## Conferência manual

Abrir MainMenu em Play Mode: verificar cintilação, botões com mouse e teclado,
Iniciar Jogo carregando Milestone1, Continuar inativo, Configurações registrando
placeholder e Sair no build. Conferir 4:3 e 16:9. Não houve teste de Play Mode
nesta tarefa. As máscaras não permitem mover personagem/fogueira sem expor
as regiões transparentes do fundo; para isso será necessário arte em camadas.
