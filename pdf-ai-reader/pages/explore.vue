<script setup lang="ts">
import type { LibraryItem } from '~/types/library'
useHead({ title: 'Thư viện 3D & 360° | PaperFlow' })
const { user } = useAuth()
const library = useLibrary()
const items = ref<LibraryItem[]>([])
const selected = ref('')
const current = computed(() => items.value.find(item => item.id === selected.value))
const loading = ref(false)
const error = ref('')
async function refresh() {
  items.value = []; selected.value = ''; error.value = ''
  if (!user.value) return
  const owner = user.value.id
  loading.value = true
  try { const list = await library.list(); if (user.value?.id === owner) items.value = list }
  catch { if (user.value?.id === owner) error.value = 'Không tải được thư viện. Hãy thử lại.' }
  finally { loading.value = false }
}
onMounted(refresh)
watch(() => user.value?.id, refresh)
const opening = ref(false)
async function openCurrent() {
  if (!current.value || opening.value) return
  opening.value = true
  try { await navigateTo({ path: '/workspace', query: { document: current.value.id } }) }
  finally { opening.value = false }
}
</script>
<template>
  <div class="pf-page min-h-dvh px-4 py-8 sm:px-8">
    <div class="mx-auto max-w-6xl">
      <header class="mb-6 flex flex-wrap items-center justify-between gap-4"><div><NuxtLink to="/" class="pf-link font-bold">PaperFlow</NuxtLink><h1 class="mt-2 text-3xl font-bold">Thư viện không gian</h1><p class="pf-muted mt-2 text-sm">Một góc nhìn mới cho những tài liệu của bạn.</p></div><div class="flex gap-3"><NuxtLink to="/reader" class="pf-button">Trình đọc PDF</NuxtLink><NuxtLink v-if="user" to="/library" class="pf-link self-center">Thư viện</NuxtLink></div></header>
      <p v-if="loading" role="status" class="mb-4">Đang tải tài liệu…</p>
      <p v-if="error" role="alert" class="mb-4 text-rose-400">{{ error }} <button class="pf-link" @click="refresh">Thử lại</button></p>
      <ClientOnly><SpatialLibrary :items="items" @select="selected = $event" /><template #fallback><p class="pf-card">Đang chuẩn bị không gian 3D…</p></template></ClientOnly>
      <section v-if="current" class="pf-card mt-6 flex flex-wrap items-center justify-between gap-4" aria-live="polite"><div><h2 class="break-words font-semibold">{{ current.name }}</h2><p class="pf-muted text-sm">Đọc tiếp trang {{ current.page }}</p></div><UButton :loading="opening" @click="openCurrent">Mở tài liệu</UButton></section>
      <section class="mt-8"><h2 class="text-xl font-bold">Tài liệu của bạn</h2><p v-if="!user" class="pf-muted mt-3">Bạn có thể khám phá nền 360° ngay. <NuxtLink to="/login" class="pf-link">Đăng nhập</NuxtLink> để đưa thư viện PDF của bạn vào không gian 3D.</p><p v-else-if="!loading && !items.length && !error" class="pf-muted mt-3">Chưa có PDF. Mở một tài liệu trong trình đọc để lưu vào tài khoản.</p><div class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3"><button v-for="item in items" :key="item.id" :aria-pressed="selected === item.id" class="pf-card text-left hover:ring-2 hover:ring-violet-400" :class="selected === item.id ? 'ring-2 ring-violet-400' : ''" @click="selected = item.id"><span class="block break-words font-semibold">{{ item.name }}</span><span class="pf-muted text-xs">Trang {{ item.page }} · {{ (item.size / 1024 / 1024).toFixed(1) }} MB</span></button></div></section>
    </div>
  </div>
</template>
