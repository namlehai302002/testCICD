## TÀI LIỆU THUYẾT TRÌNH — WMS Pro (Quản lý kho nội bộ)

> Mục tiêu khi thuyết trình: chứng minh hệ thống **đúng nghiệp vụ kho nội bộ**, **an toàn**, **dễ vận hành**, có **báo cáo/đối soát**, và có các điểm cộng như **Audit Trail**, **Excel**, **AI OCR**, **FEFO (hạn dùng)**.

---

## 1) Tổng quan hệ thống
- **Tên hệ thống**: WMS Pro
- **Mục đích**: quản lý vật tư/hàng hóa trong kho nội bộ, ghi nhận nghiệp vụ **nhập–xuất–chuyển–điều chỉnh**, theo dõi **tồn kho**, **cảnh báo**, **báo cáo**, và **đối soát kiểm kê**.
- **Phạm vi sử dụng**: dùng nội bộ cho nhân viên kho (ưu tiên thao tác đơn giản, dễ học, dễ vận hành).
- **Công nghệ**:
  - ASP.NET Core MVC (.NET 8)
  - EF Core + SQL Server
  - Cookie Authentication + Role-based Authorization
  - ClosedXML (xuất Excel)
  - (Tuỳ chọn) AI OCR hóa đơn bằng Gemini (upload ảnh → AI nhận diện → map vật tư)

---

## 2) Tài khoản & phân quyền (Security / Access Control)
### 2.1 Luồng đăng nhập
- Người dùng đăng nhập bằng **UserName + Password**
- Mật khẩu lưu dạng **BCrypt hash** (không lưu plaintext)
- Hệ thống dùng **Cookie Authentication**

### 2.2 Phân quyền theo Role
Các role chính:
- **Admin**: quản trị hệ thống, quản lý user/role, cấu hình kho, thao tác hệ thống.
- **Manager**: quản lý kho, duyệt phiếu nhập cần kiểm kho, xem báo cáo/audit/cảnh báo.
- **Staff**: tạo phiếu nhập/xuất/chuyển/điều chỉnh theo quyền, thao tác nghiệp vụ.
- **Viewer**: chỉ xem (tối thiểu quyền).

### 2.3 Giới hạn theo kho (Warehouse scoping)
- Nếu user được gán **WarehouseId**, hệ thống tự giới hạn dữ liệu theo kho đó:
  - chỉ xem/ tạo/ duyệt/ hủy phiếu trong đúng kho
  - báo cáo và sơ đồ kho chỉ hiển thị đúng kho
- Admin có toàn quyền xem tất cả kho.

### 2.4 Điểm bảo mật quan trọng (thầy hay hỏi)
- **CSRF protection**: toàn bộ POST được bảo vệ (anti-forgery).
- **Secrets** (Gemini API key, connection string) không hardcode trong `appsettings.json`, cấu hình qua **User Secrets**.
- **Trang reset dev**: có trang “Dev reset password” chỉ chạy trong **Development** và cần token trong User Secrets (dùng để demo khi lỡ quên mật khẩu, không phải “quên mật khẩu” qua email).
- **Bootstrap Admin an toàn**: màn hình tạo Admin lần đầu chỉ cho phép trong Development hoặc khi bật flag `System:AllowFirstAdminBootstrap=true`.
- **Đăng ký tài khoản**: tài khoản đăng ký mới ở trạng thái **chờ Admin kích hoạt** (IsActive=false).

---

## 3) Danh mục nền (Master Data)
### 3.1 Vật tư / Hàng hóa (Items)
Thông tin quản lý:
- Mã vật tư, tên, barcode/sku
- ĐVT cơ sở
- Danh mục
- Giá vốn (UnitCost)
- Ngưỡng cảnh báo: MinThreshold/MaxThreshold
- Vị trí mặc định (DefaultLocation) nếu có

### 3.2 Kho — Khu — Vị trí (Warehouse Topology)
- **Warehouse**: kho vật lý
- **Zone**: khu vực trong kho (kệ, bồn hóa chất…)
- **Location**: ô/vị trí cụ thể (rack/shelf/bin)

### 3.3 Đối tác (Partner)
- Nhà cung cấp / Khách hàng (PartnerType)

### 3.4 Đơn vị tính / Quy đổi / Đóng gói
- ĐVT cơ sở
- Quy đổi đơn vị (global hoặc theo item)
- Quy cách đóng gói (packaging)

---

## 4) Nghiệp vụ chính: Phiếu kho (Transactions)
Hệ thống quản lý “phiếu” để ghi nhận xuất/nhập và cập nhật tồn.

### 4.1 Các loại phiếu (VoucherType)
- **1 — Nhập kho**: hàng vào kho, thường cần kiểm kho → có thể “chờ duyệt”.
- **2 — Xuất kho**: hàng ra khỏi kho.
- **3 — Trả NCC**: xuất trả lại nhà cung cấp.
- **4 — Khách trả**: nhập hàng do khách trả.
- **5 — Điều chỉnh**: tăng/giảm tồn để khớp kiểm kê/đối soát.
- **6 — Chuyển kho**: chuyển từ vị trí/kho A → B.
- **7 — Nhập thành phẩm**: thành phẩm vào kho (có thể chờ duyệt).
- **8 — Xuất sản xuất**: xuất cho sản xuất.

### 4.2 Cập nhật tồn kho (Stock Update)
Khi phiếu “đã ghi sổ”:
- Tồn theo **vị trí**: `ItemLocations.Quantity`
- Tồn tổng theo **item**: `Items.CurrentStock`

Ngoài ra hệ thống hỗ trợ **theo dõi số lô (LotNumber)** và **hạn dùng (ExpiryDate)** theo từng dòng tồn tại vị trí (phục vụ truy vết và FEFO).

### 4.2.1 Logic chuẩn của “Phiếu Điều chỉnh” (Type=5) — bạn đang hỏi đây
**Phiếu điều chỉnh** dùng khi kho nội bộ cần “kéo tồn về đúng” do:
- kiểm kê thực tế lệch so với hệ thống
- hao hụt, hư hỏng, bể vỡ
- phát hiện nhập/xuất sai trước đó

**Cách hoạt động (đúng nghiệp vụ):**
- Mỗi dòng có lựa chọn **Tăng** hoặc **Giảm** (AdjustSign).
- Hệ thống quy đổi ra `BaseQty` có dấu:
  - **Tăng** → `BaseQty` dương → cộng tồn
  - **Giảm** → `BaseQty` âm → trừ tồn
