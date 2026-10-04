using System;
using System.Collections.Generic;
using LcdMod.Common.Mvvm;
using VRage.Game.GUI.TextPanel;

namespace LcdMod.Client.Gui.ControlsTemplates.Lists
{
    public delegate void ListBoxItemRenderHandler<T>(
        ListBoxItem<T> control,
        T item,
        List<MySprite> sprites);

    public sealed class ListBoxModel<T> : ControlModelBase
    {
        IList<T> _items;

        public ListBoxModel()
        {
            Items = new List<T>();
            RowHeight = 32f;
            ScrollerWidthPixels = 6f;
        }

        public IList<T> Items
        {
            get { return _items; }
            set
            {
                if (ReferenceEquals(_items, value))
                    return;

                UnbindObservableItems(_items as IObservableList<T>);
                _items = value;
                BindObservableItems(_items as IObservableList<T>);

                RaisePropertyChanged<IList<T>>(nameof(Items));
                RaisePropertyChanged<int>(nameof(Count));
            }
        }
        public float RowHeight { get; set; }
        public float ScrollerWidthPixels { get; set; }
        public Func<T, string> TextSelector { get; set; }
        public Func<T, string> ItemStyleIdSelector { get; set; }
        public Func<T, int, string> ItemClassSelector { get; set; }
        public ListBoxItemRenderHandler<T> ItemRenderer { get; set; }

        public int Count => Items?.Count ?? 0;

        void BindObservableItems(IObservableList<T> items)
        {
            if (items == null)
                return;

            items.ItemAdded += OnItemAdded;
            items.ItemRemoved += OnItemRemoved;
            items.ItemChanged += OnItemChanged;
        }

        void UnbindObservableItems(IObservableList<T> items)
        {
            if (items == null)
                return;

            items.ItemAdded -= OnItemAdded;
            items.ItemRemoved -= OnItemRemoved;
            items.ItemChanged -= OnItemChanged;
        }

        void OnItemAdded(IObservableCollection<T> sender, T item)
        {
            RaiseItemsChanged(true);
        }

        void OnItemRemoved(IObservableCollection<T> sender, T item)
        {
            RaiseItemsChanged(true);
        }

        void OnItemChanged(IObservableCollection<T> sender, T item)
        {
            RaiseItemsChanged(false);
        }

        void RaiseItemsChanged(bool countChanged)
        {
            RaisePropertyChanged<IList<T>>(nameof(Items));
            if (countChanged)
                RaisePropertyChanged<int>(nameof(Count));
        }

        public T GetItem(int index)
        {
            if (Items == null || index < 0 || index >= Items.Count)
                return default(T);

            return Items[index];
        }

        public string GetText(T item)
        {
            if (TextSelector != null)
                return TextSelector(item) ?? string.Empty;

            return item == null ? string.Empty : item.ToString();
        }

        public int IndexOf(T item)
        {
            return Items == null ? -1 : Items.IndexOf(item);
        }
    }
}
