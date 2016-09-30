. "$PSScriptRoot\Util-RabbitMqPath.ps1"
write-host 'install:Restarting RabbitMq' -ForegroundColor Green

$rabbitMqPath = Get-RabbitMQPath
$rabbitmqSrv = "'$rabbitMqPath\sbin\rabbitmq-service.bat'"

Write-Host "Found Comand Line Tool at $rabbitmqSrv" -ForegroundColor Green

$uninstall = "cmd.exe /C $rabbitmqSrv remove"
$install = "cmd.exe /C $rabbitmqSrv install"
$start = "cmd.exe /C $rabbitmqSrv start"

Invoke-Expression -Command:$uninstall
Start-Sleep 2
Invoke-Expression -Command:$install
Start-Sleep 2
Invoke-Expression -Command:$start
Start-Sleep 2

# Health Check
Write-Host "install: RabbitMq Health Check" -ForegroundColor Green

$rabbitmqctl = "'$rabbitMqPath\sbin\rabbitmqctl.bat'"
$healthCheck = "cmd.exe /C $rabbitmqctl node_health_check"