// Catálogo por área (GET /catalog/areas). Se a API ainda não tiver o endpoint ou falhar,
// usa a mesma cópia estática (conteúdo do contrato v2) para o onboarding e o perfil não pararem.
import { api, asList } from './api.js';

const R = (label, placeholder) => ({ label, placeholder });
const FALLBACK = [
  { id: 'tecnologia', name: 'Tecnologia', roles: ['Desenvolvimento', 'Dados', 'Suporte técnico', 'Infraestrutura', 'QA e testes', 'Produto', 'Design de interfaces'], sectors: ['Fintech', 'Agro', 'Varejo', 'Saúde', 'Educação', 'Governo'], tools: ['Git', 'SQL', 'Excel', 'Jira', 'Docker', 'Python', 'Java', 'C#', 'JavaScript'], skills: ['Lógica de programação', 'Banco de dados', 'APIs', 'Testes', 'Metodologias ágeis', 'Atendimento a usuários'], linkLabel: 'GitHub', registration: null },
  { id: 'saude', name: 'Saúde', roles: ['Técnico(a) de enfermagem', 'Enfermeiro(a)', 'Recepção hospitalar', 'Farmácia', 'Fisioterapia', 'Análises clínicas'], sectors: ['Hospital', 'Clínica', 'Laboratório', 'Home care', 'Saúde pública'], tools: ['Prontuário eletrônico', 'Tasy', 'MV Soul', 'Pacote Office'], skills: ['Atendimento humanizado', 'Administração de medicamentos', 'Primeiros socorros', 'Biossegurança', 'Triagem', 'Trabalho em equipe'], linkLabel: 'Currículo Lattes', registration: R('Registro no conselho', 'Ex.: COREN-MG 123456') },
  { id: 'vendas', name: 'Vendas e comercial', roles: ['Vendedor(a)', 'Representante comercial', 'Executivo(a) de contas', 'Pré-vendas', 'Gerente de loja'], sectors: ['Varejo', 'Indústria', 'Serviços', 'Tecnologia', 'Imobiliário'], tools: ['CRM', 'Salesforce', 'RD Station', 'Excel', 'WhatsApp Business'], skills: ['Negociação', 'Prospecção', 'Pós-venda', 'Bater metas', 'Atendimento ao cliente', 'Apresentação'], linkLabel: null, registration: null },
  { id: 'administrativo', name: 'Administrativo e financeiro', roles: ['Assistente administrativo', 'Analista financeiro', 'Contas a pagar', 'Recursos humanos', 'Faturamento'], sectors: ['Indústria', 'Serviços', 'Varejo', 'Contabilidade', 'Setor público'], tools: ['Excel', 'SAP', 'TOTVS', 'Power BI', 'Pacote Office'], skills: ['Conciliação bancária', 'Rotinas de DP', 'Organização', 'Relatórios', 'Atendimento a fornecedores'], linkLabel: null, registration: R('Registro profissional (se tiver)', 'Ex.: CRC-MG 000000') },
  { id: 'educacao', name: 'Educação', roles: ['Professor(a)', 'Auxiliar de classe', 'Coordenação pedagógica', 'Monitor(a)', 'Tutor(a) EAD'], sectors: ['Escola particular', 'Escola pública', 'Ensino superior', 'Cursos livres', 'EAD'], tools: ['Google Classroom', 'Moodle', 'Canva', 'Pacote Office'], skills: ['Planejamento de aulas', 'Gestão de sala', 'Avaliação', 'Inclusão', 'Comunicação com famílias'], linkLabel: 'Currículo Lattes', registration: null },
  { id: 'logistica', name: 'Logística e operações', roles: ['Auxiliar de logística', 'Motorista', 'Conferente', 'Operador(a) de empilhadeira', 'Almoxarife'], sectors: ['Transporte', 'Varejo', 'Indústria', 'E-commerce'], tools: ['WMS', 'Coletor de dados', 'Excel', 'SAP'], skills: ['Conferência de carga', 'Controle de estoque', 'Roteirização', 'Segurança do trabalho'], linkLabel: null, registration: R('Cursos obrigatórios (NR, MOPP…)', 'Ex.: NR-11, MOPP') },
  { id: 'juridico', name: 'Jurídico', roles: ['Advogado(a)', 'Estagiário(a) de Direito', 'Paralegal', 'Assistente jurídico'], sectors: ['Escritório', 'Departamento jurídico', 'Setor público'], tools: ['PJe', 'Projuris', 'Pacote Office'], skills: ['Peticionamento', 'Pesquisa jurisprudencial', 'Contratos', 'Audiências'], linkLabel: null, registration: R('Inscrição na OAB', 'Ex.: OAB/MG 123.456') },
  { id: 'outra', name: 'Outra área', roles: [], sectors: [], tools: [], skills: [], linkLabel: null, registration: null },
];

export const OTHER = 'Outra área';
let cache = null;

const norm = (a) => ({
  id: a.id ?? a.name, name: a.name, roles: a.roles || [], sectors: a.sectors || [], tools: a.tools || [], skills: a.skills || [],
  linkLabel: a.linkLabel ?? a.link ?? null,
  registration: a.registration && a.registration.label ? a.registration : null,
});

export async function loadAreas() {
  if (cache) return cache;
  try {
    const list = asList(await api('/catalog/areas')).map(norm).filter((a) => a.name);
    cache = list.length ? list : FALLBACK;
    if (!cache.some((a) => a.name === OTHER)) cache = [...cache, FALLBACK[FALLBACK.length - 1]];
  } catch {
    cache = FALLBACK;
  }
  return cache;
}

/** Área pelo nome (ou id). Áreas livres ("Gastronomia") caem em "Outra área". */
export function findArea(areas, nameOrId) {
  const k = String(nameOrId || '').toLowerCase();
  return areas.find((a) => a.name.toLowerCase() === k || String(a.id).toLowerCase() === k) || null;
}
export const areaOrOther = (areas, n) => findArea(areas, n) || findArea(areas, OTHER) || FALLBACK[FALLBACK.length - 1];
