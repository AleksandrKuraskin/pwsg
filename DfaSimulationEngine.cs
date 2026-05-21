using System.Collections.Generic;
using System.Linq;

namespace DfaSimulator
{
    public class DfaSimulationEngine
    {
        public List<SimulationStep> SimSteps { get; private set; } = new List<SimulationStep>();
        public int CurrentStepIndex { get; set; } = 0;
        public string? SimActiveStateId { get; private set; }
        public string? SimActiveTransitionId { get; private set; }

        public void UpdateSimulation(string inputWord, DfaManager manager)
        {
            SimSteps.Clear();
            CurrentStepIndex = 0;

            var startState = manager.States.FirstOrDefault(s => s.IsInitial);
            if (startState == null)
            {
                SimActiveStateId = null;
                SimActiveTransitionId = null;
                return;
            }

            SimSteps.Add(new SimulationStep
            {
                StepIndex = 0,
                StateId = startState.Label,
                CharIndex = 0,
                Symbol = "",
                RemainingWord = inputWord
            });

            string currentId = startState.Id;
            for (int i = 0; i < inputWord.Length; i++)
            {
                string readChar = inputWord[i].ToString();
                var trans = manager.Transitions.FirstOrDefault(t => t.FromId == currentId && t.Symbols.Contains(readChar));

                if (trans != null)
                {
                    currentId = trans.ToId;
                    var destState = manager.States.FirstOrDefault(s => s.Id == currentId);
                    
                    SimSteps.Add(new SimulationStep
                    {
                        StepIndex = i + 1,
                        StateId = destState?.Label ?? "ERR",
                        CharIndex = i + 1,
                        Symbol = readChar,
                        RemainingWord = inputWord.Substring(i + 1)
                    });
                }
                else
                {
                    SimSteps.Add(new SimulationStep
                    {
                        StepIndex = i + 1,
                        StateId = "ERR",
                        CharIndex = i + 1,
                        Symbol = "ERR",
                        RemainingWord = "brak przejścia!"
                    });
                    break;
                }
            }
        }

        public void ApplyStepSimulation(DfaManager manager)
        {
            if (SimSteps.Count == 0) return;

            var currStep = SimSteps[CurrentStepIndex];
            var realState = manager.States.FirstOrDefault(s => s.Label == currStep.StateId);
            SimActiveStateId = realState?.Id;

            if (CurrentStepIndex > 0)
            {
                var prevStep = SimSteps[CurrentStepIndex - 1];
                var beforeState = manager.States.FirstOrDefault(s => s.Label == prevStep.StateId);
                if (beforeState != null && realState != null)
                {
                    var match = manager.Transitions.FirstOrDefault(t => t.FromId == beforeState.Id && t.ToId == realState.Id && t.Symbols.Contains(currStep.Symbol));
                    SimActiveTransitionId = match?.Id;
                }
                else
                {
                    SimActiveTransitionId = null;
                }
            }
            else
            {
                SimActiveTransitionId = null;
            }
        }
    }
}
