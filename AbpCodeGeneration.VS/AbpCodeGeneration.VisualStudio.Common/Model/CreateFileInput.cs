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

        public bool FirstUse { get; set; }
        /// <summary>
        /// 验证方式
        /// </summary>
        public int ValidationType { get; set; }
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
        /// TODO:授权模式——追加、新建
        /// </summary>
        public bool IsAppend { get; set; } = true;
        /// <summary>
        /// Excel导入导出
        /// </summary>
        public bool ExcelImportAndExport { get; set; }
        /// <summary>
        /// 图片上传
        /// </summary>
        public bool PictureUpload { get; set; }


        public string KeyType { get; set; }
        public ICollection<DtoPropertyInfo> PropertyInfos { get; set; }
    }
}