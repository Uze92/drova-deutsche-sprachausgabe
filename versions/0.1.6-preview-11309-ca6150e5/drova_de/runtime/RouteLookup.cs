namespace DrovaVoices;

// The API supplies localization routes, not graph node IDs. Repeated nodes
// therefore need one unambiguous audio assignment before they reach playback.
public sealed class RouteLookup
{
    readonly Dictionary<(string,string,string,string),Route> _routes = new();
    public RouteLookup(IEnumerable<Route> routes)
    {
        foreach (var route in routes)
        foreach (string actor in route.ActorAliases.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var key=(route.Tree,route.Path,route.Key,actor.ToUpperInvariant());
            if (_routes.TryGetValue(key,out var existing)
                && (existing.Audio!=route.Audio || existing.AudioSha256!=route.AudioSha256
                    || existing.VoiceId!=route.VoiceId || existing.PlaybackGainDb!=route.PlaybackGainDb))
                throw new InvalidDataException("Conflicting dialogue audio route: "+key);
            _routes[key]=route;
        }
    }
    public Route? Find(string tree,string path,string key,string actor)
        => _routes.TryGetValue((tree,path,key,actor.ToUpperInvariant()),out var route)?route:null;
}
