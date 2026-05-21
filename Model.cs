using System;
using System.Collections.Generic;

namespace DfaSimulator
{
    public class DfaState
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Label { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsInitial { get; set; }
        public bool IsAccepting { get; set; }
        
        // Custom visual attributes matching extended WPF requirements
        public string FillColor { get; set; } = "#FFFFFF";
        public string BorderColor { get; set; } = "#475569";
        public double Radius { get; set; } = 28;
        public double BorderWidth { get; set; } = 2;
    }

    public class DfaTransition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FromId { get; set; } = "";
        public string ToId { get; set; } = "";
        public List<string> Symbols { get; set; } = new List<string>();
    }

    public class SimulationStep
    {
        public int StepIndex { get; set; }
        public string StateId { get; set; } = "";
        public int CharIndex { get; set; }
        public string Symbol { get; set; } = "";
        public string RemainingWord { get; set; } = "";
    }

    public class AutomatonData
    {
        public List<DfaState> States { get; set; } = new List<DfaState>();
        public List<DfaTransition> Transitions { get; set; } = new List<DfaTransition>();
    }
}
