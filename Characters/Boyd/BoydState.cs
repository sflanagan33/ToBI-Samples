using UnityEngine;
using System.Collections;

namespace TOBI.Characters.Boyd
{
    public abstract class BoydState : CharacterState<BoydInput, BoydTransitionInfo>
    {
        protected Boyd boyd
        {
            get
            {
                if (b == null)
                    b = GetComponent<Boyd>();
                return b;
            }
        }

        private Boyd b;
    }
}