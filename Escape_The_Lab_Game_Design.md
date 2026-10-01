# ESCAPE THE LAB --- Thiết kế đồ án Game 3D Android

## 1. Tổng quan

**Tên game:** Escape The Lab\
**Thể loại:** 3D Puzzle Adventure + Stealth nhẹ\
**Nền tảng:** Android\
**Engine:** Unity 6 LTS\
**Ngôn ngữ:** C#\
**Góc nhìn:** Third Person\
**Phong cách:** Stylized Sci-Fi\
**Thời lượng một lượt chơi:** 5--10 phút\
**Quy mô:** 1 map chính, 1 nhân vật người chơi, 1 robot bảo vệ, 3--4
puzzle chính.

### Mục tiêu đồ án

Game được thiết kế bám sát 3 nhóm tiêu chí của game 3D:

1.  **Tính giải trí và thú vị:** cốt truyện, gameplay, nhiệm vụ, thử
    thách, cơ chế tương tác và thế giới có phản hồi.
2.  **Đồ họa đẹp mắt:** model 3D, môi trường, materials, textures,
    shaders và phong cách nghệ thuật thống nhất.
3.  **Animation/VFX/Audio/độ hoàn thiện:** animation, va chạm, hiệu ứng
    vật lý, VFX, âm thanh, nhạc nền, Android ổn định và demo mượt.

------------------------------------------------------------------------

# 2. Ý tưởng game

Người chơi tỉnh dậy trong một phòng thí nghiệm công nghệ cao đang mất
điện.

Cửa thoát bị khóa. Hệ thống an ninh vẫn hoạt động và một robot bảo vệ
đang tuần tra.

Người chơi phải khám phá phòng thí nghiệm, tìm manh mối, thu thập vật
phẩm, khôi phục nguồn điện, giải các câu đố và tìm cách mở cửa thoát.

Điểm đặc biệt:

> **Việc người chơi khôi phục nguồn điện sẽ làm thay đổi trạng thái của
> toàn bộ môi trường:** đèn sáng, máy tính hoạt động, cửa mở và robot
> được kích hoạt.

Game không cần open world, multiplayer hay hệ thống phức tạp. Trọng tâm
là một map nhỏ nhưng được làm đẹp, có tương tác và có gameplay hoàn
chỉnh.

------------------------------------------------------------------------

# 3. Cốt truyện

Người chơi vào vai **Alex**, một kỹ thuật viên được cử đến kiểm tra một
phòng thí nghiệm nghiên cứu robot.

Khi Alex tỉnh lại, toàn bộ cơ sở mất điện.

Một thông báo xuất hiện:

> **SYSTEM: EMERGENCY LOCKDOWN**

Cửa chính bị khóa.

Alex phát hiện một robot bảo vệ vẫn đang hoạt động. Robot không cho phép
người chơi tiếp cận khu vực trung tâm.

Alex phải tìm hiểu:

-   Chuyện gì đã xảy ra?
-   Tại sao phòng thí nghiệm bị phong tỏa?
-   Vì sao robot vẫn hoạt động?
-   Làm thế nào để mở cửa thoát?

Cuối game, người chơi khôi phục nguồn điện và mở được cửa thoát.

Không cần xây dựng cốt truyện quá dài; câu chuyện được truyền tải qua
môi trường, terminal, bảng cảnh báo và một vài đoạn text ngắn.

------------------------------------------------------------------------

# 4. Core Gameplay Loop

``` text
Khám phá
    ↓
Tìm vật phẩm / manh mối
    ↓
Giải puzzle
    ↓
Mở khu vực mới
    ↓
Tránh / đánh lạc hướng Robot
    ↓
Tương tác với môi trường
    ↓
Khôi phục hệ thống điện
    ↓
Mở cửa thoát
    ↓
Hoàn thành game
```

------------------------------------------------------------------------

# 5. Gameplay chính

## 5.1. Di chuyển

Người chơi có:

-   Đi bộ
-   Chạy
-   Nhảy
-   Xoay camera
-   Tương tác

Không cần combat phức tạp.

## 5.2. Tương tác

Người chơi có thể tương tác với:

-   Cửa
-   Máy tính
-   Terminal
-   Generator
-   Tủ
-   Keycard reader
-   Nút bấm
-   Vật phẩm
-   Bảng điều khiển

Khi đứng gần:

