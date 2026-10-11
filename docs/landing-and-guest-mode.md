# PaperFlow — Landing, Guest Reader và Account Workspace

## Luồng hiện tại

`/` luôn là landing page, kể cả khi người dùng đã đăng nhập.
CTA Try PaperFlow Free mở `/reader` mà không yêu cầu tài khoản.
Trình đọc PDF, popup dịch/giải thích/tóm tắt và chat giữ nguyên luồng Nuxt → Gemini.
Preview trên landing được ghi nhãn minh họa, không gọi Gemini hoặc giả làm phản hồi AI thật.

Guest Reader hiển thị `Guest Session`. File được mở bằng PDF.js từ bộ nhớ trình duyệt.
Không tự upload PDF, tạo bản ghi MongoDB hoặc lưu PDF/chat/bản dịch vào localStorage/IndexedDB.
Reload hoặc đóng trang sẽ mất phiên đọc tạm. Thiết lập theme/cookie đăng nhập và metadata OTP
không phải dữ liệu PDF; cơ chế auth/OTP hiện có vẫn được giữ nguyên.

Save to Library, Save Chat History và Sync mở modal thay vì ép đăng nhập.
Guest có thể chọn Create Account hoặc Maybe Later để tiếp tục đọc.
Người đã đăng nhập nhận thông báo rằng lưu trữ/đồng bộ đang được phát triển.

## Route và phiên đăng nhập

| Route | Quyền truy cập |
| --- | --- |
| `/`, `/reader`, `/login`, `/register`, `/verify-email` | Public |
| `/workspace`, `/library`, `/settings` | Phiên cookie hợp lệ |
| `/admin` | Admin; API admin kiểm tra role tại backend |

Middleware kiểm tra `/api/auth/me`, chuyển Guest đến login với `redirect` nội bộ.
Đích được giới hạn ở workspace/library/settings/admin; URL ngoài site, protocol-relative
và đường dẫn bất thường bị thay bằng `/workspace`.
Đích được truyền tiếp qua đăng ký → OTP → login.
Header dùng kết quả session phía server nên không nháy Login trước khi xác định phiên.
Public header giữ snapshot tối đa 30 giây; protected route luôn kiểm tra session lại.
Logout thu hồi phiên backend và cập nhật header về Guest.

## Phạm vi cá nhân hóa

`/library` hiện là trang thông báo trạng thái phát triển, không tạo danh sách tài liệu giả.
`/settings` hiển thị hồ sơ thực từ session ở chế độ chỉ đọc.
Chưa có API upload/quản lý PDF riêng tư, lưu lịch sử theo owner, đồng bộ, hoặc đổi hồ sơ/mật khẩu.
Tính năng Guest và Workspace hiện vẫn hoạt động trong bộ nhớ phiên.

Bước tiếp theo: thêm các API thư viện được xác thực, lấy owner từ session server,
lưu file bên ngoài static web root, kiểm tra signature/MIME/40 MB, và lọc owner trong mọi
truy vấn download/rename/delete. Không nhận ownerId từ frontend. Lịch sử, ghi chú và
bản dịch cần gắn user/document thực và kiểm tra quyền sở hữu cùng cách.

## Bảo vệ API công khai

- API Nuxt AI: 30 request/10 phút/socket IP trên mỗi process, dùng chung bucket cho chat/dịch.
- Map chỉ lưu bộ đếm/thời gian, giới hạn số IP và dọn window hết hạn; không lưu nội dung người dùng.
- Không tin X-Forwarded-For do client tự gửi. Sau reverse proxy, cần cấu hình nguồn IP đáng tin và limiter chia sẻ nếu chạy nhiều instance.
- Chặn body trước JSON parse: dịch 30 KB, chat 3 MB; validation ký tự/trang/lịch sử vẫn được giữ.
- API .NET AI có policy 30 request/10 phút/IP và giới hạn body.
- Rate limit auth áp dụng cho POST; kiểm tra session GET không tiêu hao bucket login/OTP.
- CORS chỉ cho `Frontend:Origin`; auth POST có Origin check. Local nên dùng localhost ở cả FE và BE.
- Gemini key và Resend key ở server, không đưa vào runtimeConfig.public.

## Thiết kế và file

Landing dùng midnight/purple/pink/blue theo mẫu, với hero reveal, PDF/card nổi, aurora,
cursor glow, scroll reveal, magnetic CTA và preview chọn đoạn văn/chế độ AI.
GSAP chỉ chạy ở landing và OTP; PDF reader không bị thêm animation nền.
View transition/page transition hỗ trợ điều hướng, tôn trọng reduced motion.
Favicon `/favicon.svg` là bản vector dựng theo hình tài liệu + quỹ đạo ở ảnh tham chiếu.

Các file mới:

- `pages/reader.vue`, `pages/library.vue`, `pages/settings.vue`.
- `components/SiteHeader.vue`, `LandingProductDemo.vue`, `SaveInvitation.vue`.
- `composables/useLandingMotion.ts`, `utils/auth-redirect.ts`.
- `server/middleware/ai-limits.ts`, `server/utils/rate-limit.ts`, `read-limited-json.ts`.
- `public/favicon.svg`, `tests/access.test.mjs`.

Các file cập nhật:

- `pages/index.vue`: thay redirect cũ bằng landing.
- `composables/useAuth.ts`, `middleware/auth.ts`, `middleware/admin.ts`: session/redirect.
- `pages/login.vue`, `register.vue`, `verify-email.vue`: giữ đích sau đăng nhập.
- `components/PdfReader.client.vue`, `AiChat.vue`: Guest label và Save/Sync invitation.
- `server/api/chat.post.ts`, `translate.post.ts`: body có giới hạn.
- `nuxt.config.ts`, `assets/css/main.css`: favicon và chuyển trang.
- `tests/api-smoke.mjs`: public/protected routes, favicon, giới hạn body và rate limit.
- Backend `Program.cs`, `Controllers/AuthController.cs`, `ChatController.cs`, `TranslationController.cs`: CORS và limiter.

## Chạy và kiểm tra

Từ workspace, chạy FE và BE ở hai terminal:

```powershell
npm.cmd run dev
npm.cmd run dev:backend
```

Mở `http://localhost:3000`. Không cần BE để mở và render PDF; BE cần cho account,
MongoDB/Resend cần cho registration/OTP. Nuxt Gemini cần key server để gọi AI thật.

```powershell
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
npm.cmd run test:smoke
dotnet build backend/PaperFlow.Auth.Tests --output .verification/guest-tests
dotnet .verification/guest-tests/PaperFlow.Auth.Tests.dll
```

Kiểm tra thủ công:

1. Mở `/`: landing không redirect. CTA Try mở Guest Reader.
2. Mở PDF, chọn đoạn, dịch/chat. Reload: PDF/hội thoại cũ không còn.
3. Guest bấm Save/Sync: modal; Maybe Later giữ trang đọc.
4. Guest mở `/library`: login giữ redirect. Đăng nhập xong quay về Library.
5. Member mở `/`: vẫn là landing, header có Workspace/My Library/Avatar.
6. Logout: cookie được thu hồi, header chuyển Guest. URL redirect ngoài site không được chấp nhận.
7. Kiểm tra mobile/reduced motion, tab bàn phím, demo AI và các liên kết.

Các test browser dùng account và phản hồi API giả lập chỉ trong môi trường kiểm thử.
Test backend dùng MongoDB test riêng và email giả lập. Không gửi email hoặc gọi Gemini thật.
