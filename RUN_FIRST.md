# Run First - Beginner Setup

The complete setup guide is in `README.md`. These are the shortest Windows startup steps.

## Required Software

1. Node.js 20 or newer.
2. .NET 8 SDK or Visual Studio 2022 with the ASP.NET workload.
3. PostgreSQL 15 or newer.

## Start the Backend

1. Update `Backend/AssignmentManagement.WebAPI/appsettings.json` with your PostgreSQL username and password.
2. Ensure PostgreSQL is running.
3. Run:

```powershell
cd Backend
dotnet restore
dotnet run --project AssignmentManagement.WebAPI
```

Swagger should open at <https://localhost:7081/swagger>. The API automatically creates the schema and seeds demo data when `Database:AutoCreate` is `true`.

If automatic creation fails, run `Database/000_create_database.sql`, then `Database/001_initial_schema.sql`, and restart the API.

## Start the Frontend

Open another terminal from the repository root:

```powershell
cd FrontEnd
Copy-Item .env.example .env.local
npm install
npm run dev
```

Open <http://localhost:3000>.

## Demo Accounts

| Role | Username | Password |
| --- | --- | --- |
| Admin | `admin` | `Admin@123` |
| Teacher | `teacher` | `Teacher@123` |
| Student | `student` | `Student@123` |

Change all demo passwords before deployment.
