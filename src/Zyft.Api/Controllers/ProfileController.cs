using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Profiles;

namespace Zyft.Api.Controllers;

/// <summary>Perfil Profissional Mestre (RF-02, RF-05). Só a plataforma web vê o perfil inteiro (RNF-02).</summary>
[Route("api/v1/profile")]
[Authorize(Policy = Policies.WebUser)]
public sealed class ProfileController : ApiControllerBase
{
    /// <summary>Perfil completo com experiências, formação, cursos, competências e idiomas.</summary>
    [HttpGet]
    public Task<ProfileDto> Get(CancellationToken ct) => Sender.Send(new GetProfileQuery(), ct);

    /// <summary>Atualiza os dados básicos (substitui todos os campos básicos).</summary>
    /// <response code="422">Regra de negócio (ex.: categoria de CNH sem possuir CNH).</response>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<ProfileDto> Update(UpdateProfileCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>Perguntas curtas só sobre o que falta (RF-05).</summary>
    [HttpGet("missing")]
    public Task<IReadOnlyList<MissingItem>> Missing(CancellationToken ct) => Sender.Send(new GetMissingQuery(), ct);

    // ---------------- Experiências
    /// <summary>Lista as experiências.</summary>
    [HttpGet("experiences")]
    public Task<IReadOnlyList<ExperienceDto>> ListExperiences(CancellationToken ct) => Sender.Send(new ListExperiencesQuery(), ct);

    /// <summary>Adiciona experiência. Datas aceitam AAAA-MM ou AAAA-MM-DD. Término &lt; início → 422.</summary>
    [HttpPost("experiences")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddExperience(AddExperienceCommand cmd, CancellationToken ct) => Created201(await Sender.Send(cmd, ct));

    /// <summary>Edita experiência.</summary>
    [HttpPut("experiences/{id:guid}")]
    public Task<ExperienceDto> UpdateExperience(Guid id, UpdateExperienceCommand cmd, CancellationToken ct) => Sender.Send(cmd with { Id = id }, ct);

    /// <summary>Remove experiência.</summary>
    [HttpDelete("experiences/{id:guid}")]
    public async Task<IActionResult> DeleteExperience(Guid id, CancellationToken ct) { await Sender.Send(new DeleteExperienceCommand(id), ct); return NoContent(); }

    // ---------------- Formação
    /// <summary>Lista a formação.</summary>
    [HttpGet("educations")]
    public Task<IReadOnlyList<EducationDto>> ListEducations(CancellationToken ct) => Sender.Send(new ListEducationsQuery(), ct);

    /// <summary>Adiciona formação (level/status como nome do enum, ex.: Undergraduate / InProgress).</summary>
    [HttpPost("educations")]
    [ProducesResponseType(typeof(EducationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddEducation(AddEducationCommand cmd, CancellationToken ct) => Created201(await Sender.Send(cmd, ct));

    /// <summary>Edita formação.</summary>
    [HttpPut("educations/{id:guid}")]
    public Task<EducationDto> UpdateEducation(Guid id, UpdateEducationCommand cmd, CancellationToken ct) => Sender.Send(cmd with { Id = id }, ct);

    /// <summary>Remove formação.</summary>
    [HttpDelete("educations/{id:guid}")]
    public async Task<IActionResult> DeleteEducation(Guid id, CancellationToken ct) { await Sender.Send(new DeleteEducationCommand(id), ct); return NoContent(); }

    // ---------------- Cursos e certificações
    /// <summary>Lista cursos e certificações.</summary>
    [HttpGet("courses")]
    public Task<IReadOnlyList<CourseDto>> ListCourses(CancellationToken ct) => Sender.Send(new ListCoursesQuery(), ct);

    /// <summary>Adiciona curso (type = Course | Certification).</summary>
    [HttpPost("courses")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddCourse(AddCourseCommand cmd, CancellationToken ct) => Created201(await Sender.Send(cmd, ct));

    /// <summary>Edita curso.</summary>
    [HttpPut("courses/{id:guid}")]
    public Task<CourseDto> UpdateCourse(Guid id, UpdateCourseCommand cmd, CancellationToken ct) => Sender.Send(cmd with { Id = id }, ct);

    /// <summary>Remove curso.</summary>
    [HttpDelete("courses/{id:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid id, CancellationToken ct) { await Sender.Send(new DeleteCourseCommand(id), ct); return NoContent(); }

    // ---------------- Competências
    /// <summary>Lista competências.</summary>
    [HttpGet("skills")]
    public Task<IReadOnlyList<SkillDto>> ListSkills(CancellationToken ct) => Sender.Send(new ListSkillsQuery(), ct);

    /// <summary>Adiciona competência (nome único no perfil; duplicada → 422).</summary>
    [HttpPost("skills")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddSkill(AddSkillCommand cmd, CancellationToken ct) => Created201(await Sender.Send(cmd, ct));

    /// <summary>Edita competência.</summary>
    [HttpPut("skills/{id:guid}")]
    public Task<SkillDto> UpdateSkill(Guid id, UpdateSkillCommand cmd, CancellationToken ct) => Sender.Send(cmd with { Id = id }, ct);

    /// <summary>Remove competência.</summary>
    [HttpDelete("skills/{id:guid}")]
    public async Task<IActionResult> DeleteSkill(Guid id, CancellationToken ct) { await Sender.Send(new DeleteSkillCommand(id), ct); return NoContent(); }

    // ---------------- Idiomas
    /// <summary>Lista idiomas.</summary>
    [HttpGet("languages")]
    public Task<IReadOnlyList<LanguageDto>> ListLanguages(CancellationToken ct) => Sender.Send(new ListLanguagesQuery(), ct);

    /// <summary>Adiciona idioma (proficiency = Basic…Native).</summary>
    [HttpPost("languages")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddLanguage(AddLanguageCommand cmd, CancellationToken ct) => Created201(await Sender.Send(cmd, ct));

    /// <summary>Edita idioma.</summary>
    [HttpPut("languages/{id:guid}")]
    public Task<LanguageDto> UpdateLanguage(Guid id, UpdateLanguageCommand cmd, CancellationToken ct) => Sender.Send(cmd with { Id = id }, ct);

    /// <summary>Remove idioma.</summary>
    [HttpDelete("languages/{id:guid}")]
    public async Task<IActionResult> DeleteLanguage(Guid id, CancellationToken ct) { await Sender.Send(new DeleteLanguageCommand(id), ct); return NoContent(); }

    private IActionResult Created201(object value) => StatusCode(StatusCodes.Status201Created, value);
}