- Khi duyệt/ghi sổ:
  - **Không cho âm tồn** (giảm quá mức thì chặn)
  - Điều chỉnh **Tăng** cũng áp dụng quy tắc **“1 ô 1 vật tư”** và **sức chứa vị trí** như nhập kho

**Câu nói với thầy (ngắn gọn):**
- “Điều chỉnh là chứng từ kiểm soát sai lệch. Không sửa trực tiếp tồn, mà dùng phiếu để audit được.”

### 4.3 Ràng buộc nghiệp vụ kho nội bộ (thầy hay bắt bẻ)
- **Không cho âm tồn** khi xuất/chuyển/điều chỉnh giảm.
- Quy tắc **“1 ô 1 vật tư”** khi nhập (và điều chỉnh tăng) để tránh trộn hàng (phù hợp kho nội bộ).
- **Sức chứa vị trí**: có check quá tải theo kg hoặc L (hóa chất).

### 4.4 FEFO / FIFO theo hạn dùng (điểm cộng)
- Khi **xuất kho** mà không chọn vị trí, hệ thống **auto-pick FEFO**:
  - ưu tiên vị trí có **ExpiryDate gần nhất** (hạn sắp hết xuất trước).
- Popup gợi ý vị trí xuất hiển thị thêm:
  - **HSD**
  - badge **“Hết hạn / Còn X ngày”**
  - **Số lô** (nếu có)

---

## 5) Kiểm kê & đối soát: Chốt tồn (Stock Snapshot)
### 5.1 Mục tiêu
Kho nội bộ cần đối soát định kỳ (ngày/tuần/tháng). Hệ thống có chức năng:
- **Chốt tồn (snapshot)** theo **kho + ngày**
- So sánh tồn chốt với tồn hiện tại để ra **chênh lệch**

### 5.2 Cách hoạt động
- Vào **Báo cáo → Chốt tồn**
- Chọn kho + ngày → bấm **Xem dữ liệu**:
  - Nếu **chưa có snapshot**: hệ thống hiển thị **PREVIEW** danh sách tồn hiện tại sẽ được chốt
  - Nếu **đã có snapshot**: hệ thống hiển thị snapshot đã chốt và so sánh với tồn hiện tại
- Bấm **Chốt tồn (Generate)** → popup xác nhận → hệ thống lưu vào bảng `StockSnapshots`
- Trang hiển thị:
  - SnapshotQty (tồn chốt)
  - CurrentQty (tồn hiện tại)
  - DiffQty (chênh lệch)
  - DiffValue (giá trị chênh)
- Có nút **Xuất Excel**

### 5.3 Tạo phiếu điều chỉnh từ snapshot (rất “đúng bài”)
- Nếu có chênh lệch, bấm **Tạo phiếu điều chỉnh**
- Hệ thống tự tạo nháp **phiếu Điều chỉnh (Type=5)** gồm các dòng:
  - diff > 0 → “Tăng”
  - diff < 0 → “Giảm”
- Người dùng kiểm tra rồi lưu → tồn được kéo về khớp snapshot

**Lưu ý nghiệp vụ quan trọng (để thầy hiểu đúng):**
- Snapshot là “số chốt sổ theo ngày”, **không chỉnh sửa tay**.
- Nếu sai lệch, xử lý bằng **phiếu điều chỉnh** để hệ thống có chứng từ + audit trail.

---

## 6) Cảnh báo hệ thống (Alerts)
### 6.1 Tồn thấp / phục hồi cảnh báo
- Khi tồn của item ≤ MinThreshold → tạo alert “Tồn kho thấp”
- Khi tồn phục hồi > MinThreshold → tự resolve alert

### 6.2 Cảnh báo sắp hết hạn (Expiry Alert)
- Dựa vào `ItemLocation.ExpiryDate`
- Có trang **Báo cáo → Cảnh Báo**
- Bấm **Làm mới hết hạn** để quét lại dữ liệu theo ngưỡng (ví dụ 30 ngày)
- Có thao tác **đánh dấu đã xử lý**

---

## 7) Audit Trail (Nhật ký thao tác)
### 7.1 Mục tiêu
Kho nội bộ cần truy vết: “Ai sửa gì, lúc nào, từ đâu”.

### 7.2 Cách hoạt động
EF Core intercept SaveChanges:
- Ghi log INSERT/UPDATE/DELETE cho các bảng quan trọng
- Lưu:
  - TableName, RecordId
  - ActionType
  - OldValue/NewValue (JSON)
  - ChangedBy, ChangedAt, IP

Trang xem: **Báo cáo → Nhật ký (Audit Trail)**, lọc theo user/bảng/ngày.

---

## 8) Xuất Excel báo cáo (điểm cộng dễ thấy)
Các báo cáo có thể xuất Excel:
- **Tồn kho** (Inventory)
- **Xuất nhập tồn / Thẻ kho** (Stock Movement)
- **Chốt tồn (Snapshot)** theo kho/ngày

---

## 9) AI OCR hóa đơn (tuỳ chọn demo)
### 9.1 Mục tiêu
Giảm nhập liệu: chụp/đưa ảnh hóa đơn → AI nhận diện danh sách vật tư.

### 9.2 Cách demo
- Trên màn tạo phiếu, bấm **Quét hóa đơn AI**
- Upload ảnh hóa đơn
- AI trả JSON, hệ thống map vật tư:
  - vật tư có sẵn → gán vào dòng
  - vật tư chưa có → (tuỳ) auto tạo mã AI-xxxxxx

Lưu ý: cần cấu hình `GeminiApiKey` trong User Secrets.

---

## 10) Kịch bản thuyết trình 30 phút (timeline chuẩn)
> Mục tiêu chấm điểm: thầy thấy bạn hiểu **nghiệp vụ kho nội bộ**, biết **kiểm soát rủi ro dữ liệu**, có **đối soát/kiểm kê**, có **báo cáo/Excel**, và có **bảo mật/audit**.

### 0:00 – 2:00 | Mở bài (giới thiệu nhanh)
**Lời thoại gợi ý:**
- “Nhóm em làm **WMS Pro – quản lý kho nội bộ**. Em tập trung vào luồng nhập–xuất–chuyển–điều chỉnh, quản lý tồn theo vị trí, cảnh báo và báo cáo.”
- “Điểm cộng chính của hệ thống là **bảo mật (CSRF, secrets)**, **đúng nghiệp vụ kho** (không âm tồn, 1 ô 1 vật tư, sức chứa), **FEFO theo hạn dùng**, **kiểm kê bằng snapshot**, **audit trail**, và **xuất Excel**.”

