using System;
using System.Linq;

namespace Toggl.Multivac.Extensions
{
    public static class TypeExtensions
    {
        public static string GetFriendlyName(this Type type)
        {
            if (type.IsGenericType)
            {
                var name = type.Name.Substring(0, type.Name.IndexOf('`'));
                var types = string.Join(",", type.GetGenericArguments().Select(GetFriendlyName));
                return $"{name}<{types}>";
            }
            else
            {
                return type.Name;
            }
        }

        public static bool ImplementsOrDerivesFrom<TBaseType>(this Type type)
            => typeof(TBaseType).IsAssignableFrom(type);

        public static bool ImplementsOrDerivesFrom(this Type type, Type baseType)
            => baseType.IsAssignableFrom(type);
    }
}
