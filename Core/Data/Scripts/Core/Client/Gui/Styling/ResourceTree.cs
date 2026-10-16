using System.Collections.Generic;

namespace LcdMod.Client.Gui.Styling
{
    public sealed class ResourceTree
    {
        readonly Dictionary<int, object> _values = new Dictionary<int, object>();

        public ResourceTree Set<TValue>(ResourceKey<TValue> key, TValue value)
        {
            _values[key.Id] = new LiteralStyleValue<TValue>(value);
            return this;
        }

        public ResourceTree Set<TValue>(string key, TValue value)
        {
            ResourceKey<TValue> resourceKey;
            if (!ResourceKey.TryGet(key, out resourceKey))
                resourceKey = ResourceKey.Register<TValue>(key);
            return Set(resourceKey, value);
        }

        public bool TryGet<TValue>(ResourceKey<TValue> key, out TValue value)
        {
            object raw;
            if (_values.TryGetValue(key.Id, out raw))
            {
                var typed = raw as LiteralStyleValue<TValue>;
                if (typed != null)
                    return typed.TryResolve(null, out value);
            }

            value = default(TValue);
            return false;
        }
    }
}
