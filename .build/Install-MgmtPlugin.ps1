Write-Host "install: verifying that RabbitMq Mgmt Tool is up"

$client = New-Object System.Net.Sockets.TcpClient([System.Net.Sockets.AddressFamily]::InterNetwork)
$attempt=0

while(!$client.Connected -and $attempt -lt 30) {
	try {
		$attempt++;
		$client.Connect("127.0.0.1", 15672);
		Write-Host "install: mgmt tool is listening on port"
		}
	catch {
		Write-Host "install: mgmt tool not is listening on port"
		Start-Sleep 1
		}
}
$client.Close()