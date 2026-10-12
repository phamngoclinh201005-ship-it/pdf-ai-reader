import { GoogleGenAI } from '@google/genai'
import type { H3Event } from 'h3'
import { fail } from './api-error'
import { useRuntimeConfig } from '#imports'

export async function generateAnswer(event: H3Event, instructions: string, contents: string | { role: string; parts: { text: string }[] }[]) {
  const config = useRuntimeConfig(event)
  if (!config.geminiApiKey) fail(503, 'Chưa cấu hình GEMINI_API_KEY trong file .env.')
  const ai = new GoogleGenAI({ apiKey: String(config.geminiApiKey), httpOptions: { timeout: 60000 } })
  try {
    const response = await ai.models.generateContent({
      model: String(config.geminiModel), contents,
      config: { systemInstruction: instructions, temperature: 0.2, maxOutputTokens: 4096 },
    })
    const result = response.text?.trim()
    if (!result) fail(502, 'Gemini chưa trả về nội dung. Hãy thử lại với một câu hỏi khác.')
    return result!
  } catch (err: unknown) {
    const status = Number((err as { status?: number })?.status)
    console.error('Gemini request failed', { status: status || 502, model: String(config.geminiModel) })
    if (status === 429) fail(429, 'Gemini đang giới hạn lượt gọi hoặc đã hết hạn mức. Hãy thử lại sau.')
    if (status === 401 || status === 403) fail(502, 'Gemini từ chối API key. Kiểm tra khóa và quyền truy cập model.')
    if (status === 404) fail(502, 'Model Gemini không khả dụng với cấu hình hiện tại. Kiểm tra NUXT_GEMINI_MODEL trên máy chủ FE rồi khởi động hoặc deploy lại.')
    fail(502, 'Không thể nhận phản hồi từ Gemini. Vui lòng thử lại.')
  }
}
