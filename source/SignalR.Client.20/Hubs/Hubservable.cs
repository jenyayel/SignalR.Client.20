using System;

namespace SignalR.Client._20.Hubs
{
	public class Hubservable : IObservable<object[]>
	{
		private readonly string _eventName;
		private readonly HubProxy _proxy;

		public Hubservable(HubProxy proxy, string eventName)
		{
			_proxy = proxy;
			_eventName = eventName;
		}

		public IDisposable Subscribe(IObserver<object[]> observer)
		{
			var subscription = _proxy.Subscribe(_eventName);
			subscription.Data += observer.OnNext;

			return new DisposableAction(() =>
			{
				subscription.Data -= observer.OnNext;
			});
		}
	}

	public interface IObserver<T>
	{
		void OnNext(T value);
	}

	public interface IObservable<T>
	{
		IDisposable Subscribe(IObserver<T> observer);
	}
}
