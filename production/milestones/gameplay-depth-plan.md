# Kế hoạch tăng chiều sâu gameplay: Phóng mèo (B), Ba họ mèo (A), Hợp đồng wave (C)

> Lập ngày 2026-11-12. Trạng thái: **bước 0 và B đã code xong; chờ chủ dự án chơi thử bước B.** Thay cho prototype Tactical Merge trong roadmap.

## Vì sao làm

Chơi thử Merge Strike cho thấy ghép vẫn là thao tác hiển nhiên. Số liệu hiện tại giải thích điều đó: mèo cấp sau có sát thương gấp 2,1 lần cấp trước, nên một mèo ghép luôn mạnh hơn hai mèo chưa ghép. Game thiếu ba thứ: quân khác nhau, sự khan hiếm, và quyết định không rút lại được. Ba bước dưới đây lần lượt bổ sung từng thứ.

## Cách làm việc

- Người lập kế hoạch và kiểm nghiệm: phiên Claude chính. Người viết code: một agent model thấp hơn.
- **Làm tuần tự, mỗi lần một bước.** Cả ba bước đều sửa `GameManager.cs`, `BoardView.cs`, `GameBuilder.cs`, nên không chạy song song.
- Sau mỗi bước có một cổng: test tự động qua, phiên chính đọc diff, rồi chủ dự án chơi thử. Không qua cổng thì không làm bước sau.
- Mỗi bước một commit cục bộ. Không push.

### Quy tắc bắt buộc cho agent viết code

1. Đọc `CLAUDE.md` trước. Scene, prefab và dữ liệu do `Assets/Game/Editor/GameBuilder.cs` sinh ra: sửa builder rồi chạy `GameBuilder.Build()`, không sửa tay file `.unity` hay `.asset`.
2. Mọi con số tinh chỉnh nằm trong ScriptableObject, gán giá trị trong `GameBuilder`. Không viết cứng trong code gameplay.
3. Luật thuần (không dùng Unity) đặt trong `Assets/Game/Scripts/Core/` và có test EditMode.
4. Mọi chuỗi hiển thị thêm vào `Loc.cs`, đủ cả tiếng Anh và tiếng Việt.
5. Không sửa hay xoá test đang có để cho qua. Nếu một test cũ sai vì luật đổi, ghi rõ trong báo cáo test nào và vì sao.
6. Không thêm tính năng ngoài bước đang làm. Không đụng `Assets/cat-defense-kit/`.
7. Xong việc phải chạy `unity recompile` và `unity command test run -- --mode editor`, rồi báo số test qua/trượt kèm nguyên văn lỗi nếu có. Không tự kết luận cơ chế "vui" hay "ổn".

---

## Bước 0: Dọn Merge Strike (0,5 giờ)

Hiện trạng chưa commit: ghép chỉ hợp lệ giữa hai ô kề nhau; đòn đánh bằng một viên đạn.

| Việc | Chi tiết |
|---|---|
| 0.1 | Chủ dự án commit hiện trạng làm mốc so sánh. |
| 0.2 | Bỏ luật ghép kề: hai mèo cùng cấp ở bất kỳ ô nào đều ghép được, như trước. Xoá `MergeDirection`, `DirectionBetween`, `slotColumns` và constructor thứ hai của `MergeBoard`. |
| 0.3 | Đòn Merge Strike còn một dạng: đánh mọi quái trên hàng của ô đích. Sát thương = sát thương mèo mới × `mergeStrikeMultiplier` (thêm vào `LevelData`, giá trị nháp 4). Preview chỉ còn một hàng. |
| 0.4 | Sửa các test Merge Strike theo luật mới; đây là trường hợp được phép đổi test, ghi rõ trong báo cáo. |

**Xong khi:** mọi test qua; ghép hai mèo không kề nhau chạy được trong Play mode.

---

## Bước B: Phóng mèo (1–2 giờ)

**Luật:** kéo một con mèo và thả lên mặt đường. Mèo rời bàn vĩnh viễn, bay tới điểm thả rồi nổ, gây sát thương cho mọi quái trong bán kính.

