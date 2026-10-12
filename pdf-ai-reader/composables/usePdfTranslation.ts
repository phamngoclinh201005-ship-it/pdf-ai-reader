import type { AiMode, TranslationResponse } from '~/types/ai'
import { AiCache } from '~/utils/ai-cache'
export type { AiMode } from '~/types/ai'

/** Request cancellation and UI state are owned by the selecting component. */
export const usePdfTranslation = () => {
  const cache = new AiCache<TranslationResponse>(100, 2_000_000, 900_000, 8)
  const cached = ref(false)
  const clearCache = () => { cache.clear(); cached.value = false }
  const { user } = useAuth()
  watch(() => user.value?.id, clearCache)
  onBeforeUnmount(clearCache)
  const processText = async (text: string, mode: AiMode = 'translate', signal?: AbortSignal) => {
    const responseState = await cache.run(JSON.stringify(['v2', text.trim(), mode, 'auto', 'vi']), async () => {
      const response = await $fetch<TranslationResponse>('/api/translate', {
        method: 'POST', body: { text, mode }, signal, retry: 0, timeout: 40000,
      })
      if (typeof response?.result !== 'string' || !response.result.trim()) throw new Error('API AI không trả dữ liệu. Kiểm tra cấu hình máy chủ FE.')
      return response
    })
    const response = responseState.value
    cached.value = responseState.cached || !!response.cached
    if (typeof response?.result !== 'string' || !response.result.trim())
      throw new Error('API AI không trả dữ liệu. Kiểm tra FE Render đang chạy Nuxt server và cấu hình Gemini.')
    return response.result
  }

  return { processText, cached, clearCache }
}
