namespace Common.Core;

public class Singleton<T> where T : class
{
    private static readonly Lazy<T> _lazyInstance = new (() =>
    {
        var instance = Activator.CreateInstance(typeof(T), nonPublic: true) as T;

        if (instance == null)
        {
            throw new Exception($"Failed to create instance of {typeof(T).Name}. " +
                                $"Make sure it has a private parameterless constructor.");
        }

        return instance;
    });

    public static T Instance => _lazyInstance.Value;
    
    protected Singleton()
    {
    }
    
}