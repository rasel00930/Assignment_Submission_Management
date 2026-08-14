# Requirement Coverage

## Admin

- Manage users: create, list, details, update, activate/deactivate, reset password, assign role and student class.
- Manage classes/courses: list, details, create, update, deactivate.
- Manage subjects: list, details, create, update, deactivate.
- Assign teachers to class/course and subject: list, create, update, deactivate.
- View all assignments: `GET /api/assignments`.
- View all submissions: `GET /api/submissions`.
- Manage institution configuration: `GET/PUT /api/admin/institution`.
- Manage application settings: `GET/PUT /api/admin/settings`.

## Teacher

- View own class-subject mappings.
- Create assignments as draft or publish immediately.
- Update assignments.
- Publish, return to draft, close, and delete assignments subject to business rules.
- View only submissions for assignments created by that teacher.
- Set UnderReview, Graded, or Returned status.
- Provide marks and feedback.

## Student

- View only Published/Closed assignments for the student's own class/course.
- View assignment details and deadline.
- Submit an answer.
- Update before deadline only when the assignment and institution settings allow it.
- Accept a late first submission only when both the institution setting and that assignment allow it.
- Create optional boolean policies from the backend catalogue and show matching Teacher assignment toggles only while each global policy is enabled.
- View only own submissions, status, marks, and feedback.

## Technical

- ASP.NET Core Web API and C# on .NET 8.
- PostgreSQL with EF Core relationships and constraints.
- Clean layered structure: Core, Infrastructure, Service, WebAPI, Tests.
- Generic Repository and Unit of Work.
- JWT access token and rotating hashed refresh token.
- Role-based authorization for Admin, Teacher, and Student.
- Validation, global error handling, Serilog, Swagger/OpenAPI, CORS.
- Unit tests for assignment rules, submission workflow, marks, and authorization rules.
- Automatic database/table creation and SQL fallback scripts.
