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
        public int? MinLength { get; set; }
        public long? MaxLength { get; set; }
        /// <summary>
        /// 正则
        /// </summary>
        public string Regular { get; set; }

        public bool IsValidate {
            get {
                return Required || MinLength.HasValue || MaxLength.HasValue || !String.IsNullOrEmpty(Regular);
            }
        }
    }
}