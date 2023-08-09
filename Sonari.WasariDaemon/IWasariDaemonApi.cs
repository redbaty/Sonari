using Refit;

namespace Sonari.WasariDaemon;

public interface IWasariDaemonApi
{
    [Post("/media/download")]
    public Task Download([Body] DownloadRequest request);
}