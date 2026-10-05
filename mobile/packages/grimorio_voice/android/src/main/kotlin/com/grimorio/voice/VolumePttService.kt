package com.grimorio.voice

import android.accessibilityservice.AccessibilityService
import android.view.KeyEvent
import android.view.accessibility.AccessibilityEvent

class VolumePttService : AccessibilityService() {
    override fun onServiceConnected() {
        GrimorioVoicePlugin.accessibilityConnected = true
    }
    override fun onKeyEvent(event: KeyEvent): Boolean = GrimorioVoicePlugin.dispatchKeyEvent(event)
    override fun onAccessibilityEvent(event: AccessibilityEvent?) { /* No screen content is used. */ }
    override fun onInterrupt() { GrimorioVoicePlugin.cancelPress() }
    override fun onDestroy() {
        GrimorioVoicePlugin.accessibilityConnected = false
        GrimorioVoicePlugin.cancelPress()
        super.onDestroy()
    }
}
