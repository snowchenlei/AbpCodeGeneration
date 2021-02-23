using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbpCodeGeneration.VisualStudio.Common.Enums
{
    public enum ValidationType
    {
        //[Description("没有")]
        //Normal,
        [Description("FluentApi")]
        FluentApi = 1,
        //[Description("数据注解")]
        //DataAnnotation
    }
}
