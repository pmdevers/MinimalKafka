using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka.Factory;

internal sealed class ParameterBindingMethodCache
{
    internal static readonly ParameterExpression KafkaContextExpr = Expression.Parameter(typeof(KafkaContext), "kafkaContext");

    public ParameterBindingMethodCache()
    {
    }
}