| Thành phần | Quy tắc |
|---|---|
| Vùng thả hợp lệ | `x >= WallX + 0.5` và y nằm trong dải đường. Thả ở chỗ khác giữ hành vi cũ (về ô cũ). |
| Điều kiện | Chỉ khi `State == Playing` và không tạm dừng. |
| Sát thương | `cat.damage × launchMultiplier`, đi qua luật giáp hiện có. Trường mới trong `CatData`: `launchMultiplier` (nháp 12), `launchRadius` (nháp 1,8). |
| Bay | Khoảng 0,35 giây từ ô tới điểm thả, có độ cong nhẹ. Sát thương tính lúc chạm đất, với quái đang ở đó lúc ấy. |
| Hiệu ứng | Dùng `SpawnExplosion` và cue âm thanh nổ sẵn có. |
| Preview | Khi đang kéo mèo trên vùng thả hợp lệ, hiện vòng mờ đúng bán kính nổ. |
| Giá | Không thêm phí. Cái giá là mất mèo, và giá mua mèo vốn tăng theo số lần mua. |

**Chỗ sửa:** `BoardView.Release` (nhánh thả ngoài ô), `BoardView.Drag` (preview), hàm mới `GameManager.LaunchCat(CatData, Vector3 from, Vector3 to)`, `CatData`, `GameBuilder.BuildCats`. Luật chọn quái trong bán kính tách thành hàm thuần để test.

**Test cần có:** thả ngoài vùng không mất mèo; phóng xong ô trống và mua mèo mới được; quái trong bán kính nhận đúng sát thương, quái ngoài bán kính không nhận.

**Chủ dự án chơi thử, trả lời:**
- Có lúc nào bạn phân vân giữa giữ mèo và phóng mèo không?
- Phóng mèo cấp 1 vừa mua có thành chiến thuật áp đảo không? (Nếu có, tăng giá mua hoặc giảm `launchMultiplier`.)
- Có khi nào phóng mèo cứu được một hàng sắp vỡ không?

---

## Bước A: Ba họ mèo (2–3 giờ)

**Luật:** 15 sprite mèo chia thành 3 họ × 5 bậc. Chỉ ghép được hai mèo cùng họ, cùng bậc.

| Họ | Sprite | Đạn | Cách bắn | Mạnh khi | Yếu khi |
|---|---|---|---|---|---|
| Xạ thủ | C1–C5 | loại 1 | Như hiện tại: bắn nhanh, một mục tiêu | Quái lẻ, quái đổi hàng | Quái giáp |
| Xuyên | C6–C10 | loại 2 | Đạn bay thẳng hết hàng, trúng mọi quái trên đường; sát thương mỗi lần trúng × 0,6; bắn chậm hơn 1,4 lần | Hàng đông quái | Quái lẻ máu trâu |
| Pháo | C11–C15 | loại 3 | Bắn chậm 2,2 lần; sát thương × 2,5; nổ lan bán kính nhỏ | Quái giáp, boss | Quái nhanh, đông |

Các hệ số trên là bản nháp, đặt trong `CatData` (`family`, `damageScale` đã nhân sẵn vào `damage`, `fireInterval`, `pierce`, `splashRadius`).

**Cách hiện thực ít sửa nhất:** giữ `MergeBoard` lưu số 1–15 như hiện nay. Họ = `(level − 1) / 5`, bậc = `(level − 1) % 5 + 1`. Chỉ đổi hai chỗ:
- Không ghép khi mèo đang ở bậc 5 (`level % 5 == 0`); thêm tham số `tiersPerFamily` cho `MergeBoard`.
- `Place` nhận cấp bắt đầu (1, 6 hoặc 11) thay vì luôn là 1.

Sát thương theo bậc: `4 × 2,1^(bậc − 1) × hệ số họ`, không còn theo số 1–15.

**Mua mèo:** nút Mua tách thành hai nút, mỗi nút hiện icon một họ chọn ngẫu nhiên trong 3 (hai nút khác họ nhau). Mua xong thì rút lại cả hai. Giá như cũ.

**Chỗ sửa:** `MergeBoard`, `CatData`, `Cat` (bắn theo họ), `Bullet` (thêm chế độ xuyên: bay thẳng theo hàng, trúng mỗi quái một lần), `GameManager.BuyCat` và trạng thái hai lựa chọn, `Hud`, `GameBuilder` (`BuildCats`, `BuildHud`), `Loc`.

