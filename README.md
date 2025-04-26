# ObjectInShadowChecker
ターゲットが任意の物体の影に隠れているかを判定するサンプルプロジェクト
## Requirement
```
unity6000.0.26f
```
## Demo
<!-- sample movie -->


## How to calculate
概略
1. 影を落とす物体(C)、判定する対象のターゲット(S)、光源(L)を指定する
2. Cの各頂点から地面へLの角度でRayを飛ばし、地面との接地点を記録する（`y = 0`に固定）
3. 2.で求めた複数の点から凸包を計算し、影の形を求める
4. Sの各頂点座標（`y = 0`に固定）が全て凸包の内部に存在するなら、Sはその物体の影に隠れていると判定される

## Reference
- 凸包を求めるアルゴリズム、内外判定<br>
  https://tech-deliberate-jiro.com/convex-hull/

## Author
 みみすけ名人 mimisukeMaster<br>

 [<img src="https://img.shields.io/badge/-X-X.svg?style=flat-square&logo=X&logoColor=white&color=black">](https://x.com/mimisukeMaster)
[<img src="https://img.shields.io/badge/-ArtStation-artstation.svg?&style=flat-square&logo=artstation&logoColor=blue&color=gray">](https://www.artstation.com/mimisukemaster)