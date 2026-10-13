# DRP — Documento de Requisitos do Produto

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Requisitos do Produto (DRP) |
| Versão | 2.0 (substitui o DRP do marketplace, arquivado) |
| Status | Aprovado para refinamento |
| Dono | Product Owner |
| Relacionados | [DVP-E](dvp-e.md), [Fluxos](fluxos.md), [DAT](../03-arquitetura/dat.md), [Critérios de aceite](../04-qualidade/criterios-de-aceite-mvp1.md) |

## 1. Atores

| Ator | Descrição |
|---|---|
| Pessoa candidata | Usuária do Zyft. Tem conta na plataforma e, por conta própria, no portal de vagas. |
| Plataforma web | Aplicação onde a pessoa mantém o Perfil Mestre, os consentimentos e os dados. |
| Extensão | Extensão Chrome/Edge que lê e preenche a página do portal aberta pela própria pessoa. |
| Serviço de IA | Modelo de linguagem usado só por meio da API do Zyft, nunca direto pela extensão. |
| Portal de vagas | Site de terceiros. Não é integrado por API; o Zyft atua só na página que a pessoa abriu. |

## 2. Priorização

MoSCoW aplicado ao **MVP 1**. "Won't (MVP 1)" significa MVP 2 ou nunca, conforme indicado.

| Prioridade | Requisitos |
|---|---|
| **Must** | RF-01 a RF-16, RF-18, RF-19, RF-20; todos os RNF |
| **Should** | RF-17 (análise de vaga visível) |
| **Could** | RF-21 (melhorar texto escrito pela pessoa), RF-22 (versões do currículo para o portal) |
| **Won't (MVP 1)** | RF-M2-01 a RF-M2-12 (seção 8) |

## 3. Requisitos funcionais — MVP 1

Cada requisito tem critérios de aceite verificáveis em [criterios-de-aceite-mvp1.md](../04-qualidade/criterios-de-aceite-mvp1.md).

### 3.1 Conta, consentimento e dados

| ID | Requisito | Regras |
|---|---|---|
| RF-01 | Criar conta e autenticar na plataforma web. | E-mail com verificação ou login Google. Sem cadastro de senha de portais em nenhum momento. |
| RF-07 | Gerenciar consentimentos por finalidade. | Finalidades separadas e desligáveis: (a) IA para textos livres, (b) análise de vaga, (c) uso de dados sensíveis. (c) começa **desligado**. Desligar (b) bloqueia a análise na API, não só na interface. |
| RF-08 | Exportar e excluir os próprios dados. | Exportação em JSON legível com perfil, respostas aprovadas, consentimentos e registro de ações. A exclusão apaga tudo e encerra o pareamento das extensões. |

### 3.2 Perfil Profissional Mestre

| ID | Requisito | Regras |
|---|---|---|
| RF-02 | Criar e editar o Perfil Mestre. | Seções: contato e localização; objetivo (cargos, áreas, senioridade, modalidade, localidade, resumo); experiências (organização, cargo, período, atividades, ferramentas, resultados); formação; cursos; certificações; competências; idiomas; links; preferências (disponibilidade, pretensão salarial, viagens, mudança, tipos de contrato, CNH). Nenhum campo é obrigatório para todos; a plataforma não presume que todos se aplicam. |
| RF-05 | Perguntar só o que falta. | Depois da importação ou da edição, a plataforma lista apenas dados ausentes ou ambíguos **necessários ao fluxo do portal suportado**, em linguagem simples. |
| RF-06 | Manter respostas aprovadas para perguntas recorrentes. | Cada resposta guarda a pergunta original, o texto aprovado e a data. Antes de cada reuso, a resposta é mostrada para revisão. Pode ser editada ou excluída. |
| RF-22 | (Could) Escolher a versão do currículo usada num portal. | Só registra qual arquivo a pessoa anexou; o Zyft nunca anexa arquivos. |

### 3.3 Importação de currículo

