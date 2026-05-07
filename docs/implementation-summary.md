# Implementation Summary

This document summarizes the major changes made to the Stargate Astronaut Career Tracking System.

## Backend API

Implemented and hardened the required API workflows:

- Retrieve all people.
- Retrieve a person by name.
- Add a person by name.
- Update a person by name.
- Retrieve astronaut duties by person name.
- Add an astronaut duty.

Notable backend improvements:

- Added parameterized Dapper queries where user input is used.
- Added uniform response handling through shared controller response helpers.
- Added duplicate-name protection for `Person.Name`.
- Added case-insensitive person name matching and uniqueness.
- Added validation for person names, ranks, and duty titles.
- Added astronaut duty normalization:
  - Trims leading/trailing whitespace.
  - Stores duty titles in title case.
  - Treats `RETIRED` as `Retired`.
- Added a transaction around astronaut duty creation so detail and duty updates commit together.

## Validation Rules

Input validation is enforced in the backend and mirrored in the Angular UI.

Person name:

- Allows letters, numbers, spaces, apostrophes, and hyphens.
- Rejects other special characters.

Rank and duty title:

- Allows letters, numbers, and spaces.
- Rejects special characters.

## Database

Database-related changes:

- Added `ApplicationLog` table for request logging.
- Added unique index on `Person.Name`.
- Added case-insensitive collation for `Person.Name`.
- Stabilized seed data dates.
- Confirmed seed data follows normalization rules.

Seeded people:

- `John Doe`
- `Jane Doe`

Seeded astronaut data:

- `John Doe`
- Rank: `1LT`
- Duty title: `Commander`
- Career start date: `2024-01-01`

## Database Reset Script

Added a PowerShell reset script:

```powershell
.\scripts\reset-database.ps1 -Environment Development -Force
```

The script deletes the selected SQLite database and its sidecar files, then runs EF migrations to recreate the seeded database.

Preview DEV reset:

```powershell
.\scripts\reset-database.ps1 -Environment Development -WhatIf
```

Reset DEV:

```powershell
.\scripts\reset-database.ps1 -Environment Development -Force
```

Reset default/production:

```powershell
.\scripts\reset-database.ps1 -Environment Production -Force
```

Stop the API before running the reset script so SQLite does not lock the database.

## Logging

Added database-backed request logging using a MediatR pipeline behavior.

Logging captures:

- Successful requests.
- Failed requests.
- Exceptions.
- Operation name.
- Log level.
- Message.
- Exception message and stack trace when applicable.

Logs are stored in the `ApplicationLog` table.

## Tests And Coverage

Added backend integration tests using a temporary SQLite database.

Backend test coverage includes:

- Person creation.
- Person retrieval.
- Person rename.
- Duplicate person handling.
- Case-insensitive name handling.
- Astronaut duty retrieval.
- Astronaut duty creation.
- Astronaut duty transitions.
- Retired duty behavior.
- Logging.
- Input validation.

Latest backend verification:

- Tests: `40 passed, 0 failed`
- Line coverage: `91.91%`
- Branch coverage: `87.09%`

Backend test command:

```powershell
dotnet test StargateAPI.Tests\StargateAPI.Tests.csproj --collect:"XPlat Code Coverage" --settings StargateAPI.Tests\coverage.runsettings
```

Migrations are excluded from coverage through:

```text
StargateAPI.Tests\coverage.runsettings
```

## Angular UI

Added a production-style Angular UI in the `ui` folder.

Implemented UI workflows:

- View all people.
- Filter people by name.
- Add a person.
- Select a person.
- Rename a person.
- View astronaut duty history.
- Add astronaut duty.
- Display loading/error/success states.

UI polish:

- Light and dark mode.
- Theme selection persisted in local storage.
- Accessible sun/moon theme toggle.
- Stargate-inspired but original visual styling.
- Light-mode Egypt-inspired background art.
- Responsive layout.

## Accessibility

Accessibility improvements include:

- Skip link.
- Semantic landmarks.
- Explicit form labels.
- Screen-reader-only helper text where useful.
- Live regions for success and error messages.
- Strong keyboard focus states.
- Semantic duty history table.
- Accessible theme toggle labels.

## Angular Tests

Added Angular unit tests for component behavior and API service request shape.

Angular test coverage includes:

- Component render.
- People loading.
- Filtering.
- Person selection.
- Client-side validation.
- Theme persistence.
- API error display.
- API service GET/POST/PUT requests.

Latest Angular verification:

- Tests: `13 passed, 0 failed`
- Statements: `66.94%`
- Branches: `43.24%`
- Functions: `75%`
- Lines: `67.24%`

Angular test command:

```powershell
cd ui
npm run test:ci
```

Angular build command:

```powershell
cd ui
npm run build
```

## Running The Application

Start the API:

```powershell
cd api
dotnet run
```

Start the Angular UI:

```powershell
cd ui
npm start
```

The Angular dev server runs at:

```text
http://localhost:4200
```

The Angular proxy forwards `/api` calls to:

```text
http://localhost:5204
```

## Recent Commit Trail

Recent commits include:

- `Prevent SQL injection in person lookup`
- `Add database-backed request logging`
- `Add backend API integration tests`
- `Add unique index for person names`
- `Add application logging tests`
- `Harden astronaut duty and person validation`
- `Polish backend response handling`
- `Add Angular UI and input validation`
