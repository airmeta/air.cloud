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
                text: '.NET 版本',
                items: [
                    { text: '.NET 6', link: '/guide/dotnet/net6.html' },
                    { text: '.NET 10', link: '/guide/dotnet/net10.html' }
                ]
            },
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
                        '/guide/air-cloud-core/app-aspect.md',
                        '/guide/air-cloud-core/webapp/unify-result.md',
                        '/guide/air-cloud-core/webapp/data-validation.md',
                        '/guide/air-cloud-core/webapp/friendly-exception.md',
                        '/guide/air-cloud-core/webapp/exception-handling.md',
                        '/guide/air-cloud-core/webapp/cors.md',
                        '/guide/air-cloud-core/webapp/customization.md',
                    ]
                },
                '/guide/air-cloud-core/hostapp.md',
                {
                    title: '数据源',
                    path: '/guide/air-cloud-core/database/',
                    collapsable: true,
                    children: [
                        '/guide/air-cloud-core/database/',
                        {
                            title: '基础能力',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/database/efcore-core.md',
                                '/guide/air-cloud-core/database/transactions.md',
                                '/guide/air-cloud-core/database/options.md',
                                '/guide/air-cloud-core/database/column-metadata-provider.md'
                            ]
                        },
                        {
                            title: '数据库 Provider',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/database/mysql.md',
                                '/guide/air-cloud-core/database/postgresql.md',
                                '/guide/air-cloud-core/database/oracle.md',
                                '/guide/air-cloud-core/database/kingbase.md'
                            ]
                        }
                    ]
                },
                {
                    title: '标准',
                    path: '/guide/air-cloud-core/standard.html',
                    collapsable: true,
                    children: [
                        {
                            title: '服务治理标准',
                            path: '/guide/air-cloud-core/standards/service-governance.html',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/consul.md',
                                '/guide/air-cloud-core/modules/nacos.md'
                            ]
                        },
                        {
                            title: '缓存与锁标准',
                            path: '/guide/air-cloud-core/standards/cache-lock.html',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/redis-cache.md'
                            ]
                        },
                        {
                            title: '消息队列标准',
                            path: '/guide/air-cloud-core/standards/message-queue.html',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/kafka.md',
                                '/guide/air-cloud-core/modules/kafka-usage.md',
                                '/guide/air-cloud-core/modules/rocketmq.md'
                            ]
                        },
                        '/guide/air-cloud-core/standards/scheduler.md',
                        {
                            title: 'Actor 标准',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/akka-application-paradigm.md',
                                '/guide/air-cloud-core/modules/akka.md'
                            ]
                        },
                        {
                            title: '运行容器标准',
                            path: '/guide/air-cloud-core/standards/runtime-container.html',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/docker.md'
                            ]
                        },
                        {
                            title: 'NoSQL 数据访问标准',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/elasticsearch.md'
                            ]
                        },
                        '/guide/air-cloud-core/standards/trace-log.md',
                        '/guide/air-cloud-core/standards/remote-call.md',
                        '/guide/air-cloud-core/standards/object-storage.md',
                        {
                            title: '安全标准',
                            path: '/guide/air-cloud-core/standards/security-plugin.html',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/standards/sky-mirror-shield.md'
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
                {
                    title: '能力组件',
                    collapsable: true,
                    children: [
                        '/guide/air-cloud-core/capabilities/control-plane-auth.md'
                    ]
                },
                {
                    title: '能力与接入',
                    collapsable: true,
                    children: [
                        {
                            title: 'AirTunnel',
                            path: '/guide/network-tunnel/',
                            collapsable: true,
                            children: [
                                '/guide/network-tunnel/',
                                '/guide/network-tunnel/scenarios.md',
                                '/guide/network-tunnel/deployment.md',
                                '/guide/network-tunnel/enrollment-token.md',
                                '/guide/network-tunnel/configuration-push.md',
                                '/guide/network-tunnel/configuration-management.md',
                                '/guide/network-tunnel/management-panel.md',
                                '/guide/network-tunnel/configuration-synchronization.md',
                                '/guide/network-tunnel/server-agent.md',
                                {
                                    title: '隧道接入',
                                    collapsable: true,
                                    children: [
                                        '/guide/network-tunnel/access/ocelot.md',
                                        '/guide/network-tunnel/access/tcp.md',
                                        '/guide/network-tunnel/access/udp.md'
                                    ]
                                },
                                '/guide/network-tunnel/config-security.md',
                                '/guide/network-tunnel/development-plan.md',
                                '/guide/network-tunnel/verification.md',
                                '/guide/network-tunnel/performance.md',
                                {
                                    title: '模块实现',
                                    path: '/guide/air-cloud-core/modules/reverse-tunnel.html',
                                    collapsable: true,
                                    children: [
                                        '/guide/air-cloud-core/modules/reverse-tunnel.md'
                                    ]
                                }
                            ]
                        },
                        {
                            title: 'AirGateway',
                            path: '/guide/air-cloud-core/gateway/standalone.html',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/gateway/standalone.md',
                                '/guide/air-cloud-core/gateway/plugins.md'
                            ]
                        },
                        {
                            title: 'AirForge',
                            path: '/guide/air-cloud-core/dynamic-app/',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/dynamic-app/',
                                '/guide/air-cloud-core/dynamic-app/architecture.md',
                                '/guide/air-cloud-core/dynamic-app/lifecycle.md',
                                 '/guide/air-cloud-core/dynamic-app/manager-api.md',
                                 '/guide/air-cloud-core/dynamic-app/panel.md',
                                 '/guide/air-cloud-core/dynamic-app/硬路由升级动态路由计划.md',
                                 '/guide/air-cloud-core/dynamic-app/local-debug.md',
                                '/guide/air-cloud-core/dynamic-app/publish.md',
                                '/guide/air-cloud-core/dynamic-app/deployment.md',
                                '/guide/air-cloud-core/dynamic-app/manifest.md',
                                '/guide/air-cloud-core/dynamic-app/verification.md'
                            ]
                        }
                    ]
                },
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
                                '/guide/test-report/unit/rocketmq-message-queue.md',
                                '/guide/test-report/unit/akka.md',
                                '/guide/test-report/unit/redis-cache-lock.md',
                                '/guide/test-report/unit/consul-kv-config.md',
                                '/guide/test-report/unit/consul-server-center.md',
                                '/guide/test-report/unit/nacos.md',
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
                                '/guide/test-report/integration/rocketmq.md',
                                '/guide/test-report/integration/akka.md',
                                '/guide/test-report/integration/redis.md',
                                '/guide/test-report/integration/consul.md',
                                '/guide/test-report/integration/consul-server-center.md',
                                '/guide/test-report/integration/nacos.md',
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
                        '/guide/middleware/nacos.md',
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
