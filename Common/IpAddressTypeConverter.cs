using System.ComponentModel;
using System.Globalization;
using System.Net;

namespace Common;

public class IpAddressTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            return IPAddress.Parse(stringValue);
        }
        return base.ConvertFrom(context, culture, value);
    }
}