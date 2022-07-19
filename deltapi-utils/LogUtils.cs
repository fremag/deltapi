using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using NLog;

namespace deltapi_utils;

public static class LogUtils
{
    public static void ExtInfo(this Logger logger, object value1=null, object value2 = null, object value3 = null, object value4= null, object value5 = null, Exception ex = null, [CallerMemberName] string caller = "")
    {
        if (!logger.IsInfoEnabled)
        {
            return;
        }

        var sb = GetLogString(value1, value2, value3, value4, value5, ex);
        
        logger.Info($"{caller}: {sb}");
    }

    private static StringBuilder GetLogString(object value1, object value2 = null, object value3 = null, object value4= null, object value5 = null, Exception ex = null)
    {
        StringBuilder sb = new();
        Append(value1, sb);
        Append(value2, sb);
        Append(value3, sb);
        Append(value4, sb);
        Append(value5, sb);
        
        if (ex != null)
        {
            sb.Append($"{ex.GetType().Name}[{ex.Message}]");
        }

        return sb;
    }

    private static void Append(object obj, StringBuilder sb, string prefix = null, bool scanProperties=true)
    {
        if (prefix != null)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(prefix).Append('[');
        }

        if (obj == null)
        {
            if (prefix != null) sb.Append(']');
            return;
        }

        var type = obj.GetType();

        if (type.IsEnum)
        {
            sb.Append(Enum.GetName(type, obj));
        }

        else if (type == typeof(string))
        {
            sb.Append(obj);
        }

        else if (type.IsPrimitive)
        {
            sb.Append(obj);
        }
        else if (type == typeof(DateTime))
        {
            sb.Append(((DateTime)obj).ToString("yyyy-MM-dd"));
        }
        else if (type == typeof(TimeSpan))
        {
            sb.Append(obj);
        }
        else if (obj is IEnumerable enumerable)
        {
            foreach(var element in enumerable) 
            {
                Append(element, sb, null, false);
            }
        }
        else if(scanProperties)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                var propValue = property.GetValue(obj);
                var propName = property.Name;
                Append(propValue, sb, propName, false);
            }
        }

        if (prefix != null) sb.Append(']');
    }
}