# KhalawanyTube (Local Family Network)

A private mini YouTube-like web app built with **.NET / C# (ASP.NET Core MVC)** for family use over a local network.

## Main features
- Record and share **audio** clips.
- Record and share **video** clips.
- Upload visibility control: **Shared** or **Private**.
- Edit/delete your own uploads and toggle share/private status.
- **Site administrator** dashboard.
- **User registration** and login.
- **Profile and personal data management** (display name, DOB, bio).
- Age gate set to **7+ years old**.
- Twitter-like UI skin for cleaner social-feed styling, rendered with React components for feed pages.

## Host on local IIS (Windows)

### 1) Install prerequisites
1. Install **.NET 8 Hosting Bundle** on the IIS server.
2. Enable IIS features:
   - Web Server
   - ASP.NET Core Module (installed with hosting bundle)

### 2) Publish the app
From project folder:
```bash
dotnet restore
dotnet publish -c Release -o .\publish
```

### 3) Create IIS site
1. Open IIS Manager.
2. Create an **Application Pool**:
   - .NET CLR version: **No Managed Code**
   - Managed pipeline mode: **Integrated**
3. Create a new Site:
   - Physical path: your `publish` folder
   - Binding: choose LAN IP and port (for example `http://192.168.1.10:5000`)
4. Assign the app pool to this site.

### 4) Folder permissions
Grant IIS app-pool identity write access to:
- `publish\wwwroot\uploads`
- `publish` root folder (for `familytube.db` SQLite file)

### 5) Firewall / LAN access
Open chosen TCP port in Windows Firewall so family devices can reach the site over LAN.

## Admin seed account
Admin is seeded from config (`appsettings.json`):
- `AdminSeed:Email`
- `AdminSeed:Password`

> Change this password before production use.

## Data storage
- SQLite file: `familytube.db`
- Upload folder: `wwwroot/uploads`


## Database migration script
If your existing `familytube.db` was created before the `IsShared` field existed, run:

```bash
sqlite3 familytube.db ".read db/migrations/2026-04-17_add_is_shared.sql"
```

Rollback script (if needed):

```bash
sqlite3 familytube.db ".read db/migrations/2026-04-17_add_is_shared_rollback.sql"
```
