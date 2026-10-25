using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Zyft.Application.Common;
using Zyft.Application.Common.Behaviors;
using Zyft.Application.Forms;
using Zyft.Application.Imports;
using Zyft.Application.Jobs;
using Zyft.Application.Writing;

namespace Zyft.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IAuthTokenIssuer, AuthTokenIssuer>();
        services.AddScoped<IConsentGuard, ConsentGuard>();
        services.AddScoped<IFieldMapper, FieldMapper>();
        services.AddScoped<IJobAnalyzer, JobAnalyzer>();
        services.AddScoped<IAnswerWriter, AnswerWriter>();
        services.AddScoped<IResumeParser, ResumeParser>();
        services.AddScoped<Billing.IUsageMeter, Billing.UsageMeter>();
        services.AddScoped<Billing.BillingState>();
        return services;
    }
}
