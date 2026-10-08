using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Attributes;
using MinimalKafka.Serialization;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MinimalKafka.Runtime;

internal static class HandlerAdapter
{
    public static ConsumerDelegate Create(Delegate handler)
    {
        var method = handler.Method;
        var returnType = method.ReturnType;
        if (returnType != typeof(Task) && returnType != typeof(void))
        {
            throw new ArgumentException("A consumer handler must return either Task or void.", nameof(handler));
        }
        if (handler.GetInvocationList().Length != 1)
        {
            throw new ArgumentException("A consumer handler must be a single-cast delegate.", nameof(handler));
        }

        var parameters = method.GetParameters();
        foreach (var parameter in parameters)
        {
            ValidateBinding(parameter);
        }

        var isVoidReturn = returnType == typeof(void);

        return async context =>
        {
            var arguments = new object?[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                arguments[index] = await BindAsync(parameters[index], context).ConfigureAwait(false);
            }

            var result = handler.DynamicInvoke(arguments);

            if (isVoidReturn)
            {
                // Void handlers are automatically wrapped in a completed Task
                return;
            }

            if (result is not Task task)
            {
                throw new InvalidOperationException($"Consumer handler '{method.Name}' did not return a Task.");
            }

            await task.ConfigureAwait(false);
        };
    }

    private static async ValueTask<object?> BindAsync(ParameterInfo parameter, KafkaContext context)
    {
        var type = parameter.ParameterType;
        if (type == typeof(KafkaContext))
        {
            return context;
        }

        if (type == typeof(CancellationToken))
        {
            return context.CancellationToken;
        }

        var serviceAttribute = parameter.GetCustomAttribute<FromServicesAttribute>();
        var keyAttribute = parameter.GetCustomAttribute<FromKeyAttribute>();
        var valueAttribute = parameter.GetCustomAttribute<FromValueAttribute>();
        var headerAttribute = parameter.GetCustomAttribute<FromHeaderAttribute>();
        var bindingCount = (serviceAttribute is null ? 0 : 1)
            + (keyAttribute is null ? 0 : 1)
            + (valueAttribute is null ? 0 : 1)
            + (headerAttribute is null ? 0 : 1);
        if (bindingCount != 1)
        {
            throw new InvalidOperationException(
                $"Handler parameter '{parameter.Name}' must have exactly one binding attribute, except KafkaContext and CancellationToken.");
        }

        if (serviceAttribute is not null)
        {
            return context.RequestServices.GetService(type)
                ?? throw new InvalidOperationException(
                    $"Service '{type.FullName}' required by handler parameter '{parameter.Name}' is not registered.");
        }

        if (keyAttribute is not null)
        {
            return ConvertText(context.Key, type, parameter);
        }

        if (valueAttribute is not null)
        {
            return await ConvertValueAsync(context, type, parameter).ConfigureAwait(false);
        }

        var header = context.Headers.LastOrDefault(item => item.Key == headerAttribute!.Name);
        var headerValue = header?.GetValueBytes();
        var text = headerValue is null ? null : Encoding.UTF8.GetString(headerValue);
        return ConvertText(text, type, parameter);
    }

    private static void ValidateBinding(ParameterInfo parameter)
    {
        var type = parameter.ParameterType;
        if (type == typeof(KafkaContext) || type == typeof(CancellationToken))
        {
            return;
        }

        var services = parameter.IsDefined(typeof(FromServicesAttribute));
        var key = parameter.IsDefined(typeof(FromKeyAttribute));
        var value = parameter.IsDefined(typeof(FromValueAttribute));
        var header = parameter.GetCustomAttribute<FromHeaderAttribute>();
        var bindingCount = (services ? 1 : 0) + (key ? 1 : 0) + (value ? 1 : 0) + (header is null ? 0 : 1);
        if (bindingCount != 1)
        {
            throw new ArgumentException(
                $"Handler parameter '{parameter.Name}' must have exactly one binding attribute, except KafkaContext and CancellationToken.");
        }
        if (header is not null && string.IsNullOrWhiteSpace(header.Name))
        {
            throw new ArgumentException($"Handler parameter '{parameter.Name}' must specify a Kafka header name.");
        }
    }

    private static async Task<object?> ConvertValueAsync(KafkaContext context, Type type, ParameterInfo parameter)
    {
        var value = context.Value;
        if (value is null)
        {
            if (IsNullable(type))
            {
                return null;
            }
            throw new InvalidOperationException($"Kafka value is null for required handler parameter '{parameter.Name}'.");
        }

        if (type == typeof(byte[]))
        {
            return value;
        }
        if (type == typeof(string))
        {
            return Encoding.UTF8.GetString(value);
        }

        var serializer = context.RequestServices.GetRequiredService<IMessageSerializerRegistry>().Get(context.Format);
        var result = await serializer
            .DeserializeAsync(value, type, context.Topic, context.Headers, context.CancellationToken)
            .ConfigureAwait(false);
        return result
            ?? (IsNullable(type)
                ? null
                : throw new InvalidOperationException($"Kafka value deserialized to null for handler parameter '{parameter.Name}'."));
    }

    private static object? ConvertText(string? text, Type type, ParameterInfo parameter)
    {
        if (text is null)
        {
            if (IsNullable(type))
            {
                return null;
            }
            throw new InvalidOperationException($"Kafka data is missing for required handler parameter '{parameter.Name}'.");
        }
        var targetType = Nullable.GetUnderlyingType(type) ?? type;
        if (targetType == typeof(string))
        {
            return text;
        }
        if (targetType == typeof(Guid))
        {
            return Guid.Parse(text);
        }
        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, text, ignoreCase: true);
        }
        return Convert.ChangeType(text, targetType, CultureInfo.InvariantCulture);
    }

    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
