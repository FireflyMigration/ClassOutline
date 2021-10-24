using System;

namespace ClassOutline
{
    public static class TypeExt
    {
        public static Type FindBaseTypeOrSelf(this Type o, Func<Type, bool> predicate)
        {
            if (predicate(o)) return o;

            if (o.BaseType != null)
            {
                return FindBaseTypeOrSelf(o.BaseType, predicate);
            }
            return null;
        }
    }
}