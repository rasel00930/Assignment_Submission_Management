# Assignment & Submission Management System

A full-stack, role-based assignment platform for schools and colleges. Admins configure the institution and users, Teachers publish and grade assignments, and Students submit work and review feedback. This repository contains the frontend, backend API, database scripts, and unit tests required to run and evaluate the complete system.

## Repository

GitHub: <https://github.com/rasel00930/Assignment_Submission_Management>

## Main Features

### Admin

- Manage Admin, Teacher, and Student accounts.
- Activate/deactivate accounts and reset passwords.
- Manage classes/courses and subjects.
- Assign Teachers to class-subject combinations.
- Configure institution information and application settings.
- View every assignment and submission in the institution.

### Teacher

- Create, update, publish, return to draft, close, and delete assignments.
- Target an assignment to an assigned class/course and subject.
- Set title, description, UTC deadline, maximum marks, and resubmission policy.
- View submissions only for assignments owned by the Teacher.
- mark submissions as Under Review, Graded, or Returned.
- Award marks and provide written feedback.

### Student

- View published or closed assignments for the Student's class/course.
- View assignment details, deadline, status, and maximum marks.
- Submit an answer and update it before the deadline when permitted.
- View personal submission status, awarded marks, and Teacher feedback.

## Technology Stack

| Area | Technology |
| --- | --- |
| Frontend | Next.js 15 App Router, React 19, TypeScript, Tailwind CSS |
| Forms | React Hook Form, Zod |
| API client | Axios |
| Backend | ASP.NET Core Web API, C#, .NET 8 |
| Data access | Entity Framework Core, Repository and Unit of Work |
| Database | PostgreSQL |
| Authentication | JWT access token and rotating refresh token |
| API documentation | Swagger/OpenAPI |
| Logging | Serilog |
| Testing | xUnit, Microsoft.NET.Test.Sdk, Coverlet collector |

## Project Structure

```text
Assignment_Submission_Management/
|-- FrontEnd/                         Next.js web application
|   |-- app/                          App Router pages and layouts
|   |-- components/                   Feature and reusable UI components
|   |-- lib/                          API, auth, services, types, utilities
|   |-- .env.example                  Frontend environment template
|   `-- README.md                     Frontend-specific documentation
|-- Backend/
|   |-- AssignmentManagement.Core/    Entities, DTOs, rules, interfaces
|   |-- AssignmentManagement.Infrastructure/ EF Core and repositories
|   |-- AssignmentManagement.Service/ Business services
|   |-- AssignmentManagement.WebAPI/  Controllers, middleware, startup
|   |-- AssignmentManagement.Tests/   Business and authorization tests
|   |-- AssignmentSubmissionManagement.sln
|   `-- README.md                     Backend-specific documentation
|-- Database/
|   |-- 000_create_database.sql       PostgreSQL database creation
|   `-- 001_initial_schema.sql        Complete relational schema
`-- README.md                         Complete project guide
```

## Prerequisites

- Git
- Node.js 20 or newer and npm
- .NET 8 SDK
- PostgreSQL 15 or newer
- Visual Studio 2022 or VS Code (optional)

Check the required runtimes:

```powershell
node --version
npm --version
dotnet --version
psql --version
```

## Local Setup

### 1. Clone the repository

```powershell
git clone https://github.com/rasel00930/Assignment_Submission_Management.git
cd Assignment_Submission_Management
```

### 2. Configure PostgreSQL

Open `Backend/AssignmentManagement.WebAPI/appsettings.json` and set `ConnectionStrings:DefaultConnection` for your local PostgreSQL server:

```text
Host=localhost;Port=5432;Database=assignment_management_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD
```

The default development configuration uses `Database:AutoCreate=true`. On first API startup, EF Core creates missing tables and the initializer seeds roles, institution data, application settings, demo users, a demo class, a subject, and a Teacher mapping.

If automatic creation is unavailable, use pgAdmin or `psql`:

1. Run `Database/000_create_database.sql` while connected to the default `postgres` database.
2. Connect to `assignment_management_db`.
3. Run `Database/001_initial_schema.sql`.
4. Start the API to seed the initial data.

The SQL scripts are the reproducible database setup artifacts for this submission. The project currently uses `EnsureCreatedAsync` instead of EF migration files.

### 3. Run the backend

```powershell
cd Backend
dotnet restore
dotnet run --project AssignmentManagement.WebAPI
```

Development URLs:

- Swagger: <https://localhost:7081/swagger>
- API HTTP: <http://localhost:5081>
- Health check: <https://localhost:7081/health>

For a local HTTPS certificate warning:

```powershell
dotnet dev-certs https --trust
```

