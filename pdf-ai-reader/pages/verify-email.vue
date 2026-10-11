<script setup lang="ts">
import { safeAuthRedirect } from '~/utils/auth-redirect'
import { secondsUntil } from '~/utils/otp'

const route = useRoute()
const { verify, resendOtp } = useAuth()
const { challenge, restore, clear } = useOtpChallenge()
const root = ref<HTMLElement | null>(null)
const otp = ref<{ focus: (index?: number) => void } | null>(null)
const { shake, success: celebrate } = useOtpMotion(root)
const email = ref(typeof route.query.email === 'string' ? route.query.email.trim().toLowerCase() : '')
const code = ref('')
const busy = ref(false)
const resendBusy = ref(false)
const error = ref('')
const message = ref('')
const verified = ref(false)
const now = ref(0)
const pointer = reactive({ x: 50, y: 50, dx: 0, dy: 0 })
let timer: ReturnType<typeof setInterval> | undefined
let frame = 0
let active = true
const currentChallenge = computed(() => challenge.value?.email === email.value ? challenge.value : null)
const remaining = computed(() => currentChallenge.value && now.value ? secondsUntil(currentChallenge.value.expiresAt, now.value) : null)
const cooldown = computed(() => currentChallenge.value && now.value ? secondsUntil(currentChallenge.value.resendAt, now.value) : 0)
const expired = computed(() => remaining.value === 0)
const clock = computed(() => remaining.value === null ? '— : —' : `${Math.floor(remaining.value / 60).toString().padStart(2, '0')}:${(remaining.value % 60).toString().padStart(2, '0')}`)
const disabled = computed(() => busy.value || resendBusy.value || verified.value)
useHead({ title: 'Unlock your universe — Xác minh email | PaperFlow' })

onMounted(() => {
  restore()
  if (!email.value) email.value = challenge.value?.email || ''
  now.value = Date.now()
  timer = setInterval(() => { now.value = Date.now() }, 1000)
  if (email.value) void nextTick(() => otp.value?.focus())
})
onBeforeUnmount(() => { active = false; clearInterval(timer); cancelAnimationFrame(frame) })
watch(code, () => { if (error.value) error.value = '' })
watch(email, () => { code.value = ''; error.value = ''; message.value = '' })

