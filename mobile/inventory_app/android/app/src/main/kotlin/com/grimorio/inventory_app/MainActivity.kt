package com.grimorio.inventory_app

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
    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "com.grimorio.inventory_app/updater")
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
                        result.error("INVALID_APK", "La APK no es una version nueva de Inventario.", null)
                        return@setMethodCallHandler
                    }
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O && !packageManager.canRequestPackageInstalls()) {
                        if (call.argument<Boolean>("requestPermission") != false) {
                            startActivity(Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES, Uri.parse("package:$packageName")))
                        }
                        result.success("permissionRequired")
                        return@setMethodCallHandler
                    }
                    val uri = FileProvider.getUriForFile(this, "$packageName.fileprovider", apk)
                    startActivity(Intent(Intent.ACTION_VIEW).apply {
                        setDataAndType(uri, "application/vnd.android.package-archive")
                        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                        addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                    })
                    result.success("launched")
                } catch (error: Exception) {
                    result.error("INSTALL_FAILED", error.message, null)
                }
            }
    }
}
