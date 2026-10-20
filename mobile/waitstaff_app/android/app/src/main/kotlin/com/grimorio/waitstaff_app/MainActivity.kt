package com.grimorio.waitstaff_app

import android.content.Intent
import android.net.Uri
import android.os.Build
import android.provider.Settings
import android.view.WindowManager
import android.view.KeyEvent
import androidx.core.content.FileProvider
import com.grimorio.voice.GrimorioVoicePlugin
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel
import java.io.File

class MainActivity : FlutterActivity() {
    private var updaterInProgress = false
    override fun dispatchKeyEvent(event: KeyEvent): Boolean =
        GrimorioVoicePlugin.dispatchKeyEvent(event) || super.dispatchKeyEvent(event)
	private val overlayChannelName = "grimorio/overlay_bubble"
	private val screenAwakeChannelName = "grimorio/screen_awake"

	override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
		super.configureFlutterEngine(flutterEngine)

        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "com.grimorio.waitstaff_app/updater")
            .setMethodCallHandler { call, result ->
                if (call.method != "installApk") {
                    result.notImplemented()
                    return@setMethodCallHandler
                }
                try {
                    val path = call.argument<String>("path")
                    if (path.isNullOrBlank()) {
                        result.error("INVALID_PATH", "No se recibio la ruta de la APK.", null)
                        return@setMethodCallHandler
                    }
                    val apk = File(path).canonicalFile
                    val updates = File(cacheDir, "updates").canonicalFile
                    if (!apk.isFile || !apk.path.startsWith(updates.path + File.separator)) {
                        result.error("INVALID_APK", "La APK no pertenece al cache de actualizaciones.", null)
                        return@setMethodCallHandler
                    }
                    val archive = packageManager.getPackageArchiveInfo(apk.path, 0)
                    val installed = packageManager.getPackageInfo(packageName, 0)
                    val incomingVersion = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P)
                        archive?.longVersionCode ?: 0 else archive?.versionCode?.toLong() ?: 0
                    val currentVersion = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P)
                        installed.longVersionCode else installed.versionCode.toLong()
                    if (archive?.packageName != packageName || incomingVersion <= currentVersion) {
                        result.error("INVALID_APK", "La APK no es una version nueva de Meseros.", null)
                        return@setMethodCallHandler
                    }
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O &&
                        !packageManager.canRequestPackageInstalls()
                    ) {
                        if (call.argument<Boolean>("requestPermission") != false) {
                            updaterInProgress = true
                            startActivity(Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,
                                Uri.parse("package:$packageName")))
                        }
                        result.success("permissionRequired")
                        return@setMethodCallHandler
                    }
                    val apkUri = FileProvider.getUriForFile(this, "$packageName.fileprovider", apk)
                    updaterInProgress = true
                    startActivity(Intent(Intent.ACTION_VIEW).apply {
                        setDataAndType(apkUri, "application/vnd.android.package-archive")
                        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                        addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                    })
                    result.success("launched")
                } catch (error: Exception) {
                    updaterInProgress = false
                    result.error("INSTALL_FAILED", error.message, null)
                }
            }

		MethodChannel(flutterEngine.dartExecutor.binaryMessenger, overlayChannelName)
			.setMethodCallHandler { call, result ->
				when (call.method) {
					"canDrawOverlays" -> {
						result.success(canDrawOverlays())
					}

					"requestOverlayPermission" -> {
						requestOverlayPermission()
						result.success(null)
					}

					"showBubble" -> {
						if (!canDrawOverlays()) {
							result.error("permission_denied", "Overlay permission is required", null)
							return@setMethodCallHandler
						}
						startService(Intent(this, OverlayBubbleService::class.java))
						result.success(true)
					}

					"hideBubble" -> {
						stopService(Intent(this, OverlayBubbleService::class.java))
						result.success(true)
					}

					else -> result.notImplemented()
				}
			}

		MethodChannel(flutterEngine.dartExecutor.binaryMessenger, screenAwakeChannelName)
			.setMethodCallHandler { call, result ->
				when (call.method) {
					"setKeepScreenOn" -> {
						val enabled = call.argument<Boolean>("enabled") ?: false
						setKeepScreenOn(enabled)
						result.success(null)
					}

					else -> result.notImplemented()
				}
			}
	}

	private fun setKeepScreenOn(enabled: Boolean) {
		if (enabled) {
			window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
		} else {
			window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
		}
	}

	private fun canDrawOverlays(): Boolean {
		return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
			Settings.canDrawOverlays(this)
		} else {
			true
		}
	}

	private fun requestOverlayPermission() {
		if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M) {
			return
		}

		val intent = Intent(
			Settings.ACTION_MANAGE_OVERLAY_PERMISSION,
			Uri.parse("package:$packageName")
		).apply {
			addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
		}
		startActivity(intent)
	}

	override fun onStop() {
		super.onStop()

		if (!isChangingConfigurations && !updaterInProgress && canDrawOverlays()) {
			startService(Intent(this, OverlayBubbleService::class.java))
		}
	}

	override fun onStart() {
		super.onStart()
		updaterInProgress = false
		stopService(Intent(this, OverlayBubbleService::class.java))
	}
}
