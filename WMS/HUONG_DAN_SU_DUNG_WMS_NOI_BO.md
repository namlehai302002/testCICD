# HƯỚNG DẪN TRÌNH DIỄN WMS NỘI BỘ (BẠN DÙNG ĐỂ THUYẾT TRÌNH)

Tài liệu này viết theo kiểu "cầm tay chỉ việc" để bạn dùng khi demo trên lớp.  
Mục tiêu: bạn vừa thao tác vừa nói, thầy nhìn vào thấy rõ nghiệp vụ và logic hệ thống.

Nguyên tắc:

1 người KHÔNG được tự tạo và tự kiểm / tự duyệt

---

## A. Bản chất bài này (bạn nói mở đầu 30 giây)

Nói ý này:

1. Đây là WMS nội bộ theo mô hình "mỗi người một việc".
2. Nhân viên kho chỉ tạo phiếu và thao tác kho.
3. Quản lý/người kiểm là người duyệt cuối cùng.
4. Nếu nhân viên nhập sai thì hệ thống ghi nhận trách nhiệm rõ ràng, không sửa chui.
5. Luồng nhập và xuất có giao dịch an toàn + chặn lỗi để tránh âm tồn, tránh bấm lặp.

---

## B. Ma trận vai trò (bạn show nhanh để thầy thấy rõ)

### 1) Nhân viên kho
- Tạo phiếu nhập/xuất.
- Xác nhận nhiệm vụ lấy hàng bằng quét mã.
- Không được duyệt tăng tồn.
- Không được chốt xuất cuối.

### 2) Quản lý/Quản trị
- Kiểm phiếu nhập, chỉnh sai lệch.
- Duyệt tăng tồn.
- Xác nhận tạo đợt lấy hàng.
- Chốt xuất.
- Hủy phiếu, kiểm kê, khóa kỳ.

### 3) Rule nghiệp vụ bắt buộc
- Người kiểm **không được trùng** người lập phiếu.
- Nếu kiểm có sai lệch:
  - bắt buộc có ghi chú,
  - bắt buộc có điểm trách nhiệm (1..100).

---

## C. Script demo 1 - Luồng NHẬP kho có kiểm soát

> Mục tiêu: chứng minh "nhân viên nhập, người cao hơn kiểm", và có cơ chế chịu trách nhiệm khi sai.

### Bước 1: Đăng nhập Nhân viên kho, tạo phiếu nhập
Bạn thao tác:
- Vào `Tạo & tra cứu phiếu` -> `Tạo phiếu nhập`.
- Chọn kho, chọn vật tư, nhập số lượng, vị trí, lô/hạn dùng (nếu có).
- Bấm `Lưu phiếu`.

Bạn nói:
- "Bước này cho nhân viên thao tác nhanh để không nghẽn vận hành."
- "Phiếu vào trạng thái chờ kiểm, chưa tăng tồn ngay."

### Bước 2: Đăng nhập Quản lý/Quản trị, vào chi tiết phiếu
Bạn thao tác:
- Mở phiếu vừa tạo.
- Tại từng dòng, nếu phát hiện sai thì nhập `SL lỗi` + `Lý do chỉnh`, bấm `Lưu lệch`.

Bạn nói:
- "Người kiểm được quyền chỉnh sai lệch trước khi duyệt."
- "Mọi thay đổi đều có dấu vết trên phiếu."

### Bước 3: Duyệt tăng tồn
Bạn thao tác:
- Bấm `Kiểm Kho & Tăng Tồn`.
- Chọn:
  - `Đạt - không sai lệch`, hoặc
  - `Có sai lệch - đã chỉnh`.
- Nếu có sai lệch thì nhập:
  - `Điểm trách nhiệm`,
  - `Ghi chú kiểm`.
- Xác nhận duyệt.

Bạn nói:
- "Hệ thống tăng tồn theo số lượng thực nhận (đã trừ lỗi)."
- "Nếu có sai lệch thì gán trách nhiệm cho người lập phiếu."

### Bước 4: Chứng minh kết quả
Bạn thao tác:
- Ở chi tiết phiếu, chỉ thấy các mục:
  - `Người lập`
  - `Người kiểm`
  - `Kết quả kiểm`
  - `Điểm trách nhiệm`
  - `Ghi chú kiểm`

