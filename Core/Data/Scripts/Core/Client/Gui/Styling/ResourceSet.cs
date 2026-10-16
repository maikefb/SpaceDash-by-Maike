using System.Collections.Generic;

namespace LcdMod.Client.Gui.Styling
{
    public sealed class ResourceSet
    {
        readonly Dictionary<int, object> _values = new Dictionary<int, object>();

        public ResourceSet Set<TValue>(
            StyleProperty<TValue> property,
            TValue value)
        {
            _values[property.Id] = new LiteralStyleValue<TValue>(value);
            return this;
        }

        public ResourceSet Set<TValue>(
            StyleProperty<TValue> property,
            ResourceKey<TValue> key)
        {
            _values[property.Id] = new ResourceStyleValue<TValue>(key);
            return this;
        }

        public bool TryResolve<TValue>(
            IVisualStyleScope target,
            StyleProperty<TValue> property,
            out TValue value)
        {
            object raw;
            if (_values.TryGetValue(property.Id, out raw))
            {
                var typed = raw as StyleValue<TValue>;
                if (typed != null)
                    return typed.TryResolve(target, out value);
            }

            value = default(TValue);
            return false;
        }
    }
}
