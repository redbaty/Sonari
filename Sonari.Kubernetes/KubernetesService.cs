using System.Collections;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace Sonari.Kubernetes;

public class KubernetesService
{
    public KubernetesService(k8s.Kubernetes kubernetes, IOptions<KubernetesOptions> options)
    {
        Kubernetes = kubernetes;
        Options = options.Value;
    }

    private k8s.Kubernetes Kubernetes { get; }
    
    private KubernetesOptions Options { get; }

    private static IEnumerable<V1EnvVar> GetEnvironmentVariables() =>
        from DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables()
        select new V1EnvVar
        {
            Name = environmentVariable.Key?.ToString(),
            Value = environmentVariable.Value?.ToString()
        };

    private V1Job CreateJobDefinition(string name, string url, int episode, int season, int seriesId)
    {
        const string mountPath = "/output";
        return new V1Job
        {
            Metadata = new V1ObjectMeta
            {
                Name = name
            },
            Spec = new V1JobSpec
            {
                TtlSecondsAfterFinished = Options.JobTtl,
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        RestartPolicy = "Never",
                        Volumes = new V1Volume[]
                        {
                            new()
                            {
                                Name = "nfs-vol",
                                Nfs = new V1NFSVolumeSource
                                {
                                    Path = Options.NfsPath,
                                    Server = Options.NfsHost
                                }
                            }
                        },
                        Containers = new V1Container[]
                        {
                            new()
                            {
                                Name = "wasari",
                                Image = Options.JobImage,
                                Args = new[] { "crunchy", url, "-e", episode.ToString(), "-s", season.ToString(), "-o", mountPath },
                                Env = GetEnvironmentVariables()
                                    .Where(i => i.Name.StartsWith("WASARI_"))
                                    .Concat(new[]
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "NO_PROGRESS_BAR",
                                            Value = "1"
                                        }
                                    })
                                    .ToArray(),
                                VolumeMounts = new V1VolumeMount[]
                                {
                                    new()
                                    {
                                        Name = "nfs-vol",
                                        MountPath = mountPath
                                    }
                                },
                                Resources = new V1ResourceRequirements
                                {
                                    Limits = GetLimits()
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private Dictionary<string, ResourceQuantity> GetLimits()
    {
        var toReturn = new Dictionary<string, ResourceQuantity>();
        
        if(Options.JobCpuLimit != null)
            toReturn.Add("cpu", Options.JobCpuLimit);
        
        if(Options.JobMemoryLimit != null)
            toReturn.Add("memory", Options.JobMemoryLimit);

        return toReturn;
    }

    public async IAsyncEnumerable<V1Job> ListJobs()
    {
        var jobs = await Kubernetes.ListNamespacedJobAsync(Options.Namespace);

        foreach (var jobsItem in jobs.Items)
            yield return jobsItem;
    }

    public Task<V1Job> CreateJob(string name, string url, int episode, int season, int seriesId)
    {
        var jobDefinition = CreateJobDefinition(name, url, episode, season, seriesId);
        return Kubernetes.CreateNamespacedJobAsync(jobDefinition, Options.Namespace);
    }
}