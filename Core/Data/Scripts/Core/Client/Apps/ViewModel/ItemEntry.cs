using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Common.Mvvm;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace LcdMod.Client.Apps.ViewModel
{
    public sealed partial class ItemEntry : ControlModelBase
    {
         MyFixedPoint _amount;
         double _craftAmount = 1d;
         string _icon;
         string _displayName;
         string _amountText;
         string _primaryAmountText;
         string _secondaryAmountText;
         ItemAmountDisplayMode _amountDisplayMode;
         ItemAvailabilityStatus _availabilityStatus;
         Color _listTextColor;
         Color _listAmountColor;
         Color _listIconColor;
         Color _gridTextColor;
         Color _gridAmountColor;
         Color _gridIconColor;

        public ItemEntry(MyItemType itemType, MyFixedPoint amount)
        {
            ItemType = itemType;
            _amount = amount;
        }

        public MyItemType ItemType { get; private set; }

        public string TypeId => ItemType.TypeId;

        public void SetSimpleAmount(string amountText)
        {
            AmountDisplayMode = ItemAmountDisplayMode.Simple;
            AmountText = amountText;
            PrimaryAmountText = amountText;
            SecondaryAmountText = null;
        }

        public void SetQuotaAmount(string hasText, string needText)
        {
            AmountDisplayMode = ItemAmountDisplayMode.Quota;
            PrimaryAmountText = hasText;
            SecondaryAmountText = needText;
            AmountText = hasText + "/"+ needText;
        }
    }

    public enum ItemAmountDisplayMode
    {
        Simple,
        Quota
    }

    public enum ItemAvailabilityStatus
    {
        Normal,
        Warning,
        Error
    }
}
