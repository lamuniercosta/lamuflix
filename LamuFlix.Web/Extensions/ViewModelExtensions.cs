using System;
using System.Collections;

namespace LamuFlix.Web.Extensions
{
    public static class ViewModelExtensions
    {
        public static bool HasQuery(this object model)
        {
            var properties = model.GetType().GetProperties();
            object? propertyValue;
            foreach (var property in properties)
            {
                if (!(property.PropertyType.GetInterface(nameof(IEnumerable)) != null) || property.PropertyType == typeof(String))
                {
                    // It's not an array.
                    if (property.CanRead)
                    {
                        propertyValue = property.GetValue(model, null);
                        if (propertyValue != null)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    propertyValue = property.GetValue(model, null);
                    IList? enumerable = propertyValue as IList;
                    if (enumerable?.Count > 0) return true;
                }
            }
            return false;
        }

        public static bool HasPropertyValue(this object model, string propertyName)
        {
            var property = model.GetType().GetProperty(propertyName);
            if (property == null)
            {
                return false;
            }

            var propertyValue = property.GetValue(model, null);
            if (!(property.PropertyType.GetInterface(nameof(IEnumerable)) != null) || property.PropertyType == typeof(String))
            {
                if (propertyValue != null)
                {
                    return true;
                }
            }
            else
            {
                IList? enumerable = propertyValue as IList;
                if (enumerable?.Count > 0) return true;
            }
            return false;
        }
    }
}

