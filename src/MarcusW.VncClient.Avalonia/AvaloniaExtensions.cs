using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;

namespace MarcusW.VncClient.Avalonia
{
    public static class AvaloniaExtensions
    {
        public static readonly global::Avalonia.Size EmptySize = new global::Avalonia.Size(0, 0);
        public static global::Avalonia.Size Empty(this global::Avalonia.Size size)
        {
            return EmptySize;
        }
    }

}
