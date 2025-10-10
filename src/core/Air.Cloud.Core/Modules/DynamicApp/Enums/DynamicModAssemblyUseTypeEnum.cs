namespace Air.Cloud.Core.Modules.DynamicApp.Enums
{
    /// <summary>
    /// <para>zh-cn:动态模组程序集使用类型枚举</para>
    /// <para>en-us:Dynamic Module Assembly Use Type Enum</para>
    /// </summary>
    /// <remarks>
    ///    <para>zh-cn:在V1版本中我们将会在此枚举中定义Service,Domain,Entity,Model,Repository,Dependency这几个分类,所有的加载逻辑都围绕这几个分类完成</para>
    ///    <para>en-us:In version V1, we will define several categories in this enumeration: Service, Domain, Entity, Model, Repository, and Dependency. All loading logic revolves around these categories.</para>
    /// </remarks>
    public enum DynamicModAssemblyUseTypeEnum
    {
        /// <summary>
        /// <para>zh-cn:接口服务</para>
        /// <para>en-us:Interface Service</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:标记为此类型的程序集中,更多的是关于接口服务的实现以及对于各个领域接口的调用,是直接接受来自主程序请求的入口</para>
        /// <para>en-us:Assemblies marked as this type are more about the implementation of interface services and the invocation of various domain interfaces</para>
        /// </remarks>
        Service,
        /// <summary>
        /// <para>zh-cn:领域实现</para>
        /// <para>en-us:Domain Implementation</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:标记为此类型的程序集,需要对Model中定义的领域接口进行实现</para>
        ///  <para>en-us:Assemblies marked as this type need to implement the domain interfaces defined in the Model</para>
        /// </remarks>
        Domain,

        /// <summary>
        /// <para>zh-cn:实体</para>
        /// <para>en-us:Entity</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:包含数据库实体的程序集,解释了数据库的数据表结构,在更加细分的项目中会用到,普通的五层服务中,以Model作为代替</para>
        ///  <para>en-us:Contains the assembly of database entities, which explains the structure of the database tables. It will be used in more subdivided projects. In ordinary five-layer services, it is replaced by Model.</para>
        /// </remarks>
        Entity,

        /// <summary>
        /// <para>zh-cn:模型</para>
        /// <para>en-us:Model</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:模型用来描述数据结构,会覆盖Entity的职能范围,但不包含任何业务逻辑,Model中的内容包含Entity的全部内容,是Entity的高级版本,多出了领域定义,领域工具类等一些关于实体领域的操作</para>
        /// <para>en-us:The model is used to describe the data structure, which will cover the functional scope of the Entity, but does not contain any business logic. The content in the Model contains all the content of the Entity, which is an advanced version of the Entity, with additional domain definitions, domain tools, and other operations related to entity domains.</para>
        /// </remarks>
        Model,

        /// <summary>
        /// <para>zh-cn:仓储</para>
        /// <para>en-us:Repository</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:用来描述如何连接到数据源</para>
        ///  <para>en-us:Used to describe how to connect to the data source</para>
        /// </remarks>
        Repository,

        /// <summary>
        /// <para>zh-cn:依赖</para>
        /// <para>en-us:Dependency</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:其他需要加载的程序集,依赖包等</para>
        ///  <para>en-us:Other assemblies that need to be loaded, dependency packages, etc.</para>
        /// </remarks>
        Dependency
    }
}
