# Reader tools

- Reader icon buttons provide native title hints and accessible labels, including Logout, theme, AI chat, history, save/sync and page/zoom controls.
- Signed-in readers can open the folder button beside “Tài liệu của bạn” to list their saved PDFs without leaving the reader. The dialog loads the real owner-scoped library and supports loading/error/empty states. File changes wait for saving the current state; failed saves remain visible and block switching.
- “Đọc thành tiếng” offers Vietnamese/English and speed selection, current-page reading, pause/resume and stop. Selecting PDF text also offers “Đọc”; after AI translation it reads the result. It uses browser speech synthesis, with no paid API. Available voices and quality depend on the browser/OS; some voices can use the platform's network service. Scanned PDFs need OCR to obtain readable text. Reading stops on page/document changes and leaving the reader.
- Native view transitions and Nuxt page transitions are disabled to avoid the blank-screen failure during route changes. Other component animations remain available.

Validation includes TypeScript, production build/smoke checks and browser checks for inline document selection, tooltip labels, speech controls using a simulated speech engine, and client navigation to Library and back without reload. Actual audio/voice availability still requires testing on the user's device.
