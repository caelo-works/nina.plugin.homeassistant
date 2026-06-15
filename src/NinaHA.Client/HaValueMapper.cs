using System;
using System.Collections.Generic;
using System.Globalization;
using NinaHA.Client.Configuration;
using NinaHA.Client.Dto;

namespace NinaHA.Client {

    /// <summary>A resolved Home Assistant service call ready to be posted to the REST API.</summary>
    public readonly struct ServiceCall {
        public ServiceCall(string domain, string service, IReadOnlyDictionary<string, object?> data) {
            Domain = domain;
            Service = service;
            Data = data;
        }
        public string Domain { get; }
        public string Service { get; }
        public IReadOnlyDictionary<string, object?> Data { get; }
    }

    /// <summary>Inclusive numeric range plus step for an analog channel.</summary>
    public readonly struct AnalogRange {
        public AnalogRange(double minimum, double maximum, double step) {
            Minimum = minimum;
            Maximum = maximum;
            Step = step;
        }
        public double Minimum { get; }
        public double Maximum { get; }
        public double Step { get; }
    }

    /// <summary>
    /// Pure conversion logic between Home Assistant entity states/services and the NINA switch model
    /// (a single <c>double</c> value, plus min/max/step for writable switches). No I/O.
    /// </summary>
    public static class HaValueMapper {

        private static readonly HashSet<string> TruthyStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "on", "true", "open", "home", "1", "yes", "active", "unlocked"
        };

        /// <summary>The domain part of an entity id (before the first dot), or empty.</summary>
        public static string InferDomain(string entityId) {
            if (string.IsNullOrEmpty(entityId)) {
                return string.Empty;
            }
            var idx = entityId.IndexOf('.');
            return idx > 0 ? entityId.Substring(0, idx) : string.Empty;
        }

        /// <summary>Converts the current entity state to the NINA switch double value, per channel type.</summary>
        public static double StateToValue(SwitchChannel channel, HaState? state) {
            if (state == null) {
                return 0d;
            }

            switch (channel.Type) {
                case ChannelType.Binary:
                    return TruthyStates.Contains(state.State.Trim()) ? 1d : 0d;

                case ChannelType.Stepped:
                    var options = ResolveOptions(channel, state);
                    var idx = options.IndexOf(state.State);
                    return idx >= 0 ? idx : 0d;

                case ChannelType.Analog:
                default:
                    return double.TryParse(state.State, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0d;
            }
        }

        /// <summary>
        /// Resolves the option list for a stepped channel: configured options take precedence,
        /// otherwise the entity's <c>options</c> attribute is used.
        /// </summary>
        public static List<string> ResolveOptions(SwitchChannel channel, HaState? state) {
            if (channel.Options != null && channel.Options.Count > 0) {
                return channel.Options;
            }
            if (state != null && state.TryGetAttributeStringList("options", out var opts)) {
                return new List<string>(opts);
            }
            return new List<string>();
        }

        /// <summary>
        /// Resolves min/max/step for a writable channel. Binary is fixed 0/1/1; stepped is 0/(N-1)/1;
        /// analog uses channel overrides, then entity attributes (<c>min</c>/<c>max</c>/<c>step</c> or
        /// their <c>native_*</c> variants), then sensible defaults (0/100/1).
        /// </summary>
        public static AnalogRange ResolveRange(SwitchChannel channel, HaState? state) {
            switch (channel.Type) {
                case ChannelType.Binary:
                    return new AnalogRange(0d, 1d, 1d);

                case ChannelType.Stepped:
                    var count = ResolveOptions(channel, state).Count;
                    return new AnalogRange(0d, Math.Max(0d, count - 1), 1d);

                case ChannelType.Analog:
                default:
                    var min = channel.Minimum ?? ReadAttr(state, "min", "native_min_value", 0d);
                    var max = channel.Maximum ?? ReadAttr(state, "max", "native_max_value", 100d);
                    var step = channel.StepSize ?? ReadAttr(state, "step", "native_step", 1d);
                    return new AnalogRange(min, max, step <= 0 ? 1d : step);
            }
        }

        private static double ReadAttr(HaState? state, string primary, string fallback, double @default) {
            if (state == null) {
                return @default;
            }
            if (state.TryGetAttributeDouble(primary, out var v)) {
                return v;
            }
            if (state.TryGetAttributeDouble(fallback, out var v2)) {
                return v2;
            }
            return @default;
        }

        /// <summary>
        /// Builds the Home Assistant service call that applies <paramref name="targetValue"/> to a channel.
        /// </summary>
        public static ServiceCall BuildServiceCall(SwitchChannel channel, double targetValue, HaState? state = null) {
            var domain = !string.IsNullOrWhiteSpace(channel.WriteDomain)
                ? channel.WriteDomain!.Trim()
                : InferDomain(channel.EntityId);

            switch (channel.Type) {
                case ChannelType.Binary: {
                    var service = !string.IsNullOrWhiteSpace(channel.WriteService)
                        ? channel.WriteService!.Trim()
                        : (targetValue >= 0.5 ? "turn_on" : "turn_off");
                    return new ServiceCall(domain, service, new Dictionary<string, object?> {
                        ["entity_id"] = channel.EntityId
                    });
                }

                case ChannelType.Stepped: {
                    var options = ResolveOptions(channel, state);
                    var index = (int)Math.Round(targetValue, MidpointRounding.AwayFromZero);
                    index = Math.Max(0, Math.Min(index, options.Count - 1));
                    var option = options.Count > 0 ? options[index] : string.Empty;
                    var domainS = !string.IsNullOrWhiteSpace(channel.WriteDomain) ? domain : "select";
                    var serviceS = !string.IsNullOrWhiteSpace(channel.WriteService) ? channel.WriteService!.Trim() : "select_option";
                    return new ServiceCall(domainS, serviceS, new Dictionary<string, object?> {
                        ["entity_id"] = channel.EntityId,
                        ["option"] = option
                    });
                }

                case ChannelType.Analog:
                default: {
                    string service;
                    string valueParam;
                    switch (domain) {
                        case "light":
                            service = "turn_on";
                            valueParam = "brightness_pct";
                            break;
                        case "fan":
                            service = "set_percentage";
                            valueParam = "percentage";
                            break;
                        case "input_number":
                            service = "set_value";
                            valueParam = "value";
                            break;
                        case "number":
                        default:
                            service = "set_value";
                            valueParam = "value";
                            break;
                    }
                    if (!string.IsNullOrWhiteSpace(channel.WriteService)) {
                        service = channel.WriteService!.Trim();
                    }
                    return new ServiceCall(domain, service, new Dictionary<string, object?> {
                        ["entity_id"] = channel.EntityId,
                        [valueParam] = targetValue
                    });
                }
            }
        }
    }
}
