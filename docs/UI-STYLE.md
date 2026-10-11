# PaperFlow UI

The shared palette and primitives live in `pdf-ai-reader/assets/css/main.css`.

- Use `--pf-bg`, `--pf-surface`, `--pf-ink`, `--pf-muted`, `--pf-border` and `--pf-accent` for colors. `--pf-gradient` is the violet-to-pink primary action.
- Use `pf-card`, `pf-button`, `pf-input`, `pf-link` and `pf-kicker` for account forms and content. New account pages should use `AccountShell`, which includes `SiteHeader` and responsive page spacing.
- Use `/favicon.svg` for the PaperFlow mark. Keep the document canvas white for reading.
- The shared Nuxt color mode defaults to dark and persists the user's preference across landing, reader and account pages. Login's lamp scene and OTP's universe scene intentionally keep a dark art background; their controls and branding use the same palette.
- Keep the lamp, OTP, upload and 404 animations, and honor `prefers-reduced-motion`. Use real session data and preserve loading, error and disabled states.
