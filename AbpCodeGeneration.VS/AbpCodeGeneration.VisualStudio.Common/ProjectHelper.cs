
using AbpCodeGeneration.VisualStudio.Common.Model;
using AbpCodeGeneration.VisualStudio.Common.Templates;
using EnvDTE;
using EnvDTE80;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Engine = RazorEngine.Engine;

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

        private static ParallelLoopResult CacheParallelLoopResult { get; set; }
        public static void InitRazor()
        {
            InitRazorEngine();
            string[] names = {
                "Dto.GetsInputTemplate", "Dto.ListDtoTemplate", "Dto.DetailDtoTemplate",
                "Dto.GetForEditorOutputDtoTemplate", "Dto.CreateDtoTemplate",
                "Validation.CreateValidationTemplate", "Validation.UpdateValidationTemplate",
                "ApplicationService.SettingsTemplate", "Controller.ControllerTemplate",
                "ApplicationService.SettingDefinitionProviderTemplate", "MapperTemplate",
                "ApplicationService.ServiceAuthTemplate", "ApplicationService.ServiceTemplate",
                "ApplicationService.IServiceTemplate", "Dto.CreateOrUpdateDtoBaseTemplate",
                "DomainService.IDomainServiceTemplate", "DomainService.DomainServiceTemplate",
                "Dto.UpdateDtoTemplate"
            };
            CacheParallelLoopResult = Parallel.ForEach(names, n =>
            {
                CacheTemplate(n);
            });
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
                GetBaseProperty(CodeClass, ref classProperties);
            }
            var codeMembers = CodeClass.Members;
            AddClassProperty(codeMembers, ref classProperties);
            model.ClassPropertys = classProperties;

            return model;
        }

        private void GetBaseProperty(CodeClass2 currentClass, ref List<ClassProperty> classProperties)
        {
            //C#仅支持单继承
            CodeClass2 baseClass = currentClass.Bases.Cast<CodeClass2>().ToList()[0];
            CodeElements baseMembers = baseClass.Members;
            AddClassProperty(baseMembers, ref classProperties);
            currentClass = baseClass;
            if (currentClass.Bases.Count > 0)
            {
                GetBaseProperty(currentClass, ref classProperties);
            }
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
            //InitRazorEngine();
            //CompileTemplate();
            while (true)
            {
                if (CacheParallelLoopResult.IsCompleted)
                {
                    break;
                }
            }
            ProjectItem applicationProjectItem = SolutionProjectItems.Find(t => t.Name == ApplicationRootNamespace + model.Prefix + ".Application");
            ProjectItem apiProjectItem = SolutionProjectItems.Find(t => t.Name == ApplicationRootNamespace + model.Prefix + ".HttpApi");
            ProjectItem applicationContractsProjectItem = SolutionProjectItems.Find(t => t.Name == ApplicationRootNamespace + model.Prefix + ".Application.Contracts");
            ProjectItem applicationContractsSharedProjectItem = SolutionProjectItems.Find(t => t.Name == ApplicationRootNamespace + ".Application.Contracts.Shared");
            string moduleName = ClassAbsolutePathInProject.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries)[0];
            model.ModuleName = moduleName;
            //获取当前点击的类所在的项目
            Project topProject = SelectProjectItem.ContainingProject;

            //添加项目目录结构
            var applicationNewFolder = GetDeepProjectItem(applicationProjectItem, ClassAbsolutePathInProject)
                ?? applicationProjectItem.SubProject.ProjectItems.AddFolder(ClassAbsolutePathInProject);
            ProjectItem applicationContractsNewFolder = null;
            ProjectItem dtoFolder;
            if (model.Setting.IsStandardProject)
            {
                applicationContractsNewFolder = GetDeepProjectItem(applicationContractsProjectItem, ClassAbsolutePathInProject)
                    ?? applicationContractsProjectItem.SubProject.ProjectItems.AddFolder(ClassAbsolutePathInProject);                
            }

            //权限
            if (model.Setting.AuthorizationService)
            {
                CreatePermission(model,
                    model.Setting.IsStandardProject
                        ? applicationContractsSharedProjectItem
                        : applicationContractsProjectItem);
            }

            if (model.Setting.Controller)
            {
                CreateController(model, apiProjectItem.SubProject.ProjectItems.Item("Controllers"));
            }
            // 应用服务
            if (model.Setting.ApplicationService)
            {
                CreateSettingFile(model, applicationNewFolder);
                CreateServiceClass(model, applicationNewFolder);
                if (model.Setting.IsStandardProject)
                {
                    CreateServiceInterface(model, applicationContractsNewFolder);
                    dtoFolder = applicationContractsNewFolder.ProjectItems.Item("Dtos")
                        ?? applicationContractsNewFolder.ProjectItems.AddFolder("Dtos");
                }
                else
                {
                    CreateServiceInterface(model, applicationNewFolder);
                    dtoFolder = applicationNewFolder.ProjectItems.Item("Dtos")
                        ?? applicationNewFolder.ProjectItems.AddFolder("Dtos");
                }
                CreateDtos(model, applicationProjectItem, dtoFolder);

                // 参数验证
                if (model.Setting.ValidationType == Enums.ValidationType.FluentApi)
                {
                    CreateValidatorFile(model, dtoFolder);
                }
            }
            // 领域服务
            if (model.Setting.DomainService)
            {
                ProjectItem currentProjectItem = GetDeepProjectItem(topProject, ClassAbsolutePathInProject);
                CreateDomainServiceFile(model, currentProjectItem);
            }
            // 仓储
        }

        /// <summary>
        /// 创建Dto
        /// </summary>
        /// <param name="model"></param>
        /// <param name="applicationProjectItem"></param>
        /// <param name="dtoFolder"></param>
        private void CreateDtos(CreateFileInput model, ProjectItem applicationProjectItem, ProjectItem dtoFolder)
        {
            CreateDtoFile(model, dtoFolder);
            //设置Autompper映射
            
            ProjectItem moduleItem = applicationProjectItem.SubProject.ProjectItems.Item(model.ModuleName);
            ProjectItem mapperItem = moduleItem.ProjectItems.Item(model.ModuleName + "ApplicationAutoMapperProfile.cs");
            if (mapperItem == null)
            {
                CreateMapperFile(model, moduleItem);
            }
            mapperItem = moduleItem.ProjectItems.Item(model.ModuleName + "ApplicationAutoMapperProfile.cs");
            EditMapper(mapperItem, $"{model.Namespace}.{model.DirectoryName}", model.Prefix, model.ClassName, model.LocalName);
        }

        /// <summary>
        /// 缓存模板
        /// </summary>
        /// <param name="name"></param>
        private static void CacheTemplate(string name)
        {
            if (!Engine.Razor.IsTemplateCached(RazorEngine.Engine.Razor.GetKey(name), typeof(DtoFileModel)))
            {
                Engine.Razor.Compile(name, typeof(DtoFileModel));
            }
        }
        /// <summary>
        /// 运行模板
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private string RunTemplate(string name, object model)
        {
            if (!Engine.Razor.IsTemplateCached(Engine.Razor.GetKey(name), typeof(DtoFileModel)))
            {
                Engine.Razor.Compile(name, typeof(DtoFileModel));
            }
            return Engine.Razor.Run(name, typeof(CreateFileInput), model);

        }
        private static void InitRazorEngine()
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
        /// 创建领域服务文件
        /// </summary>
        /// <param name="model"></param>
        /// <param name="coreFolder"></param>
        private void CreateDomainServiceFile(CreateFileInput model, ProjectItem coreFolder)
        {
            string contentAuthorizationProvider = RunTemplate("DomainService.IDomainServiceTemplate", model);
            string fileNameAuthorizationProvider = $"I{model.ClassName}Manager.cs";
            AddFileToProjectItem(coreFolder, contentAuthorizationProvider, fileNameAuthorizationProvider);

            string contentPermissionName = RunTemplate("DomainService.DomainServiceTemplate", model);
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
            string content_GetsInput = RunTemplate("Dto.GetsInputTemplate", model);
            string fileName_GetsInput = $"Get{model.ClassName}sInput.cs";
            AddFileToProjectItem(dtoFolder, content_GetsInput, fileName_GetsInput);

            string content_List = RunTemplate("Dto.ListDtoTemplate", model);
            string fileName_List = $"{model.ClassName}ListDto.cs";
            AddFileToProjectItem(dtoFolder, content_List, fileName_List);

            string content_Detail = RunTemplate("Dto.DetailDtoTemplate", model);
            string fileName_Detail = $"{model.ClassName}DetailDto.cs";
            AddFileToProjectItem(dtoFolder, content_Detail, fileName_Detail);

            string content_CreateAndUpdate = RunTemplate("Dto.CreateOrUpdateDtoBaseTemplate", model);
            string fileName_CreateAndUpdate = $"{model.ClassName}CreateOrUpdateDtoBase.cs";
            AddFileToProjectItem(dtoFolder, content_CreateAndUpdate, fileName_CreateAndUpdate);

            string content_Create = RunTemplate("Dto.CreateDtoTemplate", model);
            string fileName_Create = $"{model.ClassName}CreateDto.cs";
            AddFileToProjectItem(dtoFolder, content_Create, fileName_Create);

            string content_Update = RunTemplate("Dto.UpdateDtoTemplate", model);
            string fileName_Update = $"{model.ClassName}UpdateDto.cs";
            AddFileToProjectItem(dtoFolder, content_Update, fileName_Update);

            string content_GetForUpdate = RunTemplate("Dto.GetForEditorOutputDtoTemplate", model);
            string fileName_GetForUpdate = $"Get{model.ClassName}ForEditorOutput.cs";
            AddFileToProjectItem(dtoFolder, content_GetForUpdate, fileName_GetForUpdate);
        }

        /// <summary>
        /// 创建验证文件
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateValidatorFile(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_Create = RunTemplate("Validation.CreateValidationTemplate", model);
            string fileName_Create = $"{model.ClassName}CreateValidator.cs";
            AddFileToProjectItem(dtoFolder, content_Create, fileName_Create);

            string content_Update = RunTemplate("Validation.UpdateValidationTemplate", model);
            string fileName_Update = $"{model.ClassName}UpdateValidator.cs";
            AddFileToProjectItem(dtoFolder, content_Update, fileName_Update);
        }

        /// <summary>
        /// 创建Setting相关类
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dtoFolder"></param>
        private void CreateSettingFile(CreateFileInput model, ProjectItem dtoFolder)
        {
            string content_IService = RunTemplate("ApplicationService.SettingsTemplate", model);
            string fileName_IService = $"{model.ClassName}Settings.cs";
            AddFileToProjectItem(dtoFolder, content_IService, fileName_IService);

            string content_Service = RunTemplate("ApplicationService.SettingDefinitionProviderTemplate", model);
            string fileName_Service = $"{model.ClassName}SettingDefinitionProvider.cs";
            AddFileToProjectItem(dtoFolder, content_Service, fileName_Service);
        }

        /// <summary>
        /// 创建控制器
        /// </summary>
        /// <param name="model"></param>
        /// <param name="project"></param>
        private void CreateController(CreateFileInput model, ProjectItem project)
        {
            string content_Controller = RunTemplate("Controller.ControllerTemplate", model);
            string fileName_Controller = $"{model.ClassName}Controller.cs";
            AddFileToProjectItem(project, content_Controller, fileName_Controller);
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

            if (model.Setting.AuthorizationService)
            {
                content_Service = RunTemplate("ApplicationService.ServiceAuthTemplate", model);
            }
            else
            {
                content_Service = RunTemplate("ApplicationService.ServiceTemplate", model);
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
            string content_IService = RunTemplate("ApplicationService.IServiceTemplate", model);
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
            string content_Edit = RunTemplate("MapperTemplate", model);
            string fileName_Edit = model.ModuleName + "ApplicationAutoMapperProfile.cs";
            AddFileToProjectItem(dtoFolder, content_Edit, fileName_Edit);
        }
        #endregion

        /// <summary>
        /// 编辑CustomDtoMapper.cs,添加映射
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="className"></param>
        /// <param name="classCnName"></param>
        /// <param name="mapperItem"></param>
        /// <param name="nameSpace"></param>
        private void EditMapper(ProjectItem mapperItem, string nameSpace, string prefix, string className, string classCnName)
        {
            if (mapperItem != null)
            {
                CodeClass codeClass = GetClass(mapperItem.FileCodeModel.CodeElements);
                var insertUsingCode = codeClass.StartPoint.CreateEditPoint();//codeClass.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                insertUsingCode.MoveToLineAndOffset(1, 1);
                insertUsingCode.Insert($"using {nameSpace};\r\n");
                insertUsingCode.Insert($"using {nameSpace}{prefix}.Dtos;\r\n");

                var codeChilds = codeClass.Members;
                foreach (CodeElement codeChild in codeChilds)
                {
                    if (codeChild.Kind == vsCMElement.vsCMElementFunction 
                        && codeChild.Name == mapperItem.Name.Substring(0, mapperItem.Name.IndexOf('.')))
                    {
                        var insertCode = codeChild.GetEndPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                        insertCode.Insert("             #region " + (String.IsNullOrEmpty(classCnName) ? className + "\r\n" : classCnName+ "\r\n"));
                        insertCode.Insert($"            CreateMap<{className}, Get{className}ForEditorOutput>();\r\n");
                        insertCode.Insert($"            CreateMap<{className}, {className}ListDto>();\r\n");
                        insertCode.Insert($"            CreateMap<{className}, {className}DetailDto>();\r\n");
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
            ProjectItem permissionStatement = applicationContractsProjectItem.SubProject.ProjectItems.Item("Permissions").ProjectItems.Item(model.AbsoluteNamespace + "Permissions.cs");
            ProjectItem permissionDefinition = applicationContractsProjectItem.SubProject.ProjectItems.Item("Permissions").ProjectItems.Item(model.AbsoluteNamespace + "PermissionDefinitionProvider.cs");

            if (permissionStatement != null)
            {
                CodeClass permissionCodeClass = GetClass(permissionStatement.FileCodeModel.CodeElements);
                EditPoint permissionPoint = permissionCodeClass.GetEndPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
                permissionPoint.Insert("\r\n");
                permissionPoint.Insert($"public static class {model.ClassName}s\r\n");
                permissionPoint.Insert("{\r\n");
                permissionPoint.Insert($"public const string Default = GroupName + \".{model.ClassName}\";\r\n");
                permissionPoint.Insert($"public const string Create = Default + \".Create\";\r\n");
                permissionPoint.Insert($"public const string Update = Default + \".Update\";\r\n");
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
                            authorizationPoint.Insert($"var {model.CamelClassName}s = {model.CamelAbsoluteNamespace}Group.AddPermission({model.AbsoluteNamespace}Permissions.{model.ClassName}s.Default, L(\"Permission:{model.ClassName}s\"));\r\n");
                            authorizationPoint.Insert($"{model.CamelClassName}s.AddChild({model.AbsoluteNamespace}Permissions.{model.ClassName}s.Create, L(\"Permission:Create\"));\r\n");
                            authorizationPoint.Insert($"{model.CamelClassName}s.AddChild({model.AbsoluteNamespace}Permissions.{model.ClassName}s.Update, L(\"Permission:Edit\"));\r\n");
                            authorizationPoint.Insert($"{model.CamelClassName}s.AddChild({model.AbsoluteNamespace}Permissions.{model.ClassName}s.Delete, L(\"Permission:Delete\"));\r\n");
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