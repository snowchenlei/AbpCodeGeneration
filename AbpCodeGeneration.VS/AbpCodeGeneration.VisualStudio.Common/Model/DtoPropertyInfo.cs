using System;
using System.Collections.Generic;
using System.Text;

namespace AbpCodeGeneration.VisualStudio.Common.Model
{
    public class DtoPropertyInfo
    {
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        public string Local { get; set; }
        public bool IsEdit { get; set; }
        public bool IsList { get; set; }

        public bool Required { get; set; }
    }
}