<script setup lang="ts">
import { computePosition, flip, shift, offset, autoUpdate } from '@floating-ui/dom'
const { user, logout } = useAuth()
const saveOpen = ref(false)
const saveAction = ref('lưu tài liệu')
function requestSave(action = 'lưu tài liệu') {
  if (user.value) { void persistence.sync(); return }
  saveAction.value = action; saveOpen.value = true
}
const signingOut = ref(false)
const signOutError = ref('')
async function signOut() {
  if (signingOut.value) return
  signingOut.value = true
  signOutError.value = ''
  try { await logout(); await navigateTo('/reader') }
  catch { signOutError.value = 'Logout failed. Please try again.' }
  finally { signingOut.value = false }
}
const { processText, cached: aiCached, clearCache: clearAiCache } = usePdfTranslation()
const scroller = ref<HTMLElement | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)
const popupEl = ref<HTMLElement | null>(null)
const { fileName, fileSize, pageCount, activePage, zoom, busy, indexing, indexedCount, limited, error: pdfError, documentId, pages, dimensions, setHost, loadFile, goToPage, setZoom, fitWidth, trackPage } = usePdfDocument(scroller)
const persistence = useReadingPersistence({ page: activePage, zoom, count: pageCount, load: loadFile, navigate: page => goToPage(page, 'auto'), setZoom })
const { messages: chatMessages, actions: savedActions, highlights, status: saveStatus, error: saveError, saving: isSaving } = persistence
const historyOpen = ref(false)
const highlightsOpen = ref(false)
const selectionRects = ref<import('~/types/library').HighlightRect[]>([])
const selectionPage = ref(1)
function markSelection() {
  if (!selectionRects.value.length) return
  if (highlights.value.length >= 200) { aiError.value = 'Tối đa 200 đánh dấu mỗi tài liệu.'; return }
  highlights.value.push({ id: crypto.randomUUID(), page: selectionPage.value, text: selectedText.value, rects: selectionRects.value.map(r => ({ ...r })) })
  window.getSelection()?.removeAllRanges()
  closePopup()
}

const speech = useTextSpeech()
const { supported: speechSupported, speaking, paused, error: speechError, language: speechLanguage, rate: speechRate } = speech
const speechOpen = ref(false)
const filesOpen = ref(false)
const libraryApi = useLibrary()
const savedFiles = ref<import('~/types/library').LibraryItem[]>([])
const filesLoading = ref(false)
const filesError = ref('')
async function showFiles() {
  filesOpen.value = true; filesLoading.value = true; filesError.value = ''
  try { savedFiles.value = await libraryApi.list() }
  catch (e: any) { filesError.value = e?.data?.message || 'Không tải được danh sách. Hãy thử lại.' }
  finally { filesLoading.value = false }
}
async function openSavedFile(id: string) {
  if (isSaving.value || persistence.restoring.value) return
  await persistence.sync()
  if (saveError.value) return
  speech.stop()
  await persistence.restore(id)
  if (!saveError.value) filesOpen.value = false
}
function readPage() { speech.read(pages.value.find(p => p.page === activePage.value)?.text || '') }
watch(documentId, () => speech.stop())
watch(activePage, () => speech.stop())

