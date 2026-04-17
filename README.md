# MaintenanceWorkflow MVP

## Stack
- API: ASP.NET Core 8 + EF Core SQL Server
- Frontend: React + TypeScript (Vite)

## Prerequisites
- .NET 8 SDK
- Node.js 20+
- SQL Server

## API setup
1. Update connection string in `/home/runner/work/MaintenanceWorkflow/MaintenanceWorkflow/MaintenanceWorkflow.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`).
2. Run migrations:
   ```bash
   cd /home/runner/work/MaintenanceWorkflow/MaintenanceWorkflow/MaintenanceWorkflow.Api
   dotnet tool install --global dotnet-ef --version 8.0.11
   dotnet ef database update
   ```
3. Run API:
   ```bash
   dotnet run
   ```
4. Swagger:
   - `https://localhost:5001/swagger` or `http://localhost:5000/swagger`

## Frontend setup
```bash
cd /home/runner/work/MaintenanceWorkflow/MaintenanceWorkflow/maintenanceworkflow-web
npm install
npm run dev
```

Optional `.env` for API URL:
```bash
VITE_API_BASE_URL=http://localhost:5000
```

## Implemented screens
- `/companies`
- `/workflows`
- `/workflows/:id/statuses`
- `/workflows/:id/transitions`
- `/ncs`
- `/ncs/:id`

Selected runtime role is stored in `localStorage`.
