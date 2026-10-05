package com.grimorio.voice

import android.content.Context
import android.content.Intent
import android.media.AudioManager
import android.media.MediaPlayer
import android.media.ToneGenerator
import android.os.Handler
import android.os.Looper
import android.os.PowerManager
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
    private var tone: ToneGenerator? = null
    private var player: MediaPlayer? = null
    private var pendingTone: MethodChannel.Result? = null
    private val preferences get() = context.getSharedPreferences("grimorio_voice_controls", Context.MODE_PRIVATE)
    private val isScreenInteractive: Boolean
        get() = (context.getSystemService(Context.POWER_SERVICE) as PowerManager).isInteractive
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
    override fun onDetachedFromActivityForConfigChanges() = Unit

    // Keep routing physical keys to this engine while its foreground voice
    // service remains alive. Android/XOS may detach the Activity shortly after
    // the app is backgrounded or the screen turns off.
    override fun onDetachedFromActivity() = Unit

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
                connected && preferences.getBoolean("volume", false) && isScreenInteractive,
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
        player?.setOnCompletionListener(null)
        player?.setOnErrorListener(null)
        try {
            player?.stop()
        } catch (_: IllegalStateException) {}
        player?.release()
        player = null
        tone?.stopTone()
        tone?.release()
        tone = null
        val pending = pendingTone
        pendingTone = null
        pending?.success(null)
    }

    private fun playTone(kind: String, result: MethodChannel.Result) {
        stopTone()
        if (!preferences.getBoolean("sounds", true)) { result.success(null); return }
        try {
            if (kind == "start" || kind == "end") {
                val media = MediaPlayer.create(context, R.raw.walkie_talkie)
                if (media == null) {
                    result.success(null)
                    return
                }
                player = media
                pendingTone = result
                media.setOnCompletionListener { stopTone() }
                media.setOnErrorListener { _, _, _ ->
                    stopTone()
                    true
                }
                media.start()
                // Safety fallback in case a vendor omits the completion callback.
                handler.postDelayed(endTone, 2000)
                return
            }
            tone = ToneGenerator(AudioManager.STREAM_MUSIC, 45)
            val duration = 220
            val type = ToneGenerator.TONE_PROP_NACK
            tone?.startTone(type, duration)
            pendingTone = result
            // Complete only after the sound and a short acoustic tail, before opening the mic.
            handler.postDelayed(endTone, duration.toLong() + 100)
        } catch (error: RuntimeException) {
            player?.release()
            player = null
            tone?.release()
            tone = null
            pendingTone = null
            result.success(null)
        }
    }
}
