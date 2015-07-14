using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AJ.Andon.Entity.Dictionary;
using AJ.Andon.Entity.Report;

namespace DownTimeSplitService
{
    public class GlobalVars
    {
        public static List<Line> lines = null;
        public static Line line = null;
        public static List<Station> stations = null;
        public static bool IsUndoneDownTime = false;
        public static FlowProduction LastFlowProduction { set; get; }
        public static FlowProduction IsUndoneFlowProduction { set; get; }
        public static Dictionary<int, FlowProduction> dicIsUndoneFlowProduction = new Dictionary<int, FlowProduction>();
    }
}
