using System.Collections.Generic;
using UnityEngine;

public static class SEHub
{
    static readonly HashSet<YokaiSE> PlayedThisFrame = new HashSet<YokaiSE>();
    static int lastFrame = -1;

    // SE設計メモ:
    // - 命名規則（将来の音ファイル名）: se_<category>_<action>.wav
    //   例: se_evolution_charge.wav / se_evolution_burst.wav / se_purify_success.wav
    // - 推奨音量は 0.6〜0.9 の範囲で調整（ゲーム内SFXの基準音量）
    // - 推奨長さは 0.2s〜0.7s を目安（演出テンポを崩さない短尺）
    //
    // YokaiSE ↔ 期待する音素材:
    // - Evolution_Charge   : se_evolution_charge.wav  | 溜めのふわっと音 | 0.7 | 0.4s
    // - Evolution_Burst    : se_evolution_burst.wav   | 魔法破裂音        | 0.8 | 0.2s
    // - Evolution_Complete : se_evolution_complete.wav| 完了きらめき      | 0.85| 0.6s
    // - Danger_Start       : se_danger_start.wav      | 低い警告音        | 0.75| 0.35s
    // - Danger_End         : se_danger_end.wav        | 短い安堵音        | 0.7 | 0.25s
    // - Purify_Success     : se_purify_success.wav    | 清らかな成功音    | 0.8 | 0.5s
    //
    // 将来の実装メモ:
    // - AudioManager への差し替え場所: Debug.Log の直後（PlayOneShot へ移行）
    // - 実装時は 1フレーム重複抑制を維持し、同SEの多重再生を防ぐ
    // - ミュート/親AudioGroup設定は AudioManager 側で一元管理
    public static void Play(YokaiSE se)
    {
        if (!EffectSettings.EnableEffects)
        {
            EffectSettings.LogEffectsOff($"[SE] {se} skipped.");
            return;
        }

        int frame = Time.frameCount;
        if (frame != lastFrame)
        {
            lastFrame = frame;
            PlayedThisFrame.Clear();
        }

        if (!PlayedThisFrame.Add(se))
            return;

        Debug.Log($"[SE] {se}");
        // TODO: Replace with AudioManager hook when available.
    }
}