### 2:00 – 5:00 | Kiến trúc & dữ liệu (thầy hay hỏi “lưu gì ở đâu?”)
Bạn mở nhanh 1–2 trang rồi nói:
- **MVC + EF Core + SQL Server**
- Tồn kho có 2 lớp:
  - `ItemLocations` = tồn theo **vị trí**
  - `Items.CurrentStock` = tồn tổng theo **item**
- Phiếu kho là “nguồn phát sinh” cập nhật tồn: Nhập/Xuất/Chuyển/Điều chỉnh.

**Câu chốt để thầy dễ chấm:**
- “Tồn hiển thị nhanh theo item nhưng vẫn đảm bảo đúng vì tồn theo vị trí là sổ chi tiết; snapshot dùng để đối soát.”

### 5:00 – 9:00 | Security + phân quyền (nói gọn, đúng trọng tâm)
Bạn vào Login → đăng nhập Admin.
- Role:
  - Admin / Manager / Staff / Viewer
- Bảo mật:
  - Password dùng **BCrypt hash**
  - POST có **CSRF**
  - `ConnectionString` + `GeminiApiKey` dùng **User Secrets**, không hardcode

**Nếu thầy hỏi “quên mật khẩu?”**
- “Hiện tại có công cụ reset trong môi trường Development để demo, có token bảo vệ; production sẽ làm flow email sau.”

### 9:00 – 13:00 | Master Data (thầy hay hỏi “có danh mục không?”)
Bạn lướt nhanh:
- **Vật tư**: mã, tên, ĐVT, ngưỡng min/max, vị trí mặc định
- **Kho/Zone/Location**: cấu hình sơ đồ kho
- **Đối tác**: NCC/Khách

**Câu chốt:**
- “Phải có master data chuẩn thì chứng từ mới đúng, báo cáo mới ra.”

### 13:00 – 19:00 | Demo nghiệp vụ NHẬP kho (điểm ăn tiền)
**Bước thao tác (đúng thứ tự):**
1) Vào `Phiếu Nhập / Xuất` → tạo **Phiếu Nhập (Type=1)**  
2) Chọn đối tác, chọn vật tư, số lượng, vị trí
3) (Tuỳ chọn để ăn điểm) Nhập **Số lô** (Lot) và/hoặc **Hạn dùng** nếu vật tư có theo lô/HSD
4) Lưu phiếu → vào chi tiết → bấm **Kiểm kho & tăng tồn**

**Lời thoại gợi ý:**
- “Phiếu nhập thường cần kiểm kho. Khi duyệt, hệ thống cộng tồn theo **SL quy đổi (ĐVT tồn)** và lưu theo **vị trí + lô + hạn dùng** (nếu có) để truy vết.”
- “Có ràng buộc **1 ô 1 vật tư** và **sức chứa** để tránh trộn hàng/đổ quá tải.”

**Nếu thầy bắt bẻ “tại sao cần duyệt?”**
- “Vì kho nội bộ cần kiểm nhận/QA. Staff lập phiếu, Manager duyệt để ghi sổ.”

### 19:00 – 23:00 | Demo XUẤT kho + FEFO hạn dùng
**Bước thao tác:**
1) Tạo **Phiếu Xuất (Type=2)**  
2) Chọn vật tư → bấm gợi ý vị trí  
3) Cho thầy thấy cột **HSD (FEFO)** + badge “Còn X ngày / Hết hạn” + số lô

**Lời thoại:**
- “Xuất kho dùng **FEFO**: ưu tiên vị trí có hạn dùng gần nhất để giảm rủi ro hết hạn.”
- “Hệ thống chặn **âm tồn** khi xuất.”

### 23:00 – 26:30 | Kiểm kê: Chốt tồn (Snapshot) + tạo phiếu điều chỉnh
**Bước thao tác:**
1) Vào `Báo cáo → Chốt tồn`  
2) Chọn kho + ngày → **Generate Snapshot**
3) Show bảng so sánh SnapshotQty vs CurrentQty, Diff
4) Bấm **Tạo phiếu điều chỉnh** từ snapshot (Type=5) → lưu

**Lời thoại:**
- “Snapshot là cơ chế chốt sổ theo kho/ngày để đối soát kiểm kê.”
- “Nếu lệch, hệ thống tự tạo phiếu điều chỉnh tăng/giảm để đưa tồn về đúng.”

### 26:30 – 28:30 | Audit Trail + truy vết
**Bước thao tác:**
1) Vào `Báo cáo → Nhật ký` (Audit Trail)
2) Lọc theo bảng `Vouchers` hoặc `ItemLocations`

**Lời thoại:**
- “Audit trả lời được câu hỏi: **ai làm gì, lúc nào, từ đâu**. Kho nội bộ rất cần.”

### 28:30 – 30:00 | Excel + kết bài
**Bước thao tác:**
- Vào `Báo cáo Tồn kho` hoặc `Thẻ kho` → bấm **Xuất Excel** (download file)

**Kết bài (1–2 câu):**
- “Hệ thống đáp ứng nghiệp vụ kho nội bộ: chứng từ chuẩn, kiểm soát tồn, kiểm kê/snapshot, cảnh báo, audit, và báo cáo Excel.”
- “Nếu nâng cấp thêm, em sẽ tối ưu thêm trải nghiệm nhập liệu khi lập phiếu (quét nhanh tại màn nghiệp vụ), in tem nhãn, và quy trình duyệt 2 bước.”

---

## 10.1 Script demo nghiệp vụ kho lớn (5–7 phút)
> Dùng khi thầy yêu cầu chứng minh hệ thống phù hợp **kho quy mô lớn**: có reservation, FEFO đa lô, wave/pick task, scan confirm, post outbound và KPI vận hành.

### Mục tiêu demo
- Chứng minh xuất kho theo logic: `available = onHand - reserved`.
- Chứng minh 1 phiếu xuất lớn có thể tách ra nhiều lô/vị trí theo FEFO.
- Chứng minh quy trình thực thi kho: `Confirm Picking -> Scan Pick -> Post Outbound`.
- Chứng minh dashboard vận hành có KPI để quản lý hiệu suất.

### Dữ liệu chuẩn bị trước demo (1 phút chuẩn bị)
- Chọn 1 vật tư có tồn ở **ít nhất 2-3 vị trí/lô** với hạn dùng khác nhau.
- Đảm bảo có tồn khả dụng đủ lớn để tạo 1 phiếu xuất cần tách nhiều lô.
- Có tài khoản Manager/Admin để thao tác đầy đủ.

