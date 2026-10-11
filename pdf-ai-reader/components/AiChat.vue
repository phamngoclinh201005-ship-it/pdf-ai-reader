<script setup lang="ts">
import type { DocumentPage } from '~/types/ai'
import type { SavedMessage } from '~/types/library'
const { askPdf } = usePdfChat()
const props = defineProps<{ pages: DocumentPage[]; fileName: string; documentId: number; indexing: boolean; limited: boolean; selection: string }>()
const emit = defineEmits<{ close: []; navigate: [page: number]; clearSelection: []; requestSave: [] }>()
const messages = defineModel<SavedMessage[]>('messages', { default: () => [] })
const question = ref('')
const pending = ref(false)
const error = ref('')
const thread = ref<HTMLElement | null>(null)
let controller: AbortController | undefined
let version = 0
const ready = computed(() => !!props.pages.some(p => p.text.trim()) && !props.indexing)
const prompts = ['Tóm tắt các ý chính của tài liệu', 'Giải thích các khái niệm quan trọng', 'Kết luận và điểm cần lưu ý là gì?']
function reset() {
  ++version
  controller?.abort()
  messages.value = []
  pending.value = false
  error.value = ''
  question.value = ''
}
watch(() => props.documentId, reset)
async function scrollBottom() { await nextTick(); thread.value?.scrollTo({ top: thread.value.scrollHeight, behavior: 'smooth' }) }
async function send(value = question.value) {
  const text = value.trim()
  if (!text || !ready.value || pending.value) return
  if (text.length > 4000) { error.value = 'Câu hỏi tối đa 4000 ký tự.'; return }
  const history = messages.value.slice(-12).map(m => ({ role: m.role, text: m.text.slice(0, 12000) }))
  messages.value.push({ role: 'user', text })
  question.value = ''
  pending.value = true
  error.value = ''
  const id = ++version
  controller = new AbortController()
  await scrollBottom()
  try {
    const response = await askPdf(
      { question: text, pages: props.pages, history, selection: props.selection },
      controller.signal,
    )
    if (id !== version) return
    const validPages = new Set(props.pages.map(p => p.page))
    const citations = [...new Set([...response.result.matchAll(/\[Trang\s+(\d+)\]/gi)].map(m => Number(m[1])))].filter(p => validPages.has(p))
    messages.value.push({ role: 'assistant', text: response.result, citations })
  } catch (err: any) {
    if (id === version) {
      messages.value.pop()
      question.value = text
      error.value = err?.data?.data?.message || 'Không thể gửi câu hỏi. Vui lòng thử lại.'
    }
  } finally {
    if (id === version) { pending.value = false; await scrollBottom() }
  }
}
function onKey(event: KeyboardEvent) {
  if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) { event.preventDefault(); void send() }
}
onBeforeUnmount(() => { ++version; controller?.abort() })
</script>

