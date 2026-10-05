package com.grimorio.voice

import org.junit.Assert.*
import org.junit.Test

class PttKeyGateTest {
    @Test fun inactiveButtonKeepsNormalVolume() {
        val gate = PttKeyGate()
        assertFalse(gate.down(false, false, 0))
        assertFalse(gate.up())
    }
    @Test fun releaseInvalidatesDelayedMicrophoneGrant() {
        val gate = PttKeyGate()
        assertTrue(gate.down(true, false, 0))
        val id = gate.pressId
        assertTrue(gate.up())
        assertFalse(gate.isHeld(id, 20))
        assertTrue(gate.down(true, false, 30))
        assertFalse(gate.isHeld(id, 40))
        assertTrue(gate.isHeld(gate.pressId, 40))
    }
    @Test fun repeatsCannotRestartAfterDisconnect() {
        val gate = PttKeyGate()
        gate.down(true, false, 0)
        gate.cancel()
        assertTrue(gate.down(true, true, 500))
        assertFalse(gate.isHeld(gate.pressId, 500))
        assertTrue(gate.up())
        gate.down(true, false, 600)
        assertTrue(gate.isHeld(gate.pressId, 600))
    }
    @Test fun holdWithoutVendorRepeatsWaitsForRelease() {
        val gate = PttKeyGate()
        gate.down(true, false, 0)
        assertTrue(gate.isHeld(gate.pressId, 1501))
        assertTrue(gate.isHeld(gate.pressId, 5000))
        assertTrue(gate.up())
        assertFalse(gate.isHeld(gate.pressId, 5001))
    }
    @Test fun missingReleaseUsesMaximumAndNextPressRecovers() {
        val gate = PttKeyGate()
        gate.down(true, false, 0)
        assertFalse(gate.isHeld(gate.pressId, 28000))
        gate.down(true, true, 28100)
        assertFalse(gate.isHeld(gate.pressId, 28100))
        gate.down(true, false, 28200)
        assertTrue(gate.isHeld(gate.pressId, 28200))
    }
    @Test fun longHoldCannotBypassMaximum() {
        val gate = PttKeyGate()
        gate.down(true, false, 0)
        for (time in 1000L..27000L step 1000) {
            gate.down(true, true, time)
            assertTrue(gate.isHeld(gate.pressId, time))
        }
        gate.down(true, true, 28000)
        assertFalse(gate.isHeld(gate.pressId, 28000))
    }
    @Test fun reconnectWhileAlreadyPressedDoesNotTakeOverVolume() {
        val gate = PttKeyGate()
        assertFalse(gate.down(false, false, 0))
        assertFalse(gate.down(true, true, 500))
        assertFalse(gate.up())
    }
}
