namespace BridgeDaemon
{
    public interface IJob
    {
        void Start(object param);
        void Stop();
    }
}