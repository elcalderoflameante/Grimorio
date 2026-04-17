package com.grimorio.waitstaff_app

import android.app.Service
import android.content.Intent
import android.graphics.Color
import android.graphics.PixelFormat
import android.graphics.drawable.GradientDrawable
import android.os.IBinder
import android.view.Gravity
import android.view.MotionEvent
import android.view.View
import android.view.WindowManager
import android.widget.FrameLayout
import android.widget.ImageView

class OverlayBubbleService : Service() {
    private var windowManager: WindowManager? = null
    private var bubbleView: View? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        showBubble()
    }

    override fun onDestroy() {
        super.onDestroy()
        removeBubble()
    }

    private fun showBubble() {
        if (bubbleView != null) return

        val wm = getSystemService(WINDOW_SERVICE) as WindowManager
        windowManager = wm

        val bubbleSize = 56.dp
        val iconSize = 28.dp

        val params = WindowManager.LayoutParams(
            bubbleSize,
            bubbleSize,
            WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY,
            WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE or
                WindowManager.LayoutParams.FLAG_LAYOUT_NO_LIMITS,
            PixelFormat.TRANSLUCENT
        ).apply {
            gravity = Gravity.TOP or Gravity.START
            x = 20.dp
            y = 240.dp
        }

        val icon = ImageView(this).apply {
            setImageResource(R.mipmap.ic_launcher)
            alpha = 0.96f
            scaleType = ImageView.ScaleType.CENTER_CROP
            layoutParams = FrameLayout.LayoutParams(iconSize, iconSize, Gravity.CENTER)
        }

        val container = FrameLayout(this).apply {
            background = GradientDrawable().apply {
                shape = GradientDrawable.OVAL
                setColor(Color.parseColor("#F9FFFFFF"))
                setStroke(1.dp, Color.parseColor("#22000000"))
            }
            elevation = 8f
            addView(icon)
            clipToOutline = true
            setOnClickListener {
                openAppAndCloseBubble()
            }
        }

        container.setOnTouchListener(DraggableTouchListener(params))

        bubbleView = container
        wm.addView(container, params)
    }

    private fun removeBubble() {
        val view = bubbleView ?: return
        windowManager?.removeView(view)
        bubbleView = null
    }

    private fun openAppAndCloseBubble() {
        val launchIntent = packageManager.getLaunchIntentForPackage(packageName)
        launchIntent?.addFlags(
            Intent.FLAG_ACTIVITY_NEW_TASK or
                Intent.FLAG_ACTIVITY_SINGLE_TOP or
                Intent.FLAG_ACTIVITY_CLEAR_TOP
        )
        if (launchIntent != null) {
            startActivity(launchIntent)
        }
        stopSelf()
    }

    private inner class DraggableTouchListener(
        private val layoutParams: WindowManager.LayoutParams
    ) : View.OnTouchListener {
        private var initialX = 0
        private var initialY = 0
        private var initialTouchX = 0f
        private var initialTouchY = 0f

        override fun onTouch(v: View, event: MotionEvent): Boolean {
            when (event.action) {
                MotionEvent.ACTION_DOWN -> {
                    initialX = layoutParams.x
                    initialY = layoutParams.y
                    initialTouchX = event.rawX
                    initialTouchY = event.rawY
                    return false
                }

                MotionEvent.ACTION_MOVE -> {
                    layoutParams.x = initialX + (event.rawX - initialTouchX).toInt()
                    layoutParams.y = initialY + (event.rawY - initialTouchY).toInt()
                    windowManager?.updateViewLayout(v, layoutParams)
                    return true
                }
            }
            return false
        }
    }

    private val Int.dp: Int
        get() = (this * resources.displayMetrics.density).toInt()
}
