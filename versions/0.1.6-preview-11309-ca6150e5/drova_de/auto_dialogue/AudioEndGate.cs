namespace DrovaAutoDialogue;

// Engine-independent decision logic; no wall-clock guess based on text length.
public sealed class AudioEndGate
{
    public bool Started { get; private set; }
    public bool Finished { get; private set; }
    public bool Cancelled { get; private set; }
    public bool Sent { get; private set; }
    double _furthest;
    double _quietTime;
    double _waitingTime;

    public bool Tick(bool playing, double position, double duration, double delta,
                     bool suspended, double delay)
    {
        if (Cancelled || Sent || suspended) return false;
        if (!double.IsFinite(duration) || duration <= 0 || !double.IsFinite(position)
            || !double.IsFinite(delta) || delta < 0 || !double.IsFinite(delay))
        { Cancelled = true; return false; }
        if (playing)
        {
            // A restarted clip is not evidence that the previous playback ended.
            if (Finished) { Cancelled = true; return false; }
            Started = true;
            _furthest = Math.Max(_furthest, Math.Clamp(position, 0, duration));
            return false;
        }
        if (!Started)
        {
            _waitingTime += delta;
            if (_waitingTime >= 15) Cancelled = true;
            return false;
        }
        // Stop(), a failed load, or a pause must not masquerade as a natural end.
        // Include this frame's time because Unity can reset the cursor on completion.
        double tolerance = Math.Min(0.25, Math.Max(0.08, delta * 2));
        if (!Finished && _furthest < duration - tolerance)
        { Cancelled = true; return false; }
        Finished = true;
        _quietTime += delta;
        if (_quietTime < Math.Clamp(delay, 0, 2)) return false;
        Sent = true;
        return true;
    }
}
