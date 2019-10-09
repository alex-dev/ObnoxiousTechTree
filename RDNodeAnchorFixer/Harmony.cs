using System.Reflection;
using Harmony;
using UnityEngine;

namespace RDNodeAnchorFixer {
  [KSPAddon(KSPAddon.Startup.Instantly, true)]
  public class Harmony : MonoBehaviour {
    public void Awake() {
#if DEBUG
      HarmonyInstance.DEBUG = true;
#endif

      Debug.Log("[RDNodeFixer]: Patching KSP!");
      HarmonyInstance.Create("RDNodeFixer").PatchAll(Assembly.GetExecutingAssembly());
    }
  }
}
