# Verify RabbitMq Mgmt Tool is up
$client = New-Object System.Net.Sockets.TcpClient([System.Net.Sockets.AddressFamily]::InterNetwork)
$attempt=0

while(!$client.Connected -and $attempt -lt 10) {
	try {
		$attempt++;
		$client.Connect("127.0.0.1", 15672);
		write-host "install: mgmt tool is listening on port $port"
		}
	catch {
		write-host "install: mgmt tool not is listening on port $port"
		}
}
$client.Close()