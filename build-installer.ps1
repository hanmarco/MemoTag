param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root "MemoTag\MemoTag.csproj"
$InstallerScript = Join-Path $Root "installer\MemoTag.iss"

dotnet publish $Project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishReadyToRun=true

$Command = Get-Command iscc -ErrorAction SilentlyContinue
$Iscc = if ($Command) { $Command.Source } else { $null }

if (-not $Iscc) {
    $Candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )

    $Iscc = $Candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $Iscc) {
    throw "Inno Setup 6 compiler was not found. Install it from https://jrsoftware.org/isinfo.php, then run this script again."
}

& $Iscc $InstallerScript
