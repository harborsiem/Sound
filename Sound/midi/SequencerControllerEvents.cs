using System;
using System.Collections.Generic;
using System.Text;

namespace SystemX.Sound.Midi {
    public sealed class SequencerControllerEvents : ControllerEvents {
        private ISequencer sequencer;

        public override event EventHandler<ControllerEventArgs> FilteredEvent {
            add {
                lock (m_eventLock) {
                    if (IsFilteredEventEmpty()) { //Connect handler just once
                        //sequencer.Controller += FilterEventHandler;
                    }
                    m_FilteredEvent += value;
                }
            }
            remove {
                lock (m_eventLock) {
                    m_FilteredEvent -= value;
                    if (IsFilteredEventEmpty()) {
                        //sequencer.Controller -= FilterEventHandler;
                    }
                }
            }
        }

        public SequencerControllerEvents(ISequencer sequencer, int[] controllers)
            : base(controllers) {
            if (sequencer == null) {
                throw new ArgumentNullException(nameof(sequencer));
            }
            this.sequencer = sequencer;
        }
    }
}
