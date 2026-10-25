namespace Zyft.Application.Common;

/// <summary>Regras de sistema enviadas em toda chamada de IA (porte de <c>llm.js</c>).</summary>
public static class LlmPrompts
{
    /// <summary>Serialização legível (acentos sem escape) para trechos JSON dentro de prompts.</summary>
    public static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public const string SystemRules = """
        Você é o assistente de candidaturas de um candidato brasileiro.
        Regras inegociáveis:
        - Use SOMENTE fatos presentes no Perfil Mestre fornecido. Nunca invente empresas, cargos, cursos, certificações, competências, idiomas, resultados, datas ou experiências.
        - Se a informação não existir no perfil, diga que não existe (use null / lista vazia) em vez de supor.
        - Nunca infira dados sensíveis (gênero, raça/etnia, deficiência, religião, saúde, orientação sexual).
        - Escreva em português do Brasil, tom profissional e objetivo.
        """;
}
