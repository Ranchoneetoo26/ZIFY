"""Gera o PDF revisado da Entrega 1 (Pesquisa e Documentação).

Mesmo conteúdo e visual do PDF entregue em 15/10/2026. Única mudança de conteúdo: a linha
"Tecnologia planejada" passa a citar C#/.NET 8 (ASP.NET Core), conforme o ADR-0017.

Uso: python3 ferramentas/entregas/gerar_entrega1.py [saida.pdf]
"""
import os
import sys

from reportlab.lib.units import cm
from reportlab.platypus import PageBreak, Paragraph, SimpleDocTemplate, Spacer

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from estilo import ESTILOS, MARGEM, MARGEM_VERT, PAGE, bullet, rodape, tabela  # noqa: E402

TITULO = "Zyft – Entrega 1: Pesquisa e Documentação"
SAIDA_PADRAO = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "..", "..", "entregas", "Zyft_Entrega1_Pesquisa_e_Documentacao.pdf")
)

C = ESTILOS["corpo"]
H1 = ESTILOS["h1"]
H2 = ESTILOS["h2"]


def P(t, s=C):
    return Paragraph(t, s)


def capa():
    return [
        Spacer(1, 5 * cm),
        P("Zyft", ESTILOS["capa_titulo"]),
        Spacer(1, 10),
        P("Assistente de Candidatura a Vagas", ESTILOS["capa_sub"]),
        Spacer(1, 6),
        P("Entrega 1 – Pesquisa e Documentação", ESTILOS["capa_sub"]),
        Spacer(1, 4 * cm),
        P("Integrantes", ESTILOS["capa_cab"]),
        P("Antonio Randolfo Barroso Neto", ESTILOS["capa_txt"]),
        P("Guilherme Brogio Macedo da Silva", ESTILOS["capa_txt"]),
        P("Lucas Ulisses Clementino De Arruda E Silva", ESTILOS["capa_txt"]),
        P("Matheus Guilherme Moreira De Oliveira", ESTILOS["capa_txt"]),
        Spacer(1, 2 * cm),
        P("15 de outubro de 2026", ESTILOS["capa_txt"]),
        PageBreak(),
    ]


