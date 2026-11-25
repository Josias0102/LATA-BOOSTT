# LATA BOOST — 0.4.0 Preview
Protótipo WPF para Windows x64. Sem serviço residente, telemetria ou dependência de Python. Não validado em Windows ainda.

## Compilar
Instale .NET SDK 8 e Inno Setup 6 no Windows. Execute na raiz:
```powershell
dotnet publish src/LataBoost.csproj -c Release -r win-x64 --self-contained true -o publish
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" installer\setup.iss
```
O instalador ficará em dist. Alternativamente envie todo o repositório (incluindo .github) ao GitHub, abra Actions > Windows installer > Run workflow. Baixe o artefato da execução bem-sucedida e teste antes de publicar em Releases. O executável não é assinado; não desative proteções do Windows para executá-lo.

## Pix
Preencha pix.txt com sua chave pública destinada a doações (preferencialmente aleatória). O arquivo acompanha o instalador. Nenhuma cobrança automática.

## Escopo real
Quatro ajustes HKCU reversíveis, diagnóstico de disco/CPU, snapshot de processos, benchmark SHA-256, atalhos de configurações e suporte Pix. Backup em %LOCALAPPDATA%\LataBoost\backup.json. Restaure antes de desinstalar. Não há alterações de tensão, fans, GPU ou BIOS nesta versão. Não há ganho garantido. A captura de jogos pode deixar de funcionar após desativada.

## Validação antes do release
Em máquina de teste: instalar; verificar ajustes um por vez; conferir backup e restauração inclusive valores ausentes; testar com usuário comum; medir consumo; testar desinstalação. Não publique como versão estável sem esses testes. Futuras integrações devem usar APIs documentadas, detecção de compatibilidade, limites do fabricante, restauração e proteção térmica. Não carregar drivers vulneráveis nem contornar bloqueios de firmware.

Licença proposta: MIT (confirme autoria e licença antes da publicação).

## 0.2.0 — diagnóstico e compatibilidade
- Inventário CIM/WMI local de CPU, GPU, RAM, BIOS, placa-mãe, discos, rede, bateria e ventoinhas quando expostas. Nenhum identificador serial é solicitado.
- Planos de energia existentes enumerados por powercfg, confirmação antes de alterar, backup do GUID original e verificação do resultado. Restauração separada na aba Recovery.
- Atalhos para ferramentas oficiais de gráficos, paginação, discos, rede e aplicativos. Atalhos não são ajustes automáticos.
- Controles físicos de CPU/GPU/RAM/ventoinhas e sensores ao vivo permanecem bloqueados: não há backend validado. A presença do dispositivo não implica suporte à escrita. Nenhuma gravação MSR/EC/BIOS ou driver de kernel.
- Consultas indisponíveis não são tratadas como prova de ausência de hardware.
- Testar em Windows 10/11 antes de publicar: inventário, planos disponíveis, alteração/restauração, execução sem administrador, consultas bloqueadas, backup de registro e Pix. Não distribuir como versão estável antes desses testes.

## 0.4.0 — monitor térmico
- Integra LibreHardwareMonitorLib 0.9.6 via NuGet, incluída no publish.
- Na aba Monitor térmico, coleta em thread de trabalho com intervalo de 2 segundos após cada leitura. Pausar ou sair da aba cancela a coleta e fecha o backend.
- Temperatura, RPM, tensão, potência, carga e frequência quando disponíveis; mínimo/máximo por sessão; alertas informativos CPU/GPU a 80/90 °C, não limites oficiais.
- Alguns sensores exigem execução como administrador e instalação separada do driver PawnIO oficial: https://pawnio.eu/ . Não há instalação automática de driver; não desative proteções do Windows. Se indisponível, o painel informa ausência de dados.
- Biblioteca de terceiros MPL-2.0: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/v0.9.6 ; preserve os avisos e a licença e disponibilize o código-fonte correspondente da biblioteca ao distribuir binários. Confira também as licenças das dependências NuGet antes de publicar.
- Teste em Windows: abrir monitor, comparar leituras com ferramenta independente (não simultaneamente se houver conflito), verificar ausência de driver, pausar, trocar de aba, reiniciar coleta, fechar programa e medir uso de recursos. Não validado em Windows. Nenhum controle de tensão, fan ou frequência foi liberado.

## Identidade LATA BOOST
Diagnóstico: Analisando a Lata...; Latinha Seca (menos de 12 GiB de RAM física ou menos de quatro núcleos físicos), Latinha Nutela (demais casos com dados válidos). Classificação recreativa, não é benchmark e não libera controles de hardware. Dados ausentes: Lata não classificada. Ajustes de registro e energia confirmados exibem Seeeeco Seco! Backups anteriores são copiados se ainda não houver backup novo. Fast Boost ainda não implementado. Site em docs/; configurar Pix em pix.txt e docs/index.html.
