using System;
using System.Collections.Generic;
using System.Text;

namespace SystemX.Sound.Sampled {
    //public delegate void EventHandler<LineEventArgs>(object sender, LineEventArgs e);

    public sealed class LineEventArgs : EventArgs {
        private LineEvent evnt;

        public LineEventArgs(LineEvent evnt) {
            this.evnt = evnt;
        }

        public LineEvent Event {
            get { return evnt; }
            private set { evnt = value; }
        }
    }
}
