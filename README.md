# PaperFlow workspace

Ứng dụng đọc PDF và trợ lý AI, gồm frontend Nuxt 3 và API ASP.NET Core (.NET 10).

## Cấu trúc

```text
pdf-ai-reader/                 Frontend Nuxt (giữ đường dẫn hiện có)
  components/                  Giao diện và trạng thái tương tác
  composables/                 Vòng đời PDF và lời gọi API
  types/                       Kiểu dữ liệu dùng chung cho frontend/server Nuxt
  server/api/                  Xác thực request và điều phối AI
  server/utils/                Chọn ngữ cảnh, Gemini và lỗi API
  tests/                       Test ngữ cảnh và smoke test bản build
backend/PaperFlow.Api/
  Contracts/                   DTO request cho API .NET
  Controllers/                 Route, validation và response HTTP
  Services/                    Tích hợp Gemini
  Middleware/                  Xử lý lỗi tập trung
  Properties/                  Profile chạy local
```

## Luồng hiện tại

PDF được render và trích xuất văn bản trong trình duyệt. Component gọi
`usePdfChat` / `usePdfTranslation` → API cùng origin của Nuxt → Gemini.
Nuxt chọn các trang liên quan, giữ lịch sử chat và trả nguồn theo trang.
Component quản lý trạng thái loading và hủy request theo từng phiên đọc;
composable chịu trách nhiệm gọi API.

API .NET chạy độc lập. `useBackend().checkHealth()` dùng `NUXT_PUBLIC_API_BASE`
để kiểm tra .NET. Hiện giao diện AI chưa gọi các endpoint AI .NET.
Chat .NET nhận `{ message, context }` và trả `{ answer }`, trong khi Nuxt nhận
`{ question, pages, history, selection }` và trả `{ result, sources }`.
Không đổi base URL của composable chat sang .NET khi chưa có adapter cho hai hợp đồng này.

## Chạy local

Yêu cầu Node.js 22.18+ và .NET SDK 10. Cài dependency frontend:

```powershell
npm.cmd --prefix pdf-ai-reader install
```

Cấu hình frontend trong `pdf-ai-reader/.env` theo `.env.example`.
`GEMINI_API_KEY` chỉ dùng phía Nuxt server; `NUXT_PUBLIC_API_BASE` là URL .NET.

Từ thư mục workspace, dùng hai terminal:

```powershell
npm.cmd run dev
npm.cmd run dev:backend
```

Cấu hình Gemini cho .NET bằng user secrets hoặc biến môi trường:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "YOUR_API_KEY" --project backend/PaperFlow.Api
dotnet user-secrets set "Gemini:Model" "gemini-2.5-flash" --project backend/PaperFlow.Api
```

Biến môi trường tương ứng là `Gemini__ApiKey` và `Gemini__Model`.
Giữ khóa API trong môi trường hoặc user secrets; không đưa khóa vào source.

## Kiểm tra

```powershell
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
npm.cmd run test:smoke
npm.cmd run build:backend
```

Smoke test chạy bản production Nuxt và gửi request không hợp lệ, không gọi Gemini.
API .NET có request mẫu tại `backend/PaperFlow.Api/PaperFlow.Api.http`.
Chi tiết tính năng, giới hạn PDF và cấu hình production: [README frontend](pdf-ai-reader/README.md).

## Authentication

Các trang `/register`, `/verify-email`, `/login`, `/workspace`, `/admin` đã tích hợp.
Trang chủ luôn hiển thị landing. `/reader` mở công khai cho Guest; `/workspace`,
`/library`, `/settings` được bảo vệ và giữ đích quay lại sau login.
Session được lưu trong MongoDB và nhận diện bằng cookie HttpOnly.
Backend cần MongoDB khi khởi động để tạo các index unique và TTL.

Cấu hình `Resend:ApiKey` và `Auth:OtpSecret` (ít nhất 32 ký tự) bằng user secrets;
`Resend:From` phải phù hợp domain/email được Resend cho phép gửi.
Xem [hướng dẫn authentication](docs/authentication.md) để cấu hình OTP, Admin và kiểm thử.

## Landing và Guest Mode

Landing có preview tương tác, animation và favicon SVG theo mẫu thiết kế.
Guest đọc PDF, dịch và chat trong bộ nhớ phiên; không tự upload hoặc lưu nội dung.
Khách giữ PDF/chat/kết quả AI trong bộ nhớ, reload sẽ mất. Tài khoản đăng nhập tự lưu và khôi phục qua API thư viện MongoDB riêng theo chủ sở hữu.
Xem [luồng Guest/Account và danh sách file](docs/landing-and-guest-mode.md).
