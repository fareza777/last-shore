using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastShore
{
    /// <summary>Tomas's unfinished raft: finish it, stock it, sail home.</summary>
    public class RaftSite : MonoBehaviour, IInteractable
    {
        public GameObject FrameVisual, RaftVisual;
        const int Logs = 12, Rope = 6, Canvas = 1, Meals = 3, Flasks = 2;

        int Cooked => GameManager.I.Inv.Count("meat_cooked") + GameManager.I.Inv.Count("fish_cooked");

        public void Refresh()
        {
            bool built = GameManager.I != null && GameManager.I.RaftStage >= 1;
            if (FrameVisual != null) FrameVisual.SetActive(!built);
            if (RaftVisual != null) RaftVisual.SetActive(built);
        }

        public string Prompt()
        {
            var inv = GameManager.I.Inv;
            if (GameManager.I.RaftStage == 0)
                return "Finish the raft   logs " + Mathf.Min(inv.Count("log"), Logs) + "/" + Logs + "   rope " + Mathf.Min(inv.Count("rope"), Rope) + "/" + Rope + "   sailcloth " + Mathf.Min(inv.Count("canvas"), Canvas) + "/" + Canvas;
            return "Set sail   meals " + Mathf.Min(Cooked, Meals) + "/" + Meals + "   water flasks " + Mathf.Min(inv.Count("flask_full"), Flasks) + "/" + Flasks;
        }

        public void Interact()
        {
            var gm = GameManager.I; var inv = gm.Inv;
            if (gm.RaftStage == 0)
            {
                if (inv.Count("log") < Logs || inv.Count("rope") < Rope || inv.Count("canvas") < Canvas) { gm.Hud.Hint("You still need more materials for the raft."); return; }
                inv.Remove("log", Logs); inv.Remove("rope", Rope); inv.Remove("canvas", Canvas);
                gm.RaftStage = 1;
                AudioManager.I.Play("craft"); AudioManager.I.Play("place");
                Refresh();
                if (RaftVisual != null) { RaftVisual.transform.localScale = Vector3.one * 0.9f; Tween.Scale(RaftVisual.transform, Vector3.one, 0.5f, Ease.OutBack); }
                FX.Burst(transform.position + Vector3.up, new Color(0.8f, 0.7f, 0.5f), 30, 0.2f, 4f);
                gm.Quests.Notify("raft_built");
                return;
            }
            if (Cooked < Meals || inv.Count("flask_full") < Flasks) { gm.Hud.Hint("Stock the raft first: " + Meals + " cooked meals and " + Flasks + " full water flasks."); return; }
            EndingCinematic.Play(this);
        }
    }
}
