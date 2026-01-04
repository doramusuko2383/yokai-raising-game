using UnityEngine;

// AudioHook は将来 AudioManager / AudioSource を導入する際の差し替えポイント。
// 現段階では音を鳴らさず、設計ドキュメントとしてコメントのみを保持する。
//
// 想定する責務:
// - YokaiSE → AudioClip のマッピング
// - 音量/長さの設計をここで一元管理
// - ミュートや親AudioGroupの拡張ポイント
//
// 命名規則（将来の音ファイル名）:
// - se_<category>_<action>.wav
//   例: se_evolution_charge.wav / se_evolution_burst.wav / se_purify_success.wav
//
// 将来の実装メモ:
// - AudioManager.PlayOneShot(clip, volume) を呼ぶ行に差し替え
// - AudioSource の親は「SFX」Group に集約して一括ミュート可能にする
// - 3D化が必要になった場合は位置引数を追加する
public static class AudioHook
{
    // NOTE: ここに Play(YokaiSE se) を追加して SEHub から委譲する想定。
    // 例: AudioManager.PlayOneShot(GetClip(se), GetVolume(se));
}
