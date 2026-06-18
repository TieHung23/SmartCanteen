param(
    [string]$Source = "API-Document.md",
    [string]$OutputDirectory = "API-Modules"
)

$ErrorActionPreference = "Stop"

$sourcePath = Join-Path $PSScriptRoot "..\$Source"
$outputPath = Join-Path $PSScriptRoot "..\$OutputDirectory"
$content = Get-Content -LiteralPath $sourcePath -Raw -Encoding utf8

$firstModuleIndex = $content.IndexOf("## Auth", [StringComparison]::Ordinal)
if ($firstModuleIndex -lt 0) {
    throw "Could not find the first API module heading in $Source."
}

$common = $content.Substring(0, $firstModuleIndex).TrimEnd()

$modules = [ordered]@{
    "user-api.md" = @("Auth")
    "category-api.md" = @("Categories")
    "dish-api.md" = @("Dishes")
    "session-api.md" = @("Sessions")
    "order-api.md" = @("Orders")
    "payment-api.md" = @("Payments", "WalletTransaction")
    "setting-api.md" = @("Settings")
    "verification-api.md" = @("Verification", "Admin*Verification")
}

$headingMatches = [regex]::Matches(
    $content,
    "(?m)^## (?<heading>[^\r\n]+)\r?$")

$sections = @{}
for ($index = 0; $index -lt $headingMatches.Count; $index++) {
    $match = $headingMatches[$index]
    $start = $match.Index
    $end = if ($index + 1 -lt $headingMatches.Count) {
        $headingMatches[$index + 1].Index
    }
    else {
        $content.Length
    }

    $sections[$match.Groups["heading"].Value] =
        $content.Substring($start, $end - $start).Trim()
}

New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

foreach ($module in $modules.GetEnumerator()) {
    $moduleSections = foreach ($heading in $module.Value) {
        $resolvedHeading = if ($sections.ContainsKey($heading)) {
            $heading
        }
        else {
            $sections.Keys |
                Where-Object { $_ -like $heading } |
                Select-Object -First 1
        }

        if ([string]::IsNullOrWhiteSpace($resolvedHeading)) {
            throw "Could not find section '$heading' in $Source."
        }

        $sections[$resolvedHeading]
    }

    $title = [IO.Path]::GetFileNameWithoutExtension($module.Key)
    $title = ($title -replace "-api$", "") -replace "-", " "
    $title = (Get-Culture).TextInfo.ToTitleCase($title)

    $document = @(
        "# SmartCanteen $title API"
        ""
        '> Generated from `API-Document.md`. Run `.\tools\generate-api-modules.ps1` after updating the main document.'
        ""
        $common
        ""
        ($moduleSections -join "`r`n`r`n---`r`n`r`n")
        ""
    ) -join "`r`n"

    Set-Content -LiteralPath (Join-Path $outputPath $module.Key) `
        -Value $document `
        -Encoding utf8
}

$indexLines = @(
    "# SmartCanteen API Modules"
    ""
    'The module files are generated from [`API-Document.md`](../API-Document.md).'
    ""
    "| Module | Documentation |"
    "|---|---|"
    '| User / Auth | [`user-api.md`](user-api.md) |'
    '| Category | [`category-api.md`](category-api.md) |'
    '| Dish | [`dish-api.md`](dish-api.md) |'
    '| Session | [`session-api.md`](session-api.md) |'
    '| Order | [`order-api.md`](order-api.md) |'
    '| Payment / Wallet | [`payment-api.md`](payment-api.md) |'
    '| Setting | [`setting-api.md`](setting-api.md) |'
    '| Verification | [`verification-api.md`](verification-api.md) |'
    ""
    "## Regenerate"
    ""
    '```powershell'
    '.\tools\generate-api-modules.ps1'
    '```'
)

Set-Content -LiteralPath (Join-Path $outputPath "README.md") `
    -Value ($indexLines -join "`r`n") `
    -Encoding utf8
