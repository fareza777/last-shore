using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>Ladder: moves the player between the bottom and the top of a tall structure.</summary>
    public class Climbable : MonoBehaviour, IInteractable
    {
        public Vector3 TopOffset = new Vector3(0, 6.2f, 0), BottomOffset = new Vector3(0, 0.2f, -2.6f);
        bool AtTop => GameManager.I.Player.transform.position.y > transform.position.y + TopOffset.y - 1.5f;
        public string Prompt() => AtTop ? "Climb down" : "Climb the ladder";
        public void Interact() { GameManager.I.StartCoroutine(Climb()); }
        IEnumerator Climb()
        {
            var gm = GameManager.I; bool up = !AtTop;
            gm.Cinematic = true; bool black = false;
            AudioManager.I.Play("ladder", 0.8f);
            SceneFader.FadeOut(0.45f, () => black = true);
            while (!black) yield return null;
            gm.Player.Teleport(transform.TransformPoint(up ? TopOffset : BottomOffset), gm.Player.Yaw);
            yield return new WaitForSeconds(0.5f);
            gm.Cinematic = false; SceneFader.FadeIn(0.5f);
        }
    }
}
