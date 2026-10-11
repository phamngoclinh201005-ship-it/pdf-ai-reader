<script setup lang="ts">
useHead({ title: 'Quên mật khẩu | PaperFlow' })
const config = useRuntimeConfig()
const { user } = useAuth()
const sessionCheckedAt = useState<number>('auth-checked-at', () => 0)
const email = ref('')
const code = ref('')
const token = ref('')
const password = ref('')
const confirmation = ref('')
const stage = ref<'email' | 'otp' | 'reset' | 'done'>('email')
const busy = ref(false)
const error = ref('')
const notice = ref('')
const now = ref(Date.now())
const resendAt = ref(0)
const expiresAt = ref(0)
const cooldown = computed(() => Math.max(0, Math.ceil((resendAt.value - now.value) / 1000)))
const expired = computed(() => expiresAt.value > 0 && now.value >= expiresAt.value)
let timer: ReturnType<typeof setInterval> | undefined
onMounted(() => { timer = setInterval(() => { now.value = Date.now() }, 1000) })
onBeforeUnmount(() => clearInterval(timer))
async function sendCode() {
  if (busy.value || cooldown.value > 0) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    const response = await $fetch<{ message: string; timing: { serverTime: string; expiresAt: string; resendAvailableAt: string } }>('/api/auth/forgot-password', {
      baseURL: config.public.apiBase, method: 'POST', credentials: 'include', body: { email: email.value.trim() },
    })
    const issued = Date.now()
    now.value = issued
    const server = Date.parse(response.timing.serverTime)
    resendAt.value = issued + Date.parse(response.timing.resendAvailableAt) - server
    expiresAt.value = issued + Date.parse(response.timing.expiresAt) - server
    email.value = email.value.trim(); code.value = ''; stage.value = 'otp'; notice.value = response.message
  } catch (e: any) { error.value = e?.data?.message || 'Không gửi được mã. Vui lòng thử lại.' }
  finally { busy.value = false }
}
async function verifyCode() {
  if (busy.value || code.value.length !== 6 || expired.value) return
  busy.value = true; error.value = ''
  try {
    const response = await $fetch<{ token: string }>('/api/auth/verify-password-reset', {
      baseURL: config.public.apiBase, method: 'POST', credentials: 'include', body: { email: email.value, code: code.value },
    })
    token.value = response.token; code.value = ''; stage.value = 'reset'
  } catch (e: any) { error.value = e?.data?.message || 'Mã xác minh không hợp lệ.' }
  finally { busy.value = false }
}
async function resetPassword() {
  if (busy.value) return
  error.value = ''
  if (password.value !== confirmation.value) { error.value = 'Hai mật khẩu chưa khớp.'; return }
  busy.value = true
  try {
    await $fetch('/api/auth/reset-password', { baseURL: config.public.apiBase, method: 'POST', credentials: 'include', body: {
      email: email.value, token: token.value, password: password.value,
    } })
    password.value = ''; confirmation.value = ''; code.value = ''; token.value = ''; stage.value = 'done'
    user.value = null
    sessionCheckedAt.value = 0
  } catch (e: any) { error.value = e?.data?.message || 'Không đổi được mật khẩu. Vui lòng thử lại.' }
  finally { busy.value = false }
}
function changeEmail() {
  token.value = ''; stage.value = 'email'; code.value = ''; password.value = ''; confirmation.value = ''; error.value = ''; notice.value = ''
}
</script>

