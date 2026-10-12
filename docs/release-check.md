> Email production now defaults to Brevo HTTPS API; see [brevo-email.md](brevo-email.md). SMTP instructions apply only when Email__Provider=Gmail.

# Release verification

Before deploying, run FE `npm test`, `npm run typecheck`, `npm run build`, `npm run test:smoke`, then build/run `PaperFlow.Auth.Tests` and `PaperFlow.Library.Tests`. Backend production output is produced with `dotnet publish backend/PaperFlow.Api -c Release`.

Coverage includes auth redirects, OTP timing/input, AI context selection/validation/rate limits, registration/verification/login/logout/password reset, failed email retry, private PDF/state storage, cross-account isolation, large PDF chunking, deletion, public pages, guarded pages, favicon and custom 404. Browser library checks additionally exercise PDF loading, chat/AI history, guest reload loss, member reload restore, switching documents, logout and mobile layout. Tests use temporary databases and fake email/AI; they do not prove delivery to a real Gmail inbox.

Render backend must have `MongoDB__ConnectionString`, `MongoDB__DatabaseName`, `Auth__OtpSecret` (32+ characters), `Frontend__Origin=https://paperflow-web.onrender.com`, and the SMTP settings documented in [gmail-smtp.md](gmail-smtp.md). Do not include a trailing slash in Frontend Origin.

Render frontend needs `NUXT_BACKEND_API_BASE=https://paperflow-api-reif.onrender.com` at build time. Leave `NUXT_PUBLIC_API_BASE` empty or remove its override so account/library requests use the FE same-origin proxy and session cookies remain accessible during SSR. AI keys belong only in server environment variables.

Gmail SMTP cannot send from a Render Free web service because outgoing SMTP ports are blocked. Deploy the BE to a service that permits SMTP before testing actual OTP delivery. A successful application build does not remove this hosting limitation.

After pushing changes, redeploy both services from the intended commit. Reproduce a registration request and inspect its Response plus matching BE logs. Unexpected API failures include a trace ID; SMTP logs report acceptance or failure type/status without credentials or OTP. HTTP 202 does not guarantee inbox delivery; already verified registration addresses and active resend cooldowns intentionally have the same public response shape.