``` text
[TƯƠNG TÁC]
```

xuất hiện trên UI.

## 5.3. Vật phẩm

Các vật phẩm chính:

-   Security Card
-   Fuse
-   Battery
-   Access Code
-   Laboratory Key

Không cần inventory lớn. Chỉ cần hệ thống lưu trạng thái đã sở hữu vật
phẩm.

------------------------------------------------------------------------

# 6. Thiết kế Map

Chỉ sử dụng **một map chính**, chia thành nhiều khu vực.

``` text
                    ┌──────────────────┐
                    │  PHÒNG NGHIÊN CỨU│
                    └────────┬─────────┘
                             │
                    ┌────────▼─────────┐
                    │  HÀNH LANG CHÍNH │
                    └───────┬──────────┘
                            │
             ┌──────────────┼──────────────┐
             │              │              │
      ┌──────▼─────┐ ┌─────▼──────┐ ┌────▼─────┐
      │  PHÒNG KHO │ │ PHÒNG ĐIỆN │ │ SERVER   │
      └──────┬─────┘ └─────┬──────┘ └────┬─────┘
             │              │              │
             └──────────────┼──────────────┘
                            │
                    ┌───────▼────────┐
                    │   CỬA THOÁT    │
                    └────────────────┘
```

## Khu vực 1 --- Phòng nghiên cứu

Mục đích:

-   Tutorial
-   Giới thiệu cốt truyện
-   Tìm manh mối đầu tiên

Có:

-   Bàn nghiên cứu
-   Máy tính
-   Hộp dụng cụ
-   Terminal

## Khu vực 2 --- Hành lang

Mục đích:

-   Kết nối các khu vực
-   Giới thiệu Robot

Có:

-   Đèn nhấp nháy
-   Camera
-   Cửa khóa
-   Robot tuần tra

## Khu vực 3 --- Phòng kho

Mục đích:

-   Tìm Fuse

Có:

-   Kệ hàng
-   Thùng
-   Tủ
-   Một puzzle nhỏ

## Khu vực 4 --- Phòng điện

Mục đích:

-   Khôi phục nguồn điện

Có:

-   Generator
-   Control panel
-   Fuse socket
-   Đèn cảnh báo

## Khu vực 5 --- Server Room

Mục đích:

-   Tìm Access Code
-   Giải puzzle cuối

Có:

-   Server
-   Terminal
-   Hologram
-   Cáp điện

## Khu vực 6 --- Cửa thoát

Sau khi hoàn thành tất cả điều kiện:

``` text
Power Restored
+
Access Code
+
Security Card
=
EXIT UNLOCKED
```

------------------------------------------------------------------------

# 7. Puzzle Design

## Puzzle 1 --- Security Card

Mục tiêu:

> Tìm Security Card trong phòng nghiên cứu.

Luồng:

``` text
Tìm Terminal
    ↓
Đọc manh mối
    ↓
Tìm tủ
    ↓
Mở tủ
    ↓
Nhận Security Card
```

## Puzzle 2 --- Fuse

Mục tiêu:

> Tìm Fuse trong kho.

Người chơi phải tìm đúng thùng dựa trên ký hiệu được hiển thị trên
Terminal.

## Puzzle 3 --- Generator

Mục tiêu:

> Khôi phục điện.

``` text
Có Fuse
    ↓
Lắp Fuse
    ↓
Bật Generator
    ↓
Điện được khôi phục
```

Sau khi bật:

-   Đèn sáng
-   Terminal hoạt động
-   Một số cửa mở
-   Robot chuyển sang trạng thái hoạt động mạnh hơn
-   VFX điện xuất hiện
-   Âm thanh máy móc bắt đầu phát

## Puzzle 4 --- Server Code

Mục tiêu:

> Tìm mã mở cửa thoát.

Ví dụ:

``` text
BLUE → RED → GREEN
```

Nếu nhập sai:

``` text
ACCESS DENIED
```

Nếu đúng:

``` text
ACCESS GRANTED
EXIT UNLOCKED
```

------------------------------------------------------------------------

# 8. Robot AI

Chỉ cần **1 loại Robot**, nhưng AI phải rõ ràng.

## Trạng thái

``` text
PATROL
   ↓
DETECT
   ↓
CHASE
   ↓
SEARCH
   ↓
RETURN
   ↓
PATROL
```

## PATROL

