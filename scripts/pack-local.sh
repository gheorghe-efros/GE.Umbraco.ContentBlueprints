#!/usr/bin/env bash
# Packs the package into the local feed and builds the test site against the .nupkg,
# exactly as a user would install it. (By default the site uses a project reference.)
#
# Each run gets a unique prerelease version (e.g. 1.0.0-dev.20261007093000). NuGet
# caches packages by version, so repacking the same version would silently keep
# serving the old build; a fresh version guarantees the site gets this one.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
project="$root/src/Umbraco.Community.DocumentBlueprintsInContent"
feed="$root/artifacts/local-feed"

base_version="$(dotnet msbuild "$project" -getProperty:Version)"
version="$base_version-dev.$(date +%Y%m%d%H%M%S)"

# Only ever keep the build being made: old ones would pile up in the feed.
rm -f "${feed:?}"/Umbraco.Community.DocumentBlueprintsInContent.*.nupkg "${feed:?}"/Umbraco.Community.DocumentBlueprintsInContent.*.snupkg

dotnet pack "$project" -c Release -o "$feed" -p:Version="$version"

# The test site floats to the newest version in the feed, but a no-op restore does
# not re-evaluate floating versions, so force it.
dotnet restore "$root/test/TestSite" -p:UsePackageReference=true --force-evaluate
dotnet build "$root/test/TestSite" -p:UsePackageReference=true --no-restore

# Restore also copies each dev build into the global NuGet cache. Remove this package's
# older dev builds from there; published versions and every other package are untouched.
cache="$(dotnet nuget locals global-packages --list | sed -n 's/^global-packages: //p')"
package_cache="${cache%/}/umbraco.community.documentblueprintsincontent"
if [ -n "$cache" ] && [ -d "$package_cache" ]; then
  for old in "$package_cache"/*-dev.*; do
    [ -d "$old" ] && [ "$(basename "$old")" != "$version" ] && rm -rf "${old:?}"
  done
fi

echo
echo "Packed $version and built test/TestSite against it. Start it with:"
echo
echo "  dotnet run --project test/TestSite --no-build"
echo
echo "(Without --no-build, dotnet run rebuilds with the default project reference.)"
