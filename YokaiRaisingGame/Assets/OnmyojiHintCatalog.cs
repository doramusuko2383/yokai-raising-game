using System.Collections.Generic;

public static class OnmyojiHintCatalog
{
    static readonly Dictionary<OnmyojiHintType, string> HintMessages = new Dictionary<OnmyojiHintType, string>
    {
        { OnmyojiHintType.EnergyZero, "ちからが なくなっておる" },
        { OnmyojiHintType.EnergyRecovered, "うむ、げんきが もどったのう" },
        { OnmyojiHintType.KegareWarning, "けがれが たまってきとるのう" },
        { OnmyojiHintType.KegareMax, "けがれが たまりすぎて モノノケに なってしもうた" },
        { OnmyojiHintType.KegareRecovered, "あぶない ところじゃったわい" },
        { OnmyojiHintType.OkIYomeGuide, "まわりを なぞって おきよめ するのじゃ" },
        { OnmyojiHintType.EvolutionStart, "なにか ようすが おかしいようじゃ" },
        { OnmyojiHintType.EvolutionCompleteChild, "しんかしたようじゃな" },
        { OnmyojiHintType.EvolutionCompleteAdult, "本来の姿に なったようじゃ" }
    };

    public static string GetMessage(OnmyojiHintType type)
    {
        if (HintMessages.TryGetValue(type, out string message))
            return message;

        return string.Empty;
    }
}
