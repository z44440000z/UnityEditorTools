# Unity 編輯器小工具整理

| 工具名稱                  | 功能說明 |
|---------------------------|----------|
| **AnimatorClipReplacer**  | 把 Animator 中的 Clip 一次性替換掉。 |
| **BatchOperationObjects** | 提供各種批量操作物件的功能。 |
| **SkinMeshTool**         | 💀骨架替換工具，用來把飾品的 SkinnedMeshRenderer 的骨架 (`bones`) 替換成角色模型的骨架。<br>**注意**：製作服裝模型時需使用與角色完全相同的骨架進行綁定，才能正確轉寫骨架資訊。 |
| **ThumbnailCreator**      | 根據 Asset 物件生成縮圖，極簡版。 |
| **VariantReplacer**       | 將 Prefab 中的模型 A 替換成模型 B。會一併複製子物件、Tag、Layer 和 Collider。<br>**注意**：模型 A 和模型 B 的結構需相同。 |
|**BuildTimestampRecorder**|在每次Build之後，將Build的時間記錄下來|