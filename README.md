# AbpCodeGeneration
基于Abp构建的代码生成器，避免了基础代码的编写。

| Package | VS Stable |
| ------- | ------------ |
| [AbpCodeGeneration](https://marketplace.visualstudio.com/items?itemName=Snow258.AbpCodeGeneration) | [![AbpCodeGeneration](https://img.shields.io/badge/VS%20Marketplace-v1.1.4-blue)](https://www.nuget.org/packages/Newtonsoft.Json/) |
| [AbpCodeGeneration VS2022](https://marketplace.visualstudio.com/items?itemName=Snow258.AbpCodeGenerationVS2022) | [![AbpCodeGeneration VS2022](https://img.shields.io/badge/VS%20Marketplace-v0.1.0-blue)](https://www.nuget.org/packages/Dapper.EntityFramework/) |

### 文档
首次启动需要加载模板缓存，请耐心等待。

![basic setting](https://github.com/snowchenlei/AbpCodeGeneration/blob/master/docs/images/%E5%90%AF%E5%8A%A8%E9%A1%B5.png)
#### 初始化设置
- 基础设置
  - 参数验证：当前仅支持`FluentValidation`
  - DDD方式：
    - 简化DDD：未使用`.Contracts`项目, `DTO`、`ApplicationService`、`IApplicationService`等均在`.Application`
    - 标准DDD：使用了`.Contracts`项目, `DTO`、`ApplicationService`、`IApplicationService`等分别置于`.Application`和`.Application.Contracts`
  - 分离服务共享权限：支持前后台项目分离, 权限统一在单项目中。官网默认模板无需勾选此选项
    ![project structure](https://github.com/snowchenlei/AbpCodeGeneration/blob/master/docs/images/%E5%A4%8D%E6%9D%82%E9%A1%B9%E7%9B%AE%E7%BB%93%E6%9E%84.png)
    > 如上项目结构, 若要生成`admin-app`请键入`.Admin`命名空间前缀
- 框架：目录结构均与所选实体目录一致
  - 应用服务：生成`DTO`、`ApplicationService`、`Settings`等文件
  - 领域服务：生成`DomainService`
  - 权限服务：添加权限定义并给`ApplicationService`增加权限验证
  - 控制器：生成`Controller`
  - 仓储：生成`Repository`。注意：此项依赖于应用服务。
- 功能：其它辅助功能，暂未实现

#### 点击下一步将对实体字段进行配置
![field setting](https://github.com/snowchenlei/AbpCodeGeneration/blob/master/docs/images/%E7%94%9F%E6%88%90%E5%AD%97%E6%AE%B5%E9%85%8D%E7%BD%AE.png)
- 类中文名：用于注释
- 类主键：自动读取实体Id字段类型
- 无需使用字段可删除
