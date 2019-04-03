using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimioAgentInterface
{
    public class AgentRequestMessage
    {
        public double Status { get; set; }
        public double EpisodeNumber { get; set; }
        public double Reward { get; set; }
        public IList<double> States { get; set; }
    }
}
