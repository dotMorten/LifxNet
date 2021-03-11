﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LifxNet {
	public partial class LifxClient : IDisposable {
		/// <summary>
		/// This message is used for changing the color of either a single or multiple zones.
		/// </summary>
		/// <param name="device">Target device</param>
		/// <param name="startIndex">Start index to target</param>
		/// <param name="endIndex">End index to target</param>
		/// <param name="color">LifxColor to use</param>
		/// <param name="transitionDuration">How long to fade</param>
		/// <param name="apply">Whether the effect should be applied immediately or not.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task SetColorZonesAsync(Device device, int startIndex, int endIndex, LifxColor color,
			TimeSpan transitionDuration, bool apply=false) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			if (transitionDuration.TotalMilliseconds > uint.MaxValue ||
			    transitionDuration.Ticks < 0) {
				throw new ArgumentOutOfRangeException(nameof(transitionDuration));
			}

			if (startIndex > endIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));
			var doApply = apply ? 0x01 : 0x00;
			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);
			var duration = (uint) transitionDuration.TotalMilliseconds;
			await BroadcastMessageAsync<AcknowledgementResponse>(device.HostName, header,
				MessageType.SetColorZones, (byte) startIndex, (byte) endIndex, color, duration, doApply);
		}

		/// <summary>
		/// Set a zone of colors
		/// </summary>
		/// <param name="device">The device to set</param>
		/// <param name="transitionDuration">Duration in ms</param>
		/// <param name="index">Start index of the zone. Should probably just be 0 for most cases.</param>
		/// <param name="colors">An array of system.drawing.colors. For completeness, I should probably make an
		/// overload for this that accepts HSB values, but that's kind of a pain. :P</param>
		/// <param name="apply">Whether to apply the effect or immediately or not. defaults to false.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">Thrown if the device is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the duration is longer than the max</exception>
		/// 
		public async Task SetExtendedColorZonesAsync(Device device, TimeSpan transitionDuration, uint index,
			List<LifxColor> colors, bool apply = false) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			if (transitionDuration.TotalMilliseconds > uint.MaxValue ||
			    transitionDuration.Ticks < 0) {
				throw new ArgumentOutOfRangeException(nameof(transitionDuration));
			}

			FrameHeader header = new FrameHeader(GetNextIdentifier(), true);
			var duration = (uint) transitionDuration.TotalMilliseconds;
			var count = (byte) colors.Count;
			var colorBytes = new List<byte>();
			foreach (var color in colors) {
				colorBytes.AddRange(color.ToBytes());
			}
			var doApply = apply ? 0x01 : 0x00;

			await BroadcastMessageAsync<AcknowledgementResponse>(device.HostName, header,
				MessageType.SetExtendedColorZones, duration, doApply, index, count, colorBytes);
		}

		/// <summary>
		/// Try to get the color zones from our device.
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public Task<StateExtendedColorZonesResponse> GetExtendedColorZonesAsync(Device device) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return BroadcastMessageAsync<StateExtendedColorZonesResponse>(
				device.HostName, header, MessageType.GetExtendedColorZones);
		}

		/// <summary>
		/// Try to get the color zones from our device, non-extended.
		/// </summary>
		/// <param name="device">Target device</param>
		/// <param name="startIndex">Start index of requested zones</param>
		/// <param name="endIndex">End index of requested zones</param>
		/// <returns>Either a "StateZone" response for single-zone devices, or "StateMultiZone" response.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public Task<StateMultiZoneResponse> GetColorZonesAsync(Device device, int startIndex, int endIndex) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			if (startIndex > endIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));
			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return BroadcastMessageAsync<StateMultiZoneResponse>(
				device.HostName, header, MessageType.GetColorZones, (byte) startIndex, (byte) endIndex);
		}
		
		/// <summary>
		/// Try to get the color zone from our device, non-extended.
		/// </summary>
		/// <param name="device">Target device</param>
		/// <param name="index">Selected index of the requested zone</param>
		/// <returns>Either a "StateZone" response for single-zone devices, or "StateMultiZone" response.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public Task<StateZoneResponse> GetColorZoneAsync(Device device, int index) {
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			FrameHeader header = new FrameHeader(GetNextIdentifier());
			return BroadcastMessageAsync<StateZoneResponse>(
				device.HostName, header, MessageType.GetColorZones, (byte) index, (byte) index);
		}
	}
}