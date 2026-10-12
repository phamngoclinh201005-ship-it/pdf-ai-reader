import { defineEventHandler } from 'h3'
import { readLimitedJson } from '../utils/read-limited-json'
import { generateAnswer } from '../utils/gemini'
import { fail } from '../utils/api-error'
import { useRuntimeConfig } from '#imports'
import { cachedAi, digest } from '../utils/ai-result-cache'
import { GoogleCloudTranslationService, GeminiTranslationService, type ITranslationService } from '../utils/translation-service'
import { providerFailure } from '../utils/ai-provider-error'
export default defineEventHandler(async (event) => {
  const body = await readLimitedJson(event, 30000)
  if (typeof body?.text !== 'string' || !body.text.trim() || body.text.length > 5000) fail(400, 'Vui lòng chọn từ 1 đến 5000 ký tự.')
  const mode = body.mode ?? 'translate'
  if (!['translate', 'explain', 'summarize'].includes(mode)) fail(400, 'Chế độ AI không hợp lệ.')
  const prompts: Record<string, string> = {
    translate: 'Dịch đoạn văn sang tiếng Việt chính xác, giữ công thức, ký hiệu và ý nghĩa. Chỉ trả bản dịch.',
    explain: 'Giải thích đoạn văn bằng tiếng Việt dễ hiểu. Làm rõ thuật ngữ và cho ví dụ ngắn khi cần. Giữ công thức, không bịa thông tin.',
    summarize: 'Tóm tắt đoạn văn bằng tiếng Việt thành các ý chính ngắn gọn, giữ số liệu và kết luận quan trọng. Không thêm thông tin ngoài đoạn văn.',
  }
  const source = body.sourceLanguage ?? 'auto'
  const target = body.targetLanguage ?? 'vi'
  if (typeof source !== 'string' || typeof target !== 'string' || !/^(auto|[a-z]{2,3}(-[A-Za-z]{2,4})?)$/.test(source) || !/^[a-z]{2,3}(-[A-Za-z]{2,4})?$/.test(target)) fail(400, 'Ngôn ngữ không hợp lệ.')
  const config = useRuntimeConfig(event)
  const cloudEnabled = String(config.googleTranslationEnabled) === 'true' && !!String(config.googleTranslationApiKey).trim()
  const service: ITranslationService = cloudEnabled
    ? new GoogleCloudTranslationService(String(config.googleTranslationApiKey))
    : new GeminiTranslationService((prompt, text, tokens) => generateAnswer(event, prompt, text, tokens))
  const provider = mode === 'translate' ? service.provider : 'gemini'
  const text = body.text.trim()
  const tokens = mode === 'summarize' ? 900 : 1600
  const parameters = { text, source, target, mode, provider, model: provider === 'gemini' ? config.geminiModel : 'nmt-v2', credential: digest(String(provider === 'gemini' ? config.geminiApiKey : config.googleTranslationApiKey)), promptVersion: 'paperflow-v2' }
  const response = await cachedAi(event, parameters, async () => {
    if (mode !== 'translate') return generateAnswer(event, prompts[mode]!, text, tokens)
    try { return await service.translate({ text, source, target }) }
    catch (error) {
      if ((error as any)?.data?.message) throw error
      const failure = providerFailure(error)
      fail(failure.status === 429 ? 429 : 502, failure.kind === 'quota_exceeded' ? 'Dịch vụ dịch đã hết hạn mức.' : 'Dịch vụ dịch tạm thời không khả dụng. Kiểm tra cấu hình hoặc thử lại sau.', { ...failure, retryAfter: failure.status === 429 ? 60 : 0 })
    }
  })
  return { result: response.value, mode, cached: response.cached, provider }
})