### Kịch bản thao tác chi tiết
#### Bước 1 — Tạo phiếu xuất lớn (Draft)
1) Vào `Phiếu Nhập / Xuất` -> tạo phiếu **Xuất kho (Type=2)**.  
2) Chọn vật tư, nhập số lượng lớn hơn tồn của 1 lô đơn lẻ (để hệ thống phải chia nhiều lô).  
3) Lưu phiếu.

**Lời thoại gợi ý**:
- “Ở kho lớn, em không trừ tồn ngay khi tạo nháp. Nháp chỉ là yêu cầu xuất.”

#### Bước 2 — Confirm Picking (tạo reservation + wave/task)
1) Vào chi tiết phiếu, bấm **Confirm Picking**.  
2) Hệ thống tự:
- tạo `Wave`,
- tạo `StockReservation` theo FEFO đa lô,
- tạo `PickTask` tương ứng từng lô/vị trí.

**Điểm cần chỉ cho thầy**:
- tồn khả dụng giảm qua `ReservedQty`, nhưng `onHand` chưa giảm ngay.
- task được tách nhiều dòng theo nhiều vị trí/lô.

**Lời thoại gợi ý**:
- “Đây là điểm khác kho lớn: hệ thống giữ chỗ trước để tránh over-allocate khi nhiều người cùng xuất.”

#### Bước 3 — Scan Pick Task
1) Vào `Wave Board` hoặc `Pick Tasks`.  
2) Chọn task, nhập scan value (barcode/lô) + qty, bấm **Scan**.  
3) Scan đến khi task done, toàn bộ task wave hoàn tất.

**Kỳ vọng**:
- task chuyển trạng thái `Open/Assigned/InProgress -> Done`,
- wave chuyển `Released -> Completed`,
- phiếu chuyển fulfillment sang trạng thái sẵn sàng ghi sổ.

#### Bước 4 — Post Outbound (ghi sổ xuất kho)
1) Quay lại phiếu, bấm **Post Outbound**.  
2) Hệ thống tiêu thụ reservation:
- reservation `Active -> Consumed`,
- tồn vị trí nguồn giảm đúng theo qty đã pick,
- `Items.CurrentStock` giảm đúng (với phiếu xuất 2/3/8).

**Lời thoại gợi ý**:
- “Lúc này mới giảm tồn thực tế. Nhờ vậy luồng kho lớn rõ ràng: đặt chỗ -> thực thi -> ghi sổ.”

#### Bước 5 — Đối soát KPI vận hành
1) Mở `Reports -> Ops KPI`.  
2) Trình bày nhanh các chỉ số:
- Open task,
- Short-pick,
- Avg phút/task,
- Fill-rate reservation.

**Lời thoại gợi ý**:
- “KPI giúp quản lý kho đo hiệu suất vận hành theo ca, không chỉ xem mỗi tồn kho.”

### Case bắt lỗi để ăn điểm (30 giây)
- Thử tạo thêm phiếu xuất vượt phần tồn khả dụng còn lại.
- Kỳ vọng: hệ thống chặn tại bước confirm/reservation với lỗi không đủ available.

**Câu chốt với thầy**:
- “Kho lớn phải quản lý theo tồn khả dụng và tiến trình thực thi. Em đã tách rõ 3 lớp: chứng từ, reservation, và execution task.”

---

## 11) Câu hỏi thầy hay hỏi & câu trả lời gợi ý
### “Tại sao dùng phiếu?”
- Phiếu là chứng từ nghiệp vụ: có mã, loại, ngày, người lập; giúp truy vết và đối soát.

### “Làm sao tránh âm kho?”
- Khi xuất/chuyển/điều chỉnh giảm, hệ thống kiểm tra tồn tại vị trí và tổng tồn, không cho âm.

### “Có kiểm kê không?”
- Có: Chốt tồn (snapshot) theo kho/ngày, đối soát chênh lệch và tự tạo phiếu điều chỉnh.

### “Có theo hạn dùng không?”
- Có: FEFO (hạn gần nhất xuất trước), UI show HSD và cảnh báo hết hạn.

### “Bảo mật: có CSRF không, secrets lưu ở đâu?”
- Có CSRF toàn app; secrets dùng User Secrets, không hardcode trong appsettings.

### “Vì sao phải lưu tồn 2 nơi (ItemLocations và Item.CurrentStock)?”
- `ItemLocations` là sổ chi tiết theo vị trí (đúng nghiệp vụ).
- `Item.CurrentStock` là số tổng để hiển thị nhanh/báo cáo nhanh.
- Khi duyệt phiếu, hệ thống cập nhật đồng thời; snapshot dùng để đối soát sai lệch.

### “Nếu 2 người cùng xuất 1 vật tư thì sao?”
- Khi duyệt/ghi sổ có kiểm tra âm tồn; hệ thống đã có **RowVersion (optimistic concurrency)** trên các bảng tồn. Khi tải cao có thể nâng cấp thêm isolation/locking theo nhu cầu.

### “Kho nội bộ thì ‘đối tác’ có cần không?”
- Có. Nhập kho thường gắn NCC, xuất có thể xuất nội bộ hoặc xuất khách hàng; đối tác giúp đối soát chứng từ và báo cáo.

### “Nếu hàng bị lỗi/thiếu thì xử lý thế nào?”
- Phiếu có hỗ trợ dữ liệu lỗi/thiếu ở backend để đúng nghiệp vụ, nhưng UI demo hiện tại tối giản cho kho nội bộ.
- Nếu cần quy trình QA chi tiết: có thể bật lại nhập lỗi/thiếu hoặc tách thành biên bản QA riêng (vẫn audit được).

### “Nhập 100 mặt hàng / 10 hóa đơn thì thao tác kiểu gì cho nhanh?”
- **Chuẩn nghiệp vụ**: thường 1 hóa đơn ↔ 1 phiếu nhập (dễ đối soát). 10 bill → 10 phiếu.
- **Cách nhanh kiểu doanh nghiệp (khuyến nghị để ăn điểm)**: dùng **Import Excel**:
  - Tải **file mẫu** (hoặc nút **Demo 100 dòng**) → upload → hệ thống tự đổ vào chi tiết phiếu.
- File demo 100 dòng hiện có sẵn **Số lô** để trình bày rõ batch tracking.
- **Cách hỗ trợ (tuỳ chọn demo)**: **Quét nhiều hóa đơn AI** (chọn nhiều ảnh 1 lần):
  - OCR từng bill → gom danh sách vật tư → **gộp dòng trùng** → người dùng kiểm tra rồi lưu.
