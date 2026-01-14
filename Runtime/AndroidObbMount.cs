using System.IO;
using UnityEngine;

namespace AndroidObbMount
{
    public class AndroidObbMount : MonoBehaviour
    {
        public static string mountPoint { get; private set; } = string.Empty;
        private AndroidJavaObject storageManager;

        void Start()
        {
            if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
            {
                MountObb();
            }
            else
            {
                // fallback
                mountPoint = Application.streamingAssetsPath;
            }
        }

        void MountObb()
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    // Path for your main OBB
                    string obbPath =
                        activity.Call<AndroidJavaObject>("getObbDir")
                        .Call<string>("getAbsolutePath")
                        + "/main." + GetVersionCode() + "." + Application.identifier + ".obb";

                    Debug.Log("OBB Path: " + obbPath);

                    if (!File.Exists(obbPath))
                    {
                        // fallback
                        mountPoint = Application.streamingAssetsPath;
                        Debug.LogWarning("OBB file not found: " + obbPath);
                        return;
                    }

                    // Mount OBB
                    storageManager = activity.Call<AndroidJavaObject>("getSystemService", "storage");
                    bool result = storageManager.Call<bool>("mountObb", obbPath, null, new AndroidJavaObject("ai.lookbe.obbmount.ObbListener"));
                    if (!result)
                    {
                        // fallback
                        mountPoint = Application.streamingAssetsPath;
                        Debug.LogError("Failed to call mountObb");
                    }
                }
            }
            catch (System.Exception e)
            {
                // fallback
                mountPoint = Application.streamingAssetsPath;
                Debug.LogError("Error mounting OBB: " + e.Message);
            }
        }

        // Called by Java when OBB is mounted
        public void OnObbMounted(string obbPath)
        {
            mountPoint = storageManager.Call<string>("getMountedObbPath", obbPath);
            Debug.Log("OBB mounted at: " + mountPoint);
        }

        int GetVersionCode()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var pm = activity.Call<AndroidJavaObject>("getPackageManager"))
            {
                var info = pm.Call<AndroidJavaObject>(
                    "getPackageInfo", Application.identifier, 0);

                return info.Get<int>("versionCode");
            }
        }
    }
}
