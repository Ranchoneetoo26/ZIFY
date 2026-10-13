# ADR-0010 — Sem credenciais, sem contorno de segurança, campos bloqueados

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 12/10/2026 |
| Decisores | Arquiteto de software, Product Owner |
| Consultados | Responsável por privacidade, Dev extensão, QA |
| Requisitos | RF-11, RF-18; RNF-01, RNF-13; RN-01, RN-04 |
| Relacionados | ADR-0002, ADR-0006, ADR-0009, ADR-0014 |

## Contexto

O DVP-E define que o Zyft não pede, não guarda e não usa senhas de portais, e que CAPTCHA, autenticação adicional e verificações de segurança são resolvidos pela própria pessoa. Também há campos que, mesmo sem ser de segurança, exigem decisão humana:

- declarações e aceite de termos;
- anexos;
- dados sensíveis.

O protótipo implementa a maior parte disso:

- **Bloqueios no `mapper.js`:**
  - tipos `password`, `hidden`, `submit`, `button`, `image` e `reset` são ignorados;
  - CAPTCHA é detectado por expressão regular e vira `manual`;
  - `file` vira `manual` ("Anexe o arquivo manualmente");
  - dados sensíveis viram `sensitive`, com o valor só quando há consentimento, e vêm desmarcados;
  - checkboxes de declaração viram `declaration`, desmarcados.
- **`content.js`, ao preencher:** recusa `file` e `password`.

**Lacuna encontrada na revisão:** a função `scanFields()` de `content.js` não exclui `type=password` da leitura. O valor atual (`el.value`) de um campo de senha visível é coletado como `currentValue` e enviado à API em `POST /api/map-fields`. O servidor descarta o campo, mas o valor já trafegou. Isso viola RNF-01 e precisa ser corrigido no MVP 1.

## Fatores de decisão

- Segurança da conta da pessoa no portal.
- Termos de uso dos portais e legislação (contornar mecanismos de segurança pode ser ilícito).
- Confiança: a pessoa precisa ter certeza de que o Zyft nunca vê a senha.
- Clareza: os campos bloqueados precisam aparecer como "ação sua", não sumir.

## Opções consideradas

### Opção A — Guardar credenciais criptografadas para "facilitar" o login

- **Prós:** fluxo mais curto.
- **Contras:** risco de vazamento catastrófico; viola o DVP-E e a política de privacidade; caracteriza controle da conta da pessoa.

### Opção B — Ignorar os campos de segurança silenciosamente

- **Prós:** simples.
- **Contras:** a pessoa não sabe o que falta; formulários ficam incompletos sem aviso.

### Opção C — Lista fixa de bloqueios, aplicada na leitura e no preenchimento e mostrada como "ação sua"

- **Prós:** defesa em profundidade (extensão e API); a pessoa vê exatamente o que precisa fazer.
- **Contras:** as expressões regulares podem gerar falsos positivos (ex.: "Termos técnicos que domina" detectado como declaração). Isso é aceitável, porque o erro é a favor da segurança.

## Decisão

Escolhemos a **Opção C**, com estas regras obrigatórias:

1. **Credenciais:** o Zyft não pede, não armazena, não transmite e não usa senhas de portais. O content script **não lê** campos `type=password` nem campos com `autocomplete` igual a `current-password`, `new-password` ou `one-time-code`. Eles não entram na lista de campos enviada à API. **Corrige a lacuna do protótipo.**
2. **Mecanismos de segurança:** CAPTCHA, reCAPTCHA, hCaptcha, códigos de verificação, 2FA e bloqueios não são tocados nem contornados. O painel mostra "Verificação de segurança: sempre feita por você."
3. **Campos bloqueados** (nunca preenchidos automaticamente; aparecem como "ação sua"):
   - senha e códigos;
   - CAPTCHA;
   - anexos (`type=file`);
   - declarações, aceite de termos e consentimentos do portal;
   - dados sensíveis sem consentimento (c) **e** marcação manual por campo ([ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md)).
4. **Sem tráfego no portal:** o Zyft não faz requisições ao portal (nem pela extensão em segundo plano, nem pelo servidor). Leitura e escrita acontecem só no DOM da página aberta pela pessoa (RNF-13).
5. **Defesa em profundidade:** os bloqueios são aplicados na extensão (leitura e preenchimento) **e** na API (mapeamento), com a mesma lista vinda de `packages/field-rules`.

## Consequências

**Positivas**
- Nenhum incidente possível envolvendo senhas de portais, porque elas nunca existem no sistema.
- Posição clara diante das lojas de extensões e dos portais.

**Negativas**
- Falsos positivos deixam alguns campos legítimos para preenchimento manual.
- Em portais que pedem login no meio do formulário, o fluxo é interrompido até a pessoa autenticar.

**Riscos e mitigação**
- **Um campo de senha sem `type=password`** (ex.: campo de texto com máscara). Mitigação: bloqueio também por rótulo (`senha`, `password`, `pin`, `codigo`) e por `autocomplete`.
- **Regressão do bloqueio.** Mitigação: testes automatizados obrigatórios (abaixo).

## Conformidade

- Teste unitário de `scanFields`: uma fixture com `input[type=password]` preenchido não aparece no resultado e nenhum `currentValue` contém o valor.
- Teste de contrato da API: requisição com campo `password` é rejeitada ou o campo é descartado **e** registrado como violação no teste.
- Teste E2E: CAPTCHA, `file`, declaração e sensível aparecem como "ação sua" e permanecem intocados após "Preencher".
- Item obrigatório no checklist de PR que toca `content.js` ou as regras de bloqueio.

## Links

- [Segurança](../seguranca.md)
- Protótipo: `extension/content.js` (`scanFields`, `fill`), `server/src/mapper.js` (`SENSITIVE`, `DECLARATION`, `CAPTCHA`, `mapFields`)
