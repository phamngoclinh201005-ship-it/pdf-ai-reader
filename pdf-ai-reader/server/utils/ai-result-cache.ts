import { createHash } from 'node:crypto'
import type { H3Event } from 'h3'
import { AiCache } from '../../utils/ai-cache'
import { fail } from './api-error'

const cache = new AiCache<string>(500, 8_000_000, 900_000, 32)
export const digest = (value: string) => createHash('sha256').update(value).digest('hex')
export async function cachedAi(event: H3Event, parameters: unknown, compute: () => Promise<string>) {
  const scope = event.context.aiScope as string | undefined
  if (!scope) fail(401, 'Không xác định được phiên AI. Hãy tải lại trang.')
  const key = digest(JSON.stringify([scope, parameters]))
  try { return await cache.run(key, compute) }
  catch (error) { if (error instanceof Error && error.message === 'AI_BUSY') fail(503, 'AI đang bận. Hãy thử lại sau ít phút.'); throw error }
}
