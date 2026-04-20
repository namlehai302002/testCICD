# Tóm Tắt 2 Phút Bảo Vệ WMS

## 1) Bài toán em giải
- Quản lý kho nội bộ theo chuẩn kho lớn: nhập, xuất, chuyển kho, kiểm kê, khóa kỳ.
- Kiểm soát tồn theo vị trí/lô/hạn dùng, không cho âm tồn, có theo dõi reservation.

## 2) Luồng nghiệp vụ chính
- **Nhập kho:** tạo phiếu -> kiểm duyệt -> tăng tồn.
- **Xuất kho:** tạo phiếu -> release picking -> pick task -> chốt xuất (toàn phần hoặc một phần).
- **Chuyển kho:** giảm tồn vị trí nguồn, tăng tồn vị trí đích, tổng tồn không đổi.
- **Kiểm kê:** nhập số thực tế -> duyệt -> tự sinh điều chỉnh chênh lệch.

## 3) Điểm “kho lớn” nổi bật
- FEFO đa lô/đa vị trí.
- Reservation + pick task + wave.
- Quản lý vị trí nguồn/đích, số lô, quy đổi đơn vị.
- Dashboard KPI vận hành (wave, task mở, short-pick, fill-rate).

## 4) Kiểm soát an toàn dữ liệu
- Transaction `Serializable` cho các luồng ảnh hưởng tồn.
- Chặn double-submit/idempotency ở confirm picking và post outbound.
- Hủy phiếu hoàn tác đúng tồn (kể cả phiếu xuất một phần).
- Ràng buộc dữ liệu quy đổi ĐVT bằng unique index.

## 5) Phân quyền
- Staff: thao tác vận hành theo quyền.
- Manager/Admin: duyệt và thao tác nhạy cảm.
- OCR/Excel: Staff không được tự tạo vật tư mới; chỉ Manager/Admin được tạo mới.

## 6) Câu chốt khi thầy hỏi
- “Hệ thống của em ưu tiên đúng tồn và truy vết kho: mọi nhập/xuất/chuyển đều kiểm soát theo vị trí, lô và transaction an toàn; giao diện đã đổi sang ngôn ngữ vận hành để nhân sự không kỹ thuật vẫn dùng được.”
