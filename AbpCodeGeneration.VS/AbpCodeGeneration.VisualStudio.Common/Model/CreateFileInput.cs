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
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string CamelClassName
        {
            get
            {
                return ClassName.Substring(0, 1).ToLower() + ClassName.Substring(1);
            }
        }
        public string LocalName { get; set; }
        public string DirectoryName { get; set; }

        public bool IsFirst { get; set; } = true;
        public bool ExistDomainService { get; set; } = true;
        public bool ExistAuthorization { get; set; } = true; 
        public bool ExistValidation { get; set; } = true;


        public string KeyType { get; set; }
        public ICollection<DtoPropertyInfo> PropertyInfos { get; set; }
    }
}