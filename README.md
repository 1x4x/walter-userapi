# Walter UserAPI

ASP.NET Core MVC user CRUD API with anonymous access and a dark-mode web dashboard.

## Technology

- .NET 8 / ASP.NET Core MVC
- REST controllers with `[AllowAnonymous]`
- JSON file persistence (`Data/users.json`)
- Responsive dark mode with purple/pink accents
- No external NuGet packages required

## Run

```bash
dotnet restore
dotnet run
```

Open `http://localhost:5080` for the dashboard. The API is at `http://localhost:5080/api/users`.

## API endpoints

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/api/users` | List users |
| GET | `/api/users/{id}` | Get one user (GUID) |
| POST | `/api/users` | Create a user |
| PUT | `/api/users/{id}` | Update a user |
| DELETE | `/api/users/{id}` | Delete a user |

Create:

```bash
curl -X POST http://localhost:5080/api/users \
  -H 'Content-Type: application/json' \
  -d '{"name":"Alex Example","email":"alex@example.com","role":"User"}'
```

The API validates names and email addresses, rejects duplicate emails (`409`), returns `404` for missing records, and writes changes to `Data/users.json` with serialized, asynchronous file operations.

## Structure

- `Controllers/UsersController.cs` — REST CRUD API
- `Controllers/HomeController.cs` — dashboard page
- `Models/User.cs` — user entity and validated input model
- `Services/IUserStore.cs`, `Services/JsonUserStore.cs` — persistence
- `Views/Home/Index.cshtml` — interactive dark-mode dashboard
- `wwwroot/css/site.css` — styling

## ⚠️ Security

This sample has anonymous access to all user operations. Anyone who can reach the server can view, create, edit, and delete users. Keep it on localhost or within a trusted network, and do not store passwords or sensitive personal data. It is not suitable for internet-facing production use without authentication, authorization, rate limiting, and other hardening. JSON file persistence is intended for a single process.

## Validation

Source code has been reviewed but has not been compiled in this environment because the .NET SDK is unavailable.
