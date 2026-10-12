import { createError } from 'h3'

export function fail(statusCode: number, message: string, metadata: Record<string, unknown> = {}): never {
  throw createError({ statusCode, statusMessage: 'AI request failed', data: { message, ...metadata } })
}