const colorMode = useColorMode()
const leftOpen = ref(false)
const rightOpen = ref(true)
const pageSearch = ref('')
const jumpPage = ref(1)
const dragging = ref(false)
const openingFileName = ref('')
const selectedText = ref('')
const chatSelection = ref('')
const popupVisible = ref(false)
const popupX = ref(0)
const popupY = ref(0)
const action = ref<'translate' | 'explain' | 'summarize'>('translate')
const result = ref('')
const aiError = ref('')
const aiPending = ref(false)
const aiRetryable = ref(false)
const aiRetryAt = ref(0)
const aiNow = ref(Date.now())
let retryClock: ReturnType<typeof setInterval> | undefined
onMounted(() => { retryClock = setInterval(() => { aiNow.value = Date.now() }, 1000) })
onBeforeUnmount(() => clearInterval(retryClock))
const copied = ref(false)
let anchor: DOMRect | null = null
let cleanup: (() => void) | undefined
let selectionTimer: ReturnType<typeof setTimeout> | undefined
let controller: AbortController | undefined
let requestVersion = 0
const pageList = computed(() => Array.from({ length: pageCount.value }, (_, i) => i + 1).filter(n => !pageSearch.value || String(n).includes(pageSearch.value)))
const progress = computed(() => pageCount.value ? Math.round(activePage.value / pageCount.value * 100) : 0)
const actionLabel = computed(() => ({ translate: 'Bản dịch tiếng Việt', explain: 'Giải thích đoạn văn', summarize: 'Tóm tắt ý chính' }[action.value]))
watch(activePage, n => { jumpPage.value = n })
watch(documentId, () => { clearAiCache(); closePopup(); chatSelection.value = ''; leftOpen.value = false; pageSearch.value = '' })
watch(zoom, closePopup)
function toggleTheme() { colorMode.preference = colorMode.value === 'dark' ? 'light' : 'dark' }
async function openFile(file?: File) {
  if (!file || busy.value || persistence.saving.value) return
  persistence.prepare(file)
  openingFileName.value = file.name
  await loadFile(file)
  if (pageCount.value && !pdfError.value) await persistence.opened(file)
}
function pickFile(event: Event) { const target = event.target as HTMLInputElement; void openFile(target.files?.[0]); target.value = '' }
function drop(event: DragEvent) { dragging.value = false; void openFile(event.dataTransfer?.files?.[0]) }
function leaveDropArea(event: DragEvent) {
  if (!(event.relatedTarget instanceof Node) || !scroller.value?.contains(event.relatedTarget)) dragging.value = false
}
function navigate(n: number) { closePopup(); goToPage(n); leftOpen.value = false; if (window.innerWidth < 1280) rightOpen.value = false }
function closePopup() {
  popupVisible.value = false; selectionRects.value = []; selectedText.value = ''; result.value = ''; aiError.value = ''; aiPending.value = false
  ++requestVersion; controller?.abort(); cleanup?.(); cleanup = undefined; anchor = null
  clearTimeout(selectionTimer)
}
async function positionPopup() {
  if (!popupEl.value || !anchor) return
  const rect = anchor
  const position = await computePosition({ getBoundingClientRect: () => rect }, popupEl.value, { strategy: 'fixed', placement: 'top', middleware: [offset(12), flip(), shift({ padding: 12 })] })
  if (!popupVisible.value) return
  popupX.value = position.x; popupY.value = position.y
}
function handleSelection(event?: Event) {
  if (event instanceof KeyboardEvent && event.key === 'Escape') return
  clearTimeout(selectionTimer)
  selectionTimer = setTimeout(async () => {
    const selection = window.getSelection()
    const text = selection?.toString().trim() || ''
    if (!selection || !text || !selection.rangeCount || !selection.anchorNode || !selection.focusNode) return
    const start = selection.anchorNode.parentElement?.closest('.textLayer')
    const end = selection.focusNode.parentElement?.closest('.textLayer')
    if (!start || !end || !scroller.value?.contains(start) || !scroller.value?.contains(end)) return
    if (text.length > 5000) { pdfError.value = 'Chọn tối đa 5000 ký tự mỗi lần.'; return }
    const range = selection.getRangeAt(0)
    const rect = range.getBoundingClientRect()
    const host = start.closest<HTMLElement>('.pdf-page')
    const samePage = host && host === end.closest('.pdf-page')
    const pageRect = host?.getBoundingClientRect()
    const marks = samePage && pageRect ? Array.from(range.getClientRects()).filter(r => r.width > 0 && r.height > 0).slice(0, 200).map(r => {
      const x = Math.max(0, Math.min(1, (r.left - pageRect.left) / pageRect.width))
      const y = Math.max(0, Math.min(1, (r.top - pageRect.top) / pageRect.height))
      return { x, y, width: Math.min(1 - x, r.width / pageRect.width), height: Math.min(1 - y, r.height / pageRect.height) }
    }).filter(r => r.width > 0 && r.height > 0) : []
    if (!rect.width && !rect.height) return
    closePopup()
    selectionRects.value = marks; selectionPage.value = Number(host?.dataset.page || activePage.value); selectedText.value = text; anchor = rect; popupVisible.value = true; copied.value = false
    await nextTick(); await positionPopup()
    if (popupEl.value) cleanup = autoUpdate({ getBoundingClientRect: () => rect }, popupEl.value, positionPopup)
  }, 160)
}
async function runAction(mode: typeof action.value) {
  if (aiPending.value || Date.now() < aiRetryAt.value) return
  controller?.abort()
  const id = ++requestVersion
  controller = new AbortController()
  action.value = mode; aiPending.value = true; aiError.value = ''; aiRetryable.value = false; result.value = ''
  try {
    const response = await processText(selectedText.value, mode, controller.signal)
    if (id === requestVersion) {
      result.value = response
      if (!savedActions.value.some(a => a.mode === mode && a.source === selectedText.value && a.result === response && a.page === activePage.value)) savedActions.value.push({ mode, source: selectedText.value, result: response, page: activePage.value })
    }
  } catch (err: any) {
    if (id === requestVersion) {
      const data = err?.data?.data || err?.data || {}
      aiError.value = data.message || err?.message || 'Không thể nhận phản hồi AI. Hãy thử lại.'
      aiRetryable.value = data.retryable !== false && data.kind !== 'quota_exceeded'
      aiRetryAt.value = Date.now() + (Number(data.retryAfter) || 0) * 1000
    }
  }
  finally { if (id === requestVersion) { aiPending.value = false; await nextTick(); await positionPopup() } }
}
function askSelection() { chatSelection.value = selectedText.value; rightOpen.value = true; closePopup() }
async function copyText() {
  try { await navigator.clipboard.writeText(result.value || selectedText.value); copied.value = true }
  catch { aiError.value = 'Trình duyệt không cho phép sao chép. Hãy dùng Ctrl+C.' }
}
function pointerDown(event: PointerEvent) { if (!popupEl.value?.contains(event.target as Node)) closePopup() }
function keydown(event: KeyboardEvent) { if (event.key === 'Escape') { closePopup(); leftOpen.value = false; if (window.innerWidth < 1280) rightOpen.value = false } }
onMounted(() => {
  document.addEventListener('pointerdown', pointerDown)
  document.addEventListener('keydown', keydown)
  scroller.value?.addEventListener('scroll', closePopup, { passive: true })
  if (window.innerWidth < 1280) rightOpen.value = false
})
onBeforeUnmount(() => {
  closePopup()
  document.removeEventListener('pointerdown', pointerDown)
  document.removeEventListener('keydown', keydown)
  scroller.value?.removeEventListener('scroll', closePopup)
})
</script>

