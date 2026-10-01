# Cinemax

An ASP.NET Core movie catalogue with genres, ratings, and viewer reviews.

## Features

- Browse movies, genres, and reviews
- Register and sign in with ASP.NET Core Identity
- Signed-in visitors can add movies and reviews
- Administrators manage catalogue entries and moderate reviews
- Server-side validation and protected forms

## Run locally

Requires .NET 9 and SQL Server. Configure `ConnectionStrings:DefaultConnection` with .NET user secrets or the `ConnectionStrings__DefaultConnection` environment variable, then apply the existing Entity Framework migrations.

```sh
dotnet run --project Cinemax
dotnet test Cinemax.sln
```

Google and GitHub sign-in are optional. Configure their client IDs and secrets using `Authentication__Google__ClientId`, `Authentication__Google__ClientSecret`, `Authentication__GitHub__ClientId`, and `Authentication__GitHub__ClientSecret`. Supply credentials privately.

Assign the `Administrator` role privately to trusted accounts. Public registration grants no administrator access. Configure email delivery for account confirmation before enabling public signups.
