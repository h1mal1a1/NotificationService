Write-Host "Stopping containers..."
docker compose down

Write-Host "Removing volume..."
docker volume rm notificationservice_notification_pgdata -f

Write-Host "Starting containers..."
docker compose up --build -d

Write-Host "Done!"