Bạn nói:
- "Nội dung này phục vụ truy vết trách nhiệm và KPI nội bộ."

---

## D. Kịch bản trình diễn 2 - Luồng XUẤT kho cho kho lớn

> Mục tiêu: chứng minh xuất kho có giữ chỗ tồn + đợt lấy hàng + nhiệm vụ lấy hàng, không xuất âm tồn.

### Bước 1: Tạo phiếu xuất (Nhân viên kho/Quản lý)
- Tạo phiếu xuất nội bộ.
- Lưu phiếu.

### Bước 2: Xác nhận tạo nhiệm vụ lấy hàng (Quản lý/Quản trị)
- Vào chi tiết phiếu.
- Bấm `Xác nhận tạo nhiệm vụ lấy hàng`.

Bạn nói:
- "Hệ thống giữ chỗ tồn khả dụng."
- "Cấp phát theo nguyên tắc gần hết hạn xuất trước (FEFO)."

### Bước 3: Xác nhận nhiệm vụ lấy hàng bằng quét mã (Nhân viên kho)
- Vào `Nhiệm vụ lấy hàng`.
- Demo quét giả lập (không cần súng quét thật):
  - Nhập tay vào ô `Mã vạch / Số lô` (ví dụ: `LOT-ABC-001` hoặc `DEMO-SCAN-01`).
  - Nhập `Số lượng` đã lấy.
  - Bấm `Xác nhận lấy`.
- Hoàn tất các nhiệm vụ.

Nói với thầy như này là chuẩn:

“Hệ thống nhận dữ liệu quét qua trường giá trị quét; khi trình diễn em nhập tay để giả lập máy quét. Khi triển khai thực tế, súng quét sẽ đổ chuỗi vào đúng ô này.”

Bạn nói:
- "Nhân viên kho chỉ thực hiện thao tác kho, chưa được chốt xuất."

### Bước 4: Chốt xuất (Quản lý/Quản trị)
- Quay lại phiếu.
- Chọn 1 trong 2 cách:
  - `Chốt phần đã lấy` (xuất một phần),
  - `Chốt & hủy phần còn lại` (đóng đơn xuất).

Bạn nói:
- "Hệ thống cho phép xuất một phần nếu doanh nghiệp cần giao trước."
- "Phần còn lại có thể giữ lại để xử lý tiếp hoặc hủy theo quyết định quản lý."

---

## E. Các điểm logic bạn cần nhấn mạnh với thầy

1. Không cho âm tồn khi xuất/chốt/hủy.
2. Hỗ trợ xuất một phần, không bắt buộc lấy đủ 100% mới được ghi sổ.
3. Hủy phiếu hoàn tác đúng số đã áp dụng.
4. Cấp phát xuất theo nguyên tắc gần hết hạn xuất trước (FEFO).
5. Khóa kỳ theo kho để chốt số liệu.
6. Chống xung đột đồng thời bằng giao dịch mức cô lập cao ở điểm nhạy cảm.

---

## F. Các câu hỏi thầy hay hỏi và cách trả lời ngắn gọn

### Hỏi: "Tại sao không cho nhân viên tự duyệt luôn?"
Trả lời:
- "Vì để tách biệt kiểm soát nội bộ, tránh một người vừa tạo vừa duyệt."

### Hỏi: "Nếu nhập sai thì xử lý sao?"
Trả lời:
- "Người kiểm chỉnh sai lệch trên dòng hàng, duyệt với kết quả có sai lệch, ghi điểm trách nhiệm và ghi chú bắt buộc."

### Hỏi: "Có tránh được xuất âm tồn không?"
Trả lời:
- "Có. Hệ thống kiểm tồn khả dụng + lượng đã giữ chỗ + lượng đã lấy trước khi ghi sổ."

### Hỏi: "Xuất nội bộ là xuất đi đâu?"
Trả lời:
- "Xuất nội bộ là xuất từ kho đến đơn vị nhận trong cùng doanh nghiệp, ví dụ xưởng sản xuất, bộ phận bảo trì, hoặc chi nhánh nội bộ."
- "Không phải xuất bán cho khách hàng bên ngoài."

