using System.Collections.Generic;

namespace NinaHA.Client.Configuration {

    /// <summary>
    /// A single user-configured channel: one Home Assistant entity surfaced as one NINA switch.
    /// </summary>
    public sealed class SwitchChannel {

        /// <summary>Display name shown in NINA. Falls back to the entity id when empty.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The Home Assistant entity id, e.g. <c>switch.observatory_power</c>.</summary>
        public string EntityId { get; set; } = string.Empty;

        public ChannelType Type { get; set; } = ChannelType.Binary;

        public ChannelDirection Direction { get; set; } = ChannelDirection.Read;

        /// <summary>Optional override for analog min. When null, derived from the entity attributes.</summary>
        public double? Minimum { get; set; }

        /// <summary>Optional override for analog max. When null, derived from the entity attributes.</summary>
        public double? Maximum { get; set; }

        /// <summary>Optional override for analog step. When null, derived from the entity attributes.</summary>
        public double? StepSize { get; set; }

        /// <summary>
        /// Discrete options for a <see cref="ChannelType.Stepped"/> channel, in index order.
        /// When empty, they are auto-populated from the entity's <c>options</c> attribute at connect time.
        /// </summary>
        public List<string> Options { get; set; } = new List<string>();

        /// <summary>Optional override for the service domain used on write (defaults to the entity domain).</summary>
        public string? WriteDomain { get; set; }

        /// <summary>Optional override for the service name used on write.</summary>
        public string? WriteService { get; set; }

        public bool IsWritable => Direction == ChannelDirection.Write || Direction == ChannelDirection.ReadWrite;

        public bool IsReadable => Direction == ChannelDirection.Read || Direction == ChannelDirection.ReadWrite;
    }
}
