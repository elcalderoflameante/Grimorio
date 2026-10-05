package com.grimorio.voice

/** A release or timeout requires a fresh DOWN; repeats can never reopen the microphone. */
internal class PttKeyGate {
    var held = false
        private set
    var pressId = 0L
        private set
    private var consumed = false
    private var startedAt = 0L
    private var lastEventAt = 0L

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
        if (consumed) lastEventAt = now
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
        if (now - lastEventAt > 1500 || now - startedAt >= 28000) held = false
        return held && id == pressId
    }
}
