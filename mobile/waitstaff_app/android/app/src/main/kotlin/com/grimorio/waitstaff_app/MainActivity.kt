package com.grimorio.waitstaff_app

import android.content.Intent
import android.net.Uri
import android.os.Build
import android.provider.Settings
import android.view.WindowManager
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
	private val overlayChannelName = "grimorio/overlay_bubble"
	private val screenAwakeChannelName = "grimorio/screen_awake"

	override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
		super.configureFlutterEngine(flutterEngine)

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

		if (!isChangingConfigurations && canDrawOverlays()) {
			startService(Intent(this, OverlayBubbleService::class.java))
		}
	}

	override fun onStart() {
		super.onStart()
		stopService(Intent(this, OverlayBubbleService::class.java))
	}
}
