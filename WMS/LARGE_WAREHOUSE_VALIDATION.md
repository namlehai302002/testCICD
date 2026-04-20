# Large Warehouse Validation Checklist

## 1) Các lỗi nghiệp vụ đã fix (14/04/2026)

### A. Outbound/Post/Cancel
- Fixed: Bấm `Chốt phần đã lấy` đủ 10/10 nhưng vẫn hiện `Xuất một phần`.
  - Nguyên nhân: kiểm tra reservation còn mở bằng query DB trước khi đồng bộ trạng thái đang cập nhật trong transaction.
  - Kết quả sau fix: nếu đã hết open reservation -> `FulfillmentStatus = 5 (Hoàn tất)`, `IsPosted = true`.

- Fixed: Hủy phiếu outbound đã xuất một phần có thể không hoàn tồn.
  - Nguyên nhân: nhánh rollback tồn chỉ chạy khi `IsPosted == true`; phiếu partial trước đó là `IsPosted == false`.
  - Kết quả sau fix: chỉ cần có `ConsumedQty > 0` là rollback tồn nguồn (và tồn đích nếu phiếu chuyển kho), không phụ thuộc `IsPosted`.

- Fixed: Hủy phiếu outbound có cả phần consumed + phần reservation đang active chưa release hết.
  - Kết quả sau fix: khi cancel sẽ release luôn toàn bộ reservation active còn lại và recalc `ItemLocations.ReservedQty`.

- Fixed: Nhánh `cancelRemaining` trong `PostReservedOutbound` chưa recalc đủ `ReservedQty`.
  - Kết quả sau fix: mọi trường hợp release reservation đều add location vào danh sách recalc.

### B. Confirm Picking/Wave
- Fixed: Có thể `Confirm Picking` lại sau khi phiếu đã qua bước release picking.
  - Kết quả sau fix: state machine chặn release lại, tránh tạo wave/pick task trùng.

### C. Quy đổi ĐVT (UnitConversion)
- Fixed: Rủi ro trùng conversion toàn cục (`ItemId = null`) gây lấy sai rate.
  - Kết quả sau fix:
    - Thêm unique filtered index cho cặp `(FromUomId, ToUomId)` khi `ItemId IS NULL`.
    - Lookup conversion trong tạo phiếu ưu tiên item-specific, đồng thời phát hiện dữ liệu trùng và báo lỗi rõ ràng.
    - API `GetConversionRate` cũng chặn trường hợp trùng.

### D. Phân quyền nghiệp vụ OCR/Excel import
- Fixed: Staff có thể auto-create item master từ OCR/Excel.
  - Kết quả sau fix: chỉ `Admin/Manager` mới được auto-create item; `Staff` chỉ map vật tư có sẵn.

### E. Chỉ số vận hành đợt lấy hàng/nhiệm vụ mở
- Fixed: `OpenTasks` đang tính cả nhiệm vụ đã hủy/lấy thiếu.
- Kết quả sau fix: chỉ trạng thái `1/2/3` mới tính là nhiệm vụ đang mở.

### F. Endpoint nguy hiểm
- Fixed: `Warehouses/FixData` được mở cho Admin mọi môi trường.
  - Kết quả sau fix: chỉ chạy khi Development hoặc bật cờ `System:AllowDangerOps=true`.

## 2) Migration đã áp dụng
- `20260414175458_HardenBusinessLogicAndConversionUniqueness`
  - Tạo unique filtered index cho conversion global.
  - Đã chạy `dotnet ef database update` thành công.

## 3) Checklist test nhanh trước bảo vệ
- Test outbound full pick:
  - Tạo phiếu xuất 10.
  - Confirm picking -> scan 10/10 -> chốt phần đã lấy.
  - Kỳ vọng: trạng thái `Đã ghi sổ` (không phải `Xuất một phần`).

- Test outbound partial + cancel:
  - Tạo phiếu xuất 10, pick 4, chốt phần đã lấy.
  - Hủy phiếu.
  - Kỳ vọng: tồn nguồn hoàn đúng 4 đã xuất; reservation còn lại được release; `ReservedQty` về đúng.

