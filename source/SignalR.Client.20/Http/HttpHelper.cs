using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using SignalR.Client._20.Transports;
using SignalR.Infrastructure;

namespace SignalR.Client._20.Http
{
	internal static class HttpHelper
	{
		public static EventSignal<CallbackDetail<HttpWebResponse>> PostAsync(string url)
		{
			return PostInternal(url, requestPreparer: null, postData: null);
		}

		public static void PostAsync(string url, IDictionary<string, string> postData)
		{
			PostInternal(url, requestPreparer: null, postData: postData);
		}

		public static EventSignal<CallbackDetail<HttpWebResponse>> PostAsync(string url, Action<HttpWebRequest> requestPreparer)
		{
			return PostInternal(url, requestPreparer, postData: null);
		}

		public static EventSignal<CallbackDetail<HttpWebResponse>> PostAsync(string url, Action<HttpWebRequest> requestPreparer, IDictionary<string, string> postData)
		{
			return PostInternal(url, requestPreparer, postData);
		}

		public static string ReadAsString(HttpWebResponse response)
		{
			try
			{
				using (response)
				{
					using (Stream stream = response.GetResponseStream())
					{
						using (var reader = new StreamReader(stream))
						{
							return reader.ReadToEnd();
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(string.Format("Failed to read response: {0}", ex));
				// Swallow exceptions when reading the response stream and just try again.
				return null;
			}
		}

		private static EventSignal<CallbackDetail<HttpWebResponse>> PostInternal(string url, Action<HttpWebRequest> requestPreparer, IDictionary<string, string> postData)
		{
			var request = (HttpWebRequest)HttpWebRequest.Create(url);

			if (requestPreparer != null)
			{
				requestPreparer(request);
			}

			byte[] buffer = ProcessPostData(postData);

			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			// Set the content length if the buffer is non-null
			request.ContentLength = buffer != null ? buffer.LongLength : 0;

			var signal = new EventSignal<CallbackDetail<HttpWebResponse>>();

			if (buffer == null)
			{
				// If there's nothing to be written to the request then just get the response
				GetResponseAsync(request, signal);
				return signal;
			}

			var requestState = new RequestState { PostData = buffer, Request = request, Response = signal };

			try
			{
				request.BeginGetRequestStream(GetRequestStreamCallback, requestState);
			}
			catch (Exception ex)
			{
				signal.OnFinish(new CallbackDetail<HttpWebResponse> { IsFaulted = true, Exception = ex });
			}
			return signal;
		}

		public static EventSignal<CallbackDetail<HttpWebResponse>> GetAsync(string url)
		{
			return GetAsync(url, requestPreparer: null);
		}

		public static EventSignal<CallbackDetail<HttpWebResponse>> GetAsync(string url, Action<HttpWebRequest> requestPreparer)
		{
			var request = (HttpWebRequest)HttpWebRequest.Create(url);
			if (requestPreparer != null)
			{
				requestPreparer(request);
			}
			var signal = new EventSignal<CallbackDetail<HttpWebResponse>>();
			GetResponseAsync(request, signal);
			return signal;
		}

		public static void GetResponseAsync(HttpWebRequest request, EventSignal<CallbackDetail<HttpWebResponse>> signal)
		{
			try
			{
				request.BeginGetResponse(GetResponseCallback,
										 new RequestState { Request = request, PostData = new byte[] { }, Response = signal });
			}
			catch (Exception ex)
			{
				signal.OnFinish(new CallbackDetail<HttpWebResponse> { Exception = ex, IsFaulted = true });
			}
		}

		private static void GetRequestStreamCallback(IAsyncResult asynchronousResult)
		{
			RequestState requestState = (RequestState)asynchronousResult.AsyncState;

			// End the operation
			try
			{
				Stream postStream = requestState.Request.EndGetRequestStream(asynchronousResult);

				// Write to the request stream.
				postStream.Write(requestState.PostData, 0, requestState.PostData.Length);
				postStream.Close();
			}
			catch (WebException exception)
			{
				requestState.Response.OnFinish(new CallbackDetail<HttpWebResponse> { IsFaulted = true, Exception = exception });
				return;
			}

			// Start the asynchronous operation to get the response
			requestState.Request.BeginGetResponse(GetResponseCallback, requestState);
		}

		private static void GetResponseCallback(IAsyncResult asynchronousResult)
		{
			RequestState requestState = (RequestState)asynchronousResult.AsyncState;

			// End the operation
			try
			{
				HttpWebResponse response = (HttpWebResponse)requestState.Request.EndGetResponse(asynchronousResult);
				requestState.Response.OnFinish(new CallbackDetail<HttpWebResponse> { Result = response });
			}
			catch (Exception ex)
			{
				requestState.Response.OnFinish(new CallbackDetail<HttpWebResponse> { IsFaulted = true, Exception = ex });
			}
		}

		private static byte[] ProcessPostData(IDictionary<string, string> postData)
		{
			if (postData == null || postData.Count == 0)
			{
				return null;
			}

			var sb = new StringBuilder();
			foreach (var pair in postData)
			{
				if (sb.Length > 0)
				{
					sb.Append("&");
				}

				if (String.IsNullOrEmpty(pair.Value))
				{
					continue;
				}

				sb.AppendFormat("{0}={1}", pair.Key, UriQueryUtility.UrlEncode(pair.Value));
			}

			return Encoding.UTF8.GetBytes(sb.ToString());
		}
	}

	public class RequestState
	{
		public HttpWebRequest Request { get; set; }
		public EventSignal<CallbackDetail<HttpWebResponse>> Response { get; set; }
		public byte[] PostData { get; set; }
	}

	public class CallbackDetail<T>
	{
		public bool IsFaulted { get; set; }
		public Exception Exception { get; set; }
		public T Result { get; set; }
	}
}