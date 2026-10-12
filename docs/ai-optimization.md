# AI and translation optimization

## Architecture and findings

Reader translation/explanation/summary use Nuxt `/api/translate`; chat uses Nuxt `/api/chat`. The .NET controllers are legacy standalone AI endpoints and are not used by these reader composables. Account, library, OTP and MongoDB stay on .NET. Google/Gemini credentials belong on the Nuxt FE **server**, never in public runtime config.

Previously, selection itself did not call AI, but repeated task clicks could abort/restart calls, no result cache existed, all tasks requested 4096 output tokens, and chat sent up to 600,000 PDF characters to Nuxt. Provider context could be 45,000 characters plus twelve large history entries. The installed Google SDK defaulted to five attempts unless configured otherwise.

## Final behavior

- Only explicit task actions call providers. While a task is running, duplicate task clicks are disabled. Selection continues to show the existing popup; copy/close/highlights/speech remain available.
- FE Map cache lives in the reader instance: 100 task results/2 MB; chat 50 results/1 MB. TTL 15 minutes. Document/account changes and unmount clear it, including in-flight cache writes. Task keys include exact trimmed text, mode, language and client prompt version. Chat keys include selected context, question, selection and bounded history.
- Nuxt cache: 500 results/8 MB, TTL 15 minutes, max 32 concurrent distinct provider operations. Duplicate in-flight requests share one Promise. Only successful results are retained. Key is SHA-256 over private session scope, text/context, source/target language, mode, provider/model, hashed credential identity and prompt version. No raw session token or PDF input is stored as a key.
- Member sessions are checked against .NET `/api/auth/me`. Anonymous readers use a signed HttpOnly session cookie. Cache scopes are separate for members and guests. Responses have `Cache-Control: no-store`. Origin checks and rate limits protect AI routes: 30 POSTs/10 minutes/session and 300/socket-IP/10 minutes, without trusting spoofable forwarding headers.
- Cache is local to each server process, ephemeral and cleared on restart. It is intentionally not a shared MongoDB cache: no additional durable copy of private PDF text, indexes, or database coupling. Multiple Render instances do not share cache/rate limits; shared infrastructure would be needed at larger scale. Signed guest sessions are renewed on restart. Do not use a CDN cache for these POST responses.
- Chat retrieval happens before upload: 8,000 characters of selected context or 18,000 for a general question. Nuxt revalidates and bounds it again. Six history entries, 2,000 characters each. Prompts require evidence, page citations and treat document instructions as untrusted data.
- Output limits: translation 512–3200 tokens based on input length; explanation 1600; summary 900; chat 1600. Truncated Gemini output is rejected with an actionable message rather than cached as a complete answer.
- Gemini has exactly one SDK attempt, 30-second timeout. FE POST fetches do not retry automatically. Known daily/zero quota errors differ from temporary rate limits when metadata permits; ambiguous 429 is not falsely labeled a daily quota. Provider cooldown is 60 seconds for rate limits, 10 minutes for identifiable quota exhaustion, with no automatic request scheduled. UI shows cooldown/retry where appropriate.

## Optional Google Cloud Translation

`ITranslationService` is a TypeScript interface on the active Nuxt API. `GoogleCloudTranslationService` calls the official Basic v2 HTTPS endpoint using plain text; `GeminiTranslationService` is the fallback. Existing `{text, mode}` requests remain valid. Optional `sourceLanguage` defaults to `auto`, `targetLanguage` to `vi`. Extra response metadata: `provider`, `cached`; existing `result` and `mode` remain.

Set on **FE Render Environment**, then redeploy:

```text
NUXT_GOOGLE_TRANSLATION_ENABLED=true
NUXT_GOOGLE_TRANSLATION_API_KEY=YOUR_RESTRICTED_SERVER_API_KEY
NUXT_GEMINI_API_KEY=YOUR_GEMINI_KEY
NUXT_GEMINI_MODEL=YOUR_AVAILABLE_MODEL
NUXT_BACKEND_API_BASE=https://paperflow-api-reif.onrender.com
```

Default Cloud flag is false. The integration does not provision a project, enable an API, enable billing, buy credits or modify quotas. Only turn it on after you authorize/configure Cloud Translation and suitable quotas/budgets. There is no assumption of unlimited free translation. Without the flag/key, translation uses Gemini; without Gemini availability, it reports the error. If configured Cloud rejects credentials/quota or times out, it reports failure without automatically spending a second request on Gemini.

LaTeX spans are protected with placeholders and restored. Ordinary terms/symbols use provider translation; terminology accuracy and complex formulas still require human review. Results are rendered as text, never HTML. Cloud Basic has no custom glossary; a future Advanced provider can implement the same interface.

Official references: [Basic API](https://docs.cloud.google.com/translate/docs/reference/rest/v2/translate), [Gemini troubleshooting](https://ai.google.dev/gemini-api/docs/troubleshooting).

## Files and validation

Changed: `composables/usePdfTranslation.ts`, `usePdfChat.ts`, `components/PdfReader.client.vue`, `AiChat.vue`, `server/api/translate.post.ts`, `chat.post.ts`, `server/middleware/ai-limits.ts`, `server/utils/gemini.ts`, `document-context.ts`, `api-error.ts`, `nuxt.config.ts`, `types/ai.ts`, `.env.example`, smoke tests. Added: `utils/ai-cache.ts`, `server/utils/ai-result-cache.ts`, `translation-service.ts`, `ai-provider-error.ts`, optimization unit tests and fake-provider production API fixture.

```powershell
npm --prefix pdf-ai-reader test
npm --prefix pdf-ai-reader run typecheck
npm --prefix pdf-ai-reader run build
npm --prefix pdf-ai-reader run test:smoke
npm --prefix pdf-ai-reader run test:ai
```

`test:ai` intercepts Google calls with test-only transports and never calls paid APIs. It exercises first/repeated/concurrent requests, cache isolation for two authenticated users, invalid sessions, timeout, Google enabled/disabled fallback, Gemini quota and single SDK attempt. Browser checks additionally verify selection alone makes zero API calls and repeated translation makes one request. Existing auth/OTP/Brevo/library/highlight regression tests use temporary MongoDB and fake email.

On Render: open a PDF, select without clicking and observe zero AI requests in Network; translate then repeat (local cache is instant), change document/account and verify a fresh scope. Inspect JSON metadata `cached`/`provider`. Explanation/summary/chat should report Gemini. Test quota on mock fixtures, not by deliberately spending production quota. Real Cloud delivery and linguistic accuracy are not proven by mocked tests and need an explicitly configured account.