- **Câu chốt nói với thầy**: “Em ưu tiên Excel vì ổn định và nhanh; OCR là hỗ trợ giảm nhập liệu nhưng vẫn cần người dùng review trước khi ghi sổ.”

### “Phiếu điều chỉnh (Type=5) dùng khi nào? và có giống ‘sửa tồn’ không?”
- Dùng khi kiểm kê/đối soát thấy lệch, hoặc hư hỏng/hao hụt.
- Không sửa tồn trực tiếp; dùng phiếu **Tăng/Giảm** để hệ thống có chứng từ và audit.

### “FEFO hoạt động dựa trên dữ liệu nào?”
- Dựa trên `ItemLocation.ExpiryDate` (hạn dùng theo vị trí/lô). Khi xuất, ưu tiên hạn gần nhất.

### “Snapshot khác gì ‘duyệt phiếu’?”
- Duyệt phiếu ghi nhận phát sinh giao dịch.
- Snapshot là chốt sổ theo thời điểm để kiểm kê/đối soát và tạo điều chỉnh.

### “AI OCR dùng để làm gì và có rủi ro gì?”
- Dùng để giảm nhập liệu khi nhập kho: upload ảnh hóa đơn → AI trích dữ liệu.
- Rủi ro: nhận sai → nên để người dùng kiểm tra lại trước khi lưu; API key quản lý bằng secrets.

### “Nếu mất dữ liệu hoặc ai đó phá dữ liệu?”
- Có audit trail để truy vết.
- Các endpoint ‘nguy hiểm’ (seed/reset DB) bị giới hạn theo môi trường/flag và chuyển sang POST có CSRF.

---

## 12) Ghi chú setup khi mang qua laptop thuyết trình
Trong PowerShell tại thư mục dự án:
```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=HeThongNaNaNa;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet user-secrets set "GeminiApiKey" "YOUR_GEMINI_KEY"
dotnet user-secrets set "DevResetToken" "some-long-random-token"
dotnet user-secrets set "System:AllowFirstAdminBootstrap" "true"
```

Nếu laptop chưa có DB schema:
```powershell
dotnet ef database update
```

---

## 13) Flashcard Q&A (học thuộc 5 phút)
> Mỗi câu trả lời 1–2 câu ngắn. Thầy hỏi là bạn trả lời gọn, rồi mời thầy xem demo ngay.

### 13.1 Nghiệp vụ kho
- **Vì sao dùng “phiếu” thay vì sửa tồn trực tiếp?**  
  Vì phiếu là chứng từ nghiệp vụ: có mã, loại, ngày, người lập/duyệt → dễ truy vết và đối soát.
- **Làm sao tránh âm tồn?**  
  Khi xuất/chuyển/điều chỉnh giảm, hệ thống kiểm tra tồn theo vị trí và tổng tồn, không cho âm.
- **“1 ô 1 vật tư” để làm gì?**  
  Tránh trộn hàng/nhầm lẫn (đúng kho nội bộ). Nhập và điều chỉnh tăng đều kiểm tra.
- **Sức chứa vị trí kiểm soát thế nào?**  
  Khi duyệt, hệ thống check quá tải theo kg/L (hóa chất), vượt ngưỡng là chặn.
- **Hàng lỗi/thiếu xử lý sao?**  
  Phiếu có hỗ trợ dữ liệu lỗi/thiếu ở backend để đúng nghiệp vụ, nhưng UI demo hiện tại tối giản; nếu cần QA chi tiết có thể bật lại hoặc tách biên bản QA (vẫn audit được).

### 13.2 Hạn dùng / FEFO
- **FEFO là gì?**  
  First-Expired First-Out: xuất lô gần hết hạn trước để giảm rủi ro hết hạn.
- **FEFO dựa trên dữ liệu nào?**  
  Dựa trên `ItemLocation.ExpiryDate` theo vị trí/lô. UI có badge “còn X ngày / hết hạn”.

### 13.3 Kiểm kê / Snapshot
- **Snapshot khác gì “duyệt phiếu”?**  
  Duyệt phiếu ghi nhận phát sinh; Snapshot chốt sổ theo thời điểm để kiểm kê/đối soát.
- **Lệch snapshot xử lý sao?**  
  Tạo phiếu điều chỉnh (Type=5) từ snapshot: diff > 0 tăng, diff < 0 giảm.

### 13.4 Báo cáo / Excel
- **Excel để làm gì?**  
  Xuất tồn kho, thẻ kho, snapshot để gửi quản lý/kế toán và lưu chứng từ.

### 13.5 Nhập số lượng lớn (thầy hay hỏi)
- **Nhập 100 dòng / 10 hóa đơn thao tác sao cho nhanh?**  
  Em ưu tiên **Import Excel** (file mẫu → paste 100 dòng → upload). OCR nhiều bill là hỗ trợ, vẫn cần review trước khi lưu.

### 13.6 Bảo mật / Audit
- **Mật khẩu lưu thế nào?**  
  BCrypt hash, không lưu plaintext.
- **CSRF là gì, hệ thống có không?**  
  CSRF là tấn công giả mạo request; hệ thống bật anti-forgery cho toàn bộ POST.
- **Secrets lưu ở đâu?**  
  User Secrets/env, không hardcode trong `appsettings.json`.
- **Audit Trail để làm gì?**  
  Truy vết “ai làm gì lúc nào” với dữ liệu Old/New + user + thời gian + IP.

---

## 14) Checklist trước khi lên lớp (để demo không lỗi)
### 14.1 Trước khi đi
- **DB OK**: có SQL Server instance trên laptop, chạy được app.
- **User Secrets OK**: set `DefaultConnection`, `GeminiApiKey` (nếu demo OCR), `DevResetToken`.
- **Có dữ liệu demo**:
  - 1 kho + vài zone/location
  - vài vật tư có tồn (ít nhất 1 vật tư có ExpiryDate)
  - 1–2 đối tác
  - 1 user Admin, 1 Manager, 1 Staff
- **Chuẩn bị file Excel demo**:
  - Cách 1: có sẵn 1 file Excel đã điền 50–100 dòng.
  - Cách 2 (nhanh nhất): trên màn tạo phiếu, bấm **Demo 100 dòng** để tải `WMS_ImportLines_Demo_100rows.xlsx`.
- **Ảnh hóa đơn** (1 ảnh đẹp + 2–3 ảnh khác) nếu demo OCR.

### 14.2 5 phút trước khi lên thuyết trình
- Chạy app trước (`dotnet run`) và đăng nhập sẵn Admin.
- Mở sẵn các tab: `Vouchers/Create?type=1`, `Reports/StockSnapshot`, `Reports/AuditTrail`, `Reports/Inventory`.
- Test nhanh 2 nút: **Xuất Excel** và **Tạo snapshot**.

