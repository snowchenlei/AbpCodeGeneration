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
        /// 验证方式
        /// </summary>
        public ValidationType ValidationType { get; set; }

        /// <summary>
        /// 控制器
        /// </summary>
        public bool Controller { get; set; }
        /// <summary>
        /// 应用服务
        /// </summary>
        public bool ApplicationService { get; set; }
        /// <summary>
        /// 领域服务
        /// </summary>
        public bool DomainService { get; set; }
        /// <summary>
        /// 授权服务
        /// </summary>
        public bool AuthorizationService { get; set; }
        /// <summary>
        /// Excel导入导出
        /// </summary>
        public bool ExcelImportAndExport { get; set; }

        /// <summary>
        /// 命名空间前缀
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; }
        /// <summary>
        /// 标准项目
        /// </summary>
        public bool IsStandardProject { get; set; }

        public string KeyType { get; set; }
        public ICollection<DtoPropertyInfo> PropertyInfos { get; set; }
    }
}