# Install RabbitMq Server
Write-Host "install: RabbitMq (Choco)" -ForegroundColor Green
choco install -y rabbitmq

Invoke-Expression -Command:$healthCheck
