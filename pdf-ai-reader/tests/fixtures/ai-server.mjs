// Test-only provider transport. Blocks all Google AI network calls from this process.
const nativeFetch = globalThis.fetch
let calls = 0
globalThis.fetch = async (input, options = {}) => {
  const url = String(input instanceof Request ? input.url : input)
  if (url.includes('127.0.0.1:9999/api/auth/me')) {
    const cookie = new Headers(options.headers).get('cookie') || ''
    return cookie.includes('member-A') || cookie.includes('member-B') ? Response.json({ user: { id: cookie.includes('member-A') ? 'A' : 'B' } }) : Response.json({}, { status: 401 })
  }
  if (url.startsWith('https://translation.googleapis.com/') || url.startsWith('https://generativelanguage.googleapis.com/')) {
    calls++; process.stdout.write('PROVIDER_CALL\n')
    const body = JSON.parse(options.body || '{}')
    const text = body.q || JSON.stringify(body.contents)
    if (text.includes('TIMEOUT')) throw Object.assign(Error('simulated timeout'), { name: 'TimeoutError' })
    if (text.includes('QUOTA')) return Response.json({ error: { code: 429, status: 'RESOURCE_EXHAUSTED', message: 'quota metric GenerateRequestsPerDay' } }, { status: 429 })
    await new Promise(resolve => setTimeout(resolve, 40))
    if (url.includes('translation.googleapis.com')) return Response.json({ data: { translations: [{ translatedText: 'Bản dịch: ' + text }] } })
    return Response.json({ candidates: [{ content: { role: 'model', parts: [{ text: 'Mock Gemini answer [Trang 1]' }] }, finishReason: 'STOP' }] })
  }
  return nativeFetch(input, options)
}
const entry = new URL('../../.output/server/index.mjs', import.meta.url)
globalThis._importMeta_ = { url: entry.href, env: process.env }
await import(entry.href)
