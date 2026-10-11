// Nitro's import.meta fallback is not a valid absolute file URL on Windows.
// Initialize it before loading the generated server's static dependencies.
// See https://github.com/nitrojs/nitro/discussions/3177
const entry = new URL('../.output/server/index.mjs', import.meta.url)
globalThis._importMeta_ = { url: entry.href, env: process.env }
await import(entry.href)
