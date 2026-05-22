### Apache Kafka

Apache Kafka是一个开源的分布式流处理平台，‌由Apache软件基金会开发，‌最初由LinkedIn公司开发并开源。‌

它设计用于处理实时数据流，‌允许从各种数据源高效地捕获、‌聚合、‌处理和移动数据。‌Kafka特别适合处理需要实时处理和大数据量的应用场景，‌如高吞吐量的分布式发布订阅消息系统。‌

它的主要特性包括：‌

高吞吐量：‌能够处理大量的数据，‌适合处理需要实时处理和大数据量的应用场景。‌

分布式：‌支持分布式部署，‌能够处理跨多个节点和系统的数据流。‌

可扩展性：‌系统设计具有良好的可扩展性，‌可以根据需求增加更多的节点来处理更多的数据。‌

语言无关：‌使用简单的、‌高性能的、‌语言无关的TCP协议进行客户端与服务器之间的通信，‌保持了向后兼容性。‌


Apache Kafka被广泛应用于构建实时数据流处理管道和流分析应用，‌包括高性能数据管道、‌流分析、‌数据集成和关键任务应用等。


[官方文档链接](https://kafka.apache.org/documentation/)


### docker安装
DockerCompose配置:

```yaml
    services:
     zookeeper:
       image: zookeeper
       container_name: zookeeper
       restart: always
       privileged: true
       ports:
         - 2181:2181
       volumes:
         - "/opt/docker/zookeeper/data:/data"
         - "/opt/docker/zookeeper/datalog:/datalog"
         - "/opt/docker/zookeeper/logs:/logs"
         - "/opt/docker/zookeeper/conf:/conf"
       environment:
         ZOO_MY_ID: 1
         ZOO_SERVERS: server.1=192.168.0.52:2888:3888
       command: ["zkServer.sh", "start-foreground"]
       networks:
         - test
     kafka:
       image: wurstmeister/kafka
       container_name: kafka
       restart: always
       ports:
         - 9092:9092
       volumes:
         - "/opt/docker/kafka/data/conf/server.properties:/etc/kafka/config/server.properties"
         - "/opt/docker/kafka/data/logs:/kafka"
         - "/opt/docker/kafka/data/plugins:/opt/kafka/plugins"
       environment:
         KAFKA_ADVERTISED_HOST_NAME: 192.168.100.159
         KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://192.168.100.159:9092
         KAFKA_ZOOKEEPER_CONNECT: "192.168.100.159:2181"
         KAFKA_ADVERTISED_PORT: 9092
         KAFKA_BROKER_ID: 1
         KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
       depends_on:
         - zookeeper
       networks:
         - test
     networks:
       test:
         external: true
```

#### zoookeeper配置
该配置文件放在: /opt/docker/zookeeper/conf 目录下
```
zoo.conf:
    dataDir=/data
    dataLogDir=/datalog
    clientPort=2181
    tickTime=2000
    initLimit=5
    syncLimit=2
    autopurge.snapRetainCount=3
    autopurge.purgeInterval=0
    maxClientCnxns=60
    standaloneEnabled=true
    admin.enableServer=true
    server.1=zookeeper:2888:3888
    jute.maxbuffer=104857600

```

server.properties

```editorconfig
默认 kafka server.properties  配置如下：
############################# Server Basics #############################          # 服务器基础知识

# The id of the broker. This must be set to a unique integer for each broker.      # 必须为每个代理设置一个唯一的整数
broker.id=0

############################# Socket Server Settings #############################   # 套接字服务器设置

# The address the socket server listens on. It will get the value returned from 
# java.net.InetAddress.getCanonicalHostName() if not configured.
#   FORMAT:
#     listeners = listener_name://host_name:port
#   EXAMPLE:
#     listeners = PLAINTEXT://your.host.name:9092
#listeners=PLAINTEXT://:9092

# Hostname and port the broker will advertise to producers and consumers. If not set, 
# it uses the value for "listeners" if configured.  Otherwise, it will use the value
# returned from java.net.InetAddress.getCanonicalHostName().
#advertised.listeners=PLAINTEXT://your.host.name:9092

# Maps listener names to security protocols, the default is for them to be the same. See the config documentation for more details
#listener.security.protocol.map=PLAINTEXT:PLAINTEXT,SSL:SSL,SASL_PLAINTEXT:SASL_PLAINTEXT,SASL_SSL:SASL_SSL

# The number of threads that the server uses for receiving requests from the network and sending responses to the network
num.network.threads=3                                       # 服务器用于从网络接收请求并向网络发送响应的线程数  默认是3 

# The number of threads that the server uses for processing requests, which may include disk I/O
num.io.threads=8                                               # 服务器用于处理请求的线程数，可能包括磁盘I / O.  默认是 8

# The send buffer (SO_SNDBUF) used by the socket server         ＃套接字服务器使用的发送缓冲区（SO_SNDBUF）
socket.send.buffer.bytes=102400

# The receive buffer (SO_RCVBUF) used by the socket server      ＃套接字服务器使用的接收缓冲区（SO_RCVBUF）
socket.receive.buffer.bytes=102400

# The maximum size of a request that the socket server will accept (protection against OOM)         # 套接字服务器将接受的请求的最大大小（防止OOM）
socket.request.max.bytes=104857600

############################# Log Basics #############################   日志基础

# A comma separated list of directories under which to store log files          ＃逗号分隔的目录列表，用于存储日志文件
log.dirs=/tmp/kafka-logs

# The default number of log partitions per topic. More partitions allow greater    ＃每个主题的默认日志分区数。更多分区允许更大
# parallelism for consumption, but this will also result in more files across      #dileism for consumption，但这也会导致更多的文件
# the brokers.
num.partitions=1                                                                   # 建议broker少的话，默认就几个broker 就设置成几个分区

＃在启动时用于日志恢复和在关闭时刷新的每个数据目录的线程数。
＃对于数据目录位于RAID阵列中的安装，建议增加此值。
# The number of threads per data directory to be used for log recovery at startup and flushing at shutdown.
# This value is recommended to be increased for installations with data dirs located in RAID array.
num.recovery.threads.per.data.dir=1

############################# Internal Topic Settings  #############################   内部主题设置
＃组元数据内部主题“__consumer_offsets”和“__transaction_state”的复制因子
＃对于除开发测试之外的任何其他内容，建议使用大于1的值以确保可用性，例如3。
# The replication factor for the group metadata internal topics "__consumer_offsets" and "__transaction_state"
# For anything other than development testing, a value greater than 1 is recommended for to ensure availability such as 3.
offsets.topic.replication.factor=1
transaction.state.log.replication.factor=1
transaction.state.log.min.isr=1
//关于这3个参数，可以在修改kafka程序中指定的 __consumer_offsets 的副本数 
然后@上海-马吉辉 说只要num.partitions=3，__consumer_offsets副本数就是3，我测试不是 还是1
所以还是以offsets.topic.replication.factor参数控制为准
如果不是第一次启动kafka  那几个配置只有在初次启动生效的。 apache kafka 下载下来应该都默认是 1 吧，2.* 也是 1 啊。
可以这样修改
先停止kafka集群，删除每个broker  data目录下所有__consumer_offsets_*
然后删除zookeeper下rmr /kafkatest/brokers/topics/__consumer_offsets    然后重启kafka
消费一下，这个__consumer_offsets就会创建了
注意：是在第一次消费时，才创建这个topic的，不是broker集群启动就创建，还有那个__trancation_state  topic也是第一次使用事务的时候才会创建

小结：在生产上，没人去删zk里的内容，危险系数大，还是推荐动态扩副本，只要把json写对就好

############################# Log Flush Policy #############################    日志刷新政策
＃消息立即写入文件系统，但默认情况下我们只有fsync（）才能同步
＃懒惰的操作系统缓存。以下配置控制将数据刷新到磁盘。
＃这里有一些重要的权衡：
＃1。持久性：如果您不使用复制，则可能会丢失未刷新的数据。
＃2。延迟：当刷新确实发生时，非常大的刷新间隔可能会导致延迟峰值，因为会有大量数据需要刷新。
＃3。吞吐量：冲洗通常是最昂贵的操作，并且小的冲洗间隔可能导致过多的搜索。
＃以下设置允许配置刷新策略以在一段时间后刷新数据或
＃每N条消息（或两者）。这可以在全局范围内完成，并在每个主题的基础上进行覆盖。

# Messages are immediately written to the filesystem but by default we only fsync() to sync
# the OS cache lazily. The following configurations control the flush of data to disk.
# There are a few important trade-offs here:
#    1. Durability: Unflushed data may be lost if you are not using replication.
#    2. Latency: Very large flush intervals may lead to latency spikes when the flush does occur as there will be a lot of data to flush.
#    3. Throughput: The flush is generally the most expensive operation, and a small flush interval may lead to excessive seeks.
# The settings below allow one to configure the flush policy to flush data after a period of time or
# every N messages (or both). This can be done globally and overridden on a per-topic basis.

# The number of messages to accept before forcing a flush of data to disk   ＃强制刷新数据到磁盘之前要接受的消息数
#log.flush.interval.messages=10000

# The maximum amount of time a message can sit in a log before we force a flush  ＃强制刷新之前消息可以在日志中停留的最长时间
#log.flush.interval.ms=1000

############################# Log Retention Policy #############################   日志保留政策
＃以下配置控制日志段的处理。政策可以
＃设置为在一段时间后或在累积给定大小后删除段。
＃只要满足这些条件* *，就会删除一个段。删除总是发生
＃从日志的末尾开始。

# The following configurations control the disposal of log segments. The policy can
# be set to delete segments after a period of time, or after a given size has accumulated.
# A segment will be deleted whenever *either* of these criteria are met. Deletion always happens
# from the end of the log.

# The minimum age of a log file to be eligible for deletion due to age      ＃由于年龄原因有资格删除的日志文件的最小年龄
log.retention.hours=168

＃日志的基于大小的保留策略。除非剩下，否则将从日志中删除段
＃segments落在log.retention.bytes之下。功能独立于log.retention.hours。
＃log.retention.bytes = 1073741824
# A size-based retention policy for logs. Segments are pruned from the log unless the remaining
# segments drop below log.retention.bytes. Functions independently of log.retention.hours.
#log.retention.bytes=1073741824

＃日志段文件的最大大小。达到此大小时，将创建新的日志段。
# The maximum size of a log segment file. When this size is reached a new log segment will be created.
log.segment.bytes=1073741824

＃检查日志段以查看是否可以删除日志段的时间间隔
# The interval at which log segments are checked to see if they can be deleted according
# to the retention policies
log.retention.check.interval.ms=300000

############################# Zookeeper #############################

# Zookeeper connection string (see zookeeper docs for details).
# This is a comma separated host:port pairs, each corresponding to a zk
# server. e.g. "127.0.0.1:3000,127.0.0.1:3001,127.0.0.1:3002".
# You can also append an optional chroot string to the urls to specify the
# root directory for all kafka znodes.
zookeeper.connect=localhost:2181                      # zookeeper集群的地址，可以是多个，多个之间用逗号分割 hostname1:port1,hostname2:port2,hostname3:port3

# Timeout in ms for connecting to zookeeper
zookeeper.connection.timeout.ms=6000                # ZooKeeper的连接超时时间

############################# Group Coordinator Settings #############################   组协调员设置
＃以下配置指定GroupCoordinator将延迟初始消费者重新平衡的时间（以毫秒为单位）。
＃当新成员加入组时，重新平衡将进一步延迟group.initial.rebalance.delay.ms的值，最多为max.poll.interval.ms。
＃默认值为3秒。
＃我们将此覆盖为0，因为它为开发和测试提供了更好的开箱即用体验。
＃但是，在生产环境中，默认值3秒更合适，因为这有助于避免在应用程序启动期间不必要且可能很昂贵的重新平衡。
group.initial.rebalance.delay.ms = 0

# The following configuration specifies the time, in milliseconds, that the GroupCoordinator will delay the initial consumer rebalance.
# The rebalance will be further delayed by the value of group.initial.rebalance.delay.ms as new members join the group, up to a maximum of max.poll.interval.ms.
# The default value for this is 3 seconds.
# We override this to 0 here as it makes for a better out-of-the-box experience for development and testing.
# However, in production environments the default value of 3 seconds is more suitable as this will help to avoid unnecessary, and potentially expensive, rebalances during application startup.
group.initial.rebalance.delay.ms=0
kafka的扩展参数 抓重点说明
background.threads =4                           # 一些后台任务处理的线程数，例如过期消息文件的删除等，一般情况下不需要去做修改
queued.max.requests =500                          # 等待IO线程处理的请求队列最大数，若是等待IO的请求超过这个数值，那么会停止接受外部消息，应该是一种自我保护机制。
controller.socket.timeout.ms =30000             # partition leader与replicas之间通讯时,socket的超时时间 
controller.message.queue.size=10                # partition leader与replicas数据同步时,消息的队列尺寸
replica.lag.time.max.ms =10000                  # replicas响应partition leader的最长等待时间，若是超过这个时间，就将replicas列入ISR(in-sync replicas)，并认为它是死的，不会再加入管理中 
replica.lag.max.messages =4000                  # 如果follower落后与leader太多,将会认为此follower[或者说partition relicas]已经失效
                                                ##通常,在follower与leader通讯时,因为网络延迟或者链接断开,总会导致replicas中消息同步滞后
                                                ##如果消息之后太多,leader将认为此follower网络延迟较大或者消息吞吐能力有限,将会把此replicas迁移
                                                ##到其他follower中.
                                                ##在broker数量较少,或者网络不足的环境中,建议提高此值.
                                                // Leader会跟踪与其保持同步的Replica列表，该列表称为ISR（即in-sync Replica）。如果一个Follower宕机，或者落后太多，Leader将把它从ISR中移除。这里所描述的“落后太多”指Follower复制的消息落后于Leader后的条数超过预定值（该值可在$KAFKA_HOME/config/server.properties中通过replica.lag.max.messages配置，其默认值是4000）或者Follower超过一定时间（该值可在$KAFKA_HOME/config/server.properties中通过replica.lag.time.max.ms来配置，其默认值是10000）未向Leader发送fetch请求。
replica.socket.timeout.ms=30*1000               # follower与leader之间的socket超时时间 
replica.socket.receive.buffer.bytes=64*1024      # leader复制时候的socket缓存大小  建议  1048576 B = 1M 
replica.fetch.max.bytes =1024*1024             # replicas每次获取数据的最大大小 
replica.fetch.wait.max.ms =500                  # replicas同leader之间通信的最大等待时间，失败了会重试
replica.fetch.min.bytes =1                      # fetch的最小数据尺寸,如果leader中尚未同步的数据不足此值,将会阻塞,直到满足条件
num.replica.fetchers=1                          # leader进行复制的线程数，增大这个数值会增加follower的IO
replica.high.watermark.checkpoint.interval.ms =5000   # 每个replica检查是否将最高水位进行固化的频率
leader.imbalance.per.broker.percentage =10     # leader的不平衡比例，若是超过这个数值，会对分区进行重新的平衡
leader.imbalance.check.interval.seconds =300   # 检查leader是否不平衡的时间间隔
zookeeper.connect = localhost:2181             # zookeeper集群的地址，可以是多个，多个之间用逗号分割 hostname1:port1,hostname2:port2,hostname3:port3
zookeeper.session.timeout.ms=6000              # ZooKeeper的最大超时时间，就是心跳的间隔，若是没有反映，那么认为已经死了，不易过大
zookeeper.connection.timeout.ms =6000          # ZooKeeper的连接超时时间
zookeeper.sync.time.ms =2000                   # ZooKeeper集群中leader和follower之间的同步时间
###############################################
grep '^[a-Z]' server.properties 
broker.id=1                             # //当前机器在集群中的唯一标识，和zookeeper的myid性质一样
host.name=10.9.39.110                   # 这个参数默认是关闭的，在0.8.1有个bug，DNS解析问题，失败率的问题。 尽量写ip
num.network.threads=8                   # 这个是borker进行网络处理的线程数 一般num.network.threads主要处理网络io，读写缓冲区数据，基本没有io等待，配置线程数量为cpu核数加1
num.io.threads=16                       # num.io.threads主要进行磁盘io操作，高峰期可能有些io等待，因此配置需要大些。配置线程数量为cpu核数2倍，最大不超过3倍  
socket.send.buffer.bytes=102400         # 发送缓冲区buffer大小，数据不是一下子就发送的，先回存储到缓冲区了到达一定的大小后在发送，能提高性能   100kb （发送缓冲区）推荐1M
socket.receive.buffer.bytes=102400      # kafka接收缓冲区大小，当数据到达一定大小后在序列化到磁盘   100kb   （接收缓冲区）  推荐1M
socket.request.max.bytes=104857600      # 这个参数是向kafka请求消息或者向kafka发送消息的请请求的最大数，这个值不能超过java的堆栈大小 104857600B =  100M （防止oom）
log.dirs=/data/kafka/kafka-logs         # 消息存放的目录，这个目录可以配置为“，”逗号分割的表达式，上面的num.io.threads要大于这个目录的个数，
                                        //这个目录如果配置多个目录，新创建的topic他把消息持久化的地方是，当前以逗号分割的目录中，那个分区数最少就放那一个
num.partitions=1                        # 默认的分区数，一个topic默认1个分区数  我建议根据brocker数设置 broker有3个 就设置成默认分区为3
num.recovery.threads.per.data.dir=1     # 每个数据目录用来日志恢复的线程数目 对于数据目录位于RAID阵列中的安装，建议增加此值。 一般保持默认
offsets.topic.replication.factor=1
transaction.state.log.replication.factor=1
transaction.state.log.min.isr=1 
//以上3个推荐＃组元数据内部主题“__consumer_offsets”和“__transaction_state”的复制因子对于除开发测试之外的任何其他内容，建议使用大于1的值以确保可用性，例如3。
#log.flush.interval.messages=10000       # 强制刷新数据到磁盘之前要接受的消息数 
#log.flush.interval.ms=1000              ＃ 强制刷新之前消息可以在日志中停留的最长时间 
log.retention.hours=24                  # 默认消息的最大持久化时间，168小时，7天 
＃log.retention.bytes = 1073741824      ＃ 日志的基于大小的保留策略。除非剩下，否则将从日志中删除段  segments落在log.retention.bytes之下。功能独立于log.retention.hours。
log.segment.bytes=1073741824            # 这个参数是：因为kafka的消息是以追加的形式落地到文件，当超过这个值的时候，kafka会新起一个文件
log.retention.check.interval.ms=300000  # 每隔300000毫秒去检查上面配置的log失效时间 
zookeeper.connect=10.9.39.110:2181,10.9.139.65:2181,10.9.35.206:2181,10.9.88.40:2181,10.9.74.126:2181/kafkagroup  # 设置zookeeper的连接端口
zookeeper.connection.timeout.ms=60000   # 设置zookeeper的连接超时时间 
group.initial.rebalance.delay.ms=3      # 以下配置指定GroupCoordinator将延迟初始消费者重新平衡的时间（以毫秒为单位）。 官方推荐成 3

```