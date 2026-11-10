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

Mỗi cue có âm lượng riêng và được giới hạn không phát lặp nhanh hơn một lần trong 0,06 giây. `GameBuilder` khai báo tường minh từng cặp cue/đường dẫn; hai file `confirmation_001.ogg` được phân biệt theo thư mục: UI dùng bản `UI_Interaction`, mua mèo dùng bản `Economy_Upgrade`.

| Tình huống | Cue |
|---|---|
| Nút UI chung, mở/đóng cài đặt và chuyển tab | `click_001`, `open_001`, `close_001`, `switch_002` |
| Chọn/kéo/thả mèo; mua mèo, sửa tường; nâng cấp hoặc chọn perk | `select_001`, `drop_001`, `confirmation_001`, `maximize_005`, `confirmation_003` |
| Không đủ coin/gem và đếm ngược từng giây trước wave | `error_004`, `tick_002` |
| Mèo cấp 1–5 bắn | Luân phiên `laserSmall_000` và `laserSmall_001` |
| Mèo cấp 6–10 / 11–15 bắn | `laserRetro_000` / `laserLarge_000` |
| Đạn trúng quái thường / quái giáp; quái thường chết / boss chết | `impactSoft_medium_002`, `impactMetal_000`, `impactSoft_heavy_000`, `lowFrequency_explosion_000` |
| Nhận coin; đạn perk nổ lan | Luân phiên `glass_001` và `pluck_001`; `explosionCrunch_000` |
| Chông / TNT / Cat Boxing kích hoạt và đánh trúng / Cat Guardian | `laserRetro_001`, `explosionCrunch_002`, `confirmation_001` lúc kích hoạt và âm va chạm mạnh fallback khi đánh, `forceField_000` |

`impactPunch_heavy_001.ogg` có trong danh sách dự kiến nhưng chưa có trong `Assets/Game/Audio`, nên Cat Boxing tạm dùng `impactSoft_heavy_000.ogg`. Các cue không có clip sẽ được bỏ qua; âm thanh bị tắt cũng không phát.