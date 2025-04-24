# ObjectInShadowChecker
ターゲットが任意の物体の影に隠れているかを判定するサンプルプロジェクト
## Requirement
```
unity6000.0.26f
```

## How to calculate
概略
1. 影を落とす物体(C)、判定する対象のターゲット(S)、光源(L)を指定する
2. Cの各頂点から地面へLの角度でRayを飛ばし、地面との接地点を記録する（`y = 0`に固定）
3. 2.で求めた複数の点から凸包を計算し、影の形を求める
4. Sの各頂点座標（`y = 0`に固定）が全て凸包の内部に存在するなら、Sはその物体の影に隠れていると判定される

## Reference
- 凸包を求めるアルゴリズム、内外判定
https://tech-deliberate-jiro.com/convex-hull/
