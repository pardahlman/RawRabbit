. "$PSScriptRoot\Util-RabbitMqPath.ps1"
write-host 'install:Add RabbitMq User "RawRabbit"' -ForegroundColor Green

$rabbitMqPath = Get-RabbitMQPath
$rabbitmqctl = "'$rabbitMqPath\sbin\rabbitmqctl.bat'"

Write-Host "Found Comand Line Tool at $rabbitmqctl" -ForegroundColor Green

$createUser = "cmd.exe /C $rabbitmqctl add_user RawRabbit RawRabbit"
$setPermission = "cmd.exe /C $rabbitmqctl set_permissions -p / RawRabbit `".*`" `".*`" `".*`""

Invoke-Expression -Command:$createUser
Invoke-Expression -Command:$setPermission