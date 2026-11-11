# Fixed: use $PSScriptRoot instead of a -RootDir argument whose trailing backslash
# made cmd.exe pass a quote character into the path ("illegal characters in path").
$ErrorActionPreference = 'Continue'
$RootDir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RootDir)) { $RootDir = (Get-Location).Path }
$log = Join-Path $RootDir 'install-result.txt'

function Write-Log([string]$text) {
    try { Add-Content -LiteralPath $log -Value $text -ErrorAction Stop } catch { Write-Host $text }
}

try { Set-Content -LiteralPath $log -Value ('START ' + (Get-Date -Format o)) -ErrorAction Stop } catch { }

try {
    $cer = Join-Path $RootDir 'dev-signing.cer'
    $msix = Join-Path $RootDir 'TranslateCmdPal.msix'
    if (-not (Test-Path -LiteralPath $msix)) { throw 'MSIX not found next to this script.' }

    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if ($isAdmin) {
        try {
            $unlock = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
            if (-not (Test-Path $unlock)) { New-Item -Path $unlock -Force | Out-Null }
            New-ItemProperty -Path $unlock -Name 'AllowAllTrustedApps' -Value 1 -PropertyType DWord -Force | Out-Null
            New-ItemProperty -Path $unlock -Name 'AllowDevelopmentWithoutDevLicense' -Value 1 -PropertyType DWord -Force | Out-Null
            Write-Log 'POLICY OK'
        } catch { Write-Log ('POLICY SKIPPED ' + $_.Exception.Message) }

        try {
            $certutil = Join-Path $env:WINDIR 'System32\certutil.exe'
            & $certutil -addstore -f Root $cer 2>&1 | Out-Null
            & $certutil -addstore -f TrustedPeople $cer 2>&1 | Out-Null
            Write-Log 'CERT OK'
        } catch { Write-Log ('CERT SKIPPED ' + $_.Exception.Message) }
    } else {
        Write-Log 'POLICY/CERT SKIPPED (not elevated; already configured on this machine)'
    }

    $pkg = Get-AppxPackage -Name 'Microsoft.CmdPal.Ext.Translate'
    if ($pkg) {
        Write-Log ('ALREADY INSTALLED ' + $pkg.PackageFullName)
    } else {
        Add-AppxPackage -Path $msix -ErrorAction Stop
        Write-Log 'INSTALL OK'
    }

    $pkg = Get-AppxPackage -Name 'Microsoft.CmdPal.Ext.Translate'
    if ($pkg) { Write-Log ('PACKAGE ' + $pkg.PackageFullName) } else { Write-Log 'PACKAGE MISSING' }
} catch {
    Write-Log ('FAILED ' + $_.Exception.Message)
}

Write-Log ('DONE ' + (Get-Date -Format o))
