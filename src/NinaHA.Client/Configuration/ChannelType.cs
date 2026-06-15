namespace NinaHA.Client.Configuration {

    /// <summary>How a Home Assistant entity is represented as a NINA switch value.</summary>
    public enum ChannelType {

        /// <summary>On/off entity. Value is 0 or 1.</summary>
        Binary = 0,

        /// <summary>Discrete multi-state entity (e.g. <c>select</c>). Value is the option index 0..N-1.</summary>
        Stepped = 1,

        /// <summary>Continuous numeric entity (e.g. <c>number</c>, <c>input_number</c>, sensor). Value is the raw number.</summary>
        Analog = 2
    }
}