- Test transfer rollback:
  - Phiếu chuyển kho type 6, post một phần rồi hủy.
  - Kỳ vọng: source cộng lại, destination trừ lại đúng theo lượng consumed.

- Test phân quyền import:
  - Đăng nhập Staff, import/OCR dòng item chưa có.
  - Kỳ vọng: bị chặn, không tạo mới item master.

- Test trùng conversion:
  - Cố tạo dữ liệu trùng conversion global cùng cặp ĐVT.
  - Kỳ vọng: DB chặn bằng unique index hoặc API báo lỗi trùng.

## 4) Kết luận hiện trạng
- Luồng nhập/xuất/chuyển/hủy đã được siết lại theo chuẩn kho lớn, ưu tiên nhất quán tồn và reservation.
- Các lỗi ảnh hưởng trực tiếp dữ liệu tồn đã được sửa xong và build pass.

## 5) Cách dùng phiếu chuyển kho (dễ hiểu cho demo)

### 5.1 Mục đích
- Phiếu chuyển kho dùng để chuyển vật tư từ kho/vị trí nguồn sang kho/vị trí đích.
- Phiếu này không làm tăng/giảm tổng tồn doanh nghiệp, chỉ đổi nơi chứa.

### 5.2 Cách dùng nhanh (bản dễ demo)
- Bước 1: Vào `Tạo phiếu` -> chọn `Chuyển kho`.
- Bước 2: Chọn `Kho đích`.
- Bước 3: Ở dòng vật tư, chọn `Vật tư` + nhập `Số lượng xuất`.
- Bước 4: Nếu vật tư có quản lý lô thì nhập `Số lô`.
- Bước 5: Bấm `Lưu phiếu` -> vào chi tiết phiếu bấm `Xác nhận tạo nhiệm vụ lấy hàng` -> xác nhận lấy hàng -> `Chốt xuất kho`.

### 5.3 Ý nghĩa các cột dễ gây nhầm
- `Lấy từ vị trí`: ô/kệ nguồn đang chứa hàng để lấy ra.
- `Chuyển đến vị trí`: ô/kệ đích nhận hàng.
- `Đơn vị xuất`: đơn vị người kho thao tác thực tế (ví dụ: Bộ, Thùng).
- `Đơn vị tồn kho`: đơn vị chuẩn hệ thống dùng để tính tồn (ví dụ: Cái, Kg, L).
- `Quy đổi ra đơn vị tồn`: hệ số đổi từ đơn vị xuất sang đơn vị tồn.

### 5.4 Công thức ngắn gọn
- `Số lượng thực chuyển vào tồn` = `Số lượng xuất` x `Quy đổi ra đơn vị tồn`.
- Ví dụ: xuất `2 Bộ`, quy đổi `10 Cái/Bộ` -> hệ thống ghi nhận chuyển `20 Cái`.

### 5.5 Lưu ý nghiệp vụ quan trọng
- `Lấy từ vị trí` và `Chuyển đến vị trí` không được trùng nhau.
- Không thể chốt xuất nếu chưa xác nhận lấy hàng.
- Nếu chốt một phần, phần còn lại vẫn giữ reservation để xử lý tiếp hoặc hủy phần còn lại theo lựa chọn.

## 6) Cách sử dụng đầy đủ các chức năng chính

### 6.1 Nhập kho (Voucher type 1)
- Vào `Tạo phiếu` -> chọn `Nhập kho`.
- Chọn đối tác/NCC, nhập vật tư, số lượng, vị trí, số lô (nếu có).
- Bấm `Lưu phiếu` (trạng thái chờ kiểm).
- Manager/Admin vào chi tiết phiếu để kiểm và `Tăng tồn`.

### 6.2 Xuất kho (Voucher type 2)
- Tạo phiếu xuất, nhập vật tư + số lượng.
- Bấm `Xác nhận tạo nhiệm vụ lấy hàng` (hệ thống tạo reservation + pick task theo FEFO).
- Nhân viên vào `Nhiệm vụ lấy hàng` xác nhận lấy theo số lượng thực tế.
- Về chi tiết phiếu bấm:
  - `Chốt phần đã lấy` (xuất một phần), hoặc
  - `Chốt & hủy phần còn lại` (kết phiếu luôn phần chưa lấy).

