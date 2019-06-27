
using AbpCodeGeneration.VisualStudio.Common.Model;
using AbpCodeGeneration.VisualStudio.Common.Templates;
using EnvDTE;
using EnvDTE80;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AbpCodeGeneration.VisualStudio.Common
{
    public class ProjectHelper
    {
        private CodeClass2 CodeClass;
        private Solution Solution;
        private string ClassName;
        private string ClassNameLocal;
        private string ClassNamespace;

        private string ClassAbsolutePathInProject;
        private string ClassPathInProject;
        private string ApplicationRootNamespace;
        private ProjectItem SelectProjectItem;
        private List<ProjectItem> SolutionProjectItems;

        public ProjectHelper(DTE2 dte)
        {
            InitBase(dte);
        }

        private void InitBase(DTE2 dte)
        {
            SelectedItem selectedItem = dte.SelectedItems.Item(1);
            SelectProjectItem = selectedItem.ProjectItem;
            Solution = dte.Solution;
            SolutionProjectItems = GetSolutionProjects(Solution);            
            //获取当前点击的类所在的项目
            Project topProject = SelectProjectItem.ContainingProject;
            //当前类在当前项目中的目录结构
            ClassAbsolutePathInProject = GetSelectFileDirPath(topProject, SelectProjectItem);

            //当前类命名空间
            string namespaceStr = SelectProjectItem.FileCodeModel.CodeElements.OfType<CodeNamespace>().First().FullName;
            //当前项目根命名空间
            if (!string.IsNullOrEmpty(namespaceStr))
            {
                ApplicationRootNamespace = topProject.Name.Substring(0, topProject.Name.LastIndexOf('.'));
            }
            //当前类
            CodeClass = GetClass(SelectProjectItem.FileCodeModel.CodeElements);
        }

        /// <summary>
        /// 获取DtoModel
        /// </summary>
        /// <param name="applicationStr"></param>
        /// <param name="name"></param>
        /// <param name="dirName"></param>
        /// <param name="codeClass"></param>
        /// <returns></returns>
        public DtoFileModel GetDtoModel()
        {
            var model = new DtoFileModel() { Namespace = ApplicationRootNamespace, Name = CodeClass.Name, DirName = ClassAbsolutePathInProject.Replace("\\", ".") };
            List<ClassProperty> classProperties = new List<ClassProperty>();

            if (CodeClass.Bases.Count > 0)
            {
                //C#仅支持单继承
                CodeElements baseMembers = CodeClass.Bases.Cast<CodeClass2>().ToList()[0].Members;
                AddClassProperty(baseMembers, ref classProperties);
            }
            var codeMembers = CodeClass.Members;
            AddClassProperty(codeMembers, ref classProperties);
            model.ClassPropertys = classProperties;

            return model;
        }

        private void AddClassProperty(CodeElements codeElements, ref List<ClassProperty> classProperties)
        {
            foreach (CodeElement2 codeElement in codeElements)
            {
                if (codeElement.Kind == vsCMElement.vsCMElementProperty)
                {
                    ClassProperty classProperty = new ClassProperty();
                    CodeProperty2 property = codeElement as CodeProperty2;
                    classProperty.Name = property.Name;
                    //获取属性类型
                    var propertyType = property.Type;
                    switch (propertyType.TypeKind)
                    {
                        case vsCMTypeRef.vsCMTypeRefString:
                            classProperty.PropertyType = "string";
                            break;

                        case vsCMTypeRef.vsCMTypeRefInt:
                            classProperty.PropertyType = "int";
                            break;

                        case vsCMTypeRef.vsCMTypeRefLong:
                            classProperty.PropertyType = "long";
                            break;

                        case vsCMTypeRef.vsCMTypeRefByte:
                            classProperty.PropertyType = "byte";
                            break;

                        case vsCMTypeRef.vsCMTypeRefChar:
                            classProperty.PropertyType = "char";
                            break;

                        case vsCMTypeRef.vsCMTypeRefShort:
                            classProperty.PropertyType = "short";
                            break;

                        case vsCMTypeRef.vsCMTypeRefBool:
                            classProperty.PropertyType = "bool";
                            break;

                        case vsCMTypeRef.vsCMTypeRefDecimal:
                            classProperty.PropertyType = "decimal";
                            break;

                        case vsCMTypeRef.vsCMTypeRefFloat:
                            classProperty.PropertyType = "float";
                            break;

                        case vsCMTypeRef.vsCMTypeRefDouble:
                            classProperty.PropertyType = "double";
                            break;

                        default:
                            classProperty.PropertyType = propertyType.AsFullName;
                            break;
                    }

                    classProperties.Add(classProperty);
                }
            }
        }

        public void CreateFile(CreateFileInput model)
        {
            InitRazorEngine();
            //获取当前点击的类所在的项目
            Project topProject = SelectProjectItem.ContainingProject;
            //当前类在当前项目中的目录结构
            string dirPath = ClassPathInProject = GetSelectFileDirPath(topProject, SelectProjectItem);           

            ProjectItem applicationProjectItem = SolutionProjectItems.Find(t => t.Name == ApplicationRootNamespace + ".Application");
            //首次使用
            if (model.IsFirst)
            {
                //首次初始化，添加通用Dto
                var applicationBasicFolder = applicationProjectItem.SubProject.ProjectItems.Item("Dto") ?? applicationProjectItem.SubProject.ProjectItems.AddFolder("Dto");
                CreateBasicDto(model, applicationBasicFolder);
                //编辑AppConst
                SetConst(applicationProjectItem.SubProject);
                //创建验证模板
                CreateValidationBase(model, applicationProjectItem);
            }

            //添加项目目录结构
            var applicationNewFolder = GetDeepProjectItem(applicationProjectItem) ?? applicationProjectItem.SubProject.ProjectItems.AddFolder(dirPath);
            //添加Dto
            var applicationDtoFolder = applicationNewFolder.ProjectItems.Item("Dto") ?? applicationNewFolder.ProjectItems.AddFolder("Dto");
            CreateDtoFile(model, applicationDtoFolder);
            //设置Autompper映射
            ProjectItem customDtoMapperProjectItem = applicationProjectItem.SubProject.ProjectItems.Item("CustomDtoMapper.cs");
            if (customDtoMapperProjectItem == null)
            {   //没有则创建文件
                CreateCustomDtoMapper(model, applicationProjectItem);
            }
            SetMapper(applicationProjectItem.SubProject, $"{model.Namespace}.{model.DirectoryName}", model.ClassName, model.LocalName);
            //添加Validator
            if (model.ExistValidation)
            {
                var applicationValidatorFolder = applicationNewFolder.ProjectItems.Item("Validators") ?? applicationNewFolder.ProjectItems.AddFolder("Validators");
                CreateValidatorFile(model, applicationValidatorFolder);
            }
            ProjectItem currentProjectItem = GetDeepProjectItem(topProject);
            //添加权限
            if (model.ExistAuthorization)
            {
                var coreAuthorizationFolder = currentProjectItem.ProjectItems.Item("Authorization")
                    ?? currentProjectItem.ProjectItems.AddFolder("Authorization");
                CreateAuthorizationFile(model, coreAuthorizationFolder);
            }
            if (model.ExistDomainService)
            {
                var coreDomainServiceFolder = currentProjectItem.ProjectItems.Item("DomainService")
                    ?? currentProjectItem.ProjectItems.AddFolder("DomainService");
                CreateDomainServiceFile(model, coreDomainServiceFolder);
            }
            //添加服务
            CreateServiceFile(model, applicationNewFolder);
            
        }
        #region 获取项目信息
        private ProjectItem GetDeepProjectItem(Project project)
        {
            string[] classAbsolutePaths = ClassAbsolutePathInProject.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            ProjectItem projectItem = project.ProjectItems.Item(classAbsolutePaths[0]);
            for (int i = 1; i < classAbsolutePaths.Length; i++)
            {
                if (projectItem == null)
                {
                    return null;
                }
                projectItem = projectItem.ProjectItems.Item(classAbsolutePaths[i]);
            }
            return projectItem;
        }
        private ProjectItem GetDeepProjectItem(ProjectItem project)
        {
            string[] classAbsolutePaths = ClassAbsolutePathInProject.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            ProjectItem projectItem = project;
            foreach (string item in classAbsolutePaths)
            {
                if (projectItem == null)
                { return null; }
                if (projectItem.ProjectItems == null)
                {
                    projectItem = projectItem.SubProject.ProjectItems.Item(item);
                }
                else
                {
                    projectItem = projectItem.ProjectItems.Item(item);
                }
            }
            return projectItem;
        } 
        #endregion
        private void InitRazorEngine()
        {
            var config = new TemplateServiceConfiguration
            {
                TemplateManager = new EmbeddedResourceTemplateManager(typeof(Template))
            };
            RazorEngine.Engine.Razor = RazorEngineService.Create(config);
        }

        /// <summary>
        /// 获取类
        /// </summary>
        /// <param name="codeElements"></param>
        /// <returns></returns>
        private CodeClass2 GetClass(CodeElements codeElements)
        {
            List<CodeElement2> elements = codeElements.Cast<CodeElement2>().ToList();
            CodeClass2 result = elements.FirstOrDefault(codeElement => codeElement.Kind == vsCMElement.vsCMElementClass) as CodeClass2;

            if (result != null)
            {
                return result;
            }
            foreach (CodeElement2 codeElement in elements)
            {
                result = GetClass(codeElement.Children);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
        private CodeImport GetImport(CodeElements codeElements)
        {
            List<CodeElement2> elements = codeElements.Cast<CodeElement2>().ToList();
            CodeImport imp = elements.FirstOrDefault(codeElement => codeElement.Kind == vsCMElement.vsCMElementIDLImport) as CodeImport;
            return imp;
        }

        /// <summary>
        /// 获取解决方案里面所有项目
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        private List<ProjectItem> GetSolutionProjects(Solution solution)
        {
            List<ProjectItem> projectItemList = new List<ProjectItem>();
            var projects = solution.Projects.OfType<Project>();
            foreach (var project in projects)
            {
                var projectitems = GetProjects(project.ProjectItems);

                foreach (var projectItem in projectitems)
                {
                    projectItemList.Add(projectItem);
                }
            }

            return projectItemList;
        }

        /// <summary>
        /// 获取所有项目
        /// </summary>
        /// <param name="projectItems"></param>
        /// <returns></returns>
        private IEnumerable<ProjectItem> GetProjects(ProjectItems projectItems)
        {
            if (projectItems != null)
            {
                foreach (ProjectItem item in projectItems)
                {
                    yield return item;

                    if (item.SubProject != null)
                    {
                        foreach (ProjectItem childItem in GetProjects(item.SubProject.ProjectItems))
                            if (childItem.Kind == Constants.vsProjectItemKindSolutionItems)
                                yield return childItem;
                    }
                    else
                    {
                        foreach (ProjectItem childItem in GetProjects(item.ProjectItems))
                            if (childItem.Kind == Constants.vsProjectItemKindSolutionItems)
                                yield return childItem;
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前所选文件去除项目目录后的文件夹结构
        /// </summary>
        /// <param name="selectProjectItem"></param>
        /// <returns></returns>
        private string GetSelectFileDirPath(Project topProject, ProjectItem selectProjectItem)
        {
            string dirPath = "";
            if (selectProjectItem != null)
            {
                //所选文件对应的路径
                string fileNames = selectProjectItem.FileNames[0];
                string selectedFullName = fileNames.Substring(0, fileNames.LastIndexOf('\\'));

                //所选文件所在的项目
                if (topProject != null)
                {
                    //项目目录
                    string projectFullName = topProject.FullName.Substring(0, topProject.FullName.LastIndexOf('\\'));

                    //当前所选文件去除项目目录后的文件夹结构
                    dirPath = selectedFullName.Replace(projectFullName, "");
                }
            }

            return dirPath.Substring(1);
        }

        #region 创建文件
        /// <summary>
        /// 创建授权文件
        /// </summary>
        /// <param name="model"></param>
        /// <param name="coreFolder"></param>
        private void CreateAuthorizationFile(CreateFileInput model, ProjectItem coreFolder)
        {
            string contentAuthorizationProvider = RazorEngine.Engine.Razor.RunCompile("AppAuthorizationProviderTemplate", typeof(CreateFileInput), model);
            string fileNameAuthorizationProvider = model.ClassName + "AuthorizationProvider.cs";
            AddFileToProjectItem(coreFolder, contentAuthorizationProvider, fileNameAuthorizationProvider);

            string contentPermissionName = RazorEngine.Engine.Razor.RunCompile("AppPermissionName", typeof(CreateFileInput), model);
            string fileNamePermissionName = model.ClassName + "PermissionName.cs";
            AddFileToProjectItem(coreFolder, contentPermissionName, fileNamePermissionName);
        }
        /// <summary>
        /// 创建领域服务文件
        /// </summary>
        /// <param name="model"></param>
        /// <param name="coreFolder"></param>
        private void CreateDomainServiceFile(CreateFileInput model, ProjectItem coreFolder)
        {
            string contentAuthorizationProvider = RazorEngine.Engine.Razor.RunCompile("IDomainServiceTemplate", typeof(CreateFileInput), model);
            string fileNameAuthorizationProvider = $"I{model.ClassName}Manager.cs";
            AddFileToProjectItem(coreFolder, contentAuthorizationProvider, fileNameAuthorizationProvider);

            string contentPermissionName = RazorEngine.Engine.Razor.RunCompile("DomainServiceTemplate", typeof(CreateFileInput), model);
            string fileNamePermissionName = model.ClassName + "Manager.cs";
            AddFileToProjectItem(coreFolder, contentPermissionName, fileNamePermissionName);
        }
        
        /// <summary>
        /// 创建基础Dto
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateBasicDto(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_PagedAndSorted = RazorEngine.Engine.Razor.RunCompile("PagedAndSortedTemplate", typeof(CreateFileInput), model);
            string fileName_PagedAndSorted = $"PagedAndSortedInputDto.cs";
            AddFileToProjectItem(dtoFolder, content_PagedAndSorted, fileName_PagedAndSorted);

            string content_Paged = RazorEngine.Engine.Razor.RunCompile("PagedInputTemplate", typeof(CreateFileInput), model);
            string fileName_Paged = $"PagedInputDto.cs";
            AddFileToProjectItem(dtoFolder, content_Paged, fileName_Paged);
        }
        /// <summary>
        /// 创建Dto文件
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateDtoFile(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_Edit = RazorEngine.Engine.Razor.RunCompile("EditDtoTemplate", typeof(CreateFileInput), model);
            string fileName_Edit = $"{model.ClassName}EditDto.cs";
            AddFileToProjectItem(dtoFolder, content_Edit, fileName_Edit);

            string content_List = RazorEngine.Engine.Razor.RunCompile("ListDtoTemplate", typeof(DtoFileModel), model);
            string fileName_List = $"{model.ClassName}ListDto.cs";
            AddFileToProjectItem(dtoFolder, content_List, fileName_List);

            string content_CreateAndUpdate = RazorEngine.Engine.Razor.RunCompile("CreateOrUpdateInputDtoTemplate", typeof(DtoFileModel), model);
            string fileName_CreateAndUpdate = $"CreateOrUpdate{model.ClassName}Input.cs";
            AddFileToProjectItem(dtoFolder, content_CreateAndUpdate, fileName_CreateAndUpdate);

            string content_GetForUpdate = RazorEngine.Engine.Razor.RunCompile("GetForEditOutputDtoTemplate", typeof(DtoFileModel), model);
            string fileName_GetForUpdate = $"Get{model.ClassName}ForEditOutput.cs";
            AddFileToProjectItem(dtoFolder, content_GetForUpdate, fileName_GetForUpdate);

            string content_GetsInput = RazorEngine.Engine.Razor.RunCompile("GetsInputTemplate", typeof(DtoFileModel), model);
            string fileName_GetsInput = $"Get{model.ClassName}sInput.cs";
            AddFileToProjectItem(dtoFolder, content_GetsInput, fileName_GetsInput);
        }
        /// <summary>
        /// 创建验证文件
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateValidatorFile(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_Edit = RazorEngine.Engine.Razor.RunCompile("ValidationTemplate", typeof(CreateFileInput), model);
            string fileName_Edit = $"{model.ClassName}EditValidator.cs";
            AddFileToProjectItem(dtoFolder, content_Edit, fileName_Edit);
        }
        /// <summary>
        /// 创建Service类
        /// </summary>
        /// <param name="applicationStr">根命名空间</param>
        /// <param name="name">类名</param>
        /// <param name="dtoFolder">父文件夹</param>
        /// <param name="dirName">类所在文件夹目录</param>
        private void CreateServiceFile(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_IService = RazorEngine.Engine.Razor.RunCompile("IServiceTemplate", typeof(CreateFileInput), model);
            string fileName_IService = $"I{model.ClassName}AppService.cs";
            AddFileToProjectItem(dtoFolder, content_IService, fileName_IService);

            string content_Service = RazorEngine.Engine.Razor.RunCompile("ServiceTemplate", typeof(CreateFileInput), model);
            string fileName_Service = $"{model.ClassName}AppService.cs";
            AddFileToProjectItem(dtoFolder, content_Service, fileName_Service);
        } 
        /// <summary>
        /// 创建自定义Automapper映射
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateCustomDtoMapper(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_Edit = RazorEngine.Engine.Razor.RunCompile("CustomDtoTemplate", typeof(CreateFileInput), model);
            string fileName_Edit = model.AbsoluteNamespace + "DtoMapper.cs";
            AddFileToProjectItem(dtoFolder, content_Edit, fileName_Edit);
        }
        /// <summary>
        /// 创建验证基类
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateValidationBase(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_Edit = RazorEngine.Engine.Razor.RunCompile("ValidationBaseTemplate", typeof(CreateFileInput), model);
            string fileName_Edit = model.AbsoluteNamespace + "Validator.cs";
            AddFileToProjectItem(dtoFolder, content_Edit, fileName_Edit);
        }
        #endregion
        /// <summary>
        /// 编辑AppConst文件
        /// </summary>
        /// <param name="applicationProject"></param>
        private void SetConst(Project applicationProject)
        {
            ProjectItem customAppConstProjectItem = applicationProject.ProjectItems.Item("AppConsts.cs");
            if(customAppConstProjectItem != null)
            {
                CodeClass codeClass = GetClass(customAppConstProjectItem.FileCodeModel.CodeElements);
                var insertCode = codeClass.GetEndPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                insertCode.Insert("            public const int MaxPageSize = 1000;\r\n");
                insertCode.Insert("            public const int DefaultPageSize = 10;\r\n");
                customAppConstProjectItem.Save();
            }
        }
        /// <summary>
        /// 编辑CustomDtoMapper.cs,添加映射
        /// </summary>
        /// <param name="applicationProject"></param>
        /// <param name="className"></param>
        /// <param name="classCnName"></param>
        private void SetMapper(Project applicationProject, string nameSpace, string className, string classCnName)
        {
            ProjectItem customDtoMapperProjectItem = applicationProject.ProjectItems.Item("CustomDtoMapper.cs");            
            if (customDtoMapperProjectItem != null)
            {
                CodeClass codeClass = GetClass(customDtoMapperProjectItem.FileCodeModel.CodeElements);
                var insertUsingCode = codeClass.StartPoint.CreateEditPoint();//codeClass.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                insertUsingCode.MoveToLineAndOffset(1, 1);
                insertUsingCode.Insert($"using {nameSpace};\r\n");
                insertUsingCode.Insert($"using {nameSpace}.Dto;\r\n");

                var codeChilds = codeClass.Members;
                foreach (CodeElement codeChild in codeChilds)
                {
                    if (codeChild.Kind == vsCMElement.vsCMElementFunction && codeChild.Name == "CreateMappings")
                    {
                        var insertCode = codeChild.GetEndPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                        insertCode.Insert("            // " + classCnName ?? className + "\r\n");
                        insertCode.Insert("            configuration.CreateMap<" + className + ", " + className + "EditDto>();\r\n");
                        insertCode.Insert("            configuration.CreateMap<" + className + ", " + className + "ListDto>();\r\n");
                        insertCode.Insert("            configuration.CreateMap<" + className + "EditDto, " + className + ">();\r\n");
                        insertCode.Insert("            configuration.CreateMap<" + className + "ListDto, " + className + ">();\r\n");
                        insertCode.Insert("\r\n");
                    }
                }
                customDtoMapperProjectItem.Save();
            }
        }
        /// <summary>
        /// 添加文件到项目中
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="content"></param>
        /// <param name="fileName"></param>
        private void AddFileToProjectItem(ProjectItem folder, string content, string fileName)
        {
            try
            {
                string path = Path.GetTempPath();
                Directory.CreateDirectory(path);
                string file = Path.Combine(path, fileName);
                File.WriteAllText(file, content, System.Text.Encoding.UTF8);
                try
                {
                    if (folder.ProjectItems == null)
                    {   //解决方案需在SubProject中添加文件。本身ProjectItems为null
                        folder.SubProject.ProjectItems.AddFromFileCopy(file);
                    }
                    else
                    {
                        folder.ProjectItems.AddFromFileCopy(file);
                    }
                }
                finally
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}