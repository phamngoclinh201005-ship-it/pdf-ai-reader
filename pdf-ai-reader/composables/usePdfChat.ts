import type { PdfChatRequest, PdfChatResponse } from '~/types/ai'
import { selectContext } from '~/server/utils/document-context'
import { AiCache } from '~/utils/ai-cache'

/** Uses Nuxt's validated document-context API, including history and citations. */
export const usePdfChat = () => {
  const cache = new AiCache<PdfChatResponse>(50, 1_000_000, 900_000, 8)
  const clearCache = () => cache.clear()
  const { user } = useAuth()
  watch(() => user.value?.id, clearCache)
  onBeforeUnmount(clearCache)
  const askPdf = async (body: PdfChatRequest, signal?: AbortSignal) => {
    const chosen = selectContext(body.pages, body.question, body.selection, body.selection ? 8000 : 18000, true)
    const request = { ...body, pages: chosen.documents || [], history: body.history?.slice(-6).map(m => ({ ...m, text: m.text.slice(-2000) })) }
    const state = await cache.run(JSON.stringify(['chat-v2', request]), async () => {
      const response = await $fetch<PdfChatResponse>('/api/chat', { method: 'POST', body: request, signal, retry: 0, timeout: 40000 })
      if (typeof response?.result !== 'string' || !response.result.trim()) throw new Error('API AI không trả dữ liệu. Kiểm tra cấu hình máy chủ FE.')
      return response
    })
    const response = state.value
    if (typeof response?.result !== 'string' || !response.result.trim())
      throw new Error('API AI không trả dữ liệu. Kiểm tra FE Render đang chạy Nuxt server và cấu hình Gemini.')
    return response
  }

  return { askPdf, clearCache }
}
