$Zip = [IO.Path]::GetTempFileName() + ".zip"
[System.Net.ServicePointManager]::SecurityProtocol = 'Tls12'
Invoke-WebRequest -Uri 'https://github.com/PowerShell/PowerShellEditorServices/releases/download/v1.7.0/PowerShellEditorServices.zip' -OutFile $Zip 
$PsesDirectory = Join-Path $PSScriptRoot "VisualStudio.PowerShell\PowerShellEditorServices"
New-Item $PsesDirectory -ItemType Directory
Expand-Archive -Path $Zip -OutputPath $PsesDirectory
Remove-Item $Zip