using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    public class NotePickup : MonoBehaviour, IInteractable
    {
        public int Index;
        public string Prompt() => "Read the weathered page";
        public void Interact()
        {
            var gm = GameManager.I;
            if (!gm.Notes.Contains(Index)) gm.Notes.Add(Index);
            AudioManager.I.Play("page");
            gm.InvUI.ShowNote(Index);
            gm.Quests.Notify("note");
            gameObject.SetActive(false);
        }
        public void Refresh() { if (GameManager.I.Notes.Contains(Index)) gameObject.SetActive(false); }
    }
}
