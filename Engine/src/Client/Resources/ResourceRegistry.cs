namespace Terr3D.Client.Resources;

public class RegistryOfResources<T>
{
    private readonly Dictionary<string, T> _storage = new();

    public void Register(string key, T item)
    {
        _storage[key] = item;
    }

    public T this[string key]
    {
        get
        {
            if (key == null) return default!;
            return _storage.GetValueOrDefault(key)!;
        }
    }

    public IEnumerable<string> Keys => _storage.Keys;
    public IEnumerable<T> Values => _storage.Values;

    public bool Contains(string key) => _storage.ContainsKey(key);
}
