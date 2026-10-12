import { spawn } from 'node:child_process'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'

const port = 49000 + process.pid % 1000
const origin = 'http://127.0.0.1:' + port
const server = spawn(process.execPath, ['scripts/start.mjs'], {
  cwd: fileURLToPath(new URL('..', import.meta.url)),
  env: { ...process.env, NITRO_HOST: '127.0.0.1', NITRO_PORT: String(port) },
  stdio: 'ignore',
})
try {
  let ready = false
  for (let i = 0; i < 100; i++) {
    try {
      const response = await fetch(origin)
      assert.equal(response.status, 200)
      assert.match(await response.text(), /PaperFlow/)
      ready = true
      break
    } catch { await new Promise(resolve => setTimeout(resolve, 100)) }
  }
  assert.ok(ready, 'Production server did not become ready')
  for (const path of ['/reader', '/login', '/register', '/verify-email']) {
    const response = await fetch(origin + path)
    assert.equal(response.status, 200, path)
    assert.match(await response.text(), /PaperFlow/)
  }
  for (const path of ['/workspace', '/library', '/settings', '/admin']) {
    const response = await fetch(origin + path, { redirect: 'manual' })
    assert.equal(response.status, 302, path)
    const destination = new URL(response.headers.get('location'), origin)
    assert.equal(destination.pathname, '/login', path)
    assert.equal(destination.searchParams.get('redirect'), path)
  }
  const missing = await fetch(origin + '/does-not-exist', { headers: { Accept: 'text/html' } })
  assert.equal(missing.status, 404)
  assert.match(await missing.text(), /404-caveman/)
  const favicon = await fetch(origin + '/favicon.svg')
  assert.equal(favicon.status, 200)
  assert.match(favicon.headers.get('content-type'), /svg/)
  // Invalid requests exercise validation without calling Gemini or spending quota.
  for (const [path, body] of [
    ['/api/translate', {}],
    ['/api/translate', { text: 'x'.repeat(5001) }],
    ['/api/translate', { text: 'sample', mode: 'invalid' }],
    ['/api/chat', { question: 'sample', pages: [] }],
    ['/api/chat', { question: 'sample', pages: [{ page: -1, text: 'sample' }] }],
    ['/api/chat', { question: 'sample', pages: [{ page: 1, text: 'sample' }], history: [{ role: 'system', text: 'invalid' }] }],
  ]) {
    const response = await fetch(origin + path, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
    assert.equal(response.status, 400, path)
    const payload = await response.json()
    assert.ok(payload.data?.message, 'Expected actionable validation message')
  }
  const oversized = await fetch(origin + '/api/translate', {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ text: 'x'.repeat(40000) }),
  })
  assert.equal(oversized.status, 413)
  let limited = false
  let aiCookie = ''
  for (let i = 0; i < 32; i++) {
    const response = await fetch(origin + '/api/translate', {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Forwarded-For': `192.0.2.${i + 1}`, ...(aiCookie ? { Cookie: aiCookie } : {}) }, body: '{}',
    })
    if (!aiCookie && response.headers.get('set-cookie')) aiCookie = response.headers.get('set-cookie').split(';')[0]
    if (response.status === 429) {
      assert.ok(response.headers.get('retry-after'))
      limited = true
      break
    }
    assert.equal(response.status, 400)
  }
  assert.ok(limited, 'AI rate limit must apply even if clients spoof forwarding headers')
  console.log('Production smoke checks passed: public landing/reader, guarded routes, custom 404, favicon, API validation, payload bounds and AI rate limiting; no provider calls.')
} finally { server.kill() }
