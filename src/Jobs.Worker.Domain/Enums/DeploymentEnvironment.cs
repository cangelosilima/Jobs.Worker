namespace Jobs.Worker.Domain.Enums;

[Flags]
public enum DeploymentEnvironment
{
    None = 0,
    Development = 1,
    Homologation = 2,
    Production = 4,
    All = Development | Homologation | Production
}
