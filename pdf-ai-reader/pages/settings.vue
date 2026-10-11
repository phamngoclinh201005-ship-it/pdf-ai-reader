<script setup lang="ts">
definePageMeta({ middleware: 'auth' })
const { user } = useAuth()
useHead({ title: 'Account Settings | PaperFlow' })
</script>
<template>
  <AccountShell><span class="pf-kicker">YOUR ACCOUNT</span><h1 >Account settings.</h1><dl class="pf-card account-details mt-8"><div class="py-4"><dt class="text-xs pf-muted">Tên hiển thị</dt><dd class="mt-2 font-semibold">{{ user?.displayName }}</dd></div><div class="py-4"><dt class="text-xs pf-muted">Email đã xác minh</dt><dd class="mt-2 break-all">{{ user?.email }}</dd></div><div class="py-4"><dt class="text-xs pf-muted">Vai trò</dt><dd class="mt-2">{{ user?.role }}</dd></div></dl><p class="mt-6 text-sm leading-7 pf-muted">Chỉnh sửa hồ sơ và đổi mật khẩu sẽ được bổ sung ở bước tiếp theo. Thông tin trên được lấy từ phiên đăng nhập thực tế.</p><NuxtLink to="/workspace" class="pf-button mt-6">Quay lại workspace</NuxtLink></AccountShell>
</template>

<style scoped>
.account-details > div + div { border-top: 1px solid var(--pf-border); }
</style>
