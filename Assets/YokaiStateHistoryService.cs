using System.Collections.Generic;

namespace Yokai
{
    public class YokaiStateHistoryService
    {
        const int MaxHistory = 10;

        readonly Queue<string> history = new Queue<string>();

        public void Record(
            YokaiState previous,
            YokaiState next,
            string reason,
            int frame
        )
        {
            string entry = $"[Frame {frame}] {previous} -> {next} ({reason})";

            history.Enqueue(entry);

            if (history.Count > MaxHistory)
                history.Dequeue();

            YokaiLogger.State($"[HISTORY] {entry}");
        }

        public IReadOnlyCollection<string> GetHistory()
        {
            return history;
        }

        public IReadOnlyList<string> GetHistoryList()
        {
            return history.ToArray();
        }
    }
}
