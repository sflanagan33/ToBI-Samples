using UnityEngine;
using System.Collections;

namespace TOBI.Characters.Example
{
    // The scripts in this folder show how to set up a new character for TOBI!
    
    // This class isn't doing much, because most of what a state does is already defined in CharacterState.
    // But there's one useful thing it can do: store and expose a reference to the owning character, so
    // any of its states can easily access it and its exposed fields (rigidbody, etc.)

    public abstract class ExampleState : CharacterState<ExampleInput, ExampleTransitionInfo>
    {
        protected Example example
        {
            get
            {
                if (e == null)
                    e = GetComponent<Example>();
                return e;
            }
        }

        private Example e;
    }
}