Robot đi giữa các waypoint.

## DETECT

Robot phát hiện Player bằng:

-   Khoảng cách
-   Field of View
-   Line of Sight

Thông số đề xuất:

-   Detection Range: 10--12m
-   FOV: 90°
-   Detection Time: 0.5--1 giây

## CHASE

Robot sử dụng NavMeshAgent để đuổi theo Player.

## SEARCH

Khi mất Player:

-   Robot đi tới vị trí cuối cùng nhìn thấy Player
-   Tìm trong vài giây
-   Nếu không thấy thì RETURN

## RETURN

Robot quay về waypoint gần nhất.

------------------------------------------------------------------------

# 9. Không cần combat phức tạp

Robot có thể gây nguy hiểm bằng cách:

-   Đuổi theo
-   Chạm Player → mất HP
-   Kích hoạt Alarm

Người chơi chủ yếu **né và đánh lạc hướng**.

Điều này giúp giảm đáng kể khối lượng code.

------------------------------------------------------------------------

# 10. Cơ chế tương tác môi trường

Đây là điểm quan trọng để đáp ứng tiêu chí:

> Thế giới game sinh động và có tính tương tác cao.

Ví dụ:

### Generator

``` text
Generator OFF
    ↓
Player lắp Fuse
    ↓
Generator ON
    ↓
Lights ON
    ↓
Computer ON
    ↓
Door UNLOCK
    ↓
Robot ACTIVE
```

### Terminal

Khi tương tác:

``` text
> SYSTEM STATUS

POWER: OFF
SECURITY: ACTIVE
EXIT: LOCKED
```

Sau khi khôi phục điện:

``` text
> SYSTEM STATUS

POWER: ONLINE
SECURITY: ACTIVE
EXIT: LOCKED
```

Sau khi nhập code:

``` text
> EXIT

ACCESS GRANTED
EXIT DOOR UNLOCKED
```

------------------------------------------------------------------------

# 11. Phong cách đồ họa

## Stylized Sci-Fi Laboratory

Mục tiêu:

-   Không realistic quá mức
-   Không cartoon quá mức
-   Đồng nhất toàn game

### Màu chủ đạo

-   Xám kim loại
-   Trắng
-   Xanh dương
-   Cyan

### Màu cảnh báo

-   Đỏ
-   Cam

### Vật liệu

-   Metal
-   Glass
-   Plastic
-   Emissive panels

------------------------------------------------------------------------

# 12. Model 3D

## Player

1 nhân vật.

## Robot

1 robot.

## Environment

-   Wall
-   Floor
-   Ceiling
-   Door
-   Window

## Props

-   Computer
-   Server
-   Generator
-   Table
-   Chair
-   Box
-   Shelf
-   Pipe
-   Cable
-   Terminal
-   Warning sign

Không cần quá nhiều model. Ưu tiên chất lượng và sự nhất quán.

------------------------------------------------------------------------

# 13. Materials / Textures / Shaders

Sử dụng:

-   Metal material
-   Glass material
-   Emission material
-   Sci-Fi panel material

Các object quan trọng nên có emission:

-   Terminal
-   Buttons
-   Generator
-   Warning light
-   Robot sensor

Có thể dùng shader đơn giản, ưu tiên hiệu năng Android.

------------------------------------------------------------------------

# 14. Animation

## Player

-   Idle
-   Walk
-   Run
-   Jump
-   Interact

## Robot

-   Idle
-   Walk
-   Patrol
-   Alert
-   Chase
-   Attack
-   Search

## Environment

-   Door Open
-   Door Close
-   Generator Start
-   Button Press

------------------------------------------------------------------------

# 15. VFX

## Generator

-   Spark
-   Electric arc
-   Glow

## Robot

-   Sensor light
-   Hit spark
-   Damage effect

## Door

-   Unlock effect
-   Light indicator

## Alarm

-   Red flashing light
-   Particle/smoke nhẹ

Không cần quá nhiều particle để tránh giảm FPS.

------------------------------------------------------------------------

# 16. Audio

## Nhạc nền

-   Laboratory Ambient
-   Suspense

## Sound Effects

-   Footstep
-   Door
-   Button
-   Pickup
-   Generator
-   Electric
-   Robot
-   Alarm
-   Hit
-   UI Click

## Environmental Audio

-   Máy móc
-   Quạt
-   Điện
-   Server

