using Microsoft.Extensions.DependencyInjection;
using StudentPartTime.Services;

namespace StudentPartTime.Infrastructure;

/// <summary>
/// FEATURE: ONLINE-CV
/// Registers the services added by the Online CV / job-recommendation feature.
/// Call from Program.cs: builder.Services.AddOnlineCvFeature();
/// </summary>
public static class OnlineCvServiceCollectionExtensions
{
    public static IServiceCollection AddOnlineCvFeature(this IServiceCollection services)
    {
        services.AddScoped<IJobRecommendationService, JobRecommendationService>();
        return services;
    }
}