### Hỏi: "Nếu hủy phiếu đã ghi sổ thì sao?"
Trả lời:
- "Hệ thống hoàn tác theo luồng gốc và chặn nếu hoàn tác gây âm tồn."

---

## G. Checklist trước giờ demo (bạn tự check)

1. Đăng nhập được 2 vai trò:
   - 1 tài khoản Nhân viên kho
   - 1 tài khoản Quản lý/Quản trị
2. Tạo sẵn 2-3 vật tư có tồn.
3. Có ít nhất 1 kho, 1 vài vị trí.
4. Chạy nhanh 1 luồng nhập và 1 luồng xuất trước khi vào lớp.
5. Mở sẵn:
   - Trang chính
   - Danh sách phiếu
   - Nhiệm vụ lấy hàng

---

## H. Thứ tự demo để an toàn (không bị rối)

1. Demo nhập có sai lệch trước.
2. Demo vai trò không trùng người lập/người kiểm.
3. Trình diễn xuất theo đợt lấy hàng/nhiệm vụ lấy hàng/chốt ghi sổ sau.
4. Cuối cùng mở bảng chỉ số vận hành (KPI) để kết luận.

Nếu run, chỉ cần bấm đúng thứ tự trên là quá đủ để thầy đánh giá "đúng nghiệp vụ".

---

## I. Ghi chú bổ sung trước giờ bảo vệ (cập nhật cuối)

### 1) Điểm đặt hàng lại là gì?
- `Điểm đặt hàng lại` là ngưỡng để bắt đầu lên kế hoạch nhập thêm hàng.
- Khi `Tồn hiện tại <= Điểm đặt hàng lại` thì phải chuẩn bị mua/nhập bổ sung.
- Khác với `Tồn tối thiểu`:
  - `Tồn tối thiểu`: ngưỡng cảnh báo rủi ro thiếu hàng.
  - `Điểm đặt hàng lại`: ngưỡng hành động đặt hàng (thường cao hơn tồn tối thiểu).

### 2) Vì sao thêm vật tư không có LOT?
- Màn `Thêm vật tư` là khai báo **danh mục vật tư (master data)**.
- `Số lô / Hạn dùng` là dữ liệu theo từng giao dịch nhập/xuất, nên được nhập ở:
  - `Phiếu nhập kho`,
  - và được lưu theo vị trí/lô trong tồn kho chi tiết.
- Nói với thầy: "Master không chứa LOT cụ thể, LOT thuộc giao dịch để truy vết theo lô thực tế."

### 3) Loại xuất đã mở lại theo nghiệp vụ thực tế
- Phiếu xuất có 2 lựa chọn:
  - `Xuất nội bộ`,
  - `Xuất bán / ra ngoài`.
- Nếu chọn xuất ra ngoài thì chọn đối tác (khách hàng/doanh nghiệp).

### 4) Luồng lấy hàng/quét mã trình diễn không cần súng quét thật
- Tại ô `Mã vạch / Số lô`, nhập tay chuỗi giả lập (ví dụ `LOT-BLN-5` hoặc `DEMO-SCAN-01`).
- Nhập số lượng đã lấy rồi bấm `Xác nhận lấy`.
- Hệ thống ghi nhận nhật ký quét bình thường để trình diễn.

### 5) Các fix UI/UX đã làm để demo mượt
- Cảnh báo bắt buộc chuyển sang tiếng Việt (không còn `This field is required`).
- Bỏ mũi tên tăng/giảm ở ô số (`input number`) để dễ nhập và đỡ rối.
- Số lượng hiển thị kiểu `N0`: `10` là `10`, không còn `10.00`.
- Cụm chỉnh sai lệch dòng hàng đã căn lại cho thẳng hàng, dễ nhìn.
- Nút duyệt đổi thành `Tăng tồn` và popup không hỏi kiểm lại lần 2.

### 6) Trạng thái xuất kho nâng cao (ăn điểm)
- Hỗ trợ `Xuất một phần`:
  - `Chốt phần đã lấy`,
  - hoặc `Chốt & hủy phần còn lại`.
