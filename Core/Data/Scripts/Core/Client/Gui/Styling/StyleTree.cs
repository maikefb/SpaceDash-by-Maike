using System.Collections.Generic;
using LcdMod.Client.Gui.ControlsTemplates;

namespace LcdMod.Client.Gui.Styling
{
    public sealed class StyleTree
    {
        readonly List<StyleNode> _roots = new List<StyleNode>();

        public Style<TControl> For<TControl>()
            where TControl : ControlTemplate
        {
            Style<TControl> root = new Style<TControl>();
            _roots.Add(root);
            return root;
        }

        public bool TryResolve<TValue>(
            ControlTemplate target,
            string id,
            StyleState state,
            StyleProperty<TValue> property,
            out TValue value)
        {
            value = default(TValue);
            bool found = false;
            for (int i = 0; i < _roots.Count; i++)
                found = TryResolve(_roots[i], target, id, state, property, ref value) || found;

            return found;
        }

        static bool TryResolve<TValue>(
            StyleNode node,
            ControlTemplate target,
            string id,
            StyleState state,
            StyleProperty<TValue> property,
            ref TValue value)
        {
            if (node == null ||
                !node.MatchesControl(target) ||
                !node.MatchesSelector(id, state))
                return false;

            TValue current;
            bool found = node.Resources.TryResolve(target, property, out current);
            if (found)
                value = current;

            var children = node.Children;
            for (int i = 0; i < children.Count; i++)
                found = TryResolve(children[i], target, id, state, property, ref value) || found;

            return found;
        }
    }
}
