# AssignmentHub Frontend

Responsive Next.js frontend for the role-based Assignment & Submission Management System. The complete repository setup, database instructions, demo credentials, and evaluation flow are documented in the root `README.md`.

## Technology

- Next.js 15 App Router
- React 19 and TypeScript
- Tailwind CSS
- React Hook Form and Zod validation
- Axios API integration
- JWT access token and refresh-token handling
- Email verification-code password reset
- Role-based protected routes

## Role-Based Screens

### Admin

- Dashboard and institution summary
- User creation, update, activation/deactivation, and password reset
- Class/course and subject management
- Teacher-Class-Subject mapping
- Dynamic backend policy catalogue with createable suggestions and conditionally available assignment-level controls
- All assignments and submissions

### Teacher

- Dashboard
- Assignment creation and editing
- Per-assignment answer-file upload toggle
- Draft, publish, close, and delete actions
- Submission review, marks, feedback, and status changes

### Student

- Dashboard and assigned work
- Published assignment details and deadlines
- Answer submission and permitted updates
- Conditional JPG, JPEG, PNG, or PDF upload (maximum 10 MB)
- Submission status, marks, and feedback

## Requirements

- Node.js 20 or newer
- npm
- Running backend API from this repository

## Setup

From the `FrontEnd` directory:

```powershell
Copy-Item .env.example .env.local
npm install
npm run dev
```

Configure `.env.local`:

```env
NEXT_PUBLIC_API_BASE_URL=https://localhost:7081
```

Open <http://localhost:3000>. The backend CORS configuration permits this origin by default.

Start the backend before testing login or data screens. Swagger should be available at <https://localhost:7081/swagger>.

## Demo Logins

| Role | Username | Password |
| --- | --- | --- |
| Admin | `admin` | `Admin@123` |
| Teacher | `teacher` | `Ra123456@#` |
| Student | `rasel0098` | `Ra123456@#` |

The accounts are seeded by the backend on first startup.

## Project Structure

```text
FrontEnd/
|-- app/
|   |-- login/
|   `-- (protected)/
|       |-- admin/
|       |-- teacher/
|       |-- student/
|       |-- dashboard/
|       `-- profile/
|-- components/
|   |-- assignments/
|   |-- auth/
|   |-- common/
|   |-- layout/
|   |-- submissions/
|   `-- ui/
|-- lib/
|   |-- api.ts
|   |-- auth-storage.ts
|   |-- constants.ts
|   |-- services.ts
|   |-- types.ts
|   `-- utils.ts
|-- .env.example
|-- package.json
`-- README.md
```

## Authentication Design

- The access token and refresh token are stored together in browser local storage.
- Axios automatically adds the JWT access token to API requests.
- A `401 Unauthorized` response attempts one refresh-token rotation.
- A failed refresh removes the session and sends the user to login.
- Client-side guards restrict pages by Admin, Teacher, and Student role.
- The forgot-password flow sends a six-digit email code before accepting a new password.
- The backend remains responsible for final authorization and data ownership checks.

## Validation

React Hook Form and Zod validate login, user administration, passwords, class/course data, subjects, Teacher mappings, institution settings, assignments, Student answers, and Teacher reviews.

## Production Build

```powershell
npm run build
npm run start
```

## Common Issues

### Network or CORS error

- Confirm the API is running and Swagger opens.
- Check `NEXT_PUBLIC_API_BASE_URL` in `.env.local`.
- Restart `npm run dev` after editing the environment file.
- If the frontend uses another port, add that origin to backend `AllowedOrigins`.

### Local HTTPS certificate error

```powershell
dotnet dev-certs https --trust
```

Restart the backend and browser after trusting the certificate.

### Port 3000 is occupied

```powershell
npm run dev -- -p 3001
```

Also add `http://localhost:3001` to backend `AllowedOrigins`.
