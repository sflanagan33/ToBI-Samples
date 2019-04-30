using UnityEngine;
using System.Collections;

namespace TOBI.Characters
{
    // This defines the most generic concept of a "character". A character is a state machine, which can be in any one
    // of its "character states". The states can tell their owning character to transition with the NewState<>() method.
    // The character also provides input to its states, which is read from the outside world in the ReadInput() method
    // (which must be overridden for your specific character / whatever data it uses.) Optionally, your character's states
    // can be provided with CharacterTransitionInfo, which you should subclass per state that requires it. You pass the
    // info when you call NewState<>(), and receive it in the new state's Enter().

    [DisallowMultipleComponent]
    public abstract class Character<S, I, T> : MonoBehaviour where S : CharacterState<I, T>
                                                             where I : CharacterInput, new()
                                                             where T : CharacterTransitionInfo
    {
        protected S state = null;
        protected I input = new I();

        protected abstract void ReadInput();

        // Every update, read the input this character requires and then run the current state's animation based on it.

        protected void LateUpdate()
        {
            ReadInput();
            state.RunAnimation(input);

            if (Time.timeScale == 0)
                input.Clean();
        }

        // Every fixed update, run the current state's logic based on the character's input and then clean it up afterwards.

        protected void FixedUpdate()
        {
            state.RunLogic(input);
            input.Clean();
        }

        // This can be called by any of this character's states in order to transition to a new one.
        // The optional transition info provided is passed to the new state when it is Enter()-ed.

        public void NewState<N>(T info = null) where N : S
        {
            if (state != null)
            {
                state.Exit();
                Destroy(state);
            }

            state = gameObject.AddComponent<N>();
            state.Enter(info);
        }
    }
    
    // The input used by a character's state machine. Must define some way to "clean" the input (resetting to compiler-default values is fine.)

    public abstract class CharacterInput
    {
        public abstract void Clean();
    }

    // This class doesn't do much, but is here for strong typing. Characters subclass this, and then again per state requiring it.

    public abstract class CharacterTransitionInfo
    {

    }
}