using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShadowHideManager : MonoBehaviour
{
    public GameObject targetObj;
    public Light SceneLight;

    /// <summary>
    /// 光源の角度
    /// </summary>
    private Vector3 lightAngles;

    /// <summary>
    /// 対象オブジェクトの頂点
    /// </summary>
    private List<Vector3> targetVertices;

    /// <summary>
    /// 頂点の相対座標
    /// </summary>
    private Vector3[] initVertices;

    /// <summary>
    /// 計算される影の頂点
    /// </summary>
    private Vector3[] shadowVertices;

    /// <summary>
    /// 影の凸包の頂点
    /// </summary>
    private Vector3[] convexVertices;

    /// <summary>
    /// 対象オブジェクトの座標の一時保存用
    /// </summary>
    private Vector3 targetCastVector;

    /// <summary>
    /// 対象オブジェクトの回転の一時保存用
    /// </summary>
    private Quaternion targetCastQuaternion;

    private Vector3 groundPos;

    private bool isHide;

    private void Start() {
        lightAngles = SceneLight.transform.forward;
        groundPos = new Vector3(0f, 0,0f);

        // 頂点情報取得
        targetVertices = targetObj.GetComponent<MeshFilter>().mesh.vertices.ToList().Distinct().ToList();
        initVertices = targetVertices.ToArray();

        shadowVertices = new Vector3[targetVertices.Count];

        targetCastVector = targetObj.transform.position;
        targetCastQuaternion = targetObj.transform.rotation;
    }

    private void Update()
    {
        // 座標変更時に処理
        if (targetCastVector != targetObj.transform.position ||
                targetCastQuaternion != targetObj.transform.rotation || Time.frameCount == 1) {
            targetVertices = UpdateVertices(targetVertices);
        }
        
        // 影の頂点座標を更新
        shadowVertices = UpdateShadowVertices(targetVertices);

        // 凸包を求める
        convexVertices = FindConvexHull(shadowVertices);
        
        // 内外判定
        isHide = IsPointInside(groundPos, convexVertices);

        print(isHide);
    }

    /// <summary>
    /// 対象オブジェクトの頂点座標を更新
    /// </summary>
    private List<Vector3> UpdateVertices(List<Vector3> Vertices) {
        for (int i = 0; i < Vertices.Count; i++) {
            Vertices[i] = targetObj.transform.position +  initVertices[i];
        }
        targetCastVector = targetObj.transform.position;
        targetCastQuaternion = targetObj.transform.rotation;
        return Vertices;
    }

    /// <summary>
    /// 影の頂点座標を更新
    /// </summary>
    private Vector3[] UpdateShadowVertices(List<Vector3> Vertices) {
        Vector3[] hitPoints = new Vector3[Vertices.Count];
        for (int i = 0; i < Vertices.Count; i++) {
            RaycastHit hit;
            if(Physics.Raycast(Vertices[i], lightAngles, out hit)) {
                hitPoints[i] = hit.point;
                Debug.DrawLine(Vertices[i], hitPoints[i], Color.red);    
            }
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
                return a.y.CompareTo(b.y); // Vector2のyは元のVector3のz
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
    /// p1からp2、p1からp3に向かうベクトル同士の外積を求める
    /// </summary>
    /// <returns> 正: 反時計回り (左回り) 負: 時計回り (右回り) 0: 同一直線上 </returns>
    private float Orientation(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x);
    }

    /// <summary>
    /// 与えられた点が凸包の内部にあるか判定
    /// </summary>
    /// <returns>内部または境界上: true でなければ: false</returns>
    private bool IsPointInside(Vector3 pointToCheck, Vector3[] convexHullVertices)
    {
        // 凸包頂点が反時計回り順で与えられていることを前提とする
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
}
