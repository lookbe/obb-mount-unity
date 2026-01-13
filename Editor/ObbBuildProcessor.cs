using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Diagnostics;

namespace AndroidObbMount
{
    public class ObbBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string StreamingAssetsPath = "Assets/StreamingAssets";
        private const string StreamingAssetsTempPath = "StreamingAssets_Temp";
        private static bool originalUseAPKExpansionFiles;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            if (!ObbSettingsProvider.IsEnabled)
            {
                UnityEngine.Debug.Log("[ObbBuildProcessor] OBB Automation is disabled in Project Settings.");
                return;
            }

            // Check if we are building an AAB or APK
            bool isAab = EditorUserBuildSettings.buildAppBundle;

            if (!isAab)
            {
                UnityEngine.Debug.Log($"[ObbBuildProcessor] APK Build detected. Moving {StreamingAssetsPath} to {StreamingAssetsTempPath} to exclude from APK.");

                // Handle Split Binary option: disable it during build to avoid conflict
                originalUseAPKExpansionFiles = PlayerSettings.Android.splitApplicationBinary;
                PlayerSettings.Android.splitApplicationBinary = false;


                if (Directory.Exists(StreamingAssetsPath))
                {
                    if (Directory.Exists(StreamingAssetsTempPath))
                    {
                        Directory.Delete(StreamingAssetsTempPath, true);
                    }
                    Directory.Move(StreamingAssetsPath, StreamingAssetsTempPath);

                    // Need to refresh database so Unity notices the folder is gone
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                UnityEngine.Debug.Log("[ObbBuildProcessor] AAB Build detected. StreamingAssets will be handled by Unity.");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            if (!ObbSettingsProvider.IsEnabled)
                return;

            bool isAab = EditorUserBuildSettings.buildAppBundle;

            // Restore Split Binary option
            if (!isAab)
            {
                PlayerSettings.Android.splitApplicationBinary = originalUseAPKExpansionFiles;
            }

            // Restore StreamingAssets regardless of build success/failure if it was moved
            if (Directory.Exists(StreamingAssetsTempPath))
            {
                UnityEngine.Debug.Log("[ObbBuildProcessor] Restoring StreamingAssets folder.");
                if (Directory.Exists(StreamingAssetsPath))
                {
                    // This shouldn't happen unless something went wrong, but let's be safe
                    Directory.Delete(StreamingAssetsPath, true);
                }
                Directory.Move(StreamingAssetsTempPath, StreamingAssetsPath);
                AssetDatabase.Refresh();
            }

            BuildResult result = report.summary.result;
            if (result == BuildResult.Cancelled || result == BuildResult.Failed || report.summary.totalErrors > 0)
                return;

            if (!isAab)
            {
                CreateObb(report);
            }
        }

        private void CreateObb(BuildReport report)
        {
            string packageName = PlayerSettings.applicationIdentifier;
            int versionCode = PlayerSettings.Android.bundleVersionCode;
            string outputPath = Path.GetDirectoryName(report.summary.outputPath);
            string obbFileName = $"main.{versionCode}.{packageName}.obb";
            string obbFullPath = Path.Combine(outputPath, obbFileName);

            UnityEngine.Debug.Log($"[ObbBuildProcessor] Starting OBB creation: {obbFullPath}");

            // Find tools
            string sdkPath = EditorPrefs.GetString("AndroidSdkRoot");
            string jdkPath = EditorPrefs.GetString("JdkRoot");

            // Unity 6+ might have different ways to get these, but often they are in EditorPrefs
            // or can be found relative to the editor.
            if (string.IsNullOrEmpty(jdkPath))
            {
                // Try common Unity Hub location
                string editorDataPath = EditorApplication.applicationContentsPath;
                jdkPath = Path.Combine(editorDataPath, "PlaybackEngines", "AndroidPlayer", "OpenJDK");
                if (!Directory.Exists(jdkPath)) jdkPath = "";
            }

            string jobbPath = "";
            if (!string.IsNullOrEmpty(sdkPath))
            {
                // Common locations for jobb.bat
                string[] possibleJobbPaths = new string[]
                {
                Path.Combine(sdkPath, "tools", "bin", "jobb.bat"),
                Path.Combine(sdkPath, "tools", "jobb.bat"),
                Path.Combine(sdkPath, "build-tools", "jobb.bat")
                };

                foreach (var p in possibleJobbPaths)
                {
                    if (File.Exists(p))
                    {
                        jobbPath = p;
                        break;
                    }
                }
            }

            // Final fallback for Unity's internal SDK
            if (string.IsNullOrEmpty(jobbPath))
            {
                string editorDataPath = EditorApplication.applicationContentsPath;
                jobbPath = Path.Combine(editorDataPath, "PlaybackEngines", "AndroidPlayer", "SDK", "tools", "bin", "jobb.bat");
            }

            if (!File.Exists(jobbPath))
            {
                UnityEngine.Debug.LogError("[ObbBuildProcessor] Could not find jobb.bat. OBB creation failed.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = jobbPath;
            // -d: directory, -o: output, -pn: package name, -pv: package version
            // We use the temporary StreamingAssets_Temp if it exists, otherwise StreamingAssets
            // Actually, during Postprocess, StreamingAssets should have been restored.
            startInfo.Arguments = $"-d \"{Path.GetFullPath(StreamingAssetsPath)}\" -o \"{obbFullPath}\" -pn {packageName} -pv {versionCode}";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            // Set environment variables for the process
            if (!string.IsNullOrEmpty(jdkPath))
            {
                startInfo.EnvironmentVariables["JAVA_HOME"] = jdkPath;
                string javaBin = Path.Combine(jdkPath, "bin");
                string currentPath = System.Environment.GetEnvironmentVariable("PATH");
                startInfo.EnvironmentVariables["PATH"] = javaBin + Path.PathSeparator + currentPath;
            }

            try
            {
                UnityEngine.Debug.Log($"[ObbBuildProcessor] Executing: {jobbPath} {startInfo.Arguments}");
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log($"[ObbBuildProcessor] OBB created successfully: {obbFileName}\n{output}");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[ObbBuildProcessor] OBB creation failed with exit code {process.ExitCode}\nError: {error}\nOutput: {output}");
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[ObbBuildProcessor] Exception during OBB creation: {e.Message}");
            }
        }
    }
}
