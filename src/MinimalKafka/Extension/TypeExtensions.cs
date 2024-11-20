using System.Text;

namespace MinimalKafka.Extension;

public static class TypeExtensions
{
    public static string FQN(this Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        
        var sb = new StringBuilder();
        
        if (type.IsNested)
        {
            sb.Append(type.DeclaringType!.FQN());
            sb.Append('.');
        }
        else
        {
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                sb.Append(type.Namespace);
                sb.Append('.');
            }
        }

        sb.Append(type.Name);
        
        if (type.IsGenericType)
        {
            var genericArguments = type.GetGenericArguments();
            var genericTypeName = type.Name.Substring(0, type.Name.IndexOf('`'));
            sb.Append(genericTypeName);
            sb.Append('<');
            for (int i = 0; i < genericArguments.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(genericArguments[i].FQN());
            }
            sb.Append('>');
        }

        return sb.ToString();
    }
}