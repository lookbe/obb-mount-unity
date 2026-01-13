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
