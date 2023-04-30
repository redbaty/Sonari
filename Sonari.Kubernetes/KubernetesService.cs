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

    public async IAsyncEnumerable<JobWithPods> GetJobsPods()
    {
        var jobs = await Kubernetes.ListNamespacedJobAsync(Options.Namespace);
        foreach (var job in jobs.Items.Where(i => i.Name().StartsWith(Options.Prefix)))
        {
            var pods = await Kubernetes.ListNamespacedPodAsync(Options.Namespace, labelSelector: $"job-name={job.Spec.Template.Metadata.Labels["job-name"]}");

            yield return new JobWithPods
            {
                Job = job,
                Pods = pods.Items.ToArray()
            };
        }
    }

    private V1Job CreateJobDefinition(string name, string url, int episode, int season, string? token, string[] args, string? downloadPath)
    {
        const string mountPath = "/output";
        var outputArgs = new List<string>()
        {
            "-o",
            string.IsNullOrEmpty(downloadPath) ? mountPath : $"{mountPath}/{downloadPath}"
        };

        if (!string.IsNullOrEmpty(downloadPath))
        {
            outputArgs.Add("--series-folder");
            outputArgs.Add("false");
        }

        return new V1Job
        {
            Metadata = new V1ObjectMeta
            {
                Name = name
            },
            Spec = new V1JobSpec
            {
                TtlSecondsAfterFinished = Options.JobTtl,
                BackoffLimit = 3,
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        Affinity = new V1Affinity
                        {
                            NodeAffinity = new V1NodeAffinity
                            {
                                PreferredDuringSchedulingIgnoredDuringExecution = new List<V1PreferredSchedulingTerm>
                                {
                                    new(new V1NodeSelectorTerm(new List<V1NodeSelectorRequirement>
                                    {
                                        new("nvidia.com/gpu.count", "Gt", new List<string> { "0" })
                                    }), 1)
                                }
                            }
                        },
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
                        RuntimeClassName = Environment.GetEnvironmentVariable("POD_RUNTIME"),
                        Containers = new V1Container[]
                        {
                            new()
                            {
                                Name = "wasari",
                                Image = Options.JobImage,
                                ImagePullPolicy = "Always",
                                Args = new[] { url, "-e", episode.ToString(), "-s", season.ToString(), "-v" }
                                    .Concat(outputArgs)
                                    .Concat(args)
                                    .ToArray(),
                                Env = GetEnvironmentVariables()
                                    .Where(i => i.Name.StartsWith("WASARI_"))
                                    .Concat(new[]
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "NO_PROGRESS_BAR",
                                            Value = "1"
                                        },
                                        new V1EnvVar
                                        {
                                            Name = "WASARI_AUTH_TOKEN",
                                            Value = token
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

        if (Options.JobCpuLimit != null)
            toReturn.Add("cpu", Options.JobCpuLimit);

        if (Options.JobMemoryLimit != null)
            toReturn.Add("memory", Options.JobMemoryLimit);

        if (Options.NvidiaGpuLimit != null)
            toReturn.Add("nvidia.com/gpu", Options.NvidiaGpuLimit);

        return toReturn;
    }

    public async IAsyncEnumerable<V1Job> ListJobs()
    {
        var jobs = await Kubernetes.ListNamespacedJobAsync(Options.Namespace);

        foreach (var jobsItem in jobs.Items)
            yield return jobsItem;
    }

    public Task<V1Status> DeleteJob(string name) => Kubernetes.DeleteNamespacedJobAsync(name, Options.Namespace);

    public Task<V1Job> CreateJob(string name, string url, int episode, int season, string? token, string[] args, string? downloadPath)
    {
        var jobDefinition = CreateJobDefinition($"{Options.Prefix}-{name}", url, episode, season, token, args, downloadPath);
        return Kubernetes.CreateNamespacedJobAsync(jobDefinition, Options.Namespace);
    }
}