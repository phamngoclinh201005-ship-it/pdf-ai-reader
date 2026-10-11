# PaperFlow Authentication v1 — ASP.NET Core 10 + MongoDB + Resend + Nuxt 3

Bộ code đã được tích hợp vào PaperFlow. `/` hiện là landing public; `/reader` dùng trình đọc PDF ở Guest Mode. `/workspace`, `/library` và `/settings` yêu cầu đăng nhập. `/admin` kiểm tra quyền cả phía frontend và backend. Các route guard chạy trên server và trình duyệt; cookie phiên được chuyển tiếp khi Nuxt render server. Chi tiết: [Landing và Guest Mode](landing-and-guest-mode.md).

Phần dưới giữ hướng dẫn cấu hình của bộ module gốc. Không cần sao chép file hoặc thay `Program.cs` nữa. DTO auth nằm trong `Contracts/AuthContracts.cs`, MongoDB records nằm trong `Models/AuthModels.cs`. MongoDB.Driver đang dùng bản 3.12.0.

## Kiểm thử bản tích hợp

```powershell
dotnet run --project backend/PaperFlow.Auth.Tests
```

Test sử dụng database tạm `PaperFlowAuthTests_<random>` và tự dọn sau khi chạy. Email được giả lập, không gửi qua Resend. Kiểm tra đăng ký, cooldown gửi lại, OTP sai/hết lượt/tái sử dụng, đăng nhập, hash session và thu hồi phiên khi đăng xuất. Nếu backend đang chạy và khóa file build, build test vào output riêng rồi chạy DLL.

Chưa bao gồm lưu PDF riêng tư theo tài khoản. Các API AI hiện tại giữ nguyên luồng cũ, chưa áp dụng xác thực theo user.

## 1. Backend

Nếu đã có `backend/PaperFlow.Api`, **sao lưu trước**, sau đó chép `Controllers/AuthController.cs`, `Models/AuthModels.cs`, `Security/Crypto.cs`, `Services/{AuthStore,AuthService,ResendEmailService}.cs` vào project. Chỉ thay `Program.cs` nếu bạn chưa tích hợp Gemini; nếu đã có, **gộp** các đăng ký service/middleware vào `Program.cs` hiện tại. Không ghi đè `.csproj`: chạy `dotnet add package MongoDB.Driver` trong backend để cài bản tương thích mới nhất.

Nếu muốn thử độc lập, sử dụng nguyên thư mục `backend/PaperFlow.Api` trong ZIP.

```powershell
cd backend\PaperFlow.Api
dotnet restore
dotnet user-secrets init
dotnet user-secrets set "Resend:ApiKey" "re_xxxxxxxxx"
dotnet user-secrets set "Auth:OtpSecret" "MOT_CHUOI_BI_MAT_NGAU_NHIEN_DAI_IT_NHAT_32_KY_TU"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --no-launch-profile --urls "http://localhost:5050"
```

Khi chạy với `--no-launch-profile`, cần đặt `ASPNETCORE_ENVIRONMENT=Development` như trên để .NET tải User Secrets.

`Auth:OtpSecret` là bí mật riêng dùng HMAC mã OTP, không phải Gemini key. Có thể tạo chuỗi ngẫu nhiên bằng PowerShell:

```powershell
$secret = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "Auth:OtpSecret" $secret
```

**Resend**: cấu hình `Resend:From` trong `appsettings.json`. `onboarding@resend.dev` thường chỉ cho gửi đến email tài khoản Resend để thử nghiệm; muốn gửi OTP tới email của người dùng thật, cần xác minh domain gửi trên Resend và đổi `From` thành `PaperFlow <verify@tenmiencuaban.com>`.

MongoDB Community Server cần đang chạy ở `mongodb://localhost:27017`. Cơ sở dữ liệu `PaperFlowDB` sẽ được tạo khi ghi dữ liệu lần đầu.

## 2. Frontend

Sao chép các file trong `frontend/` vào **gốc dự án Nuxt hiện tại** (thư mục có `nuxt.config.ts`). Giữ nguyên trang PDF reader; file `pages/workspace.vue` chỉ là trang chào mẫu để thử login, nên khi tích hợp thật cần đưa reader vào đây.

