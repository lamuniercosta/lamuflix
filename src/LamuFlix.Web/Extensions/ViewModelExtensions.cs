using System.Collections;

namespace LamuFlix.Web.Extensions;

public static class ViewModelExtensions
{
    extension(object model)
    {
        public bool HasQuery()
        {
            var properties = model.GetType().GetProperties();
            foreach (var property in properties)
            {
                object? propertyValue;
                if (!(property.PropertyType.GetInterface(nameof(IEnumerable)) != null) || property.PropertyType == typeof(string))
                {
                    // It's not an array.
                    if (!property.CanRead) continue;
                    propertyValue = property.GetValue(model, null);
                    if (propertyValue != null)
                    {
                        return true;
                    }
                }
                else
                {
                    propertyValue = property.GetValue(model, null);
                    var enumerable = propertyValue as IList;
                    if (enumerable?.Count > 0) return true;
                }
            }
            return false;
        }

        public bool HasPropertyValue(string propertyName)
        {
            var property = model.GetType().GetProperty(propertyName);
            if (property == null)
            {
                return false;
            }

            var propertyValue = property.GetValue(model, null);
            if (!(property.PropertyType.GetInterface(nameof(IEnumerable)) != null) || property.PropertyType == typeof(string))
            {
                if (propertyValue != null)
                {
                    return true;
                }
            }
            else
            {
                var enumerable = propertyValue as IList;
                if (enumerable?.Count > 0) return true;
            }
            return false;
        }
    }
}