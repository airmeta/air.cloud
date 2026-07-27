namespace Air.Cloud.Modules.ReverseTunnel.Common.Http;

internal sealed class ReverseTunnelHttpLimitExceededException : InvalidOperationException
{
    public ReverseTunnelHttpLimitExceededException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
