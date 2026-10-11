<script setup lang="ts">
definePageMeta({ middleware: 'auth' })
</script>

<template>
  <ClientOnly>
    <PdfReader />
    <template #fallback><div class="flex h-screen items-center justify-center text-slate-500">PaperFlow?</div></template>
  </ClientOnly>
</template>
