<script setup lang="ts">
const { me, user } = useAuth()
await me()
useHead({ title: 'PDF Reader — Guest welcome | PaperFlow' })
</script>
<template>
  <ClientOnly><PdfReader :key="user?.id || 'guest'" /><template #fallback><div class="flex h-screen items-center justify-center text-slate-500">Đang mở PaperFlow…</div></template></ClientOnly>
</template>
