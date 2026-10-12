<script setup lang="ts">
const { user, loading, ensureSession, logout } = useAuth()
await ensureSession()
const colorMode = useColorMode()
const darkHeader = computed(() => colorMode.value === 'dark')
const scrolled = ref(false)
const signingOut = ref(false)
const error = ref('')
const initials = computed(() => user.value?.displayName.trim().split(/\s+/).slice(0, 2).map(s => s[0]).join('').toUpperCase() || 'P')
function onScroll() { scrolled.value = window.scrollY > 16 }
function toggleTheme() {
  colorMode.preference = colorMode.value === 'dark' ? 'light' : 'dark'
}
async function signOut() {
  if (signingOut.value) return
  signingOut.value = true; error.value = ''
  try { await logout(); await navigateTo('/') }
  catch { error.value = 'Chưa đăng xuất được. Thử lại nhé.' }
  finally { signingOut.value = false }
}
onMounted(() => { onScroll(); window.addEventListener('scroll', onScroll, { passive: true }) })
onBeforeUnmount(() => window.removeEventListener('scroll', onScroll))
</script>
<template>
  <header class="site-header" :class="{ 'header-dark': darkHeader, scrolled }">
    <div class="header-inner">
      <NuxtLink to="/" class="site-brand"><img src="/favicon.svg" alt="" width="36" height="36" /><span>PaperFlow</span></NuxtLink>
      <nav class="site-nav" aria-label="Điều hướng chính">
        <template v-if="user"><NuxtLink to="/workspace">Workspace</NuxtLink><NuxtLink to="/library">My Library</NuxtLink></template>
        <template v-else><NuxtLink to="/#features">Features</NuxtLink><NuxtLink to="/#how-it-works">How it works</NuxtLink><NuxtLink to="/#library">Your space</NuxtLink></template>
      </nav>
      <div class="header-actions">
        <button class="theme-button" type="button" :aria-label="darkHeader ? 'Giao diện sáng' : 'Giao diện tối'" @click="toggleTheme" title="darkHeader ? 'Giao diện sáng' : 'Giao diện tối'"><UIcon :name="darkHeader ? 'i-lucide-moon' : 'i-lucide-sun'" /></button>
        <span v-if="loading && !user" class="session-check" role="status">Đang kiểm tra phiên…</span>
        <details v-else-if="user" class="account-menu"><summary><span class="account-avatar">{{ initials }}</span><span class="account-name">{{ user.displayName }}</span><UIcon name="i-lucide-chevron-down" /></summary><div class="account-popover"><span class="account-email">{{ user.email }}</span><NuxtLink to="/workspace">My Workspace</NuxtLink><NuxtLink to="/library">My Library</NuxtLink><NuxtLink to="/settings">Account Settings</NuxtLink><NuxtLink v-if="user.role === 'Admin'" to="/admin">Admin</NuxtLink><button :disabled="signingOut" @click="signOut">{{ signingOut ? 'Đang đăng xuất…' : 'Logout' }}</button><p v-if="error" role="alert">{{ error }}</p></div></details>
        <template v-else><NuxtLink to="/login" class="header-login">Log in</NuxtLink><NuxtLink to="/reader" class="header-start" aria-label="Get Started - dùng thử không đăng nhập">Get Started <UIcon name="i-lucide-circle-arrow-right" /></NuxtLink></template>
      </div>
    </div>
  </header>
</template>
<style scoped>
.site-header.site-header.site-header { background: color-mix(in srgb, var(--pf-bg) 88%, transparent); color: var(--pf-ink); }
.site-header.site-header.site-header.scrolled { background: color-mix(in srgb, var(--pf-bg) 96%, transparent); }
.account-popover.account-popover.account-popover { background: var(--pf-surface); }
.site-header { position: sticky; top: 0; z-index: 50; background: #ffffffc9; border-bottom: 1px solid transparent; color: #17152c; transition: background .3s, border-color .3s; backdrop-filter: blur(18px); }.site-header.header-dark { background: #070816a6; color: #f8f5ff; }.site-header.scrolled { border-color: #8b5cf622; background: #fffffff0; }.header-dark.scrolled { background: #080918ed; }
.header-inner { display: flex; align-items: center; gap: 50px; max-width: 1500px; margin: auto; padding: 22px 48px; }.site-brand { display: flex; align-items: center; gap: 10px; font-size: 23px; font-weight: 750; letter-spacing: -.8px; flex-shrink: 0; }.site-nav { display: flex; gap: 30px; font-size: 12px; }.site-nav a { opacity: .75; }.site-nav a:hover { color: #a78bfa; opacity: 1; }.header-actions { display: flex; align-items: center; gap: 12px; margin-left: auto; font-size: 12px; }.theme-button { display: grid; place-items: center; width: 34px; height: 34px; font-size: 20px; }.header-login { padding: 10px 21px; border: 1px solid #a78bfa66; border-radius: 14px; }.header-start { display: flex; gap: 8px; align-items: center; padding: 11px 17px; background: linear-gradient(105deg, #9560ff, #f5a9ec); color: #140b26; font-weight: 700; border-radius: 14px; box-shadow: inset 0 1px #ffffff50; }.header-start .icon { font-size: 16px; }.session-check { opacity: .5; font-size: 10px; }
.account-menu { position: relative; }.account-menu summary { list-style: none; display: flex; align-items: center; gap: 8px; cursor: pointer; }.account-menu summary::-webkit-details-marker { display: none; }.account-avatar { display: grid; place-items: center; width: 34px; height: 34px; border-radius: 50%; background: #8b5cf630; border: 1px solid #a78bfa88; font-weight: 700; }.account-name { max-width: 100px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.account-popover { position: absolute; right: 0; top: 45px; width: 235px; padding: 12px; background: #fff; border: 1px solid #8b5cf640; border-radius: 14px; box-shadow: 0 14px 40px #0003; }.header-dark .account-popover { background: #171527; }.account-popover a, .account-popover button { display: block; width: 100%; padding: 10px; text-align: left; border-radius: 7px; }.account-popover a:hover, .account-popover button:hover { background: #8b5cf61a; }.account-email { display: block; padding: 8px 10px; font-size: 10px; opacity: .55; overflow-wrap: anywhere; }.account-popover p { color: #fb7185; padding: 10px; font-size: 11px; }
@media (max-width: 900px) { .header-inner { gap: 25px; padding: 18px 24px; }.site-nav { gap: 18px; font-size: 11px; } }
@media (max-width: 680px) { .header-inner { padding: 16px 20px; gap: 15px; }.site-brand { font-size: 20px; }.site-brand img { width: 29px; height: 29px; }.site-nav { display: none; }.header-actions { gap: 8px; font-size: 11px; }.header-start { padding: 10px; }.header-login { padding: 9px 12px; }.theme-button { width: 25px; }.account-name { display: none; } }
@media (max-width: 380px) { .header-actions { gap: 5px; }.header-start .icon { display: none; }.header-start, .header-login { padding: 9px; }.site-brand { font-size: 18px; gap: 6px; }.site-brand img { width: 26px; height: 26px; } }
@media (prefers-reduced-motion: reduce) { .site-header { transition: none; } }
</style>
