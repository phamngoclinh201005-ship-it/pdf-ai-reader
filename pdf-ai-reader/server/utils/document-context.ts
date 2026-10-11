import type { DocumentPage } from '../../types/ai'
export type { DocumentPage } from '../../types/ai'
export function selectContext(pages: DocumentPage[], question: string, selection = '', limit = 45000) {
  const terms = [...new Set(question.toLocaleLowerCase().match(/[\p{L}\p{N}]{3,}/gu) || [])]
  const ranked = pages.map((page, index) => {
    const lower = page.text.toLocaleLowerCase()
    const score = terms.reduce((sum, term) => sum + (lower.includes(term) ? 1 : 0), 0)
      + (selection && lower.includes(selection.slice(0, 80).toLocaleLowerCase()) ? 20 : 0)
    return { ...page, score, index }
  }).sort((a, b) => b.score - a.score || a.index - b.index)
  let remaining = limit
  const chosen: DocumentPage[] = []
  for (const page of ranked) {
    if (remaining < 100) break
    const text = page.text.slice(0, remaining - 30)
    if (!text.trim()) continue
    chosen.push({ page: page.page, text })
    remaining -= text.length + 30
  }
  chosen.sort((a, b) => a.page - b.page)
  return { text: chosen.map(p => `[Trang ${p.page}]\n${p.text}`).join('\n\n'), pages: chosen.map(p => p.page) }
}
