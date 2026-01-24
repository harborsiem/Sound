using System;
using System.Collections.Generic;
using System.Text;

namespace SystemX.Addon {
    public interface IEventDispatcher {
        void Dispatch(IEventDispatcher sender, EventArgs eventArgs);
    }
}
