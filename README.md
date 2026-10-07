# Umbraco.Community.DocumentBlueprintsInContent

Adds **Document Blueprints** to the Umbraco **Content** section, behind a dedicated
user group permission — so editors can create and manage blueprints without being
given access to Settings.

The Settings tree is left exactly as it is: users with access to Settings keep the
copy they already know, and this adds a second, permission-gated entry point in
Content.

## Install

```bash
dotnet add package Umbraco.Community.DocumentBlueprintsInContent
```

Restart the site. Nothing else to configure.

## Usage

1. Go to **Users → User Groups** and open a group.
2. Under **Document Blueprints**, tick **Access in Content**, and save.
3. Members of that group who have access to the Content section now see a
   **Document Blueprints** menu below the content tree.

The permission grants full management of blueprints and their folders — create,
edit, move and delete — so grant it as you would any other editing right.

Umbraco permissions belong to user groups, not individual users. To grant access
to one person, create a group for it (for example "Blueprint Editors") and add
them to it — users can belong to several groups, so this does not affect their
other permissions.

### Creating blueprints

From the Content section, blueprints are created the usual way: open a document
and choose **Create Document Blueprint** from its actions menu. The Blueprints
menu itself offers folders, for organising them.

The "Document Blueprint for…" option in the create menu stays in Settings only.
It needs Document Type access that Content-only editors do not have, so showing
it to them would present an empty picker and a dead end.

## How it works

**Backoffice** — registers the permission (`GE.DocumentBlueprint.ContentAccess`)
and a Content sidebar menu that appears only when the permission is granted. It
reuses Umbraco's own blueprint tree, and swaps in a section-aware version of the
children table so links resolve to whichever section you are in.

**Management API** — Umbraco's Document Blueprint endpoints require Settings
section access. On those endpoints only, this package substitutes a policy that
allows **Settings access, or Content access plus the permission above**. Every
other authorization check Umbraco performs is left untouched.

## Compatibility

Umbraco **17 and 18** (`[17.0.0, 19.0.0)`), .NET 10.

Earlier versions are not supported: Umbraco 16 and below are missing backoffice
APIs this package depends on — notably the `Umb.Condition.UserPermission.Fallback`
condition in 15 and earlier, which is what gates the menu by permission. Umbraco
14, 15 and 16 are also all past end-of-life.

## Uninstalling

Remove the package and restart the site. The Content menu and the permission option
disappear, and the Settings tree is unaffected. The permission stays recorded on any
user groups it was granted to, but without the package it has no effect.

## Contributing

Issues and pull requests are welcome on
[GitHub](https://github.com/gheorghe-efros/Umbraco.Community.DocumentBlueprintsInContent). See
[CONTRIBUTING.md](https://github.com/gheorghe-efros/Umbraco.Community.DocumentBlueprintsInContent/blob/main/CONTRIBUTING.md)
for working on the package locally.

## Licence

MIT
