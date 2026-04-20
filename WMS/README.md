# WMS Pro (Quản lý kho nội bộ)

## Cấu hình chạy trên máy khác (laptop thuyết trình)
Dự án **không hardcode** `ConnectionStrings:DefaultConnection` và `GeminiApiKey` trong `appsettings.json` nữa.

Bạn cấu hình bằng **User Secrets** (khuyến nghị cho demo/thuyết trình).

### 1) Set secrets (PowerShell)
Mở PowerShell tại thư mục có `WMS.csproj` rồi chạy:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet user-secrets set "GeminiApiKey" "YOUR_GEMINI_KEY"
dotnet user-secrets set "DevResetToken" "some-long-random-token"
```

### 2) Run

```powershell
dotnet run
```

## Thiết lập Admin lần đầu
Nếu hệ thống **chưa có user nào**, vào `/Account/Login` sẽ tự chuyển sang trang **Setup Admin** để tạo tài khoản Admin lần đầu.

Yêu cầu mật khẩu: **tối thiểu 8 ký tự**, có **chữ hoa + chữ thường + số + ký tự đặc biệt**.

## Dev reset mật khẩu (khi bị lock hết tài khoản)
Chỉ hoạt động trong **Development**.

1) Set token:

```powershell
dotnet user-secrets set "DevResetToken" "some-long-random-token"
```

2) Vào trang: `/Account/DevResetPassword` (có link ở màn Login khi Development)


## CSRF Protection
Toàn bộ request POST đã bật CSRF (antiforgery). Các form trong Views đã được thêm token; riêng upload AI OCR (AJAX) gửi token qua header `RequestVerificationToken`.

## Demo checklist (gợi ý thuyết trình)
- Admin tạo user (Manager/Staff) + phân kho quản lý
- Staff tạo phiếu nhập (chờ duyệt) → Manager duyệt → tồn tăng
- Tạo phiếu xuất → tồn giảm → thử xuất quá tồn để show validation
- Mở báo cáo + audit trail để show lịch sử thao tác

