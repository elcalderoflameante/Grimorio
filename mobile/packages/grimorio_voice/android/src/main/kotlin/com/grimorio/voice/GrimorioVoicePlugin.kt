package com.grimorio.voice

import android.content.Context
import android.content.Intent
import android.media.AudioManager
import android.media.ToneGenerator
import android.os.Handler
import android.os.Looper
import android.os.SystemClock
import android.provider.Settings
import android.view.KeyEvent
import io.flutter.embedding.engine.plugins.FlutterPlugin
import io.flutter.embedding.engine.plugins.activity.ActivityAware
import io.flutter.embedding.engine.plugins.activity.ActivityPluginBinding
import io.flutter.plugin.common.MethodCall
import io.flutter.plugin.common.MethodChannel

class GrimorioVoicePlugin : FlutterPlugin, MethodChannel.MethodCallHandler, ActivityAware {
    companion object {
        private var current: GrimorioVoicePlugin? = null
        fun dispatchKeyEvent(event: KeyEvent): Boolean = current?.handleKey(event) ?: false
        fun cancelPress() { current?.cancel() }
        var accessibilityConnected = false
    }

    private lateinit var context: Context
    private lateinit var channel: MethodChannel
    private val handler = Handler(Looper.getMainLooper())
    private val gate = PttKeyGate()
    private var connected = false
    private var lastPing = 0L
    private var tone: ToneGenerator? = null
    private var pendingTone: MethodChannel.Result? = null
    private val preferences get() = context.getSharedPreferences("grimorio_voice_controls", Context.MODE_PRIVATE)
    private val watchdog = object : Runnable {
        override fun run() {
            val wasHeld = gate.held
            gate.isHeld(gate.pressId, SystemClock.elapsedRealtime())
            if (wasHeld && !gate.held) emit(false)
            if (gate.held) handler.postDelayed(this, 200)
        }
    }
    private val endTone = Runnable {
        stopTone()
    }

    override fun onAttachedToEngine(binding: FlutterPlugin.FlutterPluginBinding) {
        context = binding.applicationContext
        channel = MethodChannel(binding.binaryMessenger, "grimorio/voice_controls")
        channel.setMethodCallHandler(this)
    }

    // A headless Firebase engine must not steal the active app's key channel.
    override fun onAttachedToActivity(binding: ActivityPluginBinding) { current = this }
    override fun onReattachedToActivityForConfigChanges(binding: ActivityPluginBinding) { current = this }
    override fun onDetachedFromActivityForConfigChanges() = onDetachedFromActivity()
    override fun onDetachedFromActivity() {
        cancel()
        if (current === this) current = null
    }

    override fun onDetachedFromEngine(binding: FlutterPlugin.FlutterPluginBinding) {
        connected = false
        cancel()
        stopTone()
        channel.setMethodCallHandler(null)
        if (current === this) current = null
    }

    override fun onMethodCall(call: MethodCall, result: MethodChannel.Result) {
        when (call.method) {
            "settings" -> result.success(mapOf(
                "volume" to preferences.getBoolean("volume", false),
                "sounds" to preferences.getBoolean("sounds", true),
                "accessibility" to accessibilityConnected,
            ))
            "saveSettings" -> {
                preferences.edit()
                    .putBoolean("volume", call.argument<Boolean>("volume") == true)
                    .putBoolean("sounds", call.argument<Boolean>("sounds") != false).apply()
                cancel()
                stopTone()
                result.success(null)
            }
            "arm" -> {
                connected = call.argument<Boolean>("connected") == true
                lastPing = SystemClock.elapsedRealtime()
                if (!connected) cancel()
                result.success(null)
            }
            "isHeld" -> {
                val id = call.argument<Number>("id")?.toLong() ?: -1
                result.success(connected && gate.isHeld(id, SystemClock.elapsedRealtime()))
            }
            "cancel" -> { cancel(); stopTone(); result.success(null) }
            "accessibilitySettings" -> {
                try {
                    context.startActivity(Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS)
                        .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
                    result.success(null)
                } catch (error: Exception) {
                    result.error("settings_unavailable", "Abre Ajustes > Accesibilidad en el teléfono.", null)
                }
            }
            "tone" -> playTone(call.argument<String>("kind") ?: "end", result)
            else -> result.notImplemented()
        }
    }

    private fun handleKey(event: KeyEvent): Boolean {
        if (event.keyCode != KeyEvent.KEYCODE_VOLUME_UP) return false
        val wasHeld = gate.held
        val handled = when (event.action) {
            KeyEvent.ACTION_DOWN -> gate.down(
                connected && preferences.getBoolean("volume", false) &&
                    SystemClock.elapsedRealtime() - lastPing < 6000,
                event.repeatCount != 0, SystemClock.elapsedRealtime())
            KeyEvent.ACTION_UP -> gate.up()
            else -> false
        }
        if (!wasHeld && gate.held) {
            emit(true)
            handler.removeCallbacks(watchdog)
            handler.postDelayed(watchdog, 200)
        } else if (wasHeld && !gate.held) {
            emit(false)
        }
        return handled
    }

    private fun emit(down: Boolean) {
        channel.invokeMethod("key", mapOf("down" to down, "id" to gate.pressId))
    }

    private fun cancel() {
        val wasHeld = gate.held
        gate.cancel()
        handler.removeCallbacks(watchdog)
        if (wasHeld) emit(false)
    }

    private fun stopTone() {
        handler.removeCallbacks(endTone)
        tone?.stopTone()
        tone?.release()
        tone = null
        pendingTone?.success(null)
        pendingTone = null
    }

    private fun playTone(kind: String, result: MethodChannel.Result) {
        stopTone()
        if (!preferences.getBoolean("sounds", true)) { result.success(null); return }
        try {
            tone = ToneGenerator(AudioManager.STREAM_MUSIC, 45)
            val duration = if (kind == "start") 100 else 220
            val type = when (kind) {
                "start" -> ToneGenerator.TONE_PROP_BEEP
                "busy" -> ToneGenerator.TONE_PROP_NACK
                else -> ToneGenerator.TONE_PROP_BEEP2
            }
            tone?.startTone(type, duration)
            pendingTone = result
            // Complete only after the sound and a short acoustic tail, before opening the mic.
            handler.postDelayed(endTone, duration.toLong() + 100)
        } catch (error: RuntimeException) {
            tone?.release()
            tone = null
            result.success(null)
        }
    }
}