### 6.3 Chuyển kho (Voucher type 6)
- Tạo phiếu chuyển kho, chọn kho đích.
- Chọn vật tư, số lượng xuất, vị trí nguồn/đích, số lô (nếu có).
- Lưu phiếu -> xác nhận tạo nhiệm vụ -> xác nhận lấy hàng -> chốt xuất kho.
- Kết quả đúng: tồn nguồn giảm, tồn đích tăng, tổng tồn toàn hệ thống không đổi.

### 6.4 Điều chỉnh tồn (Voucher type 5)
- Tạo phiếu điều chỉnh cho vật tư cần sửa tồn.
- Chọn `Tăng/Giảm` và nhập số lượng điều chỉnh.
- Lưu và duyệt theo quy trình để cập nhật tồn thực tế.

### 6.5 Kiểm kê vị trí/lô
- Vào màn kiểm kê tạo phiếu nháp theo kho/vị trí/lô.
- Nhập số kiểm thực tế.
- Manager/Admin duyệt phiếu kiểm kê để sinh điều chỉnh chênh lệch.
- Quy tắc maker-checker: người tạo kiểm kê không tự duyệt (trừ cấu hình đặc biệt).

### 6.6 Wave và nhiệm vụ lấy hàng
- `Bảng đợt lấy hàng`: xem wave, tiến độ, số task mở/đã xong.
- `Nhiệm vụ lấy hàng`: xác nhận lấy theo task, có thể scan mã.
- Khi task xong hết, phiếu chuyển sang trạng thái sẵn sàng chốt.

### 6.7 Hủy phiếu an toàn
- Hủy phiếu nháp: release reservation, trả lại available.
- Hủy phiếu đã post/đã xuất một phần: rollback đúng lượng đã consumed.
- Không cho hủy khi vướng khóa kỳ.

### 6.8 Khóa kỳ theo kho
- Khi kho đã khóa tới ngày X, chứng từ có ngày <= X sẽ không được sửa/hủy/post.
- Dùng để chốt số liệu kế toán-kho theo kỳ.

### 6.9 OCR hóa đơn và import Excel
- OCR/import dùng để đưa nhanh dòng vật tư vào phiếu.
- Staff chỉ được map vật tư có sẵn; không được tự tạo mới item master.
- Admin/Manager mới có quyền auto-create vật tư khi OCR/import chưa khớp.

### 6.10 Bảng chỉ số vận hành
- Theo dõi đợt đang mở, nhiệm vụ chưa xong, lấy thiếu, tỷ lệ đáp ứng.
- Dùng chỉ số vận hành để đánh giá năng suất lấy hàng và mức đáp ứng tồn.

## 7) Rà soát cuối cùng (đã fix)
- Fixed: `Approve` cho phiếu điều chỉnh (`type=5`) nay cập nhật tồn vị trí và tổng tồn đúng logic.
- Fixed: chặn duyệt trùng nhiều phiếu kiểm kê cùng kho/cùng ngày.
- Fixed: hoàn tất wave trong `ConfirmPickTask` chỉ tính task mở theo status `1/2/3`.
- Fixed: fail-open scope theo kho (non-admin thiếu claim kho) bằng cơ chế fail-closed.
- Fixed: chặn đăng nhập tài khoản non-admin chưa gán kho.
- Fixed: chặn tạo user non-admin nếu chưa gán kho.
- Fixed: chặn `PostReservedOutbound` khi chứng từ thuộc kỳ đã khóa.
- Fixed: khi chốt outbound, hệ thống đóng task/wave đồng bộ để KPI không bị treo trạng thái.
- Fixed: chặn xác nhận pick task nếu phiếu đã hủy hoặc đã ghi sổ.
- Fixed: màn `PickTasks` chỉ hiển thị danh sách wave đúng phạm vi kho được phân quyền.

