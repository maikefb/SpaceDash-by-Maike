using LcdMod.Client.Helpers;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.ClockDashboard
{
    internal static class ClockDashboardLocalization
    {
        const string PREFIX = MOD_PREFIX + "ClockDashboard_";

        public const string TITLE_KEY = MOD_PREFIX + "InGameClockDashboard";
        public const string CONTROL_24_HOUR_TITLE_KEY = PREFIX + "Control_24Hour";
        public const string CONTROL_TEMPERATURE_TITLE_KEY = PREFIX + "Control_Temperature";
        public const string CONTROL_TEMPERATURE_TOOLTIP_KEY = PREFIX + "Control_Temperature_Tooltip";
        public const string TEMPERATURE_FUZZY_KEY = PREFIX + "Temperature_Fuzzy";
        public const string TEMPERATURE_CELSIUS_KEY = PREFIX + "Temperature_Celsius";
        public const string TEMPERATURE_KELVIN_KEY = PREFIX + "Temperature_Kelvin";
        public const string TEMPERATURE_FAHRENHEIT_KEY = PREFIX + "Temperature_Fahrenheit";

        public static string Unavailable => LocHelper.GetLoc(MOD_PREFIX + "Common_Value_Unavailable");
        public static string DeepSpace => LocHelper.GetLoc(PREFIX + "DeepSpace");
        public static string UnknownPlanet => LocHelper.GetLoc(PREFIX + "UnknownPlanet");
        public static string ClearWeather => LocHelper.GetLoc(PREFIX + "Weather_Clear");
        public static string UnknownWeather => LocHelper.GetLoc(PREFIX + "Weather_Unknown");
        public static string PlayTime => LocHelper.GetLoc(PREFIX + "PlayTime");
        public static string ServerUptime => LocHelper.GetLoc(PREFIX + "ServerUptime");

        public static string GetDayMoment(DayMoment moment)
        {
            switch (moment)
            {
                case DayMoment.Night:
                    return LocHelper.GetLoc(PREFIX + "DayMoment_Night");
                case DayMoment.Dawn:
                    return LocHelper.GetLoc(PREFIX + "DayMoment_Dawn");
                case DayMoment.Morning:
                    return LocHelper.GetLoc(PREFIX + "DayMoment_Morning");
                case DayMoment.Noon:
                    return LocHelper.GetLoc(PREFIX + "DayMoment_Noon");
                case DayMoment.Afternoon:
                    return LocHelper.GetLoc(PREFIX + "DayMoment_Afternoon");
                case DayMoment.Dusk:
                    return LocHelper.GetLoc(PREFIX + "DayMoment_Dusk");
                case DayMoment.NoLocalDayCycle:
                    return LocHelper.GetLoc(PREFIX + "DayMoment_NoLocalDay");
                case DayMoment.PolarDay:
                    return LocHelper.GetLoc(PREFIX + "DayMoment_PolarDay");
                default:
                    return LocHelper.GetLoc(PREFIX + "DayMoment_Unknown");
            }
        }
    }
}
