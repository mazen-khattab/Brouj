[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$candidatePaths = @(git -C $repositoryRoot ls-files --cached --others --exclude-standard)

if ($LASTEXITCODE -ne 0) {
    throw 'Cannot enumerate repository files for the secret review.'
}

# Report only a rule name, repository-relative file and line. Never echo the matched credential.
# Test fixtures intentionally contain synthetic passwords. Literal password/config rules therefore
# target production code/configuration; strong token/private-key rules apply to every text file.
$allTextRules = @{
    'PrivateKey' = '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'
    'CloudAccessKey' = '\b(?:AKIA|ASIA)[A-Z0-9]{16}\b'
    'GitHubToken' = '\bgh[pousr]_[A-Za-z0-9]{30,}\b'
}
$productionRules = @{
    'LiteralPasswordHashInput' = '\b(?:hasher|_hasher)\.Hash\(\s*"[^"\r\n]+"'
    'ConnectionPassword' = '(?i);\s*(?:Password|Pwd)\s*=\s*[^;\s"<>]+'
    'JsonSigningSecret' = '"(?:SigningKey|ClientSecret|SecretAccessKey|AccountKey)"\s*:\s*"(?!<|REPLACE|CHANGE_ME)[^"\r\n]+"'
}
$findings = @()

foreach ($relativePath in ($candidatePaths | Sort-Object -Unique)) {
    $extension = [System.IO.Path]::GetExtension($relativePath).ToLowerInvariant()

    if ($extension -notin @('.cs', '.json', '.config', '.xml', '.txt', '.md', '.yml', '.yaml', '.env', '.pem', '.key', '.http', '.ps1')) {
        continue
    }

    $rules = $allTextRules.Clone()

    if ($relativePath.StartsWith('src/') -or $extension -in @('.json', '.config', '.env', '.pem', '.key')) {
        foreach ($rule in $productionRules.GetEnumerator()) {
            $rules[$rule.Key] = $rule.Value
        }
    }

    $absolutePath = Join-Path $repositoryRoot $relativePath

    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        continue
    }

    $lineNumber = 0

    foreach ($line in [System.IO.File]::ReadLines($absolutePath)) {
        $lineNumber++

        foreach ($rule in $rules.GetEnumerator()) {
            if ($line -match $rule.Value) {
                $findings += [pscustomobject]@{ Rule = $rule.Key; Path = $relativePath; Line = $lineNumber }
            }
        }
    }
}

if ($findings.Count -gt 0) {
    $findings | Format-Table -AutoSize
    Write-Error 'Potential committed secrets require review. Matched values were redacted.'
    exit 1
}

Write-Output 'Secret heuristics passed for production literals and repository text; values were not printed.'
Write-Output 'This check reviews current files. Git history and runtime secret stores require separate review.'
