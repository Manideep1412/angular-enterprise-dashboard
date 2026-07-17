# Angular Enterprise Dashboard

A full-stack enterprise admin dashboard demonstrating Angular 19 + .NET 9 production patterns.

**Tech:** Angular 19 Signals · .NET 9 Web API · JWT Auth · RBAC · SQLite · Chart.js · Tailwind CSS · Docker

---

## Demo Credentials

| Role    | Email                       | Password     |
|---------|-----------------------------|--------------|
| Admin   | admin@enterprise.dev        | Admin@123    |
| Manager | sarah.j@enterprise.dev      | Manager@123  |
| User    | bob.w@enterprise.dev        | User@123     |

---

## Quick Start — Local Development

### Prerequisites
- Node 22+, .NET 9 SDK, (optionally Docker Desktop)

### Option A — Run without Docker

**Backend:**
```bash
cd backend/EnterpriseDashboard.Api
dotnet restore
dotnet run
# API: http://localhost:5000   Swagger: http://localhost:5000/swagger
```

**Frontend:**
```bash
cd frontend
npm install
ng serve
# App: http://localhost:4200
```

### Option B — Docker Compose (full stack)

```bash
docker compose up --build
# App: http://localhost:4200
# API: http://localhost:5000/swagger
```

> SQLite database auto-seeds on first run. Data persists in a named Docker volume.

---

## Project Structure

```
angular-enterprise-dashboard/
├── backend/
│   └── EnterpriseDashboard.Api/
│       ├── Controllers/          # AuthController, UsersController, RolesController,
│       │                         # AuditLogsController, DashboardController
│       ├── Data/                 # AppDbContext, SeedData
│       ├── Entities/             # User, Role, Permission, AuditLog...
│       ├── Services/             # AuthService, UserService, RoleService, AuditService
│       ├── DTOs/                 # Request/Response models
│       └── Program.cs            # Full app bootstrap
│
├── frontend/
│   └── src/app/
│       ├── core/
│       │   ├── auth/             # AuthService (Signals), auth.guard, auth.interceptor
│       │   ├── models/           # user.models, role.models, audit.models
│       │   └── services/         # DashboardService, UserService, RoleService, AuditService
│       ├── features/
│       │   ├── auth/login/       # Login page
│       │   ├── dashboard/        # KPI cards + Chart.js charts
│       │   ├── users/            # CRUD table + modal
│       │   ├── roles/            # Role cards + permission matrix
│       │   └── audit-logs/       # Filterable log viewer
│       └── layout/               # Shell, Sidebar, Header
│
├── docker-compose.yml
└── README.md
```

---

## Architecture Highlights

| Layer | Pattern |
|-------|---------|
| Frontend state | Angular 19 Signals (`signal`, `computed`, `effect`) |
| Auth | Self-contained JWT — API issues & validates tokens |
| Authorization | Claims-based RBAC; `AdminOnly` / `ManagerOrAdmin` policies |
| API style | RESTful, versioned (`/api/v1/`) |
| ORM | Entity Framework Core 9 + SQLite (zero-migration demo) |
| Passwords | BCrypt hashing (BCrypt.Net-Next) |
| Logging | Serilog structured logging + request logging middleware |
| DI | Angular functional `inject()`, .NET constructor DI |
| Routing | Angular lazy-loaded standalone components; functional guards |

---

## API Endpoints

```
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout

GET    /api/v1/users?page=1&pageSize=10&search=&status=
POST   /api/v1/users                    [ManagerOrAdmin]
PUT    /api/v1/users/{id}               [ManagerOrAdmin]
DELETE /api/v1/users/{id}               [AdminOnly]

GET    /api/v1/roles
GET    /api/v1/dashboard/stats

GET    /api/v1/audit-logs?page=1&pageSize=20&action=&severity=
```

Swagger UI available at `/swagger` when running locally.

---

## Deployment (Vercel / Render / Fly.io)

**Backend — Render (free tier):**
1. Connect GitHub → "New Web Service" → Docker runtime
2. Root directory: `backend`
3. Add environment variables from `docker-compose.yml`

**Frontend — Vercel:**
1. Connect GitHub → import project → root: `frontend`
2. Build command: `npm run build -- --configuration=production`
3. Output: `dist/frontend/browser`
4. Set `NEXT_PUBLIC_API_URL` env var to your Render backend URL

---

## Environment Variables

| Variable | Default (dev) | Description |
|----------|--------------|-------------|
| `ConnectionStrings__DefaultConnection` | `Data Source=enterprise.db` | SQLite path |
| `Jwt__Key` | *(set in appsettings)* | Signing key — 32+ chars |
| `Jwt__Issuer` | `EnterpriseDashboard` | |
| `Jwt__Audience` | `EnterpriseDashboardUI` | |
| `Jwt__ExpiryMinutes` | `60` | Token TTL |

---

Built by Manideep Salla — [Portfolio](https://manideep.dev)
