. "$PSScriptRoot\Util-RabbitMqPath.ps1"

Write-Host "install: verifying that RabbitMq Mgmt Tool is up" -ForegroundColor Green

$rabbitMqPath = Get-RabbitMQPath
$rabbitmqPlugin = "'$rabbitMqPath\sbin\rabbitmq-plugins.bat'"
$enableMgmt = "cmd.exe /C $rabbitmqPlugin enable rabbitmq_management"


$client = New-Object System.Net.Sockets.TcpClient([System.Net.Sockets.AddressFamily]::InterNetwork)
$attempt=0

while(!$client.Connected -and $attempt -lt 30) {
	try {
		$attempt++;
		$client.Connect("127.0.0.1", 15672);
		Write-Host "install: mgmt tool is listening on port" -ForegroundColor Green
		}
	catch {
		Write-Host "install: mgmt tool not is listening on port" -ForegroundColor Green
		Invoke-Expression -Command:$enableMgmt
		Start-Sleep 2
		}
}
$client.Close()