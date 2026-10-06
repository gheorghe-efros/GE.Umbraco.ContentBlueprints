using GE.Umbraco.ContentBlueprints.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.Authorization;

namespace GE.Umbraco.ContentBlueprints;

/// <summary>
/// Registers the Document Blueprint access policy, its handler, and the action
/// descriptor provider that applies the policy to Umbraco's blueprint endpoints.
/// </summary>
public class ContentBlueprintsComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ContentBlueprintAccessHandler>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IActionDescriptorProvider, ContentBlueprintActionDescriptorProvider>());

        builder.Services.AddOptions<AuthorizationOptions>()
            .PostConfigure<ILogger<ContentBlueprintsComposer>>((options, logger) =>
            {
                AuthorizationPolicy? corePolicy = options.GetPolicy(AuthorizationPolicies.TreeAccessDocumentTypes);
                if (corePolicy is null)
                {
                    // Without the core policy there are no blueprint endpoints to reconfigure. Never
                    // take the site down over this: Settings users keep blueprints, Content users don't.
                    logger.LogWarning(
                        "The Umbraco '{Policy}' authorization policy is not registered, so Document Blueprints will not be available in the Content section.",
                        AuthorizationPolicies.TreeAccessDocumentTypes);
                    return;
                }

                // Authenticate the same way as the core policy being replaced (back-office token scheme).
                options.AddPolicy(ContentBlueprintPermissions.AccessPolicy, policy => policy
                    .AddAuthenticationSchemes(corePolicy.AuthenticationSchemes.ToArray())
                    .AddRequirements(new ContentBlueprintAccessRequirement()));
            });
    }
}
