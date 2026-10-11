/** Metadata only: no PDF, prompts, answers or session tokens are retained here. */
export class RequestLimiter {
  private windows = new Map<string, { count: number; resetAt: number }>()
  private limit: number
  private windowMs: number
  private maxKeys: number
  constructor(limit = 30, windowMs = 600000, maxKeys = 10000) {
    this.limit = limit; this.windowMs = windowMs; this.maxKeys = maxKeys
  }
  take(key: string, now = Date.now()) {
    for (const [ip, window] of this.windows) if (window.resetAt <= now) this.windows.delete(ip)
    let window = this.windows.get(key)
    if (!window) {
      if (this.windows.size >= this.maxKeys) return { allowed: false, retryAfter: 60 }
      window = { count: 0, resetAt: now + this.windowMs }
      this.windows.set(key, window)
    }
    const allowed = window.count < this.limit
    if (allowed) window.count++
    return { allowed, retryAfter: Math.max(1, Math.ceil((window.resetAt - now) / 1000)) }
  }
}
