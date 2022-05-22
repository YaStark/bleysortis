using System;

namespace Bleysortis.Main
{
    public static class Utils
    {
        public static void DoIfNotNull<T>(this Nullable<T> item, Action<T> action)
            where T : struct
        {
            if (item.HasValue)
            {
                action(item.Value);
            }
        }

        public static void DoIfNotNull<T>(this T item, Action<T> action)
            where T : class
        {
            if (item != null)
            {
                action(item);
            }
        }
    }
}