<template>
  <div class="workspace flex flex-col bg-white text-slate-800 dark:bg-slate-950 dark:text-slate-100">
    <UModal v-model="highlightsOpen"><section class="pf-card max-h-[80dvh] overflow-auto"><div class="mb-4 flex items-center justify-between"><h2 class="text-xl font-bold">Đoạn văn đã đánh dấu</h2><UButton icon="i-lucide-x" color="gray" variant="ghost" title="Đóng danh sách đánh dấu" aria-label="Đóng danh sách đánh dấu" @click="highlightsOpen = false" /></div><p v-if="!highlights.length" class="pf-muted text-sm">Bôi đen đoạn văn trong một trang và chọn Đánh dấu.</p><article v-for="mark in highlights" :key="mark.id" class="mb-3 rounded-xl border border-yellow-400/30 p-3"><p class="mb-2 text-xs text-yellow-500">Trang {{ mark.page }}</p><p class="whitespace-pre-wrap text-sm">{{ mark.text }}</p><div class="mt-3 flex gap-2"><UButton size="xs" color="gray" @click="navigate(mark.page); highlightsOpen = false">Đến đoạn văn</UButton><UButton size="xs" color="gray" icon="i-lucide-trash-2" @click="highlights = highlights.filter(h => h.id !== mark.id)">Xóa đánh dấu</UButton></div></article></section></UModal>
    <UModal v-model="historyOpen"><section class="pf-card max-h-[80dvh] overflow-auto"><div class="mb-5 flex items-center justify-between"><h2 class="text-xl font-bold">Lịch sử AI</h2><UButton icon="i-lucide-x" color="gray" variant="ghost" aria-label="Đóng lịch sử" @click="historyOpen = false" title="Đóng lịch sử" /></div><p v-if="!savedActions.length" class="pf-muted text-sm">Chưa có bản dịch hoặc giải thích nào.</p><article v-for="(entry, i) in savedActions" :key="i" class="mb-5 border-b border-[var(--pf-border)] pb-5"><p class="pf-link text-xs font-semibold">{{ entry.mode }} · Trang {{ entry.page }}</p><blockquote class="pf-muted mt-2 whitespace-pre-wrap break-words text-xs">{{ entry.source }}</blockquote><p class="mt-3 whitespace-pre-wrap break-words text-sm leading-7">{{ entry.result }}</p></article></section></UModal>
    <p v-if="saveError" role="alert" class="border-b border-rose-500/20 bg-rose-500/10 px-4 py-2 text-xs text-rose-500">{{ saveError }}</p>
    <UModal v-model="filesOpen"><section class="pf-card max-h-[80dvh] overflow-auto"><div class="mb-4 flex items-center justify-between"><h2 class="text-xl font-bold">Mở tài liệu đã lưu</h2><UButton icon="i-lucide-x" title="Đóng danh sách tài liệu" aria-label="Đóng danh sách tài liệu" color="gray" variant="ghost" @click="filesOpen = false" /></div><p v-if="filesLoading" role="status">Đang tải danh sách…</p><div v-else-if="filesError"><p role="alert" class="text-rose-400">{{ filesError }}</p><UButton class="mt-3" @click="showFiles">Thử lại</UButton></div><p v-else-if="!savedFiles.length" class="pf-muted">Chưa có tài liệu đã lưu. Hãy mở PDF để bắt đầu.</p><button v-for="file in savedFiles" :key="file.id" :disabled="isSaving || persistence.restoring.value" class="mb-2 block w-full rounded-xl border border-[var(--pf-border)] p-4 text-left hover:bg-violet-500/10 disabled:opacity-50" :aria-current="persistence.id.value === file.id ? 'true' : undefined" @click="openSavedFile(file.id)"><span class="block truncate font-semibold">{{ file.name }}</span><span class="pf-muted text-xs">Đọc tiếp trang {{ file.page }} · {{ (file.size / 1024 / 1024).toFixed(1) }} MB</span></button><p v-if="saveError" role="alert" class="text-rose-400">{{ saveError }}</p></section></UModal>
    <UModal v-model="speechOpen"><section class="pf-card space-y-4"><div class="flex items-center justify-between"><h2 class="text-xl font-bold">Đọc thành tiếng</h2><UButton icon="i-lucide-x" title="Đóng bảng đọc thành tiếng" aria-label="Đóng bảng đọc thành tiếng" color="gray" variant="ghost" @click="speechOpen = false" /></div><p class="pf-muted text-sm">Đọc trang hiện tại hoặc bôi đen đoạn văn và chọn Đọc. Giọng đọc phụ thuộc trình duyệt và thiết bị.</p><label class="block text-sm">Ngôn ngữ<select v-model="speechLanguage" :disabled="speaking" class="pf-input"><option value="vi-VN">Tiếng Việt</option><option value="en-US">English</option></select></label><label class="block text-sm">Tốc độ {{ speechRate }}×<input v-model.number="speechRate" :disabled="speaking" type="range" min="0.5" max="2" step="0.25" class="mt-2 w-full" /></label><div class="flex flex-wrap gap-2"><UButton :disabled="!speechSupported || !pageCount || indexing" icon="i-lucide-volume-2" @click="readPage">Đọc trang {{ activePage }}</UButton><UButton v-if="speaking" color="gray" @click="speech.togglePause">{{ paused ? 'Tiếp tục' : 'Tạm dừng' }}</UButton><UButton v-if="speaking" color="gray" @click="speech.stop">Dừng đọc</UButton></div><p v-if="!speechSupported" role="status" class="pf-muted text-sm">Tr?nh duy?t ch?a h? tr? Đọc thành tiếng.</p><p v-if="speechError" role="alert" class="text-sm text-rose-400">{{ speechError }}</p></section></UModal>
    <SaveInvitation :open="saveOpen" :authenticated="!!user" :action="saveAction" @close="saveOpen = false" />
    <input ref="fileInput" type="file" accept="application/pdf,.pdf" class="hidden" @change="pickFile" />
    <header class="panel z-30 flex h-16 shrink-0 items-center justify-between gap-3 border-b px-4 lg:px-6">
      <div class="flex min-w-0 items-center gap-3"><UButton icon="i-lucide-panel-left" color="gray" variant="ghost" class="lg:hidden" aria-label="Mở danh sách trang" @click="leftOpen = !leftOpen" title="Mở danh sách trang" /><NuxtLink to="/" class="flex shrink-0 items-center gap-2 text-lg font-bold tracking-tight" aria-label="PaperFlow"><img src="/favicon.svg" width="34" height="34" alt="" /><span class="hidden sm:inline">PaperFlow</span></NuxtLink><span class="hidden border-l border-slate-200 pl-4 text-xs text-slate-400 dark:border-slate-700 md:block">Không gian đọc thông minh</span></div>
      <div class="flex items-center gap-1 sm:gap-2"><UBadge v-if="!user" color="primary" variant="soft" class="hidden sm:inline-flex">Guest Session</UBadge><span v-else class="hidden max-w-28 truncate text-xs text-slate-500 sm:inline" :title="user.email">{{ user.displayName }}</span><NuxtLink v-if="!user" to="/login" class="text-xs text-violet-500">Log in</NuxtLink><NuxtLink v-else to="/library" class="hidden text-xs text-violet-500 sm:inline">My Library</NuxtLink><NuxtLink v-if="user?.role === 'Admin'" to="/admin" class="hidden text-xs font-medium text-violet-500 sm:inline">Admin</NuxtLink><UButton v-if="user" icon="i-lucide-log-out" color="gray" variant="ghost" aria-label="Logout" :loading="signingOut" @click="signOut" title="Đăng xuất tài khoản" /><UButton :icon="colorMode.value === 'dark' ? 'i-lucide-sun' : 'i-lucide-moon'" color="gray" variant="ghost" aria-label="Đổi giao diện sáng tối" @click="toggleTheme" title="Đổi giao diện sáng tối" /><UButton icon="i-lucide-messages-square" color="gray" variant="ghost" aria-label="Mở hoặc đóng trợ lý AI" :aria-expanded="rightOpen" @click="rightOpen = !rightOpen" title="Mở hoặc đóng trợ lý AI" /><UButton icon="i-lucide-volume-2" color="gray" variant="ghost" aria-label="Đọc thành tiếng" @click="speechOpen = true" title="Đọc thành tiếng" /><UButton icon="i-lucide-highlighter" color="gray" variant="ghost" class="hidden sm:inline-flex" title="Xem các đoạn đã đánh dấu" aria-label="Xem các đoạn đã đánh dấu" @click="highlightsOpen = true" /><UButton icon="i-lucide-history" color="gray" variant="ghost" aria-label="Lịch sử AI" @click="historyOpen = true" title="Lịch sử AI" /><UButton v-if="user" @click="showFiles" icon="i-lucide-folder-heart" color="gray" variant="ghost" class="sm:hidden" aria-label="My Library" title="Mở danh sách PDF đã lưu" /><UButton :class="user ? 'hidden sm:inline-flex' : ''" icon="i-lucide-cloud-upload" color="gray" variant="ghost" aria-label="Save to Library" @click="requestSave()" title="Lưu vào tài khoản" /><UButton class="hidden sm:inline-flex" icon="i-lucide-refresh-cw" color="gray" variant="ghost" aria-label="Sync" @click="requestSave('đồng bộ dữ liệu')" title="Đồng bộ dữ liệu" /><UButton icon="i-lucide-plus" size="sm" :disabled="busy || isSaving" @click="fileInput?.click()"><span class="hidden sm:inline">Mở PDF</span></UButton></div>
    </header>
    <div class="relative flex min-h-0 flex-1">
      <button v-if="leftOpen" class="fixed inset-0 top-16 z-30 bg-slate-950/40 lg:hidden" aria-label="Đóng danh sách trang" @click="leftOpen = false" />
      <aside class="panel z-40 flex w-60 shrink-0 flex-col border-r max-lg:absolute max-lg:inset-y-0 max-lg:left-0" :class="leftOpen ? 'flex' : 'max-lg:hidden'" aria-label="Tài liệu và danh sách trang">
        <div class="p-5"><div class="mb-5 flex items-center justify-between"><span class="text-[10px] font-semibold uppercase tracking-[.18em] text-slate-400">Tài liệu của bạn</span><UButton v-if="user" icon="i-lucide-folder-open" title="Mở danh sách PDF đã lưu" aria-label="Mở danh sách PDF đã lưu" color="gray" variant="ghost" size="xs" @click="showFiles" /></div><button class="flex w-full gap-3 rounded-xl border border-violet-100 bg-violet-50 p-3 text-left dark:border-violet-500/20 dark:bg-violet-500/10" @click="fileInput?.click()"><UIcon name="i-lucide-file-text" class="mt-1 size-5 shrink-0 text-violet-500" /><div class="min-w-0"><p class="truncate text-xs font-semibold text-violet-700 dark:text-violet-300">{{ fileName || 'Mở tài liệu PDF' }}</p><p class="mt-1 text-[10px] text-slate-500">{{ pageCount ? `${pageCount} trang · ${fileSize}` : 'Bắt đầu một phiên đọc mới' }}</p></div></button></div>
        <UButton class="mx-5 mb-3" icon="i-lucide-highlighter" color="gray" variant="soft" size="xs" @click="highlightsOpen = true">Đánh dấu ({{ highlights.length }})</UButton><div class="border-b border-slate-200 px-5 pb-4 dark:border-slate-800"><div class="mb-3 flex items-center gap-2 text-xs font-semibold"><UIcon name="i-lucide-layers" class="size-4" />Danh sách trang<span class="ml-auto text-slate-400">{{ pageCount }}</span></div><UInput v-if="pageCount" v-model="pageSearch" icon="i-lucide-search" size="xs" placeholder="Tìm số trang…" aria-label="Tìm số trang" /></div>
        <nav class="min-h-0 flex-1 overflow-y-auto p-3"><div v-if="!pageCount" class="px-3 py-8 text-center text-xs leading-6 text-slate-400">Các trang tài liệu sẽ<br />xuất hiện tại đây.</div><button v-for="n in pageList" :key="n" class="mb-1 flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-xs transition" :class="activePage === n ? 'bg-violet-50 font-semibold text-violet-600 dark:bg-violet-500/15 dark:text-violet-300' : 'text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-800'" :aria-current="activePage === n ? 'page' : undefined" @click="navigate(n)"><span class="flex h-9 w-7 shrink-0 items-center justify-center rounded border border-current/15 bg-white text-[10px] dark:bg-slate-900">{{ n }}</span><span>Trang {{ n }}</span><UIcon v-if="activePage === n" name="i-lucide-chevron-right" class="ml-auto size-3" /></button><p v-if="pageCount && !pageList.length" class="p-3 text-xs text-slate-400">Không tìm thấy trang.</p></nav>
        <div class="border-t border-slate-200 p-4 dark:border-slate-800"><div class="flex items-center justify-between text-[10px] text-slate-400"><span>Tiến độ đọc</span><span>{{ progress }}%</span></div><div class="mt-2 h-1 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800"><div class="h-full rounded-full bg-violet-500 transition-all" :style="{ width: progress + '%' }" /></div><p class="mt-3 flex items-center gap-1.5 text-[10px] text-slate-400"><UIcon name="i-lucide-shield-check" class="size-3" />PDF được đọc trên thiết bị</p></div>
      </aside>
      <main class="flex min-w-0 flex-1 flex-col">
        <div class="panel flex min-h-16 shrink-0 flex-wrap items-center justify-between gap-2 border-b px-4 py-2 lg:px-6"><div class="flex min-w-0 items-center gap-2"><UIcon name="i-lucide-file-text" class="size-4 shrink-0 text-slate-400" /><span class="max-w-36 truncate text-xs font-medium md:max-w-60">{{ fileName || 'Không gian đọc' }}</span></div><div class="flex items-center gap-1"><template v-if="pageCount"><UButton icon="i-lucide-chevron-left" color="gray" variant="ghost" size="xs" :disabled="activePage <= 1" aria-label="Trang trước" @click="goToPage(activePage - 1)" title="Trang trước" /><form class="flex items-center gap-1 text-xs text-slate-400" @submit.prevent="navigate(Number(jumpPage))"><input v-model="jumpPage" type="number" min="1" :max="pageCount" aria-label="Đến trang" class="h-7 w-10 rounded-md border border-slate-200 bg-transparent px-1 text-center text-slate-700 dark:border-slate-700 dark:text-slate-200" /><span>/ {{ pageCount }}</span></form><UButton icon="i-lucide-chevron-right" color="gray" variant="ghost" size="xs" :disabled="activePage >= pageCount" aria-label="Trang sau" @click="goToPage(activePage + 1)" /><span class="mx-2 h-4 border-l border-slate-200 dark:border-slate-700" /></template><UButton icon="i-lucide-minus" color="gray" variant="ghost" size="xs" :disabled="!pageCount || zoom <= .35" aria-label="Thu nhỏ" @click="setZoom(zoom - .1)" title="Thu nhỏ" /><span class="w-11 text-center text-xs tabular-nums text-slate-500">{{ Math.round(zoom * 100) }}%</span><UButton icon="i-lucide-plus" color="gray" variant="ghost" size="xs" :disabled="!pageCount || zoom >= 2.5" aria-label="Phóng to" @click="setZoom(zoom + .1)" /><UButton icon="i-lucide-scan" color="gray" variant="ghost" size="xs" :disabled="!pageCount" aria-label="Vừa chiều rộng" @click="fitWidth" title="Vừa chiều rộng" /></div></div>
        <p v-if="signOutError" role="alert" class="px-5 py-2 text-xs text-red-500">{{ signOutError }}</p>
        <div v-if="pdfError" role="alert" class="flex items-center justify-between gap-3 bg-red-50 px-5 py-3 text-xs text-red-600 dark:bg-red-500/10"><span>{{ pdfError }}</span><button aria-label="Đóng thông báo" @click="pdfError = ''"><UIcon name="i-lucide-x" class="size-4" /></button></div>
        <div ref="scroller" class="reader-surface relative min-h-0 flex-1 overflow-auto overscroll-contain" :class="dragging ? 'ring-4 ring-inset ring-violet-400' : ''" @scroll.passive="trackPage" @dragover.prevent="dragging = true" @dragleave="leaveDropArea" @drop.prevent="drop" @mouseup="handleSelection" @keyup="handleSelection">
          <div v-if="!pageCount" class="flex min-h-full items-center justify-center p-5 sm:p-10">
            <PdfUpload :busy="busy" :dragging="dragging" :file-name="openingFileName" @select="fileInput?.click()" />
          </div>
          <div v-else class="flex w-max min-w-full flex-col items-center gap-6 p-6"><div v-for="(size, i) in dimensions" :key="`${documentId}-${i}`"><div class="relative"><div :ref="el => setHost(i + 1, el)" :data-page="i + 1" class="pdf-page overflow-hidden rounded-sm" :style="{ width: size.width * zoom + 'px', height: size.height * zoom + 'px' }" /><div class="pointer-events-none absolute inset-0 z-[2] overflow-hidden" aria-hidden="true"><template v-for="mark in highlights.filter(h => h.page === i + 1)" :key="mark.id"><span v-for="(rect, n) in mark.rects" :key="n" class="absolute bg-yellow-400/35 mix-blend-multiply" :style="{ left: rect.x * 100 + '%', top: rect.y * 100 + '%', width: rect.width * 100 + '%', height: rect.height * 100 + '%' }" /></template></div></div><p class="mt-2 text-center text-[10px] tabular-nums text-slate-400">{{ i + 1 }} / {{ pageCount }}</p></div></div>
          <div v-if="busy && pageCount" class="absolute inset-0 z-10 flex items-center justify-center bg-white/70 backdrop-blur-sm dark:bg-slate-950/70"><div class="flex items-center gap-3 rounded-xl bg-white px-5 py-4 text-sm shadow-lg dark:bg-slate-800"><UIcon name="i-lucide-loader-circle" class="size-5 animate-spin text-violet-500" />Đang chuẩn bị tài liệu…</div></div>
        </div>
        <footer class="panel flex h-8 shrink-0 items-center justify-between border-t px-4 text-[10px] text-slate-400"><span class="flex items-center gap-1.5"><span class="size-1.5 rounded-full bg-emerald-500" />{{ !user ? 'Guest Session · ' : saveStatus ? saveStatus + ' · ' : 'Member · ' }}{{ indexing ? `Đang đọc trang ${indexedCount}/${pageCount} cho AI…` : 'Cuộn liên tục' }}</span><span>{{ pageCount ? 'Bôi đen văn bản để mở công cụ AI' : 'Không gian của bạn. Nhịp đọc của bạn.' }}</span></footer>
      </main>
      <button v-if="rightOpen" class="fixed inset-0 top-16 z-30 bg-slate-950/40 xl:hidden" aria-label="Đóng trợ lý AI" @click="rightOpen = false" />
      <aside v-show="rightOpen" class="z-40 w-[360px] shrink-0 max-xl:absolute max-xl:inset-y-0 max-xl:right-0 max-xl:max-w-[calc(100vw-24px)]"><AiChat v-model:messages="chatMessages" :pages="pages" :file-name="fileName" :document-id="documentId" :indexing="indexing" :limited="limited" :selection="chatSelection" @close="rightOpen = false" @navigate="navigate" @clear-selection="chatSelection = ''" @request-save="requestSave('lưu lịch sử chat')" /></aside>
    </div>
    <Teleport to="body"><div v-if="popupVisible" ref="popupEl" class="fixed z-50 max-w-[calc(100vw-24px)] rounded-2xl border border-slate-200 bg-white p-2 text-slate-800 shadow-2xl dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100" :style="{ left: popupX + 'px', top: popupY + 'px' }" @pointerdown.stop.prevent>
      <div class="flex flex-wrap items-center gap-1"><UButton icon="i-lucide-highlighter" color="gray" variant="ghost" size="xs" :disabled="!selectionRects.length" title="Đánh dấu đoạn văn trên một trang" @click="markSelection">Đánh dấu</UButton><UButton icon="i-lucide-languages" color="gray" variant="ghost" size="xs" :disabled="aiPending || aiNow < aiRetryAt" :loading="aiPending && action === 'translate'" @click="runAction('translate')">Dịch</UButton><UButton icon="i-lucide-lightbulb" color="gray" variant="ghost" size="xs" :disabled="aiPending || aiNow < aiRetryAt" :loading="aiPending && action === 'explain'" @click="runAction('explain')">Giải thích</UButton><UButton icon="i-lucide-list-filter" color="gray" variant="ghost" size="xs" :disabled="aiPending || aiNow < aiRetryAt" :loading="aiPending && action === 'summarize'" @click="runAction('summarize')">Tóm tắt</UButton><UButton icon="i-lucide-volume-2" color="gray" variant="ghost" size="xs" :disabled="!speechSupported" @click="speech.read(result || selectedText); speechOpen = true">Đọc</UButton><UButton icon="i-lucide-message-circle" variant="soft" size="xs" @click="askSelection">Hỏi AI</UButton><UButton :icon="copied ? 'i-lucide-check' : 'i-lucide-copy'" color="gray" variant="ghost" size="xs" aria-label="Sao chép văn bản" @click="copyText" title="Sao chép văn bản" /><UButton icon="i-lucide-x" color="gray" variant="ghost" size="xs" aria-label="Đóng công cụ AI" @click="closePopup" title="Đóng công cụ AI" /></div>
      <div v-if="aiPending || result || aiError" class="mt-2 w-[420px] max-w-full border-t border-slate-100 p-3 dark:border-slate-800" aria-live="polite"><p class="mb-2 text-xs font-semibold text-violet-500">{{ actionLabel }}<span v-if="aiCached && result" class="ml-2 text-[10px] text-emerald-500">Đã lưu trong phiên</span></p><p v-if="aiPending" class="flex items-center gap-2 text-xs text-slate-400"><UIcon name="i-lucide-loader-circle" class="size-4 animate-spin" />Đang xử lý…</p><p v-else-if="result" class="max-h-64 overflow-y-auto whitespace-pre-wrap text-sm leading-7">{{ result }}</p><p v-if="aiError" role="alert" class="text-xs text-red-500">{{ aiError }}</p><UButton v-if="aiError && aiRetryable" class="mt-2" size="xs" :disabled="aiPending || aiNow < aiRetryAt" @click="runAction(action)">{{ aiNow < aiRetryAt ? `Thử lại sau ${Math.ceil((aiRetryAt - aiNow) / 1000)}s` : 'Thử lại' }}</UButton></div>
    </div></Teleport>
  </div>
</template>