---

## 15) Nâng cấp hardening mới nhất (production-ready)
> Phần này dùng để trả lời khi thầy hỏi: “Hệ thống có xử lý race condition / double-submit / tamper không?”

### 15.1 Chống double-submit và race ở luồng outbound
- `Confirm Picking` và `Post Outbound` đã chạy trong transaction mức `Serializable`.
- Có cơ chế idempotent:
  - nếu phiếu đã có reservation active thì không tạo lại wave/reservation,
  - trả về kết quả “đã giữ chỗ trước đó” để tránh thao tác trùng.
- Tầng DB có thêm unique filtered index cho reservation active:
  - khóa: `(VoucherId, VoucherDetailId, ItemId, LocationId, LotNumber, ExpiryDate)`
  - điều kiện: `Status = 1`
  - mục tiêu: chặn trùng dữ liệu ngay cả khi 2 request đến cùng lúc.

### 15.2 Rollback outbound đúng theo lịch sử tiêu thụ thực tế
- Khi hủy phiếu outbound đã post (2/3/6/8), hệ thống hoàn tác theo `StockReservation.ConsumedQty`, không hoàn tác “ước lượng” theo `VoucherDetail`.
- Lợi ích:
  - không lệch tồn theo vị trí/lô,
  - đúng trong trường hợp FEFO đa lô và split pick task.

### 15.3 Chuyển kho nhiều dòng cùng item: map đích chính xác
- Luồng post chuyển kho map đích theo `VoucherDetailId` (không map theo `ItemId` chung).
- Tránh lỗi đổ nhầm toàn bộ quantity vào dòng/vị trí đích đầu tiên khi 1 item có nhiều dòng.

### 15.4 Chống tamper dữ liệu kiểm kê
- `StockCountSaveDraft` không còn tin `SystemQty` từ client.
- `SystemQty` được tính lại server-side từ `ItemLocations` theo key:
  - `ItemId + LocationId + LotNumber + ExpiryDate`.
- Mục tiêu: người dùng không thể sửa HTML/JS để gian lận chênh lệch kiểm kê.

### 15.5 Bảo mật audit log
- Audit trail đã loại trừ `PasswordHash` khỏi dữ liệu log.
- Đảm bảo khả năng truy vết mà không làm lộ thông tin nhạy cảm.

---

## 16) UAT checklist “ăn điểm” (demo 3–5 phút)
> Dùng checklist này để chứng minh hệ thống “không chỉ chạy được, mà còn an toàn khi vận hành thật”.

### 16.1 Case A — Double-click Confirm Picking
**Mục tiêu**: chứng minh idempotent + chống tạo reservation trùng.

**Thao tác**:
1) Tạo 1 phiếu xuất Draft.
2) Ở màn chi tiết, bấm `Confirm Picking` liên tiếp (hoặc mở 2 tab bấm gần như cùng lúc).

**Kỳ vọng đúng**:
- Chỉ có 1 bộ reservation active cho mỗi dòng/lô/vị trí.
- Không tạo wave/task trùng.
- Hệ thống báo “đã giữ chỗ trước đó” nếu request lặp.

### 16.2 Case B — Transfer nhiều dòng cùng item
**Mục tiêu**: chứng minh map đích theo `VoucherDetailId` là đúng.

**Thao tác**:
1) Tạo phiếu chuyển kho có 2 dòng cùng item nhưng `DestLocation` khác nhau.
2) Confirm picking -> Post outbound.

**Kỳ vọng đúng**:
- Mỗi dòng tăng tồn đúng vị trí đích tương ứng.
- Không có chuyện dồn hết vào 1 vị trí đích.

### 16.3 Case C — Cancel outbound đã post
**Mục tiêu**: chứng minh rollback theo consumed reservation.

**Thao tác**:
1) Tạo phiếu xuất có FEFO tách nhiều lô.
2) Confirm -> Scan -> Post.
3) Hủy phiếu.

**Kỳ vọng đúng**:
- Tồn nguồn/lô phục hồi đúng theo lượng đã consumed.
- Không lệch tổng tồn và không lệch tồn theo từng lô/vị trí.

### 16.4 Case D — Tamper `SystemQty` kiểm kê
**Mục tiêu**: chứng minh server không tin dữ liệu client.

**Thao tác**:
1) Mở màn kiểm kê, thử sửa `SystemQty` trên trình duyệt (dev tools) trước khi submit.
2) Lưu nháp và duyệt.

**Kỳ vọng đúng**:
- Hệ thống vẫn dùng `SystemQty` từ DB.
- Chênh lệch (`DiffQty`) được tính đúng theo dữ liệu server-side.

---

## 17) Lệnh kỹ thuật đã áp dụng (để minh chứng)
```powershell
dotnet ef migrations add HardenReservationIdempotencyAndIndexes
dotnet ef database update
dotnet build
```

**Trạng thái**:
- Migration đã apply thành công.
- Build pass, không lỗi compile.

---

## 18) Script nói nhanh 5 phút (học thuộc)
> Dùng khi thầy yêu cầu trình bày ngắn, đi thẳng vào điểm ăn điểm.

### 18.0 Mở màn siêu ngắn 90 giây
- “Đề tài của em là WMS Pro cho kho nội bộ, nhưng em triển khai theo chuẩn kho lớn để vận hành an toàn khi tải cao.”
- “Hệ thống quản lý đầy đủ nhập, xuất, chuyển, điều chỉnh, kiểm kê; dữ liệu tồn được quản lý theo vị trí, lô, hạn dùng để truy vết chính xác.”
- “Điểm cốt lõi của em là tách rõ outbound thành 3 bước: `Confirm Picking -> Scan Pick Task -> Post Outbound`, nên không trừ tồn sớm và tránh over-allocate.”
- “Về an toàn dữ liệu, em dùng transaction nghiêm ngặt, idempotency chống bấm lặp, chặn âm tồn, FEFO khi xuất, và kiểm kê không tin dữ liệu client.”
- “Ngoài nghiệp vụ, hệ thống có role-based authorization, CSRF protection, audit trail, báo cáo Excel và KPI vận hành để quản lý theo ca.”
- “Nói ngắn gọn: hệ thống không chỉ chạy được, mà còn đúng logic kho, có kiểm soát rủi ro dữ liệu và sẵn sàng demo vận hành thực tế.”

