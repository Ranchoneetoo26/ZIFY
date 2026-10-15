using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Profiles;

namespace Zyft.Application.Profiles;

// Regras de FORMATO ficam nos validadores (400). Regras de NEGÓCIO (período, duplicidade) ficam no domínio (422).

// ================================================================== Experiências

public record ExperienceInput
{
    public string Company { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsCurrent { get; init; }
    public string? Activities { get; init; }
    public string? Tools { get; init; }
    public string? Results { get; init; }

    public ExperienceData ToData() => new(Company, Role, StartDate!.Value, EndDate, IsCurrent, Activities, Tools, Results);
}

public sealed class ExperienceInputValidator<T> : AbstractValidator<T> where T : ExperienceInput
{
    public ExperienceInputValidator()
    {
        RuleFor(x => x.Company).NotEmpty().WithMessage("Informe a organização.").MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().WithMessage("Informe o cargo.").MaximumLength(200);
        RuleFor(x => x.StartDate).NotNull().WithMessage("Informe a data de início.");
        RuleFor(x => x.Activities).MaximumLength(4000);
        RuleFor(x => x.Tools).MaximumLength(1000);
        RuleFor(x => x.Results).MaximumLength(4000);
    }
}

public sealed record ListExperiencesQuery : IRequest<IReadOnlyList<ExperienceDto>>;
public sealed record AddExperienceCommand : ExperienceInput, IRequest<ExperienceDto>;
public sealed record UpdateExperienceCommand : ExperienceInput, IRequest<ExperienceDto> { [JsonIgnore] public Guid Id { get; init; } }
public sealed record DeleteExperienceCommand(Guid Id) : IRequest<Unit>;
public sealed class AddExperienceValidator : AbstractValidator<AddExperienceCommand> { public AddExperienceValidator() => Include(new ExperienceInputValidator<AddExperienceCommand>()); }
public sealed class UpdateExperienceValidator : AbstractValidator<UpdateExperienceCommand> { public UpdateExperienceValidator() => Include(new ExperienceInputValidator<UpdateExperienceCommand>()); }

public sealed class ExperienceHandlers(IProfileRepository profiles, ICurrentUser current, IUnitOfWork uow) :
    IRequestHandler<ListExperiencesQuery, IReadOnlyList<ExperienceDto>>,
    IRequestHandler<AddExperienceCommand, ExperienceDto>,
    IRequestHandler<UpdateExperienceCommand, ExperienceDto>,
    IRequestHandler<DeleteExperienceCommand, Unit>
{
    public async Task<IReadOnlyList<ExperienceDto>> Handle(ListExperiencesQuery q, CancellationToken ct) =>
        ProfileFacts.OrderedExperiences(await profiles.LoadAsync(current, ct)).Select(ExperienceDto.From).ToList();

    public async Task<ExperienceDto> Handle(AddExperienceCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var e = p.AddExperience(cmd.ToData());
        await uow.SaveChangesAsync(ct);
        return ExperienceDto.From(e);
    }

    public async Task<ExperienceDto> Handle(UpdateExperienceCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var e = p.UpdateExperience(cmd.Id, cmd.ToData()) ?? throw new NotFoundException("Experiência não encontrada.");
        await uow.SaveChangesAsync(ct);
        return ExperienceDto.From(e);
    }

    public async Task<Unit> Handle(DeleteExperienceCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        if (!p.RemoveExperience(cmd.Id)) throw new NotFoundException("Experiência não encontrada.");
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ================================================================== Formação

public record EducationInput
{
    public string Institution { get; init; } = string.Empty;
    public string Course { get; init; } = string.Empty;
    public EducationLevel? Level { get; init; }
    public EducationStatus? Status { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }

    public EducationData ToData() => new(Institution, Course, Level!.Value, Status!.Value, StartDate!.Value, EndDate);
}

public sealed class EducationInputValidator<T> : AbstractValidator<T> where T : EducationInput
{
    public EducationInputValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().WithMessage("Informe a instituição.").MaximumLength(200);
        RuleFor(x => x.Course).NotEmpty().WithMessage("Informe o curso.").MaximumLength(200);
        RuleFor(x => x.Level).NotNull().WithMessage("Informe o nível.").IsInEnum().WithMessage("Nível inválido.");
        RuleFor(x => x.Status).NotNull().WithMessage("Informe a situação.").IsInEnum().WithMessage("Situação inválida.");
        RuleFor(x => x.StartDate).NotNull().WithMessage("Informe a data de início.");
    }
}

public sealed record ListEducationsQuery : IRequest<IReadOnlyList<EducationDto>>;
public sealed record AddEducationCommand : EducationInput, IRequest<EducationDto>;
public sealed record UpdateEducationCommand : EducationInput, IRequest<EducationDto> { [JsonIgnore] public Guid Id { get; init; } }
public sealed record DeleteEducationCommand(Guid Id) : IRequest<Unit>;
public sealed class AddEducationValidator : AbstractValidator<AddEducationCommand> { public AddEducationValidator() => Include(new EducationInputValidator<AddEducationCommand>()); }
public sealed class UpdateEducationValidator : AbstractValidator<UpdateEducationCommand> { public UpdateEducationValidator() => Include(new EducationInputValidator<UpdateEducationCommand>()); }

public sealed class EducationHandlers(IProfileRepository profiles, ICurrentUser current, IUnitOfWork uow) :
    IRequestHandler<ListEducationsQuery, IReadOnlyList<EducationDto>>,
    IRequestHandler<AddEducationCommand, EducationDto>,
    IRequestHandler<UpdateEducationCommand, EducationDto>,
    IRequestHandler<DeleteEducationCommand, Unit>
{
    public async Task<IReadOnlyList<EducationDto>> Handle(ListEducationsQuery q, CancellationToken ct) =>
        ProfileFacts.OrderedEducations(await profiles.LoadAsync(current, ct)).Select(EducationDto.From).ToList();

    public async Task<EducationDto> Handle(AddEducationCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var e = p.AddEducation(cmd.ToData());
        await uow.SaveChangesAsync(ct);
        return EducationDto.From(e);
    }

    public async Task<EducationDto> Handle(UpdateEducationCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var e = p.UpdateEducation(cmd.Id, cmd.ToData()) ?? throw new NotFoundException("Formação não encontrada.");
        await uow.SaveChangesAsync(ct);
        return EducationDto.From(e);
    }

    public async Task<Unit> Handle(DeleteEducationCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        if (!p.RemoveEducation(cmd.Id)) throw new NotFoundException("Formação não encontrada.");
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ================================================================== Cursos e certificações

public record CourseInput
{
    public string Name { get; init; } = string.Empty;
    public string? Institution { get; init; }
    public int? Hours { get; init; }
    public DateOnly? CompletedAt { get; init; }
    public CourseType Type { get; init; } = CourseType.Course;

    public CourseData ToData() => new(Name, Institution, Hours, CompletedAt, Type);
}

public sealed class CourseInputValidator<T> : AbstractValidator<T> where T : CourseInput
{
    public CourseInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Informe o nome do curso.").MaximumLength(200);
        RuleFor(x => x.Institution).MaximumLength(200);
        RuleFor(x => x.Hours).LessThanOrEqualTo(10000).WithMessage("Carga horária muito alta.");
        RuleFor(x => x.Type).IsInEnum().WithMessage("Tipo inválido.");
    }
}

public sealed record ListCoursesQuery : IRequest<IReadOnlyList<CourseDto>>;
public sealed record AddCourseCommand : CourseInput, IRequest<CourseDto>;
public sealed record UpdateCourseCommand : CourseInput, IRequest<CourseDto> { [JsonIgnore] public Guid Id { get; init; } }
public sealed record DeleteCourseCommand(Guid Id) : IRequest<Unit>;
public sealed class AddCourseValidator : AbstractValidator<AddCourseCommand> { public AddCourseValidator() => Include(new CourseInputValidator<AddCourseCommand>()); }
public sealed class UpdateCourseValidator : AbstractValidator<UpdateCourseCommand> { public UpdateCourseValidator() => Include(new CourseInputValidator<UpdateCourseCommand>()); }

public sealed class CourseHandlers(IProfileRepository profiles, ICurrentUser current, IUnitOfWork uow) :
    IRequestHandler<ListCoursesQuery, IReadOnlyList<CourseDto>>,
    IRequestHandler<AddCourseCommand, CourseDto>,
    IRequestHandler<UpdateCourseCommand, CourseDto>,
    IRequestHandler<DeleteCourseCommand, Unit>
{
    public async Task<IReadOnlyList<CourseDto>> Handle(ListCoursesQuery q, CancellationToken ct) =>
        ProfileDto.From(await profiles.LoadAsync(current, ct)).Courses;

    public async Task<CourseDto> Handle(AddCourseCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var c = p.AddCourse(cmd.ToData());
        await uow.SaveChangesAsync(ct);
        return CourseDto.From(c);
    }

    public async Task<CourseDto> Handle(UpdateCourseCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var c = p.UpdateCourse(cmd.Id, cmd.ToData()) ?? throw new NotFoundException("Curso não encontrado.");
        await uow.SaveChangesAsync(ct);
        return CourseDto.From(c);
    }

    public async Task<Unit> Handle(DeleteCourseCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        if (!p.RemoveCourse(cmd.Id)) throw new NotFoundException("Curso não encontrado.");
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ================================================================== Competências

public record SkillInput
{
    public string Name { get; init; } = string.Empty;
    public SkillLevel? Level { get; init; }
}

public sealed class SkillInputValidator<T> : AbstractValidator<T> where T : SkillInput
{
    public SkillInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Informe a competência.").MaximumLength(100);
        RuleFor(x => x.Level).IsInEnum().WithMessage("Nível inválido.");
    }
}

public sealed record ListSkillsQuery : IRequest<IReadOnlyList<SkillDto>>;
public sealed record AddSkillCommand : SkillInput, IRequest<SkillDto>;
public sealed record UpdateSkillCommand : SkillInput, IRequest<SkillDto> { [JsonIgnore] public Guid Id { get; init; } }
public sealed record DeleteSkillCommand(Guid Id) : IRequest<Unit>;
public sealed class AddSkillValidator : AbstractValidator<AddSkillCommand> { public AddSkillValidator() => Include(new SkillInputValidator<AddSkillCommand>()); }
public sealed class UpdateSkillValidator : AbstractValidator<UpdateSkillCommand> { public UpdateSkillValidator() => Include(new SkillInputValidator<UpdateSkillCommand>()); }

public sealed class SkillHandlers(IProfileRepository profiles, ICurrentUser current, IUnitOfWork uow) :
    IRequestHandler<ListSkillsQuery, IReadOnlyList<SkillDto>>,
    IRequestHandler<AddSkillCommand, SkillDto>,
    IRequestHandler<UpdateSkillCommand, SkillDto>,
    IRequestHandler<DeleteSkillCommand, Unit>
{
    public async Task<IReadOnlyList<SkillDto>> Handle(ListSkillsQuery q, CancellationToken ct) =>
        ProfileDto.From(await profiles.LoadAsync(current, ct)).Skills;

    public async Task<SkillDto> Handle(AddSkillCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var s = p.AddSkill(cmd.Name, cmd.Level);
        await uow.SaveChangesAsync(ct);
        return SkillDto.From(s);
    }

    public async Task<SkillDto> Handle(UpdateSkillCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var s = p.UpdateSkill(cmd.Id, cmd.Name, cmd.Level) ?? throw new NotFoundException("Competência não encontrada.");
        await uow.SaveChangesAsync(ct);
        return SkillDto.From(s);
    }

    public async Task<Unit> Handle(DeleteSkillCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        if (!p.RemoveSkill(cmd.Id)) throw new NotFoundException("Competência não encontrada.");
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ================================================================== Idiomas

public record LanguageInput
{
    public string Name { get; init; } = string.Empty;
    public LanguageProficiency? Proficiency { get; init; }
}

public sealed class LanguageInputValidator<T> : AbstractValidator<T> where T : LanguageInput
{
    public LanguageInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Informe o idioma.").MaximumLength(60);
        RuleFor(x => x.Proficiency).NotNull().WithMessage("Informe o nível.").IsInEnum().WithMessage("Nível inválido.");
    }
}

public sealed record ListLanguagesQuery : IRequest<IReadOnlyList<LanguageDto>>;
public sealed record AddLanguageCommand : LanguageInput, IRequest<LanguageDto>;
public sealed record UpdateLanguageCommand : LanguageInput, IRequest<LanguageDto> { [JsonIgnore] public Guid Id { get; init; } }
public sealed record DeleteLanguageCommand(Guid Id) : IRequest<Unit>;
public sealed class AddLanguageValidator : AbstractValidator<AddLanguageCommand> { public AddLanguageValidator() => Include(new LanguageInputValidator<AddLanguageCommand>()); }
public sealed class UpdateLanguageValidator : AbstractValidator<UpdateLanguageCommand> { public UpdateLanguageValidator() => Include(new LanguageInputValidator<UpdateLanguageCommand>()); }

public sealed class LanguageHandlers(IProfileRepository profiles, ICurrentUser current, IUnitOfWork uow) :
    IRequestHandler<ListLanguagesQuery, IReadOnlyList<LanguageDto>>,
    IRequestHandler<AddLanguageCommand, LanguageDto>,
    IRequestHandler<UpdateLanguageCommand, LanguageDto>,
    IRequestHandler<DeleteLanguageCommand, Unit>
{
    public async Task<IReadOnlyList<LanguageDto>> Handle(ListLanguagesQuery q, CancellationToken ct) =>
        ProfileFacts.OrderedLanguages(await profiles.LoadAsync(current, ct)).Select(LanguageDto.From).ToList();

    public async Task<LanguageDto> Handle(AddLanguageCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var l = p.AddLanguage(cmd.Name, cmd.Proficiency!.Value);
        await uow.SaveChangesAsync(ct);
        return LanguageDto.From(l);
    }

    public async Task<LanguageDto> Handle(UpdateLanguageCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var l = p.UpdateLanguage(cmd.Id, cmd.Name, cmd.Proficiency!.Value) ?? throw new NotFoundException("Idioma não encontrado.");
        await uow.SaveChangesAsync(ct);
        return LanguageDto.From(l);
    }

    public async Task<Unit> Handle(DeleteLanguageCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        if (!p.RemoveLanguage(cmd.Id)) throw new NotFoundException("Idioma não encontrado.");
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
