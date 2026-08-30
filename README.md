# Library

An API-driven application for a public library: books, borrowers, lending, and statistics over borrowing activity.

Three projects: `Library.Api` (public REST API), `Library.Service` (domain logic and SQL Server), and `Library.Contracts` (the `.proto` the two use to talk to each other over gRPC).

## Running

Requires Docker. Nothing else — the .NET SDK is only needed to run the tests.

```bash
docker compose up -d --build
```

That starts SQL Server, the gRPC service, and the API. The service applies its migrations and seeds the database on start, so there is no separate setup step.

- Swagger: <http://localhost:5006/swagger>
- `Library.Api/Library.Api.http` has a ready-made request for every endpoint, including the ones that are supposed to fail.

The first run builds two images and takes a few minutes; later ones are quick.

```bash
docker compose down
```

The database is not given a volume, so stopping the stack discards it and the next start seeds a fresh one.

### Endpoints

```
GET  /api/books/most-borrowed?limit=&from=&to=
GET  /api/books/{bookId}/also-borrowed?limit=
GET  /api/borrowers/top?limit=&from=&to=
GET  /api/borrowers/{borrowerId}/reading-pace
POST /api/loans                            { borrowerId, bookId }
POST /api/loans/{loanId}/return
```

## Running the tests

```bash
dotnet test                              # everything, about a minute
dotnet test Library.Tests.Unit           # milliseconds, no Docker needed
dotnet test Library.Tests.Integration    # needs Docker
```

The integration tests start their own SQL Server container through Testcontainers.

The warm-up tasks and their tests are in `Library.Tests.Unit/WarmUp`.
