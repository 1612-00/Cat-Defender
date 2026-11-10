# Lộ trình CatDefender: 6 mốc

> Lập ngày 2026-11-10. Trạng thái: **mốc 1, 2 và 2.5 xong; chờ chủ dự án chơi thử lại mốc 2.5 trước khi sang mốc 3.**

## Cách đọc con số

- **Giờ AI:** thời gian phiên làm việc của AI (viết code, điều khiển Unity Editor, chạy test, sửa lỗi). Đây là ước tính chưa được đo. Khoảng dao động dự kiến là từ −30% đến +50%.
- **Giờ bạn:** thời gian chủ dự án chơi thử, trả lời câu hỏi thiết kế, làm việc AI không làm thay được (tài khoản, thiết bị thật, âm thanh).
- **Theo lịch:** giả định mỗi ngày 4 giờ AI và khoảng 1 giờ của bạn.
- Sau mốc 1, so giờ thực tế với ước tính rồi nhân hệ số đó cho các mốc còn lại.

## Tổng quan

| Mốc | Tên | Giờ AI | Giờ bạn | Theo lịch | Kết quả |
|---|---|---|---|---|---|
| 1 | Lõi chơi được | 7 | 1 | 2 ngày | Một level chơi được từ đầu đến thắng/thua |
| 2 | Nội dung | 14 | 2 | 4 ngày | Đủ 15 mèo, 15 quái, 4 addon, 30 level bản nháp |
| 3 | Ngoài màn chơi | 18 | 2 | 4–5 ngày | Vòng chơi khép kín: bản đồ, nâng cấp, shop, lưu game |
| 4 | Hoàn thiện | 11 | 3 | 3 ngày | Có âm thanh, hiệu ứng, hướng dẫn, chạy mượt trên điện thoại |
| 5 | Cân bằng | 14 | 10 | 10 ngày | 30 level có độ khó hợp lý, đã qua 3 vòng chơi thử |
| 6 | Phát hành | 18 | 11 | 7–14 ngày | Bản Android và iOS trên store, có quảng cáo |
| | **Tổng** | **82** | **29** | **5–6 tuần** | |

Bản chơi được trọn vẹn (hết mốc 4): 50 giờ AI, khoảng 2 tuần.
Đã tính theo quyết định ngày 2026-11-10: có iOS, hai ngôn ngữ, không có mua trong game, âm thanh do chủ dự án thêm sau.

---

## Mốc 1: Lõi chơi được (7 giờ AI, 1 giờ bạn) — XONG, thực tế 0,7 giờ AI

**Mục tiêu:** kiểm chứng vòng ghép + thủ tường có vui không trước khi đầu tư tiếp.

| # | Đầu việc | Ước tính | Thực tế |
|---|---|---|---|
| 1.1 | Dựng cấu trúc `Assets/Game/`, asmdef, scene Gameplay, camera ngang, Input System | 0,5 | |
| 1.2 | Thiết lập import sprite; dựng animation cho 2 mèo và 1 quái từ chuỗi PNG | 1 | |
| 1.3 | Lưới 2×5 ô, mua mèo cấp 1, kéo thả, ghép hai mèo cùng cấp | 1,5 | |
| 1.4 | Mèo tự chọn mục tiêu và bắn; đạn, sát thương | 1 | |
| 1.5 | Quái đi, đập tường, chết, rơi coin; máu tường, sửa tường | 1 | |
| 1.6 | Sinh wave từ dữ liệu, điều kiện thắng/thua, HUD tối thiểu (coin, wave, máu tường) | 1 | |
| 1.7 | Test EditMode cho logic ghép và sinh wave; một vòng sửa lỗi | 1 | |

**Xong khi:**
- Mở scene, bấm Play, chơi được một level 5 wave tới thắng hoặc thua.
- Ghép hai mèo cấp N cho ra đúng một mèo cấp N+1.
- Test EditMode chạy qua.
- Bạn chơi thử và xác nhận đi tiếp, hoặc nêu điều cần đổi.

**Kết quả kiểm tra (2026-11-10):**
- 9/9 test EditMode qua (ghép, di chuyển, đổi chỗ, ví tiền, giá mèo, sinh wave).
- Chạy trong Play mode: mèo bắn, quái chết rơi coin, qua wave, tường hết máu thì hiện popup thua, hết 5 wave thì hiện popup thắng; console không có lỗi.
- Ghép, di chuyển, đổi chỗ, thả ra ngoài, bỏ thùng rác: kiểm tra qua `BoardView.Press/Drag/Release`.
- **Chưa kiểm tra được:** thao tác kéo bằng chuột/ngón tay thật (công cụ giả lập chuột không giữ được trạng thái nhấn). Cần chủ dự án kéo thử.
- Làm vượt phạm vi: đã có dữ liệu và animation cho cả 15 mèo và 2 quái thường; mèo bỏ qua quái mà đạn đang bay đã đủ hạ.
- Giờ thực tế đo theo đồng hồ từ commit đầu tới lúc xong, không tách theo từng đầu việc.