- Không bắt buộc lấy đủ 100% mới được ghi sổ.

---

## J. Thầy hỏi full bài: “Cái này là gì?”

> Dùng mục này để trả lời nhanh khi thầy chỉ vào bất kỳ thành phần nào trên màn hình.

### 1) Voucher/Phiếu là gì?
- Là chứng từ nghiệp vụ kho.
- Mọi tăng/giảm tồn phải đi qua phiếu để truy vết và kiểm soát.

### 2) Giữ chỗ tồn là gì?
- Là phần tồn được giữ riêng cho một phiếu xuất.
- Mục tiêu: tránh 2 người cùng lấy trùng một lượng hàng.

### 3) Đợt lấy hàng (Wave) là gì?
- Là đợt gom nhiều nhiệm vụ lấy hàng để điều độ kho.
- Dùng trong kho lớn để điều phối nhân sự và tuyến lấy hàng.

### 4) Nhiệm vụ lấy hàng là gì?
- Là nhiệm vụ lấy hàng chi tiết theo vị trí/lô/số lượng.
- Nhân viên kho thực hiện nhiệm vụ, quản lý chốt cuối.

### 5) Giá trị quét là gì?
- Là dữ liệu quét (mã vạch/số lô) khi xác nhận lấy hàng.
- Demo có thể nhập tay để giả lập máy quét.

### 6) FEFO là gì?
- Gần hết hạn xuất trước: ưu tiên lấy lô gần hết hạn trước.
- Dùng để giảm rủi ro hàng quá hạn trong kho.

### 7) Xuất một phần là gì?
- Xuất một phần khi chưa đủ 100% số lượng yêu cầu.
- Phần còn lại có thể giữ lại xử lý tiếp hoặc hủy.

### 8) Đã ghi sổ là gì?
- Là trạng thái đã ghi nhận biến động tồn chính thức.
- Chưa ghi sổ thì chỉ là nháp/chờ xử lý.

### 9) Khóa kỳ theo kho là gì?
- Khóa giao dịch đến một ngày nhất định cho từng kho.
- Mục đích: chốt số liệu, tránh sửa ngược kỳ.

### 10) Kiểm kê vị trí là gì?
- So sánh tồn hệ thống với tồn thực tế tại vị trí/lô.
- Sai lệch được xử lý bằng phiếu điều chỉnh có phê duyệt.

### 11) Người lập vs người kiểm là gì?
- Người lập: tạo chứng từ.
- Người kiểm: xác nhận/chỉnh sai lệch/duyệt.
- Hai vai trò tách biệt để đảm bảo kiểm soát nội bộ.

### 12) Điểm trách nhiệm là gì?
- Là điểm ghi nhận mức độ sai lệch khi kiểm phát hiện lỗi nhập.
- Dùng cho KPI nội bộ, không phải tự động trừ lương.

### 12.1) Nếu thầy hỏi: "KPI nội bộ là gì?"
- Trả lời ngắn:
  - "KPI nội bộ là bộ chỉ số đo hiệu suất vận hành trong doanh nghiệp, dùng để quản lý và cải tiến quy trình."
- Trả lời theo ngữ cảnh bài WMS:
  - "Trong hệ thống này, KPI nội bộ gồm tiến độ nhiệm vụ lấy hàng, tỷ lệ lấy thiếu, tỷ lệ đáp ứng giữ chỗ tồn, và mức sai lệch khi kiểm nhập (điểm trách nhiệm)."
  - "Mục tiêu là minh bạch hiệu suất theo vai trò và theo ca/kho, không phải cơ chế tự động trừ lương."
  - "Nhờ KPI, quản lý nhìn thấy chỗ nghẽn để đào tạo lại quy trình, thay vì xử lý cảm tính."

### 13) Điểm đặt hàng lại là gì?
- Là ngưỡng để kích hoạt kế hoạch nhập thêm.
- Khác tồn tối thiểu (tồn tối thiểu là ngưỡng cảnh báo thấp).

### 14) LOT/Hạn dùng nằm ở đâu?
- Không nằm ở master vật tư.
- LOT/Hạn dùng nằm ở giao dịch nhập/xuất và tồn chi tiết theo vị trí.

