using System;
using System.Collections.Generic;
using System.Text;

namespace SystemX.Sound.Midi {
    //public delegate void EventHandler<ControllerEventArgs>(object sender, ControllerEventArgs e);

    public sealed class ControllerEventArgs : EventArgs {
        private int controller;
        private ShortMessage message;

        public ControllerEventArgs(ShortMessage message, int controller) {
            this.message = message;
            this.controller = controller;
        }

        public ShortMessage Message {
            get { return message; }
            private set { message = value; }
        }

        public int Controller {
            get { return controller; }
            private set { controller = value; }
        }
    }
}
