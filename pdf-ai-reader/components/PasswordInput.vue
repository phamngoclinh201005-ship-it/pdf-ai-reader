<script setup lang="ts">
defineProps<{ modelValue: string; label: string; autocomplete?: string; disabled?: boolean; placeholder?: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
const visible = ref(false)
const id = useId()
</script>
<template>
  <div>
    <label :for="id" class="block text-sm">{{ label }}</label>
    <div class="relative">
      <input :id="id" :value="modelValue" :type="visible ? 'text' : 'password'" class="pf-input !pr-12" :autocomplete="autocomplete || 'new-password'" :disabled="disabled" :placeholder="placeholder" required minlength="8" maxlength="128" @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)" />
      <button class="pf-muted absolute bottom-0 right-1 grid size-11 place-items-center rounded-xl" type="button" :disabled="disabled" :aria-label="`${visible ? 'Ẩn' : 'Hiện'} mật khẩu: ${label}`" :aria-pressed="visible" @click="visible = !visible"><UIcon :name="visible ? 'i-lucide-eye-off' : 'i-lucide-eye'" class="size-5" /></button>
    </div>
  </div>
</template>
