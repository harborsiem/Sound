using System;
using System.Collections.Generic;
using System.Text;

namespace SystemX.Addon {
    internal static class Objects {
        /**
         * Returns {@code true} if the arguments are equal to each other
         * and {@code false} otherwise.
         * Consequently, if both arguments are {@code null}, {@code true}
         * is returned.  Otherwise, if the first argument is not {@code
         * null}, equality is determined by calling the {@link
         * Object#equals equals} method of the first argument with the
         * second argument of this method. Otherwise, {@code false} is
         * returned.
         *
         * @param a an object
         * @param b an object to be compared with {@code a} for equality
         * @return {@code true} if the arguments are equal to each other
         * and {@code false} otherwise
         * @see Object#equals(Object)
         */
        public new static bool Equals(Object a, Object b) {
            return (a == b) || (a != null && a.Equals(b));
        }
    }
}
