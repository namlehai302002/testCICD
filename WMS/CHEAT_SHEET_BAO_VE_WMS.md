# CHEAT SHEET BAO VE WMS (DOC NHANH 1 TRANG)

## 1) Mo dau 20 giay
- De tai la WMS noi bo cho nhan vien kho, nhung thiet ke theo huong kho lon.
- Trong tam la dung nghiep vu, an toan du lieu, va de su dung cho nguoi khong ranh IT.
- Luong xuat duoc tach 3 buoc de tranh tru ton sai: Confirm Picking -> Scan Pick Task -> Post Outbound.

## 2) 5 diem an diem cao (noi ngay dau bai)
1. Tach ro vai tro: Staff thao tac kho, Manager/Admin kiem-duyet-chot.
2. Nguoi kiem khong duoc trung nguoi lap phieu.
3. Co co che trach nhiem khi nhap sai: ghi chu + diem trach nhiem.
4. Chan am ton va ho tro xuat mot phan (Partial Issue) theo thuc te kho.
5. Khoa ky theo kho + audit trail + CSRF + role authorization.

## 3) Script demo 5-7 phut (di dung thu tu nay)
### Buoc 1 - Demo NHAP co kiem soat
- Dang nhap Staff -> tao phieu nhap.
- Dang nhap Manager/Admin -> mo phieu -> chinh `SL loi` 1 dong.
- Bam `Kiem Kho & Tang Ton` voi ket qua `Co sai lech`, nhap ghi chu + diem.
- Mo lai chi tiet phieu, chi thay:
  - Nguoi lap
  - Nguoi kiem
  - Ket qua kiem
  - Diem trach nhiem

### Buoc 2 - Demo XUAT kho lon
- Tao phieu xuat.
- Manager/Admin bam `Xac nhan tao nhiem vu lay hang`.
- Staff vao `Nhiem vu lay hang` scan pick.
- Manager/Admin quay lai chon:
  - `Chot phan da lay`, hoac
  - `Chot & huy phan con lai`.
- Noi: he thong cho xuat truoc phan da pick, phan thieu xu ly sau.

## 4) Cau hoi thay hay hoi + tra loi cuc gon
### Hoi: Tai sao khong cho nhan vien tu duyet?
- De tach kiem soat noi bo, tranh vua lap vua duyet.

### Hoi: Neu nhap sai thi sao?
- Nguoi kiem chinh sai lech, duyet voi ghi chu bat buoc, gan diem trach nhiem.

### Hoi: Co tranh duoc am ton khong?
- Co, da chan o luong post/huy/rollback va kiem tra ton kha dung.

### Hoi: Tai sao phai 3 buoc xuat?
- De tach yeu cau va thuc thi, tranh tru ton som, tranh over-allocate.

### Hoi: Co bao mat co ban khong?
- Co: role-based auth, CSRF, audit log, transaction o diem nhay cam.

## 5) Neu bi run thi doc 6 cau nay
1. Em tap trung vao dung nghiep vu kho, khong chi lam giao dien.
2. Em tach ro nguoi lap va nguoi kiem de kiem soat noi bo.
3. Em co co che ghi nhan trach nhiem khi nhap sai.
4. Em xu ly xuat kho theo reservation/wave/pick/post de dung thuc te kho lon.
5. Em bo sung guard am ton, khoa ky va rollback dung du lieu.
6. He thong hien da build pass, migration da apply, demo duoc end-to-end.

## 6) Checklist 2 phut truoc khi vao bao ve
- Chay duoc app va dang nhap duoc 2 role (Staff + Manager/Admin).
- Co san 1 phieu nhap va 1 phieu xuat de demo nhanh neu mang cham.
- Mo san 3 tab:
  - Danh sach phieu
  - Chi tiet phieu
  - Nhiem vu lay hang
- Nho cau chot: "Moi nguoi mot viec, co truy vet trach nhiem, khong am ton."
