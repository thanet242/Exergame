# 🍪 ระบบเกมวิ่ง 2D สไตล์ Cookie Run (Cookie Run System for Unity)

ชุดโค้ดนี้ถูกออกแบบมาเพื่อสร้างเกมวิ่งอัตโนมัติ 2D (Endless / Stage Runner) ที่มีระบบกลไกการเล่นแบบเกม **Cookie Run** ไว้อย่างครบถ้วน ภายในโฟลเดอร์ `Assets/Code/CookieRun`

---

## 📁 โครงสร้างไฟล์โค้ด (Script Overview)

| สคริปต์ | หน้าที่หลัก |
|---|---|
| `CookiePlayerController.cs` | ควบคุมการวิ่งอัตโนมัติ, กระโดด 2 จังหวะ (Double Jump), สไลด์มุดเปลี่ยนขนาด Collider, ตกเหว, ระบบบัฟ |
| `CookieHealth.cs` | ระบบ Energy/HP ที่ลดลงเรื่อยๆ ตามเวลา (Decay), โดนดาเมจ, เติมเลือดจาก Potion |
| `CookieObstacle.cs` | สิ่งกีดขวางบนพื้น (โดดข้าม), สิ่งกีดขวางด้านบน (สไลด์หลบ), สิ่งกีดขวางที่ทำลายได้ด้วย Giant/Blast |
| `CookieAudio.cs` | เสียงทั้งหมดของโหมดวิ่ง: เพลงประกอบ, เสียงกระโดด/สไลด์/เหรียญ/ไอเทม/ชน/สิ่งกีดขวาง, เสียงฝีเท้า, เสียงขึ้นระดับ และเพลงตอน Game Over (เปลี่ยนไฟล์เสียงได้ที่ GameObject `CookieAudio` ในฉาก) |
| `CookieRollingBoulder.cs` | บอลหินกลิ้งเข้าหาผู้เล่นเมื่อเข้าใกล้ (ต้องกระโดดข้าม) ปลดล็อกระดับ 2 |
| `CookieFallingStalactite.cs` | หินย้อยสั่นเตือนแล้วร่วงปักพื้นก่อนผู้เล่นมาถึง (ต้องกระโดดข้าม) ปลดล็อกระดับ 3 |
| `CookieSpikeTrap.cs` | หนามผุดขึ้น-หดลงเป็นจังหวะ มีกะพริบแดงเตือน ตั้ง `phaseOffset` ให้แต่ละอันไม่พร้อมกัน ปลดล็อกระดับ 4 |
| `CookieSpriteHitbox.cs` | ปรับ Hitbox ให้ตรงรูปทรงที่มองเห็น (PolygonCollider2D จาก Physics Shape ของ Sprite) และหดเข้าอีกเล็กน้อยตาม `hitboxScale` |
| `CookieRockVisual.cs` | ตกแต่งหิน: ย้อมสีโทนถ้ำ, เส้นขอบเข้ม, เงาใต้หิน, สุ่มรูปหิน/กลับด้านทุกครั้งที่ Chunk ถูกสร้าง |
| `CookieItem.cs` | ไอเทมเก็บ: เยลลี่คะแนน, เหรียญ, ขวดเลือด, แม่เหล็กดูดของ, ยักษ์ชนแหลก, บูสต์พุ่งไว |
| `CookieMapSpawner.cs` | ระบบต่อฉากสุ่มต่อเนื่อง (Chunk Spawner) ไม่รู้จบ และคืนฉากที่วิ่งผ่านแล้วเข้า Pool |
| `CookiePool.cs` | Object Pooling สำหรับ Chunk, เหรียญ/ไอเทม และเอฟเฟกต์ (ใช้ `CookiePool.Spawn` / `SpawnEffect` / `Release` แทน Instantiate/Destroy) สร้างตัวเองอัตโนมัติ ไม่ต้องวางใน Scene |
| `CookiePooledObject.cs` | ตัวกำกับวัตถุใน Pool (เพิ่มให้อัตโนมัติ) คอมโพเนนต์ที่ต้องรีเซ็ตตอนใช้ซ้ำให้ implement `ICookiePoolable` |
| `CookieCameraFollow.cs` | กล้องติดตามผู้เล่น 2D แบบ Smooth เลื่อนนำหน้า (Look-Ahead) และล็อคแกน Y |
| `CookieParallax.cs` | พื้นหลังเคลื่อนที่หลายมิติ (Parallax Scrolling) แบบวนซ้ำไร้รอยต่อ |
| `CookieGameManager.cs` | คุมสถานะเกม, นับคะแนน, เหรียญ, ระยะทาง (เมตร), บันทึก High Score อัตโนมัติ |
| `CookieUIManager.cs` | ควบคุมหลอดเลือด Energy Bar, ตัวเลขคะแนน/เหรียญ/ระยะทาง และหน้าต่าง Game Over |
| `CookieTouchButton.cs` | รองรับปุ่ม UI หน้าจอสำหรับการกดค้าง (Hold to Slide) และแตะกระโดด (Tap to Jump) |

