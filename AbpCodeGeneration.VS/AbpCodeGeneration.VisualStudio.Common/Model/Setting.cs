using AbpCodeGeneration.VisualStudio.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbpCodeGeneration.VisualStudio.Common.Model
{
    public class Setting
    {
        /// <summary>
        /// 首次使用
        /// </summary>
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
        /// 控制器
        /// </summary>
        public bool Controller { get; set; }
        /// <summary>
        /// Excel导入导出
        /// </summary>
        public bool ExcelImportAndExport { get; set; }
        /// <summary>
        /// 图片上传
        /// </summary>
        public bool PictureUpload { get; set; }

        /// <summary>
        /// 标准项目
        /// </summary>
        public bool IsStandardProject { get; set; }
    }
}
