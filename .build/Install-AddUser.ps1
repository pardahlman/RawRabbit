. "$PSScriptRoot\Util-RabbitMqPath.ps1"
write-host 'install:Add RabbitMq User "RawRabbit"'

$rabbitMqPath = Get-RabbitMQPath
$rabbitmqctl = '$rabbitMqPath\sbin\rabbitmqctl.bat'

$healthCheck = "cmd.exe /C $rabbitmqctl node_health_check"
$createUser = "cmd.exe /C $rabbitmqctl add_user RawRabbit RawRabbit"
$setPermission = "cmd.exe /C $rabbitmqctl set_permissions -p / RawRabbit `".*`" `".*`" `".*`""

Invoke-Expression -Command:$healthCheck
Invoke-Expression -Command:$createUser
Invoke-Expression -Command:$setPermission