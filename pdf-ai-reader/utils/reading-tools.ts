import type { SavedHighlight, SavedBookmark } from '~/types/library'

export function searchDocument(pages: { page: number; text: string }[], query: string) {
  const needle = query.trim().toLocaleLowerCase()
  if (needle.length < 2) return []
  const matches: { page: number; snippet: string }[] = []
  for (const page of pages) {
    const text = page.text.replace(/\s+/g, ' ')
    const lower = text.toLocaleLowerCase()
    let from = 0
    while (matches.length < 100) {
      const at = lower.indexOf(needle, from)
      if (at < 0) break
      matches.push({ page: page.page, snippet: text.slice(Math.max(0, at - 55), at + needle.length + 85) })
      from = at + needle.length
    }
    if (matches.length >= 100) break
  }
  return matches
}

export function exportReadingNotes(name: string, highlights: SavedHighlight[], bookmarks: SavedBookmark[]) {
  const quote = (text: string) => text.split('\n').map(line => `> ${line}`).join('\n')
  return [`# ${name}`, '', '## Bookmarks', ...bookmarks.map(b => `- Page ${b.page}: ${b.title}`), '', '## Highlights & notes',
    ...highlights.flatMap(h => [`### Page ${h.page}`, quote(h.text), '', h.note || '', ''])].join('\n')
}
