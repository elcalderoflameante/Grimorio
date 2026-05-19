package com.grimorio.station_app

import android.Manifest
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.media.AudioManager
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.speech.RecognitionListener
import android.speech.RecognizerIntent
import android.speech.SpeechRecognizer
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.EventChannel
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    private val audioChannelName = "com.grimorio.station_app/audio"
    private val speechMethodName = "com.grimorio.station_app/speech"
    private val speechEventName = "com.grimorio.station_app/speech_events"

    private var recognizer: SpeechRecognizer? = null
    private var eventSink: EventChannel.EventSink? = null
    private var shouldRestart = false
    private var preferOffline = true
    private var pendingPermissionResult: MethodChannel.Result? = null
    private val mutedStreamVolumes = mutableMapOf<Int, Int>()
    private val handler = Handler(Looper.getMainLooper())

    companion object {
        private const val REQUEST_RECORD_AUDIO = 4101
    }

    private fun buildIntent(): Intent =
        Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH).apply {
            putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM)
            putExtra(RecognizerIntent.EXTRA_LANGUAGE, "es-EC")
            putExtra(RecognizerIntent.EXTRA_LANGUAGE_PREFERENCE, "es-EC")
            putExtra("android.speech.extra.EXTRA_ADDITIONAL_LANGUAGES", arrayOf("es-US", "es-ES", "es"))
            putExtra(RecognizerIntent.EXTRA_ONLY_RETURN_LANGUAGE_PREFERENCE, false)
            putExtra(RecognizerIntent.EXTRA_PREFER_OFFLINE, preferOffline)
            putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, false)
            putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS, 2500L)
            putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS, 1500L)
            putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_MINIMUM_LENGTH_MILLIS, 300L)
        }

    private fun startListeningMuted() {
        muteRecognitionBeeps()
        recognizer!!.startListening(buildIntent())
    }

    private fun startContinuousListening() {
        if (!hasAudioPermission()) {
            throw SecurityException("Permiso de microfono no concedido.")
        }
        if (!SpeechRecognizer.isRecognitionAvailable(this)) {
            throw IllegalStateException("Reconocimiento de voz no disponible.")
        }
        if (recognizer == null) {
            recognizer = SpeechRecognizer.createSpeechRecognizer(this)
            recognizer!!.setRecognitionListener(listener)
        }
        shouldRestart = true
        startListeningMuted()
    }

    private fun stopContinuousListening() {
        shouldRestart = false
        handler.removeCallbacksAndMessages(null)
        recognizer?.stopListening()
        recognizer?.destroy()
        recognizer = null
        unmuteRecognitionBeeps()
    }

    private fun restartSession(delayMs: Long = 200) {
        if (!shouldRestart || recognizer == null) return
        handler.postDelayed({
            if (shouldRestart && recognizer != null) {
                startListeningMuted()
            }
        }, delayMs)
    }

    private val listener = object : RecognitionListener {
        override fun onReadyForSpeech(params: Bundle?) {}
        override fun onBeginningOfSpeech() {}
        override fun onRmsChanged(rmsdB: Float) {}
        override fun onBufferReceived(buffer: ByteArray?) {}
        override fun onEndOfSpeech() {}
        override fun onPartialResults(partialResults: Bundle?) {}
        override fun onEvent(eventType: Int, params: Bundle?) {}

        override fun onResults(results: Bundle?) {
            val text = results
                ?.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION)
                ?.firstOrNull()
            if (!text.isNullOrBlank()) {
                eventSink?.success(text)
            }
            restartSession(150)
        }

        override fun onError(error: Int) {
            val name = when (error) {
                SpeechRecognizer.ERROR_AUDIO -> "AUDIO"
                SpeechRecognizer.ERROR_CLIENT -> "CLIENT"
                SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS -> "NO_PERMISSION"
                SpeechRecognizer.ERROR_NETWORK -> "NETWORK"
                SpeechRecognizer.ERROR_NETWORK_TIMEOUT -> "NETWORK_TIMEOUT"
                SpeechRecognizer.ERROR_NO_MATCH -> "NO_MATCH"
                SpeechRecognizer.ERROR_RECOGNIZER_BUSY -> "BUSY"
                SpeechRecognizer.ERROR_SERVER -> "SERVER"
                SpeechRecognizer.ERROR_SPEECH_TIMEOUT -> "SPEECH_TIMEOUT"
                12 -> "LANGUAGE_NOT_SUPPORTED"
                else -> "UNKNOWN($error)"
            }
            android.util.Log.d("KDS_Speech", "onError: $name (preferOffline=$preferOffline)")

            val delay = when (error) {
                12 -> {
                    if (preferOffline) {
                        android.util.Log.d("KDS_Speech", "Modelo offline no disponible, cambiando a online")
                        preferOffline = false
                    }
                    500L
                }
                SpeechRecognizer.ERROR_NO_MATCH,
                SpeechRecognizer.ERROR_SPEECH_TIMEOUT -> 200L
                SpeechRecognizer.ERROR_RECOGNIZER_BUSY -> {
                    recognizer?.destroy()
                    recognizer = SpeechRecognizer.createSpeechRecognizer(this@MainActivity)
                    recognizer!!.setRecognitionListener(this)
                    800L
                }
                SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS -> {
                    shouldRestart = false
                    0L
                }
                else -> 1500L
            }
            if (delay > 0L) restartSession(delay)
        }
    }

    private fun muteRecognitionBeeps() {
        val audio = getSystemService(Context.AUDIO_SERVICE) as AudioManager
        val streams = listOf(
            AudioManager.STREAM_SYSTEM,
            AudioManager.STREAM_NOTIFICATION,
            AudioManager.STREAM_RING,
        )

        for (stream in streams) {
            if (!mutedStreamVolumes.containsKey(stream)) {
                mutedStreamVolumes[stream] = audio.getStreamVolume(stream)
            }
            try {
                audio.setStreamVolume(stream, 0, 0)
            } catch (e: Exception) {
                android.util.Log.d("KDS_Speech", "No se pudo silenciar stream $stream: ${e.message}")
            }
        }
    }

    private fun unmuteRecognitionBeeps() {
        if (mutedStreamVolumes.isEmpty()) return
        val audio = getSystemService(Context.AUDIO_SERVICE) as AudioManager
        for ((stream, volume) in mutedStreamVolumes) {
            try {
                audio.setStreamVolume(stream, volume, 0)
            } catch (e: Exception) {
                android.util.Log.d("KDS_Speech", "No se pudo restaurar stream $stream: ${e.message}")
            }
        }
        mutedStreamVolumes.clear()
    }

    private fun hasAudioPermission(): Boolean =
        Build.VERSION.SDK_INT < Build.VERSION_CODES.M ||
            checkSelfPermission(Manifest.permission.RECORD_AUDIO) == PackageManager.PERMISSION_GRANTED

    private fun requestAudioPermission(result: MethodChannel.Result) {
        if (hasAudioPermission()) {
            result.success(true)
            return
        }
        if (pendingPermissionResult != null) {
            result.error("PENDING_PERMISSION", "Ya hay una solicitud de microfono en curso.", null)
            return
        }
        pendingPermissionResult = result
        requestPermissions(arrayOf(Manifest.permission.RECORD_AUDIO), REQUEST_RECORD_AUDIO)
    }

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, audioChannelName)
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "muteSystemSounds" -> { muteRecognitionBeeps(); result.success(null) }
                    "unmuteSystemSounds" -> { unmuteRecognitionBeeps(); result.success(null) }
                    else -> result.notImplemented()
                }
            }

        EventChannel(flutterEngine.dartExecutor.binaryMessenger, speechEventName)
            .setStreamHandler(object : EventChannel.StreamHandler {
                override fun onListen(arguments: Any?, sink: EventChannel.EventSink?) {
                    eventSink = sink
                }

                override fun onCancel(arguments: Any?) {
                    eventSink = null
                }
            })

        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, speechMethodName)
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "startListening" -> {
                        try {
                            startContinuousListening()
                            result.success(null)
                        } catch (e: SecurityException) {
                            result.error("NO_PERMISSION", e.message, null)
                        } catch (e: Exception) {
                            result.error("UNAVAILABLE", e.message, null)
                        }
                    }
                    "stopListening" -> { stopContinuousListening(); result.success(null) }
                    "isAvailable" -> result.success(SpeechRecognizer.isRecognitionAvailable(this@MainActivity))
                    "hasAudioPermission" -> result.success(hasAudioPermission())
                    "requestAudioPermission" -> requestAudioPermission(result)
                    "openOfflineSettings" -> {
                        val intents = listOf(
                            Intent("android.speech.action.MANAGE_OFFLINE_LANGUAGE_DATA"),
                            Intent(android.provider.Settings.ACTION_VOICE_INPUT_SETTINGS),
                            Intent(android.provider.Settings.ACTION_LOCALE_SETTINGS),
                        )
                        val launched = intents.any { intent ->
                            try { startActivity(intent); true } catch (_: Exception) { false }
                        }
                        if (launched) result.success(null)
                        else result.error("UNAVAILABLE", "No se encontro pantalla de voz offline en este dispositivo.", null)
                    }
                    else -> result.notImplemented()
                }
            }
    }

    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode != REQUEST_RECORD_AUDIO) return

        val granted = grantResults.firstOrNull() == PackageManager.PERMISSION_GRANTED
        pendingPermissionResult?.success(granted)
        pendingPermissionResult = null
    }

    override fun onDestroy() {
        stopContinuousListening()
        super.onDestroy()
    }
}
