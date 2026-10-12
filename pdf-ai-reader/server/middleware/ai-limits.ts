import { createError, defineEventHandler, getRequestIP, getRequestURL, setResponseHeader, getRequestHeader, getCookie, setCookie } from 'h3'
import { randomBytes, createHmac, timingSafeEqual } from 'node:crypto'
import { useRuntimeConfig } from '#imports'
import { digest } from '../utils/ai-result-cache'
import { RequestLimiter } from '../utils/rate-limit'
const limiter = new RequestLimiter(300)
const sessionLimiter = new RequestLimiter(30)
const guestSecret = randomBytes(32)
const sign = (id: string) => createHmac('sha256', guestSecret).update(id).digest('hex')
export default defineEventHandler(async event => {
  if (event.method !== 'POST' || !['/api/chat', '/api/translate'].includes(getRequestURL(event).pathname)) return
  // Use the socket peer. Do not trust client-controlled X-Forwarded-For headers.
  const result = limiter.take(getRequestIP(event) || 'unknown')
  if (!result.allowed) {
    setResponseHeader(event, 'Retry-After', result.retryAfter)
    throw createError({ statusCode: 429, statusMessage: 'Too many AI requests', data: { message: 'Bạn đã dùng hết lượt AI tạm thời. Đợi một chút rồi thử lại nhé.' } })
  }
  setResponseHeader(event, 'Cache-Control', 'no-store')
  const origin = getRequestHeader(event, 'origin')
  if (origin) {
    let valid = false
    try { valid = new URL(origin).host === getRequestHeader(event, 'host') } catch {}
    if (!valid) throw createError({ statusCode: 403, data: { message: 'Nguồn yêu cầu không hợp lệ.' } })
  }
  // Validate member sessions; guests use a signed, opaque session cookie.
  const session = getCookie(event, 'paperflow_session')
  let guest = getCookie(event, 'paperflow_ai_guest') || ''
  const [guestId = '', signature = ''] = guest.split('.')
  const validGuest = /^[a-f0-9]{64}$/.test(guestId) && /^[a-f0-9]{64}$/.test(signature) && timingSafeEqual(Buffer.from(signature), Buffer.from(sign(guestId)))
  if (!session && !validGuest) {
    const id = randomBytes(32).toString('hex'); guest = id + '.' + sign(id)
    setCookie(event, 'paperflow_ai_guest', guest, { httpOnly: true, sameSite: 'lax', secure: !import.meta.dev, path: '/' })
  }
  if (session) {
    const config = useRuntimeConfig(event)
    try {
      const response = await $fetch<{ user?: { id: string } }>(`${String(config.backendApiBase).replace(/\/$/, '')}/api/auth/me`, {
        headers: { cookie: `paperflow_session=${encodeURIComponent(session)}` }, timeout: 5000, retry: 0,
      })
      if (!response.user?.id) throw createError({ statusCode: 401 })
    } catch (error) {
      const expired = Number((error as { statusCode?: number; status?: number }).statusCode || (error as { status?: number }).status) === 401
      throw createError({ statusCode: expired ? 401 : 503, data: { message: expired ? 'Phiên đã hết hạn. Hãy đăng nhập lại hoặc dùng chế độ khách.' : 'Chưa kiểm tra được phiên tài khoản. Hãy thử lại sau.', retryable: !expired } })
    }
  }
  event.context.aiScope = digest(session ? 'member:' + session : 'guest:' + guest)
  const allowance = sessionLimiter.take(event.context.aiScope)
  if (!allowance.allowed) {
    setResponseHeader(event, 'Retry-After', allowance.retryAfter)
    throw createError({ statusCode: 429, data: { message: 'Phiên của bạn đã hết lượt AI tạm thời.', kind: 'rate_limit', retryable: true, retryAfter: allowance.retryAfter } })
  }
})
