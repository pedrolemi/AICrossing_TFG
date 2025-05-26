using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Utilities
{
    public static class EnumExtensions
    {
        // Se utiliza un diccionario concurrente por si se accede desde varios hilos
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Enum, string>> descriptionCache =
            new ConcurrentDictionary<Type, ConcurrentDictionary<Enum, string>>();

        // Se obtiene el atributo decripcion
        private static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field == null)
            {
                return null;
            }
            object[] attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                DescriptionAttribute descriptionAttribute = attributes[0] as DescriptionAttribute;
                return descriptionAttribute.Description;
            }
            return null;
        }

        // Se agrega un metodo de extension al enum para poder obtener los que hay en el atributo description
        // Ademas, esta cacheado para que el rendimiento sea mejor
        public static string GetDescriptionCached(this Enum enumValue)
        {
            Type type = enumValue.GetType();
            ConcurrentDictionary<Enum, string> typeCache = descriptionCache.GetOrAdd(type, new ConcurrentDictionary<Enum, string>());

            return typeCache.GetOrAdd(enumValue, enumValue.GetDescription());
        }
    }
}