. "$PSScriptRoot\Util-RabbitMqPath.ps1"

# Install RabbitMq Server
Write-Host "install: RabbitMq (Choco)" -ForegroundColor Green
choco install -y rabbitmq

# Health Check
Write-Host "install: RabbitMq Health Check" -ForegroundColor Green

$rabbitMqPath = Get-RabbitMQPath
$rabbitmqctl = "'$rabbitMqPath\sbin\rabbitmqctl.bat'"
$healthCheck = "cmd.exe /C $rabbitmqctl node_health_check"

Invoke-Expression -Command:$healthCheck
