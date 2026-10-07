using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalKafka.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromValueAttribute : Attribute
{
}