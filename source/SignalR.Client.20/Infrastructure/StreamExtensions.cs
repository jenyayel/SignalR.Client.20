using System;
using System.IO;
using SignalR.Client._20.Http;
using SignalR.Client._20.Transports;

namespace SignalR.Client._20.Infrastructure
{
	internal static class StreamExtensions
	{
		public static void ReadAsync(EventSignal<CallbackDetail<int>> signal, Stream stream, byte[] buffer)
		{
			var state = new StreamState { Stream = stream, Response = signal, Buffer = buffer};
			
			ReadAsyncInternal(state);
		}

		internal static void ReadAsyncInternal(StreamState streamState)
		{
			try
			{
				streamState.Stream.BeginRead(streamState.Buffer, 0, streamState.Buffer.Length, GetResponseCallback, streamState);
			}
			catch (Exception exception)
			{
				streamState.Response.OnFinish(new CallbackDetail<int> { IsFaulted = true, Exception = exception });
			}
		}

		private static void GetResponseCallback(IAsyncResult asynchronousResult)
		{
			StreamState streamState = (StreamState)asynchronousResult.AsyncState;

			// End the operation
			try
			{
				var response = streamState.Stream.EndRead(asynchronousResult);
				streamState.Response.OnFinish(new CallbackDetail<int> { Result = response });
			}
			catch (Exception ex)
			{
				try
				{
					ReadAsyncInternal(streamState);
				}
				catch (Exception)
				{
					streamState.Response.OnFinish(new CallbackDetail<int> { IsFaulted = true, Exception = ex });
				}
			}
		}
	}

	internal class StreamState
	{
		public Stream Stream { get; set; }
		public byte[] Buffer { get; set; }
		public EventSignal<CallbackDetail<int>> Response { get; set; }
	}
}
