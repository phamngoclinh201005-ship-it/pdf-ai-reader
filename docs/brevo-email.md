# Brevo HTTPS email on Render Free

The default email implementation is now Brevo HTTP API (HTTPS port 443), for registration OTP, resend and password reset. Gmail SMTP is optional with `Email__Provider=Gmail`, but cannot connect from Render Free. No automatic SMTP fallback or paid upgrade is used.

Create a free Brevo account, complete account approval for transactional email, and create/verify a sender in Settings → Senders, Domains & IPs. Use an email you own; Gmail senders may have their From domain rewritten by Brevo. The free tier currently permits 300 emails/day; reaching its limit stops sending. Check current terms at https://www.brevo.com/products/transactional-email/.

Create an **API key**, not an SMTP key, in SMTP & API → API Keys. Do not paste it into chat or frontend .env.

Set these on the **backend Render service**:

```text
Email__Provider=Brevo
Brevo__ApiKey=YOUR_BREVO_API_KEY
Brevo__SenderEmail=YOUR_VERIFIED_SENDER_EMAIL
```

Keep Auth__OtpSecret and MongoDB settings. Save and deploy the new backend code. Old Smtp__ and Resend__ variables are unused in Brevo mode. No frontend email settings are needed.

Local configuration from the repository root:

```powershell
dotnet user-secrets set "Brevo:ApiKey" "YOUR_BREVO_API_KEY" --project backend/PaperFlow.Api
dotnet user-secrets set "Brevo:SenderEmail" "YOUR_VERIFIED_SENDER_EMAIL" --project backend/PaperFlow.Api
```

Test registration with a new email after deploy. HTTP acceptance is not guaranteed inbox delivery; inspect Brevo transactional logs and spam folders. API errors are logged by HTTP status only. Missing settings return 503; send failures return 502. Failed OTP delivery removes the unsent code so the user can retry.

Tests use a fake HTTP handler and temporary MongoDB, covering API endpoint/header/payload, provider rejection and existing auth flows; they send no real mail. Real delivery requires your verified sender and key.
