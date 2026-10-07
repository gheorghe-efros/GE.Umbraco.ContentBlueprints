namespace Umbraco.Community.DocumentBlueprintsInContent;

/// <summary>
/// Well-known identifiers used by this package's user group permission and its
/// authorization policy.
/// </summary>
public static class ContentBlueprintPermissions
{
    /// <summary>
    /// Fallback (user group) permission verb that grants access to Document Blueprints from the Content section.
    /// Must match the verb registered by the "entityUserPermission" manifest in wwwroot/App_Plugins/Umbraco.Community.DocumentBlueprintsInContent.
    /// </summary>
    /// <remarks>
    /// This value is stored on user groups in the database. Never change it after a release: every
    /// existing grant would silently stop working.
    /// </remarks>
    public const string ContentAccess = "GE.DocumentBlueprint.ContentAccess";

    /// <summary>
    /// Authorization policy that replaces Umbraco's Settings-only policy on the Document Blueprint Management API.
    /// </summary>
    public const string AccessPolicy = "GE.ContentBlueprintAccess";
}
