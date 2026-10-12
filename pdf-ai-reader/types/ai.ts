/** Contracts for the same-origin Nuxt AI endpoints. */
export interface DocumentPage { page: number; text: string }
export type AiMode = 'translate' | 'explain' | 'summarize'
export interface ChatMessage { role: 'user' | 'assistant'; text: string }
export interface PdfChatRequest {
  question: string
  pages: DocumentPage[]
  history?: ChatMessage[]
  selection?: string
}
export interface PdfChatResponse { result: string; sources: number[]; cached?: boolean }
export interface TranslationResponse { result: string; mode: AiMode; cached?: boolean; provider?: string }
