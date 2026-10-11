<script setup lang="ts">
defineProps<{ open: boolean; authenticated: boolean; action: string }>()
const emit = defineEmits<{ close: [] }>()
</script>
<template>
  <UModal :model-value="open" @update:model-value="value => { if (!value) emit('close') }">
    <section class="pf-card" aria-labelledby="save-title">
      <div class="mb-4 flex items-center justify-between"><UIcon name="i-lucide-sparkles" class="size-8 text-violet-500" /><UButton icon="i-lucide-x" color="gray" variant="ghost" aria-label="Đóng" @click="emit('close')" /></div>
      <h2 id="save-title" class="text-2xl font-bold">Giữ lại hành trình đọc của bạn.</h2>
      <p class="pf-muted mt-3 text-sm leading-7">Khách chỉ đọc trong phiên tạm: tải lại trang sẽ mất PDF và lịch sử. Đăng nhập để tự lưu tài liệu, vị trí đọc, chat và kết quả AI vào thư viện riêng.</p>
      <div class="mt-6 flex flex-wrap gap-3"><UButton to="/register" icon="i-lucide-arrow-up-right">Tạo tài khoản</UButton><UButton to="/login" variant="outline">Đăng nhập</UButton><UButton color="gray" variant="ghost" @click="emit('close')">Tiếp tục dùng thử</UButton></div>
    </section>
  </UModal>
</template>
