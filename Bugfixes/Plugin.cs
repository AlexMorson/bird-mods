using BepInEx;

namespace Bugfixes
{
    [BepInPlugin("com.alexmorson.bugfixes", "Bugfixes", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Plugin()
        {
            gameObject.AddComponent<CameraFix>();
            gameObject.AddComponent<QualityFix>();
        }
    }
}
