# Assignment & Submission Management Backend

A role-based backend for a school or college. The project follows the same high-level architecture style as the supplied DivineHRM project while removing menu-based permission handling.

## Architecture

```text
AssignmentManagement.Core
  Enums, entities, DTOs, interfaces, exceptions, business rules

AssignmentManagement.Infrastructure
  EF Core DbContext, PostgreSQL, Generic Repository, Unit of Work

AssignmentManagement.Service
  Authentication, Admin configuration, assignment and submission business logic

AssignmentManagement.WebAPI
  Controllers, JWT setup, Swagger, CORS, logging, exception middleware, DB initializer

AssignmentManagement.Tests
  Business-rule, workflow and authorization unit tests
```

Request flow:

```text
Controller -> Service -> Unit of Work -> Generic Repository -> EF Core -> PostgreSQL
```

## Main design decisions

1. **Single institution per deployed system.** The initial institution is seeded and an Admin can update its School/College configuration.
2. **One primary role per user.** Supported roles are Admin, Teacher and Student.
3. **Student class/course assignment.** Every Student must belong to an active class/course.
4. **Teacher authorization.** A Teacher can create assignments only for an active Teacher-Class-Subject mapping assigned by Admin.
5. **Tenant isolation.** Every main query is restricted to the authenticated user's institution.
6. **Submission update rule.** A Student may update before the deadline only when both the assignment and application setting permit it.
7. **Late submission.** Disabled by default and configurable with `AllowLateSubmission`.
8. **Marks rule.** Awarded marks cannot exceed the assignment's maximum marks.
9. **Refresh-token security.** Only SHA-256 refresh-token hashes are stored in PostgreSQL. Refresh tokens rotate on use.
10. **Deletion safety.** Assignments with submissions cannot be deleted; classes with active students cannot be deactivated.

## Database

The default connection string is:

```text
Host=localhost;Port=5432;Database=assignment_management_db;Username=postgres;Password=postgres
```

Edit `AssignmentManagement.WebAPI/appsettings.json` to match your PostgreSQL password.

With `Database:AutoCreate=true`, the application calls EF Core `EnsureCreatedAsync()` during startup. It creates the database/tables when the PostgreSQL account has permission. SQL fallback files are available in `Database/`.

## Seeded login

```text
Username: admin
Password: Admin@123
```

Change the default password and JWT key before deployment.

## Recommended setup flow

1. Login as Admin.
2. Update institution configuration.
3. Create class/course records.
4. Create subjects.
5. Create Teacher users.
6. Create Student users and assign classes.
7. Map Teachers to classes and subjects.
8. Login as Teacher and create/publish assignments.
9. Login as Student and submit answers.
10. Login as Teacher and review, grade and provide feedback.

## Important endpoints

### Authentication

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/change-password`
- `GET /api/auth/me`

### Admin users

- `GET /api/admin/users`
- `GET /api/admin/users/{id}`
- `POST /api/admin/users`
- `PUT /api/admin/users/{id}`
- `PATCH /api/admin/users/{id}/active?value=true|false`
- `POST /api/admin/users/{id}/reset-password`

### Institution and configuration

- `GET /api/admin/institution`
- `PUT /api/admin/institution`
- `GET/POST/PUT/DELETE /api/admin/classes`
- `GET/POST/PUT/DELETE /api/admin/subjects`
- `GET/POST/PUT/DELETE /api/teacher-assignments`
- `GET/PUT /api/admin/settings`

### Assignments

- `GET /api/assignments`
- `GET /api/assignments/{id}`
- `POST /api/assignments` — Teacher
- `PUT /api/assignments/{id}` — Teacher owner
- `POST /api/assignments/{id}/publish`
- `POST /api/assignments/{id}/draft`
- `POST /api/assignments/{id}/close`
- `DELETE /api/assignments/{id}`

### Submissions

- `GET /api/submissions`
- `GET /api/submissions/{id}`
- `POST /api/submissions/assignment/{assignmentId}` — Student
- `PUT /api/submissions/{id}/review` — Teacher owner

The GET endpoints automatically restrict data by role:

- Admin: all institution records
- Teacher: own assignments and their submissions
- Student: own-class assignments and own submissions

## Example Admin user creation

```json
{
  "fullName": "Teacher One",
  "email": "teacher1@example.com",
  "userName": "teacher1",
  "password": "Teacher@123",
  "role": "Teacher",
  "academicClassId": null
}
```

Student example:

```json
{
  "fullName": "Student One",
  "email": "student1@example.com",
  "userName": "student1",
  "password": "Student@123",
  "role": "Student",
  "academicClassId": 1
}
```

## Assignment timestamps

Send `deadlineUtc` in ISO 8601 UTC format:

```text
2026-12-31T12:00:00Z
```

## Application settings

The database initializer creates:

```text
AllowLateSubmission = false
AllowStudentSubmissionUpdate = true
```

Admin can update either through `PUT /api/admin/settings`.

## Testing

In Visual Studio:

1. Open **Test Explorer**.
2. Select **Run All Tests**.

Command line:

```powershell
dotnet test AssignmentSubmissionManagement.sln
```

Tests cover:

- assignment deadline and maximum marks
- late submission rule
- resubmission before/after deadline
- graded submission protection
- grading marks and feedback validation
- Admin/Teacher/Student access rules

## Production checklist

- Replace the JWT key with a long random secret.
- Replace the default Admin password.
- Move secrets to environment variables or User Secrets.
- Restrict `AllowedOrigins` to the deployed frontend URL.
- Set `Database:AutoCreate` to `false` after adopting EF migrations.
- Use HTTPS and a restricted PostgreSQL user.
- Do not commit production `appsettings` secrets.

See `RUN_FIRST.md` for the simplest startup steps and `REQUIREMENT_COVERAGE.md` for the full requirement mapping.
