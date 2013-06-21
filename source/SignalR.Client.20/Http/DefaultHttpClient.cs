using System;
using System.Collections.Generic;
using SignalR.Client._20.Infrastructure;
using SignalR.Client._20.Transports;

namespace SignalR.Client._20.Http
{
    public class DefaultHttpClient : IHttpClient
    {
        public EventSignal<IResponse> GetAsync(string url, Action<IRequest> prepareRequest)
        {
        	var returnSignal = new EventSignal<IResponse>();
            var signal = HttpHelper.GetAsync(url, request => prepareRequest(new HttpWebRequestWrapper(request)));
			signal.Finished += (sender,e) => returnSignal.OnFinish(new HttpWebResponseWrapper(e.Result.Result) {Exception = e.Result.Exception, IsFaulted = e.Result.IsFaulted});
        	return returnSignal;
        }

		public EventSignal<IResponse> PostAsync(string url, Action<IRequest> prepareRequest, Dictionary<string, string> postData)
        {
			var returnSignal = new EventSignal<IResponse>();
			var signal = HttpHelper.PostAsync(url, request => prepareRequest(new HttpWebRequestWrapper(request)), postData);
			signal.Finished += (sender, e) => returnSignal.OnFinish(new HttpWebResponseWrapper(e.Result.Result) { Exception = e.Result.Exception, IsFaulted = e.Result.IsFaulted });
        	return returnSignal;
        }
    }
}
