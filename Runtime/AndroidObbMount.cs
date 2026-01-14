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
                        Debug.LogWarning("OBB file not found: " + obbPath);
                        // fallback
                        mountPoint = Application.streamingAssetsPath;
                        return;
                    }

                    // Mount OBB
                    storageManager = activity.Call<AndroidJavaObject>("getSystemService", "storage");
                    bool result = storageManager.Call<bool>("mountObb", obbPath, null, new AndroidJavaObject("ai.lookbe.obbmount.ObbListener"));
                    if (!result)
                    {
                        Debug.LogError("Failed to call mountObb");
                        // fallback
                        mountPoint = Application.streamingAssetsPath;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error mounting OBB: " + e.Message);

                // fallback
                mountPoint = Application.streamingAssetsPath;
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
