using UnityEngine;
using System.Collections;

namespace TOBI.Characters.Boyd
{
    public class BoydSoundEffects : MonoBehaviour
    {
        public AudioSource source;
        public AudioClip[] flaps;
        public AudioClip slap;
        public AudioClip death;

        private int flapLast = 0;

        private float pitchVariation { get { return (Random.value - 0.5f) * 0.1f; } }

        public void Flap()
        {
            source.pitch = 1f + pitchVariation;

            int n = flaps.Length;
            int index = (flapLast % n) + Random.Range(1, n);
            flapLast = (index % n);

            AudioClip clip = flaps[flapLast];
            source.PlayOneShot(clip);
        }

        public void Slap()
        {
            source.pitch = 1f + pitchVariation;
            source.PlayOneShot(slap);
        }

        public void Death()
        {
            source.pitch = 1f + pitchVariation;
            source.PlayOneShot(death);
        }
    }
}