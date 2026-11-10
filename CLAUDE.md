# Cats Defender

Game mobile thể loại **merge + thủ tường**: người chơi mua mèo, ghép hai con cùng cấp thành cấp cao hơn, mèo tự bắn zombie đang tiến tới đập tường. Chiến dịch theo level, mỗi level gồm nhiều wave.

Giao tiếp với người dùng bằng **tiếng Việt**.

## Tài liệu cần đọc trước khi làm

- [design/gdd/game-concept.md](design/gdd/game-concept.md): ý tưởng, vòng chơi, phạm vi, các câu hỏi còn mở.
- [production/milestones/roadmap.md](production/milestones/roadmap.md): lộ trình 6 mốc, đầu việc, giờ ước tính, tiêu chí xong. Cập nhật cột "Thực tế" khi xong mỗi đầu việc.
- `.claude/docs/`: quy ước của bộ khung studio (cấu trúc thư mục, chuẩn code, quy tắc phối hợp agent).

## Kỹ thuật

- **Engine:** Unity 6000.6.6f1, URP 2D, C#.
- **Input:** Input System (`com.unity.inputsystem`), cảm ứng là chính.
- **UI:** uGUI (`com.unity.ugui`).
- **Test:** Unity Test Framework, đặt trong `Assets/Tests/EditMode/` và `Assets/Tests/PlayMode/`. Test đặt ngoài `Assets/` sẽ không được biên dịch.
- **Nền tảng đích:** Android và iOS, màn hình ngang.
- **Ngôn ngữ trong game:** tiếng Anh và tiếng Việt. Mọi chuỗi hiển thị đi qua bảng chuỗi, không viết cứng.
- **Âm thanh:** chủ dự án thêm sau. Code gọi âm thanh qua một điểm chung, clip để trống thì bỏ qua.
- **Doanh thu bản đầu:** chỉ quảng cáo, không có mua trong game.
- `project.yaml` chưa được tạo. Chạy `/setup-engine` nếu cần dùng các skill đọc cấu hình từ file đó.

## Bố cục thư mục

| Đường dẫn | Nội dung |
|---|---|
| `Assets/Game/Scripts/` | Code game, namespace `CatDefender` |
| `Assets/Game/Editor/` | Công cụ Editor (dựng animation, prefab từ PNG) |
| `Assets/Game/Data/` | ScriptableObject: mèo, quái, level, wave, addon |
| `Assets/Game/Prefabs/`, `Assets/Game/Scenes/` | Prefab và scene của game |
| `Assets/Tests/` | Test EditMode và PlayMode |
| `Assets/cat-defense-kit/` | Bộ asset Craftpix, **không sửa file gốc** |
| `Ai/`, `Eps/` | File nguồn vector của bộ asset, nằm ngoài `Assets/` để Unity không import |
| `design/`, `production/`, `docs/` | Tài liệu thiết kế, quản lý sản xuất, tài liệu kỹ thuật |

## Bộ asset `Assets/cat-defense-kit/`

- `Png/Characters/C1..C15/{Idle,Shoot}`: 15 mèo, dùng làm 15 cấp merge. **Không có animation bị đánh hoặc chết.**
- `Png/CatBoxing`, `Png/Cat Guardian`: 2 mèo hỗ trợ, `{Idle,Attack}`.
- `Png/Enemies/Enemy Reg 1..8`, `Enemy Boss 1..7`: mỗi con có `{Idle,Walk,Attack,Dead}`.
- `Png/Area/Area1..5.png`: 5 nền màn chơi. `Png/Bullets`: 3 loại đạn. `Png/Explosion`, `Png/ShootFx`: hiệu ứng.
- `Png/Ui/`: 104 sprite UI (landing, level map, shop, nâng cấp, addon, popup thắng/thua, wave bar, coin/gem, cài đặt).
- `Preview/`: ảnh mẫu bố cục màn chơi và UI, dùng làm tham chiếu khi dựng scene.
- `Spine/`, `Json Atlas/`: bản Spine. Dự án dùng chuỗi PNG, không cài spine-unity.
- Font: Passion One (Google Fonts), chưa được thêm vào project.
- **Không có âm thanh hay nhạc.**

## Cách dựng và kiểm tra

- **Scene, prefab và dữ liệu đều do code sinh ra**, không chỉnh tay: sửa `Assets/Game/Editor/GameBuilder.cs` rồi chạy menu `CatDefender > 2. Build Game` (hoặc `GameBuilder.Build()`). Chỉnh tay trong scene hay asset dữ liệu sẽ bị ghi đè ở lần dựng sau.
- Chỉ số bản nháp của mèo, quái, addon và công thức sinh 30 level nằm trong `GameBuilder.cs`.
- Thêm thư mục PNG mới: thêm vào `FixImports()` rồi chạy `CatDefender > 1. Fix Sprite Imports` trước khi Build.
- Điều khiển Editor đang mở bằng Unity CLI: `unity recompile`, `unity command script eval '<C#>'`, `unity command test run -- --mode editor`, `unity command editor.playmode start|stop`, `unity command capture game_view -- --output-path Screenshots/x.png`.
- Sau `recompile` hoặc đổi Play mode, Editor từ chối lệnh vài giây (lỗi 400 hoặc "Invalid response"): thử lại sau 5 giây.
- `runtime simulate_pointer` chỉ bấm được nút UI; không giữ được trạng thái nhấn nên không kiểm tra được kéo thả. Kiểm tra kéo thả qua `BoardView.Press/Drag/Release`.
- Đổi scene trong code Editor làm asset đã tải thành null: luôn mở scene trước rồi mới tải asset.

## Ràng buộc bản quyền

Giấy phép Craftpix (https://craftpix.net/file-licenses/) cho phép dùng trong game thương mại nhưng **cấm phát tán file gốc**. Không đẩy `Assets/cat-defense-kit/`, `Ai/`, `Eps/` lên repo công khai. Không dùng asset để huấn luyện AI.

## Quy tắc làm việc

- Không áp dụng đầy đủ quy trình của bộ khung `.claude` (cổng duyệt, nhiều agent). Ưu tiên làm nhanh, giữ tài liệu đủ dùng.
- Chủ dự án cho toàn quyền thao tác; chỉ dừng lại hỏi khi khẩn cấp hoặc không tự làm được.
- Làm theo thứ tự mốc trong roadmap. Không thêm tính năng ngoài mốc đang làm nếu người dùng chưa yêu cầu.
- Chỉ số game (máu, sát thương, giá, wave) nằm trong ScriptableObject ở `Assets/Game/Data/`, không viết cứng trong code.
- Logic thuần (merge, kinh tế, sinh wave, lưu game) tách khỏi MonoBehaviour để test được bằng EditMode.
- Mỗi đầu việc xong phải kèm cách kiểm tra: test chạy qua, hoặc ghi rõ bước người dùng cần chơi thử.
- Commit cục bộ khi xong mỗi mốc. Không push khi chưa được yêu cầu.
- Không tự cảm nhận được game có vui hay không: mọi quyết định cân bằng cần người dùng chơi thử xác nhận.
