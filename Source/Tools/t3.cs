var gm = LastShore.GameManager.I;
var fader = GameObject.Find("[Fader]");
Debug.Log("T3 cin=" + gm.Cinematic + " dead=" + gm.Dead + " time=" + gm.Sky.TimeOfDay + " day=" + gm.Sky.Day + " fader=" + (fader ? fader.GetComponent<CanvasGroup>().alpha.ToString() : "none") + " hp=" + gm.Vitals.Health + " ts=" + Time.timeScale + " pos=" + gm.Player.transform.position + " camPos=" + gm.Player.Cam.transform.localPosition + " sun=" + RenderSettings.sun.intensity);
ScreenCapture.CaptureScreenshot("C:/Survival Fable 62/Source/Shots/p4.png", 1);
