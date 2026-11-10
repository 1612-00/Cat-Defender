A. UI & Interaction
Hành động	File gốc 
Nhấn button	click_001.ogg	
Chọn mèo	select_001.ogg
Đặt mèo lên ô	drop_001.ogg	
Mở menu / shop	open_001.ogg
Đóng menu	close_001.ogg
Không đủ tiền	error_004.ogg
Xác nhận mua	confirmation_001.ogg
Chuyển trạng thái nút	switch_002.ogg	
Đếm ngược	tick_002.ogg
Merge mèo thành công  merge-cat
Game completed  game-level-completed
Gave over musical-game-over
Wave bắt đầu  classic-alarm
Boss xuất hiện  giant-monster-footstep
Bảng chọn thẻ xuất hiện  happy-bell-alert
Xóa mèo  money-bag-drop

B. Combat — Mèo bắn và zombie trúng đòn
Hành động	File gốc Kenney	
Mèo bắn thường	laserSmall_000.ogg
Biến thể tiếng bắn	laserSmall_001.ogg
Đạn đặc biệt	laserRetro_000.ogg
Đạn mạnh	laserLarge_000.ogg
Đạn trúng zombie	impactSoft_medium_002.ogg
Mèo cận chiến đánh	impactPunch_heavy_001.ogg
Đạn nổ	explosionCrunch_000.ogg
Đạn nổ lớn / boss	lowFrequency_explosion_000.ogg
Zombie bị đánh	impactSoft_heavy_000.ogg
Trúng giáp kim loại	impactMetal_000.ogg
Tường bị tấn công  impactWood_heavy_004
Tường còn 25%  facility-alarm
Boss stun mèo  impactPlate_heavy_004
Quái tách đôi  long-pop

C. Economy, Upgrade và kỹ năng
Hành động	File gốc
Thu thập coin	glass_001.ogg
Coin biến thể	pluck_001.ogg
Mua mèo	confirmation_001.ogg
Nâng cấp mèo	maximize_005.ogg
Xác nhận nâng cấp	confirmation_003.ogg
Skill lá chắn	forceField_000.ogg
Skill năng lượng	laserRetro_001.ogg
Skill nổ diện rộng	explosionCrunch_002.ogg

## Tích hợp trong game

Âm thanh hiệu ứng được phát qua `AudioManager` trong cả hai scene; `GameBuilder` gắn các clip vào scene, vì vậy hãy dựng lại bằng `CatDefender > 2. Build Game` sau khi đổi tham chiếu. `AudioManager` được giữ qua chuyển scene và đọc tuỳ chọn `sound` trong tiến trình người chơi. Bật/tắt nhạc vẫn được lưu trong cài đặt, nhưng chưa phát nhạc nền vì thư mục hiện không có file nhạc.

Mỗi cue có âm lượng và khoảng cách phát tối thiểu riêng trong `GameBuilder`. Tiếng bắn/coin/va chạm được hạ nhỏ và giới hạn lặp nhanh hơn alarm, nâng cấp, hoặc sự kiện một lần; hiệu ứng lặp liên tục (đánh tường) có khoảng cách dài hơn. Đây là mức cân bằng khởi điểm theo vai trò và tần suất, cần nghe thử trên điện thoại rồi tinh chỉnh. `GameBuilder` khai báo tường minh từng cặp cue/đường dẫn; các file trùng tên được phân biệt theo thư mục: xác nhận UI dùng `UI_Interaction/confirmation_001.ogg`, mua mèo/Cat Boxing dùng `Economy_Upgrade/confirmation_001.ogg`.

| Tình huống | Cue |
|---|---|
| Nút UI chung; mở/đóng panel; chọn tab, tuỳ chọn | `click_001`, `open_001`, `close_001`, `switch_002` |
| Chọn/kéo/thả mèo; mua mèo; sửa tường; nâng cấp; chọn thẻ | `select_001`, `drop_001`, `confirmation_001`, `maximize_005`, `confirmation_003` |
| Không đủ coin/gem; đếm ngược; bắt đầu wave | `error_004`, `tick_002`, `classic-alarm` (một lần khi wave bắt đầu sinh quái) |
| Mèo cấp 1–5 bắn; cấp 6–10 / 11–15 bắn | Luân phiên `laserSmall_000` / `laserSmall_001`; `laserRetro_000` / `laserLarge_000` |
| Đạn trúng quái thường / giáp; hạ quái thường / boss; đạn perk nổ lan | `impactSoft_medium_002`, `impactMetal_000`, `impactSoft_heavy_000`, `lowFrequency_explosion_000`, `explosionCrunch_000` |
| Tường bị đánh; tường vừa tụt xuống dưới 25% máu; boss làm choáng mèo | `impactWood_heavy_004`, `facility-alarm` (một lần mỗi lần vượt ngưỡng), `impactPlate_heavy_004` (chỉ khi có mèo trong hàng) |
| Boss xuất hiện; quái tách đôi; mèo ghép; mèo bị xoá | `giant-monster-footstep`, `long-pop`, `merge-cat`, `money-bag-drop` |
| Hiện lựa chọn perk; thắng level; thua level | `happy-bell-alert`, `game-level-completed`, `musical-game-over` |
| Nhận coin; chông / TNT / Cat Boxing kích hoạt và đánh trúng / Cat Guardian | Luân phiên `glass_001` / `pluck_001`; `laserRetro_001`, `explosionCrunch_002`, `confirmation_001` / âm va chạm fallback, `forceField_000` |

Âm lượng được sinh theo từng cue: UI thao tác 18%, UI xác nhận 30%, súng 10–14%, va chạm 16–20%, coin 12%, nổ/nhấn sự kiện 26–34%. Cue có tần suất dày được giới hạn riêng: bắn tối đa 10 lần/giây mỗi cue, coin/đạn trúng/nổ lan tối đa 12, va tường tối đa 4; tick countdown tối đa 2 lần/giây. Các cue một lần giữ khoảng cách tối thiểu 0,06 giây để tránh lặp do trùng sự kiện.

`impactPunch_heavy_001.ogg` có trong danh sách dự kiến nhưng chưa có trong `Assets/Game/Audio`, nên Cat Boxing tạm dùng `impactSoft_heavy_000.ogg`. Các cue không có clip sẽ được bỏ qua; âm thanh bị tắt cũng không phát.