# Visualizador ESC/POS

Mostra na tela como uma impressora térmica imprimiria um arquivo com comandos ESC/POS, como o arquivo gerado pelo ACBr quando a porta da impressora é um caminho de arquivo (por exemplo `C:\impressao.prn`).

## Como usar

- Abra `bin\VisualizadorEscPos.exe` e clique em **Abrir arquivo...** (ou Ctrl+O), ou arraste os arquivos para dentro da janela.
- Também dá para soltar os arquivos em cima do `.exe`, ou usar **Abrir com** no Windows.
- Cada arquivo abre na sua aba. Para fechar uma aba, use o **X** dela, o botão do meio do mouse ou Ctrl+W; Ctrl+Tab troca de aba. Arraste uma aba para mudar a ordem. Uma bolinha verde marca a aba que recebeu impressão nova.
- **Modelo**: escolha o mesmo modelo configurado na impressora do Lógica. EPSON e DIEBOLD usam *Epson e compatíveis*; BEMATECH, ELGIN e DARUMA têm opção própria. TEXTO é texto puro e aparece certo em qualquer modelo. NENHUM não gera arquivo.
- **Atualizar sozinho** (ligado por padrão): a cada nova impressão o ACBr regrava o arquivo e a tela recarrega.
- **F5** recarrega na hora. Modelo, colunas, zoom e *Atualizar sozinho* valem para a aba aberta.
- **Colunas**: largura do papel em caracteres da fonte normal (48 para 80 mm, 32 para 58 mm).
- **Zoom** fica no canto da barra de status; também funciona com Ctrl + roda do mouse, Ctrl + / Ctrl − e Ctrl+0.
- O ícone de disquete grava a prévia em PNG.
- **Comandos** (canto direito da barra): cada comando com a posição no arquivo, os bytes e o significado.

## Impressora de rede

O botão **Rede** abre uma aba que funciona como uma impressora térmica de rede, sem precisar de arquivo.

1. Clique em **Rede** para abrir a aba e depois em **Ligar**. Ela começa a receber em `127.0.0.1:9100`.
2. Coloque na porta da impressora `TCP:127.0.0.1:9100`. O botão **Copiar** da aba copia esse texto.
3. Cada impressão aparece na lista da esquerda, com a mais nova em cima e já selecionada. Clique numa anterior para rever.

- Uma impressão termina quando a conexão fecha ou quando ela fica 0,7 s sem receber dados. Assim, cada impressão fica separada mesmo se o ACBr deixar a porta aberta.
- Pedidos de status da Epson (`DLE EOT`) recebem a resposta "impressora pronta".
- **Aceitar de outros computadores** recebe impressões vindas de outra máquina da rede. O Windows pode pedir liberação no firewall.
- Clique com o botão direito numa impressão para salvá-la como `.prn` ou apagá-la, ou para limpar o histórico. A tecla Delete também apaga.
- O programa lembra como a impressora de rede estava ao ser fechado: se estava ligada, volta ligada; se estava desligada, ou com a aba fechada, abre sem ela. O botão **Rede** só abre a aba, sem ligar. A porta, a opção de aceitar outros computadores e esse estado ficam guardados em `%APPDATA%\VisualizadorEscPos\config.ini`.
- Fechar a aba desliga a impressora de rede.

## Linha de comando

```
VisualizadorEscPos.exe arquivo.prn /png saida.png [colunas] [epson|bematech|elgin|daruma]
```

Pelo PowerShell ou cmd. No Git Bash o `/png` é convertido em caminho; use `//png`.

## O que é simulado

Negrito, altura e largura dupla, sublinhado, itálico, condensado, impressão invertida, fonte B, alinhamento, páginas de código (437, 850, 860, 863, 865, 858, 866, 1252), avanço de linhas e corte. Imagens, código de barras, QR code, bipe e gaveta aparecem como um aviso no lugar. Os comandos de cada modelo seguem os do ACBr (`ACBrEscPosEpson`, `ACBrEscBematech`, `ACBrEscElgin`, `ACBrEscDaruma`).

## Compilar

Rode `build.bat`. Usa o `csc.exe` do .NET Framework 4, que já vem no Windows; o executável sai em `bin\`.