## 8) Checklist demo 10 bước (bấm theo thứ tự)
- B1: Đăng nhập Manager có kho được gán.
- B2: Tạo phiếu nhập kho 1 vật tư có số lô -> duyệt tăng tồn.
- B3: Vào tồn kho kiểm tra số lượng đã tăng đúng.
- B4: Tạo phiếu xuất kho cùng vật tư -> xác nhận tạo nhiệm vụ lấy hàng.
- B5: Vào `Nhiệm vụ lấy hàng` scan/confirm đủ số lượng.
- B6: Quay lại chi tiết phiếu -> bấm `Chốt phần đã lấy` -> kiểm tra trạng thái `Đã ghi sổ`.
- B7: Tạo phiếu chuyển kho -> hoàn tất luồng pick/post -> kiểm tra nguồn giảm, đích tăng.
- B8: Tạo một phiếu xuất khác, pick một phần -> `Chốt phần đã lấy` -> rồi hủy phiếu, kiểm tra hoàn tồn đúng.
- B9: Vào chỉ số vận hành, kiểm tra đợt/nhiệm vụ đang mở không bị treo sai.
- B10: Kiểm tra phân quyền: staff không được tạo item mới qua OCR/Excel và user non-admin không gán kho sẽ không đăng nhập được.

## 9) Bản đồ chức năng (hỏi là trả lời nhanh)
- `Vouchers/Create`: tạo phiếu nhập/xuất/chuyển/điều chỉnh.
- `Vouchers/Details`: xem chi tiết phiếu, duyệt, hủy, chốt xuất.
- `Vouchers/ConfirmForPicking`: tạo đợt lấy hàng + giữ chỗ tồn + nhiệm vụ lấy hàng cho phiếu xuất.
- `Vouchers/ConfirmPickTask`: xác nhận lấy hàng theo nhiệm vụ (có quét mã).
- `Vouchers/PostReservedOutbound`: chốt xuất toàn phần/một phần, cập nhật tồn + lượng giữ chỗ.
- `Reports/StockCount`: tạo/duyệt phiếu kiểm kê, tự sinh phiếu điều chỉnh.
- `Reports/OpsKpi`: chỉ số vận hành (nhiệm vụ đang mở, đã xong, lấy thiếu, tỷ lệ đáp ứng).
- `Operations/Waves` + `Operations/PickTasks`: theo dõi đợt và nhiệm vụ lấy hàng.

## 10) Câu hỏi thầy hay hỏi và cách trả lời ngắn

### 10.1 "Vì sao chuyển kho có vị trí nguồn/đích, số lô, đơn vị quy đổi?"
- Vì chuẩn kho lớn cần truy vết theo vị trí và lô.
- Đơn vị quy đổi dùng khi thao tác theo Bộ/Thùng nhưng tồn chuẩn theo Cái/Kg/L.
- Mục tiêu: không mơ hồ dữ liệu, dễ kiểm toán và đối soát.

### 10.2 "Chốt phần đã lấy và chốt & hủy phần còn lại khác gì?"
- `Chốt phần đã lấy`: ghi nhận phần đã lấy, phần còn lại giữ lượng giữ chỗ để xử lý tiếp.
- `Chốt & hủy phần còn lại`: ghi nhận phần đã lấy và giải phóng phần chưa lấy, kết phiếu nhanh.

### 10.3 "Hủy phiếu có hoàn tồn đúng không?"
- Có. Hủy phiếu nháp thì giải phóng lượng giữ chỗ.
- Hủy phiếu đã ghi sổ hoặc đã xuất một phần thì hoàn tồn đúng lượng đã xuất thực tế.
- Có chặn âm tồn và chặn hủy nếu đã khóa kỳ.

### 10.4 "Làm sao chống sửa sai kỳ cũ?"
- Có `Khóa kỳ theo kho`.
- Chứng từ có ngày <= ngày khóa sẽ không được post/hủy/duyệt nữa.

### 10.5 "Nếu 2 người thao tác cùng lúc có bị trùng mã không?"
- Đã xử lý retry khi trùng unique cho `VoucherCode` và `WaveCode`.
- Nếu collision đồng thời, hệ thống tự thử lại thay vì văng lỗi ngay.