| ID | Requisito | Regras |
|---|---|---|
| RF-03 | Importar currículo PDF (com texto) ou DOCX, até 10 MB. | O conteúdo vira uma **proposta** de perfil. PDF escaneado (imagem) não é suportado no MVP 1 e gera mensagem clara. Dados sensíveis encontrados no texto **não** são extraídos. |
| RF-04 | Revisar e aprovar a proposta campo a campo. | Nada entra no perfil sem aprovação. A pessoa pode aceitar, editar ou descartar cada campo e cada item de lista. Conflitos com o perfil atual são mostrados lado a lado. |

### 3.4 Extensão e preenchimento assistido

| ID | Requisito | Regras |
|---|---|---|
| RF-09 | Parear a extensão com a conta. | Pareamento iniciado na extensão e confirmado na plataforma (código curto ou login). A plataforma lista as extensões pareadas e permite revogar cada uma. |
| RF-10 | Reconhecer a página autorizada e mostrar as ações disponíveis. | A extensão só atua em páginas do portal suportado e em sites que a pessoa autorizar explicitamente. Ações possíveis por tela: "Ler formulário", "Analisar vaga" (RF-17), "Importar do portal" (RF-16). |
| RF-11 | Ler os campos visíveis do formulário. | Para cada campo: rótulo (label, aria, placeholder, texto vizinho), seção, tipo, opções e valor atual. Com um modal aberto, lê só o modal. Campos de senha, CAPTCHA e upload são ignorados. |
| RF-12 | Propor o de-para entre campos e Perfil Mestre. | Ordem: (1) regras determinísticas; (2) IA, que só pode **apontar uma chave** do perfil; (3) validação: a chave existe, o valor vem do perfil e a opção de select existe na página. Blocos repetidos (Experiência 1, 2…) são alinhados por organização ou instituição. |
| RF-13 | Revisar e selecionar o que preencher. | Painel lateral com cada campo, valor proposto, origem (regra, IA, resposta aprovada) e estado (pronto, ambíguo, ausente, bloqueado). Ambíguos e ausentes nunca vêm marcados. A pessoa pode editar qualquer valor. |
| RF-14 | Preencher só os campos aprovados. | Dispara os eventos que o portal espera (input, change, blur). **Nunca** clica em enviar, salvar, aceitar ou avançar. Ao terminar, mostra quantos campos foram preenchidos e quais precisam de ação manual. |
| RF-15 | Sugerir texto para campos abertos. | Usa apenas fatos aprovados do perfil, ou reaproveita uma resposta aprovada para pergunta parecida. Respeita o limite de caracteres do campo. Sempre editável e nunca pré-aprovado. Requer o consentimento (a) da RF-07. |
| RF-16 | Comparar o que já está no portal com o perfil. | Quando o campo do portal já tem valor diferente, a opção padrão é **manter o valor do portal**. A pessoa pode escolher "usar o perfil" ou "levar para o perfil", e importar valores do portal para o perfil exige aprovação. |
| RF-18 | Bloquear campos que exigem ação humana. | Nunca preencher automaticamente: dados sensíveis (sem consentimento (c) **e** marcação manual por campo), declarações e termos, anexos, CAPTCHA, senhas e códigos de verificação. Esses campos aparecem no painel como "ação sua". |

### 3.5 Vaga, conclusão e registro

| ID | Requisito | Regras |
|---|---|---|
| RF-17 | (Should) Analisar a vaga visível. | Extrai cargo, empresa, local, contrato, modalidade, jornada, salário e requisitos **visíveis**. Cada requisito recebe "atende", "parcial", "não encontrado" ou "confirmar", e "atende" ou "parcial" exige uma linha real do perfil como evidência. Pontos como CNH, pretensão salarial e modalidade viram "a confirmar". Nunca mostra nota, porcentagem ou promessa de compatibilidade. Requer o consentimento (b). |
| RF-19 | Confirmar a conclusão. | Depois que a pessoa salvar ou enviar **no portal, por conta própria**, a extensão mostra a confirmação visual do que foi preenchido. Guardar um registro mínimo depende do [ADR-0012](../03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md). |
| RF-20 | Registrar as ações automatizadas. | Registro legível pela pessoa, com data, ação, portal e quantidade de campos, **sem os valores preenchidos**. Visível na plataforma e incluído na exportação. |
| RF-21 | (Could) Melhorar um texto escrito pela pessoa. | Corrige gramática e clareza sem acrescentar fatos; mostra a diferença antes de aplicar. |

