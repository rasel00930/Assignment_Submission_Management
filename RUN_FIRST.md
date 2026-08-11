# Run First — Beginner Setup

## Required software

1. Visual Studio 2022 with **ASP.NET and web development** workload.
2. .NET 8 SDK.
3. PostgreSQL 15 or newer.
4. pgAdmin 4 (installed with PostgreSQL).

## First run

1. Extract the ZIP.
2. Open `AssignmentSubmissionManagement.sln` in Visual Studio.
3. Right-click `AssignmentManagement.WebAPI` and select **Set as Startup Project**.
4. Open `AssignmentManagement.WebAPI/appsettings.json`.
5. Change the PostgreSQL username/password in `DefaultConnection` if your local values are different.
6. Start PostgreSQL from Windows Services if it is not running.
7. Press **F5**.

`Database:AutoCreate` is `true`, so the API attempts to create:

- database: `assignment_management_db`
- all required tables
- Admin, Teacher, Student roles
- default institution
- initial Admin user
- default application settings

## Default Admin

- Username: `admin`
- Password: `Admin@123`

Change the password after the first login.

## If automatic database creation fails

1. Open pgAdmin.
2. Connect to the `postgres` database.
3. Run `Database/000_create_database.sql`.
4. Connect to `assignment_management_db`.
5. Run `Database/001_initial_schema.sql`.
6. Run the API again. The API will seed roles, settings, and the Admin user.

## First Swagger test

1. Run the API and open `/swagger`.
2. Call `POST /api/auth/login`.
3. Copy `data.accessToken`.
4. Click **Authorize** and paste only the token.
5. Test Admin endpoints.