Gộp cấu hình sau vào `nuxt.config.ts` đang có (không xóa cấu hình cũ):

```ts
runtimeConfig: {
  public: {
    apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5050',
  },
},
```

`.env` frontend:

```env
NUXT_PUBLIC_API_BASE=http://localhost:5050
```

Chạy `npm run dev` từ gốc Nuxt và truy cập `/register`, `/verify-email`, `/login`, `/workspace`, `/admin`.

**CORS**: `Frontend:Origin` trong backend mặc định `http://localhost:3000`. Nếu Nuxt chạy ở cổng khác, đổi giá trị cho khớp. Cookie dùng `SameSite=Lax` và `HttpOnly`; trong local nên sử dụng **localhost ở cả FE và BE** (không trộn `127.0.0.1` với `localhost`).

## 3. API

| Method | Endpoint | Mục đích |
|---|---|---|
| POST | `/api/auth/register` | Đăng ký + gửi OTP |
| POST | `/api/auth/resend-otp` | Gửi lại OTP (tối thiểu 60 giây) |
| POST | `/api/auth/verify-email` | Xác minh OTP |
| POST | `/api/auth/login` | Đăng nhập, set cookie HttpOnly |
| POST | `/api/auth/logout` | Xóa session và cookie |
| GET | `/api/auth/me` | Trả về user hiện tại |
| GET | `/api/auth/admin-check` | Chỉ Admin truy cập |
| GET | `/api/health` | Kiểm tra backend |

## 4. Cấp quyền Admin

Đăng ký và xác minh email bằng UI trước. Sau đó mở MongoDB Compass → `PaperFlowDB` → `users` → chọn đúng tài khoản của bạn → sửa trường `Role` từ `User` thành `Admin`. **Không có API tự nâng quyền**. Đăng xuất và đăng nhập lại để UI cập nhật role.

## 5. Lưu ý bảo mật trước khi triển khai công khai

- Bản này là **starter cho phát triển**, chưa phải hệ thống authentication được audit bảo mật. Giới hạn request theo IP mới là lớp bảo vệ cơ bản, chưa có rate limit theo email/thiết bị, chống bot và cơ chế khóa account đầy đủ.
- OTP lưu bằng HMAC, hết hạn 5 phút, tối đa 5 lần sai, gửi lại tối thiểu 60 giây. Khi Resend thất bại, user có thể tồn tại ở trạng thái chưa xác minh và dùng gửi lại OTP sau.
- Cookie `HttpOnly`, `SameSite=Lax`, chỉ `Secure` khi dùng hostname không phải localhost. **Production phải bật HTTPS và cookie Secure bắt buộc**.
- Dùng Origin check và CORS cho request có cookie; khi triển khai qua nhiều domain khác site cần thiết kế lại cookie/CSRF (ưu tiên reverse proxy cùng site hoặc Nuxt server proxy). Không đơn giản đổi `SameSite=None` mà không thêm CSRF token.
- Phiên đăng nhập lưu hash token trong MongoDB, logout thu hồi session. MongoDB cần xác thực và bảo vệ kết nối khi deploy.
- **Không có upload PDF riêng tư trong giai đoạn này**. Khi bổ sung upload, tất cả API phải lấy user từ session phía server và truy vấn `ownerId` từ database. Lưu PDF ngoài web root, đặt giới hạn kích thước và kiểm tra định dạng.
- Không ghi mật khẩu, OTP, Resend key hay session token vào log, GitHub hoặc ảnh chụp màn hình.
- `appsettings.json` có MongoDB local không mật khẩu chỉ để dev; production sử dụng secrets/env.
- `register` cố ý trả phản hồi chung khi email đã tồn tại, tránh lộ danh sách tài khoản. Bản demo chưa có reset password, xác minh đổi email, xóa tài khoản, và email queue.

## 6. Lệnh kiểm thử nhanh

```powershell
$body = @{ email='your_email@example.com'; displayName='Tester'; password='MyStrongPassword123!' } | ConvertTo-Json
Invoke-RestMethod -Uri 'http://localhost:5050/api/auth/register' -Method Post -ContentType 'application/json' -Body $body
```

Sau đó kiểm tra email và sử dụng giao diện `/verify-email` để nhập OTP.
