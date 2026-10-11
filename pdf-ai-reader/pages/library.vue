<script setup lang="ts">
import type { LibraryItem } from '~/types/library'
definePageMeta({ middleware: 'auth' })
useHead({ title: 'My Library | PaperFlow' })
const library = useLibrary()
const items = ref<LibraryItem[]>([])
const loading = ref(true)
const error = ref('')
const removing = ref('')
async function refresh() {
  loading.value = true; error.value = ''
  try { items.value = await library.list() }
  catch (e: any) { error.value = e?.data?.message || 'Không tải được thư viện. Thử lại nhé.' }
  finally { loading.value = false }
}
async function remove(item: LibraryItem) {
  if (removing.value || !window.confirm(`Xóa “${item.name}” cùng lịch sử đọc và chat?`)) return
  removing.value = item.id; error.value = ''
  try { await library.remove(item.id); items.value = items.value.filter(x => x.id !== item.id) }
  catch (e: any) { error.value = e?.data?.message || 'Không xóa được tài liệu.' }
  finally { removing.value = '' }
}
onMounted(refresh)
</script>
<template>
  <AccountShell>
    <span class="pf-kicker">YOUR PERSONAL SPACE</span><h1>Thư viện của bạn.</h1>
    <p>PDF, vị trí đọc, cuộc trò chuyện và kết quả AI được lưu riêng trong tài khoản. Mở lại tài liệu để tiếp tục từ nơi bạn dừng.</p>
    <NuxtLink to="/workspace" class="pf-button mt-6">Mở workspace</NuxtLink>
    <p v-if="error" role="alert" class="mt-6 text-sm text-rose-500">{{ error }} <button class="pf-link" @click="refresh">Thử lại</button></p>
    <p v-if="loading" role="status" class="pf-muted mt-8">Đang tải thư viện…</p>
    <div v-else-if="!items.length && !error" class="pf-card mt-8 text-center"><UIcon name="i-lucide-folder-heart" class="size-10 text-violet-500" /><h2 class="mt-4 font-semibold">Chưa có tài liệu</h2><p class="pf-muted mt-3 text-sm">Mở PDF trong workspace. PaperFlow sẽ tự lưu vào tài khoản của bạn.</p></div>
    <ul v-else class="mt-8 space-y-4">
      <li v-for="item in items" :key="item.id" class="pf-card flex flex-wrap items-center gap-4">
        <UIcon name="i-lucide-file-text" class="size-8 shrink-0 text-violet-500" />
        <div class="min-w-0 flex-1"><h2 class="break-words font-semibold">{{ item.name }}</h2><p class="pf-muted mt-2 text-xs">{{ (item.size / 1024 / 1024).toFixed(1) }} MB · Trang {{ item.page }} · {{ new Date(item.updatedAt).toLocaleDateString('vi-VN') }}</p></div>
        <NuxtLink :to="{ path: '/workspace', query: { document: item.id } }" class="pf-button">Đọc tiếp</NuxtLink>
        <button class="pf-muted grid size-10 place-items-center rounded-xl hover:text-rose-500" :disabled="!!removing" :aria-label="`Xóa ${item.name}`" @click="remove(item)"><UIcon :name="removing === item.id ? 'i-lucide-loader-circle' : 'i-lucide-trash-2'" class="size-5" /></button>
      </li>
    </ul>
  </AccountShell>
</template>
