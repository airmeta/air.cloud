
module.exports = {
    title: 'Air.Cloud',
    host: 'localhost',
    description: 'Air.Cloud Documentation',
    themeConfig: {
        logo: '/assets/logo.png',
        sidebarDepth: 2,
        displayAllHeaders: true,
        activeHeaderLinks: true,
        nav: [
            { text: '首页', link: '/' },
            { text: '指南', link: '/guide/' },
            {
                text: '相关链接',
                items: [
                    { text: 'GitHub', link: 'https://github.com/AccessCross/air.cloud' },
                    { text: 'NuGet', link: 'https://www.nuget.org/packages?q=Air.Cloud&includeComputedFrameworks=true&prerel=true' },
                    { text: '许可证', link: 'https://github.com/AccessCross/air.cloud/blob/main/LICENSE' }
                ]
            }
        ],
        sidebar: {
            '/guide/' : [
                {
                    title: 'Air.Cloud',
                    collapsable: false,
                    children: [
                        '/guide/air-cloud-core/core.md',
                        '/guide/constractor.md',
                        '/guide/air-cloud-core/standard.md',
                        '/guide/air-cloud-core/libs.md',
                        '/guide/air-cloud-core/plugins.md',
                        '/guide/air-cloud-core/config.md',
                    ]
                },
                  {
                    title: '核心功能',
                    collapsable: false,
                    children: [
                        '/guide/air-cloud-core/docs/DistributedLock.md',
                    ]
                },
                {
                    title: '模组',
                    collapsable: false,
                    children: [
                        '/guide/air-cloud-core/modules/consul.md',
                        '/guide/air-cloud-core/modules/kafka.md'
                    ]
                },
                {
                    title: '插件',
                    collapsable: false,
                    children: [
                        '/guide/air-cloud-core/plugins/air_jwt.md',
                        '/guide/air-cloud-core/plugins/air_swagger.md',
                    ]
                },
                   {
                    title: 'Air.Cloud.DataBase',
                    collapsable: false,
                    children: [
                        '/guide/air-cloud-core/database/options.md',
                        '/guide/air-cloud-core/database/kingbase.md'
                    ]
                },
                {
                    title: '其他内容',
                    collapsable: false,
                    children: [
                        '/guide/example.md',
                        '/guide/use.md'
                    ]
                },
                {
                    title: '技术学习',
                    collapsable: false,
                    children: [
                        '/guide/middleware/consul.md',
                        '/guide/middleware/kafka.md',
                        '/guide/software/docker.md',
                        '/guide/software/elk/elasticsearch.md'
                    ]
                }
            ],
            '/':[''], 
        }
      }
  }

