param(
    [switch]$SkipRuntimeTests
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$referenceAssemblies = Join-Path ${env:ProgramFiles(x86)} "Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
$project = Join-Path $root "Switch\Switch.csproj"
$testProject = Join-Path $root "Switch.Tests\Switch.Tests.csproj"
$testOutput = Join-Path $root "Switch.Tests\bin\Release\Switch.Tests.exe"
$output = Join-Path $root "Switch\bin\Release\Switch.exe"
$installerScript = Join-Path $root "setup.iss"

if (-not (Test-Path -LiteralPath $referenceAssemblies)) {
    throw "The .NET Framework 4.8 Developer Pack is required: $referenceAssemblies"
}

$candidates = @(
    (Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe"),
    (Join-Path ${env:ProgramFiles} "Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"),
    (Get-Command msbuild -ErrorAction SilentlyContinue).Source
)
$msbuild = $candidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1

if (-not $msbuild) {
    throw "MSBuild was not found. Install Visual Studio Build Tools and the .NET Framework 4.8 Developer Pack."
}

& $msbuild $project /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Test-Path -LiteralPath $output)) {
    throw "Expected application output was not produced: $output"
}

if (-not $SkipRuntimeTests) {
    & $msbuild $testProject /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not (Test-Path -LiteralPath $testOutput)) {
        throw "Expected test output was not produced: $testOutput"
    }

    & $testOutput
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (Get-Command iscc -ErrorAction SilentlyContinue) {
    & iscc $installerScript
}
else {
    Write-Warning "Inno Setup compiler (iscc.exe) was not found; application build and tests completed without packaging."
}
