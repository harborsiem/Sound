using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace SystemX.Addon {
    public abstract class EventFilter<TEventArgs> where TEventArgs : EventArgs {
        protected readonly object m_eventLock = new object();
        protected object sender;

        protected event EventHandler<TEventArgs> m_FilteredEvent;
        private object eventObject;
        private EventInfo eventInfo;
        private Delegate dHandler;

        public virtual event EventHandler<TEventArgs> FilteredEvent {
            add {
                lock (m_eventLock) {
                    if (eventInfo != null && IsFilteredEventEmpty()) { //Connect handler just once
                        eventInfo.AddEventHandler(eventObject, dHandler);
                    }
                    m_FilteredEvent += value;
                }
            }
            remove {
                lock (m_eventLock) {
                    m_FilteredEvent -= value;
                    if (eventInfo != null && IsFilteredEventEmpty()) {
                        eventInfo.RemoveEventHandler(eventObject, dHandler);
                    }
                }
            }
        }

        public EventFilter() {
        }

        public EventFilter(object eventObject, string eventName) {
            if (eventObject == null) {
                throw new ArgumentNullException(nameof(eventObject));
            }
            if (string.IsNullOrEmpty(eventName)) {
                throw new ArgumentNullException(nameof(eventName));
            }
            this.eventObject = eventObject;
            this.eventInfo = GetEventInfo(eventObject, eventName);
        }

        private EventInfo GetEventInfo(object eventObject, string eventName) {
            EventInfo infoName = null;
            Type tEventObject = eventObject.GetType();
            infoName = tEventObject.GetEvent(eventName);
            if (infoName != null) {
                try {
                    dHandler = Delegate.CreateDelegate(infoName.EventHandlerType, this, "FilterEventHandler");
                }
                catch (ArgumentException) {
                    throw new ArgumentException("Type of TEventArgs is wrong for the event");
                }
            } else {
                throw new ArgumentException("Event name does not exist");
            }
            return infoName;
        }

        protected bool IsFilteredEventEmpty() {
            return m_FilteredEvent == null;
        }

        protected virtual void FilterEventHandler(object sender, TEventArgs e) {
            this.sender = sender;
            OnFilterEventHandler(e);
        }

        protected virtual void OnFilterEventHandler(TEventArgs e) {
            EventHandler<TEventArgs> handler = m_FilteredEvent;

            if (handler != null) {
                handler(sender, e);
            }
        }
    }
}
