# PaperFlow — Modern PDF Workspace

Trang chủ `/` là landing public. `/reader` cho phép đọc và dùng AI ở Guest Session,
không bắt buộc tài khoản; `/workspace`, `/library`, `/settings` yêu cầu session hợp lệ.
Thư viện lưu PDF riêng tư và đồng bộ vẫn đang phát triển; các nút Save/Sync hiển thị
trạng thái rõ ràng. Xem [luồng landing và Guest Mode](../docs/landing-and-guest-mode.md).

Không gian đọc PDF 3 cột bằng Nuxt 3, Vue 3, Nuxt UI 2, Tailwind CSS 3, PDF.js, Floating UI và Gemini (`@google/genai`). Nuxt UI 2 được chọn để tương thích với Tailwind 3 của dự án; dark mode dùng `@nuxtjs/color-mode` do Nuxt UI tích hợp. Icon Lucide được cài local.

## Chạy local

Yêu cầu **Node.js 22.18+ hoặc Node.js 24 LTS**, npm và Gemini API key từ [Google AI Studio](https://aistudio.google.com/apikey).

Mở terminal tại thư mục chứa `nuxt.config.ts`. Nếu đang ở thư mục ngoài cùng của workspace:

```powershell
cd pdf-ai-reader
npm.cmd install
```

Nếu chưa có `.env`, tạo từ file mẫu (không ghi đè `.env` đã có):

```powershell
Copy-Item .env.example .env
```

Điền khóa thật trong **`.env`**, không điền vào `.env.example`:

```dotenv
GEMINI_API_KEY=your_gemini_api_key_here
GEMINI_MODEL=gemini-2.5-flash
```

```powershell
npm.cmd run dev
```

Mở **http://localhost:3000**. Khởi động lại dev server khi thay đổi `.env`. Trên macOS/Linux dùng `npm` thay `npm.cmd`.

## Tính năng

- **3 cột:** danh sách trang và tiến độ đọc; vùng PDF; chat với tài liệu. Trên mobile/tablet, sidebar mở thành panel; Escape đóng panel và toolbar.
- **Cuộn PDF liên tục:** trang ngoài vùng nhìn thấy được giải phóng canvas, trang gần vùng nhìn thấy được render lại. Hỗ trợ text layer, các trang có kích thước khác nhau, zoom 35–250%, vừa chiều rộng, nhập số trang và chọn trang từ sidebar.
- **Floating AI Toolbar:** bôi đen tối đa 5000 ký tự, chọn Dịch / Giải thích / Tóm tắt / Hỏi AI / Sao chép. Không tự gọi AI khi chỉ chọn văn bản. “Hỏi AI” đính kèm đoạn chọn vào câu hỏi trong chat.
- **Chat with PDF:** trích xuất văn bản theo trang, giữ lịch sử phiên hiện tại, gợi ý câu hỏi, hiển thị trạng thái xử lý và lỗi có thể thử lại. Các trích dẫn `[Trang N]` có nút mở đúng trang.
- **Dark mode:** mặc định theo hệ thống, nút mặt trăng/mặt trời đổi theme và lưu lựa chọn trên thiết bị. Trang PDF giữ màu gốc để dễ đọc.
- **Hủy yêu cầu cũ:** đổi file hoặc đóng toolbar sẽ bỏ phản hồi cũ; chat được reset khi đổi tài liệu.

## Kiểm tra và production

```powershell
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
npm.cmd run test:smoke
```

Chạy bản build với biến môi trường runtime. Nuxt production không tự đọc file `.env` khi chạy trực tiếp bằng Node:

```powershell
$env:NUXT_GEMINI_API_KEY="your_gemini_api_key_here"
$env:NUXT_GEMINI_MODEL="gemini-2.5-flash"
npm.cmd start
```

## Cấu trúc

- `components/PdfReader.client.vue`: workspace, toolbar chọn văn bản, zoom, điều hướng, theme.
- `composables/usePdfDocument.ts`: vòng đời PDF, lazy render, text layer, trích xuất nội dung và giải phóng tài nguyên.
- `components/AiChat.vue`: hội thoại theo tài liệu, hủy yêu cầu, dẫn nguồn theo trang.
- `composables/usePdfChat.ts`: gọi API chat Nuxt, nhận pages/history/selection và hỗ trợ AbortSignal.
- `composables/usePdfTranslation.ts`: gọi API dịch/giải thích/tóm tắt Nuxt, hỗ trợ AbortSignal.
- `composables/useBackend.ts`: kiểm tra health của API .NET qua `NUXT_PUBLIC_API_BASE`.
- `types/ai.ts`: hợp đồng dữ liệu dùng chung; component không import kiểu từ thư mục server.
- `server/api/translate.post.ts`: dịch, giải thích, tóm tắt đoạn chọn.
- `server/api/chat.post.ts`: xác thực dữ liệu, chọn ngữ cảnh liên quan và gọi Gemini.
- `server/utils/gemini.ts`: cấu hình Gemini phía server và thông báo lỗi.
- `server/utils/api-error.ts`: định dạng lỗi validation và provider dùng chung.
- `tests/context.test.mjs`: kiểm tra chọn trang liên quan, ưu tiên đoạn chọn, giới hạn ngữ cảnh và tài liệu không có text.
- `scripts/start.mjs`: khởi động production với đường dẫn import.meta hợp lệ trên Windows; `start`, `preview` và smoke test dùng chung entry này.

## Phạm vi và giới hạn

File PDF được xử lý trong trình duyệt, không lưu trên server. **Khi dùng AI, đoạn chọn hoặc văn bản trích xuất được gửi đến Nuxt server và Gemini.** Tài liệu/chat chỉ tồn tại trong phiên hiện tại, chưa có thư viện lưu file hay đồng bộ tài khoản.

Giới hạn: PDF 40 MB / 2000 trang; chat trích xuất tối đa 500.000 ký tự, mỗi trang tối đa 50.000 ký tự. Mỗi câu hỏi chọn tối đa 45.000 ký tự ngữ cảnh theo từ khóa và đoạn chọn. Đây là truy hồi theo từ khóa, chưa có embedding/vector database; câu hỏi tổng quát về tài liệu rất dài có thể thiếu ngữ cảnh. UI thông báo khi nội dung bị giới hạn.

PDF scan cần OCR trước khi dịch/chat; chưa tích hợp OCR. Phản hồi AI hiện trả về trọn nội dung, chưa streaming. AI có thể trích dẫn sai: nút dẫn nguồn giúp đối chiếu với trang gốc. Model có thể thay đổi quyền truy cập/hạn mức; đặt `GEMINI_MODEL` theo model tài khoản được cấp quyền.

Trước khi triển khai công khai cần thêm xác thực và giới hạn lượt gọi API theo người dùng. Không commit `.env` và không đưa API key vào `runtimeConfig.public`.

Tài liệu tham khảo: [Nuxt UI 2](https://ui2.nuxt.com/getting-started/installation), [Gemini text generation](https://ai.google.dev/gemini-api/docs/text-generation).
