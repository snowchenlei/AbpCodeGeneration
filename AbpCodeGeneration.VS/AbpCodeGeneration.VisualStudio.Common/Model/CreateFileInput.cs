using AbpCodeGeneration.VisualStudio.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AbpCodeGeneration.VisualStudio.Common.Model
{
    public class CreateFileInput
    {
        public string AbsoluteNamespace
        {
            get;set;
            //get
            //{
            //    return Namespace.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Last();
            //}
        }
        public string CamelAbsoluteNamespace
        {
            get
            {
                return AbsoluteNamespace.Substring(0, 1).ToLower() + AbsoluteNamespace.Substring(1);
            }
        }

        public string Namespace { get; set; }
        public string ClassName { get; set; }

        public bool IsModule { get; set; }

        public string ModuleName { get; set; }
        public string CamelClassName
        {
            get
            {
                return ClassName.Substring(0, 1).ToLower() + ClassName.Substring(1);
            }
        }
        public string LocalName { get; set; }
        public string DirectoryName { get; set; }

        /// <summary>
        /// 设置
        /// </summary>
        public Setting Setting { get; set; }

        /// <summary>
        /// 命名空间前缀
        /// </summary>
        public string Prefix { get; set; }

        public string KeyType { get; set; }
        public ICollection<DtoPropertyInfo> PropertyInfos { get; set; }
    }
}