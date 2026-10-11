<script setup lang="ts">
definePageMeta({ middleware: 'auth' })
useHead({ title: 'My Library | PaperFlow' })
</script>
<template>
  <AccountShell><span class="pf-kicker">PERSONAL SPACE · ĐANG PHÁT TRIỂN</span><h1 >Your next chapter, saved.</h1><p class="mt-6 text-base leading-8 pf-muted">Thư viện cá nhân sẽ giúp bạn lưu PDF và tiếp tục đọc trên các thiết bị. Hiện API lưu trữ PDF riêng tư, quản lý tài liệu và đồng bộ lịch sử chưa được triển khai. Các phiên đọc hiện chỉ giữ dữ liệu trong bộ nhớ tạm.</p><div class="pf-card mt-8"><UIcon name="i-lucide-folder-heart" class="size-10 text-violet-500" /><p class="mt-3 text-sm pf-muted">Bạn vẫn có thể mở PDF, dịch và chat với AI trong workspace.</p></div><NuxtLink to="/workspace" class="pf-button mt-8">Mở workspace</NuxtLink></AccountShell>
</template>
