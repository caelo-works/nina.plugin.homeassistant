using System.Collections.Generic;
using System.Text.Json;
using NinaHA.Client;
using NinaHA.Client.Configuration;
using NinaHA.Client.Dto;
using Xunit;

namespace NinaHA.Client.Tests {

    public class HaValueMapperTests {

        private static HaState State(string entityId, string state, object? attributes = null) {
            var json = JsonSerializer.Serialize(new {
                entity_id = entityId,
                state,
                attributes = attributes ?? new { }
            });
            return JsonSerializer.Deserialize<HaState>(json)!;
        }

        [Theory]
        [InlineData("switch.foo", "switch")]
        [InlineData("input_number.bar", "input_number")]
        [InlineData("noseparator", "")]
        public void InferDomain_returns_prefix(string entityId, string expected) {
            Assert.Equal(expected, HaValueMapper.InferDomain(entityId));
        }

        [Theory]
        [InlineData("on", 1d)]
        [InlineData("off", 0d)]
        [InlineData("unavailable", 0d)]
        public void StateToValue_binary(string state, double expected) {
            var ch = new SwitchChannel { EntityId = "switch.x", Type = ChannelType.Binary };
            Assert.Equal(expected, HaValueMapper.StateToValue(ch, State("switch.x", state)));
        }

        [Fact]
        public void StateToValue_stepped_uses_option_index() {
            var ch = new SwitchChannel {
                EntityId = "select.mode",
                Type = ChannelType.Stepped,
                Options = new List<string> { "idle", "cool", "heat" }
            };
            Assert.Equal(2d, HaValueMapper.StateToValue(ch, State("select.mode", "heat")));
        }

        [Fact]
        public void StateToValue_analog_parses_number() {
            var ch = new SwitchChannel { EntityId = "sensor.temp", Type = ChannelType.Analog };
            Assert.Equal(21.5d, HaValueMapper.StateToValue(ch, State("sensor.temp", "21.5")));
        }

        [Fact]
        public void ResolveRange_analog_reads_entity_attributes() {
            var ch = new SwitchChannel { EntityId = "number.t", Type = ChannelType.Analog };
            var st = State("number.t", "5", new { min = 1.0, max = 10.0, step = 0.5 });
            var r = HaValueMapper.ResolveRange(ch, st);
            Assert.Equal(1d, r.Minimum);
            Assert.Equal(10d, r.Maximum);
            Assert.Equal(0.5d, r.Step);
        }

        [Fact]
        public void ResolveRange_analog_overrides_win() {
            var ch = new SwitchChannel { EntityId = "number.t", Type = ChannelType.Analog, Minimum = 0, Maximum = 255, StepSize = 1 };
            var st = State("number.t", "5", new { min = 1.0, max = 10.0, step = 0.5 });
            var r = HaValueMapper.ResolveRange(ch, st);
            Assert.Equal(0d, r.Minimum);
            Assert.Equal(255d, r.Maximum);
            Assert.Equal(1d, r.Step);
        }

        [Fact]
        public void BuildServiceCall_binary_on_off() {
            var ch = new SwitchChannel { EntityId = "switch.x", Type = ChannelType.Binary };
            var on = HaValueMapper.BuildServiceCall(ch, 1d);
            Assert.Equal("switch", on.Domain);
            Assert.Equal("turn_on", on.Service);
            Assert.Equal("switch.x", on.Data["entity_id"]);

            var off = HaValueMapper.BuildServiceCall(ch, 0d);
            Assert.Equal("turn_off", off.Service);
        }

        [Fact]
        public void BuildServiceCall_stepped_select_option() {
            var ch = new SwitchChannel {
                EntityId = "select.mode",
                Type = ChannelType.Stepped,
                Options = new List<string> { "idle", "cool", "heat" }
            };
            var call = HaValueMapper.BuildServiceCall(ch, 1d);
            Assert.Equal("select", call.Domain);
            Assert.Equal("select_option", call.Service);
            Assert.Equal("cool", call.Data["option"]);
        }

        [Fact]
        public void BuildServiceCall_analog_number_set_value() {
            var ch = new SwitchChannel { EntityId = "number.t", Type = ChannelType.Analog };
            var call = HaValueMapper.BuildServiceCall(ch, 7.5d);
            Assert.Equal("number", call.Domain);
            Assert.Equal("set_value", call.Service);
            Assert.Equal(7.5d, call.Data["value"]);
        }

        [Fact]
        public void BuildServiceCall_analog_light_uses_brightness_pct() {
            var ch = new SwitchChannel { EntityId = "light.lamp", Type = ChannelType.Analog };
            var call = HaValueMapper.BuildServiceCall(ch, 60d);
            Assert.Equal("light", call.Domain);
            Assert.Equal("turn_on", call.Service);
            Assert.Equal(60d, call.Data["brightness_pct"]);
        }

        [Fact]
        public void BuildServiceCall_analog_fan_uses_percentage() {
            var ch = new SwitchChannel { EntityId = "fan.office", Type = ChannelType.Analog };
            var call = HaValueMapper.BuildServiceCall(ch, 40d);
            Assert.Equal("fan", call.Domain);
            Assert.Equal("set_percentage", call.Service);
            Assert.Equal(40d, call.Data["percentage"]);
        }
    }
}
