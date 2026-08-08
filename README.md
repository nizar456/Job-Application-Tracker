# Job Application Tracker

## Purpose

A private, multi-user web application for keeping job applications organised. It replaces scattered notes and spreadsheets with one place to record where a user applied, the role, the CV version used, and the current outcome.

## MVP goals

- Users can register, sign in, and access only their own data.
- Users can create, view, edit, archive, and delete job applications.
- Each application records:
  - company name and website;
  - role title, location, and work mode (remote, hybrid, or on-site);
  - job URL, source, and job description;
  - date applied;
  - CV **version/name** used (no CV-file uploads);
  - application status, contact details, and notes.
- Users can track status changes: Saved, Applied, Recruiter Contacted, Interviewing, Offer, Rejected, Withdrawn, and Archived.
- Users can filter and search their applications by company, role, status, date, location, and source.
- The dashboard shows useful personal totals, recent applications, and status breakdowns.

## Explicitly out of scope for the MVP

- Reminder or email notification features.
- Exporting to CSV, PDF, or other formats.
- CV or cover-letter file storage/upload.
- Hosting, deployment, and cloud-provider configuration.

## Technical direction

- `Backend`: ASP.NET Core Web API.
- `Frontend`: Blazor web application.
- Database: PostgreSQL with Entity Framework Core migrations.
- Authentication: ASP.NET Core Identity with secure password hashing.

Keep API endpoints user-scoped: an authenticated user must never read or change another user's applications, companies, CV versions, contacts, or tags.

## Dashboard API

`GET /dashboard` (authenticated) returns the current user's application totals:

- `totalCount` and `activeCount` (total minus archived);
- `applicationsByStatus` — counts per current status;
- `recentApplications` — the five most recently applied, newest first;
- `applicationsPerMonth` — count grouped by `DateApplied` month (ascending);
- `responseRate` — the ratio of applications with a response to applications where a response is still expected.

**Response rate (MVP definition):** the numerator counts applications currently in `RecruiterContacted`, `Interviewing`, `Offer`, `Rejected`, or `Withdrawn`. The denominator is the number of non-archived applications that are not in `Saved` (that is, `activeCount − saved`). A ratio of `0` is returned when the denominator is zero.

## Planned domain concepts

| Concept | Responsibility |
| --- | --- |
| ApplicationUser | Account identity and ownership boundary. |
| Company | A company recorded by one user. |
| JobApplication | The role, application details, current status, and notes. |
| ResumeVersion | A user-owned CV name/version, such as `CV-Backend-v3`. |
| ApplicationStatusHistory | An immutable record of status changes. |
| Contact | Recruiter or hiring-manager details for an application. |
| Tag | User-defined categorisation for applications. |

## Local development

### Run PostgreSQL locally

Option A — Homebrew:

```sh
brew install postgresql@17
brew services start postgresql@17
createdb inventorydb
```

Option B — Docker:

```sh
docker run --name jobtracker-db -e POSTGRES_PASSWORD=admin -e POSTGRES_DB=JobApplicationDB -p 5432:5432 -d postgres:17
```

The development connection string is read from `Backend/appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=JobApplicationDB;Username=fikhanenizar;Password=admin"
}
```

Keep real secrets out of source control. Override the connection string in production via environment variables (e.g. `ConnectionStrings__DefaultConnection`) or user secrets, never in `appsettings.json`.

### JWT configuration

Authentication uses ASP.NET Core Identity and JWT bearer tokens. The signing key and token details are read from the `Jwt` section of `Backend/appsettings.Development.json`:

```json
"Jwt": {
  "Issuer": "JobApplicationTracker",
  "Audience": "JobApplicationTracker",
  "SigningKey": "dev-only-signing-key-change-me-0123456789abcdef",
  "ExpiryMinutes": 120
}
```

The dev signing key is a local-only placeholder; generate a new random key (minimum 32 bytes) for any other environment and override it via `Jwt__SigningKey` or user secrets. Keep it out of source control.

### Apply database migrations

The Backend uses Entity Framework Core migrations (`Backend/Data/Migrations`).

```sh
# one-time setup: install the local dotnet-ef tool (uses .config/dotnet-tools.json)
dotnet tool restore

# create the database and apply pending migrations
dotnet ef database update --project Backend --startup-project Backend
```

Add a new migration after domain changes with:

```sh
dotnet ef migrations add <Name> --project Backend --startup-project Backend
```

## Definition of done

A feature is done when it has clear validation, authorization checks, appropriate tests, a simple usable Blazor UI, and no unrelated changes.
