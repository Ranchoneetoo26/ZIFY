## O que muda

<!-- Descreva em 2–4 linhas o que este PR faz e por quê. -->

## Rastreabilidade

- Issue: #
- Requisitos: <!-- RF-xx, RNF-xx, RN-xx -->
- Critérios de aceite: <!-- CA-xx -->
- ADRs: <!-- ADR-NNNN (novo ou afetado) -->
- Escopo: [ ] MVP 1  [ ] MVP 2 (exige decisão registrada — RN-07)

## Regras inegociáveis

- [ ] A extensão não clica em enviar, salvar, aceitar ou avançar e não submete formulário (ADR-0009).
- [ ] Nenhum campo de senha, CAPTCHA, declaração ou anexo é lido ou preenchido automaticamente (ADR-0010).
- [ ] Valor objetivo vem do perfil; texto de IA só com fatos aprovados e revisado (ADR-0004, ADR-0013).
- [ ] Dados sensíveis segregados, fora da IA, só com consentimento e marcação (ADR-0014).
- [ ] Nenhum dado pessoal em logs, métricas, fixtures ou evals (ADR-0015).
- [ ] Nenhuma promessa de compatibilidade ou contratação em textos.

## Testes

- [ ] Testes unitários e de integração
- [ ] E2E (Chrome / Edge) — quando toca extensão ou web
- [ ] Testes negativos de confiança
- [ ] Evals de IA dentro dos limiares — quando toca IA
- [ ] axe sem erros críticos — quando toca interface

<!-- Cole a saída relevante, capturas ou vídeo curto. -->

## Documentação

- [ ] DRP / DAT / contratos (OpenAPI) / docs de arquitetura atualizados
- [ ] Histórico de versão dos documentos alterados

## Revisão

- Agentes de IA usados: <!-- ex.: dev-extensao, qa — ou "nenhum" -->
- [ ] Revisão humana por pessoa diferente do autor
- [ ] Parecer de privacidade e segurança (se toca dados pessoais, auth, permissões ou IA)
- [ ] Aceite do PO
