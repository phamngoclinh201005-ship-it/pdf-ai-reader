import { createError, defineEventHandler, getRequestIP, getRequestURL, setResponseHeader } from 'h3'
import { RequestLimiter } from '../utils/rate-limit'
const limiter = new RequestLimiter()
export default defineEventHandler(event => {
  if (event.method !== 'POST' || !['/api/chat', '/api/translate'].includes(getRequestURL(event).pathname)) return
  // Use the socket peer. Do not trust client-controlled X-Forwarded-For headers.
  const result = limiter.take(getRequestIP(event) || 'unknown')
  if (!result.allowed) {
    setResponseHeader(event, 'Retry-After', result.retryAfter)
    throw createError({ statusCode: 429, statusMessage: 'Too many AI requests', data: { message: 'Bạn đã dùng hết lượt AI tạm thời. Đợi một chút rồi thử lại nhé.' } })
  }
})
