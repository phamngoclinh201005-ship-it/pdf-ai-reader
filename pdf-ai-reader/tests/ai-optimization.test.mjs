import { test } from 'node:test'
import assert from 'node:assert/strict'
import { AiCache } from '../utils/ai-cache.ts'
import { GoogleCloudTranslationService, GeminiTranslationService } from '../server/utils/translation-service.ts'
import { providerFailure } from '../server/utils/ai-provider-error.ts'

test('first call computes; repeat and concurrent calls reuse the result', async () => {
  const cache = new AiCache(); let calls = 0
  const compute = async () => { calls++; await new Promise(r => setTimeout(r, 5)); return 'translation' }
  const answers = await Promise.all(Array.from({ length: 10 }, () => cache.run('same', compute)))
  assert.equal(calls, 1); assert.ok(answers.slice(1).every(a => a.cached))
  assert.equal((await cache.run('same', compute)).cached, true); assert.equal(calls, 1)
})
test('failures are not cached and can be explicitly retried', async () => {
  const cache = new AiCache(); let calls = 0
  await assert.rejects(cache.run('x', async () => { calls++; throw Error('timeout') }))
  await cache.run('x', async () => { calls++; return 'ok' }); assert.equal(calls, 2)
})
test('expiry, capacity and clear prevent stale results', async () => {
  const cache = new AiCache(1, 1000, 5)
  await cache.run('one', async () => 'one'); await cache.run('two', async () => 'two')
  assert.equal(cache.get('one'), undefined); assert.equal(cache.get('two', Date.now() + 10), undefined)
  let finish; const flight = cache.run('old', () => new Promise(resolve => { finish = resolve }))
  await Promise.resolve(); cache.clear(); finish('old'); await flight
  assert.equal(cache.get('old'), undefined)
})
test('different scope, language, mode and model keys cannot share content', async () => {
  const cache = new AiCache(); let calls = 0
  for (const key of ['owner-A|vi|translate|model-A', 'owner-B|vi|translate|model-A', 'owner-A|en|translate|model-A', 'owner-A|vi|explain|model-A', 'owner-A|vi|translate|model-B'])
    await cache.run(key, async () => String(++calls))
  assert.equal(calls, 5)
})
test('without Cloud configuration, Gemini abstraction preserves scientific input and token bound', async () => {
  const text = 'Quantum entropy: $S = -k_B \\sum p_i \\ln p_i$, α ≤ β; E=mc².'
  let received
  const service = new GeminiTranslationService(async (...args) => { received = args; return 'Bản dịch' })
  assert.equal(await service.translate({ text, source: 'en', target: 'vi' }), 'Bản dịch')
  assert.equal(received[1], text); assert.match(received[0], /en to vi/); assert.ok(received[2] <= 3200)
})
test('Cloud uses official HTTPS endpoint and restores LaTeX, never enables services or retries', async () => {
  let calls = 0
  const service = new GoogleCloudTranslationService('test-key', async (url, options) => {
    calls++; assert.equal(url, 'https://translation.googleapis.com/language/translate/v2')
    assert.equal(options.headers['X-Goog-Api-Key'], 'test-key')
    const body = JSON.parse(options.body); assert.equal(body.target, 'vi'); assert.equal(body.source, 'en'); assert.equal(body.format, 'text')
    return Response.json({ data: { translations: [{ translatedText: 'Khoa học &amp; ' + body.q }] } })
  })
  const text = 'Entropy $S = k_B ln Ω$ and ΔE ≥ 0.'
  const result = await service.translate({ text, source: 'en', target: 'vi' })
  assert.ok(result.includes('$S = k_B ln Ω$')); assert.ok(result.includes('ΔE ≥ 0')); assert.match(result, /Khoa học & /); assert.equal(calls, 1)
})
test('Cloud failure and timeout propagate without retry or Gemini double billing', async () => {
  let calls = 0
  const service = new GoogleCloudTranslationService('fake', async () => { calls++; return Response.json({ error: {} }, { status: 429 }) })
  await assert.rejects(service.translate({ text: 'Hello', source: 'auto', target: 'vi' }), error => error.status === 429)
  assert.equal(calls, 1)
  const timeout = new GoogleCloudTranslationService('fake', async () => { throw Object.assign(Error('timeout'), { name: 'TimeoutError' }) })
  await assert.rejects(timeout.translate({ text: 'Hello', source: 'auto', target: 'vi' }))
})
test('quota is distinguished only when metadata supports it; unknown 429 remains rate limit', () => {
  assert.equal(providerFailure({ status: 429, details: [{ quotaId: 'GenerateRequestsPerDay' }] }).kind, 'quota_exceeded')
  assert.equal(providerFailure({ status: 429 }).kind, 'rate_limit')
  assert.equal(providerFailure({ status: 429, details: [{ quotaId: 'GenerateRequestsPerMinute' }] }).retryable, true)
  assert.equal(providerFailure({ name: 'TimeoutError' }).kind, 'timeout')
})
