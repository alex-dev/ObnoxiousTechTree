using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using KSP.UI.Screens;
using UnityEngine;

namespace RDNodeAnchorFixer {
  [HarmonyPatch(typeof(RDNode), "GetPosition")]
  public static class RDNode_GetPosition {
    private static readonly MethodInfo ExecuteInfo
      = typeof(RDNode_GetPosition).GetMethod("Execute", new Type[] { typeof(RDNode), typeof(RDNode.Anchor), typeof(Vector3) });

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> _) {
      yield return new CodeInstruction(OpCodes.Ldarg_0);
      yield return new CodeInstruction(OpCodes.Ldarg_1);
      yield return new CodeInstruction(OpCodes.Ldarg_2);
      yield return new CodeInstruction(OpCodes.Call, ExecuteInfo);
      yield return new CodeInstruction(OpCodes.Ret);
    }

    public static Vector3 Execute(RDNode node, RDNode.Anchor anchor, Vector3 toAnchorPos) =>
      new RDNodeWrapper(node).GetPosition(anchor, toAnchorPos);

    private struct RDNodeWrapper {
      public RDNodeWrapper(RDNode node) => this.traverse = Traverse.Create(node);

      public Vector3 GetPosition(RDNode.Anchor anchor, Vector3 toAnchorPos) {
        var position = this.DirectionVector(anchor);

        switch (anchor) {
          case RDNode.Anchor.TOP:
          case RDNode.Anchor.BOTTOM:
            return this.CalculateVerticalPosition(position, toAnchorPos);
          case RDNode.Anchor.RIGHT:
          case RDNode.Anchor.LEFT:
            return this.CalculateHorizontalPosition(position, toAnchorPos);
          default: throw new ArgumentException($"Direction {anchor} is not handled.", nameof(anchor));
        }
      }

      #region Helpers
      private Vector3 CalculateHorizontalPosition(Vector3 from, Vector3 to) =>
        new Vector3(from.x, from.y + Mathf.Sign((to - from).y) * 0.5f, from.z);

      private Vector3 CalculateVerticalPosition(Vector3 from, Vector3 to) =>
        new Vector3(from.x + Mathf.Sign((to - from).x) * 0.5f, from.y, from.z);
      #endregion

      #region Anchors Vectors
      private Vector3 AnchorTop => this.traverse.Property<Vector3>("anchor_top").Value;
      private Vector3 AnchorBottom => this.traverse.Property<Vector3>("anchor_bottom").Value;
      private Vector3 AnchorRight => this.traverse.Property<Vector3>("anchor_right").Value;
      private Vector3 AnchorLeft => this.traverse.Property<Vector3>("anchor_left").Value;

      private Vector3 DirectionVector(RDNode.Anchor direction) {
        switch (direction) {
          case RDNode.Anchor.TOP: return this.AnchorTop;
          case RDNode.Anchor.BOTTOM: return this.AnchorBottom;
          case RDNode.Anchor.RIGHT: return this.AnchorRight;
          case RDNode.Anchor.LEFT: return this.AnchorLeft;
          default: throw new ArgumentException($"Direction {direction} is not handled.", nameof(direction));
        }
      }
      #endregion

      private readonly Traverse traverse;
    }
  }
}
