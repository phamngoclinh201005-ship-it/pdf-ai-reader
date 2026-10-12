# Gmail SMTP

Registration, resend OTP and password reset use `IAuthEmailService` implemented by `GmailEmailService` (MailKit). Resend is no longer used. Missing settings return 503 before creating registration/OTP records; SMTP delivery failures return 502. TLS is required and delivery has a 20-second deadline.

Enable Google 2-Step Verification and create an [App Password](https://support.google.com/accounts/answer/185833). Use that App Password, not the account login password. Do not put it in frontend `.env`, source files or chat.

Run from the repository root, replacing the placeholder values:

```powershell
dotnet user-secrets set "Smtp:Username" "YOUR_GMAIL@gmail.com" --project backend/PaperFlow.Api
dotnet user-secrets set "Smtp:Password" "YOUR_GOOGLE_APP_PASSWORD" --project backend/PaperFlow.Api
```

Restart BE after setting secrets. Existing `Auth:OtpSecret` is still required. Defaults are `smtp.gmail.com`, port 587 with STARTTLS; port 465 uses TLS from connection start. The sender defaults to Username; only configure `Smtp:From` for a sender allowed by your Google account. Spaces in the generated App Password are removed when authenticating.

On the Render **backend** service, set:

```text
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__Username=YOUR_GMAIL@gmail.com
Smtp__Password=YOUR_GOOGLE_APP_PASSWORD
```

Then redeploy BE. FE needs no SMTP settings. Old `Resend__ApiKey`/`Resend__From` variables are unused and can be removed.

Render Free web services block outgoing ports 25, 465 and 587. Gmail SMTP requires a paid Render instance or hosting that permits SMTP. See [Render Free limitations](https://render.com/docs/free) and [Google SMTP settings](https://support.google.com/mail/answer/7104828).

Verification uses a temporary MongoDB database and a fake implementation of the email interface. It sends no real email:

```powershell
dotnet build backend/PaperFlow.Auth.Tests --output .verification/gmail-auth-tests
dotnet .verification/gmail-auth-tests/PaperFlow.Auth.Tests.dll
```
