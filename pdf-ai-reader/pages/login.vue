<script setup lang="ts">
import { safeAuthRedirect } from '~/utils/auth-redirect'
const route = useRoute()
const destination = computed(() => safeAuthRedirect(route.query.redirect))
const { login } = useAuth()
const email = ref('')
const password = ref('')
const showPassword = ref(false)
const busy = ref(false)
const error = ref('')
const isOn = ref(false)
const dragging = ref(false)
const dragX = ref(0)
const dragY = ref(0)
let startX = 0
let startY = 0
const fireflies = ref<Record<string, string>[]>([])
useHead({ title: 'Đăng nhập | PaperFlow' })

function toggleLamp() {
  if (busy.value) return
  isOn.value = !isOn.value
  if (!isOn.value) { fireflies.value = []; return }
  fireflies.value = Array.from({ length: 18 }, () => {
    const size = `${3 + Math.random() * 4}px`
    const style: Record<string, string> = {
      width: size, height: size, '--duration': `${35 + Math.random() * 30}s`, '--delay': `${Math.random() * 3}s`,
    }
    for (let n = 1; n <= 5; n++) {
      style[`--x${n}`] = `${Math.random() * 100}vw`
      style[`--y${n}`] = `${Math.random() * 100}vh`
    }
    return style
  })
}
function pointerDown(event: PointerEvent) {
  if (busy.value || event.button !== 0) return
  dragging.value = true
  startX = event.clientX; startY = event.clientY
  dragX.value = 0; dragY.value = 0
  ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
}
function pointerMove(event: PointerEvent) {
  if (!dragging.value) return
  dragX.value = Math.max(-35, Math.min(35, (event.clientX - startX) * .3))
  dragY.value = Math.min(180, Math.max(0, event.clientY - startY) * .6)
}
function pointerUp(event: PointerEvent) {
  if (!dragging.value) return
  const pulled = dragY.value > 30
  const clicked = Math.abs(event.clientX - startX) < 5 && Math.abs(event.clientY - startY) < 5
  resetPull()
  if (pulled || clicked) toggleLamp()
}
function resetPull() { dragging.value = false; dragX.value = 0; dragY.value = 0 }
function keyboardToggle(event: MouseEvent) { if (event.detail === 0) toggleLamp() }
async function submit() {
  if (busy.value || !isOn.value) return
  busy.value = true; error.value = ''
  try { await login(email.value.trim(), password.value); await navigateTo(destination.value) }
  catch (e: any) { error.value = e?.data?.message || 'Không thể đăng nhập. Kiểm tra email, mật khẩu và thử lại.' }
  finally { busy.value = false }
}
</script>

<template>
  <main class="room" :class="{ on: isOn }">
    <NuxtLink to="/" class="brand"><img src="/favicon.svg" width="34" height="34" alt="" />PaperFlow</NuxtLink>
    <div class="room-light" aria-hidden="true" />
    <div class="fireflies" aria-hidden="true"><span v-for="(style, index) in fireflies" :key="index" class="firefly" :style="style" /></div>
    <section class="lamp-side" aria-label="Đèn tương tác">
      <div class="lamp-container">
        <div class="lamp-glow" /><div class="lamp-head" /><div class="light-beam" />
        <div class="lamp-stem" /><div class="lamp-base" /><div class="desk-surface" />
        <svg class="string-svg" aria-hidden="true"><path :d="`M 0 0 L ${dragX} ${80 + dragY}`" /></svg>
        <button type="button" class="string-handle" :class="{ dragging }" :style="{ transform: `translate(${dragX}px, ${dragY}px)` }" :disabled="busy" :aria-pressed="isOn" :aria-label="isOn ? 'Tắt đèn' : 'Bật đèn để đăng nhập'" @pointerdown="pointerDown" @pointermove="pointerMove" @pointerup="pointerUp" @pointercancel="resetPull" @lostpointercapture="resetPull" @click="keyboardToggle" />
      </div>
      <button type="button" class="lamp-prompt" :disabled="busy" :aria-pressed="isOn" @click="toggleLamp">{{ isOn ? 'Kéo dây để tắt đèn' : 'Kéo dây hoặc bấm để bật đèn' }} <span aria-hidden="true">↓</span></button>
    </section>
    <section class="login-side" aria-label="Đăng nhập">
      <form class="glass login-card" :inert="!isOn" :aria-hidden="!isOn" :aria-busy="busy" @submit.prevent="submit">
        <div class="glass-line" />
        <div class="login-heading"><h1>Chào mừng<br />trở lại.</h1><p>Bật sáng không gian đọc của bạn.</p></div>
        <div class="inputs">
          <label class="input-wrap"><span class="sr-only">Email</span><span class="input-icon mail-icon" aria-hidden="true" /><input v-model="email" type="email" placeholder="Địa chỉ email" autocomplete="email" maxlength="254" :disabled="busy" required /></label>
          <label class="input-wrap"><span class="sr-only">Mật khẩu</span><span class="input-icon lock-icon" aria-hidden="true" /><input v-model="password" :type="showPassword ? 'text' : 'password'" placeholder="Mật khẩu" autocomplete="current-password" maxlength="128" :disabled="busy" required /><button type="button" class="password-eye" :disabled="busy" :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'" :aria-pressed="showPassword" @click="showPassword = !showPassword"><UIcon :name="showPassword ? 'i-lucide-eye-off' : 'i-lucide-eye'" /></button></label>
        </div>
        <NuxtLink to="/forgot-password" class="verify-link">Quên mật khẩu?</NuxtLink>
        <p v-if="error" role="alert" class="login-error">{{ error }}</p>
        <button class="sign-in" type="submit" :disabled="busy">{{ busy ? 'Đang đăng nhập…' : 'Đăng nhập' }} <span v-if="!busy" aria-hidden="true">→</span></button>
        <div class="divider"><span /><b>KHÔNG GIAN ĐỌC CỦA BẠN</b><span /></div>
        <p class="register-link">Chưa có tài khoản? <NuxtLink :to="{ path: '/register', query: { redirect: destination } }">Đăng ký ngay</NuxtLink></p>
        <NuxtLink to="/verify-email" class="verify-link">Xác minh email của bạn</NuxtLink>
      </form>
    </section>
  </main>
