using k8s;
using k8s.Models;

namespace Sonari.Kubernetes;

public class KubernetesService
{
    public KubernetesService(k8s.Kubernetes kubernetes)
    {
        Kubernetes = kubernetes;
    }

    private k8s.Kubernetes Kubernetes { get; }

    private static V1Job CreateJobDefinition(string url, int episode, int season, int seriesId)
    {
        return new V1Job
        {
            Metadata = new V1ObjectMeta
            {
                Name = $"job-sonari-{seriesId}-s{season:00}-e{episode:000}"
            },
            Spec = new V1JobSpec
            {
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        RestartPolicy = "Never",
                        Containers = new List<V1Container>
                        {
                            new()
                            {
                                Name = "wasari",
                                Image = "redbaty/wasari:latest-kb",
                                Args = new[] { "crunchy", url, "-e", episode.ToString(), "-s", season.ToString() },
                                Env = new List<V1EnvVar>
                                {
                                    new()
                                    {
                                        Name = "WASARI_USERNAME",
                                        Value = "redbaty"
                                    },
                                    new()
                                    {
                                        Name = "WASARI_PASSWORD",
                                        Value = "jas@YUNK2scud3duck"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    public async Task CreateJob(string url, int episode, int season, int seriesId)
    {
        var jobDefinition = CreateJobDefinition(url, episode, season, seriesId);
        await Kubernetes.CreateNamespacedJobAsync(jobDefinition, "default");
    }
}