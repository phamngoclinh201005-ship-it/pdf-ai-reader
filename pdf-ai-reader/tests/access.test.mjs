import { test } from 'node:test'
import assert from 'node:assert/strict'
import { safeAuthRedirect } from '../utils/auth-redirect.ts'
import { RequestLimiter } from '../server/utils/rate-limit.ts'

test('keeps safe protected destinations and rejects open redirects', () => {
  assert.equal(safeAuthRedirect('/library?folder=research'), '/library?folder=research')
  assert.equal(safeAuthRedirect('/settings'), '/settings')
  for (const value of ['https://evil.example', '//evil.example', '/\\evil.example', '/%2f%2fevil.example', '/login', null]) {
    assert.equal(safeAuthRedirect(value), '/workspace')
  }
})
test('limits callers independently and reopens the window after expiry', () => {
  const limiter = new RequestLimiter(2, 1000)
  assert.equal(limiter.take('a', 0).allowed, true)
  assert.equal(limiter.take('a', 1).allowed, true)
  assert.equal(limiter.take('a', 2).allowed, false)
  assert.equal(limiter.take('b', 2).allowed, true)
  assert.equal(limiter.take('a', 1001).allowed, true)
})
test('bounds the limiter metadata instead of allowing unlimited IP keys', () => {
  const limiter = new RequestLimiter(2, 1000, 1)
  assert.equal(limiter.take('a', 0).allowed, true)
  assert.equal(limiter.take('b', 1).allowed, false)
  assert.equal(limiter.take('b', 1001).allowed, true)
})
