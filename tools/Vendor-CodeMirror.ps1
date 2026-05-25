# Re-vendor the CodeMirror 6 ESM bundles under
# src/ScribanTutorial/wwwroot/lib/codemirror/. Reads pinned versions from this
# script's $packages table, fetches each from unpkg, and overwrites the
# vendored copy. Idempotent. Reports SHA-256 of each file so a re-run on a
# clean checkout can be diff-compared.
#
# Why a script instead of npm: this app has no Node tooling and uses an
# importmap to resolve bare specifiers at runtime. The vendored files are
# served as-is; bumping a version is "fetch the new file and commit it".
#
# Bump procedure:
#   1. Update the version in $packages below.
#   2. ./tools/Vendor-CodeMirror.ps1
#   3. Update src/ScribanTutorial/wwwroot/lib/codemirror/VERSION.txt to match.
#   4. dotnet test (catches obvious tokenisation breakage via ContentBuilderTests).
#   5. dotnet run --project src/ScribanTutorial and spot-check the editor.
#   6. Commit the bumped files + VERSION.txt together.

[CmdletBinding()]
param(
    [string]$OutDir = (Join-Path $PSScriptRoot '..\src\ScribanTutorial\wwwroot\lib\codemirror')
)

$ErrorActionPreference = 'Stop'

# Each pin: bare specifier + version + unpkg sub-path + vendored filename.
# The sub-paths come from each upstream package.json's `exports.import` /
# `module` field. They're explicit so a future package layout change is a
# one-line edit here rather than guessing from convention.
$packages = @(
    [pscustomobject]@{ Name = '@codemirror/state';           Version = '6.5.2';  Path = 'dist/index.js';   Out = 'codemirror_state.js' }
    [pscustomobject]@{ Name = '@codemirror/view';            Version = '6.38.5'; Path = 'dist/index.js';   Out = 'codemirror_view.js' }
    [pscustomobject]@{ Name = '@codemirror/language';        Version = '6.12.3'; Path = 'dist/index.js';   Out = 'codemirror_language.js' }
    [pscustomobject]@{ Name = '@codemirror/commands';        Version = '6.8.0';  Path = 'dist/index.js';   Out = 'codemirror_commands.js' }
    [pscustomobject]@{ Name = '@lezer/common';               Version = '1.2.3';  Path = 'dist/index.js';   Out = 'lezer_common.js' }
    [pscustomobject]@{ Name = '@lezer/lr';                   Version = '1.4.2';  Path = 'dist/index.js';   Out = 'lezer_lr.js' }
    [pscustomobject]@{ Name = '@lezer/highlight';            Version = '1.2.1';  Path = 'dist/index.js';   Out = 'lezer_highlight.js' }
    [pscustomobject]@{ Name = '@marijn/find-cluster-break';  Version = '1.0.2';  Path = 'src/index.js';    Out = 'marijn_find-cluster-break.js' }
    [pscustomobject]@{ Name = 'style-mod';                   Version = '4.1.3';  Path = 'src/style-mod.js'; Out = 'style-mod.js' }
    [pscustomobject]@{ Name = 'w3c-keyname';                 Version = '2.2.8';  Path = 'index.js';        Out = 'w3c-keyname.js' }
    [pscustomobject]@{ Name = 'crelt';                       Version = '1.0.6';  Path = 'index.js';        Out = 'crelt.js' }
)

if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
}

Write-Host "Vendoring $($packages.Count) CodeMirror packages into $OutDir" -ForegroundColor Cyan
Write-Host ''

$results = foreach ($p in $packages) {
    $url = "https://unpkg.com/$($p.Name)@$($p.Version)/$($p.Path)"
    $dest = Join-Path $OutDir $p.Out
    Write-Host ("  {0,-32} {1,-8} → {2}" -f $p.Name, $p.Version, $p.Out)
    Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
    $hash = (Get-FileHash -Algorithm SHA256 -Path $dest).Hash.Substring(0, 16).ToLower()
    [pscustomobject]@{ Package = $p.Name; Version = $p.Version; Out = $p.Out; SHA256 = $hash }
}

Write-Host ''
Write-Host 'SHA-256 prefixes (first 16 hex chars of each file):' -ForegroundColor Cyan
$results | Format-Table -AutoSize Out, Version, SHA256

Write-Host ("Done. Compare against the previous vendoring with `git diff src/ScribanTutorial/wwwroot/lib/codemirror/`.") -ForegroundColor Green
Write-Host ("Remember to update VERSION.txt if you bumped any version.") -ForegroundColor Yellow
