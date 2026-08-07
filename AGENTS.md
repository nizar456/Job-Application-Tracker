# Agent instructions

## Working style

- Read the relevant code and these instructions before changing files.
- Make the smallest change that fully satisfies the request; do not add speculative features.
- Preserve existing user changes and avoid unrelated refactors.
- State assumptions briefly when they materially affect implementation.
- Run focused build/tests after changes when practical, and report what was run.

## Design and clean code

- Use clear, domain-based names. Prefer `JobApplication` over vague names such as `Item` or `Data`.
- Keep methods short and focused; extract only when it improves clarity or reuse.
- Separate concerns: API endpoints handle HTTP, application services hold use cases, and persistence code stays in the data layer.
- Depend on abstractions at boundaries when there is a real substitution or testing need; do not create interfaces or layers solely by habit.
- Keep domain models, request/response DTOs, and UI models separate when crossing boundaries.
- Validate input at the API boundary and return consistent, useful problem responses.
- Do not expose entities, secrets, stack traces, or other users' data in API responses.

## SOLID, applied pragmatically

- **Single responsibility:** one class or component should have one primary reason to change.
- **Open/closed:** extend behaviour with focused types or configuration when change is expected; avoid premature frameworks.
- **Liskov substitution:** implementations must honour their abstraction's contract.
- **Interface segregation:** prefer small, purpose-specific interfaces.
- **Dependency inversion:** high-level use cases depend on abstractions at genuine external boundaries.

## Security and data ownership

- Require authentication for user data.
- Scope every query and mutation by the authenticated user ID; never trust an owner ID received from the client.
- Authorize an owned record before reading, updating, or deleting it.
- Keep secrets and connection strings out of source control; use configuration and secret storage.
- Use parameterized EF Core/LINQ queries; never build SQL from user input.

## Token-efficient collaboration

- Inspect narrowly with targeted searches; do not load or repeat unrelated files.
- Communicate conclusions, changed files, and verification—not long command logs or code already present in the repository.
- Prefer concise plans and focused diffs over broad rewrites.
- Reuse established project patterns and dependencies before introducing new ones.
- Ask a question only when a choice changes scope, security, data model, or user-visible behaviour; otherwise make a reasonable documented assumption.

## Feature boundaries

For the current MVP, CVs are names/versions only. Do not implement uploads, reminders, exports, hosting, or deployment work unless explicitly requested.
