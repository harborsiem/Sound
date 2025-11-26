using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Addon;

namespace SystemX.Sound.Midi {
    public abstract class ControllerEvents : EventFilter<ControllerEventArgs> {
        private List<int> controllers = new List<int>();

        public ControllerEvents(int[] controllers) {
            if (controllers == null) {
                throw new ArgumentNullException(nameof(controllers));
            }
            for (int i = 0; i < controllers.Length; i++) {
                if (!this.controllers.Contains(controllers[i])) {
                    this.controllers.Add(controllers[i]);
                }
            }
        }

        public void AddControllers(int[] controllers) {
            if (controllers == null) {
                return;
            }
            for (int i = 0; i < controllers.Length; i++) {
                if (!this.controllers.Contains(controllers[i])) {
                    this.controllers.Add(controllers[i]);
                }
            }
        }

        public void RemoveControllers(int[] controllers) {
            if (controllers == null) {
                this.controllers.Clear();
                return;
            }
            for (int i = 0; i < controllers.Length; i++) {
                if (this.controllers.Contains(controllers[i])) {
                    this.controllers.Remove(controllers[i]);
                }
            }
        }

        protected override void OnFilterEventHandler(ControllerEventArgs e) {
            if (!controllers.Contains(e.Controller)) {
                return;
            }
            base.OnFilterEventHandler(e);
        }
    }
}
