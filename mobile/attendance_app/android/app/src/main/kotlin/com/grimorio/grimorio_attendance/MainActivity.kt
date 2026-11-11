package com.grimorio.grimorio_attendance

import android.media.AudioManager
import android.media.ToneGenerator
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    private var feedbackChannel: MethodChannel? = null
    private var toneGenerator: ToneGenerator? = null
    private var foreground = false

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        feedbackChannel = MethodChannel(
            flutterEngine.dartExecutor.binaryMessenger,
            "grimorio.attendance/feedback"
        ).also { channel ->
            channel.setMethodCallHandler { call, result ->
                if (call.method != "play") {
                    result.notImplemented()
                } else {
                    val type = call.argument<String>("type")
                    if (type !in listOf("confirmed", "error", "test")) {
                        result.error("invalid_sound", "Sonido no válido", null)
                    } else if (!foreground) {
                        result.success(false)
                    } else {
                        try {
                            val generator = toneGenerator ?: ToneGenerator(
                                AudioManager.STREAM_MUSIC, 80
                            ).also { toneGenerator = it }
                            generator.stopTone()
                            val error = type == "error"
                            result.success(generator.startTone(
                                if (error) ToneGenerator.TONE_PROP_NACK
                                else ToneGenerator.TONE_PROP_ACK,
                                if (error) 450 else 250
                            ))
                        } catch (_: RuntimeException) {
                            releaseAudio()
                            result.success(false)
                        }
                    }
                }
            }
        }
    }

    override fun onResume() {
        super.onResume()
        foreground = true
        volumeControlStream = AudioManager.STREAM_MUSIC
    }

    override fun onPause() {
        foreground = false
        releaseAudio()
        super.onPause()
    }

    override fun cleanUpFlutterEngine(flutterEngine: FlutterEngine) {
        feedbackChannel?.setMethodCallHandler(null)
        feedbackChannel = null
        releaseAudio()
        super.cleanUpFlutterEngine(flutterEngine)
    }

    private fun releaseAudio() {
        toneGenerator?.release()
        toneGenerator = null
    }
}
