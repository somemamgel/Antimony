# Antimony 博客

.NET 10 Razor Pages 单人博客，使用 SQLite 保存 Markdown 文章、标签和关联。支持草稿、发布、撤回、Markdown 预览、独立标签管理和单管理员登录。

## 启动

安装 .NET 10 SDK，在项目目录执行：

```sh
dotnet restore
dotnet run -- --hash-password
```

输入管理员密码，复制输出的密码散列。命令只生成散列，不启动网站、不操作数据库。随后配置凭据（将下面的占位符替换为实际值）：

```sh
dotnet user-secrets set 'Admin:Username' 'admin'
dotnet user-secrets set 'Admin:PasswordHash' '复制的密码散列'
dotnet run --launch-profile http
```

打开 http://localhost:5104，进入“管理登录”。前台无需登录。未配置凭据时网站仍能启动，后台登录不可用。没有预设管理员密码。

Production 环境使用环境变量 `Admin__Username` 和 `Admin__PasswordHash` 配置同样的值；通过宿主提供 HTTPS。用户密码不会存入源码或数据库，配置中仅保存散列。重新生成散列并更新配置后，重启应用即可更换密码。

## 写作和标签

登录后进入“文章管理”，选择“新建文章”。填写标题、Markdown 正文及逗号分隔的标签，点击“更新预览”查看排版。保存草稿不公开，发布后出现在首页；已发布文章保存后立即更新，撤回后仅后台可见。撤回、重新发布保留首次发布时间。文章时间按 UTC 保存和显示。

Markdown 支持标题、代码块、表格、引用、列表、图片和链接。原始 HTML 不解析，渲染结果经过清理；图片使用 Markdown 图片 URL，不提供上传功能。

标签保存时自动创建、去重，也可在“标签管理”单独创建、重命名或删除。标签最长 80 字符，不区分大小写，支持中文。删除标签保留文章；删除文章保留标签。前台只显示有关联已发布文章的标签及公开文章数量，后台计数包含草稿。

## 数据存储

首次启动自动应用已提交的 EF Core 迁移并创建 `App_Data/blog.db`。空数据库启动后页面显示空状态，需要自行创建文章。数据库文件不会被提交到版本控制。

默认连接字符串为 `Data Source=App_Data/blog.db`，相对路径以项目内容根目录为基准。通过 `ConnectionStrings:Blog` 或环境变量 `ConnectionStrings__Blog` 可覆盖，例如 `Data Source=/var/lib/antimony/blog.db`。部署时保证数据库目录可写，并持久化整个 `App_Data` 目录；备份时先停止应用，再复制数据库文件。

应用面向单实例运行，启动时自动迁移。后续修改实体使用 .NET 10 的 `dotnet-ef` 生成新迁移并随代码发布；不要删除历史迁移或替换为 `EnsureCreated`。

## 编译

```sh
dotnet build
```

## 测试

在解决方案根目录执行：

```sh
dotnet test Antimony.slnx
```

`Antimony.Test` 使用 xUnit，覆盖管理员凭据、Markdown 渲染与安全清理、标签标准化、解析和查询创建行为。数据库相关用例各自使用独立的 SQLite 内存数据库，不访问 `App_Data/blog.db`，也不需要配置管理员凭据。

测试按类型和被测代码层次组织：

```text
Antimony.Test/
├── Unit/
│   └── Services/
│       ├── AdminCredentialsTests.cs
│       ├── MarkdownServiceTests.cs
│       └── TagServiceTests.cs
└── Integration/
    └── Services/
        └── TagServiceDatabaseTests.cs
```

测试类通过 `Category` 标记分类，可以单独运行单元测试或数据库集成测试：

```sh
dotnet test Antimony.slnx --filter "Category=Unit"
dotnet test Antimony.slnx --filter "Category=Integration"
```

后续测试沿用此目录约定，有实际用例时再创建对应目录。
