using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SystemX.Addon {
    public class DelayEventFilter<TEventArgs> : EventFilter<TEventArgs> where TEventArgs : EventArgs {
        private const int DefaultTimeMs = 150;
        private Timer _timer;
        private TEventArgs _pendingEventArgs;
        private int timeMs;

        public DelayEventFilter(object eventObject, string eventName)
            : this(eventObject, eventName, DefaultTimeMs) {
        }

        public DelayEventFilter(object eventObject, string eventName, int timeMs)
            : base(eventObject, eventName) {
            this.timeMs = timeMs;
            _timer = new Timer(TimerCallback, null, timeMs, timeMs);
        }

        private void TimerCallback(Object state) {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            base.OnFilterEventHandler(_pendingEventArgs);
        }

        protected override void FilterEventHandler(object sender, TEventArgs e) {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.sender = sender;
            _pendingEventArgs = e;
            _timer.Change(timeMs, timeMs);
        }
    }
}
