using System;
using System.Collections.Generic;
using System.Text;

namespace AbpCodeGeneration.VisualStudio.Common.Model
{
    public class DtoFileModel
    {
        public string Namespace { get; set; }

        public string Name { get; set; }

        public string CnName { get; set; }

        public string Description { get; set; }

        public string DirName { get; set; }

        public List<ClassProperty> ClassPropertys { get; set; }
    }

    /// <summary>
    /// 属性
    /// </summary>
    public class ClassProperty
    {
        /// <summary>
        /// 属性类型
        /// </summary>
        public string PropertyType { get; set; }

        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 属性中文名称
        /// </summary>
        public string CnName { get; set; }

        /// <summary>
        /// 属性特性
        /// </summary>
        public List<ClassAttribute> ClassAttributes { get; set; }
    }

    /// <summary>
    /// 属性特性
    /// </summary>
    public class ClassAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string NameValue { get; set; }
    }
}