using System;
using System.Threading;

namespace SignalR.Client._20.Transports
{
	public class EventSignal<T>
	{
		private int _attemptCount;
		private readonly int _maxAttempts;

		public EventSignal(int maxAttempts)
		{
			_maxAttempts = maxAttempts;
		}

		public EventSignal() : this(5)
		{
		}

		public event EventHandler<CustomEventArgs<T>> Finished;

		public void OnFinish(T result)
		{
			var handler = Finished;
			if (handler==null)
			{
				if (maxAttemptsReached())
				{
					handleNoEventHandler();
					return;
				}
				_attemptCount++;
				Thread.SpinWait(1000);
				OnFinish(result);
				return;
			}

			handler.Invoke(this, new CustomEventArgs<T> {Result = result});
		}

		protected virtual void handleNoEventHandler()
		{
			throw new InvalidOperationException("You must attach an event handler to the event signal within a reasonable amount of time.");
		}

		private bool maxAttemptsReached()
		{
			return _attemptCount>_maxAttempts;
		}
	}
}