---

## 🛠️ ขั้นตอนการติดตั้งใน Scene (Step-by-Step Setup)

### 1. การตั้งค่าตัวละคร (Player)
1. สร้าง GameObject 2D ว่างใน Scene ตั้งชื่อว่า `Player`
2. ใส่ Components ดังนี้:
   - `SpriteRenderer` (ใส่ Sprite ตัวละคร)
   - `Rigidbody2D`:
     - **Collision Detection**: `Continuous`
     - **Constraints**: ติ๊ก `Freeze Rotation Z`
   - `BoxCollider2D`: ปรับขนาดให้พอดีกับตัวละครขณะยืน
   - `CookiePlayerController`:
     - ลาก Ground Layer ที่สร้างไว้ (เช่น Layer `Ground`) ไปใส่ที่ช่อง `Ground Layer`
     - กำหนด `Ground Check Point` (สร้าง Empty GameObject ใต้เท้าตัวละคร)
     - ปรับขนาด `Slide Collider Size` และ `Slide Collider Offset` สำหรับตอนสไลด์
   - `CookieHealth`: กำหนด Max Health (เช่น 100) และ Decay Rate (เช่น 2.5 HP/วินาที)
   - `AudioSource` สำหรับเสียงกระโดด/สไลด์

### 2. การควบคุม (Controls)
- **คีย์บอร์ด**:
  - กระโดด / Double Jump: `Spacebar`, `W`, หรือ `ลูกศรขึ้น`
  - สไลด์มุด: กดค้าง `S`, `ลูกศรลง`, หรือ `Left Control`
- **หน้าจอมือถือ / Touch UI**:
  - รองรับผ่านคอมโพเนนต์ `CookieTouchButton` กดปุ่มค้างเพื่อสไลด์ และแตะเพื่อกระโดด

### 3. การสร้างสิ่งกีดขวาง (Obstacles)
1. **สิ่งกีดขวางบนพื้น (Ground Obstacle)**:
   - ใส่ `BoxCollider2D` (เปิด `Is Trigger`)
   - ใส่คอมโพเนนต์ `CookieObstacle` เลือกประเภท `GroundObstacle`
   - ผู้เล่นต้องกระโดดข้าม ถ้าชนจะเสียเลือด
2. **สิ่งกีดขวางด้านบน (Overhead Obstacle)**:
   - วางตำแหน่งไว้ระดับหัว-ตัว
   - ใส่ `BoxCollider2D` (เปิด `Is Trigger`)
   - ใส่คอมโพเนนต์ `CookieObstacle` เลือกประเภท `OverheadObstacle`
   - ผู้เล่นต้องกด **Slide มุด** เท่านั้น ถ้าสไลด์จะผ่านได้ไม่โดนดาเมจ

### 4. การสร้างไอเทมและเยลลี่ (Items / Jellies)
- สร้าง Sprite วัตถุ เช่น เหรียญ หรือเยลลี่
- ใส่ `CircleCollider2D` หรือ `BoxCollider2D` (เปิด `Is Trigger`)
- ใส่คอมโพเนนต์ `CookieItem` เลือกประเภท:
  - `Jelly`: เพิ่มคะแนน
  - `Coin`: เพิ่มเหรียญ
  - `HealthPotion`: ฟื้นฟู Energy
  - `Magnet`: ดึงดูดไอเทมรอบตัว
  - `Giant`: ตัวยักษ์ วิ่งชนสิ่งกีดขวางพัง
  - `Blast`: วิ่งไว พุ่งทะลวงอมตะ

### 5. การสร้างชิ้นส่วนฉากสุ่ม (Chunk Prefabs & Spawner)
1. รวมพื้น, สิ่งกีดขวาง, และเยลลี่ไว้ใน GameObject แม่ 1 อัน (เรียกว่า 1 Chunk กว้างประมาณ 20-30 ยูนิต)
2. สร้างจุดสิ้นสุด `EndPoint` (Empty Object) ไว้ที่ขอบขวาสุดของ Chunk
3. ลากทำเป็น Prefab เก็บไว้ใน Assets
4. ใน Scene สร้าง GameObject เปล่าใส่ `CookieMapSpawner` แล้วลาก Prefab Chunks ต่างๆ ใส่ในช่อง `Chunk Prefabs`

### 6. การตั้งค่า UI และกล้อง (HUD & Camera)
- **Main Camera**: ใส่คอมโพเนนต์ `CookieCameraFollow` แล้วลากตัวละครไปใส่ที่ช่อง `Target`
- **Canvas UI**:
  - ใส่คอมโพเนนต์ `CookieUIManager`
  - ลาก `Slider` หลอดเลือด, `TextMeshProUGUI` สำหรับ Score, Coins, Distance
  - ลากปุ่ม Jump และ Slide (ที่ใส่ `CookieTouchButton`) เข้ามาเชื่อมต่อ
  - ลาก `GameOverPanel` เข้ามาเชื่อมต่อ
