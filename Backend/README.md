# Assignment Management Backend

ASP.NET Core Web API for the role-based Assignment & Submission Management System. For the complete full-stack setup and submission documentation, begin with the repository root `README.md`.

## Technology and Architecture

- ASP.NET Core Web API on .NET 8
- Entity Framework Core with PostgreSQL
- JWT authentication and role-based authorization
- Swagger/OpenAPI
- Serilog request and file logging
- xUnit business-rule tests
- Layered Core, Infrastructure, Service, WebAPI, and Tests projects

```text
Controller -> Service -> Unit of Work -> Repository -> EF Core -> PostgreSQL
```

## Projects

```text
Backend/
|-- AssignmentManagement.Core/          Entities, DTOs, rules, interfaces
|-- AssignmentManagement.Infrastructure/ DbContext, PostgreSQL, repositories
|-- AssignmentManagement.Service/       Authentication and business services
|-- AssignmentManagement.WebAPI/        Controllers, JWT, Swagger, middleware
|-- AssignmentManagement.Tests/         xUnit business and authorization tests
`-- AssignmentSubmissionManagement.sln
```

## Requirements

- .NET 8 SDK
- PostgreSQL 15 or newer
- Visual Studio 2022 or VS Code (optional)

## Configuration

Set the local PostgreSQL connection string in `AssignmentManagement.WebAPI/appsettings.json`:

```text
Host=localhost;Port=5432;Database=assignment_management_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD
```

The default development URLs are:

- HTTPS: `https://localhost:7081`
- HTTP: `http://localhost:5081`
- Swagger: `https://localhost:7081/swagger`

The default allowed frontend origin is `http://localhost:3000`.

Submission files are stored outside the public web root. By default the API uses
`AssignmentManagement.WebAPI/App_Data/SubmissionFiles`; set `FileStorage:RootPath`
to an absolute persistent-storage path when deploying the API.

### Email and password reset

Account credentials and password-reset verification codes are sent through SMTP. Keep SMTP credentials out of `appsettings.json`; configure them with .NET User Secrets from `Backend/`:

```powershell
dotnet user-secrets set "Email:Enabled" "true" --project AssignmentManagement.WebAPI
dotnet user-secrets set "Email:Host" "smtp.gmail.com" --project AssignmentManagement.WebAPI
dotnet user-secrets set "Email:Port" "587" --project AssignmentManagement.WebAPI
dotnet user-secrets set "Email:UseSsl" "true" --project AssignmentManagement.WebAPI
dotnet user-secrets set "Email:UserName" "YOUR_SMTP_USERNAME" --project AssignmentManagement.WebAPI
dotnet user-secrets set "Email:Password" "YOUR_SMTP_PASSWORD" --project AssignmentManagement.WebAPI
dotnet user-secrets set "Email:FromEmail" "YOUR_FROM_EMAIL" --project AssignmentManagement.WebAPI
```

User creation is transactional: if the credentials email cannot be delivered, the new account is not committed. Password-reset codes contain six digits, expire after 10 minutes, are one-time use, and are stored only as keyed hashes.

## Database Setup

With `Database:AutoCreate=true`, startup calls `EnsureCreatedAsync()` and then performs idempotent data seeding. The initializer creates roles, institution settings, three demo users, `Class 10 - A`, `Mathematics (MATH-101)`, and a Teacher mapping.

For manual setup:

1. Run `../Database/000_create_database.sql` against the default `postgres` database.
2. Connect to `assignment_management_db`.
3. Run `../Database/001_initial_schema.sql`.
4. Start the API to seed data.

The provided SQL scripts replace migration files for this submission.

## Run

From `Backend/`:

```powershell
dotnet restore
dotnet run --project AssignmentManagement.WebAPI
```

To trust the local HTTPS certificate:

```powershell
dotnet dev-certs https --trust
```

## Demo Credentials

| Role | Username | Password |
| --- | --- | --- |
| Admin | `admin` | `Admin@123` |
| Teacher | `teacher` | `Ra123456@#` |
| Student | `rasel0098` | `Ra123456@#` |

Change these local demo passwords and the JWT key before any deployment.

## Main Endpoints

### Authentication

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/change-password`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/auth/me`

### Administration

- `GET/POST/PUT /api/admin/users`
- `PATCH /api/admin/users/{id}/active`
- `POST /api/admin/users/{id}/reset-password`
- `GET/POST/PUT/DELETE /api/admin/classes`
- `GET/POST/PUT/DELETE /api/admin/subjects`
- `GET/PUT /api/admin/institution`
- `GET/PUT /api/admin/settings`
- `GET/POST/PUT/DELETE /api/teacher-assignments`

### Assignments

- `GET /api/assignments`
- `GET /api/assignments/{id}`
- `POST /api/assignments`
- `PUT /api/assignments/{id}`
- `POST /api/assignments/{id}/publish`
- `POST /api/assignments/{id}/draft`
- `POST /api/assignments/{id}/close`
- `DELETE /api/assignments/{id}`

### Submissions

- `GET /api/submissions`
- `GET /api/submissions/{id}`
- `POST /api/submissions/assignment/{assignmentId}`
- `GET /api/submissions/{id}/file` (inline view)
- `GET /api/submissions/{id}/file?download=true`
- `PUT /api/submissions/{id}/review`

Swagger contains the complete request/response schema and JWT Bearer authorization control.

## Authorization Rules

- Admin can manage and view institution-wide records.
- Teacher can create assignments only for active assigned mappings and can access only owned assignments and their submissions.
- Student can view published/closed assignments for the Student's class and can access only personal submissions.
- All main queries apply institution isolation.
- JWT validation rejects inactive accounts.

## Important Business Rules

- Assignment deadline values must be future UTC timestamps.
- Maximum marks are limited to `0 < marks <= 10000`.
- Late submission is controlled by `AllowLateSubmission` and defaults to `false`.
- Student updates require both assignment resubmission and the global update setting.
- Graded submissions and submissions past the deadline cannot be changed by Students.
- Awarded marks cannot exceed maximum marks.
- Returned submissions require feedback.
- Assignments containing submissions cannot be deleted.
- Teachers can enable answer-file upload per assignment. Accepted formats are JPG, JPEG, PNG, and PDF, with a 10 MB maximum.
- Submission file endpoints enforce institution, ownership, and Teacher-assignment access before streaming a file.

## Tests

```powershell
dotnet test AssignmentSubmissionManagement.sln
```

The xUnit suite covers assignment deadlines and marks, Admin/Teacher/Student authorization, ownership isolation, late submissions, resubmission, graded submission protection, and review validation.

## Production Notes

- Move database credentials and JWT configuration to environment variables, User Secrets, or a secret manager.
- Use a strong random JWT key and replace all seeded passwords.
- Restrict CORS to the deployed frontend origin.
- Disable automatic creation and adopt versioned migrations for production.
- Use HTTPS and a restricted PostgreSQL account.
