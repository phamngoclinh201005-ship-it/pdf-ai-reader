import { defineEventHandler } from 'h3'
import { readLimitedJson } from '../utils/read-limited-json'
import { generateAnswer } from '../utils/gemini'
import { fail } from '../utils/api-error'
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
  return { result: await generateAnswer(event, prompts[mode]!, body.text.trim()), mode }
})
