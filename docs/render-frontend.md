# Render frontend API troubleshooting

The PDF reader calls `/api/translate` and `/api/chat` on the frontend origin. Both are Nuxt server routes available to guests and members. Setting `Gemini__ApiKey` on the .NET backend does not configure these routes.

Deploy the FE as a **Web Service** running Node, with root directory `pdf-ai-reader`, build command `npm ci && npm run build`, and start command `npm run start`. A static deployment of generated browser assets does not run the Nuxt AI endpoints or account/library proxy.

Set these variables on the frontend service:

```text
NITRO_PRESET=node-server
NUXT_GEMINI_API_KEY=YOUR_GEMINI_API_KEY
NUXT_GEMINI_MODEL=YOUR_AVAILABLE_GEMINI_MODEL
NUXT_BACKEND_API_BASE=https://paperflow-api-reif.onrender.com
```

Leave `NUXT_PUBLIC_API_BASE` empty (or remove it). Do not expose the Gemini key with a public variable. Rebuild and deploy from the latest source commit. The startup script respects Render's PORT via Nitro.

Verify `/api/health` returns backend JSON, and POST `/api/translate` with an empty JSON body returns JSON validation status 400. A real valid translation should return JSON containing a nonempty `result`; an empty 200 response does not mean AI succeeded.

The BE's `Dịch vụ dữ liệu tạm thời không khả dụng` response is separate: find the corresponding trace ID in BE runtime logs and inspect the error type. Check MongoDB Atlas credentials, network access and connection settings based on that error. Do not publish connection strings or passwords.
