
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
        /// <summary>
        /// 选中类在下面中的相对路径
        /// </summary>
        private string ClassAbsolutePathInProject;
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
            // TODO:区分Contracts
            InitRazorEngine();
            //CompileTemplate();

            ProjectItem applicationProjectItem = SolutionProjectItems.Find(t => t.Name == ApplicationRootNamespace + model.Prefix+ ".Application");
            ProjectItem applicationContractsProjectItem = SolutionProjectItems.Find(t => t.Name == ApplicationRootNamespace + model.Prefix+ ".Application.Contracts");

            //获取当前点击的类所在的项目
            Project topProject = SelectProjectItem.ContainingProject;         

            //添加项目目录结构
            var applicationNewFolder = GetDeepProjectItem(applicationProjectItem, ClassAbsolutePathInProject) 
                ?? applicationProjectItem.SubProject.ProjectItems.AddFolder(ClassAbsolutePathInProject);
            var applicationContractsNewFolder = GetDeepProjectItem(applicationContractsProjectItem, ClassAbsolutePathInProject)
                ?? applicationProjectItem.SubProject.ProjectItems.AddFolder(ClassAbsolutePathInProject);

            //添加Dto
            var dtoFolder = model.IsStandardProject 
                ? (applicationContractsNewFolder.ProjectItems.Item("Dtos") 
                    ?? applicationContractsNewFolder.ProjectItems.AddFolder("Dtos")) 
                : (applicationNewFolder.ProjectItems.Item("Dtos") 
                    ?? applicationNewFolder.ProjectItems.AddFolder("Dtos"));
            CreateDtoFile(model, dtoFolder);
            //设置Autompper映射
            string moduleName = ClassAbsolutePathInProject.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries)[0];
            model.ModuleName = moduleName;
            ProjectItem moduleItem = applicationProjectItem.SubProject.ProjectItems.Item(moduleName);
            ProjectItem mapperItem = moduleItem.ProjectItems.Item(moduleName+ "ApplicationAutoMapperProfile.cs");
            if(mapperItem == null)
            {
                CreateMapperFile(model, moduleItem);
            }
            mapperItem = moduleItem.ProjectItems.Item(moduleName + "ApplicationAutoMapperProfile.cs");
            EditMapper(mapperItem, $"{model.Namespace}.{model.DirectoryName}", model.ClassName, model.LocalName);
            // TODO:参数验证
            //var applicationValidatorFolder = applicationNewFolder.ProjectItems.Item("Validators") ?? applicationNewFolder.ProjectItems.AddFolder("Validators");
            //CreateValidatorFile(model, applicationValidatorFolder);
            //权限
            if (model.AuthorizationService)
            {
                CreatePermission(model, applicationContractsProjectItem);
            }
            //应用服务
            if (model.ApplicationService)
            {
                CreateSettingFile(model, applicationNewFolder);
                CreateServiceClass(model, applicationNewFolder);
                if (model.IsStandardProject)
                {
                    CreateServiceInterface(model, applicationContractsNewFolder);
                }
                else
                {
                    CreateServiceInterface(model, applicationNewFolder);
                }
            }
            //领域服务
            if (model.DomainService)
            {
                ProjectItem currentProjectItem = GetDeepProjectItem(topProject, ClassAbsolutePathInProject);
                CreateDomainServiceFile(model, currentProjectItem);
            }            
        }

        /// <summary>
        /// 缓存模板
        /// </summary>
        private void CompileTemplate()
        {
            RazorEngine.Engine.Razor.Compile("CreateDtoTemplate");
            RazorEngine.Engine.Razor.Compile("UpdateDtoTemplate");
            RazorEngine.Engine.Razor.Compile("ListDtoTemplate");
            RazorEngine.Engine.Razor.Compile("CreateOrUpdateDtoBaseTemplate");
            RazorEngine.Engine.Razor.Compile("GetForEditOutputDtoTemplate");
            RazorEngine.Engine.Razor.Compile("GetsInputTemplate");

            RazorEngine.Engine.Razor.Compile("IServiceTemplate");
            RazorEngine.Engine.Razor.Compile("ServiceTemplate");
            RazorEngine.Engine.Razor.Compile("ServiceAuthTemplate");

            RazorEngine.Engine.Razor.Compile("SettingsTemplate");
            RazorEngine.Engine.Razor.Compile("SettingDefinitionProviderTemplate");

            RazorEngine.Engine.Razor.Compile("IDomainServiceTemplate");
            RazorEngine.Engine.Razor.Compile("DomainServiceTemplate");

            RazorEngine.Engine.Razor.Compile("MapperTemplate");
            
        }
        private void InitRazorEngine()
        {
            var config = new TemplateServiceConfiguration
            {
                TemplateManager = new EmbeddedResourceTemplateManager(typeof(Template)),
                CachingProvider = new DefaultCachingProvider()
            };
            RazorEngine.Engine.Razor = RazorEngineService.Create(config);
        }

        #region 获取项目信息
        private ProjectItem GetDeepProjectItem(Project project, string path)
        {
            string[] classAbsolutePaths = path.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
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

        private ProjectItem GetDeepProjectItem(ProjectItem project, string path)
        {
            string[] classAbsolutePaths = path.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
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
            string contentAuthorizationProvider = RazorEngine.Engine.Razor.RunCompile("DomainService.IDomainServiceTemplate", typeof(CreateFileInput), model);
            string fileNameAuthorizationProvider = $"I{model.ClassName}Manager.cs";
            AddFileToProjectItem(coreFolder, contentAuthorizationProvider, fileNameAuthorizationProvider);

            string contentPermissionName = RazorEngine.Engine.Razor.RunCompile("DomainService.DomainServiceTemplate", typeof(CreateFileInput), model);
            string fileNamePermissionName = model.ClassName + "Manager.cs";
            AddFileToProjectItem(coreFolder, contentPermissionName, fileNamePermissionName);
        }

        /// <summary>
        /// 创建Dto文件
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateDtoFile(CreateFileInput model, ProjectItem dtoFolder)
        {
            // TODO:支持创建/修改包含独立属性
            string content_GetsInput = RazorEngine.Engine.Razor.RunCompile("Dto.GetsInputTemplate", typeof(DtoFileModel), model);
            string fileName_GetsInput = $"Get{model.ClassName}sInput.cs";
            AddFileToProjectItem(dtoFolder, content_GetsInput, fileName_GetsInput);

            string content_List = RazorEngine.Engine.Razor.RunCompile("Dto.ListDtoTemplate", typeof(DtoFileModel), model);
            string fileName_List = $"{model.ClassName}ListDto.cs";
            AddFileToProjectItem(dtoFolder, content_List, fileName_List);

            string content_CreateAndUpdate = RazorEngine.Engine.Razor.RunCompile("Dto.CreateOrUpdateDtoBaseTemplate", typeof(DtoFileModel), model);
            string fileName_CreateAndUpdate = $"{model.ClassName}CreateOrUpdateDtoBase.cs";
            AddFileToProjectItem(dtoFolder, content_CreateAndUpdate, fileName_CreateAndUpdate);

            string content_Create = RazorEngine.Engine.Razor.RunCompile("Dto.CreateDtoTemplate", typeof(DtoFileModel), model);
            string fileName_Create = $"{model.ClassName}CreateDto.cs";
            AddFileToProjectItem(dtoFolder, content_Create, fileName_Create);

            string content_Update = RazorEngine.Engine.Razor.RunCompile("Dto.UpdateDtoTemplate", typeof(DtoFileModel), model);
            string fileName_Update = $"{model.ClassName}UpdateDto.cs";
            AddFileToProjectItem(dtoFolder, content_Update, fileName_Update);

            string content_GetForUpdate = RazorEngine.Engine.Razor.RunCompile("Dto.GetForEditOutputDtoTemplate", typeof(DtoFileModel), model);
            string fileName_GetForUpdate = $"Get{model.ClassName}ForEditOutput.cs";
            AddFileToProjectItem(dtoFolder, content_GetForUpdate, fileName_GetForUpdate);

            
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
        /// 创建Setting相关类
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateSettingFile(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_IService = RazorEngine.Engine.Razor.RunCompile("ApplicationService.SettingsTemplate", typeof(CreateFileInput), model);
            string fileName_IService = $"{model.ClassName}Settings.cs";
            AddFileToProjectItem(dtoFolder, content_IService, fileName_IService);

            string content_Service = RazorEngine.Engine.Razor.RunCompile("ApplicationService.SettingDefinitionProviderTemplate", typeof(CreateFileInput), model);
            string fileName_Service = $"{model.ClassName}SettingDefinitionProvider.cs";
            AddFileToProjectItem(dtoFolder, content_Service, fileName_Service);
        }

        /// <summary>
        /// 创建Service类
        /// </summary>
        /// <param name="applicationStr">根命名空间</param>
        /// <param name="name">类名</param>
        /// <param name="project">父文件夹</param>
        /// <param name="dirName">类所在文件夹目录</param>
        private void CreateServiceClass(CreateFileInput model, ProjectItem project)
        {
            string content_Service = String.Empty;

            if (model.AuthorizationService)
            {
                content_Service = RazorEngine.Engine.Razor.RunCompile("ApplicationService.ServiceAuthTemplate", typeof(CreateFileInput), model);
            }
            else
            {
                content_Service = RazorEngine.Engine.Razor.RunCompile("ApplicationService.ServiceTemplate", typeof(CreateFileInput), model);
            }
            string fileName_Service = $"{model.ClassName}AppService.cs";
            AddFileToProjectItem(project, content_Service, fileName_Service);
        }
        /// <summary>
        /// 创建Service接口
        /// </summary>
        /// <param name="model"></param>
        /// <param name="project"></param>
        private void CreateServiceInterface(CreateFileInput model, ProjectItem project)
        {
            string content_IService = RazorEngine.Engine.Razor.RunCompile("ApplicationService.IServiceTemplate", typeof(CreateFileInput), model);
            string fileName_IService = $"I{model.ClassName}AppService.cs";
            AddFileToProjectItem(project, content_IService, fileName_IService);
        }

        /// <summary>
        /// 创建自定义Automapper映射
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateMapperFile(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_Edit = RazorEngine.Engine.Razor.RunCompile("MapperTemplate", typeof(CreateFileInput), model);
            string fileName_Edit = model.ModuleName + "ApplicationAutoMapperProfile.cs";
            AddFileToProjectItem(dtoFolder, content_Edit, fileName_Edit);
        }
        #endregion

        /// <summary>
        /// 编辑CustomDtoMapper.cs,添加映射
        /// </summary>
        /// <param name="applicationProject"></param>
        /// <param name="className"></param>
        /// <param name="classCnName"></param>
        private void EditMapper(ProjectItem mapperItem, string nameSpace, string className, string classCnName)
        {
            if (mapperItem != null)
            {
                CodeClass codeClass = GetClass(mapperItem.FileCodeModel.CodeElements);
                var insertUsingCode = codeClass.StartPoint.CreateEditPoint();//codeClass.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                insertUsingCode.MoveToLineAndOffset(1, 1);
                insertUsingCode.Insert($"using {nameSpace};\r\n");
                insertUsingCode.Insert($"using {nameSpace}.Dto;\r\n");

                var codeChilds = codeClass.Members;
                foreach (CodeElement codeChild in codeChilds)
                {
                    if (codeChild.Kind == vsCMElement.vsCMElementFunction 
                        && codeChild.Name == mapperItem.Name.Substring(0, mapperItem.Name.IndexOf('.')))
                    {
                        var insertCode = codeChild.GetEndPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                        insertCode.Insert("             #region " + (String.IsNullOrEmpty(classCnName) ? className + "\r\n" : classCnName+ "\r\n"));
                        insertCode.Insert($"            CreateMap<{className}, Get{className}ForEditOutput>();\r\n");
                        insertCode.Insert($"            CreateMap<{className}, {className}ListDto>();\r\n");
                        insertCode.Insert($"            CreateMap<{className}CreateDto, {className}>();\r\n");
                        insertCode.Insert($"            CreateMap<{className}UpdateDto, {className}>();\r\n");
                        insertCode.Insert($"            #endregion");
                        insertCode.Insert("\r\n");
                    }
                }
                mapperItem.Save();
            }
        }

        /// <summary>
        /// 编辑PermissionNames.cs\AuthorizationProvider.cs
        /// </summary>
        /// <param name="topProject"></param>
        /// <param name="model"></param>
        private void CreatePermission(CreateFileInput model, ProjectItem applicationContractsProjectItem)
        {
            string permissionPrefix = ApplicationRootNamespace.Split('.').Last();
            ProjectItem permissionStatement = applicationContractsProjectItem.SubProject.ProjectItems.Item("Permissions").ProjectItems.Item(permissionPrefix + "Permissions.cs");
            ProjectItem permissionDefinition = applicationContractsProjectItem.SubProject.ProjectItems.Item("Permissions").ProjectItems.Item(permissionPrefix + "PermissionDefinitionProvider.cs");

            if (permissionStatement != null)
            {
                CodeClass permissionCodeClass = GetClass(permissionStatement.FileCodeModel.CodeElements);
                EditPoint permissionPoint = permissionCodeClass.GetEndPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                permissionPoint.Insert("\r\n");
                permissionPoint.Insert($"public static class {model.ClassName}s\r\n");
                permissionPoint.Insert("{\r\n");
                permissionPoint.Insert($"public const string Default = GroupName + \".{model.ClassName}\";\r\n");
                permissionPoint.Insert($"public const string Create = Default + \".Create\";\r\n");
                permissionPoint.Insert($"public const string Edit = Default + \".Edit\";\r\n");
                permissionPoint.Insert($"public const string Delete = Default + \".Delete\";\r\n");
                permissionPoint.Insert("}\r\n");

                if(permissionDefinition != null)
                {
                    CodeClass authorizationCodeClass = GetClass(permissionDefinition.FileCodeModel.CodeElements);
                    var codeChilds = authorizationCodeClass.Members;
                    foreach (CodeElement codeChild in codeChilds)
                    {
                        if (codeChild.Kind == vsCMElement.vsCMElementFunction && codeChild.Name == "Define")
                        {
                            EditPoint authorizationPoint = codeChild.GetEndPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                            authorizationPoint.Insert("\r\n");
                            authorizationPoint.Insert($"var {model.CamelClassName}s = {permissionPrefix}Group.AddPermission({permissionPrefix}Permissions.{model.ClassName}s.Default, L(\"Permission:{model.ClassName}s\"));\r\n");
                            authorizationPoint.Insert($"{model.CamelClassName}s.AddChild({permissionPrefix}Permissions.{model.ClassName}s.Create, L(\"Permission:Create\"));\r\n");
                            authorizationPoint.Insert($"{model.CamelClassName}s.AddChild({permissionPrefix}Permissions.{model.ClassName}s.Edit, L(\"Permission:Edit\"));\r\n");
                            authorizationPoint.Insert($"{model.CamelClassName}s.AddChild({permissionPrefix}Permissions.{model.ClassName}s.Delete, L(\"Permission:Delete\"));\r\n");
                            authorizationPoint.Insert("\r\n");
                        }
                    }
                }
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