def corpo():
    s = []
    a = s.append
    a(P("Apresentação", H1))
    a(P("O Zyft é um assistente digital que reduz o trabalho repetitivo de se candidatar a vagas pela internet, "
        "mantendo a pessoa no controle de cada decisão. Este documento apresenta o problema que a equipe escolheu "
        "trabalhar, o público-alvo, a pesquisa com usuários, a persona, a jornada, a solução proposta, o levantamento "
        "inicial das funcionalidades e as duas metáforas que guiarão o projeto."))
    a(P("<b>Sobre a pesquisa:</b> o questionário com usuários está em aplicação e ainda não atingiu a amostra mínima "
        "definida pela equipe. Por isso, as necessidades, dores e personas apresentadas aqui foram construídas a partir "
        "da observação de formulários reais de candidatura e de um protótipo técnico, e serão revisadas quando os "
        "resultados forem consolidados."))

    a(P("1. Definição do problema", H1))
    a(P("Quem se candidata a vagas pela internet repete os mesmos dados em vários sites, mantém currículos "
        "inconsistentes entre plataformas e trava em perguntas abertas. A equipe identificou cinco problemas principais:"))
    for b in [
        "<b>P1 – Dados repetidos:</b> preencher dados parecidos em sites diferentes; há formulários com 40 ou mais campos.",
        "<b>P2 – Informações inconsistentes:</b> currículos e perfis em plataformas distintas ficam incompletos, desatualizados ou divergentes.",
        "<b>P3 – Perguntas abertas:</b> campos de texto exigem boas respostas, e a pessoa não quer (nem deve) inventar conteúdo.",
        "<b>P4 – Formulários demorados:</b> etapas longas e difíceis de revisar.",
        "<b>P5 – Adequação à vaga:</b> é difícil saber se o próprio perfil atende ao que a vaga pede.",
    ]:
        a(bullet(b))
    a(P("Um sexto problema, lembrar em quais vagas já se candidatou, foi deixado para uma segunda versão do produto."))
    a(P("As alternativas atuais falham de formas diferentes: preencher à mão é lento; o preenchimento automático do "
        "navegador cobre apenas nome, e-mail e endereço; a “candidatura fácil” funciona só dentro de um portal; robôs de "
        "envio em massa geram candidaturas ruins e podem violar os termos dos sites; e chatbots genéricos inventam "
        "informações e exigem copiar e colar."))

    a(P("2. Objetivo do sistema", H1))
    a(P("Ajudar a pessoa a preparar e preencher candidaturas a partir das suas próprias informações verdadeiras, com "
        "revisão de cada resposta e confirmação de cada ação. <b>O Zyft nunca envia nada sozinho.</b> A primeira versão "
        "(MVP 1, “Candidatura Assistida”) tem quatro objetivos mensuráveis:"))
    for b in [
        "<b>O1:</b> reduzir o esforço de candidatura, comparando o tempo gasto com e sem o Zyft.",
        "<b>O2:</b> gerar confiança no preenchimento, medida pelos campos aceitos sem edição e por uma nota de confiança.",
        "<b>O3:</b> garantir que a IA não invente dados: nenhum valor objetivo (datas, cargos, nomes) escrito pela IA.",
        "<b>O4:</b> aprender o que priorizar na segunda versão.",
    ]:
        a(bullet(b))
    a(PageBreak())

    a(P("3. Público-alvo", H1))
    a(P("Qualquer pessoa que se candidata a vagas pela internet: quem busca o primeiro emprego, profissionais "
        "experientes, pessoas empregadas buscando mudança, desempregadas, estudantes e pessoas em transição de carreira. "
        "O produto não se limita a uma área, nível de experiência ou modalidade de trabalho."))
    a(P("A equipe também definiu uma <b>antipersona</b>: quem quer disparar candidaturas em massa sem revisão ou "
        "automatizar a conta do portal sem supervisão. O produto não atende esse uso: não tem botão de envio, não tem "
        "fila de vagas e não guarda senhas."))
    a(P("<b>Contexto:</b> o projeto começou como um marketplace de talentos de tecnologia. Em outubro de 2026, a equipe "
        "reposicionou a proposta para a Candidatura Assistida por quatro motivos: a dor é imediata e ampla; um "
        "marketplace exige atrair empresas e candidatos ao mesmo tempo; um protótipo já comprovou a parte técnica "
        "central; e o risco de começar é menor. O marketplace ficou como direção para a segunda versão."))

    a(P("4. Pesquisa com usuários", H1))
    a(P("Metodologia", H2))
    for b in [
        "<b>Instrumento:</b> questionário anônimo no Google Forms, com cerca de 5 minutos e 25 perguntas divididas em "
        "cinco seções: sobre você; como você se candidata hoje; o que atrapalha; com quem você se parece; e o que você "
        "acha da ideia.",
        "<b>Participantes:</b> pessoas que se candidataram a pelo menos uma vaga online nos últimos 6 meses, de qualquer "
        "área, idade ou escolaridade.",
        "<b>Amostra:</b> mínimo de 15 respostas para qualquer conclusão; meta de 60, com pelo menos 10 em cada grupo "
        "(primeiro emprego, empregados buscando mudança, desempregados e transição de carreira).",
        "<b>Divulgação:</b> grupos de vagas da região, cursos fora da área de TI, LinkedIn e grupos de WhatsApp da "
        "equipe, evitando comunidades só de tecnologia.",
        "<b>Etapa qualitativa:</b> entrevistas de 20 minutos com 5 a 8 pessoas e teste de usabilidade em um portal de "
        "demonstração.",
        "<b>Ética:</b> respostas anônimas, contato opcional e nenhum dado pessoal guardado no repositório do projeto.",
    ]:
        a(bullet(b))
    a(P("Critérios de validação", H2))
    a(P("Uma dor é considerada validada quando tem média igual ou maior que 3,5 (escala de 1 a 5), pelo menos 50% de "
        "notas 4 ou 5 e pelo menos 15 respostas válidas. Uma persona é confirmada quando é escolhida por pelo menos 15% "
        "dos participantes, com identificação média igual ou maior que 3,5."))
    a(PageBreak())
    a(P("Hipóteses em teste", H2))
    a(tabela([
        ["ID", "Hipótese", "Validada se"],
        ["H1", "Preencher os mesmos dados em cada site incomoda (P1)", "Critério de dor"],
        ["H2", "Manter currículo e perfis consistentes incomoda (P2)", "Critério de dor"],
        ["H3", "Perguntas abertas incomodam (P3)", "Critério de dor"],
        ["H4", "Saber se tem o perfil da vaga incomoda (P5)", "Critério de dor"],
        ["H5", "Entender o formulário e revisar sem erro incomodam (P4)", "Critério de dor em pelo menos um dos dois itens"],
        ["H6", "Uma candidatura típica leva 15 minutos ou mais", "≥ 50% das respostas em “15 a 30 min” ou “mais de 30 min”"],
        ["H7", "A confirmação antes do envio gera confiança", "Média ≥ 3,5"],
        ["H8", "O público não se concentra em tecnologia", "Nenhuma área acima de 50%"],
    ], [1.2 * cm, 8.3 * cm, 6.5 * cm]))
    a(Spacer(1, 8))
    a(P("O questionário inclui duas dores de controle: “lembrar onde já me candidatei” (necessidade prevista para a "
        "segunda versão) e “não ter retorno das empresas” (dor que o Zyft não resolve). Elas servem para comparar a "
        "intensidade das dores que o produto pretende resolver."))
    a(P("Situação atual: a primeira versão do questionário foi testada internamente e a versão final está em aplicação "
        "desde 12/10/2026. Os resultados serão apresentados na próxima entrega."))

    a(P("5. Principais necessidades", H1))
    for b in [
        "<b>PN1 – Reduzir tarefas repetitivas</b>, reaproveitando informações já informadas.",
        "<b>PN2 – Organizar informações profissionais</b> corretas e atualizadas em um só lugar.",
        "<b>PN3 – Apoio para redigir respostas abertas</b> usando apenas fatos verdadeiros.",
        "<b>PN4 – Entender o que a vaga pede</b> e o que do próprio histórico atende.",
        "<b>PN5 – Orientação no preenchimento:</b> o que cada campo pede e o que ainda falta.",
    ]:
        a(bullet(b))

    a(P("6 e 7. Principais dificuldades e pontos de dor", H1))
    a(P("A tabela relaciona cada dificuldade observada ao ponto de dor que ela causa e à necessidade correspondente."))
    a(tabela([
        ["Dificuldade observada", "Ponto de dor", "Necessidade"],
        ["Formulários longos, com blocos repetidos e datas em formatos diferentes",
         "Perda de tempo e de dados quando a página expira", "PN1"],
        ["Currículo antigo, perfil em rede profissional e cadastro em portal regional com informações divergentes",
         "Insegurança sobre qual versão está certa; medo de informar algo errado", "PN2"],
        ["Campos abertos com limite de caracteres", "Desistência da vaga; medo de exagerar ou soar genérico", "PN3"],
        ["Descrições de vaga confusas, com requisitos implícitos", "Tempo perdido com vagas que não combinam", "PN4"],
        ["Campos obrigatórios sem explicação, perguntas sensíveis e aceite de termos",
         "Medo de errar sem poder corrigir; insegurança com dados pessoais", "PN5"],
    ], [6 * cm, 7 * cm, 3 * cm]))
    a(Spacer(1, 8))
    a(P("Entre os receios mapeados estão compartilhar contato, documentos, endereço, pretensão salarial e dados "
        "sensíveis, além de enviar respostas abertas escritas por IA."))

    a(P("8. Persona", H1))
    a(P("Personas por necessidade", H2))
    a(P("A equipe definiu as personas como categorias de necessidade, e não como perfis demográficos: a mesma pessoa "
        "pode ter mais de uma necessidade, e a principal muda conforme o momento da busca."))
    a(tabela([
        ["Persona", "Quer", "Frustrações", "Como o Zyft ajuda"],
        ["PN1 – Reduzir tarefas repetitivas", "Reaproveitar dados e digitar menos",
         "Formulários longos, blocos repetidos, dados perdidos", "Perfil Mestre único; a extensão propõe o valor de cada campo"],
        ["PN2 – Organizar informações", "Dados corretos e atualizados", "Inconsistência entre sites, medo de errar",
         "Importa o currículo como proposta, aprovada campo a campo"],
        ["PN3 – Apoio na redação", "Respostas claras, sem inventar", "Medo de exagerar ou soar genérico",
         "Sugere texto usando só fatos aprovados"],
        ["PN4 – Adequação à vaga", "Saber se o histórico atende", "Requisitos implícitos, tempo perdido",
         "Marca cada requisito como atende, parcial, não encontrado ou a confirmar, com evidência"],
        ["PN5 – Orientação no formulário", "Entender cada campo e revisar com calma",
         "Mensagens de erro vagas, insegurança com dados", "Painel lateral com o estado de cada campo e alertas"],
    ], [3.4 * cm, 3.4 * cm, 4.1 * cm, 5.1 * cm]))
    a(Spacer(1, 8))
    a(P("Como referência secundária, a equipe usa quatro arquétipos de comportamento: <b>Maratonista</b> (candidata-se "
        "com frequência), <b>Estrategista</b> (poucas vagas, quer acertar), <b>Iniciante Inseguro</b> e <b>Explorador</b> "
        "(está mudando de área)."))
    a(P("Persona principal: Camila", H2))
    a(P("Para representar a jornada, a equipe criou a Camila, que reúne as necessidades PN1, PN3 e PN5."))
    a(tabela([
        ["Aspecto", "Descrição"],
        ["Quem é", "Camila, 24 anos, recém-formada em Administração, desempregada, em busca do primeiro emprego formal."],
        ["Comportamento", "Candidata-se a várias vagas por semana em portais diferentes e usa um currículo em PDF."],
        ["Familiaridade digital", "Média: usa muito o celular, mas se perde em formulários longos."],
        ["Dor central", "Digita os mesmos dados toda vez e deixa as perguntas abertas para o final, às vezes desistindo da vaga."],
        ["Medos", "Informar algo errado ou “exagerar” e perder a oportunidade; não confia em ferramentas que enviam coisas sozinhas."],
        ["O que espera", "Terminar mais rápido, entender o que cada campo pede e ter certeza de que nada será enviado sem sua autorização."],
    ], [3.8 * cm, 12.2 * cm]))
    a(Spacer(1, 8))

    a(P("9. Jornada do usuário", H1))
    a(P("A jornada mostra o fluxo principal do MVP 1 sob o ponto de vista da Camila."))
    a(tabela([
        ["Etapa", "O que a pessoa faz", "O que o Zyft faz", "Sentimento"],
        ["1. Descobrir e criar conta", "Cria a conta e escolhe os consentimentos",
         "Explica o que será feito com os dados; IA e dados sensíveis começam desligados", "Curiosidade e cautela"],
        ["2. Montar o Perfil Mestre", "Importa o currículo (PDF/DOCX) ou preenche à mão",
         "Propõe os campos e pergunta só o que falta", "Alívio de fazer uma vez só"],
        ["3. Revisar e aprovar", "Aceita, edita ou descarta cada campo", "Nada entra no perfil sem aprovação", "Controle e confiança"],
        ["4. Abrir o portal", "Abre o portal de vagas e faz login sozinha",
         "Não pede nem guarda senha; só atua em portais suportados", "Segurança"],
        ["5. Ler o formulário", "Clica em “Ler formulário” no painel lateral",
         "Classifica cada campo: pronto, ambíguo, ausente, divergente ou “ação sua”", "Clareza sobre o que falta"],
        ["6. Revisar e preencher", "Marca, edita e confirma “Preencher”",
         "Preenche só o que foi aprovado; sugere textos com fatos aprovados", "Economia de tempo"],
        ["7. Conferir e enviar", "Resolve pendências, confere e clica em enviar no próprio portal",
         "Nunca clica em enviar, salvar ou aceitar termos", "Autonomia"],
        ["8. Concluir", "Vê a confirmação e o registro das ações", "Registra as ações sem guardar os valores preenchidos",
         "Satisfação e vontade de voltar"],
    ], [3.2 * cm, 4.3 * cm, 5.4 * cm, 3.1 * cm]))
    a(Spacer(1, 8))
    a(P("<b>Pontos de atenção na jornada:</b> se a IA estiver indisponível, o Zyft continua só com regras; em campo "
        "ambíguo, a pessoa escolhe; se o valor do portal for diferente do perfil, o padrão é manter o do portal; campos "
        "sensíveis, CAPTCHA, senhas e termos são sempre “ação sua”; PDFs escaneados ou acima de 10 MB pedem outro "
        "formato; e em sites não suportados a extensão fica inativa."))

    a(P("10. Descrição da solução proposta", H1))
    a(P("O Zyft é formado por três partes:"))
    for b in [
        "<b>Plataforma web</b> com o Perfil Profissional Mestre: conta, consentimentos, importação de currículo, "
        "exportação e exclusão de dados.",
        "<b>Extensão para Chrome e Edge</b> com um painel lateral que lê e preenche o formulário da página aberta.",
        "<b>Camada de inteligência artificial</b>, usada apenas através do servidor do Zyft.",
    ]:
        a(bullet(b))
    a(P("O preenchimento acontece em três etapas: primeiro, regras fixas identificam os campos mais comuns; depois, a "
        "IA apenas indica qual informação do perfil corresponde a cada campo, sem escrever o valor; por fim, o sistema "
        "confere se essa informação realmente existe no perfil. Textos livres usam apenas fatos que a pessoa aprovou."))
    a(P("Regras do produto", H2))
    for b in [
        "Nada é enviado sem confirmação, e a extensão nunca clica em enviar, salvar, aceitar ou avançar.",
        "A IA não inventa fatos nem escreve valores de campos objetivos.",
        "O Zyft não pede nem guarda senhas de portais e não contorna CAPTCHA ou autenticação.",
        "Dados sensíveis ficam separados da IA e só são usados com consentimento e marcação manual.",
        "O Zyft não promete contratação nem compatibilidade com a vaga.",
    ]:
        a(bullet(b))

    a(P("11. Levantamento inicial das funcionalidades", H1))
    a(P("As funcionalidades foram priorizadas em três níveis: <b>Essencial</b> (obrigatória no MVP 1), "
        "<b>Importante</b> (deve entrar se houver tempo) e <b>Desejável</b> (pode entrar)."))
    a(tabela([
        ["Prioridade", "Funcionalidade", "Requisito"],
        ["Essencial", "Conta, login e consentimentos separados (IA em textos, análise de vaga, dados sensíveis)", "RF-01, RF-07"],
        ["Essencial", "Exportar dados em JSON e excluir tudo", "RF-08"],
        ["Essencial", "Perfil Profissional Mestre editável (contato, experiências, formação, cursos, competências, idiomas, links, preferências)", "RF-02"],
        ["Essencial", "Importação de currículo em PDF com texto ou DOCX de até 10 MB, revisada campo a campo", "RF-03, RF-04"],
        ["Essencial", "Perguntas só sobre dados faltantes ou ambíguos; respostas aprovadas reutilizáveis", "RF-05, RF-06"],
        ["Essencial", "Conexão da extensão com a conta e atuação só em portal suportado ou site autorizado", "RF-09, RF-10"],
        ["Essencial", "Leitura do formulário, identificação dos campos e painel lateral com estados", "RF-11 a RF-13"],
        ["Essencial", "Preenchimento apenas dos campos aprovados, sem enviar", "RF-14"],
        ["Essencial", "Sugestão de texto para campos abertos usando fatos aprovados", "RF-15"],
        ["Essencial", "Comparação entre o valor do portal e o do perfil (padrão: manter o do portal)", "RF-16"],
        ["Essencial", "Bloqueio de dados sensíveis, termos, anexos, CAPTCHA e senhas (“ação sua”)", "RF-18"],
        ["Essencial", "Confirmação de conclusão e registro das ações sem guardar valores", "RF-19, RF-20"],
        ["Importante", "Análise da vaga com evidências reais do perfil, sem nota ou porcentagem", "RF-17"],
        ["Desejável", "Melhoria de gramática de textos da própria pessoa; registro do currículo anexado", "RF-21, RF-22"],
    ], [2.4 * cm, 10.9 * cm, 2.7 * cm]))
    a(Spacer(1, 8))

    a(P("Requisitos de qualidade", H2))
    for b in [
        "<b>Privacidade:</b> a extensão recebe apenas os valores dos campos da tela atual; registros sem dados pessoais; "
        "conformidade com a LGPD, com exportação e exclusão em até 15 dias.",
        "<b>Acessibilidade:</b> WCAG 2.1 nível AA, navegação por teclado e estados indicados por cor e texto.",
        "<b>Desempenho:</b> preenchimento por regras em até 300 ms para formulários de até 100 campos; sugestão de texto "
        "em até 15 s; análise de vaga em até 30 s.",
        "<b>Linguagem:</b> português do Brasil simples e inclusivo.",
    ]:
        a(bullet(b))
    a(P("Fora do escopo e próximos passos", H2))
    for b in [
        "<b>Nunca fará:</b> envio automático ou em massa, guardar senhas, contornar CAPTCHA, prometer contratação.",
        "<b>Segunda versão:</b> painel central de candidaturas, lembretes, mais portais, análise de vaga ampliada e o "
        "marketplace original.",
        # Única alteração de conteúdo em relação ao PDF entregue (ADR-0017).
        "<b>Tecnologia planejada:</b> API em C# com .NET 8 (ASP.NET Core), banco PostgreSQL e um modelo de IA aberto "
        "executado em servidor próprio.",
        "<b>Principais riscos:</b> nenhum portal ser viável técnica e juridicamente, a IA inventar valores, a pesquisa "
        "não validar as dores e falta de confiança em uma extensão que lê formulários.",
    ]:
        a(bullet(b))

    a(P("12. As duas metáforas do projeto", H1))
    a(P("A equipe escolheu duas metáforas que traduzem os princípios do Zyft para a experiência do usuário: o "
        "<b>Copiloto</b> e o <b>Passaporte</b>."))
    a(P("Metáfora 1: o Copiloto", H2))
    a(P("No avião, quem pilota é o piloto; o copiloto lê os instrumentos, aponta o que viu e sugere, mas não assume o "
        "comando nem decola sozinho."))
    for b in [
        "<b>Por que escolhemos:</b> traduz o princípio central do produto, “a pessoa decide”. A IA aponta qual "
        "informação do perfil serve para cada campo, mas nunca escreve valores objetivos nem envia nada.",
        "<b>Como contribui para a experiência:</b> o painel lateral funciona como o painel de instrumentos, mostrando o "
        "estado de cada campo (pronto, ambíguo, ausente, ação sua). A pessoa entende o que vai acontecer antes de "
        "acontecer, o que reduz o medo de errar e a desconfiança em relação à IA, duas barreiras identificadas na pesquisa.",
        "<b>Limite da metáfora:</b> o Copiloto nunca “aterrissa” por conta própria; o botão de enviar do portal é sempre "
        "da pessoa. Isso afasta a ideia de robô de candidatura em massa.",
    ]:
        a(bullet(b))
    a(P("Metáfora 2: o Passaporte", H2))
    a(P("O Perfil Profissional Mestre funciona como um passaporte: um documento único, pessoal e verdadeiro, "
        "apresentado em qualquer “fronteira” (cada portal de vagas), com carimbos que só valem depois de aprovados."))
    for b in [
        "<b>Por que escolhemos:</b> resolve o problema de repetir os mesmos dados em cada site e de manter currículos "
        "diferentes. Todo mundo entende que um passaporte é feito uma vez e reutilizado em várias viagens.",
        "<b>Como contribui para a experiência:</b> cada informação só entra no perfil depois de aprovada, como um "
        "carimbo, e por isso a IA só usa fatos confirmados. A metáfora também comunica que os dados pertencem à pessoa: "
        "ela pode revisar, exportar ou apagar tudo.",
        "<b>Limite da metáfora:</b> um passaporte não garante a entrada no país. Da mesma forma, o Zyft não promete "
        "contratação nem compatibilidade com a vaga.",
    ]:
        a(bullet(b))
    a(P("Como as duas se complementam", H2))
    a(P("O <b>Passaporte</b> explica <i>o que</i> o Zyft guarda e reaproveita: informações verdadeiras e aprovadas. O "
        "<b>Copiloto</b> explica <i>como</i> ele ajuda: sugere e aponta, e a pessoa decide. Juntas, sustentam a promessa "
        "do produto: menos trabalho repetitivo, sem perder o controle."))
    return s


def main():
    saida = sys.argv[1] if len(sys.argv) > 1 else SAIDA_PADRAO
    os.makedirs(os.path.dirname(saida), exist_ok=True)
    doc = SimpleDocTemplate(
        saida, pagesize=PAGE, leftMargin=MARGEM, rightMargin=MARGEM, topMargin=MARGEM_VERT, bottomMargin=MARGEM_VERT,
        title=TITULO,
    )
    doc.build(capa() + corpo(), onFirstPage=lambda c, d: None, onLaterPages=rodape(TITULO))
    print(saida)


if __name__ == "__main__":
    main()
