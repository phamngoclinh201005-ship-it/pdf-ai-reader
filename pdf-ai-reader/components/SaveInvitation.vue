<script setup lang="ts">
defineProps<{ open: boolean; authenticated: boolean; action: string }>()
const emit = defineEmits<{ close: [] }>()
</script>
<template>
  <UModal :model-value="open" @update:model-value="value => { if (!value) emit('close') }">
    <section class="pf-card" aria-labelledby="save-title">
      <div class="mb-4 flex items-center justify-between"><UIcon name="i-lucide-sparkles" class="size-8 text-violet-500" /><UButton icon="i-lucide-x" color="gray" variant="ghost" aria-label="Đóng" @click="emit('close')" /></div>
      <h2 id="save-title" class="text-2xl font-bold">{{ authenticated ? 'Your space is taking shape ✦' : 'Make it yours ✦' }}</h2>
      <p class="mt-3 text-sm leading-7 text-slate-500">{{ authenticated ? `Tính năng ${action} đang được phát triển. Tài liệu và hội thoại hiện chỉ nằm trong phiên đọc; chưa được lưu hoặc đồng bộ lên server.` : 'Create an account to save your documents and pick up where you left off. Thư viện cá nhân đang được phát triển; bạn có thể tiếp tục đọc miễn phí ngay bây giờ.' }}</p>
      <div class="mt-6 flex flex-wrap gap-3"><UButton v-if="!authenticated" to="/register" icon="i-lucide-arrow-up-right">Create Account</UButton><UButton v-else to="/library" icon="i-lucide-folder-open">Xem My Library</UButton><UButton color="gray" variant="ghost" @click="emit('close')">{{ authenticated ? 'Tiếp tục đọc' : 'Maybe Later' }}</UButton></div>
    </section>
  </UModal>
</template>
