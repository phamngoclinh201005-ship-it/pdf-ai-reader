import type { AiMode, TranslationResponse } from '~/types/ai'
export type { AiMode } from '~/types/ai'

/** Request cancellation and UI state are owned by the selecting component. */
export const usePdfTranslation = () => {
  const processText = async (text: string, mode: AiMode = 'translate', signal?: AbortSignal) => {
    const response = await $fetch<TranslationResponse>('/api/translate', {
      method: 'POST', body: { text, mode }, signal,
    })
    if (typeof response?.result !== 'string' || !response.result.trim())
      throw new Error('API AI không trả dữ liệu. Kiểm tra FE Render đang chạy Nuxt server và cấu hình Gemini.')
    return response.result
  }

  return { processText }
}
