namespace WarframeRelics;

public class WriteLimiter
{
    private readonly Queue<DateTime> _writeTimestamps = new();

    public async Task Wait()
    {
        DateTime now = DateTime.Now;

        DateTime next;
        lock (_writeTimestamps)
        {
            ClearOld(now);

            _writeTimestamps.Enqueue(now);
            next = _writeTimestamps.Count < 58 ? now.AddSeconds(-1) : _writeTimestamps.Peek().AddMinutes(1);
        }

        if (next < now)
            return;

        TimeSpan delay = next - now;
        Console.WriteLine($"Waiting {delay.TotalMilliseconds:0} ms for spreadsheet api to allow further writes");
        await Task.Delay(delay);
    }

    private void ClearOld(DateTime now)
    {
        DateTime cutoff = now.AddMinutes(-1);
        while (_writeTimestamps.TryPeek(out DateTime oldest) && oldest < cutoff)
            _writeTimestamps.Dequeue();
    }
}