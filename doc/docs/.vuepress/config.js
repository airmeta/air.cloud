module.exports = {
    title: 'Air.Cloud',
    host: 'localhost',
    description: 'Air.Cloud Documentation',
    themeConfig: {
        logo: '/assets/logo.png',
        sidebarDepth: 0,
        displayAllHeaders: false,
        initialOpenGroupIndex: -1,
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
                '/guide/',
                '/guide/constractor.md',
                '/guide/air-cloud-core/concepts.md',
                '/guide/air-cloud-core/loading-mechanism.md',
                '/guide/air-cloud-core/config.md',
                {
                    title: 'Web服务',
                    path: '/guide/air-cloud-core/webapp.html',
                    collapsable: true,
                    children: [
                        '/guide/air-cloud-core/webapp/',
                        '/guide/air-cloud-core/webapp/getting-started.md',
                        '/guide/air-cloud-core/webapp/unify-result.md',
                        '/guide/air-cloud-core/webapp/data-validation.md',
                        '/guide/air-cloud-core/webapp/friendly-exception.md',
                        '/guide/air-cloud-core/webapp/exception-handling.md',
                        '/guide/air-cloud-core/webapp/cors.md',
                        '/guide/air-cloud-core/webapp/customization.md'
                    ]
                },
                '/guide/air-cloud-core/hostapp.md',
                {
                    title: '标准',
                    path: '/guide/air-cloud-core/standard.html',
                    collapsable: true,
                    children: [
                        '/guide/air-cloud-core/standards/message-queue.md',
                        '/guide/air-cloud-core/standards/service-governance.md',
                        '/guide/air-cloud-core/standards/cache-lock.md',
                        '/guide/air-cloud-core/standards/runtime-container.md',
                        '/guide/air-cloud-core/standards/trace-log.md',
                        '/guide/air-cloud-core/standards/scheduler.md',
                        '/guide/air-cloud-core/standards/remote-call.md',
                        '/guide/air-cloud-core/standards/object-storage.md',
                        '/guide/air-cloud-core/standards/security-plugin.md',
                        '/guide/air-cloud-core/standards/sky-mirror-shield.md'
                    ]
                },
                {
                    title: '模块',
                    path: '/guide/air-cloud-core/libs.html',
                    collapsable: true,
                    children: [
                        {
                            title: '服务治理标准',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/consul.md'
                            ]
                        },
                        {
                            title: '缓存与锁标准',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/redis-cache.md'
                            ]
                        },
                        {
                            title: '消息队列标准',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/kafka-usage.md'
                            ]
                        },
                        {
                            title: 'Actor Cluster',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/akka.md'
                            ]
                        },
                        {
                            title: 'NoSQL 数据访问扩展',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/elasticsearch.md'
                            ]
                        }
                    ]
                },
                {
                    title: '插件',
                    path: '/guide/air-cloud-core/plugins.html',
                    collapsable: true,
                    children: [
                        '/guide/air-cloud-core/plugins/api-catalog.md',
                        '/guide/air-cloud-core/plugins/air_jwt.md',
                        '/guide/air-cloud-core/plugins/air_swagger.md',
                    ]
                },
                '/guide/air-cloud-core/usage-advice.md',
                {
                    title: '测试报告',
                    path: '/guide/test-report/',
                    collapsable: true,
                    children: [
                        {
                            title: '单元测试',
                            path: '/guide/test-report/unit.html',
                            collapsable: true,
                            children: [
                                '/guide/test-report/unit/core-standard.md',
                                '/guide/test-report/unit/kafka-message-queue.md',
                                '/guide/test-report/unit/akka.md',
                                '/guide/test-report/unit/redis-cache-lock.md',
                                '/guide/test-report/unit/consul-kv-config.md',
                                '/guide/test-report/unit/elasticsearch-nosql.md',
                                '/guide/test-report/unit/webapp-apicatalog.md',
                                '/guide/test-report/unit/other-modules.md'
                            ]
                        },
                        {
                            title: '集成测试',
                            path: '/guide/test-report/integration.html',
                            collapsable: true,
                            children: [
                                '/guide/test-report/integration/core-standard.md',
                                '/guide/test-report/integration/kafka.md',
                                '/guide/test-report/integration/akka.md',
                                '/guide/test-report/integration/redis.md',
                                '/guide/test-report/integration/consul.md',
                                '/guide/test-report/integration/elasticsearch.md',
                                '/guide/test-report/integration/oracle.md'
                            ]
                        }
                    ]
                },
                {
                    title: '技术学习',
                    collapsable: true,
                    children: [
                        '/guide/middleware/consul.md',
                        '/guide/middleware/kafka.md',
                        '/guide/software/docker.md',
                        '/guide/software/elk/elasticsearch.md'
                    ]
                },
                {
                    title: '其他内容',
                    collapsable: true,
                    children: [
                        '/guide/example.md',
                        '/guide/use.md'
                    ]
                }
            ],
            '/':[''], 
        }
      }
  }

