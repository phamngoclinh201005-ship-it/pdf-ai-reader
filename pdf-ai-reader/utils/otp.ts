import type { OtpChallenge, OtpTiming } from '../types/otp'

/** Align backend timestamps with the browser clock; never restart expiry on refresh. */
export function challengeFromTiming(email: string, timing: OtpTiming, now = Date.now()): OtpChallenge | null {
  const server = Date.parse(timing.serverTime)
  const expires = Date.parse(timing.expiresAt)
  const resend = Date.parse(timing.resendAvailableAt)
  if (![server, expires, resend].every(Number.isFinite)) return null
  return { email: email.trim().toLowerCase(), expiresAt: now + expires - server, resendAt: now + resend - server }
}
export function normalizeOtp(value: string) { return value.replace(/\D/g, '').slice(0, 6) }
export function secondsUntil(timestamp: number, now: number) { return Math.max(0, Math.ceil((timestamp - now) / 1000)) }
