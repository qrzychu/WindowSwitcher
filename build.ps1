param(
    [string]$Task = 'Package',
    [string]$Version = '1.0.0.0'
)

# Ensure psake is installed
if (!(Get-Module -ListAvailable -Name psake)) {
    Install-Module -Name psake -Scope CurrentUser -Force
}

# Import psake module
Import-Module psake

# Invoke psake with the given task and parameters
Invoke-psake -buildFile .\psakefile.ps1 -taskList $Task -parameters @{"version"=$Version}

# Return the exit code
exit ( [int]( -not $psake.build_success ) )