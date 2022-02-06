using UnityEngine;

namespace TasBird
{
    public class Invalidate : MonoBehaviour
    {
        private void Update()
        {
            PlayerPip.Instance.InvalidateSubmission();
        }
    }
}