</template>

<style scoped>
.room {
  position: relative;
  width: 100%;
  min-height: 100dvh;
  min-height: max(600px, 100dvh);
  color: #fff;
  color-scheme: dark;
  display: flex;
  overflow: hidden;
  background: #080918;
  transition: background-color .8s ease;
}

.room.on { background: #101023; }

.room-light {
  position: absolute;
  top: -30%;
  left: 0;
  width: 1000px;
  height: 1000px;
  border-radius: 50%;
  pointer-events: none;
  z-index: 0;
  opacity: 0;
  transform: scale(.5);
  background: radial-gradient(circle, rgba(167,139,250,.15) 0%, rgba(139,92,246,.05) 30%, transparent 70%);
  transition: opacity .8s ease, transform .8s ease;
}

.room.on .room-light { opacity: 1; transform: scale(1.5); }

.fireflies {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 15;
  overflow: hidden;
}

.firefly {
  position: absolute;
  border-radius: 50%;
  background: #c4b5fd;
  box-shadow: 0 0 10px 3px rgba(196,181,253,.9), 0 0 20px rgba(139,92,246,.6);
  animation: fireflyMove var(--duration) ease-in-out var(--delay) infinite alternate;
}

@keyframes fireflyMove {
  0% { transform: translate(var(--x1), var(--y1)); opacity: .45; }
  25% { transform: translate(var(--x2), var(--y2)); opacity: 1; }
  50% { transform: translate(var(--x3), var(--y3)); opacity: .65; }
  75% { transform: translate(var(--x4), var(--y4)); opacity: 1; }
  100% { transform: translate(var(--x5), var(--y5)); opacity: .5; }
}

.lamp-side {
  flex: 1;
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  z-index: 5;
}

.lamp-container {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  z-index: 10;
  transform: scale(1.1);
  -webkit-tap-highlight-color: transparent;
}

.lamp-head {
  width: 140px;
  height: 50px;
  background: #151515;
  border-radius: 140px 140px 4px 4px;
  box-shadow: inset 0 2px 5px rgba(255,255,255,.1), 0 10px 20px rgba(0,0,0,.9);
  position: relative;
  z-index: 6;
  transition: box-shadow .4s ease;
  border-bottom: 2px solid #050505;
}

.room.on .lamp-head {
  box-shadow: inset 0 -3px 10px rgba(196,181,253,.4), inset 0 2px 5px rgba(255,255,255,.1);
}

.lamp-glow {
  position: absolute;
  top: 40px;
  width: 90px;
  height: 20px;
  background: #fff;
  border-radius: 50%;
  box-shadow: 0 0 40px 20px rgba(216,180,254,.9), 0 0 80px 40px rgba(167,139,250,.6);
  opacity: 0;
  transition: opacity .4s ease-in-out;
  z-index: 5;
  pointer-events: none;
}

.room.on .lamp-glow { opacity: 1; }

.light-beam {
  position: absolute;
  top: 45px;
  width: 500px;
  height: 350px;
  clip-path: polygon(40% 0, 60% 0, 100% 100%, 0 100%);
  background: linear-gradient(to bottom, rgba(216,180,254,.8) 0%, rgba(139,92,246,.2) 60%, transparent 100%);
  opacity: 0;
  transition: opacity .4s ease-in-out;
  z-index: 3;
  pointer-events: none;
  filter: blur(8px);
}

.room.on .light-beam { opacity: 1; }

.lamp-stem {
  width: 6px;
  height: 290px;
  background: linear-gradient(to right, #050505 0%, #2a2a2a 50%, #050505 100%);
  position: relative;
  z-index: 4;
  transition: background .4s ease, box-shadow .4s ease;
}

.room.on .lamp-stem {
  background: linear-gradient(to right, #050505 0%, #2a2010 50%, #050505 100%);
  box-shadow: 0 0 10px rgba(167,139,250,.15);
}

.lamp-base {
  width: 100px;
  height: 16px;
  background: #151515;
  border-radius: 30px 30px 4px 4px;
  box-shadow: inset 0 2px 5px rgba(255,255,255,.1), 0 10px 20px rgba(0,0,0,.9);
  position: relative;
  z-index: 5;
  transition: box-shadow .4s ease;
  border-bottom: 2px solid #050505;
}

.room.on .lamp-base {
  box-shadow: inset 0 2px 6px rgba(196,181,253,.3), 0 10px 20px rgba(0,0,0,.9);
}

.desk-surface {
  position: absolute;
  bottom: -40px;
  width: 550px;
  height: 100px;
  background: radial-gradient(ellipse at center, rgba(196,181,253,.25) 0%, rgba(139,92,246,.05) 50%, transparent 70%);
  border-radius: 50%;
  opacity: 0;
  transition: opacity .4s ease-in-out;
  z-index: 1;
  pointer-events: none;
}

.room.on .desk-surface { opacity: 1; }

.string-svg {
  position: absolute;
  top: 45px;
  left: calc(50% + 55px);
  width: 2px;
  height: 2px;
  overflow: visible;
  z-index: 5;
  pointer-events: none;
}

.string-svg path {
  stroke: #222;
  stroke-width: 2;
  stroke-linecap: round;
  fill: none;
}

.string-handle {
  position: absolute;
  top: 125px;
  left: calc(50% + 49px);
  width: 10px;
  height: 20px;
  background: linear-gradient(to bottom, #d8b4fe, #8955f1);
  border-radius: 5px;
  box-shadow: inset 0 2px 3px rgba(255,255,255,.5), 0 3px 5px rgba(0,0,0,.7);
  cursor: grab;
  z-index: 6;
  touch-action: none;
  user-select: none;
  transition: transform .35s cubic-bezier(.2,.8,.2,1);
}

.string-handle.dragging { cursor: grabbing; transition: none; }

.login-side {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
  padding: 88px 6% 64px 24px;
}

.login-card {
  padding: 48px;
  border-radius: 24px;
  width: 100%;
  max-width: 440px;
  display: flex;
  flex-direction: column;
  gap: 24px;
  position: relative;
  overflow: hidden;
  opacity: 0;
  transform: translateX(50px);
  filter: blur(10px);
  pointer-events: none;
  transition: opacity .6s ease .0s, transform .6s cubic-bezier(.2,.8,.2,1), filter .6s ease;
}

.room.on .login-card {
  opacity: 1;
  transform: translateX(0);
  filter: blur(0);
  pointer-events: auto;
  transition-delay: .2s;
}

.glass {
  background: rgba(255,255,255,.05);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border: 1px solid rgba(255,255,255,.1);
  box-shadow: 0 8px 32px rgba(0,0,0,.3);
}

.glass-line {
  position: absolute;
  top: 0; left: 0; right: 0;
  height: 1px;
  background: linear-gradient(90deg, transparent, rgba(255,255,255,.3), transparent);
}

.login-heading {
  text-align: center;
  margin-bottom: 8px;
}

.login-heading h1 {
  font-size: 36px;
  font-weight: 700;
  margin-bottom: 8px;
  color: #fff;
  letter-spacing: -.5px;
}

.login-heading p { color: #aaa; font-size: 15px; }

.inputs { display: flex; flex-direction: column; gap: 16px; }

.input-wrap { position: relative; display: block; }

.input-wrap input {
  width: 100%;
  padding: 16px 16px 16px 48px;
  border-radius: 16px;
  border: 1px solid rgba(255,255,255,.1);
  background: rgba(0,0,0,.4);
  color: #fff;
  font-size: 15px;
  outline: none;
  transition: border-color .3s, box-shadow .3s, background-color .3s;
  box-shadow: inset 0 2px 4px rgba(0,0,0,.5);
}

.input-wrap input:focus {
  border-color: rgba(167,139,250,.45);
  box-shadow: inset 0 2px 4px rgba(0,0,0,.5), 0 0 0 3px rgba(167,139,250,.06);
}

.input-wrap input::placeholder { color: #888; }

.input-icon {
  position: absolute;
  left: 16px;
  top: 50%;
  width: 20px;
  height: 20px;
  transform: translateY(-50%);
  z-index: 2;
  opacity: .8;
}

.mail-icon::before {
  content: '';
  position: absolute; inset: 3px 1px;
  border: 2px solid #888; border-radius: 3px;
}
.mail-icon::after {
  content: '';
  position: absolute;
  width: 9px; height: 9px;
  border-left: 2px solid #888; border-bottom: 2px solid #888;
  transform: rotate(-45deg);
  left: 5px; top: 1px;
}

.lock-icon::before {
  content: '';
  position: absolute;
  width: 12px; height: 10px;
  border: 2px solid #888; border-radius: 3px;
  left: 2px; bottom: 1px;
}
.lock-icon::after {
  content: '';
  position: absolute;
  width: 8px; height: 9px;
  border: 2px solid #888; border-bottom: 0;
  border-radius: 8px 8px 0 0;
  left: 4px; top: 0;
}

.sign-in {
  padding: 16px;
  border-radius: 16px;
  border: none;
  background: linear-gradient(105deg, #9560ff, #f5a9ec);
  color: #000;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  margin-top: 8px;
  transition: transform .2s, background-color .3s, box-shadow .3s;
}

.sign-in:hover {
  transform: scale(1.02);
  background: #c4b5fd;
  box-shadow: 0 0 20px rgba(167,139,250,.4);
}

.sign-in:active { transform: scale(.98); }

.divider { display: flex; align-items: center; gap: 12px; margin: 8px 0; }
.divider span { flex: 1; height: 1px; background: rgba(255,255,255,.1); }
.divider b { color: #888; font-size: 13px; font-weight: 500; white-space: nowrap; }

@keyframes promptArrow {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(10px); }
}

@media (max-width: 900px) {
  .room { flex-direction: column; overflow-y: auto; }
  .lamp-side { flex: 0 0 55vh; min-height: 430px; }
  .login-side { padding: 30px 24px 50px; flex: 0 0 auto; }
  .login-card { max-width: 440px; }
}

@media (max-height: 650px) and (min-width: 901px) {
  .lamp-container { transform: scale(.8); }
  .login-card { padding: 30px; gap: 16px; }
}

.brand { position: absolute; top: 28px; left: 36px; z-index: 20; color: #f6f3ff; font-weight: 600; letter-spacing: -.4px; text-decoration: none; }
.lamp-prompt { position: absolute; bottom: 12%; padding: 12px; border: 0; background: transparent; color: #b1abc7; font-size: 13px; cursor: pointer; }
.lamp-prompt span { display: block; margin-top: 6px; animation: promptArrow 2s infinite; }
.room.on .lamp-prompt { color: #c4b5fd; }
.string-handle { border: 0; padding: 0; }
.string-handle::after { content: ''; position: absolute; inset: -14px; }
.string-svg path { stroke: #a78bfa; }
.login-heading h1 { line-height: 1.15; }
.login-heading p { margin-top: 12px; }
.sign-in { display: flex; justify-content: center; align-items: center; gap: 12px; }
.sign-in:disabled { opacity: .6; cursor: wait; transform: none; }
.login-error { color: #fda4af; font-size: 13px; line-height: 1.6; }
.register-link { margin: 0; text-align: center; color: #aaa; font-size: 13px; }
.register-link a { color: #c4b5fd; font-weight: 500; }
.verify-link { text-align: center; color: #aaa; font-size: 12px; }
.room button:focus-visible, .room a:focus-visible { outline: 2px solid #c4b5fd; outline-offset: 6px; }
@media (max-width: 900px) {
  .lamp-side { flex-basis: 360px; min-height: 360px; }
  .lamp-container { transform: scale(.72); }
  .lamp-prompt { bottom: 0; }
  .brand { top: 20px; left: 24px; }
  .login-side { padding: 24px 20px 40px; }
  .login-card { padding: 30px 24px; gap: 20px; }
  .login-heading h1 { font-size: 30px; }
  .light-beam { width: 420px; }
}
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after { animation: none !important; transition: none !important; }
  .fireflies { display: none; }
}

.brand { display: flex; align-items: center; gap: 10px; font-size: 23px; font-weight: 750; }
.glass { background: #141326cc; border-color: #9380d633; border-radius: 24px; }
.sign-in { border-radius: 14px; color: #140b26; }
.sign-in:hover { background: linear-gradient(105deg, #a477ff, #f8bbef); }
.input-wrap input { border-radius: 12px; background: #08091880; }
.password-eye { position: absolute; right: 5px; top: 5px; width: 44px; height: 44px; display: grid; place-items: center; color: #b1abc7; border-radius: 12px; }.input-wrap input { padding-right: 52px; }
</style>
