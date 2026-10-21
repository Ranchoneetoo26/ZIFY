# Glossário

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Glossário |
| Versão | 1.1 |
| Status | Vigente |
| Dono | Product Owner |
| Documentos relacionados | [DVP-E](../01-produto/dvp-e.md), [DRP](../01-produto/drp.md), [DAT](../03-arquitetura/dat.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Versão inicial para a Candidatura Assistida. |
| 1.1 | 21/10/2026 | Termos da versão 2: área profissional, catálogo por área, campo personalizado, onboarding em passos, pendentes, planos, pacote, anúncio recompensado, placement, patrocinado, limite de uso, candidatura assistida como unidade de contagem. Definição de MVP 2 ajustada. |

## Termos

| Termo | Definição |
|---|---|
| **Ação sua** | Rótulo do painel lateral para campos que o Zyft nunca preenche e que a pessoa precisa resolver no portal (ver *campo bloqueado*). |
| **ADR** (Architecture Decision Record) | Registro curto de uma decisão de arquitetura ou de escopo: contexto, opções, decisão, consequências e status (Proposta, Aceita, Substituída, Rejeitada). Ficam em [`docs/03-arquitetura/adr/`](../03-arquitetura/adr/). |
| **Análise de vaga** | Leitura das informações **visíveis** de uma página de vaga e comparação de cada requisito com o Perfil Mestre, sempre com evidência. Requisito RF-17 (Should). |
| **Antipersona** | Perfil de uso que o produto recusa por princípio. No Zyft, quem quer disparar candidaturas em massa sem revisão. |
| **Anúncio recompensado** | Oferta patrocinada que a pessoa do plano Gratuito escolhe ver em troca de +3 candidaturas assistidas no mês, até 3 vezes (+9). Opcional e sempre iniciado por ela. RF-28. |
| **Área profissional** | Área escolhida no onboarding (Saúde, Tecnologia, Vendas e comercial, Administrativo e financeiro, Educação, Logística e operações, Jurídico ou Outra área). Muda as sugestões e as perguntas da área. Campo `workArea` do perfil. RF-24, [ADR-0019](../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md). |
| **Campo ambíguo** | Campo da página para o qual há mais de uma correspondência possível no perfil, ou cuja opção de seleção não corresponde claramente ao valor do perfil. Nunca vem marcado para preenchimento. |
| **Campo ausente** | Campo da página sem dado correspondente no perfil. O painel convida a pessoa a informar o dado (e, se ela quiser, guardá-lo no perfil). |
| **Campo bloqueado** | Campo que o Zyft nunca preenche automaticamente: senha, CAPTCHA, código de verificação, anexo, declaração ou aceite de termos, e dado sensível sem consentimento e marcação manual. Ver [ADR-0010](../03-arquitetura/adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md). |
| **Campo personalizado** | Informação criada pela pessoa em "Informações da sua área", com nome e valor livres (ex.: "Disponível para plantão noturno: sim"). Usada no de-para com a chave `custom:<id>` e nos fatos da IA. Não aceita dado sensível. RF-25, RN-15. |
| **Candidatura Assistida** | O produto do MVP 1: preparar e preencher candidaturas a partir do Perfil Mestre, com revisão e confirmação da pessoa. |
| **Candidatura assistida (unidade de contagem)** | Unidade do limite de uso: uma leitura de formulário (`POST /forms/mappings`) contada uma vez por pessoa e por página (origem + caminho) no mês. Ler de novo a mesma página não conta outra vez. RF-26, RNF-16. |
| **Catálogo por área** | Lista estática de sugestões por área (cargos, setores, ferramentas, competências, link pedido e registro profissional), servida por `GET /catalog/areas`. A pessoa sempre pode cadastrar valores próprios. RF-24. |
| **Conector (do portal)** | Conjunto de regras específicas de um portal de vagas: como reconhecer suas páginas, etapas, campos e componentes próprios. Complementa o crawler genérico. Ver [ADR-0011](../03-arquitetura/adr/0011-primeiro-portal-e-conectores-por-portal.md). |
| **Confirmação explícita** | Ação deliberada da pessoa (clique ou escolha) antes de preencher, salvar ou enviar. O envio no portal é sempre feito pela própria pessoa. Ver [ADR-0009](../03-arquitetura/adr/0009-nunca-enviar-confirmacao-explicita.md). |
| **Consentimento por finalidade** | Autorização separada e revogável para cada uso de dados: (a) IA para textos livres, (b) análise de vaga, (c) uso de dados sensíveis. |
| **Crawler (da página)** | Parte da extensão (*content script*) que lê os campos visíveis da página aberta: rótulo, seção, tipo, opções e valor atual. Não navega, não faz requisições ao portal e não lê senhas. |
| **Dado sensível** | Dado pessoal sensível na definição da LGPD (art. 5º, II): origem racial ou étnica, convicção religiosa, opinião política, saúde, vida sexual, dado genético ou biométrico, entre outros. No Zyft também se tratam com o mesmo cuidado gênero, deficiência (PcD), nome social, estado civil e filhos. Ver [ADR-0014](../03-arquitetura/adr/0014-dados-sensiveis-segregados-e-criptografados.md). |
| **De-para** | Correspondência entre cada campo da página e uma chave do Perfil Mestre. Feito em três camadas: regras determinísticas, IA (que só aponta a chave) e validação. Ver [ADR-0006](../03-arquitetura/adr/0006-regras-deterministicas-antes-da-ia.md). |
| **Evidência** | Linha real do Perfil Mestre que comprova um requisito da vaga. A IA aponta o número da linha; a interface mostra a linha, não um texto da IA. Ver [ADR-0013](../03-arquitetura/adr/0013-evidencia-por-linha-numerada-na-analise-de-vaga.md). |
| **Extensão** | Extensão de navegador Chrome/Edge (Manifest V3) com painel lateral, que lê e preenche a página do portal aberta pela pessoa. Ver [ADR-0002](../03-arquitetura/adr/0002-extensao-de-navegador-mv3-com-painel-lateral.md). |
| **Fixture (de página)** | Cópia gravada do HTML de uma página do portal, usada em testes automatizados do conector sem acessar o portal real. |
| **IA (camada de apoio)** | Modelo de linguagem usado só por meio da API do Zyft para organizar currículos, apontar chaves do perfil, sugerir textos e analisar vagas. Não controla o navegador. |
| **Importação de currículo** | Envio de um PDF (com texto) ou DOCX que vira uma **proposta** de perfil, revisada campo a campo antes de entrar no Perfil Mestre. |
| **Importar do portal** | Levar para o Perfil Mestre um valor que já está preenchido no portal, com aprovação da pessoa. |
| **Limite de uso** | Quantidade mensal de candidaturas assistidas, sugestões de IA, análises de vaga e importações no plano Gratuito. Ao exceder, a API responde `402 plan.limit_reached`. Nunca limita editar o perfil, exportar ou excluir os dados (RN-12). |
| **Manter portal** | Opção padrão quando o valor já preenchido no portal difere do perfil: o Zyft não substitui nada sem escolha da pessoa. |
| **MVP 1** | Primeira entrega: Candidatura Assistida (Perfil Mestre, importação de currículo, preenchimento assistido num primeiro portal, IA para textos livres, confirmação explícita). |
| **MVP 2** | Entrega posterior: painel central de candidaturas com lembretes, mais portais, marketplace bilateral, PJ/CLT, assinaturas do marketplace, matching e reputação. O plano Plus do assistente não é a assinatura do marketplace e está no MVP 1 ([ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md)). Ver [roadmap](../01-produto/roadmap.md). |
| **Onboarding em passos** (passaporte) | Primeira configuração do perfil: cerca de 25 perguntas em 6 capítulos, uma por tela, todas adiáveis. O perfil é apresentado como um passaporte, e cada item aprovado é um carimbo. RF-23, [ADR-0018](../03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md). |
| **Pacote avulso** | Compra única de R$ 4,90 que dá +15 candidaturas assistidas e +10 sugestões de IA, válida por 60 dias. RF-27. |
| **Painel lateral** | Interface da extensão aberta ao lado da página (*side panel*), onde a pessoa revisa, edita e aprova o que será preenchido. |
| **Pareamento** | Vinculação da extensão a uma conta da plataforma, iniciada na extensão e confirmada na plataforma. Pode ser revogada. Ver [ADR-0007](../03-arquitetura/adr/0007-autenticacao-e-pareamento-da-extensao.md). |
| **Patrocinado** | Rótulo obrigatório de todo anúncio, fora da moldura da interface do Zyft, junto de "Ocultar" e "Por que estou vendo isso?". RN-10. |
| **Pendentes** | Perguntas do onboarding que a pessoa deixou para depois. Aparecem num alerta no topo do Perfil Mestre, com "Responda agora"; enquanto faltarem, a extensão marca os campos correspondentes como Precisa de você. |
| **Perfil Profissional Mestre** (Perfil Mestre) | Fonte única de dados profissionais escolhida e aprovada pela pessoa: contato, objetivo, experiências, formação, cursos, certificações, competências, idiomas, links, preferências e respostas aprovadas. |
| **Persona de necessidade** | Categoria de necessidade (PN1–PN5) usada no MVP 1 no lugar de perfis demográficos. Ver [personas](../01-produto/personas.md). |
| **Placement** | Lugar fixo onde um anúncio pode aparecer (ex.: `web.profile.rail`, `ext.job.contextual`). Os placements proibidos estão na RN-08. RF-29. |
| **Plano Gratuito** | Plano padrão, sem custo: limites mensais de uso, até 15 respostas no banco, 1 navegador conectado e anúncios rotulados. RF-26. |
| **Plano Plus** | Assinatura de R$ 9,90/mês ou R$ 99/ano, com 7 dias grátis: sem anúncios, sem limites de uso e até 3 navegadores. No MVP 1 o pagamento é simulado (RN-13). RF-26. |
| **Portal (de vagas)** | Site de terceiros onde a pessoa se candidata (ex.: portais regionais, Gupy, LinkedIn). O MVP 1 suporta **um** portal, a definir. |
| **Portal demo** | Portal de vagas falso, servido localmente pelo protótipo em `/demo/`, para testes e demonstrações sem tocar em portais reais. |
| **Preencher mais tarde** | Opção presente em toda pergunta do onboarding. Pula a pergunta sem bloquear nada (RN-14) e a transforma em pendente. |
| **Proposta (de perfil)** | Resultado da importação de currículo antes da aprovação; nada dela é usado sem revisão. |
| **Registro de ações** (auditoria) | Lista legível, para a própria pessoa, das ações automatizadas: data, ação, portal e quantidade de campos, sem os valores. Requisito RF-20. |
| **Regras determinísticas** | Expressões e heurísticas fixas (PT/EN) que resolvem campos objetivos comuns sem IA, em milissegundos. |
| **Resposta aprovada** | Texto aprovado pela pessoa para uma pergunta recorrente ("Por que você quer trabalhar aqui?"), reaproveitável em perguntas parecidas, sempre com revisão antes do uso. |
| **Spike** | Investigação técnica curta e com prazo fixo para reduzir uma incerteza (ex.: viabilidade do primeiro portal). |
| **Texto livre** | Campo aberto (textarea) que pede redação: apresentação, motivação, resumo. Recebe sugestão da IA só com fatos aprovados. |
