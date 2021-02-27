using System;
using System.Linq;

namespace ChatAlerts {
    public static class EnumExtension
    {
        public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
        {
            try {
                var type = value.GetType();
                var name = Enum.GetName(type, value);
                return name == null ? null : type.GetField(name).GetCustomAttributes(false).OfType<TAttribute>().SingleOrDefault();
            } catch {
                return null;
            }
        }
    }
}
