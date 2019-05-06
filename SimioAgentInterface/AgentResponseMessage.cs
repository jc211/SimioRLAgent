using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimioAgentInterface
{
    public class AgentResponseMessage
    {
        public bool IsNoOp { get; set; } = false;
        public double Action { get; set; }
    }
}
