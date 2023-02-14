using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Snaek.models
{
    public class SnakePart
    {
        public System.Windows.Shapes.Path UiElement { get; set; }
        public Point Position { get; set; }
        public bool IsHead { get; set; }
    }
}
