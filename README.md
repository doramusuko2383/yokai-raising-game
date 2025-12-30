# yokai-raising-game
A casual Yokai idle growing game for Android built with Unity.

## Buttons (Normal Daily Actions)
- Btn_Purify: Normal purification (daily care).
- Btn_Dango: Normal dango (daily energy recovery).

## Special Recovery Actions (Not Normal Buttons)
- Emergency Purification:
  - Used to recover from Mononoke state.
  - Separate from Btn_Purify.

- Special Triple Dango:
  - Used to recover from Near-Death state.
  - Separate from Btn_Dango.

## Character States Summary
### Normal
- Player regularly uses:
  - Btn_Purify to control Kegare.
  - Btn_Dango to recover Energy.

### Mononoke State
- Trigger: Kegare reaches MAX and is left unattended.
- Recovery: Emergency Purification (special action).

### Near-Death State
- Trigger: Energy reaches 0 and is left unattended.
- Recovery: Special Triple Dango (special reward action).

## メンターキャラクター：おんみょうじい

本作では、プレイヤーを導くメンターとして  
「おんみょうじい」というキャラクターが登場する。

### 設定
- 主人公のおじいちゃん
- 妖怪を見守る年老いた陰陽師
- 昔から妖怪と人の世界の間に立つ存在

主人公は幼いころから彼を  
「おんみょうじい」と呼んでいる。

### 役割
- ゲーム内のチュートリアル説明
- 妖怪の状態変化に関する解説
- プレイヤーの行動を肯定的に見守る存在

### 口調
- 「〜じゃ」「〜のう」を基本とした穏やかな語り
- 命令・叱責は行わない
- 状況を観察し、簡潔に伝えるのみ

### オープニング
- 初回起動時用のオープニング会話を用意している
- 開発・テスト効率を考慮し、デフォルトでは非表示
- 設定やデバッグフラグで表示切替可能

### 設計意図
説明文をすべて「おんみょうじい」の語りとして統一することで、
チュートリアル・警告・ヘルプを自然に世界観へ溶け込ませる。
