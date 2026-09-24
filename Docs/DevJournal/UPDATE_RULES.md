# Castleveil Dev Journal Update Rules

## Objetivo
Manter `Docs/DevJournal/Castleveil_Dev_Journal_Compact.html` atualizado ao final de cada milestone ou submilestone relevante, sem transformar o diário na fonte de verdade do projeto.

A fonte de verdade continua sendo:
- código;
- cenas/prefabs;
- assets;
- Git;
- validações de compilação e Play Mode.

## Quando atualizar
Atualize o diário somente quando ocorrer pelo menos um dos casos abaixo:
- milestone concluído;
- submilestone relevante concluído;
- milestone pausado;
- roadmap alterado;
- decisão técnica importante consolidada;
- mudança relevante de identidade visual/narrativa;
- fechamento de uma sessão de trabalho importante.

Não atualizar por microajustes visuais isolados ou pequenas correções que não mudem o estado do roadmap.

## Regras obrigatórias
1. Preserve todo o histórico existente.
2. Não apague milestones concluídos.
3. Não reescreva lore, decisões narrativas ou roadmap sem solicitação explícita.
4. Não marque nada como concluído antes de:
   - compilar sem erros relevantes;
   - validar referências necessárias;
   - concluir a validação manual solicitada, quando aplicável.
5. Não invente resultados de Play Mode.
6. Se Play Mode ainda não foi validado manualmente, registre isso.
7. Evite duplicar itens já existentes.
8. Mantenha o HTML válido.
9. Preserve filtros, JavaScript, layout compacto e estilos existentes.
10. Não transforme o diário em documentação técnica extensa; use resumos curtos e úteis.

## Como mover status

### Em andamento -> Concluído
Ao concluir:
- mover o item para `Concluído`;
- registrar resumo curto do que ficou funcionando;
- registrar arquivos/sistemas principais envolvidos;
- remover do checklist de próxima sessão se já estiver resolvido.

### Em andamento -> Pausado
Ao pausar:
- mover para `Pausados`;
- registrar motivo curto;
- registrar o que falta para retomar.

### Novo trabalho
Ao surgir trabalho novo:
- adicionar ao `Roadmap`;
- manter ordem lógica de milestones;
- evitar criar milestone nova para microtarefas.

## Bloco "Última atualização"
Sempre atualizar:
- data;
- milestone/submilestone;
- status;
- commit, se conhecido;
- arquivos principais;
- validação;
- próximo passo.

Se o commit ainda não existir, usar:
`Commit: pendente`

## Bloco "Última sessão"
Atualizar com 3 a 6 bullets:
- principais entregas;
- decisões;
- problemas encontrados;
- próximo passo.

## Checklist da próxima sessão
Manter somente tarefas realmente acionáveis.
Limite recomendado: 3 a 6 itens.

## Regras para Codex / CLI
Tudo que já está funcionando deve ser tratado como estável.

Prioridade:
`ADD > MODIFY > REFACTOR`

Antes de alterar um sistema existente:
- revisar o código atual;
- preservar APIs;
- preservar campos serializados;
- evitar renomear classes/GameObjects;
- preferir novos componentes/arquivos;
- evitar mudanças em cenas existentes sem pedido explícito.

Sistemas protegidos atualmente:
- movimento/câmera;
- stats/health;
- combate melee;
- dodge/stamina;
- HUD;
- visual direcional do Player;
- FeralDog AI e visuais;
- hit reaction/knockback;
- hit stop;
- impact VFX;
- WorldItem pickup;
- PlayerEquipment;
- LootLabel;
- WorldItemHighlight;
- WorldItemSparkles;
- Input System atual.

Se uma feature exigir alterar sistema protegido:
- explicar o motivo;
- listar arquivos afetados;
- fazer a menor mudança possível;
- preservar comportamento validado.

## Passo final obrigatório de cada milestone
Ao finalizar:
1. compilar;
2. validar;
3. resumir arquivos criados/modificados;
4. atualizar o Dev Journal;
5. informar se há validação manual pendente;
6. sugerir commit somente depois da validação necessária.
