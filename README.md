# Book Store --- Full Stack Technical Assessment

A full-stack book inventory application built with **.NET 10**,
**Angular 21**, **EF Core**, and **SQLite**.

The application lets users search the public **Open Library** catalog
through the backend API, select a published work, and save a store-owned
inventory record. Once a book is saved, reading, editing, filtering,
sorting, and deleting that inventory record use the local SQLite
database rather than requesting it again from Open Library.

## Technology stack

  -----------------------------------------------------------------------
  Layer                               Technology
  ----------------------------------- -----------------------------------
  Backend                             .NET 10, ASP.NET Core REST API

  Persistence                         Entity Framework Core, SQLite

  Backend architecture                Domain, Application (CQRS/feature
                                      slices), Infrastructure, API

  Frontend                            Angular 21, TypeScript, PrimeNG,
                                      Tailwind CSS

  Frontend state                      Angular signals

  External catalog                    Open Library (accessed through the
                                      backend)

  Backend tests                       xUnit

  Frontend tests                      Vitest, jsdom
  -----------------------------------------------------------------------

## Prerequisites

-   **.NET SDK 10** --- verify with `dotnet --version`.
-   **Node.js** compatible with Angular 21: Node **20.19+**, **22.12+**,
    or **24+** (use a supported Node release).
-   **npm** --- verify with `npm --version`.
-   Internet access for initial NuGet/npm installation and live Open
    Library searches.

No Open Library API key is required.

## Project structure

``` text
bookstore-submission/
├── api/
│   ├── BookStore.Domain/
│   ├── BookStore.Application/
│   ├── BookStore.Infrastructure/
│   ├── BookStore.Api/
│   └── BookStore.Tests/
├── web/
│   ├── src/
│   │   └── app/
│   │       ├── domain/
│   │       ├── application/
│   │       ├── infrastructure/
│   │       └── presentation/
│   ├── package.json
│   ├── package-lock.json
│   └── vitest.config.ts
└── README.md
```

## Quick start

Commands below assume you are in the **repository root** unless
otherwise noted.

### 1. Start the backend (terminal 1)

``` bash
cd api/BookStore.Api
dotnet restore
dotnet run --launch-profile http
```

The API runs at **http://localhost:5080**.

On initial startup, the application creates the SQLite database
(`api/BookStore.Api/books.db`) and seeds **three sample books**.

To reset the demo database, **stop the API first**, delete
`api/BookStore.Api/books.db`, and restart the API. This removes any
books you added locally.

The database connection is configured in
`api/BookStore.Api/appsettings.json`. Override it using the
`ConnectionStrings__Books` environment variable if needed.

### 2. Start the frontend (terminal 2)

From the repository root:

``` bash
cd web
npm install
npm start
```

Open **http://localhost:4200**.

The API allows CORS requests from this frontend origin. If the backend
uses a different port, update the `base` URL in
`web/src/app/infrastructure/http-repositories.ts`.

## Core functionality

-   Display the store's saved book inventory, including three seeded
    records on first run.
-   Filter books by genre, with the genre selection shared between the
    Header and Body.
-   Paginate and sort the inventory.
-   Search Open Library through the backend catalog endpoint.
-   Select a catalog work and add it to the store with genre, price, and
    stock.
-   Edit store-owned inventory fields.
-   Delete a saved book.
-   Report duplicate catalog selections and API/search failures.

**Catalog searches require internet access.** Existing stored inventory
remains local and does not depend on live Open Library requests.

## Manual end-to-end verification

1.  Start the API and web app. Confirm the three seeded books appear.
2.  Select a genre in the Header. Confirm the Body table reflects the
    selected genre.
3.  Click **Add book**, search for `Dune`, select a result, enter genre,
    price, and stock, and save.
4.  Confirm the new book appears in the inventory. Edit its price and
    stock, and verify the updated values.
5.  Attempt to add the same Open Library work again. Confirm the
    duplicate warning.
6.  Delete the newly added book. Confirm it disappears.
7.  With the backend running but external catalog access unavailable,
    attempt a catalog search and verify the error state.
8.  Check inventory pagination and sorting.

## API examples

Run these commands while the API is listening on port `5080`.

``` bash
# List books with pagination and descending price sort
curl 'http://localhost:5080/api/books?page=1&pageSize=10&sort=price&desc=true'

# Filter inventory by genre
curl 'http://localhost:5080/api/books?genre=Fiction'

# Search the external catalog through the API
curl 'http://localhost:5080/api/catalog/search?q=Dune'

# List inventory genres
curl 'http://localhost:5080/api/books/genres'
```

## Automated tests and build verification

### Backend: xUnit

From the repository root:

``` bash
dotnet test api/BookStore.Tests/BookStore.Tests.csproj -c Release
```

**Latest verified result:** 9 tests passed, 0 failed, 0 skipped.

Build the backend API in Release mode:

``` bash
dotnet build api/BookStore.Api/BookStore.Api.csproj -c Release
```

**Latest verified result:** Release build succeeded.

> Running `dotnet test` directly against `BookStore.Api` builds that
> project but does not execute the tests in `BookStore.Tests`. Use the
> test-project command above.

### Frontend: Vitest

From the repository root:

``` bash
cd web
npm test
```

**Latest verified result:** 6 tests passed, 0 failed.

The frontend tests cover:

1.  Loading inventory and updating shared signals.
2.  Sharing genre selection and resetting pagination.
3.  Handling inventory-list errors.
4.  Ignoring catalog searches shorter than two characters.
5.  Storing catalog search results.
6.  Reloading inventory and invoking the completion callback after
    adding a book.

Build the Angular application for production:

``` bash
npm run build
```

**Latest verified result:** Production build succeeded. Output is
written to `web/dist/bookstore`.

Run the frontend dependency audit:

``` bash
npm audit
```

**Latest verified result:** 0 reported vulnerabilities.

**Combined automated test result: 15 passed (9 backend + 6 frontend).**

These results verify the included automated test suites; they do not
constitute full production readiness or exhaustive end-to-end test
coverage.

## Architecture

### Backend

-   **`api/BookStore.Domain`** --- core book entity and genre
    definitions.
-   **`api/BookStore.Application`** --- feature-sliced CQRS handlers,
    mediator contracts, repository ports, and validation.
-   **`api/BookStore.Infrastructure`** --- EF Core SQLite persistence
    and isolated Open Library HTTP client.
-   **`api/BookStore.Api`** --- application composition root, REST
    endpoints, correlation IDs, error-handling middleware, and initial
    data seeding.
-   **`api/BookStore.Tests`** --- automated backend tests using xUnit.

A non-trivial explicit LINQ query is implemented in
**`SqliteBookStore.Counts()`** in
`api/BookStore.Infrastructure/Database.cs`, which groups stored books by
genre.

### Frontend

-   **`web/src/app/domain`** --- domain types and repository contracts.
-   **`web/src/app/application`** --- `BookState` use-case orchestration
    and shared reactive state.
-   **`web/src/app/infrastructure`** --- Angular `HttpClient` repository
    implementations and dependency-injection wiring.
-   **`web/src/app/presentation`** --- Header, Body, and Footer sibling
    components.

Angular signals in `BookState` synchronize the Header's genre selection
with the Body's inventory refresh without requiring direct parent-child
event wiring.

## Design decisions and limitations

1.  **Lightweight mediator:** A hand-rolled mediator avoids another
    dependency while keeping individual feature handlers explicit.
2.  **Catalog isolation:** Open Library is accessed through the backend
    for catalog searches. Persisted inventory operations use SQLite, not
    Open Library.
3.  **Catalog selection trust:** Search results are transient. `AddBook`
    trusts the selected catalog payload supplied by the frontend. A
    hardened production design could use a short-lived signed selection
    token or server-side selection cache to prevent metadata spoofing
    without re-fetching persisted inventory.
4.  **Database initialization:** EF Core `EnsureCreated` simplifies a
    fresh clone and demo startup. Production schema evolution should use
    versioned EF Core migrations.
5.  **Price storage:** Price is decimal in the domain/API, but SQLite
    maps it to `REAL` for ordering. Financial-grade applications should
    consider integer minor units or an explicit decimal converter.
6.  **Duplicate prevention:** `OpenLibraryKey` has a unique index, while
    ISBN duplicate checking occurs in the handler. A production
    implementation should consider a normalized unique ISBN index and
    concurrency-safe duplicate handling.
7.  **Scope:** Authentication, Docker, and CI pipelines were not added,
    consistent with the intended assessment scope.
8.  **Testing coverage:** Automated backend and Angular state tests are
    included and passing. Additional HTTP contract, integration, browser
    end-to-end, and failure-mode tests would be appropriate before a
    production release.

## Submission guidance

Include source code, project files, lockfiles, tests, and this README.
Do **not** include generated build outputs, dependency directories,
local SQLite database files, or secrets.

Common exclusions:

``` text
**/node_modules/
**/bin/
**/obj/
**/dist/
**/.angular/
**/coverage/
**/.DS_Store
**/*.db
**/*.db-shm
**/*.db-wal
```

Before submitting, confirm the repository can be restored, built, and
tested using the commands above. If submitting a ZIP, inspect its
contents to ensure both application code and automated tests are
present.

## Suggested incremental Git commits

If you are creating a commit history as you develop or review the
solution, use meaningful milestones such as:

``` text
chore: scaffold projects
feat(api): domain and CQRS slices
feat(api): SQLite and catalog adapter
feat(web): repositories and state
feat(web): CRUD UI
test(api): add xUnit tests
test(web): add Vitest state tests
docs: setup, verification and tradeoffs
```

Do not fabricate historical commits; use these messages only for actual
changes.
