using UnityEngine;
using System.Collections;

using TOBI.Damage;

namespace TOBI.Characters.Example
{
    // The scripts in this folder show how to set up a new character for TOBI!

    // A "character" in TOBI is a very generic concept. Luke is a character, and so is every enemy.
    // But they have a lot in common! Their behavior consists of discrete "states" (running, punching,
    // falling down, whatever.) These states can transition to each other, to create characters with
    // complex behavior. (An enemy might transition from "idle" to "attack" when you're close to it.)
    // Every character in TOBI is a state machine: a collection of many states that describe how the
    // character acts in various situations, as well as when and why they should change how they're acting.
    
    // States aren't enough, though! Many characters need to rely on information from the "outside world"
    // in order to make decisions about when to change states. This information is known as "input". This
    // doesn't necessarily mean player input - it's just the input to the state machine. When Luke is in
    // his standing state (LukeUprightState), he'll transition to his jumping state (LukeAirborneState)
    // when the jump boolean in his input is true. Likewise, an enemy might need to respond to being
    // punched in the face - but the state machine can't feel pain. There's an easy solution! When the
    // enemy receives damage, it can put the damage in its input. This means its states will be able
    // to see the damage and respond accordingly (transitioning to ExampleHurtState, etc.)
    
    // Implementationwise, states are MonoBehaviors that get instantiated on your character's GameObject.
    // When you change states (with NewState<>), the old state is Exit()-ed and destroyed, and the new state is
    // created and Enter()-ed. This isn't great for garbage collection, but ensures that you can use some very
    // common methods in your states if you need them (e.g. Destroy()) and also lets you think of states as
    // momentary encapsulations of data, that only exist for the duration the state is actually active.
    
    // WOW THAT WAS A LOT OF READING! So what do I need to do to make an actual character? It's actually not that bad:
    // the point of the files in this folder is to give you an easily copyable template. This script is the character
    // itself (note it's a subclass of Character), and is secretly running the state machine (check out Character.cs).
    
    public class Example : Character<ExampleState, ExampleInput, ExampleTransitionInfo>, IDamageReceiver
    {
        // This character might have a rigidbody, etc. that its states would want to access to mess around with
        // to provide the character behavior. You should expose them here (they'll show up in the Inspector).

        public Rigidbody body;
        
        // When this character starts, it needs to enter some state by default.
        // This example character has a WalkState, so let's start there.

        private void Start()
        {
            NewState<States.ExampleWalkState>();
        }

        // This method is called right before the character runs its current state. What outside stuff does the character always need to know,
        // no matter what state it's in? Let's say it always needs to know whether or not its friend is close (random value because idc)

        protected override void ReadInput()
        {
            input.friendIsClose = Random.value > 0.5f;
        }

        // Hey look! This character can be damaged: this is the method that the interface IDamageReceiver (see the class signature) requires us to implement.
        // When something deals this character damage, it puts it in its input to be read by the current state the next time its logic is run.

        void IDamageReceiver.TakeDamage(DamageQuery damage)
        {
            input.damage = damage;
        }

        bool IDamageReceiver.IsVulnerableToPlayer()
        {
            return true;
        }
    }

    // This is the input that this character's states will have access to. You can put basically anything
    // in here that your character needs to know about. The idea is to put a layer of indirection between
    // your character's state machine and the outside world (because stuff outside your character shouldn't
    // really know that its state machine exists, and vice versa.)

    public class ExampleInput : CharacterInput
    {
        public bool friendIsClose;
        public DamageQuery? damage;

        // Your character's input also needs to be able to be "cleaned" (which happens after the current state's logic is run.)
        // Note that which values you "clean" and which you don't is very crucial - if you're storing a "key down" kind of event,
        // like receiving damage, you definitely want to reset it after it's used once by state logic. But stuff like movement input,
        // which might be spread over multiple logic updates if the game is visually lagging really badly, should usually be preserved
        // so that it "buffers" / "smears" over the missing input. In general, resetting to compiler-default values is fine.

        public override void Clean()
        {
            damage = null;
        }
    }

    // This class doesn't do much, but is here for strong typing. Check out the bottom of ExampleTalkState.cs for a use case.

    public abstract class ExampleTransitionInfo : CharacterTransitionInfo
    {

    }
}