### 18.1 Mở bài (30 giây)
- “Hệ thống của em là WMS Pro cho kho nội bộ, quản lý đầy đủ nhập, xuất, chuyển kho, điều chỉnh, kiểm kê và báo cáo.”
- “Điểm khác biệt là em làm theo chuẩn vận hành: không âm tồn, theo dõi tồn theo vị trí/lô/hạn dùng, có reservation và wave picking cho kho lớn.”

### 18.2 Nghiệp vụ cốt lõi (1 phút 30 giây)
- “Mọi phát sinh đi qua phiếu kho, không sửa tay tồn trực tiếp, nên luôn có chứng từ và audit.”
- “Tồn được quản lý 2 lớp:
  - `ItemLocations` là tồn chi tiết theo vị trí/lô/hạn dùng,
  - `Items.CurrentStock` là tồn tổng để báo cáo nhanh.”
- “Xuất kho áp dụng FEFO, ưu tiên lô gần hết hạn trước.”

### 18.3 Chuẩn kho lớn (1 phút 30 giây)
- “Luồng outbound kho lớn được tách 3 bước rõ ràng:
  - `Confirm Picking`: giữ chỗ tồn khả dụng bằng reservation,
  - `Scan Pick Task`: xác nhận thực thi tại kho,
  - `Post Outbound`: mới trừ tồn thực tế và ghi sổ.”
- “Nhờ vậy tránh over-allocate khi nhiều người thao tác đồng thời.”

### 18.4 An toàn dữ liệu (1 phút)
- “Em harden hệ thống theo hướng production:
  - transaction `Serializable` ở các bước nhạy cảm,
  - idempotent chống bấm lặp,
  - unique filtered index chống reservation trùng,
  - rollback outbound theo consumed reservation để không lệch tồn vị trí/lô.”
- “Kiểm kê không tin dữ liệu client: `SystemQty` tính lại server-side để chống tamper.”

### 18.5 Kết (30 giây)
- “Tóm lại, hệ thống không chỉ chạy được mà còn vận hành an toàn: đúng nghiệp vụ kho, có kiểm soát rủi ro dữ liệu, có audit và KPI để quản trị.”

---

## 19) Q&A phản biện khó (để luyện trước)

### 19.1 “Tại sao không trừ tồn ngay khi tạo phiếu xuất?”
- “Vì kho lớn cần tách yêu cầu xuất và thực thi xuất. Tạo phiếu chỉ là nhu cầu, còn trừ tồn chỉ khi task pick hoàn tất và post outbound.”
- “Cách này giúp tránh ghi sổ sai khi picking chưa xong hoặc bị short-pick.”

### 19.2 “Nếu 2 người cùng bấm Confirm Picking thì sao?”
- “Đã có 3 lớp bảo vệ:
  - guard idempotent ở nghiệp vụ,
  - transaction serializable ở DB transaction,
  - unique filtered index để chặn reservation active trùng.”

### 19.3 “Hủy phiếu outbound đã post có bị lệch tồn không?”
- “Không. Rollback dựa trên `ConsumedQty` của reservation đã tiêu thụ, nên khôi phục đúng từng vị trí/lô thực tế đã xuất.”

### 19.4 “Transfer cùng item nhiều dòng có đổ nhầm đích không?”
- “Đã fix map đích theo `VoucherDetailId`, không map theo `ItemId` chung nữa, nên mỗi dòng về đúng `DestLocation`.”

### 19.5 “Người dùng sửa HTML để tăng/giảm chênh kiểm kê thì sao?”
- “Phần `SystemQty` không lấy từ client. Server tự đọc lại tồn thật từ DB theo item/location/lot/expiry rồi mới tính diff.”

### 19.6 “Audit log có lộ thông tin nhạy cảm không?”
- “Đã loại `PasswordHash` khỏi audit payload. Vẫn truy vết được thao tác mà không rò rỉ dữ liệu nhạy cảm.”

### 19.7 “Tại sao cần cả ItemLocations và CurrentStock?”
- “`ItemLocations` là sổ chi tiết vận hành kho; `CurrentStock` là số tổng để hiển thị/báo cáo nhanh.”
- “Hai lớp này được cập nhật cùng lúc trong luồng ghi sổ để vừa đúng nghiệp vụ vừa tối ưu truy vấn.”

### 19.8 “FEFO có đảm bảo đúng khi một phiếu phải lấy nhiều lô không?”
- “Có. Hệ thống phân bổ FEFO đa lô theo thứ tự hạn dùng tăng dần, đồng thời kiểm tra tồn khả dụng (`onHand - reserved`).”

### 19.9 “Vì sao phải có period lock theo kho?”
- “Để khóa sổ theo ngày từng kho, tránh phát sinh/hủy/duyệt ngược kỳ đã chốt, bảo toàn tính nhất quán dữ liệu kế toán-kho.”

### 19.10 “Nếu thầy yêu cầu minh chứng kỹ thuật?”
- “Em có migration hardening riêng, đã chạy `dotnet ef database update`, và build pass sau khi áp dụng.”

---

## 20) NHAT KY DAY DU NHUNG GI DA LAM (TONG HOP TU DAU DEN CUOI)

> Muc nay dung de bao cao toan bo qua trinh thuc hien de tai.  
> Noi dung da duoc cap nhat den lan ra soat cuoi cung.

### 20.1 Phase A - Xu ly loi quantity/format ban dau
- Da sua nhom loi nhap so luong theo dau phay/cham (`1,000`, `1.000`, `1000`) de tranh sai so luong hien thi va sai quy doi.
- Dong bo logic parse/format so luong giua UI va server.
- Bo sung guard de tranh nham so luong khi quy doi giua don vi giao dich va don vi ton.

### 20.2 Phase B - Hardening nghiep vu phieu kho (critical)
- Refactor cac action nhay cam sang transaction manh (co diem da nang cap `Serializable`) de tranh race condition.
- Chot lai luong outbound theo 3 buoc:
  1) `ConfirmForPicking` (giu cho),
  2) `ConfirmPickTask` (scan thuc thi),
  3) `PostReservedOutbound` (ghi so tru ton).
- Bo sung idempotency guard cho luong giu cho de tranh bam lap tao du lieu trung.
- Chot rollback huy phieu outbound dua tren `ConsumedQty` reservation thay vi rollback "uoc luong", dam bao dung theo vi tri/lo da xuat.
- Chan am ton o nhieu lop: item-location, tong ton, rollback transfer/adjustment.

### 20.3 Phase C - FEFO/reservation/wave cho kho lon
- Trien khai FEFO cap phat theo han dung tang dan, co tinh ton kha dung (`Quantity - ReservedQty`).
- Ho tro phan bo da lo/da vi tri trong cung mot dong xuat.
- Them domain kho lon:
  - `Wave`, `WaveLine`, `PickTask`, `PickTaskScanLog`, `StockReservation`.
