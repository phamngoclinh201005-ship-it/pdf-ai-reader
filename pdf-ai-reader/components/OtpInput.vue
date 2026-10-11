<script setup lang="ts">
import { normalizeOtp } from '~/utils/otp'

const props = defineProps<{ modelValue: string; disabled?: boolean; invalid?: boolean; success?: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
const digits = ref<string[]>(Array(6).fill(''))
const inputs: HTMLInputElement[] = []
watch(() => props.modelValue, value => {
  if (value !== digits.value.join('')) digits.value = Array.from({ length: 6 }, (_, i) => normalizeOtp(value)[i] || '')
}, { immediate: true })
function focus(index = 0) { if (!props.disabled) { inputs[index]?.focus(); inputs[index]?.select() } }
function publish() { emit('update:modelValue', digits.value.join('')) }
function fill(value: string, start: number) {
  const clean = normalizeOtp(value)
  if (!clean) return
  if (clean.length === 6) start = 0
  for (let n = 0; n < clean.length && start + n < 6; n++) digits.value[start + n] = clean[n]!
  publish()
  focus(Math.min(start + clean.length, 5))
}
function input(event: Event, index: number) {
  const element = event.target as HTMLInputElement
  if (!element.value) { digits.value[index] = ''; publish(); return }
  fill(element.value, index)
  element.value = digits.value[index] || ''
}
function paste(event: ClipboardEvent, index: number) { event.preventDefault(); fill(event.clipboardData?.getData('text') || '', index) }
function key(event: KeyboardEvent, index: number) {
  if (event.key === 'Backspace') {
    event.preventDefault()
    if (digits.value[index]) digits.value[index] = ''
    else if (index > 0) { digits.value[index - 1] = ''; focus(index - 1) }
    publish()
  } else if (event.key === 'Delete') { event.preventDefault(); digits.value[index] = ''; publish() }
  else if (event.key === 'ArrowLeft') { event.preventDefault(); focus(Math.max(0, index - 1)) }
  else if (event.key === 'ArrowRight') { event.preventDefault(); focus(Math.min(5, index + 1)) }
  else if (event.key === 'Home') { event.preventDefault(); focus(0) }
  else if (event.key === 'End') { event.preventDefault(); focus(5) }
}
defineExpose({ focus })
</script>

<template>
  <div class="otp-digits" role="group" aria-label="Mã xác minh gồm 6 chữ số" :class="{ invalid, verified: success }">
    <input v-for="(_, index) in digits" :key="index" :ref="el => { if (el) inputs[index] = el as HTMLInputElement }" :value="digits[index]" type="text" inputmode="numeric" pattern="[0-9]*" maxlength="6" :autocomplete="index === 0 ? 'one-time-code' : 'off'" :aria-label="`Chữ số ${index + 1}`" :aria-invalid="invalid || undefined" :aria-describedby="invalid ? 'otp-feedback' : undefined" :disabled="disabled" :class="{ filled: digits[index] }" @input="input($event, index)" @paste="paste($event, index)" @keydown="key($event, index)" @focus="($event.target as HTMLInputElement).select()" />
  </div>
</template>

<style scoped>
.otp-digits { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: 9px; }
input { width: 100%; min-width: 0; height: 64px; border: 1px solid #ffffff24; border-radius: 12px; background: #0b0c2099; color: #faf7ff; text-align: center; font-size: 27px; font-weight: 600; caret-color: #f472d0; outline: none; transition: border-color .2s, box-shadow .2s, background .2s; }
input:focus { border-color: #ae88ff; box-shadow: 0 0 0 3px #8b5cf628, 0 0 22px #8b5cf64a; background: #8b5cf615; }
input.filled { border-color: #a78bfa80; background: #8b5cf61c; animation: digit-pop .2s ease-out; }
.invalid input { border-color: #fb7185b3; box-shadow: 0 0 14px #f472d01a; }
.verified input { border-color: #6ee7d0; color: #b1ffe7; box-shadow: 0 0 22px #6ee7d04d; }
input:disabled { opacity: .8; }
@keyframes digit-pop { 50% { transform: scale(1.06); } }
@media (max-width: 480px) { .otp-digits { gap: 6px; } input { height: 53px; font-size: 23px; border-radius: 9px; } }
@media (prefers-reduced-motion: reduce) { input { animation: none !important; transition: none; } }
</style>
