import type { OtpChallenge, OtpTiming } from '~/types/otp'
import { challengeFromTiming } from '~/utils/otp'

const storageKey = 'paperflow-otp-challenge'
export function useOtpChallenge() {
  const challenge = useState<OtpChallenge | null>('otp-challenge', () => null)
  function remember(email: string, timing: OtpTiming) {
    challenge.value = challengeFromTiming(email, timing)
    if (import.meta.client) {
      try { sessionStorage.setItem(storageKey, JSON.stringify(challenge.value)) } catch { /* Storage may be disabled. */ }
    }
  }
  function restore() {
    if (!import.meta.client) return
    try {
      const saved = JSON.parse(sessionStorage.getItem(storageKey) || 'null')
      if (saved && typeof saved.email === 'string' && Number.isFinite(saved.expiresAt) && Number.isFinite(saved.resendAt)) challenge.value = saved
    } catch { /* Keep in-memory state when storage is unavailable. */ }
  }
  function clear() {
    challenge.value = null
    if (import.meta.client) { try { sessionStorage.removeItem(storageKey) } catch { /* Optional storage. */ } }
  }
  return { challenge, remember, restore, clear }
}
