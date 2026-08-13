# AssignmentHub Frontend

A complete responsive frontend for the **Assignment & Submission Management System**.

## Technology

- Next.js App Router
- React
- TypeScript
- Tailwind CSS
- React Hook Form
- Zod validation
- Axios API integration
- JWT access token + refresh token handling
- Role-based route protection

## Roles and screens

### Admin

- Dashboard
- User management
- Class/course management
- Subject management
- Teacher–class–subject mapping
- Institution configuration
- Application settings
- View all assignments
- View all submissions
- Password reset and account activation/deactivation

### Teacher

- Dashboard
- Create assignment
- Edit assignment
- Draft/publish/close/delete assignment
- View submissions
- Review answer
- Give marks and feedback
- Change submission status

### Student

- Dashboard
- View published assignments
- View assignment details and deadline
- Submit answer
- Update answer before deadline when permitted
- View submission status, marks and feedback

## 1. Install Node.js

Install Node.js 20 or newer. Check it from Command Prompt:

```powershell
node --version
npm --version
```

## 2. Open the project

Extract the ZIP, then open the extracted frontend folder in Visual Studio Code.

## 3. Create environment file

Copy `.env.example` and rename the copy to `.env.local`.

```env
NEXT_PUBLIC_API_BASE_URL=https://localhost:7081
```

The backend project currently uses:

```text
https://localhost:7081
http://localhost:5081
```

Use the HTTPS address shown by Swagger or Visual Studio.

## 4. Install packages

Open the VS Code terminal inside the frontend folder:

```powershell
npm install
```

## 5. Start the backend first

Open the ASP.NET Core solution in Visual Studio and run `AssignmentManagement.WebAPI`.

Confirm Swagger opens at approximately:

```text
https://localhost:7081/swagger
```

If the browser shows a local certificate warning, open the Swagger URL and accept/trust the development certificate. You can also run:

```powershell
dotnet dev-certs https --trust
```

## 6. Start the frontend

```powershell
npm run dev
```

Open:

```text
http://localhost:3000
```

The backend already allows `http://localhost:3000` in its CORS configuration.

## Initial login

```text
Username: admin
Password: Admin@123
```

Change the password after the first login.

## Recommended first setup flow

1. Login as Admin.
2. Configure institution information.
3. Create classes/courses.
4. Create subjects.
5. Create teacher accounts.
6. Create student accounts and assign their class.
7. Map teachers to a class and subject.
8. Login as Teacher.
9. Create and publish an assignment.
10. Login as Student.
11. Submit an answer.
12. Login as Teacher and review the submission.
13. Login as Student and view marks and feedback.

## Project structure

```text
assignment-management-frontend
├── app
│   ├── login
│   └── (protected)
│       ├── admin
│       ├── teacher
│       ├── student
│       ├── dashboard
│       └── profile
├── components
│   ├── assignments
│   ├── auth
│   ├── common
│   ├── layout
│   ├── submissions
│   └── ui
├── lib
│   ├── api.ts
│   ├── auth-storage.ts
│   ├── constants.ts
│   ├── services.ts
│   ├── types.ts
│   └── utils.ts
├── .env.example
├── package.json
└── README.md
```

## Authentication design

- Access token and refresh token are stored in one local browser session object.
- Axios adds the JWT access token automatically.
- A `401 Unauthorized` response triggers refresh-token rotation.
- Failed refresh clears the session and redirects to login.
- Frontend role guards protect Admin, Teacher and Student routes.
- Backend authorization remains the final security authority.

## Form validation

React Hook Form and Zod validate:

- Login
- User creation and update
- Password reset/change
- Class/course forms
- Subject forms
- Teacher mapping
- Institution configuration
- Application settings
- Assignment creation/update
- Student answer submission
- Teacher review, marks and feedback

## Production build

```powershell
npm run build
npm run start
```

## Common issues

### `Failed to fetch`, network error or CORS error

- Ensure the backend is running.
- Check `.env.local`.
- Confirm Swagger opens.
- Restart `npm run dev` after changing `.env.local`.

### HTTPS certificate error

```powershell
dotnet dev-certs https --trust
```

Then restart Visual Studio and the browser.

### Port 3000 is busy

```powershell
npm run dev -- -p 3001
```

If using port 3001, add `http://localhost:3001` to the backend `AllowedOrigins` array.

### Login works in Swagger but not frontend

Check browser Developer Tools → Network and confirm requests are going to the correct API base URL.
