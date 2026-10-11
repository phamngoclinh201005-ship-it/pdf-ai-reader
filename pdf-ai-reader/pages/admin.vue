<script setup lang="ts">
definePageMeta({ middleware: 'admin' });
const { user } = useAuth();
const config = useRuntimeConfig();
const status = ref('Đang kiểm tra quyền...');
onMounted(async () => {
  try {
    const result = await $fetch<{message: string}>('/api/auth/admin-check', { baseURL: config.public.apiBase, credentials: 'include' });
    status.value = result.message;
  } catch { status.value = 'Không đủ quyền hoặc phiên đăng nhập hết hạn.'; }
});
</script>
<template>
  <AccountShell>
    <div class="pf-card">
      <h1 class="!mt-0">PaperFlow Admin</h1>
      <p class="mt-3">{{ user?.email }}</p>
      <p class="pf-muted mt-2">{{ status }}</p>
      <NuxtLink to="/workspace" class="pf-button mt-5">← Workspace</NuxtLink>
    </div>
  </AccountShell>
</template>
