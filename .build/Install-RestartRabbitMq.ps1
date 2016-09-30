. "$PSScriptRoot\Util-RabbitMqPath.ps1"
write-host 'install:Restarting RabbitMq' -ForegroundColor Green

$rabbitMqPath = Get-RabbitMQPath
$rabbitmqSrv = "'$rabbitMqPath\sbin\rabbitmq-service.bat'"

Write-Host "Found Comand Line Tool at $rabbitmqSrv" -ForegroundColor Green

$uninstall = "cmd.exe /C $rabbitmqSrv remove"
$install = "cmd.exe /C $rabbitmqSrv install"
$install = "cmd.exe /C $rabbitmqSrv start"

Invoke-Expression -Command:$uninstall
Invoke-Expression -Command:$install
Invoke-Expression -Command:$start