<template>
  <div class="pf-page"><SiteHeader /><main class="mx-auto max-w-md px-5 py-12 sm:py-20">
    <section class="pf-card space-y-6" aria-labelledby="reset-title" :aria-busy="busy">
      <span class="pf-kicker">YOUR SPACE · RECOVERY</span>
      <h1 id="reset-title" class="text-3xl font-bold tracking-tight">{{ stage === 'done' ? 'Mật khẩu đã được đổi.' : stage === 'otp' ? 'Xác minh mã OTP' : stage === 'reset' ? 'Đặt mật khẩu mới' : 'Quên mật khẩu?' }}</h1>
      <p v-if="stage !== 'done'" class="pf-muted text-xs">Bước {{ stage === 'email' ? 1 : stage === 'otp' ? 2 : 3 }} / 3 · Email → OTP → Mật khẩu mới</p>
      <template v-if="stage === 'done'">
        <p class="pf-muted text-sm leading-7" role="status">Bạn có thể đăng nhập bằng mật khẩu mới. Các phiên đăng nhập cũ đã được đăng xuất.</p>
        <NuxtLink to="/login" class="pf-button w-full">Đăng nhập</NuxtLink>
      </template>
      <form v-else-if="stage === 'email'" class="space-y-5" @submit.prevent="sendCode">
        <p class="pf-muted text-sm leading-7">Nhập email đã xác minh của bạn để nhận mã đặt lại mật khẩu.</p>
        <label class="block text-sm">Email<input v-model="email" class="pf-input" type="email" autocomplete="email" required maxlength="254" :disabled="busy" placeholder="you@example.com" /></label>
        <p v-if="error" class="text-sm text-rose-500" role="alert">{{ error }}</p>
        <button class="pf-button w-full" :disabled="busy || cooldown > 0">{{ busy ? 'Đang gửi…' : cooldown > 0 ? `Thử lại sau ${cooldown}s` : 'Gửi mã xác minh' }}</button>
      </form>
      <form v-else-if="stage === 'otp'" class="space-y-5" @submit.prevent="verifyCode">
        <p class="pf-muted text-sm leading-7" role="status">{{ notice }} Kiểm tra cả hộp thư spam.</p>
        <div class="flex items-center gap-3 text-sm"><span class="min-w-0 flex-1 break-all">{{ email }}</span><button class="pf-link shrink-0" type="button" :disabled="busy" @click="changeEmail">Đổi email</button></div>
        <div><label id="reset-code-label" class="mb-3 block text-sm">Mã xác minh 6 số</label><OtpInput v-model="code" :disabled="busy" aria-labelledby="reset-code-label" /></div>
        <p v-if="expired" class="text-sm text-rose-500" role="status">Mã đã hết hạn. Hãy gửi lại mã.</p>
        <p v-if="error" class="text-sm text-rose-500" role="alert">{{ error }}</p>
        <button class="pf-button w-full" :disabled="busy || code.length !== 6 || expired">{{ busy ? 'Đang xác minh…' : 'Xác minh mã' }}</button>
        <button class="pf-link w-full text-sm" type="button" :disabled="busy || cooldown > 0" @click="sendCode">{{ cooldown > 0 ? `Gửi lại mã sau ${cooldown}s` : 'Gửi lại mã' }}</button>
      </form>
      <form v-else class="space-y-5" @submit.prevent="resetPassword">
        <p class="pf-muted text-sm leading-7">Email đã được xác minh. Nhập mật khẩu mới cho tài khoản của bạn.</p>
        <PasswordInput v-model="password" label="Mật khẩu mới" :disabled="busy" placeholder="Ít nhất 8 ký tự" />
        <PasswordInput v-model="confirmation" label="Nhập lại mật khẩu mới" :disabled="busy" placeholder="Ít nhất 8 ký tự" />
        <p v-if="error" class="text-sm text-rose-500" role="alert">{{ error }}</p>
        <p v-if="expired" class="text-sm text-rose-500">Phiên xác minh đã hết hạn.</p>
        <button class="pf-button w-full" :disabled="busy || expired">{{ busy ? 'Đang xử lý…' : 'Đặt lại mật khẩu' }}</button>
        <button class="pf-link w-full text-sm" type="button" :disabled="busy" @click="changeEmail">Bắt đầu lại</button>
      </form>
      <NuxtLink v-if="stage !== 'done'" to="/login" class="pf-link block text-center text-sm">← Quay lại đăng nhập</NuxtLink>
    </section>
  </main></div>
</template>

<style scoped>
:deep(.otp-digits input) { background: var(--pf-bg); color: var(--pf-ink); border-color: var(--pf-border); }
:deep(.otp-digits input:focus) { border-color: #a78bfa; }
</style>
