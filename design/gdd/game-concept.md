# Game Concept: Cats Defender

> **Trạng thái:** bản nháp, lập ngày 2026-11-10. Phạm vi đã được chủ dự án chốt (xem "Quyết định đã chốt").

## Elevator Pitch

Ghép những chú mèo cầm súng thành mèo mạnh hơn và giữ bức tường trước các đợt zombie. Mỗi lượt kéo thả là một quyết định: mua thêm mèo cấp thấp hay dồn tiền sửa tường.

## Core Identity

| Mục | Nội dung |
|---|---|
| Thể loại | Merge + thủ tường (merge defense), casual |
| Nền tảng | Mobile, Android và iOS; màn hình ngang |
| Cấu trúc | Chiến dịch theo level, mỗi level 5–10 wave |
| Phiên chơi | 2–4 phút một level |
| Mô hình doanh thu | Miễn phí, bản đầu chỉ có quảng cáo |
| Ngôn ngữ | Tiếng Anh, tiếng Việt |
| Người làm | Một chủ dự án + AI |

## Core Loop

### Trong một level (30 giây một nhịp)

1. Zombie đi từ phải sang trái tới bức tường.
2. Mèo đứng trong lưới 2×5 ô sau tường, tự bắn zombie gần nhất.
3. Zombie chết rơi coin. Người chơi dùng coin để:
   - mua một mèo cấp 1 vào ô trống;
   - kéo hai mèo cùng cấp vào nhau để ra mèo cấp cao hơn;
   - sửa tường;
   - kích hoạt addon (chông, TNT, Cat Boxing, Cat Guardian), số lần dùng có hạn.
4. Tường hết máu thì thua. Qua hết wave thì thắng.

### Giữa các level

- Thắng nhận coin, gem và mở level kế tiếp trên bản đồ.
- Dùng coin/gem nâng cấp vĩnh viễn: sát thương mèo, máu tường, cấp và số lượt addon.
- Thua thì quay lại nâng cấp rồi đánh lại.

### Dài hạn

- 5 chương theo 5 nền, mỗi chương kết thúc bằng boss.
- Quái mới xuất hiện dần qua các chương.
- Sau bản đầu: thưởng hằng ngày, chế độ vô tận.

## Game Pillars

1. **Ghép là quyết định chính.** Mọi hệ thống phải làm cho việc kéo thả ghép mèo có ý nghĩa; không thêm thao tác cạnh tranh sự chú ý với nó.
2. **Đọc được trong một cái nhìn.** Người chơi luôn thấy máu tường, wave hiện tại, số coin và cấp của từng mèo mà không cần mở menu.
3. **Thua là lý do để nâng cấp, không phải bức tường trả tiền.** Level khó vượt được bằng chơi lại và nâng cấp bằng coin thường; gem chỉ rút ngắn thời gian.

### Không phải là

- Không phải game nhiều hàng kiểu lane defense: mèo không bị tấn công trực tiếp, chỉ bức tường có máu.
- Không tăng tốc độ game theo thời gian; độ khó tăng bằng chỉ số và thành phần quái.
- Không có nhiều người chơi, không có cốt truyện.

## Nội dung có sẵn và phần thiếu

| Có sẵn trong bộ asset | Thiếu, phải bổ sung |
|---|---|
| 15 mèo (Idle, Shoot) = 15 cấp merge | Âm thanh và nhạc |
| 2 mèo hỗ trợ: Cat Boxing, Cat Guardian | Animation mèo bị đánh/chết (không cần với thiết kế này) |
| 8 quái thường, 7 boss (Idle, Walk, Attack, Dead) | UI cho điểm số/kỷ lục (chỉ cần nếu làm chế độ vô tận) |
| 5 nền, 3 loại đạn, hiệu ứng nổ và bắn | Hiệu ứng trạng thái (làm chậm, độc) |
| 104 sprite UI đủ cho mọi màn hình | Màn hướng dẫn chơi |

## MVP Definition

Bản chơi được đầu tiên (mốc 1): một level, mua mèo, ghép, mèo bắn, zombie đập tường, 5 wave, thắng/thua.

Bản đầy đủ (mốc 1–5): 30 level trong 5 chương, 15 cấp mèo, 15 loại quái, 4 addon, bản đồ level, nâng cấp, shop bằng tiền trong game, lưu game, âm thanh, hướng dẫn chơi.

### Nếu phải cắt phạm vi

1. Cắt iOS, chỉ ra Android.
2. Cắt mua trong game, chỉ giữ quảng cáo có thưởng.
3. Cắt thưởng hằng ngày.
4. Giảm 30 level xuống 20 level (2 chương cuối dùng lại nền).

## Risks

- **Thiết kế:** vòng ghép + thủ tường có thể nhàm sau chương 2 vì chỉ có 15 loại quái. Kiểm tra bằng chơi thử ở mốc 2.
- **Cân bằng:** kinh tế coin trong level và ngoài level dễ lệch; cần nhiều vòng chơi thử (mốc 5).
- **Kỹ thuật:** hơn 3.000 file PNG cần dựng thành animation tự động; dung lượng build và bộ nhớ trên máy yếu.
- **Phát hành:** tích hợp quảng cáo và mua trong game hay lỗi build; thời gian duyệt store không kiểm soát được.

## Quyết định đã chốt (2026-11-10)

1. Bản đầu có 30 level.
2. Nền tảng: cả Android và iOS.
3. Tên game: **Cats Defender**.
4. Âm thanh: chủ dự án tự tìm và thêm sau. Code chừa sẵn điểm gắn âm thanh (sự kiện + `AudioClip` để trống).
5. Doanh thu bản đầu: chỉ quảng cáo, chưa có mua trong game bằng tiền thật.
6. Ngôn ngữ: tiếng Anh và tiếng Việt.
7. Quy trình: không áp dụng đầy đủ bộ khung `.claude`; ưu tiên làm nhanh và có đủ tài liệu.
