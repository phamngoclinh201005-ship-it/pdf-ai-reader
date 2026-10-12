export function useTextSpeech() {
  const supported = ref(false)
  const speaking = ref(false)
  const paused = ref(false)
  const error = ref('')
  const language = ref('vi-VN')
  const rate = ref(1)
  let generation = 0
  function stop() {
    ++generation
    if (supported.value) window.speechSynthesis.cancel()
    speaking.value = false; paused.value = false
  }
  function read(text: string) {
    stop(); error.value = ''
    if (!supported.value) { error.value = 'Trình duyệt này chưa hỗ trợ đọc thành tiếng.'; return }
    const clean = text.replace(/\s+/g, ' ').trim()
    if (!clean) { error.value = 'Chưa có văn bản để đọc. PDF scan cần nhận dạng chữ trước.'; return }
    const chunks = clean.match(/.{1,220}(?:\s|$)|.{1,220}/g) || []
    const current = generation
    speaking.value = true
    let index = 0
    function next() {
      if (current !== generation) return
      if (index >= chunks.length) { speaking.value = false; paused.value = false; return }
      const utterance = new SpeechSynthesisUtterance(chunks[index++]!)
      utterance.lang = language.value; utterance.rate = rate.value
      const voice = window.speechSynthesis.getVoices().find(v => v.lang.toLowerCase() === language.value.toLowerCase())
      if (voice) utterance.voice = voice
      utterance.onend = next
      utterance.onerror = () => {
        if (current !== generation) return
        error.value = 'Không phát được giọng đọc. Hãy kiểm tra giọng đọc và âm thanh của thiết bị.'
        stop()
      }
      window.speechSynthesis.speak(utterance)
    }
    next()
  }
  function togglePause() {
    if (!supported.value || !speaking.value) return
    paused.value ? window.speechSynthesis.resume() : window.speechSynthesis.pause()
    paused.value = !paused.value
  }
  onMounted(() => { supported.value = 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window })
  onBeforeUnmount(stop)
  return { supported, speaking, paused, error, language, rate, read, stop, togglePause }
}
