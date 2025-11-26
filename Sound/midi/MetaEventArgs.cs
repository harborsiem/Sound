using System;
using System.Collections.Generic;
using System.Text;

namespace SystemX.Sound.Midi {
    //public delegate void EventHandler<MetaEventArgs>(object sender, MetaEventArgs e);

    public sealed class MetaEventArgs : EventArgs {
        private MetaMessage message;

        public MetaEventArgs(MetaMessage message) {
            this.message = message;
        }

        public MetaMessage Message {
            get { return message; }
            private set { message = value; }
        }
    }
}