- Luong pick task co trang thai van hanh, scan log, va dieu kien chot xuat theo so da pick.
- Bao ve DB bang index unique filtered cho reservation active de chan reservation trung.

### 20.4 Phase D - Kiem ke, khoa ky, bao cao
- Them phieu kiem ke vi tri/lo va duyet 2 buoc.
- O duyet kiem ke:
  - tinh lai `SystemQty` server-side, khong tin client;
  - validate location thuoc dung kho;
  - tao phieu dieu chinh tu sai lech.
- Them `WarehousePeriodLock`:
  - chan tao/duyet/huy neu giao dich nam trong ky da khoa.
- Mo rong bao cao ton, movement, snapshot va canh bao.

### 20.5 Phase E - Bao mat va toan ven du lieu
- CSRF auto validate cho cac HTTP method nguy hiem.
- Role-based authorization theo route/action.
- Audit trail tu dong o `DbContext.SaveChangesAsync`.
- Loai tru du lieu nhay cam (`PasswordHash`) khoi payload audit.
- Chan tamper cross-warehouse o luong tao phieu.

### 20.6 Phase F - UI/UX de su dung noi bo, de thuyet trinh
- Chuyen giao dien sang tong mau sang, de doc tren man chieu.
- Giam kich thuoc UI theo huong compact de giam scroll.
- Don gian hoa text/menu theo ngu canh nhan vien kho.
- Them khung huong dan nhanh tren cac trang chinh.
- Them client-side filter/pagination cho cac bang lon de thao tac nhanh.
- Login duoc polish lai dung chuan noi bo (bo placeholder email mau, text ro rang, compact).

### 20.7 Phase G - Viet hoa dong bo
- Viet hoa cac nhan con sot:
  - `Ops KPI` -> `KPI van hanh`,
  - `Wave/Pick` labels -> nhan tieng Viet de hieu voi nhan vien.
- Viet hoa text chi dan tren flow xuat (`Confirm Picking`, `Post Outbound`) sang dong tu nghiep vu de hieu.

### 20.8 Phase H - Sua loi runtime Ops KPI + UI bug thuc te
- Da sua loi EF LINQ khong translate duoc o `OpsKpi` (`DateDiffMinute + DefaultIfEmpty + AverageAsync`) bang cach tinh trung binh an toan sau khi lay list.
- Da sua loi dropdown chon vat tu bi "mo/toet" do style Select2 dark khong khop giao dien sang.
- Da dong bo format so o dashboard canh bao:
  - bo `.00` thua,
  - hien thi `5` la `5`,
  - `5000` la `5,000`.

### 20.9 Phase I - Tach vai tro nguoi nhap va nguoi kiem (theo yeu cau thay)
- Them mo hinh trach nhiem tren `Voucher`:
  - `ReviewedBy`, `ReviewedAt`, `ReviewResult`, `ReviewNote`, `ResponsibilityScore`.
- Rule bat buoc:
  - nguoi kiem khong trung nguoi lap;
  - neu sai lech thi bat buoc ghi chu + diem trach nhiem.
- Them API/chuc nang chinh sai lech tung dong (`UpdateInboundDefect`) truoc khi duyet.
- Hien thi day du thong tin kiem tra tren man hinh chi tiet phieu.
- Da bo sung rang buoc DB:
  - default `ReviewResult = 1 (Pending)`,
  - check constraint diem trach nhiem trong [0..100].

### 20.10 Migration/lenh da chay trong qua trinh
- Da tao va apply migration hardening reservation/index.
- Da tao va apply migration accountability:
  - `AddInboundReviewAccountability`.
- Da tao va apply migration chuan hoa default/range:
  - `FixInboundReviewDefaultsAndConstraints`.
- Da chay `dotnet ef database update` va `dotnet build` sau moi dot fix quan trong.

### 20.11 Ket qua ra soat lan cuoi (final audit)
- Nhap kho:
  - luong tao -> kiem -> duyet tang ton hop le.
  - chinh sai lech truoc duyet co guard nghiep vu.
- Xuat kho:
  - confirm -> pick -> post ro rang, co ho tro `Partial Issue`.
  - co 2 lua chon van hanh: xuat phan da pick hoac huy phan con lai.
  - chan am ton va co rollback dung du lieu tieu thu.
- Kiem soat noi bo:
  - nguoi lap/nguoi kiem tach biet.
  - role duyet/chot nam o `Manager/Admin`.
- Bao cao/KPI:
  - trang KPI van hanh da on dinh (khong con loi runtime da bao).
- UI:
  - da de hieu, de dung voi nhan vien kho, text tieng Viet dong bo.

### 20.12 Tai lieu huong dan demo da bo sung
- Da tao file huong dan chi tiet de ban dung khi demo:
  - `HUONG_DAN_SU_DUNG_WMS_NOI_BO.md`
- Da tao cheat sheet 1 trang truoc gio bao ve:
  - `CHEAT_SHEET_BAO_VE_WMS.md`
- Noi dung theo format "noi gi + bam dau + xu ly tinh huong" cho buoi bao cao.

### 20.13 Ket luan bao cao
- He thong dat muc tieu:
  - dung logic nghiep vu kho noi bo,
  - co kiem soat rui ro du lieu,
  - co tach nhiem vu nguoi nhap/nguoi kiem,
  - co kha nang demo ro rang theo chuan kho lon.

### 20.14 Cap nhat sat gio bao ve (UI + nghiep vu)
- Da mo lai lua chon `Loai xuat` cho phieu xuat:
  - `Xuat noi bo`,
  - `Xuat ban / ra ngoai` (co doi tac).
- Da bo sung nghiep vu `Diem dat hang lai` tren form vat tu:
  - dung de kich hoat hanh dong dat hang,
  - phan biet ro voi `Ton toi thieu` (nguong canh bao).
- Da lam ro nghiep vu LOT:
  - LOT/Han dung nhap o phieu giao dich (nhap/xuat),
  - khong dat LOT co dinh o master vat tu.
- Da chinh canh bao bat buoc sang tieng Viet (khong con thong bao mac dinh tieng Anh).
- Da bo spinner tang/giam cua input so de giao dien gon, de thao tac.
- Da chuan hoa hien thi so luong khong .00 khi khong can thiet (VD: 10 thay vi 10.00).
- Da toi uu lai layout cum `Luu lech` o chi tiet phieu de khong bi lech khi demo.