<template>
  <section class="panel flex h-full min-h-0 flex-col border-l" aria-label="Chat với PDF">
    <header class="flex h-16 shrink-0 items-center justify-between border-b border-slate-200 px-5 dark:border-slate-800">
      <div class="flex items-center gap-3"><div class="flex size-9 items-center justify-center rounded-xl bg-violet-100 text-violet-600 dark:bg-violet-500/15 dark:text-violet-400"><UIcon name="i-lucide-sparkles" class="size-5" /></div><div><h2 class="text-sm font-semibold">Trợ lý AI</h2><p class="text-[11px] text-slate-500">Powered by Gemini</p></div></div>
      <div class="flex"><UButton icon="i-lucide-save" color="gray" variant="ghost" aria-label="Save Chat History" @click="emit('requestSave')" /><UButton icon="i-lucide-rotate-ccw" color="gray" variant="ghost" aria-label="Xóa cuộc trò chuyện" :disabled="!messages.length || pending" @click="reset" /><UButton icon="i-lucide-x" color="gray" variant="ghost" aria-label="Đóng trợ lý AI" @click="emit('close')" /></div>
    </header>
    <div class="border-b border-slate-100 px-5 py-3 dark:border-slate-800"><div class="flex items-center gap-2 text-xs text-slate-500"><span class="size-1.5 rounded-full" :class="ready ? 'bg-emerald-500' : 'bg-slate-400'" /><span class="truncate">{{ indexing ? 'Đang đọc nội dung tài liệu…' : fileName || 'Chưa có tài liệu' }}</span></div><p v-if="limited" class="mt-2 text-xs text-amber-600 dark:text-amber-400">Chat chỉ dùng phần văn bản trong giới hạn 500.000 ký tự.</p></div>
    <div ref="thread" class="min-h-0 flex-1 overflow-y-auto px-5 py-6" aria-live="polite">
      <div v-if="!messages.length" class="flex h-full flex-col justify-center">
        <div class="mb-5 flex size-12 items-center justify-center rounded-2xl bg-gradient-to-br from-violet-100 to-violet-100 text-violet-600 dark:from-violet-500/20 dark:to-violet-500/20"><UIcon name="i-lucide-messages-square" class="size-6" /></div>
        <h3 class="text-xl font-semibold tracking-tight">Hiểu sâu hơn.<br />Đọc nhẹ nhàng hơn.</h3>
        <p class="mt-3 text-sm leading-6 text-slate-500">Hỏi bất cứ điều gì về tài liệu. AI giúp bạn tìm ý chính, giải thích và dẫn nguồn theo trang.</p>
        <div class="mt-6 space-y-2"><button v-for="prompt in prompts" :key="prompt" :disabled="!ready" class="group flex w-full items-center justify-between gap-3 rounded-xl border border-slate-200 p-3 text-left text-xs transition hover:border-violet-300 hover:bg-violet-50 disabled:opacity-50 dark:border-slate-700 dark:hover:bg-violet-500/10" @click="send(prompt)">{{ prompt }}<UIcon name="i-lucide-arrow-up-right" class="size-4 shrink-0 text-slate-400 group-hover:text-violet-500" /></button></div>
        <p v-if="fileName && !indexing && !ready" class="mt-4 text-xs text-amber-600">PDF này không có văn bản. Hãy dùng OCR trước khi chat.</p>
      </div>
      <div v-else class="space-y-6"><div v-for="(message, index) in messages" :key="index"><div v-if="message.role === 'user'" class="ml-6 rounded-2xl rounded-tr-sm bg-violet-600 px-4 py-3 text-sm text-white"><p class="whitespace-pre-wrap break-words">{{ message.text }}</p></div><div v-else><div class="mb-2 flex items-center gap-2 text-xs font-semibold text-violet-600 dark:text-violet-400"><UIcon name="i-lucide-sparkles" class="size-4" />PaperFlow</div><p class="chat-answer text-sm text-slate-700 dark:text-slate-300">{{ message.text }}</p><div v-if="message.citations?.length" class="mt-3 flex flex-wrap gap-2"><button v-for="page in message.citations" :key="page" class="page-link" @click="emit('navigate', page)">Trang {{ page }} ↗</button></div></div></div></div>
      <div v-if="pending" class="mt-5 flex items-center gap-2 text-xs text-violet-500"><UIcon name="i-lucide-loader-circle" class="size-4 animate-spin" />Đang tìm câu trả lời trong tài liệu…</div>
    </div>
    <footer class="shrink-0 border-t border-slate-200 p-4 dark:border-slate-800">
      <div v-if="selection" class="mb-3 rounded-lg bg-violet-50 p-2.5 dark:bg-violet-500/10"><div class="flex items-center justify-between text-[11px] font-medium text-violet-500"><span>Đoạn văn đang chọn</span><button aria-label="Bỏ đoạn chọn khỏi câu hỏi" @click="emit('clearSelection')"><UIcon name="i-lucide-x" /></button></div><p class="mt-1 line-clamp-2 text-xs text-slate-500">{{ selection }}</p></div>
      <p v-if="error" role="alert" class="mb-3 text-xs text-red-500">{{ error }}</p>
      <form class="rounded-xl border border-slate-200 bg-slate-50 p-2 dark:border-slate-700 dark:bg-slate-900" @submit.prevent="send()"><textarea v-model="question" rows="2" maxlength="4000" aria-label="Câu hỏi về PDF" :disabled="!ready" placeholder="Hỏi về tài liệu của bạn…" class="w-full resize-none border-0 bg-transparent p-2 text-sm outline-none focus:ring-0 disabled:opacity-50" @keydown="onKey" /><div class="flex items-center justify-between px-1"><span class="text-[10px] text-slate-400">Enter để gửi · Shift + Enter xuống dòng</span><UButton type="submit" icon="i-lucide-arrow-up" size="xs" :loading="pending" :disabled="!ready || !question.trim()" aria-label="Gửi câu hỏi" /></div></form>
      <p class="mt-2 text-center text-[10px] text-slate-400">AI có thể nhầm. Luôn đối chiếu tài liệu gốc.</p>
    </footer>
  </section>
</template>
