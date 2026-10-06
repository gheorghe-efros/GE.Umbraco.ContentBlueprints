using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;

namespace GE.Umbraco.ContentBlueprints.Authorization;

/// <summary>
/// Requirement for the Document Blueprint access policy. Satisfied by
/// <see cref="ContentBlueprintAccessHandler"/>.
/// </summary>
internal class ContentBlueprintAccessRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Grants Document Blueprint access to users with the Settings section (Umbraco's default rule),
/// or with the Content section and the <see cref="ContentBlueprintPermissions.ContentAccess"/> permission.
/// </summary>
internal class ContentBlueprintAccessHandler : AuthorizationHandler<ContentBlueprintAccessRequirement>
{
    private readonly IAuthorizationHelper _authorizationHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentBlueprintAccessHandler"/> class.
    /// </summary>
    /// <param name="authorizationHelper">Used to resolve the Umbraco user from the principal.</param>
    public ContentBlueprintAccessHandler(IAuthorizationHelper authorizationHelper)
        => _authorizationHelper = authorizationHelper;

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ContentBlueprintAccessRequirement requirement)
    {
        if (_authorizationHelper.TryGetUmbracoUser(context.User, out IUser? user) && HasAccess(user))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasAccess(IUser user)
    {
        if (user.AllowedSections.Contains(Constants.Applications.Settings))
        {
            return true;
        }

        return user.AllowedSections.Contains(Constants.Applications.Content)
               && user.Groups.Any(group => group.Permissions.Contains(ContentBlueprintPermissions.ContentAccess));
    }
}