**Việc của bạn:** chơi thử 30 phút, trả lời các câu hỏi mở 1–3 trong game-concept.

---

## Mốc 2: Nội dung (14 giờ AI, 2 giờ bạn) — XONG, thực tế 0,35 giờ AI

**Mục tiêu:** đưa toàn bộ nhân vật và cơ chế chiến đấu vào game.

| # | Đầu việc | Ước tính | Thực tế |
|---|---|---|---|
| 2.1 | Công cụ Editor tự dựng animation và prefab từ thư mục PNG cho 15 mèo, 15 quái, 2 mèo hỗ trợ, hiệu ứng | 3 | |
| 2.2 | ScriptableObject cho mèo và quái (máu, sát thương, tốc độ, giá, phần thưởng) | 1,5 | |
| 2.3 | Hành vi boss (máu lớn, đòn riêng, thanh máu boss) | 1,5 | |
| 2.4 | 4 addon: chông, TNT, Cat Boxing, Cat Guardian, có giới hạn lượt dùng | 3 | |
| 2.5 | 5 nền; định dạng dữ liệu level; 30 level bản nháp sinh theo công thức | 2 | |
| 2.6 | Gắn hiệu ứng bắn, nổ, trúng đạn | 1 | |
| 2.7 | Test và một vòng sửa lỗi | 2 | |

**Xong khi:**
- Cả 15 cấp mèo ghép được và bắn đúng animation.
- Cả 15 loại quái xuất hiện đúng trong các level bản nháp; boss ở cuối mỗi chương.
- 4 addon dùng được trong level.
- Chọn được level bất kỳ trong 30 level từ menu debug.

**Rủi ro:** 2.1 phụ thuộc vào việc điều khiển Editor tự động có trơn không; nếu không, cộng thêm 3–5 giờ.

**Kết quả kiểm tra (2026-11-10):**
- 12/12 test EditMode qua, gồm 3 test mới kiểm tra 30 level đều có nền, wave hợp lệ, boss ở cuối chương.
- Chạy trong Play mode các level 6, 12, 18, 24, 30: không lỗi console; level 6, 12, 18, 24 chạy tới popup thắng, level 30 chạy tới wave 9/9.
- Boss có thanh máu trên đầu; 4 addon (chông, TNT, Cat Boxing, Cat Guardian) đều kích hoạt và hiển thị đúng; có hiệu ứng nòng súng, nổ, quái nháy đỏ khi trúng đạn.
- Nút `<` `>` cạnh nhãn Level để nhảy level khi chơi thử (tạm thời, mốc 3 thay bằng bản đồ level). Popup thắng có nút Next Level.
- **Chưa kiểm tra được:** cảm giác chơi và độ khó. Chỉ số là bản nháp sinh theo công thức trong `GameBuilder.cs`; tôi chỉ xác nhận level chạy được, chưa xác nhận level thắng được bằng cách chơi bình thường.
- **Điều tôi quan sát được về cân bằng:** tường 100 máu sập rất nhanh khi vài quái chạy nhanh (loại 3, 5, 7) tới nơi; cần xem lại ở mốc 5.
- Boss chưa có đòn đánh riêng, chỉ khác ở máu, sát thương, kích thước và thanh máu.

**Việc của bạn:** chơi thử 5–6 level rải đều, ghi lại quái hoặc addon nào thấy sai hoặc chán.

---

## Mốc 2.5: Chiều sâu gameplay (chèn thêm ngày 2026-11-10) — XONG, thực tế dưới 1 giờ AI (ước chừng, không đo được chính xác)

**Lý do:** chủ dự án chơi thử mốc 1–2, không gặp lỗi nhưng thấy vòng chơi quá đơn giản. Các mốc 3–6 không thêm chiều sâu cho lúc đang chơi, nên chèn mốc này trước.

| # | Đầu việc | Kết quả |
|---|---|---|
| 2.5.1 | Hàng ngang: mèo chỉ bắn quái trên hàng của nó; quái đi theo hàng; dải đỏ báo hàng sắp có quái trong lúc nghỉ | Xong |
| 2.5.2 | Chọn 1 trong 3 thẻ nâng cấp sau mỗi wave (8 loại thẻ, cộng dồn trong level) | Xong |
| 2.5.3 | Quái có hành vi riêng: giáp, đổi hàng, tách đôi khi chết; boss gọi đàn em và làm choáng mèo | Xong |

