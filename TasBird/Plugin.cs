using BepInEx;

namespace TasBird
{
    [BepInPlugin("com.alexmorson.tasbird", "TasBird", "1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Plugin()
        {
            Instance = this;

            gameObject.AddComponent<Invalidate>();
            gameObject.AddComponent<Util>();
            gameObject.AddComponent<Time>();
            gameObject.AddComponent<Data>();
            gameObject.AddComponent<Camera>();
            gameObject.AddComponent<Replay>();
            gameObject.AddComponent<Link.Link>();
            gameObject.AddComponent<Practise>();
        }

        public static Plugin Instance;
    }
}
