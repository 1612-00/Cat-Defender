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

> Cập nhật 2026-11-10 sau lần chơi thử đầu: vòng chơi "mua, ghép, bấm addon" bị chủ dự án đánh giá là quá đơn giản. Đã thêm ba cơ chế: hàng ngang, thẻ nâng cấp sau wave, quái có hành vi riêng.

### Trong một level

1. Sân có **5 hàng ngang**. Lưới đặt mèo có 2 ô mỗi hàng (hàng dưới cùng chỉ 1 ô, ô còn lại là thùng rác).
2. **Mèo chỉ bắn quái đi trên hàng của nó.** Quái tới tường thì đập tường, bất kể hàng nào.
3. Trước mỗi wave có 5 giây nghỉ. Trong lúc nghỉ, **dải đỏ nhấp nháy báo những hàng sắp có quái** (2 hàng ở wave đầu, thêm 1 hàng sau mỗi 2 wave, tối đa 5).
4. Người chơi dùng coin để mua mèo cấp 1, kéo hai mèo cùng cấp vào nhau để ghép, **kéo mèo sang hàng đang bị ép**, sửa tường, hoặc dùng addon (chông, TNT, Cat Boxing, Cat Guardian).
5. **Sau mỗi wave được hạ sạch, game tạm dừng và đưa ra 3 thẻ nâng cấp ngẫu nhiên, chọn 1.** Thẻ cộng dồn trong level.
6. Tường hết máu thì thua. Qua hết wave thì thắng.

### Thẻ nâng cấp (8 loại)

| Thẻ | Tác dụng |
|---|---|
| Rapid Fire | Mèo bắn nhanh hơn 20% |
| Sharp Claws | Sát thương đạn +25% |
| Blast Rounds | Đạn lan 30% sát thương sang quái đứng gần |
| Lucky Coins | Coin từ quái +30% |
| Sticky Shots | Đạn làm chậm quái 20% (tối đa 70%, boss chịu một nửa) |
| Thick Wall | Máu tối đa của tường +40 |
| Restock | Mỗi addon thêm 1 lượt |
| Stray Cat | Tặng một mèo cấp 2 (chỉ xuất hiện khi còn ô trống) |

### Hành vi riêng của quái

| Quái | Hành vi | Buộc người chơi làm gì |
|---|---|---|
| Reg 1 | Cơ bản | |
| Reg 2, Reg 6 | Có giáp: mỗi phát đạn bị trừ một lượng sát thương cố định | Ghép lên mèo cấp cao thay vì dàn nhiều mèo cấp thấp |
| Reg 3, Reg 5, Reg 7 | Chạy nhanh, giữa đường nhảy sang hàng bên cạnh | Giữ mèo ở hàng kề, phản ứng nhanh |
| Reg 4, Reg 8 | Chết thì tách thành 2 con nhỏ hơn, một con sang hàng bên | Không dồn hết mèo vào một hàng |
| Boss | Gọi 2 đàn em mỗi 7 giây; làm choáng mèo trên hàng của nó 2,5 giây mỗi 9 giây | Chia hoả lực, dùng addon đúng lúc |

Quái có giáp được nhuộm màu xanh thép để phân biệt.

### Giữa các level

- Thắng nhận coin, gem và mở level kế tiếp trên bản đồ.
- Dùng coin/gem nâng cấp vĩnh viễn: sát thương mèo, máu tường, cấp và số lượt addon.
- Thua thì quay lại nâng cấp rồi đánh lại.

### Dài hạn

- 5 chương theo 5 nền, mỗi chương kết thúc bằng boss.
- Quái mới xuất hiện dần qua các chương.
- Sau bản đầu: thưởng hằng ngày, chế độ vô tận.

## Game Pillars

1. **Mỗi giây đều có việc để quyết định.** Xếp mèo theo hàng, ghép, chọn thẻ, dùng addon: người chơi không được ngồi nhìn.
2. **Đọc được trong một cái nhìn.** Người chơi luôn thấy máu tường, wave hiện tại, số coin, cấp của từng mèo và hàng nào sắp có quái mà không cần mở menu.
3. **Mỗi lượt chơi một khác.** Hàng có quái và thẻ nâng cấp là ngẫu nhiên, nên chơi lại cùng một level không diễn ra y hệt.
4. **Thua là lý do để nâng cấp, không phải bức tường trả tiền.** Level khó vượt được bằng chơi lại và nâng cấp bằng coin thường; gem chỉ rút ngắn thời gian.

### Không phải là

- Mèo không bị tấn công trực tiếp: chỉ bức tường có máu (boss chỉ làm choáng mèo tạm thời).
- Không tăng tốc độ game theo thời gian; độ khó tăng bằng chỉ số, thành phần và hành vi của quái.
- Không có nhiều người chơi, không có cốt truyện.

### Ý tưởng để dành (chưa làm)

- Ba hệ mèo theo loại đạn (bắn đơn, làm chậm, nổ lan), mua ra hệ ngẫu nhiên. Chỉ làm nếu sau khi chơi thử ba cơ chế trên vẫn thấy thiếu.
- Coin rơi phải chạm để nhặt, combo khi hạ nhiều quái liên tiếp.

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
