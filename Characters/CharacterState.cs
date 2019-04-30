using UnityEngine;
using System.Collections;

namespace TOBI.Characters
{
    // This defines the most generic concept of a "character state".
    // It can be entered, it can run logic and animation, and it can be exited.

    public abstract class CharacterState<I, T> : MonoBehaviour where I : CharacterInput
                                                               where T : CharacterTransitionInfo
    {
        // What should this state do when it starts? This is the place to do any setup.
        // This method is also optionally passed some CharacterTransitionInfo when it is called.
        // This isn't guaranteed to have a value, but if this state is expecting some special info
        // on what it should do (that the prior state would know about), this is where it gets it.
        // You should subclass your character's CharacterTransitionInfo for each state, since it's
        // unlikely every state would need the same special information. (if they do it's probably input)
        
        public abstract void Enter(T info = null);

        // This is called every UPDATE - every time the game renders. This is where your state should
        // handle any "animation", or more generically, non-core-input code like visuals and sound.
        // DO NOT add forces to rigidbodies, etc in this.

        public abstract void RunAnimation(I input);

        // This is called every FIXED UPDATE - every time the physics ticks. This is where your state
        // should handle all core logical behavior, including all state transitions. This method also returns a bool:
        // you should return true IMMEDIATELY if you're transitioning and calling NewState<> as a result of this state's logic,
        // and return false at the end otherwise (signaling the character will remain in this state for the next fixed update.)
        //
        //  if (myExitCondition)
        //  {
        //      example.NewState<ExampleOtherState>();
        //      return true;
        //  }
        //
        //  return false;
        //
        // Note that not doing this won't actually break anything in the character, but it's useful for another reason:
        // if you need to make hierarchical states, where each state defines partial behavior and its children further flesh it out,
        // it is extremely important to run the highest-level state first because its behavior should define the most generic transitions.
        // In that case, you need to start every overridden RunLogic like this:
        //
        //  if (base.RunLogic(input))
        //      return true;
        //
        // This ensures that a child state will not erroneously run its behavior after a parent state has already determined it should exit.

        public abstract bool RunLogic(I input);

        // What should this state do when it stops? This is the place to do any cleanup.

        public abstract void Exit();
    }
}