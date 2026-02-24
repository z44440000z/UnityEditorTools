using System;
using System.Globalization;
using UnityEngine;
[CreateAssetMenu(fileName = "New Build Data", menuName = "Samoi/Build/Build Data")]
public class BuildTimestamp : ScriptableObject
{
    public int UtcYear;
    public int UtcMonth;
    public int UtcDay;
    public int UtcHour;
    public int UtcMinute;
    public int UtcSecond;
    [TextArea(6, 6)] public string message;

    public override string ToString()
    {
        return new DateTime(UtcYear, UtcMonth, UtcDay, UtcHour, UtcMinute, UtcSecond)
            .ToString(CultureInfo.CurrentCulture);
    }

    public string ToString(string format)
    {
        return new DateTime(UtcYear, UtcMonth, UtcDay, UtcHour, UtcMinute, UtcSecond)
            .ToString(format);
    }

    public string ToString(string format, double utcOffsetHours)
    {
        return new DateTime(UtcYear, UtcMonth, UtcDay, UtcHour, UtcMinute, UtcSecond)
            .AddHours(utcOffsetHours)
            .ToString(format);
    }
    public void SetMessage(string text)
    {
        message = text;
    }

}