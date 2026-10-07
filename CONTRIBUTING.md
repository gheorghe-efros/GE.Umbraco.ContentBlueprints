# Contributing

## Test site

`test/TestSite` is an Umbraco **17** harness — the lowest supported major, so that
regressions against the floor surface locally rather than in someone's production site.

It references the package project directly, so a fresh clone runs with no setup and
JS changes are served live:

```bash
dotnet run --project test/TestSite
```

On first run Umbraco shows its install wizard. To skip it, copy
`test/TestSite/appsettings.Local.json.example` to `appsettings.Local.json` and fill in
a throwaway password. That file is gitignored, as are the site's database, logs and
media.

Installing also writes an `Imaging:HMACSecretKey` into the tracked
`test/TestSite/appsettings.json`. Move it into `appsettings.Local.json` rather than
committing it.

## Testing the package as users install it

A project reference bypasses the `.nupkg`, including its static-asset wiring. To test
the package exactly as users get it, pack it into a local feed and run against that:

```bash
./scripts/pack-local.sh
dotnet run --project test/TestSite --no-build
```

Keep `--no-build`: a plain `dotnet run` rebuilds with the default project reference.
Each pack gets a unique `-dev.<timestamp>` version, because NuGet caches packages by
version and would otherwise keep serving a stale build. `nuget.config` pins this
package to the local feed, so the site can never pick up a published copy by accident.

## Umbraco version support

The package pins Umbraco to `[17.0.0, 19.0.0)`. NuGet resolves a range to its lowest
version, so `dotnet build` compiles against 17.0.0 and the compiler rejects anything
that exists only in 18 — that build is the real compatibility gate. Do not raise the
floor without re-checking the package on the new minimum.

The permission ID `Umbraco.Community.DocumentBlueprintsInContent.ContentAccess` is stored on user groups in the
database. Never change it after a release: every existing grant would silently stop
working.

## Checks and releases

CI builds, packs and verifies the package on every push to `main` and every pull
request (`scripts/verify-package.sh`). Releases publish to nuget.org from a version
tag on a commit that is on `main` — see `.github/workflows/release.yml` for the
one-time nuget.org setup.

To release:

1. In `CHANGELOG.md`, replace `Unreleased` with today's date for the new version.
2. Commit that to `main` and let CI pass.
3. Tag and push: `git tag v1.2.3` then `git push origin v1.2.3`.

The tag is the version: the workflow stamps it into the package and into
`umbraco-package.json`, so nothing else needs bumping. A published version can never
be replaced on nuget.org, so a mistake means releasing a new patch version.