### 15) Tại sao không cho duyệt ngay từ nhân viên?
- Vì doanh nghiệp cần tách nhiệm vụ để kiểm soát rủi ro.
- Tránh trường hợp một người vừa tạo vừa tự duyệt.

### 16) Tại sao phải 3 bước xuất kho?
- `Xác nhận tạo lấy hàng -> Xác nhận nhiệm vụ lấy hàng -> Ghi sổ xuất kho`.
- Giúp tránh trừ tồn sớm, tránh lệch số khi lấy hàng chưa hoàn tất.

### 17) Tại sao có cả tồn chi tiết và tồn tổng?
- `Tồn chi tiết theo vị trí`: tồn theo vị trí/lô/hạn dùng.
- `Tồn tổng`: tồn tổng để báo cáo nhanh.
- Hai lớp này giúp vừa đúng nghiệp vụ vừa tối ưu hiển thị.

### 18) Nếu thầy hỏi “hệ thống chống sai kiểu gì?”
- Chống âm tồn.
- Giao dịch an toàn ở bước nhạy cảm.
- Phân quyền + chống giả mạo biểu mẫu + nhật ký truy vết.
- Kiểm tra dữ liệu ở máy chủ, không tin hoàn toàn dữ liệu từ giao diện.


“Đề tài của em là WMS nội bộ, nhưng em triển khai theo chuẩn vận hành kho lớn để đảm bảo an toàn dữ liệu và dễ mở rộng.”
“Điểm cốt lõi là tách đúng vai trò: nhân viên thao tác kho, quản lý/kiểm soát là người duyệt cuối.”
“Ở luồng nhập, hệ thống bắt buộc kiểm trước khi tăng tồn; nếu có sai lệch thì ghi chú và gán điểm trách nhiệm.”
“Hệ thống chặn trường hợp một người vừa lập vừa kiểm để đảm bảo kiểm soát nội bộ.”
“Ở luồng xuất, em tách thành 3 bước: xác nhận tạo lấy hàng, xác nhận nhiệm vụ lấy hàng, rồi mới ghi sổ xuất kho.”
“Nhờ vậy hệ thống tránh trừ tồn sớm và tránh sai lệch khi lấy hàng chưa hoàn tất.”
“Ngoài ra em hỗ trợ nghiệp vụ thực tế là xuất một phần: có thể chốt phần đã lấy hoặc hủy phần còn lại.”
“Toàn bộ luồng đều có chặn âm tồn, hoàn tác khi hủy, và khóa kỳ theo kho để giữ nhất quán số liệu.”
“Về bảo mật và kiểm soát, hệ thống có phân quyền theo vai trò, chống giả mạo biểu mẫu và nhật ký truy vết thao tác.”
“Tóm lại, hệ thống không chỉ chạy được mà còn đúng nghiệp vụ kho, có kiểm soát trách nhiệm và sẵn sàng vận hành thực tế.”

---

## K. Bảng thuật ngữ tiếng Việt (để trả lời nhanh khi thầy hỏi)

- **WMS**: Hệ thống quản lý kho.
- **KPI nội bộ**: Chỉ số đo hiệu suất vận hành nội bộ.
- **FEFO**: Gần hết hạn xuất trước.
- **Giữ chỗ tồn (Reservation)**: Khóa trước một phần tồn cho phiếu xuất.
- **Đợt lấy hàng (Wave)**: Nhóm nhiệm vụ lấy hàng theo đợt.
- **Nhiệm vụ lấy hàng (Pick Task)**: Lệnh lấy hàng chi tiết cho nhân viên kho.
- **Giá trị quét**: Dữ liệu đọc từ mã vạch/số lô khi xác nhận lấy hàng.
- **Ghi sổ xuất kho**: Bước xác nhận trừ tồn chính thức.
- **Xuất một phần**: Chốt phần đã lấy khi chưa đủ 100%.
- **Khóa kỳ theo kho**: Chặn thao tác sửa/ghi sổ cho chứng từ thuộc kỳ đã khóa.
- **Nhật ký truy vết**: Lịch sử ai làm gì, lúc nào, trên dữ liệu nào.