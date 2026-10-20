package com.grimorio.station_app

import android.content.Intent
import android.net.Uri
import android.os.Build
import android.provider.Settings
import androidx.core.content.FileProvider
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel
import java.io.File

class MainActivity : FlutterActivity() {
    private val updaterChannel = "com.grimorio.station_app/updater"

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, updaterChannel)
            .setMethodCallHandler { call, result ->
                if (call.method != "installApk") {
                    result.notImplemented()
                    return@setMethodCallHandler
                }

                val path = call.argument<String>("path")
                if (path.isNullOrBlank()) {
                    result.error("INVALID_PATH", "No se recibio la ruta de la APK.", null)
                    return@setMethodCallHandler
                }

                try {
                    val apk = File(path).canonicalFile
                    val cache = cacheDir.canonicalFile
                    if (!apk.exists() || !apk.path.startsWith(cache.path + File.separator)) {
                        result.error("INVALID_APK", "La APK no pertenece al cache de la app.", null)
                        return@setMethodCallHandler
                    }

                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O &&
                        !packageManager.canRequestPackageInstalls()
                    ) {
                        val permissionIntent = Intent(
                            Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,
                            Uri.parse("package:$packageName"),
                        )
                        if (permissionIntent.resolveActivity(packageManager) != null) {
                            startActivity(permissionIntent)
                        } else {
                            startActivity(Intent(Settings.ACTION_SECURITY_SETTINGS))
                        }
                        result.success("permissionRequired")
                        return@setMethodCallHandler
                    }

                    val apkUri = FileProvider.getUriForFile(
                        this,
                        "$packageName.fileprovider",
                        apk,
                    )
                    val installIntent = Intent(Intent.ACTION_VIEW).apply {
                        setDataAndType(apkUri, "application/vnd.android.package-archive")
                        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                        addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                    }
                    startActivity(installIntent)
                    result.success("launched")
                } catch (error: Exception) {
                    result.error("INSTALL_FAILED", error.message, null)
                }
            }
    }
}
