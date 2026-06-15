using System;
using System.Collections.Generic;
using System.Globalization;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Container;

namespace NinaHA.Plugin.SequenceItems {

    /// <summary>
    /// Expands a curated subset of N.I.N.A. "$$TOKEN$$" patterns in free text, using live values from the
    /// equipment mediators and the enclosing target container. This is intentionally not a 1:1 copy of
    /// N.I.N.A.'s full file-naming pattern catalog (those are produced at image-save time); it covers the
    /// values that are reliably available at an arbitrary point in a sequence.
    /// </summary>
    public static class HaPatternResolver {

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>The tokens this resolver understands (for documentation/tooltips).</summary>
        public static readonly string[] SupportedTokens = {
            "$$TARGETNAME$$", "$$FILTER$$", "$$CAMERA$$", "$$GAIN$$", "$$OFFSET$$",
            "$$TEMPERATURE$$", "$$SQM$$", "$$DATE$$", "$$TIME$$", "$$DATETIME$$"
        };

        public static string Resolve(
            string? text,
            ISequenceContainer? parent,
            ICameraMediator? camera,
            IFilterWheelMediator? filterWheel,
            IWeatherDataMediator? weather) {

            if (string.IsNullOrEmpty(text) || text!.IndexOf("$$", StringComparison.Ordinal) < 0) {
                return text ?? string.Empty;
            }

            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            try {
                map["$$TARGETNAME$$"] = FindTargetContainer(parent)?.Target?.TargetName ?? string.Empty;
            } catch { map["$$TARGETNAME$$"] = string.Empty; }

            try {
                map["$$FILTER$$"] = filterWheel?.GetInfo()?.SelectedFilter?.Name ?? string.Empty;
            } catch { map["$$FILTER$$"] = string.Empty; }

            try {
                var ci = camera?.GetInfo();
                map["$$CAMERA$$"] = ci?.Name ?? string.Empty;
                map["$$GAIN$$"] = ci != null ? ci.Gain.ToString(Inv) : string.Empty;
                map["$$OFFSET$$"] = ci != null ? ci.Offset.ToString(Inv) : string.Empty;
                map["$$TEMPERATURE$$"] = ci != null ? ci.Temperature.ToString("F1", Inv) : string.Empty;
            } catch { /* leave camera tokens unset */ }

            try {
                var wi = weather?.GetInfo();
                map["$$SQM$$"] = wi != null ? wi.SkyQuality.ToString("F2", Inv) : string.Empty;
            } catch { map["$$SQM$$"] = string.Empty; }

            var now = DateTime.Now;
            map["$$DATE$$"] = now.ToString("yyyy-MM-dd", Inv);
            map["$$TIME$$"] = now.ToString("HH:mm:ss", Inv);
            map["$$DATETIME$$"] = now.ToString("yyyy-MM-dd HH:mm:ss", Inv);

            var result = text;
            foreach (var kv in map) {
                result = result.Replace(kv.Key, kv.Value);
            }
            return result;
        }

        /// <summary>Walks up the parent chain to the nearest target (deep sky object) container.</summary>
        private static IDeepSkyObjectContainer? FindTargetContainer(ISequenceContainer? parent) {
            var current = parent;
            while (current != null) {
                if (current is IDeepSkyObjectContainer dso) {
                    return dso;
                }
                current = current.Parent;
            }
            return null;
        }
    }
}
