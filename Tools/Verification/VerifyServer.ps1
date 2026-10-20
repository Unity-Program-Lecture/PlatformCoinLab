param([ValidateSet('Basic', 'Remote')][string]$Mode = 'Basic')
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$server = Join-Path $project 'ContentServer'
$day = if ($Mode -eq 'Basic') { 8 } else { 9 }
$evidence = Join-Path $project 'Evidence/Rebuild'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
if (Get-NetTCPConnection -LocalPort 5443 -State Listen -ErrorAction SilentlyContinue) { throw 'Port 5443 is already in use; existing service was not stopped.' }
$checks = [System.Collections.Generic.List[string]]::new()
$ownedServer = $null
$playing = $false

function Check([bool]$Condition, [string]$Name) {
    if (!$Condition) { throw "Failed: $Name" }
    $checks.Add($Name)
}

function U([string]$Command, [string[]]$Parameters = @(), [int]$Timeout = 60) {
    $arguments = @('command', $Command, '--project-path', $project, '--caller', 'plugin', '--skill', 'unity-cli', '--format', 'json', '--timeout', "$Timeout")
    if ($Parameters.Count) { $arguments += '--'; $arguments += $Parameters }
    $raw = (& unity.exe @arguments 2>&1) -join "`n"
    $result = $raw | ConvertFrom-Json
    if (!$result.success -or $result.data.result.success -eq $false) { throw $raw }
    return $result
}

function StartServer {
    $root = if ($Mode -eq 'Basic') { Join-Path $server 'Content' } else { Join-Path $project 'ServerData' }
    $arguments = @(('"' + (Join-Path $server 'bin/Debug/net10.0/ContentServer.dll') + '"'), '--FilesRoot', ('"' + $root + '"'))
    $script:ownedServer = Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory $server -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $evidence "Day$day-https-stdout.log") -RedirectStandardError (Join-Path $evidence "Day$day-https-stderr.log")
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    do {
        try { return Invoke-RestMethod 'https://localhost:5443/health' -TimeoutSec 2 }
        catch {
            if ($ownedServer.HasExited -or [DateTime]::UtcNow -gt $deadline) { throw }
            Start-Sleep -Milliseconds 200
        }
    } while ($true)
}

function StopServer {
    if ($script:ownedServer -and !$script:ownedServer.HasExited) {
        Stop-Process -Id $script:ownedServer.Id
        $script:ownedServer.WaitForExit(5000) | Out-Null
    }
    $script:ownedServer = $null
}

& dotnet build (Join-Path $server 'ContentServer.csproj') --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'ContentServer build failed' }
try {
    if ($Mode -eq 'Remote') {
        U 'run_script' @('--file', 'Tools/Teacher/CourseBuild.cs', '--entry', 'CourseBuild.Main', '--args', '[9,true,false]', '--timeout_ms', '180000') 210 | Out-Null
    }
    $health = StartServer
    Check ($health.status -eq 'ok') 'HTTPS health response with normal certificate validation'
    if ($Mode -eq 'Basic') {
        $response = Invoke-WebRequest 'https://localhost:5443/platform-config.json' -TimeoutSec 5
        Check ($response.StatusCode -eq 200) 'JSON HTTP 200'
        Check (($response.Content | ConvertFrom-Json).roundSeconds -eq 45) 'JSON duration is 45 seconds'
        Check (($response.Headers['Content-Type'] -join ';') -match 'application/json') 'JSON MIME type'
    }
    else {
        $bundle = Get-ChildItem -LiteralPath (Join-Path $project 'ServerData/StandaloneWindows64') -Filter '*.bundle' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        Check ($null -ne $bundle) 'Remote Windows bundle exists'
        $response = Invoke-WebRequest ('https://localhost:5443/StandaloneWindows64/' + $bundle.Name) -TimeoutSec 10
        Check ($response.StatusCode -eq 200) 'Bundle HTTPS HTTP 200'
        Check (($response.Headers['Content-Type'] -join ';') -match 'application/octet-stream') 'Bundle MIME type'
        Check ($response.RawContentLength -eq $bundle.Length) 'Bundle response length matches the file'
        U 'set_autotick' | Out-Null
        U 'editor_play' | Out-Null
        $playing = $true
        & unity.exe status --until-ready --project-path $project --timeout 50 --format json | Out-Null
        U 'run_script' @('--file', 'Tools/Verification/CourseFlowProbe.cs', '--entry', 'CourseFlowProbe.Main', '--args', '[9]', '--timeout_ms', '55000') 65 | Out-Null
        Check $true 'Packed Addressables HTTPS stage completes the live game flow'
        U 'editor_stop' | Out-Null
        $playing = $false
    }
    $missing = Invoke-WebRequest 'https://localhost:5443/missing-config.json' -SkipHttpErrorCheck -TimeoutSec 5
    Check ($missing.StatusCode -eq 404) 'Missing path returns HTTP 404'
    StopServer
    $connectionFailed = $false
    try { Invoke-WebRequest 'https://localhost:5443/health' -TimeoutSec 2 | Out-Null }
    catch { $connectionFailed = $true }
    Check $connectionFailed 'Stopped server is a connection failure'
    $health = StartServer
    Check ($health.status -eq 'ok') 'Server restart restores HTTPS response'
    $report = [pscustomobject]@{day=$day; mode=$Mode; passed=$true; certificateValidation='normal'; checks=@($checks); checkedUtc=[DateTime]::UtcNow.ToString('O')}
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidence ('Day{0:00}-https.json' -f $day)) -Encoding utf8
    Write-Output "DAY $day HTTPS PASS: $($checks.Count) checks"
}
finally {
    if ($playing) { U 'editor_stop' | Out-Null }
    StopServer
    if ($Mode -eq 'Remote') {
        U 'run_script' @('--file', 'Tools/Teacher/CourseBuild.cs', '--entry', 'CourseBuild.Main', '--args', '[9,false,false]', '--timeout_ms', '180000') 210 | Out-Null
    }
}