**Kết quả kiểm tra (2026-11-10):**
- 17/17 test EditMode qua (thêm test cho thẻ nâng cấp, bộ chọn ngẫu nhiên, số hàng theo wave, giờ nghỉ trước wave đầu).
- Chạy trong Play mode: level 1 thua khi không có mèo ở hàng bị tấn công (đúng thiết kế); level 6, 9, 30 chạy tới popup thắng với mèo đặt sẵn ở mọi hàng và thẻ được chọn tự động; console không có lỗi.
- Kiểm tra trực tiếp từng hành vi: quái tách thành 2 con ở hai hàng; giáp 3 biến 10 sát thương thành 7 và 2 thành 1; quái nhanh đổi từ hàng 2 sang hàng 1; boss gọi thêm 2 quái mỗi 7 giây và làm mèo cùng hàng choáng ở giây thứ 9, lặp lại ở giây 18.
- Bảng chọn thẻ hiển thị đúng 3 thẻ, game tạm dừng trong lúc chọn và chạy lại sau khi chọn.
- **Chưa kiểm tra được:** cảm giác chơi (có đủ việc để làm chưa, 5 giây nghỉ có đủ để xếp mèo không), và độ khó. Chỉ số vẫn là bản nháp.
- Thay đổi chỉ số kèm theo: máu tường 100 → 150, nghỉ trước mỗi wave 5 giây.
- Chữ trên thẻ mới có tiếng Anh; bảng chuỗi Anh/Việt làm ở mốc 3.

**Việc của bạn:** chơi lại level 1–10, cho biết (1) kéo mèo theo hàng có thấy bận tay và có ý nghĩa không, (2) thẻ nâng cấp có tạo khác biệt giữa các lượt không, (3) loại quái nào gây khó chịu theo kiểu xấu.

---

## Mốc 3: Ngoài màn chơi (18 giờ AI, 2 giờ bạn)

**Mục tiêu:** khép kín vòng chơi từ mở app đến level kế tiếp.

| # | Đầu việc | Ước tính | Thực tế |
|---|---|---|---|
| 3.1 | Lưu và tải tiến trình (level đã qua, coin, gem, nâng cấp, cài đặt) | 1,5 | |
| 3.2 | Coin, gem, phần thưởng thắng level, WinBonus | 1 | |
| 3.3 | Màn landing và bản đồ level (khóa, đã qua, đang chơi) | 2,5 | |
| 3.4 | Màn nâng cấp mèo và tường | 2,5 | |
| 3.5 | Shop dùng tiền trong game | 2 | |
| 3.6 | Màn chọn và nâng cấp addon | 1,5 | |
| 3.7 | Popup thắng, thua, tạm dừng, cài đặt (nhạc, âm thanh, rung) | 2 | |
| 3.8 | Luồng chuyển màn, vùng an toàn cho màn hình tai thỏ, font Passion One | 1 | |
| 3.9 | Bảng chuỗi tiếng Anh và tiếng Việt, đổi ngôn ngữ trong cài đặt | 2 | |
| 3.10 | Test lưu game và kinh tế; một vòng sửa lỗi | 2 | |

**Xong khi:**
- Từ landing chơi liên tục được: chọn level, thắng, nhận thưởng, nâng cấp, sang level sau.
- Thoát app và mở lại, tiến trình còn nguyên.
- Mọi nút trên mọi màn hình đều hoạt động hoặc bị vô hiệu rõ ràng.

**Việc của bạn:** chơi liên tục 10 level đầu, kiểm tra từng màn hình.

---

## Mốc 4: Hoàn thiện (11 giờ AI, 3 giờ bạn)

**Mục tiêu:** game cho cảm giác hoàn chỉnh và chạy tốt trên điện thoại.

| # | Đầu việc | Ước tính | Thực tế |
|---|---|---|---|
| 4.1 | Hệ thống âm thanh với điểm gắn cho mọi sự kiện; clip để trống tới khi chủ dự án thêm file | 2 | |
| 4.2 | Cảm giác chơi: rung màn hình, số sát thương bay, hiệu ứng ghép, chuyển động UI | 2 | |
| 4.3 | Hướng dẫn chơi ở level 1–3 | 2,5 | |
| 4.4 | Tối ưu: sprite atlas, pool đối tượng, giảm dung lượng texture, đo khung hình | 2,5 | |
| 4.5 | Một vòng sửa lỗi từ bản build thử | 2 | |

**Xong khi:**
- Mọi hành động chính có âm thanh và phản hồi hình ảnh.
- Người chơi mới qua được 3 level đầu mà không cần giải thích ngoài game.
- Bản build Android chạy ổn định trên máy thử của bạn ở wave đông quái nhất.

**Việc của bạn:** chọn và cung cấp file âm thanh, nhạc (khoảng 2 giờ); cài bản build lên điện thoại và chơi thử (1 giờ).

**Phụ thuộc bên ngoài:** nguồn âm thanh có giấy phép thương mại.

---

## Mốc 5: Cân bằng (14 giờ AI, 10 giờ bạn)