**Test cần có:** khác họ không ghép; bậc 5 không ghép tiếp; ghép giữ nguyên họ; hai lựa chọn mua luôn khác họ; đạn xuyên trúng mỗi quái đúng một lần.

**Rủi ro cần theo dõi:** trần sức mạnh hạ từ 15 cấp xuống 5 bậc. Nếu người chơi chạm bậc 5 sớm ở các level cuối, 30 level sẽ lệch cân bằng. Bước này chỉ kiểm tra level 1–6; cân bằng lại toàn chiến dịch thuộc mốc 5.

**Chủ dự án chơi thử, trả lời:**
- Bạn có chọn họ theo loại quái của level, hay luôn lấy một họ?
- Hai lựa chọn mua có tạo tình huống khó xử không, hay chỉ gây phiền?
- Họ nào thấy vô dụng?

---

## Bước C: Hợp đồng wave (1,5–2,5 giờ)

**Luật:** bảng thẻ sau mỗi wave giữ nguyên, nhưng mỗi thẻ gắn thêm một kiểu tấn công cho wave kế tiếp. Chọn thẻ nào thì chịu kiểu tấn công của thẻ đó. Không thêm bảng chọn mới.

| Kiểu tấn công | Hiệu lực với wave kế tiếp (nháp) |
|---|---|
| Biển zombie | Số quái × 1,6; khoảng cách sinh × 0,8; máu × 0,8 |
| Đột kích | Tốc độ quái × 1,4; số quái × 0,8 |
| Khổng lồ | Số quái × 0,7; thêm 1 quái tinh nhuệ máu × 6, to hơn |
| Dồn hàng | Toàn bộ quái chỉ ra ở 2 hàng |

Mỗi lần đưa thẻ, ba thẻ nhận ba kiểu khác nhau, chọn ngẫu nhiên. Mỗi thẻ hiện thêm một dòng mô tả kiểu tấn công, màu đỏ.

**Chỗ sửa:**
- `Core/`: struct thuần `WaveModifier` (các hệ số trên) và hàm áp vào danh sách `SpawnGroup` của một wave; `WaveRunner` nhận modifier cho wave kế tiếp.
- Dữ liệu: `WaveModifierData` (ScriptableObject: khoá tên, khoá mô tả, các hệ số), sinh trong `GameBuilder`.
- `GameManager`: `OfferPerks` ghép modifier vào từng lựa chọn; `ChoosePerk` kích hoạt modifier; `SpawnEnemyAt` áp hệ số máu và tốc độ; dải cảnh báo hàng phản ánh đúng "Dồn hàng".
- `Hud`, `GameBuilder.BuildHud`, `Loc`.

**Test cần có:** hệ số số quái và khoảng cách áp đúng một wave rồi hết; ba thẻ luôn nhận ba kiểu khác nhau; quái tinh nhuệ được thêm đúng một con; wave cuối (boss) không bị bỏ boss.

**Chủ dự án chơi thử, trả lời:**
- Bạn có chọn thẻ yếu hơn để tránh một kiểu tấn công không?
- Kiểu tấn công nào luôn bị né hoặc luôn được chọn?
- Wave sau khi chọn có khác nhau rõ rệt không?

---

## Tổng ước tính

| Bước | Giờ AI | Giờ chủ dự án chơi thử |
|---|---|---|
| 0. Dọn Merge Strike | 0,5 | – |
| B. Phóng mèo | 1–2 | 0,3 |
| A. Ba họ mèo | 2–3 | 0,5 |
| C. Hợp đồng wave | 1,5–2,5 | 0,5 |
| **Tổng** | **5–8** | **1,3** |

Chưa tính: hướng dẫn cho ba cơ chế mới (thuộc mốc 4.3) và cân bằng lại 30 level (mốc 5).

## Kết quả từng cổng

| Cổng | Ngày | Test | Nhận xét chơi thử | Quyết định |
|---|---|---|---|---|
| 0 | 2026-11-12 | 39/39 qua | Chưa chơi thử riêng | Giữ |
| B | 2026-11-12 | 42/42 qua; Play mode: phóng mèo cấp 1 vào quái, mèo rời bàn, quái trong vùng nổ chết, ô mua lại được | Chờ chủ dự án. Đã biết: vùng báo trước là hình vuông, vùng nổ thật là hình tròn | Chờ |
| A | | | | |
| C | | | | |
