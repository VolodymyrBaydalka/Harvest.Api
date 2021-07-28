using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class UndocumentedAttribute : Attribute
    {
    }
}
