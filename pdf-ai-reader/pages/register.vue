<script setup lang="ts">
import { safeAuthRedirect } from '~/utils/auth-redirect';
const route = useRoute();
const destination = computed(() => safeAuthRedirect(route.query.redirect));
const { register } = useAuth();
const form = reactive({ email: '', displayName: '', password: '' });
const busy = ref(false);
const error = ref('');
async function submit() {
  if (busy.value) return;
  busy.value = true; error.value = '';
  try { await register(form); await navigateTo({ path: '/verify-email', query: { email: form.email, redirect: destination.value } }); }
  catch (e: any) { error.value = e?.data?.message || 'Đăng ký thất bại.'; }
  finally { busy.value = false; }
}
</script>
<template>
  <div class="pf-page"><SiteHeader /><main class="mx-auto grid min-h-[calc(100dvh-90px)] max-w-md items-center px-5 py-10">
    <form @submit.prevent="submit" class="pf-card w-full space-y-5">
      <NuxtLink to="/" class="pf-kicker">✦ PaperFlow</NuxtLink>
      <h1 class="text-3xl font-bold tracking-tight">Tạo tài khoản</h1>
      <p class="text-sm pf-muted">Tạo tài khoản để truy cập không gian đọc PaperFlow.</p>
      <label class="block text-sm">Tên hiển thị<input v-model="form.displayName" required minlength="2" maxlength="80" class="pf-input" placeholder="Tên của bạn" /></label>
      <label class="block text-sm">Email<input v-model="form.email" type="email" required class="pf-input" placeholder="you@example.com" /></label>
      <PasswordInput v-model="form.password" label="Mật khẩu" :disabled="busy" placeholder="Ít nhất 8 ký tự" />
      <p v-if="error" role="alert" class="text-sm text-red-600">{{ error }}</p>
      <button :disabled="busy" class="pf-button w-full">{{ busy ? 'Đang xử lý...' : 'Đăng ký' }}</button>
      <p class="text-center text-sm">Đã có tài khoản? <NuxtLink :to="{ path: '/login', query: { redirect: destination } }" class="pf-link">Đăng nhập</NuxtLink></p>
    </form>
  </main></div>
</template>