**Mục tiêu:** 30 level có đường cong độ khó hợp lý. Mốc này phụ thuộc vào bạn nhiều nhất.

| # | Đầu việc | Ước tính | Thực tế |
|---|---|---|---|
| 5.1 | Mô hình kinh tế và công cụ mô phỏng level (ước lượng tỉ lệ thắng theo mức nâng cấp) | 3 | |
| 5.2 | Vòng chỉnh 1: level 1–10 | 2 | |
| 5.3 | Vòng chỉnh 2: level 11–20 | 2 | |
| 5.4 | Vòng chỉnh 3: level 21–30 và boss | 2 | |
| 5.5 | Thưởng hằng ngày (cắt được) | 2 | |
| 5.6 | Sửa lỗi phát sinh từ chơi thử | 3 | |

**Xong khi:**
- Bạn (và nếu được, 2–3 người khác) chơi hết 30 level.
- Không level nào phải chơi lại quá 4–5 lần với mức nâng cấp bình thường.
- Tổng thời gian chơi hết nằm trong khoảng 4–8 giờ.
- Không còn lỗi làm hỏng tiến trình.

**Việc của bạn:** mỗi vòng chơi thử khoảng 3 giờ, ghi lại level nào quá dễ, quá khó, hoặc chán.

---

## Mốc 6: Phát hành (18 giờ AI, 11 giờ bạn)

**Mục tiêu:** bản Android và iOS lên store.

| # | Đầu việc | Ước tính | Thực tế |
|---|---|---|---|
| 6.1 | Quảng cáo có thưởng và quảng cáo xen kẽ | 4 | |
| 6.2 | Build iOS: cấu hình Xcode, quyền theo dõi quảng cáo, icon (cần máy Mac của bạn) | 4 | |
| 6.3 | Cấu hình build Android, icon, màn hình khởi động, phiên bản | 3 | |
| 6.4 | Màn đồng ý quyền riêng tư, chính sách bảo mật | 2 | |
| 6.5 | Danh sách kiểm tra phát hành, nội dung trang store, ảnh chụp màn hình | 2 | |
| 6.6 | Sửa lỗi sau khi thử trên thiết bị và bản thử nghiệm nội bộ | 3 | |

**Xong khi:**
- Bản build ký hợp lệ được tải lên kênh thử nghiệm nội bộ của Google Play.
- Quảng cáo và giao dịch thử chạy đúng trên thiết bị thật.
- Bản phát hành được store duyệt.

**Việc của bạn (AI không làm thay được):**
- Tạo tài khoản Google Play Developer, tài khoản mạng quảng cáo, khai báo thanh toán và thuế.
- Tạo và giữ khóa ký app; nhập mọi mật khẩu và khóa bí mật.
- Thử giao dịch và quảng cáo trên điện thoại thật.
- Bấm gửi duyệt và chờ store duyệt.

**Rủi ro:** lỗi build do thư viện quảng cáo/thanh toán có thể cộng thêm 4–8 giờ. Thời gian duyệt store và yêu cầu thử nghiệm kín của Google Play nằm ngoài tầm kiểm soát.

---

## Rủi ro chung

| Rủi ro | Ảnh hưởng | Cách giảm |
|---|---|---|
| Điều khiển Unity Editor tự động không ổn định | +20–50% giờ AI ở mọi mốc | Kiểm chứng ngay ở 1.1–1.2; nếu không được, bạn làm các bước kéo thả trong Editor theo hướng dẫn |
| Đổi thiết kế sau mốc 2 | Làm lại dữ liệu level và cân bằng | Chốt các câu hỏi mở trong game-concept trước khi hết mốc 1 |
| Chơi thử chậm | Lịch kéo dài dù giờ AI không đổi | Mỗi mốc có danh sách việc chơi thử cụ thể |
| Nội dung lặp sau chương 2 | Người chơi bỏ sớm | Đánh giá ở mốc 2; nếu cần thì giảm số level thay vì kéo dài |

## Nhật ký tiến độ

| Ngày | Mốc | Việc đã xong | Giờ AI thực tế | Ghi chú |
|---|---|---|---|---|
| 2026-11-10 | 2.5 | Hàng ngang, thẻ nâng cấp, hành vi quái | dưới 1 | Mốc chèn thêm sau phản hồi chơi thử. |
| 2026-11-10 | 2 | Toàn bộ 2.1–2.7 | 0,35 | Ước tính 14 giờ. Đo theo đồng hồ giữa hai commit. Hệ số cộng dồn hai mốc ≈ 0,05. |
| 2026-11-10 | 1 | Toàn bộ 1.1–1.7 | 0,7 | Ước tính 7 giờ; hệ số thực tế/ước tính ≈ 0,1. Điều khiển Editor qua Unity CLI chạy trơn. |
