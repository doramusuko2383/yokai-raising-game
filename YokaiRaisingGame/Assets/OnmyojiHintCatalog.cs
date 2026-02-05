using System.Collections.Generic;

public static class OnmyojiHintCatalog
{
    static readonly Dictionary<OnmyojiHintType, string> HintMessages = new Dictionary<OnmyojiHintType, string>
    {
        { OnmyojiHintType.EnergyZero, "ちからが なくなっておる" },
        { OnmyojiHintType.EnergyRecovered, "うむ、げんきが もどったのう" },
        { OnmyojiHintType.PurityWarning, "せいじょうどが さがってきとるのう" },
        { OnmyojiHintType.PurityEmpty, "せいじょうどが なくなって モノノケに なってしもうた" },
        { OnmyojiHintType.PurityRecovered, "あぶない ところじゃったわい" },
        { OnmyojiHintType.PurityEmergencyRecover, "けがれが溜まりすぎておる。\nこのままではモノノケになってしまう……\n案ずるでない、わしにまかせい！" },
        { OnmyojiHintType.OkIYomeGuide, "おきよめボタンを ながおし するのじゃ" },
        { OnmyojiHintType.OkIYomeSuccess, "よしよし、きれいに なったぞい" },
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
