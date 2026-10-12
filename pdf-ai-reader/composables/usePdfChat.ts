import type { PdfChatRequest, PdfChatResponse } from '~/types/ai'

/** Uses Nuxt's validated document-context API, including history and citations. */
export const usePdfChat = () => {
  const askPdf = async (body: PdfChatRequest, signal?: AbortSignal) => {
    const response = await $fetch<PdfChatResponse>('/api/chat', { method: 'POST', body, signal })
    if (typeof response?.result !== 'string' || !response.result.trim())
      throw new Error('API AI không trả dữ liệu. Kiểm tra FE Render đang chạy Nuxt server và cấu hình Gemini.')
    return response
  }

  return { askPdf }
}
