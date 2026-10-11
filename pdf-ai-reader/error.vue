<script setup lang="ts">
import type { NuxtError } from '#app'

const props = defineProps<{ error: NuxtError }>()
const isNotFound = computed(() => props.error.statusCode === 404)
useHead({
  title: computed(() => `${props.error.statusCode} — ${isNotFound.value ? 'Không tìm thấy trang' : 'Đã xảy ra lỗi'} | PaperFlow`),
  meta: [{ name: 'robots', content: 'noindex' }],
})
const goHome = () => clearError({ redirect: '/' })
</script>

<template>
  <main class="error-page">
    <section class="error-content" aria-labelledby="error-title">
      <div class="error-illustration">
        <h1>{{ error.statusCode }}</h1>
      </div>
      <div class="error-description">
        <h2 id="error-title">{{ isNotFound ? 'Có vẻ bạn đã lạc đường rồi!' : 'Có chút trục trặc xảy ra' }}</h2>
        <p>{{ isNotFound ? 'Trang bạn đang tìm không tồn tại hoặc đã được chuyển đi.' : 'Vui lòng quay về trang chủ và thử lại nhé.' }}</p>
        <button class="pf-button home-button" @click="goHome">Về trang chủ</button>
      </div>
    </section>
  </main>
</template>

<style scoped>
.error-page { display: flex; min-height: 100dvh; align-items: center; justify-content: center; padding: 40px 20px; background: radial-gradient(ellipse at top, #8b5cf61a, transparent 60%), var(--pf-bg); color: var(--pf-ink); }
.error-content { width: 100%; max-width: 700px; text-align: center; }
.error-illustration { border-radius: 24px; overflow: hidden; border: 1px solid var(--pf-border); height: 400px; color: #211a38; background: #fff url('/images/404-caveman.gif') center center / auto 400px no-repeat; }
.error-illustration h1 { margin: 0; padding-top: 12px; font-size: 80px; font-weight: 400; line-height: 1.2; }
.error-description { position: relative; margin-top: 28px; }
.error-description h2 { margin: 0 0 12px; font-size: clamp(24px, 4vw, 32px); font-weight: 750; letter-spacing: -1px; line-height: 1.3; }
.error-description p { margin: 0; font-size: 16px; line-height: 1.7; }
.home-button { margin-top: 24px; }.error-description p { color: var(--pf-muted); }
@media (max-width: 480px) { .error-page { padding: 24px 16px; } .error-illustration { height: 340px; background-size: auto 340px; } .error-illustration h1 { font-size: 68px; } .error-description { margin-top: 24px; } .error-description p { font-size: 14px; } }
</style>
