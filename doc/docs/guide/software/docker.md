### Docker
Docker 是一个开源的应用容器引擎，基于 Go 语言 并遵从 Apache2.0 协议开源。

Docker 可以让开发者打包他们的应用以及依赖包到一个轻量级、可移植的容器中，然后发布到任何流行的 Linux 机器上，也可以实现虚拟化。

容器是完全使用沙箱机制，相互之间不会有任何接口（类似 iPhone 的 app）,更重要的是容器性能开销极低。

Docker 从 17.03 版本之后分为 CE（Community Edition: 社区版） 和 EE（Enterprise Edition: 企业版），我们用社区版就可以了。

#### 离线安装docker

1. 上传docker离线安装包
    根据你的服务器版本前往Docker官方网站下载离线安装包
2. 解压Docker

    ```
    #解压Docker
    tar -zxvf docker-26.1.4.tgz
    #复制解压后的包
    mv docker/* /usr/bin/
    #测试运行 屏幕上输出 类似: containerd successfully booted in 0.036548s 即可证明你的Dokcer能够在此服务器上运行
    dockerd
    ```

3. 制作systemd 文件

    3.1 定位到systemd文件目录

        ```
        cd /usr/lib/systemd/system
        ```

    3.2 创建docker.service文件

        ```
        #el7版本
        #Linux server-a05e079f-fb2c-4ded-99b7-8fc6dde1ac8b.novalocal 3.10.0-1062.el7.x86_64 #1 SMP Wed Aug 7 18:08:02 UTC 2019 x86_64 x86_64 x86_64 GNU/Linux
        [Unit]
        Description=Docker Application Container Engine
        Documentation=https://docs.docker.com
        After=network - online.target firewalld.service
        Wants=network - online.target
        [Service]
        Type=notify
        # the default is not to use systemd for cgroups because the delegate issues still
        # exists and systemd currently does not support the cgroup feature set required
        # for containers run by docker 
        ExecStart=/usr/bin/dockerd  --ipv6=false
        ExecReload=/bin/kill -s HUP $MAINPID
        # Having non - zero Limit*s causes performance problems due to accounting overhead
        # in the kernel. We recommend using cgroups to do container - local accounting.
        LimitNOFILE=infinity
        LimitNPROC=infinity
        LimitCORE=infinity
        # Uncomment TasksMax if your systemd version supports it.
        # Only systemd 226 and above support this version.
        #TasksMax=infinity
        TimeoutStartSec=0
        # set delegate yes so that systemd does not reset the cgroups of docker containers
        Delegate=yes
        # kill only the docker process, not all processes in the cgroup
        KillMode=process
        # restart the docker process if it exits prematurely
        Restart=on - failure
        StartLimitBurst=3
        StartLimitInterval=60s
        [Install]
        WantedBy=multi - user.target
        ```
    ---
        ```
        #麒麟 统信等版本
        # Linux localhost.localdomain 4.19.90-52.22.v2207.ky10.x86_64 #1 SMP Tue Mar 14 12:19:10 CST 2023 x86_64 x86_64 x86_64 GNU/Linux
        [Unit]
        Description=Docker Application Container Engine
        Documentation=https://docs.docker.com
        After=network - online.target firewalld.service
        Wants=network - online.target
        [Service]
        Type=notify
        # the default is not to use systemd for cgroups because the delegate issues still
        # exists and systemd currently does not support the cgroup feature set required
        # for containers run by docker 
        ExecStart=/usr/bin/dockerd  --ipv6=false
        ExecReload=/bin/kill -s HUP $MAINPID
        # Having non - zero Limit*s causes performance problems due to accounting overhead
        # in the kernel. We recommend using cgroups to do container - local accounting.
        LimitNOFILE=infinity
        LimitNPROC=infinity
        LimitCORE=infinity
        # Uncomment TasksMax if your systemd version supports it.
        # Only systemd 226 and above support this version.
        #TasksMax=infinity
        TimeoutStartSec=0
        # set delegate yes so that systemd does not reset the cgroups of docker containers
        Delegate=yes
        # kill only the docker process, not all processes in the cgroup
        KillMode=process
        # restart the docker process if it exits prematurely
        Restart=on - failure
        StartLimitBurst=3
        StartLimitInterval=60s
        [Install]
        WantedBy=multi - user.target
        ```
    ---
    3.3 创建docker.socket

        ```
        [Unit]
        Description=Docker Socket for the API

        [Socket]
        # If /var/run is not implemented as a symlink to /run, you may need to
        # specify ListenStream=/var/run/docker.sock instead.
        ListenStream=/run/docker.sock
        SocketMode=0660
        SocketUser=root
        SocketGroup=docker

        [Install]
        WantedBy=sockets.target
        EOF
        ```

4. 启动
   
   命令:
    ```
    #重新刷新systemd服务配置
    systemctl daemon-reload
    #设置docker自启动
    systemctl enable docker
    #启动docker进程
    systemctl start docker
    ```

5. 测试
    
    ```
    docker ps -a
    CONTAINER ID   IMAGE     COMMAND   CREATED   STATUS    PORTS     NAMES

    docker images
    REPOSITORY   TAG       IMAGE ID   CREATED   SIZE
    ```
6. 安装Docker-Compose

    6.1 上传docker-compose 离线安装包 到/usr/local/bin

    <a href="https://github.com/docker/compose/tree/main" target="_blank">安装包下载</a>
    
    6.2 赋予权限   
	```
    chmod +x /usr/local/bin/docker-compose
    ```

    6.3 执行docker-compose 命令检查服务状态

    ```
    [root@localhost ~]# docker-compose 

    Usage:  docker compose [OPTIONS] COMMAND

    Define and run multi-container applications with Docker

    Options:
        --ansi string                Control when to print ANSI control characters ("never"|"always"|"auto") (default "auto")
        --compatibility              Run compose in backward compatibility mode
        --dry-run                    Execute command in dry run mode
        --env-file stringArray       Specify an alternate environment file
    -f, --file stringArray           Compose configuration files
        --parallel int               Control max parallelism, -1 for unlimited (default -1)
        --profile stringArray        Specify a profile to enable
        --progress string            Set type of progress output (auto, tty, plain, quiet) (default "auto")
        --project-directory string   Specify an alternate working directory
                                    (default: the path of the, first specified, Compose file)
    -p, --project-name string        Project name

    Commands:
    attach      Attach local standard input, output, and error streams to a service's running container
    build       Build or rebuild services
    config      Parse, resolve and render compose file in canonical format
    cp          Copy files/folders between a service container and the local filesystem
    create      Creates containers for a service
    down        Stop and remove containers, networks
    events      Receive real time events from containers
    exec        Execute a command in a running container
    images      List images used by the created containers
    kill        Force stop service containers
    logs        View output from containers
    ls          List running compose projects
    pause       Pause services
    port        Print the public port for a port binding
    ps          List containers
    pull        Pull service images
    push        Push service images
    restart     Restart service containers
    rm          Removes stopped service containers
    run         Run a one-off command on a service
    scale       Scale services 
    start       Start services
    stats       Display a live stream of container(s) resource usage statistics
    stop        Stop services
    top         Display the running processes
    unpause     Unpause services
    up          Create and start containers
    version     Show the Docker Compose version information
    wait        Block until the first service container stops
    watch       Watch build context for service and rebuild/refresh containers when files are updated

    Run 'docker compose COMMAND --help' for more information on a command.

    ```
    
    6.4 安装完成