#!/usr/bin/env bash
# Checks a packed .nupkg before it can reach nuget.org, where a published version
# can never be deleted or replaced. Used by both the CI and release workflows.
#
# Usage: scripts/verify-package.sh <folder containing the .nupkg>
set -euo pipefail

dir="${1:?usage: verify-package.sh <folder containing the .nupkg>}"

matches="$(find "$dir" -maxdepth 1 -name 'GE.Umbraco.ContentBlueprints.*.nupkg' ! -name '*.snupkg')"
count="$(printf '%s' "$matches" | grep -c . || true)"
# Exactly one: with several, any pick could be a stale build rather than the one just packed.
[ "$count" -eq 1 ] || { echo "::error::Expected exactly one GE.Umbraco.ContentBlueprints .nupkg in $dir, found $count"; exit 1; }
nupkg="$matches"
echo "Verifying $(basename "$nupkg")"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
unzip -qo "$nupkg" -d "$work"

failed=0
fail() { echo "::error::$1"; failed=1; }

# 1. Expected contents.
for f in \
  lib/net10.0/GE.Umbraco.ContentBlueprints.dll \
  README.md \
  icon.png \
  staticwebassets/App_Plugins/GE.Umbraco.ContentBlueprints/umbraco-package.json \
  staticwebassets/App_Plugins/GE.Umbraco.ContentBlueprints/entry-point.js \
  staticwebassets/App_Plugins/GE.Umbraco.ContentBlueprints/table-collection-view.element.js
do
  [ -f "$work/$f" ] || fail "Package is missing $f"
done

# 2. Supported Umbraco range: exactly one Umbraco dependency, declaring 17-18.
nuspec="$work/GE.Umbraco.ContentBlueprints.nuspec"
grep -q 'id="Umbraco.Cms.Web.Common" version="\[17.0.0, 19.0.0)"' "$nuspec" \
  || fail "Umbraco.Cms.Web.Common dependency range is not [17.0.0, 19.0.0)"
[ "$(grep -c '<dependency id="Umbraco\.' "$nuspec")" -eq 1 ] \
  || fail "Expected exactly one Umbraco dependency (Umbraco.Cms.Web.Common)"

# 3. The assembly must carry the package's version. A build followed by pack --no-build
#    -p:Version=X ships a DLL stamped with the csproj's version instead of X.
package_version="$(sed -n 's:.*<version>\(.*\)</version>.*:\1:p' "$nuspec" | head -n 1)"
assembly_version="$(python3 - "$work/lib/net10.0/GE.Umbraco.ContentBlueprints.dll" <<'PY'
import re, sys
data = open(sys.argv[1], 'rb').read()
# The informational version lands in the Win32 version resource as the UTF-16
# "ProductVersion" entry, e.g. "1.2.3+<commit>", padded to a 4-byte boundary.
key = 'ProductVersion'.encode('utf-16-le')
match = re.search(re.escape(key) + rb'(?:\x00\x00)+((?:[^\x00]\x00)+)', data)
print(match.group(1).decode('utf-16-le').split('+')[0] if match else '')
PY
)"
[ "$assembly_version" = "$package_version" ] \
  || fail "Assembly version '$assembly_version' does not match package version '$package_version'"

# 4. No Umbraco-18-only references. Umbraco 18.2 pulls in Microsoft.AspNetCore.OpenApi,
#    whose source generator injects references that do not resolve on Umbraco 17.
#    Compiling against the 17.0.0 floor keeps them out; this catches a regression.
if grep -aqiE 'openapi|swashbuckle' "$work/lib/net10.0/GE.Umbraco.ContentBlueprints.dll"; then
  fail "Assembly references OpenAPI/Swashbuckle, which do not resolve on Umbraco 17"
fi

[ "$failed" -eq 0 ] || exit 1
echo "Package OK"
