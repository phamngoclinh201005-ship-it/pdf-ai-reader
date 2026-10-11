import { test } from 'node:test'
import assert from 'node:assert/strict'
import { selectContext } from '../server/utils/document-context.ts'

test('retrieves the relevant page rather than always truncating the start', () => {
  const context = selectContext([{ page: 1, text: 'Introduction '.repeat(100) }, { page: 9, text: 'Quantum entanglement connects particles.' }], 'quantum entanglement', '', 140)
  assert.ok(context.pages.includes(9))
  assert.match(context.text, /\[Trang 9\]/)
})
test('selection ranks its page above unrelated matches', () => {
  const context = selectContext([{ page: 1, text: 'results '.repeat(100) }, { page: 7, text: 'Special passage about neural networks.' }], 'results', 'Special passage', 100)
  assert.deepEqual(context.pages, [7])
})
test('keeps the context within its budget and preserves real page numbers', () => {
  const context = selectContext([{ page: 25, text: 'A'.repeat(1000) }, { page: 100, text: 'B'.repeat(1000) }], 'overview', '', 300)
  assert.ok(context.text.length <= 300)
  assert.deepEqual(context.pages, [25])
})
test('does not create citations for an empty or scanned document', () => {
  assert.deepEqual(selectContext([{ page: 1, text: '  ' }], 'summary'), { text: '', pages: [] })
})
