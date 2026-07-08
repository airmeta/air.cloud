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
            { text: '棣栭〉', link: '/' },
            { text: '鎸囧崡', link: '/guide/' },
            {
                text: '鐩稿叧閾炬帴',
                items: [
                    { text: 'GitHub', link: 'https://github.com/AccessCross/air.cloud' },
                    { text: 'NuGet', link: 'https://www.nuget.org/packages?q=Air.Cloud&includeComputedFrameworks=true&prerel=true' },
                    { text: '璁稿彲璇�', link: 'https://github.com/AccessCross/air.cloud/blob/main/LICENSE' }
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
                    title: 'Web鏈嶅姟',
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
                    title: '鏍囧噯',
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
                    title: '妯″潡',
                    path: '/guide/air-cloud-core/libs.html',
                    collapsable: true,
                    children: [
                        {
                            title: '鏈嶅姟娌荤悊鏍囧噯',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/consul.md',
                                '/guide/air-cloud-core/modules/nacos.md'
                            ]
                        },
                        {
                            title: '缂撳瓨涓庨攣鏍囧噯',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/redis-cache.md'
                            ]
                        },
                        {
                            title: '娑堟伅闃熷垪鏍囧噯',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/kafka-usage.md',
                                '/guide/air-cloud-core/modules/rocketmq.md'
                            ]
                        },
                        {
                            title: 'Actor Cluster',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/akka-application-paradigm.md',
                                '/guide/air-cloud-core/modules/akka.md'
                            ]
                        },
                        {
                            title: 'NoSQL 鏁版嵁璁块棶鎵╁睍',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/modules/elasticsearch.md'
                            ]
                        },
                        {
                            title: '数据库适配',
                            collapsable: true,
                            children: [
                                '/guide/air-cloud-core/database/column-metadata-provider.md',
                                '/guide/air-cloud-core/database/kingbase.md'
                            ]
                        }
                    ]
                },
                {
                    title: '鎻掍欢',
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
                    title: '娴嬭瘯鎶ュ憡',
                    path: '/guide/test-report/',
                    collapsable: true,
                    children: [
                        {
                            title: '鍗曞厓娴嬭瘯',
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
                            title: '闆嗘垚娴嬭瘯',
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
                    title: '鎶€鏈涔�',
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
                    title: '鍏朵粬鍐呭',
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
