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
        var bindings = BuildBindings(parameters);
        var isVoidReturn = returnType == typeof(void);

        return async context =>
        {
            var arguments = new object?[bindings.Length];
            for (var index = 0; index < bindings.Length; index++)
            {
                arguments[index] = await BindAsync(bindings[index], context).ConfigureAwait(false);
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

    private static BindingPlan[] BuildBindings(ParameterInfo[] parameters)
    {
        var bindings = new BindingPlan[parameters.Length];
        var hasExplicitKey = false;
        var hasExplicitValue = false;

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            if (parameter.ParameterType == typeof(KafkaContext))
            {
                bindings[index] = new BindingPlan(parameter, BindingKind.KafkaContext, null);
                continue;
            }

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                bindings[index] = new BindingPlan(parameter, BindingKind.CancellationToken, null);
                continue;
            }

            if (parameter.IsDefined(typeof(FromKeyAttribute)))
            {
                hasExplicitKey = true;
            }

            if (parameter.IsDefined(typeof(FromValueAttribute)))
            {
                hasExplicitValue = true;
            }
        }

        var implicitKeyAssigned = false;
        var implicitValueAssigned = false;

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            var type = parameter.ParameterType;
            if (type == typeof(KafkaContext))
            {
                continue;
            }

            if (type == typeof(CancellationToken))
            {
                continue;
            }

            var serviceAttribute = parameter.GetCustomAttribute<FromServicesAttribute>();
            var keyAttribute = parameter.GetCustomAttribute<FromKeyAttribute>();
            var valueAttribute = parameter.GetCustomAttribute<FromValueAttribute>();
            var headerAttribute = parameter.GetCustomAttribute<FromHeaderAttribute>();
            var bindingCount = (serviceAttribute is null ? 0 : 1)
                + (keyAttribute is null ? 0 : 1)
                + (valueAttribute is null ? 0 : 1)
                + (headerAttribute is null ? 0 : 1);

            if (bindingCount > 1)
            {
                throw new ArgumentException(
                    $"Handler parameter '{parameter.Name}' must have exactly one binding attribute, except KafkaContext and CancellationToken.");
            }

            if (headerAttribute is not null && string.IsNullOrWhiteSpace(headerAttribute.Name))
            {
                throw new ArgumentException($"Handler parameter '{parameter.Name}' must specify a Kafka header name.");
            }

            if (serviceAttribute is not null)
            {
                bindings[index] = new BindingPlan(parameter, BindingKind.Services, null);
                continue;
            }

            if (keyAttribute is not null)
            {
                bindings[index] = new BindingPlan(parameter, BindingKind.Key, null);
                continue;
            }

            if (valueAttribute is not null)
            {
                bindings[index] = new BindingPlan(parameter, BindingKind.Value, null);
                continue;
            }

            if (headerAttribute is not null)
            {
                bindings[index] = new BindingPlan(parameter, BindingKind.Header, headerAttribute);
                continue;
            }

            if (!hasExplicitKey && !implicitKeyAssigned && string.Equals(parameter.Name, "key", StringComparison.OrdinalIgnoreCase))
            {
                bindings[index] = new BindingPlan(parameter, BindingKind.Key, null);
                implicitKeyAssigned = true;
                continue;
            }

            if (!hasExplicitValue && !implicitValueAssigned && string.Equals(parameter.Name, "value", StringComparison.OrdinalIgnoreCase))
            {
                bindings[index] = new BindingPlan(parameter, BindingKind.Value, null);
                implicitValueAssigned = true;
                continue;
            }

            bindings[index] = new BindingPlan(parameter, BindingKind.Services, null);
        }

        return bindings;
    }

    private static async ValueTask<object?> BindAsync(BindingPlan binding, KafkaContext context)
    {
        var parameter = binding.Parameter;
        var type = parameter.ParameterType;
        return binding.Kind switch
        {
            BindingKind.KafkaContext => context,
            BindingKind.CancellationToken => context.CancellationToken,
            BindingKind.Services => context.RequestServices.GetService(type)
                ?? throw new InvalidOperationException(
                    $"Service '{type.FullName}' required by handler parameter '{parameter.Name}' is not registered."),
            BindingKind.Key => await ConvertKeyAsync(context, type, parameter).ConfigureAwait(false),
            BindingKind.Value => await ConvertValueAsync(context, type, parameter).ConfigureAwait(false),
            BindingKind.Header => ConvertHeader(context, binding.Header!, type, parameter),
            _ => throw new InvalidOperationException($"Unsupported binding kind '{binding.Kind}'."),
        };
    }

    private static object? ConvertHeader(KafkaContext context, FromHeaderAttribute headerAttribute, Type type, ParameterInfo parameter)
    {
        var header = context.Headers.LastOrDefault(item => item.Key == headerAttribute.Name);
        var headerValue = header?.GetValueBytes();
        var text = headerValue is null ? null : Encoding.UTF8.GetString(headerValue);
        return ConvertText(text, type, parameter);
    }

    private enum BindingKind
    {
        KafkaContext,
        CancellationToken,
        Services,
        Key,
        Value,
        Header
    }

    private readonly record struct BindingPlan(ParameterInfo Parameter, BindingKind Kind, FromHeaderAttribute? Header);

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

        var serializer = context.RequestServices.GetRequiredService<IKafkaSerializerRegistry>().Get(context.Format);
        var result = await serializer
            .DeserializeAsync(value, type, context.Topic, context.Headers, context.CancellationToken)
            .ConfigureAwait(false);
        return result
            ?? (IsNullable(type)
                ? null
                : throw new InvalidOperationException($"Kafka value deserialized to null for handler parameter '{parameter.Name}'."));
    }

    private static async Task<object?> ConvertKeyAsync(KafkaContext context, Type type, ParameterInfo parameter)
    {
        var value = context.Key;
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

        var serializer = context.RequestServices.GetRequiredService<IKafkaSerializerRegistry>().Get(context.Format);
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