------------------------------------------------------------------------

# 17. UI

## Main Menu

``` text
ESCAPE THE LAB

[ PLAY ]
[ SETTINGS ]
[ EXIT ]
```

## Gameplay

``` text
HP ██████████

OBJECTIVE
Find the Security Card
```

Bên trái:

-   Virtual Joystick

Bên phải:

-   Interact
-   Jump
-   Run

## Notification

``` text
SECURITY CARD ACQUIRED
```

## Objective

``` text
✓ Find Security Card
✓ Find Fuse
□ Restore Power
□ Find Access Code
□ Escape
```

------------------------------------------------------------------------

# 18. Android Controls

### Joystick

Bên trái màn hình.

### Nút

Bên phải:

``` text
      [INTERACT]

[JUMP]       [RUN]
```

Không sử dụng quá nhiều nút.

------------------------------------------------------------------------

# 19. Cấu trúc Unity Project

``` text
Assets/
├── _Project/
│   ├── Art/
│   │   ├── Characters/
│   │   ├── Environment/
│   │   ├── Props/
│   │   ├── Materials/
│   │   └── Textures/
│   │
│   ├── Audio/
│   │   ├── Music/
│   │   ├── SFX/
│   │   └── Ambient/
│   │
│   ├── Animations/
│   ├── Prefabs/
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   └── Lab.unity
│   │
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Player/
│   │   ├── AI/
│   │   ├── Interaction/
│   │   ├── Puzzle/
│   │   ├── UI/
│   │   └── Audio/
│   │
│   ├── UI/
│   └── VFX/
│
└── ThirdParty/
```

------------------------------------------------------------------------

# 20. Các Script chính

``` text
Core/
├── GameManager.cs
└── GameState.cs

Player/
├── PlayerController.cs
├── PlayerInteraction.cs
├── PlayerHealth.cs
└── MobileInputController.cs

AI/
├── RobotAI.cs
├── RobotDetection.cs
├── RobotState.cs
└── RobotPatrol.cs

Interaction/
├── IInteractable.cs
├── Door.cs
├── Terminal.cs
├── Generator.cs
├── PickupItem.cs
└── KeycardReader.cs

Puzzle/
├── PuzzleManager.cs
├── SecurityCardPuzzle.cs
├── GeneratorPuzzle.cs
└── AccessCodePuzzle.cs

UI/
├── UIManager.cs
├── ObjectiveUI.cs
├── InteractionUI.cs
└── NotificationUI.cs

Audio/
└── AudioManager.cs
```

------------------------------------------------------------------------

# 21. Kiến trúc code

Ưu tiên kiến trúc đơn giản, dễ hiểu.

``` text
GameManager
│
├── Player
│
├── PuzzleManager
│
├── ObjectiveManager
│
├── AudioManager
│
└── UIManager
```

Các object tương tác sử dụng:

``` csharp
public interface IInteractable
{
    void Interact();
}
```

Các object:

``` text
IInteractable
├── Door
├── Terminal
├── Generator
├── PickupItem
└── KeycardReader
```

------------------------------------------------------------------------

# 22. Trạng thái Game

``` text
GameState
├── SecurityCardCollected
├── FuseCollected
├── PowerRestored
├── AccessCodeSolved
└── ExitUnlocked
```

Điều kiện hoàn thành:

``` text
SecurityCardCollected
        AND
FuseCollected
        AND
PowerRestored
        AND
AccessCodeSolved
        ↓
ExitUnlocked
```

------------------------------------------------------------------------

# 23. Performance Android

Mục tiêu:

> 30 FPS trở lên trên thiết bị Android tầm trung.

Áp dụng:

-   Low-poly model
-   Texture compression
-   Baked lighting
-   Hạn chế realtime lights
-   LOD
-   Occlusion Culling
-   Mobile-friendly shader
-   Object Pooling cho particle
-   Hạn chế Instantiate/Destroy liên tục
-   Hạn chế particle quá lớn

------------------------------------------------------------------------

# 24. Phạm vi dự án

## Bắt buộc

-   1 map
-   1 Player
-   1 Robot
-   4 puzzle
-   5 loại item
-   1 cửa thoát
-   Android controls
-   Animation
-   VFX
-   Audio
-   UI
-   Android build

## Không làm

