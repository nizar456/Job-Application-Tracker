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

## Definition of done

A feature is done when it has clear validation, authorization checks, appropriate tests, a simple usable Blazor UI, and no unrelated changes.
