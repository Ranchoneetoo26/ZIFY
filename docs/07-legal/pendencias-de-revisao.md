# Pendências de revisão dos textos legais

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Lista do que revisar nos termos de uso e na política de privacidade antes de publicar |
| Versão | 1.0 |
| Status | Aberto. Nenhum item foi resolvido no texto legal: esta lista só aponta o que mudou no produto e o que o texto ainda diz |
| Dono | Product Owner, com Privacidade e segurança (encarregado a nomear) |
| Documentos relacionados | [Termos de uso](termos-de-uso.md), [Política de privacidade](politica-de-privacidade.md), páginas `web/legal/termos.html` e `web/legal/privacidade.html`, [Segurança](../03-arquitetura/seguranca.md), [Frontend web §8](../03-arquitetura/frontend-web.md), [API §4.8 a §4.13](../03-arquitetura/api.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 02/11/2026 | Primeira lista, depois da verificação de e-mail, do aceite dos termos com versão, da troca e recuperação de senha e do rascunho do onboarding em `localStorage`. Cada item foi conferido por busca nos arquivos citados. |

## Como usar

Os textos legais são rascunho e **não foram reescritos** nesta rodada: mudar cláusula é decisão de revisão jurídica e do encarregado, não do time de desenvolvimento. Cada item abaixo diz onde está o texto, o que ele afirma e o que o produto faz hoje. Ao resolver um item, atualize o texto em `docs/07-legal/` **e** a página correspondente em `web/legal/` (as duas cópias hoje dizem a mesma coisa), registre a versão nova no histórico do documento legal e marque o item aqui.

## Lista

| # | Onde | O texto diz hoje | O produto faz hoje | O que decidir |
|---|---|---|---|---|
| L1 | `termos-de-uso.md` (aviso no topo) e `web/legal/termos.html` (`legal__draft`) | "Rascunho para revisão jurídica. … Não publicar antes da revisão." Versão 0.2 (rascunho) | O cadastro já linka as duas páginas e exige o aceite (RF-01) | Revisão jurídica antes de qualquer uso com pessoas reais. Ao publicar, tirar o aviso de rascunho das duas cópias |
| L2 | `politica-de-privacidade.md` (aviso no topo) e `web/legal/privacidade.html` | "Rascunho para revisão jurídica. … Requer revisão jurídica, nomeação do encarregado e RIPD antes da publicação." Versão 0.3 (rascunho). Não usa a frase "Não publicar antes da revisão", mas tem o mesmo efeito | Igual a L1 | Igual a L1, mais encarregado nomeado (seção 12) e RIPD (RNF-07) |
| L3 | Termos, cláusula 2.1 (md e html) | "conta, criada com e-mail verificado ou com login Google" | **Não existe login Google.** A conta é criada com e-mail e senha e fica ativa na hora; a confirmação do e-mail existe desde 02/11/2026, mas é **só um aviso**: nada é bloqueado sem ela | Reescrever a 2.1 para o que existe (e-mail e senha, confirmação recomendada e não obrigatória), ou tornar a confirmação obrigatória no produto, o que é outra decisão (CA-01.4) |
| L4 | Privacidade, seção 3 ("Dados que tratamos"), linha "Conta" (md e html) | "E-mail, nome, identificador do login Google" | Sem login Google. A conta guarda e-mail, nome, hash da senha, data da confirmação do e-mail e **versão e data do aceite dos termos** | Tirar o identificador do Google e incluir confirmação do e-mail e registro do aceite |
| L5 | Privacidade, seção 6 (operadores) (md e html) | "Login Google (opcional) — Autenticação, se você escolher — Google" | Não há integração com o Google para login | Tirar a linha enquanto o login Google não existir |
| L6 | Privacidade, seção 6 (operadores), "E-mail transacional — Envio de e-mails de verificação e avisos — A definir" | Fala de e-mails de verificação e avisos | A API gera links de **confirmação de e-mail** (24 h) e de **redefinição de senha** (30 min), mas o envio é simulado: o link só vai para o log da API. Não há provedor contratado | Nomear o provedor antes de ligar o envio real; citar também o e-mail de redefinição de senha. Ver a lacuna em [segurança §3.3](../03-arquitetura/seguranca.md) |
| L7 | Termos, cláusula 11.1, e Privacidade, seção 13 (Alterações) | Mudanças "serão avisadas na plataforma e por e-mail" | Não há envio real de e-mail (L6). Mudar a versão dos termos obriga só quem se cadastra depois a aceitar; contas existentes não veem novo aceite | Definir como avisar quem já tem conta e se haverá novo aceite no login |
| L8 | `src/Zyft.Domain/Users/User.cs`, `Terms.CurrentVersion = "2026-10-22"` (devolvido por `GET /auth/terms` e gravado em cada conta) | Os textos não mostram esse identificador: as páginas dizem "Versão 0.2 (rascunho)" (termos, última mudança em 21/10/2026) e "Versão 0.3 (rascunho)" (privacidade, 22/10/2026) | O aceite registra `2026-10-22` como a versão aceita dos **dois** textos | Ao publicar, a constante precisa identificar exatamente o par de textos publicado. Sugestão: mostrar o mesmo identificador nas duas páginas e trocar a constante no mesmo PR de qualquer mudança nos textos |
| L9 | Privacidade, seções 3 (dados), 7 (retenção) e 8 (segurança) | Nada sobre dados guardados no navegador da pessoa | Desde 02/11/2026 o **rascunho do onboarding** fica em `localStorage` (`zyft.onboarding.draft.<id>`): o que a pessoa digitou e ainda não salvou (empresa, cargo, atividades, formação, idiomas…), por até 7 dias. Some ao concluir o onboarding e ao clicar em **Sair**, mas **não** ao fechar o navegador nem quando a sessão expira. Em computador compartilhado, outra pessoa pode ler | Parecer de privacidade sobre manter isso (ver [segurança T31](../03-arquitetura/seguranca.md) e [frontend §8](../03-arquitetura/frontend-web.md)). Se mantido, a política precisa dizer o que fica no navegador, por quanto tempo e como apagar; avaliar o mesmo para o refresh token em `sessionStorage` |
| L10 | Privacidade, seção 7 (retenção) | Não cita tokens de e-mail nem o histórico de troca de período do Plus | `UserTokens` (só o hash, uso único; a linha fica até a conta ser excluída) e `SubscriptionPeriodChanges` (enquanto a conta existir, também na exportação) | Incluir na tabela de retenção, ou decidir uma limpeza periódica dos tokens usados ou vencidos |

## Fora desta lista

Os itens que já estavam marcados como *a definir* nos próprios textos (razão social, CNPJ, foro, idade mínima, local dos operadores, canal do encarregado) continuam valendo e não foram repetidos aqui.
