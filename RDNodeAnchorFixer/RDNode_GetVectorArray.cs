using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using KSP.UI.Screens;
using UnityEngine;

namespace RDNodeAnchorFixer {
  [HarmonyPatch(typeof(RDNode), "GetVectorArray")]
  public static class RDNode_GetVectorArray {
    private static readonly MethodInfo ExecuteInfo
      = typeof(RDNode_GetVectorArray).GetMethod("Execute", new Type[] { typeof(RDNode), typeof(RDNode.Parent) });

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> _) {
      yield return new CodeInstruction(OpCodes.Ldarg_0);
      yield return new CodeInstruction(OpCodes.Ldarg_1);
      yield return new CodeInstruction(OpCodes.Call, ExecuteInfo);
      yield return new CodeInstruction(OpCodes.Ret);
    }

    public static List<Vector2> Execute(RDNode node, RDNode.Parent parent) =>
      new RDNodeWrapper(node).GetVectorArray(parent);

    private struct RDNodeWrapper {
      public RDNodeWrapper(RDNode node) {
        this.node = node;
        this.traverse = Traverse.Create(node);
      }

      public List<Vector2> GetVectorArray(RDNode.Parent link) {
        Func<Vector2, Vector2, RDNode.Anchor, IEnumerable<Vector2>> builder;

        if (link.parent.anchor == link.anchor)
          throw new NotImplementedException("Link between same anchors are not supported.");
        else if ((link.parent.anchor <= RDNode.Anchor.BOTTOM && link.anchor >= RDNode.Anchor.RIGHT)
          || (link.parent.anchor >= RDNode.Anchor.RIGHT && link.anchor <= RDNode.Anchor.BOTTOM))
          builder = this.BuildSingleCorner;
        else
          builder = this.BuildLine;

        return this.Generate(builder(link.parent.GetPosition(Vector2.zero),
                                     this.GetPosition(link.anchor),
                                     link.parent.anchor).ToArray()).ToList();
      }

      #region Builders
      private IEnumerable<Vector2> BuildLine(Vector2 from, Vector2 to, RDNode.Anchor anchor) {
        yield return from;

        var Δ = to - from;
        if (1 < Mathf.Abs(Δ.x) && anchor <= RDNode.Anchor.BOTTOM) {
          yield return from + (Vector2) this.TransformDirection(Vector2.up * Δ.y / 2f);
          yield return to + (Vector2) this.TransformDirection(Vector2.down * Δ.y / 2f);
        } else if (1 < Mathf.Abs(Δ.y) && anchor >= RDNode.Anchor.RIGHT) {
          yield return from + (Vector2) this.TransformDirection(Vector2.right * Δ.x / 2f);
          yield return to + (Vector2) this.TransformDirection(Vector2.left * Δ.x / 2f);
        }

        yield return to;
      }

      private IEnumerable<Vector2> BuildSingleCorner(Vector2 from, Vector2 to, RDNode.Anchor anchor) {
        yield return from;

        var Δ = to - from;
        if (anchor <= RDNode.Anchor.BOTTOM)
          yield return from + (Vector2) this.TransformDirection(Vector2.up * Δ.y);
        else if (anchor >= RDNode.Anchor.RIGHT)
          yield return from + (Vector2) this.TransformDirection(Vector2.right * Δ.x);

        yield return to;
      }
      #endregion

      #region Helpers
      private IEnumerable<Vector2> Generate(IList<Vector2> vectors) {
        yield return vectors[0];

        for (int i = 1; i < vectors.Count - 1; ++i)
          foreach (var vector in this.ChamferCorner(vectors[i - 1], vectors[i], vectors[i + 1]))
            yield return vector;

        yield return vectors[vectors.Count - 1];
      }

      private Vector2 GetPosition(RDNode.Anchor anchor) =>
        this.node.GetPosition(anchor, Vector2.zero);

      private IEnumerable<Vector2> ChamferCorner(Vector2 inPoint, Vector2 corner,
                                                 Vector2 outPoint) =>
        this.traverse.Method("chamferCorner", inPoint, corner, outPoint,
                             this.node.controller.gridArea.roundedCornerRadius,
                             this.node.controller.gridArea.lineCornerSegments)
                     .GetValue<Vector2[]>();

      private Vector3 TransformDirection(Vector3 direction) =>
        this.node.transform.TransformDirection(direction);
      #endregion

      private readonly Traverse traverse;
      private readonly RDNode node;
    }
  }
}
