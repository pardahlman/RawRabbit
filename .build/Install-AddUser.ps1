. "$PSScriptRoot\Util-RabbitMqPath.ps1"
write-host 'install:Add RabbitMq User "RawRabbit"'

$rabbitMqPath = Get-RabbitMQPath
$rabbitmqctl = '$rabbitMqPath\sbin\rabbitmqctl.bat'

$createUser = "cmd.exe /C $rabbitmqctl add_user RawRabbit RawRabbit"
$setPermission = "cmd.exe /C $rabbitmqctl set_permissions -p / RawRabbit `".*`" `".*`" `".*`""

Invoke-Expression -Command:$createUser
Invoke-Expression -Command:$setPermission