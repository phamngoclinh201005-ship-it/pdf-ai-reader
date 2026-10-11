import * as pdfjs from 'pdfjs-dist'
import workerUrl from 'pdfjs-dist/build/pdf.worker.min.mjs?url'
import type { PDFDocumentProxy, RenderTask } from 'pdfjs-dist'
import type { DocumentPage } from '~/types/ai'

export function usePdfDocument(scroller: Ref<HTMLElement | null>) {
  pdfjs.GlobalWorkerOptions.workerSrc = workerUrl
  const fileName = ref('')
  const fileSize = ref('')
  const pageCount = ref(0)
  const activePage = ref(1)
  const zoom = ref(1)
  const busy = ref(false)
  const indexing = ref(false)
  const indexedCount = ref(0)
  const limited = ref(false)
  const error = ref('')
  const documentId = ref(0)
  const pages = shallowRef<DocumentPage[]>([])
  const dimensions = shallowRef<{ width: number; height: number }[]>([])
  const hosts = new Map<number, HTMLElement>()
  const tasks = new Map<number, RenderTask>()
  const rendered = new Set<number>()
  const rendering = new Set<number>()
  const visible = new Set<number>()
  let pdf: PDFDocumentProxy | null = null
  let loadingTask: ReturnType<typeof pdfjs.getDocument> | null = null
  let observer: IntersectionObserver | null = null
  let generation = 0
  let zoomTimer: ReturnType<typeof setTimeout> | undefined
  let resizeObserver: ResizeObserver | undefined
  let fitMode = false

  function setHost(page: number, value: unknown) {
    if (value instanceof HTMLElement) hosts.set(page, value)
    else hosts.delete(page)
  }
  async function render(pageNumber: number, token = generation) {
    const doc = pdf
    const host = hosts.get(pageNumber)
    if (!doc || !host || token !== generation || rendered.has(pageNumber) || rendering.has(pageNumber)) return
    rendering.add(pageNumber)
    let task: RenderTask | undefined
    try {
      const page = await doc.getPage(pageNumber)
      if (token !== generation || !host.isConnected) return
      const viewport = page.getViewport({ scale: zoom.value })
      const canvas = document.createElement('canvas')
      const context = canvas.getContext('2d')
      if (!context) throw new Error('Canvas unavailable')
      const dpr = Math.min(window.devicePixelRatio || 1, 2)
      canvas.width = Math.ceil(viewport.width * dpr)
      canvas.height = Math.ceil(viewport.height * dpr)
      canvas.style.width = viewport.width + 'px'
      canvas.style.height = viewport.height + 'px'
      host.replaceChildren(canvas)
      task = page.render({ canvasContext: context, viewport, transform: [dpr, 0, 0, dpr, 0, 0] })
      tasks.set(pageNumber, task)
      await task.promise
      if (token !== generation) return
      const content = await page.getTextContent()
      if (token !== generation) return
      const layer = document.createElement('div')
      layer.className = 'textLayer'
      layer.style.setProperty('--scale-factor', String(viewport.scale))
      host.appendChild(layer)
      await new pdfjs.TextLayer({ textContentSource: content, container: layer, viewport }).render()
      if (token === generation) {
        if (visible.has(pageNumber)) rendered.add(pageNumber)
        else host.replaceChildren()
      }
    } catch (err: any) {
      if (token === generation && err?.name !== 'RenderingCancelledException') error.value = 'Không render được trang ' + pageNumber + '. Hãy thử mở lại PDF.'
    } finally {
      if (tasks.get(pageNumber) === task) tasks.delete(pageNumber)
      if (token === generation) rendering.delete(pageNumber)
    }
  }
  function observe() {
    observer?.disconnect()
    visible.clear()
    observer = new IntersectionObserver(entries => {
      for (const entry of entries) {
        const n = Number((entry.target as HTMLElement).dataset.page)
        if (entry.isIntersecting) { visible.add(n); void render(n) }
        else {
          visible.delete(n)
          // Release offscreen canvas memory, keep page placeholders for continuous scrolling.
          if (rendered.has(n)) { hosts.get(n)?.replaceChildren(); rendered.delete(n) }
        }
      }
    }, { root: scroller.value, rootMargin: '700px 0px' })
    hosts.forEach(host => observer!.observe(host))
  }
  function trackPage() {
    const root = scroller.value
    if (!root) return
    const line = root.getBoundingClientRect().top + Math.min(root.clientHeight * .35, 200)
    let best = Infinity
    for (const n of visible) {
      const rect = hosts.get(n)?.getBoundingClientRect()
      if (!rect) continue
      const distance = rect.top <= line && rect.bottom >= line ? 0 : Math.abs(rect.top - line)
      if (distance < best) { best = distance; activePage.value = n }
    }
  }
  async function extract(doc: PDFDocumentProxy, id: number) {
    indexing.value = true
    const result: DocumentPage[] = []
    let budget = 500000
    try {
      for (let n = 1; n <= doc.numPages; n++) {
        if (id !== documentId.value) return
        if (budget <= 0) { limited.value = true; break }
        const page = await doc.getPage(n)
        const content = await page.getTextContent()
        if (id !== documentId.value) return
        const fullText = content.items.map(item => 'str' in item ? item.str + (item.hasEOL ? '\n' : ' ') : '').join('')
        const text = fullText.slice(0, Math.min(budget, 50000))
        if (text.length < fullText.length) limited.value = true
        result.push({ page: n, text })
        budget -= text.length
        indexedCount.value = n
        // Yield to painting and user interaction during long document indexing.
        if (n % 5 === 0) await new Promise(resolve => setTimeout(resolve, 0))
      }
      if (id === documentId.value) pages.value = result
    } catch {
      if (id === documentId.value) { pages.value = result; limited.value = true; error.value = 'Một số trang không thể trích xuất văn bản. Chat dùng các trang đã đọc được.' }
    } finally { if (id === documentId.value) indexing.value = false }
  }
  async function loadFile(file?: File) {
    if (!file) return
    if (!file.name.toLowerCase().endsWith('.pdf') && file.type !== 'application/pdf') { error.value = 'Vui lòng chọn file PDF.'; return }
    if (file.size > 40 * 1024 * 1024) { error.value = 'Giới hạn kích thước PDF là 40 MB.'; return }
    const id = ++documentId.value
    ++generation
    clearTimeout(zoomTimer)
    observer?.disconnect()
    tasks.forEach(task => task.cancel())
    tasks.clear(); rendered.clear(); rendering.clear(); visible.clear()
    busy.value = true; error.value = ''; pageCount.value = 0; pages.value = []; dimensions.value = []
    indexedCount.value = 0; indexing.value = false; limited.value = false
    fileName.value = ''; activePage.value = 1
    const previous = pdf
    pdf = null
    try {
      await loadingTask?.destroy()
      loadingTask = null
      await previous?.destroy()
      if (id !== documentId.value) return
      const bytes = new Uint8Array(await file.arrayBuffer())
      if (id !== documentId.value) return
      loadingTask = pdfjs.getDocument({ data: bytes })
      const doc = await loadingTask.promise
      if (id !== documentId.value) { await doc.destroy(); return }
      if (doc.numPages > 2000) { await doc.destroy(); throw new Error('Too many pages') }
      pdf = doc
      const first = await doc.getPage(1)
      if (id !== documentId.value) return
      const viewport = first.getViewport({ scale: 1 })
      dimensions.value = Array.from({ length: doc.numPages }, () => ({ width: viewport.width, height: viewport.height }))
      fileName.value = file.name; fileSize.value = (file.size / 1024 / 1024).toFixed(1) + ' MB'
      pageCount.value = doc.numPages
      zoom.value = 1; fitMode = true
      await nextTick()
      fitWidth()
      await nextTick()
      observe()
      void extract(doc, id)
      // Preserve each page's true size, including PDFs with mixed orientations.
      void (async () => {
        for (let n = 2; n <= doc.numPages; n++) {
          if (id !== documentId.value) return
          try {
            const page = await doc.getPage(n)
            if (id !== documentId.value) return
            const size = page.getViewport({ scale: 1 })
            const next = [...dimensions.value]; next[n - 1] = { width: size.width, height: size.height }; dimensions.value = next
          } catch { return }
        }
      })()
    } catch {
      if (id === documentId.value) { error.value = 'Không mở được PDF. Kiểm tra file, mật khẩu hoặc giới hạn 2000 trang.'; pageCount.value = 0 }
    } finally { if (id === documentId.value) busy.value = false }
  }
  function goToPage(n: number, behavior: ScrollBehavior = 'smooth') {
    if (n < 1 || n > pageCount.value) return
    activePage.value = n
    hosts.get(n)?.scrollIntoView({ behavior, block: 'start', inline: 'center' })
  }
  function setZoom(value: number, fitting = false) {
    fitMode = fitting
    const next = Math.max(.35, Math.min(2.5, Math.round(value * 100) / 100))
    if (next === zoom.value) return
    zoom.value = next
    ++generation
    tasks.forEach(task => task.cancel()); tasks.clear(); rendered.clear(); rendering.clear()
    hosts.forEach(host => host.replaceChildren())
    clearTimeout(zoomTimer)
    zoomTimer = setTimeout(async () => {
      await nextTick()
      observe()
      hosts.get(activePage.value)?.scrollIntoView({ block: 'start', inline: 'center' })
    }, 140)
  }
  function fitWidth() {
    const width = dimensions.value[activePage.value - 1]?.width
    if (width && scroller.value) setZoom((scroller.value.clientWidth - 48) / width, true)
  }
  onMounted(() => {
    resizeObserver = new ResizeObserver(() => { if (fitMode) fitWidth() })
    if (scroller.value) resizeObserver.observe(scroller.value)
  })
  onBeforeUnmount(() => {
    ++documentId.value; ++generation; clearTimeout(zoomTimer)
    observer?.disconnect(); resizeObserver?.disconnect()
    tasks.forEach(task => task.cancel())
    void loadingTask?.destroy(); void pdf?.destroy()
  })
  return { fileName, fileSize, pageCount, activePage, zoom, busy, indexing, indexedCount, limited, error, documentId, pages, dimensions, setHost, loadFile, goToPage, setZoom, fitWidth, trackPage }
}
