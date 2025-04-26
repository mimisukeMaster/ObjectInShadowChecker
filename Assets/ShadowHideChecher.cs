using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShadowHideManager : MonoBehaviour
{
    public GameObject Caster;
    public Light Light;
    public GameObject Target;

    /// <summary>
    /// 光源の角度
    /// </summary>
    private Vector3 lightAngles;

    /// <summary>
    /// 影を落とす物体の頂点
    /// </summary>
    private Vector3[] casterVertices;

    /// <summary>
    /// 計算される影の頂点
    /// </summary>
    private Vector3[] shadowVertices;

    /// <summary>
    /// 影の凸包の頂点
    /// </summary>
    private Vector3[] convexVertices;

    /// <summary>
    /// 影を落とす物体の座標の一時保存用
    /// </summary>
    private Vector3 casterTempPosition;

    /// <summary>
    /// 影を落とす物体の回転の一時保存用
    /// </summary>
    private Quaternion casterTempQuaternion;

    /// <summary>
    /// 影を落とす物体のスケールの一時保存用
    /// </summary>
    private Vector3 casterTempScale;

    /// <summary>
    /// 影を落とす物体の頂点座標の変換行列の一時保存用
    /// </summary>
    private Matrix4x4 casterTempMatrix;

    /// <summary>
    /// ターゲットのオブジェクトの頂点
    /// </summary>
    private Vector3[] targetVertices;

    /// <summary>
    /// 影に隠れているか
    /// </summary>
    private bool isHide;

    private void Start()
    {
        lightAngles = Light.transform.forward;

        // 影を落とす物体の頂点を取得
        casterVertices = GetMeshVertices(Caster);
        
        // ターゲットの頂点を取得
        targetVertices = GetMeshVertices(Target);

        shadowVertices = new Vector3[casterVertices.Length];

        casterTempPosition = Caster.transform.position;
        casterTempQuaternion = Caster.transform.rotation;
        casterTempScale = Caster.transform.lossyScale;
        casterTempMatrix = Caster.transform.localToWorldMatrix;
    }

    private void Update()
    {
        // 座標・回転・大きさ変更時でなければ何もしない
        if (casterTempPosition == Caster.transform.position && casterTempQuaternion == Caster.transform.rotation
            && casterTempScale == Caster.transform.lossyScale && Time.frameCount != 1) return;
        
        // 影を落とす物体の頂点座標を更新
        casterVertices = UpdateVertices(Caster, casterVertices, casterTempMatrix);
        for (int i = 0; i < casterVertices.Length - 1; i++)
        {
            Debug.DrawLine(casterVertices[i], casterVertices[i+1], Color.cyan);
        }
        // 影の頂点座標を更新
        shadowVertices = UpdateShadowVertices(casterVertices);
        for (int i = 0; i < shadowVertices.Length - 1; i++)
        {
            Debug.DrawLine(shadowVertices[i], shadowVertices[i+1], Color.red);
        }

        // 凸包を求める
        convexVertices = FindConvexHull(shadowVertices);
        for (int i = 0; i < convexVertices.Length - 1; i++)
        {
            Debug.DrawLine(convexVertices[i], convexVertices[i+1], Color.green);
        }

        // 内外判定
        isHide = IsVerticesInside(targetVertices, convexVertices);

        print(isHide);

        casterTempPosition = Caster.transform.position;
        casterTempQuaternion = Caster.transform.rotation;
        casterTempScale = Caster.transform.lossyScale;
        casterTempMatrix = Caster.transform.localToWorldMatrix;
    }

    /// <summary>
    /// オブジェクトの頂点のグローバル座標を取得
    /// </summary>
    private Vector3[] GetMeshVertices(GameObject meshObject)
    {
        // 頂点情報（相対座標）取得
        Vector3[] vertices;
        vertices = meshObject.GetComponent<MeshFilter>().mesh.vertices.Distinct().ToArray();

        // 各頂点を絶対座標に変換
        Matrix4x4 casterLocalToWorld = meshObject.transform.localToWorldMatrix;
        return TransformVertices(vertices, casterLocalToWorld);
    }

    /// <summary>
    /// 各頂点の座標を更新
    /// </summary>
    private Vector3[] UpdateVertices(GameObject meshObject, Vector3[] vertices, Matrix4x4 prevMatrix)
    {
        // 頂点の座標は 現在の変換行列 * 前の状態の変換行列の逆行列 で求まる
        Matrix4x4 updateMatrix = meshObject.transform.localToWorldMatrix * prevMatrix.inverse;
        return TransformVertices(vertices, updateMatrix);
    }

    /// <summary>
    /// 与えられた行列で各頂点の座標を変換
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    private Vector3[] TransformVertices(Vector3[] vertices, Matrix4x4 matrix)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
        }
        return vertices;
    }


    /// <summary>
    /// 影の頂点座標を更新
    /// </summary>
    private Vector3[] UpdateShadowVertices(Vector3[] castVertices)
    {
        Vector3[] hitPoints = new Vector3[castVertices.Length];
        for (int i = 0; i < castVertices.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(castVertices[i], lightAngles, out hit)) hitPoints[i] = hit.point;
        }
        return hitPoints;
    }

    /// <summary>
    /// 凸包の頂点を計算
    /// </summary>
    /// <returns>凸包を構成する頂点の配列（反時計回り順）</returns>
    private Vector3[] FindConvexHull(Vector3[] inputVertices)
    {
        // 各x成分とz成分のベクトルとして変換
        List<Vector2> points2D = new List<Vector2>();
        foreach (Vector3 v in inputVertices) points2D.Add(new Vector2(v.x, v.z));

        // x座標でソートし、x座標が同じ場合はz座標でソート
        // または List.Sort を使用:
        points2D.Sort((a, b) =>
        {
            int compareX = a.x.CompareTo(b.x);
            if (compareX == 0)
            {
                return a.y.CompareTo(b.y);
            }
            return compareX;
        });

        List<Vector2> hull2D = new List<Vector2>();

        // 下側凸包の構築
        foreach (Vector2 p in points2D)
        {
            // Monotone Chain を使用して凸包を構築
            // 直前の2点と現在の点が右回り、または同一直線上にある間は、直前の点を凸包から取り除く
            while (hull2D.Count >= 2 && Orientation(hull2D[hull2D.Count - 2], hull2D[hull2D.Count - 1], p) <= 0)
            {
                hull2D.RemoveAt(hull2D.Count - 1);
            }
            hull2D.Add(p);
        }

        // 上側凸包の構築
        // ソートされた点を逆順に処理
        int lowerHullCount = hull2D.Count;
        // 最右端の点 (points2D.Count - 1) は下側凸包の終点として既に含まれているため、上側凸包の開始点からは除外
        // 最左端の点 (points2D[0]) は上側凸包の終点となるため、ループ範囲に含める
        for (int i = points2D.Count - 2; i >= 0; i--)
        {
            Vector2 p = points2D[i];
            while (hull2D.Count >= lowerHullCount && Orientation(hull2D[hull2D.Count - 2], hull2D[hull2D.Count - 1], p) <= 0)
            {
                hull2D.RemoveAt(hull2D.Count - 1);
            }
            hull2D.Add(p);
        }

        // hull2Dの最後の点は最初の点と同じ（最左端の点）であるため、重複を削除する
        // ただし、計算結果が1点しかない場合は削除しない (全ての点が同一直線上にある極端な場合など)
        if (hull2D.Count > 1) hull2D.RemoveAt(hull2D.Count - 1);

        // 計算された2D凸包の点をVector3 (x, 0, z) に戻す
        Vector3[] outlineVertices = new Vector3[hull2D.Count];
        for (int i = 0; i < hull2D.Count; i++) outlineVertices[i] = new Vector3(hull2D[i].x, 0, hull2D[i].y);

        return outlineVertices;
    }

    /// <summary>
    /// 各頂点に対して内外判定を実行
    /// </summary>
    /// <returns>全て内部なら: true でなければ: false</returns>
    private bool IsVerticesInside(Vector3[] verticesToCheck, Vector3[] convexVertices)
    {
        bool isInside;
        for (int i = 0; i < verticesToCheck.Length; i++)
        {
            isInside = IsPointInside(verticesToCheck[i], convexVertices);
            if (!isInside) return false;
        }
        return true;
    }

    /// <summary>
    /// 与えられた点が凸包の内部にあるか判定
    /// </summary>
    /// <returns>内部または境界上: true でなければ: false</returns>
    private bool IsPointInside(Vector3 pointToCheck, Vector3[] convexHullVertices)
    {
        Vector2 point_2D = new Vector2(pointToCheck.x, pointToCheck.z);

        // 点が全ての辺に対して左または線上にあれば内部または境界上
        for (int i = 0; i < convexHullVertices.Length; i++)
        {
            Vector2 p1_2D = new Vector2(convexHullVertices[i].x, convexHullVertices[i].z);
            // 次の頂点へ (末尾の場合は最初に戻る)
            Vector2 p2_2D = new Vector2(convexHullVertices[(i + 1) % convexHullVertices.Length].x, convexHullVertices[(i + 1) % convexHullVertices.Length].z);

            // 2点からなる辺に対し、点の位置を判定
            if (Orientation(p1_2D, p2_2D, point_2D) < 0)
            {
                return false; // 一つでも右側にあれば外部
            }
        }
        return true;
    }

    /// <summary>
    /// p1からp2、p1からp3に向かうベクトル同士の外積を求める
    /// </summary>
    /// <returns> 正: 反時計回り (左回り) 負: 時計回り (右回り) 0: 同一直線上 </returns>
    private float Orientation(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x);
    }
}
