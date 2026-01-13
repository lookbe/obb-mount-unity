# Unity OBB Mounter (Debug & AI Testing Tool)

This package provides a utility to pack **StreamingAssets** into an OBB (Opaque Binary Blob) expansion file and **mount it directly** to the Android filesystem.



## 🧠 The Problem
On Android, files in `StreamingAssets` are compressed inside the APK. Most native AI engines (**ONNX Runtime**, **LlamaCpp**) require a direct absolute file path (`/storage/emulated/0/...`) to load models efficiently. 

Normally, you have to copy the model from the APK to `Application.persistentDataPath`, which:
1. **Doubles the storage space** (one copy in APK, one on disk).
2. **Wastes time** on the first app launch during the extraction process.

**This tool mounts the OBB as a virtual folder**, giving you a direct path to your assets with **zero copying**.

---

## ⚠️ WARNING: DEBUG TOOL ONLY
* **OBB is Obsolete:** Google Play now uses **Play Asset Delivery (PAD)** for production.
* **Intended Use:** This is a **debugging/testing utility** to help AI developers iterate quickly without writing complex extraction logic.
* **Compatibility:** OBB mounting relies on the Android `StorageManager` API, which can vary across device manufacturers.

---

### Usage
This package is designed to be plug-and-play:
1. Drag and drop the `AndroidObbMount` **prefab** into your initial scene.
2. In your code, start a coroutine to wait for the mount point to resolve.
3. Keep `AndroidObbMount` **prefab** alive if you are changing scene, or obb file will be unmounted.

---

## 💻 Code Example

Use a coroutine to wait for the OBB to initialize. If the device doesn't support OBB mounting or an error occurs, the tool automatically falls back to the standard `Application.streamingAssetsPath`.

```csharp
using System.Collections;
using UnityEngine;
using System.IO;
using AndroidObbMount;
 
public class MyModelLoader : MonoBehaviour
{
    IEnumerator Start()
    {
        // Wait until the mount point is filled
        while (string.IsNullOrEmpty(AndroidObbMount.mountPoint))
        {
            yield return null;
        }

        string rootPath = AndroidObbMount.mountPoint;
        string modelPath = Path.Combine(rootPath, "my_ai_model.onnx");

        Debug.Log("Accessing model at: " + modelPath);
        
        // Pass modelPath directly to your native AI plugin
        MyNativePlugin.Init(modelPath);
    }
}
```

