# Structura Docker Setup Analysis
## Docker Configuration Location
- **File**: D:\Code\structura\infrastructure\docker\docker-compose.yml
## Database Setup (SQL Server)
### Container Details
- **Image**: mcr.microsoft.com/mssql/server:2022-latest
- **Container Name**: structura-sql
- **Edition**: Developer
### Connection Information
- **Host**: localhost (or 127.0.0.1)
- **Port**: 1433
- **Username**: sa
- **Password**: YourStrong@Passw0rd
### Connection String Format
``
Server=localhost,1433;Database=YourDatabaseName;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
``
### Volume
- **Data Volume**: sqlserver-data mapped to /var/opt/mssql
- **Driver**: local
### Health Check
- Command: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q 'SELECT 1'
- Interval: 10s
- Timeout: 5s
- Retries: 5
## Azurite (Azure Storage Emulator)
### Container Details
- **Image**: mcr.microsoft.com/azure-storage/azurite:latest
- **Container Name**: structura-azurite
### Ports
- **Blob Service**: 10000
- **Queue Service**: 10001
- **Table Service**: 10002
### Connection Strings
``
# Blob Service
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
# Queue Service
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;
# Table Service
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;
``
### Volume
- **Data Volume**: zurite-data mapped to /data
- **Driver**: local
## Starting the Stack
`powershell
cd D:\Code\structura\infrastructure\docker
docker-compose up -d
`
## Stopping the Stack
`powershell
cd D:\Code\structura\infrastructure\docker
docker-compose down
`
## Checking Status
`powershell
docker ps | findstr structura
`
## For RajFinancial Integration
You can use the same docker-compose setup! Just copy it to your rajfinancial project or reference the same containers since they're named uniquely (structura-sql, structura-azurite).
**Recommended**: Create your own docker-compose.yml with different container names for RajFinancial:
- Container: ajfinancial-sql 
- Port: Keep 1433 (or use 1434 if running both simultaneously)
- Password: Change to something different for security
