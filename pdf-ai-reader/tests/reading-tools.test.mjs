import { test } from 'node:test'
import assert from 'node:assert/strict'
import { searchDocument, exportReadingNotes } from '../utils/reading-tools.ts'

test('search finds case-insensitive occurrences with correct source pages and bounded results', () => {
  const pages = [{ page: 3, text: 'Important\nscience and SCIENCE formulas α + β.' }, { page: 8, text: 'Other content' }]
  assert.deepEqual(searchDocument(pages, 'science').map(hit => hit.page), [3, 3])
  assert.equal(searchDocument(pages, 'α + β')[0].page, 3)
  assert.equal(searchDocument(pages, 'missing').length, 0)
  assert.equal(searchDocument(pages, ' ').length, 0)
  assert.equal(searchDocument([{ page: 1, text: 'word '.repeat(200) }], 'word').length, 100)
})
test('export includes source text, personal notes, bookmark names and page references', () => {
  const result = exportReadingNotes('Research.pdf', [{ id: 'h', page: 7, text: 'First line\nSecond line', note: 'My explanation', rects: [] }], [{ id: 'b', page: 9, title: 'Methods' }])
  assert.match(result, /Page 9: Methods/)
  assert.match(result, /### Page 7/)
  assert.match(result, /> First line\n> Second line/)
  assert.match(result, /My explanation/)
})
