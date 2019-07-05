using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbpCodeGeneration.VisualStudio.UI.Enums
{
    public enum ValidationType
    {
        [Description("FluentApi")]
        FluentApi,
        [Description("数据注解")]
        DataAnnotation
    }
}
