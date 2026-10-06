using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Web.Common.Authorization;

namespace GE.Umbraco.ContentBlueprints.Authorization;

/// <summary>
/// Swaps the Settings-only <see cref="AuthorizationPolicies.TreeAccessDocumentTypes"/> policy on the Document Blueprint
/// Management API controllers for <see cref="ContentBlueprintPermissions.AccessPolicy"/>. Other policies are untouched.
/// </summary>
internal class ContentBlueprintActionDescriptorProvider : IActionDescriptorProvider
{
    private const string ManagementApiControllersNamespace = "Umbraco.Cms.Api.Management.Controllers";
    private const string DocumentBlueprintControllersNamespace = ManagementApiControllersNamespace + ".DocumentBlueprint";

    private readonly IOptions<AuthorizationOptions> _authorizationOptions;
    private readonly ILogger<ContentBlueprintActionDescriptorProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentBlueprintActionDescriptorProvider"/> class.
    /// </summary>
    /// <param name="authorizationOptions">Used to confirm the replacement policy is registered.</param>
    /// <param name="logger">Reports when the blueprint endpoints could not be found.</param>
    public ContentBlueprintActionDescriptorProvider(
        IOptions<AuthorizationOptions> authorizationOptions,
        ILogger<ContentBlueprintActionDescriptorProvider> logger)
    {
        _authorizationOptions = authorizationOptions;
        _logger = logger;
    }

    /// <summary>
    /// Runs after the default <c>ControllerActionDescriptorProvider</c> (order -1000),
    /// so the descriptors already exist by the time this provider sees them.
    /// </summary>
    public int Order => 0;

    /// <inheritdoc />
    public void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
        // The composer skips registering the replacement policy when Umbraco's own is missing
        // (and logs why). Pointing endpoints at a policy that does not exist would break them,
        // so in that case leave every endpoint exactly as Umbraco configured it.
        if (_authorizationOptions.Value.GetPolicy(ContentBlueprintPermissions.AccessPolicy) is null)
        {
            return;
        }

        var managementApiPresent = false;
        var reconfigured = 0;

        foreach (ControllerActionDescriptor action in context.Results.OfType<ControllerActionDescriptor>())
        {
            string? controllerNamespace = action.ControllerTypeInfo.Namespace;
            if (!IsInNamespace(controllerNamespace, ManagementApiControllersNamespace))
            {
                continue;
            }

            managementApiPresent = true;
            if (!IsInNamespace(controllerNamespace, DocumentBlueprintControllersNamespace))
            {
                continue;
            }

            var settingsOnlyMetadata = action.EndpointMetadata
                .Where(metadata => metadata is IAuthorizeData { Policy: AuthorizationPolicies.TreeAccessDocumentTypes })
                .ToList();

            if (settingsOnlyMetadata.Count == 0)
            {
                continue;
            }

            foreach (object metadata in settingsOnlyMetadata)
            {
                action.EndpointMetadata.Remove(metadata);
            }

            action.EndpointMetadata.Add(new AuthorizeAttribute(ContentBlueprintPermissions.AccessPolicy));
            reconfigured++;
        }

        // The Management API is there but none of its blueprint endpoints matched: Umbraco has
        // moved them or renamed their policy. Say so, rather than fail silently with 403s.
        if (managementApiPresent && reconfigured == 0)
        {
            _logger.LogWarning(
                "No Document Blueprint endpoints using the '{Policy}' policy were found under '{Namespace}', so Document Blueprints will not be available in the Content section. This Umbraco version may not be supported.",
                AuthorizationPolicies.TreeAccessDocumentTypes,
                DocumentBlueprintControllersNamespace);
        }
    }

    /// <inheritdoc />
    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
    }

    private static bool IsInNamespace(string? candidate, string @namespace)
        => candidate is not null
           && (candidate == @namespace || candidate.StartsWith(@namespace + ".", StringComparison.Ordinal));
}
