using System.Reflection;
using FluentValidation;
using Lumora.Server.Application.Clipboard;
using Lumora.Server.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Lumora.Server.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddOptions<ClipboardRetentionOptions>();

        return services;
    }
}
