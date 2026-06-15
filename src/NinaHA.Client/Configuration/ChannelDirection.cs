namespace NinaHA.Client.Configuration {

    /// <summary>Whether a channel can be read, written, or both.</summary>
    public enum ChannelDirection {

        /// <summary>Read-only. Exposed to NINA as a plain <c>ISwitch</c>.</summary>
        Read = 0,

        /// <summary>Write-only. Exposed to NINA as an <c>IWritableSwitch</c>.</summary>
        Write = 1,

        /// <summary>Read and write. Exposed to NINA as an <c>IWritableSwitch</c>.</summary>
        ReadWrite = 2
    }
}
