package ai.lookbe.obbmount;

import android.os.storage.OnObbStateChangeListener;
import android.util.Log;
import com.unity3d.player.UnityPlayer;

public class ObbListener extends OnObbStateChangeListener {
    private static final String TAG = "ObbListener";

    // OBB State Constants
    private static final int MOUNTED = 1;
    private static final int ERROR_ALREADY_MOUNTED = 24;
    private static final int ERROR_PERMISSION_DENIED = 25;

    @Override
    public void onObbStateChange(String path, int state) {
        Log.d(TAG, "OBB State Change: " + state + " for path: " + path);

        // API 30+ often returns 24 (Already Mounted) if the system held onto the previous mount
        if (state == MOUNTED || state == ERROR_ALREADY_MOUNTED) {
            UnityPlayer.UnitySendMessage(
                "AndroidObbMount", 
                "OnObbMounted",    
                path
            );
        } else {
            // Log specific errors for debugging
            String errorMsg = "Mount Failed. State: " + state;
            if (state == ERROR_PERMISSION_DENIED) {
                errorMsg = "Permission Denied. Check Scoped Storage / Manifest.";
            }
            
            Log.e(TAG, errorMsg);
            
            // Optionally notify Unity about the failure
            UnityPlayer.UnitySendMessage("AndroidObbMount", "OnObbMountFailed", String.valueOf(state));
        }
    }
}