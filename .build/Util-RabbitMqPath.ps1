# https://github.com/rabbitmq/chocolatey-package/blob/master/tools/chocolateyHelpers.ps1
function Get-RabbitMQPath {
  $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RabbitMQ"
  if (Test-Path "HKLM:\SOFTWARE\Wow6432Node\") {
    $regPath = "HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\RabbitMQ"
  }
  
  if (Test-Path $regPath) {
    $path = Split-Path -Parent (Get-ItemProperty $regPath "UninstallString").UninstallString
    $version = (Get-ItemProperty $regPath "DisplayVersion").DisplayVersion
    return "$path\rabbitmq_server-$version"
  }
}