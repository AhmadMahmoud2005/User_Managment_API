using System.Collections.Concurrent;

public class RequestCountService
{
   private readonly ConcurrentDictionary<string, int> _requests = new();

   public void IncrementCount(string path)
   {
      _requests.AddOrUpdate(path, 1, (key, value) => value + 1);
   }
   public int GetCount(string path)
   {
      _requests.TryGetValue(path, out int value);
      return value;
   }
   public IDictionary<string, int> GetAllCounts()
   {
      return _requests;
   }
}
