import { test } from 'node:test'
import assert from 'node:assert/strict'
import { challengeFromTiming, normalizeOtp, secondsUntil } from '../utils/otp.ts'

test('aligns OTP expiry with backend time even when client clock is different', () => {
  const challenge = challengeFromTiming(' Reader@Example.com ', {
    serverTime: '2026-10-12T12:00:00Z', expiresAt: '2026-10-12T12:05:00Z', resendAvailableAt: '2026-10-12T12:01:00Z',
  }, 1000)
  assert.deepEqual(challenge, { email: 'reader@example.com', expiresAt: 301000, resendAt: 61000 })
  assert.equal(secondsUntil(challenge.expiresAt, 121000), 180)
  assert.equal(secondsUntil(challenge.expiresAt, 301001), 0)
})
test('does not invent a deadline for invalid backend timestamps', () => {
  assert.equal(challengeFromTiming('reader@example.com', { serverTime: 'invalid', expiresAt: '', resendAvailableAt: '' }), null)
})
test('sanitizes an OTP pasted from email without accepting letters or extra digits', () => {
  assert.equal(normalizeOtp('Your code: 12 34 56'), '123456')
  assert.equal(normalizeOtp('0123456789'), '012345')
  assert.equal(normalizeOtp('abc'), '')
})
