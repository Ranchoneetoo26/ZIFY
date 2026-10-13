# Política de Privacidade — Zyft

> **Rascunho para revisão jurídica.** Escrito pelo time de produto para orientar o MVP 1. Requer revisão jurídica, nomeação do encarregado e RIPD antes da publicação.

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Política de Privacidade |
| Versão | 0.1 (rascunho) |
| Status | Rascunho para revisão jurídica |
| Dono | Privacidade e segurança (encarregado a nomear) |
| Documentos relacionados | [Termos de uso](termos-de-uso.md), [Segurança](../03-arquitetura/seguranca.md), [Modelo de dados](../03-arquitetura/modelo-de-dados.md), ADRs [0010](../03-arquitetura/adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md), [0014](../03-arquitetura/adr/0014-dados-sensiveis-segregados-e-criptografados.md), [0015](../03-arquitetura/adr/0015-observabilidade-sem-dados-pessoais.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 0.1 | 12/10/2026 | Primeiro rascunho para a Candidatura Assistida (MVP 1). |

## 1. Quem somos

O Zyft *(razão social e CNPJ a definir)* é o **controlador** dos dados pessoais tratados para prestar o serviço de Candidatura Assistida, nos termos da Lei nº 13.709/2018 (LGPD).

## 2. Resumo

- Usamos seus dados para montar seu Perfil Mestre e ajudar você a preencher candidaturas.
- **Não pedimos nem guardamos a senha dos portais de vagas.**
- **Não enviamos candidaturas por você.**
- Dados sensíveis ficam separados, não vão para a IA e só são usados se você autorizar e marcar cada campo.
- Você pode exportar e excluir tudo a qualquer momento.
- Não vendemos seus dados e não os usamos para publicidade.

## 3. Dados que tratamos

| Categoria | Exemplos | De onde vem | Finalidade |
|---|---|---|---|
| Conta | E-mail, nome, identificador do login Google | Você | Criar e proteger sua conta |
| Perfil Mestre | Contato, localização, objetivo, experiências, formação, cursos, certificações, competências, idiomas, links, preferências (disponibilidade, pretensão salarial, viagens, mudança, tipos de contrato, CNH) | Você ou o currículo que você importar | Preencher candidaturas com sua aprovação |
| Currículo importado | Texto extraído do PDF ou DOCX | Você | Gerar a proposta de perfil para revisão. **O arquivo não é guardado após a importação.** |
| Respostas aprovadas | Textos que você aprovou para perguntas recorrentes | Você | Reaproveitar em perguntas parecidas, com revisão |
| Campos do formulário | Rótulos, tipos e opções dos campos da página aberta; valor atual quando você compara portal × perfil | A página do portal que você abriu | Propor o preenchimento da tela atual. **Senhas, CAPTCHA e anexos não são lidos.** |
| Página de vaga | Texto visível da vaga | A página que você abriu | Análise de vaga, se você autorizar |
| Consentimentos | O que você autorizou e quando | Você | Comprovar e respeitar suas escolhas |
| Registro de ações | Data, ação, domínio do portal e número de campos, **sem valores** | Uso do serviço | Mostrar a você o que foi feito |
| Dados técnicos | Identificador pseudônimo, versão da extensão, contagens e durações, sem conteúdo | Uso do serviço | Segurança, funcionamento e melhoria, sem dados pessoais nos logs |
| Dados sensíveis (opcional) | Gênero, raça/etnia, deficiência e outros que você decidir guardar | Você | Responder campos sensíveis **somente** com consentimento específico e marcação manual de cada campo |

## 4. Bases legais

| Finalidade | Base legal (LGPD) |
|---|---|
| Conta, Perfil Mestre, preenchimento assistido, registro de ações | Execução de contrato (art. 7º, V) |
| IA para textos livres e análise de vaga | Consentimento (art. 7º, I), separado por finalidade e revogável |
| Dados sensíveis | Consentimento específico e destacado (art. 11, I) |
| Segurança, prevenção a fraude e logs técnicos sem conteúdo | Legítimo interesse (art. 7º, IX), com avaliação documentada |
| Cumprimento de obrigação legal | Art. 7º, II |

*(Bases legais a confirmar na revisão jurídica e no RIPD.)*

## 5. Inteligência artificial

- A IA recebe só o necessário para cada tarefa: para identificar campos, recebe os rótulos dos campos e a lista de **tipos** de informação do perfil, sem os valores; para sugerir textos e analisar vagas, recebe os fatos aprovados do perfil.
- **Dados sensíveis nunca são enviados à IA.**
- Seus dados não são usados para treinar modelos de IA. *(Confirmar no contrato com o provedor, se houver provedor externo.)*
- A IA é executada em infraestrutura contratada pelo Zyft ([ADR-0005](../03-arquitetura/adr/0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md)). Se um provedor externo de IA for usado, ele será listado na seção 6 antes do início do uso.

## 6. Compartilhamento e operadores

Não vendemos nem alugamos dados pessoais. Compartilhamos apenas com **operadores** que nos ajudam a prestar o serviço, sob contrato e instruções do Zyft:

| Operador | Função | Local *(a definir)* |
|---|---|---|
| Provedor de nuvem | Hospedagem da API, plataforma e banco de dados | A definir |
| Infraestrutura de IA | Execução do modelo de linguagem | A definir |
| E-mail transacional | Envio de e-mails de verificação e avisos | A definir |
| Login Google (opcional) | Autenticação, se você escolher | Google |

Se houver transferência internacional, ela seguirá os mecanismos do art. 33 da LGPD.

**Os portais de vagas** recebem os dados que **você** decide preencher e enviar neles. O Zyft não envia dados aos portais por conta própria; o tratamento pelos portais segue a política de cada um.

## 7. Retenção

| Dado | Retenção |
|---|---|
| Conta e Perfil Mestre | Enquanto a conta existir |
| Arquivo de currículo | Não é guardado; o texto da proposta é descartado após a revisão |
| Registro de ações | Enquanto a conta existir, limitado aos últimos 12 meses *(a confirmar)* |
| Logs técnicos sem conteúdo | Até 30 dias |
| Backups | Expiram no ciclo de retenção (7 dias); dados excluídos não são restaurados |

Ao excluir a conta, os dados são apagados imediatamente do banco principal.

## 8. Segurança

- Comunicação criptografada (TLS) e autenticação em todas as chamadas.
- Dados sensíveis segregados e criptografados em nível de campo.
- Tokens de curta duração para a extensão, com revogação pela plataforma.
- Logs e métricas sem dados pessoais.
- Acesso interno restrito e registrado.

Em caso de incidente de segurança com risco relevante, comunicaremos você e a ANPD, conforme o art. 48 da LGPD.

## 9. Seus direitos

Você pode, a qualquer momento:

- confirmar e acessar seus dados;
- corrigir dados incompletos ou desatualizados (direto no Perfil Mestre);
- exportar seus dados em formato legível (JSON), pela plataforma;
- excluir seus dados e sua conta, pela plataforma;
- revogar consentimentos, nas configurações, sem perder as funções que não dependem deles;
- obter informação sobre compartilhamento;
- peticionar à ANPD.

Pedidos que não puderem ser feitos pela plataforma: *(canal do encarregado a definir)*. Prazo de resposta: até 15 dias.

## 10. A extensão de navegador

- A extensão só lê a página do portal suportado ou de sites que você autorizou, e só quando você aciona uma ação no painel.
- Não lê campos de senha, CAPTCHA nem anexos.
- Não faz requisições aos portais e não monitora sua navegação.
- O uso de dados segue a política de "uso limitado" das lojas de extensões: os dados servem apenas para a função descrita e não são vendidos nem usados para publicidade.

## 11. Crianças e adolescentes

O Zyft não é destinado a menores de 16 anos. *(Idade e regras a confirmar na revisão jurídica.)*

## 12. Encarregado (DPO)

Encarregado pelo tratamento de dados pessoais: *(nome e contato a definir antes da publicação)*.

## 13. Alterações

Mudanças relevantes serão avisadas na plataforma e por e-mail antes de entrarem em vigor. Mudanças de finalidade que dependam de consentimento pedirão novo consentimento.
