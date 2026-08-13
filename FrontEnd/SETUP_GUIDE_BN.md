# Frontend চালানোর সহজ বাংলা গাইড

## যেগুলো install থাকতে হবে

```text
Node.js 20+
Visual Studio Code
Backend project running
PostgreSQL running
```

## ধাপ ১: ZIP extract

Frontend ZIP extract করে folder-টি VS Code-এ open করবে।

## ধাপ ২: `.env.local` বানাবে

`.env.example` file copy করে নতুন file-এর নাম দেবে:

```text
.env.local
```

ভেতরে থাকবে:

```env
NEXT_PUBLIC_API_BASE_URL=https://localhost:7081
```

Visual Studio-তে backend run করার সময় অন্য port দেখালে ওই port বসাবে।

## ধাপ ৩: package install

VS Code → Terminal → New Terminal খুলে চালাবে:

```powershell
npm install
```

## ধাপ ৪: backend run

Visual Studio-তে backend solution open করে:

```text
AssignmentManagement.WebAPI
→ Set as Startup Project
→ F5
```

Swagger open হতে হবে।

## ধাপ ৫: frontend run

VS Code terminal-এ:

```powershell
npm run dev
```

Browser-এ:

```text
http://localhost:3000
```

## প্রথম login

```text
Username: admin
Password: Admin@123
```

## Admin হিসেবে আগে যা করবে

```text
Institution configure
→ Class/Course create
→ Subject create
→ Teacher user create
→ Student user create + class assign
→ Teacher mapping create
```

## এরপর Teacher

```text
Teacher login
→ Assignment create
→ Publish
→ Student submissions দেখবে
→ Marks + Feedback দেবে
```

## এরপর Student

```text
Student login
→ Assignment দেখবে
→ Answer submit করবে
→ Marks + Feedback দেখবে
```

## Error হলে

Backend Swagger URL browser-এ open হয় কি না আগে দেখবে। তারপর `.env.local`-এর port মিলাবে। HTTPS warning এলে:

```powershell
dotnet dev-certs https --trust
```

তারপর backend এবং frontend restart করবে।