function move(event: PointerEvent) {
  if (event.pointerType !== 'mouse' || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
  const element = event.currentTarget as HTMLElement
  const rect = element.getBoundingClientRect()
  cancelAnimationFrame(frame)
  frame = requestAnimationFrame(() => {
    pointer.x = (event.clientX - rect.left) / rect.width * 100
    pointer.y = (event.clientY - rect.top) / rect.height * 100
    pointer.dx = (pointer.x - 50) * .12
    pointer.dy = (pointer.y - 50) * .12
  })
}
function resetPointer() { cancelAnimationFrame(frame); pointer.x = 50; pointer.y = 50; pointer.dx = 0; pointer.dy = 0 }
function failureMessage(e: unknown, fallback: string) {
  const failure = e as { statusCode?: number; status?: number; data?: { message?: string } }
  if (failure.statusCode === 429 || failure.status === 429) return 'Bạn đã thử nhiều lần. Đợi một chút rồi quay lại nhé.'
  return failure.data?.message || fallback
}
async function submit() {
  if (disabled.value) return
  error.value = ''; message.value = ''
  if (!email.value.trim()) { error.value = 'Nhập email bạn đã dùng để đăng ký nhé.'; return }
  if (expired.value) { error.value = 'Mã đã hết hạn. Gửi lại mã mới để tiếp tục nhé.'; return }
  if (!/^\d{6}$/.test(code.value)) { error.value = 'Nhập đủ 6 chữ số trong email của bạn nhé.'; otp.value?.focus(); return }
  busy.value = true
  try {
    await verify(email.value.trim(), code.value)
    if (!active) return
    verified.value = true
    clear()
    await celebrate()
    if (active) await navigateTo({ path: '/login', query: { redirect: safeAuthRedirect(route.query.redirect) } })
  } catch (e: unknown) {
    if (!active) return
    error.value = failureMessage(e, 'Chưa xác minh được mã. Kiểm tra email rồi thử lại nhé.')
    shake()
    otp.value?.focus(0)
  } finally {
    if (active) {
      busy.value = false
      if (error.value) { await nextTick(); otp.value?.focus(0) }
    }
  }
}
async function resend() {
  if (disabled.value || cooldown.value) return
  if (!email.value.trim()) { error.value = 'Nhập email đã đăng ký để nhận mã nhé.'; return }
  resendBusy.value = true; error.value = ''; message.value = ''
  try {
    const response = await resendOtp(email.value.trim())
    if (!active) return
    now.value = Date.now()
    code.value = ''
    message.value = response.message
    if (!response.timing) message.value += ' Khởi động lại backend để đồng bộ thời hạn mã.'
  } catch (e: unknown) { if (active) error.value = failureMessage(e, 'Chưa gửi được mã. Thử lại sau một chút nhé.') }
  finally { if (active) { resendBusy.value = false; await nextTick(); otp.value?.focus() } }
}
</script>

<template>
  <main ref="root" class="otp-universe" :class="{ 'is-verified': verified }" :style="{ '--pointer-x': `${pointer.x}%`, '--pointer-y': `${pointer.y}%`, '--parallax-x': `${pointer.dx}px`, '--parallax-y': `${pointer.dy}px` }" @pointermove="move" @pointerleave="resetPointer">
    <div class="aurora aurora-purple" aria-hidden="true" /><div class="aurora aurora-blue" aria-hidden="true" />
    <div class="universe-grid" aria-hidden="true" />
    <div class="particles" aria-hidden="true"><i v-for="n in 16" :key="n" :style="{ left: `${(n * 37) % 100}%`, top: `${(n * 23) % 100}%`, animationDelay: `${n * -.7}s` }" /></div>

    <header class="universe-header">
      <NuxtLink to="/" class="paperflow-logo"><img src="/favicon.svg" width="34" height="34" alt="" />PaperFlow</NuxtLink>
      <span class="header-caption">A LITTLE CURIOUS. A LOT OF POSSIBILITIES.</span>
      <NuxtLink to="/login" class="header-login">Đã có tài khoản <UIcon name="i-lucide-arrow-up-right" /></NuxtLink>
    </header>

    <div class="universe-layout">
      <section class="universe-story" aria-labelledby="story-heading">
        <span class="story-kicker story-line"><i /> THE NEXT CHAPTER IS YOURS</span>
        <h1 id="story-heading"><span class="story-line">ONE MORE</span><span class="story-line">STEP TO</span><span class="story-line gradient-type">YOUR SPACE<span class="headline-star" aria-hidden="true">✦</span></span></h1>
        <p class="story-subtitle story-line">Your papers. Your ideas.<br /><b>Your universe.</b></p>
        <div class="story-doodle story-line"><svg viewBox="0 0 130 40" aria-hidden="true"><path d="M3 20Q65 3 113 23m-13-14 15 14-17 9" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" /></svg><span>GOOD IDEAS<br />LIVE HERE</span></div>
        <div class="story-bottom story-line"><span class="mini-avatars"><i>✦</i><i>↗</i><i>☺</i></span><span>For the thinkers.<br /><b>The dreamers. The you.</b></span></div>
      </section>

      <section class="verification-zone" aria-labelledby="verify-heading">
        <span class="inbox-sticker" aria-hidden="true">CHECK YOUR INBOX! <UIcon name="i-lucide-arrow-down-left" /></span>
        <form class="otp-panel" :class="{ 'panel-success': verified }" :aria-busy="disabled" @submit.prevent="submit">
          <div class="panel-topline"><span>AUTHENTICATION</span><b>STEP 02 <i>/ 02</i></b></div>
          <div class="envelope-scene" aria-hidden="true">
            <span class="envelope-spark spark-one">✦</span><span class="envelope-spark spark-two">+</span>
            <div class="envelope-halo" />
            <div class="holo-envelope"><div class="envelope-letter"><svg viewBox="0 0 40 40"><path d="m10 21 7 7 14-16" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" /></svg></div><div class="envelope-back" /><div class="envelope-flap" /><div class="envelope-front" /><UIcon name="i-lucide-sparkles" class="envelope-seal" /></div>
          </div>
          <template v-if="!verified">
            <span class="card-eyebrow">YOU'RE ALMOST IN ✦</span>
            <h2 id="verify-heading">Verify your email<span>.</span></h2>
            <p class="card-intro">Một chiếc mã nhỏ, một vũ trụ lớn.<br />Nhập 6 chữ số được gửi đến email của bạn.</p>
          </template>
          <div v-else class="success-copy" role="status"><span class="card-eyebrow">ACCESS GRANTED ✦</span><h2 id="verify-heading">YOU'RE IN.</h2><p>WELCOME TO YOUR UNIVERSE ✦</p><span>Đang đưa bạn đến trang đăng nhập…</span></div>

          <label class="email-line"><UIcon name="i-lucide-mail" /><span class="sr-only">Email đã đăng ký</span><input v-model="email" type="email" autocomplete="email" maxlength="254" :disabled="disabled" placeholder="Email bạn đã đăng ký" required /><UIcon name="i-lucide-pencil" class="email-edit" /></label>
          <OtpInput ref="otp" v-model="code" :disabled="disabled" :invalid="!!error" :success="verified" />
          <div class="countdown-line" :class="{ 'is-expired': expired }"><span><UIcon :name="expired ? 'i-lucide-circle-alert' : 'i-lucide-timer'" />{{ expired ? 'Mã đã hết hạn' : remaining === null ? 'Chưa có thời hạn mã' : 'Mã còn hiệu lực' }}</span><b>{{ verified ? '✓' : clock }}</b></div>
          <p v-if="remaining === null && !verified" class="timing-hint">Bạn đã nhận mã? Nhập mã để xác minh, hoặc gửi lại để đồng bộ thời hạn.</p>
          <p v-if="expired && !error" class="expired-message" role="status">Hết giờ rồi. Gửi một mã mới để mở khóa tiếp nhé.</p>
          <p v-if="error" id="otp-feedback" role="alert" class="feedback feedback-error"><UIcon name="i-lucide-circle-alert" />{{ error }}</p>
          <p v-if="message" role="status" class="feedback feedback-info"><UIcon name="i-lucide-send" />{{ message }}</p>

          <button class="unlock-button" type="submit" :disabled="disabled || expired || code.length !== 6"><UIcon v-if="busy && !verified" name="i-lucide-loader-circle" class="animate-spin" /><span>{{ verified ? 'WELCOME TO YOUR SPACE' : busy ? 'ĐANG MỞ KHÓA…' : 'UNLOCK MY SPACE' }}</span><UIcon :name="verified ? 'i-lucide-check' : 'i-lucide-arrow-right'" /></button>
          <div class="resend-line"><span>Chưa thấy email?</span><button type="button" :disabled="disabled || cooldown > 0" @click="resend">{{ resendBusy ? 'Đang gửi…' : cooldown ? `Gửi lại sau ${cooldown}s` : 'Resend OTP ↗' }}</button></div>
          <NuxtLink to="/register" class="back-signup"><UIcon name="i-lucide-arrow-left" />Back to signup</NuxtLink>
          <div class="card-trust"><UIcon name="i-lucide-shield-check" />A tiny code. A safer space.</div>
        </form>
      </section>

      <aside class="universe-visual"><OtpUniverse /></aside>
    </div>
    <footer class="universe-footer"><span>© {{ new Date().getFullYear() }} PAPERFLOW</span><span>LESS FRICTION. MORE IMAGINATION. <b>✦</b></span><span class="system-status"><i />YOUR UNIVERSE IS WAITING</span></footer>
  </main>
</template>

<style scoped>
.otp-universe { position: relative; min-height: 100dvh; overflow: hidden; color: #f8f5ff; background: #070816; color-scheme: dark; font-family: Inter, ui-sans-serif, system-ui, sans-serif; animation: universe-appear .8s ease both; }
.aurora { position: absolute; pointer-events: none; width: 650px; height: 650px; border-radius: 50%; filter: blur(85px); opacity: .2; animation: aurora-drift 22s ease-in-out infinite alternate; }
.aurora-purple { top: -320px; left: 25%; background: radial-gradient(circle, #8b5cf6, transparent 70%); }.aurora-blue { bottom: -390px; right: -50px; background: radial-gradient(circle, #38bdf8, transparent 70%); animation-delay: -10s; opacity: .15; }
.universe-grid { position: absolute; inset: 0; pointer-events: none; background-image: linear-gradient(#ffffff04 1px, transparent 1px), linear-gradient(90deg, #ffffff04 1px, transparent 1px); background-size: 70px 70px; mask-image: linear-gradient(transparent, #000 30%, transparent); }
.particles { position: absolute; inset: 0; pointer-events: none; }.particles i { position: absolute; width: 3px; height: 3px; background: #c4b5fd; border-radius: 50%; box-shadow: 0 0 7px #8b5cf6; opacity: .3; animation: particle-drift 7s ease-in-out infinite alternate; }
.universe-header, .universe-footer { position: relative; z-index: 2; display: flex; align-items: center; justify-content: space-between; gap: 24px; max-width: 1600px; margin: 0 auto; padding: 32px 48px; }
.paperflow-logo { display: flex; align-items: center; font-size: 23px; font-weight: 750; letter-spacing: -1px; }.paperflow-logo > span:first-child { display: grid; place-items: center; width: 34px; height: 34px; margin-right: 10px; border-radius: 10px; border: 1px solid #a78bfa66; background: #8b5cf625; color: #c4b5fd; font-size: 18px; }.logo-flow { color: #c4b5fd; }.paperflow-logo sup { margin-left: 6px; color: #f472d0; font-size: 13px; }
.header-caption { color: #77738d; font-size: 9px; letter-spacing: 2.3px; }.header-login { display: flex; align-items: center; gap: 8px; color: #ada5c5; font-size: 11px; }.header-login:hover { color: #f5d0fe; }
.universe-layout { position: relative; z-index: 1; display: grid; grid-template-columns: minmax(0, 1.02fr) minmax(360px, 430px) minmax(0, .9fr); align-items: center; gap: 36px; max-width: 1504px; margin: 0 auto; padding: 50px 48px 65px; min-height: calc(100dvh - 185px); }
.universe-story { padding-bottom: 18px; }.story-kicker { display: flex; gap: 9px; align-items: center; margin-bottom: 30px; font-size: 9px; letter-spacing: 2px; color: #b0a6ce; }.story-kicker i { width: 5px; height: 5px; background: #f472d0; border-radius: 50%; box-shadow: 0 0 9px #f472d0; }
h1 { margin: 0; font-size: clamp(40px, 4.25vw, 66px); font-weight: 850; line-height: 1.04; letter-spacing: -3.5px; }h1 > span { display: block; }.gradient-type { position: relative; background: linear-gradient(120deg, #a78bfa 5%, #f472d0 85%); background-clip: text; color: transparent; text-shadow: 0 0 60px #8b5cf625; }.headline-star { display: inline-block; font-size: .5em; vertical-align: top; color: #f472d0; transform: rotate(15deg); margin-left: 6px; }
.story-subtitle { margin-top: 24px; font-size: 14px; line-height: 1.9; color: #9992b0; }.story-subtitle b { font-weight: 500; color: #e8dcff; }
.story-doodle { display: flex; align-items: center; gap: 10px; margin-top: 33px; color: #c4b5fd; transform: rotate(-8deg); }.story-doodle svg { width: 100px; }.story-doodle span { font-family: 'Comic Sans MS', cursive; font-size: 12px; letter-spacing: 1px; line-height: 1.4; }
.story-bottom { display: flex; gap: 13px; align-items: center; margin-top: 48px; font-size: 10px; line-height: 1.7; color: #817892; }.story-bottom b { color: #b9afcc; font-weight: 400; }.mini-avatars { display: flex; padding-left: 6px; }.mini-avatars i { display: grid; place-items: center; width: 27px; height: 27px; margin-left: -6px; font-style: normal; font-size: 13px; border-radius: 50%; background: #34264c; border: 2px solid #070816; color: #f5d0fe; }.mini-avatars i:nth-child(2) { background: #163b49; color: #7dd3fc; }.mini-avatars i:nth-child(3) { background: #503343; color: #f9a8d4; }
.verification-zone { position: relative; }.inbox-sticker { position: absolute; top: -20px; right: -14px; z-index: 4; display: flex; gap: 9px; align-items: center; padding: 9px 13px; background: #ded0ff; color: #271744; border: 2px solid #070816; box-shadow: 3px 3px 0 #8b5cf6; transform: rotate(8deg); font-size: 9px; font-weight: 800; letter-spacing: .7px; animation: sticker-rock 6s ease-in-out infinite alternate; }.inbox-sticker .icon { font-size: 17px; }
.otp-panel { position: relative; isolation: isolate; padding: 28px; border: 1px solid #a78bfa65; border-radius: 25px; background: linear-gradient(145deg, #1c1930ed, #100f23f0); backdrop-filter: blur(24px); box-shadow: 0 25px 70px #0006, 0 0 60px #8b5cf60c, inset 0 1px 0 #ffffff14; }
.otp-panel::before { position: absolute; z-index: -1; inset: 0; border-radius: inherit; content: ''; pointer-events: none; background: radial-gradient(230px circle at var(--pointer-x) var(--pointer-y), #8b5cf614, transparent); }.otp-panel::after { position: absolute; content: ''; height: 1px; top: -1px; left: 15%; right: 15%; background: linear-gradient(90deg, transparent, #f472d0, #38bdf8, transparent); }
.panel-topline { display: flex; justify-content: space-between; align-items: center; color: #8b809d; font: 8px monospace; letter-spacing: 1.6px; }.panel-topline b { color: #c4b5fd; font-size: 9px; font-weight: 400; }.panel-topline i { color: #665b7b; font-style: normal; }
.envelope-scene { position: relative; height: 130px; display: grid; place-items: center; }.envelope-halo { position: absolute; width: 115px; height: 85px; border-radius: 50%; background: #8b5cf625; filter: blur(24px); animation: halo-breathe 4s ease-in-out infinite; }
.holo-envelope { position: relative; width: 84px; height: 59px; transform: rotate(-9deg); animation: envelope-float 4s ease-in-out infinite; perspective: 400px; }.envelope-back { position: absolute; inset: 0; background: linear-gradient(135deg, #6139a866, #38bdf822); border: 1px solid #a78bfa; border-radius: 9px; box-shadow: 0 0 30px #8b5cf633; }
.envelope-front { position: absolute; inset: 0; z-index: 3; clip-path: polygon(0 0, 50% 55%, 100% 0, 100% 100%, 0 100%); border: 1px solid #bcb3ff; border-radius: 9px; background: linear-gradient(135deg, #8b5cf6, #704da9 55%, #38bdf8); }.envelope-flap { position: absolute; top: 0; left: 0; right: 0; z-index: 4; height: 36px; clip-path: polygon(0 0, 100% 0, 50% 100%); background: linear-gradient(150deg, #d5b8ff, #b78ded 50%, #67d5ff); transform-origin: top; transition: transform .5s, z-index .5s; }
.envelope-letter { position: absolute; z-index: 2; inset: 6px 10px; border-radius: 5px; border: 1px solid #b2ffe7; background: #d6fff0; color: #047857; transform: translateY(0); transition: transform .6s .1s; }.envelope-letter svg { width: 100%; height: 100%; }.envelope-letter path { stroke-dasharray: 40; stroke-dashoffset: 40; transition: stroke-dashoffset .5s .45s; }.envelope-seal { position: absolute; left: 35px; top: 27px; z-index: 5; color: #f5d0fe; font-size: 16px; }.envelope-spark { position: absolute; color: #f472d0; }.spark-one { left: 23%; top: 35px; font-size: 19px; }.spark-two { right: 21%; bottom: 32px; color: #38bdf8; }
.card-eyebrow { display: block; margin-bottom: 10px; font-size: 8px; letter-spacing: 2px; color: #c4b5fd; }h2 { margin: 0; font-size: 28px; letter-spacing: -1.2px; font-weight: 750; line-height: 1.2; }h2 > span { color: #f472d0; }.card-intro { margin: 13px 0 22px; color: #a49ab8; font-size: 11px; line-height: 1.8; }
.email-line { display: flex; align-items: center; gap: 9px; margin-bottom: 20px; padding: 11px 12px; background: #ffffff04; border: 1px solid #ffffff0d; border-radius: 9px; color: #ac9dc2; font-size: 13px; }.email-line input { background: none; border: 0; outline: none; color: #e4d9f5; font-size: 11px; width: 100%; min-width: 0; padding: 0; }.email-line:focus-within { border-color: #a78bfa; }.email-edit { font-size: 11px; opacity: .5; }
.countdown-line { display: flex; align-items: center; justify-content: space-between; margin: 15px 0 22px; font-size: 10px; color: #8e84a4; }.countdown-line span { display: flex; align-items: center; gap: 6px; }.countdown-line b { font: 12px monospace; color: #c4b5fd; letter-spacing: 1px; }.countdown-line.is-expired, .countdown-line.is-expired b { color: #fda4af; }
.timing-hint, .expired-message { margin: -10px 0 17px; font-size: 10px; line-height: 1.7; color: #9285a9; }.expired-message { color: #fda4af; }.feedback { display: flex; align-items: flex-start; gap: 7px; margin-bottom: 15px; padding: 10px; border-radius: 8px; font-size: 11px; line-height: 1.7; }.feedback .icon { margin-top: 3px; flex-shrink: 0; }.feedback-error { color: #fda4af; background: #fb71850d; border: 1px solid #fb718530; }.feedback-info { color: #a5f3fc; background: #38bdf80d; }
.unlock-button { display: flex; align-items: center; justify-content: center; gap: 9px; width: 100%; padding: 16px 10px; border-radius: 11px; background: linear-gradient(110deg, #8955f1, #aa64ec 55%, #dd73c6); box-shadow: 0 6px 25px #8b5cf62e; border: 1px solid #d3a2ff4d; color: #fff; font-size: 11px; font-weight: 750; letter-spacing: 1.2px; transition: box-shadow .2s, filter .2s; }.unlock-button .icon { font-size: 17px; }.unlock-button:hover:not(:disabled) { filter: brightness(1.15); box-shadow: 0 0 30px #8b5cf655; }.unlock-button:disabled { opacity: .5; cursor: not-allowed; }.panel-success .unlock-button { opacity: 1; background: linear-gradient(110deg, #177969, #7bbcac); }
.resend-line { display: flex; align-items: center; justify-content: center; gap: 7px; margin-top: 20px; font-size: 10px; color: #978ba8; }.resend-line button { color: #d4b8ff; font-weight: 500; }.resend-line button:disabled { color: #847290; cursor: not-allowed; }.back-signup { display: flex; align-items: center; justify-content: center; gap: 6px; margin-top: 18px; font-size: 10px; color: #9a8daf; }.back-signup:hover { color: #f5d0fe; }.card-trust { display: flex; gap: 6px; align-items: center; justify-content: center; margin-top: 22px; padding-top: 15px; border-top: 1px solid #ffffff0a; color: #655b76; font-size: 8px; letter-spacing: .5px; }
.universe-visual { transform: translate(var(--parallax-x), var(--parallax-y)); transition: transform .3s ease-out; }.universe-footer { padding-top: 20px; padding-bottom: 22px; border-top: 1px solid #ffffff06; font-size: 8px; letter-spacing: 1.5px; color: #6d647f; }.universe-footer b { color: #f472d0; margin-left: 8px; }.system-status { display: flex; align-items: center; gap: 7px; }.system-status i { width: 4px; height: 4px; border-radius: 50%; background: #6ee7d0; box-shadow: 0 0 7px #6ee7d0; }
.success-copy h2 { color: #b1ffe7; font-size: 36px; }.success-copy p { margin: 8px 0; font-size: 10px; color: #d0baff; letter-spacing: 1px; }.success-copy > span:last-child { display: block; color: #b1a4c9; font-size: 10px; margin-bottom: 22px; }.panel-success .envelope-flap { transform: rotateX(180deg); z-index: 1; }.panel-success .envelope-letter { transform: translateY(-30px); }.panel-success .envelope-letter path { stroke-dashoffset: 0; }.panel-success .envelope-seal { opacity: 0; }.panel-success .envelope-halo { background: #6ee7d036; }
@keyframes universe-appear { from { opacity: 0; } to { opacity: 1; } }@keyframes aurora-drift { to { transform: translate(80px, 45px) scale(1.2); } }@keyframes particle-drift { to { transform: translate(8px, -18px); opacity: .7; } }@keyframes envelope-float { 50% { transform: translateY(-6px) rotate(-4deg); } }@keyframes halo-breathe { 50% { opacity: .5; transform: scale(1.2); } }@keyframes sticker-rock { to { transform: rotate(5deg); } }
@media (min-width: 1600px) { .universe-layout { gap: 60px; } }
@media (max-width: 1200px) { .universe-layout { gap: 25px; padding-left: 32px; padding-right: 32px; grid-template-columns: minmax(0, 1fr) minmax(340px, 400px) minmax(0, .7fr); }h1 { font-size: 44px; letter-spacing: -2.5px; }.otp-panel { padding: 24px; }.header-caption { display: none; } }
@media (max-width: 1000px) { .universe-layout { grid-template-columns: minmax(0, 1fr) minmax(340px, 430px); max-width: 880px; gap: 36px; }.universe-visual { display: none; }h1 { font-size: 49px; }.universe-header, .universe-footer { padding-left: 32px; padding-right: 32px; }.universe-footer > span:nth-child(2) { display: none; } }
@media (max-width: 680px) { .universe-header { padding: 22px; }.paperflow-logo { font-size: 20px; }.header-login { font-size: 10px; }.universe-layout { display: block; padding: 20px 22px 36px; max-width: 440px; }.universe-story { padding-bottom: 38px; }.story-kicker { font-size: 8px; margin-bottom: 20px; }h1 { font-size: 44px; letter-spacing: -2px; }h1 > span:first-child, h1 > span:nth-child(2) { display: inline; }h1 > span:first-child::after { content: ' '; }.story-subtitle { margin: 14px 0 0; font-size: 12px; }.story-subtitle br { display: none; }.story-bottom, .story-doodle { display: none; }.otp-panel { padding: 23px; border-radius: 20px; }.inbox-sticker { right: 4px; top: -16px; font-size: 8px; }.envelope-scene { height: 113px; }h2 { font-size: 27px; }.universe-footer { padding: 18px 22px; font-size: 7px; letter-spacing: 1px; }.aurora { width: 450px; height: 450px; }.aurora-purple { top: 50px; left: -200px; } }
@media (max-width: 360px) { .universe-layout { padding-left: 14px; padding-right: 14px; }.otp-panel { padding: 18px; }.header-login { max-width: 90px; }h1 { font-size: 38px; }.system-status { display: none; } }
@media (prefers-reduced-motion: reduce) { *, *::before, *::after { animation: none !important; transition: none !important; }.universe-visual { transform: none; } }
.paperflow-logo { gap: 10px; }.unlock-button { background: var(--pf-gradient); color: #140b26; border-radius: 14px; }
</style>
