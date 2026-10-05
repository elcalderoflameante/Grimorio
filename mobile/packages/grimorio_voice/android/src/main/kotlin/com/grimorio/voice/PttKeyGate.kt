package com.grimorio.voice

/** A release or maximum transmission timeout requires a fresh DOWN. */
internal class PttKeyGate {
    var held = false
        private set
    var pressId = 0L
        private set
    private var consumed = false
    private var startedAt = 0L

    fun down(armed: Boolean, repeat: Boolean, now: Long): Boolean {
        if (!repeat) {
            // A new physical press also recovers if Android omitted the previous UP.
            consumed = false
            held = false
            if (!armed) return false
            consumed = true
            held = true
            pressId++
            startedAt = now
        }
        return consumed
    }

    fun up(): Boolean {
        val handled = consumed
        consumed = false
        held = false
        return handled
    }

    fun cancel() { held = false }

    fun isHeld(id: Long, now: Long): Boolean {
        // Some Android vendors send only the initial DOWN and the final UP,
        // without repeat events while the physical key remains pressed.
        if (now - startedAt >= 28000) held = false
        return held && id == pressId
    }
}
