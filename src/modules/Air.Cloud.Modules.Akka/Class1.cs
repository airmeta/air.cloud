namespace Air.Cloud.Modules.Akka
{
    public interface IAkkaService
    {
        /// <summary>
        /// 启动AkkaService
        /// </summary>
        void Start();
        /// <summary>
        /// 停止AkkaService 
        /// </summary>
        void Stop();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        void Tell(object message);
        void Tell(object message, string actorName);



    }
}
