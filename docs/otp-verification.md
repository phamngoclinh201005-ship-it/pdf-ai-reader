# PaperFlow — Unlock your universe

## Chạy và xem giao diện

Từ workspace, khởi động MongoDB và chạy FE/BE trong hai terminal:

```powershell
npm.cmd run dev:backend
npm.cmd run dev
```

Đăng ký ở `/register`, sau đó nhập OTP tại `/verify-email`.
Cấu hình Resend và OTP secret theo [authentication.md](authentication.md).
Không có mã OTP mẫu hoặc đường tắt xác minh trong giao diện.

Để xem thiết kế trực tiếp, mở `/verify-email?email=your-email@example.com`.
Nếu chưa có metadata từ đăng ký, giao diện không tự tạo countdown 5 phút:
người dùng có thể nhập mã đã nhận hoặc gửi lại để lấy thời hạn từ backend.

## Cấu trúc implementation

- `pdf-ai-reader/pages/verify-email.vue`: bố cục ba vùng, trạng thái xác minh, hết hạn, resend và redirect login.
- `pdf-ai-reader/components/OtpInput.vue`: sáu ô OTP, bàn phím, paste, autofill và focus.
- `pdf-ai-reader/components/OtpUniverse.vue`: nhân vật headphone, robot AI và các tài liệu holographic bằng SVG/code.
- `pdf-ai-reader/composables/useOtpMotion.ts`: GSAP entrance, floating documents, mascot, shake và success timeline.
- `pdf-ai-reader/composables/useOtpChallenge.ts`: lưu metadata thời hạn theo tab, khôi phục khi refresh; không lưu mã OTP.
- `pdf-ai-reader/types/otp.ts`, `utils/otp.ts`: kiểu dữ liệu, chuẩn hóa mã và tính thời gian theo clock server.
- `pdf-ai-reader/composables/useAuth.ts`: nhận metadata khi đăng ký/resend qua các endpoint hiện có.
- `backend/PaperFlow.Api/Contracts/AuthContracts.cs`: thêm DTO `OtpTiming`.
- `backend/PaperFlow.Api/Services/AuthService.cs`, `Controllers/AuthController.cs`: trả metadata từ OTP đã lưu, giữ nguyên cooldown, xác minh, cookie và session.
- `pdf-ai-reader/pages/login.vue`: sửa handler bật đèn bằng bàn phím để Vue build production hợp lệ.
- `pdf-ai-reader/package.json` và lockfile: thêm GSAP.

## API thời hạn

Response đăng ký/resend vẫn có `message`, bổ sung `timing`:

```json
{
  "message": "Nếu tài khoản cần xác minh, mã sẽ được gửi.",
  "timing": {
    "expiresAt": "2026-10-12T12:05:00Z",
    "resendAvailableAt": "2026-10-12T12:01:00Z",
    "serverTime": "2026-10-12T12:00:00Z"
  }
}
```

Frontend chuyển các timestamp này sang clock trình duyệt và giữ deadline tuyệt đối.
Refresh không reset countdown; resend trong cooldown không đổi thời hạn mã cũ.
Đối với email không tồn tại/đã xác minh, backend giữ response chung và metadata cùng
định dạng. Countdown là thông tin UI; quyết định xác minh luôn thuộc backend.
Thành công chỉ xuất hiện sau API verify trả thành công, sau đó animation khoảng 1,3 giây
và chuyển `/login`. Reduced motion rút ngắn animation và tắt chuyển động nền.

## Kiểm thử

```powershell
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
npm.cmd run test:smoke
dotnet run --project backend/PaperFlow.Auth.Tests
```

Nếu backend đang chạy và khóa output, build test vào thư mục riêng:

```powershell
dotnet build backend/PaperFlow.Auth.Tests --output .verification/otp-tests
dotnet .verification/otp-tests/PaperFlow.Auth.Tests.dll
```

Test backend dùng MongoDB riêng tự dọn và email giả lập. Cần kiểm tra gửi email thật
bằng Resend đã cấu hình trước khi sử dụng với người dùng thực tế.
