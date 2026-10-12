import { GoogleGenAI } from '@google/genai'
import type { H3Event } from 'h3'
import { fail } from './api-error'
import { useRuntimeConfig } from '#imports'
import { providerFailure } from './ai-provider-error'
import { digest } from './ai-result-cache'
const cooldowns = new Map<string, { until: number; quota: boolean }>()

export async function generateAnswer(event: H3Event, instructions: string, contents: string | { role: string; parts: { text: string }[] }[], maxOutputTokens = 1600) {
  const config = useRuntimeConfig(event)
  if (!config.geminiApiKey) fail(503, 'Chưa cấu hình GEMINI_API_KEY trong file .env.')
  const providerKey = digest(String(config.geminiApiKey) + ':' + String(config.geminiModel))
  for (const [key, value] of cooldowns) if (value.until <= Date.now()) cooldowns.delete(key)
  const blocked = cooldowns.get(providerKey)
  if (blocked) fail(429, blocked.quota ? 'Gemini đã hết hạn mức. Hãy chờ hạn mức được cấp lại.' : 'Gemini đang giới hạn tần suất. Hãy đợi rồi thử lại.', {
    kind: blocked.quota ? 'quota_exceeded' : 'rate_limit', retryable: !blocked.quota, retryAfter: Math.ceil((blocked.until - Date.now()) / 1000),
  })
  const ai = new GoogleGenAI({ apiKey: String(config.geminiApiKey), httpOptions: { timeout: 30000, retryOptions: { attempts: 1 } } })
  try {
    const response = await ai.models.generateContent({
      model: String(config.geminiModel), contents,
      config: { systemInstruction: instructions, temperature: 0.2, maxOutputTokens },
    })
    const result = response.text?.trim()
    if (response.candidates?.some(candidate => candidate.finishReason === 'MAX_TOKENS'))
      fail(422, 'Đoạn văn quá dài cho một lượt xử lý. Hãy chọn đoạn ngắn hơn.', { retryable: false, kind: 'output_limit' })
    if (!result) fail(502, 'Gemini chưa trả về nội dung. Hãy thử lại với một câu hỏi khác.')
    return result!
  } catch (err: unknown) {
    if ((err as { data?: { message?: string } })?.data?.message) throw err
    const failure = providerFailure(err)
    const status = failure.status
    console.error('Gemini request failed', { status: status || 502, model: String(config.geminiModel) })
    if (status === 429) {
      if (cooldowns.size >= 32) cooldowns.delete(cooldowns.keys().next().value!)
      cooldowns.set(providerKey, { until: Date.now() + (failure.kind === 'quota_exceeded' ? 600000 : 60000), quota: failure.kind === 'quota_exceeded' })
    }
    if (status === 429) fail(429, failure.kind === 'quota_exceeded' ? 'Gemini đã hết hạn mức. Hãy chờ hạn mức được cấp lại.' : 'Gemini đang giới hạn tần suất. Hãy đợi một phút rồi thử lại.', { ...failure, retryAfter: 60 })
    if (status === 401 || status === 403) fail(502, 'Gemini từ chối API key. Kiểm tra khóa và quyền truy cập model.')
    if (status === 404) fail(502, 'Model Gemini không khả dụng với cấu hình hiện tại. Kiểm tra NUXT_GEMINI_MODEL trên máy chủ FE rồi khởi động hoặc deploy lại.')
    fail(status === 504 ? 504 : 502, 'Không thể nhận phản hồi từ Gemini. Vui lòng thử lại.', failure)
  }
}
