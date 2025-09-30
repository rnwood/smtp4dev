param(
    [string]$proj,
    [string]$filter,
    [int]$timeoutMs = 20000
)

$out = [System.IO.Path]::GetTempFileName()
$err = [System.IO.Path]::GetTempFileName()

if (Test-Path $out) { Remove-Item $out }
if (Test-Path $err) { Remove-Item $err }

$args = @('test', $proj, '--filter', $filter, '-v', 'minimal')
Write-Host "Running: dotnet $($args -join ' ')"
$p = Start-Process -FilePath 'dotnet' -ArgumentList $args -RedirectStandardOutput $out -RedirectStandardError $err -NoNewWindow -PassThru

if (-not $p.WaitForExit($timeoutMs)) {
    Write-Host "TIMEOUT after ${timeoutMs}ms - killing process (Id: $($p.Id))"
    try { $p.Kill() } catch { Write-Host "Failed to kill process: $_" }
    Write-Host '--- STDOUT (partial) ---'
    if (Test-Path $out) { Get-Content $out -Tail 200 }
    Write-Host '--- STDERR (partial) ---'
    if (Test-Path $err) { Get-Content $err -Tail 200 }
    exit 2
} else {
    Write-Host "Process exited with code $($p.ExitCode)"
    Write-Host '--- STDOUT ---'
    if (Test-Path $out) { Get-Content $out }
    Write-Host '--- STDERR ---'
    if (Test-Path $err) { Get-Content $err }
    exit $p.ExitCode
}