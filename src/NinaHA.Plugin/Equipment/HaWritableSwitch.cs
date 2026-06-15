using System;
using System.Threading;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NinaHA.Client;
using NinaHA.Client.Configuration;
using NinaHA.Client.Dto;

namespace NinaHA.Plugin.Equipment {

    /// <summary>
    /// A writable NINA switch backed by a Home Assistant entity. Writing maps the target value to the
    /// appropriate Home Assistant service call via <see cref="HaValueMapper"/>.
    /// </summary>
    public sealed class HaWritableSwitch : HaSwitch, IWritableSwitch {

        private double targetValue;

        public HaWritableSwitch(short id, SwitchChannel channel, HaSwitchContext context)
            : base(id, channel, context) {
            var state = context.Store.Get(channel.EntityId);
            var range = HaValueMapper.ResolveRange(channel, state);
            Minimum = range.Minimum;
            Maximum = range.Maximum;
            StepSize = range.Step;
            // Seed the target with the current value so the UI doesn't jump on connect.
            targetValue = channel.IsReadable ? Value : Minimum;
        }

        public double Minimum { get; }

        public double Maximum { get; }

        public double StepSize { get; }

        public double TargetValue {
            get => targetValue;
            set {
                targetValue = value;
                RaisePropertyChanged();
            }
        }

        public void SetValue() {
            try {
                var state = Context.Store.Get(Channel.EntityId);
                var call = HaValueMapper.BuildServiceCall(Channel, targetValue, state);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                Context.Rest.CallServiceAsync(call.Domain, call.Service, call.Data, cts.Token).GetAwaiter().GetResult();
            } catch (Exception ex) {
                Logger.Error($"Home Assistant: failed to set '{Channel.EntityId}' to {targetValue}", ex);
            }
        }
    }
}
