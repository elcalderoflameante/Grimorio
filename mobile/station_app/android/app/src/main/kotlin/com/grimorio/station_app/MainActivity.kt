package com.grimorio.station_app

import io.flutter.embedding.android.FlutterActivity
import android.view.KeyEvent
import com.grimorio.voice.GrimorioVoicePlugin

class MainActivity : FlutterActivity() {
    override fun dispatchKeyEvent(event: KeyEvent): Boolean =
        GrimorioVoicePlugin.dispatchKeyEvent(event) || super.dispatchKeyEvent(event)
}
