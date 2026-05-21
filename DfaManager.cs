using System;
using System.Collections.Generic;
using System.Linq;

namespace DfaSimulator
{
    public class DfaManager
    {
        public List<DfaState> States { get; set; } = new List<DfaState>();
        public List<DfaTransition> Transitions { get; set; } = new List<DfaTransition>();

        public DfaState? ActiveState { get; set; }
        public DfaTransition? ActiveTransition { get; set; }

        public HashSet<string> GetAlphabet()
        {
            var set = new HashSet<string>();
            foreach (var t in Transitions)
            {
                foreach (var s in t.Symbols)
                {
                    if (!string.IsNullOrEmpty(s)) set.Add(s);
                }
            }
            return set;
        }

        public void AddState(DfaState state)
        {
            States.Add(state);
            if (state.IsInitial)
            {
                EnsureSingleInitial(state.Id);
            }
        }

        public void EnsureSingleInitial(string initialId)
        {
            foreach (var s in States)
            {
                if (s.Id != initialId)
                    s.IsInitial = false;
            }
        }

        public void DeleteState(DfaState state)
        {
            string delId = state.Id;
            bool wasInit = state.IsInitial;

            Transitions.RemoveAll(t => t.FromId == delId || t.ToId == delId);
            States.Remove(state);

            if (wasInit && States.Count > 0)
            {
                States[0].IsInitial = true;
            }
        }

        public bool IsBidirectional(string stateA, string stateB)
        {
            if (stateA == stateB) return false;
            return Transitions.Any(t => t.FromId == stateA && t.ToId == stateB) &&
                   Transitions.Any(t => t.FromId == stateB && t.ToId == stateA);
        }
    }
}
