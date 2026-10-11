# Local API connection

Start the frontend with `npm run dev`. Its `.env` uses `NUXT_PUBLIC_API_BASE=http://localhost:5050`; restart Nuxt after changing this value.

Start the API with `npm run dev:backend` (HTTP launch profile: port 5050). Railway's `PORT` overrides the local launch profile only when supplied. Docker continues using port 8080 through `ASPNETCORE_HTTP_PORTS`.

If an Atlas connection in .NET User Secrets fails authentication, use `npm run dev:backend:local` while developing. MongoDB Community Server must be running locally. This command uses the localhost connection from `appsettings.json` for that process only; it does not modify Atlas secrets. Local and Atlas databases have separate accounts and data.

Check `http://localhost:5050/api/health`. A POST to `/api/auth/register` with `{}` should return 400 validation errors, confirming the endpoint is reachable without creating an account or sending email. A real registration also requires valid Resend settings.
