namespace DrovaVoices;

// The audio API's static constructor needs the gameplay world. A failed static
// constructor stays failed for the entire process, so it must never be probed early.
public sealed class RuntimeStartGate
{
    public bool Attempted { get; private set; }
    public bool Started { get; private set; }

    public bool TryStart(bool worldReady, Action connect)
    {
        if (!worldReady || Attempted) return false;
        Attempted = true;
        connect();
        Started = true;
        return true;
    }
}
