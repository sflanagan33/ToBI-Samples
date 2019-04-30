using UnityEngine;
using System.Collections;

using TOBI.Damage;

namespace TOBI.Characters.Boyd.States
{
    public abstract class BoydAliveState : BoydState
    {
        public override bool RunLogic(BoydInput input)
        {
            if (input.damage != null)
            {
                BoydDeathTransitionInfo info = new BoydDeathTransitionInfo();
                info.damage = (DamageQuery) input.damage;

                boyd.NewState<BoydDeathState>(info);
                return true;
            }

            return false;
        }
    }
}