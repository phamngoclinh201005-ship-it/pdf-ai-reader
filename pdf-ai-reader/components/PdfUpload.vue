<script setup lang="ts">
defineProps<{ busy: boolean; dragging: boolean; fileName: string }>()
const emit = defineEmits<{ select: [] }>()
</script>

<template>
  <section class="upload-card" :class="{ 'is-dragging': dragging, 'is-busy': busy }" aria-labelledby="upload-title" :aria-busy="busy">
    <header class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h2 id="upload-title" class="text-lg font-semibold tracking-tight text-slate-900 dark:text-white">Tải tài liệu lên</h2>
        <p class="mt-1 text-xs text-slate-400">Một tài liệu. Vô vàn điều để khám phá.</p>
      </div>
      <span class="rounded-full bg-violet-50 px-3 py-1.5 text-[10px] font-medium text-violet-600 dark:bg-violet-500/10 dark:text-violet-300">PDF · 40 MB</span>
    </header>

    <div class="upload-stage">
      <div class="upload-file" :class="{ 'file-active': busy || dragging }" aria-live="polite">
        <div class="flex items-center gap-3">
          <span class="flex size-8 shrink-0 items-center justify-center rounded-lg bg-violet-100 text-violet-600 dark:bg-violet-500/20 dark:text-violet-300"><UIcon name="i-lucide-file-text" class="size-4" /></span>
          <div class="min-w-0 flex-1"><p class="truncate text-xs font-medium text-slate-700 dark:text-slate-200">{{ fileName || 'Tài liệu của bạn.pdf' }}</p><p class="mt-1 text-[10px] text-slate-400">{{ busy ? 'Đang chuẩn bị tài liệu…' : dragging ? 'Thả file để bắt đầu' : 'Sẵn sàng cho một phiên đọc mới' }}</p></div>
          <UIcon :name="busy ? 'i-lucide-loader-circle' : 'i-lucide-arrow-down-to-line'" class="size-4 shrink-0 text-violet-500" :class="{ 'animate-spin': busy }" />
        </div>
        <div v-if="busy" class="upload-progress" aria-label="Đang mở PDF"><span /></div>
      </div>

      <button class="upload-target" :disabled="busy" aria-label="Chọn tài liệu PDF từ thiết bị" @click="emit('select')">
        <svg viewBox="0 0 180 170" class="upload-illustration" fill="none" aria-hidden="true">
          <ellipse cx="90" cy="150" rx="41" ry="6" fill="currentColor" opacity=".06" />
          <g class="floating-document">
            <rect x="55" y="12" width="70" height="53" rx="10" fill="currentColor" opacity=".05" />
            <rect x="57" y="14" width="66" height="49" rx="8" stroke="currentColor" stroke-opacity=".25" />
            <path d="M78 27h17l9 9v19H78V27Z" fill="currentColor" fill-opacity=".04" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round" />
            <path d="M95 27v9h9M84 42h13M84 47h9" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
          </g>
          <g class="upload-basket" stroke="currentColor" stroke-width="1.1">
            <path d="m67 91 8 48c1 7 29 7 30 0l8-48" stroke-opacity=".6" />
            <path d="m72 94 7 47m0-46 5 48m1-48 2 49m7-49-1 49m8-49-4 48m11-49-8 48" stroke-opacity=".3" />
            <path d="M69 103c12 5 30 5 42 0m-40 10c11 5 27 5 38 0m-36 10c9 4 23 4 34 0m-32 10c9 4 20 4 30 0" stroke-opacity=".3" />
            <ellipse cx="90" cy="90" rx="25" ry="6" fill="currentColor" fill-opacity=".06" />
            <ellipse cx="90" cy="90" rx="18" ry="3" stroke-opacity=".4" />
          </g>
          <path class="upload-arrow" d="M90 69v10m-4-4 4 4 4-4" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M43 81v6m-3-3h6m89-40v6m-3-3h6" stroke="currentColor" stroke-opacity=".3" stroke-linecap="round" />
        </svg>
      </button>
      <p class="text-center text-sm font-medium text-slate-700 dark:text-slate-200">{{ busy ? 'Đang mở PDF của bạn' : dragging ? 'Thả vào đây nhé!' : 'Kéo và thả PDF vào đây' }}</p>
      <p class="mt-2 text-center text-xs text-slate-400">hoặc <button :disabled="busy" class="font-medium text-violet-600 underline decoration-violet-200 underline-offset-4 transition hover:text-violet-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-violet-500 disabled:opacity-50 dark:text-violet-300" @click="emit('select')">chọn file từ thiết bị</button></p>
    </div>

    <footer class="flex items-center justify-between gap-3 border-t border-slate-100 pt-4 text-[10px] text-slate-400 dark:border-slate-800">
      <span class="flex items-center gap-1.5"><UIcon name="i-lucide-shield-check" class="size-3.5" />PDF được mở trên thiết bị</span>
      <span>Tối đa 2.000 trang</span>
    </footer>
  </section>
</template>

<style scoped>
.upload-card { width: 100%; max-width: 440px; padding: 28px; border: 1px solid var(--pf-border); border-radius: 24px; background: var(--pf-surface); box-shadow: 0 20px 70px -25px rgb(60 40 100 / .14); transition: border-color .25s, box-shadow .25s, transform .25s; }
.upload-stage { padding: 36px 0 32px; }
.upload-file { max-width: 310px; margin: 0 auto; padding: 12px; border: 1px solid var(--pf-border); border-radius: 12px; background: var(--pf-bg); box-shadow: 0 8px 24px -10px rgb(124 58 237 / .16); transition: transform .35s, box-shadow .35s; }
.upload-target { display: block; width: 180px; margin: 12px auto 0; color: #8b5cf6; border-radius: 18px; outline-offset: 4px; }
.upload-target:focus-visible { outline: 2px solid #8b5cf6; }
.upload-illustration { width: 180px; height: 170px; }
.floating-document { animation: float-document 3.5s ease-in-out infinite; }
.upload-arrow { animation: arrow-drop 2s ease-in-out infinite; }
.upload-basket { transform-origin: 90px 145px; transition: transform .3s; }
.upload-target:hover .upload-basket, .is-dragging .upload-basket { transform: rotate(-5deg); }
.is-dragging { border-color: #a78bfa; box-shadow: 0 0 0 5px rgb(139 92 246 / .08), 0 20px 70px -25px rgb(60 40 100 / .2); transform: translateY(-4px); }
.file-active { transform: translateY(-4px); box-shadow: 0 8px 30px -8px rgb(124 58 237 / .23); }
.is-dragging .floating-document { animation: drop-document .9s ease-in-out infinite; }
.upload-progress { height: 3px; overflow: hidden; margin-top: 12px; border-radius: 4px; background: #ede9fe; }
.upload-progress span { display: block; width: 40%; height: 100%; background: #8b5cf6; border-radius: 4px; animation: progress-slide 1.2s ease-in-out infinite; }
@keyframes float-document { 0%,100% { transform: translateY(0); } 50% { transform: translateY(-6px); } }
@keyframes arrow-drop { 0%,100% { opacity: .3; transform: translateY(-2px); } 50% { opacity: 1; transform: translateY(3px); } }
@keyframes drop-document { 0%,100% { transform: translateY(-4px); } 50% { transform: translateY(16px); } }
@keyframes progress-slide { from { transform: translateX(-100%); } to { transform: translateX(350%); } }
@media (max-width: 480px) { .upload-card { padding: 22px; } .upload-stage { padding-top: 24px; } }
@media (prefers-reduced-motion: reduce) { *, *::before, *::after { animation: none !important; transition: none !important; } }
</style>
