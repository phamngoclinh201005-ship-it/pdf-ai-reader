import type { SavedAction, SavedMessage, ReadingState } from '~/types/library'

export function useReadingPersistence(options: {
  page: Ref<number>; zoom: Ref<number>; count: Ref<number>; load: (file: File) => Promise<void>;
  navigate: (page: number) => void; setZoom: (zoom: number) => void;
}) {
  const { user } = useAuth()
  const library = useLibrary()
  const config = useRuntimeConfig()
  const route = useRoute()
  const router = useRouter()
  const messages = ref<SavedMessage[]>([])
  const actions = ref<SavedAction[]>([])
  const id = ref('')
  const restoring = ref(false)
  const status = ref('')
  const error = ref('')
  const saving = ref(false)
  let file: File | undefined
  let version = 0
  let timer: ReturnType<typeof setTimeout> | undefined
  let queue: Promise<void> = Promise.resolve()
  let disposed = false
  function snapshot(): ReadingState {
    return { page: options.page.value, zoom: options.zoom.value, messages: messages.value.map(m => ({ ...m, citations: m.citations || [] })), actions: actions.value.map(a => ({ ...a })) }
  }
  async function save() {
    if (!user.value || !id.value || restoring.value || disposed) return
    clearTimeout(timer); timer = undefined
    const owner = user.value.id; const document = id.value; const current = version; const body = snapshot()
    saving.value = true; status.value = 'Đang lưu…'; error.value = ''
    queue = queue.catch(() => {}).then(async () => {
      if (disposed || user.value?.id !== owner) return
      try {
        await library.save(document, body)
        if (current === version && !disposed) status.value = 'Đã lưu vào tài khoản'
      } catch (e: any) {
        if (current === version && !disposed) { error.value = e?.data?.message || 'Chưa lưu được. Bấm Sync để thử lại.'; status.value = 'Chưa đồng bộ' }
      } finally { if (current === version && !disposed) saving.value = false }
    })
    await queue
  }
  async function upload() {
    if (!user.value || !file || !options.count.value || id.value || disposed) return
    const current = version; const owner = user.value.id; const selected = file
    saving.value = true; status.value = 'Đang lưu PDF…'; error.value = ''
    try {
      const saved = await library.upload(selected)
      if (current !== version || disposed || user.value?.id !== owner) return
      id.value = saved.id
      await router.replace({ query: { ...route.query, document: saved.id } })
      await save()
    } catch (e: any) {
      if (current === version && !disposed) { error.value = e?.data?.message || 'Chưa lưu được PDF. Bấm Sync để thử lại.'; status.value = 'Chưa đồng bộ' }
    } finally { if (current === version && !disposed) saving.value = false }
  }
  function prepare(selected: File) {
    if (id.value) void save()
    ++version; clearTimeout(timer); timer = undefined; id.value = ''; file = selected; messages.value = []; actions.value = []
    restoring.value = false; saving.value = false
    status.value = ''; error.value = ''
  }
  async function opened(selected: File) {
    if (file !== selected) return
    if (user.value && options.count.value) await upload()
  }
  async function sync() { if (saving.value) return; if (!id.value) await upload(); else await save() }
  async function restore(document: string) {
    if (!user.value || document === id.value || disposed) return
    const current = ++version; const owner = user.value.id
    restoring.value = true; status.value = 'Đang khôi phục…'; error.value = ''
    try {
      const detail = await library.detail(document)
      const blob = await library.file(document)
      if (current !== version || disposed || user.value?.id !== owner) return
      file = new File([blob], detail.name, { type: 'application/pdf' })
      await options.load(file)
      if (!options.count.value || current !== version || disposed) return
      await nextTick()
      id.value = detail.id
      messages.value = detail.state.messages || []; actions.value = detail.state.actions || []
      options.setZoom(detail.state.zoom)
      await nextTick()
      options.navigate(Math.min(detail.state.page, options.count.value))
      status.value = 'Đã khôi phục từ tài khoản'
      if (route.query.document !== detail.id) await router.replace({ query: { ...route.query, document: detail.id } })
    } catch (e: any) { if (current === version && !disposed) { error.value = e?.data?.message || 'Không khôi phục được tài liệu. Hãy mở lại từ My Library.'; status.value = '' } }
    finally { if (current === version && !disposed) { await nextTick(); restoring.value = false } }
  }
  watch([options.page, options.zoom, messages, actions], () => {
    if (!user.value || !id.value || restoring.value) return
    clearTimeout(timer); status.value = 'Đang lưu…'
    timer = setTimeout(() => { timer = undefined; void save() }, 250)
  }, { deep: true })
  watch(() => route.query.document, document => { if (typeof document === 'string' && document !== id.value && !restoring.value) void restore(document) })
  onMounted(async () => {
    if (!user.value) return
    if (typeof route.query.document === 'string') await restore(route.query.document)
    else {
      try { const recent = await library.list(); if (!file && recent[0] && !disposed) await restore(recent[0].id) }
      catch { error.value = 'Không tải được thư viện. Bạn vẫn có thể mở PDF và thử lưu lại.' }
    }
  })
  function flush() {
    if (!user.value || !id.value || restoring.value || !timer) return
    const body = JSON.stringify(snapshot())
    if (new Blob([body]).size <= 60000) {
      const base = config.public.apiBase
      navigator.sendBeacon(`${base}/api/library/${encodeURIComponent(id.value)}/state`, new Blob([body], { type: 'application/json' }))
    }
  }
  onMounted(() => window.addEventListener('pagehide', flush))
  onBeforeUnmount(() => { flush(); disposed = true; ++version; clearTimeout(timer); window.removeEventListener('pagehide', flush) })
  return { messages, actions, id, saving, restoring, status, error, prepare, opened, sync }
}
