using Jellyfin.Plugin.PersonalRatings.Data;
using Jellyfin.Plugin.PersonalRatings.Data.Repositories;
using Jellyfin.Plugin.PersonalRatings.Services;
using Jellyfin.Plugin.PersonalRatings.Web;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PersonalRatings;

/// <summary>
/// Registers plugin services with the Jellyfin host.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        serviceCollection.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        serviceCollection.AddHostedService<DatabaseInitializationHostedService>();

        serviceCollection.AddScoped<IRatingRepository, RatingRepository>();
        serviceCollection.AddScoped<ITagRepository, TagRepository>();
        serviceCollection.AddScoped<IDeleteAuditLogRepository, DeleteAuditLogRepository>();
        serviceCollection.AddScoped<IJellyfinItemResolver, JellyfinItemResolver>();
        serviceCollection.AddScoped<IJellyfinDeletionAdapter, JellyfinDeletionAdapter>();
        serviceCollection.AddSingleton<IPluginFeatureService, PluginFeatureService>();
        serviceCollection.AddScoped<IAuditLogService, AuditLogService>();
        serviceCollection.AddScoped<IRatingService, RatingService>();
        serviceCollection.AddScoped<ITagService, TagService>();
        serviceCollection.AddScoped<IDeletionService, DeletionService>();
        serviceCollection.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, PersonalRatingsWebStartupFilter>();
    }
}
