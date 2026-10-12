import { spawn } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import assert from 'node:assert/strict'
const port = 48000 + process.pid % 1000
const origin = `http://127.0.0.1:${port}`
for (const enabled of ['true', 'false']) {
  let calls = 0
  const child = spawn(process.execPath, ['tests/fixtures/ai-server.mjs'], {
    cwd: fileURLToPath(new URL('..', import.meta.url)), env: { ...process.env,
      NITRO_HOST: '127.0.0.1', NITRO_PORT: String(port), NUXT_GOOGLE_TRANSLATION_ENABLED: enabled,
      NUXT_GOOGLE_TRANSLATION_API_KEY: 'fake-cloud-key', NUXT_GEMINI_API_KEY: 'fake-gemini-key',
      NUXT_BACKEND_API_BASE: 'http://127.0.0.1:9999',
    }, stdio: ['ignore', 'pipe', 'pipe'],
  })
  child.stdout.on('data', data => { calls += (data.toString().match(/PROVIDER_CALL/g) || []).length })
  let diagnostics = ''; child.stderr.on('data', data => { diagnostics += data.toString() })
  async function send(text, cookie, mode = 'translate') {
    const response = await fetch(origin + '/api/translate', { method: 'POST', headers: { 'Content-Type': 'application/json', ...(cookie ? { Cookie: cookie } : {}) }, body: JSON.stringify({ text, mode, sourceLanguage: 'en', targetLanguage: 'vi' }) })
    return { response, body: await response.json() }
  }
  try {
    let ready = false
    for (let i = 0; i < 100; i++) { try { ready = (await fetch(origin)).status === 200 } catch {} if (ready) break; await new Promise(resolve => setTimeout(resolve, 100)) }
    assert.ok(ready, diagnostics)
    const first = await send('Scientific entropy $S=k_B ln Ω$', 'paperflow_session=member-A')
    assert.equal(first.response.status, 200, JSON.stringify(first.body)); assert.equal(first.body.provider, enabled === 'true' ? 'google-cloud-v2-nmt' : 'gemini')
    assert.equal(calls, 1)
    const repeated = await send('Scientific entropy $S=k_B ln Ω$', 'paperflow_session=member-A')
    assert.equal(repeated.body.cached, true); assert.equal(calls, 1)
    await send('Scientific entropy $S=k_B ln Ω$', 'paperflow_session=member-B'); assert.equal(calls, 2, 'Private member caches are isolated')
    await Promise.all(Array.from({ length: 8 }, () => send('Concurrent example', 'paperflow_session=member-A')))
    assert.equal(calls, 3, 'One provider call for concurrent identical requests')
    const invalidSession = await send('No access', 'paperflow_session=invalid')
    assert.equal(invalidSession.response.status, 401); assert.equal(calls, 3)
    const timeout = await send('TIMEOUT', 'paperflow_session=member-A'); assert.ok([502, 504].includes(timeout.response.status)); assert.equal(calls, 4)
    const explain = await send('Scientific concept', 'paperflow_session=member-A', 'explain'); assert.equal(explain.body.provider, 'gemini'); assert.equal(calls, 5)
    const quota = await send('QUOTA', 'paperflow_session=member-A', 'explain'); assert.equal(quota.response.status, 429)
    assert.equal(quota.body.data.kind, 'quota_exceeded'); assert.equal(quota.body.data.retryable, false); assert.equal(calls, 6, 'No SDK auto retry')
    await send('QUOTA', 'paperflow_session=member-B', 'explain'); assert.equal(calls, 6, 'Provider quota cooldown prevents repeat calls')
    console.log(`AI production API checks passed with Cloud enabled=${enabled}: cache, dedup, auth/isolation, timeout, fallback and quota; no real provider calls.`)
  } finally {
    child.kill()
    await new Promise(resolve => { if (child.exitCode !== null) resolve(); else child.once('exit', resolve) })
  }
}
