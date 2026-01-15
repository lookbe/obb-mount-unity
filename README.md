# Unity OBB Mounter (Debug & AI Testing Tool)

This package provides a utility to pack **StreamingAssets** into an OBB (Opaque Binary Blob) expansion file and **mount it directly** to the Android filesystem.

---

## 🧠 The Problem

On Android, Unity typically handles large builds by splitting them into an APK and an OBB expansion file. However, this creates a significant technical hurdle for developers using native libraries:

### 1. Unity's Default OBB is an Unmounted Archive
By default, Unity treats the OBB like a standard zip archive. While Unity's internal `UnityWebRequest` or `Resources` APIs can "see" inside it via the `jar:file://` protocol, the Android OS does not actually **mount** the file into the filesystem. It remains a single compressed blob that the OS views as a single file.

### 2. Native Libraries Cannot Access "Internal" Paths
Most native AI engines and C++ plugins (**ONNX Runtime**, **Llama.cpp**, **TensorFlow Lite**) require a standard absolute file path (e.g., `/storage/emulated/0/...`). 
* **Native Failure:** They cannot "reach inside" a compressed OBB or APK.
* **No Abstraction:** They do not have access to Unity's internal asset management layer.
* **Result:** Even if your model is inside the OBB, passing a path to a native plugin will return a `File Not Found` error because the path isn't "real" to the Linux kernel.

### 3. The "Double Storage" Penalty
To bypass this, developers usually copy models from the OBB to `Application.persistentDataPath` at runtime. This results in:
* **Wasted Space:** A 2GB model now takes up 4GB of user storage (one copy in the OBB, one in the data folder).
* **Slow First Launch:** Users must wait minutes for the "extraction" process to finish before the AI is ready.

**This tool mounts the OBB as a virtual filesystem**, providing a direct, raw path to your assets that native C++ libraries can read immediately with **zero copying and zero storage overhead.**

---

## 🚀 Comparison

| Feature | Standard Unity OBB | **OBB Mounter (This Tool)** |
| :--- | :--- | :--- |
| **Access Method** | Unity-only APIs (WebRequest) | Standard File I/O (`File.Open`) |
| **Native Compatibility** | ❌ No | ✅ **Yes (Direct Path)** |
| **Disk Overhead** | 2x (if extracted for native) | ✅ **1x (Zero Copy)** |
| **Initial Load Time** | Slow (Extraction required) | ✅ **Instant** |

---

## ⚠️ WARNING: DEBUG TOOL ONLY
* **OBB is Obsolete:** Google Play now uses **Play Asset Delivery (PAD)** for production.
* **Intended Use:** This is a **debugging/testing utility** to help AI developers iterate quickly without writing complex extraction logic.
* **Compatibility:** OBB mounting relies on the Android `StorageManager` API, which can vary across device manufacturers and Android versions.

---

## 💻 Usage

1. **Setup:** Drag and drop the `AndroidObbMount` **prefab** into your initial scene.
2. **Persistence:** Keep the prefab alive (or use `DontDestroyOnLoad`) if you are changing scenes, or the OBB file will be unmounted.
3. **Implementation:** Use a coroutine to wait for the mount point to resolve. If mounting fails, the tool falls back to the standard `Application.streamingAssetsPath`.

```csharp
using System.Collections;
using UnityEngine;
using System.IO;

public class MyModelLoader : MonoBehaviour
{
    IEnumerator Start()
    {
        // Wait until the mount point is resolved by the prefab
        while (string.IsNullOrEmpty(AndroidObbMount.AndroidObbMount.mountPoint))
        {
            yield return null;
        }

        string rootPath = AndroidObbMount.AndroidObbMount.mountPoint;
        string modelPath = Path.Combine(rootPath, "my_ai_model.onnx");

        Debug.Log("Accessing model at: " + modelPath);
        
        // Pass modelPath directly to your native AI plugin (C++/Rust/etc)
        MyNativePlugin.Init(modelPath);
    }
}
