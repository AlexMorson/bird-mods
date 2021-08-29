using BepInEx;

namespace TasBird
{
    [BepInPlugin("com.alexmorson.tasbird.invalidate", "TasBird.Invalidate", "1.0")]
    public class Invalidate : BaseUnityPlugin
    {
        private void Update()
        {
            PlayerPip.Instance.InvalidateSubmission();
        }
    }
}
