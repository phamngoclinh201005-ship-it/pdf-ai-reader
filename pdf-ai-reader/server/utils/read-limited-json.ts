import { Buffer } from 'node:buffer'
import { getRequestHeader, type H3Event } from 'h3'
import { fail } from './api-error'

export async function readLimitedJson(event: H3Event, limit: number) {
  const length = Number(getRequestHeader(event, 'content-length'))
  if (Number.isFinite(length) && length > limit) fail(413, 'Nội dung yêu cầu quá lớn.')
  const chunks: Buffer[] = []
  let size = 0
  // Keep the stream intact so an over-limit request can receive a JSON error.
  for await (const chunk of event.node.req.iterator({ destroyOnReturn: false })) {
    const bytes = Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk)
    size += bytes.length
    if (size > limit) { event.node.req.resume(); fail(413, 'Nội dung yêu cầu quá lớn.') }
    chunks.push(bytes)
  }
  try { return JSON.parse(Buffer.concat(chunks).toString('utf8')) }
  catch { fail(400, 'Dữ liệu JSON không hợp lệ.') }
}
