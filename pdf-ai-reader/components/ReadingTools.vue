<script setup lang="ts">
import type { SavedBookmark, SavedHighlight } from '~/types/library'
import { exportReadingNotes, searchDocument } from '~/utils/reading-tools'
const props = defineProps<{ pages: { page: number; text: string }[]; outline: { page: number; title: string; depth: number }[]; page: number; count: number; name: string; indexing: boolean; limited: boolean }>()
const highlights = defineModel<SavedHighlight[]>('highlights', { required: true })
const bookmarks = defineModel<SavedBookmark[]>('bookmarks', { required: true })
const emit = defineEmits<{ navigate: [page: number] }>()
const tab = ref('notes')
const query = ref('')
const settledQuery = ref('')
let timer: ReturnType<typeof setTimeout> | undefined
watch(query, value => { clearTimeout(timer); timer = setTimeout(() => { settledQuery.value = value }, 180) })
onBeforeUnmount(() => clearTimeout(timer))
const matches = computed(() => searchDocument(props.pages, settledQuery.value))
const title = ref('')
const toolError = ref('')
function bookmark() {
  toolError.value = ''
  if (!props.count) return
  if (bookmarks.value.length >= 200) { toolError.value = 'Tối đa 200 bookmark mỗi tài liệu.'; return }
  if (bookmarks.value.some(b => b.page === props.page)) { toolError.value = 'Trang này đã có bookmark.'; return }
  bookmarks.value.push({ id: crypto.randomUUID(), page: props.page, title: title.value.trim() || `Trang ${props.page}` })
  title.value = ''
}
function download() {
  const url = URL.createObjectURL(new Blob([exportReadingNotes(props.name, highlights.value, bookmarks.value)], { type: 'text/markdown;charset=utf-8' }))
  const link = document.createElement('a'); link.href = url; link.download = `${props.name.replace(/\.pdf$/i, '') || 'paperflow'}-notes.md`
  link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000)
}
</script>
<template>
  <div class="space-y-4">
    <div class="flex flex-wrap gap-2" aria-label="Công cụ học tập">
      <UButton v-for="item in [{ id: 'notes', label: 'Ghi chú' }, { id: 'bookmarks', label: 'Bookmark' }, { id: 'search', label: 'Tìm kiếm' }, { id: 'outline', label: 'Mục lục' }]" :key="item.id" :variant="tab === item.id ? 'soft' : 'ghost'" :aria-pressed="tab === item.id" @click="tab = item.id">{{ item.label }}</UButton>
      <UButton color="gray" icon="i-lucide-download" :disabled="!highlights.length && !bookmarks.length" @click="download">Xuất .md</UButton>
    </div>
    <template v-if="tab === 'notes'">
      <p v-if="!highlights.length" class="pf-muted text-sm">Bôi đen văn bản và chọn Đánh dấu để thêm ghi chú.</p>
      <article v-for="mark in highlights" :key="mark.id" class="rounded-xl border border-yellow-400/30 p-3">
        <button class="pf-link text-xs" @click="emit('navigate', mark.page)">Trang {{ mark.page }} · Đến đoạn văn</button>
        <p class="mt-2 whitespace-pre-wrap text-sm">{{ mark.text }}</p>
        <label class="mt-3 block text-xs">Ghi chú của bạn<textarea v-model="mark.note" maxlength="2000" rows="3" class="pf-input mt-2 w-full" placeholder="Ý chính, câu hỏi hoặc điều cần nhớ…" /></label>
        <UButton class="mt-2" size="xs" color="gray" icon="i-lucide-trash-2" @click="highlights = highlights.filter(h => h.id !== mark.id)">Xóa đánh dấu</UButton>
      </article>
    </template>
    <template v-else-if="tab === 'bookmarks'">
      <form class="flex gap-2" @submit.prevent="bookmark"><input v-model="title" maxlength="120" class="pf-input min-w-0 flex-1" aria-label="Tên bookmark" :placeholder="`Đặt tên cho trang ${page}`" /><UButton type="submit" :disabled="!count">Lưu trang {{ page }}</UButton></form>
      <p v-if="toolError" role="alert" class="text-sm text-rose-500">{{ toolError }}</p>
      <p v-if="!bookmarks.length" class="pf-muted text-sm">Chưa có bookmark.</p>
      <div v-for="mark in bookmarks" :key="mark.id" class="flex items-center gap-2 rounded-xl border border-[var(--pf-border)] p-3"><button class="min-w-0 flex-1 text-left text-sm" @click="emit('navigate', mark.page)">{{ mark.title }} · Trang {{ mark.page }}</button><UButton icon="i-lucide-trash-2" color="gray" variant="ghost" :aria-label="`Xóa bookmark ${mark.title}`" @click="bookmarks = bookmarks.filter(b => b.id !== mark.id)" /></div>
    </template>
    <template v-else-if="tab === 'outline'">
      <p v-if="!outline.length" class="pf-muted text-sm">PDF này chưa có mục lục điều hướng. Bạn có thể tự thêm bookmark.</p>
      <button v-for="(entry, index) in outline" :key="index" class="block w-full rounded-xl border border-[var(--pf-border)] py-3 pr-3 text-left text-sm hover:bg-violet-500/10" :style="{ paddingLeft: `${12 + entry.depth * 12}px` }" @click="emit('navigate', entry.page)">{{ entry.title }} · Trang {{ entry.page }}</button>
    </template>
    <template v-else>
      <UInput v-model="query" icon="i-lucide-search" placeholder="Tìm từ hoặc cụm từ trong PDF…" aria-label="Tìm nội dung PDF" />
      <p class="pf-muted text-xs">{{ indexing ? 'Đang đọc thêm các trang; kết quả sẽ cập nhật.' : limited ? 'Kết quả chỉ trong phần văn bản đã được trích xuất.' : 'Tìm trong văn bản PDF trên thiết bị, không gọi AI.' }}</p>
      <p role="status" class="text-sm">{{ settledQuery.trim().length < 2 ? 'Nhập ít nhất 2 ký tự.' : `${matches.length}${matches.length === 100 ? '+' : ''} kết quả` }}</p>
      <button v-for="(match, index) in matches" :key="index" class="block w-full rounded-xl border border-[var(--pf-border)] p-3 text-left hover:bg-violet-500/10" @click="emit('navigate', match.page)"><span class="pf-link text-xs">Trang {{ match.page }}</span><p class="mt-1 text-sm">…{{ match.snippet }}…</p></button>
    </template>
  </div>
</template>
