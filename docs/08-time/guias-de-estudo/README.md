# Guias de estudo do time

| Campo | Valor |
|---|---|
| Documento | Guias de estudo por integrante |
| Versão | 1.0 |
| Data | 21/10/2026 |
| Status | Vigente (revisar a cada entrega) |
| Relacionados | [Papéis e RACI](../papeis-e-raci.md), [Por que o Zyft](../../00-contexto/por-que-o-zyft.md), [Resultados da pesquisa v2](../../05-pesquisa/resultados-v2.md), [DRP](../../01-produto/drp.md), [ADRs](../../03-arquitetura/adr/README.md) |

## Para que servem

A banca pode perguntar qualquer coisa a qualquer pessoa do time. Estes guias existem para que cada integrante consiga:

1. explicar o projeto inteiro em poucos minutos, com os números certos da pesquisa;
2. dominar a área do próprio papel a fundo, inclusive o "por quê" de cada decisão;
3. responder às perguntas difíceis com honestidade, sem inventar e sem se contradizer com os documentos;
4. abrir o arquivo certo na hora em que alguém pedir "mostra onde está isso".

**Importante:** são guias de estudo. Eles não registram quem escreveu qual código ou documento. A divisão de papéis está em [papéis e RACI](../papeis-e-raci.md); o histórico de autoria está no Git.

## Como estudar

1. Leia a parte comum (abaixo) e o seu guia.
2. Faça o exercício prático de 30 minutos do seu guia, com a API rodando.
3. Leia as perguntas difíceis em voz alta e responda sem olhar. Depois confira.
4. Leia também a seção 3 dos guias dos colegas: a banca não respeita fronteira de papel.
5. Quando a resposta for "não sabemos" ou "ainda não decidimos", diga isso e aponte onde a decisão está registrada (ADR em Proposta, pendência D-xx, hipótese na pesquisa). Isso vale mais do que uma resposta inventada.

## Ordem de leitura comum (cerca de 2 horas)

| # | Documento | Por quê |
|---|---|---|
| 1 | [Por que o Zyft](../../00-contexto/por-que-o-zyft.md) | A história, o problema e as decisões da v2 |
| 2 | [Resultados da pesquisa v2](../../05-pesquisa/resultados-v2.md) | Os números que justificam tudo; decore os principais |
| 3 | [Personas](../../01-produto/personas.md) | Rafael, Bia, Beatriz (hipótese) e a antipersona |
| 4 | [DRP](../../01-produto/drp.md) | RF, RNF e RN; os IDs que a banca vai citar |
| 5 | [Monetização](../../01-produto/monetizacao.md) e [ADR-0020](../../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) | O ponto mais questionável da v2 |
| 6 | [ADRs](../../03-arquitetura/adr/README.md), em especial 0004, 0009, 0010, 0012, 0014, 0017, 0018, 0019, 0020 | O "por quê" técnico |
| 7 | [Guia do usuário](../../06-operacao/guia-do-usuario.md) | Para demonstrar o sistema sem travar |

## Os números que todo mundo precisa saber

| Dado | Valor |
|---|---|
| Respostas válidas | 15 (16 linhas; 1 era cópia da v1 e foi excluída) |
| Área | Tecnologia 60% (9 de 15). Saúde: 1 resposta |
| Dor principal | Redigitar os mesmos dados: média 4,40, 80% com nota 4–5 |
| Manter perfis iguais | 3,60 e 53% (validada no limite) |
| Perguntas abertas | 3,27 e 40% (não validada) |
| Sem retorno das empresas | 4,40 e 87% (controle, fora do MVP 1) |
| Confiança no assistente | 3,4 de 5 (meta era 3,5) |
| Interesse | 3,73 |
| Recursos mais valorizados | Perfil único e importar currículo: 87% cada |
| Barreiras | Preço 47%, privacidade 40%, poucos sites 40% |
| Disposição a pagar | Não pagaria 3, até R$ 10 7, R$ 10–20 2, R$ 20–40 2, mais de R$ 40 1 |
| Preferência de uso | Gratuita com limite: 9 de 15 (60%) |

## Guias

| Integrante | Papel | Guia |
|---|---|---|
| Antonio Randolfo Barroso Neto | Product Owner, Product Manager e gestão | [antonio-po-pm.md](antonio-po-pm.md) |
| Guilherme Brogio Macedo da Silva | Arquiteto de software e full stack (acumula Arquiteto de IA e Privacidade) | [guilherme-arquitetura.md](guilherme-arquitetura.md) |
| Lucas Ulisses Clementino De Arruda E Silva | Frontend web e painel da extensão (acumula UX) | [lucas-frontend.md](lucas-frontend.md) |
| Matheus Guilherme Moreira De Oliveira | Backend .NET | [matheus-backend.md](matheus-backend.md) |

## Regra de ouro na banca

Se a resposta depende de um documento, cite o ID (RF-26, RN-09, ADR-0020, CA-29, PN3). Se não depende de documento nenhum, é opinião: diga que é opinião.
