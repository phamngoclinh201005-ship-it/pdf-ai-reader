import type { PdfChatRequest, PdfChatResponse } from '~/types/ai'

/** Uses Nuxt's validated document-context API, including history and citations. */
export const usePdfChat = () => {
  const askPdf = (body: PdfChatRequest, signal?: AbortSignal) =>
    $fetch<PdfChatResponse>('/api/chat', { method: 'POST', body, signal })

  return { askPdf }
}
