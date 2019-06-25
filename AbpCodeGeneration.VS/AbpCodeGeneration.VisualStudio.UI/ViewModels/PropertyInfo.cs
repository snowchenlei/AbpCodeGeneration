using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbpCodeGeneration.VisualStudio.UI.ViewModels
{
    public class PropertyInfo
    {
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        public bool IsEdit { get; set; }
        public bool IsList { get; set; }
        public string Local { get; set; }
    }
}