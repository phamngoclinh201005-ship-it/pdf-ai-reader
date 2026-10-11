import type { AiMode, TranslationResponse } from '~/types/ai'
export type { AiMode } from '~/types/ai'

/** Request cancellation and UI state are owned by the selecting component. */
export const usePdfTranslation = () => {
  const processText = async (text: string, mode: AiMode = 'translate', signal?: AbortSignal) => {
    const response = await $fetch<TranslationResponse>('/api/translate', {
      method: 'POST', body: { text, mode }, signal,
    })
    return response.result
  }

  return { processText }
}
