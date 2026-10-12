/** Bounded per-process/per-reader cache. Only successful results are retained. */
export class AiCache<T> {
  private values = new Map<string, { value: T; expires: number; bytes: number }>()
  private pending = new Map<string, Promise<T>>()
  private bytes = 0
  private generation = 0
  private maxEntries: number; private maxBytes: number; private ttl: number; private maxPending: number
  constructor(maxEntries = 100, maxBytes = 2_000_000, ttl = 900_000, maxPending = 32) {
    this.maxEntries = maxEntries; this.maxBytes = maxBytes; this.ttl = ttl; this.maxPending = maxPending
  }
  get(key: string, now = Date.now()): T | undefined {
    const entry = this.values.get(key)
    if (!entry) return
    if (entry.expires <= now) { this.values.delete(key); this.bytes -= entry.bytes; return }
    this.values.delete(key); this.values.set(key, entry)
    return entry.value
  }
  async run(key: string, compute: () => Promise<T>): Promise<{ value: T; cached: boolean }> {
    const hit = this.get(key)
    if (hit !== undefined) return { value: hit, cached: true }
    const flight = this.pending.get(key)
    if (flight) return { value: await flight, cached: true }
    if (this.pending.size >= this.maxPending) throw new Error('AI_BUSY')
    const promise = Promise.resolve().then(compute)
    const generation = this.generation
    this.pending.set(key, promise)
    try {
      const value = await promise
      const size = new TextEncoder().encode(key + JSON.stringify(value)).length
      if (generation === this.generation && size <= this.maxBytes) {
        const now = Date.now()
        for (const [k, entry] of this.values) if (entry.expires <= now) { this.values.delete(k); this.bytes -= entry.bytes }
        while (this.values.size >= this.maxEntries || this.bytes + size > this.maxBytes) {
          const oldest = this.values.keys().next().value
          if (oldest === undefined) break
          this.bytes -= this.values.get(oldest)!.bytes; this.values.delete(oldest)
        }
        this.values.set(key, { value, expires: now + this.ttl, bytes: size }); this.bytes += size
      }
      return { value, cached: false }
    } finally { if (this.pending.get(key) === promise) this.pending.delete(key) }
  }
  clear() { ++this.generation; this.values.clear(); this.pending.clear(); this.bytes = 0 }
}
