# Cinemax

An ASP.NET Core movie site with real posters, cast details, search, and viewer reviews.

**Live site:** [cinemax.jonathanilori.xyz](https://cinemax.jonathanilori.xyz)

![Cinemax home page with a featured movie and popular titles](docs/screenshot.jpg)

## Features

- Browse 300 movies with data from TMDB, filtered by genre
- Live search by title, director, or cast, with results from TMDB for anything else
- Open a movie page with its plot, cast, scores, and viewer reviews
- Read member reviews from TMDB, credited to their authors, alongside reviews posted on Cinemax
- Register and sign in with ASP.NET Core Identity, with email confirmation and password reset by email
- Signed-in visitors can add reviews
- Administrators add movies, manage entries, and moderate reviews
- Server-side validation and protected forms
- 51 automated tests (MSTest) covering permissions, request security, email flows and the TMDB client

## Run locally

Requires .NET 9. Cinemax uses SQLite and creates `cinemax.db` the first time it runs. To store it somewhere else, set `ConnectionStrings__DefaultConnection`.

```sh
dotnet run --project Cinemax
dotnet test Cinemax.sln
```

Set `Tmdb__ApiKey` privately to load 24 popular movies on the first run and turn on TMDB search for administrators. Without it, movies can still be added by hand.

Account emails go through [Resend](https://resend.com). Set `Email__ResendApiKey` and `Email__From` (a sender on a domain verified in Resend). Without a key, emails are written to the app log, so you can confirm accounts locally.

Google and GitHub sign-in are optional. Configure their client IDs and secrets using `Authentication__Google__ClientId`, `Authentication__Google__ClientSecret`, `Authentication__GitHub__ClientId`, and `Authentication__GitHub__ClientSecret`. Supply credentials privately.

To create the administrator, set `Admin__Email` and `Admin__Password` privately before the first run. Public registration grants no administrator access, even with the same email address. Email confirmation is off until email delivery is set up.

## Credits

Movie data and images come from [TMDB](https://www.themoviedb.org/). This product uses the TMDB API but is not endorsed or certified by TMDB.
