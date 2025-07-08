using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaWPF
{
    class WidgetProperties
    {
        public string guid { get; set; }  //Guid ID || Unique ID
        public string name { get; set; } // Name of the html file
        public string tag { get; set; } //Technicaly the path
        public double top { get; set; }
        public double left { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public bool isTransparent { get; set; }
    }
}
