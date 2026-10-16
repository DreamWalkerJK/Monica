namespace Monica.EventBus.Kafka.UIEventBusKafka.Workspaces;

/// <summary>Identifies the operational task hosted by a Kafka workspace route.</summary>
public enum KafkaWorkspace
{
    /// <summary>Integration identity and available broker evidence.</summary>
    Overview,
    /// <summary>Configured clusters and connection probes.</summary>
    Clusters,
    /// <summary>Topic topology, record inspection, and administration.</summary>
    Topics,
    /// <summary>Consumer membership and committed-offset evidence.</summary>
    ConsumerGroups,
    /// <summary>Live captures and comparable offset rates.</summary>
    Performance
}
