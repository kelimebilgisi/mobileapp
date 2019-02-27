using System;

namespace Toggl.Foundation.Serialization
{
    public interface IJsonSerializer
    {
        T Deserialize<T>(string json);
    }
}