### 10.6 "Có thể lấy chuỗi `PasswordHash` trong DB để đăng nhập trực tiếp không?"
- Không. Hệ thống dùng `BCrypt.Verify(plainPassword, storedHash)`, tức là người dùng phải nhập mật khẩu gốc (plaintext input), không thể nhập chuỗi hash để thay thế.
- Nếu `PasswordHash` bị sai format/corrupt thì login cũng bị từ chối (fail-closed), không có fallback so sánh plaintext.
- Mục tiêu bảo mật: giảm rủi ro lộ dữ liệu nhạy cảm khi lộ DB; đồng thời audit payload đã loại trừ trường `PasswordHash`.

## 11) Kết luận ngắn để chốt buổi bảo vệ
- Hệ thống đã đạt luồng kho lớn: FEFO đa lô, reservation, wave, pick task, partial issue.
- Các lỗi logic ảnh hưởng tồn và trạng thái đã được fix và test lại.
- Build pass, phân quyền và khóa kỳ đã siết chặt, đủ để demo ổn định trước hội đồng.

## 12) Cấu hình vận hành & bảo mật (không phải nghiệp vụ kho)
- `System:AllowFirstAdminBootstrap`:
  - Cờ cho phép luồng tạo tài khoản Admin đầu tiên khi hệ thống chưa có user nào.
  - Dùng ở giai đoạn khởi tạo ban đầu để tránh bị khóa hệ thống do chưa có tài khoản quản trị.
- `DevResetToken`:
  - Token bảo vệ trang `/Account/DevResetPassword` trong môi trường Development.
  - Mục đích: hỗ trợ reset mật khẩu khi demo/dev bị lock, không phải chức năng quên mật khẩu cho môi trường production.

## 13) Bổ sung nghiệp vụ xuất nội bộ + mã vạch (bản chốt)

### 13.1 "Xuất nội bộ" là gì?
- Là xuất dùng trong nội bộ doanh nghiệp (sản xuất, bảo trì, tiêu hao nội bộ), không phải bán ra ngoài.
- Vẫn đi theo luồng kho chuẩn: tạo phiếu -> giữ chỗ tồn -> tạo đợt lấy hàng/nhiệm vụ -> xác nhận lấy -> chốt xuất.

### 13.2 Nếu 1 phiếu xuất có 10 vật tư thì hệ thống chạy thế nào?
- Mỗi dòng vật tư hợp lệ trên phiếu sẽ được tách thành nhiệm vụ lấy hàng theo tồn thực tế.
- Nếu 1 vật tư nằm ở nhiều vị trí/lô (FEFO) thì có thể tách thành nhiều nhiệm vụ; vì vậy 10 vật tư có thể ra 10 hoặc nhiều hơn 10 nhiệm vụ.
- Nguyên tắc điều độ: 1 nhiệm vụ gắn với 1 vật tư + 1 vị trí nguồn + (nếu có) 1 số lô.

### 13.3 Quy tắc mã vạch khi pick (chuẩn kho lớn)
- Không dùng 1 mã vạch cho 2 vật tư khác nhau.
- Khi xác nhận lấy hàng, mã quét phải khớp đúng nhiệm vụ hiện tại: `ItemCode`/`Barcode`/`SkuCode` hoặc số lô của chính nhiệm vụ.
- Nếu mã quét không khớp vật tư nhiệm vụ thì hệ thống chặn xác nhận, tránh pick nhầm.

### 13.4 Vì sao có lúc thấy ít nhiệm vụ hơn số dòng vật tư?
- Chỉ dòng vật tư hợp lệ mới tạo được reservation/task.
- Dòng thiếu dữ liệu hoặc không đủ tồn khả dụng sẽ không tạo nhiệm vụ và hệ thống báo lỗi ở bước release/chốt.
- Cần kiểm tra lại chi tiết phiếu, tồn vị trí/lô và log thông báo để xác định dòng nào bị chặn.

### 13.5 Ghi chú UI trang chỉ số vận hành
- Nút đúng là `Lọc` theo kho (không phải lock).
- Trang chỉ số hiện đã có cả phần thẻ tổng hợp và bảng chi tiết đợt/nhiệm vụ gần đây để đối soát nhanh khi demo.