Alternatively, open `Backend/AssignmentSubmissionManagement.sln` in Visual Studio, set `AssignmentManagement.WebAPI` as the startup project, and run the HTTPS profile.

### 4. Configure and run the frontend

Open a second terminal from the repository root:

```powershell
cd FrontEnd
Copy-Item .env.example .env.local
npm install
npm run dev
```

The `.env.local` file should contain:

```env
NEXT_PUBLIC_API_BASE_URL=https://localhost:7081
```

Open <http://localhost:3000>. Restart the Next.js server after changing `.env.local`.

## Demo Credentials

The API seeds these accounts on first startup:

| Role | Username | Email | Password |
| --- | --- | --- | --- |
| Admin | `admin` | `admin@demo.local` | `Admin@123` |
| Teacher | `teacher` | `raselahmed00950@gmail.com` | `Ra123456@#` |
| Student | `rasel0098` | `raselahmed00930@gmail.com` | `Ra123456@#` |

The demo Teacher is mapped to `Class 10 - A` and `Mathematics (MATH-101)`. The demo Student belongs to that class. These credentials are development data only; change all passwords before deployment.

## Recommended Evaluation Flow

1. Start PostgreSQL, the backend, and the frontend.
2. Log in as Admin and inspect users, class, subject, mapping, institution, and settings.
3. Log in as Teacher and create and publish an assignment.
4. Log in as Student and submit an answer.
5. Log in as Teacher and assign marks, feedback, and a status.
6. Log in as Student and view the result.

## Authentication and Authorization

- Login returns a JWT access token and refresh token.
- The frontend stores both tokens in one browser session object and Axios attaches the access token.
- A `401 Unauthorized` response triggers refresh-token rotation; failed refresh clears the session and redirects to login.
- Only SHA-256 refresh-token hashes are stored in PostgreSQL.
- Frontend route guards improve navigation and user experience.
- Backend role policies and ownership checks are the final security authority.
- Institution filtering is applied to the main queries.
- Inactive users are rejected during JWT validation.

## Database Design

The relational model includes `Institutions`, `Users`, `Roles`, `UserRoles`, `AcademicClasses`, `Subjects`, `TeacherClassSubjects`, `Assignments`, `Submissions`, `RefreshTokens`, and `ApplicationSettings`.

Important constraints include unique usernames/emails, unique class-subject Teacher mappings, and one submission per Student per assignment. Foreign keys enforce institution, class, subject, assignment, Student, and reviewing Teacher relationships.

## Business Rules

- Teachers can create assignments only for their active mappings.
- Students can see assignments only for their class/course.
- Assignment deadlines must be future UTC values.
- Maximum marks must be greater than zero and no more than 10,000.
- Late submissions are disabled by default.
- Submission updates require both assignment and application settings to allow them.
- Updates after the deadline and updates to graded submissions are rejected.
- Awarded marks cannot exceed the assignment maximum.
- Returned submissions require feedback.
- Assignments with submissions cannot be deleted.

## Tests

Run the backend test suite from `Backend/`:

```powershell
dotnet test AssignmentSubmissionManagement.sln
```

Tests cover assignment deadlines and marks, role/ownership authorization, late submission behavior, resubmission rules, graded submission protection, and review validation.

Validate a frontend production build from `FrontEnd/`:

```powershell
npm install
npm run build
```

## Environment and Security

- Frontend configuration template: `FrontEnd/.env.example`
- Backend local configuration: `Backend/AssignmentManagement.WebAPI/appsettings.json`
- Never commit production database passwords, JWT keys, or other secrets.
- For production, supply configuration through environment variables, User Secrets, or a secret manager.
- Replace the development JWT key and every seeded password.
- Restrict `AllowedOrigins` to the deployed frontend URL.
- Disable `Database:AutoCreate` after adopting a production migration process.

## Design Decisions and Assumptions

1. A deployed instance represents one institution, while every main record still carries an institution ID for isolation.
2. Each user has one operational role in the UI: Admin, Teacher, or Student.
3. Every Student belongs to one active class/course.
4. Teacher authorization is based on an Admin-created Teacher-Class-Subject mapping.
5. Deadlines are stored and exchanged in UTC.
6. Answers are text submissions; file upload and attachment storage are outside the current scope.
7. Application settings control late submission and Student update behavior.
8. Pagination is supported by list APIs and screens where applicable.

## Known Limitations

- No email delivery or self-service password recovery workflow is included; Admin resets passwords.
- No file attachments, notifications, or real-time updates are included.
- No Docker configuration or hosted live URL is provided.
- Database versioning currently uses automatic creation plus SQL scripts, not EF Core migrations.
- Demo data and configuration are intended only for local evaluation.

See `FrontEnd/README.md` and `Backend/README.md` for component-specific details.