-   Multiplayer
-   Online
-   Open world
-   Database server
-   Shop
-   Multiplayer AI
-   Hệ thống vũ khí phức tạp
-   Character customization
-   Nhiều level độc lập

Mục tiêu là **ít tính năng nhưng hoàn thiện cao**.

------------------------------------------------------------------------

# 25. Timeline

## Giai đoạn 1 --- Foundation

-   Tạo Unity project
-   Android build
-   Player
-   Camera
-   Mobile controls

## Giai đoạn 2 --- Interaction

-   Interface IInteractable
-   Door
-   Item
-   Terminal
-   Generator

## Giai đoạn 3 --- Puzzle

-   Security Card
-   Fuse
-   Generator
-   Access Code
-   Exit

## Giai đoạn 4 --- AI

-   NavMesh
-   Patrol
-   Detection
-   Chase
-   Search
-   Return

## Giai đoạn 5 --- Art

-   Environment
-   Materials
-   Textures
-   Lighting
-   Props

## Giai đoạn 6 --- Polish

-   Animation
-   VFX
-   Audio
-   UI
-   Notifications

## Giai đoạn 7 --- Optimization

-   FPS
-   Memory
-   Android testing
-   Bug fixing
-   APK

------------------------------------------------------------------------

# 26. Mapping với tiêu chí chấm điểm

  Tiêu chí               Cách đáp ứng
  ---------------------- -------------------------------------------
  Cốt truyện hấp dẫn     Bí ẩn phòng thí nghiệm
  Gameplay thú vị        Puzzle + Stealth + Exploration
  Nhiệm vụ/thử thách     4 puzzle + Robot
  Cơ chế game            Power system + Robot AI + Interaction
  Thế giới tương tác     Door, Terminal, Generator, Server, Lights
  Model 3D               Player, Robot, Props
  Địa hình/Môi trường    Laboratory nhiều khu vực
  Materials              Metal, Glass, Emission
  Textures               Sci-Fi textures
  Shaders                Emission/Mobile shader
  Nhất quán nghệ thuật   Stylized Sci-Fi
  Animation              Player, Robot, Door, Generator
  Va chạm                Player-Robot, interaction
  Vật lý                 Props/Particle nhẹ
  VFX                    Spark, Electric, Alarm, Unlock
  Âm thanh               Music, SFX, Ambient
  Hoàn thiện             UI, Objective, Android build
  Ổn định                Optimization, testing
  Demo mượt              30+ FPS mục tiêu

------------------------------------------------------------------------

# 27. Kịch bản Demo

Thời lượng khoảng 5 phút.

### 0:00--0:30

Mở game.

### 0:30--1:00

Cho thấy:

-   Movement
-   Camera
-   Mobile controls

### 1:00--2:00

Tìm Security Card và Fuse.

### 2:00--3:00

Giải Generator Puzzle.

Cho thấy:

-   VFX
-   Sound
-   Lights ON
-   Environment changes

### 3:00--4:00

Cho Robot phát hiện Player.

Robot:

``` text
PATROL
→ DETECT
→ CHASE
→ SEARCH
→ PATROL
```

### 4:00--4:30

Giải Access Code.

### 4:30--5:00

Mở cửa và Escape.

------------------------------------------------------------------------

# 28. Tiêu chuẩn hoàn thành

Game được coi là hoàn thành khi:

-   Có thể build APK
-   Player di chuyển mượt
-   Camera hoạt động
-   Mobile controls hoạt động
-   Có ít nhất 4 puzzle
-   Robot AI hoạt động
-   Có tương tác môi trường
-   Có animation
-   Có VFX
-   Có âm thanh
-   Có UI
-   Có objective
-   Có màn hình thắng
-   Không crash trong quá trình demo
-   FPS ổn định trên thiết bị mục tiêu

------------------------------------------------------------------------

# 29. Kết luận

**Escape The Lab** được cố tình thiết kế theo hướng:

> **Một map nhỏ + gameplay rõ ràng + môi trường tương tác + đồ họa thống
> nhất + polish tốt.**

Đây là lựa chọn phù hợp hơn với yêu cầu của đồ án so với việc xây một
game 3D quá lớn.

Mục tiêu không phải làm một game AAA mà là tạo một sản phẩm có thể
**demo hoàn chỉnh, đẹp, ổn định và đáp ứng trực tiếp cả 3 tiêu chí chấm
game 3D.**