## 4. Requisitos não funcionais

| ID | Categoria | Requisito | Meta verificável |
|---|---|---|---|
| RNF-01 | Segurança | Não coletar nem armazenar credenciais de portais. | Nenhum campo `password` lido; teste automatizado no crawler; revisão de código obrigatória. |
| RNF-02 | Privacidade | Mínimo acesso: a API devolve à extensão só os valores dos campos propostos para a tela atual. | O contrato de `POST /v1/mappings` não retorna o perfil inteiro; teste de contrato. |
| RNF-03 | Segurança | Comunicação autenticada e criptografada. | TLS 1.2+; tokens de acesso da extensão com validade ≤ 15 min e rotação de refresh token. |
| RNF-04 | Desempenho | Respostas rápidas o bastante para o fluxo de preenchimento. | Regras: p95 ≤ 300 ms para 100 campos. De-para com IA: p95 ≤ 10 s. Texto livre: p95 ≤ 15 s. Análise de vaga: p95 ≤ 30 s, com progresso visível. |
| RNF-05 | Confiabilidade da IA | A IA não escreve valores objetivos nem inventa evidências. | 0 ocorrências na suíte de avaliação de IA (ver [estratégia de testes](../04-qualidade/estrategia-de-testes.md#5-avaliação-de-ia-evals)); 100% dos "atende" com linha real. |
| RNF-06 | Acessibilidade | Plataforma e painel lateral conforme WCAG 2.1 AA. | Navegação completa por teclado; contraste AA; leitores de tela; auditoria axe sem erros críticos. |
| RNF-07 | Conformidade | LGPD: base legal, finalidade, consentimento por finalidade, direitos do titular. | Política de privacidade publicada; RIPD do MVP 1; exportação e exclusão em até 15 dias (meta: imediato). |
| RNF-08 | Compatibilidade | Extensão Manifest V3 em Chrome e Edge atuais. | Testes E2E nos dois navegadores no CI. |
| RNF-09 | Observabilidade | Logs técnicos e métricas sem dados pessoais. | Lista de campos permitidos nos logs; teste que falha se um valor do perfil aparecer no log. |
| RNF-10 | Manutenibilidade | O conector do portal tolera mudanças de layout. | Conector versionado; testes com páginas gravadas (fixtures HTML) do portal; alerta quando a taxa de campos reconhecidos cair. |
| RNF-11 | Linguagem | Interface em português do Brasil, simples e inclusiva. | Revisão de UX writing; sem jargão de RH ou de TI nas telas. |
| RNF-12 | Disponibilidade | Plataforma e API disponíveis no horário de uso. | 99,5% mensal no MVP 1. A extensão degrada para só regras se a IA estiver fora. |
| RNF-13 | Uso respeitoso do portal | A extensão não gera tráfego automatizado no portal. | Nenhuma requisição ao portal feita pelo Zyft; só leitura e escrita no DOM da página aberta. |

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-01 | Nenhuma candidatura é enviada pelo Zyft. O envio é sempre um clique da pessoa no portal. |
| RN-02 | Valor objetivo preenchido vem sempre do Perfil Mestre ou de edição feita pela pessoa no painel; nunca de texto gerado pela IA. |
| RN-03 | Texto gerado pela IA só usa fatos do perfil aprovado e é sempre revisado antes do uso. |
| RN-04 | Dados sensíveis ficam separados do perfil, não são enviados à IA e só são preenchidos com consentimento e marcação manual do campo. |
| RN-05 | Em caso de dúvida (correspondência ambígua, opção inexistente, valor divergente), o Zyft pergunta em vez de decidir. |
| RN-06 | O Zyft não promete aprovação, contratação ou compatibilidade. |
| RN-07 | Nenhuma funcionalidade do MVP 2 entra no MVP 1 sem decisão explícita de escopo (ADR ou revisão aprovada do DVP-E). |

## 6. Requisitos de UX

- O painel lateral sempre mostra **o que vai acontecer** antes de acontecer ("Vamos preencher 18 campos. 3 precisam de você.").
- Estados com cor **e** texto (pronto, ambíguo, ausente, bloqueado), nunca só cor.
- Textos sugeridos pela IA aparecem marcados como sugestão, com o botão "Usar este texto" e edição livre.
- Mensagens de erro dizem o que fazer ("A IA está indisponível. Os campos simples continuam funcionando.").
- Funciona para quem tem pouca familiaridade digital: um passo de cada vez, sem termos técnicos.

## 7. Matriz de rastreabilidade (DVP-E → DRP → arquitetura → testes)

| DVP-E | Requisitos | Componente (DAT) | ADR | Testes |
|---|---|---|---|---|
| E1 | RF-01, RF-07, RF-08 | Web, API (módulos conta, consentimento, privacidade) | 0007, 0008, 0014 | CA-01, CA-07, CA-08 |
| E2 | RF-02, RF-05, RF-06 | Web, API (perfil) | 0008 | CA-02, CA-05, CA-06 |
| E3 | RF-03, RF-04 | API (importação), IA | 0004, 0005 | CA-03, CA-04, evals de extração |
| E4 | RF-09 | Extensão, API (auth) | 0002, 0007 | CA-09 |
| E5 | RF-10 a RF-14, RF-16, RF-18 | Extensão (crawler, conector), API (mapeamento), IA | 0002, 0004, 0006, 0009, 0010, 0011 | CA-10 a CA-14, CA-16, CA-18; evals de de-para |
| E6 | RF-15 | API (redação), IA | 0004, 0005 | CA-15; evals de texto livre |
| E7 | RF-17 | Extensão, API (vaga), IA | 0013 | CA-17; evals de análise de vaga |
| E8 | RF-13, RF-14, RF-19 | Extensão | 0009, 0012 | CA-13, CA-14, CA-19 |
| E9 | RF-20 | API (auditoria), Web | 0015 | CA-20 |

## 8. Requisitos do MVP 2 (mapeados, fora do MVP 1)

| ID | Requisito | Origem |
|---|---|---|
| RF-M2-01 | Painel central de candidaturas confirmadas (cargo, empresa, portal, data, link, versão do currículo) | Reposicionamento §10 |
| RF-M2-02 | Estados configuráveis, próximas ações, lembretes, filtros e busca | Reposicionamento §10; protótipo (10 status) |
| RF-M2-03 | Conectores para outros portais | Reposicionamento §10 |
| RF-M2-04 | Importação ou sincronização de candidaturas com portais, quando viável e autorizado | Reposicionamento §10 |
| RF-M2-05 | Análise ampliada de vagas e comparação de oportunidades | Reposicionamento §5 |
| RF-M2-06 | Relatórios e automações adicionais, sob consentimento | Reposicionamento §5 |
| RF-M2-07 | Marketplace bilateral profissionais × empresas | Marketplace v1 (DRP antigo RF-01 a RF-05) |
| RF-M2-08 | Modalidades PJ e CLT em perfil unificado | Marketplace v1 |
| RF-M2-09 | Assinaturas fixas sem comissão | Marketplace v1 (RF-10 a RF-12) |
| RF-M2-10 | Matching e fila de prioridade (senioridade validada, reputação) | Marketplace v1 (RF-05, RF-07) |
| RF-M2-11 | Reputação e avaliações bilaterais imutáveis | Marketplace v1 (RF-06, RNF-01) |
| RF-M2-12 | Chat e liberação de contato | Marketplace v1 (RF-08, RF-09) |
