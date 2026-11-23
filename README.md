# 🌙 Walter UserAPI

**A dark-mode ASP.NET Core MVC dashboard and anonymous REST API for managing users.**

![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-7B4FCD) ![Storage](https://img.shields.io/badge/Storage-JSON-35C69A) ![Auth](https://img.shields.io/badge/Access-Anonymous-ED9C49) ![License](https://img.shields.io/badge/Status-Development-7D69D9)

## ✨ Overview

Walter UserAPI provides a browser-based **user management dashboard** and fifteen JSON REST endpoints. It uses a local JSON file rather than a database server. The interface has a dark background, purple-to-pink accents, user counters, editable records, and an add/edit dialog.

### 🖼️ UI previews

> **Important:** These are illustrative previews based on the dashboard design, with fictional example users. They are **not screenshots of a running .NET application**.

**Dashboard**

![Illustrative dark-mode user dashboard](docs/images/dashboard-preview.svg)

**Add user dialog**

![Illustrative add-user dialog](docs/images/add-user-preview.svg)

## 🚀 Quick start

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
git clone https://github.com/1x4x/walter-userapi.git
cd walter-userapi
dotnet restore
dotnet run
```

Open **http://localhost:5080** for the UI, or **http://localhost:5080/api/users** for JSON. The launch profile specifies port 5080; use the URL printed by `dotnet run` if your environment overrides it.

## 🧩 Features

| | Feature | Details |
|---|---|---|
| 👥 | User CRUD | Create, list, view, edit, and delete users |
| 🛡️ | Roles CRUD | Manage roles and assign permissions |\n| 🔑 | Permissions CRUD | Manage named permissions and descriptions |\n| 🌙 | Dark-mode UI | Responsive dashboard with purple/pink highlights |
| 🔓 | Anonymous endpoints | No account or token required |
| 💾 | JSON persistence | Saves to `Data/users.json` |
| 🧵 | Async storage | Serializes file access within one app process |
| ✅ | Validation | Required name/email, email format, length limits |
| 🛡️ | Duplicate protection | Rejects repeated email addresses (case-insensitive) |
| 📦 | Minimal dependencies | Uses ASP.NET Core built-in framework |

## 🔌 API reference

| HTTP | Route | Result |
|---|---|---|
| `GET` | `/api/users` | `200` list |
| `GET` | `/api/users/{id}` | `200` user / `404` |
| `POST` | `/api/users` | `201` created / `400` validation / `409` duplicate |
| `PUT` | `/api/users/{id}` | `204` updated / `400` / `404` / `409` |
| `DELETE` | `/api/users/{id}` | `204` deleted / `404` |

IDs are GUIDs. `POST` and `PUT` accept:

```json
{
  "name": "Alex Example",
  "email": "alex@example.com",
  "role": "User"
}
```

### 🧪 Try the API

**List users**

```bash
curl http://localhost:5080/api/users
```

**Create user**

```bash
curl -X POST http://localhost:5080/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Alex Example","email":"alex@example.com","role":"User"}'
```

**Update user** (replace `YOUR_GUID` with the returned ID)

```bash
curl -X PUT http://localhost:5080/api/users/YOUR_GUID \
  -H "Content-Type: application/json" \
  -d '{"name":"Alex Updated","email":"alex@example.com","role":"Admin"}'
```

**Delete user**

```bash
curl -X DELETE http://localhost:5080/api/users/YOUR_GUID
```


## 🛡️ Roles & 🔑 Permissions CRUD

The dashboard now has three tabs: **Users**, **Roles**, and **Permissions**, each with create, list, edit, and delete actions.

- **Roles** have a name, description, and a set of permission IDs. Initial roles: `User` and `Admin`.
- **Permissions** have a unique dotted key (for example, `users.read`, `users.create`, `roles.manage`) and description.
- Users select an existing role. Creating or updating a user with an unknown role returns `400`.
- Duplicate role names or permission keys return `409`.
- A role assigned to a user cannot be deleted or renamed (`409`). Reassign its users first.
- Deleting a permission removes its ID from roles. The files are updated sequentially, not in a multi-file transaction.
- Roles persist in `Data/roles.json`; permissions persist in `Data/permissions.json`.

| Method | Roles | Permissions |
|---|---|---|
| GET | `/api/roles` | `/api/permissions` |
| GET by ID | `/api/roles/{id}` | `/api/permissions/{id}` |
| POST | `/api/roles` | `/api/permissions` |
| PUT | `/api/roles/{id}` | `/api/permissions/{id}` |
| DELETE | `/api/roles/{id}` | `/api/permissions/{id}` |

**Create a permission**

```bash
curl -X POST http://localhost:5080/api/permissions \
  -H "Content-Type: application/json" \
  -d '{"key":"users.read","description":"Read user records"}'
```

**Create a role** (use an existing permission GUID from the response above)

```json
{
  "name": "Editor",
  "description": "Can view user records",
  "permissionIds": ["REPLACE-WITH-REAL-PERMISSION-GUID"]
}
```

**Important:** These permissions are **metadata, not enforced access control**. The API still uses anonymous access. Assigning an `Admin` role does not grant or restrict anything; no role checks or permission-based authorization middleware is implemented. Do not expose this service to untrusted clients.

## 🗂️ Project structure

```text
walter-userapi/
├── Controllers/\n│   ├── RolesController.cs       # Roles CRUD\n│   ├── PermissionsController.cs # Permissions CRUD
│   ├── HomeController.cs       # MVC dashboard
│   └── UsersController.cs      # Anonymous REST endpoints
├── Models/\n│   ├── AccessModels.cs         # Roles and permissions
│   └── User.cs                 # User and validated input
├── Services/\n│   ├── IAccessStore.cs         # Access metadata contract\n│   ├── JsonAccessStore.cs      # Roles/permissions JSON storage
│   ├── IUserStore.cs           # Persistence contract
│   └── JsonUserStore.cs        # JSON file storage
├── Views/Home/
│   └── Index.cshtml            # Dashboard and CRUD interactions
├── wwwroot/css/
│   └── site.css                # Dark-mode styles
├── Data/users.json             # Persisted user records\n├── Data/roles.json             # Role definitions\n├── Data/permissions.json       # Permission definitions
├── docs/images/                # Illustrated UI previews
├── Properties/launchSettings.json
├── Program.cs
└── AnonymousUserMvcApi.csproj
```

## 🏗️ How it works

```mermaid
flowchart LR
    A["🌙 MVC Dashboard"] --> B["🔌 UsersController"]
    C["🌐 REST Client"] --> B
    B --> D["🧩 IUserStore"]
    D --> E["💾 JsonUserStore"]
    E --> F[("📄 Data/users.json")]
```

The MVC dashboard calls the same REST endpoints as external clients. `JsonUserStore` uses a semaphore to serialize reads and writes **within one process** and replaces the JSON file via a temporary file.

## ⚠️ Security and limitations

**All API endpoints allow anonymous CRUD access.** Anyone who can connect can read, create, modify, or delete records. **Run only on localhost or an explicitly trusted development network.** Do not store passwords, secrets, or sensitive user information.

For internet-facing use, add authentication, authorization, rate limiting, HTTPS enforcement, and suitable data protection. The JSON store is **not** designed for multiple server instances or concurrent processes.

## 🔧 Development status

The code has been added to the repository, but a .NET build, automated tests, and live browser screenshots **have not been verified** in this environment. The images above are design previews, not evidence of a successful runtime test.

---
Made with 💜 using C#, ASP.NET Core MVC, and JSON.
