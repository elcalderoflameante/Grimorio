package com.grimorio.waitstaff_app

import android.content.Intent
import android.net.Uri
import android.os.Build
import android.provider.Settings
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
	private val channelName = "grimorio/overlay_bubble"

	override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
		super.configureFlutterEngine(flutterEngine)

		MethodChannel(flutterEngine.dartExecutor.binaryMessenger, channelName)
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
